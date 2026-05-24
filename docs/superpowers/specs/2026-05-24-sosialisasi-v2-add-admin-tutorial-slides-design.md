# Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2 — Tambah 4 Tutorial Slide Admin Panel

**Tanggal:** 2026-05-24
**File target:** `docs/Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html`
**Status:** Design draft — pending user review

---

## Konteks

Deck `Sosialisasi-Internal-Tim-HC-PortalHC-KPB-v2.html` (30 slide, 3506 baris) adalah sosialisasi internal Tim HC untuk Portal HC KPB. Saat ini fitur Admin Panel di Bagian 4 (Sl22–Sl28) belum mencakup tutorial UI untuk 3 area kritis:

1. **Bank soal** (`/Admin/AssessmentAdmin/ManagePackageQuestions` + `ImportPackageQuestions`) — tidak ada slide dedicated, hanya badge `ImportQuestions` di Audit Log (Sl28).
2. **Create Assessment** (`/Admin/CreateAssessment`) — Sl12 cuma konsep 7-step swim-lane, bukan tutorial UI wizard.
3. **Monitoring Actions** (per-peserta + group-level di `/Admin/AssessmentMonitoringDetail`) — Sl27 hanya overview tabel grup, action detail tidak dijelaskan.

Tambahkan 4 slide tutorial baru di Bagian 4 (urutan lifecycle assessment). Deck final 30 → 34 slide.

---

## Section Map Sebelum vs Sesudah

| # | Sebelum | Sesudah |
|---|---------|---------|
| 22 | Admin Panel Landing | Admin Panel Landing |
| 23 | Manajemen Pekerja | Manajemen Pekerja |
| 24 | Coach-Coachee Mapping | Coach-Coachee Mapping |
| 25 | Silabus + Guidance | Silabus + Guidance |
| 26 | Override KKJ | **NEW: Manage Package Question** |
| 27 | Assessment Monitoring | Override KKJ *(shift)* |
| 28 | Maintenance + Audit Log | **NEW: Create Assessment — Wizard Overview** |
| 29 | Quick Reference *(B5 Closing)* | **NEW: Create Assessment — Field Detail** |
| 30 | Terima Kasih *(B5 Closing)* | Assessment Monitoring (Overview) *(shift)* |
| 31 | — | **NEW: Monitoring Actions** |
| 32 | — | Maintenance + Audit Log *(shift)* |
| 33 | — | Quick Reference *(shift)* |
| 34 | — | Terima Kasih *(shift)* |

**4 slide baru:** Sl26, Sl28, Sl29, Sl31. **Shift:** 7 slide (existing Sl26..Sl30 → Sl27, Sl30, Sl32, Sl33, Sl34).

---

## Layout Pattern (Konsisten dengan Slide Existing)

Semua slide baru pakai struktur Bagian 4:

```html
<div class="slide default-deco" data-slide="{N}">
  <div class="slide-header">
    <div>
      <p class="section-eyebrow">BAGIAN 4 &mdash; ADMIN PANEL</p>
      <h1 class="slide-title">{Title} <span class="accent">{Accent}</span></h1>
      <p class="slide-subtitle">{Subtitle}</p>
    </div>
    <div class="slide-badge">SLIDE {N} / 34</div>
  </div>
  <div class="slide-body">
    <div class="slide-mockup-split">
      <div class="mockup-frame">
        <div class="mockup-bar">
          <span class="mockup-dot red"></span><span class="mockup-dot yellow"></span><span class="mockup-dot green"></span>
          <span class="mockup-url">localhost:5277/KPB-PortalHC/{route}</span>
        </div>
        <div class="mockup-recreated">{...recreated UI...}</div>
      </div>
      <div class="mockup-content">
        <h4>{icon} {heading}</h4>
        <ul>{...bullets...}</ul>
        <div class="mockup-warn">{warning}</div>
      </div>
    </div>
    <div class="tip-bar">{fungsi summary}</div>
  </div>
  <p class="panduan-ref">{ref}</p>
</div>
```

---

## Slide 26 NEW — Manage Package Question

**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Manage Package <span class="accent">Question</span>
**Subtitle:** Bank soal per package — manual form / Import Excel / Paste from Excel
**Badge:** SLIDE 26 / 34
**Route:** `localhost:5277/KPB-PortalHC/Admin/AssessmentAdmin/ManagePackageQuestions?packageId=…`

