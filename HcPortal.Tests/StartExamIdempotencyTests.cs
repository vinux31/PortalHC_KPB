// StartExamIdempotencyTests — v32.8 Phase 428 Plan 01 (EXSEC-02).
// GET CMP/StartExam(id) idempoten untuk transisi status Upcoming->Open: tidak ada write-on-GET.
// Transisi dihitung in-memory (effective-status by-schedule); persist Upcoming->Open dihapus.
// 6 test real-SQL (disposable DB @localhost\SQLEXPRESS via RetakeServiceFixture, MigrateAsync full chain).
// Membuktikan 4 success-criteria EXSEC-02 + regresi token-gate 427:
//   T1 (SC#1, SC#2) Impersonate owner, Upcoming + waktu tiba -> ViewResult, Status DB tetap Upcoming, StartedAt null.
//   T2 (SC#1 idempoten) GET 2x berturut (impersonate) -> kedua ViewResult, Status DB tetap Upcoming.
//   T3 (SC#3 time-gate) Owner, Upcoming + belum waktunya -> Redirect Assessment, Status tetap Upcoming.
//   T4 (SC#3 GRDF-01) Owner, Post linked ke Pre status != Completed, waktu tiba -> Redirect + error Pre-Test, Status Upcoming.
//   T5 (SC#4) Owner non-impersonate, Upcoming waktu tiba -> ViewResult, Status InProgress + StartedAt + assignment ter-create.
//   T6 (regresi 427) Owner, IsTokenRequired + TokenVerifiedAt null + StartedAt null, waktu tiba -> Redirect token, Status Upcoming.
//
// KUNCI strategi (RESEARCH §3): GET owner non-impersonate pada Upcoming-waktu-tiba men-trigger justStarted
// InProgress write (worker mulai aktual). Jalur IMPERSONATION adalah satu-satunya GET non-starting
// (justStarted write di-guard !IsImpersonating()) -> observasi "Status DB tetap Upcoming pasca-GET" = bukti
// tak ada write-on-GET untuk transisi status (SC#1/#2). Pola factory disalin dari TokenVerifiedAtTests.cs.
// [Trait("Category","Integration")].
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class StartExamIdempotencyTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public StartExamIdempotencyTests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // Pesan error gate StartExam (untuk assert TempData["Error"]).
    private const string TimeGateMsg = "Ujian belum dibuka. Silakan kembali setelah waktu ujian dimulai.";
    private const string Grdf01Msg   = "Selesaikan Pre-Test dulu sebelum mulai Post-Test.";
    private const string TokenMsg    = "Ujian ini membutuhkan token akses. Silakan masukkan token terlebih dahulu.";

    // ---------- Fake user store (GetUserAsync via FindByIdAsync + IUserRoleStore empty) ----------
    private sealed class FakeUserStore : IUserStore<ApplicationUser>, IUserRoleStore<ApplicationUser>
    {
        private readonly Dictionary<string, ApplicationUser> _byId = new();
        public void Add(ApplicationUser u) => _byId[u.Id] = u;

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct)
            => Task.FromResult(_byId.TryGetValue(userId, out var u) ? u : null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
            => Task.FromResult<ApplicationUser?>(null);
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.UserName);
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.NormalizedUserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken ct) { user.UserName = userName; return Task.CompletedTask; }
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken ct) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct) { _byId[user.Id] = user; return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct) { _byId[user.Id] = user; return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) { _byId.Remove(user.Id); return Task.FromResult(IdentityResult.Success); }
        public void Dispose() { }

        public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.CompletedTask;
        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult<IList<string>>(new List<string>());
        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.FromResult(false);
        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken ct) => Task.FromResult<IList<ApplicationUser>>(new List<ApplicationUser>());
    }

    private sealed class StubSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => "stub";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static UserManager<ApplicationUser> MakeUserManager(FakeUserStore store)
        => new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

    /// <summary>CMPController non-impersonate (owner = signed-in user). Deps lain null!-substitute.</summary>
    private static (CMPController ctrl, ITempDataDictionary tempData) MakeCmp(
        ApplicationDbContext ctx, ApplicationUser signedInUser)
    {
        var store = new FakeUserStore();
        store.Add(signedInUser);
        var userManager = MakeUserManager(store);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new StubSession();   // no impersonation key → UseRealUser
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, signedInUser.Id)
        }, "TestAuth"));

        var impersonation = new ImpersonationService(new HttpContextAccessor { HttpContext = httpContext });
        var retakeService = new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);

        #pragma warning disable CS8625 // null-substitute: StartExam tak deref deps berikut.
        var ctrl = new CMPController(
            userManager, null!, null!, ctx, null!, null!, null!,
            NullLogger<CMPController>.Instance, null!, new NoOpHubContext(), null!, null!, null!,
            impersonation, retakeService);
        #pragma warning restore CS8625

        var tempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        ctrl.TempData = tempData;
        ctrl.Url = new StubUrlHelper(ctrl.ControllerContext);
        return (ctrl, tempData);
    }

    /// <summary>
    /// CMPController saat impersonate: admin (signed-in) view-as owner. justStarted/assignment write di-guard
    /// !IsImpersonating() → GET 100% read-only. Effective user = owner (GetEffectiveUserAsync → FindByIdAsync(owner.Id)).
    /// </summary>
    private static (CMPController ctrl, ITempDataDictionary tempData) MakeCmpImpersonating(
        ApplicationDbContext ctx, ApplicationUser signedInAdmin, ApplicationUser owner)
    {
        var store = new FakeUserStore();
        store.Add(signedInAdmin);
        store.Add(owner);                                   // WAJIB: GetEffectiveUserAsync → FindByIdAsync(owner.Id)
        var userManager = MakeUserManager(store);

        var httpContext = new DefaultHttpContext();
        var session = new StubSession();
        session.SetString(ImpersonationKeys.Mode, "user");                                  // IsImpersonating()=true
        session.SetString(ImpersonationKeys.TargetUserId, owner.Id);                        // effective user = owner
        session.SetString(ImpersonationKeys.TargetUserName, owner.FullName ?? "owner");
        session.SetString(ImpersonationKeys.StartedAt, DateTime.UtcNow.Ticks.ToString());   // !IsExpired() agar tak fallback UseRealUser
        httpContext.Session = session;
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, signedInAdmin.Id)
        }, "TestAuth"));

        var impersonation = new ImpersonationService(new HttpContextAccessor { HttpContext = httpContext });
        var retakeService = new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);
        #pragma warning disable CS8625
        var ctrl = new CMPController(
            userManager, null!, null!, ctx, null!, null!, null!,
            NullLogger<CMPController>.Instance, null!, new NoOpHubContext(), null!, null!, null!,
            impersonation, retakeService);
        #pragma warning restore CS8625
        var tempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        ctrl.TempData = tempData;
        ctrl.Url = new StubUrlHelper(ctrl.ControllerContext);
        return (ctrl, tempData);
    }

    /// <summary>IUrlHelper minimal: Url.Action(...) → string non-null. Tak render route asli.</summary>
    private sealed class StubUrlHelper : IUrlHelper
    {
        public StubUrlHelper(ActionContext ctx) => ActionContext = ctx;
        public ActionContext ActionContext { get; }
        public string? Action(UrlActionContext actionContext) => "/CMP/StartExam";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => "/";
        public string? RouteUrl(UrlRouteContext routeContext) => "/";
    }

    // ---------- Seed helpers ----------
    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext ctx, string label = "user")
    {
        var u = new ApplicationUser
        {
            UserName = label + "-" + Guid.NewGuid().ToString("N")[..8],
            Email = label + "@test.local",
            FullName = "Idem Test " + label,
            NIP = "9" + Guid.NewGuid().ToString("N")[..4]
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u;
    }

    // Schedule "waktu tiba" = lampau (vs nowWib). Schedule "belum waktunya" = masa depan (vs nowWib).
    private static readonly DateTime TimeArrived  = new DateTime(2026, 2, 1);
    private static DateTime TimeFuture => DateTime.UtcNow.AddHours(7).AddDays(7);

    /// <summary>Seed sesi fleksibel — kontrol Status/Schedule/type/token/link.</summary>
    private static async Task<int> SeedSessionAsync(
        ApplicationDbContext ctx, string userId, string title,
        string status, DateTime schedule,
        bool isTokenRequired = false, DateTime? tokenVerifiedAt = null,
        DateTime? startedAt = null, string? assessmentType = null,
        int? linkedSessionId = null, int? linkedGroupId = null)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = "Test", Status = status,
            Schedule = schedule, DurationMinutes = 60,
            // Kolom AccessToken NOT NULL di schema → selalu isi (irelevan saat IsTokenRequired=false; gate token off).
            IsTokenRequired = isTokenRequired, AccessToken = "ABC23X",
            TokenVerifiedAt = tokenVerifiedAt, StartedAt = startedAt,
            AssessmentType = assessmentType, LinkedSessionId = linkedSessionId, LinkedGroupId = linkedGroupId
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    /// <summary>Seed 1 package + 1 MC soal (1 opsi benar) agar StartExam mencapai jalur package → View(vm).</summary>
    private static async Task SeedPackageAsync(ApplicationDbContext ctx, int sessionId)
    {
        var pkg = new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "Paket A", PackageNumber = 1 };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        var q = new PackageQuestion { AssessmentPackageId = pkg.Id, QuestionText = "Soal 1", Order = 0, ScoreValue = 10, QuestionType = "MultipleChoice" };
        ctx.PackageQuestions.Add(q);
        await ctx.SaveChangesAsync();

        ctx.PackageOptions.AddRange(
            new PackageOption { PackageQuestionId = q.Id, OptionText = "Benar", IsCorrect = true },
            new PackageOption { PackageQuestionId = q.Id, OptionText = "Salah", IsCorrect = false });
        await ctx.SaveChangesAsync();
    }

    private async Task<(string Status, DateTime? StartedAt)> ReloadStatusAsync(int id)
    {
        await using var verify = NewCtx();
        return await verify.AssessmentSessions
            .Where(a => a.Id == id)
            .Select(a => new ValueTuple<string, DateTime?>(a.Status, a.StartedAt))
            .SingleAsync();
    }

    // ====================== T1 (SC#1, SC#2): impersonate, waktu tiba → render tanpa persist ======================
    [Fact]
    public async Task StartExam_Impersonate_TimeArrivedUpcoming_RendersWithoutPersisting()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx, "owner");
        var admin = await SeedUserAsync(ctx, "admin");
        var id = await SeedSessionAsync(ctx, owner.Id, "T1", status: "Upcoming", schedule: TimeArrived);
        await SeedPackageAsync(ctx, id);   // ada paket → StartExam → View(vm)

        var (ctrl, _) = MakeCmpImpersonating(ctx, admin, owner);
        var result = await ctrl.StartExam(id);

        Assert.IsType<ViewResult>(result);                 // effective-open: lolos gate (bukan redirect)

        var (status, startedAt) = await ReloadStatusAsync(id);
        Assert.Equal("Upcoming", status);                  // TIDAK berubah (no write-on-GET transisi status)
        Assert.Null(startedAt);                            // impersonate → justStarted write di-skip
    }

    // ====================== T2 (SC#1 idempoten): double-GET → Status tetap Upcoming ======================
    [Fact]
    public async Task StartExam_Impersonate_DoubleGet_StatusStaysUpcoming()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx, "owner");
        var admin = await SeedUserAsync(ctx, "admin");
        var id = await SeedSessionAsync(ctx, owner.Id, "T2", status: "Upcoming", schedule: TimeArrived);
        await SeedPackageAsync(ctx, id);

        var (ctrl, _) = MakeCmpImpersonating(ctx, admin, owner);
        var first = await ctrl.StartExam(id);
        var second = await ctrl.StartExam(id);

        Assert.IsType<ViewResult>(first);
        Assert.IsType<ViewResult>(second);

        var (status, _) = await ReloadStatusAsync(id);
        Assert.Equal("Upcoming", status);                  // stabil setelah 2 GET (idempoten)
    }

    // ====================== T3 (SC#3 time-gate): belum waktunya → blok + no write ======================
    [Fact]
    public async Task StartExam_Upcoming_NotYetTime_BlocksAndNoWrite()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx, "owner");
        var id = await SeedSessionAsync(ctx, owner.Id, "T3", status: "Upcoming", schedule: TimeFuture);

        var (ctrl, tempData) = MakeCmp(ctx, owner);
        var result = await ctrl.StartExam(id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Assessment", redirect.ActionName);
        Assert.Equal(TimeGateMsg, tempData["Error"]);

        var (status, _) = await ReloadStatusAsync(id);
        Assert.Equal("Upcoming", status);
    }

    // ====================== T4 (SC#3 GRDF-01): Post butuh Pre Completed → blok ======================
    [Fact]
    public async Task StartExam_PostTest_PreNotCompleted_Blocks()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx, "owner");
        // Pre milik owner, status InProgress (!= Completed) → GRDF-01 blok.
        var preId = await SeedSessionAsync(ctx, owner.Id, "T4Pre", status: "InProgress",
            schedule: TimeArrived, assessmentType: "PreTest");
        // Post milik owner, waktu tiba, linked ke Pre via LinkedSessionId (cabang kanonik FindPairedPreAsync).
        var postId = await SeedSessionAsync(ctx, owner.Id, "T4Post", status: "Upcoming",
            schedule: TimeArrived, assessmentType: "PostTest", linkedSessionId: preId);

        var (ctrl, tempData) = MakeCmp(ctx, owner);
        var result = await ctrl.StartExam(postId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Assessment", redirect.ActionName);
        Assert.Equal(Grdf01Msg, tempData["Error"]);

        var (status, _) = await ReloadStatusAsync(postId);
        Assert.Equal("Upcoming", status);                  // tetap Upcoming (terblok sebelum start)
    }

    // ====================== T5 (SC#4): owner waktu tiba → InProgress + StartedAt + assignment ======================
    [Fact]
    public async Task StartExam_Owner_TimeArrived_StartsInProgress()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx, "owner");
        var id = await SeedSessionAsync(ctx, owner.Id, "T5", status: "Upcoming", schedule: TimeArrived);
        await SeedPackageAsync(ctx, id);   // paket+soal → jalur package, assignment ter-create

        var (ctrl, _) = MakeCmp(ctx, owner);
        var result = await ctrl.StartExam(id);

        Assert.IsType<ViewResult>(result);                 // exam skeleton

        var (status, startedAt) = await ReloadStatusAsync(id);
        Assert.Equal("InProgress", status);                // worker mulai aktual (justStarted write)
        Assert.NotNull(startedAt);

        await using var verify = NewCtx();
        var hasAssignment = await verify.UserPackageAssignments.AnyAsync(a => a.AssessmentSessionId == id);
        Assert.True(hasAssignment);                        // exam-taking utuh
    }

    // ====================== T6 (regresi 427): token-required, unverified → blok ======================
    [Fact]
    public async Task StartExam_TokenRequired_NotVerified_Blocks()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx, "owner");
        var id = await SeedSessionAsync(ctx, owner.Id, "T6", status: "Upcoming", schedule: TimeArrived,
            isTokenRequired: true, tokenVerifiedAt: null, startedAt: null);

        var (ctrl, tempData) = MakeCmp(ctx, owner);
        var result = await ctrl.StartExam(id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Assessment", redirect.ActionName);
        Assert.Equal(TokenMsg, tempData["Error"]);

        var (status, _) = await ReloadStatusAsync(id);
        Assert.Equal("Upcoming", status);                  // tetap Upcoming (terblok token-gate)
    }
}
