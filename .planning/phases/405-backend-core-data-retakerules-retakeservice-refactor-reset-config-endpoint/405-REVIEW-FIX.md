---
phase: 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
fixed_at: 2026-06-21T00:00:00Z
review_path: .planning/phases/405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint/405-REVIEW.md
iteration: 1
findings_in_scope: 3
fixed: 3
skipped: 0
status: all_fixed
---

# Phase 405: Code Review Fix Report

**Fixed at:** 2026-06-21
**Source review:** .planning/phases/405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint/405-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 3 (WR-01, WR-02, WR-03 — all WARNING; the 4 INFO items IR-01..04 are out of scope for `critical_warning`)
- Fixed: 3
- Skipped: 0

All three warnings live in `Services/RetakeService.ExecuteAsync` and are interdependent (the deferred
AttemptHistory-insert that resolves WR-03 also removes the WR-02 split-commit and provides the
anti-double-archive guarantee that WR-01's Open-success path relies on). The fix guidance itself
directed pairing WR-01 with WR-03 and noted WR-03 option (a) "also resolves WR-02's split commit".
Because the three edits interleave in the same method body and cannot be committed separately without
leaving a non-compiling / failing intermediate state, they were applied as one coherent atomic commit
that names all three findings. Per-finding rationale is documented individually below.

## Fixed Issues

### WR-01: HC reset of an `Open`-status session now fails (behavioral regression)

**Files modified:** `Services/RetakeService.cs`, `HcPortal.Tests/RetakeServiceTests.cs`
**Commit:** 4e538ee4
**Status:** fixed: requires human verification (behavioral-contract change — confirm UX of Open-reset no-op)

**Applied fix:** Aligned the service's resettable-status set with the controller's
(`ResetAssessment` :4234-4235 permits Open/InProgress/Completed/Abandoned, rejects Cancelled).
Before the claim, the service now:
- Rejects only `Cancelled` explicitly (matching the controller).
- Short-circuits `Status == "Open"` to a **successful no-op** (`RetakeResult(true, null)`) — an Open
  session (assigned-not-started or just-reset) has no score/responses to archive or delete, so the prior
  "Sesi tidak dapat direset (sudah terbuka)" error was a spurious failure for an HC-permitted action.

The double-click / re-claim race remains protected: the claim `WHERE Status NOT IN (Cancelled, Open)`
is unchanged, and if a concurrent connection has already flipped the row to Open between load and claim
(`rows == 0`), the transaction is rolled back and a success no-op is returned (no second archive). The
worker-retake path (`CanRetakeAsync` → `RetakeRules.CanRetake` requires `Status == "Completed"`) is
untouched.

**Logic-change note (human verification flag):** This changes an observable contract — resetting an
already-Open session now reports success instead of an error. The load-bearing invariant
(`histCount == 1`, no double-archive) is preserved and tested, but the success-vs-error behavior is a
semantic decision that warrants a human confirming the intended UX before merge.

### WR-02: `ExecuteAsync` spanned three independent commits — partial failure left orphan history + undeleted responses

**Files modified:** `Services/RetakeService.cs`
**Commit:** 4e538ee4

**Applied fix:** Wrapped the mutating sequence — claim (`ExecuteUpdateAsync`) → AttemptHistory insert →
snapshot `AddRange` → response/assignment/ET-score deletes — in a single explicit transaction
(`await using var tx = await _context.Database.BeginTransactionAsync();` … `await tx.CommitAsync();`),
mirroring the bulk-assign pattern at `AssessmentAdminController.cs:2196`. `ExecuteUpdateAsync` enlists in
the explicit transaction on the same connection, so a mid-operation failure now rolls the whole unit
back instead of leaving a claimed (Open/Score=null) session with a childless AttemptHistory and surviving
`PackageUserResponses`. Claim-atomic-FIRST ordering and snapshot-before-delete are preserved (the
intra-transaction `SaveChangesAsync` that materializes `attemptHistory.Id` still runs before the builder,
and the archive `AddRange` is staged before the `RemoveRange` flush). Audit logging and the SignalR
`sessionReset` broadcast remain OUTSIDE the committed transaction (warn-only side effects that must not
roll back a successful reset).

### WR-03: Completed session with missing/empty assignment committed a childless AttemptHistory

**Files modified:** `Services/RetakeService.cs`, `HcPortal.Tests/RetakeServiceTests.cs`
**Commit:** 4e538ee4

**Applied fix:** Applied review option (a) — deferred the `AttemptHistory` insert until questions are
proven non-empty. The question/response load and `questions.Count > 0` check now run *before* the
AttemptHistory is created; the era-retake counting, the `AttemptHistory.Add` + `SaveChangesAsync`, and
the snapshot `AddRange` all moved inside the `if (questions.Count > 0)` block. A Completed session whose
`UserPackageAssignment` is null or whose `ShuffledQuestionIds` deserializes to `[]`
(`GetShuffledQuestionIds()` swallows JSON errors → `[]`) now produces no AttemptHistory row at all — no
childless orphan persists. D-01 counting is unaffected: it counts only AttemptHistory rows that have
child archives (`EXISTS child`), and childless rows are no longer created in the first place, so the cap
math is unchanged. Essay full-text, MC/MA `BuildAnswerCell`, the `(UserId, Title, Category)` counting
key, and the snapshot-before-delete order are all preserved.

## Verification Evidence

**Build:** `dotnet build` → **0 Error(s)**, 25 Warning(s) (all pre-existing, unrelated to the edited
files; the edited `RetakeService.cs` / `RetakeServiceTests.cs` produced no new warnings).

**Tests:**
- `dotnet test --filter "FullyQualifiedName~RetakeServiceTests"` (real SQLEXPRESS,
  `localhost\SQLEXPRESS`, Development) → **Passed 7 / Failed 0 / Skipped 0** (was 5; +2 new tests).
- `dotnet test --filter "FullyQualifiedName~ResetGuard"` → **Passed 2 / Failed 0**.
- `dotnet test --filter "FullyQualifiedName~Retake"` (RetakeRules + RetakeArchiveBuilder + RetakeService)
  → **Passed 28 / Failed 0**.

The four pre-existing invariant tests still pass after the transaction-wrap + Open-handling +
childless-skip changes: `Snapshot_WrittenBeforeResponsesDeleted` (snapshot-before-delete),
`CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap` (D-01 legacy-no-count),
`CanRetake_RetakeEraArchiveWithSnapshot_ConsumesCap` and `Counting_PrePostSameTitle_NoConflate`
(D-01 counting / anti-conflation).

**Test changes (justified):**
- `Claim_DoubleExecute_SecondAborts` → renamed/repurposed to `Claim_DoubleExecute_NoSecondArchive`.
  The old test asserted the second execute returns `Success == false` with an error — that encoded the
  *old buggy* WR-01 behavior (Open excluded from the resettable set). The load-bearing assertion
  (`histCount == 1`, no second archive) is retained; the success-flag assertion was updated to the new
  WR-01 contract (resetting an already-Open session is a successful no-op). This is the
  "test encoded the old buggy behavior" case the fixer guidance permits — justified here.
- Added `Execute_OpenSession_SuccessNoArchive` (WR-01: HC reset of a legitimately Open session returns
  success and creates no AttemptHistory) — the new test the post-fix verification explicitly requested.
- Added `Execute_CompletedNoAssignment_NoChildlessHistory` (WR-03: a Completed session with no
  assignment/questions resets to Open but persists no childless AttemptHistory).

## Skipped Issues

None — all in-scope findings were fixed.

---

_Fixed: 2026-06-21_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
