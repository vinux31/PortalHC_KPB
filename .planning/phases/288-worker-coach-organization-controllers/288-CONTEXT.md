# Phase 288: Worker, Coach & Organization Controllers - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Ekstraksi 3 domain dari AdminController ke controller terpisah: WorkerController, CoachMappingController, OrganizationController. Semua mewarisi AdminBaseController. Semua URL tetap identik via `[Route]` attributes. Zero fitur baru, zero perubahan UI.

</domain>

<decisions>
## Implementation Decisions

### D-01: Proton Progress Helpers → CoachMappingController
- `AutoCreateProgressForAssignment()` dan `CleanupProgressForAssignment()` (line 3460-3558) ikut pindah ke CoachMappingController
- Alasan: private methods yang hanya dipanggil oleh coach mapping actions, domain-nya coaching
- **Status: Decided**

### Claude's Discretion: WorkerController Scope
- Semua action worker (line 1682-2610): ManageWorkers, ExportWorkers, CreateWorker (GET+POST), EditWorker (GET+POST), DeleteWorker, DeactivateWorker, ReactivateWorker, WorkerDetail, ImportWorkers (GET+POST), DownloadImportTemplate
- Authorization: `[Authorize(Roles = "Admin, HC")]` pada semua action

### Claude's Discretion: CoachMappingController Scope
- Semua action coach-coachee (line 621-1682): CoachCoacheeMapping, DownloadMappingImportTemplate, ImportCoachCoacheeMapping, CoachCoacheeMappingAssign, CoachCoacheeMappingEdit, CleanupCoachCoacheeMappingOrg, CoachCoacheeMappingGetSessionCount, CoachCoacheeMappingActiveAssignmentCount, CoachCoacheeMappingDeactivate, CoachCoacheeMappingReactivate, MarkMappingCompleted, CoachCoacheeMappingDeletePreview, CoachCoacheeMappingDelete, CoachCoacheeMappingExport, GetEligibleCoachees
- Plus: `#region Proton Progress Helpers` (line 3460-3558) — per D-01
- Authorization: `[Authorize(Roles = "Admin, HC")]` pada semua action

### Claude's Discretion: OrganizationController Scope
- Semua action organization (line 4057-4389): ManageOrganization, AddOrganizationUnit, EditOrganizationUnit, ToggleOrganizationUnitActive, DeleteOrganizationUnit, ReorderOrganizationUnit
- Authorization: `[Authorize(Roles = "Admin, HC")]` pada semua action

### Claude's Discretion: DI Dependencies
- Setiap controller inject dependencies tambahan di luar base sesuai kebutuhan action-nya
- Analisis kode untuk menentukan mana yang benar-benar dipakai

### Claude's Discretion: Route & View Pattern
- Sesuai Phase 286 D-07: duplikasi `[Route("Admin")]` dan `[Route("Admin/[action]")]` di setiap controller baru
- Views tetap di `Views/Admin/` — tidak perlu pindah
- View resolution mengikuti pola Phase 287 (override jika perlu)

### Claude's Discretion: Cross-Controller Redirects
- Tidak ada cross-domain redirect yang bermasalah — semua redirect dalam domain masing-masing
- Worker actions redirect ke ManageWorkers, Coach ke CoachCoacheeMapping, Org ke ManageOrganization
- Training actions (tetap di AdminController) sudah mengarah ke `"AssessmentAdmin"` controller

### Claude's Discretion: Authorization
- Class-level `[Authorize]` inherited dari AdminBaseController
- Per-action `[Authorize(Roles = "Admin, HC")]` harus identik dengan saat ini

</decisions>

<specifics>
## Specific Ideas

Tidak ada — user menyerahkan semua keputusan teknis ke Claude's Discretion kecuali D-01. Pure refactoring, yang penting:
- Zero regression — semua URL dan behavior identik
- Authorization tetap sama persis
- Build bersih tanpa error

</specifics>

<canonical_refs>
## Canonical References

### Prior decisions (Phase 286-287)
- D-01 (286): Base menyediakan 4 shared DI (ApplicationDbContext, UserManager, AuditLogService, IWebHostEnvironment)
- D-07 (286): Route attributes harus diduplikasi di child class
- D-08 (286): [Authorize] class-level inherited dari base
- Phase 287: View resolution pattern — Views tetap di Views/Admin/, override FindView jika perlu

### Existing code
- `Controllers/AdminBaseController.cs` — Base class (Phase 286)
- `Controllers/AssessmentAdminController.cs` — Contoh ekstraksi berhasil (Phase 287)
- `Controllers/AdminController.cs` — 4,413 baris saat ini
  - Coach mapping: line 621-1682 + Proton helpers line 3460-3558
  - Worker management: line 1682-2610
  - Organization management: line 4057-4389

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- AdminBaseController sudah ada dengan 4 shared DI
- AssessmentAdminController sebagai reference implementation (Phase 287)
- Pattern route duplication + view override sudah terbukti

### Established Patterns
- Phase 287 pattern: copy actions → add route attributes → override view resolution → verify build
- Authorization: semua action di 3 domain ini konsisten `[Authorize(Roles = "Admin, HC")]`

### Integration Points
- Tidak ada cross-domain redirect yang perlu diupdate
- Views di `Views/Admin/` tetap di tempat

</code_context>

<deferred>
## Deferred Ideas

None — semua keputusan dalam scope phase

</deferred>

---

*Phase: 288-worker-coach-organization-controllers*
*Context gathered: 2026-04-02*
