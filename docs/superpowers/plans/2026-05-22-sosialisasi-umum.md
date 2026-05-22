# Sosialisasi Umum PortalHC KPB Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (22 slide, audiens Tim HC) menjadi `docs/Sosialisasi-Umum-PortalHC-KPB.html` (19 slide, audiens pekerja Pertamina umum non-HC) via rename + cut 3 slide + retone 8 slide.

**Architecture:** Single-file HTML edit. Rename via `git mv` (preserve history). Cut 3 DOM blocks (slide 10 Pre/Post, 14 Coaching Dual Track, 15 Hierarki). Reorder DOM ke linear sequence. Renumber `data-slide` 1..19 + standard `slide-badge` + JS `const total`. Retone copy 8 slide tanpa overhaul CSS / layout. Flow slide (9, 12, 13, 17, 18 original = Alur Assessment + Proton + Coaching) zero edit body content.

**Tech Stack:** HTML5 + vanilla CSS + vanilla JS (no framework). Single static HTML file. Browser navigation via keyboard + JS `showSlide()`. File served lokal via dev server (`dotnet run` di `http://localhost:5277`) atau static file open.

**Spec reference:** `docs/superpowers/specs/2026-05-22-sosialisasi-umum-design.md`

---

## File Structure

| File | Change |
|---|---|
| `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` | RENAME → `docs/Sosialisasi-Umum-PortalHC-KPB.html` |
| `docs/Sosialisasi-Umum-PortalHC-KPB.html` (after rename) | Edit in-place: cut 3 slide, retone 8 slide, renumber 18 badge + JS const + totalNum |

**No other file touched.** Spec dan plan ini sendiri tak diubah (sudah committed).

---

## Renumber Map (untuk referensi semua task)

Original `data-slide` → New `data-slide`:

| Original | New | Slide |
|---|---|---|
| 1 | 1 | Cover |
| 2 | 2 | Agenda |
| 3 | 3 | Latar Belakang |
| 4 | 4 | Apa Itu HC Portal |
| 5 | 5 | 3 Platform |
| 6 | 6 | Role |
| 7 | 7 | Sistem Assessment |
| 8 | 8 | 5 Kategori |
| 9 | 9 | Alur Assessment 7-step (FLOW) |
| ~~10~~ | CUT | Pre/Post Test |
| 11 | 10 | Assessment Proton |
| 12 | 11 | Alur Proton T1&2 (FLOW) |
| 13 | 12 | Alur Proton T3 (FLOW) |
| ~~14~~ | CUT | Coaching Dual Track |
| ~~15~~ | CUT | Hierarki Kompetensi |
| 16 | 13 | Progresi Kompetensi |
| 17 | 14 | Alur Coaching Pemula (FLOW) |
| 18 | 15 | Alur Coaching Mahir (FLOW) |
| 19 | 16 | IDP & Training Records |
| 20 | 17 | Integrasi & Keamanan |
| 21 | 18 | Cara Mengakses |
| 22 | 19 | Penutup |

**Agenda `goTo()` remap (slide 2):**
- `goTo(3)` → `goTo(3)` (Latar Belakang)
- `goTo(7)` → `goTo(7)` (Sistem Assessment)
- `goTo(11)` → `goTo(10)` (Assessment Proton)
- `goTo(14)` → **REPLACE** target. Coaching Dual Track CUT — redirect ke `goTo(13)` (Progresi Kompetensi) sebagai entry Bagian 3
- `goTo(20)` → `goTo(17)` (Integrasi & Keamanan)
- `goTo(22)` → `goTo(19)` (Penutup)

**Slide 11 (Assessment Proton) internal reference:** body text mention "Slide 16 · Progresi Kompetensi" → update ke "Slide 13 · Progresi Kompetensi".

---

### Task 1: Rename file via `git mv`

**Files:**
- Rename: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` → `docs/Sosialisasi-Umum-PortalHC-KPB.html`

- [ ] **Step 1: Rename file**

```bash
git mv "docs/Sosialisasi-Aplikasi-PortalHC-KPB.html" "docs/Sosialisasi-Umum-PortalHC-KPB.html"
```

- [ ] **Step 2: Verify rename**

```bash
git status
```

Expected output contains:
```
renamed: docs/Sosialisasi-Aplikasi-PortalHC-KPB.html -> docs/Sosialisasi-Umum-PortalHC-KPB.html
```

- [ ] **Step 3: Commit rename only (preserve history detection)**

```bash
git commit -m "$(cat <<'EOF'
chore(sosialisasi-umum): rename Sosialisasi-Aplikasi → Sosialisasi-Umum

Preserve git history via git mv. Edit content in subsequent commits.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 4: Verify history preserved**

```bash
git log --follow --oneline docs/Sosialisasi-Umum-PortalHC-KPB.html | head -5
```

Expected: log shows commits from original `Sosialisasi-Aplikasi-PortalHC-KPB.html` history.

---

