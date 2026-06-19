---
phase: 397
slug: link-pre-post-ke-room-existing
status: approved
shadcn_initialized: false
preset: none
created: 2026-06-18
reviewed_at: 2026-06-18
---

# Phase 397 — UI Design Contract

> Kontrak visual & interaksi untuk **penautan Pre/Post ke room existing** di wizard `/Admin/InjectAssessment`. Dibuat oleh gsd-ui-researcher, diverifikasi gsd-ui-checker.
>
> **PENTING — fase ini MEMPERLUAS halaman yang sudah ada, bukan membuat baru.** `/Admin/InjectAssessment` (`Views/Admin/InjectAssessment.cshtml`) dibangun di Phase 394 (wizard 6-langkah nav-pills), diperluas 395 (Langkah 5 jawaban per-pekerja) & 396 (Langkah 5 toggle Form/Excel). Sistem desain (warna, spasi, tipografi, kartu, badge, modal, permukaan Pratinjau) **SUDAH MAPAN & DISETUJUI** ([394-UI-SPEC.md](../394-page-setup-room-authoring-soal/394-UI-SPEC.md), [395-UI-SPEC.md](../395-mode-jawaban-input-asli-auto-generate/395-UI-SPEC.md), [396-UI-SPEC.md](../396-import-excel-retire-bulkbackfill/396-UI-SPEC.md) — checker 6/6 PASS masing-masing). Spec ini **MEWARISI** token tersebut dan **HANYA** mendeklarasikan elemen NET-BARU: tombol "Cari Room" + chip terpilih di Langkah 1, **modal search picker room**, **ringkasan pairing** di permukaan Pratinjau, **entri anti-dobel-link** di daftar error, dan **kontrol unlink/ubah tautan** pasca-commit. Tidak ada bahasa visual baru.
>
> Semua copy = **Bahasa Indonesia** (alat internal HC, CLAUDE.md mewajibkan Bahasa Indonesia). Token teknis (hex/px/weight) tetap apa adanya.
>
> Semua 12 keputusan (D-01..D-12) di [397-CONTEXT.md](397-CONTEXT.md) **TERKUNCI** — spec ini hanya men-detailkan lapisan visual/interaksi, tidak memutus-ulang pilihan produk (picker = MODAL D-05; filter = tipe-lawan D-06; link OPSIONAL D-04; unlink IN-scope D-12; dll).

---

## Inheritance Statement (apa yang DIWARISI — JANGAN ciptakan ulang)

| Kontrak | Sumber | Status di 397 |
|---------|--------|---------------|
| Design system (Bootstrap 5.3 + Bootstrap Icons 1.10.0 + Inter, no shadcn/React) | 394/395/396 §Design System | **INHERIT verbatim** |
| Spacing scale (Bootstrap spacer kelipatan 4: 4/8/16/24/32) | 394/395/396 §Spacing | **INHERIT verbatim** |
| Typography (3 ukuran + **kontrak 2-berat**: 400 reguler + 700 bold) | 395/396 §Typography | **INHERIT verbatim** |
| Color (60 putih / 30 `bg-light` / 10 primary `#0d6efd`; success/warning/danger/info semantik) | 394/395/396 §Color | **INHERIT verbatim** |
| Pola wizard: `.card.shadow-sm.border-0` → `.card-body.p-4` → `h5.fw-bold` heading | 394 §Component Inventory | **INHERIT** (tak disentuh) |
| Pills + `goToStep`/`updatePills` (navigasi antar-langkah) | 394 wizard scaffold | **INHERIT — JANGAN refactor** (394 D-01) |
| Permukaan Pratinjau (engine `AssessmentScoreAggregator.Compute`, `preview == commit`, **tanpa nomor sertifikat**) | 395 K5 / 396 N3 / D-07 | **INHERIT engine** — diperluas dengan **blok ringkasan pairing** (lihat N3) |
| Tombol commit `#btnInject` (`.btn.btn-success`, "Inject Assessment") | 394/395 | **INHERIT — jangan ubah varian/label** |
| Toggle metode Form/Excel Step-5 (`step5Method`) | 396 N1 | **INHERIT — tak disentuh** (link Pre/Post ortogonal dari metode jawaban) |
| Pola Bootstrap modal (`.modal.fade` → `.modal-dialog-centered.modal-lg` → `.modal-content.border-0.shadow` → header berwarna → `.modal-body.p-4`) | `CreateAssessment.cshtml:757-834` (`#successModal`) | **REUSE pola verbatim** untuk modal picker (N2) |
| Pola badge (`.badge.bg-primary` / `.bg-secondary` / `.bg-success` / `.bg-danger`) | InjectAssessment `:241/883/1337` | **REUSE** untuk badge tipe room, indikator grup, badge unpaired |
| Pola input-group + tombol search (`#btnCheckTitle` `:145-148`) | InjectAssessment Step-1 | **REUSE** sebagai analog tombol "Cari Room" |
| Render data user via `.textContent` (XSS-safe), bukan `innerHTML` | 395/396 §Design System | **INHERIT — wajib** untuk judul/kategori/NIP/Nama room |
| Verifikasi runtime Playwright dari **main working tree** (no Razor RuntimeCompilation; lesson 354/392) | 394/395/396 | **INHERIT — wajib** (modal/picker/chip/preview render runtime) |

