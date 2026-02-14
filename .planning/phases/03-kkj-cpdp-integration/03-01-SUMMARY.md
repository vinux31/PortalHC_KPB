---
phase: 03-kkj-cpdp-integration
plan: 01
subsystem: competency-tracking
tags: [database, models, migration, foundation]
dependency-graph:
  requires: [kkj-matrix, assessment-sessions]
  provides: [competency-tracking-models, position-mapping, competency-database]
  affects: [gap-analysis, auto-update, cpdp-tracking]
tech-stack:
  added: [UserCompetencyLevel, AssessmentCompetencyMap, PositionTargetHelper]
  patterns: [ef-core-relationships, reflection-based-mapping, unique-constraints]
key-files:
  created:
    - Models/Competency/AssessmentCompetencyMap.cs
    - Models/Competency/UserCompetencyLevel.cs
    - Models/Competency/CompetencyGapViewModel.cs
    - Helpers/PositionTargetHelper.cs
    - Migrations/20260214070450_AddCompetencyTracking.cs
  modified:
    - Data/ApplicationDbContext.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs
decisions:
  - User FK uses Restrict instead of Cascade to avoid SQL Server multiple cascade path limitation
  - Position mapping uses reflection for flexibility
  - Unique index enforces one level per user per competency
  - Gap property is computed, not stored
metrics:
  duration: 3 minutes
  tasks-completed: 2
  files-created: 5
  files-modified: 2
  commits: 2
  completed: 2026-02-14T07:05:30Z
---

# Phase 03 Plan 01: Competency Tracking Foundation Summary

Data foundation for KKJ competency tracking with EF Core models, position mapping helper, and database migration.

## Tasks Completed

### Task 1: Create competency tracking models and ViewModels
**Status:** Complete
**Commit:** ec455e2

Created three new model files in `Models/Competency/` directory:

1. **AssessmentCompetencyMap.cs** - Join table linking assessment categories to KKJ competencies
   - Maps assessment categories (e.g., "Assessment OJ", "IHT") to specific KKJ competencies
   - Defines which competency level is granted when assessment is passed
   - Optional title pattern matching for finer-grained control
   - Configurable minimum score threshold (defaults to assessment's PassPercentage)

2. **UserCompetencyLevel.cs** - Per-user competency level tracking with history
   - Tracks current level (0-5) and target level per user per competency
   - Unique constraint enforces one record per user per competency
   - Source tracking: Assessment, Manual, Training
   - Audit fields: AchievedAt, UpdatedAt, UpdatedBy
   - Computed Gap property (not stored in DB)

3. **CompetencyGapViewModel.cs** - ViewModels for gap analysis dashboard
   - `CompetencyGapViewModel`: User-level summary with overall progress
   - `CompetencyGapItem`: Individual competency details with status and suggestions

All models compile successfully with proper navigation properties to existing entities.

### Task 2: Update DbContext, create position helper, and run migration
**Status:** Complete
**Commit:** e4c5103

**1. ApplicationDbContext.cs updates:**
- Added `using HcPortal.Models.Competency`
- Added DbSets: `AssessmentCompetencyMaps`, `UserCompetencyLevels`
- Configured relationships with proper FK constraints:
  - AssessmentCompetencyMap → KkjMatrixItem (Cascade)
  - UserCompetencyLevel → User (Restrict - SQL Server limitation fix)
  - UserCompetencyLevel → KkjMatrixItem (Restrict)
  - UserCompetencyLevel → AssessmentSession (SetNull)
- Added indexes for performance:
  - AssessmentCategory
  - {AssessmentCategory, TitlePattern}
  - {UserId, KkjMatrixItemId} (Unique)
- Added check constraints for CurrentLevel and TargetLevel (0-5 range)
- Ignored computed Gap property

**2. PositionTargetHelper.cs creation:**
- Static helper class with 15 position mappings
- Maps user position strings to KkjMatrixItem target level columns
- Uses reflection to retrieve target level values dynamically
- Handles special cases: null positions, "-" values, unparseable levels
- Provides utility methods: `GetTargetLevel()`, `GetColumnName()`, `GetAllPositions()`

All 15 positions mapped:
- Section Head
- Sr Supervisor GSH, Shift Supervisor GSH
- Panelman GSH 12-13, Panelman GSH 14
- Operator GSH 8-11, Operator GSH 12-13
- Shift Supervisor ARU
- Panelman ARU 12-13, Panelman ARU 14
- Operator ARU 8-11, Operator ARU 12-13
- Sr Supervisor Facility
- Jr Analyst
- HSE Officer

**3. EF Core migration:**
- Migration name: `20260214070450_AddCompetencyTracking`
- Created tables: `AssessmentCompetencyMaps`, `UserCompetencyLevels`
- Applied successfully with all indexes and constraints
- Migration verified in database

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking Issue] Fixed SQL Server cascade delete conflict**
- **Found during:** Task 2 - Database migration
- **Issue:** SQL Server doesn't allow multiple cascade paths. The FK from UserCompetencyLevel → User (Cascade) conflicted with User → AssessmentSession → UserCompetencyLevel cascade chain, causing error: "Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths"
- **Fix:** Changed UserCompetencyLevel → User FK from `DeleteBehavior.Cascade` to `DeleteBehavior.Restrict` in ApplicationDbContext.cs. This prevents the cascade cycle while maintaining referential integrity. If a user needs to be deleted, competency records must be removed first.
- **Files modified:** Data/ApplicationDbContext.cs
- **Commit:** e4c5103 (included with Task 2)
- **Impact:** Minimal - competency records are historical data that should be preserved even if user is deactivated

