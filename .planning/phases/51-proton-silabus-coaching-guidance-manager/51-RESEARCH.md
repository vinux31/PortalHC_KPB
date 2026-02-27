# Phase 51: Proton Silabus & Coaching Guidance Manager - Research

**Researched:** 2026-02-27
**Domain:** ASP.NET Core MVC — EF Core migration, CRUD UI with rowspan table, file upload management, Bootstrap tabs
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Phase repurposing:**
- Phase 51 originally "Proton Track Assignment Manager" — absorbed into Phase 50 (Coach-Coachee Mapping)
- Repurposed to: Proton Silabus & Coaching Guidance Manager
- Roadmap needs rename: "Proton Silabus & Coaching Guidance Manager"
- OPER-02 requirement mapping needs updating

**Tab Silabus — Data structure:**
- Extend existing ProtonKompetensi model with `Bagian` (string) and `Unit` (string) fields
- Hierarchy: Bagian > Unit > Track (ProtonTrackId) > Kompetensi > SubKompetensi > Deliverable
- Each Unit can have different silabus content for the same track
- Multiple Kompetensi sets allowed per Bagian+Unit+Track combination
- Filter cascade on page: Bagian dropdown → Unit dropdown (filtered by Bagian) → Track dropdown
- No, Kompetensi, SubKompetensi, Deliverable are all string/text columns — No is manual input (flexible: "1", "1.1", "2a")

**Tab Silabus — Display:**
- Flat table: one row per deliverable (all columns: No, Kompetensi, SubKompetensi, Deliverable)
- Merge/rowspan for Kompetensi and SubKompetensi columns when same value spans multiple deliverables
- In edit mode: rowspan expands — all rows shown individually for easier editing
- View mode shows merged cells, edit mode shows expanded rows

**Tab Silabus — CRUD:**
- Inline editing: click cell to edit directly in table
- Inline add: "+" button per row to insert new row (not just at bottom — can insert anywhere)
- Inline delete: delete button per row with modal confirmation
- Save All button for batch save (changes held in memory until Save All clicked)
- No per-row highlight for changed rows

**Tab Coaching Guidance — Files:**
- New database entity: `CoachingGuidanceFile` (Id, Bagian, Unit, ProtonTrackId, FileName, FilePath, FileSize, UploadedAt, UploadedById)
- Filter: Bagian > Unit > Track (independent filter state from Silabus tab)
- Table columns: Nama File, Unit, Ukuran, Tanggal Upload, Actions (Download/Delete)
- Allowed file types: PDF, Word (.doc/.docx), Excel (.xls/.xlsx), PowerPoint (.ppt/.pptx)
- Max file size: 10 MB per file
- Unlimited files per Unit+Track combination
- Upload: standard "Choose File" button, one file at a time
- Replace in-place: edit existing file record, upload replacement file — record stays same
- Storage: server local (wwwroot/uploads/guidance/) — note for future: connect to company server
- Delete: modal confirmation before deletion

**Page layout & navigation:**
- URL: /Admin/ProtonData
- Admin/Index card: "Silabus & Coaching Guidance" in Section A (Master Data), at end of section
- Tab style: Bootstrap nav-tabs (consistent with app — same as Dashboard, Assessment, etc.)
- Two tabs: Silabus | Coaching Guidance
- Each tab has its own independent filter state (Bagian > Unit > Track)
- Empty state: message "Belum ada data silabus untuk [Unit] - [Track]" + Tambah button

**ProtonCatalog replacement:**
- Delete ProtonCatalog card from Admin/Index
- ProtonCatalogController: Claude's discretion on delete vs redirect
- All ProtonCatalog functionality replaced by Silabus tab in new page

**Data migration:**
- Add `Bagian` (string) and `Unit` (string) columns to ProtonKompetensi via EF Core migration
- Create new `CoachingGuidanceFile` table
- Migration cleans up old data: delete all ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, ProtonDeliverableProgress records
- ProtonTrack records (6 seeded tracks) kept — still valid
- ProtonTrackAssignment records: Claude's discretion on keep or clean

**Access control:**
- Admin and HC only (RoleLevel <= 2) — same as current ProtonCatalogController
- Both Admin and HC have full CRUD (no difference in permissions)
- Delete actions require modal confirmation

**Audit & logging:**
- AuditLogService logs every action: create, edit, delete in both tabs (Silabus and Coaching Guidance)

### Claude's Discretion
- ProtonCatalogController: delete entirely vs redirect to new page
- ProtonTrackAssignment records during migration: keep or clean
- Exact modal confirmation design
- Inline editing UX details (contenteditable vs input fields)
- File naming convention for uploaded guidance files (sanitization)
- Table pagination (if needed for large datasets)

