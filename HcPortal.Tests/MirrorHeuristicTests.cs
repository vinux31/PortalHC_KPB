// Phase 367 Plan 01 Task 2 — heuristik mirror legacy (#15) via BuildPreviewAsync + read-only invariant.
// InMemory cukup (mirror query tak butuh FK enforcement). 6 [Fact]: 5 match/non-match + 1 no-mutation.
// Toleransi ±1 hari HANYA untuk kandidat mirror preview (BEDA dari guard duplikat #12 EXACT).
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

public class MirrorHeuristicTests
{
    private static readonly DateTime Base = new DateTime(2026, 3, 10);

    private static RecordCascadeDeleteService MakeService(out ApplicationDbContext ctx)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        ctx = new ApplicationDbContext(options);
        // null-substitute: BuildPreviewAsync/FindMirrorCandidates tak deref _protonCompletion/_auditLog/_env.
        return new RecordCascadeDeleteService(ctx, NullLogger<RecordCascadeDeleteService>.Instance, null!, null!, null!);
    }

    // session root (no renewal children) — agar TR yang muncul di preview = murni kandidat mirror.
    private static AssessmentSession RootSession(int id = 1, string userId = "u1") =>
        new AssessmentSession
        {
            Id = id, UserId = userId, Title = "Welding Inspector", Category = "X",
            Schedule = Base, Status = "Open", AccessToken = ""
        };

    private static TrainingRecord Mirror(int id, string userId, string? judul, DateTime tanggal) =>
        new TrainingRecord { Id = id, UserId = userId, Judul = judul, Tanggal = tanggal, Status = "Valid" };

    private static async Task<bool> PreviewFlagsMirror(RecordCascadeDeleteService svc, int trId)
    {
        var preview = await svc.BuildPreviewAsync("session", 1);
        return preview.Any(n => n.Type == "training" && n.Id == trId && n.IsMirrorCandidate);
    }

    private static async Task<bool> PreviewContainsTraining(RecordCascadeDeleteService svc, int trId)
    {
        var preview = await svc.BuildPreviewAsync("session", 1);
        return preview.Any(n => n.Type == "training" && n.Id == trId);
    }

    [Fact]
    public async Task Mirror_SameDay_PrefixedTitle_Detected()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(RootSession());
        ctx.TrainingRecords.Add(Mirror(10, "u1", "Assessment: Welding Inspector", Base));
        await ctx.SaveChangesAsync();
        Assert.True(await PreviewFlagsMirror(svc, 10));
    }

    [Fact]
    public async Task Mirror_PlusOneDay_ExactTitle_Detected()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(RootSession());
        ctx.TrainingRecords.Add(Mirror(10, "u1", "Welding Inspector", Base.AddDays(1)));
        await ctx.SaveChangesAsync();
        Assert.True(await PreviewFlagsMirror(svc, 10));
    }

    [Fact]
    public async Task Mirror_PlusTwoDays_NotDetected()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(RootSession());
        ctx.TrainingRecords.Add(Mirror(10, "u1", "Welding Inspector", Base.AddDays(2)));
        await ctx.SaveChangesAsync();
        Assert.False(await PreviewContainsTraining(svc, 10));
    }

    [Fact]
    public async Task Mirror_DifferentTitle_NotDetected()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(RootSession());
        ctx.TrainingRecords.Add(Mirror(10, "u1", "Totally Different Course", Base));
        await ctx.SaveChangesAsync();
        Assert.False(await PreviewContainsTraining(svc, 10));
    }

    [Fact]
    public async Task Mirror_DifferentUser_NotDetected()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(RootSession());
        ctx.TrainingRecords.Add(Mirror(10, "u2", "Assessment: Welding Inspector", Base));
        await ctx.SaveChangesAsync();
        Assert.False(await PreviewContainsTraining(svc, 10));
    }

    [Fact]
    public async Task BuildPreview_DoesNotMutateDb()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(RootSession());
        ctx.TrainingRecords.Add(Mirror(10, "u1", "Assessment: Welding Inspector", Base));
        await ctx.SaveChangesAsync();

        var sBefore = await ctx.AssessmentSessions.CountAsync();
        var tBefore = await ctx.TrainingRecords.CountAsync();
        await svc.BuildPreviewAsync("session", 1);
        Assert.Equal(sBefore, await ctx.AssessmentSessions.CountAsync());
        Assert.Equal(tBefore, await ctx.TrainingRecords.CountAsync());
    }
}
