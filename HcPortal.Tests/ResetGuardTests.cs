// Phase 367 Plan 04 Task 2 — #20 ResetAssessment tolak IsManualEntry.
// Uji predikat single-source AssessmentAdminController.IsResettable (dipakai guard di ResetAssessment).
using HcPortal.Controllers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class ResetGuardTests
{
    // Assessment manual → tidak boleh di-reset (guard menolak).
    [Fact]
    public void Reset_ManualEntry_Blocked()
        => Assert.False(AssessmentAdminController.IsResettable(new AssessmentSession { IsManualEntry = true }));

    // Assessment online → boleh di-reset (lolos guard, lanjut logic existing).
    [Fact]
    public void Reset_Online_Allowed()
        => Assert.True(AssessmentAdminController.IsResettable(new AssessmentSession { IsManualEntry = false }));
}
