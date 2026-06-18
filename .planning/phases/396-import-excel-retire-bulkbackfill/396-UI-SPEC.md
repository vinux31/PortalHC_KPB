---
phase: 396
slug: import-excel-retire-bulkbackfill
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-18
---

# Phase 396 — UI Design Contract

> Kontrak visual & interaksi untuk **jalur input kedua (Import Excel)** di Langkah 5 wizard `/Admin/InjectAssessment`, + **penghapusan UI** tool lama BulkBackfill. Dibuat oleh gsd-ui-researcher, diverifikasi gsd-ui-checker.
>
> **PENTING — fase ini MEMPERLUAS halaman yang sudah ada, bukan membuat baru.** `/Admin/InjectAssessment` (`Views/Admin/InjectAssessment.cshtml`) sudah dibangun di Phase 394 (wizard 6-langkah nav-pills) dan diperluas Phase 395 (Langkah 5 sub-komponen jawaban per-pekerja, `#step5Root`). Sistem desain (warna, spasi, tipografi, pola kartu, roster, permukaan Pratinjau) **SUDAH MAPAN & DISETUJUI** ([394-UI-SPEC.md](../394-page-setup-room-authoring-soal/394-UI-SPEC.md), [395-UI-SPEC.md](../395-mode-jawaban-input-asli-auto-generate/395-UI-SPEC.md) — checker 6/6 PASS). Spec ini **MEWARISI** token tersebut dan **HANYA** mendeklarasikan elemen NET-BARU: toggle metode jawaban (Form/Excel), tombol Download Template, kontrol file-upload, tombol Upload & Pratinjau, dan **panel daftar error validasi**. Tidak ada bahasa visual baru.
>
> Semua copy = **Bahasa Indonesia** (alat internal HC, CLAUDE.md mewajibkan Bahasa Indonesia). Token teknis (hex/px/weight) tetap apa adanya.

---

## Inheritance Statement (apa yang DIWARISI — JANGAN ciptakan ulang)

| Kontrak | Sumber | Status di 396 |
|---------|--------|---------------|
| Design system (Bootstrap 5.3 + Bootstrap Icons 1.10.0 + Inter, no shadcn/React) | 394 §Design System / 395 §Design System | **INHERIT verbatim** |
| Spacing scale (Bootstrap spacer kelipatan 4: 4/8/16/24/32) | 394 §Spacing / 395 §Spacing | **INHERIT verbatim** |
| Typography (3 ukuran + **kontrak 2-berat**: 400 reguler + 700 bold) | 395 §Typography (2-berat) | **INHERIT verbatim** |
| Color (60 putih / 30 `bg-light` / 10 primary `#0d6efd`; success/warning/danger/info semantik) | 394 §Color / 395 §Color | **INHERIT verbatim** |
| Pola wizard: `.container-fluid px-4 py-4` → `.card.shadow-sm.border-0` → `.card-body.p-4` → `h5.fw-bold` heading | 394 §Component Inventory | **INHERIT** (tak disentuh) |
| Pills + `goToStep`/`updatePills` (navigasi antar-langkah) | 394 wizard scaffold | **INHERIT — JANGAN refactor** (D-01) |
| Roster pekerja (`#step5RosterBody`), indikator pekerja aktif, kontrol Prev/Next pekerja | 395 K1 | **INHERIT** (dipakai ulang oleh path Form; tersembunyi saat path Excel — lihat IC-1) |
| Permukaan Pratinjau (engine `AssessmentScoreAggregator.Compute`, `preview == commit`, **tanpa nomor sertifikat**) | 395 K5 / D-09 | **INHERIT engine** — direpresentasikan ulang sebagai **tabel batch** di path Excel (lihat C-3) |
| Tombol commit `#btnInject` (`.btn.btn-success`, "Inject Assessment") | 394/395 | **INHERIT — jangan ubah varian/label**; path Excel mengisi `#AnswersJson` lalu submit form yang sama |
| Render data user via `.textContent` (XSS-safe), bukan `innerHTML` | 395 §Design System | **INHERIT — wajib** untuk NIP/Nama/pesan error |
| Verifikasi runtime Playwright dari **main working tree** (no Razor RuntimeCompilation; lesson 354/392) | 394/395 | **INHERIT — wajib** |

