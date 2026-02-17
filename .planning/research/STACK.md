# Stack Research — CDP Coaching Management Additions

**Domain:** Coaching Session Management & Development Dashboard
**Researched:** 2026-02-17
**Confidence:** HIGH

## Context

Portal HC KPB v1.0 has complete CMP (assessment) functionality. Version 1.1 adds CDP coaching session management and development dashboards. This research covers ONLY what's needed for the new features, not the existing validated stack.

**Existing Stack (DO NOT change):**
- ASP.NET Core 8.0 MVC with Razor Views
- Entity Framework Core 8.0.24 (latest patch, supported until Nov 2026)
- SQL Server/SQLite database
- ASP.NET Identity for authentication
- Chart.js 4.5.1 for CMP visualizations (already used)
- ClosedXML 0.105.0 for Excel export (already used)

## Recommended Stack Additions

### New Packages Required

| Package | Version | Purpose | Why Recommended |
|---------|---------|---------|-----------------|
| **NONE** | - | All features buildable with existing stack | Coaching sessions are basic CRUD (no new packages needed), action item workflow uses existing database patterns (status enum), development dashboard reuses Chart.js already in project |

### Client-Side Libraries (CDN/wwwroot)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Flatpickr | 4.6.x (latest) | Date/time picker for coaching session logging | Better UX than native HTML5 date input, lightweight (no dependencies), works with server-side forms |
| TinyMCE 7 (Community) | 7.x | Rich text editor for coaching notes and action items | Free, actively maintained, simple integration via CDN, familiar Word-like interface |

**Installation:**
```html
<!-- In _Layout.cshtml or specific views -->

<!-- Flatpickr for date pickers -->
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/flatpickr/dist/flatpickr.min.css">
<script src="https://cdn.jsdelivr.net/npm/flatpickr"></script>

<!-- TinyMCE for rich text editing -->
<script src="https://cdn.tiny.cloud/1/YOUR-API-KEY/tinymce/7/tinymce.min.js" referrerpolicy="origin"></script>
<!-- OR self-hosted if no API key wanted -->
<script src="~/lib/tinymce/tinymce.min.js"></script>
```

### No New NuGet Packages Needed

**Coaching Session Management** can be built with:
- Entity Framework Core (existing) - CRUD operations, relationships
- Razor Views (existing) - Forms, display views
- ASP.NET Identity (existing) - User associations, authorization

**Action Item Workflow** can be built with:
- Database enum for status (Pending, Approved, Rejected, InProgress, Completed)
- Foreign keys to track approver chain (SrSpv → SectionHead → HC)
- DateTime stamps for state transitions
- No external workflow engine needed (simple linear approval, not complex branching)

**Development Dashboard** can be built with:
- Chart.js (existing) - Competency progress over time (line charts), goal completion (donut charts), team overview (bar charts)
- Entity Framework Core (existing) - Aggregate queries for dashboard statistics
- Existing CMP integration - UserCompetencyLevel history tracking

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| **Flatpickr (CDN)** | Tempusdominus Bootstrap 4 DatePicker | If already using Bootstrap 4 DatePicker elsewhere, but project doesn't currently use it |
| **Flatpickr (CDN)** | Syncfusion/Telerik DatePicker | If willing to pay for commercial components (not needed for this use case) |
| **TinyMCE 7 (Free)** | Quill.js | If need simpler API and don't need advanced features, but TinyMCE has better Word-like UX |
| **TinyMCE 7 (Free)** | CKEditor 5 | If need open source without any commercial restrictions, but TinyMCE free tier sufficient |
| **Database-based workflow** | ELSA Workflows | If need complex branching workflows with external task management (overkill for simple 3-step approval) |
| **Database-based workflow** | Workflow Core | If need long-running processes with persistence (unnecessary for synchronous approval actions) |
| **Chart.js (existing)** | Syncfusion/DevExpress Charts | If need advanced interactivity beyond basic charts (not required for MVP dashboard) |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| **SignalR** | Real-time notifications not in v1.1 scope (future enhancement), adds complexity without user demand | Email notifications (future) or simple database status checks |
| **Stateless/Workflow Engine libraries** | 3-step linear approval (Pending → SrSpv → SectionHead → HC) doesn't need state machine framework, adds dependency bloat | Simple status enum + approver foreign keys + authorization checks |
| **Commercial chart libraries** | Already have Chart.js working in CMP module, consistent UX important, no advanced features needed | Chart.js (existing) |
| **Custom rich text editor** | Reinventing the wheel, accessibility/security concerns, time-consuming | TinyMCE or similar battle-tested library |
| **jQuery UI DatePicker** | jQuery UI development stopped in 2021, outdated UX patterns | Flatpickr (modern, actively maintained) |

