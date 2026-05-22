# Sosialisasi PROTON Deck Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure deck `Sosialisasi-Umum-PortalHC-KPB.html` (19 slide generik) jadi `Sosialisasi-PROTON-KPB.html` (22 slide fokus PROTON dengan Template C demo layout untuk 8 slide).

**Architecture:** Single-file HTML deck dengan inline CSS + JS. Rename via `git mv`, drop 3 slide non-PROTON, tambah 6 slide baru (3 referensi dokumen + 1 timeline + 1 role + 1 dashboard), refactor 5 slide existing jadi Template C demo layout (kiri foto-placeholder · kanan teks bullet+takeaway), retone 5 slide existing dengan POV PROTON-first.

**Tech Stack:** HTML5, CSS3 (custom properties + grid), Vanilla JS (existing keyboard nav + slide-index counter). Tidak ada framework/build step.

**Spec:** `docs/superpowers/specs/2026-05-22-sosialisasi-proton-deck-design.md`

---

## File Structure

**Modify:**
- `docs/Sosialisasi-Umum-PortalHC-KPB.html` → rename to `docs/Sosialisasi-PROTON-KPB.html`
  - 2307 baris → estimasi ~2500 baris final
  - All edits in-place pada file ini

**Sumber screenshot (read-only, user paste manual):**
- `docs/doc support Proton/SS KPB_KKJ Fungsi System Completion & Simops.pdf`
- `docs/doc support Proton/silabus/Operator_Kompetensi {1-5} RevFinal.docx`
- `docs/doc support Proton/silabus/Panelman_Kompetensi {1-5} RevFinal.docx`
- `docs/doc support Proton/coaching guidance/CoachingGuidance_RFCC NHT Dimensi {1-5} {Operator,Panelman}.docx`
- Portal views (untuk screenshot demo): `Views/CDP/CoachingProton.cshtml`, `Views/CDP/PlanIdp.cshtml`, `Views/CDP/HistoriProton.cshtml`, `Views/CDP/EditCoachingSession.cshtml`, `Views/CMP/Assessment.cshtml`

**Branch:** `feat/sosialisasi-proton-deck` (sudah dibuat off main, spec sudah committed `1503d614`)

---

## Verification Strategy

Tidak ada unit test (single static HTML file). Verifikasi per task:
1. **Grep validation** — cek string ada/tidak ada (rename, drop, renumber)
2. **Browser smoke test** — open file lokal, klik nav arrows, klik agenda chip, toggle dark mode
3. **Visual snapshot mental** — render check Template C demo layout (frame ratio 16:10, bullet marker ▸, takeaway box amber)

**Browser test command (post setiap commit Template C atau struktural):**
```bash
# Windows
start "" "docs/Sosialisasi-PROTON-KPB.html"
# atau buka manual di Edge/Chrome
```

**Grep validation patterns:**
- Tidak ada `CPDP` / `Umum` (kecuali historical reference yg sengaja)
- Semua `data-slide="N"` unique & sequential 1..22
- Semua `SLIDE X / 22` badge konsisten
- `const TOTAL_SLIDES = 22` di JS block

---

## GROUP A — Foundation (Task 1-4)

### Task 1: Rename file + cover branding

**Files:**
- Rename: `docs/Sosialisasi-Umum-PortalHC-KPB.html` → `docs/Sosialisasi-PROTON-KPB.html`
- Modify: `docs/Sosialisasi-PROTON-KPB.html` (title + cover block lines ~1-10 + ~1356-1367)

- [ ] **Step 1.1: Rename file via git mv (preserve history)**

```bash
git mv "docs/Sosialisasi-Umum-PortalHC-KPB.html" "docs/Sosialisasi-PROTON-KPB.html"
git status
```
Expected: `renamed: docs/Sosialisasi-Umum-PortalHC-KPB.html -> docs/Sosialisasi-PROTON-KPB.html`

- [ ] **Step 1.2: Update `<title>` tag**

Find di file:
```html
<title>Sosialisasi Portal HC KPB — Untuk Pekerja Pertamina</title>
```
Replace:
```html
<title>Sosialisasi PROTON · Portal HC KPB</title>
```

- [ ] **Step 1.3: Update cover slide block**

Find (di sekitar line 1356-1367):
```html
<div class="slide cover active" data-slide="1">
    <div class="cover-content">
      <div class="cover-eyebrow">PERTAMINA KILANG BALIKPAPAN</div>
      <h1 class="cover-title">HUMAN CAPITAL PORTAL</h1>
      <div class="cover-divider"></div>
      <div class="cover-meta">
        <div class="cover-meta-eyebrow">Sosialisasi untuk Pekerja Pertamina</div>
        <div class="cover-meta-date">Balikpapan, 25 Mei 2026</div>
      </div>
    </div>
  </div>
```
Replace dengan:
```html
<div class="slide cover active" data-slide="1">
    <div class="cover-content">
      <div class="cover-eyebrow">PERTAMINA KILANG BALIKPAPAN</div>
      <h1 class="cover-title">PROTON</h1>
      <div class="cover-subtitle-small" style="font-size:1.1rem; opacity:0.85; margin-top:8px;">Human Capital Portal · Kilang Pertamina Balikpapan</div>
      <div class="cover-divider"></div>
      <div class="cover-meta">
        <div class="cover-meta-eyebrow">Sosialisasi Program · Section Head · Sr. Supervisor · Coach · Coachee</div>
        <div class="cover-meta-date">Balikpapan, [TBD tanggal acara]</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 1.4: Browser smoke test**

Buka `docs/Sosialisasi-PROTON-KPB.html` di Edge/Chrome. Verify:
- Tab title menampilkan "Sosialisasi PROTON · Portal HC KPB"
- Cover slide menampilkan **PROTON** sebagai title besar
- Cover meta menampilkan "Section Head · Sr. Supervisor · Coach · Coachee"
- Date placeholder `[TBD tanggal acara]` terlihat

- [ ] **Step 1.5: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "chore(sosialisasi-proton): git mv + rename title + cover retone"
```

---

### Task 2: Add Template C CSS

**Files:**
- Modify: `docs/Sosialisasi-PROTON-KPB.html` — tambah CSS block sebelum `</style>` penutup di header

- [ ] **Step 2.1: Locate end of existing `<style>` block**

Grep:
```bash
grep -n "</style>" docs/Sosialisasi-PROTON-KPB.html | head -1
```
Catat line number — itu insertion point.

- [ ] **Step 2.2: Insert Template C CSS block**

Sebelum `</style>`, tambah:
```css
/* ============================================================
   TEMPLATE C — Demo split layout (kiri foto · kanan teks)
   ============================================================ */
.slide-body.demo-split {
  display: grid;
  grid-template-columns: 55% 45%;
  gap: 32px;
  align-items: start;
}
.demo-image-frame {
  border: 2px dashed #94a3b8;
  border-radius: 12px;
  background: #f1f5f9;
  aspect-ratio: 16 / 10;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  color: #64748b;
  font-size: 0.9rem;
  overflow: hidden;
  text-align: center;
  padding: 16px;
}
.demo-image-frame img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  border-radius: 10px;
}
.demo-image-caption {
  margin-top: 10px;
  font-size: 0.78rem;
  color: #64748b;
  text-align: center;
  font-style: italic;
}
.demo-text-col { padding: 8px 0; }
.demo-lead {
  font-style: italic;
  color: #475569;
  margin-bottom: 14px;
  font-size: 0.95rem;
}
.demo-bullets {
  list-style: none;
  padding: 0;
  margin: 0 0 18px 0;
}
.demo-bullets li {
  padding: 6px 0 6px 22px;
  position: relative;
  font-size: 0.95rem;
  line-height: 1.5;
}
.demo-bullets li::before {
  content: "▸";
  position: absolute;
  left: 0;
  color: var(--navy);
  font-weight: 700;
}
.demo-takeaway {
  background: linear-gradient(90deg, #fef3c7, #fde68a);
  border-left: 4px solid #f59e0b;
  padding: 12px 14px;
  border-radius: 6px;
  font-weight: 600;
  font-size: 0.92rem;
  color: #78350f;
}
body.dark .demo-image-frame { background: #1e293b; border-color: #475569; color: #94a3b8; }
body.dark .demo-text-col { color: #e2e8f0; }
body.dark .demo-lead { color: #cbd5e1; }
body.dark .demo-takeaway { background: rgba(245, 158, 11, 0.15); color: #fef3c7; border-left-color: #fbbf24; }
```

