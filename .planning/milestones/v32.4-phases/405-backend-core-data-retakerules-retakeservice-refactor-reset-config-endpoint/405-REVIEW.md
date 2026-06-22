---
phase: 405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint
reviewed: 2026-06-21T08:12:27Z
depth: standard
files_reviewed: 12
files_reviewed_list:
  - Helpers/RetakeRules.cs
  - Helpers/RetakeArchiveBuilder.cs
  - Services/RetakeService.cs
  - Controllers/AssessmentAdminController.cs
  - Models/AssessmentAttemptResponseArchive.cs
  - Models/AssessmentSession.cs
  - Data/ApplicationDbContext.cs
  - Program.cs
  - Migrations/20260621065918_AddRetakeColumnsAndArchive.cs
  - HcPortal.Tests/RetakeRulesTests.cs
  - HcPortal.Tests/RetakeArchiveBuilderTests.cs
  - HcPortal.Tests/RetakeServiceTests.cs
findings:
  critical: 0
  warning: 3
  info: 4
  total: 7
status: issues_found
---

# Phase 405: Code Review Report

**Reviewed:** 2026-06-21T08:12:27Z
**Depth:** standard
**Files Reviewed:** 12
**Status:** issues_found

## Summary

Phase 405 backend core for v32.4 Ujian Ulang (retake). Reviewed the two pure helpers
(`RetakeRules`, `RetakeArchiveBuilder`), the `RetakeService` engine, the controller wiring
(`ResetAssessment` delegation + new `UpdateRetakeSettings` + bulk-add carry), the new model +
EF migration, and all three test files.

**Overall assessment: strong.** The high-risk correctness invariants the phase brief flags are
all satisfied:

- Claim-atomic (`ExecuteUpdateAsync` + `rows==0` abort) runs FIRST, before any snapshot/archive
  — anti double-archive verified, and `Status != "Open"` correctly prevents re-claim of a
  request already in flight (covered by `Claim_DoubleExecute_SecondAborts`).
- Snapshot (`RetakeArchiveBuilder.Build`) is staged BEFORE `PackageUserResponses.RemoveRange`
  (verified by `Snapshot_WrittenBeforeResponsesDeleted`).
- D-01 era-retake counting uses `EXISTS child archive` so legacy HC-reset histories do NOT
  consume cap, and counting key is `(UserId, Title, Category)` — anti-conflation Pre/Post
  (tests 3/4/5 lock both behaviors).
- Essay archived FULL-TEXT (no 300-char truncate); MC/MA use `BuildAnswerCell`. Verdict via
  the central `IsQuestionCorrect` aggregator (kill-drift).
- `UpdateRetakeSettings` carries `[Authorize(Admin,HC)]` + `[ValidateAntiForgeryToken]` +
  `Math.Clamp(1..5 / 0..168)` + sibling propagation + audit (warn-only).
- `ResetAssessment` HC guards stay in the controller; `TempData.Remove($"TokenVerified_{id}")`
  fires after success. Audit + SignalR failures are swallowed warn-only (reset stays committed).
  Cooldown and all timestamps use `DateTime.UtcNow` consistently.
- Migration: `bit`/`int` columns with `defaultValue: false/2/24`, cascade FK + index on
  `AttemptHistoryId`, snapshot ProductVersion 8.0.0, latest in the chain after
  `AddUserUnitsTable`. No competing migration.

Findings below are all non-Critical. The two notable ones are a behavioral regression for HC
reset of `Open`-status sessions (WR-01) and a multi-statement atomicity gap in `ExecuteAsync`
(WR-02). Neither blocks correctness of the happy path that the tests exercise, but both deserve
a decision before merge.

## Warnings

### WR-01: HC reset of an `Open`-status session now fails (behavioral regression)

