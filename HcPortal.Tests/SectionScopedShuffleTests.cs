using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 416 SHF-01/02/03/04 — pure unit tests for the SECTION-SCOPED behaviour of
/// <see cref="ShuffleEngine"/>. No DB, no fixture, no [Trait("Category","Integration")] — the engine
/// is pure so packages are built in-memory (pola identik <see cref="ShuffleEngineTests"/>).
///
/// Membuktikan: golden-order regression (all-null = baseline pra-416), isolasi section (soal tak
/// bocor antar-Section), grup "Lainnya" selalu terakhir (D-15), precedence induk/anak (D-14),
/// option-shuffle di-gate per-Section (D-416-01), pooling lintas-paket per-section + cakupan ET
/// per-section (D-09 / SHF-03), dan determinisme workerIndex. Determinisme via fixed seed
/// (new Random(42)).
///
/// Suite lama <see cref="ShuffleEngineTests"/> (Phase 373) = kontrak golden-order; HARUS tetap hijau.
/// </summary>
public class SectionScopedShuffleTests
{
    // ============================ Fixture builders (ber-section) ============================

    // Build an in-memory package with per-question section info. Each question gets 2 options
    // (ids id*10, id*10+1) — sama pola Pkg(...) di ShuffleEngineTests. Untuk soal ber-section:
    //   - SectionId = sectionNumber (skalar; engine partisi via q.Section?.SectionNumber)
    //   - q.Section = AssessmentPackageSection { Id=sectionNumber, SectionNumber=sectionNumber, ShuffleEnabled }
    //     (set objek langsung karena no-DB; konsisten antar-paket saudara karena pakai sectionNumber sbg Id)
    //   - sectionNumber == null → SectionId=null, Section=null (grup "Lainnya").
    // shuffleEnabledBySection: opsional override ShuffleEnabled per SectionNumber (default true).
    private static AssessmentPackage PkgSec(
        int packageNumber,
        (int id, int order, string? et, int? sectionNumber)[] qs,
        Dictionary<int, bool>? shuffleEnabledBySection = null)
    {
        var p = new AssessmentPackage { PackageNumber = packageNumber, Id = packageNumber };
        foreach (var (id, order, et, sectionNumber) in qs)
        {
            var q = new PackageQuestion
            {
                Id = id,
                Order = order,
                ElemenTeknis = et,
                SectionId = sectionNumber,
                Options = { new PackageOption { Id = id * 10 }, new PackageOption { Id = id * 10 + 1 } }
            };
            if (sectionNumber != null)
            {
                bool shuffleEnabled = shuffleEnabledBySection != null
                    && shuffleEnabledBySection.TryGetValue(sectionNumber.Value, out var se)
                        ? se
                        : true;
                q.Section = new AssessmentPackageSection
                {
                    Id = sectionNumber.Value,
                    SectionNumber = sectionNumber.Value,
                    ShuffleEnabled = shuffleEnabled
                };
            }
            p.Questions.Add(q);
        }
        return p;
    }

    // Convenience: build the all-null golden-order fixture (≥2 paket, semua SectionId=null, ET bervariasi).
    // p1: (10,1,ET-A),(11,2,ET-B),(12,3,ET-A) ; p2: (20,1,ET-A),(21,2,ET-B) → K=min=2.
    private static List<AssessmentPackage> AllNullFixture() => new()
    {
        PkgSec(1, new (int, int, string?, int?)[]
        {
            (10, 1, "ET-A", null), (11, 2, "ET-B", null), (12, 3, "ET-A", null)
        }),
        PkgSec(2, new (int, int, string?, int?)[]
        {
            (20, 1, "ET-A", null), (21, 2, "ET-B", null)
        }),
    };

    // ============================ Golden-order baseline ============================

    // Captured pre-416 refactor: output of
    //   ShuffleEngine.BuildQuestionAssignment(AllNullFixture(), shuffleQuestions:true, workerIndex:0, new Random(42))
    // dijalankan terhadap engine PRA-refactor (Phase 373). Ini KONTRAK backward-compat (SHF-04):
    // jalur all-null (semua SectionId=null) WAJIB byte-identik dengan list ini.
    // K = min(3,2) = 2. Jangan ubah nilai ini — kalau test merah, perbaiki ENGINE (Pitfall 6).
    // Captured 2026-06-23 dari engine Phase 373 (seed 42, fixture all-null di atas).
    private static readonly List<int> GoldenOrderBaseline = new() { 12, 21 };

