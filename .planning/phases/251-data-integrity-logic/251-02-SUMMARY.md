---
phase: 251-data-integrity-logic
plan: "02"
subsystem: CDPController / ApplicationDbContext
tags: [thread-safety, refactor, ef-migration, unique-index, data-integrity]
dependency_graph:
  requires: []
  provides: [DATA-06, DATA-02]
  affects: [Controllers/CDPController.cs, Data/ApplicationDbContext.cs, Migrations/]
tech_stack:
  added: []
  patterns: [C# tuple return, EF Core composite HasIndex]
key_files:
  created:
    - Migrations/20260324030227_ChangeUniqueIndexToComposite.cs
    - Migrations/20260324030227_ChangeUniqueIndexToComposite.Designer.cs
  modified:
    - Controllers/CDPController.cs
    - Data/ApplicationDbContext.cs
decisions:
  - "Tuple return (ProtonProgressSubModel, string) menggantikan private field _lastScopeLabel untuk thread-safety"
  - "Composite unique index (ParentId, Name) memungkinkan sub-unit/sub-kategori dengan nama sama di parent berbeda"
  - "FilterCoachingProton menggunakan var (model, _) karena tidak memerlukan scopeLabel"
metrics:
  duration: "8m"
  completed_date: "2026-03-24"
  tasks_completed: 2
  files_modified: 4
---

# Phase 251 Plan 02: Thread-Safe Scope Label + Composite Unique Index Summary

**One-liner:** Refactor _lastScopeLabel ke tuple return untuk thread-safety dan ubah unique index OrganizationUnit/AssessmentCategory ke composite (ParentId, Name) via EF Core migration.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Refactor _lastScopeLabel ke tuple return (DATA-06) | ab3f7b36 | Controllers/CDPController.cs |
| 2 | Composite unique index + EF Core migration (DATA-02) | b9172d7b | Data/ApplicationDbContext.cs, Migrations/* |

## What Was Built

### Task 1: Thread-Safe BuildProtonProgressSubModelAsync (DATA-06)

- Hapus private field `_lastScopeLabel = ""` dari CDPController
- Ubah return type `BuildProtonProgressSubModelAsync` dari `Task<ProtonProgressSubModel>` ke `Task<(ProtonProgressSubModel subModel, string scopeLabel)>`
- Dashboard() menggunakan tuple deconstruction: `var (progressData, scopeLabel) = await BuildProtonProgressSubModelAsync(...)`
- FilterCoachingProton() menggunakan `var (model, _)` karena hanya butuh subModel
- Eliminasi shared mutable state — controller kini thread-safe

### Task 2: Composite Unique Index (DATA-02)

- OrganizationUnit: `HasIndex(u => u.Name).IsUnique()` diubah ke `HasIndex(u => new { u.ParentId, u.Name }).IsUnique()`
- AssessmentCategory: `HasIndex(c => c.Name).IsUnique()` diubah ke `HasIndex(c => new { c.ParentId, c.Name }).IsUnique()`
- Migration `20260324030227_ChangeUniqueIndexToComposite` dibuat dengan operasi DropIndex + CreateIndex yang benar
- Sub-unit/sub-kategori dengan nama sama di parent berbeda kini diizinkan

## Deviations from Plan

**1. [Rule 2 - Missing functionality] FilterCoachingProton juga caller BuildProtonProgressSubModelAsync**

- Ditemukan saat: Task 1
- Masalah: Plan hanya menyebut Dashboard sebagai caller, padahal FilterCoachingProton (line 307) juga memanggil method ini
- Fix: Update FilterCoachingProton dengan `var (model, _)` untuk handle tuple return tanpa menggunakan scopeLabel
- Files modified: Controllers/CDPController.cs
- Commit: ab3f7b36

Tidak ada deviasi lain — plan dieksekusi sesuai spesifikasi.

## Known Stubs

Tidak ada stub.

## Self-Check: PASSED

- Controllers/CDPController.cs: FOUND — _lastScopeLabel = 0 baris, tuple return = 1 baris, tuple deconstruction Dashboard = 1 baris
- Data/ApplicationDbContext.cs: FOUND — composite index OrganizationUnit = 1 baris, composite index AssessmentCategory = 1 baris
- Migration file: FOUND — 20260324030227_ChangeUniqueIndexToComposite.cs
- Commits: ab3f7b36 (Task 1), b9172d7b (Task 2) — FOUND
- dotnet build: 0 errors
