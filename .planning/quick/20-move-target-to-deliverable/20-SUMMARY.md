---
phase: quick-20
plan: 01
subsystem: proton-silabus
tags: [model-change, data-migration, silabus]
dependency_graph:
  requires: []
  provides: [per-deliverable-target]
  affects: [proton-silabus, plan-idp]
tech_stack:
  patterns: [ef-data-migration-in-schema-migration]
key_files:
  created:
    - Migrations/20260308111015_MoveTargetToDeliverable.cs
  modified:
    - Models/ProtonModels.cs
    - Controllers/ProtonDataController.cs
    - Controllers/CDPController.cs
    - Views/ProtonData/Index.cshtml
    - Views/CDP/PlanIdp.cshtml
decisions:
  - "Combined model+controller changes in single commit since they must build together"
metrics:
  duration: 168s
  completed: "2026-03-08T11:11:35Z"
---

# Quick Task 20: Move Target to Deliverable Summary

Moved Target field from ProtonSubKompetensi to ProtonDeliverable with data migration preserving existing values.

## What Was Done

### Task 1: Model change + EF migration with data migration
- Removed `Target` property from `ProtonSubKompetensi`
- Added `Target` property to `ProtonDeliverable`
- Generated EF migration with reordered operations: AddColumn, SQL data copy, DropColumn
- Data migration copies Target from each SubKompetensi to all its child Deliverables
- Down migration copies first deliverable's Target back to SubKompetensi
- **Commit:** f30d489

### Task 2: Update controllers and views
- `ProtonDataController.Index`: reads `d.Target` instead of `s.Target`
- `ProtonDataController.SilabusSave`: removed Target from SubKompetensi upsert, added to Deliverable upsert (update, stale-create, and new-create paths)
- `CDPController.PlanIdp`: reads `d.Target` instead of `s.Target`
- `Views/ProtonData/Index.cshtml`: Target cell no longer uses SubKompetensi rowspan, renders per deliverable row
- `Views/CDP/PlanIdp.cshtml`: same rowspan removal
- Edit mode already rendered Target per-row (no change needed)
- **Commit:** 7a83a86

## Deviations from Plan

**1. [Rule 3 - Blocking] Combined Task 1 and Task 2 controller changes**
- Plan separated model/migration from controller changes, but removing Target from model breaks controllers immediately
- Fixed by applying controller changes before generating migration
- View changes committed separately as Task 2

## Verification

- `dotnet build` succeeds with 0 errors
- Migration applied successfully to database
- Manual verification needed: Silabus view/edit and PlanIdp display

## Self-Check: PASSED
