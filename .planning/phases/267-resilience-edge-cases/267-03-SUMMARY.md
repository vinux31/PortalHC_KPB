---
phase: 267-resilience-edge-cases
plan: "03"
subsystem: exam-timer-resilience
tags: [timer, auto-submit, visibilitychange, edge-case, gap-closure]
dependency_graph:
  requires: []
  provides: [EDGE-05, EDGE-07]
  affects: [Views/CMP/StartExam.cshtml, Views/CMP/ExamSummary.cshtml, Controllers/CMPController.cs]
tech_stack:
  added: []
  patterns: [visibilitychange-re-anchor, server-authoritative-timer-check]
key_files:
  created: []
  modified:
    - Views/CMP/StartExam.cshtml
    - Views/CMP/ExamSummary.cshtml
    - Controllers/CMPController.cs
decisions:
  - "visibilitychange listener memanggil updateTimer() langsung saat tab visible — timer tidak di-pause by design (server-timed exam anti-cheat)"
  - "isAutoSubmit hidden field di ExamSummary sekarang mengikuti timerExpired dari server, bukan hardcoded false"
  - "SubmitExam controller cek serverTimerExpired secara mandiri sebagai fallback agar tidak 100% bergantung client-side flag"
metrics:
  duration: "10 menit"
  completed_date: "2026-03-29"
  tasks_completed: 2
  tasks_total: 2
  files_changed: 3
---

# Phase 267 Plan 03: Timer Display Fix + Auto-Submit Blocker Fix Summary

Menutup 2 UAT gap: visibilitychange re-anchor agar display timer akurat saat tab resume, dan dual-layer fix (client + server) agar submit partial berhasil saat timer habis.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Fix timer display on tab resume + visibilitychange listener | 25f061f7 | Views/CMP/StartExam.cshtml |
| 2 | Fix blocker — isAutoSubmit=true saat timerExpired + server-side fallback | 5d35ff0c | Views/CMP/ExamSummary.cshtml, Controllers/CMPController.cs |

## What Was Built

### Task 1 — Timer Display Accuracy on Tab Resume (EDGE-05)

Ditambahkan `visibilitychange` event listener di StartExam.cshtml yang memanggil `updateTimer()` segera saat tab kembali aktif. Timer **tidak di-pause** (by design — server-timed exam untuk anti-cheat). Yang diperbaiki adalah akurasi **display** saat tab resume, karena browser throttle `setInterval` di background tabs menyebabkan display bisa tertinggal beberapa detik. Karena `updateTimer()` sudah menghitung dari wall clock (`Date.now() - timerStartWallClock`), memanggil ulang fungsi ini saat tab visible sudah cukup untuk menampilkan sisa waktu yang benar.

### Task 2 — Submit Saat Timer Habis dengan Jawaban Partial (EDGE-07)

Bug: hidden field `isAutoSubmit` di ExamSummary selalu bernilai `false` karena hardcoded, sehingga saat timer expired dan user sampai ke ExamSummary, controller menolak submit karena mendeteksi jawaban belum lengkap.

Fix dilakukan di dua lapis:
1. **Client-side (ExamSummary.cshtml):** Nilai hidden field `isAutoSubmit` kini mengikuti `timerExpired` — jika server menyatakan timer sudah habis, field otomatis bernilai `true`.
2. **Server-side (CMPController SubmitExam):** Ditambahkan `serverTimerExpired` check yang menghitung sendiri apakah waktu ujian sudah habis. Guard incomplete submission hanya aktif jika `!isAutoSubmit && !serverTimerExpired`, sehingga kedua kondisi harus false untuk memblokir submit.

## Deviations from Plan

Tidak ada — plan dieksekusi tepat sesuai instruksi.

## Verification

- Build: 0 error, 69 warning (semua warning pre-existing)
- `visibilitychange` ada di StartExam.cshtml baris 388
- `timerExpired ? "true" : "false"` ada di ExamSummary.cshtml baris 83
- `serverTimerExpired` ada di CMPController.cs baris 1371, 1376, 1379

## Known Stubs

Tidak ada stubs.
