---
phase: 356-audit-fix-assign-coach-coachee
plan: 03
subsystem: coaching-notification
tags: [coaching, notification, performance, audit-fix]
requires: [356-02]
provides: [reassign-notifications, batched-progression-warning]
affects: [Controllers/CoachMappingController.cs]
tech-stack:
  added: []
  patterns: [warn-only-notification, batch-query]
key-files:
  created: []
  modified:
    - Controllers/CoachMappingController.cs
key-decisions:
  - "AF-5 reassign notif 3 recipient warn-only (gagal kirim tak menggagalkan reassign), EVENT_TYPE COACH_REASSIGNED"
  - "AF-7 batch query (3 pre-load) ganti N+1, output incompleteCoachees identik (zero behavior change)"
requirements-completed: [AF-5, AF-7]
duration: ~12 min
completed: 2026-06-09
---

# Phase 356 Plan 03: AF-5 Reassign Notif + AF-7 Batch Query Summary

`ApproveReassignSuggestion` kini mengirim 3 notifikasi `COACH_REASSIGNED` (coach lama dilepas / coach baru ditunjuk / coachee dipindah) warn-only. Loop progression-warning di `CoachCoacheeMappingAssign` direfaktor dari N+1 (~4 query/coachee) menjadi 3 batch pre-load + evaluasi in-memory, dengan output `incompleteCoachees` identik (zero behavior change).

## Tasks
- **Task 1** (`503beffa`): AF-5 — 3 SendAsync warn-only, microcopy BI dikunci UI-SPEC.
- **Task 2** (`d44fc92c`): AF-7 — batch pre-load (hasForRequestedTrack/prevByCoachee/progressByAssignment), 3 cabang direproduksi persis.

## Verification
- `dotnet build HcPortal.csproj` → 0 error.
- `dotnet test` → **135/135 passed** (131 baseline + 4 AF-1), 0 regresi (10s).
- grep: `COACH_REASSIGNED` ×3; 3 judul BI present; 0 query async di dalam `foreach (var coacheeId in coacheeIdsForWarning)`; warning message `belum menyelesaikan {prevTrack.DisplayName}` verbatim.

## Deviations from Plan
None - plan executed exactly as written. (Edit AF-5 awalnya match 2 lokasi `SaveChangesAsync()+return Json`; ditambah konteks audit-log block untuk unik — bukan deviasi substantif.)

## Issues Encountered
None.

## Next Phase Readiness
Ready for 356-04 (AF-2 view + D-06 badge). AF-5 notif + AF-7 parity diverifikasi Plan 05 (UAT). Full suite hijau.
