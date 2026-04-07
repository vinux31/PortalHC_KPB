---
phase: 301-advanced-reporting
plan: "01"
subsystem: analytics-backend
tags: [item-analysis, gain-score, analytics, reporting, backend]
dependency_graph:
  requires: []
  provides: [GetItemAnalysisData, GetGainScoreData, GetPrePostAssessmentList, GainScoreTrend]
  affects: [Controllers/CMPController.cs, Models/AnalyticsDashboardViewModel.cs]
tech_stack:
  added: []
  patterns: [Kelley-discrimination-index, normalized-gain-score-formula]
key_files:
  created: []
  modified:
    - Models/AnalyticsDashboardViewModel.cs
    - Controllers/CMPController.cs
decisions:
  - "IsCorrect di-resolve via PackageOption navigation property, bukan field di PackageUserResponse (model tidak memiliki field IsCorrect)"
  - "GainScoreTrend filter bagian/unit menggunakan sub-query userId karena prePostPostSessions sudah di-materialize ke memory"
  - "Gain score formula normalized: (Post-Pre)/(100-Pre)*100 dengan edge case PreScore=100 -> Gain=100"
metrics:
  duration: "25 menit"
  completed_date: "2026-04-07"
  tasks_completed: 2
  files_modified: 2
---

# Phase 301 Plan 01: Backend Models dan Endpoints Item Analysis + Gain Score Report

**One-liner:** 3 endpoint analytics baru (item analysis Kelley D-index, gain score per worker/elemen/group, PrePostTest list) + GainScoreTrend terintegrasi di GetAnalyticsData.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Model classes + GetItemAnalysisData + GetPrePostAssessmentList | 8ddaa556 | Models/AnalyticsDashboardViewModel.cs, Controllers/CMPController.cs |
| 2 | GetGainScoreData + GainScoreTrend di GetAnalyticsData | 8ddaa556 | Controllers/CMPController.cs |

## What Was Built

### Models (AnalyticsDashboardViewModel.cs)

- `ItemAnalysisResult` — container dengan TotalResponden dan IsLowN flag
- `ItemAnalysisRow` — per-soal: DifficultyIndex (p-value), DiscriminationIndex (Kelley), Distractors
- `DistractorRow` — opsi jawaban dengan Count dan Percent
- `GainScoreResult` — container untuk PerWorker, PerElemen, GroupComparison
- `GainScorePerWorker` — PreScore, PostScore, GainScore per pekerja
- `GainScorePerElemen` — AvgPre, AvgPost, AvgGain per elemen teknis
- `GroupComparisonItem` — group comparison per Bagian (RPT-07)
- `PrePostAssessmentListItem` — dropdown item untuk PrePostTest
- `GainScoreTrendItem` — trend AvgGainScore per bulan
- `AnalyticsDataResult.GainScoreTrend` property baru

### Endpoints (CMPController.cs)

- `GET /CMP/GetPrePostAssessmentList?bagian=&unit=` — daftar assessment PrePostTest untuk dropdown
- `GET /CMP/GetItemAnalysisData?assessmentGroupId=` — item analysis lengkap per soal
- `GET /CMP/GetGainScoreData?assessmentGroupId=` — gain score per pekerja, per elemen, group comparison
- `GetAnalyticsData` diperluas dengan GainScoreTrend per bulan

### Helper Method

- `CalculateKelleyDiscrimination` — static helper: upper/lower 27% split, menghitung D-index

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] PackageUserResponse tidak memiliki field IsCorrect**
- **Found during:** Task 1
- **Issue:** PLAN.md menggunakan `r.IsCorrect` pada `PackageUserResponse`, tetapi model tersebut tidak memiliki field IsCorrect — IsCorrect ada di `PackageOption`
- **Fix:** Diubah menjadi `r.PackageOption != null && r.PackageOption.IsCorrect` dengan menambahkan `.Include(r => r.PackageOption)` pada query
- **Files modified:** Controllers/CMPController.cs
- **Commit:** 8ddaa556

**2. [Rule 2 - Missing] GainScoreTrend filter bagian/unit menggunakan sub-query**
- **Found during:** Task 2
- **Issue:** PLAN.md mengasumsikan filter bisa diterapkan langsung pada query prePostPairs, tetapi setelah `.ToListAsync()` data sudah di-memory. Filter via `.Where(p => bagianUserIds.Contains(p.UserId))` pada list in-memory digunakan.
- **Fix:** Query userId per bagian/unit dilakukan terpisah lalu filter di-apply pada list in-memory
- **Files modified:** Controllers/CMPController.cs
- **Commit:** 8ddaa556

## Self-Check: PASSED

- [x] Models/AnalyticsDashboardViewModel.cs contains `public class ItemAnalysisResult`
- [x] Models/AnalyticsDashboardViewModel.cs contains `public class GainScoreResult`
- [x] Models/AnalyticsDashboardViewModel.cs contains `public class DistractorRow`
- [x] Models/AnalyticsDashboardViewModel.cs contains `public class GainScoreTrendItem`
- [x] Models/AnalyticsDashboardViewModel.cs contains `GainScoreTrend` property
- [x] Controllers/CMPController.cs contains `GetItemAnalysisData(int assessmentGroupId)`
- [x] Controllers/CMPController.cs contains `GetPrePostAssessmentList(string? bagian`
- [x] Controllers/CMPController.cs contains `CalculateKelleyDiscrimination`
- [x] Controllers/CMPController.cs contains `IsLowN = totalResponden < 30`
- [x] Controllers/CMPController.cs contains `GetGainScoreData(int assessmentGroupId)`
- [x] Controllers/CMPController.cs contains `GainScoreTrend = gainScoreTrend`
- [x] Controllers/CMPController.cs contains `preScore >= 100 ? 100`
- [x] dotnet build: 0 errors, 74 warnings
- [x] Commit 8ddaa556 exists
