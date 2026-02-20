---
quick: 5
title: group-manage-view-cards-by-assessment
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/CMPController.cs
  - Views/CMP/Assessment.cshtml
autonomous: true

must_haves:
  truths:
    - "Management tab shows 1 card per unique assessment (Title+Category+Schedule.Date), not 1 card per user assignment"
    - "Each card displays all assigned users compactly (up to 3 names, then '+N more')"
    - "Edit and Questions buttons navigate using a representative session Id"
    - "Delete on a grouped card deletes ALL sibling sessions in the group"
    - "Pagination counts distinct groups, not individual rows"
  artifacts:
    - path: "Controllers/CMPController.cs"
      provides: "Grouped ManagementData + DeleteAssessmentGroup action"
      contains: "DeleteAssessmentGroup"
    - path: "Views/CMP/Assessment.cshtml"
      provides: "Group-aware card loop"
      contains: "confirmDeleteGroup"
  key_links:
    - from: "Views/CMP/Assessment.cshtml"
      to: "Controllers/CMPController.cs"
      via: "confirmDeleteGroup JS -> POST /CMP/DeleteAssessmentGroup"
      pattern: "DeleteAssessmentGroup"
    - from: "CMPController.cs Assessment() action"
      to: "ViewBag.ManagementData"
      via: "in-memory grouping after EF query"
      pattern: "GroupBy.*Title.*Category.*Schedule"
---

<objective>
Group the HC/Admin manage view so each unique assessment (Title+Category+Schedule.Date) shows as one card instead of one card per user assignment.

Purpose: 10 users assigned to the same assessment currently produce 10 identical cards. HC thinks in assessments, not sessions — one card per assessment is the correct mental model.
Output: Modified CMPController.cs (grouped data + DeleteAssessmentGroup action) and Assessment.cshtml (group-aware card loop with compact user list and group delete).
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/PROJECT.md
@Controllers/CMPController.cs
@Views/CMP/Assessment.cshtml
</context>

<tasks>

<task type="auto">
  <name>Task 1: Group ManagementData in controller + add DeleteAssessmentGroup action</name>
  <files>Controllers/CMPController.cs</files>
  <action>
**In `Assessment()` action, replace the flat `managementData` build with an in-memory grouped approach.**

Current code (around line 126-139):
```csharp
var totalCount = await managementQuery.CountAsync();
var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

var managementData = await managementQuery
    .OrderByDescending(a => a.Schedule)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

ViewBag.CurrentPage = page;
ViewBag.TotalPages = totalPages;
ViewBag.TotalCount = totalCount;
ViewBag.PageSize = pageSize;
ViewBag.ManagementData = managementData;
```

Replace with (fetch all matching rows, group in-memory, then paginate on group count):
```csharp
// Fetch all matching sessions (include User for grouping)
var allSessions = await managementQuery
    .OrderByDescending(a => a.Schedule)
    .ToListAsync();

// Group by (Title, Category, Schedule.Date)
var grouped = allSessions
    .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
    .Select(g =>
    {
        var rep = g.OrderBy(a => a.CreatedAt).First(); // representative session
        return new
        {
            Title           = rep.Title,
            Category        = rep.Category,
            Schedule        = rep.Schedule,
            DurationMinutes = rep.DurationMinutes,
            Status          = rep.Status,
            IsTokenRequired = rep.IsTokenRequired,
            AccessToken     = rep.AccessToken,
            PassPercentage  = rep.PassPercentage,
            AllowAnswerReview = rep.AllowAnswerReview,
            RepresentativeId  = rep.Id,
            Users = g.Select(a => new { FullName = a.User?.FullName ?? "Unknown", Email = a.User?.Email ?? "" }).ToList(),
            AllIds = g.Select(a => a.Id).ToList()
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

ViewBag.CurrentPage = page;
ViewBag.TotalPages  = totalPages;
ViewBag.TotalCount  = totalCount;
ViewBag.PageSize    = pageSize;
ViewBag.ManagementData = managementData;
```

Also update the `return View(managementData)` line at the end of the HC branch — change it to:
```csharp
return View(); // ManagementData is in ViewBag; no typed model needed
```

**Add `DeleteAssessmentGroup` action** immediately after the existing `DeleteAssessment` action (after line ~476). Model it on `DeleteAssessment` but delete all siblings:

