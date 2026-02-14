---
phase: quick-1
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/CMPController.cs
  - Views/CMP/ReportsIndex.cshtml
  - Views/CDP/Dashboard.cshtml
  - Models/DashboardViewModel.cs
autonomous: true

must_haves:
  truths:
    - "Section filter dropdown shows all 4 sections (GAST, RFCC, NGP, DHT/HMU) regardless of whether users in those sections have completed assessments"
    - "User search field provides autocomplete suggestions showing matching names/NIPs as user types"
    - "CDP Dashboard shows a quick link card that displays assessment summary stats and links to CMP Reports"
  artifacts:
    - path: "Controllers/CMPController.cs"
      provides: "Fixed section dropdown using OrganizationStructure.GetAllSections(), new SearchUsers JSON endpoint for autocomplete"
    - path: "Views/CMP/ReportsIndex.cshtml"
      provides: "Autocomplete-enabled user search input with dropdown suggestions"
    - path: "Views/CDP/Dashboard.cshtml"
      provides: "Assessment Reports quick link widget card"
    - path: "Models/DashboardViewModel.cs"
      provides: "Assessment summary properties for dashboard widget"
  key_links:
    - from: "Views/CMP/ReportsIndex.cshtml"
      to: "Controllers/CMPController.cs"
      via: "fetch /CMP/SearchUsers?term=X for autocomplete"
      pattern: "fetch.*SearchUsers"
    - from: "Controllers/CMPController.cs"
      to: "Models/OrganizationStructure.cs"
      via: "OrganizationStructure.GetAllSections() for section dropdown"
      pattern: "OrganizationStructure\\.GetAllSections"
    - from: "Views/CDP/Dashboard.cshtml"
      to: "Controllers/CDPController.cs"
      via: "Model.AssessmentSummary properties rendered in widget"
      pattern: "Model\\.(TotalCompletedAssessments|OverallPassRate)"
---

<objective>
Fix three Phase 2 follow-up improvements: (1) section filter showing all sections from OrganizationStructure instead of only from Users table, (2) autocomplete typeahead on user search field in reports, (3) quick link widget on CDP Dashboard showing assessment summary and linking to CMP Reports.

Purpose: Improve HC workflow by ensuring all sections are filterable, making user search faster with autocomplete, and providing quick navigation from CDP Dashboard to assessment reports.
Output: Updated CMPController, ReportsIndex view, CDPController, and CDP Dashboard view.
</objective>

<context>
@.planning/PROJECT.md
@.planning/STATE.md
@.planning/ROADMAP.md

Key source files:
@Controllers/CMPController.cs (ReportsIndex action, lines 1141-1295)
@Controllers/CDPController.cs (Dashboard action, lines 75-151)
@Views/CMP/ReportsIndex.cshtml (full file - filter form and results table)
@Views/CDP/Dashboard.cshtml (full file - dashboard cards and charts)
@Models/ReportsDashboardViewModel.cs (ViewModels for reports)
@Models/DashboardViewModel.cs (ViewModel for CDP dashboard)
@Models/OrganizationStructure.cs (static sections: RFCC, DHT/HMU, NGP, GAST)
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix Section Filter and Add User Search Autocomplete</name>
  <files>
    Controllers/CMPController.cs
    Views/CMP/ReportsIndex.cshtml
  </files>
  <action>
**1. Fix Section Filter (CMPController.cs, ~line 1259-1264):**

The current section dropdown query pulls sections only from the Users table:
```csharp
var sections = await _context.Users
    .Where(u => u.Section != null && u.Section != "")
    .Select(u => u.Section!)
    .Distinct()
    .OrderBy(s => s)
    .ToListAsync();
```

This means if no user in a particular section has completed an assessment, that section won't appear. Replace with the static OrganizationStructure source that already exists in the codebase:

```csharp
var sections = OrganizationStructure.GetAllSections();
```

This returns all 4 sections (RFCC, DHT/HMU, NGP, GAST) from `Models/OrganizationStructure.cs`. The same pattern is already used in other actions (e.g., CreateAssessment, EditAssessment at lines 354, 450, 513, 588).

**2. Add SearchUsers JSON endpoint (CMPController.cs):**

