# Phase 354: Render Gambar di 6 Layar - Pattern Map

**Mapped:** 2026-06-08
**Files analyzed:** 13 (1 partial baru + 1 modal-partial saran + 6 view + 2 controller + 4 ViewModel; sebagian overlap)
**Analogs found:** 13 / 13 (semua punya analog in-repo — fase plumbing murni)

> **Stack:** ASP.NET Core MVC + Razor + Bootstrap 5, server-rendered. SEMUA 6 surface render soal/opsi via Razor `@foreach` server-side (TIDAK ada client-side template) — termasuk EssayGrading di AssessmentMonitoringDetail (verified L405 `@foreach (var essayItem in essayItems)`).
>
> **Baseline kanonik:** `Views/Admin/_PreviewQuestion.cshtml` (Phase 353, live) = otoritas MARKUP `<img>` (kelas/cap/lazy/alt/render-if-not-null). Bukan otoritas layout opsi (UI-SPEC §3: D-03 block-bawah authoritative, partial menggantikan pola inline-flex).

---

## Verifikasi Line Number (drift check vs CONTEXT)

Semua line number di CONTEXT **AKURAT** per pembacaan 2026-06-08, plus 2 koreksi lokasi VM:

| Anchor CONTEXT | Klaim | Aktual | Status |
|----------------|-------|--------|--------|
| CMPController StartExam populate | ~L1055 | L1048-1070 (`new ExamOptionItem` L1055, `new ExamQuestionItem` L1061) | AKURAT |
| CMPController Results populate | ~L2300 | `new QuestionReviewItem` L2293, `new OptionReviewItem` L2300 | AKURAT |
| AssessmentAdminController essay grading | ~L3401 | `new EssayGradingItemViewModel` L3397-3406 | AKURAT |
| `ExamSummaryItem` (ExamSummary VM) | CONTEXT L-01 bilang di `EditPesertaAnswersViewModel.cs` | **SALAH** — sebenarnya di `Models/PackageExamViewModel.cs` L50-76; populate di **CMPController L1521/1544/1561** (3 cabang Essay/MA/MC) | KOREKSI |
| `EditQuestionRow`/`EditOptionRow` populate | CONTEXT L-01 implisit | **AssessmentAdminController L2986-2998** (bukan CMPController) | KOREKSI |

> **Catatan penting `ExamSummaryItem`:** TIDAK menyimpan list objek opsi — hanya `SelectedOptionTexts` (huruf "A","C") + `SelectedOptionText`. Untuk render gambar OPSI di ExamSummary, populate (CMPController L1512-1571) PUNYA akses `q.Options` (entity) di scope loop → planner tambah field gambar opsi ke `ExamSummaryItem` ATAU bawa list opsi-dengan-gambar. Gambar SOAL trivial (`q.ImagePath` tersedia di loop).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Views/Shared/_QuestionImage.cshtml` (NEW, D-04) | view-partial | transform (VM→markup) | `Views/Admin/_PreviewQuestion.cshtml` L20-24 / L69-73 | exact (markup diangkat) |
| `Views/Shared/_ImageLightboxModal.cshtml` (NEW, saran D-02) | view-partial | request-response (UI) | `Views/Admin/ManagePackageQuestions.cshtml` L263-283 (`#previewModal`) | role-match |
| `Models/PackageExamViewModel.cs` | model | transform | self (field existing per item) | exact (self) |
| `Models/AssessmentResultsViewModel.cs` | model | transform | self | exact (self) |
| `Models/AssessmentMonitoringViewModel.cs` | model | transform | self | exact (self) |
| `Models/EditPesertaAnswersViewModel.cs` | model | transform | self | exact (self) |
| `Controllers/CMPController.cs` (StartExam + Results + ExamSummary populate) | controller | CRUD (read→VM map) | `_PreviewQuestion` entity→render (sumber field sama) | exact |
| `Controllers/AssessmentAdminController.cs` (essay grading + EditPesertaAnswers populate) | controller | CRUD (read→VM map) | self L2991-2994 (EditOptionRow map) | exact |
| `Views/CMP/StartExam.cshtml` (RND-01) | view | request-response | `_PreviewQuestion.cshtml` | exact (markup mirror) |
| `Views/CMP/ExamSummary.cshtml` (RND-02) | view | request-response | `_PreviewQuestion.cshtml` | exact |
| `Views/CMP/Results.cshtml` (RND-03) | view | request-response | `_PreviewQuestion.cshtml` | exact |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` (RND-05, soal-saja) | view | request-response | `_PreviewQuestion.cshtml` (soal blok saja) | exact |
| `Views/Admin/EditPesertaAnswers.cshtml` (RND-06) | view | request-response | `_PreviewQuestion.cshtml` | exact |
| `Views/Admin/_PreviewQuestion.cshtml` (RND-04, RETROFIT ke partial) | view-partial | transform | self (refactor) | exact (self) |

---

## Pattern Assignments

### `Views/Shared/_QuestionImage.cshtml` (NEW — partial reusable, D-04)

**Analog:** `Views/Admin/_PreviewQuestion.cshtml` L20-24 (soal) + L69-73 (opsi). Markup `<img>` di sana = otoritas; partial mengangkatnya + menambah atribut lightbox (UI-SPEC §1).

**Markup `<img>` baseline yang diangkat** (`_PreviewQuestion.cshtml` L20-24):
```cshtml
@if (!string.IsNullOrWhiteSpace(Model.ImagePath))
{
    <img src="@Model.ImagePath" alt="@Model.ImageAlt" class="img-fluid rounded border mb-3"
         style="max-height:240px" loading="lazy" />
}
```

**Target markup partial (LOCKED, UI-SPEC §1)** — tambah trigger lightbox (`cursor:pointer`, `role="button"`, `tabindex="0"`, `data-bs-toggle/target`, `data-img-src/alt`, `aria-label`). `src` ber-encode Razor (L-02, no XSS surface baru). `cap` (240 soal / 120 opsi) di-pass param atau hardcode per pemanggilan (diskresi). Render NOTHING bila `ImagePath` null/whitespace.

**Catatan input partial:** karena tiap VM (`ExamOptionItem`, `OptionReviewItem`, `EditOptionRow`, entity `PackageOption/PackageQuestion`, `ExamSummaryItem`) punya bentuk berbeda, partial sebaiknya bertipe **model ringan** (mis. `(string? ImagePath, string? ImageAlt, int Cap, string? AriaContext)`) atau `@model dynamic` / ViewDataDictionary param — diskresi planner. JANGAN ikat partial ke 1 tipe VM konkret (anti-reuse).

---

### `Views/Shared/_ImageLightboxModal.cshtml` (NEW — modal global, D-02)

**Analog:** `Views/Admin/ManagePackageQuestions.cshtml` L263-283 (`#previewModal`).

