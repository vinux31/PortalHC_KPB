# Phase 49: Assessment Management Migration - Research

**Researched:** 2026-02-26
**Domain:** ASP.NET Core MVC — Admin CRUD consolidation, migrating manage operations from CMP to Admin portal, AuditLog relocation
**Confidence:** HIGH

<user_constraints>
## User Constraints (from Phase Description & Success Criteria)

### Locked Decisions

#### Assessment Management Consolidation
- **Source:** CMPController.Assessment with `view=manage` parameter (all manage operations currently there)
- **Target:** AdminController at `/Admin/ManageAssessment` (new dedicated page)
- **Operations to migrate:** Create, Edit, Delete (single & group), Reset, Force Close, Export, Monitoring (with detail view), User History
- **Current data location:** AuditLog currently in CMPController — must be moved to AdminController at `/Admin/AuditLog`

#### CMP/Assessment Page Becomes Personal View Only
- **Remove from CMP:** Manage toggle, manage-related UI elements, manage view mode
- **Keep in CMP:** Personal view mode only (workers see their own assessments)
- **Remove card from CMP Index:** "Manage Assessments" card (currently part of toggle)
- **Rename card in CMP Index:** "Assessment Lobby" → "My Assessments"

#### Admin Index Card Updates
- **Replace card:** "Assessment Competency Map" card (MDAT-03 stub) → "Manage Assessments" card linking to `/Admin/ManageAssessment`
- **Position:** Stays in Master Data section (Category A) where it currently lives

