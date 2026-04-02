---
phase: 291-fix-view-urlaction-references
plan: "01"
subsystem: views/admin
tags: [bugfix, url-routing, admin-hub, worker-views]
dependency_graph:
  requires: []
  provides: [valid-admin-hub-links, valid-worker-view-links]
  affects: [Views/Admin]
tech_stack:
  added: []
  patterns: [Url.Action with explicit controller names]
key_files:
  created: []
  modified:
    - Views/Admin/Index.cshtml
    - Views/Admin/ManageWorkers.cshtml
    - Views/Admin/CreateWorker.cshtml
    - Views/Admin/EditWorker.cshtml
    - Views/Admin/ImportWorkers.cshtml
    - Views/Admin/WorkerDetail.cshtml
    - Views/Admin/CoachCoacheeMapping.cshtml
decisions:
  - "Index.cshtml hub links diperbaiki ke 10 controller target yang benar sesuai refactor domain controllers"
  - "Worker views diperbaiki ke WorkerController (bukan AdminController) sesuai refactor phase sebelumnya"
metrics:
  duration: 8m
  completed: 2026-04-02
  tasks_completed: 2
  files_modified: 7
---

# Phase 291 Plan 01: Fix View Url.Action References Summary

**One-liner:** Perbaikan 11 broken Url.Action references di 7 Admin views — Index hub (10 links) dan CoachCoacheeMapping (1 link) ke domain controllers yang benar.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Fix Index hub + Worker views Url.Action references | 05641b87 | Index.cshtml, ManageWorkers.cshtml, CreateWorker.cshtml, EditWorker.cshtml, ImportWorkers.cshtml, WorkerDetail.cshtml |
| 2 | Fix CoachCoacheeMapping Url.Action references | 0a58b201 | CoachCoacheeMapping.cshtml |

## What Was Fixed

### Index.cshtml (10 replacements)
- `ManageWorkers` → controller `Worker`
- `ManageOrganization` → controller `Organization`
- `KkjMatrix` → controller `DocumentAdmin`
- `CpdpFiles` → controller `DocumentAdmin`
- `CoachCoacheeMapping` → controller `CoachMapping`
- `ManageAssessment` → controller `AssessmentAdmin`
- `AssessmentMonitoring` → controller `AssessmentAdmin`
- `AuditLog` → controller `AssessmentAdmin`
- `ManageCategories` → controller `AssessmentAdmin`
- `RenewalCertificate` → controller `Renewal`

### ManageWorkers.cshtml (9 replacements)
CreateWorker, ImportWorkers, ExportWorkers, ManageWorkers (toggle/filter/reset), WorkerDetail, DeactivateWorker, ReactivateWorker, DeleteWorker — semua ganti dari `Admin` ke `Worker`.

### CreateWorker.cshtml (3 replacements)
ManageWorkers breadcrumb dan tombol Batal → `Worker`.

### EditWorker.cshtml (3 replacements)
ManageWorkers breadcrumb dan tombol Batal → `Worker`.

### ImportWorkers.cshtml (3 replacements)
ManageWorkers, ReactivateWorker, DownloadImportTemplate → `Worker`.

### WorkerDetail.cshtml (2 replacements)
ManageWorkers breadcrumb dan tombol Kembali → `Worker`.

### CoachCoacheeMapping.cshtml (1 replacement)
DownloadMappingImportTemplate → controller `CoachMapping`.

## Verification

- grep 'Url.Action.*"Admin"' pada 7 files — hanya mengembalikan Url.Action("Index", "Admin") dan Url.Action("Maintenance", "Admin") (valid)
- Zero Url.Action("ManageWorkers", "Admin"), Zero Url.Action("DeactivateWorker", "Admin"), dll.
- dotnet build: 0 errors, 70 warnings (warnings pre-existing, tidak terkait perubahan ini)

## Deviations from Plan

None — plan dieksekusi persis sesuai instruksi.

## Known Stubs

None.

## Self-Check: PASSED

- Views/Admin/Index.cshtml: FOUND
- Views/Admin/ManageWorkers.cshtml: FOUND
- Views/Admin/CreateWorker.cshtml: FOUND
- Views/Admin/EditWorker.cshtml: FOUND
- Views/Admin/ImportWorkers.cshtml: FOUND
- Views/Admin/WorkerDetail.cshtml: FOUND
- Views/Admin/CoachCoacheeMapping.cshtml: FOUND
- Commit 05641b87: FOUND
- Commit 0a58b201: FOUND
