---
phase: 235-audit-execution-flow
plan: 02
subsystem: api
tags: [cdp, coaching, deliverable, race-condition, notification, approval]

# Dependency graph
requires:
  - phase: 235-audit-execution-flow
    provides: "235-01 StatusHistory, EvidencePathHistory, UploadEvidence rollback"
provides:
  - "ApproveDeliverable first-write-wins race condition guard (D-10)"
  - "HCReviewDeliverable notifikasi ke Coach (D-14, EXEC-04)"
  - "UploadEvidence notifikasi resubmit khusus COACH_EVIDENCE_RESUBMITTED (D-18)"
  - "CreateHCNotificationAsync dedup exact message match per-HC-user (D-14)"
affects: [235-03-audit-completion-flow, Phase 236-monitoring]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Race condition guard: reload AsNoTracking sebelum write untuk first-write-wins"
    - "Per-recipient dedup: AnyAsync per HC user bukan global Contains check"
    - "Conditional notification type: wasRejected flag menentukan RESUBMITTED vs SUBMITTED"

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs

key-decisions:
  - "Admin override di ApproveDeliverable adalah by-design via section check skip — tidak ada dedicated AdminOverride action, acceptable (D-11)"
  - "Race guard ditempatkan SETELAH per-role field set tapi SEBELUM overall Status assignment untuk first-write-wins semantics"
  - "Dedup CreateHCNotificationAsync diubah ke per-recipient check (loop inside) agar setiap HC user bisa deduplicated secara independen"

patterns-established:
  - "Reload AsNoTracking pattern untuk race condition guard sebelum SaveChangesAsync"
  - "wasRejected flag sudah ada di UploadEvidence — gunakan untuk conditional notification tanpa perubahan signature NotifyReviewersAsync"

requirements-completed: [EXEC-02, EXEC-04]

# Metrics
duration: 15min
completed: 2026-03-22
---

# Phase 235 Plan 02: Audit Execution Flow — Race Condition & Notification Gaps Summary

**Race condition guard first-write-wins di ApproveDeliverable, HC Review notifikasi ke Coach, resubmit notifikasi khusus, dan dedup exact-match per recipient di CreateHCNotificationAsync**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-22T14:50:00Z
- **Completed:** 2026-03-22T15:05:00Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- ApproveDeliverable: reload AsNoTracking freshStatus setelah per-role field set, re-check stillCanApprove, redirect "sudah diproses oleh approver lain" jika race kalah
- HCReviewDeliverable: tambah try-catch blok kirim HC_REVIEW_COMPLETE ke Coach via CoachCoacheeMappings lookup
- UploadEvidence: conditional COACH_EVIDENCE_RESUBMITTED jika wasRejected, dikirim ke section reviewers (RoleLevel=4)
- CreateHCNotificationAsync: dedup diubah dari global Contains(coacheeId) menjadi per-recipient exact Message == expectedMessage

## Task Commits

1. **Task 1: Race condition guard di ApproveDeliverable + audit Admin override flow** - `f8cf357` (fix)
2. **Task 2: Fix notifikasi gaps — HC Review notif, resubmit notif, dedup fix** - `87abaa3` (fix)

## Files Created/Modified
- `Controllers/CDPController.cs` - Race guard, HC Review notif, resubmit notif, dedup fix

## Decisions Made
- Admin override = section bypass only (L786 check), tidak ada dedicated action — by-design, acceptable (D-11)
- Race guard ditempatkan setelah per-role set agar SrSpv/SH approval fields tetap tercatat meski overall Status sudah di-set concurrent
- Dedup CreateHCNotificationAsync direfactor ke per-recipient loop agar tiap HC user dapat dedup-check independen

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] ProtonDeliverable.Name -> NamaDeliverable**
- **Found during:** Task 2 (HC Review notifikasi)
- **Issue:** Plan menggunakan `ProtonDeliverable?.Name` tapi property sebenarnya `NamaDeliverable` — compile error CS1061
- **Fix:** Ganti `.Name` menjadi `.NamaDeliverable` di dua lokasi (HCReviewDeliverable dan UploadEvidence resubmit block)
- **Files modified:** Controllers/CDPController.cs
- **Verification:** `dotnet build` berhasil tanpa error
- **Committed in:** 87abaa3 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Fix wajib — compile error tanpa ini. Tidak ada scope creep.

## Issues Encountered
- Parallel agent (235-01) sudah commit perubahan besar ke CDPController.cs tepat sebelum Task 2 dimulai — perubahan Task 2 berhasil di-apply di atas perubahan 235-01 tanpa konflik

## User Setup Required
None - tidak ada konfigurasi external service yang diperlukan.

## Next Phase Readiness
- Race condition guard aktif di ApproveDeliverable
- Semua notification gaps EXEC-04 tertutup
- Siap untuk 235-03: audit completion flow (sequential lock, all-approved state, ExportProgressExcel auth)

---
*Phase: 235-audit-execution-flow*
*Completed: 2026-03-22*

## Self-Check: PASSED
- Controllers/CDPController.cs: FOUND (sudah dimodifikasi dengan semua perubahan)
- Commit f8cf357: FOUND (race guard)
- Commit 87abaa3: FOUND (notification gaps)
- Build: PASSED (0 errors)
