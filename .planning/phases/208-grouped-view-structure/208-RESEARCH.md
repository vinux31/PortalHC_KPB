# Phase 208: Grouped View Structure - Research

**Researched:** 2026-03-20
**Domain:** ASP.NET Core MVC — ViewModel grouping layer + Bootstrap 5 accordion + AJAX partial view
**Confidence:** HIGH

## Summary

Phase 208 mengubah tampilan flat table RenewalCertificate menjadi accordion grouped per judul sertifikat. Tidak ada perubahan database, tidak ada library baru — semua implementasi memanfaatkan Bootstrap 5 collapse yang sudah ada, ViewModel grouping di C#, dan pattern AJAX partial view yang sudah mapan.

Perubahan utama ada di tiga lapisan: (1) ViewModel baru `RenewalGroupViewModel` untuk menampung group data, (2) partial view baru `_RenewalGroupedPartial.cshtml` menggantikan `_RenewalCertificateTablePartial.cshtml`, dan (3) `FilterRenewalCertificate` action di-update untuk return partial baru. `FilterRenewalCertificateGroup` endpoint tambahan diperlukan untuk pagination per-group tanpa refresh seluruh halaman.

Semua keputusan desain sudah dikunci di CONTEXT.md dan UI-SPEC.md. Riset ini memverifikasi bahwa pendekatan yang dipilih sepenuhnya layak dengan stack existing dan tidak ada rintangan teknis.

**Primary recommendation:** Buat `RenewalGroupViewModel` + `RenewalGroup` model baru, reuse `BuildRenewalRowsAsync()` sebagai data source, dan implementasikan Bootstrap 5 `data-bs-toggle="collapse"` di partial view baru.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- Accordion card style — setiap group adalah card terpisah dengan header clickable
- Chevron icon (bi-chevron-right / bi-chevron-down) untuk indicate expand/collapse state
- Header menampilkan: judul sertifikat, kategori/sub-kategori, badge count
- Default state: semua collapsed saat halaman dibuka
- Badge count: tiga badge terpisah — total orang (bg-secondary), expired count (bg-danger, hanya jika > 0), akan expired count (bg-warning text-dark, hanya jika > 0)
- Sorting antar group: berdasarkan MinValidUntil terkecil di antara anggotanya (paling mendesak di atas)
- Sorting row dalam group: Expired dulu, kemudian ValidUntil ascending
- Pagination per group (bukan global), setiap group punya pagination sendiri
- Kolom dihilangkan: Judul Sertifikat, No
- Kolom tetap ada: Checkbox, Nama, Kategori, Sub Kategori, Valid Until, Status, Aksi

### Claude's Discretion

- Page size per group (dipilih: 10 baris per group — cukup untuk sebagian besar sertifikat)
- Animasi collapse/expand (dipilih: Bootstrap collapse default transition ~200ms)
- Exact badge styling dan warna (dikunci di UI-SPEC: bg-secondary / bg-danger / bg-warning)
- Loading state saat pagination per group (dipilih: spinner-border spinner-border-sm di dalam group body)

### Deferred Ideas (OUT OF SCOPE)

- Filter compatibility pada grouped view — Phase 209 (FILT-01, FILT-02)
- Bulk renew per group — Phase 209 (BULK-01, BULK-02)
- Summary cards update sesuai filter — Phase 209 (FILT-02)
- Checkbox select-all per group dan tombol "Renew N Pekerja" per group — Phase 209
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| GRP-01 | Tabel RenewalCertificate menampilkan data dikelompokkan per judul sertifikat (bukan flat list per-orang) | Ditangani oleh `RenewalGroup` model + grouping LINQ di `FilterRenewalCertificate`. `BuildRenewalRowsAsync()` sudah menghasilkan semua data yang diperlukan. |
| GRP-02 | Group header menampilkan judul sertifikat, kategori/sub-kategori, dan badge count (N expired, N akan expired) | Ditangani oleh `RenewalGroup.ExpiredCount` + `AkanExpiredCount` + `TotalCount` properties. UI-SPEC mendefinisikan exact badge markup. |
| GRP-03 | Setiap group bisa di-collapse/expand (default: collapsed) | Bootstrap 5 `data-bs-toggle="collapse"` sudah tersedia. Default collapsed: tidak pakai class `show` pada `.collapse` element. Chevron swap via `show.bs.collapse` / `hide.bs.collapse` events. |
| GRP-04 | Tabel per group hanya menampilkan kolom: Checkbox, Nama, Valid Until, Status, Aksi | Dua kolom dihilangkan dari partial: Judul Sertifikat (redundant dengan header group) dan No (nomor urut). UI-SPEC mendefinisikan 7 kolom tersisa. |
</phase_requirements>

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap 5 | sudah di-install (project) | Accordion/collapse, badge, card, pagination | Sudah ada — tidak perlu instalasi baru |
| Bootstrap Icons | sudah di-install (project) | bi-chevron-down, bi-chevron-right | Sudah ada |
| ASP.NET Core MVC | existing | ViewModel, PartialView, AJAX endpoint | Stack project |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `PaginationHelper.Calculate()` | existing helper | Hitung skip/take/totalPages per group | Digunakan di tiap group, bukan global |
| `BuildRenewalRowsAsync()` | existing private method | Fetch semua renewal rows dari DB | Dipanggil sekali, lalu di-group di memory |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Bootstrap 5 collapse (native) | Custom JS toggle | Bootstrap sudah ada dan accessible — tidak perlu hand-roll |
| Server-side grouping (LINQ) | Client-side JS grouping | Server-side lebih aman untuk pagination per group yang akurat |

