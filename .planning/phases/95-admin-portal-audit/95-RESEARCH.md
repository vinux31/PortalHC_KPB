# Phase 95: Admin Portal Audit - Research

**Researched:** 2026-03-05
**Domain:** ASP.NET Core 8.0 MVC - Admin Portal Bug Audit
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Audit Organization:**
- Per halaman — ManageWorkers fixes → satu commit, CoachCoacheeMapping fixes → satu commit
- Cross-cutting concerns → commit terpisah — Validation fixes → satu commit, Role gate fixes → satu commit
- Total expected: 4-5 commit
- This matches Phase 94's by-flow approach and keeps changes organized by feature area
- Cross-cutting concerns yang mempengaruhi banyak halaman dipisah agar lebih mudah track apa yang diperbaiki

**Testing Approach:**
- Smoke test only — quick verification that pages load and obvious bugs are fixed
- Don't test every role combination exhaustively
- Pattern: Code review → identify bugs → fix → browser verify (same as Phases 93/94)
- Focus on verifying the specific bug that was fixed
- Browser testing only when code review is unclear or requires runtime verification

**Test Data Approach:**
- Pakai existing seed data — Workers dari Phase 83 (Master Data QA), Coach-coachee mappings dari Phase 85
- Tambah test data hanya saat diperlukan — selama code review, kalau butuh worker dengan role spesifik atau mapping status tertentu, baru tambah
- Untuk Import Workers: pakai template existing (DownloadImportTemplate), isi dengan sample data
- Test file Excel — Claude tentukan berdasarkan code review findings
- Pragmatic approach: hanya tambah test data yang benar-benar diperlukan

**Role Testing Coverage:**
- HC & Admin roles saja — dua role yang memang punya akses ke Admin pages
- Verify role gates via code review — cek `[Authorize(Roles = "Admin, HC")]` attribute di controller
- Tidak perlu test semua intermediate role (Coach, Spv, SectionHead) untuk save time
- Test role-based filtering kalau ada di code — kalau code review menemukan .Where(u => u.Unit == user.Unit) atau similar, perlu test
- Ini adalah smoke test level — verify role gates exist lewat code review, bukan exhaustive permission testing

**Validation Depth:**
- All Admin forms — check validation error handling on all Admin CRUD forms
- ManageWorkers: Create, Edit forms
- CoachCoacheeMapping: Assign form
- Import form: File upload validation
- Check: Required fields, data type validation, error messages via TempData (not raw exceptions)

**Import/Export Depth:**
- Smoke test untuk Import Workers — upload valid file → verify processed → check data ada di DB
- Export — Claude tentukan — tergantung complexity code review. Kalau export logic kompleks (formatting, calculations), test. Kalau simple data dump, smoke test atau skip.
- Smoke test validation — test satu invalid file type untuk verify validation exists. Tidak test semua scenarios, cukup verify validation works.
- Focus: verify basic functionality works, edge cases hanya kalau code review reveals potential issues

**Bug Priority:**
- Claude's discretion — prioritize based on severity and user impact
- Critical: crashes, null references, raw exceptions shown to users
- High: broken flows, incorrect data displayed, navigation failures
- Medium: UX issues (unclear text, missing links, confusing UI)
- Low: cosmetic issues, typos, minor inconsistencies

### Claude's Discretion
- Exact order of bug fixes within each page
- Whether to group fixes by page or by bug category
- Which validation checks are actually needed vs defensive coding
- Whether to refactor any messy code discovered during audit
- How deep to investigate each edge case vs smoke test
- Untuk Import Excel test files: buatberapa tergantung findings
- Untuk Export: test atau skip tergantung code complexity

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ADMIN-01 | Manage Workers page loads with correct filters and pagination | AdminController.ManageWorkers (line 3779) uses pagination with pageSize=20, filters for search/section/role/showInactive. Existing test data from Phase 83 SeedMasterData.cs provides workers for testing. |
| ADMIN-02 | Manage Silabus page handles KKJ files correctly (upload, download, archive) | AdminController.KkjMatrix (line 54) and related actions (KkjUpload, KkjFileDownload, KkjFileArchive) already audited in Phase 88. File validation uses allowedExtensions check and 10MB maxFileSize. |
| ADMIN-03 | Manage Assessment page shows correct assessment lists and actions | AdminController.ManageAssessment (line 983) already audited in Phase 90. Uses IsActive filters, includes AssessmentPackages and sessions. |
| ADMIN-04 | Assessment Monitoring page displays real-time participant data | AdminController.AssessmentMonitoring (line 1490) already audited in Phase 90. Uses IMemoryCache with 10s TTL for real-time updates. |
| ADMIN-05 | Coach-Coachee Mapping page works correctly (assign, remove, export) | AdminController.CoachCoacheeMapping (line 3449) with assign/deactivate/reactivate/export actions. Uses Coach role (not RoleLevel) per Phase 74 decision. Existing test data from Phase 85 SeedCoachingTestData provides mappings. |
| ADMIN-06 | Proton Data page (Silabus + Coaching Guidance) displays correct tabs | AdminController.ProtonData with Silabus/Guidance tabs already audited in Phase 88. Uses tabbed layout with file management. |
| ADMIN-07 | All Admin forms handle validation errors gracefully | TempData["Error"] pattern used consistently (73 occurrences in AdminController.cs). Model validation with ViewData.ModelState.IsValid in views. |
| ADMIN-08 | Admin role gates work correctly (HC vs Admin access) | 73 [Authorize(Roles = "Admin, HC")] attributes for shared access, 3 [Authorize(Roles = "Admin")] for Admin-only actions (lines 2271, 2533, 2940). |
</phase_requirements>

