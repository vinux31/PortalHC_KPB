---
phase: 301-advanced-reporting
verified: 2026-04-07T10:45:00Z
status: human_needed
score: 10/10 must-haves verified
re_verification: false
human_verification:
  - test: "Buka Analytics Dashboard di browser, aktifkan tab Item Analysis, pilih assessment dari dropdown, klik Terapkan Filter"
    expected: "Tabel soal tampil dengan kolom p-value, badge Mudah/Sedang/Sulit, discrimination index, N Responden. Badge kuning 'Data belum cukup (N<30)' muncul jika responden < 30."
    why_human: "Membutuhkan data test nyata di DB dan browser rendering; tidak bisa diverifikasi via grep"
  - test: "Klik salah satu baris soal di tab Item Analysis"
    expected: "Panel distractor analysis muncul/tersembunyi (collapsible). Opsi jawaban benar di-highlight hijau dengan badge 'Jawaban Benar'."
    why_human: "Interaksi DOM dinamis (toggle) membutuhkan browser"
  - test: "Aktifkan tab Gain Score Report, pilih assessment, klik Terapkan Filter"
    expected: "Tabel Per Pekerja (Nama, NIP, Pre, Post, Gain Score) tampil. Toggle ke 'Per Elemen Kompetensi' tanpa AJAX re-fetch. Tabel Group Comparison per Bagian tampil di bawah."
    why_human: "Membutuhkan data pre-post test berpasangan di DB dan browser rendering"
  - test: "Klik tombol 'Export Item Analysis' di tab Item Analysis dengan assessment terpilih"
    expected: "File .xlsx terunduh. File memiliki 2 sheet: 'Item Analysis' dan 'Distractor Analysis'. Header berwarna biru, freeze row 1."
    why_human: "Perlu membuka file Excel dan memverifikasi format secara visual"
  - test: "Klik tombol 'Export Gain Score' di tab Gain Score dengan assessment terpilih"
    expected: "File .xlsx terunduh dengan 2 sheet: 'Per Pekerja' dan 'Per Elemen Kompetensi'. Header biru, freeze row 1. Jawaban benar di-highlight hijau di sheet Distractor."
    why_human: "Perlu membuka file Excel dan memverifikasi format secara visual"
  - test: "Aktifkan tab Trend di Analytics Dashboard setelah memilih periode yang mengandung data Pre-Post Test"
    expected: "Line chart 'Rata-rata Gain Score per Bulan' tampil di bawah chart lulus/gagal dengan warna ungu #6f42c1. Tooltip menampilkan sample count."
    why_human: "Membutuhkan data gain score di DB; chart rendering tidak bisa diverifikasi via grep"
  - test: "Verifikasi empty state saat tab Item Analysis atau Gain Score aktif tanpa assessment terpilih"
    expected: "Pesan 'Pilih assessment terlebih dahulu' tampil. Dropdown 'Pilih Assessment' muncul di filter bar. Dropdown tersembunyi di tab lain."
    why_human: "Kondisi UI dinamis — show/hide filterAssessmentGroup berdasarkan tab aktif"
  - test: "Verifikasi 4 tab existing (Fail Rate, Trend, Skor Elemen Teknis, Sertifikat Expired) tidak regression setelah konversi ke nav-tabs"
    expected: "Semua chart dan tabel 4 tab lama tetap berfungsi normal"
    why_human: "Regression check membutuhkan data rendering di browser"
---

# Phase 301: Advanced Reporting — Laporan Verifikasi

**Phase Goal:** HC dapat melihat kualitas soal secara statistik (item analysis), tren gain score Pre-Post, dan perbandingan antar kelompok — semua dapat diekspor ke Excel
**Verified:** 2026-04-07T10:45:00Z
**Status:** HUMAN_NEEDED
**Re-verification:** Tidak — verifikasi awal

---

## Ringkasan Pencapaian Goal

Semua komponen teknis telah diimplementasikan dan terverifikasi di codebase. Verifikasi visual dan fungsional di browser diperlukan untuk konfirmasi akhir.

---

## Observable Truths

