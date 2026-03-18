# Phase 188: AJAX Filter Bar - Research

**Researched:** 2026-03-18
**Domain:** ASP.NET Core MVC — AJAX partial view filtering, cascade dropdown, in-memory post-query filtering
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Filter bar diposisikan di atas tabel, dalam card yang sama (konsisten dengan Dashboard Proton)
- Bagian/Unit menggunakan cascade dropdown — reuse GetCascadeOptions pattern dari CDPController
- Search field instant dengan debounce 300ms (keyup)
- Tombol Reset yang clear semua filter + reload data
- Summary cards (Total, Aktif, Akan Expired, Expired) di-update dari filtered dataset — AJAX response include counts
- Pagination reset ke page 1 setiap kali filter berubah
- Loading indicator: opacity 0.5 + spinner pada container tabel (pattern Dashboard)
- URL tidak di-update (no pushState) — filter state hanya di JS

### Claude's Discretion
- Detail implementasi partial view structure
- Exact debounce implementation approach

### Deferred Ideas (OUT OF SCOPE)
Tidak ada deferred ideas dari diskusi.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| FILT-01 | Filter by Bagian/Unit cascade | `GetCascadeOptions` sudah tersedia di CDPController, `OrganizationStructure.GetUnitsForSection()` sebagai data source. Pattern cascade sudah proven di Dashboard.cshtml |
| FILT-02 | Filter by status (Aktif/AkanExpired/Expired/Permanent) | `CertificateStatus` enum sudah terdefinisi di ViewModel. Filter dilakukan post-query (in-memory) pada `List<SertifikatRow>` karena status is derived, bukan kolom DB |
| FILT-03 | Filter by tipe (Training/Assessment) | `RecordType` enum sudah terdefinisi. `SertifikatRow.RecordType` tersedia. Filter in-memory sederhana |
| FILT-04 | Free-text search dengan debounce, update tabel + summary cards via AJAX | Pattern fetch+AbortController sudah proven di Dashboard.cshtml. Summary cards harus di luar partial view, di-update via JSON atau dari data attribute di partial |
</phase_requirements>

---

## Summary

Phase ini menambahkan filter bar interaktif di halaman CertificationManagement yang sudah ada (dibuat Phase 187). Filter mencakup cascade Bagian/Unit, status sertifikat, tipe (Training/Assessment), dan free-text search — semua via AJAX tanpa reload.

Semua building block sudah ada di proyek: `GetCascadeOptions` endpoint untuk cascade, `FilterCoachingProton` + Dashboard.cshtml sebagai referensi pola AJAX, `PaginationHelper` untuk paginasi, dan `BuildSertifikatRowsAsync()` yang perlu diperluas dengan parameter filter. Yang baru adalah: (1) action AJAX baru `FilterCertificationManagement`, (2) partial view `_CertificationManagementTablePartial`, (3) modifikasi ViewModel untuk membawa filter params dan count yang dibutuhkan summary cards, (4) JS filter bar wiring di CertificationManagement.cshtml.

Tantangan utama adalah summary cards: cards berada di luar area yang di-refresh oleh partial view. Solusi terpilih adalah mengembalikan data count sebagai `data-*` attribute di root elemen partial view, lalu JS membacanya setelah innerHTML replace untuk mengupdate cards.

**Primary recommendation:** Ikuti pola `FilterCoachingProton` secara exact — action HttpGet menerima filter params, memanggil helper yang sudah ada (dengan filter tambahan), return `PartialView`. Filter diterapkan in-memory setelah `BuildSertifikatRowsAsync()` karena status is derived dan sudah dimaterialisasi.

---

## Standard Stack

### Core (sudah ada di proyek)

| Komponen | Lokasi | Peran |
|----------|--------|-------|
| `CDPController.GetCascadeOptions` | CDPController.cs:288 | Endpoint cascade Bagian→Unit, return JSON |
| `OrganizationStructure.GetUnitsForSection()` | (static) | Data source unit per section |
| `PaginationHelper.Calculate()` | Helpers/PaginationHelper.cs | Server-side pagination |
| `BuildSertifikatRowsAsync()` | CDPController.cs:3052 | Ambil semua rows, perlu tambah filter params |
| `CertificationManagementViewModel` | Models/CertificationManagementViewModel.cs | Perlu tambah filter state properties + count |
| Dashboard.cshtml JS pattern | Views/CDP/Dashboard.cshtml | fetch + AbortController + loading class |

