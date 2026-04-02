# Phase 286: AdminBaseController - Research

**Researched:** 2026-04-02
**Domain:** ASP.NET Core 8 controller inheritance / refactoring
**Confidence:** HIGH

## Summary

Phase ini membuat abstract base class `AdminBaseController` yang menyediakan 4 shared dependencies (ApplicationDbContext, UserManager, AuditLogService, IWebHostEnvironment) dan attribute routing `[Route("Admin")]` + `[Authorize]`. AdminController yang ada akan diubah untuk mewarisi base class ini sebagai proof-of-concept sebelum domain controllers dipecah di phase berikutnya.

Tantangan utama: AdminController saat ini menggunakan **conventional routing** (`{controller}/{action}`) — bukan attribute routing. Keputusan D-07 mengharuskan `[Route("Admin")]` di base class. Ini berarti perlu menambahkan `[Route("Admin/[action]")]` agar semua existing action tetap resolve ke URL yang sama. Perlu hati-hati karena mixing conventional dan attribute routing bisa menyebabkan routing conflicts.

**Primary recommendation:** Buat abstract AdminBaseController dengan 4 protected fields + `[Route("Admin")]` + `[Authorize]`, lalu ubah AdminController untuk inherit dari base class. Verifikasi dengan build + manual URL check.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: AdminBaseController menyediakan 4 shared dependencies: ApplicationDbContext, UserManager<ApplicationUser>, AuditLogService, IWebHostEnvironment
- D-02: Dependencies lain (IMemoryCache, IConfiguration, INotificationService, IHubContext, IWorkerDataService, ImpersonationService) tetap di-inject per domain controller
- D-03: ILogger<T> di-inject di masing-masing controller karena generic per class
- D-04: Base class TIDAK berisi helper methods — hanya DI
- D-05: Shuffle() + BuildCrossPackageAssignment() ikut pindah ke AssessmentAdminController (Phase 287)
- D-06: Proton Progress Helpers ikut pindah ke CoachMappingController (Phase 288)
- D-07: [Route("Admin")] dan [Route("Admin/[action]")] ditaruh di AdminBaseController
- D-08: [Authorize] class-level tetap di AdminBaseController

### Claude's Discretion
- Nama class: AdminBaseController vs AdminControllerBase (konvensi ASP.NET)
- Apakah base class abstract atau tidak
- Constructor pattern (base constructor call)

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| BASE-01 | AdminBaseController dibuat dengan shared DI dan helper methods yang dipakai bersama | Catatan: REQUIREMENTS.md menyebut SignInManager + ILogger, tapi CONTEXT.md decisions (D-01) mengunci 4 deps: DbContext, UserManager, AuditLogService, IWebHostEnvironment. CONTEXT.md adalah keputusan final. D-04 mengunci: TIDAK ada helper methods di base. |
| BASE-02 | Semua controller baru mewarisi AdminBaseController dan bisa mengakses shared dependencies tanpa duplikasi constructor | Pattern: protected fields di base + child calls `base(...)` constructor |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- Selalu respond dalam Bahasa Indonesia

## Architecture Patterns

### Recommended Approach: Abstract Base Controller

```csharp
// Controllers/AdminBaseController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Authorize]
    [Route("Admin")]
    [Route("Admin/[action]")]
    public abstract class AdminBaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly AuditLogService _auditLog;
        protected readonly IWebHostEnvironment _env;

        protected AdminBaseController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _env = env;
        }
    }
}
```

### AdminController After Inheritance

```csharp
[Authorize]
[Route("Admin")]
[Route("Admin/[action]")]
public class AdminController : AdminBaseController
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminController> _logger;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<AssessmentHub> _hubContext;
    private readonly IWorkerDataService _workerDataService;
    private readonly ImpersonationService _impersonationService;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        AuditLogService auditLog,
        IWebHostEnvironment env,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<AdminController> logger,
        INotificationService notificationService,
        IHubContext<AssessmentHub> hubContext,
        IWorkerDataService workerDataService,
        ImpersonationService impersonationService)
        : base(context, userManager, auditLog, env)
    {
        _cache = cache;
        _config = config;
        _logger = logger;
        _notificationService = notificationService;
        _hubContext = hubContext;
        _workerDataService = workerDataService;
        _impersonationService = impersonationService;
    }
    // ... all existing actions unchanged
}
```

### Discretion Recommendations

| Decision | Recommendation | Reason |
|----------|---------------|--------|
| Nama class | `AdminBaseController` | Lebih eksplisit; `ControllerBase` bisa bingung dengan `Microsoft.AspNetCore.Mvc.ControllerBase` |
| Abstract atau tidak | `abstract` | Tidak ada alasan instantiate base class langsung |
| Constructor pattern | Child passes 4 deps via `base(...)` | Standard ASP.NET pattern |

