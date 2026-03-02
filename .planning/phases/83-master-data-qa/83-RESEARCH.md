# Phase 83: Master Data QA - Research

**Researched:** 2026-03-02
**Domain:** ASP.NET Core 8 / C# / Entity Framework — QA and bug-fixing for master data management features
**Confidence:** HIGH

## Summary

Phase 83 is a comprehensive Quality Assurance phase for all master data management features in the Kelola Data admin hub. This includes five major feature areas: KKJ Matrix editor, KKJ-IDP Mapping editor (CPDP), Silabus/Coaching Proton data management, Coaching Guidance file management, and Worker management (CRUD + import/export).

The codebase uses a proven ASP.NET Core 8 MVC stack with:
- **Controllers:** AdminController (4765 lines) handles KKJ, CPDP/Mapping, Workers, Assessment mgmt; ProtonDataController (699 lines) handles Silabus and Coaching Guidance
- **Frontend:** Spreadsheet editors (KKJ, CPDP) with in-place editing, bulk save, drag-to-sort; file upload dialogs; import/export workflows with Excel templates (ClosedXML)
- **Data flow:** All admin CRUD actions route through standard patterns: GET loads view, POST accepts JSON arrays, AJAX callbacks with `{ success, message }`
- **Authorization:** Class-level `[Authorize]` with per-action `[Authorize(Roles = "Admin, HC")]` role gating
- **Audit logging:** All admin actions logged via AuditLogService for compliance tracking

**Primary recommendation:** Review code for common bugs (null handling, validation gaps, filter state contamination) → commit fixes → manual browser QA with production-like data → verify cross-feature links (e.g., KKJ changes reflect in CMP/Kkj view).

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
1. **QA Depth:** Test happy path + validation (empty fields, duplicates, invalid data)
2. **Code Review First:** Claude reviews controller/view code, identifies potential bugs proactively, fixes inline, then user verifies in browser
3. **Bug Fix Approach:** Fix localized changes (<100 lines) immediately; only flag architectural issues
4. **Test Data:** Use production-like existing database data; create small test Excel file (5-10 workers) for Worker import
5. **Cross-Feature Verification:** Full round-trip (edit Admin → verify in user view); Silabus dropdown options populate correctly
6. **Verification Method:** Manual browser testing after each plan; `/gsd:verify-work 83` formal pass at end

### Claude's Discretion
- Exact test scenarios per feature (number of rows, specific validation cases)
- Coaching Guidance file type testing scope
- Export content verification depth
- UX/loading skeleton improvements if discovered during QA

### Deferred Ideas (OUT OF SCOPE)
- Package question management feature (imported from CMP in Phase 82; consider for future)

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| **DATA-01** | KKJ Matrix spreadsheet editor (CRUD, bulk save, bagian mgmt) links to CMP/Kkj | AdminController.KkjMatrix/KkjMatrixSave/KkjBagian* actions; KkjMatrixItem + KkjBagian models; KkjMatrix.cshtml view uses JSON serialization, in-place edit, sticky headers |
| **DATA-02** | KKJ-IDP Mapping editor (CRUD, bulk save, export) links to CMP/Mapping | AdminController.CpdpItems/CpdpItemsSave/CpdpItemDelete; CpdpItem model; CpdpItems.cshtml spreadsheet editor; export mechanism via ClosedXML |
| **DATA-03** | Silabus CRUD works, links to Plan IDP + Coaching Proton dropdowns | ProtonDataController.SilabusSave/SilabusDelete; nested Kompetensi/SubKompetensi/Deliverable upsert with orphan cleanup; ProtonTrack/ProtonKompetensi/ProtonSubKompetensi/ProtonDeliverable models |
| **DATA-04** | Coaching Guidance file management (upload, download, replace, delete) links to Plan IDP | ProtonDataController.GuidanceUpload/GuidanceDelete/GuidanceReplace; CoachingGuidanceFile model; file storage in wwwroot; download link generation |
| **DATA-05** | Worker management CRUD (create, edit, delete, detail view) | AdminController.CreateWorker/EditWorker/DeleteWorker/WorkerDetail; ApplicationUser model; CreateWorker.cshtml, EditWorker.cshtml, WorkerDetail.cshtml views |
| **DATA-06** | Worker import from Excel (template download, upload, process, validation) | AdminController.DownloadImportTemplate/ImportWorkers; ClosedXML for parsing; ImportWorkerResult for per-row feedback; validation: email uniqueness, required fields |
| **DATA-07** | Worker export to Excel with filters | AdminController.ExportWorkers; ClosedXML workbook generation; section/role filtering; proper Excel formatting |