```csharp
// --- DELETE ASSESSMENT GROUP ---
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAssessmentGroup(int representativeId)
{
    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<CMPController>>();

    try
    {
        // Load representative to get grouping key
        var rep = await _context.AssessmentSessions
            .FirstOrDefaultAsync(a => a.Id == representativeId);

        if (rep == null)
        {
            logger.LogWarning($"DeleteAssessmentGroup: representative session {representativeId} not found");
            return Json(new { success = false, message = "Assessment not found." });
        }

        var scheduleDate = rep.Schedule.Date;

        // Find all siblings (same Title + Category + Schedule.Date)
        var siblings = await _context.AssessmentSessions
            .Include(a => a.Questions)
                .ThenInclude(q => q.Options)
            .Include(a => a.Responses)
            .Where(a =>
                a.Title == rep.Title &&
                a.Category == rep.Category &&
                a.Schedule.Date == scheduleDate)
            .ToListAsync();

        logger.LogInformation($"DeleteAssessmentGroup: deleting {siblings.Count} sessions for '{rep.Title}'");

        foreach (var session in siblings)
        {
            if (session.Responses.Any())
                _context.UserResponses.RemoveRange(session.Responses);

            if (session.Questions.Any())
            {
                var opts = session.Questions.SelectMany(q => q.Options).ToList();
                if (opts.Any()) _context.AssessmentOptions.RemoveRange(opts);
                _context.AssessmentQuestions.RemoveRange(session.Questions);
            }

            _context.AssessmentSessions.Remove(session);
        }

        await _context.SaveChangesAsync();

        logger.LogInformation($"DeleteAssessmentGroup: successfully deleted group '{rep.Title}'");
        return Json(new { success = true, message = $"Assessment '{rep.Title}' and all {siblings.Count} assignment(s) deleted." });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"DeleteAssessmentGroup error for representative {representativeId}: {ex.Message}");
        return Json(new { success = false, message = $"Failed to delete assessment group: {ex.Message}" });
    }
}
```
  </action>
  <verify>Build the project: `dotnet build` must produce 0 errors. If anonymous type causes issues with the view cast, consider using `dynamic` cast in the view instead of a typed cast.</verify>
  <done>Project builds cleanly. ManagementData in ViewBag is a list of grouped anonymous objects, each with RepresentativeId, Users, AllIds. DeleteAssessmentGroup action exists and finds siblings by Title+Category+Schedule.Date.</done>
</task>

<task type="auto">
  <name>Task 2: Update manage tab card loop to render grouped data</name>
  <files>Views/CMP/Assessment.cshtml</files>
  <action>
**Replace the typed cast and card loop in the manage tab.**

**Line 85 — change the cast:**
```csharp
// OLD:
var managementData = ViewBag.ManagementData as List<HcPortal.Models.AssessmentSession> ?? new List<HcPortal.Models.AssessmentSession>();

// NEW — anonymous types can't be cast, use dynamic list:
var managementData = ViewBag.ManagementData as IEnumerable<dynamic> ?? Enumerable.Empty<dynamic>();
```

**Line 94 — update the badge count to work with IEnumerable:**
```csharp
// OLD:
<span class="badge bg-secondary ms-1">@managementData.Count</span>

// NEW:
<span class="badge bg-secondary ms-1">@managementData.Count()</span>
```

**Line 111 — update empty check:**
```csharp
// OLD:
@if (managementData.Count == 0)

// NEW:
@if (!managementData.Any())
```

**Lines 137-260 — replace the entire `@foreach` card block.** The new loop uses `group` instead of `item`, replaces "Assigned to: single user" with a compact user badge list, and calls `confirmDeleteGroup` instead of `confirmDelete`.

