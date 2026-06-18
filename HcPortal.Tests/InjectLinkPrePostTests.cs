// Phase 397 INJ-12 — RED (TDD Wave 0). Mengunci kontrak per-worker bidirectional linking + Kasus A/B
// (adopt vs tulis-stiker-ke-online) + atomic rollback. NO broadcast (Pitfall 1).
//
// ⚠ RED until Wave 1 (397-02) adds per-worker link resolution via req.LinkTargetRepId
//   (InjectBatchAsync membaca req.LinkTargetRepId → resolve LinkedSessionId by-UserId + Kasus A/B,
//    GANTI broadcast InjectAssessmentService.cs:120). Restoring fast suite + integration to green = Wave 1 gate.
//   Pada Wave 0 assembly test ini TIDAK compile (LinkTargetRepId resolution belum di-wire) — itu RED yang BENAR:
//   tes mendeskripsikan perilaku yang diharapkan; symbol kontrak (req.LinkTargetRepId) sudah ADA di DTO (Task 1),
//   tetapi perilaku resolve link belum diimplementasikan. Tes ini akan FAIL (bukan compile-error) sampai Wave 1.
//
// Fixture pakai disposable DB HcPortalDB_Test_{guid} (InjectAssessmentFixture, di-reuse dari
// InjectAssessmentServiceTests). DB lokal HcPortalDB_Dev TAK tersentuh (T-397-02). [Trait Category=Integration].
// EF Core 8 ExecuteUpdate/real grading butuh REAL SQLEXPRESS — BUKAN InMemory.
//
// Invarian load-bearing (CMPController.cs:3417-3433 GetGainScoreData): display memasangkan
// by LinkedGroupId + UserId. LinkedGroupId WAJIB benar (D-01); LinkedSessionId = fidelitas (D-02).
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
public class InjectLinkPrePostTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    public InjectLinkPrePostTests(InjectAssessmentFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // Real GradingService — SALIN VERBATIM dari InjectAssessmentServiceTests:69-77.
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

    // ---- Seed helpers ----

    // Worker dengan NIP (inject resolve by NIP). UserName/Email unik via Guid.
    private static async Task<(string userId, string nip)> SeedUserAsync(ApplicationDbContext ctx, string nipPrefix)
    {
        var nip = nipPrefix + "-" + Guid.NewGuid().ToString("N")[..6];
        var u = new ApplicationUser
        {
            UserName = "inj-" + Guid.NewGuid().ToString("N")[..8],
            Email = $"inj-{Guid.NewGuid().ToString("N")[..8]}@test.local",
            FullName = "Link Test " + nip,
            NIP = nip
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return (u.Id, nip);
    }

    // Seed satu sesi ONLINE Completed (Score set) dengan tipe + grouping tertentu.
    // linkedGroupId null = standalone (Kasus B siap); non-null = grouped (Kasus A siap).
    private static async Task<int> SeedOnlineSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string category, string assessmentType,
        int score, int? linkedGroupId, int? linkedSessionId, DateTime completedAt)
    {
        var s = new AssessmentSession
        {
            UserId = userId,
            Title = title,
            Category = category,
            AssessmentType = assessmentType,
            IsManualEntry = false,                 // ONLINE asli
            AccessToken = "ONLINE",
            IsTokenRequired = false,
            Status = AssessmentConstants.AssessmentStatus.Completed,
            Score = score,
            IsPassed = score >= 70,
            PassPercentage = 70,
            DurationMinutes = 60,
            LinkedGroupId = linkedGroupId,
            LinkedSessionId = linkedSessionId,
            Schedule = completedAt,
            StartedAt = completedAt,
            CompletedAt = completedAt,
            Progress = 100,
            AllowAnswerReview = true,
            CreatedAt = DateTime.UtcNow
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    // 1 soal MC (opsi benar=TempId 1) — paket inject minimal.
    private static List<InjectQuestionSpec> OneMcQuestion() => new()
    {
        new() { TempId = 1, Order = 1, QuestionType = "MultipleChoice", ScoreValue = 10, ElemenTeknis = "ET-A", QuestionText = "MC",
            Options = new() { new() { TempId = 1, OptionText = "benar", IsCorrect = true }, new() { TempId = 2, OptionText = "salah", IsCorrect = false } } }
    };

    private static InjectWorkerSpec McWorker(string nip, bool correct = true) => new()
    {
        Nip = nip,
        Answers = new() { new() { QuestionTempId = 1, SelectedOptionTempIds = new() { correct ? 1 : 2 } } }
    };

    // ── PerWorker_NoBroadcast: 2 pekerja inject Pre tertaut ke grup yang punya 2 sibling Post (1 per UserId).
    //    Tiap sesi inject.LinkedSessionId == sibling Post UserId-nya SENDIRI (BUKAN identik antar-pekerja). ──
    [Fact]
    public async Task PerWorker_NoBroadcast_LinkedSessionIdDistinctByUserId()
    {
        var (uidA, nipA) = (default(string), default(string));
        var (uidB, nipB) = (default(string), default(string));
        int repPostId; int postAId, postBId;
        await using (var seed = NewCtx())
        {
            (uidA, nipA) = await SeedUserAsync(seed, "LNK-PWA");
            (uidB, nipB) = await SeedUserAsync(seed, "LNK-PWB");
            var dt = new DateTime(2026, 3, 1);
            // Grup Post ONLINE ber-grup (Kasus A): 2 sesi PostTest, LinkedGroupId = rep (post A) Id.
            postAId = await SeedOnlineSessionAsync(seed, uidA!, "Post Linkgrp PW", "IHT", "PostTest", 80, null, null, dt);
            repPostId = postAId;
            // jadikan grouped: LinkedGroupId = repPostId untuk semua sesi room
            postBId = await SeedOnlineSessionAsync(seed, uidB!, "Post Linkgrp PW", "IHT", "PostTest", 90, repPostId, null, dt);
            var pa = await seed.AssessmentSessions.FindAsync(postAId);
            pa!.LinkedGroupId = repPostId; await seed.SaveChangesAsync();
        }

        var title = "Inject Pre PW " + Guid.NewGuid().ToString("N")[..6];
        var req = new InjectRequest
        {
            Title = title, Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 1), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId,                          // 397: server resolve LinkedGroupId/LinkedSessionId
            Questions = OneMcQuestion(),
            Workers = new() { McWorker(nipA!), McWorker(nipB!) }
        };

        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(result.Success, result.Message);
        Assert.Equal(2, result.SuccessSessionIds.Count);

        await using var verify = NewCtx();
        var injects = await verify.AssessmentSessions
            .Where(s => result.SuccessSessionIds.Contains(s.Id)).ToListAsync();
        var injA = injects.First(s => s.UserId == uidA);
        var injB = injects.First(s => s.UserId == uidB);

        // Anti-broadcast: tiap inject menunjuk sibling UserId-nya SENDIRI, BUKAN nilai identik.
        Assert.NotEqual(injA.LinkedSessionId, injB.LinkedSessionId);
        Assert.Equal(postAId, injA.LinkedSessionId);
        Assert.Equal(postBId, injB.LinkedSessionId);

        // LinkedGroupId = grup target (Kasus A adopt repPostId) untuk keduanya.
        Assert.Equal(repPostId, injA.LinkedGroupId);
        Assert.Equal(repPostId, injB.LinkedGroupId);

        // Bidirectional write-back: sibling online menunjuk balik ke sesi inject.
        var postA = await verify.AssessmentSessions.FindAsync(postAId);
        var postB = await verify.AssessmentSessions.FindAsync(postBId);
        Assert.Equal(injA.Id, postA!.LinkedSessionId);
        Assert.Equal(injB.Id, postB!.LinkedSessionId);
    }

    // ── KasusA_Adopt_OnlineUntouched: target sudah ber-grup → inject ADOPT LinkedGroupId,
    //    skor/lulus/status/responses online TIDAK berubah. ──
    [Fact]
    public async Task KasusA_Adopt_OnlineScoreStatusUnchanged()
    {
        string uid, nip; int repPostId; int onlineScoreBefore = 85;
        await using (var seed = NewCtx())
        {
            (uid, nip) = await SeedUserAsync(seed, "LNK-KA");
            var dt = new DateTime(2026, 3, 5);
            repPostId = await SeedOnlineSessionAsync(seed, uid, "Post KasusA", "IHT", "PostTest", onlineScoreBefore, null, null, dt);
            var p = await seed.AssessmentSessions.FindAsync(repPostId);
            p!.LinkedGroupId = repPostId; await seed.SaveChangesAsync();   // grouped (Kasus A)
        }

        // snapshot online sebelum
        string statusBefore; bool? passedBefore; int respBefore;
        await using (var pre = NewCtx())
        {
            var p = await pre.AssessmentSessions.FindAsync(repPostId);
            statusBefore = p!.Status; passedBefore = p.IsPassed;
            respBefore = await pre.PackageUserResponses.CountAsync(r => r.AssessmentSessionId == repPostId);
        }

        var req = new InjectRequest
        {
            Title = "Inject Pre KA " + Guid.NewGuid().ToString("N")[..6], Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 5), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(), Workers = new() { McWorker(nip) }
        };
        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(result.Success, result.Message);

        await using var verify = NewCtx();
        var inj = await verify.AssessmentSessions.FindAsync(result.SuccessSessionIds[0]);
        Assert.Equal(repPostId, inj!.LinkedGroupId);   // ADOPT

        // Online UNCHANGED — Kasus A tak menyentuh data online.
        var post = await verify.AssessmentSessions.FindAsync(repPostId);
        Assert.Equal(onlineScoreBefore, post!.Score);
        Assert.Equal(passedBefore, post.IsPassed);
        Assert.Equal(statusBefore, post.Status);
        Assert.Equal(repPostId, post.LinkedGroupId);   // grup online tak berubah
        Assert.Equal(respBefore, await verify.PackageUserResponses.CountAsync(r => r.AssessmentSessionId == repPostId));

        // Kasus A → TIDAK ada audit "LinkPrePost" (online grouping tak disentuh, hanya bidirectional sibling).
        // (bidirectional write-back boleh tulis LinkedSessionId; audit "LinkPrePost" khusus stiker grup Kasus B).
    }

    // ── KasusB_WriteSticker_AllTargetSessions: target standalone → resolvedGroupId == RepresentativeId,
    //    SEMUA sesi room target dapat LinkedGroupId (bukan hanya yang ter-pair, Pitfall 2); online skor/status UNCHANGED;
    //    audit "LinkPrePost" per sesi online dimutasi (D-09). ──
    [Fact]
    public async Task KasusB_WriteSticker_AllTargetSessions_AuditPerMutated()
    {
        string uidPaired, nipPaired, uidUnpaired, nipPairedWorker; int repPostId; int postPairedId, postOtherId;
        await using (var seed = NewCtx())
        {
            (uidPaired, nipPaired) = await SeedUserAsync(seed, "LNK-KBP");
            (uidUnpaired, _) = await SeedUserAsync(seed, "LNK-KBU");
            var dt = new DateTime(2026, 3, 8);
            // Room Post STANDALONE (LinkedGroupId == null): 2 sesi, 1 akan ter-pair (uidPaired), 1 tidak.
            postPairedId = await SeedOnlineSessionAsync(seed, uidPaired, "Post KasusB", "IHT", "PostTest", 75, null, null, dt);
            postOtherId = await SeedOnlineSessionAsync(seed, uidUnpaired, "Post KasusB", "IHT", "PostTest", 60, null, null, dt);
            repPostId = postPairedId;   // RepresentativeId room target = sesi representatif
            nipPairedWorker = nipPaired;
        }

        var title = "Inject Pre KB " + Guid.NewGuid().ToString("N")[..6];
        var req = new InjectRequest
        {
            Title = title, Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 8), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(),
            Workers = new() { McWorker(nipPairedWorker) }   // hanya pekerja yang punya sibling
        };
        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(result.Success, result.Message);

        await using var verify = NewCtx();
        var inj = await verify.AssessmentSessions.FindAsync(result.SuccessSessionIds[0]);
        Assert.Equal(repPostId, inj!.LinkedGroupId);   // resolvedGroupId == RepresentativeId (konvensi :1270)

        // SEMUA sesi room target online dapat LinkedGroupId (Pitfall 2) — bukan hanya yang ter-pair.
        var postPaired = await verify.AssessmentSessions.FindAsync(postPairedId);
        var postOther = await verify.AssessmentSessions.FindAsync(postOtherId);
        Assert.Equal(repPostId, postPaired!.LinkedGroupId);
        Assert.Equal(repPostId, postOther!.LinkedGroupId);   // sesi yang TAK ter-pair pun dapat stiker grup

        // Online skor/status UNCHANGED (hanya kolom link disentuh).
        Assert.Equal(75, postPaired.Score);
        Assert.Equal(60, postOther.Score);
        Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, postPaired.Status);
        Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, postOther.Status);

        // Audit "LinkPrePost" per sesi online dimutasi (D-09) — minimal 2 (kedua sesi room target).
        var linkAudits = await verify.AuditLogs
            .Where(a => a.ActionType == "LinkPrePost"
                        && a.TargetId != null
                        && (a.TargetId == postPairedId || a.TargetId == postOtherId))
            .ToListAsync();
        Assert.Equal(2, linkAudits.Count);
        Assert.All(linkAudits, a => Assert.Equal("AssessmentSession", a.TargetType));
    }

    // ── AtomicRollback: error mid-batch (1 NIP invalid bercampur valid) → NO sesi inject persisted
    //    AND NO mutasi link online persisted (read-after-rollback null/original). ──
    [Fact]
    public async Task AtomicRollback_NoInjectSession_NoOnlineLinkMutation()
    {
        string uidValid, nipValid; int repPostId;
        await using (var seed = NewCtx())
        {
            (uidValid, nipValid) = await SeedUserAsync(seed, "LNK-RB");
            var dt = new DateTime(2026, 3, 9);
            // Room Post STANDALONE (Kasus B) — bila batch sukses akan ditulisi stiker; rollback harus batalkan ini.
            repPostId = await SeedOnlineSessionAsync(seed, uidValid, "Post Rollback", "IHT", "PostTest", 70, null, null, dt);
        }

        var title = "Inject Pre RB " + Guid.NewGuid().ToString("N")[..6];
        var req = new InjectRequest
        {
            Title = title, Category = "IHT", AssessmentType = "PreTest",
            CompletedAt = new DateTime(2026, 2, 9), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repPostId, Questions = OneMcQuestion(),
            Workers = new() { McWorker(nipValid), McWorker("NIP-TIDAK-ADA") }   // 1 invalid → reject-all
        };
        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");
        Assert.True(result.Rejected);
        Assert.False(result.Success);

        await using var verify = NewCtx();
        // NO sesi inject ter-tulis.
        Assert.Equal(0, await verify.AssessmentSessions.CountAsync(s => s.Title == title));
        // NO mutasi link online: room Post tetap standalone (LinkedGroupId null, original).
        var post = await verify.AssessmentSessions.FindAsync(repPostId);
        Assert.Null(post!.LinkedGroupId);
        Assert.Null(post.LinkedSessionId);
        // NO audit "LinkPrePost" untuk sesi ini.
        Assert.Equal(0, await verify.AuditLogs.CountAsync(a => a.ActionType == "LinkPrePost" && a.TargetId == repPostId));
    }
}
