# Phase 418: Opsi Jawaban Dinamis 2–6 - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

HC dapat membuat/mengubah soal dengan **2–6 opsi jawaban** (bukan terkunci A–D) di **form authoring web** (Kelola Soal) dan **form Inject** (v32.2), dan semua layar (ujian/preview/results) menampilkan **huruf A–F dinamis** dengan penilaian tetap benar. Mencakup: refactor kontrak HTTP `CreateQuestion`/`EditQuestion` (param diskret → list/array ≤6), validator min-2/max-6, render A–F dinamis.

Requirements: **OPT-01, OPT-02, OPT-03**. migration=FALSE.

**Sudah selesai di luar fase ini (jangan dikerjakan ulang):** Import Excel Opsi A–F + dual-format (IMP-01/02/03) sudah SHIPPED di Phase 415. Data model `PackageOption` (tanpa batas, tanpa field huruf) sudah dinamis. Export label Section + sync Pre→Post = Phase 419.
</domain>

<decisions>
## Implementation Decisions

### UX form authoring opsi (D-418-01 — keputusan user 2026-06-24)
- Mulai **4 baris** opsi (sesuai kebiasaan A–D lama), bukan 6 penuh, bukan 2.
- Tombol **"+ Tambah Opsi"** menambah baris hingga maksimum **6**.
- Tombol **"Hapus"** per baris ekstra, aktif hanya untuk baris di atas minimum (boleh hapus s/d tersisa **2** baris).
- Huruf A–F ditetapkan dinamis per posisi baris (display-only).
- Selektor jawaban benar (radio SingleAnswer / checkbox MultipleAnswer) mengikuti baris yang ada.

### Penanganan edit-shrink opsi yang sudah dijawab (D-418-02 — keputusan user 2026-06-24, harden sekarang)
- Saat `EditQuestion` MENGHAPUS/menyusutkan `PackageOption` (mis. 4→3, atau konversi tipe yang `RemoveRange(options)`), **guard sebelum `SaveChangesAsync`**: bila opsi yang akan dihapus punya `PackageUserResponse.PackageOptionId` yang mereferensikannya → **tolak dengan pesan jelas** (mis. "Opsi ini sudah dijawab peserta, tidak bisa dihapus.").
- Tujuan: cegah `DbUpdateException` FK Restrict (`PackageUserResponse → PackageOption` = Restrict, `ApplicationDbContext.cs:561`) yang sekarang melempar **500 mentah** (hazard backlog **999.14**, persis di jalur yang di-refactor 418). **Tutup 999.14 di jalur ini.**
- Tanpa kehilangan data; HC diberi tahu agar membatalkan/menyesuaikan.

### Kontrak HTTP (locked oleh spec §8.1)
- Ganti param diskret `optionA..D` + `correctA..D` (`CreateQuestion` AAC:7703, `EditQuestion` AAC:7924) → binding **list/array** opsi (≤6), masing-masing dgn teks + flag benar + (untuk authoring) gambar.
- **Pertahankan bentuk JSON response `EditQuestion` GET** (sudah variable-length `options[]`) supaya pemanggil AJAX `populateEditForm()` tetap jalan — audit & uji ulang semua pemanggil.

### Render huruf A–F dinamis (locked oleh spec §8.1 + §306)
- Generalisasi huruf index-derived `{A,B,C,D}` → **A–F** di `StartExam.cshtml`, `Results.cshtml`, `ExamSummary.cshtml` (sudah index-derived dgn fallback numerik — tinggal perluas array ke 6).
- **Perbaiki `PreviewPackage.cshtml`**: saat ini array huruf cap di **"E"** dengan `% letters.Length` (modulo → bug: opsi ke-6 tampil "A"). Jadikan A–F penuh tanpa wrap.
- Huruf murni tampilan per posisi (post-shuffle). **Grading tetap berbasis `PackageOption.Id`** (`GradingService.cs:110/121`) — tak berubah, sudah aman untuk opsi dinamis & acak.

### Validator (locked oleh spec D-06 + §8.1)
- `QuestionOptionValidator` (`Helpers/QuestionOptionValidator.cs:20`): tegakkan **min 2** (sudah ada) **+ max 6** (baru). Semua opsi ber-flag benar wajib punya teks (sudah ada). Per-soal bebas 2–6 tanpa setting tambahan (D-06).

### Form Inject parity (locked oleh spec §8.1 — "nyaris ke-miss")
- `_InjectQuestionForm.cshtml:35` (sekarang 4-tuple A–D hardcoded) ikut pola dinamis yang sama dgn authoring (tambah/hapus, A–F). Tanpa blok upload gambar (scope Phase 394 inject).

