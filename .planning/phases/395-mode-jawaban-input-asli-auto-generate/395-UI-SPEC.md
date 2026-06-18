---
phase: 395
slug: mode-jawaban-input-asli-auto-generate
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-18
---

# Phase 395 — UI Design Contract

> Kontrak visual & interaksi untuk Langkah 5 (Jawaban per-pekerja) + permukaan Pratinjau wizard `/Admin/InjectAssessment`. Dibuat oleh gsd-ui-researcher, diverifikasi gsd-ui-checker.
>
> **PENTING — fase ini MEMPERLUAS halaman yang sudah ada, bukan membuat baru.** `/Admin/InjectAssessment` (`Views/Admin/InjectAssessment.cshtml`) sudah dibangun di Phase 394 sebagai wizard 6-langkah nav-pills. Sistem desain (warna, spasi, tipografi, pola kartu, gaya admin "Kelola Data" Section C) **SUDAH MAPAN**. Spec ini **mewarisi & mendokumentasikan** token wizard yang ada (Bootstrap 5.3 + tema Portal HC), bukan menciptakan sistem baru. Eksekutor wajib setia pada pola yang sudah berjalan.
>
> Semua copy = **Bahasa Indonesia** (alat internal HC, CLAUDE.md mewajibkan Bahasa Indonesia). Token teknis (hex/px/weight) tetap apa adanya.

---

## Scope Visual Fase Ini

Yang **DIBANGUN** di fase ini (mengganti `#step5Placeholder` `InjectAssessment.cshtml:404`, + permukaan Pratinjau, + roster):

1. **Sub-komponen navigasi pekerja 1-per-layar** (D-03) — kontrol Prev/Next antar pekerja, indikator pekerja saat ini, roster ringkas mode (auto/manual) + status kelengkapan.
2. **Form jawaban per-pekerja** — toggle mode (input-asli / auto-generate); input-asli render soal authored (MC radio, MA checkbox, Essay textarea + input skor); auto-generate input skor target (+override per-pekerja).
3. **Permukaan Pratinjau** (D-09) — tombol "Pratinjau" → skor final aktual % + badge Lulus/Tidak. State: belum-dipratinjau / sedang-memuat / hasil-tampil / catatan overshoot.
4. **State warning BLOCKING** (D-08.3/D-10) — alert inline saat `target > ceiling` + aksi "Beralih ke input asli".
5. **State validasi/kosong/error** — warning soal di-skip (warn-but-allow D-05), essay teks-wajib-jika-skor-diisi (D-04), MC-pilih-1 / MA-pilih-≥1.
6. **Carry-in LBL-02** — perbaiki label tipe soal `injTypeLabel()` (`:735-738`) + pesan validasi (`:832-833`) dari "Pilihan Ganda"/"Pilihan Majemuk" → "Single Answer"/"Multiple Answer".

Di luar scope visual: Import Excel (396), link Pre/Post (397), preview nomor sertifikat (ditolak D-09), matrix/accordion (ditolak D-03).

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (ASP.NET Core MVC + Razor `.cshtml`, bukan shadcn/React) |
| Preset | not applicable |
| Component library | Bootstrap 5.3.0 (CDN `bootstrap.min.css`, `_Layout.cshtml:38`) — utility classes + komponen `card`/`nav-pills`/`alert`/`badge`/`form-check`/`input-group` + CSS kustom proyek (`wwwroot/css/site.css`) |
| Icon library | Bootstrap Icons 1.10.0 (`<i class="bi bi-*">`, `_Layout.cshtml:39`) |
| Font | Inter (Google Fonts, weights 300;400;500;600;700;800, `_Layout.cshtml:41`); fallback ke Bootstrap system stack |

