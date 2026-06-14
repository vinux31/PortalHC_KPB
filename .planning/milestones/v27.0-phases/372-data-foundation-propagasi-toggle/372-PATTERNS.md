# Phase 372: Data Foundation + Propagasi Toggle - Pattern Map

**Mapped:** 2026-06-13
**Files analyzed:** 5 (4 modified + 1 new migration)
**Analogs found:** 5 / 5 (all exact, end-to-end `AllowAnswerReview` triplet)
**Line verification:** All RESEARCH.md line numbers re-verified against live code this session — NO v25.0 shift yet. Executor MUST re-grep at execute-time (367/368 paralel may shift `AssessmentAdminController.cs` lines 1200-2130).

> **Key insight:** Every aspect of Phase 372 has an identical, living analog in the codebase. This is a "copy the pattern, rename the symbol" phase. The single bool `AllowAnswerReview` already exists end-to-end (entity prop `= true` → Fluent `HasDefaultValue(true)` → `bit defaultValue:true` migration → snapshot `.ValueGeneratedOnAdd().HasDefaultValue(true)` → set `= model.AllowAnswerReview` at ALL 7 write-sites + 2 foreach → summary JS). The dominant risk is NOT technical — it is **completeness** (hitting all 7 `new AssessmentSession` write-sites, including the 3 add-participant-on-Edit sites the spec omitted).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Models/AssessmentSession.cs` | model (entity) | CRUD (persisted column) | `AllowAnswerReview` prop (line 33) / `GenerateCertificate` (line 36) | exact |
| `Data/ApplicationDbContext.cs` | config (Fluent mapping) | CRUD (default-value config) | `AllowAnswerReview` `HasDefaultValue(true)` (line 215) | exact |
| `Migrations/<ts>_AddShuffleTogglesToAssessmentSession.cs` | migration | batch (DDL backfill) | `20260214011828_AddAssessmentResultFields.cs:14-19` + single-bool template `20260311012214_AddGenerateCertificateToAssessmentSession.cs` | exact |
| `Controllers/AssessmentAdminController.cs` | controller | request-response (POST → entity write) | `AllowAnswerReview = model.AllowAnswerReview` at the same 7 sites + 2 foreach | exact |
| `Views/Admin/CreateAssessment.cshtml` | view (Razor + JS) | request-response (form bind to ENTITY) + client mirror | `IsTokenRequired` form-switch (505-508) + summary token rows + `populateSummary` token JS (1160-1163) | exact |

**Critical data-flow note (bind target):** The CreateAssessment / EditAssessment POST actions bind the **`AssessmentSession` ENTITY itself** as the form model (NOT a separate ViewModel) — verified POST signatures at `:846` and `:1766`. Therefore the toggle bind props (`ShuffleQuestions` / `ShuffleOptions`) live on the entity (file #1) and are picked up by `asp-for="ShuffleQuestions"` directly. Do NOT introduce a ViewModel (anti-pattern — see Shared Patterns).

---

## Pattern Assignments

### `Models/AssessmentSession.cs` (model, CRUD)

**Analog:** `AllowAnswerReview` (line 33), `GenerateCertificate` (line 36) — same file.

**Entity prop pattern** (lines 28-36, verbatim live):
```csharp
[Range(0, 100)]
[Display(Name = "Pass Percentage (%)")]
public int PassPercentage { get; set; } = 70;

[Display(Name = "Allow Answer Review")]
public bool AllowAnswerReview { get; set; } = true;

[Display(Name = "Terbitkan Sertifikat")]
public bool GenerateCertificate { get; set; } = false;
```

**To add** (after line 36, near `GenerateCertificate`) — copy the `AllowAnswerReview` shape, default `= true` (D-03/D-06, EF bool-trap fix D-08), Bahasa Indonesia `[Display]` per spec §4:
```csharp
[Display(Name = "Acak Soal")]
public bool ShuffleQuestions { get; set; } = true;

