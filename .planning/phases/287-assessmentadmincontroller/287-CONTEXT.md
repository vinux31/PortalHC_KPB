# Phase 287: AssessmentAdminController - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Ekstraksi semua assessment-related actions dari AdminController ke AssessmentAdminController yang mewarisi AdminBaseController. Semua URL tetap identik via `[Route]` attributes. Zero fitur baru, zero perubahan UI.

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion: DI Dependencies
- AssessmentAdminController inject dependencies tambahan yang dibutuhkan assessment actions di luar base (4 shared dari base + tambahan per kebutuhan: IMemoryCache, INotificationService, IHubContext<AssessmentHub>, ILogger<AssessmentAdminController>, dll)
- Analisis kode untuk menentukan mana yang benar-benar dipakai oleh assessment actions

### Claude's Discretion: Scope Assessment Actions
- Semua action berikut dipindahkan ke AssessmentAdminController:
  - ManageAssessment, ManageCategories (+ CRUD categories)
  - CreateAssessment (GET + POST), EditAssessment (GET + POST)
  - DeleteAssessment, DeleteAssessmentGroup, ResetAssessment
  - AssessmentMonitoring, AssessmentMonitoringDetail
  - ExportAssessmentResults, UserAssessmentHistory
  - ReshufflePackage, ReshuffleAll
  - Package Management region (ManagePackages, CreatePackage, EditPackage, DeletePackage, dll)
  - Activity Log region (AssessmentActivityLog)
  - Helper Methods region (Shuffle, BuildCrossPackageAssignment)
- Tentukan dari kode mana action yang benar-benar assessment-related

### Claude's Discretion: Cross-Controller Redirects
- Training actions (masih di AdminController, akan pindah Phase 289) melakukan `RedirectToAction("ManageAssessment", new { tab = "training" })`
- Setelah ManageAssessment pindah ke AssessmentAdminController, redirect ini perlu update controller name
- Pendekatan: update redirect calls yang tersisa di AdminController agar mengarah ke controller baru, ATAU biarkan dulu dan fix di Phase 289/290

### Claude's Discretion: Authorization
- Pertahankan authorization attribute yang sama persis di setiap action seperti di AdminController saat ini
- Class-level `[Authorize]` sudah inherited dari AdminBaseController
- Per-action `[Authorize(Roles = "Admin")]` atau `[Authorize(Roles = "Admin, HC")]` harus identik

### Claude's Discretion: Route Attributes
- Sesuai keputusan D-07 Phase 286: ASP.NET Core attribute routes TIDAK inherited — AssessmentAdminController HARUS duplikasi `[Route("Admin")]` dan `[Route("Admin/[action]")]`
- Semua URL harus tetap sama persis: /Admin/ManageAssessment, /Admin/CreateAssessment, dll

### Claude's Discretion: View References
- Views tetap di folder `Views/Admin/` — tidak perlu pindah
- `return View()` calls tetap bekerja karena routing prefix sama

</decisions>

<specifics>
## Specific Ideas

Tidak ada — user menyerahkan semua keputusan teknis ke Claude's Discretion. Pure refactoring, yang penting:
- Zero regression — semua URL dan behavior identik
- Authorization tetap sama persis
- Build bersih tanpa error

</specifics>

<canonical_refs>
## Canonical References

### Prior decisions (Phase 286 CONTEXT.md)
- D-01: Base menyediakan 4 shared DI (ApplicationDbContext, UserManager, AuditLogService, IWebHostEnvironment)
- D-05: Shuffle() + BuildCrossPackageAssignment() ikut pindah ke AssessmentAdminController
- D-07: Route attributes harus diduplikasi di child class
- D-08: [Authorize] class-level inherited dari base

### Existing code
- `Controllers/AdminBaseController.cs` — Base class (sudah ada, Phase 286)
- `Controllers/AdminController.cs` — 8,146 baris, assessment actions tersebar di line 632-3731 dan 6556-7283
- Assessment actions: line 632 (ManageAssessment) sampai ~3575 (end of assessment actions)
- Helper Methods region: line 3575-3731 (Shuffle, BuildCrossPackageAssignment)
- Package Management region: line 6556-7114
- Activity Log region: line 7216-7283

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- AdminBaseController sudah ada dengan 4 shared DI
- AdminController sudah inherit AdminBaseController

### Established Patterns
- Route duplication pattern: AdminController sudah punya `[Route("Admin")]` dan `[Route("Admin/[action]")]` di class level
- Per-action authorization: `[Authorize(Roles = "Admin")]` pada sebagian besar assessment actions

### Integration Points
- Cross-controller redirect: Training actions (line 6157, 6193, 6226, 6276, 6281, 6292, 6332, 6359) redirect ke ManageAssessment — perlu update saat pindah
- Package Management (line 7086) redirect ke ManagePackages — akan ikut pindah bersama
- Views di `Views/Admin/` tetap di tempat

</code_context>

<deferred>
## Deferred Ideas

None — semua keputusan dalam scope phase

</deferred>

---

*Phase: 287-assessmentadmincontroller*
*Context gathered: 2026-04-02*
