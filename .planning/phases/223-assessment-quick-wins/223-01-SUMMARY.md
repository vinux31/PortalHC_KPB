---
phase: 223-assessment-quick-wins
plan: 01
subsystem: assessment
tags: [model, migration, et-scores, submitted-at, data-integrity]
dependency_graph:
  requires: []
  provides: [SessionElemenTeknisScore model, UserResponse.SubmittedAt, ET score persistence]
  affects: [CMPController.SubmitExam, CMPController.SaveLegacyAnswer, AdminController.GradeFromSavedAnswers]
tech_stack:
  added: [SessionElemenTeknisScore EF entity]
  patterns: [EF unique composite index, upsert with SubmittedAt audit trail]
key_files:
  created:
    - Models/SessionElemenTeknisScore.cs
    - Migrations/20260321161415_AddSessionETScoreAndUserResponseSubmittedAt.cs
  modified:
    - Models/UserResponse.cs
    - Data/ApplicationDbContext.cs
    - Controllers/CMPController.cs
    - Controllers/AdminController.cs
decisions:
  - ET score persist di package path only — legacy path skip (AssessmentQuestion tidak punya ElemenTeknis field)
  - Unique index (AssessmentSessionId, ElemenTeknis) di DB level untuk data integrity
  - "Lainnya" sebagai fallback key untuk soal tanpa tag ET
metrics:
  duration: ~15 minutes
  completed_date: "2026-03-22"
  tasks_completed: 2
  files_changed: 6
---

# Phase 223 Plan 01: Assessment Quick Wins — ET Scores & SubmittedAt Summary

**One-liner:** Persist skor per ElemenTeknis ke tabel SessionElemenTeknisScores dan tambah SubmittedAt audit trail pada UserResponse untuk analytics Phase 224.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Model + Migration | e7d325a | Models/SessionElemenTeknisScore.cs, Models/UserResponse.cs, Data/ApplicationDbContext.cs, Migrations/ |
| 2 | Persist ET Scores + SubmittedAt | a6becc8 | Controllers/CMPController.cs, Controllers/AdminController.cs |

## What Was Built

### Task 1: Model + Migration
- Buat `Models/SessionElemenTeknisScore.cs` dengan 5 properties: Id, AssessmentSessionId (FK), ElemenTeknis, CorrectCount, QuestionCount
- Tambah `public DateTime? SubmittedAt { get; set; }` ke `Models/UserResponse.cs`
- Tambah `DbSet<SessionElemenTeknisScore> SessionElemenTeknisScores` ke ApplicationDbContext
- Konfigurasi unique composite index `IX_SessionElemenTeknisScores_AssessmentSessionId_ElemenTeknis` via Fluent API
- EF migration applied ke DB: tabel SessionElemenTeknisScores terbuat, kolom SubmittedAt ditambah ke UserResponses

### Task 2: Logic Persist
- **CMPController.SubmitExam package path**: GroupBy ElemenTeknis (fallback "Lainnya"), hitung correct per group, persist SessionElemenTeknisScore sebelum SaveChanges
- **AdminController.GradeFromSavedAnswers package path**: sama seperti package path di CMPController, menggunakan `responses` dictionary yang sudah di-load
- **Legacy path komentar**: komentar eksplisit bahwa ET scores tidak dipersist (AssessmentQuestion tidak punya ElemenTeknis)
- **CMPController.SaveLegacyAnswer**: tambah `.SetProperty(r => r.SubmittedAt, DateTime.UtcNow)` pada ExecuteUpdateAsync + `SubmittedAt = DateTime.UtcNow` pada insert
- **CMPController.SubmitExam legacy path**: tambah `SubmittedAt = DateTime.UtcNow` pada update dan insert UserResponse (4 lokasi total)

## Decisions Made

1. **ET persist: package path only** — Legacy path (AssessmentQuestion) tidak punya field ElemenTeknis, sehingga ET scoring tidak bisa dilakukan. Komentar eksplisit ditambah di kedua tempat.
2. **Fallback "Lainnya"** — Soal tanpa tag ET dikelompokkan di bawah key "Lainnya", bukan dilewati, sehingga 100% soal terhitung dalam ET breakdown.
3. **Unique index di DB level** — Garantia bahwa tidak ada duplikat (SessionId, ElemenTeknis) meski ada race condition.

## Deviations from Plan

None — plan dieksekusi persis seperti yang tertulis.

## Self-Check

- [x] `Models/SessionElemenTeknisScore.cs` ada — FOUND
- [x] `Models/UserResponse.cs` berisi `SubmittedAt` — FOUND
- [x] `Data/ApplicationDbContext.cs` berisi `DbSet<SessionElemenTeknisScore>` — FOUND
- [x] Migration file ada di Migrations/ — FOUND
- [x] `dotnet build` exit 0 — PASSED
- [x] `SessionElemenTeknisScores.Add` di CMPController — FOUND (line 1496)
- [x] `SessionElemenTeknisScores.Add` di AdminController — FOUND (line 2858)
- [x] `SubmittedAt = DateTime.UtcNow` di CMPController — FOUND (6 lokasi)
- [x] Komentar legacy path — FOUND

## Self-Check: PASSED
