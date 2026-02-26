# Phase 47: KKJ Matrix Manager - Research

**Researched:** 2026-02-26
**Domain:** ASP.NET Core MVC — Admin CRUD UI with spreadsheet-style inline editing, bulk save via AJAX
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Admin Portal Infrastructure (applies to all phases 47-58)
- **New controller:** `AdminController` with routes `/Admin/*` — all 12 admin tools in one controller
- **Portal name:** "Data Management" / "Kelola Data" — appears in nav sidebar, page titles, and headings
- **Navigation:** New menu item in sidebar, visible only for Admin role
- **Admin index page:** `/Admin/Index` — landing page listing all 12 tools with short descriptions
- **Index grouping:** 3 sections on index page: **Master Data** (Cat A: MDAT-01–03), **Operational** (Cat B: OPER-01–05), **CRUD Completions** (Cat C: CRUD-01–04)

#### Table Layout (read mode)
- Compact table: show only `No`, `Indeks`, `Kompetensi`, `SkillGroup` + action buttons (Edit/Delete)
- Target_* columns NOT shown in read mode — too wide

#### Edit & Create Interaction — Spreadsheet/Excel Mode
- **Toggle edit mode:** "Edit" button above table; when clicked, entire table becomes editable
- **Edit mode:** All 18 columns shown (including 13 Target_* columns) with horizontal scroll — admin edits directly in cell input fields
- **Copy-paste support:** Admin can copy from Excel and paste into table (multi-row paste via clipboard)
- **Save:** "Simpan" (Submit) button saves all changes at once to server, then locks table back to read mode
- **Create:** When edit mode is active, an empty row at the bottom of the table allows adding a new row — submitted together with edits
- **Cancel:** "Batal" button discards all changes and returns to read mode

#### Delete Guard
- Check for references to `UserCompetencyLevel` before delete
- **If references exist: BLOCK** — show error message with worker count affected, cannot delete
- **If no references:** Delete directly with brief confirmation

### Claude's Discretion
- Implementation of multi-row copy-paste (contenteditable vs input fields, Tab/Enter navigation)
- Exact styling of wide table mode (column widths for Target_* columns)
- Error display positioning (toast vs inline)
- Pagination vs full list (KKJ Matrix items may be 50-100+ rows)

### Deferred Ideas (OUT OF SCOPE)
- None — discussion stayed within phase scope

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MDAT-01 | Admin can view, create, edit, and delete KKJ Matrix items (KkjMatrixItem) through a dedicated management page — no database/code change required | AdminController scaffold, spreadsheet-mode inline editing, bulk-save JSON endpoint, delete-with-usage-guard endpoint, read/edit view toggle |

</phase_requirements>

---

## Summary

This phase establishes the Admin Portal infrastructure (`AdminController` + `/Admin/Index` hub page) and implements the first management tool: KKJ Matrix Manager. The `KkjMatrixItem` model already exists with 18 columns (5 metadata + 13 Target_* columns for different job positions), is already seeded in SQL Server via `SeedMasterData.SeedKkjMatrixAsync`, and is already served read-only through `CMPController.Kkj`. The task is to give Admin users a write-capable management page that mirrors spreadsheet UX.

The primary technical challenge is the spreadsheet/Excel-mode editing. The locked decision requires that in edit mode all 18 columns appear as `<input>` fields in each table row, with horizontal scroll, Tab/Enter navigation, and clipboard paste support for multi-row bulk import from Excel. When the admin clicks "Simpan", a single JSON POST sends the full table state (all current + new rows, IDs included) to a bulk-save endpoint. The server performs an upsert: update existing rows by Id, insert rows with Id=0 as new. Delete is a separate guarded action.

`UserCompetencyLevel` has a `RESTRICT` FK on `KkjMatrixItemId` (confirmed in `ApplicationDbContext`), so attempting to delete a referenced row will throw a DB exception. The delete guard should check `_context.UserCompetencyLevels.AnyAsync(u => u.KkjMatrixItemId == id)` before calling `Remove`, and if any exist, return `Json(new { success = false, message = "..." })` instead of deleting.

