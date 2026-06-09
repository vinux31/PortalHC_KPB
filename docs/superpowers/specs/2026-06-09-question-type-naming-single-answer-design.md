# Standarisasi Istilah Tipe Soal → "Single Answer / Multiple Answer / Essay"

**Tanggal:** 2026-06-09
**Phase target:** 357 (v24.0, addon off-theme — label work, bukan image)
**REQ:** LBL-02 (lanjutan LBL-01 Phase 305)
**Risiko:** Rendah — pure label + dead-code removal. No DB, no migration, no logic change.

## Latar Belakang

Phase 305 (v15.0, 2026-04-28) sudah standarisasi label tipe soal jadi **"Single Choice / Multiple Answers / Essay"** (ikut konvensi Moodle), dengan helper `Models/QuestionTypeLabels.cs` sebagai *intended* single-source-of-truth. Nilai DB enum sengaja TIDAK diubah (tetap `MultipleChoice`/`MultipleAnswer`/`Essay`).

User minta re-label jadi **"Single Answer / Multiple Answer / Essay"** — alasan: kata **"Answer" konsisten** di dua tipe pilihan (standarisasi istilah). Ini override keputusan editorial Phase 305 (Moodle convention), keputusan sadar user.

**Temuan saat re-check:** helper BUKAN single-source penuh — beberapa surface masih hardcode label (dropdown options, badge EditPeserta, tombol Import, docs HTML). Phase ini sekalian konsolidasi surface hardcode ke helper.

## Keputusan (locked)

- **1A** — Wording: "Single Answer / Multiple Answer / Essay".
- **2A** — Cakupan: label-only semua surface + konsolidasi hardcode ke helper + hapus dead code. DB enum TETAP (no migration).
- **opsi i** — Badge EditPesertaAnswers pakai `Short()` (label penuh "Single Answer"), bukan singkatan "MC/MA". Verified: spot badge = header card full-width, ruang cukup; singkatan lama cuma shortcut dev Phase 321 yang ganjil sendiri.
- **S1** — Singkatan: di tempat yang memang pakai abbrev (export Excel sel + guide), **"MC"→"SA"**, **"MA" tetap "MA"** (ikut wording baru: Single Answer→SA). Bukan dibuang jadi label penuh.

## Mapping Wording

| DB value (TETAP) | Label lama (Phase 305) | Label baru |
|---|---|---|
| `MultipleChoice` | Single Choice | **Single Answer** |
| `MultipleAnswer` | Multiple Answers | **Multiple Answer** |
| `Essay` | Essay | Essay (tetap) |

- Long form (dropdown form admin): "Single Answer (1 jawaban benar)" / "Multiple Answer (≥2 jawaban benar)" / "Essay".
- Short form (badge): "Single Answer" / "Multiple Answer" / "Essay".
- `BadgeClass()` tidak berubah.

## Scope Kerja (4 grup)

### Grup A — Helper (core, 1 file)
- `Models/QuestionTypeLabels.cs` — ubah string return di `Long()` + `Short()` (termasuk fallback `_` default). `BadgeClass()` tidak disentuh.

### Grup B — Surface UI hardcode → konsolidasi ke helper
- `Views/Admin/ManagePackageQuestions.cshtml:131-133` — text `<option>` dropdown ganti wording baru. **`value` attribute (`MultipleChoice`/`MultipleAnswer`/`Essay`) TETAP** — JS handler & binding baca value, jangan diubah. (Boleh tetap static text atau pakai `@QuestionTypeLabels.Long(...)` — planner pilih; jika static, pastikan match helper.)
- `Views/Admin/EditPesertaAnswers.cshtml:49` — badge ternary `"MC"/"MA"/"Essay"` → `@QuestionTypeLabels.Short(q.QuestionType)`.
- `Views/Admin/ImportPackageQuestions.cshtml:39,42` — tombol "Template Single Choice"/"Template Multiple Answers" → "Template Single Answer"/"Template Multiple Answer". Bullet baris 32 (`<code>MultipleChoice</code>` dst) = developer-facing enum value, **TETAP** (itu nilai kolom Excel, bukan label).
- `Controllers/AssessmentAdminController.cs:4550` — export Excel per-peserta, sel tipe `tipe == "MultipleChoice" ? "MC" : "MA"` → ganti **"MC"→"SA"** (S1), "MA" tetap. User-facing di file download.

