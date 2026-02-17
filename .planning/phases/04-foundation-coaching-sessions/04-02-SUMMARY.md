---
phase: 04-foundation-coaching-sessions
plan: 02
subsystem: ui
tags: [asp-net-core, razor, ef-core, coaching, crud, viewmodel]

# Dependency graph
requires:
  - phase: 04-foundation-coaching-sessions/04-01
    provides: CoachingSession, ActionItem entities, CoachingHistoryViewModel, CreateSessionViewModel, AddActionItemViewModel, EF migration with tables

provides:
  - CDPController.Coaching() GET with fromDate/toDate/status filters returning CoachingHistoryViewModel
  - CDPController.CreateSession() POST (COACH-01): creates coaching session with role check
  - CDPController.AddActionItem() POST (COACH-02): adds action item to session
  - Coaching.cshtml: real form-backed view with summary cards, filter bar, session history, create modal, inline action item forms
  - Batch user name resolution via ViewBag.UserNames dictionary (avoids N+1)
  - Role-based session visibility (coach sees coached sessions, coachee sees theirs, Admin respects SelectedView)

affects: [04-03, phase5, phase6]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ModelState.Remove('CoachId') for server-set fields before validation"
    - "Bootstrap collapse for inline action item forms (no JavaScript needed)"
    - "Batch user name resolution via ToDictionaryAsync before rendering session list"
    - "ViewBag.IsCoach gate for all create/add UI elements (modal button, inline form, empty state CTA)"

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/Coaching.cshtml

key-decisions:
  - "User name dictionary built in controller via batch query — avoids N+1 reads per session card"
  - "Role check for CreateSession uses RoleLevel > 5 (Coachee-only check) — no separate Authorize attribute to keep consistent with existing CDPController pattern"
  - "Selected option in Razor status dropdown uses if/else block — tag helper does not support C# in attribute declaration area (RZ1031 error)"

patterns-established:
  - "Filter bar collapsible with Bootstrap collapse show by default, collapsed when filters active"
  - "Inline action item form per session card using Bootstrap collapse, no JavaScript"

# Metrics
duration: 3min
completed: 2026-02-17
---

# Phase 4 Plan 02: Coaching Controller and View Summary

**CDPController GET/POST actions for coaching sessions with role-based filtering, and Coaching.cshtml replaced with real CoachingHistoryViewModel-backed view including create modal, filter bar, and inline action item forms**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-17T04:51:49Z
- **Completed:** 2026-02-17T04:54:40Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- Rewrote Coaching() GET with fromDate/toDate/status filters, role-based session visibility, coachee list population, and batch user name resolution
- Added CreateSession POST (COACH-01): validates role level, creates CoachingSession, TempData success/error
- Added AddActionItem POST (COACH-02): verifies session ownership (CoachId == user.Id), creates ActionItem, TempData success/error
- Replaced Coaching.cshtml stub (List<CoachingLog> model) with full form-backed view using CoachingHistoryViewModel
- View includes: summary cards (TotalSessions, SubmittedSessions, TotalActionItems/OpenActionItems), collapsible filter bar, session history cards with action item tables, create session modal, inline action item collapse forms
- All existing CDP methods (Index, PlanIdp, Dashboard, Progress) unchanged — zero regression

## Task Commits

Each task was committed atomically:

1. **Task 1: Update CDPController with Coaching GET filters and POST actions** - `8c00072` (feat)
2. **Task 2: Replace Coaching.cshtml with real form-backed view** - `c34bea7` (feat)
3. **Task 3: User name resolution** - included in Task 1 commit `8c00072` (Coaching GET already contained the batch name query)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `Controllers/CDPController.cs` - Rewrote Coaching() GET (filter params, role logic, coachee list, user names dict); added CreateSession POST and AddActionItem POST
- `Views/CDP/Coaching.cshtml` - Replaced stub with full CoachingHistoryViewModel view: summary cards, filter bar, session history, create session modal, inline action item forms, TempData alerts, empty state

## Decisions Made
- User name dictionary built via ToDictionaryAsync batch query in controller — one query for all users in the session list, not one per session card (N+1 avoided)
- Role check for CreateSession uses `user.RoleLevel > 5` (Forbid if Coachee-only) — consistent with existing CDPController pattern, no separate Authorize attribute needed
- Razor tag helper `<option>` does not accept C# in attribute declaration (RZ1031 error) — fixed by using if/else block to render the correct option with `selected` attribute

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Razor RZ1031 error on status dropdown**
- **Found during:** Task 2 (Coaching.cshtml first build)
- **Issue:** `@(Model.StatusFilter == "Draft" ? "selected" : "")` in `<option>` attribute causes RZ1031 — tag helper does not allow C# in attribute declaration area
- **Fix:** Replaced with `@if/else if/else` blocks to render each option conditionally with/without `selected` attribute
- **Files modified:** Views/CDP/Coaching.cshtml
- **Verification:** `dotnet build -c Release` passes with 0 errors
- **Committed in:** c34bea7 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug — Razor attribute syntax)
**Impact on plan:** One-line fix to dropdown rendering. No scope creep.

## Issues Encountered

- Razor `<option selected="@bool">` and `<option @(condition ? "selected" : "")>` both cause RZ1031 in ASP.NET Core 8.0 with tag helpers enabled. Resolved with explicit `@if/@else` blocks — standard Razor pattern for conditional HTML attributes on tag-helper elements.

## User Setup Required

None - no external service configuration required.

## Self-Check: PASSED

- Controllers/CDPController.cs: FOUND
- Views/CDP/Coaching.cshtml: FOUND
- Coaching() GET with filter params (fromDate, toDate, status): FOUND (line 180)
- CreateSession POST method: FOUND (line 295)
- AddActionItem POST method: FOUND (line 333)
- ViewBag.UserNames (batch query): FOUND (line 274-288)
- @model CoachingHistoryViewModel in view: FOUND
- Commit 8c00072: FOUND
- Commit c34bea7: FOUND
- dotnet build -c Release: 0 errors

## Next Phase Readiness
- COACH-01 (log sessions): complete — coach can create session with coachee, date, topic, notes
- COACH-02 (add action items): complete — coach can add action items with description and due date
- COACH-03 (view history with filtering): complete — date range and status filters functional
- All existing v1.0 CDP pages verified intact (Index, PlanIdp, Dashboard, Progress)
- Ready for 04-03: any additional coaching session requirements in remaining Phase 4 plans

---
*Phase: 04-foundation-coaching-sessions*
*Completed: 2026-02-17*
