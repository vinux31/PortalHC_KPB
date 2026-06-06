# Desain: Gambar di Soal Assessment (Manage Package)

- **Tanggal:** 2026-06-06
- **Status:** Approved (brainstorm) — siap jadi milestone GSD baru
- **Penulis:** Rino (via brainstorming)

## 1. Latar Belakang & Tujuan

Saat ini soal assessment (entity `PackageQuestion`) hanya bisa berisi teks. Tidak ada
fasilitas menampilkan gambar. Banyak materi teknis (diagram kompresor, grafik, foto alat,
APD) butuh gambar agar soal jelas.

**Tujuan:** admin bisa melampirkan gambar pada **soal** dan pada **tiap pilihan jawaban**,
lalu gambar tampil konsisten di semua layar tempat soal muncul.

Berlaku untuk semua tipe soal:
- **MultipleChoice** (1 jawaban benar) — soal + opsi punya gambar
- **MultipleAnswer** (≥2 benar) — soal + opsi punya gambar
- **Essay** — hanya soal (essay tidak punya opsi)

## 2. Keputusan (hasil brainstorm)

| # | Keputusan | Pilihan |
|---|-----------|---------|
| 1 | Gambar nempel di mana | Soal **dan** tiap pilihan jawaban |
| 2 | Jumlah maksimal | **1** gambar per soal + **1** gambar per opsi |
| 3 | Alt text | Tambah field alt, **opsional** (tidak wajib isi) |
| 4 | Bulk import | **Manual-only** — import Excel tetap teks; gambar via Edit per soal |
| 5 | Tampil di layar mana | **Semua 6 layar** (Pilihan B, lihat §5) |

## 3. Ruang Lingkup (Scope)

### Termasuk
- 1 migration (4 kolom baru)
- Field gambar di entity `PackageQuestion` & `PackageOption`
- Upload gambar di form admin (per soal + per opsi)
- Render gambar di 6 layar
- Sinkron gambar saat Pre→Post (SamePackage)
- Helper upload mode image-only
- Hapus file gambar saat soal/opsi/gambar dihapus (atomic, pola Phase 333)
- Test (xUnit + Playwright)

### TIDAK termasuk (out of scope)
- Bulk import gambar (zip/URL) — keputusan #4
- Banyak gambar per soal/opsi (galeri) — keputusan #2
- Edit/crop gambar dalam aplikasi
- Image CDN / srcset multi-resolusi (cukup `img-fluid` + `loading=lazy`)
- Perbaikan XSS `@q.QuestionText` render bare yang sudah ada (catatan di §8, diputuskan terpisah)

## 4. Desain Data (Database)

Tambah **4 kolom** lewat 1 migration. Semua **nullable** → data soal lama tetap aman.

| Tabel | Kolom | Tipe | Isi |
|-------|-------|------|-----|
| `PackageQuestions` | `ImagePath` | `nvarchar(max)` null | path relatif file, mis. `/uploads/questions/12/abc.jpg` |
| `PackageQuestions` | `ImageAlt` | `nvarchar(255)` null | deskripsi alt (opsional) |
| `PackageOptions` | `ImagePath` | `nvarchar(max)` null | path relatif gambar opsi |
| `PackageOptions` | `ImageAlt` | `nvarchar(255)` null | deskripsi alt opsi (opsional) |

Lokasi entity: `Models/AssessmentPackage.cs` (`PackageQuestion` L27, `PackageOption` L63).
Migration baru di `Migrations/` — **wajib di-commit** (lihat DEV_WORKFLOW), notifikasi IT
dengan flag migration.

## 5. Layar yang Render Soal (Pilihan B — 6 layar)

| # | Layar | View | Sisi | Render |
|---|-------|------|------|--------|
| 1 | Ujian | `Views/CMP/StartExam.cshtml` | Peserta | gambar soal + gambar opsi |
| 2 | Review sebelum submit | `Views/CMP/ExamSummary.cshtml` | Peserta | gambar soal + opsi |
| 3 | Pembahasan/hasil | `Views/CMP/Results.cshtml` | Peserta | gambar soal + opsi |
| 4 | Preview admin | `Views/Admin/_PreviewQuestion.cshtml` | Admin | gambar soal + opsi |
| 5 | Nilai essay | `Views/Admin/AssessmentMonitoringDetail.cshtml` | Admin | gambar soal (essay) |
| 6 | Edit jawaban peserta | `Views/Admin/EditPesertaAnswers.cshtml` | Admin | gambar soal + opsi |

