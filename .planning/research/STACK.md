# Stack Research

**Domain:** Certificate monitoring feature — CDP module extension (PortalHC KPB v7.4)
**Researched:** 2026-03-17
**Confidence:** HIGH

---

## Context: What Already Exists (Do Not Re-add)

These packages are already installed and validated. The certificate monitoring feature
uses them directly — no version changes, no new NuGet packages.

| Package | Version | Role in this feature |
|---------|---------|----------------------|
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.0 | Query `TrainingRecord` + `AssessmentSession` |
| `ClosedXML` | 0.105.0 | Export certificate list to Excel (reuse existing export pattern) |
| `QuestPDF` | 2026.2.2 | Download/regenerate certificate PDF (CMP pattern already established) |
| Bootstrap 5 (layout CDN) | 5.x | Summary cards, status badges, responsive table |
| Bootstrap Icons (layout CDN) | 1.x | Icons on status badges and nav card |

**No new NuGet packages are required for this feature.**

---

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| ASP.NET Core MVC | .NET 8 (existing) | New `MonitoringSertifikat` action in `CDPController` + Razor view | Fits existing controller/view/model structure with zero friction |
| EF Core (SqlServer) | 8.0.0 (existing) | LINQ projection joining `TrainingRecord` and `AssessmentSession` into a flat `CertificateMonitoringRow` | Both entities already mapped; `ValidUntil` and `CertificateType` exist on `TrainingRecord` |
| ClosedXML | 0.105.0 (existing) | Export filtered certificate rows as `.xlsx` | Exact same `FileStreamResult` + `XLWorkbook` pattern as `AdminController.ExportWorkers` — copy and adapt |
| Bootstrap 5 | Layout CDN (existing) | `card` grid for summary counters, `badge` for expiry status, `table-responsive` for the list | No JS framework needed; server-rendered HTML is sufficient for this data density |

### Supporting Libraries

No new libraries are needed. Each capability is handled by what already exists:

| Capability | Handled By | Notes |
|------------|-----------|-------|
| Days-until-expiry calculation | `TrainingRecord.DaysUntilExpiry` computed property (already in model) | Returns `int?`; already written — read it in the controller, pass to ViewModel |
| Expiry status classification | C# in ViewModel mapping (no library) | `CertificateType == "Permanent"` → Permanent; `DaysUntilExpiry < 0` → Expired; `<= 30` → Akan Expired; else → Aktif |
| Status badge color | Bootstrap contextual classes (`bg-success`, `bg-warning`, `bg-danger`, `bg-secondary`) | No custom CSS needed |
| Cascade filter dropdown (Bagian > Unit) | Vanilla `fetch()` + existing JSON endpoint pattern | Copy `GetUnitsForSection` AJAX pattern from `CDPController` / Dashboard |
| Role-scoped data filtering | Existing `User.IsInRole()` + `_userManager.GetUserId()` pattern | Same approach as CDP Dashboard — Admin/HC see all, SH/SrSpv see section, Coach/Coachee see own |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| EF Core Tools (existing) | Migration not needed — `ValidUntil` and `CertificateType` columns already exist on `TrainingRecord` | Verify with `dotnet ef migrations list` before starting phases |

---

## Installation

No installation steps required. All dependencies are already in `HcPortal.csproj`.

---

## Alternatives Considered

