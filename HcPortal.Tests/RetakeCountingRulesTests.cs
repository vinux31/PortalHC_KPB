// RetakeCountingRulesTests — v32.7 Phase 421 Plan 02 (RTH-03 / RTK-LOGIC-03).
// Integration real-SQL (reuse RetakeServiceFixture: disposable HcPortalDB_Test_{guid} @ localhost\SQLEXPRESS).
// Membuktikan D-05 satu-sumber counting era-retake (snapshot-presence):
//   1. per-user cap (CountForUserAsync) = jumlah arsip ber-snapshot untuk (userId,Title,Category).
//   2. legacy HC-reset (AttemptHistory TANPA child archive) TIDAK dihitung di kedua bentuk.
//   3. max-in-group (MaxInGroupAsync) = MAX count antar-user di grup (Title,Category), bukan per-user/sum.
//   4. parity: kedua bentuk memakai filter snapshot-presence yang SAMA → cap & warning konsisten.
// [Trait("Category","Integration")] → SQL-less CI skip via --filter "Category!=Integration".
using System;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class RetakeCountingRulesTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public RetakeCountingRulesTests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "cnt-" + Guid.NewGuid().ToString("N")[..8], Email = "cnt@test.local", FullName = "Count Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    /// <summary>Seed 1 era-retake AttemptHistory (DENGAN ≥1 child archive) → konsumsi count (D-05).</summary>
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

    /// <summary>Seed 1 legacy AttemptHistory (TANPA child archive) → TIDAK dihitung (D-05).</summary>
    private static async Task SeedLegacyArchiveAsync(ApplicationDbContext ctx, string userId, string title, string category)
    {
        ctx.AssessmentAttemptHistory.Add(new AssessmentAttemptHistory { SessionId = 0, UserId = userId, Title = title, Category = category, AttemptNumber = 1 });
        await ctx.SaveChangesAsync();
    }

    // ====================== Test 1: per-user cap menghitung arsip ber-snapshot ======================
    [Fact]
    public async Task CountForUser_CountsSnapshotArchives()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        await SeedEraRetakeArchiveAsync(ctx, userId, "T1", "Cat");
        await SeedEraRetakeArchiveAsync(ctx, userId, "T1", "Cat");

        int count = await RetakeCountingRules.CountForUserAsync(ctx, userId, "T1", "Cat");
        Assert.Equal(2, count);
    }

    // ====================== Test 2: legacy (tanpa child) TIDAK dihitung ======================
    [Fact]
    public async Task CountForUser_ExcludesLegacyArchives()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        await SeedEraRetakeArchiveAsync(ctx, userId, "T2", "Cat");   // ber-snapshot → dihitung
        await SeedLegacyArchiveAsync(ctx, userId, "T2", "Cat");      // legacy → diabaikan
        await SeedLegacyArchiveAsync(ctx, userId, "T2", "Cat");      // legacy → diabaikan

        int count = await RetakeCountingRules.CountForUserAsync(ctx, userId, "T2", "Cat");
        Assert.Equal(1, count);   // hanya 1 era-retake ber-snapshot
    }

    // ====================== Test 3: max-in-group = MAX antar-user (bukan sum/per-user) ======================
    [Fact]
    public async Task MaxInGroup_ReturnsMaxAcrossUsers()
    {
        await using var ctx = NewCtx();
        var userA = await SeedUserAsync(ctx);
        var userB = await SeedUserAsync(ctx);
        // UserA = 3 arsip ber-snapshot, UserB = 1 — semua (Title,Category) sama.
        for (int i = 0; i < 3; i++) await SeedEraRetakeArchiveAsync(ctx, userA, "T3", "Cat");
        await SeedEraRetakeArchiveAsync(ctx, userB, "T3", "Cat");
        // + legacy noise userA (tak boleh menambah max)
        await SeedLegacyArchiveAsync(ctx, userA, "T3", "Cat");

        int max = await RetakeCountingRules.MaxInGroupAsync(ctx, "T3", "Cat");
        Assert.Equal(3, max);   // max(UserA=3, UserB=1) = 3 (bukan 4 sum, bukan legacy-inflated)
    }

    // ====================== Test 4: parity — cap & warning memakai filter snapshot-presence yang SAMA ======================
    [Fact]
    public async Task Parity_CapAndWarning_ShareSnapshotFilter()
    {
        await using var ctx = NewCtx();
        var userA = await SeedUserAsync(ctx);
        var userB = await SeedUserAsync(ctx);
        await SeedEraRetakeArchiveAsync(ctx, userA, "T4", "Cat");
        await SeedEraRetakeArchiveAsync(ctx, userA, "T4", "Cat");   // userA = 2 era
        await SeedLegacyArchiveAsync(ctx, userA, "T4", "Cat");      // legacy → diabaikan KEDUA bentuk
        await SeedEraRetakeArchiveAsync(ctx, userB, "T4", "Cat");   // userB = 1 era

        int capA = await RetakeCountingRules.CountForUserAsync(ctx, userA, "T4", "Cat");
        int maxGroup = await RetakeCountingRules.MaxInGroupAsync(ctx, "T4", "Cat");
        Assert.Equal(2, capA);       // per-user userA (legacy excluded)
        Assert.Equal(2, maxGroup);   // max(userA=2, userB=1) = 2 (legacy excluded di base yang sama)
    }

    // ====================== Test 5: grup kosong → 0 (guard FirstOrDefault) ======================
    [Fact]
    public async Task MaxInGroup_EmptyGroup_ReturnsZero()
    {
        await using var ctx = NewCtx();
        int max = await RetakeCountingRules.MaxInGroupAsync(ctx, "NoSuchTitle", "NoCat");
        Assert.Equal(0, max);
    }
}
