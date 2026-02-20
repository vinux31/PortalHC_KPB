---
phase: quick-6
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/CMPController.cs
  - Views/CMP/Assessment.cshtml
autonomous: true

must_haves:
  truths:
    - "Assessment manage page loads faster — only one DB query on page load instead of two"
    - "Management tab data is correct — same grouping and fields visible"
    - "Monitoring tab still shows its table when clicked"
    - "Monitor tab badge shows 0 (loaded lazily, not from server on page load)"
  artifacts:
    - path: "Controllers/CMPController.cs"
      provides: "Projected management query + new GetMonitorData JSON endpoint"
      contains: "GetMonitorData"
    - path: "Views/CMP/Assessment.cshtml"
      provides: "AJAX lazy-load for monitoring tab on first click"
      contains: "GetMonitorData"
  key_links:
    - from: "Views/CMP/Assessment.cshtml monitor-tab button"
      to: "/CMP/GetMonitorData"
      via: "fetch() on shown.bs.tab event (fires once)"
      pattern: "GetMonitorData"
    - from: "Controllers/CMPController.cs Assessment action"
      to: "AssessmentSessions table"
      via: "Single projected SELECT — no Include(a => a.User)"
      pattern: "\\.Select\\("
---

<objective>
Fix two performance problems on the Assessment manage page that cause a full table scan twice on every page load:
1. Management query loads full User entities via Include() — replace with Select() projection.
2. Monitor query runs a second full table scan on every page load even when the tab is never opened — move it to a lazy-load AJAX endpoint called once on first tab click.

Purpose: Page load DB cost drops from two full scans to one targeted SELECT.
Output: Faster manage page + new GET /CMP/GetMonitorData JSON endpoint.
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/PROJECT.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Replace management Include() with Select() projection in CMPController</name>
  <files>Controllers/CMPController.cs</files>
  <action>
In the `Assessment` action (lines 108–234), inside the `if (view == "manage" && isHCAccess)` block:

**Management query (currently lines 111–166):**

Replace the existing query block with a projected version. Instead of `.Include(a => a.User).AsQueryable()` followed by `.ToListAsync()` loading full entities, project directly in the query to anonymous objects before hitting the database. The search filter must also run in-query (not in-memory), so keep the Where clause on the IQueryable before materializing.

Replace lines 111–166 with:

```csharp
// Management tab: ALL assessments (CRUD operations) — projected to avoid loading full User entities
var managementQuery = _context.AssessmentSessions
    .AsQueryable();

if (!string.IsNullOrEmpty(search))
{
    var lowerSearch = search.ToLower();
    managementQuery = managementQuery.Where(a =>
        a.Title.ToLower().Contains(lowerSearch) ||
        a.Category.ToLower().Contains(lowerSearch) ||
        (a.User != null && (
            a.User.FullName.ToLower().Contains(lowerSearch) ||
            (a.User.NIP != null && a.User.NIP.Contains(lowerSearch))
        ))
    );
}

ViewBag.SearchTerm = search;

// Project only needed fields — no full User entity load
var allSessions = await managementQuery
    .OrderByDescending(a => a.Schedule)
    .Select(a => new
    {
        a.Id,
        a.Title,
        a.Category,
        a.Schedule,
        a.DurationMinutes,
        a.Status,
        a.IsTokenRequired,
        a.AccessToken,
        a.PassPercentage,
        a.AllowAnswerReview,
        a.CreatedAt,
        UserFullName = a.User != null ? a.User.FullName : "Unknown",
        UserEmail    = a.User != null ? a.User.Email    : ""
    })
    .ToListAsync();

// Group by (Title, Category, Schedule.Date) — in-memory after projection
var grouped = allSessions
    .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
    .Select(g =>
    {
        var rep = g.OrderBy(a => a.CreatedAt).First();
        return new
        {
            Title             = rep.Title,
            Category          = rep.Category,
            Schedule          = rep.Schedule,
            DurationMinutes   = rep.DurationMinutes,
            Status            = rep.Status,
            IsTokenRequired   = rep.IsTokenRequired,
            AccessToken       = rep.AccessToken,
            PassPercentage    = rep.PassPercentage,
            AllowAnswerReview = rep.AllowAnswerReview,
            RepresentativeId  = rep.Id,
            Users    = g.Select(a => new { FullName = a.UserFullName, Email = a.UserEmail }).ToList(),
            AllIds   = g.Select(a => a.Id).ToList()
        };
    })
    .OrderByDescending(g => g.Schedule)
    .ToList();

var totalCount = grouped.Count;
var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

var managementData = grouped
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToList();

ViewBag.CurrentPage    = page;
ViewBag.TotalPages     = totalPages;
ViewBag.TotalCount     = totalCount;
ViewBag.PageSize       = pageSize;
ViewBag.ManagementData = managementData;
```

