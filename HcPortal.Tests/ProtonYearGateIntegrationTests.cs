using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 359 (PCOMP-07/D-03) — integration test jembatan DB IsPrevYearPassedAsync.
/// Reuse ProtonCompletionFixture (disposable HcPortalDB_Test_&lt;guid&gt;, real SQL Server) — bukan HcPortalDB_Dev,
/// jadi tidak perlu SEED_WORKFLOW snapshot. [Trait("Category","Integration")] → skip di CI SQL-less.
/// </summary>
[Trait("Category", "Integration")]
public class ProtonYearGateIntegrationTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ProtonYearGateIntegrationTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static ProtonCompletionService NewSvc(ApplicationDbContext ctx)
        => new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance);

    private async Task<int> TrackIdAsync(ApplicationDbContext ctx, string trackType, string tahunKe)
        => (await ctx.ProtonTracks.FirstAsync(t => t.TrackType == trackType && t.TahunKe == tahunKe)).Id;

    [Fact]
    public async Task IsPrevYearPassed_PenandaAda_True_dan_TahunTakLulus_False()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"yrgate-{Guid.NewGuid():N}";
        var trackId = await TrackIdAsync(ctx, "Operator", "Tahun 1");

        // Seed assignment Tahun 1 + penanda (lulus Tahun 1).
        var asg = new ProtonTrackAssignment { CoacheeId = coachee, AssignedById = "hc", ProtonTrackId = trackId, IsActive = true };
        ctx.ProtonTrackAssignments.Add(asg);
        await ctx.SaveChangesAsync();
        ctx.ProtonFinalAssessments.Add(new ProtonFinalAssessment
        {
            CoacheeId = coachee,
            CreatedById = "hc",
            ProtonTrackAssignmentId = asg.Id,
            Status = "Completed",
            Origin = "Exam",
            CompletedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var svc = NewSvc(ctx);

        // Tahun 1 sudah ber-penanda → prereq "Tahun 1" terpenuhi.
        Assert.True(await svc.IsPrevYearPassedAsync(coachee, "Operator", "Tahun 1"));
        // Tahun 2 belum ber-penanda → prereq "Tahun 2" gagal.
        Assert.False(await svc.IsPrevYearPassedAsync(coachee, "Operator", "Tahun 2"));
        // prevTahunKe null (Tahun 1, tanpa prasyarat) → selalu true.
        Assert.True(await svc.IsPrevYearPassedAsync(coachee, "Operator", null));
    }

    // ===== Phase 360 plan 08 — exempt gate Origin="Bypass" (D-06a/A-M4 + D-05) =====
    // Gate CreateAssessment embedded di controller (AssessmentAdminController:1368-1397) →
    // test tingkat predikat: replikasi PERSIS kombinasi (a) cross-year + exempt dan (b) gate 100%.

    /// <summary>Predikat exempt PERSIS seperti gate (a) AssessmentAdminController:1372-1379.</summary>
    private static async Task<bool> SkippedByCrossYearGateAsync(
        ApplicationDbContext ctx, ProtonCompletionService svc,
        string uid, int targetTrackId, string trackType, string? prevTahunKe, bool isRenewal = false)
    {
        bool isBypassAssignment = await ctx.ProtonTrackAssignments
            .AnyAsync(a => a.CoacheeId == uid && a.ProtonTrackId == targetTrackId
                        && a.IsActive && a.Origin == "Bypass");
        return !isRenewal && !isBypassAssignment
            && !await svc.IsPrevYearPassedAsync(uid, trackType, prevTahunKe);
    }

    [Fact]
    public async Task Exempt_BypassOrigin_LolosCrossYear()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"exempt-{Guid.NewGuid():N}";
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");

        // Assignment Tahun 2 Origin="Bypass" aktif, TANPA penanda Tahun 1 (normalnya keblok cross-year).
        ctx.ProtonTrackAssignments.Add(new ProtonTrackAssignment
        { CoacheeId = coachee, AssignedById = "hc", ProtonTrackId = t2, IsActive = true, Origin = "Bypass" });
        await ctx.SaveChangesAsync();

        var svc = NewSvc(ctx);
        // Sanity: prereq Tahun 1 memang TIDAK terpenuhi (exempt-lah yang meloloskan, bukan penanda).
        Assert.False(await svc.IsPrevYearPassedAsync(coachee, "Operator", "Tahun 1"));
        // Gate (a): TIDAK di-skip — Origin="Bypass" exempt cross-year.
        Assert.False(await SkippedByCrossYearGateAsync(ctx, svc, coachee, t2, "Operator", "Tahun 1"));
    }

    [Fact]
    public async Task Exempt_BypassOrigin_GateSeratusPersenTetap()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"d05-{Guid.NewGuid():N}";
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var unit = $"U-D05-{coachee[..6]}";

        // Assignment Tahun 2 Origin="Bypass" + deliverable Tahun 2 unit BELUM 100% Approved.
        var asg = new ProtonTrackAssignment
        { CoacheeId = coachee, AssignedById = "hc", ProtonTrackId = t2, IsActive = true, Origin = "Bypass" };
        ctx.ProtonTrackAssignments.Add(asg);
        var komp = new ProtonKompetensi { Bagian = "Bagian-T", Unit = unit, NamaKompetensi = $"K-{Guid.NewGuid():N}", Urutan = 1, ProtonTrackId = t2 };
        ctx.ProtonKompetensiList.Add(komp);
        await ctx.SaveChangesAsync();
        var sub = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = "Sub", Urutan = 1 };
        ctx.ProtonSubKompetensiList.Add(sub);
        await ctx.SaveChangesAsync();
        var d1 = new ProtonDeliverable { ProtonSubKompetensiId = sub.Id, NamaDeliverable = "D1", Urutan = 1 };
        var d2 = new ProtonDeliverable { ProtonSubKompetensiId = sub.Id, NamaDeliverable = "D2", Urutan = 2 };
        ctx.ProtonDeliverableList.AddRange(d1, d2);
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping
        { CoacheeId = coachee, CoachId = "coach-d05", AssignmentUnit = unit, IsActive = true, StartDate = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        // 1 Approved + 1 Pending → BUKAN 100%.
        ctx.ProtonDeliverableProgresses.AddRange(
            new ProtonDeliverableProgress { CoacheeId = coachee, ProtonDeliverableId = d1.Id, ProtonTrackAssignmentId = asg.Id, Status = "Approved", CreatedAt = DateTime.UtcNow },
            new ProtonDeliverableProgress { CoacheeId = coachee, ProtonDeliverableId = d2.Id, ProtonTrackAssignmentId = asg.Id, Status = "Pending", CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var svc = NewSvc(ctx);
        // Gate (a): lolos cross-year (exempt Bypass).
        Assert.False(await SkippedByCrossYearGateAsync(ctx, svc, coachee, t2, "Operator", "Tahun 1"));

        // Gate (b) D-05: gate 100% target-year TETAP — replikasi resolve unit + IsEligiblePerUnit (:1383-1396).
        var resolvedUnit = await ctx.CoachCoacheeMappings
            .Where(m => m.CoacheeId == coachee && m.IsActive).Select(m => m.AssignmentUnit).FirstOrDefaultAsync();
        Assert.Equal(unit, resolvedUnit);
        var unitDeliverableIds = await ctx.ProtonDeliverableList
            .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == t2
                     && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == resolvedUnit!.Trim())
            .Select(d => d.Id).ToListAsync();
        var myStatuses = await ctx.ProtonDeliverableProgresses
            .Where(p => p.CoacheeId == coachee && unitDeliverableIds.Contains(p.ProtonDeliverableId))
            .Select(p => p.Status).ToListAsync();
        // Belum 100% Approved → TETAP di-skip gateSkippedNotHundred (exempt CUMA cross-year, BUKAN 100%).
        Assert.False(HcPortal.Helpers.CoacheeEligibilityCalculator.IsEligiblePerUnit(myStatuses, unitDeliverableIds.Count));
    }

    [Fact]
    public async Task NoBypass_NormalAssignment_KeblokCrossYear()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"normal-{Guid.NewGuid():N}";
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");

        // Regresi Phase 359: assignment normal (Origin=null) Tahun 2 tanpa penanda Tahun 1 → keblok.
        ctx.ProtonTrackAssignments.Add(new ProtonTrackAssignment
        { CoacheeId = coachee, AssignedById = "hc", ProtonTrackId = t2, IsActive = true, Origin = null });
        await ctx.SaveChangesAsync();

        var svc = NewSvc(ctx);
        Assert.True(await SkippedByCrossYearGateAsync(ctx, svc, coachee, t2, "Operator", "Tahun 1"));
    }
}
