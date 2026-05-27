# Phase 325: Security Hardening (P01 + P02 + P05) — Research

**Researched:** 2026-05-27
**Domain:** ASP.NET Core MVC + EF Core 8 security hardening (file upload + delete error UX)
**Confidence:** HIGH (spec locked + codebase verified line-by-line)

## Summary

Spec `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §5 sudah memberikan snippet code verbatim untuk semua 3 fix. Hasil cross-check terhadap codebase 2026-05-27: **semua line target masih akurat** dengan 2 catatan drift kecil (lihat §Spec Drift Check). Helper static (`FileUploadHelper`) tidak punya akses DI ke `ILogger`, namun controller pemanggil sudah inject `_logger` — pattern recommended: pass `ILogger` sebagai parameter ke `SaveFileAsync` (lebih simpel daripada refactor jadi instance class). Magic byte `FF D8 FF` 3-byte sufficient untuk semua varian JPEG (JFIF/EXIF/SPIFF) karena variant byte ada di byte ke-4 — spec snippet correct.

**Primary recommendation:** Eksekusi spec §5 verbatim dengan 4 tambahan ringan:
1. Pass `ILogger?` opsional ke `SaveFileAsync` untuk D-10 LogWarning (nullable supaya unit test tetap mudah).
2. Extract magic byte ke `AssessmentConstants.FileValidation.MagicBytes` + helper `MatchesMagicByte(ext, header)` per D-09 supaya unit test target nya pure function.
3. Buat `HcPortal.Tests/HcPortal.Tests.csproj` sibling, registrasikan ke `HcPortal.sln` existing (sudah ada di root).
4. Tambah 1 line `_logger.LogWarning(...)` ke 3 endpoint P05 saat catch `DbUpdateException` (defensive jika ada FK lain selain RenewsTrainingId/RenewsSessionId yang muncul di masa depan).

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** P01 fix — `Path.GetFileName(file.FileName)` strip directory component sebelum compose `safeFileName` di `SaveFileAsync` body.
- **D-02:** P02 fix — hardcoded magic byte switch (3 format: PDF `25 50 44 46`, JPG `FF D8 FF`, PNG `89 50 4E 47`). No NuGet library.
- **D-03:** P02 reads first 8 bytes via `stream.Read(header, 0, 8)`, then `stream.Position = 0` reset.
- **D-04:** P05 = quick patch only — pre-check referencing count + try/catch `DbUpdateException` + `TempData["Error"]` user-friendly. Soft delete defer v20.0.
- **D-05:** P05 catch level: `DbUpdateException` saja (bukan broad `Exception`), `_logger.LogWarning(ex, ...)`.
- **D-06:** Error message strings locked verbatim per spec §5.2 + §5.3.
- **D-07:** No EF migration (pure code changes).
- **D-08:** Tambah project xUnit baru `HcPortal.Tests/` sibling ke `HcPortal.csproj`.
- **D-09:** Extract magic byte signatures ke `AssessmentConstants.FileValidation.MagicBytes` (Dictionary + helper method `MatchesMagicByte`).
- **D-10:** Log warning kalau filename mengandung path separator atau `..` di `SaveFileAsync`.
- **D-11:** Pre-check referencing `AssessmentSession` di awal endpoint sebelum buka transaction scope (`AssessmentAdminController.DeleteAssessment`).

### Claude's Discretion

- xUnit version pinning (match SDK stable terbaru untuk net8.0).
- Test naming convention (`MethodName_Scenario_ExpectedResult`).
- Magic byte `.jpeg` alias (lookup dictionary value sharing dengan `.jpg`).
- LogWarning structured logging format (`{Original}`, `{Safe}`).

### Deferred Ideas (OUT OF SCOPE)

- Soft delete proper (`IsDeleted` column + global query filter) — v20.0 candidate.
- `MimeDetective` NuGet library — fallback kalau hardcoded miss.
- DB CHECK constraint untuk FK mutual exclusion (P09) — sudah app-level mitigated via `CK_TrainingRecord_RenewalChain` + `CK_AssessmentSession_RenewalChain` (verified di `ApplicationDbContext.cs:168-169, 231-232`).
- RBAC integration test (P12).
- CI integration untuk xUnit project — lokal saja Phase 325.
- Migrate Playwright tests/ ke xUnit.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| File extension + magic byte validation | API/Backend (helper static) | — | Pure function, no I/O di luar stream read 8-byte. Test target unit. |
| Path traversal strip | API/Backend (helper static) | Browser (defensive — sudah enforced server-side) | `Path.GetFileName()` server-side authoritative. Browser filename input tidak dipercaya. |
| FK pre-check delete | API/Backend (controller) | Database (FK NoAction enforce) | Pre-check di app layer untuk UX friendly; DB FK NoAction sebagai safety net (akan throw kalau pre-check missed race window). |
| Audit log path traversal attempt | API/Backend (`_logger`) | — | Defensive observability — attack attempts captured untuk forensik. |
| TempData error flash | API/Backend (controller) → Browser (Razor view render) | — | Pattern existing project. |

## Codebase Findings

> Verified terhadap working tree 2026-05-27. Line numbers/identifier shapes confirmed via `Read` tool.

### `Helpers/FileUploadHelper.cs` (61 baris, current state)

| Section | Line | Status |
|---------|------|--------|
| `public static class FileUploadHelper` | 6 | OK |
| `ValidateCertificateFile(IFormFile? file)` | 12-24 | OK — saat ini hanya extension + size check. Magic byte HARUS ditambah di sini. |
| Extension check `Path.GetExtension(file.FileName)` | 16 | **MISSING `.ToLowerInvariant()`** — spec line 100 minta tambah ini (HashSet `OrdinalIgnoreCase` jadi bukan bug fungsional, tapi micro-cleanup). |
| Size check | 20 | OK |
| `SaveFileAsync(file, webRootPath, subFolder)` | 30-45 | OK — line 37 compose `safeFileName` tanpa `Path.GetFileName()` strip → **P01 target**. |
| Guid + timestamp compose | 36-37 | OK |
| FileStream write | 39-42 | OK |
| `DeleteFile(webRootPath, relativeUrl)` | 51-59 | OK — tidak disentuh Phase 325. |

### `Models/AssessmentConstants.cs` (48 baris)

| Section | Line | Status |
|---------|------|--------|
| `FileValidation.MaxCertificateFileSizeBytes = 10 * 1024 * 1024` | 32 | OK |
| `FileValidation.AllowedCertificateExtensions` HashSet `OrdinalIgnoreCase` | 37-40 | OK — `.pdf .jpg .jpeg .png` |
| **Magic byte dict** | — | **MISSING — D-09 target tambah `MagicBytes` Dictionary + `MatchesMagicByte()` helper.** |

### `Controllers/TrainingAdminController.cs` (1085 baris)

| Endpoint | Line | Status |
|----------|------|--------|
| Ctor inject `_logger` (ILogger) | 17, 24, 27 | OK — siap dipanggil P05 catch |
| `AddTraining` POST — file validation inline | **206-215** | **CODE-SMELL DUPLICATE** — duplicates `ValidateCertificateFile` logic inline. Spec §3 table list ini sebagai P02 target. Plan harus refactor pakai `FileUploadHelper.ValidateCertificateFile`. |
| `AddTraining` per-worker file validation inline | 221-233 | **CODE-SMELL DUPLICATE** ke-2 — sama inline pattern. Refactor jadi loop call `ValidateCertificateFile`. (Line 581 `AddManualAssessment` sudah pakai helper — model yang benar.) |
| `EditTraining` POST — file validation inline | **459-471** | **CODE-SMELL DUPLICATE** ke-3 — sama inline pattern. Spec §3 P02 mention `TrainingAdminController.cs:206-215` tapi tidak mention :459. **PLAN-PHASE PERLU AWARE site ke-3 ini.** |
| `AddManualAssessment` POST — uses helper | 581 | OK — already calls `FileUploadHelper.ValidateCertificateFile(wc.CertificateFile)` |
| `EditManualAssessment` POST — uses helper | 681 | OK |
| **`DeleteTraining`** (P05 target #1) | **523-548** | OK — line 527 `DeleteTraining(int id)`, line 529 fetch, line 539 `Remove(record)`, line 540 `SaveChangesAsync`. Plan tambah pre-check + try/catch antara line 538 dan 547. |
| **`DeleteManualAssessment`** (P05 target #2 — disebut spec sebagai `:744`) | **732-753** | **CONFIRMED** — line 744 = `_context.AssessmentSessions.Remove(session)`. Ini endpoint `DeleteManualAssessment` (bukan `DeleteTraining` variant). **Entity yang dihapus = AssessmentSession** (bukan TrainingRecord). Pre-check query harus check referencing dari TR + AS dengan `RenewsTrainingId == session.Id`? **TIDAK** — `RenewsTrainingId` itu FK ke `TrainingRecords.Id`, bukan AssessmentSessions.Id. Untuk AssessmentSession.Id referencing, query nya `RenewsSessionId == session.Id` di kedua tabel TR + AS. **Spec §5.3 generic statement "ganti query referencing sesuai entity" — plan harus tegasin distinction ini.** |

### `Controllers/AssessmentAdminController.cs` (6221 baris)

| Section | Line | Status |
|---------|------|--------|
| `DeleteAssessment(int id)` entry | 2011 | OK |
| Logger fetch via `RequestServices.GetRequiredService<ILogger<>>` | 2013 | Pattern existing — bukan field. Plan reuse. |
| Outer try block start | 2015 | OK |
| Fetch `assessment` | 2017-2018 | OK |
| D-19 PreTest/PostTest guard | 2030-2035 | OK |
| **`using var tx = BeginTransactionAsync()`** | **2040** | **TX SCOPE OPEN HERE.** D-11 says pre-check **SEBELUM** tx open → insert pre-check di range line 2036-2039. |
| Phase 312 cascade logic | 2042-2133 | UNCHANGED |
| **`_context.AssessmentSessions.Remove(assessment)`** | **2136** | OK — line yang spec mention. |
| `await tx.CommitAsync()` | 2139 | OK |
| Audit log | 2146-2152 | OK |
| Outer catch `Exception ex` | 2163-2168 | **DRIFT WARNING** — existing catch generic `Exception`, NOT `DbUpdateException`. Plan D-05 minta narrow `DbUpdateException` — tapi di sini ada catch `Exception` lain (eg, audit-log try/catch line 2154). **Pattern keep:** tambah `catch (DbUpdateException ex) { ... }` SEBELUM `catch (Exception ex)` supaya friendly message untuk FK violation, generic message untuk lain-lain. Atau letakkan TRY/CATCH baru hanya around pre-check redirect (di luar existing try). Plan-phase decide. |

### `Data/ApplicationDbContext.cs` — Renewal FK Behavior (CRITICAL)

| FK | Line | OnDelete | Konsekuensi |
|----|------|----------|-------------|
| TR.RenewsTrainingId → TR | 157-160 | **NoAction** | Hard delete parent TR → SQL throw error 547. |
| TR.RenewsSessionId → AS | 162-165 | **NoAction** | Hard delete parent AS → SQL throw error 547. |
| AS.RenewsSessionId → AS | 220-223 | **NoAction** | Hard delete parent AS → SQL throw error 547. |
| AS.RenewsTrainingId → TR | 225-228 | **NoAction** | Hard delete parent TR → SQL throw error 547. |
| TR CHECK constraint mutual exclusion | 168-169 | — | Already prevents both FKs simultaneously set. |
| AS CHECK constraint mutual exclusion | 231-232 | — | Same. |

**Insight:** Pre-check query untuk **delete TrainingRecord(id)** = `CountAsync` di 2 tabel filtering `RenewsTrainingId == id`:
- `_context.TrainingRecords.CountAsync(t => t.RenewsTrainingId == id)`
- `_context.AssessmentSessions.CountAsync(a => a.RenewsTrainingId == id)`

Pre-check query untuk **delete AssessmentSession(id)** = `CountAsync` di 2 tabel filtering `RenewsSessionId == id`:
- `_context.TrainingRecords.CountAsync(t => t.RenewsSessionId == id)`
- `_context.AssessmentSessions.CountAsync(a => a.RenewsSessionId == id)`

**Both `RenewsTrainingId` and `RenewsSessionId` are indexed** (`IX_*_RenewsTrainingId` + `IX_*_RenewsSessionId` di migration `20260319001833_AddRenewalChainFKs`). Pre-check perf O(log n) — aman walau ribuan record.

### `HcPortal.sln` (root, 25 baris)

| Detail | Status |
|--------|--------|
| Solution file exists | ✓ — `Microsoft Visual Studio Solution File, Format Version 12.00` |
| Single project entry `HcPortal.csproj` | OK |
| GUID `{B16CC238-1A00-06DF-1F9A-DEF516C8CD8D}` | OK |
| xUnit project entry | **MISSING — plan harus tambah `HcPortal.Tests.csproj` ke .sln** via `dotnet sln HcPortal.sln add HcPortal.Tests/HcPortal.Tests.csproj`. |

### `tests/` directory existing

Verified isinya Playwright TypeScript (`e2e`, `helpers`, `playwright.config.ts`, `tsconfig.json`, `node_modules`). **Tidak ada konflik** dengan rencana buat `HcPortal.Tests/` sibling.

### `.NET SDK`

`dotnet --version` = **8.0.418** stable di mesin lokal. Target net8.0 di `HcPortal.csproj` aligned.

## Spec Drift Check

| Item | Spec Statement | Codebase Reality 2026-05-27 | Action |
|------|----------------|----------------------------|--------|
| `Helpers/FileUploadHelper.cs:37` (P01) | Compose `safeFileName` dengan `file.FileName` raw | Confirmed line 37 verbatim | None — apply fix per §5.1 |
| `Helpers/FileUploadHelper.cs:12-24` (P02) | `ValidateCertificateFile` hanya extension + size | Confirmed | None — apply fix per §5.2 |
| `TrainingAdminController.cs:206-215` (P02) | Inline file validation duplicate | **CONFIRMED** + 2 sites lain ditemukan: line 221-233 (per-worker AddTraining) + line 459-471 (EditTraining) | **NEW: Plan harus refactor 3 site, bukan 1.** |
| `TrainingAdminController.cs:539` (P05 target) | `_context.TrainingRecords.Remove(record)` | Confirmed line 539 verbatim | None — apply pre-check per §5.3 |
| `TrainingAdminController.cs:744` (P05 target #2) | "endpoint kedua" tanpa snippet | **RESOLVED**: line 744 = `_context.AssessmentSessions.Remove(session)` di `DeleteManualAssessment` (line 732-753). Entity = **AssessmentSession** bukan TrainingRecord. Pre-check query gunakan `RenewsSessionId == session.Id` di 2 tabel. | **NEW: Plan harus eksplisit gunakan RenewsSessionId untuk site ini, bukan RenewsTrainingId.** |
| `AssessmentAdminController.cs:2136` (P05 target #3) | `_context.AssessmentSessions.Remove(assessment)` di dalam tx | Confirmed line 2136 verbatim di dalam tx (line 2040 open). | **NEW: D-11 pre-check sebelum tx — sisip antara line 2035 dan 2040.** Existing outer catch `Exception ex` (line 2163) → plan tambah `catch (DbUpdateException ex)` BEFORE generic catch. |
| `AllowedCertificateExtensions` lowercase | Spec line 100 minta `.ToLowerInvariant()` | Line 16 belum lowercase (HashSet `OrdinalIgnoreCase` jadi tidak bug, tapi micro-cleanup) | Minor — include di plan plan-helper-fixes. |
| `AssessmentConstants.MagicBytes` (D-09) | Tidak ada di spec, decision baru di CONTEXT.md | Tidak ada di constants file | New addition per D-09. |
| `ILogger` injection ke static helper (D-10) | Tidak ada di spec | `FileUploadHelper` static class | New addition — plan pakai opsional param `ILogger?` ke `SaveFileAsync`. |
| `HcPortal.sln` file di root | CONTEXT.md `code_context.Integration Points` bilang "Solution file (.sln) — currently belum ada di root" | **DRIFT**: Solution file ADA (`HcPortal.sln` 25 baris, 1 project entry). | **CORRECTION**: Plan reuse existing `HcPortal.sln`, tambah project Tests entry, bukan bikin sln baru. |

## Architecture Patterns

### Pattern 1: Static Helper + Optional ILogger Parameter (D-10 implementation)

**What:** Pass `ILogger?` sebagai parameter opsional ke method static yang butuh logging, default `null` supaya unit test gampang.

**When to use:** Helper static yang sebelumnya pure tapi sekarang butuh audit trail. Refactor jadi instance class = overkill untuk 1 method.

**Example:**
```csharp
// Helpers/FileUploadHelper.cs (after Phase 325)
public static async Task<string?> SaveFileAsync(
    IFormFile? file,
    string webRootPath,
    string subFolder,
    ILogger? logger = null)
{
    if (file == null || file.Length == 0) return null;

    var uploadDir = Path.Combine(webRootPath, subFolder);
    Directory.CreateDirectory(uploadDir);

    var uniqueId = Guid.NewGuid().ToString("N")[..8];
    // D-01: Strip directory component
    var originalName = Path.GetFileName(file.FileName);

    // D-10: Audit trail kalau original beda dari stripped
    if (logger != null && !string.Equals(originalName, file.FileName, StringComparison.Ordinal))
    {
        logger.LogWarning(
            "Path traversal attempt: filename={Original} stripped to {Safe}",
            file.FileName, originalName);
    }

    var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{uniqueId}_{originalName}";
    var filePath = Path.Combine(uploadDir, safeFileName);
    await using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    return $"/{subFolder.Trim().Trim('\\', '/')}/{safeFileName}";
}
```

**Caller change (controllers):**
```csharp
// SEBELUM
await FileUploadHelper.SaveFileAsync(wc.CertificateFile, _env.WebRootPath, "uploads/certificates");

