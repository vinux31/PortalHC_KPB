# Phase 31: HC Reporting Actions - Research

**Researched:** 2026-02-23
**Domain:** Excel export of assessment results and bulk session force-close operations
**Confidence:** HIGH

## Summary

Phase 31 extends HC monitoring capabilities with two complementary actions: (1) **Excel results export** allowing HC to download a comprehensive report of all workers assigned to an assessment with pass/fail status and completion time, and (2) **Bulk force-close** enabling HC to transition all Open/InProgress sessions to Abandoned in a single click rather than per-worker actions.

The research confirms this phase is technically straightforward because the codebase already has proven patterns in place: **ClosedXML 0.105.0** is installed and used in Phase 2 (ExportAnalyticsResults in CDPController), **ForceCloseAssessment** exists as a per-session action (Phase 22), **AuditLogService** is standardized for audit logging (writes immediately), and the monitoring detail page has proper UI structure (AssessmentMonitoringDetail.cshtml) with session data in MonitoringGroupViewModel and MonitoringSessionViewModel.

Key architectural decisions: (1) Export action queries all workers assigned to an assessment regardless of completion status (includes completed, open, and not-started), (2) Force close reuses existing session status transition logic to Abandoned (preserves existing pattern), (3) Both actions use existing authorization (HC/Admin), follow hidden form POST + confirm() pattern, and log via AuditLogService after SaveChangesAsync completes.

**Primary recommendation:** Port ExportAnalyticsResults pattern from CDPController to CMPController as ExportAssessmentResults (scoped to single assessment group), add ForceCloseAll action that loops through sessions matching group (Title + Category + Schedule.Date) transitioning Open/InProgress → Abandoned, add both buttons to AssessmentMonitoringDetail.cshtml with confirmation, and ensure audit logs use existing "NIP - FullName" naming convention.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | 0.105.0 | Excel file generation and export | Already installed in project, proven in CDPController.ExportAnalyticsResults, MIT license, handles up to 100k rows efficiently |
| Entity Framework Core | 8.0 | Querying assessment groups and sessions | Already in stack, used throughout for assessments and audit logs |
| ASP.NET Core Identity | (installed) | Authorization (HC/Admin roles) | Standard auth system already enforced in CMPController, all existing actions use [Authorize(Roles = "Admin, HC")] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap 5 | (via CDN) | UI buttons and modals for confirmation | Already in project, used for all action buttons in monitoring detail |
| No external packages needed | — | Both actions use built-in ASP.NET features only | Hidden form POST pattern + JavaScript confirm() is sufficient |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ClosedXML | EPPlus 5+ or OpenXML SDK | ClosedXML is already proven in project; alternatives would require re-evaluation and testing |
| AuditLogService.LogAsync calls | Custom audit logging | Service is standardized, simple, and already integrated; no benefit to custom approach |
| Per-group force-close loop | Batch update with EF Core | Current per-session loop allows reusing ForceCloseAssessment update logic and ensures consistency |

**Installation:** No new packages required. ClosedXML is already in csproj.

## Architecture Patterns

### Recommended Project Structure
```
Controllers/
├── CMPController.cs                         # Add ExportAssessmentResults & ForceCloseAll actions

Views/CMP/
├── AssessmentMonitoringDetail.cshtml        # Add Export Results button and Force Close All button
└── (no new views required)

Services/
├── AuditLogService.cs                       # (existing, no changes)

Models/
└── (no new models required - use existing MonitoringGroupViewModel)
```

### Pattern 1: Scoped Excel Export (Assessment Group Level)

**What:** Query all sessions in a group (matched by Title + Category + Schedule.Date), project to include completion status for all workers whether completed or not, generate workbook with one row per session including Name, NIP, Package (if applicable), Score, Pass/Fail, Completion Time.

**When to use:** For assessment-level results export where HC needs full roster with progress status.

