---
phase: 298-question-types
plan: 02
subsystem: assessment-import
tags: [excel-import, question-types, template, bulk-import]
dependency_graph:
  requires: [298-01]
  provides: [bulk-import-MC-MA-Essay, 4-template-variants]
  affects: [Controllers/AssessmentAdminController.cs, Views/Admin/ImportPackageQuestions.cshtml]
tech_stack:
  added: []
  patterns: [ClosedXML Excel parsing, multi-correct parsing, backward-compat default]
key_files:
  modified:
    - Controllers/AssessmentAdminController.cs
    - Views/Admin/ImportPackageQuestions.cshtml
decisions:
  - "Backward compat: QuestionType kosong/invalid di-default ke MultipleChoice (D-12)"
  - "MA correctLetters di-parse dari comma-separated, filter hanya A-D (T-298-05 whitelist)"
  - "Essay: PackageOption tidak dibuat, hanya QuestionText + Rubrik yang disimpan"
  - "validRowCount untuk cross-package: Essay dihitung valid hanya dengan QuestionText"
metrics:
  duration: 15min
  completed_date: 2026-04-07
  tasks_completed: 2
  files_modified: 2
---

# Phase 298 Plan 02: Excel Import Multi-Type Questions Summary

**One-liner:** Template Excel 4 varian (MC/MA/Essay/Universal) + import parser mendukung multi-correct MA dan Essay tanpa opsi, dengan backward compat untuk file lama.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Update DownloadQuestionTemplate — 4 Varian | 90c1ec50 | Controllers/AssessmentAdminController.cs |
| 2 | Update ImportPackageQuestions Parser + View 4 Tombol | 7ee8ba40 | Controllers/AssessmentAdminController.cs, Views/Admin/ImportPackageQuestions.cshtml |

## What Was Built

### Task 1 — DownloadQuestionTemplate 4 Varian

Method `DownloadQuestionTemplate` diupdate dengan parameter `string type = "MC"` (whitelist: MC/MA/Essay/Universal).

Header 9 kolom seragam untuk semua varian:
`Pertanyaan | Opsi A | Opsi B | Opsi C | Opsi D | Jawaban Benar | Elemen Teknis | QuestionType | Rubrik`

Contoh baris per tipe:
- **MC**: single correct `A`, QuestionType `MultipleChoice`
- **MA**: multi-correct `A,C`, QuestionType `MultipleAnswer`
- **Essay**: opsi kosong, QuestionType `Essay`, Rubrik diisi
- **Universal**: ketiga contoh baris di atas sekaligus

Filename: `Template_Soal_{type}.xlsx`

### Task 2 — ImportPackageQuestions Parser + View

**Parser update:**
- Baca kolom 8 (QuestionType) dan kolom 9 (Rubrik) dari Excel
- `NormalizeQuestionType()`: whitelist server-side, default MC jika kosong/invalid (T-298-04 + D-12)
- MA: parse `correctLetters` dari format `A,C` — filter hanya huruf A-D (T-298-05)
- Essay: skip validasi opsi A-D dan Jawaban Benar, tidak buat `PackageOption`
- `PackageQuestion` dibuat dengan field `QuestionType`, `Rubrik` (Essay only), `MaxCharacters = 2000`
- Cross-package `validRowCount`: Essay dihitung valid bila `QuestionText` tidak kosong
- Paste text mode juga mendukung kolom 8-9

**View update:**
- 1 tombol lama diganti 4 tombol: Template MC, MA, Essay, Universal
- Info format baru: 9 kolom, penjelasan QuestionType, format MA multi-correct, Essay rubrik wajib
- Catatan backward compat: file lama tanpa QuestionType otomatis jadi MultipleChoice

## Deviations from Plan

None — plan dieksekusi sesuai spesifikasi.

## Threat Mitigations Applied

| Threat ID | Mitigation | Location |
|-----------|-----------|----------|
| T-298-04 | Server-side whitelist QuestionType, default MC jika invalid | `NormalizeQuestionType()` di parser |
| T-298-05 | Parse MA correct letters hanya A-D via `.Where(s => new[] {"A","B","C","D"}.Contains(s))` | Loop rows, MA branch |

## Self-Check: PASSED

- `Controllers/AssessmentAdminController.cs` — FOUND (modified)
- `Views/Admin/ImportPackageQuestions.cshtml` — FOUND (modified)
- Commit `90c1ec50` — FOUND
- Commit `7ee8ba40` — FOUND
- `dotnet build` — 0 errors, 70 warnings (pre-existing LDAP CA1416 warnings)
