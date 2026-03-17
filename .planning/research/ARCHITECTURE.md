# Architecture Research

**Domain:** Certificate Monitoring — CDP module extension, ASP.NET Core MVC
**Researched:** 2026-03-17
**Confidence:** HIGH (derived from direct codebase inspection)

---

## Standard Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Browser (Bootstrap 5)                        │
│  CDP/Index card  →  MonitoringSertifikat page                        │
│  Filter bar (Bagian > Unit > Status > Search, AJAX)                  │
│  Summary cards (Total / Aktif / Akan Expired / Expired)              │
│  Certificate table + download + Export Excel button                  │
└─────────────────────┬───────────────────────────────────────────────┘
                      │ HTTP GET / AJAX partial
┌─────────────────────▼───────────────────────────────────────────────┐
│                     CDPController                                     │
│  MonitoringSertifikat(bagian?, unit?, status?, search?)              │
│  FilterMonitoringSertifikat(...)   <- AJAX partial reload            │
│  GetCascadeOptions(section?)       <- reuse existing endpoint        │
│  ExportSertifikatExcel(...)        <- ClosedXML file response        │
└─────────────────────┬───────────────────────────────────────────────┘
                      │ EF Core queries
┌─────────────────────▼───────────────────────────────────────────────┐
│                    ApplicationDbContext                               │
│  TrainingRecords  (+ User navigation)                                 │
│  AssessmentSessions  (Status=="Completed", IsPassed==true,           │
│                        GenerateCertificate==true)                    │
│  AspNetUsers  (Section, Unit, FullName, NIP for scope/display)       │
└─────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Implementation |
|-----------|----------------|----------------|
| `CDPController.MonitoringSertifikat` | Full-page GET; builds ViewModel with summary counts + rows | New action, same class |
| `CDPController.FilterMonitoringSertifikat` | AJAX partial; mirrors `FilterCoachingProton` pattern | New action, returns `PartialView` |
| `CDPController.ExportSertifikatExcel` | ClosedXML workbook stream; mirrors existing export actions | New action, returns `File(...)` |
| `CDPController.GetCascadeOptions` | Already exists; returns units for a section; reused unchanged | Existing — no modification |
| `MonitoringSertifikatViewModel` | Page-level ViewModel: summary counts + list rows + filter state | New file in `Models/` |
| `SertifikatRow` | Flat row ViewModel per certificate (both sources unified) | Inner class or separate file |
| `Views/CDP/MonitoringSertifikat.cshtml` | Full page: filter bar + summary cards + table | New view |
| `Views/CDP/Shared/_MonitoringSertifikatTablePartial.cshtml` | Table partial returned by AJAX filter | New partial |
| `Views/CDP/Index.cshtml` | Add one new card linking to `MonitoringSertifikat` | Modified |

---

## Recommended Project Structure

```
Controllers/
└── CDPController.cs                   # Add 3 new actions + 1 private helper; no structural change

Models/
└── MonitoringSertifikatViewModel.cs   # New ViewModel file

Views/
└── CDP/
    ├── Index.cshtml                   # Modified: add one card
    ├── MonitoringSertifikat.cshtml    # New full page
    └── Shared/
        └── _MonitoringSertifikatTablePartial.cshtml  # New AJAX partial
```

### Structure Rationale

- **No new controller:** Certificate monitoring is a CDP feature. Adding to `CDPController` keeps routing at `/CDP/MonitoringSertifikat` and shares the existing DI constructor (UserManager, context, env, logger).
- **Separate ViewModel file:** Other multi-concern ViewModels (`CDPDashboardViewModel.cs`, `CoachingViewModels.cs`) each have their own file. Following that convention avoids bloating an existing model file.
- **Partial view for AJAX:** Matches the `_CoachingProtonContentPartial` pattern exactly. JS calls `FilterMonitoringSertifikat`, replaces a `div#cert-table-container`. No page reload.

---

## Architectural Patterns

### Pattern 1: Role-Scoped Data Query

**What:** Controller reads the current user's role level then applies a WHERE clause at the query layer — not in the view — to restrict which rows are visible.

**When to use:** Any action returning data scoped to the caller's organizational position.