**Pola wizard yang DIWARISI (jangan ciptakan ulang):**
- Container: `.container-fluid px-4 py-4`.
- Setiap langkah: `.card.shadow-sm.border-0` → `.card-body.p-4` → heading `<h5 class="fw-bold mb-4"><i class="bi bi-* text-primary me-2"></i>Langkah N: …</h5>`.
- Sub-kartu dalam langkah: `.card` → `.card-header.bg-light` (judul `fw-semibold`/`<h6 class="mb-0">`) → `.card-body`.
- Tombol nav langkah: `.btn.btn-outline-secondary.btn-prev` (kiri, ikon `bi-arrow-left`) + `.btn.btn-primary.btn-next` (kanan, ikon `bi-arrow-right`), dibungkus `.d-flex.justify-content-between.mt-4`.
- Render dinamis JS = **`.textContent` (XSS-safe)**, bukan `innerHTML` untuk data user (pola `:763`, `:665`). Wajib diikuti sub-komponen 395.
- Step-5 self-contained: ganti `#step5Placeholder` **tanpa menyentuh** pills/`goToStep` luar (seam comment `:403`). Navigasi antar-pekerja independen dari navigasi antar-langkah.

---

## Spacing Scale

Bootstrap spacer = `1rem = 16px`; kelas `*-1..*-5` memetakan ke kelipatan 4. Skala yang dipakai (kelipatan 4):

| Token | Value | Bootstrap util | Usage di fase ini |
|-------|-------|----------------|-------------------|
| xs | 4px | `*-1`, `gap-1`, `py-0`/`px-1` | Jarak ikon-teks, padding tombol mini (hapus baris) |
| sm | 8px | `*-2`, `gap-2`, `mb-2` | Jarak antar opsi jawaban, jarak antar badge roster |
| md | 16px | `*-3`/`g-3`, `mb-3`, `card-body` default | Jarak antar field form jawaban, gap baris soal |
| lg | 24px | `*-4`, `mb-4`, `.card-body.p-4`, `g-4` | Padding kartu langkah, jarak heading→konten |
| xl | 32px | `*-5`, `py-4` (≈24) + tambahan | Jeda antar blok besar (roster ↔ form) |

Exceptions:
- Kontrol Prev/Next antar pekerja memakai pola `.d-flex.justify-content-between` dengan `mt-3`/`mt-4` (selaras tombol langkah), bukan token baru.
- Daftar pekerja/roster yang panjang memakai `max-height` + `overflow-y:auto` (pola `#userCheckboxContainer` `:246`, `max-height:320px`) — bukan token spasi.

---

## Typography

Inter via Bootstrap default scale. **3 ukuran + 2 berat** dideklarasikan: **400 (reguler)** untuk semua teks bertubuh, **700 (bold)** untuk semua penekanan (heading, sub-heading kartu, label field wajib). Konsisten dengan wizard 394.

| Role | Size | Weight | Line Height |
|------|------|--------|-------------|
| Heading langkah (`h5.fw-bold`) | 20px (`1.25rem`, Bootstrap h5) | 700 (bold) | 1.2 |
| Sub-heading kartu (`h6.fw-bold`) | 16px (`1rem`, Bootstrap h6) | 700 (bold) | 1.2 |
| Body / teks soal / opsi | 16px (`1rem`) | 400 (reguler) | 1.5 |
| Label field wajib (`.form-label.fw-bold`) | 16px (`1rem`) | 700 (bold) | 1.5 |
| Small / bantuan / badge roster (`.small`, `.form-text`) | 14px (`0.875rem`) | 400 (reguler) | 1.4 |

Aturan:
- **Hanya 2 berat dipakai sebagai kontrak: 400 (reguler) & 700 (bold).** Semua penekanan → 700; sisanya → 400.
- Body & teks soal/opsi: 16px / 400 / line-height 1.5.
- Teks bantuan, catatan overshoot, hint di bawah field: `.form-text.text-muted` 14px / 400.
- **Jangan** memperkenalkan ukuran/berat di luar tabel ini. (Catatan: utility Bootstrap `fw-medium`/`fw-semibold` ada di chrome wizard 394 yang sudah dikirim; permukaan baru Langkah 5 mengikuti kontrak 2-berat di atas.)

---

## Color

