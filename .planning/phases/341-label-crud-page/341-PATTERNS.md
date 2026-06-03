# Phase 341: Label CRUD Page — Pattern Map

**Mapped:** 2026-06-03
**Files analyzed:** 5 (3 edit + 2 create)
**Analogs found:** 5 / 5 (100% — semua pola tersedia di neighbor codebase, zero ekstrapolasi)
**Milestone:** v21.0 (ManageOrganization Overhaul + Level Label CRUD)
**Predecessor:** Phase 340 SHIPPED LOCAL (`OrgLabelService` + `OrgLabelController` skeleton + 13 [Fact])

---

## File Classification

| File | Op | Role | Data Flow | Closest Analog | Match Quality |
|------|-----|------|-----------|----------------|---------------|
| `Controllers/OrgLabelController.cs` | EDIT (extend) | controller | request-response (GET render + 3× POST JSON mutation) | `Controllers/OrganizationController.cs` | **exact** (same admin CRUD, same `[Route("Admin/[action]")]`, sibling service) |
| `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs` | CREATE | view-model | data-bag (controller → Razor) | `Models/ViewModels/CMPRecordsViewModel.cs` | **exact** (same namespace convention, same plain POCO pattern) |
| `Views/Admin/ManageOrgLevelLabels.cshtml` | CREATE | view | server-render Razor + 2× AJAX mutation client | `Views/Admin/ManageOrganization.cshtml` | **exact** (table + modal + antiforgery + shared-toast + AJAX submit) |
| `Views/Admin/Index.cshtml` | EDIT (1 card insert) | view | static navigation | `Views/Admin/Index.cshtml` L35-50 (self — replicate sibling card block) | **exact** (in-file precedent) |
| `HcPortal.Tests/OrgLabelControllerTests.cs` | CREATE | test | unit test (in-memory) | `HcPortal.Tests/OrgLabelServiceTests.cs` | **role-match** (same project, same DI infra; controller-test specific bits inferred) |

---

## Pattern Assignments

### 1. `Controllers/OrgLabelController.cs` (controller, request-response)

**Analog primary:** `Controllers/OrganizationController.cs`
**Analog secondary:** `Controllers/OrgLabelController.cs` (existing 32 LoC Phase 340) — keep + extend

**Existing file state** (verified `Controllers/OrgLabelController.cs:1-32`):
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Authorize]
    [Route("Admin/[action]")]
    public class OrgLabelController : Controller
    {
        private readonly IOrgLabelService _orgLabels;

        public OrgLabelController(IOrgLabelService orgLabels) { _orgLabels = orgLabels; }

        // GET /Admin/GetLevelLabels  (existing — keep verbatim)
        [HttpGet]
        public IActionResult GetLevelLabels() { ... return Json(jsonDict); }
    }
}
```

**Replication points** (copy verbatim from analog):

**P-1a — Class-level routing + authorize** — already present (Phase 340), keep:
- `[Authorize]` class-level (any authenticated user can hit GetLevelLabels per Phase 340 D-03)
- `[Route("Admin/[action]")]` covers all new action URLs (`/Admin/ManageOrgLevelLabels`, `/Admin/UpdateLevelLabel`, etc.)

**P-1b — View resolution override** (CRITICAL — Pitfall 1 in RESEARCH.md §Common Pitfalls):
Source: `Controllers/OrganizationController.cs:24-27` verbatim:
```csharp
// Override View resolution to use Views/Admin/ folder (controller name is Organization, but views stay in Admin/)
protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);
```
**Adaptation:** Paste verbatim into `OrgLabelController` (sebab controller name `OrgLabel` ≠ folder name `Admin`). Comment teks ganti ke `(controller name is OrgLabel, but views stay in Admin/)`.

**P-1c — Constructor DI extension** — extend from 1 to 4 dependencies:
Source: `Controllers/OrganizationController.cs:14-21` (AdminBaseController inherits `_context`, `_userManager`, `_auditLog`, `_env`). Since `OrgLabelController` does NOT inherit `AdminBaseController` (Phase 340 chose lean `Controller` base), add directly:
```csharp
private readonly IOrgLabelService _orgLabels;
private readonly ApplicationDbContext _context;          // ADD — for unique-check + used-level query
private readonly UserManager<ApplicationUser> _userManager; // ADD — for actor resolution