**Monitor query — DELETE from Assessment action:**

Delete lines 175–228 (the entire monitor query block including `ViewBag.MonitorData = monitorGroups;`). The monitor data is no longer server-rendered on page load.

**Add GetMonitorData action:**

Add a new public action method in CMPController after the Assessment action (before EditAssessment). This action contains the exact same monitor query logic that was deleted above:

```csharp
[HttpGet]
public async Task<IActionResult> GetMonitorData()
{
    var userRole = HttpContext.Session.GetString("UserRole") ?? "Worker";
    bool isHCAccess = userRole == "HC" || userRole == "Admin";
    if (!isHCAccess) return Forbid();

    var cutoff = DateTime.UtcNow.AddDays(-30);
    var monitorSessions = await _context.AssessmentSessions
        .Where(a => a.Status == "Open"
                 || a.Status == "Upcoming"
                 || (a.Status == "Completed" && a.CompletedAt != null && a.CompletedAt >= cutoff))
        .Select(a => new
        {
            a.Id,
            a.Title,
            a.Category,
            a.Schedule,
            a.Status,
            a.Score,
            a.IsPassed,
            a.CompletedAt,
            UserFullName = a.User != null ? a.User.FullName : "Unknown",
            UserNIP      = a.User != null ? a.User.NIP      : ""
        })
        .ToListAsync();

    var monitorGroups = monitorSessions
        .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
        .Select(g =>
        {
            var sessions = g.Select(a =>
            {
                bool isCompleted = a.Score != null || a.CompletedAt != null;
                return new MonitoringSessionViewModel
                {
                    Id           = a.Id,
                    UserFullName = a.UserFullName,
                    UserNIP      = a.UserNIP,
                    UserStatus   = isCompleted ? "Completed" : "Not started",
                    Score        = a.Score,
                    IsPassed     = a.IsPassed,
                    CompletedAt  = a.CompletedAt
                };
            }).ToList();

            bool hasOpen     = g.Any(a => a.Status == "Open");
            bool hasUpcoming = g.Any(a => a.Status == "Upcoming");
            string groupStatus = hasOpen ? "Open" : hasUpcoming ? "Upcoming" : "Closed";

            int completedCount = sessions.Count(s => s.UserStatus == "Completed");
            int passedCount    = sessions.Count(s => s.IsPassed == true);

            return new MonitoringGroupViewModel
            {
                Title          = g.Key.Title,
                Category       = g.Key.Category,
                Schedule       = g.First().Schedule,
                GroupStatus    = groupStatus,
                TotalCount     = sessions.Count,
                CompletedCount = completedCount,
                PassedCount    = passedCount,
                Sessions       = sessions
            };
        })
        .OrderBy(g => g.GroupStatus == "Closed" ? 1 : 0)
        .ThenBy(g => g.GroupStatus != "Closed" ? g.Schedule : DateTime.MaxValue)
        .ThenByDescending(g => g.GroupStatus == "Closed" ? g.Schedule : DateTime.MinValue)
        .ToList();

    return Json(monitorGroups);
}
```

Note: The Select() projection on monitorSessions uses `.User.NIP` — confirm NIP is a navigation property on AssessmentSession via the User FK. If `a.User.NIP` is not translatable by EF Core (unlikely but possible), fall back to in-memory after a separate `.Include(a => a.User)` only on the filtered subset. Prefer the Select() path first.
  </action>
  <verify>dotnet build — zero errors. No compile errors from missing properties or type mismatches.</verify>
  <done>Build succeeds. Assessment action no longer references .Include(a => a.User) in the manage branch. GetMonitorData action exists and returns Json(monitorGroups).</done>
