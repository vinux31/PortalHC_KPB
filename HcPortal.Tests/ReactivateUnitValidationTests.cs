// Phase 401 Plan 01 Task 3 — PSU-07 reactivation guard scaffold (RED until 401-03).
// TARGET behavior: CoachCoacheeMappingReactivate (CoachMappingController:1017-1097) HARUS
// menolak reaktivasi bila AssignmentUnit mapping ∉ coachee active UserUnits (unit sudah dilepas).
// PENTING (D-05): window korelasi AF-4 (DateDiffSecond ±5 di :1052-1076) WAJIB tetap utuh —
// grep guard `DateDiffSecond >= -5 && <= 5` ada di acceptance 401-03.
// Sanity (GREEN): helper buktikan primitif keputusan guard — unit dilepas (∉ active) → false.
using System;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class ReactivateUnitValidationTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Sanity (GREEN): unit "UnitDilepas" TIDAK ada di active UserUnits coachee → helper false.
    // Ini primitif keputusan guard reaktivasi (401-03 menolak bila false).
    [Fact]
    public async Task Released_unit_not_in_active_userunits_returns_false()
    {
        await using var ctx = InMemoryContext();
        var coacheeId = Guid.NewGuid().ToString();
        // coachee sekarang hanya pegang "UnitSekarang"; "UnitDilepas" sudah tak ada
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "UnitSekarang", IsPrimary = true, IsActive = true });
        await ctx.SaveChangesAsync();

        var stillValid = await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, "UnitDilepas");

        Assert.False(stillValid); // unit dilepas → reaktivasi harus ditolak (401-03)
    }

    // AF-4 window ±5s must remain — see grep guard in 401-03.
    // TODO 401-03: CoachCoacheeMappingReactivate guard (reject AssignmentUnit ∉ active UserUnits).
    [Fact(Skip = "RED until 401-03 adds reactivation unit guard (window AF-4 untouched)")]
    public Task Reactivate_rejects_when_AssignmentUnit_no_longer_in_UserUnits()
    {
        // Seed inactive mapping AssignmentUnit="UnitDilepas" where UnitDilepas ∉ coachee active
        // UserUnits → reactivation must be rejected. Window AF-4 (DateDiffSecond ±5) preserved.
        // Wired & asserted in 401-03.
        return Task.CompletedTask;
    }
}