public OrgLabelController(
    IOrgLabelService orgLabels,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager)
{
    _orgLabels = orgLabels;
    _context = context;
    _userManager = userManager;
}
```
**Note:** `AuditLogService` NOT injected — Phase 340 service wraps audit internally (RESEARCH §Reusable Assets confirms `_auditLog.LogAsync` runs inside `OrgLabelService` mutations).

**P-1d — JSON success/failure mutation pattern** (D-01, D-05) — copy from `OrganizationController.AddOrganizationUnit` lines 74-122:
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddOrganizationUnit(string name, int? parentId)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        if (IsAjaxRequest())
            return Json(new { success = false, message = "Nama tidak boleh kosong." });
        // ... TempData fallback ...
    }

    bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim());
    if (duplicate)
    {
        if (IsAjaxRequest())
            return Json(new { success = false, message = "Nama unit sudah digunakan. ..." });
        // ...
    }

    // ... mutation ...

    if (IsAjaxRequest())
        return Json(new { success = true, message = "Unit berhasil ditambahkan." });
    // ...
}
```
**Adaptation Phase 341:** Drop TempData fallback (no non-AJAX submit path per D-01 fetch-only). Replace `name` → `label`, replace `OrganizationUnits` → `OrganizationLevelLabels`, replace duplicate predicate per CONTEXT D-05: `AnyAsync(l => l.Label == label && l.Level != currentLevel)`. Three mutation actions follow this skeleton (UpdateLevelLabel, AddLevelLabel, DeleteLevelLabel).

**P-1e — Method-level role authorize + antiforgery** (D-06, D-09) — copy verbatim attribute trio from `OrganizationController.cs:71-73`:
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```
**Adaptation:** Apply to UpdateLevelLabel, AddLevelLabel, DeleteLevelLabel. For `[HttpGet] ManageOrgLevelLabels`, apply `[HttpGet]` + `[Authorize(Roles = "Admin, HC")]` (no antiforgery on GET).

**P-1f — Actor name resolution** (Phase 340 service requires `userId` + `actorName` params) — copy verbatim from `OrganizationController.cs:425-428`:
```csharp
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";
await _orgLabels.UpdateAsync(level, label, currentUser?.Id ?? "", actorName);
```
**Adaptation:** Identical paste — same `ApplicationUser` shape (NIP + FullName), same fallback chain.

**P-1g — Service exception → friendly JSON** (Pitfall 4 in RESEARCH §Common Pitfalls):
```csharp
try
{
    await _orgLabels.UpdateAsync(level, label, currentUser?.Id ?? "", actorName);
    return Json(new { success = true, message = $"Label level {level} berhasil diubah menjadi '{label}'." });
}
catch (InvalidOperationException ex)
{
    return Json(new { success = false, message = ex.Message });
}
```
**Adaptation:** Also wrap `AddAsync` with extra `catch (DbUpdateException)` for race (Pitfall 8). DeleteAsync may also throw `InvalidOperationException` if level not found.

**P-1h — Server-side mid-tier delete bypass guard** (T-341-05 mitigation, RESEARCH §Pitfall 7):
```csharp
int maxConfig = _orgLabels.GetMaxConfiguredLevel();
if (level != maxConfig)
    return Json(new { success = false, message = "Hanya level tertinggi yang bisa dihapus." });

bool isUsed = await _context.OrganizationUnits.AnyAsync(u => u.Level == level);
if (isUsed)
    return Json(new { success = false, message = "Level masih dipakai unit, tidak bisa dihapus." });
