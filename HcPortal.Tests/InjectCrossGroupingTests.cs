// Phase 397 INJ-12 / spec §13 (KRITIS) — RED (TDD Wave 0). Mengunci invarian SILANG inject↔online:
// pasangan Pre↔Post tampil utuh saat 1 sisi inject & 1 sisi online (pair by LinkedGroupId + UserId,
// CMPController.cs:3417-3433 GetGainScoreData). Plus inject↔inject TIDAK menyentuh data online (D-10).
//
// Ini test PALING penting fase 397 — membuktikan nilai fitur NYATA (pasangan benar-benar tampil),
// bukan sekadar kolom tertulis. GREEN hanya bila LinkedGroupId ditulis benar per D-01.
//
// ⚠ RED until Wave 1 (397-02) adds per-worker link resolution via req.LinkTargetRepId (LinkedGroupId
//   ditulis benar). Tes ini juga me-reference PreviewPairingAsync (Wave 1) untuk verifikasi inject↔inject
//   pairing sebelum commit — sehingga assembly TIDAK compile di Wave 0 (missing-symbol, BUKAN syntax).
//   Restoring fast suite + integration green = Wave 1 gate.
//
// Cross-grouping di-assert dengan MENG-QUERY cara yang SAMA dengan GetGainScoreData (controller action
// return Json, butuh HttpContext → tidak dipanggil langsung; query EF identik = invarian yang dikunci).
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
public class InjectCrossGroupingTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    public InjectCrossGroupingTests(InjectAssessmentFixture fixture) => _fixture = fixture;

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
            FullName = "CrossGrp Test " + nip,
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

    // Replikasi query GetGainScoreData (CMPController.cs:3415-3433): pasang Pre↔Post by LinkedGroupId + UserId,
    // Status=="Completed", Score.HasValue. Mengembalikan jumlah pasangan per-worker (gain rows).
    private static async Task<List<(string userId, int preScore, int postScore)>> GainScorePairsAsync(
        ApplicationDbContext ctx, int assessmentGroupId)
    {
        var preSessions = await ctx.AssessmentSessions
            .Where(s => s.LinkedGroupId == assessmentGroupId
                        && s.AssessmentType == "PreTest"
                        && s.Status == "Completed"
                        && s.Score.HasValue)
            .ToListAsync();
        var postSessionDict = await ctx.AssessmentSessions
            .Where(s => s.LinkedGroupId == assessmentGroupId
                        && s.AssessmentType == "PostTest"
                        && s.Status == "Completed"
                        && s.Score.HasValue)
            .ToDictionaryAsync(s => s.UserId, s => s);

        var pairs = new List<(string, int, int)>();
        foreach (var pre in preSessions)
        {
            if (!postSessionDict.TryGetValue(pre.UserId, out var post)) continue;
            pairs.Add((pre.UserId, pre.Score ?? 0, post.Score ?? 0));
        }
        return pairs;
    }

    // ── CrossLink_GainScore_Intact: ONLINE Post group + inject Pre tertaut → GetGainScoreData-equivalent
    //    mengembalikan gain row per-worker yang punya BOTH inject Pre (Completed, Score) AND online Post
    //    (Completed, Score). Membuktikan pasangan tampil silang inject↔online by LinkedGroupId + UserId. ──
    [Fact]
    public async Task CrossLink_GainScore_Intact_InjectPre_OnlinePost()
    {
        string uid, nip; int repPostId;
        await using (var seed = NewCtx())
        {
            (uid, nip) = await SeedUserAsync(seed, "XGRP-A");
            var dt = new DateTime(2026, 3, 17);
            // ONLINE Post group (Kasus A): rep + LinkedGroupId = rep.
            repPostId = await SeedOnlineSessionAsync(seed, uid, "Post Cross", "PostTest", 90, null, dt);
            var p = await seed.AssessmentSessions.FindAsync(repPostId);
            p!.LinkedGroupId = repPostId; await seed.SaveChangesAsync();
        }

        // Inject Pre tertaut ke grup online — semua-benar → Score 100.
        var req = new InjectRequest
        {
            Title = "Inject Pre Cross " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 17), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(), Workers = new() { McWorker(nip) }
        };
        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(result.Success, result.Message);

        await using var verify = NewCtx();
        // GetGainScoreData-equivalent: pasangan inject-Pre ↔ online-Post HARUS muncul (KRITIS).
        var pairs = await GainScorePairsAsync(verify, repPostId);
        Assert.Single(pairs);
        Assert.Equal(uid, pairs[0].userId);
        Assert.Equal(100, pairs[0].preScore);   // inject Pre (semua benar)
        Assert.Equal(90, pairs[0].postScore);    // online Post asli
    }

    // ── CrossLink_Both_Inject: inject Post group dulu (committed), lalu inject Pre tertaut (inject↔inject);
    //    pairing utuh; TIDAK ada sesi online disentuh → 0 audit "LinkPrePost" (D-10). ──
    [Fact]
    public async Task CrossLink_Both_Inject_PairingIntact_NoOnlineTouched()
    {
        string uid, nip;
        await using (var seed = NewCtx()) { (uid, nip) = await SeedUserAsync(seed, "XGRP-B"); }

        // 1) Inject Post group dulu (standalone — tak tertaut apa pun saat dibuat).
        var titlePost = "Inject Post II " + Guid.NewGuid().ToString("N")[..6];
        var reqPost = new InjectRequest
        {
            Title = titlePost, Category = "IHT", AssessmentType = "PostTest",
            CompletedAt = new DateTime(2026, 2, 18), PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = OneMcQuestion(), Workers = new() { McWorker(nip) }   // LinkTargetRepId null (standalone)
        };
        var postResult = await NewInjectService(NewCtx()).InjectBatchAsync(reqPost, "actor", "Actor");
        Assert.True(postResult.Success, postResult.Message);
        int injectPostId = postResult.SuccessSessionIds[0];

        // RepresentativeId room Post inject = sesi inject Post (target untuk Pre).
        int repPostId = injectPostId;

        // 2) Inject Pre tertaut ke room INJECT Post (inject↔inject).
        var titlePre = "Inject Pre II " + Guid.NewGuid().ToString("N")[..6];
        var reqPre = new InjectRequest
        {
            Title = titlePre, Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 19), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(), Workers = new() { McWorker(nip) }
        };
        var preResult = await NewInjectService(NewCtx()).InjectBatchAsync(reqPre, "actor", "Actor");
        Assert.True(preResult.Success, preResult.Message);
        int injectPreId = preResult.SuccessSessionIds[0];

        await using var verify = NewCtx();
        // resolvedGroupId — Pre & Post inject berbagi LinkedGroupId.
        var preS = await verify.AssessmentSessions.FindAsync(injectPreId);
        var postS = await verify.AssessmentSessions.FindAsync(injectPostId);
        Assert.NotNull(preS!.LinkedGroupId);
        Assert.Equal(preS.LinkedGroupId, postS!.LinkedGroupId);

        // Pairing inject↔inject utuh via query GetGainScoreData-equivalent.
        var pairs = await GainScorePairsAsync(verify, preS.LinkedGroupId!.Value);
        Assert.Single(pairs);
        Assert.Equal(uid, pairs[0].userId);

        // inject↔inject TIDAK menyentuh data online → 0 audit "LinkPrePost" (D-10).
        // (kedua sesi inject IsManualEntry=true; tak ada sesi online dimutasi.)
        var linkPrePostCount = await verify.AuditLogs.CountAsync(a => a.ActionType == "LinkPrePost"
            && (a.TargetId == injectPreId || a.TargetId == injectPostId));
        Assert.Equal(0, linkPrePostCount);
    }

    // ── CrossLink_PreviewPairing_BeforeCommit: PreviewPairingAsync (Wave 1) untuk inject↔inject
    //    melaporkan Paired==1 (1 pekerja punya sibling Post inject di grup target) sebelum commit. ──
    [Fact]
    public async Task CrossLink_PreviewPairing_InjectInject_ReportsPaired()
    {
        string uid, nip;
        await using (var seed = NewCtx()) { (uid, nip) = await SeedUserAsync(seed, "XGRP-C"); }

        // Inject Post group dulu (akan jadi target Pre).
        var reqPost = new InjectRequest
        {
            Title = "Inject Post III " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PostTest",
            CompletedAt = new DateTime(2026, 2, 20), PassPercentage = 70, CertMode = InjectCertMode.None,
            Questions = OneMcQuestion(), Workers = new() { McWorker(nip) }
        };
        var postResult = await NewInjectService(NewCtx()).InjectBatchAsync(reqPost, "actor", "Actor");
        Assert.True(postResult.Success, postResult.Message);
        int repPostId = postResult.SuccessSessionIds[0];

        // PreviewPairing untuk Pre inject yang akan tertaut ke room inject Post → Paired==1 (uid punya sibling).
        var pairing = await NewInjectService(NewCtx())
            .PreviewPairingAsync(repPostId, "PreTest", new List<string> { uid }, new DateTime(2026, 2, 21));
        Assert.True(pairing.HasLink);
        Assert.Equal(1, pairing.Paired);
        Assert.Equal(0, pairing.Unpaired);
    }
}
