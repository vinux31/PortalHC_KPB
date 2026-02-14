---
phase: 02-hc-reports-dashboard
plan: 03
subsystem: hc-reports
tags: [analytics, charts, visualization, chart.js, performance-metrics]
dependency_graph:
  requires: [02-01, 02-02]
  provides: [chart-analytics, category-stats, score-distribution]
  affects: [reports-dashboard]
tech_stack:
  added: [Chart.js]
  patterns: [data-aggregation, histogram-bucketing, empty-state-handling]
key_files:
  created: []
  modified:
    - path: Controllers/CMPController.cs
      lines_added: 38
      purpose: Category statistics and score distribution data aggregation
    - path: Views/CMP/ReportsIndex.cshtml
      lines_added: 143
      purpose: Chart.js CDN, canvas elements, and chart initialization scripts
decisions:
  - decision: Chart.js for visualization library
    rationale: Consistent with existing CDP Dashboard implementation
    alternatives: [ApexCharts, Highcharts]
  - decision: Score distribution buckets (0-20, 21-40, 41-60, 61-80, 81-100)
    rationale: Standard grading ranges for clear performance visualization
    alternatives: [10-point intervals, quartiles]
  - decision: Color-coded score ranges (red→yellow→cyan→blue→green)
    rationale: Intuitive visual mapping from poor to excellent performance
    alternatives: [monochrome, category-based colors]
  - decision: In-memory score bucketing instead of EF GroupBy
    rationale: EF Core struggles with complex GroupBy bucketing expressions
    alternatives: [raw SQL, stored procedure]
metrics:
  duration_minutes: 1
  completed_date: 2026-02-14
  tasks_completed: 2
  files_modified: 2
  commits: 1
---

# Phase 2 Plan 3: Chart.js Analytics Summary

**One-liner:** Interactive Chart.js visualizations (category pass rates and score distribution histogram) with filter-responsive data on HC Reports Dashboard

## What Was Built

Added two performance analytics charts to the HC Reports Dashboard using Chart.js:

1. **Pass Rate by Category Chart**
   - Grouped bar chart showing pass rate percentage and average score per category
   - Blue bars for pass rate, green bars for average score
   - Y-axis scaled 0-100% with percentage labels
   - Tooltips display exact values with % suffix

2. **Score Distribution Chart**
   - Histogram showing assessment count across 5 score ranges
   - Color-coded buckets: red (0-20), yellow (21-40), cyan (41-60), blue (61-80), green (81-100)
   - Integer-only Y-axis for assessment counts
   - Tooltips display "N assessment(s)" format

**Key Features:**
- Charts reflect currently applied filters (same filtered query as results table)
- Empty state shown when no data available (icon + message)
- Responsive design with Bootstrap 5 card layout
- Charts placed below results table in 2-column grid layout
- Data passed via ViewBag serialized to JSON

## Implementation Details

### Controller Changes (CMPController.cs)

Added two data aggregation queries in `ReportsIndex` action after existing summary stats calculation:

**Category Statistics:**
```csharp
var categoryStats = await query
    .GroupBy(a => a.Category)
    .Select(g => new CategoryStatistic
    {
        CategoryName = g.Key,
        TotalAssessments = g.Count(),
        PassedCount = g.Count(a => a.IsPassed == true),
        PassRate = g.Count() > 0
            ? Math.Round(g.Count(a => a.IsPassed == true) * 100.0 / g.Count(), 1)
            : 0,
        AverageScore = Math.Round(g.Average(a => (double?)a.Score) ?? 0, 1)
    })
    .OrderBy(c => c.CategoryName)
    .ToListAsync();
```

**Score Distribution:**
```csharp
var allScores = await query.Select(a => a.Score ?? 0).ToListAsync();
var scoreDistribution = new List<int>
{
    allScores.Count(s => s >= 0 && s <= 20),
    allScores.Count(s => s >= 21 && s <= 40),
    allScores.Count(s => s >= 41 && s <= 60),
    allScores.Count(s => s >= 61 && s <= 80),
    allScores.Count(s => s >= 81 && s <= 100)
};
```

Both datasets passed to view via ViewBag for JSON serialization.

### View Changes (ReportsIndex.cshtml)

