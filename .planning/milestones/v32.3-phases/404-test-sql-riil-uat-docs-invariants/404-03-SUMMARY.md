---
phase: 404-test-sql-riil-uat-docs-invariants
plan: 03
subsystem: testing
tags: [xunit, sql-server, ef-core, assignment-unit, userunits, b-06, backfill, filtered-unique]

requires:
  - phase: 404-test-sql-riil-uat-docs-invariants
    provides: MultiUnitSqlFixture (404-01)
  - phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
    provides: AddUserUnitsTable migration + backfill SQL + filtered-unique indexes
provides:
  - UnitMembershipInvariantSqlTests — QA-04 (AssignmentUnit∈UserUnits + B-06 cross-unit + 1:1 + one-primary)
  - UserUnitsBackfillIntegrationTests — 3 migration-399 backfill Facts implemented (QA-01, Open Q1 closed)
affects: [404-04]

tech-stack:
  added: []
  patterns:
    - "Drive production static helpers (ValidateAssignmentUnitInUserUnits, ProtonDeliverableBootstrap) against SQL-real fixture"
    - "Re-run the migration's OWN verbatim backfill SQL via ExecuteSqlRawAsync to prove idempotency truthfully"

key-files:
  created:
    - HcPortal.Tests/UnitMembershipInvariantSqlTests.cs
  modified:
    - HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs

key-decisions:
  - "Real SQL enforces the UserUnits.UserId FK → AspNetUsers — Facts that insert UserUnits must seed an ApplicationUser first (InMemory let this slide; SQL does not)."
  - "B-06 cross-unit proven by asserting per-assignment progress counts (unitX=2, unitY=2) — unit-Y not skipped despite unit-X progress existing."

patterns-established:
  - "Backfill idempotency proven by running the migration's verbatim INSERT...SELECT WHERE NOT EXISTS twice → 2nd run inserts 0."

requirements-completed: [QA-01, QA-04]

duration: ~25min
completed: 2026-06-21
---

# Phase 404 Plan 03: Unit-Membership SQL Invariants + Backfill Summary

**QA-04 unit-membership invariants proven SQL-real (AssignmentUnit∈UserUnits via production helper, B-06 cross-unit no-skip via production bootstrap, ProtonKompetensi/deliverable 1:1 + one-primary UserUnits via DbUpdateException) + the 3 residual migration-399 backfill stubs implemented and passing.**

## Performance

- **Duration:** ~25 min
- **Tasks:** 2 (4 Facts + 3 Facts)
- **Files modified:** 2 (1 created, 1 stub-fill)

## Accomplishments
- **Fact A** — `ValidateAssignmentUnitInUserUnits` accepts member units (primary + secondary), rejects non-member + blank, on the SQL-real fixture (represents Assign/Edit/Import/bypass-TargetUnit/reactivate, all gate through this helper).
- **Fact B** — `ProtonDeliverableBootstrap.CreateProgressAsync` for unit X then unit Y (same coachee) leaves 2 unit-X + 2 unit-Y progress rows — the B-06 guard does NOT skip unit-Y (different deliverable ids → cross-unit safe).
- **Fact C** — duplicate `(ProtonTrackAssignmentId, ProtonDeliverableId)` → `DbUpdateException` (1:1 filtered-unique).
- **Fact D** — 2nd `IsPrimary=true` UserUnits row for the same user → `DbUpdateException` (one-primary filtered-unique).
- **Backfill ×3** — migration-399's verbatim backfill SQL re-run via `ExecuteSqlRawAsync`: 1 primary-row per Unit-non-null user, 0 rows for Unit-null/empty, idempotent re-run adds 0 (closes Open Q1).
- Zero production code (R-1).

## Task Commits

1. **Task 1: UnitMembershipInvariantSqlTests (QA-04)** - `3d6c73da` (test)
2. **Task 2: implement 3 migration-399 backfill stubs (QA-01)** - `e85552ff` (test)

## Files Created/Modified
- `HcPortal.Tests/UnitMembershipInvariantSqlTests.cs` (new) - 4 SQL-real QA-04 Facts.
- `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs` (modified) - 3 `[Skip]` stubs → live `async Task` Facts running the verbatim backfill SQL.

## Decisions Made
- Followed plan; drove production helpers + DB constraints, no seam extraction.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Seed parent ApplicationUser before inserting UserUnits (Facts A & D)**
- **Found during:** Task 1 (first SQL run: Facts A & D failed)
- **Issue:** `UserUnits.UserId` is a real FK → `AspNetUsers`; inserting UserUnits rows for a fresh coachee with no Users row threw a FK-violation `DbUpdateException` at the first `SaveChangesAsync` (InMemory does not enforce FKs, so the InMemory CXU tests get away with it — but this is the SQL-real suite).
- **Fix:** Added `ctx.Users.Add(new ApplicationUser { Id = ..., UserName = ..., FullName = ... })` before the UserUnits insert in Facts A and D.
- **Verification:** Re-ran filter → 4/4 UnitMembership Facts pass.
- **Committed in:** `3d6c73da` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking — necessary for SQL-real correctness)
**Impact on plan:** No scope creep — only the missing parent-row seed required by the real FK.

## Issues Encountered
- See deviation above (FK seed). Resolved within the task.

## User Setup Required
None - requires local SQLEXPRESS (already present).

## Next Phase Readiness
- QA-01 + QA-04 fully covered SQL-real. **Wave 2 complete.**
- **Full suite: 562 passed / 0 failed / 2 skipped** (2m7s) — no regression; skip count dropped 6→2 (4 stubs now live; the 2 remaining skips are unrelated ProtonUnitResolve/UnitUnresolvedAudit endtoend).
- Remaining: 404-04 (Wave 3, checkpoint UAT @5270 + IT-handoff HTML + D1=b doc + SEED_JOURNAL).

---
*Phase: 404-test-sql-riil-uat-docs-invariants*
*Completed: 2026-06-21*
