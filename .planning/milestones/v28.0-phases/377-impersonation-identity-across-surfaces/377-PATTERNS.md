# Phase 377: Impersonation Identity Across Surfaces - Pattern Map

**Mapped:** 2026-06-14
**Files analyzed:** 8 (6 modify + 2 deliverable/test baru)
**Analogs found:** 8 / 8 (semua punya analog di-codebase; bukan green-field)

> Catatan bahasa (CLAUDE.md): narasi Bahasa Indonesia. Semua identifier kode, path file, nama method, signature, dan SQL ditulis verbatim/English. Semua excerpt di bawah dikutip dari kode NYATA yang sudah dibaca (bukan usulan), kecuali ditandai `[USULAN]`.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Services/ImpersonationService.cs` (MODIFY) | service | transform (session→identity) | `Services/ImpersonationService.cs:100` `GetEffectiveRoleLevel()` (self-analog) | exact (extend method paralel) |
| `Controllers/CMPController.cs` (MODIFY) | controller | request-response (GET read) | `CMPController.cs:2388` `GetCurrentUserRoleLevelAsync()` (self-analog) | exact |
| `Controllers/CDPController.cs` (MODIFY) | controller | request-response (GET read) | `CMPController.cs:42-72` constructor DI + `CMPController.cs:2388` resolver | role-match (CMP = template) |
| `Controllers/HomeController.cs` (MODIFY) | controller | request-response (GET read) | `HomeController.cs:53` existing `GetEffectiveRoleLevel()` usage (self-analog) | exact |
| `Middleware/ImpersonationMiddleware.cs` (MODIFY) | middleware | request-response (pipeline guard) | `ImpersonationMiddleware.cs:56-66` auto-expire `Stop()`+redirect (self-analog) | exact |
| `377-AUDIT.md` (NEW deliverable) | doc (audit) | — | Phase 328 cascade audit sweep doc (D-07 cites pola 328) | role-match |
| `HcPortal.Tests/ImpersonationIdentityTests.cs` (NEW) | test | — | `ResultsAuthorizationTests.cs` (pure `[Theory]`) + `ProtonCompletionFixture` (real-SQL) | exact (pure-logic) |
| `tests/e2e/impersonation.spec.ts` (MODIFY/extend) | test (e2e) | — | `impersonation.spec.ts:240` IMP-02 flow (self-analog) | exact |

---

## Pattern Assignments

### `Services/ImpersonationService.cs` (service, transform) — TAMBAH effective-user resolver

**Analog:** method `GetEffectiveRoleLevel()` (L100-117) di file yang sama. Method baru WAJIB paralel persis (D-05 single-source, pola shared-core 363/365/366).

**Struktur service + akses session/HttpContext** (L18-28, verbatim):
```csharp
public class ImpersonationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const int ExpirationMinutes = 30;

    public ImpersonationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession Session => _httpContextAccessor.HttpContext!.Session;