- [ ] **Step 2.3: Browser CSS syntax verify**

Open di browser, F12 → Console — pastikan tidak ada CSS warning/error. Cek satu slide existing (#7 Assessment) masih render normal (tidak break karena CSS baru).

- [ ] **Step 2.4: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "feat(sosialisasi-proton): add Template C demo-split CSS"
```

---

### Task 3: Drop 3 non-PROTON slides

**Files:**
- Modify: `docs/Sosialisasi-PROTON-KPB.html` — hapus 3 slide block

- [ ] **Step 3.1: Locate old slide #8 block (5 Kategori Assessment Umum)**

Grep:
```bash
grep -n 'data-slide="8"' docs/Sosialisasi-PROTON-KPB.html
grep -n 'data-slide="9"' docs/Sosialisasi-PROTON-KPB.html
```
Old #8 mulai di sekitar line 1639, end sebelum line 1676 (start of #9). Verify range dengan baca file.

- [ ] **Step 3.2: Hapus old #8 — gunakan Edit tool**

Hapus seluruh block dari `<!-- comment Slide 8 -->` (jika ada) sampai dan termasuk `</div>` penutup slide tersebut. Block diawali:
```html
<div class="slide default-deco" data-slide="8">
```
dan diakhiri `</div>` yang match dengan div slide tersebut (cek nested div untuk slide-header dan slide-body).

- [ ] **Step 3.3: Locate dan hapus old #9 block (Alur Assessment 7-step Umum)**

Grep + hapus block:
```html
<div class="slide default-deco" data-slide="9">
  ...alur 7-step end-to-end content...
</div>
```

- [ ] **Step 3.4: Locate dan hapus old #17 block (Integrasi & Keamanan)**

Grep + hapus block:
```html
<div class="slide default-deco" data-slide="17">
  ...Integrasi & Keamanan content...
</div>
```

- [ ] **Step 3.5: Validasi slide count = 16**

```bash
grep -c 'data-slide=' docs/Sosialisasi-PROTON-KPB.html
```
Expected: **16** (sebelum drop = 19, setelah drop 3 = 16).

- [ ] **Step 3.6: Browser smoke test**

Buka file. Navigate dengan arrow keys. Pastikan:
- Tidak ada slide kosong / error
- Nav berhenti di slide ke-16 (terakhir)
- Sequence: 1, 2, 3, 4, 5, 6, 7, **10, 11, 12, 13, 14, 15, 16, 18, 19** (numeric gaps OK untuk sekarang — akan renumber di Task 4)

- [ ] **Step 3.7: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): drop slide #8 #9 #17 — non-PROTON"
```

---

### Task 4: Renumber data-slide + JS update + add empty placeholders for new slides

**Files:**
- Modify: `docs/Sosialisasi-PROTON-KPB.html`

**Mapping renumber + insert placeholder:**

| Old data-slide | New data-slide | Note |
|---|---|---|
| 1 | 1 | Cover (no change) |
| 2 | 2 | Agenda |
| 3 | 3 | Latar Belakang |
| 4 | 4 | Apa Itu HC Portal |
| 5 | 5 | 3 Platform |
| 6 | 6 | Struktur Role |
| 7 | 7 | Sistem Assessment |
| — | **8** | **PLACEHOLDER** Role di PROTON (BARU) |
| 10 | 9 | Assessment PROTON Overview |
| — | **10** | **PLACEHOLDER** Timeline 3 Tahun PROTON (BARU) |
| 13 | 11 | Progresi Kompetensi |
| — | **12** | **PLACEHOLDER** KKJ (BARU) |
| — | **13** | **PLACEHOLDER** Silabus (BARU) |
| — | **14** | **PLACEHOLDER** Coaching Guidance (BARU) |
| 11 | 15 | Alur PROTON T1&2 |
| 12 | 16 | Alur PROTON T3 |
| 14 | 17 | Alur Coaching 9-step T1&2 |
| 15 | 18 | Alur Coaching Mahir T3 |
| — | **19** | **PLACEHOLDER** Dashboard Tracking PROTON (BARU) |
| 16 | 20 | IDP & Training Records |
| 18 | 21 | Cara Mengakses |
| 19 | 22 | Penutup |

- [ ] **Step 4.1: Reorder slide blocks di file**

Untuk reordering: temukan tiap slide block (start dari `<div class="slide ..." data-slide="X">` sampai matching `</div>`), copy ke posisi baru sesuai urutan target, hapus dari posisi lama. Lakukan untuk slide yg pindah posisi:
- Old #13 (Progresi) pindah ke slot 11 (sebelum #11 lama yg sekarang jadi 15)
- Old #16 (IDP) pindah ke slot 20 (sebelum #18 lama yg sekarang jadi 21)
- Slide-slide lain udah urut, cuma renumber

**Catatan:** Karena reordering kompleks, alternatif sederhana — pakai pendekatan "renumber in-place, biarkan urutan DOM agak campur, JS akan navigasi sesuai data-slide numeric". Tapi rekomendasi: reorder agar DOM mengikuti slide order (lebih mudah maintain).

Pilih pendekatan: **reorder DOM**. Gunakan editor untuk cut-paste 2 block (Progresi + IDP) ke posisi target.

- [ ] **Step 4.2: Renumber semua `data-slide` attribute sequential 1..22**

Setelah reorder, update tiap `<div class="slide ..." data-slide="X">` agar X = posisi sequential 1..22 sesuai DOM order. Termasuk update badge text `SLIDE X / 19` jadi `SLIDE X / 22`.

Grep validate:
```bash
grep -oE 'data-slide="[0-9]+"' docs/Sosialisasi-PROTON-KPB.html | sort -u
```
Expected (16 numbers — sebelum placeholder ditambah): `data-slide="1"` ... `data-slide="16"`.

- [ ] **Step 4.3: Insert 6 placeholder slide blocks**

Untuk masing-masing slide BARU (#8, #10, #12, #13, #14, #19), insert HTML placeholder di posisi yg benar:

Template placeholder:
```html
<div class="slide default-deco" data-slide="N">
    <div class="slide-header">
      <div>
        <h1 class="slide-title">[PLACEHOLDER Slide N]</h1>
        <p class="slide-subtitle">Konten akan diisi di task berikutnya</p>
      </div>
      <div class="slide-badge">SLIDE N / 22</div>
    </div>
    <div class="slide-body">
      <p style="text-align:center; padding:40px; color:#94a3b8;">⏳ Placeholder — slide #N belum diisi</p>
    </div>
  </div>
```
Ganti `N` dengan 8, 10, 12, 13, 14, 19. Insert di posisi DOM yg benar (di antara slide sebelum & sesudahnya).

Setelah renumber + insert, total slide harus = 22.

- [ ] **Step 4.4: Validate 22 slides + sequential numbering**

```bash
grep -c 'data-slide=' docs/Sosialisasi-PROTON-KPB.html
```
Expected: **22**

```bash
grep -oE 'data-slide="[0-9]+"' docs/Sosialisasi-PROTON-KPB.html | sort -t'"' -k2 -n | uniq
```
Expected: data-slide="1" sampai "22" sequential tanpa skip/dup.

- [ ] **Step 4.5: Update JS `TOTAL_SLIDES` constant**

Grep:
```bash
grep -n "TOTAL_SLIDES\|totalSlides\|total slides\|/ 19" docs/Sosialisasi-PROTON-KPB.html
```
Ubah konstanta JS yg masih `19` (jika ada) → `22`. Cek juga di script block: konstanta atau hard-coded `19` di counter, progress bar, dll.

- [ ] **Step 4.6: Update agenda chip goto IDs**

Slide #2 (Agenda) punya chip clickable yg lompat ke slide tertentu. Update target ID sesuai struktur baru:
- PROLOG → #3
- FONDASI PROTON → #7
- REFERENSI → #12
- ALUR EKSEKUSI → #15
- PENDUKUNG → #20

Lokasi: di slide #2 block, cari `onclick="goTo(...)"` atau `data-target="..."`.

- [ ] **Step 4.7: Update semua slide-badge text dari `X / 19` → `X / 22`**

Replace all:
```bash
grep -c 'SLIDE [0-9]\+ / 19' docs/Sosialisasi-PROTON-KPB.html
```
Jika count > 0, replace setiap badge text. Gunakan sed atau Edit tool replace_all.

```bash
grep -c 'SLIDE [0-9]\+ / 22' docs/Sosialisasi-PROTON-KPB.html
```
Expected: setidaknya 22 (badge per slide).

- [ ] **Step 4.8: Browser smoke test**

Buka file. Klik tiap agenda chip → pastikan lompat ke slide target yg benar. Navigate ←→ → pastikan dari #1 sampai #22 semua terbuka berurutan. Placeholder slide tampil dengan ⏳ icon.

- [ ] **Step 4.9: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "chore(sosialisasi-proton): renumber data-slide + JS TOTAL_SLIDES 22 + agenda goto IDs"
```

---

## GROUP B — Retone Existing Slides (Task 5-9)

### Task 5: Retone slide #3 Latar Belakang

**Files:** Modify slide #3 block.

- [ ] **Step 5.1: Locate slide #3 body**

Grep `data-slide="3"`, baca block.

- [ ] **Step 5.2: Replace body content**

Body content baru:
```html
<div class="slide-body">
  <div style="display:grid; grid-template-columns: 1fr 1fr; gap:24px; align-items:start;">
    <div>
      <h3 style="margin-bottom:12px;">Tantangan</h3>
      <ul>
        <li>Kompetensi pekerja KPB dulu dikelola manual & tersebar di banyak dokumen</li>
        <li>Tracking progresi 3 tahun sulit dilakukan</li>
        <li>Coaching & assessment tidak terstandardisasi antar section</li>
      </ul>
    </div>
    <div>
      <h3 style="margin-bottom:12px;">Jawaban</h3>
      <ul>
        <li><strong>PROTON</strong> — jalur pengembangan terstruktur 3 tahun</li>
        <li><strong>HC Portal</strong> — alat digital pengelola PROTON end-to-end</li>
      </ul>
    </div>
  </div>
  <div style="margin-top:28px; background:linear-gradient(90deg,#dbeafe,#bfdbfe); padding:14px 18px; border-left:4px solid #2563eb; border-radius:6px; font-weight:600;">
    🎯 Tujuan: Panelman & Operator KPB kompeten, terdokumentasi, & tersertifikasi
  </div>
</div>
```

- [ ] **Step 5.3: Browser verify & Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): retone slide #3 latar belakang"
```

---

### Task 6: Ringkas slide #4 Apa Itu HC Portal

**Files:** Modify slide #4 block.

- [ ] **Step 6.1: Replace body — fokus payung definisi (slide #5 nanti handle breakdown)**

```html
<div class="slide-body">
  <p style="font-size:1.15rem; line-height:1.6; max-width:900px;">
    HC Portal adalah <strong>sistem informasi pengembangan kompetensi pekerja</strong> Kilang Pertamina Balikpapan — mengintegrasikan kelola, kembangkan, & dampingi pekerja dalam satu platform.
  </p>
  <div style="display:flex; gap:16px; margin-top:32px;">
    <div style="flex:1; padding:18px; background:#f1f5f9; border-radius:8px; text-align:center;">
      <div style="font-size:1.5rem; font-weight:700; color:#64748b;">CMP</div>
      <div style="font-size:0.85rem; color:#64748b;">Competency Management</div>
    </div>
    <div style="flex:1.3; padding:18px; background:linear-gradient(135deg,#1e3a8a,#1e40af); color:#fff; border-radius:8px; text-align:center; box-shadow:0 8px 20px rgba(30,58,138,0.3);">
      <div style="font-size:1.5rem; font-weight:700;">CDP</div>
      <div style="font-size:0.85rem;">Coaching & Development — rumah PROTON</div>
    </div>
    <div style="flex:1; padding:18px; background:#f1f5f9; border-radius:8px; text-align:center;">
      <div style="font-size:1.5rem; font-weight:700; color:#64748b;">BP</div>
      <div style="font-size:0.85rem; color:#64748b;">Business Process</div>
    </div>
  </div>
  <p style="margin-top:20px; font-style:italic; color:#475569;">Detail per platform di slide berikutnya.</p>
</div>
```

- [ ] **Step 6.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): ringkas slide #4 apa itu HC Portal"
```

---

### Task 7: Retone slide #5 — CDP Highlight

**Files:** Modify slide #5 block.

- [ ] **Step 7.1: Replace body — CDP card dominan**

```html
<div class="slide-body">
  <div style="display:grid; grid-template-columns: 1fr 1.6fr 1fr; gap:20px; align-items:stretch;">
    <div style="padding:20px; background:#f1f5f9; border-radius:10px;">
      <h3 style="margin-top:0;">CMP</h3>
      <p style="font-size:0.9rem; color:#475569;">Competency Management Platform</p>
      <ul style="font-size:0.85rem; padding-left:18px;">
        <li>Assessment engine generic</li>
        <li>Pre/Post test</li>
        <li>Bank soal</li>
      </ul>
    </div>
    <div style="padding:24px; background:linear-gradient(135deg,#1e3a8a,#3b82f6); color:#fff; border-radius:10px; box-shadow:0 10px 24px rgba(30,58,138,0.35);">
      <div style="display:inline-block; background:rgba(255,255,255,0.2); padding:4px 10px; border-radius:12px; font-size:0.75rem; margin-bottom:8px;">⭐ RUMAH PROTON</div>
      <h3 style="margin-top:0; color:#fff;">CDP</h3>
      <p style="font-size:0.95rem;">Coaching & Development Platform</p>
      <ul style="font-size:0.9rem; padding-left:18px;">
        <li><strong>PROTON</strong> — 3 tahun progresi</li>
        <li>Coaching workflow (Coach-Coachee)</li>
        <li>IDP & Training Records</li>
        <li>Histori & Dashboard tracking</li>
        <li>Sertifikasi final</li>
      </ul>
    </div>
    <div style="padding:20px; background:#f1f5f9; border-radius:10px;">
      <h3 style="margin-top:0;">BP</h3>
      <p style="font-size:0.9rem; color:#475569;">Business Process</p>
      <ul style="font-size:0.85rem; padding-left:18px;">
        <li>Workflow operasional</li>
        <li>Tidak masuk scope sosialisasi ini</li>
      </ul>
    </div>
  </div>
</div>
```

- [ ] **Step 7.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): retone slide #5 platform — CDP highlight"
```

---

### Task 8: Retone slide #6 — Struktur Role 4-audience highlight

**Files:** Modify slide #6 block.

- [ ] **Step 8.1: Update subtitle + body**

Subtitle:
```html
<p class="slide-subtitle">6 level role HC Portal — 4 role audience sosialisasi ini di-highlight</p>
```

Body content:
```html
<div class="slide-body">
  <div style="display:grid; grid-template-columns: repeat(6, 1fr); gap:10px;">
    <!-- L1 -->
    <div style="padding:14px 8px; background:#f1f5f9; border-radius:8px; text-align:center; font-size:0.8rem;">
      <div style="font-weight:700;">L1</div>
      <div>Admin</div>
    </div>
    <!-- L2 -->
    <div style="padding:14px 8px; background:#f1f5f9; border-radius:8px; text-align:center; font-size:0.8rem;">
      <div style="font-weight:700;">L2</div>
      <div>HC</div>
    </div>
    <!-- L3 -->
    <div style="padding:14px 8px; background:#f1f5f9; border-radius:8px; text-align:center; font-size:0.8rem;">
      <div style="font-weight:700;">L3</div>
      <div>Direktur · VP · Manager</div>
    </div>
    <!-- L4 HIGHLIGHT -->
    <div style="padding:14px 8px; background:linear-gradient(135deg,#1e3a8a,#3b82f6); color:#fff; border-radius:8px; text-align:center; font-size:0.8rem; box-shadow:0 6px 14px rgba(30,58,138,0.3);">
      <div style="font-weight:700; font-size:0.9rem;">⭐ L4</div>
      <div>Section Head<br>Sr. Supervisor</div>
    </div>
    <!-- L5 HIGHLIGHT -->
    <div style="padding:14px 8px; background:linear-gradient(135deg,#1e3a8a,#3b82f6); color:#fff; border-radius:8px; text-align:center; font-size:0.8rem; box-shadow:0 6px 14px rgba(30,58,138,0.3);">
      <div style="font-weight:700; font-size:0.9rem;">⭐ L5</div>
      <div>Coach</div>
    </div>
    <!-- L6 HIGHLIGHT -->
    <div style="padding:14px 8px; background:linear-gradient(135deg,#1e3a8a,#3b82f6); color:#fff; border-radius:8px; text-align:center; font-size:0.8rem; box-shadow:0 6px 14px rgba(30,58,138,0.3);">
      <div style="font-weight:700; font-size:0.9rem;">⭐ L6</div>
      <div>Coachee</div>
    </div>
  </div>
  <div style="margin-top:28px; padding:16px; background:#fef3c7; border-left:4px solid #f59e0b; border-radius:6px; font-size:0.92rem;">
    💡 <strong>Slide ini = peta akses role di HC Portal.</strong> Slide #8 nanti membahas role spesifik di alur kerja PROTON.
  </div>
</div>
```

- [ ] **Step 8.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): retone slide #6 role — 4 audience highlight"
```

---

### Task 9: Flip slide #7 — PROTON Pilar

**Files:** Modify slide #7 block.

- [ ] **Step 9.1: Update slide title + body**

Title:
```html
<h1 class="slide-title">Sistem <span class="accent">Assessment</span> — PROTON Pilar Utama</h1>
<p class="slide-subtitle">PROTON menjadi pondasi utama assessment kompetensi di KPB</p>
```

Body content:
```html
<div class="slide-body">
  <div style="display:grid; grid-template-columns: 3fr 1fr; gap:24px; align-items:stretch;">
    <!-- PROTON box BESAR -->
    <div style="padding:32px; background:linear-gradient(135deg,#1e3a8a,#3b82f6); color:#fff; border-radius:12px; box-shadow:0 12px 28px rgba(30,58,138,0.4);">
      <div style="display:inline-block; background:rgba(255,255,255,0.2); padding:4px 12px; border-radius:14px; font-size:0.75rem; margin-bottom:14px;">⭐ PILAR UTAMA · ~75% BOBOT</div>
      <h2 style="margin:0 0 12px 0; font-size:2rem; color:#fff;">PROTON</h2>
      <p style="margin:0 0 16px 0; font-size:1rem;">Program 3 tahun untuk Panelman & Operator KPB</p>
      <ul style="font-size:0.95rem; padding-left:20px;">
        <li>Assessment + Coaching + Sertifikasi terintegrasi</li>
        <li>Tahun 1 & 2: ujian online + coaching</li>
        <li>Tahun 3: interview Mahir + final assessment</li>
      </ul>
    </div>
    <!-- Pre/Post box KECIL -->
    <div style="padding:18px; background:#f1f5f9; border-radius:10px; align-self:center;">
      <div style="font-size:0.7rem; color:#64748b; margin-bottom:6px;">Pelengkap · ~25%</div>
      <h4 style="margin:0 0 8px 0; font-size:1.1rem;">Pre / Post Test</h4>
      <ul style="font-size:0.82rem; padding-left:18px; color:#475569;">
        <li>Assessment sebelum/sesudah training</li>
        <li>Tidak masuk jalur sertifikasi PROTON</li>
      </ul>
    </div>
  </div>
</div>
```

- [ ] **Step 9.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): flip slide #7 assessment — PROTON pilar"
```

---

## GROUP C — PROTON Fondasi (Task 10-13)

### Task 10: New slide #8 Role di PROTON

**Files:** Replace placeholder slide #8.

- [ ] **Step 10.1: Replace placeholder #8 dengan konten**

Cari `<div class="slide default-deco" data-slide="8">` (yg ⏳ placeholder), replace seluruh slide block dengan:
```html
<div class="slide default-deco" data-slide="8">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">PROTON FONDASI · ROLE</p>
        <h1 class="slide-title">Role di <span class="accent">PROTON</span></h1>
        <p class="slide-subtitle">Siapa eksekusi step apa di alur kerja PROTON</p>
      </div>
      <div class="slide-badge">SLIDE 8 / 22</div>
    </div>
    <div class="slide-body">
      <div style="display:grid; grid-template-columns: repeat(4, 1fr); gap:14px;">
        <!-- Coachee -->
        <div style="padding:18px; background:#fef3c7; border-radius:10px; border-top:4px solid #f59e0b;">
          <div style="font-size:0.7rem; color:#78350f; margin-bottom:6px;">L6 · EKSEKUTOR</div>
          <h4 style="margin:0 0 10px 0;">Coachee</h4>
          <ul style="font-size:0.82rem; padding-left:18px; margin:0;">
            <li>Ikut ujian online (T1&2)</li>
            <li>Kerjakan deliverable</li>
            <li>Upload evidence</li>
            <li>Ikut interview Mahir (T3)</li>
          </ul>
        </div>
        <!-- Coach -->
        <div style="padding:18px; background:#dbeafe; border-radius:10px; border-top:4px solid #2563eb;">
          <div style="font-size:0.7rem; color:#1e3a8a; margin-bottom:6px;">L5 · PENDAMPING</div>
          <h4 style="margin:0 0 10px 0;">Coach</h4>
          <ul style="font-size:0.82rem; padding-left:18px; margin:0;">
            <li>Sampaikan silabus</li>
            <li>Sesi coaching reguler</li>
            <li>Validasi evidence</li>
            <li>Catat hasil sesi</li>
          </ul>
        </div>
        <!-- Sr. Supervisor -->
        <div style="padding:18px; background:#d1fae5; border-radius:10px; border-top:4px solid #059669;">
          <div style="font-size:0.7rem; color:#064e3b; margin-bottom:6px;">L4 · APPROVER</div>
          <h4 style="margin:0 0 10px 0;">Sr. Supervisor</h4>
          <ul style="font-size:0.82rem; padding-left:18px; margin:0;">
            <li>Approve deliverable</li>
            <li><code>SrSpvApprovalStatus</code></li>
            <li>Mapping Coach-Coachee</li>
            <li>Monitoring section</li>
          </ul>
        </div>
        <!-- Section Head -->
        <div style="padding:18px; background:#e9d5ff; border-radius:10px; border-top:4px solid #7c3aed;">
          <div style="font-size:0.7rem; color:#4c1d95; margin-bottom:6px;">L4 · GOVERNANCE</div>
          <h4 style="margin:0 0 10px 0;">Section Head</h4>
          <ul style="font-size:0.82rem; padding-left:18px; margin:0;">
            <li>Overview section</li>
            <li>Approve final</li>
            <li>Eskalasi & decision</li>
            <li>Review dashboard</li>
          </ul>
        </div>
      </div>
      <div style="margin-top:20px; padding:14px; background:#f1f5f9; border-radius:8px; font-size:0.85rem; color:#475569;">
        <strong>Pendukung:</strong> <em>HC</em> review final + sertifikasi · <em>Admin</em> kelola data master (Silabus, KKJ, Guidance)
      </div>
    </div>
  </div>
```

- [ ] **Step 10.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "feat(sosialisasi-proton): new slide #8 role di PROTON"
```

---

### Task 11: Expand slide #9 Assessment PROTON Overview

**Files:** Modify slide #9 (was old #10).

- [ ] **Step 11.1: Replace body**

Title (jika perlu retone):
```html
<h1 class="slide-title">Assessment <span class="accent">PROTON</span> — Overview</h1>
<p class="slide-subtitle">3 tahun, 2 jenis assessment, 1 sertifikasi Mahir</p>
```

Body:
```html
<div class="slide-body">
  <div style="display:grid; grid-template-columns: repeat(3, 1fr); gap:18px; align-items:stretch;">
    <!-- Tahun 1 -->
    <div style="padding:24px; background:#f1f5f9; border-radius:12px; border-top:6px solid #3b82f6;">
      <div style="font-size:0.75rem; color:#64748b;">TAHUN 1</div>
      <h3 style="margin:6px 0;">Ujian Online</h3>
      <p style="font-size:0.88rem; color:#475569; margin-bottom:10px;">Pilihan ganda per Kompetensi</p>
      <ul style="font-size:0.82rem; padding-left:18px;">
        <li>5 dimensi Kompetensi</li>
        <li>Auto-grading sistem</li>
        <li>Output: sertif per kompetensi</li>
      </ul>
    </div>
    <!-- Tahun 2 -->
    <div style="padding:24px; background:#f1f5f9; border-radius:12px; border-top:6px solid #3b82f6;">
      <div style="font-size:0.75rem; color:#64748b;">TAHUN 2</div>
      <h3 style="margin:6px 0;">Ujian Online + Coaching</h3>
      <p style="font-size:0.88rem; color:#475569; margin-bottom:10px;">Lanjutan kedalaman kompetensi</p>
      <ul style="font-size:0.82rem; padding-left:18px;">
        <li>Soal lebih kompleks dari T1</li>
        <li>Coaching mendalam</li>
        <li>Output: progresi sertif</li>
      </ul>
    </div>
    <!-- Tahun 3 MAHIR -->
    <div style="padding:24px; background:linear-gradient(135deg,#fef3c7,#fde68a); border-radius:12px; border-top:6px solid #f59e0b;">
      <div style="font-size:0.75rem; color:#78350f;">TAHUN 3 · MAHIR</div>
      <h3 style="margin:6px 0;">Interview Offline</h3>
      <p style="font-size:0.88rem; color:#78350f; margin-bottom:10px;">Tatap muka, panel juri</p>
      <ul style="font-size:0.82rem; padding-left:18px;">
        <li>Panel: Section Head + HC + Ahli</li>
        <li>Kriteria mahir per KKJ</li>
        <li>Output: <strong>Sertif Mahir</strong></li>
      </ul>
    </div>
  </div>
</div>
```

- [ ] **Step 11.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): expand slide #9 assessment overview"
```

---

### Task 12: New slide #10 Timeline 3 Tahun PROTON

**Files:** Replace placeholder slide #10.

- [ ] **Step 12.1: Replace placeholder #10**

```html
<div class="slide default-deco" data-slide="10">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">PROTON FONDASI · TIMELINE</p>
        <h1 class="slide-title">Timeline <span class="accent">3 Tahun</span> PROTON</h1>
        <p class="slide-subtitle">Milestone per tahun — silabus, coaching, assessment, sertifikasi</p>
      </div>
      <div class="slide-badge">SLIDE 10 / 22</div>
    </div>
    <div class="slide-body">
      <div style="position:relative; padding:30px 0;">
        <!-- Timeline line -->
        <div style="position:absolute; top:50%; left:5%; right:5%; height:4px; background:linear-gradient(90deg,#3b82f6,#3b82f6 66%,#f59e0b); transform:translateY(-50%);"></div>
        <!-- Year markers -->
        <div style="display:grid; grid-template-columns: repeat(3, 1fr); gap:30px; position:relative;">
          <!-- Year 1 -->
          <div style="text-align:center;">
            <div style="width:60px; height:60px; margin:0 auto; background:#3b82f6; color:#fff; border-radius:50%; display:flex; align-items:center; justify-content:center; font-weight:700; font-size:1.1rem; box-shadow:0 6px 14px rgba(59,130,246,0.4);">T1</div>
            <div style="margin-top:14px; padding:14px; background:#f1f5f9; border-radius:8px; text-align:left;">
              <div style="font-size:0.75rem; color:#64748b;">BULAN 1-12</div>
              <ul style="font-size:0.82rem; padding-left:18px; margin:6px 0 0 0;">
                <li>Silabus dibagikan</li>
                <li>Coaching reguler</li>
                <li>Evidence per deliverable</li>
                <li>Ujian Online akhir tahun</li>
              </ul>
            </div>
          </div>
          <!-- Year 2 -->
          <div style="text-align:center;">
            <div style="width:60px; height:60px; margin:0 auto; background:#3b82f6; color:#fff; border-radius:50%; display:flex; align-items:center; justify-content:center; font-weight:700; font-size:1.1rem; box-shadow:0 6px 14px rgba(59,130,246,0.4);">T2</div>
            <div style="margin-top:14px; padding:14px; background:#f1f5f9; border-radius:8px; text-align:left;">
              <div style="font-size:0.75rem; color:#64748b;">BULAN 13-24</div>
              <ul style="font-size:0.82rem; padding-left:18px; margin:6px 0 0 0;">
                <li>Silabus lanjutan</li>
                <li>Coaching mendalam</li>
                <li>Evidence progressif</li>
                <li>Ujian Online akhir tahun</li>
              </ul>
            </div>
          </div>
          <!-- Year 3 MAHIR -->
          <div style="text-align:center;">
            <div style="width:60px; height:60px; margin:0 auto; background:#f59e0b; color:#fff; border-radius:50%; display:flex; align-items:center; justify-content:center; font-weight:700; font-size:1.1rem; box-shadow:0 6px 14px rgba(245,158,11,0.4);">T3</div>
            <div style="margin-top:14px; padding:14px; background:linear-gradient(135deg,#fef3c7,#fde68a); border-radius:8px; text-align:left;">
              <div style="font-size:0.75rem; color:#78350f;">BULAN 25-36 · MAHIR</div>
              <ul style="font-size:0.82rem; padding-left:18px; margin:6px 0 0 0;">
                <li>Silabus Mahir</li>
                <li>Coaching intensif</li>
                <li>Final Assessment HC</li>
                <li><strong>Interview Panel + Sertif Mahir</strong></li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
```

- [ ] **Step 12.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "feat(sosialisasi-proton): new slide #10 timeline 3 tahun"
```

---

### Task 13: Expand slide #11 Progresi Kompetensi

**Files:** Modify slide #11 (was old #13).

- [ ] **Step 13.1: Replace body — tabel 5 aspek × 3 tahun dengan visual bar**

Body:
```html
<div class="slide-body">
  <table style="width:100%; border-collapse:collapse; font-size:0.88rem;">
    <thead>
      <tr style="background:#1e3a8a; color:#fff;">
        <th style="padding:10px 14px; text-align:left; width:25%;">Aspek</th>
        <th style="padding:10px 14px; text-align:center;">Tahun 1</th>
        <th style="padding:10px 14px; text-align:center;">Tahun 2</th>
        <th style="padding:10px 14px; text-align:center;">Tahun 3 · Mahir</th>
      </tr>
    </thead>
    <tbody>
      <tr style="background:#f8fafc;">
        <td style="padding:10px 14px; font-weight:600;">📚 Pengetahuan</td>
        <td style="padding:10px 14px; text-align:center;">Dasar konsep</td>
        <td style="padding:10px 14px; text-align:center;">Pendalaman teori</td>
        <td style="padding:10px 14px; text-align:center; background:#fef3c7; font-weight:600;">Sintesis & evaluasi</td>
      </tr>
      <tr>
        <td style="padding:10px 14px; font-weight:600;">🔧 Keterampilan</td>
        <td style="padding:10px 14px; text-align:center;">Operasi standar</td>
        <td style="padding:10px 14px; text-align:center;">Troubleshooting</td>
        <td style="padding:10px 14px; text-align:center; background:#fef3c7; font-weight:600;">Optimasi sistem</td>
      </tr>
      <tr style="background:#f8fafc;">
        <td style="padding:10px 14px; font-weight:600;">⚙️ Aplikasi</td>
        <td style="padding:10px 14px; text-align:center;">Tugas terbimbing</td>
        <td style="padding:10px 14px; text-align:center;">Tugas mandiri</td>
        <td style="padding:10px 14px; text-align:center; background:#fef3c7; font-weight:600;">Pengambilan keputusan</td>
      </tr>
      <tr>
        <td style="padding:10px 14px; font-weight:600;">🧩 Kompleksitas</td>
        <td style="padding:10px 14px; text-align:center;">Skenario sederhana</td>
        <td style="padding:10px 14px; text-align:center;">Multi-variabel</td>
        <td style="padding:10px 14px; text-align:center; background:#fef3c7; font-weight:600;">Sistem terintegrasi</td>
      </tr>
      <tr style="background:#f8fafc;">
        <td style="padding:10px 14px; font-weight:600;">🎯 Otonomi</td>
        <td style="padding:10px 14px; text-align:center;">Diawasi penuh</td>
        <td style="padding:10px 14px; text-align:center;">Konsultatif</td>
        <td style="padding:10px 14px; text-align:center; background:#fef3c7; font-weight:600;">Mandiri & coaching others</td>
      </tr>
    </tbody>
  </table>
</div>
```

- [ ] **Step 13.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): expand slide #11 progresi kompetensi"
```

---

## GROUP D — Referensi Dokumen Demo (Task 14-16)

### Task 14: New slide #12 KKJ — Template C demo

**Files:** Replace placeholder slide #12.

- [ ] **Step 14.1: Replace placeholder #12**

```html
<div class="slide default-deco" data-slide="12">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">REFERENSI · DOKUMEN DASAR</p>
        <h1 class="slide-title">KKJ — <span class="accent">Kebutuhan Kompetensi</span> Jabatan</h1>
        <p class="slide-subtitle">Peta kompetensi target per jabatan — dasar acuan PROTON</p>
      </div>
      <div class="slide-badge">SLIDE 12 / 22</div>
    </div>
    <div class="slide-body demo-split">
      <div>
        <div class="demo-image-frame">
          📷 Screenshot akan ditambah<br>
          <small>SS KPB_KKJ Fungsi System Completion &amp; Simops</small>
        </div>
        <div class="demo-image-caption">Sumber: docs/doc support Proton/SS KPB_KKJ Fungsi System Completion &amp; Simops.pdf</div>
      </div>
      <div class="demo-text-col">
        <p class="demo-lead">KKJ = peta kompetensi per jabatan</p>
        <ul class="demo-bullets">
          <li>Sumber dasar acuan PROTON</li>
          <li>Per fungsi (System Completion, Simops, dll)</li>
          <li>Mapping ke level kompetensi target</li>
          <li>Dasar penyusunan silabus</li>
        </ul>
        <div class="demo-takeaway">Tanpa KKJ, PROTON tidak punya target</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 14.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "feat(sosialisasi-proton): new slide #12 KKJ — Template C demo"
```

---

### Task 15: New slide #13 Silabus PROTON — Template C demo

- [ ] **Step 15.1: Replace placeholder #13**

```html
<div class="slide default-deco" data-slide="13">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">REFERENSI · SILABUS</p>
        <h1 class="slide-title">Silabus <span class="accent">PROTON</span></h1>
        <p class="slide-subtitle">Penjabaran KKJ jadi deliverable per dimensi kompetensi</p>
      </div>
      <div class="slide-badge">SLIDE 13 / 22</div>
    </div>
    <div class="slide-body demo-split">
      <div>
        <div class="demo-image-frame">
          📷 Screenshot akan ditambah<br>
          <small>Folder silabus — Operator + Panelman Kompetensi 1-5</small>
        </div>
        <div class="demo-image-caption">Sumber: docs/doc support Proton/silabus/ (10 file .docx)</div>
      </div>
      <div class="demo-text-col">
        <p class="demo-lead">Silabus = jabarkan KKJ jadi deliverable konkret</p>
        <ul class="demo-bullets">
          <li>2 track: <strong>Panelman</strong> &amp; <strong>Operator</strong></li>
          <li>5 dimensi Kompetensi per track (Kompetensi 1-5)</li>
          <li>Hierarki: Kompetensi → SubKompetensi → Deliverable</li>
          <li>Diinput &amp; dikelola Admin/HC via Silabus Manager</li>
        </ul>
        <div class="demo-takeaway">Silabus = peta kerja Coach + Coachee</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 15.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "feat(sosialisasi-proton): new slide #13 Silabus — Template C demo"
```

---

### Task 16: New slide #14 Coaching Guidance — Template C demo

- [ ] **Step 16.1: Replace placeholder #14**

```html
<div class="slide default-deco" data-slide="14">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">REFERENSI · PEDOMAN COACH</p>
        <h1 class="slide-title">Coaching <span class="accent">Guidance</span></h1>
        <p class="slide-subtitle">Pedoman materi coaching per dimensi kompetensi</p>
      </div>
      <div class="slide-badge">SLIDE 14 / 22</div>
    </div>
    <div class="slide-body demo-split">
      <div>
        <div class="demo-image-frame">
          📷 Screenshot akan ditambah<br>
          <small>Coaching Guidance RFCC NHT Dimensi 1-5</small>
        </div>
        <div class="demo-image-caption">Sumber: docs/doc support Proton/coaching guidance/ (10 file .docx)</div>
      </div>
      <div class="demo-text-col">
        <p class="demo-lead">Pedoman materi coaching per dimensi</p>
        <ul class="demo-bullets">
          <li>5 Dimensi × 2 track (Operator + Panelman) = 10 dokumen</li>
          <li>Rujukan Coach saat sesi coaching</li>
          <li>Diakses via portal (entity <code>CoachingGuidanceFile</code>)</li>
          <li>Update ikut revisi KKJ</li>
        </ul>
        <div class="demo-takeaway">Coach tidak coaching tanpa pedoman</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 16.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "feat(sosialisasi-proton): new slide #14 Coaching Guidance — Template C demo"
```

---

## GROUP E — Alur Eksekusi Demo (Task 17-21)

### Task 17: Slide #15 Alur PROTON T1&2 → Template C

**Files:** Modify slide #15 (was old #11). Convert from full-width ke Template C demo.

- [ ] **Step 17.1: Replace slide body dengan Template C demo-split**

```html
<div class="slide default-deco" data-slide="15">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">ALUR EKSEKUSI · TAHUN 1 &amp; 2</p>
        <h1 class="slide-title">Alur PROTON — Tahun <span class="accent">1 &amp; 2</span></h1>
        <p class="slide-subtitle">Ujian Online Pilihan Ganda</p>
      </div>
      <div class="slide-badge">SLIDE 15 / 22</div>
    </div>
    <div class="slide-body demo-split">
      <div>
        <div class="demo-image-frame">
          📷 Screenshot akan ditambah<br>
          <small>Tampilan ujian online PROTON di portal</small>
        </div>
        <div class="demo-image-caption">TBD source: Views/CMP/Assessment.cshtml atau Views/CDP/PlanIdp.cshtml — pilih view yg paling representatif saat capture</div>
      </div>
      <div class="demo-text-col">
        <p class="demo-lead">Ujian online pilihan ganda per Kompetensi</p>
        <ul class="demo-bullets">
          <li>Login portal → pilih ujian aktif</li>
          <li>Kerjakan soal pilihan ganda</li>
          <li>Submit jawaban → grading otomatis</li>
          <li>Hasil tampil langsung + sertif per kompetensi</li>
          <li>Akumulasi sertif jadi syarat naik ke T3</li>
        </ul>
        <div class="demo-takeaway">Lulus T1 &amp; T2 → syarat naik ke T3 (Mahir)</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 17.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): slide #15 alur T1&2 → Template C demo"
```

---

### Task 18: Slide #16 Alur PROTON T3 → Template C

- [ ] **Step 18.1: Replace slide body dengan Template C**

```html
<div class="slide default-deco" data-slide="16">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">ALUR EKSEKUSI · TAHUN 3 MAHIR</p>
        <h1 class="slide-title">Alur PROTON — Tahun <span class="accent" style="color:var(--amber);">3</span></h1>
        <p class="slide-subtitle">Interview Offline · Tatap Muka · Panel Juri</p>
      </div>
      <div class="slide-badge" style="background:var(--amber);">🎤 OFFLINE MODE</div>
    </div>
    <div class="slide-body demo-split">
      <div>
        <div class="demo-image-frame">
          📷 Screenshot akan ditambah<br>
          <small>EditCoachingSession / panel interview Mahir</small>
        </div>
        <div class="demo-image-caption">Sumber: Views/CDP/EditCoachingSession.cshtml (form interview + scoring)</div>
      </div>
      <div class="demo-text-col">
        <p class="demo-lead">Penilaian Mahir via wawancara panel</p>
        <ul class="demo-bullets">
          <li>Panel juri: <strong>Section Head + HC + Ahli</strong></li>
          <li>Format: tanya-jawab tatap muka</li>
          <li>Kriteria mahir berdasar KKJ</li>
          <li>Skoring per dimensi kompetensi</li>
          <li>Output: sertifikasi final Mahir</li>
        </ul>
        <div class="demo-takeaway">T3 = pintu sertifikasi Mahir</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 18.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): slide #16 alur T3 → Template C demo"
```

---

### Task 19: Slide #17 Alur Coaching 9-step T1&2 → Template C

- [ ] **Step 19.1: Replace slide body dengan Template C**

```html
<div class="slide default-deco" data-slide="17">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">ALUR EKSEKUSI · COACHING T1&amp;2</p>
        <h1 class="slide-title">Alur <span class="accent">Coaching</span> — 9 Step</h1>
        <p class="slide-subtitle">Pendampingan Coachee oleh Coach</p>
      </div>
      <div class="slide-badge">SLIDE 17 / 22</div>
    </div>
    <div class="slide-body demo-split">
      <div>
        <div class="demo-image-frame">
          📷 Screenshot akan ditambah<br>
          <small>Views/CDP/CoachingProton.cshtml — form Coach</small>
        </div>
        <div class="demo-image-caption">Sumber: form Coach (Result · Kesimpulan · AcuanPedoman)</div>
      </div>
      <div class="demo-text-col">
        <p class="demo-lead">Coaching = jantung PROTON, bukan formalitas</p>
        <ul class="demo-bullets">
          <li>1. Silabus dibagikan ke Coachee</li>
          <li>2. Assign Coach-Coachee oleh Sr.Supervisor</li>
          <li>3. Sesi coaching reguler + isi catatan</li>
          <li>4. Coachee submit evidence per deliverable</li>
          <li>5. Coach validasi → Sr.Supervisor approve</li>
          <li>6. HC review → 7. Approve final</li>
          <li>8. Lanjut deliverable berikutnya</li>
          <li>9. Akumulasi sertif kompetensi</li>
        </ul>
        <div class="demo-takeaway">9 step = siklus per deliverable</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 19.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): slide #17 coaching 9-step → Template C demo"
```

---

### Task 20: Slide #18 Alur Coaching Mahir T3 → Template C

- [ ] **Step 20.1: Replace slide body dengan Template C**

```html
<div class="slide default-deco" data-slide="18">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">ALUR EKSEKUSI · COACHING MAHIR T3</p>
        <h1 class="slide-title">Alur Coaching <span class="accent" style="color:var(--amber);">Mahir</span> — 9 Step</h1>
        <p class="slide-subtitle">Coaching level Mahir dengan kriteria lebih tegas</p>
      </div>
      <div class="slide-badge" style="background:var(--amber);">🎯 LEVEL MAHIR</div>
    </div>
    <div class="slide-body demo-split">
      <div>
        <div class="demo-image-frame">
          📷 Screenshot akan ditambah<br>
          <small>Views/CDP/Shared/_CoachingProtonContentPartial.cshtml</small>
        </div>
        <div class="demo-image-caption">Sumber: form Coach level Mahir + Final Assessment</div>
      </div>
      <div class="demo-text-col">
        <p class="demo-lead">Coach Mahir = kunci kualitas sertifikasi</p>
        <ul class="demo-bullets">
          <li>1. Silabus Mahir dibagikan</li>
          <li>2. Assign Coach senior</li>
          <li>3. Coaching intensif (frequency lebih tinggi)</li>
          <li>4. Evidence + Final Assessment HC</li>
          <li>5-6. Review berjenjang (SrSpv + HC)</li>
          <li>7. Interview Panel (Section Head + Ahli)</li>
          <li>8. Skoring per dimensi KKJ</li>
          <li>9. <strong>Sertif Mahir final</strong></li>
        </ul>
        <div class="demo-takeaway">Mahir bukan otomatis — wajib interview panel</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 20.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): slide #18 coaching mahir → Template C demo"
```

---

### Task 21: New slide #19 Dashboard Tracking PROTON — Template C

- [ ] **Step 21.1: Replace placeholder #19**

```html
<div class="slide default-deco" data-slide="19">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">ALUR EKSEKUSI · MONITORING</p>
        <h1 class="slide-title">Dashboard <span class="accent">Tracking</span> PROTON</h1>
        <p class="slide-subtitle">Visibility untuk Section Head &amp; Sr. Supervisor</p>
      </div>
      <div class="slide-badge">SLIDE 19 / 22</div>
    </div>
    <div class="slide-body demo-split">
      <div>
        <div class="demo-image-frame">
          📷 Screenshot akan ditambah<br>
          <small>Views/CDP/HistoriProton.cshtml / Dashboard.cshtml</small>
        </div>
        <div class="demo-image-caption">Sumber: tampilan tracking + audit trail</div>
      </div>
      <div class="demo-text-col">
        <p class="demo-lead">Tool monitoring untuk Section Head &amp; Sr. Supervisor</p>
        <ul class="demo-bullets">
          <li>Filter per section / track / tahun</li>
          <li>Status progress per coachee real-time</li>
          <li>Audit trail (<code>DeliverableStatusHistory</code>)</li>
          <li>Drill-down ke detail per deliverable</li>
          <li>Export Excel / PDF untuk reporting</li>
        </ul>
        <div class="demo-takeaway">Dashboard = visibility manajemen lini</div>
      </div>
    </div>
  </div>
```

- [ ] **Step 21.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "feat(sosialisasi-proton): new slide #19 dashboard tracking — Template C demo"
```

---

## GROUP F — Pendukung & Audit (Task 22-25)

### Task 22: Slide #20 IDP appendix ringkas

**Files:** Modify slide #20 (was old #16).

- [ ] **Step 22.1: Ringkas slide #20 — 1 slide compact**

```html
<div class="slide default-deco" data-slide="20">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">PENDUKUNG · APPENDIX</p>
        <h1 class="slide-title">IDP &amp; <span class="accent">Training Records</span></h1>
        <p class="slide-subtitle">Rencana karir &amp; riwayat pelatihan terdokumentasi</p>
      </div>
      <div class="slide-badge">SLIDE 20 / 22</div>
    </div>
    <div class="slide-body">
      <div style="display:grid; grid-template-columns: 1fr 1fr; gap:24px; align-items:start;">
        <div style="padding:18px; background:#f1f5f9; border-radius:10px;">
          <h4 style="margin-top:0;">IDP — Individual Development Plan</h4>
          <ul style="font-size:0.88rem;">
            <li>Rencana karir pekerja jangka menengah</li>
            <li>Target kompetensi per tahun</li>
            <li>Terhubung dengan deliverable PROTON</li>
          </ul>
        </div>
        <div style="padding:18px; background:#f1f5f9; border-radius:10px;">
          <h4 style="margin-top:0;">Training Records</h4>
          <ul style="font-size:0.88rem;">
            <li>Riwayat pelatihan internal &amp; eksternal</li>
            <li>Sertifikasi diterima</li>
            <li>Deliverable PROTON masuk otomatis</li>
          </ul>
        </div>
      </div>
      <div style="margin-top:20px; padding:14px; background:#dbeafe; border-left:4px solid #2563eb; border-radius:6px; font-size:0.88rem;">
        🔗 IDP &amp; Training Records = ekosistem pendukung PROTON. Setiap progres PROTON tercatat otomatis di sini.
      </div>
    </div>
  </div>
```

- [ ] **Step 22.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): slide #20 IDP appendix ringkas"
```

---

### Task 23: Slide #21 Cara Mengakses

**Files:** Modify slide #21 (was old #18).

- [ ] **Step 23.1: Update body**

```html
<div class="slide default-deco" data-slide="21">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">PENDUKUNG · AKSES</p>
        <h1 class="slide-title">Cara <span class="accent">Mengakses</span> HC Portal</h1>
        <p class="slide-subtitle">Saat ini fase Development — Production akan menyusul</p>
      </div>
      <div class="slide-badge">SLIDE 21 / 22</div>
    </div>
    <div class="slide-body">
      <div style="display:grid; grid-template-columns: repeat(3, 1fr); gap:18px;">
        <div style="padding:18px; background:#f1f5f9; border-radius:10px;">
          <div style="font-size:0.7rem; color:#64748b;">URL DEV</div>
          <h4 style="margin:6px 0;">🌐 Alamat</h4>
          <code style="background:#fff; padding:6px 10px; border-radius:4px; display:block; word-break:break-all;">http://10.55.3.3/KPB-PortalHC</code>
          <p style="font-size:0.78rem; color:#64748b; margin-top:8px;">Hanya bisa diakses dari jaringan internal Pertamina KPB</p>
        </div>
        <div style="padding:18px; background:#f1f5f9; border-radius:10px;">
          <div style="font-size:0.7rem; color:#64748b;">LOGIN</div>
          <h4 style="margin:6px 0;">🔑 Akun</h4>
          <ul style="font-size:0.88rem; padding-left:18px;">
            <li>SSO Pertamina</li>
            <li>atau akun disiapkan HC</li>
          </ul>
          <p style="font-size:0.78rem; color:#64748b;">Hubungi HC kalau belum dapat akses</p>
        </div>
        <div style="padding:18px; background:#f1f5f9; border-radius:10px;">
          <div style="font-size:0.7rem; color:#64748b;">BROWSER</div>
          <h4 style="margin:6px 0;">🌐 Rekomendasi</h4>
          <ul style="font-size:0.88rem; padding-left:18px;">
            <li>Microsoft Edge (latest)</li>
            <li>Google Chrome (latest)</li>
          </ul>
          <p style="font-size:0.78rem; color:#64748b;">Hindari IE11 / browser jadul</p>
        </div>
      </div>
      <div style="margin-top:20px; padding:14px; background:#fef3c7; border-left:4px solid #f59e0b; border-radius:6px; font-size:0.88rem;">
        ⚠️ Fase Development — bug mungkin masih ditemukan. Laporkan ke tim HC untuk perbaikan.
      </div>
    </div>
  </div>
```

- [ ] **Step 23.2: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): slide #21 cara akses"
```

---

### Task 24: Slide #22 Penutup tanpa Q&A

**Files:** Modify slide #22 (was old #19).

- [ ] **Step 24.1: Replace slide block — drop Q&A section**

```html
<div class="slide penutup" data-slide="22">
    <div class="penutup-content">
      <div class="penutup-icon">🙏</div>
      <h1 class="penutup-title">Terima Kasih</h1>
      <p class="penutup-subtitle">Mari kembangkan kompetensi pekerja KPB lewat PROTON</p>
      <div style="margin-top:40px; font-size:0.9rem; opacity:0.8;">
        Section Head · Sr. Supervisor · Coach · Coachee — bersama membangun jalur Mahir
      </div>
    </div>
  </div>