### Task 2: Update `<title>` element

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html:6`

- [ ] **Step 1: Edit `<title>`**

Old (line 6):
```html
<title>Sosialisasi Aplikasi HC Portal KPB</title>
```

New:
```html
<title>Sosialisasi Portal HC KPB — Untuk Pekerja Pertamina</title>
```

- [ ] **Step 2: Verify change**

```bash
grep -n "<title>" docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: `6:<title>Sosialisasi Portal HC KPB — Untuk Pekerja Pertamina</title>`

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): update title — Untuk Pekerja Pertamina

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Cut Slide 10 (Pre/Post Test Gain Score)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html:1675-1723`

- [ ] **Step 1: Delete DOM block slide 10 + comment**

Delete entire block from line 1675 (comment) through line 1723 (closing `</div>` of slide + blank line). Block to remove:

```html
  <!-- ================= SLIDE 10 (was 9): PRE & POST TEST GAIN SCORE ================= -->
  <div class="slide default-deco" data-slide="10">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 1 · CMP</p>
        <h1 class="slide-title">Pre &amp; Post Test &mdash; <span class="accent">Gain Score</span></h1>
        <p class="slide-subtitle">Pasangan ujian sebelum &amp; sesudah training untuk hitung peningkatan kompetensi</p>
      </div>
      <div class="slide-badge">SLIDE 10 / 22</div>
    </div>
    <div class="slide-body">
      <div class="pp-stepper">
        <div class="pp-step">
          <div class="pp-num">1</div>
          <span class="pp-icon">📋</span>
          <div class="pp-title">Pre Test</div>
          <div class="pp-desc">Sebelum training, ukur baseline kompetensi peserta</div>
        </div>
        <div class="pp-step">
          <div class="pp-num">2</div>
          <span class="pp-icon">🎓</span>
          <div class="pp-title">Training</div>
          <div class="pp-desc">Sesi pembelajaran in-class atau on-the-job</div>
        </div>
        <div class="pp-step">
          <div class="pp-num">3</div>
          <span class="pp-icon">✅</span>
          <div class="pp-title">Post Test</div>
          <div class="pp-desc">Setelah training, ujian dengan paket soal sejenis</div>
        </div>
        <div class="pp-step final">
          <div class="pp-num">4</div>
          <span class="pp-icon">📊</span>
          <div class="pp-title">Gain Score</div>
          <div class="pp-desc">Analisis selisih skor Post &mdash; Pre</div>
        </div>
      </div>
      <div class="pp-metrics">
        <div class="pp-metric">
          <h5>📈 Gain Score</h5>
          <p>Selisih skor Post &minus; Pre. Indikator efektivitas training per peserta &amp; per kategori.</p>
        </div>
        <div class="pp-metric alt">
          <h5>🔍 Item Analysis</h5>
          <p>Per-soal: tingkat kesulitan, daya beda, distractor power. Bantu HC perbaiki paket soal.</p>
        </div>
      </div>
    </div>
  </div>

```

- [ ] **Step 2: Verify slide 10 gone**

```bash
grep -n 'data-slide="10"' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: no output (slide 10 deleted).

```bash
grep -n 'PRE & POST TEST\|pp-stepper\|pp-metric' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: no output (semua reference Pre/Post DOM gone). CSS untuk `.pp-stepper` masih ada di `<style>` block — tak perlu cleanup (no-op CSS aman).

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): cut slide 10 (Pre/Post Test Gain Score)

Audiens umum tak butuh detail mekanisme Pre/Post. Info absorbed ke
slide 7 tip-bar di task selanjutnya.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Cut Slide 14 (Coaching Proton Dual Track)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (block slide 14 location bergeser setelah Task 3; find by grep)

- [ ] **Step 1: Locate slide 14 block**

```bash
grep -n 'data-slide="14"\|COACHING DUAL TRACK' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: locate `<!-- SLIDE 14: BAGIAN 3 — COACHING DUAL TRACK -->` comment + `<div ... data-slide="14">`.

- [ ] **Step 2: Delete DOM block slide 14 + comment**

Delete entire block:

```html
  <!-- ================= SLIDE 14: BAGIAN 3 — COACHING DUAL TRACK ================= -->
  <div class="slide default-deco" data-slide="14">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 3 · CDP</p>
        <h1 class="slide-title">Coaching <span class="accent">Proton</span> &mdash; Dual Track</h1>
        <p class="slide-subtitle">Program 3 tahun pengembangan kompetensi &middot; 2 track independen</p>
      </div>
      <div class="slide-badge">SLIDE 14 / 22</div>
    </div>
    <div class="slide-body">
      <div class="dt-grid">
        <div class="dt-col">
          <div class="dt-head">👷 Panelman</div>
          <div class="dt-pills">
            <span class="dt-pill">Th 1</span>
            <span class="dt-pill">Th 2</span>
            <span class="dt-pill">Th 3</span>
          </div>
          <p class="dt-note">3 track terpisah &middot; hierarki &amp; deliverable independen</p>
        </div>
        <div class="dt-col operator">
          <div class="dt-head">🔧 Operator</div>
          <div class="dt-pills">
            <span class="dt-pill">Th 1</span>
            <span class="dt-pill">Th 2</span>
            <span class="dt-pill">Th 3</span>
          </div>
          <p class="dt-note">3 track terpisah &middot; hierarki &amp; deliverable independen</p>
        </div>
      </div>
      <div class="tip-bar">💡 <strong>Setiap track berdiri sendiri</strong> &mdash; hierarki kompetensi &amp; deliverable independen. Pekerja dipromosikan setiap tahun setelah semua deliverable selesai.</div>
    </div>
  </div>

```

- [ ] **Step 3: Verify slide 14 gone**

```bash
grep -n 'data-slide="14"\|COACHING DUAL TRACK\|dt-grid\|dt-col' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): cut slide 14 (Coaching Dual Track)

Detail Reviewer Chain dual-track = internal flow, tak perlu untuk
audiens umum. Konsep "3 tahun program kompetensi" sudah cukup
disampaikan di slide Assessment Proton dan Progresi Kompetensi.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Cut Slide 15 (Hierarki Kompetensi per Track)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (block slide 15 location bergeser; find by grep)

- [ ] **Step 1: Locate slide 15 block**

```bash
grep -n 'data-slide="15"\|HIERARKI KOMPETENSI' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: locate `<!-- SLIDE 15 (was 16): HIERARKI KOMPETENSI -->` + `<div ... data-slide="15">`.

- [ ] **Step 2: Delete DOM block slide 15 + comment**

Delete entire block:

