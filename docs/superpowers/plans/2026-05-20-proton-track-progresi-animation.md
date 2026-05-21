# PROTON Bagian 3 — Track & Progresi Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bangun animasi HTML 16:9 standalone untuk PROTON video bagian 3 (Track & Progresi Tahunan, durasi 25 detik), lalu record menjadi `.webm` lewat Playwright.

**Architecture:** File HTML standalone berisi 1 stage 16:9 dengan pure CSS animation (`animation-delay` sequential, no JS). Pola sama dengan `smart-animation.html` existing. Record script Node.js pakai Playwright headless yang serve file via Python `http.server`, capture viewport 1920×1080 selama 26 detik (25 + 1s buffer), output WebM.

**Tech Stack:** HTML5 + CSS3 (no framework), Google Fonts Inter, Playwright (chromium), Node.js ≥18, Python (untuk static server lokal saat record).

**Spec reference:** `docs/superpowers/specs/2026-05-20-proton-track-progresi-animation-design.md`

---

## File Structure

| File | Status | Tanggung jawab |
|------|--------|----------------|
| `docs/assets/proton-video/track-progresi.html` | CREATE | Sumber animasi — 1 file standalone, 16:9 stage, semua markup + CSS + keyframes |
| `docs/assets/proton-video/record-track-progresi.mjs` | CREATE | Script Playwright untuk record HTML jadi WebM |
| `docs/assets/proton-video/track-progresi.webm` | CREATE (generated) | Output video hasil record, di-commit ke repo |
| `.gitignore` | NO CHANGE | `.webm` di folder `proton-video/` tetap di-track (sama pola dengan `smart-animation.webm`) |

File existing yang **tidak dipakai** (untracked, abaikan, tidak dihapus dalam plan ini):
- `docs/assets/proton-video/track-progresi-animation.html`
- `docs/assets/proton-video/track-progresi-v-G.html`
- `docs/assets/proton-video/track-progresi-v-H.html`
- `docs/assets/proton-video/track-progresi-animation.webm`
- `docs/assets/proton-video/screenshot-track.mjs`
- `docs/assets/proton-video/screenshot-versions.mjs`
- `docs/assets/proton-video/record-track-progresi.mjs` (kalau ada — kita overwrite dengan versi baru sesuai plan ini)

Apabila file ini sudah ada di working tree (status untracked), task 1 akan overwrite `track-progresi.html` saja. File untracked lain abaikan.

---

## Task 1: Scaffold HTML — stage 16:9, palette, header static

**Files:**
- Create: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Bikin file HTML dengan skeleton stage 16:9 + CSS variables + font loading**

