# Phase 7: Development Dashboard - Research

**Researched:** 2026-02-18
**Domain:** ASP.NET Core MVC role-scoped dashboard, Chart.js time-series, EF Core aggregation
**Confidence:** HIGH

---

## Summary

Phase 7 adds a role-scoped development dashboard for supervisors and HC to monitor team competency progress, deliverable status, and pending approvals. The dashboard must scope data by role (Spv sees their unit, SrSpv/SectionHead see their section, HC/Admin see all sections), and include Chart.js trend charts for competency level changes over time.

The codebase already has Chart.js integrated via CDN in three existing views (CDP/Dashboard, CMP/CompetencyGap, CMP/ReportsIndex). The established pattern is: serialize C# model data with `@Json.Serialize()` into JavaScript variables, then pass to `new Chart(ctx, { ... })`. No library wrapper is used — raw Chart.js via CDN is the project standard.

**Critical constraint discovered:** `UserCompetencyLevel` stores only the CURRENT level per user per competency — there is no history table. For DASH-04 ("competency level changes over time"), time-series data must be derived from `ProtonFinalAssessment.CompletedAt` + `CompetencyLevelGranted`, or from `ProtonDeliverableProgress.ApprovedAt` as a proxy. The planner must address this architectural gap. Recommended approach: use `ProtonFinalAssessment` records grouped by month as the trend data source, since they have `CompletedAt` timestamps and represent the authoritative level-grant event.

**Primary recommendation:** Add `CDPController.DevDashboard` action with a new `DevDashboardViewModel`. Use the existing role-scoping infrastructure (`user.Section`, `user.Unit`, `user.RoleLevel`) already proven in `CDPController.Dashboard` and `ProtonMain`. The new view is `Views/CDP/DevDashboard.cshtml` — a separate route from the existing `Dashboard` (which is HC-only and IDP-focused).

---

## Standard Stack

No new packages needed. Phase 7 uses everything already in the project.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Chart.js | latest via CDN (`https://cdn.jsdelivr.net/npm/chart.js`) | Bar/line/doughnut charts | Already used in 3 views; do not change CDN source |
| ASP.NET Core MVC | Project version | Controller + View routing | Project framework |
| Entity Framework Core | Project version | Role-scoped DB queries | Project ORM |
| Bootstrap 5.3 | CDN | Layout, cards, badges, progress bars | Already in `_Layout.cshtml` |
| Bootstrap Icons 1.10 | CDN | Icon set (bi-*) | Already in `_Layout.cshtml` |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `@Json.Serialize()` | Razor built-in | Pass C# lists to Chart.js | For all chart data arrays |
| `UserManager<ApplicationUser>` | Identity | Get current user + roles | Already injected in CDPController |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Raw Chart.js CDN | ChartJSCore NuGet | ChartJSCore adds complexity; CDN is project standard |
| CDN Chart.js | npm bundle | npm not used in this project |

**Installation:** No new packages required.

---

## Architecture Patterns

### Recommended Project Structure

New files for Phase 7:
```
Controllers/
└── CDPController.cs              # Add DevDashboard GET action
Models/
└── DevDashboardViewModel.cs      # New ViewModel file
Views/CDP/
└── DevDashboard.cshtml           # New view
```

### Pattern 1: Role-Scoped Data Filtering

**What:** The dashboard must filter data based on the current user's role and organizational scope. This pattern is already established in `CDPController.Dashboard` (section filter) and `CDPController.ProtonMain` (section coachees).

**When to use:** All database queries in `DevDashboard` action.

**Example:**
```csharp
// Source: CDPController.cs (existing pattern)
var user = await _userManager.GetUserAsync(User);
var roles = await _userManager.GetRolesAsync(user);
var userRole = roles.FirstOrDefault();

// Step 1: Build scoped coachee ID list
List<string> scopedCoacheeIds;

if (userRole == UserRoles.HC || userRole == UserRoles.Admin)
{
    // HC/Admin: all sections — no filter
    scopedCoacheeIds = await _context.Users
        .Where(u => u.RoleLevel == 6) // Coachee only
        .Select(u => u.Id)
        .ToListAsync();
}
else if (userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead)
{
    // SrSpv/SectionHead: their section
    scopedCoacheeIds = await _context.Users
        .Where(u => u.Section == user.Section && u.RoleLevel == 6)
        .Select(u => u.Id)
        .ToListAsync();
}
else if (userRole == UserRoles.Coach)
{
    // Spv (Coach): their unit only
    scopedCoacheeIds = await _context.Users
        .Where(u => u.Unit == user.Unit && u.RoleLevel == 6)
        .Select(u => u.Id)
        .ToListAsync();
}
else
{
    // Coachee or unknown: no access
    return Forbid();
}
```