```html
  <!-- ================= SLIDE 15 (was 16): HIERARKI KOMPETENSI ================= -->
  <div class="slide default-deco" data-slide="15">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 3 · STRUKTUR</p>
        <h1 class="slide-title">Hierarki <span class="accent">Kompetensi</span> per Track</h1>
        <p class="slide-subtitle">Track &rarr; Kompetensi &rarr; Sub-Kompetensi &rarr; Deliverable</p>
      </div>
      <div class="slide-badge">SLIDE 15 / 22</div>
    </div>
    <div class="slide-body">
      <div class="tree-grid">
        <div class="tree-col generic">
          <h4>Struktur Umum</h4>
          <div class="tree-node">📁 Track<span class="node-level">Level 0</span></div>
          <div class="tree-arrow">↓</div>
          <div class="tree-node">📂 Kompetensi<span class="node-level">Level 1</span></div>
          <div class="tree-arrow">↓</div>
          <div class="tree-node">📄 Sub-Kompetensi<span class="node-level">Level 2</span></div>
          <div class="tree-arrow">↓</div>
          <div class="tree-node">🎯 Deliverable<span class="node-level">Output Konkret</span></div>
        </div>
        <div class="tree-col example">
          <h4>Contoh Konkret</h4>
          <div class="tree-node">📁 Operator &mdash; Tahun 1<span class="node-level">Track</span></div>
          <div class="tree-arrow">↓</div>
          <div class="tree-node">📂 Safety Operation<span class="node-level">Kompetensi</span></div>
          <div class="tree-arrow">↓</div>
          <div class="tree-node">📄 LOTO (Lock Out Tag Out)<span class="node-level">Sub-Kompetensi</span></div>
          <div class="tree-arrow">↓</div>
          <div class="tree-node">🎯 Submit prosedur LOTO unit X<span class="node-level">Deliverable</span></div>
        </div>
      </div>
      <div class="tip-bar">💡 <strong>Independen per track</strong> &mdash; tidak shared. Semua deliverable selesai = lulus track &rarr; promosi ke tahun berikutnya.</div>
    </div>
  </div>

```

- [ ] **Step 3: Verify slide 15 gone**

```bash
grep -n 'data-slide="15"\|HIERARKI KOMPETENSI\|tree-grid\|tree-col' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): cut slide 15 (Hierarki Kompetensi per Track)

Tree-grid hierarchy detail (Track→Kompetensi→Sub→Deliverable) terlalu
deep untuk audiens umum. Progresi Kompetensi (slide 16 original) lebih
digestible — langsung jawab "apa beda Tahun 1/2/3" dengan tabel ringkas.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Reorder DOM blocks ke linear sequence

**Konteks:** Existing DOM order chaotic. Setelah Task 3-5 cut, sisa DOM order pakai original `data-slide`:
`1,2,3,4,5,6,7,8,(10 cut→DOM-before 9),9,11,12,13,(14 cut),19,(15 cut),16,17,18,20,21,22`

Setelah cut → sisa: `1,2,3,4,5,6,7,8,9,11,12,13,19,16,17,18,20,21,22` (19 slide).

Tapi DOM order saat ini: `1,2,3,4,5,6,7,8,9,11,12,13,19,16,17,18,20,21,22` — wait, sebelum cut slide 10 muncul DOM-before slide 9. Setelah cut, DOM order jadi: `1,2,3,4,5,6,7,8,9,11,12,13,19,16,17,18,20,21,22`. Slide 19 (IDP) masih sandwiched antara 13 dan 16 — TIDAK linear.

**Linear logical order target:** `1,2,3,4,5,6,7,8,9,11,12,13,16,17,18,19,20,21,22`

**Diff:** Move slide 19 (IDP) dari DOM position antara 13 dan 16 ke DOM position antara 18 dan 20.

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html`

- [ ] **Step 1: Locate slide 19 (IDP) block**

```bash
grep -n 'data-slide="19"\|IDP & TRAINING RECORDS' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: locate `<!-- SLIDE 19 (was 15): IDP & TRAINING RECORDS -->` + `<div ... data-slide="19">`.

- [ ] **Step 2: Locate slide 18 (Coaching Mahir) closing + slide 20 (Integrasi) opening**

```bash
grep -n 'data-slide="18"\|data-slide="20"\|SLIDE 20: INTEGRASI' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: locate `<div ... data-slide="18">` (Coaching Mahir) start dan `<div ... data-slide="20">` (Integrasi) start.

- [ ] **Step 3: Cut slide 19 (IDP) block dari current position**

Block to move (45 baris dari `<!-- SLIDE 19 (was 15)... -->` sampai `</div>` closing tag slide 19 inclusive):

```html
  <!-- ================= SLIDE 19 (was 15): IDP & TRAINING RECORDS ================= -->
  <div class="slide default-deco" data-slide="19">
    <div class="slide-header">
      <div>
        <p class="section-eyebrow">BAGIAN 3 · CDP</p>
        <h1 class="slide-title">IDP &amp; <span class="accent">Training Records</span></h1>
        <p class="slide-subtitle">Dua komponen pelengkap Coaching Proton di CDP</p>
      </div>
      <div class="slide-badge">SLIDE 19 / 22</div>
    </div>
    <div class="slide-body">
      <div class="modules-grid" style="grid-template-columns:1fr 1fr;gap:18px;">
        <div class="module-card cmp">
          <div class="module-head">
            <div class="module-icon">📋</div>
            <div>
              <div class="module-name">IDP</div>
              <div class="module-subname">Individual Development Plan (Perpustakaan)</div>
            </div>
          </div>
          <ul class="module-bullets">
            <li>📂 Repository dokumen IDP per pekerja</li>
            <li>📄 Akses dokumen KKJ (Kebutuhan Kompetensi Jabatan)</li>
            <li>👁 Worker view &amp; download dokumen</li>
            <li>🔍 Filter &amp; search per jabatan / unit</li>
          </ul>
        </div>
        <div class="module-card cdp">
          <div class="module-head">
            <div class="module-icon">📚</div>
            <div>
              <div class="module-name">Training Records</div>
              <div class="module-subname">Riwayat Pelatihan</div>
            </div>
          </div>
          <ul class="module-bullets">
            <li>🏫 Training internal &amp; eksternal</li>
            <li>🏷️ Kategori + sub-kategori</li>
            <li>📎 Sertifikat upload (PDF/image)</li>
            <li>⏳ Validity period &amp; renewal</li>
          </ul>
        </div>
      </div>
      <div class="tip-bar" style="margin-top:14px;">💡 IDP &amp; Training Records <strong>terintegrasi dengan profile pekerja</strong> &mdash; jadi referensi gap analysis &amp; promosi.</div>
    </div>
  </div>
```

- [ ] **Step 4: Paste slide 19 (IDP) block AFTER slide 18 (Coaching Mahir) closing, BEFORE slide 20 (Integrasi) comment**

