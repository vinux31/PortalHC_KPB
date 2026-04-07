# Phase 301: Advanced Reporting - Research

**Researched:** 2026-04-07
**Domain:** ASP.NET Core MVC — Analytics Dashboard, Statistik Soal (Item Analysis), Gain Score Report, ClosedXML Export
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Item Analysis ditampilkan sebagai tab baru di Analytics Dashboard (bersama Fail Rate, Trend, ET Breakdown)
- **D-02:** Reuse filter Bagian/Unit/Kategori yang sudah ada + dropdown pilih Assessment
- **D-03:** Per soal menampilkan: difficulty index (p-value), discrimination index (Kelley upper/lower 27%)
- **D-04:** Discrimination index dengan warning < 30 responden: inline badge kuning "Data belum cukup (N<30)" di samping nilai, nilai tetap ditampilkan tapi di-gray-out
- **D-05:** Distractor analysis: tabel opsi per soal — Opsi | Jumlah pemilih | Persentase | Highlight jawaban benar
- **D-06:** Gain Score Report ditampilkan sebagai tab baru di Analytics Dashboard
- **D-07:** Dua view dalam tab: (1) Tabel per pekerja: Pre Score | Post Score | Gain Score, (2) Tabel per elemen kompetensi: Avg Pre | Avg Post | Avg Gain
- **D-08:** Hanya muncul untuk assessment bertipe PrePostTest
- **D-09:** File terpisah per report — tombol "Export" di masing-masing tab menghasilkan file .xlsx sendiri
- **D-10:** Styling profesional: header berwarna, border, auto-fit column width, freeze header row — menggunakan ExcelExportHelper + ClosedXML yang sudah ada
- **D-11:** Line chart tren gain score per bulan ditambahkan di tab Trend yang sudah ada (di bawah chart trend lulus/gagal)
- **D-12:** Sumbu X = bulan, sumbu Y = rata-rata gain score assessment PrePostTest

### Claude's Discretion

- Exact UI spacing dan typography di tab baru
- Loading state dan skeleton design
- Error handling untuk assessment tanpa data PrePostTest
- Color coding untuk interpretasi p-value (mudah/sedang/sulit)
- Perbandingan antar kelompok (RPT-07) — cara grouping dan visualisasi

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| RPT-01 | Item Analysis — difficulty index (p-value) per soal | Query PackageUserResponse grouped by PackageQuestionId; p-value = correct responses / total responses |
| RPT-02 | Discrimination index (Kelley upper/lower 27%) dengan warning < 30 responden | Sort session scores, take top/bottom 27%, compare correct rates; warning badge saat N < 30 |
| RPT-03 | Distractor analysis — persentase per opsi per soal | GROUP BY PackageOptionId; join ke PackageOption untuk OptionText dan IsCorrect |
| RPT-04 | Pre-Post Gain Score Report per pekerja dan per elemen kompetensi | Join AssessmentSession (AssessmentType=PreTest/PostTest) by LinkedGroupId dan UserId; formula dari WKPPT-05 |
| RPT-05 | Export Item Analysis dan Gain Score Report ke Excel | ExcelExportHelper.cs + ClosedXML 0.105.0; endpoint baru di CMPController |
| RPT-06 | Analytics Dashboard panel tren gain score | Dataset baru di `trendChart` atau canvas terpisah di Panel Trend; data dari endpoint baru atau extend GetAnalyticsData |
| RPT-07 | Perbandingan antar kelompok (group comparison) | Group by User.Section atau User.Unit; tabel perbandingan avg gain score antar group |

</phase_requirements>

---

## Summary

Phase 301 menambahkan tiga kapabilitas analitik baru ke Analytics Dashboard yang sudah ada di `/CMP/AnalyticsDashboard`:

1. **Item Analysis tab** — statistik kualitas soal (difficulty index, discrimination index, distractor analysis) untuk assessment tertentu yang dipilih HC
2. **Gain Score Report tab** — laporan perbandingan Pre vs Post Score per pekerja dan per elemen kompetensi, khusus untuk assessment PrePostTest
3. **Gain Score Trend** — line chart baru di tab Trend yang sudah ada, menampilkan rata-rata gain score per bulan

