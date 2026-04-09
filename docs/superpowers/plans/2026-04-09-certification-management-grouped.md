# CertificationManagement Grouped by Sertifikat — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor CertificationManagement page from flat worker-cert rows to 2-level navigation: grouped sertifikat list → detail worker list per sertifikat.

**Architecture:** Reuse existing `BuildSertifikatRowsAsync()` to get flat rows, then group by `Judul` for the main page. New detail page filters flat rows by Judul. Both pages use AJAX partial views for filtering/pagination.

**Tech Stack:** ASP.NET Core MVC, Razor Views, ClosedXML (Excel), Bootstrap 5, vanilla JS

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `Models/CertificationManagementViewModel.cs` | Modify | Add `SertifikatGroupRow` and `SertifikatGroupViewModel` |
| `Controllers/CMPController.cs` | Modify | Refactor `CertificationManagement()`, add detail/filter/export actions |
| `Views/CMP/CertificationManagement.cshtml` | Rewrite | Grouped sertifikat list with category filters |
| `Views/CMP/Shared/_SertifikatGroupTablePartial.cshtml` | Create | Partial for grouped table (AJAX target) |
| `Views/CMP/CertificationManagementDetail.cshtml` | Create | Detail worker list page |
| `Views/CMP/Shared/_CertificationManagementTablePartial.cshtml` | Modify | Minor: remove Judul column, always show Nama/Bagian/Unit |

---

### Task 1: Add ViewModel classes

**Files:**
- Modify: `Models/CertificationManagementViewModel.cs`

- [ ] **Step 1: Add SertifikatGroupRow and SertifikatGroupViewModel**

Add these classes after the existing `CertificationManagementViewModel` class (before `RenewalGroup`):

```csharp
// ============================================================
// Grouped Sertifikat View
// ============================================================

public class SertifikatGroupRow
{
    public string Judul { get; set; } = "";
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }
    public int JumlahWorker { get; set; }
}

public class SertifikatGroupViewModel
{
    public List<SertifikatGroupRow> Groups { get; set; } = new();
    public int TotalCount { get; set; }
    public int MandatoryCount { get; set; }
    public int NonMandatoryCount { get; set; }
    public int OjtCount { get; set; }
    public int IhtCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 20;
    public int RoleLevel { get; set; }
}
```

- [ ] **Step 2: Add DetailViewModel for the worker detail page**

Add after `SertifikatGroupViewModel`:

```csharp
public class SertifikatDetailViewModel
{
    public string Judul { get; set; } = "";
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }
    public List<SertifikatRow> Rows { get; set; } = new();
    public int TotalCount { get; set; }
    public int AktifCount { get; set; }
    public int AkanExpiredCount { get; set; }
    public int ExpiredCount { get; set; }
    public int PermanentCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 20;
    public int RoleLevel { get; set; }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build --no-restore`
Expected: Build succeeded, 0 errors

- [ ] **Step 4: Commit**

```bash
git add Models/CertificationManagementViewModel.cs
git commit -m "feat(cert-mgmt): add SertifikatGroupRow, SertifikatGroupViewModel, SertifikatDetailViewModel"
```

---

### Task 2: Add controller actions

**Files:**
- Modify: `Controllers/CMPController.cs`

- [ ] **Step 1: Refactor CertificationManagement() to return grouped view**

Replace the existing `CertificationManagement` method (lines ~3097-3134) with:

```csharp
public async Task<IActionResult> CertificationManagement(int page = 1)
{
    var (allRows, roleLevel) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);

    var groups = allRows
        .GroupBy(r => r.Judul)
        .Select(g => new SertifikatGroupRow
        {
            Judul = g.Key,
            Kategori = g.First().Kategori,
            SubKategori = g.First().SubKategori,
            JumlahWorker = g.Select(r => r.WorkerId).Distinct().Count()
        })
        .OrderBy(g => g.Judul)
        .ToList();

    var vm = new SertifikatGroupViewModel
    {
        TotalCount = groups.Count,
        MandatoryCount = groups.Count(g => string.Equals(g.Kategori, "Mandatory HSSE Training", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(g.Kategori, "MANDATORY", StringComparison.OrdinalIgnoreCase)),
        NonMandatoryCount = groups.Count(g => string.Equals(g.Kategori, "NON MANDATORY", StringComparison.OrdinalIgnoreCase)),
        OjtCount = groups.Count(g => string.Equals(g.Kategori, "OJT", StringComparison.OrdinalIgnoreCase)),
        IhtCount = groups.Count(g => string.Equals(g.Kategori, "IHT", StringComparison.OrdinalIgnoreCase)),
        RoleLevel = roleLevel
    };

    var paging = PaginationHelper.Calculate(groups.Count, page, vm.PageSize);
    vm.Groups = groups.Skip(paging.Skip).Take(paging.Take).ToList();
    vm.CurrentPage = paging.CurrentPage;
    vm.TotalPages = paging.TotalPages;

    ViewBag.AllCategories = await _context.AssessmentCategories
        .Where(c => c.ParentId == null && c.IsActive)
        .OrderBy(c => c.SortOrder)
        .Select(c => c.Name)
        .ToListAsync();

    return View(vm);
}
```

