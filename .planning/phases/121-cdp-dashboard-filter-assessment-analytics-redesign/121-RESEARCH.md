# Phase 121: CDP Dashboard Filter & Assessment Analytics Redesign - Research

**Researched:** 2026-03-08
**Domain:** ASP.NET Core MVC AJAX cascade filters, Razor partial rendering, Chart.js
**Confidence:** HIGH

## Summary

This phase transforms the CDP Dashboard from a static server-rendered page into an AJAX-driven dashboard with cascade filters on both tabs (Coaching Proton and Assessment Analytics). The Coaching Proton tab currently has zero filters -- it simply shows data scoped by the user's role. The Assessment Analytics tab has filters but they use full-page GET form submission and suffer from a bug where filter changes redirect to the wrong tab.

The implementation requires: (1) two new AJAX controller actions that return `PartialView()` results, (2) cascade dropdown logic using `OrganizationStructure.GetUnitsForSection()` which already exists, (3) role-based filter scoping using existing `UserRoles.GetRoleLevel()` and `HasFullAccess()`, (4) JavaScript fetch handlers that replace container innerHTML on filter change, and (5) restructured partials that include filter bars above KPI cards.

**Primary recommendation:** Build two new AJAX endpoints (`FilterCoachingProton` and `FilterAssessmentAnalytics`) on CDPController that return `PartialView()`. Reuse existing `BuildProtonProgressSubModelAsync` and `BuildAnalyticsSubModelAsync` with added filter parameters. Use vanilla JS fetch (already the project pattern) for cascade and content refresh.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Coaching Proton: 4 filters in order Section -> Unit -> Category -> Track (cascade)
- Assessment Analytics: 3 filters in order Section -> Unit -> Category (cascade), remove StartDate/EndDate/UserSearch
- No Apply button on either tab -- auto-refresh on every dropdown change
- Filter bar placed ABOVE KPI cards on both tabs
- Table columns stay fixed regardless of filter selection
- Remove Apply Filters button from Assessment Analytics, keep Clear button
- Keep AJAX-based pagination on Assessment Analytics
- Default: show all data when no filters selected
- Excel export respects active filters
- Assessment Analytics tab visibility remains HC/Admin only
- Level 1-3 (HasFullAccess): "Semua Section" + all categories, default to showing all
- Level 4 (Section Head, Sr Supervisor): Section pre-filled & locked, can filter Unit/Category/Track within
- Level 5 (Coach, Supervisor): Section + Unit pre-filled & locked, can filter Category/Track within
- Single AJAX endpoint per tab returning partial HTML (server-rendered Razor partials)
- Loading state: fade content to 50% opacity + centered spinner overlay
- Fix bug where Assessment filter change redirects to Coaching Proton tab
- Both tabs follow identical layout: Filter row -> KPI cards -> Charts -> Table
- Identical filter bar position and table styling between tabs

### Claude's Discretion
- Chart type selection for Assessment Analytics (keep bar charts or switch)
- Exact AJAX endpoint naming and parameter design
- Spinner/overlay CSS implementation details
- How cascade dropdown options are populated (from filtered data queries)

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 | Server-side rendering + AJAX partial views | Project framework |
| Chart.js | CDN (already loaded) | Dashboard charts (line, doughnut, bar) | Already used in both partials |
| Bootstrap 5 | CDN (already loaded) | UI components, grid, form controls | Project standard |
| Bootstrap Icons | CDN (already loaded) | Icon library | Project standard |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| OrganizationStructure | static class | Section->Unit mapping for cascade | Filter dropdown population |
| UserRoles | static class | Role level detection, access scoping | Filter visibility/locking |

## Architecture Patterns

### Recommended Project Structure
```
Controllers/
  CDPController.cs          # Add 2 new AJAX actions + 1 cascade helper endpoint
Models/
  CDPDashboardViewModel.cs  # Add filter state properties to sub-models
Views/CDP/
  Dashboard.cshtml          # Add JS fetch handlers, filter containers, loading overlay
  Shared/
    _CoachingProtonPartial.cshtml    # Add filter bar, wrap content in replaceable container
    _AssessmentAnalyticsPartial.cshtml # Redesign: remove old filters, add cascade bar, align layout
```

### Pattern 1: AJAX Partial View Return
**What:** Controller action returns `PartialView()` instead of `View()`, JS replaces container innerHTML.
**When to use:** For AJAX content refresh without full page reload.
**Example:**
```csharp
[HttpGet]
public async Task<IActionResult> FilterCoachingProton(
    string? section, string? unit, string? category, string? track)
{
    var user = await _userManager.GetUserAsync(User);
    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault() ?? "";

    // Apply role-based scoping + filter params
    var model = await BuildProtonProgressSubModelAsync(user, userRole, section, unit, category, track);
    return PartialView("Shared/_CoachingProtonPartial", model);
}
```

