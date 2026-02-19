# Phase 12: Dashboard Consolidation - Research

**Researched:** 2026-02-19
**Domain:** ASP.NET Core MVC — ViewModel consolidation, role-gated tab layout, partial views, controller data aggregation, nav link removal
**Confidence:** HIGH (all findings from direct codebase inspection)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Assessment Analytics tab — role visibility
- Follows **isHCAccess pattern**: visible to Admin and HC regardless of SelectedView simulation
- Admin simulating Coachee/Atasan/Coach still sees the Analytics tab — SelectedView is ignored for this gate
- HC view simulation (Admin with SelectedView = HC) also sees the Analytics tab
- Server vs client-side enforcement of the tab gate: **Claude's discretion**

#### HC Reports retirement
- CMPController.ReportsIndex() action, its view, and its nav link are **deleted entirely** in Plan 12-03
- No redirect — the route is fully retired, not preserved
- Url.Action("ReportsIndex") calls in UserAssessmentHistory.cshtml: **remove the links entirely** (no replacement link)
- HC Reports nav link in _Layout.cshtml removed in the **same plan as page deletion (12-03)**
- Excel export button stays on the Analytics tab at the **top level** (not only accessible from drill-down)
- UserAssessmentHistory.cshtml disposition (whether to keep as drill-down from Analytics or remove): **Claude's discretion**

#### Dev Dashboard retirement
- CDPController.DevDashboard() action, DevDashboard.cshtml, and its nav link are **deleted entirely** in Plan 12-03
- Both retirements (HC Reports + Dev Dashboard) happen **together in Plan 12-03**

#### Proton Progress tab — layout
- **Flat table** of all coachees, sorted by name or section — no section grouping headers
- **Stat cards at top**, then table (e.g. total coachees, pending approvals, completion rate)
- **Chart.js trend charts preserved** — competency level changes over time from former Dev Dashboard
- **No approval queue on the tab** — approvals are actioned from individual Deliverable pages, not from Dashboard

#### Coachee access — new in Phase 12
- Coachees were blocked from Dev Dashboard; Phase 12 opens Dashboard to all roles
- Coachee sees Proton Progress tab only (no Analytics tab — matches isHCAccess gate)
- Coachee tab content: their own **deliverable progress** — current Proton track, completed/remaining deliverables
- **Stat cards** for Coachees: deliverables completed, current status, competency level
- Assessment results stay in Training Records — no assessment summary on the Dashboard for Coachees

#### Navigation
- Dashboard nav link label: **"Dashboard"** — same for all roles (no role-specific labels)
- Nav link must become accessible to Coachees in Phase 12 (previously blocked)

### Claude's Discretion
- Server-side vs client-side enforcement of Analytics tab gate
- Whether UserAssessmentHistory.cshtml is kept as a drill-down from Analytics tab or removed entirely
- Exact column set and sort order for the coachee table
- Chart placement within the Proton Progress tab (above/below stat cards/table)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

---

## Summary

Phase 12 consolidates two standalone pages — CDPController.DevDashboard() and CMPController.ReportsIndex() — into the existing CDP Dashboard as two role-scoped tabs. The phase also opens Dashboard access to Coachees for the first time. Three work tracks are involved: (1) a new CDPDashboardViewModel that carries sub-models for all three role audiences, (2) a rewritten Dashboard.cshtml with the two-tab layout and partial view includes, and (3) a cleanup plan that deletes both standalone pages and corrects every cross-controller reference.

The codebase is already well-structured for this work. The isHCAccess pattern is established in CDPController (lines 1025-1027, 1059-1061, 1155-1157, 1219-1221) and is the correct gate to use for the Analytics tab. DevDashboard's scoping logic (DASH-02: HC/Admin sees all, SrSpv/SectionHead sees section, Coach sees unit) is complete and only needs to move into the consolidated controller method. ReportsIndex's data query is also complete and only needs to be re-invoked from Dashboard().

The primary risk is the large data aggregation in Dashboard() — the method must conditionally load three different data sets without loading unnecessary data for roles that don't need them. Use early-exit sub-model population keyed on role, not a single monolithic query.