**Example flow (from Phase 2 ExportAnalyticsResults adapted):**
```csharp
// Source: CDPController.ExportAnalyticsResults pattern (lines 507-605)
public async Task<IActionResult> ExportAssessmentResults(
    string title,
    string category,
    DateTime scheduleDate)
{
    // 1. Query all sessions in this group (all workers assigned)
    var sessionGroup = await _context.AssessmentSessions
        .Include(a => a.User)
        .Where(a => a.Title == title &&
                    a.Category == category &&
                    a.Schedule.Date == scheduleDate)
        .Select(a => new {
            a.Id,
            UserName = a.User != null ? a.User.FullName : "Unknown",
            NIP = a.User != null ? a.User.NIP ?? "" : "",
            a.Score,
            a.IsPassed,
            a.CompletedAt
        })
        .ToListAsync();

    // 2. Generate workbook (ClosedXML pattern from Phase 2)
    using var workbook = new XLWorkbook();
    var worksheet = workbook.Worksheets.Add("Results");

    // Header row
    worksheet.Cell(1, 1).Value = "Worker Name";
    worksheet.Cell(1, 2).Value = "NIP";
    worksheet.Cell(1, 3).Value = "Score";
    worksheet.Cell(1, 4).Value = "Status";
    worksheet.Cell(1, 5).Value = "Completed At";

    var headerRange = worksheet.Range(1, 1, 1, 5);
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

    // Data rows (include both completed and not-completed)
    for (int i = 0; i < sessionGroup.Count; i++) {
        var session = sessionGroup[i];
        var row = i + 2;
        worksheet.Cell(row, 1).Value = session.UserName;
        worksheet.Cell(row, 2).Value = session.NIP;
        worksheet.Cell(row, 3).Value = session.Score ?? 0;
        worksheet.Cell(row, 4).Value = session.IsPassed == true ? "Pass" :
                                        session.IsPassed == false ? "Fail" : "Not Started";
        worksheet.Cell(row, 5).Value = session.CompletedAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
    }

    worksheet.Columns().AdjustToContents();

    // 3. Return file download
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var content = stream.ToArray();
    var fileName = $"{title}_{scheduleDate:yyyyMMdd}_Results.xlsx";
    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
}
```

### Pattern 2: Bulk Status Transition with Audit Logging

**What:** Loop through all sessions matching the group criteria, transition Open/InProgress → Abandoned (preserving existing ForceCloseAssessment logic), write one audit log entry per session using AuditLogService.

**When to use:** For bulk actions that must maintain consistency with existing per-item logic and full audit trail.

**Key Detail:** AuditLogService.LogAsync calls SaveChangesAsync internally, so audit is written immediately after session update completes.

**Example flow (from ForceCloseAssessment pattern at line 550-597):**
```csharp
// Source: CMPController.ForceCloseAssessment pattern (lines 550-597)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ForceCloseAll(
    string title,
    string category,
    DateTime scheduleDate)
{
    // 1. Find all sessions in this group
    var sessionsToClose = await _context.AssessmentSessions
        .Where(a => a.Title == title &&
                    a.Category == category &&
                    a.Schedule.Date == scheduleDate &&
                    (a.Status == "Open" || a.Status == "InProgress"))
        .ToListAsync();

    if (!sessionsToClose.Any()) {
        TempData["Error"] = "No Open or InProgress sessions to close.";
        return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate });
    }

    // 2. Update each session (reuse ForceCloseAssessment logic)
    foreach (var session in sessionsToClose) {
        session.Status = "Abandoned";  // Transition status
        session.UpdatedAt = DateTime.UtcNow;
    }

    await _context.SaveChangesAsync();

    // 3. Audit log (ONE entry summarizing the bulk action, OR one per session for detail)
    // Current recommendation: ONE summary entry for bulk action
    var user = await _userManager.GetUserAsync(User);
    var actorName = $"{user?.NIP ?? "?"} - {user?.FullName ?? "Unknown"}";
    await _auditLog.LogAsync(
        user?.Id ?? "",
        actorName,
        "ForceCloseAll",
        $"Force-closed all Open/InProgress sessions for '{title}' (Category: {category}, Date: {scheduleDate:yyyy-MM-dd}) — {sessionsToClose.Count} session(s) closed",
        null,
        null);

    TempData["Success"] = $"Successfully closed {sessionsToClose.Count} session(s).";
    return RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate });
}
```

### Pattern 3: UI Confirmation with Hidden Form POST

**What:** Add button to monitoring detail view that triggers hidden form POST with antiforgery token and JavaScript confirm() prompt.

**When to use:** For destructive bulk actions that must prevent accidental execution.