### Tidak Ada Dependency Baru

Tidak perlu install library tambahan. Semua yang dibutuhkan sudah ada.

---

## Architecture Patterns

### Recommended Project Structure (tambahan)

```
Views/CDP/
├── CertificationManagement.cshtml     # Existing — tambah filter bar + JS
└── Shared/
    └── _CertificationManagementTablePartial.cshtml   # NEW — tabel + pagination

Controllers/
└── CDPController.cs
    ├── CertificationManagement()      # Existing — tetap untuk initial page load
    └── FilterCertificationManagement()  # NEW — AJAX endpoint
```

### Pattern 1: AJAX Endpoint returns PartialView

Action baru `FilterCertificationManagement` terima semua filter params, jalankan filtering in-memory, return partial:

```csharp
// Source: Proven pattern dari FilterCoachingProton (CDPController.cs:267)
[HttpGet]
public async Task<IActionResult> FilterCertificationManagement(
    string? bagian = null,
    string? unit = null,
    string? status = null,
    string? tipe = null,
    string? search = null,
    int page = 1)
{
    var allRows = await BuildSertifikatRowsAsync();

    // Apply filters in-memory (status is derived — cannot filter at DB level)
    if (!string.IsNullOrEmpty(bagian))
        allRows = allRows.Where(r => r.Bagian == bagian).ToList();
    if (!string.IsNullOrEmpty(unit))
        allRows = allRows.Where(r => r.Unit == unit).ToList();
    if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
        allRows = allRows.Where(r => r.Status == st).ToList();
    if (!string.IsNullOrEmpty(tipe) && Enum.TryParse<RecordType>(tipe, out var rt))
        allRows = allRows.Where(r => r.RecordType == rt).ToList();
    if (!string.IsNullOrEmpty(search))
    {
        var q = search.ToLower();
        allRows = allRows.Where(r =>
            r.NamaWorker.ToLower().Contains(q) ||
            r.Judul.ToLower().Contains(q) ||
            (r.NomorSertifikat?.ToLower().Contains(q) ?? false)
        ).ToList();
    }

    allRows = allRows.OrderByDescending(r => r.TanggalTerbit).ToList();

    var vm = new CertificationManagementViewModel
    {
        TotalCount = allRows.Count,
        AktifCount = allRows.Count(r => r.Status == CertificateStatus.Aktif),
        AkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired),
        ExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired),
        PermanentCount = allRows.Count(r => r.Status == CertificateStatus.Permanent),
    };

    var paging = PaginationHelper.Calculate(allRows.Count, page, vm.PageSize);
    vm.Rows = allRows.Skip(paging.Skip).Take(paging.Take).ToList();
    vm.CurrentPage = paging.CurrentPage;
    vm.TotalPages = paging.TotalPages;

    return PartialView("_CertificationManagementTablePartial", vm);
}
```

### Pattern 2: Summary Card Update via data-* Attributes

Summary cards berada di luar partial view sehingga tidak bisa di-replace dengan innerHTML. Solusi: partial view root element membawa count di `data-*` attributes, JS membacanya setelah replace.

```html
<!-- Di _CertificationManagementTablePartial.cshtml root element -->
<div id="cert-table-content"
     data-total="@Model.TotalCount"
     data-aktif="@Model.AktifCount"
     data-akan-expired="@Model.AkanExpiredCount"
     data-expired="@Model.ExpiredCount"
     data-permanent="@Model.PermanentCount">
    <!-- tabel + pagination -->
</div>
```

```javascript
// JS setelah innerHTML replace
container.innerHTML = html;
var content = container.querySelector('#cert-table-content');
if (content) {
    document.getElementById('count-total').textContent = content.dataset.total;
    document.getElementById('count-aktif').textContent = content.dataset.aktif;
    document.getElementById('count-akan-expired').textContent = content.dataset.akanExpired;
    document.getElementById('count-expired').textContent = content.dataset.expired;
}
```