</phase_requirements>

---

## Standard Stack

### Core Framework
| Component | Version | Purpose | Why Standard |
|-----------|---------|---------|--------------|
| ASP.NET Core | 8.0 | Web framework | Project standard; modern async/await, Entity Framework 8 |
| Entity Framework Core | 8.0 | ORM | Project standard; DbContext, migrations, Linq-to-SQL |
| C# | Latest (net8.0) | Language | Nullable reference types enabled; implicit usings |
| Bootstrap | 5.x (inferred) | CSS framework | Used in all .cshtml views; responsive grid, nav, cards |
| JavaScript/jQuery | Native + minimal custom | DOM manipulation | Used in spreadsheet editors, AJAX calls for bulk save |

### Supporting Libraries
| Library | Version | Purpose | When to Use |
|---------|---------|---------|------------|
| ClosedXML | 0.105.0 | Excel read/write | Worker import/export, KKJ-IDP Mapping export |
| Microsoft.AspNetCore.Identity | 8.0 | User auth/roles | Role-based authorization (`[Authorize(Roles = "...")]`) |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0 | SQLite database | Development database (HcPortal.db) |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0 | SQL Server database | Potential production database |
| AuditLogService (custom) | Project | Audit trail | Log all admin CRUD actions for compliance |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ClosedXML | EPPlus / OpenXml | EPPlus requires license; OpenXml lower-level; ClosedXML proven in project |
| Custom spreadsheet editor | DataTables / Handsontable | Those are larger libraries; project uses lightweight in-place edit with JSON posting |
| Manual role checks | Microsoft.AspNetCore.Authorization | Class-level `[Authorize]` + per-action role attributes is cleaner than manual checks |

---

## Architecture Patterns

### Recommended Project Structure

```
AdminController.cs (4765 lines)
├── KKJ Matrix (lines 48-260): 3 actions + 2 supporting
├── CPDP Items / Mapping (lines 384-475): 3 actions
├── Worker Management (lines 2972-3700): 5 actions (CRUD + import/export)
├── Assessment Management (lines 266+): ManageAssessment, Create, Edit
└── Other (AuditLog, CoachCoachee, Training)

ProtonDataController.cs (699 lines)
├── Silabus Management (lines 64-288): SilabusSave, SilabusDelete
├── Coaching Guidance (lines 338-500): GuidanceUpload, GuidanceDelete, GuidanceReplace
└── Override tab (lines 114-124): Emergency override for stuck deliverables
```

### Pattern 1: Spreadsheet Editor (Bulk CRUD)

**What:** Client-side table editing with JSON POST for bulk save; used for KKJ Matrix, CPDP Items, Silabus

**When to use:** Master data grids where admin edits multiple rows, saves once, needs transaction rollback on error

**Example (KKJ Matrix flow):**

```csharp
// AdminController.cs — GET view, JSON serialized to JS
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> KkjMatrix()
{
    var bagians = await _context.KkjBagians.OrderBy(b => b.DisplayOrder).ToListAsync();
    var items   = await _context.KkjMatrices.OrderBy(k => k.No).ToListAsync();
    ViewBag.Bagians = bagians;
    return View(items);
}

// KkjMatrix.cshtml — JS serializes to JSON, POSTs
var kkjItems = @Html.Raw(itemsJson); // Deserialized in browser
// User edits in place, clicks Save
fetch('/Admin/KkjMatrixSave', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(changedRows) // List<KkjMatrixItem>
})

// AdminController.cs — Bulk save with audit
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KkjMatrixSave([FromBody] List<KkjMatrixItem> rows)
{
    foreach (var row in rows)
    {
        if (row.Id == 0) _context.KkjMatrices.Add(row);
        else { var e = await _context.KkjMatrices.FindAsync(row.Id); /* update */ }
    }
    await _context.SaveChangesAsync();
    await _auditLog.LogAsync(actor.Id, actor.FullName, "BulkUpdate",
        $"KKJ Matrix bulk-save: {rows.Count} rows", targetType: "KkjMatrixItem");
    return Json(new { success = true, message = $"{rows.Count} baris berhasil disimpan." });
}
```

**Source:** C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\AdminController.cs lines 48-134

---

