---
phase: 41-auto-save
plan: 01
subsystem: api
tags: [dotnet, entity-framework, csharp, upsert, atomic, assessment]

# Dependency graph
requires:
  - phase: 39-close-early
    provides: "SaveAnswer endpoint + session ownership pattern (session.UserId != user.Id check)"
  - phase: 40-history-tab
    provides: "AssessmentSession model with Status field"
provides:
  - "SaveAnswer hardened with ExecuteUpdateAsync atomic upsert (no race condition on concurrent saves)"
  - "UNIQUE constraint on PackageUserResponse(AssessmentSessionId, PackageQuestionId) — enforced at DB level"
  - "SaveLegacyAnswer endpoint for legacy exam path, writing to UserResponse table via atomic upsert"
affects: [42-resume, 43-polling, 44-monitoring, frontend-auto-save-js]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ExecuteUpdateAsync atomic upsert: try update first, insert only if updatedCount == 0 — avoids EF change tracking race"
    - "UNIQUE DB constraint as safety net backing the atomic upsert pattern"

key-files:
  created:
    - Migrations/20260224090357_AddUniqueConstraintPackageUserResponse.cs
    - Migrations/20260224090357_AddUniqueConstraintPackageUserResponse.Designer.cs
  modified:
    - Controllers/CMPController.cs
    - Data/ApplicationDbContext.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "Used ExecuteUpdateAsync + conditional Add pattern instead of AddOrUpdate — more explicit, avoids SaveChanges on update path"
  - "UNIQUE constraint added at DB level (not just EF) to prevent duplicates even if upsert logic has edge cases under extreme concurrency"
  - "SaveLegacyAnswer targets UserResponse (not PackageUserResponse) — matches the existing legacy exam scoring path"

patterns-established:
  - "Atomic upsert pattern: ExecuteUpdateAsync → if (updatedCount == 0) Add + SaveChangesAsync"
  - "Session guard pattern: FindAsync → GetUserAsync → ownership check → status check — consistent across SaveAnswer and SaveLegacyAnswer"

# Metrics
duration: 2min
completed: 2026-02-24
---

# Phase 41 Plan 01: Auto-Save Backend Summary

**ExecuteUpdateAsync atomic upsert hardening for SaveAnswer + new SaveLegacyAnswer endpoint with UNIQUE DB constraint on PackageUserResponse**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-24T09:03:08Z
- **Completed:** 2026-02-24T09:05:50Z
- **Tasks:** 2
- **Files modified:** 5 (2 source + 3 migration files)

## Accomplishments
- SaveAnswer rewritten with ExecuteUpdateAsync — eliminates FirstOrDefaultAsync check-then-insert race condition under concurrent auto-saves
- UNIQUE constraint applied to PackageUserResponse(AssessmentSessionId, PackageQuestionId) at DB level via EF migration (applied successfully)
- SaveLegacyAnswer endpoint created targeting UserResponse table with identical guard and atomic upsert pattern

## Task Commits

Each task was committed atomically:

1. **Task 1: Harden SaveAnswer with ExecuteUpdateAsync and add UNIQUE constraint migration** - `fce57b3` (feat)
2. **Task 2: Create SaveLegacyAnswer endpoint for legacy exam path** - `701b426` (feat)

## Files Created/Modified
- `Controllers/CMPController.cs` - SaveAnswer body replaced with ExecuteUpdateAsync; SaveLegacyAnswer added after it
- `Data/ApplicationDbContext.cs` - `.IsUnique()` added to PackageUserResponse composite index
- `Migrations/20260224090357_AddUniqueConstraintPackageUserResponse.cs` - Migration dropping old non-unique index, creating UNIQUE index
- `Migrations/20260224090357_AddUniqueConstraintPackageUserResponse.Designer.cs` - EF migration designer snapshot
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated model snapshot reflecting IsUnique

## Decisions Made
- Used `ExecuteUpdateAsync` + conditional `Add` rather than EF's `AddOrUpdate` — the explicit two-step pattern makes the upsert intent clear and avoids SaveChanges on the happy path (update path skips SaveChanges entirely)
- `UNIQUE` constraint added at DB level as a safety net; even under extreme concurrency the database will enforce uniqueness
- `SaveLegacyAnswer` targets `UserResponse` (not `PackageUserResponse`) because the legacy exam scoring path already reads from `UserResponse` — consistent with Phase 39 design

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- SaveAnswer and SaveLegacyAnswer are both live, atomic, and DB-safe
- Phase 42 (resume) can call SaveAnswer/SaveLegacyAnswer endpoints confidently — no duplicate rows possible
- Phase 43 (polling) wires setInterval on top of CheckExamStatus (no changes to auto-save endpoints needed)
- Frontend JS for Phase 41 can target `/CMP/SaveAnswer` for package exams and `/CMP/SaveLegacyAnswer` for legacy exams

---
*Phase: 41-auto-save*
*Completed: 2026-02-24*

## Self-Check: PASSED

- Controllers/CMPController.cs — FOUND
- Data/ApplicationDbContext.cs — FOUND
- Migrations/20260224090357_AddUniqueConstraintPackageUserResponse.cs — FOUND
- .planning/phases/41-auto-save/41-01-SUMMARY.md — FOUND
- Commit fce57b3 — FOUND
- Commit 701b426 — FOUND