### Pattern 3: JS Debounce untuk Search Field

```javascript
// Source: Pattern minimal tanpa library external
var searchTimer = null;
searchEl.addEventListener('keyup', function () {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(refreshTable, 300);
});
```

### Pattern 4: AbortController untuk Cancel In-flight Request

```javascript
// Source: Proven pattern dari Dashboard.cshtml
var certAbort = null;

function refreshTable() {
    var container = document.getElementById('cert-table-container');
    container.classList.add('dashboard-loading');

    var params = new URLSearchParams();
    params.set('page', '1'); // Reset ke page 1
    if (bagianEl.value) params.set('bagian', bagianEl.value);
    if (unitEl.value) params.set('unit', unitEl.value);
    if (statusEl.value) params.set('status', statusEl.value);
    if (tipeEl.value) params.set('tipe', tipeEl.value);
    if (searchEl.value.trim()) params.set('search', searchEl.value.trim());

    if (certAbort) certAbort.abort();
    certAbort = new AbortController();

    fetch('/CDP/FilterCertificationManagement?' + params, { signal: certAbort.signal })
        .then(function (resp) { return resp.text(); })
        .then(function (html) {
            container.innerHTML = html;
            // Re-wire pagination links
            container.querySelectorAll('a[data-page]').forEach(function (link) {
                link.addEventListener('click', function (e) {
                    e.preventDefault();
                    currentPage = parseInt(this.dataset.page);
                    refreshTablePage(currentPage);
                });
            });
            // Update summary cards
            updateSummaryCards(container);
            container.classList.remove('dashboard-loading');
        })
        .catch(function (e) {
            if (e.name !== 'AbortError') {
                console.error(e);
                container.classList.remove('dashboard-loading');
            }
        });
}
```

### Pattern 5: Paginasi dalam Partial View

Paginasi di partial view tidak bisa menggunakan `asp-route-page` tag helper (link ke full page). Harus menggunakan `data-page` attribute + JS event delegation:

```html
<!-- Di partial view — bukan asp-action link -->
<a href="#" class="page-link" data-page="@(Model.CurrentPage - 1)">
    <i class="bi bi-chevron-left"></i>
</a>
```

JS menangkap click dan memanggil `refreshTablePage(page)` — versi `refreshTable` yang preserve filter params saat ini tapi ganti page number.

### Pattern 6: ViewModel Extension

Tambah filter state ke ViewModel agar partial view bisa merender paginasi yang benar:

```csharp
// Di CertificationManagementViewModel — tambahkan:
public string? FilterBagian { get; set; }
public string? FilterUnit { get; set; }
public string? FilterStatus { get; set; }
public string? FilterTipe { get; set; }
public string? FilterSearch { get; set; }
```

Filter state tidak diperlukan oleh partial view untuk merender tabel, tapi berguna jika pagination link perlu membawa state. Karena JS mengelola state, ini opsional — partial view hanya butuh `CurrentPage` dan `TotalPages`.

### Anti-Patterns to Avoid

- **Jangan pakai `asp-route-page` di partial view:** Tag helper menghasilkan link ke full page action, bukan AJAX. Gunakan `data-page` + JS handler.
- **Jangan filter di DB level untuk `Status`:** `CertificateStatus` adalah derived property (bukan kolom DB). Selalu filter in-memory setelah `BuildSertifikatRowsAsync()`.
- **Jangan rebuild tabel dari scratch:** Reuse `BuildSertifikatRowsAsync()` yang sudah ada — hanya tambah filter params, jangan duplikasi query logic.
- **Jangan update summary cards dengan `innerText` hardcode di JS:** Baca dari `data-*` di partial view agar konsisten dengan server-side calculation.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cascade Bagian→Unit | Custom endpoint baru | `CDPController.GetCascadeOptions` (line 288) | Sudah proven, endpoint yang sama sudah dipakai Dashboard |
| Pagination calculation | Custom math | `PaginationHelper.Calculate()` | Sudah ada, handles edge cases |
| Loading state CSS | Custom spinner component | CSS class `.dashboard-loading` (sudah di Dashboard.cshtml) | Konsisten visual, copy pattern |
| In-flight cancel | Custom flag variable | `AbortController` (Web API native) | Pattern sudah proven di Dashboard |

