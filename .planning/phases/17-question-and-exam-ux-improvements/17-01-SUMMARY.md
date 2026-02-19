---
phase: 17-question-and-exam-ux-improvements
plan: "01"
subsystem: database

tags: [ef-core, sql-server, migrations, entity-framework, packages, exam, shuffle]

# Dependency graph
requires:
  - phase: prior-assessment-phases
    provides: AssessmentSession model and ApplicationDbContext

provides:
  - AssessmentPackage entity (FK to AssessmentSession, Cascade)
  - PackageQuestion entity (FK to AssessmentPackage, Cascade)
  - PackageOption entity (FK to PackageQuestion, Cascade)
  - UserPackageAssignment entity with JSON shuffle persistence (FK to Session Cascade, FK to Package Restrict)
  - DbContext DbSets and FK/cascade/index configuration for all 4 entities
  - Migration AddPackageSystem applied to database

affects:
  - 17-02-PLAN.md (package import/management controller)
  - 17-03-PLAN.md (exam assignment and shuffle logic)
  - 17-04-PLAN.md (exam-taking UI)
  - 17-05-PLAN.md (grading using PackageOption.Id)
  - 17-06-PLAN.md (submission and results)
  - 17-07-PLAN.md (HC preview)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "JSON columns for ordered ID arrays (ShuffledQuestionIds, ShuffledOptionIdsPerQuestion)"
    - "String UserId FK without nav property — consistent with Phase 6 decision"
    - "ID-based grading: PackageOption.Id is the stable key, never displayed letter A/B/C/D"
    - "Helper methods on model for JSON deserialization (GetShuffledQuestionIds, GetShuffledOptionIds)"

key-files:
  created:
    - Models/AssessmentPackage.cs
    - Models/UserPackageAssignment.cs
    - Migrations/20260219140545_AddPackageSystem.cs
    - Migrations/20260219140545_AddPackageSystem.Designer.cs
  modified:
    - Data/ApplicationDbContext.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "No Letter field on PackageOption — letters are display-only, assigned at render time based on shuffled position; grading uses PackageOption.Id"
  - "UserPackageAssignment -> AssessmentPackage FK uses Restrict (not Cascade) — assignments must not be deleted if package is later removed after exam completion"
  - "ShuffledQuestionIds and ShuffledOptionIdsPerQuestion stored as JSON strings — avoids join table overhead for per-user shuffle data"
  - "Unique index on UserPackageAssignment(AssessmentSessionId, UserId) — enforces one assignment per session per user"

patterns-established:
  - "Package system: one AssessmentSession -> N AssessmentPackages -> N PackageQuestions -> N PackageOptions"
  - "Per-user shuffle stored as JSON on UserPackageAssignment, not as rows in a join table"

# Metrics
duration: 3min
completed: 2026-02-19
---

# Phase 17 Plan 01: Package System Data Layer Summary

**Four EF Core entities for multi-package exam system with JSON-persisted per-user question/option shuffle, applied via AddPackageSystem migration**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-19T14:03:56Z
- **Completed:** 2026-02-19T14:07:07Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Created `AssessmentPackage`, `PackageQuestion`, `PackageOption` entity classes with proper FK chain (Session -> Package -> Question -> Option, all Cascade)
- Created `UserPackageAssignment` with JSON columns for per-user shuffled question and option ID arrays, plus typed helper methods for deserialization
- Registered all 4 entities in `ApplicationDbContext` with correct cascade behaviors, indexes, and unique constraint; migration `AddPackageSystem` created and applied

## Task Commits

1. **Task 1: Create AssessmentPackage, PackageQuestion, PackageOption models** - `ce5d8a9` (feat)
2. **Task 2: Create UserPackageAssignment model, DbContext registration, and migration** - `ead730f` (feat)

## Files Created/Modified

- `Models/AssessmentPackage.cs` - AssessmentPackage (FK to Session), PackageQuestion (FK to Package), PackageOption (FK to Question, IsCorrect flag, no Letter field)
- `Models/UserPackageAssignment.cs` - Per-user package assignment with JSON shuffle fields and typed deserializer helpers
- `Data/ApplicationDbContext.cs` - 4 new DbSets + OnModelCreating FK/cascade/index configuration
- `Migrations/20260219140545_AddPackageSystem.cs` - Migration creating all 4 tables with constraints and indexes
- `Migrations/20260219140545_AddPackageSystem.Designer.cs` - EF migration designer snapshot
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated model snapshot

## Decisions Made

- No Letter field on `PackageOption` — letters (A/B/C/D) are display-only, assigned at render time based on shuffled position. Grading uses `PackageOption.Id` exclusively to be shuffle-safe.
- `UserPackageAssignment -> AssessmentPackage` FK uses `DeleteBehavior.Restrict` — assignments must survive package deletion if the exam was already completed.
- Shuffle data stored as JSON strings on `UserPackageAssignment` rather than a join table — simpler, avoids N+1 joins when loading exam display order.
- Unique index on `(AssessmentSessionId, UserId)` enforces one-assignment-per-session-per-user at the DB level.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed invalid [NotMapped] attribute from methods**
- **Found during:** Task 2 (UserPackageAssignment model)
- **Issue:** `[NotMapped]` attribute was applied to methods (`GetShuffledQuestionIds`, `GetShuffledOptionIds`) and a `static readonly` field — CS0592 compile error: `[NotMapped]` is only valid on class/property/indexer/field declarations, not methods
- **Fix:** Removed `[NotMapped]` from the static field and two helper methods; EF Core ignores non-property members automatically — no attribute needed
- **Files modified:** Models/UserPackageAssignment.cs
- **Verification:** `dotnet build` exits 0 after fix
- **Committed in:** ead730f (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug fix)
**Impact on plan:** Compile error fix only. No scope change.

## Issues Encountered

None beyond the auto-fixed compile error above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All 4 tables exist in the database and are queryable via EF Core
- Plan 17-02 (package import/management) can reference `AssessmentPackages`, `PackageQuestions`, `PackageOptions` DbSets directly
- Plan 17-03 (shuffle and assignment) can write to `UserPackageAssignments` with JSON shuffle payloads
- Plan 17-05 and 17-06 grading logic must use `PackageOption.Id` (not displayed letter) — this pattern is locked in by the model design

## Self-Check: PASSED

- Models/AssessmentPackage.cs: FOUND
- Models/UserPackageAssignment.cs: FOUND
- Migrations/20260219140545_AddPackageSystem.cs: FOUND
- 17-01-SUMMARY.md: FOUND
- Commit ce5d8a9 (Task 1): FOUND
- Commit ead730f (Task 2): FOUND

---
*Phase: 17-question-and-exam-ux-improvements*
*Completed: 2026-02-19*
