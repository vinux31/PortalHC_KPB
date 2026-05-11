# Phase 305: Question Type Naming Clarity - Context

**Gathered:** 2026-04-28
**Status:** Ready for planning
**REQ:** LBL-01

<domain>
## Phase Boundary

Rename **user-facing label** untuk tipe soal `MultipleChoice` (MC) dan `MultipleAnswer` (MA) di seluruh surface UI agar tidak rancu, dengan **single source of truth** lewat C# helper class. Internal enum/string DB **tidak** diubah.

**Surface yang di-update:**

1. **`Models/QuestionTypeLabels.cs`** (NEW) — static helper: `Long(type)` / `Short(type)` / `BadgeClass(type)`
2. **`Views/Admin/ManagePackageQuestions.cshtml`** — badge tabel (line 70) + dropdown form (lines 133-134)
3. **`Views/Admin/_PreviewQuestion.cshtml`** — badge preview (line 7)
4. **`Views/CMP/StartExam.cshtml`** — badge inline soal (lines 102-106), tambah badge MC juga
5. **`Views/CMP/ExamSummary.cshtml`** — tambah badge baru di kolom "Pertanyaan"
6. **`Views/Admin/ImportPackageQuestions.cshtml`** — bullet text + button label "Template MC/MA" → label panjang
7. **`Controllers/AssessmentAdminController.cs`** — flash error TempData (lines 4688, 4693, 4829, 4834)
8. **8 file dokumentasi HTML** — `wwwroot/documents/` (7 file) + `docs/` (1 file)
9. **PDF panduan + screenshot training** — flagged untuk regen manual oleh user (deferred, tracked)

**Tidak termasuk dalam phase ini:** Migrasi DB, rename enum string `MultipleChoice`/`MultipleAnswer`, rename property model `QuestionType`, multi-language i18n (terjemahan), tooltip/info-icon legend transisi, button color scheme change, A/B test feature flag, Excel import template content (file binary tidak diubah).

</domain>

<decisions>
## Implementation Decisions

### LBL-01: Wording & Surface Strategy

- **D-01:** **Wording final** mengikuti standar Moodle/Canvas LMS (English):
  - MC = `"Single Choice"`
  - MA = `"Multiple Answers"`
  - Essay = `"Essay"` (tidak berubah dari existing)
  - **Rasional:** User explicit choice setelah riset Kemendikbud AKM ("Pilihan Ganda Kompleks") + Moodle ("Single answer / Multiple answers") + Canvas ("Multiple Choice / Multiple Answer"). User pilih pure English untuk match international LMS convention.

- **D-02:** **Long form vs short form per surface:**
  - **Long form** (form admin dropdown): `"Single Choice (1 jawaban benar)"` / `"Multiple Answers (≥2 jawaban benar)"` / `"Essay"`
  - **Short form** (badge tabel/exam/preview): `"Single Choice"` / `"Multiple Answers"` / `"Essay"`
  - **Hybrid bahasa:** Label tipe Inggris, keterangan numerik Indonesia. Konsisten dengan rest of app berbahasa Indonesia. Worker dapat hint Indonesia via parentheses.

### LBL-01: Single Source of Truth

- **D-03:** **Extract C# helper** `Models/QuestionTypeLabels.cs` (NEW file) — static class dengan 3 method:
  ```csharp
  public static class QuestionTypeLabels {
      public static string Long(string type) => type switch {
          "MultipleChoice" => "Single Choice (1 jawaban benar)",
          "MultipleAnswer" => "Multiple Answers (≥2 jawaban benar)",
          "Essay" => "Essay",
          _ => "Single Choice (1 jawaban benar)"
      };

      public static string Short(string type) => type switch {
          "MultipleChoice" => "Single Choice",
          "MultipleAnswer" => "Multiple Answers",
          "Essay" => "Essay",
          _ => "Single Choice"
      };

      public static string BadgeClass(string type) => type switch {
          "MultipleChoice" => "bg-secondary",
          "MultipleAnswer" => "bg-primary",
          "Essay" => "bg-info text-dark",
          _ => "bg-secondary"
      };
  }
  ```