// SESUDAH
await FileUploadHelper.SaveFileAsync(wc.CertificateFile, _env.WebRootPath, "uploads/certificates", _logger);
```

### Pattern 2: Magic Byte Dictionary di Constants (D-09)

```csharp
// Models/AssessmentConstants.cs (extension)
public static class FileValidation
{
    public const long MaxCertificateFileSizeBytes = 10 * 1024 * 1024;
    public static readonly HashSet<string> AllowedCertificateExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".pdf", ".jpg", ".jpeg", ".png" };

    // Phase 325 D-09: Magic byte signatures per extension (lowercase keys)
    // Value = array of valid byte prefixes (multiple = OR match)
    public static readonly Dictionary<string, byte[][]> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"]  = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },                // %PDF
        [".jpg"]  = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },                       // JFIF/EXIF/SPIFF universal prefix
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },                       // alias .jpg
        [".png"]  = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47 } }                  // PNG signature
    };

    public static bool MatchesMagicByte(string ext, byte[] header)
    {
        if (!MagicBytes.TryGetValue(ext, out var prefixes)) return false;
        foreach (var prefix in prefixes)
        {
            if (header.Length < prefix.Length) continue;
            bool match = true;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (header[i] != prefix[i]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }
}
```

**ValidateCertificateFile** jadi:
```csharp
public static (bool IsValid, string? Error) ValidateCertificateFile(IFormFile? file)
{
    if (file == null || file.Length == 0) return (true, null);

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!AssessmentConstants.FileValidation.AllowedCertificateExtensions.Contains(ext))
        return (false, "Hanya file PDF, JPG, dan PNG yang diperbolehkan.");

    if (file.Length > AssessmentConstants.FileValidation.MaxCertificateFileSizeBytes)
        return (false, "Ukuran file maksimal 10MB.");

    // P02: Magic byte
    using var stream = file.OpenReadStream();
    var header = new byte[8];
    var read = stream.Read(header, 0, 8);
    stream.Position = 0;

    if (read < 3) // smallest magic prefix is 3 bytes (JPG)
        return (false, "Isi file tidak cocok dengan ekstensi (magic byte mismatch).");

    if (!AssessmentConstants.FileValidation.MatchesMagicByte(ext, header))
        return (false, "Isi file tidak cocok dengan ekstensi (magic byte mismatch).");

    return (true, null);
}
```

### Pattern 3: P05 Pre-Check Outside Transaction (D-11 implementation)

```csharp
// AssessmentAdminController.cs DeleteAssessment — insert antara line 2035 dan 2040
// (sesudah PreTest/PostTest guard, sebelum BeginTransactionAsync)