> **Aturan 2-berat (carry 395):** hanya **400 (reguler)** + **700 (bold/`fw-bold`)** sebagai kontrak. `fw-semibold` (600) yang muncul di chrome wizard 394 (mis. `card-header`, `modal` label CreateAssessment `:770` `fw-semibold`) adalah brownfield carry-over — pertahankan untuk kesetiaan mirror modal, jangan tambah berat baru di markup NET-BARU 397.

---

## Scope Visual Fase Ini

Yang **DIBANGUN** di fase ini (semua di `InjectAssessment.cshtml`, ditambah endpoint pendukung non-UI):

1. **Pemicu "Cari Room" + chip terpilih di Langkah 1 (D-04/D-05)** — saat `#assessmentTypeInput` = `PreTest`/`PostTest`, tampilkan tombol "Cari Room" (mengganti placeholder note `:162` "Penautan Pre/Post ke room existing tersedia pada fase berikutnya"). Setelah memilih room target, room tampil sebagai **chip** removable. Penautan **OPSIONAL** — chip boleh dikosongkan (skip).
2. **Modal search picker room (D-05/D-06/D-10)** — Bootstrap modal pop-up: kotak search (Judul/Kategori/jadwal, debounced) + daftar baris room **tipe-LAWAN saja** (inject Pre → daftar PostTest; inject Post → daftar PreTest), mencakup room **inject MAUPUN online** (D-10). Tiap baris: judul + kategori + tanggal + badge tipe + jumlah peserta + indikator **"sudah ber-grup"** (Kasus A) vs **"standalone"** (Kasus B).
3. **Ringkasan pairing di permukaan Pratinjau (D-07)** — di Pratinjau pra-commit (Step-5/6, reuse `PreviewInjectScore`): blok ringkasan = berapa pekerja akan **ter-pair** ke sibling existing, berapa **unpaired** (D-03, styling warn), + **banner peringatan** bila penautan akan **menulis ke data online** (Kasus B standalone — "data online akan disentuh"), + **baris WARN** bila tanggal Pre > Post target (D-11).
4. **Entri anti-dobel-link di daftar error (D-08)** — bila per-pekerja sudah punya sibling tipe-sama di grup target → entri **blok/warn** di daftar error Pratinjau (pola daftar-lengkap 396 N4).
5. **Kontrol unlink/ubah tautan pasca-commit (D-12)** — kontrol minimal untuk membatalkan/mengganti tautan setelah commit, dengan **konfirmasi destruktif** (karena dapat me-revert stiker grup pada sesi online). Lihat N5.

Di luar scope visual: link untuk tipe Standard (hanya Pre/Post punya picker, D-06); editor link umum / bulk re-link (out-of-scope D-12); auto-detect by judul saja sebagai satu-satunya jalur (ditolak — picker eksplisit D-05/D-06); multi-paket per room & import gambar Excel (out-of-scope spec §12); mengubah skor/jawaban/status sesi online (haram — hanya `LinkedGroupId`/`LinkedSessionId`).

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (ASP.NET Core MVC + Razor `.cshtml`, **bukan** shadcn/React — shadcn gate N/A) |
| Preset | not applicable |
| Component library | Bootstrap 5.3.0 (CDN, `_Layout.cshtml:38`) — `modal`/`card`/`nav`/`alert`/`badge`/`form-check`/`input-group`/`table`/`btn` + `wwwroot/css/site.css` |
| Icon library | Bootstrap Icons 1.10.0 (`<i class="bi bi-*">`, `_Layout.cshtml:39`) |
| Font | Inter (Google Fonts 300;400;500;600;700;800, `_Layout.cshtml:41`); fallback system stack |

**Stack TETAP — jangan perkenalkan component library / CSS framework / SPA framework.** Halaman server-rendered Razor + vanilla JS. Tiru konvensi markup Step-1 + pola modal CreateAssessment verbatim.

**shadcn gate:** N/A. Proyek = ASP.NET Core MVC + Razor (bukan React/Next/Vite) → tidak ada `components.json`, tidak ada inisialisasi shadcn, registry safety **not applicable**.

**Verifikasi wajib (carry 354/392):** app pakai `AddControllersWithViews()` TANPA `AddRazorRuntimeCompilation` → view embedded saat build. Perilaku runtime — show/hide tombol "Cari Room" saat tipe Pre/Post, buka/tutup modal, search debounced, render baris room, set chip, render ringkasan pairing di Pratinjau, daftar anti-dobel, dialog unlink — **WAJIB diverifikasi Playwright runtime** (grep+`dotnet build` tak cukup). Jalankan app dari **main working tree** (bukan sibling `PortalHC_KPB-ITHandoff`), AD-off (`Authentication__UseActiveDirectory=false`), Playwright `--workers=1`.

---

## Spacing Scale

**INHERIT dari 394/395/396** — Bootstrap spacer (`1rem = 16px`), kelas `*-1..*-5` kelipatan 4. Markup NET-BARU 397 memakai HANYA token berikut:

