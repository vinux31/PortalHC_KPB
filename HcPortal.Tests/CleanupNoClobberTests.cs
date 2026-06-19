// Phase 401 Plan 01 Task 2 — PSU-04 cleanup no-clobber scaffold (RED until 401-03).
// TARGET behavior: CleanupCoachCoacheeMappingOrg (CoachMappingController:887-934) HARUS
// preserve AssignmentUnit non-primary yg VALID (∈ coachee active UserUnits) — JANGAN clobber
// ke primary (clobber site :921-922 `m.AssignmentUnit = userUnit`). Wiring di 401-03.
// Sanity (non-skip): helper membuktikan unit sekunder valid (∈ active UserUnits) → true,
// yakni precondition gate "preserve" terpenuhi.
using System;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class CleanupNoClobberTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Sanity (GREEN): unit sekunder (non-primary) yg AKTIF di junction = valid → helper true.
    // Membuktikan kondisi gate "AssignmentUnit ∈ UserUnits ⇒ preserve" bisa terpenuhi.
    [Fact]
    public async Task Secondary_unit_in_active_userunits_is_valid_preserve_precondition()
    {
        await using var ctx = InMemoryContext();
        var coacheeId = Guid.NewGuid().ToString();
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "UnitPrimary", IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "UnitSekunder", IsPrimary = false, IsActive = true });
        await ctx.SaveChangesAsync();

        var valid = await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, "UnitSekunder");

        Assert.True(valid); // sekunder valid → cleanup harus preserve, bukan clobber ke primary
    }

    // 401-03 (PSU-04): preserve-gate primitive — unit sekunder valid (∈ active UserUnits) → helper
    // true ⇒ cleanup PRESERVE (jangan clobber ke primary, :899-900). Unit dilepas ∉ → false ⇒
    // cleanup boleh last-resort fix. Controller mutation butuh HTTP-context; gate diuji di level primitif.
    [Fact]
    public async Task Cleanup_preserves_valid_secondary_AssignmentUnit_not_clobbered_to_primary()
    {
        await using var ctx = InMemoryContext();
        var coacheeId = Guid.NewGuid().ToString();
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "UnitX", IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "UnitY", IsPrimary = false, IsActive = true });
        await ctx.SaveChangesAsync();

        // mapping AssignmentUnit="UnitY" (sekunder valid) → gate TRUE ⇒ cleanup PRESERVE, tak clobber
        Assert.True(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, "UnitY"));
        // mapping AssignmentUnit="UnitGone" ∉ active UserUnits → gate FALSE ⇒ cleanup boleh perbaiki
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, "UnitGone"));
    }
}
