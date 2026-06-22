# Desain: Section + Scoped Shuffle + Section Pagination + Opsi Jawaban Dinamis

**Tanggal:** 2026-06-22
**Status:** Disetujui untuk lanjut ke perencanaan (writing-plans)
**Penulis:** Brainstorm Rino + Claude
**Modul:** Assessment (PortalHC_KPB)

---

## 1. Ringkasan & Tujuan

Mengangkat konsep template Excel referensi (`docs/KPB - Licensor Training - SRU - Pre Test batch 1.xlsx`, gaya ClassMarker) ke dalam sistem web. HC/Admin dapat:

1. Mengelompokkan soal ke dalam **Section** (mis. per area/equipment) di dalam tiap paket.
2. Mengacak soal & pilihan **hanya di dalam lingkup section** (scoped shuffle) — soal tidak melompat antar-section; acak bisa dimatikan per-section.
3. Mengatur **pagination per-section** (section tertentu mulai di halaman baru).
4. Menggunakan **jumlah opsi jawaban dinamis (2–6)**, bukan lagi kunci A–D.
5. Mengunggah semua ini lewat **import Excel** yang diperluas.

**Prinsip utama:** kompatibel-mundur 100%. Soal/assessment tanpa Section = perilaku persis seperti sekarang.

---

## 2. Scope

### Termasuk (milestone ini)
- Entity **Section** per paket + kolom `SectionId` (nullable) pada soal.
- Refactor **ShuffleEngine** menjadi acak per-section (generalisasi dari acak lintas-paket sekarang).
- **Pagination** sadar-section (toggle "Mulai Halaman Baru" per section).
- **Opsi jawaban dinamis 2–6** (refactor authoring UI, exam render, kontrak HTTP, preview, import).
- **Import Excel** format baru (kolom Section + Opsi E–F) dengan dual-format kompatibel-mundur.
- Penyesuaian alur yang tersentuh: **Inject (v32.2)**, **sync Pre/Post**, **export per-soal**, **preview**, **monitoring** (label saja).

### TIDAK termasuk (milestone terpisah)
- Excel "zero-config skor + dropdown data-validation" (quick-win, milestone A terpisah).
- Fitur **hapus peserta** (sudah selesai di v32.5, tinggal deploy).
- Tipe soal baru (Matching / File Upload / Text-Media). Tetap **Single / Multiple / Essay**.
- **Breakdown skor per-section** di hasil/sertifikat (keputusan P1=a → tidak). Breakdown per-Elemen-Teknis yang sudah ada tetap dipakai.

---

## 3. Keputusan Terkunci (Decision Log)

| Kode | Keputusan | Nilai |
|------|-----------|-------|
| D-01 | Lingkup milestone | Grup "berat & saling bergantung" (Section, scoped shuffle, pagination, opsi dinamis) |
| D-02 | Section vs Elemen Teknis | Section = konsep BARU & terpisah; **1 Section memuat beberapa Elemen Teknis** |
| D-03 | Penempatan Section | **Di dalam tiap Paket** (Tipe A/B/C), bukan level assessment |
| D-04 | Penentu urutan section | **Kolom "No. Section" eksplisit** (HC ketik 1, 2, 3) |
| D-05 | Section opsional | Boleh dikosongkan; kosong = perilaku global lama (kompatibel-mundur) |
| D-06 | Max opsi jawaban | **6** (min tetap 2); per-soal bebas 2–6 tanpa setting tambahan |
| D-07 | Tipe soal | Tetap Single / Multiple / Essay |
| D-08 | Strategi milestone | **Pisah**: milestone ini = berat; quick-win Excel = milestone lain |
| D-09 | Model acak >1 paket | Per section: kumpulkan section padanan dari **semua paket** (antar-paket) lalu acak/sampling **di dalam batas section**; syarat jumlah soal per section sama antar-paket |
| D-10 | Pagination | Default 10 soal/halaman mengalir + header section; toggle **"Mulai Halaman Baru" per-section** + tombol cepat "semua section pisah halaman"; section panjang auto-pecah per 10 |
| D-11 | Kontrol halaman | Tingkat **section** (BUKAN page-number per soal — karena acak merusaknya) |
| D-12 | Breakdown skor per-section (P1) | **Tidak** — cukup breakdown per-Elemen-Teknis yang sudah ada |
| D-13 | Struktur section antar-paket mismatch (P2) | **Tolak keras** — blokir simpan/mulai ujian sampai diperbaiki |
| D-14 | Precedence toggle acak | Toggle level-assessment = saklar INDUK; toggle per-section = ANAK (hanya berlaku saat induk ON) |
| D-15 | Soal tanpa section di paket bersection | Masuk 1 grup implisit "Lainnya" di urutan terakhir |

