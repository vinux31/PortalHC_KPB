# Phase 121: CDP Dashboard Filter & Assessment Analytics Redesign - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Overhaul CDP Dashboard filters on both tabs (Coaching Proton + Assessment Analytics). Add cascade filter system to Coaching Proton tab (currently has none). Redesign Assessment Analytics filters to cascade. Align UI/layout between both tabs. Expand filter visibility based on role level.

</domain>

<decisions>
## Implementation Decisions

### Coaching Proton Filters
- Add 4 filters: Section → Unit → Category → Track (cascade order)
- Cascade behavior: each dropdown change triggers AJAX, updates dependent dropdowns + refreshes all data (KPIs, charts, table)
- No Apply button — auto-refresh on change
- Filter bar placed ABOVE KPI cards (top of tab)
- Table columns stay fixed regardless of filter selection

### Assessment Analytics Filters
- Remove: Start Date, End Date, User Search filters
- Add: Unit filter (new)
- Final filter order: Section → Unit → Category (cascade)
- Cascade behavior: each dropdown change triggers AJAX, updates dependent dropdowns + refreshes everything (KPIs, charts, table)
- Remove Apply Filters button (cascade auto-refreshes)
- Keep Clear button (resets all to "Semua"/All)
- Keep pagination (AJAX-based, no full page reload)
- Default: show all data when no filters selected
- Excel export respects active filters
- Tab visibility remains HC/Admin only

### Role-Based Filter Scoping (both tabs)
- Level 1-3 (Admin, HC, Direktur, VP, Manager): HasFullAccess=true → "Semua Section" + all categories, default to showing all
- Level 4 (Section Head, Sr Supervisor): Section filter pre-filled & locked to their section, can filter Unit/Category/Track within
- Level 5 (Coach, Supervisor): Section + Unit pre-filled & locked, can filter Category/Track within their unit
- Uses existing UserRoles.GetRoleLevel() and HasFullAccess() helpers

### AJAX Implementation
- Single endpoint per tab returning partial HTML (server-rendered Razor partials)
- JS replaces container innerHTML on filter change
- Loading state: fade content to 50% opacity + centered spinner overlay
- Fixes current bug where Assessment filter change redirects to Coaching Proton tab

### UI Alignment
- Both tabs follow identical layout: Filter row → KPI cards → Charts → Table
- Identical filter bar position (above KPI cards) on both tabs
- Identical table styling (header color, row striping, hover effects)
- Assessment Analytics chart types: Claude's discretion (keep best-fit chart types for assessment data, but match visual style/colors)

### Claude's Discretion
- Chart type selection for Assessment Analytics (keep bar charts or switch — pick what fits the data best)
- Exact AJAX endpoint naming and parameter design
- Spinner/overlay CSS implementation details
- How cascade dropdown options are populated (from filtered data queries)

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `UserRoles.GetRoleLevel()` and `HasFullAccess()` — role-based scoping already built
- `_CoachingProtonPartial.cshtml` — existing partial with KPI cards, charts, table (reference for layout)
- `_AssessmentAnalyticsPartial.cshtml` — existing partial with filter form, charts, table (to be redesigned)
- `CDPDashboardViewModel` with sub-models — existing view model structure
- `OrganizationStructure.GetAllSections()` — section list helper
- `ExportAnalyticsResults` action — existing Excel export (needs filter param update)

### Established Patterns
- Dashboard action (CDPController lines 234-609) handles role scoping and data loading
- Coaching Proton data scoping: HC/Admin=all, SectionHead=section, Coach=unit (lines 326-361)
- Chart.js used for charts in both partials
- Bootstrap 5 card/table styling throughout

### Integration Points
- CDPController.Dashboard action needs new AJAX endpoints (2 new actions)
- Both partials need restructuring to support AJAX partial rendering
- JavaScript needs cascade filter handlers (onChange → fetch → replace HTML)

</code_context>

<specifics>
## Specific Ideas

- "untuk design dan ui tab assessment analytics, bisa di samakan dengan coaching proton, biar setara" — both tabs should feel like one cohesive page
- "setiap perubahan filter langsung update table" — immediate feedback on filter change, no Apply button
- "untuk fungsi level 3 keatas filter ada fasilitas melihat semua section dan category" — Level 3+ gets cross-section visibility

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 121-cdp-dashboard-filter-assessment-analytics-redesign*
*Context gathered: 2026-03-08*
