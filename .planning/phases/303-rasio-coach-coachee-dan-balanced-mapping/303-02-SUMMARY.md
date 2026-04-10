---
phase: 303-rasio-coach-coachee-dan-balanced-mapping
plan: "02"
status: paused-at-checkpoint
started: 2026-04-10T09:30:00Z
completed: ~
tasks_completed: 2
tasks_total: 3
subsystem: CoachWorkload UI
tags: [coach, workload, chart.js, ajax, modal, cmp-menu]
dependency_graph:
  requires: ["303-01"]
  provides: ["Views/Admin/CoachWorkload.cshtml", "Views/CMP/Index.cshtml CoachWorkload card"]
  affects: ["Views/Admin/CoachCoacheeMapping.cshtml assign modal"]
tech_stack:
  added: ["Chart.js v4 via CDN (horizontal bar chart)"]
  patterns: ["AJAX POST dengan anti-forgery token", "Role-based rendering Razor", "ViewBag JSON serialize ke JS"]
key_files:
  created:
    - Views/Admin/CoachWorkload.cshtml
  modified:
    - Views/CMP/Index.cshtml
    - Views/Admin/CoachCoacheeMapping.cshtml
    - Controllers/CoachMappingController.cs
decisions:
  - "Chart.js v4 dengan indexAxis:'y' untuk horizontal bar (bukan Chart.js v2 horizontalBar)"
  - "Auto-suggest coach hint menggunakan data-section attribute di option element untuk matching tanpa server round-trip"
  - "coachWorkloads dictionary di-serialize ke JS saat page load — tidak perlu AJAX endpoint terpisah"
---

## Summary

Halaman Coach Workload lengkap dengan Chart.js horizontal bar berwarna threshold, 4 summary cards, tabel detail, saran penyeimbangan dengan approve/skip AJAX, modal Set Threshold, dan auto-suggest coach beban terendah di assign modal.

## Tasks Completed

| # | Task | Status | Commit |
|---|------|--------|--------|
| 1 | View CoachWorkload.cshtml — halaman lengkap | Done | 978b1b6d |
| 2 | Menu CMP/Index + auto-suggest di assign modal | Done | 43f856a3 |
| 3 | Verifikasi visual dan fungsional | Pending checkpoint | — |

## Key Files

### Created
- `Views/Admin/CoachWorkload.cshtml` — Halaman utama Coach Workload (466 baris): summary cards, Chart.js horizontal bar, tabel detail, saran reassign, modal threshold, filter section, export Excel

### Modified
- `Views/CMP/Index.cshtml` — Ditambah card "Coach Workload" di section Admin/HC
- `Views/Admin/CoachCoacheeMapping.cshtml` — Ditambah hint coachSuggestHint + JS updateCoachSuggestHint() + data-section pada coach options
- `Controllers/CoachMappingController.cs` — Ditambah ViewBag.CoachWorkloads di CoachCoacheeMapping GET action

## Verification

- `dotnet build` — 0 errors, 0 warnings relevan
- Semua acceptance criteria Task 1 dan Task 2 terpenuhi

## Deviations from Plan

None — diimplementasikan sesuai rencana.

## Known Stubs

None — semua data terhubung ke ViewBag dari Plan 01 controller actions.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| mitigated: T-303-07 | Views/Admin/CoachWorkload.cshtml | Anti-forgery token header dikirim di semua AJAX POST (approve, skip, threshold) |
| mitigated: T-303-09 | Views/Admin/CoachWorkload.cshtml | User.IsInRole("Admin") untuk semua tombol aksi; HC hanya read-only |

## Self-Check: PASSED

- `Views/Admin/CoachWorkload.cshtml` — ada (466 baris)
- Commit 978b1b6d — ada (Task 1)
- Commit 43f856a3 — ada (Task 2)
- Build sukses tanpa error