**Primary recommendation:** Implement AdminController with Index hub + KkjMatrix action (GET returns read view, edit mode toggled client-side via JS, POST bulk-save endpoint accepts JSON array, DELETE endpoint returns JSON with guard result). Use `[Authorize(Roles = "Admin")]` on the controller class.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 (project target) | Controller + Razor views | Already the project framework |
| Entity Framework Core | 8.0.0 | DB access to SQL Server | Already configured in project |
| Microsoft.AspNetCore.Identity | 8.0.0 | `[Authorize(Roles)]` authentication | Already in use across all controllers |
| Bootstrap | 5.3.0 (CDN) | Responsive table, button, badge UI | Already in `_Layout.cshtml` |
| Bootstrap Icons | 1.10.0 (CDN) | Icons (bi-pencil, bi-trash, etc.) | Already in layout |
| jQuery | 3.7.1 (CDN) | AJAX $.ajax calls, DOM manipulation | Already in layout, used across all views |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| AuditLogService | project class | Log admin Create/Update/Delete actions | Use for all write operations — same as CMPController |
| System.Text.Json | .NET built-in | Serialize/deserialize JSON body in bulk-save | Use for `[FromBody]` model binding |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Inline `<input>` fields in edit mode | `contenteditable` cells | Contenteditable is simpler for paste but harder to extract values reliably; `<input>` is more controlled and avoids XSS |
| jQuery $.ajax | Vanilla fetch() | Either works — project uses both; $.ajax is dominant for POST operations, stick with $.ajax for consistency |
| Single bulk-save endpoint | Per-row AJAX saves | Bulk-save matches user's mental model (click Simpan = save all), avoids N save calls |

**Installation:** No new packages needed. All dependencies already in project.

---

## Architecture Patterns

### Recommended Project Structure

New files to create:
```
Controllers/
  AdminController.cs              # New — all 12 admin tools (phase 47 adds Index + KkjMatrix)

Views/Admin/
  Index.cshtml                    # Hub page — 12 tool cards in 3 category groups
  KkjMatrix.cshtml                # Read/Edit view for KkjMatrixItem CRUD
```

No new models needed — `KkjMatrixItem` and `ApplicationDbContext` already exist.

### Pattern 1: AdminController with Class-Level Authorization

**What:** Single controller for all 12 admin tools, `[Authorize(Roles = "Admin")]` at class level so every action inherits it.

**When to use:** All admin tools — no per-action authorization needed since all are Admin-only.

**Example:**
```csharp
// Source: Existing CMPController pattern + ASP.NET Core docs
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLog;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        AuditLogService auditLog)
    {
        _context = context;
        _userManager = userManager;
        _auditLog = auditLog;
    }

    // GET /Admin/Index
    public IActionResult Index()
    {
        return View();
    }

    // GET /Admin/KkjMatrix
    public async Task<IActionResult> KkjMatrix()
    {
        var items = await _context.KkjMatrices
            .OrderBy(k => k.No)
            .ToListAsync();
        return View(items);
    }
}
```

### Pattern 2: Bulk-Save JSON Endpoint

**What:** Single POST action receives the entire table as a JSON array. Server upserts: update rows with existing Id, insert rows with Id=0.

**When to use:** Spreadsheet-mode save where all edits happen client-side then submit at once.