Mewarisi tema default Bootstrap 5.3 (`--bs-*`) seperti seluruh wizard 394. Tidak ada palet kustom baru.

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `#ffffff` / `--bs-body-bg` | Latar halaman & `card-body` |
| Secondary (30%) | `#f8f9fa` (`--bs-light` / `.bg-light`) + border `#dee2e6` | Header kartu, latar roster, panel sekunder, kartu langkah ber-`shadow-sm` |
| Accent (10%) | `#0d6efd` (`--bs-primary`) | Lihat daftar reserved di bawah |
| Destructive | `#dc3545` (`--bs-danger`) | Hanya: tombol hapus baris soal/aksi destruktif, ikon `text-danger` penanda wajib `*`, alert error |

Accent (`--bs-primary` biru) **hanya** untuk:
- Tombol aksi utama maju (`.btn.btn-primary.btn-next`, "Selanjutnya").
- Ikon heading langkah (`<i class="bi bi-pencil-square text-primary>`).
- Pill langkah aktif (`bg-primary text-white active`) — dikelola `updatePills()` yang sudah ada, **jangan diubah**.
- Badge hitung-pekerja / hitung-poin (`.badge.bg-primary`), badge roster mode.
- Tombol "Pratinjau Skor" boleh `.btn-outline-primary` (aksi sekunder on-demand, bukan submit utama).

Warna semantik tambahan (sudah dipakai wizard — pakai konsisten, bukan accent baru):
- **Success** `#198754` (`--bs-success`): badge "Lulus", tombol commit final `.btn.btn-success#btnInject` (jangan ubah varian — tetap success), pill langkah selesai (`bg-success`).
- **Warning** `#ffc107` / `.alert-warning`: warning soal di-skip (warn-but-allow), warning BLOCKING target>ceiling, blok hint MA "centang ≥2".
- **Danger** `#dc3545` / `.alert-danger`: badge "Tidak Lulus", error validasi inline (essay teks wajib, MC/MA kosong), tombol hapus.
- **Info** `#0dcaf0` / `.alert-info` (gaya `alert-secondary` juga dipakai): banner mode/penjelasan auto-gen, baris info netral.

---

## Komponen & State (kontrak interaksi)

**Focal point Langkah 5:** anchor visual utama = **indikator pekerja `"Pekerja {n} dari {total} — {NIP} · {Nama}"` (`h6.fw-bold`) + blok form soal aktif** tepat di bawahnya. Roster (K1) = sekunder (kartu `.bg-light` di samping/atas), navigasi Prev/Next = tersier (`.btn-sm.btn-outline-secondary`). Saat hasil Pratinjau tampil (K5), skor final `fs-4 fw-bold` menjadi anchor sekunder yang menonjol. Hierarki: form aktif > skor pratinjau > roster > kontrol nav.

