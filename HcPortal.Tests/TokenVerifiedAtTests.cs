// TokenVerifiedAtTests — v32.8 Phase 427 Plan 01 (EXSEC-01).
// Token gate ujian server-authoritative via kolom AssessmentSession.TokenVerifiedAt (ganti TempData.Peek).
// 5 test real-SQL (disposable DB @localhost\SQLEXPRESS via RetakeServiceFixture, MigrateAsync full chain
// → otomatis apply AddTokenVerifiedAt). Membuktikan 4 success-criteria EXSEC-01:
//   T1 (SC#1) StartExam gate baca kolom: TokenVerifiedAt null → blokir (redirect Assessment + token error).
//   T2 (SC#1) TokenVerifiedAt set → lolos gate (proceeds — ViewResult, BUKAN redirect token-error).
//   T3 (SC#2) VerifyToken(token-required) sukses → stamp TokenVerifiedAt=UtcNow persist DB.
//   T4 (SC#3) RetakeService.ExecuteAsync → reset TokenVerifiedAt=null (single source D-01, re-arm).
//   T5 (SC#4) sesi legacy InProgress (StartedAt set, TokenVerifiedAt null) → lolos (no lockout, guard StartedAt==null).
//
// Pola CMPController factory + FakeUserStore/MakeUserManager/StubSession disalin dari RetakeExamEndpointTests.cs
// (VerifyToken & StartExam tidak deref deps null-substitute). Seed package (1 MC soal) untuk T2/T5 agar StartExam
// mencapai jalur package → View(vm) (ViewResult) = sinyal "lolos gate". [Trait("Category","Integration")].
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
public class TokenVerifiedAtTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public TokenVerifiedAtTests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // Token error message yang dihasilkan gate StartExam saat TokenVerifiedAt == null (CMPController:967).
    private const string TokenErrorMsg = "Ujian ini membutuhkan token akses. Silakan masukkan token terlebih dahulu.";

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

    /// <summary>Bangun CMPController dgn real ctx/userManager/impersonation/retakeService; deps lain null!-substitute.</summary>
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

        // hubContext = NoOpHubContext (StartExam broadcast "workerStarted" saat justStarted → tak boleh null-deref).
        // Url = StubUrlHelper (VerifyToken pakai Url.Action untuk redirectUrl JSON — IUrlHelper WAJIB non-null).
        #pragma warning disable CS8625 // null-substitute: VerifyToken & StartExam tak deref deps berikut.
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

    /// <summary>IUrlHelper minimal: Url.Action(...) → string non-null (VerifyToken redirectUrl). Tak render route asli.</summary>
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
    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "tva-" + Guid.NewGuid().ToString("N")[..8], Email = "tva@test.local", FullName = "Token Test", NIP = "99999" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u;
    }

    /// <summary>Seed sesi token-required. Schedule lampau + Status default Open agar lolos time-gate StartExam.</summary>
    private static async Task<int> SeedSessionAsync(
        ApplicationDbContext ctx, string userId, string title,
        string status = "Open", string accessToken = "ABC23X",
        DateTime? startedAt = null, DateTime? tokenVerifiedAt = null)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = "Test", Status = status,
            IsTokenRequired = true, AccessToken = accessToken,
            Schedule = new DateTime(2026, 2, 1),          // lampau → Upcoming time-gate tak men-trigger
            DurationMinutes = 60,                          // > 0 (guard StartExam)
            StartedAt = startedAt, TokenVerifiedAt = tokenVerifiedAt,
            AssessmentType = null                          // Standard → tak ada pairing Pre/Post (GRDF-01 lewat)
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

    // ====================== T1 (SC#1): gate baca kolom — null → blokir ======================
    [Fact]
    public async Task StartExam_TokenRequired_TokenVerifiedAtNull_Blocks()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx);
        var id = await SeedSessionAsync(ctx, owner.Id, "T1Title", status: "Open",
            startedAt: null, tokenVerifiedAt: null);

        var (ctrl, tempData) = MakeCmp(ctx, owner);
        var result = await ctrl.StartExam(id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Assessment", redirect.ActionName);
        Assert.True(tempData.ContainsKey("Error"));
        Assert.Equal(TokenErrorMsg, tempData["Error"]);   // gate token (BUKAN error lain)
    }

    // ====================== T2 (SC#1): TokenVerifiedAt set → lolos gate ======================
    [Fact]
    public async Task StartExam_TokenRequired_TokenVerifiedAtSet_Proceeds()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx);
        var id = await SeedSessionAsync(ctx, owner.Id, "T2Title", status: "Open",
            startedAt: null, tokenVerifiedAt: DateTime.UtcNow);
        await SeedPackageAsync(ctx, id);   // ada paket → StartExam → View(vm)

        var (ctrl, tempData) = MakeCmp(ctx, owner);
        var result = await ctrl.StartExam(id);

        // Lolos gate token: BUKAN redirect-ke-Assessment-dengan-error-token.
        Assert.IsType<ViewResult>(result);
        Assert.False(tempData.ContainsKey("Error") && (string?)tempData["Error"] == TokenErrorMsg);
    }

    // ====================== T3 (SC#2): VerifyToken sukses → stamp persist ======================
    [Fact]
    public async Task VerifyToken_CorrectToken_StampsTokenVerifiedAt()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx);
        var id = await SeedSessionAsync(ctx, owner.Id, "T3Title", status: "Open",
            accessToken: "ABC23X", startedAt: null, tokenVerifiedAt: null);

        var (ctrl, _) = MakeCmp(ctx, owner);
        var result = await ctrl.VerifyToken(id, "ABC23X");

        var json = Assert.IsType<JsonResult>(result);
        var success = (bool)json.Value!.GetType().GetProperty("success")!.GetValue(json.Value)!;
        Assert.True(success);

        // Reload dari DB (ctx fresh) → TokenVerifiedAt ter-persist (server-authoritative).
        await using var verify = NewCtx();
        var stamped = await verify.AssessmentSessions.Where(a => a.Id == id).Select(a => a.TokenVerifiedAt).SingleAsync();
        Assert.NotNull(stamped);
    }

    // ====================== T4 (SC#3): RetakeService reset → null (single source) ======================
    [Fact]
    public async Task RetakeService_Execute_ResetsTokenVerifiedAtNull()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx);
        // Sesi Completed dgn TokenVerifiedAt set → ExecuteAsync me-reset null (re-arm gate D-01).
        var id = await SeedSessionAsync(ctx, owner.Id, "T4Title", status: "Completed",
            startedAt: DateTime.UtcNow.AddHours(-2), tokenVerifiedAt: DateTime.UtcNow.AddHours(-1));

        var svc = new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);
        var rs = await svc.ExecuteAsync(id, owner.Id, "99999 - Token Test", "RetakeAssessment", "worker_retake");
        Assert.True(rs.Success);

        await using var verify = NewCtx();
        var afterReset = await verify.AssessmentSessions.Where(a => a.Id == id).Select(a => a.TokenVerifiedAt).SingleAsync();
        Assert.Null(afterReset);
    }

    // ====================== T5 (SC#4): legacy InProgress (StartedAt set) → no lockout ======================
    [Fact]
    public async Task StartExam_LegacyInProgress_StartedAtSet_TokenVerifiedAtNull_NotLocked()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx);
        // StartedAt set (sesi sudah dimulai via TempData lama) + TokenVerifiedAt null → guard StartedAt==null
        // mem-bypass gate token → tidak terkunci pasca-deploy.
        var id = await SeedSessionAsync(ctx, owner.Id, "T5Title", status: "InProgress",
            startedAt: DateTime.UtcNow.AddMinutes(-5), tokenVerifiedAt: null);
        await SeedPackageAsync(ctx, id);

        var (ctrl, tempData) = MakeCmp(ctx, owner);
        var result = await ctrl.StartExam(id);

        // Lolos gate (StartedAt!=null bypass): BUKAN redirect token-error.
        Assert.IsType<ViewResult>(result);
        Assert.False(tempData.ContainsKey("Error") && (string?)tempData["Error"] == TokenErrorMsg);
    }
}
