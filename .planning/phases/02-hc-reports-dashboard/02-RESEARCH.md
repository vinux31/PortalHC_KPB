# Phase 2: HC Reports Dashboard - Research

**Researched:** 2026-02-14
**Domain:** ASP.NET Core MVC reporting and analytics dashboards with data visualization and Excel export
**Confidence:** HIGH

## Summary

Phase 2 builds a comprehensive reporting dashboard for HC staff to analyze assessment results across all users. This requires implementing advanced filtering, data aggregation with Entity Framework Core, interactive charts using Chart.js, and Excel export functionality. The current codebase already has solid foundations: Bootstrap 5 UI framework, Entity Framework Core 8, existing pagination patterns in the Assessment lobby, and Chart.js integration in the CDP Dashboard.

The research reveals three critical technical pillars: (1) **ClosedXML** for Excel exports (free, developer-friendly API, proven in .NET Core environments), (2) **Chart.js** via CDN for visualization (already used in CDP Dashboard, lightweight, no server dependencies), and (3) **Entity Framework Core GroupBy aggregation** for analytics queries (server-side processing for performance).

Key architectural decisions include server-side filtering and aggregation to handle potentially large datasets efficiently, reusing existing pagination patterns from the Assessment lobby, and maintaining consistency with the established Bootstrap 5 + Razor views approach (no SPA frameworks).

**Primary recommendation:** Use ClosedXML for Excel export, Chart.js for charts, server-side EF Core aggregation for statistics, and extend existing filtering/pagination patterns from Assessment lobby with additional filters (date range, category, section).

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | 0.104.x+ | Excel file generation and export | Most intuitive API for .NET, actively maintained, MIT license (free for commercial use), handles up to 100k rows efficiently |
| Chart.js | 4.x (via CDN) | Client-side data visualization | Already used in project (CDP Dashboard), 60k+ GitHub stars, lightweight canvas rendering, Bootstrap-compatible |
| Entity Framework Core | 8.0 (installed) | Data aggregation and querying | Already in stack, handles GroupBy aggregation server-side since EF Core 2.1 |
| Bootstrap | 5.3 (via CDN) | UI framework and date/range pickers | Already in stack, consistent with existing UI patterns |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Tempus Dominus | 6.x (via CDN) | Date/date-range picker for Bootstrap 5 | For date range filtering in reports dashboard - successor to bootstrap-datetimepicker, no jQuery dependency |
| Flatpickr | 4.x (via CDN, alternative) | Lightweight date picker | If Tempus Dominus proves too heavy, Flatpickr is simpler alternative |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ClosedXML | EPPlus 5+ | EPPlus requires commercial license for production use, though it has better performance for very large datasets (200k+ rows) |
| ClosedXML | OpenXML SDK | OpenXML SDK is lower-level and more complex API, requires more code but offers maximum control |
| Chart.js | CanvasJS | CanvasJS requires commercial license, has more chart types but overkill for basic analytics |
| Tempus Dominus | Custom date inputs | HTML5 date inputs lack cross-browser consistency and range selection UX |

**Installation:**
```bash
dotnet add package ClosedXML --version 0.104.1
# Chart.js and Tempus Dominus: use CDN in view (already established pattern in project)
```

## Architecture Patterns

### Recommended Project Structure
```
Controllers/
├── CMPController.cs        # Add ReportsIndex, ExportResults actions

Models/
├── ReportsDashboardViewModel.cs    # Dashboard summary statistics
├── AssessmentReportItem.cs         # Individual assessment report row
├── UserAssessmentHistoryViewModel.cs  # User-specific history view
└── ReportFilters.cs                # Filter parameters (category, date range, etc.)

Views/CMP/
├── ReportsIndex.cshtml      # Main reports dashboard
├── UserAssessmentHistory.cshtml  # Individual user history detail
└── _ReportFilters.cshtml    # Partial view for filter controls (reusable)

wwwroot/js/
└── reports-charts.js        # Chart.js initialization for dashboard charts
```

### Pattern 1: Server-Side Filtering with ViewModels

