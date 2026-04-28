# Phase 305: Question Type Naming Clarity - Research

**Researched:** 2026-04-28
**Domain:** ASP.NET Core MVC view-label refactor + C# static helper extraction + cross-cutting docs sed
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Wording final (D-01):**
- MC = `"Single Choice"` (override ROADMAP "Pilihan Tunggal" — final adalah CONTEXT.md)
- MA = `"Multiple Answers"`
- Essay = `"Essay"` (tidak berubah)
- Rasional: standar Moodle/Canvas LMS, pure English

**Long vs short form (D-02):**
- Long form (dropdown form admin): `"Single Choice (1 jawaban benar)"` / `"Multiple Answers (≥2 jawaban benar)"` / `"Essay"`
- Short form (badge tabel/exam/preview): `"Single Choice"` / `"Multiple Answers"` / `"Essay"`
- Hybrid bahasa: label tipe Inggris, hint numerik Indonesia.

**Helper class extraction (D-03 D-04 D-05 D-06):**
- NEW file `Models/QuestionTypeLabels.cs` — static class dengan 3 method:
  - `Long(string type)` → long form
  - `Short(string type)` → short form
  - `BadgeClass(string type)` → Bootstrap badge class
- Default fallback (null/unknown) = `"Single Choice"` / `"bg-secondary"`
- Namespace `HcPortal.Models` (mengikuti `AssessmentConstants.cs`)
- View Razor: `@QuestionTypeLabels.Short(qtype)`, `@QuestionTypeLabels.BadgeClass(qtype)`
- Controller C#: `QuestionTypeLabels.Short("MultipleChoice")` untuk flash error

**View File Updates (D-07 D-08 D-09 D-10):**
- D-07 `ManagePackageQuestions.cshtml`:
  - Line 69-70 (badge logic): replace inline ternary dengan helper call
  - Lines 133-135 (dropdown options): update text ke long form. **Value attribute (`MultipleChoice`/`MultipleAnswer`/`Essay`) tetap**
- D-08 `_PreviewQuestion.cshtml`:
  - Line 6-7 (badge logic): replace inline ternary dengan helper call
  - Sisanya (radio/checkbox switch, Essay textarea) tidak berubah
- D-09 `StartExam.cshtml`:
  - Lines 100-106 (badge inline soal): tambah badge MC juga (asimetris → simetris)
  - Pakai `@QuestionTypeLabels.Short(qtype)` + `@QuestionTypeLabels.BadgeClass(qtype)`
- D-10 `ExamSummary.cshtml` [SCOPE EXTENSION]: Tambah badge tipe soal di kolom "Pertanyaan" (sebelumnya tidak ada). Append badge sebelum `@item.QuestionText` di line 53

**Controller & Import (D-11 D-12):**
- D-11 `AssessmentAdminController.cs` lines 4688, 4693, 4829, 4834 — flash error hybrid English+Indonesian:
  - Output: `"Single Choice hanya boleh memiliki 1 jawaban benar."` / `"Multiple Answers membutuhkan minimal 2 jawaban benar."`
- D-12 `ImportPackageQuestions.cshtml`:
  - Lines 38-43: button "Template MC" → "Template Single Choice"; "Template MA" → "Template Multiple Answers"
  - Line 32-35 bullet helper: tetap pakai enum value (developer-facing). Optional tambah catatan label baru — planner decide

**Documentation Updates (D-13 D-14 D-15):**
- D-13: 8 file HTML/MD/PY untuk sed-replace (context-by-context, bukan blind sed)
- D-14: PDF panduan + screenshot training → deferred manual user
- D-15: E2E Playwright tests `tests/e2e/` → ZERO match verified, tidak update

**Worker UX (D-16):**
- Tambah badge MC di `StartExam.cshtml` — visual hierarchy konsisten
- Badge order: `<span class="badge bg-primary">DisplayNumber</span> <span class="badge {BadgeClass} ms-1 small">{Short}</span> {QuestionText}`

**Validation & Behavior Guards (D-17 D-18 D-19 D-20):**
- D-17: Internal enum value `"MultipleChoice"`/`"MultipleAnswer"`/`"Essay"` di DB column TIDAK berubah
- D-18: Backward compat — Excel import file lama tetap berfungsi
- D-19: JS handler di `ManagePackageQuestions.cshtml` (lines 297-311, 356, 393-394) baca enum value via `data-question-type` atau dropdown.value — JS TIDAK berubah
- D-20: Planner tambah task DB query verifikasi `SELECT DISTINCT QuestionType FROM PackageQuestions`

### Claude's Discretion