- [ ] **Step 2: Replace FilterCertificationManagement with grouped filter**

Replace the existing `FilterCertificationManagement` method (lines ~3137-3181) with:

```csharp
[HttpGet]
public async Task<IActionResult> FilterCertificationManagement(
    string? category = null,
    string? subCategory = null,
    string? search = null,
    int page = 1)
{
    var (allRows, roleLevel) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);

    var groups = allRows
        .GroupBy(r => r.Judul)
        .Select(g => new SertifikatGroupRow
        {
            Judul = g.Key,
            Kategori = g.First().Kategori,
            SubKategori = g.First().SubKategori,
            JumlahWorker = g.Select(r => r.WorkerId).Distinct().Count()
        })
        .OrderBy(g => g.Judul)
        .ToList();

    if (!string.IsNullOrEmpty(category))
        groups = groups.Where(g => g.Kategori == category).ToList();
    if (!string.IsNullOrEmpty(subCategory))
        groups = groups.Where(g => g.SubKategori == subCategory).ToList();
    if (!string.IsNullOrEmpty(search))
        groups = groups.Where(g => g.Judul.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

    var vm = new SertifikatGroupViewModel
    {
        TotalCount = groups.Count,
        MandatoryCount = groups.Count(g => string.Equals(g.Kategori, "Mandatory HSSE Training", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(g.Kategori, "MANDATORY", StringComparison.OrdinalIgnoreCase)),
        NonMandatoryCount = groups.Count(g => string.Equals(g.Kategori, "NON MANDATORY", StringComparison.OrdinalIgnoreCase)),
        OjtCount = groups.Count(g => string.Equals(g.Kategori, "OJT", StringComparison.OrdinalIgnoreCase)),
        IhtCount = groups.Count(g => string.Equals(g.Kategori, "IHT", StringComparison.OrdinalIgnoreCase)),
        RoleLevel = roleLevel
    };

    var paging = PaginationHelper.Calculate(groups.Count, page, vm.PageSize);
    vm.Groups = groups.Skip(paging.Skip).Take(paging.Take).ToList();
    vm.CurrentPage = paging.CurrentPage;
    vm.TotalPages = paging.TotalPages;

    return PartialView("Shared/_SertifikatGroupTablePartial", vm);
}
```

- [ ] **Step 3: Add CertificationManagementDetail action**

Add after the `FilterCertificationManagement` method:

```csharp
public async Task<IActionResult> CertificationManagementDetail(string judul, int page = 1)
{
    var (allRows, roleLevel) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);
    var filtered = allRows.Where(r => r.Judul == judul).ToList();

    var first = filtered.FirstOrDefault();
    var vm = new SertifikatDetailViewModel
    {
        Judul = judul,
        Kategori = first?.Kategori,
        SubKategori = first?.SubKategori,
        TotalCount = filtered.Count,
        AktifCount = filtered.Count(r => r.Status == CertificateStatus.Aktif),
        AkanExpiredCount = filtered.Count(r => r.Status == CertificateStatus.AkanExpired),
        ExpiredCount = filtered.Count(r => r.Status == CertificateStatus.Expired),
        PermanentCount = filtered.Count(r => r.Status == CertificateStatus.Permanent),
        RoleLevel = roleLevel
    };

    var paging = PaginationHelper.Calculate(filtered.Count, page, vm.PageSize);
    vm.Rows = filtered.Skip(paging.Skip).Take(paging.Take).ToList();
    vm.CurrentPage = paging.CurrentPage;
    vm.TotalPages = paging.TotalPages;

    var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
    ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDict);
    ViewBag.AllBagian = sectionUnitsDict.Keys.ToList();
    ViewBag.UserBagian = (await GetCurrentUserRoleLevelAsync()).User.Section;

    return View(vm);
}
```

- [ ] **Step 4: Add FilterCertificationManagementDetail action**

```csharp
[HttpGet]
public async Task<IActionResult> FilterCertificationManagementDetail(
    string judul,
    string? bagian = null,
    string? unit = null,
    string? status = null,
    int page = 1)
{
    var (allRows, roleLevel) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);
    var filtered = allRows.Where(r => r.Judul == judul).ToList();

    if (!string.IsNullOrEmpty(bagian))
        filtered = filtered.Where(r => r.Bagian == bagian).ToList();
    if (!string.IsNullOrEmpty(unit))
        filtered = filtered.Where(r => r.Unit == unit).ToList();
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
        filtered = filtered.Where(r => r.Status == st).ToList();

    var pageSize = 20;
    var paging = PaginationHelper.Calculate(filtered.Count, page, pageSize);

    var vm = new CertificationManagementViewModel
    {
        Rows = filtered.Skip(paging.Skip).Take(paging.Take).ToList(),
        TotalCount = filtered.Count,
        AktifCount = filtered.Count(r => r.Status == CertificateStatus.Aktif),
        AkanExpiredCount = filtered.Count(r => r.Status == CertificateStatus.AkanExpired),
        ExpiredCount = filtered.Count(r => r.Status == CertificateStatus.Expired),
        PermanentCount = filtered.Count(r => r.Status == CertificateStatus.Permanent),
        CurrentPage = paging.CurrentPage,
        TotalPages = paging.TotalPages,
        PageSize = pageSize,
        RoleLevel = roleLevel
    };

    return PartialView("Shared/_CertificationManagementTablePartial", vm);
}
```