```

**Guard-clause pattern yang DIPAKAI ULANG** (L100-108, verbatim — copy struktur `if (!IsImpersonating() || IsExpired()) return null;` + `if (mode == "role")` branch):
```csharp
public int? GetEffectiveRoleLevel()
{
    if (!IsImpersonating() || IsExpired()) return null;

    var mode = GetMode();
    if (mode == "role")
    {
        var role = GetTargetRole();
        return role != null ? HcPortal.Models.UserRoles.GetRoleLevel(role) : null;
    }
    // mode == "user": role level is stored in HttpContext.Items by middleware
    var ctx = _httpContextAccessor.HttpContext;
    if (ctx?.Items["ImpersonateTargetRoleLevel"] is int level)
        return level;
    ...
}
```

**Building block yang sudah ada (jangan duplikasi):** `IsImpersonating()` (L30), `IsExpired()` (L62), `GetMode()` (L82), `GetTargetUserId()` (L92). Resolver effective-user tinggal komposisi method-method ini.

**Aksi fix [USULAN — bentuk API = Claude's Discretion per A1]:** tambah `GetEffectiveTargetUserId()` mengikuti pola guard di atas — return `null` saat `!IsImpersonating() || IsExpired()`, return `null` saat `GetMode() == "role"` (D-03), return `GetTargetUserId()` saat `mode == "user"`. Penambahan resolver yang mengembalikan `ApplicationUser` perlu `UserManager` sebagai PARAMETER (service ini TIDAK inject `UserManager` — hanya `IHttpContextAccessor`).

---

### `Controllers/CMPController.cs` (controller, request-response) — rewrite resolver impersonation-aware

**Analog:** `GetCurrentUserRoleLevelAsync()` (L2388) di file yang sama = method yang DI-rewrite.

**Resolver SEKARANG** (L2388-2395, verbatim — return tuple nullable, sudah ada guard `if (user == null)`):
```csharp
private async Task<(ApplicationUser? User, int RoleLevel)> GetCurrentUserRoleLevelAsync()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return (null, 0);
    var userRoles = await _userManager.GetRolesAsync(user);
    var roleLevel = UserRoles.GetRoleLevel(userRoles.FirstOrDefault() ?? "");
    return (user, roleLevel);
}
```

**DI constructor — `_impersonationService` SUDAH ter-inject** (L40, L56, L71, verbatim) → CMP siap pakai resolver baru tanpa ubah constructor:
```csharp
private readonly ImpersonationService _impersonationService;
// ...constructor param (L56): ImpersonationService impersonationService)
// ...assignment (L71): _impersonationService = impersonationService;
```

**Contoh pemakaian `_impersonationService.GetEffectiveRoleLevel()` yang SUDAH ADA di CMP** (L86-88, verbatim — pola override role-level; resolver baru extend ini ke USER):
```csharp
var currentUser = await _userManager.GetUserAsync(User) as ApplicationUser;
// Respect impersonation: override role level if impersonating
var userLevel = _impersonationService.GetEffectiveRoleLevel() ?? currentUser?.RoleLevel ?? 6;
```

**Caller utama (akar bug live)** `Records:481` (L481-484, verbatim):
```csharp
var (user, roleLevel) = await GetCurrentUserRoleLevelAsync();
if (user == null) return RedirectToAction("Login", "Account");   // ◄ Pitfall 1: SALAH untuk mode-role (D-03)

var unified = await _workerDataService.GetUnifiedRecords(user.Id);
```
**Aksi fix:** setelah rewrite resolver, `user.Id` otomatis = X.Id. TAPI baris `if (user == null) return RedirectToAction(...)` WAJIB dibedakan (Pitfall 1): mode-role → render view kosong + hint; genuinely-null → redirect.

**Ownership authz — pure static `IsResultsAuthorized` (L2399-2408, verbatim)** dipanggil Certificate/CertificatePdf/Results dengan `currentUserId` = `user.Id`; full-fidelity D-01 = otomatis benar saat `user.Id` jadi X.Id:
```csharp
public static bool IsResultsAuthorized(string? ownerUserId, string currentUserId, int roleLevel, string? currentUserSection, string? ownerSection)
{
    if (ownerUserId == currentUserId) return true;          // owner (coach/coachee self)
    if (roleLevel is >= 1 and <= 3) return true;            // Admin(1)/HC(2)/L3: full
    if (roleLevel == 4 && !string.IsNullOrEmpty(currentUserSection) && ownerSection == currentUserSection)
        return true;                                         // L4 section-scoped
    return false;
}
```

**Surface BYPASS resolver (call `GetUserAsync(User)` langsung — route ke resolver eksplisit):**

`Assessment:203` (L203-209, verbatim):
```csharp
var user = await _userManager.GetUserAsync(User);
var userId = user?.Id ?? "";
var query = _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.UserId == userId);     // ◄ userId harus = X.Id saat impersonasi
```

`StartExam:867` — **write-on-GET (Pitfall 3)** (L867-877, verbatim):
```csharp
var user = await _userManager.GetUserAsync(User);
if (user == null) return Challenge();
if (assessment.UserId != user.Id && !User.IsInRole("Admin") && !User.IsInRole("HC"))
    return Forbid();

// Auto-transition: Upcoming → Open ... (persisted to DB)
if (assessment.Status == "Upcoming" && assessment.Schedule <= DateTime.UtcNow.AddHours(7))
{
    assessment.Status = "Open";
    assessment.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();   // ◄ Pitfall 3: write saat impersonasi (read-only) — guard if(!IsImpersonating())
}
```

---

### `Controllers/CDPController.cs` (controller, request-response) — rewrite resolver + INJECT ImpersonationService

**Analog primer (template DI):** `CMPController` constructor (L42-72) — CDP belum inject `ImpersonationService` (Pitfall 2 / VERIFIED). CDP HARUS menambah pola yang sama persis dengan CMP.

**Constructor CDP SEKARANG (TANPA ImpersonationService)** (L33-50, verbatim):
```csharp
private readonly UserManager<ApplicationUser> _userManager;
private readonly SignInManager<ApplicationUser> _signInManager;
private readonly ApplicationDbContext _context;
private readonly IWebHostEnvironment _env;
private readonly INotificationService _notificationService;
private readonly ILogger<CDPController> _logger;
private readonly AuditLogService _auditLog;

