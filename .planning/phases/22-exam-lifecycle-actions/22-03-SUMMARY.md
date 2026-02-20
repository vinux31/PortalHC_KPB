---
phase: 22-exam-lifecycle-actions
plan: 03
subsystem: api
tags: [exam, timer, server-side-validation, csharp, assessment]

# Dependency graph
requires:
  - phase: 21-exam-state-foundation
    provides: StartedAt column on AssessmentSessions, InProgress status writes in StartExam GET
provides:
  - Server-side elapsed-time guard in SubmitExam POST (LIFE-03)
  - Submissions after DurationMinutes + 2 minutes are rejected without grading
affects: [22-04-force-close, 23-package-enforcement, future-audit-log]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Timer enforcement: server computes elapsed = UtcNow - StartedAt, rejects if > DurationMinutes + 2"
    - "Null-StartedAt skip: legacy sessions without StartedAt bypass the check and proceed to grading"
    - "No-mutation expiry: rejected submissions do not alter Status or trigger SaveChanges"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "Grace period fixed at +2 minutes — not configurable per-assessment in this phase, to avoid scope creep"
  - "Redirect on expiry targets StartExam (not Assessment lobby) so the worker sees the error on the exam page they came from"
  - "No Status mutation on expiry — session stays InProgress so HC can ForceClose later if needed (22-04)"
  - "Null-StartedAt sessions skip the check entirely — prevents blocking any legacy sessions that predate Phase 21"

patterns-established:
  - "LIFE-03 guard: insert server-side timer check after Completed idempotency guard, before package/legacy branch"

# Metrics
duration: 5min
completed: 2026-02-20
---

# Phase 22 Plan 03: Server-Side Elapsed-Time Enforcement Summary

**Server-side SubmitExam timer guard rejects late submissions after DurationMinutes + 2 minutes using UtcNow - StartedAt arithmetic, with no DB writes on the expiry path**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-20T13:51:13Z
- **Completed:** 2026-02-20T13:56:00Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Inserted LIFE-03 guard block in SubmitExam POST at line 2097 of CMPController.cs
- Guard activates only when `assessment.StartedAt.HasValue` (null = legacy session, passes through)
- Expired submissions redirect to `StartExam` with Indonesian error message "Waktu ujian Anda telah habis. Pengiriman jawaban tidak dapat diproses."
- Non-expired submissions fall through to existing package/legacy grading branch unchanged
- No SaveChangesAsync or Status mutation in the expiry path — session stays InProgress

## Task Commits

Each task was committed atomically:

1. **Task 1: Insert elapsed-time check in SubmitExam POST before grading** - `2838c07` (feat — included in 22-02 commit)

**Plan metadata:** _(docs commit — created with SUMMARY.md and STATE.md update)_

_Note: The LIFE-03 guard block was committed as part of the 22-02 AbandonExam commit (2838c07). The code was already present and correct when 22-03 execution began — the task was verified complete without requiring a new source commit._

## Files Created/Modified
- `Controllers/CMPController.cs` — SubmitExam POST now contains elapsed-time guard at line 2097 (committed in 2838c07)

## Decisions Made
- Grace period fixed at 2 minutes (not configurable) — keeps scope minimal; HC can adjust in a future phase if needed
- Redirect goes to `StartExam` (not `Assessment` lobby) — worker sees the error on the exam page, not the lobby
- No Status mutation on expiry — session stays `InProgress`; HC can use ForceClose (22-04) to clean up if needed
- Null-StartedAt sessions bypass the check entirely — defensive guard for legacy sessions predating Phase 21

## Deviations from Plan

None in terms of logic. One execution note: the LIFE-03 guard block was already present in `Controllers/CMPController.cs` when 22-03 execution began (it had been included in the 22-02 AbandonExam commit). All success criteria were verified as met without requiring a new source file commit.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- LIFE-03 (server-side timer enforcement) is complete and verified
- 22-04 (ForceClose action) can proceed — it uses the same `assessment.Status` field and follows the same pattern established by AbandonExam (22-02)
- SubmitExam grading logic is untouched — existing package and legacy grading paths continue to work normally

---
*Phase: 22-exam-lifecycle-actions*
*Completed: 2026-02-20*
