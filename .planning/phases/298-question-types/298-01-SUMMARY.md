---
phase: 298-question-types
plan: 01
subsystem: assessment-admin
tags: [question-types, migration, form, mc, ma, essay, preview]
dependency_graph:
  requires: []
  provides: [Rubrik field, MaxCharacters field, EssayScore field, ManagePackageQuestions endpoint, PreviewQuestion endpoint, QuestionType in ExamQuestionItem]
  affects: [Models/AssessmentPackage.cs, Models/PackageUserResponse.cs, Models/PackageExamViewModel.cs, Controllers/AssessmentAdminController.cs]
tech_stack:
  added: []
  patterns: [EF Core migration, Bootstrap dynamic form, AJAX partial view, server-side whitelist validation]
key_files:
  created:
    - Migrations/20260407061826_AddRubrikEssayScoreMaxCharFields.cs
    - Views/Admin/ManagePackageQuestions.cshtml
    - Views/Admin/_PreviewQuestion.cshtml
  modified:
    - Models/AssessmentPackage.cs
    - Models/PackageUserResponse.cs
    - Models/PackageExamViewModel.cs
    - Controllers/AssessmentAdminController.cs
decisions:
  - "EditQuestion GET return JSON saat X-Requested-With: XMLHttpRequest, memungkinkan inline form population tanpa pindah halaman"
  - "ScoreValue MC/MA di-force ke 10 di server-side meski client kirim nilai lain (T-298-03)"
  - "QuestionType whitelist ['MultipleChoice','MultipleAnswer','Essay'] di server-side sebelum save (T-298-01)"
metrics:
  duration: "~25 menit"
  completed_date: "2026-04-07"
  tasks_completed: 2
  files_changed: 7
---

# Phase 298 Plan 01: DB Migration + HC Form Create/Edit Soal Summary

**One-liner:** DB migration 3 field baru (Rubrik/MaxCharacters/EssayScore) + form create/edit soal HC dengan dynamic switching MC/MA/Essay, validasi server-side per tipe, dan preview modal AJAX.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | DB Migration + Model Updates | f82bad2e | Models/AssessmentPackage.cs, Models/PackageUserResponse.cs, Migrations/* |
| 2 | HC Form Create/Edit Soal per Tipe + Preview Modal | 408c05d2 | Controllers/AssessmentAdminController.cs, Views/Admin/ManagePackageQuestions.cshtml, Views/Admin/_PreviewQuestion.cshtml, Models/PackageExamViewModel.cs |

## What Was Built

### Task 1: DB Migration
- `PackageQuestion.Rubrik` (string?, null untuk MC/MA) — referensi HC saat grading essay
- `PackageQuestion.MaxCharacters` (int, default 2000) — batas karakter jawaban essay per soal
- `PackageUserResponse.EssayScore` (int?, null = belum dinilai) — skor manual HC per soal essay
- Migration `AddRubrikEssayScoreMaxCharFields` applied ke database

### Task 2: HC Form + Endpoints
- `ManagePackageQuestions` GET: halaman daftar soal per package dengan tabel + form create/edit inline
- `CreateQuestion` POST: validasi MC 1 benar, MA min 2 benar, Essay wajib rubrik; force ScoreValue=10 MC/MA
- `EditQuestion` GET/POST: JSON response untuk AJAX inline edit, POST dengan validasi sama
- `DeleteQuestion` POST: hapus soal + responses + options
- `PreviewQuestion` GET: return partial view `_PreviewQuestion.cshtml` via AJAX
- Dynamic form JavaScript: dropdown QuestionType mengubah tampilan form (optionsSection/rubrikSection), radio↔checkbox
- Edit type warning modal: konfirmasi jika tipe soal diubah saat edit
- `ExamQuestionItem.QuestionType` + `MaxCharacters` ditambahkan untuk plan 03 downstream

## Deviations from Plan

### Auto-added Enhancements

**1. [Rule 2 - Missing functionality] EditQuestion GET return JSON untuk inline edit**
- **Found during:** Task 2 — plan menyebutkan form inline, tapi tidak spesifik mekanismenya
- **Fix:** EditQuestion GET detect `X-Requested-With: XMLHttpRequest` dan return JSON, sehingga form di ManagePackageQuestions bisa diisi tanpa redirect. Non-AJAX redirect ke ManagePackageQuestions.
- **Files modified:** Controllers/AssessmentAdminController.cs

**2. [Rule 2 - Security] DeleteQuestion membersihkan PackageUserResponses**
- **Found during:** Task 2 — menghapus soal tanpa menghapus responses menyebabkan orphaned FK records
- **Fix:** DeleteQuestion endpoint remove related responses sebelum delete soal
- **Files modified:** Controllers/AssessmentAdminController.cs

## Security Threats Mitigated

| Threat | Mitigation | Location |
|--------|-----------|---------|
| T-298-01: Tampering QuestionType | Whitelist validasi server-side | CreateQuestion + EditQuestion POST |
| T-298-02: Elevation | [Authorize(Roles = "Admin, HC")] semua endpoint baru | AssessmentAdminController.cs |
| T-298-03: Tampering ScoreValue | Server-side force ScoreValue=10 untuk MC/MA | CreateQuestion + EditQuestion POST |

## Known Stubs

Tidak ada stub — semua field ter-wire ke DB dan form.

## Self-Check

- [x] `public string? Rubrik` ada di Models/AssessmentPackage.cs
- [x] `public int MaxCharacters` = 2000 ada di Models/AssessmentPackage.cs
- [x] `public int? EssayScore` ada di Models/PackageUserResponse.cs
- [x] Migration file ada: Migrations/20260407061826_AddRubrikEssayScoreMaxCharFields.cs
- [x] `correctCount < 2` validasi MA ada di controller
- [x] `PreviewQuestion` endpoint ada di controller
- [x] `optionsSection` dan `rubrikSection` ada di view
- [x] Commit f82bad2e dan 408c05d2 exist
- [x] dotnet build: 0 errors

## Self-Check: PASSED