---

## Common Pitfalls

### Pitfall 1: Summary Cards di Luar Partial View

**What goes wrong:** Developer mem-partial-view seluruh area termasuk summary cards, atau lupa update cards setelah AJAX replace.

**Why it happens:** Partial view hanya mengganti `innerHTML` container tabel. Cards adalah sibling di atas, bukan child container.

**How to avoid:** Encode counts sebagai `data-*` attributes di root element partial view. JS baca setelah replace dan update card elements individually.

**Warning signs:** Cards menampilkan angka lama setelah filter diubah.

### Pitfall 2: Paginasi Link yang Reload Full Page

**What goes wrong:** Partial view menggunakan `asp-action` tag helper untuk pagination, klik page melakukan full page reload.

**How to avoid:** Gunakan `<a href="#" data-page="N">` di partial view. JS di parent page wire event listener setelah setiap `innerHTML` replace (karena DOM baru).

**Warning signs:** Klik pagination hilangkan filter state dan reload seluruh halaman.

### Pitfall 3: Race Condition pada Rapid Typing

**What goes wrong:** User mengetik cepat, multiple fetch in-flight, response datang out-of-order.

**How to avoid:** `AbortController` — abort request sebelumnya sebelum buat yang baru. Pattern sudah ada di Dashboard.

### Pitfall 4: Script Tags di Partial View Tidak Dieksekusi

**What goes wrong:** Jika partial view punya `<script>` tags, `innerHTML = html` tidak mengeksekusinya.

**How to avoid:** Jika partial view butuh script, re-clone script elements (pattern sudah ada di Dashboard.cshtml lines 133-136). Namun lebih baik: tidak masukkan `<script>` di partial view — semua JS di parent page.

---

## Code Examples

### Existing AJAX Pattern (HIGH confidence — dari codebase aktual)

```javascript
// Source: Views/CDP/Dashboard.cshtml — pattern fetch+AbortController
function refreshProtonContent() {
    var container = document.getElementById('proton-content');
    container.classList.add('dashboard-loading');

    var params = new URLSearchParams();
    if (sectionEl.value) params.set('section', sectionEl.value);
    // ...

    if (protonAbort) protonAbort.abort();
    protonAbort = new AbortController();

    fetch('/CDP/FilterCoachingProton?' + params, { signal: protonAbort.signal })
        .then(function (resp) { return resp.text(); })
        .then(function (html) {
            container.innerHTML = html;
            container.classList.remove('dashboard-loading');
        })
        .catch(function (e) {
            if (e.name !== 'AbortError') {
                console.error(e);
                container.classList.remove('dashboard-loading');
            }
        });
}
```

### Existing GetCascadeOptions (HIGH confidence — dari codebase aktual)

```csharp
// Source: CDPController.cs:288
[HttpGet]
public IActionResult GetCascadeOptions(string? section)
{
    var units = string.IsNullOrEmpty(section) ? new List<string>() : OrganizationStructure.GetUnitsForSection(section);
    var categories = _context.ProtonTracks.Select(t => t.TrackType).Distinct().OrderBy(t => t).ToList();
    var tracks = _context.ProtonTracks.OrderBy(t => t.Urutan).Select(t => t.DisplayName).ToList();
    return Json(new { units, categories, tracks });
}
```

Untuk CertificationManagement, endpoint ini bisa dipakai langsung untuk mendapat `units`. Field `categories` dan `tracks` bisa diabaikan.

### Existing Loading CSS (HIGH confidence — dari codebase aktual)

```css
/* Source: Views/CDP/Dashboard.cshtml */
.dashboard-loading { opacity: 0.5; pointer-events: none; position: relative; }
.dashboard-loading::after {
    content: '';
    position: absolute; top: 50%; left: 50%;
    width: 2rem; height: 2rem;
    margin: -1rem 0 0 -1rem;
    border: 3px solid #dee2e6; border-top-color: #0d6efd;
    border-radius: 50%; animation: spin 0.6s linear infinite;
}
```