#### AuditLog Relocation
- **Source location:** CMPController.AuditLog (currently at `/CMP/AuditLog`)
- **Target location:** AdminController.AuditLog (must be at `/Admin/AuditLog`)
- **Scope:** Show ALL global audit entries (Assessment + other admin actions as they're added in phases 50-58)
- **Current entries:** CreateAssessment, EditAssessment, BulkAssign, DeleteAssessment, DeleteAssessmentGroup, ForceCloseAssessment, ResetAssessment

### Claude's Discretion
- Exact grouping logic for "representative" assessments (currently grouped by Title+Category+Schedule.Date) — verify if grouping remains in Manage view or becomes per-session rows
- UI layout for ManageAssessment page (card-based vs. table layout matching current CMP manage view or new design)
- Pagination strategy for Manage Assessments (current: 20 items/page in CMP manage view)

### Deferred Ideas (OUT OF SCOPE)
- None — all manage operations are phase scope

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MDAT-03 | **NOT the primary requirement for this phase** — MDAT-03 is Assessment Competency Map Manager (maps assessment categories to KKJ items). Phase 49 goal statement says "migrate Assessment Management" but success criterion #4 mentions replacing MDAT-03 stub card. **Clarification needed:** Confirm if Phase 49 is Assessment Management Migration (matching goal) OR Assessment Competency Map Manager (matching MDAT-03 requirement ID). | Research covers both: (A) Migration of manage operations from CMP to Admin (stated in goal), (B) MDAT-03 Assessment Competency Map CRUD if that's the actual scope. See Open Questions. |

</phase_requirements>

---

## Summary

Phase 49 presents a **scope clarification issue** that must be resolved before detailed planning:

**Goal statement says:** "Move Manage Assessments from CMP to Kelola Data (/Admin) — migrate all manage actions (Create, Edit, Delete, Reset, Force Close, Export, Monitoring, History) from CMPController to AdminController"

**But requirement ID says:** MDAT-03 = Assessment Competency Map Manager (maps assessment categories to KKJ items) — NOT Assessment Management

**Success criteria show both:**
- Criteria 1-6: Assessment Management consolidation (full scope of migration)
- Criteria 4: Replace "Assessment Competency Map" card with "Manage Assessments" card

**This indicates Phase 49 is Assessment Management Migration, not MDAT-03.** However, since MDAT-03 is the listed requirement, we must confirm:

1. **If Phase 49 IS Assessment Management Migration:** MDAT-03 requirement is misaligned; should be validated separately or deferred to Phase 49b
2. **If Phase 49 IS Assessment Competency Map Manager:** The goal/success criteria need revision, and assessment management migration belongs in a separate phase

**Assuming Phase 49 IS Assessment Management Migration** (per stated goal + success criteria alignment):

The technical scope is **well-defined and straightforward:** consolidate 8 existing CMPController operations into AdminController, make CMP Assessment personal-view-only, relocate AuditLog, update Index cards. No new database models needed — `AssessmentSession`, `AuditLog`, and all related entities already exist.

**Primary recommendation:** Confirm with stakeholder that Phase 49 is Assessment Management Migration (not MDAT-03 Assessment Competency Map Manager). If confirmed, proceed with 5-plan structure: (1) AdminController scaffold + ManageAssessment GET + view layout, (2) Create/Edit/Delete operations, (3) Reset/ForceClose/Export operations, (4) Monitoring + Monitoring Detail + History views, (5) CMP cleanup + AuditLog relocation + index card updates.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 (project target) | Controller + Razor views | Already the project framework |
| Entity Framework Core | 8.0.0 | DB queries for AssessmentSession, AuditLog | Already configured in project |
| Microsoft.AspNetCore.Identity | 8.0.0 | `[Authorize(Roles)]` authorization | Already in use across all controllers |
| Bootstrap | 5.3.0 (CDN) | Responsive page layout, card grids, pagination | Already in `_Layout.cshtml` |
| Bootstrap Icons | 1.10.0 (CDN) | Icons (bi-sliders, bi-plus, bi-trash, etc.) | Already in layout |
| jQuery | 3.7.1 (CDN) | AJAX calls for Reset/ForceClose/Export operations | Already in layout |
| ClosedXML | (current version in project) | Excel export for assessment results | Already used in CMPController.ExportAssessmentResults |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| AuditLogService | project class | Log admin Create/Update/Delete actions | Use for all write operations (same as CMPController) |
| IMemoryCache | .NET built-in | Cache filtering/search results if needed | Optional — only if manage page has performance issues with large datasets |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Copy current CMPController manage logic to AdminController | Refactor into shared service layer | Current approach keeps logic close to view; refactoring adds complexity — defer to Phase 60 consolidation if needed |
| Single ManageAssessment view with tabs (Management/Monitoring) | Separate pages (/Admin/ManageAssessment + /Admin/MonitoringAssessment) | Current CMP tab approach is familiar to users; splitting adds navigation complexity — keep tabs pattern |
| Per-session row layout in Manage grid | Grouped by (Title+Category+Schedule) as in current CMP | Current grouping reduces cognitive load — keep grouping |

**Installation:** No new packages needed. All dependencies already in project.

---

## Architecture Patterns

### Recommended Project Structure

Existing AdminController + new views:
```
Controllers/
  AdminController.cs              # Already exists — add ManageAssessment actions

Views/Admin/
  ManageAssessment.cshtml         # Main page with Management + Monitoring tabs (new)
  ManageAssessmentDetail.cshtml   # Monitoring detail modal or separate page (new)
  AuditLog.cshtml                 # Relocated from CMP (move from Views/CMP/AuditLog.cshtml)
```

**No new models needed** — `AssessmentSession`, `AuditLog`, `UserResponse` already exist.

### Pattern 1: Migration of Grouped Management View to AdminController

**What:** Current CMP Assessment manage view (grouped by Title+Category+Schedule.Date) becomes AdminController.ManageAssessment. Identical grouping logic, projection, and pagination.

**When to use:** Consolidating multi-level CRUD operations from one controller to another with minimal refactoring.

**Example:**
```csharp
// GET /Admin/ManageAssessment
[HttpGet]
public async Task<IActionResult> ManageAssessment(string? search, int page = 1, int pageSize = 20)
{
    var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

    var managementQuery = _context.AssessmentSessions
        .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
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
            UserFullName = a.User != null ? a.User.FullName : "Unknown",
            UserEmail = a.User != null ? a.User.Email : ""
        })
        .ToListAsync();

    // Group by (Title, Category, Schedule.Date)
    var grouped = allSessions
        .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
        .Select(g =>
        {
            var rep = g.OrderBy(a => a.CreatedAt).First();
            return new
            {
                Title = rep.Title,
                Category = rep.Category,
                Schedule = rep.Schedule,
                DurationMinutes = rep.DurationMinutes,
                Status = rep.Status,
                Users = g.Select(a => new { FullName = a.UserFullName, Email = a.UserEmail }).ToList(),
                AllIds = g.Select(a => a.Id).ToList()
            };
        })
        .OrderByDescending(g => g.Schedule)
        .ToList();

    // Pagination
    var totalCount = grouped.Count;
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    var managementData = grouped
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    ViewBag.ManagementData = managementData;
    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;

    return View();
}
```

### Pattern 2: Operation-Specific Actions (Reset, ForceClose, Export)

**What:** Separate action methods for each manage operation, each returning JSON (for AJAX) or redirect (for form submit).

**When to use:** Admin operations that need confirmation or async processing (Reset Session → clear user responses, ForceClose → set status to Completed, Export → generate Excel).

**Example:**
```csharp
// POST /Admin/ResetAssessment/{id}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ResetAssessment(int id)
{
    var assessment = await _context.AssessmentSessions.FindAsync(id);
    if (assessment == null)
        return Json(new { success = false, message = "Assessment not found" });

    // Delete all responses for this session
    var responses = _context.UserResponses.Where(r => r.SessionId == id);
    _context.UserResponses.RemoveRange(responses);

    // Reset session state
    assessment.Status = "Open";
    assessment.Progress = 0;
    assessment.IsPassed = null;
    assessment.Score = null;
    assessment.StartedAt = null;
    assessment.CompletedAt = null;
    assessment.ElapsedSeconds = 0;

    await _context.SaveChangesAsync();

    // Log action
    var user = await _userManager.GetUserAsync(User);
    await _auditLog.LogActionAsync(
        actorId: user.Id,
        actorName: $"{user.NIP} {user.FullName}",
        actionType: "ResetAssessment",
        description: $"Reset assessment '{assessment.Title}' for {assessment.User?.FullName}",
        targetId: id,
        targetType: "AssessmentSession"
    );

    return Json(new { success = true, message = "Assessment reset successfully" });
}
```

### Pattern 3: Monitoring Detail Modal or Page

**What:** Separate view or modal showing live progress of workers in an assessment session, with links to individual worker detail/history.

**When to use:** Real-time admin monitoring of assessment completion rates, worker status, elapsed time.

**Example:** MonitoringDetail.cshtml shows worker name, started time, current question, elapsed time, status with refresh capability.

### Pattern 4: AuditLog Relocation

**What:** Move current CMPController.AuditLog (at `/CMP/AuditLog`) to AdminController (at `/Admin/AuditLog`). View stays the same; only route and authorization change.

**When to use:** Consolidating all admin audit views into one controller as new admin tools are added (phases 50-58).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel export with formatted headers + data | Custom Excel library | ClosedXML (already in project) | ClosedXML handles styles, formulas, merged cells; custom code is fragile |
| Grouped pagination (grouping at view level, then paginating) | Calc pagination in controller | LINQ GroupBy + in-memory Skip/Take | EF Core doesn't paginate grouped results well; projection + grouping in memory is standard pattern |
| Admin action confirmation + logging | Manual alert + separate log call | ValidateAntiForgeryToken + AuditLogService | Prevents CSRF; logging service centralizes audit trail |
| Real-time progress monitoring | WebSocket or SignalR | Polling with 5-10s refresh + caching | Assessment sessions are not high-frequency updates; polling is simpler than SignalR infrastructure |

**Key insight:** Assessment Management is an existing feature with well-proven patterns. Migration is **refactoring the location and authorization**, not building new capability. Copy current logic; change routes, controller, and auth decorators.

---

## Common Pitfalls

### Pitfall 1: Losing Grouped View Logic During Migration

**What goes wrong:** Developer copies individual assessment operations but forgets the grouping query (Title+Category+Schedule.Date). Admin sees flat list of 500+ individual sessions instead of grouped view of 20 assessment campaigns.

**Why it happens:** Grouping logic is buried in the middle of the query projection; easy to miss when copy-pasting just the "Create" or "Edit" logic.

**How to avoid:** Copy entire Assessment() action from CMPController as a block first, test it works in AdminController, THEN remove the view toggle and personal-view branch.

**Warning signs:** Management view shows >50 rows when there should be <10 grouped campaigns; performance is slow (N+1 query against ungrouped sessions).

### Pitfall 2: Forgetting Auth on AuditLog — Exposing Sensitive Audit Data

**What goes wrong:** AuditLog page accidentally left at `/CMP/AuditLog` with `[Authorize]` (all authenticated users can access). Audit logs contain sensitive admin actions (who deleted what, when). Workers shouldn't see this.

**Why it happens:** During migration, AuditLog action is left in CMPController with broad `[Authorize]` instead of `[Authorize(Roles = "Admin")]`.

**How to avoid:** When moving AuditLog to AdminController, AdminController already has `[Authorize(Roles = "Admin")]` at class level. Action inherits it automatically. Remove old CMPController.AuditLog completely.

**Warning signs:** Audit page accessible from non-admin accounts; history shows admin-only actions visible to workers.

### Pitfall 3: CMP Assessment View Still Has "Manage" Toggle Button

**What goes wrong:** After migrating manage operations to Admin, CMPController.Assessment is updated to remove `view=manage` parameter handling. But view file (Assessment.cshtml) still renders "Manage Assessments" button that leads to `?view=manage`. Button now 404s or redirects back to personal view, confusing users.

**Why it happens:** View file is complex with nested conditionals for `viewMode == "manage"`. Developer removes server-side handling but forgets to update view markup.

**How to avoid:** Search for all `view == "manage"` and `view="manage"` in Assessment.cshtml. Delete entire conditional blocks that render manage UI. Replace card in CMP Index simultaneously.

**Warning signs:** Users report broken "Manage" button in Assessment lobby; manage-related UI elements still visible (Create Assessment, Reset buttons).

### Pitfall 4: Audit Log Entries Reference Old Routes

**What goes wrong:** Old audit log entries say "action performed at /CMP/AuditLog"; new entries say "/Admin/AuditLog". Inconsistent history makes debugging harder.

**Why it happens:** AuditLog entries don't record the route, only ActionType and Description. OK as-is; no code change needed. But confusion about where users navigate.

**How to avoid:** No code fix needed — audit entries are immutable. Just understand: old entries remain unchanged; all new actions log to AdminController.AuditLog. Routes are separate from audit data.

**Warning signs:** None — this is expected behavior. Old audit entries showing old routes is correct.

### Pitfall 5: Monitoring Detail View References CMPController Route

**What goes wrong:** ManageAssessmentDetail.cshtml has links to edit/reset/delete that still point to `@Url.Action("EditAssessment", "CMP")` instead of `@Url.Action("EditAssessment", "Admin")`. Links go to old routes.

**Why it happens:** When copying CMP view to Admin views, Razor helpers with controller name aren't updated.

**How to avoid:** Search all new Admin views for `asp-controller="CMP"`. Replace with `asp-controller="Admin"`. Test all links and buttons.

**Warning signs:** Clicking manage action buttons in new Admin page takes you back to old CMP routes; 404 if CMP action was deleted.

---

## Code Examples

Verified patterns from current project (CMPController.Assessment + AdminController.KkjMatrix):

### Example 1: Fetch Grouped Assessment Sessions for Management View

```csharp
// Source: CMPController.Assessment (manage branch) — confirmed working
var managementQuery = _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
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
        UserEmail = a.User != null ? a.User.Email : ""
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
            Title = rep.Title,
            Category = rep.Category,
            Schedule = rep.Schedule,
            DurationMinutes = rep.DurationMinutes,
            Status = rep.Status,
            IsTokenRequired = rep.IsTokenRequired,
            AccessToken = rep.AccessToken,
            PassPercentage = rep.PassPercentage,
            AllowAnswerReview = rep.AllowAnswerReview,
            RepresentativeId = rep.Id,
            Users = g.Select(a => new { FullName = a.UserFullName, Email = a.UserEmail }).ToList(),
            AllIds = g.Select(a => a.Id).ToList()
        };
    })
    .OrderByDescending(g => g.Schedule)
    .ToList();
```

### Example 2: Reset Assessment Session (Clear Responses + Reset Status)

```csharp
// Source: CMPController.ResetAssessment — confirmed working
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ResetAssessment(int id)
{
    var assessment = await _context.AssessmentSessions.FindAsync(id);
    if (assessment == null)
        return Json(new { success = false, message = "Assessment not found" });

    try
    {
        // Delete all user responses for this session
        var responses = _context.UserResponses.Where(r => r.SessionId == id);
        _context.UserResponses.RemoveRange(responses);

        // Reset assessment state
        assessment.Status = "Open";
        assessment.Progress = 0;
        assessment.Score = null;
        assessment.IsPassed = null;
        assessment.StartedAt = null;
        assessment.CompletedAt = null;
        assessment.ElapsedSeconds = 0;
        assessment.LastActivePage = null;

        await _context.SaveChangesAsync();

        // Audit log
        var user = await _userManager.GetUserAsync(User);
        await _auditLog.LogActionAsync(
            actorId: user.Id,
            actorName: $"{user.NIP} {user.FullName}",
            actionType: "ResetAssessment",
            description: $"Reset assessment '{assessment.Title}' for {assessment.User?.FullName} (ID: {id})",
            targetId: id,
            targetType: "AssessmentSession"
        );

        return Json(new { success = true, message = "Assessment reset successfully" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Error resetting assessment: " + ex.Message });
    }
}
```

### Example 3: Force Close Assessment (Set Status to Completed)

```csharp
// Source: CMPController.ForceCloseAssessment — confirmed working
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ForceCloseAssessment(int id)
{
    var assessment = await _context.AssessmentSessions
        .FirstOrDefaultAsync(a => a.Id == id);

    if (assessment == null)
        return Json(new { success = false, message = "Assessment not found" });

    try
    {
        assessment.Status = "Completed";
        assessment.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        var user = await _userManager.GetUserAsync(User);
        await _auditLog.LogActionAsync(
            actorId: user.Id,
            actorName: $"{user.NIP} {user.FullName}",
            actionType: "ForceCloseAssessment",
            description: $"Force closed assessment '{assessment.Title}' for {assessment.User?.FullName}",
            targetId: id,
            targetType: "AssessmentSession"
        );

        return Json(new { success = true, message = "Assessment force closed successfully" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Error force closing: " + ex.Message });
    }
}
```

### Example 4: Export Assessment Results to Excel

```csharp
// Source: CMPController.ExportAssessmentResults — confirmed working with ClosedXML
[HttpGet]
public async Task<IActionResult> ExportAssessmentResults(string title, string category, DateTime scheduleDate)
{
    var sessions = await _context.AssessmentSessions
        .Include(a => a.User)
        .Include(a => a.Responses)
        .Where(a => a.Title == title && a.Category == category && a.Schedule.Date == scheduleDate)
        .ToListAsync();

    if (!sessions.Any())
        return BadRequest("No sessions found for this group.");

    using (var workbook = new XLWorkbook())
    {
        var worksheet = workbook.Worksheets.Add("Results");

        // Header row
        worksheet.Cell(1, 1).Value = "Worker Name";
        worksheet.Cell(1, 2).Value = "NIP";
        worksheet.Cell(1, 3).Value = "Status";
        worksheet.Cell(1, 4).Value = "Score";
        worksheet.Cell(1, 5).Value = "Passed";
        worksheet.Cell(1, 6).Value = "Started At";
        worksheet.Cell(1, 7).Value = "Completed At";

        // Data rows
        int row = 2;
        foreach (var session in sessions)
        {
            worksheet.Cell(row, 1).Value = session.User?.FullName ?? "Unknown";
            worksheet.Cell(row, 2).Value = session.User?.NIP ?? "";
            worksheet.Cell(row, 3).Value = session.Status;
            worksheet.Cell(row, 4).Value = session.Score;
            worksheet.Cell(row, 5).Value = session.IsPassed ? "Yes" : "No";
            worksheet.Cell(row, 6).Value = session.StartedAt;
            worksheet.Cell(row, 7).Value = session.CompletedAt;
            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using (var stream = new MemoryStream())
        {
            workbook.SaveAs(stream);
            var fileName = $"{title}_{category}_{scheduleDate:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
```

### Example 5: AuditLog Query with Pagination

```csharp
// Source: CMPController.AuditLog — confirmed working
[HttpGet]
public async Task<IActionResult> AuditLog(int page = 1)
{
    const int pageSize = 25;

    var totalCount = await _context.AuditLogs.CountAsync();
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    var logs = await _context.AuditLogs
        .OrderByDescending(l => l.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    ViewBag.Logs = logs;
    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;
    ViewBag.TotalCount = totalCount;

    return View();
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Assessment manage in CMPController with toggle | Assessment manage moved to AdminController | Phase 49 | Consolidates admin tools in one controller; improves authorization; makes CMP personal-only |
| AuditLog in CMPController | AuditLog in AdminController | Phase 49 | Centralizes all admin audit trail; scope expands as new admin tools added (phases 50-58) |
| Manual assessment create/edit forms | Existing create/edit actions in CMPController | Phases 30-45 (completed) | Admin can already create/edit; Phase 49 just relocates to Admin |

**Deprecated/outdated:**
- Assessment manage view not needed — stays in project but with `view=manage` removed (CMP Assessment becomes personal-only)
- Separate `/CMP/AuditLog` route — deprecated once migration complete; old bookmarks will 404

---

## Open Questions

1. **Primary Scope Clarification**
   - What we know: Goal says "Assessment Management Migration"; requirement ID says MDAT-03 (Assessment Competency Map Manager)
   - What's unclear: Which is correct? Are they separate phases, or is there misalignment?
   - Recommendation: **MUST CONFIRM with stakeholder before detailed planning.** If Assessment Management Migration, proceed with current research. If Assessment Competency Map Manager, this research covers wrong domain.

2. **Grouped View Retention**
   - What we know: Current CMP manage view groups assessments by (Title+Category+Schedule.Date) to show "campaigns" rather than 500+ individual sessions
   - What's unclear: Should AdminController.ManageAssessment keep this grouping, or show flat per-session rows?
   - Recommendation: **Keep grouping** — reduces cognitive load, matches current UX, admin won't need to scroll through 500+ rows. Verify assumption in planning phase.

3. **Monitoring Detail UI Placement**
   - What we know: Current CMP has "Monitoring" tab with live progress; clicking detail shows AssessmentMonitoringDetail page
   - What's unclear: Should AdminController keep tabs (Management + Monitoring), or show Management main page with Monitoring as separate route?
   - Recommendation: **Keep tabs** — familiar pattern, matches current CMP UX. Alternative (separate page) adds navigation complexity.

4. **AuditLog Scope Expansion**
   - What we know: Currently shows Assessment admin actions (Create, Edit, Delete, Reset, ForceClose)
   - What's unclear: As phases 50-58 add more admin tools (Coach-Coachee, Proton Track, etc.), should AuditLog filter by action type or show all?
   - Recommendation: **Show all** initially, add filter UI in later phase (phase 59+) if audit log grows too large. Current scope is ~6 actions; filter can be added when >20 actions exist.

5. **Monitoring History View Specifics**
   - What we know: Current UserAssessmentHistory shows individual worker's attempt history; Success Criterion #1 mentions "User History" as part of Manage Assessments migration
   - What's unclear: Is "User History" a new history tab in Admin, or is it the existing UserAssessmentHistory accessed via worker link?
   - Recommendation: **Existing UserAssessmentHistory stays in CMP** (workers access their own history). Admin sees history via Monitoring Detail page (link to worker detail). Clarify in planning if Admin needs new summary history view.

---

## Validation Architecture

Test framework: **Not yet detected in project** (no jest.config.*, vitest.config.*, pytest.ini found; no test/ or tests/ directories detected)

### Current State
- No automated test framework configured
- Assessment CRUD operations exist but no test coverage
- Phase 49 operations are refactored existing code (low risk for regression)

### Phase 49 Test Strategy
Since nyquist_validation is `true` in config.json but project has no test infrastructure:

**Option A (Recommended):** Skip formal test coverage for Phase 49 (existing code, refactoring only). Wave 0 focuses on integration testing (manual: verify Create/Edit/Delete work in new location, Manage view groups correctly, AuditLog shows all actions).

**Option B (Future):** Set up xUnit + moq in Wave 0; add test suite for CRUD operations as part of Phase 49. Adds 3-5 days overhead.

### Recommendation
Proceed with **Option A**: Phase 49 is refactoring + consolidation, not new feature development. Test via integration testing (manual browser testing) in execution wave. If bug discovered post-release, add corresponding unit test in Phase 50+.

**Wave 0 Manual Verification Checklist:**
- [ ] Create Assessment via Admin works (generates new AssessmentSession with correct User assignment)
- [ ] Edit Assessment via Admin updates existing session
- [ ] Delete Assessment removes session + audit log entry created
- [ ] Reset Assessment clears UserResponses, resets status to "Open"
- [ ] Force Close Assessment sets status to "Completed", logs action
- [ ] Export Assessment generates Excel with correct headers + data rows
- [ ] Monitoring Detail shows live progress for all workers in session
- [ ] User History accessible from Monitoring Detail
- [ ] AuditLog shows all admin actions from /Admin/AuditLog
- [ ] CMP Assessment no longer shows Manage toggle or manage UI elements
- [ ] Admin Index card points to /Admin/ManageAssessment
- [ ] CMP Index card renamed to "My Assessments", no Manage Assessments card

---

## Sources

### Primary (HIGH confidence)
- CMPController.cs (verified source) — Assessment manage logic, AuditLog implementation, patterns for Reset/ForceClose/Export/Monitoring
- AdminController.cs (verified source) — Admin authorization pattern (`[Authorize(Roles = "Admin")]`), KkjMatrix scaffold structure
- AssessmentSession.cs (verified source) — Model structure, FK relationships
- AuditLog.cs (verified source) — Audit log schema and fields
- Assessment.cshtml, AssessmentMonitoringDetail.cshtml (verified source) — Current UI patterns, view layout

### Secondary (MEDIUM confidence)
- REQUIREMENTS.md — Phase 49 mapped to MDAT-03, but goal statement differs from requirement ID (clarification needed)
- Phase 47 RESEARCH.md — Admin portal patterns, AdminController scaffold pattern, view organization

### Tertiary (LOW confidence)
- None — all findings based on verified source code inspection

---

## Metadata

**Confidence breakdown:**
- **Standard stack:** HIGH — All dependencies already in project, verified in CMPController and AdminController
- **Architecture:** HIGH — Refactoring existing code to new location; patterns proven in production (CMPController + AdminController.KkjMatrix)
- **Pitfalls:** HIGH — Common migration errors documented; warning signs clear
- **Validation:** MEDIUM — No test infrastructure in project; manual verification sufficient for refactoring phase

**Research date:** 2026-02-26
**Valid until:** 2026-03-12 (14 days — assessment CRUD code is stable; only migration risk is auth/routing, which is low-risk refactoring)

**Next step:** MUST resolve scope ambiguity (Assessment Management Migration vs. MDAT-03 Assessment Competency Map Manager) before planning begins.