**Example:**
```csharp
// POST /Admin/KkjMatrixSave
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KkjMatrixSave([FromBody] List<KkjMatrixItem> rows)
{
    if (rows == null || !rows.Any())
        return Json(new { success = false, message = "No data received." });

    try
    {
        foreach (var row in rows)
        {
            if (row.Id == 0)
            {
                // New row — insert
                _context.KkjMatrices.Add(row);
            }
            else
            {
                // Existing row — update
                var existing = await _context.KkjMatrices.FindAsync(row.Id);
                if (existing != null)
                {
                    existing.No = row.No;
                    existing.SkillGroup = row.SkillGroup;
                    existing.SubSkillGroup = row.SubSkillGroup;
                    existing.Indeks = row.Indeks;
                    existing.Kompetensi = row.Kompetensi;
                    existing.Target_SectionHead = row.Target_SectionHead;
                    // ... all 13 Target_* columns
                    _context.KkjMatrices.Update(existing);
                }
            }
        }
        await _context.SaveChangesAsync();

        var actor = await _userManager.GetUserAsync(User);
        await _auditLog.LogAsync(actor!.Id, actor.FullName, "BulkUpdate",
            $"KKJ Matrix bulk-save: {rows.Count} rows", targetType: "KkjMatrixItem");

        return Json(new { success = true, message = $"{rows.Count} rows saved." });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}
```

**CRITICAL:** When using `[FromBody]`, the jQuery AJAX call must set `contentType: 'application/json'` and `data: JSON.stringify(rowArray)`. The antiforgery token must be sent as a request header `RequestVerificationToken`, not in the body (since body is JSON not form-encoded). See Code Examples section for the complete JS pattern.

### Pattern 3: Delete with Usage Guard

**What:** Check `UserCompetencyLevel` FK references before deleting. Return JSON error if in use, proceed if not.

**When to use:** All deletes of KkjMatrixItem.

**Example:**
```csharp
// POST /Admin/KkjMatrixDelete
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KkjMatrixDelete(int id)
{
    var item = await _context.KkjMatrices.FindAsync(id);
    if (item == null)
        return Json(new { success = false, message = "Item tidak ditemukan." });

    // Check for FK references in UserCompetencyLevel
    var usageCount = await _context.UserCompetencyLevels
        .CountAsync(u => u.KkjMatrixItemId == id);

    if (usageCount > 0)
        return Json(new { success = false, blocked = true,
            message = $"Tidak dapat dihapus — digunakan oleh {usageCount} worker." });

    _context.KkjMatrices.Remove(item);
    await _context.SaveChangesAsync();

    var actor = await _userManager.GetUserAsync(User);
    await _auditLog.LogAsync(actor!.Id, actor.FullName, "Delete",
        $"Deleted KkjMatrixItem Id={id} ({item.Kompetensi})",
        targetId: id, targetType: "KkjMatrixItem");

    return Json(new { success = true });
}
```

Note: `AssessmentCompetencyMap` also has a FK to `KkjMatrixItem` with `DeleteBehavior.Cascade`, meaning those rows auto-delete with the parent. No separate guard needed for that table — only `UserCompetencyLevel` uses `DeleteBehavior.Restrict`.

### Pattern 4: Nav Sidebar Addition for Admin Role

**What:** Add "Kelola Data" menu item to `_Layout.cshtml`, visible only when `userRole == "Admin"` (RoleLevel == 1).

**When to use:** One-time layout change for phase 47 that persists for all v2.3 phases.

**Example:**
```html
@* In _Layout.cshtml navbar, after existing nav items *@
@if (userRole == "Admin")
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-controller="Admin" asp-action="Index">
            <i class="bi bi-gear-fill me-1"></i>Kelola Data
        </a>
    </li>
}
```

### Pattern 5: Client-Side Edit Mode Toggle

**What:** JavaScript toggles between read mode (compact, no inputs) and edit mode (all 18 columns, input fields). On page load, the full data is embedded in the page as a JSON array in a `<script>` tag or hidden field. Edit mode renders the table with inputs using JS DOM manipulation.

**When to use:** The spreadsheet-style toggle approach decided by user.