```
**Adaptation:** Place at top of `DeleteLevelLabel` action body before service call. No analog — distillation from research §Common Pitfalls 7 + D-05 enforce.

**P-1i — Add level constraint** (T-341-03 mitigation, D-08):
```csharp
int expectedNext = _orgLabels.GetMaxConfiguredLevel() + 1;
if (level != expectedNext)
    return Json(new { success = false, message = $"Hanya level berikutnya (Level {expectedNext}) yang bisa ditambahkan." });
```
**Adaptation:** Place at top of `AddLevelLabel` action body before validation checks.

**Extension points** (NEW code, not from analog):
- `ManageOrgLevelLabels()` GET — build `ManageOrgLevelLabelsViewModel` (see RESEARCH §Code Examples #1). Loop `0..displayMax` + 1 buffer row.
- `using HcPortal.Models.ViewModels;` import.
- `using HcPortal.Data;` (`ApplicationDbContext`) + `using HcPortal.Models;` (`ApplicationUser`) imports.
- `using Microsoft.AspNetCore.Identity;` (`UserManager`).
- `using Microsoft.EntityFrameworkCore;` (`AnyAsync`, `DbUpdateException`).

---

### 2. `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs` (view-model, data-bag)

**Analog:** `Models/ViewModels/CMPRecordsViewModel.cs`

**Imports + namespace pattern** (lines 1-4 verbatim):
```csharp
using HcPortal.Models;

namespace HcPortal.Models.ViewModels
{
    public class CMPRecordsViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        // ... plain auto-prop POCO ...
    }
}
```
**Adaptation Phase 341:**
- Drop `using HcPortal.Models;` import (Phase 341 ViewModel has no entity refs — only `int`, `string`, `bool`, `List<>`).
- Keep `namespace HcPortal.Models.ViewModels`.
- Define two classes in same file: `ManageOrgLevelLabelsViewModel` (top-level) + `LabelRowVM` (nested type but kept flat in same namespace for Razor `@model` simplicity).

**Core pattern — POCO with auto-props + collection init**:
```csharp
public List<LabelRowVM> Rows { get; set; } = new();   // pattern: List + init
public int MaxConfigured { get; set; }                 // pattern: scalar int
```
Both lines from CMPRecordsViewModel L9, L12 (mixed scalar + collection auto-props).

**Final shape** (per RESEARCH §Code Examples #5):
```csharp
namespace HcPortal.Models.ViewModels
{
    public class ManageOrgLevelLabelsViewModel
    {
        public List<LabelRowVM> Rows { get; set; } = new();
        public int MaxConfigured { get; set; }
        public int MaxUsed { get; set; }
        public int NextAddLevel { get; set; }
    }

    public class LabelRowVM
    {
        public int Level { get; set; }
        public string? Label { get; set; }   // null = "(belum diset)" buffer row
        public bool IsHighest { get; set; }
        public bool IsUsed { get; set; }
        public bool CanDelete { get; set; }
    }
}
```

**No analog for nullable-string + computed-bool combo** — design new, but matches general C# nullable-aware conventions used elsewhere in codebase.

---

### 3. `Views/Admin/ManageOrgLevelLabels.cshtml` (view, server-render + AJAX mutation)

**Analog primary:** `Views/Admin/ManageOrganization.cshtml` (modal + antiforgery + shared-toast scripts section)
**Analog secondary:** `Views/Admin/AssessmentMonitoringDetail.cshtml:325-326` (native confirm() pattern, BUT adapted per D-01 fetch instead of form POST)

**Imports + ViewData pattern** — top of file (new code, but conventional Razor):
```html
@model HcPortal.Models.ViewModels.ManageOrgLevelLabelsViewModel
@{
    ViewData["Title"] = "Kelola Label Tier Organisasi";
}
```

**Bootstrap modal pattern** — copy verbatim layout from `Views/Admin/ManageOrganization.cshtml:133-164`:
```html
<div class="modal fade" id="unitModal" tabindex="-1" aria-labelledby="unitModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title" id="unitModalLabel">Tambah Unit</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <input type="hidden" id="unitModalId" value="" />
                <div class="mb-3">
                    <label for="unitModalName" class="form-label fw-bold">Nama <span class="text-danger">*</span></label>
                    <input type="text" id="unitModalName" class="form-control" placeholder="Nama unit" required maxlength="100" />
                    <div class="invalid-feedback">Nama tidak boleh kosong.</div>
                </div>
                ...
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                <button type="button" class="btn btn-primary" id="unitModalSubmit" onclick="submitUnitModal()">
                    <i class="bi bi-save me-1"></i>Simpan
                </button>
            </div>
        </div>
    </div>