| Token | Value | Bootstrap util | Usage NET-BARU di fase ini |
|-------|-------|----------------|----------------------------|
| xs | 4px | `*-1`, `gap-1`, `me-1` | Jarak ikon-teks tombol "Cari Room"/chip; gap badge dalam baris room |
| sm | 8px | `*-2`, `gap-2`, `mb-2` | Jarak antar baris hasil picker; gap meta room (kategori·tanggal·peserta); padding chip; jarak item ringkasan pairing |
| md | 16px | `*-3`/`g-3`, `mb-3`, `modal-body p-4` (lihat lg) | Jarak antar blok dalam modal (search ↔ daftar); jarak chip↔tombol; jarak blok ringkasan pairing dalam Pratinjau |
| lg | 24px | `*-4`, `mb-4`, `card-body p-4`, `modal-body p-4` | Padding body modal picker; padding kartu; jarak heading→konten |

Exceptions:
- Daftar hasil picker panjang: `max-height:340px; overflow-y:auto` pada container daftar di `modal-body` (reuse pola overflow roster `:443` `max-height:280px` & picker existing `:246` `max-height:320px` — bukan token spasi baru).
- Modal picker pakai `modal-lg` (lebar) agar baris room (judul + kategori + tanggal + badge + peserta + indikator grup) muat satu baris — sama seperti `#successModal` CreateAssessment (`modal-lg`).

---

## Typography

**INHERIT kontrak 2-berat dari 395/396 verbatim.** 3 ukuran + **2 berat**: **400 (reguler)** untuk teks bertubuh, **700 (`fw-bold`)** untuk penekanan. Tidak ada ukuran/berat baru.

| Role | Size | Weight | Line Height | Usage NET-BARU |
|------|------|--------|-------------|----------------|
| Heading langkah (`h5.fw-bold`) | 20px (h5) | 700 | 1.2 | (diwarisi — "Langkah 1: Setup Room", tak disentuh) |
| Judul modal (`h5.modal-title`) | 20px | 700 (`fw-bold`) | 1.2 | "Cari Room Pasangan (Pre/Post)" |
| Judul room di baris hasil | 16px (1rem) | 700 (`fw-bold`) | 1.5 | Judul room dalam tiap baris picker (penekanan utama baris) |
| Body / meta room / teks ringkasan | 16px (1rem) | 400 | 1.5 | Kategori·tanggal·jumlah peserta; teks ringkasan pairing; teks item daftar error |
| Label field (`.form-label.fw-bold`) | 16px | 700 | 1.5 | Label "Tipe Assessment", label search modal |
| Small / bantuan / chip / badge | 14px (0.875rem) | 400 | 1.4 | `.form-text.text-muted` hint; teks chip room terpilih; badge tipe/indikator grup; baris meta sekunder |

Aturan (carry 395): semua penekanan → 700; sisanya → 400. Jangan memperkenalkan ukuran/berat di luar tabel ini.

---

## Color

**INHERIT tema Bootstrap 5.3 (`--bs-*`) dari 394/395/396.** Tidak ada palet kustom baru. Pembagian 60/30/10 sama persis dengan wizard.

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `#ffffff` / `--bs-body-bg` | Latar halaman, `card-body`, `modal-body`, baris hasil picker |
| Secondary (30%) | `#f8f9fa` (`--bs-light` / `.bg-light`) + border `#dee2e6` | `modal-header` picker, hover baris room (`list-group-item-action`), latar chip terpilih, `shadow`/`shadow-sm` |
| Accent (10%) | `#0d6efd` (`--bs-primary`) | Lihat daftar reserved di bawah |
| Success (semantik) | `#198754` (`--bs-success`) | Badge "Sudah ber-grup" (Kasus A — aman, tak sentuh online); badge "Lulus" di Pratinjau (diwarisi); ikon ter-pair di ringkasan pairing |
| Warning (semantik) | `#ffc107` (`--bs-warning`) / `.alert-warning` | **Banner "data online akan disentuh"** (Kasus B, D-07); badge "Standalone" (Kasus B — penautan akan menulis ke online); baris "N pekerja tanpa pasangan" (D-03); WARN tanggal Pre>Post (D-11); entri anti-dobel mode-warn (D-08) |
| Danger (semantik) | `#dc3545` (`--bs-danger`) / `.alert-danger` | Entri anti-dobel-link mode-BLOK (D-08, bila diperlakukan blocking); ikon `*` wajib; **konfirmasi unlink** (tombol `btn-danger`, D-12) |
| Info (semantik) | `#0dcaf0` (`--bs-info`) / `.alert-info` | Instruksi modal picker (cara memilih room pasangan); penjelasan ringkas "stiker grup" |

**Accent (`#0d6efd` primary) reserved for (carry 394/395/396, + NET-BARU):**
1. (diwarisi) Tombol nav maju `.btn-primary.btn-next`, pill aktif, ikon heading langkah, badge hitung.
2. **Tombol "Cari Room"** = `.btn.btn-outline-primary` (ikon `bi-search`) — aksi sekunder on-demand di Step-1 (sejajar "Cek Judul" `btn-outline-secondary` & "Pratinjau Skor" 395 outline-primary). *(Pakai outline-primary, bukan outline-secondary, untuk membedakan dari "Cek Judul" karena ini membuka modal pencarian utama.)*
3. **Tombol "Pilih"** pada baris room dalam modal = `.btn.btn-sm.btn-outline-primary` (aksi pilih per-baris). Alternatif: seluruh baris `list-group-item-action` klik-able (lihat N2c).
4. **Chip room terpilih** — varian netral (lihat N1b): latar `bg-light` + border, BUKAN accent biru penuh (chip = state tampilan, bukan aksi). Tombol hapus chip = `btn-close` kecil (abu, bukan accent).