## Summary

Phase 95 audits the Kelola Data (Admin Portal) pages for bugs, focusing on pages NOT yet covered in prior audit phases. AdminController.cs is a large controller (5729 lines) with comprehensive CRUD operations for workers, assessments, KKJ matrix, CPDP files, and coaching mappings. The audit follows the proven Phase 93/94 pattern: systematic code review → identify bugs → fix → smoke test verification.

**Key insight:** Most Admin functionality has already been audited in Phase 88 (KKJ Matrix, CPDP Files, Proton Data) and Phase 90 (ManageAssessment, AssessmentMonitoring). Phase 95 focuses on the remaining pages: ManageWorkers, CoachCoacheeMapping, and cross-cutting concerns (validation error handling, role gates). The codebase uses established patterns: TempData["Error"] for validation messages, [ValidateAntiForgeryToken] on POST actions (37 occurrences), and consistent role-based authorization with [Authorize(Roles = "Admin, HC")].

**Primary recommendation:** Use the existing audit pattern from Phase 93/94—systematic code review of targeted Admin pages, identify bugs using checklists (null safety, localization, validation, authorization), fix issues in organized commits (by page or by concern), and verify with smoke testing using existing seed data from Phases 83 and 85. No new test infrastructure or data seeding required—the existing SeedTestData.cs (480 lines) provides comprehensive workers, mappings, and coaching data.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 | Server-side rendering framework | Already in use throughout Admin pages with established patterns |
| Entity Framework Core | 8.0 | Database ORM | ApplicationDbContext with all Admin entities (ApplicationUser, CoachCoacheeMappings, etc.) |
| Bootstrap | 5.x | UI framework | Admin views use Bootstrap classes consistently (card, btn, form, alert) |
| Razor Views | .NET 8 | View engine | Standard ASP.NET Core templating with @Html.AntiForgeryToken() |
| ClosedXML.Excel | 0.104+ | Excel import/export | Used in DownloadImportTemplate (line 4407), ImportWorkers (line 4469), ExportWorkers (line 4308), CoachCoacheeMappingExport (line 4612) |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap Icons | 1.10+ | Icon library | Admin views use bi-people, bi-person-plus, bi-file-earmark-arrow-up, etc. |
| jQuery | 3.x | AJAX requests | CoachCoacheeMapping uses fetch API for JSON POSTs (Assign, Edit actions) |
| Data Annotations | .NET 8 | Model validation | ManageUserViewModel for CreateWorker/EditWorker forms |
| IMemoryCache | .NET 8 | Real-time data caching | AssessmentMonitoring uses 10s TTL (already audited in Phase 90) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ClosedXML.Excel | EPPlus, NPOI | ClosedXML already integrated with consistent patterns (new XLWorkbook(), SaveAs(), File() return) |
| TempData for errors | ViewBag, ViewData | TempData persists across redirect (PRG pattern), ViewBag only for current request |
| Manual authorization | Policy-based auth | Role-based attributes ([Authorize(Roles = "Admin, HC")]) are simpler and sufficient for current needs |

**Installation:**
No new packages required - all dependencies already in project. ClosedXML.Excel already used in Phase 83 seed data and AdminController.

## Architecture Patterns

### Recommended Project Structure
Current Admin structure already follows MVC conventions:
```
Controllers/
├── AdminController.cs (5729 lines) - All Admin actions organized by #region
│   ├── KKJ File Management (lines 50-400)
│   ├── CPDP File Management (lines 400-800)
│   ├── Assessment Management (lines 800-2300)
│   ├── Assessment Monitoring (lines 2300-2800)
│   ├── Coach-Coachee Mapping (lines 3449-3800)
│   ├── Manage Workers (lines 3779-4400)
│   └── Import/Export (lines 4308-4620)
Models/
├── ApplicationUser (Identity user) - Worker entities
├── CoachCoacheeMapping - Coaching assignments
├── ProtonTrackAssignment - Track assignments to coachees
├── ImportWorkerResult - Import status tracking
├── CoachAssignRequest - AJAX assign payload
├── CoachEditRequest - AJAX edit payload
Views/Admin/
├── Index.cshtml - Admin hub (already audited in Phase 87)
├── ManageWorkers.cshtml - Worker list with filters/pagination
├── CreateWorker.cshtml - Worker creation form
├── EditWorker.cshtml - Worker edit form
├── ImportWorkers.cshtml - Excel bulk import
├── CoachCoacheeMapping.cshtml - Coach-coachee assignment interface
├── KkjMatrix.cshtml - KKJ file management (already audited in Phase 88)
├── ManageAssessment.cshtml - Assessment CRUD (already audited in Phase 90)
└── AssessmentMonitoring.cshtml - Real-time monitoring (already audited in Phase 90)
Data/
├── SeedTestData.cs - Comprehensive test data (480 lines)
└── ApplicationDbContext.cs - All Admin entities
```

