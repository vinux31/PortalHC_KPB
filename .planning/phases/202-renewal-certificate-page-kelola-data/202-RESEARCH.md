# Phase 202: Renewal Certificate Page (Kelola Data) - Research

**Researched:** 2026-03-19
**Domain:** ASP.NET Core MVC — Admin halaman daftar sertifikat expired/akan expired dengan AJAX filter dan bulk renew
**Confidence:** HIGH

## Summary

Phase 202 membangun halaman Renewal Sertifikat di Kelola Data (AdminController) yang menampilkan semua sertifikat dengan status Expired atau AkanExpired yang belum di-renew (`IsRenewed == false`). Halaman mengadopsi pola AJAX filter dari `CertificationManagement` di CDPController secara penuh — partial view untuk refresh tabel, cascade Bagian→Unit, dan PaginationHelper yang sudah ada. Satu-satunya fitur baru adalah kolom checkbox dengan logika bulk-select per kategori dan tombol Renew yang membangun query string multi-param untuk CreateAssessment.

Data source utama adalah `BuildSertifikatRowsAsync` yang sudah ada di CDPController — akan di-copy/adapt ke AdminController sebagai `BuildRenewalRowsAsync` dengan filter tambahan: `Status in (Expired, AkanExpired)` dan `IsRenewed == false`. Halaman ini adalah Admin/HC only (role level 1-3 setara L1-L4 di CDP, tapi tanpa role-level scoping — Admin dan HC selalu full access).

**Primary recommendation:** Copy `BuildSertifikatRowsAsync` ke AdminController, strip role-scoping (Admin/HC selalu full access), tambahkan filter Expired+AkanExpired+!IsRenewed, lalu buat halaman dengan pola yang identik dengan CertificationManagement.

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions

**Tampilan Daftar**
- Kolom tabel ringkas: Nama, Judul Sertifikat, Kategori, Sub Kategori, Valid Until, Status, Aksi
- Sorting default: Expired di atas, lalu Akan Expired. Dalam grup status sama, sort by ValidUntil ascending (paling dekat expired di atas)
- Pagination 20 baris per halaman (konsisten dengan CertificationManagement)
- Badge count summary di atas tabel: Expired (N), Akan Expired (N)

**Filter**
- 4 dropdown filter: Bagian, Unit, Kategori, Status (Expired/Akan Expired)
- Filter menggunakan AJAX (tabel update tanpa reload halaman), konsisten dengan CertificationManagement
- Default state: semua filter kosong = tampilkan semua sertifikat expired/akan expired

**Bulk Select & Renew**
- Checkbox per baris. Setelah checkbox pertama dicentang, checkbox sertifikat dengan kategori berbeda otomatis disabled + tooltip "Hanya sertifikat kategori sama"
- Tombol "Renew Selected" muncul/aktif di atas tabel setelah minimal 1 checkbox dicentang
- Klik Renew Selected langsung redirect ke CreateAssessment tanpa confirm dialog
- Passing data via query string multi-param: `?renewSessionId=1&renewSessionId=2&...` atau campuran renewSessionId+renewTrainingId

**Card di Kelola Data**
- Card "Renewal Sertifikat" di Section C dengan badge count (jumlah total sertifikat perlu renew)
- Icon bi-arrow-repeat, warna text-warning (orange)
- Role: Admin dan HC

**Navigasi**
- Breadcrumb: Kelola Data > Renewal Sertifikat (tanpa tombol kembali eksplisit)

### Claude's Discretion
- Empty state halaman jika tidak ada sertifikat perlu renew
- Exact styling badge status (Expired vs Akan Expired)
- Apakah Sub Kategori juga perlu filter dropdown atau cukup di kolom tabel saja
- Handling jika sertifikat dari TrainingRecord (tidak punya Kategori/SubKategori)