**Installation:** Tidak ada instalasi baru diperlukan.

---

## Architecture Patterns

### Recommended Structure Perubahan

```
Models/
└── CertificationManagementViewModel.cs   # Tambah RenewalGroup + RenewalGroupViewModel

Views/Admin/Shared/
├── _RenewalCertificateTablePartial.cshtml  # AKAN DIGANTI
└── _RenewalGroupedPartial.cshtml           # BARU — accordion grouped view

Controllers/
└── AdminController.cs
    ├── FilterRenewalCertificate()          # UPDATE — return grouped partial
    └── FilterRenewalCertificateGroup()     # BARU — pagination per satu group
```

### Pattern 1: RenewalGroup ViewModel

Tambahkan dua class baru di `CertificationManagementViewModel.cs`:

```csharp
// Model untuk satu group (satu judul sertifikat)
public class RenewalGroup
{
    public string GroupKey { get; set; } = "";  // slug-safe: judul sertifikat di-encode
    public string Judul { get; set; } = "";
    public string? Kategori { get; set; }
    public string? SubKategori { get; set; }

    // Badge counts
    public int TotalCount { get; set; }
    public int ExpiredCount { get; set; }
    public int AkanExpiredCount { get; set; }

    // Untuk sorting antar group
    public DateTime? MinValidUntil { get; set; }

    // Paginated rows untuk halaman ini
    public List<SertifikatRow> Rows { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ViewModel untuk seluruh halaman grouped
public class RenewalGroupViewModel
{
    public List<RenewalGroup> Groups { get; set; } = new();
    public int TotalExpiredCount { get; set; }
    public int TotalAkanExpiredCount { get; set; }
}
```

### Pattern 2: Grouping Logic di Controller

Grouping terjadi SETELAH `BuildRenewalRowsAsync()` dipanggil, di memory (bukan DB query baru):

```csharp
// Di FilterRenewalCertificate — setelah filtering existing:
var grouped = allRows
    .GroupBy(r => r.Judul)
    .Select(g => new RenewalGroup
    {
        GroupKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(g.Key))
                         .Replace("+","_").Replace("/","-").Replace("=",""),
        Judul = g.Key,
        Kategori = g.First().Kategori,
        SubKategori = g.First().SubKategori,
        TotalCount = g.Count(),
        ExpiredCount = g.Count(r => r.Status == CertificateStatus.Expired),
        AkanExpiredCount = g.Count(r => r.Status == CertificateStatus.AkanExpired),
        MinValidUntil = g.Min(r => r.ValidUntil)
    })
    .OrderBy(g => g.MinValidUntil ?? DateTime.MaxValue)
    .ToList();

// Paginate rows dalam setiap group (page 1 untuk semua group pada initial load)
foreach (var group in grouped)
{
    var allGroupRows = allRows
        .Where(r => r.Judul == group.Judul)
        .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
        .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
        .ToList();
    var paging = PaginationHelper.Calculate(allGroupRows.Count, 1, group.PageSize);
    group.Rows = allGroupRows.Skip(paging.Skip).Take(paging.Take).ToList();
    group.CurrentPage = paging.CurrentPage;
    group.TotalPages = paging.TotalPages;
}
```

### Pattern 3: Bootstrap 5 Accordion di Partial View

Struktur HTML per group (semua collapsed by default — tidak ada class `show`):