### Grup C — Dead code
- `Controllers/CMPController.cs:3389,3624` — hapus cabang `"TrueFalse"` (tipe hantu: tak ada di model/enum/UI/DB; `NormalizeQuestionType` coerce unknown→MultipleChoice, jadi cabang unreachable). Pastikan penghapusan tak ubah hasil analitik untuk 3 tipe valid.

### Grup D — Docs user-facing (served di portal)
Replace **context-aware** (BUKAN blind sed). Mapping:
- "Single Choice" → "Single Answer"
- "Multiple Answers" → "Multiple Answer"
- **"Multiple Choice" → "Single Answer"** (istilah LAMA pra-Phase-305 yang masih nyangkut di beberapa konten, mis. GuideContentProvider — merujuk tipe MC = single-correct)
- abbrev "MC" → "SA" (S1), "MA" tetap

Target:
- `wwwroot/documents/guides/Struktur-Website-PortalHC-KPB.html`
- `wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html`
- `wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html`
- `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html`
- `wwwroot/documents/guides/Panduan-Lengkap-Assessment.html`
- `wwwroot/documents/guides/Panduan-Admin-Buat-Assessment-dan-Input-Soal.html`
- `wwwroot/documents/TKI/generate_bab_x.py` + `Draft-BAB-X-INSTRUKSI-KERJA.html` + `Draft-BAB-X-INSTRUKSI-KERJA-outline.md` (py = generator HTML; regen kalau perlu konsisten)
- `Services/GuideContentProvider.cs:175-188` — konten guide in-code. **Saat ini pakai istilah LAMA** "Multiple Choice (MC)" (Title:175, Step:179, body:186, Keywords:188). Reword penuh ke "Single Answer (SA)" + "Multiple Answer (MA)" + Keywords. Hati-hati: "Multiple Answer" di file ini SUDAH wording baru — jangan ganda-replace.

## TIDAK Disentuh (eksplisit)
- Semua `.planning/**` + `docs/superpowers/**` — arsip historis; mengedit = merusak catatan masa lalu.
- `docs/mockup-presentasi/353-layout-form-gambar-mockup.html` — artifact dev, tak di-serve.
- Excel template binary (`.xlsx`) — kolom internal pakai enum value, tak berubah.
- PDF panduan — **defer manual regen** oleh user (sama kebijakan Phase 305 D-14).
- DB enum / migration / property `QuestionType` / JS handler / logic grading.
- **~30 spot logic-check** `== "Essay"` / `== "MultipleAnswer"` / `qtype === 'MultipleAnswer'` (CMPController, GradingService, AssessmentAdminController, PackageExamViewModel, view radio/checkbox switch, JS ManagePackageQuestions:407-414) — kontrol alur baca enum value, BUKAN label. TETAP.
- **Route/query key `type="MC"/"MA"`** di `DownloadQuestionTemplate` (AssessmentAdminController:5589-5633) + href `ImportPackageQuestions.cshtml:38,41` — URL contract, bukan label. TETAP (ubah = link template pecah).
- Flash-error TempData (AssessmentAdminController:6155/6160/6362/6367) — sudah lewat `QuestionTypeLabels.Short()`, auto ter-update via Grup A. Tak perlu edit manual.

## Verifikasi (Success Criteria)
1. `dotnet build` 0 error + `dotnet test` hijau (tak ada test yang assert label lama; cek dulu).
2. `SELECT DISTINCT QuestionType FROM PackageQuestions` → masih `MultipleChoice`/`MultipleAnswer`/`Essay` (enum utuh, bukti no-migration).
3. Playwright cek 5 surface tampil wording baru: dropdown form Manage · badge tabel Manage · StartExam · ExamSummary · EditPesertaAnswers.
4. grep residual "Single Choice"/"Multiple Answers"/"Multiple Choice"(konteks tipe soal) = **0** di file non-arsip (kode + docs served + GuideContentProvider).
5. Export Excel per-peserta (AssessmentAdminController:4550) sel tipe = "SA"/"MA" (S1), bukan "MC"/"MA".
6. Flow ujian existing (MC/MA/Essay tanpa gambar) tetap normal — tak ada regresi (value DB tak berubah).

## Catatan Koordinasi
Phase 355 (Test & UAT) sedang di-plan di sesi lain saat phase ini dibuat. Insert ke ROADMAP pakai surgical Edit (bukan full-file rewrite) untuk hindari clobber. Phase 357 independen dari 352-356 (jalur file beda: label vs image), bisa dikerjakan paralel.
