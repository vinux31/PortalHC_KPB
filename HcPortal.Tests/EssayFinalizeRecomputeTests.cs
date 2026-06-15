// Phase 376 GRADE-01/02 — integration real-SQL untuk forward aggregation + RecomputeEssayScores.
// Pola disposable DB (RecordCascadeFixture): HcPortalDB_Test_{guid} @localhost\SQLEXPRESS, MigrateAsync, drop on dispose.
// [Trait Category=Integration] → skip via --filter "Category!=Integration". DB lokal HcPortalDB_Dev TAK tersentuh.
// Logika recompute/forward direplikasi di-level data PERSIS seperti AssessmentAdminController (reuse helper
// AssessmentScoreAggregator — D-02 single source of truth) supaya membuktikan behavior tanpa DI controller berat.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class EssayFinalizeRecomputeFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public EssayFinalizeRecomputeFixture()
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
                $"Phase 376 integration setup failed during MigrateAsync of disposable DB {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug grading. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class EssayFinalizeRecomputeTests : IClassFixture<EssayFinalizeRecomputeFixture>
{
    private readonly EssayFinalizeRecomputeFixture _fixture;
    public EssayFinalizeRecomputeTests(EssayFinalizeRecomputeFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "essay-" + Guid.NewGuid().ToString("N")[..8], Email = "essay@test.local", FullName = "Essay Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    /// <summary>Seed an essay-only session (1 Essay question) with a graded response + a UserPackageAssignment.</summary>
    private static async Task<int> SeedEssayOnlyAsync(ApplicationDbContext ctx, string userId,
        int? score, int essayScore, string status, int passPercentage = 70, int scoreValue = 100)
    {
        var session = new AssessmentSession
        {
            UserId = userId, Title = "Essay Exam", Category = "IHT", Status = status, AccessToken = "",
            Schedule = new DateTime(2026, 2, 1), Score = score, HasManualGrading = true,
            PassPercentage = passPercentage, NomorSertifikat = null, GenerateCertificate = true
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionType = "Essay", ScoreValue = scoreValue, Order = 1, QuestionText = "Jelaskan." };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();

        ctx.PackageUserResponses.Add(new PackageUserResponse { AssessmentSessionId = session.Id, PackageQuestionId = q.Id, EssayScore = essayScore });
        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = session.Id, AssessmentPackageId = pkg.Id, UserId = userId,
            ShuffledQuestionIds = JsonSerializer.Serialize(new List<int> { q.Id })
        });
        await ctx.SaveChangesAsync();
        return session.Id;
    }

    // Mirror of FinalizeEssayGrading derivation + helper (forward path, Plan 02).
    private static async Task<ScoreAggregateResult> ForwardAggregateAsync(ApplicationDbContext ctx, int sessionId)
    {
        var session = await ctx.AssessmentSessions.FindAsync(sessionId);
        var pa = await ctx.UserPackageAssignments.FirstAsync(a => a.AssessmentSessionId == sessionId);
        var shuffledIds = pa.GetShuffledQuestionIds();
        var allQuestions = shuffledIds.Count > 0
            ? await ctx.PackageQuestions.Include(q => q.Options).Where(q => shuffledIds.Contains(q.Id)).ToListAsync()
            : await ctx.PackageQuestions.Include(q => q.Options)
                .Where(q => ctx.PackageUserResponses.Where(r => r.AssessmentSessionId == sessionId).Select(r => r.PackageQuestionId).Contains(q.Id))
                .ToListAsync();
        var allResponses = await ctx.PackageUserResponses.Where(r => r.AssessmentSessionId == sessionId).ToListAsync();
        return AssessmentScoreAggregator.Compute(allQuestions, allResponses, session!.PassPercentage);
    }

    // Mirror of RecomputeEssayScores action core (Plan 03 Task 1). Returns (repaired, skipped, alreadyOk).
    private static async Task<(int repaired, int skipped, int alreadyOk)> RunRecomputeAsync(ApplicationDbContext ctx)
    {
        int repaired = 0, skipped = 0, alreadyOk = 0;
        var candidateIds = await ctx.AssessmentSessions
            .Where(s => s.Status == AssessmentConstants.AssessmentStatus.Completed
                     && s.HasManualGrading
                     && (s.Score == null || s.Score == 0))
            .Select(s => s.Id).ToListAsync();

        foreach (var candId in candidateIds)
        {
            var session = await ctx.AssessmentSessions.FindAsync(candId);
            if (session == null) { skipped++; continue; }
            var pa = await ctx.UserPackageAssignments.FirstOrDefaultAsync(a => a.AssessmentSessionId == candId);
            if (pa == null) { skipped++; continue; }
            var shuffledIds = pa.GetShuffledQuestionIds();
            var allQuestions = shuffledIds.Count > 0
                ? await ctx.PackageQuestions.Include(q => q.Options).Where(q => shuffledIds.Contains(q.Id)).ToListAsync()
                : await ctx.PackageQuestions.Include(q => q.Options)
                    .Where(q => ctx.PackageUserResponses.Where(r => r.AssessmentSessionId == candId).Select(r => r.PackageQuestionId).Contains(q.Id))
                    .ToListAsync();
            var allResponses = await ctx.PackageUserResponses.Where(r => r.AssessmentSessionId == candId).ToListAsync();
            var essayQIds = allQuestions.Where(q => (q.QuestionType ?? "MultipleChoice") == "Essay").Select(q => q.Id).ToHashSet();
            if (essayQIds.Count > 0 && allResponses.Any(r => essayQIds.Contains(r.PackageQuestionId) && r.EssayScore == null)) { skipped++; continue; }
            var agg = AssessmentScoreAggregator.Compute(allQuestions, allResponses, session.PassPercentage);
            if (agg.MaxScore == 0) { skipped++; continue; }
            var rows = await ctx.AssessmentSessions
                .Where(s => s.Id == candId && (s.Score == null || s.Score == 0))
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Score, agg.Percentage).SetProperty(r => r.IsPassed, agg.IsPassed));
            if (rows > 0) repaired++; else alreadyOk++;
        }
        return (repaired, skipped, alreadyOk);
    }

    // ---- ECG-06 Plan 04: mirror SubmitEssayScore core (AssessmentAdminController.cs:3460-3477) ----
    // Mirror data-level (precedent file ini — hindari ctor 12-dep controller). Lock: persist EssayScore +
    // range guard `score < 0 || score > question.ScoreValue` (controller L3472). Returns (success, message).
    // Drift-guard: bila body controller berubah, test ini harus diperbarui agar tetap mencerminkan L3460-3477.
    private static async Task<(bool success, string? message)> MirrorSubmitEssayScoreAsync(
        ApplicationDbContext ctx, int sessionId, int questionId, int score)
    {
        // 1. Load response (mirror L3461-3464)
        var response = await ctx.PackageUserResponses
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
        if (response == null) return (false, "Jawaban tidak ditemukan");
        // 2. Load question untuk validasi ScoreValue (mirror L3467-3469)
        var question = await ctx.PackageQuestions.FindAsync(questionId);
        if (question == null) return (false, "Soal tidak ditemukan");
        // 3. Range guard (mirror L3472)
        if (score < 0 || score > question.ScoreValue) return (false, $"Skor harus antara 0 dan {question.ScoreValue}");
        // 4. Persist EssayScore (mirror L3476-3477)
        response.EssayScore = score;
        await ctx.SaveChangesAsync();
        return (true, null);
    }

    // Ambil 1 PackageQuestion milik session tertentu (fixture = shared DB; FirstAsync global tak aman).
    private static async Task<PackageQuestion> QuestionOfSessionAsync(ApplicationDbContext ctx, int sessionId)
    {
        var pkgIds = await ctx.AssessmentPackages
            .Where(p => p.AssessmentSessionId == sessionId).Select(p => p.Id).ToListAsync();
        return await ctx.PackageQuestions.FirstAsync(q => pkgIds.Contains(q.AssessmentPackageId));
    }

    // ---- ECG-06: SubmitEssayScore persist EssayScore saat skor dalam range (lock L3476-3477) ----
    [Fact]
    public async Task SubmitEssayScore_Persists_WhenInRange()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedEssayOnlyAsync(ctx, userId, score: 0, essayScore: 0,
            status: AssessmentConstants.AssessmentStatus.PendingGrading, scoreValue: 100);
        // scope ke session ini (fixture shared DB — jangan ambil FirstAsync global)
        var q = await QuestionOfSessionAsync(ctx, sessionId);

        var (ok, _) = await MirrorSubmitEssayScoreAsync(ctx, sessionId, q.Id, 80);

        Assert.True(ok);
        await using var verify = NewCtx();
        var resp = await verify.PackageUserResponses.FirstAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == q.Id);
        Assert.Equal(80, resp.EssayScore);   // persisted ke DB
    }

    // ---- ECG-06: range guard menolak skor di luar 0..ScoreValue (lock L3472, T-298-13 / V5 ASVS) ----
    [Fact]
    public async Task SubmitEssayScore_Rejects_WhenOutOfRange()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedEssayOnlyAsync(ctx, userId, score: 0, essayScore: 0,
            status: AssessmentConstants.AssessmentStatus.PendingGrading, scoreValue: 100);
        // scope ke session ini (fixture shared DB — jangan ambil FirstAsync global)
        var q = await QuestionOfSessionAsync(ctx, sessionId);

        var (okHigh, _) = await MirrorSubmitEssayScoreAsync(ctx, sessionId, q.Id, 150);  // > ScoreValue
        var (okNeg, _)  = await MirrorSubmitEssayScoreAsync(ctx, sessionId, q.Id, -5);   // < 0

        Assert.False(okHigh);
        Assert.False(okNeg);
        // tak boleh ter-persist: EssayScore tetap nilai awal (0), bukan 150/-5
        await using var verify = NewCtx();
        var resp = await verify.PackageUserResponses.FirstAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == q.Id);
        Assert.Equal(0, resp.EssayScore);
    }

    // ---- GRADE-02: forward aggregation essay-only → Score ≠ 0 ----
    [Fact]
    public async Task Forward_EssayOnly_ScoreNotZero()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var sessionId = await SeedEssayOnlyAsync(ctx, userId, score: 0, essayScore: 80, status: AssessmentConstants.AssessmentStatus.PendingGrading);

        var agg = await ForwardAggregateAsync(ctx, sessionId);

        Assert.Equal(100, agg.MaxScore);
        Assert.Equal(80, agg.Percentage);   // bukan 0
        Assert.True(agg.IsPassed);
    }

    // ---- D-02/D-07: recompute idempotent + hanya sentuh Score=0/null, baris lain untouched ----
    [Fact]
    public async Task Recompute_Idempotent_OnlyTouchesScoreZero()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // broken historis: Completed, HasManualGrading, Score=0, EssayScore=80
        var brokenId = await SeedEssayOnlyAsync(ctx, userId, score: 0, essayScore: 80, status: AssessmentConstants.AssessmentStatus.Completed);
        // control: sudah benar Score=90 → BUKAN kandidat, harus untouched
        var controlId = await SeedEssayOnlyAsync(ctx, userId, score: 90, essayScore: 90, status: AssessmentConstants.AssessmentStatus.Completed);

        var (repaired1, _, _) = await RunRecomputeAsync(ctx);
        Assert.True(repaired1 >= 1);

        await using (var verify = NewCtx())
        {
            Assert.Equal(80, (await verify.AssessmentSessions.FindAsync(brokenId))!.Score);   // diperbaiki
            Assert.Equal(90, (await verify.AssessmentSessions.FindAsync(controlId))!.Score);  // untouched
        }

        // run kedua → idempotent: broken sudah 80 (bukan 0/null) → tak lagi kandidat → tak berubah
        var (repaired2, _, _) = await RunRecomputeAsync(ctx);
        await using (var verify2 = NewCtx())
        {
            Assert.Equal(80, (await verify2.AssessmentSessions.FindAsync(brokenId))!.Score);
            Assert.Equal(90, (await verify2.AssessmentSessions.FindAsync(controlId))!.Score);
        }
        Assert.Equal(0, repaired2);   // tak ada yang diperbaiki run kedua (idempotent)
    }

    // ---- D-03: recompute set Score+IsPassed ONLY — no cert/Proton/TR, Status unchanged ----
    [Fact]
    public async Task Recompute_NoSideEffects_NoCertNoProtonNoTR()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var brokenId = await SeedEssayOnlyAsync(ctx, userId, score: 0, essayScore: 80, status: AssessmentConstants.AssessmentStatus.Completed);

        await RunRecomputeAsync(ctx);

        await using var verify = NewCtx();
        var s = (await verify.AssessmentSessions.FindAsync(brokenId))!;
        Assert.Equal(80, s.Score);
        Assert.True(s.IsPassed);                                       // di-set
        Assert.Null(s.NomorSertifikat);                               // NO cert retroaktif (D-03)
        Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, s.Status);  // Status TIDAK berubah
        var trCount = await verify.TrainingRecords.CountAsync(t => t.UserId == userId);
        Assert.Equal(0, trCount);                                     // NO TrainingRecord baru (D-03)
    }
}
