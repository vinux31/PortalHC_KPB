---
phase: 349-manageassessment-monitoring-low-polish
plan: 01
subsystem: ManageAssessment Tab3 History (view polish)
tags: [i18n, a11y, empty-state, skeleton, razor]
requires: []
provides:
  - "Tab3 History: label NIP konsisten + drill-down bukan nested-interactive"
  - "Tab3 client-filter feedback: pesan 0-match (aria-live) + counter Menampilkan X dari Y"
  - "Skeleton Tab2/Tab3 kolom-match (zero layout-shift)"
affects:
  - Views/Admin/Shared/_HistoryTab.cshtml
  - Views/Admin/ManageAssessment.cshtml
tech-stack:
  added: []
  patterns: [bootstrap-placeholder-glow, aria-live-polite, client-filter-dom-toggle]
key-files:
  created: []
  modified:
    - Views/Admin/Shared/_HistoryTab.cshtml
    - Views/Admin/ManageAssessment.cshtml
key-decisions:
  - "MAP-02 placeholder Training pakai 'Cari nama pekerja / NIP...' (parity Assessment sibling di file sama) — bukan literal UI-SPEC 'Cari nama atau NIP...'; lebih konsisten in-file, acceptance (NIP/no-Nopeg) tetap terpenuhi"
  - "MAP-04 drop role/tabindex/aria-label/Html.Raw + JS row-nav + CSS dead cil03-row-link; tombol Lihat = satu-satunya affordance (kurangi surface XSS)"
  - "MAP-20 no-op: badge PendingGrading sudah single (Phase 346 REC-07), tidak ditambah duplikat (D-D)"
requirements-completed: [MAP-02, MAP-04, MAP-07, MAP-08, MAP-09, MAP-20]
duration: ~12 min
completed: 2026-06-05
---

# Phase 349 Plan 01: Tab3 History Polish Summary

Polish Tab3 History `ManageAssessment` — i18n label "NIP", drop ARIA nested-interactive drill-down, empty-state/feedback client-filter (pesan 0-match aria-live + counter "Menampilkan X dari Y"), dan skeleton loader kolom-match. Controller-free (2 file view), build 0 error.

**Tasks:** 3 | **Files:** 2 modified | **Duration:** ~12 min

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | `2afb86ce` | refactor(349-01): Tab3 i18n NIP + drop ARIA nested-interactive (MAP-02/04/20) |
| 2+3 | `37a437f5` | feat(349-01): Tab3 feedback 0-match/counter + skeleton kolom-match (MAP-07/08/09) |

## What Was Built

- **MAP-02 (i18n NIP):** Sub-tab Training `Nopeg` → `NIP` (header + placeholder). Sub-tab Assessment sudah "NIP" (verified, no edit).
- **MAP-04 (drop ARIA nested-interactive):** `<tr>` Riwayat Assessment drill-down — buang `role="link"`/`tabindex="0"`/`aria-label`/`Html.Raw`, drop class `cil03-row-link`, hapus JS click/keydown row-nav + CSS dead. Tombol "Lihat" dipertahankan. Bonus: drop `Html.Raw` kurangi surface XSS.
- **MAP-07 (0-match message):** Container `#assessmentNoResult`/`#trainingNoResult` (`aria-live="polite"`) render server-side; toggle display via client-filter JS host page.
- **MAP-08 (counter):** `#assessmentVisibleCount`/`#trainingVisibleCount` "Menampilkan X dari Y" di-update `filterAssessmentRows()`/`filterTrainingRows()`.
- **MAP-09 (skeleton match):** Skeleton Tab2 Training 4→5 filter + 5→7 kolom (match `_TrainingRecordsTab` thead); Tab3 History 4→8 kolom (match thead Assessment, max). Zero CSS baru — Bootstrap `placeholder` utility.
- **MAP-20 (no-op):** Badge "Menunggu Penilaian" tetap single (Phase 346 REC-07), tidak duplikat (D-D).

## Deviations from Plan

**[Rule 1 — Consistency] MAP-02 placeholder string** — Found during: Task 1 | UI-SPEC literal LOCK = "Cari nama atau NIP..." | Dipakai "Cari nama pekerja / NIP..." agar parity persis dengan sibling Assessment placeholder (file sama, L33) — UI-SPEC §Design System core rule "Match pola yang SUDAH ADA" | Files: `_HistoryTab.cshtml` | Verification: grep NIP present, Nopeg=0 (acceptance terpenuhi) | Commit `2afb86ce`.

**[Process] Task 2 + Task 3 di-commit bersama** — `ManageAssessment.cshtml` disentuh kedua task (Task-2 JS + Task-3 skeleton) → tidak bisa split per-task tanpa partial-hunk staging. Di-commit cohesive sebagai MAP-07/08/09 (`37a437f5`). Build gate lulus untuk keduanya.

**Total deviations:** 2 (1 consistency choice, 1 process). **Impact:** none — semua acceptance criteria PASS, build 0 error.

## Verification

- `dotnet build HcPortal.csproj -c Debug` → **0 Error** (21 warning pre-existing, bukan dari perubahan ini)
- Grep acceptance Task 1: Nopeg=0, NIP=8, role=link/tabindex/Html.Raw/cil03-row-link=0, Lihat=3, PendingGrading badge=1 (single)
- Grep acceptance Task 2: NoResult×2 + aria-live×2 + "Tidak ada hasil"×2 + VisibleCount×2 + getElementById wiring×2 + "Menampilkan...rows.length"×2
- Grep acceptance Task 3: filter loop i<5, 7-col Tab2 + 8-col Tab3, zero `<style>` custom
- Manual/Playwright spot deferred ke phase gate (Plan 05): filter 0-match render + tab-switch tidak rusakkan filter (Pitfall 9)

## Self-Check: PASSED

- key-files modified exist on disk ✓
- `git log --grep="349-01"` → 2 commits ✓
- All `<acceptance_criteria>` re-verified PASS ✓
- build 0 error ✓

Ready for Plan 349-02 (Tab1/Tab2 chevron+aria + empty-state + tri-state + paging.Take).