Tulis `docs/assets/proton-video/track-progresi.html` dengan isi berikut:

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8">
<title>Animasi Track & Progresi PROTON — Video Bagian 3 (0:50–1:15)</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap" rel="stylesheet">
<link rel="icon" href="data:,">
<style>
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

  :root {
    --navy-deep: #0A2447;
    --navy:      #0F2D5C;
    --navy-soft: #1E4280;
    --red:       #E63329;
    --red-dark:  #C5251D;
    --ink:       #1A1A1A;
    --text:      #3D3D3D;
    --muted:     #888888;
    --hairline:  #E5E7EB;
    --paper:     #F9FAFB;
    --sky:       #F0F4FA;
  }

  html, body { width: 100%; height: 100%; overflow: hidden; background: #FFFFFF; }

  body {
    font-family: 'Inter', system-ui, sans-serif;
    color: var(--ink);
    display: grid;
    place-items: center;
    min-height: 100vh;
    background: #FFFFFF;
  }

  /* 16:9 stage */
  .stage {
    width: min(96vw, 1600px);
    aspect-ratio: 16 / 9;
    position: relative;
    background: linear-gradient(180deg, #FFFFFF 0%, var(--sky) 100%);
    border: 1px solid var(--hairline);
    box-shadow: 0 1px 2px rgba(0,0,0,0.02), 0 24px 64px rgba(10,36,71,0.06);
    border-radius: 8px;
    overflow: hidden;
    padding: clamp(40px, 4.5vw, 80px) clamp(48px, 5.5vw, 96px);
    display: flex;
    flex-direction: column;
  }
</style>
</head>
<body>
  <div class="stage" id="stage">
    <!-- header + role-row + timeline akan ditambahkan di task berikutnya -->
  </div>
</body>
</html>
```

- [ ] **Step 2: Buka di browser, pastikan stage 16:9 tampil rapi**

Run (di terminal terpisah):
```bash
cd "docs/assets/proton-video" && python -m http.server 8800
```
Buka `http://localhost:8800/track-progresi.html`. Expected: persegi panjang 16:9 kosong dengan background gradient putih→sky, border tipis, rounded corner. Tutup server (Ctrl+C) setelah verifikasi.

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "feat(proton-video): scaffold track-progresi.html — stage 16:9 + palette"
```

---

## Task 2: Header bar — eyebrow + title + progress dots

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah CSS header bar**

Sisipkan di dalam `<style>` (setelah block `.stage`):

```css
  /* ===== HEADER ===== */
  .header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: clamp(24px, 2.5vw, 40px);
  }
  .header .title-block {
    opacity: 0;
    transform: translateY(-12px);
    animation: fadeDown 0.8s 0.2s cubic-bezier(0.2, 0, 0.2, 1) forwards;
  }
  .eyebrow {
    font-size: clamp(11px, 0.9vw, 14px);
    font-weight: 800;
    color: var(--red);
    letter-spacing: 0.2em;
    text-transform: uppercase;
    margin-bottom: 8px;
  }
  .title {
    font-size: clamp(28px, 2.6vw, 44px);
    font-weight: 900;
    color: var(--navy-deep);
    line-height: 1.1;
    letter-spacing: -0.01em;
  }
  .title em { font-style: normal; color: var(--red); }

  .progress-dots {
    display: flex;
    gap: 8px;
    margin-top: 12px;
    opacity: 0;
    animation: fadeIn 0.6s 0.5s ease-out forwards;
  }
  .dot {
    width: 32px;
    height: 4px;
    background: var(--hairline);
    border-radius: 2px;
    transition: background 0.5s ease, width 0.5s ease;
  }
  .dot.active { background: var(--navy); width: 44px; }
  .dot.done   { background: var(--navy-soft); }
  .dot.final  { background: var(--red); }

  @keyframes fadeDown {
    to { opacity: 1; transform: translateY(0); }
  }
  @keyframes fadeIn {
    to { opacity: 1; }
  }
```

- [ ] **Step 2: Tambah markup header di dalam `.stage`**

Ganti komentar `<!-- header + role-row + timeline ... -->` jadi:

```html
    <div class="header">
      <div class="title-block">
        <div class="eyebrow">Track &amp; Progresi Tahunan</div>
        <h1 class="title">Dua Track. Tiga Tahun. <em>Satu Tujuan.</em></h1>
      </div>
      <div class="progress-dots">
        <span class="dot" id="d1"></span>
        <span class="dot" id="d2"></span>
        <span class="dot" id="d3"></span>
      </div>
    </div>
```

- [ ] **Step 3: Verifikasi tampilan di browser**

Run python http.server seperti Task 1 Step 2. Buka URL. Expected: eyebrow merah uppercase di kiri-atas, title navy besar di bawahnya dengan kata "Satu Tujuan" warna merah, 3 dot abu-abu di kanan-atas. Eyebrow+title fade-down dari atas saat load.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "feat(proton-video): header — eyebrow + title + progress dots"
```

---

## Task 3: Role badges row — Panelman & Operator + divider

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah CSS role row**

Sisipkan di `<style>` setelah block `.dot.final`:

```css
  /* ===== ROLE ROW ===== */
  .role-row {
    display: grid;
    grid-template-columns: 1fr 1px 1fr;
    gap: clamp(20px, 2vw, 40px);
    align-items: center;
    margin-bottom: clamp(24px, 2.5vw, 40px);
    opacity: 0;
    animation: fadeUp 0.9s 0.9s cubic-bezier(0.2, 0, 0.2, 1) forwards;
  }
  .role {
    display: flex;
    align-items: center;
    gap: 14px;
    justify-content: center;
  }
  .role .icon {
    width: clamp(40px, 3.4vw, 56px);
    height: clamp(40px, 3.4vw, 56px);
    border: 2px solid var(--navy);
    border-radius: 8px;
    display: grid;
    place-items: center;
    background: #FFFFFF;
    color: var(--navy);
    flex-shrink: 0;
  }
  .role .icon svg { width: 60%; height: 60%; }
  .role .label {
    font-size: clamp(14px, 1.2vw, 20px);
    font-weight: 800;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--navy-deep);
  }
  .role-divider {
    width: 1px;
    height: clamp(40px, 3.6vw, 60px);
    background: linear-gradient(180deg, transparent, var(--hairline) 30%, var(--hairline) 70%, transparent);
  }

  @keyframes fadeUp {
    to { opacity: 1; transform: translateY(0); }
  }
  .role-row { transform: translateY(12px); }
```

- [ ] **Step 2: Tambah markup role row setelah `.header`**

Sisipkan di dalam `.stage`, setelah `<div class="header">…</div>`:

```html
    <div class="role-row">
      <div class="role">
        <div class="icon" aria-label="Panelman">
          <!-- ikon panel/HMI: grid 2x3 cells -->
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="3" y="4" width="18" height="16" rx="1.5"/>
            <line x1="3" y1="10" x2="21" y2="10"/>
            <line x1="9" y1="10" x2="9" y2="20"/>
            <line x1="15" y1="10" x2="15" y2="20"/>
          </svg>
        </div>
        <div class="label">Panelman</div>
      </div>
      <div class="role-divider" aria-hidden="true"></div>
      <div class="role">
        <div class="icon" aria-label="Operator">
          <!-- ikon gear -->
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="3"/>
            <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33h0a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51h0a1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82v0a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
          </svg>
        </div>
        <div class="label">Operator</div>
      </div>
    </div>
```

- [ ] **Step 3: Verifikasi visual di browser**

Refresh. Expected: 2 ikon role (panel grid kiri, gear kanan) dengan label "PANELMAN" dan "OPERATOR" uppercase, garis vertikal tipis di tengah antara keduanya. Muncul fade-up dengan delay setelah header.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "feat(proton-video): role row — Panelman + Operator ikon abstrak + divider"
```

---

## Task 4: Timeline 3 kartu tahun — markup + styling static

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah CSS timeline**

Sisipkan setelah block `.role-divider`:

```css
  /* ===== TIMELINE ===== */
  .timeline {
    flex: 1;
    display: grid;
    grid-template-columns: 1fr auto 1fr auto 1fr;
    align-items: stretch;
    gap: clamp(12px, 1.2vw, 24px);
  }
  .year-card {
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-top: 4px solid var(--navy);
    border-radius: 10px;
    padding: clamp(18px, 1.8vw, 28px) clamp(20px, 2vw, 32px);
    display: flex;
    flex-direction: column;
    gap: clamp(10px, 1vw, 16px);
    box-shadow: 0 1px 2px rgba(10,36,71,0.03), 0 8px 24px rgba(10,36,71,0.04);
    opacity: 0;
    transform: translateY(20px) scale(0.97);
  }
  .year-card.y1 { border-top-color: var(--navy); }
  .year-card.y2 { border-top-color: var(--navy-soft); }
  .year-card.y3 { border-top-color: var(--red); }

  .year-badge {
    display: inline-block;
    padding: 4px 10px;
    border-radius: 4px;
    background: var(--navy);
    color: #FFFFFF;
    font-size: clamp(10px, 0.85vw, 13px);
    font-weight: 800;
    letter-spacing: 0.15em;
    text-transform: uppercase;
    width: fit-content;
  }
  .year-card.y2 .year-badge { background: var(--navy-soft); }
  .year-card.y3 .year-badge { background: var(--red); }

  .year-theme {
    font-size: clamp(15px, 1.4vw, 22px);
    font-weight: 800;
    color: var(--navy-deep);
    letter-spacing: 0.05em;
    text-transform: uppercase;
  }
  .year-card.y3 .year-theme { color: var(--red-dark); }

  .chips {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-top: 4px;
  }
  .chip {
    background: var(--paper);
    border: 1px solid var(--hairline);
    border-radius: 6px;
    padding: 6px 10px;
    font-size: clamp(11px, 0.95vw, 14px);
    font-weight: 600;
    color: var(--text);
    opacity: 0;
    transform: translateY(6px);
  }
  .year-card.y3 .chip { border-color: rgba(230,51,41,0.18); }

  .year-card .footer-line {
    margin-top: auto;
    padding-top: clamp(10px, 1vw, 16px);
    border-top: 1px dashed var(--hairline);
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: clamp(11px, 0.85vw, 13px);
    font-weight: 700;
    color: var(--navy-soft);
    text-transform: uppercase;
    letter-spacing: 0.1em;
    opacity: 0;
  }
  .year-card.y3 .footer-line { color: var(--red); }
  .footer-line .check { font-size: 1.2em; }

  /* Arrow connector antar kartu */
  .arrow {
    align-self: center;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
    color: var(--muted);
    font-size: clamp(10px, 0.8vw, 12px);
    font-weight: 700;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    opacity: 0;
  }
  .arrow svg { width: clamp(28px, 2.4vw, 40px); height: auto; color: var(--navy-soft); }
```

- [ ] **Step 2: Tambah markup timeline setelah role row**

Sisipkan di dalam `.stage`, setelah `<div class="role-row">…</div>`:

```html
    <div class="timeline">
      <div class="year-card y1" id="card-y1">
        <span class="year-badge">Tahun 1</span>
        <div class="year-theme">Foundation</div>
        <div class="chips" id="chips-y1">
          <div class="chip">Safe Work Practice</div>
          <div class="chip">Refinery Ops · Basic</div>
        </div>
        <div class="footer-line" id="foot-y1">Lulus &rarr;</div>
      </div>

      <div class="arrow" id="arrow-1">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
          <line x1="4" y1="12" x2="20" y2="12"/>
          <polyline points="14 6 20 12 14 18"/>
        </svg>
        <span>Naik Tahun</span>
      </div>

      <div class="year-card y2" id="card-y2">
        <span class="year-badge">Tahun 2</span>
        <div class="year-theme">Pendalaman</div>
        <div class="chips" id="chips-y2">
          <div class="chip">Energy Management</div>
          <div class="chip">Catalyst &amp; Chemical</div>
          <div class="chip">Refinery Ops · Sub-Proses</div>
        </div>
        <div class="footer-line" id="foot-y2">Lulus &rarr;</div>
      </div>

      <div class="arrow" id="arrow-2">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
          <line x1="4" y1="12" x2="20" y2="12"/>
          <polyline points="14 6 20 12 14 18"/>
        </svg>
        <span>Naik Tahun</span>
      </div>

      <div class="year-card y3" id="card-y3">
        <span class="year-badge">Tahun 3</span>
        <div class="year-theme">Mastery</div>
        <div class="chips" id="chips-y3">
          <div class="chip">Process Control</div>
          <div class="chip">Refinery Ops · Optimization</div>
        </div>
        <div class="footer-line" id="foot-y3"><span class="check">&#10004;</span> Kompetensi Tervalidasi</div>
      </div>
    </div>
```

- [ ] **Step 3: Tambah footer brand (dibawah timeline)**

Sisipkan CSS di `<style>` (setelah block `.arrow svg`):

```css
  /* ===== FOOTER ===== */
  .brand-footer {
    margin-top: clamp(20px, 2vw, 32px);
    text-align: center;
    font-size: clamp(10px, 0.78vw, 12px);
    font-weight: 600;
    color: var(--muted);
    letter-spacing: 0.15em;
    text-transform: uppercase;
    opacity: 0;
    animation: fadeIn 0.8s 1.3s ease-out forwards;
  }
  .brand-footer .dot-sep { display: inline-block; margin: 0 8px; color: var(--hairline); }
```

Sisipkan markup di dalam `.stage` (setelah `<div class="timeline">…</div>`):

```html
    <div class="brand-footer">
      PROTON <span class="dot-sep">•</span> Program Coaching Pekerja KPB
    </div>
```

- [ ] **Step 4: Verifikasi tampilan di browser**

Refresh. Expected: 3 kartu tahun horizontal dengan badge tahun, tema (FOUNDATION/PENDALAMAN/MASTERY), chip kompetensi list, footer "Lulus →" (Y3: ✓). Antar kartu ada panah dengan label "NAIK TAHUN". Y3 ada accent merah (border-top + badge merah). Saat ini kartu **semua opacity:0** (akan dianimasikan di Task 5). Footer brand line muncul di bawah.

Karena animation belum disetel, kartu masih invisible. Untuk verifikasi visual sementara, tambahkan inline style `style="opacity:1; transform:none"` di salah satu `.year-card` sementara, lihat tampilannya, kemudian hapus inline style sebelum commit. Atau buka DevTools dan inspect element.

- [ ] **Step 5: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "feat(proton-video): timeline — 3 kartu tahun + arrow connector + brand footer"
```

---

## Task 5: Animation keyframes + timing 25 detik

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah keyframes untuk kartu, chip, arrow, footer**

Sisipkan di `<style>` (setelah `@keyframes fadeIn`):

```css
  /* ===== ANIMATION KEYFRAMES ===== */
  @keyframes cardIn {
    to { opacity: 1; transform: translateY(0) scale(1); }
  }
  @keyframes chipIn {
    to { opacity: 1; transform: translateY(0); }
  }
  @keyframes arrowIn {
    to { opacity: 1; }
  }
  @keyframes footerIn {
    to { opacity: 1; }
  }
```

- [ ] **Step 2: Set animation timing tiap elemen sesuai timeline 25 detik**

Sisipkan di `<style>` (setelah keyframes baru):

```css
  /* ===== ANIMATION DELAYS (timeline 25 detik) ===== */
  /* Kartu Y1: t=5.0s, duration 0.7s */
  #card-y1 { animation: cardIn 0.7s 5.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  /* Chip Y1 sequential stagger 120ms, start t=5.7s */
  #chips-y1 .chip:nth-child(1) { animation: chipIn 0.4s 5.7s ease-out forwards; }
  #chips-y1 .chip:nth-child(2) { animation: chipIn 0.4s 5.82s ease-out forwards; }
  /* Footer-line Y1: t=7.0s */
  #foot-y1 { animation: footerIn 0.5s 7.0s ease-out forwards; }
  /* Arrow 1: t=9.0s */
  #arrow-1 { animation: arrowIn 0.5s 9.0s ease-out forwards; }

  /* Kartu Y2: t=10.0s */
  #card-y2 { animation: cardIn 0.7s 10.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  /* Chip Y2 stagger */
  #chips-y2 .chip:nth-child(1) { animation: chipIn 0.4s 10.7s ease-out forwards; }
  #chips-y2 .chip:nth-child(2) { animation: chipIn 0.4s 10.82s ease-out forwards; }
  #chips-y2 .chip:nth-child(3) { animation: chipIn 0.4s 10.94s ease-out forwards; }
  /* Footer-line Y2: t=12.5s */
  #foot-y2 { animation: footerIn 0.5s 12.5s ease-out forwards; }
  /* Arrow 2: t=13.0s */
  #arrow-2 { animation: arrowIn 0.5s 13.0s ease-out forwards; }

  /* Kartu Y3: t=14.0s */
  #card-y3 { animation: cardIn 0.7s 14.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  /* Chip Y3 stagger */
  #chips-y3 .chip:nth-child(1) { animation: chipIn 0.4s 14.7s ease-out forwards; }
  #chips-y3 .chip:nth-child(2) { animation: chipIn 0.4s 14.82s ease-out forwards; }
  /* Footer-line Y3 (check): t=16.5s, sedikit pop */
  #foot-y3 {
    animation: footerIn 0.5s 16.5s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
  }
```

- [ ] **Step 3: Tambah JS minimal untuk animate progress dots**

Sisipkan tepat sebelum `</body>`:

```html
<script>
  // Progress dots — sync dengan munculnya tiap kartu tahun
  const milestones = [
    { delay: 5200, dot: 'd1', cls: 'active' },
    { delay: 9500, dot: 'd1', cls: 'done' },
    { delay: 9700, dot: 'd2', cls: 'active' },
    { delay: 13500, dot: 'd2', cls: 'done' },
    { delay: 13700, dot: 'd3', cls: 'active final' },
  ];
  milestones.forEach(m => {
    setTimeout(() => {
      const el = document.getElementById(m.dot);
      el.classList.remove('active', 'done', 'final');
      m.cls.split(' ').forEach(c => el.classList.add(c));
    }, m.delay);
  });
</script>
```

Catatan: skrip JS minimal hanya untuk dot state machine. Animasi inti tetap pure CSS.

- [ ] **Step 4: Verifikasi animasi end-to-end di browser**

Refresh halaman, langsung perhatikan urutan dari 0s:

| Waktu (s) | Yang harus terjadi |
|-----------|--------------------|
| 0–2 | Header (eyebrow + title) fade-down |
| 0.5 | Dot abu-abu muncul (semua belum aktif) |
| ~1 | Role row fade-up (Panelman + Operator + divider) |
| ~5 | Kartu Y1 pop-up + dot 1 jadi navy (active) |
| ~5.7–5.9 | Chip Y1 muncul satu per satu |
| ~7 | Footer "Lulus →" Y1 muncul |
| ~9 | Arrow 1 muncul + dot 1 jadi navy-soft (done), dot 2 active |
| ~10 | Kartu Y2 pop-up |
| ~10.7–11 | Chip Y2 muncul stagger |
| ~12.5 | Footer "Lulus →" Y2 muncul |
| ~13 | Arrow 2 muncul + dot 2 done, dot 3 active red |
| ~14 | Kartu Y3 pop-up dengan accent merah |
| ~14.7–15 | Chip Y3 muncul stagger |
| ~16.5 | Footer ✓ Y3 muncul |
| 16.5–25 | Hold semua visible (final frame poster) |

Pastikan semua kartu pop-in mulus (soft bounce dari translateY+scale), chip muncul sequential, dan tidak ada layout shift.

- [ ] **Step 5: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "feat(proton-video): animation timeline 25s — cards, chips, arrows, dots sync"
```

---

## Task 6: Record script Playwright → WebM

**Files:**
- Create: `docs/assets/proton-video/record-track-progresi.mjs`

- [ ] **Step 1: Tulis script record**

Tulis `docs/assets/proton-video/record-track-progresi.mjs` dengan isi:

```javascript
// Record track-progresi.html to WebM via Playwright
// Usage: node docs/assets/proton-video/record-track-progresi.mjs
//
// Output: docs/assets/proton-video/track-progresi.webm (26s, 1920x1080)
// Convert to MP4: ffmpeg -i track-progresi.webm -c:v libx264 -crf 18 -preset slow track-progresi.mp4

import { chromium } from 'playwright';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import http from 'node:http';

const here = path.dirname(fileURLToPath(import.meta.url));
const PORT = 8767;
const URL = `http://127.0.0.1:${PORT}/track-progresi.html`;
const OUT_DIR = here;
const RECORD_DURATION_MS = 26_000; // 25s animation + 1s buffer

// 1. Start http server
const server = spawn(
  process.platform === 'win32' ? 'python.exe' : 'python',
  ['-m', 'http.server', String(PORT), '--bind', '127.0.0.1'],
  { cwd: here, stdio: 'ignore' }
);

async function waitForServer(url, timeout = 5000) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    try {
      await new Promise((resolve, reject) => {
        const req = http.get(url, res => { res.resume(); resolve(); });
        req.on('error', reject);
        req.setTimeout(500, () => { req.destroy(); reject(new Error('timeout')); });
      });
      return;
    } catch {
      await new Promise(r => setTimeout(r, 200));
    }
  }
  throw new Error('Server did not start in time');
}

