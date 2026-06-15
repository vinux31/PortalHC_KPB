// Phase 382 WSE-07 (STAT-01) — GradingService menolak commit Completed-lulus pada sesi terminal.
//
// DEVIATION dari PLAN (Rule 3 blocking): plan menyebut "InMemory", tapi GradeAndCompleteAsync memakai
// ExecuteUpdateAsync yang TIDAK didukung EF Core 8 InMemory provider. Maka dipakai disposable real-SQL
// fixture (pola EssayFinalizeRecomputeFixture). [Trait Category=Integration] → skip via "Category!=Integration".
// DB lokal HcPortalDB_Dev TAK tersentuh.
//
// Test C (Grade_OnAbandonedSession_Rejected): sesi Status=Abandoned → GradeAndCompleteAsync return false
//   dan Status TETAP Abandoned (tak resurrect jadi Completed-lulus). RED sebelum Task 3 (guard L239 hanya != "Completed").
// Test D (Grade_OnCancelledSession_Rejected): sesi Status=Cancelled → idem. RED.
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

public class SubmitResurrectionFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public SubmitResurrectionFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"Phase 382 SubmitResurrection setup failed during MigrateAsync of disposable DB {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug guard. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class SubmitResurrectionTests : IClassFixture<SubmitResurrectionFixture>
{
    private readonly SubmitResurrectionFixture _fixture;
    public SubmitResurrectionTests(SubmitResurrectionFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private GradingService NewGradingService(ApplicationDbContext ctx)
    {
        var fakeNotif = new FakeNotificationService();
        var audit = new AuditLogService(ctx);
        var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
        var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
        var worker = new FakeWorkerDataService();
        return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
    }

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "res-" + Guid.NewGuid().ToString("N")[..8], Email = "res@test.local", FullName = "Resurrection Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    /// <summary>
    /// Seed sesi single-answer MC (jawaban benar 100%) ber-Status terminal tertentu. Tujuannya: andai guard
    /// gagal, GradeAndCompleteAsync akan menulis Completed + Score=100 + IsPassed=true (resurrection) — yang
    /// HARUS ditolak. Sesi non-essay (tanpa Essay) → path non-essay L238 (guard STAT-01).
    /// </summary>
    private static async Task<int> SeedTerminalSessionAsync(ApplicationDbContext ctx, string userId, string status)
    {
        var session = new AssessmentSession
        {
            UserId = userId, Title = "Terminal Exam", Category = "IHT", Status = status, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), PassPercentage = 70, GenerateCertificate = true, Score = null, IsPassed = null
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionType = "MultipleChoice", ScoreValue = 100, Order = 1, QuestionText = "Pilih A." };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();
        var optA = new PackageOption { PackageQuestionId = q.Id, OptionText = "A (benar)", IsCorrect = true };
        var optB = new PackageOption { PackageQuestionId = q.Id, OptionText = "B (salah)", IsCorrect = false };
        ctx.PackageOptions.AddRange(optA, optB);
        await ctx.SaveChangesAsync();

        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, PackageOptionId = optA.Id, SubmittedAt = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc) });
        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = session.Id, AssessmentPackageId = pkg.Id, UserId = userId,
            ShuffledQuestionIds = JsonSerializer.Serialize(new List<int> { q.Id })
        });
        await ctx.SaveChangesAsync();
        return session.Id;
    }

    // ---- Test C: sesi Abandoned → GradeAndCompleteAsync ditolak (Status tetap Abandoned) ----
    [Fact]
    public async Task Grade_OnAbandonedSession_Rejected()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedTerminalSessionAsync(ctx, userId, S.Abandoned);

        var session = await ctx.AssessmentSessions.FindAsync(sessionId);
        var svc = NewGradingService(ctx);
        var ok = await svc.GradeAndCompleteAsync(session!);

        Assert.False(ok);   // rowsAffected==0 → return false
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.Equal(S.Abandoned, s!.Status);   // TIDAK resurrect jadi Completed
        Assert.True(s.IsPassed != true);          // tidak di-set lulus
        Assert.Null(s.NomorSertifikat);           // tidak terbit sertifikat
    }

    // ---- Test D: sesi Cancelled → GradeAndCompleteAsync ditolak (Status tetap Cancelled) ----
    [Fact]
    public async Task Grade_OnCancelledSession_Rejected()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedTerminalSessionAsync(ctx, userId, S.Cancelled);

        var session = await ctx.AssessmentSessions.FindAsync(sessionId);
        var svc = NewGradingService(ctx);
        var ok = await svc.GradeAndCompleteAsync(session!);

        Assert.False(ok);
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.Equal(S.Cancelled, s!.Status);
        Assert.True(s.IsPassed != true);
        Assert.Null(s.NomorSertifikat);
    }
}