### Deferred Ideas (OUT OF SCOPE)
Tidak ada — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| RNPAGE-01 | HC/Admin melihat daftar sertifikat Expired dan Akan Expired yang belum di-renew | BuildRenewalRowsAsync: filter `IsRenewed==false` + `Status in (Expired, AkanExpired)` dari data BuildSertifikatRowsAsync yang sudah ada |
| RNPAGE-02 | HC/Admin dapat filter berdasarkan Bagian, Unit, Kategori | Pola identik dengan FilterCertificationManagement (CDPController) — in-memory filter setelah query |
| RNPAGE-03 | Klik Renew pada satu sertifikat → redirect ke CreateAssessment dengan data pre-filled | AdminController.CreateAssessment sudah menerima `renewSessionId` / `renewTrainingId` query param (Phase 201) |
| RNPAGE-04 | Checkbox bulk select + Renew Selected untuk sertifikat dengan kategori sama | JavaScript di partial view: track selected kategori, disable checkbox beda kategori, build multi-param URL |
| RNPAGE-05 | Card Renewal Certificate di Kelola Data Section C | Tambahkan card di `Views/Admin/Index.cshtml` setelah card terakhir Section C |
</phase_requirements>

## Standard Stack

### Core
| Library/Pattern | Purpose | Why Standard |
|----------------|---------|--------------|
| ASP.NET Core MVC partial view | AJAX table refresh | Sudah dipakai di CertificationManagement — proven pattern |
| `PaginationHelper.Calculate()` | Hitung skip/take/totalPages | Sudah ada di `Helpers/PaginationHelper.cs` |
| `CertificationManagementViewModel` | ViewModel tabel dengan counts + pagination | Sudah ada, field `ExpiredCount` dan `AkanExpiredCount` relevan langsung |
| `SertifikatRow` | Flat row model untuk data gabungan Training+Assessment | Sudah ada dengan `IsRenewed`, `Status`, `Kategori`, `SourceId`, `RecordType` |
| Bootstrap 5 + Bootstrap Icons | UI components, badge, icon | Standar project |
| jQuery fetch / AbortController | AJAX filter tanpa reload halaman | Pola yang sudah ada di CertificationManagement.cshtml |

### Supporting
| Library | Purpose | When to Use |
|---------|---------|-------------|
| `OrganizationStructure.GetAllSections()` | Populate dropdown Bagian | Dipakai di AdminController ViewBag pattern |
| `/CDP/GetCascadeOptions?section=` | Populate dropdown Unit via AJAX | Endpoint CDPController yang sudah ada — dipakai langsung dari view JS |
| `/CDP/GetSubCategories?category=` | Populate Sub Kategori via AJAX | Jika Sub Kategori filter diaktifkan (Claude's discretion) |

**Catatan:** Filter endpoint untuk Renewal halaman ditambahkan ke AdminController, bukan CDPController, karena halaman ada di Admin area.

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
└── AdminController.cs
    ├── RenewalCertificate()          ← GET, load halaman + badge count card
    ├── FilterRenewalCertificate()    ← GET, AJAX partial view refresh
    └── BuildRenewalRowsAsync()       ← Private helper, adapt dari BuildSertifikatRowsAsync

Views/Admin/
├── RenewalCertificate.cshtml         ← Halaman utama (filter bar + table container)
└── Shared/
    └── _RenewalCertificateTablePartial.cshtml  ← Partial view untuk AJAX refresh + checkbox
```

### Pattern 1: Adapt BuildSertifikatRowsAsync untuk Renewal

`BuildSertifikatRowsAsync` di CDPController memiliki role-level scoping (L1-L6). Untuk AdminController, Admin dan HC selalu full access — tidak perlu scoping. Adapt jadi `BuildRenewalRowsAsync`:

```csharp
// AdminController.cs
private async Task<List<SertifikatRow>> BuildRenewalRowsAsync()
{
    // 1. Query TrainingRecords dengan sertifikat
    var trQuery = _context.TrainingRecords
        .Include(t => t.User)
        .Where(t => t.SertifikatUrl != null);

    var trainingAnon = await trQuery
        .Select(t => new { /* sama dengan CDPController */ })
        .ToListAsync();

    // 2. Batch renewal lookup (4 set, identik dengan CDPController)
    var renewedByAsSessionIds = await _context.AssessmentSessions
        .Where(a => a.RenewsSessionId.HasValue && a.IsPassed == true)
        .Select(a => a.RenewsSessionId!.Value).Distinct().ToListAsync();
    // ... (3 set lainnya identik)

    // 3. Map trainingRows + assessmentRows (identik)

    // 4. POST-FILTER: hanya expired/akan expired yang belum di-renew
    var allRows = rows
        .Where(r => !r.IsRenewed &&
                    (r.Status == CertificateStatus.Expired ||
                     r.Status == CertificateStatus.AkanExpired))
        .ToList();

    return allRows;
}
```

### Pattern 2: Sorting Default (Expired dulu, lalu AkanExpired, sort ValidUntil asc)

```csharp
allRows = allRows
    .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)  // Expired di atas
    .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)                 // Paling dekat expired di atas
    .ToList();