public CDPController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IWebHostEnvironment env, INotificationService notificationService, ILogger<CDPController> logger, AuditLogService auditLog)
{
    _userManager = userManager;
    // ... (TIDAK ada _impersonationService = ...)
}
```
**Aksi fix:** tambah field `private readonly ImpersonationService _impersonationService;` + param constructor + assignment, copy persis dari `CMPController.cs:40/56/71`. DI sudah terdaftar `Program.cs:63` `AddScoped<ImpersonationService>()` — tidak perlu registrasi baru.

**Resolver CDP SEKARANG — signature DIVERGEN (non-null)** (L3696-3702, verbatim — VERIFIED return `(ApplicationUser User, ...)` pakai `user!`):
```csharp
private async Task<(ApplicationUser User, int RoleLevel)> GetCurrentUserRoleLevelAsync()
{
    var user = await _userManager.GetUserAsync(User);
    var userRoles = await _userManager.GetRolesAsync(user!);
    var roleLevel = UserRoles.GetRoleLevel(userRoles.FirstOrDefault() ?? "");
    return (user!, roleLevel);
}
```
**Aksi fix:** seragamkan ke **nullable** (`(ApplicationUser? User, int RoleLevel)`) seperti CMP supaya mode-role D-03 punya null-path. Caller perlu null-guard tambahan.

**Caller CDP yang asumsi non-null** (L3740, verbatim — `.User.Section` tanpa guard → akan NRE bila diubah nullable; perlu null-handling):
```csharp
ViewBag.UserBagian = (await GetCurrentUserRoleLevelAsync()).User.Section;
```

---

### `Controllers/HomeController.cs` (controller, request-response) — effective user di Index/GetProgress + fold split-brain

**Analog (self):** `Index:53` sudah pakai `_impersonationService.GetEffectiveRoleLevel()` (role-level aware) — bukti split-brain (role-level efektif TAPI identitas user asli L38).

**DI constructor — `_impersonationService` SUDAH ter-inject** (L22, L26, L33, verbatim):
```csharp
private readonly ImpersonationService _impersonationService;
// constructor (L26): ImpersonationService impersonationService)
// assignment (L33): _impersonationService = impersonationService;
```

**Bug split-brain — Index** (L38-53, verbatim — L38 user ASLI, L53 role-level EFEKTIF):
```csharp
var user = await _userManager.GetUserAsync(User);          // L38 = admin asli (BUG)
if (user == null) return Challenge();