Replace the foreach block with:
```csharp
@foreach (var group in managementData)
{
    var defaultBadgeClass = (string)group.Category switch
    {
        "OJT"                    => "bg-primary",
        "IHT"                    => "bg-success",
        "Training Licencor"      => "bg-danger",
        "OTS"                    => "bg-warning text-dark",
        "Mandatory HSSE Training" => "bg-info",
        "Proton"                 => "bg-purple",
        _                        => "bg-secondary"
    };
    var badgeClass = defaultBadgeClass;
    var statusBadgeClass = (string)group.Status switch
    {
        "Open"      => "bg-success",
        "Upcoming"  => "bg-warning text-dark",
        "Completed" => "bg-primary",
        _           => "bg-secondary"
    };
    var statusIcon = (string)group.Status switch
    {
        "Open"      => "bi-check-circle-fill text-success",
        "Upcoming"  => "bi-clock-fill text-warning",
        "Completed" => "bi-check-circle text-primary",
        _           => "bi-circle"
    };

    // Build user display: up to 3 names + "+N more"
    var userList = (IEnumerable<dynamic>)group.Users;
    var userCount = userList.Count();
    var visibleUsers = userList.Take(3).Select(u => (string)u.FullName).ToList();
    var hiddenCount = userCount - visibleUsers.Count;
    var userDisplay = string.Join(", ", visibleUsers) + (hiddenCount > 0 ? $" +{hiddenCount} more" : "");

    // Duration display
    int dur = (int)group.DurationMinutes;
    var dHours = dur / 60;
    var dMins  = dur % 60;
    var durationText = dHours > 0
        ? (dMins > 0 ? $"{dHours}h {dMins}m" : $"{dHours} hour{(dHours > 1 ? "s" : "")}")
        : $"{dMins} minute{(dMins > 1 ? "s" : "")}";

    <div class="col-12 col-md-6 col-lg-4 assessment-card" data-status="@((string)group.Status).ToLower()">
        <div class="card h-100 border-0 shadow-sm assessment-card-item">
            <div class="card-body d-flex flex-column p-4">
                <!-- Header: Category Badge + user count pill -->
                <div class="mb-3 d-flex align-items-center gap-2">
                    <span class="badge @badgeClass">@group.Category</span>
                    <span class="badge bg-light text-dark border">
                        <i class="bi bi-people me-1"></i>@userCount assigned
                    </span>
                </div>
                <!-- Title -->
                <h5 class="card-title fw-bold mb-3">@group.Title</h5>
                <!-- Assessment Info -->
                <div class="assessment-meta mb-3">
                    <div class="meta-item">
                        <i class="bi bi-calendar3"></i>
                        <span>@((DateTime)group.Schedule).ToString("dd MMM yyyy")</span>
                    </div>
                    <div class="meta-item">
                        <i class="bi bi-clock"></i>
                        <span>@durationText</span>
                    </div>
                    <div class="meta-item">
                        <i class="bi @statusIcon"></i>
                        <span>Status: @group.Status</span>
                    </div>
                    @if ((bool)group.IsTokenRequired)
                    {
                        <div class="meta-item">
                            <i class="bi bi-shield-lock text-danger"></i>
                            <span>Token Required</span>
                        </div>
                    }
                    else
                    {
                        <div class="meta-item">
                            <i class="bi bi-unlock text-success"></i>
                            <span>Open Access</span>
                        </div>
                    }
                </div>
                <!-- Assigned Users -->
                <div class="manage-info mb-3">
                    <div class="text-muted small mb-1">
                        <i class="bi bi-people me-1"></i><strong>Assigned to:</strong>
                    </div>
                    <div class="text-muted small">@userDisplay</div>
                    @if ((bool)group.IsTokenRequired && !string.IsNullOrEmpty((string)group.AccessToken))
                    {
                        <div class="d-flex align-items-center text-muted small mt-2">
                            <i class="bi bi-key me-2"></i>
                            <span><strong>Token:</strong> <code id="token-@group.RepresentativeId" class="text-primary">@group.AccessToken</code></span>
                            <button class="btn btn-sm btn-link p-0 ms-2" onclick="copyToken('@group.AccessToken', @group.RepresentativeId, event)" title="Copy Token">
                                <i class="bi bi-clipboard"></i>
                            </button>
                        </div>
                    }
                </div>
                <!-- Management Actions -->
                <div class="mt-auto">
                    <div class="d-flex gap-2 flex-wrap">
                        <a asp-action="EditAssessment" asp-route-id="@group.RepresentativeId" class="btn btn-sm btn-outline-primary flex-fill">
                            <i class="bi bi-pencil"></i> Edit
                        </a>
                        <a asp-action="ManageQuestions" asp-route-id="@group.RepresentativeId" class="btn btn-sm btn-outline-info flex-fill">
                            <i class="bi bi-list-check"></i> Questions
                        </a>
                    </div>
                    <div class="d-flex gap-2 flex-wrap mt-2">
                        @if ((bool)group.IsTokenRequired)
                        {
                            <button class="btn btn-sm btn-outline-warning flex-fill" onclick="regenerateToken(@group.RepresentativeId)">
                                <i class="bi bi-arrow-clockwise"></i> Regen Token
                            </button>
                        }
                        <button class="btn btn-sm btn-outline-danger flex-fill"
                                onclick="confirmDeleteGroup(@group.RepresentativeId, '@group.Title', @userCount)">
                            <i class="bi bi-trash"></i> Delete
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
}
```