**Primary recommendation:** Build CDPDashboardViewModel as a wrapper with three nullable sub-models (CoacheeSubModel, ProtonProgressSubModel, AssessmentAnalyticsSubModel) and populate only the sub-model(s) the requesting role requires. Dashboard.cshtml renders tabs from what is non-null.

---

## Standard Stack

### Core (already in project — no new installs)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | (project's current) | Controller/ViewModel/View pattern | Project stack |
| ClosedXML | (already in CMPController) | Excel export in ExportResults action | Already working, same action moves to Dashboard |
| Chart.js | CDN (cdn.jsdelivr.net) | Line + doughnut charts from DevDashboard | Already used in DevDashboard.cshtml |
| Bootstrap 5.3 | CDN | Tab layout (`nav-tabs`, `tab-content`) | Already in _Layout.cshtml |
| Bootstrap Icons 1.10 | CDN | Icons on stat cards and tab headers | Already in _Layout.cshtml |

### No New Dependencies
This phase is pure refactoring + consolidation. No new packages are needed. The existing ClosedXML, Chart.js, Bootstrap, and ASP.NET Core MVC infrastructure covers all requirements.

---

## Architecture Patterns

### Recommended Project Structure Changes

```
Controllers/
├── CDPController.cs          # Dashboard() method fully rewritten; DevDashboard() deleted
└── CMPController.cs          # ReportsIndex(), ExportResults(), UserAssessmentHistory() deleted (Plan 12-03)

Models/
├── CDPDashboardViewModel.cs  # NEW: wrapper ViewModel with sub-models (Plan 12-01)
├── DevDashboardViewModel.cs  # DELETED in Plan 12-03 (or keep if reused elsewhere — check)
└── DashboardViewModel.cs     # DELETED — superseded by CDPDashboardViewModel

Views/
├── CDP/
│   ├── Dashboard.cshtml      # REWRITTEN: two-tab layout (Plan 12-02)
│   ├── DevDashboard.cshtml   # DELETED (Plan 12-03)
│   └── Shared/
│       ├── _ProtonProgressPartial.cshtml     # NEW partial view (Plan 12-02)
│       └── _AssessmentAnalyticsPartial.cshtml # NEW partial view (Plan 12-02)
├── CMP/
│   ├── ReportsIndex.cshtml   # DELETED (Plan 12-03)
│   └── UserAssessmentHistory.cshtml  # DELETED or kept per Claude discretion
└── Shared/
    └── _Layout.cshtml        # Nav changes: remove DevDashboard link, add/update Dashboard link
```

### Pattern 1: Wrapper ViewModel with Nullable Sub-Models

**What:** CDPDashboardViewModel wraps three optional sub-models. Controller populates only what the requesting role needs. View renders tabs based on what is non-null.

**When to use:** Whenever a single action must serve multiple role audiences with substantially different data sets.