Semua data analytics baru di-fetch via AJAX (pattern yang sama dengan `GetAnalyticsData`). Dua endpoint baru diperlukan: `GetItemAnalysisData` dan `GetGainScoreData`. Dua endpoint export baru diperlukan: `ExportItemAnalysisExcel` dan `ExportGainScoreExcel`. Gain score trend dapat dimasukkan ke dalam `GetAnalyticsData` yang sudah ada (extend response JSON) untuk menghindari AJAX call tambahan.

**Primary recommendation:** Ikuti pattern yang sudah established — endpoint AJAX baru di CMPController, tab baru di AnalyticsDashboard.cshtml, model baru di AnalyticsDashboardViewModel.cs. Jangan ubah struktur tab atau filter yang sudah ada.

---

## Standard Stack

### Core (Sudah Ada di Project — VERIFIED: codebase grep)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | 0.105.0 | Excel export (.xlsx) | Sudah dipakai di seluruh project; ExcelExportHelper.cs wrapper sudah ada |
| Chart.js | 4.x (CDN) | Line chart, bar chart | Sudah dimuat di AnalyticsDashboard.cshtml; tidak perlu library tambahan |
| Bootstrap 5 | Existing | Tabel, badge, tab styling | Sudah ada di layout |

### Tidak Perlu Library Baru

Seluruh fungsionalitas Phase 301 dapat diimplementasikan menggunakan stack yang sudah ada:
- Item Analysis: pure SQL/LINQ queries ke `PackageUserResponse`, `PackageQuestion`, `PackageOption`
- Gain Score: LINQ join `AssessmentSession` by `LinkedGroupId`
- Export: `ExcelExportHelper.CreateSheet()` + `ToFileResult()` yang sudah ada
- Chart: tambah dataset baru ke `trendChart` yang sudah ada

**Installation:** Tidak diperlukan instalasi package baru.

---

## Architecture Patterns

### Pola Yang Sudah Ada (VERIFIED: baca CMPController.cs)

**AJAX Analytics Pattern:**
```
GET /CMP/GetAnalyticsData?bagian=X&unit=Y&kategori=Z&periodeStart=D&periodeEnd=D
→ Returns AnalyticsDataResult (JSON)
→ JS renders chart/table
```

Tab baru mengikuti pattern ini persis. Bedanya:
- Item Analysis memerlukan parameter tambahan: `assessmentGroupId` (LinkedGroupId)
- Gain Score memerlukan parameter: `assessmentGroupId` (LinkedGroupId)

### Struktur Endpoint Baru

```
GET /CMP/GetItemAnalysisData?assessmentGroupId=123
→ Returns ItemAnalysisResult (JSON)
  - List<ItemAnalysisRow> (satu per soal)
    - QuestionNumber, QuestionText
    - DifficultyIndex (p-value, 0.0–1.0)
    - DiscriminationIndex (Kelley, bisa null jika N<30)
    - IsLowN (bool — apakah N < 30)
    - TotalResponden (int)
    - List<DistractorRow> (satu per opsi)
      - OptionText, IsCorrect, Count, Percent

GET /CMP/GetGainScoreData?assessmentGroupId=123&bagian=X&unit=Y
→ Returns GainScoreResult (JSON)
  - List<GainScorePerWorker>
    - NamaPekerja, NIP, PreScore, PostScore, GainScore
  - List<GainScorePerElemenTeknis>
    - ElemenTeknis, AvgPre, AvgPost, AvgGain

GET /CMP/ExportItemAnalysisExcel?assessmentGroupId=123
→ Returns .xlsx file download

GET /CMP/ExportGainScoreExcel?assessmentGroupId=123
→ Returns .xlsx file download
```

Gain Score Trend dimasukkan ke `GetAnalyticsData` yang sudah ada (extend `AnalyticsDataResult`):
```csharp
// Tambah property baru ke AnalyticsDataResult
public List<GainScoreTrendItem> GainScoreTrend { get; set; } = new();

// Model baru
public class GainScoreTrendItem {
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label => $"{Year}-{Month:D2}";
    public double AvgGainScore { get; set; }
    public int SampleCount { get; set; }
}
```