- [ ] **Step 5: Replace ExportSertifikatExcel with grouped export**

Replace the existing `ExportSertifikatExcel` method with:

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ExportSertifikatExcel(
    string? category = null,
    string? subCategory = null,
    string? search = null)
{
    var (allRows, _) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);

    var groups = allRows
        .GroupBy(r => r.Judul)
        .Select(g => new SertifikatGroupRow
        {
            Judul = g.Key,
            Kategori = g.First().Kategori,
            SubKategori = g.First().SubKategori,
            JumlahWorker = g.Select(r => r.WorkerId).Distinct().Count()
        })
        .OrderBy(g => g.Judul)
        .ToList();

    if (!string.IsNullOrEmpty(category))
        groups = groups.Where(g => g.Kategori == category).ToList();
    if (!string.IsNullOrEmpty(subCategory))
        groups = groups.Where(g => g.SubKategori == subCategory).ToList();
    if (!string.IsNullOrEmpty(search))
        groups = groups.Where(g => g.Judul.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

    using var workbook = new XLWorkbook();
    var ws = ExcelExportHelper.CreateSheet(workbook, "Sertifikat", new[]
    {
        "No", "Nama Sertifikat", "Kategori", "Sub Kategori", "Jumlah Worker"
    });

    for (int i = 0; i < groups.Count; i++)
    {
        var g = groups[i];
        var row = i + 2;
        ws.Cell(row, 1).Value = i + 1;
        ws.Cell(row, 2).Value = g.Judul;
        ws.Cell(row, 3).Value = g.Kategori ?? "";
        ws.Cell(row, 4).Value = g.SubKategori ?? "";
        ws.Cell(row, 5).Value = g.JumlahWorker;
    }

    var fileName = $"Sertifikat_Grouped_{DateTime.Now:yyyy-MM-dd}.xlsx";
    return ExcelExportHelper.ToFileResult(workbook, fileName, this);
}
```

- [ ] **Step 6: Add ExportSertifikatDetailExcel action**

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ExportSertifikatDetailExcel(
    string judul,
    string? bagian = null,
    string? unit = null,
    string? status = null)
{
    var (allRows, _) = await BuildSertifikatRowsAsync(l5OwnDataOnly: true);
    var filtered = allRows.Where(r => r.Judul == judul).ToList();

    if (!string.IsNullOrEmpty(bagian))
        filtered = filtered.Where(r => r.Bagian == bagian).ToList();
    if (!string.IsNullOrEmpty(unit))
        filtered = filtered.Where(r => r.Unit == unit).ToList();
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
        filtered = filtered.Where(r => r.Status == st).ToList();

    using var workbook = new XLWorkbook();
    var ws = ExcelExportHelper.CreateSheet(workbook, "Detail", new[]
    {
        "No", "Nama Worker", "Bagian", "Unit", "Tipe", "Status",
        "Valid Until", "Nomor Sertifikat", "Sertifikat URL"
    });

    for (int i = 0; i < filtered.Count; i++)
    {
        var r = filtered[i];
        var row = i + 2;
        ws.Cell(row, 1).Value = i + 1;
        ws.Cell(row, 2).Value = r.NamaWorker;
        ws.Cell(row, 3).Value = r.Bagian ?? "";
        ws.Cell(row, 4).Value = r.Unit ?? "";
        ws.Cell(row, 5).Value = r.RecordType.ToString();
        ws.Cell(row, 6).Value = r.Status.ToString();
        ws.Cell(row, 7).Value = r.ValidUntil?.ToString("dd MMM yyyy") ?? "";
        ws.Cell(row, 8).Value = r.NomorSertifikat ?? "";
        ws.Cell(row, 9).Value = r.SertifikatUrl ?? "";
    }

    var safeJudul = string.Join("_", judul.Split(Path.GetInvalidFileNameChars()));
    var fileName = $"Sertifikat_{safeJudul}_{DateTime.Now:yyyy-MM-dd}.xlsx";
    return ExcelExportHelper.ToFileResult(workbook, fileName, this);
}
```

- [ ] **Step 7: Verify build**

Run: `dotnet build --no-restore`
Expected: Build succeeded, 0 errors

- [ ] **Step 8: Commit**

```bash
git add Controllers/CMPController.cs
git commit -m "feat(cert-mgmt): refactor controller to grouped sertifikat + detail worker actions"
```

---

### Task 3: Create grouped table partial view

**Files:**
- Create: `Views/CMP/Shared/_SertifikatGroupTablePartial.cshtml`

- [ ] **Step 1: Create the partial view**