---

## 4. Hierarki & Konsep

```
AssessmentSession (event ujian)
└── AssessmentPackage (Tipe A / B / C)            ← varian set soal, anti-nyontek
    └── AssessmentPackageSection (BARU)           ← No.Section, Nama, MulaiHalamanBaru, AcakOn
        └── PackageQuestion (punya SectionId nullable + ElemenTeknis tag)
            └── PackageOption (2–6, dinamis)
```

- **Section** = pengelompokan kasar (area/equipment). **Elemen Teknis** = sub-kompetensi; 1 Section ⊃ beberapa ET (D-02).
- Section milik **satu paket** (D-03). Saat >1 paket, section dengan `No.Section` yang sama dianggap padanan antar-paket.
- `SectionId = null` → soal "tanpa section" → perilaku global lama (D-05).

---

## 5. Model Data

### 5.1 Tabel baru: `AssessmentPackageSection`
| Kolom | Tipe | Catatan |
|-------|------|---------|
| `Id` | int PK | |
| `AssessmentPackageId` | int FK | → AssessmentPackage |
| `SectionNumber` | int | unik per paket; penentu urutan tampil |
| `Name` | nvarchar null | label (mis. "Area Reaktor") |
| `StartNewPage` | bit default 0 | toggle pagination per-section (D-10) |
| `ShuffleEnabled` | bit default 1 | toggle acak per-section (D-14) |

Index unik: `(AssessmentPackageId, SectionNumber)`.

### 5.2 Perubahan `PackageQuestion`
- Tambah `SectionId` int? (FK nullable → AssessmentPackageSection). `null` = tanpa section.
- (Tetap) `ElemenTeknis`, `Order`, `ScoreValue`, dst — tidak berubah.

### 5.3 `PackageOption` — opsi dinamis
- **Tidak ada perubahan skema** (sudah `ICollection<PackageOption>`, tanpa batas, tanpa field huruf). Huruf A–F murni tampilan saat render. Grading **berbasis `PackageOption.Id`** (sudah terbukti aman). Batas max-6 ditegakkan di **layer aplikasi** (UI + validator + import), bukan DB.

### 5.4 Migrasi
- 1 migration: buat tabel `AssessmentPackageSection` + kolom `PackageQuestion.SectionId` (nullable).
- **Non-breaking**: soal lama `SectionId = null`; assessment lama tak berubah perilaku.
- Rollback: drop tabel + kolom.

> Catatan migration flag untuk IT: **migration = TRUE** untuk fase fondasi Section.

---

## 6. Alur Acak (Scoped Shuffle)

### 6.1 Prinsip generalisasi
ShuffleEngine sekarang mengumpulkan **semua soal lintas-paket** jadi 1 kolam lalu ET-aware K-min sampling + Fisher-Yates. Desain baru: **operasi yang sama, tapi dijalankan per-section**. "Tanpa section" = 1 section raksasa = persis perilaku sekarang.

### 6.2 Refactor
- Ekstrak fungsi murni baru: `ShuffleEngine.BuildSectionQuestionAssignment(section, allSiblingPackages, shuffleEnabled, workerIndex, rng)`.
- `BuildQuestionAssignment` (entry lama) menjadi: iterasi section sesuai `SectionNumber` urut → panggil fungsi per-section → gabungkan hasil terurut (section 1 → 2 → 3 → … → "Lainnya").
- **Phase-1 ET-aware** diubah dari kunci `ET` global menjadi **kunci komposit `(SectionNumber, ET)`**: untuk tiap section, jamin cakupan semua ET yang ADA di section itu (subset ET global). ET yang membentang 2 section → ditangani independen di tiap section (ET-SECTION-01).

### 6.3 Per-mode
- **`ShuffleEnabled` section = ON:** acak soal di dalam section (Fisher-Yates) + acak opsi.
- **`ShuffleEnabled` section = OFF:** urut `Question.Order` di dalam section; section tetap di posisinya.
- **Precedence (D-14):** toggle assessment `ShuffleQuestions` = induk. Jika induk OFF → semua section terurut (`ShuffleEnabled` per-section diabaikan). Jika induk ON → tiap section ikuti `ShuffleEnabled`-nya. Sama untuk `ShuffleOptions`.

