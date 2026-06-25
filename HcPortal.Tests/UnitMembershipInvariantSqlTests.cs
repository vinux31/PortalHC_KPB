// Phase 404 Plan 03 (QA-04) — unit-membership + anti-dobel invariants on real SQL Server.
//
//   Fact A: AssignmentUnit ∈ coachee.UserUnits — drive production CoachMappingController.ValidateAssignmentUnitInUserUnits
//           (∈ → true, ∉ → false, blank → false). Represents Assign/Edit/Import/bypass-TargetUnit/reactivate (all gate here).
//   Fact B: B-06 anti-dobel cross-unit — ProtonDeliverableBootstrap.CreateProgressAsync for unit X then unit Y (same coachee);
//           unit-Y deliverables are NOT skipped (different ids) → cross-unit progress co-exists.
//   Fact C: ProtonKompetensi/deliverable 1:1 — duplicate (ProtonTrackAssignmentId, ProtonDeliverableId) → DbUpdateException.
//   Fact D: one-primary UserUnits — 2nd IsPrimary=true row for same user → DbUpdateException.
//
// R-1 boundary: NOL kode produksi. Drives EXISTING static helpers + direct DB constraint asserts. No seam extracted.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class UnitMembershipInvariantSqlTests : IClassFixture<MultiUnitSqlFixture>
{
    private readonly MultiUnitSqlFixture _fixture;
    public UnitMembershipInvariantSqlTests(MultiUnitSqlFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Fact A: AssignmentUnit ∈ coachee.UserUnits (production helper; represents all junction-write paths) ----

    [Fact]
    public async Task AssignmentUnit_InUserUnits_AcceptsMembers_RejectsNonMembers()
    {
        await using var ctx = NewCtx();
        var coachee = $"auv-{Guid.NewGuid():N}";
        ctx.Users.Add(new ApplicationUser { Id = coachee, UserName = coachee, FullName = "Coachee AUV" }); // UserUnits.UserId is an FK → AspNetUsers
        ctx.UserUnits.Add(new UserUnit { UserId = coachee, Unit = MultiUnitSqlFixture.UnitX, IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = coachee, Unit = MultiUnitSqlFixture.UnitY, IsPrimary = false, IsActive = true });
        await ctx.SaveChangesAsync();

        // Member units (primary AND secondary) accepted; non-member rejected; blank rejected (no silent primary resolve).
        Assert.True(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coachee, MultiUnitSqlFixture.UnitX));
        Assert.True(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coachee, MultiUnitSqlFixture.UnitY));
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coachee, "UnitZ-not-member"));
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coachee, "  "));
    }

    // ---- Fact B: B-06 anti-dobel cross-unit (production bootstrap; unit-Y deliverables not skipped) ----

    [Fact]
    public async Task B06_BootstrapUnitX_thenUnitY_SameCoachee_DoesNotSkipUnitY()
    {
        await using var ctx = NewCtx();
        var coachee = $"b06-{Guid.NewGuid():N}";
        var unitX = $"uX-{coachee[..8]}";
        var unitY = $"uY-{coachee[..8]}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");

        // PROTON chains: 2 deliverables per unit (ids distinct between unitX and unitY).
        await SeedDeliverablesAsync(ctx, t1, unitX, 2);
        await SeedDeliverablesAsync(ctx, t2, unitY, 2);
        var asgX = await SeedAssignmentAsync(ctx, coachee, t1);
        var asgY = await SeedAssignmentAsync(ctx, coachee, t2);

        await ProtonDeliverableBootstrap.CreateProgressAsync(ctx, asgX.Id, t1, coachee, unitX);
        await ProtonDeliverableBootstrap.CreateProgressAsync(ctx, asgY.Id, t2, coachee, unitY);

        // Unit-X progress exists (2) AND unit-Y progress exists (2) — B-06 guard did NOT skip unit-Y just because
        // unit-X progress already existed (different deliverable ids → cross-unit safe).
        Assert.Equal(2, await ctx.ProtonDeliverableProgresses.CountAsync(p => p.ProtonTrackAssignmentId == asgX.Id));
        Assert.Equal(2, await ctx.ProtonDeliverableProgresses.CountAsync(p => p.ProtonTrackAssignmentId == asgY.Id));
    }

    // ---- Fact C: ProtonKompetensi/deliverable 1:1 DB-enforced (filtered-unique :429) ----

    [Fact]
    public async Task DuplicateDeliverableProgress_SamePtaAndDeliverable_ViolatesUnique()
    {
        await using var ctx = NewCtx();
        var coachee = $"dup-{Guid.NewGuid():N}";
        var unit = $"uD-{coachee[..8]}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var delIds = await SeedDeliverablesAsync(ctx, t1, unit, 1);
        var asg = await SeedAssignmentAsync(ctx, coachee, t1);

        ctx.ProtonDeliverableProgresses.Add(new ProtonDeliverableProgress { CoacheeId = coachee, ProtonDeliverableId = delIds[0], ProtonTrackAssignmentId = asg.Id, Status = "Pending", CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        // Same (ProtonTrackAssignmentId, ProtonDeliverableId) again → unique index rejects.
        ctx.ProtonDeliverableProgresses.Add(new ProtonDeliverableProgress { CoacheeId = coachee, ProtonDeliverableId = delIds[0], ProtonTrackAssignmentId = asg.Id, Status = "Pending", CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    // ---- Fact D: one-primary UserUnits DB-enforced (filtered-unique :350-353) ----

    [Fact]
    public async Task SecondPrimaryUserUnit_SameUser_ViolatesFilteredUnique()
    {
        await using var ctx = NewCtx();
        var user = $"pri-{Guid.NewGuid():N}";
        ctx.Users.Add(new ApplicationUser { Id = user, UserName = user, FullName = "User Pri" }); // UserUnits.UserId is an FK → AspNetUsers
        ctx.UserUnits.Add(new UserUnit { UserId = user, Unit = MultiUnitSqlFixture.UnitX, IsPrimary = true, IsActive = true });
        await ctx.SaveChangesAsync();

        // 2nd IsPrimary=true row for the same user → filtered-unique [IsPrimary]=1 rejects.
        ctx.UserUnits.Add(new UserUnit { UserId = user, Unit = MultiUnitSqlFixture.UnitY, IsPrimary = true, IsActive = true });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    // ---- test helpers (copied from ProtonBypassServiceTests.cs:39-66; pure test, no production coupling) ----

    private static async Task<int> TrackIdAsync(ApplicationDbContext ctx, string trackType, string tahunKe)
        => (await ctx.ProtonTracks.FirstAsync(t => t.TrackType == trackType && t.TahunKe == tahunKe)).Id;

    private static async Task<ProtonTrackAssignment> SeedAssignmentAsync(
        ApplicationDbContext ctx, string coacheeId, int trackId, bool active = true, string? origin = null)
    {
        var a = new ProtonTrackAssignment { CoacheeId = coacheeId, AssignedById = "hc", ProtonTrackId = trackId, IsActive = active, Origin = origin };
        ctx.ProtonTrackAssignments.Add(a);
        await ctx.SaveChangesAsync();
        return a;
    }

    private static async Task<List<int>> SeedDeliverablesAsync(ApplicationDbContext ctx, int trackId, string unit, int count)
    {
        var komp = new ProtonKompetensi { Bagian = "Bagian-T", Unit = unit, NamaKompetensi = $"K-{Guid.NewGuid():N}", Urutan = 1, ProtonTrackId = trackId };
        ctx.ProtonKompetensiList.Add(komp);
        await ctx.SaveChangesAsync();
        var sub = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = "Sub", Urutan = 1 };
        ctx.ProtonSubKompetensiList.Add(sub);
        await ctx.SaveChangesAsync();
        var dels = Enumerable.Range(1, count)
            .Select(i => new ProtonDeliverable { ProtonSubKompetensiId = sub.Id, NamaDeliverable = $"D{i}", Urutan = i })
            .ToList();
        ctx.ProtonDeliverableList.AddRange(dels);
        await ctx.SaveChangesAsync();
        return dels.Select(d => d.Id).ToList();
    }
}