- **D-04:** **Caller substitution:**
  - View Razor (`.cshtml`): `@QuestionTypeLabels.Short(qtype)`, `@QuestionTypeLabels.BadgeClass(qtype)`
  - Controller C#: `QuestionTypeLabels.Short("MultipleChoice")` untuk flash error
  - Dropdown options: hard-code lewat `@QuestionTypeLabels.Long("MultipleChoice")` interpolated value (atau static text — keduanya valid; planner pilih)
- **D-05:** Default fallback (kasus null/unknown qtype) = "Single Choice" / `"bg-secondary"` (mirroring existing pattern `qtype ?? "MultipleChoice"`).
- **D-06:** Helper diletakkan di `Models/QuestionTypeLabels.cs` (bukan `Helpers/` atau `Utilities/`) karena semantically domain-related ke property `QuestionType` di `AssessmentPackage`/`PackageExamViewModel`/`AnalyticsDashboardViewModel`. Namespace: `HcPortal.Models` (mengikuti pattern `Models/AssessmentCategory.cs` & `Models/AssessmentConstants.cs`). **Verified:** `Views/_ViewImports.cshtml` sudah `@using HcPortal.Models` global — semua view langsung dapat akses helper tanpa per-file `@using`.

### LBL-01: View File Updates

- **D-07:** `ManagePackageQuestions.cshtml`:
  - Line 69-70 (badge logic): replace inline ternary dengan `@QuestionTypeLabels.Short(qtype)` + `@QuestionTypeLabels.BadgeClass(qtype)`
  - Lines 133-135 (dropdown options): update text ke `Single Choice (1 jawaban benar)` / `Multiple Answers (≥2 jawaban benar)` / `Essay`. **Value attribute (`MultipleChoice`/`MultipleAnswer`/`Essay`) tetap** — DB enum tidak berubah.
- **D-08:** `_PreviewQuestion.cshtml`:
  - Line 6-7 (badge logic): replace inline ternary dengan helper call (sama pattern dengan D-07).
  - Sisanya (radio/checkbox switch, Essay textarea) **tidak berubah**.
- **D-09:** `StartExam.cshtml`:
  - Lines 100-106 (badge inline soal): tambah badge MC juga (sebelumnya hanya MA & Essay yang punya badge). **Asimetris → simetris** — semua tipe punya badge.
  - Pakai `@QuestionTypeLabels.Short(qtype)` + `@QuestionTypeLabels.BadgeClass(qtype)`.
- **D-10:** `ExamSummary.cshtml` **[SCOPE EXTENSION]:** Tambah badge tipe soal di kolom "Pertanyaan" (sebelumnya tidak ada user-facing label). Konsisten dengan StartExam — worker yang baru selesai ujian melihat summary dengan tampilan tipe yang sama. Implementation: append badge sebelum `@item.QuestionText` di line 53.

### LBL-01: Controller & Import Helper

- **D-11:** `AssessmentAdminController.cs` lines 4688, 4693, 4829, 4834 — flash error message **hybrid English+Indonesian**:
  ```
  TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleChoice")} hanya boleh memiliki 1 jawaban benar.";
  TempData["Error"] = $"{QuestionTypeLabels.Short("MultipleAnswer")} membutuhkan minimal 2 jawaban benar.";
  ```
  Output: `"Single Choice hanya boleh memiliki 1 jawaban benar."` dan `"Multiple Answers membutuhkan minimal 2 jawaban benar."`
- **D-12:** `ImportPackageQuestions.cshtml`:
  - Lines 38-43 (button Template): "Template MC" → "Template Single Choice"; "Template MA" → "Template Multiple Answers"; Essay & Universal **tetap**.
  - Line 32-35 (bullet helper text): tetap pakai enum value `MultipleChoice`/`MultipleAnswer` (developer-facing schema doc, BUKAN label). Boleh **opsional** tambah catatan di bullet: "Label di UI menampilkan sebagai 'Single Choice' / 'Multiple Answers'" jika merasa perlu — planner decide.

