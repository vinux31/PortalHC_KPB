---
phase: 83-master-data-qa
plan: 02
subsystem: ui
tags: [cpdp, kkj-idp-mapping, admin, excel-export, reference-guard]

# Dependency graph
requires:
  - phase: 83-master-data-qa
    provides: CpdpItems data verified as foundation for IDP flows
provides:
  - IDP reference guard on CpdpItemDelete (blocks deletion when IdpItems reference the competency)
  - CMP/Mapping cross-link on CpdpItems editor
affects: [84-assessment-flow-qa, 86-plan-idp-development]

# Tech tracking
tech-stack:
  added: []
  patterns: [reference-guard-on-delete (mirrors KkjMatrixDelete/KkjBagianDelete pattern)]

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/CpdpItems.cshtml

key-decisions:
  - "CpdpItemDelete gets reference guard: block if IdpItems.CountAsync(i => i.Kompetensi == item.NamaKompetensi) > 0"
  - "Export link section filter already implemented correctly in JS — no controller change needed"
  - "CSRF tokens already present in all fetch calls — no view change needed"

patterns-established:
  - "Delete reference guard pattern: check referencing table before Remove(), return { success=false, blocked=true, message } if count > 0"

requirements-completed: [DATA-02]

# Metrics
duration: 15min
completed: 2026-03-02
---

# Phase 83 Plan 02: KKJ-IDP Mapping Editor QA Summary

**CpdpItemDelete hardened with IDP reference guard (mirrors KkjMatrixDelete pattern); CMP/Mapping cross-link added to editor breadcrumb**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-02T07:26:18Z
- **Completed:** 2026-03-02T07:41:00Z
- **Tasks:** 1 of 2 complete (paused at human-verify checkpoint)
- **Files modified:** 2

## Accomplishments
- Reviewed all 5 CpdpItems controller actions against plan checklist
- Added delete reference guard to CpdpItemDelete — counts IdpItems referencing the competency name, returns blocked error with count if > 0
- Added "Lihat di CMP/Mapping" button in CpdpItems.cshtml breadcrumb for cross-navigation
- Verified: rename guard uses correct DbSet (`IdpItems`) and field (`Kompetensi`), all null coalescing present, export section filter already implemented in JS, CSRF tokens present in all fetch calls, export uses standard `File()` response

## Task Commits

Each task was committed atomically:

1. **Task 1: Code review and fix KKJ-IDP Mapping controller + view** - `eec9211` (fix)

**Plan metadata:** pending final commit

## Files Created/Modified
- `Controllers/AdminController.cs` - Added IDP reference guard to CpdpItemDelete (lines 459-475)
- `Views/Admin/CpdpItems.cshtml` - Added CMP/Mapping cross-link button in breadcrumb

## Decisions Made
- Delete reference guard added (Rule 2 - missing critical functionality): CpdpItemDelete had no guard while the rename path in CpdpItemsSave correctly blocks. Parity required.
- Export section filter: already implemented dynamically in JavaScript (sectionFilter change event updates `btnExport.href`). No controller change needed.
- CSRF tokens: already present on all three fetch call sites. No change needed.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added IDP reference guard to CpdpItemDelete**
- **Found during:** Task 1 (code review)
- **Issue:** Plan identified this gap — CpdpItemDelete had no check for referencing IDP records, while CpdpItemsSave's rename path correctly blocks. Deleting a CpdpItem with IDP references would leave orphaned IdpItem.Kompetensi string references.
- **Fix:** Added `_context.IdpItems.CountAsync(i => i.Kompetensi == item.NamaKompetensi)` check before Remove(); returns `{ success=false, blocked=true, message }` if count > 0.
- **Files modified:** Controllers/AdminController.cs
- **Verification:** No CS errors in build. Pattern matches KkjBagianDelete guard.
- **Committed in:** eec9211 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 missing critical)
**Impact on plan:** Fix was explicitly called out in plan task description as required. No scope creep.

## Issues Encountered
- dotnet build reported MSBuild copy error (MSB3027/MSB3021) because the running dev server (PID 15028) had HcPortal.exe locked. No C# compilation errors (`error CS`) were found. Code compiles correctly.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- Task 1 complete and committed (eec9211)
- Awaiting human browser verification (Task 2 checkpoint) before DATA-02 is fully signed off
- All 8 test flows documented in the checkpoint for user verification

---
*Phase: 83-master-data-qa*
*Completed: 2026-03-02*