**Key decisions for implementation (Claude's Discretion):**
- Use `<input type="text">` fields inside `<td>` cells (not `contenteditable`) for reliable value extraction and keyboard navigation
- Render read-mode rows from Razor (server-side), then swap to input-based rows via JS when edit mode activates
- Or: render all rows with hidden inputs always present, show/hide columns via CSS classes (simpler toggle)
- Recommended approach: Render input-mode table as hidden, swap visibility on Edit toggle (avoids complex DOM construction in JS)

### Anti-Patterns to Avoid

- **Separate create form/page:** User explicitly wants inline row creation in edit mode, not a modal or separate page.
- **Per-cell auto-save:** User wants bulk save ("Simpan") not auto-save on blur.
- **Using `[FromForm]` for JSON body:** When sending `JSON.stringify` from JS, the controller must use `[FromBody]`, and the antiforgery token goes in the request header, not the body.
- **EF Core tracking conflicts:** If loading existing entity then detaching to update, use `FindAsync` + update properties directly rather than `_context.Update(row)` with a deserialized object (avoids tracking conflicts).
- **Cascade delete assumption:** Only `AssessmentCompetencyMap` has Cascade — `UserCompetencyLevel` has Restrict. Always check `UserCompetencyLevels` count before deleting.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Antiforgery validation | Custom CSRF token logic | `[ValidateAntiForgeryToken]` + jQuery header pattern | Built into ASP.NET Core, proven |
| Role authorization | Manual `if (userRole == "Admin")` checks in every action | `[Authorize(Roles = "Admin")]` on controller class | Fail-safe — returns 403 if not Admin |
| Audit logging | Custom logging code | `AuditLogService.LogAsync()` | Already built and injected in project |
| Upsert logic | Raw SQL MERGE statements | EF Core `FindAsync` + conditional `Add`/`Update` | Type-safe, tracked by EF |
| Clipboard paste parsing | Custom clipboard API implementation | Browser `paste` event on table + `clipboardData.getData('text')` | Native browser API, no library needed |

**Key insight:** The entire infrastructure (DB context, audit log, antiforgery, identity) already exists. This phase is purely UI + controller wiring.

---

## Common Pitfalls

### Pitfall 1: `[FromBody]` + Antiforgery Token Header Mismatch

**What goes wrong:** Sending JSON body with `[ValidateAntiForgeryToken]` fails with 400 if the token is put inside the JSON body instead of the `RequestVerificationToken` HTTP header.

**Why it happens:** `[ValidateAntiForgeryToken]` reads from the HTTP header `RequestVerificationToken` when Content-Type is `application/json`, not from the body.

**How to avoid:** In jQuery AJAX:
```javascript
$.ajax({
    url: '/Admin/KkjMatrixSave',
    type: 'POST',
    contentType: 'application/json',
    headers: {
        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
    },
    data: JSON.stringify(rowsArray),
    success: function(response) { ... }
});
```
Include a hidden `@Html.AntiForgeryToken()` form somewhere in the Razor view so the token is available in the DOM.

### Pitfall 2: EF Core Tracking Conflict on Bulk Update

**What goes wrong:** If you attach a deserialized `KkjMatrixItem` object (from `[FromBody]`) directly to the context via `_context.Update(row)`, EF may throw if another instance with the same key is already tracked.

**Why it happens:** EF Core tracks entities by primary key. Deserializing from JSON creates a new CLR object, but if `FindAsync` was called first, there's already a tracked instance.

**How to avoid:** Always use `FindAsync(id)` to get the tracked entity, then update its properties individually. Never call `_context.Update()` on a deserialized/detached object when another tracked instance may exist.

### Pitfall 3: Clipboard Paste — Tab-Separated vs Comma-Separated

**What goes wrong:** Excel clipboard data is tab-separated (`\t`) with newlines (`\n`) between rows. Parsing as CSV with commas will corrupt data.

**Why it happens:** Excel copies cells as TSV, not CSV.

**How to avoid:** Parse clipboard text by splitting on `\n` first (rows), then `\t` (columns):
```javascript
document.getElementById('pasteZone').addEventListener('paste', function(e) {
    const text = e.clipboardData.getData('text');
    const rows = text.trim().split('\n');
    rows.forEach(function(rowText, rowIndex) {
        const cols = rowText.split('\t');
        // Map cols to corresponding input fields in each row
    });
});
```

### Pitfall 4: Wide Table Horizontal Scroll + Sticky Columns in Edit Mode

**What goes wrong:** In edit mode, the table has 18+ columns (5 info + 13 Target_*), becoming very wide. The sticky-column CSS from the existing `Kkj.cshtml` view uses `position: sticky; left: 0` — this requires the parent container to have `overflow-x: auto`.

**Why it happens:** Sticky positioning only works relative to a scrollable ancestor. If the container doesn't overflow, sticky columns don't "stick".

**How to avoid:** Wrap the edit-mode table in a `div` with `overflow-x: auto; max-height: 80vh`. Apply `position: sticky; left: 0; background-color: white; z-index: 10` to the first 2-3 columns. Copy the existing `.sticky-col`, `thead th` sticky patterns from `Views/CMP/Kkj.cshtml`.

### Pitfall 5: `No` Column Ordering After Insert

**What goes wrong:** After bulk-save with new rows, the `No` (display number) field may collide or create gaps if admin leaves it blank or enters duplicate values.

**Why it happens:** `No` is a display field (not the PK), and its value comes from admin input.

**How to avoid:** In the server-side bulk-save, after all rows are saved, re-sequence `No` values based on row order as a post-save step — or trust the admin to manage it (simpler: don't auto-resequence, just validate for duplicate `No` values and return an error).

### Pitfall 6: Missing AdminController Route Registration

**What goes wrong:** `/Admin/Index` returns 404 because MVC route conventions need the controller to exist with that exact name.

**Why it happens:** New controller, no route confusion, but namespace or file location must match project conventions.

**How to avoid:** Place `AdminController.cs` in `Controllers/` folder (same as all other controllers). No custom route attributes needed — default MVC convention `{controller}/{action}` handles `/Admin/Index`, `/Admin/KkjMatrix`, etc.

---

## Code Examples

### Complete jQuery AJAX Bulk-Save Call (JSON body + antiforgery header)

```javascript
// Source: pattern derived from existing CMPController AJAX usage + ASP.NET Core docs
function saveAllRows() {
    var rows = [];
    $('#editTable tbody tr').each(function() {
        var row = {
            id:           parseInt($(this).data('id')) || 0,
            no:           parseInt($(this).find('[name="No"]').val()) || 0,
            skillGroup:   $(this).find('[name="SkillGroup"]').val(),
            subSkillGroup: $(this).find('[name="SubSkillGroup"]').val(),
            indeks:       $(this).find('[name="Indeks"]').val(),
            kompetensi:   $(this).find('[name="Kompetensi"]').val(),
            target_SectionHead: $(this).find('[name="Target_SectionHead"]').val() || '-',
            // ... all 13 Target_* fields
        };
        rows.push(row);
    });

    var token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Admin/KkjMatrixSave',
        type: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(rows),
        success: function(response) {
            if (response.success) {
                // Reload page or switch to read mode
                location.reload();
            } else {
                alert('Error: ' + response.message);
            }
        },
        error: function(xhr) {
            alert('Server error: ' + xhr.statusText);
        }
    });
}
```

### Delete with Guard (jQuery AJAX)

```javascript
// Source: pattern derived from existing CMPController delete patterns
function deleteRow(id, kompetensi) {
    if (!confirm('Hapus "' + kompetensi + '"?')) return;

    var token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Admin/KkjMatrixDelete',
        type: 'POST',
        data: { id: id, __RequestVerificationToken: token },
        success: function(response) {
            if (response.success) {
                $('tr[data-id="' + id + '"]').remove();
            } else if (response.blocked) {
                alert(response.message); // "Tidak dapat dihapus — digunakan oleh N worker."
            } else {
                alert('Error: ' + response.message);
            }
        }
    });
}
```

### Edit Mode Toggle (JS pattern)

```javascript
// Toggle edit mode: show edit table, hide read table
document.getElementById('btnEdit').addEventListener('click', function() {
    document.getElementById('readTable').classList.add('d-none');
    document.getElementById('editTable').classList.remove('d-none');
    document.getElementById('editActions').classList.remove('d-none');
    document.getElementById('btnEdit').classList.add('d-none');
});

document.getElementById('btnCancel').addEventListener('click', function() {
    // Revert: hide edit table, show read table
    document.getElementById('editTable').classList.add('d-none');
    document.getElementById('readTable').classList.remove('d-none');
    document.getElementById('editActions').classList.add('d-none');
    document.getElementById('btnEdit').classList.remove('d-none');
});
```

### Razor View: Embed Data for Edit Mode

```cshtml
@* Pass all items as JSON to JavaScript for client-side edit rendering *@
@{
    var itemsJson = System.Text.Json.JsonSerializer.Serialize(Model,
        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });
}
<script>
    var kkjItems = @Html.Raw(itemsJson);
</script>
```

### Admin Index Hub Page Card Pattern

```cshtml
@* Category A: Master Data *@
<h5 class="fw-bold text-primary mb-3">Master Data</h5>
<div class="row g-3 mb-4">
    <div class="col-md-4">
        <a href="@Url.Action("KkjMatrix", "Admin")" class="text-decoration-none">
            <div class="card shadow-sm h-100">
                <div class="card-body">
                    <div class="fw-bold"><i class="bi bi-table me-2 text-primary"></i>KKJ Matrix</div>
                    <small class="text-muted">Kelola item KKJ Matrix (master kompetensi)</small>
                </div>
            </div>
        </a>
    </div>
    @* ... MDAT-02, MDAT-03 cards — stubs for future phases *@
</div>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Full-page form submit for CRUD | AJAX JSON endpoints + partial update | Established project pattern | No page reload on save/delete |
| Hardcoded KKJ data in C# | Seeded to SQL Server via `SeedMasterData` | Previous phase | Data is in DB, ready to CRUD |
| No admin management UI | Admin Portal (phases 47-58) | Now (v2.3) | Admin can manage without code changes |

**What already exists (do not rebuild):**
- `KkjMatrixItem` model with all 18 columns — in `Models/KkjModels.cs`
- `DbSet<KkjMatrixItem> KkjMatrices` — in `ApplicationDbContext`
- `KkjMatrices` SQL table — exists in SQL Server with 17 seeded rows
- `PositionTargetHelper` — maps position strings to Target_* column names (may be useful for display)
- Read-only view in `Views/CMP/Kkj.cshtml` — reference for sticky-column CSS patterns
- `AuditLogService` — inject and use for all write operations
- jQuery 3.7.1, Bootstrap 5.3, Bootstrap Icons — all loaded in `_Layout.cshtml`

---

## Open Questions

1. **JSON property casing in `[FromBody]` deserialization**
   - What we know: ASP.NET Core's default JSON deserializer (System.Text.Json) uses camelCase by default. The `KkjMatrixItem` properties are PascalCase.
   - What's unclear: Whether the default `AddControllersWithViews()` in `Program.cs` has custom JSON options configured.
   - Recommendation: In the JS `saveAllRows()`, use PascalCase property names in the JSON object to match the C# model exactly. OR add `[JsonPropertyName("...")]` attributes. Simplest fix: configure `options.JsonSerializerOptions.PropertyNameCaseInsensitive = true` globally in Program.cs, or just match casing in JS.

2. **Pagination vs full list for KKJ Matrix**
   - What we know: Current seed has 17 rows. User estimates 50-100+ rows possible. No pagination exists in similar admin pages.
   - What's unclear: Will 100+ rows cause browser performance issues in edit mode with 100 rows × 18 input fields = 1800 inputs?
   - Recommendation: Load all rows without pagination for simplicity. If performance is an issue post-implementation, add virtual scrolling or pagination. Start simple.

3. **Multi-row paste target area**
   - What we know: User wants Excel-paste support. Locked implementation details are Claude's Discretion.
   - What's unclear: Should paste activate on a specific "paste zone" or on the entire edit table?
   - Recommendation: Add a `paste` event listener on the `#editTable` element. Detect paste events, parse TSV clipboard data, and populate rows starting from the currently focused row. If no row is focused, append rows at the bottom (as new rows with Id=0).

---

## Validation Architecture

nyquist_validation is enabled in `.planning/config.json`.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | No automated test framework detected in project |
| Config file | None — Wave 0 gap |
| Quick run command | Manual: navigate to `/Admin/KkjMatrix` and verify behavior |
| Full suite command | Manual verification per success criteria |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MDAT-01 | GET `/Admin/KkjMatrix` returns 200 with all rows listed | smoke (manual) | N/A — no test runner | ❌ Wave 0 |
| MDAT-01 | Clicking Edit shows all 18 columns with input fields | manual/UI | N/A | ❌ Wave 0 |
| MDAT-01 | POST `/Admin/KkjMatrixSave` with valid rows updates DB | integration (manual) | N/A | ❌ Wave 0 |
| MDAT-01 | POST `/Admin/KkjMatrixSave` with Id=0 row inserts new record | integration (manual) | N/A | ❌ Wave 0 |
| MDAT-01 | Delete unreferenced item: succeeds, row removed from table | manual | N/A | ❌ Wave 0 |
| MDAT-01 | Delete referenced item (has UserCompetencyLevel): blocked with worker count | manual | N/A | ❌ Wave 0 |
| MDAT-01 | Non-Admin role cannot access `/Admin/KkjMatrix` (403) | manual | N/A | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** Manual smoke — open browser, verify page loads and primary action works
- **Per wave merge:** Full manual checklist against all 7 behaviors above
- **Phase gate:** All 7 behaviors confirmed green before `/gsd:verify-work`

### Wave 0 Gaps
- No automated test framework exists in the project. All validation for this phase is manual browser-based verification.
- Recommend creating a verification checklist as part of the PLAN rather than automated tests.

*(If a test project is added in future phases: xUnit + Microsoft.AspNetCore.Mvc.Testing would be the standard for ASP.NET Core 8 integration tests.)*

---

## Sources

### Primary (HIGH confidence)
- Direct code inspection of `Models/KkjModels.cs` — KkjMatrixItem model structure (18 columns confirmed)
- Direct code inspection of `Data/ApplicationDbContext.cs` — DbSet, table names, FK constraints (Restrict on UserCompetencyLevel, Cascade on AssessmentCompetencyMap)
- Direct code inspection of `Data/SeedMasterData.cs` — 17 seeded rows, seed pattern
- Direct code inspection of `Controllers/CMPController.cs` — AJAX patterns, `[ValidateAntiForgeryToken]`, `Json()` return pattern, `AuditLogService` usage
- Direct code inspection of `Views/CMP/Kkj.cshtml` — sticky-column CSS, table structure for 18-column wide table
- Direct code inspection of `Views/Shared/_Layout.cshtml` — CDN dependencies (jQuery 3.7.1, Bootstrap 5.3.0, Bootstrap Icons 1.10.0), nav structure, TempData alert pattern
- Direct code inspection of `Models/UserRoles.cs` — Admin role constant = "Admin", RoleLevel 1
- Direct code inspection of `Models/ApplicationUser.cs` — RoleLevel field

### Secondary (MEDIUM confidence)
- ASP.NET Core 8 docs on `[FromBody]` + antiforgery: header-based token required when Content-Type is application/json (verified through existing project patterns in `Views/CMP/StartExam.cshtml` using `'RequestVerificationToken'` header in fetch calls)
- Excel clipboard TSV format: well-established browser behavior, verified through general knowledge of clipboard API

### Tertiary (LOW confidence)
- Browser performance estimate for 100 rows × 18 inputs: based on general JS DOM knowledge, not benchmarked for this project

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries confirmed in project files
- Architecture patterns: HIGH — derived directly from existing controller/view code in project
- Pitfalls: HIGH for EF/antiforgery (confirmed from code), MEDIUM for clipboard TSV/performance
- Delete guard logic: HIGH — FK constraint type confirmed from ApplicationDbContext

**Research date:** 2026-02-26
**Valid until:** 2026-03-28 (stable — no external library changes expected)
