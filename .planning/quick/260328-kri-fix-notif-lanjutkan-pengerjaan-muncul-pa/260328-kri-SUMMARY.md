---
phase: quick
plan: 260328-kri
subsystem: CMPController / StartExam
tags: [bug-fix, exam-flow, resume-notification]
key-files:
  modified:
    - Controllers/CMPController.cs
decisions:
  - "Gunakan justStarted flag (di-capture sebelum StartedAt di-set) sebagai basis isResume, bukan assessment.StartedAt != null"
metrics:
  duration: "< 5 menit"
  completed: "2026-03-28"
  tasks: 1
  files: 1
---

# Quick Task 260328-kri: Fix Notif Lanjutkan Pengerjaan Muncul Saat First Visit

**One-liner:** Fix isResume = !justStarted agar notifikasi "lanjutkan pengerjaan" tidak muncul saat worker pertama kali masuk assessment baru.

## Summary

Bug: Di `StartExam`, variabel `justStarted` di-capture sebelum `StartedAt` di-set (line 782), tapi pengecekan `isResume` menggunakan `assessment.StartedAt != null` (line 924) — yang sudah pasti `!= null` karena StartedAt sudah di-set di block lines 783-788.

Fix: Ganti `bool isResume = assessment.StartedAt != null` menjadi `bool isResume = !justStarted`.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Fix isResume logic di StartExam | 9cdcb1f2 | Controllers/CMPController.cs |

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check: PASSED

- File modified: Controllers/CMPController.cs (FOUND)
- Commit 9cdcb1f2 (FOUND)
- grep "bool isResume = !justStarted" → line 924 (FOUND)
- Satu definisi isResume, tidak ada definisi lain