**Example (CDPDashboardViewModel.cs):**
```csharp
// Source: derived from existing DevDashboardViewModel.cs pattern in this project
namespace HcPortal.Models
{
    public class CDPDashboardViewModel
    {
        // Populated for all roles
        public string CurrentUserRole { get; set; } = "";
        public string ScopeLabel { get; set; } = "";

        // Populated for Coachee only (RoleLevel == 6, not simulated)
        public CoacheeDashboardSubModel? CoacheeData { get; set; }

        // Populated for HC/Admin and non-Coachee supervisor roles
        public ProtonProgressSubModel? ProtonProgressData { get; set; }

        // Populated for HC/Admin only (isHCAccess gate)
        public AssessmentAnalyticsSubModel? AssessmentAnalyticsData { get; set; }
    }

    public class CoacheeDashboardSubModel
    {
        public string TrackType { get; set; } = "";
        public string TahunKe { get; set; } = "";
        public int TotalDeliverables { get; set; }
        public int ApprovedDeliverables { get; set; }
        public int ActiveDeliverables { get; set; }
        public int? CompetencyLevelGranted { get; set; }  // from ProtonFinalAssessment
        public string CurrentStatus { get; set; } = "";   // "In Progress" / "Completed"
    }

    public class ProtonProgressSubModel
    {
        // Cards
        public int TotalCoachees { get; set; }
        public int TotalDeliverables { get; set; }
        public int ApprovedDeliverables { get; set; }
        public int PendingSpvApprovals { get; set; }
        public int PendingHCReviews { get; set; }
        public int CompletedCoachees { get; set; }

        // Flat table rows (sorted by name)
        public List<CoacheeProgressRow> CoacheeRows { get; set; } = new();

        // Chart data (from DevDashboard — unchanged)
        public List<string> TrendLabels { get; set; } = new();
        public List<double> TrendValues { get; set; } = new();
        public List<string> StatusLabels { get; set; } = new();
        public List<int> StatusData { get; set; } = new();
    }

    public class AssessmentAnalyticsSubModel
    {
        // Cards (from ReportsDashboardViewModel)
        public int TotalAssigned { get; set; }
        public int TotalCompleted { get; set; }
        public int PassedCount { get; set; }
        public double PassRate { get; set; }
        public double AverageScore { get; set; }

        // Paginated table
        public List<AssessmentReportItem> Assessments { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }

        // Filters
        public ReportFilters CurrentFilters { get; set; } = new();
        public List<string> AvailableCategories { get; set; } = new();
        public List<string> AvailableSections { get; set; } = new();

        // Chart data (from ReportsIndex via ViewBag — move into sub-model)
        public List<CategoryStatistic> CategoryStats { get; set; } = new();
        public List<int> ScoreDistribution { get; set; } = new();
    }
}
```

### Pattern 2: isHCAccess Gate — Server-Side Tab Absence

**What:** The Analytics tab must be completely absent from the DOM for non-HC/non-Admin users. The established pattern in this project is `bool isHCAccess = userRole == UserRoles.HC || (userRole == UserRoles.Admin && user.SelectedView == "HC")`.

**Decision:** Use server-side enforcement (AssessmentAnalyticsSubModel is null for non-HC/Admin). The view checks `Model.AssessmentAnalyticsData != null` to decide whether to render the tab and its panel. This is consistent with how isHCAccess already works in CDPController for HCApprovals, CreateFinalAssessment, HCReviewDeliverable.

**Critical nuance from CONTEXT.md:** The Analytics tab gate ignores SelectedView for Admin. Admin simulating Coachee still sees the Analytics tab. The correct check is:

```csharp
// Source: isHCAccess pattern from CDPController.cs lines 1025-1027
// IMPORTANT: This gate ignores SelectedView — it's based on role identity, not simulation
bool isHCAccess = userRole == UserRoles.HC || userRole == UserRoles.Admin;
// NOT: userRole == UserRoles.Admin && user.SelectedView == "HC"
// Admin always gets Analytics tab regardless of what view they're simulating
```

This is a departure from the existing isHCAccess pattern elsewhere in CDPController (which gates on `user.SelectedView == "HC"` for Admin). For the Analytics tab specifically, the Context.md decision is explicit: SelectedView is ignored.

### Pattern 3: Scoped Coachee Data Population (from DevDashboard)

**What:** The ProtonProgress sub-model scope varies by role. The existing DevDashboard() method already implements this correctly (lines 247-281 in CDPController.cs).

**How to apply:**
```csharp
// Source: CDPController.cs DevDashboard() lines 247-281 — copy exactly into Dashboard()
List<string> scopedCoacheeIds;
string scopeLabel;

if (userRole == UserRoles.HC || userRole == UserRoles.Admin)
{
    scopedCoacheeIds = await _context.Users
        .Where(u => u.RoleLevel == 6).Select(u => u.Id).ToListAsync();
    scopeLabel = "All Sections";
}
else if (userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead)
{
    scopedCoacheeIds = await _context.Users
        .Where(u => u.Section == user.Section && u.RoleLevel == 6)
        .Select(u => u.Id).ToListAsync();
    scopeLabel = $"Section: {user.Section ?? "(unknown)"}";
}
else // Coach
{
    // Null-guard: fall back to Section if Unit is unset
    if (!string.IsNullOrEmpty(user.Unit))
    {
        scopedCoacheeIds = await _context.Users
            .Where(u => u.Unit == user.Unit && u.RoleLevel == 6)
            .Select(u => u.Id).ToListAsync();
        scopeLabel = $"Unit: {user.Unit}";
    }
    else
    {
        scopedCoacheeIds = await _context.Users
            .Where(u => u.Section == user.Section && u.RoleLevel == 6)
            .Select(u => u.Id).ToListAsync();
        scopeLabel = $"Section: {user.Section ?? "(unknown)"} (Unit not set)";
    }
}
```

