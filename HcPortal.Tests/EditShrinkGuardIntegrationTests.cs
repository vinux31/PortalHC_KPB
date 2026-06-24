// Phase 418-04 Task 1 (D-418-02) — INTEGRATION real-SQL untuk guard edit-shrink.
//
// Komplemen pure-logic EditShrinkGuardLogicTests (Plan 01): di sini kita gerakkan action ASLI
// AssessmentAdminController.EditQuestion atas SQL Server NYATA (SectionFixture, MigrateAsync), dengan
// PackageUserResponse yang men-seed jawaban peserta ke salah satu PackageOption. Membuktikan:
//   (1) Hapus/menyusutkan opsi yang SUDAH dijawab → guard memblok SEBELUM SaveChanges →
//       TIDAK ada DbUpdateException (FK Restrict 500 / hazard 999.14); redirect + TempData["Error"];
//       opsi yang terblok TETAP ADA di DB (tak terhapus); response peserta utuh.
//   (2) Hapus opsi yang BELUM dijawab (4→3, opsi D) → SUKSES; opsi D terhapus; tak ada error.
//
// Kenapa real-SQL (bukan InMemory): FK PackageUserResponse → PackageOption = Restrict hanya nyata di
// SQL Server. Tanpa guard, RemoveRange opsi terjawab → SaveChangesAsync lempar DbUpdateException → 500.
// Test ini mengunci bahwa guard mencegah jalur itu (no-throw + state DB benar), bukan sekadar irisan set.
//
// Harness (StubUserManager/UserStore/WebHostEnv/TempData, MakeController, SeedActor) disalin VERBATIM
// dari SectionFixRegressionTests agar konvensi & kompilasi sama persis. HcPortalDB_Dev TAK tersentuh
// (SectionFixture = DB sekali-pakai HcPortalDB_Test_{guid}, EnsureDeletedAsync di Dispose).
//
// [Trait Category=Integration] — butuh FK Restrict + persist → SQLEXPRESS.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class EditShrinkGuardIntegrationTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public EditShrinkGuardIntegrationTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ============================ Controller harness (verbatim dari SectionFixRegressionTests) ============================
    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        private readonly ApplicationUser _actor;
        public StubUserManager(ApplicationUser actor)
            : base(new StubUserStore(), null!, null!, null!, null!, null!, null!, null!, null!)
            => _actor = actor;
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

    private AssessmentAdminController MakeController(ApplicationDbContext ctx, ApplicationUser actor, string actionName)
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
            protonBypassService:     null!);
        #pragma warning restore CS8625
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            ActionDescriptor = new ControllerActionDescriptor { ActionName = actionName }
        };
        ctrl.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
        return ctrl;
    }

    private static async Task<ApplicationUser> SeedActorAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser
        {
            UserName = "actor-" + Guid.NewGuid().ToString("N")[..8],
            Email = "actor@test.local", FullName = "HC Actor", NIP = "99999", IsActive = true
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u;
    }

    // Seed 1 session + 1 package, return (sessionId, packageId).
    private static async Task<(int sessionId, int packageId)> SeedSessionPackageAsync(ApplicationDbContext ctx, string packageName)
    {
        var user = new ApplicationUser
        {
            UserName = "imp-" + Guid.NewGuid().ToString("N")[..8],
            Email = "imp@test.local", FullName = "Import Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var session = new AssessmentSession
        {
            UserId = user.Id, Title = "ES-" + Guid.NewGuid().ToString("N")[..8], Category = "OJT",
            Status = "Open", AccessToken = "", Schedule = DateTime.UtcNow.Date, DurationMinutes = 60,
            PassPercentage = 70, Progress = 0, AssessmentType = "Standard"
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage { AssessmentSessionId = session.Id, PackageName = packageName, PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        return (session.Id, pkg.Id);
    }

    // Seed soal MC 4-opsi (A benar). Return (questionId, optionIds urut Id) + sessionId.
    private static async Task<(int questionId, List<int> optionIds, int sessionId)> SeedFourOptionQuestionAsync(
        ApplicationDbContext ctx, int sessionId, int packageId)
    {
        var q = new PackageQuestion
        {
            AssessmentPackageId = packageId, QuestionText = "Soal 4 opsi (edit-shrink)", Order = 1, ScoreValue = 10,
            QuestionType = "MultipleChoice", MaxCharacters = 2000,
            Options = new List<PackageOption>
            {
                new() { OptionText = "A", IsCorrect = true  },
                new() { OptionText = "B", IsCorrect = false },
                new() { OptionText = "C", IsCorrect = false },
                new() { OptionText = "D", IsCorrect = false },
            }
        };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();
        var optionIds = q.Options.OrderBy(o => o.Id).Select(o => o.Id).ToList();
        return (q.Id, optionIds, sessionId);
    }

    // =================================================================================================
    //  TEST 1 (D-418-02 KUNCI): opsi B SUDAH dijawab peserta. EditQuestion menyusutkan 4→3 dengan
    //  mengosongkan teks B → guard memblok pre-SaveChanges → TIDAK throw (bukan FK Restrict 500); opsi B
    //  TETAP ADA; redirect + TempData["Error"] memuat huruf opsi terblok.
    //
    //  Mekanika shrink: kontrak loop upsert + guard memakai aturan index-aligned OrderBy(Id) — posisi i
    //  dihapus bila i>=keep ATAU options[i].Text kosong. Kita kirim 4 OptionInput dengan posisi-B (index 1)
    //  ber-teks KOSONG → opsi B (existing[1]) ditandai removed → karena B sudah dijawab → BLOCKED.
    // =================================================================================================
    [Fact]
    public async Task EditShrinkGuard_AnsweredOption_NotRemoved_NoException()
    {
        int packageId, sessionId, questionId; string actorId; List<int> optionIds;
        await using (var seed = NewCtx())
        {
            (sessionId, packageId) = await SeedSessionPackageAsync(seed, "ESpkg-blocked");
            (questionId, optionIds, _) = await SeedFourOptionQuestionAsync(seed, sessionId, packageId);
            actorId = (await SeedActorAsync(seed)).Id;

            // Peserta menjawab opsi B (existing[1]) → PackageUserResponse merujuk PackageOptionId opsi B.
            seed.PackageUserResponses.Add(new PackageUserResponse
            {
                AssessmentSessionId = sessionId,
                PackageQuestionId = questionId,
                PackageOptionId = optionIds[1],   // opsi B dijawab
                SubmittedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "EditQuestion");

            // Shrink 4→3: kosongkan teks opsi B (index 1) → B ditandai dihapus. A benar (correctIndex=0).
            // Record.ExceptionAsync menjamin TIDAK ada DbUpdateException / 500 (guard memblok pre-SaveChanges).
            IActionResult? res = null;
            var ex = await Record.ExceptionAsync(async () =>
            {
                res = await ctrl.EditQuestion(
                    questionId, packageId, "Soal 4 opsi (edit-shrink)", "MultipleChoice", 10, "K3", null, 2000,
                    null, null, false,
                    new List<OptionInput>
                    {
                        new OptionInput { Text = "A" },
                        new OptionInput { Text = "" },    // B dikosongkan → ditandai dihapus (tapi sudah dijawab)
                        new OptionInput { Text = "C" },
                        new OptionInput { Text = "D" },
                    },
                    correctIndex: 0,
                    sectionId: null);
            });

            Assert.Null(ex);                                                    // TIDAK 500 (guard, bukan FK Restrict)
            var redirect = Assert.IsType<RedirectToActionResult>(res);
            Assert.Equal("ManagePackageQuestions", redirect.ActionName);
            var error = ctrl.TempData["Error"] as string;
            Assert.False(string.IsNullOrWhiteSpace(error));                     // pesan jelas (bukan exception page)
            Assert.Contains("sudah dijawab", error!);                          // wording guard D-418-02
            Assert.Contains("B", error!);                                       // huruf opsi terblok (posisi index 1)
        }

        await using (var verify = NewCtx())
        {
            // Opsi B TETAP ADA (tak terhapus) — soal masih 4 opsi utuh.
            var q = await verify.PackageQuestions.Include(x => x.Options).SingleAsync(x => x.Id == questionId);
            Assert.Equal(4, q.Options.Count);
            Assert.Contains(q.Options, o => o.Id == optionIds[1] && o.OptionText == "B");
            // Response peserta ke opsi B masih ada (tak ter-cascade).
            Assert.True(await verify.PackageUserResponses.AnyAsync(r => r.PackageOptionId == optionIds[1]));
        }
    }

    // =================================================================================================
    //  TEST 2 (kontrol): opsi D BELUM dijawab. EditQuestion menyusutkan 4→3 (hapus D) → SUKSES; opsi D
    //  terhapus; tak ada error. (Buktikan guard tidak over-block: opsi belum-terjawab boleh dihapus.)
    //
    //  Kita kirim 3 OptionInput (A,B,C) → keep=3 → posisi index 3 (opsi D, OrderBy Id) di luar keep →
    //  removed; karena D tak ada response → tidak terblok → di-Remove. A benar.
    // =================================================================================================
    [Fact]
    public async Task EditShrinkGuard_UnansweredOption_Removed_Succeeds()
    {
        int packageId, sessionId, questionId; string actorId; List<int> optionIds;
        await using (var seed = NewCtx())
        {
            (sessionId, packageId) = await SeedSessionPackageAsync(seed, "ESpkg-allowed");
            (questionId, optionIds, _) = await SeedFourOptionQuestionAsync(seed, sessionId, packageId);
            actorId = (await SeedActorAsync(seed)).Id;

            // Peserta menjawab opsi A (existing[0]) — BUKAN D. Maka menghapus D tidak terblok.
            seed.PackageUserResponses.Add(new PackageUserResponse
            {
                AssessmentSessionId = sessionId,
                PackageQuestionId = questionId,
                PackageOptionId = optionIds[0],   // opsi A dijawab (bukan D yang akan dihapus)
                SubmittedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "EditQuestion");

            IActionResult? res = null;
            var ex = await Record.ExceptionAsync(async () =>
            {
                res = await ctrl.EditQuestion(
                    questionId, packageId, "Soal 4 opsi (shrink ke 3)", "MultipleChoice", 10, "K3", null, 2000,
                    null, null, false,
                    new List<OptionInput>
                    {
                        new OptionInput { Text = "A" },
                        new OptionInput { Text = "B" },
                        new OptionInput { Text = "C" },   // hanya 3 opsi → D (index 3) di luar keep → dihapus
                    },
                    correctIndex: 0,
                    sectionId: null);
            });

            Assert.Null(ex);                                                    // tak ada error (D belum dijawab)
            var redirect = Assert.IsType<RedirectToActionResult>(res);
            Assert.Equal("ManagePackageQuestions", redirect.ActionName);
            Assert.True(string.IsNullOrWhiteSpace(ctrl.TempData["Error"] as string)); // SUKSES (tak ada pesan error)
        }

        await using (var verify = NewCtx())
        {
            // Opsi D TERHAPUS → soal tinggal 3 opsi (A,B,C). Opsi D (optionIds[3]) tak ada lagi.
            var q = await verify.PackageQuestions.Include(x => x.Options).SingleAsync(x => x.Id == questionId);
            Assert.Equal(3, q.Options.Count);
            Assert.DoesNotContain(q.Options, o => o.Id == optionIds[3]);       // D benar-benar dihapus
            Assert.Equal(new[] { "A", "B", "C" }, q.Options.OrderBy(o => o.Id).Select(o => o.OptionText).ToArray());
            // Response peserta ke opsi A (tak dihapus) tetap ada.
            Assert.True(await verify.PackageUserResponses.AnyAsync(r => r.PackageOptionId == optionIds[0]));
        }
    }
}
