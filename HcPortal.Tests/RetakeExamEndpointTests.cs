// RetakeExamEndpointTests — v32.4 Phase 407 Plan 02 (RTK-09).
// Menguji 3 kasus endpoint worker self-service POST CMP/RetakeExam(id):
//   1. non-owner (assessment.UserId != effective user) → ForbidResult (IDOR guard SEBELUM mutasi).
//   2. not-eligible (CanRetakeAsync false) → RedirectToAction("Results") + TempData["Error"] (server-authoritative).
//   3. sukses (failed+eligible+cooldown lewat) → kolom DB TokenVerifiedAt di-reset null (EXSEC-01, server-authoritative) + RedirectToAction("StartExam").
//
// Strategy (Opsi A): controller unit test atas RetakeServiceFixture (real-SQL disposable DB @localhost\SQLEXPRESS,
// MigrateAsync full chain incl AddRetakeColumnsAndArchive) — reuse RetakeServiceFixture + NoOpHubContext dari
// RetakeServiceTests (assembly sama). UserManager dibangun atas FakeUserStore (FindByIdAsync key-by-id, tak
// drag Identity EF store penuh); ImpersonationService atas DefaultHttpContext + StubSession (no impersonation →
// UseRealUser → GetUserAsync(User) baca NameIdentifier claim → FakeUserStore). Deps controller lain di-null!-substitute
// (RetakeExam tak deref). [Trait("Category","Integration")] → SQL-less CI skip via --filter "Category!=Integration".
//
// Catatan: ketiga kasus pakai real-SQL untuk fidelitas penuh (CanRetakeAsync menyentuh DB). Forbid (kasus 1)
// terjadi SEBELUM CanRetakeAsync — guard ownership murni.
using System;
using System.Collections.Generic;
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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class RetakeExamEndpointTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public RetakeExamEndpointTests(RetakeServiceFixture fixture) => _fixture = fixture;

    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---------- Fake user store: GetUserAsync (FindByIdAsync) + GetRolesAsync (IUserRoleStore, empty roles) ----------
    // GetCurrentUserRoleLevelAsync memanggil GetUserAsync LALU GetRolesAsync (UseRealUser path), jadi store WAJIB
    // implement IUserRoleStore. Role kosong → role level 0 (RetakeExam tak pakai role level; cukup user non-null).
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

        // IUserRoleStore — role kosong (RetakeExam tak gating by role level).
        public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.CompletedTask;
        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.CompletedTask;
        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult<IList<string>>(new List<string>());
        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken ct) => Task.FromResult(false);
        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken ct) => Task.FromResult<IList<ApplicationUser>>(new List<ApplicationUser>());
    }

    // ---------- Stub ISession: ImpersonationService.IsImpersonating() baca GetString(Mode) ----------
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
    private static (CMPController ctrl, ITempDataDictionary tempData) MakeController(
        ApplicationDbContext ctx, ApplicationUser signedInUser)
    {
        var store = new FakeUserStore();
        store.Add(signedInUser);
        var userManager = MakeUserManager(store);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new StubSession();   // no impersonation key → UseRealUser
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, signedInUser.Id)   // GetUserAsync(User) resolve via FakeUserStore
        }, "TestAuth"));

        var impersonation = new ImpersonationService(new HttpContextAccessor { HttpContext = httpContext });
        var retakeService = new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);

        #pragma warning disable CS8625 // null-substitute: RetakeExam tak deref deps berikut.
        var ctrl = new CMPController(
            userManager, null!, null!, ctx, null!, null!, null!,
            NullLogger<CMPController>.Instance, null!, null!, null!, null!, null!,
            impersonation, retakeService);
        #pragma warning restore CS8625

        var tempData = new TempDataDictionary(httpContext, new NullTempDataProvider());
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };
        ctrl.TempData = tempData;
        return (ctrl, tempData);
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    // ---------- Seed helpers (mirror RetakeServiceTests) ----------
    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext ctx, string fullName = "Pekerja Uji")
    {
        var u = new ApplicationUser { UserName = "rtk-" + Guid.NewGuid().ToString("N")[..8], Email = "rtk@test.local", FullName = fullName, NIP = "12345" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u;
    }

    private static async Task<int> SeedSessionAsync(
        ApplicationDbContext ctx, string userId, string title, string category,
        string status = "Completed", bool? isPassed = false, bool allowRetake = true,
        int maxAttempts = 2, int cooldownHours = 0, DateTime? completedAt = null,
        string? assessmentType = null, bool isManualEntry = false, DateTime? tokenVerifiedAt = null)
    {
        var s = new AssessmentSession
        {
            UserId = userId, Title = title, Category = category, Status = status,
            AccessToken = "", Schedule = new DateTime(2026, 2, 1),
            IsPassed = isPassed, AllowRetake = allowRetake, MaxAttempts = maxAttempts,
            RetakeCooldownHours = cooldownHours, CompletedAt = completedAt,
            AssessmentType = assessmentType, IsManualEntry = isManualEntry,
            Score = 50, Progress = 100, TokenVerifiedAt = tokenVerifiedAt
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    // ====================== Kasus 1: non-owner → Forbid (IDOR guard, sebelum mutasi) ======================
    [Fact]
    public async Task RetakeExam_NonOwner_ReturnsForbid()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx, "Pemilik Sesi");
        var attacker = await SeedUserAsync(ctx, "Penyerang");
        // Sesi failed+eligible milik OWNER, tapi yang sign-in adalah ATTACKER.
        var sessionId = await SeedSessionAsync(ctx, owner.Id, "OwnTitle", "Test",
            status: "Completed", isPassed: false, allowRetake: true, completedAt: DateTime.UtcNow.AddDays(-2));

        var (ctrl, _) = MakeController(ctx, attacker);
        var result = await ctrl.RetakeExam(sessionId);

        Assert.IsType<ForbidResult>(result);
        // Tak ada mutasi: sesi tetap Completed (Forbid terjadi SEBELUM ExecuteAsync).
        await using var verify = NewCtx();
        var status = await verify.AssessmentSessions.Where(a => a.Id == sessionId).Select(a => a.Status).SingleAsync();
        Assert.Equal("Completed", status);
    }

    // ====================== Kasus 2: not-eligible → redirect Results + TempData[Error] ======================
    [Fact]
    public async Task RetakeExam_NotEligible_RedirectsToResultsWithError()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx);
        // Owner sign-in, TAPI sesi LULUS (isPassed:true) → CanRetakeAsync false (server-authoritative re-check).
        var sessionId = await SeedSessionAsync(ctx, owner.Id, "PassedTitle", "Test",
            status: "Completed", isPassed: true, allowRetake: true, completedAt: DateTime.UtcNow.AddDays(-2));

        var (ctrl, tempData) = MakeController(ctx, owner);
        var result = await ctrl.RetakeExam(sessionId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Results", redirect.ActionName);
        Assert.True(tempData.ContainsKey("Error"));
        // Tak ada mutasi: sesi tetap Completed.
        await using var verify = NewCtx();
        var status = await verify.AssessmentSessions.Where(a => a.Id == sessionId).Select(a => a.Status).SingleAsync();
        Assert.Equal("Completed", status);
    }

    // ====================== Kasus 3: sukses → token cleared + redirect StartExam ======================
    [Fact]
    public async Task RetakeExam_Success_ClearsTokenAndRedirectsToStartExam()
    {
        await using var ctx = NewCtx();
        var owner = await SeedUserAsync(ctx);
        // Owner sign-in, sesi failed+eligible (cooldown 0 → lewat) → CanRetakeAsync true.
        // Sesi sebelumnya sudah verifikasi token (kolom DB ter-stamp) — sukses-case WAJIB me-reset-nya null (EXSEC-01).
        var sessionId = await SeedSessionAsync(ctx, owner.Id, "FailedTitle", "Test",
            status: "Completed", isPassed: false, allowRetake: true, maxAttempts: 2,
            cooldownHours: 0, completedAt: DateTime.UtcNow.AddDays(-2),
            tokenVerifiedAt: DateTime.UtcNow.AddMinutes(-30));

        var (ctrl, _) = MakeController(ctx, owner);

        var result = await ctrl.RetakeExam(sessionId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("StartExam", redirect.ActionName);
        // EXSEC-01 (Phase 427): token gate kini server-authoritative — retake me-reset kolom DB TokenVerifiedAt=null
        // via single-source RetakeService (bukan lagi TempData). Sesi ter-reset ke Open (ExecuteAsync claim sukses).
        await using var verify = NewCtx();
        var row = await verify.AssessmentSessions.Where(a => a.Id == sessionId)
            .Select(a => new { a.Status, a.TokenVerifiedAt }).SingleAsync();
        Assert.Equal("Open", row.Status);
        Assert.Null(row.TokenVerifiedAt);   // token gate re-arm — minta token ulang di percobaan baru
    }
}