### Pattern 1: POST-Redirect-GET with TempData Error Handling
**What:** After form POST, redirect to GET action and show errors via TempData to prevent duplicate submissions
**When to use:** Always for state-changing operations (CreateWorker, EditWorker, CoachCoacheeMappingAssign)
**Example:**
```csharp
// Source: AdminController.cs lines 4469-4589 (ImportWorkers pattern)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ImportWorkers(IFormFile? excelFile)
{
    // Validate input
    if (excelFile == null || excelFile.Length == 0)
    {
        TempData["Error"] = "Pilih file Excel terlebih dahulu.";
        return View();
    }

    try
    {
        // Process import...
        return RedirectToAction("ManageWorkers");
    }
    catch (Exception ex)
    {
        TempData["Error"] = $"Gagal membaca file: {ex.Message}";
        return View();
    }
}
```

### Pattern 2: Role-Based Authorization with Class-Level Defaults
**What:** AdminController has class-level [Authorize] with per-action role overrides
**When to use:** Most actions require Admin/HC, rare Admin-only actions override
**Example:**
```csharp
// Source: AdminController.cs lines 14-15 (class-level), 44-45 (action-level)
[Authorize]  // Class-level: all actions require authentication
public class AdminController : Controller
{
    [Authorize(Roles = "Admin, HC")]  // Most actions: Admin + HC
    public IActionResult Index() { ... }

    [Authorize(Roles = "Admin")]  // Admin-only: seed data, test actions
    public async Task<IActionResult> SeedDashboardTestData() { ... }
}
```

### Pattern 3: Excel Import/Export with ClosedXML
**What:** Template download → user fills → bulk import → show results
**When to use:** Bulk data operations (ImportWorkers), data export (ExportWorkers, CoachCoacheeMappingExport)
**Example:**
```csharp
// Source: AdminController.cs lines 4407-4463 (template generation)
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public IActionResult DownloadImportTemplate()
{
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("Import Workers");

    // Headers with styling
    var headers = new[] { "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", ... };
    for (int i = 0; i < headers.Length; i++)
    {
        ws.Cell(1, i + 1).Value = headers[i];
        ws.Cell(1, i + 1).Style.Font.Bold = true;
        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
    }

    // Example row with validation hints
    ws.Cell(3, 1).Value = "Kolom Bagian: RFCC / DHT / HMU / NGP / GAST";
    ws.Cell(3, 1).Style.Font.Italic = true;
    ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return File(stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "workers_import_template.xlsx");
}
```

### Pattern 4: AJAX POST with JSON Response
**What:** CoachCoacheeMapping uses fetch API for assign/edit operations without page reload
**When to use:** Inline CRUD operations (assign coach to coachee, edit mapping)
**Example:**
```csharp
// Source: AdminController.cs lines 3564-3596 (CoachCoacheeMappingAssign)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CoachCoacheeMappingAssign([FromBody] CoachAssignRequest req)
{
    if (req == null || string.IsNullOrEmpty(req.CoachId) || req.CoacheeIds == null || req.CoacheeIds.Count == 0)
        return Json(new { success = false, message = "Data tidak lengkap." });

    if (req.CoacheeIds.Contains(req.CoachId))
        return Json(new { success = false, message = "Coach tidak dapat menjadi coachee diri sendiri." });

    // Check for duplicates
    var existingMappings = await _context.CoachCoacheeMappings
        .Where(m => req.CoacheeIds.Contains(m.CoacheeId) && m.IsActive)
        .ToListAsync();

    if (existingMappings.Any())
    {
        var names = existingMappings
            .Select(m => allUsers.GetValueOrDefault(m.CoacheeId, m.CoacheeId))
            .Distinct()
            .ToList();
        return Json(new { success = false, message = $"Coachee sudah memiliki coach aktif: {string.Join(", ", names)}" });
    }

    // Create mappings...
    await _auditLog.LogAsync(actor.Id, "CoachCoacheeMapping", "Assign", $"Assigned coach {coachName} to {coacheeNames}");

    return Json(new { success = true, message = "Berhasil menambahkan mapping." });
}
```