```

### Pattern 3: Card di Index.cshtml dengan Badge Count

Card memerlukan count — harus dihitung di `RenewalCertificate` GET action:

```csharp
// AdminController.cs
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> RenewalCertificate(int page = 1)
{
    var allRows = await BuildRenewalRowsAsync();
    // sorting...
    var vm = new CertificationManagementViewModel
    {
        TotalCount = allRows.Count,
        ExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired),
        AkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired),
    };
    // pagination, ViewBag dropdowns...
    return View(vm);
}
```

Untuk card badge di `Index.cshtml`, AdminController `Index` GET perlu query tambahan:

```csharp
// Di AdminController.Index() — tambahkan ViewBag.RenewalCount
var renewalCount = await GetRenewalCount(); // atau inline query
ViewBag.RenewalCount = renewalCount;
```

Atau badge bisa di-render langsung di card dengan AJAX setelah halaman load — tapi pola project adalah server-rendered ViewBag, gunakan server-rendered.

### Pattern 4: AJAX Filter — identik dengan CertificationManagement

```javascript
// RenewalCertificate.cshtml script
function refreshTable(page) {
    var params = new URLSearchParams();
    params.set('page', page || 1);
    if (bagianEl.value) params.set('bagian', bagianEl.value);
    if (unitEl.value) params.set('unit', unitEl.value);
    if (statusEl.value) params.set('status', statusEl.value);
    if (categoryEl.value) params.set('category', categoryEl.value);

    fetch('/Admin/FilterRenewalCertificate?' + params, { signal: certAbort.signal })
        .then(r => r.text())
        .then(html => {
            container.innerHTML = html;
            wirePagination();
            updateSummaryBadges();
        });
}
```

### Pattern 5: Bulk Select dengan Category Lock

```javascript
// Di _RenewalCertificateTablePartial.cshtml atau script inline
var selectedKategori = null;

document.querySelectorAll('.cb-select').forEach(function(cb) {
    cb.addEventListener('change', function() {
        var kategori = this.dataset.kategori;
        if (this.checked) {
            if (!selectedKategori) selectedKategori = kategori;
            // Disable checkbox kategori berbeda
            document.querySelectorAll('.cb-select').forEach(function(other) {
                if (other.dataset.kategori !== selectedKategori) {
                    other.disabled = true;
                    other.title = 'Hanya sertifikat kategori sama';
                }
            });
        } else {
            // Cek apakah masih ada yang tercentang
            var anyChecked = Array.from(document.querySelectorAll('.cb-select:checked')).length > 0;
            if (!anyChecked) {
                selectedKategori = null;
                document.querySelectorAll('.cb-select').forEach(function(other) {
                    other.disabled = false;
                    other.title = '';
                });
            }
        }
        updateRenewSelectedButton();
    });
});

function updateRenewSelectedButton() {
    var checked = document.querySelectorAll('.cb-select:checked');
    var btn = document.getElementById('btn-renew-selected');
    if (btn) btn.classList.toggle('d-none', checked.length === 0);
}