**What:** Controller builds filtered query based on parameters, executes aggregation server-side, returns ViewModel to view.

**When to use:** For reports with complex filters and aggregations - avoids loading large datasets into memory.

**Example:**
```csharp
// Source: ASP.NET Core MVC best practices + EF Core documentation
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ReportsIndex(
    string? category,
    DateTime? startDate,
    DateTime? endDate,
    string? section,
    string? userId,
    int page = 1,
    int pageSize = 20)
{
    // Base query - only completed assessments have results
    var query = _context.AssessmentSessions
        .Include(a => a.User)
        .Where(a => a.Status == "Completed");

    // Apply filters
    if (!string.IsNullOrEmpty(category))
        query = query.Where(a => a.Category == category);

    if (startDate.HasValue)
        query = query.Where(a => a.CompletedAt >= startDate.Value);

    if (endDate.HasValue)
        query = query.Where(a => a.CompletedAt <= endDate.Value);

    if (!string.IsNullOrEmpty(section))
        query = query.Where(a => a.User != null && a.User.Section == section);

    if (!string.IsNullOrEmpty(userId))
        query = query.Where(a => a.UserId == userId);

    // Get summary statistics (server-side aggregation)
    var totalAssessments = await query.CountAsync();
    var passedCount = await query.CountAsync(a => a.IsPassed == true);
    var avgScore = await query.AverageAsync(a => (double?)a.Score) ?? 0;

    // Paginated results
    var assessments = await query
        .OrderByDescending(a => a.CompletedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(a => new AssessmentReportItem
        {
            Id = a.Id,
            Title = a.Title,
            Category = a.Category,
            UserName = a.User != null ? a.User.FullName : "",
            Score = a.Score ?? 0,
            IsPassed = a.IsPassed ?? false,
            CompletedAt = a.CompletedAt
        })
        .ToListAsync();

    var viewModel = new ReportsDashboardViewModel
    {
        Assessments = assessments,
        TotalAssessments = totalAssessments,
        PassedCount = passedCount,
        PassRate = totalAssessments > 0 ? (passedCount * 100.0 / totalAssessments) : 0,
        AverageScore = avgScore,
        // Pagination
        CurrentPage = page,
        TotalPages = (int)Math.Ceiling(totalAssessments / (double)pageSize),
        // Filter state (for maintaining filter selections)
        CurrentFilters = new ReportFilters
        {
            Category = category,
            StartDate = startDate,
            EndDate = endDate,
            Section = section,
            UserId = userId
        }
    };

    return View(viewModel);
}
```

### Pattern 2: GroupBy Aggregation for Analytics

**What:** Use EF Core GroupBy to calculate statistics per category, section, or date range. Translates to SQL GROUP BY for performance.

**When to use:** For dashboard summary cards, category comparisons, and trend analysis.

**Example:**
```csharp
// Source: EF Core Complex Query Operators documentation
// Category-based analytics
var categoryStats = await _context.AssessmentSessions
    .Where(a => a.Status == "Completed")
    .GroupBy(a => a.Category)
    .Select(g => new CategoryStatistic
    {
        CategoryName = g.Key,
        TotalAssessments = g.Count(),
        PassedCount = g.Count(a => a.IsPassed == true),
        PassRate = g.Count() > 0
            ? (g.Count(a => a.IsPassed == true) * 100.0 / g.Count())
            : 0,
        AverageScore = g.Average(a => (double?)a.Score) ?? 0
    })
    .ToListAsync();

// Pass the statistics to Chart.js for visualization
ViewBag.CategoryStats = categoryStats;
```

### Pattern 3: Keyset Pagination for Performance

**What:** Use WHERE clause to skip rows instead of SKIP/TAKE offset pagination. More performant for large datasets.

**When to use:** If report tables grow beyond 10,000+ records and users navigate deep pages.

