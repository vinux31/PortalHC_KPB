---
phase: 195-certificate-signatory-settings
plan: 01
subsystem: data-model
tags: [entity-framework, migration, assessment-category, signatory, hierarchy]
dependency_graph:
  requires: []
  provides: [AssessmentCategory.ParentId, AssessmentCategory.SignatoryUserId, EF migration]
  affects: [Models/AssessmentCategory.cs, Data/ApplicationDbContext.cs, Migrations/]
tech_stack:
  added: []
  patterns: [self-referencing FK, SetNull delete behavior, Restrict delete behavior]
key_files:
  created:
    - Migrations/20260318023131_AddParentAndSignatoryToAssessmentCategory.cs
    - Migrations/20260318023131_AddParentAndSignatoryToAssessmentCategory.Designer.cs
  modified:
    - Models/AssessmentCategory.cs
    - Data/ApplicationDbContext.cs
    - Controllers/CMPController.cs
decisions:
  - Self-referencing FK uses DeleteBehavior.Restrict (prevents deleting categories with children)
  - SignatoryUserId FK uses DeleteBehavior.SetNull (signatory user deletion keeps category intact)
metrics:
  duration: 10min
  completed_date: "2026-03-18"
  tasks_completed: 2
  files_modified: 5
---

# Phase 195 Plan 01: AssessmentCategory Model Extension Summary

**One-liner:** Added ParentId self-referencing FK and SignatoryUserId FK to ApplicationUser on AssessmentCategory, with EF Core Restrict/SetNull delete behaviors and applied migration.

## Tasks Completed

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Extend AssessmentCategory with ParentId, SignatoryUserId, navigation properties | cabe667 |
| 2 | Configure EF relationships, generate and apply migration | 69ecc8b |

## Decisions Made

- Self-referencing FK (ParentId) configured with `DeleteBehavior.Restrict` — DB prevents deleting a category that has children, enforcing referential integrity at the database level.
- Signatory FK (SignatoryUserId) configured with `DeleteBehavior.SetNull` — if the signatory user is deleted, the category retains its record but loses the signatory reference (graceful degradation).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed assessment.AssessmentCategory reference (x2) in CMPController**
- **Found during:** Task 2 (dotnet build)
- **Issue:** CMPController (commit 6e24c89 from 195-03) referenced `assessment.AssessmentCategory` which does not exist; the correct property is `assessment.Category` (string).
- **Fix:** Replaced both occurrences with `assessment.Category`.
- **Files modified:** Controllers/CMPController.cs
- **Commit:** 69ecc8b

**2. [Rule 1 - Bug] Fixed File.Exists ambiguity (x2) in CMPController**
- **Found during:** Task 2 (dotnet build)
- **Issue:** `File.Exists()` calls were ambiguous between `System.IO.File` and `ControllerBase.File()`.
- **Fix:** Qualified with `System.IO.File.Exists()`.
- **Files modified:** Controllers/CMPController.cs
- **Commit:** 69ecc8b

## Self-Check: PASSED

- Models/AssessmentCategory.cs — contains ParentId, SignatoryUserId, Parent, Children, Signatory
- Data/ApplicationDbContext.cs — contains DeleteBehavior.Restrict, DeleteBehavior.SetNull
- Migrations/20260318023131_AddParentAndSignatoryToAssessmentCategory.cs — AddColumn for both columns, AddForeignKey for both FKs
- dotnet build: 0 errors
- database update: Done
