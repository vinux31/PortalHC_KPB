// Phase 404 Plan 02 (QA-03) — single-active invariants on real SQL Server.
//
// Assert-strategy split (the load-bearing decision, Pitfall #1):
//   - CoachCoacheeMapping single-active = DB-enforced (filtered-unique IX_CoachCoacheeMappings_CoacheeId_ActiveUnique,
//     ApplicationDbContext.cs:333-336) → Assert.ThrowsAsync<DbUpdateException>. ONE mapping Fact represents
//     Assign/Edit/Import/Reactivate because all write through the SAME index (R-2).
//   - ProtonTrackAssignment single-active = APP-level only (NON-unique (CoacheeId,IsActive) index :393) →
//     drive the REAL bypass T1@X→T2@Y via ProtonBypassService, then COUNT active == 1. NEVER DbUpdateException.
//
// R-1 boundary: NOL kode produksi. Mapping covered by direct DB constraint assert + DbContext write-pattern
// replication (reactivate); PTA covered by the existing ProtonBypassService. No controller seam extracted.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class SingleActiveInvariantSqlTests : IClassFixture<MultiUnitSqlFixture>
{
    private readonly MultiUnitSqlFixture _fixture;
    public SingleActiveInvariantSqlTests(MultiUnitSqlFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Fact A: mapping single-active is DB-enforced (covers Assign/Edit/Import/Reactivate, R-2) ----

    [Fact]
    public async Task SecondActiveMapping_SameCoachee_ViolatesFilteredUnique()
    {
        await using var ctx = NewCtx();
        var coachee = $"mapA-{Guid.NewGuid():N}";   // per-Fact unique coachee — single-active index is per-coachee.
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoachId = "co1", CoacheeId = coachee, IsActive = true, StartDate = DateTime.UtcNow, AssignmentUnit = MultiUnitSqlFixture.UnitX });
        await ctx.SaveChangesAsync();

        // 2nd active mapping for the SAME coachee (any CoachId/unit) — DB rejects via filtered-unique [IsActive]=1.
        // InMemory would NOT throw → this is the SQL-real reason. Represents Assign/Edit/Import/Reactivate (shared index).
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoachId = "co2", CoacheeId = coachee, IsActive = true, StartDate = DateTime.UtcNow, AssignmentUnit = MultiUnitSqlFixture.UnitY });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    // ---- Fact B: reactivate-without-deactivate replication (R-1 (b) — DbContext write-pattern, NOT a seam) ----

    [Fact]
    public async Task ReactivateWithoutDeactivate_SameCoachee_ViolatesFilteredUnique()
    {
        await using var ctx = NewCtx();
        var coachee = $"mapB-{Guid.NewGuid():N}";
        // 1 active + 1 inactive for the same coachee is allowed (filter only covers IsActive=1).
        var active = new CoachCoacheeMapping { CoachId = "co1", CoacheeId = coachee, IsActive = true, StartDate = DateTime.UtcNow, AssignmentUnit = MultiUnitSqlFixture.UnitX };
        var inactive = new CoachCoacheeMapping { CoachId = "co2", CoacheeId = coachee, IsActive = false, StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow.AddDays(-1), AssignmentUnit = MultiUnitSqlFixture.UnitY };
        ctx.CoachCoacheeMappings.AddRange(active, inactive);
        await ctx.SaveChangesAsync();

        // Buggy reactivate: flip the inactive row to active WITHOUT deactivating the existing active one → 2 active → reject.
        // Proves the DB index protects the reactivate / import-reactivate path too.
        inactive.IsActive = true;
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    // ---- Fact C: ProtonTrackAssignment single-active via REAL bypass T1@X→T2@Y — COUNT, not exception (Pitfall #1) ----

    [Fact]
    public async Task SequentialBypass_T1X_to_T2Y_LeavesExactlyOneActivePta_AndPreservesHistori()
    {
        await using var ctx = NewCtx();
        var coachee = $"pta-{Guid.NewGuid():N}";
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");

        // Seed source assignment at Tahun 1 + deliverables + submitted progress (mirror CL-B(a) precedent :88-100).
        var source = await SeedAssignmentAsync(ctx, coachee, t1);
        var dels = await SeedDeliverablesAsync(ctx, t1, $"U-{coachee[..8]}", 2);
        await SeedProgressAsync(ctx, coachee, source.Id, dels, "Submitted");

        // Drive the real sequential bypass T1@X→T2@Y.
        var result = await NewBypassSvc(ctx).ExecuteInstantBypassAsync(
            Req(coachee, t1, t2, "CL-B(a)", targetUnit: $"U-{coachee[..8]}"));
        Assert.True(result.Success, result.Message);

        // Single-active by COUNT (Pattern 3) — T1 deactivated, T2 active → exactly 1. NOT DbUpdateException.
        var activeCount = await ctx.ProtonTrackAssignments.CountAsync(a => a.CoacheeId == coachee && a.IsActive);
        Assert.Equal(1, activeCount);

        // Cert histori utuh (D-02/D-06): the prior track's record is NOT destroyed by the bypass —
        // both assignments co-exist (1 inactive source + 1 active target) and the source penanda survives.
        var totalAssignments = await ctx.ProtonTrackAssignments.CountAsync(a => a.CoacheeId == coachee);
        Assert.Equal(2, totalAssignments);
        var finalCount = await ctx.ProtonFinalAssessments.CountAsync(f => f.CoacheeId == coachee);
        Assert.True(finalCount >= 1, "source-track penanda (cert histori) must be preserved after bypass");
    }

    // ---- test helpers copied verbatim from ProtonBypassServiceTests.cs:29-86 (pure test, no production coupling) ----

    private static ProtonBypassService NewBypassSvc(ApplicationDbContext ctx, FakeNotificationService? notif = null)
    {
        var fake = notif ?? new FakeNotificationService();
        return new(ctx,
               new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fake, new AuditLogService(ctx)),
               fake,
               new AuditLogService(ctx),
               NullLogger<ProtonBypassService>.Instance);
    }

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

    private static async Task SeedProgressAsync(ApplicationDbContext ctx, string coacheeId, int assignmentId, IEnumerable<int> deliverableIds, string status)
    {
        foreach (var dId in deliverableIds)
        {
            ctx.ProtonDeliverableProgresses.Add(new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = dId,
                ProtonTrackAssignmentId = assignmentId,
                Status = status,
                CreatedAt = DateTime.UtcNow
            });
        }
        await ctx.SaveChangesAsync();
    }

    private static BypassRequest Req(string coachee, int sourceTrack, int targetTrack, string mode,
        string targetUnit = "U-404", string? targetCoachId = null)
        => new(coachee, sourceTrack, targetTrack, targetUnit, targetCoachId, "Alasan bypass test", mode, null, "hc-init");
}
