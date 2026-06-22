---
phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja
reviewed: 2026-06-22T00:00:00Z
depth: standard
files_reviewed: 10
files_reviewed_list:
  - Controllers/CMPController.cs
  - Helpers/RetakeRules.cs
  - Models/AllWorkersHistoryRow.cs
  - Models/AssessmentResultsViewModel.cs
  - Views/CMP/Results.cshtml
  - Views/CMP/_RiwayatPekerja.cshtml
  - HcPortal.Tests/RetakeExamEndpointTests.cs
  - HcPortal.Tests/RetakeRulesTests.cs
  - tests/e2e/retake-worker-407.spec.ts
  - tests/sql/retake-worker-407-seed.sql
findings:
  critical: 0
  warning: 1
  info: 3
  total: 4
status: issues_found
---

# Phase 407: Code Review Report

**Reviewed:** 2026-06-22
**Depth:** standard
**Files Reviewed:** 10
**Status:** issues_found

## Summary

Phase 407 (worker self-service retake, milestone v32.4) was reviewed at standard depth with primary focus on the security-critical leak-safety surface and the retake endpoint. The implementation is strong:

- **LEAK-SAFETY (verified clean):** `Views/CMP/Results.cshtml` correctly partitions the answer-key rendering. The answer key (`list-group-item-success`, `(Jawaban Benar)`, essay `CorrectAnswer`) is rendered ONLY in the `ShowFullReview` switch arm. The `ShowWrongFlagsOnly` arm renders verdict badge + `UserAnswer` only — no correct-option highlight, no key label, no `CorrectAnswer`/rubric. The Riwayat partial (`_RiwayatPekerja.cshtml`) and its data source (`RetakeArchiveBuilder` / `AssessmentAttemptResponseArchive`) only carry `AnswerText` (worker's own answer) + `IsCorrect` verdict — no answer key is stored or rendered even when `HideDetail` is false. The server is authoritative (`RetakeRules.ResolveReviewMode`), and the e2e spec asserts the leak-safe DOM with unique key sentinels (`KUNCIBENAR_*`).
- **`RetakeRules.ResolveReviewMode` truth table (verified correct & total):** `!allowReview → ScoreOnly`; `isPassed != true && attemptsRemaining → ShowWrongFlagsOnly` (treats pending `null` identically to failed, preventing key-leak on a question that may be retaken); else `ShowFullReview` (passed, or failed/pending with attempts exhausted). All three states and the `bool? × bool` input space are covered by `RetakeRulesTests`.
- **`RetakeExam` endpoint (verified):** `[ValidateAntiForgeryToken]` present, ownership `Forbid()` IDOR guard runs BEFORE any mutation, server-side `CanRetakeAsync` re-check, TempData token cleared on success, redirect to `StartExam`. Double-submit/race is defended in `RetakeService.ExecuteAsync` (atomic claim + transaction + Open no-op). Endpoint tests cover non-owner, not-eligible, and success paths.
- **XSS:** No `Html.Raw` of user content. The two `Html.Raw(Json.Serialize(...))` usages emit Elemen-Teknis names into a `<script>` block via JSON-encoding — the safe canonical pattern.

One Warning (dead/unreachable view branch causing a UX gap during cooldown) and three Info items below.

## Warnings

### WR-01: Cooldown-disabled retake button is unreachable — no button rendered during active cooldown

**File:** `Views/CMP/Results.cshtml:497-513`
**Issue:** The retake control renders the button only inside `else if (Model.CanRetake)`. Within that block, the cooldown-countdown variant (the disabled button with `data-cooldown-until` + `#retakeCountdown`, lines 500-505) requires `Model.CooldownUntilUtc > DateTime.UtcNow`. But `Model.CanRetake = await _retakeService.CanRetakeAsync(id)` (CMPController.cs:2485), and `CanRetakeAsync` delegates to the pure `RetakeRules.CanRetake`, which returns **false** whenever the cooldown has not elapsed (`RetakeRules.cs:49-51`). Therefore, during an active cooldown, `Model.CanRetake` is false and `Model.IsCapReached` is also false (cap not reached), so **neither** the cap-lock alert nor the button block renders — the worker sees no retake affordance and no countdown at all. The countdown branch at lines 500-505 (and its companion JS at lines 565-600) is effectively dead code under current semantics.

This is a UX/correctness gap, not a security issue: server authority is intact (a real retake POST during cooldown is still rejected by `CanRetakeAsync`). The phase's own seed comment (`tests/sql/retake-worker-407-seed.sql:24-27`) and e2e spec (`tests/e2e/retake-worker-407.spec.ts:184-197`) already anticipate this by asserting the countdown "only when tombol hadir" — confirming the button may legitimately be absent, which means scenario 6 can pass vacuously without ever exercising the countdown.

**Fix:** Decide the intended behavior and make the view and `CanRetake` semantics agree. Option A — populate a separate flag so the cooldown button renders while still gating the POST. Compute eligibility ignoring cooldown for rendering, and keep `CanRetakeAsync` for the actual POST guard:
```csharp
// CMPController.Results — render-time eligibility that INCLUDES cooldown-pending as "show disabled button"
bool eligibleIgnoringCooldown = assessment.AllowRetake
    && !RetakeRules.ShouldHideRetakeToggle(assessment.AssessmentType, assessment.IsManualEntry)
    && assessment.Status == "Completed"
    && assessment.IsPassed == false
    && currentAttempt < assessment.MaxAttempts;
viewModel.CanRetake = eligibleIgnoringCooldown;   // view renders button (disabled while CooldownUntilUtc > now)
```
The cooldown-disabled vs enabled state is then driven purely by `CooldownUntilUtc` in the view (already implemented), and `RetakeExam` keeps the authoritative `CanRetakeAsync` re-check. Alternatively (Option B), if the product intent is to hide the button entirely during cooldown, remove the now-dead countdown branch (Results.cshtml:500-505) and the countdown `<script>` (lines 564-600) to avoid maintaining unreachable code.

## Info

### IN-01: Scenario 6 (cooldown countdown) can pass without exercising the countdown

**File:** `tests/e2e/retake-worker-407.spec.ts:184-197`
**Issue:** Because of WR-01, the cooldown button is not rendered, so `btnCount > 0` is false and the entire countdown assertion block is skipped. The test then only asserts "no JS error," so RTK-10's cooldown countdown (the `data-cooldown-until` → `#retakeCountdown` ticking behavior and its anti-ReferenceError guard from lesson 413) is never actually verified by an executed assertion.
**Fix:** After resolving WR-01 so the disabled cooldown button renders, tighten the spec to require the button to be present for the cooldown seed (`sidC`) and drop the conditional `if (btnCount > 0)`, so the countdown path is genuinely covered.

### IN-02: `IsCapReached` excludes pending (`IsPassed == null`) — confirm intended

**File:** `Controllers/CMPController.cs:2489`
**Issue:** `viewModel.IsCapReached = assessment.IsPassed == false && assessment.AllowRetake && currentAttempt >= assessment.MaxAttempts;`. The `IsPassed == false` term means a session whose grading is still pending (`IsPassed == null`) with attempts exhausted will NOT show the "Batas percobaan tercapai" lock alert. In that state, `attemptsRemaining` is also false (`currentAttempt >= MaxAttempts`), so `RetakeMode` resolves to `ShowFullReview` and the retake-control block renders nothing (neither cap-lock nor button). This is internally consistent and not a leak (full review for an exhausted attempt is allowed by design), but the missing lock messaging for a pending+exhausted session is a minor UX inconsistency worth a deliberate decision.
**Fix:** If the lock messaging should also apply to pending+exhausted, broaden the guard to `assessment.IsPassed != true` to mirror the `ResolveReviewMode` concealment semantics; otherwise leave as-is and note the intentional exclusion.

### IN-03: `currentAttempt` counting query duplicated between controller and `RetakeService`

**File:** `Controllers/CMPController.cs:2472-2475`
**Issue:** The `eraRetakeArchives` count (AttemptHistory joined to AttemptResponseArchives by UserId+Title+Category, snapshot-presence per D-01) is hand-rolled in `Results` and is an exact copy of the same query in `RetakeService.CanRetakeAsync` (RetakeService.cs:237-242) and `ExecuteAsync` (RetakeService.cs:145-150). Three copies of the cap-counting rule risk drift if the D-01 snapshot-presence definition ever changes (e.g., a future filter on archive type). The code comment at CMPController.cs:2470 already flags it as a "mirror," acknowledging the duplication.
**Fix:** Extract the count into a single reusable method (e.g., `RetakeService.CountEraRetakeArchivesAsync(userId, title, category)`) and call it from all three sites, keeping the cap rule single-source like the rest of the v32.4 kill-drift design (`RetakeRules`, `RiwayatUnifier`, `RetakeArchiveBuilder`).

---

_Reviewed: 2026-06-22_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