> **Pemisahan tegas (carry 395):** accent biru BUKAN untuk semua elemen interaktif. Commit final = **success green** (`#btnInject`, jangan diubah). Peringatan "sentuh online" + unpaired + tanggal janggal = **warning** (kuning, allow). Anti-dobel-link = **danger/warning** sesuai blok/warn. Unlink destruktif = **danger**. Indikator "sudah ber-grup" aman = **success**; "standalone" (akan tulis online) = **warning**.

---

## Komponen NET-BARU & State (kontrak interaksi)

**Focal point per permukaan:**
- **Step-1 (saat Pre/Post):** anchor = tombol "Cari Room" → setelah pilih, anchor berpindah ke **chip room terpilih** (ringkasan pilihan, visual menonjol-ringan). Tipe-dropdown tetap kontrol utama setup.
- **Modal picker:** anchor = **kotak search** di atas + **daftar baris room** (hierarki: search > daftar > instruksi).
- **Pratinjau:** anchor utama = tabel skor (diwarisi 395/396); **blok ringkasan pairing** = anchor sekunder tepat di bawah/atas tabel; **banner "sentuh online"** = paling menonjol bila Kasus B (warning di puncak ringkasan).

### N1 — Pemicu "Cari Room" + chip terpilih di Langkah 1 (D-04/D-05)

**Penempatan:** di dalam blok `Tipe Assessment` Step-1 (`InjectAssessment.cshtml:154-163`). **Ganti** placeholder note `:162` ("Penautan Pre/Post ke room existing tersedia pada fase berikutnya") dengan blok tautan kondisional `#prePostLinkBlock`.

#### N1a — Tombol "Cari Room"
- **Visibilitas:** `d-none` saat `#assessmentTypeInput` = `Standard`; tampil saat `PreTest`/`PostTest` (toggle via JS pada event `change`, pola show/hide `d-none` existing). *(D-06: hanya Pre/Post punya picker.)*
- **Kontrol:** `<button type="button" class="btn btn-outline-primary" id="btnCariRoom"><i class="bi bi-search me-1"></i>Cari Room Pasangan</button>` → membuka modal N2 (`data-bs-toggle="modal" data-bs-target="#roomPickerModal"` atau via JS).
- **Hint** (`.form-text.text-muted` `bi-info-circle`): "Opsional — tautkan ke room pasangan (Pre↔Post) agar tampil berpasangan. Bisa di-skip dan ditautkan nanti." *(D-04 opsional.)*
- **Konteks tipe-lawan ditampilkan ke HC:** hint dinamis menyesuaikan — saat inject PreTest: "Cari room **Post-Test** pasangannya." / saat PostTest: "Cari room **Pre-Test** pasangannya." (D-06).

#### N1b — Chip room terpilih
- Awalnya `d-none`. Setelah HC memilih room di modal → chip muncul, tombol "Cari Room" berubah jadi "Ganti Room" (atau tetap + chip di sampingnya — discretion, salah satu).
- **Markup chip:** `<span class="badge bg-light text-dark border d-inline-flex align-items-center gap-2 p-2" id="selectedRoomChip">` berisi:
  - ikon `bi-link-45deg text-primary`
  - teks judul room + (tanggal) via `.textContent` (XSS-safe)
  - badge tipe kecil (`bg-secondary`)
  - badge indikator grup (Kasus A `bg-success` "ber-grup" / Kasus B `bg-warning text-dark` "standalone")
  - tombol hapus: `<button type="button" class="btn-close ms-1" aria-label="Hapus tautan room"></button>` → mengosongkan chip + reset hidden field (`#LinkedGroupId`/`#LinkedTargetRepId`) → kembali ke state "belum tertaut". *(D-04: boleh dikosongkan/skip.)*
- **Hidden field** (form): chip men-set `<input type="hidden">` untuk `LinkedGroupId` (atau `RepresentativeId` target + flag Kasus A/B) yang akan terbawa ke `MapToRequest`. *(Implementasi field = discretion planner; chip = representasi visualnya.)*

### N2 — Modal search picker room (D-05/D-06/D-10) — `#roomPickerModal`

Reuse pola `#successModal` CreateAssessment (`:757-834`): `.modal.fade` → `.modal-dialog.modal-dialog-centered.modal-lg` → `.modal-content.border-0.shadow`.

#### N2a — Header
- `.modal-header` (latar **`bg-light`** atau `bg-primary text-white` — pilih `bg-light` agar netral; header sukses-hijau CreateAssessment tak cocok untuk picker) → `<h5 class="modal-title fw-bold"><i class="bi bi-link-45deg me-2"></i>Cari Room Pasangan (Pre/Post)</h5>` + `<button class="btn-close" data-bs-dismiss="modal" aria-label="Tutup">`.

#### N2b — Body: search + instruksi
- `.modal-body.p-4`.
- **Instruksi** (`.alert.alert-info.small.py-2` `bi-info-circle`): "Pilih room **{tipe-lawan}** untuk ditautkan. Room **'ber-grup'** sudah punya pasangan (aman). Room **'standalone'** belum — menautkannya akan menulis penanda grup ke data online tersebut (skor/jawaban tidak diubah)." *(D-06 + D-01 Kasus A/B framing non-teknis.)*
- **Search box:** `.input-group.mb-3` → `<span class="input-group-text"><i class="bi bi-search"></i></span>` + `<input type="text" class="form-control" id="roomSearchInput" placeholder="Cari judul, kategori, atau tanggal…">`. **Debounce ~300ms** lalu fetch endpoint picker (tipe-lawan ter-filter di server). *(D-06.)*

