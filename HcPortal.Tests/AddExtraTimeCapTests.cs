// Phase 380 (WSE-03 / RST-04) — per-session extra-time cap (D-03).
// Tests the pure AssessmentAdminController.ExtraTimeWithinCap predicate that backs the AddExtraTime
// cap gate. Controller construction is infeasible (12-dep ctor); the cap rule is the testable unit
// ("invoke its cap logic" per plan). Total extra time per session must stay ≤ original DurationMinutes.
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class AddExtraTimeCapTests
{
    [Fact] // WSE-03: 40 already granted + 30 requested = 70 > 60 duration → REJECT (closes T-380-05)
    public void Cap_RejectsWhenTotalExceedsDuration()
    {
        Assert.False(AssessmentAdminController.ExtraTimeWithinCap(currentExtra: 40, requestMinutes: 30, durationMinutes: 60));
    }

    [Fact] // WSE-03: 10 + 30 = 40 ≤ 60 → allow
    public void Cap_AllowsWhenWithinDuration()
    {
        Assert.True(AssessmentAdminController.ExtraTimeWithinCap(currentExtra: 10, requestMinutes: 30, durationMinutes: 60));
    }

    [Fact] // WSE-03: exact boundary 30 + 30 = 60 ≤ 60 → allow (total 2× original)
    public void Cap_AllowsExactBoundary()
    {
        Assert.True(AssessmentAdminController.ExtraTimeWithinCap(currentExtra: 30, requestMinutes: 30, durationMinutes: 60));
    }

    [Fact] // WSE-03: already at cap, any further grant → REJECT
    public void Cap_RejectsWhenAlreadyAtCap()
    {
        Assert.False(AssessmentAdminController.ExtraTimeWithinCap(currentExtra: 60, requestMinutes: 5, durationMinutes: 60));
    }
}
