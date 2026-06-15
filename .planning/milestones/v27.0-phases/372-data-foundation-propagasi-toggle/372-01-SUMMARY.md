---
phase: 372-data-foundation-propagasi-toggle
plan: 01
subsystem: database
tags: [efcore, migration, sqlserver, assessment-session, shuffle-toggle, xunit, real-sql]

requires:
  - phase: none
    provides: data foundation for v27.0 shuffle toggle
provides:
  - "AssessmentSession.ShuffleQuestions/ShuffleOptions bool columns (= true, bit NOT NULL DEFAULT 1)"
  - "Migration AddShuffleTogglesToAssessmentSession applied to local DB (58 old rows backfilled ON)"
  - "ShuffleMigrationFixture (disposable real-SQL) + ShuffleMigrationTests (SHUF-01, 2 green)"
  - "Wave 0 stubs ShuffleCreatePersistenceTests (SHUF-02) + ShufflePropagationTests (SHUF-03) for Plan 02"
affects: [372-02, 372-03, 373, 374]

tech-stack:
  added: []
  patterns: ["bool-with-default triplet (entity =true + Fluent HasDefaultValue(true) + migration defaultValue:true + snapshot ValueGeneratedOnAdd)", "disposable real-SQL HcPortalDB_Test_<guid> fixture for migration-default proof"]

key-files:
  created:
    - Migrations/20260613095102_AddShuffleTogglesToAssessmentSession.cs
    - HcPortal.Tests/ShuffleMigrationTests.cs
    - HcPortal.Tests/ShuffleCreatePersistenceTests.cs
    - HcPortal.Tests/ShufflePropagationTests.cs
  modified:
    - Models/AssessmentSession.cs
    - Data/ApplicationDbContext.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "Used AllowAnswerReview triplet analog verbatim (entity+Fluent+snapshot ValueGeneratedOnAdd().HasDefaultValue(true)) — avoids GenerateCertificate snapshot drift"
  - "SHUF-01 backfill proved via raw-SQL INSERT omitting shuffle columns (DB DEFAULT 1 fills) — the same mechanism that backfilled 58 existing rows; InMemory cannot prove this"
  - "Dedicated ShuffleMigrationFixture (not reused ProtonCompletionFixture) for a self-contained disposable real-SQL DB"

patterns-established:
  - "bool-with-default-true column: entity = true, Fluent HasDefaultValue(true), migration defaultValue:true"
  - "migration-default proof via raw SQL omitting the new columns on a disposable real-SQL DB"

requirements-completed: [SHUF-01]

duration: ~25min
completed: 2026-06-13
---

# Phase 372 Plan 01: Data Foundation Summary

**2 bool columns ShuffleQuestions/ShuffleOptions (bit NOT NULL DEFAULT 1) added to AssessmentSession + migration applied to local DB backfilling all 58 existing rows ON, proven by a real-SQL xUnit fixture**

## Performance

- **Duration:** ~25 min
- **Tasks:** 3/3 (interactive inline, no subagents)
- **Files modified:** 7 (3 source/snapshot + 1 migration + 3 test)

## Accomplishments
- Entity props `ShuffleQuestions`/`ShuffleOptions` (`= true`) + Fluent `HasDefaultValue(true)` mirroring the `AllowAnswerReview` triplet (no snapshot drift — both emit `.ValueGeneratedOnAdd().HasDefaultValue(true)`).
- Migration `AddShuffleTogglesToAssessmentSession` (auto-generated via EF CLI): Up = `AddColumn<bool> ... defaultValue: true` ×2; Down = `DropColumn` ×2. Applied to local `HcPortalDB_Dev`.
- **SHUF-01 proven on real DB:** post-apply query → 58/58 rows `ShuffleQuestions=1 AND ShuffleOptions=1` (data lama tak berubah).
- `ShuffleMigrationTests` (real-SQL disposable): backfill-via-DEFAULT (raw INSERT omitting columns → true) + round-trip (false/true persists). 2/2 green.
- Wave 0 stubs for SHUF-02/03 (skipped) to be filled by Plan 02.

## Task Commits

1. **Task 1: entity props + Fluent HasDefaultValue** - `be1a7178` (feat)
2. **Task 2 [BLOCKING]: generate + apply migration** - `75f81512` (feat) — local DB backfill 58 rows ON
3. **Task 3: 3 test files + SHUF-01 real-SQL green (2/2) + Wave 0 stubs** - `75b02029` (test)

## Files Created/Modified
- `Models/AssessmentSession.cs` — 2 bool props (`= true`)
- `Data/ApplicationDbContext.cs` — 2 Fluent `HasDefaultValue(true)`
- `Migrations/20260613095102_AddShuffleTogglesToAssessmentSession.cs` — additive bit DEFAULT 1 ×2
- `Migrations/ApplicationDbContextModelSnapshot.cs` — both props (no drift)
- `HcPortal.Tests/ShuffleMigrationTests.cs` — SHUF-01 real-SQL (2 [Fact])
- `HcPortal.Tests/ShuffleCreatePersistenceTests.cs` + `ShufflePropagationTests.cs` — Wave 0 stubs

## Decisions Made
- Dedicated `ShuffleMigrationFixture` over reusing `ProtonCompletionFixture` (clearer self-contained disposable DB). Plan explicitly allowed either.
- Backfill proof via raw-SQL INSERT omitting shuffle columns (exercises the actual DB DEFAULT constraint, not the C# entity default).

## Deviations from Plan
None - plan executed as written. (Added a second [Fact] round-trip test alongside the backfill test — within the plan's behavior spec.)

## Issues Encountered
- `sqlcmd` ODBC Driver 18 rejected connection until `-C` (TrustServerCertificate) added; queries needed `-d HcPortalDB_Dev` (default DB was master). Resolved.

## Migration / IT Handoff Note
**1 migration this plan:** `AddShuffleTogglesToAssessmentSession` (commit `75f81512`). Additive (`bit NOT NULL DEFAULT 1`, reversible `DropColumn`). Local DB snapshot taken pre-apply: `C:\Temp\HcPortalDB_Dev_pre372_20260613.bak`. **IT notify required** (commit hash + migration flag) per DEV_WORKFLOW step 5 — bundle with existing carry-over. Do NOT apply to Dev/Prod (IT task).

## Next Phase Readiness
- Columns live + queryable → Plan 02 (controller write-sites + propagation) and Plan 03 (wizard UI) unblocked.
- Wave 0 stubs ready for Plan 02 to fill SHUF-02/03.

---
*Phase: 372-data-foundation-propagasi-toggle*
*Completed: 2026-06-13*