### Pattern 4: Bootstrap 5 Tab Layout

**What:** Bootstrap 5 nav-tabs + tab-content + tab-pane to render two tabs in Dashboard.cshtml.

```html
<!-- Source: Bootstrap 5 docs — already used in Assessment.cshtml (Phase 11) -->
<ul class="nav nav-tabs mb-4" id="dashboardTabs" role="tablist">
    <li class="nav-item" role="presentation">
        <button class="nav-link active" id="proton-tab" data-bs-toggle="tab"
                data-bs-target="#proton-pane" type="button" role="tab">
            <i class="bi bi-graph-up me-1"></i>Proton Progress
        </button>
    </li>
    @if (Model.AssessmentAnalyticsData != null)
    {
        <li class="nav-item" role="presentation">
            <button class="nav-link" id="analytics-tab" data-bs-toggle="tab"
                    data-bs-target="#analytics-pane" type="button" role="tab">
                <i class="bi bi-clipboard-data me-1"></i>Assessment Analytics
            </button>
        </li>
    }
</ul>

<div class="tab-content">
    <div class="tab-pane fade show active" id="proton-pane" role="tabpanel">
        @if (Model.CoacheeData != null)
        {
            <partial name="_CoacheeDashboardPartial" model="Model.CoacheeData" />
        }
        else if (Model.ProtonProgressData != null)
        {
            <partial name="_ProtonProgressPartial" model="Model.ProtonProgressData" />
        }
    </div>
    @if (Model.AssessmentAnalyticsData != null)
    {
        <div class="tab-pane fade" id="analytics-pane" role="tabpanel">
            <partial name="_AssessmentAnalyticsPartial" model="Model.AssessmentAnalyticsData" />
        </div>
    }
</div>
```

### Pattern 5: Analytics Tab Filter — Form Action Must Point to Dashboard

**What:** ReportsIndex.cshtml uses `asp-action="ReportsIndex"` in its filter form and Clear link. When content moves to Dashboard.cshtml as a partial, form actions and links must be updated to `asp-controller="CDP" asp-action="Dashboard"`.

The ExportResults action on CMPController also needs to move to CDPController or be called cross-controller. Since the action is being retained on the Analytics tab, and ReportsIndex is being deleted entirely, ExportResults() must also move to CDPController.Dashboard-adjacent action (or move to CDPController.ExportResults).

### Anti-Patterns to Avoid
- **Loading all three sub-models unconditionally:** The Coachee data path, Proton Progress path, and Analytics path each hit different tables with different query volumes. Populate only what the role needs.
- **Hardcoded role checks in partial views:** Keep role gates in the controller (sub-model nullability), not in `@if (User.IsInRole(...))` scattered in partials.
- **Forgetting pagination query strings in Analytics tab:** The filter form and pagination links in the Analytics partial must pass through all filter parameters. Copy the approach from ReportsIndex.cshtml exactly.
- **Double Chart.js CDN import:** Dashboard.cshtml already uses Chart.js (from the old DashboardViewModel era). DevDashboard also loaded Chart.js. The final view must load Chart.js exactly once.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel export | Custom CSV/byte builder | ClosedXML (already in CMPController.ExportResults) | Already works, just move the action |
| Role tab gating | Custom middleware | Null sub-model check in view (`@if (Model.AssessmentAnalyticsData != null)`) | Simple, server-enforced, no JS needed |
| Coachee scope resolution | New query logic | Copy DevDashboard() scoping code exactly (lines 247-281 CDPController) | Already tested, handles all edge cases including Unit null fallback |
| Partial view rendering | Full view inheritance | `<partial name="..." model="..." />` tag helper | Clean separation, already used in project |

**Key insight:** The majority of the data logic already exists and works. The risk is in the integration, not in building new query logic. Move existing code carefully rather than rewriting.

---