function renewSelected() {
    var checked = document.querySelectorAll('.cb-select:checked');
    var params = new URLSearchParams();
    checked.forEach(function(cb) {
        if (cb.dataset.recordtype === 'Assessment') {
            params.append('renewSessionId', cb.dataset.sourceid);
        } else {
            params.append('renewTrainingId', cb.dataset.sourceid);
        }
    });
    window.location.href = '/Admin/CreateAssessment?' + params.toString();
}
```

Setiap checkbox row perlu atribut: `data-kategori`, `data-sourceid`, `data-recordtype`.

### Pattern 6: Single Renew — URL ke CreateAssessment

```razor
@if (row.RecordType == RecordType.Assessment)
{
    <a href="@Url.Action("CreateAssessment", "Admin", new { renewSessionId = row.SourceId })"
       class="btn btn-sm btn-warning">
        <i class="bi bi-arrow-repeat me-1"></i>Renew
    </a>
}
else
{
    <a href="@Url.Action("CreateAssessment", "Admin", new { renewTrainingId = row.SourceId })"
       class="btn btn-sm btn-warning">
        <i class="bi bi-arrow-repeat me-1"></i>Renew
    </a>
}
```

### Anti-Patterns to Avoid

- **Jangan filter di DB query langsung** — Status (Expired/AkanExpired) adalah derived field dari ValidUntil yang dihitung di aplikasi, bukan kolom DB. Filter harus dilakukan in-memory setelah query, persis seperti CDPController.
- **Jangan buat ViewModel baru** — `CertificationManagementViewModel` sudah punya semua field yang dibutuhkan (`ExpiredCount`, `AkanExpiredCount`, `Rows`, `CurrentPage`, `TotalPages`). Reuse langsung.
- **Jangan duplicate card badge query** — AdminController `Index` GET sudah melakukan beberapa query ViewBag. Tambahkan renewal count di sini, jangan buat endpoint terpisah hanya untuk badge.
- **Jangan gunakan `GetCascadeOptions` dari AdminController** — Endpoint ini ada di CDPController dan sudah diakses via `/CDP/GetCascadeOptions` dari view JS. View Renewal di Admin area boleh memanggil endpoint CDPController yang sama.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Pagination | Custom skip/take logic | `PaginationHelper.Calculate()` | Sudah ada di Helpers, konsisten dengan semua halaman lain |
| Cascade Bagian → Unit | Query ke DB dari view | `/CDP/GetCascadeOptions` endpoint | Sudah ada, return `{ units }` yang langsung dipakai |
| Renewal chain detection | Re-query di BuildRenewalRowsAsync | Copy 4-set batch lookup dari BuildSertifikatRowsAsync | Pattern yang sudah proven, menghindari N+1 query |
| AJAX table refresh | `fetch` + `innerHTML` | Copy pola dari CertificationManagement.cshtml | Pattern sudah dibuktikan bekerja dengan AbortController |

## Common Pitfalls

### Pitfall 1: Checkbox Kategori Kosong (TrainingRecord)
**What goes wrong:** TrainingRecord tidak memiliki Kategori — `row.Kategori == null`. Checkbox dengan `data-kategori=""` akan conflict: semua Training tanpa kategori dianggap "sama kategori".
**Why it happens:** Desain model — TrainingRecord tidak punya hierarchy kategori.
**How to avoid:** Di logika bulk-select JS, treat `null`/empty kategori sebagai kategori tersendiri yang tidak bisa di-bulk dengan apapun. Atau: disable checkbox row TrainingRecord tanpa kategori untuk bulk select (tetap bisa single Renew). Ini termasuk Claude's Discretion.
**Warning signs:** User mencentang 2 TrainingRecord tanpa kategori dan bisa bulk-renew dengan kategori yang sebenarnya berbeda.

### Pitfall 2: Badge Count di Card Index Stale
**What goes wrong:** Badge count di card Kelola Data menampilkan jumlah yang berbeda dengan jumlah aktual di halaman renewal.
**Why it happens:** Index GET tidak menggunakan `BuildRenewalRowsAsync` — query terpisah.
**How to avoid:** Gunakan query minimal di `Index` GET — cukup count sertifikat expired/akan expired belum di-renew. Tidak perlu full BuildRenewalRowsAsync (mahal). Buat helper count query yang ringan.

### Pitfall 3: Multi-param Query String di `Url.Action`
**What goes wrong:** `Url.Action("CreateAssessment", "Admin", new { renewSessionId = ids })` tidak bisa passing array dengan cara ini.
**Why it happens:** `Url.Action` route values tidak support array natively.
**How to avoid:** Build URL multi-param secara manual di JavaScript (`params.append('renewSessionId', id)`) — ini sudah direncanakan di CONTEXT.md dan merupakan approach yang benar.

### Pitfall 4: Partial View tidak bisa akses User.IsInRole
**What goes wrong:** Partial view mencoba cek role tapi User object tidak tersedia.
**Why it happens:** Partial view inherit HttpContext — sebenarnya bisa. Tapi lebih aman pass role info via ViewModel atau ViewBag.
**How to avoid:** Karena halaman ini hanya untuk Admin/HC (full access), tidak perlu role branching di partial view. Selalu tampilkan kolom Nama, Bagian, Unit — berbeda dengan CertificationManagement yang punya L5/L6 scoping.

### Pitfall 5: AJAX partial view kehilangan checkbox state setelah filter
**What goes wrong:** User mencentang beberapa baris lalu mengubah filter → tabel refresh → semua checkbox reset.
**Why it happens:** AJAX replace `innerHTML` container → semua state DOM hilang.
**How to avoid:** Ini expected behavior — filter refresh selalu reset selection. Pastikan `selectedKategori = null` di `refreshTable()` dan tombol "Renew Selected" hilang setelah refresh.

## Code Examples

### FilterRenewalCertificate action (pattern identik dengan FilterCertificationManagement)
```csharp
// Source: CDPController.cs FilterCertificationManagement (adapted)
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> FilterRenewalCertificate(
    string? bagian = null,
    string? unit = null,
    string? status = null,
    string? category = null,
    int page = 1)
{
    var allRows = await BuildRenewalRowsAsync();

    if (!string.IsNullOrEmpty(bagian))
        allRows = allRows.Where(r => r.Bagian == bagian).ToList();
    if (!string.IsNullOrEmpty(unit))
        allRows = allRows.Where(r => r.Unit == unit).ToList();
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
        allRows = allRows.Where(r => r.Status == st).ToList();
    if (!string.IsNullOrEmpty(category))
        allRows = allRows.Where(r => r.Kategori == category).ToList();

    // Sorting: Expired dulu, sort ValidUntil asc
    allRows = allRows
        .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
        .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
        .ToList();

    var vm = new CertificationManagementViewModel
    {
        TotalCount = allRows.Count,
        ExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired),
        AkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired),
    };

    var paging = PaginationHelper.Calculate(allRows.Count, page, vm.PageSize);
    vm.Rows = allRows.Skip(paging.Skip).Take(paging.Take).ToList();
    vm.CurrentPage = paging.CurrentPage;
    vm.TotalPages = paging.TotalPages;

    return PartialView("Shared/_RenewalCertificateTablePartial", vm);
}
```

### Card di Index.cshtml (append ke Section C)
```razor
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-md-4">
    <a href="@Url.Action("RenewalCertificate", "Admin")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-arrow-repeat fs-5 text-warning"></i>
                    <span class="fw-bold">Renewal Sertifikat</span>
                    @if (ViewBag.RenewalCount != null && (int)ViewBag.RenewalCount > 0)
                    {
                        <span class="badge bg-warning text-dark ms-auto">@ViewBag.RenewalCount</span>
                    }
                </div>
                <small class="text-muted">Kelola sertifikat expired dan akan expired yang perlu di-renew</small>
            </div>
        </div>
    </a>