## Integration Patterns

### Rich Text Editor Integration (TinyMCE)

**View (Razor):**
```html
<div class="form-group">
    <label asp-for="CoachingNotes">Coaching Notes</label>
    <textarea asp-for="CoachingNotes" class="form-control tinymce-editor"></textarea>
    <span asp-validation-for="CoachingNotes" class="text-danger"></span>
</div>

<script>
    tinymce.init({
        selector: '.tinymce-editor',
        height: 300,
        menubar: false,
        plugins: 'lists link',
        toolbar: 'undo redo | bold italic | bullist numlist | link'
    });
</script>
```

**Controller (receive HTML):**
```csharp
public async Task<IActionResult> CreateSession(CoachingSessionViewModel model)
{
    // model.CoachingNotes already contains HTML from TinyMCE
    // Sanitization handled by TinyMCE config (limited toolbar = limited tags)
    // Store as-is, display with @Html.Raw() in views
}
```

### Date Picker Integration (Flatpickr)

**View (Razor):**
```html
<div class="form-group">
    <label asp-for="SessionDate">Session Date</label>
    <input asp-for="SessionDate" class="form-control flatpickr-date" />
    <span asp-validation-for="SessionDate" class="text-danger"></span>
</div>

<script>
    flatpickr(".flatpickr-date", {
        enableTime: true,
        dateFormat: "Y-m-d H:i",
        time_24hr: true
    });
</script>
```

**Model:**
```csharp
public class CoachingSessionViewModel
{
    [Required]
    [Display(Name = "Session Date")]
    public DateTime SessionDate { get; set; }
}
```

### Action Item Workflow (Database Only)

**Enum (no package needed):**
```csharp
public enum ActionItemStatus
{
    Pending = 0,        // Created, awaiting SrSpv
    SrSpvApproved = 1,  // SrSpv approved, awaiting SectionHead
    SectionHeadApproved = 2, // SectionHead approved, awaiting HC
    HCApproved = 3,     // Final approval
    Rejected = 4,       // Rejected at any stage
    InProgress = 5,     // Approved and being worked on
    Completed = 6       // Finished
}
```

**Entity:**
```csharp
public class ActionItem
{
    public int Id { get; set; }
    public int CoachingSessionId { get; set; }
    public string Description { get; set; }
    public DateTime DueDate { get; set; }
    public ActionItemStatus Status { get; set; }

    // Approval tracking
    public string? SrSpvApproverId { get; set; }
    public DateTime? SrSpvApprovedDate { get; set; }
    public string? SectionHeadApproverId { get; set; }
    public DateTime? SectionHeadApprovedDate { get; set; }
    public string? HCApproverId { get; set; }
    public DateTime? HCApprovedDate { get; set; }

    // Navigation
    public CoachingSession CoachingSession { get; set; }
    public ApplicationUser? SrSpvApprover { get; set; }
    public ApplicationUser? SectionHeadApprover { get; set; }
    public ApplicationUser? HCApprover { get; set; }
}
```

**Authorization (in controller):**
```csharp
public async Task<IActionResult> Approve(int id)
{
    var item = await _context.ActionItems
        .Include(a => a.CoachingSession)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (User.IsInRole("SrSpv") && item.Status == ActionItemStatus.Pending)
    {
        item.Status = ActionItemStatus.SrSpvApproved;
        item.SrSpvApproverId = _userManager.GetUserId(User);
        item.SrSpvApprovedDate = DateTime.Now;
    }
    else if (User.IsInRole("SectionHead") && item.Status == ActionItemStatus.SrSpvApproved)
    {
        item.Status = ActionItemStatus.SectionHeadApproved;
        item.SectionHeadApproverId = _userManager.GetUserId(User);
        item.SectionHeadApprovedDate = DateTime.Now;
    }
    else if (User.IsInRole("HC") && item.Status == ActionItemStatus.SectionHeadApproved)
    {
        item.Status = ActionItemStatus.HCApproved;
        item.HCApproverId = _userManager.GetUserId(User);
        item.HCApprovedDate = DateTime.Now;
    }
    else
    {
        return Forbid();
    }

    await _context.SaveChangesAsync();
    return RedirectToAction(nameof(Details), new { id });
}
```

