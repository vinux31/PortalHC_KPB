---
phase: 279-tambah-komponen-waktu-jam-menit-pada-tanggal-tutup-ujian-examwindowclosedate
plan: 01
subsystem: assessment-admin
tags: [ui, form, datetime, assessment]
dependency_graph:
  requires: []
  provides: [ExamWindowCloseDate-datetime-combiner]
  affects: [CreateAssessment, EditAssessment, AdminController]
tech_stack:
  added: []
  patterns: [date-time-hidden-combiner]
key_files:
  created: []
  modified:
    - Views/Admin/CreateAssessment.cshtml
    - Views/Admin/EditAssessment.cshtml
    - Controllers/AdminController.cs
decisions:
  - Field ExamWindowCloseDate berubah dari opsional menjadi wajib (required) di frontend
  - Default waktu 23:59 untuk kedua view
  - ModelState.Remove dihapus karena field sekarang wajib
  - Model property tetap DateTime? untuk backward compatibility
metrics:
  duration: 4 minutes
  completed: 2026-04-01
---

# Phase 279 Plan 01: Tambah Komponen Waktu ExamWindowCloseDate Summary

Date+time combiner untuk ExamWindowCloseDate di CreateAssessment dan EditAssessment mengikuti pola Schedule combiner yang sudah ada, dengan default 23:59 dan field wajib.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Tambah date+time combiner di CreateAssessment dan step validation | 614b36dd | Views/Admin/CreateAssessment.cshtml |
| 2 | Tambah date+time combiner di EditAssessment + hapus ModelState.Remove | f233f584 | Views/Admin/EditAssessment.cshtml, Controllers/AdminController.cs |

## Changes Made

### Task 1: CreateAssessment
- Ganti single `<input type="date">` dengan date+time+hidden combiner (ewcdDateInput, ewcdTimeInput, ewcdHidden)
- Tambah validasi step 3 untuk kedua field (date dan time)
- Tambah JS combiner di form submit handler (setelah schedule combiner)
- Tambah entry "Tutup Ujian" di summary panel Step 4
- Default waktu: 23:59, kedua field required

### Task 2: EditAssessment + Controller
- Ganti single `<input type="date">` dengan date+time+hidden combiner mengikuti layout Schedule
- Populate dari model value dengan null-safe operators (`?.ToString()`, `?? "23:59"`)
- Tambah ewcd combiner JS di KEDUA form submit handlers (package-warning handler + always-run handler)
- Hapus `ModelState.Remove("ExamWindowCloseDate")` di AdminController.cs

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Verification Results

- `grep -c "ewcdHidden" CreateAssessment.cshtml` = 4 (PASS, minimal 3)
- `grep -c "ewcdHidden" EditAssessment.cshtml` = 7 (PASS, minimal 4)
- `grep -c "ModelState.Remove.*ExamWindowCloseDate" AdminController.cs` = 0 (PASS)
- `grep -c "ewcdDateInput" CreateAssessment.cshtml` = 6 (PASS, minimal 2)
- Build: compilation success (file copy error only due to running app, no CS errors)
