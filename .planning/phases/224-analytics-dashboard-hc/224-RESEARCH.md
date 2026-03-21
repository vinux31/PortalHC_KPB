# Phase 224: Analytics Dashboard HC - Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC Dashboard — Chart.js visualisasi, AJAX partial reload, aggregate query EF Core
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Dashboard diakses via card/link di CMP Hub (CMP/Index) — sejajar dengan Assessment, Records, dll
- **D-02:** Akses dibatasi untuk role Admin dan HC — `[Authorize(Roles = "Admin, HC")]`
- **D-03:** Grid 2×2 — 4 card/chart dalam 2 kolom, compact, semua terlihat tanpa banyak scroll
- **D-04:** Fail rate = bar chart (per section/category), Trend = line chart (time series) menggunakan Chart.js
- **D-05:** Breakdown skor ET = tabel dengan heatmap warna (merah=rendah, hijau=tinggi), baris=kategori, kolom=rata-rata/min/max/distribusi
- **D-06:** Filter global di atas halaman — satu set filter berlaku untuk semua 4 visualisasi
- **D-07:** 4 dropdown filter + date range: Bagian, Unit (OrganizationUnit), Kategori, SubKategori, Periode
- **D-08:** Dropdown cascade: Bagian → Unit ter-filter, Kategori → SubKategori ter-filter
- **D-09:** AJAX partial reload saat filter berubah — tanpa full page reload, mirip pattern CDP Dashboard
- **D-10:** Default state: tampilkan semua data, periode 1 tahun terakhir
- **D-11:** Hanya tampilkan sertifikat expired dalam 30 hari ke depan (bukan 30/60/90)
- **D-12:** Ringkasan saja, tanpa link ke detail pekerja
- **D-13:** 4 kolom: Nama Pekerja, Nama Sertifikat, Tanggal Expired, Section/Unit

### Claude's Discretion

- Chart.js configuration details (colors, tooltips, responsive options)
- Loading skeleton/spinner design saat AJAX fetch
- Empty state handling jika tidak ada data
- Exact spacing, typography, card styling

### Deferred Ideas (OUT OF SCOPE)

Tidak ada — diskusi tetap dalam scope phase.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ANLT-01 | HC dapat melihat halaman Analytics Dashboard dengan visualisasi fail rate per section dan per category | Bar chart Chart.js; query AssessmentSession GROUP BY User.Section + Category WHERE IsPassed = false/true |
| ANLT-02 | HC dapat melihat trend assessment (pass/fail) dalam periode waktu tertentu | Line chart Chart.js; query AssessmentSession GROUP BY Month(CompletedAt) WHERE CompletedAt dalam range; filter Periode |
| ANLT-03 | HC dapat melihat breakdown skor ElemenTeknis aggregate per kategori assessment | Tabel heatmap; query SessionElemenTeknisScore JOIN AssessmentSession GROUP BY ElemenTeknis, Category; avg/min/max CorrectCount/QuestionCount |
| ANLT-04 | HC dapat melihat ringkasan sertifikat yang akan expired dalam 30 hari ke depan | Query TrainingRecord + AssessmentSession WHERE ValidUntil BETWEEN today AND today+30 AND Status = 'Valid'; tabel 4 kolom |
</phase_requirements>

---

## Summary

Phase 224 membangun halaman Analytics Dashboard untuk role Admin/HC di CMPController. Halaman menampilkan 4 visualisasi dalam layout grid 2×2 dengan filter global AJAX yang berlaku untuk semua panel. Pattern dasarnya sudah ada di CDP Dashboard (`Views/CDP/Dashboard.cshtml`) — loading state CSS, AbortController fetch, innerHTML replace.

Chart.js adalah library baru yang belum ada di project. Cara termudah adalah CDN via `<script>` tag di halaman Analytics saja, tanpa mengubah global `_Layout.cshtml`. Semua query data di-serve via JSON endpoint terpisah di CMPController yang menerima parameter filter yang sama.

Data sumber utama adalah `AssessmentSession` (ANLT-01/02), `SessionElemenTeknisScore` (ANLT-03), dan gabungan `TrainingRecord` + `AssessmentSession` (ANLT-04). Model `ApplicationUser` memiliki field `Section` dan `Unit` yang cukup untuk filter Bagian/Unit. `AssessmentCategory` sudah mendukung hierarki Kategori/SubKategori via `ParentId`.