// PHASE 325 P05: Pre-check referencing rows sebelum buka tx
var refTr = await _context.TrainingRecords
    .CountAsync(t => t.RenewsSessionId == id);
var refAs = await _context.AssessmentSessions
    .CountAsync(a => a.RenewsSessionId == id);

if (refTr + refAs > 0)
{
    var total = refTr + refAs;
    TempData["Error"] = $"Tidak bisa hapus: {total} sertifikat lain "
                      + "menggunakan record ini sebagai sumber renewal. "
                      + "Hapus atau update sertifikat pemakai terlebih dulu.";
    return RedirectToAction("ManageAssessment");
}

// existing tx open
using var tx = await _context.Database.BeginTransactionAsync();
```

**Plus** tambah `catch (DbUpdateException ex)` sebelum generic `catch (Exception ex)` (line 2163):

```csharp
catch (DbUpdateException ex)
{
    logger.LogWarning(ex, "Delete failed for AssessmentSession {Id}: FK constraint", id);
    TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
    return RedirectToAction("ManageAssessment");
}
catch (Exception ex) // existing
{
    logger.LogError(ex, "Error deleting assessment {Id}", id);
    TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi.";
    return RedirectToAction("ManageAssessment");
}
```

### Anti-Patterns to Avoid

- **Refactor `FileUploadHelper` jadi instance class** → overkill untuk 1 method butuh logger. Opsional param lebih simpel.
- **Pakai `Exception` broad catch untuk P05** → kehilangan info FK-specific. D-05 minta narrow `DbUpdateException`.
- **Pre-check di dalam transaction** → useless karena setiap pre-check query open implicit tx connection. Pre-check di luar = fail-fast + simpler.
- **Skip inline duplicate refactor (line 206-215, 221-233, 459-471 TrainingAdminController)** → magic byte check tidak akan run untuk 3 endpoint ini kalau inline-duplicate tidak direplace dengan `FileUploadHelper.ValidateCertificateFile`. **Ini critical** — kalau plan hanya touch helper tanpa refactor inline duplicate, P02 fix tidak efektif di 3 endpoint Add/Edit Training.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Path traversal sanitize | Custom regex `[^a-zA-Z0-9_.]` strip | `Path.GetFileName(file.FileName)` | Built-in handle backslash + forward slash + UNC + relative path edge cases. |
| Magic byte signature DB | Custom signature table maintained by hand | Hardcoded 3-format dict (sufficient untuk Portal HC) ATAU `MimeDetective` NuGet (5000+ format) | Spec D-02 explicit: hardcode untuk 3 format, library overkill. |
| FK violation detect | Inspect `SqlException.Number == 547` di InnerException | `catch (DbUpdateException ex)` | EF Core wraps semua DB constraint violation jadi `DbUpdateException` — narrow catch sudah cukup tanpa SqlException inspect. |
| Test harness | Custom xUnit-like asserter | xUnit + Microsoft.NET.Test.Sdk + xunit.runner.visualstudio | Industry standard, full IDE integration. |

**Key insight:** Spec sudah memilih minimum-tool path (`Path.GetFileName` + hardcoded magic byte). Plan jangan over-engineer.

## Common Pitfalls

### Pitfall 1: Inline Validation Duplicate Bypass P02
**What goes wrong:** Plan hanya update `FileUploadHelper.ValidateCertificateFile` tapi 3 endpoint `TrainingAdminController` (AddTraining line 206-215, AddTraining per-worker line 221-233, EditTraining line 459-471) masih pakai inline `allowedExtensions.Contains(ext)` check — magic byte validation **tidak jalan** di 3 endpoint ini.
**Why it happens:** Spec line 35 di table P02 hanya mention `Helpers/FileUploadHelper.cs:12-24` + `TrainingAdminController.cs:206-215`. Researcher sebelumnya tidak grep semua site duplicate.
**How to avoid:** Plan harus eksplisit refactor 3 inline sites pakai `FileUploadHelper.ValidateCertificateFile`.
**Warning signs:** Postman test upload `.exe→.pdf` lewat `/Admin/AddTraining` lolos = P02 fix bypassed.

### Pitfall 2: Salah `RenewsSessionId` vs `RenewsTrainingId` untuk Pre-Check P05
**What goes wrong:** Plan copy-paste pre-check spec §5.3 ke `DeleteManualAssessment` (line 744) atau `DeleteAssessment` (line 2136) tanpa swap FK column — query `RenewsTrainingId == session.Id` MISS karena session.Id itu AssessmentSession.Id, harusnya cek `RenewsSessionId`.
**Why it happens:** Spec §5.3 generic snippet pakai TR contoh; "Adaptasi: ganti query referencing" line 168 ambigu.
**How to avoid:** Plan eksplisit 3 endpoint dengan FK column yang benar:
- `DeleteTraining(id)` → `RenewsTrainingId == id` di TR + AS
- `DeleteManualAssessment(id)` → `RenewsSessionId == id` di TR + AS
- `DeleteAssessment(id)` (assessment session) → `RenewsSessionId == id` di TR + AS
**Warning signs:** Delete AS dengan referencing → tidak block (pre-check miss), lalu DB FK throw → falls ke catch.

### Pitfall 3: ILogger Param Break Existing Caller Sites
**What goes wrong:** Method signature `SaveFileAsync` ditambah param required → semua caller site existing (AddManualAssessment line 601, EditManualAssessment line 702, plus AddTraining/EditTraining setelah refactor) break compile.
**Why it happens:** Tidak set default `null` untuk param baru.
**How to avoid:** Param `ILogger? logger = null` (nullable + default null). Existing caller tetap compile; plan tambah `, _logger` argument hanya di site baru kalau mau log.
**Warning signs:** `dotnet build` error CS7036 "no argument given for required parameter".

### Pitfall 4: xUnit Project Auto-Load Pollute Build
**What goes wrong:** `HcPortal.Tests.csproj` di root → `dotnet build` auto-include test project → CI/release build slower.
**Why it happens:** SDK auto-discover sibling csproj.
**How to avoid:** Letakkan di subfolder `HcPortal.Tests/HcPortal.Tests.csproj`. Solution explicit reference di `HcPortal.sln`. `dotnet build HcPortal.csproj` (target single project) tetap fast.
**Warning signs:** Build time tiba-tiba tambah 5-10 detik.

### Pitfall 5: `stream.Read` Returns Less Than 8 Bytes Untuk File Kecil
**What goes wrong:** File 4-byte (corrupt PDF placeholder) → `stream.Read(header, 0, 8)` returns 4, sisanya header[4..7] = 0x00 → PNG check `0x89 0x50 0x4E 0x47` di byte 0-3 mungkin lolos kalau corrupt file kebetulan punya 4 byte yang sama.
**Why it happens:** `stream.Read` non-blocking, return bytes-read bukan exception kalau buffer > stream length.
**How to avoid:** Capture return value `var read = stream.Read(header, 0, 8)` + reject kalau `read < 3` (smallest prefix = JPG 3-byte). Implemented di pattern §Pattern 2 di atas.
**Warning signs:** Unit test "1-byte file" lolos validation salah.

### Pitfall 6: `dotnet test` Run Playwright Tests
**What goes wrong:** Playwright di `tests/` punya `package.json` → tidak ada interaksi dengan `dotnet test`. Tapi user typo `dotnet test tests/` atau plan dokumentasi bingung.
**Why it happens:** Naming collision: 2 directory `tests/` (Playwright) + `HcPortal.Tests/` (xUnit). Mata tidak fokus easy confusion.
**How to avoid:** Plan dokumentasi eksplisit: `dotnet test HcPortal.Tests/` untuk unit, `npx playwright test` (cwd `tests/`) untuk E2E.
**Warning signs:** README/plan ambigu tentang test directory.

## Code Examples

### Unit Test Skeleton — FileUploadHelperTests.cs

```csharp
// HcPortal.Tests/FileUploadHelperTests.cs
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.AspNetCore.Http;
using Xunit;
using System.IO;
using System.Text;