**File:** `Services/RetakeService.cs:76` (claim guard) vs `Controllers/AssessmentAdminController.cs:4234-4235`
**Issue:** The controller explicitly permits reset for any active status — its own comment reads
*"Reset is valid for any active status (Open, InProgress, Completed, Abandoned)"* and the guard
at line 4235 lets an `Open` session through. But the service claim guard excludes `Open`
(`s.Status != "Open"`), so `ExecuteUpdateAsync` returns `rows == 0` and the service aborts with
*"Sesi tidak dapat direset (sudah dibatalkan atau sudah terbuka)."* Net effect: an HC who clicks
Reset on a session that is legitimately in `Open` status (e.g. assigned-but-not-started, or a
previously-reset session) now gets an error instead of the prior success. The `Open` exclusion
is needed as the double-click claim guard, but it also swallows this legitimate single-click case.
The two layers disagree on the resettable-status set.
**Fix:** Decide the intended contract and make the layers agree. If resetting an already-`Open`
session should be a successful no-op (it has no responses/score to clear), keep the `Open`
exclusion for the double-click race but special-case it in the controller/service so it returns
success rather than the "sudah terbuka" error — e.g. detect `wasCompleted == false && status ==
"Open"` and short-circuit to a successful result (clear token, broadcast) without treating
`rows == 0` as failure:
```csharp
// after loading assessment, before claim:
if (assessment.Status == "Open")
    // nothing to archive/delete; just re-arm + report success
    return new RetakeResult(true, null);
```
Alternatively, narrow the controller guard to exclude `Open` so the UI never offers Reset for an
Open session, keeping both layers consistent. Add a test for the `Open`-status reset path.

### WR-02: `ExecuteAsync` spans three independent commits with no outer transaction — partial-failure leaves orphan AttemptHistory + undeleted responses

**File:** `Services/RetakeService.cs:75-160`
**Issue:** The method performs three separate DB round-trips that each commit on their own:
(1) `ExecuteUpdateAsync` claim (auto-commit), (2) `SaveChangesAsync` at line 117 to materialize
`attemptHistory.Id`, (3) `SaveChangesAsync` at line 160 for the snapshot `AddRange` + all
`RemoveRange` deletes. There is no enclosing `BeginTransactionAsync`. If the process dies or the
DB throws between step 2 and step 3, the session is already `Open`/`Score=null` (claimed) and an
`AssessmentAttemptHistory` row is committed with ZERO child archives, while the live
`PackageUserResponses` are still present. D-01 counting (`EXISTS child`) naturally excludes that
childless history from the cap, so it will not over-count — good — but it is an orphan row, the
old responses survive, and a retry creates a *second* `AttemptHistory`. The snapshot+delete are
correctly batched together in one `SaveChangesAsync` (so they are atomic relative to each other),
but the claim and the history-insert are not in the same unit of work as the delete.
**Fix:** Wrap claim → history-insert → snapshot → deletes in a single explicit transaction so a
mid-operation failure rolls the whole thing back (mirroring the bulk-assign pattern already used
at `AssessmentAdminController.cs:2196`). `ExecuteUpdateAsync` enlists in an ambient/explicit
transaction on the same connection:
```csharp
await using var tx = await _context.Database.BeginTransactionAsync();
// ... claim (ExecuteUpdateAsync), history insert + SaveChanges, snapshot, deletes + SaveChanges ...
await tx.CommitAsync();
```
Keep the audit + SignalR (steps 5-6) OUTSIDE the committed transaction (they are correctly
warn-only and must not roll back a successful reset).

### WR-03: Completed session with missing/empty assignment commits a childless AttemptHistory

