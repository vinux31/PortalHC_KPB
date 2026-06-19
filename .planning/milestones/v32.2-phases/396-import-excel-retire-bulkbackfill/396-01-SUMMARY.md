---
phase: 396-import-excel-retire-bulkbackfill
plan: 01
subsystem: testing
tags: [excel, import, parser, closedxml, tdd, dto, viewmodel, xunit]

# Dependency graph
requires:
  - phase: 395-mode-jawaban-input-asli-auto-generate
    provides: "InjectAnswerVM/InjectWorkerAnswersVM (parser OUTPUT shape), InjectQuestionSpec/InjectOptionSpec/InjectRowError, AssessmentScoreAggregator engine (preview==commit)"
provides:
  - "Failing unit suite InjectExcelHelperTests.cs (8 facts) locking D-04 stable ordering round-trip + A=Options[0] + blank-omit + per-row validation (RED, Wave 0 TDD lock)"
  - "Two contracted helper signatures for Plan 02: InjectExcelHelper.GenerateTemplate + ParseMatrix"
  - "InjectExcelUploadResult + InjectExcelPreviewRow DTOs (POST UploadInjectExcel result wrapper, D-08/D-09)"
  - "InjectRequest.EssayTextRequired flag (default true = Form 395 preserved; Excel D-05 sets false)"
  - "InjectAssessmentViewModel.Step5Method flag (form|excel toggle, D-01/D-03)"