**Example (Razor syntax, from AssessmentMonitoringDetail.cshtml pattern):**
```html
<!-- Force Close All button (top of Per-User Status section) -->
<form asp-action="ForceCloseAll" method="post" class="d-inline"
      onsubmit="return confirm('Force close all Open/InProgress sessions? This cannot be undone.')">
    @Html.AntiForgeryToken()
    <input type="hidden" name="title" value="@Model.Title" />
    <input type="hidden" name="category" value="@Model.Category" />
    <input type="hidden" name="scheduleDate" value="@Model.Schedule.Date.ToString("yyyy-MM-dd")" />
    <button type="submit" class="btn btn-danger btn-sm">
        <i class="bi bi-x-circle me-1"></i>Force Close All
    </button>
</form>

<!-- Export Results button (top of Per-User Status section) -->
<form asp-action="ExportAssessmentResults" method="get" class="d-inline">
    <input type="hidden" name="title" value="@Model.Title" />
    <input type="hidden" name="category" value="@Model.Category" />
    <input type="hidden" name="scheduleDate" value="@Model.Schedule.Date.ToString("yyyy-MM-dd")" />
    <button type="submit" class="btn btn-success btn-sm">
        <i class="bi bi-download me-1"></i>Export Results
    </button>
</form>
```

### Anti-Patterns to Avoid
- **Querying all sessions globally then filtering in memory:** Use LINQ Where() at DB level to avoid loading unnecessary rows
- **Writing audit logs before SaveChangesAsync completes:** Use AuditLogService.LogAsync which saves immediately (pattern from Phase 22)
- **Hardcoding role checks instead of [Authorize(Roles = "...")]:** Existing actions use attributes consistently
- **Adding buttons/forms directly to view without testing export filename:** ClosedXML filename strategy is proven (see Phase 2); reuse pattern

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel file generation | Custom Open XML markup or CSV generation | ClosedXML (0.105.0 already installed) | ClosedXML abstracts complex Open XML format; CSV lacks formatting for professional reports |
| Session status bulk updates | Custom loop with manual EF tracking | EF Core SaveChangesAsync after loop (proven in existing code) | EF Core handles change tracking; manual tracking leads to missed updates or orphaned changes |
| Audit logging | Custom DB inserts or file logging | AuditLogService (existing pattern) | Service ensures consistent actor name format ("NIP - FullName"), standardized CreatedAt timestamp, and immediate persistence |
| Confirmation UI | Custom JavaScript dialogs or confirmation page | HTML form + confirm() (existing pattern in AssessmentMonitoringDetail.cshtml) | Pattern is proven, antiforgery token is automatic, no additional dependencies |

**Key insight:** This phase reuses proven patterns from Phase 2 (Excel export) and Phase 22 (bulk session updates) to maintain consistency and reduce custom code. The codebase is mature enough that ad-hoc solutions create maintenance burden.

## Common Pitfalls

### Pitfall 1: Exporting Only Completed Sessions Instead of All Assigned
**What goes wrong:** Developer misreads requirement and exports only rows where Score is not null or Status == "Completed", missing workers who were assigned but haven't started or abandoned.

**Why it happens:** Excel export language naturally suggests "results," which implies completion. Requirement says "all workers assigned to the assessment."

**How to avoid:** Explicitly confirm export query includes `.Where(a => a.Title == title && a.Category == category && a.Schedule.Date == scheduleDate)` without status filter. Display "Not Started" or empty score for unstarted workers.

**Warning signs:**
- Export row count is significantly less than total workers shown in monitoring table
- Unstarted workers are missing from exported file
- Verification: Count rows in exported file vs. TotalCount in monitoring summary

### Pitfall 2: Force Close Updates Only Without Audit Log Entry
**What goes wrong:** Developer adds status update for Open/InProgress sessions but forgets to call AuditLogService.LogAsync, creating untraced bulk action in audit log.

**Why it happens:** Copy-paste error or misreading existing ForceCloseAssessment pattern (single session) vs. new bulk requirement.

**How to avoid:** After SaveChangesAsync completes, immediately call `_auditLog.LogAsync(user?.Id, actorName, "ForceCloseAll", description, null, null)` with summary or per-session logging. Verify in AuditLog table.

**Warning signs:**
- AuditLog has no entry for the timestamp when Force Close All was executed
- New session statuses appear without corresponding audit trail

### Pitfall 3: Force Close Group Query Doesn't Match Monitoring Display Logic
**What goes wrong:** ForceCloseAll query uses different group criteria (e.g., only checks Title) than GetMonitorData (which groups by Title + Category + Schedule.Date), causing HC to close unintended sessions or miss some.

**Why it happens:** Copy-paste from GetMonitorData but incomplete refactoring of Where clause.

