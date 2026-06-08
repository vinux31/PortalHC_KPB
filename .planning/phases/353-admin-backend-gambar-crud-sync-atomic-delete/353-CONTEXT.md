# Phase 353: Admin Backend Gambar (CRUD + Sync + Atomic Delete) - Context

**Gathered:** 2026-06-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin dapat mengelola gambar pada soal assessment dari halaman Manage Package Questions:
- Upload 1 gambar (JPG/PNG ≤5MB) ke soal + ke tiap opsi A-D (IMG-01/02)
- Isi alt text opsional per gambar (IMG-03)
- Ganti gambar lama (file lama dihapus disk) (IMG-05)
- Hapus gambar via checkbox (IMG-06)
- Saat edit, gambar lama tampil sebagai thumbnail (IMG-07)
- Preview render gambar soal+opsi di `_PreviewQuestion` (RND-04)
- Sinkron Pre→Post bawa ImagePath+ImageAlt via shared-file (SYN-01)
- Hapus file gambar atomic pola Phase 333 saat soal/opsi dihapus atau gambar di-replace (SYN-02)

Semua menyentuh satu file backend `AssessmentAdminController.cs` (CRUD ~L6067-6377, JSON prefill L6214, SyncPackagesToPost L5337, DeleteQuestion L6377) + view `ManagePackageQuestions.cshtml` + partial `_PreviewQuestion.cshtml`.

**Bukan scope ini:** render gambar di 6 layar peserta (Phase 354) + test/UAT konsolidasi (Phase 355).
</domain>

<decisions>
## Implementation Decisions

### Layout Form Upload
- **D-01:** **Opsi A — Inline kontekstual.** Field gambar soal diletakkan tepat di bawah textarea teks soal; field gambar opsi diletakkan inline di tiap baris opsi A-D (di bawah masing-masing `input-group`). Tiap field = thumbnail + tombol pilih/ganti file + (saat edit) checkbox hapus + input alt text. Alasan: konteks paling jelas (gambar nempel ke item-nya), klik paling sedikit, cocok dengan struktur form existing yang sudah per-baris A-D. Mockup disetujui user: `docs/mockup-presentasi/353-layout-form-gambar-mockup.html`.
- **D-02:** Form `#questionForm` WAJIB ditambah `enctype="multipart/form-data"` (saat ini hanya `method="post"` tanpa enctype) agar file upload terkirim.

### Edit — Tampil & Hapus/Ganti
- **D-03:** Saat edit soal: tampilkan **thumbnail gambar lama** per item (IMG-07). `<input type=file>` TIDAK bisa di-prefill (browser security) — yang ditampilkan adalah thumbnail dari ImagePath tersimpan, bukan isi file input.
- **D-04:** Mekanisme per item: **checkbox "Hapus gambar"** (IMG-06) + **pilih file baru = ganti** (IMG-05, file lama dihapus disk via atomic delete SYN-02).
- **D-05:** **Resolusi konflik:** jika admin centang "Hapus" DAN pilih file baru pada item yang sama → **file baru menang** (gambar baru tersimpan, file lama dihapus, checkbox hapus diabaikan). Pilih file baru = niat eksplisit mengganti.
- **D-06:** JSON `EditQuestion` GET perlu diperluas membawa `imagePath` + `imageAlt` untuk soal dan tiap opsi (Gap 3) supaya JS prefill bisa render thumbnail lama + isi alt.

### Preview
- **D-07:** **Keduanya** (RND-04 penuh): (1) thumbnail client-side instan saat admin pilih file (JS FileReader, sebelum submit) + (2) `_PreviewQuestion.cshtml` render `<img>` gambar soal + opsi setelah simpan. Saat ini `_PreviewQuestion` belum punya `<img>` sama sekali — tambahkan.

### Feedback Validasi Gagal
- **D-08:** Pesan error (file non-image / >5MB, hasil `ValidateImageFile`) ditampilkan via **alert atas `TempData["Error"]`** + form repopulate. Konsisten pola existing `CreateQuestion`/`EditQuestion`. TIDAK pakai inline per-field (hindari client-side validation JS tambahan tanpa pola existing).

### Carried Forward (locked dari Phase 352 + spec — jangan re-decide)
- **C-01:** Format JPG/PNG ≤5MB, magic-byte — panggil `FileUploadHelper.ValidateImageFile(IFormFile?)` (sudah ada, Phase 352). 5MB per Phase 352 D-03 (override 2MB spec).
- **C-02:** Folder upload `/uploads/questions/{packageId}/`. Reuse `FileUploadHelper.SaveFileAsync` (auto-create folder, format-agnostic, no resize).
- **C-03:** Sync Pre→Post = **shared-file** (SYN-01): Post copy pakai path file yang sama dengan Pre; sync TIDAK membuat/menghapus file fisik, hanya menyalin ImagePath+ImageAlt.
- **C-04:** Hapus file atomic **pola Phase 333** (SYN-02): kumpul path SEBELUM `BeginTransactionAsync`, `File.Delete` SETELAH `CommitAsync`, inner try/catch warn-only per file. Berlaku di `DeleteQuestion` + saat replace gambar.
- **C-05:** DB kolom `ImagePath` (nvarchar(max) null) + `ImageAlt` (nvarchar(255) null) di `PackageQuestion` + `PackageOption` sudah ada (migration `20260606030844_AddImageToPackageQuestionAndOption` applied lokal — re-applied 2026-06-08).

