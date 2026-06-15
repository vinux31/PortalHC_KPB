---
phase: 386-assessmentadmincontroller-hardening
plan: 02
subsystem: testing
tags: [xunit, tdd-green, validation, multiple-answer, essay, pdf-export, pure-helper]

# Dependency graph
requires:
  - phase: 386-01-wave0-red-scaffolds
    provides: "OptionValidationTests (7 Fact) + PdfAnswerCellTests (6 Fact) — RED contracts locking ValidateQuestionOptions + BuildAnswerCell signatures and the ', ' MA join format"
  - phase: 383-essay-grading-correctness
    provides: "AssessmentScoreAggregator.IsQuestionCorrect (BuildAnswerCell added beside it, same file/style; bodies untouched)"
provides:
  - "QuestionOptionValidator.ValidateQuestionOptions(type, texts, corrects) — pure EF-free option-presence validator (PXF-02), single source for CreateQuestion + EditQuestion (Wave 2 wires it)"
  - "AssessmentScoreAggregator.BuildAnswerCell(q, responses) — pure EF-free display-cell builder (PXF-05), single source for PDF + Excel answer cell (Wave 4 wires it)"
  - "OptionValidationTests + PdfAnswerCellTests turned GREEN; IsQuestionCorrect regression still GREEN (24/24 across the 4 pure filters)"
affects: [386-03, 386-04, 386-wave2, 386-wave4]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "TDD-GREEN against Wave-0 fixed contracts: implement to the RED test signatures, no exploration"
    - "Pure helper extraction (EF-free, System.Linq + HcPortal.Models only) for kill-drift single-source validation/display"

key-files:
  created:
    - Helpers/QuestionOptionValidator.cs
  modified:
    - Helpers/AssessmentScoreAggregator.cs

key-decisions:
  - "ValidateQuestionOptions message strings LOCKED for Wave 2 reuse: ≥2 = '{Short} membutuhkan minimal 2 opsi jawaban yang berisi teks.'; correct-text = 'Opsi yang ditandai sebagai jawaban benar harus berisi teks ({Short}).' (QuestionTypeLabels.Short → 'Single Answer' / 'Multiple Answer')"
  - "Validator deliberately does NOT replicate the controller correctCount gate (MC==1, MA>=2 correct) — that stays in AssessmentAdminController.cs:6440-6456; helper adds ONLY text-presence (D-01 ≥2 ber-teks, D-03 correct-flag needs text, D-02 ber-teks = !IsNullOrWhiteSpace)"
  - "BuildAnswerCell MA join = ', ' (comma-space, D-10 Excel L4860 precedent) with explicit .OrderBy(o => o.Id) before Select for deterministic Id-order join; MC single OptionText; Essay TextAnswer truncate 300 + '...'; empty = '—' (em dash U+2014)"
  - "BuildAnswerCell added beside IsQuestionCorrect; Compute + IsQuestionCorrect bodies untouched (D-11 scoring/correctness intact)"

patterns-established:
  - "Pattern: Wave-1 GREEN implements exactly the symbol+behavior the Wave-0 RED tests assert (no signature drift)"
  - "Pattern: extract-once validation/display helper shared by two controller call-sites to kill drift (CreateQuestion+EditQuestion for PXF-02; PDF+Excel for PXF-05)"

requirements-completed: []  # PXF-02/PXF-05 helpers exist+tested, but the REQs close only after Wave 2 (PXF-02 wiring) and Wave 4 (PXF-05 wiring) connect them to the controller export/POST paths

# Metrics
duration: 5min
completed: 2026-06-15
---

# Phase 386 Plan 02: Wave-1 Pure Helper Extraction Summary

