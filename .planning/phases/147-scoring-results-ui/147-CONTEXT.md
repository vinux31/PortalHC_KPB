# Phase 147: Scoring & Results UI - Context

**Gathered:** 2026-03-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Results page displays per-sub-competency analysis with radar chart and summary table after exam submission. Calculates correct percentage per sub-competency via LINQ GroupBy on SubCompetency field. Certificate changes are out of scope.

</domain>

<decisions>
## Implementation Decisions

### Section Placement
- After score summary card + motivational message, before Kompetensi Diperoleh section
- Inside a card with header "Analisis Sub Kompetensi" (with bi-radar icon or similar)
- Chart on top (full-width, centered, max height ~300-350px), summary table below

### Radar Chart Styling
- Blue theme: fill rgba(54, 162, 235, 0.2), border rgba(54, 162, 235, 1), points solid blue
- Fixed 0-100% scale with scale ring labels (0%, 25%, 50%, 75%, 100%)
- Data point values shown as tooltip on hover (standard Chart.js behavior)
- Long sub-competency names truncated to ~20 chars with ellipsis on radar labels; full name in tooltip
- Min 3 sub-competencies required for radar chart; below 3, show table only

### Summary Table
- Columns: Sub Kompetensi, Benar, Total, Persentase
- Rows sorted alphabetically by sub-competency name
- Percentage cells color-coded using Bootstrap badges: green (bg-success) if >= assessment's PassPercentage, red (bg-danger) if below
- Totals row at bottom showing sum of Benar, sum of Total, overall percentage
- No row number (#) column

### Edge Cases
- Untagged questions (null/empty SubCompetency) grouped under "Lainnya" row/axis
- 1-2 sub-competencies: show table only, no radar chart
- Legacy assessments (no SubCompetency data at all): entire section silently hidden — no "no data" message
- Print-friendly: chart and table render in print view

### Admin/HC View
- Identical sub-competency analysis for Admin/HC viewing worker results — no differences from owner view

### Claude's Discretion
- Exact chart canvas dimensions and responsive breakpoints
- Bootstrap icon choice for card header
- Print CSS details
- Tooltip formatting

</decisions>

<specifics>
## Specific Ideas

- Color thresholds are dynamic per assessment (uses PassPercentage from the assessment session, not a fixed cutoff)
- Certificate page is explicitly out of scope — potential future phase

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- Chart.js CDN already loaded in `_Layout.cshtml` — no additional script imports needed
- `AssessmentResultsViewModel` in Models — needs new properties for sub-competency data
- Results page (`Views/CMP/Results.cshtml`) — add new card section between motivational message and Kompetensi Diperoleh

### Established Patterns
- Results page uses Bootstrap cards with `shadow-sm`, `card-header bg-light`, `table-hover` styling
- Package path loads `PackageQuestion` with `.Include(q => q.Options)` — SubCompetency field accessible
- Score already calculated in controller; sub-competency GroupBy follows same pattern

### Integration Points
- `CMPController.Results()` action (line ~1785) — add GroupBy logic after loading packageQuestions
- `AssessmentResultsViewModel` — add sub-competency score list property
- Package path only (SubCompetency lives on PackageQuestion); legacy path has no sub-competency data

</code_context>

<deferred>
## Deferred Ideas

- Sub-competency data on Certificate page — future phase
- Enhanced Admin/HC view with team averages/benchmarks — future phase

</deferred>

---

*Phase: 147-scoring-results-ui*
*Context gathered: 2026-03-10*