### Mockup Frame (kiri)
Header card mini: `Package: Safety Op T1 — 24 soal · 240 poin`

Split 2-col dalam mockup:
- **Kiri (col-7)** tabel mini 4-row contoh dengan badge tipe berbeda:
  | # | Teks Soal (truncated) | Tipe | Skor | Aksi |
  |---|---|---|---|---|
  | 1 | "LOTO step pertama yang…" | `MC` (biru) | 10 | 👁 ✏ 🗑 |
  | 2 | "Pilih semua APAR class…" | `MA` (ungu) | 15 | 👁 ✏ 🗑 |
  | 3 | "Jelaskan prosedur ESD…" | `Essay` (orange) | 25 | 👁 ✏ 🗑 |
  | 4 | "Hot work permit valid…" | `MC` (biru) | 10 | 👁 ✏ 🗑 |
- **Kanan (col-5)** card "Tambah Soal Baru":
  - dropdown QuestionType: MultipleChoice / MultipleAnswer / Essay
  - textarea teks soal (placeholder)
  - 4 input Opsi A-D (conditional, hidden untuk Essay)
  - input skor + button Simpan
- Header kanan atas: 2 button outline — `📤 Import Excel` (success) · `← Kembali ke Packages` (secondary)

### Mockup Content (kanan)
- **h4** `📝 3 Entry Mode`
- **ul**:
  - **Manual form** — tambah per soal, langsung simpan
  - **Import Excel** — upload `.xlsx` 9 kolom (`Pertanyaan | A | B | C | D | Benar | Elemen Teknis | QuestionType | Rubrik`)
  - **Paste from Excel** — copy-paste cell langsung dari Excel (clipboard mode)
- **ul kedua** — `3 QuestionType`:
  - `MultipleChoice` (default) — 1 jawaban benar
  - `MultipleAnswer` — multi jawaban (format `A,C` di kolom Benar)
  - `Essay` — opsi kosong, **Rubrik wajib** (manual grading)
- **mockup-tip**: `💡 4 template download tersedia: MC · MA · Essay · Universal — pilih sesuai mode soal`
- **mockup-warn**: `⚠ Delete soal cascade ke history attempt. Preview dulu via tombol 👁 sebelum hapus.`

### Tip-bar
`📝 Fungsi: Kelola bank soal per package assessment. 3 cara input (manual / upload Excel / paste). Mix tipe MC + MA + Essay dalam 1 package. Hapus per soal atau bulk import.`

### Panduan ref
`Panduan Operasional HC §5.3 — Bank Soal & Package Question`

---

## Slide 28 NEW — Create Assessment (Wizard Overview)

**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Create <span class="accent">Assessment</span> — Wizard 4 Step
**Subtitle:** Kategori → Peserta → Settings → Konfirmasi
**Badge:** SLIDE 28 / 34
**Route:** `localhost:5277/KPB-PortalHC/Admin/CreateAssessment`

### Mockup Frame (kiri)
- **Breadcrumb mini**: Kelola Data › Manage Assessment & Training › **Buat Assessment**
- **nav-pills 4 step** (recreate visual exact):
  - Pill 1: `● 1. Kategori` (bg-primary, active)
  - Pill 2: `○ 2. Peserta` (border, disabled, muted)
  - Pill 3: `○ 3. Settings` (border, disabled, muted)
  - Pill 4: `○ 4. Konfirmasi` (border, disabled, muted)
- **Card Step 1 expanded** dengan fields:
  - Label `Kategori Assessment *` + `<select>` dengan optgroup contoh:
    - `HSSE` (standalone)
    - `Operations` (optgroup parent) → `Alkylation`, `RFCC`, `NHT` (indent children)
  - Label `Judul Assessment *` + input `Operator T1 Q2 2026` + counter `26/255`
  - Label `Tipe Assessment` + `<select>` Standard (selected)
  - Helper text: `Assessment standar dengan satu sesi ujian.`
- **Footer button**: `Lanjut ke Step 2 →` (primary, disabled state aware)