### Pattern 2: Cascade Filter via JSON Endpoint
**What:** Small endpoint returns dropdown options as JSON for cascade population.
**When to use:** When child dropdown needs to update based on parent selection.
**Example:**
```csharp
[HttpGet]
public IActionResult GetCascadeOptions(string? section)
{
    var units = string.IsNullOrEmpty(section)
        ? new List<string>()
        : OrganizationStructure.GetUnitsForSection(section);
    return Json(new { units });
}
```

### Pattern 3: Role-Based Filter Locking
**What:** Server sends role level info; JS pre-fills and disables locked dropdowns.
**When to use:** Level 4 users get Section locked; Level 5 users get Section + Unit locked.
**Example:**
```javascript
// Embed role data in the page (set by Dashboard action)
var filterConfig = {
    roleLevel: @roleLevel,
    lockedSection: '@lockedSection',
    lockedUnit: '@lockedUnit'
};
// On init: pre-fill locked filters and set disabled attribute
if (filterConfig.roleLevel >= 4) {
    sectionSelect.value = filterConfig.lockedSection;
    sectionSelect.disabled = true;
}
```

### Anti-Patterns to Avoid
- **Full page form GET for filter changes:** Current Assessment Analytics pattern -- causes tab-switching bug and poor UX. Replace with fetch().
- **Separate JS files for simple filter logic:** Keep inline in Dashboard.cshtml (project convention, both partials already have inline `<script>` blocks).
- **Rebuilding Chart.js instances without destroying old ones:** Must call `chart.destroy()` before creating new chart on same canvas ID, or use unique IDs per render.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Section->Unit mapping | Custom dictionary | `OrganizationStructure.GetUnitsForSection()` | Already maintained, 4 sections with units |
| Role level detection | Switch statement on role names | `UserRoles.GetRoleLevel(userRole)` | Already exists, tested |
| Full access check | Manual role comparisons | `UserRoles.HasFullAccess(level)` | Already exists (level <= 3) |
| Section access check | Custom logic | `UserRoles.HasSectionAccess(level)` | Already exists (level == 4) |

## Common Pitfalls

### Pitfall 1: Chart.js Canvas Reuse
**What goes wrong:** After AJAX partial replacement, Chart.js tries to use a canvas that was in a destroyed DOM. Or if partial is re-rendered, old chart instance is not destroyed.
**Why it happens:** Chart.js registers charts against canvas elements. When innerHTML is replaced, old references are gone but Chart.js internal registry is not cleaned.
**How to avoid:** Charts are created inside the partial's `<script>` block. Since innerHTML replacement destroys the old DOM and creates new canvas elements, Chart.js will create fresh charts. However, if any chart variable is stored globally, it will leak. The current partials use local variables inside DOMContentLoaded -- but after AJAX replace, DOMContentLoaded won't fire again.
**Solution:** After fetching partial HTML and setting innerHTML, manually execute the script tags in the response, OR move chart initialization to a named function called after innerHTML replacement. Best approach: use `eval()` on script content extracted from the partial response, or better, use a MutationObserver pattern. Simplest: extract scripts from response HTML, evaluate them after insertion.

### Pitfall 2: Tab-Switching Bug on Assessment Filter
**What goes wrong:** Currently, Assessment Analytics uses `<form method="get">` which navigates to `/CDP/Dashboard?analyticsCategory=...`. The page loads with Coaching Proton tab active by default. The URL params trigger tab activation JS, but there's a race condition or the JS doesn't fire correctly.
**How to avoid:** Replace the form GET with AJAX fetch. The tab stays active because there's no page navigation.

### Pitfall 3: Cascade Filter Race Conditions
**What goes wrong:** User rapidly changes Section dropdown, multiple AJAX requests fire, responses arrive out of order.
**How to avoid:** Use an AbortController pattern -- abort previous fetch when new filter change occurs. Or use a simple request counter and ignore stale responses.

### Pitfall 4: Locked Filter Bypass
**What goes wrong:** Level 4/5 users have disabled dropdowns client-side, but could manipulate the request to filter other sections.
**How to avoid:** Server-side enforcement in the AJAX endpoint. Always override section/unit based on user's role level regardless of what the request sends.

### Pitfall 5: Category and Track Filter Source for Coaching Proton
**What goes wrong:** "Category" for Coaching Proton is unclear -- it could mean assessment category or a different concept.
**How to avoid:** Based on the assessment analytics context, "Category" likely refers to assessment/competency categories (e.g., from AssessmentSession.Category or the competency grouping). "Track" refers to ProtonTrack (Operator/Panelman, Tahun 1/2/3). Verify by checking what categories exist in the data. The cascade filter should derive available categories from data filtered by section+unit, and tracks similarly.

## Code Examples

### Current Dashboard Action Signature (to be extended)
```csharp
// Source: CDPController.cs line 234
public async Task<IActionResult> Dashboard(
    string? analyticsCategory, DateTime? analyticsStartDate, DateTime? analyticsEndDate,
    string? analyticsSection, string? analyticsUserSearch,
    int analyticsPage = 1, int analyticsPageSize = 20)
```

