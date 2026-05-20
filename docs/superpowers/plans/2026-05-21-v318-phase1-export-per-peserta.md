# Milestone v3.18 — Phase 1: Export Per-Peserta Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend `ExportAssessmentResults` agar file Excel berisi 1 sheet "Summary" + N sheet per peserta (data lengkap + PNG radar chart) untuk Admin/HC.

**Architecture:** Refactor controller existing (`AssessmentAdminController.ExportAssessmentResults`) jadi 2 layer: (1) Summary sheet (rename "Results"→"Summary"), (2) Per-peserta loop yang generate sheet content via helper baru `Helpers/SpiderChartRenderer.cs` (SkiaSharp PNG renderer). Chart PNG di-generate paralel pakai `Task.WhenAll` dengan `MaxDegreeOfParallelism = Environment.ProcessorCount`. No DB schema change.

**Tech Stack:** .NET 8 + ClosedXML 0.105.0 (existing), SkiaSharp 3.116.1 + SkiaSharp.NativeAssets.Win32 (NEW), Bootstrap 5 view layer (no UI change Phase 1).

**Spec reference:** `docs/superpowers/specs/2026-05-20-assessment-admin-power-tools-design.md` Section 3 (commit c37e55ef).

**Project test infra:** Project TIDAK punya unit/integration test (per spec 5.10). Verification path = `dotnet build` + manual UAT via browser. Pre-commit checklist dari [`docs/DEV_WORKFLOW.md`](../../DEV_WORKFLOW.md) §5 wajib dijalankan per task yang menghasilkan commit.

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `HcPortal.csproj` | Modify | Tambah `SkiaSharp` + `SkiaSharp.NativeAssets.Win32` PackageReference |
| `Helpers/SpiderChartRenderer.cs` | Create | Static class: `byte[] RenderRadarPng(IList<(string label, double percentage)> data, int size = 500)` |
| `Helpers/SheetNameSanitizer.cs` | Create | Static helper: `string Sanitize(string nip, string fullName, ISet<string> usedNames)` — Excel-safe `{NIP}_{FullName}` |
| `Controllers/AssessmentAdminController.cs` | Modify | Refactor `ExportAssessmentResults` (line 3651) — rename sheet "Results"→"Summary"; tambah per-peserta sheet loop |

Tidak ada migration. Tidak ada perubahan model EF.

---

## Task 1: Tambah SkiaSharp Package

**Files:**
- Modify: `HcPortal.csproj`

- [ ] **Step 1: Tambah PackageReference**

Edit `HcPortal.csproj`, di dalam `<ItemGroup>` existing (setelah `QuestPDF` line 25), tambah:

```xml
<PackageReference Include="SkiaSharp" Version="3.116.1" />
<PackageReference Include="SkiaSharp.NativeAssets.Win32" Version="3.116.1" />
```

- [ ] **Step 2: Restore + verify build**

Run:
```bash
dotnet restore
dotnet build
```
Expected: Build succeeded, 0 warning baru, 0 error. SkiaSharp DLL muncul di `bin/Debug/net8.0/`.

- [ ] **Step 3: Smoke test render**

Test via `dotnet script` atau bikin temporary test endpoint. Pastikan `SKBitmap`, `SKCanvas`, `SKImage.Encode(SKEncodedImageFormat.Png, 100)` runtime OK. Kalau native DLL missing → cek `SkiaSharp.NativeAssets.Win32` ter-extract di `bin/.../runtimes/win-x64/native/libSkiaSharp.dll`.

- [ ] **Step 4: Commit**

```bash
git add HcPortal.csproj
git commit -m "feat(v3.18-phase1): add SkiaSharp 3.116.1 for spider chart PNG render"
```

---

## Task 2: Helper SpiderChartRenderer

**Files:**
- Create: `Helpers/SpiderChartRenderer.cs`

- [ ] **Step 1: Implementasi class**

Buat `Helpers/SpiderChartRenderer.cs`:

```csharp
using SkiaSharp;

namespace HcPortal.Helpers
{
    public static class SpiderChartRenderer
    {
        /// <summary>
        /// Render radar/spider chart sebagai PNG byte array.
        /// </summary>
        /// <param name="data">List (label, percentage 0..100). Minimum 3 untuk render valid; kalau &lt; 3 return empty array.</param>
        /// <param name="size">Lebar+tinggi canvas (default 500px).</param>
        /// <returns>PNG bytes, atau byte[0] kalau data &lt; 3 elemen.</returns>
        public static byte[] RenderRadarPng(IList<(string label, double percentage)> data, int size = 500)
        {
            if (data == null || data.Count < 3) return Array.Empty<byte>();

            using var bitmap = new SKBitmap(size, size);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            float cx = size / 2f;
            float cy = size / 2f;
            float radius = size * 0.35f;
            int n = data.Count;

            // Grid radial (0/25/50/75/100)
            using var gridPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
            for (int level = 1; level <= 4; level++)
            {
                float r = radius * level / 4f;
                using var path = new SKPath();
                for (int i = 0; i < n; i++)
                {
                    double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                    float x = cx + (float)(r * Math.Cos(angle));
                    float y = cy + (float)(r * Math.Sin(angle));
                    if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
                }
                path.Close();
                canvas.DrawPath(path, gridPaint);
            }

            // Axis lines
            using var axisPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                float x = cx + (float)(radius * Math.Cos(angle));
                float y = cy + (float)(radius * Math.Sin(angle));
                canvas.DrawLine(cx, cy, x, y, axisPaint);
            }

            // Data polygon
            using var fillPaint = new SKPaint { Color = new SKColor(54, 162, 235, 96), Style = SKPaintStyle.Fill, IsAntialias = true };
            using var strokePaint = new SKPaint { Color = new SKColor(54, 162, 235), Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
            using var dataPath = new SKPath();
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                double pct = Math.Clamp(data[i].percentage, 0, 100);
                float r = (float)(radius * pct / 100);
                float x = cx + (float)(r * Math.Cos(angle));
                float y = cy + (float)(r * Math.Sin(angle));
                if (i == 0) dataPath.MoveTo(x, y); else dataPath.LineTo(x, y);
            }
            dataPath.Close();
            canvas.DrawPath(dataPath, fillPaint);
            canvas.DrawPath(dataPath, strokePaint);

            // Labels (truncate > 20 char dengan ellipsis, konsisten Results.cshtml:274)
            using var textFont = new SKFont(SKTypeface.Default, 12);
            using var textPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI / 2 + 2 * Math.PI * i / n;
                float labelR = radius + 25;
                float x = cx + (float)(labelR * Math.Cos(angle));
                float y = cy + (float)(labelR * Math.Sin(angle));
                string label = data[i].label;
                if (label.Length > 20) label = label.Substring(0, 17) + "...";
                var textWidth = textFont.MeasureText(label);
                canvas.DrawText(label, x - textWidth / 2, y, SKTextAlign.Left, textFont, textPaint);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
            return pngData.ToArray();
        }
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeded, 0 error.

- [ ] **Step 3: Manual smoke test (one-off)**

Bikin temporary `Controllers/DebugController.cs` action atau pakai `dotnet run` + buka endpoint test:

```csharp
[HttpGet]
[Authorize(Roles = "Admin")]
public IActionResult TestSpiderChart()
{
    var data = new List<(string, double)>
    {
        ("Safety", 80),
        ("Operation", 65),
        ("Maintenance", 90),
        ("Quality", 75),
        ("Communication", 50)
    };
    var png = SpiderChartRenderer.RenderRadarPng(data);
    return File(png, "image/png");
}
```

Buka di browser. Verify chart muncul 500×500 dengan 5 axis + polygon biru. Hapus debug endpoint setelah verify.

- [ ] **Step 4: Commit**

```bash
git add Helpers/SpiderChartRenderer.cs
git commit -m "feat(v3.18-phase1): add SpiderChartRenderer (SkiaSharp PNG radar)"
```

---

## Task 3: Helper SheetNameSanitizer

**Files:**
- Create: `Helpers/SheetNameSanitizer.cs`

- [ ] **Step 1: Implementasi**

Buat `Helpers/SheetNameSanitizer.cs`:

```csharp
namespace HcPortal.Helpers
{
    public static class SheetNameSanitizer
    {
        private static readonly char[] InvalidChars = { '\\', '/', '?', '*', '[', ']', ':' };
        private const int ExcelSheetNameLimit = 31;