```html
@model HcPortal.Models.SertifikatGroupViewModel

<div id="cert-table-content"
     data-total="@Model.TotalCount"
     data-mandatory="@Model.MandatoryCount"
     data-non-mandatory="@Model.NonMandatoryCount"
     data-ojt="@Model.OjtCount"
     data-iht="@Model.IhtCount">

    <div class="card-body p-0">
    <div class="table-responsive">
        <table class="table table-striped table-hover mb-0">
            <thead class="table-light">
                <tr>
                    <th class="ps-3">No</th>
                    <th>Nama Sertifikat</th>
                    <th>Kategori</th>
                    <th>Sub Kategori</th>
                    <th class="text-center">Jumlah Worker</th>
                    <th>Aksi</th>
                </tr>
            </thead>
            <tbody>
                @if (Model.Groups.Count == 0)
                {
                    <tr>
                        <td colspan="6" class="text-center text-muted py-4">Belum ada data sertifikat</td>
                    </tr>
                }
                else
                {
                    @for (int i = 0; i < Model.Groups.Count; i++)
                    {
                        var g = Model.Groups[i];
                        var nomor = (Model.CurrentPage - 1) * Model.PageSize + i + 1;
                        <tr>
                            <td class="ps-3">@nomor</td>
                            <td>@g.Judul</td>
                            <td>@(g.Kategori ?? "-")</td>
                            <td>@(g.SubKategori ?? "-")</td>
                            <td class="text-center">
                                <span class="badge bg-primary rounded-pill">@g.JumlahWorker</span>
                            </td>
                            <td>
                                <a href="@Url.Action("CertificationManagementDetail", "CMP", new { judul = g.Judul })"
                                   class="btn btn-sm btn-outline-primary">
                                    <i class="bi bi-eye me-1"></i>Detail
                                </a>
                            </td>
                        </tr>
                    }
                }
            </tbody>
        </table>
    </div>
    </div>

    @if (Model.TotalPages > 1)
    {
        <nav class="mt-4" aria-label="Pagination">
            <ul class="pagination justify-content-center">
                <li class="page-item @(Model.CurrentPage == 1 ? "disabled" : "")">
                    <a href="#" class="page-link" data-page="@(Model.CurrentPage - 1)">
                        <i class="bi bi-chevron-left"></i>
                    </a>
                </li>
                @for (int i = 1; i <= Model.TotalPages; i++)
                {
                    <li class="page-item @(i == Model.CurrentPage ? "active" : "")">
                        <a href="#" class="page-link" data-page="@i">@i</a>
                    </li>
                }
                <li class="page-item @(Model.CurrentPage == Model.TotalPages ? "disabled" : "")">
                    <a href="#" class="page-link" data-page="@(Model.CurrentPage + 1)">
                        <i class="bi bi-chevron-right"></i>
                    </a>
                </li>
            </ul>
        </nav>
    }

    @if (Model.TotalCount > 0)
    {
        var from = (Model.CurrentPage - 1) * Model.PageSize + 1;
        var to = Math.Min(Model.CurrentPage * Model.PageSize, Model.TotalCount);
        <p class="text-center text-muted small mt-2">
            Menampilkan @from - @to dari @Model.TotalCount sertifikat
        </p>
    }
</div>
```

- [ ] **Step 2: Commit**

```bash
git add Views/CMP/Shared/_SertifikatGroupTablePartial.cshtml
git commit -m "feat(cert-mgmt): add grouped sertifikat table partial view"
```

---

### Task 4: Rewrite CertificationManagement main view

**Files:**
- Rewrite: `Views/CMP/CertificationManagement.cshtml`

- [ ] **Step 1: Replace entire file content**