Pola render aman (semua layar):
```html
<img src="@question.ImagePath" alt="@question.ImageAlt" class="img-fluid" loading="lazy" style="max-height:400px" />
```
- `img-fluid` → auto-mengecil di HP (responsive)
- `loading="lazy"` → muat saat perlu, halaman cepat
- path lewat atribut `src` (di-encode Razor) — **bukan** HTML mentah → aman XSS
- render hanya jika `ImagePath` tidak null

## 6. Upload

Pakai ulang `Helpers/FileUploadHelper.cs` (`SaveFileAsync`).

- **Format:** hanya **JPG/PNG** (PDF ditolak untuk gambar soal)
- **Validasi magic-byte** (cek isi file beneran gambar) — sudah ada di helper
- **Nama file** auto-aman: `timestamp_GUID_nama` + strip direktori (anti path-traversal) — sudah ada
- **Folder:** `/uploads/questions/{packageId}/`
- **Batas ukuran: 2 MB** per gambar (lebih masuk akal dari default 10 MB; selaras best practice LMS — Schoology 512KB, web ≤1–2MB)

### Gap 4 — helper image-only (PERLU KERJA)
Helper saat ini izinkan PDF+JPG+PNG (mode sertifikat) lewat
`AssessmentConstants.FileValidation.AllowedCertificateExtensions`. Belum ada mode image-only.
**Solusi:** tambah overload/param agar bisa membatasi ke JPG/PNG saja (tolak PDF), magic-byte tetap.

## 7. Form Admin (`ManagePackageQuestions`)

Controller: `Controllers/AssessmentAdminController.cs`
- `CreateQuestion` POST (~L6067)
- `EditQuestion` GET JSON (~L6196) + POST (~L6241)
- `DeleteQuestion` POST (~L6377)

Per soal & per opsi, form tambah:
- Input upload gambar (`<input type="file" accept="image/*">`)
- Input alt text (opsional)
- Jika sudah ada gambar: thumbnail + checkbox **"Hapus gambar"**

### Gap 3 — prefill edit (PERLU KERJA)
`EditQuestion` GET balikin JSON untuk isi ulang form. JSON sekarang **tak ada** field gambar
(`id, order, questionText, questionType, scoreValue, elemenTeknis, rubrik, maxCharacters, options[{optionText,isCorrect}]`).
**Solusi:** tambah `imagePath`+`imageAlt` ke JSON (level soal & tiap opsi) ~L6214.

## 8. Gap & Resolusi (hasil verifikasi kode)

### Gap 1 — Sinkron Pre→Post tak bawa gambar ⚠️ KRITIS
`SyncPackagesToPost` (`AssessmentAdminController.cs` L5337) deep-clone seluruh soal Pre→Post
saat `SamePackage=true` (trigger di create L6176 & edit L6356). Blok copy sekarang hanya
menyalin 7 field soal (`QuestionText, Order, ScoreValue, QuestionType, ElemenTeknis, Rubrik,
MaxCharacters`, L5370) + 2 field opsi (`OptionText, IsCorrect`, L5379). **ImagePath/ImageAlt
tidak ikut.**

**Resolusi — file dibagi (shared), bukan digandakan fisik:**
- Tambah `ImagePath`+`ImageAlt` ke blok copy soal & opsi
- Copy Post **memakai path file yang sama** dengan Pre (cuma menyalin string path)
- Sync **tidak pernah** membuat/menghapus file fisik
- Lifecycle file fisik **hanya dimiliki paket Pre** — file dihapus hanya saat aksi di Pre (replace/delete gambar/soal/opsi)
- Alasan: sync jalan tiap edit & nge-drop-recreate seluruh Post → kalau file digandakan fisik akan terus meng-orphan file lama. Berbagi path lebih aman & konsisten.
- Edge: gambar di-upload langsung pada soal Post-only (tak terhubung Pre) → file dimiliki Post; folder per-packageId menampung kasus ini.

### Gap 2 — ViewModel tak bawa gambar
Field gambar belum ada di ViewModel sehingga data DB tak sampai ke layar:
- `Models/PackageExamViewModel.cs:25` `ExamQuestionItem` + `:43` `ExamOptionItem`
- `Models/AssessmentResultsViewModel.cs:24` `QuestionReviewItem` (+ `OptionReviewItem`)
- `Models/AssessmentMonitoringViewModel.cs:73` `EssayGradingItemViewModel`
- ViewModel/item untuk `ExamSummary.cshtml` & `EditPesertaAnswers.cshtml`

**Resolusi:** tambah `ImagePath`+`ImageAlt` ke tiap ViewModel + isi saat populate
(`CMPController` L1055 StartExam & L2300 Results; `AssessmentAdminController` L3401 essay grading).