var upcomingEvents = await GetUpcomingEvents(user.Id);     // L41 = events admin (BUG)
var progress = await GetProgress(user.Id);                 // L42 = progress admin (BUG)
// ...
var effectiveLevel = _impersonationService.GetEffectiveRoleLevel();   // L53 = EFEKTIF (sudah benar)
```

**Konsumen `userId`** — `GetProgress(string userId)` (L213) + `GetUpcomingEvents(string userId)` (L275): agnostik identitas (terima param), otomatis benar bila `user.Id` di-resolve ke X.Id di hulu.

**OUT-of-scope di Home (jangan sentuh, A4):** `Guide:329` / `GuideDetail:346` (L327-349, verbatim) — konten guide per-role, bukan data-diri:
```csharp
var user = await _userManager.GetUserAsync(User);
if (user == null) return Challenge();
var userRoles = await _userManager.GetRolesAsync(user);
var userRole = userRoles.FirstOrDefault() ?? "User";
// → GuideContentProvider.GetModuleCards(userRole)   (mapping role→konten, OUT)
```

---

### `Middleware/ImpersonationMiddleware.cs` (middleware, pipeline guard) — D-04 fallback target null

**Analog (self):** auto-expire `Stop()`+redirect (L56-66) = pola PERSIS untuk D-04.

**Pola auto-expire = template fallback D-04** (L56-66, verbatim — copy struktur `Stop()` + `ITempDataDictionaryFactory` + `Redirect("/Admin/Index")`):
```csharp
if (impersonationService.IsExpired())
{
    impersonationService.Stop();

    var tempDataFactory = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
    var tempData = tempDataFactory.GetTempData(context);
    tempData["ErrorMessage"] = "Sesi impersonation telah berakhir (batas 30 menit).";

    context.Response.Redirect("/Admin/Index");
    return;
}
```

**Titik sisip D-04 — `SetContextItems` sudah `FindByIdAsync`** (L128-145, verbatim — else-branch `targetUser == null` adalah titik fallback):
```csharp
else if (mode == "user")
{
    var targetUserId = service.GetTargetUserId();
    if (targetUserId != null)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var targetUser = await userManager.FindByIdAsync(targetUserId);
        if (targetUser != null)            // ◄ titik sisip D-04: else null → Stop()+redirect (pola L56-66)
        {
            var roles = await userManager.GetRolesAsync(targetUser);
            // ... set HttpContext.Items
        }
    }
}
```
**Catatan implementasi (RESEARCH Pattern 2):** `SetContextItems` saat ini `private static async Task` (L111), dipanggil dari 2 tempat (GET L74, whitelisted-write L107) dan TIDAK pegang kendali redirect ergonomis. Planner: pindahkan cek null+redirect ke `InvokeAsync` (yang sudah pegang `context.Response` + pola redirect L56-66), atau ubah `SetContextItems` return `bool` (false=sudah redirect → short-circuit `_next`).

---

### `377-AUDIT.md` (NEW deliverable, D-07/SC1/IMP-02)

**Analog:** Phase 328 cascade audit sweep doc (D-07 eksplisit "sesuai pola fase audit lalu (328)"). Doc audit mandiri = input planner.

**Bahan mentah SUDAH JADI:** RESEARCH §Audit Call-Site Inventory (377-RESEARCH.md L235-278) berisi 3 tabel siap-materialize:
- IN-SCOPE self-worker-data READ (11 baris: CMP 481/545/1733/1839/2080/203/611/867/3870 + Home 38 + CDP 3859)
- BORDERLINE team-view (5 baris: CMP 660/721/774/507/86-88)
- OUT-OF-SCOPE write/admin/authz (pola + contoh)

**Kolom tabel WAJIB (D-07):** `file:line` | surface (action) | jenis read | impersonation-aware sekarang? | in-scope? | aksi fix.

**Verifikasi parity (RESEARCH test-map SC1):** `rg "GetUserAsync\(User\)|GetCurrentUserRoleLevelAsync" Controllers/` → cross-check tiap baris ter-triage di AUDIT (gunakan tool Grep, bukan rg langsung).

---

### `HcPortal.Tests/ImpersonationIdentityTests.cs` (NEW test)

**Analog primer (pure-logic `[Theory]`):** `ResultsAuthorizationTests.cs` (full, verbatim) — pola yang DIPAKAI untuk resolver-decision pure-logic (no Moq di proyek):
```csharp
using HcPortal.Controllers;
using Xunit;

namespace HcPortal.Tests;

