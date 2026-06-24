# Phase 415: Section Foundation + Import Excel Diperluas - Pattern Map

**Mapped:** 2026-06-22
**Files analyzed:** 9 (3 new, 6 modified) + 4 new test files
**Analogs found:** 13 / 13 (all in-codebase; zero "no analog")

> All line refs below were **re-grepped against live code this session** (research warned of ±20 drift). Symbols are stable; line numbers verified accurate. Stack = ASP.NET Core 8 MVC + EF Core 8 (SQL Server) + ClosedXML 0.105 + Bootstrap 5 Razor SSR. **Zero new dependencies.**

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Models/AssessmentPackage.cs` (ADD `AssessmentPackageSection` class + `PackageQuestion.SectionId int?`) | model | transform | same file: `PackageQuestion`/`PackageOption` (entity + nullable FK precedent `ImagePath?`) | exact |
| `Data/ApplicationDbContext.cs` (ADD DbSet + Fluent config + unique index + FK SetNull) | config | transform | same file: `PackageQuestion`/`PackageOption` Fluent block `:475-496` + unique-index `:513-514` | exact |
| `Migrations/<ts>_AddAssessmentPackageSection.cs` (NEW) | migration | transform | `Migrations/20260606030844_AddImageToPackageQuestionAndOption.cs` (cols) + need CreateTable+FK+index (no exact in-repo CreateTable precedent — see note) | role-match |
| `Controllers/AssessmentAdminController.cs` — Section CRUD endpoints (NEW) | controller | CRUD | `UpdateShuffleSettings` `:6188` (RBAC+antiforgery+sibling+audit+PRG) | exact |
| `Controllers/AssessmentAdminController.cs` — `ImportPackageQuestions` dual-format parser (MODIFY) | controller | file-I/O (batch) | self `:6690` (9-col parser body `:6755-6772`) | exact (extend) |
| `Controllers/AssessmentAdminController.cs` — `DownloadQuestionTemplate` (MODIFY) | controller | file-I/O | self `:6599` (ClosedXML template gen) | exact (extend) |
| `Controllers/AssessmentAdminController.cs` — `MakePackageFingerprint` + `ExtractPackageCorrectLetter` (MODIFY) | utility | transform | self `:7081` / `:7060` | exact (extend) |
| `Controllers/AssessmentAdminController.cs` — per-Section count validation (MODIFY) | controller | transform | self cross-package count check `:6823-6863` | exact (extend) |
| `Controllers/AssessmentAdminController.cs` — `SyncPackagesToPost` clone Section (MODIFY) | service | transform (deep-clone) | self `:6366` deep-clone body `:6397-6418` | exact (extend) |
| `Controllers/AssessmentAdminController.cs` — `CreateQuestion`/`EditQuestion` add `sectionId` + `ManagePackageQuestions` GET grouping (MODIFY) | controller | request-response | self `CreateQuestion` `:7116`, `ManagePackageQuestions` `:7090` | exact (extend) |
| `Controllers/CMPController.cs` — D-13 re-guard at StartExam (MODIFY) | controller | request-response | self seam before `BuildQuestionAssignment` `:1074` | exact (insert) |
| `Views/Admin/ManagePackageQuestions.cshtml` — inline Section panel + grouped list + dropdown (MODIFY) | component | request-response | self `row g-4`/`col-lg-7` layout `:37-44` + toggle precedent `ManagePackages.cshtml:107-116` | exact |
| `Views/Admin/ImportPackageQuestions.cshtml` — format card + error/result alerts (MODIFY) | component | request-response | self + TempData alert pattern `ManagePackageQuestions.cshtml:28-35` | role-match |
| `HcPortal.Tests/Section*.cs` (NEW ×4 + fixture) | test | transform | `FlexibleParticipantAddTests.cs:20-53` (fixture) + `InjectExcelImportTests.cs` (Integration import) | exact |

## Pattern Assignments

### `Models/AssessmentPackage.cs` (model, transform) — ADD entity + nullable FK

**Analog:** same file — `PackageQuestion` (entity shape) + `PackageOption` + the nullable-FK precedent on `ImagePath?`.

**Entity shape pattern** (`Models/AssessmentPackage.cs:6-25` `AssessmentPackage`; `:70-94` `PackageOption`):
- `[Key] public int Id` + `public int <Parent>Id` + `[ForeignKey("<Parent>Id")] public virtual <Parent> <Parent> { get; set; } = null!;` + `public virtual ICollection<Child> ...= new List<>()`.
- Nullable string with default: `public string? QuestionType { get; set; }` (no default) and `public int MaxCharacters { get; set; } = 2000;` (C# default for `bool`/`int` columns).
- **CRITICAL — grading is `PackageOption.Id`-based, NOT letter-based** (`:80-86`: "No Letter field — letters are display-only … Grading uses PackageOption.Id exclusively"). This is *why* storing a correct E/F option in 415 is safe (O-1): render/grading of the letter is 418, but `IsCorrect` data is stored now via Id.

**New `AssessmentPackageSection`** (per CONTEXT §38 + RESEARCH Pattern 1): `Id`, `AssessmentPackageId` (FK + nav, mirror `PackageQuestion.AssessmentPackageId:32-34`), `SectionNumber int`, `Name string?` (nullable), `StartNewPage bool = false`, `ShuffleEnabled bool = true`, `ICollection<PackageQuestion> Questions`.

**Add to `PackageQuestion`** (after `:64`, mirror the `ImagePath?` nullable add): `public int? SectionId { get; set; }` + `[ForeignKey("SectionId")] public virtual AssessmentPackageSection? Section { get; set; }`.

---

### `Data/ApplicationDbContext.cs` (config, transform) — DbSet + Fluent + unique index

**Analog:** same file — package-family Fluent block + the `UserPackageAssignment` unique composite index.

**DbSet declaration** (mirror `:55-57`):
```csharp
public DbSet<AssessmentPackage> AssessmentPackages { get; set; }
public DbSet<PackageQuestion> PackageQuestions { get; set; }
public DbSet<PackageOption> PackageOptions { get; set; }
// ADD: public DbSet<AssessmentPackageSection> AssessmentPackageSections { get; set; }
```

**Fluent FK + cascade pattern** (`:475-496` — `PackageQuestion`→`AssessmentPackage` Cascade is THE precedent):
```csharp
// PackageQuestion -> AssessmentPackage (Cascade)
builder.Entity<PackageQuestion>(entity =>
{
    entity.HasOne(q => q.AssessmentPackage)
        .WithMany(p => p.Questions)
        .HasForeignKey(q => q.AssessmentPackageId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasIndex(q => q.AssessmentPackageId);
    entity.HasIndex(q => q.Order);
});
```

**Unique composite index pattern** (`:513-514` — copy EXACTLY for `(AssessmentPackageId, SectionNumber)`):
```csharp
entity.HasIndex(a => new { a.AssessmentSessionId, a.UserId }).IsUnique();
```

**NEW config to add** (matching the block style):
```csharp
builder.Entity<AssessmentPackageSection>(entity =>
{
    entity.HasOne<AssessmentPackage>()                 // or nav if added
        .WithMany()
        .HasForeignKey(s => s.AssessmentPackageId)
        .OnDelete(DeleteBehavior.Cascade);             // section dies with package
    entity.HasIndex(s => new { s.AssessmentPackageId, s.SectionNumber }).IsUnique();
});
// PackageQuestion.SectionId FK MUST be SetNull (NOT Cascade) — UI-SPEC delete promise + avoids
// multiple-cascade-path on PackageQuestions (Pitfall 2). Add to the EXISTING PackageQuestion block:
builder.Entity<PackageQuestion>(entity =>
{
    entity.HasOne(q => q.Section)
        .WithMany(s => s.Questions)
        .HasForeignKey(q => q.SectionId)
        .OnDelete(DeleteBehavior.SetNull);
    entity.HasIndex(q => q.SectionId);
});
```
> Note: existing index `IsUnique()` examples that are FILTERED (e.g. `:229-230` NomorSertifikat, `:434` CoacheeId active) → DbUpdateException on violation (Phase 404 lesson). The Section index `(AssessmentPackageId, SectionNumber)` is a plain non-filtered unique → also surfaces as `DbUpdateException`; pre-check with `AnyAsync` in the CreateSection endpoint (see Section CRUD pattern).

---

### `Migrations/<ts>_AddAssessmentPackageSection.cs` (migration, transform) — NEW

**Analog (columns):** `Migrations/20260606030844_AddImageToPackageQuestionAndOption.cs` (full file) — the `AddColumn<int>` / `DropColumn` symmetric Up/Down shape for the new `PackageQuestion.SectionId` nullable column.

**Column add + symmetric drop** (verbatim shape, `AddImageToPackageQuestionAndOption.cs:13-24` / `:43-49`):
```csharp
migrationBuilder.AddColumn<string>(name: "ImageAlt", table: "PackageQuestions",
    type: "nvarchar(255)", maxLength: 255, nullable: true);
// Down:
migrationBuilder.DropColumn(name: "ImageAlt", table: "PackageQuestions");
```
For 415: `migrationBuilder.AddColumn<int>(name:"SectionId", table:"PackageQuestions", nullable:true);`

**Most recent migration (chain baseline + Up/Down idiom):** `Migrations/20260621011101_AddParticipantRemovalColumns.cs` (Phase 409). Same `#nullable disable`, `partial class : Migration`, `Up`/`Down` structure.

> **No in-repo CreateTable+FK+index precedent** is co-located in these two simple column-add migrations. **Scaffold via `dotnet ef migrations add AddAssessmentPackageSection` from repo root** (local tool 8.0.0 pin, `.config/dotnet-tools.json`) — let EF generate the `CreateTable`/`CreateIndex`/`AddForeignKey` calls from the DbContext config above. RESEARCH Pattern 1 (lines 175-203) gives the expected generated shape. **Verify snapshot ProductVersion stays `8.0.0`** (Pitfall 1). FK behaviors: Section→Package `Cascade`, Question→Section `SetNull` (Pitfall 2).

---

### `AssessmentAdminController.cs` — Section CRUD endpoints (controller, CRUD) — NEW

**Analog:** `UpdateShuffleSettings` (`:6188-6244`) — the canonical RBAC + antiforgery + sibling-resolution + audit + PRG-redirect admin-write endpoint.

**Endpoint signature + RBAC + antiforgery** (`:6185-6188`):
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateShuffleSettings(int assessmentId, bool shuffleQuestions, bool shuffleOptions)
```

**Audit log try/catch warn-only pattern** (`:6228-6240` — copy for Section CRUD audit):
```csharp
try
{
    var hcUser = await _userManager.GetUserAsync(User);
    var actorNameStr = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
    await _auditLog.LogAsync(hcUser?.Id ?? "", actorNameStr, "UpdateShuffleSettings",
        $"...desc...", assessmentId, "AssessmentSession");
}
catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed ..."); }
```

**PRG redirect + TempData feedback** (`:6242-6243`):
```csharp
TempData["Success"] = "Pengaturan pengacakan berhasil disimpan.";
return RedirectToAction("ManagePackages", new { assessmentId });
```
> For Section CRUD, redirect to `ManagePackageQuestions` (where the inline panel lives, D-415-01). The full Section-CRUD endpoint skeleton (CreateSection with `AnyAsync` dup pre-check + TempData error) is in RESEARCH "Code Examples" (lines 346-368) — copy verbatim; it already matches this controller's conventions. Needed: `CreateSection`, `EditSection`, `DeleteSection` (sets question `SectionId=null` via FK SetNull — no manual loop), `SetAllSectionsNewPage` (quick button → bulk set `StartNewPage=true`).

---

### `AssessmentAdminController.cs` — `ImportPackageQuestions` dual-format parser (controller, file-I/O batch) — MODIFY

**Analog:** itself, `:6690` (POST overload). Position-based ClosedXML parser at `:6755-6772`.

**Current 9-col position parser to extend** (`:6755-6772`):
```csharp
var ws = workbook.Worksheets.First();
int rowNum = 1;
foreach (var row in ws.RowsUsed().Skip(1))
{
    rowNum++;
    var q   = (row.Cell(1).GetString() ?? "").Trim();
    var a   = (row.Cell(2).GetString() ?? "").Trim();
    var b   = (row.Cell(3).GetString() ?? "").Trim();
    var c   = (row.Cell(4).GetString() ?? "").Trim();
    var d   = (row.Cell(5).GetString() ?? "").Trim();
    var cor = (row.Cell(6).GetString() ?? "").Trim().ToUpper();
    var cell7 = (row.Cell(7).GetString() ?? "").Trim();          // ElemenTeknis
    var questionType = NormalizeQuestionType(row.Cell(8).GetString() ?? "");
    var rubrik = row.Cell(9).GetString()?.Trim();
    rows.Add((q, a, b, c, d, cor, subComp, questionType, rubrik));
}
```

**Extension (D-415-03, RESEARCH Pattern 2 + Pitfall 3/4):**
- Detect format from **HEADER row (row 1)**, not data rows: `int colCount = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0; bool isNewFormat = colCount > 9;` (Pitfall 4: data rows have blank trailing cells → wrong count).
- **Boundary = `>9`** (CONTEXT D-415-03 is authority; ignore spec's stray "< 11" — Pitfall 3).
- New column order (spec §9.1): `Q(1) | A-F(2-7) | Correct(8) | No.Section(9) | NamaSection(10) | ET(11) | Type(12) | Rubrik(13)`. Old order unchanged (`Q | A-D(2-5) | Correct(6) | ET(7) | Type(8) | Rubrik(9)`).
- Widen the row tuple to carry `OptE, OptF, int? SectionNumber, string? SectionName`. Legacy rows → `E="",F="",SectionNumber=null`.
- The `pasteText` branch (`:6781-6815`) stays A–D 9-col only (no new-format paste required for 415).
- **Widen answer whitelist A→F at parse** (Pitfall 5 / O-1 recommendation): the hard-coded `{"A","B","C","D"}` at `:6851`, `:6897`, `:6913` must accept E/F so new-format MA rows like `"A,C,E"` import. Store correct E/F option (`IsCorrect` true) — safe (Id-based grading). Letter render/grading display = 418.
- **Atomic persist precedent** (`:6962-6977`): single transaction `BeginTransactionAsync` → `SaveChangesAsync` → `CommitAsync` / rollback-on-catch. Keep zero-write-on-error (D-415-04 / Inject D-09).

---

### `AssessmentAdminController.cs` — per-Section count validation (controller, transform) — MODIFY

**Analog:** itself — cross-package total-count check `:6823-6863`.

**Current sibling resolution + single total-count check** (`:6827-6861`):
```csharp
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == targetSession.Title &&
                s.Category == targetSession.Category &&
                s.Schedule.Date == targetSession.Schedule.Date)
    .Select(s => s.Id).ToListAsync();
