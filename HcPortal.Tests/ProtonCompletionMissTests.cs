using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 363-03 (T4/D-08) — surface-on-miss penanda Proton.
/// Lulus exam TANPA assignment aktif → AuditLog "PROTON_PENANDA_MISS" + bell HC.
/// Jalur idempotent (penanda sudah ada) TIDAK boleh surface (Pitfall 3 false-alarm).
/// Real SQL via ProtonCompletionFixture; coacheeId unik per fact (DB shared).
/// </summary>
[Trait("Category", "Integration")]
public class ProtonCompletionMissTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public ProtonCompletionMissTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static ProtonCompletionService NewSvc(ApplicationDbContext ctx, FakeNotificationService notif)
        => new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance,
               notif, new AuditLogService(ctx));

    private static async Task<string> SeedHcUserAsync(ApplicationDbContext ctx)
    {
        var hcId = $"hc-{Guid.NewGuid():N}";
        ctx.Users.Add(new ApplicationUser { Id = hcId, UserName = hcId, FullName = $"HC {hcId[..8]}", RoleLevel = 2, IsActive = true });
        await ctx.SaveChangesAsync();
        return hcId;
    }

    [Fact]
    public async Task EnsureAsync_NoActiveAssignment_SurfacesAuditAndHCNotif()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"miss-{Guid.NewGuid():N}";
        var hcId = await SeedHcUserAsync(ctx);
        var track = await ctx.ProtonTracks.FirstAsync(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 1");

        // Skenario T4: lulus exam tapi assignment di-DEAKTIVASI admin mid-exam.
        ctx.ProtonTrackAssignments.Add(new ProtonTrackAssignment
        { CoacheeId = coachee, AssignedById = "hc", ProtonTrackId = track.Id, IsActive = false });
        await ctx.SaveChangesAsync();

        var notif = new FakeNotificationService();
        var svc = NewSvc(ctx, notif);

        var created = await svc.EnsureAsync(coachee, track.Id, "grader", "Exam", null);

        Assert.False(created); // D-07: strict IsActive — tetap TIDAK auto-penanda
        // (a) AuditLog surface
        var audit = await ctx.AuditLogs
            .Where(a => a.ActionType == "PROTON_PENANDA_MISS" && a.Description.Contains(coachee))
            .ToListAsync();
        Assert.Single(audit);
        // (b) bell HC — HANYA ke user RoleLevel==2
        Assert.Contains(notif.Sent, s => s.UserId == hcId && s.Type == "PROTON_PENANDA_MISS");
    }

    [Fact]
    public async Task EnsureAsync_PenandaAlreadyExists_DoesNotSurface()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"miss-{Guid.NewGuid():N}";
        await SeedHcUserAsync(ctx);
        var track = await ctx.ProtonTracks.FirstAsync(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 1");

        // Assignment AKTIF + penanda sudah ada → jalur idempotent (return false TANPA surface).
        var asg = new ProtonTrackAssignment { CoacheeId = coachee, AssignedById = "hc", ProtonTrackId = track.Id, IsActive = true };
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

        var notif = new FakeNotificationService();
        var svc = NewSvc(ctx, notif);

        var created = await svc.EnsureAsync(coachee, track.Id, "grader", "Exam", null);

        Assert.False(created); // idempotent
        // TIDAK ada surface: nol audit baru untuk coachee ini, nol notif terkirim (Pitfall 3).
        var audit = await ctx.AuditLogs
            .Where(a => a.ActionType == "PROTON_PENANDA_MISS" && a.Description.Contains(coachee))
            .ToListAsync();
        Assert.Empty(audit);
        Assert.DoesNotContain(notif.Sent, s => s.Type == "PROTON_PENANDA_MISS");
    }
}
