# Phase 50: Coach-Coachee Mapping Manager - Research

**Researched:** 2026-02-27
**Domain:** ASP.NET Core MVC — Admin CRUD for CoachCoacheeMapping with grouped-by-coach display, bulk assign modal, soft-delete, ProtonTrackAssignment side-effect, ClosedXML export, AuditLog
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Table layout & display
- Grouped by Coach — each coach is a collapsible section header
- Coach header shows: Name, Section, active coachee count (e.g. "Rino — RFCC — 3 coachee")
- Collapsible: click coach header to expand/collapse coachee list
- Coachee row columns: Name, NIP, Section, Position, Status, StartDate, Actions
- Default: show active mappings only; toggle/checkbox to include inactive
- Section filter dropdown + text search (coach/coachee name)
- Pagination: 20 coach groups per page

#### Assign flow
- Modal form triggered by "Tambah Mapping" button
- Coach dropdown: only users with Spv, SrSpv, SectionHead, HC, or Admin role
- Coachee multi-select: section-filtered picker, excludes users who already have an active coach mapping
- Optional Proton Track dropdown (Panelman Tahun 1/2/3, Operator Tahun 1/2/3) — can be left empty
- StartDate: default today, can be changed by Admin
- If coachee already has ProtonTrackAssignment, new selection overwrites the old track
- Bulk assign: pick one coach, select multiple coachees at once

#### Edit flow
- Edit via modal (click edit on coachee row)
- Can change: coach, track, StartDate
- Same validation rules as assign

#### Unassign & status
- Soft delete: set IsActive=false + EndDate=today (record preserved)
- Confirmation modal shows active coaching session count before deactivation
- Inactive mappings visible via "show all" toggle
- Reactivate button on inactive mappings: sets IsActive=true, clears EndDate

#### Validation rules
- 1 coachee = 1 active coach (no duplicate active mappings for same coachee)
- No self-assign (CoachId != CoacheeId)
- No max coachee limit per coach
- Coach eligible roles: Spv, SrSpv, SectionHead, HC, Admin
- Coachee: all users eligible (any role)

#### Audit & export
- AuditLogService logs every assign/edit/deactivate/reactivate action
- Export Excel: download all mappings (active + inactive) to spreadsheet

#### Admin/Index card
- Section B: Operasional
- Card label: "Coach-Coachee Mapping"

### Claude's Discretion
- Track assignment behavior on deactivate (keep track or remove)
- Exact modal layout and field ordering
- Export Excel column structure
- Empty state design (no mappings yet)

### Deferred Ideas (OUT OF SCOPE)
- Phase 51 (Proton Track Assignment Manager) — absorbed into this phase as optional track selection. Phase 51 can be removed from roadmap.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| OPER-01 | Admin can view, create, edit, and delete Coach-Coachee Mappings (CoachCoacheeMapping) — assign and unassign coaches to coachees | CoachCoacheeMapping model is already in DB; full CRUD operations go into AdminController; view pattern follows grouped-by-coach with Bootstrap collapse |

</phase_requirements>

---

## Summary

Phase 50 implements the Coach-Coachee Mapping Manager as a new page at `/Admin/CoachCoacheeMapping` inside the existing `AdminController`. The data model (`CoachCoacheeMapping`) and related entities (`ProtonTrackAssignment`, `CoachingSession`, `ApplicationUser`) are all already defined and indexed in `ApplicationDbContext`. No EF migrations are needed.

The UI pattern is a grouped-by-coach table with Bootstrap `collapse` rows (identical in concept to the ManageAssessment `collapseId` pattern), a fixed section filter dropdown, a text search field, pagination over coach groups, and modals for Assign and Edit. The assign modal includes a multi-select coachee picker and optional ProtonTrack dropdown. All write operations call `AuditLogService.LogAsync(...)` following the established Admin pattern. Excel export uses `ClosedXML` (already in the project at v0.105.0, no install needed).

The most complex logic is the coachee picker: it must exclude users who already have an active CoachCoacheeMapping, and the ProtonTrackAssignment side-effect on assign/edit (overwrite existing active track). Role filtering for the coach dropdown uses `RoleLevel` — since Identity roles are stored but the codebase consistently filters by `user.RoleLevel` (e.g., `RoleLevel <= 5` = can be coach), this pattern should be followed to avoid N+1 `GetUsersInRoleAsync` calls. The eligible coach RoleLevels are 1 (Admin), 2 (HC), 4 (SectionHead/SrSpv), and 5 (Coach/Spv).