#### N2c — Body: daftar hasil room
- Container `<div id="roomPickerResults" class="list-group" style="max-height:340px;overflow-y:auto;" role="listbox" aria-label="Hasil pencarian room">`.
- **Tiap baris** = `<button type="button" class="list-group-item list-group-item-action">` (keyboard-navigable, klik = pilih). Layout 1 baris (`d-flex justify-content-between align-items-center`):
  - **Kiri:** judul room (`fw-bold`, `.textContent`) di atas, baris meta (`.small.text-muted`): "{Kategori} · {tanggal CompletedAt/Schedule} · {n} peserta".
  - **Kanan:** badge tipe (`bg-secondary` "Pre-Test"/"Post-Test") + badge indikator grup:
    - **Kasus A** (target sudah ber-`LinkedGroupId`): `<span class="badge bg-success">Sudah ber-grup</span>` (aman, tak sentuh online).
    - **Kasus B** (standalone, `LinkedGroupId=null`): `<span class="badge bg-warning text-dark">Standalone</span>` (penautan akan tulis ke online).
    - Badge tambahan `bg-info text-dark` "Inject" bila room target sendiri `IsManualEntry=true` (D-10 — picker tampilkan inject maupun online; opsional, bantu HC bedakan).
- **Klik baris** → set chip N1b + tutup modal (`data-bs-dismiss` via JS) → kembali ke Step-1 dengan room terpilih.
- **Render semua teks via `.textContent`** (judul/kategori user-authored → XSS-safe).

#### N2d — State modal
- **Loading** (saat fetch search): `<div class="text-center py-3"><span class="spinner-border spinner-border-sm me-1"></span>Mencari…</div>` di `#roomPickerResults` (pola loading 395/396).
- **Empty (0 hasil)**: `<div class="text-center text-muted py-4"><i class="bi bi-inbox d-block fs-3 mb-2"></i>Tidak ada room {tipe-lawan} yang cocok. Coba kata kunci lain.</div>`.
- **Empty awal (belum search)**: tampilkan daftar default (mis. terbaru, dari endpoint tanpa term) ATAU prompt "Ketik untuk mencari room pasangan." (discretion — konsisten dgn perilaku `ManageAssessmentTab_Assessment` default tanpa filter).
- **Error fetch**: `.alert.alert-danger.small` "Gagal memuat daftar room. Coba lagi." di dalam `#roomPickerResults`.

### N3 — Ringkasan pairing di Pratinjau (D-07) — `#previewPairingSummary`

Tampil di permukaan Pratinjau (Step-5/6, reuse `PreviewInjectScore`) **hanya bila room tertaut** (ada `LinkedGroupId`/target). Diletakkan **di atas atau tepat di bawah tabel skor** (anchor sekunder). Awalnya `d-none` (terisi pasca-preview).

- **Heading:** `.h6.fw-bold` "Ringkasan Penautan Pre/Post".
- **Baris status pairing** (3 metrik, format ringkas):
  - `<span class="badge bg-success">{p} ter-pair</span>` — pekerja yang punya sibling tipe-lawan di grup target (`LinkedSessionId` terisi).
  - `<span class="badge bg-warning text-dark">{u} tanpa pasangan</span>` — unpaired (D-03): `LinkedGroupId` di-set, `LinkedSessionId=null`, tampil sisi tunggal. Diikuti teks `.small` "Pekerja ini akan tampil sebagai sisi tunggal (mis. Pre-only). Tetap diizinkan."
- **Banner "data online akan disentuh" (Kasus B, D-07)** — bila room target standalone: `<div class="alert alert-warning d-flex align-items-start gap-2" role="alert"><i class="bi bi-exclamation-triangle-fill mt-1"></i><div>` heading `<strong>Data online akan disentuh.</strong>` + teks: "Room pasangan ini belum ber-grup. Saat commit, penanda grup ditulis ke {n} sesi online tersebut (skor, jawaban, dan status TIDAK diubah). Tindakan tercatat di audit." *(D-07 + D-09.)*
  - Kasus A (sudah ber-grup) → banner ini **TIDAK** muncul; ganti dengan `.alert.alert-success.small` opsional: "Room sudah ber-grup — penautan tidak menyentuh data online."
- **Baris WARN tanggal Pre>Post (D-11):** bila tanggal Pre (`CompletedAt`) lebih BARU dari Post target → `.alert.alert-warning.small.py-2` (`bi-calendar-x me-1`): "Tanggal Pre lebih baru dari Post — urutan janggal untuk gain-score. Periksa backdate. (Tetap diizinkan.)" **Tidak memblokir** commit (warn-but-allow).
- **`role="status" aria-live="polite"`** pada container (diperbarui pasca-preview).
- **Render angka & teks via `.textContent`** (count, nama room).

### N4 — Entri anti-dobel-link di daftar error (D-08)

Masuk ke **daftar error/warn Pratinjau yang sama** (reuse panel daftar 396 N4 + permukaan preview 395). Bukan panel baru — entri tambahan di list yang sudah ada.

