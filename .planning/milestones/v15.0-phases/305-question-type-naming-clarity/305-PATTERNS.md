# Phase 305: Question Type Naming Clarity - Pattern Map

**Mapped:** 2026-04-28
**Files analyzed:** 9 (1 NEW + 7 modified Razor/Controller + 8 docs sed-replace)
**Analogs found:** 9 / 9 (100% coverage — semua perubahan punya precedent di codebase)

> **Konteks:** Phase 305 = UI label rename + extract C# static helper class sebagai single source of truth. Phase 304 (`.planning/phases/304-ui-label-polish-login-wib/304-PATTERNS.md`) adalah precedent format paling dekat (same milestone v15.0, same UI-label-polish style), namun Phase 305 deviasi *justified* dengan helper extraction karena n=6 callers (vs Phase 304 n=2).

---

## File Classification

| File | Status | Role | Data Flow | Closest Analog | Match Quality |
|------|--------|------|-----------|----------------|---------------|
| `Models/QuestionTypeLabels.cs` | NEW | model helper (static class) | server-render → string mapping | `Models/AssessmentConstants.cs` | role-match (static helper di Models/) |
| `Views/Admin/ManagePackageQuestions.cshtml` | EDIT | view (Razor) — admin form | server-render badge + dropdown | self (lines 67-70 inline ternary) | exact (in-place) |
| `Views/Admin/_PreviewQuestion.cshtml` | EDIT | view partial (Razor) | server-render badge | self (lines 5-7 inline ternary) | exact (in-place) |
| `Views/CMP/StartExam.cshtml` | EDIT | view (Razor) — worker exam | server-render badge inline soal | self (lines 100-106 if/elseif) | exact (in-place + simetrisasi) |
| `Views/CMP/ExamSummary.cshtml` | EDIT (SCOPE EXTENSION D-10) | view (Razor) — worker review | server-render badge baru | `Views/CMP/StartExam.cshtml` (badge pattern) | role-match (badge baru di summary) |
| `Views/Admin/ImportPackageQuestions.cshtml` | EDIT | view (Razor) — button label | static text | self (lines 38-43 button text) | exact (in-place) |
| `Controllers/AssessmentAdminController.cs` | EDIT | controller — flash error | TempData → next-render | self (lines 4688, 4693, 4829, 4834) + 16 lokasi `TempData["Error"]` existing | exact (in-place) |
| 7 file `wwwroot/documents/` | EDIT (sed-replace) | static doc HTML/MD/PY | served by StaticFiles middleware | n/a — manual context-aware sed | n/a |
| `docs/Persiapan-Test-Manual-Assessment.html` | EDIT (sed-replace) | static doc HTML | served direct | n/a — manual context-aware sed | n/a |

---

## Pattern Assignments

### 1. `Models/QuestionTypeLabels.cs` (NEW model helper, server-render → string mapping)

**Analog:** `Models/AssessmentConstants.cs` — existing static helper class di `Models/` dengan namespace `HcPortal.Models`.

**Excerpt analog (`Models/AssessmentConstants.cs` lines 1-14):**
```csharp
namespace HcPortal.Models
{
    public static class AssessmentConstants
    {
        public static class AssessmentType
        {
            public const string Manual = "Manual";
            public const string Online = "Online";
            public const string PreTest = "PreTest";
            public const string PostTest = "PostTest";
        }
        // ...
    }
}
```

**Style yang harus di-match dari analog:**
- Block-scoped namespace `namespace HcPortal.Models { ... }` (BUKAN file-scoped) — match precedent `AssessmentConstants.cs`
- `public static class` (bukan instance class) — pure stateless mapping, no DI
- Indentasi 4 spasi, brace di line baru (Allman) — match precedent

