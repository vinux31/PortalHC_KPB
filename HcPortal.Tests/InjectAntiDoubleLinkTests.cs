// Phase 397 INJ-12 / D-08 — RED (TDD Wave 0). Mengunci anti-dobel-link per-pekerja:
// bila pekerja sudah punya sibling TIPE-SAMA di grup target, tolak pekerja itu — daftar LENGKAP
// (no early-return, pola 396 D-09).
//
// ⚠ RED until Wave 1 (397-02) adds anti-double-link preflight (by UserId+LinkedGroupId+AssessmentType)
//   ke PreflightValidateAsync, dipicu oleh req.LinkTargetRepId. Restoring fast suite + integration green = Wave 1 gate.
//   Pada Wave 0 perilaku ini belum diimplementasikan → tes FAIL (bukan compile-error).
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
public class InjectAntiDoubleLinkTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    public InjectAntiDoubleLinkTests(InjectAssessmentFixture fixture) => _fixture = fixture;

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
            FullName = "AntiDouble Test " + nip,
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

    // ── SameTypeSibling_RejectFullList: 2 pekerja (X & Y) SUDAH punya Pre di grup target;
    //    inject Pre untuk X+Y (+ 1 pekerja bersih Z) → reject (atau X,Y di PerRowErrors),
    //    PerRowErrors mendaftar SEMUA offending (X & Y) — bukan early-return di pekerja pertama.
    //    Pesan Bahasa Indonesia memuat NIP. ──
    [Fact]
    public async Task SameTypeSibling_RejectFullList_AllOffendingWorkers()
    {
        string uidX, nipX, uidY, nipY, uidZ, nipZ; int repId;
        await using (var seed = NewCtx())
        {
            (uidX, nipX) = await SeedUserAsync(seed, "ADL-X");
            (uidY, nipY) = await SeedUserAsync(seed, "ADL-Y");
            (uidZ, nipZ) = await SeedUserAsync(seed, "ADL-Z");
            var dt = new DateTime(2026, 3, 10);
            // Grup target (Pre+Post): X & Y SUDAH punya Pre di grup; Z belum.
            // RepresentativeId room = sesi Post representatif. Grup = repId.
            var postRep = await SeedOnlineSessionAsync(seed, uidX, "Grp AntiDouble", "PostTest", 80, null, dt);
            repId = postRep;
            var post = await seed.AssessmentSessions.FindAsync(postRep);
            post!.LinkedGroupId = repId; await seed.SaveChangesAsync();
            // X & Y punya Pre tipe-SAMA (dengan yang akan di-inject = PreTest) di grup repId.
            await SeedOnlineSessionAsync(seed, uidX, "Grp AntiDouble Pre", "PreTest", 50, repId, dt);
            await SeedOnlineSessionAsync(seed, uidY, "Grp AntiDouble Pre", "PreTest", 55, repId, dt);
        }

        var title = "Inject Pre ADL " + Guid.NewGuid().ToString("N")[..6];
        var req = new InjectRequest
        {
            Title = title, Category = "IHT", AssessmentType = "PreTest",   // tipe SAMA dengan Pre existing X & Y
            CompletedAt = new DateTime(2026, 2, 10), PassPercentage = 70, CertMode = InjectCertMode.None,
            LinkTargetRepId = repId, Questions = OneMcQuestion(),
            Workers = new() { McWorker(nipX), McWorker(nipY), McWorker(nipZ) }
        };
        var result = await NewInjectService(NewCtx()).InjectBatchAsync(req, "actor", "Actor");

        // Default BLOK (UI-SPEC N4) — batch reject; PerRowErrors mendaftar SEMUA offending (X & Y), tidak early-return.
        Assert.True(result.Rejected);
        Assert.False(result.Success);
        Assert.Contains(result.PerRowErrors, e => e.Nip == nipX);
        Assert.Contains(result.PerRowErrors, e => e.Nip == nipY);
        // ≥2 offending → daftar lengkap (no early-return). Z bersih tidak harus di daftar.
        Assert.True(result.PerRowErrors.Count(e => e.Nip == nipX || e.Nip == nipY) >= 2,
            "PerRowErrors harus mendaftar KEDUA pekerja offending (X & Y), bukan early-return di pertama");
        // Pesan Bahasa Indonesia memuat NIP offending.
        Assert.Contains(result.PerRowErrors, e => e.Message.Contains(nipX));

        // 0 tulisan (reject-all, atomic).
        await using var verify = NewCtx();
        Assert.Equal(0, await verify.AssessmentSessions.CountAsync(s => s.Title == title));
    }
}