- **Kondisi:** per-pekerja sudah punya sibling **tipe-sama** di grup target (mis. pekerja X sudah punya Pre online di grup itu → inject Pre kedua = ambigu untuk pairing & gain-score yang match by `UserId`).
- **Perlakuan (D-08 "blok/peringatkan"):** entri di daftar dengan **dua mode** (planner pilih sesuai keputusan blocking vs warn — CONTEXT menulis "tolak/peringatkan"):
  - Mode **BLOK** → `.alert-danger` item, ikon `bi-x-octagon-fill`, mencegah commit pekerja itu (atau seluruh batch bila atomic strict).
  - Mode **WARN** → `.alert-warning` item, ikon `bi-exclamation-triangle`, izinkan dengan peringatan.
  - **Rekomendasi default = BLOK per-pekerja** (integritas pairing; 2 sibling tipe-sama merusak gain-score by `UserId`). Konsistensi atomic dgn 396 D-09 (daftar lengkap, bukan stop di error pertama).
- **Contoh copy item:** "Pekerja {Nama} (NIP {NIP}) sudah memiliki {Pre/Post}-Test di grup target — tidak dapat ditautkan dua kali. Lepaskan tautan lama atau pilih room lain."
- **Render via `.textContent`** (Nama/NIP → XSS-safe).
- **Daftar LENGKAP** (semua pekerja bermasalah sekaligus), pola 396 N4; bungkus `max-height:240px;overflow-y:auto` bila >8 item.

### N5 — Kontrol unlink/ubah tautan pasca-commit (D-12)

Scope addition. **Minimal & fokus** — batal/ubah tautan, BUKAN editor link umum.

- **Lokasi:** karena wizard inject = alur sekali-jalan (commit lalu selesai), kontrol unlink hidup di **permukaan yang menampilkan room/grup tertaut pasca-commit**. Rekomendasi (discretion planner, verifikasi saat plan): tombol kecil pada tampilan detail/monitoring room Pre/Post tertaut (mis. di `ManageAssessment`/`AssessmentMonitoringDetail` baris grup) ATAU pada modal konfirmasi sukses inject. **Spec ini mengontrak lapisan visual + pola konfirmasi**, lokasi pasti = plan-time decision.
- **Pemicu:** `<button type="button" class="btn btn-sm btn-outline-danger" id="btnUnlinkRoom"><i class="bi bi-link-45deg"></i><i class="bi bi-x"></i> Lepaskan Tautan</button>` (ikon rantai-putus). Atau "Ubah Tautan" (`btn-outline-secondary`) bila mengganti, yang membuka kembali modal picker N2.
- **Konfirmasi destruktif WAJIB (D-12)** — karena unlink dapat me-revert stiker `LinkedGroupId` pada sesi ONLINE (Kasus B) bila grup jadi kosong-sebelah. Pola: **modal konfirmasi** (bukan `confirm()` native; reuse pola modal Bootstrap), warna header `bg-warning text-dark` atau `bg-danger text-white`:
  - **Heading:** "Lepaskan tautan Pre/Post?"
  - **Body:** "Tautan antar room ini akan dilepas. {Bila Kasus B & grup jadi kosong: 'Penanda grup pada sesi online yang sebelumnya ditulis juga akan dilepas — skor/jawaban tetap utuh.'} Tindakan tercatat di audit dan bersifat atomic."
  - **Tombol:** `<button class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>` + `<button class="btn btn-danger" id="btnConfirmUnlink"><i class="bi bi-link-45deg me-1"></i>Ya, Lepaskan</button>`.
- **Pasca-unlink:** notice sukses (`alert-success`/toast) "Tautan dilepas." + UI room kembali ke state tak-tertaut. Revert `LinkedSessionId` bidirectional + (opsional) revert `LinkedGroupId` online — atomic + audit (`"LinkPrePost"` reverse, D-09/D-12).
- **Jaga minimal:** TIDAK ada UI untuk memilih sesi individual, mengedit banyak grup sekaligus, atau re-assign per-pekerja. Hanya: lepas / ganti-room (re-open picker).

---

## Interaction Contracts (runtime — Playwright-verified)