**Pattern target untuk `QuestionTypeLabels.cs`** (per CONTEXT.md D-03 + RESEARCH.md Pattern 1):
```csharp
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

**Wajib (per D-03, D-05, D-06):**
- Param `string?` (nullable) — match property `QuestionType` nullable di `AssessmentPackage.cs` line 48 + `PackageExamViewModel.cs` line 37/62 + `AnalyticsDashboardViewModel.cs` line 86
- `_` discard arm catches null AND unknown values — single defensive default
- Switch expression style (C# 12 idiomatic, sudah dipakai di codebase per Phase 12.0 controller refactor)
- Namespace literal `HcPortal.Models` (bukan `HcPortal.Helpers`) — D-06 explicit pilih `Models/` karena semantically domain-related

---

### 2. `Views/Admin/ManagePackageQuestions.cshtml` (view, server-render badge + dropdown)

**Analog:** Self (existing inline ternary pattern di lines 67-70 dan dropdown text di lines 132-136).

#### 2a. Badge replacement (lines 67-70)

**Existing code (BEFORE) — lines 66-79:**
```razor
@foreach (var q in questions)
{
    var qtype = q.QuestionType ?? "MultipleChoice";
    var badgeClass = qtype == "MultipleAnswer" ? "bg-primary" : (qtype == "Essay" ? "bg-info text-dark" : "bg-secondary");
    var badgeLabel = qtype == "MultipleAnswer" ? "Multi Jawaban" : (qtype == "Essay" ? "Essay" : "Pilihan Ganda");
    <tr>
        <td class="text-muted">@q.Order</td>
        <td>
            <span class="d-inline-block text-truncate" style="max-width:260px" title="@q.QuestionText">
                @q.QuestionText
            </span>
        </td>
        <td>
            <span class="badge @badgeClass small">@badgeLabel</span>
        </td>
```

**Pattern target (AFTER) — replace lines 68-70 + 79:**
```razor
@foreach (var q in questions)
{
    var qtype = q.QuestionType ?? "MultipleChoice";
    <tr>
        <td class="text-muted">@q.Order</td>
        <td>
            <span class="d-inline-block text-truncate" style="max-width:260px" title="@q.QuestionText">
                @q.QuestionText
            </span>
        </td>
        <td>
            <span class="badge @QuestionTypeLabels.BadgeClass(qtype) small">@QuestionTypeLabels.Short(qtype)</span>
        </td>
```

**Notes (per RESEARCH.md Pitfall #4):**
- Hapus 2 local var `badgeClass` dan `badgeLabel` (verified hanya digunakan 1x masing-masing di line 79)
- Fallback `qtype ?? "MultipleChoice"` di line 68 **tetap** (existing pattern, double-safety dengan helper `_` discard)
- Line 79 `<span class="badge @badgeClass small">@badgeLabel</span>` → `<span class="badge @QuestionTypeLabels.BadgeClass(qtype) small">@QuestionTypeLabels.Short(qtype)</span>`

#### 2b. Dropdown options (lines 132-136)

**Existing code (BEFORE) — lines 132-136:**
```razor
<select class="form-select form-select-sm" id="QuestionType" name="questionType">
    <option value="MultipleChoice">Pilihan Ganda (MC)</option>
    <option value="MultipleAnswer">Multiple Answer (MA)</option>
    <option value="Essay">Essay</option>
</select>
```

**Pattern target (AFTER) — option value attr TETAP (D-07, D-17):**
```razor
<select class="form-select form-select-sm" id="QuestionType" name="questionType">
    <option value="MultipleChoice">@QuestionTypeLabels.Long("MultipleChoice")</option>
    <option value="MultipleAnswer">@QuestionTypeLabels.Long("MultipleAnswer")</option>
    <option value="Essay">@QuestionTypeLabels.Long("Essay")</option>
</select>
```

**Atau alternatif hard-code (planner discretion per D-04):**
```razor
<select class="form-select form-select-sm" id="QuestionType" name="questionType">
    <option value="MultipleChoice">Single Choice (1 jawaban benar)</option>
    <option value="MultipleAnswer">Multiple Answers (≥2 jawaban benar)</option>
    <option value="Essay">Essay</option>
</select>
```

**Rekomendasi:** Hard-code text — dropdown selalu render statis 3 option, tidak ada loop. Helper call menambah 1 lookup tanpa benefit DRY substantial. **Konsistensi:** sama nilai output, value attr `MultipleChoice`/`MultipleAnswer`/`Essay` tetap.

**JS handler integrity (lines 297-311, 356, 393-394) — TIDAK BOLEH DIUBAH** (D-19): JS membaca `<option value>` (enum value) via `data-question-type` atau `dropdown.value`. Karena value attr tetap, JS aman.

---

### 3. `Views/Admin/_PreviewQuestion.cshtml` (view partial, server-render badge)

**Analog:** Self (lines 5-7 inline ternary + line 14 badge render).

**Existing code (BEFORE) — lines 1-16:**
```razor
@using HcPortal.Models
@model PackageQuestion

@{
    var qtype = Model.QuestionType ?? "MultipleChoice";
    var badgeClass = qtype == "MultipleAnswer" ? "bg-primary" : (qtype == "Essay" ? "bg-info text-dark" : "bg-secondary");
    var badgeLabel = qtype == "MultipleAnswer" ? "Multi Jawaban" : (qtype == "Essay" ? "Essay" : "Pilihan Ganda");
}

<div class="card border-0">
    <div class="card-body p-0">
        <!-- Badge tipe soal -->
        <div class="mb-3">
            <span class="badge @badgeClass me-2">@badgeLabel</span>
            <span class="text-muted small">Preview tampilan pekerja</span>
        </div>
```

**Pattern target (AFTER):**
```razor
@using HcPortal.Models
@model PackageQuestion

@{
    var qtype = Model.QuestionType ?? "MultipleChoice";
}

<div class="card border-0">
    <div class="card-body p-0">
        <!-- Badge tipe soal -->
        <div class="mb-3">
            <span class="badge @QuestionTypeLabels.BadgeClass(qtype) me-2">@QuestionTypeLabels.Short(qtype)</span>
            <span class="text-muted small">Preview tampilan pekerja</span>
        </div>
```

**Notes (per RESEARCH.md Pitfall #4):**
- Hapus 2 local var `badgeClass` dan `badgeLabel` di lines 6-7 (verified hanya digunakan 1x di line 14)
- Variabel `qtype` (line 5) **DIPERTAHANKAN** — masih dipakai di lines 21, 43, 58 untuk switch radio/checkbox/textarea logic
- `@using HcPortal.Models` line 1 sudah ada — boleh dihapus karena `_ViewImports.cshtml` sudah global, tapi keeping is harmless (planner discretion)
- Sisanya (radio/checkbox switch lines 21-66, ScoreValue display lines 68-74) **TIDAK DIUBAH**

---

### 4. `Views/CMP/StartExam.cshtml` (view, server-render badge inline soal — simetrisasi)

**Analog:** Self (lines 100-108 if/elseif badge inline) + admin pattern (Manage tabel) untuk warna konsisten.

**Existing code (BEFORE) — lines 95-108:**
```razor
@foreach (var q in pageQuestions)
{
    var qtype = q.QuestionType ?? "MultipleChoice";
    <div class="card shadow-sm mb-3 border-0" id="qcard_@q.QuestionId">
        <div class="card-body p-3">
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

**Pattern target (AFTER) — simetris, semua tipe punya badge (D-09, D-16):**
```razor
@foreach (var q in pageQuestions)
{
    var qtype = q.QuestionType ?? "MultipleChoice";
    <div class="card shadow-sm mb-3 border-0" id="qcard_@q.QuestionId">
        <div class="card-body p-3">
            <p class="fw-bold mb-3">
                <span class="badge bg-primary me-2">@q.DisplayNumber</span>
                <span class="badge @QuestionTypeLabels.BadgeClass(qtype) ms-1 small">@QuestionTypeLabels.Short(qtype)</span>
                @q.QuestionText
            </p>
```

**Notes (per D-09, D-16, RESEARCH.md Pitfall #5):**
- Replace if/elseif block (lines 102-106) dengan single `<span>` unconditional
- **MC sekarang punya badge** (asimetris → simetris) — sebelumnya hanya MA + Essay
- **INTENDED color shift:** MA badge dulu `bg-secondary` (grey) → sekarang `bg-primary` (blue). Essay dulu `bg-secondary` → sekarang `bg-info text-dark` (cyan + dark text). Ini **alignment ke admin tabel** (Manage), bukan regression.
- Class chain `me-2` (display number) dan `ms-1 small` (type badge) — match existing styling. Helper `BadgeClass` returns base color, badge variant (small/me-2/ms-1) tetap tag-level.

---

### 5. `Views/CMP/ExamSummary.cshtml` (view, server-render badge BARU — SCOPE EXTENSION D-10)

**Analog:** `Views/CMP/StartExam.cshtml` lines 100-108 (badge inline soal pattern, post-edit) — konsistensi worker UX antara mengerjakan ujian dan melihat summary.

**Existing code (BEFORE) — lines 46-55:**
```razor
@foreach (var item in Model)
{
    <tr class="@(item.IsAnswered ? "" : "table-warning")">
        <td class="fw-bold">@item.DisplayNumber</td>
        <td>
            <span class="text-truncate d-inline-block" style="max-width: 420px;"
                  title="@item.QuestionText">
                @item.QuestionText
            </span>
        </td>
```

**Pattern target (AFTER) — append badge sebelum text:**
```razor
@foreach (var item in Model)
{
    <tr class="@(item.IsAnswered ? "" : "table-warning")">
        <td class="fw-bold">@item.DisplayNumber</td>
        <td>
            <span class="badge @QuestionTypeLabels.BadgeClass(item.QuestionType) small me-2">@QuestionTypeLabels.Short(item.QuestionType)</span>
            <span class="text-truncate d-inline-block" style="max-width: 380px;"
                  title="@item.QuestionText">
                @item.QuestionText
            </span>
        </td>
```

**Notes (per D-10, RESEARCH.md Pattern 4):**
- **Sebelumnya tidak ada user-facing label** untuk tipe soal di summary — D-10 SCOPE EXTENSION menambah badge
- `item.QuestionType` (`ExamSummaryItem` model) — string, helper handles null via `_` discard (verified di line 57 existing kode `item.QuestionType == "MultipleAnswer"` — confirms string type)
- `max-width: 420px` reduced ke `380px` (~40px reserved untuk badge inline) — atau gunakan flex layout (planner discretion)
- Class chain `small me-2` — `me-2` (margin-end) lebih tepat dari `ms-1` di awal cell
- Lines 56-89 (cell "Jawaban Anda" dengan `if/else if` per qtype) **TIDAK DIUBAH** — JS reads enum value, label irrelevant

---

### 6. `Views/Admin/ImportPackageQuestions.cshtml` (view, static button text)

**Analog:** Self (lines 37-50 button group + lines 31-36 bullet helper text).

#### 6a. Button label (lines 38, 41)

**Existing code (BEFORE) — lines 37-50:**
```razor
<div class="btn-group flex-wrap gap-1">
    <a href="@Url.Action("DownloadQuestionTemplate", "AssessmentAdmin", new { type = "MC" })" class="btn btn-outline-success btn-sm">
        <i class="bi bi-download me-1"></i>Template MC
    </a>
    <a href="@Url.Action("DownloadQuestionTemplate", "AssessmentAdmin", new { type = "MA" })" class="btn btn-outline-success btn-sm">
        <i class="bi bi-download me-1"></i>Template MA
    </a>
    <a href="@Url.Action("DownloadQuestionTemplate", "AssessmentAdmin", new { type = "Essay" })" class="btn btn-outline-success btn-sm">
        <i class="bi bi-download me-1"></i>Template Essay
    </a>
    <a href="@Url.Action("DownloadQuestionTemplate", "AssessmentAdmin", new { type = "Universal" })" class="btn btn-outline-primary btn-sm">
        <i class="bi bi-download me-1"></i>Template Universal
    </a>
</div>
```

**Pattern target (AFTER) — D-12 update 2 button text only:**
```razor
<div class="btn-group flex-wrap gap-1">
    <a href="@Url.Action("DownloadQuestionTemplate", "AssessmentAdmin", new { type = "MC" })" class="btn btn-outline-success btn-sm">
        <i class="bi bi-download me-1"></i>Template Single Choice
    </a>
    <a href="@Url.Action("DownloadQuestionTemplate", "AssessmentAdmin", new { type = "MA" })" class="btn btn-outline-success btn-sm">
        <i class="bi bi-download me-1"></i>Template Multiple Answers
    </a>
    <a href="@Url.Action("DownloadQuestionTemplate", "AssessmentAdmin", new { type = "Essay" })" class="btn btn-outline-success btn-sm">
        <i class="bi bi-download me-1"></i>Template Essay
    </a>
    <a href="@Url.Action("DownloadQuestionTemplate", "AssessmentAdmin", new { type = "Universal" })" class="btn btn-outline-primary btn-sm">
        <i class="bi bi-download me-1"></i>Template Universal
    </a>
</div>
```

**Notes (per D-12):**
- `type = "MC"` dan `type = "MA"` query param **TETAP** (controller action `DownloadQuestionTemplate` reads via query param, file binary tetap)
- Hanya text inside `<a>` tag yang berubah: "Template MC" → "Template Single Choice"; "Template MA" → "Template Multiple Answers"
- "Template Essay" dan "Template Universal" **TIDAK DIUBAH**
- Hard-code text (bukan helper call) — alasan: button text static, tidak butuh helper lookup. Konsistensi dengan dropdown alternative.

#### 6b. Bullet helper text (lines 32-35) — OPTIONAL (planner discretion per D-12)

**Existing code:**
```razor
<ul class="mb-2 small text-muted">
    <li><strong>QuestionType:</strong> <code>MultipleChoice</code> (default jika kosong), <code>MultipleAnswer</code>, atau <code>Essay</code></li>
    <li><strong>MA — Jawaban Benar:</strong> huruf dipisah koma, contoh: <code>A,C</code> atau <code>A,B,D</code></li>
    <li><strong>Essay:</strong> Opsi A-D dan Jawaban Benar dikosongkan. Kolom Rubrik wajib diisi.</li>
    <li>File lama tanpa kolom QuestionType akan diimpor sebagai MultipleChoice secara otomatis.</li>
</ul>
```

**Pattern target (OPTIONAL addition — keep enum value untuk developer-facing schema doc per D-12):**
```razor
<ul class="mb-2 small text-muted">
    <li><strong>QuestionType:</strong> <code>MultipleChoice</code> (default jika kosong), <code>MultipleAnswer</code>, atau <code>Essay</code> <span class="text-muted">— di UI ditampilkan sebagai "Single Choice" / "Multiple Answers" / "Essay"</span></li>
    <li><strong>MA — Jawaban Benar:</strong> huruf dipisah koma, contoh: <code>A,C</code> atau <code>A,B,D</code></li>
    <li><strong>Essay:</strong> Opsi A-D dan Jawaban Benar dikosongkan. Kolom Rubrik wajib diisi.</li>
    <li>File lama tanpa kolom QuestionType akan diimpor sebagai MultipleChoice secara otomatis.</li>
</ul>
```

**Notes (per D-12, D-18):**
- `<code>` tag wrap enum value `MultipleChoice`/`MultipleAnswer`/`Essay` **TETAP** — backward compat Excel import file lama
- Optional addition: catatan label baru di parenthetical span untuk clarity
- Planner decide — minor, no functional impact

---

### 7. `Controllers/AssessmentAdminController.cs` (controller, TempData flash error)

**Analog:** Self (16 lokasi `TempData["Error"]` existing pattern di file sama) + 4 lokasi target lines 4688, 4693, 4829, 4834.

**Existing imports (verified line 6):**
```csharp
using HcPortal.Models;
```
✅ **Already exists** — Pitfall #3 mitigated. Helper `QuestionTypeLabels` immediately accessible.

**Existing code (BEFORE) — lines 4685-4695 (CreateQuestion validation):**
```csharp
// Validate per type (D-07)
var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
if (questionType == "MultipleChoice" && correctCount != 1)
{
    TempData["Error"] = "Pilihan Ganda hanya boleh memiliki 1 jawaban benar.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
if (questionType == "MultipleAnswer" && correctCount < 2)
{
    TempData["Error"] = "Multiple Answer membutuhkan minimal 2 jawaban benar.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```

**Pattern target (AFTER) — D-11 hybrid English+Indonesian:**
```csharp
// Validate per type (D-07)
var correctCount = (correctA ? 1 : 0) + (correctB ? 1 : 0) + (correctC ? 1 : 0) + (correctD ? 1 : 0);
if (questionType == "MultipleChoice" && correctCount != 1)
{
    TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
if (questionType == "MultipleAnswer" && correctCount < 2)
{
    TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleAnswer")} membutuhkan minimal 2 jawaban benar.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```

**Output rendered di view (TempData flash):**
- `"Single Choice hanya boleh memiliki 1 jawaban benar."`
- `"Multiple Answers membutuhkan minimal 2 jawaban benar."`

**Apply ke 4 lokasi:**

| Line | Method | Existing String | Replace With |
|------|--------|-----------------|--------------|
| 4688 | CreateQuestion (MC validation) | `"Pilihan Ganda hanya boleh memiliki 1 jawaban benar."` | `$"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar."` |
| 4693 | CreateQuestion (MA validation) | `"Multiple Answer membutuhkan minimal 2 jawaban benar."` | `$"{QuestionTypeLabels.Short("MultipleAnswer")} membutuhkan minimal 2 jawaban benar."` |
| 4829 | EditQuestion (MC validation) | `"Pilihan Ganda hanya boleh memiliki 1 jawaban benar."` | `$"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar."` |
| 4834 | EditQuestion (MA validation) | `"Multiple Answer membutuhkan minimal 2 jawaban benar."` | `$"{QuestionTypeLabels.Short("MultipleAnswer")} membutuhkan minimal 2 jawaban benar."` |

**Notes (per D-11, RESEARCH.md Pattern 5):**
- C# 6+ string interpolation `$"..."` — sudah idiomatic di codebase
- Pakai **literal string** `"MultipleChoice"` / `"MultipleAnswer"` (enum value), bukan `questionType` parameter — defensive: pastikan label match validation context, bukan input dependent
- 4 lokasi punya structure identik (CreateQuestion + EditQuestion masing-masing punya MC + MA validation) — same pattern × 4
- Essay validation line 4696/4837 (`"Rubrik wajib diisi untuk soal Essay."`) **TIDAK DIUBAH** — D-11 hanya cover MC/MA flash error

---

### 8. Documentation Files (8 files, sed-replace context-aware)

**Analog:** N/A — static HTML/MD/PY files, manual edit per occurrence.

**Files & expected occurrences (per RESEARCH.md Pitfall #2 — total 17, BUKAN 14 seperti CONTEXT.md klaim):**

| File | Path | Expected Occurrences | Replace Patterns |
|------|------|----------------------|------------------|
| `Draft-BAB-X-INSTRUKSI-KERJA.html` | `wwwroot/documents/TKI/` | 2 | "Pilihan Ganda" → "Single Choice" / "Multi Jawaban" → "Multiple Answers" |
| `Draft-BAB-X-INSTRUKSI-KERJA-outline.md` | `wwwroot/documents/TKI/` | 2 | sama |
| `generate_bab_x.py` | `wwwroot/documents/TKI/` | 2 | sama (Python source) |
| `Panduan-Penggunaan-Website-HC-Portal-KPB.html` | `wwwroot/documents/guides/` | 1+ | sama + cek "Multiple Choice" hits |
| `Release-Notes-HC-Portal-KPB.html` | `wwwroot/documents/guides/` | 2-3 | sama + line 327-328 punya "Multiple Choice (MC)" + "pilihan ganda, satu jawaban benar" — perhatikan dual context |
| `Penjelasan-Halaman-PortalHC-KPB.html` | `wwwroot/documents/guides/` | 3-4 | sama + line 532 "Multiple Choice, Multiple Answer, dan Essay" |
| `Struktur-Website-PortalHC-KPB.html` | `wwwroot/documents/guides/` | 2-3 | sama + line 430 "Multiple Choice, Multiple Answer, Essay" |
| `Persiapan-Test-Manual-Assessment.html` | `docs/` | 1-2 | sama + line 396 "Multiple Choice" |

**Replace pattern (context-aware, BUKAN blind sed per D-13 + RESEARCH.md Pitfall #1):**

| Old | New | Konteks |
|-----|-----|---------|
| `Pilihan Ganda` (label tipe MC) | `Single Choice` | Capitalized, di tabel/list label tipe |
| `pilihan ganda` (deskripsi generic) | review case-by-case (mungkin keep, mungkin rephrase) | Lowercase, prosa naratif |
| `Multi Jawaban` | `Multiple Answers` | Label tipe MA |
| `Multiple Answer` (singular) | `Multiple Answers` (plural) | Match D-01 final wording |
| `Multiple Choice` (jika reference label MC type) | `Single Choice` | English label, sebelumnya pakai MC convention |
| `MC` / `MA` (singkatan dalam parenthetical) | optional update — planner decide | mis. `Single Choice (MC)` keep "MC" sebagai legacy abbrev |

**Pattern command untuk planner (grep dulu, baru sed):**
```bash
# Identify all occurrences pre-sed (per RESEARCH.md A3 — incomplete CONTEXT.md grep)
grep -rn "Pilihan Ganda\|Pilihan ganda\|Multi Jawaban\|Multiple Answer\|Multiple Choice" wwwroot/documents/ docs/ 2>/dev/null
```

**Notes (per D-13, D-14, RESEARCH.md Pitfall #1, #2):**
- **Manual review per occurrence** — bukan blind sed
- "Pilihan Ganda" 2 makna: (1) label tipe MC = ganti "Single Choice"; (2) deskripsi generic "pilihan ganda" lowercase = review case-by-case
- "Multiple Choice" string juga muncul (CONTEXT.md miss) — total occurrences 17, bukan 14
- PDF panduan + screenshot training **deferred manual user task** (D-14) — tidak block phase
- File binary `.xlsx` template **TIDAK DIUBAH** (D-18, deferred #5)

---

## Shared Patterns

### A. Helper Class Static Style (cross-cut: 1 file definition + 6 callers)

**Source pattern:** `Models/AssessmentConstants.cs` (existing) + `QuestionTypeLabels.cs` (NEW)

**Apply to:**
- View Razor (4 file): `@QuestionTypeLabels.Short(qtype)`, `@QuestionTypeLabels.BadgeClass(qtype)`, `@QuestionTypeLabels.Long("MultipleChoice")` (dropdown alternatif)
- Controller (1 file): `QuestionTypeLabels.Short("MultipleChoice")` di string interpolation

**Format access:**
- View Razor: `@QuestionTypeLabels.{Method}(arg)` — auto-encoded by Razor `@`
- Controller C#: `QuestionTypeLabels.{Method}(arg)` — direct call, no `@`
- Namespace `HcPortal.Models` — view via `_ViewImports.cshtml` line 2 (global), controller via `using HcPortal.Models;` line 6 (verified)

### B. Bootstrap Badge Pattern (Bootstrap 5.3 — UNCHANGED)

**Source:** Existing 3 view (Manage line 79, Preview line 14, StartExam line 101) — sudah dipakai dengan format `<span class="badge {color-class}">{label}</span>`.

**Apply to:** Helper-driven 4 view (4 callers + 1 NEW di ExamSummary).

**Format persis:**
```razor
<span class="badge @QuestionTypeLabels.BadgeClass(qtype) {extra-classes}">@QuestionTypeLabels.Short(qtype)</span>
```

**Variant per surface:**
| Surface | Extra Classes |
|---------|---------------|
| Manage tabel | `small` |
| _PreviewQuestion | `me-2` |
| StartExam (inline soal) | `ms-1 small` |
| ExamSummary (NEW D-10) | `small me-2` |

### C. TempData Flash Error (UNCHANGED)

**Source:** `Controllers/AssessmentAdminController.cs` 16 lokasi `TempData["Error"] = "..."` — pattern existing.

**Apply to:** 4 lokasi target (lines 4688, 4693, 4829, 4834) — only string content yang berubah, mechanism tetap.

**Format:**
```csharp
TempData["Error"] = $"{QuestionTypeLabels.Short("EnumValue")} {kalimat instruksi Indonesia}.";
return RedirectToAction("ManagePackageQuestions", new { packageId });
```

**Konsistensi:** existing flash error di codebase pakai pure Indonesian (e.g., line 4698 "Rubrik wajib diisi untuk soal Essay."). Phase 305 introduces hybrid English+Indonesian — sesuai D-11 explicit choice (label tipe English, kalimat Indonesia).

### D. Razor `@using` (NO CHANGE NEEDED)

**Source:** `Views/_ViewImports.cshtml` line 2 — `@using HcPortal.Models` (global)

**Apply to:** All 4 view targets — auto-accessible, **TIDAK perlu** tambah `@using` per-file.

**Verified:**
- `Views/_ViewImports.cshtml` line 2: `@using HcPortal.Models` ✅
- `Views/CMP/_ViewImports.cshtml` — file **tidak ada** (verified via Glob), inheritance otomatis dari parent ✅
- `_PreviewQuestion.cshtml` line 1 sudah punya `@using HcPortal.Models` (redundant tapi harmless) ✅

---

## No Analog Found

| File | Alasan tidak ada analog |
|------|-------------------------|
| 8 file dokumentasi (`wwwroot/documents/` + `docs/`) | Static HTML/MD/PY non-Razor — tidak ada helper, manual sed-replace per occurrence |

**Mitigasi:** RESEARCH.md Code Examples + RESEARCH.md Pitfall #1 #2 sudah provide pattern guidance untuk context-aware sed. Planner decide granularity (atomic per file vs batch sed).

---

## Cross-Reference Quick Lookup

| Need | Reference File | Line(s) |
|------|---------------|---------|
| Static helper class style (block-scoped namespace, public static class) | `Models/AssessmentConstants.cs` | 1-40 |
| Existing inline badge ternary (Manage tabel) | `Views/Admin/ManagePackageQuestions.cshtml` | 67-70, 79 |
| Existing inline badge ternary (Preview) | `Views/Admin/_PreviewQuestion.cshtml` | 5-7, 14 |
| Existing if/elseif badge inline (StartExam) | `Views/CMP/StartExam.cshtml` | 100-108 |
| Worker summary cell (target SCOPE EXTENSION D-10) | `Views/CMP/ExamSummary.cshtml` | 46-55 |
| Existing button text Template MC/MA (target rename) | `Views/Admin/ImportPackageQuestions.cshtml` | 38-43 |
| Existing dropdown text MC/MA/Essay (target rename) | `Views/Admin/ManagePackageQuestions.cshtml` | 132-136 |
| Existing TempData flash error (target rename) — Create | `Controllers/AssessmentAdminController.cs` | 4688, 4693 |
| Existing TempData flash error (target rename) — Edit | `Controllers/AssessmentAdminController.cs` | 4829, 4834 |
| `using HcPortal.Models` (verified ada di controller) | `Controllers/AssessmentAdminController.cs` | 6 |
| `@using HcPortal.Models` (verified global di view) | `Views/_ViewImports.cshtml` | 2 |
| JS handler enum value reads — TIDAK DIUBAH | `Views/Admin/ManagePackageQuestions.cshtml` | 297-311, 356, 393-394 |
| Property `QuestionType` nullable string (model — helper signature match) | `Models/AssessmentPackage.cs` | 48 |
| Property `QuestionType` default string (view model) | `Models/PackageExamViewModel.cs` | 37, 62 |
| Property `QuestionType` default string (analytics) | `Models/AnalyticsDashboardViewModel.cs` | 86 |

---

## Metadata

**Analog search scope:**
- `Models/` — `AssessmentConstants.cs` (style precedent), `AssessmentPackage.cs`, `PackageExamViewModel.cs`, `AnalyticsDashboardViewModel.cs` (property reference)
- `Views/Admin/` — `ManagePackageQuestions.cshtml`, `_PreviewQuestion.cshtml`, `ImportPackageQuestions.cshtml` (target + self-analog)
- `Views/CMP/` — `StartExam.cshtml`, `ExamSummary.cshtml` (target + self-analog)
- `Views/_ViewImports.cshtml` (global imports verification)
- `Controllers/` — `AssessmentAdminController.cs` (target + import verification)
- `wwwroot/documents/` (TKI + guides), `docs/` (8 file dokumentasi)
- `.planning/phases/304-ui-label-polish-login-wib/304-PATTERNS.md` (precedent format)

**Files scanned:** 12 source files + 1 precedent PATTERNS.md
**Pattern extraction date:** 2026-04-28
**Phase:** 305-question-type-naming-clarity

---

*Generated by gsd-pattern-mapper. Konsumsi oleh gsd-planner untuk PLAN.md per file/wave.*
