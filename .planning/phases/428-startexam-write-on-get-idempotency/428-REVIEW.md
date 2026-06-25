---
phase: 428-startexam-write-on-get-idempotency
reviewed: 2026-06-25T00:00:00Z
depth: standard
files_reviewed: 2
files_reviewed_list:
  - Controllers/CMPController.cs
  - HcPortal.Tests/StartExamIdempotencyTests.cs
findings:
  critical: 0
  warning: 0
  info: 3
  total: 3
status: issues_found
---

# Phase 428: Code Review Report

**Reviewed:** 2026-06-25T00:00:00Z
**Depth:** standard
**Files Reviewed:** 2
**Status:** issues_found (3 Info only — no Critical, no Warning)

## Summary

Reviewed the surgical refactor of `GET CMP/StartExam(id)` (EXSEC-02) that removes the write-on-GET `Upcoming -> Open` persist and replaces it with an in-memory effective-status time-gate, plus the new 6-test real-SQL idempotency suite.

**The refactor is correct and behaviorally sound.** Key verifications:

- **Time-gate equivalence at the boundary.** Old logic: auto-transition when `Schedule <= nowWib`, then block any remaining `Status == "Upcoming"`. New logic: block only `Status == "Upcoming" && Schedule > nowWib`. These are equivalent — an Upcoming session whose time has arrived (`Schedule <= nowWib`) is no longer blocked (openable), an Upcoming session not-yet-time (`Schedule > nowWib`) is still blocked, and the exact boundary `Schedule == nowWib` is openable in both old and new code (`> nowWib` is false). Confirmed against the time-gate at `Controllers/CMPController.cs:929`.

- **No DB write for the transition.** The removed block was the only `SaveChangesAsync` tied to the status transition. The new code computes `nowWib` and gates in-memory only. Confirmed no remaining transition write between authz (`:920`) and the justStarted write (`:1017`).

- **Downstream `assessment.Status` readers are unaffected.** Grepped all `assessment.Status` references in StartExam (`:929`, `:935`, `:983`, `:1017`). Between the edited block and the justStarted write, nothing branches on `Status == "Open"`. The justStarted write sets `Status = "InProgress"` directly (`:1017`), not transitioning from "Open". So leaving Status as "Upcoming" in-memory (the impersonation path) does not misbehave — the old in-memory "Open" value was already dead downstream within StartExam. **The impersonation path is safe.**

- **D-01 scope preserved.** justStarted InProgress write (`:1015-1020`) and assignment-create (`:1100-1117`) remain on GET, both correctly guarded by `!_impersonationService.IsImpersonating()`. Token-gate EXSEC-01 (`:958-966`) and GRDF-01 (`:944-953`) are untouched and ordered after the time-gate / Completed check, before the StartedAt write — exactly as documented.

**Test quality is high.** The suite uses real SQL (disposable DB via `RetakeServiceFixture`), and every status/idempotency assertion is made against a **reloaded DB row** (`ReloadStatusAsync` opens a fresh `NewCtx()`), not the in-memory tracked object — so it genuinely proves no write-on-GET. The impersonation vector is the correct choice: it is the only non-starting owner GET (justStarted write is `!IsImpersonating()`-guarded), so "Status DB stays Upcoming after GET" is real evidence of idempotency (T1/T2). T5 proves the worker-start path (InProgress + StartedAt + assignment created), and T3/T4/T6 prove the time-gate, GRDF-01, and token-gate (427 regression) respectively. No false-confidence patterns found.

Findings below are all Info-level (no behavior risk in this phase).

## Info

### IN-01: Idempotency coverage proves no-write, but not "stable identical render" across double-GET

**File:** `HcPortal.Tests/StartExamIdempotencyTests.cs:284-301`
**Issue:** T2 (`DoubleGet_StatusStaysUpcoming`) asserts both calls return `ViewResult` and the DB Status stays "Upcoming". This proves the transition is idempotent at the persistence layer. It does not assert that the two renders are equivalent (e.g., same assignment, no duplicate `UserPackageAssignment` rows on the impersonation path). Because impersonation skips the assignment persist (`:1100`), a duplicate row cannot occur here, so this is not a correctness gap for the current code — only an observation that double-GET equivalence is proven for persistence, not for full render determinism. A non-impersonate double-GET (resume) would exercise the assignment-resume idempotency (`:1064-1067`), which is out of this phase's scope.
**Fix:** Optional — no change required. If broader idempotency confidence is wanted later, add a non-impersonate resume test asserting `UserPackageAssignments.Count(...) == 1` after two GETs. Defer to backlog; not needed for EXSEC-02.

### IN-02: Test `TimeArrived` uses a fixed past date that will not age out, but relies on Unspecified DateTimeKind

**File:** `HcPortal.Tests/StartExamIdempotencyTests.cs:211`
**Issue:** `TimeArrived = new DateTime(2026, 2, 1)` is a `DateTimeKind.Unspecified` literal compared in-controller against `nowWib = DateTime.UtcNow.AddHours(7)`. The comparison is correct today (Feb 2026 is comfortably in the past vs Jun 2026), and matches how the product stores `Schedule` (WIB-naive, Unspecified). The only latent concern is that a hardcoded calendar date will eventually no longer be "well in the past" if these tests are run far in the future — but that horizon is years away and the schedule semantics are WIB-naive throughout, so there is no Kind mismatch bug. `TimeFuture` correctly uses a relative `DateTime.UtcNow.AddHours(7).AddDays(7)`.
**Fix:** Optional — consider `TimeArrived => DateTime.UtcNow.AddHours(7).AddDays(-7)` for symmetry with `TimeFuture` and to make it permanently relative. Cosmetic; current value is correct for the foreseeable future.

### IN-03: `StubUrlHelper.Action` returns a constant unrelated to the requested action

**File:** `HcPortal.Tests/StartExamIdempotencyTests.cs:188`
**Issue:** `Action(UrlActionContext)` always returns `"/CMP/StartExam"` regardless of the action requested. In StartExam the only `Url.Action` consumer reached on the success path is incidental (the view is not rendered in unit context), and the redirect assertions check `RedirectToActionResult.ActionName` ("Assessment") directly rather than a generated URL, so the stub's constant return value does not weaken any assertion. It is a harmless test double, but the constant could mislead a future reader into thinking the URL is asserted.
**Fix:** Optional — return `actionContext.Action` (or `$"/CMP/{actionContext.Action}"`) so the stub reflects the requested action. No behavioral impact on current tests.

---

_Reviewed: 2026-06-25T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
