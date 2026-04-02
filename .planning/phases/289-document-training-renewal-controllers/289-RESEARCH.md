# Phase 289: Document, Training & Renewal Controllers - Research

**Researched:** 2026-04-02
**Domain:** ASP.NET Core controller extraction / refactoring
**Confidence:** HIGH

## Summary

Phase ini mengekstrak 3 domain terakhir dari AdminController (1,877 baris) ke controller terpisah: DocumentAdminController, TrainingAdminController, dan RenewalController. Pola sudah terbukti 4 kali di Phase 287-288 — ini pure repetition.

**Temuan kritis:** `AdminController.Index()` memanggil `BuildRenewalRowsAsync()` (private method di renewal region) untuk renewal badge count. Setelah method ini pindah ke RenewalController, AdminController.Index() harus punya cara mengakses data renewal count. Solusi: duplikasi query sederhana atau inject RenewalController/service — tapi paling simpel adalah inline query kecil langsung di Index().

**Primary recommendation:** Ikuti pola identik Phase 287-288. Satu-satunya komplikasi adalah dependency Index→BuildRenewalRowsAsync yang harus di-resolve.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
Tidak ada locked decisions eksplisit — semua keputusan di-delegate ke Claude's Discretion.

### Claude's Discretion
- DocumentAdminController scope: KKJ region (line 48-392) + CPDP region (line 394-638), auth `[Authorize(Roles = "Admin")]`
- TrainingAdminController scope: Training actions (line 641-1316), auth `[Authorize(Roles = "Admin, HC")]`
- RenewalController scope: Renewal region (line 1318-1812), auth `[Authorize(Roles = "Admin, HC")]`
- DI dependencies: analisis kode untuk menentukan yang dipakai per controller
- Route & View pattern: duplikasi `[Route("Admin")]` + view override sesuai Phase 286-288
- Cross-controller redirects: Training → AssessmentAdmin (sudah benar di kode saat ini)
- Sisa AdminController: Index, Maintenance saja (AuditLog/Impersonation tidak ada di kode)

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DOC-01 | DocumentAdminController berisi semua action KKJ dan CPDP | KKJ line 48-392, CPDP line 394-638 — 13 actions total, butuh ILogger |
| DOC-02 | Semua URL dokumen tetap sama via [Route] attribute | Route duplication pattern dari Phase 287-288 |
| TRN-01 | TrainingAdminController berisi semua action training | Training line 641-1316 — 7 actions + 1 private helper, butuh ILogger + ClosedXML |
| TRN-02 | Semua URL training tetap sama via [Route] attribute | Route duplication pattern dari Phase 287-288 |
| RNW-01 | RenewalController berisi semua action renewal + helper methods | Renewal line 1318-1812 — 4 actions + BuildRenewalRowsAsync, no extra DI |
| RNW-02 | Semua URL renewal tetap sama via [Route] attribute | Route duplication pattern dari Phase 287-288 |
</phase_requirements>

## Architecture Patterns

### Reference Implementation (dari Phase 288 — OrganizationController)
```csharp
[Route("Admin")]
[Route("Admin/[action]")]
public class OrganizationController : AdminBaseController
{
    public OrganizationController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        AuditLogService auditLog,
        IWebHostEnvironment env)
        : base(context, userManager, auditLog, env) { }

    // View resolution override — views tetap di Views/Admin/
    protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
    protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
    protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
    protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

    // Actions with per-action authorization...
}
```

### DI Dependencies per Controller

| Controller | Base DI (4) | Extra DI | Using Statements Tambahan |
|------------|-------------|----------|---------------------------|
| DocumentAdminController | Ya | `ILogger<DocumentAdminController>` | Tidak ada yang baru |
| TrainingAdminController | Ya | `ILogger<TrainingAdminController>` | `ClosedXML.Excel`, `System.Globalization`, `HcPortal.Helpers` |
| RenewalController | Ya | Tidak ada | Tidak ada yang baru |

### Action Inventory per Controller

**DocumentAdminController (13 actions):**
- KKJ: KkjMatrix, KkjUpload (GET), KkjUpload (POST), KkjFileDownload, KkjFileDelete, KkjFileHistory, KkjBagianAdd, DeleteBagian
- CPDP: CpdpFiles, CpdpUpload (GET), CpdpUpload (POST), CpdpFileDownload, CpdpFileArchive, CpdpFileHistory
- Authorization: `[Authorize(Roles = "Admin")]` on KKJ actions — **VERIFIKASI**: cek per-action auth di kode aktual, bisa jadi "Admin, HC"

**TrainingAdminController (7 actions + 1 helper):**
- AddTraining (GET+POST), EditTraining (GET+POST), DeleteTraining, DownloadImportTrainingTemplate, ImportTraining (GET+POST)
- Private: `SetTrainingCategoryViewBag()`
- Authorization: `[Authorize(Roles = "Admin, HC")]`
- Redirects: semua redirect ke `RedirectToAction("ManageAssessment", "AssessmentAdmin", ...)` — sudah benar

