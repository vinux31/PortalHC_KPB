// Phase 415 SEC-01 (Wave-0) — data-layer integration tests untuk AssessmentPackageSection.
// Membuktikan, pada SQL Server NYATA (bukan InMemory), bahwa migration 415 menegakkan:
//   1. Unique index (AssessmentPackageId, SectionNumber) → duplikat melempar DbUpdateException
//      (Phase 404 lesson: non-filtered unique → DbUpdateException).
//   2. Distinct SectionNumber dalam satu paket boleh.
//   3. FK Question->Section = SET NULL: hapus Section men-set SectionId soal jadi NULL, soal TETAP ada.
//   4. PackageQuestion dengan SectionId=null persist normal (legacy / "Lainnya").
// Fokus murni data-layer (tanpa controller). Fresh DbContext per-assertion (mirror pola integration lain).
using System;
using System.Collections.Generic;
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
public class SectionCrudTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public SectionCrudTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Controller construction (Phase 415-02): drive REAL AssessmentAdminController endpoints.
    //      Stub UserManager (audit GetUserAsync) + no-op notif/hub; pola FlexibleParticipantAddLiveWriteTests.
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

    // Stub IWebHostEnvironment: CreateQuestion mengevaluasi _env.WebRootPath untuk arg SaveFileAsync
    // (SaveFileAsync sendiri return null saat file==null → path TIDAK pernah ditulis di test ini).
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

    // Instantiate AssessmentAdminController ASLI atas SQLEXPRESS ctx. Section CRUD + CreateQuestion hanya
    // memakai _context / _userManager (audit) / _auditLog / _logger → service lain (env/notif/grading) AMAN null.
    // hubContext non-null (NoopHubContext) sebagai defensif (tak diakses oleh Section endpoint).
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

    // Actor untuk audit (GetUserAsync); seed agar FK audit-log aman.
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

    // ---- Seed helpers ----
    private static async Task<int> SeedPackageAsync(ApplicationDbContext ctx)
    {
        // AssessmentSession.UserId punya FK ke Users.Id → seed user dulu (pola FlexibleParticipantAddTests).
        var user = new ApplicationUser
        {
            UserName = "sec-" + Guid.NewGuid().ToString("N")[..8],
            Email = "sec@test.local",
            FullName = "Section Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var session = new AssessmentSession
        {
            UserId = user.Id,
            Title = "Sec-" + Guid.NewGuid().ToString("N")[..8],
            Category = "OJT",
            Status = "Open",
            AccessToken = "",
            Schedule = DateTime.UtcNow,
            DurationMinutes = 60,
            PassPercentage = 70,
            Progress = 0
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage
        {
            AssessmentSessionId = session.Id,
            PackageName = "Paket A",
            PackageNumber = 1
        };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        return pkg.Id;
    }

    // (1) Unique index (AssessmentPackageId, SectionNumber) ditegakkan → duplikat = DbUpdateException.
    [Fact]
    public async Task DuplicateSectionNumber_SamePackage_ThrowsDbUpdateException()
    {
        int packageId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            seed.AssessmentPackageSections.Add(new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Pompa" });
            await seed.SaveChangesAsync();
        }

        await using var dup = NewCtx();
        dup.AssessmentPackageSections.Add(new AssessmentPackageSection
        { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Kompresor" });

        // Plain non-filtered unique → DbUpdateException (Phase 404 lesson).
        await Assert.ThrowsAsync<DbUpdateException>(() => dup.SaveChangesAsync());
    }

    // (2) SectionNumber berbeda dalam satu paket → boleh.
    [Fact]
    public async Task DistinctSectionNumbers_SamePackage_Succeed()
    {
        int packageId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            seed.AssessmentPackageSections.Add(new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Pompa" });
            seed.AssessmentPackageSections.Add(new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 2, Name = "Kompresor" });
            await seed.SaveChangesAsync();
        }

        await using var verify = NewCtx();
        var count = await verify.AssessmentPackageSections
            .CountAsync(s => s.AssessmentPackageId == packageId);
        Assert.Equal(2, count);
    }

    // (3) FK SetNull: hapus Section men-set SectionId soal jadi NULL — soal TIDAK ikut terhapus.
    [Fact]
    public async Task DeleteSection_SetsQuestionSectionIdToNull_QuestionsRemain()
    {
        int packageId, sectionId, q1Id, q2Id;

        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            var section = new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Pompa" };
            seed.AssessmentPackageSections.Add(section);
            await seed.SaveChangesAsync();
            sectionId = section.Id;

            var q1 = new PackageQuestion { AssessmentPackageId = packageId, QuestionText = "Q1", Order = 1, SectionId = sectionId };
            var q2 = new PackageQuestion { AssessmentPackageId = packageId, QuestionText = "Q2", Order = 2, SectionId = sectionId };
            seed.PackageQuestions.AddRange(q1, q2);
            await seed.SaveChangesAsync();
            q1Id = q1.Id; q2Id = q2.Id;
        }

        // Hapus Section.
        await using (var del = NewCtx())
        {
            var sec = await del.AssessmentPackageSections.FindAsync(sectionId);
            del.AssessmentPackageSections.Remove(sec!);
            await del.SaveChangesAsync();
        }

        // Reload: soal tetap ada, SectionId di-set NULL oleh FK SET NULL.
        await using var verify = NewCtx();
        var sectionGone = await verify.AssessmentPackageSections.AnyAsync(s => s.Id == sectionId);
        Assert.False(sectionGone);

        var q1Reload = await verify.PackageQuestions.FirstOrDefaultAsync(q => q.Id == q1Id);
        var q2Reload = await verify.PackageQuestions.FirstOrDefaultAsync(q => q.Id == q2Id);
        Assert.NotNull(q1Reload);                 // soal TIDAK terhapus
        Assert.NotNull(q2Reload);
        Assert.Null(q1Reload!.SectionId);         // SectionId di-set NULL
        Assert.Null(q2Reload!.SectionId);
    }

    // (4) PackageQuestion dengan SectionId=null persist normal (legacy / "Lainnya").
    [Fact]
    public async Task QuestionWithNullSection_PersistsFine()
    {
        int packageId, qId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            var q = new PackageQuestion { AssessmentPackageId = packageId, QuestionText = "Lainnya", Order = 1, SectionId = null };
            seed.PackageQuestions.Add(q);
            await seed.SaveChangesAsync();
            qId = q.Id;
        }

        await using var verify = NewCtx();
        var reload = await verify.PackageQuestions.FirstOrDefaultAsync(q => q.Id == qId);
        Assert.NotNull(reload);
        Assert.Null(reload!.SectionId);
    }

    // =================================================================================================
    //  Phase 415-02 — Controller-driven integration (SEC-01/02/03). Drive REAL endpoints (de-tautology:
    //  NO replica of CRUD logic); assert kolom DB nyata dengan fresh context. RBAC/antiforgery tak
    //  ditegakkan di harness unit, tapi action body (persist + FK SetNull + dup-precheck) DIJALANKAN.
    // =================================================================================================

    // (5) CreateSection ASLI menyimpan section dgn toggle; duplikat (packageId, sectionNumber) → TempData error, NO row kedua.
    [Fact]
    public async Task CreateSection_Persists_WithToggles_DuplicateRejected()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // CreateSection #1 (StartNewPage=true, ShuffleEnabled=false → bukan default).
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CreateSection");
            var res = await ctrl.CreateSection(packageId, sectionNumber: 1, name: "Pompa", startNewPage: true, shuffleEnabled: false);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var s = await verify.AssessmentPackageSections
                .SingleAsync(x => x.AssessmentPackageId == packageId && x.SectionNumber == 1);
            Assert.Equal("Pompa", s.Name);
            Assert.True(s.StartNewPage);
            Assert.False(s.ShuffleEnabled);
        }

        // CreateSection #2 dengan sectionNumber sama → reject (NO row kedua, masih 1 total).
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CreateSection");
            var res = await ctrl.CreateSection(packageId, sectionNumber: 1, name: "Kompresor", startNewPage: false, shuffleEnabled: true);
            Assert.IsType<RedirectToActionResult>(res);
            Assert.NotNull(ctrl.TempData["Error"]);
        }

        await using (var verify = NewCtx())
        {
            var count = await verify.AssessmentPackageSections.CountAsync(x => x.AssessmentPackageId == packageId);
            Assert.Equal(1, count);   // duplikat ditolak → tetap 1
        }
    }

    // (6) EditSection ASLI memperbarui Nama + toggle.
    [Fact]
    public async Task EditSection_Updates_NameAndToggles()
    {
        int packageId, sectionId; string actorId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
            var s = new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Lama", StartNewPage = false, ShuffleEnabled = true };
            seed.AssessmentPackageSections.Add(s);
            await seed.SaveChangesAsync();
            sectionId = s.Id;
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "EditSection");
            var res = await ctrl.EditSection(sectionId, packageId, sectionNumber: 2, name: "Baru", startNewPage: true, shuffleEnabled: false);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var s = await verify.AssessmentPackageSections.FindAsync(sectionId);
            Assert.NotNull(s);
            Assert.Equal(2, s!.SectionNumber);
            Assert.Equal("Baru", s.Name);
            Assert.True(s.StartNewPage);
            Assert.False(s.ShuffleEnabled);
        }
    }

    // (7) SetAllSectionsNewPage ASLI men-set StartNewPage=true untuk SEMUA section di paket.
    [Fact]
    public async Task SetAllSectionsNewPage_SetsTrue_ForEverySection()
    {
        int packageId; string actorId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
            seed.AssessmentPackageSections.AddRange(
                new AssessmentPackageSection { AssessmentPackageId = packageId, SectionNumber = 1, StartNewPage = false },
                new AssessmentPackageSection { AssessmentPackageId = packageId, SectionNumber = 2, StartNewPage = false },
                new AssessmentPackageSection { AssessmentPackageId = packageId, SectionNumber = 3, StartNewPage = false });
            await seed.SaveChangesAsync();
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "SetAllSectionsNewPage");
            var res = await ctrl.SetAllSectionsNewPage(packageId);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            var all = await verify.AssessmentPackageSections.Where(s => s.AssessmentPackageId == packageId).ToListAsync();
            Assert.Equal(3, all.Count);
            Assert.All(all, s => Assert.True(s.StartNewPage));
        }
    }

    // (8) DeleteSection ASLI menghapus section; soal-nya SectionId=null (FK SetNull) — soal TETAP ada.
    [Fact]
    public async Task DeleteSection_ViaController_QuestionsSurvive_SectionIdNull()
    {
        int packageId, sectionId, qId; string actorId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
            var s = new AssessmentPackageSection { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Hapus" };
            seed.AssessmentPackageSections.Add(s);
            await seed.SaveChangesAsync();
            sectionId = s.Id;

            var q = new PackageQuestion { AssessmentPackageId = packageId, QuestionText = "Q-dalam-section", Order = 1, SectionId = sectionId };
            seed.PackageQuestions.Add(q);
            await seed.SaveChangesAsync();
            qId = q.Id;
        }

        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "DeleteSection");
            var res = await ctrl.DeleteSection(sectionId, packageId);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using (var verify = NewCtx())
        {
            Assert.False(await verify.AssessmentPackageSections.AnyAsync(s => s.Id == sectionId));
            var q = await verify.PackageQuestions.FindAsync(qId);
            Assert.NotNull(q);              // soal TIDAK terhapus
            Assert.Null(q!.SectionId);      // FK SetNull → jadi "Lainnya"
        }
    }

    // (9) CreateQuestion ASLI dengan sectionId menetapkan soal ke section; dengan null → "Lainnya".
    [Fact]
    public async Task CreateQuestion_AssignsSection_OrLeavesUngrouped()
    {
        int packageId, sectionId; string actorId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            actorId = (await SeedActorAsync(seed)).Id;
            var s = new AssessmentPackageSection { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Sec" };
            seed.AssessmentPackageSections.Add(s);
            await seed.SaveChangesAsync();
            sectionId = s.Id;
        }

        // Soal A: di-assign ke section. Soal B: sectionId=null → Lainnya. MultipleChoice valid (1 benar, ≥2 opsi).
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CreateQuestion");
            // Phase 418: kontrak baru — List<OptionInput> options + correctIndex (MC single-select).
            // Param order: ...maxChars(7), questionImage+alt(2), options, correctIndex, sectionId.
            var resA = await ctrl.CreateQuestion(
                packageId, "Soal A", "MultipleChoice", 10, null, null, 2000,
                null, null,
                new List<OptionInput>
                {
                    new OptionInput { Text = "opsi1" },
                    new OptionInput { Text = "opsi2" }
                },
                correctIndex: 0,
                sectionId: sectionId);
            Assert.IsType<RedirectToActionResult>(resA);

            var resB = await ctrl.CreateQuestion(
                packageId, "Soal B", "MultipleChoice", 10, null, null, 2000,
                null, null,
                new List<OptionInput>
                {
                    new OptionInput { Text = "opsi1" },
                    new OptionInput { Text = "opsi2" }
                },
                correctIndex: 1,
                sectionId: null);
            Assert.IsType<RedirectToActionResult>(resB);
        }

        await using (var verify = NewCtx())
        {
            var qA = await verify.PackageQuestions.SingleAsync(q => q.AssessmentPackageId == packageId && q.QuestionText == "Soal A");
            var qB = await verify.PackageQuestions.SingleAsync(q => q.AssessmentPackageId == packageId && q.QuestionText == "Soal B");
            Assert.Equal(sectionId, qA.SectionId);   // ter-assign
            Assert.Null(qB.SectionId);                // Lainnya
        }
    }
}
