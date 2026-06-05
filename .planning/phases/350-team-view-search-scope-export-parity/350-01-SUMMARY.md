---
phase: 350-team-view-search-scope-export-parity
plan: 01
subsystem: testing
tags: [xunit, playwright, ef-inmemory, seed-sql, cmp-records]

requires:
  - phase: 346-cmp-records-detail-search-logic
    provides: cmp346-seed.sql + cmp-records-346.spec.ts SEED_WORKFLOW analog, WorkerDataServiceSearchTests harness
provides:
  - 4 new xUnit [Fact] (RED) covering SF-01 assessment-title search + D-07 badge invariant + SF-06 export worker-list
  - tests/sql/cmp350-seed.sql (temporary+local-only Completed/OJT titled assessment)
  - tests/e2e/cmp-records-350.spec.ts Team View UAT (RED until impl)
  - SEED_JOURNAL entry (active)
affects: [350-02, 350-03]

tech-stack:
  added: []
  patterns: [RED-first Wave 0 scaffold cloned from Phase 346 analogs]

key-files:
  created:
    - tests/sql/cmp350-seed.sql
    - tests/e2e/cmp-records-350.spec.ts
  modified:
    - HcPortal.Tests/WorkerDataServiceSearchTests.cs
    - docs/SEED_JOURNAL.md

key-decisions:
  - "Override .Title/.Category after Session(...) construction (D-08 light touch) — no helper overload"
  - "Spec scope = href-only + counter (RESEARCH OQ2) — XLSX content check deferred to Plan 03 manual gate"
  - "Reuse email rino.prasetyo@pertamina.com (same as cmp346-seed) for accessible-section worker"

patterns-established:
  - "Wave 0 RED scaffold: 4 facts fail before predicate, 6 existing green — establishes GREEN signal for Plan 02"

requirements-completed: [SF-01, SF-06]

duration: ~12min
completed: 2026-06-05
---

# Phase 350 Plan 01: Wave 0 Validation Scaffold Summary

**4 RED xUnit facts (assessment-title search + D-07 badge invariant + export worker-list) + cloned Phase 346 SEED_WORKFLOW Playwright spec + temporary OJT-titled seed**

## Performance
- **Duration:** ~12 min
- **Completed:** 2026-06-05
- **Tasks:** 3
- **Files modified:** 4 (2 created, 2 modified)

## Accomplishments
- 4 new `[Fact]` in WorkerDataServiceSearchTests.cs — confirmed RED (4 fail / 6 existing pass) before predicate lands
- `cmp350-seed.sql` clones 346 structure: Completed, Category='OJT', Title='[PENDING350] OJT v14.2 Migas', idempotent DELETE-by-prefix + THROW 51350 precondition
- `cmp-records-350.spec.ts` Team View UAT: SF-01 (worker appears) + SF-02 (honest copy) + SF-06 (export href) with backup→seed→restore→Layer4 assert
- SEED_JOURNAL row (status active — will be marked cleaned after Plan 03 restore)

## Task Commits
1. **Task 1: 4 assessment-title xUnit facts (RED)** — `cc9e7e86` (test)
2. **Task 2: cmp350-seed.sql + SEED_JOURNAL** — `13fcaa93` (test)
3. **Task 3: cmp-records-350.spec.ts** — `f319bc4e` (test)

## Files Created/Modified
- `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — +4 facts (Scope_Training_FiltersByAssessmentTitle, Scope_Keduanya_Union_IncludesAssessment, Search_DoesNotMutate_BadgeCounts_D07, Keduanya_AssessmentTitle_ReturnsWorker_ForExport)
- `tests/sql/cmp350-seed.sql` — temporary seed (NEW)
- `tests/e2e/cmp-records-350.spec.ts` — Team View UAT (NEW)
- `docs/SEED_JOURNAL.md` — Phase 350 entry (active)

## Decisions Made
- None beyond key-decisions above — followed plan as specified.

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None. `dotnet build HcPortal.Tests` 0 errors (22 pre-existing warnings incl. xUnit2031 on an existing fact, untouched). Test run: 4 new RED + 6 existing GREEN as intended.

## Next Phase Readiness
- Plan 02 (SF-01 predicate + SF-02 copy) will turn the 4 facts GREEN.
- Seed + spec ready; afterAll restore will mark SEED_JOURNAL cleaned at Plan 03 gate.

---
*Phase: 350-team-view-search-scope-export-parity*
*Completed: 2026-06-05*
