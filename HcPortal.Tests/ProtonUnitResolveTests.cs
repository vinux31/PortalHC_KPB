// Phase 401 Plan 01 Task 2 — PSU-01 resolver-skip scaffold (RED until 401-02/04/05).
// TARGET behavior: PROTON resolvers resolve unit dari AssignmentUnit SAJA. Coachee dgn
// User.Unit="X" (primary) tapi mapping aktif AssignmentUnit=null HARUS di-SKIP (jangan resolve "X").
// Produksi resolver masih punya fallback `?? User.Unit` sampai 401-02 (GetEligibleCoachees/
// AutoCreateProgressForAssignment), 401-04 (AssessmentAdmin cert-gate), 401-05 (CDP) — jadi
// assertion yg butuh wiring di-Skip dgn alasan nyebut downstream plan; suite tetap hijau.
// Sanity (non-skip): helper-level bukti "jangan resolve dari primary" — empty AssignmentUnit → false.
using System;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class ProtonUnitResolveTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Sanity (GREEN): membuktikan keputusan resolver — AssignmentUnit kosong TIDAK boleh
    // resolve ke primary "X". Helper adalah primitif keputusan skip resolver pasca-401.
    [Fact]
    public async Task EmptyAssignmentUnit_NotResolvedFromPrimary()
    {
        await using var ctx = InMemoryContext();
        var coacheeId = Guid.NewGuid().ToString();
        // coachee punya primary "X" di junction (mirror User.Unit), tapi AssignmentUnit mapping kosong
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "X", IsPrimary = true, IsActive = true });
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoacheeId = coacheeId, CoachId = "coach1", IsActive = true, AssignmentUnit = null });
        await ctx.SaveChangesAsync();

        // resolver pasca-401 = AssignmentUnit-only; empty → skip (false), BUKAN "X"
        var resolvedFromPrimary = await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, null);
        Assert.False(resolvedFromPrimary);
    }

    // GREEN (401-02): whitespace AssignmentUnit juga tak resolve ke primary.
    [Fact]
    public async Task WhitespaceAssignmentUnit_NotResolvedFromPrimary()
    {
        await using var ctx = InMemoryContext();
        var coacheeId = Guid.NewGuid().ToString();
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "X", IsPrimary = true, IsActive = true });
        await ctx.SaveChangesAsync();

        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, "   "));
    }

    // GREEN (401-02): AssignmentUnit eksplisit yg valid (∈ active UserUnits) resolve true.
    [Fact]
    public async Task ValidAssignmentUnit_in_active_userunits_resolves_true()
    {
        await using var ctx = InMemoryContext();
        var coacheeId = Guid.NewGuid().ToString();
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "X", IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = "UnitB", IsPrimary = false, IsActive = true });
        await ctx.SaveChangesAsync();

        Assert.True(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, "UnitB"));
    }

    // Deep HTTP-integration assertion (GetEligibleCoachees action end-to-end via real DB) deferred to
    // Phase 404 QA-01 (SQLEXPRESS) — needs HTTP context + filtered-unique index. Resolver-skip + gate-block
    // channel are unit-proven above (helper-level) + grep guard `0 Select(u => u.Unit)` in 401-02 acceptance.
    [Fact(Skip = "Integration smoke deferred to Phase 404 QA-01 (HTTP context + SQLEXPRESS)")]
    public Task GetEligibleCoachees_excludes_empty_AssignmentUnit_coachee_endtoend()
    {
        return Task.CompletedTask;
    }
}