**Two pure EF-free helpers — `QuestionOptionValidator.ValidateQuestionOptions` (PXF-02 option-presence) and `AssessmentScoreAggregator.BuildAnswerCell` (PXF-05 answer-cell with ', ' MA join) — that turn the Wave-0 RED unit tests GREEN (24/24) without touching any controller, view, or scoring logic.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-15T14:42:56Z
- **Completed:** 2026-06-15T14:47:16Z
- **Tasks:** 2
- **Files modified:** 2 (1 created, 1 modified)

## Accomplishments
- Created `Helpers/QuestionOptionValidator.cs` (namespace `HcPortal.Helpers`): pure `ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)` returning `(bool ok, string? error)`. Essay/non-option types bypass; MC/MA require ≥2 text-filled options (D-01) and every correct-flagged option must contain text (D-03). All 7 `OptionValidationTests` GREEN.
- Added `BuildAnswerCell(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)` beside `IsQuestionCorrect` in `Helpers/AssessmentScoreAggregator.cs`: MA joins all selected OptionText in Id-order with `", "`; MC single OptionText; Essay TextAnswer truncated to 300 + `"..."`; empty = `"—"`. All 6 `PdfAnswerCellTests` GREEN.
- `IsQuestionCorrect` v30.0 regression suite still GREEN — no scoring/correctness drift (`Compute` + `IsQuestionCorrect` bodies byte-unchanged).
- Test project now compiles (was RED on exactly these 2 symbols); full solution `dotnet build` exits 0. 0 production controller/view code touched, 0 migration.

## Verification Results

| Filter | Result |
|--------|--------|
| `dotnet build HcPortal.Tests/HcPortal.Tests.csproj` | Build succeeded (both Wave-1 symbols resolve) |
| `dotnet test --filter OptionValidation/BuildAnswerCell/IsQuestionCorrect/PdfAnswerCell` | **Passed! Failed: 0, Passed: 24, Total: 24** |
| `dotnet build` (full solution) | Build succeeded, 0 Error(s) |

## Locked Message Strings (for Wave 2 controller wiring)

Wave 2 (Plan 03) must reuse these exact strings when wiring `ValidateQuestionOptions` into `CreateQuestion` + `EditQuestion` (so the controller surface matches the tested contract):

- **<2 filled options:** `"{QuestionTypeLabels.Short(type)} membutuhkan minimal 2 opsi jawaban yang berisi teks."`
- **correct-flag on empty option:** `"Opsi yang ditandai sebagai jawaban benar harus berisi teks ({QuestionTypeLabels.Short(type)})."`
- `QuestionTypeLabels.Short`: `MultipleChoice → "Single Answer"`, `MultipleAnswer → "Multiple Answer"`, `Essay → "Essay"`.

## MA Answer-Cell Join Format (for Wave 4 PDF/Excel wiring)

`BuildAnswerCell` for `MultipleAnswer` joins ALL selected OptionText with **`", "` (comma-space)** in **ascending option-Id order** (`.OrderBy(o => o.Id)`), mirroring the Excel per-session precedent (AssessmentAdminController.cs:4860-4861). `MultipleChoice` = single OptionText; `Essay` = TextAnswer truncate 300 + `"..."` (mirror PDF L5083); no response = `"—"` (em dash U+2014). Wave 4 (PXF-05 PDF/Excel wiring) must call this helper rather than recomputing — single source of display truth alongside `IsQuestionCorrect`.

## Task Commits

Each task was committed atomically (TDD: RED tests pre-existed from Wave-0 Plan 01; this plan supplies GREEN production code):

1. **Task 1: Create pure QuestionOptionValidator.ValidateQuestionOptions (PXF-02)** — `d7a49dc3` (feat)
2. **Task 2: Add pure BuildAnswerCell to AssessmentScoreAggregator (PXF-05)** — `85ce39e1` (feat)

_Note: the RED test commits for these helpers are `cda73e58` (Wave-0 Plan 01) — this plan's GREEN code makes them pass._