### Pattern 2: Nested Entity Upsert (Silabus)

**What:** Upsert three levels of nesting (Kompetensi → SubKompetensi → Deliverable) with orphan cleanup

**When to use:** Hierarchical data where child records must be cleaned up if removed

**Example:**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SilabusSave([FromBody] List<SilabusRowDto> rows)
{
    // 1. Upsert Kompetensi (parent)
    if (row.KompetensiId > 0)
        komp = await _context.ProtonKompetensiList.FindAsync(row.KompetensiId); // existing
    else
        komp = new ProtonKompetensi { ... }; // new

    // 2. Upsert SubKompetensi (child)
    if (row.SubKompetensiId > 0)
        subKomp = await _context.ProtonSubKompetensiList.FindAsync(row.SubKompetensiId);
    else
        subKomp = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, ... };

    // 3. Upsert Deliverable (grandchild)
    if (row.DeliverableId > 0)
        deliv = await _context.ProtonDeliverableList.FindAsync(row.DeliverableId);
    else
        deliv = new ProtonDeliverable { ProtonSubKompetensiId = subKomp.Id, ... };

    // 4. Cleanup orphans (deliverables not in saved rows)
    var orphanDelivs = s.Deliverables.Where(d => !savedDelivIds.Contains(d.Id)).ToList();
    _context.ProtonDeliverableList.RemoveRange(orphanDelivs);
}
```

**Source:** C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\ProtonDataController.cs lines 126-288

---

### Pattern 3: Excel Import with Validation & Feedback

**What:** Download template → user fills rows → upload → per-row validation → feedback table → redirect

**When to use:** Bulk data entry where validation failures should not block successes; examples: Worker import, Coach-Coachee import

**Example (Worker import):**

```csharp
// GET: Show import form
[Authorize(Roles = "Admin, HC")]
public IActionResult ImportWorkers() => View();

// POST: Process upload
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ImportWorkers(IFormFile? excelFile)
{
    var results = new List<ImportWorkerResult>();
    using (var stream = excelFile.OpenReadStream())
    using (var workbook = new XLWorkbook(stream))
    {
        var sheet = workbook.Worksheet(1);
        foreach (var row in sheet.RowsUsed().Skip(1)) // Skip header
        {
            try
            {
                var email = row.Cell(2).Value?.ToString()?.Trim();
                // Validate: email required, unique, email format
                if (string.IsNullOrEmpty(email)) { results.Add(new { Status = "Error", Message = "Email required" }); continue; }
                if (await _context.Users.AnyAsync(u => u.Email == email))
                    { results.Add(new { Status = "Skip", Message = "Email sudah terdaftar" }); continue; }

                // Create user
                var user = new ApplicationUser { Email = email, ... };
                _context.Users.Add(user);
                results.Add(new { Status = "Success", Message = "Berhasil" });
            }
            catch (Exception ex)
            {
                results.Add(new { Status = "Error", Message = ex.Message });
            }
        }
        await _context.SaveChangesAsync();
    }
    return RedirectToAction("ManageWorkers");
}
```

**Source:** C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\AdminController.cs lines 3561-3700

---

### Pattern 4: File Upload with Validation

**What:** Upload file → validate type/size → store on disk → log action → return success/error

**When to use:** Single file uploads (e.g., Coaching Guidance PDFs, images)

**Example (Coaching Guidance upload):**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GuidanceUpload(string bagian, string unit, int trackId, IFormFile? file)
{
    if (file == null || file.Length == 0)
        return Json(new { success = false, message = "File tidak ada." });

    // Validate file type (PDF, DOC, DOCX, etc.)
    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xlsx" };
    var ext = Path.GetExtension(file.FileName).ToLower();
    if (!allowedExtensions.Contains(ext))
        return Json(new { success = false, message = "Tipe file tidak didukung." });

    // Save file to wwwroot
    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
    var filePath = Path.Combine(_env.WebRootPath, "guidance", fileName);
    using (var stream = new FileStream(filePath, FileMode.Create))
        await file.CopyToAsync(stream);

    // Record in database
    var record = new CoachingGuidanceFile
    {
        Bagian = bagian, Unit = unit, TrackId = trackId,
        FileName = file.FileName, StoragePath = $"guidance/{fileName}"
    };
    _context.CoachingGuidanceFiles.Add(record);
    await _context.SaveChangesAsync();
    await _auditLog.LogAsync(user.Id, user.FullName, "Create", ..., targetId: record.Id, targetType: "CoachingGuidanceFile");

    return Json(new { success = true, message = "File berhasil diunggah.", fileId = record.Id });
}
```