### Project Structure

```
Controllers/
  AdminBaseController.cs    # NEW — abstract base (Phase 286)
  AdminController.cs        # MODIFIED — inherits AdminBaseController
  AccountController.cs      # unchanged
  CMPController.cs          # unchanged
  CDPController.cs          # unchanged
  HomeController.cs         # unchanged
```

## Common Pitfalls

### Pitfall 1: Routing Conflict (Conventional vs Attribute)
**What goes wrong:** ASP.NET Core treats controllers with `[Route]` attributes differently from conventional-routed controllers. Jika base class punya `[Route]` tapi conventional routing masih aktif, bisa terjadi ambiguous match.
**Why it happens:** ASP.NET Core: controller yang punya ANY attribute route TIDAK participate di conventional routing. Ini adalah by-design behavior.
**How to avoid:** Setelah menambah `[Route("Admin")]` + `[Route("Admin/[action]")]` di base/child, AdminController otomatis keluar dari conventional routing. Pastikan SEMUA action tetap reachable via attribute route. Action dengan parameter `{id}` perlu `[Route("Admin/[action]/{id?}")]` atau `[HttpGet]`/`[HttpPost]` dengan template.
**Warning signs:** 404 errors pada URL yang sebelumnya bekerja.

### Pitfall 2: Action Parameter Routing
**What goes wrong:** Action yang menerima `id` parameter (seperti `EditWorker(int id)`) tidak resolve jika route template hanya `[action]` tanpa `{id?}`.
**How to avoid:** Gunakan `[Route("Admin/[action]/{id?}")]` sebagai salah satu route template, ATAU tambahkan `[HttpGet("{id?}")]` / `[HttpPost]` per action yang butuh id.
**Recommendation:** Route template `[Route("Admin/[action]/{id?}")]` di base class sudah cukup cover semua kasus.

### Pitfall 3: Private Field Visibility
**What goes wrong:** AdminController saat ini pakai `private readonly` untuk semua fields. Setelah 4 fields dipindah ke base, child class tidak bisa akses `private` fields.
**How to avoid:** Base class pakai `protected readonly`. AdminController hapus 4 field declarations dan ganti referensi ke inherited fields.

### Pitfall 4: Duplicate [Authorize] Attribute
**What goes wrong:** Jika `[Authorize]` ada di base DAN child, tidak error tapi redundant.
**How to avoid:** Cukup di base saja. Hapus dari AdminController. Per-action `[Authorize(Roles = "...")]` tetap di masing-masing action.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Route attribute inheritance | Custom route convention | `[Route("Admin/[action]/{id?}")]` di base | ASP.NET built-in token replacement |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright (browser tests only) |
| Config file | `tests/playwright.config.ts` |
| Quick run command | `dotnet build` (compile check) |
| Full suite command | `dotnet build && dotnet run` + manual URL verification |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| BASE-01 | AdminBaseController exists with 4 DI | unit (compile) | `dotnet build` | N/A (compile check) |
| BASE-02 | AdminController inherits base and works | smoke | Manual: navigate `/Admin/Index` | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` — must compile without errors
- **Per wave merge:** `dotnet build` + manual URL spot-check (`/Admin/Index`, `/Admin/ManageWorkers`)
- **Phase gate:** Full build clean + key admin pages load correctly

### Wave 0 Gaps
None — no unit test framework in project. Verification is build + manual smoke test (consistent with project's existing approach).

## Open Questions

1. **Route template completeness**
   - What we know: `[Route("Admin/[action]/{id?}")]` covers most actions
   - What's unclear: Apakah ada action di AdminController yang punya parameter routing non-standard (bukan `id`)? Perlu scan saat implementasi.
   - Recommendation: Scan semua action signatures untuk parameter selain `id` sebelum finalize route template.

## Environment Availability

Step 2.6: SKIPPED (no external dependencies — pure code refactoring).

## Sources

### Primary (HIGH confidence)
- Kode sumber langsung: `Controllers/AdminController.cs` lines 1-58 (constructor, DI, attributes)
- `Program.cs` line 202-204 (conventional routing setup)
- ASP.NET Core 8 attribute routing behavior — from training data, well-established pattern (HIGH confidence)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — ini ASP.NET Core 8, well-understood framework
- Architecture: HIGH — controller inheritance adalah standard pattern
- Pitfalls: HIGH — routing conflict adalah well-documented gotcha

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (stable domain, no changes expected)
