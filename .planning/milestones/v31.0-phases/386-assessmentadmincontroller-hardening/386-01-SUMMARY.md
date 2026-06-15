---
phase: 386-assessmentadmincontroller-hardening
plan: 01
subsystem: testing
tags: [xunit, playwright, tdd-red, essay-grading, multiple-answer, assessment]

# Dependency graph
requires:
  - phase: 384-monitoring-essay-ui-refactor
    provides: "EssayFinalizeRecomputeFixture + SeedEssayOnlyAsync + EssaySubmitFinalizeAuthzTests + /Admin/EssayGrading page selectors"
  - phase: 383-essay-grading-correctness
    provides: "AssessmentScoreAggregator.IsQuestionCorrect (reused as-is for MA correctness in PdfAnswerCellTests)"
provides:
  - "6 RED/skip-gated test scaffolds locking PXF-02/04/05 acceptance contracts up front"
  - "OptionValidationTests (7 Fact) — contract for Wave-1 helper QuestionOptionValidator.ValidateQuestionOptions"
  - "PdfAnswerCellTests (6 Fact) — contract for Wave-1 method AssessmentScoreAggregator.BuildAnswerCell (MA join format ', ')"
  - "EssayEmptyPendingParityTests (8 Fact, Integration) — 4-fixture count-parity + upsert + status-guard mirrors of 4 controller predicate sites"
  - "Extended authz lock: SubmitEssayScore retains [Authorize Admin,HC] + [ValidateAntiForgeryToken] after Wave-3 status-guard edit"
  - "2 Playwright e2e specs (test.fixme) — PXF-02 reject path + PXF-04 finalize round-trip"
affects: [386-02, 386-03, 386-04, 386-wave1, 386-wave2, 386-wave3, 386-wave4]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "TDD-RED whole-phase: pure unit tests reference Wave-1 helpers that do not exist yet → project build fails ONLY on those symbols (intended RED gate)"
    - "Mirror count-builder with mandatory drift-guard comment per production predicate site (pattern from MirrorSubmitEssayScoreAsync)"
    - "e2e test.fixme gating: spec body documents full flow but stays green until later wave un-skips (ganti test.fixme → test)"

key-files:
  created:
    - HcPortal.Tests/OptionValidationTests.cs
    - HcPortal.Tests/PdfAnswerCellTests.cs
    - HcPortal.Tests/EssayEmptyPendingParityTests.cs
    - tests/e2e/option-validation-386.spec.ts
    - tests/e2e/essay-empty-finalize-386.spec.ts
  modified:
    - HcPortal.Tests/EssayFinalizeRecomputeTests.cs

key-decisions:
  - "MA answer-cell join format = ', ' (comma-space, D-10 Excel precedent) — Wave 1 BuildAnswerCell WAJIB match this; locked in PdfAnswerCellTests"
  - "Reused existing EssayFinalizeRecomputeFixture (public, same assembly) for the new Integration parity test — no new fixture class"
  - "New local SeedEssayParityAsync helper (4 variants) instead of mutating v30.0-locked SeedEssayOnlyAsync"
  - "Whole-project RED accepted as Wave-0 verification: build error list confirmed to contain ONLY Wave-1 symbols (ValidateQuestionOptions, BuildAnswerCell); no other file errors"

patterns-established:
  - "Pattern: Wave-0 RED scaffolds set fixed acceptance contracts so later-wave executors implement against signatures, not exploration"
  - "Pattern: 4 mirror count-builders encode the NEW (post-fix) predicate so Wave 3 makes production match the test, not vice versa"

requirements-completed: []  # PXF-02/04/05 NOT complete — only RED scaffolds laid; requirements close in later waves

# Metrics
duration: 10min
completed: 2026-06-15
---

# Phase 386 Plan 01: Wave-0 RED Test Scaffolds Summary

**6 RED/skip-gated test scaffolds (xUnit + Playwright) that lock PXF-02 option validation, PXF-04 essay-empty finalize count-parity, and PXF-05 PDF MA answer-cell contracts before any production code — project builds RED only on the two Wave-1 helper symbols.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-06-15T14:27:59Z
- **Completed:** 2026-06-15T14:37:43Z
- **Tasks:** 3
- **Files modified:** 6 (5 created, 1 extended)

