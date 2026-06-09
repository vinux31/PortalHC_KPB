# Phase 357: Standarisasi Istilah Tipe Soal - Context

**Gathered:** 2026-06-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Re-label tipe soal jadi trio konsisten **"Single Answer / Multiple Answer / Essay"** (kata "Answer" konsisten) di SEMUA surface user-facing + jadikan `Models/QuestionTypeLabels.cs` single-source penuh (konsolidasi surface hardcode) + hapus dead code `"TrueFalse"`. Override editorial Phase 305 ("Single Choice / Multiple Answers"). Off-theme dari v24.0 image-work (jalur file beda — label/helper/docs, independen 352-356).

**Pure label + dead-code removal.** NO DB, NO migration, NO model/property change, NO grading logic, NO JS handler, NO route/query-key change. DB enum TETAP `MultipleChoice`/`MultipleAnswer`/`Essay`.

REQ: **LBL-02** (lanjutan LBL-01 Phase 305).
</domain>

<decisions>
## Implementation Decisions

### Wording & Mapping [LOCKED spec 1A + tabel]
- **D-01:** Wording baru: `MultipleChoice`→**Single Answer**, `MultipleAnswer`→**Multiple Answer**, `Essay`→Essay (tetap). DB value TIDAK berubah (no migration).
- **D-02:** Long form (dropdown admin): "Single Answer (1 jawaban benar)" / "Multiple Answer (≥2 jawaban benar)" / "Essay". Short form (badge): "Single Answer" / "Multiple Answer" / "Essay". `BadgeClass()` TIDAK disentuh.
- **D-03 (opsi-i):** Badge EditPesertaAnswers pakai `QuestionTypeLabels.Short()` (label penuh), bukan singkatan "MC/MA" lama.
- **D-04 (S1):** Di tempat yang memang pakai abbrev (export Excel sel + guide): **"MC"→"SA"**, **"MA" tetap "MA"**. Bukan dibuang jadi label penuh.

### Scope kerja 4 grup [LOCKED spec 2A]
- **D-05 Grup A (helper core):** `Models/QuestionTypeLabels.cs` — ubah string return `Long()` + `Short()` (termasuk fallback `_` default). `BadgeClass()` tetap.
- **D-06 Grup B (surface hardcode → konsolidasi):**
  - `Views/Admin/ManagePackageQuestions.cshtml:131-133` — dropdown `<option>` text pakai **`@QuestionTypeLabels.Long(...)` binding** (DRY single-source, sesi ini DIPILIH binding bukan static text — selaras goal single-source penuh). **`value` attribute (`MultipleChoice` dst) TETAP** (JS handler & binding baca value).
  - `Views/Admin/EditPesertaAnswers.cshtml:49` — badge ternary "MC"/"MA"/"Essay" → `@QuestionTypeLabels.Short(q.QuestionType)`.
  - `Views/Admin/ImportPackageQuestions.cshtml:39,42` — tombol "Template Single Choice"/"Multiple Answers" → "Single Answer"/"Multiple Answer". Bullet baris 32 (`<code>MultipleChoice</code>`) = enum value dev-facing, TETAP.
  - `Controllers/AssessmentAdminController.cs:4550` — export Excel sel tipe `"MC"→"SA"` (S1), "MA" tetap.
- **D-07 Grup C (dead code):** `Controllers/CMPController.cs:3389,3624` — hapus cabang `"TrueFalse"` (tipe hantu, unreachable — `NormalizeQuestionType` coerce unknown→MultipleChoice). Pastikan hasil analitik 3 tipe valid tak berubah.
- **D-08 Grup D (docs served):** Edit context-aware 6 guide HTML + `Services/GuideContentProvider.cs:175-188`. Mapping: "Single Choice"→"Single Answer", "Multiple Answers"→"Multiple Answer", **"Multiple Choice"→"Single Answer"** (istilah lama pra-305), abbrev "MC"→"SA". TKI `generate_bab_x.py` + BAB-X HTML: regen bila perlu konsisten. **PDF panduan: DEFER manual regen user** (kebijakan Phase 305 D-14). HATI-HATI GuideContentProvider: "Multiple Answer" di file itu SUDAH wording baru — jangan ganda-replace.

