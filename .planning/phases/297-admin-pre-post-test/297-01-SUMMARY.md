---
phase: 297-admin-pre-post-test
plan: 01
subsystem: assessment-admin
tags: [pre-post-test, create-assessment, viewmodel, backend, frontend]
dependency_graph:
  requires: []
  provides: [pre-post-session-creation, MonitoringSubRowViewModel, ppt-jadwal-form]
  affects: [AssessmentMonitoringViewModel, AssessmentAdminController, CreateAssessment-view]
tech_stack:
  added: []
  patterns: [transactional-dual-save, linkedGroupId-cross-link, collapse-toggle-js]
key_files:
  created: []
  modified:
    - Models/AssessmentMonitoringViewModel.cs
    - Controllers/AssessmentAdminController.cs
    - Views/Admin/CreateAssessment.cshtml
decisions:
  - "isPrePostMode branch di POST action return early — flow standard tidak terpengaruh sama sekali"
  - "Transaksi database tunggal untuk Pre+Post sessions: rollback atomik jika gagal"
  - "durationFieldWrapper dipindahkan ke dalam standard-jadwal-section untuk toggle yang bersih"
  - "T-297-01 diterapkan: AssessmentTypeInput divalidasi server-side hanya 'Standard' atau 'PrePostTest'"
metrics:
  duration: "~25 menit"
  completed_date: "2026-04-07"
  tasks_completed: 3
  tasks_total: 3
  files_changed: 3
---

# Phase 297 Plan 01: Admin Pre-Post Test Foundation Summary

**One-liner:** Fondasi Pre-Post Test dengan ViewModel extension, CreateAssessment POST transaksional (2 session per peserta), dan form view dengan dropdown tipe + dual-section jadwal.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Extend MonitoringGroupViewModel dengan field Pre-Post | 2a64e6bb | Models/AssessmentMonitoringViewModel.cs |
| 2 | CreateAssessment POST — buat 2 session per user untuk Pre-Post | 0b36738d | Controllers/AssessmentAdminController.cs |
| 3 | CreateAssessment view — dropdown AssessmentType + dual-section jadwal | 074d9b94 | Views/Admin/CreateAssessment.cshtml |

## What Was Built

### Task 1: MonitoringGroupViewModel Extension
Ditambahkan 4 property baru ke `MonitoringGroupViewModel`:
- `IsPrePostGroup` — flag apakah grup ini adalah Pre-Post
- `LinkedGroupId` — ID group penghubung Pre dan Post
- `PreSubRow` / `PostSubRow` — sub-row monitoring per fase

Ditambahkan class baru `MonitoringSubRowViewModel` untuk representasi data monitoring per fase (Pre atau Post).

### Task 2: CreateAssessment POST Pre-Post Logic
Modifikasi `AssessmentAdminController.CreateAssessment` POST:
- **Parameter baru:** `AssessmentTypeInput`, `PreSchedule/PostSchedule`, `PreDurationMinutes/PostDurationMinutes`, `PreExamWindowCloseDate/PostExamWindowCloseDate`, `SamePackage`
- **Validasi server-side:** PostSchedule > PreSchedule (D-06), semua field wajib, T-297-01 (tipe input hanya "Standard" atau "PrePostTest")
- **Branch isPrePostMode:** Buat Pre sessions → SaveChanges → Buat Post sessions → SaveChanges → Cross-link LinkedSessionId → Commit, dalam satu transaksi. Return early, flow standard tidak terpengaruh.
- Pre session: `AssessmentType="PreTest"`, `GenerateCertificate=false` (D-20)
- Post session: `AssessmentType="PostTest"`, `LinkedGroupId=linkedGroupId`, `LinkedSessionId=preSession.Id`

### Task 3: CreateAssessment View
- Dropdown `AssessmentTypeInput` (Standard / Pre-Post Test) ditambahkan di Step 1 setelah field Title
- Field jadwal standar (Schedule, Duration) di-wrap dalam `id="standard-jadwal-section"`
- `id="ppt-jadwal-section"` ditambahkan dengan 2 card: Jadwal Pre-Test dan Jadwal Post-Test
- Checkbox "Gunakan paket soal yang sama untuk Pre dan Post" dengan info badge
- JavaScript: toggle show/hide section, min date Post >= Pre (D-06), badge toggle

## Deviations from Plan

### Auto-added Security

**1. [Rule 2 - Security] T-297-01: Validasi server-side AssessmentTypeInput**
- **Found during:** Task 2 — threat model T-297-01 menetapkan disposisi `mitigate`
- **Issue:** Tanpa validasi, user bisa submit nilai arbitrary ke AssessmentTypeInput
- **Fix:** Tambah validasi: jika AssessmentTypeInput tidak null dan bukan "Standard" atau "PrePostTest", tambah ModelState error
- **Files modified:** Controllers/AssessmentAdminController.cs
- **Commit:** 0b36738d (termasuk dalam task commit)

### Structural Adjustment

**2. [Rule 1 - Layout] durationFieldWrapper dipindahkan ke dalam standard-jadwal-section**
- **Found during:** Task 3 — Plan meminta "wrap field Schedule, DurationMinutes, ExamWindowCloseDate existing dalam div standard-jadwal-section". Duration berada di posisi berbeda dari Schedule.
- **Fix:** Duration dimasukkan ke dalam standard-jadwal-section bersama Schedule, karena keduanya perlu disembunyikan bersama saat Pre-Post dipilih. Referensi JS `durationFieldWrapper` tetap valid karena elemen masih ada (hanya posisi parent berubah).
- **Files modified:** Views/Admin/CreateAssessment.cshtml

## Known Stubs

Tidak ada stub. Semua field terhubung ke backend dan tersimpan ke DB.

## Self-Check: PASSED

Files created/modified:
- FOUND: Models/AssessmentMonitoringViewModel.cs
- FOUND: Controllers/AssessmentAdminController.cs
- FOUND: Views/Admin/CreateAssessment.cshtml

Commits:
- FOUND: 2a64e6bb
- FOUND: 0b36738d
- FOUND: 074d9b94
