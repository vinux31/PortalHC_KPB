---
phase: 224-analytics-dashboard-hc
plan: "01"
subsystem: CMP / Analytics
tags: [analytics, dashboard, backend, viewmodel, controller]
dependency_graph:
  requires: []
  provides: [analytics-backend-endpoints, analytics-viewmodel]
  affects: [CMPController, CMP Hub]
tech_stack:
  added: []
  patterns: [EF Core GroupBy aggregate, cascade dropdown endpoints, role-scoped card visibility]
key_files:
  created:
    - Models/AnalyticsDashboardViewModel.cs
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Index.cshtml
decisions:
  - "ParentCategory navigation property tidak ada — gunakan Parent (sesuai AssessmentCategory model)"
  - "subKategori filter di GetAnalyticsData di-skip — AssessmentSession tidak punya SubCategory field"
  - "ExpiringSoon query filter Bagian/Unit saja (bukan kategori) sesuai Research Open Question 2"
metrics:
  duration: "~15 menit"
  completed: "2026-03-22"
  tasks_completed: 2
  files_created: 1
  files_modified: 2
---

# Phase 224 Plan 01: Analytics Backend — ViewModel, Endpoints, CMP Hub

Backend analytics infrastructure: 6 DTO/ViewModel classes, 4 controller action methods (fail rate + trend + ET breakdown + expiring soon aggregation queries), dan CMP Hub card link.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Buat ViewModel dan DTO classes | 7d03338 | Models/AnalyticsDashboardViewModel.cs (created) |
| 2 | Tambah controller actions dan CMP Hub link | ca94e89 | Controllers/CMPController.cs, Views/CMP/Index.cshtml |

## What Was Built

### Models/AnalyticsDashboardViewModel.cs
6 class terdefinisi:
- `AnalyticsDashboardViewModel` — page load data (Sections, Categories)
- `AnalyticsDataResult` — JSON response wrapper (4 dataset)
- `FailRateItem` — fail rate per Bagian + Kategori dengan computed `FailRatePercent`
- `TrendItem` — trend bulanan Passed/Failed dengan computed `Label`
- `EtBreakdownItem` — avg/min/max skor ET per Elemen Teknis + Kategori
- `ExpiringSoonItem` — sertifikat akan expired (NamaPekerja, NamaSertifikat, TanggalExpired, SectionUnit)

### Controllers/CMPController.cs (4 action methods)
- `AnalyticsDashboard()` — loads dropdown data dari DB (Sections + parent Categories)
- `GetAnalyticsData()` — 4 parallel aggregate queries dengan filter opsional: bagian, unit, kategori, periode
- `GetAnalyticsCascadeUnits()` — cascade dropdown units untuk bagian tertentu
- `GetAnalyticsCascadeSubKategori()` — cascade dropdown sub-kategori untuk kategori tertentu

### Views/CMP/Index.cshtml
Card "Analytics Dashboard" ditambahkan di CMP Hub, visible hanya untuk role Admin dan HC.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Navigation property ParentCategory tidak ada**
- **Found during:** Task 2 (build error)
- **Issue:** Plan mendefinisikan query menggunakan `c.ParentCategory.Name` tetapi AssessmentCategory model hanya memiliki navigation property `Parent` (bukan `ParentCategory`)
- **Fix:** Ganti `c.ParentCategory` menjadi `c.Parent` di GetAnalyticsCascadeSubKategori query
- **Files modified:** Controllers/CMPController.cs
- **Commit:** ca94e89 (included in same commit)

## Self-Check: PASSED

- Models/AnalyticsDashboardViewModel.cs: FOUND
- Controllers/CMPController.cs: FOUND
- Commit 7d03338: FOUND
- Commit ca94e89: FOUND