        /// <summary>
        /// Format sheet name {NIP}_{FullName}, truncate ke 31 char dengan collision guard.
        /// NIP-first ensures uniqueness karena NIP guaranteed unique per worker.
        /// </summary>
        public static string Sanitize(string nip, string fullName, ISet<string> usedNames)
        {
            string cleanNip = ScrubChars(nip ?? "");
            string cleanName = ScrubChars(fullName ?? "");
            string raw = $"{cleanNip}_{cleanName}";
            if (raw.Length > ExcelSheetNameLimit)
                raw = raw.Substring(0, ExcelSheetNameLimit);

            // Collision guard (rare — only if truncation creates collision)
            string candidate = raw;
            int counter = 2;
            while (usedNames.Contains(candidate))
            {
                string suffix = $"({counter})";
                int allowed = ExcelSheetNameLimit - suffix.Length;
                candidate = (raw.Length > allowed ? raw.Substring(0, allowed) : raw) + suffix;
                counter++;
            }
            usedNames.Add(candidate);
            return candidate;
        }

        private static string ScrubChars(string s)
        {
            foreach (var c in InvalidChars) s = s.Replace(c, '_');
            return s;
        }
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Helpers/SheetNameSanitizer.cs
git commit -m "feat(v3.18-phase1): add SheetNameSanitizer ({NIP}_{FullName} format)"
```

---

## Task 4: Rename Sheet "Results" → "Summary"

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs:3727`

- [ ] **Step 1: Ubah sheet name**

Di `Controllers/AssessmentAdminController.cs:3727`, ubah:

```csharp
// BEFORE
var worksheet = workbook.Worksheets.Add("Results");

// AFTER
var worksheet = workbook.Worksheets.Add("Summary");
```

- [ ] **Step 2: Update filename suffix juga**

Di line 3793, ubah `_Results.xlsx` → `_Summary.xlsx`:

```csharp
var fileName = $"{safeTitle}_{scheduleDate:yyyyMMdd}_Summary.xlsx";
```

- [ ] **Step 3: Verify build + UAT**

Run: `dotnet build && dotnet run`. Buka `http://localhost:5277/AssessmentAdmin/ExportAssessmentResults?...` dgn login admin. Download file. Buka di Excel → tab pertama bernama "Summary", filename berakhiran `_Summary.xlsx`.

- [ ] **Step 4: Commit (breaking change tag)**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "refactor(v3.18-phase1)!: rename export sheet Results->Summary

BREAKING CHANGE: Tab pertama export Excel sekarang bernama 'Summary' (sebelumnya 'Results'). File suffix berubah _Results.xlsx -> _Summary.xlsx."
```

---

## Task 5: Filter Peserta Eligible untuk Per-Peserta Sheet

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (akhir method `ExportAssessmentResults`, sebelum return line 3794)

- [ ] **Step 1: Tambah filter list eligible sessions**

Setelah block "Data rows" (line 3789), sebelum `var safeTitle = ...`, sisipkan:

```csharp
// === Per-Peserta Sheets (v3.18 Phase 1) ===
// Filter peserta eligible: Completed + Abandoned only. Skip InProgress, Not Started, Cancelled.
var eligibleSessions = sessions
    .Where(s => s.Status == "Completed" || s.Status == "Abandoned")
    .OrderBy(s => s.User?.FullName ?? "")
    .ToList();

var usedSheetNames = new HashSet<string> { "Summary", StringComparer.OrdinalIgnoreCase ? "Summary" : "Summary" };
```

Note: simplify ke `new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Summary" }`.

```csharp
var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Summary" };
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(v3.18-phase1): filter eligible sessions (Completed+Abandoned) for per-peserta sheets"
```

---

## Task 6: Per-Peserta Sheet — Header + Info Section

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (lanjut block dari Task 5)

- [ ] **Step 1: Tambah loop sheet creation dengan header**

Setelah block Task 5, sisipkan:

```csharp
// Pre-load all per-session data in single query (avoid N+1)
var eligibleSessionIds = eligibleSessions.Select(s => s.Id).ToList();
var allResponses = await _context.PackageUserResponses
    .Where(r => eligibleSessionIds.Contains(r.AssessmentSessionId))
    .ToListAsync();
var allEtScores = await _context.SessionElemenTeknisScores
    .Where(et => eligibleSessionIds.Contains(et.AssessmentSessionId))
    .ToListAsync();

// Load all questions+options for involved packages
var sessionPackageMap = await _context.UserPackageAssignments
    .Where(a => eligibleSessionIds.Contains(a.AssessmentSessionId))
    .Select(a => new { a.AssessmentSessionId, a.AssessmentPackageId })
    .ToListAsync();
var packageIds = sessionPackageMap.Select(x => x.AssessmentPackageId).Distinct().ToList();
var allQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => packageIds.Contains(q.AssessmentPackageId))
    .ToListAsync();

foreach (var session in eligibleSessions)
{
    string sheetName = HcPortal.Helpers.SheetNameSanitizer.Sanitize(
        session.User?.NIP ?? "NA",
        session.User?.FullName ?? "Unknown",
        usedSheetNames);
    var ws = workbook.Worksheets.Add(sheetName);

    // === Header ===
    ws.Cell(1, 1).Value = $"{session.User?.FullName ?? "Unknown"} (NIP {session.User?.NIP ?? "—"})";
    ws.Range(1, 1, 1, 4).Merge();
    ws.Cell(1, 1).Style.Font.Bold = true;
    ws.Cell(1, 1).Style.Font.FontSize = 13;

    ws.Cell(2, 1).Value = "Started At";
    ws.Cell(2, 2).Value = session.StartedAt?.ToString("dd MMM yyyy HH:mm") ?? "—";
    ws.Cell(3, 1).Value = "Completed At";
    ws.Cell(3, 2).Value = session.CompletedAt?.ToString("dd MMM yyyy HH:mm") ?? "—";
    ws.Cell(4, 1).Value = "Durasi Aktual";
    int? durasi = session.ElapsedSeconds.HasValue ? session.ElapsedSeconds.Value / 60 : (int?)null;
    ws.Cell(4, 2).Value = durasi.HasValue ? $"{durasi.Value} menit" : "—";
    ws.Cell(5, 1).Value = "Tipe Assessment";
    ws.Cell(5, 2).Value = session.AssessmentType ?? "—";

    ws.Range(2, 1, 5, 1).Style.Font.Bold = true;
    // Per-peserta content continues di Task 7+ (ElemenTeknis section)
}
```

- [ ] **Step 2: Verify build + UAT export 1 group**

Run: `dotnet build && dotnet run`. Export dari `ManageAssessment` → Excel. Verify tab "Summary" + N tab `{NIP}_{FullName}` untuk peserta Completed/Abandoned. Tab peserta berisi header info session.

- [ ] **Step 3: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(v3.18-phase1): per-peserta sheet with session header + info"
```

---

## Task 7: Per-Peserta Sheet — Section ElemenTeknis Table

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (lanjut `foreach` Task 6)

- [ ] **Step 1: Tambah branching Variant A vs Variant B + tabel ET**

Di dalam `foreach (var session in eligibleSessions)` setelah header (akhir step Task 6), tambah:

```csharp
    int currentRow = 7;

