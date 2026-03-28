---
phase: 272-block-submit-jika-belum-semua-soal-terisi
plan: 01
subsystem: ui
tags: [exam, submit-guard, frontend-disable, backend-validation, auto-submit]

# Dependency graph
requires:
  - phase: 266-review-submit-hasil
    provides: ExamSummary form + SubmitExam action yang menjadi target perubahan
  - phase: 271-fix-time-remaining
    provides: timerExpired logic di ExamSummary yang digunakan sebagai exception case

provides:
  - Tombol Kumpulkan Ujian disabled (type=button) jika unanswered>0 dan timer belum habis
  - Pesan warning yang jelas memberitahu submit diblokir (bukan hanya peringatan)
  - Backend guard di SubmitExam yang menolak manual submit jika soal kosong
  - isAutoSubmit flag yang memungkinkan auto-submit saat waktu habis tetap berjalan
affects: [273-fix-jawaban-tidak-bisa-disubmit, exam-flow, worker-ujian]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Frontend disabled button (type=button) + backend guard — dual-layer protection mencegah submit tidak lengkap"
    - "isAutoSubmit flag via hidden form field — membedakan manual submit vs auto-submit timer expired"

key-files:
  created: []
  modified:
    - Views/CMP/ExamSummary.cshtml
    - Controllers/CMPController.cs

key-decisions:
  - "Button disabled menggunakan type=button (bukan disabled attribute pada type=submit) agar tidak bisa di-bypass via JS remove attribute"
  - "Auto-submit exception: isAutoSubmit=false default pada form manual, auto-submit dari timer set true sebelum submit"
  - "Backend guard menggunakan pkgAssign.GetShuffledQuestionIds().Count sebagai total questions — konsisten dengan ExamSummary action"

patterns-established:
  - "Dual-layer submit guard: frontend (disabled button) + backend (validation in action) untuk keamanan berlapis"

requirements-completed: [BLOCK-01, BLOCK-02, BLOCK-03]

# Metrics
duration: 10min
completed: 2026-03-28
---

# Phase 272 Plan 01: Block Submit Jika Belum Semua Soal Terisi Summary

**Frontend disabled button + backend completeness guard dengan isAutoSubmit exception untuk auto-submit saat timer habis**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-03-28T~07:00Z
- **Completed:** 2026-03-28T~07:10Z
- **Tasks:** 1 of 2 (Task 2 adalah checkpoint human-verify, belum dilaksanakan)
- **Files modified:** 2

## Accomplishments
- Tombol "Kumpulkan Ujian" diganti menjadi `type="button" disabled` jika ada soal belum dijawab dan timer belum habis
- Pesan alert warning diperbarui: "Jawab semua soal terlebih dahulu sebelum mengumpulkan" (sebelumnya "Anda masih bisa mengumpulkan")
- Hidden field `isAutoSubmit=false` ditambahkan ke form submit
- `SubmitExam` signature diperbarui dengan parameter `bool isAutoSubmit = false`
- Blok validasi completeness ditambahkan di backend — reject manual submit jika ada soal kosong, auto-submit dikecualikan

## Task Commits

1. **Task 1: Frontend disable button + backend validation + auto-submit exception** - `364f9df4` (feat)

## Files Created/Modified
- `Views/CMP/ExamSummary.cshtml` - Disabled button saat soal kosong, pesan warning diperbarui, hidden field isAutoSubmit
- `Controllers/CMPController.cs` - Parameter isAutoSubmit + blok validasi completeness di SubmitExam

## Decisions Made
- Menggunakan `type="button"` untuk disabled state (bukan `disabled` pada `type="submit"`) — lebih robust, tidak bisa di-bypass
- isAutoSubmit default false — form manual selalu melewati validasi, hanya auto-submit JS yang harus set true secara eksplisit
- Backend validasi menggunakan pkgAssign.GetShuffledQuestionIds() — konsisten dengan cara ExamSummary action menghitung unanswered

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Perubahan siap diverifikasi di browser (Task 2: checkpoint human-verify)
- Setelah approved: Phase 273 (fix jawaban tidak bisa disubmit saat waktu habis) siap dieksekusi

## Self-Check: PASSED
- `Views/CMP/ExamSummary.cshtml` FOUND
- `Controllers/CMPController.cs` FOUND
- Commit `364f9df4` FOUND

---
*Phase: 272-block-submit-jika-belum-semua-soal-terisi*
*Completed: 2026-03-28*