| # | Truth | Status | Bukti |
|---|-------|--------|-------|
| 1 | Endpoint GetItemAnalysisData mengembalikan JSON dengan p-value, discrimination index, dan distractor data per soal | ✓ VERIFIED | `CMPController.cs:2640` — endpoint ada, p-value dihitung, Kelley D-index via `CalculateKelleyDiscrimination`, distractor loop pada opsi MC/TF |
| 2 | Endpoint GetGainScoreData mengembalikan JSON dengan gain score per pekerja dan per elemen kompetensi | ✓ VERIFIED | `CMPController.cs:2758` — PerWorker, PerElemen, GroupComparison di-populate dan di-return via `Json(new GainScoreResult {...})` |
| 3 | Endpoint GetPrePostAssessmentList mengembalikan daftar assessment PrePostTest yang completed | ✓ VERIFIED | `CMPController.cs:2610` — query AssessmentSessions dengan filter `AssessmentType == "PreTest"` dan `LinkedGroupId.HasValue` |
| 4 | Warning N<30 ditandai dengan property IsLowN=true pada response | ✓ VERIFIED | `CMPController.cs:2716,2724` — `IsLowN = totalResponden < 30` di ItemAnalysisRow dan ItemAnalysisResult |
| 5 | Group comparison per Bagian tersedia di GainScoreResult | ✓ VERIFIED | `CMPController.cs:2831` — `GroupComparisonItem` di-populate dari `.GroupBy(g => g.Section)` pada perWorker list |
| 6 | Tab Item Analysis tampil di Analytics Dashboard dengan tabel soal dan badge Mudah/Sedang/Sulit | ✓ VERIFIED | `AnalyticsDashboard.cshtml:244,722` — `#tabItemAnalysis` ada, `renderItemAnalysis` function ada, badge `bg-success">Mudah` ada |
| 7 | Tab Gain Score Report tampil dengan tabel per pekerja, per elemen, dan group comparison | ✓ VERIFIED | `AnalyticsDashboard.cshtml:285,320,336,342` — `#tabGainScore`, `gainWorkerBody`, `gainElemenBody`, `groupComparisonBody`, "Perbandingan Antar Kelompok" semua present |
| 8 | HC dapat klik Export Item Analysis dan mendapat file .xlsx dengan header berwarna, freeze row, auto-fit | ✓ VERIFIED | `CMPController.cs:2855,2883-2884` — `ExportItemAnalysisExcel`, `FreezeRows(1)`, `SetBackgroundColor(XLColor.FromHtml("#0d6efd"))`, `ExcelExportHelper.ToFileResult` |
| 9 | HC dapat klik Export Gain Score dan mendapat file .xlsx terpisah | ✓ VERIFIED | `CMPController.cs:2953,2975-2976` — `ExportGainScoreExcel`, header biru, FreezeRows, 2 sheet (Per Pekerja + Per Elemen Kompetensi) |
| 10 | Line chart Gain Score Trend tampil di tab Trend di bawah chart lulus/gagal | ✓ VERIFIED | `AnalyticsDashboard.cshtml:177,563,590` — canvas `gainScoreTrendChart`, function `renderGainScoreTrend`, borderColor `#6f42c1`, dipanggil via `renderGainScoreTrend(data.gainScoreTrend)` baris 401 |

**Skor:** 10/10 truths terverifikasi secara programatik

---

## Required Artifacts

| Artifact | Expected | Status | Detail |
|----------|----------|--------|--------|
| `Models/AnalyticsDashboardViewModel.cs` | ItemAnalysisResult, GainScoreResult, model pendukung | ✓ VERIFIED | `ItemAnalysisResult` baris 75, `GainScoreResult` baris 105, `DistractorRow` baris 94, `GainScoreTrendItem` baris 152, `GainScoreTrend` property baris 21 |
| `Controllers/CMPController.cs` | GetItemAnalysisData, GetGainScoreData, GetPrePostAssessmentList, ExportItemAnalysisExcel, ExportGainScoreExcel | ✓ VERIFIED | Semua 5 endpoint ada pada baris 2610, 2640, 2758, 2855, 2953 |
| `Views/CMP/AnalyticsDashboard.cshtml` | 6 nav-tabs, Item Analysis tab, Gain Score tab, dropdown assessment, gain score trend chart, export JS | ✓ VERIFIED | `#analyticsTabs` baris 104, semua tab pane, semua JS functions, canvas chart baris 177 |

---

## Key Link Verification

