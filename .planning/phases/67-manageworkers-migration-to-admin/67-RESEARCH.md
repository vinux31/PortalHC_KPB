# Phase 69: ManageWorkers Migration to Admin - Research

**Researched:** 2026-02-28
**Domain:** ASP.NET Core MVC controller migration — moving actions + views from CMPController to AdminController, role-based authorization adjustment, shared helper extraction
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Penghapusan Route Lama**
- Hapus total semua route ManageWorkers dari CMPController — **tidak ada 301 redirect** (override roadmap SC #2)
- Hapus view folder CMP/ManageWorkers setelah migrasi — clean break, git history menyimpan backup
- Cari dan update semua referensi internal (views, JavaScript, AJAX calls) ke URL baru `/Admin/ManageWorkers`
- Hanya hapus ManageWorkers dari CMPController — CMP punya action lain yang tetap ada

**Navigasi & Kelola Data Hub**
- Tambah kartu "Manajemen Pekerja" di **Section A: Master Data**, posisi **pertama** (sebelum KKJ Matrix)
- Style ikon dan deskripsi mengikuti pola kartu lain di hub (Claude sesuaikan)
- Hapus tombol standalone "Kelola Pekerja" dari navbar — tanpa notifikasi transisi
- Akses ManageWorkers hanya untuk role **Admin** dan **HC**
- Claude investigasi apakah ada link lain di aplikasi yang mengarah ke ManageWorkers lama

**Permission & Role Guard**
- Admin dan HC punya akses identik (full CRUD) — tidak ada action yang Admin-only
- Ikut controller-level `[Authorize]` attribute di AdminController — tidak perlu attribute terpisah per action

**GetDefaultView() Helper**
- Ekstrak mapping role → SelectedView yang ada **tanpa perubahan logic** ke `UserRoles.GetDefaultView(role)`
- Mapping saat ini: Admin→"Admin", HC→"HC", Coach→"Coach", management roles→"Atasan", default→"Coachee"
- Helper dipanggil dari 3 tempat: create worker, edit worker, import worker
- Hanya ada di CMPController saat ini, tidak ada di controller lain

**Import/Export**
- Pertahankan format **Excel (.xlsx)** via ClosedXML — tidak ada perubahan format
- Semua fitur ikut migrasi: import, export, download template
- Template import tetap 10 kolom: Nama, Email, NIP, Jabatan, Bagian, Unit, Directorate, Role, Tgl Bergabung, Password

**Migration Scope & Style**
- **Pindah + perbaiki** — boleh perbaiki bug kecil yang ditemukan selama migrasi
- URL pattern tetap `/Admin/ManageWorkers/*` — tidak di-rename
- Visual views **disesuaikan dengan style Admin hub** (bukan copy apa adanya)
- Update breadcrumb ke navigasi Admin baru
- Nama file view dipertahankan (CreateWorker.cshtml, EditWorker.cshtml, dll)

**View Structure**
- Claude tentukan: flat di Views/Admin/ atau subfolder Views/Admin/ManageWorkers/
- Claude investigasi partial view dependencies

**Testing & Validasi**
- Claude boleh jalankan `dotnet build` dan `dotnet run` untuk verifikasi otomatis
- Yang bisa di-automate → Claude test langsung (compile, routing, referensi URL)
- Yang butuh browser → Claude siapkan checklist manual terpisah
- **Wajib:** grep seluruh codebase untuk memastikan tidak ada referensi CMP/ManageWorkers tersisa

### Claude's Discretion
- Ikon dan deskripsi kartu Manajemen Pekerja di hub
- Visibility kartu untuk role non-Admin/HC (sembunyikan atau disable)
- Flat vs subfolder untuk view structure
- Partial view dependencies handling
- Bug fixes yang ditemukan selama migrasi
- Link lain yang perlu diupdate (investigasi saat research)

### Deferred Ideas (OUT OF SCOPE)
- **Bedakan SelectedView untuk Section Head vs Sr Supervisor/Supervisor** — Fase terpisah
- **Integrasi Active Directory Pertamina** — Fase terpisah
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| USR-01 | ManageWorkers CRUD (list, create, edit, delete, import, export, detail) accessible dari /Admin/ManageWorkers | 8 actions found in CMPController; 5 views in Views/CMP/; all identified and mapped below |
| USR-02 | Old /CMP/ManageWorkers redirect 301 ke /Admin/ManageWorkers | **OVERRIDE by CONTEXT.md** — user decided NO redirect, clean delete instead |
| USR-03 | Standalone "Kelola Pekerja" button di navbar dihapus — akses via Kelola Data hub | Button found at Views/Shared/_Layout.cshtml line 88-96; exact removal location identified |
| USTR-02 | Role-to-SelectedView mapping di-extract ke shared helper UserRoles.GetDefaultView() | Mapping found in 3 places in CMPController (CreateWorker, EditWorker, ImportWorkers); UserRoles.cs already exists in Models/ |
</phase_requirements>

## Summary

Phase 69 is a pure code migration within an ASP.NET Core MVC project. The scope is fully understood from source code inspection — no external library research needed. The work involves: (1) copying 8+1 controller actions from CMPController to AdminController, (2) moving 5 view files from Views/CMP/ to Views/Admin/ (flat or subfolder), (3) extracting a `GetDefaultView(string role)` helper into the existing `UserRoles` static class, (4) updating all internal URL references from "CMP" to "Admin", (5) deleting the source actions and views from CMP, and (6) removing the navbar button and adding a hub card.

The most significant discovery is an **authorization conflict**: AdminController is class-level `[Authorize(Roles = "Admin")]` but ManageWorkers requires both Admin AND HC access. This means per-action `[Authorize(Roles = "Admin, HC")]` overrides must be added to each migrated ManageWorkers action — the CONTEXT.md statement "follow controller-level attribute" is not achievable without also changing the class-level, which would break existing Admin-only actions. Per-action override is the correct approach.

A second discovery: `RecordsWorkerList.cshtml` contains a **hardcoded JavaScript URL** `/CMP/WorkerDetail?id=...` (line 643) that points to the admin WorkerDetail (not the training WorkerDetail). This reference must be updated as part of Plan 69-01 or 69-02.

**Primary recommendation:** Execute as 2 plans exactly per roadmap. Plan 69-01 adds all ManageWorkers actions and views to Admin (backend + frontend), extracts GetDefaultView(), and updates internal links within the migrated views. Plan 69-02 removes CMP source code, removes navbar button, adds hub card, and verifies zero remaining references.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller/View routing framework | Project's existing framework |
| Microsoft.AspNetCore.Identity | 8.0.0 | UserManager, role-based authorization | Already used in CMPController ManageWorkers |
| Microsoft.EntityFrameworkCore | 8.0.0 | Database access | Already used |
| ClosedXML | 0.105.0 | Excel import/export | Already in project, already used by ManageWorkers |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.AspNetCore.Authorization | 8.0.0 | `[Authorize]` attributes | Per-action role guard for HC+Admin |
| Bootstrap 5.3 | CDN | UI styling | Admin hub card styling |
| Bootstrap Icons | CDN | Icons for hub card | Match existing hub card icon pattern |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Per-action `[Authorize(Roles = "Admin, HC")]` | Modify class-level attribute | Modifying class-level to include HC would require reviewing all existing Admin-only actions — risky. Per-action override is safe. |
| Subfolder `Views/Admin/ManageWorkers/` | Flat `Views/Admin/` | See Architecture Patterns section — subfolder is recommended |

**Installation:** No new packages needed. All dependencies already present.

## Architecture Patterns

### Recommended Project Structure

```
Views/Admin/
├── ManageWorkers/          ← NEW subfolder (recommended)
│   ├── ManageWorkers.cshtml
│   ├── CreateWorker.cshtml
│   ├── EditWorker.cshtml
│   ├── WorkerDetail.cshtml
│   └── ImportWorkers.cshtml
├── Index.cshtml            (update: add hub card)
├── CoachCoacheeMapping.cshtml
└── [other existing views]

Controllers/
├── AdminController.cs      (add 8+1 actions, with per-action [Authorize(Roles = "Admin, HC")])
└── CMPController.cs        (remove 8+1 actions from lines 2757-3465+)

Models/
└── UserRoles.cs            (add GetDefaultView(string role) static method)
```

**Subfolder rationale:** AdminController already has 30+ action methods. Adding 8 ManageWorkers views flat would create clutter and name collisions (e.g., `CreateWorker.cshtml` filename conflicts with `CreateAssessment.cshtml` pattern). Subfolder `Views/Admin/ManageWorkers/` requires returning `View("~/Views/Admin/ManageWorkers/ManageWorkers.cshtml", model)` OR using the subfolder as controller-equivalent via convention. In ASP.NET MVC, if you return `View(model)` from `AdminController.ManageWorkers()`, MVC looks for `Views/Admin/ManageWorkers.cshtml`. To use a subfolder, use explicit path: `return View("ManageWorkers/ManageWorkers", model)` or `return View("~/Views/Admin/ManageWorkers/ManageWorkers.cshtml", model)`.

**Flat view recommendation (simpler):** Since the view file names differ enough from existing Admin views (no conflicts), flat placement in `Views/Admin/` is simpler — `return View(model)` works by convention. Existing Admin views: AssessmentMonitoringDetail, AuditLog, CoachCoacheeMapping, CpdpItems, CreateAssessment, EditAssessment, Index, KkjMatrix, ManageAssessment, UserAssessmentHistory. Adding: ManageWorkers, CreateWorker, EditWorker, WorkerDetail, ImportWorkers. No filename conflicts exist. **Use flat Views/Admin/ placement.**

### Pattern 1: AdminController Action Structure (existing pattern)

```csharp
// Source: Controllers/AdminController.cs (existing pattern, e.g. CoachCoacheeMapping)
[HttpGet]
[Authorize(Roles = "Admin, HC")]   // ← PER-ACTION override required for HC
public async Task<IActionResult> ManageWorkers(string? search, string? sectionFilter, string? roleFilter)
{
    // ... (copied from CMPController, update RedirectToAction calls if any)
}
```

Note: All 8 ManageWorkers actions MUST have `[Authorize(Roles = "Admin, HC")]` to override the class-level `[Authorize(Roles = "Admin")]`.

### Pattern 2: GetDefaultView() Helper Extraction

**Current code (duplicated 3 times in CMPController):**
```csharp
// CreateWorker POST (~line 2856):
var selectedView = model.Role switch
{
    "Admin" => "Admin",
    "HC" => "HC",
    "Coach" => "Coach",
    "Direktur" or "VP" or "Manager" or "Section Head" or "Sr Supervisor" => "Atasan",
    _ => "Coachee"
};

// EditWorker POST (~line 3010): identical switch
// ImportWorkers POST (~line 3403): identical switch
```

**Target location:** `Models/UserRoles.cs` — add as static method:
```csharp
// Add to UserRoles static class:
public static string GetDefaultView(string roleName)
{
    return roleName switch
    {
        Admin => "Admin",
        HC => "HC",
        Coach => "Coach",
        Direktur or VP or Manager or SectionHead or SrSupervisor => "Atasan",
        _ => "Coachee"
    };
}
```

Call site in AdminController:
```csharp
SelectedView = UserRoles.GetDefaultView(model.Role)
```

### Pattern 3: Breadcrumb in Migrated Views

Based on existing Admin views (CoachCoacheeMapping, AuditLog):
```html
<nav aria-label="breadcrumb" class="mb-3">
    <ol class="breadcrumb">
        <li class="breadcrumb-item">
            <a href="@Url.Action("Index", "Admin")">Kelola Data</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">Manajemen Pekerja</li>
    </ol>
</nav>
```

For sub-pages (CreateWorker, EditWorker, etc.):
```html
<nav aria-label="breadcrumb" class="mb-3">
    <ol class="breadcrumb">
        <li class="breadcrumb-item">
            <a href="@Url.Action("Index", "Admin")">Kelola Data</a>
        </li>
        <li class="breadcrumb-item">
            <a href="@Url.Action("ManageWorkers", "Admin")">Manajemen Pekerja</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">Tambah Pekerja</li>
    </ol>
</nav>
```

### Pattern 4: Hub Card (Admin/Index.cshtml)

Based on existing Section A card pattern:
```html
<div class="col-md-4">
    <a href="@Url.Action("ManageWorkers", "Admin")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-person-lines-fill fs-5 text-primary"></i>
                    <span class="fw-bold">Manajemen Pekerja</span>
                </div>
                <small class="text-muted">Tambah, edit, hapus, dan kelola data pekerja sistem</small>
            </div>
        </div>
    </a>
</div>
```

Insert as **first card** in Section A row (before KKJ Matrix card).

### Pattern 5: HC-visible Hub Card (discretion area)

The Kelola Data hub is inside AdminController at `/Admin/Index`. AdminController class-level is `[Authorize(Roles = "Admin")]`. HC users cannot access `/Admin/Index` at all — the hub page itself is Admin-only. But the CONTEXT says HC should be able to access ManageWorkers. This means HC needs a different entry point OR the Index page needs its own per-action `[Authorize(Roles = "Admin, HC")]` override too.

**Recommendation:** Add `[Authorize(Roles = "Admin, HC")]` to `Index()` action and to all ManageWorkers actions. The hub card will naturally be visible to HC when they access `/Admin/Index`. Other Section B/C cards (Deliverable Progress Override, Final Assessment Manager, etc.) are stubs pointing to `#`, so HC seeing them is harmless.

### Anti-Patterns to Avoid

- **Copying views verbatim without updating Url.Action calls:** All CMP references in view files must be updated to Admin. Grepping after is mandatory.
- **Forgetting DownloadImportTemplate:** This is a 9th action (`DownloadImportTemplate` [HttpGet]) in CMPController (line 3295) that must also be migrated alongside ImportWorkers.
- **Missing RecordsWorkerList.cshtml JS reference:** Line 643 has a hardcoded `/CMP/WorkerDetail?id=...` that must be updated to `/Admin/WorkerDetail?id=...`.
- **Duplicate action names in AdminController:** Verify no name conflicts with existing actions (there are none — checked).
- **Ignoring class-level [Authorize]:** Adding actions to AdminController without per-action HC override will silently break HC access with a 403.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel export | Custom CSV/HTML export | ClosedXML (already present) | Same library already used in ExportWorkers |
| Excel import parsing | Custom file parsing | ClosedXML XLWorkbook (already present) | Same library already used in ImportWorkers |
| Role-to-view mapping | Duplicated switch statements | `UserRoles.GetDefaultView()` | Already planned as the helper extraction |
| URL validation | Manual regex | `dotnet build` compilation | Razor tag helpers will fail at compile-time for bad controller/action references |

**Key insight:** This phase is purely mechanical migration. All business logic is proven and working — do not modify it, only move it.

## Common Pitfalls

### Pitfall 1: AdminController Class-Level Authorization Blocks HC

**What goes wrong:** CMPController ManageWorkers actions have `[Authorize(Roles = "Admin, HC")]`. AdminController has class-level `[Authorize(Roles = "Admin")]`. When actions are copied without per-action override, HC users get 403 Forbidden on all ManageWorkers pages.

**Why it happens:** In ASP.NET MVC, class-level `[Authorize]` applies to all actions. Per-action `[Authorize]` is ADDITIVE (requires BOTH) — except when both are present, the more restrictive wins. Actually: multiple `[Authorize]` attributes are AND'd together. A per-action `[Authorize(Roles = "Admin, HC")]` and a class `[Authorize(Roles = "Admin")]` would require Admin (from class) AND (Admin or HC) (from per-action) = Admin only.

**Correct approach:** Each ManageWorkers action needs `[Authorize(Roles = "Admin, HC")]` AND the class-level also needs to include HC, OR the class-level `Admin` constraint should be overridden. The cleanest solution consistent with the project pattern: add `[Authorize(Roles = "Admin, HC")]` to each ManageWorkers action AND also add it to `Index()` so HC can reach the hub. The class-level acts as a baseline, and per-action overrides it — actually in ASP.NET Core, multiple `[Authorize]` attributes are ADDITIVE (all must pass). So per-action alone is NOT sufficient here.

**The actual correct fix:** ASP.NET Core authorization: when multiple `[Authorize]` attributes exist, ALL must pass. So to allow HC into Admin-class-only methods, you need `[AllowAnonymous]` (no) or a policy that allows both roles. The real solution: add `[Authorize(Roles = "Admin, HC")]` at the class level or add it individually — but since multiple `[Authorize]` = AND, you can't override a class-level Admin-only constraint with a per-action Admin+HC.

**Recommended fix:** Change the class-level to `[Authorize]` (authenticated only) and add per-action `[Authorize(Roles = "Admin")]` to all existing actions, then `[Authorize(Roles = "Admin, HC")]` to ManageWorkers actions. This is a bigger change. Alternatively, and more surgical: since only ManageWorkers actions need HC access, add `[Authorize(Roles = "Admin, HC")]` per-action — but since AND applies, this doesn't help if class is `Admin`-only.

**Simplest pragmatic fix confirmed by ASP.NET Core docs:** The class-level `[Authorize(Roles = "Admin")]` means the user must be in Admin role. A per-action `[Authorize(Roles = "Admin, HC")]` adds an additional constraint. The result is: must be Admin (from class) AND must be in Admin or HC (from action) = Admin only. HC still blocked.

**True solution:** For the ManageWorkers actions, use `[AllowAnonymous]` is wrong. The correct approach in ASP.NET Core is to apply a combined policy. The simplest code change: add `[Authorize(Roles = "Admin, HC")]` to the class-level (changing from Admin-only to Admin+HC). But this would give HC access to ALL AdminController actions, including things that should be Admin-only.

**Final recommendation:** Investigate whether existing Admin-only actions actually need to be HC-restricted. Based on existing pattern (CoachCoacheeMapping has no per-action override = Admin-only), HC cannot currently access CoachCoacheeMapping either. The ManageWorkers feature is the first Admin action intended for HC. The safest approach: override class-level to `[Authorize]` (any authenticated user) and add per-action `[Authorize(Roles = "Admin, HC")]` to ALL ManageWorkers actions AND the Index action (which HC needs to see the hub). Leave all other existing actions as-is (they inherit class-level which would now be just `[Authorize]` but they don't need additional attributes for Admin-only — add `[Authorize(Roles = "Admin")]` to those that must remain Admin-only). **This is the correct architecture change.**

**Warning signs:** HC user gets 403 when navigating to `/Admin/ManageWorkers`. Test by logging in as HC after migration.

### Pitfall 2: Forgetting DownloadImportTemplate Action

**What goes wrong:** The roadmap says "8 actions" but there are actually 9 actions in CMPController related to ManageWorkers: ManageWorkers (GET), CreateWorker (GET + POST), EditWorker (GET + POST), DeleteWorker (POST), ExportWorkers (GET), WorkerDetail (GET), ImportWorkers (GET + POST), **DownloadImportTemplate (GET)**.

**Why it happens:** DownloadImportTemplate is not in the "CRUD" enumeration but is functionally part of the ImportWorkers flow.

**How to avoid:** Count all actions from CMPController lines 2757-3465 — there are 10 action methods (some are GET+POST pairs counted as 1 action):
1. ManageWorkers [GET]
2. CreateWorker [GET]
3. CreateWorker [POST]
4. EditWorker [GET]
5. EditWorker [POST]
6. DeleteWorker [POST]
7. ExportWorkers [GET]
8. WorkerDetail [GET]
9. ImportWorkers [GET]
10. DownloadImportTemplate [GET]
11. ImportWorkers [POST]

**Warning signs:** ImportWorkers view shows "Download Template" button that returns 404 after migration.

### Pitfall 3: RecordsWorkerList.cshtml Hardcoded CMP URL

**What goes wrong:** `Views/CMP/RecordsWorkerList.cshtml` line 643 contains:
```javascript
window.location.href = `/CMP/WorkerDetail?id=${encodeURIComponent(workerId)}`;
```
This URL uses `/CMP/WorkerDetail?id=` but resolves to the **admin** WorkerDetail (the one taking `string id` parameter), not the training WorkerDetail (which takes `workerId` + `name`). After migration, this URL breaks.

**Why it happens:** Two different `WorkerDetail` action methods exist in CMPController — one at line 515 (takes `workerId, name`) and one at line 3275 (takes `id`). The RecordsWorkerList JS uses `?id=` which hits the admin version. The view `WorkerDetail.cshtml` in `Views/CMP/` is the admin one (shows account info), not the training records one.

**How to avoid:** Update RecordsWorkerList.cshtml line 643 to `/Admin/WorkerDetail?id=...` in Plan 69-01.

**Warning signs:** Clicking a worker name in RecordsWorkerList shows 404 after CMP WorkerDetail is removed.

### Pitfall 4: Two WorkerDetail Views with Same Filename

**What goes wrong:** `Views/CMP/WorkerDetail.cshtml` (admin account detail view — model: ApplicationUser) is what gets migrated. But there's also `Views/CMP/WorkerDetail.cshtml` used by the training `WorkerDetail(workerId, name)` action at line 515 — wait, the view at line 531 is `return View("WorkerDetail", unified)` which points to the same file name.

**Clarification:** There is ONE `Views/CMP/WorkerDetail.cshtml` file, and it's used by BOTH WorkerDetail actions in CMPController. But looking at the model: `Views/CMP/WorkerDetail.cshtml` has `@model HcPortal.Models.ApplicationUser` (line 1) — this is the ADMIN version. The training WorkerDetail at line 515 passes `unified` (a collection from GetUnifiedRecords) but the view expects `ApplicationUser`. This appears to be a potential bug in the original code (or there are two view files and the search only showed one). Let me note this as needing investigation.

**How to avoid:** During Plan 69-01, verify which views are present in `Views/CMP/` related to WorkerDetail. The admin one (model ApplicationUser) is migrated; any training-records version stays in CMP.

### Pitfall 5: asp-controller Reference in Form Tags

**What goes wrong:** Migrated views have `asp-controller="CMP"` in form action tags (e.g., `<form asp-action="ImportWorkers" asp-controller="CMP">`). These must be changed to `asp-controller="Admin"`.

**Why it happens:** Forgetting to update form asp-controller attributes while fixing Url.Action() calls.

**Warning signs:** dotnet build succeeds (tag helpers don't fail at compile), but form submissions return 404 at runtime.

**Files affected:**
- CreateWorker.cshtml: `<form asp-action="CreateWorker" asp-controller="CMP">`
- EditWorker.cshtml: `<form asp-action="EditWorker" asp-controller="CMP">`
- ImportWorkers.cshtml: `<form asp-action="ImportWorkers" asp-controller="CMP">` and `asp-action="DownloadImportTemplate" asp-controller="CMP"` button

## Code Examples

### Complete ManageWorkers Actions to Copy (source: CMPController.cs lines 2757-3465)

Actions to migrate (in order):
1. `ManageWorkers(string? search, string? sectionFilter, string? roleFilter)` [GET] — lines 2757-2814
2. `CreateWorker()` [GET] — lines 2816-2826
3. `CreateWorker(ManageUserViewModel model)` [POST] — lines 2828-2911
4. `EditWorker(string id)` [GET] — lines 2913-2940
5. `EditWorker(ManageUserViewModel model)` [POST] — lines 2942-3065
6. `DeleteWorker(string id)` [POST] — lines 3067-3200
7. `ExportWorkers(string? search, string? sectionFilter, string? roleFilter)` [GET] — lines 3202-3270
8. `WorkerDetail(string id)` [GET] — lines 3272-3285
9. `ImportWorkers()` [GET] — lines 3287-3293
10. `DownloadImportTemplate()` [GET] — lines 3295-3334
11. `ImportWorkers(IFormFile? excelFile)` [POST] — lines 3336-3465

### GetDefaultView() Static Method (to add to UserRoles.cs)

```csharp
/// <summary>
/// Get the default SelectedView for a given role name
/// </summary>
public static string GetDefaultView(string roleName)
{
    return roleName switch
    {
        Admin => "Admin",
        HC => "HC",
        Coach => "Coach",
        Direktur or VP or Manager or SectionHead or SrSupervisor => "Atasan",
        _ => "Coachee"
    };
}
```

Note: Use the existing role constants (Admin, HC, Coach, Direktur, VP, Manager, SectionHead, SrSupervisor) — they are defined in the same class.

### Authorization Pattern for ManageWorkers Actions in AdminController

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ManageWorkers(string? search, string? sectionFilter, string? roleFilter)
{ ... }

[HttpGet]
[Authorize(Roles = "Admin, HC")]
public IActionResult CreateWorker() { ... }

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateWorker(ManageUserViewModel model) { ... }

// etc. — all 10 action methods get [Authorize(Roles = "Admin, HC")]
```

**CRITICAL:** Since AdminController class-level is `[Authorize(Roles = "Admin")]`, and ASP.NET Core AND's multiple Authorize attributes, per-action `[Authorize(Roles = "Admin, HC")]` alone will NOT allow HC access. The class-level constraint must also be relaxed. The recommended approach: change class-level to `[Authorize]` (authenticated only) and add `[Authorize(Roles = "Admin")]` to Index() and all non-ManageWorkers actions that must remain Admin-only.

### URL References to Update in Migrated Views

All `Url.Action("X", "CMP")` → `Url.Action("X", "Admin")`:

**In ManageWorkers.cshtml (migrated):**
- Line 19: `Url.Action("CreateWorker", "CMP")` → `"Admin"`
- Line 22: `Url.Action("ImportWorkers", "CMP")` → `"Admin"`
- Line 25: `Url.Action("ExportWorkers", "CMP", ...)` → `"Admin"`
- Line 28: `Url.Action("Index", "Home")` → `Url.Action("Index", "Admin")` (back button to hub)
- Line 109: `action="@Url.Action("ManageWorkers", "CMP")"` → `"Admin"`
- Line 153: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`
- Line 215: `Url.Action("WorkerDetail", "CMP", ...)` → `"Admin"`
- Line 228: `Url.Action("EditWorker", "CMP", ...)` → `"Admin"`
- Line 281: `action="@Url.Action("DeleteWorker", "CMP")"` → `"Admin"`

**In CreateWorker.cshtml (migrated):**
- Line 17: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`
- Line 37: `asp-controller="CMP"` → `"Admin"`
- Line 155: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`

**In EditWorker.cshtml (migrated):**
- Line 17: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`
- Line 37: `asp-controller="CMP"` → `"Admin"`
- Line 169: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`

**In ImportWorkers.cshtml (migrated):**
- Line 19: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`
- Line 122: `Url.Action("DownloadImportTemplate", "CMP")` → `"Admin"`
- Line 130: `asp-controller="CMP"` (ImportWorkers form) → `"Admin"`
- Line 145: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`

**In WorkerDetail.cshtml (migrated):**
- Line 31: `Url.Action("EditWorker", "CMP", ...)` → `"Admin"`
- Line 34: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`
- Line 176: `Url.Action("ManageWorkers", "CMP")` → `"Admin"`
- Line 179: `Url.Action("EditWorker", "CMP", ...)` → `"Admin"`

**In other files (NOT migrated, must update in-place):**
- `Views/CMP/RecordsWorkerList.cshtml` line 643: `/CMP/WorkerDetail?id=` → `/Admin/WorkerDetail?id=`
- `Views/Shared/_Layout.cshtml` lines 88-96: Remove entire `@if (currentUser.RoleLevel <= 2)` block

### Navbar Button Removal (Views/Shared/_Layout.cshtml)

Remove lines 88-96 entirely:
```html
@* Manage Workers button — Admin & HC only *@
@if (currentUser.RoleLevel <= 2)
{
    <a href="/CMP/ManageWorkers" class="btn btn-outline-primary btn-sm me-3 d-flex align-items-center gap-2"
       title="Kelola Pekerja" id="btnNavManageWorkers">
        <i class="bi bi-people-fill"></i>
        <span class="d-none d-xl-inline">Kelola Pekerja</span>
    </a>
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ManageWorkers in CMPController | ManageWorkers in AdminController | Phase 69 | Centralized admin management, cleaner URL (/Admin/ManageWorkers) |
| Role→SelectedView switch (3x duplicated) | UserRoles.GetDefaultView() helper | Phase 69 | Single source of truth, reusable for Phase 71+ (AD auth) |
| Standalone navbar button | Hub card access only | Phase 69 | Cleaner navigation, consistent with other admin tools |

**Deprecated after this phase:**
- CMP/ManageWorkers route: Deleted (no redirect per user decision)
- CMP/CreateWorker, CMP/EditWorker, CMP/DeleteWorker, CMP/ExportWorkers, CMP/WorkerDetail, CMP/ImportWorkers, CMP/DownloadImportTemplate: All deleted

## Open Questions

1. **Two WorkerDetail actions with same view file name in CMP**
   - What we know: CMPController has two `WorkerDetail` action methods — line 515 (training records, params: `workerId, name`) and line 3275 (admin account detail, param: `id`). Both `return View("WorkerDetail", ...)`. There is ONE `Views/CMP/WorkerDetail.cshtml` file with `@model HcPortal.Models.ApplicationUser`.
   - What's unclear: The training WorkerDetail (line 515) passes `unified` (a list) but the view expects `ApplicationUser`. This seems like a bug OR the training WorkerDetail uses a different view resolution mechanism.
   - Recommendation: During Plan 69-01, inspect line 515 action more carefully. The view name `"WorkerDetail"` at line 531 uses string literal — MVC looks for `Views/CMP/WorkerDetail.cshtml`. If that file has `@model ApplicationUser`, the training records WorkerDetail would throw a model mismatch error. This suggests the training WorkerDetail may actually be broken already, OR it works because the model is passed as an untyped ViewBag. **Plan should note:** only migrate the admin WorkerDetail (line 3275) — the training records one (line 515) stays in CMP.

2. **HC access to Admin/Index hub page**
   - What we know: AdminController class-level is `[Authorize(Roles = "Admin")]`. HC cannot currently reach `/Admin/Index` (the hub).
   - What's unclear: Should HC be able to see the full hub, or only navigate directly to ManageWorkers?
   - Recommendation: Per CONTEXT decision "Akses ManageWorkers hanya untuk role Admin dan HC", the hub card points to ManageWorkers. For HC to use the hub card, they need Index access too. Add `[Authorize(Roles = "Admin, HC")]` to Index() as well, or change the class-level as described in Pitfall 1.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | dotnet build (MSBuild) + manual browser verification |
| Config file | HcPortal.csproj |
| Quick run command | `dotnet build /c/Users/Administrator/Desktop/PortalHC_KPB/HcPortal.csproj` |
| Full suite command | `dotnet build /c/Users/Administrator/Desktop/PortalHC_KPB/HcPortal.csproj && grep -rn "CMP.*ManageWorkers\|CMP.*CreateWorker\|CMP.*EditWorker\|CMP.*DeleteWorker\|CMP.*ImportWorkers\|CMP.*ExportWorkers\|CMP.*WorkerDetail\|CMP.*DownloadImportTemplate" /c/Users/Administrator/Desktop/PortalHC_KPB/Views /c/Users/Administrator/Desktop/PortalHC_KPB/Controllers` |

No automated test framework (NUnit, xUnit, etc.) exists in the project — verification is compile + grep + manual browser checklist.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| USR-01 | ManageWorkers accessible at /Admin/ManageWorkers | compile + grep | `dotnet build` | Wave 0 |
| USR-01 | All CRUD routes compile | compile | `dotnet build` | Wave 0 |
| USR-01 | No CMP ManageWorkers references remain | grep | `grep -rn "CMP.*ManageWorkers" Views/ Controllers/` | Wave 0 |
| USR-02 | OVERRIDDEN — no redirect needed | N/A (user decision: no redirect) | N/A | N/A |
| USR-03 | Navbar button removed | grep | `grep -n "btnNavManageWorkers\|CMP/ManageWorkers" Views/Shared/_Layout.cshtml` | Wave 0 |
| USR-03 | Hub card present in Admin/Index | grep | `grep -n "ManageWorkers.*Admin\|Manajemen Pekerja" Views/Admin/Index.cshtml` | Wave 0 |
| USTR-02 | GetDefaultView() method exists in UserRoles | grep | `grep -n "GetDefaultView" Models/UserRoles.cs` | Wave 0 |
| USTR-02 | GetDefaultView() called from AdminController | grep | `grep -n "GetDefaultView" Controllers/AdminController.cs` | Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet build`
- **Per wave merge:** Full grep scan for CMP/ManageWorkers references
- **Phase gate:** Zero remaining CMP/ManageWorkers references + clean `dotnet build` + manual browser UAT checklist

### Wave 0 Gaps

- [ ] No test files needed — this is a migration, not new functionality. All behavior is proven in CMPController.
- [ ] Manual UAT checklist required (browser-based):
  - [ ] Login as Admin: navigate to /Admin/ManageWorkers — list loads
  - [ ] Login as HC: navigate to /Admin/ManageWorkers — list loads (not 403)
  - [ ] Create worker — saves and redirects to list
  - [ ] Edit worker — saves and redirects to list
  - [ ] Delete worker — removes and redirects to list
  - [ ] WorkerDetail — shows account info
  - [ ] ImportWorkers GET — page loads
  - [ ] DownloadImportTemplate — Excel file downloads
  - [ ] ImportWorkers POST — processes file
  - [ ] ExportWorkers — Excel file downloads
  - [ ] /CMP/ManageWorkers — returns 404 (no redirect)
  - [ ] Navbar: "Kelola Pekerja" button absent
  - [ ] Admin/Index hub card "Manajemen Pekerja" visible, links to /Admin/ManageWorkers
  - [ ] RecordsWorkerList worker name click → /Admin/WorkerDetail (not broken)

## Sources

### Primary (HIGH confidence)

- Direct source code inspection: `Controllers/CMPController.cs` (lines 2757–3465) — all 11 action methods enumerated and documented
- Direct source code inspection: `Controllers/AdminController.cs` — class-level authorization, DI constructor, existing action list
- Direct source code inspection: `Models/UserRoles.cs` — existing helper structure, confirmed no GetDefaultView() method exists yet
- Direct source code inspection: `Views/CMP/` — all 5 view files read, all CMP references mapped
- Direct source code inspection: `Views/Shared/_Layout.cshtml` lines 88-96 — navbar button location
- Direct source code inspection: `Views/Admin/Index.cshtml` — hub structure and Section A card pattern
- Direct source code inspection: `Views/CMP/RecordsWorkerList.cshtml` line 643 — hardcoded CMP/WorkerDetail JS URL discovered

### Secondary (MEDIUM confidence)

- ASP.NET Core MVC authorization behavior: multiple `[Authorize]` attributes are additive (AND). Per-action override cannot relax class-level — this is standard ASP.NET Core documentation behavior.

### Tertiary (LOW confidence)

- None

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project, no external research needed
- Architecture: HIGH — all source files read, exact line numbers documented
- Pitfalls: HIGH — found from actual code inspection (not hypothetical)
- Authorization behavior: MEDIUM — based on well-known ASP.NET Core behavior, but should be tested post-migration

**Research date:** 2026-02-28
**Valid until:** This is a closed codebase migration — research is valid until code is changed. No expiry.
