---
phase: 14-bulk-assign
plan: "01"
subsystem: ui
tags: [assessment, bulk-assign, cmp, editassessment, viewbag, razor, javascript]

# Dependency graph
requires:
  - phase: 13-nav-and-create
    provides: Clean CMP manage view baseline, functional EditAssessment page, CreateAssessment POST redirect to manage view

provides:
  - EditAssessment GET loads sibling sessions and exposes ViewBag.AssignedUsers, ViewBag.AssignedUserIds, ViewBag.Sections
  - EditAssessment POST accepts NewUserIds and bulk-creates new AssessmentSessions with identical settings
  - EditAssessment view shows currently assigned users table and filterable multi-select picker for adding more users

affects: [15-quick-edit, any phase touching EditAssessment or AssessmentSession bulk creation]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Sibling session query: Title+Category+Schedule.Date match to find all users on a batch assessment"
    - "Bulk AddRange + BeginTransactionAsync pattern (matches CreateAssessment POST) for atomic multi-session creation"
    - "ViewBag.AssignedUserIds exclusion list: populated in GET, used in Razor to skip already-assigned users from picker render"
    - "Scoped JS picker with unique element IDs (newSectionFilter, newUserCheckboxContainer, new-user-check-item class) to avoid conflicts with existing form elements"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/EditAssessment.cshtml

key-decisions:
  - "Sibling matching uses Title+Category+Schedule.Date (not just Title) — same pattern used in CreateAssessment duplicate check"
  - "Rate limit of 50 users applied at POST entry before any DB work, matching CreateAssessment behavior"
  - "Already-assigned users are excluded at Razor render time (not JS) — simplest approach, avoids client-side toggling errors"
  - "Bulk assign runs AFTER the existing field-update save — two separate DB operations to keep rollback scope tight"
  - "Success message updated only when new sessions are actually created; if all selected users were already assigned, existing update message stands"

patterns-established:
  - "NewUserIds pattern: optional List<string> param on existing POST action — no new action needed"
  - "Transaction wraps only the AddRange save, not the preceding Update save"

# Metrics
duration: ~25min
completed: 2026-02-19
---

# Phase 14 Plan 01: Bulk Assign Summary

**Bulk user assignment on EditAssessment page: sibling session display + multi-select picker + transaction-backed session creation without altering existing sessions**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-02-19T11:26:40Z
- **Completed:** 2026-02-19 (post human-verify approval)
- **Tasks:** 3 (2 auto + 1 human-verify)
- **Files modified:** 2

## Accomplishments

- EditAssessment GET now queries sibling sessions (Title+Category+Schedule.Date) and exposes four new ViewBag values: AssignedUsers (display list), AssignedUserIds (exclusion list), Users (all users for picker), Sections (filter dropdown)
- EditAssessment POST accepts optional `List<string> NewUserIds`; after updating existing session fields, bulk-creates new AssessmentSessions copying all settings (Title, Category, Schedule, DurationMinutes, Status, BannerColor, IsTokenRequired, AccessToken, PassPercentage, AllowAnswerReview), wrapped in a transaction; success message includes assigned count
- EditAssessment view adds two new cards above form actions: "Currently Assigned Users" (read-only scrollable table with count badge) and "Add More Users" (section filter + text search + Select All/Deselect All + scrollable checkbox list); checkboxes submit as `NewUserIds[]` inside the existing form; JS handles combined filtering, bulk select, and count badge
- BLK-01 and BLK-02 satisfied: HC can see who is assigned and add more users without recreating assessments from scratch

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend EditAssessment controller for bulk assign** - `a0490e7` (feat)
2. **Task 2: Add assigned users list and user picker to EditAssessment view** - `f0a2932` (feat)
3. **Task 3: Verify bulk assign flow end-to-end** - human-verify approved (no code commit)

**Plan metadata:** see final docs commit below

## Files Created/Modified

- `Controllers/CMPController.cs` — EditAssessment GET: sibling query + 4 ViewBag values; EditAssessment POST: NewUserIds param, rate limit, duplicate exclusion, AddRange + transaction, updated success message
- `Views/CMP/EditAssessment.cshtml` — "Currently Assigned Users" card (table + badge), "Add More Users" card (section filter, search, checkboxes with name="NewUserIds"), JS picker logic in @section Scripts

## Decisions Made

- Sibling matching uses Title+Category+Schedule.Date — consistent with the existing duplicate-check query in CreateAssessment POST
- Rate limit of 50 users at POST entry (before DB queries), matching CreateAssessment's `ModelState.AddModelError` guard pattern; adapted to TempData["Error"] + redirect since EditAssessment POST doesn't re-render the form on error
- Already-assigned users excluded at Razor render time using `@if (assignedUserIds.Contains((string)user.Id)) { continue; }` — avoids needing JS to hide/disable checkboxes post-render
- Bulk assign block runs after the existing field-update SaveChangesAsync — keeps each operation's rollback scope minimal; if field update succeeds but bulk assign fails, the update is preserved and an error is shown
- NewUserIds is optional (null/empty = no bulk assign) — backward compatible, existing edit flow unchanged when no new users are selected

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

The running app process held `HcPortal.exe` during `dotnet build`, producing MSB3021/MSB3027 file-lock warnings. These are deployment-time copy errors, not compilation errors. No C# or Razor compilation errors were present (`grep -E "error CS"` returned empty).

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Phase 14 complete. BLK-01 and BLK-02 satisfied.
- Phase 15 (Quick Edit) is ready to begin: new `QuickEdit` action on CMPController, inline modal on manage view for status + schedule only.
- No blockers. CMPController is now ~1180 lines; acceptable.

---
*Phase: 14-bulk-assign*
*Completed: 2026-02-19*
