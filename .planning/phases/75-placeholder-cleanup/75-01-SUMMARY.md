---
phase: 75-placeholder-cleanup
plan: 01
subsystem: navigation, controllers, views
tags: [stub-removal, dead-code, bp, privacy, cleanup]
dependency_graph:
  requires: []
  provides: [STUB-01, STUB-05]
  affects: [Views/Shared/_Layout.cshtml, Controllers/HomeController.cs]
tech_stack:
  added: []
  patterns: [file-deletion, navbar-cleanup]
key_files:
  modified:
    - Views/Shared/_Layout.cshtml
    - Controllers/HomeController.cs
  deleted:
    - Controllers/BPController.cs
    - Views/BP/Index.cshtml
    - Views/Home/Privacy.cshtml
decisions:
  - "Removed entire Views/BP/ directory after deleting Index.cshtml — directory had no other files"
  - "Removed Privacy() blank line separator above [ResponseCache] to keep consistent spacing in HomeController"
metrics:
  duration_seconds: 77
  completed_date: "2026-03-01"
  tasks_completed: 2
  tasks_total: 2
  files_modified: 2
  files_deleted: 3
requirements_satisfied:
  - STUB-01
  - STUB-05
---

# Phase 75 Plan 01: Placeholder Cleanup (BP + Privacy) Summary

**One-liner:** Removed BP stub module (navbar link, controller, view) and Privacy placeholder page (action + view) — five artifacts deleted, build passes with 0 errors.

## What Was Done

Executed two targeted deletion tasks to remove dead UI endpoints that users could accidentally navigate to but that had no backing functionality.

**Task 1 — BP Infrastructure Removal (commit b13c756):**
- Deleted the three-line BP nav-item block from `Views/Shared/_Layout.cshtml` — no user can navigate to /BP/Index via the navbar
- Deleted `Controllers/BPController.cs` (stub controller for out-of-scope Best People module)
- Deleted `Views/BP/Index.cshtml` and removed the now-empty `Views/BP/` directory

**Task 2 — Privacy Placeholder Removal (commit 6f92218):**
- Removed `HomeController.Privacy()` action from `Controllers/HomeController.cs` — /Home/Privacy now returns 404
- Deleted `Views/Home/Privacy.cshtml` (unmodified ASP.NET template default, no portal content)

## Verification Results

All 8 plan verification checks passed:
1. Build: 0 errors (56 pre-existing platform warnings)
2. `grep -r "asp-controller=\"BP\"" Views/` — no output (PASS)
3. `Controllers/BPController.cs` — absent (PASS)
4. `Views/BP/Index.cshtml` — absent (PASS)
5. `grep "public IActionResult Privacy" Controllers/HomeController.cs` — no output (PASS)
6. `Views/Home/Privacy.cshtml` — absent (PASS)
7. `Views/Home/Index.cshtml` — intact (PASS)
8. HomeController retains Index() and Error() (PASS)

## Commits

| Task | Commit | Message |
|------|--------|---------|
| 1 | b13c756 | feat(75-01): remove BP stub infrastructure |
| 2 | 6f92218 | feat(75-01): remove Privacy placeholder page |

## Deviations from Plan

None — plan executed exactly as written.

## Requirements Satisfied

- **STUB-01:** BP navbar link removed from _Layout.cshtml, BPController.cs deleted, Views/BP/Index.cshtml deleted — no BP infrastructure remains
- **STUB-05:** HomeController.Privacy() action removed, Views/Home/Privacy.cshtml deleted — /Home/Privacy returns 404

## Self-Check: PASSED

All created/modified files confirmed present. All deleted files confirmed absent. Both commits (b13c756, 6f92218) verified in git log.
