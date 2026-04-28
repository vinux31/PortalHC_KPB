---
phase: 305-question-type-naming-clarity
reviewed: 2026-04-28T00:00:00Z
depth: standard
files_reviewed: 15
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Models/QuestionTypeLabels.cs
  - Views/Admin/ImportPackageQuestions.cshtml
  - Views/Admin/ManagePackageQuestions.cshtml
  - Views/Admin/_PreviewQuestion.cshtml
  - Views/CMP/ExamSummary.cshtml
  - Views/CMP/StartExam.cshtml
  - docs/Persiapan-Test-Manual-Assessment.html
  - wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md
  - wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA.html
  - wwwroot/documents/TKI/generate_bab_x.py
  - wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html
  - wwwroot/documents/guides/Penjelasan-Halaman-PortalHC-KPB.html
  - wwwroot/documents/guides/Release-Notes-HC-Portal-KPB.html
  - wwwroot/documents/guides/Struktur-Website-PortalHC-KPB.html
findings:
  critical: 0
  warning: 0
  info: 3
  total: 3
status: issues_found
---

# Phase 305: Code Review Report

**Reviewed:** 2026-04-28
**Depth:** standard
**Files Reviewed:** 15
**Status:** issues_found (hanya Info — tidak ada Critical/Warning)

## Summary

Phase 305 (LBL-01) adalah refaktor label-only — mengubah label UI tipe soal dari "Pilihan Ganda / Multiple Answer" menjadi "Single Choice / Multiple Answers" tanpa menyentuh nilai enum DB (`MultipleChoice` / `MultipleAnswer` / `Essay`).

Hasil review:
- D-17 schema lock terverifikasi: nilai DB `QuestionType` tetap utuh (string `"MultipleChoice"`, `"MultipleAnswer"`, `"Essay"`) — `Models/AssessmentPackage.cs:48` masih `public string? QuestionType`, tidak ada enum baru, tidak ada migrasi.
- Helper `Models/QuestionTypeLabels.cs` aman: switch expression handle `string?` (null-safe), default fallback konsisten ("Single Choice" / "bg-secondary") di ketiga method (Long/Short/BadgeClass).
- Controller `AssessmentAdminController.cs` (~5000 baris): `git show 142bb609` mengkonfirmasi diff 4 insertions / 4 deletions persis di line 4688, 4693, 4829, 4834 — tidak ada perubahan tak diinginkan di tempat lain. Whitelist `validTypes = new[] { "MultipleChoice", "MultipleAnswer", "Essay" }` (line 4668, 4809) tetap utuh.
- 5 view (StartExam, ExamSummary, ManagePackageQuestions, _PreviewQuestion, ImportPackageQuestions) menggunakan `QuestionTypeLabels.Short` / `BadgeClass` dengan benar; namespace `HcPortal.Models` tersedia secara global via `Views/_ViewImports.cshtml:2`. Tidak ada XSS baru — output helper hanya literal string yang aman.
- Sintaks Python `generate_bab_x.py` valid (lulus `ast.parse`).
- Dokumentasi (8 file HTML/MD/PY) konsisten — label produk di-rename ke "Single Choice / Multiple Answers", konteks generik ("pilihan ganda" sebagai deskripsi umum metode ujian) dipertahankan sesuai keputusan plan.

Tidak ditemukan bug, kebocoran rahasia, injeksi, atau regresi keamanan. Tiga catatan **Info** di bawah hanya bersifat saran kosmetik/konsistensi dokumentasi — bukan blocker.

## Info

### IN-01: Karakter Unicode `≥2` di label dropdown — opsional sanity-check

**File:** `Models/QuestionTypeLabels.cs:8`
**Issue:** Method `Long("MultipleAnswer")` mengembalikan `"Multiple Answers (≥2 jawaban benar)"` yang mengandung karakter Unicode U+2265 (`≥`). String literal C# meng-encode UTF-8 dan Razor secara default meng-encode HTML, sehingga karakter ini akan render dengan benar di browser modern. Namun ketika label digunakan kembali di konteks non-HTML (mis. log, plain-text email, dump CSV), karakter ini bisa menjadi mojibake jika tooling tidak men-set encoding ke UTF-8.
**Fix:** Tidak perlu diubah jika hanya dipakai di view Razor. Untuk antisipasi reuse, pertimbangkan padanan ASCII fallback (`"Multiple Answers (>=2 jawaban benar)"`) atau dokumentasikan asumsi UTF-8 di header file. Saat ini hanya dipakai di dropdown `Views/Admin/ManagePackageQuestions.cshtml:132` — aman.

