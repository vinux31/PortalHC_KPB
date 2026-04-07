---
phase: 301-advanced-reporting
plan: 03
subsystem: reporting
tags: [closedxml, excel-export, chart-js, gain-score, item-analysis]

# Dependency graph
requires:
  - phase: 301-01
    provides: GetItemAnalysisData dan GetGainScoreData endpoints, ExcelExportHelper helper

provides:
  - ExportItemAnalysisExcel endpoint (2 sheet: Item Analysis + Distractor Analysis)
  - ExportGainScoreExcel endpoint (2 sheet: Per Pekerja + Per Elemen Kompetensi)
  - Gain Score Trend line chart di tab Trend AnalyticsDashboard

affects: [verifier-301]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Re-query data di export endpoints (tidak share state dengan API endpoints)"
    - "ExcelExportHelper.CreateSheet + ToFileResult untuk semua export Excel"
    - "window._gainTrendChart untuk lifecycle management Chart.js instance"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/AnalyticsDashboard.cshtml

key-decisions:
  - "Export endpoints re-query database secara independen (tidak share data dengan GetItemAnalysisData/GetGainScoreData)"
  - "IsCorrect dibaca dari PackageOption navigation property, bukan dari PackageUserResponse langsung"
  - "Fungsi export JS lama di-update endpoint-nya (ExportItemAnalysis -> ExportItemAnalysisExcel) daripada membuat duplikat"

patterns-established:
  - "Export Excel: CreateSheet -> FreezeRows(1) -> SetBackgroundColor header -> populate rows -> ToFileResult"

requirements-completed: [RPT-05, RPT-06]

# Metrics
duration: 15min
completed: 2026-04-07
---

# Phase 301 Plan 03: Export Excel + Gain Score Trend Summary

**Export Excel Item Analysis (2 sheet) dan Gain Score (2 sheet) via ClosedXML, plus line chart Gain Score Trend di tab Trend dengan Chart.js**

## Performance

- **Duration:** 15 min
- **Started:** 2026-04-07T10:10:00Z
- **Completed:** 2026-04-07T10:25:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- ExportItemAnalysisExcel: sheet Item Analysis (p-value, D-index) + Distractor Analysis (highlight hijau jawaban benar)
- ExportGainScoreExcel: sheet Per Pekerja (nama, NIP, bagian, pre/post/gain) + Per Elemen Kompetensi
- Semua export: header biru #0d6efd, font putih, freeze row 1, auto-fit kolom
- Gain Score Trend line chart (ungu #6f42c1) di tab Trend, dengan tooltip sample count
- Export JS functions di-update ke endpoint yang benar (ExportItemAnalysisExcel/ExportGainScoreExcel)

## Task Commits

1. **Task 1: ExportItemAnalysisExcel + ExportGainScoreExcel endpoints** - `6ce094a9` (feat)
2. **Task 2: Gain Score Trend line chart + export JS functions** - `6b9f6b0c` (feat)

## Files Created/Modified

- `Controllers/CMPController.cs` - Tambah ExportItemAnalysisExcel dan ExportGainScoreExcel endpoints dengan Authorize(Roles="Admin, HC")
- `Views/CMP/AnalyticsDashboard.cshtml` - Tambah canvas gainScoreTrendChart di tab Trend, fungsi renderGainScoreTrend, update export JS functions

## Decisions Made

- Export endpoints re-query database secara independen: menghindari coupling dengan API endpoints, lebih robust
- `IsCorrect` harus dibaca dari `r.PackageOption.IsCorrect` bukan `r.IsCorrect` — PackageUserResponse tidak punya field IsCorrect
- Fungsi export JS lama sudah ada dari plan 02 tapi merujuk endpoint lama; di-update daripada duplikat

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed wrong IsCorrect property reference**
- **Found during:** Task 1 (ExportItemAnalysisExcel implementation)
- **Issue:** Plan template menggunakan `r.IsCorrect` tapi PackageUserResponse tidak memiliki property tersebut — IsCorrect ada di PackageOption
- **Fix:** Tambah `.Include(r => r.PackageOption)` di query, ubah `r.IsCorrect` menjadi `r.PackageOption != null && r.PackageOption.IsCorrect`
- **Files modified:** Controllers/CMPController.cs
- **Verification:** dotnet build 0 errors
- **Committed in:** 6ce094a9 (Task 1 commit)

**2. [Rule 1 - Bug] Removed duplicate export JS functions**
- **Found during:** Task 2 (AnalyticsDashboard.cshtml update)
- **Issue:** Export functions exportItemAnalysis/exportGainScore sudah ada dari plan 02 dengan endpoint lama (ExportItemAnalysis/ExportGainScore tanpa "Excel")
- **Fix:** Hapus fungsi duplikat baru, update fungsi lama ke endpoint ExportItemAnalysisExcel/ExportGainScoreExcel
- **Files modified:** Views/CMP/AnalyticsDashboard.cshtml
- **Verification:** Tidak ada duplikat fungsi, build berhasil
- **Committed in:** 6b9f6b0c (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (2 Rule 1 - Bug)
**Impact on plan:** Kedua fix diperlukan untuk kebenaran fungsional. Tidak ada scope creep.

## Issues Encountered

- PackageUserResponse tidak memiliki property IsCorrect langsung — perlu navigation property PackageOption. Konsisten dengan implementasi GetItemAnalysisData yang sudah ada.

## User Setup Required

None - tidak ada konfigurasi eksternal.

## Next Phase Readiness

- Semua export endpoint siap: ExportItemAnalysisExcel dan ExportGainScoreExcel
- Gain Score Trend chart siap tampil saat data gainScoreTrend tersedia dari GetAnalyticsData
- Phase 301 plan 03 lengkap — siap verifikasi manual

---
*Phase: 301-advanced-reporting*
*Completed: 2026-04-07*
