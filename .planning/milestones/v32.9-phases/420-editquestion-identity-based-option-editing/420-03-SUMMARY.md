---
phase: 420
plan: 03
one_liner: "Identity-edit integration tests (2 ported + 7 new) all green in full suite 702/702; Playwright real-browser UAT 3/3 PASS DB-verified (VRF-01)"
status: complete
commits: [73bab5b7, cf8595e3]
---

# Phase 420 Plan 03 — Summary

## What changed
- **`HcPortal.Tests/EditShrinkGuardIntegrationTests.cs`** — ported TEST1/TEST2 to identity contract (pass `OptionInput.Id`, omit deleted row); added 7 new identity `[Fact]` tests: MiddleDelete_Unanswered_NoRelabel, EditAnsweredOption_TextAndCorrectness_UpdatesById, ConvertAnsweredMcToEssay_Blocked_NoException, AntiTamper_ForeignOptionId_Rejected_NoMutation, AddOption_NullId_Adds_NotOverwriteExisting, DuplicateSubmittedId_Rejected, AddOption_SetNewAsCorrect (WR-02 coverage).
- **`HcPortal.Tests/SectionFixRegressionTests.cs`** (CR-01 fix) — Edit6Options + H3 ported to identity contract + Id-stability assertions (lock UPDATE-in-place, not recreate).
- **`tests/e2e/identity-option-edit-420.spec.ts`** (new) — real-browser regression: 3 scenarios with SEED_WORKFLOW beforeAll BACKUP / afterAll RESTORE.

## Requirements
VRF-01 + OPTEDIT-05 (regression). Integration leg proves OPTEDIT-01..04 too.

## Verification
- **`dotnet test HcPortal.Tests` → 702/702 PASS** (full suite, 3m25s, SQLEXPRESS). EditShrinkGuard filter = 13 (2 ported + 7 new + 4 pure-helper). 0 regression.
- **Playwright real-browser UAT @5277 → 3/3 PASS** (via Playwright MCP driver, DB-verified) — see 420-UAT.md:
  - S1 delete-middle ANSWERED → blocked `Opsi "B" ("B") sudah dijawab...` (D-04), no 500; DB Q1 4 opts + response intact.
  - S2 delete-middle UNANSWERED → success; DB 3 opts A,C,D — C still "C" (no relabel).
  - S3 add-option → new row hidden Id empty (clone gotcha §2c), A not overwritten, E added.
  - Client-JS carriers proven live: PATCH C (populate hidden Id from GET JSON), PATCH B (reletter preserves Id), PATCH D (clone-reset clears hidden) — the layer controller tests cannot reach (lesson 354).
- SEED_WORKFLOW honored: snapshot → seed → test → RESTORE (DB pristine, 0 residue) → .bak deleted; SEED_JOURNAL cleaned.

## Notes
- Pre-existing form quirk observed in S3: adding an option drops the MC correct-radio selection (re-check needed) — unrelated to the Phase 420 hidden-Id change; not in scope.

migration=FALSE. NOT pushed.