```

**Catatan:** Jangan lupa hapus Q&A block lama (`<div class="penutup-qa">...</div>` dan elemen terkait).

- [ ] **Step 24.2: Validate Q&A dropped**

```bash
grep -c "penutup-qa\|Q&A\|Q\\&amp;A\|Tanya Jawab" docs/Sosialisasi-PROTON-KPB.html
```
Expected: **0** (semua referensi Q&A sudah hilang).

- [ ] **Step 24.3: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "refactor(sosialisasi-proton): slide #22 penutup tanpa Q&A"
```

---

### Task 25: Rewrite agenda #2 + final audit

**Files:** Modify slide #2 + full audit.

- [ ] **Step 25.1: Rewrite slide #2 Agenda dengan struktur baru**

```html
<div class="slide default-deco" data-slide="2">
    <div class="slide-header">
      <div>
        <h1 class="slide-title">Agenda <span class="accent">Sosialisasi</span></h1>
        <p class="slide-subtitle">5 bagian utama — klik untuk lompat</p>
      </div>
      <div class="slide-badge">SLIDE 2 / 22</div>
    </div>
    <div class="slide-body">
      <div style="display:grid; grid-template-columns: repeat(5, 1fr); gap:14px;">
        <div onclick="goTo(3)" style="cursor:pointer; padding:18px; background:#f1f5f9; border-radius:10px; text-align:center; transition:all 0.2s;" onmouseover="this.style.background='#dbeafe'" onmouseout="this.style.background='#f1f5f9'">
          <div style="font-size:0.7rem; color:#64748b;">SLIDE 3-6</div>
          <h4 style="margin:6px 0 4px 0;">PROLOG</h4>
          <p style="font-size:0.78rem; margin:0;">Latar Belakang · HC Portal · Platform · Role</p>
        </div>
        <div onclick="goTo(7)" style="cursor:pointer; padding:18px; background:#f1f5f9; border-radius:10px; text-align:center; transition:all 0.2s;" onmouseover="this.style.background='#dbeafe'" onmouseout="this.style.background='#f1f5f9'">
          <div style="font-size:0.7rem; color:#64748b;">SLIDE 7-11</div>
          <h4 style="margin:6px 0 4px 0;">FONDASI PROTON</h4>
          <p style="font-size:0.78rem; margin:0;">Pilar · Role · Overview · Timeline · Progresi</p>
        </div>
        <div onclick="goTo(12)" style="cursor:pointer; padding:18px; background:#f1f5f9; border-radius:10px; text-align:center; transition:all 0.2s;" onmouseover="this.style.background='#dbeafe'" onmouseout="this.style.background='#f1f5f9'">
          <div style="font-size:0.7rem; color:#64748b;">SLIDE 12-14</div>
          <h4 style="margin:6px 0 4px 0;">REFERENSI</h4>
          <p style="font-size:0.78rem; margin:0;">KKJ · Silabus · Coaching Guidance</p>
        </div>
        <div onclick="goTo(15)" style="cursor:pointer; padding:18px; background:#f1f5f9; border-radius:10px; text-align:center; transition:all 0.2s;" onmouseover="this.style.background='#dbeafe'" onmouseout="this.style.background='#f1f5f9'">
          <div style="font-size:0.7rem; color:#64748b;">SLIDE 15-19</div>
          <h4 style="margin:6px 0 4px 0;">ALUR EKSEKUSI</h4>
          <p style="font-size:0.78rem; margin:0;">T1&amp;2 · T3 · Coaching · Coaching Mahir · Dashboard</p>
        </div>
        <div onclick="goTo(20)" style="cursor:pointer; padding:18px; background:#f1f5f9; border-radius:10px; text-align:center; transition:all 0.2s;" onmouseover="this.style.background='#dbeafe'" onmouseout="this.style.background='#f1f5f9'">
          <div style="font-size:0.7rem; color:#64748b;">SLIDE 20-22</div>
          <h4 style="margin:6px 0 4px 0;">PENDUKUNG</h4>
          <p style="font-size:0.78rem; margin:0;">IDP · Akses · Penutup</p>
        </div>
      </div>
    </div>
  </div>
```

