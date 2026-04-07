---
phase: 302-accessibility-wcag-quick-wins
plan: "02"
subsystem: assessment-extra-time
tags: [accessibility, extra-time, signalr, wcag, monitoring]
requirements: [A11Y-05]

dependency_graph:
  requires: []
  provides: [extra-time-feature]
  affects: [AssessmentSession, AssessmentAdminController, CMPController, AssessmentHub, AssessmentMonitoringDetail, StartExam]

tech_stack:
  added: []
  patterns:
    - SignalR group broadcast ke batch peserta ujian
    - AJAX POST dengan CSRF token via fetch API
    - Wall-clock anchor timer update untuk real-time timer adjustment

key_files:
  created:
    - Migrations/20260407110442_AddExtraTimeMinutesToAssessmentSession.cs
    - Migrations/20260407110442_AddExtraTimeMinutesToAssessmentSession.Designer.cs
  modified:
    - Models/AssessmentSession.cs
    - Controllers/AssessmentAdminController.cs
    - Controllers/CMPController.cs
    - Hubs/AssessmentHub.cs
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - Views/CMP/StartExam.cshtml

decisions:
  - "assessmentId parameter di AddExtraTime endpoint merujuk ke RepresentativeId (satu session Id) — endpoint mencari AccessToken dari session tersebut lalu update semua session InProgress dengan AccessToken yang sama"
  - "timerStartRemaining + timerStartWallClock keduanya di-reset saat ExtraTimeAdded diterima untuk menjaga wall-clock anchor tetap benar"
  - "ExtraTimeMinutes diakumulasi (bukan di-replace) sehingga HC dapat menambah waktu berkali-kali"

metrics:
  duration: "25 menit"
  completed_date: "2026-04-07"
  tasks_completed: 2
  files_modified: 6
---

# Phase 302 Plan 02: Extra Time Accessibility Summary

**One-liner:** Full-stack extra time feature — DB column, AJAX endpoint, SignalR broadcast, dan real-time timer update untuk akomodasi peserta ujian berkebutuhan khusus.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Model + DB migration + controller endpoint + SignalR broadcast + server-side timer update | 5223ce55 | AssessmentSession.cs, AssessmentAdminController.cs, CMPController.cs, AssessmentHub.cs, Migrations/ |
| 2 | Modal extra time di monitoring + SignalR handler di StartExam | a95e7c09 | AssessmentMonitoringDetail.cshtml, StartExam.cshtml, AssessmentAdminController.cs |

## What Was Built

### Database
- Kolom `ExtraTimeMinutes` (nullable int) ditambahkan ke tabel `AssessmentSessions`
- Migration dijalankan: `20260407110442_AddExtraTimeMinutesToAssessmentSession`

### Backend
- Endpoint `POST /Admin/AddExtraTime(assessmentId, minutes)` di `AssessmentAdminController`:
  - Validasi: minutes harus antara 5-120, kelipatan 5
  - Mencari representative session via assessmentId, lalu update SEMUA session InProgress dengan AccessToken yang sama
  - Mengakumulasi ExtraTimeMinutes (bukan replace)
  - Broadcast SignalR `ExtraTimeAdded` ke group `batch-{accessToken}` dengan nilai `minutes * 60` detik
- Server-side timer check diperbarui di 4 lokasi di `CMPController.cs`:
  - StartExam: `durationSeconds = (DurationMinutes + ExtraTimeMinutes) * 60`
  - ExamSummary timerExpired check
  - SubmitExam serverTimerExpired check
  - AkhiriUjian grace period check
- `AssessmentHub.SaveMultipleAnswer` diperbarui untuk memperhitungkan `ExtraTimeMinutes`

### Frontend
- Tombol "Tambah Waktu" (btn-warning) di header token AssessmentMonitoringDetail
- Modal `#extraTimeModal`: dropdown 5/10/15/20/25/30/45/60/90/120 menit, default 15
- AJAX submit dengan CSRF token, feedback alert inline (success/error) tanpa reload
- SignalR handler `ExtraTimeAdded` di StartExam: update `timerStartRemaining` + `timerStartWallClock` secara atomik agar timer bertambah real-time tanpa refresh

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed batch query di AddExtraTime endpoint**
- **Found during:** Task 2 — review endpoint saat menyiapkan view
- **Issue:** Query awal `Where(s => s.Id == assessmentId)` hanya menemukan satu session (representative), bukan semua peserta dalam batch. Semua peserta dalam satu batch memiliki AccessToken yang sama.
- **Fix:** Dua-langkah query: cari representative session untuk mendapat AccessToken, lalu cari semua session InProgress dengan AccessToken yang sama
- **Files modified:** Controllers/AssessmentAdminController.cs
- **Commit:** a95e7c09

## Self-Check

- [x] Models/AssessmentSession.cs mengandung `ExtraTimeMinutes`
- [x] Controllers/AssessmentAdminController.cs mengandung `AddExtraTime` method
- [x] Controllers/CMPController.cs mengandung `ExtraTimeMinutes` di 4 lokasi timer
- [x] Hubs/AssessmentHub.cs mengandung `ExtraTimeMinutes` di timer check
- [x] Views/Admin/AssessmentMonitoringDetail.cshtml mengandung `extraTimeModal` dan `addExtraTime`
- [x] Views/CMP/StartExam.cshtml mengandung `ExtraTimeAdded` handler
- [x] dotnet build: 0 errors, 74 warnings (pre-existing)
- [x] DB migration applied sukses

## Self-Check: PASSED
