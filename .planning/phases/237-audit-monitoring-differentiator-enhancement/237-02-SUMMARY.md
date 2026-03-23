---
phase: 237-audit-monitoring-differentiator-enhancement
plan: "02"
subsystem: ui
tags: [chart.js, asp.net-core, cdp, proton-coaching, dashboard]

requires:
  - phase: 237-01
    provides: filter bug fixes dan audit trail yang dipakai sebagai fondasi dashboard accuracy

provides:
  - Dashboard stats akurat per filter category/track aktif (MON-01)
  - Bottleneck horizontal bar chart top-10 deliverable pending >30 hari (DIFF-03)
  - Kolom Coachee Aktif dengan badge warna di CoachCoacheeMapping (DIFF-01)

affects:
  - 237-03 (batch approval DIFF-02 juga ada di CoachingProton)

tech-stack:
  added: []
  patterns:
    - "Bottleneck chart: allProgresses sudah-difilter → BottleneckLabels/Values di ViewModel → Chart.js indexAxis:'y'"
    - "Re-scope allProgresses setelah category/track filter untuk konsistensi stat cards"

key-files:
  created: []
  modified:
    - Models/CDPDashboardViewModel.cs
    - Controllers/CDPController.cs
    - Views/CDP/Shared/_CoachingProtonContentPartial.cshtml
    - Views/Admin/CoachCoacheeMapping.cshtml

key-decisions:
  - "MON-01 fix: allProgresses di-filter ulang setelah assignments difilter per category/track — bukan query baru"
  - "DIFF-03 bottleneck: ambil dari allProgresses yang sudah difilter (Pitfall 3 avoidance), bukan query terpisah"
  - "DIFF-01: badge warna merah (>=8), kuning (>=5), biru (default) sebagai workload threshold di tabel mapping"

patterns-established:
  - "Bottleneck data flow: controller populate BottleneckLabels/Values → partial view render Chart.js horizontal bar"

requirements-completed: [MON-01, DIFF-01, DIFF-03]

duration: 20min
completed: 2026-03-23
---

# Phase 237 Plan 02: Dashboard Audit + Bottleneck Chart + Workload Indicator Summary

**Dashboard stats diaudit untuk konsistensi filter, bottleneck horizontal bar chart ditambah untuk visibility, dan kolom Coachee Aktif dengan badge warna beban ditambah di mapping page.**

## Performance

- **Duration:** 20 min
- **Started:** 2026-03-23T05:20:00Z
- **Completed:** 2026-03-23T05:40:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- MON-01 bug fix: ketika filter category/track aktif, `allProgresses` sekarang di-re-scope ke filtered assignments sebelum stats dihitung — memastikan PendingSpvApprovals, PendingHCReviews, dan chart data konsisten dengan filter
- DIFF-03: tambah `BottleneckLabels`/`BottleneckValues` ke `ProtonProgressSubModel`, populate top-10 deliverable pending >30 hari di `BuildProtonProgressSubModelAsync`, render horizontal bar chart di `_CoachingProtonContentPartial.cshtml`
- DIFF-01: tambah kolom "Coachee Aktif" di header tabel CoachCoacheeMapping dengan badge berwarna (merah >= 8, kuning >= 5, biru default)

## Task Commits

1. **Task 1: Audit dashboard stats + bottleneck chart (MON-01, DIFF-03)** - `e280ccb6` (feat)
2. **Task 2: Workload indicator di CoachCoacheeMapping (DIFF-01)** - `a3bed041` (feat)

## Files Created/Modified

- `Models/CDPDashboardViewModel.cs` — tambah `BottleneckLabels` dan `BottleneckValues` ke `ProtonProgressSubModel`
- `Controllers/CDPController.cs` — re-scope allProgresses setelah filter, populate bottleneck data
- `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml` — render bottleneck chart card + Chart.js horizontal bar
- `Views/Admin/CoachCoacheeMapping.cshtml` — kolom header "Coachee Aktif", badge warna per beban, kolom coachee rows

## Decisions Made

- MON-01 fix: gunakan filtered assignments ID set untuk re-scope `allProgresses` — tidak perlu query baru ke DB
- DIFF-03: bottleneck query dari `allProgresses` yang sudah di-scope (per Pitfall 3 di RESEARCH.md) — data bottleneck konsisten dengan filter aktif
- DIFF-01: badge threshold >= 8 merah, >= 5 kuning, default biru — mengikuti bootstrap badge colors

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 02 selesai — MON-01, DIFF-01, DIFF-03 diimplementasikan
- Plan 03 (DIFF-02 batch HC approval) dapat dilanjutkan
- Build bersih tanpa error

---
*Phase: 237-audit-monitoring-differentiator-enhancement*
*Completed: 2026-03-23*
