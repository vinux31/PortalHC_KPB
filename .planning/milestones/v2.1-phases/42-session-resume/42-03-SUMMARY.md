---
phase: 42-session-resume
plan: 03
subsystem: ui
tags: [razor, bootstrap, javascript, session-resume, assessment]

# Dependency graph
requires:
  - phase: 42-session-resume-02
    provides: UpdateSessionProgress endpoint + StartExam GET ViewBag flags (IsResume, LastActivePage, ElapsedSeconds, RemainingSeconds, ExamExpired, SavedAnswers)
provides:
  - btn-warning Resume button on assignment cards for InProgress sessions
  - resumeConfirmModal in StartExam with single Lanjutkan button
  - Timer initialized from REMAINING_SECONDS_FROM_DB (server-calculated)
  - prePopulateAnswers() pre-checks all saved radio buttons before render
  - showResumeFailureToast() for IS_RESUME sessions with no SAVED_ANSWERS
  - saveSessionProgress() periodic 30s POST + on-navigation POST to UpdateSessionProgress
  - examExpiredModal with auto-submit when EXAM_EXPIRED is true
affects: [42-session-resume, 43-exam-status-polling, 44-monitoring]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Resume ViewBag constants deserialized as JS const at top of script block before state variables"
    - "IS_RESUME guard at top of prePopulateAnswers: returns immediately for fresh sessions, no work done"
    - "Bootstrap 5 bootstrap.Toast() constructed in JS for dynamic warning messages"
    - "Init block: prePopulateAnswers first, then EXAM_EXPIRED, then IS_RESUME modal, then normal path"
    - "saveSessionProgress silent catch: console.warn only, no UI impact on 30s poll failure"

key-files:
  created: []
  modified:
    - Views/CMP/Assessment.cshtml
    - Views/CMP/StartExam.cshtml

key-decisions:
  - "btn-warning (yellow) for Resume button per user decision for visual distinction from btn-primary Start Assessment"
  - "a tag with asp-action/asp-route-id for Resume (not JS button): direct navigation to StartExam; modal appears on load"
  - "RESUME_PAGE > 0 gates resume modal: page 0 resumes silently (worker was on first page, modal not needed)"
  - "prePopulateAnswers runs before modal: answeredQuestions Set and answered count badge correct before any user interaction"
  - "Toast message 'Gagal memuat jawaban sebelumnya. Lanjutkan dari soal no. X.' where X = RESUME_PAGE + 1 (1-based) per locked user decision"
  - "elapsedSeconds++ in updateTimer keeps local count current between 30s saves; ELAPSED_SECONDS_FROM_DB seeds it on load"
  - "window.onbeforeunload = null before any auto-submit (timer expiry or EXAM_EXPIRED) to prevent browser leave-page dialog"
  - "EXAM_EXPIRED auto-submit: OK button submits immediately, setTimeout 5000ms fires as fallback if worker ignores modal"

patterns-established:
  - "Resume frontend pattern: constants from ViewBag -> pre-populate -> modal gate -> navigate to page"

# Metrics
duration: 2min
completed: 2026-02-24
---

# Phase 42 Plan 03: Session Resume Frontend Summary

**Resume button (btn-warning) on assignment cards + StartExam resume modal, timer seeded from server remaining time, answer pre-population with failure toast, periodic 30s + on-navigation progress save, and expired-exam auto-submit path**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-24T11:29:04Z
- **Completed:** 2026-02-24T11:31:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- InProgress sessions show a yellow "Resume" button (btn-warning anchor with bi-play-circle-fill) on the assignment card, bypassing token re-check; statusBadgeClass and statusIcon switches updated for InProgress
- StartExam.cshtml: resumeConfirmModal ("Ada ujian yang belum selesai — Lanjutkan") and examExpiredModal ("Waktu Assessment Habis") HTML added; single Lanjutkan button navigates to LastActivePage
- Timer initializes from REMAINING_SECONDS_FROM_DB (not full DURATION_SECONDS); elapsedSeconds tracked locally and incremented every tick; alert() removed, replaced with silent auto-submit and window.onbeforeunload = null guard
- prePopulateAnswers() pre-checks all radios from SAVED_ANSWERS before any modal or render; shows failure toast "Gagal memuat jawaban sebelumnya. Lanjutkan dari soal no. X." when IS_RESUME is true but SAVED_ANSWERS is empty
- saveSessionProgress() POSTs sessionId + elapsedSeconds + currentPage to UpdateSessionProgress every 30s via setInterval and on every Prev/Next navigation via performPageSwitch

## Task Commits

Each task was committed atomically:

1. **Task 1: Assessment.cshtml — Resume button for InProgress sessions** - `203841d` (feat)
2. **Task 2: StartExam.cshtml — Resume modal, timer init, answer pre-population, periodic save, expired path** - `7aea75d` (feat)

## Files Created/Modified
- `Views/CMP/Assessment.cshtml` - Added InProgress branch with btn-warning Resume anchor tag; added InProgress to statusBadgeClass and statusIcon switches
- `Views/CMP/StartExam.cshtml` - Added resumeConfirmModal + examExpiredModal HTML; resume constants block; timer fix; prePopulateAnswers + showResumeFailureToast functions; saveSessionProgress with 30s setInterval; saveSessionProgress call in performPageSwitch; new init block with expired/resume/normal paths

## Decisions Made
- `btn-warning` (yellow) for Resume button: visual distinction from `btn-primary` Start Assessment, matching user decision
- `<a asp-action asp-route-id>` instead of JS button: direct navigation is correct since the resume modal fires on StartExam load, not on card click
- RESUME_PAGE > 0 gates the resume confirmation modal: page 0 means worker was on the first page, resume silently without modal
- `prePopulateAnswers()` runs first in init block: answeredCount badge and answered Set must be accurate before modal or render
- Failure toast message: "Gagal memuat jawaban sebelumnya. Lanjutkan dari soal no. X." (X = RESUME_PAGE + 1, 1-based) per locked user decision
- EXAM_EXPIRED path: show modal + submit on OK click + 5s fallback timeout; `window.onbeforeunload = null` prevents leave-page dialog on auto-submit

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Initial build used wrong csproj name (`PortalHC_KPB.csproj`); corrected to `HcPortal.csproj` found via directory scan. Build successful.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Session resume frontend fully wired: worker sees Resume button, modal, timer, pre-populated answers, and progress saves automatically
- Plan 04 (if any) or Phase 43 (exam status polling) can proceed immediately
- Build is green (0 errors, 36 warnings pre-existing)

---
*Phase: 42-session-resume*
*Completed: 2026-02-24*
