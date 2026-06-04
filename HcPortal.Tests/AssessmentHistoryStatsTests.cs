// Phase 345 CMP06R-05: regression lock untuk AssessmentAdminController.ComputeHistoryStats (Plan 345-02).
// Math passRate exclude-pending (D-04) + all-pending guard (D-05) + averageScore exclude pending (D-07)
// + VM nullable mapping (D-11a) + group PassedCount regression-guard semantik (C-3/D-10).
// Pure static (analog CertificateStatusTests) - tanpa DbContext/HttpContext (ctor 10-dep di-bypass).

using System.Collections.Generic;
using HcPortal.Models;
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class AssessmentHistoryStatsTests
{
    // Rakit AssessmentReportItem minimal (IsPassed bool? + Score) untuk math test.
    private static AssessmentReportItem Item(bool? isPassed, int score) =>
        new() { IsPassed = isPassed, Score = score };

    [Fact]
    public void ComputeHistoryStats_Mixed_PassRateExcludesPending()
    {
        var items = new List<AssessmentReportItem> { Item(true, 80), Item(false, 60), Item(null, 0) };
        var (total, graded, pending, passed, passRate, avg) = AssessmentAdminController.ComputeHistoryStats(items);
        Assert.Equal(3, total);
        Assert.Equal(2, graded);   // D-04: denominator passRate = graded-only (IsPassed != null)
        Assert.Equal(1, pending);
        Assert.Equal(1, passed);
        Assert.Equal(50.0, passRate); // 1 pass / 2 graded (pending tidak menurunkan)
        Assert.Equal(70.0, avg);   // D-07: (80+60)/2, pending (Score 0) excluded
    }

    [Fact]
    public void ComputeHistoryStats_AllPending_NoDivByZero_PassRateZero()
    {
        var items = new List<AssessmentReportItem> { Item(null, 0), Item(null, 0) };
        var (total, graded, pending, passed, passRate, avg) = AssessmentAdminController.ComputeHistoryStats(items);
        Assert.Equal(2, total);
        Assert.Equal(0, graded);   // D-05 guard: graded==0 -> tidak div-by-zero
        Assert.Equal(2, pending);
        Assert.Equal(0, passed);
        Assert.Equal(0, passRate);
        Assert.Equal(0, avg);
    }

    [Fact]
    public void ComputeHistoryStats_AllPass_HundredPercent()
    {
        var items = new List<AssessmentReportItem> { Item(true, 90), Item(true, 100) };
        var (_, graded, _, passed, passRate, avg) = AssessmentAdminController.ComputeHistoryStats(items);
        Assert.Equal(2, graded);
        Assert.Equal(2, passed);
        Assert.Equal(100.0, passRate);
        Assert.Equal(95.0, avg);
    }

    [Fact]
    public void ComputeHistoryStats_AllFail_ZeroPercentButGraded()
    {
        // BEDA dari all-pending: graded>0 (denominator 2), passRate 0 karena 0 pass.
        var items = new List<AssessmentReportItem> { Item(false, 40), Item(false, 50) };
        var (_, graded, pending, passed, passRate, _) = AssessmentAdminController.ComputeHistoryStats(items);
        Assert.Equal(2, graded);
        Assert.Equal(0, pending);
        Assert.Equal(0, passed);
        Assert.Equal(0, passRate);
    }

    [Fact]
    public void ComputeHistoryStats_Empty_AllZero()
    {
        var items = new List<AssessmentReportItem>();
        var (total, graded, pending, passed, passRate, avg) = AssessmentAdminController.ComputeHistoryStats(items);
        Assert.Equal(0, total);
        Assert.Equal(0, graded);
        Assert.Equal(0, pending);
        Assert.Equal(0, passed);
        Assert.Equal(0, passRate);
        Assert.Equal(0, avg);
    }

    [Fact]
    public void AssessmentReportItem_NullIsPassed_StaysNull()
    {
        // D-11a: bool? ripple (CDPDashboardViewModel.cs:111) — pending tidak collapse ke false.
        var item = Item(null, 0);
        Assert.Null(item.IsPassed);
    }

    [Fact]
    public void ComputeHistoryStats_PassPlusPending_PassedCountExcludesPending()
    {
        // C-3/D-10 regression-guard: semantik "pending TIDAK terhitung passed".
        // Group PassedCount = Count(a => a.IsPassed == true) atas projeksi -> pending (null) tidak naikkan passed.
        var items = new List<AssessmentReportItem> { Item(true, 80), Item(null, 0) };
        var (total, graded, pending, passed, passRate, _) = AssessmentAdminController.ComputeHistoryStats(items);
        Assert.Equal(2, total);
        Assert.Equal(1, graded);
        Assert.Equal(1, pending);
        Assert.Equal(1, passed);      // pending tidak terhitung passed
        Assert.Equal(100.0, passRate); // 1 pass / 1 graded
    }
}