**How to avoid:** Exact match group key: `a.Title == title && a.Category == category && a.Schedule.Date == scheduleDate`. Test with same assessment appearing multiple times (different dates/categories) to verify isolation.

**Warning signs:**
- Force Close affects sessions outside the monitoring detail view group
- Sessions in different categories with same title are affected together

### Pitfall 4: Excel File Memory Issues with Very Large Groups
**What goes wrong:** HC attempts to export assessment with 10k+ workers, Excel generation consumes excessive memory causing timeout or crash.

**Why it happens:** Entire result set loaded into MemoryStream before file download; no streaming or chunking.

**How to avoid:** ClosedXML 0.105.0 handles up to 100k rows efficiently per Phase 2 research. If exceeding this, implement pagination or error messaging. Phase 2 caps exports at 10k rows (see ExportAnalyticsResults line 544 `var maxExportRows = 10000`); consider same cap for consistency.

**Warning signs:**
- Export hangs for assessments with very large worker assignments
- Memory spikes during export process
- Verification: Test with realistic data volume (e.g., 5k+ workers)

### Pitfall 5: Antiforgery Token Missing in Export Link
**What goes wrong:** Export button is GET request but missing antiforgery validation; if exported during CSRF attack, HC data could be exposed via redirect to attacker URL.

**Why it happens:** GET requests don't typically need antiforgery tokens; developer assumes export is "safe" and forgets that action parameters could be manipulated.

**How to avoid:** Export action is GET (following Phase 2 CDPController.ExportAnalyticsResults which is HttpGet), but parameters come from hidden form inputs or verified route. Alternatively, make ExportAssessmentResults POST with antiforgery token. Current pattern (GET) is acceptable if assessmentId comes from monitoring context.

**Warning signs:**
- Export GET action doesn't validate that requesting HC can access this assessment
- Parameters are unconstrained user input

## Code Examples

Verified patterns from existing codebase:

### Excel Export Workbook Structure (ClosedXML)
```csharp
// Source: CDPController.ExportAnalyticsResults (lines 561-605)
using var workbook = new XLWorkbook();
var worksheet = workbook.Worksheets.Add("Assessment Results");

// Header
worksheet.Cell(1, 1).Value = "Assessment Title";
worksheet.Cell(1, 2).Value = "Category";
// ... more columns ...

// Style header
var headerRange = worksheet.Range(1, 1, 1, 9);
headerRange.Style.Font.Bold = true;
headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

// Data rows
for (int i = 0; i < results.Count; i++) {
    var r = results[i];
    var row = i + 2;
    worksheet.Cell(row, 1).Value = r.AssessmentTitle;
    // ... more columns ...
}

worksheet.Columns().AdjustToContents();

using var stream = new MemoryStream();
workbook.SaveAs(stream);
var content = stream.ToArray();
var fileName = $"AssessmentResults_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
```

### Session Status Update with Audit Log
```csharp
// Source: CMPController.ForceCloseAssessment (lines 550-597)
assessment.Status = "Abandoned";  // or "Completed" depending on logic
assessment.UpdatedAt = DateTime.UtcNow;

await _context.SaveChangesAsync();

var user = await _userManager.GetUserAsync(User);
var actorName = $"{user?.NIP ?? "?"} - {user?.FullName ?? "Unknown"}";
await _auditLog.LogAsync(
    user?.Id ?? "",
    actorName,
    "ForceCloseAssessment",
    $"Force-closed assessment '{assessment.Title}' for user {assessment.UserId} [ID={id}]",
    id,
    "AssessmentSession");
```

