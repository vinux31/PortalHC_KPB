// RetakeServiceTests — v32.4 Phase 405 Plan 03 (RTK-07/13).
// Integration real-SQL (disposable DB HcPortalDB_Test_{guid} @ localhost\SQLEXPRESS, MigrateAsync full chain
// incl AddRetakeColumnsAndArchive). Pola mirror MultiUnitSqlFixture/RecordCascadeFixture: drop on dispose +
// mid-migration failure catch melempar XunitException ("MIGRATION-CHAIN break") supaya bedakan chain-break
// dari bug retake. [Trait("Category","Integration")] → SQL-less CI skip via --filter "Category!=Integration".
//
// Membuktikan 3 koreksi RetakeService vs ResetAssessment existing:
//   1. CLAIM-ATOMIK DULU (anti double-archive): Claim_DoubleExecute_SecondAborts.
//   2. D-01 snapshot-presence: legacy AttemptHistory tanpa child TIDAK konsumsi cap
//      (CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap) DAN era-retake archive (with snapshot)
//      DOES count (CanRetake_RetakeEraArchiveWithSnapshot_ConsumesCap).
//   3. Counting (UserId,Title,Category) anti-konflasi Pre/Post: Counting_PrePostSameTitle_NoConflate.
//   + Snapshot ditulis SEBELUM responses dihapus: Snapshot_WrittenBeforeResponsesDeleted.
//
// Hub dependency: project test TIDAK punya Moq/NSubstitute → pakai NoOpHubContext (hand-stub) yang
// SendAsync no-op (tak ada SignalR backplane di test). Logger = NullLogger. AuditLogService real atas test DbContext.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Hubs;
using HcPortal.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

