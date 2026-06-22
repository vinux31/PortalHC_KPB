# Phase 415: Section Foundation + Import Excel Diperluas - Research

**Researched:** 2026-06-22
**Domain:** ASP.NET Core 8 MVC + EF Core 8 (SQL Server) — additive schema migration, Excel import (ClosedXML), Razor admin UI
**Confidence:** HIGH (spec is code-verified via 2 prior multi-agent sweeps; all key signatures + line refs re-confirmed against live code this session)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-415-01:** UI kelola Section = **panel inline di `Views/Admin/ManagePackageQuestions.cshtml`** (BUKAN halaman khusus / modal). HC lihat Section + soal-nya sekaligus, assign langsung.
- **D-415-02:** Urutan Section ditentukan **No.Section angka** (HC ketik 1,2,3 — D-04). Assign soal→Section = **dropdown pilih Section di form buat/edit soal**. JS minim (BUKAN drag-drop). Bulk-assign = OUT (defer; tak diminta).
- **D-415-03:** **SATU template universal diperluas** (tambah kolom No.Section + Nama Section + Opsi A–F). Deteksi format otomatis by jumlah kolom: ≤9 kolom = file lama (tanpa Section, opsi A–D — tetap di-import, IMP-02); >9 = format baru. HC TIDAK pilih template manual. Kolom Opsi E/F **disiapkan di 415** (import menerima + data model simpan); authoring-form + render + grading huruf A–F = **Phase 418**.
- **D-415-04:** Error 'struktur Section antar-paket tidak sama' muncul di **DUA titik**: (1) **saat upload import** — tolak + tampilkan **DAFTAR ketidakcocokan LENGKAP** (sebut SectionNumber + jumlah soal diharapkan vs aktual per paket); (2) **guard ulang saat mulai ujian** (cek drift edit manual pasca-import). Pesan jelas Bahasa Indonesia.

### Carried forward (locked di spec — TIDAK dibahas ulang)
- Section = entity baru per-paket, terpisah dari ElemenTeknis; 1 Section ⊃ banyak ET (D-02/D-03).
- `AssessmentPackageSection`(Id, AssessmentPackageId FK, SectionNumber int, Name nvarchar null, StartNewPage bit default 0, ShuffleEnabled bit default 1) + index unik `(AssessmentPackageId, SectionNumber)`. `PackageQuestion.SectionId` int? nullable (spec §5.1/5.2).
- Section opsional → kosong = perilaku global lama; soal tanpa Section = grup "Lainnya" di urutan akhir (D-05/D-15).
- migration=TRUE `AddAssessmentPackageSection` (tabel + kolom), non-breaking (SectionId=null backfill), rollback drop (spec §5.4/§11).
- Fingerprint dedup = hash(Q, OptA..F, SectionNumber) (spec §15.C).
- Sync Pre→Post (`SyncPackagesToPost`/`CopyPackagesFromPre`) salin record Section + SectionId + opsi 5–6 (SEC-06, spec §15.E).
- Nama Section = opsional (boleh kosong, label tampilan).

### Claude's Discretion
- Ekstraksi abstraksi urutan-soal `SectionAwareQuestionProvider`/`IQuestionSequence` di awal 415 (spec §13 — pangkas penyebaran ~23 titik di fase 416/417/419). Keputusan teknis planner. **(Research recommendation below: DO NOT extract in 415 — see "IQuestionSequence Abstraction Decision".)**
- Mekanik migration EF, skema index, impl fingerprint, struktur view partial inline, lokasi seam validasi.

### Deferred Ideas (OUT OF SCOPE)
- Bulk-assign banyak soal ke Section sekaligus — future bila perlu (415 pakai per-soal dropdown).
- Drag-drop reorder Section (SortableJS) — future; 415 pakai No.Section angka.
- Scoped shuffle per-section (416), pagination (417), authoring-form A–F render/grading (418), export label Section (419).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SEC-01 | HC membuat/ubah/hapus/urut Section (No.Section + Nama) per paket via UI web | New entity `AssessmentPackageSection` + inline CRUD panel in `ManagePackageQuestions.cshtml` (D-415-01). CRUD endpoints in `AssessmentAdminController`. |
| SEC-02 | Toggle "Mulai Halaman Baru" + "Acak" per-Section + tombol cepat "semua Section mulai halaman baru" | `StartNewPage bit default 0`, `ShuffleEnabled bit default 1` columns. Toggle UI = `form-check form-switch` (precedent ManagePackages). 415 stores values only; consumed by 416/417. |
| SEC-03 | Tetapkan Section pada soal lewat No.Section; soal tanpa Section → grup "Lainnya" urutan terakhir | `PackageQuestion.SectionId int?` nullable FK. Dropdown in CreateQuestion/EditQuestion form (D-415-02). `null` = legacy behavior (D-05/D-15). |
| SEC-04 | Hard-block simpan/mulai ujian bila struktur Section antar-paket tidak identik | Validation at 2 seams (D-415-04): import (full mismatch list) + StartExam re-guard (`CMPController.cs:~1074` before BuildQuestionAssignment). |
| SEC-05 | Daftar & preview soal admin menampilkan soal dikelompokkan per Section dengan header | Group `questions` by SectionId in `ManagePackageQuestions.cshtml`; section group headers + "Lainnya (tanpa Section)" trailing group. |
| SEC-06 | Sync Pre→Post (SamePackage): struktur Section + opsi ikut tersalin | Extend `SyncPackagesToPost` (`AssessmentAdminController.cs:6366`) deep-clone to also clone Section records + remap SectionId + clone options 5–6. |
| IMP-01 | Template & parser import mendukung kolom No.Section, Nama Section, Opsi A–F | Extend `DownloadQuestionTemplate` (`:6599`) + `ImportPackageQuestions` parser (`:6690`). |
| IMP-02 | Dual-format kompatibel-mundur — file lama 9-kolom (Opsi A–D, tanpa Section) tetap di-import | Column-count auto-detect (≤9 lama / >9 baru) in parser. SectionId=null + opsi E/F empty for legacy. |
| IMP-03 | Validasi jumlah soal per-Section antar-paket (tolak keras) + fingerprint anti-dup +Section+opsi5–6 | Per-SectionNumber count validation (extends existing cross-package count check `:6823`). Extend `MakePackageFingerprint` (`:7081`) to include OptE/OptF + SectionNumber (§15.C). |
</phase_requirements>

## Summary

