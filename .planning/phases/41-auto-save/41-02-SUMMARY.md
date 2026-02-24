---
phase: 41-auto-save
plan: 02
subsystem: ui
tags: [javascript, fetch, debounce, auto-save, bootstrap, cshtml, razorpages]

# Dependency graph
requires:
  - phase: 41-auto-save
    plan: 01
    provides: "SaveAnswer and SaveLegacyAnswer atomic upsert endpoints"
provides:
  - "Debounced auto-save (300ms) on radio clicks in StartExam — both package and legacy paths"
  - "Save indicator at bottom-right: saving/saved/error states with auto-fade"
  - "Navigation blocking (Prev/Next/ReviewSubmit) while save in-flight, 5s timeout fallback"
  - "1x retry on failure + toast 'Koneksi bermasalah, cek jaringan'"
  - "'Semua jawaban sudah tersimpan' reassurance badge on ExamSummary"
affects: [42-resume, 43-polling]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Debounce pattern: pendingSaves map stores timeout IDs; each radio change cancels prior timeout before setting new one"
    - "inFlightSaves Set tracks in-progress requests; prevents duplicate concurrent requests for same question"
    - "Navigation blocking: setInterval polls pendingSaves + inFlightSaves; setTimeout provides 5s escape hatch"
    - "IS_PACKAGE_PATH constant routes fetch to SaveAnswer (package) or SaveLegacyAnswer (legacy) at runtime"

key-files:
  created: []
  modified:
    - Views/CMP/StartExam.cshtml
    - Views/CMP/ExamSummary.cshtml

key-decisions:
  - "Debounce at 300ms — fast enough to not feel laggy, slow enough to avoid duplicate requests on rapid clicking"
  - "1x retry immediately on failure (not delayed) — minimize window where answer might be unsaved"
  - "5s navigation timeout — prevents UI from being permanently blocked if network dies mid-save"
  - "IS_PACKAGE_PATH determined server-side via @(Model.HasPackages) — no client-side sniffing of URL"
  - "reviewSubmitBtn guarded by click listener calling e.preventDefault() — re-triggers examForm.submit() after saves clear"

patterns-established:
  - "Debounce pattern: pendingSaves[qId] = setTimeout(..., 300) with clearTimeout on repeat clicks"
  - "inFlightSaves Set: add on request start, delete in .finally() to always release lock"
  - "Save indicator: single #saveIndicator element reused for all questions, fade-out via CSS animation"

# Metrics
duration: ~7min
completed: 2026-02-24
---

# Phase 41 Plan 02: Auto-Save Frontend Summary

**Debounced radio-to-SaveAnswer JS pipeline with live save indicator, navigation blocking, 1x retry + failure toast, and ExamSummary reassurance badge**

## Performance

- **Duration:** ~7 min
- **Started:** 2026-02-24T09:08:53Z
- **Completed:** 2026-02-24T09:15:00Z (Tasks 1-2; Task 3 pending human-verify)
- **Tasks:** 2/3 complete (Task 3 = checkpoint:human-verify)
- **Files modified:** 2

## Accomplishments
- Replaced fire-and-forget `saveAnswerAsync` with full debounced auto-save pipeline: `saveAnswerWithDebounce` → `saveAnswerAsync` (with retry) → `doSaveFetch` (routes to package or legacy endpoint)
- Added `#saveIndicator` fixed bottom-right badge cycling through saving/saved/error states with 2s auto-fade on success
- `changePage()` now blocks navigation while saves are pending (50ms poll + 5s timeout escape hatch); `performPageSwitch()` extracted for clarity
- `reviewSubmitBtn` click intercepted when saves pending — re-triggers `examForm.submit()` after saves clear or 5s
- Added "Semua jawaban sudah tersimpan" badge above the SubmitExam form in ExamSummary.cshtml

## Task Commits

Each task was committed atomically:

1. **Task 1: Auto-save JS and save indicator in StartExam.cshtml** - `22f6fd8` (feat)
2. **Task 2: ExamSummary reassurance badge** - `5f23e18` (feat)
3. **Task 3: End-to-end verification** - pending human-verify checkpoint

## Files Created/Modified
- `Views/CMP/StartExam.cshtml` - Full auto-save system: debounce, indicator HTML/CSS, navigation blocking, retry, toast
- `Views/CMP/ExamSummary.cshtml` - "Semua jawaban sudah tersimpan" badge above submit form

## Decisions Made
- Debounce at 300ms: balances responsiveness vs. request volume
- IS_PACKAGE_PATH server-side boolean routes fetch at runtime — no URL pattern matching
- 1x immediate retry on failure before surfacing error state
- 5s navigation timeout prevents permanent UI blocking on network loss
- `reviewSubmitBtn` uses `e.preventDefault()` + poll pattern (same as changePage) for consistency

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Auto-save frontend live for both package and legacy exam paths
- Phase 42 (resume) can build on top of the established save state — workers see consistent saved indicators
- Phase 43 (polling) wires CheckExamStatus setInterval — no conflicts with auto-save intervals

---
*Phase: 41-auto-save*
*Completed: 2026-02-24 (partial — Task 3 checkpoint pending)*

## Self-Check: PASSED

- Views/CMP/StartExam.cshtml — FOUND
- Views/CMP/ExamSummary.cshtml — FOUND
- Commit 22f6fd8 — FOUND (Task 1)
- Commit 5f23e18 — FOUND (Task 2)
