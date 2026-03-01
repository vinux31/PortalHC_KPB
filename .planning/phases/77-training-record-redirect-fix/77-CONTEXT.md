# Phase 77: Merge Training Records into Manage Assessment & Training - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

> **Scope Change:** Original Phase 77 was "Training Record Redirect Fix" (fix dead-end redirect after CRUD). User decided to expand scope: merge CMP/RecordsWorkerList functionality into Manage Assessment page, making it the single admin location for both assessment groups and training records. Roadmap should be updated before planning.

<domain>
## Phase Boundary

Merge the admin/HC worker list and training record CRUD from `CMP/Records` (RecordsWorkerList view) into `Admin/ManageAssessment`, creating a unified "Manage Assessment & Training" page. CMP/Records becomes a personal-only view for all roles. The redirect fix is resolved as a side effect — training CRUD redirects to the new combined page.

**Not in scope:** Creating new CRUD capabilities, changing assessment creation/monitoring workflows, or modifying the personal CMP/Records view beyond removing the role-based routing.

</domain>

<decisions>
## Implementation Decisions

### Page Structure
- **3 tabs** in order: Assessment Groups | Training Records | History
- Assessment Groups tab: unchanged from current ManageAssessment
- Training Records tab: worker list (from RecordsWorkerList) with expand-row for individual records
- History tab: Riwayat Assessment + Riwayat Training sub-tabs (from RecordsWorkerList history tab)
- Tab state preserved via URL query parameter: `?tab=training`, `?tab=history`

### Page Naming & Navigation
- Page renamed to **"Manage Assessment & Training"**
- Breadcrumb updated: Admin > Kelola Data > Manage Assessment & Training
- Kelola Data Hub card (Admin/Index Section C) updated: new name + description
- Hub card visible to **Admin + HC** (was Admin-only)
- URL stays `/Admin/ManageAssessment` (no breaking change)

### Role & Access
- **Admin + HC: full access to all tabs and all actions** (assessment CRUD, training CRUD, monitoring, export, audit log)
- All assessment actions (Create, Edit, Delete, Monitor, Export, Regenerate Token) updated from `[Authorize(Roles = "Admin")]` to `[Authorize(Roles = "Admin, HC")]`
- Audit Log also accessible to HC

### Tab Training Records — Worker List
- Columns: Nama (avatar+name), Nopeg, Jabatan, Unit, Status Training, Action (expand)
- Same columns as current RecordsWorkerList
- Default state: "Pilih filter untuk menampilkan data" — requires filter before showing workers
- Filter selection: Claude's discretion (evaluate which filters are useful)
- Pagination: server-side, 20 per page (consistent with Assessment Groups tab)
- Export: upgrade to XLSX using ClosedXML (replace client-side CSV)

### Tab Training Records — Expanded Row (per worker)
- Click worker row to expand inline (not navigate away)
- Shows unified training records (assessment + training manual) with all columns from Records.cshtml:
  - Tanggal, Tipe (badge: Assessment Online=blue, Training Manual=green), Judul, Score, Pass/Fail, Penyelenggara, Tipe Sertifikat, Berlaku Sampai, Status
- **Edit/Delete buttons only on Training Manual records** — Assessment records are read-only in this view
- Click worker row still navigable to Admin/WorkerDetail via action button

### CRUD Operations
- "Tambah Training" button in tab Training Records header → navigates to separate page `/Admin/AddTraining`
- Edit → separate page `/Admin/EditTraining`
- Delete → confirm dialog (`confirm()`) → redirect back to Manage Assessment & Training with `?tab=training`
- All CRUD redirects to `/Admin/ManageAssessment?tab=training` on success
- Action URL names shortened: AddTraining, EditTraining, DeleteTraining

### Success/Error Feedback
- Success: TempData flash message (green banner) — "Training record berhasil disimpan/diupdate/dihapus"
- Validation errors: stay on form page with error messages (don't lose user input)
- Pattern: follow existing CreateAssessment success/error patterns

### Audit Log Extension
- Training record create/edit/delete actions added to audit log
- Consistent with existing assessment action logging

### Controller & View Migration
- Training CRUD actions (Create, Edit, Delete) move from CMPController to AdminController
- View files move from Views/CMP/ to Views/Admin/ (AddTraining.cshtml, EditTraining.cshtml)
- `GetUnifiedRecords()` helper duplicated to AdminController (CMP still needs it for personal view)

### Old Page Cleanup
- **RecordsWorkerList.cshtml: deleted** — functionality moved to Manage Assessment & Training
- **CMP/Records: simplified** — remove role-based view routing; ALL roles (including Admin, HC) see personal view (Records.cshtml)
- Scan codebase for all references to RecordsWorkerList to ensure no dead links (researcher task)

### Claude's Discretion
- Filter selection for worker list (evaluate which filters from RecordsWorkerList are useful to keep)
- Exact spacing, typography, and visual consistency between tabs
- How to handle expanded row loading (lazy vs eager)
- Error state handling for edge cases

</decisions>

<specifics>
## Specific Ideas

- CMP/Records personal view stays for all roles — HC/Admin see their own records there, manage others' data in Kelola Data Hub
- "duplicate di CMP tetap ada khusus untuk melihat history personal, untuk di Kelola Data Hub > Manage Assessment dan Training khusus HC dan admin fungsinya untuk pendataan"
- Badge differentiation: Assessment Online = blue, Training Manual = green — keep in expanded view

</specifics>

<deferred>
## Deferred Ideas

- Full CMP/Records page migration/deletion — currently only the admin view (RecordsWorkerList) moves; personal view stays in CMP
- Navbar "Kelola Data" visibility for HC users — separate concern (Phase 78 handles this)

</deferred>

---

*Phase: 77-training-record-redirect-fix*
*Context gathered: 2026-03-01*