> **Aturan 2-berat (carry 395):** hanya **400 (reguler)** + **700 (bold/`fw-bold`)** sebagai kontrak. `fw-semibold` (600) yang muncul di chrome wizard 394 (mis. `card-header` "Ringkasan Pekerja") adalah brownfield carry-over — pertahankan untuk kesetiaan mirror, jangan tambah berat baru. Markup NET-BARU 396 memakai 400/700 saja.

---

## Scope Visual Fase Ini

Yang **DIBANGUN** di fase ini (di dalam `#step5Root` `InjectAssessment.cshtml:406`, + 2 penghapusan UI):

1. **Toggle metode jawaban room-level (D-01/D-03)** — radio `Isi via Form` / `Import Excel` di puncak Step-5 body, di atas kontrol mode-default-room 395. **Mutually exclusive**: memilih "Import Excel" menyembunyikan SELURUH sub-komponen Form 395 (roster + form per-pekerja + mode-default-room) dan menampilkan panel Excel; sebaliknya untuk "Isi via Form". Satu metode = satu room.
2. **Panel Import Excel** — kartu berisi: (a) tombol **Download Template** (`.xlsx` ter-generate dari soal authored + NIP pekerja terpilih), (b) kontrol **file-upload** (`<input type="file" accept=".xlsx,.xls">`), (c) tombol **Upload & Pratinjau**.
3. **Tabel Pratinjau batch dry-run (D-08)** — pasca-upload sukses, sebelum commit: tabel NIP + Nama + Skor final % + Lulus/Tidak + jumlah soal terjawab (engine `Compute`, `preview == commit`, **tanpa nomor sertifikat**).
4. **Panel daftar error validasi (D-09)** — bila upload invalid: alert `danger` + **daftar LENGKAP** per-baris/sel (atomic, tak ada yang ter-commit bila ≥1 error). Dibedakan jelas dari notice sukses (warna + ikon + heading).
5. **Retire BulkBackfill (INJ-11, no new visual — REMOVAL ONLY)** — hapus kartu Section D (`Views/Admin/Index.cshtml:306-321`, seluruh blok `@if Admin`) + dropdown-item & divider (`Views/Admin/Shared/_AssessmentGroupsTab.cshtml:317-322`). **Tidak ada UI baru di sini** — hanya penghapusan dua entry-point + view file.

Di luar scope visual: link Pre/Post (397); kolom skor-target di Excel (ditolak D-07, auto-gen tetap path Form 395); preview nomor sertifikat (ditolak D-09); import gambar via Excel (out-of-scope spec §12); campur Form+Excel dalam 1 room (ditolak D-03).

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (ASP.NET Core MVC + Razor `.cshtml`, **bukan** shadcn/React — shadcn gate N/A) |
| Preset | not applicable |
| Component library | Bootstrap 5.3.0 (CDN, `_Layout.cshtml:38`) — `card`/`nav`/`alert`/`badge`/`form-check`/`input-group`/`table`/`btn` + `wwwroot/css/site.css` |
| Icon library | Bootstrap Icons 1.10.0 (`<i class="bi bi-*">`, `_Layout.cshtml:39`) |
| Font | Inter (Google Fonts 300;400;500;600;700;800, `_Layout.cshtml:41`); fallback system stack |
| Excel lib (non-UI, untuk konteks template) | ClosedXML 0.105.0 (`HcPortal.csproj`) — template `.xlsx` adalah **artefak file**, BUKAN layar UI |

**Stack TETAP — jangan perkenalkan component library / CSS framework / SPA framework.** Halaman server-rendered Razor + vanilla JS. Tiru konvensi markup `#step5Root` (394/395) verbatim.

**Verifikasi wajib (carry 354/392):** app pakai `AddControllersWithViews()` TANPA `AddRazorRuntimeCompilation` → view embedded saat build. Perilaku runtime toggle (Form↔Excel show/hide), file-upload, fetch upload→preview, render tabel batch, render daftar error — **WAJIB diverifikasi Playwright runtime** (grep+`dotnet build` tak cukup). Jalankan app dari **main working tree** (bukan sibling `PortalHC_KPB-ITHandoff`), AD-off (`Authentication__UseActiveDirectory=false`), Playwright `--workers=1`.

---

## Spacing Scale