</div>
}
```

### Badge count ringan di AdminController.Index()
```csharp
// Query ringan — tidak perlu full BuildRenewalRowsAsync
var renewedSessionIds = await _context.AssessmentSessions
    .Where(a => a.RenewsSessionId.HasValue && a.IsPassed == true)
    .Select(a => a.RenewsSessionId!.Value).Distinct().ToListAsync();
var renewedTrainingIds = await _context.AssessmentSessions
    .Where(a => a.RenewsTrainingId.HasValue && a.IsPassed == true)
    .Select(a => a.RenewsTrainingId!.Value).Distinct().ToListAsync();

var expiredTrainingCount = await _context.TrainingRecords
    .Where(t => t.SertifikatUrl != null &&
                t.ValidUntil != null &&
                t.CertificateType != "Permanent" &&
                t.ValidUntil < DateTime.Now.AddDays(30) &&
                !renewedTrainingIds.Contains(t.Id))
    .CountAsync();

var expiredAssessmentCount = await _context.AssessmentSessions
    .Where(a => a.GenerateCertificate && a.IsPassed == true &&
                a.ValidUntil != null &&
                a.ValidUntil < DateTime.Now.AddDays(30) &&
                !renewedSessionIds.Contains(a.Id))
    .CountAsync();

ViewBag.RenewalCount = expiredTrainingCount + expiredAssessmentCount;
```

Catatan: Query ini menggunakan `ValidUntil < Now + 30 days` sebagai proxy untuk Expired+AkanExpired. Ini konsisten dengan `DeriveCertificateStatus` threshold 30 hari.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project tidak punya automated test suite) |
| Config file | Tidak ada |
| Quick run command | Run app, navigate ke `/Admin/RenewalCertificate` |
| Full suite command | Manual flow testing per requirement |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Verification |
|--------|----------|-----------|-------------|
| RNPAGE-01 | Hanya tampilkan Expired+AkanExpired yang belum di-renew | Manual | Bandingkan dengan CertificationManagement — pastikan Aktif dan Permanent tidak muncul |
| RNPAGE-02 | Filter Bagian, Unit, Kategori mempersempit daftar | Manual | Pilih filter → tabel update tanpa reload |
| RNPAGE-03 | Klik Renew → CreateAssessment pre-filled | Manual | Verify Title, Kategori, peserta, GenerateCertificate=true terisi |
| RNPAGE-04 | Bulk select hanya kategori sama, Renew Selected → CreateAssessment multi-peserta | Manual | Centang 2 baris, pastikan checkbox kategori lain disabled; klik Renew Selected |
| RNPAGE-05 | Card muncul di Section C dengan badge count | Manual | Navigasi ke Kelola Data, card terlihat, badge count muncul |

## Sources

### Primary (HIGH confidence)
- `Controllers/CDPController.cs` line 3187 — `BuildSertifikatRowsAsync` full implementation, dibaca langsung
- `Controllers/CDPController.cs` line 3082 — `FilterCertificationManagement` AJAX pattern
- `Controllers/AdminController.cs` line 947 — `CreateAssessment` GET dengan renewSessionId/renewTrainingId, dibaca langsung
- `Views/CDP/CertificationManagement.cshtml` — AJAX filter JS pattern lengkap, dibaca langsung
- `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — Partial view pattern, dibaca langsung
- `Models/CertificationManagementViewModel.cs` — SertifikatRow, CertificateStatus enum, DeriveCertificateStatus, dibaca langsung
- `Helpers/PaginationHelper.cs` — PaginationHelper.Calculate signature, dibaca langsung
- `Views/Admin/Index.cshtml` line 126 — Section C struktur card existing, dibaca langsung

### Secondary (MEDIUM confidence)
- `.planning/phases/202-renewal-certificate-page-kelola-data/202-CONTEXT.md` — User decisions dan canonical refs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library/pattern adalah existing code yang dibaca langsung
- Architecture: HIGH — pattern diadaptasi dari kode yang sudah berfungsi
- Pitfalls: HIGH — pitfall bulk-select kategori kosong dan badge count staleness diidentifikasi dari analisis kode aktual

**Research date:** 2026-03-19
**Valid until:** Stable — tidak ada dependency eksternal baru