```html
<div class="card border-0 shadow-sm mb-3">
    <div class="card-header d-flex align-items-center justify-content-between"
         role="button"
         data-bs-toggle="collapse"
         data-bs-target="#group-@group.GroupKey-body"
         aria-expanded="false"
         aria-controls="group-@group.GroupKey-body"
         style="cursor:pointer; min-height:48px">
        <div class="d-flex align-items-center gap-2">
            <i class="bi bi-chevron-right group-chevron" id="chevron-@group.GroupKey"></i>
            <span class="fw-semibold">@group.Judul</span>
            <div class="d-flex gap-1" aria-label="@group.TotalCount orang, @group.ExpiredCount expired, @group.AkanExpiredCount akan expired">
                <span class="badge bg-secondary">@group.TotalCount orang</span>
                @if (group.ExpiredCount > 0) {
                    <span class="badge bg-danger">@group.ExpiredCount Expired</span>
                }
                @if (group.AkanExpiredCount > 0) {
                    <span class="badge bg-warning text-dark">@group.AkanExpiredCount Akan Expired</span>
                }
            </div>
        </div>
        <small class="text-muted">
            @(string.IsNullOrEmpty(group.SubKategori) ? group.Kategori : $"{group.Kategori} / {group.SubKategori}")
        </small>
    </div>
    <div class="collapse" id="group-@group.GroupKey-body"
         aria-labelledby="group-@group.GroupKey-header">
        <div class="card-body p-0" id="group-@group.GroupKey-table">
            @* tabel rows + pagination *@
        </div>
    </div>
</div>
```

### Pattern 4: Chevron Swap via Bootstrap Events

Di JavaScript (ditambahkan setelah innerHTML replace di `refreshTable`):

```javascript
function wireGroupChevrons() {
    document.querySelectorAll('.collapse').forEach(function(collapseEl) {
        collapseEl.addEventListener('show.bs.collapse', function() {
            var key = collapseEl.id.replace('group-', '').replace('-body', '');
            var chevron = document.getElementById('chevron-' + key);
            if (chevron) {
                chevron.classList.replace('bi-chevron-right', 'bi-chevron-down');
            }
        });
        collapseEl.addEventListener('hide.bs.collapse', function() {
            var key = collapseEl.id.replace('group-', '').replace('-body', '');
            var chevron = document.getElementById('chevron-' + key);
            if (chevron) {
                chevron.classList.replace('bi-chevron-down', 'bi-chevron-right');
            }
        });
    });
}
```

### Pattern 5: Endpoint Pagination Per Group

Endpoint baru untuk pagination satu group tanpa reload semua group:

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> FilterRenewalCertificateGroup(
    string groupKey,
    string judul,
    int page = 1,
    string? bagian = null, string? unit = null,
    string? status = null, string? category = null, string? subCategory = null)
{
    var allRows = await BuildRenewalRowsAsync();
    // Apply same filters as FilterRenewalCertificate
    // Then filter by judul
    var groupRows = allRows
        .Where(r => r.Judul == judul)
        .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
        .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
        .ToList();

    var paging = PaginationHelper.Calculate(groupRows.Count, page, 10);
    var group = new RenewalGroup {
        GroupKey = groupKey, Judul = judul,
        Rows = groupRows.Skip(paging.Skip).Take(paging.Take).ToList(),
        CurrentPage = paging.CurrentPage, TotalPages = paging.TotalPages,
        TotalCount = groupRows.Count
    };
    return PartialView("Shared/_RenewalGroupTablePartial", group);
}
```

### Anti-Patterns to Avoid

- **GroupKey dari index angka:** Jika halaman di-re-render (AJAX refresh), index berubah. Gunakan hash/encode dari Judul.
- **Pagination global untuk grouped view:** Dengan pagination global, satu group besar menyembunyikan group lain. Pagination per group adalah keputusan yang sudah dikunci.
- **Client-side JS grouping:** Tidak bisa akurat dengan pagination per group — total count per group tidak diketahui client.
- **Memanggil DB langsung di action grouping:** Reuse `BuildRenewalRowsAsync()` — method ini sudah di-optimize dan menggabungkan Training + Assessment records.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Collapse/expand toggle | Custom JS toggle dengan display:none | Bootstrap 5 `data-bs-toggle="collapse"` | Sudah ada, accessible, animasi built-in |
| Pagination per group | Custom paginasi baru | `PaginationHelper.Calculate()` dengan pageSize=10 per group | Sudah ada dan proven |
| Data fetching semua rows | Query DB baru per group | `BuildRenewalRowsAsync()` sekali, group di memory | Hindari N+1 queries |

**Key insight:** Seluruh logika grouping bisa dilakukan di memory setelah satu call ke `BuildRenewalRowsAsync()`. Tidak ada migration, tidak ada DB schema change.

---

## Common Pitfalls

### Pitfall 1: GroupKey Collision atau Invalid HTML ID
**What goes wrong:** Judul sertifikat mengandung spasi, slash, tanda kurung — tidak valid sebagai HTML id attribute.
**Why it happens:** Data langsung digunakan sebagai id.
**How to avoid:** Encode judul ke Base64 URL-safe (replace +/-/=) sebagai GroupKey. Jangan gunakan index angka (berubah saat filter).
**Warning signs:** JavaScript error "querySelector failed" atau chevron tidak swap.

### Pitfall 2: wirePagination() Tidak Meng-handle Pagination Per Group
**What goes wrong:** Klik pagination per group memicu `refreshTable()` (global refresh) bukan group-level refresh.
**Why it happens:** `wirePagination()` existing terhubung ke semua `a[data-page]`.
**How to avoid:** Bedakan pagination link global vs per-group. Tambahkan `data-group-key` attribute pada pagination link per group. Di `wirePagination()`, cek keberadaan attribute ini — jika ada, trigger group-level AJAX; jika tidak, trigger global.
**Warning signs:** Klik pagination per group me-refresh seluruh `#cert-table-container`.

