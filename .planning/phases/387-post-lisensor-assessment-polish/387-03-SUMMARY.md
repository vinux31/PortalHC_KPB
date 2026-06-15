---
phase: 387-post-lisensor-assessment-polish
plan: 03
subsystem: ui
tags: [razor, accessibility, aria, assessment, cmp]

# Dependency graph
requires:
  - phase: 386-assessmentadmincontroller-hardening
    provides: "Assessment review surfaces hardened (PXF-02/04/05) — Phase 387 polish runs after 386"
provides:
  - "Results.cshtml option images expose per-letter aria context (opsi A/B/C/D)"
  - "ExamSummary.cshtml option images expose per-letter aria context (opsi A/B/C/D)"
affects: [387-04-test, accessibility, screen-reader]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "letters[oi] index-derived option label mirrored from StartExam.cshtml across all option-image review surfaces"

key-files:
  created: []
  modified:
    - Views/CMP/Results.cshtml
    - Views/CMP/ExamSummary.cshtml

key-decisions:
  - "Converted plain foreach -> indexed for (both collections are List<T>, indexable) to derive A/B/C/D letter — verbatim mirror of StartExam.cshtml:125/134/148, no new markup invented"
  - "letters[] declared once per loop scope; neither view had a prior letters declaration (grep-verified) so no Razor redeclare conflict"

patterns-established:
  - "Per-letter aria context: AriaContext = \"opsi \" + letter where letter = oi < letters.Length ? letters[oi] : (oi+1).ToString()"

requirements-completed: [PXF-11]

# Metrics
duration: 3min
completed: 2026-06-15
---

# Phase 387 Plan 03: PXF-11 Option-Letter Aria Context Summary

**Option images on Results.cshtml and ExamSummary.cshtml now expose per-letter aria context (opsi A / opsi B / opsi C / opsi D) by converting each plain foreach into an indexed for loop, mirroring the verbatim letters[oi] derivation from StartExam.cshtml.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-15T16:47:25Z
- **Completed:** 2026-06-15T16:50:10Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Results.cshtml: Options List loop now indexed (`for (int oi = 0; oi < question.Options.Count; oi++)`); option-image partial carries `AriaContext = "opsi " + letter`. All per-option markup preserved (itemClass/icon/OptionText, `(Jawaban Anda)` / `(Jawaban Benar)` labels).
- ExamSummary.cshtml: OptionImages loop now indexed (`for (int oi = 0; oi < item.OptionImages.Count; oi++)`); option-image partial carries `AriaContext = "opsi " + letter`. The question-level image partial (`Cap = 240`) left untouched (git-diff verified).
- Both surfaces mirror StartExam.cshtml verbatim (`letters` array + `oi < letters.Length ? letters[oi] : (oi + 1).ToString()`). Screen-reader users now hear "opsi A / opsi B" instead of an undifferentiated "opsi" per option image.
- `dotnet build HcPortal.csproj` exits 0 (Razor compiles) after each task.

## Task Commits

Each task was committed atomically:

1. **Task 1: PXF-11 (Results) — indexed option loop + letter AriaContext** - `77f0f57f` (feat)
2. **Task 2: PXF-11 (ExamSummary) — indexed option loop + letter AriaContext** - `5cef4e81` (feat)

**Plan metadata:** (final docs commit — see git log)

## Files Created/Modified
- `Views/CMP/Results.cshtml` - Options List `foreach` -> indexed `for`; `letters` array; `AriaContext = "opsi " + letter` on option-image partial (L356-391)
- `Views/CMP/ExamSummary.cshtml` - OptionImages `foreach` -> indexed `for`; `letters` array; `AriaContext = "opsi " + letter` on option-image partial (L57-63); question-image partial (Cap=240) unchanged

## Decisions Made
- **foreach -> indexed for:** Both `question.Options` (`List<OptionReviewItem>`) and `item.OptionImages` (`List<ExamSummaryOptionItem>`) are `List<T>` → directly indexable with `[oi]`. Chose the indexed-`for` form (same as StartExam.cshtml) over the `Select((o,i))` projection — exact analog, simpler diff.
- **letters scope:** grep confirmed neither view previously declared `string[] letters` → declaring it once near each loop is safe (no Razor duplicate-variable error).

## Deviations from Plan

None - plan executed exactly as written. Both loops were plain `foreach` with no index var (as the plan's `<interfaces>` block predicted), so the conversion to indexed `for` and the single `AriaContext` string change were the only edits. No per-option markup, no question-level image partial, and no essay fallback block were altered.

## Issues Encountered
None.

## Known Stubs
None — both edits are render-time aria string changes wired to live data (`option.ImagePath` / `optImg.ImagePath`); `_QuestionImage` renders nothing when ImagePath is null, so the letter appears only on options that actually have an image.

## User Setup Required
None - no external service configuration required. 0 migration.

## Verification Status
- `dotnet build HcPortal.csproj` exits 0 after both tasks (Razor compiles on both surfaces).
- Grep acceptance per task PASS: `AriaContext = "opsi " + letter` present on both files; indexed `for` headers present; no residual static `AriaContext = "opsi"`; `(Jawaban Benar)` label preserved on Results; Cap=240 partial unchanged on ExamSummary.
- **D-09 MANDATORY (deferred to Plan 04):** PXF-11 requires Playwright runtime verification that the rendered `aria-label` contains the letter on BOTH surfaces (dynamic Razor a11y — grep+build insufficient, Phase 354 lesson). The Playwright spec lives in Plan 04 (387-04).

## Next Phase Readiness
- PXF-11 code-complete on both review surfaces; build green; 0 migration.
- Plan 04 (387-04) should add/run the Playwright a11y assertion to close D-09 before the second IT handoff.

## Self-Check: PASSED

- FOUND: Views/CMP/Results.cshtml
- FOUND: Views/CMP/ExamSummary.cshtml
- FOUND: .planning/phases/387-post-lisensor-assessment-polish/387-03-SUMMARY.md
- FOUND commit: 77f0f57f (Task 1 — Results)
- FOUND commit: 5cef4e81 (Task 2 — ExamSummary)

---
*Phase: 387-post-lisensor-assessment-polish*
*Completed: 2026-06-15*