1. **Tombol "Cari Room" kondisional tipe (D-04/D-06):** subah `#assessmentTypeInput` → `PreTest`/`PostTest` menampilkan `#btnCariRoom` + hint tipe-lawan; `Standard` menyembunyikannya. Placeholder note lama `:162` hilang/tergantikan. *(INJ-12, SC1)*
2. **Buka modal picker → filter tipe-lawan (D-05/D-06/D-10):** klik "Cari Room" → modal `#roomPickerModal` terbuka; daftar HANYA room tipe-lawan (inject Pre → PostTest; inject Post → PreTest); mencakup room inject & online (badge "Inject" bila manual). Search Judul/Kategori/jadwal mem-filter daftar (debounced). *(INJ-12, SC1)*
3. **Indikator Kasus A vs B di baris (D-01):** tiap baris menampilkan badge "Sudah ber-grup" (success) atau "Standalone" (warning) sesuai `LinkedGroupId` target null/non-null. *(INJ-12)*
4. **Pilih room → chip + skippable (D-04/D-05):** klik baris → modal tutup, chip room terpilih tampil di Step-1 dengan judul+tanggal+badge tipe+indikator grup; hidden field `LinkedGroupId`/target ter-set; klik `btn-close` chip → tautan dikosongkan (skip), form tetap valid untuk inject standalone. *(INJ-12, SC1, D-04)*
5. **Ringkasan pairing di Pratinjau (D-07):** saat room tertaut, Pratinjau menampilkan `{p} ter-pair` (success) + `{u} tanpa pasangan` (warning, D-03); banner warning "data online akan disentuh" muncul HANYA Kasus B; banner success "tidak menyentuh online" HANYA Kasus A. *(INJ-12, SC2, D-07)*
6. **WARN tanggal Pre>Post (D-11):** bila tanggal Pre > Post target → baris `alert-warning` di ringkasan; commit TIDAK diblokir. *(INJ-12, D-11)*
7. **Anti-dobel-link per-pekerja (D-08):** pekerja yang sudah punya sibling tipe-sama di grup target → entri di daftar error/warn Pratinjau (daftar lengkap, semua pekerja bermasalah); default BLOK mencegah double-link. *(INJ-12, D-08)*
8. **Commit byte-identik (carry 393/395):** klik `#btnInject` (success-green, tak diubah) → `MapToRequest` mengisi `LinkedGroupId`/`LinkedSessionId` per-pekerja → `InjectBatchAsync` atomic (write inject + write-back online Kasus B + audit `"LinkPrePost"` terpisah, D-09); grouping `LinkedGroupId` utuh di Records/Monitoring (silang inject↔online, spec §13). *(INJ-12, SC2/SC3)*
9. **Unlink pasca-commit + konfirmasi destruktif (D-12):** kontrol "Lepaskan Tautan" → modal konfirmasi (bukan `confirm()` native) → "Ya, Lepaskan" (`btn-danger`) → tautan dilepas atomic + audit; UI kembali ke state tak-tertaut; notice sukses. *(INJ-12, D-12)*
10. **Skip penautan (D-04):** tipe Pre/Post tanpa memilih room → inject berjalan standalone (`LinkedGroupId=null`), tanpa error; ringkasan pairing tidak muncul (tak ada room tertaut). *(INJ-12, D-04)*

---

## Copywriting Contract

Semua **Bahasa Indonesia**. Hanya string NET-BARU; sisanya diwarisi 394/395/396.

| Element | Copy |
|---------|------|
| Primary CTA (commit) | "Inject Assessment" (tombol `#btnInject` yang ada — **jangan ubah**) |
| CTA buka picker | "Cari Room Pasangan" (ikon `bi-search`) |
| CTA ganti room (chip aktif) | "Ganti Room" (ikon `bi-arrow-repeat`) |
| Hint tautan (opsional, D-04) | "Opsional — tautkan ke room pasangan (Pre↔Post) agar tampil berpasangan. Bisa di-skip dan ditautkan nanti." |
| Hint tipe-lawan (Pre) | "Cari room Post-Test pasangannya." |
| Hint tipe-lawan (Post) | "Cari room Pre-Test pasangannya." |
| Judul modal picker | "Cari Room Pasangan (Pre/Post)" |
| Instruksi modal (D-01/D-06) | "Pilih room {tipe-lawan} untuk ditautkan. Room 'ber-grup' sudah punya pasangan (aman). Room 'standalone' belum — menautkannya akan menulis penanda grup ke data online tersebut (skor/jawaban tidak diubah)." |
| Placeholder search | "Cari judul, kategori, atau tanggal…" |
| Loading search | "Mencari…" (spinner) |
| Meta baris room | "{Kategori} · {tanggal} · {n} peserta" |
| Badge tipe room | "Pre-Test" / "Post-Test" (`bg-secondary`) |
| Badge Kasus A | "Sudah ber-grup" (`bg-success`) |
| Badge Kasus B | "Standalone" (`bg-warning text-dark`) |
| Badge room inject (D-10) | "Inject" (`bg-info text-dark`) |
| **Empty state** — 0 hasil search | "Tidak ada room {tipe-lawan} yang cocok. Coba kata kunci lain." |
| **Empty state** — belum search (awal) | "Ketik untuk mencari room pasangan." (atau tampilkan daftar terbaru) |
| **Empty state** — belum tertaut (Step-1) | (tak ada chip; tombol "Cari Room Pasangan" + hint opsional terlihat) |
| Aria hapus chip | "Hapus tautan room" |
| Heading ringkasan pairing (D-07) | "Ringkasan Penautan Pre/Post" |
| Badge ter-pair | "{p} ter-pair" (`bg-success`) |
| Badge unpaired (D-03) | "{u} tanpa pasangan" (`bg-warning text-dark`) |
| Teks unpaired (D-03) | "Pekerja ini akan tampil sebagai sisi tunggal (mis. Pre-only). Tetap diizinkan." |
| **Banner sentuh online (Kasus B, D-07)** | Heading: "Data online akan disentuh." Body: "Room pasangan ini belum ber-grup. Saat commit, penanda grup ditulis ke {n} sesi online tersebut (skor, jawaban, dan status TIDAK diubah). Tindakan tercatat di audit." (`alert-warning`) |
| Notice Kasus A aman (opsional) | "Room sudah ber-grup — penautan tidak menyentuh data online." (`alert-success`) |
| WARN tanggal Pre>Post (D-11) | "Tanggal Pre lebih baru dari Post — urutan janggal untuk gain-score. Periksa backdate. (Tetap diizinkan.)" (`alert-warning`) |
| **Error — anti-dobel-link (D-08)** | "Pekerja {Nama} (NIP {NIP}) sudah memiliki {Pre/Post}-Test di grup target — tidak dapat ditautkan dua kali. Lepaskan tautan lama atau pilih room lain." |
| Error — gagal muat picker | "Gagal memuat daftar room. Coba lagi." |
| **Destructive — unlink (D-12)** | CTA: "Lepaskan Tautan" (`btn-outline-danger`, ikon rantai-putus). Konfirmasi (modal): Heading "Lepaskan tautan Pre/Post?" + Body "Tautan antar room ini akan dilepas. {Kasus B kosong-sebelah: 'Penanda grup pada sesi online yang sebelumnya ditulis juga akan dilepas — skor/jawaban tetap utuh.'} Tindakan tercatat di audit dan bersifat atomic." + tombol "Batal" (`btn-secondary`) / "Ya, Lepaskan" (`btn-danger`). |
| Notice sukses unlink | "Tautan dilepas." (`alert-success` / toast) |