### Existing Role Scoping in BuildProtonProgressSubModelAsync
```csharp
// Source: CDPController.cs lines 326-361
if (userRole == UserRoles.HC || userRole == UserRoles.Admin)
    // All coachees
else if (UserRoles.HasSectionAccess(UserRoles.GetRoleLevel(userRole)))
    // Section-scoped coachees (u.Section == user.Section)
else // Coach
    // Unit-scoped coachees (u.Unit == user.Unit)
```

### Existing Organization Structure Helper
```csharp
// Source: Models/OrganizationStructure.cs
OrganizationStructure.GetAllSections();           // ["RFCC", "DHT / HMU", "NGP", "GAST"]
OrganizationStructure.GetUnitsForSection("RFCC");  // ["RFCC LPG Treating Unit (062)", ...]
OrganizationStructure.GetSectionForUnit(unit);     // reverse lookup
```

### JavaScript Fetch Pattern (existing in project)
```javascript
// Source: _AssessmentAnalyticsPartial.cshtml line 389
fetch('/CDP/SearchUsers?term=' + encodeURIComponent(term))
    .then(function(r) { return r.json(); })
    .then(function(data) { /* handle */ });
```

### Loading Overlay CSS
```css
.dashboard-loading {
    position: relative;
}
.dashboard-loading::after {
    content: '';
    position: absolute;
    inset: 0;
    background: rgba(255,255,255,0.5);
    z-index: 10;
    display: flex;
    align-items: center;
    justify-content: center;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Full-page GET form filters (Assessment) | AJAX partial replacement | This phase | No page reload, no tab-switch bug |
| No filters on Coaching Proton | Cascade Section->Unit->Category->Track | This phase | Users can drill down into specific scopes |
| HC/Admin-only dashboard scoping | Role-based filter visibility (levels 1-5) | This phase | All supervisor roles get filtered views |

## Open Questions

1. **What is "Category" in Coaching Proton context?**
   - What we know: Assessment Analytics uses `AssessmentSession.Category` (e.g., competency categories). ProtonTrack has `TrackType` (Operator/Panelman) and `TahunKe` (Tahun 1/2/3).
   - What's unclear: The CONTEXT.md specifies "Category" as a separate filter from "Track" in Coaching Proton. This likely refers to the assessment category that coachees are being tracked on, or possibly a user attribute.
   - Recommendation: During implementation, inspect the data model. If Category means the assessment competency categories, derive available options from distinct categories of deliverables/assessments within the filtered section+unit scope.

2. **Partial script execution after AJAX innerHTML replacement**
   - What we know: Both partials have `<script>` blocks that initialize Chart.js. After innerHTML replacement, these scripts won't auto-execute.
   - Recommendation: After setting innerHTML, find all `<script>` tags in the response, create new script elements, and append them. This is a common pattern for AJAX partial rendering.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project convention) |
| Config file | none |
| Quick run command | `dotnet build` |
| Full suite command | Manual UAT per TESTING.md |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| N/A-01 | Coaching Proton cascade filters work | manual | Browser: change Section, verify Unit populates | N/A |
| N/A-02 | Assessment Analytics cascade filters work | manual | Browser: change Section, verify Unit populates | N/A |
| N/A-03 | Role-based filter locking (Level 4) | manual | Login as SectionHead, verify Section locked | N/A |
| N/A-04 | Role-based filter locking (Level 5) | manual | Login as Coach, verify Section+Unit locked | N/A |
| N/A-05 | AJAX refresh updates KPIs, charts, table | manual | Change filter, verify all sections update | N/A |
| N/A-06 | Assessment tab stays active during filter | manual | On analytics tab, change filter, verify tab stays | N/A |
| N/A-07 | Excel export respects active filters | manual | Set filter, click export, verify filtered data | N/A |
| N/A-08 | Clear button resets all filters | manual | Set filters, click Clear, verify reset to defaults | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Per wave merge:** Manual browser testing on all test cases
- **Phase gate:** All 8 test cases pass in browser

### Wave 0 Gaps
None -- project uses manual browser testing exclusively.

## Sources

### Primary (HIGH confidence)
- CDPController.cs lines 234-609 -- existing Dashboard action, BuildProtonProgressSubModelAsync, BuildAnalyticsSubModelAsync
- Models/CDPDashboardViewModel.cs -- full view model structure
- Views/CDP/Shared/_CoachingProtonPartial.cshtml -- current Coaching Proton layout (no filters)
- Views/CDP/Shared/_AssessmentAnalyticsPartial.cshtml -- current Assessment Analytics layout (GET form filters)
- Models/OrganizationStructure.cs -- Section->Unit cascade data source
- Models/UserRoles.cs -- role level helpers (GetRoleLevel, HasFullAccess, HasSectionAccess)

### Secondary (MEDIUM confidence)
- 121-CONTEXT.md -- user decisions on filter design and role scoping

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all libraries already in use, no new dependencies
- Architecture: HIGH - extends existing patterns (partials, role scoping, org structure)
- Pitfalls: HIGH - Chart.js reuse and tab-switch bug are well-understood from existing code

**Research date:** 2026-03-08
**Valid until:** 2026-04-08 (stable, internal project)