try {
  await waitForServer(URL);
  console.log('[record] Server ready, launching browser...');

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    deviceScaleFactor: 1,
    recordVideo: {
      dir: OUT_DIR,
      size: { width: 1920, height: 1080 }
    }
  });

  const page = await context.newPage();
  await page.goto(URL, { waitUntil: 'domcontentloaded' });

  console.log(`[record] Recording ${RECORD_DURATION_MS / 1000}s animation...`);
  await page.waitForTimeout(RECORD_DURATION_MS);

  const videoPath = await page.video()?.path();
  await context.close();
  await browser.close();

  console.log('[record] Video saved (raw):', videoPath);

  const fs = await import('node:fs/promises');
  const finalPath = path.join(OUT_DIR, 'track-progresi.webm');
  await fs.rename(videoPath, finalPath);
  console.log('[record] Final path:', finalPath);
} catch (err) {
  console.error('[record] ERROR:', err);
  process.exitCode = 1;
} finally {
  server.kill('SIGTERM');
}
```

- [ ] **Step 2: Commit script**

```bash
git add docs/assets/proton-video/record-track-progresi.mjs
git commit -m "feat(proton-video): record script — Playwright → track-progresi.webm"
```

---

## Task 7: Record + verify output

**Files:**
- Generate: `docs/assets/proton-video/track-progresi.webm`

- [ ] **Step 1: Cek Playwright + browser installed**

Run:
```bash
node -e "import('playwright').then(m => console.log('playwright ok'))"
```
Expected output: `playwright ok`.

Kalau error "Cannot find package 'playwright'", install dulu:
```bash
npm install --no-save playwright
npx playwright install chromium
```

- [ ] **Step 2: Run record script**

Run:
```bash
node "docs/assets/proton-video/record-track-progresi.mjs"
```

Expected log:
```
[record] Server ready, launching browser...
[record] Recording 26s animation...
[record] Video saved (raw): <some temp .webm path>
[record] Final path: .../docs/assets/proton-video/track-progresi.webm
```

Durasi total ~30-35 detik (start server + browser + recording 26s + cleanup).

- [ ] **Step 3: Verifikasi file output ada & size masuk akal**

Run:
```bash
ls -la docs/assets/proton-video/track-progresi.webm
```

Expected: file ada, size 500 KB – 5 MB range (tergantung kompleksitas visual). Kalau < 100 KB → kemungkinan record gagal/blank.

Buka file di video player (VLC, Chrome drag-drop). Pastikan:
- Durasi ~26 detik
- Visual sesuai timeline (header → role row → Y1 → Y2 → Y3 sequential)
- Tidak ada glitch / frame hilang
- Resolusi 1920×1080

- [ ] **Step 4: Commit hasil record**

```bash
git add docs/assets/proton-video/track-progresi.webm
git commit -m "chore(proton-video): record track-progresi.webm (26s, 1920x1080)"
```

---

## Task 8: Final verification + cleanup

**Files:** none (verification only)

- [ ] **Step 1: Konfirmasi semua file yang seharusnya ada, sudah ada**

Run:
```bash
ls -la docs/assets/proton-video/track-progresi.html docs/assets/proton-video/track-progresi.webm docs/assets/proton-video/record-track-progresi.mjs
```

Expected: 3 file ada semua.

- [ ] **Step 2: Konfirmasi git status bersih (atau hanya untracked file lama)**

Run:
```bash
git status docs/assets/proton-video/
```

Expected: file lama (`track-progresi-animation.html`, `v-G.html`, `v-H.html`, `screenshot-*.mjs`, `track-progresi-animation.webm`) tetap untracked (tidak diubah). 3 file baru sudah committed.

- [ ] **Step 3: Spot-check spec compliance**

Buka spec `docs/superpowers/specs/2026-05-20-proton-track-progresi-animation-design.md` section 10 (Success criteria). Cek satu per satu:

- [ ] Durasi 25 detik exact → record duration 26s, animation berakhir ~17s, hold sampai ~25s ✓
- [ ] 5 nama kompetensi tampil → cek HTML: Safe Work + Refinery Ops Basic (Y1), Energy + Catalyst + Refinery Sub-Proses (Y2), Process Control + Refinery Opt (Y3) ✓
- [ ] 2 ikon role Panelman+Operator di header ✓
- [ ] Arrow "LULUS / NAIK TAHUN" antar tahun ✓
- [ ] Y3 accent red ✓
- [ ] Final frame stabil sebagai poster ✓ (hold 16.5–25s)
- [ ] Palette + typography konsisten Sosialisasi v2 (navy + red + Inter) ✓
- [ ] Record-able lewat Playwright pattern existing ✓

- [ ] **Step 4: Final commit kalau ada perubahan minor**

Kalau ada tweak akhir:
```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "polish(proton-video): final tweak track-progresi animation"
```

---

## Definition of Done

- Semua 7 task selesai dan commit per task.
- `track-progresi.html` standalone, di-render benar di browser modern, animasi smooth 25 detik.
- `track-progresi.webm` ter-record, durasi 26 detik, resolusi 1920×1080, file size 500 KB – 5 MB.
- `record-track-progresi.mjs` runnable ulang kapan saja (idempotent).
- Semua spec success criteria (section 10) terpenuhi.
- Spec file `2026-05-20-proton-track-progresi-animation-design.md` tetap mengikuti hasil akhir (kalau ada deviasi, update spec sebelum done).
