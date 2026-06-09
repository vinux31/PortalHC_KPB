---
phase: 357-standarisasi-istilah-tipe-soal
plan: 01
subsystem: question-type-labels
tags: [label, helper, tdd]
requires: []
provides: [QuestionTypeLabels-new-wording, QuestionTypeLabelsTests]
affects: [Models/QuestionTypeLabels.cs]
tech-stack:
  added: []
  patterns: [single-source-helper, tdd-red-green]
key-files:
  created:
    - HcPortal.Tests/QuestionTypeLabelsTests.cs
  modified:
    - Models/QuestionTypeLabels.cs
key-decisions:
  - "Helper Long/Short rebrand ke Single Answer/Multiple Answer/Essay (override Phase 305)"
  - "BadgeClass + switch keys (DB enum) TIDAK diubah - no migration"
requirements-completed: [LBL-02]
duration: ~6 min
completed: 2026-06-09
---

# Phase 357 Plan 01: Helper Rebrand (Grup A) Summary

`QuestionTypeLabels.Long()`/`Short()` (+ fallback) di-rebrand dari "Single Choice / Multiple Answers" → **"Single Answer / Multiple Answer / Essay"**. TDD: test ditulis dulu (RED 6/8 fail), helper diubah (GREEN 8/8). `BadgeClass()` + switch keys (DB enum) utuh → no migration. Surface yang sudah pakai helper (badge L77, flash TempData) otomatis ikut.

## Tasks
- **Task 1 (RED)** `test(357-01)`: QuestionTypeLabelsTests [Theory] lock wording baru → 6 fail/2 pass (helper masih lama) = RED benar.
- **Task 2 (GREEN)** `59dd71e1`: edit Long/Short strings → 8/8 pass.

## Verification
- `dotnet test --filter "FullyQualifiedName~QuestionTypeLabels"` → 8/8 passed (18ms).
- grep "Single Choice"/"Multiple Answers" di QuestionTypeLabels.cs = 0; BadgeClass 4 return utuh.

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None.

## Next Phase Readiness
Helper final wording baru. Ready 357-02 (konsumsi helper di dropdown/badge + Excel + dead-code) + 357-03 (docs, paralel).
