// Phase 382 WSE-08 (STAT-02) — AbandonExam atomic guarded ExecuteUpdate (ownership di WHERE).
//
// DEVIATION dari PLAN (Rule 3 blocking, sama dgn Plan 01): plan menyebut opsi InMemory, tapi
// ExecuteUpdateAsync TIDAK didukung EF Core 8 InMemory provider, DAN race/atomic guard hanya bisa
// dibuktikan terhadap SQL nyata. Maka dipakai disposable real-SQL fixture (pola SubmitResurrectionFixture
// / ProtonCompletionFixture). [Trait Category=Integration] → CI SQL-less skip via "Category!=Integration".
// Run lokal butuh localhost\SQLEXPRESS (+ SQLBrowser). DB lokal HcPortalDB_Dev TAK tersentuh.
//
// Test mengeksekusi POLA UPDATE ber-guard yang SAMA dengan fix Task 3 (atomic, WHERE UserId &&
// (InProgress||Open)) langsung terhadap DbContext — membuktikan kontrak guard di SQL nyata.
//
// Test A (Completed→rowsAffected==0): sesi Status=Completed, abandon-guard → 0 baris, Status tetap Completed.
//   RED bila guard hilang (TOCTOU lama bisa overwrite). Test B (ownership): user lain → 0 baris.
//   Test C (happy): owner + InProgress → 1 baris, Status=Abandoned.
using System;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

public class AbandonGuardFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public AbandonGuardFixture()
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
                $"Phase 382 AbandonGuard setup failed during MigrateAsync of disposable DB {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug guard. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class AbandonGuardTests : IClassFixture<AbandonGuardFixture>
{
    private readonly AbandonGuardFixture _fixture;
    public AbandonGuardTests(AbandonGuardFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "ab-" + Guid.NewGuid().ToString("N")[..8], Email = "ab@test.local", FullName = "Abandon Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedSessionAsync(ApplicationDbContext ctx, string userId, string status)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = "Abandon Exam", Category = "IHT", Status = status, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), PassPercentage = 70, StartedAt = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc)
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    // Pola guarded-UPDATE yang IDENTIK dengan fix Task 3 (CMPController.AbandonExam).
    private static Task<int> RunAbandonGuardAsync(ApplicationDbContext ctx, int id, string actorUserId) =>
        ctx.AssessmentSessions
            .Where(a => a.Id == id && a.UserId == actorUserId
                && (a.Status == S.InProgress || a.Status == S.Open))
            .ExecuteUpdateAsync(a => a
                .SetProperty(x => x.Status, S.Abandoned)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

    // ---- Test A: sesi Completed → guard menolak (rowsAffected==0, Status tetap Completed) ----
    [Fact]
    public async Task Abandon_OnCompletedSession_RowsAffectedZero_StatusPreserved()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId, S.Completed);

        var rows = await RunAbandonGuardAsync(ctx, sessionId, userId);

        Assert.Equal(0, rows);
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.Equal(S.Completed, s!.Status); // verdict graded TIDAK ter-overwrite
    }

    // ---- Test B: ownership — user lain mencoba abandon sesi InProgress milik X → 0 baris ----
    [Fact]
    public async Task Abandon_ByNonOwner_RowsAffectedZero()
    {
        await using var ctx = NewCtx();
        var ownerId = await SeedUserAsync(ctx);
        var attackerId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, ownerId, S.InProgress);

        var rows = await RunAbandonGuardAsync(ctx, sessionId, attackerId);

        Assert.Equal(0, rows);
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.Equal(S.InProgress, s!.Status); // sesi orang lain tak tersentuh
    }

    // ---- Test C: happy — owner abandon sesi InProgress → 1 baris, Status=Abandoned ----
    [Fact]
    public async Task Abandon_ByOwner_InProgress_RowsAffectedOne_StatusAbandoned()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedSessionAsync(ctx, userId, S.InProgress);

        var rows = await RunAbandonGuardAsync(ctx, sessionId, userId);

        Assert.Equal(1, rows);
        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FindAsync(sessionId);
        Assert.Equal(S.Abandoned, s!.Status);
    }
}