**Primary recommendation:** Build in 3 plans: (1) GET scaffold + read-only grouped view + Admin/Index card activation; (2) Assign modal + Edit modal + Deactivate/Reactivate write endpoints; (3) Excel export + AuditLog integration + UAT gap closure.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller, Views, routing | Project framework |
| Entity Framework Core | 8.0.0 | DB access for CoachCoacheeMapping, ProtonTrackAssignment, CoachingSession | Already configured |
| ASP.NET Core Identity | 8.0.0 | ApplicationUser, UserManager | User lookup and role check |
| ClosedXML | 0.105.0 | Excel export | Already in HcPortal.csproj |
| Bootstrap 5 | (CDN via layout) | Cards, collapse, modals, badges, dropdowns | All previous phases use it |
| Bootstrap Icons | (CDN via layout) | Icons | All previous phases use it |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Text.Json | built-in | JSON serialization for AJAX responses | All POST endpoints return `Json(new { success, message })` |
| AuditLogService | internal | Append-only audit entries | Every state-changing operation |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| RoleLevel filter for coach dropdown | UserManager.GetUsersInRoleAsync per role | GetUsersInRoleAsync requires one DB call per role (5 roles = 5 queries); RoleLevel column filter is single EF query — use RoleLevel |
| Bootstrap collapse for coach groups | Custom accordion JS | Bootstrap collapse is already used in ManageAssessment; no custom JS needed |

**Installation:** No new packages needed. ClosedXML 0.105.0 already present.

---

## Architecture Patterns

### Recommended Project Structure
```
Controllers/
└── AdminController.cs          # Add CoachCoacheeMapping GET/POST actions (append to existing file)
Views/Admin/
└── CoachCoacheeMapping.cshtml  # New view (grouped table + modals)
Models/
└── CoachCoacheeMapping.cs      # Already exists — no changes needed
└── ProtonModels.cs             # ProtonTrackAssignment already exists
Data/
└── ApplicationDbContext.cs     # CoachCoacheeMappings DbSet already registered
```

### Pattern 1: Grouped-by-Coach GET Action

**What:** Load all mappings, group by CoachId, inject denormalized coach/coachee display data via join with Users, pass grouped list to view via ViewBag.

**When to use:** Read-only display — no ViewModel class needed, use anonymous projection with `dynamic` (matches ManageAssessment precedent).

**Example:**
```csharp
// GET /Admin/CoachCoacheeMapping
[HttpGet]
public async Task<IActionResult> CoachCoacheeMapping(
    string? search, string? section, bool showAll = false, int page = 1)
{
    // Load all relevant user data upfront (avoid N+1)
    var allUsers = await _context.Users
        .Select(u => new { u.Id, u.FullName, u.NIP, u.Section, u.Position, u.RoleLevel })
        .ToListAsync();
    var userDict = allUsers.ToDictionary(u => u.Id);

    var query = _context.CoachCoacheeMappings.AsQueryable();
    if (!showAll)
        query = query.Where(m => m.IsActive);

    var mappings = await query.ToListAsync();

    // Apply search filter in memory (both coach and coachee name match)
    // Group by CoachId, paginate over coach groups
    // ...
    ViewBag.GroupedCoaches = pagedGroups;
    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;
    ViewBag.ShowAll = showAll;
    ViewBag.SearchTerm = search;
    ViewBag.SectionFilter = section;
    ViewBag.Sections = OrganizationStructure.GetAllSections();
    return View();
}
```

### Pattern 2: Bootstrap Collapse for Coach Groups

**What:** Each coach renders as a `<tr class="table-primary">` header row. Coachee rows are hidden inside a `<tbody id="coach-{CoachId}">` with `collapse`. Click the coach header row to toggle coachees.

**When to use:** Grouped hierarchical data — already proven pattern in ManageAssessment (collapseId on `data-bs-target`).

**Example:**
```html
<!-- Coach header row -->
<tr class="table-primary" data-bs-toggle="collapse"
    data-bs-target="#coach-@group.CoachId.Replace('-','_')"
    style="cursor:pointer;">
    <td colspan="8">
        <i class="bi bi-chevron-down me-2"></i>
        @group.CoachName — @group.CoachSection — @group.ActiveCount coachee
    </td>
</tr>
<!-- Coachee detail rows -->
<tbody class="collapse show" id="coach-@group.CoachId.Replace('-','_')">
    @foreach (var coachee in group.Coachees)
    {
        <tr>
            <td>@coachee.CoacheeName</td>
            <!-- ... -->
        </tr>
    }
</tbody>
```