**INHERIT dari 394/395** — Bootstrap spacer (`1rem = 16px`), kelas `*-1..*-5` kelipatan 4. Markup NET-BARU 396 memakai HANYA token berikut:

| Token | Value | Bootstrap util | Usage NET-BARU di fase ini |
|-------|-------|----------------|----------------------------|
| xs | 4px | `*-1`, `gap-1`, `me-1` | Jarak ikon-teks pada tombol Download/Upload, item daftar error |
| sm | 8px | `*-2`, `gap-2`, `mb-2` | Jarak antar baris daftar error (`<li>`), gap kontrol upload, badge |
| md | 16px | `*-3`/`g-3`, `mb-3` | Jarak antar blok dalam panel Excel (download ↔ upload ↔ preview), padding default |
| lg | 24px | `*-4`, `mb-4`, `card-body p-4` | Padding kartu panel Excel, jarak heading→konten |

Exceptions:
- Tabel Pratinjau batch panjang: `max-height:320px; overflow-y:auto` (pola roster `:443` `max-height:280px` & picker `:246` `max-height:320px` — bukan token baru, reuse verbatim).
- Daftar error panjang: bungkus `<ul>` dalam `max-height:240px; overflow-y:auto` bila >8 item (reuse pola overflow di atas; tak ada token spasi baru).

---

## Typography

**INHERIT kontrak 2-berat dari 395 verbatim.** 3 ukuran + **2 berat**: **400 (reguler)** untuk semua teks bertubuh, **700 (`fw-bold`)** untuk semua penekanan. Tidak ada ukuran/berat baru.

| Role | Size | Weight | Line Height | Usage NET-BARU |
|------|------|--------|-------------|----------------|
| Heading langkah (`h5.fw-bold`) | 20px (h5) | 700 | 1.2 | (diwarisi — "Langkah 5: Jawaban", tak disentuh) |
| Sub-heading kartu (`h6.fw-bold`) | 16px (h6) | 700 | 1.2 | Judul kartu panel Excel ("Import Jawaban via Excel"), heading panel error ("Perbaiki kesalahan berikut") |
| Body / teks tabel / label opsi | 16px (1rem) | 400 | 1.5 | Sel tabel Pratinjau (NIP/Nama/Skor), teks item daftar error |
| Label field wajib (`.form-label.fw-bold`) | 16px | 700 | 1.5 | Label "File Excel", label radio metode |
| Small / bantuan / hint | 14px (0.875rem) | 400 | 1.4 | `.form-text.text-muted` hint upload (max 10 MB / format), `.small` instruksi template |

Aturan (carry 395): semua penekanan → 700; sisanya → 400. Jangan memperkenalkan ukuran/berat di luar tabel ini.

---

## Color

**INHERIT tema Bootstrap 5.3 (`--bs-*`) dari 394/395.** Tidak ada palet kustom baru. Pembagian 60/30/10 sama persis dengan wizard.

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `#ffffff` / `--bs-body-bg` | Latar halaman & `card-body` panel Excel |
| Secondary (30%) | `#f8f9fa` (`--bs-light` / `.bg-light`) + border `#dee2e6` | `card-header` panel Excel & panel Pratinjau, latar tabel head (`table-light`), `shadow-sm` |
| Accent (10%) | `#0d6efd` (`--bs-primary`) | Lihat daftar reserved di bawah |
| Success (semantik) | `#198754` (`--bs-success`) | Badge "Lulus" di tabel Pratinjau, notice sukses upload, tombol commit `#btnInject` (diwarisi — jangan ubah varian) |
| Warning (semantik) | `#ffc107` (`--bs-warning`) / `.alert-warning` | Notice "N sel kosong → dihitung 0" (warn-but-allow D-06), advisory backdate/dedup |
| Danger (semantik) | `#dc3545` (`--bs-danger`) / `.alert-danger` | **Panel daftar error validasi (D-09)**, badge "Tidak Lulus" di tabel Pratinjau, ikon wajib `*` |
| Info (semantik) | `#0dcaf0` (`--bs-info`) / `.alert-info` | Instruksi panel Excel (cara isi template, urutan: download → isi → upload) |