**Primary recommendation:** Bangun satu action `AnalyticsDashboard` (GET, ViewModel awal) + satu action JSON `GetAnalyticsData` (POST/GET dengan filter params) di CMPController. View merender filter form + 4 panel placeholder, lalu AJAX mengisi konten panel dengan data JSON yang dirender oleh Chart.js dan tabel Razor inline.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Chart.js | 4.x (CDN) | Bar chart & line chart | Sudah dipilih di D-04; ringan, tidak perlu npm build |
| Bootstrap 5 | existing | Grid, card, tabel, form | Sudah ada di project |
| Bootstrap Icons | existing | Icon di card header | Sudah ada di project |
| jQuery (minimal) | existing | AJAX helper | Sudah ada di project via Bootstrap |
| EF Core | existing | Query aggregate | Sudah ada di project |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Vanilla fetch API | browser built-in | AJAX request ke JSON endpoint | Lebih modern daripada $.ajax; pattern CDP Dashboard menggunakan fetch |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Chart.js CDN | npm + bundler | CDN lebih sederhana, tidak perlu ubah build pipeline |
| Vanilla fetch | jQuery $.ajax | fetch lebih modern, tidak tambah dependency |

**Installation:**

Tidak ada npm install baru. Chart.js diload via CDN di halaman Analytics saja:

```html
<script src="https://cdn.jsdelivr.net/npm/chart.js@4/dist/chart.umd.min.js"></script>
```

---

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
└── CMPController.cs          # Tambah: AnalyticsDashboard(), GetAnalyticsData()

Models/ViewModels/
└── AnalyticsDashboardViewModel.cs  # ViewModel untuk initial page load (dropdown data)

Views/CMP/
├── Index.cshtml               # Edit: tambah card link Analytics Dashboard
└── AnalyticsDashboard.cshtml  # Baru: halaman utama dengan filter + 4 panel
```

### Pattern 1: Initial Page Load + AJAX Data

**What:** Controller action GET mengembalikan ViewModel dengan data dropdown (list Bagian, Kategori). Data chart/tabel tidak dimuat saat page load — AJAX fetch dipicu segera setelah halaman siap (DOMContentLoaded).

**When to use:** Semua dashboard dengan filter interaktif di project ini (lihat CDP Dashboard).

```csharp
// Source: pattern dari CDPController.Dashboard()
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> AnalyticsDashboard()
{
    var vm = new AnalyticsDashboardViewModel
    {
        Sections = await _context.GetAllSectionsAsync(),
        Categories = await _context.AssessmentCategories
            .Where(c => c.ParentId == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => c.Name)
            .ToListAsync()
    };
    return View(vm);
}
```

### Pattern 2: JSON Data Endpoint

**What:** Action terpisah yang menerima filter params dan mengembalikan `JsonResult` berisi semua data untuk 4 panel sekaligus. Satu call AJAX mengisi semua panel.

**When to use:** Filter global berlaku untuk semua panel (D-06).

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> GetAnalyticsData(
    string? bagian, string? unit, string? kategori, string? subKategori,
    DateTime? periodeStart, DateTime? periodeEnd)
{
    // Set default periode 1 tahun terakhir jika tidak diisi (D-10)
    periodeEnd ??= DateTime.Today;
    periodeStart ??= periodeEnd.Value.AddYears(-1);

    // Build base query dengan filter
    var sessionQuery = _context.AssessmentSessions
        .Include(s => s.User)
        .Where(s => s.IsPassed.HasValue && s.CompletedAt.HasValue
                 && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd);

    if (!string.IsNullOrEmpty(bagian))
        sessionQuery = sessionQuery.Where(s => s.User!.Section == bagian);
    if (!string.IsNullOrEmpty(unit))
        sessionQuery = sessionQuery.Where(s => s.User!.Unit == unit);
    if (!string.IsNullOrEmpty(kategori))
        sessionQuery = sessionQuery.Where(s => s.Category == kategori);

    // ... build 4 datasets, return Json(new { failRate, trend, etBreakdown, expiringSoon })
    return Json(result);
}
```

### Pattern 3: AJAX Fetch dengan AbortController