**Note on ID sanitization:** ApplicationUser IDs are GUIDs with hyphens. Use `.Replace("-", "_")` or equivalent to create valid HTML element IDs. Or use a sequential index counter in the loop.

### Pattern 3: Modal-based Assign (Bulk) + Edit

**What:** Bootstrap modal with AntiForgeryToken-protected AJAX POST. Same approach as other admin modals.

**When to use:** Assign new mappings (bulk — one coach, N coachees) and edit existing mapping.

**AJAX POST pattern (consistent with project):**
```javascript
fetch('/Admin/CoachCoacheeMappingAssign', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('[name=__RequestVerificationToken]').value
    },
    body: JSON.stringify(payload)
})
.then(r => r.json())
.then(data => {
    if (data.success) location.reload();
    else alert(data.message);
});
```

### Pattern 4: Coachee Multi-Select in Assign Modal

**What:** A `<select multiple>` or checkbox list of users who do NOT have an active CoachCoacheeMapping. Filter by section dropdown inside the modal. Server provides the eligible coachee list via ViewBag at GET time or via a JSON endpoint.

**Recommendation:** Load eligible coachees at GET time into ViewBag (same pattern as CreateAssessment which loads all users). The eligible coach list (RoleLevel <= 5 or specific levels) and eligible coachee list (all users not already assigned) are both derived from `_context.Users` — load once.

```csharp
// In GET action — prepare modal data
var activeCoacheeIds = await _context.CoachCoacheeMappings
    .Where(m => m.IsActive)
    .Select(m => m.CoacheeId)
    .Distinct()
    .ToListAsync();

var eligibleCoachees = allUsers
    .Where(u => !activeCoacheeIds.Contains(u.Id))
    .OrderBy(u => u.FullName)
    .ToList();

var eligibleCoaches = allUsers
    .Where(u => u.RoleLevel <= 5)  // Admin, HC, SectionHead/SrSpv, Coach
    .OrderBy(u => u.FullName)
    .ToList();

ViewBag.EligibleCoachees = eligibleCoachees;
ViewBag.EligibleCoaches = eligibleCoaches;
ViewBag.ProtonTracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
```

### Pattern 5: Soft Delete / Reactivate

**What:** Unassign sets `IsActive = false`, `EndDate = DateTime.Today` (not UTC — local date for display). Reactivate sets `IsActive = true`, `EndDate = null`.

**Confirmation count:** Before deactivating, count active CoachingSessions for that mapping's coachee where CoachId matches:
```csharp
var activeSessionCount = await _context.CoachingSessions
    .CountAsync(s => s.CoachId == mapping.CoachId
                  && s.CoacheeId == mapping.CoacheeId
                  && s.Status == "Draft");
```
Return this count in the deactivation confirmation modal so admin can proceed informed.

### Pattern 6: ProtonTrackAssignment Side-Effect on Assign/Edit

**What:** If the modal includes a ProtonTrack selection (non-null), after saving the CoachCoacheeMapping, deactivate any existing ProtonTrackAssignment for that coachee and create a new one. Match the CDPController.AssignTrack pattern (deactivate existing, create new, do NOT delete deliverable progress on admin assign — admin only assigns track, not reset progress).

**Note from CONTEXT.md:** "If coachee already has ProtonTrackAssignment, new selection overwrites the old track." Claude's Discretion covers whether deactivate-mapping removes the track. Recommendation: deactivate-mapping does NOT remove the track (keep existing track, only the coaching relationship is deactivated). This avoids data loss if the track is already in progress.

```csharp
// When ProtonTrackId is provided in assign/edit:
if (protonTrackId.HasValue && protonTrackId.Value > 0)
{
    var existing = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == coacheeId && a.IsActive)
        .ToListAsync();
    existing.ForEach(a => a.IsActive = false);

    _context.ProtonTrackAssignments.Add(new ProtonTrackAssignment
    {
        CoacheeId = coacheeId,
        AssignedById = actor.Id,
        ProtonTrackId = protonTrackId.Value,
        IsActive = true,
        AssignedAt = DateTime.UtcNow
    });
}
```

### Pattern 7: Excel Export (ClosedXML)

**What:** Single worksheet with all mappings (active + inactive). Flat rows — one row per coachee mapping record. Follow CpdpItemsExport pattern.

**Recommended columns:** Coach Name, Coach Section, Coachee Name, Coachee NIP, Coachee Section, Coachee Position, Proton Track, Status (Active/Inactive), StartDate, EndDate.