Phase 415 is a **keystone foundation phase** built on an exceptionally well-prepared codebase. The design spec (`2026-06-22-...-design.md`) already encodes two prior multi-agent code sweeps with verified file:line references. This research **re-confirmed every load-bearing signature and line reference against live code** — they are accurate within a small drift (±20 lines), and all critical claims (validator signature, fingerprint arity, ShuffleEngine entry, sync deep-clone shape, HTTP contract) hold exactly.

The work splits into 4 cleanly-layered concerns: (1) **additive EF migration** following the established Phase 352/409 nullable pattern (3 new columns on a new table + 1 nullable FK column — non-breaking, SectionId=null backfill, EF tool pinned to 8.0.0 already); (2) **inline Razor Section CRUD panel** matching the existing Bootstrap 5 two-column admin layout (UI-SPEC is approved and prescriptive); (3) **Excel import dual-format extension** widening the existing position-based parser + fingerprint + cross-package count validation to be Section-aware; and (4) **a hard-block validation seam at two points** (import + StartExam) per D-415-04.

**Primary recommendation:** Build 415 as a pure data + import + admin-UI foundation. **Do NOT extract the `IQuestionSequence`/`SectionAwareQuestionProvider` abstraction in 415** — the only consumer (ShuffleEngine.BuildQuestionAssignment) is not refactored until 416, and extracting an unused abstraction now adds a speculative seam with no test coverage and no behavioral change. Defer it to 416 where it earns its keep. 415 must leave all ~180 shuffle / 17.5K-line xUnit tests green (Section empty = old behavior), and write a NEW focused test suite (spec §12).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| `AssessmentPackageSection` schema + `SectionId` FK | Database / EF migration | — | Persistence; additive nullable migration owns this. |
| Section CRUD + reorder (No.Section) + toggles | API/Backend (`AssessmentAdminController`) | Razor view (form POST) | Server-authoritative writes; RBAC Admin,HC; antiforgery. |
| Section management UI (inline panel, grouped list, dropdown) | Frontend Server (Razor SSR) | — | Server-rendered MVC; no client framework. Minimal JS (D-415-02). |
| Excel template generation | API/Backend (`DownloadQuestionTemplate` + ClosedXML) | — | ClosedXML workbook built server-side, returned as file. |
| Excel parse + dual-format detect + fingerprint + count-validation | API/Backend (`ImportPackageQuestions`) | — | Position-based parser; all validation server-side (never trust column count blindly — see Pitfalls). |
| D-13 hard-block at import | API/Backend (import action) | — | Reject + full mismatch list before persist. |
| D-13 re-guard at exam start | API/Backend (`CMPController.StartExam`) | — | Drift detection at `:1074` before `BuildQuestionAssignment`. |
| Sync Pre→Post Section clone | API/Backend (`SyncPackagesToPost`) | — | Deep-clone within existing transaction; remap new SectionId. |

## Standard Stack

This phase introduces **NO new dependencies**. Everything needed is already in the project. Verified against `HcPortal.csproj` + `HcPortal.Tests/HcPortal.Tests.csproj`.

### Core (already present — verified)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | ORM + migration for new entity/column | `[VERIFIED: HcPortal.csproj]` Project standard; all entities use it. |
| Microsoft.EntityFrameworkCore.Design / .Tools | 8.0.0 | `dotnet ef migrations add` scaffolding | `[VERIFIED: HcPortal.csproj]` |
| dotnet-ef (local tool) | 8.0.0 (`rollForward: false`) | EF CLI pinned to avoid 10.x snapshot stamp | `[VERIFIED: .config/dotnet-tools.json]` Pinned in Phase 409 to mitigate Pitfall: global dotnet-ef 10.x stamps snapshot with wrong ProductVersion. **Use `dotnet ef` from repo root so local tool resolves.** |
| ClosedXML | 0.105.0 | Excel template generation + parse | `[VERIFIED: HcPortal.csproj]` Used by `DownloadQuestionTemplate` + `ImportPackageQuestions` (`XLWorkbook`). |
| Bootstrap 5 + Bootstrap Icons | (global via `_Layout`) | Admin UI components | `[VERIFIED: 415-UI-SPEC.md]` No new front-end dep. |

### Supporting (test stack — already present)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xunit | 2.9.3 | Test framework | `[VERIFIED: HcPortal.Tests.csproj]` All new 415 tests. |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | Fast in-memory DB for read-path / pure-logic tests | `[VERIFIED]` Use where NO `ExecuteUpdate`/raw SQL needed (entity shape, parser, fingerprint, count-validation). |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | `[VERIFIED]` |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Extend existing single-worksheet parser | Separate "legacy" + "new" parser classes | Spec mandates ONE universal template + auto-detect (D-415-03). Two parsers = drift risk + violates locked decision. Keep one parser, branch on column count. |
| Fluent API config for new entity | Data-annotation `[Index]` | Project convention uses Fluent `builder.Entity<T>(...)` in `ApplicationDbContext.OnModelCreating` (`:466-496` for the package family). Match it — unique index `(AssessmentPackageId, SectionNumber)` via `entity.HasIndex(...).IsUnique()`. |

**Installation:** None. All packages present.

**Version verification:** Confirmed against project files this session — not re-fetched from npm/nuget registry because these are locked project pins (net8.0 LTS line; upgrading EF mid-milestone is out of scope and would break the pinned migration snapshot chain).

## Architecture Patterns

### System Architecture Diagram (Phase 415 data flow)

```
                          ┌─────────────────────────────────────────────┐
   HC (Admin/HC role)     │   ManagePackageQuestions.cshtml (SSR Razor)  │
        │                 │  ┌──────────────┐  ┌──────────────────────┐ │
        │ (1) CRUD Section │  │ Section panel │  │ Question list grouped│ │
        ├─────────────────►  │ (inline card) │  │  by Section (SEC-05) │ │
        │                 │  └──────┬───────┘  └──────────────────────┘ │
        │ (2) assign soal  │  ┌──────▼─────────────────────────────────┐ │
        │     via dropdown │  │ Create/Edit Question form (+ Section dd)│ │
        │                 │  └──────────┬──────────────────────────────┘ │
        │                 └─────────────┼───────────────────────────────┘
        │  (3) upload xlsx               │ POST (antiforgery, RBAC Admin,HC)
        ▼                                ▼
┌───────────────────┐        ┌──────────────────────────────────────────┐
│ ImportPackage     │        │      AssessmentAdminController             │
│ Questions.cshtml  │──POST─►│  - Section CRUD endpoints (NEW)           │
└───────────────────┘        │  - CreateQuestion/EditQuestion (+sectionId)│
                             │  - ImportPackageQuestions (dual-format)    │
                             │      ├─ detect cols ≤9 vs >9               │
                             │      ├─ parse rows (pos-based)            │
                             │      ├─ MakePackageFingerprint(+E,F,Sec)  │
                             │      ├─ per-Section count validation D-13 │◄── hard-block #1
                             │      │     (full mismatch list)           │    (return error list)
                             │      └─ auto-create Section rows from cols│
                             │  - SyncPackagesToPost (clone Section+SecId)│
                             └──────────────────┬───────────────────────┘
                                                │ EF Core (single tx)
                                                ▼
                          ┌──────────────────────────────────────────────┐
                          │  SQL Server (HcPortalDB_Dev / Prod)           │
                          │  AssessmentPackage ─1:N─ AssessmentPackage    │
                          │                          Section (NEW)        │
                          │  AssessmentPackageSection ─1:N (nullable)─►   │
                          │                          PackageQuestion       │
                          │                          .SectionId int? (NEW) │
                          └──────────────────────────────────────────────┘
                                                ▲
   Worker starts exam ──► CMPController.StartExam (:373 guard, :1074 build)
                          └─ D-13 re-guard (drift check) ◄── hard-block #2
                             BEFORE ShuffleEngine.BuildQuestionAssignment
```