**What:** Vanilla fetch dengan AbortController untuk cancel request sebelumnya jika filter berubah cepat. Pattern ini persis sama dengan CDP Dashboard.

```javascript
// Source: Views/CDP/Dashboard.cshtml - refreshProtonContent()
var analyticsAbort = null;

function refreshAnalytics() {
    var container = document.getElementById('analytics-content');
    container.classList.add('dashboard-loading');

    if (analyticsAbort) analyticsAbort.abort();
    analyticsAbort = new AbortController();

    var params = new URLSearchParams(/* filter values */);

    fetch('/CMP/GetAnalyticsData?' + params, { signal: analyticsAbort.signal })
        .then(r => r.json())
        .then(data => {
            renderCharts(data);
            container.classList.remove('dashboard-loading');
        })
        .catch(e => {
            if (e.name !== 'AbortError') {
                console.error(e);
                container.classList.remove('dashboard-loading');
            }
        });
}

document.addEventListener('DOMContentLoaded', () => refreshAnalytics());
```

### Pattern 4: Chart.js Initialization dengan Destroy

**What:** Setiap kali AJAX kembali, chart lama harus di-destroy sebelum membuat yang baru. Simpan referensi chart di variable.

```javascript
// Source: Chart.js docs pattern
var failRateChart = null;

function renderFailRateChart(data) {
    if (failRateChart) failRateChart.destroy();
    var ctx = document.getElementById('failRateChart').getContext('2d');
    failRateChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.labels,
            datasets: [{
                label: 'Fail Rate (%)',
                data: data.values,
                backgroundColor: 'rgba(220, 53, 69, 0.7)'
            }]
        },
        options: {
            responsive: true,
            scales: { y: { beginAtZero: true, max: 100 } }
        }
    });
}
```

### Pattern 5: Heatmap Tabel Skor ET

**What:** Tabel HTML biasa dengan warna background di-set via inline style berdasarkan nilai skor. Tidak perlu library tambahan.

```javascript
// Hitung warna berdasarkan persentase (0-100)
function scoreToColor(pct) {
    if (pct >= 80) return 'rgba(40,167,69,0.2)';   // hijau
    if (pct >= 60) return 'rgba(255,193,7,0.2)';    // kuning
    return 'rgba(220,53,69,0.2)';                   // merah
}
```

### Anti-Patterns to Avoid

- **Memuat semua data di page load:** Akan lambat karena query besar. Gunakan AJAX trigger saat DOMContentLoaded.
- **Satu endpoint per panel:** Menyebabkan 4 request paralel dengan filter yang sama. Gunakan satu endpoint yang return semua data.
- **Tidak destroy chart sebelum re-render:** Chart.js akan throw error "Canvas is already in use" atau menumpuk canvas.
- **Mengubah `_Layout.cshtml` untuk Chart.js:** Load Chart.js hanya di halaman Analytics via `@section Scripts`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cascade dropdown Bagian→Unit | Custom JS DOM manipulation | `ApplicationDbContext.GetUnitsForSectionAsync()` + fetch `/CMP/GetAnalyticsData?cascadeUnits=true` | Helper sudah ada di DbContext Phase 221 |
| Kategori→SubKategori cascade | Custom query | `AssessmentCategory` ParentId hierarchy sudah ada | Filter SubKategori = WHERE ParentId = selectedKategori.Id |
| Loading overlay | Custom CSS animation | `.dashboard-loading` CSS dari CDP Dashboard | Copy-paste dari `Views/CDP/Dashboard.cshtml` baris 7-24 |
| Bar/Line chart | Custom SVG/Canvas | Chart.js via CDN | 100+ edge cases (responsive, tooltip, legend, accessibility) |
| Sertifikat expired query | Custom logic | `TrainingRecord.IsExpiringSoon` computed property sudah ada | Property sudah ada di model, tinggal query |

**Key insight:** Hampir semua building blocks sudah ada. Phase ini lebih banyak wiring daripada building.

---

## Common Pitfalls

### Pitfall 1: AssessmentSession tidak memiliki FK ke Bagian/Unit

**What goes wrong:** `AssessmentSession` tidak punya field `Section`/`Unit` secara langsung — hanya punya `UserId`. Filter Bagian/Unit harus JOIN ke `ApplicationUser`.

