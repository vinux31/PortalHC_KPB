// Phase 415 SEC-06 (Wave-4) — Integration tests untuk deep-clone Section saat sync Pre→Post.
// Drive REAL CopyPackagesFromPre (yang memanggil private SyncPackagesToPost) atas SQL Server NYATA
// (FK remap + ID baru butuh real-SQL, bukan InMemory). De-tautology: NO replica logika clone di test.
//
// Membuktikan:
//   1. Post package punya record AssessmentPackageSection hasil clone (SectionNumber/Name/StartNewPage/
//      ShuffleEnabled identik Pre).
//   2. SectionId soal Post menunjuk ke Section milik paket POST (AssessmentPackageId == postPackage.Id),
//      BUKAN ke Section paket Pre (no cross-package leak, Pitfall 8).
//   3. Soal SectionId=null ("Lainnya") tetap null setelah clone.
//   4. Opsi 5–6 (E/F) ikut tersalin (existing q.Options.Select clone SEMUA opsi).
//   5. Re-sync (CopyPackagesFromPre dipanggil 2×) tidak meninggalkan Section Post lama (no stale rows).
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
public class SectionSyncPrePostTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public SectionSyncPrePostTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Controller harness (mirror SectionCrudTests) ----
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
        public string WebRootPath { get; set; } = System.IO.Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ApplicationName { get; set; } = "HcPortal.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = System.IO.Path.GetTempPath();
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

    // Seed sepasang Pre+Post yang ter-link (Post.LinkedSessionId = Pre.Id). Return (preSessionId, postSessionId).
    private static async Task<(int preId, int postId, int prePkgId)> SeedLinkedPrePostAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "sync-" + Guid.NewGuid().ToString("N")[..8],
            Email = "sync@test.local", FullName = "Sync Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var title = "Sync-" + Guid.NewGuid().ToString("N")[..8];
        var schedule = DateTime.UtcNow;

        var pre = new AssessmentSession
        {
            UserId = user.Id, Title = title, Category = "OJT", AssessmentType = "PreTest",
            Status = "Open", AccessToken = "", Schedule = schedule, DurationMinutes = 60, PassPercentage = 70, Progress = 0
        };
        ctx.AssessmentSessions.Add(pre);
        await ctx.SaveChangesAsync();

        var post = new AssessmentSession
        {
            UserId = user.Id, Title = title, Category = "OJT", AssessmentType = "PostTest",
            Status = "Open", AccessToken = "", Schedule = schedule, DurationMinutes = 60, PassPercentage = 70, Progress = 0,
            LinkedSessionId = pre.Id
        };
        ctx.AssessmentSessions.Add(post);
        await ctx.SaveChangesAsync();

        // Pre package + 2 Section + soal: Section 1 (1 soal MC dgn 6 opsi A–F), Section 2 (1 soal), + 1 soal Lainnya.
        var prePkg = new AssessmentPackage { AssessmentSessionId = pre.Id, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(prePkg);
        await ctx.SaveChangesAsync();

        var sec1 = new AssessmentPackageSection { AssessmentPackageId = prePkg.Id, SectionNumber = 1, Name = "Pompa", StartNewPage = true, ShuffleEnabled = false };
        var sec2 = new AssessmentPackageSection { AssessmentPackageId = prePkg.Id, SectionNumber = 2, Name = "Kompresor", StartNewPage = false, ShuffleEnabled = true };
        ctx.AssessmentPackageSections.AddRange(sec1, sec2);
        await ctx.SaveChangesAsync();

        // Soal Section 1: MC dgn 6 opsi (A–F) → membuktikan opsi 5–6 ikut tersalin.
        var qSec1 = new PackageQuestion
        {
            AssessmentPackageId = prePkg.Id, QuestionText = "Q-Sec1-6opsi", Order = 1, ScoreValue = 10,
            QuestionType = "MultipleChoice", SectionId = sec1.Id,
            Options = new List<PackageOption>
            {
                new() { OptionText = "A", IsCorrect = false },
                new() { OptionText = "B", IsCorrect = false },
                new() { OptionText = "C", IsCorrect = true  },
                new() { OptionText = "D", IsCorrect = false },
                new() { OptionText = "E", IsCorrect = false },
                new() { OptionText = "F", IsCorrect = false },
            }
        };
        var qSec2 = new PackageQuestion
        {
            AssessmentPackageId = prePkg.Id, QuestionText = "Q-Sec2", Order = 2, ScoreValue = 10,
            QuestionType = "MultipleChoice", SectionId = sec2.Id,
            Options = new List<PackageOption> { new() { OptionText = "Ya", IsCorrect = true }, new() { OptionText = "Tidak", IsCorrect = false } }
        };
        var qLainnya = new PackageQuestion
        {
            AssessmentPackageId = prePkg.Id, QuestionText = "Q-Lainnya", Order = 3, ScoreValue = 10,
            QuestionType = "MultipleChoice", SectionId = null,
            Options = new List<PackageOption> { new() { OptionText = "Ya", IsCorrect = true }, new() { OptionText = "Tidak", IsCorrect = false } }
        };
        ctx.PackageQuestions.AddRange(qSec1, qSec2, qLainnya);
        await ctx.SaveChangesAsync();

        return (pre.Id, post.Id, prePkg.Id);
    }

    // (1) + (2) + (3) + (4): clone Section + remap SectionId ke Section POST + opsi 5–6 + Lainnya tetap null.
    [Fact]
    public async Task CopyPackagesFromPre_ClonesSections_RemapsToPostSections_CarriesOptionsEF()
    {
        int postId; string actorId;
        await using (var seed = NewCtx())
        {
            var ids = await SeedLinkedPrePostAsync(seed);
            postId = ids.postId;
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Drive CopyPackagesFromPre ASLI (memanggil private SyncPackagesToPost).
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CopyPackagesFromPre");
            var res = await ctrl.CopyPackagesFromPre(postId);
            Assert.IsType<RedirectToActionResult>(res);
        }

        await using var verify = NewCtx();
        var postPkg = await verify.AssessmentPackages
            .Include(p => p.Questions).ThenInclude(q => q.Options)
            .SingleAsync(p => p.AssessmentSessionId == postId);

        // (1) Section rows ter-clone ke paket Post dgn nilai identik.
        var postSections = await verify.AssessmentPackageSections
            .Where(s => s.AssessmentPackageId == postPkg.Id)
            .OrderBy(s => s.SectionNumber)
            .ToListAsync();
        Assert.Equal(2, postSections.Count);
        Assert.Equal(1, postSections[0].SectionNumber);
        Assert.Equal("Pompa", postSections[0].Name);
        Assert.True(postSections[0].StartNewPage);
        Assert.False(postSections[0].ShuffleEnabled);
        Assert.Equal(2, postSections[1].SectionNumber);
        Assert.Equal("Kompresor", postSections[1].Name);
        Assert.False(postSections[1].StartNewPage);
        Assert.True(postSections[1].ShuffleEnabled);

        var postSectionIds = postSections.Select(s => s.Id).ToHashSet();

        // (2) SectionId soal Post menunjuk ke Section POST (bukan Pre). Tiap soal ber-section: SectionId ∈ postSectionIds.
        var qSec1 = postPkg.Questions.Single(q => q.QuestionText == "Q-Sec1-6opsi");
        var qSec2 = postPkg.Questions.Single(q => q.QuestionText == "Q-Sec2");
        var qLainnya = postPkg.Questions.Single(q => q.QuestionText == "Q-Lainnya");

        Assert.NotNull(qSec1.SectionId);
        Assert.Contains(qSec1.SectionId!.Value, postSectionIds);
        Assert.NotNull(qSec2.SectionId);
        Assert.Contains(qSec2.SectionId!.Value, postSectionIds);

        // Section yang ditunjuk soal Post benar-benar milik paket Post (assert AssessmentPackageId == postPkg.Id).
        var sec1Owner = await verify.AssessmentPackageSections.FindAsync(qSec1.SectionId.Value);
        Assert.Equal(postPkg.Id, sec1Owner!.AssessmentPackageId);
        // Remap benar: SectionNumber soal Post == SectionNumber asal di Pre (1 dan 2).
        Assert.Equal(1, sec1Owner.SectionNumber);

        // (3) Soal "Lainnya" SectionId tetap null.
        Assert.Null(qLainnya.SectionId);

        // (4) Opsi 5–6 (E/F) ter-clone: soal Section 1 punya 6 opsi.
        Assert.Equal(6, qSec1.Options.Count);
        Assert.Contains(qSec1.Options, o => o.OptionText == "E");
        Assert.Contains(qSec1.Options, o => o.OptionText == "F");
        Assert.Contains(qSec1.Options, o => o.OptionText == "C" && o.IsCorrect);

        // No cross-leak: tidak ada soal Post yang SectionId-nya menunjuk Section milik paket Pre.
        var prePkgSectionIds = await verify.AssessmentPackageSections
            .Where(s => s.AssessmentPackageId != postPkg.Id)
            .Select(s => s.Id)
            .ToListAsync();
        foreach (var q in postPkg.Questions.Where(q => q.SectionId.HasValue))
            Assert.DoesNotContain(q.SectionId!.Value, prePkgSectionIds);
    }

    // (5) Re-sync: CopyPackagesFromPre dipanggil 2× → tidak ada stale Section Post lama (count konsisten).
    [Fact]
    public async Task CopyPackagesFromPre_ResyncTwice_NoStalePostSections()
    {
        int postId; string actorId;
        await using (var seed = NewCtx())
        {
            var ids = await SeedLinkedPrePostAsync(seed);
            postId = ids.postId;
            actorId = (await SeedActorAsync(seed)).Id;
        }

        // Sync #1
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CopyPackagesFromPre");
            await ctrl.CopyPackagesFromPre(postId);
        }
        // Sync #2 (re-sync — hapus Post lama + clone ulang).
        await using (var ctx = NewCtx())
        {
            var actor = await ctx.Users.FindAsync(actorId);
            var ctrl = MakeController(ctx, actor!, "CopyPackagesFromPre");
            await ctrl.CopyPackagesFromPre(postId);
        }

        await using var verify = NewCtx();
        var postPkgs = await verify.AssessmentPackages.Where(p => p.AssessmentSessionId == postId).ToListAsync();
        Assert.Single(postPkgs);   // tetap 1 paket Post (lama terhapus, baru di-clone)

        var postPkgId = postPkgs[0].Id;
        var postSectionCount = await verify.AssessmentPackageSections.CountAsync(s => s.AssessmentPackageId == postPkgId);
        Assert.Equal(2, postSectionCount);   // tepat 2 Section (no stale: bukan 4)

        // Tidak ada Section yatim (AssessmentPackageId menunjuk paket yang sudah tak ada).
        var allPkgIds = await verify.AssessmentPackages.Select(p => p.Id).ToListAsync();
        var orphanSections = await verify.AssessmentPackageSections
            .Where(s => !allPkgIds.Contains(s.AssessmentPackageId))
            .CountAsync();
        Assert.Equal(0, orphanSections);
    }
}
