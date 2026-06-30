// Phase 999.17-02 GRADE-LOCK (D-11) — kunci normalisasi skor akhir 0-100 dengan bobot ScoreValue NON-UNIFORM.
//
// De-tautology: import 2 soal MC Universal via action ASLI ImportPackageQuestions (ScoreValue dibaca dari
// kolom Skor Excel: 30 & 10) → grade via GradingService.GradeAndCompleteAsync (jalur kanonik, weighted) →
// assert Score == 75 (benar HANYA soal-1 berbobot 30; maxScore 40 → 30/40*100). Membuktikan SEKALIGUS:
//   (a) import menulis ScoreValue dari Excel (bukan hardcode 10), dan
//   (b) GradingService menormalisasi dengan bobot non-uniform (GradingService.cs:99/144) — TIDAK diubah (D-11).
// Bila bobot uniform (10/10) seperti perilaku lama → 10/20*100 = 50 (RED sebelum impor membaca Skor).
//
// Integration: butuh FK + ExecuteUpdateAsync (tak didukung EF InMemory) → SQLEXPRESS disposable
// (pola GradingDedupeFixture/SectionFixture). HcPortalDB_Dev TAK tersentuh. [Trait Category=Integration].
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using S = HcPortal.Models.AssessmentConstants.AssessmentStatus;

namespace HcPortal.Tests;

