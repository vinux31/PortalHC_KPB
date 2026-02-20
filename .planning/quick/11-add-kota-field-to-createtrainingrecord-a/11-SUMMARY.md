---
phase: quick-11
plan: 1
subsystem: training-records
tags: [model, viewmodel, controller, migration, view, rename]
dependency_graph:
  requires: [19-01-PLAN — CreateTrainingRecord feature]
  provides: [Kota column on TrainingRecords table, Kota input on CreateTrainingRecord form]
  affects: [CreateTrainingRecord form, RecordsWorkerList page, TrainingRecord entity]
tech_stack:
  added: []
  patterns: [EF Core nullable column migration, asp-for tag helper for optional field]
key_files:
  created:
    - Migrations/20260220071022_AddKotaToTrainingRecord.cs
    - Migrations/20260220071022_AddKotaToTrainingRecord.Designer.cs
  modified:
    - Models/TrainingRecord.cs
    - Models/CreateTrainingRecordViewModel.cs
    - Controllers/CMPController.cs
    - Views/CMP/CreateTrainingRecord.cshtml
    - Views/CMP/RecordsWorkerList.cshtml
decisions:
  - Kota is nullable (string?) in both model and ViewModel — city is an optional field, no validation required
  - Kota placed after Penyelenggara in form — both are location/organizer details, logical grouping
  - No asp-validation-for span for Kota — field is not [Required], inline validation would never show
metrics:
  duration: ~3 min
  completed: 2026-02-20
  tasks_completed: 2
  files_modified: 5
  files_created: 2
---

# Quick Task 11: Add Kota Field to CreateTrainingRecord Summary

**One-liner:** Added nullable Kota (city) text field to TrainingRecord model, ViewModel, form view, and controller mapping, plus renamed all "Create Training Offline" labels to "Create Training".

## What Was Done

Added a city field to the training record creation workflow so HC staff can record where training took place. Also cleaned up the stale "Offline" label from the page title, heading, and list button.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Add Kota field to model, ViewModel, controller mapping, and create migration | 68af013 | TrainingRecord.cs, CreateTrainingRecordViewModel.cs, CMPController.cs, 2 migration files |
| 2 | Add Kota input to form view and rename "Create Training Offline" to "Create Training" | d529c1f | CreateTrainingRecord.cshtml, RecordsWorkerList.cshtml |

## Verification Results

1. `dotnet build` — 0 errors (built to temp dir to avoid file lock from running app)
2. `grep -r "Create Training Offline" Views/` — no matches (all renamed)
3. `grep "Kota" Models/TrainingRecord.cs` — property exists
4. `grep "Kota" Models/CreateTrainingRecordViewModel.cs` — property exists
5. `grep "Kota = model.Kota" Controllers/CMPController.cs` — mapping exists
6. `grep 'asp-for="Kota"' Views/CMP/CreateTrainingRecord.cshtml` — input exists
7. Migration file `20260220071022_AddKotaToTrainingRecord.cs` created and applied

## Deviations from Plan

None - plan executed exactly as written.

**Note:** `dotnet build` reported a file lock error when building to the default output path because HcPortal.exe (process 1108) was already running. Used `--no-build` flag for `dotnet ef` commands after separately confirming 0 errors by building to a temp output directory (`-o C:\Temp\hcportal_build_check`). This is normal developer workflow — not a code issue.

## Self-Check: PASSED

Files verified:
- FOUND: Models/TrainingRecord.cs (Kota property)
- FOUND: Models/CreateTrainingRecordViewModel.cs (Kota property)
- FOUND: Controllers/CMPController.cs (Kota = model.Kota mapping)
- FOUND: Views/CMP/CreateTrainingRecord.cshtml (Kota input, "Create Training" title/heading)
- FOUND: Views/CMP/RecordsWorkerList.cshtml ("Create Training" button)
- FOUND: Migrations/20260220071022_AddKotaToTrainingRecord.cs
- FOUND: Migrations/20260220071022_AddKotaToTrainingRecord.Designer.cs

Commits verified:
- FOUND: 68af013 — feat(quick-11): add Kota field to TrainingRecord model, ViewModel, controller, and migration
- FOUND: d529c1f — feat(quick-11): add Kota input field to form and rename "Create Training Offline" to "Create Training"