```csharp
// GET /Admin/CoachCoacheeMappingExport
public async Task<IActionResult> CoachCoacheeMappingExport()
{
    var mappings = await _context.CoachCoacheeMappings
        .OrderBy(m => m.CoachId).ThenBy(m => m.StartDate)
        .ToListAsync();
    // join with allUsers dict for display names
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("Coach-Coachee Mapping");
    // ... header row (dark bg) + data rows
    ws.Columns().AdjustToContents();
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    stream.Position = 0;
    return File(stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "CoachCoacheeMapping.xlsx");
}
```

### Pattern 8: AuditLog Integration

**What:** Every state-changing endpoint calls `_auditLog.LogAsync(...)` after `SaveChangesAsync`. Use consistent ActionType strings.

```csharp
// Assign
await _auditLog.LogAsync(actor.Id, actor.FullName, "Assign",
    $"Assigned coach {coachName} to coachees: {coacheeNames} (MappingIds: {idList})",
    targetType: "CoachCoacheeMapping");

// Edit
await _auditLog.LogAsync(actor.Id, actor.FullName, "Edit",
    $"Edited mapping Id={id}: coach={coachName}, track={trackName}, startDate={startDate}",
    targetId: mapping.Id, targetType: "CoachCoacheeMapping");

// Deactivate
await _auditLog.LogAsync(actor.Id, actor.FullName, "Deactivate",
    $"Deactivated mapping Id={id} (Coach={coachName} → Coachee={coacheeName})",
    targetId: mapping.Id, targetType: "CoachCoacheeMapping");

// Reactivate
await _auditLog.LogAsync(actor.Id, actor.FullName, "Reactivate",
    $"Reactivated mapping Id={id} (Coach={coachName} → Coachee={coacheeName})",
    targetId: mapping.Id, targetType: "CoachCoacheeMapping");
```

### Pattern 9: Admin/Index Card Activation

**What:** Replace the placeholder card in Section B (Operasional) that currently links to `#` with a real link to `/Admin/CoachCoacheeMapping` and remove the "Segera" badge.

```html
<!-- Before (in Views/Admin/Index.cshtml) -->
<a href="#" class="text-decoration-none">
    <div class="card shadow-sm h-100 border-0 opacity-75">
        ...
        <span class="badge bg-secondary ms-auto" style="font-size:0.6rem;">Segera</span>
        ...
    </div>
</a>

<!-- After -->
<a href="@Url.Action("CoachCoacheeMapping", "Admin")" class="text-decoration-none">
    <div class="card shadow-sm h-100 border-0">
        ...
        <!-- Remove opacity-75 and Segera badge -->
        ...
    </div>
</a>
```

### Anti-Patterns to Avoid

- **N+1 user lookups:** Do not call `_userManager.FindByIdAsync(coachId)` inside a loop. Load all users once with `_context.Users.Select(...).ToListAsync()` then use a dictionary.
- **GetUsersInRoleAsync per role for coach dropdown:** 5 separate queries. Use `RoleLevel <= 5` filter on the Users table instead.
- **GUID with hyphens in HTML ID:** Direct use of ApplicationUser.Id as HTML element ID breaks selector syntax. Always sanitize: `.Replace("-", "_")` or use a sequential loop counter.
- **Hard delete on CoachCoacheeMapping:** The CONTEXT.md specifies soft delete only (IsActive=false + EndDate). Hard deletes are not in scope.
- **Duplicate active mapping creation:** Server must validate `!_context.CoachCoacheeMappings.Any(m => m.CoacheeId == coacheeId && m.IsActive)` before inserting, even though the coachee picker excludes assigned users in the UI.
- **Self-assign:** Server must validate `coachId != coacheeId` for each mapping being created.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel export | Custom CSV/HTML table | ClosedXML (already in project) | Already used in CpdpItemsExport and ExportAssessmentResults — consistent, handles formatting |
| Audit logging | Direct DB writes in controller | AuditLogService.LogAsync | Service already injected in AdminController constructor; provides consistent schema |
| Role-based user filtering | Per-role Identity queries | `user.RoleLevel` column filter | Single EF query; RoleLevel is already the project's canonical role-level source |
| Confirmation modals | Custom confirm() calls | Bootstrap modals with JSON island data | Established pattern from Phase 49-05 (JSON island avoids quote conflicts) |

---

## Common Pitfalls