**Pola modal Bootstrap existing** (L263-283):
```cshtml
<div class="modal fade" id="previewModal" tabindex="-1" aria-labelledby="previewModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <div class="modal-content">
            <div class="modal-header bg-light">
                <h5 class="modal-title" id="previewModalLabel"><i class="bi bi-eye me-2"></i>Preview Soal</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body" id="previewModalBody"> ... </div>
        </div>
    </div>
</div>
```

**Target (LOCKED, UI-SPEC §2):** `id="imageLightboxModal"`, `modal-xl modal-dialog-centered`, title "Pratinjau Gambar", `btn-close aria-label="Tutup"`, body `text-center` + `<img id="imageLightboxImg" class="img-fluid">`. JS `show.bs.modal` handler swap `src`/`alt` dari `e.relatedTarget` `data-img-src`/`data-img-alt`. **1 instance per halaman** — render sekali per view (atau via partial `@await Html.PartialAsync("_ImageLightboxModal")` di tiap surface). JS ditaruh di `@section Scripts` tiap view (atau di partial JS bersama).

**Wiring per surface:** modal + JS HARUS hadir di SETIAP halaman yang punya gambar (6 surface; kecuali catatan: `_PreviewQuestion` di-load via AJAX ke `#previewModalBody` di ManagePackageQuestions → lightbox modal+JS perlu hadir di HOST page ManagePackageQuestions, bukan di partial yang di-inject). Planner catat khusus host RND-04.

---

### `Models/PackageExamViewModel.cs` (StartExam RND-01 + ExamSummary RND-02)

**Analog:** entity `Models/AssessmentPackage.cs` L60/L64 (`PackageQuestion.ImagePath/ImageAlt`) + L89/L93 (`PackageOption.ImagePath/ImageAlt`) — nama field konvensi sumber.

**Aksi konkret:**
- `ExamQuestionItem` (L25-41): + `public string? ImagePath { get; set; }` + `public string? ImageAlt { get; set; }`
- `ExamOptionItem` (L43-48): + `ImagePath` + `ImageAlt`
- `ExamSummaryItem` (L50-76): + `ImagePath`/`ImageAlt` SOAL; untuk OPSI lihat catatan (item ini text-only saat ini → planner putuskan bentuk bawa gambar opsi, mis. tambah `List<ExamSummaryOptionItem>` ber-gambar, ATAU karena ExamSummary review-only mungkin cukup soal — cek REQUIREMENTS RND-02; UI-SPEC tabel §"Konsistensi" bilang ExamSummary opsi 120px → WAJIB bawa gambar opsi).

---

### `Models/AssessmentResultsViewModel.cs` (Results RND-03)