</div>
```
**Adaptation Phase 341 (C-01, Pitfall 5 — distinct modal ids):**
- Modal #1 id=`labelEditModal` (aria-labelledby=`labelEditModalLabel`)
  - Hidden input id=`labelEditLevel` (PK, server-set on open)
  - Disabled input id=`labelEditLevelDisplay` (shows level number, non-submit)
  - Editable input id=`labelEditValue` (`maxlength="50"`, `required`)
  - Submit button → `onclick="submitEdit()"`
- Modal #2 id=`labelAddModal` (aria-labelledby=`labelAddModalLabel`)
  - Hidden input id=`labelAddLevel` (server-injected `@Model.NextAddLevel`)
  - Disabled input id=`labelAddLevelDisplay` (`value="@Model.NextAddLevel"`)
  - Editable input id=`labelAddValue` (`maxlength="50"`, `required`)
  - Submit button → `onclick="submitAdd()"`

**Antiforgery token render** (D-06) — Razor helper, place once outside any form:
```html
@Html.AntiForgeryToken()
```
Equivalent placement at `Views/Admin/AssessmentMonitoringDetail.cshtml:327` (inside `<form>`). Phase 341 places at top-level scope since no `<form>` wrapper (AJAX fetch reads from hidden input).

**Scripts section pattern** — copy verbatim from `Views/Admin/ManageOrganization.cshtml:189-196`:
```html
@section Scripts {
    <script src="https://cdn.jsdelivr.net/npm/sortablejs@1.15.7/Sortable.min.js"></script>
    <script src="~/js/shared-toast.js"></script>
    <script src="~/js/orgTree.js" asp-append-version="true"></script>
    <script>
        document.addEventListener('DOMContentLoaded', initTree);
    </script>
}
```
**Adaptation Phase 341 (D-07, OQ#3 resolved → inline minimal JS per RESEARCH recommendation):**
```html
@section Scripts {
    <script src="~/js/shared-toast.js"></script>
    <script>
        function getAntiForgeryToken() {
            return document.querySelector('input[name="__RequestVerificationToken"]').value;
        }

        async function ajaxPost(url, data) {
            const params = new URLSearchParams(data);
            params.append('__RequestVerificationToken', getAntiForgeryToken());
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
                body: params.toString()
            });
            return res.json();
        }

        // openEditModal, openAddModal, submitEdit, submitAdd, confirmDelete ...
    </script>
}
```
Drop SortableJS (no drag-drop in Phase 341). Drop `~/js/orgTree.js` (avoid coupling — inline minimal `getAntiForgeryToken` + `ajaxPost` matches OQ#3 decision).

**Antiforgery JS extraction pattern** — copy from `wwwroot/js/orgTree.js:8-26`:
```javascript
function getAntiForgeryToken() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
}