**Example:**
```csharp
// Source: EF Core Pagination documentation - Microsoft Learn
// Offset pagination (current pattern, good for < 10k records)
var offsetResults = await query
    .OrderByDescending(a => a.CompletedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Keyset pagination (better for large datasets, if needed)
var lastCompletedAt = GetLastCompletedAtFromPreviousPage(); // from query string
var keysetResults = await query
    .OrderByDescending(a => a.CompletedAt)
    .ThenByDescending(a => a.Id) // unique ordering
    .Where(a => a.CompletedAt < lastCompletedAt ||
                (a.CompletedAt == lastCompletedAt && a.Id < lastId))
    .Take(pageSize)
    .ToListAsync();
```

**Recommendation for Phase 2:** Start with offset pagination (consistent with Assessment lobby pattern). Only switch to keyset if performance testing reveals issues with large datasets.

### Pattern 4: Excel Export with ClosedXML

**What:** Generate Excel file from IEnumerable collection, return as FileResult for download.

**When to use:** Export filtered assessment results for external analysis.

**Example:**
```csharp
// Source: ClosedXML documentation and ASP.NET Core best practices
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ExportResults(
    string? category,
    DateTime? startDate,
    DateTime? endDate,
    string? section)
{
    // Build same query as ReportsIndex (reuse filter logic)
    var query = _context.AssessmentSessions
        .Include(a => a.User)
        .Where(a => a.Status == "Completed");

    // Apply filters (same as ReportsIndex)
    // ... filter logic ...

    // Get all results (no pagination for export)
    var results = await query
        .OrderByDescending(a => a.CompletedAt)
        .Select(a => new
        {
            AssessmentTitle = a.Title,
            Category = a.Category,
            UserName = a.User != null ? a.User.FullName : "",
            UserNIP = a.User != null ? a.User.NIP : "",
            Section = a.User != null ? a.User.Section : "",
            Score = a.Score ?? 0,
            PassPercentage = a.PassPercentage,
            IsPassed = a.IsPassed == true ? "Pass" : "Fail",
            CompletedAt = a.CompletedAt
        })
        .ToListAsync();

    using (var workbook = new XLWorkbook())
    {
        var worksheet = workbook.Worksheets.Add("Assessment Results");

        // Load data from collection (ClosedXML auto-detects headers)
        var table = worksheet.Cell(1, 1).InsertTable(results);

        // Style header row
        table.HeadersRow().Style.Font.Bold = true;
        table.HeadersRow().Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Return as file download
        using (var stream = new MemoryStream())
        {
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            var fileName = $"AssessmentResults_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
```

### Pattern 5: Chart.js Integration with Razor Views

**What:** Pass aggregated data to view via ViewBag/ViewModel, render Chart.js chart in client-side JavaScript.

**When to use:** Dashboard visualizations (pass rate trends, score distributions, category comparisons).

**Example:**
```csharp
// Controller: Pass category stats to view
ViewBag.CategoryStats = categoryStats; // from Pattern 2
```

```html
<!-- View: CMP/ReportsIndex.cshtml -->
<!-- Source: Existing pattern from Views/CDP/Dashboard.cshtml -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>

<div class="card">
    <div class="card-header">
        <h6 class="fw-bold mb-0">Pass Rate by Category</h6>
    </div>
    <div class="card-body">
        <canvas id="categoryChart" height="300"></canvas>
    </div>
</div>

<script>
    // Prepare data from server
    var categoryData = @Html.Raw(Json.Serialize(ViewBag.CategoryStats));

    var ctx = document.getElementById('categoryChart').getContext('2d');
    var chart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: categoryData.map(c => c.categoryName),
            datasets: [{
                label: 'Pass Rate (%)',
                data: categoryData.map(c => c.passRate),
                backgroundColor: 'rgba(13, 110, 253, 0.8)', // Bootstrap primary
                borderColor: 'rgba(13, 110, 253, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: {
                        callback: function(value) {
                            return value + '%';
                        }
                    }
                }
            }
        }
    });
</script>
```

### Anti-Patterns to Avoid