### Pitfall 1: GUID Hyphens in HTML Element IDs
**What goes wrong:** `data-bs-target="#abc-123-def-456"` — Bootstrap interprets hyphens as a CSS selector namespace separator, causing collapse to fail silently.
**Why it happens:** ApplicationUser.Id is a GUID string like `"3f2504e0-4f89-11d3-9a0c-0305e82c3301"`.
**How to avoid:** Use a sequential index counter from the Razor `@foreach` loop: `collapse-@i`. Or replace hyphens: `@coach.CoachId.Replace("-", "_")`.
**Warning signs:** Collapse button click has no effect; browser console shows no JS error.

### Pitfall 2: Stale Coachee Picker (Already-Assigned Coachees Appearing)
**What goes wrong:** Admin assigns user A to coach X. Later opens Assign modal again — user A still appears as available because the eligible list was loaded at page load time.
**Why it happens:** Eligible coachee list is computed at GET and embedded in HTML/ViewBag. If user doesn't reload the page, the list is stale.
**How to avoid:** After a successful assign AJAX call, trigger `location.reload()` to refresh the page (consistent with pattern in other admin modals). Do NOT try to dynamically remove the coachee from the list in JS.
**Warning signs:** Admin can assign the same coachee twice; server returns error "Coachee already has active mapping."

### Pitfall 3: Missing AntiForgery Token in AJAX Headers
**What goes wrong:** POST requests return HTTP 400 without clear error message.
**Why it happens:** `[ValidateAntiForgeryToken]` on action but AJAX not sending the token.
**How to avoid:** Include `@Html.AntiForgeryToken()` at top of view. In JS: `document.querySelector('[name=__RequestVerificationToken]').value`.
**Warning signs:** Network tab shows 400 response on POST; browser console may show "Bad Request."

### Pitfall 4: Bulk Assign Partial Failure
**What goes wrong:** Assigning 5 coachees — 3 succeed, 2 fail validation. All 5 partially committed because each mapping was saved separately.
**Why it happens:** SaveChangesAsync called per coachee in loop rather than after all are added.
**How to avoid:** Validate all coachees first (batch query), then AddRange all valid mappings, then single SaveChangesAsync. Return per-coachee result list in JSON for UI display.

### Pitfall 5: ProtonTrackAssignment Side-Effect on Bulk Assign
**What goes wrong:** Assigning 3 coachees at once — only 1 gets the ProtonTrackAssignment because the loop overwrites the previous iteration's pending add.
**Why it happens:** EF change tracker has multiple pending ProtonTrackAssignment adds for different CoacheeIds — this is actually fine since each has a different CoacheeId. But the "deactivate existing" step could fail if the same CoacheeId appears twice in the list (impossible if validation passes, but worth guarding).
**How to avoid:** Deactivate all existing assignments for the target coachee IDs in a single `_context.ProtonTrackAssignments.Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)` batch query. Then AddRange all new assignments.

### Pitfall 6: Pagination Count Wrong When showAll Toggle Changes
**What goes wrong:** User is on page 3 of active-only view, toggles "show all" — page 3 may not exist in the expanded set, or the count changes unexpectedly.
**Why it happens:** `page` query parameter persists in URL when toggling showAll.
**How to avoid:** Reset page to 1 whenever showAll toggle changes. The toggle form/link should not carry `page=3` in the href.

---

## Code Examples