## Files Created/Modified
- `Helpers/QuestionOptionValidator.cs` (created) — pure static `ValidateQuestionOptions`; `using System.Linq` + `using HcPortal.Models` only (no EF / DbContext).
- `Helpers/AssessmentScoreAggregator.cs` (modified, +40 lines) — added `BuildAnswerCell` method beside `IsQuestionCorrect`; existing `Compute` + `IsQuestionCorrect` bodies unchanged.

## Decisions Made
- Message strings + `QuestionTypeLabels.Short` interpolation locked to satisfy `OptionValidationTests` and to give Wave 2 a copy-exact contract (documented above).
- Validator scope intentionally narrow (text-presence only) — `correctCount` semantics stay in the controller (no behavioral change there).
- `BuildAnswerCell` MA join uses explicit `.OrderBy(o => o.Id)` for deterministic comma-space join (matches `"Avtur, Solar"` assertion); empty/MC/Essay branches mirror existing PDF/Excel precedents.
- `BuildAnswerCell` placed inside `AssessmentScoreAggregator` (not a new file) so the display-truth helpers (`IsQuestionCorrect` + `BuildAnswerCell`) live together — kill-drift.

## Deviations from Plan

None - plan executed exactly as written. Both helpers created at the specified physical path (`Helpers/`) and namespace (`HcPortal.Helpers`), with the exact behavior, message strings, and `", "` MA join the plan and Wave-0 tests specified. All acceptance criteria per task verified.

## Issues Encountered
- Task 1 could not be test-verified in isolation: the whole `HcPortal.Tests` project fails to compile until BOTH Wave-1 symbols exist (Task 2's `PdfAnswerCellTests` reference to `BuildAnswerCell` blocks the build), so `dotnet test --filter OptionValidation` returned exit 0 without running tests. Resolved by confirming Task 1 compiles in the main `HcPortal.csproj` build (succeeded), committing Task 1, then running all 4 pure filters jointly after Task 2 — 24/24 GREEN confirms both helpers. No impact on outcome; expected behavior of a whole-project TDD-RED gate where two symbols are missing.

## Known Stubs
None. Both helpers are complete pure functions with full behavior wired and tested (no hardcoded empty values flowing to UI, no placeholder text, no unwired data source). The REQs (PXF-02/PXF-05) are not yet *closed* only because the controller/export wiring is a deliberate later-wave step (Wave 2 / Wave 4) — this is documented phasing, not an unresolved stub.

## Threat Flags
None. This plan adds pure helpers only — no new request entry point, no data mutation, no auth surface (per plan threat_model: T-386-02-T and T-386-05-I both `accept`, mitigations carried by tests + existing authenticated POST in Wave 2).

## User Setup Required
None - no external service configuration required. 0 migration.

## Next Phase Readiness
- **Wave 2 (PXF-02 wiring):** can now call `QuestionOptionValidator.ValidateQuestionOptions` from both `CreateQuestion` + `EditQuestion`, reusing the locked message strings above; the e2e spec `option-validation-386.spec.ts` (currently `test.fixme`) can be un-skipped once wired.
- **Wave 4 (PXF-05 wiring):** can call `AssessmentScoreAggregator.BuildAnswerCell` from `GeneratePerPesertaPdf` (and Excel `AddDetailPerSoalSheet`) for the "Jawaban" cell, replacing inline recompute with the `", "`-joined single source.
- No blockers. 0 migration. 0 controller/view/scoring-engine code touched this plan.

## Self-Check: PASSED

Both deliverable files exist on disk (`Helpers/QuestionOptionValidator.cs` created, `Helpers/AssessmentScoreAggregator.cs` modified) and both task commit hashes (`d7a49dc3`, `85ce39e1`) are present in git history. Test project builds; 24/24 pure tests GREEN (OptionValidation + PdfAnswerCell + IsQuestionCorrect); full solution build 0 errors.

---
*Phase: 386-assessmentadmincontroller-hardening*
*Completed: 2026-06-15*