### Deferred Ideas (OUT OF SCOPE)
- PlanIdp page update: coachee views silabus and coaching guidance for their assigned track (read-only download) — add as new phase at end of roadmap
- ProtonProgress page update: progress data sourced from new Bagian+Unit-scoped silabus — add as new phase at end of roadmap
- PlanIdp needs redevelopment (not final yet) — future phase
</user_constraints>

---

## Summary

Phase 51 creates a new `/Admin/ProtonData` page with two Bootstrap nav-tabs replacing the existing ProtonCatalog page. Tab Silabus manages the Kompetensi/SubKompetensi/Deliverable data (now scoped by Bagian+Unit+Track) with a flat-table CRUD pattern using rowspan merging in view mode and expanded rows in edit mode. Tab Coaching Guidance manages file uploads for learning materials (PDF, Word, Excel, PPT) stored at `wwwroot/uploads/guidance/`.

The codebase already has well-established patterns for everything this phase needs: EF Core migrations with column additions and data deletion, `IFormFile` file upload to `wwwroot/uploads/` via `IWebHostEnvironment`, AuditLogService injection, Bootstrap nav-tabs, OrganizationStructure static class for Bagian→Unit cascade, and ProtonTrack as the track selection source. The AdminController uses class-level `[Authorize(Roles = "Admin")]` — the new ProtonData page needs HC access too, so it requires either a new controller or runtime `user.RoleLevel <= 2` check similar to the existing ProtonCatalogController.

The largest technical challenge is the flat-table rowspan logic for the Silabus tab: view mode must compute row spans for duplicate Kompetensi/SubKompetensi values while edit mode shows every row individually. JavaScript state management for the batch-save pattern (hold changes in memory, send all on Save All) follows the same pattern as KkjMatrix and CpdpItems. The Coaching Guidance tab is simpler — it follows the same file upload pattern already used for evidence uploads and training certificates.

**Primary recommendation:** Create a new `ProtonDataController` (not in AdminController) with `[Authorize(Roles = "Admin,HC")]` to allow both roles, following ProtonCatalogController's RoleLevel check pattern. Build the Silabus tab JS state as a flat array of row objects (matching the flat table design), compute rowspan on render.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | (project version) | Controller + Razor views | Project standard |
| Entity Framework Core | (project version) | ORM, migrations | Project standard |
| Bootstrap 5 | (project CDN) | nav-tabs, modals, table | Project standard — all existing pages use it |
| Bootstrap Icons | (project CDN) | Icon set | Project standard |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| AuditLogService | internal | Audit logging | Every create/edit/delete action |
| OrganizationStructure | internal static | Bagian→Unit cascade dropdown | Tab Silabus and Tab Coaching Guidance filters |
| IWebHostEnvironment | ASP.NET built-in | Resolve wwwroot path for file upload | Tab Coaching Guidance upload/delete |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| New ProtonDataController | Extend AdminController | AdminController is Admin-only (`[Authorize(Roles = "Admin")]`), HC must also access — new controller is cleaner |
| Local wwwroot storage | Cloud/network storage | Context locks local storage; future migration noted but out of scope |
| Rowspan table | Full JS grid (AG Grid etc.) | No external grid libraries in this project; consistent with existing table patterns |

**Installation:** No new packages needed. All dependencies already present.

---

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
└── ProtonDataController.cs   # NEW — Admin+HC, ProtonData actions

Models/
└── ProtonModels.cs           # ADD CoachingGuidanceFile class; extend ProtonKompetensi

Data/
└── ApplicationDbContext.cs   # ADD DbSet<CoachingGuidanceFile>; configure entity

Migrations/
└── [timestamp]_AddProtonSilabusAndGuidance.cs  # NEW — adds Bagian/Unit to ProtonKompetensi, creates CoachingGuidanceFile, cleans old data

Views/Admin/
└── ProtonData.cshtml         # NEW — two Bootstrap nav-tabs (Silabus + Coaching Guidance)