```html
@model HcPortal.Models.SertifikatGroupViewModel
@{
    ViewData["Title"] = "Certification Management";
}

<div class="container-fluid py-4">

    <!-- Breadcrumb -->
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-action="Index" asp-controller="CMP">CMP</a></li>
            <li class="breadcrumb-item active">Certification Management</li>
        </ol>
    </nav>

    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h2 class="mb-1">
                <i class="bi bi-patch-check me-2 text-success"></i>Certification Management
            </h2>
            <p class="text-muted mb-0">Daftar sertifikat pelatihan dan asesmen</p>
        </div>
        <div class="d-flex gap-2">
            @if (User.IsInRole("Admin") || User.IsInRole("HC"))
            {
                <a id="btn-export" href="#" class="btn btn-outline-success" onclick="exportExcel(event)">
                    <i class="bi bi-file-earmark-excel me-1"></i>Export Excel
                </a>
            }
            <a asp-controller="CMP" asp-action="Index" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left me-1"></i>Kembali ke CMP
            </a>
        </div>
    </div>

    <!-- Summary Cards -->
    <div class="row g-3 mb-4">
        <div class="col-6 col-md">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-total" class="fs-2 fw-bold text-primary">@Model.TotalCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-award me-1"></i>Total Sertifikat
                    </div>
                </div>
            </div>
        </div>
        <div class="col-6 col-md">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-mandatory" class="fs-2 fw-bold text-danger">@Model.MandatoryCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-shield-check me-1"></i>Mandatory
                    </div>
                </div>
            </div>
        </div>
        <div class="col-6 col-md">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-non-mandatory" class="fs-2 fw-bold text-success">@Model.NonMandatoryCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-bookmark-check me-1"></i>Non Mandatory
                    </div>
                </div>
            </div>
        </div>
        <div class="col-6 col-md">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-ojt" class="fs-2 fw-bold text-warning">@Model.OjtCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-person-workspace me-1"></i>OJT
                    </div>
                </div>
            </div>
        </div>
        <div class="col-6 col-md">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-iht" class="fs-2 fw-bold text-info">@Model.IhtCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-building me-1"></i>IHT
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Filter Bar -->
    <div class="card border-0 shadow-sm mb-4">
        <div class="card-body">
            <div class="row g-2 align-items-end">
                <div class="col-md-3">
                    <label class="form-label small mb-1">Kategori</label>
                    <select id="filter-category" class="form-select form-select-sm">
                        <option value="">Semua Kategori</option>
                        @foreach (var cat in (List<string>)ViewBag.AllCategories)
                        {
                            <option value="@cat">@cat</option>
                        }
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small mb-1">Sub Kategori</label>
                    <select id="filter-subcategory" class="form-select form-select-sm" disabled>
                        <option value="">Semua Sub Kategori</option>
                    </select>
                </div>
                <div class="col-md-4">
                    <label class="form-label small mb-1">Cari Sertifikat</label>
                    <input type="text" id="filter-search" class="form-control form-control-sm" placeholder="Ketik nama sertifikat...">
                </div>
                <div class="col-md-2">
                    <button id="btn-reset" class="btn btn-outline-secondary btn-sm w-100">Reset</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Table Container -->
    <div id="cert-table-container" class="card border-0 shadow-sm">
        @await Html.PartialAsync("Shared/_SertifikatGroupTablePartial", Model)
    </div>

</div>

<style>
    .dashboard-loading { opacity: 0.5; pointer-events: none; position: relative; }
    .dashboard-loading::after {
        content: '';
        position: absolute; top: 50%; left: 50%;
        width: 2rem; height: 2rem;
        margin: -1rem 0 0 -1rem;
        border: 3px solid var(--bs-border-color); border-top-color: var(--bs-primary);
        border-radius: 50%; animation: spin 0.6s linear infinite;
    }
    @@keyframes spin { to { transform: rotate(360deg); } }
</style>

<script>
(function () {
    var categoryEl = document.getElementById('filter-category');
    var subCategoryEl = document.getElementById('filter-subcategory');
    var searchEl = document.getElementById('filter-search');
    var resetBtn = document.getElementById('btn-reset');
    var container = document.getElementById('cert-table-container');
    var certAbort = null;
    var searchTimeout = null;

    // Cascade: Category -> Sub-Category
    categoryEl.addEventListener('change', function () {
        var cat = categoryEl.value;
        subCategoryEl.innerHTML = '<option value="">Semua Sub Kategori</option>';
        subCategoryEl.disabled = true;
        if (!cat) { refreshTable(); return; }
        fetch(appUrl('/CMP/GetSubCategories?category=' + encodeURIComponent(cat)))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                data.forEach(function (sc) {
                    var opt = document.createElement('option');
                    opt.value = sc; opt.textContent = sc;
                    subCategoryEl.appendChild(opt);
                });
                if (data.length > 0) subCategoryEl.disabled = false;
                refreshTable();
            });
    });
    subCategoryEl.addEventListener('change', refreshTable);

    // Search with debounce
    searchEl.addEventListener('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(refreshTable, 400);
    });

    // Reset
    resetBtn.addEventListener('click', function () {
        categoryEl.value = '';
        subCategoryEl.innerHTML = '<option value="">Semua Sub Kategori</option>';
        subCategoryEl.disabled = true;
        searchEl.value = '';
        refreshTable();
    });

    function refreshTable(page) {
        if (typeof page !== 'number') page = 1;
        container.classList.add('dashboard-loading');

        var params = new URLSearchParams();
        params.set('page', page.toString());
        if (categoryEl.value) params.set('category', categoryEl.value);
        if (subCategoryEl.value) params.set('subCategory', subCategoryEl.value);
        if (searchEl.value.trim()) params.set('search', searchEl.value.trim());

        if (certAbort) certAbort.abort();
        certAbort = new AbortController();

        fetch(appUrl('/CMP/FilterCertificationManagement?' + params), { signal: certAbort.signal })
            .then(function (resp) { return resp.text(); })
            .then(function (html) {
                container.innerHTML = html;
                wirePagination();
                updateSummaryCards();
                container.classList.remove('dashboard-loading');
            })
            .catch(function (e) {
                if (e.name !== 'AbortError') {
                    console.error(e);
                    container.classList.remove('dashboard-loading');
                }
            });
    }

    function wirePagination() {
        container.querySelectorAll('a[data-page]').forEach(function (link) {
            link.addEventListener('click', function (e) {
                e.preventDefault();
                var p = parseInt(this.dataset.page);
                if (p >= 1) refreshTable(p);
            });
        });
    }

    function updateSummaryCards() {
        var content = container.querySelector('#cert-table-content');
        if (!content) return;
        var el;
        el = document.getElementById('count-total');
        if (el) el.textContent = content.dataset.total;
        el = document.getElementById('count-mandatory');
        if (el) el.textContent = content.dataset.mandatory;
        el = document.getElementById('count-non-mandatory');
        if (el) el.textContent = content.dataset.nonMandatory;
        el = document.getElementById('count-ojt');
        if (el) el.textContent = content.dataset.ojt;
        el = document.getElementById('count-iht');
        if (el) el.textContent = content.dataset.iht;
    }

    wirePagination();

    function exportExcel(e) {
        e.preventDefault();
        var params = new URLSearchParams();
        if (categoryEl.value) params.set('category', categoryEl.value);
        if (subCategoryEl.value) params.set('subCategory', subCategoryEl.value);
        if (searchEl.value.trim()) params.set('search', searchEl.value.trim());
        window.location.href = appUrl('/CMP/ExportSertifikatExcel?' + params.toString());
    }
    window.exportExcel = exportExcel;
})();
</script>
```