### GET Action Skeleton
```csharp
// GET /Admin/CoachCoacheeMapping
[HttpGet]
public async Task<IActionResult> CoachCoacheeMapping(
    string? search, string? section, bool showAll = false, int page = 1)
{
    const int pageSize = 20; // 20 coach groups per page

    // 1. Load all users once (coach + coachee display info)
    var allUsers = await _context.Users
        .Select(u => new {
            u.Id, u.FullName, u.NIP,
            u.Section, u.Position, u.RoleLevel
        })
        .ToListAsync();
    var userDict = allUsers.ToDictionary(u => u.Id);

    // 2. Load mappings
    var query = _context.CoachCoacheeMappings.AsQueryable();
    if (!showAll)
        query = query.Where(m => m.IsActive);
    var mappings = await query.ToListAsync();

    // 3. Join + filter
    var rows = mappings.Select(m => new {
        Mapping = m,
        Coach = userDict.GetValueOrDefault(m.CoachId),
        Coachee = userDict.GetValueOrDefault(m.CoacheeId)
    });

    if (!string.IsNullOrEmpty(search))
    {
        var lower = search.ToLower();
        rows = rows.Where(r =>
            (r.Coach?.FullName?.ToLower().Contains(lower) ?? false) ||
            (r.Coachee?.FullName?.ToLower().Contains(lower) ?? false));
    }
    if (!string.IsNullOrEmpty(section))
    {
        rows = rows.Where(r =>
            r.Coach?.Section == section ||
            r.Coachee?.Section == section);
    }

    // 4. Group by Coach, paginate
    var grouped = rows
        .GroupBy(r => r.Mapping.CoachId)
        .Select(g => new {
            CoachId = g.Key,
            CoachName = g.First().Coach?.FullName ?? g.Key,
            CoachSection = g.First().Coach?.Section ?? "",
            ActiveCount = g.Count(r => r.Mapping.IsActive),
            Coachees = g.Select(r => new {
                r.Mapping.Id,
                r.Mapping.IsActive,
                r.Mapping.StartDate,
                r.Mapping.EndDate,
                CoacheeName = r.Coachee?.FullName ?? r.Mapping.CoacheeId,
                CoacheeNIP = r.Coachee?.NIP ?? "",
                CoacheeSection = r.Coachee?.Section ?? "",
                CoacheePosition = r.Coachee?.Position ?? "",
                r.Mapping.CoacheeId
            }).ToList()
        })
        .OrderBy(g => g.CoachName)
        .ToList();

    var totalCoachGroups = grouped.Count;
    var totalPages = (int)Math.Ceiling(totalCoachGroups / (double)pageSize);
    if (page < 1) page = 1;
    if (page > totalPages && totalPages > 0) page = totalPages;

    var paged = grouped.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    // 5. Modal data
    var activeCoacheeIds = await _context.CoachCoacheeMappings
        .Where(m => m.IsActive)
        .Select(m => m.CoacheeId)
        .Distinct()
        .ToListAsync();

    ViewBag.GroupedCoaches = paged;
    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;
    ViewBag.TotalCount = totalCoachGroups;
    ViewBag.ShowAll = showAll;
    ViewBag.SearchTerm = search;
    ViewBag.SectionFilter = section;
    ViewBag.Sections = OrganizationStructure.GetAllSections();
    ViewBag.EligibleCoaches = allUsers
        .Where(u => u.RoleLevel <= 5)
        .OrderBy(u => u.FullName).ToList();
    ViewBag.EligibleCoachees = allUsers
        .Where(u => !activeCoacheeIds.Contains(u.Id))
        .OrderBy(u => u.FullName).ToList();
    ViewBag.AllCoachees = allUsers.OrderBy(u => u.FullName).ToList(); // for edit modal
    ViewBag.ProtonTracks = await _context.ProtonTracks
        .OrderBy(t => t.Urutan).ToListAsync();

    return View();
}
```

### Assign POST (Bulk)
```csharp
// POST /Admin/CoachCoacheeMappingAssign
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CoachCoacheeMappingAssign([FromBody] CoachAssignRequest req)
{
    if (req == null || string.IsNullOrEmpty(req.CoachId) || req.CoacheeIds == null || !req.CoacheeIds.Any())
        return Json(new { success = false, message = "Data tidak lengkap." });

    // Validation: no self-assign
    if (req.CoacheeIds.Contains(req.CoachId))
        return Json(new { success = false, message = "Coach tidak dapat menjadi coachee dirinya sendiri." });

    // Validation: coachees not already assigned
    var alreadyAssigned = await _context.CoachCoacheeMappings
        .Where(m => req.CoacheeIds.Contains(m.CoacheeId) && m.IsActive)
        .Select(m => m.CoacheeId)
        .ToListAsync();
    if (alreadyAssigned.Any())
    {
        var names = string.Join(", ", alreadyAssigned);
        return Json(new { success = false, message = $"Coachee berikut sudah memiliki coach aktif: {names}" });
    }

    var startDate = req.StartDate.HasValue ? req.StartDate.Value : DateTime.Today;
    var newMappings = req.CoacheeIds.Select(coacheeId => new CoachCoacheeMapping
    {
        CoachId = req.CoachId,
        CoacheeId = coacheeId,
        IsActive = true,
        StartDate = startDate
    }).ToList();
    _context.CoachCoacheeMappings.AddRange(newMappings);

    // ProtonTrack side-effect (optional)
    if (req.ProtonTrackId.HasValue && req.ProtonTrackId.Value > 0)
    {
        var existingTracks = await _context.ProtonTrackAssignments
            .Where(a => req.CoacheeIds.Contains(a.CoacheeId) && a.IsActive)
            .ToListAsync();
        existingTracks.ForEach(a => a.IsActive = false);

        var actor = await _userManager.GetUserAsync(User);
        var newTracks = req.CoacheeIds.Select(coacheeId => new ProtonTrackAssignment
        {
            CoacheeId = coacheeId,
            AssignedById = actor!.Id,
            ProtonTrackId = req.ProtonTrackId.Value,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        }).ToList();
        _context.ProtonTrackAssignments.AddRange(newTracks);
    }

    await _context.SaveChangesAsync();

    var actorUser = await _userManager.GetUserAsync(User);
    if (actorUser != null)
        await _auditLog.LogAsync(actorUser.Id, actorUser.FullName, "Assign",
            $"Assigned coach {req.CoachId} to {req.CoacheeIds.Count} coachee(s); StartDate={startDate:yyyy-MM-dd}",
            targetType: "CoachCoacheeMapping");

    return Json(new { success = true, message = $"{req.CoacheeIds.Count} mapping berhasil dibuat." });
}
```