</task>

<task type="auto">
  <name>Task 2: Wire monitoring tab to lazy-load AJAX in Assessment.cshtml</name>
  <files>Views/CMP/Assessment.cshtml</files>
  <action>
Three targeted changes to Assessment.cshtml:

**Change 1 — Monitor tab badge (line 101):**
The badge previously showed `@monitorGroups.Count` from server-side ViewBag. Since ViewBag.MonitorData no longer exists, change to a static placeholder:

```html
<span class="badge bg-secondary ms-1" id="monitorBadge">...</span>
```

**Change 2 — Monitoring Tab Pane content (lines 327–427):**
Replace the entire `@if (monitorGroups.Count == 0) { ... } else { ... }` Razor block with a loading placeholder container. The JS will replace this content when the tab is clicked:

```html
<!-- Monitoring Tab Pane -->
<div class="tab-pane fade" id="monitor-tab-pane" role="tabpanel">
    <div id="monitorLoadingState" class="text-center py-5">
        <div class="spinner-border text-primary mb-3" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
        <p class="text-muted">Loading monitoring data...</p>
    </div>
    <div id="monitorContent" class="d-none"></div>
</div>
```

**Change 3 — Add JS fetch logic:**
In the `<script>` block at the bottom of the file (inside the existing `document.addEventListener('DOMContentLoaded', ...)` or add a new one), add the following after the existing tab event listeners:

```javascript
// Monitoring tab lazy-load — fires once on first click
(function () {
    var monitorTab = document.getElementById('monitor-tab');
    if (!monitorTab) return; // not in manage view

    var loaded = false;
    monitorTab.addEventListener('shown.bs.tab', function () {
        if (loaded) return;
        loaded = true;

        fetch('/CMP/GetMonitorData')
            .then(function (res) { return res.json(); })
            .then(function (groups) {
                var badge = document.getElementById('monitorBadge');
                if (badge) badge.textContent = groups.length;

                var loading = document.getElementById('monitorLoadingState');
                var content = document.getElementById('monitorContent');
                if (loading) loading.classList.add('d-none');
                if (content) content.classList.remove('d-none');

                if (groups.length === 0) {
                    content.innerHTML = '<div class="text-center py-5">' +
                        '<i class="bi bi-binoculars text-muted mb-3" style="font-size:4rem;"></i>' +
                        '<h5 class="text-muted">No active, upcoming, or recently closed assessments</h5>' +
                        '<p class="text-muted small">Closed assessments older than 30 days are not shown.</p>' +
                        '</div>';
                    return;
                }

                var catBadge = function (cat) {
                    var map = {
                        'OJT': 'bg-primary', 'IHT': 'bg-success',
                        'Training Licencor': 'bg-danger', 'OTS': 'bg-warning text-dark',
                        'Mandatory HSSE Training': 'bg-info', 'Proton': 'bg-purple'
                    };
                    return map[cat] || 'bg-secondary';
                };
                var statusBadge = function (s) {
                    return s === 'Open' ? 'bg-success' : s === 'Upcoming' ? 'bg-warning text-dark' : 'bg-secondary';
                };

                var rows = groups.map(function (g) {
                    var completedPct = g.totalCount > 0 ? Math.round(g.completedCount * 100 / g.totalCount) : 0;
                    var passRatePct  = g.completedCount > 0 ? Math.round(g.passedCount * 100 / g.completedCount) : 0;
                    var schedDate    = new Date(g.schedule).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' });
                    var detailUrl    = '/CMP/AssessmentMonitoringDetail?title=' + encodeURIComponent(g.title) +
                                      '&category=' + encodeURIComponent(g.category) +
                                      '&scheduleDate=' + new Date(g.schedule).toISOString().split('T')[0];
                    var passCell = g.completedCount === 0
                        ? '<span class="text-muted small">\u2014</span>'
                        : '<span class="' + (passRatePct >= 70 ? 'text-success' : 'text-danger') + ' fw-semibold small">' + g.passedCount + ' passed (' + passRatePct + '%)</span>';

                    return '<tr>' +
                        '<td><div class="fw-semibold">' + g.title + '</div><div class="mt-1"><span class="badge ' + catBadge(g.category) + ' small">' + g.category + '</span></div></td>' +
                        '<td class="text-nowrap">' + schedDate + '</td>' +
                        '<td><span class="badge ' + statusBadge(g.groupStatus) + '">' + g.groupStatus + '</span></td>' +
                        '<td style="min-width:160px;"><div class="d-flex align-items-center gap-2">' +
                            '<div class="progress flex-grow-1" style="height:8px;" title="' + g.completedCount + ' of ' + g.totalCount + ' completed">' +
                                '<div class="progress-bar ' + (completedPct === 100 ? 'bg-success' : 'bg-primary') + '" role="progressbar" style="width:' + completedPct + '%" aria-valuenow="' + completedPct + '" aria-valuemin="0" aria-valuemax="100"></div>' +
                            '</div>' +
                            '<span class="text-muted small text-nowrap">' + g.completedCount + '/' + g.totalCount + '</span>' +
                        '</div></td>' +
                        '<td class="text-nowrap">' + passCell + '</td>' +
                        '<td class="text-end"><a href="' + detailUrl + '" class="btn btn-sm btn-outline-primary"><i class="bi bi-eye me-1"></i>View Details</a></td>' +
                    '</tr>';
                }).join('');

                content.innerHTML = '<div class="table-responsive"><table class="table table-hover align-middle">' +
                    '<thead class="table-light"><tr>' +
                    '<th>Assessment</th><th>Schedule</th><th>Status</th><th>Completion</th><th>Pass Rate</th><th></th>' +
                    '</tr></thead><tbody>' + rows + '</tbody></table></div>';
            })
            .catch(function () {
                var loading = document.getElementById('monitorLoadingState');
                if (loading) loading.innerHTML = '<div class="text-center py-5"><p class="text-danger">Failed to load monitoring data. Please refresh the page.</p></div>';
            });
    });
}());
```