// ---------- Disposable-DB fixture (MigrateAsync full chain) ----------
public class RetakeServiceFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;

    public RetakeServiceFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(Options);
            await ctx.Database.MigrateAsync();   // FULL chain incl AddRetakeColumnsAndArchive (plan 405-01).
        }
        catch (Exception ex)
        {
            try { await using var c = new ApplicationDbContext(Options); await c.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"Phase 405-03 RetakeService fixture setup gagal saat MigrateAsync DB {DbName}. " +
                $"Indikasi MIGRATION-CHAIN break (incl AddRetakeColumnsAndArchive), BUKAN tentu bug retake. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

// ---------- No-op IHubContext<AssessmentHub> (tak ada Moq di project test) ----------
internal sealed class NoOpHubContext : IHubContext<AssessmentHub>
{
    public IHubClients Clients { get; } = new NoOpHubClients();
    public IGroupManager Groups { get; } = new NoOpGroupManager();

    private sealed class NoOpHubClients : IHubClients
    {
        private static readonly IClientProxy _proxy = new NoOpClientProxy();
        public IClientProxy All => _proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Client(string connectionId) => _proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
        public IClientProxy Group(string groupName) => _proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
        public IClientProxy User(string userId) => _proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
    }

    private sealed class NoOpClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

[Trait("Category", "Integration")]
public class RetakeServiceTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public RetakeServiceTests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static RetakeService NewService(ApplicationDbContext ctx) =>
        new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);

    // ---------- Seed helpers ----------
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "rtk-" + Guid.NewGuid().ToString("N")[..8], Email = "rtk@test.local", FullName = "Retake Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string category,
        string status = "Completed", bool? isPassed = false, bool allowRetake = true,
        int maxAttempts = 2, int cooldownHours = 0, DateTime? completedAt = null,
        string? assessmentType = null, bool isManualEntry = false)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = category, Status = status,
            AccessToken = "", Schedule = new DateTime(2026, 2, 1),
            IsPassed = isPassed, AllowRetake = allowRetake, MaxAttempts = maxAttempts,
            RetakeCooldownHours = cooldownHours, CompletedAt = completedAt,
            AssessmentType = assessmentType, IsManualEntry = isManualEntry,
            Score = 50, Progress = 100
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    /// <summary>Seed 1 package + N MC soal (1 correct option each) untuk session + assignment + responses.</summary>
    private static async Task<List<int>> SeedPackageWithResponsesAsync(ApplicationDbContext ctx, int sessionId, int nQuestions)
    {
        var pkg = new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        var qIds = new List<int>();
        for (int i = 0; i < nQuestions; i++)
        {
            var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionText = $"Soal {i + 1}", Order = i, ScoreValue = 10, QuestionType = "MultipleChoice" };
            ctx.PackageQuestions.Add(q);
            await ctx.SaveChangesAsync();
            var correct = new PackageOption { PackageQuestionId = q.Id, OptionText = "Benar", IsCorrect = true };
            var wrong = new PackageOption { PackageQuestionId = q.Id, OptionText = "Salah", IsCorrect = false };
            ctx.PackageOptions.AddRange(correct, wrong);
            await ctx.SaveChangesAsync();
            // Worker memilih opsi benar (response).
            ctx.PackageUserResponses.Add(new PackageUserResponse { AssessmentSessionId = sessionId, PackageQuestionId = q.Id, PackageOptionId = correct.Id });
            qIds.Add(q.Id);
        }
        await ctx.SaveChangesAsync();

        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = sessionId,
            AssessmentPackageId = pkg.Id,
            UserId = "",   // not needed for retake path
            ShuffledQuestionIds = "[" + string.Join(",", qIds) + "]"
        });
        await ctx.SaveChangesAsync();
        return qIds;
    }

    /// <summary>Seed 1 era-retake AttemptHistory (DENGAN ≥1 child archive) → konsumsi cap (D-01).</summary>
    private static async Task SeedEraRetakeArchiveAsync(ApplicationDbContext ctx, string userId, string title, string category)
    {
        var hist = new AssessmentAttemptHistory { SessionId = 0, UserId = userId, Title = title, Category = category, AttemptNumber = 1 };
        ctx.AssessmentAttemptHistory.Add(hist);
        await ctx.SaveChangesAsync();
        ctx.AssessmentAttemptResponseArchives.Add(new AssessmentAttemptResponseArchive
        {
            AttemptHistoryId = hist.Id, PackageQuestionId = 1, QuestionText = "Q", AnswerText = "A", IsCorrect = true, AwardedScore = 10
        });
        await ctx.SaveChangesAsync();
    }

    /// <summary>Seed 1 legacy AttemptHistory (TANPA child archive) → TIDAK konsumsi cap (D-01).</summary>
    private static async Task SeedLegacyArchiveAsync(ApplicationDbContext ctx, string userId, string title, string category)
    {
        ctx.AssessmentAttemptHistory.Add(new AssessmentAttemptHistory { SessionId = 0, UserId = userId, Title = title, Category = category, AttemptNumber = 1 });
        await ctx.SaveChangesAsync();
    }

    // ====================== Test 1: claim-atomik anti double-archive ======================
    // WR-01 fix (review 405): reset sesi yang SUDAH Open kini = SUCCESS no-op (bukan error "sudah terbuka"),
    // selaras controller yang mengizinkan reset status Open. Invariant LOAD-BEARING tetap: execute ke-2 TIDAK
    // membuat AttemptHistory kedua (anti double-archive, histCount==1). Sebelumnya test ini meng-encode
    // perilaku lama (Open di-exclude dari claim → r2.Success==false) yang justru regresi WR-01.
    [Fact]
    public async Task Claim_DoubleExecute_NoSecondArchive()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId, "ClaimTitle", "Test");
        await SeedPackageWithResponsesAsync(ctx, sessionId, 3);
        var svc = NewService(ctx);

        // Execute ke-1 → success (sesi Completed → Open + archive).
        var r1 = await svc.ExecuteAsync(sessionId, userId, "Tester", "ResetAssessment", "test_first");
        Assert.True(r1.Success);
        Assert.Null(r1.Error);

        // Execute ke-2 (sesi kini Open) → SUCCESS no-op (WR-01): tak ada archive kedua dibuat.
        var r2 = await svc.ExecuteAsync(sessionId, userId, "Tester", "ResetAssessment", "test_second");
        Assert.True(r2.Success);
        Assert.Null(r2.Error);

        // Hanya 1 AttemptHistory ter-create untuk (UserId,Title,Category) → tidak double-archive.
        await using var verify = NewCtx();
        int histCount = await verify.AssessmentAttemptHistory
            .CountAsync(h => h.UserId == userId && h.Title == "ClaimTitle" && h.Category == "Test");
        Assert.Equal(1, histCount);
    }

    // ====================== Test 1b: WR-01 — reset sesi Open = SUCCESS no-op ======================
    // HC klik Reset pada sesi yang legitim ber-status Open (assigned-not-started). Service kini balas SUCCESS
    // tanpa error "sudah terbuka", TANPA membuat AttemptHistory (WR-03: tak ada childless orphan).
    [Fact]
    public async Task Execute_OpenSession_SuccessNoArchive()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId, "OpenTitle", "Test", status: "Open");
        var svc = NewService(ctx);

        var r = await svc.ExecuteAsync(sessionId, userId, "Tester", "ResetAssessment", "hc_reset_open");
        Assert.True(r.Success);
        Assert.Null(r.Error);

        // Tidak ada AttemptHistory childless yang terbuat untuk sesi Open.
        await using var verify = NewCtx();
        int histCount = await verify.AssessmentAttemptHistory
            .CountAsync(h => h.UserId == userId && h.Title == "OpenTitle" && h.Category == "Test");
        Assert.Equal(0, histCount);
    }

    // ====================== Test 1c: WR-03 — Completed tanpa assignment TIDAK buat orphan ======================
    // Sesi Completed yang assignment-nya sudah hilang (questions kosong) → tak ada snapshot → AttemptHistory
    // TIDAK di-insert (deferred-insert). Sebelumnya menghasilkan baris AttemptHistory childless (orphan).
    [Fact]
    public async Task Execute_CompletedNoAssignment_NoChildlessHistory()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // Completed tetapi TANPA package/assignment/responses (assignment null → questionIds kosong).
        var sessionId = await SeedSessionAsync(ctx, userId, "OrphanTitle", "Test", status: "Completed");
        var svc = NewService(ctx);

        var r = await svc.ExecuteAsync(sessionId, userId, "Tester", "ResetAssessment", "hc_reset_orphan");
        Assert.True(r.Success);

        // Tak ada AttemptHistory (childless) yang persist — WR-03.
        await using var verify = NewCtx();
        int histCount = await verify.AssessmentAttemptHistory
            .CountAsync(h => h.UserId == userId && h.Title == "OrphanTitle" && h.Category == "Test");
        Assert.Equal(0, histCount);
        // Sesi tetap ter-reset ke Open (claim sukses, commit).
        var status = await verify.AssessmentSessions.Where(a => a.Id == sessionId).Select(a => a.Status).SingleAsync();
        Assert.Equal("Open", status);
    }

    // ====================== Test 2: snapshot ditulis SEBELUM responses dihapus ======================
    [Fact]
    public async Task Snapshot_WrittenBeforeResponsesDeleted()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId, "SnapTitle", "Test");
        const int N = 4;
        await SeedPackageWithResponsesAsync(ctx, sessionId, N);
        var svc = NewService(ctx);

        var r = await svc.ExecuteAsync(sessionId, userId, "Tester", "ResetAssessment", "snap");
        Assert.True(r.Success);

        await using var verify = NewCtx();
        var hist = await verify.AssessmentAttemptHistory
            .SingleAsync(h => h.UserId == userId && h.Title == "SnapTitle" && h.Category == "Test");
        int snapshotCount = await verify.AssessmentAttemptResponseArchives.CountAsync(a => a.AttemptHistoryId == hist.Id);
        int liveResponses = await verify.PackageUserResponses.CountAsync(r2 => r2.AssessmentSessionId == sessionId);

        Assert.Equal(N, snapshotCount);     // snapshot ter-tulis (N soal)
        Assert.Equal(0, liveResponses);     // responses live dihapus
        // Verdict beku benar (semua MC pilih opsi benar → IsCorrect true).
        Assert.True(await verify.AssessmentAttemptResponseArchives.AllAsync(a => a.AttemptHistoryId != hist.Id || a.IsCorrect == true));
    }

    // ====================== Test 3: D-01 legacy archive TIDAK konsumsi cap ======================
    [Fact]
    public async Task CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // Session failed, AllowRetake, MaxAttempts=2, cooldown 0 (no jeda), Completed in past.
        var sessionId = await SeedSessionAsync(ctx, userId, "LegacyTitle", "Test",
            status: "Completed", isPassed: false, allowRetake: true, maxAttempts: 2,
            cooldownHours: 0, completedAt: DateTime.UtcNow.AddDays(-2));
        // 1 legacy AttemptHistory TANPA child snapshot.
        await SeedLegacyArchiveAsync(ctx, userId, "LegacyTitle", "Test");
        var svc = NewService(ctx);

        // eraRetakeArchives==0 → attemptsUsed==1 < 2 → CanRetake true.
        Assert.True(await svc.CanRetakeAsync(sessionId));
    }

    // ====================== Test 4: D-01 era-retake archive (with snapshot) konsumsi cap ======================
    [Fact]
    public async Task CanRetake_RetakeEraArchiveWithSnapshot_ConsumesCap()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId, "EraTitle", "Test",
            status: "Completed", isPassed: false, allowRetake: true, maxAttempts: 2,
            cooldownHours: 0, completedAt: DateTime.UtcNow.AddDays(-2));
        // 1 era-retake AttemptHistory DENGAN child snapshot.
        await SeedEraRetakeArchiveAsync(ctx, userId, "EraTitle", "Test");
        var svc = NewService(ctx);

        // eraRetakeArchives==1 → attemptsUsed==2 == MaxAttempts → CanRetake false.
        Assert.False(await svc.CanRetakeAsync(sessionId));
    }

    // ====================== Test 5: counting (UserId,Title,Category) no-conflate Pre/Post ======================
    [Fact]
    public async Task Counting_PrePostSameTitle_NoConflate()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // Title sama "X", Category beda "Pre"/"Post". Post graded-retakeable (assessmentType PostTest tak diblok).
        var preSessionId = await SeedSessionAsync(ctx, userId, "X", "Pre",
            status: "Completed", isPassed: false, allowRetake: true, maxAttempts: 2,
            cooldownHours: 0, completedAt: DateTime.UtcNow.AddDays(-2), assessmentType: "PostTest");
        var postSessionId = await SeedSessionAsync(ctx, userId, "X", "Post",
            status: "Completed", isPassed: false, allowRetake: true, maxAttempts: 2,
            cooldownHours: 0, completedAt: DateTime.UtcNow.AddDays(-2), assessmentType: "PostTest");

        // 1 era-retake archive HANYA di Category="Pre".
        await SeedEraRetakeArchiveAsync(ctx, userId, "X", "Pre");
        var svc = NewService(ctx);

        // Pre punya 1 era-retake archive → attemptsUsed==2 == cap → CanRetake false.
        Assert.False(await svc.CanRetakeAsync(preSessionId));
        // Post TIDAK terpengaruh (counting (UserId,Title,Category) memisahkan) → eraRetakeArchives==0 →
        // attemptsUsed==1 < 2 → CanRetake true.
        Assert.True(await svc.CanRetakeAsync(postSessionId));
    }
}