**Why it happens:** Section/Unit tersimpan di `ApplicationUser`, bukan di session snapshot.

**How to avoid:** Query harus `.Include(s => s.User)` atau JOIN eksplisit. Perhatikan null-safety karena `User` navigation bisa null jika user dihapus.

**Warning signs:** Filter Bagian tidak berpengaruh ke hasil.

### Pitfall 2: AssessmentSession.Category adalah string bebas, bukan FK ke AssessmentCategory

**What goes wrong:** Field `Category` di `AssessmentSession` adalah string (e.g., "Assessment OJ", "IHT"), bukan FK ke tabel `AssessmentCategories`. Filter Kategori tidak bisa pakai JOIN langsung.

**Why it happens:** AssessmentCategory ditambahkan belakangan (Phase 190), sesi lama menyimpan category sebagai string.

**How to avoid:** Filter Kategori menggunakan `WHERE s.Category == kategori` (string comparison), bukan FK join. Dropdown Kategori di filter form diisi dari `AssessmentCategories.Name` tapi matching ke `AssessmentSession.Category` via string.

**Warning signs:** Filter Kategori tidak memfilter apapun.

### Pitfall 3: ANLT-04 — Sertifikat expired ada di DUA tabel

**What goes wrong:** Sertifikat bisa berasal dari `TrainingRecord` (manual import) ATAU dari `AssessmentSession` (dengan `GenerateCertificate = true`). Query hanya ke satu tabel akan miss data.

**Why it happens:** Dua jalur pencatatan sertifikat yang berbeda asal-usulnya.

**How to avoid:** Query ke dua sumber terpisah dan union hasilnya:
1. `TrainingRecord` WHERE `Status = "Valid"` AND `ValidUntil BETWEEN today AND today+30`
2. `AssessmentSession` WHERE `IsPassed = true` AND `GenerateCertificate = true` AND `ValidUntil BETWEEN today AND today+30`

Kolom "Nama Sertifikat": gunakan `TrainingRecord.Judul` atau `AssessmentSession.Title`.

**Warning signs:** Daftar sertifikat expired tampak tidak lengkap dibanding data yang diketahui.

### Pitfall 4: Chart.js tidak re-render jika canvas element di-replace via innerHTML

**What goes wrong:** Jika AJAX response mengganti innerHTML container yang berisi `<canvas>`, Chart.js kehilangan referensi ke canvas lama, tapi chart baru juga gagal karena canvas baru belum di-mount sempurna.

**Why it happens:** Pattern CDP Dashboard mengganti seluruh innerHTML. Untuk Chart.js, canvas harus persisten.

**How to avoid:** Jangan gunakan innerHTML replace untuk container chart. Biarkan `<canvas>` element tetap di DOM. AJAX hanya mengambil data JSON, render ulang chart via JavaScript (Pattern 4 di atas).

**Alternatif arsitektur:** Gunakan dua container terpisah:
- Container chart (canvas persisten, diupdate via Chart.js API)
- Container tabel (innerHTML replace aman)

### Pitfall 5: SessionElemenTeknisScore hanya ada untuk sesi package path

**What goes wrong:** Phase 223 hanya persist ET scores untuk package path. Sesi legacy (AssessmentQuestion path) tidak punya data di `SessionElemenTeknisScores`. Query ANLT-03 mungkin return data lebih sedikit dari yang diharapkan.

**Why it happens:** Keputusan di Phase 223 (lihat STATE.md): "ET persist di package path only".

**How to avoid:** Dokumentasikan limitasi ini di UI (note kecil: "Data hanya tersedia untuk assessment dengan paket soal"). Jangan error jika data kosong — tampilkan empty state.

---

## Code Examples

### Query ANLT-01: Fail Rate per Section dan Category

```csharp
// Source: EF Core GroupBy pattern
var failRateData = await _context.AssessmentSessions
    .Include(s => s.User)
    .Where(s => s.IsPassed.HasValue && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd
             && (string.IsNullOrEmpty(kategori) || s.Category == kategori))
    .GroupBy(s => new { s.User!.Section, s.Category })
    .Select(g => new {
        Section = g.Key.Section ?? "Unknown",
        Category = g.Key.Category,
        Total = g.Count(),
        Failed = g.Count(s => s.IsPassed == false)
    })
    .ToListAsync();
```

