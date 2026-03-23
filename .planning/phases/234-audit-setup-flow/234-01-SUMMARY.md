---
phase: 234-audit-setup-flow
plan: 01
subsystem: api
tags: [proton, silabus, delete-safety, transaction, file-management]

# Dependency graph
requires: []
provides:
  - SilabusDeletePreview, SubKompetensiDeletePreview, KompetensiDeletePreview endpoints (GET)
  - Hard delete blocked jika ada progress aktif (server-side + UI)
  - SilabusDelete dibungkus BeginTransactionAsync dengan rollback
  - Orphan cleanup (SubKompetensi, Kompetensi) dalam satu transaction
  - GuidanceReplace upload-first delete-last pattern
affects:
  - 234-02-PLAN
  - 234-03-PLAN

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Preview-before-delete pattern: GET endpoint mengembalikan impact count sebelum konfirmasi"
    - "Upload-first delete-last: file baru disimpan sebelum file lama dihapus (D-07)"

key-files:
  created: []
  modified:
    - Controllers/ProtonDataController.cs
    - Views/ProtonData/Index.cshtml

key-decisions:
  - "Hard delete silabus diblokir jika Status != 'Approved' — bukan total count, tapi active progress check"
  - "Orphan cleanup (SubKompetensi, Kompetensi) dilakukan dalam transaction yang sama, bukan SaveChanges terpisah"
  - "GuidanceReplace: file lama dihapus non-critical (try-catch LogWarning) — tidak boleh gagalkan operasi"

patterns-established:
  - "Preview endpoints (GET) untuk impact check sebelum destructive operations"
  - "Transaction wrapping untuk cascade delete dengan rollback on exception"

requirements-completed:
  - SETUP-01
  - SETUP-02

# Metrics
duration: 20min
completed: 2026-03-22
---

# Phase 234 Plan 01: Silabus Delete Safety + GuidanceReplace Fix Summary

**Hard delete silabus diblokir jika ada progress aktif, cascade delete dibungkus transaction, dan GuidanceReplace menggunakan upload-first delete-last pattern untuk mencegah file loss.**

## Performance

- **Duration:** 20 min
- **Started:** 2026-03-22T14:00:00Z
- **Completed:** 2026-03-22T14:20:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Tiga preview endpoints (deliverable, sub-kompetensi, kompetensi level) untuk impact check sebelum hard delete
- SilabusDelete mengembalikan `{ success: false, hasActiveProgress: true }` jika ada progress aktif
- SilabusDelete wrapped dalam `BeginTransactionAsync` dengan `RollbackAsync` on exception dan orphan cleanup dalam satu transaction
- GuidanceReplace: urutan diperbaiki — `CopyToAsync` sekarang terjadi SEBELUM `File.Delete` (file lama dihapus di akhir, wrapped try-catch)
- Modal warning di UI menampilkan "Deliverable digunakan oleh N coachee dengan M progress aktif" jika ada active progress

## Task Commits

1. **Task 1 + Task 2: Silabus Delete Safety + GuidanceReplace Fix** - `5818b8c` (feat)

## Files Created/Modified

- `Controllers/ProtonDataController.cs` - Tambah 3 preview endpoints GET, perbaiki SilabusDelete (transaction + impact check + orphan cleanup), perbaiki GuidanceReplace (upload-first order)
- `Views/ProtonData/Index.cshtml` - Tambah modal active progress warning, async fetch ke SilabusDeletePreview sebelum confirm delete

## Decisions Made

- Hard delete diblokir berdasarkan status `!= "Approved"` — progress yang masih active/in-review diblokir
- Orphan cleanup (SubKompetensi dan Kompetensi) dilakukan dalam transaction yang sama dengan delete deliverable
- GuidanceReplace: delete file lama adalah non-critical operation (wrapped try-catch, log warning saja — tidak gagalkan response)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

Build gagal dengan MSB3027 (file locked) karena aplikasi sedang berjalan di background — bukan error kompilasi. Tidak ada `error CS` dalam output build, artinya kode C# compile tanpa error.

## Next Phase Readiness

- Preview endpoints tersedia untuk digunakan oleh Phase 234-02 (execution flow audit)
- Transaction pattern established untuk referensi DeleteKompetensi dan operasi cascade lainnya
- GuidanceReplace sudah aman dari file loss

## Self-Check: PASSED

- FOUND: Controllers/ProtonDataController.cs dengan SilabusDeletePreview (L505), SubKompetensiDeletePreview (L517), KompetensiDeletePreview (L532)
- FOUND: BeginTransactionAsync di SilabusDelete (L578)
- FOUND: SilabusDeletePreview fetch call di Views/ProtonData/Index.cshtml (L680)
- FOUND: commit 5818b8c (task), 6396895 (docs)

---
*Phase: 234-audit-setup-flow*
*Completed: 2026-03-22*
