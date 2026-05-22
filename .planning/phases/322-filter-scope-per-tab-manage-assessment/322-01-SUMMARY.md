---
phase: 322-filter-scope-per-tab-manage-assessment
plan: 01
subsystem: ui
tags: [htmx, razor, aspnet-mvc, partial-view, filter, pagination]

# Dependency graph
requires:
  - phase: 311-htmx-driven-shell
    provides: HTMX 2.0 vendored + .htmx-tab-wrapper structure + shell HTMX trigger pattern
provides:
  - "Filter form HTMX inline trigger per partial (Tab 1 + Tab 2)"
  - "Pagination HTMX dengan filter state preservation (Tab 1, bonus fix)"
  - "DOM hooks #trainingHistoryTable + .training-history-row + data-worker untuk PLAN 02 JS function"
affects: [322-02, 322-03]

tech-stack:
  added: []
  patterns:
    - "Pattern A: HTMX inline trigger filter (hx-get → partial endpoint, hx-include='closest form')"
    - "Pattern B: HTMX pagination button + hx-include external form id (filter state preservation)"
    - "Pattern C: Cascade dropdown — onchange inline clear pre-HTMX request"
    - "Pattern D-prep: data-worker attribute parity (full pattern complete di PLAN 02)"

key-files:
  created: []
  modified:
    - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
    - Views/Admin/Shared/_TrainingRecordsTab.cshtml
    - Views/Admin/Shared/_HistoryTab.cshtml

key-decisions:
  - "Form id per-tab unique: filterFormAssessment + filterFormTraining (anti-pattern: jangan reuse shell #filter-form)"
  - "hx-target='closest .htmx-tab-wrapper' generic per-partial (anti shell-specific #pane-X selector)"
  - "hx-include='closest form' isolasi state per-partial; pagination tetap pakai #filterFormAssessment external ref (cross-element)"
  - "Pagination <a href> → <button type='button'> (anti-pattern: jangan <a> hx-get karena native nav race)"
  - "Reset button: clear field via onclick + HTMX trigger TANPA hx-include (empty params = server default)"

patterns-established:
  - "HTMX inline trigger filter form pattern (3-field Tab 1, 5-field Tab 2)"
  - "Pagination preserve filter state via hx-include external form id"
  - "Cascade dropdown pre-HTMX value clear via onchange (DOM event order: native → HTMX)"
  - "Sub-tab client-side row filter DOM hook (id table + class row + data-attr) — JS function deferred ke shell PLAN 02"

requirements-completed: []

# Metrics
duration: ~15min
completed: 2026-05-22
---

# Phase 322 Plan 01: Partial Views Filter HTMX Refactor

**3 partial view filter form converted dari GET submit → HTMX inline trigger ke partial endpoint masing-masing. Bonus pagination preserve filter state.**

## Performance

- **Duration:** ~15 menit
- **Tasks:** 4/4 completed
- **Files modified:** 3
- **Build:** PASS per task (no Razor compile error)

## Accomplishments
- Tab Assessment Groups: filter form HTMX inline (search + kategori + status, 3-field) + reset button HTMX + clear search button
- Tab Assessment Groups: pagination 5-button (first/prev/numbered/next/last) HTMX dengan `hx-include="#filterFormAssessment"` — bonus fix existing bug (pagination cuma kirim `search`, miss `category`+`statusFilter`)
- Tab Input Records: filter form HTMX inline 5-field native (Bagian + Kategori Training + Unit + Status + Cari Nama/Nopeg) + cascade Bagian→Unit preserved via `onchange` pre-HTMX clear
- Sub-tab Riwayat Training: filter input + `id="trainingHistoryTable"` + `class="training-history-row"` + `data-worker` attribute (JS function `filterTrainingRows()` deferred ke PLAN 02 Task 5d)

## Task Commits

Each task committed atomically:

1. **Task 1: HTMX inline filter Tab Assessment Groups** — `be22e026` (feat)
2. **Task 2: HTMX pagination Tab Assessment Groups + preserve filter state** — `e4c19c90` (feat)
3. **Task 3: HTMX inline filter Tab Input Records 5-field native** — `da8ef9ca` (feat)
4. **Task 4: Client-side filter Riwayat Training sub-tab parity** — `03981fda` (feat)

## Files Modified

- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` — Tab 1 filter form refactor + pagination HTMX (Task 1+2, +128/-68 net)
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — Tab 2 5-field filter HTMX + cascade preserved (Task 3, +41/-11)
- `Views/Admin/Shared/_HistoryTab.cshtml` — Sub-tab Training filter input + DOM hooks (Task 4, +12/-2)

## Known Incomplete State (resolved by PLAN 02)

- **Double filter Tab 1 still visible** — shell `<form id="filter-form">` exists, dihapus di PLAN 02 sub-edit 5a
- **Sub-tab Riwayat Training filter input no-effect** — `filterTrainingRows()` JS function ditambah PLAN 02 sub-edit 5d (DOM hooks Task 4 sudah ready sebagai target)
- **Wrapper `hx-include="#filter-form"` dangling reference** — diperbaiki PLAN 02 sub-edit 5g via `hx-vals` D-21 Strategy D Hybrid

## Verification

- `dotnet build` PASS per commit (22 pre-existing warnings only, 0 errors)
- Razor compile clean
- Manual browser UAT deferred ke PLAN 03 Task 7 (post PLAN 02 shell cleanup)

## Next Step

PLAN 02 (Wave 2, sequential strict) — Shell View Cleanup `ManageAssessment.cshtml` 5 sub-edit + Controller `AssessmentAdminController.ManageAssessment` action body cleanup. PLAN 02 selesai = filter scope per-tab fully working, pre-condition PLAN 03 UAT.
