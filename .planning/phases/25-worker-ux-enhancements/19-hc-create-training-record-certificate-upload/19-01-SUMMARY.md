---
phase: 19-hc-create-training-record-certificate-upload
plan: "01"
subsystem: training-records
tags: [training, certificate, file-upload, hc-admin, form, viewmodel]
dependency_graph:
  requires:
    - Models/TrainingRecord.cs (existing model with SertifikatUrl, v1.6 fields)
    - Models/UnifiedTrainingRecord.cs (extended with SertifikatUrl)
    - Controllers/CMPController.cs (Records, WorkerDetail actions)
  provides:
    - CreateTrainingRecord GET/POST controller actions
    - CreateTrainingRecordViewModel with validation
    - CreateTrainingRecord.cshtml form view
    - Certificate file upload to wwwroot/uploads/certificates/
    - Certificate download link on WorkerDetail
    - Create Training Offline button on RecordsWorkerList
  affects:
    - Views/CMP/RecordsWorkerList.cshtml (new button + TempData alerts)
    - Views/CMP/WorkerDetail.cshtml (new Sertifikat column)
    - Models/UnifiedTrainingRecord.cs (SertifikatUrl property added)
tech_stack:
  added: []
  patterns:
    - File upload pattern from CDPController.UploadEvidence (extension validation, size check, timestamp prefix, wwwroot/uploads/)
    - HC/Admin role gate pattern (userRole == UserRoles.Admin || userRole == UserRoles.HC)
    - [ValidateAntiForgeryToken] on POST actions
    - ViewBag.Workers SelectListItem pattern for worker dropdown
    - TempData["Success"] redirect pattern
key_files:
  created:
    - Models/CreateTrainingRecordViewModel.cs
    - Views/CMP/CreateTrainingRecord.cshtml
  modified:
    - Controllers/CMPController.cs (CreateTrainingRecord GET+POST added; GetUnifiedRecords SertifikatUrl populated)
    - Models/UnifiedTrainingRecord.cs (SertifikatUrl property added)
    - Views/CMP/RecordsWorkerList.cshtml (Create Training Offline button + TempData alerts)
    - Views/CMP/WorkerDetail.cshtml (Sertifikat column header + download td + CSV export updated)
decisions:
  - Redirect on success goes to Records?isFiltered=true (not root Records) to avoid blank initial state
  - Worker dropdown is system-wide (all users, no section filter) per TRN-01 requirement
  - File validation errors added to ModelState (not TempData) so they render inline with the form
  - SertifikatUrl populated in GetUnifiedRecords helper so both WorkerDetail and Coach/Coachee Records views show certificate links
  - CSV export includes Sertifikat column with href value (not button text) for meaningful data
metrics:
  duration: "4 minutes"
  completed: "2026-02-20"
  tasks_completed: 2
  files_created: 2
  files_modified: 4
---

# Phase 19 Plan 01: HC Create Training Record + Certificate Upload Summary

Bootstrap 5 "Create Training Offline" feature — ViewModel + GET/POST controller actions with HC/Admin gate + file upload to wwwroot/uploads/certificates/ + entry-point button on RecordsWorkerList + certificate download column on WorkerDetail.

## Tasks Completed

| # | Task | Commit | Status |
|---|------|--------|--------|
| 1 | Create ViewModel and controller actions for CreateTrainingRecord | db343d1 | Done |
| 2 | Create form view, add button to RecordsWorkerList, add certificate download to WorkerDetail | 7bb1ccc | Done |

## What Was Built

### CreateTrainingRecordViewModel (`Models/CreateTrainingRecordViewModel.cs`)
- `[Required]` on: UserId, Judul, Penyelenggara, Kategori, Tanggal, Status
- Optional fields: TanggalMulai, TanggalSelesai, NomorSertifikat, ValidUntil, CertificateType, CertificateFile (IFormFile)
- Default Status = "Passed", default Tanggal = DateTime.Today

### Controller Actions (`Controllers/CMPController.cs`)
- **GET CreateTrainingRecord**: HC/Admin gate → loads all users system-wide (ordered by FullName) → ViewBag.Workers as SelectListItem list → returns `View(new CreateTrainingRecordViewModel())`
- **POST CreateTrainingRecord**: HC/Admin gate → validates file type (PDF/JPG/PNG) and size (max 10MB) → if ModelState invalid, re-populates ViewBag.Workers and returns view → saves file to `wwwroot/uploads/certificates/` with timestamp prefix → creates TrainingRecord with all fields → SaveChangesAsync → TempData["Success"] → RedirectToAction("Records", new { isFiltered = "true" })
- **GetUnifiedRecords helper updated**: `SertifikatUrl = t.SertifikatUrl` added to training record mapping

### Form View (`Views/CMP/CreateTrainingRecord.cshtml`)
- Three card sections: Data Pekerja (worker dropdown), Data Training (Judul, Penyelenggara, Kategori, Tanggal, TanggalMulai, TanggalSelesai, Status), Data Sertifikat (NomorSertifikat, CertificateType, ValidUntil, CertificateFile)
- `enctype="multipart/form-data"` on form, `asp-validation-for` on each required field
- Conditional validation summary (shown only when ModelState is invalid)
- Cancel/Simpan buttons linking back to Records

### RecordsWorkerList (`Views/CMP/RecordsWorkerList.cshtml`)
- "Create Training Offline" primary button added to header `d-flex gap-2` alongside existing back button
- TempData["Success"] and TempData["Error"] alert blocks inserted after header div

### WorkerDetail (`Views/CMP/WorkerDetail.cshtml`)
- New "Sertifikat" column header (6% width) added; column widths adjusted (-1% on Tanggal, Tipe, Nama/Judul, Penyelenggara, Tipe Sertifikat, Status)
- Download button (`btn-outline-success` with `bi-download` icon, `target="_blank"`) shown when SertifikatUrl is not null/empty; em dash shown otherwise
- CSV export updated: added "Sertifikat" header + certificate URL cell (href value)

### UnifiedTrainingRecord (`Models/UnifiedTrainingRecord.cs`)
- `public string? SertifikatUrl { get; set; }` property added

## Deviations from Plan

None - plan executed exactly as written.

## Success Criteria Verification

- [x] ViewModel `CreateTrainingRecordViewModel` exists with `[Required]` on UserId, Judul, Penyelenggara, Kategori, Tanggal, Status
- [x] Controller GET loads all workers system-wide into ViewBag.Workers
- [x] Controller POST: [ValidateAntiForgeryToken], HC/Admin gate, file validation (PDF/JPG/PNG, max 10MB), saves TrainingRecord, redirects to Records
- [x] `CreateTrainingRecord.cshtml` — Bootstrap 5 form with worker dropdown + all fields + file upload
- [x] RecordsWorkerList has "Create Training Offline" button and TempData alerts
- [x] WorkerDetail has Sertifikat column with download link; CSV export updated
- [x] `dotnet build` — 0 errors

## Self-Check: PASSED

All files present and all commits verified:
- `Models/CreateTrainingRecordViewModel.cs` — FOUND
- `Views/CMP/CreateTrainingRecord.cshtml` — FOUND
- `Views/CMP/RecordsWorkerList.cshtml` — FOUND (modified)
- `Views/CMP/WorkerDetail.cshtml` — FOUND (modified)
- Commit db343d1 — FOUND
- Commit 7bb1ccc — FOUND