async function ajaxPost(url, data = {}) {
    const params = new URLSearchParams(data);
    params.append('__RequestVerificationToken', getAntiForgeryToken());
    const res = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: params.toString()
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
}
```
**Adaptation:** Verbatim inline copy. Optional: drop `if (!res.ok) throw` line for terser version (caller handles success flag in JSON body).

**Native confirm() delete pattern** — adapt from `Views/Admin/AssessmentMonitoringDetail.cshtml:325-326`:
```html
<form ... onsubmit="return confirm('Reset sesi ini? ...')">
```
**Adaptation Phase 341 (D-03, D-01):** Move confirm() into JS function (no form POST per D-01):
```javascript
async function confirmDelete(level, label) {
    if (!confirm(`Hapus label Level ${level} "${label}"? Tidak bisa diundo.`)) return;
    const result = await ajaxPost('/Admin/DeleteLevelLabel', { level });
    if (result.success) {
        showToast(result.message, 'success');
        setTimeout(() => window.location.reload(), 600);
    } else {
        showToast(result.message, 'danger');
    }
}
```
**Verbatim confirm() text** per CONTEXT.md §Specifics: `'Hapus label Level {N} "{label}"? Tidak bisa diundo.'`.

**Toast feedback pattern** — call `showToast(message, type)` from `wwwroot/js/shared-toast.js:6-15`:
```javascript
function showToast(message, type) {
    var icon = type === 'success' ? 'check-circle' : 'exclamation-triangle';
    var toast = document.createElement('div');
    toast.className = 'alert alert-' + type + ...
}
```
**Adaptation:** Use `type='success'` for OK, `type='danger'` for error (OQ#4 resolved — codebase convention `'danger'` not `'error'`; shared-toast accepts any Bootstrap variant).

**Server-side row render loop** (D-10 server-render) — new code (no exact analog of "iterate 0..max+1"):
```html
<tbody>
@foreach (var row in Model.Rows)
{
    <tr>
        <td>@row.Level</td>
        @if (row.Label == null)
        {
            <td><em class="text-muted">(belum diset)</em></td>
            <td>
                @if (row.Level == Model.NextAddLevel)
                {
                    <button class="btn btn-sm btn-outline-primary" onclick="openAddModal(@row.Level)">
                        <i class="bi bi-plus-circle me-1"></i>Tambah
                    </button>
                }
            </td>
        }
        else
        {
            <td>@row.Label</td>
            <td>
                <button class="btn btn-sm btn-outline-secondary"
                        onclick="openEditModal(@row.Level, '@Html.Raw(Json.Serialize(row.Label).ToString().Trim('"'))')">
                    <i class="bi bi-pencil-square me-1"></i>Edit
                </button>
                @if (row.CanDelete) { ... Delete button ... }
                else if (row.IsHighest && row.IsUsed) { ... disabled Delete + tooltip ... }
            </td>
        }
    </tr>
}
</tbody>
```
Pattern follows CONTEXT.md §Specifics layout literal. No analog (Phase 341 is first dynamic-buffer-row admin table); structure inferred from `Views/Admin/Index.cshtml` `@if` role-gating + Razor `@foreach`.

---

### 4. `Views/Admin/Index.cshtml` (view, static navigation — 1 card insert)

**Analog:** itself — replicate the ManageOrganization card block at `Views/Admin/Index.cshtml:35-50` verbatim, insert after L50 (closing `}`).

**Existing card block** (verified `Views/Admin/Index.cshtml:35-50` verbatim):
```html
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-md-4">
    <a href="@Url.Action("ManageOrganization", "Organization")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-diagram-3 fs-5 text-primary"></i>
                    <span class="fw-bold">Organization Structure</span>
                </div>
                <small class="text-muted">Kelola hierarki Bagian dan Unit kerja dengan tampilan tree</small>
            </div>
        </div>
    </a>
</div>
}
```

**Adaptation Phase 341 (D-04)** — insert this block after L50 (before the next `@if` at L51):
```html
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-md-4">
    <a href="@Url.Action("ManageOrgLevelLabels", "OrgLabel")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-tags fs-5 text-primary"></i>
                    <span class="fw-bold">Label Tier Organisasi</span>
                </div>
                <small class="text-muted">Kelola nama tier organisasi (Bagian/Unit/Sub-unit) tanpa edit kode</small>
            </div>
        </div>
    </a>
