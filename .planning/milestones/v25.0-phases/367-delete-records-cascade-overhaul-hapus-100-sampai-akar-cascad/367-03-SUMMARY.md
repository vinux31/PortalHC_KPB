---
phase: 367-delete-records-cascade-overhaul
plan: 03
subsystem: models
tags: [badge, recompute, worker-records, ui-consistency]
requires: []
provides:
  - "CompletionDisplayText badge = jumlah baris tampil per jenis (konsisten dgn list)"
affects:
  - Models/WorkerTrainingStatus.cs
tech-stack:
  added: []
  patterns: ["computed property reads displayed-row lists (single source of truth dgn partial projection)"]
key-files:
  created:
    - HcPortal.Tests/BadgeRecomputeTests.cs
  modified:
    - Models/WorkerTrainingStatus.cs
key-decisions:
  - "Recompute di CompletionDisplayText (computed property) baca AssessmentSessions.Count + TrainingRecords.Count — sumber data IDENTIK dgn proyeksi baris partial → badge==list dijamin"
  - "WorkerDataService.cs TIDAK diubah (deviasi dari files_modified): list sudah dimuat di sana; field duplikat akan redundant + risiko 0/0 di path konstruksi WorkerTrainingStatus lain"
  - "Label: 'X completed (A assessments + T trainings)' -> 'X record (A assessment + T training)' — 'completed' menyesatkan karena baris termasuk not-passed (acceptance: tak kontradiksi)"
  - "CompletedAssessments (IsPassed) + CompletedTrainings (Passed/Valid/Permanent) DIPERTAHANKAN — dipakai _RecordsTeamBody.cshtml:29 + WorkerDataServiceSearchTests:178"
requirements-completed: ["#16", "#17", "D-01"]
duration: "10 min"
completed: 2026-06-12
---

# Phase 367 Plan 03: Badge Recompute (#16/#17, D-01) Summary

Memperbaiki kontradiksi badge↔list di tab Input Records: angka badge ringkasan worker sekarang = jumlah baris yang BENAR-BENAR tampil per jenis (semua assessment manual+online + semua training), bukan count `IsPassed`/`Passed-Valid` yang lebih kecil.

**Tasks:** 1/1 | **Files:** 1 created + 1 modified | **Tests:** 4 [Fact]

## What was built

`CompletionDisplayText` (satu-satunya konsumen = badge `bg-info text-dark` di `_TrainingRecordsTab.cshtml:246`) di-recompute:
- **Sebelum:** `"{CompletedAssessments + CompletedTrainings} completed ({CompletedAssessments} assessments + {CompletedTrainings} trainings)"` — `CompletedAssessments`=IsPassed only, `CompletedTrainings`=Passed/Valid/Permanent only → lebih kecil dari baris tampil, bikin admin bingung + stale pasca cascade delete.
- **Sesudah:** `"{AssessmentSessions.Count + TrainingRecords.Count} record ({AssessmentSessions.Count} assessment + {TrainingRecords.Count} training)"` — sumber data IDENTIK dengan proyeksi baris partial (`trainingRows` = semua TrainingRecords; `assessmentRows`+`onlineRows` = semua AssessmentSessions manual+online) → **angka badge == jumlah baris, dijamin**.

`CompletedAssessments`/`CompletedTrainings` tidak diubah (konsumen lain aman).

## Verification

- `dotnet build` — 0 error.
- `dotnet test --filter "~BadgeRecomputeTests|~WorkerDataServiceSearchTests"` — 15/15 pass (4 badge + 11 search; search test assert `CompletedAssessments==2` tetap hijau → tidak ter-regresi).
- Quick suite `--filter "Category!=Integration"` — **194/194 pass**.
- `git status _TrainingRecordsTab.cshtml` — kosong (view TIDAK disentuh, sesuai IC-4 / plan 06 polish).

## Deviations from Plan

**[Rule 2 - Pendekatan lebih aman] WorkerDataService.cs TIDAK dimodifikasi** — Found during: Task 1. files_modified menyebut WorkerDataService.cs + acceptance crit 1 menyiratkan perubahan di sana. Tapi recompute paling bersih & aman di computed property `CompletionDisplayText`: list `AssessmentSessions`/`TrainingRecords` sudah dimuat (sama dengan yang dipakai partial untuk render baris), jadi membaca `.Count` di property menjamin badge==baris tanpa field duplikat. Menambah field di WorkerDataService berisiko 0/0 di path konstruksi WorkerTrainingStatus lain yang tak menyetelnya. Files: `Models/WorkerTrainingStatus.cs` saja. Verification: badge==baris terbukti 4 [Fact]; must_haves (angka==baris, tak kontradiksi, menyusut pasca delete) terpenuhi.

**Total deviations:** 1 (lokasi fix dipindah ke model demi keamanan/kebersihan). **Impact:** Positif — menghindari bug 0/0 + tak ada field redundant.

## Issues Encountered

None.

## Self-Check: PASSED

- Badge formula berbasis baris tampil (`AssessmentSessions.Count` + `TrainingRecords.Count`) ✓.
- `BadgeRecomputeTests.cs` 4 [Fact], exit 0 ✓.
- `_TrainingRecordsTab.cshtml` git diff kosong ✓.
- Migration = FALSE ✓.

Ready for 367-04 (sibling filter #18 + reset guard #20).