- [ ] **Step 2: Verify build**

Run: `dotnet build --no-restore`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Commit**

```bash
git add Views/CMP/CertificationManagement.cshtml
git commit -m "feat(cert-mgmt): rewrite main view to show grouped sertifikat list"
```

---

### Task 5: Create detail worker view

**Files:**
- Create: `Views/CMP/CertificationManagementDetail.cshtml`

- [ ] **Step 1: Create the detail view**

```html
@model HcPortal.Models.SertifikatDetailViewModel
@{
    ViewData["Title"] = "Detail Sertifikat — " + Model.Judul;
}

<div class="container-fluid py-4">

    <!-- Breadcrumb -->
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-action="Index" asp-controller="CMP">CMP</a></li>
            <li class="breadcrumb-item"><a asp-action="CertificationManagement" asp-controller="CMP">Certification Management</a></li>
            <li class="breadcrumb-item active">@Model.Judul</li>
        </ol>
    </nav>

    <!-- Header -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h2 class="mb-1">
                <i class="bi bi-patch-check me-2 text-success"></i>@Model.Judul
            </h2>
            <p class="text-muted mb-0">
                @(Model.Kategori ?? "-")
                @if (!string.IsNullOrEmpty(Model.SubKategori))
                {
                    <span> / @Model.SubKategori</span>
                }
            </p>
        </div>
        <div class="d-flex gap-2">
            @if (User.IsInRole("Admin") || User.IsInRole("HC"))
            {
                <a id="btn-export" href="#" class="btn btn-outline-success" onclick="exportExcel(event)">
                    <i class="bi bi-file-earmark-excel me-1"></i>Export Excel
                </a>
            }
            <a asp-controller="CMP" asp-action="CertificationManagement" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left me-1"></i>Kembali
            </a>
        </div>
    </div>

    <!-- Summary Cards -->
    <div class="row g-3 mb-4">
        <div class="col-6 col-md-3">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-total" class="fs-2 fw-bold text-primary">@Model.TotalCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-people me-1"></i>Total Worker
                    </div>
                </div>
            </div>
        </div>
        <div class="col-6 col-md-3">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-aktif" class="fs-2 fw-bold text-success">@Model.AktifCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-check-circle me-1"></i>Aktif
                    </div>
                </div>
            </div>
        </div>
        <div class="col-6 col-md-3">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-akan-expired" class="fs-2 fw-bold text-warning">@Model.AkanExpiredCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-exclamation-triangle me-1"></i>Akan Expired
                    </div>
                </div>
            </div>
        </div>
        <div class="col-6 col-md-3">
            <div class="card border-0 shadow-sm text-center">
                <div class="card-body">
                    <div id="count-expired" class="fs-2 fw-bold text-danger">@Model.ExpiredCount</div>
                    <div class="small text-muted mt-1">
                        <i class="bi bi-x-circle me-1"></i>Expired
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Filter Bar -->
    <div class="card border-0 shadow-sm mb-4">
        <div class="card-body">
            <div class="row g-2 align-items-end">
                <div class="col-md-3">
                    <label class="form-label small mb-1">Bagian</label>
                    <select id="filter-bagian" class="form-select form-select-sm" @(Model.RoleLevel >= 4 ? "disabled" : "")>
                        @if (Model.RoleLevel == 4)
                        {
                            <option value="@ViewBag.UserBagian" selected>@ViewBag.UserBagian</option>
                        }
                        else if (Model.RoleLevel <= 3)
                        {
                            <option value="">Semua Bagian</option>
                            @foreach (var s in (List<string>)ViewBag.AllBagian)
                            {
                                <option value="@s">@s</option>
                            }
                        }
                        else
                        {
                            <option value="">Semua Bagian</option>
                        }
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small mb-1">Unit</label>
                    <select id="filter-unit" class="form-select form-select-sm" @(Model.RoleLevel >= 5 ? "disabled" : "") disabled>
                        <option value="">Semua Unit</option>
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small mb-1">Status</label>
                    <select id="filter-status" class="form-select form-select-sm">
                        <option value="">Semua Status</option>
                        <option value="Aktif">Aktif</option>
                        <option value="AkanExpired">Akan Expired</option>
                        <option value="Expired">Expired</option>
                        <option value="Permanent">Permanent</option>
                    </select>
                </div>
                <div class="col-md-3">
                    <button id="btn-reset" class="btn btn-outline-secondary btn-sm w-100">Reset</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Table Container -->
    <div id="cert-table-container" class="card border-0 shadow-sm">
        @await Html.PartialAsync("Shared/_CertificationManagementTablePartial", new HcPortal.Models.CertificationManagementViewModel
        {
            Rows = Model.Rows,
            TotalCount = Model.TotalCount,
            AktifCount = Model.AktifCount,
            AkanExpiredCount = Model.AkanExpiredCount,
            ExpiredCount = Model.ExpiredCount,
            PermanentCount = Model.PermanentCount,
            CurrentPage = Model.CurrentPage,
            TotalPages = Model.TotalPages,
            PageSize = Model.PageSize,
            RoleLevel = Model.RoleLevel
        })
    </div>

</div>

<style>
    .dashboard-loading { opacity: 0.5; pointer-events: none; position: relative; }
    .dashboard-loading::after {
        content: '';
        position: absolute; top: 50%; left: 50%;
        width: 2rem; height: 2rem;
        margin: -1rem 0 0 -1rem;
        border: 3px solid var(--bs-border-color); border-top-color: var(--bs-primary);
        border-radius: 50%; animation: spin 0.6s linear infinite;
    }
    @@keyframes spin { to { transform: rotate(360deg); } }
</style>

<script>
(function () {
    var judul = '@Html.Raw(Model.Judul.Replace("'", "\\'"))';
    var bagianEl = document.getElementById('filter-bagian');
    var unitEl = document.getElementById('filter-unit');
    var statusEl = document.getElementById('filter-status');
    var resetBtn = document.getElementById('btn-reset');
    var container = document.getElementById('cert-table-container');
    var certAbort = null;
    var roleLevel = @Model.RoleLevel;

    // Cascade: Bagian -> Unit
    bagianEl.addEventListener('change', function () {
        var section = bagianEl.value;
        unitEl.innerHTML = '<option value="">Semua Unit</option>';
        if (!section) {
            unitEl.disabled = true;
            refreshTable();
            return;
        }
        fetch(appUrl('/CMP/GetCascadeOptions?section=' + encodeURIComponent(section)))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                data.units.forEach(function (u) {
                    var opt = document.createElement('option');
                    opt.value = u; opt.textContent = u;
                    unitEl.appendChild(opt);
                });
                unitEl.disabled = false;
                refreshTable();
            });
    });

    unitEl.addEventListener('change', refreshTable);
    statusEl.addEventListener('change', refreshTable);

    // Reset
    resetBtn.addEventListener('click', function () {
        if (roleLevel <= 3) {
            bagianEl.value = '';
            unitEl.innerHTML = '<option value="">Semua Unit</option>';
            unitEl.disabled = true;
        }
        if (roleLevel === 4) {
            unitEl.value = '';
        }
        statusEl.value = '';
        refreshTable();
    });

    function refreshTable(page) {
        if (typeof page !== 'number') page = 1;
        container.classList.add('dashboard-loading');

        var params = new URLSearchParams();
        params.set('judul', judul);
        params.set('page', page.toString());
        if (bagianEl.value) params.set('bagian', bagianEl.value);
        if (unitEl.value) params.set('unit', unitEl.value);
        if (statusEl.value) params.set('status', statusEl.value);

        if (certAbort) certAbort.abort();
        certAbort = new AbortController();

        fetch(appUrl('/CMP/FilterCertificationManagementDetail?' + params), { signal: certAbort.signal })
            .then(function (resp) { return resp.text(); })
            .then(function (html) {
                container.innerHTML = html;
                wirePagination();
                updateSummaryCards();
                container.classList.remove('dashboard-loading');
            })
            .catch(function (e) {
                if (e.name !== 'AbortError') {
                    console.error(e);
                    container.classList.remove('dashboard-loading');
                }
            });
    }

    function wirePagination() {
        container.querySelectorAll('a[data-page]').forEach(function (link) {
            link.addEventListener('click', function (e) {
                e.preventDefault();
                var p = parseInt(this.dataset.page);
                if (p >= 1) refreshTable(p);
            });
        });
    }

    function updateSummaryCards() {
        var content = container.querySelector('#cert-table-content');
        if (!content) return;
        var el;
        el = document.getElementById('count-total');
        if (el) el.textContent = content.dataset.total;
        el = document.getElementById('count-aktif');
        if (el) el.textContent = content.dataset.aktif;
        el = document.getElementById('count-akan-expired');
        if (el) el.textContent = content.dataset.akanExpired;
        el = document.getElementById('count-expired');
        if (el) el.textContent = content.dataset.expired;
    }

    wirePagination();

    // L4: auto-load units
    if (roleLevel === 4 && bagianEl.value) {
        fetch(appUrl('/CMP/GetCascadeOptions?section=' + encodeURIComponent(bagianEl.value)))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                data.units.forEach(function (u) {
                    var opt = document.createElement('option');
                    opt.value = u; opt.textContent = u;
                    unitEl.appendChild(opt);
                });
                unitEl.disabled = false;
            });
    }

    if (roleLevel >= 5) {
        bagianEl.disabled = true;
        unitEl.disabled = true;
    }

    function exportExcel(e) {
        e.preventDefault();
        var params = new URLSearchParams();
        params.set('judul', judul);
        if (bagianEl.value) params.set('bagian', bagianEl.value);
        if (unitEl.value) params.set('unit', unitEl.value);
        if (statusEl.value) params.set('status', statusEl.value);
        window.location.href = appUrl('/CMP/ExportSertifikatDetailExcel?' + params.toString());
    }
    window.exportExcel = exportExcel;
})();
</script>
```