### Documentation Updates

- **D-13:** **8 file HTML** akan di-sed-replace:
  - 7 file di `wwwroot/documents/`: `Draft-BAB-X-INSTRUKSI-KERJA.html`, `Draft-BAB-X-INSTRUKSI-KERJA-outline.md`, `generate_bab_x.py`, `Panduan-Penggunaan-Website-HC-Portal-KPB.html`, `Release-Notes-HC-Portal-KPB.html`, `Penjelasan-Halaman-PortalHC-KPB.html`, `Struktur-Website-PortalHC-KPB.html`
  - 1 file di `docs/`: `Persiapan-Test-Manual-Assessment.html`
  - **Replace pattern:** "Pilihan Ganda" (jika konteks MC) → "Single Choice"; "Multi Jawaban" → "Multiple Answers"; "Multiple Answer" → "Multiple Answers".
  - **Hati-hati:** "Pilihan Ganda" muncul juga di nilai enum AKM context — context-by-context review, bukan blind sed.
- **D-14:** **PDF panduan + screenshot training** — di-flag sebagai **deferred manual task** untuk user. Tracked di section deferred. Tidak block phase 305 completion.
- **D-15:** **E2E Playwright tests** (`tests/e2e/`) — verified ZERO match pada label lama. Tidak perlu update.

### Worker UX

- **D-16:** **Tambah badge MC juga** di `StartExam.cshtml` (D-09): worker melihat tipe semua soal — visual hierarchy konsisten. Badge order di soal:
  ```html
  <span class="badge bg-primary me-2">@q.DisplayNumber</span>
  <span class="badge @QuestionTypeLabels.BadgeClass(qtype) ms-1 small">@QuestionTypeLabels.Short(qtype)</span>
  @q.QuestionText
  ```

### Validation & Behavior Guards

