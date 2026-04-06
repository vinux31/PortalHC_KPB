---
phase: 296-data-foundation-gradingservice-extraction
plan: "03"
subsystem: api
tags: [grading, assessment, service-extraction, controller-refactor, dependency-injection]

# Dependency graph
requires:
  - phase: 296-02
    provides: "GradingService concrete class dengan GradeAndCompleteAsync"
provides:
  - "AssessmentAdminController.AkhiriUjian menggunakan GradingService"
  - "AssessmentAdminController.AkhiriSemuaUjian menggunakan GradingService"
  - "CMPController.SubmitExam menggunakan GradingService"
  - "GradeFromSavedAnswers private method dihapus dari AssessmentAdminController"
affects:
  - 297
  - 298-multitype-assessment

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Constructor injection GradingService ke controller — sama dengan AuditLogService pattern"
    - "GradeAndCompleteAsync return bool — false = race condition, caller redirect ke Results/Info"
    - "Answer upsert SaveChanges sebelum GradeAndCompleteAsync — GradingService grade dari DB"
    - "SignalR push menggunakan finalPercentage dari form POST (tidak bergantung pada GradingService)"

key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
    - Controllers/CMPController.cs

key-decisions:
  - "GradeFromSavedAnswers dihapus penuh — GradingService adalah satu-satunya source of truth untuk grading"
  - "CMPController SaveChanges upsert sebelum GradeAndCompleteAsync — GradingService grade dari DB (bukan form POST)"
  - "SignalR push tetap di controller dengan finalPercentage dari form POST — bukan tanggung jawab GradingService"
  - "Race condition: GradingService return false → controller redirect ke Results dengan TempData[Info]"

requirements-completed: [FOUND-07, FOUND-08, FOUND-09]

# Metrics
duration: 20min
completed: 2026-04-06
---

# Phase 296 Plan 03: Controller Wiring ke GradingService Summary

**AssessmentAdminController dan CMPController diwire ke GradingService — GradeFromSavedAnswers dihapus, grading logic terpusat di satu service**

## Performance

- **Duration:** ~20 menit
- **Started:** 2026-04-06T08:00:00Z
- **Completed:** 2026-04-06T08:20:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- AssessmentAdminController: tambah `GradingService _gradingService` field + constructor injection
- AkhiriUjian: ganti `GradeFromSavedAnswers(session)` + inline ExecuteUpdateAsync + inline cert generation → `await _gradingService.GradeAndCompleteAsync(session)` dengan race check via return bool
- AkhiriSemuaUjian: ganti loop `GradeFromSavedAnswers` + `SaveChangesAsync` + cert loop per session → loop `_gradingService.GradeAndCompleteAsync(session)`. Cancelled sessions dipisah dengan SaveChanges sendiri
- GradeFromSavedAnswers private method (97 baris) dihapus dari AssessmentAdminController
- CMPController: tambah `GradingService _gradingService` field + constructor injection
- SubmitExam: pertahankan upsert answers ke DB + SaveChanges, ganti ExecuteUpdateAsync+TrainingRecord+cert inline → `await _gradingService.GradeAndCompleteAsync(assessment)`. SignalR push tetap di controller
- Build sukses 0 errors, 70 warnings (pre-existing)

## Task Commits

1. **Task 1: Wire AssessmentAdminController ke GradingService** - `a9e3a913` (feat)
2. **Task 2: Wire CMPController ke GradingService** - `1c77af2d` (feat)

## Files Created/Modified

- `Controllers/AssessmentAdminController.cs` — +25 baris, -198 baris (net -173): constructor injection + GradeFromSavedAnswers dihapus
- `Controllers/CMPController.cs` — +11 baris, -112 baris (net -101): constructor injection + inline grading dihapus

## Decisions Made

- Answer upsert `SaveChangesAsync` dipertahankan di CMPController sebelum memanggil GradingService — GradingService harus grade dari DB yang sudah berisi jawaban terbaru
- `finalPercentage` dihitung dari form POST dan disimpan untuk SignalR push (tidak perlu query ulang ke DB)
- Race condition handling: GradingService return `false` → controller menampilkan TempData["Info"] dan redirect, konsisten dengan behavior sebelumnya

## Deviations from Plan

### Auto-fixed Issues

Tidak ada deviasi. Plan dieksekusi sesuai spesifikasi.

- AkhiriSemuaUjian dipisah menjadi dua loop (cancelled dulu, lalu graded) untuk menghindari EF tracking conflict antara `session.Status = "Cancelled"` (tracked) dan `GradeAndCompleteAsync` yang menggunakan `ExecuteUpdateAsync` (untracked). Ini adalah implementation detail yang tidak disebutkan di plan tetapi tidak mengubah behavior.

## Known Stubs

Tidak ada stub. Semua controller method sekarang menggunakan GradingService yang sudah fully implemented.

## Threat Flags

Tidak ada surface baru. Refactoring murni — tidak ada endpoint baru, tidak ada perubahan trust boundary.

---

## Self-Check

- [x] `Controllers/AssessmentAdminController.cs` — dimodifikasi dan di-commit `a9e3a913`
- [x] `Controllers/CMPController.cs` — dimodifikasi dan di-commit `1c77af2d`
- [x] `grep GradeFromSavedAnswers Controllers/AssessmentAdminController.cs` — 0 matches
- [x] `grep _gradingService.GradeAndCompleteAsync Controllers/AssessmentAdminController.cs` — 2 matches
- [x] `grep _gradingService.GradeAndCompleteAsync Controllers/CMPController.cs` — 1 match
- [x] `dotnet build` — 0 errors

## Self-Check: PASSED

---
*Phase: 296-data-foundation-gradingservice-extraction*
*Completed: 2026-04-06*