**Accent (`#0d6efd` primary) reserved for (carry 394/395, + NET-BARU):**
1. (diwarisi) Tombol nav maju `.btn-primary.btn-next`, pill aktif, ikon heading langkah, badge hitung.
2. **Tombol "Download Template"** = `.btn.btn-outline-primary` (aksi sekunder on-demand, sejajar "Pratinjau Skor" 395 yang juga outline-primary).
3. **Tombol "Upload & Pratinjau"** = `.btn.btn-outline-primary` (aksi sekunder; bukan submit utama — submit/commit tetap `#btnInject` success-green).
4. **Radio metode aktif** (Form/Excel) — indikator pilihan via `form-check` standar (tak butuh warna kustom; mengikuti default Bootstrap radio biru).

> **Pemisahan tegas (carry 395):** accent biru BUKAN untuk semua elemen interaktif. Commit final = **success green** (`#btnInject`, jangan diubah). Daftar error = **danger**. Aksi download/upload/preview = **outline-primary** (sekunder, on-demand). Notice sukses upload = **success**; instruksi = **info**; warn sel-kosong = **warning**.

---

## Komponen NET-BARU & State (kontrak interaksi)

**Focal point Step-5 (path Excel):** anchor visual utama = **panel Import Excel** (kartu tunggal berisi download → upload → preview), karena path Excel = 1 layar batch (bukan 1-pekerja-per-layar). Saat tabel Pratinjau tampil, **tabel batch + ringkasan skor** menjadi anchor sekunder. Hierarki: panel upload > tabel pratinjau > instruksi. (Path Form mempertahankan focal point 395: form pekerja aktif.)

### N1 — Toggle metode jawaban room-level (D-01/D-03)
- **Penempatan**: di dalam `#step5Body` (`:418`), **di atas** blok "Mode default room" 395 (`:421`). Bukan langkah/tab baru — tetap di Step-5.
- **Kontrol**: dua radio inline `.form-check.form-check-inline`, `name="step5Method"`:
  - `( ) Isi via Form` (`value="form"`, **default checked** — mempertahankan perilaku 395 sebagai default).
  - `( ) Import Excel` (`value="excel"`).
- **Label kelompok**: `.form-label.fw-bold.mb-1` "Metode pengisian jawaban".
- **Hint**: `.form-text.text-muted` "Pilih satu metode untuk seluruh room — semua via form atau semua via Excel (tak bisa campur)." (D-03).
- **Perilaku show/hide (mutually exclusive, IC-1)**:
  - `form` dipilih → tampilkan blok Form 395 (`#step5DefaultMode` group + roster `.row.g-4` + form pekerja), sembunyikan panel Excel (N2).
  - `excel` dipilih → sembunyikan SELURUH blok Form 395, tampilkan panel Excel (N2). Roster/form per-pekerja `d-none`.
- **Render via toggle CSS class `d-none`** (pola show/hide existing `:418`/`:488`). Transisi tidak menghapus state form 395 (bila HC kembali ke `form`, jawaban yang sudah diisi tetap ada).

### N2 — Panel Import Excel (kartu)
- **Container**: `.card` → `.card-header.bg-light` (`<h6 class="mb-0 fw-bold"><i class="bi bi-file-earmark-excel text-primary me-2"></i>Import Jawaban via Excel</h6>`) → `.card-body.p-4`. Awalnya `d-none` (tampil saat N1=excel). ID `#step5ExcelPanel`.
- **N2a — Instruksi** (`.alert.alert-info.small`, ikon `bi-info-circle`): 3 langkah ber-nomor — "1. Unduh template, 2. Isi jawaban (huruf opsi: `A` untuk Single, `A,C` untuk Multiple; skor untuk Essay), 3. Unggah & pratinjau sebelum commit." (Detail format = legenda di Sheet-2 template, bukan di layar.)
- **N2b — Download Template**: tombol `.btn.btn-outline-primary` (ikon `bi-download me-1`) "Download Template" → GET `DownloadInjectTemplate` (membawa `#QuestionsJson` + UserIds terpilih). Hint di bawah `.form-text.text-muted`: "Template berisi 1 baris per pekerja terpilih + kolom per soal."
- **N2c — File-upload**: `.mb-3` → `.form-label.fw-bold` "File Excel" + `<input type="file" class="form-control" id="step5ExcelFile" accept=".xlsx,.xls">` + `.form-text.text-muted` "Format .xlsx/.xls, maks 10 MB." (mirror BulkBackfill input — fungsional, BUKAN styling lama).
- **N2d — Upload & Pratinjau**: tombol `.btn.btn-outline-primary` (ikon `bi-upload me-1`) "Upload & Pratinjau" → POST `UploadInjectExcel` (multipart). Disabled saat tidak ada file dipilih ATAU saat fetch berjalan.
- **N2e — Loading state**: saat fetch upload, tombol N2d disabled + `<span class="spinner-border spinner-border-sm me-1">` + teks "Memproses…" (pola `:919`/395 K5 loading).