1. Added Chart.js CDN script tag at top of file
2. Added charts section below results table with two Bootstrap cards
3. Added `@section Scripts` block with Chart.js initialization:
   - Category chart: grouped bar chart with dual datasets
   - Score chart: single-dataset bar chart with color array
   - Empty state handlers for both charts

### Technical Decisions

**Why in-memory bucketing for score distribution?**
EF Core's GroupBy struggles with complex conditional expressions for bucketing. Loading scores into memory is safe because:
- Scores are small integers (4 bytes each)
- Even 10,000 assessments = ~40KB memory
- Filtering already applied, typical result sets are small

**Why ViewBag over ViewModel?**
Chart data is supplementary to existing ViewModel structure. ViewBag avoids:
- Breaking existing ViewModel contract
- Adding chart-specific properties to data-focused model
- Circular serialization issues with complex navigation properties

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

**Build Status:** ✅ PASS (0 errors, 20 pre-existing warnings)

**Human Verification:** ✅ APPROVED with follow-up improvements noted

**Test Results:**
- ✅ Summary cards display correctly
- ✅ Filter form renders with category/section dropdowns
- ✅ Results table shows assessment data
- ✅ Two charts render correctly below table
- ✅ Category chart shows pass rate and average score bars
- ✅ Score distribution shows color-coded histogram
- ✅ Filtering updates both table and charts
- ✅ Pagination preserves filter selections
- ✅ Excel export works with filtered data
- ✅ User history drill-down works
- ✅ Authorization enforced (non-HC users blocked)

**Follow-up Improvements Identified:**
1. Section filter may not show all sections (GAST, RFCC, NGP, DHT/HMU) - needs verification
2. User search could benefit from autocomplete/typeahead (e.g., typing "iw" suggests "Iwan")

These are noted for post-phase follow-up tasks, not blockers.

## Integration Points

**Upstream Dependencies:**
- Plan 02-01: ReportsIndex controller action, filter logic, summary stats
- Plan 02-02: Complete dashboard infrastructure and navigation

**Downstream Impact:**
- None - this completes Phase 2

**External Dependencies:**
- Chart.js CDN (cdn.jsdelivr.net/npm/chart.js)
- CategoryStatistic model (already existed in ReportsDashboardViewModel.cs)

## Files Changed

| File | Changes | Purpose |
|------|---------|---------|
| Controllers/CMPController.cs | +38 lines | Category statistics and score distribution aggregation |
| Views/CMP/ReportsIndex.cshtml | +143 lines | Chart.js CDN, canvas elements, chart initialization |

## Commits

- `221c184` - feat(02-03): add Chart.js analytics to reports dashboard

## Performance Considerations

**Query Performance:**
- Category stats uses single GroupBy query on filtered dataset
- Score distribution loads scores into memory (safe for typical dataset sizes)
- Both queries execute only on filtered data (not full table scan)

**Client Performance:**
- Chart.js library loaded from CDN (cached across pages)
- Charts render client-side (no server load)
- Empty state prevents chart initialization when no data

**Scalability:**
- Category stats: O(n) where n = filtered assessment count
- Score bucketing: O(n) in-memory operation on small integers
- Typical dashboard use: <1000 filtered assessments → negligible overhead

## Self-Check: PASSED

**Created files verification:**
- No new files created (only modifications)

**Modified files verification:**
```
✅ FOUND: Controllers/CMPController.cs (modified)
✅ FOUND: Views/CMP/ReportsIndex.cshtml (modified)
```

**Commit verification:**
```
✅ FOUND: 221c184 (feat(02-03): add Chart.js analytics to reports dashboard)
```

**Build verification:**
```
✅ PASSED: dotnet build (0 errors)
```

**All verification checks passed.**

## Phase 2 Completion Status

This plan completes Phase 2 (HC Reports Dashboard). All 3 plans executed successfully:
- ✅ 02-01: Main dashboard page with filters and summary stats
- ✅ 02-02: Excel export and user assessment history
- ✅ 02-03: Chart.js analytics visualization

**Phase 2 Total Metrics:**
- Plans completed: 3/3
- Total duration: ~6 minutes active work
- Total commits: 3 feature commits
- Files created: 3 (ReportsDashboardViewModel.cs, ReportsIndex.cshtml, UserAssessmentHistory.cshtml)
- Files modified: 5 (CMPController.cs, _Layout.cshtml, and views)

**Next Phase:** Phase 3 - Assessment Certificate Generation & PDF Export
