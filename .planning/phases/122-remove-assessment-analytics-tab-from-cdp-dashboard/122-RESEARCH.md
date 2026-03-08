# Phase 122: Remove Assessment Analytics Tab from CDP Dashboard - Research

**Researched:** 2026-03-08
**Domain:** ASP.NET Core MVC view/controller cleanup
**Confidence:** HIGH

## Summary

This phase removes redundant Assessment Analytics functionality from the CDP Dashboard. The analytics feature already exists in CMP/Records Team View, making the CDP Dashboard copy unnecessary. The work is purely subtractive: delete code, simplify the remaining page from a tabbed layout to a single-section Coaching Proton dashboard.

All target code has been located and verified. The removal is clean with no external dependencies on the analytics endpoints.

**Primary recommendation:** Delete analytics code bottom-up (JS, partials, controller actions, view model), then simplify Dashboard view to single-section layout.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Full removal of ALL Assessment Analytics code (controller actions, view model, partials, JS)
- Remove tab UI entirely -- single-section page with Coaching Proton content
- Page title changes to "Coaching Proton Dashboard"
- Navbar/hub card labels updated, Analytics references removed
- Old Analytics tab URLs just land on Dashboard normally (no redirect needed)
- Dashboard page accessible to all authenticated users

### Claude's Discretion
- CSS adjustments after removing tab container
- Whether to simplify Dashboard action method after removing analytics data loading
- How to handle remaining tab-related CSS classes

### Deferred Ideas (OUT OF SCOPE)
- Add Excel and PDF export to Coaching Proton tab -- separate phase
</user_constraints>

## Standard Stack

Not applicable -- this is a removal/cleanup phase using existing ASP.NET Core MVC patterns already in the project.

## Architecture Patterns

### Removal Inventory

**Files to DELETE:**
| File | Purpose |
|------|---------|
| `Views/CDP/Shared/_AssessmentAnalyticsPartial.cshtml` | Tab partial wrapper |
| `Views/CDP/Shared/_AssessmentAnalyticsContentPartial.cshtml` | AJAX content partial |

**Files to MODIFY:**
| File | Changes |
|------|---------|
| `Controllers/CDPController.cs` | Remove `FilterAssessmentAnalytics` (line 684), `ExportAnalyticsResults` (line 696), `BuildAnalyticsSubModelAsync` (line 559). Remove analytics params from `Dashboard()` action (line 234-239). Remove analytics model building (lines 261-267). |
| `Models/CDPDashboardViewModel.cs` | Remove `AssessmentAnalyticsSubModel` class (lines 76-105), its property (line 18), and supporting classes: `AssessmentReportItem`, `ReportFilters`, `CategoryStatistic` |
| `Views/CDP/Dashboard.cshtml` | Remove tab nav structure, analytics tab pane, all analytics JS (lines 180-291, 301-305). Change title to "Coaching Proton Dashboard". Render `_CoachingProtonPartial` directly without tab wrapper. |
| `Views/CDP/Index.cshtml` | Update Dashboard card title/description (lines 79-94) |
| `Views/Home/Guide.cshtml` | Update Dashboard reference text (line 346) |
| `Views/Home/GuideDetail.cshtml` | Update Dashboard guide steps (line 285+) |

### Caller Verification
- `ExportAnalyticsResults` -- only called from analytics JS in Dashboard.cshtml (line 243: `/CDP/ExportAnalyticsResults`). No other callers found. Safe to delete.
- `FilterAssessmentAnalytics` -- only called from analytics JS in Dashboard.cshtml (line 257). Safe to delete.
- `BuildAnalyticsSubModelAsync` -- private method, only called from Dashboard() and FilterAssessmentAnalytics. Safe to delete.
- `AssessmentAnalyticsSubModel` -- only used in CDPDashboardViewModel and the two analytics partials. Safe to delete.

### Dashboard Action Simplification
After removal, the `Dashboard()` action signature simplifies from 5 analytics params to zero extra params. The method body reduces to: get user/role, build CoacheeData or ProtonProgressData, return View. The `isHCAccess` block (lines 261-267) is removed entirely.