[Display(Name = "Acak Pilihan Jawaban")]
public bool ShuffleOptions { get; set; } = true;
```
> CRITICAL: `= true` initializer is load-bearing. C# `bool` default is `false`; without `= true` the object-init default is OFF (EF bool-trap, Pitfall 1).

---

### `Data/ApplicationDbContext.cs` (config, CRUD)

**Analog:** `AllowAnswerReview` Fluent (line 215) — same file, "Default values" block.

**Default-value Fluent pattern** (lines 213-216, verbatim live):
```csharp
// Default values
entity.Property(a => a.PassPercentage).HasDefaultValue(70);
entity.Property(a => a.AllowAnswerReview).HasDefaultValue(true);
entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
```

**To add** (after line 215, inside the same `builder.Entity<AssessmentSession>(entity => { ... })` block):
```csharp
entity.Property(a => a.ShuffleQuestions).HasDefaultValue(true);
entity.Property(a => a.ShuffleOptions).HasDefaultValue(true);
```
> WHY Fluent first, THEN migration: `AllowAnswerReview` has Fluent → snapshot emits `.ValueGeneratedOnAdd().HasDefaultValue(true)` (snapshot lines 375-378, verified). `GenerateCertificate` has NO Fluent → snapshot drift (Pitfall 3). Add Fluent BEFORE running `dotnet ef migrations add`.

---

### `Migrations/<ts>_AddShuffleTogglesToAssessmentSession.cs` (migration, batch)

**Analog:** `20260214011828_AddAssessmentResultFields.cs:14-19` (AllowAnswerReview `AddColumn`) + `20260311012214_AddGenerateCertificateToAssessmentSession.cs` (cleanest single-bool template with `Up`/`Down`).

**DO NOT hand-write.** Generate via CLI (auto-emits DDL + updates snapshot — Pitfall 3):
```bash
dotnet ef migrations add AddShuffleTogglesToAssessmentSession --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext
```

**Expected `Up` shape** (verbatim analog `AddAssessmentResultFields.cs:14-19`, repeated ×2):
```csharp
migrationBuilder.AddColumn<bool>(
    name: "ShuffleQuestions",
    table: "AssessmentSessions",
    type: "bit",
    nullable: false,
    defaultValue: true);

migrationBuilder.AddColumn<bool>(
    name: "ShuffleOptions",
    table: "AssessmentSessions",
    type: "bit",
    nullable: false,
    defaultValue: true);
