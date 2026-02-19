---
phase: 17-question-and-exam-ux-improvements
plan: "05"
subsystem: ui
tags: [razor, exam, paged-layout, timer, collapsible-panel, js, bootstrap]

# Dependency graph
requires:
  - phase: 17-question-and-exam-ux-improvements
    provides: PackageExamViewModel with ExamQuestionItem/ExamOptionItem (Plan 17-04)
provides:
  - Paged exam UI: 10 questions/page with JS page switching (no reload)
  - Countdown timer with red warning at 5 min, auto-submit at 0
  - Collapsible right-sidebar question number panel (green = answered, grey = unanswered)
  - Sticky header showing assessment title and answered/total counter
  - "Review and Submit" button on last page (POST to ExamSummary action)
affects: [17-06-exam-summary-and-grading]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Paged JS UX: all questions rendered in DOM; JS show/hide active page div — no server round-trips
    - Hidden input answer tracking: per-question hidden inputs updated by JS on radio change; final form POST carries all answers
    - JsonSerializer.Serialize in @Html.Raw for JS constants derived from server-side Razor data

key-files:
  created: []
  modified:
    - Views/CMP/StartExam.cshtml

key-decisions:
  - "Radio buttons use name=radio_{questionId} (not answers[{questionId}]) so JS change event works without interfering with hidden form input binding"
  - "Letters A/B/C/D assigned at render time by option index — not stored in model or DB"
  - "pageQuestionIds JS array serialized server-side with System.Text.Json so JS can look up which question IDs belong to which page for the collapsible panel"
  - "id=page_@(page) uses explicit parentheses to prevent Razor compiler from treating @page as a @page directive token (auto-fixed)"

# Metrics
duration: ~3min
completed: 2026-02-19
---

# Phase 17 Plan 05: Paged Exam Layout, Timer, and Collapsible Panel Summary

**Paged exam view with 10 questions/page, JS navigation, countdown timer (red at 5 min), answered counter, and collapsible question number sidebar panel**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-19T14:22:30Z
- **Completed:** 2026-02-19T14:25:03Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments

- Replaced the flat, all-questions-on-one-page StartExam.cshtml with a fully paged exam layout
- 10 questions per page; JS `changePage()` shows/hides page divs — no server round-trips, no page reload
- Sticky header shows assessment title and live "X/N answered" counter
- Countdown timer: formats MM:SS, turns `.text-danger` at 5 minutes remaining (300 seconds), auto-submits form at 0
- Collapsible right-sidebar panel shows current page's question numbers as rounded-pill badges: green (answered) / grey (unanswered); toggled with a button
- Prev/Next page buttons; no blocking if questions unanswered; Previous button disabled on page 1
- Last page shows "Review and Submit" instead of "Next Page" — submits form via POST to ExamSummary action (Plan 17-06 creates that action)
- Per-question hidden inputs (`name="answers[{questionId}]"`) track all answers across page switches; radio buttons use separate `name="radio_{questionId}"` prefix to avoid interfering with hidden inputs
- Letters A/B/C/D assigned at render time by loop index (not stored in model)
- `window.onbeforeunload` warns on accidental navigation; suppressed on legitimate form submit

## Task Commits

1. **Task 1: Replace StartExam.cshtml with paged exam layout** - `58eed65` (feat)

## Files Created/Modified

- `Views/CMP/StartExam.cshtml` - Complete replacement with paged layout, timer, collapsible panel, JS answer tracking

## Decisions Made

- Radio buttons use `name="radio_{questionId}"` rather than `name="answers[{questionId}]"` so JS change events work independently of hidden form inputs — avoids double-submission and JS/form conflict.
- `pageQuestionIds` array serialized server-side with `System.Text.Json.JsonSerializer.Serialize()` inside `@Html.Raw()` — allows JS to reference Razor C# data without runtime AJAX calls.
- Letters A/B/C/D assigned at render time by `oi` index in the `@for` loop — consistent with `ExamOptionItem` design (no Letter field).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Changed `id="page_@page"` to `id="page_@(page)"` to prevent Razor `@page` directive ambiguity**
- **Found during:** Task 1 (dotnet build after writing the file)
- **Issue:** Razor compiler interpreted `@page` in `id="page_@page"` as the Razor Pages `@page` directive, producing 3 compiler errors (RZ3906, RZ2005, RZ1011). The plan's code used the bare `@page` form.
- **Fix:** Changed to `id="page_@(page)"` using explicit parentheses so Razor treats it as a C# expression, not a directive.
- **Files modified:** `Views/CMP/StartExam.cshtml`
- **Commit:** `58eed65` (included in the same task commit)

## Issues Encountered

- None beyond the `@page` directive ambiguity (documented and auto-fixed above).

## User Setup Required

None — no external service configuration required. The ExamSummary POST action (form target) does not yet exist; it will be created in Plan 17-06. The Razor tag helper `asp-action="ExamSummary"` does not cause a compile error — it generates a URL at runtime.

## Self-Check: PASSED

Files verified:
- `Views/CMP/StartExam.cshtml` — FOUND (231 insertions, replaces 71 lines)
- Commit `58eed65` — FOUND

Build result: `Build succeeded. 0 Error(s).`

## Next Phase Readiness

- `StartExam.cshtml` now renders the full paged exam UI
- Form submits to `ExamSummary` POST — Plan 17-06 must create `[HttpPost] ExamSummary` action in CMPController
- The action receives `id` (AssessmentSessionId), optional `assignmentId`, and `answers[{questionId}]` dictionary from the hidden inputs

---
*Phase: 17-question-and-exam-ux-improvements*
*Completed: 2026-02-19*
