---
phase: 150-certificate-toggle-implementation
plan: "01"
subsystem: assessment-certificate
tags: [assessment, certificate, toggle, migration]
dependency_graph:
  requires: []
  provides: [GenerateCertificate toggle on AssessmentSession]
  affects: [CMP/Results, CMP/Records, CMP/RecordsWorkerDetail, Admin/CreateAssessment, Admin/EditAssessment]
tech_stack:
  added: []
  patterns: [EF Core migration with backward-compatible default, model-driven form toggle]
key_files:
  created:
    - Migrations/20260311012214_AddGenerateCertificateToAssessmentSession.cs
  modified:
    - Models/AssessmentSession.cs
    - Models/AssessmentResultsViewModel.cs
    - Models/UnifiedTrainingRecord.cs
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs
    - Views/Admin/CreateAssessment.cshtml
    - Views/Admin/EditAssessment.cshtml
    - Views/CMP/Results.cshtml
    - Views/CMP/Records.cshtml
    - Views/CMP/RecordsWorkerDetail.cshtml
decisions:
  - GenerateCertificate defaults to false in C# model (new assessments OFF by default)
  - Migration defaultValue set to true (existing rows backward compatible)
  - Certificate action returns NotFound (not redirect) when flag is OFF
metrics:
  duration: ~20 minutes
  completed: "2026-03-11"
  tasks_completed: 3
  tasks_total: 3
  files_changed: 10
---

# Phase 150 Plan 01: Assessment Certificate Toggle Summary

## One-liner

GenerateCertificate bool toggle on AssessmentSession controls certificate visibility in Results page, Certificate action guard, and Training Records views, with existing rows defaulting to true via migration.

## Tasks Completed

| # | Task | Commit | Status |
|---|------|--------|--------|
| 1 | Add GenerateCertificate to model, ViewModel, UnifiedTrainingRecord, migration | 2a8d3a7 | Done |
| 2 | Bind in AdminController Create/Edit, guard CMPController Certificate, populate GetUnifiedRecords | 2f1148d | Done |
| 3 | Toggle UI in Create/Edit forms, conditional Results button, conditional Records Sertifikat column | e6c6ff4 | Done |

## What Was Built

- `AssessmentSession.GenerateCertificate` bool property (default false for new sessions)
- EF Core migration `AddGenerateCertificateToAssessmentSession` with `defaultValue: true` for existing rows
- `AssessmentResultsViewModel.GenerateCertificate` carries flag to Results view
- `UnifiedTrainingRecord.GenerateCertificate` carries flag to Records views
- `AdminController`: CreateAssessment POST and EditAssessment POST bind the field; sibling session copy included
- `CMPController.Certificate`: returns `NotFound()` when `GenerateCertificate = false`
- `CMPController.Results`: populates `GenerateCertificate` in both new-path and legacy-path ViewModel construction
- `CMPController.GetUnifiedRecords`: assessment projection includes `GenerateCertificate = a.GenerateCertificate`
- `CreateAssessment.cshtml` / `EditAssessment.cshtml`: "Terbitkan Sertifikat" toggle in two-column row alongside ExamWindowCloseDate
- `Results.cshtml`: View Certificate button condition changed to `Model.IsPassed && Model.GenerateCertificate`
- `Records.cshtml` / `RecordsWorkerDetail.cshtml`: Sertifikat column shows "Lihat" link when `GenerateCertificate=true && AssessmentSessionId.HasValue`, otherwise dash

## Decisions Made

- New assessments default to OFF (toggle unchecked) — HC must explicitly enable certificates per assessment
- Existing rows receive `GenerateCertificate = true` via migration default — no regression in behavior
- Certificate action returns `NotFound` rather than redirect-with-error to prevent unauthorized URL access
- Toggle placed alongside ExamWindowCloseDate in same two-column row for layout efficiency

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- `dotnet ef database update` applied successfully; `AssessmentSessions` table has `GenerateCertificate` column
- `dotnet build` succeeds with 0 errors
- Manual verification: create assessment with toggle OFF → no certificate button in Results; Records shows dash
- Manual verification: create assessment with toggle ON → "View Certificate" appears when passed; "Lihat" in Records

## Self-Check

- [x] Models/AssessmentSession.cs has GenerateCertificate
- [x] Models/AssessmentResultsViewModel.cs has GenerateCertificate
- [x] Models/UnifiedTrainingRecord.cs has GenerateCertificate
- [x] Migration file exists with defaultValue: true (after manual fix)
- [x] AdminController.cs has 3 GenerateCertificate bindings
- [x] CMPController.cs has Certificate guard, 2x Results ViewModel, GetUnifiedRecords
- [x] All 5 view files updated
- [x] Build succeeds
- [x] Migration applied to database

## Self-Check: PASSED
