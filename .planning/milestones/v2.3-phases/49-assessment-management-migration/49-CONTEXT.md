# Phase 49: Assessment Management Migration - Context

**Gathered:** 2026-02-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Move the Manage Assessments functionality from CMP (`/CMP/Assessment?view=manage`) to Kelola Data (`/Admin/ManageAssessment`). All manage-related actions (Create, Edit, Delete, Reset, Force Close, Export, Monitoring Detail, User Assessment History) are migrated from CMPController to AdminController. AuditLog page also moves from CMP to Admin as a global audit log. CMP/Assessment is cleaned up to be a pure personal view for workers.

**Rename:** "Assessment Competency Map Manager" -> "Assessment Management Migration to Kelola Data"
**Assessment Competency Map page is NOT needed** - removed from roadmap.

</domain>

<decisions>
## Implementation Decisions

### Migration Scope
- All manage-related actions pindah dari CMPController ke AdminController: Assessment (manage view), CreateAssessment, EditAssessment, DeleteAssessment, DeleteAssessmentGroup, ResetAssessment, ForceCloseAssessment, ExportAssessmentResults, AssessmentMonitoringDetail, UserAssessmentHistory, RegenerateToken
- Semua fitur pindah apa adanya (1:1 migration) — UI dan fungsi tetap sama
- POST actions (Reset, ForceClose, Delete, dll) juga pindah ke AdminController dan dihapus dari CMPController
- Delete guard, validation rules, required fields — semua tetap sama

### Layout & Style
- Page baru di `/Admin/ManageAssessment` adaptasi ke style Kelola Data (breadcrumb Admin style, card shadow-sm, container-fluid) — konten dan fitur tetap sama
- Sub-pages (Create, Edit, MonitoringDetail, UserHistory) juga di AdminController: `/Admin/CreateAssessment`, `/Admin/EditAssessment`, dll
- Breadcrumb: Admin > Kelola Data > Manage Assessments. Sub-pages: Admin > Kelola Data > Manage Assessments > Create/Edit/dll

### Filtering & Navigation
- Search bar dan pagination dipindah apa adanya dari CMP — filter by title/category tetap sama
- Di Admin/Index (Kelola Data): card 'Assessment Competency Map' (dengan badge Segera) diganti jadi 'Manage Assessments' dengan link ke `/Admin/ManageAssessment`

### CMP Cleanup
- CMP/Assessment page tetap ada — menjadi pure personal view untuk worker (My Assessments)
- Hapus toggle personal/manage dari CMP/Assessment — hapus tombol 'Manage Assessments', hapus canManage logic, hapus semua manage-related UI elements
- HC/Admin yang akses CMP/Assessment hanya lihat assessment mereka sendiri (kalau ada)
- Card 'Manage Assessments' dihapus dari CMP Index
- Card 'Assessment Lobby' di CMP Index di-rename jadi 'My Assessments'

### AuditLog Migration
- AuditLog page pindah dari CMPController ke AdminController (`/Admin/AuditLog`)
- Menampilkan semua audit log (global) — sama seperti sekarang
- Disesuaikan style-nya ke Kelola Data
- CMP/AuditLog dihapus dari CMPController
- Tombol Audit Log tersedia di header Admin/ManageAssessment

### Claude's Discretion
- Exact layout adaptation details (spacing, card styling) for Kelola Data consistency
- How to handle edge cases during migration (e.g., old bookmarks to CMP manage URLs)
- View file organization (reuse vs new view files)

</decisions>

<specifics>
## Specific Ideas

- Pattern konsisten dengan KKJ Matrix (Phase 47) dan KKJ-IDP Mapping Editor (Phase 48) untuk style Kelola Data
- CMP/Assessment setelah cleanup harus jadi page yang simple — pure personal view tanpa manage elements

</specifics>

<deferred>
## Deferred Ideas

- Assessment Competency Map page (mapping assessment categories to KKJ items) — dihapus dari roadmap, tidak diperlukan
- Redesign personal view CMP/Assessment — bisa jadi phase terpisah kalau perlu

</deferred>

---

*Phase: 49-assessment-management-migration*
*Context gathered: 2026-02-26*
