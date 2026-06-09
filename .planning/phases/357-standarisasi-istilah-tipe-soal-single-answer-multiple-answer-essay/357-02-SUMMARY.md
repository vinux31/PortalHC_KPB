---
phase: 357-standarisasi-istilah-tipe-soal
plan: 02
subsystem: question-type-labels
tags: [label, view, dead-code]
requires: [357-01]
provides: [dropdown-binding, badge-short, import-buttons, excel-SA, trueFalse-removed]
affects: [Views/Admin/ManagePackageQuestions.cshtml, Views/Admin/EditPesertaAnswers.cshtml, Views/Admin/ImportPackageQuestions.cshtml, Controllers/AssessmentAdminController.cs, Controllers/CMPController.cs]
tech-stack:
  added: []
  patterns: [helper-binding, dead-code-removal]
key-files:
  created: []
  modified:
    - Views/Admin/ManagePackageQuestions.cshtml
    - Views/Admin/EditPesertaAnswers.cshtml
    - Views/Admin/ImportPackageQuestions.cshtml
    - Controllers/AssessmentAdminController.cs
    - Controllers/CMPController.cs
key-decisions:
  - "Dropdown pakai @QuestionTypeLabels.Long() binding (D-06 single-source)"
  - "Badge EditPeserta pakai Short() label penuh (D-03 opsi-i)"
  - "Excel MC->SA (S1); dead TrueFalse dihapus tanpa ubah analitik 3 tipe"
requirements-completed: [LBL-02]
duration: ~8 min
completed: 2026-06-09
---

# Phase 357 Plan 02: Grup B Surface + Grup C Dead Code Summary

Surface hardcode dikonsolidasi ke helper: dropdown ManagePackageQuestions pakai `@QuestionTypeLabels.Long()` binding, badge EditPesertaAnswers pakai `Short()`, tombol Import "Single Answer/Multiple Answer", sel Excel per-peserta "MC"→"SA" (MA tetap). Dead code `"TrueFalse"` (2 spot CMPController) dihapus. value attribute + route key + JS handler + grading logic utuh.

## Tasks
- **Task 1** (`e6a32424`): dropdown binding + badge Short + Import buttons (value/route key preserved).
- **Task 2** (`739b92eb`): Excel SA/MA + hapus TrueFalse 2 spot (kondisi MultipleChoice verbatim).

## Verification
- `dotnet build` → 0 error. `dotnet test` → **143/143** (135 + 8 helper), 0 regresi.
- grep: dropdown `@QuestionTypeLabels.Long` (1) + value attr (3) utuh; EditPeserta `Short()` (1); Import "Template Single/Multiple Answer" (2) + route key MC/MA (2) utuh; Excel `? "SA" : "MA"` (1); CMPController "TrueFalse" = 0.

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None.

## Next Phase Readiness
Grup B/C selesai. 357-03 (docs, sudah paralel di Wave 1 — eksekusi inline berikutnya). Lalu 357-04 gate+UAT.