Insert location: setelah `</div>` closing slide `data-slide="18"`, sebelum `<!-- SLIDE 20: INTEGRASI & KEAMANAN -->` comment.

- [ ] **Step 5: Verify linear DOM order**

```bash
grep -n 'data-slide=' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected order (top to bottom):
```
data-slide="1"
data-slide="2"
data-slide="3"
data-slide="4"
data-slide="5"
data-slide="6"
data-slide="7"
data-slide="8"
data-slide="9"
data-slide="11"
data-slide="12"
data-slide="13"
data-slide="16"
data-slide="17"
data-slide="18"
data-slide="19"
data-slide="20"
data-slide="21"
data-slide="22"
```

19 entries total, linear ascending (gaps OK karena belum renumber).

- [ ] **Step 6: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
chore(sosialisasi-umum): reorder DOM — IDP slide ke posisi linear

Pindahkan slide 19 (IDP & Training Records) dari DOM sandwich antara
13-16 ke posisi linear setelah Coaching Mahir (18) sebelum Integrasi
(20). Source maintainability — DOM order match logical sequence.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: Renumber `data-slide` + update standard badge + JS const + totalNum

**Konteks:** 19 slide tersisa, masih pakai original numbering. Renumber sequential 1..19 per Renumber Map di header plan.

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html`

- [ ] **Step 1: Renumber `data-slide` attribute pada 19 slide**

Apply mapping (urut dari belakang ke depan untuk hindari conflict number collision selama replace):

| Old → New |
|---|
| `data-slide="22"` → `data-slide="19"` |
| `data-slide="21"` → `data-slide="18"` |
| `data-slide="20"` → `data-slide="17"` |
| `data-slide="19"` → `data-slide="16"` |
| `data-slide="18"` → `data-slide="15"` |
| `data-slide="17"` → `data-slide="14"` |
| `data-slide="16"` → `data-slide="13"` |
| `data-slide="13"` → `data-slide="12"` |
| `data-slide="12"` → `data-slide="11"` |
| `data-slide="11"` → `data-slide="10"` |
| `data-slide="1"` through `data-slide="9"` → no change |

**Pakai urutan reverse mapping di atas** supaya tidak terjadi double-rename. Untuk setiap mapping, edit `data-slide="X"` literal jadi `data-slide="Y"`.

- [ ] **Step 2: Verify renumber data-slide**

```bash
grep -nE 'data-slide="[0-9]+"' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 19 lines, values 1..19 ascending in DOM order, no duplicates, no gaps.

- [ ] **Step 3: Update 18 standard slide-badge "SLIDE N / 22" → "SLIDE M / 19"**

Apply mapping per Renumber Map. Untuk setiap badge standard (kecuali custom badge slide 13 OFFLINE + slide 18 MAHIR original = baru slide 12 + 15 yang TIDAK punya standard badge), edit literal:

| Old badge text → New badge text |
|---|
| `SLIDE 2 / 22` → `SLIDE 2 / 19` |
| `SLIDE 3 / 22` → `SLIDE 3 / 19` |
| `SLIDE 4 / 22` → `SLIDE 4 / 19` |
| `SLIDE 5 / 22` → `SLIDE 5 / 19` |
| `SLIDE 6 / 22` → `SLIDE 6 / 19` |
| `SLIDE 7 / 22` → `SLIDE 7 / 19` |
| `SLIDE 8 / 22` → `SLIDE 8 / 19` |
| `SLIDE 9 / 22` → `SLIDE 9 / 19` |
| `SLIDE 11 / 22` → `SLIDE 10 / 19` |
| `SLIDE 12 / 22` → `SLIDE 11 / 19` |
| `SLIDE 14 / 22` → already CUT in Task 4, no action |
| `SLIDE 15 / 22` → already CUT in Task 5, no action |
| `SLIDE 16 / 22` → `SLIDE 13 / 19` |
| `SLIDE 17 / 22` → `SLIDE 14 / 19` |
| `SLIDE 19 / 22` → `SLIDE 16 / 19` |
| `SLIDE 20 / 22` → `SLIDE 17 / 19` |
| `SLIDE 21 / 22` → `SLIDE 18 / 19` |

Custom badge (TIDAK diubah):
- Slide Alur Proton T3 (was data-slide=13, now data-slide=12): badge `🎤 OFFLINE MODE` — keep
- Slide Coaching Mahir (was data-slide=18, now data-slide=15): badge `🎯 LEVEL MAHIR` — keep

- [ ] **Step 4: Verify badges**

```bash
grep -nE 'class="slide-badge"' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 20 badge total = 18 standard `SLIDE N / 19` + 2 custom (OFFLINE MODE, LEVEL MAHIR). No badge `/ 22` tersisa.

```bash
grep -nE 'SLIDE [0-9]+ / 22' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: no output (semua badge sudah `/ 19`).

- [ ] **Step 5: Update JS `const total` + HTML totalNum span**

Locate:
```bash
grep -nE 'const total = |id="totalNum"' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Edit:
```html
<div class="slide-counter"><span id="curNum">1</span> / <span id="totalNum">22</span></div>
```
→
```html
<div class="slide-counter"><span id="curNum">1</span> / <span id="totalNum">19</span></div>
```

```javascript
const total = 22;
```
→
```javascript
const total = 19;
```

- [ ] **Step 6: Verify JS + totalNum**

```bash
grep -nE 'const total =|id="totalNum"' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected:
- `const total = 19;`
- `<span id="totalNum">19</span>`

- [ ] **Step 7: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): renumber data-slide 1..19 + badge + JS const

- data-slide sequential 1..19 (linear DOM order)
- 18 standard badge "SLIDE N / 22" → "SLIDE M / 19" per renumber map
- Custom badge OFFLINE MODE (was 13→12) & LEVEL MAHIR (was 18→15) tetap
- JS const total = 19, HTML totalNum span = 19

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 8: Patch slide 7 (Sistem Assessment) — extend tip-bar absorb Pre/Post + retone

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="7", line ~varies after Task 7)

- [ ] **Step 1: Locate slide 7 tip-bar**

```bash
grep -n 'tip-bar.*Assessment Umum untuk evaluasi' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: locate `<div class="tip-bar">💡 <strong>Assessment Umum</strong> untuk evaluasi reguler per batch unit / jenis kompetensi &middot; <strong>Proton</strong> untuk program pengembangan 3 tahun</div>`

