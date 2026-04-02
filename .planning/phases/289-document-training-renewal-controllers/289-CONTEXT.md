# Phase 289: Document, Training & Renewal Controllers - Context

**Gathered:** 2026-04-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Ekstraksi 3 domain terakhir dari AdminController ke controller terpisah: DocumentAdminController, TrainingAdminController, RenewalController. Semua mewarisi AdminBaseController. Semua URL tetap identik via `[Route]` attributes. Zero fitur baru, zero perubahan UI.

Setelah phase ini, AdminController hanya tersisa sebagai hub (~240 baris): Index, AuditLog, Maintenance, Impersonation.

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion: DocumentAdminController Scope
- KKJ region (line 48-392): KkjMatrix, KkjUpload (GET+POST), KkjFileDownload, KkjFileDelete, KkjFileHistory, KkjBagianAdd, DeleteBagian
- CPDP region (line 394-638): CpdpFiles, CpdpUpload (GET+POST), CpdpFileDownload, CpdpFileArchive, CpdpFileHistory
- Authorization: `[Authorize(Roles = "Admin")]` — verifikasi dari kode aktual
- Semua private/helper methods dalam kedua region ikut pindah

### Claude's Discretion: TrainingAdminController Scope
- Training actions (line 641-1316): AddTraining (GET+POST), EditTraining (GET+POST), DeleteTraining, DownloadImportTrainingTemplate, ImportTraining (GET+POST)
- Authorization: `[Authorize(Roles = "Admin, HC")]` — verifikasi dari kode aktual
- Cross-controller redirect: `RedirectToAction("ManageAssessment", ...)` harus update ke `RedirectToAction("ManageAssessment", "AssessmentAdmin")` atau equivalent

### Claude's Discretion: RenewalController Scope
- Renewal region (line 1318-1812): RenewalCertificate, FilterRenewalCertificate, FilterRenewalCertificateGroup, CertificateHistory, plus semua helper/private methods dalam region
- Authorization: `[Authorize(Roles = "Admin, HC")]` — verifikasi dari kode aktual

### Claude's Discretion: DI Dependencies
- Setiap controller inject dependencies tambahan di luar base sesuai kebutuhan action-nya
- Analisis kode untuk menentukan mana yang benar-benar dipakai

### Claude's Discretion: Route & View Pattern
- Sesuai Phase 286 D-07: duplikasi `[Route("Admin")]` dan `[Route("Admin/[action]")]` di setiap controller baru
- Views tetap di `Views/Admin/` — tidak perlu pindah
- View resolution mengikuti pola Phase 287 (override jika perlu)

### Claude's Discretion: Cross-Controller Redirects
- Training → AssessmentAdmin: update controller name pada RedirectToAction calls
- Document dan Renewal: semua redirect dalam domain masing-masing (tidak ada cross-domain)

### Claude's Discretion: Authorization
- Class-level `[Authorize]` inherited dari AdminBaseController
- Per-action roles harus identik dengan saat ini

### Claude's Discretion: Sisa AdminController
- Setelah ekstraksi, AdminController hanya berisi: Index, Maintenance, Impersonation, AuditLog
- Verifikasi tidak ada action orphan yang terlewat

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

### Prior decisions (Phase 286-288)
- D-01 (286): Base menyediakan 4 shared DI (ApplicationDbContext, UserManager, AuditLogService, IWebHostEnvironment)
- D-07 (286): Route attributes harus diduplikasi di child class
- D-08 (286): [Authorize] class-level inherited dari base
- Phase 287: View resolution pattern — Views tetap di Views/Admin/, override FindView jika perlu
- Phase 288: 3 controller berhasil diekstrak dengan pola identik

### Existing code
- `Controllers/AdminBaseController.cs` — Base class (Phase 286)
- `Controllers/AssessmentAdminController.cs` — Reference implementation (Phase 287)
- `Controllers/WorkerController.cs` — Reference implementation (Phase 288)
- `Controllers/CoachMappingController.cs` — Reference implementation (Phase 288)
- `Controllers/OrganizationController.cs` — Reference implementation (Phase 288)
- `Controllers/AdminController.cs` — 1,877 baris saat ini
  - KKJ: line 48-392
  - CPDP: line 394-638
  - Training: line 641-1316
  - Renewal: line 1318-1812
  - Maintenance: line 1812-1877
  - Hub (Index, AuditLog, Impersonation): awal file

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- AdminBaseController sudah ada dengan 4 shared DI
- 4 controller sudah berhasil diekstrak (Phase 287-288) sebagai reference implementation
- Pattern route duplication + view override sudah terbukti 4 kali

### Established Patterns
- Phase 287-288 pattern: copy actions → add route attributes → override view resolution → verify build
- Semua prior extractions berhasil tanpa regression

### Integration Points
- Training actions redirect ke ManageAssessment (sudah di AssessmentAdminController) — perlu update controller name
- Views di `Views/Admin/` tetap di tempat
- Tidak ada cross-domain redirect lain yang bermasalah

</code_context>

<deferred>
## Deferred Ideas

None — semua keputusan dalam scope phase

</deferred>

---

*Phase: 289-document-training-renewal-controllers*
*Context gathered: 2026-04-02*
