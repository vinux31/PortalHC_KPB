using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 365 — parity-lock perilaku graduate AF-3 (D-03/D-04) di real SQL.
/// Memanggil static core CoachMappingController.MarkMappingCompletedCore LANGSUNG
/// (tanpa konstruksi controller / UserManager) via ProtonCompletionFixture (real SQL disposable).
/// Real-SQL WAJIB (D-04): hanya SQL nyata meng-enforce filtered unique index
/// IX_CoachCoacheeMappings_CoacheeId_ActiveUnique (WHERE [IsActive]=1) — bukti re-assignability.
/// DB di-share antar-fact → coacheeId unik per fact.
/// </summary>
[Trait("Category", "Integration")]
public class MarkMappingCompletedTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public MarkMappingCompletedTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Seed helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Seed chain track→kompetensi→sub→2 deliverable→2 progress untuk satu TahunKe.
    /// ProtonTracks di-seed migration HasData — REUSE via FirstAsync (jangan insert: UNIQUE TrackType+TahunKe).
    /// Optional ProtonFinalAssessment 1:1 (unik per assignment — JANGAN 2 baris).
    /// </summary>
    private static async Task<(int assignmentId, int progressCount)> SeedTrackChainAsync(
        ApplicationDbContext ctx, string coacheeId, string tahunKe, string progressStatus, bool withFinalAssessment)
    {
        var track = await ctx.ProtonTracks.FirstAsync(t => t.TrackType == "Operator" && t.TahunKe == tahunKe);
        var asg = new ProtonTrackAssignment { CoacheeId = coacheeId, AssignedById = "hc", ProtonTrackId = track.Id, IsActive = true };
        ctx.ProtonTrackAssignments.Add(asg);
        await ctx.SaveChangesAsync();

        var komp = new ProtonKompetensi { Bagian = "B-PAR", Unit = $"U-{coacheeId[..8]}", NamaKompetensi = $"K-{coacheeId}", Urutan = 1, ProtonTrackId = track.Id };
        ctx.ProtonKompetensiList.Add(komp);
        await ctx.SaveChangesAsync();
        var sub = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = "Sub", Urutan = 1 };
        ctx.ProtonSubKompetensiList.Add(sub);
        await ctx.SaveChangesAsync();

        int progressCount = 0;
        foreach (var i in new[] { 1, 2 })
        {
            var d = new ProtonDeliverable { ProtonSubKompetensiId = sub.Id, NamaDeliverable = $"D{i}", Urutan = i };
            ctx.ProtonDeliverableList.Add(d);
            await ctx.SaveChangesAsync();
            ctx.ProtonDeliverableProgresses.Add(new ProtonDeliverableProgress
            {
                CoacheeId = coacheeId,
                ProtonDeliverableId = d.Id,
                ProtonTrackAssignmentId = asg.Id,
                Status = progressStatus,
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            progressCount++;
        }

        if (withFinalAssessment)
        {
            ctx.ProtonFinalAssessments.Add(new ProtonFinalAssessment
            {
                CoacheeId = coacheeId,
                CreatedById = "hc",
                ProtonTrackAssignmentId = asg.Id,
                Status = "Completed",
                CompletedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        return (asg.Id, progressCount);
    }

    private static async Task<int> SeedActiveMappingAsync(ApplicationDbContext ctx, string coacheeId)
    {
        var mapping = new CoachCoacheeMapping
        {
            CoachId = $"coach-{Guid.NewGuid():N}",
            CoacheeId = coacheeId,
            IsActive = true,
            StartDate = DateTime.UtcNow,   // WAJIB non-null
            IsCompleted = false
        };
        ctx.CoachCoacheeMappings.Add(mapping);
        await ctx.SaveChangesAsync();
        return mapping.Id;
    }

    /// <summary>Tahun-3 lulus penuh (semua progress Approved + FinalAssessment) + mapping aktif.</summary>
    private static async Task<(int mappingId, int t3AssignmentId, int progressCount)> SeedGraduateReadyAsync(
        ApplicationDbContext ctx, string coacheeId)
    {
        var (asgId, progressCount) = await SeedTrackChainAsync(ctx, coacheeId, "Tahun 3", "Approved", withFinalAssessment: true);
        var mappingId = await SeedActiveMappingAsync(ctx, coacheeId);
        return (mappingId, asgId, progressCount);
    }

    // ── Fact 1: Happy full end-state (D-07) ──────────────────────────────────

    [Fact]
    public async Task MarkMappingCompleted_Happy_FullEndState()
    {
        var coachee = $"grad-{Guid.NewGuid():N}";
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var (mappingId, t3AssignmentId, progressCount) = await SeedGraduateReadyAsync(ctx, coachee);
        int activeBefore = await ctx.ProtonTrackAssignments.CountAsync(a => a.CoacheeId == coachee && a.IsActive);

        var (ok, error, cascadeCount) = await CoachMappingController.MarkMappingCompletedCore(ctx, mappingId);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(activeBefore, cascadeCount);

        await using var verify = new ApplicationDbContext(_fixture.Options);
        var m = await verify.CoachCoacheeMappings.AsNoTracking().FirstAsync(x => x.Id == mappingId);
        Assert.True(m.IsCompleted);
        Assert.False(m.IsActive);
        Assert.NotNull(m.CompletedAt);
        Assert.NotNull(m.EndDate);

        var assignments = await verify.ProtonTrackAssignments.AsNoTracking()
            .Where(a => a.CoacheeId == coachee).ToListAsync();
        Assert.All(assignments, a => Assert.False(a.IsActive));
        Assert.All(assignments, a => Assert.NotNull(a.DeactivatedAt));

        int progressAfter = await verify.ProtonDeliverableProgresses.AsNoTracking()
            .CountAsync(p => p.ProtonTrackAssignmentId == t3AssignmentId);
        Assert.Equal(progressCount, progressAfter);   // histori utuh
    }

    // ── Fact 2: Re-assignability pasca-graduate (D-06 #2, bukti D-03) ─────────

    [Fact]
    public async Task MarkMappingCompleted_ReassignableAfterGraduate()
    {
        var coachee = $"grad-{Guid.NewGuid():N}";
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var (mappingId, _, _) = await SeedGraduateReadyAsync(ctx, coachee);

        // Bukti index bergigi: mapping aktif KEDUA untuk coachee sama SEBELUM graduate → throw (filtered unique index).
        await using (var dup = new ApplicationDbContext(_fixture.Options))
        {
            dup.CoachCoacheeMappings.Add(new CoachCoacheeMapping
            {
                CoachId = "dup-coach",
                CoacheeId = coachee,
                IsActive = true,
                StartDate = DateTime.UtcNow
            });
            await Assert.ThrowsAnyAsync<DbUpdateException>(async () => await dup.SaveChangesAsync());
        }

        // Graduate (mapping lama → IsActive=false).
        var (ok, _, _) = await CoachMappingController.MarkMappingCompletedCore(ctx, mappingId);
        Assert.True(ok);

        // Pasca-graduate: mapping aktif baru coachee sama → SUKSES (index bebas).
        await using (var reassign = new ApplicationDbContext(_fixture.Options))
        {
            var fresh = new CoachCoacheeMapping
            {
                CoachId = "coach-baru",
                CoacheeId = coachee,
                IsActive = true,
                StartDate = DateTime.UtcNow,
                IsCompleted = false
            };
            reassign.CoachCoacheeMappings.Add(fresh);
            await reassign.SaveChangesAsync();
            Assert.True(fresh.Id > 0);
        }
    }

    // ── Fact 3: Guard no-Tahun3 (D-06 #3, D-08) ──────────────────────────────

    [Fact]
    public async Task MarkMappingCompleted_Guard_NoTahun3()
    {
        var coachee = $"grad-{Guid.NewGuid():N}";
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        await SeedTrackChainAsync(ctx, coachee, "Tahun 1", "Approved", withFinalAssessment: true);   // hanya Tahun 1
        var mappingId = await SeedActiveMappingAsync(ctx, coachee);

        var (ok, error, cascadeCount) = await CoachMappingController.MarkMappingCompletedCore(ctx, mappingId);

        Assert.False(ok);
        Assert.Contains("Tahun 3", error!);
        Assert.Equal(0, cascadeCount);

        await using var verify = new ApplicationDbContext(_fixture.Options);
        var m = await verify.CoachCoacheeMappings.AsNoTracking().FirstAsync(x => x.Id == mappingId);
        Assert.True(m.IsActive);        // tak termutasi
        Assert.False(m.IsCompleted);
    }

    // ── Fact 4a: Guard Tahun3 progress belum Approved (D-06 #4, D-08) ─────────

    [Fact]
    public async Task MarkMappingCompleted_Guard_Tahun3_ProgressNotApproved()
    {
        var coachee = $"grad-{Guid.NewGuid():N}";
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        await SeedTrackChainAsync(ctx, coachee, "Tahun 3", "Submitted", withFinalAssessment: true);   // progress BUKAN Approved
        var mappingId = await SeedActiveMappingAsync(ctx, coachee);

        var (ok, error, cascadeCount) = await CoachMappingController.MarkMappingCompletedCore(ctx, mappingId);

        Assert.False(ok);
        Assert.Contains("belum lulus", error!);
        Assert.Equal(0, cascadeCount);

        await using var verify = new ApplicationDbContext(_fixture.Options);
        var m = await verify.CoachCoacheeMappings.AsNoTracking().FirstAsync(x => x.Id == mappingId);
        Assert.True(m.IsActive);
        Assert.False(m.IsCompleted);
    }

    // ── Fact 4b: Guard Tahun3 tanpa FinalAssessment (D-06 #4, D-08) ───────────

    [Fact]
    public async Task MarkMappingCompleted_Guard_Tahun3_NoFinalAssessment()
    {
        var coachee = $"grad-{Guid.NewGuid():N}";
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        await SeedTrackChainAsync(ctx, coachee, "Tahun 3", "Approved", withFinalAssessment: false);   // SKIP FinalAssessment
        var mappingId = await SeedActiveMappingAsync(ctx, coachee);

        var (ok, error, cascadeCount) = await CoachMappingController.MarkMappingCompletedCore(ctx, mappingId);

        Assert.False(ok);
        Assert.Contains("belum lulus", error!);
        Assert.Equal(0, cascadeCount);

        await using var verify = new ApplicationDbContext(_fixture.Options);
        var m = await verify.CoachCoacheeMappings.AsNoTracking().FirstAsync(x => x.Id == mappingId);
        Assert.True(m.IsActive);
        Assert.False(m.IsCompleted);
    }

    // ── Fact 5: Mapping null / not-found (D-06 #5, OQ-2) ──────────────────────

    [Fact]
    public async Task MarkMappingCompleted_MappingNotFound()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);

        var (ok, error, cascadeCount) = await CoachMappingController.MarkMappingCompletedCore(ctx, mappingId: -1);

        Assert.False(ok);
        Assert.Contains("tidak ditemukan", error!);
        Assert.Equal(0, cascadeCount);
    }

    // ── Fact 6: Histori progress utuh (D-06 #6) ──────────────────────────────

    [Fact]
    public async Task MarkMappingCompleted_ProgressHistoryIntact()
    {
        var coachee = $"grad-{Guid.NewGuid():N}";
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var (mappingId, t3AssignmentId, progressCount) = await SeedGraduateReadyAsync(ctx, coachee);

        var (ok, _, _) = await CoachMappingController.MarkMappingCompletedCore(ctx, mappingId);
        Assert.True(ok);

        await using var verify = new ApplicationDbContext(_fixture.Options);
        int progressAfter = await verify.ProtonDeliverableProgresses.AsNoTracking()
            .CountAsync(p => p.ProtonTrackAssignmentId == t3AssignmentId);
        Assert.Equal(progressCount, progressAfter);   // graduate TIDAK RemoveRange progress
    }
}