**File:** `Services/RetakeService.cs:116-138`
**Issue:** When `wasCompleted == true`, the `AttemptHistory` row is inserted and committed at line
117 *before* the question/response load. If `assignment` is null or `GetShuffledQuestionIds()`
returns `[]` (line 122) — e.g. a completed legacy session whose `UserPackageAssignment` was
already removed, or a corrupt `ShuffledQuestionIds` JSON that deserializes to empty — then
`questions.Count == 0`, the `if (questions.Count > 0)` guard at line 133 skips the snapshot, and
you are left with a committed `AttemptHistory` carrying no archive children. This is the same
orphan shape as WR-02 but reachable on a clean run (no crash needed). It silently produces an
archive history that records `Score`/`IsPassed` but no per-question detail, partially defeating
the D-04 retention intent for that attempt.
**Fix:** Either (a) defer the `AttemptHistory` insert until after you have confirmed `questions`
is non-empty and build the snapshot in the same `SaveChangesAsync` as the deletes (preferred —
also resolves WR-02's split commit), or (b) if a completed session genuinely has no archivable
questions, log a warning and skip creating the `AttemptHistory` entirely so no childless row is
persisted. Note `GetShuffledQuestionIds()` swallows JSON errors and returns `[]`
(`Models/UserPackageAssignment.cs:60-68`), so the empty case is silent today.

## Info

### IR-01: Redundant per-question re-filter in essay branch

**File:** `Helpers/RetakeArchiveBuilder.cs:33,41`
**Issue:** `forQ` is already filtered to `r.PackageQuestionId == q.Id` at line 33, then line 41
re-applies the identical predicate (`forQ.FirstOrDefault(r => r.PackageQuestionId == q.Id)`).
Harmless but dead narrowing.
**Fix:** Simplify to `forQ.FirstOrDefault()`.

### IR-02: `UpdateRetakeSettings` propagates to siblings without re-guarding each sibling

**File:** `Controllers/AssessmentAdminController.cs:5573-5596`
**Issue:** `ShouldHideRetakeToggle` is checked only against the `assessment` matched by
`assessmentId`. The flags are then written to ALL siblings sharing `(Title, Category,
Schedule.Date)` (lines 5589-5596). If a sibling in that group were a Manual entry or a PreTest, it
would also get `AllowRetake=true` stamped on it. This is defensive-only: the pure
`RetakeRules.CanRetake`/`ShouldHideRetakeToggle` still hard-blocks PreTest/Manual at
eligibility/render time regardless of the stored flag, so no worker can actually retake. Same-batch
siblings normally share `AssessmentType`/`IsManualEntry`, so the mixed case is unlikely.
**Fix:** Optional defense-in-depth — skip writing to siblings where
`RetakeRules.ShouldHideRetakeToggle(sibling.AssessmentType, sibling.IsManualEntry)` is true, or
document that the stored flag on a hidden-toggle sibling is inert by design.

### IR-03: `ResetAssessment` holds a stale tracked `assessment` after the service mutates via ExecuteUpdate

**File:** `Controllers/AssessmentAdminController.cs:4201,4248-4277`
**Issue:** The controller loads `assessment` tracked at line 4201; the service then loads the same
keyed instance and mutates the row via `ExecuteUpdateAsync`, which does NOT sync the change
tracker. After the call the controller's `assessment.Status` is still the pre-reset value. This is
currently safe because the controller only reads `assessment.Title/Category/Schedule` (immutable
across reset) for the redirect, and the service intentionally reads the pre-claim
`Score/IsPassed/StartedAt/CompletedAt` off the same un-synced tracked instance to build the
archive (which is the desired snapshot). The correctness here depends on ExecuteUpdate-not-syncing
behavior, which is non-obvious.
**Fix:** No change required; add a one-line comment at the service load site noting that the
tracked instance deliberately retains pre-reset values for the archive snapshot, so a future
refactor doesn't "fix" it by calling `Reload()`.

### IR-04: Model snapshot omits SQL defaults for retake columns (new rows rely on C# initializers)

**File:** `Migrations/20260621065918_AddRetakeColumnsAndArchive.cs:21-33` vs
`Migrations/ApplicationDbContextModelSnapshot.cs:417-418,493-494,519-520`
**Issue:** The migration's `AddColumn` supplies `defaultValue: 2/24/false` (used only to backfill
existing rows at migrate time), but `OnModelCreating` declares no `.HasDefaultValue(...)` for
these three columns, so the snapshot lists them as plain `bit`/`int` with no DB default — unlike
`PassPercentage` which carries `.HasDefaultValue(70)`. New rows therefore get their values from the
C# property initializers (`MaxAttempts = 2`, `RetakeCooldownHours = 24`, `AllowRetake = false`),
not from a DB default. This is consistent and intentional (matches the Shuffle-toggle pattern), and
the bulk-add carry at `AssessmentAdminController.cs:2183-2186` explicitly copies the values rather
than relying on a default — so there is no functional gap. Flagged only so the "verify defaultValue
in AddColumn" invariant is documented as satisfied at the migration level while the model-level
default is deliberately absent.
**Fix:** None required. If a server-side INSERT that bypasses EF is ever expected for these columns,
add matching `entity.Property(a => a.MaxAttempts).HasDefaultValue(2)` etc. in `OnModelCreating`.

---

_Reviewed: 2026-06-21T08:12:27Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