### Formula Statistik (ASSUMED — standard psychometrics)

**Difficulty Index (p-value):**
```
p = jumlah_menjawab_benar / total_responden
Range: 0.0 (semua salah) — 1.0 (semua benar)
Interpretasi: p < 0.3 = sulit, 0.3–0.7 = sedang, > 0.7 = mudah
```

**Discrimination Index — Kelley Method:**
```
1. Sort semua sesi berdasarkan total skor
2. Upper group = top 27% (ceil)
3. Lower group = bottom 27% (ceil)
4. D = (correct_upper / n_upper) - (correct_lower / n_lower)
Range: -1 s/d +1; D > 0.3 = baik, < 0 = perlu review soal
Warning: tampilkan jika total_responden < 30
```

**Gain Score (dari WKPPT-05 — VERIFIED: REQUIREMENTS.md):**
```
Jika PreScore = 100: GainScore = 100
Else: GainScore = (PostScore - PreScore) / (100 - PreScore) * 100
```

### Query Patterns (VERIFIED: baca model structures)

**Query Item Analysis — Difficulty Index:**
```csharp
// Per soal dalam satu assessment group (PreTest saja, karena soal yang dianalisis)
var preSessionIds = await _context.AssessmentSessions
    .Where(s => s.LinkedGroupId == assessmentGroupId && s.AssessmentType == "PreTest"
                && s.Status == "Completed")
    .Select(s => s.Id)
    .ToListAsync();

var responses = await _context.PackageUserResponses
    .Include(r => r.PackageQuestion)
        .ThenInclude(q => q.Options)
    .Include(r => r.PackageOption)
    .Where(r => preSessionIds.Contains(r.AssessmentSessionId))
    .ToListAsync();

// Group by QuestionId untuk hitung p-value
var grouped = responses.GroupBy(r => r.PackageQuestionId);
```

**Query Gain Score Per Pekerja:**
```csharp
var preSessions = await _context.AssessmentSessions
    .Include(s => s.User)
    .Where(s => s.LinkedGroupId == assessmentGroupId
                && s.AssessmentType == "PreTest"
                && s.Status == "Completed"
                && s.Score.HasValue)
    .ToListAsync();

var postSessions = await _context.AssessmentSessions
    .Where(s => s.LinkedGroupId == assessmentGroupId
                && s.AssessmentType == "PostTest"
                && s.Status == "Completed"
                && s.Score.HasValue)
    .ToDictionaryAsync(s => s.UserId, s => s);

// Join by UserId untuk pair Pre-Post
foreach (var pre in preSessions) {
    if (postSessions.TryGetValue(pre.UserId, out var post)) {
        // hitung gain score
    }
}
```

**Query Gain Score Per Elemen Teknis:**
```csharp
// Gunakan SessionElemenTeknisScores yang sudah ada (sama dengan ET Breakdown di GetAnalyticsData)
var preEt = await _context.SessionElemenTeknisScores
    .Where(e => preSessionIds.Contains(e.AssessmentSessionId))
    .ToListAsync();
var postEt = await _context.SessionElemenTeknisScores
    .Where(e => postSessionIds.Contains(e.AssessmentSessionId))
    .ToListAsync();
// Join by (UserId, ElemenTeknis) — hitung avg gain per ElemenTeknis
```

### Dropdown Pilih Assessment (untuk tab Item Analysis dan Gain Score)

Tab baru memerlukan dropdown "Pilih Assessment" untuk memilih assessment mana yang akan dianalisis. Dropdown ini berisi daftar assessment PrePostTest yang sudah Completed:

```csharp
// Endpoint baru: GET /CMP/GetPrePostAssessmentList?bagian=X&unit=Y
// Returns: List<{ LinkedGroupId, Title, CompletedAt, TotalWorker }>
```

Dropdown di-populate via AJAX saat filter Bagian/Unit berubah.