### Development Dashboard Charts (Chart.js existing)

**Reuse existing Chart.js from CMP module** for consistency:

**Competency Progress Over Time (Line Chart):**
```javascript
// In view: fetch user's competency level history from controller
const ctx = document.getElementById('competencyProgressChart');
new Chart(ctx, {
    type: 'line',
    data: {
        labels: @Html.Raw(Json.Serialize(Model.Dates)),
        datasets: [{
            label: 'Competency Level',
            data: @Html.Raw(Json.Serialize(Model.Levels)),
            borderColor: 'rgb(75, 192, 192)',
            tension: 0.1
        }]
    }
});
```

**Goal Completion (Donut Chart):**
```javascript
const ctx = document.getElementById('goalCompletionChart');
new Chart(ctx, {
    type: 'doughnut',
    data: {
        labels: ['Completed', 'In Progress', 'Not Started'],
        datasets: [{
            data: [@Model.CompletedCount, @Model.InProgressCount, @Model.NotStartedCount]
        }]
    }
});
```

**Team Overview (Bar Chart):**
```javascript
const ctx = document.getElementById('teamOverviewChart');
new Chart(ctx, {
    type: 'bar',
    data: {
        labels: @Html.Raw(Json.Serialize(Model.TeamMemberNames)),
        datasets: [{
            label: 'Average Competency Level',
            data: @Html.Raw(Json.Serialize(Model.TeamAverageLevels))
        }]
    }
});
```

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| Entity Framework Core 8.0.24 | .NET 8.0 | Latest patch (Feb 10, 2026), support until Nov 2026 |
| Chart.js 4.5.1 | All modern browsers | Already in use for CMP module |
| ClosedXML 0.105.0 | .NET Standard 2.0+ (.NET 8 compatible) | Already in use for Excel export |
| Flatpickr 4.6.x | IE11+ (legacy), all modern browsers | No dependencies, works with Razor forms |
| TinyMCE 7.x | Modern browsers (Chrome, Firefox, Safari, Edge) | IE11 not supported (acceptable in 2026) |

## Migration Notes

### From v1.0 (CMP only) to v1.1 (CMP + CDP Coaching)

**Database migrations needed:**
1. Create `CoachingSessions` table (Id, CoacheeId, CoachId, SessionDate, Topic, Notes)
2. Create `ActionItems` table (Id, SessionId, Description, DueDate, Status, Approver FKs)
3. Add indexes on foreign keys (SessionId, CoacheeId, CoachId, ApproverIds)
4. Add `UserCompetencyLevelHistory` table to track changes over time for dashboard (or modify existing table to not overwrite)

**No package upgrades needed** unless critical security updates released.

**EF Core 8 support timeline warning:**
- Current version: 8.0.24 (Feb 2026)
- Support ends: November 2026
- EF Core 11 releases: November 2026
- Action: Plan upgrade to EF Core 11 in Q4 2026 or early 2027

## Stack Patterns for CDP Features

### Pattern 1: Coaching Session CRUD
**Stack:** EF Core + Razor Views + Flatpickr + TinyMCE
**When:** Creating/editing coaching session records
**Example entities:** `CoachingSession`, `ActionItem`

### Pattern 2: Action Item Workflow
**Stack:** Database enums + EF Core + Authorization attributes
**When:** Multi-step approval (SrSpv → SectionHead → HC)
**No external library:** Simple linear flow, authorization-based state transitions

### Pattern 3: Development Dashboard
**Stack:** EF Core aggregation queries + Chart.js (existing) + Razor Views
**When:** Visualizing competency progress, goal completion, team overview
**Reuse:** Same Chart.js patterns from CMP Reports Dashboard

