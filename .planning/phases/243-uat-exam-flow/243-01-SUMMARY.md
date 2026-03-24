---
phase: 243-uat-exam-flow
plan: 01
subsystem: testing
tags: [exam-flow, uat, code-review, timer, auto-save, resume]

requires:
  - phase: 241-seed-data-uat
    provides: "Seed data UAT: AssessmentSession + packages untuk user Rino"
  - phase: 242-preview-package
    provides: "PreviewPackage UI dan validasi soal sebelum exam"
provides:
  - "Code review hasil EXAM-01 s/d EXAM-04 dengan status OK/BUG per item"
  - "UAT checkpoint: user verifikasi 4 skenario di browser"
affects: [243-02, grading, certificate]

tech-stack:
  added: []
  patterns:
    - "Wall-clock timer (Date.now() anchor) untuk drift-free countdown"
    - "Debounced auto-save per klik dengan retry 3x exponential backoff"
    - "Resume state via ElapsedSeconds + LastActivePage + SavedAnswers JSON"

key-files:
  created: []
  modified: []

key-decisions:
  - "EXAM-01 OK: Assessment() memfilter by UserId, VerifyToken() via TempData, StartExam() transition Open→InProgress"
  - "EXAM-02 OK: Fisher-Yates shuffle di BuildCrossPackageAssignment, SaveAnswer upsert via PackageUserResponses"
  - "EXAM-03 OK: Timer menggunakan wall-clock anchor (Date.now()), warning CSS text-danger ≤300s, auto-submit via form.submit() setelah modal 10s"
  - "EXAM-04 OK: Resume via IsResume=true, RemainingSeconds = DurationSeconds - ElapsedSeconds, SavedAnswers JSON dari DB"

requirements-completed: [EXAM-01, EXAM-02, EXAM-03, EXAM-04]

duration: 15min
completed: 2026-03-24
---

# Phase 243 Plan 01: UAT Exam Flow — Code Review Summary

**Code review alur ujian (start/take/timer/resume) selesai: semua 4 requirement EXAM-01 s/d EXAM-04 OK tanpa bug — siap UAT browser.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-24T07:36:00Z
- **Completed:** 2026-03-24T07:51:00Z
- **Tasks:** 1 of 2 (Task 2 = checkpoint human-verify)
- **Files modified:** 0 (code review only)

## Accomplishments

- Code review menyeluruh pada CMPController.cs (Assessment, VerifyToken, StartExam, SaveAnswer, UpdateSessionProgress, SubmitExam)
- Code review pada Views/CMP/Assessment.cshtml dan Views/CMP/StartExam.cshtml
- Semua 4 requirement EXAM-01 s/d EXAM-04 dinyatakan OK

## Hasil Code Review

### EXAM-01 — Assessment list + token + start: OK

- `CMPController.Assessment()` (baris 182-261): Query menggunakan `Where(a => a.UserId == userId)` — filter by user yang login. Benar.
- `Views/CMP/Assessment.cshtml`: Tombol "Start Assessment" dengan token menggunakan class `btn-start-token` → trigger JS `openTokenModal()` → AJAX ke `VerifyToken`. Token input modal ada, verifikasi di server. Benar.
- `CMPController.VerifyToken()` (baris 667-701): Cek `assessment.AccessToken != token.ToUpper()`, set `TempData[$"TokenVerified_{id}"] = true`, redirect ke StartExam. Benar.
- `CMPController.StartExam()` (baris 705-957): Cek `TempData.Peek($"TokenVerified_{id}")` untuk token-required sessions. Transition `Open→InProgress` dengan `assessment.StartedAt = DateTime.UtcNow`. Benar.

### EXAM-02 — Soal acak, pagination, auto-save: OK

- `StartExam()` (baris 811-844): `BuildCrossPackageAssignment()` menggunakan Fisher-Yates shuffle (`Shuffle<T>(List<T>, Random)`). `ShuffledQuestionIds` disimpan ke `UserPackageAssignment`. Benar.
- `Views/CMP/StartExam.cshtml` JS: Pagination per 10 soal (`QUESTIONS_PER_PAGE = 10`), fungsi `changePage()` + `performPageSwitch()`. Benar.
- Auto-save: event listener `radio.addEventListener('change', ...)` → `saveAnswerWithDebounce(qId, optId)` → debounce 300ms → `saveAnswerAsync()` dengan retry 3x. Benar.
- `CMPController.SaveAnswer()` (baris 266-322): Upsert ke `PackageUserResponses` menggunakan `ExecuteUpdateAsync()` + insert jika tidak ada. Benar.

### EXAM-03 — Timer countdown + warning + auto-submit: OK

- Timer JS (baris 330-368): `timerStartWallClock = Date.now()` sebagai anchor, `wallElapsed = Math.floor((Date.now() - timerStartWallClock) / 1000)` — wall-clock based, bukan naive setInterval. Drift-free. Benar.
- Warning visual: `if (remaining <= WARNING_THRESHOLD)` (WARNING_THRESHOLD = 300 = 5 menit) → `el.className = 'fw-bold fs-5 text-danger'`. Benar.
- Auto-submit: `if (remaining <= 0)` → show `timeUpWarningModal` → `document.getElementById('examForm').submit()` setelah 10 detik (atau saat user klik OK). Benar.

### EXAM-04 — Disconnect/resume: OK

- `StartExam()` (baris 907-930): `isResume = assessment.StartedAt != null`, `elapsedSec = assessment.ElapsedSeconds`, `remainingSeconds = durationSeconds - elapsedSec`. `ViewBag.RemainingSeconds` diset akurat. Benar.
- `ViewBag.LastActivePage = assessment.LastActivePage ?? 0` diset dari DB. Benar.
- `ViewBag.SavedAnswers` di-load dari `PackageUserResponses` ke Dictionary JSON. Benar.
- JS (baris 530-566): `prePopulateAnswers()` apply radio buttons dari `SAVED_ANSWERS`, track `answeredQuestions`. Benar.
- Resume modal (baris 765-783): jika `IS_RESUME && RESUME_PAGE > 0`, muncul modal konfirmasi, lalu navigate ke `page_RESUME_PAGE`. Benar.
- Timer resume: `timeRemaining = REMAINING_SECONDS_FROM_DB` — server sudah hitung sisa waktu. Benar.

## Task Commits

1. **Task 1: Code review EXAM-01 s/d EXAM-04** — tidak ada perubahan kode (code review only)

**Plan metadata:** (docs commit setelah task 2 selesai)

## Files Created/Modified

Tidak ada perubahan kode — task ini adalah code review analisis.

## Decisions Made

- EXAM-01 OK: Flow token → VerifyToken TempData → StartExam transition sudah benar
- EXAM-02 OK: Fisher-Yates shuffle + SaveAnswer upsert sudah benar
- EXAM-03 OK: Wall-clock timer (Date.now()) bebas drift, warning CSS ≤300s, auto-submit modal
- EXAM-04 OK: Resume state (ElapsedSeconds + LastActivePage + SavedAnswers JSON) sudah benar

## Deviations from Plan

Tidak ada — code review selesai tanpa bug critical yang perlu difix.

## Issues Encountered

Tidak ada bug critical/blocking ditemukan. Semua 4 requirement EXAM-01 s/d EXAM-04 implementasinya benar.

## Next Phase Readiness

- Code review OK — siap UAT browser (Task 2: checkpoint human-verify)
- User perlu login sebagai Rino dan verifikasi 4 skenario: start exam, take exam, timer countdown, disconnect/resume

---
*Phase: 243-uat-exam-flow*
*Completed: 2026-03-24*