### View Simplification
Current Dashboard.cshtml has a Bootstrap tab structure (`nav-tabs` / `tab-content`) that conditionally shows an Analytics tab for HC/Admin. After removal:
- Remove `<ul class="nav nav-tabs">` and `<div class="tab-content">` wrappers
- Render Coaching Proton partial directly (or CoacheeDashboard for Coachee role)
- Remove all analytics-related `<script>` code (~110 lines)

## Don't Hand-Roll

Not applicable -- this is deletion work.

## Common Pitfalls

### Pitfall 1: Orphaned Supporting Classes
**What goes wrong:** Deleting `AssessmentAnalyticsSubModel` but leaving `AssessmentReportItem`, `ReportFilters`, `CategoryStatistic` in the view model file.
**How to avoid:** Check if these classes are used anywhere else. If only used by analytics, delete them too.

### Pitfall 2: Missing Guide Page Updates
**What goes wrong:** Dashboard renamed but Guide/GuideDetail pages still reference old "Dashboard" or "Assessment Analytics" text.
**How to avoid:** Update both `Guide.cshtml` (line 346) and `GuideDetail.cshtml` (line 285+).

### Pitfall 3: Build Errors from Missing Using Statements
**What goes wrong:** Removing classes that are referenced in other files via implicit usings.
**How to avoid:** Build after each major deletion step to catch errors early.

## Code Examples

### Simplified Dashboard Action (after removal)
```csharp
public async Task<IActionResult> Dashboard()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault() ?? "";

    var model = new CDPDashboardViewModel { CurrentUserRole = userRole };

    bool isLiteralCoachee = userRole == UserRoles.Coachee;
    if (isLiteralCoachee)
    {
        model.CoacheeData = await BuildCoacheeSubModelAsync(user.Id);
        return View(model);
    }

    model.ProtonProgressData = await BuildProtonProgressSubModelAsync(user, userRole);
    model.ScopeLabel = _lastScopeLabel;

    return View(model);
}
```

### Simplified Dashboard View Structure (after removal)
```html
@model HcPortal.Models.CDPDashboardViewModel
@{
    ViewData["Title"] = "Coaching Proton Dashboard";
}

<div class="container-fluid py-3">
    <h2><i class="bi bi-speedometer2 me-2"></i>Coaching Proton Dashboard</h2>
    <p class="text-muted">...</p>

    @if (Model.CoacheeData != null)
    {
        <partial name="Shared/_CoacheeDashboardPartial" model="Model.CoacheeData" />
    }
    else if (Model.ProtonProgressData != null)
    {
        <partial name="Shared/_CoachingProtonPartial" model="Model.ProtonProgressData" />
    }
</div>
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser verification |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet build && dotnet run` |

### Phase Requirements - Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| N/A-01 | Analytics tab removed from Dashboard | manual | Browser check: /CDP/Dashboard as HC | N/A |
| N/A-02 | Dashboard shows single Coaching Proton section | manual | Browser check: /CDP/Dashboard | N/A |
| N/A-03 | Build succeeds with no errors | build | `dotnet build` | N/A |
| N/A-04 | Hub card text updated | manual | Browser check: /CDP/Index | N/A |
| N/A-05 | Old analytics URL doesn't error | manual | Browse /CDP/Dashboard?activeTab=analytics | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Phase gate:** Full browser verification of all 5 checks

### Wave 0 Gaps
None -- no test infrastructure needed for a removal phase.

## Sources

### Primary (HIGH confidence)
- Direct code inspection of CDPController.cs, CDPDashboardViewModel.cs, Dashboard.cshtml, Index.cshtml
- Grep verification of all callers for analytics endpoints

## Metadata

**Confidence breakdown:**
- Removal scope: HIGH - all code located and caller chains verified
- Architecture: HIGH - straightforward deletion with clear simplification path
- Pitfalls: HIGH - well-understood risk areas for removal work

**Research date:** 2026-03-08
**Valid until:** 2026-04-08