### Mockup Content (kanan)
- **h4** `🧙 Wizard 4 Step Sequential`
- **ul**: ringkasan tiap step (1 baris each):
  - **Step 1** Kategori + Judul + Tipe — pilih scope
  - **Step 2** Peserta — pilih NIP target
  - **Step 3** Settings — durasi, passing grade, jadwal, package soal
  - **Step 4** Konfirmasi — review sebelum publish
- **h4** `🎯 3 Tipe Assessment` (sub-section)
- **3 mini-card** (or compact list dengan colored left-border):
  - `Standard` (biru) — 1 sesi ujian, hasil langsung
  - `Pre-Post Test` (ungu) — 2 sesi (gain score), ukur efektivitas training
  - `Proton` (orange) — per Track (Operator/Panelman, Tahun 1/2/3), conditional field track muncul
- **mockup-tip**: `💡 Mode Renewal otomatis pre-fill dari sesi expired — kategori + peserta diturunkan dari sesi sumber`

### Tip-bar
`📝 Fungsi: Wizard 4 step untuk buat assessment baru. Step bersifat sequential — step 2-4 baru aktif setelah step sebelumnya valid. 3 tipe: Standard / Pre-Post / Proton (track-aware).`

### Panduan ref
`Panduan Operasional HC §5.4 — Create Assessment (Wizard)`

---

## Slide 29 NEW — Create Assessment (Field Detail Step 2-4)

**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Create Assessment <span class="accent">Field Detail</span>
**Subtitle:** Step 2 Peserta · Step 3 Settings · Step 4 Konfirmasi
**Badge:** SLIDE 29 / 34
**Route:** `localhost:5277/KPB-PortalHC/Admin/CreateAssessment` *(lanjutan Sl28)*

### Mockup Frame (kiri)
Alih-alih full mockup, tampilkan **3 mini-card vertikal** (atau 3-col grid kecil) yang masing-masing menggambarkan 1 step:

**Card A — Step 2: Peserta**
- Filter row mini: dropdown Unit + dropdown Bagian + input search NIP
- Tabel mini 3-row: checkbox + NIP + Nama + Unit
- Counter `12 peserta dipilih`

**Card B — Step 3: Settings**
- Form mini 6 field:
  - `Durasi (menit)` — input `60`
  - `Passing Grade (%)` — input `70`
  - `Tanggal Mulai` — datepicker `2026-06-01 09:00`
  - `Package Soal` — dropdown `Safety Op T1 (24 soal)`
  - Toggle `Token Required` — `[●○]` off
  - Toggle `Anti-Copy` — `[●●]` on

**Card C — Step 4: Konfirmasi**
- Review summary card:
  - Kategori: Safety Operation
  - Judul: Operator T1 Q2 2026
  - Peserta: 12 orang
  - Jadwal: 2026-06-01 09:00 (60 menit)
  - Package: Safety Op T1
- Button `✓ Publish Assessment` (success large)

### Mockup Content (kanan)
- **h4** `📋 Field per Step`
- **tabel ringkas 3-row**:
  | Step | Field Utama | Catatan |
  |------|-------------|---------|
  | 2 Peserta | NIP picker + filter Unit/Bagian | Bisa bulk pilih per unit |
  | 3 Settings | Durasi, Passing, Jadwal, Package, Token, Anti-Copy | Anti-Copy aktif default |
  | 4 Konfirmasi | Review semua field | Tombol Publish ireversibel |
- **mockup-tip**: `💡 Token Required → tiap peserta dapat token unik (regenerate via Sl30 Monitoring)`
- **mockup-warn**: `⚠ Publish ireversibel: setelah klik, assessment muncul di portal peserta. Edit hanya boleh sebelum ada peserta submit (lihat AssessmentEditEligibility).`

### Tip-bar
`📝 Fungsi: Detail field Step 2-4 wizard. Step 2 pilih peserta dari master pekerja. Step 3 set durasi/passing/jadwal/package + 2 toggle. Step 4 review + Publish.`

### Panduan ref
`Panduan Operasional HC §5.4 — Field Wizard (Step 2-4)`

---

## Slide 31 NEW — Monitoring Actions (Detail per Peserta)