| From | To | Via | Status | Detail |
|------|----|-----|--------|--------|
| `AnalyticsDashboard.cshtml` | `/CMP/GetItemAnalysisData` | fetch AJAX | ✓ WIRED | baris 429: `fetch(appUrl('/CMP/GetItemAnalysisData?...'))`, `.then(renderItemAnalysis)` |
| `AnalyticsDashboard.cshtml` | `/CMP/GetGainScoreData` | fetch AJAX | ✓ WIRED | baris 446: `fetch(appUrl('/CMP/GetGainScoreData?...'))`, `.then(renderGainScore)` |
| `AnalyticsDashboard.cshtml` | `/CMP/ExportItemAnalysisExcel` | window.location.href | ✓ WIRED | baris 845: `window.location.href = appUrl('/CMP/ExportItemAnalysisExcel?...')` |
| `CMPController.cs` | `Models/AnalyticsDashboardViewModel.cs` | return Json(new ItemAnalysisResult) | ✓ WIRED | baris 2724 — `Json(new ItemAnalysisResult { ... })` |
| `CMPController.cs` | `GetAnalyticsData` | GainScoreTrend property | ✓ WIRED | baris 2582 — `GainScoreTrend = gainScoreTrend` dalam return statement `AnalyticsDataResult` |

---

## Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Menghasilkan Data Riil | Status |
|----------|--------------|--------|----------------------|--------|
| `AnalyticsDashboard.cshtml` renderItemAnalysis | `data.items` | `GetItemAnalysisData` endpoint | Ya — query `PackageUserResponses` + `PackageQuestion` + `Options` dari DB | ✓ FLOWING |
| `AnalyticsDashboard.cshtml` renderGainScore | `data.perWorker`, `data.perElemen`, `data.groupComparison` | `GetGainScoreData` endpoint | Ya — query `AssessmentSessions` Pre+Post + `SessionElemenTeknisScores` dari DB | ✓ FLOWING |
| `AnalyticsDashboard.cshtml` renderGainScoreTrend | `data.gainScoreTrend` | `GetAnalyticsData` endpoint (diperluas) | Ya — query AssessmentSessions PostTest + pre-score lookup dari DB | ✓ FLOWING |

---

## Behavioral Spot-Checks

Step 7b: SKIPPED — endpoint membutuhkan server yang berjalan dan data di database. Tidak dapat diverifikasi tanpa memulai aplikasi.

---

## Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Bukti |
|-------------|-------------|-----------|--------|-------|
| RPT-01 | Plan 01 | Item Analysis — difficulty index (p-value) per soal | ✓ SATISFIED | `DifficultyIndex = Math.Round(pValue, 3)` di `GetItemAnalysisData`, ditampilkan di kolom "Tingkat Kesulitan" view |
| RPT-02 | Plan 01 | Discrimination index (Kelley upper/lower 27%) dengan warning < 30 responden | ✓ SATISFIED | `CalculateKelleyDiscrimination` (upper/lower 27%), `IsLowN = totalResponden < 30`, badge "Data belum cukup (N<30)" di view |
| RPT-03 | Plan 01 | Distractor analysis — persentase per opsi per soal | ✓ SATISFIED | `DistractorRow` dengan `Count` dan `Percent`, collapsible distractor table di view dengan highlight jawaban benar |
| RPT-04 | Plan 01 | Pre-Post Gain Score Report per pekerja dan per elemen kompetensi | ✓ SATISFIED | `GetGainScoreData` menghasilkan `PerWorker` dan `PerElemen`, formula normalized `(Post-Pre)/(100-Pre)*100` |
| RPT-05 | Plan 03 | Export Item Analysis dan Gain Score Report ke Excel | ✓ SATISFIED | `ExportItemAnalysisExcel` (2 sheet) dan `ExportGainScoreExcel` (2 sheet) dengan header biru dan freeze row. REQUIREMENTS.md status: Complete |
| RPT-06 | Plan 03 | Analytics Dashboard panel tren gain score | ✓ SATISFIED | `gainScoreTrendChart` canvas, `renderGainScoreTrend` function, `GainScoreTrend` property di `AnalyticsDataResult`. REQUIREMENTS.md status: Complete |
| RPT-07 | Plan 01, 02 | Perbandingan antar kelompok (group comparison) | ✓ SATISFIED | `GroupComparisonItem` model, `.GroupBy(g => g.Section)` di `GetGainScoreData`, tabel "Perbandingan Antar Kelompok (Bagian)" di view |

**Catatan REQUIREMENTS.md:** RPT-01 s/d RPT-04 dan RPT-07 masih ditandai "Pending" di REQUIREMENTS.md meskipun implementasi sudah ada. RPT-05 dan RPT-06 sudah ditandai "Complete". File REQUIREMENTS.md perlu diperbarui setelah verifikasi human selesai.

---

## Anti-Patterns Found