- **Loading all data then filtering client-side:** With potentially thousands of assessment records, always filter and aggregate server-side using EF Core queries.
- **N+1 query problems:** Always use `.Include()` for related entities (User) to avoid lazy loading issues.
- **Exposing unfiltered endpoints:** Reports should default to date range (e.g., last 30 days) to prevent accidentally loading years of data.
- **Hard-coding filter options:** Load categories, sections from database to keep UI in sync with actual data.
- **Generating Excel in memory for large exports:** ClosedXML handles 100k rows efficiently, but for extreme cases (500k+), consider streaming or background job patterns.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel file generation | Custom XML/ZIP manipulation | ClosedXML | Excel format is complex (relationships, styles, formulas). ClosedXML handles edge cases, cell formatting, auto-sizing. |
| Chart rendering | Server-side image generation (System.Drawing) | Chart.js (client-side) | System.Drawing has GDI+ dependencies, doesn't work on Linux containers. Chart.js renders in browser, responsive, interactive. |
| Date range picker | Custom JavaScript calendar | Tempus Dominus 6.x | Date range UX has many edge cases (timezone handling, validation, localization). Tempus Dominus is mature, Bootstrap-compatible. |
| Multi-select dropdowns | Custom checkboxes + JavaScript | Bootstrap 5 native + small enhancement | Multi-select UX is complex (keyboard nav, search, clear all). Use standard select[multiple] with Bootstrap styling, optionally enhance with lightweight library if needed. |
| Aggregation queries | Loading all data in memory | EF Core GroupBy | Aggregating in memory kills performance. EF Core translates GroupBy to SQL GROUP BY, runs on database server. |

**Key insight:** Reports and analytics have deceptive complexity - filtering, pagination, aggregation, export formats all have edge cases that mature libraries solve. Use ClosedXML for Excel, Chart.js for visualization, and EF Core aggregation for statistics.

## Common Pitfalls

### Pitfall 1: DateTime Filtering Without Timezone Awareness

**What goes wrong:** Date range filter "today" shows different results for different users due to UTC vs local time conversion issues.

**Why it happens:** EF Core stores DateTime in database timezone (usually UTC), but UI shows local time. Filtering by `DateTime.Today` uses server local time, not UTC.

**How to avoid:**
- Always store `CompletedAt` as UTC (`DateTime.UtcNow`)
- When filtering by date range, convert user input dates to UTC before querying:
```csharp
// User selects "Jan 1, 2026" as start date
var startDateUtc = startDate.HasValue
    ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc)
    : (DateTime?)null;
```
- Or accept that database stores UTC and display clarifies timezone to users.

**Warning signs:** Users report "missing" results when filtering by today's date, or results from "tomorrow" appearing.

### Pitfall 2: Inefficient GroupBy with Client Evaluation

**What goes wrong:** GroupBy query executes in memory instead of database, loading thousands of records into application memory.

**Why it happens:** Using complex expressions in GroupBy or Select that EF Core cannot translate to SQL triggers client evaluation.

**How to avoid:**
- Keep GroupBy keys simple (direct property access: `a.Category`)
- Use only translatable aggregate functions (Count, Sum, Average, Max, Min)
- Test queries with `.ToQueryString()` to verify SQL translation:
```csharp
var queryString = query.ToQueryString(); // EF Core 5+
Console.WriteLine(queryString); // Verify it's a SQL GROUP BY, not client-side
```
- Enable EF Core logging to catch client evaluation warnings

**Warning signs:** Report page loads slowly (5+ seconds), high memory usage, EF Core logs show "client evaluation" warnings.

### Pitfall 3: Excel Export Timeout on Large Datasets

**What goes wrong:** Exporting 50,000+ rows times out (default 30-second request timeout) or consumes excessive memory.

**Why it happens:** ClosedXML generates entire file in memory before download. Large datasets take time to serialize.

**How to avoid:**
- Set reasonable export limits (10,000 rows max)
- Show warning if export exceeds limit: "Results limited to 10,000 rows. Apply filters to narrow results."
- For extreme cases (100k+ rows), implement background job export with email notification
- Add streaming if needed:
```csharp
// Alternative: Stream to response (advanced)
Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
workbook.SaveAs(Response.Body); // Stream directly
```

**Warning signs:** Export button causes request timeout, server memory spikes, users complain about slow exports.

