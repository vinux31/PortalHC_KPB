---
phase: 414-fix-admin-hc-answer-history-visibility-when-allowanswerrevie
reviewed: 2026-06-22T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - Models/AssessmentResultsViewModel.cs
  - Controllers/CMPController.cs
  - Views/CMP/Results.cshtml
  - HcPortal.Tests/CanReviewAnswersTests.cs
findings:
  critical: 0
  warning: 0
  info: 1
  total: 1
status: clean
---

# Phase 414: Code Review Report

**Reviewed:** 2026-06-22
**Depth:** standard
**Files Reviewed:** 4
**Status:** clean

## Summary

Reviewed only the Phase 414 diff (vs `fe0fc15c^`): the `CanReviewAnswers` static helper + its wiring in the `Results` action, the new VM field, the Results.cshtml gate/alert/admin-note edits, and the new pure-static test. The change decouples the per-question "Tinjauan Jawaban" DISPLAY gate from the `AllowAnswerReview` toggle for non-owners (Admin/HC/L3/L4-section). The implementation is correct, access-safe, and the anti-desync invariant holds. No Critical or Warning findings. One Info-level observation about a redundant boolean term in the admin-note conditional (cosmetic, not a bug).

### Verification of focus areas

- **Boolean logic** — `CanReviewAnswers(allow, isOwner) => allow || !isOwner` is correct across all 4 cases: (F,F)=true, (T,F)=true, (F,T)=false, (T,T)=true. Matches the documented intent and the test matrix exactly. Owner stays gated, non-owner always sees review.
- **Owner detection (impersonation-aware)** — `isOwner = assessment.UserId == user.Id` (CMPController.cs:2230) uses the EFFECTIVE `user` from `GetCurrentUserRoleLevelAsync()` (impersonation-aware via `_impersonationService.GetEffectiveUserAsync`), and is the SAME `user.Id` already passed to `IsResultsAuthorized` (line 2218, `ownerUserId == currentUserId` at line 2535). No re-fetch of `_userManager.GetUserAsync(User)` — Pitfall 4 avoided. Owner detection is consistent with the upstream auth check.
- **Anti-desync invariant** — Satisfied. A single `canReviewAnswers` is computed once at line 2231 (after `IsResultsAuthorized` + `Forbid`) and used at BOTH the gate-build `if (canReviewAnswers)` (line 2271) and the VM flag `CanReviewAnswers = canReviewAnswers` (line 2385). No divergent recomputation.
- **Access safety** — The loosening is DISPLAY-only. Access is still gated upstream by `IsResultsAuthorized` (line 2218) + `Forbid()` (line 2219), unchanged. A non-owner reaching the review build has already passed the authorization gate.
- **Legacy/empty path** — The no-package branch (lines 2400-2420) hardcodes both `AllowAnswerReview = false` and `CanReviewAnswers = false`, and `QuestionReviews = null`. The view's `else if (!Model.CanReviewAnswers)` (Results.cshtml:420) then renders the "tidak tersedia" alert — consistent. No leak path.
- **XSS in admin-note Razor block** — Safe. Results.cshtml:322-328 emits only static Bahasa Indonesia literal text via Razor implicit encoding. No `@Html.Raw`, no user/model string interpolation in the note. T-414-02 holds.
- **Test correctness** — `CanReviewAnswersTests` is pure static (no DB, no `WebApplicationFactory`), follows the `ResultsAuthorizationTests` convention (`using HcPortal.Controllers; using Xunit;`, same namespace). The `[Theory]` matrix covers all 4 (allow × isOwner) combinations with correct expected values and SC-tagged comments. The helper is `public static`, accessible from the test project (which has `ProjectReference` to `HcPortal.csproj`); no `InternalsVisibleTo` needed.

## Info

### IN-01: Redundant `Model.CanReviewAnswers` term in admin-note guard

**File:** `Views/CMP/Results.cshtml:323`
**Issue:** The admin-note block is nested inside `@if (Model.CanReviewAnswers && Model.QuestionReviews != null)` (line 316), then re-checks `@if (Model.CanReviewAnswers && !Model.AllowAnswerReview)` (line 323). The `Model.CanReviewAnswers` term in the inner condition is always true at that point, so it is redundant — the inner guard effectively reduces to `!Model.AllowAnswerReview`. This is purely cosmetic and does NOT cause a bug: when `CanReviewAnswers` is true and `AllowAnswerReview` is false, the only way to reach that state is the non-owner bypass, so the note's intent ("review shown only because of non-owner bypass") is correctly conveyed.
**Fix:** Optional simplification for readability — `@if (!Model.AllowAnswerReview)`. Keeping the explicit `Model.CanReviewAnswers &&` is also defensible as self-documenting intent; leave as-is if preferred. No action required.

---

_Reviewed: 2026-06-22_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