- [ ] **Step 2: Edit tip-bar — extend dengan Pre/Post 1-liner**

Old:
```html
      <div class="tip-bar">💡 <strong>Assessment Umum</strong> untuk evaluasi reguler per batch unit / jenis kompetensi &middot; <strong>Proton</strong> untuk program pengembangan 3 tahun</div>
```

New:
```html
      <div class="tip-bar">💡 <strong>Assessment Umum</strong> untuk evaluasi reguler per batch / jenis kompetensi &middot; <strong>Proton</strong> untuk program pengembangan 3 tahun &middot; <strong>Pre/Post Test</strong> mengukur Gain Score peserta sebelum vs sesudah pelatihan</div>
```

- [ ] **Step 3: Edit slide 7 subtitle (retone framing HC → umum)**

Old:
```html
        <p class="slide-subtitle">Dua jenis assessment utama di HC Portal</p>
```

New:
```html
        <p class="slide-subtitle">Dua jenis assessment utama untuk pekerja KPB</p>
```

- [ ] **Step 4: Verify slide 7 changes**

```bash
grep -n 'Pre/Post Test.*Gain Score peserta\|Dua jenis assessment utama untuk pekerja' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 2 matches found.

- [ ] **Step 5: Browser smoke test slide 7**

Buka file `docs/Sosialisasi-Umum-PortalHC-KPB.html` di browser. Navigate ke slide 7. Verify:
- tip-bar text mencakup 3 segmen (Umum / Proton / Pre/Post) tanpa overflow viewport
- 2 jenis-card (Umum + Proton) render normal

- [ ] **Step 6: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide 7 absorb Pre/Post + retone framing

- tip-bar extend dengan Pre/Post Gain Score 1-liner (no layout shift)
- subtitle "Dua jenis assessment utama di HC Portal" → "untuk pekerja KPB"

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 9: Retone slide 1 (Cover)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="1")

- [ ] **Step 1: Locate cover content**

```bash
grep -n 'cover-eyebrow\|cover-meta-eyebrow\|cover-meta-date' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

- [ ] **Step 2: Edit cover-meta-eyebrow**

Old:
```html
        <div class="cover-meta-eyebrow">Sosialisasi</div>
```

New:
```html
        <div class="cover-meta-eyebrow">Sosialisasi untuk Pekerja Pertamina</div>
```

- [ ] **Step 3: Verify**

```bash
grep -n 'Sosialisasi untuk Pekerja Pertamina' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 1 match in cover-meta-eyebrow.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide 1 cover retone — Sosialisasi untuk Pekerja Pertamina

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 10: Retone slide 2 (Agenda) + update goTo() targets

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="2")

- [ ] **Step 1: Locate agenda block**

```bash
grep -n 'agenda-grid\|agenda-item' docs/Sosialisasi-Umum-PortalHC-KPB.html | head -10
```

- [ ] **Step 2: Replace entire agenda-grid block — update goTo() + retone framing**

Old:
```html
    <div class="slide-body">
      <div class="agenda-grid">
        <div class="agenda-item" onclick="goTo(3)"><div class="agenda-num">01</div><div class="agenda-content"><h4>Pengenalan</h4><p>Latar belakang, definisi, 3 platform, role</p></div></div>
        <div class="agenda-item" onclick="goTo(7)"><div class="agenda-num">02</div><div class="agenda-content"><h4>Sistem Assessment</h4><p>2 jenis + 5 kategori + Pre/Post Test + alur 7-step</p></div></div>
        <div class="agenda-item" onclick="goTo(11)"><div class="agenda-num">03</div><div class="agenda-content"><h4>Assessment Proton</h4><p>3 tahun · online &amp; interview offline</p></div></div>
        <div class="agenda-item" onclick="goTo(14)"><div class="agenda-num">04</div><div class="agenda-content"><h4>Coaching Proton (CDP)</h4><p>Dual track, IDP, hierarki, progresi 3 tahun</p></div></div>
        <div class="agenda-item" onclick="goTo(20)"><div class="agenda-num">05</div><div class="agenda-content"><h4>Operasional</h4><p>Integrasi keamanan &amp; cara akses portal</p></div></div>
        <div class="agenda-item" onclick="goTo(22)"><div class="agenda-num">06</div><div class="agenda-content"><h4>Q&amp;A</h4><p>Diskusi dan tanya jawab</p></div></div>
      </div>
    </div>
```

New:
```html
    <div class="slide-body">
      <div class="agenda-grid">
        <div class="agenda-item" onclick="goTo(3)"><div class="agenda-num">01</div><div class="agenda-content"><h4>Pengenalan</h4><p>Latar belakang, definisi, 3 platform, role pengguna</p></div></div>
        <div class="agenda-item" onclick="goTo(7)"><div class="agenda-num">02</div><div class="agenda-content"><h4>Sistem Assessment</h4><p>2 jenis + 5 kategori + alur 7-step</p></div></div>
        <div class="agenda-item" onclick="goTo(10)"><div class="agenda-num">03</div><div class="agenda-content"><h4>Assessment Proton</h4><p>Program 3 tahun · online &amp; interview offline</p></div></div>
        <div class="agenda-item" onclick="goTo(13)"><div class="agenda-num">04</div><div class="agenda-content"><h4>Pengembangan Kompetensi</h4><p>Progresi 3 tahun, alur coaching, IDP &amp; training records</p></div></div>
        <div class="agenda-item" onclick="goTo(17)"><div class="agenda-num">05</div><div class="agenda-content"><h4>Operasional</h4><p>Integrasi keamanan &amp; cara akses portal</p></div></div>
        <div class="agenda-item" onclick="goTo(19)"><div class="agenda-num">06</div><div class="agenda-content"><h4>Q&amp;A</h4><p>Diskusi dan tanya jawab</p></div></div>
      </div>
    </div>