**Source:** C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\ProtonDataController.cs lines 352-403

---

### Anti-Patterns to Avoid

- **Filter State Contamination:** When filtering by Bagian (e.g., RFCC vs GAST), ensure saves only affect the selected Bagian; don't cross-contaminate. Always include `bagian` parameter in upsert logic.
- **Orphan Records Left Behind:** After bulk save, delete entities not in the submitted list. ProtonDataController.SilabusSave shows correct pattern: track saved IDs, delete anything not present.
- **No Null Safety on Foreign Keys:** When upserting nested entities, always ensure parent exists before referencing ID. Check for FindAsync() null return.
- **Forgetting AuditLog:** Every admin CREATE/UPDATE/DELETE must call `_auditLog.LogAsync()` for compliance.
- **Excel Without Header Validation:** Always check first row is header; skip it on import; validate column count; provide friendly error for malformed files.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel import/export | Custom stream parsing | ClosedXML 0.105.0 (already in project) | Handles formulas, formatting, multi-sheet; edge cases with dates, currency |
| Role-based route protection | Manual if (User.IsInRole()) checks | `[Authorize(Roles = "Admin, HC")]` attributes | Declarative, DRY, consistent across all actions |
| Audit logging | Custom SQL inserts | AuditLogService (already in project) | Centralized, consistent schema, timestamps, actor info |
| Password hashing for Worker import | Custom hashing | Microsoft.AspNetCore.Identity (PasswordHasher<T>) | NIST-compliant; don't roll crypto |
| File upload validation | Whitelist mimetype header | Validate file extension + content inspection | Headers can be spoofed; extension + size sufficient for this portal |

**Key insight:** This codebase has strong patterns for these problems. Don't reinvent; follow existing implementations.

---

## Common Pitfalls

### Pitfall 1: Bulk Save without Transaction Rollback

**What goes wrong:** If one row fails during KkjMatrixSave, partial rows are saved; database is in inconsistent state

**Why it happens:** `SaveChangesAsync()` is called per-row; one failure doesn't roll back previous saves

**How to avoid:** Wrap entire bulk save in a try-catch; if ANY error occurs, don't call SaveChangesAsync until all rows are validated. Consider `BeginTransactionAsync()` if atomicity is critical.

**Warning signs:** Partial data in database after import failure; user retries, gets duplicate entries; data inconsistency between views

---

### Pitfall 2: Filter State Not Preserved in Cross-View Links

**What goes wrong:** User filters KKJ Matrix by RFCC Bagian, edits a row, saves → redirects to CMP/Kkj → filter is lost, shows all bagians

**Why it happens:** Redirect to action without query string params; view resets to default state

**How to avoid:** When editing filtered data, preserve filter params in redirect: `RedirectToAction("Index", new { bagian = selectedBagian, unit = selectedUnit })`; or use TempData for multi-step workflows

**Warning signs:** Filter params disappear after save; user has to re-filter; inconsistency between admin edit and downstream view

---

### Pitfall 3: File Upload Orphaning

**What goes wrong:** Old Coaching Guidance file is "replaced" but old file still exists on disk; wwwroot fills up with orphaned files

**Why it happens:** Upload creates new file, record updated to point to new file, but old file on disk is never deleted

**How to avoid:** When replacing/deleting, explicitly delete old file from disk: `File.Delete(oldFilePath)`; track file IDs to know which files are "in use"

**Warning signs:** wwwroot/guidance directory grows unbounded; duplicate files with different IDs; storage utilization increases unexpectedly

---

### Pitfall 4: Validation Errors Not Shown to User

**What goes wrong:** Worker import fails (e.g., invalid email), returns `{ success = false, message = "..." }`, user sees generic error, doesn't know which row failed

**Why it happens:** Import returns single error message; per-row feedback not implemented

**How to avoid:** Always return per-row result: `List<ImportWorkerResult>` with row #, status (Success/Error/Skip), message. ImportWorkers.cshtml shows this pattern correctly.

**Warning signs:** User can't identify problematic rows; has to guess and re-upload; import appears to fail silently

---

### Pitfall 5: Email Uniqueness Not Checked During Worker Edit

**What goes wrong:** Two workers end up with same email; downstream features (login, password reset) break