- [ ] **Step 2: Commit**

```bash
git add Views/CMP/CertificationManagementDetail.cshtml
git commit -m "feat(cert-mgmt): add detail worker view per sertifikat"
```

---

### Task 6: Update existing table partial for detail reuse

**Files:**
- Modify: `Views/CMP/Shared/_CertificationManagementTablePartial.cshtml`

- [ ] **Step 1: Update the partial to always show worker columns and remove Judul column**

The partial is reused for the detail page where Judul is already shown in the header. Remove the Judul column and always show Nama/Bagian/Unit (no RoleLevel gate since the detail page always needs them).

Replace the `<thead>` and column rendering. The key changes:
- Remove `@if (Model.RoleLevel <= 4)` guard around Nama/Bagian/Unit columns — always show them
- Remove the Judul column entirely
- Update `colCount` accordingly

Replace the entire `<thead>` block:

```html
<thead class="table-light">
    <tr>
        <th class="ps-3">No</th>
        <th>Nama</th>
        <th class="d-none d-md-table-cell">Bagian</th>
        <th class="d-none d-md-table-cell">Unit</th>
        <th class="d-none d-md-table-cell">Kategori</th>
        <th class="d-none d-md-table-cell">Sub Kategori</th>
        <th>Nomor Sertifikat</th>
        <th>Tgl Terbit</th>
        <th>Valid Until</th>
        <th>Tipe</th>
        <th>Status</th>
        <th>Aksi</th>
    </tr>
</thead>
```