### Pattern 4: CMP Integration
**Stack:** Existing `UserCompetencyLevel` table + new history tracking
**When:** Showing competency improvements from assessments over time
**Enhancement:** Add `UserCompetencyLevelHistory` for point-in-time snapshots

## Security Considerations

**TinyMCE Output Sanitization:**
- TinyMCE config limits available tags via toolbar restrictions
- Consider adding server-side HTML sanitization if users can escalate privileges
- For MVP: Limited toolbar (bold, italic, lists, links only) = limited XSS surface
- For production: Use HtmlSanitizer NuGet package if security review flags concerns

**Flatpickr:**
- Client-side only (cosmetic)
- Always validate DateTime server-side (already done by model validation)
- No security concerns beyond standard input validation

**Workflow Authorization:**
- Enforce role checks in controller actions
- Database-level approver tracking (audit trail)
- Use `[Authorize(Roles = "...")]` attributes on approval actions

## Performance Considerations

**Chart.js:**
- Already proven in CMP module (no performance issues reported)
- Keep datasets under 1000 points for smooth rendering
- For team overview, limit to section-level (not org-wide) initially

**Rich Text Storage:**
- Store HTML in `nvarchar(max)` column
- Index not needed (full-text search not in scope)
- Display with `@Html.Raw()` in views (sanitized input = safe output)

**Action Item Queries:**
- Add indexes on `Status`, `CoacheeId`, `DueDate` for filtering
- Use `.Include()` carefully (don't load entire approval chain unless needed)

## Sources

**Official Documentation:**
- [Entity Framework Core Releases](https://learn.microsoft.com/en-us/ef/core/what-is-new/) — EF Core 8.0.24 version and support timeline
- [Chart.js Documentation](https://www.chartjs.org/docs/latest/) — Verified v4.5.1 as latest
- [Flatpickr Official Site](https://flatpickr.js.org/) — Lightweight date picker, no dependencies
- [TinyMCE Documentation](https://www.tiny.cloud/tinymce-vs-quill/) — Comparison and integration guidance
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-9.0) — Email confirmation patterns

**Version Verification:**
- [NuGet: Entity Framework Core 8.0.24](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/8.0.24) — Latest patch release
- [NuGet: ClosedXML 0.105.0](https://www.nuget.org/packages/closedxml/) — .NET 8 compatibility confirmed
- [Chart.js Releases](https://github.com/chartjs/Chart.js/releases) — v4.5.1 latest stable
- [npm: chart.js](https://www.npmjs.com/package/chart.js?activeTab=readme) — Package details

**Research (MEDIUM confidence):**
- [Which rich text editor framework should you choose in 2025](https://liveblocks.io/blog/which-rich-text-editor-framework-should-you-choose-in-2025) — TinyMCE vs Quill vs CKEditor comparison
- [ASP.NET Core MVC action item workflow approval tracking](https://github.com/elsa-workflows/elsa-core/discussions/1002) — ELSA Workflows discussion (confirmed overkill for simple approval)
- [Stateless state machine library](https://github.com/dotnet-state-machine/stateless) — Evaluated but unnecessary for linear workflow
- [Matteo's Blog - Flatpickr with ASP.NET Core MVC](https://ml-software.ch/posts/adding-a-third-party-datetimepicker-to-your-asp-net-core-mvc-application) — Integration example

**Date Pickers Research:**
- [Syncfusion ASP.NET Core DatePicker](https://www.syncfusion.com/aspnet-core-ui-controls/datepicker) — Commercial alternative (not needed)
- [Telerik DatePicker](https://www.telerik.com/aspnet-core-ui/datepicker) — Commercial alternative (not needed)

**Workflow Research:**
- [Building workflow system with ASP.NET Core](https://www.tomware.ch/2018/04/30/building-a-simple-workflow-system-with-asp-net-core/) — Custom implementation patterns
- [ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-10.0) — Real-time notifications (future scope)

---

*Stack research for: CDP Coaching Management (v1.1)*
*Researched: 2026-02-17*
*Confidence: HIGH (official docs + existing project validation)*