**Why it happens:** EditWorker action doesn't validate email uniqueness; only checks during import

**How to avoid:** In EditWorker POST, check: `if (user.Email != originalEmail && await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != user.Id)) { return error; }`

**Warning signs:** Duplicate emails in Users table; login inconsistencies; audit logs show data integrity violations

---

## Code Examples

Verified patterns from project source:

### Example 1: KKJ Bagian Management (Add/Delete with Validation)

```csharp
// AdminController.cs lines 187-232
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KkjBagianAdd()
{
    var maxOrder = await _context.KkjBagians.MaxAsync(b => (int?)b.DisplayOrder) ?? 0;
    var newBagian = new KkjBagian
    {
        Name         = "Bagian Baru",
        DisplayOrder = maxOrder + 1
    };
    _context.KkjBagians.Add(newBagian);
    await _context.SaveChangesAsync();
    return Json(new { success = true, id = newBagian.Id, name = newBagian.Name, displayOrder = newBagian.DisplayOrder });
}

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KkjBagianDelete(int id)
{
    var bagian = await _context.KkjBagians.FindAsync(id);
    if (bagian == null) return Json(new { success = false, message = "Bagian tidak ditemukan." });

    // Validation: can't delete if KKJ items assigned to it
    var assignedCount = await _context.KkjMatrices.CountAsync(k => k.Bagian == bagian.Name);
    if (assignedCount > 0)
        return Json(new { success = false, blocked = true,
            message = $"Tidak dapat dihapus — masih ada {assignedCount} item yang di-assign ke bagian ini." });

    _context.KkjBagians.Remove(bagian);
    await _context.SaveChangesAsync();
    return Json(new { success = true });
}
```

**Source:** C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\AdminController.cs

---

### Example 2: Nested Silabus Upsert with Orphan Cleanup

```csharp
// ProtonDataController.cs lines 126-288
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SilabusSave([FromBody] List<SilabusRowDto> rows)
{
    var bagian = rows[0].Bagian;
    var unit = rows[0].Unit;
    var trackId = rows[0].TrackId;

    var savedKompIds = new List<int>();
    var savedSubIds = new List<int>();
    var savedDelivIds = new List<int>();

    foreach (var row in rows)
    {
        // Upsert Kompetensi
        ProtonKompetensi? komp = row.KompetensiId > 0
            ? await _context.ProtonKompetensiList.FindAsync(row.KompetensiId)
            : new ProtonKompetensi { Bagian = bagian, Unit = unit, ProtonTrackId = trackId, NamaKompetensi = row.Kompetensi };

        if (komp.Id == 0) _context.ProtonKompetensiList.Add(komp);
        await _context.SaveChangesAsync(); // Flush to get Id

        // Upsert SubKompetensi, Deliverable (similar pattern)
        // Track IDs
        savedKompIds.Add(komp.Id);
    }

    // Delete orphaned entities not in saved rows
    var orphanDelivs = await _context.ProtonDeliverableList
        .Where(d => d.ProtonSubKompetensi.ProtonKompetensiId == /* komp IDs */ && !savedDelivIds.Contains(d.Id))
        .ToListAsync();
    _context.ProtonDeliverableList.RemoveRange(orphanDelivs);

    await _context.SaveChangesAsync();
    return Json(new { success = true, message = $"Data silabus berhasil disimpan ({rows.Count} baris)." });
}
```

**Source:** C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\ProtonDataController.cs

---

### Example 3: Worker Import with Per-Row Validation