**Analog:** self + entity field.

**Aksi konkret:**
- `QuestionReviewItem` (L24-33): + `ImagePath` + `ImageAlt`
- `OptionReviewItem` (L35-40): + `ImagePath` + `ImageAlt`

---

### `Models/AssessmentMonitoringViewModel.cs` (AssessmentMonitoringDetail RND-05 — SOAL SAJA)

**Analog:** self.

**Aksi konkret:** `EssayGradingItemViewModel` (L73-82): + `ImagePath` + `ImageAlt` **gambar soal saja** (essay tak punya opsi, RND-05). JANGAN tambah field opsi.

---

### `Models/EditPesertaAnswersViewModel.cs` (EditPesertaAnswers RND-06)

**Analog:** self.

**Aksi konkret:**
- `EditQuestionRow` (L12-21): + `ImagePath` + `ImageAlt` (soal)
- `EditOptionRow` (L23-28): + `ImagePath` + `ImageAlt` (opsi)

---

### `Controllers/CMPController.cs` — 3 populate loop

**StartExam (L1048-1070):** loop punya entity `q` (PackageQuestion) + `o` (PackageOption) di scope. Map saat ini:
```csharp
var opts = q.Options.OrderBy(o => o.Id).Select(o => new ExamOptionItem
{
    OptionId = o.Id,
    OptionText = o.OptionText
}).ToList();

examQuestions.Add(new ExamQuestionItem
{
    QuestionId = q.Id,
    QuestionText = q.QuestionText,
    DisplayNumber = displayNum++,
    Options = opts,
    QuestionType = q.QuestionType ?? "MultipleChoice",
    MaxCharacters = q.MaxCharacters > 0 ? q.MaxCharacters : 2000
});
```
**Aksi:** tambah `ImagePath = o.ImagePath, ImageAlt = o.ImageAlt` di `ExamOptionItem`; `ImagePath = q.ImagePath, ImageAlt = q.ImageAlt` di `ExamQuestionItem`.

**Results (L2293-2310):** map dari `question` (entity) + `o` (PackageOption) di scope:
```csharp
questionReviews.Add(new QuestionReviewItem
{
    QuestionNumber = questionNum,
    QuestionText = question.QuestionText,
    ...
    Options = question.Options.Select(o => new OptionReviewItem
    {
        OptionText = o.OptionText,
        IsCorrect = o.IsCorrect,
        IsSelected = selectedOptionIds.Contains(o.Id)
    }).ToList(),
    ...
});
```
**Aksi:** tambah `ImagePath = question.ImagePath, ImageAlt = question.ImageAlt` (soal) + `ImagePath = o.ImagePath, ImageAlt = o.ImageAlt` (opsi).

**ExamSummary (L1512-1571):** 3 cabang (Essay/MA/MC) — `q` (entity) di scope tiap cabang; `q.Options` tersedia. **Aksi:** set `ImagePath/ImageAlt` soal di ketiga cabang `new ExamSummaryItem`; gambar opsi via bentuk yang dipilih planner (lihat catatan VM). Cek `.Include(q => q.Options)` sudah ada (L1505 confirm Include).

---

### `Controllers/AssessmentAdminController.cs` — 2 populate loop

**Essay grading (L3397-3406):** map dari `q` (PackageQuestion). Tambah `ImagePath = q.ImagePath, ImageAlt = q.ImageAlt` (SOAL SAJA, RND-05). NB: `essayQs` di-query L3388-3390 `_context.PackageQuestions.Where(...Essay)` — kolom ImagePath/ImageAlt auto-ter-load (bukan projection), tidak perlu ubah query.

**EditPesertaAnswers (L2986-2998):** map dari `q` + `o`:
```csharp
rows.Add(new HcPortal.Models.EditQuestionRow
{
    PackageQuestionId = q.Id,
    QuestionText = q.QuestionText,
    QuestionType = q.QuestionType ?? "MultipleChoice",
    Options = q.Options.Select(o => new HcPortal.Models.EditOptionRow
    {
        Id = o.Id, OptionText = o.OptionText, IsCorrect = o.IsCorrect
    }).ToList(),
    ...
});
```
**Aksi:** tambah `ImagePath = q.ImagePath, ImageAlt = q.ImageAlt` (soal) + `ImagePath = o.ImagePath, ImageAlt = o.ImageAlt` (opsi). NB `questions` di-query dgn `.Include(q => q.Options)` (L1505-pattern; EditPesertaAnswers L2965-2978 confirm `questions`/`responses` loaded) — verifikasi Include Options ada.

---

### View render — titik sisip partial per surface

