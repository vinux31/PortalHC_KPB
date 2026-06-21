---
phase: 404-test-sql-riil-uat-docs-invariants
plan: 02
subsystem: testing
tags: [xunit, sql-server, ef-core, single-active, proton, bypass, reactivate, filtered-unique]

requires:
  - phase: 404-test-sql-riil-uat-docs-invariants
    provides: MultiUnitSqlFixture (404-01)
provides:
  - SingleActiveInvariantSqlTests — QA-03 single-active proven SQL-real across mapping + reactivate + PTA-bypass
affects: [404-04]

tech-stack:
  added: []
  patterns:
    - "Assert-strategy split: DB-enforced (filtered-unique) → DbUpdateException; app-enforced (non-unique idx) → COUNT"

key-files:
  created:
    - HcPortal.Tests/SingleActiveInvariantSqlTests.cs
  modified: []

key-decisions:
  - "One mapping Fact represents Assign/Edit/Import/Reactivate because they share IX_CoachCoacheeMappings_CoacheeId_ActiveUnique (R-2)."
  - "PTA single-active asserted by COUNT after a REAL ProtonBypassService bypass T1@X→T2@Y — never DbUpdateException (Pitfall #1: PTA has only a NON-unique index)."

patterns-established:
  - "Reactivate path proven via DbContext write-pattern replication (flip inactive→active without deactivating) → DbUpdateException — test code, not a production seam (R-1)."
  - "Cert histori assertion: after bypass, both assignments co-exist (1 inactive + 1 active) + source ProtonFinalAssessment preserved."

requirements-completed: [QA-03]

duration: ~15min
completed: 2026-06-21
---

# Phase 404 Plan 02: Single-Active SQL-Real Invariants Summary

**QA-03 single-active proven on real SQL across all write-paths: mapping (DbUpdateException, covers Assign/Edit/Import/Reactivate via the shared filtered-unique index) + reactivate-without-deactivate replication + PTA bypass T1@X→T2@Y (COUNT == 1, cert histori preserved).**

## Performance

- **Duration:** ~15 min
- **Tasks:** 2 (3 Facts in one new file)
- **Files modified:** 1 (created)

## Accomplishments
- **Fact A** — a 2nd active `CoachCoacheeMapping` for the same coachee throws `DbUpdateException` on real SQL (filtered-unique `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`). One Fact covers Assign/Edit/Import/Reactivate (R-2, shared index). InMemory would be false-green.
- **Fact B** — a buggy reactivate that flips an inactive row to active without deactivating the existing active one is rejected by the same DB index (proves the reactivate / import-reactivate path is protected).
- **Fact C** — driving the REAL `ProtonBypassService.ExecuteInstantBypassAsync` T1@X→T2@Y leaves exactly 1 active `ProtonTrackAssignment` (COUNT, not exception — Pitfall #1) AND preserves cert histori (both assignments co-exist, source `ProtonFinalAssessment` survives).
- Zero production code (R-1).

## Task Commits

1. **Tasks 1+2: SingleActiveInvariantSqlTests (mapping + reactivate + PTA-bypass)** - `daf2eebe` (test)

_Note: Tasks 1 and 2 both write the single new file `SingleActiveInvariantSqlTests.cs`; committed together as one atomic file commit._

## Files Created/Modified
- `HcPortal.Tests/SingleActiveInvariantSqlTests.cs` (new) - 3 SQL-real Facts + verbatim bypass test helpers (`NewBypassSvc`/`TrackIdAsync`/`SeedAssignmentAsync`/`SeedDeliverablesAsync`/`SeedProgressAsync`/`Req`) copied from `ProtonBypassServiceTests`.

## Decisions Made
- Followed the assert-strategy split exactly per Pitfall #1 — DbUpdateException for DB-enforced mapping, COUNT for app-enforced PTA.
- Cert-histori assertion kept lenient (`finalCount >= 1`) to match the precedent's actual behavior — the load-bearing claim is "prior track record not destroyed."

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None. Build 0 errors; 3/3 Facts passed (654 ms).

## User Setup Required
None - requires local SQLEXPRESS (already present).

## Next Phase Readiness
- QA-03 fully covered SQL-real. 404-03 (unit-membership QA-04 + backfill QA-01) is the remaining Wave-2 plan; 404-04 (UAT + docs) is the closer.

---
*Phase: 404-test-sql-riil-uat-docs-invariants*
*Completed: 2026-06-21*
