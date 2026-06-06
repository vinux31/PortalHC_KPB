# Phase 320: Assessment Export Per-Peserta Excel - Pattern Map

**Mapped:** 2026-05-21
**Files analyzed:** 4 (1 modify config, 2 create helpers, 1 modify controller method)
**Analogs found:** 4 / 4 (semua exact match in-repo)
**Language:** Bahasa Indonesia (per CLAUDE.md)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `HcPortal.csproj` | package config | build-time | `HcPortal.csproj` line 12 (`ClosedXML`) + line 25 (`QuestPDF`) — existing PackageReference siblings | exact |
| `Helpers/SpiderChartRenderer.cs` | static utility (pure function, no I/O) | transform (data -> PNG bytes) | `Helpers/CertNumberHelper.cs` (static class, namespace `HcPortal.Helpers`, no DI, pure transform) | role-match (no SkiaSharp peer di codebase) |
| `Helpers/SheetNameSanitizer.cs` | static utility (string sanitize) | transform (string -> string) | `Helpers/CertNumberHelper.cs` + `Helpers/PaginationHelper.cs` (static utility pattern, ringan, pure function) | exact (PaginationHelper paling mirip ukuran & shape) |
| `Controllers/AssessmentAdminController.cs` — method `ExportAssessmentResults` | controller action (existing, extend) | request-response + EF Core read + file download | **SAME METHOD** `ExportAssessmentResults` (line 3651-3795) — extending diri sendiri | exact (refactor in-place) |

---

## Pattern Assignments

### `HcPortal.csproj` (package config)

**Analog:** `HcPortal.csproj` itself, baris 12 + 25.

**PackageReference pattern** (existing, baris 11-26):
```xml
<ItemGroup>
  <PackageReference Include="ClosedXML" Version="0.105.0" />
  ...
  <PackageReference Include="QuestPDF" Version="2026.2.2" />
</ItemGroup>
```

**Apply pattern:**
- Tambahkan 2 baris **di dalam `<ItemGroup>` existing** (jangan bikin ItemGroup baru) setelah baris `QuestPDF` (line 25):

```xml
<PackageReference Include="SkiaSharp" Version="3.116.1" />
<PackageReference Include="SkiaSharp.NativeAssets.Win32" Version="3.116.1" />
```

**Property convention** (`HcPortal.csproj` baris 3-9):
- `<TargetFramework>net8.0</TargetFramework>` (TIDAK ubah)
- `<Nullable>enable</Nullable>` (TIDAK ubah — nullable annotation aktif untuk helper baru)
- `<ImplicitUsings>enable</ImplicitUsings>` (helper baru bisa skip `using System;` etc)
- `<NoWarn>$(NoWarn);CA1416</NoWarn>` (Windows-only — SkiaSharp.NativeAssets.Win32 selaras dengan deployment IIS Windows)

**Verifikasi pasca-edit:** `dotnet restore && dotnet build`.

---

### `Helpers/SpiderChartRenderer.cs` (static utility, transform)

**Analog struktur file:** `Helpers/CertNumberHelper.cs` (paling mirip ukuran + dokumentasi XML-comment) + `Helpers/ExcelExportHelper.cs` (image/binary-producing helper).

**Namespace + class declaration pattern** (`Helpers/CertNumberHelper.cs:1-11`):
```csharp
using Microsoft.EntityFrameworkCore;     // <-- ganti jadi: using SkiaSharp;
using HcPortal.Data;                     // <-- drop (tidak butuh DbContext)

namespace HcPortal.Helpers
{
    /// <summary>
    /// Shared helper for ...
    /// </summary>
    public static class CertNumberHelper
    {
        public static string ToRomanMonth(int month) => ...
```

**Apply ke `SpiderChartRenderer`:**
```csharp
using SkiaSharp;

namespace HcPortal.Helpers
{
    public static class SpiderChartRenderer
    {
        /// <summary>
        /// Render radar/spider chart sebagai PNG byte array.
        /// </summary>
        public static byte[] RenderRadarPng(IList<(string label, double percentage)> data, int size = 500)
        {
            ...
        }
    }
}
```

**File-scoped convention:**
- Brace-style `namespace HcPortal.Helpers { ... }` (block scope, BUKAN file-scoped) — konsisten dengan **semua** file `Helpers/*.cs` existing.
- `Nullable enable` aktif global (lihat `HcPortal.csproj:5`) — gunakan `IList<...>? data` kalau argumen nullable, atau `data == null` guard di body (sesuai 320-RESEARCH.md Task 2 step 1).
- `using` directives diposisikan di luar `namespace` block (konsisten `CertNumberHelper.cs`, `ExcelExportHelper.cs`, `FileUploadHelper.cs`).

