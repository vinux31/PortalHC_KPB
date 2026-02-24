---
phase: 39-close-early
plan: 02
subsystem: ui
tags: [csharp, aspnet-mvc, razor, javascript, ajax, polling, packageuserresponse]

# Dependency graph
requires:
  - phase: 39-01
    provides: CloseEarly POST action in CMPController that scores InProgress sessions and locks all sessions via ExamWindowCloseDate

provides:
  - Submit Assessment (was "Tutup Lebih Awal") btn-warning button in AssessmentMonitoringDetail header, visible only for Open groups
  - Bootstrap modal id=closeEarlyModal with bg-warning header, Indonesian 3-bullet warning body, POST form to CloseEarly
  - SaveAnswer POST endpoint in CMPController — upserts PackageUserResponse on each radio selection (session owner only)
  - CheckExamStatus GET endpoint in CMPController — returns {closed, redirectUrl} for session owner
  - StartExam.cshtml incremental answer saving via fire-and-forget fetch on radio change
  - StartExam.cshtml 30-second polling of CheckExamStatus with banner notification and redirect on early close
  - SubmitExam package path changed from Add to upsert for PackageUserResponse (handles pre-saved records)

affects: [phase-40-history-tab]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Incremental answer save: fire-and-forget fetch POST to SaveAnswer on radio change; SubmitExam upserts to handle pre-existing records"
    - "Status polling: setInterval(checkExamStatus, 30000) in StartExam.cshtml; clearInterval on close detection"
    - "Worker notification: fixed banner div injected into body + 3s timeout before redirect"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/AssessmentMonitoringDetail.cshtml
    - Views/CMP/StartExam.cshtml

key-decisions:
  - "SaveAnswer endpoint uses FirstOrDefaultAsync + upsert pattern (not AddOrUpdate) — safe against EF tracking conflicts"
  - "SaveAnswer authorized to session owner only (no [Authorize(Roles)] attribute — uses explicit UserId check returning Json error)"
  - "CheckExamStatus GET — no [ValidateAntiForgeryToken] needed (read-only, no state change)"
  - "SubmitExam upsert: check FirstOrDefaultAsync for each question before Add — handles partial SaveAnswer coverage (worker may not have saved all questions before submit)"
  - "Button/modal text: 'Tutup Lebih Awal' renamed to 'Submit Assessment'; modal id=closeEarlyModal kept unchanged (no JS references to change)"
  - "30s poll interval chosen: short enough to notify within 30s of early close, long enough to avoid server load during active exam"

patterns-established:
  - "Incremental save pattern: fire-and-forget fetch + upsert in both SaveAnswer and SubmitExam endpoints"
  - "Status polling pattern: setInterval + clearInterval on terminal condition + banner notification before redirect"

# Metrics
duration: ~25min
completed: 2026-02-24
---

# Phase 39 Plan 02: Close Early Frontend + Fix 3 Verification Issues Summary

**Submit Assessment modal + incremental answer saving + 30s status polling — CloseEarly now works correctly for InProgress workers who answered but didn't submit**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-02-24
- **Completed:** 2026-02-24
- **Tasks:** 2 (Task 1 from prior session + Task 2 = 3 fixes from verification)
- **Files modified:** 3

## Accomplishments

- Added "Submit Assessment" button (was "Tutup Lebih Awal") + Bootstrap confirmation modal to AssessmentMonitoringDetail, visible only for Open groups
- Fixed Score=0 bug: SaveAnswer endpoint upserts PackageUserResponse on each radio selection; SubmitExam changed to upsert so pre-saved records are respected
- Added CheckExamStatus polling in StartExam.cshtml so workers see a banner and get redirected within 30 seconds of HC triggering Submit Assessment

## Task Commits

Each task was committed atomically:

1. **Task 1: Add "Tutup Lebih Awal" button and Bootstrap modal** - `016afc3` (feat) — prior session
2. **Task 2: Fix 3 issues found during verification** - `f127022` (fix)

**Plan metadata:** (this commit)

## Files Created/Modified