**Key detail:** Per the phase requirements, "Spv" maps to `UserRoles.Coach` (RoleLevel 5) in this codebase. `ApplicationUser.Unit` is the unit field; `ApplicationUser.Section` is the section field.

### Pattern 2: Chart.js Data via `@Json.Serialize()`

**What:** C# List data is passed from the controller model to Chart.js via `@Json.Serialize()` in the Razor view.

**When to use:** All Chart.js datasets in `DevDashboard.cshtml`.

**Example:**
```javascript
// Source: Views/CDP/Dashboard.cshtml (established pattern)
document.addEventListener("DOMContentLoaded", function() {
    const ctx = document.getElementById('myChart').getContext('2d');
    const labels = @Json.Serialize(Model.ChartLabels);
    const dataset = @Json.Serialize(Model.ChartDataset);

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Competency Levels',
                data: dataset,
                borderColor: 'rgba(54, 162, 235, 1)',
                backgroundColor: 'rgba(54, 162, 235, 0.1)',
                fill: true,
                tension: 0.3
            }]
        },
        options: {
            responsive: true,
            plugins: { legend: { position: 'top' } },
            scales: {
                y: { beginAtZero: true, max: 5,
                     grid: { borderDash: [5, 5] } },
                x: { grid: { display: false } }
            }
        }
    });
});
```

### Pattern 3: Deliverable Progress Aggregation

**What:** For each coachee in scope, compute: total deliverables, approved, submitted (pending approval), rejected, locked.

**Example:**
```csharp
// Source: CDPController.cs (existing ProtonMain pattern, extended)
var progressSummaries = await _context.ProtonDeliverableProgresses
    .Where(p => scopedCoacheeIds.Contains(p.CoacheeId))
    .GroupBy(p => p.CoacheeId)
    .Select(g => new CoacheeProgressSummary
    {
        CoacheeId = g.Key,
        Total = g.Count(),
        Approved = g.Count(p => p.Status == "Approved"),
        Submitted = g.Count(p => p.Status == "Submitted"),
        Rejected = g.Count(p => p.Status == "Rejected"),
        Active = g.Count(p => p.Status == "Active"),
        Locked = g.Count(p => p.Status == "Locked")
    })
    .ToListAsync();
```

### Pattern 4: Competency Trend Data from ProtonFinalAssessment

**What:** Since `UserCompetencyLevel` has no history table, group `ProtonFinalAssessment` records by month to show level-grant trend over time.

**When to use:** DASH-04 competency progress chart.

**Example:**
```csharp
// Source: Derived from ProtonFinalAssessment entity (Models/ProtonModels.cs)
var trendData = await _context.ProtonFinalAssessments
    .Where(fa => scopedCoacheeIds.Contains(fa.CoacheeId) && fa.CompletedAt.HasValue)
    .GroupBy(fa => new {
        Year = fa.CompletedAt.Value.Year,
        Month = fa.CompletedAt.Value.Month
    })
    .Select(g => new {
        Label = g.Key.Year.ToString() + "-" + g.Key.Month.ToString("D2"),
        Count = g.Count(),
        AvgLevel = g.Average(fa => (double)fa.CompetencyLevelGranted)
    })
    .OrderBy(x => x.Label)
    .ToListAsync();
```

### Pattern 5: Access Guard (Coachee has no access)

**What:** DASH-01 requires coachees have no access. Gate the action early.

**Example:**
```csharp
// Source: CDPController.cs (existing Forbid pattern)
if (user.RoleLevel > 5 && userRole != UserRoles.Admin && userRole != UserRoles.HC)
{
    return Forbid(); // Coachees (RoleLevel 6) cannot access
}
```

**Precise rule from DASH-01:**
- Allowed: Spv (Coach, RoleLevel 5), SrSpv (RoleLevel 4), SectionHead (RoleLevel 4), HC (RoleLevel 2), Admin (RoleLevel 1)
- Blocked: Coachee (RoleLevel 6)

Note: In the codebase, Spv maps to `UserRoles.Coach`. SrSpv maps to `UserRoles.SrSupervisor`.

### Anti-Patterns to Avoid

