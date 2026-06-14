# Phase 354: Render Gambar di 6 Layar - Context

**Gathered:** 2026-06-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Gambar soal + opsi (yang sudah bisa di-upload admin di Phase 353) tampil konsisten, responsif, dan aman di seluruh 6 layar tempat soal muncul, dengan data mengalir dari DB lewat ViewModel. Render-plumbing — TIDAK menambah CRUD/upload (itu Phase 353), TIDAK test/UAT konsolidasi (Phase 355).

6 layar:
- **Peserta (3):** `StartExam` (RND-01, saat ujian), `ExamSummary` (RND-02, review sebelum submit), `Results` (RND-03, pembahasan/hasil).
- **Admin (3):** `_PreviewQuestion` (RND-04 — **SUDAH selesai Phase 353**), `AssessmentMonitoringDetail` (RND-05, nilai essay — **gambar soal saja**, essay tak punya opsi), `EditPesertaAnswers` (RND-06, edit jawaban peserta — soal + opsi).
- **RND-07:** responsif (`img-fluid` + `loading=lazy` + `alt`) di semua layar.

**Bukan scope ini:** upload/CRUD/sync/delete (Phase 353 — DONE), xUnit konsolidasi + Playwright UAT end-to-end (Phase 355), render gambar Excel import (text-only, Phase 353 D-09).
</domain>

<decisions>
## Implementation Decisions

### Ukuran Gambar (cap tampilan)
- **D-01:** **Cap seragam semua layar** — gambar soal `max-height:240px`, gambar opsi `max-height:120px` (identik dengan `_PreviewQuestion.cshtml` Phase 353). `img-fluid` → scale turun preserve aspect ratio, tidak upscale. File disimpan full-res (Phase 352 D-04 no-resize); cap hanya CSS tampilan. Predictable + konsisten lintas surface.

### Klik Perbesar (Lightbox)
- **D-02:** **Lightbox di SEMUA 6 layar.** Inline tampil capped (D-01) + klik gambar → modal full-res (akses detail diagram teknis pompa/komponen). Best-practice assessment (Moodle/Canvas click-to-expand). Pakai **Bootstrap modal** (sudah ada di project, pola modal existing di ManagePackageQuestions/CMP) — bukan library lightbox baru. File full-res tersedia (no-resize) → modal tampilkan resolusi asli. Lightbox trigger jadi bagian partial reusable (D-04).

### Penempatan Gambar Opsi
- **D-03:** **Gambar opsi di BAWAH teks opsi, full block** (img-fluid cap 120px) — mirror `_PreviewQuestion.cshtml` 353 (gambar disisipkan setelah `<span>@opt.OptionText</span>`). Konsisten, aman di mobile (tidak sempit seperti layout samping/inline).

### Pola Render (DRY)
- **D-04:** **1 partial reusable** dipakai 6 layar — buat partial render gambar (mis. `Views/Shared/_QuestionImage.cshtml` + `_OptionImage.cshtml`, ATAU 1 partial parametrik soal/opsi — planner putuskan bentuk persis). Partial: render `<img class="img-fluid" loading="lazy" alt="@ImageAlt" style="max-height:{cap}">` HANYA bila ImagePath non-null + wiring trigger lightbox (D-02). Satu tempat ubah → anti-drift markup. Sejalan dgn lightbox-semua-layar (1 implementasi modal).

### Claude's Discretion
- Bentuk persis partial (1 parametrik vs 2 terpisah soal/opsi), nama file partial.
- Mekanisme lightbox persis (1 modal global reuse + JS set src on click vs per-image) — planner/executor putuskan; syarat: Bootstrap modal existing, src ber-encode, no XSS surface baru.
- Apakah cap di-pass sebagai param partial atau hardcode per pemanggilan.
- Nama field ViewModel baru (ikuti konvensi existing tiap VM).

### Locked dari ROADMAP/Spec (jangan re-decide)
- **L-01 (Gap 2 — ViewModel plumbing):** 4 ViewModel bawa `ImagePath`+`ImageAlt` di level soal & opsi, diisi saat populate:
  - `ExamQuestionItem`/`ExamOptionItem` (`Models/PackageExamViewModel.cs`) — saat ini BELUM ada field gambar. Populate di `CMPController` StartExam (~L1055).
  - `QuestionReviewItem`/`OptionReviewItem` (`Models/AssessmentResultsViewModel.cs`) — populate `CMPController` Results (~L2300).
  - `EssayGradingItemViewModel` (`Models/AssessmentMonitoringViewModel.cs`) — gambar **soal saja** (RND-05). Populate `AssessmentAdminController` essay grading (~L3401).
  - Item ViewModel untuk **ExamSummary** & **EditPesertaAnswers** (`Models/EditPesertaAnswersViewModel.cs`) — bawa gambar soal+opsi.