namespace HcPortal.Tests;

public class FileUploadHelperTests
{
    private static IFormFile MakeFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    [Fact]
    public void ValidateCertificateFile_NullFile_ReturnsValid()
    {
        var (ok, err) = FileUploadHelper.ValidateCertificateFile(null);
        Assert.True(ok);
        Assert.Null(err);
    }

    [Fact]
    public void ValidateCertificateFile_ValidPdf_ReturnsValid()
    {
        var pdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
        var (ok, _) = FileUploadHelper.ValidateCertificateFile(MakeFile("test.pdf", pdf));
        Assert.True(ok);
    }

    [Fact]
    public void ValidateCertificateFile_ValidJpg_ReturnsValid()
    {
        var jpg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        var (ok, _) = FileUploadHelper.ValidateCertificateFile(MakeFile("test.jpg", jpg));
        Assert.True(ok);
    }

    [Fact]
    public void ValidateCertificateFile_ValidPng_ReturnsValid()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var (ok, _) = FileUploadHelper.ValidateCertificateFile(MakeFile("test.png", png));
        Assert.True(ok);
    }

    [Fact]
    public void ValidateCertificateFile_ExeRenamedPdf_ReturnsInvalidMagicByte()
    {
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 }; // MZ exe
        var (ok, err) = FileUploadHelper.ValidateCertificateFile(MakeFile("malware.pdf", exe));
        Assert.False(ok);
        Assert.Contains("magic byte", err!);
    }

    [Fact]
    public void ValidateCertificateFile_UnsupportedExtension_ReturnsInvalid()
    {
        var bytes = new byte[] { 0x00, 0x01, 0x02 };
        var (ok, err) = FileUploadHelper.ValidateCertificateFile(MakeFile("test.docx", bytes));
        Assert.False(ok);
        Assert.Contains("PDF, JPG", err!);
    }

    [Fact]
    public void MatchesMagicByte_JpegAliasMatchesJpg()
    {
        var jpg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1, 0x00, 0x00, 0x00, 0x00 }; // EXIF variant
        Assert.True(AssessmentConstants.FileValidation.MatchesMagicByte(".jpeg", jpg));
        Assert.True(AssessmentConstants.FileValidation.MatchesMagicByte(".jpg", jpg));
    }
}
```

### xUnit Project Bootstrap (`HcPortal.Tests/HcPortal.Tests.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\HcPortal.csproj" />
  </ItemGroup>