### N3 — Tabel Pratinjau batch (D-08) — `#step5ExcelPreview`
Tampil HANYA setelah upload sukses & **0 error**. Blok di bawah N2 (awalnya `display:none`):
- **Heading**: `.h6.fw-bold` "Pratinjau Hasil (sebelum commit)".
- **Tabel**: `<table class="table table-sm table-hover align-middle">` dalam `max-height:320px;overflow-y:auto`. `<thead class="table-light">`. Kolom:
  | # | NIP / Nama | Skor Final | Lulus/Tidak | Soal Terjawab |
  - **Skor Final**: persen `fw-bold` (mis. "80%").
  - **Lulus/Tidak**: badge `bg-success` "Lulus" / `bg-danger` "Tidak Lulus" (konsisten 395 K5).
  - **Soal Terjawab**: "{m} / {total}" (sel kosong = di-skip → tak terhitung; D-06).
- **Ringkasan warn-but-allow (D-06)**: bila ada sel kosong yang di-skip → `.alert.alert-warning.py-2.small` di atas/bawah tabel: "{n} jawaban kosong di Excel — soal terkait dihitung 0. Periksa pratinjau sebelum commit." Netral, **tidak memblokir**.
- **TANPA nomor sertifikat** (D-09 — nomor tak ter-reserve pra-commit).
- **`role="status" aria-live="polite"`** pada container tabel (skor diperbarui pasca-upload).
- **Render data via `.textContent`** (NIP/Nama XSS-safe).
- **Setelah preview tampil**: form siap commit — HC klik `#btnInject` (diwarisi, success-green) → klien telah mengisi `#AnswersJson` dengan hasil parse → submit form yang sama → `MapToRequest`→`InjectBatchAsync` (byte-identik). Lihat IC-3.

### N4 — Panel daftar error validasi (D-09) — `#step5ExcelErrors`
Tampil HANYA bila upload menghasilkan ≥1 error (atomic — tabel Pratinjau N3 disembunyikan; tak ada yang ter-commit):
- **Container**: `.alert.alert-danger` (ikon `bi-exclamation-triangle-fill me-2`), awalnya `d-none`. **Wajib visual berbeda dari sukses**: warna danger (merah) vs notice sukses (hijau/info), ikon segitiga vs centang, heading eksplisit.
- **Heading**: `<strong>Perbaiki kesalahan berikut, lalu unggah ulang:</strong>` (`fw-bold`).
- **Daftar LENGKAP**: `<ul class="mb-0 mt-2 small">` berisi **SEMUA** error sekaligus (bukan stop-di-error-pertama) — 1 `<li>` per masalah per-baris/sel. Bungkus `max-height:240px;overflow-y:auto` bila >8 item.
- **Contoh isi item** (format pesan; teks aktual server-side):
  - "Baris 3: NIP 12345 tidak ada di daftar pekerja terpilih." (D-02)
  - "Baris 5, kolom Soal 2: opsi 'E' tidak valid (hanya A–D)." (huruf di luar opsi authored)
  - "Baris 7, kolom Soal 4 (Essay): skor 15 melebihi maksimum 10."
- **Render item via `.textContent`** (data baris/NIP user → XSS-safe).
- **Pasca-perbaikan**: HC pilih file baru → "Upload & Pratinjau" lagi; panel error tergantikan oleh hasil baru (error baru ATAU tabel Pratinjau N3).
- **`#btnInject` saat ada error**: commit tetap tidak boleh memproses path Excel sampai upload menghasilkan 0 error + pratinjau (integritas; atomic D-09). Path Excel tanpa `#AnswersJson` valid = tak ada yang di-submit.

---

