---
phase: 427-exam-token-gate-server-authoritative
reviewed: 2026-06-25T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - Models/AssessmentSession.cs
  - Controllers/CMPController.cs
  - Controllers/AssessmentAdminController.cs
  - Services/RetakeService.cs
  - Migrations/20260624133656_AddTokenVerifiedAt.cs
  - HcPortal.Tests/TokenVerifiedAtTests.cs
findings:
  critical: 0
  warning: 1
  info: 1
  total: 2
status: issues_found
---

# Phase 427: Code Review Report

**Reviewed:** 2026-06-25T00:00:00Z
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

Reviewed the Phase 427 token-gate server-authoritative hardening (EXSEC-01). The core
security objective is met cleanly: the StartExam gate now reads the persisted
`AssessmentSession.TokenVerifiedAt` column instead of `TempData.Peek`, removing the
client-round-trippable trust path. The verdict on each focus area:

- **DB-read gate vs TempData round-trip** — CORRECT. `CMPController.StartExam:967` reads
  `assessment.TokenVerifiedAt == null` from the DB-loaded entity. No client-trusted value
  path remains in the gate.
- **Stamp only on token-required success path** — CORRECT. `VerifyToken:902` stamps
  `TokenVerifiedAt = DateTime.UtcNow` only after `IsTokenRequired` + valid-token checks
  pass. The not-required branch (`:886-892`) intentionally leaves the column null, which is
  semantically safe because the gate only evaluates the column when `IsTokenRequired==true`.
- **Single-source reset across both retake paths** — CORRECT. `RetakeService.ExecuteAsync`
  resets `TokenVerifiedAt = null` inside the `ExecuteUpdateAsync` SetProperty chain
  (`:127`). Worker `CMPController.RetakeExam:2580` and HC `AssessmentAdminController
  .ResetAssessment:4392` both delegate to this service, so no reset path is missed. The
  early-return paths (`Cancelled` error, `Open` no-op `:91`, window-closed `:98`) do not
  leave an exploitable orphan stamp: a stamped session is necessarily InProgress
  (StartedAt set) by the time it is stamped, not Open — and `Cancelled`/window-closed
  sessions are blocked by StartExam regardless. Other `Status = "Open"` writes
  (CMPController:250 display-only, :928 fresh Upcoming->Open, AssessmentAdminController:1160
  new-session default) cannot resurrect a previously-stamped session.
- **`StartedAt == null` legacy guard** — PRESERVED (`StartExam:964`). Legacy InProgress
  sessions (StartedAt set, TokenVerifiedAt NULL post-migration) bypass the gate, so no
  post-deploy lockout. Covered by test T5.
- **No remaining access-token TempData** — CONFIRMED. Grep across Controllers/ + Services/
  for the `TokenVerified_` gate key returns 0. The surviving `TempData[...Token...]` hits
  are all `AutoSubmitToken_` (timer-expiry auto-submit), a distinct concern.
- **Migration correctness** — CORRECT. Nullable `datetime2` column, no data loss, existing
  rows default to NULL, clean `Down()` drop. Model snapshot matches (`DateTime?`,
  non-required).

One stale test in a *non-changed* file is now broken by this phase's controller change
(WR-01), and one cosmetic comment-staleness item (IN-01). Neither affects the production
security posture.

## Warnings

### WR-01: Stale test asserts removal of a TempData token key the controller no longer touches

**File:** `HcPortal.Tests/RetakeExamEndpointTests.cs:223,229`
**Issue:** Phase 427 removed `TempData.Remove($"TokenVerified_{id}")` from
`CMPController.RetakeExam` (replaced with a comment at `CMPController.cs:2587-2588`), moving
the token re-arm to `RetakeService.ExecuteAsync` (DB column). However
`RetakeExamEndpointTests.cs` was not updated in this phase (last touched in Phase 407,
commit `af19a643`). The test `RetakeExam_Success_ClearsTokenAndRedirectsToStartExam`
seeds the key at line 223:

```csharp
tempData[$"TokenVerified_{sessionId}"] = true;
```

then asserts it was removed at line 229:

```csharp
Assert.False(tempData.ContainsKey($"TokenVerified_{sessionId}"));   // token TERHAPUS — re-arm lobby
```

Because the controller no longer removes that key, the seeded key persists and this
assertion evaluates `Assert.False(true)` — the test now FAILS. This is a regression in the
test suite introduced by an otherwise-correct production change, and it contradicts the new
server-authoritative model (the test still encodes the obsolete TempData re-arm
expectation). It will surface as a red `[Trait("Category","Integration")]` test on the next
real-SQL run.

**Fix:** Update the test to reflect server-authoritative re-arm. Remove the obsolete
TempData seed/assert and verify the DB column instead, e.g.:

```csharp
// Seed a stamped token on the session (server-authoritative state).
// (set TokenVerifiedAt on the seeded session before RetakeExam)

var result = await ctrl.RetakeExam(sessionId);

var redirect = Assert.IsType<RedirectToActionResult>(result);
Assert.Equal("StartExam", redirect.ActionName);

await using var verify = NewCtx();
var row = await verify.AssessmentSessions
    .Where(a => a.Id == sessionId)
    .Select(a => new { a.Status, a.TokenVerifiedAt })
    .SingleAsync();
Assert.Equal("Open", row.Status);
Assert.Null(row.TokenVerifiedAt);   // re-arm: gate will re-prompt on new attempt
```

Also update the file header comment (`:5`) which still documents the TempData expectation.
(Note: T4 in `TokenVerifiedAtTests.cs` already proves the column reset, so the
RetakeExamEndpointTests assertion can simply be dropped if redundancy is undesired — but it
must not remain as-is.)

## Info

### IN-01: Comment in CMPController VerifyToken references "gantikan TempData" but no TempData write exists to replace inline

**File:** `Controllers/CMPController.cs:889,901`
**Issue:** The EXSEC-01 comments ("gantikan TempData", "Tak ada TempData token untuk
dibersihkan") are accurate and helpful for traceability, but a future reader grepping for
the removed `TempData[$"TokenVerified_..."]` writes will find only prose, not code. This is
purely a maintainability nicety — the code itself is correct and the comments correctly
document the migration away from TempData. Consider, at the next milestone cleanup, trimming
these to a single canonical reference (e.g. a one-line "EXSEC-01: server-authoritative via
TokenVerifiedAt column") to avoid comment drift across the four touch sites
(CMPController:889, :901, :2587; AssessmentAdminController:4409).
**Fix:** Optional — consolidate the repeated EXSEC-01 rationale comments; no behavior change.

---

_Reviewed: 2026-06-25T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