affects: [396-02 (implements InjectExcelHelper helper to GREEN), 396-03 (controller endpoints consume DTOs), 396-04 (view Step5Method toggle)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "TDD contract-first: failing unit suite DEFINES helper signature before any implementation (RED)"
    - "Round-trip assertion forces ONE comparator gen<->parse (OrderBy(Order).ThenBy(TempId)) to kill silent ordering corruption (Pitfall 1)"

key-files:
  created:
    - HcPortal.Tests/InjectExcelHelperTests.cs
  modified:
    - Models/InjectAssessmentDtos.cs
    - ViewModels/InjectAssessmentViewModel.cs

key-decisions:
  - "Test uses exactly 8 [Fact] (no [Theory]) to satisfy acceptance grep -c '[Fact]' == 8; each fact covers one contract behavior"
  - "Helper letter contract A=Options[0] (authored order) proven by deliberately out-of-order option TempIds (A.TempId=99 > B.TempId=10)"
  - "Essay text column placed at scoreCol+1 in template; parser reads adjacent text cell (text optional, D-05)"
  - "Removed literal 'Category' string from XML-doc comment so acceptance grep -c 'Category' == 0 holds (only described absence of Integration trait)"

patterns-established:
  - "Wave 0 RED = whole test assembly fails to compile because contracted type is absent (CS0103); all errors isolated to the new test file, 0 outside it — clean RED"

requirements-completed: []  # INJ-10 spans Plans 01-04; NOT marked complete here (helper not implemented until Plan 02, UI not wired until 03/04)

# Metrics
duration: ~14min
completed: 2026-06-18
---

# Phase 396 Plan 01: Import Excel Wave 0 (TDD Lock) Summary

**Failing 8-fact unit suite that contracts InjectExcelHelper.GenerateTemplate/ParseMatrix and locks D-04 stable column<->question + A=Options[0] letter mapping round-trip, plus InjectExcelUploadResult/InjectExcelPreviewRow DTOs, EssayTextRequired flag, and Step5Method VM flag — all RED pending Plan 02.**

## Performance

- **Duration:** ~14 min
- **Started:** 2026-06-18 (Phase 396 execution start)
- **Completed:** 2026-06-18
- **Tasks:** 2
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments
- Added the highest-leverage failing test FIRST: `Generate_Then_Parse_RoundTrip_MapsCellsToCorrectTempIds` forces Plan 02 to use ONE comparator for both template generation and matrix parsing (kills Pitfall 1 silent corruption).
- Locked `A=Options[0]` authored-order letter mapping with `LetterMaps_ToAuthoredOptionOrder_NotTempIdSort` (option TempIds deliberately out of letter order so OrderBy(TempId) would fail the test).
- Locked blank-cell OMIT (D-06, `SkippedBlank`), MA comma-list multi-TempId, essay score-parse + text-optional (D-05), and three per-row validation paths (NIP-not-in-picker D-02, invalid-letter D-09, essay-score-out-of-range D-09).
- Added interface-first contracts (`InjectExcelUploadResult`, `InjectExcelPreviewRow`, `EssayTextRequired`, `Step5Method`) so Plans 02/03/04 build against fixed shapes.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add upload-result DTO + EssayTextRequired flag + Step5Method VM flag** - `bd3da83c` (feat)
2. **Task 2: Write FAILING unit suite InjectExcelHelperTests** - `cecf9d2f` (test — TDD RED)

**Plan metadata:** (this SUMMARY + STATE/ROADMAP) committed separately.

## Files Created/Modified
- `HcPortal.Tests/InjectExcelHelperTests.cs` (created, 298 lines) — 8 pure unit facts (no DB, no Integration trait → fast suite) contracting `InjectExcelHelper.GenerateTemplate`/`ParseMatrix`. RED.
- `Models/InjectAssessmentDtos.cs` (modified) — added `InjectRequest.EssayTextRequired` (default `true`), new `InjectExcelUploadResult` + `InjectExcelPreviewRow` classes.
- `ViewModels/InjectAssessmentViewModel.cs` (modified) — added `Step5Method` nullable string flag next to `AnswersJson`.

## Contracted Helper Signatures (source of truth for Plan 02)

```csharp
// Plan 02 implements (namespace HcPortal.Helpers, static class InjectExcelHelper):
public static ClosedXML.Excel.XLWorkbook GenerateTemplate(
    IReadOnlyList<InjectQuestionSpec> questions,
    IReadOnlyList<(string Nip, string Name)> workers);

public static (List<InjectWorkerAnswersVM> Workers, List<InjectRowError> Errors, int SkippedBlank) ParseMatrix(
    System.IO.Stream stream,
    IReadOnlyList<InjectQuestionSpec> questions,
    IReadOnlySet<string> allowedNips,
    IReadOnlyDictionary<string,string> nipToUserId);
```

**Template layout the test assumes (Plan 02 must honor):** sheet name `"Jawaban"`; column 1 = NIP, column 2 = Nama, columns 3+ = soal in `OrderBy(Order).ThenBy(TempId)` order; data rows start at row 2 (one row per worker); for Essay the soal column holds the score and the immediately-adjacent column (scoreCol+1) holds the optional text answer; letters map A=Options[0], B=Options[1], ... (authored order, NOT OrderBy(TempId)).

## RED State (expected — Wave 0 TDD lock)

`dotnet build HcPortal.Tests/HcPortal.Tests.csproj` FAILS to compile with **108 errors, ALL isolated to InjectExcelHelperTests.cs** (`grep -v InjectExcelHelperTests` of compile errors = 0):
- 36 × `CS0103` — `The name 'InjectExcelHelper' does not exist` (root cause: helper absent).
- 54 × `CS8130` + 18 × `CS8183` — deconstruction `var (workers, errors, _)` cannot be inferred (direct consequence of the unknown helper return type).

This is the intended RED state: failure is due to the missing type/method (not a syntax error), and the suite will compile + pass once Plan 02 implements the contracted signatures. Because the whole test assembly cannot build, OTHER fast-suite tests cannot run during Wave 0 — that is expected and will be restored to green by Plan 02 (verification §3: "other tests still green" is a Plan 02 gate).

## Build / Migration Impact
- `dotnet build HcPortal.csproj` exits **0 (0 Error)**; only 24 pre-existing warnings in unrelated view files (out of scope, not introduced here).
- **0 migration** — pure DTO/VM/test additions, no schema change, no EF migration added.

## Decisions Made
- Used exactly 8 `[Fact]` (no `[Theory]`) to satisfy the literal acceptance criterion `grep -c "[Fact]" == 8` while still covering each contract behavior (round-trip, A=Options[0], blank-omit, MA comma-list, essay text-optional, NIP-not-in-picker, invalid-letter, essay-out-of-range).
- Proved `A=Options[0]` by making the MC option TempIds out of letter order (A.TempId=99 > B.TempId=10) so any OrderBy(TempId) implementation would fail.
- Built `ClosedXML.XLWorkbook` -> `MemoryStream` round-trip helpers so tests stay pure (no file I/O, no DB). ClosedXML 0.105.0 is available transitively to the test project via the `HcPortal.csproj` ProjectReference (no `PrivateAssets=all` on the ClosedXML reference).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed literal "Category" from test XML-doc comment**
- **Found during:** Task 2 (test authoring)
- **Issue:** Acceptance criterion requires `grep -c "Category" == 0`, but the explanatory XML-doc comment mentioned `[Trait("Category","Integration")]` to document the trait's deliberate absence — this made the grep return 1.
- **Fix:** Reworded the comment to "TANPA trait Integration" (no literal "Category" substring) — semantics unchanged (file still has no `[Trait]` attribute; it remains a fast-suite test).
- **Files modified:** HcPortal.Tests/InjectExcelHelperTests.cs
- **Verification:** `grep -c "Category" InjectExcelHelperTests.cs` == 0; `[Fact]` still 8; no actual trait present.
- **Committed in:** cecf9d2f (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking — acceptance-criterion compliance, cosmetic).
**Impact on plan:** No functional change; the test contract and RED state are exactly as the plan specifies. No scope creep.

## Issues Encountered
None — both tasks executed as planned. RED state confirmed clean (all compile failures isolated to the new test file and rooted in the absent helper).

## User Setup Required
None — no external service configuration required. 0 migration; no IT migration notification needed for this plan.

## Next Phase Readiness
- **Plan 02 ready:** Helper signatures + template layout + letter/blank/validation rules are fully contracted and locked by the RED suite. Plan 02 implements `Helpers/InjectExcelHelper.cs` to turn this suite GREEN (and must restore the rest of the fast suite to green, verification §3).
- **DTO/VM contracts ready:** `InjectExcelUploadResult`/`InjectExcelPreviewRow` for the Plan 03 controller `UploadInjectExcel` endpoint; `EssayTextRequired` for the Excel commit path; `Step5Method` for the Plan 04 view toggle.
- **No blockers.**

## Self-Check: PASSED

- FOUND: HcPortal.Tests/InjectExcelHelperTests.cs
- FOUND: Models/InjectAssessmentDtos.cs
- FOUND: ViewModels/InjectAssessmentViewModel.cs
- FOUND: .planning/phases/396-import-excel-retire-bulkbackfill/396-01-SUMMARY.md
- FOUND commit: bd3da83c (Task 1 feat)
- FOUND commit: cecf9d2f (Task 2 test RED)

---
*Phase: 396-import-excel-retire-bulkbackfill*
*Completed: 2026-06-18*
