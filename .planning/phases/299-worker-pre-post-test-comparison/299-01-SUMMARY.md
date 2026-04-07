---
phase: 299-worker-pre-post-test-comparison
plan: "01"
subsystem: CMPController
tags: [pre-post-test, gain-score, assessment, worker-view]
dependency_graph:
  requires: []
  provides: [ViewBag.PairedGroups, ViewBag.StandaloneExams, ViewBag.ComparisonData, ViewBag.GainScorePending, ViewBag.HasComparisonSection]
  affects: [Views/CMP/Assessment.cshtml, Views/CMP/Results.cshtml]
tech_stack:
  added: []
  patterns: [ViewBag data passing, LINQ pair grouping, IDOR prevention via UserId check]
key_files:
  created: []
  modified:
    - Controllers/CMPController.cs
decisions:
  - "LinkedGroupId/LinkedSessionId adalah int? bukan Guid? — kode diadaptasi dari plan untuk menggunakan HashSet<int> dan int cast"
  - "Pair grouping ditempatkan setelah auto-transition loop agar status Open/Upcoming sudah dikoreksi sebelum grouping"
  - "Comparison data query hanya dieksekusi untuk PostTest dengan LinkedSessionId — null-check dan IDOR check sebelum query ET scores"
metrics:
  duration: "15 menit"
  completed_date: "2026-04-07"
  tasks_completed: 2
  files_modified: 1
---

# Phase 299 Plan 01: CMPController Pre-Post Data Extension Summary

**One-liner:** CMPController extended dengan pair grouping untuk Assessment() dan gain score comparison data untuk Results() termasuk IDOR prevention dan null-safety.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Extend Assessment() — Pre-Post pair grouping | 8b69c498 | Controllers/CMPController.cs |
| 2 | Extend Results() — Comparison data dengan gain score | 8b69c498 | Controllers/CMPController.cs |

## What Was Built

### Task 1: Assessment() Pair Grouping

Di `CMPController.Assessment()`, setelah query `exams` dieksekusi dan auto-transition status diterapkan, ditambahkan logika:

1. Filter `prePairs` dan `postPairs` dari `exams` yang memiliki `AssessmentType` dan `LinkedGroupId`
2. Loop `prePairs` untuk membentuk `pairedGroups` (list of `{Pre, Post}`)
3. Satu query tambahan untuk Completed Pre sessions yang pasangannya (Post) ada di `exams` tapi Pre-nya sudah Completed (tidak ada di `exams`)
4. `standaloneExams` = semua exam yang tidak masuk pair
5. Set `ViewBag.PairedGroups` dan `ViewBag.StandaloneExams`

### Task 2: Results() Comparison Data

Di `CMPController.Results()`, sebelum `return View(viewModel)`, ditambahkan:

1. Default ViewBag: `HasComparisonSection = false`, `GainScorePending = false`, `ComparisonData = null`
2. Guard: hanya proses jika `AssessmentType == "PostTest"` dan `LinkedSessionId.HasValue`
3. Query `preSession` dengan IDOR check: `preSession.UserId == assessment.UserId`
4. Null-check `preSession` sebelum akses
5. Essay pending check: `HasManualGrading && IsPassed == null`
6. Query 2 set ET scores (bukan N+1)
7. Hitung `gainScore` per ElemenTeknis dengan formula `(Post - Pre) / (100 - Pre) * 100`
8. Edge case: `PreScore >= 100` → `gainScore = 100`; Essay pending → `gainScore = null`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Tipe data LinkedGroupId/LinkedSessionId adalah int? bukan Guid?**
- **Found during:** Task 1, saat membaca Models/AssessmentSession.cs
- **Issue:** Plan menggunakan `Guid` untuk ID fields, tapi actual model menggunakan `int?`
- **Fix:** Semua referensi `Guid` diubah ke `int`; `new HashSet<Guid>` → `new HashSet<int>`; cast `(Guid)pg.Pre.Id` → `(int)pg.Pre.Id`
- **Files modified:** Controllers/CMPController.cs
- **Commit:** 8b69c498

## Known Stubs

Tidak ada stub — semua ViewBag diisi dengan data real dari query database.

## Threat Flags

Tidak ada threat surface baru di luar yang sudah didefinisikan di threat model plan.

| Mitigated | File | Description |
|-----------|------|-------------|
| T-299-01 IDOR | Controllers/CMPController.cs | `preSession.UserId == assessment.UserId` — mencegah worker melihat data session milik user lain |
| T-299-02 Info Disclosure | Controllers/CMPController.cs | `ComparisonData` hanya diisi jika preSession milik user sama dan tidak null |

## Self-Check: PASSED

- Controllers/CMPController.cs dimodifikasi: FOUND
- Commit 8b69c498: FOUND
- ViewBag.PairedGroups: FOUND (baris 302)
- ViewBag.StandaloneExams: FOUND (baris 303)
- ViewBag.ComparisonData: FOUND (baris 2318)
- ViewBag.HasComparisonSection: FOUND (baris 2320)
- dotnet build: 0 Error(s)
