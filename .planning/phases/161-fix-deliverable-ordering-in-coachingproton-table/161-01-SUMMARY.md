---
phase: 161-fix-deliverable-ordering-in-coachingproton-table
plan: "01"
subsystem: ui
tags: [cdp, coaching-proton, ordering, seed-data]

# Dependency graph
requires:
  - phase: 159-deduplication-fix-guard
    provides: ProtonTrackAssignment deduplication logic
  - phase: 160-assignment-removal
    provides: Assignment cascade-delete logic
provides:
  - Stable deliverable ordering in CoachingProton table by Kompetensi/SubKompetensi/Deliverable Urutan
  - Merged split Kompetensi/SubKompetensi seed records that caused Urutan collision
affects: [cdp, coaching-proton, proton-data]

# Tech tracking
tech-stack:
  added: []
  patterns: [explicit post-mapping OrderBy on TrackingItem list before GroupBy pagination]

key-files:
  created: []
  modified:
    - Models/TrackingModels.cs
    - Controllers/CDPController.cs
    - Data/SeedData.cs

key-decisions:
  - "Added KompetensiUrutan/SubKompetensiUrutan/DeliverableUrutan int fields to TrackingItem to preserve ordering after Select() mapping"
  - "Explicit data.OrderBy() applied after building TrackingItem list, before GroupBy pagination, to guarantee stable ordering"
  - "Root cause was split Kompetensi/SubKompetensi seed records with overlapping Urutan values — fixed by merging records in SeedData.cs"

patterns-established:
  - "Pattern: Always carry Urutan fields through TrackingItem when pagination GroupBy follows a Select() mapping"

requirements-completed: []

# Metrics
duration: ~30min
completed: 2026-03-12
---

# Phase 161 Plan 01: Fix Deliverable Ordering in CoachingProton Summary

**Explicit post-mapping OrderBy on TrackingItem + merged split seed Kompetensi/SubKompetensi records to fix scrambled deliverable rows (3,4,5,6,7,1,2 → 1,2,3,4,5,6,7) in CoachingProton table**

## Performance

- **Duration:** ~30 min
- **Started:** 2026-03-12
- **Completed:** 2026-03-12
- **Tasks:** 2 (1 auto + 1 human-verify)
- **Files modified:** 3

## Accomplishments
- Added `KompetensiUrutan`, `SubKompetensiUrutan`, `DeliverableUrutan` int fields to `TrackingItem` model
- Populated Urutan fields in CDPController Select() mapping and added explicit `data.OrderBy()` before GroupBy pagination
- Discovered and fixed root cause: split Kompetensi/SubKompetensi seed records in SeedData.cs caused Urutan values for deliverables to collide, producing the scrambled order
- Deliverable rows now display in correct 1,2,3,4,5,6,7 numerical order — verified by user in browser

## Task Commits

Each task was committed atomically:

1. **Task 1: Diagnose and fix deliverable ordering** - `1189345` (fix)
2. **Task 1 (deviation - seed fix): Merge split Kompetensi/SubKompetensi records** - `4f52288` (fix)
3. **Task 2: Verify deliverable ordering in browser** - checkpoint approved by user

## Files Created/Modified
- `Models/TrackingModels.cs` - Added KompetensiUrutan, SubKompetensiUrutan, DeliverableUrutan int properties to TrackingItem
- `Controllers/CDPController.cs` - Populated Urutan fields in Select() mapping; added explicit OrderBy on data list before pagination GroupBy
- `Data/SeedData.cs` - Merged split Kompetensi/SubKompetensi records to eliminate Urutan collisions

## Decisions Made
- Added three int Urutan fields to TrackingItem rather than relying solely on DB query order — provides stable sort guarantee even after in-memory Select() and GroupBy operations
- Merged seed records rather than patching Urutan values, since split records were the structural cause of inconsistent ordering

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Merged split Kompetensi/SubKompetensi seed records**
- **Found during:** Task 1 (Diagnose and fix deliverable ordering)
- **Issue:** Seed data had two separate Kompetensi (and SubKompetensi) records representing the same competency, causing deliverables from each to have Urutan values starting from 1. Combined, the merged group showed deliverables ordered 3,4,5,6,7 (first record) then 1,2 (second record).
- **Fix:** Merged the split records in SeedData.cs so all deliverables under the competency share a single Kompetensi/SubKompetensi with sequential Urutan values
- **Files modified:** Data/SeedData.cs
- **Verification:** Deliverables displayed in correct 1-7 order after fix — user confirmed in browser
- **Committed in:** 4f52288

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Auto-fix addressed the actual root cause. Code fix alone (adding OrderBy) would have partially helped but the seed data merge was necessary for correct ordering.

## Issues Encountered
- Initial code fix (adding explicit OrderBy) was correct but insufficient alone — the split seed records meant two separate groups existed in the DB with overlapping Urutan values. Diagnosing this required examining SeedData.cs.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- CoachingProton deliverable ordering is now correct and stable
- No known blockers
- Phase 161 complete — milestone v4.1 all phases done

---
*Phase: 161-fix-deliverable-ordering-in-coachingproton-table*
*Completed: 2026-03-12*