### Pitfall 3: wireCheckboxes() Gagal Setelah AJAX Refresh Group
**What goes wrong:** Setelah pagination per group, checkbox baru tidak ter-wire event listener.
**Why it happens:** `wireCheckboxes()` dipanggil di `refreshTable()` tapi tidak di callback pagination per group.
**How to avoid:** Panggil `wireCheckboxes()` juga di callback pagination per group setelah replace innerHTML.
**Warning signs:** Checkbox di halaman 2 group tidak memicu update `btn-renew-selected`.

### Pitfall 4: Bootstrap Collapse Tidak Reinitialize Setelah AJAX Replace
**What goes wrong:** Setelah AJAX refresh (`refreshTable()`), Bootstrap collapse tidak berfungsi pada card baru.
**Why it happens:** Bootstrap 5 collapse dengan `data-bs-toggle` biasanya auto-initialize saat DOM load, tapi tidak saat innerHTML di-replace.
**How to avoid:** Bootstrap 5 collapse dengan `data-bs-*` attributes bekerja via event delegation — tidak perlu manual init ulang jika menggunakan `data-bs-toggle="collapse"` (bukan `new bootstrap.Collapse()`). Verifikasi ini saat testing.
**Warning signs:** Klik header group setelah filter tidak toggle accordion.

### Pitfall 5: RenewalCertificate Action Tidak Return Groups
**What goes wrong:** Initial page load (GET /Admin/RenewalCertificate) masih render ViewModel lama tanpa groups — tabel kosong atau error.
**Why it happens:** `RenewalCertificate()` action mengisi `CertificationManagementViewModel` tanpa Rows (hanya counts). Partial view baru mengharapkan `RenewalGroupViewModel`.
**How to avoid:** Untuk initial load, `RenewalCertificate()` memanggil `FilterRenewalCertificate()` logic secara internal, atau partial view di-load via AJAX otomatis saat halaman pertama dibuka (mirip pattern existing). Lihat code existing: `RenewalCertificate.cshtml` memanggil `@await Html.PartialAsync("Shared/_RenewalCertificateTablePartial", Model)` — partial ini yang perlu diganti dengan partial baru + model baru.

---

## Code Examples

### Existing: BuildRenewalRowsAsync tersedia di Controller
```csharp
// Controllers/AdminController.cs — private method yang di-reuse
var allRows = await BuildRenewalRowsAsync();
// Returns: List<SertifikatRow> dengan semua Training + Assessment expired/akan expired
// Filtering dan grouping dilakukan di memory setelah call ini
```

### Existing: PaginationHelper signature
```csharp
// Helpers/PaginationHelper.cs
PaginationResult Calculate(int totalCount, int page, int pageSize)
// Returns: { CurrentPage, TotalPages, TotalCount, Skip, Take }
```

### Existing: Summary cards update mechanism (TETAP DIPERTAHANKAN)
```html
<!-- Di partial view baru, tetap sertakan hidden spans ini -->
<span id="partial-expired-count" class="d-none">@Model.TotalExpiredCount</span>
<span id="partial-akan-expired-count" class="d-none">@Model.TotalAkanExpiredCount</span>
```
```javascript
// Di RenewalCertificate.cshtml — updateSummaryCards() existing tetap berfungsi
// asalkan partial view baru menyertakan span dengan ID yang sama
```