### ExcelExportHelper Extension untuk Freeze + Color Header (D-10)

Helper yang ada hanya memberikan bold header. Untuk D-10 (freeze + color), perlu extend langsung di controller:

```csharp
// Pattern yang sudah ada
var ws = ExcelExportHelper.CreateSheet(wb, "Item Analysis", headers);

// Extension untuk freeze dan color — langsung di controller
ws.SheetView.FreezeRows(1);
ws.Range(1, 1, 1, headers.Length).Style
    .Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
ws.Range(1, 1, 1, headers.Length).Style
    .Font.FontColor = XLColor.White;
// ExcelExportHelper.ToFileResult() sudah handle AdjustToContents()
```

### Tab Structure (VERIFIED: baca AnalyticsDashboard.cshtml)

Saat ini AnalyticsDashboard.cshtml menggunakan card/panel layout (tidak ada tab bar Bootstrap). Tab baru ditambahkan dengan cara mengubah layout menjadi Bootstrap tab:

**Pilihan A:** Ubah 4 panel grid menjadi tabbed layout — 4 tab existing + 2 tab baru (Item Analysis, Gain Score)
**Pilihan B:** Tambah section terpisah di bawah 4 panel grid dengan tab baru

Berdasarkan D-01/D-06 ("ditampilkan sebagai tab baru di Analytics Dashboard bersama Fail Rate, Trend, ET Breakdown"), Pilihan A lebih sesuai. Namun karena D-11 ("ditambahkan di tab Trend yang sudah ada"), ini berarti Trend harus tetap menjadi satu tab.

**Rekomendasi implementasi:** Konversi 4 panel ke Bootstrap tab (`nav-tabs`) + tambah 2 tab baru:
- Tab 1: Fail Rate
- Tab 2: Trend (termasuk Gain Score Trend di bawah, per D-11)
- Tab 3: Skor Elemen Teknis
- Tab 4: Sertifikat Expired
- Tab 5: Item Analysis (baru)
- Tab 6: Gain Score (baru)

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel export dengan styling | Custom file writer | ExcelExportHelper + ClosedXML | Sudah ada wrapper; ClosedXML handle semua edge case |
| Discrimination index sorting | Percile logic manual yang kompleks | LINQ OrderBy + Take(ceil(n * 0.27)) | Cukup dengan LINQ standard; Kelley method sederhana |
| Chart.js line chart | Library chart baru | Chart.js yang sudah di-load | Sudah ada di halaman; tambah dataset saja |

---

## Common Pitfalls

### Pitfall 1: Item Analysis hanya dari soal MC (MultipleChoice)

**What goes wrong:** Jika query tidak filter QuestionType, soal Essay dan MultipleAnswer masuk ke kalkulasi — distractor analysis tidak bermakna untuk Essay.
**Why it happens:** PackageQuestion.QuestionType bisa null (backward compat) atau "Essay"/"MultipleAnswer".
**How to avoid:** Filter `WHERE QuestionType IS NULL OR QuestionType = 'MultipleChoice'` untuk distractor analysis. Untuk difficulty index, soal MA dan Essay bisa diinclude dengan penanganan berbeda.
**Warning signs:** Distractor analysis menampilkan opsi TextAnswer yang panjang, bukan opsi A/B/C/D.

### Pitfall 2: LinkedSessionId vs LinkedGroupId

**What goes wrong:** Confusing dua field yang berbeda.
**Root cause:** `LinkedGroupId` = ID grup yang menghubungkan semua Pre dan Post sesi dalam satu batch assessment. `LinkedSessionId` = ID sesi Pre/Post pasangan individual per worker.
**How to avoid:** Untuk analytics agregat (semua worker dalam satu assessment), gunakan `LinkedGroupId`. Untuk pair individual satu worker, gunakan `LinkedSessionId`.

### Pitfall 3: Score NULL untuk sesi yang belum selesai

**What goes wrong:** `AssessmentSession.Score` bisa null untuk sesi Abandoned atau sesi Essay yang menunggu penilaian manual.
**How to avoid:** Filter `s.Score.HasValue && s.Status == "Completed"` sebelum menghitung statistik. Khusus Essay: `s.IsPassed.HasValue` juga perlu dicek.