Also remove the line `var monitorGroups = ViewBag.MonitorData as List<HcPortal.Models.MonitoringGroupViewModel> ?? new List<HcPortal.Models.MonitoringGroupViewModel>();` from the top of the manage block (line 86) since ViewBag.MonitorData is no longer populated by the server.
  </action>
  <verify>
1. `dotnet build` — zero errors.
2. Navigate to `/CMP/Assessment?view=manage` — page loads, Management tab shows assessment cards normally.
3. Click the Monitoring tab — spinner appears briefly, then table renders (or "no assessments" message if empty).
4. Click Monitoring tab again — no second network request (loaded = true guard).
5. Check Network tab in browser DevTools: only ONE request to AssessmentSessions on page load (the management query), GetMonitorData fires only when Monitoring tab is clicked.
  </verify>
  <done>
- Manage page loads with only the management query.
- Monitor tab badge shows "..." until tab is clicked, then updates to group count.
- Monitor table renders correctly via AJAX with same columns and styling as before.
- Monitoring Detail links still work (URL format matches AssessmentMonitoringDetail route).
  </done>
</task>

</tasks>

<verification>
- `dotnet build` passes with zero errors.
- `/CMP/Assessment?view=manage` loads noticeably faster (one DB query instead of two).
- Management tab: cards display Title, Category, Schedule, Status, token controls, user list, delete — identical to pre-fix behavior.
- Monitoring tab: loads on click, shows the grouped table with Completion progress bars and Pass Rate — identical display to pre-fix.
- `/CMP/GetMonitorData` returns 403 Forbidden for Worker role (isHCAccess guard).
</verification>

<success_criteria>
- Zero compile errors.
- No `.Include(a => a.User)` left in the manage branch of the Assessment action.
- GetMonitorData action exists and returns JSON.
- Monitor tab content rendered client-side via fetch, not server-rendered Razor.
</success_criteria>

<output>
After completion, create `.planning/quick/6-fix-slow-performance-on-assessment-manag/6-SUMMARY.md`
</output>