**Catatan:** Pastikan function `goTo(N)` ada di JS block. Jika namanya beda (misal `navigateTo`, `showSlide`), sesuaikan.

- [ ] **Step 25.2: Audit grep — pastikan branding clean**

```bash
grep -i "CPDP\|Umum.*Pekerja\|Untuk Pekerja Pertamina" docs/Sosialisasi-PROTON-KPB.html
```
Expected: **no output** atau hanya match yg sengaja kept (review manual).

- [ ] **Step 25.3: Audit grep — pastikan badge konsisten**

```bash
grep -oE 'SLIDE [0-9]+ / [0-9]+' docs/Sosialisasi-PROTON-KPB.html | sort -u
```
Expected: hanya `SLIDE N / 22` untuk N=1..22 (atau badge khusus seperti `OFFLINE MODE`, `LEVEL MAHIR`).

- [ ] **Step 25.4: Audit data-slide sequential**

```bash
grep -oE 'data-slide="[0-9]+"' docs/Sosialisasi-PROTON-KPB.html | sort -t'"' -k2 -n | uniq -c | sort -rn | head
```
Expected: setiap data-slide muncul exactly 1x, sequential 1..22.

- [ ] **Step 25.5: Browser final smoke test full pass**

Buka file. Test:
- Klik tiap agenda chip → lompat ke slide target benar
- Navigate ←→ dari #1 sampai #22 → semua tampil
- Toggle dark mode → semua slide (terutama Template C demo) render benar
- Cek Template C demo slide #12-19 → frame placeholder visible, bullet ▸ render, takeaway box amber