| View | Soal: sisip setelah | Opsi: sisip dalam |
|------|---------------------|-------------------|
| `StartExam.cshtml` (RND-01) | `<p class="fw-bold mb-3">...@q.QuestionText</p>` (L100-104) | `<label class="list-group-item ... d-flex align-items-center gap-3">` MA L134-145 + MC L164-174 (`<span>@opt.OptionText</span>`) → restruktur block-bawah §3 |
| `ExamSummary.cshtml` (RND-02) | titik render `@item.QuestionText` (cek view) | titik render opsi terpilih |
| `Results.cshtml` (RND-03) | `<h6>...@question.QuestionText</h6>` (L329) | `<div class="list-group-item @itemClass d-flex align-items-center"> ... <span>@option.OptionText</span>` (L371-376) → block-bawah |
| `AssessmentMonitoringDetail.cshtml` (RND-05) | `<h6 class="fw-semibold">Soal @essayItem.DisplayNumber: @essayItem.QuestionText</h6>` (L410) | — (soal saja) |
| `EditPesertaAnswers.cshtml` (RND-06) | `<div class="mb-2">@q.QuestionText</div>` (L53) | `<label class="form-check-label" for=...>@opt.OptionText</label>` (L72-73, L91-92) → block-bawah |
| `_PreviewQuestion.cshtml` (RND-04) | L17 `<p>@Model.QuestionText</p>` (ganti L20-24 → partial) | L66-67 + L69-73 (ganti → partial, restruktur block-bawah §3) |

**Pemanggilan partial (pola existing):** `@await Html.PartialAsync("_QuestionImage", new {...})` — repo pakai pola partial via PartialAsync (CONTEXT code_context "partial via `@await Html.PartialAsync`").

---

## Shared Patterns

### Markup `<img>` render (anti-drift, D-04)
**Source:** `Views/Admin/_PreviewQuestion.cshtml` L20-24 (soal) + L69-73 (opsi)
**Apply to:** SEMUA 6 surface via 1 partial `_QuestionImage.cshtml`
```cshtml
@if (!string.IsNullOrWhiteSpace(ImagePath))
{
    <img src="@ImagePath" alt="@ImageAlt" class="img-fluid rounded border mb-3 question-image-zoom"
         style="max-height:240px; cursor:pointer" loading="lazy"
         role="button" tabindex="0"
         data-bs-toggle="modal" data-bs-target="#imageLightboxModal"
         data-img-src="@ImagePath" data-img-alt="@ImageAlt"
         aria-label="..." />
}
```
Opsi: `max-height:120px` + `d-block w-100` (block-bawah §3).

### Lightbox modal Bootstrap (D-02)
**Source:** `Views/Admin/ManagePackageQuestions.cshtml` L263-283 (`#previewModal` markup) + L419 (`new bootstrap.Modal(...)` pola JS)
**Apply to:** SEMUA 6 surface (1 instance/halaman). `show.bs.modal` swap src.

### Entity field sumber (ImagePath/ImageAlt)
**Source:** `Models/AssessmentPackage.cs` L60/64 (`PackageQuestion`) + L89/93 (`PackageOption`) — sudah terisi via Phase 353. **Apply to:** semua populate loop (langsung `q.ImagePath`/`o.ImagePath`, tidak perlu query/Include tambahan — kolom non-projected auto-load).

### Penempatan opsi block-bawah (D-03 AUTHORITATIVE, UI-SPEC §3)
**Apply to:** semua surface ber-opsi (StartExam, ExamSummary, Results, EditPesertaAnswers, _PreviewQuestion). Mekanisme A (restruktur `<label>` → stack vertikal, gambar sibling block bawah baris teks, `mt-2`) diutamakan; B (`d-block w-100` + `flex-wrap`) alternatif. Kriteria: gambar opsi selalu di baris terpisah DI BAWAH teks, lebar penuh, tidak berdampingan horizontal.

---

## No Analog Found

Tidak ada. Semua file punya analog in-repo (fase plumbing murni — markup baseline `_PreviewQuestion` + modal `ManagePackageQuestions` + entity field + populate loop existing).

---

## Metadata

**Analog search scope:** `Views/Admin/`, `Views/CMP/`, `Models/`, `Controllers/`
**Files scanned:** `_PreviewQuestion.cshtml`, `ManagePackageQuestions.cshtml`, `StartExam.cshtml`, `Results.cshtml`, `ExamSummary.cshtml`, `EditPesertaAnswers.cshtml`, `AssessmentMonitoringDetail.cshtml`, `PackageExamViewModel.cs`, `AssessmentResultsViewModel.cs`, `AssessmentMonitoringViewModel.cs`, `EditPesertaAnswersViewModel.cs`, `AssessmentPackage.cs`, `CMPController.cs`, `AssessmentAdminController.cs`
**Pattern extraction date:** 2026-06-08

---

## PATTERN MAPPING COMPLETE
