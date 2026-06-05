// Phase 348 P05 verify-gate — consolidated logic-bearing tests untuk 13 MAM correctness fix.
//
// Cakupan xUnit (logic-bearing yang ter-otomasi):
//   - MAM-07: PaginationHelper.Calculate clamp (overflow/underflow/empty) — INTI test di file ini.
//   - MAM-04: DeriveUserStatus essay-pending — headline Fact di sini; cakupan 6-cabang penuh di MonitoringUserStatusTests.cs (Plan 02).
//   - MAM-06: IsTrainingInitialState — headline Fact di sini; cakupan 7-case penuh di TrainingInitialStateTests.cs (Plan 03).
// MAM-01/02/03/05/08/09/10/11/12/13 = display/HTMX/SignalR/EF-query → di-cover via grep acceptance (Plan 01-04) + Playwright UAT (Task 2 SC1-4).

using HcPortal.Controllers;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class ManageAssessmentMedFixTests
{
    // ---- MAM-07: PaginationHelper.Calculate clamp ----

    [Fact]
    public void Pagination_EmptyList_CurrentPage1_TotalPages0_Skip0()
    {
        var r = PaginationHelper.Calculate(totalCount: 0, page: 1, pageSize: 20);
        Assert.Equal(1, r.CurrentPage);
        Assert.Equal(0, r.TotalPages);
        Assert.Equal(0, r.Skip);
        Assert.Equal(20, r.Take);
    }

    [Fact]
    public void Pagination_Page2Of45_Skip20_TotalPages3()
    {
        var r = PaginationHelper.Calculate(totalCount: 45, page: 2, pageSize: 20);
        Assert.Equal(2, r.CurrentPage);
        Assert.Equal(3, r.TotalPages);
        Assert.Equal(20, r.Skip);
        Assert.Equal(20, r.Take);
    }

    [Theory]
    [InlineData(99)]   // overflow jauh di atas TotalPages
    [InlineData(4)]    // overflow 1 di atas
    public void Pagination_OverflowPage_ClampedToTotalPages_SkipValid(int page)
    {
        var r = PaginationHelper.Calculate(totalCount: 45, page: page, pageSize: 20);
        Assert.Equal(3, r.CurrentPage);          // clamp ke TotalPages
        Assert.Equal(40, r.Skip);                // (3-1)*20 — valid, tak overflow
        Assert.True(r.Skip >= 0 && r.Skip < r.TotalCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Pagination_UnderflowPage_ClampedTo1_Skip0(int page)
    {
        var r = PaginationHelper.Calculate(totalCount: 45, page: page, pageSize: 20);
        Assert.Equal(1, r.CurrentPage);          // clamp ke 1
        Assert.Equal(0, r.Skip);                 // tak negatif
    }

    // ---- MAM-04: DeriveUserStatus (headline; full coverage MonitoringUserStatusTests.cs) ----

    [Fact]
    public void DeriveUserStatus_EssayPending_WinsOverCompletedAt()
    {
        // Essay flow set Status=PendingGrading + CompletedAt BERSAMAAN → PendingGrading menang.
        var result = AssessmentAdminController.DeriveUserStatus(
            AssessmentConstants.AssessmentStatus.PendingGrading,
            completedAt: System.DateTime.UtcNow,
            startedAt: System.DateTime.UtcNow);

        Assert.Equal("Menunggu Penilaian", result);
    }

    // ---- MAM-06: IsTrainingInitialState (headline; full coverage TrainingInitialStateTests.cs) ----

    [Fact]
    public void IsTrainingInitialState_NoFilters_True_AnyFilter_False()
    {
        Assert.True(AssessmentAdminController.IsTrainingInitialState(null, null, null, null, null, null));
        Assert.False(AssessmentAdminController.IsTrainingInitialState("true", null, null, null, null, null));
        Assert.False(AssessmentAdminController.IsTrainingInitialState(null, "Refinery", null, null, null, null));
    }
}