```

**Changes:**
- `goTo(11)` → `goTo(10)` (Assessment Proton renumber)
- `goTo(14)` → `goTo(13)` (Coaching Dual Track CUT; redirect ke Progresi Kompetensi)
- `goTo(20)` → `goTo(17)` (Integrasi renumber)
- `goTo(22)` → `goTo(19)` (Penutup renumber)
- Agenda item 02: hapus "Pre/Post Test" mention (sudah absorbed di tip-bar slide 7)
- Agenda item 04: title "Coaching Proton (CDP)" → "Pengembangan Kompetensi", desc retone (drop "dual track, hierarki" yang sudah CUT)

- [ ] **Step 3: Verify goTo targets**

```bash
grep -nE 'goTo\([0-9]+\)' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 6 lines dengan targets `3, 7, 10, 13, 17, 19` — all dalam range 1..19.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide 2 agenda — update goTo + retone framing

- goTo(11→10) Assessment Proton renumber
- goTo(14→13) Coaching Dual Track CUT → redirect ke Progresi Kompetensi
- goTo(20→17) Integrasi renumber
- goTo(22→19) Penutup renumber
- Agenda 02 hapus Pre/Post Test mention (absorbed di slide 7)
- Agenda 04 retitle "Pengembangan Kompetensi"

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 11: Retone slide 4 (Apa Itu HC Portal KPB)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="4")

- [ ] **Step 1: Locate slide 4 subtitle + definition-text**

```bash
grep -n 'Apa Itu.*HC Portal\|Sistem informasi terpadu untuk Tim Human Capital\|definition-text' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

- [ ] **Step 2: Edit subtitle**

Old:
```html
        <p class="slide-subtitle">Sistem informasi terpadu untuk Tim Human Capital Kilang Pertamina Balikpapan</p>
```

New:
```html
        <p class="slide-subtitle">Sistem informasi pengembangan kompetensi pekerja Kilang Pertamina Balikpapan</p>
```

- [ ] **Step 3: Edit definition-text**

Old:
```html
        <div class="definition-text">
          Sistem informasi berbasis web Tim <strong>Human Capital Kilang Pertamina Balikpapan</strong> untuk <strong>MENGELOLA · MENGEMBANGKAN · MENDAMPINGI</strong> kompetensi pekerja lewat tiga platform terpadu: CMP, CDP, BP.
        </div>
```

New:
```html
        <div class="definition-text">
          Sistem informasi berbasis web <strong>Kilang Pertamina Balikpapan</strong> untuk <strong>MENGELOLA · MENGEMBANGKAN · MENDAMPINGI</strong> kompetensi pekerja lewat tiga platform terpadu: CMP, CDP, BP.
        </div>
```

- [ ] **Step 4: Verify**

```bash
grep -n 'pengembangan kompetensi pekerja Kilang\|Sistem informasi berbasis web <strong>Kilang' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 2 matches.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide 4 retone — definisi POV pekerja

Subtitle + definition-text drop "Tim Human Capital" framing, ganti ke
"pengembangan kompetensi pekerja Kilang Pertamina Balikpapan".

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 12: Retone slide 5 (3 Platform Terpadu)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="5")

- [ ] **Step 1: Locate slide 5 subtitle + module-desc**

```bash
grep -n '3 <span class="accent">Platform\|module-desc' docs/Sosialisasi-Umum-PortalHC-KPB.html | head -10
```

- [ ] **Step 2: Edit subtitle**

Old:
```html
        <p class="slide-subtitle">CMP · CDP · BP &mdash; manajemen, pengembangan, &amp; strategic partner</p>
```

New:
```html
        <p class="slide-subtitle">CMP · CDP · BP &mdash; kelola, kembangkan, &amp; dampingi kompetensi pekerja</p>
```

- [ ] **Step 3: Edit CMP module-desc**

Old:
```html
          <div class="module-desc">Platform digital untuk pengelolaan kompetensi terintegrasi &mdash; penyusunan KKJ, IDP, asesmen teknis &amp; Safety.</div>
```

New:
```html
          <div class="module-desc">Platform digital untuk pengelolaan kompetensi pekerja &mdash; penyusunan KKJ (Kebutuhan Kompetensi Jabatan), IDP, asesmen teknis &amp; Safety.</div>
```

- [ ] **Step 4: Edit CDP module-desc**

Old:
```html
          <div class="module-desc">Pembelajaran terstruktur untuk menutup gap kompetensi &mdash; prinsip blended Learning (Assignment, Coaching, Self Study).</div>
```

New:
```html
          <div class="module-desc">Pembelajaran terstruktur untuk menutup gap kompetensi pekerja &mdash; prinsip blended Learning (Assignment, Coaching, Self Study).</div>
```

- [ ] **Step 5: Edit BP module-desc**

Old:
```html
          <div class="module-desc">Modul HRBP &mdash; strategic partner antara HC &amp; unit operasional untuk workforce planning, employee relations, &amp; advisory.</div>
```

New:
```html
          <div class="module-desc">Modul Business Partner &mdash; jembatan antara fungsi HC &amp; unit operasional untuk workforce planning, employee relations, &amp; advisory.</div>
```

- [ ] **Step 6: Verify**

```bash
grep -n 'kelola, kembangkan, &amp; dampingi\|pengelolaan kompetensi pekerja\|jembatan antara fungsi HC' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 3 matches.

- [ ] **Step 7: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide 5 retone — jelasin platform pakai bahasa awam

- Subtitle drop jargon "strategic partner"
- CMP desc tambah expand KKJ singkatan
- BP desc ganti "HRBP / strategic partner" → "Business Partner / jembatan"

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 13: Retone slide 6 (Struktur Role Pengguna)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="6")

- [ ] **Step 1: Locate slide 6 subtitle + stair-caption**

```bash
grep -n 'Struktur <span class="accent">Role\|stair-caption' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

- [ ] **Step 2: Edit subtitle**

Old:
```html
        <p class="slide-subtitle">10 role &middot; 6 level &mdash; makin tinggi tangga, makin luas authority</p>
```

New:
```html
        <p class="slide-subtitle">10 role &middot; 6 level &mdash; pekerja umumnya masuk L5 Coach atau L6 Coachee</p>
```

- [ ] **Step 3: Verify (stair-caption tidak diubah — sudah deskriptif tanpa HC framing)**