</Project>
```

**Versions verified 2026-05-27:**
- `Microsoft.NET.Test.Sdk` 17.13.0 — recommended by xUnit v2 official getting-started (xunit.net official site as of 2025-07-04). Latest is 18.5.1 but 17.13.0 is stable+recommended for v2 pairing.
- `xunit` 2.9.3 — current stable v2 (v3 exists but spec/CONTEXT specify simple xUnit v2 path).
- `xunit.runner.visualstudio` 3.0.1 — current stable.
- `coverlet.collector` 6.0.4 — optional, for coverage if desired (D-08 anti-scope says no CI but local coverage OK).

[VERIFIED: WebSearch xunit.net official + nuget.org]

### Solution Integration Commands

```bash
# Buat folder + project + add ke sln
dotnet new xunit -n HcPortal.Tests -o HcPortal.Tests
dotnet sln HcPortal.sln add HcPortal.Tests/HcPortal.Tests.csproj
dotnet add HcPortal.Tests/HcPortal.Tests.csproj reference HcPortal.csproj

# Run test
dotnet test HcPortal.Tests/HcPortal.Tests.csproj
```

## Runtime State Inventory

Phase 325 = pure code changes (no rename, no data migration, no FK schema change). Berikut explicit confirm tiap kategori:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — tidak ada DB row yang punya old/new naming asymmetry. Phase 325 hanya tambah magic-byte gate + pre-check query (read-only) + path strip (defensive, file existing tidak disentuh). | None |
| Live service config | None — tidak ada n8n / Datadog / external service config yang bergantung ke file path atau delete behavior Portal HC. | None |
| OS-registered state | None — tidak ada Windows Task Scheduler / pm2 / launchd yang sentuh helper file upload atau delete endpoint. | None |
| Secrets/env vars | None — fix tidak menambah / mengubah env var atau SOPS key. | None |
| Build artifacts | **New project `HcPortal.Tests/`** akan generate `bin/` + `obj/` baru. `.gitignore` existing sudah cover `bin/` + `obj/` semua subdirectory? **Plan harus verify**: kalau `.gitignore` rules ada di root scope `*/bin/` saja → cover. Kalau hanya `HcPortal/bin/` literal → harus extend ke `HcPortal.Tests/bin/`. | Verify `.gitignore` saat plan execution |

## Risk Mitigation Notes

### Risk 1: P05 TOCTOU Race (Pre-Check Pass → Concurrent INSERT → Delete Throw)

**Scenario:** Pre-check `CountAsync` return 0 (no referencing). Sebelum `Remove + SaveChanges`, user lain POST AddTraining dengan `RenewsTrainingId = id` → insert referencing row baru. Delete fire → DB FK throw 547.

**Mitigation:** `catch (DbUpdateException)` sebagai safety net. Pesan friendly. **Acceptable risk** — pre-check tujuannya UX di happy path, bukan strict ACID isolation. Phase 312 sudah handle TOCTOU pattern serupa di `DeleteAssessment` (re-check responseCount di dalam tx, line 2066-2074), tapi untuk P05 scope kita pakai pattern simpler: pre-check di luar + catch di dalam. Konsisten dengan D-04 quick patch positioning.

**Skip alternative:** Wrap pre-check + delete di tx serializable → overkill untuk Portal HC traffic profile (~puluhan user, dev belum prod). Defer ke v20.0 kalau ada bukti race aktif.

### Risk 2: Magic Byte False Positive untuk PDF Variant Lama

**Scenario:** PDF scan dari mesin foto lama tidak start dengan `%PDF` (header beda?).

**Mitigation:** PDF spec ISO 32000 mandate `%PDF-x.y` di byte 0-7 (versi). Header `25 50 44 46` (`%PDF`) **wajib** untuk PDF valid sejak versi 1.0. [CITED: ISO 32000-1 §7.5.2]

**Test plan:** Manual upload 3 PDF variant — (a) PDF native dari Word/Acrobat, (b) PDF scan multi-page, (c) PDF linearized/web-optimized. Kalau semua lolos = `%PDF` prefix universal. Spec Risk Register §12 mention same mitigation.

**Fallback:** Kalau ada miss real, install `MimeDetective` NuGet. Defer instalasi sampai miss reproduced.

### Risk 3: ILogger DI Pattern Choice — Static Helper Bukan Idiomatic

**Scenario:** Static class + DI = mismatch. Beberapa codebase choose refactor jadi instance class + register di Program.cs.

**Mitigation:** Optional param pattern (`ILogger? logger = null`) = lightweight, zero breaking change ke caller existing, easy unit test (`null` default). Trade-off: caller harus eksplisit pass `_logger` di setiap site baru. **Acceptable** untuk Phase 325 scope (3 endpoint inline refactor + 2 existing helper caller).

**Defer to v20.0:** Refactor `FileUploadHelper` jadi instance class + `IFileUploadService` interface + register di DI container. Out of scope.

### Risk 4: xUnit Infrastructure Setup Gotcha — Web SDK vs Standard SDK

**Scenario:** `HcPortal.csproj` pakai `Microsoft.NET.Sdk.Web` (web app SDK). Test project harus pakai `Microsoft.NET.Sdk` (standard SDK) — jangan copy SDK web ke test project, akan error tentang `wwwroot/` / launch settings.

**Mitigation:** `dotnet new xunit` template default pakai `Microsoft.NET.Sdk` benar. Plan tidak boleh manually copy csproj structure dari HcPortal.csproj — pakai template.

### Risk 5: `.ToLowerInvariant()` Throws NullReferenceException

**Scenario:** `Path.GetExtension(file.FileName)` return empty string untuk file tanpa extension (eg "myfile" tanpa "."). `.ToLowerInvariant()` di empty string = OK return empty. Tapi kalau `file.FileName == null`? `Path.GetExtension(null)` di .NET 8 = `ArgumentNullException`.

**Mitigation:** Function entry sudah guard `if (file == null || file.Length == 0)`. `IFormFile.FileName` adalah `string` (non-nullable) per ASP.NET Core contract → safe. [CITED: Microsoft.AspNetCore.Http.IFormFile]

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit v2 (`xunit` 2.9.3) + `Microsoft.NET.Test.Sdk` 17.13.0 |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (new — Wave 0) |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| Full suite command | Same (single test project) + `npx playwright test` (cwd `tests/`) untuk E2E smoke |
| Phase gate | `dotnet build` green + `dotnet test HcPortal.Tests/` 6/6 pass + manual UI verify per success criterion |

### Phase Requirements → Test Map

| SC ID | Behavior | Test Type | Automated Command | File Exists? |
|-------|----------|-----------|-------------------|-------------|
| SC1 (P01) | Upload `../../test.pdf` tersimpan flat di `uploads/certificates/` | Manual (Postman + filesystem ls) | — | Manual playbook in plan |
| SC2 (P02) | `.exe→.pdf` ditolak magic byte mismatch | Unit test `ValidateCertificateFile_ExeRenamedPdf_ReturnsInvalidMagicByte` | `dotnet test --filter ExeRenamedPdf` | ❌ Wave 0 — `FileUploadHelperTests.cs` |
| SC3 (P02) | PDF/JPG/PNG asli lolos | Unit test x3 `ValidateCertificateFile_Valid{Pdf,Jpg,Png}_ReturnsValid` | `dotnet test --filter "ValidPdf|ValidJpg|ValidPng"` | ❌ Wave 0 |
| SC4 (P05) | Delete TR dengan referencing → TempData error display | Manual UI (1 record A renewed by B → delete A blocked) | — | Manual playbook |
| SC5 (P05) | Delete TR tanpa referencing → sukses normal | Manual UI (record C standalone) | — | Manual playbook |
| SC6 (P02) | Unit test helper `ValidateCertificateFile` 6 case pass | Unit test (covered by SC2+SC3 + null + unsupported ext + jpeg alias) | `dotnet test` (full suite) | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet build` (verify no compile break) + `dotnet test HcPortal.Tests/` (verify 6/6 pass)
- **Per wave merge:** Same + manual Postman path traversal test + manual UI delete test (SC1, SC4, SC5)
- **Phase gate:** All 6 SC verified + `dotnet build` warnings stay constant + Playwright `tests/` regression run (only if scope-relevant scenario exists — likely none, file upload bukan E2E covered)