**Implementation body lengkap:** Lihat `320-RESEARCH.md` Task 2 Step 1 (verbatim, sudah codebase-verified — copy persis tanpa modifikasi).

**Apply di sheet via:** Lihat pattern AddPicture di section controller bawah.

---

### `Helpers/SheetNameSanitizer.cs` (static utility, transform)

**Analog terbaik:** `Helpers/PaginationHelper.cs` (paling ringan, pure function, no DI, no I/O).

**Skeleton pattern** (`Helpers/PaginationHelper.cs:1-15`):
```csharp
namespace HcPortal.Helpers
{
    public record PaginationResult(int CurrentPage, int TotalPages, int TotalCount, int Skip, int Take);

    public static class PaginationHelper
    {
        public static PaginationResult Calculate(int totalCount, int page, int pageSize)
        {
            ...
        }
    }
}
```

**Apply ke `SheetNameSanitizer`:**
```csharp
namespace HcPortal.Helpers
{
    public static class SheetNameSanitizer
    {
        private static readonly char[] InvalidChars = { '\\', '/', '?', '*', '[', ']', ':' };
        private const int ExcelSheetNameLimit = 31;

        public static string Sanitize(string nip, string fullName, ISet<string> usedNames) { ... }
        private static string ScrubChars(string s) { ... }
    }
}
```

**Conventions diikuti:**
- TIDAK ada `using` directives (tidak perlu) — sama dengan `PaginationHelper.cs` yang juga tanpa `using`.
- `private static readonly` untuk konstanta non-primitive (array) — pattern .NET standar.
- `private const` untuk integer literal (`ExcelSheetNameLimit = 31`).
- Doc-comment XML `/// <summary>` wajib pada method public (konsisten `CertNumberHelper.cs:23-27`, `ExcelExportHelper.cs:9-12`, `FileUploadHelper.cs:8-11`).

**Implementation body lengkap:** Lihat `320-RESEARCH.md` Task 3 Step 1 (verbatim — copy persis).

---

### `Controllers/AssessmentAdminController.cs` (controller, request-response refactor)

**Analog:** Method `ExportAssessmentResults` line 3651-3795 itu sendiri. Ini bukan create-new — ini **refactor in-place dari method yang sama**.

#### Existing structure to preserve (baseline read):

**Method signature** (line 3649-3651, JANGAN diubah):
```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ExportAssessmentResults(string title, string category, DateTime scheduleDate)
```
> REQ EXP-07 (HC parity) sudah inherent via attribute existing.

**EF Core query pattern** (line 3654-3659, copy untuk pre-load per-peserta sheet):
```csharp
var sessions = await _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Title == title
             && a.Category == category
             && a.Schedule.Date == scheduleDate.Date)
    .ToListAsync();
```
> Pattern `_context.<DbSet>.Include().Where().ToListAsync()` — pakai sama untuk pre-load `PackageUserResponses` / `SessionElemenTeknisScores` / `UserPackageAssignments` / `PackageQuestions.Include(o => o.Options)` di Task 6 (avoid N+1).

**Empty-guard pattern** (line 3661-3665):
```csharp
if (!sessions.Any())
{
    TempData["Error"] = "No sessions found for this assessment group.";
    return RedirectToAction("ManageAssessment");
}
```
> Pattern `TempData["Error"]` + `RedirectToAction` — pakai sama untuk early-return.

**Sheet creation pattern** (line 3725-3727):
```csharp
using var workbook = new XLWorkbook();
var worksheet = workbook.Worksheets.Add("Results");   // <-- Task 4: ubah jadi "Summary"
```
> Pattern `using var workbook = new XLWorkbook()` + `workbook.Worksheets.Add(name)` — pakai sama untuk loop per-peserta (Task 6).

**Cell + styling pattern** (line 3733-3759):
```csharp
worksheet.Cell(1, 1).Value = "Laporan Assessment";
worksheet.Range(1, 1, 1, totalCols).Merge();
worksheet.Cell(1, 1).Style.Font.Bold = true;
worksheet.Cell(1, 1).Style.Font.FontSize = 14;
...
worksheet.Range(2, 1, 6, 1).Style.Font.Bold = true;
```
> Pattern `.Cell(row, col).Value = ...`, `.Range(r1,c1,r2,c2).Merge()`, `.Style.Font.Bold = true`, `.Style.Fill.BackgroundColor = XLColor.LightBlue` (line 3774) — pakai sama untuk header per-peserta + section headers ET / Detail Jawaban / Info Sertifikasi Manual.