## Common Pitfalls

### Pitfall 1: Analytics Filter Form Pointing to Wrong Action

**What goes wrong:** The Analytics partial view inherits form actions from ReportsIndex.cshtml that use `asp-action="ReportsIndex"`. After the move, filters post to a deleted route, returning 404.

**Why it happens:** Partial views for the Analytics tab are adapted from ReportsIndex.cshtml, and the form action tag helpers are easy to overlook.

**How to avoid:** In `_AssessmentAnalyticsPartial.cshtml`, all form `asp-action` attributes and `Url.Action(...)` calls must be updated to `"Dashboard"` with controller `"CDP"`. The Clear button link (`<a asp-action="ReportsIndex">`) must become `<a asp-controller="CDP" asp-action="Dashboard">`.

**Warning signs:** 404 after clicking Apply Filters or Clear in the Analytics tab.

### Pitfall 2: ExportResults Still on CMPController After ReportsIndex Deletion

**What goes wrong:** The Export to Excel button in the Analytics partial links to `CMP/ExportResults`. When CMPController.ExportResults() is deleted along with ReportsIndex, the button 404s.

**Why it happens:** ExportResults() is a separate action from ReportsIndex() — it's easy to leave it in CMPController and only delete ReportsIndex().

**How to avoid:** In Plan 12-01 or 12-02, move ExportResults() to CDPController. In Plan 12-03, delete it from CMPController. Update the partial's export button link to point to `CDP/ExportResults`.

**Warning signs:** Export button returns 404 after Plan 12-03.

### Pitfall 3: SearchUsers Endpoint Still on CMPController

**What goes wrong:** The Analytics partial's user search autocomplete calls `CMP/SearchUsers` via fetch(). When CMPController is cleaned up, this JSON endpoint may be deleted or inaccessible.

**Why it happens:** SearchUsers() is a small helper action that's easy to overlook — it has no corresponding view.

**How to avoid:** Check whether SearchUsers() is used only by ReportsIndex. If yes, move it to CDPController in Plan 12-01. If it's used elsewhere in CMP views, leave it on CMPController and cross-call it from the partial.

**Warning signs:** User search autocomplete silently fails in the Analytics tab.

### Pitfall 4: Coachee Access Not Gated at Controller Level

**What goes wrong:** The old Dashboard() had no explicit Coachee blocking (unlike DevDashboard which had `if (user.RoleLevel >= 6 && userRole != UserRoles.Admin && userRole != UserRoles.HC) return Forbid()`). The new Dashboard() must actively admit Coachees and populate CoacheeData for them — not accidentally pass them into the ProtonProgress path.

**Why it happens:** The controller rewrite starts with the existing Dashboard() method, which had no explicit Coachee handling. Developers add Proton Progress logic without adding an early-branch for Coachees.

**How to avoid:** In Dashboard(), the first branch in the role dispatch must be the Coachee case:
```csharp
bool isCoachee = userRole == UserRoles.Coachee;
// Admin simulating Coachee is NOT treated as Coachee for Dashboard purposes
// (Admin always gets Analytics tab — see Context.md)
if (isCoachee)
{
    // populate CoacheeData only
    model.CoacheeData = await BuildCoacheeSubModel(user.Id);
    return View(model);
}
// else: populate ProtonProgressData (and possibly AssessmentAnalyticsData)
```

**Warning signs:** Coachees see the team coachee table (ProtonProgress) instead of their own deliverable summary.

### Pitfall 5: Missing Dashboard Nav Link for Coachees in _Layout.cshtml

**What goes wrong:** The current nav in `_Layout.cshtml` (lines 63-74) shows the DevDashboard link only for Coach and above. Dashboard has no separate nav link — it's never shown in the nav. After Phase 12, Dashboard must have a nav link visible to ALL roles (including Coachee).

**Why it happens:** There is currently no "Dashboard" nav link. Plan 12-02 or 12-03 must add it. It's easy to only remove the DevDashboard link and forget to add/update the Dashboard link.

**How to avoid:** In Plan 12-03, the nav change is:
1. Remove the `DevDashboard` `<li>` block (lines 69-74 in _Layout.cshtml)
2. Add a new `<li>` for `CDP/Dashboard` with no role gate (all authenticated users see it)