```bash
grep -n 'pekerja umumnya masuk L5 Coach atau L6 Coachee' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 1 match.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide 6 retone — bridge ke audiens A

Subtitle highlight role default audiens (L5 Coach / L6 Coachee).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 14: Retone slide 11 (Assessment Proton — was data-slide=11, now data-slide=10) + fix internal slide reference

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="10" after Task 7 renumber)

- [ ] **Step 1: Locate slide Proton block**

```bash
grep -n 'Assessment <span class="accent">Proton\|Detail komparasi 5 aspek per tahun' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

- [ ] **Step 2: Edit subtitle**

Old:
```html
        <p class="slide-subtitle">Program 3 tahun &middot; 1 track per role (Panelman / Operator)</p>
```

New:
```html
        <p class="slide-subtitle">Program kompetensi 3 tahun untuk Panelman / Operator &mdash; jalur pengembangan terstruktur</p>
```

- [ ] **Step 3: Update internal slide reference dari "Slide 16" → "Slide 13"**

Old:
```html
        <div class="tip-bar" style="display:inline-block;text-align:left;max-width:640px;">
          💡 Detail komparasi 5 aspek per tahun (fokus, deliverable, coaching, assessment, akhir tahun) di <strong>Slide 16 · Progresi Kompetensi</strong> (Bagian 3).
        </div>
```

New:
```html
        <div class="tip-bar" style="display:inline-block;text-align:left;max-width:640px;">
          💡 Detail komparasi 5 aspek per tahun (fokus, deliverable, coaching, assessment, akhir tahun) di <strong>Slide 13 · Progresi Kompetensi</strong> (Bagian 3).
        </div>
```

- [ ] **Step 4: Verify**

```bash
grep -n 'Slide 13 · Progresi Kompetensi\|Program kompetensi 3 tahun untuk Panelman' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 2 matches.

```bash
grep -n 'Slide 16 · Progresi' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: no output (old reference gone).

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide Assessment Proton retone + fix internal ref

- Subtitle lead-in: "Program kompetensi 3 tahun untuk Panelman / Operator"
- Internal reference "Slide 16 · Progresi Kompetensi" → "Slide 13" (renumber)

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 15: Retone slide 19 (IDP & Training Records — was data-slide=19, now data-slide=16)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="16" after Task 7 renumber)

- [ ] **Step 1: Locate slide IDP block**

```bash
grep -n 'IDP &amp; <span class="accent">Training Records\|jadi referensi gap analysis' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

- [ ] **Step 2: Edit subtitle**

Old:
```html
        <p class="slide-subtitle">Dua komponen pelengkap Coaching Proton di CDP</p>
```

New:
```html
        <p class="slide-subtitle">Jejak pengembangan pekerja &mdash; rencana karir &amp; riwayat pelatihan tersimpan rapi</p>
```

- [ ] **Step 3: Edit tip-bar**

Old:
```html
      <div class="tip-bar" style="margin-top:14px;">💡 IDP &amp; Training Records <strong>terintegrasi dengan profile pekerja</strong> &mdash; jadi referensi gap analysis &amp; promosi.</div>
```

New:
```html
      <div class="tip-bar" style="margin-top:14px;">💡 IDP &amp; Training Records <strong>terintegrasi dengan profile pekerja</strong> &mdash; referensi untuk rencana pengembangan &amp; promosi karir.</div>
```

- [ ] **Step 4: Verify**

```bash
grep -n 'Jejak pengembangan pekerja\|rencana pengembangan &amp; promosi karir' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 2 matches.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide IDP retone — angle benefit pekerja

Subtitle + tip-bar highlight benefit ke pekerja (rencana karir, riwayat
pelatihan, jejak pengembangan), bukan POV HC-internal.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 16: Retone slide 20 (Integrasi & Keamanan — was data-slide=20, now data-slide=17)

**Files:**
- Modify: `docs/Sosialisasi-Umum-PortalHC-KPB.html` (slide data-slide="17" after Task 7 renumber)

- [ ] **Step 1: Locate slide Integrasi**

```bash
grep -n 'Integrasi &amp; <span class="accent">Keamanan\|Fitur pendukung yang menjaga' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

- [ ] **Step 2: Edit subtitle**

Old:
```html
        <p class="slide-subtitle">Fitur pendukung yang menjaga integritas &amp; kemudahan</p>
```

New:
```html
        <p class="slide-subtitle">Data pekerja aman, terotorisasi, &amp; mudah diakses sesuai role</p>
```

- [ ] **Step 3: Verify**

```bash
grep -n 'Data pekerja aman, terotorisasi' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: 1 match.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
feat(sosialisasi-umum): slide Integrasi retone — angle data pekerja aman

Subtitle ganti "Fitur pendukung yang menjaga integritas" → "Data
pekerja aman, terotorisasi, mudah diakses sesuai role". Body cat-grid
existing (LDAP, Anti-Copy, Audit Log, RBAC, Notifikasi, Import Excel)
tetap utuh — fitur sudah self-explanatory untuk audiens umum.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 17: Verifikasi final + browser smoke test + final commit

**Files:**
- Browser test: open `docs/Sosialisasi-Umum-PortalHC-KPB.html`

- [ ] **Step 1: Full file structural validation**

```bash
grep -cE 'data-slide="[0-9]+"' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: `19`.

```bash
grep -nE 'data-slide="[0-9]+"' docs/Sosialisasi-Umum-PortalHC-KPB.html | head -25
```

Expected: 19 lines, `data-slide="1"` through `data-slide="19"` ascending in DOM order.

- [ ] **Step 2: Verify no `/ 22` artifacts**

```bash
grep -nE 'SLIDE [0-9]+ / 22|const total = 22|>22</span>' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: no output.

- [ ] **Step 3: Verify CUT slide content gone**

```bash
grep -nE 'pp-stepper|pp-metric|PRE & POST TEST|COACHING DUAL TRACK|HIERARKI KOMPETENSI|tree-grid|tree-col|dt-grid|dt-col' docs/Sosialisasi-Umum-PortalHC-KPB.html
```