**Eyebrow:** BAGIAN 4 — ADMIN PANEL
**Title:** Monitoring <span class="accent">Actions</span> — Detail per Peserta
**Subtitle:** Drill dari Sl30 overview — Reset / Akhiri / Reshuffle / Tambah Waktu
**Badge:** SLIDE 31 / 34
**Route:** `localhost:5277/KPB-PortalHC/Admin/AssessmentMonitoringDetail?title=...`

### Mockup Frame (kiri)
- **Top action bar (group-level)**:
  - Button warning `⏱ Tambah Waktu` (kanan atas)
  - Button danger `✕ Akhiri Semua Ujian` (kanan atas, modal-confirm)
- **Tabel peserta 4-row**:
  | NIP | Nama | Progress | Status | Aksi |
  |-----|------|----------|--------|------|
  | 754201 | Widodo A. | 15/30 (50%) | `InProgress` (green) | Reset · Akhiri · Reshuffle |
  | 754202 | Andi P.   | 30/30 (100%) | `Submitted` (blue) | Reset · — · — |
  | 754203 | Citra R.  | 0/30 (0%)   | `NotStarted` (gray) | Reset · — · Reshuffle |
  | 754204 | Budi S.   | 8/30 (27%)  | `InProgress` (green) | Reset · Akhiri · Reshuffle |

### Mockup Content (kanan)
- **h4** `🎛 Action Hierarchy — 2 Level`
- **ul Group-Level** (header style):
  - `⏱ Tambah Waktu` — extend timer semua peserta sekaligus
  - `✕ Akhiri Semua Ujian` — force-close all, auto-submit (modal confirm)
  - `🔀 Reshuffle All` — ganti package semua peserta sekaligus
- **ul Per-Peserta**:
  - `↺ Reset` — hapus semua jawaban peserta, attempt baru
  - `✕ Akhiri Ujian` — force-submit per peserta (auto-grade), `InProgress` only
  - `🔀 Reshuffle Worker` — AJAX ganti package per peserta, no reload
- **mockup-warn**: `⚠ Akhiri = ireversibel auto-submit. Reset = ireversibel hapus progress. Reshuffle = peserta dapat soal beda dari package lain.`
- **mockup-tip**: `💡 Use case Reshuffle: curiga bocor soal di tengah ujian. Use case Tambah Waktu: peserta disconnect / hardware issue.`

### Tip-bar
`📝 Fungsi: Page Detail per grup assessment (klik dari Sl30 overview). 3 action group-level + 3 action per-peserta. Pakai untuk handle disconnect, freeze peserta, atau curiga kebocoran soal.`

### Panduan ref
`Panduan Operasional HC §5.6, 5.7, 5.8 — Monitor / Reset / Force-Close (selaras Sl30 overview)`

---

## Renumber Strategy

Semua badge `SLIDE N / 30` di slide existing **wajib di-update** ke `SLIDE N / 34`. Renumber sequential:

| Existing badge | New badge | Slide |
|---|---|---|
| SLIDE 1 / 30 → SLIDE 25 / 30 | SLIDE 1 / 34 → SLIDE 25 / 34 | total denominator change |
| SLIDE 26 / 30 (Override KKJ) | SLIDE 27 / 34 | shift +1 |
| SLIDE 27 / 30 (Monitoring) | SLIDE 30 / 34 | shift +3 |
| SLIDE 28 / 30 (Maintenance) | SLIDE 32 / 34 | shift +4 |
| SLIDE 29 / 30 (Quick Ref) | SLIDE 33 / 34 | shift +4 |
| SLIDE 30 / 30 (Terima Kasih) | SLIDE 34 / 34 | shift +4 |

**Search-replace pattern (case-sensitive):**
- `data-slide="26"` → `data-slide="27"` *(Override KKJ)*
- `data-slide="27"` → `data-slide="30"` *(Monitoring)*
- `data-slide="28"` → `data-slide="32"` *(Maintenance)*
- `data-slide="29"` → `data-slide="33"` *(Quick Ref)*
- `data-slide="30"` → `data-slide="34"` *(Terima Kasih)*
- `SLIDE 26 / 30` → `SLIDE 27 / 34`
- `SLIDE 27 / 30` → `SLIDE 30 / 34`
- `SLIDE 28 / 30` → `SLIDE 32 / 34`
- `SLIDE 29 / 30` → `SLIDE 33 / 34`
- `SLIDE 30 / 30` → `SLIDE 34 / 34`
- Untuk Sl1..Sl25: hanya update denominator `/ 30` → `/ 34` (data-slide tetap)

