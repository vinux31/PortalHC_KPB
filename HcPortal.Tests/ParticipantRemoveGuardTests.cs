// ParticipantRemoveGuardTests — v32.7 Phase 421 Plan 03 (RTH-04 / PA-06 / D-06).
// Integration real-SQL (reuse RetakeServiceFixture). Mereplikasi keputusan guard + cascade delete dari
// EditAssessment POST removedUserIds loop (pola "replicate endpoint body"). Membuktikan:
//   1. Sesi Abandoned / StartedAt!=null / ber-AttemptHistory → butuh konfirmasi (tanpa flag = batal).
//   2. Dengan flag → delete (RemoveRange AttemptHistory tracked + sesi) → cascade DB hapus archive → 0 orphan.
//   3. Backward-compat: sesi Open tanpa riwayat → langsung hapus tanpa konfirmasi.
// [Trait("Category","Integration")] → SQL-less CI skip via --filter "Category!=Integration".
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class ParticipantRemoveGuardTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public ParticipantRemoveGuardTests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "prg-" + Guid.NewGuid().ToString("N")[..8], Email = "prg@test.local", FullName = "Remove Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string category, string assessmentType,
        string status = "Open", DateTime? startedAt = null)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = category, Status = status,
            AccessToken = "", Schedule = new DateTime(2026, 2, 1),
            AssessmentType = assessmentType, StartedAt = startedAt, Score = 0, Progress = 0
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    /// <summary>Seed AttemptHistory (SessionId = sesi nyata) + 1 child archive. Return (histId, archiveId).</summary>
    private static async Task<(int histId, int archiveId)> SeedAttemptWithArchiveAsync(
        ApplicationDbContext ctx, int sessionId, string userId, string title, string category)
    {
        var hist = new AssessmentAttemptHistory { SessionId = sessionId, UserId = userId, Title = title, Category = category, AttemptNumber = 1 };
        ctx.AssessmentAttemptHistory.Add(hist);
        await ctx.SaveChangesAsync();
        var arc = new AssessmentAttemptResponseArchive
        {
            AttemptHistoryId = hist.Id, PackageQuestionId = 1, QuestionText = "Q", AnswerText = "A", IsCorrect = true, AwardedScore = 10
        };
        ctx.AssessmentAttemptResponseArchives.Add(arc);
        await ctx.SaveChangesAsync();
        return (hist.Id, arc.Id);
    }

    // Replikasi keputusan guard EditAssessment POST (AssessmentAdminController removedUserIds loop).
    private static async Task<bool> NeedsConfirmAsync(
        ApplicationDbContext ctx, AssessmentSession? pre, AssessmentSession? post, bool confirmFlag)
    {
        bool preHasHistory = pre != null && (pre.Status == "Abandoned" || pre.StartedAt != null);
        bool postHasHistory = post != null && (post.Status == "Abandoned" || post.StartedAt != null);
        var ids = new List<int>();
        if (pre != null) ids.Add(pre.Id);
        if (post != null) ids.Add(post.Id);
        bool hasAttempt = ids.Count > 0 && await ctx.AssessmentAttemptHistory.AnyAsync(h => ids.Contains(h.SessionId));
        return (preHasHistory || postHasHistory || hasAttempt) && !confirmFlag;
    }

    // Replikasi cascade delete EditAssessment POST (RemoveRange AttemptHistory tracked + sesi → SaveChanges).
    private static async Task DeleteSessionsAsync(ApplicationDbContext ctx, params AssessmentSession[] sessions)
    {
        var ids = sessions.Select(s => s.Id).ToList();
        var attempts = await ctx.AssessmentAttemptHistory.Where(h => ids.Contains(h.SessionId)).ToListAsync();
        if (attempts.Any()) ctx.AssessmentAttemptHistory.RemoveRange(attempts);   // tracked → cascade fires
        ctx.AssessmentSessions.RemoveRange(sessions);
        await ctx.SaveChangesAsync();
    }

    // ====================== Test 1: Abandoned tanpa flag → butuh konfirmasi (tidak terhapus) ======================
    [Fact]
    public async Task AbandonedSession_WithoutFlag_NeedsConfirm()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var preId = await SeedSessionAsync(ctx, userId, "GuardT1", "Cat", "PreTest", status: "Abandoned");
        var pre = await ctx.AssessmentSessions.FindAsync(preId);

        bool needs = await NeedsConfirmAsync(ctx, pre, null, confirmFlag: false);
        Assert.True(needs);   // butuh konfirmasi → controller continue (sesi tak dihapus)

        // Sesi masih ada (penghapusan dibatalkan).
        Assert.True(await ctx.AssessmentSessions.AnyAsync(s => s.Id == preId));
    }

    // ====================== Test 2: StartedAt!=null tanpa flag → butuh konfirmasi ======================
    [Fact]
    public async Task StartedSession_WithoutFlag_NeedsConfirm()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var preId = await SeedSessionAsync(ctx, userId, "GuardT2", "Cat", "PreTest", status: "Open", startedAt: DateTime.UtcNow.AddHours(-2));
        var pre = await ctx.AssessmentSessions.FindAsync(preId);

        bool needs = await NeedsConfirmAsync(ctx, pre, null, confirmFlag: false);
        Assert.True(needs);
    }

    // ====================== Test 3: ber-AttemptHistory tanpa flag → butuh konfirmasi ======================
    [Fact]
    public async Task AttemptHistorySession_WithoutFlag_NeedsConfirm()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // Status Open + StartedAt null (mis. sudah di-reset), TAPI ada AttemptHistory tercatat → harus terdeteksi.
        var postId = await SeedSessionAsync(ctx, userId, "GuardT3", "Cat", "PostTest", status: "Open");
        await SeedAttemptWithArchiveAsync(ctx, postId, userId, "GuardT3", "Cat");
        var post = await ctx.AssessmentSessions.FindAsync(postId);

        bool needs = await NeedsConfirmAsync(ctx, null, post, confirmFlag: false);
        Assert.True(needs);   // hasAttemptHistory = true via AnyAsync(SessionId)
    }

    // ====================== Test 4: dengan flag → hapus + 0 orphan archive (cascade DB) ======================
    [Fact]
    public async Task WithFlag_DeletesSessionAndArchives_NoOrphan()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var postId = await SeedSessionAsync(ctx, userId, "GuardT4", "Cat", "PostTest", status: "Open");
        var (histId, archiveId) = await SeedAttemptWithArchiveAsync(ctx, postId, userId, "GuardT4", "Cat");
        var post = await ctx.AssessmentSessions.FindAsync(postId);

        bool needs = await NeedsConfirmAsync(ctx, null, post, confirmFlag: true);
        Assert.False(needs);   // flag → lewati peringatan, lanjut hapus

        await DeleteSessionsAsync(ctx, post!);

        await using var verify = NewCtx();
        Assert.False(await verify.AssessmentSessions.AnyAsync(s => s.Id == postId));            // sesi terhapus
        Assert.False(await verify.AssessmentAttemptHistory.AnyAsync(h => h.Id == histId));       // AttemptHistory terhapus
        Assert.False(await verify.AssessmentAttemptResponseArchives.AnyAsync(a => a.Id == archiveId));      // 0 orphan (cascade)
        Assert.False(await verify.AssessmentAttemptResponseArchives.AnyAsync(a => a.AttemptHistoryId == histId));   // 0 orphan by FK
    }

    // ====================== Test 5: backward-compat — Open tanpa riwayat → hapus langsung tanpa konfirmasi ======================
    [Fact]
    public async Task NoHistorySession_WithoutFlag_DeletesDirectly()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var preId = await SeedSessionAsync(ctx, userId, "GuardT5", "Cat", "PreTest", status: "Open");   // StartedAt null, no AttemptHistory
        var pre = await ctx.AssessmentSessions.FindAsync(preId);

        bool needs = await NeedsConfirmAsync(ctx, pre, null, confirmFlag: false);
        Assert.False(needs);   // tak ada riwayat → langsung hapus (perilaku existing)

        await DeleteSessionsAsync(ctx, pre!);
        await using var verify = NewCtx();
        Assert.False(await verify.AssessmentSessions.AnyAsync(s => s.Id == preId));
    }
}
