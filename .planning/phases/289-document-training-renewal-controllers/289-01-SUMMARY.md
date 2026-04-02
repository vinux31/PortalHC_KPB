---
phase: 289-document-training-renewal-controllers
plan: 01
subsystem: controllers
tags: [refactoring, controller-extraction]
dependency_graph:
  requires: [287-admin-base-controller, 288-worker-coach-organization-controllers]
  provides: [document-admin-controller, training-admin-controller, renewal-controller]
  affects: [admin-controller]
tech_stack:
  added: []
  patterns: [controller-extraction, shared-base-method]
key_files:
  created:
    - Controllers/DocumentAdminController.cs
    - Controllers/TrainingAdminController.cs
    - Controllers/RenewalController.cs
  modified:
    - Controllers/AdminBaseController.cs
    - Controllers/AdminController.cs
decisions:
  - BuildRenewalRowsAsync dipindah ke AdminBaseController karena dipakai oleh AdminController.Index() dan RenewalController
  - PartialView calls di RenewalController menggunakan absolute path ~/Views/Shared/ untuk menghindari view resolution issue
metrics:
  duration: 7m
  completed: 2026-04-02
  tasks_completed: 2
  tasks_total: 2
  files_created: 3
  files_modified: 2
---

# Phase 289 Plan 01: Document, Training, Renewal Controller Extraction Summary

Ekstraksi 3 domain terakhir dari AdminController ke controller terpisah, menyelesaikan refactoring AdminController dari 1877 baris menjadi 108 baris (Index + Maintenance saja).

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Pindahkan BuildRenewalRowsAsync + buat 3 controller baru | 3f1d6878 | AdminBaseController.cs, DocumentAdminController.cs, TrainingAdminController.cs, RenewalController.cs |
| 2 | Hapus extracted actions dari AdminController | cdc849ac | AdminController.cs |

## What Was Done

1. **AdminBaseController.cs** - Ditambahkan `BuildRenewalRowsAsync()` sebagai protected method (~160 baris) yang dipakai oleh AdminController.Index() dan RenewalController
2. **DocumentAdminController.cs** (baru) - 13 actions KKJ + CPDP: KkjMatrix, KkjUpload (GET+POST), KkjFileDownload, KkjFileDelete, KkjFileHistory, KkjBagianAdd, DeleteBagian, CpdpFiles, CpdpUpload (GET+POST), CpdpFileDownload, CpdpFileArchive, CpdpFileHistory
3. **TrainingAdminController.cs** (baru) - 7 actions training: AddTraining (GET+POST), EditTraining (GET+POST), DeleteTraining, DownloadImportTrainingTemplate, ImportTraining (GET+POST)
4. **RenewalController.cs** (baru) - 4 actions renewal: RenewalCertificate, FilterRenewalCertificate, FilterRenewalCertificateGroup, CertificateHistory
5. **AdminController.cs** - Dikurangi dari 1877 baris menjadi 108 baris (hanya Index + Maintenance)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Missing using HcPortal.Helpers in RenewalController**
- **Found during:** Task 1
- **Issue:** PaginationHelper tidak ditemukan karena missing using directive
- **Fix:** Tambahkan `using HcPortal.Helpers;` ke RenewalController.cs
- **Files modified:** Controllers/RenewalController.cs
- **Commit:** 3f1d6878

## Verification

- `dotnet build` sukses (0 errors, 70-75 warnings pre-existing)
- DocumentAdminController contains KkjMatrix, CpdpFiles
- TrainingAdminController contains AddTraining, ImportTraining
- RenewalController contains RenewalCertificate, FilterRenewalCertificate
- AdminController does NOT contain KkjMatrix, CpdpFiles, AddTraining, RenewalCertificate
- AdminBaseController contains BuildRenewalRowsAsync
- AdminController is 108 lines

## Known Stubs

None - semua code adalah copy persis dari AdminController tanpa stub.