var siblingPackagesWithQuestions = await _context.AssessmentPackages
    .Include(p => p.Questions)
    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId) && p.Id != packageId && p.Questions.Any())
    .ToListAsync();
// ... validRowCount vs referencePackage.Questions.Count ...
if (validRowCount != expectedCount) {
    TempData["Error"] = $"Jumlah soal tidak sama dengan paket lain. ...";
    return RedirectToAction("ImportPackageQuestions", new { packageId });
}
```

**Extension (IMP-03 / D-13, RESEARCH Pattern 3):**
- Reuse sibling resolution **verbatim** (key `Title+Category+Schedule.Date` already correct).
- Group BOTH incoming rows AND sibling `p.Questions` by `SectionNumber` (null → implicit "Lainnya" group, spec §15.A); compare counts per `SectionNumber`.
- **Build the COMPLETE mismatch list — never stop-at-first** (D-415-04 anti-pattern). Each entry uses UI-SPEC copy (`415-UI-SPEC.md:158`): `Section {sn}: Paket "{A}" punya {x} soal, Paket "{B}" punya {y} soal (harus sama).`
- Return full list via TempData (`List<string>`) consumed by `ImportPackageQuestions.cshtml` alert block (O-3 → keep redirect+TempData pattern; current view is form POST not AJAX).
- Legacy guard (Pitfall 6): all-null SectionNumber both sides → behaves like old total-count check (pass). Single-package / no sibling → skip.

---

### `AssessmentAdminController.cs` — `MakePackageFingerprint` + `ExtractPackageCorrectLetter` (utility, transform) — MODIFY

**Analog:** itself — `:7060-7082`.

**Current helpers** (`:7060-7082`):
```csharp
private static string ExtractPackageCorrectLetter(string raw) {
    if (string.IsNullOrWhiteSpace(raw)) return raw;
    if (raw.Length == 1) return raw;
    if ("ABCD".Contains(raw[0]) && !char.IsLetterOrDigit(raw[1])) return raw[0].ToString();
    if (raw.StartsWith("OPTION ") && raw.Length > 7 && "ABCD".Contains(raw[7])) return raw[7].ToString();
    return raw;
}
private static string NormalizePackageText(string s)
    => Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();
