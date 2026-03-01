---
phase: 77-training-record-redirect-fix
plan: 02
subsystem: Views
tags: [training, assessment, tabs, forms, views, refactor]
requirements: [REDIR-01]

dependency_graph:
  requires:
    - 77-01
  provides:
    - Views/Admin/ManageAssessment.cshtml as 3-tab unified page
    - Views/Admin/AddTraining.cshtml standalone create form
    - Views/Admin/EditTraining.cshtml standalone edit form
  affects:
    - Views/Admin/ManageAssessment.cshtml
    - Views/Admin/AddTraining.cshtml
    - Views/Admin/EditTraining.cshtml
    - Views/CMP/RecordsWorkerList.cshtml (deleted)

tech_stack:
  added: []
  patterns:
    - Bootstrap nav-tabs with 3 panes (Assessment Groups, Training Records, History)
    - Tab activation via ViewBag.ActiveTab driven by ?tab= query param
    - Inline expand/collapse rows using Bootstrap collapse for per-worker training records
    - Filter dropdowns with server-side selected state (Razor foreach/if blocks)
    - History sub-tabs (Riwayat Assessment + Riwayat Training) with client-side filter

key_files:
  created:
    - Views/Admin/AddTraining.cshtml
    - Views/Admin/EditTraining.cshtml
  modified:
    - Views/Admin/ManageAssessment.cshtml
  deleted:
    - Views/CMP/RecordsWorkerList.cshtml

key_decisions:
  - Used Razor foreach/if blocks for dropdown selected state — RZ1031 prevents C# ternary expressions in tag helper option attributes; pre-computed variables (selectedSection, selectedCategory, etc.) feed the if/else blocks
  - Kept header action buttons (Buat Assessment + Audit Log) in server-rendered conditional block — activeTab from ViewBag sets initial state; JS listener on shown.bs.tab toggles visibility dynamically
  - ManageAssessment expanded rows show only TrainingRecords (manual records) — comment documents omission of Assessment records (would require separate query); plan spec acknowledges this

metrics:
  duration_minutes: 4
  completed_date: "2026-03-01"
  tasks_completed: 2
  files_modified: 4
---

# Phase 77 Plan 02: Training Record View Layer Summary

**One-liner:** ManageAssessment.cshtml rewritten as 3-tab unified page (Assessment Groups + Training Records + History); standalone AddTraining/EditTraining forms created in Views/Admin/; RecordsWorkerList.cshtml deleted.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Rewrite ManageAssessment.cshtml as 3-tab page | 70bdffa | Views/Admin/ManageAssessment.cshtml |
| 2 | Create AddTraining.cshtml + EditTraining.cshtml + delete RecordsWorkerList.cshtml | 0565d68 | Views/Admin/AddTraining.cshtml, Views/Admin/EditTraining.cshtml, Views/CMP/RecordsWorkerList.cshtml (deleted) |

## What Was Built

### Task 1 — ManageAssessment.cshtml

**Complete rewrite — 3-tab page structure:**

- **Tab 1: Assessment Groups** — moved existing content (search form, stats badge, assessment table with collapse peserta, pagination, regenerate token actions). Search form adds `<input type="hidden" name="tab" value="assessment">` to preserve tab on search submit.

- **Tab 2: Training Records** — header with "Tambah Training" button linking to `/Admin/AddTraining`; filter form (GET to ManageAssessment with tab=training, isFiltered=true) with Bagian/Kategori/Unit/Status/Cari dropdowns; three display states: initial (prompt to filter), empty (no results), worker table. Worker rows expand via Bootstrap collapse to show manual TrainingRecords inline with Edit/Delete buttons and "Lihat Detail" link.

- **Tab 3: History** — sub-tabs for Riwayat Assessment (with client-side worker/title filter) and Riwayat Training. Uses assessmentHistory/trainingHistory from ViewBag (loaded by controller for tab=history).

**JavaScript on DOMContentLoaded:**
- Activates correct tab from `ViewBag.ActiveTab` ("assessment" | "training" | "history")
- Toggles header Buat Assessment + Audit Log buttons based on active tab
- Regenerate Token fetch handler (preserved from original)
- filterAssessmentRows() client-side filter for Riwayat Assessment sub-tab

**Bug fix during implementation:** RZ1031 error prevented inline C# ternary in option element attributes. Pre-computed variables (selectedSection, selectedCategory, selectedUnit, selectedStatus) and Razor if/else blocks for each option were used instead.

### Task 2 — AddTraining.cshtml + EditTraining.cshtml + RecordsWorkerList deletion

**Views/Admin/AddTraining.cshtml:**
- Model: CreateTrainingRecordViewModel
- Breadcrumb: Admin > Kelola Data > Manage Assessment & Training > Tambah Training
- Worker dropdown: `asp-for="UserId" asp-items="ViewBag.Workers"` (SelectListItem)
- Form fields: Judul, Penyelenggara, Kota, Kategori, Tanggal, TanggalMulai, TanggalSelesai, Status, NomorSertifikat, CertificateType, ValidUntil, CertificateFile
- Posts to: POST /Admin/AddTraining
- Cancel links to ManageAssessment?tab=training

**Views/Admin/EditTraining.cshtml:**
- Model: EditTrainingRecordViewModel
- Breadcrumb: Admin > Kelola Data > Manage Assessment & Training > Edit Training
- Worker name displayed as read-only paragraph (not editable)
- Hidden inputs: Id, WorkerId, WorkerName, ExistingSertifikatUrl
- All editable training + certificate fields
- Existing certificate download link shown when ExistingSertifikatUrl present
- Posts to: POST /Admin/EditTraining
- Cancel links to ManageAssessment?tab=training

**Views/CMP/RecordsWorkerList.cshtml:** Deleted — functionality merged into ManageAssessment tabs 2 and 3.

## Verification Results

1. `dotnet build` — 0 errors, 60 warnings (all pre-existing CA1416 warnings)
2. `Views/Admin/AddTraining.cshtml` — FOUND
3. `Views/Admin/EditTraining.cshtml` — FOUND
4. `Views/CMP/RecordsWorkerList.cshtml` — does not exist (confirmed deleted)
5. ManageAssessment.cshtml has 3 nav-tabs (tab-assessment, tab-training, tab-history)
6. Training Records tab has filter form + Tambah Training button + worker table + expand rows
7. History tab has riwayat-assessment-pane + riwayat-training-pane sub-tabs
8. Tab activation script uses ViewBag.ActiveTab value

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed RZ1031: C# ternary not allowed in tag helper option attribute**
- **Found during:** Task 1, writing filter dropdown selected state
- **Issue:** RZ1031 error "The tag helper 'option' must not have C# in the element's attribute declaration area" — using `@(condition ? "selected" : "")` in `<option value="..." ...>` is rejected by Razor tag helper compiler
- **Fix:** Pre-computed C# variables (selectedSection, selectedCategory, selectedUnit, selectedStatus, searchTerm2) in @{} block; dropdown options rendered with Razor `@if (selectedVal == val) { <option selected="selected"> }` pattern
- **Files modified:** Views/Admin/ManageAssessment.cshtml
- **Commit:** 70bdffa (same task commit)

## Self-Check: PASSED

- Views/Admin/ManageAssessment.cshtml: FOUND
- Views/Admin/AddTraining.cshtml: FOUND
- Views/Admin/EditTraining.cshtml: FOUND
- Views/CMP/RecordsWorkerList.cshtml: confirmed MISSING (deleted as required)
- Commit 70bdffa (Task 1): FOUND
- Commit 0565d68 (Task 2): FOUND
