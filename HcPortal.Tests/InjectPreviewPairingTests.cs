// Phase 397 INJ-12 / D-07 — RED (TDD Wave 0). Mengunci preview pairing (dry-run, NO write) == commit:
// jumlah Paired/Unpaired di preview == jumlah pair aktual pasca-commit; WillTouchOnline (Kasus B);
// DateWarn (D-11, skip bila CompletedAt sibling null — Open Q 2); DoubleLinkErrors (D-08 daftar lengkap).
//
// ⚠ RED until Wave 1 (397-02) adds the NEW service method:
//   Task<InjectPairingPreview> PreviewPairingAsync(int? linkTargetRepId, string injectAssessmentType,
//       IReadOnlyList<string> injectUserIds, DateTime injectCompletedAt)
//   yang nanti (Wave 2) dipanggil controller PreviewInjectScore extension. Pada Wave 0 symbol
//   PreviewPairingAsync BELUM ADA → assembly test ini TIDAK compile (missing-symbol, BUKAN syntax).
//   Itu RED yang BENAR. Restoring fast suite + integration green = Wave 1 gate.
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
public class InjectPreviewPairingTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    public InjectPreviewPairingTests(InjectAssessmentFixture fixture) => _fixture = fixture;

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
            FullName = "Pairing Test " + nip,
            NIP = nip
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return (u.Id, nip);
    }

    private static async Task<int> SeedOnlineSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string assessmentType,
        int score, int? linkedGroupId, DateTime? completedAt)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = "IHT", AssessmentType = assessmentType,
            IsManualEntry = false, AccessToken = "ONLINE", IsTokenRequired = false,
            Status = AssessmentConstants.AssessmentStatus.Completed, Score = score, IsPassed = score >= 70,
            PassPercentage = 70, DurationMinutes = 60, LinkedGroupId = linkedGroupId,
            Schedule = completedAt ?? DateTime.UtcNow, StartedAt = completedAt, CompletedAt = completedAt,
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

    // ── PairingPreview_MatchesCommit: K sibling UserIds di grup target, M inject workers, overlap P.
    //    preview.Paired==P, preview.Unpaired==M-P, NO DB write saat preview; pasca-commit REAL paired (LinkedSessionId != null) == P. ──
    [Fact]
    public async Task PairingPreview_MatchesCommit_NoWriteDuringPreview()
    {
        // K=2 sibling (uidA, uidB) di grup Post; inject M=3 (uidA, uidB, uidC) → overlap P=2, unpaired=1.
        string uidA, nipA, uidB, nipB, uidC, nipC; int repPostId;
        await using (var seed = NewCtx())
        {
            (uidA, nipA) = await SeedUserAsync(seed, "PVP-A");
            (uidB, nipB) = await SeedUserAsync(seed, "PVP-B");
            (uidC, nipC) = await SeedUserAsync(seed, "PVP-C");
            var dt = new DateTime(2026, 3, 14);
            repPostId = await SeedOnlineSessionAsync(seed, uidA, "Post Pairing", "PostTest", 80, null, dt);
            var p = await seed.AssessmentSessions.FindAsync(repPostId);
            p!.LinkedGroupId = repPostId; await seed.SaveChangesAsync();   // grouped (Kasus A)
            await SeedOnlineSessionAsync(seed, uidB, "Post Pairing", "PostTest", 90, repPostId, dt);
            // uidC TIDAK punya sibling Post di grup → unpaired.
        }

        var injectUserIds = new List<string> { uidA, uidB, uidC };
        var injectCompletedAt = new DateTime(2026, 2, 14);

        // snapshot DB count sebelum preview (NO write).
        int sessBefore, auditBefore;
        await using (var c = NewCtx())
        {
            sessBefore = await c.AssessmentSessions.CountAsync();
            auditBefore = await c.AuditLogs.CountAsync();
        }

        // PREVIEW pairing (dry-run, NEW Wave 1 method) — NO write.
        var pairing = await NewInjectService(NewCtx())
            .PreviewPairingAsync(repPostId, "PreTest", injectUserIds, injectCompletedAt);
        Assert.True(pairing.HasLink);
        Assert.Equal(2, pairing.Paired);     // P = uidA + uidB
        Assert.Equal(1, pairing.Unpaired);   // M-P = uidC
        Assert.False(pairing.WillTouchOnline);   // Kasus A (grup sudah ada)

        // NO DB write akibat preview.
        await using (var c = NewCtx())
        {
            Assert.Equal(sessBefore, await c.AssessmentSessions.CountAsync());
            Assert.Equal(auditBefore, await c.AuditLogs.CountAsync());
        }

        // COMMIT: REAL paired count == P (sesi inject dengan LinkedSessionId != null).
        var req = new InjectRequest
        {
            Title = "Inject Pre PVP " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = injectCompletedAt, PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(),
            Workers = new() { McWorker(nipA), McWorker(nipB), McWorker(nipC) }
        };
        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(result.Success, result.Message);

        await using var verify = NewCtx();
        var injects = await verify.AssessmentSessions.Where(s => result.SuccessSessionIds.Contains(s.Id)).ToListAsync();
        int realPaired = injects.Count(s => s.LinkedSessionId != null);
        Assert.Equal(pairing.Paired, realPaired);   // preview == commit outcome
    }

    // ── PairingPreview_KasusB_WillTouchOnline: target standalone → WillTouchOnline true; Kasus A → false. ──
    [Fact]
    public async Task PairingPreview_KasusB_WillTouchOnline_True_KasusA_False()
    {
        string uid, nip; int standaloneRep; int groupedRep;
        await using (var seed = NewCtx())
        {
            (uid, nip) = await SeedUserAsync(seed, "PVP-WTO");
            var dt = new DateTime(2026, 3, 15);
            // standalone (Kasus B)
            standaloneRep = await SeedOnlineSessionAsync(seed, uid, "Post Standalone", "PostTest", 70, null, dt);
            // grouped (Kasus A)
            var (uid2, _) = await SeedUserAsync(seed, "PVP-WTO2");
            groupedRep = await SeedOnlineSessionAsync(seed, uid2, "Post Grouped", "PostTest", 75, null, dt);
            var g = await seed.AssessmentSessions.FindAsync(groupedRep);
            g!.LinkedGroupId = groupedRep; await seed.SaveChangesAsync();
        }

        var ids = new List<string> { uid };
        var kasusB = await NewInjectService(NewCtx()).PreviewPairingAsync(standaloneRep, "PreTest", ids, new DateTime(2026, 2, 15));
        Assert.True(kasusB.WillTouchOnline);

        var kasusA = await NewInjectService(NewCtx()).PreviewPairingAsync(groupedRep, "PreTest", ids, new DateTime(2026, 2, 15));
        Assert.False(kasusA.WillTouchOnline);
    }

    // ── PairingPreview_DateWarn: Pre.CompletedAt > Post sibling.CompletedAt (kedua non-null) → DateWarn true;
    //    sibling CompletedAt null → DateWarn false (D-11 / Open Q 2 skip-when-null). ──
    [Fact]
    public async Task PairingPreview_DateWarn_OnlyWhenBothNonNullAndPreNewer()
    {
        string uidWarn, _nipWarn, uidNull, _nipNull; int repWarn, repNull;
        await using (var seed = NewCtx())
        {
            (uidWarn, _nipWarn) = await SeedUserAsync(seed, "PVP-DW");
            (uidNull, _nipNull) = await SeedUserAsync(seed, "PVP-DN");
            // Post sibling CompletedAt LEBIH LAMA dari Pre inject → urutan janggal → warn.
            repWarn = await SeedOnlineSessionAsync(seed, uidWarn, "Post DateWarn", "PostTest", 80, null, new DateTime(2026, 1, 1));
            var w = await seed.AssessmentSessions.FindAsync(repWarn);
            w!.LinkedGroupId = repWarn; await seed.SaveChangesAsync();
            // Post sibling CompletedAt NULL → tak ada urutan → tak warn.
            repNull = await SeedOnlineSessionAsync(seed, uidNull, "Post DateNull", "PostTest", 80, null, null);
            var n = await seed.AssessmentSessions.FindAsync(repNull);
            n!.LinkedGroupId = repNull; await seed.SaveChangesAsync();
        }

        // Pre inject CompletedAt = 2026-06-01 (lebih BARU dari Post 2026-01-01) → DateWarn.
        var warn = await NewInjectService(NewCtx())
            .PreviewPairingAsync(repWarn, "PreTest", new List<string> { uidWarn }, new DateTime(2026, 6, 1));
        Assert.True(warn.DateWarn);

        // Post CompletedAt null → skip warn (Open Q 2).
        var noWarn = await NewInjectService(NewCtx())
            .PreviewPairingAsync(repNull, "PreTest", new List<string> { uidNull }, new DateTime(2026, 6, 1));
        Assert.False(noWarn.DateWarn);
    }

    // ── PairingPreview_DoubleLink: pekerja sudah punya sibling TIPE-SAMA di grup target → muncul di DoubleLinkErrors. ──
    [Fact]
    public async Task PairingPreview_DoubleLink_ListedInDoubleLinkErrors()
    {
        string uidX, nipX; int repId;
        await using (var seed = NewCtx())
        {
            (uidX, nipX) = await SeedUserAsync(seed, "PVP-DL");
            var dt = new DateTime(2026, 3, 16);
            var postRep = await SeedOnlineSessionAsync(seed, uidX, "Post DoubleLink", "PostTest", 80, null, dt);
            repId = postRep;
            var post = await seed.AssessmentSessions.FindAsync(postRep);
            post!.LinkedGroupId = repId; await seed.SaveChangesAsync();
            // X SUDAH punya Pre tipe-SAMA di grup repId.
            await SeedOnlineSessionAsync(seed, uidX, "Post DoubleLink Pre", "PreTest", 50, repId, dt);
        }

        var pairing = await NewInjectService(NewCtx())
            .PreviewPairingAsync(repId, "PreTest", new List<string> { uidX }, new DateTime(2026, 2, 16));
        Assert.Contains(pairing.DoubleLinkErrors, e => e.Nip == nipX);
    }
}
