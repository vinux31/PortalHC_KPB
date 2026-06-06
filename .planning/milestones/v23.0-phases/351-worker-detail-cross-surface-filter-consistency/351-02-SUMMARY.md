---
phase: 351-worker-detail-cross-surface-filter-consistency
plan: 02
subsystem: api
tags: [aspnetcore, csharp, viewbag, linq, xunit, cmp-records]

requires:
  - phase: 351-worker-detail-cross-surface-filter-consistency (plan 01)
    provides: RED spec yang menargetkan #categoryFilter off-master + #typeFilter
provides:
  - CMPController.BuildActualCategories (public static helper, distinct-actual Kategori)
  - ViewBag.ActualCategoriesJson di RecordsWorkerDetail (SF-04) + Records (SF-05)
affects: [351-03, 351-04]

tech-stack:
  added: []
  patterns:
    - "Distinct-actual option source — opsi dropdown dari unifiedRecords.Kategori, bukan master AssessmentCategories"
    - "public static controller helper reachable dari test tanpa InternalsVisibleTo"

key-files:
  created:
    - HcPortal.Tests/BuildActualCategoriesTests.cs
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "Worker Detail: hapus baris ViewBag.MasterCategoriesJson lama (view Plan 03 baca ActualCategoriesJson); SubCategoryMapJson master TETAP untuk cascade SubKategori"
  - "Records (My Records): ActualCategoriesJson ditempatkan setelah konstruksi vm, SEBELUM block if(roleLevel<=4) → tersedia semua role"
  - "Team View MasterCategoriesJson (:527) tidak disentuh"

patterns-established:
  - "BuildActualCategories DRY — dipakai kedua surface; sumber opsi = sumber data-category → compare exact-equals aman"

requirements-completed: [SF-04, SF-05]

duration: 14min
completed: 2026-06-06
---

# Phase 351 Plan 02: Backend Distinct-Actual Kategori Summary

**ViewBag.ActualCategoriesJson dari record aktual (distinct unifiedRecords.Kategori) menggantikan sumber master di Worker Detail + My Records, dengan helper BuildActualCategories + 3 xUnit**

## Performance

- **Duration:** ~14 min
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- `BuildActualCategories(IEnumerable<UnifiedTrainingRecord>)` — public static, distinct non-empty Kategori, OrdinalIgnoreCase dedupe, OrderBy ascending
- SF-04: `RecordsWorkerDetail` set `ViewBag.ActualCategoriesJson` dari `unifiedRecords` (authz `:543-556` byte-identical, SubCategoryMapJson master tetap)
- SF-05: `Records` set `ViewBag.ActualCategoriesJson` dari `unified` DI LUAR block `roleLevel<=4` (My Records Kategori untuk semua role)
- 3 `[Fact]` hijau + full suite 112/112 (no regression)

## Task Commits

1. **Task 1: Helper + xUnit** - `87078423` (feat)
2. **Task 2: ViewBag.ActualCategoriesJson 2 action** - `cadd0a9b` (feat)

## Files Created/Modified
- `Controllers/CMPController.cs` - Helper BuildActualCategories + 2 ViewBag swap
- `HcPortal.Tests/BuildActualCategoriesTests.cs` - 3 [Fact]

## Decisions Made
- Worker Detail: baris MasterCategoriesJson lama dihapus (view Plan 03 baca ActualCategoriesJson); SubCategoryMapJson master DIPERTAHANKAN.
- My Records: ActualCategoriesJson di luar block roleLevel<=4 → tersedia semua role.
- Team View MasterCategoriesJson (:527) tak disentuh.

## Deviations from Plan
None - plan executed exactly as written. (TDD task 1: impl + 3 test pass first run; tidak ada RED iteration karena helper trivial-correct.)

## Issues Encountered
None.

## Next Phase Readiness
- ViewBag.ActualCategoriesJson siap dikonsumsi Plan 03 (RecordsWorkerDetail.cshtml #categoryFilter) + Plan 04 (Records.cshtml #categoryFilter).
- SF-04/05 backend hijau; view-side (selector DOM) belum → spec masih RED sampai Plan 03/04.

---
*Phase: 351-worker-detail-cross-surface-filter-consistency*
*Completed: 2026-06-06*