- [ ] **Step 25.6: Commit**

```bash
git add docs/Sosialisasi-PROTON-KPB.html
git commit -m "chore(sosialisasi-proton): rewrite agenda #2 + audit final"
```

---

## Post-Implementation Checklist (User Action)

Setelah 25 commit selesai, user harus melakukan:

- [ ] **Capture & paste screenshot:**
  - Slide #12 KKJ → screenshot dari `SS KPB_KKJ Fungsi System Completion & Simops.pdf`
  - Slide #13 Silabus → screenshot folder/dokumen Silabus
  - Slide #14 Coaching Guidance → screenshot dokumen RFCC NHT
  - Slide #15-19 → screenshot portal sesuai source TBD note
  - Ganti `<div class="demo-image-frame">...</div>` jadi `<div class="demo-image-frame"><img src="..." alt="..."></div>`

- [ ] **Isi tanggal acara** di cover meta `[TBD tanggal acara]`

- [ ] **Review konten per slide** — typo, terminology, fakta

- [ ] **Print test PDF browser** (Ctrl+P → Save as PDF, Landscape A4)

- [ ] **Push branch:**
  ```bash
  git push -u origin feat/sosialisasi-proton-deck
  ```

- [ ] **Pulihkan stash WIP `pcp-slide8`:**
  ```bash
  git checkout pcp-slide8-versi-p
  git stash pop
  ```