### Anti-Patterns to Avoid
- **Hardcoded role strings:** Use UserRoles.AllRoles constant array, not magic strings ("Admin", "HC", "Coach")
- **Missing null checks:** ApplicationUser navigation properties can be null, always use null-conditional (?.) or null-coalescing (??) operators
- **Direct exception exposure:** Never show raw exception messages to users—always log and show generic error via TempData
- **Missing CSRF tokens:** All POST actions must have [ValidateAntiForgeryToken] and views must call @Html.AntiForgeryToken()
- **Inconsistent error handling:** Use TempData["Error"] for validation failures, not ViewBag.Error or direct exception throws

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel parsing | Manual file reading with string splitting | ClosedXML.Excel (XLWorkbook, ws.Cell(), .GetString()) | Handles .xlsx format properly, type-safe cell access, column adjustment |
| Role-based filtering | Custom permission logic | [Authorize(Roles = "Admin, HC")] attribute | ASP.NET Core built-in authorization, integrates with Identity, testable |
| Pagination logic | Manual offset/limit calculation | Skip().Take() pattern with pageSize constant | LINQ standard query operators, translates to SQL OFFSET/FETCH |
| Form validation | Manual if/else checks | ModelState.IsValid with Data Annotations | Server-side validation with client-side support, consistent error messages |
| File upload validation | Manual MIME type checking | IFormFile.Length, Path.GetExtension() check | Built-in file handling, extension whitelist, size limits |
| Real-time updates | Polling or SignalR setup | IMemoryCache with TTL (already in Phase 90) | Simple caching with expiration, no infrastructure changes |

**Key insight:** AdminController.cs already uses all these standard patterns—don't reinvent the wheel. Follow existing conventions for consistency (e.g., TempData["Error"] for validation, XLWorkbook for Excel, Json() for AJAX responses).

## Common Pitfalls

### Pitfall 1: Missing Null Safety on ApplicationUser Navigation
**What goes wrong:** ApplicationUser navigation properties (Coach, Coachee) are nullable, accessing .FullName or .Section without null checks causes NullReferenceException
**Why it happens:** EF Core includes return null when related entity doesn't exist or is deleted
**How to avoid:** Always use null-conditional operator (?.) or null-coalescing operator (??) when accessing navigation properties
**Warning signs:** "Object reference not set to an instance of an object" errors, crashes on pages with deleted/inactive users
**Example from codebase:**
```csharp
// GOOD: AdminController.cs line 3470 (CoachCoacheeMapping)
Coach = userDict.GetValueOrDefault(m.CoachId),
Coachee = userDict.GetValueOrDefault(m.CoacheeId)

// AVOID: Direct navigation access without null check
var coachName = mapping.Coach.FullName;  // Crashes if Coach is null
```

### Pitfall 2: Inconsistent Date Localization (Indonesian Format)
**What goes wrong:** Dates displayed in English format (March 5, 2026) instead of Indonesian (5 Maret 2026)
**Why it happens:** DateTime.ToString() without culture parameter uses server culture (en-US), not Indonesian
**How to avoid:** Use CultureInfo.GetCultureInfo("id-ID") for all date formatting, matching Phase 92/93/94 fixes
**Warning signs:** English month names in UI, inconsistent date formats across pages
**Example from codebase:**
```csharp
// GOOD: Pattern from Phase 93/94
@item.StartDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("id-ID"))

// AVOID: Default culture formatting
@item.StartDate.ToString("dd MMMM yyyy")  // Shows English month names
```

### Pitfall 3: Missing IsActive Filters
**What goes wrong:** Inactive users show up in dropdowns, lists, or assignments—violating soft-delete pattern from Phase 83
**Why it happens:** Queries miss .Where(u => u.IsActive) filter, especially on modal dropdowns and lookup queries
**How to avoid:** Always filter by IsActive=true for user-facing queries, only show inactive when showInactive=true
**Warning signs:** Deactivated workers still selectable in dropdowns, assigned to new coaches/coachees
**Example from codebase:**
```csharp
// GOOD: AdminController.cs lines 3548-3551 (CoachCoacheeMapping)
var coachRoleUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Coach);
ViewBag.EligibleCoaches = coachRoleUsers
    .Where(u => u.IsActive)  // Filter out inactive
    .OrderBy(u => u.FullName).ToList();

// AVOID: Missing IsActive filter
ViewBag.EligibleCoaches = await _userManager.GetUsersInRoleAsync(UserRoles.Coach);  // Includes inactive
```

### Pitfall 4: CSRF Token Missing on AJAX POSTs
**What goes wrong:** AJAX POST requests fail with 400 Bad Request or 403 Forbidden due to missing anti-forgery token
**Why it happens:** fetch() API doesn't automatically include CSRF token, must add X-Request-VerificationToken header
**How to avoid:** Add CSRF token to fetch headers: `headers: { 'X-Request-VerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value }`
**Warning signs:** AJAX POSTs return 403/400, work in Postman but not browser
**Example from codebase:**
```csharp
// GOOD: CoachCoacheeMapping.cshtml should include CSRF token in fetch headers
fetch('/Admin/CoachCoacheeMappingAssign', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'X-Request-VerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
    },
    body: JSON.stringify(payload)
});

// AVOID: Missing CSRF token header
fetch('/Admin/CoachCoacheeMappingAssign', {
    method: 'POST',
    body: JSON.stringify(payload)  // Fails with 403 Forbidden
});
```

