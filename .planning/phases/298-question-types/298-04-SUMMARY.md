---
phase: 298-question-types
plan: "04"
subsystem: GradingService
tags: [grading, multiple-answer, essay, scoring]
dependency_graph:
  requires: [298-01]
  provides: [MA-scoring, essay-flow-branching]
  affects: [GradingService.cs]
tech_stack:
  added: []
  patterns: [all-or-nothing scoring, status-guard branching, ToListAsync multi-row]
key_files:
  created: []
  modified:
    - Services/GradingService.cs
decisions:
  - "allResponses sebagai List<PackageUserResponse> (bukan Dictionary) untuk support MA multi-row per satu pertanyaan"
  - "ET scoring di-refactor ke switch/case per tipe — tidak ada double-count MC di-replace menjadi per-tipe"
  - "hasEssay branch return true sebelum TrainingRecord + sertifikat generation (D-18 enforcement)"
metrics:
  duration: "10 menit"
  completed_date: "2026-04-07"
  tasks_completed: 2
  files_modified: 1
---

# Phase 298 Plan 04: GradingService MA + Essay Scoring Summary

**One-liner:** MA all-or-nothing scoring via SetEquals dan Essay "Menunggu Penilaian" flow yang memblokir auto-complete hingga HC menilai manual.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | GradingService — Responses Query + MA All-or-Nothing | 80453ef8 | Services/GradingService.cs |
| 2 | GradingService — Essay "Menunggu Penilaian" Flow Branching | 1f8e7909 | Services/GradingService.cs |

## What Was Built

### Task 1: Responses Query + MA All-or-Nothing

- Responses query diubah dari `ToDictionaryAsync` ke `ToListAsync` sebagai `allResponses` — required karena MA menghasilkan multiple rows per pertanyaan
- MultipleChoice case: gunakan `allResponses.FirstOrDefault(r => r.PackageQuestionId == q.Id)`
- MultipleAnswer case: `SetEquals(correctOptionIds)` — all-or-nothing, pilih semua correct → skor penuh, kurang satu → skor 0
- Essay case: skip scoring, `maxScore` tetap include `q.ScoreValue` sebagai denominator
- ET scores section: refactor dari `responses.TryGetValue` ke switch/case per tipe (MC/MA/Essay) untuk konsistensi dan menghindari double-count

### Task 2: Essay Flow Branching

- `bool hasEssay` dihitung setelah scoring loop selesai
- Jika `hasEssay`: interim percentage dihitung, session di-update ke status `"Menunggu Penilaian"`, `HasManualGrading = true`, `IsPassed = null`, `Progress = 100`
- Race condition guard: `WHERE Status != "Completed" AND Status != "Menunggu Penilaian"`
- Essay branch `return true` sebelum TrainingRecord dan CertNumberHelper — sertifikat tidak di-generate (D-18)
- Non-essay flow: kode existing tetap tidak berubah (no regression)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - ET Scoring Refactor] ET scoring loop diubah ke switch/case konsisten**
- **Found during:** Task 1
- **Issue:** Plan menginstruksikan refactor ET scoring ke switch/case tapi tidak memberikan detail lengkap. Implementasi menggunakan pola yang sama persis dengan main scoring loop untuk konsistensi.
- **Fix:** ET loop kini menggunakan `switch (q.QuestionType ?? "MultipleChoice")` dengan case MC, MA, dan Essay — tidak ada double-count.
- **Files modified:** Services/GradingService.cs
- **Commit:** 80453ef8

## Known Stubs

Tidak ada stub. Essay skor 0 adalah intentional interim behavior (bukan stub) — akan di-finalize oleh FinalizeEssayGrading di Plan 05.

## Threat Surface Scan

Tidak ada surface baru. Perubahan murni internal scoring logic di GradingService — tidak ada endpoint baru, tidak ada perubahan auth path.

Threat mitigations dari plan:
- T-298-10 (MA scoring bypass): Ditegakkan — grade dari `allResponses` (DB), bukan POST payload
- T-298-11 (Essay premature completion): Ditegakkan — `hasEssay` branch `return true` sebelum sertifikat; status guard `"Menunggu Penilaian"` mencegah double-grade

## Self-Check: PASSED

- [x] Services/GradingService.cs — dimodifikasi dan dikonfirmasi
- [x] Commit 80453ef8 — ada di git log
- [x] Commit 1f8e7909 — ada di git log
- [x] `dotnet build` — 0 errors
- [x] `grep SetEquals` — ada di GradingService.cs
- [x] `grep "Menunggu Penilaian"` — ada di GradingService.cs