**Update the pagination text** (around line 270) — "assessments" label already refers to `ViewBag.TotalCount` groups, so no change needed there.

**Replace the `confirmDelete` JS function** (around line 938-1002) with a new `confirmDeleteGroup` function that posts to `/CMP/DeleteAssessmentGroup` with `representativeId`. Keep the old `confirmDelete` function if it's still referenced elsewhere, but add the new one:

```javascript
// Confirm and delete assessment GROUP (all sibling sessions)
function confirmDeleteGroup(representativeId, title, userCount) {
    var confirmMessage = 'WARNING: DELETE ASSESSMENT\n\n';
    confirmMessage += 'Assessment: "' + title + '"\n\n';
    confirmMessage += 'This will permanently delete:\n';
    confirmMessage += '  The assessment for ALL ' + userCount + ' assigned user(s)\n';
    confirmMessage += '  All questions\n';
    confirmMessage += '  All user responses\n\n';
    confirmMessage += 'This action CANNOT be undone!\n\n';
    confirmMessage += 'Are you absolutely sure?';

    if (!confirm(confirmMessage)) {
        return;
    }
    if (!confirm('Final confirmation: Delete "' + title + '" for all ' + userCount + ' user(s)?')) {
        return;
    }

    var token = $('input[name="__RequestVerificationToken"]').val();
    if (!token) {
        alert('Security token not found. Please refresh the page and try again.');
        return;
    }

    var originalButton = event.target.closest('button');
    if (originalButton) {
        originalButton.disabled = true;
        originalButton.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Deleting...';
    }

    $.ajax({
        url: '/CMP/DeleteAssessmentGroup',
        type: 'POST',
        data: {
            representativeId: representativeId,
            __RequestVerificationToken: token
        },
        success: function(response) {
            if (response.success) {
                alert(response.message);
                location.reload();
            } else {
                alert('Error: ' + response.message);
                if (originalButton) {
                    originalButton.disabled = false;
                    originalButton.innerHTML = '<i class="bi bi-trash"></i> Delete';
                }
            }
        },
        error: function(xhr, status, error) {
            alert('Server error: ' + (xhr.responseText || error) + '\nPlease try again or contact administrator.');
            if (originalButton) {
                originalButton.disabled = false;
                originalButton.innerHTML = '<i class="bi bi-trash"></i> Delete';
            }
        }
    });
}
```
  </action>
  <verify>
1. `dotnet build` produces 0 errors.
2. Navigate to `/CMP/Assessment?view=manage` — management tab shows grouped cards (fewer cards than before if any assessment was assigned to multiple users).
3. Each card shows "X assigned" pill and the compact user list under "Assigned to:".
4. Click Delete on a grouped card — confirmation mentions the user count, posts to `/CMP/DeleteAssessmentGroup`, and removes all sibling sessions on success.
  </verify>
  <done>
- Management tab renders 1 card per unique (Title, Category, Schedule.Date) group.
- Card shows "N assigned" badge and comma-separated user names (truncated at 3 + "+N more").
- Edit and Questions buttons route to the representative session Id.
- Delete removes all sessions in the group and reloads the page.
- Pagination counts distinct groups via ViewBag.TotalCount.
  </done>
</task>

</tasks>

<verification>
1. `dotnet build` — 0 errors, 0 warnings about dynamic/anonymous type issues.
2. `/CMP/Assessment?view=manage` loads without runtime errors.
3. For an assessment assigned to 3 users: exactly 1 card appears (not 3), showing "3 assigned" pill and all 3 names.
4. For an assessment assigned to 5 users: card shows first 3 names + "+2 more".
5. Delete on a 3-user grouped card: confirmation dialog mentions "3 assigned user(s)", all 3 sessions are removed, page reloads to show 0 cards for that assessment.
6. Pagination: TotalPages/TotalCount reflects group count, not row count.
</verification>

<success_criteria>
- 0 compilation errors after both file changes.
- Manage tab card count = number of distinct (Title+Category+Schedule.Date) groups.
- Each card's "Assigned to" section lists actual assigned users, not a single user.
- Delete on any card triggers DeleteAssessmentGroup and removes all sibling sessions.
</success_criteria>

<output>
After completion, create `.planning/quick/5-group-manage-view-cards-by-assessment/SUMMARY.md` summarizing what was changed and any deviations from the plan.
</output>
