---
phase: 81-cleanup-remove-old-entry-points
plan: 02
subsystem: ui
tags: [razor, aspnet, admin, assessment, questions]

# Dependency graph
requires:
  - phase: 81-01
    provides: ManageAssessment dropdown cleaned up (Monitoring item removed), ready for Manage Questions addition
  - phase: 80-01
    provides: AssessmentMonitoring page and HC actions, establishing Admin-context assessment navigation
provides:
  - Admin-context ManageQuestions page (Views/Admin/ManageQuestions.cshtml) with proper breadcrumb and back link
  - Three AdminController actions: ManageQuestions GET, AddQuestion POST, DeleteQuestion POST
  - Manage Questions dropdown entry in ManageAssessment.cshtml
affects: [future-admin-assessment-features]

# Tech tracking
tech-stack:
  added: []
  patterns: [mirror CMP controller actions to AdminController for Admin-context equivalents]

key-files:
  created:
    - Views/Admin/ManageQuestions.cshtml
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/ManageAssessment.cshtml

key-decisions:
  - "Admin/ManageQuestions mirrors CMP/ManageQuestions logic but uses Admin controller context and Admin breadcrumb"
  - "Manage Questions dropdown item placed after Edit and before Export Excel in ManageAssessment dropdown"
  - "Back button on ManageQuestions returns to ManageAssessment (Admin controller), not CMP"

patterns-established:
  - "Admin breadcrumb pattern: Kelola Data > [section] > [page] for Admin-context pages"
  - "Mirror CMP controller actions in AdminController to provide same functionality without leaving Admin context"

requirements-completed: [CLN-01]

# Metrics
duration: 45min
completed: 2026-03-01
---

# Phase 81 Plan 02: Cleanup — Add Admin/ManageQuestions Summary

**Admin-context ManageQuestions page with 2-column layout wired to 3 new AdminController actions and accessible via ManageAssessment dropdown**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-03-01T10:30:00Z
- **Completed:** 2026-03-01T11:21:28Z
- **Tasks:** 3 auto + 1 checkpoint (approved)
- **Files modified:** 3

## Accomplishments

- Added ManageQuestions GET, AddQuestion POST, and DeleteQuestion POST to AdminController — exact mirror of CMPController logic, redirecting back to Admin context
- Created Views/Admin/ManageQuestions.cshtml with Admin breadcrumb (Kelola Data > Manage Assessment > Kelola Soal), 2-column layout (add form left, question list right), and back link to ManageAssessment
- Added "Manage Questions" dropdown item (bi-list-check icon) to ManageAssessment.cshtml per-group dropdown, positioned after Edit and before Export Excel

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ManageQuestions, AddQuestion, DeleteQuestion to AdminController** - `ed7e17e` (feat)
2. **Task 2: Create Views/Admin/ManageQuestions.cshtml** - `5febdaf` (feat)
3. **Task 3: Add Manage Questions dropdown item to ManageAssessment.cshtml** - `2317ba4` (feat)
4. **Task 4: checkpoint:human-verify** — approved by user

## Files Created/Modified

- `Controllers/AdminController.cs` — Added ManageQuestions GET, AddQuestion POST, DeleteQuestion POST actions under `#region Question Management (Admin)`
- `Views/Admin/ManageQuestions.cshtml` — New view: Admin breadcrumb, 2-column layout (add form + question list), forms pointing to Admin controller actions
- `Views/Admin/ManageAssessment.cshtml` — Added Manage Questions `<li>` item to per-group dropdown (after Edit, before Export Excel)

## Decisions Made

- Mirrored CMPController logic exactly in AdminController — only redirect targets changed to Admin context. No code duplication concern since the two controllers serve different user workflows (CMP vs Admin/HC).
- Manage Questions dropdown item placed second in dropdown (after Edit) — most likely action after navigating to an assessment group's options.
- Back button and breadcrumb both point to `ManageAssessment` (Admin controller) to keep users in the Admin flow.

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Phase 81 is now complete: both plans (81-01 and 81-02) are shipped
- v2.7 Assessment Monitoring milestone is complete: Phases 79, 80, and 81 all delivered
- No blockers for next milestone

---
*Phase: 81-cleanup-remove-old-entry-points*
*Completed: 2026-03-01*
