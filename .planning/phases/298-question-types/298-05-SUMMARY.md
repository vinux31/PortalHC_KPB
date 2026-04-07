---
phase: 298-question-types
plan: "05"
subsystem: ui
tags: [csharp, aspnet, ajax, essay-grading, monitoring]

# Dependency graph
requires:
  - phase: 298-03
    provides: HasManualGrading flag on AssessmentSession, EssayScore on PackageUserResponse
  - phase: 298-04
    provides: Status "Menunggu Penilaian" set saat submit ujian dengan Essay
provides:
  - SubmitEssayScore AJAX endpoint (save skor per soal Essay, validasi range 0-ScoreValue)
  - FinalizeEssayGrading AJAX endpoint (recalculate MC+MA+Essay score, Completed, sertifikat)
  - Inline essay grading UI di AssessmentMonitoringDetail per sesi HasManualGrading
  - Badge "Menunggu Penilaian" kuning + counter di AssessmentMonitoring list
affects: [299-worker-prepost, 301-reporting]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Essay grading UI: EssayGradingItemViewModel via ViewBag.EssayGradingMap dictionary (sessionId -> items)"
    - "AJAX grading: fetch POST ke /Admin/SubmitEssayScore dan /Admin/FinalizeEssayGrading dengan antiforgery token"
    - "Replay guard FinalizeEssayGrading: WHERE Status == 'Menunggu Penilaian' di ExecuteUpdateAsync"
    - "Monitoring badge: MenungguPenilaianCount aggregate di MonitoringGroupViewModel, populated di controller"

key-files:
  created: []
  modified:
    - Controllers/AssessmentAdminController.cs
    - Models/AssessmentMonitoringViewModel.cs
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - Views/Admin/AssessmentMonitoring.cshtml

key-decisions:
  - "EssayGradingMap via ViewBag (bukan nested dalam model) untuk menghindari model complexity — MonitoringGroupViewModel sudah kompleks"
  - "MenungguPenilaianCount sebagai aggregate field di MonitoringGroupViewModel — Sessions tidak di-populate di AssessmentMonitoring action (aggregate only)"
  - "Essay pending count di monitoring list tidak per-soal (butuh join mahal) — hanya menampilkan jumlah sesi Menunggu Penilaian"
  - "FinalizeEssayGrading reload halaman setelah sukses (bukan update DOM) — status berubah banyak kolom sekaligus"

patterns-established:
  - "Grading security: Authorize Admin/HC + ValidateAntiForgeryToken pada semua essay grading endpoints"
  - "Score range validation server-side: score < 0 || score > question.ScoreValue (T-298-13)"
  - "All-essay-graded check sebelum finalize: query EssayScore == null (T-298-14)"

requirements-completed: [QTYPE-10, QTYPE-11]

# Metrics
duration: 35min
completed: 2026-04-07
---

# Phase 298 Plan 05: Essay Grading Admin UI Summary

**HC dapat menilai Essay inline di AssessmentMonitoringDetail via AJAX, sistem recalculate skor total MC+MA+Essay dan generate sertifikat setelah finalize, badge "Menunggu Penilaian" muncul di monitoring list**

## Performance

- **Duration:** 35 min
- **Started:** 2026-04-07T~06:30Z
- **Completed:** 2026-04-07T~07:05Z
- **Tasks:** 2 auto (Task 3 = checkpoint:human-verify)
- **Files modified:** 4

## Accomplishments

- Endpoint `SubmitEssayScore`: validasi range 0-ScoreValue, simpan EssayScore, return pendingCount + allGraded flag
- Endpoint `FinalizeEssayGrading`: validasi semua Essay dinilai, recalculate total score MC+MA+Essay, update status Completed + IsPassed, generate TrainingRecord + sertifikat, NotifyIfGroupCompleted
- Inline essay grading UI: jawaban pekerja read-only, rubrik collapsible, input skor dengan badge status per soal, tombol "Selesaikan Penilaian" muncul saat semua dinilai
- Badge "Menunggu Penilaian" kuning + counter peserta di AssessmentMonitoring list

## Task Commits

1. **Task 1: SubmitEssayScore + FinalizeEssayGrading Endpoints** - `f7390f58` (feat)
2. **Task 2: Essay Grading UI + Badge Monitoring** - `c7be4db4` (feat)
3. **Task 3: Human Verify** - checkpoint (awaiting user)

## Files Created/Modified

- `Controllers/AssessmentAdminController.cs` - Tambah SubmitEssayScore + FinalizeEssayGrading endpoints, populate EssayPendingCountMap + EssayGradingMap di AssessmentMonitoringDetail, MenungguPenilaianCount di AssessmentMonitoring
- `Models/AssessmentMonitoringViewModel.cs` - Tambah HasManualGrading + EssayPendingCount ke MonitoringSessionViewModel, EssayGradingItemViewModel baru, MenungguPenilaianCount + EssayPendingTotal ke MonitoringGroupViewModel
- `Views/Admin/AssessmentMonitoringDetail.cshtml` - Essay grading section dengan grading cards, AJAX handlers SubmitEssayScore + FinalizeEssayGrading, status badge "Menunggu Penilaian"
- `Views/Admin/AssessmentMonitoring.cshtml` - Badge "Menunggu Penilaian" kuning + counter di kolom Status tabel

## Decisions Made

- `EssayGradingMap` via ViewBag (bukan nested dalam model) untuk menghindari menambah kompleksitas ke `MonitoringGroupViewModel` yang sudah banyak field
- `MenungguPenilaianCount` sebagai aggregate field karena `Sessions` tidak di-populate di `AssessmentMonitoring` action (hanya aggregate counts yang di-query)
- Monitoring list hanya tampilkan count sesi menunggu (bukan per-soal essay pending) — join mahal tidak perlu untuk overview
- `FinalizeEssayGrading` reload halaman setelah sukses untuk keamanan dan kesederhanaan (banyak kolom berubah sekaligus)

## Deviations from Plan

None — plan dieksekusi sesuai spesifikasi. Satu penyesuaian minor: monitoring badge menggunakan `MenungguPenilaianCount` aggregate (bukan iterasi `group.Sessions`) karena Sessions tidak di-populate di action monitoring list — ini mengikuti arsitektur yang sudah ada, bukan deviasi.

## Issues Encountered

- `AssessmentMonitoring` action tidak meng-populate `Sessions` list di tiap grup (hanya aggregate). View tidak dapat menghitung `menungguCount` dari `group.Sessions`. Solusi: tambah `MenungguPenilaianCount` ke `MonitoringGroupViewModel` dan populate di query controller.

## Known Stubs

Tidak ada stub — semua data di-query dari database secara real-time.

## Next Phase Readiness

- Essay grading flow lengkap: submit skor per soal → finalize → status Completed + sertifikat
- Badge monitoring berfungsi untuk HC melihat antrian penilaian
- Siap untuk Task 3: human verify end-to-end full question types flow (8 langkah verifikasi)

---
*Phase: 298-question-types*
*Completed: 2026-04-07*