public class WeightedScoreFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public WeightedScoreFixture()
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
                $"Phase 999.17-02 WeightedScoreFixture setup failed during MigrateAsync of {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug grade-lock. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class WeightedScoreImportTests : IClassFixture<WeightedScoreFixture>
{
    private readonly WeightedScoreFixture _fixture;
    public WeightedScoreImportTests(WeightedScoreFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- harness controller (subset pola SectionImportTests) ----
    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        private readonly ApplicationUser _actor;
        public StubUserManager(ApplicationUser actor)
            : base(new StubUserStore(), null!, null!, null!, null!, null!, null!, null!, null!) => _actor = actor;
        public override Task<ApplicationUser?> GetUserAsync(System.Security.Claims.ClaimsPrincipal principal)
            => Task.FromResult<ApplicationUser?>(_actor);
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private sealed class StubWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ApplicationName { get; set; } = "HcPortal.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
    }

    private sealed class StubUserStore : IUserStore<ApplicationUser>
    {
        public Task<string> GetUserIdAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult<string?>(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, System.Threading.CancellationToken ct) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult<string?>(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, System.Threading.CancellationToken ct) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, System.Threading.CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, System.Threading.CancellationToken ct) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, System.Threading.CancellationToken ct) => Task.FromResult<ApplicationUser?>(null);
        public void Dispose() { }
    }

    private AssessmentAdminController MakeController(ApplicationDbContext ctx, ApplicationUser actor)
    {
        var auditLog = new AuditLogService(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        #pragma warning disable CS8625
        var ctrl = new AssessmentAdminController(
            ctx,
            userManager:             new StubUserManager(actor),
            auditLog:                auditLog,
            env:                     new StubWebHostEnvironment(),
            cache:                   cache,
            logger:                  NullLogger<AssessmentAdminController>.Instance,
            notificationService:     null!,
            hubContext:              new NoopHubContext(),
            workerDataService:       null!,
            gradingService:          null!,
            protonCompletionService: null!,
            protonBypassService:     null!,
            retakeService:           new RetakeService(ctx, auditLog, new NoopHubContext(), NullLogger<RetakeService>.Instance));
        #pragma warning restore CS8625
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            ActionDescriptor = new ControllerActionDescriptor { ActionName = "ImportPackageQuestions" }
        };
        ctrl.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
        return ctrl;
    }

    // GradingService punya dependency berat; untuk session NON-Proton branch Proton tak terpanggil (pola GradingDedupeTests).
    private GradingService NewGradingService(ApplicationDbContext ctx)
    {
        var fakeNotif = new FakeNotificationService();
        var audit = new AuditLogService(ctx);
        var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
        var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
        var worker = new FakeWorkerDataService();
        return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
    }

    // 14-col Universal file (+Skor di index 13). BuildFile skip sel kosong.
    private static IFormFile BuildUniversalFile(IEnumerable<string[]> dataRows)
    {
        var headers = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Opsi E", "Opsi F", "Jawaban Benar", "No. Section", "Nama Section", "Elemen Teknis", "QuestionType", "Rubrik", "Skor" };
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Question Import");
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        int r = 2;
        foreach (var row in dataRows)
        {
            for (int c = 0; c < row.Length; c++)
                if (!string.IsNullOrEmpty(row[c])) ws.Cell(r, c + 1).Value = row[c];
            r++;
        }
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return new FormFile(ms, 0, ms.Length, "excelFile", "import.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    [Fact]
    public async Task ImportWeightedScores_GradesNormalizedTo100_NonUniformWeights()
    {
        string userId; int sessionId; int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            var user = new ApplicationUser { UserName = "wsi-" + Guid.NewGuid().ToString("N")[..8], Email = "wsi@test.local", FullName = "Weighted Test" };
            seed.Users.Add(user);
            await seed.SaveChangesAsync();
            userId = user.Id;

            var session = new AssessmentSession
            {
                UserId = userId, Title = "Bobot-" + Guid.NewGuid().ToString("N")[..8], Category = "IHT",
                Status = S.InProgress, AccessToken = "", Schedule = DateTime.UtcNow.Date,
                DurationMinutes = 60, PassPercentage = 70, GenerateCertificate = false, Progress = 0
            };
            seed.AssessmentSessions.Add(session);
            await seed.SaveChangesAsync();
            sessionId = session.Id;

            var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = "Paket Bobot", PackageNumber = 1 };
            seed.AssessmentPackages.Add(pkg);
            await seed.SaveChangesAsync();
            packageId = pkg.Id;

            var actor = new ApplicationUser { UserName = "act-" + Guid.NewGuid().ToString("N")[..8], Email = "act@test.local", FullName = "HC Actor", NIP = "99999" };
            seed.Users.Add(actor);
            await seed.SaveChangesAsync();
            actorId = actor.Id;
        }

        // Import 2 soal MC Universal dengan bobot NON-UNIFORM: soal-1 Skor=30, soal-2 Skor=10. Jawaban benar = A.
        var file = BuildUniversalFile(new[]
        {
            new[] { "Soal bobot 30?", "A1", "A2", "A3", "A4", "", "", "A", "", "", "K3", "MultipleChoice", "", "30" },
            new[] { "Soal bobot 10?", "B1", "B2", "B3", "B4", "", "", "A", "", "", "K3", "MultipleChoice", "", "10" },
        });

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!);
            var res = await ctrl.ImportPackageQuestions(packageId, file, null);
            Assert.IsType<RedirectToActionResult>(res);
        }

        // Bangun assignment + jawab BENAR hanya soal-1 (bobot 30); soal-2 (bobot 10) tak terjawab → salah.
        await using (var ctx = NewCtx())
        {
            var questions = await ctx.PackageQuestions.Include(q => q.Options)
                .Where(q => q.AssessmentPackageId == packageId).OrderBy(q => q.Order).ToListAsync();
            Assert.Equal(2, questions.Count);
            var q1 = questions[0];
            var q2 = questions[1];
            Assert.Equal(30, q1.ScoreValue);   // RED: hardcode 10 → maxScore 20 → 50%
            Assert.Equal(10, q2.ScoreValue);

            var correctOpt1 = q1.Options.Single(o => o.IsCorrect);
            ctx.PackageUserResponses.Add(new PackageUserResponse
            {
                AssessmentSessionId = sessionId, PackageQuestionId = q1.Id,
                PackageOptionId = correctOpt1.Id, SubmittedAt = DateTime.UtcNow
            });
            ctx.UserPackageAssignments.Add(new UserPackageAssignment
            {
                AssessmentSessionId = sessionId, AssessmentPackageId = packageId, UserId = userId,
                ShuffledQuestionIds = JsonSerializer.Serialize(new List<int> { q1.Id, q2.Id })
            });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx())
        {
            var session = await ctx.AssessmentSessions.FindAsync(sessionId);
            var svc = NewGradingService(ctx);
            var ok = await svc.GradeAndCompleteAsync(session!);
            Assert.True(ok);
        }

        await using (var verify = NewCtx())
        {
            var graded = await verify.AssessmentSessions.FindAsync(sessionId);
            // benar soal-1 (bobot 30) dari maxScore 40 → 75%. Bila bobot uniform (10/10) lama → 10/20 = 50%.
            Assert.Equal(75, graded!.Score);
        }
    }
}