wwwroot/uploads/guidance/     # NEW directory — coaching guidance files stored here
```

### Pattern 1: Controller Authorization for Admin+HC

The existing AdminController is `[Authorize(Roles = "Admin")]` — class-level, all actions inherit it. HC users cannot access it. ProtonCatalogController uses `[Authorize]` + runtime check `if (user.RoleLevel > 2) return Forbid()`.

The new ProtonDataController should use:
```csharp
[Authorize(Roles = "Admin,HC")]
public class ProtonDataController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLog;
    private readonly IWebHostEnvironment _env;  // needed for file upload path

    public ProtonDataController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        AuditLogService auditLog,
        IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _auditLog = auditLog;
        _env = env;
    }
}
```

This is cleaner than runtime `RoleLevel` checks and consistent with Identity role-based auth.

### Pattern 2: EF Core Migration — Add Columns + Delete Data

Based on existing migration patterns (e.g. `AddKkjBagianAndBagianField`, `ClearUserPackageAssignments`):

```csharp
// In migration Up():
migrationBuilder.AddColumn<string>(
    name: "Bagian",
    table: "ProtonKompetensiList",
    type: "nvarchar(max)",
    nullable: false,
    defaultValue: "");

migrationBuilder.AddColumn<string>(
    name: "Unit",
    table: "ProtonKompetensiList",
    type: "nvarchar(max)",
    nullable: false,
    defaultValue: "");

// Clean old ProtonKompetensi data (data no longer valid after structure change)
migrationBuilder.Sql("DELETE FROM ProtonDeliverableProgresses");
migrationBuilder.Sql("DELETE FROM ProtonDeliverableList");
migrationBuilder.Sql("DELETE FROM ProtonSubKompetensiList");
migrationBuilder.Sql("DELETE FROM ProtonKompetensiList");

// Create CoachingGuidanceFile table
migrationBuilder.CreateTable(
    name: "CoachingGuidanceFiles",
    columns: table => new {
        Id = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
        Bagian = table.Column<string>(type: "nvarchar(max)", nullable: false),
        Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
        ProtonTrackId = table.Column<int>(type: "int", nullable: false),
        FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
        FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
        FileSize = table.Column<long>(type: "bigint", nullable: false),
        UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
        UploadedById = table.Column<string>(type: "nvarchar(max)", nullable: false)
    },
    constraints: table => {
        table.PrimaryKey("PK_CoachingGuidanceFiles", x => x.Id);
    });
```

**IMPORTANT:** The `EF migrations require --configuration Release` while the app is running (Debug exe is locked) — documented in Phase 46-01 decisions.

### Pattern 3: Model Extensions

Extend `ProtonKompetensi` in `ProtonModels.cs`:
```csharp
public class ProtonKompetensi
{
    public int Id { get; set; }
    public string Bagian { get; set; } = "";   // NEW
    public string Unit { get; set; } = "";      // NEW
    public string NamaKompetensi { get; set; } = "";
    public int Urutan { get; set; }
    public int ProtonTrackId { get; set; }
    public ProtonTrack? ProtonTrack { get; set; }
    public ICollection<ProtonSubKompetensi> SubKompetensiList { get; set; } = new List<ProtonSubKompetensi>();
}
```

New model for `CoachingGuidanceFile`:
```csharp
public class CoachingGuidanceFile
{
    public int Id { get; set; }
    public string Bagian { get; set; } = "";
    public string Unit { get; set; } = "";
    public int ProtonTrackId { get; set; }
    public ProtonTrack? ProtonTrack { get; set; }
    public string FileName { get; set; } = "";       // Original display name
    public string FilePath { get; set; } = "";       // Web-relative path e.g. /uploads/guidance/...
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedById { get; set; } = "";
}
```

### Pattern 4: Flat Table with Rowspan — Silabus Tab

The flat table approach: the controller returns a flat list of deliverable rows, each containing Kompetensi, SubKompetensi, Deliverable, No. JavaScript renders rowspan merges in view mode, expands in edit mode.

View mode rowspan algorithm (JavaScript):
```javascript
// Group consecutive rows with same Kompetensi value → rowspan
// Group consecutive rows (within same Kompetensi) with same SubKompetensi → rowspan
// "consecutive" means rows must be adjacent; resets on Kompetensi/SubKompetensi change

