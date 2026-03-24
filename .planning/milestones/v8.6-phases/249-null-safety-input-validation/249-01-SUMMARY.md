---
phase: 249-null-safety-input-validation
plan: "01"
subsystem: Controllers
tags: [null-safety, input-validation, cmp, admin, hardening]
dependency_graph:
  requires: []
  provides: [SAFE-01, SAFE-02, SAFE-03]
  affects: [CMPController, AdminController]
tech_stack:
  added: []
  patterns: [TryParse, GroupBy-First, nullable-tuple-return]
key_files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Controllers/AdminController.cs
decisions:
  - "Gunakan nullable tuple return type agar caller bisa deteksi user null tanpa exception"
  - "GroupBy + First() sebagai strategi skip-duplicate untuk ToDictionary bulk renewal"
metrics:
  duration: "~10 menit"
  completed: "2026-03-24"
  tasks: 2
  files: 2
---

# Phase 249 Plan 01: Null Safety & Input Validation Summary

**One-liner:** Defensive null guard di GetCurrentUserRoleLevelAsync + TryParse date + GroupBy-safe ToDictionary di bulk renewal.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Null-safe GetCurrentUserRoleLevelAsync + TryParse di CMPController | 3b89cda7 | Controllers/CMPController.cs |
| 2 | Guard ToDictionary duplicate key di bulk renewal AdminController | 15e79530 | Controllers/AdminController.cs |

## What Was Done

### SAFE-01: GetCurrentUserRoleLevelAsync null-safe
- Ubah return type dari `(ApplicationUser User, int RoleLevel)` menjadi `(ApplicationUser? User, int RoleLevel)`
- Hapus `user!` null-forgiving operator
- Tambah early return `(null, 0)` jika user null
- Tambah 5 guard `if (user == null) return RedirectToAction("Login", "Account")` di semua caller: Records, RecordsWorkerDetail, ExportRecordsTeamAssessment, ExportRecordsTeamTraining, dan partial export

### SAFE-02: DateTime.TryParse
- Ganti 6 occurrences `DateTime.Parse(dateFrom/dateTo)` dengan `DateTime.TryParse` pattern
- Handling otomatis untuk null/empty string (TryParse return false, result menjadi null)

### SAFE-03: ToDictionary duplicate-safe
- Ganti `sourceSessions.ToDictionary(s => s.UserId, s => s.Id)` dengan GroupBy + First()
- Ganti `sourceTrainings.ToDictionary(t => t.UserId ?? "", t => t.Id)` dengan GroupBy + First()
- Mencegah `ArgumentException: An item with the same key has already been added` pada bulk renewal

## Deviations from Plan

None — plan dieksekusi persis sesuai rencana.

## Verification

- `grep "TryParse" Controllers/CMPController.cs`: 6 occurrences
- `grep "DateTime\.Parse(" Controllers/CMPController.cs`: 0 occurrences
- `grep "user == null" Controllers/CMPController.cs`: 18 occurrences (termasuk kondisi lain, minimal 5 di caller)
- `grep "GroupBy.*UserId" Controllers/AdminController.cs`: 2 occurrences
- `dotnet build --no-restore`: 0 Error(s)

## Self-Check: PASSED
