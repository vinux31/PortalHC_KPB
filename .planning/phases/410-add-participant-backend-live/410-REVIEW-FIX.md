---
phase: 410-add-participant-backend-live
fixed_at: 2026-06-21T00:00:00Z
review_path: .planning/phases/410-add-participant-backend-live/410-REVIEW.md
iteration: 1
findings_in_scope: 2
fixed: 2
skipped: 0
status: all_fixed
---

# Phase 410: Code Review Fix Report

**Fixed at:** 2026-06-21
**Source review:** `.planning/phases/410-add-participant-backend-live/410-REVIEW.md`
**Iteration:** 1

**Summary:**
- Findings in scope: 2 (WR-01 correctness, WR-02 test gap)
- Fixed: 2
- Skipped: 0
- Info findings (IN-01/IN-02/IN-03): NOT touched — accepted by design (D-02 / T-410-08 / T-410-09).

**Build:** `dotnet build` → 0 errors (26 pre-existing warnings, none new).
**Tests:**
- `FlexibleParticipantAddLive` filter → 10/10 passed (incl. hardened T9).
- Full suite → 581/581 passed, 0 failed.
- migration=FALSE. No Dev/Prod DB touched (write-path tests use disposable SQLEXPRESS `HcPortalDB_Test_{guid}`).

## Fixed Issues

### WR-01: Pre/Post branch inherited both sessions' config from a single `rep`

**Files modified:** `Controllers/AssessmentAdminController.cs`
**Commit:** `4bd68b1f`
**Applied fix:** In `AddParticipantsLive`, the Pre/Post branch now resolves **distinct** representatives from the batch before the create loop, mirroring `EditAssessment :1944-1945`:
- `repPre` = the batch's `PreTest` session (`Title + Category + Schedule.Date + AssessmentType=="PreTest"`), falling back to `rep` if absent (single-Pre batch / Post passed without Pre sibling — commented as intentional fallback).
- `repPost` = the batch's `PostTest` session, same fallback.

`newPre` is now built from `repPre` (its own Schedule / `ExamWindowCloseDate` / `DurationMinutes` / `DeriveReadyStatus`); `newPost` from `repPost`. Cert rules applied per the analog:
- `newPre.GenerateCertificate = false` (PreTest never generates a cert — mirrors `:1963`).
- `newPost.ValidUntil = repPost.ValidUntil` (mirrors `:1985`).
- `newPost.GenerateCertificate` inherits from `repPost` via `BuildReadyParticipantSession` (which copies `GenerateCertificate` from its rep).

`LinkedGroupId` (joins existing group), cross-set `LinkedSessionId`, eager UPA for both sessions, atomic tx, and all other guards (Proton / window / idempotency / cap-50) preserved unchanged.

**Note (logic-sensitive):** this is a correctness fix to inheritance logic. It is covered by the now-hardened T9 regression guard (see WR-02), which was confirmed to FAIL against the old single-rep code (negative-control: `newPost.ExamWindowCloseDate` expected Post window `2026-07-01`, old code produced Pre window `2026-06-24`) and PASS with the fix. Recommend a quick human confirmation of the Pre/Post inheritance during phase verification.

### WR-02: T9 could not detect WR-01 (no distinct PostTest sibling seeded)

**Files modified:** `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs`
**Commit:** `bfb0d009`
**Applied fix:** Hardened `AddParticipantsLive_PrePost_CreatesPair_WithCrossLink` (T9) into a genuine regression guard for WR-01:
- Seeds a realistic Pre/Post batch — an existing `PreTest` **and** an existing `PostTest` sibling — with **distinct** config: `postSched` later than `preSched`, `postWindow` (`+10d`) vs `preWindow` (`+3d`), `postDuration=90` vs `preDuration=45`, and `GenerateCertificate=true` on Post vs `false` on Pre.
- Caller passes the **PreTest** as `sessionId` (the likely monitoring-surface case).
- Asserts the new PostTest inherits the **Post** session's schedule/window/duration/cert (not the Pre's), and the new PreTest inherits the Pre's config with `GenerateCertificate==false`.
- Existing pair-count / cross-link / LinkedGroupId / ready-status assertions retained.

Also extended the `SeedRepSessionAsync` helper with optional `generateCertificate` (default `false`) and `durationMinutes` (default `60`) parameters — all existing call sites unaffected.

**Negative-control verification:** with WR-01 temporarily reverted to single-rep, T9 failed on the window assertion; restored and confirmed green. Controller diff confirmed empty vs the WR-01 commit after restore.

## Skipped Issues

None — both in-scope findings fixed. Info findings (IN-01/IN-02/IN-03) were intentionally left untouched per instructions (accepted by design: D-02 / T-410-08 / T-410-09).

---

_Fixed: 2026-06-21_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