function renderViewTable(rows) {
    // Compute kompetensi spans: iterate, count consecutive matching values
    let result = [];
    let i = 0;
    while (i < rows.length) {
        let kompSpan = 1;
        while (i + kompSpan < rows.length && rows[i + kompSpan].Kompetensi === rows[i].Kompetensi) kompSpan++;
        // Compute subkompetensi spans within this kompetensi group
        let j = 0;
        while (j < kompSpan) {
            let subSpan = 1;
            while (j + subSpan < kompSpan && rows[i+j+subSpan].SubKompetensi === rows[i+j].SubKompetensi) subSpan++;
            for (let k = 0; k < subSpan; k++) {
                result.push({
                    row: rows[i+j+k],
                    showKomp: j === 0 && k === 0,
                    kompSpan: j === 0 && k === 0 ? kompSpan : 0,
                    showSub: k === 0,
                    subSpan: k === 0 ? subSpan : 0
                });
            }
            j += subSpan;
        }
        i += kompSpan;
    }
    return result;
}
```

Edit mode: render every row individually (no rowspan), each cell shows input field. This matches the "spreadsheet feel" decision.

### Pattern 5: File Upload — Coaching Guidance Tab

Follows the established pattern from CDPController.UploadEvidence and CMPController (certificate upload):

```csharp
// POST /ProtonData/GuidanceUpload
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GuidanceUpload(string bagian, string unit, int trackId, IFormFile? file)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    if (file == null || file.Length == 0)
        return Json(new { success = false, error = "File tidak boleh kosong." });

    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(ext))
        return Json(new { success = false, error = "Tipe file tidak diperbolehkan." });

    if (file.Length > 10 * 1024 * 1024)
        return Json(new { success = false, error = "Ukuran file maksimal 10MB." });

    var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "guidance");
    Directory.CreateDirectory(uploadDir);

    var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(file.FileName)}";
    var filePath = Path.Combine(uploadDir, safeFileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var record = new CoachingGuidanceFile
    {
        Bagian = bagian,
        Unit = unit,
        ProtonTrackId = trackId,
        FileName = file.FileName,
        FilePath = $"/uploads/guidance/{safeFileName}",
        FileSize = file.Length,
        UploadedById = user.Id
    };
    _context.CoachingGuidanceFiles.Add(record);
    await _context.SaveChangesAsync();

    await _auditLog.LogAsync(user.Id, user.FullName, "Upload",
        $"Uploaded guidance file '{file.FileName}' for {bagian}/{unit}/Track {trackId}",
        targetId: record.Id, targetType: "CoachingGuidanceFile");

    return Json(new { success = true });
}
```

For file download, use `PhysicalFile()`:
```csharp
// GET /ProtonData/GuidanceDownload?id=5
public async Task<IActionResult> GuidanceDownload(int id)
{
    var record = await _context.CoachingGuidanceFiles.FindAsync(id);
    if (record == null) return NotFound();

    var physicalPath = Path.Combine(_env.WebRootPath, record.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
    if (!System.IO.File.Exists(physicalPath)) return NotFound();

    var contentType = GetContentType(Path.GetExtension(record.FilePath));
    return PhysicalFile(physicalPath, contentType, record.FileName);
}
```

### Pattern 6: Bagian→Unit Cascade Filter

`OrganizationStructure.SectionUnits` is the single source of truth. Use it in JavaScript for client-side cascade:

```javascript
const orgStructure = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(
    HcPortal.Models.OrganizationStructure.SectionUnits));

document.getElementById('bagianSelect').addEventListener('change', function() {
    const bagian = this.value;
    const unitSelect = document.getElementById('unitSelect');
    unitSelect.innerHTML = '<option value="">— Pilih Unit —</option>';
    if (bagian && orgStructure[bagian]) {
        orgStructure[bagian].forEach(unit => {
            const opt = document.createElement('option');
            opt.value = unit;
            opt.textContent = unit;
            unitSelect.appendChild(opt);
        });
    }
    unitSelect.disabled = !bagian;
});
```

Bagian values are: RFCC, DHT / HMU, NGP, GAST (from OrganizationStructure.cs).

### Pattern 7: Bootstrap Nav-Tabs

Bootstrap 5 nav-tabs — consistent with existing Dashboard, Assessment pages in this project:

```html
<ul class="nav nav-tabs mb-3" id="protonDataTabs">
    <li class="nav-item">
        <a class="nav-link active" data-bs-toggle="tab" href="#silabus">Silabus</a>
    </li>
    <li class="nav-item">
        <a class="nav-link" data-bs-toggle="tab" href="#coachingGuidance">Coaching Guidance</a>
    </li>
</ul>
<div class="tab-content">
    <div class="tab-pane fade show active" id="silabus">...</div>
    <div class="tab-pane fade" id="coachingGuidance">...</div>