**Warning signs:** Coachees have no nav link to Dashboard after Phase 12.

### Pitfall 6: CMP/Index.cshtml "HC Reports" Card Still Links to ReportsIndex

**What goes wrong:** `Views/CMP/Index.cshtml` line 116 has `@Url.Action("ReportsIndex", "CMP")`. After Plan 12-03 deletes ReportsIndex, this card produces a 404.

**Why it happens:** The pre-implementation grep check (specified in CONTEXT.md Specifics) is the catch for this. If it's missed, this is a live 404 for HC/Admin users on the CMP index page.

**How to avoid:** Grep for "ReportsIndex" before deletion (per CONTEXT.md specific). In Plan 12-03, either:
- Remove the HC Reports card from CMP/Index.cshtml entirely (clean)
- Update the link to point to the Analytics tab on CDP Dashboard

**Warning signs:** 404 on the HC Reports card in CMP/Index.

### Pitfall 7: UserAssessmentHistory Still Linked from Analytics Partial

**What goes wrong:** If UserAssessmentHistory.cshtml is kept as a drill-down from the Analytics tab, its breadcrumb links (`Url.Action("ReportsIndex")`) break immediately. If it is deleted, any remaining links to it also break.

**Why it happens:** UserAssessmentHistory.cshtml currently links back to ReportsIndex in two places (line 11 breadcrumb, line 201 back button).