## Interaction Contracts (runtime — Playwright-verified)

1. **Toggle metode (mutually exclusive, D-01/D-03):** radio `Isi via Form`↔`Import Excel`; memilih Excel menyembunyikan SELURUH sub-komponen Form 395 (roster + form per-pekerja + mode-default-room) dan menampilkan `#step5ExcelPanel`; sebaliknya untuk Form. Satu metode per room. *(INJ-10)*
2. **Download Template:** klik "Download Template" → GET men-generate `.xlsx` (2-sheet: matrix + legenda) dari soal authored + NIP pekerja terpilih → unduhan file. Verifikasi unduhan terpicu (event download Playwright). *(INJ-10, SC1)*
3. **Upload → Pratinjau (path sukses):** pilih file valid → "Upload & Pratinjau" → POST → tabel Pratinjau N3 tampil (NIP+Nama+Skor+Lulus/Tidak+terjawab); `#AnswersJson` terisi hasil parse; klik `#btnInject` → commit byte-identik → hasil di `/CMP/Records`+`/CMP/Results`. *(INJ-10, SC1/SC3, D-08)*
4. **Upload → daftar error (path invalid, atomic D-09):** file dengan ≥1 masalah → POST → panel error N4 (`.alert-danger`) menampilkan **daftar LENGKAP** semua masalah; tabel Pratinjau N3 TIDAK tampil; **0 sesi ter-commit** (rollback total). Daftar berisi semua error sekaligus, bukan error pertama saja. *(INJ-10, SC2, D-09)*
5. **Sel kosong = skip warn-but-allow (D-06):** Excel dengan sel kosong → tidak ditolak; soal terkait dihitung 0; notice `alert-warning` "N jawaban kosong → dihitung 0"; pratinjau menampilkan dampak skor; HC konfirmasi via commit. *(INJ-10, D-06)*
6. **NIP di luar picker ditolak (D-02):** NIP di baris Excel yang tidak ada di pekerja terpilih Step-2 → masuk daftar error N4 (bukan auto-add). *(INJ-10, D-02)*
7. **Preview == commit (D-08):** skor di tabel Pratinjau (engine `AssessmentScoreAggregator.Compute`) identik dengan skor pasca-commit (engine yang sama). Tanpa nomor sertifikat di pratinjau. *(INJ-10, D-08)*
8. **Retire BulkBackfill (INJ-11, removal-only):** route `/Admin/BulkBackfill` → 404; kartu Section D (Index) hilang; dropdown-item (`_AssessmentGroupsTab`) + divider hilang (tak ada divider yatim / link mati). Tidak ada UI baru. *(INJ-11, SC4)*

---

## Copywriting Contract

Semua **Bahasa Indonesia**. Hanya string NET-BARU; sisanya diwarisi 394/395.

