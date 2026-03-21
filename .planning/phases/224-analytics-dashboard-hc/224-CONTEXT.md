# Phase 224: Analytics Dashboard HC - Context

**Gathered:** 2026-03-22
**Status:** Ready for planning

<domain>
## Phase Boundary

HC memiliki halaman Analytics Dashboard dengan 4 visualisasi data assessment dan sertifikat yang actionable: fail rate per section/category, trend assessment pass/fail, breakdown skor ET aggregate, dan ringkasan sertifikat expired 30 hari.

</domain>

<decisions>
## Implementation Decisions

### Penempatan & Navigasi
- **D-01:** Dashboard diakses via card/link di CMP Hub (CMP/Index) — sejajar dengan Assessment, Records, dll
- **D-02:** Akses dibatasi untuk role Admin dan HC — `[Authorize(Roles = "Admin, HC")]`

### Layout Dashboard
- **D-03:** Grid 2×2 — 4 card/chart dalam 2 kolom, compact, semua terlihat tanpa banyak scroll
- **D-04:** Fail rate = bar chart (per section/category), Trend = line chart (time series) menggunakan Chart.js
- **D-05:** Breakdown skor ET = tabel dengan heatmap warna (merah=rendah, hijau=tinggi), baris=kategori, kolom=rata-rata/min/max/distribusi

### Filter & Interaktivitas
- **D-06:** Filter global di atas halaman — satu set filter berlaku untuk semua 4 visualisasi
- **D-07:** 4 dropdown filter + date range: Bagian, Unit (OrganizationUnit), Kategori, SubKategori, Periode
- **D-08:** Dropdown cascade: Bagian → Unit ter-filter, Kategori → SubKategori ter-filter
- **D-09:** AJAX partial reload saat filter berubah — tanpa full page reload, mirip pattern CDP Dashboard
- **D-10:** Default state: tampilkan semua data, periode 1 tahun terakhir

### Sertifikat Expired
- **D-11:** Hanya tampilkan sertifikat expired dalam 30 hari ke depan (bukan 30/60/90)
- **D-12:** Ringkasan saja, tanpa link ke detail pekerja
- **D-13:** 4 kolom: Nama Pekerja, Nama Sertifikat, Tanggal Expired, Section/Unit

### Claude's Discretion
- Chart.js configuration details (colors, tooltips, responsive options)
- Loading skeleton/spinner design saat AJAX fetch
- Empty state handling jika tidak ada data
- Exact spacing, typography, card styling

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Data Sources
- `Models/SessionElemenTeknisScore.cs` — Model skor ET per session (Phase 223), data source ANLT-03
- `Models/TrainingRecord.cs` — Field ValidUntil/expired date, data source ANLT-04
- `Models/AssessmentSession.cs` — Assessment results, data source ANLT-01/ANLT-02

### Existing Patterns
- `Views/CDP/Dashboard.cshtml` — CDP Dashboard pattern: loading state, AJAX partial reload, layout reference
- `Controllers/CDPController.cs` — Dashboard action pattern, ViewModel pattern
- `Views/CMP/Index.cshtml` — CMP Hub page where new dashboard link will be added
- `Controllers/CMPController.cs` — CMP controller where new action will be added

### Organization Structure
- `Data/ApplicationDbContext.cs` — OrganizationUnit model, Bagian/Unit hierarchy for cascade filter

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- CDP Dashboard (`Views/CDP/Dashboard.cshtml`): loading state pattern (`.dashboard-loading` CSS class), AJAX reload pattern
- Bootstrap 5 + Bootstrap Icons: existing UI framework
- OrganizationUnit: database-driven hierarchy (Phase 219-222), ready for Bagian/Unit dropdown

### Established Patterns
- Server-side Razor views with jQuery AJAX for partial updates
- ViewModel per halaman (e.g., `CDPDashboardViewModel`, `DashboardHomeViewModel`)
- `[Authorize(Roles = "Admin, HC")]` pattern untuk HC-only pages

### Integration Points
- CMP/Index hub page: tambah card link ke Analytics Dashboard
- CMPController: tambah action AnalyticsDashboard + JSON endpoint untuk AJAX data
- Chart.js: library baru, belum ada di project — perlu ditambahkan via CDN atau npm

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 224-analytics-dashboard-hc*
*Context gathered: 2026-03-22*