## Accomplishments
- PXF-02 + PXF-05 pure unit tests (`OptionValidationTests` 7 Fact, `PdfAnswerCellTests` 6 Fact) — both reference Wave-1 helpers that do not exist yet → intended RED.
- PXF-04 integration count-parity test (`EssayEmptyPendingParityTests` 8 Fact, `[Trait Integration]`) with 4 mandatory fixture variants + 4 drift-guarded mirror builders + upsert + status-guard.
- Extended `EssaySubmitFinalizeAuthzTests` with reflection lock that SubmitEssayScore keeps `[Authorize Admin,HC]` + `[ValidateAntiForgeryToken]` after the Wave-3 status-guard edit (D-08).
- 2 Playwright e2e specs (`option-validation-386`, `essay-empty-finalize-386`) gated by `test.fixme`, both listed by Playwright `--list --workers=1` with exit 0.
- 0 production code changed — controller, helpers, views untouched.

## Failing Symbols Wave 1 Must Add (intended RED)

The project build (`dotnet build HcPortal.Tests/HcPortal.Tests.csproj`) fails with these — and ONLY these — symbols, both created in Wave 1 (Plan 02):

1. **`HcPortal.Helpers.QuestionOptionValidator.ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)`** — new static helper in a new file `Helpers/QuestionOptionValidator.cs`. Referenced by `OptionValidationTests.cs`. Errors: CS0103 (`QuestionOptionValidator` not in context) + cascading CS8130/CS8183 (tuple deconstruction can't infer type because the method is absent).
2. **`HcPortal.Helpers.AssessmentScoreAggregator.BuildAnswerCell(PackageQuestion q, IEnumerable<PackageUserResponse> responsesForQ)`** — new method added beside `IsQuestionCorrect` in existing `Helpers/AssessmentScoreAggregator.cs`. Referenced by `PdfAnswerCellTests.cs`. Errors: CS0117 (`AssessmentScoreAggregator` does not contain a definition for `BuildAnswerCell`).

Verified: no build errors originate from `EssayEmptyPendingParityTests.cs`, the authz edit, or the e2e specs — they all compile/list cleanly.

## 4 Fixture Variants Seeded (PXF-04)

`SeedEssayParityAsync(ctx, userId, variant, ...)` parametrizes the single essay response row of a `PendingGrading` / `HasManualGrading=true` session:

| Variant | Response row | Expected pending (all 4 sites) |
|---------|--------------|--------------------------------|
| `NoRow` | no PackageUserResponse at all | 0 |
| `WhitespaceText` | TextAnswer = `"  "` (and `"\t\n"`), EssayScore=null | 0 (whitespace = NOT pending, D-05) |
| `FilledUngraded` | TextAnswer = "Jawaban peserta", EssayScore=null | 1 |
| `Graded` | TextAnswer = "Jawaban peserta", EssayScore=80 | 0 |

The 4 mirror builders (`MonitoringPendingCountAsync` site 4 L3308-3314, `PagePendingCountAsync` site 1 L3500, `SubmitPendingCountAsync` site 3 L3547-3551, `FinalizeGatePendingCountAsync` site 2 L3620) each encode the NEW post-fix predicate `!string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null` with a `DRIFT-GUARD` comment referencing the production line range. Wave 3 makes the real controller match these mirrors.

## MA Answer-Cell Join Format (PXF-05)

`BuildAnswerCell` for MultipleAnswer must join ALL selected OptionText in Id-order with **`", "` (comma-space)** — locked by `MultipleAnswer_ExactSet_CorrectAndAllOptionsJoined` asserting `"Avtur, Solar"`. MC = single OptionText; Essay = TextAnswer truncate 300 + `"..."` (total length 303); empty = `"—"` (em dash U+2014). Wave 1 implementation must match these exactly.

## Task Commits

Each task was committed atomically:

1. **Task 1: Scaffold PXF-02 + PXF-05 pure unit tests (RED)** — `cda73e58` (test)
2. **Task 2: Scaffold PXF-04 count-parity + upsert + status-guard + authz lock** — `d64df40d` (test)
3. **Task 3: Scaffold PXF-02 + PXF-04 Playwright e2e specs (test.fixme gated)** — `788c203a` (test)

_Note: two interleaved `docs(387)` commits (`fac3dd5e`, `7f28d84c`) in the log come from a concurrent planning session on the shared ITHandoff branch — unrelated to this plan._

## Files Created/Modified
- `HcPortal.Tests/OptionValidationTests.cs` — 7 Fact, pure (no [Trait]), targets Wave-1 `ValidateQuestionOptions` (RED).
- `HcPortal.Tests/PdfAnswerCellTests.cs` — 6 Fact, pure, targets Wave-1 `BuildAnswerCell` + reuses `IsQuestionCorrect` MA (RED).
- `HcPortal.Tests/EssayEmptyPendingParityTests.cs` — 8 Fact, `[Trait Integration]`, 4 fixture variants + 4 drift-guarded mirror builders + upsert/status-guard mirrors.
- `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` — extended `EssaySubmitFinalizeAuthzTests` with `SubmitEssayScore_RetainsAuthorizeAfterStatusGuardEdit` (existing test untouched).
- `tests/e2e/option-validation-386.spec.ts` — PXF-02 MC reject path, `test.fixme`, `.alert-danger` assert, selectors from `questionFormSelectors`.
- `tests/e2e/essay-empty-finalize-386.spec.ts` — PXF-04 finalize round-trip, `test.fixme`, "Selesaikan Penilaian" visible + success not "Jawaban tidak ditemukan".

## Decisions Made
- MA answer-cell join `", "` (comma-space) chosen and locked in tests — documented so Wave 1 matches.
- Reused public `EssayFinalizeRecomputeFixture` rather than creating a sibling fixture.
- New local `SeedEssayParityAsync` (4 variants) to avoid touching the v30.0-locked `SeedEssayOnlyAsync`.
- Accepted whole-project RED as the Wave-0 verification gate (per plan), confirming error list contains only the 2 Wave-1 symbols.

## Deviations from Plan

None - plan executed exactly as written. All 3 tasks produced the artifacts and the exact RED/skip-gated states the plan specified; acceptance criteria for each task verified.

## Issues Encountered
- `Select-String` (PowerShell cmdlet) is not available in the Bash tool used for the `<verify>` commands; substituted equivalent `grep`/redirect-to-log inspection to confirm the same symbols. Same evidence, different shell — no impact on outcome.

## Known Stubs
None that block the plan's Wave-0 goal. The 5 new tests are intentionally RED/skip-gated by design (this is a Wave-0 TDD-RED plan); they turn GREEN as Waves 1-4 add the helpers and wire the controller. This is documented intent, not an unresolved stub.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Wave 1 (Plan 02) can now implement against fixed contracts: create `QuestionOptionValidator.ValidateQuestionOptions` and add `AssessmentScoreAggregator.BuildAnswerCell` (join `", "`) — the project compiles GREEN once both exist, and `OptionValidationTests` + `PdfAnswerCellTests` + the authz lock pass immediately.
- Wave 3 wires the 4 controller predicate sites to the NEW predicate already encoded in `EssayEmptyPendingParityTests` mirrors (drift-guard comments cite exact line ranges).
- Wave 2 / Wave 3 un-skip the e2e specs (`test.fixme` → `test`) and fill the `// VERIFY` seed/packageId placeholders.
- No blockers. 0 migration. 0 production code touched this plan.

## Self-Check: PASSED

All 6 deliverable files exist on disk and all 3 task commit hashes (`cda73e58`, `d64df40d`, `788c203a`) are present in git history. Build RED confirmed limited to the 2 Wave-1 symbols; both Playwright specs list with exit 0.

---
*Phase: 386-assessmentadmincontroller-hardening*
*Completed: 2026-06-15*