### IN-02: Frase "pilihan ganda" generik masih banyak di Panduan-Penggunaan

**File:** `wwwroot/documents/guides/Panduan-Penggunaan-Website-HC-Portal-KPB.html:398, 411, 456, 505, 506, 565, 702, 703`
**Issue:** Phase plan menetapkan bahwa frase "pilihan ganda" sebagai deskripsi generik metode ujian online (bukan sebagai nama tipe soal produk) dipertahankan. Konteks di setiap baris tersebut adalah deskripsi mode ujian secara umum (mis. "Ujian online (pilihan ganda & essay, berdurasi)") — sesuai keputusan plan. Tidak ada label produk yang tertinggal menyebut "Pilihan Ganda" sebagai nama tipe soal pada controller atau view (verifikasi grep: 0 match).
**Fix:** Tidak perlu perubahan. Saran opsional: tambahkan footnote sekali di awal section assessment yang mengklarifikasi "pada produk HC Prime, tipe soal yang dimaksud adalah Single Choice / Multiple Answers / Essay" agar pembaca tidak bingung saat melihat UI yang menggunakan istilah baru.

### IN-03: Konsistensi nama tombol "Template MC" di card format reference

**File:** `Views/Admin/ImportPackageQuestions.cshtml:39`
**Issue:** Tombol download template menggunakan label baru "Template Single Choice" (good — line 39, 42, 45). Namun di card "Format kolom" line 32 list-item menyebut nilai DB literal `MultipleChoice` / `MultipleAnswer` / `Essay` di dalam `<code>` — itu sengaja karena memang nilai yang harus diisi user di kolom Excel (DB enum). Konsistensi sudah benar: label UI (button) = bahasa baru, nilai input data (code block) = nilai DB asli.
**Fix:** Tidak perlu perubahan. Saran opsional: tambah catatan kecil "(nilai sesuai sistem, tidak perlu mengubah)" pada line 32 untuk membantu user yang mungkin bingung saat melihat dropdown UI berbahasa "Single Choice" tapi diharuskan mengetik "MultipleChoice" di Excel.

---

## Verifikasi Cross-Cutting

| Aspek | Status | Bukti |
|---|---|---|
| D-17 schema lock (DB enum tidak berubah) | OK | `Models/AssessmentPackage.cs:44-48` masih `public string? QuestionType`, komentar XML masih merujuk `"MultipleChoice"`, `"MultipleAnswer"`, `"Essay"`. Tidak ada migrasi EF baru. |
| Whitelist validasi controller | OK | `Controllers/AssessmentAdminController.cs:4668, 4809` tetap `new[] { "MultipleChoice", "MultipleAnswer", "Essay" }`. |
| Helper null-safety | OK | `Models/QuestionTypeLabels.cs:5,13,21` menerima `string?` dan switch arm `_` mengembalikan default "Single Choice" / "bg-secondary". |
| Razor `@using HcPortal.Models` | OK | Tersedia global via `Views/_ViewImports.cshtml:2` — semua 5 view yang memanggil `QuestionTypeLabels.*` ter-resolve. |
| Tidak ada XSS baru | OK | Output helper berupa string literal yang aman; `@Html.Raw` di StartExam.cshtml hanya untuk JSON `JsonSerializer.Serialize` dari ViewBag (pre-existing pattern, di luar scope phase). |
| Sintaks Python valid | OK | `python -c "import ast; ast.parse(...)"` lulus untuk `generate_bab_x.py`. |
| Tidak ada `Pilihan Ganda` / `Multiple Answer` (singular) tertinggal di source | OK | Grep di `Controllers/` dan `Views/`: 0 match. |
| Audit perubahan controller (4 site saja) | OK | `git show 142bb609 --stat`: 1 file changed, 4 insertions(+), 4 deletions(-). Diff menunjukkan persis 2 site di CreateQuestion + 2 site di EditQuestion. |

---

_Reviewed: 2026-04-28_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