> **Catatan destructive (D-12):** unlink memakai **modal konfirmasi Bootstrap**, BUKAN `window.confirm()` native (a11y + konsistensi). Tombol konfirmasi = `btn-danger` (merah) karena dapat me-revert penanda grup pada data online. Commit inject (`#btnInject`) sendiri TIDAK destruktif terhadap online (Kasus B hanya menulis penanda grup, tak menghapus) → tetap success-green; gerbang konfirmasinya = Pratinjau wajib (D-07) + banner "sentuh online".

---

## Aksesibilitas

- **Modal picker** (N2): `tabindex="-1"`, `aria-labelledby` ke judul, `aria-hidden` dikelola Bootstrap; **focus trap** otomatis Bootstrap modal — verifikasi fokus pindah ke search input saat terbuka (`shown.bs.modal` → `#roomSearchInput.focus()`), dan kembali ke `#btnCariRoom` saat ditutup. Keyboard: `Esc` menutup, `Tab` terjebak dalam modal.
- **Daftar room** (N2c): `role="listbox"` pada container, tiap baris = `<button class="list-group-item-action">` (fokusable + Enter/Space memilih). `aria-label` baris ringkas judul+tipe.
- **Chip room terpilih** (N1b): tombol hapus `btn-close` punya `aria-label="Hapus tautan room"`.
- **Ringkasan pairing** (N3): `role="status" aria-live="polite"` agar metrik pairing & banner terbaca screen-reader saat Pratinjau diperbarui.
- **Banner "sentuh online" & WARN tanggal**: `.alert` punya `role="alert"` (Bootstrap default) → diumumkan saat muncul.
- **Daftar anti-dobel** (N4): `role="alert"` pada `.alert` pembungkus; entri `<li>` per pekerja.
- **Modal konfirmasi unlink** (N5): fokus awal ke tombol "Batal" (default aman), `aria-describedby` ke body teks; bukan `confirm()` native.
- **Render judul/kategori/Nama/NIP via `.textContent`** (XSS + a11y aman), bukan `innerHTML` (carry 395/396).
- **Target sentuh** tombol = ukuran default Bootstrap `.btn`/`.btn-sm` (cukup; `btn-close` chip ≥ area klik standar Bootstrap). Tidak ada pengecualian 44px khusus.

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| Bootstrap 5.3.0 (CDN) | modal, card, alert, badge, list-group, input-group, form-check, table, spinner, btn, btn-close | not applicable — pustaka CSS standar, bukan registry pihak ketiga |
| Bootstrap Icons 1.10.0 | bi-* (search, link-45deg, info-circle, exclamation-triangle-fill, calendar-x, x-octagon-fill, arrow-repeat, inbox, x) | not applicable |

Tidak ada shadcn, tidak ada component registry, tidak ada npm design system, tidak ada blok pihak ketiga. Project = ASP.NET Core MVC + Razor → shadcn initialization gate **N/A** (bukan React/Next/Vite). Registry vetting gate = **not applicable** (tidak ada registry dideklarasikan). Registry safety = **not applicable**.

---

## Catatan untuk Planner (discretion CONTEXT → diturunkan)

Lapisan visual ini mengasumsikan keputusan teknis discretion CONTEXT (planner finalisasi, jangan ubah keputusan terkunci):
- **Hidden field tautan:** chip N1b men-set field yang dikonsumsi `MapToRequest` → `InjectRequest.LinkedGroupId`/`LinkedSessionId`. Bentuk pasti (target `RepresentativeId` + flag Kasus A/B vs `LinkedGroupId` langsung) = plan-time; spec mengontrak *chip = representasinya*.
- **Endpoint picker:** reuse/extend `ManageAssessmentTab_Assessment` (filter tipe-lawan + `IsManualEntry` tidak di-filter, D-10) atau endpoint baru ringan. Output harus membawa: `RepresentativeId`, judul, kategori, tanggal, `AssessmentType`, `LinkedGroupId` (null/non-null → Kasus A/B), jumlah peserta, `IsManualEntry`. Verifikasi bentuk JSON vs view saat plan.
- **Lokasi kontrol unlink (N5):** plan-time decision (monitoring/detail room vs modal sukses inject). Spec mengontrak pola konfirmasi + copy, bukan host pasti.
- **Mode anti-dobel (N4):** BLOK vs WARN — rekomendasi BLOK per-pekerja (integritas gain-score by `UserId`); planner kunci sesuai D-08 "tolak/peringatkan".

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