### Authorization and Role Check
```csharp
// Source: CMPController pattern (line 548)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ForceCloseAssessment(int id)
{
    // Body...
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| N/A (new feature) | ClosedXML for Excel export | Established in Phase 2 (2026-02-14) | Standardized Excel generation across project |
| N/A (new feature) | Per-group force-close via hidden form POST | Established in Phase 22 | Consistent UI pattern for destructive actions |
| — | AuditLogService for standardized audit logging | Established across all sensitive actions | Ensures audit trail consistency and immediate persistence |

**Deprecated/outdated:**
- None identified for this phase; patterns are current.

## Open Questions

1. **Force Close All: One Summary Audit Entry vs. Per-Session Entries?**
   - **What we know:** AuditLogService writes immediately (SaveChangesAsync called inside LogAsync). Current per-session pattern (ForceCloseAssessment) creates one entry per action invocation.
   - **What's unclear:** Should ForceCloseAll create ONE audit entry summarizing bulk action (e.g., "Closed 25 sessions") or MULTIPLE entries (one per session)?
   - **Recommendation:** Create ONE summary audit entry with session count and group parameters. More concise and reduces audit log bloat. Can be adjusted if HC audit team requests per-session detail.

2. **Export Filename: Include Score Timestamp or Assessment Date?**
   - **What we know:** CDPController uses `DateTime.Now` in filename (line 603). Requirements don't specify.
   - **What's unclear:** Should filename be `{Title}_{ScheduleDate}_Results.xlsx` or `{Title}_{ExportDate}_Results.xlsx`?
   - **Recommendation:** Use ScheduleDate (when assessment was scheduled) for better org by assessment batch. Matches Phase 2 pattern if called repeatedly.

3. **Export Column Order: Score Before Pass/Fail or After?**
   - **What we know:** Phase 2 shows Score then Pass/Fail. Requirements don't specify order.
   - **What's unclear:** Is column order Name, NIP, Package, Score, Pass/Fail, CompletedAt OR Name, NIP, Pass/Fail, Score, Package, CompletedAt?
   - **Recommendation:** Name, NIP, Package (if applicable), Score, Status (Pass/Fail/Not Started), Completed At. Logical flow from identification to results to timestamp.

4. **Package Column in Export: Include or Omit?**
   - **What we know:** Monitoring detail shows PackageName for package mode assessments. Requirements mention "score, pass/fail, completion time" but not package.
   - **What's unclear:** Should export include AssignedPackage column? If so, should it be "—" for non-package assessments or omitted entirely?
   - **Recommendation:** Include Package column with "—" or empty for non-package assessments. Provides full context for HC reviewing results (useful for diagnosing failed assessments). Consistency with monitoring detail display.

## Sources

### Primary (HIGH confidence)
- **ClosedXML 0.105.0** — Verified in C:/Users/rinoa/Desktop/PortalHC_KPB/HcPortal.csproj; used in CDPController.ExportAnalyticsResults (lines 507-605)
- **AuditLogService pattern** — Reviewed in C:/Users/rinoa/Desktop/PortalHC_KPB/Services/AuditLogService.cs (lines 1-45); called after SaveChangesAsync in existing actions
- **ForceCloseAssessment implementation** — Reviewed in C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs (lines 550-597); exact pattern for status transition and audit logging
- **AssessmentMonitoringDetail UI** — Reviewed in C:/Users/rinoa/Desktop/PortalHC_KPB/Views/CMP/AssessmentMonitoringDetail.cshtml (lines 1-381); existing form POST pattern with confirmation
- **GetMonitorData group logic** — Reviewed in CMPController.cs (lines 277-370); establishes Title + Category + Schedule.Date as group key
- **EntityFramework Core 8** — Installed and verified in project; used throughout for session and audit queries

### Secondary (MEDIUM confidence)
- **ASP.NET Core MVC authorization patterns** — Standard [Authorize(Roles = "...")] used consistently across CMPController
- **Hidden form POST + confirm() pattern** — Verified in AssessmentMonitoringDetail.cshtml (lines 221-228 ForceCloseAssessment form) and throughout

### Tertiary (LOW confidence)
- None identified; all findings verified against codebase or official documentation

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** — ClosedXML and patterns verified in running codebase
- Architecture: **HIGH** — ForceCloseAssessment and Excel export patterns are proven in Phase 22 and Phase 2 respectively
- Pitfalls: **HIGH** — Based on patterns observed in existing code and common export/bulk-update mistakes

**Research date:** 2026-02-23
**Valid until:** 2026-03-09 (16 days - patterns are stable, no breaking changes expected)

**Files examined:**
- C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs (4393 lines)
- C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CDPController.cs (ExportAnalyticsResults)
- C:/Users/rinoa/Desktop/PortalHC_KPB/Services/AuditLogService.cs
- C:/Users/rinoa/Desktop/PortalHC_KPB/Views/CMP/AssessmentMonitoringDetail.cshtml
- C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AssessmentMonitoringViewModel.cs
- C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AssessmentSession.cs
- C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AuditLog.cs
- C:/Users/rinoa/Desktop/PortalHC_KPB/Models/UserPackageAssignment.cs
- C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AssessmentPackage.cs
- C:/Users/rinoa/Desktop/PortalHC_KPB/HcPortal.csproj (dependency verification)