## Verification Results

All success criteria met:

- [x] All new models compile and reference existing models correctly via navigation properties
- [x] DbContext registers new entities with proper FK relationships, indexes, and constraints
- [x] Position helper maps all 15 KkjMatrixItem target columns to user position strings
- [x] Database migration creates tables without errors
- [x] No existing functionality broken (dotnet build succeeds, app starts)

**Build status:** SUCCESS (0 errors, 22 warnings - all pre-existing)

**Database migration status:** APPLIED
- Tables created: AssessmentCompetencyMaps, UserCompetencyLevels
- Indexes created: 6 total (3 per table)
- Constraints: Check constraints, unique constraint, FKs all applied

**PositionTargetHelper verification:** 15 position mappings confirmed

## Next Steps

With the data foundation in place, the following plans can now execute:

1. **Plan 03-02:** Gap Analysis Dashboard - Use UserCompetencyLevel to display gaps
2. **Plan 03-03:** Auto-update Competency Levels - Populate UserCompetencyLevel on assessment completion
3. **Plan 03-04:** CPDP Tracking Integration - Link CPDP activities to competency gaps

## Files Reference

**Created:**
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/Competency/AssessmentCompetencyMap.cs`
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/Competency/UserCompetencyLevel.cs`
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/Competency/CompetencyGapViewModel.cs`
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Helpers/PositionTargetHelper.cs`
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Migrations/20260214070450_AddCompetencyTracking.cs`

**Modified:**
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Data/ApplicationDbContext.cs`
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Migrations/ApplicationDbContextModelSnapshot.cs`

## Self-Check

Verifying all claimed artifacts exist and are correct.

**File existence check:**
- FOUND: Models/Competency/AssessmentCompetencyMap.cs
- FOUND: Models/Competency/UserCompetencyLevel.cs
- FOUND: Models/Competency/CompetencyGapViewModel.cs
- FOUND: Helpers/PositionTargetHelper.cs
- FOUND: Migrations/20260214070450_AddCompetencyTracking.cs
- FOUND: Data/ApplicationDbContext.cs (modified)
- FOUND: Migrations/ApplicationDbContextModelSnapshot.cs (modified)

**Commit existence check:**
- FOUND: ec455e2 (Task 1: competency tracking models)
- FOUND: e4c5103 (Task 2: DbContext, helper, migration)

**Build verification:**
- Build status: SUCCESS (0 errors)

## Self-Check: PASSED

All claimed files exist, commits are in git history, and the build succeeds.
