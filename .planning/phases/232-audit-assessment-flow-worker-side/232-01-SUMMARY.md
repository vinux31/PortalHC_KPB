---
phase: 232-audit-assessment-flow-worker-side
plan: 01
subsystem: ui
tags: [signalr, bootstrap, javascript, assessment, exam, timer]

requires:
  - phase: 232-context
    provides: "Research gaps D-01 s/d D-19, audit findings per area"
  - phase: 231-audit-assessment-management-monitoring
    provides: "AdminController SignalR patterns (Clients.User() untuk per-worker push)"

provides:
  - "Assessment list dengan badge warna benar: Open=hijau, Upcoming=biru, Expired=merah"
  - "Token entry modal dengan auto-focus, Enter key, clear error, auto-uppercase, pesan Bahasa Indonesia"
  - "Timer exam menggunakan wall clock anchor (Date.now), tidak drift saat tab minimize"
  - "Timer expiry warning modal (timeUpWarningModal) sebelum auto-submit, submitted guard flag"
  - "assessmentHubStartPromise exposed di assessment-hub.js"
  - "networkStatusBadge persisten di exam header (Tersimpan/Menyimpan/Offline)"
  - "pendingAnswers auto-retry queue saat SignalR reconnect"

affects: [232-02, assessment-flow-worker-side]

tech-stack:
  added: []
  patterns:
    - "Wall clock timer: timerStartWallClock = Date.now() sebagai anchor, bukan setInterval decrement counter"
    - "assessmentHubStartPromise: async startHub() return value exposed ke window untuk consumer page"
    - "submitted guard flag: var submitted = false sebelum submit, set true di handler untuk mencegah double submit"
    - "pendingAnswers queue: failed saves di-queue, di-retry di onreconnected handler"

key-files:
  created: []
  modified:
    - "Views/CMP/Assessment.cshtml"
    - "Views/CMP/StartExam.cshtml"
    - "wwwroot/js/assessment-hub.js"
    - "Controllers/CMPController.cs"

key-decisions:
  - "SignalR worker push menggunakan Clients.User(userId) di AdminController — tidak perlu JoinWorkerSession method di hub karena user-targeting sudah built-in di SignalR"
  - "assessmentHubStartPromise menggantikan setTimeout(2000) fallback untuk set badge Live — lebih reliable karena await actual hub connect"
  - "pendingAnswers queue di StartExam.cshtml, bukan di assessment-hub.js — karena saveAnswerAsync ada di scope StartExam"

patterns-established:
  - "Pattern: Wall clock timer anchor dengan Date.now() untuk akurasi tanpa drift"
  - "Pattern: submitted guard flag untuk mencegah double submit di timer expiry + modal"

requirements-completed: [AFLW-01, AFLW-02, AFLW-04]

duration: 18min
completed: 2026-03-22
---

# Phase 232 Plan 01: Audit Assessment Flow Worker Side Summary

**Timer wall-clock drift fix, timeUpWarningModal, token entry UX (auto-focus/Enter/error), badge warna assessment list, networkStatusBadge persisten, pendingAnswers auto-retry, assessmentHubStartPromise**

## Performance

- **Duration:** ~18 min
- **Started:** 2026-03-22T09:00:00Z
- **Completed:** 2026-03-22T09:12:10Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- Assessment list badge warna diperbaiki: Upcoming=biru (bg-primary), Expired=merah (bg-danger berdasarkan ExamWindowCloseDate), empty state Bahasa Indonesia, IsPassed null dihandle sebagai "Belum Dinilai"
- Token entry modal dilengkapi auto-focus (shown.bs.modal), Enter key submit, clear error + auto-uppercase saat input, semua pesan error Bahasa Indonesia
- Timer exam refactor ke wall clock anchor (Date.now) — tidak drift setelah tab minimize; timer expiry sekarang tampilkan timeUpWarningModal dengan submitted guard flag mencegah double submit
- assessmentHubStartPromise exposed dari assessment-hub.js; StartExam.cshtml menggunakan .then() bukan setTimeout(2000) fallback
- networkStatusBadge di exam header menampilkan Tersimpan/Menyimpan/Offline secara persisten; pendingAnswers queue + onreconnected handler untuk auto-retry jawaban yang gagal tersimpan

## Task Commits

1. **Task 1: Audit dan fix Assessment List + Token Entry UX** - `99b60a8` (feat)
2. **Task 2: Audit dan fix Timer + SignalR Worker Push + Session Resume + Network Indicator** - `4acf712` (feat)

## Files Created/Modified

- `Views/CMP/Assessment.cshtml` - Badge warna statusBadgeClass (Upcoming=bg-primary, Expired=bg-danger), empty state Bahasa Indonesia, IsPassed null handler, token modal UX (auto-focus/Enter/clear error/uppercase), pesan error Bahasa Indonesia
- `Views/CMP/StartExam.cshtml` - Timer wall clock anchor, timeUpWarningModal HTML + handler, submitted guard flag, networkStatusBadge element + helpers, pendingAnswers queue, auto-retry di onreconnected, assessmentHubStartPromise.then() untuk badge Live
- `wwwroot/js/assessment-hub.js` - Expose window.assessmentHubStartPromise = startHub()
- `Controllers/CMPController.cs` - VerifyToken error message Bahasa Indonesia

## Decisions Made

- **SignalR grup untuk per-worker push:** AdminController `ResetAssessment` menggunakan `_hubContext.Clients.User(userId).SendAsync("sessionReset")` dan `AkhiriUjian` menggunakan `Clients.User(userId).SendAsync("examClosed")` — user-targeting built-in, tidak perlu `JoinWorkerSession` method di hub. Bulk close menggunakan `Clients.Group($"batch-{batchKey}")` yang sudah di-join via assessment-hub.js JoinBatch.
- **assessmentHubStartPromise:** Menggantikan setTimeout(2000) fallback yang flaky. async startHub() sudah return Promise secara native.
- **pendingAnswers di StartExam scope:** saveAnswerAsync ada di StartExam.cshtml, sehingga queue lebih tepat di scope yang sama. onreconnected handler ditambahkan di StartExam, bukan di assessment-hub.js.

## Deviations from Plan

None - plan dieksekusi sesuai rencana. Satu clarification dari research: JoinWorkerSession tidak diperlukan karena AdminController sudah menggunakan Clients.User() bukan group-based push untuk per-worker signals.

## Issues Encountered

None.

## Known Stubs

None — semua data dari server, tidak ada hardcoded stubs yang menghalangi fungsionalitas.

## Next Phase Readiness

- Plan 01 selesai. Plan 02 (fase berikutnya di Phase 232) dapat melanjutkan audit area scoring + results page.
- assessmentHubStartPromise tersedia jika plan 02 perlu menggunakannya.
- Timer wall-clock fix sudah solid — plan 02 tidak perlu revisit.

---
*Phase: 232-audit-assessment-flow-worker-side*
*Completed: 2026-03-22*