- **L-02 (markup SC#4 / RND-07):** `<img class="img-fluid" loading="lazy" alt="@ImageAlt">`, `src` ber-encode Razor (bukan HTML mentah → tak nambah surface XSS), render **hanya jika** `ImagePath != null`.
- **L-03 (shuffle aman):** opsi di-shuffle object-level (spec §8) → gambar opsi ikut objek, otomatis benar. Tidak perlu penanganan khusus.
- **L-04 (no migration):** Phase 354 tidak ada migration (kolom sudah ada sejak Phase 352).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Roadmap
- `docs/superpowers/specs/2026-06-06-image-in-assessment-questions-design.md` — spec induk v24.0. §4 data, §6 upload, **§8 shuffle (object-level, gambar opsi aman)**, §10 keamanan, §12 backbone (Phase 354 = backbone D Render). Baca §8 + bagian render 6 layar.
- `.planning/ROADMAP.md` — entri Phase 354 (Goal + 5 Success Criteria + daftar 4 ViewModel & lokasi populate).
- `.planning/REQUIREMENTS.md` — RND-01/02/03/05/06/07 + RND-07 responsif.

### Phase 353 (predecessor — pola dipakai/ditiru)
- `.planning/phases/353-admin-backend-gambar-crud-sync-atomic-delete/353-CONTEXT.md` — C-03 shared-file, decisions gambar admin.
- `.planning/phases/353-admin-backend-gambar-crud-sync-atomic-delete/353-03-SUMMARY.md` — pola render `<img>` di `_PreviewQuestion.cshtml` (soal 240px + opsi 120px lazy) = **template render yang ditiru 5 layar lain**.
- `Views/Admin/_PreviewQuestion.cshtml` — implementasi `<img>` referensi (RND-04, sudah live).
- `Models/AssessmentPackage.cs` — entity `PackageQuestion`/`PackageOption` `ImagePath`+`ImageAlt` (sumber data).

### Phase 352 (fondasi)
- `.planning/phases/352-data-foundation-image-only-upload/352-CONTEXT.md` — **D-04 no server-side resize** (alasan file full-res → lightbox tampil resolusi asli).

### Target files (render)
- `Views/CMP/StartExam.cshtml`, `Views/CMP/ExamSummary.cshtml`, `Views/CMP/Results.cshtml` (peserta).
- `Views/Admin/AssessmentMonitoringDetail.cshtml`, `Views/Admin/EditPesertaAnswers.cshtml` (admin).
- `Controllers/CMPController.cs` (populate StartExam ~L1055, Results ~L2300), `Controllers/AssessmentAdminController.cs` (essay grading ~L3401).
- ViewModels: `PackageExamViewModel.cs`, `AssessmentResultsViewModel.cs`, `AssessmentMonitoringViewModel.cs`, `EditPesertaAnswersViewModel.cs`.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_PreviewQuestion.cshtml` (Phase 353): pola render `<img img-fluid rounded border loading=lazy>` soal 240px + opsi 120px → **disalin/diangkat jadi partial reusable** (D-04).
- Bootstrap modal: sudah dipakai di `ManagePackageQuestions.cshtml` (preview modal) + view CMP → reuse untuk lightbox (D-02), tidak perlu library baru.
- Entity `PackageQuestion`/`PackageOption` `ImagePath`/`ImageAlt` (Phase 352) — sumber data, sudah terisi via Phase 353.

### Established Patterns
- Render view Razor server-side (bukan SPA) — `@Model`-driven, partial via `@await Html.PartialAsync`.
- ViewModel item-per-soal/opsi sudah ada di 4 VM (tinggal +2 field gambar masing-masing).
- Opsi shuffle object-level (spec §8) — gambar nempel objek opsi, tak perlu reindex.

### Integration Points
- `CMPController` StartExam/Results populate loop: tambah set `ImagePath`/`ImageAlt` saat map entity→VM item.
- `AssessmentAdminController` essay grading populate (~L3401): set gambar soal di `EssayGradingItemViewModel`.
- 6 view: panggil partial render di titik soal + (kecuali essay-monitoring) titik opsi.
</code_context>

<specifics>
## Specific Ideas

- User konfirmasi best-practice: tampilan inline capped + lightbox akses full-res = standar assessment (Moodle/Canvas click-to-expand) untuk diagram teknis. File full-res dipertahankan (no-resize 352).
- Konsistensi visual dgn admin preview 353 diinginkan (cap 240/120 sama, gambar opsi di bawah teks).
</specifics>

<deferred>
## Deferred Ideas

- Server-side resize/thumbnail generation → ditolak Phase 352 D-04, tidak diangkat (lightbox kasih full-res tanpa perlu resize).
- xUnit konsolidasi + Playwright UAT end-to-end admin→peserta → **Phase 355** (TST-01/02).
- Gambar di Excel import → out of scope (Phase 353 D-09, text-only).

None lain — diskusi tetap dalam scope render.
</deferred>

---

*Phase: 354-render-gambar-di-6-layar*
*Context gathered: 2026-06-08*