| Recommended | Alternative | Why Not |
|-------------|-------------|---------|
| Server-rendered Razor table | DataTables.js (client-side) | Adds a JS dependency for a feature that already has server-side role-scoped filtering; DataTables is worthwhile only when the full unfiltered dataset is sent to the client, which is incompatible with role scoping |
| ClosedXML export (existing) | EPPlus | ClosedXML is already installed and the export pattern is proven; no reason to introduce EPPlus |
| LINQ projection to flat ViewModel | Database view or stored procedure | EF LINQ projection is maintainable and debuggable; a stored proc only makes sense if query performance becomes a bottleneck, which is unlikely at this data volume |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Chart.js / ApexCharts for summary cards | Summary cards are four integer counters — a `<div class="card">` with a large bold number communicates faster and builds faster than a chart | Bootstrap card grid with counter numbers and Bootstrap Icons |
| SignalR for live expiry updates | Certificate expiry changes at day granularity; a page load or manual refresh is sufficient and already the app's pattern for all monitoring pages | Standard GET action on page load |
| Background job / hosted service for expiry reclassification | No notifications are in scope; classification is read-only and computed at query time | In-query LINQ `DateTime.Now` comparison |
| Separate API controller | All other CDP features use MVC actions returning views; a JSON API adds two layers (controller + JS fetch) for no benefit here | Standard `CDPController` action returning `IActionResult` (View) |

---

## Stack Patterns by Variant

**For Excel export (Admin/HC role only):**
- Add a `ExportSertifikatExcel` action to `CDPController`
- Role-gate with `[Authorize(Roles = "Admin,HC")]`
- Use same `XLWorkbook` + `FileStreamResult` pattern as `AdminController` lines ~1020-1070
- Apply the same role-scope filter as the main view before building workbook rows

**For "download certificate file" action:**
- `TrainingRecord.SertifikatUrl` stores the path/URL already
- Serve via `PhysicalFile(path, "application/pdf")` or `Redirect(url)` — no new library
- Online assessment certificates: reuse QuestPDF regeneration already wired in CMP

**For Bagian/Unit cascade filter:**
- Copy the existing `GetUnitsForSection` JSON endpoint from `CDPController`
- Wire with inline `<script>` using `fetch()` — same pattern already in `Dashboard.cshtml`
- No jQuery plugins or external select2 needed

---

## Data Model Integration

The monitoring page reads from two existing tables:

**`TrainingRecord`** — manual (non-online) certificates

Relevant columns: `UserId`, `Judul`, `Kategori`, `ValidUntil`, `CertificateType`, `Status`, `SertifikatUrl`, `NomorSertifikat`

Status logic:
- `CertificateType == "Permanent"` → always "Permanent" (ignore `ValidUntil`)
- `ValidUntil < DateTime.Now` → "Expired"
- `DaysUntilExpiry <= 30` → "Akan Expired"
- Otherwise → "Aktif"

**`AssessmentSession`** — online assessment certificates

Relevant columns: `UserId`, `Title`, `Category`, `IsPassed`, `CompletedAt`, `GenerateCertificate`

Status logic: all online assessments with `GenerateCertificate == true && IsPassed == true` → always "Permanent" (no `ValidUntil` field on this entity)

**Unified ViewModel approach:** Project both tables into a `CertificateMonitoringRow` flat record in the controller action, then pass `List<CertificateMonitoringRow>` to the ViewModel. Do not use a database view or stored procedure.

---

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| ClosedXML 0.105.0 | .NET 8 | No issues — already shipping via v7.1 export feature |
| QuestPDF 2026.2.2 | .NET 8 | Already generating certificate PDFs in CMP module |
| EF Core 8.0.0 SqlServer | SQL Server 2019+ | `DateTime.Now` comparisons in LINQ translate correctly to T-SQL `GETDATE()` |

---

## Sources

- `HcPortal.csproj` (project root) — confirmed installed packages and exact versions (HIGH confidence)
- `Models/TrainingRecord.cs` — confirmed `ValidUntil`, `CertificateType`, `DaysUntilExpiry`, `IsExpiringSoon` already exist (HIGH confidence)
- `Models/AssessmentSession.cs` — confirmed `GenerateCertificate`, `IsPassed`, `CompletedAt` fields (HIGH confidence)
- `Views/CDP/Index.cshtml` — confirmed Bootstrap 5 card layout pattern used in CDP module (HIGH confidence)
- `Models/CDPDashboardViewModel.cs` — confirmed role-scoped filter ViewModel structure to replicate (HIGH confidence)

---

*Stack research for: Certificate monitoring page — CDP module, PortalHC KPB v7.4*
*Researched: 2026-03-17*