</div>
}
```
**Changes from analog:**
- `Url.Action("ManageOrganization", "Organization")` → `Url.Action("ManageOrgLevelLabels", "OrgLabel")`
- Icon `bi-diagram-3` → `bi-tags`
- Card title `Organization Structure` → `Label Tier Organisasi`
- Subtitle copy → `Kelola nama tier organisasi (Bagian/Unit/Sub-unit) tanpa edit kode`

No structural changes — copy/paste + 4 string substitutions.

---

### 5. `HcPortal.Tests/OrgLabelControllerTests.cs` (test, unit test)

**Analog primary:** `HcPortal.Tests/OrgLabelServiceTests.cs` (factory pattern + in-memory DB)
**Analog secondary:** none — codebase has zero existing Controller-level xUnit tests (verified via `Glob HcPortal.Tests/**/*.cs` → 3 test files, all service/helper level)

**Imports pattern** (verbatim from `OrgLabelServiceTests.cs:1-8`):
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;
```
**Adaptation Phase 341:** Add `using HcPortal.Controllers;` + `using HcPortal.Models.ViewModels;` (for assert on ViewModel shape). Drop `Microsoft.Extensions.Caching.Memory` only if not constructing service directly.

**Factory pattern** (verbatim from `OrgLabelServiceTests.cs:25-45`):
```csharp
private static (OrgLabelService svc, ApplicationDbContext ctx) MakeServiceWithCtx(bool seed3Rows = true)
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    var ctx = new ApplicationDbContext(options);

    if (seed3Rows)
    {
        ctx.OrganizationLevelLabels.AddRange(
            new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" }
        );
        ctx.SaveChanges();
    }

    var cache = new MemoryCache(new MemoryCacheOptions());
    var auditLog = new AuditLogService(ctx);
    return (new OrgLabelService(ctx, cache, auditLog), ctx);
}
```