- **N+1 queries for coachee names:** Load all user display names in one batch query using `.ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName)` — exactly as done in `CDPController.HCApprovals`.
- **N+1 for progress per coachee:** Never loop and query per coachee. Use `.GroupBy(p => p.CoacheeId)` in a single query.
- **Reusing `CDPController.Dashboard`:** The existing `Dashboard` action is IDP-focused and does not have Proton deliverable data. Phase 7 adds a separate `DevDashboard` action/view.
- **Chart data computed in Razor:** All aggregation happens in the controller. The view only receives serialized lists.
- **UserCompetencyLevel for history:** This table has no history — only current state. Do NOT query it for trend lines. Use `ProtonFinalAssessment` timestamps instead.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Chart rendering | Custom SVG/HTML table charts | Chart.js via CDN (already in project) | Already integrated; consistent UI |
| Role-scoping logic | Custom permission class | `user.Section`, `user.Unit`, `user.RoleLevel` + role string | Already established in CDPController |
| User name lookups | Per-row DB queries | Batch `.ToDictionaryAsync()` | Prevents N+1; pattern already used in HCApprovals |
| Progress grouping | Loop + separate query per coachee | EF Core `.GroupBy().Select()` | Single DB round-trip |
| JSON serialization for JS | Manual string concatenation | `@Json.Serialize(Model.Property)` | Handles escaping, nulls, XSS; project standard |

**Key insight:** The data model and role-scoping infrastructure already exists. Phase 7 is primarily a new controller action + ViewModel + view, wiring together data that's already queryable.

---

## Common Pitfalls

### Pitfall 1: Role Name Mismatch ("Spv" vs "Coach")

**What goes wrong:** DASH-02 says "Spv sees their unit only" — but `UserRoles.Coach` is the constant in code. Confusing the business term "Supervisor" with the role string `"Coach"` causes wrong role checks.

**Why it happens:** Business terminology differs from the role string constants.

**How to avoid:** Always use `UserRoles.*` constants. "Spv" = `UserRoles.Coach` (RoleLevel 5). "SrSpv" = `UserRoles.SrSupervisor` (RoleLevel 4). "SectionHead" = `UserRoles.SectionHead` (RoleLevel 4).

**Warning signs:** Role check that uses string literal `"Supervisor"` or `"Spv"` instead of `UserRoles.Coach`.

### Pitfall 2: No Competency Level History Table

**What goes wrong:** DASH-04 requires "competency level changes over time" but `UserCompetencyLevel` only stores the current level (one row per user per competency, with a unique index). Querying it for a time-series returns a flat snapshot, not a trend.

**Why it happens:** The table was designed for current state, not history.

**How to avoid:** Use `ProtonFinalAssessment.CompletedAt` + `CompetencyLevelGranted` as the time-series source. Group by year-month. Acknowledge that this only shows Proton-track completions, not broader competency history.

**Warning signs:** Any query to `UserCompetencyLevels` trying to group by `AchievedAt` or `UpdatedAt` for a trend chart.

### Pitfall 3: Section vs Unit Scoping Confusion

**What goes wrong:** Spv (Coach) should see their UNIT only; SrSpv/SectionHead see their full SECTION. Using `Section` for both, or `Unit` for both, produces wrong data.

**Why it happens:** `ApplicationUser` has both `Section` (e.g., "GAST") and `Unit` (e.g., "RFCC NHT") fields. The scoping rules are different per role level.

**How to avoid:** Coach role: `u.Unit == user.Unit`. SrSpv/SectionHead: `u.Section == user.Section`. HC/Admin: no filter.

**Warning signs:** Spv dashboard shows too many coachees (section-wide instead of unit-only).

### Pitfall 4: Coachee Role Not Blocked

**What goes wrong:** Coachees navigate to the dashboard URL and see data.

**Why it happens:** Missing or incorrect access guard.

**How to avoid:** Check `user.RoleLevel == 6` or `userRole == UserRoles.Coachee` at the top of the action and `return Forbid()`. Do NOT rely on nav menu hiding alone.

**Warning signs:** Dashboard action has no explicit role check.

### Pitfall 5: Using Existing `CDPController.Dashboard` Route

**What goes wrong:** Phase 7 feature is added to the existing `Dashboard` action, breaking the existing IDP-focused dashboard for HC.

**Why it happens:** Same controller, similar name.

**How to avoid:** Create a separate `DevDashboard` action (and `DevDashboard.cshtml` view). Route: `CDP/DevDashboard`. The existing `Dashboard` action (IDP-focused) must remain unchanged.

**Warning signs:** Modifying the existing `Dashboard` action rather than adding `DevDashboard`.

### Pitfall 6: Empty Chart Data Edge Cases

**What goes wrong:** Chart.js crashes or renders badly when data arrays are empty (no completions yet).

**Why it happens:** New deployments or units with no activity have zero ProtonFinalAssessment records.

**How to avoid:** Always check `if (data.Any())` before populating chart arrays. Render a "No data yet" placeholder when empty. The `CompetencyGap.cshtml` view already does this: `@if (Model.Competencies.Any())`.