**Trade-offs:** Server-enforced scoping is safe against client-side bypass. The role-level helpers (`HasFullAccess`, `HasSectionAccess`, `IsCoachingRole`) already exist in `UserRoles` — use them verbatim.

**Example:**
```csharp
int roleLevel = UserRoles.GetRoleLevel(userRole);

IQueryable<ApplicationUser> usersQuery = _context.Users
    .Where(u => u.IsActive);

if (UserRoles.HasSectionAccess(roleLevel))        // Level 4: SH/SrSpv
    usersQuery = usersQuery.Where(u => u.Section == user.Section);
else if (UserRoles.IsCoachingRole(roleLevel))      // Level 5/6: Coach/Coachee
    usersQuery = usersQuery.Where(u => u.Id == user.Id);
// else: Admin/HC/Management — no filter, all users visible
```

### Pattern 2: Unified Row Projection from Two Sources

**What:** `UnifiedTrainingRecord` already bridges `TrainingRecord` and `AssessmentSession` into a flat row. For certificate monitoring, a new `SertifikatRow` ViewModel follows the same pattern but adds expiry-status fields.

**When to use:** Whenever the page must render rows from heterogeneous tables in one list.

**Trade-offs:** Mirrors existing `UnifiedTrainingRecord` philosophy. Keeps the view simple — one list, one partial, no branching display logic per type.

**SertifikatRow structure:**
```csharp
public class SertifikatRow
{
    public string WorkerName    { get; set; } = "";
    public string? NIP          { get; set; }
    public string? Section      { get; set; }
    public string? Unit         { get; set; }
    public string RecordType    { get; set; } = ""; // "Training Manual" | "Assessment Online"
    public string Title         { get; set; } = "";
    public DateTime? IssueDate  { get; set; }
    public DateTime? ValidUntil { get; set; }       // null = Permanent
    public string CertifikatStatus { get; set; } = ""; // "Aktif" | "Akan Expired" | "Expired" | "Permanent"
    public string? SertifikatUrl   { get; set; }
    public int? TrainingRecordId   { get; set; }
    public int? AssessmentSessionId { get; set; }
}
```

**Expiry status derivation:**
```csharp
// TrainingRecord rows
string certStatus = (record.CertificateType == "Permanent" || !record.ValidUntil.HasValue)
    ? "Permanent"
    : record.ValidUntil.Value < DateTime.Now
        ? "Expired"
        : (record.ValidUntil.Value - DateTime.Now).Days <= 30
            ? "Akan Expired"
            : "Aktif";

// AssessmentSession rows (GenerateCertificate==true, IsPassed==true)
// Online assessment certificates are always Permanent
string certStatus = "Permanent";
```

### Pattern 3: AJAX Filter with Server-Side Override

**What:** The view sends filter values via AJAX GET to `FilterMonitoringSertifikat`. The controller re-enforces role scoping before querying — even if the client sends a different section/unit.

**When to use:** All filterable list pages in this project.

**Trade-offs:** Identical to the `FilterCoachingProton` action at lines 267-281. Copy that structure exactly.

**Example:**
```csharp
[HttpGet]
public async Task<IActionResult> FilterMonitoringSertifikat(
    string? section, string? unit, string? status, string? search)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Unauthorized();
    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault() ?? "";
    int roleLevel = UserRoles.GetRoleLevel(userRole);

    // Server-side scope override — client cannot bypass
    if (UserRoles.HasSectionAccess(roleLevel)) { section = user.Section; }
    else if (UserRoles.IsCoachingRole(roleLevel)) { section = user.Section; unit = user.Unit; }

    var rows = await BuildSertifikatRowsAsync(user, userRole, section, unit, status, search);
    return PartialView("Shared/_MonitoringSertifikatTablePartial", rows);
}
```

### Pattern 4: ClosedXML Excel Export

**What:** Build an `XLWorkbook` in-memory, stream it as a file response. Lines 2137-2184 in `CDPController` demonstrate the exact pattern used throughout this codebase.

**When to use:** All Excel exports in this project.

**Trade-offs:** No streaming concern for this dataset. Certificate counts are bounded by worker headcount.

**Recommended column set:**

