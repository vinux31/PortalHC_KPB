using System;
using System.Collections.Generic;
using System.Linq;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 373 SHUF-04/05/06/07/08 — pure unit tests for <see cref="ShuffleEngine"/>.
/// No DB, no fixture, no [Trait("Category","Integration")] — the engine is pure so packages
/// are built in-memory. ON-path determinism asserted via fixed seed (new Random(42)).
/// </summary>
public class ShuffleEngineTests
{
    // helper: build an in-memory package (no DB). Each question gets 2 options (ids id*10, id*10+1).
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

    // ---- SHUF-05: OFF + 1 paket → urut q.Order, NO shuffle ----

    [Fact]
    public void Off_SinglePackage_ReturnsQuestionsInOrder()
    {
        // Questions added out of Order on purpose; expect sorted by Order.
        var p = Pkg(1, (11, 3, null), (10, 1, null), (12, 2, null));
        var packages = new List<AssessmentPackage> { p };

        var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: false, workerIndex: 0, rng: new Random(999));

        Assert.Equal(new List<int> { 10, 12, 11 }, result); // q.Order 1,2,3 → ids 10,12,11
    }

    // ---- SHUF-06: OFF + ≥2 paket → worker[i] dapat pkgWithQ[i % count] paket UTUH urut Order ----

    [Theory]
    [InlineData(0, 1)] // index 0 → first by PackageNumber (P1)
    [InlineData(1, 2)] // index 1 → P2
    [InlineData(2, 1)] // wraps → P1
    [InlineData(3, 2)] // wraps → P2
    public void Off_MultiPackage_WorkerIndexMapsToPackage(int workerIndex, int expectedPkgNumber)
    {
        var p1 = Pkg(1, (10, 1, null), (11, 2, null));
        var p2 = Pkg(2, (20, 1, null), (21, 2, null));
        // Pass in reversed order to prove the engine sorts by PackageNumber internally.
        var packages = new List<AssessmentPackage> { p2, p1 };

        var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: false, workerIndex: workerIndex, rng: new Random());

        var expected = packages.First(p => p.PackageNumber == expectedPkgNumber)
            .Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();
        Assert.Equal(expected, result); // full package, in q.Order (D-05: not cut to K-min)
    }

    [Fact]
    public void Off_MultiPackage_EmptyPackageExcludedBeforeModulo()
    {
        var p1 = Pkg(1, (10, 1, null), (11, 2, null));
        var p2 = Pkg(2); // empty — must be dropped BEFORE modulo (D-02b)
        var p3 = Pkg(3, (30, 1, null), (31, 2, null));
        var packages = new List<AssessmentPackage> { p1, p2, p3 };

        // packagesWithQuestions = [P1, P3]. index 0→P1, 1→P3, 2→P1 (wrap over 2, not 3).
        var idx0 = ShuffleEngine.BuildQuestionAssignment(packages, false, 0, new Random());
        var idx1 = ShuffleEngine.BuildQuestionAssignment(packages, false, 1, new Random());
        var idx2 = ShuffleEngine.BuildQuestionAssignment(packages, false, 2, new Random());

        Assert.Equal(new List<int> { 10, 11 }, idx0); // P1
        Assert.Equal(new List<int> { 30, 31 }, idx1); // P3 (P2 skipped — would be empty)
        Assert.Equal(new List<int> { 10, 11 }, idx2); // wrap to P1 over count==2, not count==3
        // P2 (empty) is never selected — idx1 maps to P3, not the empty middle package
        Assert.NotEmpty(idx1);
    }

    [Fact]
    public void Off_AllPackagesEmpty_ReturnsEmpty_NoDivideByZero()
    {
        var packages = new List<AssessmentPackage> { Pkg(1), Pkg(2) }; // both empty
        var ex = Record.Exception(() => ShuffleEngine.BuildQuestionAssignment(packages, false, 0, new Random()));
        Assert.Null(ex); // no DivideByZeroException (T-373-01)
        Assert.Empty(ShuffleEngine.BuildQuestionAssignment(packages, false, 5, new Random()));
    }

    [Fact]
    public void Off_MultiPackage_AppendNewWorker_DoesNotShiftExisting()
    {
        var p1 = Pkg(1, (10, 1, null), (11, 2, null));
        var p2 = Pkg(2, (20, 1, null), (21, 2, null));
        var packages = new List<AssessmentPackage> { p1, p2 };

        // Existing worker at index 0 maps to P1. Adding a new worker (a higher session Id → index 2)
        // does NOT change the package set, so index 0 still maps to P1 (index-stable on append).
        var before = ShuffleEngine.BuildQuestionAssignment(packages, false, 0, new Random());
        var newWorker = ShuffleEngine.BuildQuestionAssignment(packages, false, 2, new Random()); // wraps to P1
        var afterExisting = ShuffleEngine.BuildQuestionAssignment(packages, false, 0, new Random());

        Assert.Equal(before, afterExisting);       // existing worker's mapping unchanged
        Assert.Equal(before, newWorker);           // index 2 wraps to same package as index 0
    }

    // ---- SHUF-04: ON-path (existing behavior) deterministic via fixed seed ----

    [Fact]
    public void On_SinglePackage_SeedStable_ReturnsAllShuffled()
    {
        var packages = new List<AssessmentPackage> { Pkg(1, (10, 1, null), (11, 2, null), (12, 3, null), (13, 4, null)) };

        var a = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
        var b = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));

        Assert.Equal(a, b); // seed-stable
        Assert.Equal(new HashSet<int> { 10, 11, 12, 13 }, a.ToHashSet()); // contains ALL question Ids
        Assert.Equal(4, a.Count);
    }

    [Fact]
    public void On_MultiPackage_SeedStable_SamplesKMin()
    {
        var p1 = Pkg(1, (10, 1, "ET-A"), (11, 2, "ET-B"), (12, 3, "ET-A")); // 3 questions
        var p2 = Pkg(2, (20, 1, "ET-A"), (21, 2, "ET-B"));                  // 2 questions → K=min=2
        var packages = new List<AssessmentPackage> { p1, p2 };

        var a = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));
        var b = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));

        Assert.Equal(a, b);          // seed-stable
        Assert.Equal(2, a.Count);    // K = min(3,2) = 2 (sampling, not full)
    }

    // ---- SHUF-07: option shuffle ON/OFF, independent of question shuffle ----

    [Fact]
    public void Options_On_BuildsNonEmptyDict()
    {
        var questions = Pkg(1, (10, 1, null), (11, 2, null)).Questions;
        var dict = ShuffleEngine.BuildOptionShuffle(questions, shuffleOptions: true, rng: new Random(42));

        Assert.Equal(2, dict.Count);
        Assert.Equal(new HashSet<int> { 100, 101 }, dict[10].ToHashSet()); // q10 options 100,101 (reordered set preserved)
        Assert.Equal(2, dict[11].Count);
    }

    [Fact]
    public void Options_Off_ReturnsEmptyDict()
    {
        var questions = Pkg(1, (10, 1, null), (11, 2, null)).Questions;
        var dict = ShuffleEngine.BuildOptionShuffle(questions, shuffleOptions: false, rng: new Random());
        Assert.Empty(dict); // caller serializes "{}" → view DB-order fallback
    }

    [Fact]
    public void Independence_QuestionsOff_OptionsOn()
    {
        var p = Pkg(1, (10, 1, null), (11, 2, null), (12, 3, null));
        var packages = new List<AssessmentPackage> { p };

        // Questions OFF → ordered (no shuffle)
        var qIds = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: false, workerIndex: 0, rng: new Random());
        Assert.Equal(new List<int> { 10, 11, 12 }, qIds);

        // Options ON → non-empty dict (independent flag)
        var optDict = ShuffleEngine.BuildOptionShuffle(p.Questions, shuffleOptions: true, rng: new Random(42));
        Assert.NotEmpty(optDict);
        Assert.Equal(3, optDict.Count);
    }

    // ---- SHUF-08: determinism — called twice with identical input → identical output ----

    [Fact]
    public void Determinism_CalledTwice_SameInput_SameOutput()
    {
        var p1 = Pkg(1, (10, 1, null), (11, 2, null));
        var p2 = Pkg(2, (20, 1, null), (21, 2, null), (22, 3, null));
        var packages = new List<AssessmentPackage> { p1, p2 };

        // OFF ≥2: deterministic recompute → guard SavedQuestionCount never false-triggers
        var first = ShuffleEngine.BuildQuestionAssignment(packages, false, 1, new Random());
        var second = ShuffleEngine.BuildQuestionAssignment(packages, false, 1, new Random());
        Assert.Equal(first, second);
    }

    // ---- WSE-01 (SHF-01 / D-04): ON-path empty-package filter (mirror OFF-path :53-57) ----

    [Fact] // WSE-01: ON-path, 2 packages one empty → worker gets the filled package's questions (NOT empty)
    public void On_MultiPackage_OneEmpty_ReturnsFilledPackageQuestions()
    {
        var p1 = Pkg(1, (10, 1, null), (11, 2, null), (12, 3, null)); // 3 questions
        var p2 = Pkg(2);                                              // EMPTY
        var packages = new List<AssessmentPackage> { p1, p2 };
        var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
        Assert.NotEmpty(result);                                       // BUG: currently returns [] (K=Min=0)
        Assert.Equal(new HashSet<int> { 10, 11, 12 }, result.ToHashSet());
    }

    [Fact] // D-05 engine half: ON-path, all packages empty → engine returns empty (controller blocks)
    public void On_AllPackagesEmpty_ReturnsEmpty()
    {
        var packages = new List<AssessmentPackage> { Pkg(1), Pkg(2) };
        var result = ShuffleEngine.BuildQuestionAssignment(packages, shuffleQuestions: true, workerIndex: 0, rng: new Random(42));
        Assert.Empty(result);
    }
}