Expected: no output (semua DOM content cut slide gone; CSS rules untuk class ini tertinggal di `<style>` block — no-op aman).

- [ ] **Step 4: Verify flow slide intact (bit-identical content body)**

```bash
git diff main -- docs/Sosialisasi-Umum-PortalHC-KPB.html | grep -E '^\-' | grep -E 'alur-step|swim-step|swim-lane|swim-row' | head -20
```

Expected: no deleted lines (flow slide content body tak diubah). Hanya line `data-slide=` dan `slide-badge` text yang berubah pada flow slide.

- [ ] **Step 5: Browser smoke test**

Open `docs/Sosialisasi-Umum-PortalHC-KPB.html` di browser (Chrome / Edge / Firefox).

Checklist:
- [ ] Cover render normal, subtitle "Sosialisasi untuk Pekerja Pertamina"
- [ ] Slide counter render `1 / 19` di nav bar
- [ ] Arrow Right key → navigasi ke slide 2 (Agenda)
- [ ] Klik agenda item `01 Pengenalan` → lompat ke slide 3 (Latar Belakang)
- [ ] Klik agenda item `02 Sistem Assessment` → lompat ke slide 7
- [ ] Klik agenda item `03 Assessment Proton` → lompat ke slide 10 (was 11)
- [ ] Klik agenda item `04 Pengembangan Kompetensi` → lompat ke slide 13 (was 16 Progresi)
- [ ] Klik agenda item `05 Operasional` → lompat ke slide 17 (was 20 Integrasi)
- [ ] Klik agenda item `06 Q&A` → lompat ke slide 19 (was 22 Penutup)
- [ ] Arrow Right dari slide 19 → button `Next` disabled
- [ ] Home key → jump ke slide 1
- [ ] End key → jump ke slide 19
- [ ] Slide 7 (Sistem Assessment) tip-bar tampil 3 segmen tanpa overflow
- [ ] Slide 9 (Alur Assessment 7-step) FLOW intact — swim-row swim-lane render normal
- [ ] Slide 11 (was 12 Proton T1&2) FLOW intact
- [ ] Slide 12 (was 13 Proton T3) FLOW intact + badge `🎤 OFFLINE MODE` custom
- [ ] Slide 14 (was 17 Coaching Pemula) FLOW intact
- [ ] Slide 15 (was 18 Coaching Mahir) FLOW intact + badge `🎯 LEVEL MAHIR` custom
- [ ] Progress bar update saat navigate (proportional 1/19, 2/19, ...)
- [ ] Dark mode toggle (D) works
- [ ] Fullscreen toggle (F) works
- [ ] Tidak ada slide kosong / DOM duplicate / overflow viewport

- [ ] **Step 6: Final commit (if smoke test surface bugs, fix + commit; else skip)**

Jika smoke test PASS tanpa fix → no final commit needed (semua sudah committed per task).

Jika ada fix:
```bash
git add docs/Sosialisasi-Umum-PortalHC-KPB.html
git commit -m "$(cat <<'EOF'
fix(sosialisasi-umum): browser smoke test findings

<isi spesifik bug yang ditemukan + fix>

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 7: Verify git log clean**

```bash
git log --oneline -20 | grep sosialisasi-umum
```

Expected: 16-17 commit (1 rename + 15-16 feat commits + optional fix commit), urut dari rename → cut 10 → cut 14 → cut 15 → reorder → renumber → retone × 8 → smoke fix.

---

## Self-Review

**Spec coverage check (vs `docs/superpowers/specs/2026-05-22-sosialisasi-umum-design.md`):**

| Spec Acceptance | Implemented in |
|---|---|
| File rename via git mv | Task 1 |
| `<title>` updated | Task 2 |
| 19 slide total | Task 3-5 (cut 3) + Task 6 (reorder) + Task 7 (renumber) |
| `data-slide` sequential 1..19 | Task 7 Step 1-2 |
| DOM block linear order | Task 6 |
| `<span id="totalNum">` = 19 | Task 7 Step 5-6 |
| JS `const total = 19;` | Task 7 Step 5-6 |
| 18 standard badge "/ 22" → "/ 19" | Task 7 Step 3-4 |
| Custom badge OFFLINE / MAHIR utuh | Task 7 Step 3 |
| Slide 7 tip-bar absorb Pre/Post | Task 8 |
| 5 flow slide bit-identical body | Task 6 (move only IDP), Task 7 (renumber data-slide + badge only — body content tak disentuh); Task 17 Step 4 verify |
| Tone "Tim HC" → "pekerja" di 8 slide retone | Task 8 (slide 7), Task 9-16 (slide 1, 2, 4, 5, 6, Proton, IDP, Integrasi) |
| Browser verifikasi (nav, badge, counter, progress, dark, FS) | Task 17 Step 5 |

**Placeholder scan:** No TBD / TODO / "implement later" / "handle edge cases" / "similar to Task N". All code blocks contain actual content.

**Type consistency:** N/A (HTML/JS edit, no type system). Slide number references checked: agenda `goTo()` targets match Renumber Map; slide 11 (Proton) internal reference "Slide 16" → "Slide 13" matches Progresi renumber.

**Coverage gap:** None identified.

---

## Out-of-Scope (per spec §9)

- PDF export
- Translasi English
- Print stylesheet
- Video / animation embed
- Integrasi ke Portal HC live
- Update file Internal-Tim-HC
- Cleanup CSS rules untuk class no-op (`.pp-stepper`, `.pp-metric`, `.dt-grid`, `.dt-col`, `.tree-grid`, `.tree-col`) — no-op aman, tak rusak rendering

---

## Risk Catalog (per spec §7)

| Risk | Mitigation di plan |
|---|---|
| Slide 7 absorb overflow | Task 8 Step 5 browser smoke test |
| Renumber break navigation | Task 7 Step 5-6 verify const + totalNum; Task 17 Step 5 browser test goTo + Arrow + Home/End |
| CSS rule per data-slide value | Task 17 Step 3 grep audit (CSS no-op aman) |
| Anchor link break | N/A — file tak punya `<a href="#slide-N">` (verified spec §7) |
| DOM reorder break visual | Task 17 Step 5 browser test seluruh 19 slide |