    // ============================ Helpers ============================

    // Lookup SectionNumber for a question Id across packages (via q.Section?.SectionNumber). null → "Lainnya".
    private static int? SectionNumberOf(IEnumerable<AssessmentPackage> packages, int questionId)
    {
        var q = packages.SelectMany(p => p.Questions).First(x => x.Id == questionId);
        return q.Section?.SectionNumber;
    }

    // ============================ Tests ============================

    // SHF-04 golden-order: all-null + seed 42 → urutan IDENTIK baseline pra-416.
    [Fact]
    public void AllNullSection_ProducesIdenticalOrderToLegacyBaseline()
    {
        var actual = ShuffleEngine.BuildQuestionAssignment(
            AllNullFixture(), shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
        Assert.Equal(GoldenOrderBaseline, actual);
    }

    // SHF-01: induk ON, 2 section → tiap ID hasil ∈ section-nya; blok-blok kontigu (no interleave).
    [Fact]
    public void ScopedShuffle_NoCrossSectionLeak()
    {
        // Section 1: ids 100-series ; Section 2: ids 200-series. 2 paket saudara (struktur sama).
        var p1 = PkgSec(1, new (int, int, string?, int?)[]
        {
            (101, 1, "ET-A", 1), (102, 2, "ET-B", 1),
            (201, 3, "ET-A", 2), (202, 4, "ET-B", 2),
        });
        var p2 = PkgSec(2, new (int, int, string?, int?)[]
        {
            (111, 1, "ET-A", 1), (112, 2, "ET-B", 1),
            (211, 3, "ET-A", 2), (212, 4, "ET-B", 2),
        });
        var packages = new List<AssessmentPackage> { p1, p2 };

        var result = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));

        // Setiap ID hasil harus map ke section 1 atau 2 (tidak null), dan urutan = blok kontigu per-section.
        var sectionSequence = result.Select(id => SectionNumberOf(packages, id)).ToList();
        Assert.All(sectionSequence, sn => Assert.NotNull(sn));