### 6.4 Satu paket vs banyak paket
- **1 paket:** peserta kerjakan paket itu; per section, tampilkan semua soal section (acak di dalamnya jika ON).
- **>1 paket:** untuk tiap section, kumpulkan soal section padanan dari semua paket → sampling/acak K = jumlah-slot-section (= jumlah soal section per paket) → di dalam batas section. ET-aware di dalam section.

### 6.5 Determinisme & lock
- Seed per peserta via `workerIndex` (urutan sibling sorted) tetap. Reshuffle deterministik.
- **Shuffle lock final** (`UserPackageAssignment` terkunci setelah load pertama). Section hanya berlaku bila di-set **sebelum** ujian mulai. Section ditambah setelah peserta mulai → diabaikan untuk peserta itu (Q7).

---

## 7. Pagination per-Section

### 7.1 Aturan
- Soal terurut: Section 1 → 2 → 3 → … → "Lainnya". Di dalam section sesuai hasil acak.
- **Default page size = 10 soal/halaman** (mobile 5).
- Saat ganti section → tampilkan **header section** (boleh muncul di tengah halaman pada mode default).
- **`StartNewPage` section = ON** → page break SEBELUM section itu (mulai halaman baru). Section lain tak terpengaruh.
- Tombol cepat **"Semua section mulai halaman baru"** = set `StartNewPage = true` untuk semua section.
- Section panjang tetap auto-pecah tiap 10 soal di dalam halamannya.

### 7.2 Implementasi render
- `StartExam` controller: setelah `ShuffledQuestionIds` terbentuk, hitung `PageNumber` per soal saat render: iterasi soal urut; naikkan counter halaman bila (a) `StartNewPage=true` untuk section soal, atau (b) halaman sudah berisi 10 soal.
- `ViewBag.SectionConfig` dikirim ke view (daftar section + toggle). View kelompokkan & sisipkan header.
- **`LastActivePage` tetap `int?` (nullable) page-index global** (tak ubah skema). Saat reshuffle/resume → hitung ulang dari config section. Legacy (tanpa section) → nilai apa adanya; `null`/di luar rentang → fallback aman ke halaman 0. Aturan resume saat config berubah: lihat §15.A.
- Page-number disimpan **bukan** per soal (acak merusaknya, D-11) — selalu dihitung saat render.

---

## 8. Opsi Jawaban Dinamis (2–6)

### 8.1 Yang sudah aman
Data model, grading (`PackageOption.Id`), validator inti (`QuestionOptionValidator.ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)`), dan export sudah dinamis.

### 8.2 Yang harus diubah (semua hardcode A–D)
| Lokasi | Perubahan |
|--------|-----------|
| `CreateQuestion` / `EditQuestion` (kontrak HTTP) | Ganti param diskret `optionA..D` → binding list/array (≤6). Risiko: pemanggil lama; uji ulang. |
| `ManagePackageQuestions.cshtml` (form) | Loop opsi dinamis 0..Max; `IMAGE_FIELDS` dibangun 0..Max; `populateEditForm()` JS loop dinamis. |
| `_InjectQuestionForm.cshtml` (Inject v32.2) | Loop opsi dinamis (nyaris ke-miss). |
| `StartExam.cshtml` (render ujian) | Array huruf `{A..F}`; index aman `Math.Min(oi,5)`; loop `Options.Count`. |
| `_PreviewQuestion.cshtml` | Huruf dinamis. |
| `Results.cshtml`, `ExamSummary.cshtml` | Huruf A–F dinamis (review jawaban). |
| `ExtractPackageCorrectLetter()` + validasi import | `'ABCD'` → `'ABCDEF'` (baris ~7057/7059); whitelist jawaban `{A..F}`. |
| `QuestionOptionValidator` | Tegakkan min 2, max 6. |

### 8.3 Konfigurasi
- Per-soal bebas 2–6 (D-06). Kolom Excel selalu sediakan Opsi A–F; yang kosong diabaikan. UI authoring: tampil hingga 6 baris opsi (tambah/hapus), min 2.

---

## 9. Import Excel (Diperluas)

### 9.1 Format baru (urut kolom)
`(1) Pertanyaan | (2–7) Opsi A–F | (8) Jawaban Benar | (9) No. Section | (10) Nama Section | (11) Elemen Teknis | (12) QuestionType | (13) Rubrik`