| Element | Copy |
|---------|------|
| Primary CTA (commit) | "Inject Assessment" (tombol `#btnInject` yang ada — **jangan ubah**) |
| Label kelompok toggle metode | "Metode pengisian jawaban" |
| Opsi toggle | "Isi via Form" / "Import Excel" |
| Hint toggle metode (D-03) | "Pilih satu metode untuk seluruh room — semua via form atau semua via Excel (tak bisa campur)." |
| Judul panel Excel | "Import Jawaban via Excel" |
| Instruksi panel (D-04, N2a) | "1. Unduh template. 2. Isi jawaban — huruf opsi (A untuk Single, A,C untuk Multiple) atau skor untuk Essay. 3. Unggah & pratinjau sebelum commit." |
| CTA download | "Download Template" (ikon `bi-download`) |
| Hint download | "Template berisi 1 baris per pekerja terpilih + kolom per soal." |
| Label file-upload | "File Excel" |
| Hint file-upload | "Format .xlsx/.xls, maks 10 MB." |
| CTA upload | "Upload & Pratinjau" (ikon `bi-upload`) |
| Loading upload | "Memproses…" (spinner) |
| Heading tabel Pratinjau (D-08) | "Pratinjau Hasil (sebelum commit)" |
| Kolom tabel | "#" · "NIP / Nama" · "Skor Final" · "Lulus/Tidak" · "Soal Terjawab" |
| Badge Lulus / Tidak | "Lulus" (`bg-success`) / "Tidak Lulus" (`bg-danger`) |
| **Empty state** — tak ada pekerja terpilih (path Excel) | "Pilih pekerja di Langkah 2 sebelum mengunduh template atau mengunggah Excel." |
| **Empty state** — tak ada soal | "Belum ada soal. Tambahkan soal di Langkah 3 sebelum membuat template Excel." |
| **Empty state** — belum upload (default panel) | "Belum ada file diunggah. Unduh template, isi, lalu unggah untuk melihat pratinjau." |
| **Error state — daftar validasi (D-09)** | Heading: "Perbaiki kesalahan berikut, lalu unggah ulang:" + daftar `<ul>` per-baris/sel. Contoh item: "Baris 3: NIP 12345 tidak ada di daftar pekerja terpilih." / "Baris 5, kolom Soal 2: opsi 'E' tidak valid (hanya A–D)." / "Baris 7, kolom Soal 4 (Essay): skor 15 melebihi maksimum 10." |
| **Error state — upload gagal (file/parse)** | "Gagal membaca file Excel. Pastikan file .xlsx/.xls valid dan ≤ 10 MB, lalu coba lagi." |
| Notice warn-but-allow sel kosong (D-06) | "{n} jawaban kosong di Excel — soal terkait dihitung 0. Periksa pratinjau sebelum commit." (`alert-warning`) |
| Notice sukses upload (sebelum tabel) | "File diunggah. Periksa pratinjau di bawah, lalu klik 'Inject Assessment' untuk commit." (`alert-success` opsional / boleh implicit via tampilnya tabel) |
| **Destructive — retire BulkBackfill (INJ-11)** | **Removal only — tidak ada copy/konfirmasi UI baru.** Kartu Section D + dropdown-item dihapus (hard-remove D-10/D-11). Route lama → 404 default ASP.NET (bukan halaman kustom). Tidak ada dialog/banner deprecation. |
| Destructive confirmation (commit Excel) | **Tidak ada konfirmasi destruktif baru** — commit = `#btnInject` yang sama (395); pratinjau wajib D-08 sudah berfungsi sebagai gerbang konfirmasi. |

> **Catatan removal (INJ-11):** Phase 396 menghapus dua entry-point UI BulkBackfill + view file + route. Ini **penghapusan murni — no new visual**. Tak ada redirect 302, tak ada banner "tool dipindahkan", tak ada halaman 404 kustom (D-10: bersih total, route → 404 default). Satu-satunya pintu masuk inject = kartu Section C "Inject Assessment Manual" (394).

---

## Aksesibilitas

- Radio toggle metode (N1): `<label for>` terkait tiap radio; keyboard-navigable (Bootstrap default).
- Tabel Pratinjau batch (N3): `role="status" aria-live="polite"` agar skor terbaca screen-reader saat diperbarui pasca-upload (pola roster 395 `:444` / panel peserta 394 `:269`).
- Daftar error (N4): `.alert.alert-danger` punya `role="alert"` (Bootstrap alert default) → diumumkan saat muncul.
- Tombol "Upload & Pratinjau" disabled memakai atribut `disabled` (bukan hanya visual) saat tak ada file / saat fetch.
- Render NIP/Nama/teks-error via `.textContent` (XSS + a11y aman), bukan `innerHTML` (carry 395).
- Input file `<input type="file">` punya `<label for="step5ExcelFile">` eksplisit.
- Target sentuh tombol = ukuran default Bootstrap `.btn` (cukup; tak ada pengecualian 44px khusus).

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| Bootstrap 5.3.0 (CDN) | card, alert, badge, form-check, input-group, table, spinner, btn, form-control (file) | not applicable — pustaka CSS standar, bukan registry pihak ketiga |
| Bootstrap Icons 1.10.0 | bi-* (file-earmark-excel, download, upload, info-circle, exclamation-triangle-fill, cloud-arrow-up-fill, check-circle) | not applicable |

Tidak ada shadcn, tidak ada component registry, tidak ada npm design system. Project = ASP.NET Core MVC + Razor → shadcn initialization gate **N/A** (bukan React/Next/Vite). Tidak ada blok pihak ketiga dideklarasikan/dipakai. ClosedXML (Excel) = library backend untuk artefak file, **bukan** registry UI. Registry safety = **not applicable**.

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
