# Phase 288: Worker, Coach & Organization Controllers - Research

**Researched:** 2026-04-02
**Domain:** ASP.NET Core controller extraction / refactoring
**Confidence:** HIGH

## Summary

Phase ini mengekstrak 3 domain dari AdminController ke controller terpisah: WorkerController (line 1682-2608), CoachMappingController (line 621-1681 + Proton helpers line 3460-3558), dan OrganizationController (line 4057-4389). Pattern sudah terbukti di Phase 287 (AssessmentAdminController) — tinggal direplikasi.

AdminController saat ini punya 3 private DI di luar base: `_config` (IConfiguration), `_logger` (ILogger), `_notificationService` (INotificationService). Analisis penggunaan per domain menunjukkan kebutuhan berbeda per controller.

**Primary recommendation:** Ikuti pattern AssessmentAdminController persis: inherit AdminBaseController, duplikasi route attributes, override View resolution, inject hanya DI yang dibutuhkan.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: `AutoCreateProgressForAssignment()` dan `CleanupProgressForAssignment()` (line 3460-3558) ikut pindah ke CoachMappingController

### Claude's Discretion
- WorkerController scope: semua action worker line 1682-2608
- CoachMappingController scope: semua action coach line 621-1681 + Proton helpers
- OrganizationController scope: semua action org line 4057-4389
- DI dependencies: analisis per controller
- Route & View pattern: duplikasi route + override View resolution
- Cross-controller redirects: tidak ada cross-domain redirect bermasalah
- Authorization: class-level [Authorize] dari base, per-action [Authorize(Roles = "Admin, HC")]

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| WKR-01 | WorkerController berisi semua action ManageWorkers | DI analysis: butuh _config, _logger; line 1682-2608 |
| WKR-02 | Semua URL worker tetap sama via [Route] | Route duplication pattern dari Phase 287 |
| CCM-01 | CoachMappingController berisi semua action coach-coachee + Proton helpers | DI analysis: butuh _logger, _notificationService; line 621-1681 + 3460-3558 |
| CCM-02 | Semua URL mapping tetap sama via [Route] | Route duplication pattern dari Phase 287 |
| ORG-01 | OrganizationController berisi semua action organization | DI analysis: hanya base DI cukup; line 4057-4389 |
| ORG-02 | Semua URL organization tetap sama via [Route] | Route duplication pattern dari Phase 287 |
</phase_requirements>

## Architecture Patterns

### Reference Implementation (AssessmentAdminController)
```csharp
[Route("Admin")]
[Route("Admin/[action]")]
public class AssessmentAdminController : AdminBaseController
{
    // Extra DI beyond base
    private readonly IMemoryCache _cache;
    private readonly ILogger<AssessmentAdminController> _logger;
    // ...

    public AssessmentAdminController(
        ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        AuditLogService auditLog, IWebHostEnvironment env,
        // extra DI
        IMemoryCache cache, ILogger<AssessmentAdminController> logger, ...)
        : base(context, userManager, auditLog, env)
    { ... }

    // View resolution override — WAJIB
    protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
    protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
    protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
    protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);
}
```

### DI Analysis per Controller

**WorkerController** — butuh:
- `IConfiguration _config` — dipakai di CreateWorker (line 1856), EditWorker (line 2089), ImportWorkers (line 2388, 2463) untuk `Authentication:UseActiveDirectory`
- `ILogger<WorkerController> _logger` — dipakai untuk warning/error logging

**CoachMappingController** — butuh:
- `ILogger<CoachMappingController> _logger` — dipakai untuk error logging
- `INotificationService _notificationService` — dipakai di CoachCoacheeMappingAssign (line 1166), CoachCoacheeMappingEdit (line 1296), CoachCoacheeMappingDeactivate (line 1441)

**OrganizationController** — TIDAK butuh DI tambahan:
- Tidak ada penggunaan `_config`, `_logger`, atau `_notificationService` di line 4057-4389
- Hanya `_context` dan `_auditLog` yang tidak dipakai langsung (cek lagi), tapi base DI sudah cukup

### Model Classes yang Harus Pindah