public class ResultsAuthorizationTests
{
    [Theory]
    // ownerUserId, currentUserId, roleLevel, currentSection, ownerSection, expected
    [InlineData("u1", "u1", 6, "A", "A", true)]    // owner (walau L6 coachee)
    [InlineData("u1", "x",  1, null, "A", true)]   // Admin (roleLevel<=3, section irrelevant)
    // ...
    public void IsResultsAuthorized_Matrix(string owner, string cur, int lvl, string? curSec, string? ownSec, bool expected)
        => Assert.Equal(expected, CMPController.IsResultsAuthorized(owner, cur, lvl, curSec, ownSec));
}
```

**Analog sekunder (pure-static decision dari controller):** `MonitoringUserStatusTests.cs` — pola test method static controller via `[Theory]` + `[InlineData]` + satu `[Fact]` regresi-guard. Ini template untuk "given (isImpersonating, mode, targetUserId) → effectiveUserId" bila resolver-decision di-extract ke fungsi pure (RESEARCH seam #1).

**Analog tersier (real-SQL fixture, BILA perlu service end-to-end):** `ProtonCompletionFixture` (`ProtonCompletionServiceTests.cs:25-54`) — `IAsyncLifetime` disposable DB `HcPortalDB_Test_<guid>` + `[Trait("Category","Integration")]`. Dipakai untuk `ImpersonationService` fake-context test (RESEARCH seam #2: `DefaultHttpContext` + in-memory `ISession` stub ~30 baris, tanpa Moq).

**Test cases (RESEARCH §Validation):** `(false,_,_)→use-real`, `(true,"role",_)→null` (D-03), `(true,"user","X")→"X"` (SC2), `(true,"user",null)→trigger D-04`, branch not-impersonating identik (SC4).

---

### `tests/e2e/impersonation.spec.ts` (MODIFY/extend)

**Analog (self):** IMP-02 flow `impersonation.spec.ts:240-281` — impersonate user via `SearchUsersApi` + autocomplete. Extend dengan `page.goto('/CMP/Records')` + assert data = X.

**Pola impersonate-user yang DIPAKAI ULANG** (L240-276, verbatim — re-use SearchUsersApi → autocomplete → banner):
```javascript
test('IMP-02: impersonate specific user via autocomplete search shows user name in banner', async ({ page }) => {
    await login(page, 'admin');

    const searchResponse = await page.request.get('/Admin/SearchUsersApi?q=rino');
    const users = await searchResponse.json();
    const targetUser = users[0];

    await page.locator('nav .dropdown-toggle').last().click();
    const searchInput = page.locator('#impersonate-search');
    await searchInput.fill('rino');
    const results = page.locator('#impersonate-results');
    await expect(results).toContainText(targetUser.fullName, { timeout: 5_000 });
    await results.locator('[data-userid]').first().click();
    await page.waitForURL('**/Home/**', { timeout: 15_000 });

    const banner = page.locator('#impersonation-banner');
    await expect(banner).toContainText(targetUser.fullName);
    // [EXTEND SC2/SC3]: await page.goto('/CMP/Records'); assert tabel berisi record X (bukan admin)
});
```

**Gotcha e2e (CLAUDE.md / STATE.md):** `test.describe.configure({ mode: 'serial' })` (L4) WAJIB; run combined WAJIB `--workers=1` (impersonation session-state, paralel saling kontaminasi); start SQLBrowser + `lpc:` override + `Authentication__UseActiveDirectory=false dotnet run` untuk login lokal. Seed deterministik X (≥1 assessment + ≥1 training) = `temporary+local-only` per SEED_WORKFLOW (snapshot+restore).

---

## Shared Patterns

### Effective-identity resolution (D-05 single-source)
**Source:** `Services/ImpersonationService.cs:100` `GetEffectiveRoleLevel()` + L62 `IsExpired()` + L82 `GetMode()`.
**Apply to:** semua resolver controller (CMP/CDP) + Home Index. Guard wajib: `if (!IsImpersonating() || IsExpired()) return <sentinel>;` lalu branch `mode == "role"` → null (D-03) vs `mode == "user"` → target.
```csharp
if (!IsImpersonating() || IsExpired()) return null;
var mode = GetMode();
if (mode == "role") { /* role-level only, user = null (D-03) */ }
// mode == "user": HttpContext.Items / GetTargetUserId()
```

### Constructor DI injection
**Source:** `CMPController.cs:40,56,71` (field + param + assignment) — template untuk menambah `ImpersonationService` ke CDP.
**Apply to:** `CDPController` (Pitfall 2 — belum inject). DI sudah ada `Program.cs:63 AddScoped`.
```csharp
private readonly ImpersonationService _impersonationService;          // field
public CDPController(..., ImpersonationService impersonationService)  // param
{ ... _impersonationService = impersonationService; }                 // assignment
```

### Stop()+redirect fallback (D-04)
**Source:** `ImpersonationMiddleware.cs:56-66` auto-expire.
**Apply to:** sisipan target-null di `SetContextItems`/`InvokeAsync`.
```csharp
service.Stop();
var tempData = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(context);
tempData["ErrorMessage"] = "User yang di-impersonate tidak ditemukan.";
context.Response.Redirect("/Admin/Index");
return;
```

### Pure-static testable seam (no Moq)
**Source:** `CMPController.IsResultsAuthorized` (L2399, `public static`) + `ResultsAuthorizationTests` `[Theory]`.
**Apply to:** resolver-decision logic (extract pure fungsi) + extend `ResultsAuthorizationTests` dengan kasus impersonate-X fidelity (D-01).

---

## No Analog Found

(none) — semua file punya analog kuat di-codebase. Phase ini = extend mekanisme existing (role-level effective + middleware target-resolve + ownership helper sudah ada), bukan membangun mekanisme impersonasi baru.

---

## Metadata

**Analog search scope:** `Services/`, `Controllers/` (CMP/CDP/Home/Admin), `Middleware/`, `HcPortal.Tests/`, `tests/e2e/`, `Program.cs`.
**Files scanned (full/partial read):** ImpersonationService.cs, ImpersonationMiddleware.cs, CMPController.cs (L40-160, 195-225, 475-550, 860-890, 2380-2429), CDPController.cs (L1-60, 3690-3742), HomeController.cs (L1-75, 213/275/320-352), AdminController.cs (L136-203), Program.cs (L63, 200-217), ResultsAuthorizationTests.cs, ShuffleToggleRulesTests.cs, CDPControllerAuthTests.cs, MonitoringUserStatusTests.cs, ProtonCompletionServiceTests.cs, impersonation.spec.ts.
**Pattern extraction date:** 2026-06-14
