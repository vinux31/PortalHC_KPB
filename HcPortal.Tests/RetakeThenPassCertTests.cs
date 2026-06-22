// RetakeThenPassCertTests — v32.4 Phase 408 Plan 01 (RTK-14, GAP-1).
//
// CAPSTONE integration real-SQL (disposable DB HcPortalDB_Test_{guid} @ localhost\SQLEXPRESS via
// RetakeServiceFixture full MigrateAsync chain incl AddRetakeColumnsAndArchive). PURELY ADDITIVE:
// TIDAK menyentuh test hijau lain (RetakeServiceTests/SubmitResurrectionTests/dst). REUSE total
// infrastruktur existing (fixture RetakeServiceFixture + NoOpHubContext dari RetakeServiceTests.cs;
// stub FakeNotificationService/FakeWorkerDataService dari HcPortal.Tests/; recipe ctor GradingService
// dari SubmitResurrectionTests.cs:68-76). [Trait("Category","Integration")] → SQL-less CI skip via
// --filter "Category!=Integration".
//
// Invariant yang DIBUKTIKAN (GAP-1 / T-408-cert): sesi GAGAL (IsPassed=false) → retake (RetakeService.
// ExecuteAsync, reset-ONLY: hapus responses/assignment + arsip snapshot, Status→Open, TIDAK issue cert)
// → ambil-ulang lulus (re-seed responses-benar + GradingService.GradeAndCompleteAsync, grade-dari-DB,
// step 6 issue NomorSertifikat) → menerbitkan TEPAT 1 NomorSertifikat (anti-double-cert guard
// GradingService.cs:287-312: retry 3x + filtered WHERE NomorSertifikat==null, di bawah unique index
// IX_AssessmentSessions_NomorSertifikat). Format kanonik KPB/{seq:D3}/{RomanMonth}/{year}.
//
// Pitfall 1 (RESEARCH): ExecuteAsync TAK PERNAH issue cert — assert cert==null tepat setelah ExecuteAsync
//   selalu lulus menyesatkan (bukan invariant). Cert lahir di GradeAndCompleteAsync (langkah 2).
// Pitfall 2: AssessmentSession.GenerateCertificate default false → WAJIB di-set true (param baru seed) +
//   PassPercentage cukup rendah (default model 70; all-correct → Score 100 ≥ 70 → isPassed=true).
// A2 (RESEARCH): pasca ExecuteAsync Status sesi = "Open" (RetakeService.cs:104). GradeAndCompleteAsync
//   MENERIMA "Open" (guard menolak HANYA terminal Completed/Abandoned/Cancelled/PendingGrading,
//   GradingService.cs:254-257) → grade lanjut, cert terbit. Tidak perlu set Status manual.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class RetakeThenPassCertTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public RetakeThenPassCertTests(RetakeServiceFixture f) => _fixture = f;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // REUSE RetakeService ctor recipe (RetakeServiceTests.cs:110-111; NoOpHubContext same assembly).
    private static RetakeService NewRetake(ApplicationDbContext ctx) =>
        new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);

    // CORE GAP-1 UNBLOCK — recipe ctor GradingService VERBATIM dari SubmitResurrectionTests.cs:68-76
    // (semua stub FakeNotificationService/FakeWorkerDataService sudah ada di HcPortal.Tests/; no Moq).
    private static GradingService NewGrading(ApplicationDbContext ctx)
    {
        var fakeNotif = new FakeNotificationService();
        var audit = new AuditLogService(ctx);
        var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
        var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
        var worker = new FakeWorkerDataService();
        return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
    }

    // ---------- Seed helpers (disalin dari RetakeServiceTests.cs:114-174; private RetakeServiceTests
    //            tak terjangkau lintas-class → file self-contained). SeedSessionAsync DIPERLUAS:
    //            tambah param `generateCertificate` (set s.GenerateCertificate) + set PassPercentage. ----------
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "rtkc-" + Guid.NewGuid().ToString("N")[..8], Email = "rtkc@test.local", FullName = "Retake Cert Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string category,
        string status = "Completed", bool? isPassed = false, bool allowRetake = true,
        int maxAttempts = 2, int cooldownHours = 0, DateTime? completedAt = null,
        string? assessmentType = null, bool isManualEntry = false,
        bool generateCertificate = false, int passPercentage = 70)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = category, Status = status,
            AccessToken = "", Schedule = new DateTime(2026, 2, 1),
            IsPassed = isPassed, AllowRetake = allowRetake, MaxAttempts = maxAttempts,
            RetakeCooldownHours = cooldownHours, CompletedAt = completedAt,
            AssessmentType = assessmentType, IsManualEntry = isManualEntry,
            GenerateCertificate = generateCertificate,    // Pitfall 2: default false → WAJIB true untuk jalur cert.
            PassPercentage = passPercentage,               // all-correct (Score 100) ≥ 70 → isPassed=true.
            Score = 50, Progress = 100
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    /// <summary>Seed 1 package + N MC soal (1 correct option each) untuk session + assignment + responses-benar.</summary>
    private static async Task<List<int>> SeedPackageWithResponsesAsync(ApplicationDbContext ctx, int sessionId, int nQuestions)
    {
        var pkg = new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        var qIds = new List<int>();
        for (int i = 0; i < nQuestions; i++)
        {
            var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionText = $"Soal {i + 1}", Order = i, ScoreValue = 10, QuestionType = "MultipleChoice" };
            ctx.PackageQuestions.Add(q);
            await ctx.SaveChangesAsync();
            var correct = new PackageOption { PackageQuestionId = q.Id, OptionText = "Benar", IsCorrect = true };
            var wrong = new PackageOption { PackageQuestionId = q.Id, OptionText = "Salah", IsCorrect = false };
            ctx.PackageOptions.AddRange(correct, wrong);
            await ctx.SaveChangesAsync();
            // Worker memilih opsi benar (response) → re-grade 100%.
            ctx.PackageUserResponses.Add(new PackageUserResponse { AssessmentSessionId = sessionId, PackageQuestionId = q.Id, PackageOptionId = correct.Id });
            qIds.Add(q.Id);
        }
        await ctx.SaveChangesAsync();

        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = sessionId,
            AssessmentPackageId = pkg.Id,
            UserId = "",   // not needed for retake/grade path
            ShuffledQuestionIds = JsonSerializer.Serialize(qIds)   // GradeAndCompleteAsync membaca via GetShuffledQuestionIds()
        });
        await ctx.SaveChangesAsync();
        return qIds;
    }

    // ====================== GAP-1 capstone: retake → grade-lulus → TEPAT 1 cert ======================
    [Fact]
    public async Task RetakeThenPass_IssuesExactlyOneCertificate()
    {
        await using var ctx = NewCtx();
        var userId = await SeedUserAsync(ctx);
        // Sesi GAGAL ber-sertifikat-aktif (PostTest, cooldown 0, completed 2 hari lalu).
        var sid = await SeedSessionAsync(ctx, userId, "CertTitle", "Post",
            status: "Completed", isPassed: false, allowRetake: true, maxAttempts: 2,
            cooldownHours: 0, completedAt: DateTime.UtcNow.AddDays(-2),
            assessmentType: "PostTest", generateCertificate: true);
        await SeedPackageWithResponsesAsync(ctx, sid, 3);

        // LANGKAH 1 (retake = reset-ONLY): hapus responses/assignment + arsip snapshot, Status→Open.
        // Pitfall 1: TIDAK assert cert di sini — ExecuteAsync tak pernah issue cert.
        var retake = await NewRetake(ctx).ExecuteAsync(sid, userId, "Tester", "RetakeAssessment", "worker_retake");
        Assert.True(retake.Success);

        // LANGKAH 2 (simulasi ambil-ulang lulus): re-seed paket+assignment+respons-benar pasca-reset
        // (ExecuteAsync menghapus responses/assignment), muat ulang sesi (kini Status="Open"), lalu grade.
        // GradeAndCompleteAsync grade-dari-DB → isPassed=true → step 6 issue NomorSertifikat.
        await SeedPackageWithResponsesAsync(ctx, sid, 3);
        var session = await ctx.AssessmentSessions.FirstAsync(a => a.Id == sid);
        var graded = await NewGrading(ctx).GradeAndCompleteAsync(session);
        Assert.True(graded);

        // LANGKAH 3 (CORE ASSERT, DbContext baru): TEPAT 1 NomorSertifikat (anti-double-cert guard) +
        // format kanonik KPB/{seq:D3}/{RomanMonth}/{year}.
        await using var verify = NewCtx();
        int certCount = await verify.AssessmentSessions.CountAsync(a => a.Id == sid && a.NomorSertifikat != null);
        Assert.Equal(1, certCount);
        var cert = await verify.AssessmentSessions.Where(a => a.Id == sid).Select(a => a.NomorSertifikat).SingleAsync();
        Assert.Matches(@"^KPB/\d{3}/[IVX]+/\d{4}$", cert);

        // Sesi kini Completed-lulus → counting (UserId,Title,Category) konsisten: sesi yang lulus tidak lagi
        // eligible retake (RetakeRules menolak isPassed==true). Nilai-tambah D-02 (BUKAN duplikasi counting —
        // cakupan (UserId,Title,Category) Pre/Post no-conflate ada di RetakeServiceTests.Counting_PrePostSameTitle_NoConflate).
        Assert.False(await NewRetake(verify).CanRetakeAsync(sid));
    }
}