private static string MakePackageFingerprint(string q, string a, string b, string c, string d)
    => string.Join("|||", new[] { q, a, b, c, d }.Select(NormalizePackageText));
```

**Extension (IMP-03 / §15.C, RESEARCH lines 372-381):**
- New signature: `MakePackageFingerprint(string q, string a..f, int? sectionNumber)` → join `[q,a,b,c,d,e,f].Select(NormalizePackageText)` then `.Append(sectionNumber?.ToString() ?? "_NOSEC_")`.
- **Both call sites must use the new fn** so they stay comparable: existing-fingerprint set `:6718-6727` and new-row fingerprint `:6922`. Legacy: `e=""`, `f=""`, `sectionNumber=null`.
- `ExtractPackageCorrectLetter` `'ABCD'` literal: widen to `'ABCDEF'` at the IMPORT parse path only for 415 answer-acceptance (Pitfall 5). Display/grading of E/F letters = Phase 418 (do NOT change render code).

---

### `AssessmentAdminController.cs` — `SyncPackagesToPost` Section deep-clone (service, transform) — MODIFY

**Analog:** itself — `:6366-6424` (single point; all 6 callers route through it: `:6439,:6480,:6565,:7274,:7537,:7630`).

**Current deep-clone body** (`:6389-6421`):
```csharp
foreach (var prePkg in prePkgs)
{
    var newPkg = new AssessmentPackage { AssessmentSessionId = postSessionId,
        PackageName = prePkg.PackageName, PackageNumber = prePkg.PackageNumber };
    foreach (var q in prePkg.Questions)
    {
        var newQ = new PackageQuestion {
            QuestionText = q.QuestionText, Order = q.Order, ScoreValue = q.ScoreValue,
            QuestionType = q.QuestionType, ElemenTeknis = q.ElemenTeknis, Rubrik = q.Rubrik,
            MaxCharacters = q.MaxCharacters, ImagePath = q.ImagePath, ImageAlt = q.ImageAlt,
            Options = q.Options.Select(o => new PackageOption {
                OptionText = o.OptionText, IsCorrect = o.IsCorrect,
                ImagePath = o.ImagePath, ImageAlt = o.ImageAlt }).ToList()
        };
        newPkg.Questions.Add(newQ);
    }
    _context.AssessmentPackages.Add(newPkg);
}
await _context.SaveChangesAsync();
```

**Extension (SEC-06, Pitfall 7/8 + Assumption A2):**
- Also load Pre `AssessmentPackageSection` rows. For each `prePkg`, clone its Section rows into `newPkg` (new entities, `StartNewPage`/`ShuffleEnabled`/`SectionNumber`/`Name` copied).
- Build an old→new Section map (by `SectionNumber` within the package, or `Dictionary<int oldId, newSection>`); set `newQ.Section = map[q.SectionId]` **via navigation property** so EF assigns the new FK on save (avoids cross-package leak — Pitfall 8). Do NOT naive-copy `q.SectionId`.
- Options now include 5–6 if present (the existing `q.Options.Select(...)` already clones ALL options regardless of count — verify it carries opsi E/F once import stores them; it does, since it iterates the collection).
- Single point fix → all 6 callers inherit it (Pitfall 7 — but VERIFY each caller path, don't assume).

---

### `AssessmentAdminController.cs` — `DownloadQuestionTemplate` (controller, file-I/O) — MODIFY

**Analog:** itself — `:6599-6669` (ClosedXML workbook build + header styling + example rows).

**Current header + styling loop** (`:6605-6616`):
```csharp
using var workbook = new XLWorkbook();
var ws = workbook.Worksheets.Add("Question Import");
var headers = new[] { "Pertanyaan", "Opsi A", "Opsi B", "Opsi C", "Opsi D", "Jawaban Benar", "Elemen Teknis", "QuestionType", "Rubrik" };
for (int i = 0; i < headers.Length; i++) {
    ws.Cell(1, i + 1).Value = headers[i];
    ws.Cell(1, i + 1).Style.Font.Bold = true;
    ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
    ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
}
// ... AddExampleRow / AddInstruction helpers ...
return ExcelExportHelper.ToFileResult(workbook, fileName, this);
```

**Extension (IMP-01, D-415-03):** expand the `headers` array to the universal 13-col layout (spec §9.1 order): `Pertanyaan | Opsi A..F | Jawaban Benar | No. Section | Nama Section | Elemen Teknis | QuestionType | Rubrik`. Keep the same styling loop + `ExcelExportHelper.ToFileResult` return. UI-SPEC names the button "Template Universal" (`btn-outline-primary` + `bi bi-download`).

---

### `AssessmentAdminController.cs` — `CreateQuestion`/`EditQuestion` + `ManagePackageQuestions` GET (controller, request-response) — MODIFY

**Analog:** itself — `CreateQuestion` `:7116-7128` (discrete-param contract), `ManagePackageQuestions` GET `:7090-7107`.

**Current `CreateQuestion` discrete contract** (`:7116-7128`) — ADD `int? sectionId` param ONLY (do NOT refactor optionA..D to arrays — that's 418, RESEARCH line 391):
```csharp
public async Task<IActionResult> CreateQuestion(int packageId, string questionText, string questionType,
    int scoreValue, string? elemenTeknis, string? rubrik, int maxCharacters,
    string? optionA, string? optionB, string? optionC, string? optionD,
    bool correctA, bool correctB, bool correctC, bool correctD, /* + int? sectionId */ ...)
