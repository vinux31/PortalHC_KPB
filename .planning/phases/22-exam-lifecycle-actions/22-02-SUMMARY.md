---
phase: 22-exam-lifecycle-actions
plan: 02
subsystem: ui
tags: [csharp, razor, asp-net-core, assessment, exam]

# Dependency graph
requires:
  - phase: 22-01-exam-lifecycle-actions
    provides: StartExam GET action and InProgress status established in Phase 21

provides:
  - AbandonExam POST action in CMPController setting Status=Abandoned
  - Keluar Ujian button in StartExam.cshtml with Indonesian confirm() dialog
  - onbeforeunload bypass on confirmed abandon

affects:
  - 22-04-exam-lifecycle-actions (ResetExam action — HC resets Abandoned sessions for retake)
  - 23-token-enforcement (StartExam GET enforcement layer)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Idempotent status guard: only Abandon if status is InProgress or Open"
    - "Bypass onbeforeunload before hidden form submit to avoid double-prompt"
    - "Hidden form POST pattern for destructive actions from exam UI"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/StartExam.cshtml

key-decisions:
  - "StartedAt is NOT cleared on Abandon — HC audit requires knowing when exam started; Reset (Plan 22-04) clears it for retake"
  - "No [Authorize(Roles)] on AbandonExam — worker-facing; enforced via assessment.UserId != user.Id ownership check"
  - "confirm() in confirmAbandon() fires before onbeforeunload is cleared — correct order avoids double-prompt"

patterns-established:
  - "Abandon/lifecycle actions: idempotent guard on Status before mutating, preserve audit fields (StartedAt)"

# Metrics
duration: 8min
completed: 2026-02-20
---

# Phase 22 Plan 02: Keluar Ujian (Abandon Exam) Summary

**AbandonExam POST action with worker-owned auth guard + Keluar Ujian button in exam header using confirm() dialog and onbeforeunload bypass**

## Performance

- **Duration:** ~8 min
- **Started:** 2026-02-20T00:00:00Z
- **Completed:** 2026-02-20T00:08:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- AbandonExam POST in CMPController: sets Status="Abandoned", preserves StartedAt, redirects to Assessment lobby with Indonesian TempData["Info"] message
- Keluar Ujian danger button inserted in sticky exam header between progress span and timer
- Hidden #abandonForm POSTs to /CMP/AbandonExam with CSRF token and session id
- confirmAbandon() JS: shows Indonesian confirm() dialog; on confirm clears onbeforeunload then submits form; on dismiss exam continues unaffected

## Task Commits

Each task was committed atomically:

1. **Task 1: Add AbandonExam POST action to CMPController** - `2838c07` (feat)
2. **Task 2: Add Keluar Ujian button and abandon form to StartExam view** - `b496edc` (feat)

**Plan metadata:** (docs commit — this summary)

## Files Created/Modified
- `Controllers/CMPController.cs` - AbandonExam POST action added after StartExam GET (~line 1708)
- `Views/CMP/StartExam.cshtml` - Keluar Ujian button in header, hidden abandonForm, confirmAbandon() JS function

## Decisions Made
- StartedAt preserved on Abandon: HC needs audit trail; the Reset action (Plan 22-04) will clear it for retakes
- No role-based authorization on AbandonExam — enforced by ownership check (assessment.UserId != user.Id), consistent with StartExam GET pattern
- confirm() runs BEFORE onbeforeunload is cleared, avoiding the double-prompt bug described in the plan

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- LIFE-02 (sanctioned exit with Abandoned status) complete
- LIFE-03 (ForceClose — HC closes exam window, sets ExamWindowCloseDate) is Plan 22-03, independent of this plan
- LIFE-04 (Reset — HC resets Abandoned/Completed for retake) is Plan 22-04 and depends on Abandoned status established here

---
*Phase: 22-exam-lifecycle-actions*
*Completed: 2026-02-20*