- `Jawaban Benar`: huruf A–F (multi untuk Multiple, mis. `A,C,E`).
- `No. Section` / `Nama Section`: boleh kosong (= tanpa section).

### 9.2 Dual-format (kompatibel-mundur)
- Parser deteksi jumlah kolom: **< 11 = format lama 9-kolom** (Opsi A–D, tanpa section) → tetap jalan; `SectionId=null`, Opsi E/F kosong.
- ≥ 11 = format baru.

### 9.3 Validasi
- Per baris: QuestionText wajib; MC/MA ≥2 opsi terisi; Jawaban ∈ {A..F}; Essay butuh Rubrik.
- **Validasi jumlah per-section (D-13):** untuk tiap `SectionNumber` di antara paket-saudara, jumlah soal harus **sama**. Mismatch → import GAGAL dengan pesan jelas (tolak keras). `SectionNumber=null` = 1 section implisit.
- **Fingerprint dedup** menjadi `hash(Q, OptA..F, SectionNumber)` (sertakan opsi 5–6 walau kosong + section).

### 9.4 Template generator
- `DownloadQuestionTemplate` hasilkan template baru 13-kolom + contoh per tipe (MC/MA/Essay) + contoh section. (Dropdown Data-Validation = milestone quick-win terpisah, tidak di sini.)

---

## 10. Integrasi Tersentuh & Yang Aman

### 10.1 Tersentuh (harus disesuaikan)
- **Inject (v32.2):** tambah `SectionNumber`/`SectionName` di `InjectQuestionSpec`; saat commit buat record section; validasi jumlah per-section. Form inject ikut opsi dinamis (§8.2).
- **Sync Pre→Post (`SyncPackagesToPost` / `CopyPackagesFromPre`):** deep-clone (sudah salin teks/gambar/opsi) harus ikut menyalin **record Section + `SectionId` + opsi 5–6**. ⚠️ Audit ulang SEMUA pemicu sync (jalur CreateQuestion/EditQuestion/DeleteQuestion/CreatePackage pada PreTest ber-`SamePackage=true`) supaya tiap jalur menyalin section — **jumlah pemicu pasti diverifikasi saat implement, jangan diasumsikan**.
- **Export per-soal (Excel/PDF):** sudah per-peserta. Tambah **label/header Section** ("Section {n}: {Nama}") + nomor soal relatif/section agar konsisten lintas-peserta (EXPORT-GA-002/003). Huruf A–F.
- **Monitoring:** label section di tampilan; logika answered/total tetap (tak per-section).

### 10.2 Aman (diverifikasi, tak berubah)
- Rumus skor & Lulus/Tidak (persen) — immutable, section-agnostic.
- Grading berbasis `PackageOption.Id` (opsi dinamis & acak opsi aman).
- Breakdown per-Elemen-Teknis (`SessionElemenTeknisScore`) independen dari section.
- Timer global page-agnostic.
- Guard sertifikat (passed + PendingGrading bypass).
- Race-condition grading (status guard) robust.

---

## 11. Migrasi, Backward-Compat & Data Live
- ~600 soal + assessment live: `SectionId=null` → tak ada perubahan urutan/perilaku.
- Assessment baru pasca-migrasi bisa pakai section eksplisit.
- File Excel lama tetap bisa di-import (dual-format).
- Rollback aman (drop tabel/kolom).

---

## 12. Strategi Testing
- ~180 metode tes shuffle + ~17.5K baris xUnit saat ini mengasumsikan list datar & 4 opsi.
- **Jangan retrofit tes lama di tengah jalan.** Tulis suite BARU setelah fondasi Section masuk:
  - Unit: `BuildSectionQuestionAssignment` (isolasi section, ET-within-section, K-min per section, OFF order).
  - Validasi: mismatch per-section (tolak keras), dual-format import, fingerprint 6-opsi+section.
  - Render/Playwright: pagination (default, StartNewPage 1 section, semua section), opsi 5–6 tampil & ter-grade benar, resume.
- Tes lama harus tetap hijau (kompatibel-mundur = section kosong).

---

## 13. Rencana Fase (untuk roadmap)
Urut, tak bisa paralel kecuali #4:

1. **Fondasi Section** — tabel + migrasi + model + import kolom (No.Section/Nama) + validasi per-section + dual-format. (migration=TRUE)
2. **Scoped Shuffle** — refactor ShuffleEngine per-section + precedence toggle + UI toggle per-section.
3. **Section Pagination** — render header + StartNewPage + tombol cepat + resume mapping.
4. **Opsi Dinamis 2–6** — kontrak HTTP + form authoring + inject form + render + preview + results + import A–F + validator. (workstream agak terpisah; bisa mulai paralel pasca-#1)
5. **Export & Preview & QA** — label section di export, polish, Playwright UAT, audit milestone.

> Saran teknis: pertimbangkan lapisan abstraksi urutan-soal (`IQuestionSequence` / `SectionAwareQuestionProvider`) di awal #1 untuk memangkas risiko penyebaran perubahan ~50%.

---

## 14. Risiko Utama
| Risiko | Mitigasi |
|--------|----------|
| ShuffleEngine ET-aware jadi `(Section, ET)` — kompleks, rawan bug senyap (soal bocor antar-section) | Suite tes baru fokus invariant isolasi section; fungsi murni teruji terpisah |
| Kontrak HTTP CreateQuestion/EditQuestion berubah (param→array) | Audit semua pemanggil; uji form & inject; pertahankan EditQuestion JSON response |
| Resume `LastActivePage` saat config section berubah | Tetap integer global + hitung ulang dari config; fallback aman ke page 0 |
| Permukaan luas (23+ titik) | Pecah 5 fase berurutan; abstraksi urutan-soal di awal |
| Mismatch struktur section antar-paket | Tolak keras di import + saat assign/mulai ujian (D-13) |

---

## 15. Penajaman Re-Check (2026-06-22) — Koreksi & Edge-Case

Hasil sweep verifikasi spec (5 agen) terhadap kode live. Koreksi faktual + aturan edge-case + miss yang ditambahkan.

### 15.A Aturan rinci shuffle, "Lainnya", pagination
- **Definisi K (§6.4):** `K` = jumlah soal section pada SATU paket (mis. Section 2 di Paket A). Oleh D-13, `K` dijamin **identik** di semua paket-saudara, dan `K > 0` (count=0 ditolak D-13).
- **Skenario semua null:** jika SEMUA soal `SectionId=null` → 1 section implisit "Lainnya" = **perilaku global lama** (1 paket: tampil semua; >1 paket: pooling global ET-aware seperti sekarang). Tak ada perubahan.
- **Grup "Lainnya" (D-15):** soal `SectionId=null`, **tanpa row** di `AssessmentPackageSection` → **tidak punya toggle `StartNewPage`/`ShuffleEnabled` sendiri**; ikut toggle level-assessment (induk). Selalu di **urutan terakhir**. Tidak memaksa page-break (kecuali halaman penuh). ET-aware pakai kunci komposit `(null, ET)`.
- **OFF-mode campuran:** induk `ShuffleQuestions=OFF` → semua soal urut **`SectionNumber` asc lalu `Question.Order`**, grup "Lainnya" (null) terakhir.
- **ET lintas-section:** K-min sampling **independen tiap section**. ET yang ada di >1 section dijamin tercakup di tiap section terpisah.
- **DisplayNumber GLOBAL** (1..N), **tidak reset** per section. Header section memberi konteks. Mode 1-soal-per-layar **TIDAK** dalam scope.
- **Resume saat config berubah pasca-lock:** nomor halaman dihitung ulang dari config; jika HC ubah struktur/toggle section setelah `UserPackageAssignment` terkunci → halaman bisa bergeser tapi **identitas soal stabil** (by question id); fallback aman ke halaman 0 bila `LastActivePage` di luar rentang.

### 15.B Timing validasi mismatch (D-13)
Diberlakukan di **2 titik**: (1) saat **import/simpan** paket-saudara → hard-block bila jumlah per-`SectionNumber` beda; (2) **re-check sebelum mulai ujian** (guard) → blok start bila terdeteksi drift. Pesan error jelas (sebut SectionNumber + jumlah yang diharapkan vs aktual).

### 15.C Fingerprint dedup (penajaman §9.3)
`MakePackageFingerprint` sekarang 5-param `(Q,A,B,C,D)`. Diubah jadi sertakan **OptE, OptF, dan SectionNumber** (null untuk "Lainnya"). Soal teks sama beda section / beda jumlah opsi → dianggap **berbeda** (tidak ter-dedup keliru).

### 15.D MISS terbesar — UI Admin Kelola Section (BLOCKER, sebelumnya tak ada di spec)
Spec hanya menjelaskan model + import; **belum** ada surface web tempat HC menata section. Tambahkan:
- **Kelola Section** (di `ManagePackages` atau view baru `ManagePackageSections`): buat/edit/hapus/**urutkan** Section (No.Section, Nama); toggle **`StartNewPage`** + **`ShuffleEnabled`** per section; tombol **"Semua section mulai halaman baru"**.
- **`ManagePackageQuestions`**: tampilkan soal **dikelompokkan per Section** (header "Section {n}: {Nama}") + **assign/pindah section per soal** (dropdown) atau bulk.
- **`PreviewPackage`**: tampil berkelompok per section + tanda page-break + metadata; **perbaiki array huruf yang cap di "E" → A–F**.
- Section bisa lahir dari **Excel** (otomatis dari kolom No.Section) **atau** dibuat di UI; **toggle hanya diatur di UI** (Excel tak bawa toggle).
- Penempatan fase: surface ini sebagian masuk **Fase 1** (CRUD section dasar) + **Fase 3** (toggle pagination) + **Fase 4** (opsi dinamis di form).

### 15.E Penajaman exam-render & endpoint lain
- **Reshuffle endpoints** (`ReshufflePackage` / `ReshuffleAll`) → **section-aware** (lewat ShuffleEngine yang sudah di-refactor).
- **Mobile (5 soal/halaman)** ikut aturan section yang sama (header + StartNewPage).
- **Autosave SignalR** (essay/MA, `assessment-hub.js`): saat pindah halaman antar-section, pending autosave **WAJIB di-flush** (pertahankan perilaku flush yang sudah ada lintas-halaman).
- **Sidebar panel nav**: kelompokkan badge per section + header (saat ini flat).
- **Auto-submit/timeout**: `UpdateSessionProgress` simpan `currentPage` terakhir sebelum POST; sesi `Abandoned` rekam `ElapsedSeconds` + `LastActivePage` untuk audit.

### 15.F Interaksi LINTAS-MILESTONE (sebelumnya tak dibahas)
- **Inject (v32.2):** `InjectQuestionSpec` + Excel inject baca `No.Section`/`Nama Section` + opsi A–F; validasi D-13 saat commit.
- **LinkPrePost (Phase 397) & Sync:** struktur Section **harus identik Pre↔Post** (`SectionNumber`+`Nama`+jumlah). Blok link bila beda; Post legacy tanpa section boleh (diperlakukan sebagai Pre tanpa section).
- **AddParticipantsLive (v32.5 Phase 410):** eager-assignment untuk peserta baru **WAJIB** pakai per-section `BuildQuestionAssignment` yang sama (seed `workerIndex` konsisten).
- **RemoveParticipant Pre/Post (v32.5 Phase 411):** pasangan tetap simetris; section identik Pre/Post (dari poin LinkPrePost).
- **Retake/Attempt (v32.4, branch ITHandoff):** `AssessmentAttemptHistory`/`RetakeArchiveBuilder` — snapshot attempt sebaiknya simpan **info section** bila granularitas per-section diperlukan (per-ET sudah ada). **Rekonsiliasi saat merge** v32.4 ↔ milestone ini.
- **MultiUnit (v32.3 `UserUnits`, branch ITHandoff):** orthogonal; seed `workerIndex` by `UserId` (bukan unit) → tak ada perubahan.
- ⚠️ **v32.3/v32.4 hidup di branch ITHandoff** — sebagian asumsi tergantung branch; rekonsiliasi saat merge ke main.

### 15.G Koreksi faktual ke kode (tercatat)
| Sebelumnya di spec | Koreksi |
|---|---|
| `LastActivePage` "integer" | `int?` (nullable) — §7.2 sudah diperbaiki |
| `ValidateQuestionOptions(string?[], bool[])` | `(string type, string?[] texts, bool[] corrects)` — §8.1 diperbaiki |
| Sync "4 trigger" pasti | Pemicu **diverifikasi saat implement** — §10.1 diperbaiki |
| `PreviewPackage` huruf | Saat ini cap di "E" → jadikan **A–F** |
| `MakePackageFingerprint(Q,A,B,C,D)` | Tambah OptE/OptF + SectionNumber (§15.C) |

---

## 16. Out-of-Band: Fitur #1 (Hapus Peserta)
Sudah selesai di v32.5 (Phase 411), teruji, **belum deploy**. Akan didemokan lokal (jalankan app + Playwright) sebagai langkah terpisah; bukan bagian milestone ini. Koordinasi deploy ke IT (migration `AddParticipantRemovalColumns` = TRUE).