**Risiko**: pattern `SLIDE 30 / 30` ada di Sl30, replace dulu Sl30 sebelum Sl1..25 denominator. Atau pakai 2-pass:
1. Pass 1: rename shifted slides (urutan **descending** Sl30→Sl29→Sl28→Sl27→Sl26) untuk avoid collision
2. Pass 2: update denominator `/ 30` → `/ 34` di semua slide non-shifted

Implementasi: lakukan via Edit tool dengan `replace_all` per-slide unique string (gunakan context line) atau script Python one-shot.

---

## CSS Tambahan (Opsional)

Pattern existing punya class yang re-usable:
- `slide-mockup-split` — split 2-col mockup+content
- `mockup-frame` / `mockup-bar` / `mockup-recreated` — browser frame
- `mockup-content` / `mockup-tip` / `mockup-warn` — content side
- `tip-bar` — footer fungsi summary
- `panduan-ref` — reference link
- `mr-table` / `mr-filter-chip` / `mr-btn` / `mr-metric` — Admin Panel mockup atoms
- `mr-badge-pill` `mr-badge-green/orange/blue` — status badges

**Tidak perlu CSS baru.** Semua mockup recreate pakai class existing. Kalau butuh nav-pills wizard (Sl28) atau toggle (Sl29 settings card) → inline style `<style>` tag di scope slide tertentu, atau extend section `/* SLIDE 28 WIZARD */` di `<style>` global existing.

**Komponen baru yang mungkin perlu inline style:**
- Nav-pills wizard Sl28: `display:flex;gap:8px;` + `.pill.active{bg-primary}` + `.pill.disabled{border;color:muted}` (Bootstrap-like, ringan)
- 3 mini-card horizontal Sl29: `display:grid;grid-template-columns:repeat(3,1fr);gap:8px;` + card kecil dengan badge step number

---

## Verifikasi & Test Plan

Sosialisasi deck = HTML statis, no JS logic. Verifikasi visual only:

1. **Buka file di browser lokal** (`Live Server` VS Code atau `file://` direct).
2. **Cek 4 slide baru** muat tanpa overflow (Sl26, Sl28, Sl29, Sl31):
   - Mockup frame tidak crop
   - Content kanan tidak terpotong
   - Tip-bar terlihat di footer slide
3. **Cek renumber** semua slide badge konsisten `/ 34`.
4. **Cek navigation** (kalau ada keyboard nav) `data-slide` sequential 1..34.
5. **Cek dark mode** (kalau body.dark toggle exist) — semua slide baru contrast OK.
6. **Print preview** (Ctrl+P) — pastikan 1 slide per page, layout tidak break.

---

## Out of Scope

- ❌ Tidak refactor slide existing (Sl22..Sl28 lama) selain renumber + denominator update.
- ❌ Tidak buat versi v3 deck baru — edit langsung file v2 in-place.
- ❌ Tidak bikin PDF export — sosialisasi deck primary delivery = HTML browser presentation.
- ❌ Tidak ubah branding/header/footer/navigation existing.
- ❌ Tidak tambah JS interaktivity baru — recreate mockup pure HTML+CSS.

---

## Open Questions (Untuk User Review)

1. **Mockup data sample** — apakah nama peserta (Widodo, Andi, Citra, Budi) + NIP (754201..) OK, atau pakai placeholder generic (`User 1, 2, ...`)?
2. **Panduan ref §** — section number `5.3 / 5.4 / 5.6-8` adalah **guess**. Apakah sudah ada Panduan Operasional HC actual yang punya section number ini? Kalau belum, drop `§N` atau pakai placeholder `(TBD)`.
3. **Renewal mode** di Sl28 hanya disebut di tip — apakah perlu lebih ditonjolkan (mini-card sendiri)?
4. **Sl29 mockup** — 3 mini-card horizontal atau 1 mockup besar dengan 3 tab? Saat ini design: 3 mini-card.
5. **Tag/release** — apakah commit dengan tag `sosialisasi-internal-hc-v2.1` setelah merge?
