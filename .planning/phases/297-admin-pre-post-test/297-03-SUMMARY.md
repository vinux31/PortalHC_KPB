---
phase: 297-admin-pre-post-test
plan: "03"
subsystem: assessment-admin
tags: [pre-post-test, edit-assessment, manage-packages, peserta-sync]
dependency_graph:
  requires: [297-01]
  provides: [edit-assessment-prepost-ui, copy-packages-from-pre]
  affects: [Controllers/AssessmentAdminController.cs, Views/Admin/EditAssessment.cshtml, Views/Admin/ManagePackages.cshtml]
tech_stack:
  added: []
  patterns: [tab-layout-bootstrap, deep-clone-ef-core, cascade-delete-sessions]
key_files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
    - Views/Admin/EditAssessment.cshtml
    - Views/Admin/ManagePackages.cshtml
decisions:
  - EditAssessment Pre-Post: shared fields (Title, Category, PassPercentage, dll) di luar tab — per-phase jadwal di dalam tab Pre/Post
  - D-32 validasi: hapus peserta diblokir jika sesi InProgress atau Completed
  - CopyPackagesFromPre: validasi postSession.AssessmentType == "PostTest" dan LinkedSessionId sebelum copy
metrics:
  duration_minutes: 15
  completed_date: "2026-04-07"
  tasks_completed: 2
  files_modified: 3
---

# Phase 297 Plan 03: EditAssessment Pre-Post Tab + ManagePackages Copy Summary

**One-liner:** EditAssessment dengan tab Bootstrap Pre/Post + sinkronisasi peserta (tambah/hapus cascade) + ManagePackages CopyPackagesFromPre deep-clone.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | EditAssessment GET/POST — Tab Pre/Post + peserta sync | a96f5219 | Controllers/AssessmentAdminController.cs, Views/Admin/EditAssessment.cshtml |
| 2 | ManagePackages — tombol Copy dari Pre-Test + CopyPackagesFromPre action | f7b65dbf | Views/Admin/ManagePackages.cshtml |

## What Was Built

### Task 1: EditAssessment Pre-Post Tab Layout + Peserta Sync

**GET:**
- Deteksi `isPrePost` berdasarkan `AssessmentType == "PreTest" || "PostTest"`
- Load semua sessions dalam grup via `LinkedGroupId`
- Set `ViewBag.IsPrePostGroup`, `ViewBag.PreSession`, `ViewBag.PostSession`
- Hitung `PrePackageCount` dan `PostPackageCount` terpisah
- Override `AssignedUsers` dari Pre sessions (avoid duplicates)

**POST:**
- Validasi `PostSchedule > PreSchedule` (T-297-07)
- Update shared fields pada semua sessions dalam grup
- Update per-phase jadwal (PreSchedule/PostSchedule terpisah)
- **D-31 Tambah peserta:** buat Pre+Post session baru dengan LinkedSessionId
- **D-32 Hapus peserta:** validasi status tidak InProgress/Completed, cascade delete PackageUserResponses, AssessmentAttemptHistory, AssessmentPackages, lalu hapus kedua sessions

**View EditAssessment:**
- Tab Bootstrap `nav-tabs` dengan dua panel: `#tab-pre` dan `#tab-post`
- Shared fields (Title, Category, PassPercentage, dll) tetap di luar tab
- Setiap tab memiliki: date+time picker, DurationMinutes, ExamWindowCloseDate
- Tab Post-Test tambahan: GenerateCertificate, ValidUntil
- Link ke ManagePackages per-phase

### Task 2: ManagePackages Copy dari Pre-Test

**Controller ManagePackages GET:**
- Deteksi `isPostSession = assessment.AssessmentType == "PostTest"`
- Set `ViewBag.IsPostSession` dan `ViewBag.PreSessionId` (via LinkedSessionId)

**CopyPackagesFromPre action:**
- Validasi: postSession harus PostTest dan memiliki LinkedSessionId (T-297-06)
- Hapus semua existing Post packages (cascade: PackageOptions, PackageQuestions, AssessmentPackages)
- Deep clone Pre packages ke Post: PackageName, PackageNumber, Questions (QuestionText, Order, ScoreValue, QuestionType, ElemenTeknis) + Options (OptionText, IsCorrect)
- Tanpa referensi foreign key ke Pre packages

**View ManagePackages:**
- Tombol "Copy dari Pre-Test" hanya muncul jika `IsPostSession == true`
- Konfirmasi inline dengan `copyPreConfirm` div
- Warning: "Ini akan menimpa semua paket soal Post-Test yang ada"

## Deviations from Plan

None - semua implementasi sudah ada dari plan sebelumnya (297-01), plan ini memverifikasi dan mengcommit.

## Threat Mitigations Applied

| Threat ID | Mitigation |
|-----------|-----------|
| T-297-06 | CopyPackagesFromPre: validate postSession.AssessmentType == "PostTest" dan LinkedSessionId exists |
| T-297-07 | EditAssessment POST: validate PostSchedule > PreSchedule sebelum update |
| T-297-08 | D-32 hapus peserta: validate session.Status != InProgress/Completed sebelum delete |

## Self-Check: PASSED

- Controllers/AssessmentAdminController.cs: FOUND (contains ViewBag.IsPrePostGroup, CopyPackagesFromPre)
- Views/Admin/EditAssessment.cshtml: FOUND (contains nav-tabs, tab-pre, tab-post)
- Views/Admin/ManagePackages.cshtml: FOUND (contains Copy dari Pre-Test, copyPreConfirm)
- Commits a96f5219 dan f7b65dbf: FOUND
- dotnet build: PASSED (0 errors, 0 warnings)
