---
phase: 10-unified-training-records
plan: 02
subsystem: ui
tags: [asp.net-core, razor, bootstrap, linq]

# Dependency graph
requires:
  - phase: 10-unified-training-records (plan 01)
    provides: UnifiedTrainingRecord ViewModel, GetUnifiedRecords() helper, WorkerTrainingStatus.CompletionDisplayText
provides:
  - Records.cshtml: unified single-table view using List<UnifiedTrainingRecord>
  - RecordsWorkerList.cshtml: completion count column replacing SUDAH/BELUM badge
  - WorkerDetail.cshtml: unified 10-column table using List<UnifiedTrainingRecord>
affects:
  - 11-assessment-filter (views now ready to receive filtered unified data)
  - 12-dashboard-consolidation (Records view pattern established for other unified views)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Unified table pattern: 9 always-visible columns with em-dash for non-applicable cells
    - Type badge pattern: blue rounded-pill for Assessment Online, green for Training Manual
    - IsExpired-only expiry display: no lookahead window, only past-date danger badge

key-files:
  created: []
  modified:
    - Views/CMP/Records.cshtml
    - Views/CMP/RecordsWorkerList.cshtml
    - Views/CMP/WorkerDetail.cshtml

key-decisions:
  - "Empty state is plain text only — no icon, no call to action, exactly 'Belum ada riwayat pelatihan'"
  - "WorkerDetail stat cards: 3 cards (Total Records, Selesai, Pending) — Expiring Soon removed per phase plan"
  - "Filter bar in Records.cshtml: search + year only (status filter and tab nav removed)"

patterns-established:
  - "Unified 9-column table: Tanggal | Tipe | Nama/Judul | Score | Pass/Fail | Penyelenggara | Tipe Sertifikat | Berlaku Sampai | Status"
  - "Non-applicable cells always show em dash (—) not blank or null"
  - "Client-side filterTable() on data-title + data-year attributes; no server-side filter state for personal records"

# Metrics
duration: 5min
completed: 2026-02-18
---

# Phase 10 Plan 02: Unified Training Records — Razor Views Summary

**Three CMP Razor views rewritten to consume List<UnifiedTrainingRecord>: Records.cshtml becomes a single merged 9-column chronological table with type badges; RecordsWorkerList shows CompletionDisplayText; WorkerDetail gets a 10-column unified table with 3 stat cards**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-18T20:00:35Z
- **Completed:** 2026-02-18T20:05:06Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- Replaced Records.cshtml tab-based layout (5 tabs, 6-column table per tab) with a single 9-column chronological table consuming `List<UnifiedTrainingRecord>` — Assessment Online rows show Score/Pass/Fail; Training Manual rows show Penyelenggara/Tipe Sertifikat/Berlaku Sampai; non-applicable cells render em dash
- Updated RecordsWorkerList.cshtml Status column from SUDAH/BELUM badges (driven by CompletionPercentage) to `worker.CompletionDisplayText` text (e.g. "5 completed (3 assessments + 2 trainings)")
- Rewrote WorkerDetail.cshtml to use `List<UnifiedTrainingRecord>`, adding the full 10-column unified table, reducing stat cards from 4 to 3 (dropped Expiring Soon), and simplifying filter to title-only search

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite Records.cshtml as unified single table** - `932deb8` (feat)
2. **Task 2: Update RecordsWorkerList.cshtml and WorkerDetail.cshtml** - `2954853` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `Views/CMP/Records.cshtml` — Rewritten: @model changed to `List<UnifiedTrainingRecord>`, 4-card row dropped to 3, tab nav and all 5 tab panes removed, certificate modal removed, single 9-column table added, filterTable() updated for title+year only
- `Views/CMP/RecordsWorkerList.cshtml` — Stat block variables removed (avgCompletionRate, totalExpiring, workersWithExpiring); Status column header renamed to Completion; SUDAH/BELUM badge cell replaced with CompletionDisplayText
- `Views/CMP/WorkerDetail.cshtml` — @model changed to `List<UnifiedTrainingRecord>`; 4 stat cards → 3 (Total Records, Selesai, Pending); category and status filter dropdowns removed; 8-column table replaced with 10-column unified table; filterRecords() simplified to title-only; exportWorkerRecords() updated with new column headers and cell indices

## Decisions Made

- Empty state in Records.cshtml is plain text only ("Belum ada riwayat pelatihan") — no icon, no call to action, as specified in plan truths
- WorkerDetail retains the Export to Excel button; exportWorkerRecords() CSV updated to match new 10-column schema
- Expiring Soon stat card removed from both Records.cshtml and WorkerDetail.cshtml — phase decision is IsExpired-only (past-date), no lookahead

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None — build passed with 0 errors after each task. Pre-existing 36 CS8602 nullable warnings in CDPController.cs (unrelated to this plan) remained unchanged.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 10 is now fully complete: data layer (Plan 01) + view layer (Plan 02) both shipped
- Phase 11 (Assessment Filter) can proceed — history destination is live, filter source can now safely be added
- Phase 11 sequencing gate satisfied: Records view is unified before any filter-by-type logic is introduced

## Self-Check: PASSED

- FOUND: Views/CMP/Records.cshtml — line 1: `@model List<HcPortal.Models.UnifiedTrainingRecord>`
- FOUND: Views/CMP/WorkerDetail.cshtml — line 1: `@model List<HcPortal.Models.UnifiedTrainingRecord>`
- FOUND: Views/CMP/RecordsWorkerList.cshtml — contains `CompletionDisplayText`
- FOUND: commit 932deb8 (feat(10-02): rewrite Records.cshtml as unified single table)
- FOUND: commit 2954853 (feat(10-02): update RecordsWorkerList and WorkerDetail for unified model)
- Build: 0 errors (36 pre-existing warnings in CDPController.cs)
- No `nav-pills` or `tab-pane` in Records.cshtml: confirmed 0 matches
- No `GetPersonalTrainingRecords` in any of the 3 views: confirmed 0 matches

---
*Phase: 10-unified-training-records*
*Completed: 2026-02-18*
