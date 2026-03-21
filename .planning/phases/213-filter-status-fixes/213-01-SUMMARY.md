---
phase: 213-filter-status-fixes
plan: 01
subsystem: ui
tags: [cmp, records, filter, javascript, razor]

# Dependency graph
requires: []
provides:
  - Per-category status filter yang akurat di CMP Records Team View
  - CompletedTrainings count konsisten (Passed|Valid|Permanent)
  - NIP search case-insensitive
affects: [CMP Records Team View, WorkerDataService]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "data-completed-categories attribute untuk menyimpan kategori selesai per-worker"
    - "JS filter membaca completedCats dari data attribute alih-alih boolean rowHasTraining"

key-files:
  created: []
  modified:
    - Services/WorkerDataService.cs
    - Views/CMP/RecordsTeam.cshtml

key-decisions:
  - "completedCategories dihitung di Razor (server-side) dan disimpan sebagai data attribute lowercase"
  - "matchStatus JS menggunakan completedCats.includes(category) saat category dipilih, fallback ke rowHasTraining saat tidak ada category"

patterns-established:
  - "Status Permanent setara dengan Passed/Valid untuk semua keperluan completion count"

requirements-completed: [FLT-01, FLT-02, FLT-03]

# Metrics
duration: 8min
completed: 2026-03-21
---

# Phase 213 Plan 01: Filter & Status Fixes Summary

**Per-category status filter di Team View menggunakan data-completed-categories attribute, CompletedTrainings count menyertakan status Permanent, dan NIP search case-insensitive via ToLower()**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-21T07:10:00Z
- **Completed:** 2026-03-21T07:18:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- completedTrainings count di WorkerDataService.cs sekarang menyertakan status "Permanent" (konsisten dengan isCompleted check)
- Razor menghitung completedCategories per-worker dan mengeksposnya sebagai data attribute lowercase
- JS filterTeamTable() membaca data-completed-categories untuk filter status Sudah/Belum saat category dipilih
- NIP disimpan lowercase di data-nip sehingga search case-insensitive

## Task Commits

Setiap task di-commit secara atomik:

1. **Task 1: Fix CompletedTrainings count — tambah Permanent (FLT-02)** - `d361c84` (fix)
2. **Task 2: Fix Category+Status filter dan NIP search (FLT-01, FLT-03)** - `137fcc7` (fix)

## Files Created/Modified
- `Services/WorkerDataService.cs` - completedTrainings count tambah `tr.Status == "Permanent"`
- `Views/CMP/RecordsTeam.cshtml` - tambah completedCategories Razor var, data-completed-categories attr, JS matchStatus per-category, NIP ToLower()

## Decisions Made
- completedCategories dihitung server-side di Razor (lebih sederhana daripada menghitung ulang di JS)
- matchStatus fallback ke rowHasTraining saat tidak ada category dipilih (mempertahankan perilaku sebelumnya untuk filter status saja)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

Build menampilkan error MSB3027 (file lock) karena aplikasi sedang berjalan — ini bukan error kompilasi. Tidak ada error C#/Razor.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Filter dan status fixes selesai, siap untuk fase berikutnya (214 atau milestone berikutnya)
- Tidak ada blocker

---
*Phase: 213-filter-status-fixes*
*Completed: 2026-03-21*