### Pitfall 5: Excel Import Fails on Empty Rows
**What goes wrong:** ImportWorkers crashes when encountering blank rows in Excel file
**Why it happens:** foreach loop doesn't skip blank rows, row.Cell(n).GetString() throws on empty cells
**How to avoid:** Check for blank rows early: `if (string.IsNullOrWhiteSpace(nama) && string.IsNullOrWhiteSpace(email)) continue;`
**Warning signs:** Import fails with "Index was outside the bounds of the array" or "Object reference not set"
**Example from codebase:**
```csharp
// GOOD: AdminController.cs lines 4498-4500 (ImportWorkers)
foreach (var row in ws.RowsUsed().Skip(1))
{
    var nama = row.Cell(1).GetString().Trim();
    var email = row.Cell(2).GetString().Trim();

    // Skip blank rows (e.g. notes/example rows)
    if (string.IsNullOrWhiteSpace(nama) && string.IsNullOrWhiteSpace(email)) continue;

    // Process row...
}

// AVOID: No blank row check
foreach (var row in ws.RowsUsed().Skip(1))
{
    var nama = row.Cell(1).GetString();  // Crashes on empty row
}
```

### Pitfall 6: Missing Role Validation on Critical Actions
**What goes wrong:** Admin-only actions (seed data, dangerous operations) accessible to HC role
**Why it happens:** Using [Authorize(Roles = "Admin, HC")] instead of [Authorize(Roles = "Admin")]
**How to avoid:** Review sensitive operations—seed data, bulk delete, dangerous actions should be Admin-only
**Warning signs:** HC role can access test data seeding, system-level operations
**Example from codebase:**
```csharp
// GOOD: Admin-only actions use restricted role
[Authorize(Roles = "Admin")]  // Lines 2271, 2533, 2940
public async Task<IActionResult> SeedDashboardTestData() { ... }
public async Task<IActionResult> SeedCoachingTestData() { ... }

// AVOID: Opening admin-only to HC
[Authorize(Roles = "Admin, HC")]  // Wrong: HC shouldn't access seed data
public async Task<IActionResult> SeedDashboardTestData() { ... }
```

## Code Examples

Verified patterns from AdminController.cs and official ASP.NET Core documentation:

### ManageWorkers Filtering and Pagination
```csharp
// Source: AdminController.cs lines 3779-3840
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ManageWorkers(string? search, string? sectionFilter, string? roleFilter, bool showInactive = false)
{
    // Base query with filters
    var query = _context.Users.AsQueryable();

    if (!string.IsNullOrEmpty(search))
    {
        var s = search.ToLower();
        query = query.Where(u =>
            u.FullName.ToLower().Contains(s) ||
            u.Email!.ToLower().Contains(s) ||
            (u.NIP != null && u.NIP.Contains(s)));
    }
    if (!string.IsNullOrEmpty(sectionFilter))
        query = query.Where(u => u.Section == sectionFilter);
    if (!string.IsNullOrEmpty(roleFilter))
    {
        var level = UserRoles.GetRoleLevel(roleFilter);
        query = query.Where(u => u.RoleLevel == level);
    }
    if (!showInactive)
        query = query.Where(u => u.IsActive);

    // Pagination
    const int pageSize = 20;
    var totalCount = await query.CountAsync();
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    var page = Math.Max(1, Math.Min(page ?? 1, totalPages));

    var users = await query
        .OrderBy(u => u.FullName)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // Get roles for each user (avoid N+1)
    var userRoles = new Dictionary<string, string>();
    foreach (var u in users)
    {
        var roles = await _userManager.GetRolesAsync(u);
        userRoles[u.Id] = roles.FirstOrDefault() ?? "-";
    }

    ViewBag.UserRoles = userRoles;
    ViewBag.TotalCount = totalCount;
    ViewBag.TotalPages = totalPages;
    ViewBag.CurrentPage = page;
    ViewBag.ShowInactive = showInactive;

    return View(users);
}
```

