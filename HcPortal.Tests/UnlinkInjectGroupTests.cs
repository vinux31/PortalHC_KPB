// Phase 397 INJ-12 / D-12 — RED (TDD Wave 0). Mengunci unlink/revert pasca-commit:
// revert LinkedSessionId bidirectional + (Kasus B kosong-sebelah) revert LinkedGroupId pada online;
// skor/status online UNCHANGED; audit "LinkPrePostUndo" per sesi dimutasi; atomic.
//
// ⚠ RED until Wave 1 (397-02) adds the NEW service method:
//   Task<InjectResult> UnlinkInjectGroupAsync(int injectGroupId, string actorUserId, string actorName)
//   Pada Wave 0 symbol UnlinkInjectGroupAsync BELUM ADA → assembly test ini TIDAK compile
//   (missing-symbol, BUKAN syntax error). Itu RED yang BENAR. Restoring fast suite + integration green = Wave 1 gate.
//
// Fixture disposable HcPortalDB_Test_{guid} (InjectAssessmentFixture). HcPortalDB_Dev TAK tersentuh (T-397-02).
// [Trait Category=Integration] — real SQLEXPRESS.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class UnlinkInjectGroupTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    public UnlinkInjectGroupTests(InjectAssessmentFixture fixture) => _fixture = fixture;

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

    private InjectAssessmentService NewInjectService(ApplicationDbContext ctx)
        => new InjectAssessmentService(ctx, NewGradingService(ctx), NullLogger<InjectAssessmentService>.Instance);

    private static async Task<(string userId, string nip)> SeedUserAsync(ApplicationDbContext ctx, string nipPrefix)
    {
        var nip = nipPrefix + "-" + Guid.NewGuid().ToString("N")[..6];
        var u = new ApplicationUser
        {
            UserName = "inj-" + Guid.NewGuid().ToString("N")[..8],
            Email = $"inj-{Guid.NewGuid().ToString("N")[..8]}@test.local",
            FullName = "Unlink Test " + nip,
            NIP = nip
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return (u.Id, nip);
    }

    private static async Task<int> SeedOnlineSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string assessmentType,
        int score, int? linkedGroupId, DateTime completedAt)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = "IHT", AssessmentType = assessmentType,
            IsManualEntry = false, AccessToken = "ONLINE", IsTokenRequired = false,
            Status = AssessmentConstants.AssessmentStatus.Completed, Score = score, IsPassed = score >= 70,
            PassPercentage = 70, DurationMinutes = 60, LinkedGroupId = linkedGroupId,
            Schedule = completedAt, StartedAt = completedAt, CompletedAt = completedAt,
            Progress = 100, AllowAnswerReview = true, CreatedAt = DateTime.UtcNow
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    private static List<InjectQuestionSpec> OneMcQuestion() => new()
    {
        new() { TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10, ElemenTeknis = "ET-A", QuestionText = "MC",
            Options = new() { new() { TempId = 1, OptionText = "benar", IsCorrect = true }, new() { TempId = 2, OptionText = "salah", IsCorrect = false } } }
    };

    private static InjectWorkerSpec McWorker(string nip) => new()
    {
        Nip = nip,
        Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { 1 } } }
    };

    // ── Unlink_RevertBidirectional: setelah Kasus-A link committed, unlink grup inject →
    //    inject LinkedGroupId/LinkedSessionId null; online sibling.LinkedSessionId revert null;
    //    online Score/Status UNCHANGED; audit "LinkPrePostUndo" per sesi dimutasi. ──
    [Fact]
    public async Task Unlink_RevertBidirectional_OnlineUnchanged_AuditUndo()
    {
        string uid, nip; int repPostId; int injectGroupId; int injectSessionId;
        await using (var seed = NewCtx())
        {
            (uid, nip) = await SeedUserAsync(seed, "UNL-RB");
            var dt = new DateTime(2026, 3, 11);
            repPostId = await SeedOnlineSessionAsync(seed, uid, "Post Unlink A", "PostTest", 80, null, dt);
            var p = await seed.AssessmentSessions.FindAsync(repPostId);
            p!.LinkedGroupId = repPostId; await seed.SaveChangesAsync();   // grouped (Kasus A)
        }

        // Commit link (Kasus A adopt).
        var req = new InjectRequest
        {
            Title = "Inject Pre UNL-RB " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 11), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(), Workers = new() { McWorker(nip) }
        };
        var linkResult = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(linkResult.Success, linkResult.Message);
        injectSessionId = linkResult.SuccessSessionIds[0];
        await using (var c = NewCtx())
        {
            var inj = await c.AssessmentSessions.FindAsync(injectSessionId);
            injectGroupId = inj!.LinkedGroupId!.Value;   // = repPostId (adopt)
        }

        // snapshot online before unlink
        int onlineScoreBefore; string onlineStatusBefore;
        await using (var c = NewCtx())
        {
            var p = await c.AssessmentSessions.FindAsync(repPostId);
            onlineScoreBefore = p!.Score!.Value; onlineStatusBefore = p.Status;
        }

        // UNLINK (D-12) — NEW Wave 1 method.
        var unlink = await NewInjectService(NewCtx()).UnlinkInjectGroupAsync(injectGroupId, "actor", "Actor");
        Assert.True(unlink.Success, unlink.Message);

        await using var verify = NewCtx();
        var inj2 = await verify.AssessmentSessions.FindAsync(injectSessionId);
        Assert.Null(inj2!.LinkedGroupId);
        Assert.Null(inj2.LinkedSessionId);

        // online sibling.LinkedSessionId revert null (bidirectional).
        var post = await verify.AssessmentSessions.FindAsync(repPostId);
        Assert.Null(post!.LinkedSessionId);
        // online Score/Status UNCHANGED.
        Assert.Equal(onlineScoreBefore, post.Score);
        Assert.Equal(onlineStatusBefore, post.Status);

        // audit "LinkPrePostUndo" per sesi dimutasi.
        var undoAudits = await verify.AuditLogs.Where(a => a.ActionType == "LinkPrePostUndo").ToListAsync();
        Assert.True(undoAudits.Count >= 1);
    }

    // ── Unlink_KasusB_RevertSticker_WhenOneSided: setelah Kasus-B link (stiker ditulis ke online),
    //    unlink → bila grup jadi single-type, online LinkedGroupId revert null (stiker dilepas); Score/Status UNCHANGED. ──
    [Fact]
    public async Task Unlink_KasusB_RevertSticker_WhenGroupBecomesSingleType()
    {
        string uid, nip; int repPostId; int injectGroupId;
        await using (var seed = NewCtx())
        {
            (uid, nip) = await SeedUserAsync(seed, "UNL-KB");
            var dt = new DateTime(2026, 3, 12);
            // Room Post STANDALONE (Kasus B) — 1 sesi.
            repPostId = await SeedOnlineSessionAsync(seed, uid, "Post Unlink B", "PostTest", 70, null, dt);
        }

        var req = new InjectRequest
        {
            Title = "Inject Pre UNL-KB " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 12), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(), Workers = new() { McWorker(nip) }
        };
        var linkResult = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(linkResult.Success, linkResult.Message);
        await using (var c = NewCtx())
        {
            var post = await c.AssessmentSessions.FindAsync(repPostId);
            Assert.Equal(repPostId, post!.LinkedGroupId);   // stiker Kasus B tertulis
            injectGroupId = repPostId;
        }

        var unlink = await NewInjectService(NewCtx()).UnlinkInjectGroupAsync(injectGroupId, "actor", "Actor");
        Assert.True(unlink.Success, unlink.Message);

        await using var verify = NewCtx();
        // Setelah inject Pre dilepas, grup jadi single-type (hanya Post) → stiker online di-revert null.
        var post2 = await verify.AssessmentSessions.FindAsync(repPostId);
        Assert.Null(post2!.LinkedGroupId);          // stiker Kasus B dilepas (heuristik single-type, Open Q 1)
        // Score/Status UNCHANGED.
        Assert.Equal(70, post2.Score);
        Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, post2.Status);
    }

    // ── Unlink_Atomic: bila unlink gagal (group id tak valid / tak ada sesi inject) → tidak ada mutasi parsial. ──
    [Fact]
    public async Task Unlink_Atomic_InvalidGroupLeavesStateIntact()
    {
        string uid, nip; int repPostId; int injectGroupId; int injectSessionId;
        await using (var seed = NewCtx())
        {
            (uid, nip) = await SeedUserAsync(seed, "UNL-AT");
            var dt = new DateTime(2026, 3, 13);
            repPostId = await SeedOnlineSessionAsync(seed, uid, "Post Unlink AT", "PostTest", 88, null, dt);
            var p = await seed.AssessmentSessions.FindAsync(repPostId);
            p!.LinkedGroupId = repPostId; await seed.SaveChangesAsync();
        }
        var req = new InjectRequest
        {
            Title = "Inject Pre UNL-AT " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 13), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(), Workers = new() { McWorker(nip) }
        };
        var linkResult = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(linkResult.Success, linkResult.Message);
        injectSessionId = linkResult.SuccessSessionIds[0];
        injectGroupId = repPostId;

        // snapshot pre-unlink link columns
        int? injGroupBefore, injSibBefore, postSibBefore;
        await using (var c = NewCtx())
        {
            var inj = await c.AssessmentSessions.FindAsync(injectSessionId);
            var post = await c.AssessmentSessions.FindAsync(repPostId);
            injGroupBefore = inj!.LinkedGroupId; injSibBefore = inj.LinkedSessionId; postSibBefore = post!.LinkedSessionId;
        }

        // Unlink dengan group id TIDAK ada sesi inject → harus no-op/gagal bersih, state utuh.
        var bogusGroupId = injectGroupId + 999_999;
        var unlink = await NewInjectService(NewCtx()).UnlinkInjectGroupAsync(bogusGroupId, "actor", "Actor");
        Assert.False(unlink.Success);   // tak ada sesi inject ber-group itu

        await using var verify = NewCtx();
        var inj2 = await verify.AssessmentSessions.FindAsync(injectSessionId);
        var post2 = await verify.AssessmentSessions.FindAsync(repPostId);
        // Link columns utuh (pre-unlink state) — tidak ada mutasi parsial.
        Assert.Equal(injGroupBefore, inj2!.LinkedGroupId);
        Assert.Equal(injSibBefore, inj2.LinkedSessionId);
        Assert.Equal(postSibBefore, post2!.LinkedSessionId);
    }

    // ── 398.1 WR-01 (cross-batch): DUA batch inject adopt grup Kasus A yang SAMA → unlink(grup).
    //    Unlink BY-GROUP (signature `(int injectGroupId)` tak punya batch discriminator) → group-wide revert
    //    adalah PERILAKU BY-DESIGN (UI hanya expose unlink level-grup; per-batch narrow = perubahan
    //    signature+UI = di luar scope tech-debt → WR-01 DROP). Test ini MENGUNCI invariant keselamatan
    //    yang BENAR-BENAR penting (T-397-04): skor/status ONLINE tiap batch TIDAK tersentuh, atomic. ──
    [Fact]
    public async Task Unlink_CrossBatch_OnlineScoreStatus_Unchanged_GroupWideByDesign()
    {
        string uid1, nip1, uid2, nip2; int postW1Id, postW2Id; int groupId;
        await using (var seed = NewCtx())
        {
            (uid1, nip1) = await SeedUserAsync(seed, "XB1");
            (uid2, nip2) = await SeedUserAsync(seed, "XB2");
            var dt = new DateTime(2026, 3, 14);
            // Dua online Post (per worker) di-grup Kasus A yang sama (group = post w1).
            postW1Id = await SeedOnlineSessionAsync(seed, uid1, "Post XBATCH", "PostTest", 80, null, dt);
            postW2Id = await SeedOnlineSessionAsync(seed, uid2, "Post XBATCH", "PostTest", 85, null, dt);
            var p1 = await seed.AssessmentSessions.FindAsync(postW1Id);
            var p2 = await seed.AssessmentSessions.FindAsync(postW2Id);
            p1!.LinkedGroupId = postW1Id; p2!.LinkedGroupId = postW1Id;   // Kasus A (sudah grouped)
            await seed.SaveChangesAsync();
        }

        // Batch-1: inject Pre worker1 adopt grup (target = post w1).
        var req1 = new InjectRequest
        {
            Title = "Inject Pre XB1 " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 14), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = postW1Id, Questions = OneMcQuestion(), Workers = new() { McWorker(nip1) }
        };
        var r1 = await NewInjectService(NewCtx()).InjectBatchAsync(req1, "actor", "Actor");
        Assert.True(r1.Success, r1.Message);
        var inj1Id = r1.SuccessSessionIds[0];

        // Batch-2: inject Pre worker2 adopt grup SAMA (target = post w2, yang ber-LinkedGroupId = post w1).
        var req2 = new InjectRequest
        {
            Title = "Inject Pre XB2 " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 14), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = postW2Id, Questions = OneMcQuestion(), Workers = new() { McWorker(nip2) }
        };
        var r2 = await NewInjectService(NewCtx()).InjectBatchAsync(req2, "actor", "Actor");
        Assert.True(r2.Success, r2.Message);
        var inj2Id = r2.SuccessSessionIds[0];

        await using (var c = NewCtx())
        {
            // Kedua batch adopt grup SAMA (= post w1 id).
            groupId = (await c.AssessmentSessions.FindAsync(inj1Id))!.LinkedGroupId!.Value;
            Assert.Equal(groupId, (await c.AssessmentSessions.FindAsync(inj2Id))!.LinkedGroupId!.Value);
        }

        var unlink = await NewInjectService(NewCtx()).UnlinkInjectGroupAsync(groupId, "actor", "Actor");
        Assert.True(unlink.Success, unlink.Message);   // atomic, sukses

        await using var verify = NewCtx();
        // Group-wide revert BY-DESIGN: KEDUA batch inject ter-revert (UI hanya unlink level-grup).
        Assert.Null((await verify.AssessmentSessions.FindAsync(inj1Id))!.LinkedGroupId);
        Assert.Null((await verify.AssessmentSessions.FindAsync(inj2Id))!.LinkedGroupId);
        // INVARIANT KESELAMATAN (T-397-04): skor & status ONLINE tiap batch TIDAK tersentuh.
        var post1 = await verify.AssessmentSessions.FindAsync(postW1Id);
        var post2 = await verify.AssessmentSessions.FindAsync(postW2Id);
        Assert.Equal(80, post1!.Score);
        Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, post1.Status);
        Assert.Equal(85, post2!.Score);
        Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, post2.Status);
    }
}