`CoachAssignRequest` dan `CoachEditRequest` (line 4393-4413) didefinisikan di luar namespace AdminController. Harus dipindahkan ke file CoachMappingController.cs atau ke Models/ folder karena hanya dipakai oleh coach mapping actions.

### Anti-Patterns to Avoid
- **Jangan pindah Views**: Views tetap di `Views/Admin/`, override View resolution saja
- **Jangan ubah authorization**: Copy persis `[Authorize(Roles = "Admin, HC")]` per action
- **Jangan lupa hapus dari AdminController**: Setelah copy, hapus region dari AdminController

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| View resolution | Custom view engine | `protected new ViewResult View()` override pattern | Sudah terbukti di Phase 287 |
| Route preservation | Custom routing middleware | `[Route("Admin")]` + `[Route("Admin/[action]")]` | Pattern ASP.NET standard |

## Common Pitfalls

### Pitfall 1: Lupa Override View Resolution
**What goes wrong:** Controller baru mencari Views di `Views/Worker/` bukannya `Views/Admin/`
**How to avoid:** Copy 4 View override methods dari AssessmentAdminController

### Pitfall 2: CoachAssignRequest/CoachEditRequest Tertinggal
**What goes wrong:** Model class ini didefinisikan di luar class AdminController (line 4393-4413), bukan di dalam region coach. Mudah terlewat.
**How to avoid:** Pindahkan ke CoachMappingController.cs atau buat file terpisah

### Pitfall 3: Proton Helpers Terputus dari Caller
**What goes wrong:** Private methods `AutoCreateProgressForAssignment` dan `CleanupProgressForAssignment` dipanggil oleh coach mapping actions. Kalau tidak ikut pindah, compile error.
**How to avoid:** D-01 sudah menetapkan: ikut pindah ke CoachMappingController

### Pitfall 4: asp-controller References di Views
**What goes wrong:** Views yang menggunakan `asp-controller="Admin"` untuk action dalam domain yang dipindahkan akan 404
**How to avoid:** Scan semua Views/Admin/*.cshtml untuk `asp-controller` references ke action yang dipindahkan. Update ke controller name baru (Worker, CoachMapping, Organization). Ini sudah terjadi di Phase 287 (quick task 260402-l2d fix).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework) |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet build` + manual URL verification |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| WKR-01 | Worker actions di WorkerController | build | `dotnet build` | N/A |
| WKR-02 | URL /Admin/ManageWorkers tetap jalan | manual | Browse /Admin/ManageWorkers | N/A |
| CCM-01 | Coach actions di CoachMappingController | build | `dotnet build` | N/A |
| CCM-02 | URL /Admin/CoachCoacheeMapping tetap jalan | manual | Browse /Admin/CoachCoacheeMapping | N/A |
| ORG-01 | Org actions di OrganizationController | build | `dotnet build` | N/A |
| ORG-02 | URL /Admin/ManageOrganization tetap jalan | manual | Browse /Admin/ManageOrganization | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Per wave merge:** `dotnet build` + manual URL check
- **Phase gate:** Build clean + semua URL accessible

### Wave 0 Gaps
None — tidak ada test framework, validasi via build + manual browser check

## Code Examples

### Worker Controller Skeleton
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using ClosedXML.Excel;
using System.Globalization;
using HcPortal.Helpers;

namespace HcPortal.Controllers
{
    [Route("Admin")]
    [Route("Admin/[action]")]
    public class WorkerController : AdminBaseController
    {
        private readonly IConfiguration _config;
        private readonly ILogger<WorkerController> _logger;

        public WorkerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<WorkerController> logger)
            : base(context, userManager, auditLog, env)
        {
            _config = config;
            _logger = logger;
        }

        // View resolution override
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        // ... paste actions from AdminController line 1682-2608
    }
}
```

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminBaseController.cs` — base class structure
- `Controllers/AssessmentAdminController.cs` — proven extraction pattern (Phase 287)
- `Controllers/AdminController.cs` — source code analysis, line-by-line DI usage

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — same ASP.NET Core project, no new libraries
- Architecture: HIGH — proven pattern from Phase 287
- Pitfalls: HIGH — learned from Phase 287 (asp-controller bug already encountered)

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (stable codebase, pure refactoring)
