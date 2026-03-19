---
phase: 200-renewal-chain-foundation
plan: 02
subsystem: database
tags: [ef-core, linq, hashset, batch-query, certification]

# Dependency graph
requires:
  - phase: 200-01
    provides: RenewsSessionId, RenewsTrainingId FK columns di AssessmentSessions dan TrainingRecords

provides:
  - IsRenewed property pada SertifikatRow (bool, computed by BuildSertifikatRowsAsync)
  - 4 batch queries renewal chain resolution (AS->AS, TR->AS, AS->TR, TR->TR)
  - HashSet merge pattern untuk renewedAssessmentSessionIds dan renewedTrainingRecordIds

affects:
  - 200-03
  - 202-renewal-ui
  - 204-certificate-history

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Batch renewal lookup: 4 queries + 2 HashSet merges, bukan per-row sub-query"
    - "HashSet.Contains() untuk O(1) lookup per SertifikatRow mapping"

key-files:
  created: []
  modified:
    - Models/CertificationManagementViewModel.cs
    - Controllers/CDPController.cs

key-decisions:
  - "Batch lookup ditempatkan setelah trainingAnon (sebelum trainingRows mapping) agar renewedTrainingRecordIds tersedia saat mapping trainingRows"
  - "Tidak menggunakan ToHashSetAsync() karena mungkin tidak tersedia — pakai ToListAsync() + new HashSet<int>(...)"

patterns-established:
  - "Renewal flag IsRenewed: computed at read-time via batch lookup, bukan stored column"

requirements-completed:
  - RENEW-02

# Metrics
duration: 10min
completed: 2026-03-19
---

# Phase 200 Plan 02: Renewal Chain Foundation — IsRenewed Flag Summary

**SertifikatRow mendapatkan property IsRenewed via 4 batch queries dan HashSet merge di BuildSertifikatRowsAsync, tanpa sub-query per-row**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-03-19T~
- **Completed:** 2026-03-19T~
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments

- Tambah `public bool IsRenewed { get; set; }` ke class SertifikatRow
- 4 batch LINQ queries untuk mencari semua AS/TR ID yang sudah di-renew
- HashSet merge untuk `renewedAssessmentSessionIds` dan `renewedTrainingRecordIds`
- IsRenewed di-assign di kedua mapping (trainingRows dan assessmentRows) via `HashSet.Contains()`
- Build sukses 0 error

## Task Commits

1. **Task 1: Tambah IsRenewed ke SertifikatRow + enhance BuildSertifikatRowsAsync** - `f3af6d4` (feat)

**Plan metadata:** (akan dicommit bersama docs)

## Files Created/Modified

- `Models/CertificationManagementViewModel.cs` - Tambah property IsRenewed ke SertifikatRow
- `Controllers/CDPController.cs` - Batch renewal lookup + IsRenewed assignment di BuildSertifikatRowsAsync

## Decisions Made

- Batch lookup ditempatkan sebelum `trainingRows` mapping (bukan setelah `assessmentAnon`) agar `renewedTrainingRecordIds` tersedia lebih awal
- `ToListAsync()` + `new HashSet<int>(...)` dipilih daripada `ToHashSetAsync()` untuk kompatibilitas

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Pindahkan posisi batch lookup ke sebelum trainingRows mapping**

- **Found during:** Task 1 (implementasi batch lookup)
- **Issue:** Plan menginstruksikan lookup setelah `assessmentAnon.ToListAsync()`, tapi `renewedTrainingRecordIds` dibutuhkan lebih awal (untuk mapping `trainingRows`)
- **Fix:** Tempatkan semua 4 batch queries + HashSet merge setelah `trainingAnon.ToListAsync()` dan sebelum `var trainingRows = ...`
- **Files modified:** Controllers/CDPController.cs
- **Verification:** `dotnet build` 0 error
- **Committed in:** f3af6d4 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - bug in plan instruction ordering)
**Impact on plan:** Fix necessary for correctness — kode tidak akan compile jika tidak dipindah. Tidak ada scope creep.

## Issues Encountered

- Instruksi plan menempatkan batch lookup setelah assessmentAnon, tapi variabel dibutuhkan lebih awal untuk trainingRows. Diselesaikan dengan memindahkan posisi lookup (deviation Rule 1).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- IsRenewed flag siap digunakan oleh phase 202 (renewal UI) dan 204 (certificate history)
- BuildSertifikatRowsAsync sekarang menghasilkan flag renewal yang akurat berdasarkan chain lookup

---
*Phase: 200-renewal-chain-foundation*
*Completed: 2026-03-19*
