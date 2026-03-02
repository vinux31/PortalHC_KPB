---
phase: 90-kkj-matrix-admin-full-rewrite
plan: 04
subsystem: ui
tags: [kkj, cmp, file-download, razor, role-based]

# Dependency graph
requires:
  - phase: 90-02
    provides: AdminController KkjFileDownload action that CMP/Kkj.cshtml links to
provides:
  - CMPController.Kkj() rewritten to load KkjFiles from DB with role-based bagian filtering
  - Views/CMP/Kkj.cshtml rewritten as file download page (no competency table)
  - Views/CMP/Index.cshtml KKJ Matrix card description updated
  - All TODO-Phase90 KKJ competency blocks removed from CMPController.cs
affects: [phase-90-verification]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Role-based bagian filtering: L1-L4 (RoleLevel <= 4) see all bagians, L5-L6 see only own Unit"
    - "File download delegated to Admin/KkjFileDownload via Url.Action link"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Kkj.cshtml
    - Views/CMP/Index.cshtml

key-decisions:
  - "Use currentUser.Unit (not .Bagian or .Section) to match KkjBagian.Name for L5/L6 filtering — consistent with old stub"
  - "Razor option selected attribute uses if/else block (not inline @(condition ? 'selected' : '')) — Razor tag helper restriction"

patterns-established:
  - "CMP/Kkj.cshtml: bagian dropdown only shown when allBagians.Count > 1 (hidden for L5/L6 with single bagian)"
  - "Empty state card shown when files list is empty, no-bagian alert shown when user has no accessible bagians"

requirements-completed: [DATA-01]

# Metrics
duration: 20min
completed: 2026-03-02
---

# Phase 90 Plan 04: CMP Worker KKJ File View Summary

**CMPController.Kkj() rewritten to load KkjFiles from DB with role-filtered bagian selector, and CMP/Kkj.cshtml replaced as a file download page (no competency table)**

## Performance

- **Duration:** 20 min
- **Started:** 2026-03-02T13:10:00Z
- **Completed:** 2026-03-02T13:30:00Z
- **Tasks:** 2 auto + 1 checkpoint
- **Files modified:** 3

## Accomplishments
- CMPController.Kkj() fully rewritten: loads KkjFiles from DB, role-based bagian filtering (L1-L4 all, L5-L6 own Unit only), ViewBag.AllBagians/SelectedBagian/Files/SelectedBagianRecord set
- All 4 TODO-Phase90 blocks removed from CMPController.cs (3 comment blocks + top-of-file comment cleaned)
- Views/CMP/Kkj.cshtml rewritten: bagian dropdown selector (L1-L4 only), file list table with download buttons pointing to Admin/KkjFileDownload, empty state and no-bagian state
- Views/CMP/Index.cshtml KKJ Matrix card description updated from "View the complete KSA matrix" to "Unduh dokumen KKJ Matrix sesuai bagian Anda"

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite CMPController.Kkj() and remove TODO-Phase90 blocks** - `8137607` (feat)
2. **Task 2: Rewrite CMP/Kkj.cshtml and update CMP/Index.cshtml** - `580a581` (feat)
3. **Task 3: checkpoint:human-verify** - APPROVED 2026-03-02 (upload/download/delete/role-filtering all verified)

## Files Created/Modified
- `Controllers/CMPController.cs` - Kkj() action rewritten; TODO-Phase90 KKJ competency blocks removed
- `Views/CMP/Kkj.cshtml` - Full rewrite: file download list, bagian selector, empty/no-bagian states
- `Views/CMP/Index.cshtml` - KKJ Matrix card description updated to "Unduh dokumen KKJ Matrix sesuai bagian Anda"

## Decisions Made
- Used `currentUser.Unit` (not `.Bagian` or `.Section`) to match `KkjBagian.Name` for L5/L6 role filtering — consistent with the existing Kkj() stub that used `user?.Unit`
- Razor option `selected` attribute handled with if/else block (not inline `@(condition ? "selected" : "")`) due to Razor tag helper restriction that prohibits C# inline in attribute declarations

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Razor syntax error in bagian dropdown option selected attribute**
- **Found during:** Task 2 (Views/CMP/Kkj.cshtml rewrite)
- **Issue:** Plan spec used `@(bagian.Name == selectedBagian ? "selected" : "")` inline in `<option>` attribute — this causes Razor compiler error RZ1031 ("tag helper must not have C# in element's attribute declaration area")
- **Fix:** Replaced inline attribute with if/else block rendering two separate `<option>` elements (one with `selected="selected"`, one without)
- **Files modified:** Views/CMP/Kkj.cshtml
- **Verification:** dotnet build succeeded with zero errors after fix
- **Committed in:** 580a581 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - bug)
**Impact on plan:** Minor Razor syntax correction. No scope creep. View behavior identical.

## Issues Encountered
- Razor RZ1031 error on first build of Kkj.cshtml — the plan's inline attribute pattern is not valid Razor syntax. Fixed immediately with if/else pattern.

## User Setup Required
None - no external service configuration required.

## Checkpoint Verification

**Status:** APPROVED
**Verified:** 2026-03-02
**Verification scope:** Full Phase 90 feature verification — Admin KKJ Matrix upload/download/delete, CMP worker file download, role-based bagian filtering, assessment flow stability

## Next Phase Readiness
- Phase 90 COMPLETE — human verification checkpoint passed
- Admin KKJ Matrix (Plans 02-03) + CMP worker view (Plan 04) both rewritten and verified
- All TODO-Phase90 blocks cleared from CMPController.cs
- Assessment flow unchanged (no KKJ 500 errors expected)

---
*Phase: 90-kkj-matrix-admin-full-rewrite*
*Completed: 2026-03-02*