**RenewalController (4 actions + 1 private method):**
- RenewalCertificate, FilterRenewalCertificate, FilterRenewalCertificateGroup, CertificateHistory
- Private: `BuildRenewalRowsAsync()` — juga dipakai oleh AdminController.Index()
- Authorization: `[Authorize(Roles = "Admin, HC")]`

### Critical Dependency: Index() → BuildRenewalRowsAsync()

`AdminController.Index()` memanggil `BuildRenewalRowsAsync()` untuk `ViewBag.RenewalCount`. Setelah method pindah ke RenewalController:

**Solusi yang direkomendasikan:** Inline query sederhana di `AdminController.Index()`:
```csharp
// Simplified renewal count — replaces BuildRenewalRowsAsync dependency
var renewalCount = await _context.TrainingRecords
    .CountAsync(t => t.SertifikatUrl != null);
ViewBag.RenewalCount = renewalCount;
```

Atau lebih akurat: copy logic count dari BuildRenewalRowsAsync. Tapi karena Index hanya butuh `.Count`, query sederhana sudah cukup jika badge count tidak perlu exact match dengan filter renewal page.

**Alternatif lebih aman:** Duplikasi `BuildRenewalRowsAsync()` di AdminController (kecil, tapi duplikasi). Atau pindahkan ke service class — tapi itu out of scope (service extraction deferred).

**Rekomendasi:** Biarkan `BuildRenewalRowsAsync` tetap di AdminController sebagai private method JUGA (duplikasi), atau pindahkan ke `AdminBaseController` sebagai protected method agar bisa diakses keduanya.

**Rekomendasi final:** Pindahkan `BuildRenewalRowsAsync` ke `AdminBaseController` sebagai `protected` method. Ini paling clean — baik AdminController.Index() maupun RenewalController bisa memanggilnya tanpa duplikasi.

### Sisa AdminController Setelah Ekstraksi

```csharp
// ~60 baris: Index + Maintenance (GET+POST)
public class AdminController : AdminBaseController
{
    private readonly IMemoryCache _cache;
    // Constructor: base + IMemoryCache
    // Index() — uses BuildRenewalRowsAsync from base
    // Maintenance() GET + POST — uses _cache
}
```

## Common Pitfalls

### Pitfall 1: BuildRenewalRowsAsync Dependency
**What goes wrong:** AdminController.Index() breaks karena BuildRenewalRowsAsync pindah ke RenewalController
**How to avoid:** Pindahkan ke AdminBaseController sebagai protected, atau duplikasi

### Pitfall 2: KKJ Authorization Mismatch
**What goes wrong:** Asumsi KKJ auth = "Admin" padahal bisa "Admin, HC"
**How to avoid:** Verifikasi setiap action attribute dari kode aktual sebelum copy

### Pitfall 3: PartialView Resolution
**What goes wrong:** `PartialView("Shared/_RenewalGroupTablePartial", ...)` — PartialView resolution berbeda dari View
**How to avoid:** View override hanya untuk `View()`, PartialView menggunakan path absolut atau shared folder — test setiap partial

### Pitfall 4: Training Import Excel Dependencies
**What goes wrong:** Lupa include ClosedXML using statement di TrainingAdminController
**How to avoid:** Copy semua using statements yang relevan

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| View resolution | Custom routing | View override pattern (4 overloads) | Sudah terbukti 4x di prior phases |
| Shared renewal logic | Duplicate method | Protected method di base class | Avoid code duplication |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework) |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet build && dotnet run` (manual URL verification) |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DOC-01 | Document actions di DocumentAdminController | build | `dotnet build` | N/A |
| DOC-02 | URL /Admin/KkjMatrix dll accessible | manual | Browser test | N/A |
| TRN-01 | Training actions di TrainingAdminController | build | `dotnet build` | N/A |
| TRN-02 | URL /Admin/AddTraining dll accessible | manual | Browser test | N/A |
| RNW-01 | Renewal actions di RenewalController | build | `dotnet build` | N/A |
| RNW-02 | URL /Admin/RenewalCertificate dll accessible | manual | Browser test | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Per wave merge:** `dotnet build` + manual URL spot check
- **Phase gate:** Full suite green + browser verification semua URL

### Wave 0 Gaps
None — no automated test infrastructure exists; verification is build + manual browser.

## Project Constraints (from CLAUDE.md)

- Always respond in Bahasa Indonesia

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` — kode aktual, 1,877 baris, inspeksi langsung
- `Controllers/AdminBaseController.cs` — base class, 41 baris
- `Controllers/OrganizationController.cs` — reference implementation Phase 288
- `289-CONTEXT.md` — user decisions dan scope

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — pola identik sudah berhasil 4x
- Architecture: HIGH — reference implementations tersedia
- Pitfalls: HIGH — BuildRenewalRowsAsync dependency teridentifikasi dari kode

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (stable — pure refactoring)