### Query ANLT-02: Trend Pass/Fail per Bulan

```csharp
// Source: EF Core GroupBy + DateTime pattern
var trendData = await _context.AssessmentSessions
    .Where(s => s.IsPassed.HasValue && s.CompletedAt >= periodeStart && s.CompletedAt <= periodeEnd)
    .GroupBy(s => new {
        Year = s.CompletedAt!.Value.Year,
        Month = s.CompletedAt!.Value.Month
    })
    .Select(g => new {
        g.Key.Year,
        g.Key.Month,
        Passed = g.Count(s => s.IsPassed == true),
        Failed = g.Count(s => s.IsPassed == false)
    })
    .OrderBy(x => x.Year).ThenBy(x => x.Month)
    .ToListAsync();
```

### Query ANLT-03: ET Score Aggregate per Kategori

```csharp
// Source: EF Core join + GroupBy
var etData = await _context.SessionElemenTeknisScores
    .Include(e => e.AssessmentSession)
    .Where(e => e.AssessmentSession.CompletedAt >= periodeStart
             && e.AssessmentSession.CompletedAt <= periodeEnd
             && (string.IsNullOrEmpty(kategori) || e.AssessmentSession.Category == kategori))
    .GroupBy(e => new { e.ElemenTeknis, e.AssessmentSession.Category })
    .Select(g => new {
        g.Key.ElemenTeknis,
        g.Key.Category,
        AvgPct = g.Average(e => e.QuestionCount > 0
            ? (double)e.CorrectCount / e.QuestionCount * 100 : 0),
        MinPct = g.Min(e => e.QuestionCount > 0
            ? (double)e.CorrectCount / e.QuestionCount * 100 : 0),
        MaxPct = g.Max(e => e.QuestionCount > 0
            ? (double)e.CorrectCount / e.QuestionCount * 100 : 0),
        SampleCount = g.Count()
    })
    .ToListAsync();
```

### Query ANLT-04: Sertifikat Expired dalam 30 Hari

```csharp
// Source: Model TrainingRecord.IsExpiringSoon pattern
var today = DateTime.Today;
var cutoff = today.AddDays(30);

// Source 1: TrainingRecords
var expFromTraining = await _context.TrainingRecords
    .Include(t => t.User)
    .Where(t => t.Status == "Valid"
             && t.ValidUntil.HasValue
             && t.ValidUntil >= today
             && t.ValidUntil <= cutoff)
    .Select(t => new ExpiringSoonDto {
        NamaPekerja = t.User!.FullName,
        NamaSertifikat = t.Judul ?? "-",
        TanggalExpired = t.ValidUntil!.Value,
        SectionUnit = (t.User.Section ?? "") + " / " + (t.User.Unit ?? "")
    })
    .ToListAsync();

// Source 2: AssessmentSessions dengan sertifikat
var expFromSession = await _context.AssessmentSessions
    .Include(s => s.User)
    .Where(s => s.IsPassed == true
             && s.GenerateCertificate
             && s.ValidUntil.HasValue
             && s.ValidUntil >= today
             && s.ValidUntil <= cutoff)
    .Select(s => new ExpiringSoonDto {
        NamaPekerja = s.User!.FullName,
        NamaSertifikat = s.Title,
        TanggalExpired = s.ValidUntil!.Value,
        SectionUnit = (s.User.Section ?? "") + " / " + (s.User.Unit ?? "")
    })
    .ToListAsync();

var combined = expFromTraining.Concat(expFromSession)
    .OrderBy(x => x.TanggalExpired)
    .ToList();
```

### Cascade Dropdown Bagian → Unit (AJAX)