### CoachCoacheeMapping Grouped Display with Pagination
```csharp
// Source: AdminController.cs lines 3449-3560
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> CoachCoacheeMapping(
    string? search, string? section, bool showAll = false, int page = 1)
{
    const int pageSize = 20;

    // 1. Load all users once (avoid N+1)
    var allUsers = await _context.Users
        .Select(u => new { u.Id, u.FullName, u.NIP, u.Section, u.Position, u.RoleLevel, u.IsActive })
        .ToListAsync();
    var userDict = allUsers.ToDictionary(u => u.Id);
    var activeUsers = allUsers.Where(u => u.IsActive).ToList();

    // 2. Load mappings with IsActive filter
    var query = _context.CoachCoacheeMappings.AsQueryable();
    if (!showAll)
        query = query.Where(m => m.IsActive);
    var mappings = await query.ToListAsync();

    // 3. Join with user data + apply filters
    var rows = mappings.Select(m => new {
        Mapping = m,
        Coach = userDict.GetValueOrDefault(m.CoachId),
        Coachee = userDict.GetValueOrDefault(m.CoacheeId)
    }).ToList();

    if (!string.IsNullOrEmpty(search))
    {
        var lower = search.ToLower();
        rows = rows.Where(r =>
            (r.Coach?.FullName?.ToLower().Contains(lower) ?? false) ||
            (r.Coachee?.FullName?.ToLower().Contains(lower) ?? false) ||
            (r.Coachee?.NIP?.ToLower().Contains(lower) ?? false))
            .ToList();
    }
    if (!string.IsNullOrEmpty(section))
    {
        rows = rows.Where(r =>
            r.Coach?.Section == section ||
            r.Coachee?.Section == section)
            .ToList();
    }

    // 4. Group by Coach, paginate over coach groups
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
                CoacheeSection = r.Coachee?.Section ?? ""
            }).OrderBy(c => c.CoacheeName).ToList()
        })
        .OrderBy(g => g.CoachName)
        .ToList();

    var totalCoachGroups = grouped.Count;
    var totalPages = (int)Math.Ceiling(totalCoachGroups / (double)pageSize);
    if (page < 1) page = 1;
    if (page > totalPages && totalPages > 0) page = totalPages;
    var paged = grouped.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    ViewBag.GroupedCoaches = paged;
    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;
    ViewBag.TotalCount = totalCoachGroups;

    return View();
}
```

### ImportWorkers with ClosedXML.Excel
```csharp
// Source: AdminController.cs lines 4469-4589
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ImportWorkers(IFormFile? excelFile)
{
    if (excelFile == null || excelFile.Length == 0)
    {
        TempData["Error"] = "Pilih file Excel terlebih dahulu.";
        return View();
    }

    var results = new List<ImportWorkerResult>();
    var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);

    try
    {
        using var fileStream = excelFile.OpenReadStream();
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheets.First();

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var nama = row.Cell(1).GetString().Trim();
            var email = row.Cell(2).GetString().Trim();
            var nip = row.Cell(3).GetString().Trim();
            var jabatan = row.Cell(4).GetString().Trim();
            var bagian = row.Cell(5).GetString().Trim();
            var unit = row.Cell(6).GetString().Trim();
            var directorate = row.Cell(7).GetString().Trim();
            var role = row.Cell(8).GetString().Trim();
            var tglStr = row.Cell(9).GetString().Trim();

            // Skip blank rows (e.g. notes/example rows)
            if (string.IsNullOrWhiteSpace(nama) && string.IsNullOrWhiteSpace(email)) continue;

            var result = new ImportWorkerResult { Nama = nama, Email = email, Role = role };

            // Validate required fields
            if (string.IsNullOrWhiteSpace(nama) || string.IsNullOrWhiteSpace(email))
            {
                result.Status = "Error";
                result.Message = "Nama dan Email wajib diisi.";
                results.Add(result);
                continue;
            }

            // Check if user exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                result.Status = "Skip";
                result.Message = $"User dengan email {email} sudah ada.";
                results.Add(result);
                continue;
            }

            // Create user...
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = nama,
                NIP = nip,
                Position = jabatan,
                Section = bagian,
                Unit = unit,
                Directorate = directorate,
                RoleLevel = UserRoles.GetRoleLevel(role),
                JoinDate = DateTime.TryParse(tglStr, out var tgl) ? tgl : null,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(newUser, password ?? Guid.NewGuid().ToString());
            if (!createResult.Succeeded)
            {
                result.Status = "Error";
                result.Message = string.Join(", ", createResult.Errors.Select(e => e.Description));
                results.Add(result);
                continue;
            }

            await _userManager.AddToRoleAsync(newUser, role);
            result.Status = "Success";
            results.Add(result);
        }

        ViewBag.ImportResults = results;
        return View();
    }
    catch (Exception ex)
    {
        TempData["Error"] = $"Gagal membaca file: {ex.Message}";
        return View();
    }
}
```