### Gap 3 — Prefill edit JSON (lihat §7)

### Gap 4 — Helper image-only (lihat §6)

### Gap 5 — Preview admin tak render gambar opsi
`_PreviewQuestion.cshtml` (L45-63) render teks opsi saja.
**Resolusi:** tambah `<img>` di preview soal + tiap opsi.

### Sudah AMAN ✅
- **Shuffle opsi** (`StartExam.cshtml` L126-128): opsi dishuffle via objek `ExamOptionItem`
  penuh (`q.Options.FirstOrDefault(o => o.OptionId == oid)`), bukan teks saja → `ImagePath`
  ikut otomatis. Tidak perlu kerja tambahan.

### Catatan keamanan (di luar scope, diputuskan terpisah)
`@q.QuestionText` & `@opt.OptionText` saat ini di-render bare (tanpa encoding eksplisit) di
beberapa view. Aman untuk teks; menjadi perhatian jika kelak ada konten kaya. Gambar di fitur
ini di-render via atribut `src` ber-encode, **tidak** menambah permukaan XSS. Perbaikan
render bare existing = keputusan terpisah, tidak digabung ke milestone ini.

## 9. Hapus File (Atomicity)

Pakai pola Phase 333/335 (sudah mature):
- Kumpulkan path gambar **sebelum** `BeginTransactionAsync`
- Bangun list dari data INSIDE tx jika perlu
- `File.Delete` loop **setelah** `CommitAsync`, inner try/catch warn-only per file (tidak throw)

Berlaku saat:
- `DeleteQuestion` (sudah ada cascade options+responses → tambah kumpul `ImagePath` soal+opsi)
- Gambar di-replace via Edit (hapus file lama setelah commit)
- (Sync **tidak** menghapus file — lihat Gap 1)

## 10. Keamanan

- Upload: magic-byte + sanitasi nama file (helper handle)
- Image-only: tolak PDF/non-image (Gap 4)
- Render: `src` ber-encode, tanpa HTML mentah
- Batas 2 MB cegah abuse storage
- Folder per-package biar rapi & memudahkan cleanup

## 11. Testing

### xUnit
- Migration apply bersih (kolom muncul, data lama null aman)
- Upload valid (JPG/PNG) tersimpan; non-image (PDF/exe) **ditolak**
- `SyncPackagesToPost` menyalin `ImagePath`+`ImageAlt` (Pre→Post)
- `DeleteQuestion` menghapus file gambar soal+opsi (post-commit)
- Replace gambar menghapus file lama

### Playwright (UAT)
- Admin upload gambar soal + gambar tiap opsi → simpan
- Admin edit soal → thumbnail gambar lama muncul (prefill) → bisa ganti/hapus
- Preview admin tampil gambar soal + opsi
- Peserta `StartExam`: gambar soal + opsi tampil, responsive
- Peserta `Results` (pembahasan): gambar soal + opsi tampil
- Admin `AssessmentMonitoringDetail`: gambar soal essay tampil

## 12. Saran Pemecahan Phase (untuk milestone GSD)

Usulan (final ditentukan saat `/gsd-new-milestone` + roadmap):
1. **Phase A — Data & Upload**: migration 4 kolom + entity + helper image-only + folder
2. **Phase B — Admin CRUD**: form upload/alt/remove + Create/Edit/Delete + JSON prefill (Gap 3) + preview (Gap 5)
3. **Phase C — Sync & Cleanup**: SyncPackagesToPost (Gap 1) + atomic file delete (§9)
4. **Phase D — Render peserta & admin**: 4 ViewModel (Gap 2) + 6 view render (§5)
5. **Phase E — Test & UAT**: xUnit + Playwright (§11)

## 13. Referensi Best Practice (research eksternal)

- Alt text wajib untuk aksesibilitas screen reader, ≤100 char; Moodle sediakan field alt per gambar soal.
- Decorative vs informatif: alt opsional untuk hiasan, disarankan untuk diagram/grafik.
- Ukuran: Schoology cap 512KB; best practice web ≤100–200KB, max 1MB; kita pakai cap 2MB sebagai batas atas aman.
- Format: JPG untuk foto, PNG untuk grafik/diagram/teks.
- Responsive: `img-fluid`/srcset + `loading="lazy"`.

Sumber:
- Building Accessible Quizzes in Canvas — Illinois State
- Add Image Descriptions — Anthology Ally / Blackboard
- Optimising Images in Moodle & Totara — Catalyst EU
- Upload images to assessment questions — PowerSchool/Schoology
- Best image sizes for websites 2026 — ForegroundWeb
