using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 375 SHUF-16 (D-01) — consolidation mode-matrix sweep for <see cref="ShuffleEngine"/>.
///
/// Single-source-of-truth, high-level per-mode invariant ON TOP of the 27 existing shuffle tests
/// (ShuffleEngineTests, ShuffleToggleRulesTests, ShuffleMigration/Propagation/Reshuffle/LockGuard/
/// UpdateEndpoint, ShuffleCreatePersistence). This sweep does NOT duplicate their assertion detail
/// (D-01a): no K-min ET-balance exact, no reshuffle/propagate/migration-default checks — those stay
/// in the dedicated files. Here we touch each mode once with the invariant that defines it, plus the
/// empty-package DivideByZero guard. Determinism is asserted across ALL modes via fixed seed (Random(42)).
/// </summary>
public class ShuffleModeMatrixTests
{
    // helper: build an in-memory package (no DB). Each question gets 2 options (ids id*10, id*10+1).
    // Verbatim from ShuffleEngineTests.cs (lines 18-30) so the sweep shares the same fixture shape.
    private static AssessmentPackage Pkg(int packageNumber, params (int id, int order, string? et)[] qs)
    {
        var p = new AssessmentPackage { PackageNumber = packageNumber, Id = packageNumber };
        foreach (var (id, order, et) in qs)
            p.Questions.Add(new PackageQuestion
            {
                Id = id,
                Order = order,
                ElemenTeknis = et,
                Options = { new PackageOption { Id = id * 10 }, new PackageOption { Id = id * 10 + 1 } }
            });
        return p;
    }

    // Build `count` packages, each with >=2 questions and per-package distinct ids.
    // count==1 → 1 paket 4 soal (id 10..13, Order 1..4).
    // count>=2 → `count` paket masing-masing 2 soal (paket N → id N*10, N*10+1) supaya round-robin
    //            per-worker (OFF ≥2 paket) bisa diuji index-stabil.
    private static List<AssessmentPackage> BuildPackages(int count)
    {
        if (count == 1)
            return new List<AssessmentPackage>
            {
                Pkg(1, (10, 1, null), (11, 2, null), (12, 3, null), (13, 4, null))
            };

        var list = new List<AssessmentPackage>();
        for (int i = 0; i < count; i++)
        {
            int pkgNum = i + 1;
            int baseId = pkgNum * 10;
            list.Add(Pkg(pkgNum, (baseId, 1, null), (baseId + 1, 2, null)));
        }
        return list;
    }

    /// <summary>
    /// Mode-matrix sweep: one row per distinct mode. Asserts (a) seed-determinism in every mode,
    /// (b) the high-level question-assignment invariant that defines that mode, and (c) the option-dict
    /// ON/OFF invariant. Detail assertions live in ShuffleEngineTests (D-01a — no duplication here).
    /// </summary>
    [Theory]
    [InlineData(true,  true,  1)] // ON 1 paket      → semua soal hadir & seed-stable; opsi dict non-empty
    [InlineData(false, false, 1)] // OFF 1 paket     → urut asli q.Order; opsi dict empty
    [InlineData(false, true,  2)] // OFF ≥2 paket    → worker 0 dapat 1 paket UTUH urut Order (round-robin index); opsi non-empty
    [InlineData(true,  false, 2)] // ON ≥2 paket     → sampling K-min (non-empty); opsi dict empty
    public void ModeMatrix_Invariant(bool shuffleQuestions, bool shuffleOptions, int packageCount)
    {
        var packages = BuildPackages(packageCount);

        // (a) Determinism in EVERY mode: same fixed seed → identical assignment.
        var qIds  = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions, workerIndex: 0, rng: new Random(42));
        var qIds2 = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions, workerIndex: 0, rng: new Random(42));
        Assert.Equal(qIds, qIds2);

        // (b) Per-mode high-level question invariant.
        switch (shuffleQuestions, packageCount)
        {
            case (false, 1):
                // OFF + 1 paket → urutan asli berdasarkan q.Order (SHUF-05).
                var expectedOrder = packages[0].Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
                Assert.Equal(expectedOrder, qIds);
                break;

            case (false, 2):
                // OFF + ≥2 paket → worker 0 dapat SATU paket UTUH urut Order (SHUF-06, round-robin index).
                // Engine urut paket by PackageNumber; worker 0 → packagesWithQuestions[0].
                var firstPkg = packages.OrderBy(p => p.PackageNumber).First();
                var expectedWholePackage = firstPkg.Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
                Assert.Equal(expectedWholePackage, qIds);            // 1 paket utuh, urut asli
                Assert.True(qIds.All(id => firstPkg.Questions.Any(q => q.Id == id))); // semua id dari satu paket
                break;

            case (true, _):
                // ON → assignment shuffled ada (urutan spesifik & K-min exact sudah di ShuffleEngineTests).
                Assert.NotEmpty(qIds);
                break;
        }

        // (c) Option-dict ON/OFF invariant (independen dari question shuffle).
        var optDict = ShuffleEngine.BuildOptionShuffle(
            packages.SelectMany(p => p.Questions), shuffleOptions, new Random(42));
        if (shuffleOptions)
            Assert.NotEmpty(optDict);   // ON  → dict Fisher-Yates non-empty
        else
            Assert.Empty(optDict);      // OFF → empty dict (caller serializes "{}")
    }

    /// <summary>
    /// Guard: ≥2 paket yang SEMUANYA tanpa question harus difilter SEBELUM modulo → no DivideByZero
    /// (T-373-01). Pola dari ShuffleEngineTests.Off_AllPackagesEmpty_ReturnsEmpty_NoDivideByZero.
    /// </summary>
    [Fact]
    public void AllPackagesEmpty_NoDivideByZero()
    {
        var packages = new List<AssessmentPackage> { Pkg(1), Pkg(2) }; // dua paket, nol question
        var ex = Record.Exception(() =>
            ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: false, workerIndex: 0, rng: new Random()));
        Assert.Null(ex);                                                // tidak melempar DivideByZeroException
        Assert.Empty(ShuffleEngine.BuildQuestionAssignment(packages, false, 5, new Random())); // wrap aman
    }
}
