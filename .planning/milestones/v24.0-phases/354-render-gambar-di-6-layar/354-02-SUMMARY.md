---
phase: 354-render-gambar-di-6-layar
plan: 02
subsystem: viewmodels
tags: [viewmodel, image-plumbing, poco]
requires: []
provides:
  - ImagePath/ImageAlt di ExamQuestionItem, ExamOptionItem, ExamSummaryItem
  - ExamSummaryOptionItem carrier (opsi gambar ExamSummary RND-02)
  - ImagePath/ImageAlt di QuestionReviewItem, OptionReviewItem
  - ImagePath/ImageAlt di EssayGradingItemViewModel (soal saja)
  - ImagePath/ImageAlt di EditQuestionRow, EditOptionRow
affects:
  - Controllers/CMPController.cs (Plan 03 populate)
  - Controllers/AssessmentAdminController.cs (Plan 04 populate)
tech-stack:
  added: []
  patterns: [nullable-string-poco-field, separate-list-carrier]
key-files:
  created: []
  modified:
    - Models/PackageExamViewModel.cs
    - Models/AssessmentResultsViewModel.cs
    - Models/AssessmentMonitoringViewModel.cs
    - Models/EditPesertaAnswersViewModel.cs
key-decisions:
  - "ExamSummaryItem (text-only, tanpa list objek opsi) dapat carrier List<ExamSummaryOptionItem> OptionImages terpisah — render gambar opsi block-bawah RND-02 tanpa ubah logika tabel jawaban existing"
  - "EssayGradingItemViewModel HANYA ImagePath/ImageAlt soal (essay tak punya opsi, RND-05/L-01) — TIDAK tambah field opsi"
  - "Semua field nullable string (string?) ikut konvensi entity AssessmentPackage.cs — property existing tak diubah, hanya tambah"
requirements-completed: []
duration: 6 min
completed: 2026-06-09
---

# Phase 354 Plan 02: Plumbing Data Gambar 4 ViewModel Summary

Menambah field `ImagePath`/`ImageAlt` ke item-per-soal/opsi di 4 ViewModel agar gambar mengalir DB→controller→view (Gap 2, L-01). `ExamSummaryItem` (sebelumnya text-only) disiapkan membawa gambar soal + carrier `ExamSummaryOptionItem` untuk gambar opsi block-bawah (RND-02). `EssayGradingItemViewModel` soal-saja (RND-05).

**Tasks:** 3 | **Files:** 4 modified | **Duration:** ~6 min

## What was built

- **PackageExamViewModel.cs** — `ExamQuestionItem`+`ExamOptionItem` dapat ImagePath/ImageAlt (RND-01); `ExamSummaryItem` dapat ImagePath/ImageAlt + `List<ExamSummaryOptionItem> OptionImages`; class baru `ExamSummaryOptionItem` (RND-02).
- **AssessmentResultsViewModel.cs** — `QuestionReviewItem`+`OptionReviewItem` dapat ImagePath/ImageAlt (RND-03).
- **AssessmentMonitoringViewModel.cs** — `EssayGradingItemViewModel` dapat ImagePath/ImageAlt soal saja (RND-05).
- **EditPesertaAnswersViewModel.cs** — `EditQuestionRow`+`EditOptionRow` dapat ImagePath/ImageAlt (RND-06).

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- Grep counts: PackageExam ImagePath=4, ExamSummaryOptionItem=1, OptionImages=1, Results=2, Monitoring=1, EditPeserta=2 — semua ≥ acceptance.
- Property existing (SelectedOptionTexts, TextAnswer, IsAnswered, IsEssayPending) tak terhapus.
- `dotnet build` → **Build succeeded** (0 error).

## Issues Encountered

None.

## Next Phase Readiness

Field VM siap diisi controller. Requirements RND-01/02/03/05/06 SENGAJA belum di-mark complete — baru terpenuhi user-facing setelah populate (Plan 03/04) + render (Plan 05/06). Ready for Wave 2 (Plan 03 CMPController populate + Plan 04 AssessmentAdminController populate).

## Self-Check: PASSED