### Pitfall 4: Gain Score Trend — PreTest score vs PostTest score membingungkan

**What goes wrong:** Mengambil semua `AssessmentSession` yang `Score != null` tanpa memfilter `AssessmentType`, sehingga Pre dan Post score tercampur dalam kalkulasi rata-rata.
**How to avoid:** Selalu filter `AssessmentType == "PreTest"` dan `AssessmentType == "PostTest"` secara eksplisit. Join by `LinkedGroupId` + `UserId` untuk memastikan pasangan yang valid.

### Pitfall 5: N terlalu kecil untuk Kelley 27%

**What goes wrong:** Jika total responden = 5, maka 27% = 1.35 → ceil = 2 orang per kelompok. Dengan N=2, discrimination index tidak statistik valid.
**How to avoid:** Implementasi warning badge per D-04: tampilkan "Data belum cukup (N<30)" saat N < 30. Nilai tetap dihitung dan ditampilkan (gray-out) — tidak disembunyikan. Ini sudah menjadi keputusan D-04 sehingga tidak perlu diputuskan ulang.

### Pitfall 6: Dashboard filter existing tidak ada `assessmentGroupId`

**What goes wrong:** Filter Bagian/Unit/Kategori existing di halaman tidak dapat langsung memfilter "assessment mana" untuk Item Analysis. Item Analysis butuh satu assessment spesifik dipilih.
**How to avoid:** Tambah dropdown "Pilih Assessment" yang muncul hanya saat tab Item Analysis atau Gain Score aktif. Dropdown ini populated via AJAX berdasarkan filter Bagian/Unit yang sudah dipilih.

---

## Code Examples

### ExcelExportHelper Pattern yang Sudah Ada (VERIFIED: baca Helpers/ExcelExportHelper.cs)

```csharp
// Di CMPController — pola export yang sudah ada di project
public IActionResult ExportItemAnalysisExcel(int assessmentGroupId)
{
    using var wb = new XLWorkbook();
    var headers = new[] { "No", "Soal", "P-Value", "Interpretasi", "D-Index", "N Responden" };
    var ws = ExcelExportHelper.CreateSheet(wb, "Item Analysis", headers);

    // Freeze baris header
    ws.SheetView.FreezeRows(1);

    // Warna header biru
    ws.Range(1, 1, 1, headers.Length).Style
        .Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd"));
    ws.Range(1, 1, 1, headers.Length).Style
        .Font.SetFontColor(XLColor.White);

    // Isi data — mulai row 2
    int row = 2;
    foreach (var item in analysisData)
    {
        ws.Cell(row, 1).Value = item.QuestionNumber;
        ws.Cell(row, 2).Value = item.QuestionText;
        ws.Cell(row, 3).Value = item.DifficultyIndex;
        ws.Cell(row, 4).Value = item.Interpretasi;
        ws.Cell(row, 5).Value = item.DiscriminationIndex;
        ws.Cell(row, 6).Value = item.TotalResponden;
        row++;
    }

    // Sheet kedua: Distractor Analysis
    var wsDistractor = ExcelExportHelper.CreateSheet(wb, "Distractor Analysis",
        new[] { "No Soal", "Opsi", "Jawaban Benar", "Jumlah Pemilih", "Persentase" });
    wsDistractor.SheetView.FreezeRows(1);
    // ... isi data distractor

    return ExcelExportHelper.ToFileResult(wb,
        $"ItemAnalysis_{assessmentGroupId}_{DateTime.Now:yyyyMMdd}.xlsx",
        this);
}
```

### Chart.js Gain Score Trend (extend trendChart existing)