Add a new action method after the ReportsIndex action (after line 1295). This endpoint supports the autocomplete feature:

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> SearchUsers(string term)
{
    if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        return Json(new List<object>());

    var users = await _context.Users
        .Where(u => u.FullName.Contains(term) ||
                     (u.NIP != null && u.NIP.Contains(term)))
        .OrderBy(u => u.FullName)
        .Take(10)
        .Select(u => new {
            fullName = u.FullName,
            nip = u.NIP ?? "",
            section = u.Section ?? ""
        })
        .ToListAsync();

    return Json(users);
}
```

Key details:
- Minimum 2 characters before searching (prevents broad queries)
- Searches both FullName and NIP (same fields as the existing userSearch filter)
- Limited to 10 results for performance
- Returns JSON array with fullName, nip, section for display in dropdown
- Same Authorize attribute as ReportsIndex (Admin, HC only)

**3. Add autocomplete UI to ReportsIndex.cshtml:**

Replace the plain text input for user search (lines 113-117) with an autocomplete-enabled input. Keep the existing `name="userSearch"` and `value` binding so the form submission still works identically.

Add a wrapper div with `position-relative` around the input. Below the input, add a hidden `div` for autocomplete suggestions styled as a dropdown list:

```html
<div class="col-md-4 position-relative">
    <label class="form-label small text-muted fw-semibold">User Search</label>
    <input type="text" id="userSearchInput" name="userSearch" class="form-control"
           placeholder="Search by name or NIP..."
           value="@Model.CurrentFilters.UserSearch" autocomplete="off" />
    <div id="userSearchSuggestions" class="list-group position-absolute w-100 shadow-sm"
         style="z-index: 1050; display: none; max-height: 250px; overflow-y: auto;">
    </div>
</div>
```

Add JavaScript in the `@section Scripts` block (before the chart scripts):

```javascript
// User Search Autocomplete
(function() {
    var input = document.getElementById('userSearchInput');
    var suggestions = document.getElementById('userSearchSuggestions');
    var debounceTimer;

    input.addEventListener('input', function() {
        clearTimeout(debounceTimer);
        var term = this.value.trim();
        if (term.length < 2) {
            suggestions.style.display = 'none';
            return;
        }
        debounceTimer = setTimeout(function() {
            fetch('@Url.Action("SearchUsers")' + '?term=' + encodeURIComponent(term))
                .then(function(r) { return r.json(); })
                .then(function(data) {
                    if (data.length === 0) {
                        suggestions.style.display = 'none';
                        return;
                    }
                    suggestions.innerHTML = '';
                    data.forEach(function(user) {
                        var item = document.createElement('a');
                        item.href = '#';
                        item.className = 'list-group-item list-group-item-action py-2 px-3';
                        item.innerHTML = '<div class="fw-semibold small">' + user.fullName + '</div>' +
                            '<small class="text-muted">' + (user.nip ? 'NIP: ' + user.nip : '') +
                            (user.section ? ' | ' + user.section : '') + '</small>';
                        item.addEventListener('mousedown', function(e) {
                            e.preventDefault();
                            input.value = user.fullName;
                            suggestions.style.display = 'none';
                        });
                        suggestions.appendChild(item);
                    });
                    suggestions.style.display = 'block';
                });
        }, 300); // 300ms debounce
    });

    input.addEventListener('blur', function() {
        setTimeout(function() { suggestions.style.display = 'none'; }, 200);
    });

    input.addEventListener('focus', function() {
        if (suggestions.children.length > 0) {
            suggestions.style.display = 'block';
        }
    });
})();
```

Key UX details:
- 300ms debounce to avoid excessive API calls
- Uses `mousedown` + `preventDefault` instead of `click` to fire before `blur` hides the dropdown
- Shows name (bold) + NIP/section (muted) in each suggestion
- Clicking a suggestion fills the input with the user's full name
- Hidden on blur with 200ms delay (allows click to register)
- Uses Bootstrap list-group styling (consistent with existing UI patterns)
- IIFE to avoid polluting global scope
  </action>
  <verify>
1. Build succeeds: `dotnet build` passes with no errors
2. Navigate to /CMP/ReportsIndex - section dropdown shows all 4 sections (GAST, RFCC, NGP, DHT/HMU) even if no users in those sections have completed assessments
3. Type at least 2 characters in the user search field - autocomplete dropdown appears with matching users
4. Click a suggestion - input field is filled with the selected user's name
5. Submit the form with a selected user - results filter correctly
  </verify>
  <done>
- Section filter always shows GAST, RFCC, NGP, DHT/HMU (from OrganizationStructure, not Users table)
- User search has working autocomplete that appears after 2+ characters with 300ms debounce
- Selecting a suggestion fills the input; form submission still filters correctly
  </done>
</task>

<task type="auto">
  <name>Task 2: Add Assessment Reports Quick Link Widget to CDP Dashboard</name>
  <files>
    Controllers/CDPController.cs
    Models/DashboardViewModel.cs
    Views/CDP/Dashboard.cshtml
  </files>
  <action>
**1. Extend DashboardViewModel (Models/DashboardViewModel.cs):**

Add assessment summary properties to the existing `DashboardViewModel` class. These will power the quick link widget:

```csharp
// Assessment Reports Quick Link Summary
public int TotalCompletedAssessments { get; set; }
public double OverallPassRate { get; set; }
public int TotalUsersAssessed { get; set; }
```

**2. Populate assessment data in CDPController.cs Dashboard action (~line 121-147):**

Before creating the model (before `var model = new DashboardViewModel`), add queries to get assessment summary data:

```csharp
// Assessment summary for quick link widget
var completedAssessments = await _context.AssessmentSessions
    .Where(a => a.Status == "Completed")
    .CountAsync();

