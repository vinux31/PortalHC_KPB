---
phase: 190-db-categories-foundation
plan: "01"
subsystem: data-layer
tags: [entity-model, ef-migration, seed-data, assessment-categories]
dependency_graph:
  requires: []
  provides: [AssessmentCategory entity, AssessmentCategories table, 6 seed rows]
  affects: [Plan 02 - CRUD + views, CMPController category branching]
tech_stack:
  added: []
  patterns: [EF Core DbSet registration, OnModelCreating config, migrationBuilder.Sql MERGE]
key_files:
  created:
    - Models/AssessmentCategory.cs
    - Migrations/20260317113635_AddAssessmentCategoriesTable.cs
    - Migrations/20260317113635_AddAssessmentCategoriesTable.Designer.cs
  modified:
    - Data/ApplicationDbContext.cs
decisions:
  - "Used migrationBuilder.Sql MERGE pattern for seed data (not HasData) — consistent with project convention"
  - "Assessment Proton seed string matches exact string used in CMPController category branching logic"
metrics:
  duration: ~2 minutes
  completed: 2026-03-17
  tasks_completed: 2
  files_created: 3
  files_modified: 1
---

# Phase 190 Plan 01: DB Categories Foundation Summary

**One-liner:** AssessmentCategory EF entity with unique Name index and MERGE-seeded table containing all 6 assessment category rows.

## What Was Built

- `Models/AssessmentCategory.cs` — entity with Id, Name (MaxLength 100, Required), DefaultPassPercentage (default 70), IsActive (default true), SortOrder (default 0)
- `Data/ApplicationDbContext.cs` — DbSet<AssessmentCategory> registered with unique Name index and SortOrder index in OnModelCreating
- `Migrations/20260317113635_AddAssessmentCategoriesTable.cs` — creates table, unique index on Name, SortOrder index, MERGE seed for 6 rows

## Seed Data

| Name | DefaultPassPercentage | SortOrder |
|------|----------------------|-----------|
| OJT | 70 | 1 |
| IHT | 70 | 2 |
| Training Licencor | 80 | 3 |
| OTS | 70 | 4 |
| Mandatory HSSE Training | 100 | 5 |
| Assessment Proton | 70 | 6 |

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create AssessmentCategory model and register DbSet | a087565 | Models/AssessmentCategory.cs, Data/ApplicationDbContext.cs |
| 2 | Generate EF migration with MERGE seed data | f887042 | Migrations/20260317113635_AddAssessmentCategoriesTable.cs |

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check: PASSED

- Models/AssessmentCategory.cs: FOUND
- Migrations/20260317113635_AddAssessmentCategoriesTable.cs: FOUND
- Data/ApplicationDbContext.cs contains DbSet<AssessmentCategory>: VERIFIED
- Commits a087565 and f887042: FOUND
- Database updated with 6 seed rows: CONFIRMED (dotnet ef database update output shows MERGE executed)