Replace `var colCount = Model.RoleLevel <= 4 ? 13 : 10;` with:

```csharp
var colCount = 12;
```

Remove the `@if (Model.RoleLevel <= 4)` guard around the `<td>` cells for Nama, Bagian, Unit — always render them. Also remove the Judul `<td>`.

The `<td>` cells for each row should be:

```html
<td class="ps-3">@nomor</td>
<td>@row.NamaWorker</td>
<td class="d-none d-md-table-cell">@(row.Bagian ?? "-")</td>
<td class="d-none d-md-table-cell">@(row.Unit ?? "-")</td>
<td class="d-none d-md-table-cell">@(row.Kategori ?? "-")</td>
<td class="d-none d-md-table-cell">@(row.SubKategori ?? "-")</td>
<td>@(row.NomorSertifikat ?? "-")</td>
<td>@(row.TanggalTerbit?.ToString("dd MMM yyyy") ?? "-")</td>
<td>@(row.ValidUntil?.ToString("dd MMM yyyy") ?? "-")</td>
```

(Keep existing Tipe, Status, and Aksi `<td>` cells unchanged.)

- [ ] **Step 2: Verify build**

Run: `dotnet build --no-restore`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Commit**

```bash
git add Views/CMP/Shared/_CertificationManagementTablePartial.cshtml
git commit -m "feat(cert-mgmt): update table partial - always show worker cols, remove Judul"
```

---

### Task 7: Smoke test

- [ ] **Step 1: Build the project**

Run: `dotnet build`
Expected: Build succeeded, 0 errors

- [ ] **Step 2: Run the app and test manually**

Run: `dotnet run`

Test checklist:
1. Navigate to `/CMP/CertificationManagement` — should show grouped sertifikat table
2. Verify summary cards show counts per kategori
3. Filter by Kategori — table updates via AJAX
4. Filter by Sub Kategori (cascade) — table updates
5. Search by nama sertifikat — table updates with debounce
6. Click "Detail" on a sertifikat — navigates to detail page
7. Detail page shows worker list with summary cards (Total, Aktif, Akan Expired, Expired)
8. Filter by Bagian/Unit/Status on detail page — works
9. Pagination works on both pages
10. Export Excel works on both pages
11. "Kembali" button on detail page goes back to list

- [ ] **Step 3: Final commit if any fixes needed**
