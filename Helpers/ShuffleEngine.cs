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
        /// Combined question-assignment entry point. ON → canonical cross-package shuffle (verbatim).
        /// OFF → deterministic: 1 paket urut q.Order; ≥2 paket worker[i] dapat
        /// <c>packagesWithQuestions[workerIndex % count]</c> paket UTUH urut q.Order (D-05, tidak dipotong K-min).
        /// </summary>
        public static List<int> BuildQuestionAssignment(List<AssessmentPackage> packages, bool shuffleQuestions, int workerIndex, Random rng)
        {
            if (packages.Count == 0) return new List<int>();
            if (shuffleQuestions) return BuildCrossPackageAssignment(packages, rng);   // ON-path verbatim canonical

            // ---- OFF ----
            if (packages.Count == 1)
            {
                var q1 = packages[0].Questions;
                if (q1 == null || q1.Count == 0) return new List<int>();
                return q1.OrderBy(x => x.Order).Select(x => x.Id).ToList();            // SHUF-05: urut, NO shuffle
            }

            // OFF + ≥2 paket (SHUF-06)
            var packagesWithQuestions = packages
                .Where(p => p.Questions != null && p.Questions.Count > 0)              // D-02b: guard SEBELUM modulo
                .OrderBy(p => p.PackageNumber)                                         // anchor stabil
                .ToList();
            if (packagesWithQuestions.Count == 0) return new List<int>();              // guard DivideByZero (V5)
            var chosen = packagesWithQuestions[workerIndex % packagesWithQuestions.Count];   // D-02: index-session-stabil
            return chosen.Questions.OrderBy(x => x.Order).Select(x => x.Id).ToList();  // SHUF-06: paket UTUH urut Order, NO shuffle
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