### Wave 0 Gaps

- [ ] `HcPortal.Tests/HcPortal.Tests.csproj` — new xUnit project sibling
- [ ] `HcPortal.Tests/FileUploadHelperTests.cs` — covers SC2, SC3, SC6 (6 test case)
- [ ] `HcPortal.sln` — add test project entry
- [ ] `.gitignore` verify — confirm `HcPortal.Tests/bin/` + `obj/` ignored (likely already via wildcard, verify)
- [ ] Postman collection / curl playbook — path traversal POST test (SC1)
- [ ] Manual UI test data — 1 TR pair (parent + renewal child) untuk SC4, 1 standalone TR untuk SC5

### Test Data Fixtures Needed

| Fixture | Format | Purpose | Source |
|---------|--------|---------|--------|
| `valid-sample.pdf` | byte[] inline `{0x25,0x50,0x44,0x46,...}` | SC3 valid PDF magic byte | Inline test helper |
| `valid-sample.jpg` | byte[] inline `{0xFF,0xD8,0xFF,0xE0,...}` | SC3 valid JPG | Inline |
| `valid-sample.png` | byte[] inline `{0x89,0x50,0x4E,0x47,...}` | SC3 valid PNG | Inline |
| `malware.exe→.pdf` | byte[] inline `{0x4D,0x5A,...}` (MZ DOS) | SC2 magic mismatch | Inline |
| `path-traversal.txt` | Postman raw body multipart filename `"../../test.pdf"` | SC1 manual | Plan playbook |
| TR pair (parent + renewal) | Seed via UI atau direct SQL insert | SC4 delete blocked | Manual / SEED_WORKFLOW |
| TR standalone | Seed via UI | SC5 delete sukses | Manual |

**Seed klasifikasi (per CLAUDE.md SEED_WORKFLOW):** Temporary + local-only. Snapshot DB sebelum, restore sesudah. Catat di SEED_JOURNAL.md.

## Project Constraints (from CLAUDE.md)

| Directive | Source | Plan Impact |
|-----------|--------|------------|
| Response Bahasa Indonesia | CLAUDE.md line 3 | Plan + execute commit messages + comment code dalam Bahasa Indonesia (existing pattern). |
| DEV_WORKFLOW: Lokal verify wajib | CLAUDE.md §Develop Workflow | Plan harus include `dotnet build` + `dotnet run` + cek `http://localhost:5277` step sebelum commit. |
| DEV_WORKFLOW: IT promo tanggung jawab Team IT | CLAUDE.md item 5 | Plan **tidak** include deploy step. Akhir Phase 325 commit + push + notify IT batch akhir Phase 327 (per spec §11). |
| SEED_WORKFLOW: Snapshot + restore | CLAUDE.md §Seed Data Workflow | Plan untuk SC4/SC5 verify → snapshot DB sebelum buat TR pair, restore sesudah test, catat di SEED_JOURNAL.md. |
| ❌ Jangan edit DB Dev/Prod langsung | CLAUDE.md | Plan hanya touch lokal. Pre-check query read-only — aman secara konseptual. |
| ❌ Jangan push tanpa verifikasi lokal | CLAUDE.md | Plan task akhir = manual smoke (SC1 Postman + SC2/3 unit test + SC4/5 UI). |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | xUnit v2 lebih cocok daripada v3 untuk Phase 325 scope (D-08 anti-scope minimal) | Code Examples §xUnit Project Bootstrap | LOW — v3 valid juga; v2 official getting-started 2025-07 masih primary doc, v3 fitur baru tidak dibutuhkan. |
| A2 | `IFormFile.FileName` non-nullable di ASP.NET Core | Risk 5 | LOW — ASP.NET Core contract sejak .NET Core 2.0. [CITED: docs.microsoft.com Microsoft.AspNetCore.Http.IFormFile] |
| A3 | PDF file scan dari mesin foto lama tetap punya `%PDF` prefix | Risk 2 | LOW — ISO 32000-1 mandate. Spec §12 Risk Register sudah mention 3-sample manual test sebagai mitigation. |
| A4 | `.gitignore` existing project sudah cover `**/bin/` + `**/obj/` | Runtime State Inventory | LOW — standard .NET .gitignore pattern. Plan verify saat execution. |
| A5 | Pre-check `CountAsync` < 50ms terhadap index `IX_*_RenewsTrainingId` walau 10k+ row | Codebase Findings §FK Behavior | LOW — indexed B-tree O(log n). Portal HC current scale ~ratusan TR, scaling headroom besar. |
| A6 | Existing `outer catch (Exception)` di `DeleteAssessment` line 2163 boleh diapit dengan `catch (DbUpdateException)` baru di atasnya | Spec Drift Check + Pattern 3 | LOW — C# catch clause ordering: specific sebelum general adalah idiom standar. |

