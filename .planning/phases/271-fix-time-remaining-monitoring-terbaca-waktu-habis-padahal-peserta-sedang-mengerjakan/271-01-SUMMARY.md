---
phase: 271-fix-time-remaining-monitoring-terbaca-waktu-habis-padahal-peserta-sedang-mengerjakan
plan: 01
subsystem: api
tags: [timer, assessment, csharp, wall-clock, server-authoritative]

requires:
  - phase: 267-resilience-edge-cases
    provides: AssessmentSession dengan StartedAt, ElapsedSeconds, dan DurationMinutes fields

provides:
  - Server-authoritative timer resume: Math.Max(DB elapsed, wall-clock elapsed) di StartExam
  - Clamp validation di UpdateSessionProgress: monotonically increasing, tidak melebihi wall-clock

affects:
  - exam flow resume
  - timer accuracy
  - monitoring dashboard time remaining

tech-stack:
  added: []
  patterns:
    - "Server-authoritative timer: cross-check DateTime.UtcNow - StartedAt vs DB ElapsedSeconds"
    - "3-step clamp di UpdateSessionProgress: min(wallClock) → max(dbCurrent) → min(duration)"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs

key-decisions:
  - "Wall-clock cross-check hanya dilakukan saat !justStarted — saat first load, StartedAt baru di-set dan belum reliable"
  - "DB tidak diupdate saat StartExam load jika wall-clock lebih besar — cukup ViewBag, DB akan catch up di periodic save berikutnya"
  - "Clamp 2 (monotonically increasing) mencegah backward movement akibat sendBeacon terlambat tiba setelah periodic save dari sesi baru"

patterns-established:
  - "Pattern: Math.Max(elapsedSec, wallClockElapsed) sebagai server-authoritative timer saat resume"

requirements-completed:
  - TIMER-01
  - TIMER-02

duration: 15min
completed: 2026-03-28
---

# Phase 271 Plan 01: Fix Timer Ujian (Server-Authoritative) Summary

**Server cross-check wall-clock elapsed vs DB ElapsedSeconds di StartExam, plus 3-step monotonic clamp di UpdateSessionProgress — eliminates stale-DB timer bugs**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-03-28T00:00:00Z
- **Completed:** 2026-03-28
- **Tasks:** 1 (Task 2 adalah checkpoint human-verify)
- **Files modified:** 1

## Accomplishments

- StartExam: `elapsedSec = Math.Max(elapsedSec, wallClockElapsed)` — server tidak percaya DB stale, gunakan wall-clock sebagai lower bound
- StartExam: `elapsedSec = Math.Min(elapsedSec, durationSeconds)` — defensive clamp agar remaining tidak negatif
- UpdateSessionProgress: 3-step clamp sebelum ExecuteUpdateAsync — tidak melebihi wall-clock, tidak mundur, tidak melebihi durasi
- Build sukses: 0 errors, 69 warnings (semua CA1416 pre-existing)

## Task Commits

1. **Task 1: Server-authoritative timer di StartExam + clamp di UpdateSessionProgress** - `a686e826` (fix)

## Files Created/Modified

- `Controllers/CMPController.cs` — Fix 1: wall-clock cross-check di baris 907-917; Fix 2: 3-step clamp di UpdateSessionProgress baris 351-375

## Decisions Made

- Hanya fix di server-side (CMPController.cs) — tidak ada perubahan client-side JS sesuai D-09 dan RESEARCH Fix 3
- Guard `!justStarted && assessment.StartedAt.HasValue` mencegah NullReferenceException pada first-load (Pitfall 3 di RESEARCH)
- DB tidak di-update saat StartExam load walau wall-clock lebih besar — ViewBag saja sudah cukup, DB catch up di periodic save berikutnya

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None — kedua fix straightforward, build sukses pada percobaan pertama.

## User Setup Required

None - tidak ada konfigurasi eksternal yang diperlukan.

## Next Phase Readiness

- Kode sudah siap di-deploy ke server development untuk UAT manual (Task 2 checkpoint)
- User perlu verifikasi 3 skenario: BUG-01 (timer tidak habis mendadak), BUG-02 (timer tidak bertambah), EDGE-01 (expired modal saat resume)
- Setelah UAT pass, phase 271 selesai

---
*Phase: 271-fix-time-remaining-monitoring-terbaca-waktu-habis-padahal-peserta-sedang-mengerjakan*
*Completed: 2026-03-28*
