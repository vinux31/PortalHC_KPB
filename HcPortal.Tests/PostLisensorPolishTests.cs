// Phase 387 Plan 04 (D-09 proportional verification) — unit data-level tests untuk 3 fix logic-bearing:
//   PXF-06 (REDIRECTED, lihat 387-01-SUMMARY): SubmitEssayScore WR-01 (type-guard) + WR-02 (ownership-guard)
//     + regression status-guard 386 D-08 (Completed/non-PendingGrading tetap ditolak).
//   PXF-09: Excel BulkExport "Detail Jawaban" essay cell → TextAnswer + "Skor: x/y" / "Belum dinilai".
//   PXF-12: SubmitExam MC upsert guarded by answers.ContainsKey(q.Id) — soal absent TIDAK null-overwrite.
//
// PENDEKATAN (verbatim pola SubmitResurrectionTests / EssayFinalizeRecomputeTests):
//   Guard/cell-logic hidup di dalam controller method yang berat di-instantiate (DI penuh, antiforgery,
//   IWebHostEnvironment, dst.). Maka logika KEPUTUSAN direplikasi di-level data PERSIS seperti controller
//   (urutan guard byte-identik dgn AssessmentAdminController.SubmitEssayScore L3536-3583 + Excel branch
//   L4908-4919 + CMPController.SubmitExam MC upsert L1712-1718). Test menyentuh DB disposable
//   HcPortalDB_Test_{guid} @ localhost\SQLEXPRESS — HcPortalDB_Dev TIDAK tersentuh ([Trait Integration]).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

