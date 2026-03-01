# Phase 76: Role Fixes & Broken Link - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix three targeted issues: (1) HC users see cards on the Admin hub they can't access — hide cards based on role, (2) "Kelola Data" navbar visibility uses SelectedView session field instead of Identity role — switch to role-based gating, (3) "Deliverable Progress Override" card navigates to ProtonData but doesn't activate the correct Bootstrap tab. No new features — these are corrections to existing authorization and navigation behavior.

</domain>

<decisions>
## Implementation Decisions

### HC card visibility on Admin hub
- Hide 4 cards from HC users: KKJ Matrix, KKJ-IDP Mapping, Coach-Coachee Mapping, and Manage Assessments
- HC users should see only cards they can act on: Manajemen Pekerja and Deliverable Progress Override
- Use role-based conditional rendering (check Identity role, not session state)

### Kelola Data navbar gating
- "Kelola Data" should be visible to both Admin and HC users
- Switch from SelectedView session field to Identity role check
- Matches the ManageWorkers authorization pattern (Admin + HC)

### Tab link fix for Deliverable Progress Override
- Use query parameter approach: navigate to ProtonData?tab=deliverable-override
- Controller reads the tab parameter and passes it to the view
- View activates the specified Bootstrap tab on page load

### Claude's Discretion
- Exact conditional rendering syntax (Razor @if vs tag helpers)
- How to read the tab query parameter in the controller and pass to view (ViewBag, ViewData, or model property)
- Whether to add a fallback if the tab parameter is invalid or missing (default to first tab)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — straightforward bug fixes for role-gating and tab activation.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 76-role-fixes-broken-link*
*Context gathered: 2026-03-01*