| # | Column | Source |
|---|--------|--------|
| 1 | Nama Pekerja | `ApplicationUser.FullName` |
| 2 | NIP | `ApplicationUser.NIP` |
| 3 | Bagian | `SertifikatRow.Section` |
| 4 | Unit | `SertifikatRow.Unit` |
| 5 | Jenis | `SertifikatRow.RecordType` |
| 6 | Judul | `SertifikatRow.Title` |
| 7 | Tanggal Terbit | `SertifikatRow.IssueDate` |
| 8 | Berlaku Hingga | `SertifikatRow.ValidUntil` |
| 9 | Status | `SertifikatRow.CertifikatStatus` |

---

## Data Flow

### Full-Page Load

```
User navigates to /CDP/MonitoringSertifikat
    |
    v
CDPController.MonitoringSertifikat(bagian?, unit?, status?, search?)
    |
    v
GetCurrentUser + GetRoles
    |
    v
BuildSertifikatRowsAsync(user, role, filters...)
    |-- Enforce scope (role override on section/unit)
    |-- Query TrainingRecord JOIN User (SertifikatUrl != null OR CertificateType set)
    |-- Query AssessmentSession JOIN User (Status=="Completed" + IsPassed==true + GenerateCertificate==true)
    |-- Project both to List<SertifikatRow>
    |-- Apply filter params (section/unit/status/search)
    v
Compute summary counts from row list
    (Total, Aktif, Akan Expired, Expired — .Count() in memory)
    |
    v
MonitoringSertifikatViewModel { SummaryCards, Rows, FilterState }
    |
    v
View(model) -> MonitoringSertifikat.cshtml
```

### AJAX Filter Reload

```
User changes filter dropdown
    |
    v
JS fetch("/CDP/FilterMonitoringSertifikat?section=...&unit=...&status=...&search=...")
    |
    v
FilterMonitoringSertifikat action -> same BuildSertifikatRowsAsync
    |
    v
PartialView("Shared/_MonitoringSertifikatTablePartial", rows)
    |
    v
JS replaces innerHTML of div#cert-table-container
```

### Cascade Dropdown

```
User selects Bagian
    |
    v
JS fetch("/CDP/GetCascadeOptions?section=RFCC")  <- existing endpoint, no change
    |
    v
Returns { units: [...] }
    |
    v
JS populates Unit dropdown
```

### Excel Export

```
User clicks Export Excel
    |
    v
GET /CDP/ExportSertifikatExcel?section=...&unit=...&status=...&search=...
    |
    v
Same BuildSertifikatRowsAsync call with same filter params
    |
    v
XLWorkbook built in memory, columns per table above
    |
    v
File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
     "Sertifikat_{date}.xlsx")
```

---

## Integration Points

### Modified Components

| Component | Change |
|-----------|--------|
| `Views/CDP/Index.cshtml` | Add one card (col-12 col-md-6 col-lg-4) linking to MonitoringSertifikat — follow existing card markup exactly |
| `Controllers/CDPController.cs` | Add 3 new public actions + 1 private `BuildSertifikatRowsAsync` helper — no changes to existing actions |

### New Components

| Component | Depends On |
|-----------|-----------|
| `Models/MonitoringSertifikatViewModel.cs` | `SertifikatRow` (inner class or same file) |
| `Views/CDP/MonitoringSertifikat.cshtml` | ViewModel, Bootstrap 5, existing `site.css` |
| `Views/CDP/Shared/_MonitoringSertifikatTablePartial.cshtml` | `List<SertifikatRow>` |

### Reused Without Modification

| Component | How Reused |
|-----------|-----------|
| `CDPController.GetCascadeOptions` | Bagian->Unit cascade; already works for any section |
| `OrganizationStructure.GetUnitsForSection` | Called inside GetCascadeOptions — no change |
| `UserRoles.HasFullAccess / HasSectionAccess / IsCoachingRole` | Role-scoping gate logic |
| `TrainingRecord.IsExpiringSoon / DaysUntilExpiry` | Computed properties already on the model |
| ClosedXML `XLWorkbook` pattern | Direct structural copy from existing export actions |

---

## Suggested Build Order

Dependencies flow in this order. Each step is independently testable before proceeding.

