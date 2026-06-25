---
phase: 404-test-sql-riil-uat-docs-invariants
plan: 01
subsystem: testing
tags: [xunit, sql-server, ef-core, integration-test, fixture, multi-unit, proton]

requires:
  - phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
    provides: UserUnits junction + AddUserUnitsTable migration + filtered-unique indexes
  - phase: 402-coaching-cross-unit-mapping
    provides: SingleActive_invariant_is_sql_real_phase404 anchor name (was [Skip] stub)
provides:
  - MultiUnitSqlFixture — disposable SQL-real fixture (MigrateAsync full chain + canonical {X,Y}/coach/PROTON seed)
  - CrossUnitAssignSqlTests — live SQL-real single-active anchor (QA-03), closes the 402 carry
affects: [404-02, 404-03, 404-04]

tech-stack:
  added: []
  patterns:
    - "Shared SQL-real IClassFixture with canonical const identifiers reused across test classes"
    - "Assert-strategy split: DB-enforced filtered-unique → Assert.ThrowsAsync<DbUpdateException>"

key-files:
  created:
    - HcPortal.Tests/MultiUnitSqlFixture.cs
  modified:
    - HcPortal.Tests/CrossUnitAssignTests.cs

key-decisions:
  - "Fixture seeds canonical dataset but pre-creates NO ProtonTrackAssignments — invariant Facts drive T1@X→T2@Y so they can assert the active count themselves."
  - "Single-active anchor lives in a SEPARATE SQL-real class (CrossUnitAssignSqlTests) since the host CrossUnitAssignTests class is InMemory; old [Skip] stub removed, anchor name preserved."

patterns-established:
  - "MultiUnitSqlFixture: copy OrgLabelMigrationFixture skeleton verbatim (connstr, DbName-per-guid, MigrateAsync, catch→EnsureDeletedAsync+XunitException, DisposeAsync) + ProtonBypass seed-chain helpers."
  - "Per-Fact unique coachee ($\"sa-{Guid:N}\") because the shared-DB fixture's single-active index is per-coachee."

requirements-completed: [QA-01, QA-03]

duration: ~25min
completed: 2026-06-21
---

# Phase 404 Plan 01: SQL-Real Test Foundation Summary

**Disposable SQL-real `MultiUnitSqlFixture` (MigrateAsync full chain incl 399 AddUserUnitsTable + canonical {X,Y}/coach/PROTON seed) + live single-active anchor that closes the 402 carry.**

## Performance

- **Duration:** ~25 min
- **Tasks:** 2
- **Files modified:** 2 (1 created, 1 modified)

## Accomplishments
- `MultiUnitSqlFixture` migrates a disposable `HcPortalDB_Test_<guid>@localhost\SQLEXPRESS` through the full EF migration chain (proving migration 399 `AddUserUnitsTable` + filtered-unique indexes apply cleanly on real SQL — doubles as a deploy smoke test) and seeds the canonical multi-unit dataset.
- The 402-carry `SingleActive_invariant_is_sql_real_phase404` stub is now a LIVE, passing SQL-real Fact (asserts `DbUpdateException` on a 2nd active mapping for the same coachee). The `[Skip]`-empty stub is gone.
- Zero production code touched (R-1 boundary respected) — test fixture + test file only.

## Task Commits

1. **Task 1: Create MultiUnitSqlFixture with canonical seed** - `48f49725` (test)
2. **Task 2: Implement single-active SQL-real anchor (close 402 carry)** - `5519122f` (test)

## Files Created/Modified
- `HcPortal.Tests/MultiUnitSqlFixture.cs` (new) - Shared `IClassFixture` SQL-real fixture: `MigrateAsync` full chain + `SeedCanonicalAsync` ({X,Y} org tree, coachee+coach multi-unit `UserUnits`, PROTON Kompetensi→Sub→Deliverable chains for T1@X and T2@Y). Exposes `Options` + stable `const` ids.
- `HcPortal.Tests/CrossUnitAssignTests.cs` (modified) - Removed `[Skip]` stub; added `CrossUnitAssignSqlTests : IClassFixture<MultiUnitSqlFixture>` with the live single-active anchor.

## Decisions Made
- Fixture pre-creates no `ProtonTrackAssignment` rows — the sequential T1@X→T2@Y bypass is left for the invariant Facts (404-02) to drive, so they assert the post-drive active count.
- Anchor placed in a separate SQL-real class rather than converting the InMemory host class.

## Deviations from Plan
None - plan executed exactly as written. (One cosmetic tweak: rephrased an inline comment so it no longer contained the literal token `EnsureCreated`, satisfying the acceptance grep that checks the fixture has no `EnsureCreated` call. No behavior change.)

## Issues Encountered
None. SQLEXPRESS (SQL Server 2025 Express) verified live before execution; fixture migrate+seed succeeded; anchor passed (1/1, 116 ms).

## User Setup Required
None - no external service configuration required. (Integration tests require a local SQLEXPRESS instance, already present.)

## Next Phase Readiness
- `MultiUnitSqlFixture` is the load-bearing Wave-1 dependency — 404-02 (single-active across write-paths) and 404-03 (unit-membership + backfill) can now consume it via `IClassFixture<MultiUnitSqlFixture>`.
- Build: 0 errors / 28 warnings (baseline). `CrossUnitAssign` filter: 7/7 passed (6 InMemory CXU + 1 SQL anchor) — no regression.

---
*Phase: 404-test-sql-riil-uat-docs-invariants*
*Completed: 2026-06-21*
