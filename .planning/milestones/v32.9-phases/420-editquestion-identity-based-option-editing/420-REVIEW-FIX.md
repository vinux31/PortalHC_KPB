# Phase 420 — Code Review Fix Summary

**Date:** 2026-06-25
**Source:** 420-REVIEW.md (gsd-code-reviewer). Fix commit: `cf8595e3`.
**Full suite after fix:** `dotnet test HcPortal.Tests` → **702/702 PASS** (3m25s), 0 regression.

| Finding | Severity | Disposition | Action |
|---------|----------|-------------|--------|
| CR-01 | Critical | **FIXED** | `SectionFixRegressionTests.Edit6Options_NoResponses_Succeeds_OptionsUpdated` + `H3_EditQuestionWith4Options_SucceedsNormally` submitted null-Id OptionInputs → under identity = mass-recreate (passed only via Count/text). Ported both to identity contract: capture `optionIds` in seed, submit `OptionInput { Id = optionIds[i] }`, added `Assert.Equal(optionIds…, q.Options.Select(Id)…)` Id-stability assert → now locks UPDATE-in-place (no recreate). Both green. |
| WR-02 | Warning | **FIXED** | Added test `IdentityEdit_AddOption_SetNewAsCorrect_AddsAndMarksCorrect` — add new (null-Id) option + `correctIndex` pointing at the new row → new option ADDed and marked correct (Id new), A..D no longer correct. Closes coverage gap. |
| WR-01 | Warning | **ACCEPTED (no code change)** | Id-non-null + blank-text row → option enters removedOptionIds → silent delete if unanswered. This **matches prior behavior** (positional rule + import "kosong diabaikan") and is guarded if answered. In the form, deleting = removing the row (not blanking). Pre-existing semantics, not a regression. Documented; no change. |
| WR-03 | Warning | **ACCEPTED (already handled)** | populateEditForm padding rows: `addOptionRow` clone-reset clears `type='hidden'` (PATCH D), and `ensureRowCount` uses `addOptionRow`, so padding rows already get empty Id. Reviewer acknowledged padding rows ARE cleared. No defensive change needed. |
| IN-01, IN-02 | Info | Noted | No action. |

**Security checks (review):** all core passed — anti-tamper BEFORE mutation (D-01a), RBAC + antiforgery preserved, kill-drift met (guard + upsert share `existingIds.Except(keptIds)` / `keptIds.Contains`), Essay branch correct, CreateQuestion unaffected, JS clone-reset handles hidden Id, reletter preserves value, no XSS.

**Verification (gsd-verifier):** 5/6 must-haves verified by code+integration tests; 6th = Playwright real-browser UAT (VRF-01, Plan 03 Task 2, `autonomous:false`) — PENDING (planned gate, not a failure).
