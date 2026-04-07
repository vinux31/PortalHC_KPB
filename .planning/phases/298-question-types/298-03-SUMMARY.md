---
phase: 298-question-types
plan: 03
subsystem: assessment-worker
tags: [question-types, worker-exam, checkbox, textarea, signalr, auto-save, exam-summary]
dependency_graph:
  requires: [298-01]
  provides: [checkbox MA render, textarea Essay render, badge tipe soal, SaveTextAnswer hub, SaveMultipleAnswer hub, ExamSummary per-type]
  affects: [Views/CMP/StartExam.cshtml, Views/CMP/ExamSummary.cshtml, Controllers/CMPController.cs, Hubs/AssessmentHub.cs, Models/PackageExamViewModel.cs]
tech_stack:
  added: []
  patterns: [SignalR hub method, debounce JS, resume state restoration, STRIDE mitigations T-298-07/08/09]
key_files:
  created: []
  modified:
    - Views/CMP/StartExam.cshtml
    - Views/CMP/ExamSummary.cshtml
    - Controllers/CMPController.cs
    - Hubs/AssessmentHub.cs
    - Models/PackageExamViewModel.cs
decisions:
  - "SaveMultipleAnswer menghapus semua respons existing untuk questionId sebelum insert baru — clean delete+insert pattern untuk MA"
  - "SavedMultiAnswers dan SavedTextAnswers sebagai ViewBag terpisah untuk resume state (tidak campur dengan SAVED_ANSWERS MC)"
  - "ExamSummaryItem.IsAnswered menggunakan logic per-type: Essay=TextAnswer non-empty, MA=SelectedOptionTexts.Any(), MC=SelectedOptionId.HasValue"
metrics:
  duration_minutes: 20
  completed_date: "2026-04-07T06:32:31Z"
  tasks_completed: 2
  files_modified: 5
---

# Phase 298 Plan 03: Worker Exam UI — Multi-Type Summary

**One-liner:** Checkbox MA + textarea Essay di StartExam dengan SignalR auto-save per tipe dan ExamSummary ringkasan format per tipe soal.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | StartExam — Render per QuestionType + Auto-Save MA/Essay | 4ebfceb6 | StartExam.cshtml, AssessmentHub.cs, CMPController.cs, PackageExamViewModel.cs |
| 2 | ExamSummary — Ringkasan per Tipe Soal | 4ebfceb6 | ExamSummary.cshtml, CMPController.cs, PackageExamViewModel.cs |

## What Was Built

### AssessmentHub.cs
- **`SaveTextAnswer(sessionId, questionId, textAnswer)`** — upsert `PackageUserResponse.TextAnswer` untuk soal Essay. Validasi session ownership (T-298-07). Server-side truncate ke `MaxCharacters` (T-298-09).
- **`SaveMultipleAnswer(sessionId, questionId, optionIds)`** — delete existing rows + insert baru per optionId untuk soal MA. Validasi session ownership dan timer belum expired (T-298-08). Validasi optionIds milik questionId tersebut.

### PackageExamViewModel.cs — ExamSummaryItem
Ditambah field: `QuestionType`, `SelectedOptionTexts` (MA), `TextAnswer` (Essay). `IsAnswered` dihitung per-type.

### CMPController.cs
- **StartExam GET**: map `q.QuestionType` dan `q.MaxCharacters` ke `ExamQuestionItem`. Resume state dipisah menjadi `SavedAnswers` (MC), `SavedMultiAnswers` (MA), `SavedTextAnswers` (Essay) di ViewBag.
- **ExamSummary GET**: query semua `PackageUserResponse` sekaligus, build `ExamSummaryItem` per-type. MA: konversi optionId ke huruf (A/B/C/D). Essay: ambil `TextAnswer`. Unanswered count menggunakan `IsAnswered` multi-type.

### StartExam.cshtml
- **Rendering per QuestionType**: badge tipe di header soal, textarea Essay dengan char counter, checkbox MA dengan label "Pilih semua yang benar", radio MC tetap tidak berubah.
- **JS auto-save**: checkbox `.exam-checkbox` → `connection.invoke("SaveMultipleAnswer", ...)` immediate. textarea `.exam-essay` → `connection.invoke("SaveTextAnswer", ...)` debounce 2 detik. Char counter update real-time dengan warna warning/danger.
- **Resume state**: restore MC radio (tidak berubah), restore MA checkbox dari `SAVED_MULTI_ANSWERS`, restore Essay textarea dari `SAVED_TEXT_ANSWERS`.

### ExamSummary.cshtml
- MA: tampil "Jawaban: A, C" (join huruf yang dipilih)
- Essay: tampil 50 karakter pertama + "..." jika lebih panjang
- MC: tampil option text (existing behavior)
- Belum dijawab: teks merah bold untuk semua tipe

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 — Security] STRIDE T-298-07/08/09 diimplementasi di SaveTextAnswer dan SaveMultipleAnswer**
- **Found during:** Task 1
- **Issue:** Threat model plan memandatkan mitigasi ketiga ancaman di hub methods
- **Fix:** Validasi session ownership (T-298-07), validasi optionIds milik questionId + timer check (T-298-08), server-side truncate TextAnswer ke MaxCharacters (T-298-09)
- **Files modified:** Hubs/AssessmentHub.cs

**2. [Rule 2 — Correctness] ViewBag terpisah untuk resume state multi-type**
- **Found during:** Task 1
- **Issue:** `SavedAnswers` yang ada hanya mendukung MC (questionId → single optionId). MA butuh multiple optionIds, Essay butuh TextAnswer.
- **Fix:** Tambah `SavedMultiAnswers` dan `SavedTextAnswers` sebagai ViewBag terpisah di StartExam GET
- **Files modified:** Controllers/CMPController.cs, Views/CMP/StartExam.cshtml

## Known Stubs

Tidak ada stub. Semua data ter-wire dari DB melalui `PackageUserResponse`.

## Threat Flags

Tidak ada surface baru di luar threat model plan.

## Self-Check: PASSED

- [x] `Views/CMP/StartExam.cshtml` — FOUND (exam-checkbox, exam-essay, Multi Jawaban, charCount_)
- [x] `Views/CMP/ExamSummary.cshtml` — FOUND (MultipleAnswer, TextAnswer, Substring(0, 50))
- [x] `Hubs/AssessmentHub.cs` — FOUND (SaveTextAnswer, SaveMultipleAnswer)
- [x] `Models/PackageExamViewModel.cs` — FOUND (QuestionType di ExamSummaryItem)
- [x] Commit 4ebfceb6 — FOUND
- [x] dotnet build — 0 errors