- **D-17:** **Internal enum value tidak berubah**: `"MultipleChoice"`, `"MultipleAnswer"`, `"Essay"` di DB column `PackageQuestions.QuestionType` tetap. Semua DB query, EF migration, model property name (`QuestionType`) tidak diubah.
- **D-18:** **Backward compat:** Excel import file lama yang berisi value `MultipleChoice`/`MultipleAnswer` di kolom `QuestionType` tetap berfungsi (per import logic di `AssessmentAdminController.ImportQuestions`). Helper text di `ImportPackageQuestions.cshtml` line 32-35 tetap reference enum value.
- **D-19:** **JS handler di view `ManagePackageQuestions.cshtml`** (lines 297-311, 356, 393-394) — JS membaca `qtype` value via `data-question-type` atau dropdown.value. Reference ke string `'MultipleChoice'`/`'MultipleAnswer'`/`'Essay'` (enum value) **tetap** — JS logic tidak berubah.
- **D-20:** **DB query verifikasi** (success criteria #5): planner tambah task untuk run `SELECT DISTINCT QuestionType FROM PackageQuestions` post-implementasi untuk konfirmasi enum value masih `MultipleChoice` / `MultipleAnswer` / `Essay`.

### Claude's Discretion

- **Detail urutan badge** di `StartExam.cshtml` (apakah pakai `me-2`/`ms-1`/`small`) — pilih yang konsisten dengan existing styling.
- **Implementasi `QuestionTypeLabels.cs`** — `switch` expression vs `Dictionary<string,string>` lookup vs `if/else if/else` — pilih yang paling readable. Switch expression rekomendasi (C# 8+, sudah dipakai di codebase).
- **Apakah `BadgeClass` return string termasuk `text-dark`** atau split jadi separate method `TextClass(type)` — pilih satu, fungsi tetap.
- **Wording bullet ImportPackageQuestions optional addition (D-12)** — tambah catatan label baru atau biarkan. Planner decide.
- **Order/flow update file** — atomic commit per file vs single commit semua, atau group by file type (model + view + controller + docs). Phase 305 minimal-risk → planner pilih granularity.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Source & Requirements
- `.planning/ROADMAP.md` §"v15.0 Audit Findings 27 April 2026" — Phase 305 success criteria (5 items)
- `.planning/REQUIREMENTS.md` §"Question Type Labels" (LBL-01) + §"Out of Scope" (eksklusi rename enum/string)
- `.planning/STATE.md` — current focus & deferred items

### View Files (target perubahan)
- `Views/Admin/ManagePackageQuestions.cshtml` — target LBL-01
  - Lines 67-70 (badge logic dalam tabel `@foreach`)
  - Lines 130-137 (dropdown form `<select id="QuestionType">`)
  - Lines 297-311 (JS `applyQTypeSwitch` — **tidak diubah**, baca enum value)
- `Views/Admin/_PreviewQuestion.cshtml` — target LBL-01
  - Lines 5-7 (badge variable)
  - Line 14 (badge HTML render)
- `Views/CMP/StartExam.cshtml` — target LBL-01
  - Lines 95-108 (badge inline soal `@foreach pageQuestions`)
- `Views/CMP/ExamSummary.cshtml` — target LBL-01 (SCOPE EXTENSION D-10)
  - Lines 46-89 (`@foreach Model` rendering kolom Pertanyaan + Jawaban)
- `Views/Admin/ImportPackageQuestions.cshtml` — target LBL-01
  - Lines 31-36 (bullet helper text — **enum value, opsional update**)
  - Lines 37-50 (4 button Template — D-12 update 2 button)
- `Controllers/AssessmentAdminController.cs` — target LBL-01
  - Lines 4685-4695 (validation MC: 1 jawaban) — D-11
  - Lines 4825-4837 (validation MA: ≥2 jawaban) — D-11
  - Tidak ubah method signature, ImportQuestions logic, atau model binding

### NEW File (helper)
- `Models/QuestionTypeLabels.cs` — D-03 spec, namespace `HcPortal.Models`
  - **Pattern reference:** `Models/AssessmentConstants.cs` (existing static-style file di Models/) untuk style guidance
  - **Property `QuestionType` referenced di:** `Models/AssessmentPackage.cs` line 48 (nullable string), `Models/PackageExamViewModel.cs` line 37 (default `"MultipleChoice"`) + line 62 (nullable), `Models/AnalyticsDashboardViewModel.cs` line 86 (default `"MultipleChoice"`)

### Documentation Files (target update)
- `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA.html` — 2 occurrences
- `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md` — 2 occurrences
- `wwwroot/documents/TKI/generate_bab_x.py` — 2 occurrences (Python source untuk generate HTML)
- `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html` — 1 occurrence
- `wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html` — 2 occurrences
- `wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html` — 3 occurrences
- `wwwroot/documents/guides/Struktur-Website-PortalHC-KPB.html` — 2 occurrences
- `docs/Persiapan-Test-Manual-Assessment.html` — 1 occurrence
- **Total:** 8 file, 14 occurrences (label "Pilihan Ganda" / "Multi Jawaban" / "Multiple Answer")

### Pattern Reference
- `Phase 304 D-09, D-15` (`.planning/phases/304-ui-label-polish-login-wib/304-CONTEXT.md`) — pattern minimal-change inline. **Phase 305 PARTIAL deviasi:** extract helper class karena 6 touch points (Phase 304 hanya 2). Justified deviasi.
- `Models/AssessmentPackage.cs` line 48 — model existing dengan property `QuestionType` (string?). Helper `QuestionTypeLabels` semantically related.
- `Models/AssessmentConstants.cs` — file existing di `Models/` dengan style "static configuration" — pattern reference untuk struktur `QuestionTypeLabels.cs`

### Convention References
- `.planning/codebase/STACK.md` — ASP.NET Core MVC, Razor view, EF Core, Bootstrap 5.3
- `.planning/codebase/CONVENTIONS.md` §"Models" — namespace `HcPortal.Models`, static helper class lazim di `Models/`
- `.planning/codebase/STRUCTURE.md` — directory layout `Controllers/`, `Models/`, `Views/`, `wwwroot/`

### Out of Scope (eksklusi explicit)
- DB enum/migration
- JS handler logic di `ManagePackageQuestions.cshtml` (line 297-311) — baca enum value, tidak ubah
- Excel import template binary file (`question_import_template_GAST.xlsx`, dll) — content tidak edit
- PDF regenerasi otomatis — manual user task
- Screenshot regen otomatis — manual user task
- Tooltip transisi/legend "MC = Single Choice" — out per success criteria
- A/B test atau feature flag toggle — out, atomic swap

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **Bootstrap 5.3 badge pattern** sudah dipakai di 3 view (`ManagePackageQuestions`, `_PreviewQuestion`, `StartExam`). Pattern: `<span class="badge {color-class}">{label}</span>`. Reuse persis lewat helper `BadgeClass()` + `Short()`.
- **Razor `@using HcPortal.Models`** sudah **GLOBAL** via `Views/_ViewImports.cshtml` line 2 (verified). Semua view di `Views/Admin/` & `Views/CMP/` langsung dapat akses `QuestionTypeLabels` tanpa per-file `@using`. Tidak perlu modifikasi `_ViewImports.cshtml`.
- **C# switch expression** sudah idiomatic di codebase (verified di Phase 12.0 controller refactor). Helper `QuestionTypeLabels` aman pakai pattern ini.
- **TempData["Error"]** flash pattern sudah dipakai di 16 lokasi `AssessmentAdminController.cs`. Existing pattern: `TempData["Error"] = "..."`. Helper integration trivial.

### Established Patterns

- **3-place duplikasi badge logic** (Manage + Preview + StartExam) — existing tech debt. Phase 305 tepat untuk consolidate.
- **Enum string value** (`"MultipleChoice"` dll) bukan C# enum type — `QuestionType` property adalah `string?` (nullable) di `AssessmentPackage.cs` line 48 + `PackageExamViewModel.cs` line 37/62 + `AnalyticsDashboardViewModel.cs` line 86. Helper menggunakan `string` parameter, switch expression match string literals.
- **View `_ViewImports.cshtml`** — confirmed: `@using HcPortal.Models` global (line 2). Helper langsung accessible di view.
- **Hybrid bahasa di error msg** — pattern existing tidak pure Indo (e.g., "TempData", "ViewBag", parameter names campur). Hybrid label OK.

### Integration Points

- **Helper class `QuestionTypeLabels.cs`** = pure static, no DI, no state. Aman dipakai di view (Razor `@`) + controller (langsung) + future test (deterministic).
- **Excel import logic** di `AssessmentAdminController.ImportQuestions` (line ~5000+) baca enum value dari sheet. **Tidak terkena impact** — hanya helper text UI yang berubah.
- **JS dropdown change handler** di `ManagePackageQuestions.cshtml` line 356 (`var qtype = data.questionType || 'MultipleChoice';`) baca dari `<option value="...">`. Karena `value` attribute tetap (D-07), JS tidak berubah.
- **Future Phase 306 (QSCR-01)** akan ubah `scoreInput.disabled` logic di line 299-300. Phase 305 hanya text label — tidak konflik.

</code_context>

<specifics>
## Specific Ideas

### Wording Choice Rationale

User memilih **"Single Choice" / "Multiple Answers"** setelah riset:
- **Kemendikbud AKM standard:** "Pilihan Ganda" (1 jawaban) / "Pilihan Ganda Kompleks" (≥2 jawaban) — opsi nasional Indonesia ditolak
- **Moodle:** "Single answer" / "Multiple answers"
- **Canvas:** "Multiple Choice" / "Multiple Answer" (separate types)
- **Pertimbangan:** "Single Choice" + "Multiple Answers" paling simetris linguistic (singular/plural pair), match Moodle convention paling clean. User explicit: "dari bahasa inggris juga gapapa".

### Hybrid Bahasa Rationale

Label tipe pure English, parentheses & instruksi tetap Indonesia:
- Long form: `"Single Choice (1 jawaban benar)"` — label Inggris + hint Indonesia
- Error: `"Single Choice hanya boleh memiliki 1 jawaban benar."` — nama tipe Inggris, kalimat Indonesia
- **Why:** Rest of app (form labels, button text, navigation, flash messages lain) Bahasa Indonesia. Pure English untuk error/long-form akan inkonsisten. Hybrid = minimal disruption, maximum clarity.

### Helper Class Rationale

Extract C# helper menjadi necessary, bukan premature:
- **6 touch points** di Phase 305 saja (3 badge view + dropdown + controller flash + import helper)
- **3-place duplikasi badge logic** sudah pre-existing tech debt
- **Future relabel** (audit follow-up, A/B test, translation) realistic — 1 file vs 6 file
- **Test surface narrows** — 1 helper unit-testable vs 6 caller integration test
- **Risk additif minimal** — pure static class, no DI, no state, deterministic.
- Phase 304 D-15 spirit ("no extract premature") tetap dihormati — extract justified saat n>4 callers, bukan n=2.

### Symmetric Badge in Worker Exam

Tambah badge MC di `StartExam.cshtml` (D-09, D-16):
- Sebelum: hanya MA + Essay punya badge — asimetris, MC default tanpa indikator
- Sesudah: semua 3 tipe punya badge — konsisten dengan admin preview
- **Why:** Kalau MA & Essay butuh signaling "pilih semua / teks bebas", MC juga butuh signaling "pilih satu" — symmetric UX, no surprise.

</specifics>

<deferred>
## Deferred Ideas

### Out of Phase 305 Scope

1. **PDF panduan regenerasi** — `wwwroot/documents/GAST_RFCCNHT_Operator_Kompetensi_02022026.pdf` dan PDF lain. Tidak bisa edit binary via code. **Action:** user manual regen + replace setelah label baru live di production. Tracked di RETROSPECTIVE.

2. **Screenshot training material regenerasi** — assumed exists di folder training/internal repository tidak ter-track Git. **Action:** user manual screenshot ulang setelah deployment. Tidak block phase 305.

3. **Multi-language i18n / Localization** — saat ini app pure Indonesian. Tidak ada `IStringLocalizer` setup. Future jika perlu support EN/ID toggle, butuh i18n full architecture (resource files, locale switcher, etc.). **Defer ke milestone v16+.**

4. **Tooltip transisi "MC = Single Choice (dulu Pilihan Ganda)"** — Out of Scope per success criteria. Atomic swap, tidak ada legend.

5. **Excel import template binary regenerate** — file `.xlsx` (template MC, MA, Essay, Universal) di `wwwroot/documents/` & `Database/`. Internal kolom `QuestionType` tetap pakai enum value `MultipleChoice`/`MultipleAnswer`/`Essay`. Tidak butuh edit. Jika future user complain template button "Single Choice" mismatch dengan content download, bisa update template title cell saja.

6. **Rename enum value `MultipleChoice` → `SingleChoice`** — Out per REQUIREMENTS Out of Scope (resiko 58+ file & migrasi DB). Hanya label UI yang berubah. Internal forever stays `MultipleChoice`.

7. **Tooltip / info-icon di label dropdown form** — Out of Scope. Long form parentheses sudah cukup.

8. **DB query log audit untuk verifikasi enum unchanged** — task post-implementation. Planner tambah verification task.

### Reviewed Todos (not folded)

Tidak ada todo eksisting yang teridentifikasi cocok dengan Phase 305 LBL-01 — phase ini lahir dari audit findings 27 April 2026, bukan backlog.

</deferred>

---

*Phase: 305-question-type-naming-clarity*
*Context gathered: 2026-04-28*