---

## Code Examples

Verified patterns from existing views in this codebase:

### Summary Card with Icon (Bootstrap 5)
```html
<!-- Source: Views/CDP/Dashboard.cshtml (established UI pattern) -->
<div class="col-md-3">
    <div class="card border-0 shadow-sm text-white h-100 bg-gradient-blue hover-elevate">
        <div class="card-body position-relative overflow-hidden">
            <div class="position-absolute end-0 top-0 p-3 opacity-25">
                <i class="bi bi-check2-circle" style="font-size: 4rem;"></i>
            </div>
            <h6 class="text-uppercase fw-bold text-white-50 small mb-2">Label</h6>
            <h2 class="fw-bold display-5 mb-0">@Model.Value</h2>
            <small class="text-white-50">Subtitle text</small>
        </div>
    </div>
</div>
```

### Line Chart for Trend Data (Chart.js)
```javascript
// Source: Pattern from Views/CDP/Dashboard.cshtml + Views/CMP/CompetencyGap.cshtml
document.addEventListener("DOMContentLoaded", function() {
    const labels = @Json.Serialize(Model.TrendLabels);   // e.g. ["2025-10", "2025-11"]
    const values = @Json.Serialize(Model.TrendValues);   // e.g. [2.1, 2.5, 3.0]

    const ctx = document.getElementById('trendChart').getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Avg Competency Level Granted',
                data: values,
                borderColor: 'rgba(54, 162, 235, 1)',
                backgroundColor: 'rgba(54, 162, 235, 0.1)',
                fill: true,
                tension: 0.3,
                pointRadius: 5,
                pointHoverRadius: 7
            }]
        },
        options: {
            responsive: true,
            plugins: { legend: { position: 'top' } },
            scales: {
                y: { beginAtZero: true, max: 5,
                     title: { display: true, text: 'Level (0-5)' },
                     grid: { borderDash: [5, 5] } },
                x: { grid: { display: false } }
            }
        }
    });
});
```

### Donut Chart for Deliverable Status Distribution
```javascript
// Source: Pattern derived from existing Chart.js usage in project
const ctx2 = document.getElementById('statusChart').getContext('2d');
const statusLabels = @Json.Serialize(Model.StatusLabels);
const statusData   = @Json.Serialize(Model.StatusData);

new Chart(ctx2, {
    type: 'doughnut',
    data: {
        labels: statusLabels,
        datasets: [{
            data: statusData,
            backgroundColor: [
                'rgba(40, 167, 69, 0.8)',   // Approved (green)
                'rgba(23, 162, 184, 0.8)',   // Submitted (blue)
                'rgba(255, 193, 7, 0.8)',    // Active (yellow)
                'rgba(220, 53, 69, 0.8)',    // Rejected (red)
                'rgba(173, 181, 189, 0.8)'   // Locked (grey)
            ]
        }]
    },
    options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' } }
    }
});
```

### Batch Name Lookup (Avoid N+1)
```csharp
// Source: CDPController.HCApprovals (existing pattern)
var coacheeIds = progressList.Select(p => p.CoacheeId).Distinct().ToList();
var userNames = await _context.Users
    .Where(u => coacheeIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);
```