- Detail urutan badge di `StartExam.cshtml` (apakah `me-2`/`ms-1`/`small`) — pilih konsisten dengan existing styling
- Implementasi `QuestionTypeLabels.cs` — `switch` expression vs `Dictionary<string,string>` lookup vs `if/else` — switch expression rekomendasi (C# 8+, sudah dipakai di codebase)
- Apakah `BadgeClass` return string termasuk `text-dark` atau split jadi `TextClass(type)` — pilih satu
- Wording bullet `ImportPackageQuestions` optional addition — tambah catatan label baru atau biarkan
- Order/flow update file — atomic commit per file vs single commit, atau group by file type — phase minimal-risk, planner pilih granularity

### Deferred Ideas (OUT OF SCOPE)

1. PDF panduan regenerasi — manual user task
2. Screenshot training material regenerasi — manual user task
3. Multi-language i18n / Localization — defer ke milestone v16+
4. Tooltip transisi "MC = Single Choice (dulu Pilihan Ganda)" — out per success criteria, atomic swap
5. Excel import template binary regenerate (`.xlsx`) — file binary tidak diubah
6. Rename enum value `MultipleChoice` → `SingleChoice` — Out per REQUIREMENTS Out of Scope
7. Tooltip / info-icon di label dropdown form — Out of Scope, long form parentheses sudah cukup
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| LBL-01 | User-facing label untuk tipe soal MC dan MA dirubah agar tidak rancu di form admin (`ManagePackageQuestions.cshtml`), preview (`_PreviewQuestion.cshtml`), exam (`StartExam.cshtml`), dan summary (`ExamSummary.cshtml`). Internal enum/string DB tidak diubah. | Verified: 5 view files yang reference `MultipleChoice`/`MultipleAnswer` ditemukan via grep — semua sudah masuk scope CONTEXT.md (D-07..D-10, D-12). Helper class `QuestionTypeLabels.cs` (NEW) di Models/ dengan namespace `HcPortal.Models` (verified `Views/_ViewImports.cshtml` line 2 sudah `@using HcPortal.Models` global — auto-accessible di semua view). Controller flash error 4 lokasi (lines 4688, 4693, 4829, 4834) verified. JS handler (lines 276, 297-311, 356, 393-394) baca enum value (`'MultipleChoice'`/`'MultipleAnswer'`/`'Essay'`) — TIDAK terdampak label change. DB enum unchanged (D-17 lock). |

**Project requirement reference:** `.planning/REQUIREMENTS.md` line 21 (LBL-01 maps Temuan 7 audit 27 April 2026).
</phase_requirements>

## Project Constraints (from CLAUDE.md)

`./CLAUDE.md` (root) hanya 1 directive: **"Always respond in Bahasa Indonesia."**

**Application:** Output prose user-facing (RESEARCH.md, PLAN.md, SUMMARY.md, kalimat tugas) di Bahasa Indonesia. Identifier kode (class names, method names, variable, string literal C#) tetap literal. UI label & error messages mengikuti hybrid bahasa (D-01, D-11): label tipe English (`"Single Choice"` / `"Multiple Answers"`), kalimat instruksi/hint Indonesia (`"hanya boleh memiliki 1 jawaban benar"`).

**No `.claude/skills/` atau `.agents/skills/` directory** ditemukan di repository. Tidak ada skill loading needed.

## Summary

Phase 305 adalah UI-label-polish phase dengan 1 helper-class extraction sebagai single source of truth, mempengaruhi 5 view file + 1 controller + 8 dokumentasi cross-cutting. Risk profile: **low** untuk view edits + kontrak DOM dan JS handler tidak berubah; **medium** untuk dokumentasi cross-cutting (15 occurrences di 7 dokumen `wwwroot/` + 2 di 1 dokumen `docs/` — total 17 — hasil grep menyeluruh, lebih banyak 3 dari klaim CONTEXT.md "14 occurrences").

Pattern parallel dengan Phase 304 (just shipped 2026-04-28) — same milestone v15.0 Wave 1, same UI-label-polish category, same minimal-DOM-change ethos. Deviasi terhadap Phase 304: **extract C# helper class** karena Phase 305 menyentuh 6 caller (3 badge view + 1 dropdown + 1 controller flash + 1 import button) sehingga DRY justified vs Phase 304 yang hanya 2 caller. Helper class pattern sudah ada di codebase (`Models/AssessmentConstants.cs`) — tinggal replikasi style yang sudah established.

**Primary recommendation:** Implementasi switch expression style di `Models/QuestionTypeLabels.cs` (C# 12 / .NET 8.0), 3-method API (`Long`/`Short`/`BadgeClass`), namespace `HcPortal.Models`. Update 5 view file inline ternary → helper call, 4 controller error message lokasi, 6 button/badge label lokasi, dan 8 dokumentasi (7 di wwwroot + 1 di docs) dengan context-aware sed. PDF & screenshot regen flagged sebagai deferred manual task.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Label translation (enum → user-facing string) | Models (C# static helper) | — | Pure stateless mapping; semantically tied ke property `QuestionType` di domain models. Consumed langsung dari Razor views (server-render) dan controllers (flash msg). |
| Badge class derivation (enum → Bootstrap class) | Models (C# static helper) | View | Same helper class, separate method. View hanya rendering — class lookup di server side untuk consistency. |
| Dropdown option text (form admin) | View Razor (server-render) | Models (helper) | Razor `<option>` text dirender server-side. Value attribute (enum) tetap (DB binding). |
| Flash error message (validation) | Controller (server) | Models (helper) | TempData injected by controller, helper provides label segment. |
| Button label (Excel template download) | View Razor (static text) | — | Static button text — tidak butuh helper, hard-code di view. |
| JS handler logic (qtype switch) | Browser (client JS) | — | Reads `<option value>` (enum, unchanged). Display label irrelevant to JS — pure data binding. |
| Documentation rendering | Static HTML/MD/PY files | — | Standalone non-Razor docs di `wwwroot/documents/` dan `docs/`. Manual sed-replace per occurrence; tidak terkait helper. |

**Key insight:** Tier separation membantu memastikan **value vs display** tidak tertukar. Enum value `"MultipleChoice"` adalah data layer concern (DB column, EF property, JS dropdown value, Excel kolom QuestionType). Display label `"Single Choice"` adalah presentation layer concern (badge text, dropdown option text, error message segment). Helper class adalah **mapping function** antara dua concern, tidak boleh leak ke arah berlawanan (mis. mengganti enum value).

## Standard Stack

### Core (No new dependencies)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET | 8.0 LTS | Target framework C# 12 | Existing `[VERIFIED: HcPortal.csproj line 4 — net8.0]` — switch expressions, target-typed new, file-scoped namespaces semua available |
| ASP.NET Core MVC | 8.0 | Razor view rendering, controller pipeline | Existing stack `[VERIFIED: STACK.md line 28]` |
| Bootstrap | 5.3 | `bg-primary`, `bg-secondary`, `bg-info text-dark` badge utility classes | Existing `[VERIFIED: ManagePackageQuestions.cshtml line 79 — `class="badge @badgeClass small"`]`. Bootstrap 5.3 confirmed via Login.cshtml CDN (Phase 304 verification report). |
| Bootstrap Icons | 5.x | Already loaded — tidak butuh tambahan | `bi-info-circle`, `bi-tag`, dll sudah dipakai di views target |

### Supporting (No new dependencies)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Razor TagHelper | 8.0 | `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` | Existing `[VERIFIED: Views/_ViewImports.cshtml line 3]` — sudah aktif untuk semua views |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Static class + switch expression | `Dictionary<string, (string Long, string Short, string BadgeClass)>` static readonly | Dictionary OK tapi lebih verbose untuk 3 keys × 3 methods. Switch expression lebih idiomatic di codebase (Phase 12.0 controller refactor sudah pakai). REJECTED. |
| `QuestionTypeLabels.cs` di `Models/` | `Helpers/QuestionTypeLabelHelper.cs` di `Helpers/` | CONVENTIONS.md line 13 menyatakan helper PascalCase + `Helper` suffix di `Helpers/`. Tetapi `Models/AssessmentConstants.cs` adalah precedent kontra (static class di Models). User explicit (D-06) pilih `Models/` karena semantically domain-related. REJECTED `Helpers/`. |
| `IStringLocalizer` / resource files | Pure static class | i18n butuh resource files + locale switcher + DI; over-engineering untuk single-language UI saat ini. REJECTED — defer ke milestone v16+ per CONTEXT.md deferred #3. |
| Razor `@functions { ... }` block per view | C# static helper class | Per-view duplication = 6 callers × duplicate code = anti-DRY. Helper class central source. REJECTED. |

**Installation:** Tidak ada NuGet package install. Tidak ada npm install. Pure C# + Razor edits.

**Version verification:**
```bash
# Confirmed via .csproj inspection
TargetFramework: net8.0  [VERIFIED: HcPortal.csproj line 4]
Nullable: enable          [VERIFIED: HcPortal.csproj line 5]
ImplicitUsings: enable    [VERIFIED: HcPortal.csproj line 6]
```

C# 12 default for .NET 8.0 — switch expressions (C# 8+), records, target-typed new (C# 9+), file-scoped namespaces (C# 10+) semua available. `[VERIFIED: docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12 - matches .NET 8.0]`

## Architecture Patterns

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          PHASE 305 DATA FLOW                            │
└─────────────────────────────────────────────────────────────────────────┘

[DB column PackageQuestions.QuestionType]   ← unchanged: "MultipleChoice"/"MultipleAnswer"/"Essay"
                │
                │ EF Core read
                ▼
[Model property PackageQuestion.QuestionType: string?]
                │
                ├──────────────────────────────────┐
                │                                  │
                ▼                                  ▼
[Razor View Layer]                      [Controller Action]
  ManagePackageQuestions.cshtml             AssessmentAdminController
  _PreviewQuestion.cshtml                     ├─ CreateQuestion (line 4685+)
  StartExam.cshtml                            └─ EditQuestion (line 4825+)
  ExamSummary.cshtml                              │
  ImportPackageQuestions.cshtml                   ▼
       │                                  TempData["Error"] = $"{...}"
       │ inline ternary REPLACED with         │
       ▼                                      ▼
       └──────────► [QuestionTypeLabels static helper] ◄────────────┐
                          │                                          │
                          │ switch expression                        │
                          │   "MultipleChoice" → "Single Choice"     │
                          │   "MultipleAnswer" → "Multiple Answers"  │
                          │   "Essay"          → "Essay"             │
                          │                                          │
                          ├─→ Long(type)        (long form)          │
                          ├─→ Short(type)       (short form)         │
                          └─→ BadgeClass(type)  (CSS class)          │
                                                                     │
                          ▲                                          │
                          │ called from controller (D-11)            │
                          │                                          │
                          [Flash error message] ─────────────────────┘
                                │
                                ▼
                      [TempData["Error"]] → next request render
                                                │
                                                ▼
                                    Browser sees alert-danger
                                    "Single Choice hanya boleh
                                     memiliki 1 jawaban benar."

[Browser DOM (Worker/Admin UI)]
  Badge: <span class="badge bg-secondary small">Single Choice</span>
  Dropdown: <option value="MultipleChoice">Single Choice (1 jawaban benar)</option>
                                  │ ▲
                                  │ │
                                  ▼ │
                              [JS handler unchanged]
                              applyQTypeSwitch(this.value)  // reads enum value
                                                            // value === "MultipleChoice" / etc

[Documentation Layer (static, NON-Razor)]
  wwwroot/documents/*.html, *.md, *.py        ← context-aware sed-replace
  docs/Persiapan-Test-Manual-Assessment.html  ← context-aware sed-replace
                                              ↑
                                              │ (manual edit per occurrence,
                                              │  not via helper — these files
                                              │  don't render Razor)
```

### Component Responsibilities

| File | Layer | Phase 305 Change |
|------|-------|------------------|
| `Models/QuestionTypeLabels.cs` | Domain helper (NEW) | CREATE: 3 static methods (Long/Short/BadgeClass) |
| `Models/AssessmentConstants.cs` | Domain helper | NO CHANGE — pattern reference only |
| `Views/Admin/ManagePackageQuestions.cshtml` | View | Edit lines 69-70 (badge), 133-135 (dropdown options) |
| `Views/Admin/_PreviewQuestion.cshtml` | View partial | Edit lines 6-7 (badge logic) |
| `Views/CMP/StartExam.cshtml` | View | Edit lines 100-106 (badge inline soal — add MC) |
| `Views/CMP/ExamSummary.cshtml` | View | Edit line 53 area — append badge sebelum QuestionText (SCOPE EXTENSION) |
| `Views/Admin/ImportPackageQuestions.cshtml` | View | Edit lines 38-43 (button text "Template Single Choice"/"Template Multiple Answers") |
| `Controllers/AssessmentAdminController.cs` | Controller | Edit lines 4688, 4693, 4829, 4834 (TempData error msg) |
| `Views/_ViewImports.cshtml` | Razor global | NO CHANGE (already has `@using HcPortal.Models`) |
| `Views/CMP/_ViewImports.cshtml` (if exists) | Razor sub-area | Check existence — if exists, verify inheritance |
| `wwwroot/documents/...` (7 files) | Static doc | sed-replace context-aware |
| `docs/Persiapan-Test-Manual-Assessment.html` | Static doc | sed-replace context-aware |

### Recommended Project Structure

```
Models/
├── QuestionTypeLabels.cs       # NEW — static helper, 3 methods
├── AssessmentConstants.cs      # existing — pattern reference
├── AssessmentPackage.cs        # existing — has QuestionType property
├── PackageQuestion.cs          # existing — has QuestionType property
├── PackageExamViewModel.cs     # existing — has QuestionType property
└── AnalyticsDashboardViewModel.cs  # existing — has QuestionType property
```

### Pattern 1: Static helper class with switch expression (C# 12 idiomatic)

**What:** Pure functional mapping enum string → display string, no state, no DI.

**When to use:** ≥3 callers need same mapping (DRY threshold reached). Centralize untuk future relabel.

**Example:**

```csharp
// Source: Models/QuestionTypeLabels.cs (NEW — pattern from Models/AssessmentConstants.cs)
namespace HcPortal.Models
{
    public static class QuestionTypeLabels
    {
        public static string Long(string? type) => type switch
        {
            "MultipleChoice" => "Single Choice (1 jawaban benar)",
            "MultipleAnswer" => "Multiple Answers (≥2 jawaban benar)",
            "Essay"          => "Essay",
            _                => "Single Choice (1 jawaban benar)"  // D-05 fallback
        };

        public static string Short(string? type) => type switch
        {
            "MultipleChoice" => "Single Choice",
            "MultipleAnswer" => "Multiple Answers",
            "Essay"          => "Essay",
            _                => "Single Choice"  // D-05 fallback
        };

        public static string BadgeClass(string? type) => type switch
        {
            "MultipleChoice" => "bg-secondary",
            "MultipleAnswer" => "bg-primary",
            "Essay"          => "bg-info text-dark",
            _                => "bg-secondary"  // D-05 fallback
        };
    }
}
```

**Notes:**
- Param `string?` (nullable) untuk match property `QuestionType` yang nullable di model.
- `_` discard pattern catches null AND unknown values — single defensive default.
- File-scoped vs block-scoped namespace: codebase mix (CONVENTIONS.md line 30-31). Pattern existing `Models/AssessmentConstants.cs` pakai block-scoped → replikasi block-scoped untuk consistency.

### Pattern 2: Razor view inline replacement

**What:** Replace inline ternary expression dengan helper call, satu baris.

**Example before (ManagePackageQuestions.cshtml line 68-70):**

```razor
@foreach (var q in questions)
{
    var qtype = q.QuestionType ?? "MultipleChoice";
    var badgeClass = qtype == "MultipleAnswer" ? "bg-primary" : (qtype == "Essay" ? "bg-info text-dark" : "bg-secondary");
    var badgeLabel = qtype == "MultipleAnswer" ? "Multi Jawaban" : (qtype == "Essay" ? "Essay" : "Pilihan Ganda");
    ...
    <span class="badge @badgeClass small">@badgeLabel</span>
}
```

**Example after:**

```razor
@foreach (var q in questions)
{
    var qtype = q.QuestionType ?? "MultipleChoice";
    ...
    <span class="badge @QuestionTypeLabels.BadgeClass(qtype) small">@QuestionTypeLabels.Short(qtype)</span>
}
```

**Notes:**
- 2 local var (`badgeClass`, `badgeLabel`) bisa dihapus, atau tetap dipertahankan jika dipakai berulang dalam loop block. Phase 305 — hapus karena hanya 1 use per iteration.
- Fallback `qtype ?? "MultipleChoice"` tetap (existing pattern), helper sendiri sudah punya `_ => "Single Choice"` — double safety.

### Pattern 3: StartExam badge symmetry add (D-09 D-16)

**Before (StartExam.cshtml lines 101-106):**

```razor
<p class="fw-bold mb-3">
    <span class="badge bg-primary me-2">@q.DisplayNumber</span>
    @if (qtype == "MultipleAnswer") {
        <span class="badge bg-secondary ms-1 small">Multi Jawaban</span>
    } else if (qtype == "Essay") {
        <span class="badge bg-secondary ms-1 small">Essay</span>
    }
    @q.QuestionText
</p>
```

**After (asimetris → simetris, semua tipe punya badge):**

```razor
<p class="fw-bold mb-3">
    <span class="badge bg-primary me-2">@q.DisplayNumber</span>
    <span class="badge @QuestionTypeLabels.BadgeClass(qtype) ms-1 small">@QuestionTypeLabels.Short(qtype)</span>
    @q.QuestionText
</p>
```

**Notes:**
- MA dulu pakai `bg-secondary` (line 103), helper map MA→`bg-primary`. Diff: MA badge color berubah dari grey → blue. Konfirm sebagai bagian dari simetrisasi (Manage badge MA = bg-primary; sekarang StartExam align).
- Essay dulu `bg-secondary`, helper map Essay→`bg-info text-dark`. Diff sama — align dengan Manage tabel.

### Pattern 4: ExamSummary badge add (D-10 SCOPE EXTENSION)

**Before (ExamSummary.cshtml line 51-54):**

```razor
<td>
    <span class="text-truncate d-inline-block" style="max-width: 420px;"
          title="@item.QuestionText">
        @item.QuestionText
    </span>
</td>
```

**After (append badge sebelum text):**

```razor
<td>
    <span class="badge @QuestionTypeLabels.BadgeClass(item.QuestionType) ms-1 small me-2">@QuestionTypeLabels.Short(item.QuestionType)</span>
    <span class="text-truncate d-inline-block" style="max-width: 380px;"
          title="@item.QuestionText">
        @item.QuestionText
    </span>
</td>
```

**Notes:**
- `@item.QuestionType` tipe `string?` (nullable) — helper handles null via `_` discard.
- `max-width: 420px` reduce ke `max-width: 380px` untuk akomodasi badge inline (~40px). Atau gunakan flex layout. Planner discretion.
- `ms-1` mungkin tidak perlu di awal cell — `me-2` lebih appropriate (margin-end). Planner pilih.

### Pattern 5: Controller flash error (D-11)

**Before (line 4688):**

```csharp
TempData["Error"] = "Pilihan Ganda hanya boleh memiliki 1 jawaban benar.";
```

**After:**

```csharp
TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
```

**Output:** `"Single Choice hanya boleh memiliki 1 jawaban benar."`

**Notes:**
- C# 6+ string interpolation `$"..."` — sudah idiomatic di codebase.
- Pakai literal `"MultipleChoice"` (enum value), bukan `questionType` parameter — defensive (pastikan label sesuai validasi context, bukan input dependent).
- Same pattern untuk lines 4693, 4829, 4834.
- Need `using HcPortal.Models;` di top of `AssessmentAdminController.cs` — likely sudah ada (verifikasi via file read sebelum edit).

### Anti-Patterns to Avoid

- **Hard-code "Single Choice" string di multiple files:** menggagalkan tujuan single source of truth (DRY). Selalu via `QuestionTypeLabels.{Method}()` call.
- **Mengubah `<option value="MultipleChoice">` jadi `<option value="SingleChoice">`:** akan break DB binding karena enum DB tidak berubah (D-17). Hanya teks dalam tag `<option>` yang berubah.
- **Mengubah JS string literal `'MultipleChoice'` jadi `'Single Choice'`** di `applyQTypeSwitch`/`populateEditForm` (lines 297-311, 356, 393): akan break dropdown logic. JS reads enum value, label irrelevant.
- **Blind sed di documentation:** "Pilihan Ganda" konteks AKM Kemendikbud (deskripsi soal "pilihan ganda" lowercase = generic term) ≠ "Pilihan Ganda" sebagai label tipe MC. Manual context-by-context review.
- **Memodifikasi `Excel template .xlsx` content:** binary, kolom internal pakai enum value (D-18). Hanya button text di view berubah.
- **Razor `@QuestionTypeLabels.Long()` di dropdown jika `selected` attribute belum ditangani:** existing dropdown statis hard-code text. Pilih: hard-code text long-form di `<option>` (simple) ATAU `@QuestionTypeLabels.Long("MultipleChoice")` (consistent helper usage). Pattern reference sebelum edit.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Enum-to-display label mapping | Inline ternary in every view (current state) | `QuestionTypeLabels.Short()` static helper | DRY, single source of truth, future relabel = 1 file change vs 6+ |
| Bootstrap badge color choice | Hand-code "bg-primary"/"bg-secondary" per type per view | `QuestionTypeLabels.BadgeClass()` | Color = part of label semantics; centralized matches design system |
| Localization (i18n) | Add `IStringLocalizer<T>` + .resx files | (skip — defer to v16+) | Phase scope is rename-only; i18n is over-engineering. App is mono-lingual saat ini |
| String matching `qtype == "MultipleAnswer"` for badge logic | Inline `qtype == "..." ? "X" : "Y"` ternary | switch expression in helper | switch ≥3 cases lebih readable + exhaustive |
| Default fallback per call site | Per-view `qtype ?? "MultipleChoice"` | Helper `_ => "Single Choice"` discard arm | Defensive at single point, removes "what if null?" repetition |

**Key insight:** Phase 304 D-15 spirit ("no extract premature") tetap dihormati — Phase 305 extract justified saat n=6 callers (3 badge + dropdown + controller + import button), bukan n=2 dari Phase 304. Helper class adalah natural consolidation point, bukan premature abstraction.

## Runtime State Inventory

> Phase 305 adalah **rename label-only** (UI display strings) — bukan rename enum / data migration / config rename. Namun karena CONTEXT.md eksplisit minta DB query verifikasi (D-17, D-20), saya cek runtime state untuk memastikan tidak ada side-effect tersembunyi.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | DB column `PackageQuestions.QuestionType` (string varchar) — values `"MultipleChoice"` / `"MultipleAnswer"` / `"Essay"` | **NONE** — D-17 lock; tidak diubah. Verifikasi via query `SELECT DISTINCT QuestionType FROM PackageQuestions` post-implementation (D-20). |
| Live service config | None — tidak ada n8n/Datadog/Tailscale registry external service. Aplikasi mono-instance ASP.NET Core dengan SQLite/SQL Server. | NONE |
| OS-registered state | None — tidak ada Windows Task Scheduler / pm2 / launchd registrasi yang mengandung label "Pilihan Ganda" / "Multi Jawaban". App dijalankan via dotnet run / IIS standard hosting. | NONE |
| Secrets/env vars | None — tidak ada env var atau SOPS key yang reference label QuestionType. `appsettings.json` (DB connection, AD config) tidak menyebut label tipe soal. | NONE |
| Build artifacts / installed packages | `bin/Debug/net8.0/HcPortal.dll` — akan re-build setelah edit `QuestionTypeLabels.cs` + view + controller. EF migration directory `Migrations/` — TIDAK butuh migration baru (DB schema tidak berubah). Excel template binary `wwwroot/documents/*.xlsx` — kolom internal QuestionType pakai enum value (D-18) — TIDAK diubah. | dotnet build (otomatis), tidak perlu migrasi DB, tidak perlu regen template. |

**Verifikasi explicit:** Tidak ada cached display string di Redis / IMemoryCache / SQLite app cache yang mengandung label "Pilihan Ganda". `IMemoryCache` di codebase dipakai hanya untuk `PERF-01` (Phase 311 future, distinct Categories) — irrelevant ke phase 305.

**Excel import file lama** (user pre-existing data dengan kolom QuestionType `"MultipleChoice"`/`"MultipleAnswer"`) tetap berfungsi — backward compat verified via D-18 lock. Import logic `AssessmentAdminController.ImportQuestions` membaca enum value, tidak label.

**Documentation HTML rendered statis** (served oleh ASP.NET StaticFiles middleware dari `wwwroot/`) — tidak ada cache layer; direct file edit langsung visible setelah refresh browser.

## Common Pitfalls

### Pitfall 1: "Pilihan Ganda" lowercase ambiguous context di dokumentasi

**What goes wrong:** Blind sed `Pilihan Ganda → Single Choice` akan replace "pilihan ganda, satu jawaban benar" deskripsi teknis (lowercase) → "Single Choice, satu jawaban benar" — semantically duplikat dengan label baru `"Single Choice (1 jawaban benar)"`. Kalimat jadi tidak natural.

**Why it happens:** Ada 2 makna "Pilihan Ganda" di dokumentasi:
1. Sebagai **label tipe MC** (e.g., `<td><strong>Pilihan Ganda</strong></td>` di `Panduan-Penggunaan-Website-HC-Portal-KPB.html` line 373) → harus diganti `Single Choice`
2. Sebagai **deskripsi generic** (e.g., `"Pilihan ganda, satu jawaban benar"` di `Release-Notes-HC-Portal-KPB.html` line 328 — describing MC type's behavior) → setelah label MC jadi "Single Choice", deskripsi ini akan terlihat redundant atau bisa dihapus/di-rephrase ke "Soal dengan satu jawaban benar."

**How to avoid:** Manual review per occurrence (jangan blind sed). Untuk setiap "Pilihan Ganda"/"Pilihan ganda" hit, baca surrounding context (1-2 paragraf), tentukan apakah "label tipe" atau "deskripsi generic". Update accordingly. CONTEXT.md D-13 sudah explicitly disebut: "Hati-hati: 'Pilihan Ganda' muncul juga di nilai enum AKM context — context-by-context review, bukan blind sed."

**Warning signs:** Hasil sed yang menghasilkan kalimat dengan duplikat ("Single Choice, soal pilihan ganda dengan satu jawaban") atau awkward grammar.

### Pitfall 2: "Multiple Choice" string juga muncul di dokumentasi (CONTEXT.md miss)

**What goes wrong:** CONTEXT.md D-13 hanya list pattern grep "Pilihan Ganda" / "Multi Jawaban" / "Multiple Answer" → klaim 14 occurrences di 8 file. Tapi grep menyeluruh (this research) menemukan "Multiple Choice" string juga ada di 4 dokumentasi: `Penjelasan-Halaman-PortalHC-KPB.html` line 532 ("tipe Multiple Choice, Multiple Answer, dan Essay"), `Struktur-Website-PortalHC-KPB.html` line 430 ("Multiple Choice, Multiple Answer, Essay"), `Release-Notes-HC-Portal-KPB.html` line 327 ("Multiple Choice (MC)"), dan `docs/Persiapan-Test-Manual-Assessment.html` line 396 ("Multiple Choice"). Total occurrences naik ke **17** (15 di wwwroot + 2 di docs).

**Why it happens:** CONTEXT.md grep pattern incomplete — "Multiple Choice" tidak tercakup oleh "Multiple Answer" pattern.

**How to avoid:** Planner gunakan grep pattern **lengkap** untuk identify all occurrences sebelum sed:
```bash
grep -rn "Pilihan Ganda\|Pilihan ganda\|Multi Jawaban\|Multiple Answer\|Multiple Choice" wwwroot/documents/ docs/
```
Update RESEARCH.md occurrence count: **17 occurrences total** (CONTEXT.md says 14 — diff +3 dari "Multiple Choice" hits).

**Warning signs:** Setelah sed selesai, residual "Multiple Choice" / "Multiple Answer" di docs yang tidak terdeteksi pattern CONTEXT.md.

### Pitfall 3: Lupa update `using HcPortal.Models;` di controller

**What goes wrong:** `AssessmentAdminController.cs` mungkin sudah `using HcPortal.Models` di top (likely karena uses `PackageQuestion` model). Jika tidak ada, edit `TempData["Error"] = $"{QuestionTypeLabels.Short(...)} ..."` akan compile error `CS0103: The name 'QuestionTypeLabels' does not exist in the current context`.

**Why it happens:** Controller files biasanya have `using HcPortal.Models` (verified pattern), tapi safety check needed.

**How to avoid:** Sebelum edit line 4688/4693/4829/4834, grep top of `AssessmentAdminController.cs`:
```bash
grep -n "using HcPortal.Models" Controllers/AssessmentAdminController.cs | head -1
```
Jika tidak ada, tambah baris `using HcPortal.Models;` di import block. (Likely already exists since file uses `PackageQuestion`, `PackageOption`, `AssessmentPackage` types.)

**Warning signs:** Build error `CS0103` setelah edit controller.

### Pitfall 4: `_PreviewQuestion.cshtml` reuse local var `badgeClass`/`badgeLabel`

**What goes wrong:** `_PreviewQuestion.cshtml` declares `var badgeClass = ...` dan `var badgeLabel = ...` di code block top (lines 5-7), digunakan satu kali di line 14. Jika hapus dua var dan inline helper, OK. Tapi jika ada code lain di view yang juga reference `badgeClass`/`badgeLabel` (verify via grep), HARUS keep variables atau update semua references.

**How to avoid:** Sebelum hapus local var, grep variable name di file:
```bash
grep -c "badgeClass\|badgeLabel" Views/Admin/_PreviewQuestion.cshtml
```
Verified by this research: keduanya hanya muncul 1x masing-masing (declaration + usage line 14). Aman dihapus jika inline helper.

**Warning signs:** Razor compile error setelah edit (RZ error `CS0103` di view).

### Pitfall 5: `StartExam.cshtml` badge color shift dari rename

**What goes wrong:** Dulu MA badge `bg-secondary` (line 103), Essay badge `bg-secondary` (line 105). Setelah pakai `BadgeClass(qtype)`: MA→`bg-primary` (blue), Essay→`bg-info text-dark` (cyan dengan dark text). Worker yang familiar dengan tampilan grey badge MA bisa surprised.

**Why it happens:** Helper class memang menyatukan color scheme dengan tabel admin (Manage). Konsisten dengan tujuan symmetric badge (D-09, D-16).

**How to avoid:** Documented as INTENDED change. Human verification (D-09) wajib catat: "MA exam badge color berubah dari grey ke blue (alignment dengan admin)" — bukan regression.

**Warning signs:** User feedback "kenapa warna badge soal saya berubah?" — answer: alignment ke design system (admin & worker konsisten warna).

### Pitfall 6: Razor `@` inside HTML attribute butuh string kontainer

**What goes wrong:** Razor expression `@QuestionTypeLabels.BadgeClass(qtype)` di dalam `class="badge @QuestionTypeLabels.BadgeClass(qtype) small"` akan render `class="badge bg-primary small"` — OK karena Razor auto-encode dan handles inline expression. Tapi jika string return helper mengandung quote/space yang aneh, bisa break. Dalam kasus ini return values murni (`bg-primary`, `bg-secondary`, `bg-info text-dark`) — `bg-info text-dark` mengandung space, akan render sebagai 2 class.

**Why it happens:** Bootstrap utility class chaining via space — fine.

**How to avoid:** Verifikasi helper return values tidak mengandung HTML-meaningful chars. Saat ini OK. Jika future variant pakai `;` atau `"` di return, escape needed.

**Warning signs:** Browser DevTools menunjukkan attribute mal-formed.

## Code Examples

Verified patterns from `.planning/codebase/CONVENTIONS.md`, `Models/AssessmentConstants.cs`, dan codebase grep:

### Helper Class Definition

```csharp
// Source: pattern from Models/AssessmentConstants.cs (existing)
//         + Phase 305 D-03 spec
namespace HcPortal.Models
{
    public static class QuestionTypeLabels
    {
        public static string Long(string? type) => type switch
        {
            "MultipleChoice" => "Single Choice (1 jawaban benar)",
            "MultipleAnswer" => "Multiple Answers (≥2 jawaban benar)",
            "Essay"          => "Essay",
            _                => "Single Choice (1 jawaban benar)"
        };

        public static string Short(string? type) => type switch
        {
            "MultipleChoice" => "Single Choice",
            "MultipleAnswer" => "Multiple Answers",
            "Essay"          => "Essay",
            _                => "Single Choice"
        };

        public static string BadgeClass(string? type) => type switch
        {
            "MultipleChoice" => "bg-secondary",
            "MultipleAnswer" => "bg-primary",
            "Essay"          => "bg-info text-dark",
            _                => "bg-secondary"
        };
    }
}
```

### Razor View Helper Call (Badge Pattern)

```razor
@* Source: Views/Admin/ManagePackageQuestions.cshtml after Phase 305 edit *@
@* Replaces lines 68-70 inline ternary *@
@foreach (var q in questions)
{
    var qtype = q.QuestionType ?? "MultipleChoice";
    <tr>
        <td>...</td>
        <td>
            <span class="badge @QuestionTypeLabels.BadgeClass(qtype) small">
                @QuestionTypeLabels.Short(qtype)
            </span>
        </td>
    </tr>
}
```

### Razor Dropdown Pattern (Long-form)

```razor
@* Source: Views/Admin/ManagePackageQuestions.cshtml lines 132-136 *@
<select class="form-select form-select-sm" id="QuestionType" name="questionType">
    <option value="MultipleChoice">@QuestionTypeLabels.Long("MultipleChoice")</option>
    <option value="MultipleAnswer">@QuestionTypeLabels.Long("MultipleAnswer")</option>
    <option value="Essay">@QuestionTypeLabels.Long("Essay")</option>
</select>
@* ATAU hard-code text untuk cleaner Razor: *@
@* <option value="MultipleChoice">Single Choice (1 jawaban benar)</option> *@
@* — kedua valid; planner pilih (D-04 Claude's Discretion). *@
```

### Controller Flash Error (String Interpolation)

```csharp
// Source: Controllers/AssessmentAdminController.cs after Phase 305 edit
// Replaces line 4688
TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
// Output: "Single Choice hanya boleh memiliki 1 jawaban benar."

// Replaces line 4693
TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleAnswer")} membutuhkan minimal 2 jawaban benar.";
// Output: "Multiple Answers membutuhkan minimal 2 jawaban benar."

// Same pattern for lines 4829 (CreateQuestion) and 4834 (EditQuestion).
```

### Worker Exam Badge (Symmetric Pattern)

```razor
@* Source: Views/CMP/StartExam.cshtml after Phase 305 edit *@
@* Replaces lines 100-106 — asimetris (only MA & Essay had badge) → simetris (semua tipe) *@
<p class="fw-bold mb-3">
    <span class="badge bg-primary me-2">@q.DisplayNumber</span>
    <span class="badge @QuestionTypeLabels.BadgeClass(qtype) ms-1 small">@QuestionTypeLabels.Short(qtype)</span>
    @q.QuestionText
</p>
```

### ExamSummary Badge (NEW per D-10 SCOPE EXTENSION)

```razor
@* Source: Views/CMP/ExamSummary.cshtml after Phase 305 edit *@
@* Append badge in line 50-54 area, sebelum @item.QuestionText *@
<td>
    <span class="badge @QuestionTypeLabels.BadgeClass(item.QuestionType) small me-2">@QuestionTypeLabels.Short(item.QuestionType)</span>
    <span class="text-truncate d-inline-block" style="max-width: 380px;"
          title="@item.QuestionText">
        @item.QuestionText
    </span>
</td>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Inline ternary `qtype == "MA" ? "X" : "Y"` per view | Static helper class with switch expression | Phase 305 (this) | DRY: 1 file change for future relabel |
| `bg-secondary` ad-hoc untuk MA/Essay di StartExam | `BadgeClass(type)` consistent dengan Manage | Phase 305 (this) | Visual alignment admin & worker |
| Worker exam: only MA + Essay had badge | All types (MC, MA, Essay) have badge | Phase 305 D-09 | Symmetric UX, no surprise default |
| ExamSummary: no type badge | Add badge before question text | Phase 305 D-10 SCOPE EXTENSION | Worker melihat tipe soal saat review |

**Deprecated/outdated:**
- ROADMAP §"v15.0 Audit Findings" Phase 305 success criteria #1 wording: `"Pilihan Tunggal (1 jawaban benar)" + "Pilihan Jamak (≥2 jawaban benar)"` — **OUTDATED**, final wording dari CONTEXT.md D-01 adalah `"Single Choice (1 jawaban benar)"` / `"Multiple Answers (≥2 jawaban benar)"` (English). Planner & verifier MUST defer ke CONTEXT.md, bukan ROADMAP.
- Phase 304 D-15 ("no extract premature") — Phase 305 deviasi justified karena n=6 callers (vs Phase 304 n=2). Pattern document di CONTEXT.md `<specifics>` "Helper Class Rationale".

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `using HcPortal.Models;` already exists at top of `Controllers/AssessmentAdminController.cs` | Common Pitfalls #3 | Low — build error caught immediately, easy fix (add 1 line) |
| A2 | "Multiple Answers" plural form (D-01) konsisten dengan Moodle/Canvas convention | Standard Stack discussion | None — user explicit choice in CONTEXT.md D-01 (verified via DISCUSSION-LOG line 35-37) |
| A3 | Total occurrence di docs adalah **17** (15 wwwroot + 2 docs), bukan 14 seperti claim CONTEXT.md | Common Pitfalls #2 | Medium — planner harus grep ulang lengkap untuk catch all. Mitigation: pitfall #2 documented |
| A4 | Bootstrap badge class `bg-info text-dark` rendering OK ketika di-string-concat dengan space (mengisi 2 CSS class) | Common Pitfalls #6 | Low — verified pattern dari existing `Views/Admin/ManagePackageQuestions.cshtml` line 69 already uses `(qtype == "Essay" ? "bg-info text-dark" : ...)` — proven works |
| A5 | `Views/CMP/_ViewImports.cshtml` (sub-area) tidak override `@using HcPortal.Models` global dari `Views/_ViewImports.cshtml` | Architecture Patterns | Low — Razor convention is hierarchical inheritance; sub-folder _ViewImports adds, doesn't override. Planner verify file existence. |
| A6 | `BadgeClass(type)` returning `bg-info text-dark` (mengandung space) di dalam Razor attribute concatenation render valid 2 CSS classes | Code Examples #2 | Low — same as A4 |
| A7 | C# 12 `switch` expression dengan `_` discard arm untuk null+unknown cases adalah idiomatic dan efficient (no per-call dictionary allocation) | Standard Stack | Low — C# 12 spec compliant `[CITED: docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/switch-expression]` |

**Risk-confirmation needed:** A3 — planner MUST run lengkap grep sebelum sed dokumentasi, atau accept bahwa 3 extra "Multiple Choice" hits akan tertinggal. CONTEXT.md D-13 sed pattern incomplete. Planner decision needed di plan creation.

## Open Questions

1. **`Views/CMP/_ViewImports.cshtml` existence**
   - What we know: `Views/_ViewImports.cshtml` line 2 sudah `@using HcPortal.Models` (verified). ASP.NET Core hierarchical _ViewImports — sub-folder file ADDS to parent, tidak override.
   - What's unclear: Apakah `Views/CMP/` punya `_ViewImports.cshtml` sub-area? Jika ya, mungkin override?
   - Recommendation: Planner Bash check `ls Views/CMP/_ViewImports.cshtml 2>/dev/null && cat Views/CMP/_ViewImports.cshtml`. Jika file ada, verify it includes `@using HcPortal.Models` ATAU rely on parent inheritance (likely auto). Phase 304 SUMMARY tidak mention _ViewImports issue di CMP — likely fine.

2. **Dropdown option text style: helper call vs hard-code**
   - What we know: D-04 explicitly menyatakan "View Razor: `@QuestionTypeLabels.Short(qtype)` ... atau static text — keduanya valid; planner pilih". 
   - What's unclear: Konvensi codebase favor mana?
   - Recommendation: Hard-code static text (`<option value="MultipleChoice">Single Choice (1 jawaban benar)</option>`) lebih simpel dan readable di dropdown, tapi kurang DRY. Helper call (`<option value="MultipleChoice">@QuestionTypeLabels.Long("MultipleChoice")</option>`) lebih DRY tapi minor verbosity. Planner pilih — minor decision, no functional impact.

3. **`AssessmentSummaryItem.QuestionType` data flow ke ExamSummary**
   - What we know: `Views/CMP/ExamSummary.cshtml` line 1 declares `@model List<HcPortal.Models.ExamSummaryItem>`. Line 57 reads `item.QuestionType == "MultipleAnswer"` — confirming `ExamSummaryItem` has `QuestionType` property string-typed.
   - What's unclear: Apakah `ExamSummaryItem.QuestionType` nullable atau non-null? Helper handles both via `string?` param.
   - Recommendation: Planner read `Models/ExamSummaryItem.cs` (or wherever defined — likely `Models/PackageExamViewModel.cs` block) untuk verify nullability. Jika non-null, no risk; jika nullable, helper fallback handles.

4. **Excel template binary file naming convention**
   - What we know: `wwwroot/documents/` & `Database/` folder list (verified via Glob). Tidak ada file `template_MC.xlsx` ATAU `template_Single_Choice.xlsx` literal di Glob output. Templates likely di-generate on-the-fly via `DownloadQuestionTemplate` action di controller.
   - What's unclear: Apakah controller action `DownloadQuestionTemplate("MC")` baca template dari file binary `wwwroot/documents/template_MC.xlsx` — jika ya, file binary content (mungkin punya cell title "Pilihan Ganda") tidak terupdate.
   - Recommendation: D-12 + Deferred #5 (CONTEXT.md) explicitly says binary template tidak diubah. Button label di view berubah ("Template Single Choice"), tapi download content (cell title) tetap. Acceptable tradeoff per user decision.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | C# compile, dotnet build | ✓ | 8.0+ (verified .csproj `<TargetFramework>net8.0</TargetFramework>`) | — |
| ASP.NET Core MVC | Razor view rendering, controller routing | ✓ | 8.0 (NuGet packages 8.0.0) | — |
| EF Core | DB query (D-20 verifikasi) | ✓ | 8.0 (NuGet `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0) | — |
| Bootstrap | UI badge classes | ✓ | 5.3 (CDN/wwwroot/lib/) | — |
| sqlcmd / EF Tools | Run `SELECT DISTINCT QuestionType FROM PackageQuestions` (D-20) | sqlcmd ✓ jika SQL Server installed locally; OR `dotnet ef database query` (limited, EF tools tidak punya direct query — must use migration script ATAU SQL client) | varies | Manual user verification via SQL Server Management Studio (SSMS), Azure Data Studio, atau `sqlite3` CLI jika dev SQLite |
| Playwright | E2E test (CONTEXT.md verified ZERO match — no test update needed) | ✓ (npm dev dep, Playwright 1.58.2 per `tests/node_modules/`) | 1.58.2 | — — tidak butuh untuk phase ini per D-15 |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:** sqlcmd untuk D-20 verification — gunakan SSMS/Azure Data Studio/sqlite3 CLI sebagai fallback (manual user task post-implementation).

**Skip rationale:** Phase 305 adalah pure code/config change dengan minimal external deps. Build dependencies sudah ada (.NET 8 SDK, NuGet packages); test/run-time tidak butuh new install.

## Validation Architecture

> `nyquist_validation: true` di `.planning/config.json` — section ini diperlukan.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Playwright 1.58.2 (E2E only); `dotnet build` (compile check); manual smoke test (UI verification) |
| Config file | `tests/playwright.config.ts` (assumed; not verified — Phase 304 verification mentions `tests/e2e/`) |
| Quick run command | `dotnet build -c Debug --nologo --verbosity minimal` (compile check, ~5-10s) |
| Full suite command | `dotnet build && dotnet test` (kalau ada xUnit) atau `npx playwright test --project=chromium tests/e2e/` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| LBL-01 | `QuestionTypeLabels.Long("MultipleChoice")` returns `"Single Choice (1 jawaban benar)"` | unit (xUnit, optional) | `dotnet test --filter "QuestionTypeLabelsTests"` (jika xUnit project ada — TIDAK ada saat ini) | ❌ Wave 0 (xUnit project tidak ada di repo); manual code review acceptable |
| LBL-01 | `QuestionTypeLabels.Short("MultipleAnswer")` returns `"Multiple Answers"` | unit | sama seperti atas | ❌ Wave 0 |
| LBL-01 | `QuestionTypeLabels.BadgeClass("Essay")` returns `"bg-info text-dark"` | unit | sama | ❌ Wave 0 |
| LBL-01 | `QuestionTypeLabels.{any}(null)` returns fallback (Single Choice / bg-secondary) | unit (D-05 boundary) | sama | ❌ Wave 0 |
| LBL-01 | Badge "Single Choice" muncul di dropdown form admin | smoke (manual browser) | Manual: open `/Admin/ManagePackageQuestions/{packageId}` → verify dropdown shows Long form | manual |
| LBL-01 | Badge "Single Choice" muncul di tabel Manage badge | smoke | Manual: same view, verify badge col | manual |
| LBL-01 | Badge "Single Choice" muncul di preview modal | smoke | Manual: click eye icon → modal shows badge | manual |
| LBL-01 | Badge "Single Choice"/"Multiple Answers"/"Essay" muncul di StartExam (semua tipe sekarang) | smoke | Manual: worker login, mulai exam dengan paket yang punya 3 tipe | manual |
| LBL-01 | Badge muncul di ExamSummary (SCOPE EXTENSION D-10) | smoke | Manual: complete answers, navigate ke ExamSummary | manual |
| LBL-01 | Button "Template Single Choice" / "Template Multiple Answers" muncul di Import view | smoke | Manual: visit `/Admin/ImportPackageQuestions/{packageId}` | manual |
| LBL-01 | Flash error di-trigger saat MC dengan 0 jawaban benar = "Single Choice hanya boleh memiliki 1 jawaban benar." | smoke | Manual: try create MC question dengan no correct answer | manual |
| LBL-01 | DB enum value `MultipleChoice`/`MultipleAnswer`/`Essay` tidak berubah (D-17, D-20) | DB query post-deploy | Manual: `SELECT DISTINCT QuestionType FROM PackageQuestions` via SSMS/sqlite3 | manual (post-deploy) |
| LBL-01 | Build success (Razor compile clean) | compile | `dotnet build -c Debug --nologo --verbosity minimal` | ✓ available |

### Sampling Rate

- **Per task commit:** `dotnet build -c Debug --nologo --verbosity minimal` (~5-10s — Razor compile catches view syntax error)
- **Per wave merge:** Manual smoke test 8 functional checks (matching Phase 304 verification pattern; all 8 checks <15 menit)
- **Phase gate:** Full smoke + DB query verification + grep audit (helper class definition + 5 view files + controller + 8 docs)

### Wave 0 Gaps

- [ ] **Optional:** Create xUnit test project `HcPortal.Tests/QuestionTypeLabelsTests.cs` — covers all 4 cases (MC, MA, Essay, null) × 3 methods = 12 assertions. **Discretion:** Phase 305 minimal-risk, manual code review pattern Phase 304 acceptable. Planner pilih — if no test project bootstrapped, can defer (Phase 304 didn't bootstrap either).
- [ ] **No xUnit project exists** in repo (`.planning/codebase/STACK.md` line 33 mentions only Playwright). Bootstrapping new test project = scope creep beyond Phase 305 — **defer**.
- [ ] **Playwright E2E label-update tests** — D-15 explicitly says ZERO match in `tests/e2e/`, no update needed. **Skip**.

**Recommended approach:** Pattern Phase 304 (just-shipped 2026-04-28) — manual smoke test + grep audit + dotnet build verification + human verification checkpoint. No new test infrastructure. Phase plan checklist mirror Phase 304 plans (3-task auto + 1-task human checkpoint per plan).

## Security Domain

> `security_enforcement` not explicitly disabled in `.planning/config.json` — treat as enabled. Phase 305 adalah label-rename UI-only, surface attack minimal. Section dipertahankan untuk completeness.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Helper class tidak menyentuh auth — Auth dipegang oleh ASP.NET Core Identity (tidak diubah) |
| V3 Session Management | no | TempData["Error"] flash — built-in ASP.NET Core mechanism, secure by default |
| V4 Access Control | no | Controller `[Authorize(Roles = "Admin, HC")]` tidak diubah |
| V5 Input Validation | yes (existing) | `validTypes.Contains(questionType)` whitelist (line 4658, 4810) — verified |
| V6 Cryptography | no | No crypto in scope |
| V11 Business Logic | yes | Validation rule MC=1 correct / MA≥2 correct unchanged; only error message wording change |

### Known Threat Patterns for ASP.NET Core MVC + Razor

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| XSS via Razor expression `@variable` | Spoofing/Tampering | Razor auto-encodes `@` output by default. `QuestionTypeLabels.Short()` returns hard-coded literal strings — no user input flows. Compliant ASVS V5.3.1 |
| String interpolation in TempData | Information Disclosure | `$"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh..."` — both segments are server-side literal strings. No user-controlled string concat. Compliant ASVS V5.3.4 |
| Bootstrap class injection via `@QuestionTypeLabels.BadgeClass(qtype)` | Tampering | Helper switch returns hard-coded literals (`bg-primary`, `bg-secondary`, `bg-info text-dark`) — no user-controlled CSS class injection possible |
| SQL injection at D-20 query | Tampering | `SELECT DISTINCT QuestionType FROM PackageQuestions` — pure read query, no user param. Manual user task post-deploy |

**STRIDE Threat Register (phase-level):**

| Threat ID | Category | Component | Disposition | Mitigation |
|-----------|----------|-----------|-------------|------------|
| T-305-01 | Tampering | DOM `<option value="MultipleChoice">` value attr berubah → server binding gagal | mitigate | D-07 explicit lock — value attr tetap. Verify via grep `value="MultipleChoice"` returns ≥1 post-edit |
| T-305-02 | Tampering | Helper switch fallback `_` returns wrong default → unknown qtype displays as MC label | accept | Defensive default per D-05 explicit choice. Unknown qtype is a code-path bug; falling to MC display is safer than throw exception in view |
| T-305-03 | Information Disclosure | Error message reveals system enum (`"MultipleChoice"`) to user | accept | Error message displays user-facing label "Single Choice", bukan enum value. Internal enum hidden |
| T-305-04 | Repudiation | Audit log impact dari label change | n/a | UI cosmetic. No state change. No audit log entry |
| T-305-05 | DoS | Helper static class no memory leak risk | accept | Pure static functions, no allocation per call. Optimal performance |
| T-305-06 | Elevation of Privilege | Bypass auth via badge HTML manipulation | accept | Badge is read-only display. No control flow tied to badge text/class |

**ASVS L1 alignment:**
- V5.2.1 (output encoding): Razor `@` auto-encode — Compliant
- V5.3.1 (XSS prevention): Helper returns literal strings — Compliant
- V5.3.4 (no string concat user input → output): TempData uses server-literal segments only — Compliant
- V11.1.1 (no auth logic in client): Phase touches no auth — Compliant

**No high-risk threat — security_enforcement gate: PASS.**

## Sources

### Primary (HIGH confidence)

- `Views/_ViewImports.cshtml` — `@using HcPortal.Models` global verified line 2 [VERIFIED via Read tool]
- `Models/AssessmentConstants.cs` — pattern reference for static helper class style [VERIFIED]
- `HcPortal.csproj` — `<TargetFramework>net8.0</TargetFramework>` [VERIFIED line 4]
- `Views/Admin/ManagePackageQuestions.cshtml` — full content read; lines 67-70 (badge ternary), 132-136 (dropdown), 297-311 (JS handler), 356, 393-394 [VERIFIED]
- `Views/Admin/_PreviewQuestion.cshtml` — full content read; lines 5-7 (badge logic), 14 (badge render) [VERIFIED]
- `Views/CMP/StartExam.cshtml` lines 1-150 — badge inline soal lines 100-106 (asimetris MA & Essay only) [VERIFIED]
- `Views/CMP/ExamSummary.cshtml` — full content read; line 53 area (target SCOPE EXTENSION D-10) [VERIFIED]
- `Views/Admin/ImportPackageQuestions.cshtml` — full content read; lines 32-50 (bullet + 4 button) [VERIFIED]
- `Controllers/AssessmentAdminController.cs` lines 4670-4849 — flash error 4 lokasi (4688, 4693, 4829, 4834) verified text [VERIFIED]
- `.planning/codebase/STACK.md` — .NET 8.0, ASP.NET Core MVC 8.0, Bootstrap 5.3, no xUnit project [VERIFIED]
- `.planning/codebase/CONVENTIONS.md` — namespace pattern, file-scoped vs block-scoped namespace style mix, helper PascalCase [VERIFIED]
- Phase 304 PLAN files (`304-01-PLAN.md`, `304-02-PLAN.md`, `304-VERIFICATION.md`) — pattern reference [VERIFIED — same milestone, just shipped]

### Secondary (MEDIUM confidence)

- C# 12 switch expression behavior — Microsoft official docs [CITED: docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/switch-expression]
- ASP.NET Core hierarchical `_ViewImports.cshtml` inheritance — Microsoft docs [CITED: docs.microsoft.com/en-us/aspnet/core/mvc/views/layout]
- Bootstrap 5.3 badge utility classes — official docs [CITED: getbootstrap.com/docs/5.3/components/badge/]

### Grep audit results (HIGH confidence — direct evidence)

- E2E tests `tests/e2e/` (4 .ts files) — ZERO match for `Pilihan Ganda|Multi Jawaban|Multiple Answer|MultipleChoice|MultipleAnswer` [VERIFIED — confirms CONTEXT.md D-15 claim]
- `wwwroot/documents/` — **15 occurrences** across 7 files (CONTEXT.md says 14 — diff +1 dari "Multiple Choice" pattern not in CONTEXT.md grep)
- `docs/` — **2 occurrences** in 1 file (`Persiapan-Test-Manual-Assessment.html` lines 396, 397 — Multiple Choice + Multiple Answer)
- **Total docs occurrences: 17** vs CONTEXT.md claim 14
- Views with `MultipleChoice|MultipleAnswer` reference — exactly 5 files: ManagePackageQuestions.cshtml, _PreviewQuestion.cshtml, StartExam.cshtml, ExamSummary.cshtml, ImportPackageQuestions.cshtml [VERIFIED — matches CONTEXT.md scope]

## Metadata

**Confidence breakdown:**

- **Standard stack:** HIGH — verified via .csproj inspection, codebase pattern (`AssessmentConstants.cs`), and STACK.md document
- **Architecture patterns:** HIGH — verified via reading actual source code of all 5 view files + controller; tier mapping straightforward (Razor view + C# helper + controller text)
- **Pitfalls:** HIGH — Pitfalls #1 dan #2 directly evidenced by grep results (CONTEXT.md miss "Multiple Choice" pattern; ambiguous "Pilihan ganda" lowercase context). Pitfalls #3-6 derived from C# / Razor language standard rules.
- **Documentation occurrences:** HIGH — direct grep evidence (17 total vs CONTEXT.md's 14)
- **JS handler safety:** HIGH — verified ManagePackageQuestions.cshtml lines 276-394 reads enum value (`'MultipleChoice'`/`'MultipleAnswer'`/`'Essay'`), label irrelevant
- **DB enum lock:** HIGH — D-17 explicit; verified via grep no migration scripts in scope
- **Test infrastructure gap:** HIGH — verified no xUnit project (only Playwright in `tests/`), no test bootstrapping needed (phase 304 pattern accepted)

**Research date:** 2026-04-28
**Valid until:** 2026-05-12 (14 days — codebase stable, no major library version churn expected)

---

*Phase: 305-question-type-naming-clarity*
*Researched: 2026-04-28*
