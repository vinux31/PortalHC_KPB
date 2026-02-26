# Phase 48: KKJ-IDP Mapping Editor (CPDP Items Manager) - Research

**Researched:** 2026-02-26
**Domain:** ASP.NET Core MVC — Admin CRUD UI for competency items (CpdpItem) with section filtering, spreadsheet-style inline editing, and Excel export
**Confidence:** HIGH

<user_constraints>
## User Constraints (from Phase Description)

### Phase Overview
Admin can view, create, edit, and delete CPDP Items (CpdpItem) with section filter through a dedicated management page. Previously named "cpdp-items-manager", now renamed to **"KKJ-IDP Mapping Editor"** to reflect its role in mapping KKJ competencies to IDP activities.

### Locked Decisions
- **Page location:** `/Admin/CpdpItems` — added to AdminController (reuses phase 47 scaffold)
- **Section filter:** Dropdown menu with values: RFCC, GAST, NGP, DHT — filter persists in read-mode table
- **Read-mode table:** Compact display with columns: `No`, `NamaKompetensi`, `IndikatorPerilaku`, and Action buttons (Edit/Delete)
- **Behavioral indicators:** Not shown in read-mode table (too wide); visible only in edit-mode
- **Edit mode:** All 6 columns displayed (No, NamaKompetensi, IndikatorPerilaku, DetailIndikator, Silabus, TargetDeliverable) with horizontal scroll
- **Spreadsheet-style inline editing:** Same pattern as Phase 47 KKJ Matrix — click "Edit" to activate, "Simpan" to bulk-save, "Batal" to discard
- **Multi-cell selection and bulk operations:** Excel-like experience (Ctrl+C copy, Ctrl+V paste, range selection)
- **Bulk save endpoint:** Single POST that upserts all rows (update existing by Id, insert rows with Id=0)
- **Delete guard:** Check for references to IDP records (IdpItem.Kompetensi foreign key by name) before deletion
- **Toast notification:** Show "Data berhasil disimpan" after successful save with page reload (same pattern as phase 47)
- **CMP/Mapping view:** Read-only view updated to new format (column headers updated, no write capabilities)
- **Export to Excel:** Admin can export filtered table to Excel file (optional enhancement — Claude's discretion on timing)

### Claude's Discretion
- Excel export implementation timing (include in phase 48 or defer to later phase)
- Exact styling of edit-mode columns (column widths, padding)
- Error display positioning (inline toast vs modal)
- Pagination vs full list (estimated 100-150 CPDP items, may paginate if performance issue)
- Grouped display by bagian (show all sections in one table with filter, or separate section tables)

### Deferred Ideas (OUT OF SCOPE)
- Full IDP linking UI (IDP dashboard can link to CPDP items separately)
- CPDP item versioning/history tracking
- Bulk import from external CPDP source
- Notification when CPDP items are referenced by active IDP records

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MDAT-02 | Admin can view, create, edit, and delete CPDP Items (CpdpItem) with section filter through a dedicated management page | AdminController CpdpItems action scaffold, spreadsheet-mode inline editing with section dropdown filter, bulk-save JSON endpoint, delete-with-reference-guard endpoint, read/edit view toggle, section filter persistence |

</phase_requirements>

---

## Summary

Phase 48 builds on the Admin Portal infrastructure established in Phase 47. It implements the second Master Data management tool: **KKJ-IDP Mapping Editor** (CPDP Items Manager). The `CpdpItem` model already exists in the database with 7 columns (Id, No, NamaKompetensi, IndikatorPerilaku, DetailIndikator, Silabus, TargetDeliverable, and Section). The Section column was added in Quick Task #14 specifically to enable per-bagian filtering for this phase.

The primary technical challenge is replicating the Phase 47 spreadsheet-editing pattern for a different entity with different columns, plus adding section-based filtering. Unlike KKJ Matrix (which has 18 columns but shows only 5 in read-mode), CPDP Items will show 3 core columns in read-mode and expand to 6 in edit-mode with horizontal scroll. The section filter is a dropdown menu that filters the table client-side before rendering, and is scoped to the current session (persists until changed).

The delete guard differs from Phase 47: instead of checking `UserCompetencyLevel` (which references `KkjMatrixItemId`), Phase 48 must check `IdpItem` records that reference the `CpdpItem.NamaKompetensi` value by string matching (since IdpItem.Kompetensi is a nullable string, not a foreign key). The guard should count matching `IdpItem` records where `IdpItem.Kompetensi == CpdpItem.NamaKompetensi` and block deletion if any exist.

**Primary recommendation:** Extend AdminController with `CpdpItems` action (GET returns read view with section filter, POST bulk-save endpoint, DELETE endpoint with reference guard). Use same patterns as Phase 47 (spread to sections, multi-cell selection, bulk save, toast on success). Section filter is stored as hidden field or query parameter, and can be toggled via dropdown without full page reload (client-side filter or AJAX fetch).

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 (project target) | Controller + Razor views | Already the project framework |
| Entity Framework Core | 8.0.0 | DB access to SQL Server | Already configured in project |
| Microsoft.AspNetCore.Identity | 8.0.0 | `[Authorize(Roles)]` authentication | Already in use across all controllers |
| Bootstrap | 5.3.0 (CDN) | Responsive table, button, dropdown UI | Already in `_Layout.cshtml` |
| Bootstrap Icons | 1.10.0 (CDN) | Icons (bi-pencil, bi-trash, bi-funnel) | Already in layout |
| jQuery | 3.7.1 (CDN) | AJAX $.ajax calls, DOM manipulation | Already in layout, used across all views |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| AuditLogService | project class | Log admin Create/Update/Delete actions | Use for all write operations — same as Phase 47 |
| System.Text.Json | .NET built-in | Serialize/deserialize JSON body in bulk-save | Use for `[FromBody]` model binding |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Section dropdown filter (client-side) | Server-side AJAX filter reload | Client-side is instant, no server round-trip; server-side would be slower but could pagination |
| String-based IdpItem.Kompetensi matching | Foreign key relationship | String matching is simpler given existing schema; FK would require migration + data cleanup |
| Excel export via OpenXml/EppPlus | Manual CSV generation | OpenXml is more robust for complex sheets; CSV is simpler if deferred to later phase |
| Single sections dropdown | Multiple-select checkboxes | Dropdown is simpler UX for a 4-value filter; checkbox would allow bulk-filtering |

**Installation:** No new packages needed. All dependencies already in project.

---

## Architecture Patterns

### Recommended Project Structure

New files to create:
```
Controllers/
  AdminController.cs                  # Extend from Phase 47 — add CpdpItems actions

Views/Admin/
  CpdpItems.cshtml                    # Read/Edit view for CpdpItem CRUD with section filter
```

No new models needed — `CpdpItem` and `ApplicationDbContext` already exist. Minor updates to Phase 47's AdminController to add new actions.

### Pattern 1: Section Filter Dropdown (Persistent Client-Side)

**What:** Dropdown showing RFCC, GAST, NGP, DHT. On selection, client-side JS filters the table rows and stores the selected value in a hidden field or localStorage. When user switches to edit mode or bulk-saves, the filter value persists.

**When to use:** Whenever you have a categorical breakdown of data (sections, departments, categories) and want instant filtering without server round-trip.

**Example:**
```csharp
// In AdminController.CpdpItems GET action
public async Task<IActionResult> CpdpItems()
{
    var items = await _context.CpdpItems
        .OrderBy(c => c.No)
        .ToListAsync();

    // Sections list for dropdown — could be hardcoded or queried
    ViewBag.Sections = new[] { "RFCC", "GAST", "NGP", "DHT" };

    return View(items);
}
```

In the Razor view:
```cshtml
<div class="mb-3">
    <label for="sectionFilter" class="form-label">Filter Bagian:</label>
    <select id="sectionFilter" class="form-select">
        <option value="">Semua Bagian</option>
        <option value="RFCC">RFCC</option>
        <option value="GAST">GAST</option>
        <option value="NGP">NGP</option>
        <option value="DHT">DHT</option>
    </select>
</div>
```

JavaScript to handle filtering:
```javascript
document.getElementById('sectionFilter').addEventListener('change', function(e) {
    var selectedSection = e.target.value;
    var rows = document.querySelectorAll('#readTable tbody tr');

    rows.forEach(function(row) {
        var rowSection = row.dataset.section;  // Hidden data-section attribute
        if (selectedSection === '' || rowSection === selectedSection) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
});
```

**Key decision:** Store selected section in a hidden input `<input type="hidden" id="selectedSection" value="" />` before bulk-save, so the server knows which section was active (useful for logs or future filtering logic).

### Pattern 2: Bulk-Save with Section Scope

**What:** POST endpoint receives filtered table data (may represent only one section). Server validates that all rows belong to the same section (or allow cross-section rows if admin manually edited). Upserts all rows.

**When to use:** Combined with section filter to ensure bulk operations are intentional.

**Example:**
```csharp
// POST /Admin/CpdpItemsSave
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CpdpItemsSave([FromBody] List<CpdpItem> rows)
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
                _context.CpdpItems.Add(row);
            }
            else
            {
                // Existing row — update
                var existing = await _context.CpdpItems.FindAsync(row.Id);
                if (existing != null)
                {
                    existing.No = row.No;
                    existing.NamaKompetensi = row.NamaKompetensi;
                    existing.IndikatorPerilaku = row.IndikatorPerilaku;
                    existing.DetailIndikator = row.DetailIndikator;
                    existing.Silabus = row.Silabus;
                    existing.TargetDeliverable = row.TargetDeliverable;
                    existing.Status = row.Status;
                    existing.Section = row.Section;
                    _context.CpdpItems.Update(existing);
                }
            }
        }

        await _context.SaveChangesAsync();

        var actor = await _userManager.GetUserAsync(User);
        await _auditLog.LogAsync(actor!.Id, actor.FullName, "BulkUpdate",
            $"CPDP Items bulk-save: {rows.Count} rows", targetType: "CpdpItem");

        return Json(new { success = true, message = $"{rows.Count} baris disimpan." });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}
```

### Pattern 3: Delete with IdpItem Reference Guard

**What:** Before deleting a CpdpItem, check if any IdpItem records reference it by matching `IdpItem.Kompetensi == CpdpItem.NamaKompetensi`. Block deletion if references exist.

**When to use:** All deletes of CpdpItem.

**Example:**
```csharp
// POST /Admin/CpdpItemDelete
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CpdpItemDelete(int id)
{
    var item = await _context.CpdpItems.FindAsync(id);
    if (item == null)
        return Json(new { success = false, message = "Item tidak ditemukan." });

    // Check for references in IdpItem (string match on Kompetensi field)
    var usageCount = await _context.IdpItems
        .CountAsync(i => i.Kompetensi == item.NamaKompetensi);

    if (usageCount > 0)
        return Json(new { success = false, blocked = true,
            message = $"Tidak dapat dihapus — digunakan oleh {usageCount} IDP record." });

    _context.CpdpItems.Remove(item);
    await _context.SaveChangesAsync();

    var actor = await _userManager.GetUserAsync(User);
    await _auditLog.LogAsync(actor!.Id, actor.FullName, "Delete",
        $"Deleted CpdpItem Id={id} ({item.NamaKompetensi})",
        targetId: id, targetType: "CpdpItem");

    return Json(new { success = true });
}
```

**Important note:** This uses a string-based comparison, not a foreign key. This is by design given the existing schema. If a `NamaKompetensi` value is changed on an existing CpdpItem, orphaned IdpItem records will no longer be "blocked" by the delete guard (they won't match the new name). This is a known limitation of the string-based approach and could be addressed in a future data integrity phase.

### Pattern 4: Multi-Cell Selection and Range Copy (Excel-like)

**What:** User can click a cell, then Shift+click another to select a range. Ctrl+C copies the range as Tab-Separated Values (TSV). Ctrl+V pastes starting from the currently focused cell.

**When to use:** Spreadsheet-mode editing where bulk data entry from Excel is expected.

**Example (JavaScript):**
```javascript
let selectedCells = [];
let startCell = null;

document.addEventListener('click', function(e) {
    if (e.target.matches('#editTable td input')) {
        const cell = e.target.parentElement;  // td

        if (e.shiftKey && startCell) {
            // Range select: select all cells between startCell and this cell
            selectRange(startCell, cell);
        } else if (e.ctrlKey || e.metaKey) {
            // Add to existing selection
            if (selectedCells.includes(cell)) {
                selectedCells = selectedCells.filter(c => c !== cell);
                cell.classList.remove('cell-selected');
            } else {
                selectedCells.push(cell);
                cell.classList.add('cell-selected');
            }
        } else {
            // Single click: clear previous and select this
            selectedCells.forEach(c => c.classList.remove('cell-selected'));
            selectedCells = [cell];
            cell.classList.add('cell-selected');
        }
        startCell = cell;
    }
});

document.addEventListener('keydown', function(e) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'c') {
        // Copy selected range as TSV
        e.preventDefault();
        copySelectedCells();
    } else if ((e.ctrlKey || e.metaKey) && e.key === 'v') {
        // Paste from clipboard
        e.preventDefault();
        pasteFromClipboard();
    }
});
```

**CSS for visual feedback:**
```css
td.cell-selected input {
    background-color: #cfe9ff;
    border-color: #0d6efd;
}
```

### Pattern 5: Edit/Read Mode Toggle (Reuse Phase 47)

**What:** Same as Phase 47 — buttons "Edit" and "Batal"/"Simpan" toggle between read and edit view. Entire view structure can reuse Phase 47 patterns.

**When to use:** All spreadsheet-mode CRUD pages in Admin Portal (applies to phases 47-58).

### Anti-Patterns to Avoid

- **Server-side section filter with full reload:** Every section change would fetch from server unnecessarily. Client-side filter is instant.
- **Per-row save on blur:** User wants bulk "Simpan" not auto-save. Matches phase 47 UX.
- **Hardcoding section list:** Use seeded `KkjBagian` table or a simple enum/constant list so it's maintainable.
- **Deleting without counting IdpItem references:** String matching is easy to miss — always count first, block if > 0.
- **Mixing section filters with edit mode:** Keep filter active in edit mode. When user clicks "Simpan", all visible rows (filtered or not) are saved.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Section dropdown rendering | Custom JS <select> builder | Bootstrap `form-select` class + standard HTML <option> | Built-in, accessible, styled consistently |
| Client-side table filtering | Regex or string search logic | Simple `.display` CSS property toggle per row | Fast, no DOM manipulation, browser-native |
| Excel clipboard paste parsing | Custom clipboard.js | Browser `paste` event + `clipboardData.getData()` + split on `\n` and `\t` | Native browser API, no dependencies |
| Delete reference checking | Custom query writing | Entity Framework's `.CountAsync()` with `.Where()` | Type-safe, migrations handle schema changes |
| Multi-cell selection | DIY range algorithm | Store clicked cells in array, apply/remove CSS class | Simple, no complex selection logic |

**Key insight:** Phase 48 is almost entirely a copy-paste-and-adapt of Phase 47. The only substantive new logic is the section filter dropdown and the IdpItem reference guard. Everything else (edit toggle, bulk save, copy-paste, multi-cell selection) can be transplanted from Phase 47's JavaScript with minimal changes to column names and table IDs.

---

## Common Pitfalls

### Pitfall 1: Section Filter Not Persisting in Edit Mode

**What goes wrong:** User filters to "RFCC" section in read mode, clicks Edit, and the filter is lost — the edit table shows all 4 sections' rows.

**Why it happens:** Edit mode is toggled via `d-none`/`d-flex` visibility, and both tables (read and edit) are rendered from the same data set, but the filter state is only stored in the read-mode table's hidden input. When switching to edit mode, the script forgets which section was selected.

**How to avoid:** Store selected section in a global JavaScript variable or persistent hidden input:
```javascript
var currentSection = '';  // Global variable
document.getElementById('sectionFilter').addEventListener('change', function(e) {
    currentSection = e.target.value;
    // ... filter read table
    // Also filter edit table if it's already rendered
});
```
When toggling to edit mode, apply the same filter to the edit table's rows.

### Pitfall 2: IdpItem String Matching is Fragile

**What goes wrong:** Admin changes a CpdpItem's `NamaKompetensi` from "Komunikasi" to "Komunikasi Efektif". The old IdpItem records with `Kompetensi = "Komunikasi"` are no longer blocked by the delete guard because the new name doesn't match.

**Why it happens:** String-based foreign key matching is inherently fragile. The IdpItem.Kompetensi field is not a proper FK, just a text reference.

**How to avoid:** Document this limitation clearly in code comments. Add a validation check in the edit endpoint to warn if changing a `NamaKompetensi` that has IdpItem references:
```csharp
// In bulk-save, detect if NamaKompetensi was changed for an existing row
var oldName = existing.NamaKompetensi;
var newName = row.NamaKompetensi;

if (oldName != newName) {
    var refCount = await _context.IdpItems
        .CountAsync(i => i.Kompetensi == oldName);
    if (refCount > 0) {
        // Warn admin or block the change
        return Json(new { success = false, message =
            $"Tidak bisa ubah NamaKompetensi — {refCount} IDP record masih mereferensi nama lama." });
    }
}
```

### Pitfall 3: Status Column Not Persisted

**What goes wrong:** CpdpItem has a `Status` column (likely for approval state), but the edit form doesn't include an input for it, so bulk-save overwrites it with null or empty string.

**Why it happens:** Easy to forget a column when copying from Phase 47 (which has different columns). CpdpItem has 7 columns vs KkjMatrixItem's 18.

**How to avoid:** Generate a complete column list from the model and ensure every column is either:
1. Included as an `<input>` in edit mode, OR
2. Explicitly preserved during update (copy from existing entity)

In the bulk-save endpoint, make sure all 7 CpdpItem columns are updated:
```csharp
existing.No = row.No;
existing.NamaKompetensi = row.NamaKompetensi;
existing.IndikatorPerilaku = row.IndikatorPerilaku;
existing.DetailIndikator = row.DetailIndikator;
existing.Silabus = row.Silabus;
existing.TargetDeliverable = row.TargetDeliverable;
existing.Status = row.Status;        // ← Don't forget this
existing.Section = row.Section;      // ← Or this
```

### Pitfall 4: Section Dropdown Value Mismatch (Case Sensitivity)

**What goes wrong:** In the dropdown, the section options are "RFCC", "GAST", but CpdpItem.Section is stored as "rfcc" or "Rfcc" in the database (case mismatch). The filter doesn't match rows.

**Why it happens:** String comparison is case-sensitive by default in JavaScript and EF Core.

**How to avoid:** Always normalize case when comparing sections. In JavaScript:
```javascript
var rowSection = row.dataset.section.toUpperCase();
var selectedSection = e.target.value.toUpperCase();
if (selectedSection === '' || rowSection === selectedSection) { ... }
```

In database seeding and CpdpItem initialization, ensure Section values are always UPPERCASE (or always match the dropdown values exactly).

### Pitfall 5: Horizontal Scroll Breaking Sticky Columns

**What goes wrong:** In edit mode, the first 2 columns are sticky (No, NamaKompetensi) to stay visible while scrolling. But the sticky positioning breaks if the parent container doesn't have `overflow-x: auto`.

**Why it happens:** CSS `position: sticky` requires a scrollable ancestor. If the container is not explicitly set to overflow-x, sticky columns don't "stick".

**How to avoid:** Wrap the edit table in a container with explicit overflow:
```html
<div id="editTableContainer" style="overflow-x: auto; max-height: 80vh;">
    <table id="editTable" class="table">
        <!-- columns with sticky styling -->
    </table>
</div>
```

Apply CSS to first 2 columns:
```css
#editTable th:first-child,
#editTable td:first-child,
#editTable th:nth-child(2),
#editTable td:nth-child(2) {
    position: sticky;
    left: 0;
    background-color: white;
    z-index: 10;
}
```

---

## Code Examples

### Complete Section Filter Dropdown (HTML + JS)

```html
<!-- In CpdpItems.cshtml view -->
<div class="mb-3">
    <label for="sectionFilter" class="form-label">Filter Bagian:</label>
    <select id="sectionFilter" class="form-select">
        <option value="">Semua Bagian</option>
        <option value="RFCC">RFCC</option>
        <option value="GAST">GAST</option>
        <option value="NGP">NGP</option>
        <option value="DHT">DHT</option>
    </select>
</div>

<input type="hidden" id="selectedSection" value="" />
<input type="hidden" name="__RequestVerificationToken" id="antiForgeryToken" />

<script>
document.getElementById('sectionFilter').addEventListener('change', function(e) {
    var selectedSection = e.target.value;
    document.getElementById('selectedSection').value = selectedSection;

    var rows = document.querySelectorAll('#readTable tbody tr, #editTable tbody tr');
    rows.forEach(function(row) {
        var rowSection = row.dataset.section || '';
        if (selectedSection === '' || rowSection === selectedSection) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
});
</script>
```

### Delete with IdpItem Reference Guard (C#)

```csharp
// Source: Pattern derived from Phase 47 + CpdpItem schema
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CpdpItemDelete(int id)
{
    var item = await _context.CpdpItems.FindAsync(id);
    if (item == null)
        return Json(new { success = false, message = "CPDP item tidak ditemukan." });

    // Check for IDP records that reference this item by NamaKompetensi
    var usageCount = await _context.IdpItems
        .CountAsync(i => i.Kompetensi == item.NamaKompetensi);

    if (usageCount > 0)
        return Json(new { success = false, blocked = true,
            message = $"Tidak dapat dihapus — digunakan oleh {usageCount} IDP record." });

    _context.CpdpItems.Remove(item);
    await _context.SaveChangesAsync();

    var actor = await _userManager.GetUserAsync(User);
    await _auditLog.LogAsync(actor!.Id, actor.FullName, "Delete",
        $"Deleted CpdpItem Id={id} ({item.NamaKompetensi})",
        targetId: id, targetType: "CpdpItem");

    return Json(new { success = true });
}
```

### Bulk Save with All Columns (JSON Endpoint)

```javascript
// Source: Pattern derived from Phase 47 + CpdpItem columns
function saveAllRows() {
    var rows = [];
    var sections = new Set();

    $('#editTable tbody tr').each(function() {
        var row = {
            id:                   parseInt($(this).data('id')) || 0,
            no:                   $(this).find('[name="No"]').val(),
            namaKompetensi:       $(this).find('[name="NamaKompetensi"]').val(),
            indikatorPerilaku:    $(this).find('[name="IndikatorPerilaku"]').val(),
            detailIndikator:      $(this).find('[name="DetailIndikator"]').val(),
            silabus:              $(this).find('[name="Silabus"]').val(),
            targetDeliverable:    $(this).find('[name="TargetDeliverable"]').val(),
            status:               $(this).find('[name="Status"]').val() || '',
            section:              $(this).find('[name="Section"]').val()
        };
        rows.push(row);
        if (row.section) sections.add(row.section);
    });

    if (rows.length === 0) {
        alert('Tidak ada data untuk disimpan.');
        return;
    }

    var token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Admin/CpdpItemsSave',
        type: 'POST',
        contentType: 'application/json',
        headers: { 'RequestVerificationToken': token },
        data: JSON.stringify(rows),
        success: function(response) {
            if (response.success) {
                // Show toast
                var toast = new bootstrap.Toast(document.getElementById('saveToast'));
                toast.show();
                // Reload after toast fades
                setTimeout(function() { location.reload(); }, 1700);
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

### Razor View: Complete Edit Mode Table Structure

```cshtml
@model List<CpdpItem>

@{
    var itemsJson = System.Text.Json.JsonSerializer.Serialize(Model,
        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });
}

<div class="card">
    <div class="card-header d-flex justify-content-between align-items-center">
        <h5>KKJ-IDP Mapping Editor (CPDP Items)</h5>
        <button id="btnEdit" class="btn btn-sm btn-primary">Edit</button>
    </div>
    <div class="card-body">
        <!-- Section Filter -->
        <div class="mb-3">
            <label for="sectionFilter" class="form-label">Filter Bagian:</label>
            <select id="sectionFilter" class="form-select">
                <option value="">Semua Bagian</option>
                <option value="RFCC">RFCC</option>
                <option value="GAST">GAST</option>
                <option value="NGP">NGP</option>
                <option value="DHT">DHT</option>
            </select>
        </div>

        <!-- Read Mode Table -->
        <div id="readTable" class="table-responsive">
            <table class="table table-sm table-hover">
                <thead>
                    <tr>
                        <th>No</th>
                        <th>Nama Kompetensi</th>
                        <th>Indikator Perilaku</th>
                        <th>Aksi</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in Model)
                    {
                        <tr data-id="@item.Id" data-section="@item.Section">
                            <td>@item.No</td>
                            <td>@item.NamaKompetensi</td>
                            <td>@item.IndikatorPerilaku</td>
                            <td>
                                <button class="btn btn-sm btn-outline-danger" onclick="deleteRow(@item.Id, '@item.NamaKompetensi')">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>

        <!-- Edit Mode Table (hidden by default) -->
        <div id="editTable" class="d-none table-responsive" style="overflow-x: auto; max-height: 80vh;">
            <table class="table table-sm">
                <thead>
                    <tr>
                        <th>No</th>
                        <th>Nama Kompetensi</th>
                        <th>Indikator Perilaku</th>
                        <th>Detail Indikator</th>
                        <th>Silabus</th>
                        <th>Target Deliverable</th>
                        <th>Aksi</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in Model)
                    {
                        <tr data-id="@item.Id" data-section="@item.Section">
                            <td><input type="text" name="No" value="@item.No" class="form-control form-control-sm" /></td>
                            <td><input type="text" name="NamaKompetensi" value="@item.NamaKompetensi" class="form-control form-control-sm" /></td>
                            <td><input type="text" name="IndikatorPerilaku" value="@item.IndikatorPerilaku" class="form-control form-control-sm" /></td>
                            <td><input type="text" name="DetailIndikator" value="@item.DetailIndikator" class="form-control form-control-sm" /></td>
                            <td><input type="text" name="Silabus" value="@item.Silabus" class="form-control form-control-sm" /></td>
                            <td><input type="text" name="TargetDeliverable" value="@item.TargetDeliverable" class="form-control form-control-sm" /></td>
                            <td>
                                <button class="btn btn-sm btn-outline-danger" onclick="deleteRowInline(@item.Id)">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>

        <!-- Edit Mode Actions (hidden by default) -->
        <div id="editActions" class="d-none mt-3">
            <button id="btnSave" class="btn btn-success" onclick="saveAllRows()">Simpan</button>
            <button id="btnCancel" class="btn btn-secondary" onclick="cancelEdit()">Batal</button>
        </div>
    </div>
</div>

<!-- Toast Notification -->
<div class="position-fixed bottom-0 end-0 p-3" style="z-index: 11">
    <div id="saveToast" class="toast" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="toast-body bg-success text-white">
            Data berhasil disimpan.
        </div>
    </div>
</div>

@Html.AntiForgeryToken()

<script>
    var cpdpItems = @Html.Raw(itemsJson);

    document.getElementById('btnEdit').addEventListener('click', function() {
        document.getElementById('readTable').classList.add('d-none');
        document.getElementById('editTable').classList.remove('d-none');
        document.getElementById('editActions').classList.remove('d-none');
        document.getElementById('btnEdit').classList.add('d-none');
    });

    document.getElementById('btnCancel').addEventListener('click', function() {
        document.getElementById('editTable').classList.add('d-none');
        document.getElementById('readTable').classList.remove('d-none');
        document.getElementById('editActions').classList.add('d-none');
        document.getElementById('btnEdit').classList.remove('d-none');
    });
</script>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| CMP read-only CPDP Mapping view | Admin-editable CPDP Items Manager | Phase 48 | Admin can now maintain CPDP master data without code changes |
| Hardcoded CPDP items in C# | Seeded to SQL Server via `SeedMasterData` | Phase 48+ | Data is in DB, ready to CRUD |
| No section-based filtering | Section dropdown filter in admin portal | Phase 48 | Admin can manage per-bagian CPDP items (RFCC, GAST, NGP, DHT) |
| String-based IdpItem.Kompetensi references | Optional: Future migration to proper FK | Deferred | Current approach is workable, migration would require data cleanup |

**What already exists (do not rebuild):**
- `CpdpItem` model with 7 columns (Id, No, NamaKompetensi, IndikatorPerilaku, DetailIndikator, Silabus, TargetDeliverable, Status, Section) — in `Models/KkjModels.cs`
- `DbSet<CpdpItem> CpdpItems` — in `ApplicationDbContext`
- `CpdpItems` SQL table — exists in SQL Server with seeded rows
- `IdpItem` model with `Kompetensi` property (string, nullable) — references CpdpItem by name
- Read-only view in `Views/CMP/Mapping.cshtml` or `Views/CMP/Cpdp.cshtml` — reference for table structure
- `AuditLogService` — inject and use for all write operations
- jQuery 3.7.1, Bootstrap 5.3, Bootstrap Icons — all loaded in `_Layout.cshtml`
- Phase 47's AdminController scaffold + Index hub page — this phase extends it with CpdpItems actions

---

## Open Questions

1. **Excel export implementation timing**
   - What we know: User mentioned export to Excel as a desired feature (locked decision)
   - What's unclear: Should this be included in Phase 48 or deferred to a later admin-tools phase?
   - Recommendation: Scope Phase 48 to CRUD only (no export). If export is needed immediately, add as a simple CSV download to get MVP out faster, then upgrade to proper Excel export in a later phase using a library like OpenXml or ClosedXml.

2. **Section grouping vs single filter dropdown**
   - What we know: User decision is a dropdown filter (locked)
   - What's unclear: Should the edit table show all sections at once, or split into per-section sections (visual grouping)?
   - Recommendation: Single flat table with section column visible in edit mode. When filter is applied, hide rows that don't match. Simpler UX than switching between sections.

3. **Status column purpose and usage**
   - What we know: CpdpItem has a Status column
   - What's unclear: Is this for approval workflow, or just a note field? Should it be editable by admin?
   - Recommendation: Treat as editable text field in edit mode (like other columns). If approval workflow is needed, that's a future phase.

4. **Pagination vs full list for CPDP Items**
   - What we know: Estimated 100-150 CPDP items, seeded in database
   - What's unclear: Will 150 rows × 6 input columns = 900 inputs cause performance issues?
   - Recommendation: Load all rows without pagination for simplicity. If performance is an issue post-implementation, add pagination. Monitor browser devtools memory usage during edit mode to detect problems early.

---

## Validation Architecture

nyquist_validation is enabled in `.planning/config.json`.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | No automated test framework detected in project |
| Config file | None — Wave 0 gap |
| Quick run command | Manual: navigate to `/Admin/CpdpItems` and verify behavior |
| Full suite command | Manual verification per success criteria |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MDAT-02 | GET `/Admin/CpdpItems` returns 200 with all rows listed, section filter dropdown shown | smoke (manual) | N/A — no test runner | ❌ Wave 0 |
| MDAT-02 | Section filter changes table rows visibility in read mode | manual/UI | N/A | ❌ Wave 0 |
| MDAT-02 | Clicking Edit shows all 6 columns with input fields, section filter persists | manual/UI | N/A | ❌ Wave 0 |
| MDAT-02 | POST `/Admin/CpdpItemsSave` with valid rows updates DB | integration (manual) | N/A | ❌ Wave 0 |
| MDAT-02 | POST `/Admin/CpdpItemsSave` with Id=0 row inserts new record | integration (manual) | N/A | ❌ Wave 0 |
| MDAT-02 | Delete unreferenced item: succeeds, row removed from table | manual | N/A | ❌ Wave 0 |
| MDAT-02 | Delete referenced item (has IdpItem with matching Kompetensi): blocked with record count | manual | N/A | ❌ Wave 0 |
| MDAT-02 | Multi-cell selection (Ctrl+click, Shift+click) highlights cells | manual/UI | N/A | ❌ Wave 0 |
| MDAT-02 | Copy selected range (Ctrl+C) to clipboard as TSV | manual/UI | N/A | ❌ Wave 0 |
| MDAT-02 | Non-Admin role cannot access `/Admin/CpdpItems` (403) | manual | N/A | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** Manual smoke — open browser, verify page loads and primary action works
- **Per wave merge:** Full manual checklist against all 10 behaviors above
- **Phase gate:** All 10 behaviors confirmed green before `/gsd:verify-work`

### Wave 0 Gaps
- No automated test framework exists in the project. All validation for this phase is manual browser-based verification.
- Recommend creating a verification checklist as part of the PLAN rather than automated tests.

*(If a test project is added in future phases: xUnit + Microsoft.AspNetCore.Mvc.Testing would be the standard for ASP.NET Core 8 integration tests.)*

---

## Sources

### Primary (HIGH confidence)
- Direct code inspection of `Models/KkjModels.cs` — CpdpItem model structure (7 columns confirmed + Section column from Quick Task #14)
- Direct code inspection of `Models/IdpItem.cs` — Kompetensi property (string, nullable) used for reference matching
- Direct code inspection of `Data/ApplicationDbContext.cs` — DbSet<CpdpItem>, no explicit FK constraint on IdpItem.Kompetensi (string-based)
- Direct code inspection of `Data/SeedMasterData.cs` — seeded CPDP items with section values
- Direct code inspection of `Controllers/CMPController.cs` and existing views — reference for CPDP table structure and read-only display
- Direct code inspection of `Controllers/AdminController.cs` (from Phase 47) — scaffold for extending with CpdpItems actions
- Direct code inspection of `.planning/phases/47-kkj-matrix-manager/47-RESEARCH.md` — bulk-save patterns, delete guard patterns, edit mode toggle patterns

### Secondary (MEDIUM confidence)
- Phase 47 RESEARCH.md on antiforgery token + `[FromBody]` patterns (verified in project code)
- Bootstrap 5 dropdown/form-select component patterns (verified in `_Layout.cshtml` and existing views)
- Section values (RFCC, GAST, NGP, DHT) confirmed in Phase 47 context and Quick Task #14 migration

### Tertiary (LOW confidence)
- Browser performance estimate for 150 rows × 6 inputs: based on general JS DOM knowledge, not benchmarked for this project
- String-based FK matching fragility: known limitation of the schema design, no mitigation beyond warnings

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries confirmed in project files
- Architecture patterns: HIGH — derived directly from Phase 47 research + project code inspection
- Section filter pattern: HIGH — standard DOM filtering, well-established browser technique
- Delete guard logic: HIGH for IdpItem counting, MEDIUM for fragility of string-based matching
- Pitfalls: HIGH for Phase 47 carryover issues, MEDIUM for new pitfalls (section filter, string matching)

**Research date:** 2026-02-26
**Valid until:** 2026-03-26 (stable — no external library changes expected; assume Phase 47 patterns remain valid for Phase 48)