### K1 — Sub-komponen navigasi pekerja (1-pekerja-per-layar, D-03)
- **Container**: ganti `#step5Placeholder` dengan blok ber-state sendiri (IIFE) di dalam `#step-5`; **jangan** menyentuh `btnPrev5`/`btnNext5` (navigasi antar-langkah) maupun pills.
- **Indikator pekerja saat ini**: teks `fw-semibold` di atas form — format `Pekerja {idx} dari {total} — {NIP} · {Nama}`. Render via `.textContent` (XSS-safe).
- **Kontrol antar-pekerja**: dua tombol `.btn.btn-outline-secondary.btn-sm` "« Pekerja Sebelumnya" (ikon `bi-chevron-left`) dan "Pekerja Berikutnya »" (ikon `bi-chevron-right`), dibungkus `.d-flex.justify-content-between.align-items-center.mb-3`. Disable saat di ujung (pekerja pertama/terakhir).
- **Roster ringkas** (Claude's Discretion, RESEARCH Pattern open-Q 2): kartu `.card` `.card-header.bg-light` "Ringkasan Pekerja" + `.card-body.p-0` berisi `<table class="table table-hover table-sm mb-0 align-middle">` (pola `injRenderQuestionList` `:751-755`). Kolom: **#**, **NIP / Nama**, **Mode** (badge `bg-primary`="Auto" / `bg-secondary`="Input asli"), **Skor Pratinjau** (kosong "—" bila belum di-Pratinjau; persen + badge Lulus/Tidak bila sudah), **Status** (ikon: `bi-check-circle-fill text-success` selesai / `bi-exclamation-triangle-fill text-warning` ada peringatan / `bi-x-circle-fill text-danger` BLOCKING). Baris pekerja aktif diberi highlight `table-active`. Klik baris = lompat ke pekerja itu. Roster dalam `max-height:280px;overflow-y:auto` bila panjang.
- **Rebuild on enter**: saat masuk Step-5 (event `goToStep(5)`), bangun ulang state dari `#userCheckboxContainer .user-checkbox:checked` + prune answer dengan `QuestionTempId` yang sudah dihapus dari `injQuestions[]` (Pitfall TempId dangling). Bila tidak ada pekerja terpilih → tampilkan empty state (lihat Copywriting).

### K2 — Toggle mode per-pekerja (D-01)
- **Default per-room + override per-pekerja**: di atas roster, satu kontrol default-room (`.form-check.form-switch` atau radio inline) "Mode default room: ( ) Input asli ( ) Auto-generate".
- **Per-pekerja**: radio inline di kepala form pekerja — `.btn-group` toggle atau `.form-check.form-check-inline` dua opsi: "Input jawaban asli" / "Auto-generate dari skor target". Pilihan menimpa default room untuk pekerja itu; tercermin di badge Mode roster.
- Transisi mode meng-render-ulang body form (K3 ↔ K4) tanpa kehilangan jawaban yang sudah diisi (pre-fill saat switch, D-10).

### K3 — Form input-asli (per-soal, INJ-08, D-04/D-05)
Render `injQuestions[]` berurutan; tiap soal = blok `.mb-3` dengan label teks soal `fw-medium` + badge tipe (LBL-02 label baru):
- **Single Answer (MC)**: grup `.form-check` radio (1 grup per soal); pilih tepat 1 opsi. Pola opsi mirror `_InjectQuestionForm` (`input-group` huruf A/B/C/D), tapi read-only teks + radio pilih jawaban pekerja. `aria-label="opsi A"` dst (pola a11y `:46`).
- **Multiple Answer (MA)**: `.form-check` checkbox; pilih ≥1. Hint `.alert-info.py-1.px-2.small` "Centang semua opsi yang dipilih pekerja."
- **Essay**: `<textarea class="form-control" rows="3">` teks jawaban + `<input type="number" min="0" max="{ScoreValue}">` skor. Label "Skor Essay (0–{ScoreValue})".
- **Skip = OMIT** (D-05): soal yang sengaja dilewati **tidak** dikirim sebagai answer-spec (bukan spec kosong). UI boleh tampilkan checkbox/penanda "Lewati soal ini" yang menonaktifkan input soal; saat serialize, soal ber-skip tidak di-push ke `answers[]`. Warn-but-allow: tampilkan ringkasan "N soal dilewati → dihitung 0" di permukaan Pratinjau/konfirmasi, jangan blokir.
- **Validasi inline** (saat Pratinjau/serialize): MC belum pilih / pilih ≠1 → tandai `is-invalid` + pesan; MA pilih 0 padahal soal engaged → pesan; Essay skor diisi tapi teks kosong → `is-invalid` (D-04). Pesan via `.invalid-feedback`/alert `.alert-danger.py-1.px-2.small.d-none` (pola `#injAuthError` `:75`).

### K4 — Form auto-generate (INJ-09, D-02/D-06/D-08)
- **Input target**: `<input type="number" min="0" max="100">` "Skor target (%)" — default batch + override per-pekerja. `.form-text.text-muted` "Sistem memilih pola jawaban benar/salah agar capaian ≥ target."
- **HYBRID room ber-essay** (D-08.1): bila ada soal Essay, tetap render input skor essay (+teks D-04) untuk tiap essay di bawah input target. Banner `.alert-info` "Room ini memuat soal essay — skor essay diisi manual (auto-generate hanya menyentuh soal pilihan)."
- **Aksi**: tombol "Pratinjau Skor" (lihat K5) untuk hitung skor aktual; auto-generate tidak menghitung di klien (server-otoritas, RESEARCH Pattern 3/5).

### K5 — Permukaan Pratinjau (D-09)
Tombol `.btn.btn-outline-primary` "Pratinjau Skor" (ikon `bi-eye`) on-demand (bukan per-keystroke). State permukaan hasil (blok di bawah form, pola `#titleCheckResult` `:151` `style="display:none"`):
- **Belum dipratinjau** (default): blok tersembunyi; roster Skor Pratinjau = "—".
- **Sedang memuat**: `.text-muted.small` + `<span class="spinner-border spinner-border-sm me-1">` + "Menghitung skor…" (pola `:919`). Tombol Pratinjau disabled saat fetch.
- **Hasil tampil** (`target reachable`): `.alert-success` bila Lulus / `.alert-warning` bila Tidak Lulus. Isi: skor final aktual besar (`fs-4 fw-bold`) "{skor}%" + badge `bg-success`"Lulus" / `bg-danger`"Tidak Lulus". **TANPA nomor sertifikat** (D-09).
- **Catatan overshoot** (auto-gen, capaian > target): `.form-text.text-muted` "Target {target}% → capaian aktual {aktual}% (+{selisih}) karena pembulatan." Netral, bukan error.
- **BLOCKING** (target > ceiling, D-08.3): lihat K6.
- Roster (K1) diperbarui dengan skor + status tiap kali Pratinjau dijalankan.

### K6 — State BLOCKING target > ceiling (D-08.3 / D-10)
- Alert `.alert-warning` (atau `.alert-danger` bila ingin lebih tegas — pilih **warning** agar konsisten "warn, arahkan, jangan blok diam-diam"; commit pekerja ini ditahan). Ikon `bi-exclamation-triangle-fill`.
- Copy: lihat Copywriting (target tak tercapai + maks ceiling).
- Aksi inline: tombol `.btn.btn-sm.btn-outline-primary` "Beralih ke input asli" → set mode pekerja = input-asli, **pre-fill** grid dari pola auto-gen terakhir (D-10), HC tweak + naikkan skor essay manual.
- Roster: status pekerja ini = ikon `bi-x-circle-fill text-danger` sampai teratasi. `#btnInject` tetap aktif tetapi commit hanya memproses pekerja yang valid; pekerja BLOCKING tidak boleh ter-commit (integritas sertifikasi).

### K7 — Carry-in LBL-02 (perbaikan label)
- `injTypeLabel()` `:736-738`: `'MultipleChoice' → 'Single Answer'`, `'MultipleAnswer' → 'Multiple Answer'`, `'Essay' → 'Essay'`.
- Pesan validasi `:832-833`: "Single Answer harus tepat 1 jawaban benar." / "Multiple Answer butuh minimal 2 jawaban benar."
- Badge tipe soal di form jawaban (K3) memakai label baru ini secara konsisten.

---

## Copywriting Contract

Semua Bahasa Indonesia. Tabel ringkas + state tambahan di bawah.

| Element | Copy |
|---------|------|
| Primary CTA (commit) | "Inject Assessment" (tombol `#btnInject` yang ada — jangan ubah) |
| CTA Pratinjau | "Pratinjau Skor" |
| CTA navigasi pekerja | "« Pekerja Sebelumnya" / "Pekerja Berikutnya »" |
| CTA switch mode (BLOCKING) | "Beralih ke input asli" |
| Empty state heading (tak ada pekerja) | "Belum ada pekerja dipilih" |
| Empty state body | "Kembali ke Langkah 2 untuk memilih pekerja sebelum mengisi jawaban." |
| Empty state (tak ada soal) | "Belum ada soal. Tambahkan soal di Langkah 3 terlebih dahulu." |
| Error state (essay teks wajib, D-04) | "Teks jawaban essay wajib diisi karena skornya diisi." |
| Error state (MC kosong/≠1) | "Single Answer harus memilih tepat 1 jawaban." |
| Error state (MA kosong) | "Multiple Answer harus memilih minimal 1 jawaban." |
| Error state (Pratinjau gagal) | "Gagal menghitung pratinjau. Coba lagi." |
| Destructive confirmation | Hapus soal (di Langkah 3, sudah ada): "Hapus soal ini?" — tidak ada aksi destruktif baru di Langkah 5 |

State naratif tambahan:

| State | Copy |
|-------|------|
| Indikator pekerja | "Pekerja {n} dari {total} — {NIP} · {Nama}" |
| Toggle mode (label) | "Input jawaban asli" / "Auto-generate dari skor target" |
| Toggle default room | "Mode default room" |
| Badge roster mode | "Input asli" / "Auto" |
| Hint MA (input-asli) | "Centang semua opsi yang dipilih pekerja." |
| Hint target (auto-gen) | "Sistem memilih pola jawaban benar/salah agar capaian ≥ target." |
| Label skor essay | "Skor Essay (0–{ScoreValue})" |
| Banner HYBRID essay | "Room ini memuat soal essay — skor essay diisi manual (auto-generate hanya menyentuh soal pilihan)." |
| Pratinjau loading | "Menghitung skor…" |
| Pratinjau Lulus | "Lulus" (badge `bg-success`) |
| Pratinjau Tidak Lulus | "Tidak Lulus" (badge `bg-danger`) |
| Catatan overshoot | "Target {target}% → capaian aktual {aktual}% (+{selisih}) karena pembulatan." |
| Warning skip (warn-but-allow, D-05) | "{n} soal dilewati untuk pekerja ini — dihitung 0. Lanjut?" |
| Warning BLOCKING (D-08.3) | "Target {target}% tidak tercapai: bobot essay dikecualikan dari auto-generate, maksimum {ceiling}%. Beralih ke input asli dan naikkan skor essay secara manual." |
| Label skip soal | "Lewati soal ini (dihitung 0)" |
| Konfirmasi sebelum commit (opsional) | "Inject {n} pekerja sebagai assessment historis? Skor & sertifikat dihitung otomatis." |

LBL-02 (perbaikan label, K7): "Single Answer" / "Multiple Answer" / "Essay" — **jangan** pakai "Pilihan Ganda"/"Pilihan Majemuk".

---

## Aksesibilitas

- Radio/checkbox opsi jawaban: `aria-label="opsi A"` … "opsi D" (pola `_InjectQuestionForm.cshtml:46` + lesson PXF-11/387).
- Roster `role="status" aria-live="polite"` saat skor pratinjau diperbarui (pola panel peserta `:269`).
- Render data user via `.textContent` (XSS + a11y aman), tidak `innerHTML`.
- Tombol disabled di ujung navigasi pekerja memakai atribut `disabled` (bukan hanya visual).
- Target sentuh tombol nav-pekerja ≥ ukuran `.btn-sm` default Bootstrap (cukup; tidak ada pengecualian 44px khusus).

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| Bootstrap 5.3.0 (CDN) | card, nav-pills, alert, badge, form-check/switch, input-group, table, spinner, btn | not applicable — pustaka CSS standar, bukan registry pihak ketiga |
| Bootstrap Icons 1.10.0 | bi-* (pencil-square, eye, chevron-left/right, check-circle-fill, exclamation-triangle-fill, x-circle-fill, cloud-arrow-up-fill, dll.) | not applicable |

Tidak ada shadcn, tidak ada component registry, tidak ada npm design system. Registry safety = **not applicable**.

---

## Checker Sign-Off

- [x] Dimension 1 Copywriting: PASS
- [x] Dimension 2 Visuals: PASS
- [x] Dimension 3 Color: PASS
- [x] Dimension 4 Typography: PASS
- [x] Dimension 5 Spacing: PASS
- [x] Dimension 6 Registry Safety: PASS

**Approval:** approved 2026-06-18 (gsd-ui-checker, 6/6 PASS, 0 FLAG — after 1 revision: typography→2 weights, CTA→"Pratinjau Skor", focal point declared)