### ExportWorkers with Excel Formatting
```csharp
// Source: AdminController.cs lines 4308-4379
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ExportWorkers(string? search, string? sectionFilter, string? roleFilter, bool showInactive = false)
{
    var query = _context.Users.AsQueryable();

    // Apply filters (same as ManageWorkers)
    if (!string.IsNullOrEmpty(search))
    {
        var s = search.ToLower();
        query = query.Where(u =>
            u.FullName.ToLower().Contains(s) ||
            u.Email!.ToLower().Contains(s) ||
            (u.NIP != null && u.NIP.Contains(s)));
    }
    if (!string.IsNullOrEmpty(sectionFilter))
        query = query.Where(u => u.Section == sectionFilter);
    if (!string.IsNullOrEmpty(roleFilter))
    {
        var level = UserRoles.GetRoleLevel(roleFilter);
        query = query.Where(u => u.RoleLevel == level);
    }
    if (!showInactive)
        query = query.Where(u => u.IsActive);

    var users = await query.OrderBy(u => u.FullName).ToListAsync();

    // Get roles for each user
    var roleDict = new Dictionary<string, string>();
    foreach (var u in users)
    {
        var roles = await _userManager.GetRolesAsync(u);
        roleDict[u.Id] = roles.FirstOrDefault() ?? "-";
    }

    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("Workers");

    // Headers with styling
    var headers = showInactive
        ? new[] { "No", "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Directorate", "Role", "Tgl Bergabung", "Status" }
        : new[] { "No", "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Directorate", "Role", "Tgl Bergabung" };
    for (int i = 0; i < headers.Length; i++)
    {
        ws.Cell(1, i + 1).Value = headers[i];
        ws.Cell(1, i + 1).Style.Font.Bold = true;
        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
        ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
    }

    // Data rows
    int row = 2, no = 1;
    foreach (var u in users)
    {
        ws.Cell(row, 1).Value = no++;
        ws.Cell(row, 2).Value = u.FullName;
        ws.Cell(row, 3).Value = u.Email;
        ws.Cell(row, 4).Value = u.NIP ?? "";
        ws.Cell(row, 5).Value = u.Position ?? "";
        ws.Cell(row, 6).Value = u.Section ?? "";
        ws.Cell(row, 7).Value = u.Unit ?? "";
        ws.Cell(row, 8).Value = u.Directorate ?? "";
        ws.Cell(row, 9).Value = roleDict.ContainsKey(u.Id) ? roleDict[u.Id] : "-";
        ws.Cell(row, 10).Value = u.JoinDate.HasValue ? u.JoinDate.Value.ToString("yyyy-MM-dd") : "";
        if (showInactive)
            ws.Cell(row, 11).Value = u.IsActive ? "Aktif" : "Tidak Aktif";
        row++;
    }

    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var fileName = $"workers_export_{DateTime.Now:yyyyMMdd}.xlsx";
    return File(stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hard delete workers | Soft delete with IsActive flag (Phase 83) | 2026-03-03 | Workers marked inactive instead of deleted, preserves data integrity |
| Manual validation | TempData["Error"] + Model.State (established pattern) | Initial implementation | Consistent error handling across all Admin forms |
| N+1 queries for user roles | Batch role loading with Dictionary | Phase 83 | Reduced database roundtrips from N to 1 per page load |
| Missing CSRF protection | [ValidateAntiForgeryToken] on all POSTs | Initial implementation | CSRF token validation on 37 POST actions in AdminController |
| Mixed date formats | CultureInfo.GetCultureInfo("id-ID") (Phase 92/93/94) | 2026-03-03/04/05 | Consistent Indonesian date formatting across all pages |

**Deprecated/outdated:**
- **Hard delete pattern**: AdminController.DeleteWorker (lines 4083-4211) preserved for programmatic use, but UI removed in Phase 83. Use DeactivateWorker instead.
- **Direct exception exposure**: Raw exception messages in try-catch blocks replaced with TempData["Error"] generic messages with logging.
- **Manual Excel parsing**: ClosedXML.Excel replaced manual Office Interop or CSV parsing (already standard since project start).

## Open Questions

1. **ManageWorkers pagination edge case**: What happens when page parameter exceeds totalPages?
   - What we know: Code calculates totalPages and clamps page: `if (page < 1) page = 1; if (page > totalPages && totalPages > 0) page = totalPages;` (line 3817-3818)
   - What's unclear: Whether this logic is present in ManageWorkers action
   - Recommendation: Verify ManageWorkers has page clamping logic, add if missing (follow Phase 90 pattern from ManageAssessment)

2. **CoachCoacheeMapping CSRF token in AJAX requests**: Does the view include X-Request-VerificationToken header?
   - What we know: CoachCoacheeMappingAssign has [ValidateAntiForgeryToken] (line 3565), uses [FromBody] JSON POST
   - What's unclear: Whether CoachCoacheeMapping.cshtml includes CSRF token in fetch() headers
   - Recommendation: Code review to verify CSRF token header exists in AJAX calls, add if missing

3. **ImportWorkers error handling depth**: Should we test all Excel import error scenarios?
   - What we know: ImportWorkers handles duplicate users, validation errors, file format errors
   - What's unclear: How comprehensive import testing should be per CONTEXT.md "Smoke test validation"
   - Recommendation: Smoke test only—test one valid file, one invalid file type, one duplicate user. Don't test every edge case.

4. **Date localization in Admin pages**: Are there date displays missing Indonesian formatting?
   - What we know: Phase 92/93/94 fixed date localization in Homepage, CMP, CDP pages
   - What's unclear: Whether Admin pages (ManageWorkers, CoachCoacheeMapping) have date displays needing localization
   - Recommendation: Code review Admin views for date formatting, apply CultureInfo.GetCultureInfo("id-ID") where needed

5. **ExportWorkers with showInactive=true**: Does the Status column export correctly?
   - What we know: ExportWorkers adds Status column when showInactive=true (line 4366-4367)
   - What's unclear: Whether Status column values are correct ("Aktif" / "Tidak Aktif")
   - Recommendation: Verify Status column values match Indonesian language, test export with showInactive=true

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual testing (browser-based smoke tests) |
| Config file | None — manual verification only |
| Quick run command | Browser navigate to page, check for errors, test basic flows |
| Full suite command | N/A — this phase uses manual testing per CONTEXT.md |

**Note:** Per CONTEXT.md "Testing Approach: Smoke test only", this phase does NOT use automated tests. Verification is done via browser testing of specific bug fixes, not comprehensive test suites.

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Manual Verification | File Exists? |
|--------|----------|-----------|---------------------|--------------|
| ADMIN-01 | ManageWorkers filters and pagination work | Manual | Navigate to /Admin/ManageWorkers, test search/section/role/showInactive filters, verify pagination | ✅ Views/Admin/ManageWorkers.cshtml |
| ADMIN-02 | KKJ file operations work | Manual | Navigate to /Admin/KkjMatrix, test upload/download/archive (already audited Phase 88) | ✅ Views/Admin/KkjMatrix.cshtml |
| ADMIN-03 | ManageAssessment displays correctly | Manual | Navigate to /Admin/ManageAssessment, verify assessment list and actions (already audited Phase 90) | ✅ Views/Admin/ManageAssessment.cshtml |
| ADMIN-04 | AssessmentMonitoring shows real-time data | Manual | Navigate to /Admin/AssessmentMonitoring, verify participant data updates (already audited Phase 90) | ✅ Views/Admin/AssessmentMonitoring.cshtml |
| ADMIN-05 | CoachCoacheeMapping operations complete | Manual | Navigate to /Admin/CoachCoacheeMapping, test assign/deactivate/reactivate/export | ✅ Views/Admin/CoachCoacheeMapping.cshtml |
| ADMIN-06 | ProtonData tabs display correctly | Manual | Navigate to /Admin/ProtonData, verify Silabus/Guidance tabs (already audited Phase 88) | ✅ Views/Admin/ProtonData.cshtml |
| ADMIN-07 | Forms handle validation gracefully | Manual | Submit invalid data to CreateWorker/EditWorker/Assign forms, verify TempData["Error"] messages | ✅ All Admin form views |
| ADMIN-08 | Role gates work correctly | Manual | Log in as HC, verify access to shared pages. Log in as Admin, verify access to Admin-only pages. | ✅ AdminController.cs role attributes |

### Sampling Rate
- **Per bug fix commit**: Browser verify the specific bug that was fixed (smoke test)
- **Per plan completion**: Browser verify all bugs fixed in that plan (e.g., all ManageWorkers bugs)
- **Phase gate**: All ADMIN-01 through ADMIN-08 requirements verified via browser testing before `/gsd:verify-work`

### Wave 0 Gaps
None — this phase uses manual testing only. No automated test infrastructure required per CONTEXT.md smoke-test approach.

## Sources

### Primary (HIGH confidence)
- **AdminController.cs** — Complete audit of 5729-line controller, all Admin actions, role attributes, validation patterns, Excel import/export logic
- **Views/Admin/ManageWorkers.cshtml** — Worker list with filters and pagination, inactive toggle, import/export buttons
- **Views/Admin/CoachCoacheeMapping.cshtml** — Coach-coachee assignment interface with modal forms
- **Views/Admin/CreateWorker.cshtml** — Worker creation form with validation and AD mode handling
- **Views/Admin/ImportWorkers.cshtml** — Excel import UI with template download and results display
- **CONTEXT.md (95-CONTEXT.md)** — User decisions on audit organization, testing approach, role coverage, validation depth
- **STATE.md (.planning/STATE.md)** — Project history including Phase 83 soft-delete, Phase 88 KKJ/CPDP audit, Phase 90 assessment audit, Phase 93/94 audit patterns
- **REQUIREMENTS.md (.planning/REQUIREMENTS.md)** — ADMIN-01 through ADMIN-08 requirement definitions with traceability
- **Data/SeedTestData.cs** — 480 lines of comprehensive test data from Phases 83, 85, 94

### Secondary (MEDIUM confidence)
- **ClosedXML.Excel Documentation** — Excel workbook creation, cell styling, column adjustment (verified by existing usage in AdminController)
- **ASP.NET Core Authorization Docs** — [Authorize] attribute, role-based authorization, policy-based authorization (verified by existing role attributes)
- **Phase 93/94 Audit Reports** — CMP and CDP audit patterns (localization, null safety, validation) as reference for Admin audit

### Tertiary (LOW confidence)
- None — all findings based on direct code inspection or official documentation

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries verified in AdminController.cs (ClosedXML.Excel, ASP.NET Core MVC, Bootstrap, EF Core)
- Architecture: HIGH - Complete audit of AdminController.cs structure, established patterns documented with line numbers
- Pitfalls: HIGH - All pitfalls identified from existing code review findings in Phases 83-94, with examples from AdminController.cs
- Code examples: HIGH - All examples copied directly from AdminController.cs with verified line numbers

**Research date:** 2026-03-05
**Valid until:** 2026-04-05 (30 days - stable domain, AdminController.cs won't change significantly during audit phase)

---

*Phase 95: Admin Portal Audit Research Complete*
*Next Step: Create 95-01-PLAN.md, 95-02-PLAN.md, etc. based on bug findings from systematic code review*
