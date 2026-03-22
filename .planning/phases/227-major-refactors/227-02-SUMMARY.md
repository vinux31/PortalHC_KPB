---
phase: 227-major-refactors
plan: 02
subsystem: assessment-exam-engine
tags: [legacy-cleanup, migration, cmp-controller, admin-controller, ef-migration]
dependency_graph:
  requires: [227-01]
  provides: [CLEN-02-complete]
  affects: [CMPController, AdminController, ApplicationDbContext, AssessmentSession]
tech_stack:
  added: []
  patterns: [package-only-exam-path, ef-data-migration]
key_files:
  created:
    - Migrations/20260322032905_MigrateLegacyQuestionsAndDropTables.cs
  modified:
    - Controllers/CMPController.cs
    - Controllers/AdminController.cs
    - Data/ApplicationDbContext.cs
    - Models/AssessmentSession.cs
    - Views/CMP/StartExam.cshtml
  deleted:
    - Models/AssessmentQuestion.cs
    - Models/UserResponse.cs
    - Views/Admin/ManageQuestions.cshtml
decisions:
  - "Legacy sessions with active status and no package assignment abandoned via migration SQL (per D-04)"
  - "ManageQuestions/AddQuestion/DeleteQuestion admin actions removed — legacy question CRUD no longer valid"
  - "Results page returns empty state (not error) for non-package sessions — graceful fallback"
metrics:
  duration: "12 minutes"
  completed: "2026-03-22T03:32:00Z"
  tasks_completed: 2
  files_changed: 9
requirements: [CLEN-02]
---

# Phase 227 Plan 02: Legacy Question Path Migration & Cleanup Summary

## One-liner

Semua data legacy AssessmentQuestion/Option/UserResponse dimigrasikan ke PackageQuestion/Option/PackageUserResponse via EF migration, lalu tabel dan code path legacy dihapus sepenuhnya.

## What Was Built

### Task 1: EF Migration — Migrate legacy data + drop legacy tables

Data migration SQL 5-step yang berjalan sebelum DROP TABLE:

1. **Step 0**: Abandon active legacy sessions (Open/InProgress yang punya legacy questions tapi tidak punya package)
2. **Step 1**: Buat `AssessmentPackage` (Paket A) untuk setiap legacy session yang belum punya package
3. **Step 2**: Migrate `AssessmentQuestion` → `PackageQuestion` (preserve QuestionText, Order, ScoreValue; ElemenTeknis = NULL)
4. **Step 3**: Migrate `AssessmentOption` → `PackageOption` (preserve OptionText, IsCorrect; join via Order)
5. **Step 4**: Buat `UserPackageAssignment` untuk setiap user yang punya UserResponse (UserId dari AssessmentSession)
6. **Step 5**: Migrate `UserResponse` → `PackageUserResponse` (join via OptionText untuk resolve PackageOptionId)
7. **Step 6**: DROP TABLE `UserResponses`, `AssessmentOptions`, `AssessmentQuestions`

Model files yang dihapus: `Models/AssessmentQuestion.cs`, `Models/UserResponse.cs`

DbContext cleanup:
- Hapus `DbSet<AssessmentQuestion>`, `DbSet<AssessmentOption>`, `DbSet<UserResponse>`
- Hapus `builder.Entity<UserResponse>` dan `builder.Entity<AssessmentQuestion>` configurations

AssessmentSession cleanup:
- Hapus navigation properties `ICollection<AssessmentQuestion> Questions` dan `ICollection<UserResponse> Responses`

### Task 2: Remove legacy code paths

**CMPController:**
- Hapus `SaveLegacyAnswer` action (endpoint auto-save legacy)
- `TakeExam`: ganti legacy else-branch dengan redirect+error jika tidak ada packages
- `PreviewExam` (SubmitExam GET): hapus legacy AssessmentQuestion summary branch
- `SubmitExam` (POST): hapus entire legacy grading block (UserResponse + AssessmentQuestion scoring)
- `Results`: ganti legacy review block dengan empty viewModel (graceful fallback)
- Hapus `Include(Questions).ThenInclude(Options)` dari SubmitExam query

**AdminController:**
- Hapus Include(Questions/Responses) dari DeleteAssessment dan DeleteAssessmentGroup
- Hapus UserResponses/AssessmentOptions/AssessmentQuestions delete blocks dari DeleteAssessment
- Hapus legacy delete loop dalam DeleteAssessmentGroup foreach
- Hapus legacy questionCountMap else-branch di AssessmentMonitoringDetail
- Hapus legacy questionCountMap else-branch di ExportAssessmentResults
- Hapus UserResponses delete dari ResetSession
- Hapus UserResponses delete dari DeleteWorker
- Hapus AkhiriUjian legacy path (AssessmentQuestion + UserResponse scoring)
- Hapus seluruh region Question Management (ManageQuestions, AddQuestion, DeleteQuestion)
- Sederhanakan ExamDetailDebug answeredCount ke PackageUserResponses saja

**Views:**
- Hapus `Views/Admin/ManageQuestions.cshtml` (orphaned legacy view)
- Hapus `SAVE_LEGACY_URL` dan `IS_PACKAGE_PATH` dari StartExam.cshtml JavaScript

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | fdcc87f | feat(227-02): EF migration — migrate legacy questions to packages and drop legacy tables |
| 2 | a066cad | feat(227-02): remove all legacy code paths from CMPController and AdminController |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] UserResponse tidak punya UserId column — SQL JOIN diperbaiki**
- **Found during:** Task 1 — migration failed at Step 4
- **Issue:** Plan SQL INSERT INTO UserPackageAssignments menggunakan `ur.UserId`, tapi model UserResponse tidak punya field UserId (hanya AssessmentSessionId)
- **Fix:** JOIN ke AssessmentSessions untuk mendapatkan UserId: `JOIN AssessmentSessions sess ON sess.Id = ur.AssessmentSessionId`
- **Files modified:** `Migrations/20260322032905_MigrateLegacyQuestionsAndDropTables.cs`
- **Commit:** fdcc87f

**2. [Rule 2 - Missing field] ShuffledOptionIdsPerQuestion required column**
- **Found during:** Task 1 — second migration attempt
- **Issue:** `UserPackageAssignments.ShuffledOptionIdsPerQuestion` NOT NULL constraint, tidak disertakan di INSERT
- **Fix:** Tambahkan kolom ke INSERT dengan default value `N'{}'`
- **Files modified:** `Migrations/20260322032905_MigrateLegacyQuestionsAndDropTables.cs`
- **Commit:** fdcc87f

**3. [Rule 3 - Blocking] Views/Admin/ManageQuestions.cshtml menyebabkan compile error**
- **Found during:** Task 1 — setelah delete model files
- **Issue:** View masih merender `@Model.Questions` yang sudah dihapus dari AssessmentSession
- **Fix:** Hapus file view (action sudah dihapus di Task 2 plan, view menjadi orphan)
- **Files modified:** `Views/Admin/ManageQuestions.cshtml` (deleted)
- **Commit:** a066cad

## Known Stubs

None — semua perubahan adalah penghapusan, bukan placeholder/stub.

## Self-Check: PASSED

- Migration file exists: `Migrations/20260322032905_MigrateLegacyQuestionsAndDropTables.cs` — FOUND
- Models/AssessmentQuestion.cs — DELETED (confirmed)
- Models/UserResponse.cs — DELETED (confirmed)
- Commit fdcc87f — FOUND
- Commit a066cad — FOUND
- `dotnet build` — 0 errors