### Pitfall 4: Filter State Lost on Page Navigation

**What goes wrong:** User applies filters (category = "IHT", date range = last month), navigates to page 2, filters reset to defaults.

**Why it happens:** Filter parameters not passed to pagination links.

**How to avoid:**
- Store filter state in ViewModel and pass to pagination partial:
```csharp
// ViewModel
public ReportFilters CurrentFilters { get; set; }

// View pagination links
<a href="@Url.Action("ReportsIndex", new {
    page = page + 1,
    category = Model.CurrentFilters.Category,
    startDate = Model.CurrentFilters.StartDate,
    endDate = Model.CurrentFilters.EndDate
})">Next</a>
```
- Or use JavaScript to preserve query string parameters on link clicks

**Warning signs:** Users complain filters "don't stick" when changing pages.

### Pitfall 5: Unauthorized Access to Other Users' Data

**What goes wrong:** HC staff accidentally exposes assessment results to unauthorized users, or users can guess URLs to view others' results.

**Why it happens:** Forgot authorization check in controller action, or used client-side filtering only.

**How to avoid:**
- **Always enforce authorization at controller level:**
```csharp
[Authorize(Roles = "Admin, HC")] // Reports are HC-only
public async Task<IActionResult> ReportsIndex() { ... }
```
- For user-specific history view, verify ownership:
```csharp
public async Task<IActionResult> UserAssessmentHistory(string userId)
{
    var currentUser = await _userManager.GetUserAsync(User);
    var userRoles = await _userManager.GetRolesAsync(currentUser);

    // Users can only view their own history, HC/Admin can view anyone's
    if (userId != currentUser.Id &&
        !userRoles.Contains("Admin") &&
        !userRoles.Contains("HC"))
    {
        return Forbid();
    }
    // ... rest of action
}
```

**Warning signs:** Security audit reveals data exposure, unauthorized users can access report URLs.

## Code Examples

Verified patterns from official sources:

### Filtering Dropdown with Dynamic Options

```csharp
// Controller: Load filter options from database
var categories = await _context.AssessmentSessions
    .Where(a => a.Status == "Completed")
    .Select(a => a.Category)
    .Distinct()
    .OrderBy(c => c)
    .ToListAsync();

var sections = await _context.Users
    .Where(u => u.Section != null && u.Section != "")
    .Select(u => u.Section)
    .Distinct()
    .OrderBy(s => s)
    .ToListAsync();

ViewBag.Categories = categories;
ViewBag.Sections = sections;
```

```html
<!-- View: Filter form -->
<form method="get" asp-action="ReportsIndex">
    <div class="row g-3 mb-4">
        <div class="col-md-3">
            <label class="form-label">Category</label>
            <select name="category" class="form-select">
                <option value="">All Categories</option>
                @foreach (var cat in ViewBag.Categories)
                {
                    <option value="@cat" selected="@(cat == Model.CurrentFilters.Category)">
                        @cat
                    </option>
                }
            </select>
        </div>
        <div class="col-md-3">
            <label class="form-label">Date Range</label>
            <input type="date" name="startDate" class="form-control"
                   value="@Model.CurrentFilters.StartDate?.ToString("yyyy-MM-dd")" />
        </div>
        <div class="col-md-3">
            <label class="form-label">&nbsp;</label>
            <input type="date" name="endDate" class="form-control"
                   value="@Model.CurrentFilters.EndDate?.ToString("yyyy-MM-dd")" />
        </div>
        <div class="col-md-3">
            <label class="form-label">&nbsp;</label>
            <button type="submit" class="btn btn-primary w-100">
                <i class="bi bi-funnel"></i> Apply Filters
            </button>
        </div>
    </div>
</form>
```

### Summary Statistics Cards

