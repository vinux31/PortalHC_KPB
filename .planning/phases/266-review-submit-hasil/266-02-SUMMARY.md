---
phase: 266-review-submit-hasil
plan: 02
subsystem: assessment
tags: [cmp, exam, certificate, questpdf, bug-fix]

requires:
  - phase: 265-worker-exam-flow
    provides: ExamSummary dan CertificatePdf yang telah diuji UAT

provides:
  - ExamSummary POST yang memfilter jawaban value=0 sehingga unanswered count akurat
  - CertificatePdf dengan try-catch robust — tidak lagi return HTTP 204 jika gagal

affects: [267, review-submit-hasil-uat]

tech-stack:
  added: []
  patterns:
    - "Filter answers dictionary sebelum serialize ke TempData: kvp.Value > 0"
    - "Wrap QuestPDF font registration dan generation dalam try-catch dengan redirect fallback"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "Filter dilakukan di POST ExamSummary sebelum TempData serialize — solusi paling minimal tanpa mengubah view atau model"
  - "CertificatePdf: catch exception dan redirect ke Results page daripada membiarkan 204 atau unhandled exception"

patterns-established:
  - "Hidden input value=0 pattern: selalu filter sebelum serialize ke session/TempData"
  - "QuestPDF generation: wajib dibungkus try-catch dengan meaningful error redirect"

requirements-completed: [SUBMIT-02, CERT-01]

duration: 15min
completed: 2026-03-27
---

# Phase 266 Plan 02: Fix ExamSummary Warning Logic dan CertificatePdf 204

**Filter jawaban value=0 di ExamSummary POST dan bungkus CertificatePdf generation dalam try-catch sehingga dua UAT bug major tertutup**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-27T~08:00Z
- **Completed:** 2026-03-27
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments

- ExamSummary POST sekarang memfilter entri `value=0` (hidden input soal belum dijawab) sehingga unanswered count akurat dan warning kuning tampil
- CertificatePdf font registration dibungkus try-catch — tidak crash jika direktori fonts tidak ada di server dev
- CertificatePdf PDF generation dibungkus try-catch dengan zero-byte check — redirect ke Results page dengan error message jika gagal, tidak lagi return HTTP 204

## Task Commits

1. **Task 1 + Task 2: Fix ExamSummary unanswered detection dan CertificatePdf robustness** - `759e05e0` (fix)

## Files Created/Modified

- `Controllers/CMPController.cs` — Filter validAnswers di ExamSummary POST, wrap font registration dan PDF generation dalam try-catch

## Decisions Made

- Filter dilakukan di POST action sebelum serialize ke TempData — solusi minimal, tidak perlu mengubah view atau ExamSummaryItem model
- Redirect ke Results page jika PDF gagal, daripada membiarkan exception naik ke middleware yang menyebabkan HTTP 204

## Deviations from Plan

None — plan dieksekusi tepat seperti yang ditulis.

## Issues Encountered

None.

## User Setup Required

None — tidak ada konfigurasi eksternal yang diperlukan.

## Next Phase Readiness

- Kedua fix siap untuk di-deploy ke server dev dan re-test UAT
- ExamSummary: submit exam dengan beberapa soal belum dijawab seharusnya menampilkan warning dan count yang akurat
- CertificatePdf: jika fonts tidak ada di server dev, akan fallback ke default fonts QuestPDF; jika gagal total, user mendapat error message bukan blank page

---
*Phase: 266-review-submit-hasil*
*Completed: 2026-03-27*