### DevDashboard ViewModel Structure
```csharp
// Models/DevDashboardViewModel.cs (new file)
public class DevDashboardViewModel
{
    // Summary cards
    public int TotalCoachees { get; set; }
    public int TotalDeliverables { get; set; }
    public int ApprovedDeliverables { get; set; }
    public int PendingApprovals { get; set; }      // Status == "Submitted"
    public int CompletedCoachees { get; set; }     // All deliverables Approved

    // Per-coachee progress table
    public List<CoacheeProgressRow> CoacheeRows { get; set; } = new();

    // Chart: competency trend
    public List<string> TrendLabels { get; set; } = new();   // e.g. "2025-10"
    public List<double> TrendValues { get; set; } = new();   // avg level granted

    // Chart: deliverable status distribution
    public List<string> StatusLabels { get; set; } = new();  // ["Approved","Submitted",...]
    public List<int> StatusData { get; set; } = new();

    // Context
    public string CurrentUserRole { get; set; } = "";
    public string? ScopeLabel { get; set; }        // "Unit: RFCC NHT" or "Section: GAST" or "All Sections"
}

public class CoacheeProgressRow
{
    public string CoacheeId { get; set; } = "";
    public string CoacheeName { get; set; } = "";
    public string TrackType { get; set; } = "";     // "Operator" or "Panelman"
    public string TahunKe { get; set; } = "";       // "Tahun 1", "Tahun 2", "Tahun 3"
    public int TotalDeliverables { get; set; }
    public int Approved { get; set; }
    public int Submitted { get; set; }              // Pending approval
    public int Rejected { get; set; }
    public int Active { get; set; }
    public bool HasFinalAssessment { get; set; }
    public int? CompetencyLevelGranted { get; set; }
    public string ProgressPercent => TotalDeliverables > 0
        ? $"{(int)((double)Approved / TotalDeliverables * 100)}%"
        : "0%";
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Chart.js loaded per-view via CDN | Still per-view CDN (no global bundle) | Project standard | Phase 7 follows same CDN include in the view |
| Mock data in `CDPController.Dashboard` | Real DB queries for DASH stats | Phase 7 | New `DevDashboard` action uses live data only |

**Deprecated/outdated:**
- `CDPController.Dashboard` TopUnits mock data: Still hardcoded with mock unit compliance. Phase 7's `DevDashboard` should use real data. Do not copy the mock pattern.

---

## Open Questions

1. **Spv (Coach) unit scoping — what if `user.Unit` is null?**
   - What we know: `ApplicationUser.Unit` is nullable (`string?`). Some users may have it unset.
   - What's unclear: Whether all Coach-role users have Unit populated in production data.
   - Recommendation: Fall back to `user.Section` if `user.Unit` is null, with a warning in the view. Add a null-guard in the query: `u.Unit == user.Unit && user.Unit != null`.

2. **Competency trend chart when no ProtonFinalAssessments exist**
   - What we know: On a fresh deployment or for sections with no completed tracks, there will be zero trend data points.
   - What's unclear: Whether the chart should show a message or not render at all.
   - Recommendation: Follow the CompetencyGap pattern — `@if (Model.TrendLabels.Any())` renders the chart; else render an `alert-info` card saying "No completions recorded yet."

3. **"Pending approvals" scope — deliverables pending SrSpv/SectionHead approval vs HC review**
   - What we know: `ProtonDeliverableProgress.Status == "Submitted"` = pending SrSpv/SectionHead approval. `HCApprovalStatus == "Pending"` = pending HC review.
   - What's unclear: DASH-03 says "pending approvals" without specifying which type.
   - Recommendation: Show both: "Pending Spv Approval" (Status==Submitted) and "Pending HC Review" (HCApprovalStatus==Pending && Status==Approved) as separate counts in summary cards.

4. **Navigation — how do users reach DevDashboard?**
   - What we know: The existing `_Layout.cshtml` has nav links to CDP/Index and CMP/Index. The `CDPController.Dashboard` is reached via the existing navigation.
   - What's unclear: Whether DevDashboard should be linked from the existing CDP nav or as a top-level link.
   - Recommendation: Add a "Dev Dashboard" nav link in `_Layout.cshtml` that is only visible to roles with access (HC, Admin, SrSpv, SectionHead, Coach). Use `@if (User.IsInRole(...))` conditional rendering — same pattern already used for Assessment Reports Quick Link in `Dashboard.cshtml`.

---

## Sources

### Primary (HIGH confidence)
- Codebase: `Controllers/CDPController.cs` — role-scoping, N+1 avoidance, approval workflow patterns
- Codebase: `Views/CDP/Dashboard.cshtml` — Chart.js CDN, `@Json.Serialize()` pattern, Bootstrap card structure
- Codebase: `Views/CMP/CompetencyGap.cshtml` — radar chart, empty-state guard pattern
- Codebase: `Models/ApplicationUser.cs` — `Section`, `Unit`, `RoleLevel` fields
- Codebase: `Models/ProtonModels.cs` — `ProtonDeliverableProgress.Status` values, `ProtonFinalAssessment` structure
- Codebase: `Models/UserRoles.cs` — role constants and level mapping
- Codebase: `Data/ApplicationDbContext.cs` — entity relationships and indexes

### Secondary (MEDIUM confidence)
- WebSearch: "Chart.js line chart time series ASP.NET Core MVC Razor 2025" — confirmed CDN integration pattern; official Chart.js docs at https://www.chartjs.org/docs/latest/ apply directly to project's CDN usage

### Tertiary (LOW confidence)
- None required — all critical findings sourced from codebase directly

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; everything sourced from existing codebase files
- Architecture: HIGH — role-scoping and Chart.js patterns directly from existing controller/views
- Pitfalls: HIGH — identified from specific code examination (UserCompetencyLevel schema, role string constants, existing null patterns)

**Research date:** 2026-02-18
**Valid until:** 2026-03-20 (stable stack — Chart.js CDN, EF Core, ASP.NET Core MVC)