        // Blok kontigu: tak ada section yang muncul lagi setelah section lain menyela (no interleave).
        var seenSections = new List<int?>();
        int? current = null;
        foreach (var sn in sectionSequence)
        {
            if (sn != current)
            {
                Assert.DoesNotContain(sn, seenSections); // section ini belum pernah ditutup → tak boleh muncul lagi
                seenSections.Add(current);
                current = sn;
            }
        }
        // Section 1 mendahului Section 2 (urut SectionNumber).
        int firstSec2 = sectionSequence.FindIndex(sn => sn == 2);
        int lastSec1 = sectionSequence.FindLastIndex(sn => sn == 1);
        Assert.True(lastSec1 < firstSec2, "blok Section 1 harus seluruhnya mendahului blok Section 2");
    }

    // SHF-01 / D-15: section 1,2 + soal null → urutan = blok Sec1, blok Sec2, blok Lainnya (null) terakhir.
    [Fact]
    public void SectionOrder_LainnyaAlwaysLast()
    {
        var p = PkgSec(1, new (int, int, string?, int?)[]
        {
            (301, 1, "ET-A", 2),   // section 2
            (302, 2, "ET-A", 1),   // section 1
            (303, 3, "ET-A", null),// Lainnya
            (304, 4, "ET-B", 1),   // section 1
            (305, 5, "ET-B", null),// Lainnya
        });
        var packages = new List<AssessmentPackage> { p };

        var result = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));
        var sectionSequence = result.Select(id => SectionNumberOf(packages, id)).ToList();

        // Urutan grup: 1, lalu 2, lalu null (Lainnya) terakhir.
        int lastSec1 = sectionSequence.FindLastIndex(sn => sn == 1);
        int firstSec2 = sectionSequence.FindIndex(sn => sn == 2);
        int lastSec2 = sectionSequence.FindLastIndex(sn => sn == 2);
        int firstNull = sectionSequence.FindIndex(sn => sn == null);

        Assert.True(lastSec1 < firstSec2, "Section 1 sebelum Section 2");
        Assert.True(lastSec2 < firstNull, "Section 2 sebelum grup Lainnya");
        // Grup Lainnya berada di blok paling akhir.
        Assert.Equal(sectionSequence.Count - 1, sectionSequence.FindLastIndex(sn => sn == null));
    }

    // SHF-02 / D-14: induk shuffleQuestions=false → tiap section terurut Question.Order; per-section ShuffleEnabled diabaikan.
    [Fact]
    public void Precedence_ParentOff_AllOrdered()
    {
        // Section 1 ShuffleEnabled=true, Section 2 ShuffleEnabled=true — tapi induk OFF → semua urut Order.
        var p = PkgSec(1, new (int, int, string?, int?)[]
        {
            (401, 1, "ET-A", 1), (402, 2, "ET-B", 1),
            (403, 3, "ET-A", 2), (404, 4, "ET-B", 2),
        });
        var packages = new List<AssessmentPackage> { p };

        var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: false, workerIndex: 0, rng: new Random(123));

        // Induk OFF → urut global Order per blok section: Sec1 (401,402) lalu Sec2 (403,404).
        Assert.Equal(new List<int> { 401, 402, 403, 404 }, result);
    }

    // SHF-02: induk ON, Section A ShuffleEnabled=false → Section A urut Order; Section B ShuffleEnabled=true → teracak (seed-stabil).
    [Fact]
    public void Precedence_ParentOn_PerSectionToggle()
    {
        // Section 1 OFF (urut Order), Section 2 ON (acak). Banyak soal supaya acak terlihat.
        var toggles = new Dictionary<int, bool> { { 1, false }, { 2, true } };
        var p = PkgSec(1, new (int, int, string?, int?)[]
        {
            (501, 1, null, 1), (502, 2, null, 1), (503, 3, null, 1), (504, 4, null, 1),
            (511, 1, null, 2), (512, 2, null, 2), (513, 3, null, 2), (514, 4, null, 2),
        }, toggles);
        var packages = new List<AssessmentPackage> { p };

        var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));

        // Section 1 (OFF) → blok pertama urut Order persis 501,502,503,504.
        var sec1Block = result.Take(4).ToList();
        Assert.Equal(new List<int> { 501, 502, 503, 504 }, sec1Block);

        // Section 2 (ON) → blok kedua berisi semua id 511-514 (set), seed-stabil.
        var sec2Block = result.Skip(4).Take(4).ToList();
        Assert.Equal(new HashSet<int> { 511, 512, 513, 514 }, sec2Block.ToHashSet());

        // Determinisme: seed sama → output sama.
        var result2 = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));
        Assert.Equal(result, result2);
    }

    // SHF-02 / D-416-01: BuildSectionAwareOptionShuffle gate per-section (induk ∧ ShuffleEnabled):
    // soal di Section OFF tidak teracak (tak masuk dict), Section ON teracak (masuk dict).
    [Fact]
    public void OptionShuffle_GatedPerSection()
    {
        var toggles = new Dictionary<int, bool> { { 1, false }, { 2, true } };
        var p = PkgSec(1, new (int, int, string?, int?)[]
        {
            (601, 1, null, 1),   // Section 1 OFF → opsi tak teracak
            (602, 2, null, 2),   // Section 2 ON  → opsi teracak
            (603, 3, null, null),// Lainnya → ikut induk (ON)
        }, toggles);

        var dict = ShuffleEngine.BuildSectionAwareOptionShuffle(
            p.Questions, parentShuffleOptions: true, rng: new Random(42));

        // Section 1 OFF → soal 601 TIDAK masuk dict (caller fallback DB-order).
        Assert.False(dict.ContainsKey(601));
        // Section 2 ON → soal 602 masuk dict.
        Assert.True(dict.ContainsKey(602));
        Assert.Equal(new HashSet<int> { 6020, 6021 }, dict[602].ToHashSet());
        // Lainnya (null section) ikut induk ON → soal 603 masuk dict (D-15).
        Assert.True(dict.ContainsKey(603));
    }

    // SHF-03: 2 paket, Section 1 punya ET-A/ET-B; Phase 1 jamin ≥1 soal per (Section,ET) sampai K.
    [Fact]
    public void MultiPackage_EtCoveragePerSection()
    {
        // Section 1: ET-A + ET-B di tiap paket. K per-section = min jumlah soal section antar-paket.
        var p1 = PkgSec(1, new (int, int, string?, int?)[]
        {
            (701, 1, "ET-A", 1), (702, 2, "ET-B", 1), (703, 3, "ET-A", 1),
        });
        var p2 = PkgSec(2, new (int, int, string?, int?)[]
        {
            (711, 1, "ET-A", 1), (712, 2, "ET-B", 1), (713, 3, "ET-A", 1),
        });
        var packages = new List<AssessmentPackage> { p1, p2 };

        var result = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));

        // K=3 per section → cakupan ET: minimal 1 soal ET-A dan 1 soal ET-B di hasil.
        var ets = result.Select(id =>
            packages.SelectMany(p => p.Questions).First(q => q.Id == id).ElemenTeknis).ToList();
        Assert.Contains("ET-A", ets);
        Assert.Contains("ET-B", ets);
        Assert.Equal(3, result.Count); // K-min section = 3
    }

    // SHF-03: ET-A muncul di Section 1 DAN Section 2 → tiap section jamin cakupan ET-A independen.
    [Fact]
    public void EtSpanningSections_CoveredIndependently()
    {
        var p = PkgSec(1, new (int, int, string?, int?)[]
        {
            (801, 1, "ET-A", 1), (802, 2, "ET-B", 1),   // Section 1: ET-A + ET-B
            (811, 3, "ET-A", 2), (812, 4, "ET-C", 2),   // Section 2: ET-A + ET-C
        });
        var packages = new List<AssessmentPackage> { p };

        var result = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));

        // Section 1 harus mengandung ≥1 soal ET-A (independen dari Section 2).
        var sec1Ids = result.Where(id => SectionNumberOf(packages, id) == 1).ToList();
        var sec2Ids = result.Where(id => SectionNumberOf(packages, id) == 2).ToList();
        var sec1Ets = sec1Ids.Select(id => packages.SelectMany(p2 => p2.Questions).First(q => q.Id == id).ElemenTeknis).ToList();
        var sec2Ets = sec2Ids.Select(id => packages.SelectMany(p2 => p2.Questions).First(q => q.Id == id).ElemenTeknis).ToList();
        Assert.Contains("ET-A", sec1Ets); // ET-A tercakup di Section 1
        Assert.Contains("ET-A", sec2Ets); // ET-A tercakup independen di Section 2
    }

    // SHF-03 determinisme: workerIndex sama + seed sama → urutan sama.
    [Fact]
    public void Determinism_WorkerIndexStable()
    {
        var p1 = PkgSec(1, new (int, int, string?, int?)[]
        {
            (901, 1, "ET-A", 1), (902, 2, "ET-B", 1), (903, 3, "ET-A", 2), (904, 4, "ET-B", 2),
        });
        var p2 = PkgSec(2, new (int, int, string?, int?)[]
        {
            (911, 1, "ET-A", 1), (912, 2, "ET-B", 1), (913, 3, "ET-A", 2), (914, 4, "ET-B", 2),
        });
        var packages = new List<AssessmentPackage> { p1, p2 };

        var a = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));
        var b = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));
        Assert.Equal(a, b);
    }

    // SHF-04: re-roll (seed beda) tetap blok-blok per-section tak bocor (engine-level proof; wiring di Plan 02).
    [Fact]
    public void Reshuffle_SectionIsolation()
    {
        var p1 = PkgSec(1, new (int, int, string?, int?)[]
        {
            (1001, 1, "ET-A", 1), (1002, 2, "ET-B", 1), (1003, 3, "ET-A", 2), (1004, 4, "ET-B", 2),
        });
        var p2 = PkgSec(2, new (int, int, string?, int?)[]
        {
            (1011, 1, "ET-A", 1), (1012, 2, "ET-B", 1), (1013, 3, "ET-A", 2), (1014, 4, "ET-B", 2),
        });
        var packages = new List<AssessmentPackage> { p1, p2 };

        // Re-roll dengan seed berbeda (simulasi reshuffle) — isolasi section harus tetap berlaku.
        foreach (var seed in new[] { 1, 7, 99, 2026 })
        {
            var result = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(seed));
            var sectionSequence = result.Select(id => SectionNumberOf(packages, id)).ToList();
            Assert.All(sectionSequence, sn => Assert.NotNull(sn));
            // Section 1 seluruhnya mendahului Section 2 (blok kontigu, no leak).
            int lastSec1 = sectionSequence.FindLastIndex(sn => sn == 1);
            int firstSec2 = sectionSequence.FindIndex(sn => sn == 2);
            Assert.True(lastSec1 < firstSec2, $"seed {seed}: blok Section 1 harus mendahului Section 2");
        }
    }
}
