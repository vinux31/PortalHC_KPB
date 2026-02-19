# Phase 12: Dashboard Consolidation - Context

**Gathered:** 2026-02-19
**Status:** Ready for planning

<domain>
## Phase Boundary

Merge HC Reports (CMPController.ReportsIndex) and Dev Dashboard (CDPController.DevDashboard) into the CDP Dashboard as two role-scoped tabs: "Proton Progress" (all roles) and "Assessment Analytics" (HC/Admin only). Both standalone pages are fully deleted in this phase. New coachee access is added to the Dashboard for the first time.

</domain>

<decisions>
## Implementation Decisions

### Assessment Analytics tab — role visibility
- Follows **isHCAccess pattern**: visible to Admin and HC regardless of SelectedView simulation
- Admin simulating Coachee/Atasan/Coach still sees the Analytics tab — SelectedView is ignored for this gate
- HC view simulation (Admin with SelectedView = HC) also sees the Analytics tab
- Server vs client-side enforcement of the tab gate: **Claude's discretion**

### HC Reports retirement
- CMPController.ReportsIndex() action, its view, and its nav link are **deleted entirely** in Plan 12-03
- No redirect — the route is fully retired, not preserved
- Url.Action("ReportsIndex") calls in UserAssessmentHistory.cshtml: **remove the links entirely** (no replacement link)
- HC Reports nav link in _Layout.cshtml removed in the **same plan as page deletion (12-03)**
- Excel export button stays on the Analytics tab at the **top level** (not only accessible from drill-down)
- UserAssessmentHistory.cshtml disposition (whether to keep as drill-down from Analytics or remove): **Claude's discretion**

### Dev Dashboard retirement
- CDPController.DevDashboard() action, DevDashboard.cshtml, and its nav link are **deleted entirely** in Plan 12-03
- Both retirements (HC Reports + Dev Dashboard) happen **together in Plan 12-03**

### Proton Progress tab — layout
- **Flat table** of all coachees, sorted by name or section — no section grouping headers
- **Stat cards at top**, then table (e.g. total coachees, pending approvals, completion rate)
- **Chart.js trend charts preserved** — competency level changes over time from former Dev Dashboard
- **No approval queue on the tab** — approvals are actioned from individual Deliverable pages, not from Dashboard

### Coachee access — new in Phase 12
- Coachees were blocked from Dev Dashboard; Phase 12 opens Dashboard to all roles
- Coachee sees Proton Progress tab only (no Analytics tab — matches isHCAccess gate)
- Coachee tab content: their own **deliverable progress** — current Proton track, completed/remaining deliverables
- **Stat cards** for Coachees: deliverables completed, current status, competency level
- Assessment results stay in Training Records — no assessment summary on the Dashboard for Coachees

### Navigation
- Dashboard nav link label: **"Dashboard"** — same for all roles (no role-specific labels)
- Nav link must become accessible to Coachees in Phase 12 (previously blocked)

### Claude's Discretion
- Server-side vs client-side enforcement of Analytics tab gate
- Whether UserAssessmentHistory.cshtml is kept as a drill-down from Analytics tab or removed entirely
- Exact column set and sort order for the coachee table
- Chart placement within the Proton Progress tab (above/below stat cards/table)

</decisions>

<specifics>
## Specific Ideas

- "Remove entirely / fully retired" — user's explicit intent for both HC Reports and Dev Dashboard; no redirects, no half-measures
- isHCAccess pattern must be applied consistently (established in Phases 10-11)
- Pre-implementation: grep for "ReportsIndex" across all .cshtml files before deletion to catch any untracked references

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 12-dashboard-consolidation*
*Context gathered: 2026-02-19*