## Open Questions

1. **TrainingAdminController.cs:206-215 + 221-233 + 459-471 inline duplicate — refactor di Phase 325 atau defer?**
   - What we know: 3 site inline duplicate `allowedExtensions.Contains(ext)` logic + size check. `FileUploadHelper.ValidateCertificateFile` belum dipakai di 3 site ini.
   - What's unclear: Spec §3 P02 list `TrainingAdminController.cs:206-215` saja, tidak mention :221-233 dan :459-471. **Apakah ini scope creep atau bug nyata?**
   - Recommendation: **Plan harus refactor 3 site jadi panggil `FileUploadHelper.ValidateCertificateFile`** — alasan: kalau tidak, magic byte fix P02 tidak efektif di 3 endpoint Add/Edit Training (Postman test `.exe→.pdf` lolos lewat AddTraining). Plan-phase user dapat di-flag manual untuk konfirmasi sebelum proceed.

2. **Existing `catch (Exception ex)` di AssessmentAdminController:2163 — preserve atau replace?**
   - What we know: Current outer catch generic `Exception`, log error + TempData "Gagal menghapus assessment".
   - What's unclear: D-05 minta narrow `DbUpdateException`. Tapi non-FK exception (eg, timeout) tetap perlu di-handle.
   - Recommendation: **Tambah `catch (DbUpdateException)` SEBELUM existing `catch (Exception)`** — preserve generic catch untuk safety, tambah specific friendly message untuk FK case. Plan §Architecture Patterns Pattern 3 sudah snippet.

3. **Snapshot DB perlu untuk SC4/SC5 atau cukup pakai data existing?**
   - What we know: SC4 butuh TR pair (parent + renewal child). DB lokal kemungkinan sudah punya pair dari testing sesi lain (sertifikat ecosystem testing 2026-05-26).
   - What's unclear: Apakah cleanup data manual lebih simpel daripada snapshot+restore.
   - Recommendation: **Tetap snapshot+restore per CLAUDE.md §Seed Data Workflow** — workflow audit trail penting, walau cleanup manual mungkin lebih cepat. Defer keputusan ke plan-phase.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | Build + test | ✓ | 8.0.418 | — |
| `dotnet test` CLI | xUnit run | ✓ (built into SDK) | — | — |
| SQL Server lokal (LocalDb / Express) | DB verify SC4/SC5 | Presumed ✓ (existing Portal HC dev env per CLAUDE.md) | — | Skip DB test, mock |
| `sqlcmd` | DB snapshot SEED_WORKFLOW | Presumed ✓ | — | SSMS GUI manual |
| Postman or curl | SC1 path traversal upload test | Presumed ✓ | — | Custom HTTP client script |
| Node.js + npx | Playwright `tests/` regression (optional) | Presumed ✓ | — | Skip regression run |

**Missing dependencies with no fallback:** None — all critical tools available based on prior phase work (Phase 323 ship 2026-05-26).

**Missing dependencies with fallback:** None blocking.

## Security Domain

### Applicable ASVS Categories

Per OWASP ASVS 4.0 + 5.0 draft (V12 = Files & Resources):

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No (existing auth via Identity, tidak disentuh) | — |
| V3 Session Management | No | — |
| V4 Access Control | Partial — endpoint sudah `[Authorize(Roles = "Admin, HC")]` (existing, tidak disentuh) | Existing |
| V5 Input Validation | **Yes (P01 + P02)** | `Path.GetFileName()` strip + magic byte signature check |
| V6 Cryptography | No | — |
| V12 Files & Resources | **Yes (P01 + P02 + P05)** | V12.2.1 magic byte content validation, V12.3 file path traversal prevention, V12.1 file size limit (existing 10MB) |