**Adaptation Phase 341 — extend factory to return Controller** (NEW — no analog factory for controller; design):
```csharp
private static (OrgLabelController ctrl, ApplicationDbContext ctx) MakeControllerWithCtx(bool seed3Rows = true)
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    var ctx = new ApplicationDbContext(options);

    if (seed3Rows)
    {
        ctx.OrganizationLevelLabels.AddRange(
            new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" }
        );
        ctx.SaveChanges();
    }

    var cache = new MemoryCache(new MemoryCacheOptions());
    var auditLog = new AuditLogService(ctx);
    var svc = new OrgLabelService(ctx, cache, auditLog);

    // UserManager stub — NEW (no direct analog).
    // Planner decision: mock or null-substitute. Recommended: Moq for UserManager<ApplicationUser> + return seeded fake user with NIP/FullName.
    var userManager = StubUserManager(); // helper TBD by planner

    var ctrl = new OrgLabelController(svc, ctx, userManager);
    // Set HttpContext.User stub so currentUser resolution works:
    ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

    return (ctrl, ctx);
}
```
**NOTE:** `UserManager<ApplicationUser>` mock is the tricky bit — no precedent in `HcPortal.Tests`. Planner options:
1. Add `Moq` package (≤200KB, mainstream xUnit companion) and mock `_userManager.GetUserAsync(User)` to return a fake `ApplicationUser`.
2. Skip actor-resolution tests at controller level; assert validation-path tests only (which don't need UserManager).
3. Use `UserManager` real constructor with all-null deps (likely throws NPE — not viable).

Planner picks option in plan; recommended option 2 (skip UserManager paths) to avoid new dependency — covers SC#4 (validation rejection) which IS the high-value test surface per CONTEXT §Success Definition.

**[Fact] test structure** (verbatim from `OrgLabelServiceTests.cs:49-57`):
```csharp
[Fact]
public void GetLabel_KnownLevel_ReturnsConfiguredLabel()
{
    var svc = MakeService();

    Assert.Equal("Bagian",   svc.GetLabel(0));
    Assert.Equal("Unit",     svc.GetLabel(1));
    Assert.Equal("Sub-unit", svc.GetLabel(2));
}
```
**Adaptation Phase 341 — target tests** (per CONTEXT.md SC#4 success criterion):

| Test | Action | Expected |
|------|--------|----------|
| `UpdateLevelLabel_EmptyLabel_ReturnsJsonFailure` | POST `UpdateLevelLabel(0, "")` | `Json` body `{ success=false, message=~"kosong" }` |
| `UpdateLevelLabel_WhitespaceLabel_ReturnsJsonFailure` | POST `UpdateLevelLabel(0, "   ")` | same |
| `UpdateLevelLabel_TooLong_ReturnsJsonFailure` | POST `UpdateLevelLabel(0, new string('x', 51))` | `{ success=false, message=~"50" }` |
| `UpdateLevelLabel_DuplicateAcrossLevels_ReturnsJsonFailure` | POST `UpdateLevelLabel(0, "Unit")` (already at level 1) | `{ success=false, message=~"sudah dipakai" }` |
| `AddLevelLabel_NonNextLevel_ReturnsJsonFailure` | POST `AddLevelLabel(99, "X")` | `{ success=false, message=~"Hanya level berikutnya" }` |
| `DeleteLevelLabel_NonHighest_ReturnsJsonFailure` | POST `DeleteLevelLabel(0)` (when max=2) | `{ success=false, message=~"level tertinggi" }` |
| `DeleteLevelLabel_HighestInUse_ReturnsJsonFailure` | Seed `OrganizationUnits {Level=2}` + POST `DeleteLevelLabel(2)` | `{ success=false, message=~"dipakai unit" }` |
| `UpdateLevelLabel_Happy_ReturnsJsonSuccess` | POST `UpdateLevelLabel(0, "Direktorat")` | `{ success=true }` + DB row Level 0 has Label "Direktorat" |

Test count: 8 [Fact] (matches CONTEXT.md scope hint "validation paths + happy path").

**Assertion pattern for JsonResult** (NEW — no analog at controller level):
```csharp
var result = await ctrl.UpdateLevelLabel(0, "");
var json = Assert.IsType<JsonResult>(result);
dynamic body = json.Value!;
Assert.False((bool)body.success);
Assert.Contains("kosong", (string)body.message);
```
**Note:** Anonymous-object `Json(new { success, message })` may need `dynamic` or reflection for typed access. Planner picks: dynamic (terse) or `JObject` parse (Newtonsoft, but project may not have it). Recommended: reflection helper `result.Value.GetType().GetProperty("success")?.GetValue(result.Value)` — zero-dep.

---

## Shared Patterns

### Authentication

**Source:** `Controllers/OrganizationController.cs:71-73` (class-level + method-level layered)
**Apply to:** All new POST actions in `OrgLabelController` (UpdateLevelLabel, AddLevelLabel, DeleteLevelLabel)

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```

**Decision rationale:** Per D-09, class-level `[Authorize]` covers `GetLevelLabels` (any authenticated), method-level `[Authorize(Roles="Admin,HC")]` restricts mutation + page-render actions. Layered model — most-restrictive wins.

### Error Handling

**Source:** `Controllers/OrganizationController.cs:410-421` (try/catch DbUpdateException → friendly JSON) + RESEARCH §Pitfall 4
**Apply to:** All 3 mutation actions

```csharp
try
{
    await _orgLabels.UpdateAsync(level, label, userId, actorName);
    return Json(new { success = true, message = "..." });
}
catch (InvalidOperationException ex)
{
    return Json(new { success = false, message = ex.Message });
}
catch (DbUpdateException)   // AddLevelLabel only — race protection (Pitfall 8)
{
    return Json(new { success = false, message = "Level sudah ada, silakan refresh halaman." });
}
```

### Validation

**Source:** `Controllers/OrganizationController.cs:76-93` (inline `IsNullOrWhiteSpace` + `AnyAsync` duplicate check)
**Apply to:** UpdateLevelLabel, AddLevelLabel — all validate `label` argument
**Pattern (D-05):**
```csharp
if (string.IsNullOrWhiteSpace(label))
    return Json(new { success = false, message = "Label tidak boleh kosong." });

label = label.Trim();   // RESEARCH §Pitfall 3 — trim BEFORE next checks

if (label.Length > 50)
    return Json(new { success = false, message = "Label maksimal 50 karakter." });

bool duplicate = await _context.OrganizationLevelLabels
    .AnyAsync(l => l.Label == label && l.Level != currentLevel);
if (duplicate)
    return Json(new { success = false, message = $"Label '{label}' sudah dipakai level lain." });
```

### Actor Resolution

**Source:** `Controllers/OrganizationController.cs:425-428`
**Apply to:** UpdateLevelLabel, AddLevelLabel, DeleteLevelLabel (Phase 340 service requires `userId` + `actorName`)

```csharp
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";
```

### View Resolution Override

**Source:** `Controllers/OrganizationController.cs:24-27`
**Apply to:** `OrgLabelController` (mandatory — Pitfall 1)

4-overload `View()` redirect to `~/Views/Admin/{ActionName}.cshtml`.

### AntiForgery Token (Razor + JS layered contract)

**Sources:**
- View: `Views/Admin/ManageOrganization.cshtml` (implicit `@Html.AntiForgeryToken()` rendered by form helper) + `Views/Admin/AssessmentMonitoringDetail.cshtml:327` (explicit `@Html.AntiForgeryToken()`)
- JS: `wwwroot/js/orgTree.js:8-26` (`getAntiForgeryToken()` + `ajaxPost` URLSearchParams append)

**Apply to:** `Views/Admin/ManageOrgLevelLabels.cshtml` — render token once at page scope + inline JS reads from `input[name="__RequestVerificationToken"]`. Three-sided contract (controller `[ValidateAntiForgeryToken]` + view `@Html.AntiForgeryToken()` + JS `params.append`).

### Toast Feedback

**Source:** `wwwroot/js/shared-toast.js:6-15` + `Views/Admin/ManageOrganization.cshtml:191` (script include)
**Apply to:** `Views/Admin/ManageOrgLevelLabels.cshtml` — include `<script src="~/js/shared-toast.js"></script>` in `@section Scripts` + call `showToast(message, type)` after each AJAX completion. Use `'success'` and `'danger'` (NOT `'error'`).

---

## No Analog Found

| File / Sub-pattern | Reason | Planner Action |
|--------------------|--------|----------------|
| Controller-level unit test (`OrgLabelControllerTests.cs` top-level) | Codebase has zero existing Controller xUnit tests (only services + helpers) | Use `OrgLabelServiceTests.cs` factory pattern + design new `MakeControllerWithCtx` helper (see §5 above). Decide `UserManager` mocking strategy (recommended skip option 2). |
| Dynamic buffer-row table (auto-render `0..max+1` with last row "(belum diset)") | No precedent — Phase 341 first instance | Build new per RESEARCH §Code Examples #6 view skeleton. |
| `dynamic`/reflection assertion on anonymous-object `Json(...)` body | No precedent — first controller xUnit | Helper inline reflection getter: `(bool)json.Value.GetType().GetProperty("success")!.GetValue(json.Value)!`. Or shorter: `dynamic body = json.Value!;`. |

---

## Metadata

**Analog search scope:** `Controllers/`, `Views/Admin/`, `Models/ViewModels/`, `wwwroot/js/`, `HcPortal.Tests/`
**Files scanned:** 8 (4 controller, 1 view-model, 2 view, 2 js, 1 test)
**Pattern extraction date:** 2026-06-03
**Verification:** Every code excerpt above was directly Read from disk during this mapping — no second-hand quoting via RESEARCH.md alone.