Tidak ada blocker atau stub yang ditemukan. Semua endpoint mengembalikan data riil dari database (bukan return `[]` atau `{}` statis).

| File | Pola | Severity | Keterangan |
|------|------|----------|------------|
| Tidak ada | — | — | Tidak ada anti-pattern ditemukan |

---

## Human Verification Required

### 1. Tab Item Analysis — Rendering dan Interaksi

**Test:** Buka `/CMP/AnalyticsDashboard`, aktifkan tab "Item Analysis", pilih assessment dari dropdown, klik "Terapkan Filter".
**Expected:** Tabel soal tampil dengan p-value, badge Mudah/Sedang/Sulit, discrimination index. Badge kuning "Data belum cukup (N<30)" muncul jika N < 30.
**Why human:** Membutuhkan data test di DB dan browser rendering.

### 2. Distractor Analysis — Collapsible Toggle

**Test:** Klik salah satu baris soal di tab Item Analysis.
**Expected:** Panel distractor muncul/tersembunyi. Opsi jawaban benar di-highlight hijau dengan badge "Jawaban Benar".
**Why human:** Interaksi DOM dinamis membutuhkan browser.

### 3. Tab Gain Score Report — Data dan Toggle View

**Test:** Aktifkan tab "Gain Score Report", pilih assessment, klik "Terapkan Filter". Klik toggle "Per Elemen Kompetensi".
**Expected:** Tabel Per Pekerja tampil. Toggle ke Per Elemen tanpa AJAX. Tabel Group Comparison per Bagian tampil di bawah.
**Why human:** Membutuhkan data pre-post berpasangan di DB.

### 4. Export Item Analysis Excel

**Test:** Di tab Item Analysis dengan data tampil, klik "Export Item Analysis".
**Expected:** Download file `.xlsx` dengan 2 sheet: "Item Analysis" dan "Distractor Analysis". Header biru, freeze row 1.
**Why human:** Perlu membuka file Excel untuk verifikasi format.

### 5. Export Gain Score Excel

**Test:** Di tab Gain Score dengan data tampil, klik "Export Gain Score".
**Expected:** Download file `.xlsx` dengan 2 sheet: "Per Pekerja" dan "Per Elemen Kompetensi". Header biru, freeze row 1.
**Why human:** Perlu membuka file Excel untuk verifikasi format.

### 6. Gain Score Trend Chart di Tab Trend

**Test:** Aktifkan tab "Trend" di Analytics Dashboard setelah filter periode yang mengandung data Pre-Post Test.
**Expected:** Line chart ungu "Rata-rata Gain Score per Bulan" tampil di bawah chart lulus/gagal. Tooltip menampilkan sample count.
**Why human:** Membutuhkan data gain score di DB; chart rendering tidak bisa diverifikasi via grep.

### 7. Dropdown Pilih Assessment — Show/Hide Behavior

**Test:** Pindah antar tab di Analytics Dashboard.
**Expected:** Dropdown "Pilih Assessment" hanya muncul saat tab Item Analysis atau Gain Score aktif. Di tab lain (Fail Rate, Trend, dll) dropdown tersembunyi.
**Why human:** Kondisi UI dinamis berdasarkan tab aktif.

### 8. Regression Check — 4 Tab Existing

**Test:** Verifikasi tab Fail Rate, Trend, Skor Elemen Teknis, Sertifikat Expired setelah konversi ke Bootstrap nav-tabs.
**Expected:** Semua chart dan tabel lama tetap berfungsi normal, tidak ada data yang hilang.
**Why human:** Regression check visual membutuhkan browser dengan data nyata.

---

## Gaps Summary

Tidak ada gap teknis yang ditemukan. Semua 10 must-haves terverifikasi di codebase:
- 5 backend truths (endpoints, models, calculations) — semua ada dan substantif
- 5 frontend truths (tabs, tables, charts, exports) — semua ada dan wired ke endpoint

**Satu-satunya hal yang belum terverifikasi adalah rendering visual dan interaksi browser**, yang membutuhkan human testing (lihat bagian Human Verification di atas).

**Catatan administrasi:** Setelah human verification selesai, REQUIREMENTS.md perlu diperbarui untuk menandai RPT-01, RPT-02, RPT-03, RPT-04, dan RPT-07 sebagai "Complete" (saat ini masih "Pending" meskipun implementasi sudah ada).

---

_Verified: 2026-04-07T10:45:00Z_
_Verifier: Claude (gsd-verifier)_