```html
<!-- View: Dashboard summary cards (pattern from Home/Index.cshtml) -->
<div class="row g-4 mb-4">
    <div class="col-md-3">
        <div class="card border-0 shadow-sm text-white h-100 bg-primary">
            <div class="card-body">
                <h6 class="text-uppercase fw-bold text-white-50 small mb-2">Total Assessments</h6>
                <h2 class="fw-bold display-5 mb-0">@Model.TotalAssessments</h2>
                <small class="text-white-50">Completed assessments</small>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card border-0 shadow-sm text-white h-100 bg-success">
            <div class="card-body">
                <h6 class="text-uppercase fw-bold text-white-50 small mb-2">Pass Rate</h6>
                <h2 class="fw-bold display-5 mb-0">@Model.PassRate.ToString("F1")%</h2>
                <small class="text-white-50">@Model.PassedCount passed</small>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card border-0 shadow-sm text-white h-100 bg-info">
            <div class="card-body">
                <h6 class="text-uppercase fw-bold text-white-50 small mb-2">Average Score</h6>
                <h2 class="fw-bold display-5 mb-0">@Model.AverageScore.ToString("F1")</h2>
                <small class="text-white-50">Out of 100</small>
            </div>
        </div>
    </div>
</div>
```

### Data Table with Export Button