var assessmentPassRate = completedAssessments > 0
    ? await _context.AssessmentSessions
        .Where(a => a.Status == "Completed")
        .CountAsync(a => a.IsPassed == true) * 100.0 / completedAssessments
    : 0;

var totalUsersAssessed = await _context.AssessmentSessions
    .Where(a => a.Status == "Completed")
    .Select(a => a.UserId)
    .Distinct()
    .CountAsync();
```

Then add to the model initialization:

```csharp
TotalCompletedAssessments = completedAssessments,
OverallPassRate = Math.Round(assessmentPassRate, 1),
TotalUsersAssessed = totalUsersAssessed,
```

Note: These assessment stats are global (not filtered by view), since the quick link is meant to give HC a snapshot before navigating to the full reports page.

**3. Add quick link widget card to CDP Dashboard view (Views/CDP/Dashboard.cshtml):**

Insert a new row AFTER the summary cards row (after the closing `</div>` of the `row g-4 mb-4` at line 65) and BEFORE the Charts & Lists row (before line 68 `<!-- Charts & Lists -->`):

```html
<!-- Assessment Reports Quick Link -->
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
    <div class="row g-4 mb-4">
        <div class="col-12">
            <div class="card border-0 shadow-sm hover-elevate">
                <div class="card-body p-4">
                    <div class="d-flex justify-content-between align-items-center">
                        <div class="d-flex align-items-center">
                            <div class="rounded-3 bg-primary bg-opacity-10 p-3 me-3">
                                <i class="bi bi-clipboard-data text-primary" style="font-size: 1.8rem;"></i>
                            </div>
                            <div>
                                <h6 class="fw-bold mb-1">Assessment Reports</h6>
                                <p class="text-muted mb-0 small">View detailed assessment analytics, filter results, and export data</p>
                            </div>
                        </div>
                        <div class="d-flex align-items-center gap-4">
                            <div class="text-center">
                                <div class="fw-bold text-primary fs-5">@Model.TotalCompletedAssessments</div>
                                <small class="text-muted">Completed</small>
                            </div>
                            <div class="text-center">
                                <div class="fw-bold text-success fs-5">@Model.OverallPassRate%</div>
                                <small class="text-muted">Pass Rate</small>
                            </div>
                            <div class="text-center">
                                <div class="fw-bold text-info fs-5">@Model.TotalUsersAssessed</div>
                                <small class="text-muted">Users</small>
                            </div>
                            <a asp-controller="CMP" asp-action="ReportsIndex" class="btn btn-primary ms-3">
                                <i class="bi bi-arrow-right-circle me-1"></i>Open Reports
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
}
```

Key design decisions:
- Only visible to Admin and HC roles (same as ReportsIndex authorization)
- Uses the existing `hover-elevate` CSS class from the Dashboard for consistency
- Card layout: icon left, description middle, 3 summary stats right, button far right
- Summary stats (Completed, Pass Rate, Users) give at-a-glance value before clicking through
- Uses existing Bootstrap 5 classes and bi icons (consistent with both dashboards)
- `asp-controller="CMP" asp-action="ReportsIndex"` generates the correct route
  </action>
  <verify>
1. Build succeeds: `dotnet build` passes with no errors
2. Log in as Admin or HC user, navigate to CDP Dashboard (/CDP/Dashboard)
3. Quick link card appears between summary cards and charts showing assessment stats
4. Stats display correctly (Completed count, Pass Rate %, Users assessed count)
5. Clicking "Open Reports" navigates to /CMP/ReportsIndex
6. Log in as Coachee - quick link card should NOT appear on CDP Dashboard
  </verify>
  <done>
- CDP Dashboard shows Assessment Reports quick link widget for Admin/HC users
- Widget displays 3 summary metrics (completed count, pass rate, users assessed)
- "Open Reports" button navigates to CMP/ReportsIndex
- Widget hidden from non-Admin/non-HC users
  </done>
</task>

</tasks>

<verification>
1. `dotnet build` succeeds with zero errors
2. Section filter on /CMP/ReportsIndex shows all 4 sections (GAST, RFCC, NGP, DHT/HMU)
3. User search autocomplete works: typing 2+ chars shows suggestions, clicking fills input
4. CDP Dashboard (/CDP/Dashboard) shows assessment quick link card for Admin/HC users
5. Quick link "Open Reports" button navigates to /CMP/ReportsIndex correctly
6. All existing functionality (filters, pagination, export, charts) continues to work
</verification>

<success_criteria>
- All 3 follow-up improvements are implemented and working
- Section filter uses OrganizationStructure.GetAllSections() (4 static sections always shown)
- Autocomplete fetches from /CMP/SearchUsers with 300ms debounce, 2-char minimum
- CDP Dashboard quick link widget visible only for Admin/HC with live assessment stats
- Build compiles successfully with no regressions
</success_criteria>

<output>
After completion, update .planning/STATE.md to mark follow-up items as completed.
</output>