| Step | Task | Dependency |
|------|------|------------|
| 1 | Create `MonitoringSertifikatViewModel.cs` with `SertifikatRow` | None |
| 2 | Add `BuildSertifikatRowsAsync` private helper to `CDPController` | Step 1 |
| 3 | Add `MonitoringSertifikat` GET action (full page) | Step 2 |
| 4 | Create `Views/CDP/MonitoringSertifikat.cshtml` with static table (no AJAX yet) | Step 3 |
| 5 | Add `FilterMonitoringSertifikat` AJAX action + `_MonitoringSertifikatTablePartial` partial | Step 2 |
| 6 | Wire JS filter bar in MonitoringSertifikat.cshtml | Step 5 |
| 7 | Add `ExportSertifikatExcel` action | Step 2 |
| 8 | Add Export button to view + wire JS | Step 7 |
| 9 | Add card to `Views/CDP/Index.cshtml` | Step 4 (page must exist first) |

Steps 5 and 7 can be done in parallel (both depend only on Step 2). Steps 6 and 8 can be done in parallel.

---

## Anti-Patterns

### Anti-Pattern 1: Filtering Only in the View

**What people do:** Query all certificates regardless of role, then hide rows in Razor with `@if (userRole == ...)`.

**Why it's wrong:** Malicious users can call `FilterMonitoringSertifikat` directly with arbitrary section/unit params and bypass role scoping.

**Do this instead:** Apply scope in the controller before any filter params are honored, mirroring the `FilterCoachingProton` override block at CDPController lines 274-277.

### Anti-Pattern 2: Using AssessmentSession.User Navigation Property

**What people do:** Rely on `AssessmentSession.User` for Section/Unit display.

**Why it's wrong:** `AssessmentSession` has no EF navigation property to `ApplicationUser` — only a `UserId` string FK. Accessing `.User` causes a null reference or an N+1 query per row.

**Do this instead:** Join explicitly in LINQ or load users into a dictionary keyed by UserId, then look up per row. Example:
```csharp
var userIds = sessions.Select(s => s.UserId).Distinct().ToList();
var userMap = await _context.Users
    .Where(u => userIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id);
```

### Anti-Pattern 3: Four Separate COUNT Queries for Summary Cards

**What people do:** Write four `_context.TrainingRecords.CountAsync(...)` calls for the four status buckets.

**Why it's wrong:** Four round-trips for data that is already loaded. Certificate volumes are bounded by headcount — in-memory grouping is negligible cost.

**Do this instead:** Load all rows once into `List<SertifikatRow>`, then compute:
```csharp
var total          = rows.Count;
var aktif          = rows.Count(r => r.CertifikatStatus == "Aktif");
var akanExpired    = rows.Count(r => r.CertifikatStatus == "Akan Expired");
var expired        = rows.Count(r => r.CertifikatStatus == "Expired");
```

### Anti-Pattern 4: Duplicating the Cascade Endpoint

**What people do:** Create a new `GetCertCascadeOptions` endpoint for the Bagian->Unit dropdowns on the monitoring page.

**Why it's wrong:** `GetCascadeOptions` already works for any page; duplicating it creates two endpoints to maintain.

**Do this instead:** Call the existing `/CDP/GetCascadeOptions?section=...` from the monitoring page's filter JS, identical to CoachingProton.

---

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Current (~200 workers) | In-memory row projection is fine; no index changes needed |
| 1k workers | Add `.AsNoTracking()` to read-only certificate queries (likely already present in similar queries) |
| 10k+ workers | Move `BuildSertifikatRowsAsync` to a service class; add DB index on `TrainingRecord.UserId` and `AssessmentSession.UserId` + filter columns |

Certificate monitoring is read-heavy and introduces no write path.

---

## Sources

- Direct inspection: `Controllers/CDPController.cs` — FilterCoachingProton (L267), GetCascadeOptions (L287), ExportProgress ClosedXML block (L2137-2184)
- Direct inspection: `Models/TrainingRecord.cs`, `Models/AssessmentSession.cs`, `Models/UnifiedTrainingRecord.cs`
- Direct inspection: `Models/UserRoles.cs`, `Models/ApplicationUser.cs`
- Direct inspection: `Views/CDP/Index.cshtml` (card markup pattern)
- Project context: `.planning/PROJECT.md` (v7.4 milestone target features)

---
*Architecture research for: Certificate Monitoring (CDP module extension)*
*Researched: 2026-03-17*
