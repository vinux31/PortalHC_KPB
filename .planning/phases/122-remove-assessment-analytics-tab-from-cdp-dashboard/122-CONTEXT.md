# Phase 122: Remove Assessment Analytics Tab from CDP Dashboard - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Remove the Assessment Analytics tab entirely from the CDP Dashboard page. The functionality already exists in CMP/Records Team View. Clean up all related code (backend, frontend, view models, partials, JS). Simplify Dashboard to a single-section Coaching Proton page.

</domain>

<decisions>
## Implementation Decisions

### Cleanup Scope — Full Removal
- Remove ALL Assessment Analytics code:
  - Controller actions: `FilterAssessmentAnalytics`, `ExportAnalyticsResults`, `BuildAnalyticsSubModelAsync`
  - View model: `AssessmentAnalyticsSubModel` class and its property from `CDPDashboardViewModel`
  - Partial views: `_AssessmentAnalyticsPartial.cshtml`, `_AssessmentAnalyticsContentPartial.cshtml`
  - JavaScript: all analytics AJAX handlers, cascade filter logic for analytics tab in `Dashboard.cshtml`
- Verify `ExportAnalyticsResults` has no other callers before deleting (Claude should check)
- Scan for and remove any navigation links pointing to the Analytics tab (e.g., #analytics hash links)
- Update CDP hub page (Index) card text — remove any Analytics references
- Dashboard page accessible to all authenticated users (no HC/Admin role restriction on the page itself)

### Dashboard Layout — Single Section
- Remove tab UI entirely (tab headers, tab content panels) — single-section page
- Keep Coaching Proton content as separate partial (`_CoachingProtonPartial`) rendered directly
- Page title changes to "Coaching Proton Dashboard"

### Navigation & Naming
- Navbar label changes to "Coaching Proton Dashboard"
- CDP hub card title changes to "Coaching Proton Dashboard"
- Hub card description updated to remove Analytics references
- Old Analytics tab URLs just land on the Dashboard page normally (no redirect)

### Claude's Discretion
- Any necessary CSS adjustments after removing tab container
- Whether to simplify Dashboard action method after removing analytics data loading
- How to handle any remaining tab-related CSS classes

</decisions>

<code_context>
## Existing Code Insights

### Files to Remove
- `Views/CDP/Shared/_AssessmentAnalyticsPartial.cshtml` — tab partial
- `Views/CDP/Shared/_AssessmentAnalyticsContentPartial.cshtml` — AJAX content partial

### Files to Modify
- `Controllers/CDPController.cs` — remove FilterAssessmentAnalytics, ExportAnalyticsResults, BuildAnalyticsSubModelAsync
- `Models/CDPDashboardViewModel.cs` — remove AssessmentAnalyticsSubModel class and property
- `Views/CDP/Dashboard.cshtml` — remove tab structure, analytics JS, simplify to single section
- `Views/CDP/Index.cshtml` — update Dashboard card text
- `Views/Shared/_Layout.cshtml` (or navbar partial) — update nav link label

### Established Patterns
- Phase 121 just added the cascade filter system to both tabs — analytics code is fresh
- Dashboard action (CDPController) builds both sub-models — needs simplification

</code_context>

<specifics>
## Specific Ideas

- "fungsinya sudah ada di page CMP/Records tab Team View" — Assessment Analytics is redundant
- User wants Excel and PDF export added to Coaching Proton tab (deferred — separate phase)

</specifics>

<deferred>
## Deferred Ideas

- Add Excel and PDF export to Coaching Proton tab — separate phase (user requested during discussion)

</deferred>

---

*Phase: 122-remove-assessment-analytics-tab-from-cdp-dashboard*
*Context gathered: 2026-03-08*
