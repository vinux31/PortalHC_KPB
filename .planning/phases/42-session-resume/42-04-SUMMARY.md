---
phase: 42-session-resume
plan: 04
type: summary
status: complete
completed: 2026-02-24
duration: ~40min (includes 4 bug fixes during human verification)
---

# Plan 04 Summary — Human Verification

## Result: APPROVED

All 6 tests passed after 4 bug fixes discovered during verification.

## Tasks

- [x] Task 1: Human end-to-end verification — APPROVED (all 6 tests pass)

## Bugs Found and Fixed During Verification

### Bug 1 — InProgress card missing from Assessment page (server-side)
- **Root cause:** `CMPController.cs` Assessment GET query filtered `Status == "Open" || "Upcoming"` — `InProgress` sessions never fetched from DB
- **Fix:** Added `|| a.Status == "InProgress"` to query (commit `95b17c5`)

### Bug 2 — InProgress card hidden by JS tab filter
- **Root cause:** `Assessment.cshtml` `filterCards('open')` only matched `cardStatus === 'open'` — `inprogress` cards were hidden even when in DOM
- **Fix:** `matchStatuses = ['open', 'inprogress']` when Open tab is active (commit `0b08468`)

### Bug 3 — Resume modal not appearing (`bootstrap is not defined`)
- **Root cause:** `StartExam.cshtml` `<script>` was an inline block rendered by `@RenderBody()` — executes before Bootstrap JS loads in layout. `new bootstrap.Modal(...)` threw `ReferenceError`
- **Fix:** Wrapped entire `<script>` block in `@section Scripts { }` so it renders after Bootstrap (commit `6b8fb44`)

### Bug 4 — Modal showed page number instead of question number
- **Root cause:** `RESUME_PAGE + 1` displayed page index (1-based) instead of first question on that page
- **Fix:** `RESUME_PAGE * QUESTIONS_PER_PAGE + 1` (commit `10e71ec`)

## Verification Results

| Test | Result |
|------|--------|
| Test 1 — Resume button visible (yellow) on InProgress card | APPROVED |
| Test 2 — Modal "Ada ujian yang belum selesai — Lanjutkan dari soal no. 11?" | APPROVED |
| Test 3 — Correct page + pre-populated answers + answered count | APPROVED |
| Test 4 — Timer shows remaining time, not full duration | APPROVED |
| Test 5 — UpdateSessionProgress POST fires on nav and every 30s | APPROVED |
| Test 6 — Free navigation works with pre-populated answers | APPROVED |

## Requirements Satisfied

- RESUME-01: Worker resumes at last active page with pre-populated answers ✓
- RESUME-02: Timer restored from server-calculated remaining time (offline time excluded) ✓
- RESUME-03: Stale question detection via SavedQuestionCount mismatch check ✓