### Existing: AJAX pagination wire pattern
```javascript
// wirePagination() existing di RenewalCertificate.cshtml
container.querySelectorAll('a[data-page]').forEach(function (link) {
    link.addEventListener('click', function (e) {
        e.preventDefault();
        var p = parseInt(this.dataset.page);
        if (p >= 1) { resetKategoriLock(); refreshTable(p); }
    });
});
// Perlu diextend: tambahkan check data-group-key untuk pagination per group
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Flat list per-orang | Grouped accordion per sertifikat | Phase 208 | Admin langsung lihat mana sertifikat paling urgent tanpa scan baris satu-per-satu |
| Pagination global | Pagination per group | Phase 208 | Group dengan banyak anggota tidak menghalangi akses ke group lain |

---

## Open Questions

1. **Bagaimana initial load menampilkan grouped view?**
   - What we know: `RenewalCertificate()` action mengisi `CertificationManagementViewModel` tanpa Rows (hanya counts), lalu partial di-load via AJAX via `FilterRenewalCertificate`.
   - What's unclear: Apakah `RenewalCertificate.cshtml` perlu diubah untuk pass `RenewalGroupViewModel` sebagai model? Atau tetap pass model lama + partial langsung call `FilterRenewalCertificate` on page load?
   - Recommendation: Opsi paling clean: `RenewalCertificate()` action tetap return `CertificationManagementViewModel` (untuk summary counts), tapi partial di-load via AJAX saat `DOMContentLoaded`. Ini konsisten dengan pattern existing dan tidak mengubah signature action utama.

2. **GroupKey encoding: apakah Base64 cukup untuk uniqueness?**
   - What we know: Judul sertifikat bisa sama untuk kategori berbeda (edge case).
   - What's unclear: Apakah `Judul` saja cukup sebagai group key, atau perlu `Judul + Kategori`?
   - Recommendation: Gunakan `Judul` saja sebagai group key — sesuai dengan GRP-01 yang menyebut "per judul sertifikat". Jika ada duplikasi judul beda kategori, rows-nya akan bergabung dalam satu group (acceptable untuk phase ini).

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual browser testing (tidak ada automated test framework terdeteksi di project) |
| Config file | none |
| Quick run command | Buka browser, navigasi ke /Admin/RenewalCertificate |
| Full suite command | Manual testing checklist berikut |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Test Steps | Automated? |
|--------|----------|-----------|------------|------------|
| GRP-01 | Data tampil grouped per judul sertifikat | smoke | Buka halaman, verifikasi tabel flat tidak ada, accordion cards muncul | Manual |
| GRP-02 | Header tampil judul + kategori + badge counts | smoke | Expand satu group, verifikasi header content dan badge | Manual |
| GRP-03 | Collapse/expand berfungsi, default collapsed | smoke | Buka halaman: semua collapsed. Klik header: expand. Klik lagi: collapse. | Manual |
| GRP-04 | Tabel per group hanya kolom yang ditentukan | smoke | Expand group, verifikasi kolom Judul Sertifikat dan No tidak ada | Manual |

### Sampling Rate
- **Per task commit:** Buka browser, cek visual accordion dan toggle
- **Per wave merge:** Full checklist GRP-01 s/d GRP-04
- **Phase gate:** Semua GRP requirements pass sebelum `/gsd:verify-work`

### Wave 0 Gaps
- Tidak ada automated test infrastructure yang perlu dibuat. Project menggunakan manual testing.

---

## Sources

### Primary (HIGH confidence)
- `Views/Admin/RenewalCertificate.cshtml` — full page structure, existing JS patterns
- `Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml` — flat table yang akan diganti
- `Controllers/AdminController.cs` lines 6929-6994 — RenewalCertificate + FilterRenewalCertificate actions
- `Models/CertificationManagementViewModel.cs` — existing ViewModel classes
- `Helpers/PaginationHelper.cs` — PaginationHelper.Calculate() signature
- `.planning/phases/208-grouped-view-structure/208-CONTEXT.md` — locked decisions
- `.planning/phases/208-grouped-view-structure/208-UI-SPEC.md` — UI design contract
- Bootstrap 5 docs (collapse component) — `data-bs-toggle="collapse"` pattern

### Secondary (MEDIUM confidence)
- Bootstrap Icons bi-chevron-* — verified tersedia di project (digunakan di file existing)

### Tertiary (LOW confidence)
- Tidak ada.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library sudah ada di project, verified dari source code
- Architecture: HIGH — pattern ViewModel grouping + AJAX partial sudah mapan di project
- Pitfalls: HIGH — diidentifikasi dari code analysis langsung pada existing implementation

**Research date:** 2026-03-20
**Valid until:** 2026-04-20 (stack stabil — Bootstrap 5, ASP.NET Core MVC)