```javascript
// Extend renderTrend() untuk menambahkan dataset gain score
function renderTrend(data, gainScoreTrend) {
    // ... existing pass/fail datasets ...

    // Tambah dataset gain score jika ada data
    if (gainScoreTrend && gainScoreTrend.length > 0) {
        // Align labels: gunakan union semua label bulan
        datasets.push({
            label: 'Avg Gain Score (%)',
            data: gainScoreTrend.map(function(d) { return d.avgGainScore; }),
            borderColor: '#6f42c1',
            backgroundColor: 'rgba(111,66,193,0.1)',
            fill: false,
            tension: 0.3,
            yAxisID: 'y2'  // Axis kedua jika range berbeda
        });
    }
    // ...
}
```

### Kelley Discrimination Index — C# Implementation

```csharp
// Hitung discrimination index Kelley upper/lower 27%
private static double? CalculateDiscriminationIndex(
    List<(int SessionId, int TotalScore)> sessionScores,
    List<int> correctSessionIds)
{
    int n = sessionScores.Count;
    if (n == 0) return null;

    int groupSize = (int)Math.Ceiling(n * 0.27);
    if (groupSize == 0) return null;

    var sorted = sessionScores.OrderByDescending(s => s.TotalScore).ToList();
    var upperIds = sorted.Take(groupSize).Select(s => s.SessionId).ToHashSet();
    var lowerIds = sorted.TakeLast(groupSize).Select(s => s.SessionId).ToHashSet();

    var correctSet = new HashSet<int>(correctSessionIds);
    double upperCorrect = upperIds.Count(id => correctSet.Contains(id));
    double lowerCorrect = lowerIds.Count(id => correctSet.Contains(id));

    return (upperCorrect / groupSize) - (lowerCorrect / groupSize);
}
```

### RPT-07 Group Comparison — Tabel Perbandingan Antar Kelompok

```csharp
// Group by User.Section (Bagian) untuk perbandingan
var groupComparison = gainScoreData
    .GroupBy(g => g.Section)
    .Select(grp => new GroupComparisonItem
    {
        GroupName = grp.Key ?? "Tidak Diketahui",
        WorkerCount = grp.Count(),
        AvgPreScore = grp.Average(g => g.PreScore),
        AvgPostScore = grp.Average(g => g.PostScore),
        AvgGainScore = grp.Average(g => g.GainScore)
    })
    .OrderByDescending(g => g.AvgGainScore)
    .ToList();
```

---

## State of the Art

