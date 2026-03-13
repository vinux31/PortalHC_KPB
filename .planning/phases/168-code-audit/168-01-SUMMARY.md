---
phase: 168-code-audit
plan: "01"
subsystem: controllers
tags: [dead-code, audit, cleanup]
dependency_graph:
  requires: []
  provides: [CODE-01, CODE-04]
  affects: [Controllers/AdminController.cs, Controllers/CDPController.cs]
tech_stack:
  added: []
  patterns: []
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Controllers/CDPController.cs
decisions:
  - "CleanupDuplicateAssignments removed — POST-only utility with no UI link; deduplication logic remains in SeedData and can be triggered programmatically if needed"
  - "CDPController.SearchUsers removed — comment confirmed it was for a ReportsIndex autocomplete page that was never built in main codebase"
  - "All 53 non-shared views verified to have corresponding controller actions — zero orphaned views found"
metrics:
  duration: "~5 minutes"
  completed: "2026-03-13"
  tasks_completed: 2
  files_modified: 2
---

# Phase 168 Plan 01: Dead Code Audit Summary

**One-liner:** Removed 2 dead controller actions (CleanupDuplicateAssignments, SearchUsers) with no reachable callers; confirmed all 53 views have corresponding actions.

## Tasks Completed

| # | Task | Commit | Status |
|---|------|--------|--------|
| 1 | Audit and remove dead controller actions and unused helpers | 862deb6 | Done |
| 2 | Audit and remove orphaned views | — | Done (no removals needed) |

## Findings

### Dead Controller Actions Removed

**1. `AdminController.CleanupDuplicateAssignments` (line 5842)**
- POST-only action with no form, link, or JavaScript reference anywhere in the codebase
- Purpose: manual one-time deduplication utility from early development
- The underlying logic (`SeedData.DeduplicateProtonTrackAssignments`) remains intact
- Commit: 862deb6

**2. `CDPController.SearchUsers` (line 588)**
- GET AJAX endpoint for user search autocomplete
- Comment in code states: "only used by ReportsIndex autocomplete"
- ReportsIndex view/page does not exist in the main codebase (only in a `.claude/worktrees/` artifact)
- No references in any `.cshtml` or `.js` file in the main codebase
- Commit: 862deb6

### Orphaned Views

None found. All 53 non-shared views verified:
- **Account (4):** AccessDenied, Login, Profile, Settings — all have controller actions
- **Admin (26):** All 26 views have corresponding AdminController actions
- **CDP (9):** CoachingProton, Dashboard, Deliverable, HistoriProton, HistoriProtonDetail, Index, PlanIdp + 2 Shared partials — all covered
- **CMP (11):** Assessment, Certificate, ExamSummary, Index, Kkj, Mapping, Records, RecordsTeam, RecordsWorkerDetail, Results, StartExam — all covered
- **Home (3):** Guide, GuideDetail, Index — all covered
- **ProtonData (2):** Index, Override — both covered

### Shared Views

- `Views/Shared/Error.cshtml` — used by `HomeController.Error` (convention lookup)
- `Views/Shared/_Layout.cshtml` — referenced in `_ViewStart.cshtml`
- `Views/Shared/_PSign.cshtml` — partial used in assessment views
- `Views/Shared/_ValidationScriptsPartial.cshtml` — referenced in form views
- `Views/Shared/Components/NotificationBell/Default.cshtml` — invoked via `Component.InvokeAsync` in `_Layout.cshtml`

### Helper Methods Audited

All private/helper methods verified to have callers:
- `HomeController`: `GetUpcomingEvents`, `GetProgress`, `GetTimeBasedGreeting` — called from `Index`
- `CMPController`: All 9 private helpers have internal callers
- `CDPController`: All 5 private helpers have internal callers
- `AdminController`: All private helpers have internal callers
- `NotificationController`: `FormatRelativeTime` — called from `List` action
- `ProtonDataController`: No private helpers

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check

- [x] Dead code removed: 2 actions deleted
- [x] Build passes: 0 errors, 69 warnings (pre-existing CA1416 platform warnings)
- [x] Commits exist: 862deb6
- [x] No orphaned views deleted (none found)

## Self-Check: PASSED
