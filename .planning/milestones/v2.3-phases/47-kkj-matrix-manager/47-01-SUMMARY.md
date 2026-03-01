---
phase: 47-kkj-matrix-manager
plan: 01
subsystem: ui
tags: [admin, mvc, razor, bootstrap, identity, authorization]

# Dependency graph
requires: []
provides:
  - AdminController with class-level [Authorize(Roles="Admin")], Index GET, KkjMatrix GET
  - Views/Admin/Index.cshtml hub page with 12 tool cards in 3 category groups
  - Views/Admin/KkjMatrix.cshtml read-mode table (No/Indeks/Kompetensi/SkillGroup/Aksi columns)
  - Kelola Data nav link in _Layout.cshtml visible only to Admin role
affects: [47-02, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AdminController uses class-level [Authorize(Roles='Admin')] — no per-action auth needed"
    - "Admin views live in Views/Admin/ directory"
    - "Nav guard pattern: @if (userRole == 'Admin') block in _Layout.cshtml using pre-computed userRole variable"
    - "KkjMatrix view includes AntiForgeryToken at top for JS-based POST in Plan 02"
    - "Placeholder divs (editTable, editActions) left empty for Plan 02 to inject"

key-files:
  created:
    - Controllers/AdminController.cs
    - Views/Admin/Index.cshtml
    - Views/Admin/KkjMatrix.cshtml
  modified:
    - Views/Shared/_Layout.cshtml

key-decisions:
  - "AdminController injects only ApplicationDbContext, UserManager, AuditLogService — no SignInManager (not needed for read-only admin actions in Plan 01)"
  - "KkjMatrix view includes AntiForgeryToken hidden form at top so Plan 02 JS fetch calls can read the token without extra server round-trips"
  - "Delete button in read-mode calls deleteRow() stub — Plan 02 implements the function"
  - "Edit-mode table and action buttons are empty placeholder divs — Plan 02 injects full content"
  - "Kelola Data nav link uses userRole == 'Admin' string check, consistent with existing nav guards in _Layout.cshtml"

patterns-established:
  - "Pattern 1: All Admin/* pages protected at class level via [Authorize(Roles='Admin')] — zero risk of missing per-action auth"
  - "Pattern 2: Hub-page-first architecture — Admin/Index lists all tools before any tool is implemented"
  - "Pattern 3: Placeholder-div strategy — future plans inject into named empty divs rather than modifying the view"

requirements-completed: [MDAT-01]

# Metrics
duration: 3min
completed: 2026-02-26
---

# Phase 47 Plan 01: Admin Portal Infrastructure Summary

**AdminController with [Authorize(Roles="Admin")], 12-card hub page, KkjMatrix read-mode table, and Admin-only Kelola Data nav link establishing the structural foundation for all v2.3 Admin Portal phases**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-26T09:29:33Z
- **Completed:** 2026-02-26T09:32:18Z
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments
- AdminController.cs with class-level [Authorize(Roles="Admin")], Index GET, and KkjMatrix GET (queries KkjMatrices ordered by No)
- Views/Admin/Index.cshtml hub page with 12 tool cards in 3 category sections (Master Data, Operasional, Kelengkapan CRUD) — KKJ Matrix active, others marked "Segera"
- Views/Admin/KkjMatrix.cshtml read-mode table with No/Indeks/Kompetensi/SkillGroup/Aksi columns, sticky thead, custom scrollbar, and placeholder divs for Plan 02
- Kelola Data nav link in _Layout.cshtml rendered only when userRole == "Admin"

## Task Commits

Each task was committed atomically:

1. **Task 1: Create AdminController with Index and KkjMatrix GET actions** - `fd8b47b` (feat)
2. **Task 2: Create Admin/Index.cshtml hub page with 12 tool cards in 3 groups** - `5762f96` (feat)
3. **Task 3: Create KkjMatrix read-mode view and add Kelola Data nav link** - `2ac0f3a` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Controllers/AdminController.cs` - Admin-only controller with Index GET and KkjMatrix GET actions
- `Views/Admin/Index.cshtml` - Hub page listing 12 admin tools in 3 category sections
- `Views/Admin/KkjMatrix.cshtml` - Read-mode table for KkjMatrixItem rows with Plan 02 placeholders
- `Views/Shared/_Layout.cshtml` - Added Kelola Data nav link visible only for Admin role

## Decisions Made
- AdminController injects only ApplicationDbContext, UserManager, AuditLogService — no SignInManager (not needed for read-only admin actions in Plan 01)
- KkjMatrix view includes AntiForgeryToken at top so Plan 02 JS fetch calls can read the token without extra server round-trips
- Delete button in read-mode calls deleteRow() stub — Plan 02 implements the JS function
- Edit-mode table and action buttons are empty placeholder divs — Plan 02 injects full content
- Kelola Data nav link uses userRole == "Admin" string check, consistent with existing nav guards

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- AdminController.cs is ready for Plan 02 to add KkjMatrixSave (POST) and KkjMatrixDelete (POST) actions
- KkjMatrix.cshtml has named placeholder divs (editTable, editActions) and AntiForgeryToken in place
- Hub page infrastructure complete — all 12 planned admin tools stubbed with "Segera" badges

## Self-Check: PASSED

- Controllers/AdminController.cs: FOUND
- Views/Admin/Index.cshtml: FOUND
- Views/Admin/KkjMatrix.cshtml: FOUND
- .planning/phases/47-kkj-matrix-manager/47-01-SUMMARY.md: FOUND
- Commit fd8b47b (Task 1): FOUND
- Commit 5762f96 (Task 2): FOUND
- Commit 2ac0f3a (Task 3): FOUND

---
*Phase: 47-kkj-matrix-manager*
*Completed: 2026-02-26*