```javascript
// Source: pattern dari CDPController.GetCascadeOptions
document.getElementById('filterBagian').addEventListener('change', function() {
    var bagian = this.value;
    var unitSelect = document.getElementById('filterUnit');
    unitSelect.innerHTML = '<option value="">Semua Unit</option>';
    if (!bagian) return;

    fetch('/CMP/GetAnalyticsCascadeUnits?bagian=' + encodeURIComponent(bagian))
        .then(r => r.json())
        .then(units => {
            units.forEach(u => {
                var opt = document.createElement('option');
                opt.value = u; opt.textContent = u;
                unitSelect.appendChild(opt);
            });
        });
    refreshAnalytics();
});
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| OrganizationStructure static class | OrganizationUnits DB table | Phase 222 | Dropdown Bagian/Unit harus query DB, bukan static list |
| Wait Certificate status | Status dihapus, dimigrasikan ke Passed | Phase 223 | Query ANLT-04 cukup check Status = "Valid" |

---

## Open Questions

1. **Filter Bagian/Unit pada AssessmentSession — apakah User.Section/Unit selalu terisi?**
   - What we know: `ApplicationUser.Section` dan `Unit` adalah nullable string
   - What's unclear: Berapa persen user yang belum diisi Section/Unit-nya di data production
   - Recommendation: Grouping harus handle null (gunakan `?? "Tidak Diketahui"` atau filter null di query)

2. **Filter Bagian/Unit pada ANLT-04 (sertifikat expired)**
   - What we know: Kedua sumber (TrainingRecord, AssessmentSession) join ke User untuk mendapat Section/Unit
   - What's unclear: TrainingRecord tidak memiliki FK ke AssessmentSession.Category — filter Kategori tidak bisa diterapkan ke TrainingRecord (Kategori ≠ AssessmentCategory)
   - Recommendation: Filter Kategori hanya berlaku untuk panel ANLT-01/02/03; panel ANLT-04 cukup filter Bagian/Unit saja

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework configured) |
| Config file | none |
| Quick run command | Manual: open browser, test filter interactions |
| Full suite command | Manual: test all 4 panels + filter cascade + empty states |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ANLT-01 | Bar chart fail rate tampil dengan data valid | manual | n/a | n/a |
| ANLT-02 | Line chart trend tampil, filter periode berpengaruh | manual | n/a | n/a |
| ANLT-03 | Tabel ET breakdown tampil, heatmap warna correct | manual | n/a | n/a |
| ANLT-04 | Tabel sertifikat expired 30 hari tampil, data dari dua sumber | manual | n/a | n/a |

### Sampling Rate

- **Per task commit:** Build sukses (`dotnet build`)
- **Per wave merge:** Manual browser smoke test semua 4 panel
- **Phase gate:** Full manual UAT sebelum `/gsd:verify-work`

### Wave 0 Gaps

None — tidak ada test framework yang perlu dikonfigurasi. Project menggunakan manual browser testing.

---

## Sources

### Primary (HIGH confidence)

- `Models/SessionElemenTeknisScore.cs` — struktur data ET scores (Phase 223)
- `Models/AssessmentSession.cs` — field IsPassed, CompletedAt, ValidUntil, Category, GenerateCertificate
- `Models/TrainingRecord.cs` — field ValidUntil, Status, IsExpiringSoon computed property
- `Models/ApplicationUser.cs` — field Section, Unit (untuk filter Bagian/Unit)
- `Models/OrganizationUnit.cs` — hierarki Bagian/Unit dengan ParentId
- `Models/AssessmentCategory.cs` — hierarki Kategori/SubKategori dengan ParentId
- `Data/ApplicationDbContext.cs` — helper `GetAllSectionsAsync()`, `GetUnitsForSectionAsync()` (Phase 221)
- `Views/CDP/Dashboard.cshtml` — loading state CSS + AJAX fetch pattern canonical reference
- `Views/CMP/Index.cshtml` — CMP Hub card pattern (untuk menambah card Analytics)

### Secondary (MEDIUM confidence)

- Chart.js v4 CDN pattern: `https://cdn.jsdelivr.net/npm/chart.js@4/dist/chart.umd.min.js`
- Chart.js destroy-before-reinit pattern: standard documented pattern

### Tertiary (LOW confidence)

- Tidak ada

---

## Metadata

**Confidence breakdown:**

- Standard stack: HIGH — semua library sudah ada di project kecuali Chart.js yang dipilih eksplisit di D-04
- Architecture: HIGH — pattern CDP Dashboard sudah terbukti di project, query patterns dari existing code
- Pitfalls: HIGH — pitfall 1-4 ditemukan langsung dari membaca kode, pitfall 5 dari STATE.md

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (30 hari — stack stable)
