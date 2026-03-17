---
phase: 192-validuntil-nomorsertifikat
plan: 01
subsystem: assessment
tags: [certificate, nomor-sertifikat, valid-until, migration, unique-index]
dependency_graph:
  requires: []
  provides: [NomorSertifikat column, BuildCertNumber helper, ValidUntil propagation]
  affects: [AdminController.CreateAssessment POST, AssessmentSession model, DB schema]
tech_stack:
  added: []
  patterns: [retry loop on DbUpdateException, filtered UNIQUE index, Roman numeral month encoding]
key_files:
  created:
    - Migrations/20260317143630_AddNomorSertifikatToAssessmentSessions.cs
    - Migrations/20260317143630_AddNomorSertifikatToAssessmentSessions.Designer.cs
  modified:
    - Models/AssessmentSession.cs
    - Data/ApplicationDbContext.cs
    - Controllers/AdminController.cs
    - Migrations/ApplicationDbContextModelSnapshot.cs
decisions:
  - NomorSertifikat uses three-digit zero-padded sequence (D3) for lexicographic sort compatibility
  - Retry loop detaches sessions and resets Id=0 to allow re-insert after UNIQUE violation
  - Partial (filtered) index excludes nulls so legacy sessions (NomorSertifikat=null) are not affected by uniqueness
metrics:
  duration: 18min
  completed: 2026-03-17T14:38:44Z
  tasks_completed: 2
  files_modified: 5
---

# Phase 192 Plan 01: NomorSertifikat + ValidUntil Propagation Summary

**One-liner:** Certificate number generation (KPB/SEQ/ROMAN-MONTH/YEAR) with filtered UNIQUE DB index and 3-attempt retry loop; ValidUntil propagated to all sessions in a batch.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add NomorSertifikat property + EF migration + UNIQUE index | 12d7440 | Models/AssessmentSession.cs, Data/ApplicationDbContext.cs, Migrations/* |
| 2 | Certificate number generation + ValidUntil mapping in CreateAssessment POST | 4021781 | Controllers/AdminController.cs |

## What Was Built

**Task 1 — Model + DB schema:**
- Added `public string? NomorSertifikat { get; set; }` to `AssessmentSession` after `ValidUntil`
- Added filtered UNIQUE index `IX_AssessmentSessions_NomorSertifikat_Unique` via EF configuration (`HasFilter("[NomorSertifikat] IS NOT NULL")`)
- Generated and applied EF migration `AddNomorSertifikatToAssessmentSessions` — column is `nvarchar(450) NULL`

**Task 2 — Controller logic:**
- Added three private static helpers to `AdminController`: `ToRomanMonth`, `BuildCertNumber`, `IsDuplicateKeyException`
- Added `ModelState.Remove("NomorSertifikat")` alongside existing `ModelState.Remove("ValidUntil")`
- Refactored session creation loop from `foreach` to indexed `for` loop to use position index `i` for sequence offset
- Added `ValidUntil = model.ValidUntil` and `NomorSertifikat = BuildCertNumber(nextSeq + i, now)` assignments in session constructor
- Pre-query sequence start from existing certificate numbers for the current year
- Changed `using var transaction` to `var transaction` (required because the retry loop may reassign it)
- Wrapped `SaveChangesAsync` + `CommitAsync` in a `while (!saved && attempt < maxAttempts)` retry loop
- On UNIQUE violation: detach sessions, rollback, re-begin transaction, re-query max seq, re-assign numbers with `Id = 0` reset, re-add range

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- `Models/AssessmentSession.cs` contains `public string? NomorSertifikat { get; set; }` ✓
- `Data/ApplicationDbContext.cs` contains `HasFilter("[NomorSertifikat] IS NOT NULL")` ✓
- `Data/ApplicationDbContext.cs` contains `IX_AssessmentSessions_NomorSertifikat_Unique` ✓
- Migration file `20260317143630_AddNomorSertifikatToAssessmentSessions.cs` exists ✓
- `Controllers/AdminController.cs` contains `BuildCertNumber`, `IsDuplicateKeyException`, `ToRomanMonth` ✓
- `Controllers/AdminController.cs` contains `ModelState.Remove("NomorSertifikat")` ✓
- `Controllers/AdminController.cs` contains `ValidUntil = model.ValidUntil` ✓
- `Controllers/AdminController.cs` contains `EntityState.Detached` ✓
- `dotnet build` exits 0 ✓