**Filename + return pattern** (line 3791-3794):
```csharp
var safeTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^\w]", "_");
var fileName = $"{safeTitle}_{scheduleDate:yyyyMMdd}_Results.xlsx";   // <-- Task 4: ubah _Results -> _Summary
return ExcelExportHelper.ToFileResult(workbook, fileName, this);
```
> **PENTING:** Method existing sudah pakai `ExcelExportHelper.ToFileResult(workbook, fileName, this)` (lihat `Helpers/ExcelExportHelper.cs:27-38`) yang otomatis `AdjustToContents()` semua worksheet + return `FileContentResult` dengan MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`. **Reuse helper ini** — JANGAN reimplement.
> Konsekuensi: `ws.Columns().AdjustToContents();` di research Task 9 Step 2 sebenarnya **redundant** (sudah di-handle helper). Boleh dihilangkan atau dibiarkan (idempotent, no harm).

#### New patterns to introduce (Task 5-11):

**Eligible filter + sheet name registry** (Task 5):
```csharp
var eligibleSessions = sessions
    .Where(s => s.Status == "Completed" || s.Status == "Abandoned")
    .OrderBy(s => s.User?.FullName ?? "")
    .ToList();

var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Summary" };
```

**Pre-load batch query pattern** (Task 6 — mirror existing pattern line 3654-3659):
```csharp
var eligibleSessionIds = eligibleSessions.Select(s => s.Id).ToList();
var allResponses = await _context.PackageUserResponses
    .Where(r => eligibleSessionIds.Contains(r.AssessmentSessionId))
    .ToListAsync();
var allEtScores = await _context.SessionElemenTeknisScores
    .Where(et => eligibleSessionIds.Contains(et.AssessmentSessionId))
    .ToListAsync();
var allQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => packageIds.Contains(q.AssessmentPackageId))
    .ToListAsync();
```
> Konsisten dengan pattern `Include().Where().ToListAsync()` existing controller.

**Per-peserta sheet loop** (Task 6, reuse `XLWorkbook.Worksheets.Add` pattern):
```csharp
foreach (var session in eligibleSessions)
{
    string sheetName = HcPortal.Helpers.SheetNameSanitizer.Sanitize(
        session.User?.NIP ?? "NA",
        session.User?.FullName ?? "Unknown",
        usedSheetNames);
    var ws = workbook.Worksheets.Add(sheetName);
    // ... header cells (Task 6)
    // ... ET section (Task 7)
    // ... chart embed (Task 8 + 11)
    // ... detail jawaban (Task 9)
}
```

**PNG embed pattern** (Task 8, ClosedXML AddPicture):
```csharp
using var ms = new MemoryStream(png);
var pic = ws.AddPicture(ms, $"spider-{session.Id}")
    .MoveTo(ws.Cell(currentRow, 1))
    .WithSize(400, 400);