**How to avoid (Claude's Discretion — recommendation):** Keep UserAssessmentHistory.cshtml as a drill-down; it's a useful per-user detail view with no equivalent elsewhere. Update both `Url.Action("ReportsIndex")` calls to point to `Url.Action("Dashboard", "CDP")` in Plan 12-03. The action on CMPController stays intact (not deleted) since it doesn't depend on ReportsIndex.

**Warning signs:** Broken breadcrumb on UserAssessmentHistory page; 404 if action was accidentally deleted with ReportsIndex.

---

## Code Examples

### Dashboard() Controller Skeleton (Plan 12-01)

```csharp
// Source: CDPController.cs — rewrite of existing Dashboard() at line 131
public async Task<IActionResult> Dashboard(
    string? analyticsCategory = null,
    DateTime? analyticsStartDate = null,
    DateTime? analyticsEndDate = null,
    string? analyticsSection = null,
    string? analyticsUserSearch = null,
    int analyticsPage = 1,
    int analyticsPageSize = 20)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault() ?? "";

    var model = new CDPDashboardViewModel
    {
        CurrentUserRole = userRole
    };

    // === COACHEE BRANCH (literal Coachee role only) ===
    bool isLiteralCoachee = userRole == UserRoles.Coachee;
    if (isLiteralCoachee)
    {
        model.CoacheeData = await BuildCoacheeSubModelAsync(user.Id);
        return View(model);
    }

    // === PROTON PROGRESS: all non-Coachee roles ===
    model.ProtonProgressData = await BuildProtonProgressSubModelAsync(user, userRole);

    // === ANALYTICS: HC/Admin regardless of SelectedView ===
    bool isHCAccess = userRole == UserRoles.HC || userRole == UserRoles.Admin;
    if (isHCAccess)
    {
        model.AssessmentAnalyticsData = await BuildAnalyticsSubModelAsync(
            analyticsCategory, analyticsStartDate, analyticsEndDate,
            analyticsSection, analyticsUserSearch, analyticsPage, analyticsPageSize);
    }

    return View(model);
}
```

### isHCAccess for Analytics Tab (ignores SelectedView per CONTEXT.md)

```csharp
// Source: CONTEXT.md locked decision — Analytics tab gate
// Admin simulating Coachee STILL sees Analytics — SelectedView is NOT checked
bool isHCAccess = userRole == UserRoles.HC || userRole == UserRoles.Admin;

// Note: this differs from the existing isHCAccess pattern in CDPController
// (which uses: userRole == UserRoles.HC || (userRole == UserRoles.Admin && user.SelectedView == "HC"))
// The difference is intentional per Phase 12 Context.md
```

### Tab Visibility in Dashboard.cshtml

```html
<!-- Source: Bootstrap 5 tab pattern, same as Assessment.cshtml (Phase 11) -->
<ul class="nav nav-tabs mb-4" id="dashboardTabs" role="tablist">
    <li class="nav-item">
        <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#proton-pane">
            <i class="bi bi-graph-up me-1"></i>Proton Progress
        </button>
    </li>
    @if (Model.AssessmentAnalyticsData != null)
    {
        <li class="nav-item">
            <button class="nav-link" data-bs-toggle="tab" data-bs-target="#analytics-pane">
                <i class="bi bi-clipboard-data me-1"></i>Assessment Analytics
            </button>
        </li>
    }
</ul>
```

### Nav Link Change in _Layout.cshtml (Plan 12-03)

```html
<!-- REMOVE this block (lines 63-74 in current _Layout.cshtml): -->
@if (userRole == UserRoles.Coach || userRole == UserRoles.SrSupervisor || ...)
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-controller="CDP" asp-action="DevDashboard">
            <i class="bi bi-speedometer2 me-1"></i>Dev Dashboard
        </a>
    </li>
}

<!-- ADD this block (no role gate — all authenticated users see Dashboard): -->
<li class="nav-item">
    <a class="nav-link text-dark" asp-controller="CDP" asp-action="Dashboard">
        <i class="bi bi-speedometer2 me-1"></i>Dashboard
    </a>
</li>
```

### ExportResults Move (CDPController)

```csharp
// Source: CMPController.ExportResults() lines 1468-1573 — move verbatim, update action name
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ExportAnalyticsResults(
    string? category, DateTime? startDate, DateTime? endDate,
    string? section, string? userSearch)
{
    // ... exact same query logic as CMPController.ExportResults() ...
    // Only change: action name for route purposes
}
```

---

## Complete Reference: All ReportsIndex / DevDashboard References to Address

This is the full inventory from codebase inspection. Plan 12-03 must address every item.

### Files referencing ReportsIndex
| File | Line | Type | Action in 12-03 |
|------|------|------|-----------------|
| `Views/CDP/Dashboard.cshtml` | 97 | Link to CMP/ReportsIndex | Remove (Dashboard tab replaces it) |
| `Views/CMP/Index.cshtml` | 116 | Link to CMP/ReportsIndex | Remove card or update link to CDP/Dashboard |
| `Views/CMP/ReportsIndex.cshtml` | 85, 143 | Self-references (form action, clear link) | File deleted |
| `Views/CMP/UserAssessmentHistory.cshtml` | 11, 201 | Links back to ReportsIndex | Remove links entirely (per CONTEXT.md) |
| `Controllers/CMPController.cs` | 1294 | ReportsIndex() action | Delete action |
| `Controllers/CMPController.cs` | 1468 | ExportResults() action | Move to CDPController |

### Files referencing DevDashboard
| File | Line | Type | Action in 12-03 |
|------|------|------|-----------------|
| `Views/Shared/_Layout.cshtml` | 70-73 | Nav link to CDP/DevDashboard | Delete; add Dashboard link |
| `Controllers/CDPController.cs` | 231 | DevDashboard() action | Delete action |
| `Views/CDP/DevDashboard.cshtml` | (whole file) | View | Delete file |
| `Models/DevDashboardViewModel.cs` | (whole file) | ViewModel | Check if still referenced; delete if not |

### Additional actions to verify in CMPController cleanup
| Action | Used by | Disposition |
|--------|---------|-------------|
| `SearchUsers()` (line 1447) | ReportsIndex.cshtml autocomplete | Move to CDPController if only used by Analytics; keep on CMPController if used elsewhere |
| `UserAssessmentHistory()` (line 1577) | ReportsIndex.cshtml drill-down links | Keep on CMPController — action has no dependency on ReportsIndex |

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Standalone DevDashboard page | Proton Progress tab in CDP Dashboard | Phase 12 | Coachees now have access; Coaches/Supervisors use same Dashboard URL |
| Standalone ReportsIndex page | Assessment Analytics tab in CDP Dashboard | Phase 12 | HC/Admin access analytics from Dashboard; one URL for all |
| DashboardViewModel (IDP-centric, mostly mock data) | CDPDashboardViewModel (role-scoped sub-models, real data) | Phase 12 | Old DashboardViewModel.cs is fully superseded |
| DevDashboard nav link (visible to Coach+) | Dashboard nav link (visible to all roles) | Phase 12 | Coachees gain nav access |

**Superseded/deleted in this phase:**
- `DashboardViewModel.cs`: Replace with `CDPDashboardViewModel.cs`
- `DevDashboardViewModel.cs`: Content moved into `ProtonProgressSubModel`; file deleted after confirming no other references
- `CDPController.DevDashboard()`: Deleted after ProtonProgress sub-model is verified
- `CMPController.ReportsIndex()`: Deleted after Analytics sub-model is verified

---

## Open Questions

1. **Should DashboardViewModel.cs be deleted or kept?**
   - What we know: The existing `Dashboard()` action populates `DashboardViewModel` with a mix of real data (IDP counts, assessment summaries) and mock data (budget, learning hours chart). The new `CDPDashboardViewModel` replaces it entirely.
   - What's unclear: Whether any Home page or other controller uses `DashboardViewModel` (the `DashboardHomeViewModel` in `Models/DashboardHomeViewModel.cs` is different — that's for the homepage).
   - Recommendation: Grep for `DashboardViewModel` across all .cs and .cshtml files before Plan 12-01 executes deletion. If only used in old `CDPController.Dashboard()`, delete safely.