</div>
```

### Pattern 8: Batch Save (Save All)

Follows KkjMatrix / CpdpItems pattern — hold rows in JS array, batch POST on Save All:

```javascript
// POST JSON array to endpoint
async function saveAll() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const resp = await fetch('/ProtonData/SilabusSave', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify(silabusRows)  // flat array of row objects
    });
    const data = await resp.json();
    if (data.success) { /* reload table */ }
}
```

Controller receives `[FromBody] List<SilabusRowDto>` (or use existing model shape).

### Pattern 9: AuditLog for all actions

AuditLogService signature:
```csharp
await _auditLog.LogAsync(
    actorUserId: actor.Id,
    actorName: actor.FullName,
    actionType: "Create" | "Update" | "Delete" | "Upload",
    description: "descriptive string",
    targetId: item.Id,        // optional int
    targetType: "ProtonKompetensi" | "CoachingGuidanceFile"
);
```

### Anti-Patterns to Avoid

- **Putting ProtonData actions in AdminController:** AdminController uses `[Authorize(Roles = "Admin")]` class-level — HC users would be blocked. Create a separate ProtonDataController with `[Authorize(Roles = "Admin,HC")]`.
- **Deleting ProtonCatalog files without redirect safety:** Leave ProtonCatalogController in place (or add redirect) so any bookmarked URLs do not 404. Recommendation: redirect all ProtonCatalog actions to the new ProtonData page.
- **Running EF migration while Debug exe is running:** Debug process locks the DLL. Always use `--configuration Release` for migrations.
- **Storing ProtonKompetensi by nested FK hierarchy (Kompetensi→SubKompetensi→Deliverable) and adding Bagian/Unit to top level only:** The existing structure already works this way. Bagian+Unit on ProtonKompetensi is the right scope point since Kompetensi defines the scope.
- **Using rowspan in server-side Razor without counting groups:** Rowspan must be computed before rendering. Either compute in the controller (group/count) or in JavaScript. JavaScript approach (like KkjMatrix) is simpler given the batch-save pattern already uses JS state.
- **Sending IFormFile in JSON body:** File uploads must use `multipart/form-data` (form POST), not JSON. Use a separate upload form action, not the batch-save JSON endpoint.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| File type detection | Custom MIME lookup | `Path.GetExtension()` + extension whitelist | Established pattern in project (CDPController, CMPController) |
| Bagian→Unit structure | Database table for org units | `OrganizationStructure.SectionUnits` static class | Already exists in project |
| Role check for Admin+HC | Custom permission middleware | `[Authorize(Roles = "Admin,HC")]` | Identity framework handles it |
| File path traversal prevention | Custom sanitization | `Path.GetFileName(file.FileName)` | Strips any path components; existing pattern in project |
| File download with correct name | Custom response construction | `PhysicalFile(path, contentType, originalName)` | ASP.NET built-in |
| Audit logging | Custom log table | `AuditLogService.LogAsync()` | Already injected in AdminController |

**Key insight:** Every infrastructure need in this phase has a working pattern already in the codebase. Do not invent new mechanisms.

---

## Common Pitfalls

### Pitfall 1: Rowspan Breaks on Non-Consecutive Identical Values
**What goes wrong:** If the Silabus table has rows where the same Kompetensi value appears but is not consecutive (e.g. interleaved with a different Kompetensi), the rowspan algorithm will incorrectly merge non-adjacent cells.
**Why it happens:** Rowspan only works for visually adjacent rows. If sort order doesn't guarantee Kompetensi groups are contiguous, the render will be wrong.
**How to avoid:** Always query ProtonKompetensi rows ordered by Kompetensi, then SubKompetensi within the same Kompetensi group, then Deliverable. The Urutan field should drive order but Kompetensi+SubKompetensi text grouping must be respected in JS.
**Warning signs:** Rowspan cells appear with wrong span count or overlap adjacent rows.

### Pitfall 2: EF Migration Locks with Debug Process Running
**What goes wrong:** `dotnet ef migrations add` or `dotnet ef database update` fails with MSB3021/MSB3027 file-lock errors.
**Why it happens:** The running Debug IIS Express process holds the DLL lock.
**How to avoid:** Use `--configuration Release`: `dotnet ef migrations add MigrationName --configuration Release` and `dotnet ef database update --configuration Release`.

### Pitfall 3: AdminController Authorization Excludes HC
**What goes wrong:** If ProtonData actions are added to AdminController, HC users get 403 Forbidden.
**Why it happens:** `[Authorize(Roles = "Admin")]` at class level blocks all non-Admin roles.
**How to avoid:** Create `ProtonDataController` with `[Authorize(Roles = "Admin,HC")]`.

### Pitfall 4: File Upload Size Limit — ASP.NET Default
**What goes wrong:** Files larger than ASP.NET's default multipart body limit (~28 MB in some configs, 30MB Kestrel default) get rejected with 400 before the controller sees them. But for 10MB limit enforced by the app, this is fine — no config change needed.
**Why it happens:** Kestrel's default MaxRequestBodySize is 30MB, well above the 10MB app limit.
**How to avoid:** No config change needed for 10MB files. If size limit ever increases above 30MB, `[RequestSizeLimit]` attribute would be needed.

### Pitfall 5: Concurrent Filter State Between Tabs
**What goes wrong:** The Silabus and Coaching Guidance tabs share the same page URL. If filter state is stored in query parameters, switching tabs resets the other tab's filter.
**Why it happens:** Page reload clears JS state.
**How to avoid:** Both tabs load data via AJAX (not page reload on filter change). Filter state is JavaScript variables per tab, not URL parameters. Silabus filter loads from server on initial GET; Coaching Guidance filter also loads via AJAX when tab becomes active.

### Pitfall 6: Deleting Files from wwwroot Without Cleaning DB Record
**What goes wrong:** Delete from DB but file remains on disk (orphaned). Or delete file from disk but DB record remains (broken link).
**Why it happens:** Doing them in separate steps with no transaction.
**How to avoid:** Delete the DB record first (if delete fails, file stays safely). Then delete the physical file. If file delete fails, log the error but don't rollback the DB record (file can be cleaned manually). This is the same approach used throughout the project.

### Pitfall 7: Razor auto-encoding in data attributes
**What goes wrong:** Using `Html.AttributeEncode` causes compile errors on typed Razor views (`List<T>` models).
**Why it happens:** `Html.AttributeEncode` is not available on `IHtmlHelper<List<T>>` typed views.
**How to avoid:** Use Razor auto-encoding: `data-value="@item.Property"` — Razor automatically HTML-encodes attributes. This is the established pattern since Phase 48-01.

---

## Code Examples

### GET Action — Silabus Data Load

```csharp
// Source: project pattern (AdminController.CpdpItems, AdminController.CoachCoacheeMapping)
// GET /ProtonData/Index
public async Task<IActionResult> Index(string? bagian, string? unit, int? trackId)
{
    var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
    ViewBag.AllTracks = tracks;
    ViewBag.Bagian = bagian;
    ViewBag.Unit = unit;
    ViewBag.TrackId = trackId;

    List<object> silabusRows = new();
    if (!string.IsNullOrEmpty(bagian) && !string.IsNullOrEmpty(unit) && trackId.HasValue)
    {
        // Flat rows: Kompetensi → SubKompetensi → Deliverable
        silabusRows = await _context.ProtonKompetensiList
            .Include(k => k.SubKompetensiList)
                .ThenInclude(s => s.Deliverables)
            .Where(k => k.Bagian == bagian && k.Unit == unit && k.ProtonTrackId == trackId.Value)
            .OrderBy(k => k.Urutan)
            .SelectMany(k => k.SubKompetensiList.OrderBy(s => s.Urutan)
                .SelectMany(s => s.Deliverables.OrderBy(d => d.Urutan)
                    .Select(d => new {
                        KompetensiId = k.Id,
                        Kompetensi = k.NamaKompetensi,
                        SubKompetensiId = s.Id,
                        SubKompetensi = s.NamaSubKompetensi,
                        DeliverableId = d.Id,
                        Deliverable = d.NamaDeliverable,
                        Urutan = d.Urutan
                    })))
            .Cast<object>()
            .ToListAsync();
    }

    ViewBag.SilabusRowsJson = System.Text.Json.JsonSerializer.Serialize(silabusRows,
        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });

    return View();
}
```

**NOTE:** The CONTEXT decision is that No, Kompetensi, SubKompetensi, Deliverable are ALL string columns. The flat-row model needs to be reconsidered. Since the existing ProtonKompetensi uses the Kompetensi/SubKompetensi/Deliverable nested hierarchy (3 tables), the "flat table display" with inline editing still operates on the nested DB structure but presents flattened. The "No" column is a manual text field stored... where? The ProtonKompetensi.NamaKompetensi, ProtonSubKompetensi.NamaSubKompetensi, ProtonDeliverable.NamaDeliverable are the three string columns. The "No" (e.g. "1", "1.1", "2a") — CONTEXT says "No is manual input" — this needs to be a separate `No` (string) column, one on each level entity OR on the deliverable row only. Since the display is flat (one row per deliverable, showing all four: No, Kompetensi, SubKompetensi, Deliverable), "No" is likely the Deliverable-level number. **This is an open question for the planner — see Open Questions.**

### Save All Endpoint

```csharp
// Source: project pattern (AdminController.KkjMatrixSave, AdminController.CpdpItemsSave)
// POST /ProtonData/SilabusSave
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SilabusSave([FromBody] List<SilabusRowDto> rows)
{
    if (rows == null || !rows.Any())
        return Json(new { success = false, message = "Tidak ada data." });

    // Upsert pattern: Id == 0 → add, Id > 0 → update
    foreach (var row in rows)
    {
        if (row.KompetensiId == 0)
        {
            // new kompetensi (+ new subkomp + new deliverable)
        }
        else
        {
            var k = await _context.ProtonKompetensiList.FindAsync(row.KompetensiId);
            if (k != null) k.NamaKompetensi = row.Kompetensi;
        }
        // similar for subkomp and deliverable
    }
    await _context.SaveChangesAsync();
    // AuditLog
    return Json(new { success = true });
}
```

### DbContext Registration for CoachingGuidanceFile

```csharp
// In ApplicationDbContext.cs
public DbSet<CoachingGuidanceFile> CoachingGuidanceFiles { get; set; }