currentRow += 22;
```
> ClosedXML API native. Tidak ada peer di codebase yang embed image — research code authoritative.

**Hyperlink pattern** (Task 10, ClosedXML XLHyperlink):
```csharp
ws.Cell(row, 2).Value = session.ManualSertifikatUrl;
ws.Cell(row, 2).SetHyperlink(new ClosedXML.Excel.XLHyperlink(session.ManualSertifikatUrl));
```

**Parallel PNG pre-compute** (Task 11, NEW pattern di codebase):
```csharp
var pngCache = new Dictionary<int, byte[]>();
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
> No `Parallel.ForEachAsync` precedent di repo. Pertimbangkan `ConcurrentDictionary<int, byte[]>` untuk hilangkan `lock` boilerplate (CONTEXT.md D Claude's Discretion — diizinkan).

**Field access AssessmentSession Manual Entry** (Task 10, verified `Models/AssessmentSession.cs:130-147`):
| Field | Line | Type |
|-------|------|------|
| `IsManualEntry` | 130 | `bool` (default `false`) |
| `ManualSertifikatUrl` | 135 | `string?` |
| `Penyelenggara` | 138 | `string?` |
| `Kota` | — | `string?` (sekitar 139-141) |
| `SubKategori` | — | `string?` (sekitar 142-145) |
| `CertificateType` | 147 | `string?` |

> Semua nullable — guard dengan `?? "—"` (em-dash konsisten dengan existing line 3703-3706 `"—"`).

---

## Shared Patterns

### Excel Output (apply ke seluruh edit di `ExportAssessmentResults`)

**Source:** `Helpers/ExcelExportHelper.cs:27-38`
**Apply to:** Final return statement (sudah existing — tidak boleh diubah).

```csharp
public static FileContentResult ToFileResult(XLWorkbook workbook, string fileName, ControllerBase controller)
{
    foreach (var ws in workbook.Worksheets)
        ws.Columns().AdjustToContents();
    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    return controller.File(
        stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
}
```
> Auto adjust-to-contents + MIME content-type sudah dihandle helper. Jangan duplicate logic.

### Authorization (apply ke method-level)

**Source:** `Controllers/AssessmentAdminController.cs:3650`
```csharp
[Authorize(Roles = "Admin, HC")]
```
> Inherit attribute existing. REQ EXP-06 + EXP-07 (Admin + HC parity) covered. Worker block test (Playwright 03 Task 12) memverifikasi.

### Em-dash Placeholder

**Source:** `Controllers/AssessmentAdminController.cs:3703-3706` + 3714
```csharp
string resultText = a.Status == "Cancelled" ? "—" : ...
```
> Gunakan `"—"` atau literal "—" konsisten untuk null/empty placeholder. Hindari `"-"` atau `"N/A"` (inkonsisten dengan existing style).

### EF Core Pre-load (avoid N+1)

**Source:** `Controllers/AssessmentAdminController.cs:3654-3686`
**Apply to:** Semua data lookup per session di Task 6-9 (bukan loop query in-flight).
> Pattern: load `_context.<DbSet>.Where(<id IN list>).ToListAsync()` SATU KALI di luar `foreach`, lalu filter in-memory dengan LINQ `.Where(x => x.SessionId == session.Id)`.

### Logging on Error (jika ditambahkan)

**Source:** `Controllers/AssessmentAdminController.cs:3643`
```csharp
logger.LogError(ex, "GetDeleteImpact failed for type={Type} id={Id}", type, id);
```
> Kalau parallel PNG generate Task 11 muncul exception bubble-up, pakai pattern logger.LogError yang sama. Method existing `ExportAssessmentResults` saat ini TIDAK ada try/catch — keep konsisten kecuali ada justifikasi (SkiaSharp native crash bisa pertimbangkan wrap).

---

## No Analog Found

Files / pattern tanpa close match di codebase — planner fallback ke RESEARCH.md verbatim:

| Pattern | Reason | Fallback |
|---------|--------|----------|
| SkiaSharp `SKBitmap` / `SKCanvas` / `SKPath` chart drawing | Tidak ada penggunaan SkiaSharp di repo (lib NEW Task 1) | Copy 100% dari `320-RESEARCH.md` Task 2 Step 1 |
| `Parallel.ForEachAsync` + concurrent cache | Tidak ada precedent paralel CPU-bound di repo | Copy 100% dari `320-RESEARCH.md` Task 11 Step 1; consider ConcurrentDictionary upgrade (D Claude's Discretion) |
| Playwright auth + download regression test | Project belum punya Playwright suite | Implement script sebagai standalone `tests/playwright/` atau skip otomasi sesuai D-05 hybrid strategy |

---

## Commit Message Convention (apply per task)

**Source:** Recent commits `9e7f1b3c`, `bfb7cbdc`, `df37e7ca`, `386a1dc6`
```
feat(v17.0-p320): <subject>
refactor(v17.0-p320)!: <subject>     # breaking change
perf(v17.0-p320): <subject>
```
> Scope `(v17.0-p320)` — konsisten milestone v17.0 phase 320. Breaking change suffix `!` + paragraf `BREAKING CHANGE:` body (lihat Task 4).

---

## Metadata

**Analog search scope:**
- `Helpers/*.cs` (5 files: ExcelExportHelper, CertNumberHelper, PaginationHelper, FileUploadHelper, MaintenanceScopeCatalog)
- `Controllers/AssessmentAdminController.cs` line 3640-3795 (ExportAssessmentResults full body)
- `HcPortal.csproj` root
- `Models/AssessmentSession.cs` line 130-147 (Manual Entry fields verification)

**Files scanned:** 8
**Pattern extraction date:** 2026-05-21
**Codebase verification:** All fields/lines confirmed via direct Read 2026-05-21.
