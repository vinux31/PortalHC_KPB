// Phase 415 SEC-04 / D-13 titik #2 (Wave-4) — Integration tests untuk re-guard struktur Section di
// CMPController.StartExam SEBELUM BuildQuestionAssignment. Drive REAL StartExam atas SQL Server NYATA
// (de-tautology: NO replica logika compare di test — guard ASLI yang memblok/melewatkan).
//
// Cover:
//   (a) 2 paket saudara, Section 1 = 3 vs 2 soal → BLOK (redirect Assessment + pesan re-guard).
//   (b) Count cocok per-Section → LOLOS (assignment ter-build; bukan redirect re-guard).
//   (c) Semua SectionId null kedua paket (legacy) → LOLOS (kompatibel-mundur).
//   (d) Paket tunggal (no sibling) → LOLOS.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Hubs;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class SectionMismatchGuardTests : IClassFixture<SectionFixture>
{
    private const string DriftError = "Ujian tidak dapat dimulai: struktur Section antar-paket tidak identik. Hubungi HC untuk memperbaiki paket soal.";

    private readonly SectionFixture _fixture;
    public SectionMismatchGuardTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Stubs untuk CMPController.StartExam ----

    // UserManager: GetUserAsync → worker seeded; GetRolesAsync → role "Pekerja" (level worker, non-admin).
    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        private readonly ApplicationUser _user;
        public StubUserManager(ApplicationUser user)
            : base(new StubUserStore(), null!, null!, null!, null!, null!, null!, null!, null!)
            => _user = user;
        public override Task<ApplicationUser?> GetUserAsync(System.Security.Claims.ClaimsPrincipal principal)
            => Task.FromResult<ApplicationUser?>(_user);
        public override Task<IList<string>> GetRolesAsync(ApplicationUser user)
            => Task.FromResult<IList<string>>(new List<string> { "Pekerja" });
        public override Task<ApplicationUser?> FindByIdAsync(string userId)
            => Task.FromResult<ApplicationUser?>(_user.Id == userId ? _user : null);
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

    // Stub ISession yang TIDAK impersonating (Mode unset → GetString null). ImpersonationService butuh Session.
    private sealed class StubSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => "stub";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public Task LoadAsync(System.Threading.CancellationToken ct = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
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

    private CMPController MakeController(ApplicationDbContext ctx, ApplicationUser worker)
    {
        var httpContext = new DefaultHttpContext { Session = new StubSession() };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var impersonation = new ImpersonationService(accessor);

        var auditLog = new AuditLogService(ctx);
        var cache = new MemoryCache(new MemoryCacheOptions());
        #pragma warning disable CS8625
        var ctrl = new CMPController(
            userManager:          new StubUserManager(worker),
            roleManager:          null!,
            signInManager:        null!,
            context:              ctx,
            env:                  new StubWebHostEnvironment(),
            auditLog:             auditLog,
            cache:                cache,
            logger:               NullLogger<CMPController>.Instance,
            notificationService:  null!,
            hubContext:           new NoopHubContext(),
            scopeFactory:         null!,
            workerDataService:    null!,
            gradingService:       null!,
            impersonationService: impersonation);
        #pragma warning restore CS8625
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            ActionDescriptor = new ControllerActionDescriptor { ActionName = "StartExam" }
        };
        ctrl.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        ctrl.TempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        return ctrl;
    }

    // Seed seorang worker. Return ApplicationUser (id dipakai sebagai UserId sesi yang akan di-StartExam).
    private static async Task<ApplicationUser> SeedWorkerAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser
        {
            UserName = "wkr-" + Guid.NewGuid().ToString("N")[..8],
            Email = "wkr@test.local", FullName = "Worker Test", NIP = "12345", IsActive = true, RoleLevel = 6
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u;
    }

    // Seed sesi InProgress (StartedAt != null → justStarted=false: hindari jalur write-on-GET/broadcast/log).
    // Token off, window terbuka, durasi >0, RemovedAt null. Return session.
    private static async Task<AssessmentSession> SeedSessionAsync(ApplicationDbContext ctx, string userId, string title, DateTime schedule)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = "OJT", AssessmentType = "Standard",
            Status = "InProgress", StartedAt = DateTime.UtcNow.AddMinutes(-1),
            AccessToken = "", IsTokenRequired = false, Schedule = schedule,
            DurationMinutes = 60, PassPercentage = 70, Progress = 0,
            ShuffleQuestions = false, ShuffleOptions = false
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s;
    }

    // Tambah paket ke sesi + soal dengan distribusi Section tertentu.
    // sectionCounts: dict SectionNumber(null=Lainnya) → jumlah soal. Section row dibuat utk key non-null.
    private static async Task AddPackageWithSectionsAsync(ApplicationDbContext ctx, int sessionId, int pkgNumber,
        IEnumerable<(int? sectionNumber, int count)> dist)
    {
        var pkg = new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = $"Paket {pkgNumber}", PackageNumber = pkgNumber };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        int order = 1;
        foreach (var (sectionNumber, count) in dist)
        {
            int? sectionId = null;
            if (sectionNumber.HasValue)
            {
                var sec = new AssessmentPackageSection { AssessmentPackageId = pkg.Id, SectionNumber = sectionNumber.Value, Name = $"Sec{sectionNumber}" };
                ctx.AssessmentPackageSections.Add(sec);
                await ctx.SaveChangesAsync();
                sectionId = sec.Id;
            }
            for (int i = 0; i < count; i++)
            {
                ctx.PackageQuestions.Add(new PackageQuestion
                {
                    AssessmentPackageId = pkg.Id, QuestionText = $"P{pkgNumber}-S{sectionNumber}-Q{i}", Order = order++,
                    ScoreValue = 10, QuestionType = "MultipleChoice", SectionId = sectionId,
                    Options = new List<PackageOption> { new() { OptionText = "A", IsCorrect = true }, new() { OptionText = "B", IsCorrect = false } }
                });
            }
            await ctx.SaveChangesAsync();
        }
    }

    // (a) Mismatch per-Section → blok dengan pesan re-guard.
    [Fact]
    public async Task StartExam_SectionCountDriftAcrossSiblings_BlocksWithReguardMessage()
    {
        string workerId, title; int sessionAId;
        var schedule = DateTime.UtcNow;
        await using (var seed = NewCtx())
        {
            var w = await SeedWorkerAsync(seed);
            workerId = w.Id;
            title = "Drift-" + Guid.NewGuid().ToString("N")[..8];

            // Sesi A (yang di-StartExam) + sesi saudara B (sibling key Title+Category+Schedule.Date).
            var sA = await SeedSessionAsync(seed, workerId, title, schedule);
            var sB = await SeedSessionAsync(seed, workerId, title, schedule);
            sessionAId = sA.Id;

            // Paket A: Section 1 = 3 soal. Paket B: Section 1 = 2 soal → mismatch.
            await AddPackageWithSectionsAsync(seed, sA.Id, 1, new[] { ((int?)1, 3) });
            await AddPackageWithSectionsAsync(seed, sB.Id, 2, new[] { ((int?)1, 2) });
        }

        await using var ctx = NewCtx();
        var worker = await ctx.Users.FindAsync(workerId);
        var ctrl = MakeController(ctx, worker!);
        var res = await ctrl.StartExam(sessionAId);

        var redirect = Assert.IsType<RedirectToActionResult>(res);
        Assert.Equal("Assessment", redirect.ActionName);
        Assert.Equal(DriftError, ctrl.TempData["Error"]);

        // Tidak ada assignment ter-build (guard memblok SEBELUM BuildQuestionAssignment).
        await using var verify = NewCtx();
        Assert.False(await verify.UserPackageAssignments.AnyAsync(a => a.AssessmentSessionId == sessionAId));
    }

    // (b) Count cocok per-Section → lolos (assignment ter-build, bukan redirect re-guard).
    [Fact]
    public async Task StartExam_SectionCountsMatch_PassesAndBuildsAssignment()
    {
        string workerId, title; int sessionAId;
        var schedule = DateTime.UtcNow;
        await using (var seed = NewCtx())
        {
            var w = await SeedWorkerAsync(seed);
            workerId = w.Id;
            title = "Match-" + Guid.NewGuid().ToString("N")[..8];

            var sA = await SeedSessionAsync(seed, workerId, title, schedule);
            var sB = await SeedSessionAsync(seed, workerId, title, schedule);
            sessionAId = sA.Id;

            // Kedua paket: Section 1 = 2 soal, Section 2 = 1 soal → identik.
            await AddPackageWithSectionsAsync(seed, sA.Id, 1, new[] { ((int?)1, 2), ((int?)2, 1) });
            await AddPackageWithSectionsAsync(seed, sB.Id, 2, new[] { ((int?)1, 2), ((int?)2, 1) });
        }

        await using var ctx = NewCtx();
        var worker = await ctx.Users.FindAsync(workerId);
        var ctrl = MakeController(ctx, worker!);
        var res = await ctrl.StartExam(sessionAId);

        // Tidak memblok dengan pesan re-guard.
        Assert.NotEqual(DriftError, ctrl.TempData["Error"] as string);

        // Assignment ter-build untuk sesi A (guard lolos → BuildQuestionAssignment jalan + persist).
        await using var verify = NewCtx();
        Assert.True(await verify.UserPackageAssignments.AnyAsync(a => a.AssessmentSessionId == sessionAId));
    }

    // (c) Legacy: semua SectionId null kedua paket → lolos (guard tidak fire).
    [Fact]
    public async Task StartExam_LegacyAllNullSections_Passes()
    {
        string workerId, title; int sessionAId;
        var schedule = DateTime.UtcNow;
        await using (var seed = NewCtx())
        {
            var w = await SeedWorkerAsync(seed);
            workerId = w.Id;
            title = "Legacy-" + Guid.NewGuid().ToString("N")[..8];

            var sA = await SeedSessionAsync(seed, workerId, title, schedule);
            var sB = await SeedSessionAsync(seed, workerId, title, schedule);
            sessionAId = sA.Id;

            // Tidak ada Section; jumlah TOTAL berbeda (3 vs 2) — guard re-guard TIDAK fire (all-null = legacy).
            // (Jumlah beda di legacy adalah kondisi lama; re-guard Section tak boleh memblok all-null.)
            await AddPackageWithSectionsAsync(seed, sA.Id, 1, new[] { ((int?)null, 3) });
            await AddPackageWithSectionsAsync(seed, sB.Id, 2, new[] { ((int?)null, 2) });
        }

        await using var ctx = NewCtx();
        var worker = await ctx.Users.FindAsync(workerId);
        var ctrl = MakeController(ctx, worker!);
        var res = await ctrl.StartExam(sessionAId);

        Assert.NotEqual(DriftError, ctrl.TempData["Error"] as string);
        await using var verify = NewCtx();
        Assert.True(await verify.UserPackageAssignments.AnyAsync(a => a.AssessmentSessionId == sessionAId));
    }

    // (d) Paket tunggal (no sibling) → lolos (tidak ada paket pembanding).
    [Fact]
    public async Task StartExam_SinglePackage_Passes()
    {
        string workerId, title; int sessionAId;
        var schedule = DateTime.UtcNow;
        await using (var seed = NewCtx())
        {
            var w = await SeedWorkerAsync(seed);
            workerId = w.Id;
            title = "Single-" + Guid.NewGuid().ToString("N")[..8];

            var sA = await SeedSessionAsync(seed, workerId, title, schedule);
            sessionAId = sA.Id;

            // Satu paket dengan Section → tetap lolos (tidak ada saudara untuk dibandingkan).
            await AddPackageWithSectionsAsync(seed, sA.Id, 1, new[] { ((int?)1, 3) });
        }

        await using var ctx = NewCtx();
        var worker = await ctx.Users.FindAsync(workerId);
        var ctrl = MakeController(ctx, worker!);
        var res = await ctrl.StartExam(sessionAId);

        Assert.NotEqual(DriftError, ctrl.TempData["Error"] as string);
        await using var verify = NewCtx();
        Assert.True(await verify.UserPackageAssignments.AnyAsync(a => a.AssessmentSessionId == sessionAId));
    }
}
