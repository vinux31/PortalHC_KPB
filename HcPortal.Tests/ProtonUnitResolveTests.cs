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

    // TODO 401-02: GetEligibleCoachees / AutoCreateProgressForAssignment resolve dari AssignmentUnit saja.
    [Fact(Skip = "RED until 401-02/04/05 drop the `?? User.Unit` resolver fallback")]
    public Task Resolver_skips_coachee_with_empty_AssignmentUnit_does_not_use_primary()
    {
        // Seed: coachee User.Unit="X" (primary) + active mapping AssignmentUnit=null.
        // Expect post-401: resolver returns no unit (skip) — coachee NOT resolved against "X".
        // Wired & asserted in 401-02 (GetEligibleCoachees gate + AutoCreateProgressForAssignment read-path).
        return Task.CompletedTask;
    }
}