### Deactivate with Session Count
```csharp
// POST /Admin/CoachCoacheeMappingDeactivate
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CoachCoacheeMappingDeactivate(int id)
{
    var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
    if (mapping == null)
        return Json(new { success = false, message = "Mapping tidak ditemukan." });

    // Count active coaching sessions for confirmation
    var activeSessionCount = await _context.CoachingSessions
        .CountAsync(s => s.CoachId == mapping.CoachId
                      && s.CoacheeId == mapping.CoacheeId
                      && s.Status == "Draft");

    mapping.IsActive = false;
    mapping.EndDate = DateTime.Today;
    await _context.SaveChangesAsync();

    var actor = await _userManager.GetUserAsync(User);
    if (actor != null)
        await _auditLog.LogAsync(actor.Id, actor.FullName, "Deactivate",
            $"Deactivated CoachCoacheeMapping Id={id}; {activeSessionCount} active session(s) exist",
            targetId: id, targetType: "CoachCoacheeMapping");

    return Json(new { success = true, activeSessionCount });
}
```

### Request DTO (in AdminController or separate file)
```csharp
// Used for CoachCoacheeMappingAssign and CoachCoacheeMappingEdit
public class CoachAssignRequest
{
    public string CoachId { get; set; } = "";
    public List<string> CoacheeIds { get; set; } = new();
    public int? ProtonTrackId { get; set; }
    public DateTime? StartDate { get; set; }
}

public class CoachEditRequest
{
    public int MappingId { get; set; }
    public string CoachId { get; set; } = "";
    public int? ProtonTrackId { get; set; }
    public DateTime StartDate { get; set; }
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| CoachCoacheeMapping managed via CDPController (Coach view) | Admin-managed via AdminController | Phase 50 | Admin has full CRUD; coaches see their own view via CDP |
| ProtonTrackAssignment only assignable by Coach/HC via CDP | Also assignable via Coach-Coachee Mapping modal | Phase 50 (merged from Phase 51) | Admin can set track at assignment time |
| Phase 51 (Proton Track Assignment Manager) as separate phase | Absorbed into Phase 50 as optional track dropdown | Phase 50 (CONTEXT.md) | Phase 51 removed from roadmap |

**Deprecated/outdated:**
- Phase 51 as standalone: Absorbed. Roadmap can remove Phase 51 entry.

---

## Open Questions

1. **Track behavior on deactivate-mapping (Claude's Discretion)**
   - What we know: CONTEXT.md says this is Claude's Discretion
   - What's unclear: Should deactivating a coaching mapping also deactivate the coachee's ProtonTrackAssignment?
   - Recommendation: Do NOT deactivate the track when deactivating the mapping. The coaching relationship ends but the coachee's deliverable progress (tied to the track) is independent. Removing the track would orphan deliverable progress records. If admin needs to change the track, they use the Edit flow.

2. **Export Excel column for ProtonTrack**
   - What we know: Mappings don't store ProtonTrackId — CoachCoacheeMapping model has no TrackId column. Track is in ProtonTrackAssignment (separate table, per-coachee).
   - What's unclear: Should the export join to ProtonTrackAssignment to show the current active track?
   - Recommendation: Yes, join to ProtonTrackAssignment for the export — add a `Current Track` column. This requires a secondary lookup: `ProtonTrackAssignments.Where(a => a.IsActive)` keyed by CoacheeId.

3. **Edit modal coachee dropdown**
   - What we know: Edit can change coach, track, StartDate. Coachee itself cannot be changed (changing coachee = deactivate + new assign).
   - What's unclear: Should coachee field in edit modal be read-only or not shown?
   - Recommendation: Show coachee as a read-only label (not a dropdown) in the edit modal. Coach dropdown should exclude coachees who would create a duplicate active mapping (i.e., exclude coaches already assigned to this coachee via another mapping).

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (no automated test framework in HcPortal.csproj — single web project, no xUnit/NUnit test project) |
| Config file | None |
| Quick run command | `dotnet run --project HcPortal.csproj` + browser manual test |
| Full suite command | Manual UAT checklist per VERIFICATION.md pattern |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| OPER-01 | Admin can view grouped-by-coach mapping table | Manual/smoke | Navigate to /Admin/CoachCoacheeMapping | ❌ Wave 0 |
| OPER-01 | Admin can assign coach to multiple coachees via modal | Manual | Open Assign modal, select coach + coachees, submit | ❌ Wave 0 |
| OPER-01 | Assign validates 1 coachee = 1 active coach | Manual | Attempt to assign already-assigned coachee | ❌ Wave 0 |
| OPER-01 | Assign validates no self-assign | Manual | Select same user as coach and coachee | ❌ Wave 0 |
| OPER-01 | Admin can edit mapping (coach, track, startDate) | Manual | Click Edit on coachee row | ❌ Wave 0 |
| OPER-01 | Soft delete: IsActive=false + EndDate=today | Manual | Click Deactivate, confirm | ❌ Wave 0 |
| OPER-01 | Reactivate: IsActive=true, EndDate=null | Manual | Click Reactivate on inactive mapping | ❌ Wave 0 |
| OPER-01 | ProtonTrack optional on assign — overwrites existing | Manual | Assign with track; verify ProtonTrackAssignment | ❌ Wave 0 |
| OPER-01 | Export Excel downloads file with all mappings | Manual | Click Export button | ❌ Wave 0 |
| OPER-01 | AuditLog receives entries for all actions | Manual | Check /Admin/AuditLog after each action | ❌ Wave 0 |
| OPER-01 | Admin/Index card links to /Admin/CoachCoacheeMapping | Manual | Visit /Admin, click card | ❌ Wave 0 |
| OPER-01 | Section filter + text search narrow results | Manual | Apply filter, verify grouped display | ❌ Wave 0 |
| OPER-01 | Pagination: 20 coach groups per page | Manual | Requires ≥21 coaches with mappings | ❌ Wave 0 |
| OPER-01 | showAll toggle reveals inactive mappings | Manual | Toggle, verify inactive rows appear | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet build` (confirm no compile errors)
- **Per wave merge:** Full UAT checklist (browser walkthrough of all behaviors)
- **Phase gate:** Full UAT green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] No automated test project exists — all validation is manual UAT via browser
- [ ] `Views/Admin/CoachCoacheeMapping.cshtml` — does not exist yet (Wave 0 task)
- [ ] AdminController actions (CoachCoacheeMapping GET, Assign POST, Edit POST, Deactivate POST, Reactivate POST, Export GET) — do not exist yet