```html
<!-- View: Results table with export -->
<div class="card border-0 shadow-sm">
    <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
        <h6 class="fw-bold mb-0">
            <i class="bi bi-table me-2"></i>Assessment Results
        </h6>
        <a href="@Url.Action("ExportResults", Model.CurrentFilters)"
           class="btn btn-success btn-sm">
            <i class="bi bi-file-earmark-excel"></i> Export to Excel
        </a>
    </div>
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-hover mb-0">
                <thead class="table-light">
                    <tr>
                        <th>Assessment</th>
                        <th>User</th>
                        <th>Category</th>
                        <th>Score</th>
                        <th>Pass/Fail</th>
                        <th>Completed</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in Model.Assessments)
                    {
                        <tr>
                            <td>@item.Title</td>
                            <td>@item.UserName</td>
                            <td><span class="badge bg-secondary">@item.Category</span></td>
                            <td>@item.Score</td>
                            <td>
                                @if (item.IsPassed)
                                {
                                    <span class="badge bg-success">Pass</span>
                                }
                                else
                                {
                                    <span class="badge bg-danger">Fail</span>
                                }
                            </td>
                            <td>@item.CompletedAt?.ToString("yyyy-MM-dd HH:mm")</td>
                            <td>
                                <a href="@Url.Action("Results", "CMP", new { id = item.Id })"
                                   class="btn btn-sm btn-outline-primary">
                                    <i class="bi bi-eye"></i> View
                                </a>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>
    <div class="card-footer bg-white">
        <!-- Pagination (reuse pattern from Assessment.cshtml) -->
        @Html.Partial("_Pagination", Model)
    </div>
</div>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| EPPlus 4.x (LGPL) | EPPlus 5+ (commercial license) OR ClosedXML (MIT) | EPPlus v5 (2020) | EPPlus 5+ requires paid license for commercial use. ClosedXML remains free MIT license, making it the community standard for .NET Core Excel export. |
| jQuery DatePicker | Tempus Dominus 6.x OR Flatpickr | Bootstrap 5 migration (2021+) | Old jQuery datepickers incompatible with Bootstrap 5. Tempus Dominus v6 removes jQuery dependency, uses vanilla JS. |
| Client-side filtering (DataTables.js) | Server-side filtering + pagination | For large datasets (10k+ rows) | DataTables loads all data into browser memory, causing performance issues. Server-side filtering handles millions of records efficiently. |
| System.Drawing for charts | Chart.js (client-side) | .NET Core migration (2016+) | System.Drawing depends on GDI+, doesn't work on Linux. Chart.js is cross-platform, responsive, interactive. |

**Deprecated/outdated:**
- **bootstrap-datetimepicker (eonasdan):** Replaced by Tempus Dominus 6.x for Bootstrap 5
- **EPPlus 4.x (LGPL):** Version 5+ changed to commercial license; use ClosedXML instead for free license
- **jQuery-based UI widgets:** Bootstrap 5 ecosystem prefers vanilla JS libraries (no jQuery dependency)

## Open Questions

1. **Should we implement real-time dashboard updates?**
   - What we know: CDP Dashboard uses Chart.js statically. SignalR could enable real-time updates.
   - What's unclear: Is real-time needed for assessment reports? Results are historical, not live.
   - Recommendation: Start with static dashboard (refresh on page load). Add SignalR only if HC requests "live" updates during assessment sessions.

2. **What is the expected data volume for exports?**
   - What we know: ClosedXML handles 100k rows efficiently. Current system is new (Phase 1 just completed).
   - What's unclear: How many assessments will accumulate over months/years? Unknown user count.
   - Recommendation: Implement 10,000 row export limit initially. Monitor usage, adjust limit or implement background jobs if needed.

3. **Should we support PDF export in addition to Excel?**
   - What we know: Requirements mention "Excel/PDF" but Phase 2 success criteria only specifies Excel.
   - What's unclear: Is PDF export actually required, or just Excel?
   - Recommendation: Implement Excel export only (definite requirement). Ask user if PDF is needed before adding complexity.

4. **What date range should be the default filter?**
   - What we know: No default specified in requirements.
   - What's unclear: Should dashboard default to "all time", "last 30 days", "current month", or require user selection?
   - Recommendation: Default to "last 30 days" to prevent loading excessive data on initial page load. Add "View All Time" option.

## Sources

### Primary (HIGH confidence)
- [ClosedXML GitHub](https://github.com/ClosedXML/ClosedXML) - Excel library documentation
- [Working with Excel files in .NET: OpenXML vs EPPlus vs ClosedXML](https://blog.elmah.io/working-with-excel-files-in-net-openxml-vs-epplus-vs-closedxml/) - Library comparison
- [Excel exports in .NET Core using ClosedXML](https://codingpipe.com/posts/exporting-c-objects-to-excel-with-closedxml/) - Implementation guide
- [Microsoft Learn: Pagination - EF Core](https://learn.microsoft.com/en-us/ef/core/querying/pagination) - Official EF Core pagination patterns
- [Microsoft Learn: Complex Query Operators - EF Core](https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators) - GroupBy and aggregation
- [Microsoft Learn: Tutorial - Sorting, Filtering, Paging with EF Core](https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/sort-filter-page?view=aspnetcore-10.0) - ASP.NET Core MVC patterns
- [Chart.js Official Documentation](https://www.chartjs.org/) - Chart library reference
- [Tempus Dominus Official Docs](https://getdatepicker.com/) - Date picker for Bootstrap 5

### Secondary (MEDIUM confidence)
- [Creating Excel Reports with EPPlus in ASP.NET Core 8.0](https://www.c-sharpcorner.com/article/creating-excel-reports-with-epplus-in-asp-net-core-8-0/) - Alternative Excel approach
- [Implementing Pagination and Filtering in ASP.NET Core 8.0 API](https://www.c-sharpcorner.com/article/implementing-pagination-and-filtering-in-asp-net-core-8-0-api/) - API patterns (adaptable to MVC)
- [EF Core Group By](https://www.csharptutorial.net/entity-framework-core-tutorial/ef-core-group-by/) - GroupBy examples
- [Real-time Charts with ASP.NET Core, SignalR, and Chart.js](https://khalidabuhakmeh.com/real-time-charts-with-aspnet-core-signalr-and-chartjs) - Real-time patterns (future consideration)
- [Role-based authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0) - Authorization patterns
- [Bootstrap DatePicker Example (Date Range Picker) To Use in 2025](https://themeselection.com/bootstrap-datepicker-example/) - Date picker comparison

### Tertiary (LOW confidence)
- Various WebSearch results on multi-select dropdowns and dashboard UI patterns - principles are sound but implementations vary

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - ClosedXML and Chart.js are well-documented, widely used in .NET Core community, verified in official docs
- Architecture: HIGH - Patterns verified against Microsoft Learn EF Core documentation and existing codebase patterns
- Pitfalls: MEDIUM-HIGH - Based on common .NET community issues and official documentation warnings, though not all have been observed in this specific project

**Research date:** 2026-02-14
**Valid until:** 2026-03-15 (30 days - stack is stable, no fast-moving changes expected)