Copy CSS ini ke `CertificationManagement.cshtml` `<style>` block, atau letakkan di shared CSS jika ada.

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| Full page reload per filter | AJAX partial view replace | Sudah proven di Dashboard Proton (Phase 121) |
| Pagination via URL `asp-route-page` | `data-page` + JS handler dalam AJAX context | Diperlukan agar pagination preserve filter state |

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual browser testing (proyek tidak memiliki automated test suite) |
| Config file | none |
| Quick run command | `dotnet run` + manual browser verify |
| Full suite command | `dotnet build` (compile check) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Command | Notes |
|--------|----------|-----------|---------|-------|
| FILT-01 | Cascade Bagian→Unit update dropdown | manual | Browser: pilih Bagian → Unit dropdown terisi | |
| FILT-01 | Filter tabel by Bagian/Unit | manual | Browser: pilih Bagian/Unit → tabel hanya tampilkan rows matching | |
| FILT-02 | Filter tabel by Status | manual | Browser: pilih Status "Expired" → hanya rows status Expired | |
| FILT-03 | Filter tabel by Tipe | manual | Browser: pilih "Training" → hanya training rows | |
| FILT-04 | Free-text search dengan debounce | manual | Browser: ketik nama worker → hasil update setelah 300ms | |
| FILT-04 | Summary cards update via AJAX | manual | Browser: filter → summary card counts berubah sesuai | |
| FILT-04 | Pagination reset ke page 1 | manual | Browser: ada di page 2, ganti filter → kembali ke page 1 | |
| FILT-04 | Tombol Reset | manual | Browser: isi semua filter, klik Reset → semua clear + tabel reload | |

### Wave 0 Gaps

- [ ] `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` — dibuat di Wave 1 (task utama phase ini)

---

## Sources

### Primary (HIGH confidence)
- Codebase aktual — `Controllers/CDPController.cs` (FilterCoachingProton line 267, GetCascadeOptions line 288, CertificationManagement line 3026, BuildSertifikatRowsAsync line 3052)
- Codebase aktual — `Views/CDP/Dashboard.cshtml` (fetch+AbortController pattern, loading CSS)
- Codebase aktual — `Models/CertificationManagementViewModel.cs` (enums, SertifikatRow, ViewModel)
- Codebase aktual — `Views/CDP/CertificationManagement.cshtml` (existing page structure)
- Codebase aktual — `Helpers/PaginationHelper.cs` (PaginationResult record)

### Tertiary (LOW confidence — tidak diverifikasi karena semua sudah ada di codebase)
- Tidak ada external sources diperlukan

---

## Open Questions

1. **Apakah `GetCascadeOptions` endpoint perlu versi baru untuk CertificationManagement?**
   - Yang diketahui: Endpoint existing mengembalikan `{ units, categories, tracks }` — `categories` dan `tracks` tidak relevan untuk sertifikat.
   - Yang tidak jelas: Apakah JSON extra fields menjadi masalah? Tidak — JS hanya pakai `data.units`.
   - Rekomendasi: Pakai endpoint yang sama, abaikan `categories` dan `tracks` di JS.

2. **Bagaimana partial view mendapat list Bagian yang tersedia untuk populate dropdown?**
   - Yang diketahui: Bagian adalah static list dari `OrganizationStructure`. Dropdown pilihan Bagian perlu dirender di main view (bukan partial).
   - Rekomendasi: Render `<select id="filter-bagian">` di main page dengan options dari `OrganizationStructure.GetSections()` (atau equivalent static list). Ini dirender saat page load, tidak perlu AJAX.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua komponen diverifikasi langsung dari codebase
- Architecture: HIGH — mengikuti pola yang sudah proven di proyek yang sama
- Pitfalls: HIGH — identified dari analisis pola yang ada dan perbedaan halaman ini (summary cards di luar partial)

**Research date:** 2026-03-18
**Valid until:** Stabil — tidak bergantung pada library external yang berubah
