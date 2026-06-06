---
phase: 349-manageassessment-monitoring-low-polish
plan: 02
subsystem: ManageAssessment Tab1/Tab2 (view + controller ViewBag)
tags: [a11y, chevron, empty-state, tri-state, pagination, razor]
requires: [349-01]
provides:
  - "Tab1/Tab2 toggle collapse: aria-label deskriptif + chevron rotate CSS"
  - "Tab1 empty-state filter-aware + Reset Semua Filter (clear-all)"
  - "Tab2 manual-assessment IsPassed==null -> Menunggu Penilaian (konstanta)"
  - "Tab2 badge selalu CompletionDisplayText"
  - "Tab1 pagination baca ViewBag.PageSize (drop magic-number 20)"
affects:
  - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
  - Views/Admin/Shared/_TrainingRecordsTab.cshtml
  - Controllers/AssessmentAdminController.cs
tech-stack:
  added: []
  patterns: [css-aria-expanded-rotate, switch-constant-pattern, viewbag-pagesize]
key-files:
  created: []
  modified:
    - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
    - Views/Admin/Shared/_TrainingRecordsTab.cshtml
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "MAP-03 chevron rotate via CSS [aria-expanded] (bukan JS) — Bootstrap auto-set; CSS block identik di kedua partial"
  - "MAP-06 Reset Semua Filter = navigate ke ManageAssessment?tab=assessment tanpa query (fresh load clear semua filter); reuse di searchTerm branch + filter branch"
  - "MAP-18 statusClass switch pakai constant pattern AssessmentConstants.AssessmentStatus.PendingGrading (const string valid di C# switch expression)"
  - "MAP-21 controller ViewBag.PageSize = paging.Take (1 baris, ViewBag-only, tanpa ubah perilaku paging)"
requirements-completed: [MAP-03, MAP-05, MAP-06, MAP-18, MAP-19, MAP-21]
duration: ~16 min
completed: 2026-06-05
---

# Phase 349 Plan 02: Tab1/Tab2 Polish Summary

Polish Tab1 Assessment Groups + Tab2 Training Records — a11y chevron+aria toggle collapse (CSS rotate), empty-state filter-aware + "Reset Semua Filter", tri-state IsPassed==null + selalu CompletionDisplayText, dan drop magic-number 20 via ViewBag.PageSize. 3 file (2 partial + controller 1 baris), build 0 error.

**Tasks:** 3 | **Files:** 3 modified | **Duration:** ~16 min

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1+2+3 | `66546ac0` | feat(349-02): Tab1/Tab2 a11y chevron + empty-state + tri-state + paging.Take (MAP-03/05/06/18/19/21) |

> Catatan: 3 task di-commit cohesive — kedua partial disentuh oleh ≥2 task (MAP-03 chevron di Tab1+Tab2; MAP-21 di controller+Tab1; MAP-18/19 di Tab2), tidak bisa split per-task tanpa partial-hunk staging. Build gate lulus untuk seluruh perubahan.

## What Was Built

- **MAP-03 (a11y chevron):** Tab1 button "N peserta" + Tab2 expand-records → `.toggle-chevron` + `aria-label="Tampilkan/sembunyikan ..."` + ikon `chevron-icon`. CSS `.toggle-chevron[aria-expanded="true"] .chevron-icon { transform: rotate(180deg); }` (block identik di kedua partial; Bootstrap auto-set `aria-expanded`). Fallback JS bila tak rotate → verifikasi Plan 05 (Assumption A1).
- **MAP-05 (empty-state filter-aware):** Cabang `hasActiveFilter` (kategori/status aktif + search kosong) → "Tidak ada assessment untuk filter ini" + "Coba ubah atau reset filter...". Pisah dari cabang "Buat assessment pertama".
- **MAP-06 (Reset Semua Filter, D-01):** Relabel "Hapus Pencarian" → "Reset Semua Filter" di searchTerm branch + filter branch; link reset ke `ManageAssessment?tab=assessment` (fresh load hapus search+kategori+status).
- **MAP-18 (tri-state, konstanta D-C):** `_TrainingRecordsTab` projection `IsPassed==null` → `AssessmentConstants.AssessmentStatus.PendingGrading` (Detail + Status) + statusClass switch cabang `PendingGrading => "bg-warning text-dark"`. Zero literal.
- **MAP-19 (CompletionDisplayText):** Badge "Status Training" selalu `@worker.CompletionDisplayText` — drop gated `worker.TotalTrainings == 0 → "Belum ada"`.
- **MAP-21 (paging.Take):** Controller `ManageAssessmentTab_Assessment` → `ViewBag.PageSize = paging.Take`; `_AssessmentGroupsTab` baca `var pageSize = (int)(ViewBag.PageSize ?? 20)` untuk hidden input + rowNum + text "Menampilkan". Zero literal 20.

## Deviations from Plan

**[Process] Plan di-commit dalam 1 commit cohesive** — file overlap antar task (kedua partial disentuh ≥2 task) → tidak bisa per-task commit tanpa partial-hunk staging. Di-commit utuh sebagai MAP-03/05/06/18/19/21 (`66546ac0`).

**Total deviations:** 1 (process). **Impact:** none — semua acceptance criteria PASS, build 0 error.

## Verification

- `dotnet build HcPortal.csproj -c Debug` → **0 Error**
- Grep acceptance T1: toggle-chevron (3/3 kedua partial), aria-label Tampilkan/sembunyikan (Tab1) + rekam jejak (Tab2), rotate-180deg ×1 tiap partial
- Grep acceptance T2: "Tidak ada assessment untuk filter ini"=1, "Coba ubah atau reset filter..."=1, "Reset Semua Filter"=2, "Hapus Pencarian"=0
- Grep acceptance T3: PendingGrading konstanta (projection+statusClass), literal "Menunggu Penilaian"=0, ">Belum ada<"=0, "ViewBag.PageSize = paging.Take"=2 (Assessment baru + Training existing MAM-07), literal `*20`=0 di groupsTab
- Phase gate (Plan 05): Playwright spot — chevron rotate saat expand, empty-state filter-aware, Tab1 paging text konsisten, Tab2 essay-pending badge amber

## Self-Check: PASSED

- key-files modified exist on disk ✓
- `git log --grep="349-02"` → 1 commit ✓
- All `<acceptance_criteria>` re-verified PASS ✓
- build 0 error ✓

Ready for Plan 349-03 (Monitoring list: real-time/kategori + Regen Token + exclude-Cancelled + search Category).