### Claude's Discretion
- Nama persis field form (mis. `questionImage`, `optionAImage`), bentuk DTO/binding model untuk multipart, struktur JS thumbnail handler, urutan operasi di POST handler — planner/executor putuskan.
- Apakah alt text soal & opsi pakai 1 helper render bersama atau inline — planner putuskan.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & Roadmap
- `docs/superpowers/specs/2026-06-06-image-in-assessment-questions-design.md` — spec induk v24.0 (§4 data, §6 upload, §10 keamanan, §12 backbone A→E). Phase 353 = backbone B (Admin CRUD) + C (Sync & Cleanup) digabung.
- `.planning/ROADMAP.md` — entri v24.0 + revisi 4 phase (kompresi old 353+354 → 353).
- `.planning/REQUIREMENTS.md` — IMG-01/02/03/05/06/07, RND-04, SYN-01/02 (IMG-01/02 ≤5MB sudah disesuaikan).

### Phase 352 (predecessor — fondasi dipakai)
- `.planning/phases/352-data-foundation-image-only-upload/352-CONTEXT.md` — D-01..D-06 (format JPG/PNG, 5MB override, no-resize, ValidateImageFile, folder/kolom).
- `Helpers/FileUploadHelper.cs` — `ValidateImageFile` (L45) + `SaveFileAsync` (reuse).
- `Models/AssessmentConstants.cs` — `AllowedImageExtensions` + `MaxImageFileSizeBytes`.
- `Models/AssessmentPackage.cs` — entity `PackageQuestion`/`PackageOption` ImagePath+ImageAlt.

### Pola atomic delete (acuan SYN-02)
- Lihat memory/Phase 333 (DeleteCoachingSession): declare List<string>? path outer tx + build inside tx + File.Delete POST CommitAsync + inner try/catch warn-only.

### Mockup (disetujui user)
- `docs/mockup-presentasi/353-layout-form-gambar-mockup.html` — layout Opsi A Inline (+ `353-mockup-preview.png`).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `FileUploadHelper.ValidateImageFile` + `SaveFileAsync` — validasi + simpan (Phase 352, siap pakai).
- Pola atomic delete Phase 333 — template untuk SYN-02.
- Pola TempData["Error"] + repopulate di CreateQuestion/EditQuestion existing — untuk D-08.

### Established Patterns
- Form `#questionForm` (`ManagePackageQuestions.cshtml` ~L122): `asp-action="CreateQuestion" method="post"` TANPA enctype → tambah `multipart/form-data` (D-02).
- Opsi fixed A-D: `optionA..optionD` + radio/checkbox `correctA..D` (L150-163). textarea `questionText` (L140).
- Edit pakai JS prefill via AJAX `EditQuestion` GET JSON + resetForm → perluas JSON dengan imagePath+imageAlt (D-06).
- `_PreviewQuestion.cshtml` render teks soal + opsi, BELUM ada `<img>` → tambah (D-07).

### Integration Points
- `AssessmentAdminController.cs`: `CreateQuestion` POST ~L6067, `EditQuestion` GET JSON ~L6196/L6214, `EditQuestion` POST ~L6241, `DeleteQuestion` POST ~L6377, `SyncPackagesToPost` L5337.
- Semua sisi backend gambar berada di satu file ini (sequential-strict, alasan merge old 353+354).
</code_context>

<specifics>
## Specific Ideas

- User minta mockup HTML "real dan bagus" untuk memilih layout → dibuat 3-opsi side-by-side dengan komponen real Portal HC + contoh nyata (diagram pompa, opsi Impeller/Casing/Shaft/Bearing). User pilih Opsi A Inline.
- Thumbnail instan saat pilih file (FileReader) diinginkan — admin mau lihat gambar sebelum submit.
</specifics>

<deferred>
## Deferred Ideas

- Render gambar di 6 layar peserta (StartExam/ExamSummary/Results/AssessmentMonitoringDetail/EditPesertaAnswers + _PreviewQuestion sisi peserta) → **Phase 354** (RND-01/02/03/05/06/07).
- Test xUnit konsolidasi + Playwright UAT end-to-end → **Phase 355** (TST-01/02).
- Server-side resize/kompres gambar → ditolak Phase 352 D-04 (prioritas simpel), tidak akan diangkat.

None lain — diskusi tetap dalam scope phase.
</deferred>

---

*Phase: 353-admin-backend-gambar-crud-sync-atomic-delete*
*Context gathered: 2026-06-08*
