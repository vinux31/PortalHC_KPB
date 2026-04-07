# Phase 301: Advanced Reporting - Context

**Gathered:** 2026-04-07
**Status:** Ready for planning

<domain>
## Phase Boundary

HC dapat melihat kualitas soal secara statistik (item analysis), tren gain score Pre-Post, dan perbandingan antar kelompok — semua dapat diekspor ke Excel. Fitur ditambahkan ke Analytics Dashboard yang sudah ada di /CMP/AnalyticsDashboard.

</domain>

<decisions>
## Implementation Decisions

### Item Analysis UI
- **D-01:** Item Analysis ditampilkan sebagai tab baru di Analytics Dashboard (bersama Fail Rate, Trend, ET Breakdown)
- **D-02:** Reuse filter Bagian/Unit/Kategori yang sudah ada + dropdown pilih Assessment
- **D-03:** Per soal menampilkan: difficulty index (p-value), discrimination index (Kelley upper/lower 27%)
- **D-04:** Discrimination index dengan warning < 30 responden: inline badge kuning "Data belum cukup (N<30)" di samping nilai, nilai tetap ditampilkan tapi di-gray-out
- **D-05:** Distractor analysis: tabel opsi per soal — Opsi | Jumlah pemilih | Persentase | Highlight jawaban benar

### Gain Score Report
- **D-06:** Gain Score Report ditampilkan sebagai tab baru di Analytics Dashboard
- **D-07:** Dua view dalam tab: (1) Tabel per pekerja: Pre Score | Post Score | Gain Score, (2) Tabel per elemen kompetensi: Avg Pre | Avg Post | Avg Gain
- **D-08:** Hanya muncul untuk assessment bertipe PrePostTest

### Export Excel
- **D-09:** File terpisah per report — tombol "Export" di masing-masing tab (Item Analysis dan Gain Score) menghasilkan file .xlsx sendiri
- **D-10:** Styling profesional: header berwarna, border, auto-fit column width, freeze header row — menggunakan ExcelExportHelper + ClosedXML yang sudah ada

### Dashboard Gain Score Trend
- **D-11:** Line chart tren gain score per bulan ditambahkan di tab Trend yang sudah ada (di bawah chart trend lulus/gagal)
- **D-12:** Sumbu X = bulan, sumbu Y = rata-rata gain score assessment PrePostTest

### Claude's Discretion
- Exact UI spacing dan typography di tab baru
- Loading state dan skeleton design
- Error handling untuk assessment tanpa data PrePostTest
- Color coding untuk interpretasi p-value (mudah/sedang/sulit)
- Perbandingan antar kelompok (RPT-07) — cara grouping dan visualisasi

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Analytics Dashboard (existing)
- `Views/CMP/AnalyticsDashboard.cshtml` — Existing dashboard view dengan tab structure dan filter
- `Models/AnalyticsDashboardViewModel.cs` — Existing ViewModel dan JSON response classes
- `Controllers/CMPController.cs` — Endpoint GetAnalyticsData dan AnalyticsDashboard action

### Assessment & PrePostTest
- `Controllers/AssessmentAdminController.cs` — PrePostTest flow, AssessmentType handling, session grouping logic
- `Models/AssessmentMonitoringViewModel.cs` — Assessment monitoring models

### Excel Export
- `Helpers/ExcelExportHelper.cs` — Existing Excel export helper menggunakan ClosedXML
- `Models/ReportsDashboardViewModel.cs` — Existing report models

### Requirements
- `.planning/REQUIREMENTS.md` §RPT-01 through RPT-07 — Full requirement definitions

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ExcelExportHelper.cs`: Helper class untuk ClosedXML export — reuse untuk Item Analysis dan Gain Score export
- `AnalyticsDashboard.cshtml`: Tab structure dengan JS-based tab switching — extend dengan tab baru
- Filter bar (Bagian/Unit/Kategori): Sudah ada dengan cascading dropdown — reuse sepenuhnya
- Chart.js: Sudah dipakai untuk Trend dan Fail Rate chart — reuse untuk gain score trend line chart

### Established Patterns
- Analytics data di-fetch via AJAX GET ke `/CMP/GetAnalyticsData` dengan filter parameters — ikuti pattern yang sama untuk data baru
- Tab switching via JavaScript tanpa page reload — konsisten untuk tab baru
- ClosedXML 0.105.0 untuk semua Excel export di project

### Integration Points
- Tab baru ditambahkan ke `AnalyticsDashboard.cshtml` tab bar
- Endpoint baru di `CMPController.cs` untuk GetItemAnalysisData dan GetGainScoreData
- Chart gain score trend ditambahkan ke panel Trend yang sudah ada
- Export endpoints baru di CMPController untuk download Excel

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

*Phase: 301-advanced-reporting*
*Context gathered: 2026-04-07*
