using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// Phase 373 — Pure shuffle engine (no EF, no DB, fully synchronous). Single source of truth for
    /// exam question distribution + option shuffle across StartExam (CMPController) and the
    /// reshuffle endpoints (AssessmentAdminController). Extracted to kill the previously
    /// duplicated (and divergent) <c>BuildCrossPackageAssignment</c> copies.
    ///
    /// Gates on the per-assessment flags <c>ShuffleQuestions</c> / <c>ShuffleOptions</c> (v27.0):
    /// - ON  = perilaku existing (1 paket acak / ≥2 paket sampling K-min ET-balanced) — moved VERBATIM
    ///         from CMPController (CANONICAL per-ElemenTeknis Phase 2), preserving SC#1.
    /// - OFF = deterministik: 1 paket urut q.Order; ≥2 paket round-robin index-session-stabil
    ///         1 paket UTUH per worker (filter paket kosong SEBELUM modulo), urut q.Order.
    ///
    /// Phase 416 (SHF-01..04) — SECTION-SCOPED: <c>BuildQuestionAssignment</c> kini mempartisi soal
    /// lintas-paket berdasarkan Section (kunci komposit <c>(SectionNumber, ET)</c>) lalu menjalankan
    /// algoritma distribusi existing per-Section via <see cref="BuildSectionQuestionAssignment"/>, lalu
    /// concat urut SectionNumber dengan grup "Lainnya" (SectionId=null) selalu TERAKHIR (D-15).
    /// Option-shuffle di-gate per-Section via <c>AssessmentPackageSection.ShuffleEnabled</c> (D-416-01)
    /// melalui <see cref="BuildSectionAwareOptionShuffle"/>. Precedence induk/anak (D-14): induk
    /// <c>ShuffleQuestions</c> OFF → SEMUA section terurut; induk ON → tiap section ikut ShuffleEnabled-nya.
    ///
    /// BACKWARD-COMPAT (invariant keystone, SHF-04): bila SEMUA <c>SectionId=null</c> → satu grup tunggal
    /// "Lainnya" → sub-fungsi dipanggil SEKALI atas seluruh kolam dengan <c>sectionShuffle = shuffleQuestions</c>
    /// = output BYTE-IDENTIK baseline pra-416 (golden-order). Partisi & sort Section memakai operasi
    /// deterministik non-RNG → TIDAK menggeser stream <c>rng</c> pada jalur all-null (Pitfall 2).
    ///
    /// Asumsi (Pitfall 3): SectionNumber dibaca dari navigasi <c>q.Section?.SectionNumber</c>. Call-site
    /// produksi (Plan 02) WAJIB memuat <c>q.Section</c> (atau menyediakan map) agar partisi benar; bila
    /// <c>q.Section</c> null, soal jatuh ke grup "Lainnya" (perilaku global lama, aman).
    ///
    /// Pure by design (only System/Linq/HcPortal.Models) → unit-testable without a database.
    /// </summary>
    public static class ShuffleEngine
    {
        // Helper: Fisher-Yates shuffle (moved from CMPController.Shuffle<T>, visibility public so the core hosts it).
        public static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Section-scoped question-assignment entry point (Phase 416, signature WAJIB tetap untuk 3 call-site).
        /// Mempartisi soal lintas-paket berdasarkan Section (kunci <c>SectionNumber</c>; null → "Lainnya")
        /// lalu menjalankan distribusi existing per-Section via <see cref="BuildSectionQuestionAssignment"/>,
        /// lalu concat urut SectionNumber (grup "Lainnya" selalu TERAKHIR, D-15).
        ///
        /// Precedence D-14: induk <paramref name="shuffleQuestions"/> OFF → sectionShuffle=false untuk SEMUA
        /// section (terurut); induk ON → sectionShuffle = ShuffleEnabled section itu ("Lainnya"/null section
        /// tak punya row → ikut induk, D-15).
        ///
        /// Invariant golden-order (SHF-04): all-null → 1 grup "Lainnya" → sub-fungsi dipanggil SEKALI atas
        /// seluruh kolam dengan sectionShuffle = shuffleQuestions → byte-identik baseline pra-416. Partisi &amp;
        /// sort memakai operasi deterministik NON-RNG (tak menggeser stream rng pada jalur all-null, Pitfall 2).
        /// </summary>
        public static List<int> BuildQuestionAssignment(List<AssessmentPackage> packages, bool shuffleQuestions, int workerIndex, Random rng)
        {
            if (packages.Count == 0) return new List<int>();

            var allQuestions = packages.SelectMany(p => p.Questions).ToList();
            if (allQuestions.Count == 0) return new List<int>();

            // Partisi key = SectionNumber (null → LainnyaKey). Operasi deterministik NON-RNG (Pitfall 2).
            // "Lainnya" selalu terakhir (D-15): OrderBy(k => k == LainnyaKey).ThenBy(k => k).
            var sectionKeys = allQuestions
                .Select(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber))
                .Distinct()
                .OrderBy(k => k == SectionStructureComparer.LainnyaKey)
                .ThenBy(k => k)
                .ToList();

            // Jalur all-null (atau single section): satu key saja → sub-fungsi dipanggil SEKALI atas
            // seluruh kolam → identik BuildQuestionAssignment lama (golden-order by construction).
            var result = new List<int>();
            foreach (var sk in sectionKeys)
            {
                var sectionPackages = SlicePackagesBySection(packages, sk);
                bool sectionShuffle = shuffleQuestions && ResolveSectionShuffle(allQuestions, sk);
                result.AddRange(BuildSectionQuestionAssignment(sectionPackages, sectionShuffle, workerIndex, rng));
            }
            return result;
        }

        /// <summary>
        /// Distribusi soal untuk SATU Section (slice paket sudah berisi hanya soal section itu). Logika =
        /// SAMA PERSIS <c>BuildQuestionAssignment</c> lama (pra-416): ON → canonical cross-package shuffle
        /// (verbatim <see cref="BuildCrossPackageAssignment"/>); OFF → deterministik 1 paket urut q.Order /
        /// ≥2 paket worker[i] dapat <c>packagesWithQuestions[workerIndex % count]</c> paket UTUH urut q.Order.
        /// Guard defensif: slice kosong → return empty (skip section), JANGAN throw (D-416-03 best-effort).
        /// </summary>
        private static List<int> BuildSectionQuestionAssignment(List<AssessmentPackage> sectionPackages, bool sectionShuffle, int workerIndex, Random rng)
        {
            if (sectionPackages.Count == 0) return new List<int>();
            if (sectionShuffle) return BuildCrossPackageAssignment(sectionPackages, rng);   // ON-path verbatim canonical

            // ---- OFF ----
            if (sectionPackages.Count == 1)
            {
                var q1 = sectionPackages[0].Questions;
                if (q1 == null || q1.Count == 0) return new List<int>();
                return q1.OrderBy(x => x.Order).Select(x => x.Id).ToList();            // SHUF-05: urut, NO shuffle
            }

            // OFF + ≥2 paket (SHUF-06)
            var packagesWithQuestions = sectionPackages
                .Where(p => p.Questions != null && p.Questions.Count > 0)              // D-02b: guard SEBELUM modulo
                .OrderBy(p => p.PackageNumber)                                         // anchor stabil
                .ToList();
            if (packagesWithQuestions.Count == 0) return new List<int>();              // guard DivideByZero (V5)
            var chosen = packagesWithQuestions[workerIndex % packagesWithQuestions.Count];   // D-02: index-session-stabil
            return chosen.Questions.OrderBy(x => x.Order).Select(x => x.Id).ToList();  // SHUF-06: paket UTUH urut Order, NO shuffle
        }

        /// <summary>
        /// Slice tiap paket menjadi AssessmentPackage shallow yang Questions-nya HANYA berisi soal milik
        /// <paramref name="sectionKey"/> (kunci = SectionNumber via <see cref="SectionStructureComparer.KeyOf"/>;
        /// LainnyaKey = soal SectionId=null). Shallow + filtered Questions (ref ke PackageQuestion yang sama,
        /// JANGAN clone objek EF). Pertahankan Id/PackageNumber agar anchor stabil. Pure helper (no RNG).
        /// </summary>
        private static List<AssessmentPackage> SlicePackagesBySection(List<AssessmentPackage> packages, int sectionKey)
        {
            var sliced = new List<AssessmentPackage>(packages.Count);
            foreach (var p in packages)
            {
                var sectionQuestions = p.Questions
                    .Where(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber) == sectionKey)
                    .ToList();
                var shallow = new AssessmentPackage
                {
                    Id = p.Id,
                    PackageNumber = p.PackageNumber,
                    AssessmentSessionId = p.AssessmentSessionId,
                    Questions = sectionQuestions   // ref sama (no deep clone)
                };
                sliced.Add(shallow);
            }
            return sliced;
        }

        /// <summary>
        /// Resolve ShuffleEnabled untuk satu Section key dari soal yang ada. Grup "Lainnya" (LainnyaKey, soal
        /// SectionId=null) tak punya row Section → effective = induk (D-15), jadi return true (gate akhir =
        /// induk ∧ true = induk). Untuk section riil: ambil ShuffleEnabled dari navigasi <c>q.Section</c> soal
        /// pertama pada section itu (semua soal section sama Section). Bila navigasi null (Section tak ter-load),
        /// default true (ikut induk; aman, scoped-shuffle senyap mengikuti perilaku global). Pure helper (no RNG).
        /// </summary>
        private static bool ResolveSectionShuffle(List<PackageQuestion> allQuestions, int sectionKey)
        {
            if (sectionKey == SectionStructureComparer.LainnyaKey) return true;        // Lainnya ikut induk (D-15)
            var section = allQuestions
                .Where(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber) == sectionKey)
                .Select(q => q.Section)
                .FirstOrDefault(s => s != null);
            return section?.ShuffleEnabled ?? true;                                   // default ikut induk bila tak ter-load
        }

        /// <summary>
        /// Per-question option shuffle. ON → dict[questionId] = shuffled option Ids. OFF → empty dict
        /// (caller serializes <c>"{}"</c> → view falls back to DB option order). Independent of question shuffle.
        /// Grading uses PackageOption.Id (not letter position), so option order never affects scoring.
        /// </summary>
        public static Dictionary<int, List<int>> BuildOptionShuffle(IEnumerable<PackageQuestion> questions, bool shuffleOptions, Random rng)
        {
            var dict = new Dictionary<int, List<int>>();
            if (!shuffleOptions) return dict;        // OFF → empty → caller serializes "{}" → view DB-order fallback
            foreach (var q in questions)
            {
                var optionIds = q.Options.Select(o => o.Id).ToList();
                Shuffle(optionIds, rng);
                dict[q.Id] = optionIds;
            }
            return dict;
        }

        /// <summary>
        /// Phase 416 D-416-01 — Section-aware option shuffle. Gate per-soal = induk
        /// <paramref name="parentShuffleOptions"/> ∧ ShuffleEnabled Section soal itu (dari navigasi
        /// <c>q.Section</c>). Soal pada Section yang OFF (atau induk OFF) TIDAK masuk dict → caller fallback
        /// DB-order (sama perilaku OFF existing). Soal "Lainnya" (SectionId=null, q.Section==null) → ikut induk
        /// (D-15): effective = parentShuffleOptions. Hanya soal effective==true yang opsinya diacak.
        ///
        /// Bila induk OFF, dict KOSONG (semua fallback). Grading by PackageOption.Id → urutan opsi tak pengaruhi skor.
        /// </summary>
        public static Dictionary<int, List<int>> BuildSectionAwareOptionShuffle(IEnumerable<PackageQuestion> assignedQuestions, bool parentShuffleOptions, Random rng)
        {
            var dict = new Dictionary<int, List<int>>();
            if (!parentShuffleOptions) return dict;  // induk OFF → semua fallback DB-order (precedence D-14)
            foreach (var q in assignedQuestions)
            {
                bool sectionEnabled = q.Section?.ShuffleEnabled ?? true;   // null section (Lainnya) → ikut induk (D-15)
                if (!sectionEnabled) continue;                            // Section OFF → soal ini fallback DB-order
                var optionIds = q.Options.Select(o => o.Id).ToList();
                Shuffle(optionIds, rng);
                dict[q.Id] = optionIds;
            }
            return dict;
        }

        /// <summary>
        /// Builds a cross-package ShuffledQuestionIds list using the ET-aware distribution algorithm.
        /// For 1 package: returns all questions shuffled (ET coverage is inherent).
        /// For N packages: Phase 1 guarantees at least one question per ElemenTeknis group (best-effort),
        /// Phase 2 fills remaining quota with balanced package distribution,
        /// Phase 3 Fisher-Yates shuffles the combined list.
        /// Falls back to original slot-list algorithm when no questions have ElemenTeknis data.
        /// All packages must be loaded with .Include(p => p.Questions) — questions ordered by q.Order.
        ///
        /// CANONICAL ON-path — moved VERBATIM from CMPController (per-ElemenTeknis Phase 2, basePerET).
        /// </summary>
        private static List<int> BuildCrossPackageAssignment(List<AssessmentPackage> packages, Random rng)
        {
            if (packages.Count == 0)
                return new List<int>();

            // WSE-01 (SHF-01 / D-04): filter empty packages BEFORE Count==1 / K=Min — mirror OFF-path :53-57.
            // So "2 paket, satu kosong" collapses into the single-package shuffle (worker dapat paket berisi)
            // dan K=packages.Min(...) tak pernah 0 (cegah batch-wide 0% Fail palsu).
            packages = packages
                .Where(p => p.Questions != null && p.Questions.Count > 0)
                .OrderBy(p => p.PackageNumber)
                .ToList();
            if (packages.Count == 0) return new List<int>();

            // Single package: shuffle question order so each worker sees a unique sequence
            if (packages.Count == 1)
            {
                var singlePackageQuestions = packages[0].Questions;
                if (singlePackageQuestions == null || !singlePackageQuestions.Any())
                    return new List<int>();
                var singlePackageIds = singlePackageQuestions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
                Shuffle(singlePackageIds, rng);
                return singlePackageIds;
            }

            // Safety fallback: use minimum question count across packages (edge case per user decision)
            int K = packages.Min(p => p.Questions.Count);
            if (K == 0)
                return new List<int>();

            // Collect all questions across all packages with their package index
            var allQuestions = packages.SelectMany((p, pIdx) =>
                p.Questions.Select(q => new { Question = q, PackageIndex = pIdx })).ToList();

            // Identify distinct ET groups (non-null ElemenTeknis values across all packages)
            var etGroups = allQuestions
                .Where(x => !string.IsNullOrWhiteSpace(x.Question.ElemenTeknis))
                .Select(x => x.Question.ElemenTeknis!)
                .Distinct()
                .ToList();

            // Fallback: if no questions have ElemenTeknis, use original slot-list algorithm
            if (etGroups.Count == 0)
            {
                // No ElemenTeknis data — fall back to original slot-list distribution
                int N0 = packages.Count;
                int baseCount0 = K / N0;
                int remainder0 = K % N0;
                var remainderIndices0 = Enumerable.Range(0, N0)
                    .OrderBy(_ => rng.Next())
                    .Take(remainder0)
                    .ToHashSet();
                var slots0 = new List<int>();
                for (int i = 0; i < N0; i++)
                {
                    int count = baseCount0 + (remainderIndices0.Contains(i) ? 1 : 0);
                    for (int j = 0; j < count; j++)
                        slots0.Add(i);
                }
                Shuffle(slots0, rng);
                var pkgCounter0 = new int[N0];
                var fallbackIds = new List<int>();
                var orderedQuestions0 = packages.Select(p => p.Questions.OrderBy(q => q.Order).ToList()).ToList();
                for (int pos = 0; pos < K; pos++)
                {
                    int pkgIdx = slots0[pos];
                    var question = orderedQuestions0[pkgIdx][pkgCounter0[pkgIdx]];
                    pkgCounter0[pkgIdx]++;
                    fallbackIds.Add(question.Id);
                }
                return fallbackIds;
            }

            // ET-aware distribution
            var selectedIds = new HashSet<int>();
            var selectedList = new List<int>();

            // Phase 1 — Guarantee one question per ET group (best-effort, capped at K)
            // NULL ElemenTeknis questions are excluded from Phase 1 (they participate in Phase 2 only)
            foreach (var etGroup in etGroups)
            {
                if (selectedIds.Count >= K) break;

                var candidates = allQuestions
                    .Where(x => x.Question.ElemenTeknis == etGroup && !selectedIds.Contains(x.Question.Id))
                    .Select(x => x.Question.Id)
                    .ToList();

                Shuffle(candidates, rng);
                if (candidates.Count > 0)
                {
                    int picked = candidates[0];
                    selectedIds.Add(picked);
                    selectedList.Add(picked);
                }
            }

            // Phase 2 — Fill remaining quota with balanced ET distribution (round-robin per-ET)
            int remaining = K - selectedIds.Count;
            if (remaining > 0)
            {
                int M = etGroups.Count;
                int basePerET = remaining / M;
                int extraCount = remaining % M;
                var extraETs = etGroups.OrderBy(_ => rng.Next()).Take(extraCount).ToHashSet();

                foreach (var et in etGroups)
                {
                    int quota = basePerET + (extraETs.Contains(et) ? 1 : 0);
                    var etCandidates = allQuestions
                        .Where(x => x.Question.ElemenTeknis == et && !selectedIds.Contains(x.Question.Id))
                        .Select(x => x.Question.Id)
                        .ToList();
                    Shuffle(etCandidates, rng);
                    int toTake = Math.Min(quota, etCandidates.Count);
                    foreach (var id in etCandidates.Take(toTake))
                    {
                        selectedIds.Add(id);
                        selectedList.Add(id);
                    }
                }

                // Fallback: jika masih kurang (ET kehabisan soal), ambil dari NULL-ET atau sisa soal manapun
                if (selectedIds.Count < K)
                {
                    var fallbackCandidates = allQuestions
                        .Where(x => !selectedIds.Contains(x.Question.Id))
                        .Select(x => x.Question.Id)
                        .ToList();
                    Shuffle(fallbackCandidates, rng);
                    foreach (var id in fallbackCandidates.Take(K - selectedIds.Count))
                    {
                        selectedIds.Add(id);
                        selectedList.Add(id);
                    }
                }
            }

            // Phase 3 — Fisher-Yates shuffle the combined list
            Shuffle(selectedList, rng);
            return selectedList;
        }
    }
}