```

**Expected `Down` shape** (verbatim analog `AddGenerateCertificateToAssessmentSession.cs:24-26`, repeated ×2):
```csharp
migrationBuilder.DropColumn(name: "ShuffleQuestions", table: "AssessmentSessions");
migrationBuilder.DropColumn(name: "ShuffleOptions", table: "AssessmentSessions");
```
> `defaultValue: true` makes SQL Server backfill ALL existing rows to `1` on `ADD COLUMN` — fulfills D-06 "data lama tak berubah" (SHUF-01). Verify locally: `SELECT TOP 5 Id, ShuffleQuestions, ShuffleOptions FROM AssessmentSessions ORDER BY Id;` → all `1`.

---

### `Controllers/AssessmentAdminController.cs` (controller, request-response)

**Analog:** `AllowAnswerReview = model.AllowAnswerReview` assignment at the IDENTICAL sites — same file.

**COMPLETENESS IS THE RISK.** There are **7 `new AssessmentSession` sites + 2 propagation foreach** — re-verified live this session (all match RESEARCH.md). The spec only named "3 create loops + EditAssessment foreach". The 3 add-participant-on-Edit sites (1895/1914/2111) were NOT in spec — **RESEARCH recommends including them** (without them, participants added later via Edit hit the EF bool-trap a second time → silent OFF; Pitfall 2). Planner default: INCLUDE all.

| # | Line | Site | Source of value | Add |
|---|------|------|-----------------|-----|
| 1 | 684 | GET model init | entity default `= true` (no explicit set) | NOT required — entity default `= true` renders `checked`. Consistent with `AllowAnswerReview = true` at :689. Optional for clarity. (Open Q#2: recommend skip.) |
| 2 | 1218 | Pre create loop (`preSession`) | `model.*` (D-04 same value Pre & Post) | `ShuffleQuestions = model.ShuffleQuestions, ShuffleOptions = model.ShuffleOptions,` |
| 3 | 1252 | Post create loop (`postSession`) | `model.*` (D-04) | idem |
| 4 | 1427 | Standard create loop (`session`) | `model.*` | idem |
| 5 | 1895 | Add-participant Edit, `newPre` | `model.*` (follows `AllowAnswerReview = model.AllowAnswerReview` at :1904) | `ShuffleQuestions = model.ShuffleQuestions, ShuffleOptions = model.ShuffleOptions,` |
| 6 | 1914 | Add-participant Edit, `newPost` | `model.*` (follows :1923) | idem |
| 7 | 2111 | Add-participant Edit, `newSessions` | **`savedAssessment.*`** (follows `AllowAnswerReview = savedAssessment.AllowAnswerReview` at :2122 — already propagated, safe) | `ShuffleQuestions = savedAssessment.ShuffleQuestions, ShuffleOptions = savedAssessment.ShuffleOptions,` |
| F1 | 1797-1806 | Pre-Post propagation `foreach (var s in allGroupSessions)` | `model.*` | `s.ShuffleQuestions = model.ShuffleQuestions; s.ShuffleOptions = model.ShuffleOptions;` |
| F2 | 2016-2031 | Standard propagation `foreach (var sibling in siblings)` | `model.*` | `sibling.ShuffleQuestions = model.ShuffleQuestions; sibling.ShuffleOptions = model.ShuffleOptions;` |

**Create-loop assignment pattern** (verbatim live, standard loop lines 1435-1439):
```csharp
var session = new AssessmentSession
{
    // ...
    IsTokenRequired = model.IsTokenRequired,
    AccessToken = model.AccessToken,
    PassPercentage = model.PassPercentage,
    AllowAnswerReview = model.AllowAnswerReview,   // ← analog: add Shuffle* lines right after
    GenerateCertificate = model.GenerateCertificate,
    // ...
};
```

**Site 2111 — source-from-savedAssessment pattern** (verbatim live, lines 2121-2123) — NOTE the source differs (`savedAssessment.*`, not `model.*`):
```csharp
var newSessions = filteredNewUserIds.Select(uid => new AssessmentSession
{
    // ...
    PassPercentage = savedAssessment.PassPercentage,
    AllowAnswerReview = savedAssessment.AllowAnswerReview,   // ← add: ShuffleQuestions = savedAssessment.ShuffleQuestions, etc.
    GenerateCertificate = savedAssessment.GenerateCertificate,
    // ...
}).ToList();
```

**Propagation foreach pattern — standard branch** (verbatim live, lines 2016-2031):
```csharp
foreach (var sibling in siblings)
{
    sibling.Title = model.Title;
    sibling.Category = model.Category;
    // ...
    sibling.PassPercentage = model.PassPercentage;
    sibling.AllowAnswerReview = model.AllowAnswerReview;   // ← add: sibling.ShuffleQuestions = model.ShuffleQuestions; etc.
    sibling.GenerateCertificate = model.GenerateCertificate;
    sibling.ExamWindowCloseDate = model.ExamWindowCloseDate;
    sibling.UpdatedAt = now;
}
```

**Propagation foreach pattern — Pre-Post branch** (verbatim live, lines 1797-1806):
```csharp
foreach (var s in allGroupSessions)
{
    s.Title = model.Title;
    s.Category = model.Category;
    s.PassPercentage = model.PassPercentage;
    s.AllowAnswerReview = model.AllowAnswerReview;   // ← add: s.ShuffleQuestions = model.ShuffleQuestions; etc.
    s.IsTokenRequired = model.IsTokenRequired;
    s.AccessToken = model.IsTokenRequired ? (model.AccessToken ?? s.AccessToken ?? "") : "";
    s.UpdatedAt = DateTime.UtcNow;
}
```

**Auth pattern** (inherited — no change): POST `CreateAssessment` (`[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` at `:844`) and POST `EditAssessment` (`:1764`) already protect these forms. Phase 372 adds NO new endpoint (`UpdateShuffleSettings` = Phase 374). Toggles ride existing protected forms.

---

### `Views/Admin/CreateAssessment.cshtml` (view, request-response bind-to-entity + client JS mirror)

**Analog:** `IsTokenRequired` form-switch (505-508) for the toggle; existing summary `dt/dd` rows (653-661 / 691-697); `populateSummary` token JS (1119-1122 Pre-Post, 1160-1163 standard).

**(A) Toggle input — form-switch pattern** (verbatim live, lines 500-508, the wrapping `col-md-6` + `form-label fw-bold` heading + `form-check form-switch`):
```html
<!-- Token (analog) -->
<div class="col-md-6">
    <label class="form-label fw-bold">
        <i class="bi bi-shield-lock text-primary me-1"></i>Security Token
    </label>
    <div class="form-check form-switch mb-2">
        <input class="form-check-input" type="checkbox" asp-for="IsTokenRequired" id="IsTokenRequired" />
        <label class="form-check-label" for="IsTokenRequired">Wajib token untuk memulai ujian</label>
    </div>
    ...
</div>
```
**To add:** a NEW `col-md-6` AFTER the Token column (after line 519, inside the same `div.row.g-3` of Group B "Pengaturan Ujian" — D-01, NO new card). Per UI-SPEC: one `col-md-6` containing a `bi-shuffle text-primary` sub-heading "Pengacakan Soal & Jawaban" + TWO stacked `form-check form-switch mb-2` toggles, each followed by a `div.form-text.text-muted` help-text. Shape mirror per toggle:
```html
<div class="form-check form-switch mb-2">
    <input class="form-check-input" type="checkbox" asp-for="ShuffleQuestions" id="ShuffleQuestions" />
    <label class="form-check-label" for="ShuffleQuestions">Acak Soal</label>
</div>
<div class="form-text text-muted">[help-text VERBATIM from 372-UI-SPEC.md Copywriting Contract]</div>
```
> Copy is LOCKED VERBATIM in `372-UI-SPEC.md §Copywriting Contract` — executor MUST NOT trim. The "jawaban benar tetap dinilai dengan benar" phrase in Toggle 2 help-text is MANDATORY (D-11 reassurance). Default ON comes from the model (`= true`) rendering `checked`, NOT a hardcoded `checked` attribute (D-08).

**(B) Summary rows — `dt/dd` pattern** (verbatim live, standard block lines 658-659):
```html
<dt class="col-sm-4">Token</dt><dd class="col-sm-8" id="summary-token"></dd>
<dt class="col-sm-4">Pass Percentage</dt><dd class="col-sm-8" id="summary-pass"></dd>
```
**To add:** 2 rows in BOTH summary blocks (standard `summary-standard-settings` after line 659, Pre-Post `summary-ppt-settings` after line 694), placed after "Pass Percentage" (D-04: one toggle pair applies to Pre & Post, D-05: show in summary). Suggested IDs (executor discretion): `summary-shuffle-questions`/`summary-shuffle-options` (standard) and `summary-ppt-shuffle-questions`/`summary-ppt-shuffle-options` (Pre-Post):
```html
<dt class="col-sm-4">Acak Soal</dt><dd class="col-sm-8" id="summary-shuffle-questions"></dd>
<dt class="col-sm-4">Acak Pilihan Jawaban</dt><dd class="col-sm-8" id="summary-shuffle-options"></dd>
```

**(C) Summary JS mirror — `populateSummary()` pattern** (verbatim live, standard branch lines 1160-1163):
```javascript
var tokenEl = document.getElementById('IsTokenRequired');
var accessTokenEl = document.getElementById('AccessToken');
var summToken = document.getElementById('summary-token');
if (summToken) summToken.textContent = (tokenEl && tokenEl.checked) ? 'Ya — ' + (accessTokenEl ? accessTokenEl.value : '') : 'Tidak';
```
**To add:** read `.checked`, write plain text (UI-SPEC: NO colored badge — ON & OFF both valid). MUST be added to BOTH branches (Pre-Post `isPrePost` ~line 1119+, AND standard ~line 1160+) because one toggle pair applies to Pre & Post (D-04):
```javascript
var sqEl = document.getElementById('ShuffleQuestions');
var summSQ = document.getElementById('summary-shuffle-questions');
if (summSQ) summSQ.textContent = (sqEl && sqEl.checked) ? 'Aktif (ON)' : 'Nonaktif (OFF)';
```

---

## Shared Patterns

### Bool-with-default column (end-to-end triplet)
**Source:** `AllowAnswerReview` — `Models/AssessmentSession.cs:33` + `Data/ApplicationDbContext.cs:215` + `Migrations/20260214011828_AddAssessmentResultFields.cs:14-19` + snapshot `ApplicationDbContextModelSnapshot.cs:375-378`.
**Apply to:** SHUF-01 (both new columns).
**Rule:** entity `= true` → Fluent `HasDefaultValue(true)` → `dotnet ef migrations add` (auto DDL `defaultValue:true` + snapshot `.ValueGeneratedOnAdd().HasDefaultValue(true)`). Do all three or you get drift / bool-trap.

### Form binds to ENTITY (NO ViewModel)
**Source:** POST signatures `AssessmentAdminController.cs:846` (`CreateAssessment(AssessmentSession model, ...)`) and `:1766` (`EditAssessment(AssessmentSession model, ...)`).
**Apply to:** the view toggles (`asp-for="ShuffleQuestions"`) and all controller write-sites.
**Rule:** the bind model IS `AssessmentSession`. Toggle bind props live on the entity (file #1). DO NOT create a `CreateAssessmentVm` — anti-pattern, breaks project consistency.

### Explicit-set at EVERY write-site (EF bool-trap fix)
**Source:** `AllowAnswerReview = model.AllowAnswerReview` appears at 6 of 7 `new AssessmentSession` sites + both foreach (lines 1227, 1261, 1438, 1802, 1904, 1923, 2027) and `savedAssessment.AllowAnswerReview` at 2122.
**Apply to:** ALL of SHUF-02 + SHUF-03 + add-participant sites.
**Rule:** `bool` C# default = `false`; the DB `defaultValue` only backfills rows inserted WITHOUT the column — EF always sends the C# value. Miss a site → that path silently saves OFF.

### Auth / anti-forgery (inherited, no change)
**Source:** `AssessmentAdminController.cs:844` and `:1764` — `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`.
**Apply to:** both POST forms the toggles ride.
**Rule:** Phase 372 adds NO endpoint. Toggles inherit existing access control + CSRF protection.

### Migration-default test (real-SQL disposable fixture)
**Source:** `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` (`OrgLabelMigrationFixture`, lines 24-66) — `IAsyncLifetime` + `UseSqlServer` disposable `HcPortalDB_Test_<guid>` + `MigrateAsync()` + `EnsureDeletedAsync()`. Reusable alt: `ProtonCompletionFixture`.
**Apply to:** SHUF-01 backfill verification (test files are Wave 0 — created in Phase 375, but the fixture pattern is here for reference).
**Rule:** InMemory bypasses migration DDL (builds schema-from-model) → CANNOT prove `defaultValue:true`. Default-backfill assertions MUST use the real-SQL fixture with `[Trait("Category","Integration")]`.

---

## No Analog Found

None. Every file/aspect maps to an exact living analog (the `AllowAnswerReview` end-to-end triplet + `IsTokenRequired` form-switch + summary token JS + `OrgLabelMigrationFixture`). RESEARCH.md `## Code Examples` are redundant safety nets — prefer the live analogs cited above.

---

## Constraints for Planner / Executor (carry-forward)

- **File-overlap v25.0 ACTIVE:** `AssessmentAdminController.cs` is touched by Phase 367/368 in parallel sessions. DO NOT `/gsd-execute-phase 372` before 367/368 ship or merge is coordinated. Re-grep all `AssessmentAdminController.cs` line numbers at execute-time — lines 1200-2130 may shift (Pitfall 4).
- **Sequential strict v27.0:** 372 → 373 → 374 → 375. DO NOT `/gsd-new-milestone` / `/gsd-complete-milestone` vanilla (clobbers STATE/phases pinned v25.0).
- **Migration handoff:** 1 migration `AddShuffleTogglesToAssessmentSession`. Notify IT with commit hash + migration flag (DEV_WORKFLOW step 5); IT runs `dotnet ef database update` on Dev/Prod. Bundle with existing carry-over IT.
- **Copy is LOCKED:** toggle/summary copy is verbatim in `372-UI-SPEC.md §Copywriting Contract` — Bahasa Indonesia mandate (CLAUDE.md), no trimming.
- **Out of scope (downstream):** reading shuffle in StartExam/reshuffle/round-robin = Phase 373; ManagePackages UI + `UpdateShuffleSettings` endpoint + lock + warning + reminder + hide Proton/Manual = Phase 374; xUnit + Playwright UAT = Phase 375.

## Metadata

**Analog search scope:** `Models/`, `Data/`, `Migrations/`, `Controllers/`, `Views/Admin/`, `HcPortal.Tests/`
**Files scanned (read in full or targeted):** `Models/AssessmentSession.cs`, `Data/ApplicationDbContext.cs`, `Migrations/20260311012214_AddGenerateCertificateToAssessmentSession.cs`, `Migrations/20260214011828_AddAssessmentResultFields.cs`, `Migrations/ApplicationDbContextModelSnapshot.cs`, `Controllers/AssessmentAdminController.cs` (9 regions), `Views/Admin/CreateAssessment.cshtml` (4 regions), `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs`
**Pattern extraction date:** 2026-06-13