- `Controllers/CMPController.cs` - Added SaveAnswer POST endpoint (upsert PackageUserResponse), CheckExamStatus GET endpoint; modified SubmitExam package path from Add to upsert
- `Views/CMP/AssessmentMonitoringDetail.cshtml` - Added Submit Assessment button trigger + Bootstrap modal with CloseEarly POST form; renamed all occurrences from "Tutup Lebih Awal" to "Submit Assessment"
- `Views/CMP/StartExam.cshtml` - Added saveAnswerAsync (fire-and-forget fetch on radio change), added 30s CheckExamStatus poll with banner notification and redirect

## Decisions Made

- SaveAnswer uses explicit `if (session.UserId != user.Id) return Json({success:false})` rather than `[Authorize(Roles)]` — workers don't have HC/Admin roles, and the session-owner check is sufficient
- CheckExamStatus is a plain GET with no antiforgery requirement — it's read-only and returns JSON for JS consumption
- SubmitExam upsert checks each question individually (FirstOrDefaultAsync in the loop) rather than loading all pre-existing records upfront — acceptable for exam sizes; keeps the fix minimal and localized
- Poll interval of 30 seconds — balances responsiveness (worker notified within 30s) with server load during active exam sessions
- Banner notification injected as fixed-position div before redirect (not Bootstrap modal) — simpler, no Bootstrap JS dependency in the notification path

## Deviations from Plan

### Human Verification Issues (continuation from checkpoint)

The plan's original Task 2 was a human-verify checkpoint. The user found 3 issues during testing that were fixed as continuation work:

**1. [Rule 1 - Bug] Score=0 despite workers having answered questions**
- **Found during:** Human verification of CloseEarly
- **Issue:** CloseEarly reads PackageUserResponse records, but SubmitExam was the ONLY place that wrote them (on final submit). Workers who were InProgress with answered questions had zero records in DB, so CloseEarly scored them 0.
- **Fix:** Added SaveAnswer POST endpoint to upsert PackageUserResponse on each radio change; added fire-and-forget fetch call in StartExam.cshtml radio event listener; changed SubmitExam package path from `Add` to upsert to handle pre-existing records
- **Files modified:** Controllers/CMPController.cs, Views/CMP/StartExam.cshtml
- **Committed in:** f127022

**2. [Rule 1 - Bug] Worker browser not notified of early close**
- **Found during:** Human verification of CloseEarly
- **Issue:** Standard web app with no WebSocket/SSE. Workers only saw the exam close on manual reload or next navigation.
- **Fix:** Added CheckExamStatus GET endpoint returning `{closed, redirectUrl}`; added setInterval(checkExamStatus, 30000) in StartExam.cshtml that shows a banner and redirects after 3 seconds when closed=true
- **Files modified:** Controllers/CMPController.cs, Views/CMP/StartExam.cshtml
- **Committed in:** f127022

**3. [Simple text change] Rename "Tutup Lebih Awal" to "Submit Assessment"**
- **Found during:** Human verification feedback
- **Fix:** Three text replacements in AssessmentMonitoringDetail.cshtml: trigger button text, modal title, modal confirm button text
- **Files modified:** Views/CMP/AssessmentMonitoringDetail.cshtml
- **Committed in:** f127022

---

**Total deviations:** 3 issues fixed (all from human verification, continuation after checkpoint)
**Impact on plan:** All 3 fixes required for correctness and usability. No scope creep.

## Issues Encountered

None beyond the 3 documented user issues.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 39 CloseEarly feature complete end-to-end: backend scoring logic (39-01) + frontend button/modal + incremental save + worker notification (39-02)
- Phase 40 (History tab) can proceed: depends on AssessmentSession data being correct, which is now properly maintained by CloseEarly

---
*Phase: 39-close-early*
*Completed: 2026-02-24*

## Self-Check: PASSED

- [x] `Controllers/CMPController.cs` — modified, contains SaveAnswer and CheckExamStatus endpoints
- [x] `Views/CMP/AssessmentMonitoringDetail.cshtml` — modified, contains "Submit Assessment" text
- [x] `Views/CMP/StartExam.cshtml` — modified, contains saveAnswerAsync and checkExamStatus polling
- [x] Commit f127022 — exists (fix: 3 issues found during verification)
- [x] Commit 016afc3 — exists (Task 1 from prior session)
- [x] dotnet build: 0 errors