### Claude's Discretion
- Refactor JS `populateEditForm()` + `IMAGE_FIELDS` (`ManagePackageQuestions.cshtml:694-732`) dari hardcoded A–D ke enumerasi dinamis 0..n.
- Styling tombol Tambah/Hapus, layout baris, animasi.
- Tempat tepat guard edit-shrink dieksekusi (controller pre-check vs service) + wording pesan persisnya.
- Mekanisme binding array di action (model binder list vs FormCollection parse).
</decisions>

<specifics>
## Specific Ideas

- Semantik data **persis seperti import Excel**: kolom selalu sediakan slot A–F, yang **kosong diabaikan** saat simpan (spec §171). Form authoring jadi cerminan UI dari aturan import yang sudah ada.
- Per-soal bebas 2–6 **tanpa setting global tambahan** (D-06) — jumlah opsi = jumlah baris terisi.
- Jaga kompatibilitas-mundur: soal lama 4-opsi tetap tampil & dinilai identik.
</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Desain milestone v32.6 (sumber keputusan)
- `docs/superpowers/specs/2026-06-22-section-scoped-shuffle-pagination-dynamic-options-design.md` §8 "Opsi Jawaban Dinamis (2–6)" — daftar surface (kontrak HTTP, form authoring, form inject, render, preview, validator), aturan slot A–F kosong-diabaikan.
- spec §5.3 "PackageOption — opsi dinamis" — **tidak ada perubahan skema**; huruf display-only; grading by `PackageOption.Id`; max-6 di layer aplikasi.
- spec D-06 (§46) — max 6, min 2, per-soal bebas tanpa setting.
- spec §5.2.5 / §306 — audit form Pre-Post + `PreviewPackage` huruf cap "E" → A–F.

### Requirements
- `.planning/REQUIREMENTS.md` — OPT-01/02/03 (acceptance), catatan IMP-01/02/03 sudah selesai (415).

### Backlog terkait
- `.planning/ROADMAP.md` Phase 999.14 — EditQuestion hapus opsi sudah-dijawab → FK Restrict 500 (D-418-02 menutup ini di-jalur).
</canonical_refs>

<code_context>
## Existing Code Insights
(dari scout codebase 2026-06-24)

### Reusable Assets (sudah future-proof — tak perlu ubah)
- `Services/GradingService.cs:110,121` — grading match by `PackageOption.Id` (Single & MultipleAnswer set-equality). Agnostik jumlah/huruf opsi.
- `Models/AssessmentPackage.cs:107-131` — `PackageOption` tanpa field huruf (komentar eksplisit: huruf display-only). Tak ada migration.
- `EditQuestion` GET JSON (`AssessmentAdminController.cs:7874`) — sudah kembalikan `options[]` variable-length; form yang harus menyesuaikan, bukan response.
- Render letters sudah **index-derived + fallback numerik** di `StartExam.cshtml:137/146`, `Results.cshtml:363`, `ExamSummary.cshtml:57` — perluas array `{A..D}`→`{A..F}`.
- `Helpers/QuestionOptionValidator.cs:20` — sudah enforce min-2 + correct-must-have-text; tambah max-6.

### Lock points A–D yang harus di-generalisasi
- HTTP: `CreateQuestion` POST `AssessmentAdminController.cs:7703-7708` (optionA..D + correctA..D); `EditQuestion` POST `:7915-7930` (sama).
- Form authoring: `ManagePackageQuestions.cshtml:395` (foreach 4-tuple A–D hardcoded); `IMAGE_FIELDS` `:726-732` (5 entri optA..D); `populateEditForm()` JS `:694-709` (optLetters/optFields hardcoded, radio `correct_A`..).
- Form inject: `_InjectQuestionForm.cshtml:35` (4-tuple A–D hardcoded).
- Preview bug: `PreviewPackage.cshtml:6,62` (array cap "E" + `% letters.Length` modulo-wrap).

### Hazard (D-418-02 menangani)
- `AssessmentAdminController.cs:8082-8087` — `_context.PackageOptions.Remove(slot)` saat opsi dikosongkan (shrink), **tanpa guard** → `SaveChangesAsync` `:8102` lempar FK Restrict (`ApplicationDbContext.cs:561-564`) bila opsi sudah dijawab.

### Integration Points
- Sync Pre→Post (deep-clone) meng-copy `ICollection<PackageOption>` apa adanya → opsi 5–6 ikut tersalin otomatis; **verifikasi** clone tak mengasumsikan tepat-4 opsi (cek saat plan; perbaikan tuntas sync = Phase 419).
</code_context>

<deferred>
## Deferred Ideas

- **Excel "zero-config" dropdown (Data Validation) + import skor per-soal** — milestone terpisah yang DITUNDA (spec D-08; lihat memory `project_excel_zeroconfig_dropdown_deferred`). Bukan scope 418.
- **Export label/header Section per-soal (Excel/PDF)** + sync Pre→Post struktur Section + audit lintas-fitur + UAT milestone — **Phase 419** (PAG-04).
</deferred>

---

*Phase: 418-opsi-jawaban-dinamis-2-6*
*Context gathered: 2026-06-24*
