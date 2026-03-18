# Phase 198: CRUD Consolidation - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Training Record create/edit/delete has exactly one entry point (Admin), Training Import is accessible from Admin context, and Worker Detail views in Admin vs CMP serve clearly distinct purposes. This is a refactoring phase — no new features, no UI/UX changes beyond route consolidation.

</domain>

<decisions>
## Implementation Decisions

### Penghapusan CMP CRUD
- Hapus langsung `EditTrainingRecord` dan `DeleteTrainingRecord` dari CMPController — endpoints sudah orphaned (tidak ada view/link yang mengarah ke sana)
- Hapus juga `EditTrainingRecordViewModel` jika hanya dipakai oleh CMP edit action yang dihapus (cek usage dulu)
- Tidak perlu redirect karena tidak ada user flow yang terdampak

### Training Import Relocation
- Pindahkan `ImportTraining` dan `DownloadImportTrainingTemplate` dari CMPController ke AdminController
- View ditulis ulang mengikuti pattern `ImportWorkers.cshtml` di Admin (download template + upload + process) — bukan copy as-is dari CMP
- Placement: tombol Import di `ManageAssessment?tab=training` (di samping tombol Add Training yang sudah ada)
- Hapus tombol import dari CMP/RecordsTeam — import hanya dari Admin
- Hapus action dari CMP tanpa redirect (tidak perlu handle old URL)
- Authorization: tetap `[Authorize(Roles = "Admin, HC")]`

### Diferensiasi Worker Detail
- Tidak perlu perubahan tambahan — kedua view sudah distinct:
  - Admin/WorkerDetail = profil pekerja (nama, NIP, posisi, unit, role, email)
  - CMP/RecordsWorkerDetail = history training & assessment (read-only)
- CMP/RecordsWorkerDetail tetap read-only — tidak ada tombol edit/delete training di CMP

### Claude's Discretion
- Detail implementasi view ImportTraining baru (selama mengikuti pattern ImportWorkers.cshtml)
- Cleanup ViewModel/model yang jadi orphan setelah penghapusan

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Admin Training CRUD (reference implementation)
- `Controllers/AdminController.cs` — AddTraining, EditTraining, DeleteTraining actions (line ~5221-5431)
- `Views/Admin/AddTraining.cshtml` — Training form structure
- `Views/Admin/EditTraining.cshtml` — Edit form with file upload
- `Views/Admin/ManageAssessment.cshtml` — Tab=training layout where import button will be placed

### Admin Import Pattern (target pattern for rewrite)
- `Controllers/AdminController.cs` — ImportWorkers + DownloadImportTemplate actions
- `Views/Admin/ImportWorkers.cshtml` — Import view pattern to follow

### CMP Code to Remove/Move
- `Controllers/CMPController.cs` — EditTrainingRecord (~line 845), DeleteTrainingRecord (~line 930), ImportTraining (~line 716), DownloadImportTrainingTemplate (~line 673)
- `Views/CMP/ImportTraining.cshtml` — Current import view (will be rewritten, not copied)
- `Views/CMP/RecordsTeam.cshtml` — Import button to remove (~line 118-127)

### Worker Detail Views (no changes needed, reference only)
- `Views/Admin/WorkerDetail.cshtml` — Profile view
- `Views/CMP/RecordsWorkerDetail.cshtml` — Records history view

### Models
- `Models/EditTrainingRecordViewModel.cs` — Candidate for deletion if orphaned
- `Models/ImportTrainingResult.cs` — Used by import logic, moves with ImportTraining

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ImportWorkers.cshtml` pattern: download template button + file upload + process + redirect — reuse for ImportTraining
- `ExcelExportHelper` (Phase 197): available for template generation if needed
- `IWorkerDataService` (Phase 196): shared service already in use by both controllers

### Established Patterns
- Admin CRUD: GET form + POST action + redirect to list with tab parameter
- Admin Import: download template + upload Excel + validate + bulk create + redirect
- Authorization: `[Authorize(Roles = "Admin, HC")]` for all ManageWorkers/Training actions

### Integration Points
- `ManageAssessment.cshtml` tab=training section: add Import button here
- `CMP/RecordsTeam.cshtml`: remove import button references
- CMPController: remove 4 actions (EditTrainingRecord, DeleteTrainingRecord, ImportTraining, DownloadImportTrainingTemplate)

</code_context>

<specifics>
## Specific Ideas

- View ImportTraining baru harus mengikuti pattern ImportWorkers.cshtml — bukan copy dari CMP
- Tombol import ditempatkan di tab training ManageAssessment, bukan di Admin hub

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 198-crud-consolidation*
*Context gathered: 2026-03-18*