Trace the primary use case (HC defines sections + imports questions): HC opens ManagePackageQuestions → creates Sections in inline panel (1) → either assigns existing questions via the per-question dropdown (2) or uploads an Excel file (3) whose No.Section column auto-creates Section rows. Import validates per-Section counts across sibling packages (hard-block #1). At exam time, StartExam re-validates (hard-block #2) before building the shuffle assignment.

### Recommended File Touch Map (NOT a new structure — additive edits)
```
Models/AssessmentPackage.cs            # ADD class AssessmentPackageSection + PackageQuestion.SectionId int?
Data/ApplicationDbContext.cs           # ADD DbSet<AssessmentPackageSection> + Fluent config + unique index
Migrations/<ts>_AddAssessmentPackageSection.cs   # NEW (dotnet ef migrations add) — verify apply locally
Controllers/AssessmentAdminController.cs
   ├─ NEW: CreateSection / EditSection / DeleteSection / SetAllSectionsNewPage endpoints
   ├─ EDIT: CreateQuestion (+ int? sectionId param) / EditQuestion (+ int? sectionId)
   ├─ EDIT: ImportPackageQuestions (dual-format parse + per-Section validation + auto-create Section)
   ├─ EDIT: DownloadQuestionTemplate (universal 13-col template)
   ├─ EDIT: MakePackageFingerprint (+ OptE, OptF, SectionNumber)
   ├─ EDIT: ExtractPackageCorrectLetter ('ABCD' → keep A-D for 415; A-F deferred to 418*)
   ├─ EDIT: SyncPackagesToPost (clone Section rows + remap SectionId)
   └─ EDIT: ManagePackageQuestions GET (pass sections + grouped questions to view)
Views/Admin/ManagePackageQuestions.cshtml   # ADD inline Section panel + grouped list + Section dropdown
Views/Admin/ImportPackageQuestions.cshtml    # EDIT format-reference card + error/result alerts
Controllers/CMPController.cs                  # ADD D-13 re-guard before :1074 BuildQuestionAssignment
HcPortal.Tests/                               # NEW test suite (see Validation Architecture)
```
> *Note on ExtractPackageCorrectLetter: spec §8.2 says `'ABCD'→'ABCDEF'`. That is a **Phase 418** change (dynamic options render/grading A–F). For 415, the import column-acceptance for Opsi E/F is the only requirement — but **be careful**: if the import parser persists options E/F (data model supports unlimited PackageOptions), the correct-answer whitelist for MA/MC in the import path may need to accept E/F **at parse time** to not reject valid new-format rows. **OPEN QUESTION O-1 (below)** — confirm whether 415 import accepts answer letters E/F or only stores E/F option text with answers limited A–D. CONTEXT D-415-03 says "import menerima + data model simpan"; grading huruf A–F = 418. Recommended: 415 import **stores** options A–F and **accepts** correct-letters A–F (whitelist widen at parse), since rejecting E/F answers would make new-format files un-importable — but grading/render of those letters is 418's job (data is already PackageOption.Id-based, so storing a correct E option is safe).

### Pattern 1: Additive nullable EF migration (precedent Phase 352 / 409)
**What:** New table + nullable FK column, no defaultValue backfill needed beyond null, symmetric Down().
**When to use:** Any non-breaking schema add on a live ~600-question DB.
**Example:**
```csharp
// Source: precedent Migrations/20260606030844_AddImageToPackageQuestionAndOption.cs
//         + Migrations/20260621011101_AddParticipantRemovalColumns.cs (Phase 409)
// New table:
migrationBuilder.CreateTable(
    name: "AssessmentPackageSections",
    columns: table => new {
        Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
        AssessmentPackageId = table.Column<int>(nullable: false),
        SectionNumber = table.Column<int>(nullable: false),
        Name = table.Column<string>(nullable: true),
        StartNewPage = table.Column<bool>(nullable: false, defaultValue: false),
        ShuffleEnabled = table.Column<bool>(nullable: false, defaultValue: true),
    },
    constraints: table => {
        table.PrimaryKey("PK_AssessmentPackageSections", x => x.Id);
        table.ForeignKey("FK_..._AssessmentPackages_AssessmentPackageId",
            x => x.AssessmentPackageId, "AssessmentPackages", "Id",
            onDelete: ReferentialAction.Cascade);   // Section dies with package
    });
migrationBuilder.CreateIndex("IX_AssessmentPackageSections_AssessmentPackageId_SectionNumber",
    "AssessmentPackageSections", new[] { "AssessmentPackageId", "SectionNumber" }, unique: true);
// New nullable FK column on PackageQuestion:
migrationBuilder.AddColumn<int>("SectionId", "PackageQuestions", nullable: true);
migrationBuilder.CreateIndex("IX_PackageQuestions_SectionId", "PackageQuestions", "SectionId");
migrationBuilder.AddForeignKey("FK_PackageQuestions_AssessmentPackageSections_SectionId",
    "PackageQuestions", "SectionId", "AssessmentPackageSections", "Id",
    onDelete: ReferentialAction.SetNull);   // delete Section → questions become "Lainnya" (D-415 delete-confirm copy)
```
> **FK delete behavior decision (DESIGN-CRITICAL):** PackageQuestion.SectionId FK MUST be `ReferentialAction.SetNull` (NOT Cascade) — the UI-SPEC delete-Section confirmation promises "Soal di dalamnya menjadi 'Tanpa Section' (Lainnya), tidak terhapus." Cascade would delete the questions. `SetNull` matches the copy. BUT: `AssessmentPackageSection.AssessmentPackageId` FK to AssessmentPackages SHOULD be `Cascade` (section dies with its package). **Verify EF doesn't complain about multiple cascade paths** (AssessmentPackage→Section→? and AssessmentPackage→Question→?) — since Section→Question is SetNull, there is no multiple-cascade-path cycle. `[ASSUMED]` — confirm at scaffold time SQL Server accepts both FKs (it should: only one cascade path to PackageQuestions exists, via AssessmentPackageId; the SectionId path is SetNull).

### Pattern 2: Position-based dual-format Excel parse (extend existing)
**What:** Read worksheet, branch on header/used-column count, map cells by position.
**When to use:** The single universal template (D-415-03).
**Example (shape — extends `ImportPackageQuestions` `:6757`):**
```csharp
// Source: Controllers/AssessmentAdminController.cs:6755-6772 (current 9-col parser)
var ws = workbook.Worksheets.First();
// Detect format by header column count (use header row, robust against blank trailing cells in data rows):
int colCount = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
bool isNewFormat = colCount > 9;   // ≤9 = legacy A-D no-section (IMP-02); >9 = new
foreach (var row in ws.RowsUsed().Skip(1)) {
    var q = row.Cell(1).GetString().Trim();
    // NEW format column order (spec §9.1): Q | A-F(2-7) | Correct(8) | No.Section(9) | NamaSection(10) | ET(11) | Type(12) | Rubrik(13)
    // OLD format column order (current):  Q | A-D(2-5) | Correct(6) | ET(7) | Type(8) | Rubrik(9)
    // ... branch cell indices on isNewFormat ...
}
```
> **CRITICAL parser ambiguity:** spec §9.2 text says "< 11 = format lama" in one place and CONTEXT D-415-03 says "≤9 kolom = file lama; >9 = format baru". These disagree on the boundary (9 vs 11). **The CONTEXT decision (≤9 old / >9 new) is the locked authority.** Resolve: legacy template has exactly 9 columns; new template has 13. Any file with 10–13 columns is "new" (treat missing trailing as empty). Boundary check `colCount > 9`. Flag for planner: do NOT use the spec's stray "< 11" — use D-415-03's `>9`.

### Pattern 3: Cross-package count validation → per-Section (extend existing)
**What:** Current code validates total question count matches sibling packages (`:6823-6863`). Generalize to per-SectionNumber.
**When to use:** D-13 hard-block #1 at import.
**Example (current seam to extend):**
```csharp
// Source: Controllers/AssessmentAdminController.cs:6843-6861
// CURRENT: single total-count check vs referencePackage.Questions.Count
// EXTEND: group both incoming rows AND sibling package questions by SectionNumber,
//         compare counts per SectionNumber, build FULL mismatch list (never stop-at-first):
//   foreach sectionNumber in union(incoming, sibling):
//       if incomingCount[sn] != siblingCount[sn]:
//           errors.Add($"Section {sn}: Paket \"{me}\" punya {x} soal, Paket \"{sib}\" punya {y} soal (harus sama).");
//   if errors.Any() → return Json/redirect with FULL list, ZERO writes (atomic, like Inject D-09)
// SectionNumber=null → treated as one implicit "Lainnya" section (spec §15.A).
```

### Pattern 4: Inline Razor panel matching existing two-column layout
**What:** Add a `card shadow-sm` Section panel into the existing `row g-4` grid in `ManagePackageQuestions.cshtml`.
**When to use:** SEC-01/02 UI (D-415-01).
**Example:** The view already uses `<div class="row g-4">` with `col-lg-7` (question list) + (form column). Add the Section CRUD card in the form column or as a full-width card above the row. Toggles reuse the `form-check form-switch` precedent. (Full copy contract is in `415-UI-SPEC.md` — approved.)

### Anti-Patterns to Avoid
- **Trusting client-supplied column count for format detection** — read the actual worksheet header; never accept a hidden form field "isNewFormat". Server decides.
- **Retrofitting the ~180 shuffle tests / 17.5K xUnit lines** — spec §12 forbids it. Section-empty MUST equal old behavior; old tests stay green. Write a NEW focused suite.
- **Extracting IQuestionSequence with no consumer** — speculative abstraction (see decision below).
- **Cascade-delete on PackageQuestion.SectionId FK** — breaks the UI-SPEC promise; use SetNull.
- **Stop-at-first validation error** — D-415-04 mandates the COMPLETE mismatch list.
- **Global EF `HasQueryFilter` for sections** — never (matches Phase 409 lesson: per-query `.Where` only).

### IQuestionSequence Abstraction Decision (Claude's Discretion — spec §13)
**Recommendation: DO NOT extract `IQuestionSequence`/`SectionAwareQuestionProvider` in Phase 415.**

Rationale `[VERIFIED: codebase]`:
- The only consumer of question-ordering logic is `ShuffleEngine.BuildQuestionAssignment(List<AssessmentPackage>, bool, int, Random) → List<int>` (`Helpers/ShuffleEngine.cs:39`), called from `CMPController.StartExam:1074` and the reshuffle endpoints. **None of these are refactored in 415.**
- 415 only *stores* section data + section toggles; it does not *consume* them for ordering (that is 416 scoped-shuffle + 417 pagination).
- Extracting an abstraction now means: a new interface with one trivial passthrough implementation, zero behavioral change, and no test that exercises the section-aware path (since nothing reads it yet). That is a speculative seam — it will be re-shaped the moment 416 actually generalizes BuildQuestionAssignment to `BuildSectionQuestionAssignment`.
- **Better:** introduce the abstraction in **416** where `BuildSectionQuestionAssignment(section, allSiblingPackages, shuffleEnabled, workerIndex, rng)` is genuinely built (spec §6.2) and immediately tested. The "~23 touch-points / 50% blast-radius reduction" the spec cites are all in 416/417/419 — they pay for the abstraction there, not in 415.
- Risk of deferring: none for 415 (no consumer). The planner for 416 inherits a clean data model and can design the sequence abstraction against real requirements.

If the planner disagrees and wants the seam early, it should be a *pure* `Helpers/` static (matching `ShuffleEngine`/`QuestionOptionValidator` precedent), not a DI-injected service — but this research recommends against it.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| EF migration scaffolding | Hand-written SQL DDL | `dotnet ef migrations add AddAssessmentPackageSection` (local tool 8.0.0) | `[VERIFIED]` Snapshot consistency; pinned tool avoids 10.x stamp (Phase 409 lesson). |
| Excel read/write | Manual OpenXML | ClosedXML 0.105 `XLWorkbook` | `[VERIFIED]` Already used by both template + parser. |
| Option validation (min2/max6, correct-has-text) | Inline checks | `QuestionOptionValidator.ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)` | `[VERIFIED: Helpers/QuestionOptionValidator.cs:20]` Shared pure validator (kill-drift, Phase 386). Note: max-6 enforcement is 418's scope; 415 import widens A–F acceptance only. |
| Fingerprint dedup | New hashing | Extend `MakePackageFingerprint` (`:7081`) | `[VERIFIED]` Existing `NormalizePackageText` (whitespace+lowercase) reuse; just add OptE/OptF/SectionNumber to the join. |
| Pre→Post clone | New clone path | Extend `SyncPackagesToPost` (`:6366`) | `[VERIFIED]` Single transaction deep-clone already exists; add Section row clone + SectionId remap. **Audit ALL sync triggers** (spec §10.1 — do not assume trigger count). |
| Cross-package count guard | New validation | Extend existing total-count check (`:6823`) to per-Section | `[VERIFIED]` Sibling resolution (`Title+Category+Schedule.Date`) already implemented at `:6827`. |
| Toggle UI | New switch component | Bootstrap `form-check form-switch` (ManagePackages precedent) | `[VERIFIED: 415-UI-SPEC.md]` |

**Key insight:** This phase is almost entirely *extension of existing seams*, not new construction. The riskiest hand-roll temptation is re-implementing the parser as a separate "new-format" path — resist it (one parser, branch on column count). The second is the IQuestionSequence abstraction (defer to 416).

## Runtime State Inventory

> Phase 415 is primarily additive code/schema, but it DOES introduce a schema migration on a live DB. Inventory below.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | ~600 existing PackageQuestions across live packages; all will get `SectionId = NULL` on migration (backfill = null, additive). Existing AssessmentSessions/UserPackageAssignments unaffected. | **Migration only** (no data migration). SectionId=null = legacy behavior (D-05). |
| Live service config | None. No external service stores section structure. | None — verified: section is internal entity, no n8n/Datadog/etc. dependency. |
| OS-registered state | None. | None — verified: no scheduled tasks or OS registrations touch question schema. |
| Secrets/env vars | None. | None — verified: no env var references question/section schema. |
| Build artifacts | EF model snapshot `Migrations/ApplicationDbContextModelSnapshot.cs` regenerated by `migrations add`. `bin/`/`obj/` rebuilt by `dotnet build`. | **Commit the new migration + updated snapshot.** Verify snapshot ProductVersion stays `8.0.0` (local tool pin) — NOT 10.x (Phase 409 Pitfall). |

**The canonical migration concern:** After the migration is applied to a live DB with ~600 questions, every existing question has `SectionId = NULL` and no AssessmentPackageSection rows exist → 100% backward-compatible (spec §11). Locally: apply via `dotnet ef database update` against `localhost\SQLEXPRESS` / `HcPortalDB_Dev`, verify with `sqlcmd -C -I`, then notify IT with commit hash + **migration=TRUE** flag (CLAUDE.md step 5). Do NOT apply to Dev/Prod (IT's job).

## Common Pitfalls

### Pitfall 1: EF `dotnet-ef` version stamp (snapshot ProductVersion)
**What goes wrong:** Running global `dotnet ef` (10.x) stamps `ApplicationDbContextModelSnapshot.cs` with ProductVersion 10.0.x, polluting the migration chain (project is net8.0 / EF 8.0.0).
**Why it happens:** Global tool shadows project pin.
**How to avoid:** Use the pinned local tool — `.config/dotnet-tools.json` pins `dotnet-ef` to 8.0.0 (`rollForward: false`). Run `dotnet ef ...` **from repo root** so the local manifest resolves. `[VERIFIED: .config/dotnet-tools.json + Phase 409 SUMMARY]`
**Warning signs:** Snapshot diff shows `.HasAnnotation("ProductVersion", "10.0.x")`. Revert and re-run with local tool.

### Pitfall 2: Multiple cascade paths on PackageQuestions
**What goes wrong:** SQL Server rejects `migrations add`/`database update` with "may cause cycles or multiple cascade paths."
**Why it happens:** AssessmentPackage→Question is already Cascade (`:481`). If AssessmentPackageSection→Question were ALSO Cascade, two cascade paths reach PackageQuestions.
**How to avoid:** Set PackageQuestion.SectionId FK to **SetNull** (not Cascade). Only one cascade path (via AssessmentPackageId) remains. This ALSO satisfies the UI-SPEC delete-Section promise (questions become "Lainnya"). `[VERIFIED: ApplicationDbContext.cs:475-485 + design intent]`
**Warning signs:** Migration scaffold throws or `database update` errors on FK creation.

### Pitfall 3: Format auto-detect boundary mismatch (9 vs 11)
**What goes wrong:** Spec §9.2 says "< 11 = lama"; CONTEXT D-415-03 says "≤9 = lama / >9 = baru". Picking the wrong boundary breaks IMP-02 (legacy files) or mis-parses 10-col files.
**Why it happens:** Spec internal inconsistency (one of the §15 sweeps didn't fully reconcile §9.2).
**How to avoid:** **CONTEXT D-415-03 is authority: ≤9 old, >9 new.** Legacy = exactly 9 cols; new = up to 13. Detect via header row's `LastCellUsed().Address.ColumnNumber > 9`. `[CITED: 415-CONTEXT.md D-415-03]`
**Warning signs:** A valid old 9-col file gets parsed as new (columns shifted) → wrong options/answers.

### Pitfall 4: Blank trailing cells in data rows skew column detection
**What goes wrong:** Using a data row's `LastCellUsed()` to detect format: an Essay row (blank options) reports few columns even in a new-format file.
**Why it happens:** ClosedXML `RowsUsed()`/`LastCellUsed()` ignores trailing empties per-row.
**How to avoid:** Detect format from the **header row (row 1)**, not data rows. Header is always fully populated. `[VERIFIED: current parser reads header via Skip(1) but detects nothing today — add header-based detect]`
**Warning signs:** Mixed-type imports (Essay + MC) detected as different formats within one file.

### Pitfall 5: Correct-answer whitelist rejects new-format E/F answers
**What goes wrong:** New-format MA row with `Jawaban Benar = "A,C,E"` gets rejected because the current parser hard-codes `{"A","B","C","D"}` (`:6851`, `:6897`, `:6913`) + `ExtractPackageCorrectLetter` uses `"ABCD"` (`:7064`).
**Why it happens:** 415 import accepts Opsi A–F (D-415-03) but the answer whitelist is still A–D.
**How to avoid:** For 415, widen the import-path answer whitelist to A–F **at parse** so new-format files import. Storing a correct E/F option is safe (grading is `PackageOption.Id`-based, spec §5.3). Render/grading *display* of E/F is 418 — but the *data* (which option IsCorrect) must be stored correctly now. **See OPEN QUESTION O-1.** `[CITED: spec §15.C + §5.3]`
**Warning signs:** New-format import silently skips MA/MC rows with E/F answers.

### Pitfall 6: D-13 re-guard at StartExam without breaking legacy
**What goes wrong:** Adding a "sections must match across packages" guard at StartExam blocks legacy assessments (no sections) or single-package assessments.
**Why it happens:** Over-broad guard.
**How to avoid:** The re-guard only fires when sibling packages exist AND at least one has sections. SectionId-all-null → skip (legacy = pass). Single package → skip (no sibling to mismatch). Place BEFORE `ShuffleEngine.BuildQuestionAssignment` at `CMPController.cs:~1074`. `[VERIFIED: CMPController.cs:1074]`
**Warning signs:** Existing exams fail to start after deploy.

### Pitfall 7: Sync trigger audit (SEC-06) — do not assume count
**What goes wrong:** Only `CopyPackagesFromPre` is updated to clone sections; other sync triggers (CreateQuestion/EditQuestion/DeleteQuestion on a SamePackage PreTest, CreatePackage) silently produce sectionless Post packages.
**Why it happens:** `SyncPackagesToPost` is called from MULTIPLE sites (`:6439`, `:6480`, `:6565`, `:7274`, `:7537`, `:7630`). The deep-clone body (`:6366-6424`) is the single point to fix, but verify every caller path is covered by fixing the body.
**How to avoid:** Fix the `SyncPackagesToPost` body (`:6397-6418`) to clone Section rows + remap SectionId — since all callers route through it, all paths inherit the fix. **Verify, don't assume** (spec §10.1 / §15.G). `[VERIFIED: 6 call sites grepped this session]`
**Warning signs:** PostTest after sync has questions with SectionId pointing to Pre's section IDs (wrong) or null.

### Pitfall 8: SectionId remap during clone (FK integrity)
**What goes wrong:** Cloning sets Post question.SectionId to the **Pre** section's Id (which belongs to a different package) → FK violation or cross-package leak.
**Why it happens:** Naive `newQ.SectionId = q.SectionId`.
**How to avoid:** During `SyncPackagesToPost`, first clone Section rows for the new package, build a `Dictionary<int oldSectionId, AssessmentPackageSection newSection>` map (or map by SectionNumber within the package), then set `newQ.SectionId = map[q.SectionId].Id` (after SaveChanges assigns new IDs, or use navigation property so EF wires it). Cleanest: attach new Section objects to `newQ.Section` navigation and let EF assign FKs on save. `[ASSUMED — confirm EF navigation wiring at implement time]`

## Code Examples

### Section CRUD endpoint shape (NEW — matches existing controller conventions)
```csharp
// Source: convention from AssessmentAdminController.cs (RBAC + antiforgery + redirect pattern)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateSection(int packageId, int sectionNumber, string? name,
                                               bool startNewPage, bool shuffleEnabled)
{
    var pkg = await _context.AssessmentPackages.FindAsync(packageId);
    if (pkg == null) return NotFound();
    // unique (packageId, sectionNumber) — catch DbUpdateException or pre-check (Phase 404 lesson: filtered-unique → DbUpdateException)
    bool dup = await _context.Set<AssessmentPackageSection>()
        .AnyAsync(s => s.AssessmentPackageId == packageId && s.SectionNumber == sectionNumber);
    if (dup) { TempData["Error"] = $"No. Section {sectionNumber} sudah ada di paket ini."; 
               return RedirectToAction("ManagePackageQuestions", new { packageId }); }
    _context.Add(new AssessmentPackageSection {
        AssessmentPackageId = packageId, SectionNumber = sectionNumber,
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
        StartNewPage = startNewPage, ShuffleEnabled = shuffleEnabled });
    await _context.SaveChangesAsync();
    TempData["Success"] = "Section berhasil disimpan.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```

### Fingerprint extension (§15.C)
```csharp
// Source: AssessmentAdminController.cs:7081 (extend signature + body)
private static string MakePackageFingerprint(string q, string a, string b, string c, string d,
                                             string e, string f, int? sectionNumber)
    => string.Join("|||", new[] { q, a, b, c, d, e, f }.Select(NormalizePackageText)
         .Append((sectionNumber?.ToString() ?? "_NOSEC_")));
// Callers at :6721 (existing fingerprints) and :6922 (new rows) must pass e, f, sectionNumber.
// Legacy questions: e="", f="", sectionNumber=null → "_NOSEC_". Backward-compatible (same hash as before for A-D no-section IF you keep the join order — VERIFY: adding e/f/section changes hash for ALL, which is fine because both existing AND new use the new fn; dedup is within-import-session + against current package).
```
> **Subtle:** the existing-fingerprint set (`:6718-6727`) and the new-row fingerprint (`:6922`) must use the SAME extended function, so they remain comparable. Since both are recomputed with the new signature each import, dedup stays correct. `[VERIFIED: both call sites identified]`

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Flat question list, 4 fixed options A–D | Section-grouped questions, dynamic 2–6 options | v32.6 (this milestone) | 415 lays data foundation; 416–418 consume it. |
| Single total-count cross-package guard | Per-Section count guard (D-13) | Phase 415 | Stricter; enables scoped shuffle (416). |
| `MakePackageFingerprint(Q,A,B,C,D)` 5-arg | `(Q,A..F,SectionNumber)` 8-arg | Phase 415 | Prevents false dedup across sections / option counts. |

**Deprecated/outdated:** None removed in 415 (purely additive). The discrete `optionA..D`/`correctA..D` HTTP contract on CreateQuestion/EditQuestion (`:7124-7128`) stays in 415 — only a `sectionId` param is added. Its refactor to arrays is **Phase 418** (spec §8.2). Do not refactor it in 415.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | SQL Server accepts both FKs (Section→Package Cascade, Question→Section SetNull) without multiple-cascade-path error | Pattern 1 / Pitfall 2 | LOW — only one cascade path to PackageQuestions; SetNull is the standard mitigation. Confirmed by reasoning, verify at scaffold. |
| A2 | EF navigation-property wiring during SyncPackagesToPost clone correctly remaps SectionId to new package's section IDs | Pitfall 8 | MED — naive Id copy would cross-link packages. Must build old→new section map. |
| A3 | 415 import path should WIDEN correct-answer whitelist to A–F (store correct E/F option) while render/grading display stays 418 | Pitfall 5 / O-1 | MED — if 415 keeps A–D whitelist, new-format files with E/F answers are un-importable, defeating IMP-01. Resolve via O-1. |
| A4 | Snapshot ProductVersion stays 8.0.0 via local tool pin | Runtime State Inventory | LOW — pin verified; just must run from repo root. |

## Open Questions

1. **O-1: Does 415 import accept correct-answer letters E/F (store correct E/F option), or only store option *text* for E/F with answers limited to A–D?**
   - What we know: CONTEXT D-415-03 says "import menerima [Opsi A–F] + data model simpan"; grading huruf A–F = Phase 418. Data model (PackageOption.Id-based grading, spec §5.3) safely supports a correct E/F option.
   - What's unclear: Whether a new-format MA row `Jawaban Benar = "A,E"` should be accepted+stored (E marked IsCorrect) in 415, or rejected until 418.
   - Recommendation: **Accept + store A–F correct answers in 415** (widen whitelist at parse). Rejecting E/F answers makes new-format files un-importable, contradicting IMP-01. Storing a correct E option is data-only and safe; the *display/grading of letter E* is render-layer (418) but the underlying IsCorrect flag must be right now. Planner to confirm with user if ambiguity remains — this is the single decision that could change import scope.

2. **O-2: Section auto-creation from import — when No.Section appears but no AssessmentPackageSection row exists, does import auto-create the Section row (with default toggles StartNewPage=false, ShuffleEnabled=true)?**
   - What we know: spec §15.D "Section bisa lahir dari Excel (otomatis dari kolom No.Section)... toggle hanya diatur di UI (Excel tak bawa toggle)." CONTEXT code_context line 86: "Import → auto-buat record Section dari No.Section/Nama saat commit."
   - What's unclear: Behavior when Nama Section differs across rows with the same No.Section (last-wins? first-wins? error?).
   - Recommendation: Auto-create Section row on first sight of a No.Section; if a row repeats the same No.Section with a *different* Nama, take first-non-empty Nama (or warn). Toggles default (false/true). Confirm tiebreak rule with planner.

3. **O-3: Where exactly does the import write its result — current code redirects with TempData (`:7056`), but D-415-04 demands a FULL error list rendered.**
   - What we know: Current import returns `Json(new { success=false, errors })` in one branch (`:6753`) and `RedirectToAction` + TempData in others. UI-SPEC says error list renders on `ImportPackageQuestions.cshtml` post-back (server-rendered alert blocks, TempData pattern).
   - Recommendation: Render the full mismatch list via TempData (a `List<string>` or serialized list) consumed by the view's existing alert block, OR return the existing JSON shape if the import is AJAX. Confirm whether ImportPackageQuestions submit is a full form POST (redirect) or AJAX (JSON) — current view uses form POST. Keep the redirect+TempData full-list pattern for consistency.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test/run | ✓ (assumed — project builds in prior phases) | net8.0 | — |
| SQL Server (localhost\SQLEXPRESS) | migration apply + Integration tests | ✓ (used through Phase 413) | SQL Server 2025 (per STATE.md) | InMemory for non-ExecuteUpdate tests |
| dotnet-ef local tool | migration scaffold/apply | ✓ | 8.0.0 (pinned) | — |
| ClosedXML | template + parse | ✓ | 0.105.0 | — |
| HcPortalDB_Dev | local DB target | ✓ | — | — |
| Playwright | UAT (deferred to Phase 419 per roadmap; 415 is data/admin, optional smoke) | ✓ (used Phase 413) | — | xUnit + manual browser check |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None blocking. Note: per lesson 354, any Razor/JS in the Section panel SHOULD get a real-browser check, but heavy Playwright UAT is roadmapped to Phase 419; 415 minimal-JS (D-415-02) lowers that risk.

## Validation Architecture

> nyquist_validation = true (`.planning/config.json`). Section included.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (`HcPortal.Tests/HcPortal.Tests.csproj`) |
| Config file | none (xunit auto-discovery); InMemory EF 8.0.0 for fast tests, disposable SQLEXPRESS `HcPortalDB_Test_{guid}` for Integration |
| Quick run command | `dotnet test --filter "Category!=Integration"` (fast, InMemory only) |
| Full suite command | `dotnet test` (includes `[Trait("Category","Integration")]` SQLEXPRESS write-path) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SEC-01 | Create/Edit/Delete Section; unique (pkg, sectionNumber) enforced; delete sets questions SectionId=null | integration (real-SQL, FK SetNull + unique index) | `dotnet test --filter "FullyQualifiedName~SectionCrud"` | ❌ Wave 0 |
| SEC-02 | Toggle StartNewPage/ShuffleEnabled persist; "semua section new page" sets all true | unit/integration | `dotnet test --filter "FullyQualifiedName~SectionToggle"` | ❌ Wave 0 |
| SEC-03 | Assign question→Section via sectionId; null = "Lainnya"; grouping order | integration | `dotnet test --filter "FullyQualifiedName~SectionAssign"` | ❌ Wave 0 |
| SEC-04 | Hard-block import + StartExam re-guard on per-Section count mismatch; full list; legacy/single-pkg pass | unit (count-compare pure) + integration (StartExam guard) | `dotnet test --filter "FullyQualifiedName~SectionMismatchGuard"` | ❌ Wave 0 |
| SEC-05 | Question list grouped by Section header + "Lainnya" trailing | manual/UAT (Razor render) | manual browser @5277 (D-415-01 inline panel) | ❌ Wave 0 (no automated Razor render test infra) |
| SEC-06 | Sync Pre→Post clones Section rows + remaps SectionId + options 5–6; all sync triggers covered | integration (real-SQL deep-clone) | `dotnet test --filter "FullyQualifiedName~SectionSyncPrePost"` | ❌ Wave 0 |
| IMP-01 | New 13-col template generated; parser reads No.Section/Nama/Opsi A–F | unit (ClosedXML build+parse roundtrip) | `dotnet test --filter "FullyQualifiedName~ImportSectionFormat"` | ❌ Wave 0 |
| IMP-02 | Legacy 9-col file imports (SectionId=null, A–D, E/F empty) | unit (parse 9-col fixture) | `dotnet test --filter "FullyQualifiedName~ImportBackwardCompat"` | ❌ Wave 0 |
| IMP-03 | Per-Section count validation (reject) + fingerprint includes E/F+SectionNumber (no false dedup) | unit (fingerprint) + unit/integration (count) | `dotnet test --filter "FullyQualifiedName~ImportFingerprint\|FullyQualifiedName~ImportSectionCount"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (fast InMemory + pure-logic; <30s) — MUST stay green incl. all ~600+ legacy tests (backward-compat invariant).
- **Per wave merge:** `dotnet test` (full, incl. Integration SQLEXPRESS) — confirms no regression in shuffle/import/sync suites.
- **Phase gate:** Full suite green + `dotnet build` 0-error + `dotnet run` @5277 boots + migration applied & verified via `sqlcmd -C -I` before `/gsd-verify-work`. Per lesson 354, a real-browser check of the Section panel (SEC-05 grouping + dropdown + toggles render) is recommended even though heavy Playwright UAT is roadmapped to 419.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/SectionCrudTests.cs` — covers SEC-01 (unique index, FK SetNull on delete). Integration (real-SQL needed for index/FK semantics).
- [ ] `HcPortal.Tests/SectionImportTests.cs` — covers IMP-01/02/03 (template roundtrip, dual-format parse, fingerprint, per-Section count). Mostly InMemory (parser/fingerprint are pure); count-validation can be InMemory.
- [ ] `HcPortal.Tests/SectionSyncPrePostTests.cs` — covers SEC-06 (deep-clone remap). Integration (SQLEXPRESS; verifies FK + new IDs).
- [ ] `HcPortal.Tests/SectionMismatchGuardTests.cs` — covers SEC-04 (import reject full-list + StartExam re-guard; legacy/single-pkg pass). Pure count-compare = InMemory; StartExam path = Integration.
- [ ] Shared fixture: reuse the established disposable-SQLEXPRESS fixture pattern (e.g. `FlexibleParticipantAddFixture` / `InjectAssessmentFixture` precedent — `IClassFixture<T>`, `HcPortalDB_Test_{guid}`, `[Trait("Category","Integration")]`). Build a `SectionFixture` mirroring it, OR reuse an existing one if its seed suffices.
- [ ] **Backward-compat guard:** confirm `dotnet test --filter "FullyQualifiedName~Shuffle"` (~180 methods) stays green after Section columns added — Section-empty = old behavior (spec §12). This is the keystone invariant; no new test, just MUST stay green.

*(Framework already installed — no install step needed.)*

## Sources

### Primary (HIGH confidence — verified against live code this session)
- `Models/AssessmentPackage.cs` (read) — PackageQuestion/PackageOption shape; no Letter field (grading by Id). Confirms §5.3.
- `Helpers/QuestionOptionValidator.cs:20` (read) — `ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)` exact signature. Confirms §8.1/§15.G.
- `Controllers/AssessmentAdminController.cs` (read) — `DownloadQuestionTemplate:6599`, `ImportPackageQuestions:6690` (parser body :6755-6772, count-validation :6823-6863, persist :6930-6960), `ExtractPackageCorrectLetter:7060` (`"ABCD"`), `MakePackageFingerprint:7081` (5-arg), `SyncPackagesToPost:6366` (deep-clone body :6397-6418, 6 call sites), `CreateQuestion:7116` (discrete optionA..D contract), `EditQuestion:7333`, `ManagePackageQuestions:7090`.
- `Data/ApplicationDbContext.cs` (read) — `DbSet<AssessmentPackage>:55`, Fluent config for package family :466-496 (cascade rules), HasIndex precedents.
- `Controllers/CMPController.cs` (grep) — `StartExam` guard `IsParticipantRemoved:373`, `BuildQuestionAssignment` call `:1074` (D-13 re-guard seam).
- `Helpers/ShuffleEngine.cs:39` (grep) — `BuildQuestionAssignment(List<AssessmentPackage>, bool, int, Random) → List<int>` (sole consumer; basis for IQuestionSequence decision).
- `HcPortal.csproj` + `HcPortal.Tests.csproj` (read) — net8.0, EF 8.0.0, ClosedXML 0.105.0, xunit 2.9.3, EF InMemory 8.0.0.
- `.config/dotnet-tools.json` (read) — dotnet-ef 8.0.0 pinned, rollForward:false.
- `Migrations/` listing (bash) — latest = `AddParticipantRemovalColumns` (Phase 409); migration chain baseline.
- `Views/Admin/ManagePackageQuestions.cshtml` (read) — `row g-4` / `col-lg-7` / `card shadow-sm` layout; inline panel feasibility confirmed.
- `415-CONTEXT.md`, `415-UI-SPEC.md`, `.planning/REQUIREMENTS.md`, `.planning/STATE.md`, `CLAUDE.md` (read) — locked decisions, REQ mapping, workflow.

### Secondary (HIGH-MEDIUM)
- `docs/superpowers/specs/2026-06-22-section-...-design.md` (read in full) — §3 (D-01..15), §5 (model), §6 (shuffle, for 416 context), §9 (import), §11 (migration), §12 (testing), §13 (phasing + abstraction), §15.A-G (re-check corrections). PRIMARY design source; encodes 2 prior code sweeps.
- `HcPortal.Tests/InjectExcelImportTests.cs` (read) — established Integration test pattern (`IClassFixture`, disposable SQLEXPRESS, `[Trait Category=Integration]`, ClosedXML roundtrip).

### Tertiary (LOW — none)
- No web sources needed; this is internal-codebase + spec-driven research.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions read directly from project files; zero new deps.
- Architecture/data model: HIGH — entity shape, FK behavior, and migration pattern verified against 2 prior precedents (352/409) + live ApplicationDbContext config.
- Import/parser/fingerprint: HIGH — exact line refs + signatures confirmed; 2 ambiguities surfaced as Open Questions (O-1 answer-letter scope, format boundary 9-vs-11 resolved to D-415-03).
- Sync (SEC-06): MEDIUM-HIGH — clone body + all 6 call sites identified; SectionId remap mechanism flagged as A2 (verify EF nav wiring at implement).
- Pitfalls: HIGH — derived from verified code + documented prior-phase lessons (409 EF pin, 404 filtered-unique→DbUpdateException).
- IQuestionSequence decision: HIGH — sole consumer confirmed not refactored until 416.

**Research date:** 2026-06-22
**Valid until:** ~2026-07-22 (stable internal codebase; line refs may drift if AssessmentAdminController.cs is edited by an intervening phase — re-grep symbols, not line numbers, at plan time).
