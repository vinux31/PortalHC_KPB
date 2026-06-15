// Phase 367 Plan 01 Task 1 — unit traversal BFS lintas tabel + cycle guard.
// InMemory cukup: traversal hanya query Renews*Id (tak butuh FK enforcement). Pola MakeService + null-substitute
// logger (NullLogger). 4 [Fact] sesuai <behavior>: multi-level, cycle, no-children, training-root arah benar.
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

public class RecordCascadeServiceTests
{
    private static RecordCascadeDeleteService MakeService(out ApplicationDbContext ctx)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        ctx = new ApplicationDbContext(options);
        // null-substitute: traversal/preview tak deref _protonCompletion/_auditLog/_env.
        return new RecordCascadeDeleteService(ctx, NullLogger<RecordCascadeDeleteService>.Instance, null!, null!, null!);
    }

    private static AssessmentSession Session(int id, int? renewsSession = null, int? renewsTraining = null) =>
        new AssessmentSession
        {
            Id = id, UserId = "u1", Title = "S" + id, Category = "X", Schedule = new DateTime(2026, 1, 1),
            Status = "Open", AccessToken = "", RenewsSessionId = renewsSession, RenewsTrainingId = renewsTraining
        };

    private static TrainingRecord Training(int id, int? renewsSession = null, int? renewsTraining = null) =>
        new TrainingRecord
        {
            Id = id, UserId = "u1", Judul = "T" + id, Tanggal = new DateTime(2026, 1, 1),
            Status = "Valid", RenewsSessionId = renewsSession, RenewsTrainingId = renewsTraining
        };

    // induk session(1) -> anak TR(2 via RenewsSessionId=1) -> cucu session(3 via RenewsTrainingId=2)
    [Fact]
    public async Task Traversal_MultiLevel_ReturnsAllNodesOnce_RootFirst()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(Session(1));
        ctx.TrainingRecords.Add(Training(2, renewsSession: 1));
        ctx.AssessmentSessions.Add(Session(3, renewsTraining: 2));
        await ctx.SaveChangesAsync();

        var result = await svc.CollectCascadeIds("session", 1);

        Assert.Equal(3, result.Count);
        Assert.Equal(("session", 1), result[0]); // root dulu
        Assert.Contains(("training", 2), result);
        Assert.Contains(("session", 3), result);
    }

    // A(session 1) <-> B(training 2) saling renew → cycle guard berhenti, tiap node sekali.
    [Fact]
    public async Task Traversal_Cycle_StopsViaVisitedGuard()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(Session(1, renewsTraining: 2));
        ctx.TrainingRecords.Add(Training(2, renewsSession: 1));
        await ctx.SaveChangesAsync();

        var result = await svc.CollectCascadeIds("session", 1);

        Assert.Equal(2, result.Count);
        Assert.Single(result, n => n == ("session", 1));
        Assert.Single(result, n => n == ("training", 2));
    }

    [Fact]
    public async Task Traversal_NoChildren_ReturnsRootOnly()
    {
        var svc = MakeService(out var ctx);
        ctx.AssessmentSessions.Add(Session(1));
        await ctx.SaveChangesAsync();

        var result = await svc.CollectCascadeIds("session", 1);

        Assert.Single(result);
        Assert.Equal(("session", 1), result[0]);
    }

    // root training(1): anak via RenewsTrainingId (BUKAN RenewsSessionId — Pitfall 2).
    [Fact]
    public async Task Traversal_TrainingRoot_FindsChildrenViaRenewsTrainingId_NotSessionId()
    {
        var svc = MakeService(out var ctx);
        ctx.TrainingRecords.Add(Training(1));
        ctx.AssessmentSessions.Add(Session(2, renewsTraining: 1)); // anak benar
        ctx.TrainingRecords.Add(Training(3, renewsTraining: 1));   // anak benar
        ctx.AssessmentSessions.Add(Session(4, renewsSession: 1));  // arah SALAH — tak boleh ikut
        await ctx.SaveChangesAsync();

        var result = await svc.CollectCascadeIds("training", 1);

        Assert.Equal(3, result.Count);
        Assert.Contains(("training", 1), result);
        Assert.Contains(("session", 2), result);
        Assert.Contains(("training", 3), result);
        Assert.DoesNotContain(("session", 4), result);
    }

    [Fact]
    public async Task Traversal_InvalidRootType_Throws()
    {
        var svc = MakeService(out _);
        await Assert.ThrowsAsync<ArgumentException>(() => svc.CollectCascadeIds("bogus", 1));
    }
}
