---
phase: 350-team-view-search-scope-export-parity
plan: 03
subsystem: api
tags: [ef-core, excel-export, cmp-records, playwright, uat]

requires:
  - phase: 350-team-view-search-scope-export-parity (plan 02)
    provides: SF-01 shared predicate (makes Export Assessment non-empty for assessment-title search, D-06)
provides:
  - SF-06 export Category symmetry — a.Category projected into AllWorkersHistoryRow.Kategori (additive)
  - Controller-level Category narrow + drop-archived in ExportRecordsTeamAssessment
  - Phase gate: 109/109 xUnit GREEN + Playwright e2e 2 passed (real browser UAT)
affects: [351-worker-detail-cross-surface (sequential — shares WorkerDataService.cs)]

tech-stack:
  added: []
  patterns: [controller-level export narrow to avoid regressing shared service callers]

key-files:
  created: []
  modified:
    - Services/WorkerDataService.cs
    - Controllers/CMPController.cs

key-decisions:
  - "SF-06 narrow at controller (ExportRecordsTeamAssessment), NOT in GetAllWorkersHistory — preserves no-arg caller AssessmentAdminController:308 (Pitfall 3)"
  - "Archived rows (Kategori==null, no Category column) auto-dropped when Category active via !IsNullOrEmpty guard — matches on-screen worker-visibility (D-07)"

patterns-established:
  - "Additive projection of an existing-but-null model field (Kategori) instead of schema/migration change"

requirements-completed: [SF-06]

duration: ~18min
completed: 2026-06-05
---

# Phase 350 Plan 03: SF-06 Export Category Symmetry + Phase Gate Summary

**Export Assessment is now WYSIWYG — non-empty for assessment-title search (from SF-01) and current-session rows narrowed per Category with archived (no-Category) rows dropped when filtered; verified by full test suite + real-browser Playwright UAT**

## Performance
- **Duration:** ~18 min
- **Completed:** 2026-06-05
- **Tasks:** 3 (2 auto + 1 human-verify gate, approved)
- **Files modified:** 2

## Accomplishments
- `GetAllWorkersHistory` current-session projection now carries `a.Category` → `AllWorkersHistoryRow.Kategori` (additive; archived rows stay null — no Category column; no migration)
- `ExportRecordsTeamAssessment` narrows `filtered` by Category (case-insensitive) when Category active, dropping archived rows; when Category empty, archived rows appear normally
- Auth guard (`roleLevel >= 5 → Forbid`) + L4 section-lock + service `category: null` call preserved verbatim (grep-asserted)
- No regression to admin History tab no-arg caller (`AssessmentAdminController:308`) — Pitfall 3 avoided
- **Phase gate GREEN:** full `dotnet test` 109/109 (105 baseline + 4 new); Playwright `cmp-records-350.spec.ts` 2 passed (real browser SF-01/02/06); DB restored clean (Layer 4=0); user approved

## Task Commits
1. **Task 1: project a.Category → Kategori (additive)** — `fadc0799` (feat)
2. **Task 2: controller Category narrow + drop-archived** — `fc908d59` (feat)
3. **Task 3: phase gate UAT** — verification only; SEED_JOURNAL cleaned `6fb66951`; user approved

## Files Created/Modified
- `Services/WorkerDataService.cs` — `a.Category` added to current-session anon projection + `Kategori = a.Category` on row (training category filter + 3-caller behavior unchanged)
- `Controllers/CMPController.cs` — `if (!string.IsNullOrEmpty(category))` narrow after `var filtered = assessmentRows;` (auth/section-lock/Excel loop untouched)

## Decisions Made
- None beyond key-decisions — followed plan verbatim.

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
- Playwright project runs a global matrix seed (global.setup.ts) alongside the spec's own cmp350 beforeAll; both self-clean. Net effect: SEED_JOURNAL gained one self-cleaned matrix row (harness churn) — committed as-is. No impact on the test outcome (2 passed).

## User Setup Required
None.

## Next Phase Readiness
- Phase 350 complete pending phase verifier. Bundle v19-v23 NOT pushed (pending IT; flag migration = false).
- Phase 351 (Worker Detail + cross-surface) is sequential — shares WorkerDataService.cs (GetUnifiedRecords).

---
*Phase: 350-team-view-search-scope-export-parity*
*Completed: 2026-06-05*
