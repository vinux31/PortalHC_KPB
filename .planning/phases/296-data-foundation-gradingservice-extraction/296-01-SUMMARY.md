---
phase: 296-data-foundation-gradingservice-extraction
plan: "01"
subsystem: data-model
tags: [ef-core, migration, model, assessment, v14]
dependency_graph:
  requires: []
  provides: [AssessmentSession-v14-columns, PackageQuestion-QuestionType, PackageUserResponse-TextAnswer]
  affects: [GradingService-plan-02, AssessmentAdminController, CMPController]
tech_stack:
  added: []
  patterns: [ef-core-code-first-migration, nullable-columns-backward-compat]
key_files:
  created:
    - Migrations/20260406075820_AddAssessmentV14Columns.cs
    - Migrations/20260406075820_AddAssessmentV14Columns.Designer.cs
  modified:
    - Models/AssessmentSession.cs
    - Models/AssessmentPackage.cs
    - Models/PackageUserResponse.cs
decisions:
  - "QuestionType disimpan sebagai string nullable (null = MultipleChoice untuk backward compat) per D-06"
  - "HasManualGrading defaultValue false dikonfirmasi otomatis oleh EF Core — tidak perlu edit manual"
  - "Satu migration untuk semua 7 kolom per D-05"
metrics:
  duration: ~10 menit
  completed_date: "2026-04-06"
  tasks_completed: 2
  files_modified: 5
---

# Phase 296 Plan 01: Data Foundation — Model Column Additions Summary

**One-liner:** Tambah 7 kolom nullable/default ke 3 model entity (AssessmentSession, PackageQuestion, PackageUserResponse) sebagai fondasi data untuk GradingService v14.0.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Tambah kolom baru ke 3 model entity | 569eb0a8 | Models/AssessmentSession.cs, Models/AssessmentPackage.cs, Models/PackageUserResponse.cs |
| 2 | Generate EF Core migration AddAssessmentV14Columns | a7bb443e | Migrations/20260406075820_AddAssessmentV14Columns.cs, Migrations/20260406075820_AddAssessmentV14Columns.Designer.cs |

## What Was Built

### Model Changes

**AssessmentSession.cs** — 5 kolom baru:
- `AssessmentType` (string nullable) — tipe: 'PreTest', 'PostTest', null = tidak ditentukan
- `AssessmentPhase` (string nullable) — fase dalam siklus assessment
- `LinkedGroupId` (int nullable) — FK ke grup assessment
- `LinkedSessionId` (int nullable) — FK ke session pasangan (pre/post test linking)
- `HasManualGrading` (bool, default false) — flag untuk session yang punya soal Essay butuh grading manual HC

**AssessmentPackage.cs (PackageQuestion class)** — 1 kolom baru:
- `QuestionType` (string nullable) — tipe soal: 'MultipleChoice', 'MultipleAnswer', 'Essay'. Null = MultipleChoice (backward compat)

**PackageUserResponse.cs** — 1 kolom baru:
- `TextAnswer` (string nullable) — jawaban teks untuk soal Essay. Null untuk MC/MA

### Migration

File: `Migrations/20260406075820_AddAssessmentV14Columns.cs`
- 7 AddColumn calls di Up()
- 7 DropColumn calls di Down() untuk rollback safety
- `HasManualGrading`: `nullable: false, defaultValue: false` — EF Core otomatis mengisi nilai benar
- Build result: 0 errors, 70 warnings (pre-existing warnings, tidak ada yang baru)

## Deviations from Plan

Tidak ada deviasi — plan dieksekusi tepat seperti yang ditulis.

Catatan operasional: worktree di-reset ke commit `de0f2bfc` (base yang benar) sebelum eksekusi karena HEAD worktree menunjuk ke commit yang lebih baru dari main. File planning dari commit yang lebih baru (296-01-PLAN.md, 296-02-PLAN.md, 296-03-PLAN.md, 296-RESEARCH.md, 296-VALIDATION.md) tidak ada di worktree ini karena belum ada di base commit — ini behavior normal untuk parallel worktree execution.

## Known Stubs

Tidak ada stubs. Kolom baru adalah data model murni — belum ada UI yang render nilai-nilai ini (direncanakan di plan-plan selanjutnya).

## Self-Check: PASSED

| Item | Status |
|------|--------|
| Models/AssessmentSession.cs | FOUND |
| Models/AssessmentPackage.cs | FOUND |
| Models/PackageUserResponse.cs | FOUND |
| Migrations/20260406075820_AddAssessmentV14Columns.cs | FOUND |
| Commit 569eb0a8 (model changes) | FOUND |
| Commit a7bb443e (migration) | FOUND |