public class PostLisensorPolishFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public PostLisensorPolishFixture()
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
                $"Phase 387 PostLisensorPolish setup failed during MigrateAsync of disposable DB {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug fix. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class PostLisensorPolishTests : IClassFixture<PostLisensorPolishFixture>
{
    private readonly PostLisensorPolishFixture _fixture;
    public PostLisensorPolishTests(PostLisensorPolishFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "pxf-" + Guid.NewGuid().ToString("N")[..8], Email = "pxf@test.local", FullName = "Polish Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    /// <summary>
    /// Seed 1 sesi + 1 paket + soal-soal (per spec) + opsi. Return (sessionId, packageId).
    /// Status diparametrisasi agar test status-guard (Completed vs PendingGrading) bisa drive cabang berbeda.
    /// </summary>
    private static async Task<(int sessionId, int packageId)> SeedSessionAsync(ApplicationDbContext ctx, string userId, string status)
    {
        var session = new AssessmentSession
        {
            UserId = userId, Title = "Polish Exam", Category = "OJT", Status = status, AccessToken = "",
            Schedule = new DateTime(2026, 6, 16), PassPercentage = 70, GenerateCertificate = false, Score = null, IsPassed = null
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        return (session.Id, pkg.Id);
    }

    private static async Task<int> SeedQuestionAsync(ApplicationDbContext ctx, int packageId, string type, int scoreValue, int order, string text)
    {
        var q = new PackageQuestion { AssessmentPackageId = packageId, QuestionType = type, ScoreValue = scoreValue, Order = order, QuestionText = text };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();
        return q.Id;
    }

    // ============================================================================================
    // PXF-06 (REDIRECTED) — SubmitEssayScore guard chain.
    // Replikasi PERSIS urutan guard AssessmentAdminController.SubmitEssayScore (L3536-3583):
    //   1. session null            → "Session tidak ditemukan"
    //   2. Status != PendingGrading → "Penilaian hanya bisa dilakukan saat status Menunggu Penilaian." (386 D-08)
    //   3. question null            → "Soal tidak ditemukan"
    //   4. score out of range       → "Skor harus antara 0 dan {ScoreValue}"
    //   5. WR-01 type-guard         → "Soal ini bukan tipe Essay."          (387-01)
    //   6. WR-02 ownership-guard    → "Soal bukan milik sesi ini."          (387-01)
    //   7. UPSERT EssayScore
    // ============================================================================================

    /// <summary>Hasil keputusan SubmitEssayScore — mirror Json(new { success, message }) controller.</summary>
    private readonly record struct EssayScoreResult(bool Success, string Message);

    /// <summary>
    /// Replikasi keputusan + side-effect SubmitEssayScore PERSIS (urutan guard byte-identik controller).
    /// Mengembalikan reject pertama yang kena; bila lolos semua guard → upsert EssayScore lalu success.
    /// </summary>
    private static async Task<EssayScoreResult> RunSubmitEssayScoreLogicAsync(ApplicationDbContext ctx, int sessionId, int questionId, int score)
    {
        // 1. STATUS-GUARD (386 D-08)
        var session = await ctx.AssessmentSessions.FindAsync(sessionId);
        if (session == null)
            return new(false, "Session tidak ditemukan");
        if (session.Status != S.PendingGrading)
            return new(false, "Penilaian hanya bisa dilakukan saat status Menunggu Penilaian.");

        // 2. load question + range
        var question = await ctx.PackageQuestions.FindAsync(questionId);
        if (question == null)
            return new(false, "Soal tidak ditemukan");
        if (score < 0 || score > question.ScoreValue)
            return new(false, $"Skor harus antara 0 dan {question.ScoreValue}");

        // 2b. WR-01 — questionId WAJIB tipe Essay (387-01)
        if (question.QuestionType != "Essay")
            return new(false, "Soal ini bukan tipe Essay.");

        // 2c. WR-02 — questionId WAJIB milik sesi ini (387-01)
        var ownsQuestion = await ctx.PackageQuestions
            .AnyAsync(q => q.Id == questionId && q.AssessmentPackage.AssessmentSessionId == sessionId);
        if (!ownsQuestion)
            return new(false, "Soal bukan milik sesi ini.");

        // 3. UPSERT EssayScore
        var response = await ctx.PackageUserResponses
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
        if (response == null)
        {
            response = new PackageUserResponse
            {
                AssessmentSessionId = sessionId, PackageQuestionId = questionId,
                PackageOptionId = null, TextAnswer = null, EssayScore = score
            };
            ctx.PackageUserResponses.Add(response);
        }
        else
        {
            response.EssayScore = score;
        }
        await ctx.SaveChangesAsync();
        return new(true, "");
    }

    [Fact]
    public async Task SubmitEssayScore_NonEssayQuestion_RejectedAndNoScoreWritten()  // WR-01
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var (sessionId, pkgId) = await SeedSessionAsync(ctx, userId, S.PendingGrading);
        // Soal MC (BUKAN essay) — milik sesi.
        var mcQ = await SeedQuestionAsync(ctx, pkgId, "MultipleChoice", 10, 1, "Soal MC.");

        var result = await RunSubmitEssayScoreLogicAsync(ctx, sessionId, mcQ, 8);

        Assert.False(result.Success);
        Assert.Equal("Soal ini bukan tipe Essay.", result.Message);
        // Tidak ada baris EssayScore tertulis untuk soal MC.
        await using var verify = NewCtx();
        var row = await verify.PackageUserResponses
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == mcQ);
        Assert.Null(row);  // upsert tak pernah jalan (guard kena di WR-01)
    }

    [Fact]
    public async Task SubmitEssayScore_CrossSessionQuestion_RejectedAndNoScoreWritten()  // WR-02
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var (sessionA, pkgA) = await SeedSessionAsync(ctx, userId, S.PendingGrading);
        var (sessionB, pkgB) = await SeedSessionAsync(ctx, userId, S.PendingGrading);
        // Soal essay milik sesi B, tapi SubmitEssayScore dipanggil dgn sessionId = A.
        var essayB = await SeedQuestionAsync(ctx, pkgB, "Essay", 10, 1, "Essay milik B.");

        var result = await RunSubmitEssayScoreLogicAsync(ctx, sessionA, essayB, 7);

        Assert.False(result.Success);
        Assert.Equal("Soal bukan milik sesi ini.", result.Message);
        await using var verify = NewCtx();
        var row = await verify.PackageUserResponses
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionA && r.PackageQuestionId == essayB);
        Assert.Null(row);  // tak ada baris cross-session tertulis
    }

    [Fact]
    public async Task SubmitEssayScore_CompletedSession_RejectedByStatusGuard()  // 386 D-08 regression (anti-tamper intent PXF-06)
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var (sessionId, pkgId) = await SeedSessionAsync(ctx, userId, S.Completed);
        var essayQ = await SeedQuestionAsync(ctx, pkgId, "Essay", 10, 1, "Essay valid.");
        // Seed baris essay existing dgn skor awal 3 — harus TIDAK berubah karena status-guard menolak.
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = sessionId, PackageQuestionId = essayQ, TextAnswer = "jawaban peserta", EssayScore = 3 });
        await ctx.SaveChangesAsync();

        var result = await RunSubmitEssayScoreLogicAsync(ctx, sessionId, essayQ, 9);

        Assert.False(result.Success);
        Assert.Equal("Penilaian hanya bisa dilakukan saat status Menunggu Penilaian.", result.Message);
        await using var verify = NewCtx();
        var row = await verify.PackageUserResponses
            .FirstAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == essayQ);
        Assert.Equal(3, row.EssayScore);  // skor lama UNCHANGED (edit pasca-finalize ditolak)
    }

    [Fact]
    public async Task SubmitEssayScore_PendingGradingValidEssay_SavesScore()  // happy path
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var (sessionId, pkgId) = await SeedSessionAsync(ctx, userId, S.PendingGrading);
        var essayQ = await SeedQuestionAsync(ctx, pkgId, "Essay", 10, 1, "Essay valid.");
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = sessionId, PackageQuestionId = essayQ, TextAnswer = "jawaban peserta", EssayScore = null });
        await ctx.SaveChangesAsync();

        var result = await RunSubmitEssayScoreLogicAsync(ctx, sessionId, essayQ, 8);

        Assert.True(result.Success);
        await using var verify = NewCtx();
        var row = await verify.PackageUserResponses
            .FirstAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == essayQ);
        Assert.Equal(8, row.EssayScore);  // skor tertulis
    }

    // ============================================================================================
    // PXF-09 — Excel BulkExport "Detail Jawaban" essay cell.
    // Replikasi PERSIS ekspresi AssessmentAdminController BulkExport branch (L4915 + L4917-4919):
    //   col4 = IsNullOrWhiteSpace(essayResp?.TextAnswer) ? "Tidak dijawab" : essayResp.TextAnswer
    //   col6 = essayResp?.EssayScore.HasValue == true ? $"Skor: {EssayScore}/{ScoreValue}" : "Belum dinilai"
    // ============================================================================================

    private static string EssayCellAnswer(PackageUserResponse? essayResp) =>
        string.IsNullOrWhiteSpace(essayResp?.TextAnswer) ? "Tidak dijawab" : essayResp!.TextAnswer!;

    private static string EssayCellScore(PackageUserResponse? essayResp, int scoreValue) =>
        essayResp?.EssayScore.HasValue == true
            ? $"Skor: {essayResp.EssayScore}/{scoreValue}"
            : "Belum dinilai";

    [Fact]
    public async Task EssayCell_GradedAnswer_ShowsTextAndScore()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var (sessionId, pkgId) = await SeedSessionAsync(ctx, userId, S.Completed);
        var essayQ = await SeedQuestionAsync(ctx, pkgId, "Essay", 10, 1, "Jelaskan proses.");
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = sessionId, PackageQuestionId = essayQ, TextAnswer = "jawaban X", EssayScore = 8 });
        await ctx.SaveChangesAsync();

        await using var verify = NewCtx();
        var q = await verify.PackageQuestions.FindAsync(essayQ);
        var essayResp = await verify.PackageUserResponses
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == essayQ);

        Assert.Equal("jawaban X", EssayCellAnswer(essayResp));        // col4 = teks peserta (bukan "—")
        Assert.Equal("Skor: 8/10", EssayCellScore(essayResp, q!.ScoreValue));  // col6 = skor nyata
    }

    [Fact]
    public async Task EssayCell_BlankAnswer_ShowsTidakDijawab()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var (sessionId, pkgId) = await SeedSessionAsync(ctx, userId, S.PendingGrading);
        var essayQ = await SeedQuestionAsync(ctx, pkgId, "Essay", 10, 1, "Jelaskan proses.");
        // TextAnswer whitespace-only + EssayScore null → "Tidak dijawab" + "Belum dinilai"
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = sessionId, PackageQuestionId = essayQ, TextAnswer = "   ", EssayScore = null });
        await ctx.SaveChangesAsync();

        await using var verify = NewCtx();
        var q = await verify.PackageQuestions.FindAsync(essayQ);
        var essayResp = await verify.PackageUserResponses
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == essayQ);

        Assert.Equal("Tidak dijawab", EssayCellAnswer(essayResp));     // blank → "Tidak dijawab"
        Assert.Equal("Belum dinilai", EssayCellScore(essayResp, q!.ScoreValue));  // EssayScore null → "Belum dinilai"
    }

    // ============================================================================================
    // PXF-12 — SubmitExam MC upsert no null-overwrite.
    // Replikasi PERSIS guard CMPController.SubmitExam MC upsert (L1712-1718):
    //   if (existingResponses.TryGetValue(q.Id, out var existingResponse)) {
    //       if (answers.ContainsKey(q.Id)) {  // guard: jangan null-overwrite soal absent di form
    //           existingResponse.PackageOptionId = selectedOptId; existingResponse.SubmittedAt = ...
    //       }
    //   }
    // ============================================================================================

    /// <summary>
    /// Replikasi upsert MC PERSIS controller terhadap existing PackageUserResponse di DB. `answers` =
    /// dict form POST (qId → optId). Saat key absent → BIARKAN baris tersimpan (guard ContainsKey).
    /// </summary>
    private static async Task RunMcUpsertLogicAsync(ApplicationDbContext ctx, int sessionId, int questionId, Dictionary<int, int?> answers)
    {
        var existingResponse = await ctx.PackageUserResponses
            .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
        if (existingResponse != null)
        {
            if (answers.ContainsKey(questionId))  // guard PXF-12
            {
                existingResponse.PackageOptionId = answers[questionId];
                existingResponse.SubmittedAt = DateTime.UtcNow;
            }
        }
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task McUpsert_AbsentQuestion_PreservesSavedAnswer()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var (sessionId, pkgId) = await SeedSessionAsync(ctx, userId, S.Completed);
        var mcQ = await SeedQuestionAsync(ctx, pkgId, "MultipleChoice", 10, 1, "Pilih A.");
        var optA = new PackageOption { PackageQuestionId = mcQ, OptionText = "A", IsCorrect = true };
        var optB = new PackageOption { PackageQuestionId = mcQ, OptionText = "B", IsCorrect = false };
        ctx.PackageOptions.AddRange(optA, optB);
        await ctx.SaveChangesAsync();
        // Jawaban ter-autosave via SignalR sebelumnya = optA.
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = sessionId, PackageQuestionId = mcQ, PackageOptionId = optA.Id, SubmittedAt = new DateTime(2026, 6, 16, 8, 0, 0, DateTimeKind.Utc) });
        await ctx.SaveChangesAsync();

        // Form submit TIDAK mengandung mcQ (mis. form parsial / JS gagal).
        var answers = new Dictionary<int, int?>();  // absent: tidak ada key mcQ
        await RunMcUpsertLogicAsync(ctx, sessionId, mcQ, answers);

        await using var verify = NewCtx();
        var row = await verify.PackageUserResponses
            .FirstAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == mcQ);
        Assert.Equal(optA.Id, row.PackageOptionId);  // jawaban tersimpan UNCHANGED (tidak ter-null-overwrite)
        Assert.NotNull(row.PackageOptionId);
    }

    [Fact]
    public async Task McUpsert_PresentQuestion_UpdatesAnswer()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        var (sessionId, pkgId) = await SeedSessionAsync(ctx, userId, S.Completed);
        var mcQ = await SeedQuestionAsync(ctx, pkgId, "MultipleChoice", 10, 1, "Pilih A.");
        var optA = new PackageOption { PackageQuestionId = mcQ, OptionText = "A", IsCorrect = true };
        var optB = new PackageOption { PackageQuestionId = mcQ, OptionText = "B", IsCorrect = false };
        ctx.PackageOptions.AddRange(optA, optB);
        await ctx.SaveChangesAsync();
        ctx.PackageUserResponses.Add(new PackageUserResponse
        { AssessmentSessionId = sessionId, PackageQuestionId = mcQ, PackageOptionId = optA.Id, SubmittedAt = new DateTime(2026, 6, 16, 8, 0, 0, DateTimeKind.Utc) });
        await ctx.SaveChangesAsync();

        // Form submit MENGANDUNG mcQ → optB (ganti jawaban).
        var answers = new Dictionary<int, int?> { [mcQ] = optB.Id };
        await RunMcUpsertLogicAsync(ctx, sessionId, mcQ, answers);

        await using var verify = NewCtx();
        var row = await verify.PackageUserResponses
            .FirstAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == mcQ);
        Assert.Equal(optB.Id, row.PackageOptionId);  // ter-update ke pilihan baru
    }
}