*(No test framework install needed — project uses manual UAT throughout all prior phases)*

---

## Sources

### Primary (HIGH confidence)
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/Models/CoachCoacheeMapping.cs` — model fields confirmed
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/Models/ProtonModels.cs` — ProtonTrackAssignment model confirmed
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/Data/ApplicationDbContext.cs` — DbSet registration, indexes, config
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/Controllers/AdminController.cs` — established patterns for AuditLog, ClosedXML, pagination, AJAX POST
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/Controllers/CDPController.cs` — AssignTrack pattern for ProtonTrackAssignment side-effect
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/Views/Admin/ManageAssessment.cshtml` — Bootstrap collapse pattern for grouped rows
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/Models/UserRoles.cs` — RoleLevel constants
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/Models/ApplicationUser.cs` — FullName, NIP, Section, Position, RoleLevel fields
- Codebase: `C:/Users/Administrator/Desktop/PortalHC_KPB/HcPortal.csproj` — ClosedXML v0.105.0 confirmed, net8.0 target

### Secondary (MEDIUM confidence)
- Phase 49 RESEARCH.md — confirmed AdminController patterns (AuditLog injection, JSON responses, anti-forgery)

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project and used in prior phases
- Architecture: HIGH — all patterns directly derived from existing AdminController and CDPController code
- Pitfalls: HIGH — derived from actual code inspection (GUID IDs, AntiForgery, bulk save patterns)

**Research date:** 2026-02-27
**Valid until:** 2026-03-29 (30 days — stable ASP.NET Core 8 + Bootstrap 5 stack)