2. **Where does ExportResults belong after migration?**
   - What we know: `CMPController.ExportResults()` must be accessible from the Analytics tab. `ReportsIndex` is being deleted.
   - What's unclear: Whether any non-Analytics view links to `CMP/ExportResults` directly.
   - Recommendation: Move to `CDPController.ExportAnalyticsResults()` in Plan 12-01. The Analytics partial updates its export button link accordingly. In Plan 12-03, delete from CMPController.

3. **Where does SearchUsers belong after migration?**
   - What we know: `CMPController.SearchUsers()` serves the user autocomplete in ReportsIndex.cshtml.
   - What's unclear: Whether any other CMP view uses this endpoint.
   - Recommendation: Grep for `SearchUsers` in all views. If only used by ReportsIndex autocomplete, move to CDPController in Plan 12-01. If used elsewhere (e.g., CreateAssessment user picker), leave on CMPController.

4. **UserAssessmentHistory: keep or delete?** (Claude's Discretion)
   - What we know: It's a standalone per-user history drill-down view. Currently linked from ReportsIndex. The action `CMPController.UserAssessmentHistory()` has no dependency on ReportsIndex — it's self-contained.
   - Recommendation: Keep the view and action. It provides useful per-user detail that has no equivalent in Training Records. Update both `Url.Action("ReportsIndex")` references to `Url.Action("Dashboard", "CDP")`. The Analytics table's row actions (link to UserAssessmentHistory) remain intact.

---

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection — `CDPController.cs` (full file read)
- Direct codebase inspection — `CMPController.cs` (full file read, lines 1291-1628)
- Direct codebase inspection — `DevDashboard.cshtml`, `Dashboard.cshtml`, `ReportsIndex.cshtml`, `UserAssessmentHistory.cshtml`
- Direct codebase inspection — `_Layout.cshtml` (navigation structure)
- Direct codebase inspection — `DashboardViewModel.cs`, `DevDashboardViewModel.cs`, `ReportsDashboardViewModel.cs`, `ApplicationUser.cs`, `UserRoles.cs`, `ProtonModels.cs`
- Direct codebase inspection — `Views/CMP/Index.cshtml` (HC Reports card reference)

### Secondary (MEDIUM confidence)
- Bootstrap 5 tab documentation — tab/pane pattern confirmed used in Assessment.cshtml (Phase 11, established pattern in project)

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; all tools already in project
- Architecture (ViewModel design): HIGH — derived directly from existing model files and confirmed data shapes
- Reference inventory (files to change): HIGH — grep-verified across all .cshtml and .cs files
- Pitfalls: HIGH — derived from direct code reading (form actions, nav links, cross-controller references)

**Research date:** 2026-02-19
**Valid until:** 2026-03-21 (stable codebase, 30-day window)
