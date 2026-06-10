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
}