### Verifikasi [sesi ini]
- **D-09:** `dotnet build` 0 error + `dotnet test` hijau (cek dulu tak ada test assert label lama) + grep residual "Single Choice"/"Multiple Answers"/"Multiple Choice"(konteks tipe soal) = **0** di file non-arsip + `SELECT DISTINCT QuestionType` masih 3 enum utuh (bukti no-migration) + export Excel sel "SA"/"MA".
- **D-10:** **Playwright UAT 5 surface** (dropdown form Manage · badge tabel Manage · StartExam · ExamSummary · EditPesertaAnswers) di localhost:5277 (AD=false). Claude jalankan via Playwright MCP. Flow ujian existing tanpa regresi.

### Claude's Discretion
- Bentuk persis regen TKI BAB-X (jalankan .py vs edit manual HTML) — asal output konsisten wording baru.
- Apakah dropdown Long() binding perlu null-guard (QuestionType selalu salah satu 3 enum).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec (primary)
- `docs/superpowers/specs/2026-06-09-question-type-naming-single-answer-design.md` — keputusan 1A/2A/opsi-i/S1, mapping table, scope 4 grup dgn file:line, daftar "TIDAK disentuh" eksplisit, 6 success criteria. **Sumber kebenaran utama.**

### Target Code
- `Models/QuestionTypeLabels.cs` — helper core (Long/Short, BadgeClass tetap).
- `Views/Admin/ManagePackageQuestions.cshtml` L131-133 (dropdown), `Views/Admin/EditPesertaAnswers.cshtml` L49 (badge), `Views/Admin/ImportPackageQuestions.cshtml` L32/39/42 (tombol+bullet).
- `Controllers/AssessmentAdminController.cs` L4550 (Excel sel), L5589-5633 (route key TETAP), L6155/6160/6362/6367 (flash auto-update via helper).
- `Controllers/CMPController.cs` L3389/3624 (dead TrueFalse).
- `Services/GuideContentProvider.cs` L175-188 (guide in-code, istilah lama).
- 6 `wwwroot/documents/guides/*.html` + `wwwroot/documents/TKI/generate_bab_x.py`.

### Predecessor
- Phase 305 (v15.0) — LBL-01, helper QuestionTypeLabels.cs dibuat. Phase 357 override wording-nya.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Models/QuestionTypeLabels.cs` — `Long()`/`Short()`/`BadgeClass()` sudah ada (Phase 305). Ubah string di Long/Short → otomatis propagate ke surface yang sudah pakai helper (flash TempData L6155 dst).

### Established Patterns
- DB enum value `MultipleChoice`/`MultipleAnswer`/`Essay` dipakai di ~30 spot logic-check (`== "Essay"`, `qtype === 'MultipleAnswer'`) + route key `type="MC"/"MA"` — INI BUKAN label, baca enum/URL contract. JANGAN sentuh.
- `NormalizeQuestionType` coerce unknown→MultipleChoice → cabang "TrueFalse" unreachable (dead).

### Integration Points
- Dropdown ManagePackageQuestions value attribute → JS handler L407-414 baca value (TETAP).
- Helper dipakai badge/flash → ubah helper sekali, propagate.
</code_context>

<specifics>
## Specific Ideas
- User override Phase 305 Moodle convention SECARA SADAR — alasan: "Answer" konsisten 2 tipe pilihan. Wording final = keputusan user, jangan revert ke "Choice".
- Dropdown DIPILIH binding `@QuestionTypeLabels.Long()` (bukan static) — wujudkan goal "single-source penuh".
</specifics>

<deferred>
## Deferred Ideas
- **PDF panduan regen** — manual oleh user (kebijakan Phase 305 D-14, di luar scope phase ini).
</deferred>

---

*Phase: 357-standarisasi-istilah-tipe-soal*
*Context gathered: 2026-06-09*