```

**Current `ManagePackageQuestions` GET** (`:7090-7107`) — passes `ViewBag.Questions` ordered by `Order`:
```csharp
var pkg = await _context.AssessmentPackages
    .Include(p => p.Questions.OrderBy(q => q.Order)).ThenInclude(q => q.Options)
    .FirstOrDefaultAsync(p => p.Id == packageId);
ViewBag.Questions = pkg.Questions.OrderBy(q => q.Order).ToList();
return View();
```
**Extension (SEC-05):** also `.Include` Section rows; pass sections list + a Section dropdown source to the view via ViewBag. Grouping (by SectionId, null trailing as "Lainnya") can be computed in the view from `Questions` (which now carry `SectionId`/`Section` nav). Use `QuestionOptionValidator.ValidateQuestionOptions(...)` (`Helpers/QuestionOptionValidator.cs:20`) unchanged — Section adds no new option-validation.

---

### `Controllers/CMPController.cs` — D-13 re-guard at StartExam (controller, request-response) — MODIFY

**Analog:** itself — the seam **immediately before** `ShuffleEngine.BuildQuestionAssignment` at `:1074`.

**Insertion point** (`:1059-1075`): after `packages` are loaded (`:1050-1055`) and inside `if (packages.Any())`, BEFORE building the assignment:
```csharp
if (packages.Any())
{
    // <<< INSERT D-13 re-guard here (drift check, Pitfall 6) >>>
    var assignment = await _context.UserPackageAssignments.FirstOrDefaultAsync(...);
    if (assignment == null) {
        var shuffledIds = ShuffleEngine.BuildQuestionAssignment(packages, assessment.ShuffleQuestions, workerIndex, rng);
```
**Guard logic (Pitfall 6 — must NOT break legacy):** fire ONLY when sibling packages exist AND ≥1 has Sections; group each sibling's questions by `SectionNumber` and compare counts; on mismatch return a server-side block with UI-SPEC copy (`415-UI-SPEC.md:159`): `"Ujian tidak dapat dimulai: struktur Section antar-paket tidak identik. Hubungi HC ..."`. All-null SectionId (legacy) → pass; single package → pass. The existing `IsParticipantRemoved` guard at `:373` is the precedent for an early StartExam block.

---

### `Views/Admin/ManagePackageQuestions.cshtml` (component, request-response) — MODIFY

**Analog:** itself — two-column `row g-4` layout `:37-44` + the toggle precedent in `ManagePackages.cshtml:107-116`.

**Existing two-column shell** (`ManagePackageQuestions.cshtml:9,37-44`):
```html
<div class="container-fluid py-4">
  <div class="row g-4">
    <div class="col-lg-7">
      <div class="card shadow-sm">
        <div class="card-header bg-light d-flex justify-content-between align-items-center">
          <span class="fw-semibold">Daftar Soal (...)</span>
        </div>
        <div class="card-body p-0"> ... table table-hover table-sm ... </div>
```

**Toggle (form-switch) precedent** (`ManagePackages.cshtml:107-116` — copy for StartNewPage/ShuffleEnabled per-Section):
```html
<div class="form-check form-switch mb-2">
  <input class="form-check-input" type="checkbox" name="shuffleQuestions" value="true" @(sqChecked ? "checked" : "") />
  <label class="form-check-label" for="shuffleQuestions">Acak Soal</label>
</div>
<div class="form-text text-muted mb-3">...helper...</div>
```

**Empty-state + TempData alert precedents** (`ManagePackageQuestions.cshtml:28-35,47-50`):
```html
<div class="alert alert-success alert-dismissible fade show">@TempData["Success"]<button class="btn-close" data-bs-dismiss="alert"></button></div>
<div class="p-4 text-center text-muted"><i class="bi bi-inbox fs-2 d-block mb-2"></i>Belum ada soal...</div>
```

**Build (D-415-01/02, SEC-01/02/03/05):** add a `card shadow-sm` Section panel (inline, full-width above the `row g-4` or in the form column) with: Section list/table (No.Section, Nama, toggles, Aksi icon-buttons), create/edit Section form (No.Section `*`, Nama optional, two `form-check form-switch` toggles, "Semua Section mulai halaman baru" quick button, "Simpan Section" `btn-primary`), the `Section` dropdown in the create/edit-question form (`— Tanpa Section (Lainnya) —` default), and group the question list by Section with `bg-light fw-semibold` group headers + trailing "Lainnya (tanpa Section)". Native `confirm(...)` for delete (copy from `415-UI-SPEC.md:162`). All copy/colors/spacing are PRESCRIBED in `415-UI-SPEC.md` — follow it verbatim. **JS minim** (D-415-02): no SortableJS/drag-drop.

---

### `Views/Admin/ImportPackageQuestions.cshtml` (component, request-response) — MODIFY

**Analog:** itself + the TempData alert pattern. Add the format-reference card (`text-info`/`border-info` per UI-SPEC), the dual-format note (UI-SPEC `:146`), and the D-13 full mismatch `<ul>` error block fed by `TempData` list (`415-UI-SPEC.md:158`). Keep form POST (not AJAX) — consistent with current view (O-3).

---

### `HcPortal.Tests/Section*.cs` (test, transform) — NEW ×4 + fixture

**Analog (real-SQL fixture):** `FlexibleParticipantAddTests.cs:20-53` — `IAsyncLifetime` disposable `HcPortalDB_Test_{guid}` fixture.

**Fixture pattern (copy verbatim → `SectionFixture`)** (`FlexibleParticipantAddTests.cs:20-52`):
```csharp
public class FlexibleParticipantAddFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public DbContextOptions<ApplicationDbContext> Options => _options;
    public FlexibleParticipantAddFixture() {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }
    public async Task InitializeAsync() {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try { await using var ctx = new ApplicationDbContext(_options); await ctx.Database.MigrateAsync(); }
        catch (Exception ex) { /* EnsureDeleted + throw XunitException MIGRATION-CHAIN */ }
    }
    public async Task DisposeAsync() { await using var ctx = new ApplicationDbContext(_options); await ctx.Database.EnsureDeletedAsync(); }
}
[Trait("Category", "Integration")]
public class FlexibleParticipantAddTests : IClassFixture<FlexibleParticipantAddFixture> { ... }
```
> `MigrateAsync()` now includes the 415 migration — that is exactly how the new migration gets exercised (real FK SetNull + unique index need real SQL, NOT InMemory).

**Analog (Excel import roundtrip + InMemory pure tests):** `InjectExcelImportTests.cs:12-49` — ClosedXML usings, `NewCtx()`, in-memory builders. The parser/fingerprint/count-compare are pure → use EF InMemory 8.0.0 (`Category!=Integration`).

**Wave-0 suites (RESEARCH §Validation):**
- `SectionCrudTests.cs` (Integration) — unique index + FK SetNull on delete.
- `SectionImportTests.cs` (mostly InMemory) — IMP-01 template roundtrip, IMP-02 9-col legacy parse, IMP-03 fingerprint (E/F+SectionNumber) + per-Section count.
- `SectionSyncPrePostTests.cs` (Integration) — SEC-06 deep-clone remap + opsi 5–6.
- `SectionMismatchGuardTests.cs` (InMemory count-compare + Integration StartExam) — SEC-04 full-list reject + legacy/single-pkg pass.
- **Backward-compat invariant:** `dotnet test --filter "FullyQualifiedName~Shuffle"` (~180) MUST stay green (Section-empty = old behavior).

## Shared Patterns

### RBAC + Antiforgery (all new/modified Admin POST endpoints)
**Source:** `AssessmentAdminController.cs:6185-6188` (`UpdateShuffleSettings`)
**Apply to:** CreateSection / EditSection / DeleteSection / SetAllSectionsNewPage / CreateQuestion / EditQuestion / ImportPackageQuestions
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```
Views POST with `@Html.AntiForgeryToken()` (`ManagePackages.cshtml:105`).

### Audit log (warn-only, never throw)
**Source:** `AssessmentAdminController.cs:6228-6240`
**Apply to:** all Section CRUD writes + ImportPackageQuestions (already audited at `:6996-7007`)
```csharp
try { await _auditLog.LogAsync(userId, actorName, "ActionType", "desc", entityId, "EntityType"); }
catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed ..."); }
```
Actor name idiom: `string.IsNullOrWhiteSpace(user?.NIP) ? (user?.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}"`.

### PRG + TempData feedback (all Admin write endpoints)
**Source:** `AssessmentAdminController.cs:6242-6243` + view alert blocks `ManagePackageQuestions.cshtml:28-35`
**Apply to:** every Section write + import result/error
```csharp
TempData["Success"] = "...";  // or TempData["Error"] / TempData["Warning"]
return RedirectToAction("ManagePackageQuestions", new { packageId });
```
Bahasa Indonesia copy is prescribed in `415-UI-SPEC.md` (Copywriting Contract).

### Sibling-package resolution (import + StartExam guard + sync)
**Source:** `AssessmentAdminController.cs:6827-6832` (and identical key in `UpdateShuffleSettings:6195-6200`, `CMPController` StartExam)
**Apply to:** per-Section count validation (import) + D-13 re-guard (StartExam)
```csharp
.Where(s => s.Title == target.Title && s.Category == target.Category && s.Schedule.Date == target.Schedule.Date)
```
> The sibling key `Title+Category+Schedule.Date` is the SAME across StartExam / Reshuffle / EditAssessment / import — reuse it; do NOT invent a new grouping key (Phase 397 lesson: standalone grouping key must equal service write key).

### Atomic write (zero-write-on-error)
**Source:** `AssessmentAdminController.cs:6962-6977` (import persist)
**Apply to:** import commit (D-415-04 reject = zero writes)
```csharp
using var tx = await _context.Database.BeginTransactionAsync();
try { await _context.SaveChangesAsync(); await tx.CommitAsync(); }
catch { await tx.RollbackAsync(); throw; }
```

### Shared pure option validator (unchanged — do not duplicate)
**Source:** `Helpers/QuestionOptionValidator.cs:20`
**Apply to:** CreateQuestion/EditQuestion (already wired) — Section adds NO new option-validation rule in 415.
```csharp
public static (bool ok, string? error) ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)
```

## No Analog Found

None. Every Phase-415 file extends or mirrors an existing in-codebase seam. The ONE construct without a co-located precedent is the migration's `CreateTable`/`CreateIndex`/`AddForeignKey` block — but that is **generated by `dotnet ef migrations add`** from the DbContext config (do not hand-write), so no hand-roll analog is needed (RESEARCH Pattern 1 shows the expected generated shape).

## Metadata

**Analog search scope:** `Models/`, `Data/`, `Controllers/` (AssessmentAdminController.cs, CMPController.cs), `Helpers/`, `Views/Admin/`, `Migrations/`, `HcPortal.Tests/`
**Files scanned:** ~12 read in full/part + 3 grep symbol sweeps; all RESEARCH line refs re-verified live (accurate within ±20, symbols stable)
**Key cross-cutting:** RBAC `Admin, HC` + antiforgery + warn-only audit + PRG/TempData + sibling-key `Title+Category+Schedule.Date` + atomic tx — uniform across the entire Admin controller; new Section endpoints MUST match.
**Open items carried for planner:** O-1 (widen answer whitelist A–F at import parse — RESEARCH recommends YES), O-2 (Section auto-create tiebreak on differing Nama — first-non-empty), O-3 (import result = redirect+TempData full list, not AJAX). Pitfall 1 (EF tool pin 8.0.0 from repo root), Pitfall 2 (FK SetNull on PackageQuestion.SectionId — avoids multiple-cascade-path).
**Pattern extraction date:** 2026-06-22