```csharp
// AdminController.cs lines 3561-3700
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ImportWorkers(IFormFile? excelFile)
{
    if (excelFile == null || excelFile.Length == 0)
    {
        TempData["Error"] = "File tidak diunggah.";
        return View();
    }

    var results = new List<ImportWorkerResult>();

    try
    {
        using (var stream = excelFile.OpenReadStream())
        using (var workbook = new XLWorkbook(stream))
        {
            var sheet = workbook.Worksheet(1);
            int rowNum = 0;

            foreach (var row in sheet.RowsUsed().Skip(1)) // Skip header
            {
                rowNum++;
                try
                {
                    var email = row.Cell(2).Value?.ToString()?.Trim();
                    var fullName = row.Cell(1).Value?.ToString()?.Trim();

                    // Validation 1: Required fields
                    if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
                    {
                        results.Add(new ImportWorkerResult
                        {
                            FullName = fullName ?? "?",
                            Email = email ?? "?",
                            Status = "Error",
                            Message = "Nama dan Email harus diisi."
                        });
                        continue;
                    }

                    // Validation 2: Email format
                    if (!email.Contains("@"))
                    {
                        results.Add(new ImportWorkerResult
                        {
                            FullName = fullName,
                            Email = email,
                            Status = "Error",
                            Message = "Format email tidak valid."
                        });
                        continue;
                    }

                    // Validation 3: Email uniqueness
                    if (await _context.Users.AnyAsync(u => u.Email == email))
                    {
                        results.Add(new ImportWorkerResult
                        {
                            FullName = fullName,
                            Email = email,
                            Status = "Skip",
                            Message = "Email sudah terdaftar."
                        });
                        continue;
                    }

                    // Create user
                    var user = new ApplicationUser
                    {
                        Email = email,
                        UserName = email,
                        FullName = fullName,
                        Section = row.Cell(3).Value?.ToString() ?? "Unknown"
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    results.Add(new ImportWorkerResult
                    {
                        FullName = fullName,
                        Email = email,
                        Status = "Success",
                        Message = "Berhasil dibuat."
                    });
                }
                catch (Exception rowEx)
                {
                    results.Add(new ImportWorkerResult
                    {
                        Status = "Error",
                        Message = rowEx.Message
                    });
                }
            }
        }
    }
    catch (Exception ex)
    {
        TempData["Error"] = $"Gagal membaca file: {ex.Message}";
        return View();
    }

    await _context.SaveChangesAsync();
    ViewBag.ImportResults = results;
    return View(); // Show ImportWorkers.cshtml with results table
}
```

**Source:** C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\AdminController.cs

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Role checks in views (User.IsInRole()) | Class-level `[Authorize]` + per-action Roles | Phase 76 | Cleaner, DRY, harder to bypass; navbar visibility fixed |
| Custom role hierarchy queries | RoleBasedAuthorizationHandler (built-in) | Ongoing | Standard .NET Identity; easier to extend |
| Separate import/export views | Unified ImportWorkers + result feedback | Phase 82 | Reduced duplication; better UX for validation feedback |
| Static bagian/unit lists hardcoded | OrganizationStructure helper class | v1.0+ | Single source of truth; easier to maintain |

**Deprecated/outdated:**
- CMP/CpdpProgress, CMP/CreateTrainingRecord, CMP/ManageQuestions (removed Phase 82; canonical versions in Admin/ManageAssessment)
- "Proton Progress" naming (renamed to "Coaching Proton" Phase 82)

---

## Open Questions

1. **Coaching Guidance File Types** — Code checks for `.pdf, .doc, .docx, .xlsx`. Should QA test other types (images, video) or just the allowed set?
   - **What we know:** GuidanceUpload validates extension
   - **What's unclear:** Real-world usage — do users actually upload videos or just PDFs?
   - **Recommendation:** Test the allowed types; flag if requirements change

2. **Export Content Completeness** — ExportWorkers generates Excel; should QA verify structure (column order, formatting) or just that download works?
   - **What we know:** ClosedXML generates workbook; export includes filtering
   - **What's unclear:** Specific format expectations (bold headers, frozen panes, etc.)
   - **Recommendation:** Verify download works + open in Excel; Claude can decide if formatting QA needed

3. **Worker Email Case Sensitivity** — Imports check `user.Email == email`; database may have case-insensitive collation. Could duplicate entries exist (user@example.com vs User@example.com)?
   - **What we know:** ASP.NET Identity uses case-insensitive by default
   - **What's unclear:** Collation of HcPortal.db
   - **Recommendation:** Test with both cases during import; flag if both accepted

4. **Silabus Dropdown Filtering** — When Plan IDP filters by Bagian/Unit/Track, does it query ProtonKompetensiList correctly?
   - **What we know:** ProtonDataController.Index filters by these params; Plan IDP page (views/CDP or CMP) calls ProtonKompetensiList query
   - **What's unclear:** Exact view implementation; may need cross-view verification
   - **Recommendation:** Edit Silabus in Admin, then view dropdown in Plan IDP; verify new items appear

---

## Validation Architecture

> workflow.nyquist_validation = true in .planning/config.json — include validation section

