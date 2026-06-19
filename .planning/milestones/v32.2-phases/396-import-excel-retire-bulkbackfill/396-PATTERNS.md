# Phase 396: Import Excel + retire BulkBackfill - Pattern Map

**Mapped:** 2026-06-18
**Files analyzed:** 11 (3 NEW · 4 MODIFY · 4 REMOVE/edit-for-removal)
**Analogs found:** 9 with concrete excerpt / 9 mappable (removals need no analog)
**Verified against:** working tree HEAD `2345083d` (Phase 395 committed; line numbers re-verified live — research's ranges still accurate).

> **Konteks (CLAUDE.md):** alat internal HC, semua copy UI = Bahasa Indonesia. Phase 396 = lapisan translasi tipis (sel Excel → `InjectAnswerSpec`) + hard-remove. Nol grading/validasi baru — reuse `InjectBatchAsync` (393) + `AssessmentScoreAggregator.Compute`/`PreviewInjectScore` (395). 0 migration. ClosedXML 0.105.0 (sudah ada).

---

## File Classification

| File | New/Mod | Role | Data Flow | Closest Analog | Match Quality |
|------|---------|------|-----------|----------------|---------------|
| `Helpers/InjectExcelHelper.cs` | NEW | utility (helper, EF-free static) | file-I/O + transform | `Helpers/ExcelExportHelper.cs` (gen shape) + `Controllers/TrainingAdminController.cs` ClosedXML gen `:1159/:1211` & parse `:861-873` + `Helpers/AssessmentScoreAggregator.cs` (pure EF-free shape) | exact (composite) |
| `Controllers/InjectAssessmentController.cs` | MODIFY | controller | request-response + file-upload | SAME file: `PreviewInjectScore:106`, `MapToRequest:163`, `ParseAnswerVms:327`, `userIdToNip:57`; upload tx pattern `TrainingAdminController.BulkBackfillAssessment:836` | exact (in-file) |
| `Views/Admin/InjectAssessment.cshtml` | MODIFY | view (Razor + vanilla JS) | event-driven (toggle/upload/preview) | SAME file: `#step5DefaultMode` toggle `:421-434`, `#step5Body:418`, serialize listener `#AnswersJson:985-996`; file-input markup ref `Views/Admin/BulkBackfill.cshtml` | exact (in-file) |
| `Models/InjectAssessmentDtos.cs` | MODIFY | model (DTO) | data-structure | SAME file: `InjectRowError:65`, `InjectPreviewResult:104`, `InjectAnswerSpec:28` | exact (in-file) |
| `ViewModels/InjectAssessmentViewModel.cs` | MODIFY | model (VM) | data-structure | SAME file: `InjectWorkerAnswersVM:75`, `InjectAnswerVM:62` | exact (in-file) |
| `HcPortal.Tests/InjectExcelHelperTests.cs` | NEW | test (unit, fast suite) | transform-assert | `HcPortal.Tests/BuildAutoGenAnswersTests.cs` (pure unit, no DB, in-memory builders) | exact |
| `HcPortal.Tests/InjectExcelImportTests.cs` | NEW | test (integration, `Category=Integration`) | CRUD-assert (real SQL) | `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs` (`InjectAssessmentFixture`, preview==commit) | exact |
| `tests/e2e/inject-excel-396.spec.ts` | NEW | test (Playwright e2e) | event-driven (UI runtime) | `tests/e2e/inject-assessment-395.spec.ts` (DB-write, snapshot/restore, anti silent-grade-0) | exact |
| `Controllers/TrainingAdminController.cs` (BulkBackfill) | REMOVE | controller (action removal) | — | none (D-10 hard-remove) | n/a |
| `Views/Admin/BulkBackfill.cshtml` | DELETE | view (removal) | — | none (D-10) | n/a |
| `Views/Admin/Index.cshtml` + `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | EDIT (remove link) | view (UI link removal) | — | none (D-11) | n/a |

---

## Pattern Assignments

### `Helpers/InjectExcelHelper.cs` (NEW — utility, EF-free static)

> **Tujuan:** `GenerateTemplate(questions, workerNipNames) → XLWorkbook` (2 sheet: matrix + legenda) + `ParseMatrix(stream, questions, allowedNips, nipToUserId) → (List<InjectWorkerAnswersVM> workers, List<InjectRowError> errors)`. Pure/EF-free → unit-testable round-trip tanpa DB (analog `AssessmentScoreAggregator`).

**Analog A — namespace + EF-free static shape:** `Helpers/AssessmentScoreAggregator.cs:1-29`
```csharp
using System.Collections.Generic;
using System.Linq;
using HcPortal.Models;

namespace HcPortal.Helpers
{
    // Pure by design (only System.Linq / HcPortal.Models), fully synchronous, EF-free,
    // no logging dependency → unit-testable without a database.
    public static class AssessmentScoreAggregator
    {
        public static ScoreAggregateResult Compute(...) { ... }
    }
}
```
→ Mirror: `public static class InjectExcelHelper` in `namespace HcPortal.Helpers`, methods static, hanya `System.*` + `ClosedXML.Excel` + `HcPortal.Models` + `HcPortal.ViewModels`. NO `ApplicationDbContext`.

**Analog B — ClosedXML template generation (header + bold + ToFileResult):** `Controllers/TrainingAdminController.cs:1159-1203`
```csharp
using var workbook = new XLWorkbook();
var ws = workbook.Worksheets.Add("Import Training");
var headers = new[] { "NIP", "Judul", ... };
for (int i = 0; i < headers.Length; i++)
{
    ws.Cell(1, i + 1).Value = headers[i];
    ws.Cell(1, i + 1).Style.Font.Bold = true;
    ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
    ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
}
// Example/data rows start row 2...
return ExcelExportHelper.ToFileResult(workbook, "training_import_template.xlsx", this);
```
> **Catatan helper-vs-controller:** template generator menghasilkan `XLWorkbook` saja (return ke caller); controller GET memanggil `ExcelExportHelper.ToFileResult(wb, "inject_template.xlsx", this)` (`ExcelExportHelper.cs:28`) untuk `FileContentResult` (adjust columns + mime `...spreadsheetml.sheet`). JANGAN inline `new MemoryStream()` + `File(...)`.

**Analog C — multi-sheet (Sheet-2 legenda) + freeze + adjust:** `Helpers/ExcelExportHelper.cs:50-112` (`AddDetailPerSoalSheet`)
```csharp
var ws = workbook.Worksheets.Add("Detail Per Soal");
var sortedQuestions = questions.OrderBy(q => q.Order).ThenBy(q => q.Id).ToList();   // STABLE sort
ws.Cell(1, 1).Value = "No"; ...
var headerRange = ws.Range(1, 1, 1, col);
headerRange.Style.Font.Bold = true;
headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
ws.Columns().AdjustToContents();
ws.SheetView.FreezeRows(1);
```
> **D-04 STABLE ORDER (HIGHEST RISK — Pitfall 1):** matrix Sheet-1 menulis kolom soal dengan `questions.OrderBy(q => q.Order).ThenBy(q => q.TempId)` dan parser membaca dengan comparator **IDENTIK**. (Catatan: analog di atas pakai `.ThenBy(q.Id)` karena POCO ber-DB-Id; di 396 soal ber-`TempId` pre-persist → gunakan `.ThenBy(q.TempId)`, identik dengan persist `InjectAssessmentService` & auto-gen.) Huruf opsi → `q.Options[i]` **urutan authored apa adanya** (A=`Options[0]`), JANGAN `OrderBy(TempId)`.

**Analog D — ClosedXML matrix parsing (stream, LastRowUsed, cell coercion):** `Controllers/TrainingAdminController.cs:861-873`
```csharp
using var stream = excel.OpenReadStream();
using var workbook = new XLWorkbook(stream);
var ws = workbook.Worksheets.First();          // fallback bila sheet di-rename (A2)
int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
for (int rowIdx = 2; rowIdx <= lastRow; rowIdx++)   // header row 1 skip
{
    var nipCell = ws.Cell(rowIdx, 1).GetString().Trim();
    if (string.IsNullOrEmpty(nipCell)) continue;     // baris kosong total → skip
    int score = 0;
    var scoreCell = ws.Cell(rowIdx, 3);
    if (scoreCell.TryGetValue<double>(out var d)) score = (int)d;     // numerik
    else if (int.TryParse(scoreCell.GetString().Trim(), out var i)) score = i;  // string fallback
    rows.Add((nipCell, score));
}
```
> **Reuse persis:** header row-1 skip, data row 2+, `GetString().Trim()` untuk NIP/huruf opsi, `TryGetValue<double>` lalu `int.TryParse` fallback untuk skor essay (Pitfall 6). Pembungkus `try/catch (Exception ex)` → pesan ramah, BUKAN 500 (Security V5/V12).

**Output DTO yang dihasilkan parser:** `List<InjectWorkerAnswersVM>` (`ViewModelMap` di bawah) — `Mode="manual"` selalu (D-07), `Answers = List<InjectAnswerVM>`. Bentuk SAMA dgn payload `#AnswersJson` form → preview & commit reuse jalur 395 tanpa cabang baru.

**Helper huruf↔index (RESEARCH §Code Examples — embed sebagai private):**
```csharp
private static int LetterToIndex(string letter) {
    var s = letter.Trim().ToUpperInvariant();
    if (s.Length != 1 || s[0] < 'A' || s[0] > 'Z') return -1;
    return s[0] - 'A';                                   // A→0
}
private static string IndexToLetter(int i) => ((char)('A' + i)).ToString();   // 0→A (legend Sheet-2)
```

**Anti-patterns (dari RESEARCH, WAJIB dihindari):**
- Sel kosong → `continue` (OMIT, D-06). JANGAN push `InjectAnswerVM{ SelectedOptionTempIds=[] }` (MC kosong → `PreflightValidate` reject-all SELURUH batch).
- `allowedNips` = NIP resolve dari `vm.UserIds` (picker Step-2) SAJA. NIP valid di AspNetUsers tapi tak di picker → error per-baris (D-02), BUKAN auto-add.
- JANGAN hitung skor di parser. Parser hanya translasi sel→spec.

---

### `Controllers/InjectAssessmentController.cs` (MODIFY — controller, +2 endpoint)

> **Tujuan:** GET `DownloadInjectTemplate` (return `.xlsx`) + POST `UploadInjectExcel` (parse→validate→preview JSON). Reuse `userIdToNip`, `PreviewInjectScore` engine, `MapToRequest`→`InjectBatchAsync` (commit lewat `#AnswersJson` yang sama).

**Analog — RBAC + antiforgery + JSON return (existing endpoint):** `InjectAssessmentController.cs:103-156` (`PreviewInjectScore`)
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public IActionResult PreviewInjectScore([FromBody] InjectPreviewRequest preq)
{
    ...
    var (qInMem, respInMem) = MapToInMemory(questions, answers);
    var agg = AssessmentScoreAggregator.Compute(qInMem, respInMem, preq.PassPercentage);
    return Json(result);   // Json → tak terkena override View() ~/Views/Admin/
}
```
> **Engine preview-batch (D-08):** loop `MapToInMemory(questions, workerAnswers)` (private `:283`) + `AssessmentScoreAggregator.Compute` per worker di endpoint upload → `previews[]`. `MapToInMemory` saat ini private — planner buat preview-batch internal di endpoint upload (1 round-trip), JANGAN N panggilan `PreviewInjectScore` (Open Q1: rekomendasi loop internal). preview == commit dijamin (engine sama).

**Analog — `userIdToNip` resolve (picker UserId → NIP):** `InjectAssessmentController.cs:55-59`
```csharp
var userIds = vm.UserIds ?? new List<string>();
var userIdToNip = await _context.Users
    .Where(u => userIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id, u => u.NIP ?? "");
```
> Untuk template: extend projection tambah `FullName` (Nama informational kolom 2) + `nipToUserId` invers untuk parser. `allowedNips` = nilai NIP non-kosong dari dict ini (D-02). `ApplicationUser.NIP` = `string?` nullable → skip user tanpa NIP.

**Analog — `IFormFile` upload + size limit + RBAC:** `TrainingAdminController.cs:832-846`
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin")]                                  // ← 396 ubah ke "Admin, HC"
[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
public async Task<IActionResult> BulkBackfillAssessment(IFormFile? excel, ...)
{
    if (excel == null || excel.Length == 0) { TempData["Error"] = "File Excel wajib diupload."; ... }
```
> **Salin attribut:** `[Authorize(Roles="Admin, HC")]` (bukan Admin-only — mirror existing inject `:41`/`:104`) + `[ValidateAntiForgeryToken]` + `[RequestFormLimits 10MB]`. Validasi ekstensi `.xlsx/.xls` whitelist (Security V12) + parse `try/catch` → daftar error, JANGAN throw (Security V5). NB: endpoint upload baru return `Json` (preview), BUKAN `RedirectToAction` (pola lama).

**Commit (NOL cabang baru):** klien menaruh `answersJson` (dari respons upload) ke `#AnswersJson` → submit form yang sama → `MapToRequest:163` (manual = `ResolveWorkerAnswers` copy spec apa adanya `:247`) → `InjectBatchAsync`. `PreflightValidateAsync` (393) memvalidasi MC==1/MA≥1/essay 0..ScoreValue/opsi valid + reject-all → Excel mewarisi gratis.

**Discretion fold-in:** komentar-only ref BulkBackfill (`AssessmentAdminController.cs:4105`, `AdminBaseController.cs:263`) = light-touch/biarkan. JANGAN hapus `ManualDuplicatePredicate` (dipakai AddManual/Import).

---

### `Views/Admin/InjectAssessment.cshtml` (MODIFY — Step-5 toggle N1 + panel N2 + tabel N3 + error N4)

> **Tujuan:** toggle metode jawaban room-level (N1, D-01/D-03) di dalam `#step5Body` di atas blok mode-default-room 395 + panel Excel (N2) + tabel preview batch (N3) + panel error (N4). Bahasa Indonesia. Render data via `.textContent` (XSS-safe, carry 395).

**Analog — toggle radio room-level (mirror VERBATIM untuk N1):** `InjectAssessment.cshtml:420-434` (`#step5DefaultMode`)
```html
<div class="mb-3">
    <label class="form-label fw-bold mb-1">Mode default room</label>
    <div>
        <div class="form-check form-check-inline">
            <input class="form-check-input" type="radio" name="step5DefaultMode" id="step5DefaultManual" value="manual" checked />
            <label class="form-check-label" for="step5DefaultManual">Input jawaban asli</label>
        </div>
        <div class="form-check form-check-inline">
            <input class="form-check-input" type="radio" name="step5DefaultMode" id="step5DefaultAuto" value="auto" />
            <label class="form-check-label" for="step5DefaultAuto">Auto-generate dari skor target</label>
        </div>
    </div>
    <div class="form-text text-muted">Mode awal tiap pekerja; dapat ditimpa per-pekerja di bawah.</div>
</div>
```
> N1 = `name="step5Method"`, radio `Isi via Form` (value=form, **checked**) / `Import Excel` (value=excel), label kelompok "Metode pengisian jawaban", hint D-03 "Pilih satu metode untuk seluruh room…". Tempatkan di `#step5Body` (`:418`) DI ATAS `#step5DefaultMode` (`:421`). Show/hide via `d-none` class (mutually exclusive IC-1): excel → sembunyikan SELURUH blok Form 395, tampilkan `#step5ExcelPanel`.

**Analog — serialize hidden-JSON di submit listener (KRITIS, isi `#AnswersJson` dari Excel):** `InjectAssessment.cshtml:985-996`
```javascript
var injForm = document.getElementById('injectAssessmentForm');
if (injForm) {
    injForm.addEventListener('submit', function () {
        var hidden = document.getElementById('QuestionsJson');
        if (hidden) hidden.value = JSON.stringify(injQuestions);
        // Phase 395 (Pitfall 4 — KRITIS): serialize answers di listener yang SAMA dgn #QuestionsJson.
        var answersHidden = document.getElementById('AnswersJson');
        if (answersHidden) answersHidden.value = JSON.stringify(buildWorkerAnswersPayload());
    });
}
```
> **Path Excel:** saat mode=excel, `#AnswersJson` diisi dari hasil parse upload (server kembalikan `answersJson`), BUKAN `buildWorkerAnswersPayload()` (form). Planner: cabang di listener ini — mode form → payload form; mode excel → payload Excel-cache. `#QuestionsJson` tetap di-serialize seperti biasa (template-gen + parse pakai soal yang sama).

**Analog — file-input markup (referensi fungsional saja, BUKAN styling lama):** `Views/Admin/BulkBackfill.cshtml` (akan DIHAPUS — copy markup `<input type="file">` ke N2c sebelum delete)
> N2c: `<input type="file" class="form-control" id="step5ExcelFile" accept=".xlsx,.xls">` + `<label for="step5ExcelFile" class="form-label fw-bold">File Excel</label>` + hint `.form-text.text-muted` "Format .xlsx/.xls, maks 10 MB."

**Token UI (dari 396-UI-SPEC, INHERIT 394/395):** Bootstrap 5.3 + bi icons; download/upload/preview = `.btn.btn-outline-primary`; commit tetap `#btnInject` (success-green, **jangan ubah**); panel error = `.alert.alert-danger` (`role="alert"`); tabel preview `role="status" aria-live="polite"`; warn sel-kosong = `.alert.alert-warning`; instruksi = `.alert.alert-info`. Overflow: tabel `max-height:320px;overflow-y:auto` (pola roster `:443`/picker `:246`); error `<ul>` `max-height:240px` bila >8 item.

**Verifikasi runtime WAJIB (Pitfall 4, carry 354/392):** `AddControllersWithViews()` TANPA RuntimeCompilation → view embedded saat build. Toggle show/hide + upload + fetch + render tabel/error WAJIB Playwright dari **main tree** (bukan sibling `PortalHC_KPB-ITHandoff`), AD-off, `--workers=1`.

---

### `Models/InjectAssessmentDtos.cs` (MODIFY — DTO error/preview)

**Analog — error per-baris (reuse apa adanya untuk D-09):** `InjectAssessmentDtos.cs:64-69`
```csharp
/// <summary>Satu error per-baris (NIP) untuk D-03 reject-all.</summary>
public class InjectRowError
{
    public string Nip { get; set; } = "";
    public string Message { get; set; } = "";   // Bahasa Indonesia
}
```
> **`InjectRowError` sudah cukup** untuk daftar error D-09 (parser emit `List<InjectRowError>` dengan pesan per-baris/sel: "Baris 3: NIP… tidak ada di picker", "Baris 5, kolom Soal 2: opsi 'E' tidak valid"). Jika perlu DTO upload-result wrapper baru (mis. `InjectExcelUploadResult { bool Ok; List<InjectRowError> Errors; string AnswersJson; List<...> Previews; }`), buat di file ini mengikuti pola DTO record/class existing.

**Analog — preview result (reuse engine output):** `InjectAssessmentDtos.cs:104-115` (`InjectPreviewResult`) — sudah ada `Percentage/IsPassed/TotalScore/MaxScore`. Per-NIP preview row D-08 (NIP+Nama+Skor%+Lulus+terjawab) bisa anonymous-object di `Json` upload, ATAU DTO baru `InjectExcelPreviewRow` di file ini bila planner mau strongly-typed.

**Konvensi file:** XML-doc `<summary>` Bahasa Indonesia per-class, namespace `HcPortal.Models`, plain POCO (no attribute). Mirror `InjectRowError`/`InjectPreviewResult`.

---

### `ViewModels/InjectAssessmentViewModel.cs` (MODIFY — VM, opsional flag)

**Analog — per-worker answers VM (parser OUTPUT bentuk ini):** `InjectAssessmentViewModel.cs:62-81`
```csharp
public class InjectAnswerVM
{
    public int QuestionTempId { get; set; }
    public List<int> SelectedOptionTempIds { get; set; } = new();   // MC: 1; MA: ≥1; Essay: kosong
    public string? TextAnswer { get; set; }   // Essay
    public int? EssayScore { get; set; }       // Essay
}
public class InjectWorkerAnswersVM
{
    public string UserId { get; set; } = "";
    public string Mode { get; set; } = "manual";   // "manual" | "auto"  ← Excel SELALU "manual" (D-07)
    public int TargetScore { get; set; }
    public List<InjectAnswerVM> Answers { get; set; } = new();
}
```
> **Reuse persis** — parser `InjectExcelHelper.ParseMatrix` menghasilkan `List<InjectWorkerAnswersVM>` ini (Mode="manual", TargetScore unused). Tidak perlu VM baru untuk answers. Opsional: tambah flag `public string? Step5Method { get; set; }` ("form"|"excel") bila planner mau bind metode toggle ke server (umumnya tak perlu — metode hanya memengaruhi isi `#AnswersJson` klien).
> XML-doc Bahasa Indonesia, nested class di `InjectAssessmentViewModel`, namespace `HcPortal.ViewModels`.

---

### `HcPortal.Tests/InjectExcelHelperTests.cs` (NEW — unit, fast suite)

**Analog — pure unit, no DB, in-memory builders:** `HcPortal.Tests/BuildAutoGenAnswersTests.cs:1-65`
```csharp
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

// TANPA DB, TANPA fixture, TANPA [Trait("Category","Integration")] → masuk fast suite.
public class BuildAutoGenAnswersTests
{
    private static InjectQuestionSpec Q(int tempId, string type, int sv, int order,
        params (int optTemp, bool correct)[] opts) => new InjectQuestionSpec { ... };
    private static InjectQuestionSpec Mc(int tempId, int sv = 10, int? order = null) => ...;
    private static InjectQuestionSpec Ma(int tempId, int sv = 10, int? order = null) => ...;
    private static InjectQuestionSpec Essay(int tempId, int sv, int? order = null) => ...;
}
```
> **Mirror:** NO `[Trait("Category","Integration")]` → fast suite (`dotnet test --filter Category!=Integration`). Reuse builder `Q/Mc/Ma/Essay`. Test round-trip: `GenerateTemplate(questions, nips)` → mutate cells in-memory (ClosedXML `XLWorkbook` → MemoryStream) → `ParseMatrix(stream, questions, allowedNips)` → assert TempId mapping identik. Cover (RESEARCH Test Map): huruf→opsi (A=Options[0]), round-trip stable order, blank=skip (OMIT), MA `A,C`→2 TempId, essay skor+teks-opsional, NIP-not-in-picker→error, huruf-invalid `E`→error per-sel.

---

### `HcPortal.Tests/InjectExcelImportTests.cs` (NEW — integration, real SQL)

**Analog — `InjectAssessmentFixture` + preview==commit:** `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs:24-46,104-156`
```csharp
[Trait("Category", "Integration")]
public class InjectPreviewEqualsCommitTests : IClassFixture<InjectAssessmentFixture>
{
    private readonly InjectAssessmentFixture _fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);
    private InjectAssessmentService NewInjectService(ApplicationDbContext ctx)
        => new InjectAssessmentService(ctx, NewGradingService(ctx), NullLogger<InjectAssessmentService>.Instance);
    // SeedUserAsync(ctx, nip) — inject resolve by NIP, WAJIB set NIP.
}
```
> **Mirror:** `[Trait("Category","Integration")]` → skip fast suite; disposable DB `HcPortalDB_Test_{guid}` via `InjectAssessmentFixture`. Key facts (RESEARCH): (1) Excel-parsed `List<InjectWorkerAnswersVM>` → `MapToRequest` → `InjectBatchAsync` score IDENTIK dengan form-path workers (preview==commit); (2) atomic ≥1 error → 0 sesi ter-commit; (3) essay skor > ScoreValue → reject (delegasi `PreflightValidate`). Boleh extend pola `InjectPreviewEqualsCommitTests` ATAU file baru `InjectExcelImportTests`. Real `GradingService` via `NewGradingService` (salin verbatim `:35-43`).

---

### `tests/e2e/inject-excel-396.spec.ts` (NEW — Playwright)

**Analog — DB-write e2e + snapshot/restore + anti silent-grade-0:** `tests/e2e/inject-assessment-395.spec.ts:1-67`
```typescript
import { test, expect, type Page } from '@playwright/test';
import { accounts } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';
test.describe.configure({ mode: 'serial' });

async function loginAdmin(page: Page) { /* /Account/Login admin@pertamina.com */ }
async function fillToStep5(page, title, workerCount) {
  // Setup → pilih pekerja ber-NIP (data-email) → authoring soal MC → cert → Langkah 5
  await expect(page.locator('#step5Body')).toBeVisible();
}
test.beforeAll(async () => { /* BACKUP DB (InstanceDefaultBackupPath) */ });
```
> **Mirror:** `helpers/accounts` (admin@pertamina.com), `helpers/dbSnapshot` (BACKUP `beforeAll`/RESTORE `afterAll`, CLAUDE.md Seed Workflow + SEED_JOURNAL.md), `mode: 'serial'`, `--workers=1`. Reuse `loginAdmin`/`authorMcQuestion`/`fillToStep5`. WORKER_EMAILS ber-NIP (`rino.prasetyo@pertamina.com`). Scenarios (RESEARCH Test Map): toggle Form↔Excel → Download Template (download event) → fill cells → upload → preview tabel N3 → `#btnInject` commit → `/CMP/Results` skor benar (anti silent-grade-0); invalid → daftar error N4 LENGKAP + rollback (0 write); `/Admin/BulkBackfill` → 404 + kartu Index + dropdown-item hilang (INJ-11). Jalankan dari **main tree** (Pitfall 4).

---

### REMOVALS (D-10/D-11 — no analog, hard-remove)

**`Controllers/TrainingAdminController.cs` — 2 blok NON-KONTIGU (verified live):**
| Hapus | Lines (HEAD `2345083d`) | Keterangan |
|-------|------|-----------|
| Comment block + GET `BulkBackfill` | `776-790` | header comment `776-782` + GET action `784-790` |
| **KEEP `CleanupAttemptHistory` GET/POST** | `792-829` | ⚠️ JANGAN hapus — di antara GET & POST BulkBackfill |
| POST `BulkBackfillAssessment` (+comment) | `831-985` | comment `831` + method `832-985` |
- **KEEP** `using ClosedXML.Excel;` (`:8`) — masih dipakai `:1159/:1211/:1298` (template+import training).
- **KEEP** `AdminBaseController.ManualDuplicatePredicate` (`:263`) — dipakai AddManual/Import.
- `nameof(BulkBackfill)` hanya dipakai DI DALAM method yang dihapus (`:849-983`) → hilang bersamaan, no dangling ref.

**`Views/Admin/BulkBackfill.cshtml`** — DELETE seluruh file (119 baris). Copy `<input type="file">` markup ke N2c sebelum delete.

**`Views/Admin/Index.cshtml:306-321`** — hapus seluruh blok `@if (User.IsInRole("Admin")) { <div col-md-4> … BulkBackfill … </div> }` (card adalah satu-satunya isi blok). Verified live:
```html
@if (User.IsInRole("Admin"))
{
<div class="col-md-4">
    <a href="@Url.Action("BulkBackfill", "TrainingAdmin")" ...>
        ... Bulk Import Nilai (Excel) ...
    </a>
</div>
}
```

**`Views/Admin/Shared/_AssessmentGroupsTab.cshtml:317-322`** — hapus divider yatim + li link. Verified live:
```html
<li><hr class="dropdown-divider"></li>                                    <!-- :317 hapus -->
<li>
    <a class="dropdown-item" href="@Url.Action("BulkBackfill", "TrainingAdmin")">  <!-- :318-322 hapus -->
        <i class="bi bi-arrow-counterclockwise me-2"></i>Bulk Import Nilai (Excel)
    </a>
</li>
```
> Hapus divider `:317` JUGA agar tak ada `<hr>` yatim. Item Bulk Export PDF (`:312-315`) di atasnya TETAP.

**`HcPortal.Tests/DuplicateGuardTests.cs`** — TIDAK panggil action `BulkBackfillAssessment` (`#14` pakai HashSet replica + `ManualDuplicatePredicate`, `:118-163`) → hard-remove **tak break compile**. Komentar "BulkBackfill" (`:118`,`:138`,`:153`) = kosmetik; planner boleh light-touch rename (opsional, non-blocking).

---

## Shared Patterns

### Authentication / RBAC
**Source:** `Controllers/InjectAssessmentController.cs:41,104` + `TrainingAdminController.cs:834`
**Apply to:** semua endpoint baru (GET DownloadInjectTemplate + POST UploadInjectExcel)
```csharp
[Authorize(Roles = "Admin, HC")]   // mirror existing inject; BulkBackfill lama Admin-only → diganti Admin+HC
[ValidateAntiForgeryToken]          // POST upload/preview
[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]   // upload 10MB (Security V12)
```

### Error handling (file/parse → daftar, bukan 500)
**Source:** `TrainingAdminController.cs:876-880` + DTO `InjectAssessmentDtos.cs:64-69`
**Apply to:** `InjectExcelHelper.ParseMatrix` + POST UploadInjectExcel
```csharp
try { using var wb = new XLWorkbook(stream); ... }
catch (Exception ex) { /* Json error "Gagal membaca file Excel…" — NOT throw/500 (Security V5/V12) */ }
// Validasi sel → kumpul List<InjectRowError> (D-09 daftar LENGKAP), atomic: errors≥1 → NO write.
```

### Stable ordering (kill silent corruption)
**Source:** `Helpers/ExcelExportHelper.cs:58` + `InjectAssessmentController.cs:287-293` (`MapToInMemory`)
**Apply to:** template-gen + parser (SATU comparator)
```csharp
questions.OrderBy(q => q.Order).ThenBy(q => q.TempId)   // IDENTIK gen↔parse (D-04)
// huruf opsi: q.Options[LetterToIndex(L)] — urutan AUTHORED (A=Options[0]); JANGAN OrderBy(TempId).
```

### Excel download FileResult
**Source:** `Helpers/ExcelExportHelper.cs:28`
**Apply to:** GET DownloadInjectTemplate
```csharp
return ExcelExportHelper.ToFileResult(workbook, "inject_template.xlsx", this);   // adjust cols + mime + filename
```

### Preview engine (preview == commit, D-08)
**Source:** `Helpers/AssessmentScoreAggregator.cs:26` via `InjectAssessmentController.MapToInMemory:283`
**Apply to:** POST UploadInjectExcel preview rows
```csharp
var (qInMem, respInMem) = MapToInMemory(questions, workerAnswers);   // TempId=Id sintetis, EF-free
var agg = AssessmentScoreAggregator.Compute(qInMem, respInMem, passPct);   // engine IDENTIK commit; NO cert#
```

### Commit jalur sama (nol cabang baru)
**Source:** `InjectAssessmentController.cs:163` (`MapToRequest`) → `InjectBatchAsync`
**Apply to:** path Excel commit
```
parser → List<InjectWorkerAnswersVM> → #AnswersJson (klien) → submit → MapToRequest → InjectBatchAsync (393)
```

---

## No Analog Found

| File/Aspek | Role | Data Flow | Reason |
|------------|------|-----------|--------|
| (none) | — | — | Semua file baru memetakan ke analog konkret. Removals tak butuh analog. Tidak ada library/pattern baru — 396 = translasi tipis + hard-remove di atas fondasi 393/395. |

---

## Metadata

**Analog search scope:** `Helpers/`, `Controllers/`, `Views/Admin/` (+ `Shared/`), `Models/`, `ViewModels/`, `HcPortal.Tests/`, `tests/e2e/`.
**Files scanned (read):** `ExcelExportHelper.cs`, `AssessmentScoreAggregator.cs`, `InjectAssessmentController.cs`, `InjectAssessmentDtos.cs`, `InjectAssessmentViewModel.cs`, `TrainingAdminController.cs` (`:770-1009`, `:1150-1269`), `Index.cshtml` (`:300-322`), `_AssessmentGroupsTab.cshtml` (`:310-327`), `InjectAssessment.cshtml` (`:398-437`, `:975-1004`), `BuildAutoGenAnswersTests.cs`, `InjectPreviewEqualsCommitTests.cs`, `DuplicateGuardTests.cs` (`:100-164`), `inject-assessment-395.spec.ts`.
**Line-number verification:** HEAD `2345083d` (post-395). Removal ranges (`776-790`+`831-985`, Index `306-321`, dropdown `317-322`) re-verified live — match RESEARCH.
**Pattern extraction date:** 2026-06-18

---

## PATTERN MAPPING COMPLETE