// In OnModelCreating:
builder.Entity<CoachingGuidanceFile>(entity =>
{
    entity.HasOne(f => f.ProtonTrack)
        .WithMany()
        .HasForeignKey(f => f.ProtonTrackId)
        .OnDelete(DeleteBehavior.Restrict);
    entity.HasIndex(f => new { f.Bagian, f.Unit, f.ProtonTrackId });
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ProtonKompetensi scoped by ProtonTrackId only | ProtonKompetensi scoped by Bagian + Unit + ProtonTrackId | Phase 51 | Each Unit gets its own silabus per track |
| ProtonCatalog standalone page (/ProtonCatalog) | ProtonData page (/Admin/ProtonData) with two tabs | Phase 51 | Replaces ProtonCatalog; coaching guidance now managed here |
| No coaching guidance files | CoachingGuidanceFile entity + wwwroot/uploads/guidance/ | Phase 51 | HC can upload/manage learning materials per unit/track |

**Deprecated/outdated after this phase:**
- ProtonCatalog card on Admin/Index: removed
- ProtonCatalogController: redirect to /Admin/ProtonData or keep for URL safety

---

## Open Questions

1. **Where does "No" (the row number column) live in the data model?**
   - What we know: CONTEXT says "No, Kompetensi, SubKompetensi, Deliverable are all string/text columns — No is manual input (flexible: '1', '1.1', '2a')"
   - What's unclear: The existing ProtonKompetensi/SubKompetensi/Deliverable models have `Urutan` (int) but no `No` (string) field. The flat display shows a "No" column. Where should this string be stored — on the ProtonDeliverable model? Or on all three? For the flat-table view, a single `No` per deliverable-row makes most sense.
   - Recommendation: Add `No` (string, nullable) to `ProtonDeliverable`. This gives one No per leaf row, which matches the flat-table concept. Kompetensi and SubKompetensi grouping still uses the nested entity structure for DB storage; only Deliverable has the row-level No string. Alternatively, since the whole structure is being rebuilt, the planner might opt to flatten the model entirely (one table with Kompetensi, SubKompetensi, Deliverable, No as columns). Either approach works — planner to decide.

2. **ProtonTrackAssignment records during migration — keep or clean?**
   - What we know: CONTEXT marks this as Claude's Discretion.
   - What's unclear: ProtonTrackAssignments link CoacheeId to ProtonTrackId. The ProtonTracks (6 records) are kept. Assignments themselves are still valid references. However, if the silabus data for those tracks is being deleted, the assignments become "orphaned" in terms of silabus content.
   - Recommendation: Keep ProtonTrackAssignment records. They are valid FK references to ProtonTracks which are being kept. Deleting them would be destructive. A future phase can manage them.

3. **ProtonCatalogController — delete or redirect?**
   - What we know: CONTEXT marks this as Claude's Discretion.
   - Recommendation: Add a redirect from all ProtonCatalog actions to `/Admin/ProtonData` rather than deleting the controller. This preserves URL safety for any bookmarks or links. The controller file can remain with minimal redirect actions until a future cleanup phase.

4. **Inline editing UX — contenteditable vs input fields?**
   - What we know: CONTEXT marks this as Claude's Discretion.
   - Recommendation: Use `<input>` fields in edit mode rows (not contenteditable). This matches the project's existing KkjMatrix and CpdpItems edit pattern. Input fields are more predictable for reading values via JavaScript.

5. **Table pagination for Silabus tab?**
   - What we know: CONTEXT marks this as Claude's Discretion.
   - Recommendation: No server-side pagination. Filter by Bagian+Unit+Track first (narrowing the dataset significantly before any query). Client-side rendering of the filtered results should be fast enough. If a single Bagian+Unit+Track combination has hundreds of deliverables, the planner can add simple client-side pagination later.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (no automated test framework detected in project) |
| Config file | none |
| Quick run command | Manual smoke test — load /Admin/ProtonData, verify tabs render |
| Full suite command | UAT checklist (see Phase Gate) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| OPER-02 | Admin/HC can manage Proton silabus data through /Admin/ProtonData | manual smoke | Load /Admin/ProtonData — both tabs visible | N/A |
| OPER-02 | Silabus: filter cascade Bagian→Unit→Track loads data | manual | Select Bagian, Unit, Track → table populates | N/A |
| OPER-02 | Silabus: view mode shows rowspan merges | manual | Multiple rows with same Kompetensi → merged cell | N/A |
| OPER-02 | Silabus: edit mode expands rows individually | manual | Click Edit → all rows individual, no rowspan | N/A |
| OPER-02 | Silabus: inline add inserts row at any position | manual | Click "+" mid-table → row inserted after | N/A |
| OPER-02 | Silabus: inline delete with modal confirmation | manual | Click delete → modal appears → confirm → row removed | N/A |
| OPER-02 | Silabus: Save All persists batch changes | manual | Edit multiple rows → Save All → reload → changes present | N/A |
| OPER-02 | Coaching Guidance: upload file (PDF, Word, Excel, PPT) | manual | Upload file → appears in table | N/A |
| OPER-02 | Coaching Guidance: reject file >10MB | manual | Upload 11MB file → error message | N/A |
| OPER-02 | Coaching Guidance: download file | manual | Click download → file downloads with original name | N/A |
| OPER-02 | Coaching Guidance: delete with modal | manual | Click delete → modal → confirm → record removed | N/A |
| OPER-02 | HC user can access /Admin/ProtonData | manual | Login as HC → navigate to page → not 403 | N/A |
| OPER-02 | AuditLog records all actions | manual | Perform actions → AuditLog page shows entries | N/A |
| OPER-02 | ProtonCatalog card removed from Admin/Index | manual | Admin/Index → Section A → no ProtonCatalog card | N/A |
| OPER-02 | New "Silabus & Coaching Guidance" card in Section A | manual | Admin/Index → Section A → card with correct link | N/A |
| OPER-02 | EF migration runs clean | manual | dotnet ef database update → no errors | N/A |

### Wave 0 Gaps
- [ ] `wwwroot/uploads/guidance/.gitkeep` — directory must exist for file uploads (create with gitkeep, like existing certificates/)
- [ ] No automated test framework to install — all UAT is manual

*(All phase requirements covered by manual UAT checklist. No automated test framework exists in project.)*

---

## Sources

### Primary (HIGH confidence)
- Project codebase: `Controllers/AdminController.cs` — KkjMatrix/CpdpItems/CoachCoacheeMapping patterns (batch save, AuditLog, filter, tabs)
- Project codebase: `Controllers/CDPController.cs` — IFormFile upload to wwwroot/uploads/, extension validation, size check
- Project codebase: `Controllers/CMPController.cs` — certificate upload pattern
- Project codebase: `Controllers/ProtonCatalogController.cs` — RoleLevel <= 2 check, ProtonKompetensi queries with Include chains
- Project codebase: `Models/ProtonModels.cs` — current ProtonKompetensi/SubKompetensi/Deliverable/ProtonTrack structure
- Project codebase: `Models/OrganizationStructure.cs` — Bagian→Unit cascade dictionary
- Project codebase: `Models/ApplicationUser.cs` — RoleLevel field definitions
- Project codebase: `Models/UserRoles.cs` — role constants (Admin = "Admin", HC = "HC")
- Project codebase: `Data/ApplicationDbContext.cs` — DbSet registration, OnModelCreating relationship config patterns
- Project codebase: `Services/AuditLogService.cs` — LogAsync signature
- Project codebase: `Migrations/20260226104042_AddKkjBagianAndBagianField.cs` — AddColumn + CreateTable migration pattern
- Project codebase: `Views/Admin/CpdpItems.cshtml` — edit/read mode toggle, section filter, batch save UI
- Project codebase: `Views/Admin/CoachCoacheeMapping.cshtml` — Bootstrap tabs pattern, filter form, modal wiring
- Project decisions (STATE.md): `[Phase 47-01]` through `[Phase 50-02]` documented patterns

### Secondary (MEDIUM confidence)
- None needed — all patterns verified from project codebase directly

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries are existing project dependencies
- Architecture: HIGH — all patterns are established in project codebase with multiple prior phases
- Pitfalls: HIGH — directly observed from project decisions log and existing code

**Research date:** 2026-02-27
**Valid until:** 2026-03-28 (30 days — stable project)