    if (session.IsManualEntry)
    {
        // Variant B: Manual Entry — diisi di Task 11
        currentRow = WriteManualEntrySection(ws, session, currentRow);
        continue;
    }

    // === Variant A: Online ===
    var sessionEt = allEtScores.Where(et => et.AssessmentSessionId == session.Id).ToList();
    if (sessionEt.Any())
    {
        ws.Cell(currentRow, 1).Value = "Analisis Elemen Teknis";
        ws.Cell(currentRow, 1).Style.Font.Bold = true;
        ws.Range(currentRow, 1, currentRow, 4).Merge();
        currentRow++;

        // Table header
        ws.Cell(currentRow, 1).Value = "Elemen Teknis";
        ws.Cell(currentRow, 2).Value = "Benar";
        ws.Cell(currentRow, 3).Value = "Total";
        ws.Cell(currentRow, 4).Value = "Persentase";
        ws.Range(currentRow, 1, currentRow, 4).Style.Font.Bold = true;
        ws.Range(currentRow, 1, currentRow, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;
        currentRow++;

        foreach (var et in sessionEt.OrderBy(e => e.ElemenTeknis))
        {
            ws.Cell(currentRow, 1).Value = et.ElemenTeknis;
            ws.Cell(currentRow, 2).Value = et.CorrectCount;
            ws.Cell(currentRow, 3).Value = et.QuestionCount;
            double pct = et.QuestionCount > 0 ? (double)et.CorrectCount / et.QuestionCount * 100 : 0;
            ws.Cell(currentRow, 4).Value = $"{pct:F1}%";
            currentRow++;
        }
        currentRow++; // blank separator
    }
    // Skip section kalau sessionEt kosong (Abandoned tanpa ET, atau Essay-only)
```

- [ ] **Step 2: Tambah stub method `WriteManualEntrySection`**

Di akhir class `AssessmentAdminController`, tambah private helper (full body diisi di Task 11):

```csharp
private int WriteManualEntrySection(ClosedXML.Excel.IXLWorksheet ws, AssessmentSession session, int startRow)
{
    // Implemented in Task 11
    return startRow;
}
```

- [ ] **Step 3: Verify build + UAT**

Run: `dotnet build && dotnet run`. Export → buka tab peserta Online (non-Manual) → verify section "Analisis Elemen Teknis" tampil tabel. Buka tab peserta Abandoned → verify section auto-skip (cek empty `sessionEt`).

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(v3.18-phase1): per-peserta ElemenTeknis section (Variant A)"
```

---

## Task 8: Per-Peserta Sheet — Spider Chart PNG Embed

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (lanjut `foreach` Task 7)

- [ ] **Step 1: Tambah PNG render + embed sebelum Detail Jawaban**

Lanjut di dalam Variant A foreach (setelah block ET tabel Task 7):

```csharp
    // === Spider Chart PNG ===
    if (sessionEt.Count >= 3)
    {
        var chartData = sessionEt
            .OrderBy(e => e.ElemenTeknis)
            .Select(e => (e.ElemenTeknis, e.QuestionCount > 0 ? (double)e.CorrectCount / e.QuestionCount * 100 : 0d))
            .ToList();
        var png = HcPortal.Helpers.SpiderChartRenderer.RenderRadarPng(chartData);
        if (png.Length > 0)
        {
            using var ms = new MemoryStream(png);
            var pic = ws.AddPicture(ms, $"spider-{session.Id}")
                .MoveTo(ws.Cell(currentRow, 1))
                .WithSize(400, 400);
            currentRow += 22; // approx rows occupied by 400px image
        }
    }
```

- [ ] **Step 2: Verify build + UAT**

Run: `dotnet build && dotnet run`. Export → tab peserta Online dengan ≥ 3 ElemenTeknis → buka di Excel → verify gambar radar/spider tampil 400×400 di bawah tabel ET. Tab peserta dengan < 3 ElemenTeknis → chart skip.

- [ ] **Step 3: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(v3.18-phase1): embed spider chart PNG (>=3 ET, SkiaSharp render)"
```

---

## Task 9: Per-Peserta Sheet — Section Detail Jawaban (MC + MA)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (lanjut `foreach` Task 8)

- [ ] **Step 1: Tambah section Detail Jawaban**

Lanjut di dalam Variant A foreach (setelah block chart Task 8):

```csharp
    // === Detail Jawaban ===
    var sessionPackage = sessionPackageMap.FirstOrDefault(x => x.AssessmentSessionId == session.Id);
    if (sessionPackage != null)
    {
        var sessionQuestions = allQuestions
            .Where(q => q.AssessmentPackageId == sessionPackage.AssessmentPackageId)
            .OrderBy(q => q.Id)
            .ToList();
        var sessionResp = allResponses.Where(r => r.AssessmentSessionId == session.Id).ToList();

        ws.Cell(currentRow, 1).Value = "Detail Jawaban";
        ws.Cell(currentRow, 1).Style.Font.Bold = true;
        ws.Range(currentRow, 1, currentRow, 6).Merge();
        currentRow++;

        // Table header
        ws.Cell(currentRow, 1).Value = "No";
        ws.Cell(currentRow, 2).Value = "Soal";
        ws.Cell(currentRow, 3).Value = "Tipe";
        ws.Cell(currentRow, 4).Value = "Jawaban Peserta";
        ws.Cell(currentRow, 5).Value = "Jawaban Benar";
        ws.Cell(currentRow, 6).Value = "Status";
        ws.Range(currentRow, 1, currentRow, 6).Style.Font.Bold = true;
        ws.Range(currentRow, 1, currentRow, 6).Style.Fill.BackgroundColor = XLColor.LightBlue;
        currentRow++;

        int no = 1;
        foreach (var q in sessionQuestions)
        {
            string tipe = q.QuestionType ?? "MultipleChoice";

            if (tipe == "Essay")
            {
                ws.Cell(currentRow, 1).Value = no++;
                ws.Cell(currentRow, 2).Value = q.QuestionText;
                ws.Cell(currentRow, 3).Value = "Essay";
                ws.Cell(currentRow, 4).Value = "Essay – manual grading (lihat Penilaian Essay)";
                ws.Cell(currentRow, 5).Value = "—";
                ws.Cell(currentRow, 6).Value = "—";
                currentRow++;
                continue;
            }

            var responses = sessionResp.Where(r => r.PackageQuestionId == q.Id && r.PackageOptionId.HasValue).ToList();
            string jawabanText;
            bool correct;

            if (!responses.Any())
            {
                // Soal tanpa response (Abandoned skip soal)
                jawabanText = "Tidak dijawab";
                correct = false;
            }
            else if (tipe == "MultipleChoice")
            {
                var optId = responses.First().PackageOptionId!.Value;
                var opt = q.Options.FirstOrDefault(o => o.Id == optId);
                jawabanText = opt?.OptionText ?? "—";
                correct = opt?.IsCorrect == true;
            }
            else // MultipleAnswer
            {
                var selectedIds = responses.Select(r => r.PackageOptionId!.Value).ToHashSet();
                jawabanText = string.Join(", ",
                    q.Options.Where(o => selectedIds.Contains(o.Id)).Select(o => o.OptionText));
                var correctIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
                correct = selectedIds.SetEquals(correctIds);
            }

            string correctText = string.Join(", ", q.Options.Where(o => o.IsCorrect).Select(o => o.OptionText));

            ws.Cell(currentRow, 1).Value = no++;
            ws.Cell(currentRow, 2).Value = q.QuestionText;
            ws.Cell(currentRow, 3).Value = tipe == "MultipleChoice" ? "MC" : "MA";
            ws.Cell(currentRow, 4).Value = jawabanText;
            ws.Cell(currentRow, 5).Value = correctText;
            ws.Cell(currentRow, 6).Value = correct ? "✓" : "✗";
            currentRow++;
        }
    }
```

- [ ] **Step 2: Auto-fit columns**

Tambah sebelum `}` penutup foreach session:

```csharp
    ws.Columns().AdjustToContents();
```

- [ ] **Step 3: Verify build + UAT**

Run: `dotnet build && dotnet run`. Export → tab peserta Completed → verify tabel "Detail Jawaban" tampil semua MC/MA, status ✓/✗, kolom "Jawaban Benar" terisi. Tab peserta Abandoned → verify soal yang tidak dijawab tampil "Tidak dijawab".

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(v3.18-phase1): per-peserta Detail Jawaban table (MC+MA, Tidak dijawab handling)"
```

---

## Task 10: Per-Peserta Sheet — Variant B (Manual Entry)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (isi method `WriteManualEntrySection` stub dari Task 7)

- [ ] **Step 1: Implementasi method**

Replace stub di akhir class:

```csharp
private int WriteManualEntrySection(ClosedXML.Excel.IXLWorksheet ws, AssessmentSession session, int startRow)
{
    int row = startRow;
    ws.Cell(row, 1).Value = "Info Sertifikasi Manual";
    ws.Cell(row, 1).Style.Font.Bold = true;
    ws.Range(row, 1, row, 2).Merge();
    row++;

    var fields = new (string Label, string? Value)[]
    {
        ("Penyelenggara", session.Penyelenggara),
        ("Kota",           session.Kota),
        ("Sub Kategori",   session.SubKategori),
        ("Tipe Sertifikat", session.CertificateType),
    };
    foreach (var (label, value) in fields)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = value ?? "—";
        row++;
    }

    // Hyperlink ManualSertifikatUrl
    ws.Cell(row, 1).Value = "Link Sertifikat";
    ws.Cell(row, 1).Style.Font.Bold = true;
    if (!string.IsNullOrWhiteSpace(session.ManualSertifikatUrl))
    {
        ws.Cell(row, 2).Value = session.ManualSertifikatUrl;
        ws.Cell(row, 2).SetHyperlink(new ClosedXML.Excel.XLHyperlink(session.ManualSertifikatUrl));
    }
    else
    {
        ws.Cell(row, 2).Value = "—";
    }
    row++;

    return row;
}
```

- [ ] **Step 2: Verify field nama AssessmentSession**

Run `dotnet build`. Kalau error "AssessmentSession does not contain Penyelenggara/Kota/...", grep nama field aktual:

```bash
```

Run: `Grep pattern="Penyelenggara|ManualSertifikatUrl" path=Models/AssessmentSession.cs` — sesuaikan nama property kalau berbeda.

- [ ] **Step 3: UAT**

Run `dotnet run`. Export grup yang punya 1+ session dengan `IsManualEntry = true`. Verify tab peserta manual berisi section "Info Sertifikasi Manual" + hyperlink aktif, NO ElemenTeknis/Chart/Detail Jawaban.

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat(v3.18-phase1): per-peserta Variant B (Manual Entry section)"
```

---

## Task 11: Performance — Parallel PNG Generate

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs`

- [ ] **Step 1: Refactor PNG generate ke parallel pre-compute**

Sebelum `foreach (var session in eligibleSessions)`, tambah pre-compute paralel:

```csharp
// Parallel PNG pre-compute (CPU-bound) — cap concurrency biar tidak starvation thread pool
var pngCache = new Dictionary<int, byte[]>();
var pngTasks = eligibleSessions
    .Where(s => !s.IsManualEntry)
    .Select(s => new
    {
        SessionId = s.Id,
        EtData = allEtScores
            .Where(et => et.AssessmentSessionId == s.Id)
            .OrderBy(e => e.ElemenTeknis)
            .Select(e => (e.ElemenTeknis, e.QuestionCount > 0 ? (double)e.CorrectCount / e.QuestionCount * 100 : 0d))
            .ToList()
    })
    .Where(x => x.EtData.Count >= 3)
    .ToList();

await Parallel.ForEachAsync(
    pngTasks,
    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
    (item, ct) =>
    {
        var png = HcPortal.Helpers.SpiderChartRenderer.RenderRadarPng(item.EtData);
        lock (pngCache) { pngCache[item.SessionId] = png; }
        return ValueTask.CompletedTask;
    });
```

- [ ] **Step 2: Ganti inline `RenderRadarPng` di Task 8 jadi cache lookup**

Di block Spider Chart PNG (Task 8), replace inline render:

```csharp
    // BEFORE
    if (sessionEt.Count >= 3)
    {
        var chartData = ...;
        var png = HcPortal.Helpers.SpiderChartRenderer.RenderRadarPng(chartData);
        ...
    }

    // AFTER
    if (pngCache.TryGetValue(session.Id, out var png) && png.Length > 0)
    {
        using var ms = new MemoryStream(png);
        var pic = ws.AddPicture(ms, $"spider-{session.Id}")
            .MoveTo(ws.Cell(currentRow, 1))
            .WithSize(400, 400);
        currentRow += 22;
    }
```

- [ ] **Step 3: Verify build + UAT benchmark**

Run: `dotnet build && dotnet run`. Export grup dengan 20 peserta. Catat response time (dev tools Network tab). Export grup 50+ peserta kalau ada → catat response time. Expected < 30 detik untuk 50 peserta.

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "perf(v3.18-phase1): parallel PNG pre-compute (cap MaxDegreeOfParallelism=Cores)"
```

---

## Task 12: Manual UAT Full Checklist + Final Commit

**Files:** None (UAT only)

- [ ] **Step 1: Jalankan UAT checklist Phase 1 (spec 5.10)**

Tickbox di branch lokal sebelum push:

- [ ] Export 1 peserta Completed → verify struktur sheet, chart muncul
- [ ] Export 10 peserta mix Completed/Abandoned → verify N+1 sheets, Abandoned dapat sheet, In Progress/Not Started/Cancelled tidak
- [ ] Export 50+ peserta → verify response < 30 detik, file size masuk akal (3–5 MB)
- [ ] Export dengan Manual Entry session → verify Variant B sheet (info sertifikat manual, no chart)
- [ ] Export session ElemenTeknis < 3 elemen → verify tabel tampil, chart skip
- [ ] Buka file di Excel + LibreOffice → verify chart render OK kedua aplikasi
- [ ] Export grup dengan peserta yang nama sangat panjang (>25 char) → verify sheet name truncated tepat di 31 char, tidak ada collision
- [ ] Login sebagai HC (bukan Admin) → verify export tetap accessible (sesuai `[Authorize(Roles = "Admin, HC")]`)
- [ ] Login sebagai Worker → verify export return 403/redirect ke login

- [ ] **Step 2: Pre-commit checklist DEV_WORKFLOW §5**

```
- [ ] dotnet build pass (tanpa warning baru)
- [ ] dotnet run + manual verify di http://localhost:5277
- [ ] Golden path & edge case dicek manual
- [ ] DB lokal: no migration Phase 1 → skip
- [ ] (Optional) Playwright tests pass
- [ ] No migration file
- [ ] Team IT di-notify (commit hash, no migration flag)
```

- [ ] **Step 3: Tag milestone phase**

```bash
git tag -a v3.18-phase1-complete -m "Milestone v3.18 Phase 1: Export Per-Peserta complete"
```

- [ ] **Step 4: Update spec status di memory + push**

User notify IT team commit hash via channel komunikasi yang biasa dipakai (Teams/WhatsApp), include flag "No migration".

```bash
git push origin main
git push origin v3.18-phase1-complete
```

---

## Spec Coverage Map

| Spec Section | Covered by Task |
|---|---|
| 3.1 Output Structure (Sheet "Summary" + N) | Task 4, 5, 6 |
| 3.1 Sheet name sanitization `{NIP}_{FullName}` | Task 3, 6 |
| 3.2 Variant A header | Task 6 |
| 3.2 Section Analisis ElemenTeknis | Task 7 |
| 3.2 Section Detail Jawaban + "Tidak dijawab" | Task 9 |
| 3.2 Variant B (Manual Entry) | Task 10 |
| 3.3 Sumber data (PackageUserResponses + SessionElemenTeknisScores) | Task 6 (pre-load), 7, 9 |
| 3.4 Spider Chart render SkiaSharp | Task 2, 8 |
| 3.4 Skip kondisi (< 3 elemen) | Task 8, 11 |
| 3.5 Performance (`Task.WhenAll` paralel) | Task 11 |
| 3.6 Permission `[Authorize(Roles = "Admin, HC")]` | Inherited from existing line 3650 (no change) |
| 3.7 Breaking change "Results"→"Summary" | Task 4 |
| 5.10 Manual UAT checklist | Task 12 |
| 5.12 Library dep csproj | Task 1 |

Tidak ada gap.