| Old Approach | Current Approach | Status |
|--------------|-----------------|--------|
| Chart baru untuk setiap feature | Extend dataset di Chart.js instance yang sudah ada | Sesuai untuk Phase 301 (D-11) |
| Tab switching dengan page reload | JavaScript tab switching tanpa reload | Pattern existing — ikuti |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Item Analysis menggunakan soal Pre-Test saja (bukan Post-Test) untuk kalkulasi kualitas soal | Architecture Patterns | Jika Post-Test menggunakan paket soal berbeda, harus dipilih per paket — tanya ke user jika diperlukan |
| A2 | SessionElemenTeknisScores sudah ter-populated untuk sesi PrePostTest | Architecture Patterns — Query Gain Score Per ET | Jika tabel ini belum terisi untuk PrePostTest, Gain Score Per ET tidak bisa dihitung — perlu verifikasi data |
| A3 | Kelley discrimination index menggunakan skor total ujian (bukan skor per soal lain) untuk ranking | Code Examples | Ini adalah implementasi standar Kelley; alternatif menggunakan item score correlation |
| A4 | RPT-07 grouping berdasarkan User.Section (Bagian) sebagai unit perbandingan default | Architecture Patterns | Bisa juga per Unit atau per Kategori — discretion diberikan ke Claude (Claude's Discretion) |

---

## Open Questions

1. **Paket soal mana yang dianalisis untuk Item Analysis?**
   - What we know: PrePostTest bisa menggunakan paket soal yang sama (D-03 dari Phase 297) atau berbeda untuk Pre dan Post.
   - What's unclear: Jika berbeda, Item Analysis menganalisis paket Pre atau Post atau keduanya?
   - Recommendation: Default ke paket Pre-Test untuk Item Analysis (mengukur kemampuan awal). Jika user ingin Post-Test juga, bisa ditambahkan toggle. Untuk MVP Phase 301, gunakan Pre-Test.

2. **SessionElemenTeknisScores apakah sudah ada data untuk PrePostTest?**
   - What we know: Tabel `SessionElemenTeknisScores` digunakan untuk ET Breakdown di GetAnalyticsData.
   - What's unclear: Apakah GradingService mengisi tabel ini untuk sesi PrePostTest (AssessmentType="PreTest"/"PostTest")?
   - Recommendation: Verifikasi dengan query DB sebelum implementasi. Jika belum terisi, Gain Score per ET harus menggunakan kalkulasi alternatif dari `PackageUserResponse`.

---

## Environment Availability

Step 2.6: SKIPPED (tidak ada external dependencies baru — semua library sudah ada di project)

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual testing (tidak ada automated test framework yang terdeteksi) |
| Config file | none |
| Quick run command | Build project: `dotnet build` |
| Full suite command | `dotnet build` + manual browser verification |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| RPT-01 | p-value dihitung benar per soal | manual | — | N/A |
| RPT-02 | Discrimination index Kelley + warning N<30 | manual | — | N/A |
| RPT-03 | Distractor analysis % per opsi | manual | — | N/A |
| RPT-04 | Gain score per pekerja dan per ET | manual | — | N/A |
| RPT-05 | Export .xlsx terbuka dan berisi data | manual | — | N/A |
| RPT-06 | Gain score trend line chart tampil di tab Trend | manual | — | N/A |
| RPT-07 | Tabel perbandingan antar kelompok | manual | — | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` — pastikan tidak ada compile error
- **Per wave merge:** `dotnet build` + manual smoke test di browser
- **Phase gate:** Semua success criteria manual verified sebelum `/gsd-verify-work`

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | `[Authorize(Roles = "Admin, HC")]` — sama dengan endpoint existing |
| V4 Access Control | yes | HC-only access; worker tidak dapat akses endpoint analytics |
| V5 Input Validation | yes | `assessmentGroupId` divalidasi sebagai int; filter string di-sanitize via parameterized query (EF Core) |
| V6 Cryptography | no | Tidak ada data sensitif baru |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR: akses analytics assessment orang lain | Spoofing | Verifikasi HC/Admin role via `[Authorize]`; tidak ada per-worker scoping yang perlu karena HC melihat semua |
| SQL Injection via filter params | Tampering | EF Core parameterized queries — tidak ada raw SQL |
| Excel formula injection | Tampering | ClosedXML meng-escape cell values secara default; tidak ada formula injection risk |

---

## Sources

### Primary (HIGH confidence)
- `[VERIFIED: codebase]` Controllers/CMPController.cs — Pattern GetAnalyticsData, endpoint structure, AJAX pattern
- `[VERIFIED: codebase]` Helpers/ExcelExportHelper.cs — ExcelExportHelper.CreateSheet() dan ToFileResult() API
- `[VERIFIED: codebase]` Models/AnalyticsDashboardViewModel.cs — Existing model classes
- `[VERIFIED: codebase]` Views/CMP/AnalyticsDashboard.cshtml — Tab structure, JS rendering pattern, Chart.js usage
- `[VERIFIED: codebase]` Models/AssessmentPackage.cs — PackageQuestion, PackageOption, PackageUserResponse structure
- `[VERIFIED: codebase]` Controllers/AssessmentAdminController.cs — LinkedGroupId, AssessmentType pattern
- `[VERIFIED: REQUIREMENTS.md]` WKPPT-05 — Gain score formula: (Post - Pre) / (100 - Pre) x 100

### Secondary (MEDIUM confidence)
- `[ASSUMED]` Kelley discrimination index formula (standard psychometrics: upper/lower 27% method) — widely documented in educational measurement literature

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library sudah verified ada di codebase
- Architecture patterns: HIGH — endpoint dan model patterns di-verify dari kode existing
- Formula kalkulasi (statistik): MEDIUM — Kelley method adalah standard psikometri, formula assumed
- Pitfalls: HIGH — berbasis analisis langsung dari model structures yang ada

**Research date:** 2026-04-07
**Valid until:** 2026-05-07 (stack stabil, tidak ada dependency baru)