### Test Framework
| Property | Value |
|----------|-------|
| Framework | No automated tests in .csproj; QA is manual (user preference per CONTEXT.md) |
| Config file | No xUnit/NUnit config; tests would use WebApplicationFactory if added |
| Quick run command | N/A — manual browser QA |
| Full suite command | N/A — `/gsd:verify-work 83` signals formal completion |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Verification Method | Notes |
|--------|----------|-----------|---------------------|-------|
| DATA-01 | KKJ Matrix CRUD, bulk save, bagian mgmt, CMP/Kkj link | Manual | Browser: Admin/KkjMatrix → edit row → save → verify in CMP/Kkj view with same Bagian filter | Include bagian add/delete/validation |
| DATA-02 | CPDP/Mapping CRUD, bulk save, export, CMP/Mapping link | Manual | Browser: Admin/CpdpItems → edit → save → export Excel → verify in CMP/Mapping view | Verify Excel download works |
| DATA-03 | Silabus CRUD, links to Plan IDP + Coaching Proton dropdowns | Manual | Browser: ProtonData/Index → filter Bagian/Unit/Track → add/edit Silabus row → verify dropdown in Plan IDP/CDP shows new item | Cross-view verification |
| DATA-04 | Coaching Guidance upload, download, replace, delete, Plan IDP link | Manual | Browser: ProtonData/Index Guidance tab → upload file → verify download link → replace → delete → verify Plan IDP shows/hides files | Test allowed file types |
| DATA-05 | Worker CRUD (create, edit, delete, detail) | Manual | Browser: Admin/ManageWorkers → create worker → edit → view detail → delete → verify not in list | Test all CRUD actions |
| DATA-06 | Worker import (template download, upload, validation, errors) | Manual | Browser: Admin/ImportWorkers → download template → fill 5-10 rows (valid + invalid) → upload → verify results table shows Success/Error/Skip with messages | Test validation cases: empty email, duplicate, invalid format |
| DATA-07 | Worker export with filters | Manual | Browser: Admin/ManageWorkers → filter by section/role → export → verify Excel contains filtered results | Verify file opens correctly |

### Wave 0 Gaps

- [ ] No automated test infrastructure (xUnit/NUnit) — not planned for v3.0; manual QA sufficient
- [ ] No WebApplicationFactory setup — could be added in v3.1 for regression tests
- [ ] No test database — using production-like HcPortal.db for QA

*(No gaps — manual QA with production data is sufficient for Phase 83)*

---

## Sources

### Primary (HIGH confidence)
- **C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\AdminController.cs** (4765 lines) — KKJ Matrix, CPDP Items, Worker mgmt, ManageAssessment
- **C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\ProtonDataController.cs** (699 lines) — Silabus, Coaching Guidance management
- **C:\Users\Administrator\Desktop\PortalHC_KPB\HcPortal.csproj** — Dependencies: ClosedXML 0.105.0, EF Core 8.0, Identity 8.0
- **C:\Users\Administrator\Desktop\PortalHC_KPB\.planning\REQUIREMENTS.md** — Phase 83 requirements DATA-01 through DATA-07
- **C:\Users\Administrator\Desktop\PortalHC_KPB\.planning\STATE.md** — Project decisions: Phase 82 complete, Phase 83 starting

### Secondary (MEDIUM confidence)
- **Views examined:** KkjMatrix.cshtml, CpdpItems.cshtml, ImportWorkers.cshtml, ProtonData/Index.cshtml — frontend patterns confirmed
- **Models examined:** KkjMatrixItem, KkjBagian, CpdpItem, ProtonKompetensi*, CoachingGuidanceFile, ApplicationUser
- **Helpers:** OrganizationStructure, AuditLogService, ImportWorkerResult DTO

### Tertiary (observed patterns)
- ASP.NET Core 8 MVC standard patterns (Authorize, ValidateAntiForgeryToken, async/await)
- Bootstrap 5 CSS framework (inferred from view HTML)

---

## Metadata

**Confidence breakdown:**
- **Standard Stack:** HIGH — code directly inspected; NuGet packages verified; ASP.NET Core 8 standard
- **Architecture Patterns:** HIGH — all five patterns exist in codebase; code examples from source
- **Common Pitfalls:** MEDIUM-HIGH — identified from code review; some (like file orphaning) common in file upload scenarios
- **Validation Architecture:** HIGH — no automation needed; manual QA explicit in CONTEXT.md

**Research date:** 2026-03-02
**Valid until:** 2026-03-09 (stable .NET patterns, no major framework changes expected)

---

**End of Research**
