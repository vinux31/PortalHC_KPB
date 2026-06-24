// CertIssuanceIntegrationTests — v32.7 Phase 423 Plan 02 Task 3 (CERT-01/02/03/05).
//
// Integration real-SQL (disposable DB HcPortalDB_Test_{guid} @ localhost\SQLEXPRESS via RetakeServiceFixture
// full MigrateAsync chain). [Trait("Category","Integration")] → SQL-less CI skip via --filter "Category!=Integration".
// PURELY ADDITIVE: tak menyentuh test hijau lain. REUSE fixture RetakeServiceFixture + NoOpHubContext +
// FakeNotificationService/FakeWorkerDataService dari assembly test. Recipe ctor GradingService + seed helper
// disalin VERBATIM dari RetakeThenPassCertTests.cs (file self-contained — private cross-class tak terjangkau,
// duplikasi seed helper OK).
//
// Invariant yang DIBUKTIKAN end-to-end di site nyata GradingService.GradeAndCompleteAsync (gate Phase 423
// CertIssuanceRules.ShouldIssueCertificate + CertNumberHelper.TryAssignNextSeqAsync):
//   CERT-01: PreTest TIDAK pernah menerbitkan cert (gate tolak AssessmentType==PreTest) walau lulus + GenerateCertificate.
//   CERT-03: PostTest lulus → TEPAT 1 NomorSertifikat, format kanonik KPB/{seq:D3}/{ROMAN}/{year}.
//   CERT-02: ValidUntil di-derive dari CompletedAt utk CertificateType kanonik (Annual → +1 tahun).
//   CERT-05: anti-dup HasActiveCertForTitleAsync (predikat direplikasi — method private di controller):
//            cert aktif → blok; cert kedaluwarsa → lolos; sesi RenewsSessionId terisi → dikecualikan (renewal exempt).
//   CERT-03 seq-fail signal: predikat queryable HC (IsPassed && GenerateCertificate && !PreTest && NomorSertifikat==null).
//
// Catatan perilaku produksi (DIVERIFIKASI dari GradingService.cs): GradeAndCompleteAsync menulis CompletedAt =
// DateTime.UtcNow saat grade (ExecuteUpdateAsync) DAN menyinkronkan session.CompletedAt in-memory ke nilai
// yang SAMA → DeriveValidUntil(session.CertificateType, session.CompletedAt) memakai tanggal grading
// (≈ hari ini), BUKAN CompletedAt yang di-seed. Maka CERT-02 meng-assert ValidUntil = today + 1y (rentang
// before/after untuk aman dari pergantian hari).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class CertIssuanceIntegrationTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public CertIssuanceIntegrationTests(RetakeServiceFixture f) => _fixture = f;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // Marker isolasi seed per-test (semua DB share fixture instance) — title/category prefix unik.
    private static string Tag() => "TAG-" + Guid.NewGuid().ToString("N").Substring(0, 8);

    // CORE recipe ctor GradingService VERBATIM dari RetakeThenPassCertTests.cs:53-61 (stub di assembly test; no Moq).
    private static GradingService NewGrading(ApplicationDbContext ctx)
    {
        var fakeNotif = new FakeNotificationService();
        var audit = new AuditLogService(ctx);
        var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
        var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
        var worker = new FakeWorkerDataService();
        return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
    }

    // ---------- Seed helpers (disalin dari RetakeThenPassCertTests.cs:66-136) ----------
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "certi-" + Guid.NewGuid().ToString("N")[..8], Email = "certi@test.local", FullName = "Cert Issuance Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static async Task<int> SeedSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string category,
        string status = "Completed", bool? isPassed = false, bool allowRetake = true,
        int maxAttempts = 2, int cooldownHours = 0, DateTime? completedAt = null,
        string? assessmentType = null, bool isManualEntry = false,
        bool generateCertificate = false, int passPercentage = 70,
        string? certificateType = null, string? nomorSertifikat = null,
        DateOnly? validUntil = null, int? renewsSessionId = null)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = category, Status = status,
            AccessToken = "", Schedule = new DateTime(2026, 2, 1),
            IsPassed = isPassed, AllowRetake = allowRetake, MaxAttempts = maxAttempts,
            RetakeCooldownHours = cooldownHours, CompletedAt = completedAt,
            AssessmentType = assessmentType, IsManualEntry = isManualEntry,
            GenerateCertificate = generateCertificate,
            PassPercentage = passPercentage,
            CertificateType = certificateType,
            NomorSertifikat = nomorSertifikat,
            ValidUntil = validUntil,
            RenewsSessionId = renewsSessionId,
            Score = 50, Progress = 100
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    /// <summary>Seed 1 package + N MC soal (1 correct option each) + assignment + responses-benar (re-grade 100%).</summary>
    private static async Task<List<int>> SeedPackageWithResponsesAsync(ApplicationDbContext ctx, int sessionId, int nQuestions)
    {
        int nextPackageNumber = (await ctx.AssessmentPackages
            .Where(p => p.AssessmentSessionId == sessionId)
            .Select(p => (int?)p.PackageNumber)
            .MaxAsync() ?? 0) + 1;
        var pkg = new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "Paket A", PackageNumber = nextPackageNumber };
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
            ctx.PackageUserResponses.Add(new PackageUserResponse { AssessmentSessionId = sessionId, PackageQuestionId = q.Id, PackageOptionId = correct.Id });
            qIds.Add(q.Id);
        }
        await ctx.SaveChangesAsync();

        ctx.UserPackageAssignments.Add(new UserPackageAssignment
        {
            AssessmentSessionId = sessionId,
            AssessmentPackageId = pkg.Id,
            UserId = "",
            ShuffledQuestionIds = JsonSerializer.Serialize(qIds)
        });
        await ctx.SaveChangesAsync();
        return qIds;
    }

    // ====================== CERT-01: PreTest TIDAK menerbitkan cert (site nyata) ======================
    [Fact]
    public async Task CERT01_PreTest_GradesButNeverIssuesCertificate()
    {
        await using var ctx = NewCtx();
        var tag = Tag();
        var userId = await SeedUserAsync(ctx);
        // Status="Open" agar guard GradeAndCompleteAsync (tolak terminal) lolos → grade jalan.
        // PreTest + GenerateCertificate=true + all-correct (100% ≥ 70 → isPassed) → gate ShouldIssueCertificate
        // HARUS tolak karena AssessmentType==PreTest (CERT-01).
        var sid = await SeedSessionAsync(ctx, userId, $"{tag} PreTest Title", "Assessment",
            status: "Open", isPassed: false, assessmentType: "PreTest", generateCertificate: true);
        await SeedPackageWithResponsesAsync(ctx, sid, 3);

        var session = await ctx.AssessmentSessions.FirstAsync(a => a.Id == sid);
        var graded = await NewGrading(ctx).GradeAndCompleteAsync(session);
        Assert.True(graded);

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FirstAsync(a => a.Id == sid);
        Assert.Equal("Completed", s.Status);          // graded (selesai)
        Assert.True(s.IsPassed);                       // lulus secara skor
        Assert.Null(s.NomorSertifikat);                // tetapi TIDAK ada cert (gate PreTest)
        int certCount = await verify.AssessmentSessions.CountAsync(a => a.Id == sid && a.NomorSertifikat != null);
        Assert.Equal(0, certCount);
    }

    // ====================== CERT-03: PostTest lulus → TEPAT 1 cert, format kanonik ======================
    [Fact]
    public async Task CERT03_PostTestPassing_IssuesExactlyOneCertificate_CanonicalFormat()
    {
        await using var ctx = NewCtx();
        var tag = Tag();
        var userId = await SeedUserAsync(ctx);
        var sid = await SeedSessionAsync(ctx, userId, $"{tag} PostTest Cert", "Assessment",
            status: "Open", isPassed: false, assessmentType: "PostTest", generateCertificate: true);
        await SeedPackageWithResponsesAsync(ctx, sid, 3);

        var session = await ctx.AssessmentSessions.FirstAsync(a => a.Id == sid);
        var graded = await NewGrading(ctx).GradeAndCompleteAsync(session);
        Assert.True(graded);

        await using var verify = NewCtx();
        int certCount = await verify.AssessmentSessions.CountAsync(a => a.Id == sid && a.NomorSertifikat != null);
        Assert.Equal(1, certCount);
        var cert = await verify.AssessmentSessions.Where(a => a.Id == sid).Select(a => a.NomorSertifikat).SingleAsync();
        Assert.Matches(@"^KPB/\d{3}/[IVX]+/\d{4}$", cert);
    }

    // ====================== CERT-02: ValidUntil derive (Annual → +1 tahun dari grade-date) ======================
    [Fact]
    public async Task CERT02_PostTestAnnual_DerivesValidUntilPlusOneYear()
    {
        await using var ctx = NewCtx();
        var tag = Tag();
        var userId = await SeedUserAsync(ctx);
        var sid = await SeedSessionAsync(ctx, userId, $"{tag} PostTest Annual", "Assessment",
            status: "Open", isPassed: false, assessmentType: "PostTest", generateCertificate: true,
            certificateType: AssessmentConstants.CertificateType.Annual);
        await SeedPackageWithResponsesAsync(ctx, sid, 3);

        // GradeAndCompleteAsync set CompletedAt = DateTime.UtcNow (in-memory & DB) → DeriveValidUntil dari
        // tanggal grading. Tangkap rentang before/after agar tahan pergantian hari saat test berjalan.
        var beforeDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);
        var session = await ctx.AssessmentSessions.FirstAsync(a => a.Id == sid);
        var graded = await NewGrading(ctx).GradeAndCompleteAsync(session);
        Assert.True(graded);
        var afterDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);

        await using var verify = NewCtx();
        var s = await verify.AssessmentSessions.FirstAsync(a => a.Id == sid);
        Assert.NotNull(s.NomorSertifikat);             // cert terbit (prasyarat ValidUntil)
        Assert.NotNull(s.ValidUntil);                  // Annual → derive non-null
        // ValidUntil = DateOnly.FromDateTime(grade-CompletedAt).AddYears(1) ∈ [beforeDate, afterDate].
        Assert.True(s.ValidUntil >= beforeDate && s.ValidUntil <= afterDate,
            $"ValidUntil {s.ValidUntil} di luar rentang [{beforeDate}, {afterDate}].");
        // Konsistensi langsung dgn kontrak Wave 1 (CertIssuanceRules.DeriveValidUntil) atas CompletedAt aktual DB.
        Assert.Equal(CertIssuanceRules.DeriveValidUntil(AssessmentConstants.CertificateType.Annual, s.CompletedAt), s.ValidUntil);
    }

    // ====================== CERT-05: anti-dup HasActiveCertForTitleAsync (predikat direplikasi) ======================
    // Method HasActiveCertForTitleAsync private di AssessmentAdminController — predikat (Controllers/
    // AssessmentAdminController.cs:5937-5946) DIREPLIKASI persis di sini untuk membuktikan logika D-07.
    [Fact]
    public async Task CERT05_AntiDup_ActiveBlocks_ExpiredPasses_RenewalExempt()
    {
        await using var ctx = NewCtx();
        var tag = Tag();
        var title = $"{tag} K3 Migas";
        var norm = AdminBaseController.NormalizeTitleForDup(title);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var userActive = await SeedUserAsync(ctx);
        var userExpired = await SeedUserAsync(ctx);
        var userRenewal = await SeedUserAsync(ctx);

        // (a) cert AKTIF (ValidUntil masa depan) → harus terdeteksi (blok).
        await SeedSessionAsync(ctx, userActive, title, "Assessment",
            status: "Completed", isPassed: true, assessmentType: "PostTest",
            nomorSertifikat: "KPB/901/VI/2026", validUntil: today.AddYears(1), renewsSessionId: null);

        // (b) cert KEDALUWARSA (ValidUntil < today) → tidak terdeteksi (lolos terbit baru).
        await SeedSessionAsync(ctx, userExpired, title, "Assessment",
            status: "Completed", isPassed: true, assessmentType: "PostTest",
            nomorSertifikat: "KPB/902/VI/2026", validUntil: today.AddDays(-1), renewsSessionId: null);

        // (c) sesi RENEWAL (RenewsSessionId terisi) → dikecualikan (predikat RenewsSessionId==null).
        // RenewsSessionId ber-self-FK (FK_AssessmentSessions_AssessmentSessions_RenewsSessionId) → seed dulu
        // sesi induk (yang diperpanjang) lalu rujuk Id-nya.
        var renewedSid = await SeedSessionAsync(ctx, userRenewal, title, "Assessment",
            status: "Completed", isPassed: true, assessmentType: "PostTest",
            nomorSertifikat: "KPB/900/VI/2026", validUntil: today.AddDays(-1), renewsSessionId: null);
        await SeedSessionAsync(ctx, userRenewal, title, "Assessment",
            status: "Completed", isPassed: true, assessmentType: "PostTest",
            nomorSertifikat: "KPB/903/VI/2026", validUntil: today.AddYears(1), renewsSessionId: renewedSid);

        // Predikat HasActiveCertForTitleAsync (replika 1:1; excludeSessionId=null).
        async Task<bool> HasActiveCertForTitle(string userId)
        {
            var candidates = await ctx.AssessmentSessions
                .Where(s => s.UserId == userId
                         && s.NomorSertifikat != null
                         && s.IsPassed == true
                         && s.RenewsSessionId == null
                         && (s.ValidUntil == null || s.ValidUntil >= today))
                .Select(s => s.Title)
                .ToListAsync();
            return candidates.Any(t => AdminBaseController.NormalizeTitleForDup(t) == norm);
        }

        Assert.True(await HasActiveCertForTitle(userActive));    // (a) aktif → blok
        Assert.False(await HasActiveCertForTitle(userExpired));  // (b) kedaluwarsa → lolos
        Assert.False(await HasActiveCertForTitle(userRenewal));  // (c) renewal → exempt
    }

    // ====================== CERT-03 seq-fail: predikat sinyal HC queryable ======================
    // Tidak memaksa kolisi nyata (D-03) — cukup buktikan predikat sinyal benar (lulus + GenerateCertificate +
    // bukan PreTest + NomorSertifikat==null) menemukan sesi yang gagal terbit, dan TIDAK menjaring PreTest/already-issued.
    [Fact]
    public async Task CERT03_SeqFailSignal_QueryablePredicate_FindsStuckSession()
    {
        await using var ctx = NewCtx();
        var tag = Tag();
        var userId = await SeedUserAsync(ctx);

        // Target: simulasi gagal terbit (lulus, GenerateCertificate, PostTest, NomorSertifikat==null).
        var stuck = await SeedSessionAsync(ctx, userId, $"{tag} Stuck", "Assessment",
            status: "Completed", isPassed: true, assessmentType: "PostTest",
            generateCertificate: true, nomorSertifikat: null);

        // Distraktor 1: PreTest lulus → JANGAN terjaring (bukan kandidat cert).
        await SeedSessionAsync(ctx, userId, $"{tag} PreDistractor", "Assessment",
            status: "Completed", isPassed: true, assessmentType: "PreTest",
            generateCertificate: true, nomorSertifikat: null);
        // Distraktor 2: PostTest lulus sudah ber-cert → JANGAN terjaring (NomorSertifikat != null).
        await SeedSessionAsync(ctx, userId, $"{tag} Issued", "Assessment",
            status: "Completed", isPassed: true, assessmentType: "PostTest",
            generateCertificate: true, nomorSertifikat: "KPB/904/VI/2026");

        await using var verify = NewCtx();
        var signal = await verify.AssessmentSessions
            .Where(s => s.IsPassed == true
                     && s.GenerateCertificate
                     && s.AssessmentType != "PreTest"
                     && s.NomorSertifikat == null
                     && s.Title.StartsWith(tag))
            .Select(s => s.Id)
            .ToListAsync();

        Assert.Single(signal);
        Assert.Equal(stuck, signal[0]);
    }
}
