---
phase: 291-fix-view-urlaction-references
plan: "03"
subsystem: views/admin
tags: [bug-fix, url-routing, assessment, training]
dependency_graph:
  requires: []
  provides: [valid-assessment-category-urls, valid-training-urls]
  affects: [ManageCategories, UserAssessmentHistory, TrainingRecordsTab, AddTraining, EditTraining, ImportTraining]
tech_stack:
  added: []
  patterns: [Url.Action with correct controller names]
key_files:
  created: []
  modified:
    - Views/Admin/ManageCategories.cshtml
    - Views/Admin/UserAssessmentHistory.cshtml
    - Views/Admin/Shared/_TrainingRecordsTab.cshtml
    - Views/Admin/AddTraining.cshtml
    - Views/Admin/EditTraining.cshtml
    - Views/Admin/ImportTraining.cshtml
decisions:
  - WorkerDetail link di _TrainingRecordsTab diubah dari "Admin" ke "Worker" controller (sesuai lokasi action yang sebenarnya di WorkerController)
metrics:
  duration: 8m
  completed_date: "2026-04-02"
  tasks_completed: 2
  files_modified: 6
---

# Phase 291 Plan 03: Fix Assessment & Training Views Url.Action References Summary

**One-liner:** Fixed 17+ broken Url.Action references across 6 view files — ManageCategories CRUD forms, UserAssessmentHistory breadcrumb, and all Training CRUD/import links now point to correct AssessmentAdmin/TrainingAdmin/Worker controllers.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Fix Assessment views Url.Action references | 65ba402f | ManageCategories.cshtml, UserAssessmentHistory.cshtml |
| 2 | Fix Training views Url.Action references | ec9a0227 | _TrainingRecordsTab.cshtml, AddTraining.cshtml, EditTraining.cshtml, ImportTraining.cshtml |

## Changes Made

### Task 1: Assessment Views

**ManageCategories.cshtml** (8 fixes):
- `ExportCategoriesExcel` → AssessmentAdmin
- `ExportCategoriesPdf` → AssessmentAdmin
- `AddCategory` form action → AssessmentAdmin
- `EditCategory` form action → AssessmentAdmin
- `ManageCategories` cancel link → AssessmentAdmin
- `EditCategory` link (parent row) → AssessmentAdmin
- `ToggleCategoryActive` form action (3 occurrences: parent, child, grandchild) → AssessmentAdmin
- `EditCategory` link (child + grandchild rows) → AssessmentAdmin
- `DeleteCategory` in modal JS → AssessmentAdmin

**UserAssessmentHistory.cshtml** (2 fixes):
- `ManageAssessment` breadcrumb link → AssessmentAdmin
- `ManageAssessment` back button → AssessmentAdmin

### Task 2: Training Views

**_TrainingRecordsTab.cshtml** (7 fixes):
- `ImportTraining` header button → TrainingAdmin
- `AddTraining` header button → TrainingAdmin
- filter form action `ManageAssessment` → AssessmentAdmin
- reset filter link `ManageAssessment` → AssessmentAdmin
- `AddTraining` inline "Tambah" link (empty state) → TrainingAdmin
- `EditTraining` record action → TrainingAdmin
- `DeleteTraining` form action → TrainingAdmin
- `WorkerDetail` link → Worker (sesuai lokasi yang benar di WorkerController)

**AddTraining.cshtml** (3 fixes):
- form `asp-controller` → TrainingAdmin
- `ManageAssessment` breadcrumb → AssessmentAdmin
- `ManageAssessment` cancel button → AssessmentAdmin

**EditTraining.cshtml** (3 fixes):
- form `asp-controller` → TrainingAdmin
- `ManageAssessment` breadcrumb → AssessmentAdmin
- `ManageAssessment` cancel button → AssessmentAdmin

**ImportTraining.cshtml** (4 fixes):
- `ManageAssessment` breadcrumb → AssessmentAdmin
- `ManageAssessment` back button → AssessmentAdmin
- `DownloadImportTrainingTemplate` → TrainingAdmin
- form `asp-controller` → TrainingAdmin
- `ManageAssessment` cancel button → AssessmentAdmin

## Verification

- Zero non-Index Url.Action("X", "Admin") di 9 files: PASS
- Build: 0 errors, 70 warnings (pre-existing): PASS

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed WorkerDetail controller reference**
- **Found during:** Task 2
- **Issue:** `WorkerDetail` link di `_TrainingRecordsTab` menggunakan "Admin" controller, padahal action tersebut ada di `WorkerController` (bukan di AdminController maupun refactored controllers)
- **Fix:** Ubah ke "Worker" controller
- **Files modified:** Views/Admin/Shared/_TrainingRecordsTab.cshtml
- **Commit:** ec9a0227

## Known Stubs

None — semua links dan form actions sudah terhubung ke controller yang benar.

## Self-Check: PASSED

- ManageCategories.cshtml: FOUND (modified)
- UserAssessmentHistory.cshtml: FOUND (modified)
- _TrainingRecordsTab.cshtml: FOUND (modified)
- AddTraining.cshtml: FOUND (modified)
- EditTraining.cshtml: FOUND (modified)
- ImportTraining.cshtml: FOUND (modified)
- Commit 65ba402f: FOUND
- Commit ec9a0227: FOUND
- Zero broken references: VERIFIED
- Build 0 errors: VERIFIED