[CITED: OWASP ASVS 4.0 V12 Files-Resources https://github.com/OWASP/ASVS/blob/master/4.0/en/0x20-V12-Files-Resources.md]

### Known Threat Patterns for ASP.NET Core MVC

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Path traversal upload (`../../`) | Tampering | `Path.GetFileName()` strip — verified via OWASP cheat sheet [CITED: cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html] |
| MIME spoofing (rename `.exe→.pdf`) | Tampering | Magic byte signature validation — OWASP ASVS V12.2.1 recommend (caveat: not alone, but combo dengan extension + size). |
| Null byte truncation (`file.pdf%00.exe`) | Tampering | `.NET 5+` `Path.GetFileName()` handle null byte — modern runtime strict. Pre-.NET 5 had risk. [VERIFIED: dotnet runtime] |
| FK violation expose stack trace | Information Disclosure | Narrow `catch (DbUpdateException)` + friendly TempData message — current spec compliant. |
| TOCTOU race (pre-check pass → concurrent insert → FK throw) | TOCTOU | Defense-in-depth: pre-check (UX) + DB FK NoAction (data integrity) + catch (graceful fallback). Acceptable for current scale. |

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Extension-only validation | Magic byte + extension + size combo | OWASP ASVS V12.2 (V4.0+) | Defense-in-depth — extension dapat disepoof, magic byte harder. |
| `Exception` broad catch | `DbUpdateException` narrow catch | EF Core 2.0+ wrap pattern stable sejak 2018 | Cleaner intent + better forensik. |
| Manual byte signature DB | `MimeDetective` NuGet (5000+ format) | Available since 2020 | For project scope (3 format), hardcoded simpler + zero-dep. |
| Hard delete + DB throw | Soft delete (`IsDeleted` filter) | DDD pattern sejak ~2015 | Long-term solution, defer v20.0 per D-04. |
| Path validation via regex | `Path.GetFileName()` native | .NET Framework 1.0 | Built-in safer than custom regex. |

**Deprecated/outdated:**
- Inline `allowedExtensions.Contains(ext)` di controller (3 site di TrainingAdminController) — pattern Phase ≤ 200, dropped in favor of `FileUploadHelper` since 2026-03. Refactor di Phase 325.

## Sources

### Primary (HIGH confidence)
- `docs/superpowers/specs/2026-05-26-v19.0-portal-hc-bug-fixes-design.md` §5 — spec verbatim snippets locked.
- `.planning/phases/325-security-hardening-p01-p02-p05/325-CONTEXT.md` — 11 D-decisions.
- Codebase verified line-by-line via `Read` tool 2026-05-27:
  - `Helpers/FileUploadHelper.cs` (61 baris)
  - `Models/AssessmentConstants.cs` (48 baris)
  - `Controllers/TrainingAdminController.cs` (1085 baris — sampled 1, 195-235, 450-548, 600-755)
  - `Controllers/AssessmentAdminController.cs` (6221 baris — sampled 1990-2170)
  - `Data/ApplicationDbContext.cs` (verified OnDelete behavior line 152-232)
  - `Migrations/20260319001833_AddRenewalChainFKs.cs` (verified IX_*_RenewsTrainingId index)
  - `HcPortal.sln` (verified existing 1-project sln)
  - `HcPortal.csproj` (net8.0 + EF Core 8.0.0)
- `dotnet --version` lokal = 8.0.418.

### Secondary (MEDIUM confidence)
- [OWASP File Upload Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html) — magic byte + path traversal best practice.
- [OWASP ASVS V12 Files-Resources](https://github.com/OWASP/ASVS/blob/master/4.0/en/0x20-V12-Files-Resources.md) — V12.2.1 content validation.
- [xUnit.net v2 Getting Started](https://xunit.net/docs/getting-started/v2/getting-started) — recommended `Microsoft.NET.Test.Sdk` 17.13.0 + `xunit` 2.9.3.
- [JPEG Signature Format](https://www.file-recovery.com/jpg-signature-format.htm) — `FF D8 FF` universal prefix untuk JFIF/EXIF/SPIFF.
- [Microsoft Path.GetFileName](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfilename) — handle DirectorySeparatorChar + AltDirectorySeparatorChar.
- [SQL Server Error 547 — FK Conflict](https://builder.ai2sql.io/blog/sql-server-error-547-foreign-key-conflict) — EF wraps as `DbUpdateException`.

### Tertiary (LOW confidence)
- WebSearch ecosystem articles (StackHawk, Praetorian) — supporting context, not authoritative. Cross-verified dengan OWASP primary.

## Metadata

**Confidence breakdown:**
- Standard stack (xUnit + Test.Sdk): HIGH — xUnit official docs + matched lokal SDK.
- Architecture (helper static + optional ILogger param): HIGH — verified codebase pattern existing.
- Pitfalls (TrainingAdminController inline duplicate): HIGH — grep + read confirm 3 site duplicate, NOT mentioned in spec.
- Spec drift (`HcPortal.sln` exists, CONTEXT.md says doesn't): HIGH — file open + read 25 baris confirmed.
- Magic byte universal `FF D8 FF` untuk JPG variant: HIGH — cross-verified WebSearch + JPEG spec.
- xUnit version 17.13.0 vs 18.5.1 choice: MEDIUM — both valid, recommend 17.13.0 untuk v2 pairing stability per official getting-started.

**Research date:** 2026-05-27
**Valid until:** 2026-06-26 (stable spec + code already locked di CONTEXT.md; xUnit version may bump but compat unchanged)

---

## RESEARCH COMPLETE

**Phase:** 325 - Security Hardening (P01 + P02 + P05)
**Confidence:** HIGH

### Key Findings

1. **Spec snippets verbatim accurate** — all line targets (`FileUploadHelper.cs:37`, `:12-24`, `TrainingAdminController.cs:539`, `:744`, `AssessmentAdminController.cs:2136`) match codebase 2026-05-27 exactly.
2. **Line :744 RESOLVED** — `DeleteManualAssessment` endpoint removes `AssessmentSession` (not TrainingRecord). Pre-check query harus pakai `RenewsSessionId == id`, BUKAN `RenewsTrainingId`. Critical distinction yang plan harus encode explicit.
3. **3 inline duplicate validation sites discovered** di `TrainingAdminController.cs` (line 206-215, 221-233, 459-471) yang tidak pakai `FileUploadHelper.ValidateCertificateFile`. **Magic byte P02 fix tidak efektif kecuali 3 site ini direfactor pakai helper.** Spec §3 hanya mention site 206-215. **Plan harus address all 3** atau P02 bypass-able via AddTraining/EditTraining.
4. **`HcPortal.sln` ALREADY EXISTS di root** (CONTEXT.md incorrect claim "belum ada"). Plan reuse + add Test project entry, bukan generate new sln.
5. **Optional `ILogger?` parameter pattern** = simplest for D-10 (static helper + audit log) — no instance class refactor needed. Existing caller sites tetap compile karena param `= null` default.
6. **`OnDelete(DeleteBehavior.NoAction)` confirmed** untuk 4 renewal FK — hard delete parent dengan referencing child → SQL error 547 → EF wraps `DbUpdateException`. Spec D-05 narrow catch correct.
7. **JPG 3-byte `FF D8 FF` universal** untuk JFIF/EXIF/SPIFF variant (4th byte E0/E1/E8 varies but irrelevant to validation gate). Spec D-02 hardcode correct.

### File Created
`.planning/phases/325-security-hardening-p01-p02-p05/325-RESEARCH.md`

### Confidence Assessment

| Area | Level | Reason |
|------|-------|--------|
| Standard Stack (xUnit + Test.Sdk versions) | HIGH | xUnit.net official getting-started v2 + nuget.org cross-check |
| Architecture (helper static + optional ILogger) | HIGH | Existing codebase pattern verified line-by-line |
| Pitfalls (TrainingAdminController inline duplicates) | HIGH | Grep + read confirm 3 sites not in spec |
| Spec Drift (sln exists, CONTEXT claim wrong) | HIGH | File read 25 baris confirmed |
| P05 FK column distinction (RenewsTrainingId vs RenewsSessionId) | HIGH | Migration + DbContext + Model triangulated |
| Magic byte universal prefix | HIGH | Multiple sources + JPEG spec |

### Open Questions (deferred to plan-phase decision)

1. Refactor 3 TrainingAdminController inline duplicate sites di Phase 325 atau defer? **Recommendation: refactor sekarang** — kalau tidak, P02 bypass-able.
2. Preserve existing `catch (Exception)` di DeleteAssessment line 2163 + tambah specific `DbUpdateException` di atasnya, atau replace?  **Recommendation: preserve + insert specific catch above.**
3. SC4/SC5 verify pakai DB snapshot+restore atau cleanup manual? **Recommendation: snapshot per CLAUDE.md SEED_WORKFLOW.**

### Ready for Planning

Research complete. Planner dapat lanjut buat PLAN.md files dengan addressing 3 open questions sebagai split decision di plan structure. Estimated plan structure:
- **Plan 01:** Bootstrap `HcPortal.Tests/` + add ke `HcPortal.sln` + 1 sample test (Wave 0 infrastructure)
- **Plan 02:** Implement P01 + P02 helper changes + `AssessmentConstants.MagicBytes` extension + LogWarning + 6 unit test
- **Plan 03:** Refactor 3 inline duplicate sites di `TrainingAdminController` (line 206-215, 221-233, 459-471) → call `ValidateCertificateFile`
- **Plan 04:** Implement P05 di 3 endpoint delete (`DeleteTraining`, `DeleteManualAssessment`, `DeleteAssessment`) dengan correct FK column per endpoint + narrow `DbUpdateException` catch
- **Plan 05:** Manual UAT + Postman path traversal test + DB snapshot/restore workflow per CLAUDE.md
