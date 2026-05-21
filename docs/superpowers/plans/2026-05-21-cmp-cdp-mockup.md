# CMP & CDP Mockup Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bangun animasi HTML 16:9 standalone PROTON video — 2 scene cross-fade real mockup Portal HC KPB (Scene CMP 3 card + Scene CDP 4 card), durasi 20s. Match exact Bootstrap card style dari real `Views/CMP/Index.cshtml` + `Views/CDP/Index.cshtml`. Record `.webm` lewat Playwright.

**Architecture:** Single file `docs/assets/proton-video/cmp-cdp-mockup.html`. Portal-frame chrome (navbar+breadcrumb+page title) carry from assessment-akhir pattern. Bootstrap card primitive CSS dengan `.bs-card`, `.icon-box.{color}`, `.action.{color}` variants. 2 scene cross-fade dengan stagger card pop + sequential highlight pulse. Bootstrap Icons via CDN. JS inline ~30 LOC orchestration.

**Tech Stack:** HTML5 + CSS3 (`@keyframes`, custom props, `clamp`) + vanilla JS (`setTimeout`, `matchMedia`) + Inter Google Fonts + Bootstrap-Icons CDN (`https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css`) + Playwright chromium + Python `http.server`.

**Spec reference:** `docs/superpowers/specs/2026-05-21-cmp-cdp-mockup-design.md`

---

## File Structure

| File | Status | Tanggung jawab |
|------|--------|----------------|
| `docs/assets/proton-video/cmp-cdp-mockup.html` | CREATE | Animasi 20s, 2 scene cross-fade, CSS+JS inline + Bootstrap-Icons CDN |
| `docs/assets/proton-video/record-cmp-cdp-mockup.mjs` | CREATE | Playwright record 21s, port 8770 |
| `docs/assets/proton-video/cmp-cdp-mockup.webm` | GENERATE | Output 20s, 1920×1080, ~2-3 MB |
| `docs/Naskah Video PROTON.docx` | NO CHANGE | Out-of-scope |

---

## Verification Approach

Manual visual check di browser (no unit tests).

1. Start `python -m http.server 8800` di `docs/assets/proton-video/` (Task 0).
2. Buka `http://localhost:8800/cmp-cdp-mockup.html` Chrome.
3. Refresh setelah tiap edit, inspect class state via DevTools.
4. DevTools Rendering simulate `prefers-reduced-motion: reduce`.

---

## Task 0: Setup — verify Playwright + start server

**Files:** none

- [ ] **Step 1: Verify Playwright**

```bash
node -e "import('playwright').then(m => console.log('playwright ok'))"
```

Expected: `playwright ok`.

- [ ] **Step 2: Start http server background**

Terminal terpisah:
```bash
cd "docs/assets/proton-video" && python -m http.server 8800
```

**No commit.**

---

## Task 1: Scaffold HTML — stage + palette + grid + header + brand mark + Bootstrap-Icons CDN

**Files:**
- Create: `docs/assets/proton-video/cmp-cdp-mockup.html`

- [ ] **Step 1: Tulis file dengan skeleton**

Tulis `docs/assets/proton-video/cmp-cdp-mockup.html`:

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8">
<title>Animasi CMP & CDP Mockup — PROTON Video</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap" rel="stylesheet">
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
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

  .stage {
    width: min(96vw, 1600px);
    aspect-ratio: 16 / 9;
    position: relative;
    background: linear-gradient(180deg, #FFFFFF 0%, var(--sky) 100%);
    border: 1px solid var(--hairline);
    box-shadow: 0 1px 2px rgba(0,0,0,0.02), 0 24px 64px rgba(10,36,71,0.06);
    border-radius: 8px;
    overflow: hidden;
    padding: clamp(28px, 3vw, 48px) clamp(36px, 4vw, 64px);
    display: flex;
    flex-direction: column;
  }

  .stage::before {
    content: "";
    position: absolute;
    inset: 0;
    background-image:
      linear-gradient(rgba(15,45,92,0.04) 1px, transparent 1px),
      linear-gradient(90deg, rgba(15,45,92,0.04) 1px, transparent 1px);
    background-size: 40px 40px;
    pointer-events: none;
    opacity: 0;
    animation: fadeIn 0.4s 0.1s ease-out forwards;
    z-index: 0;
  }
  .stage > * { position: relative; z-index: 1; }

  .header {
    margin-bottom: clamp(12px, 1.5vw, 24px);
    opacity: 0;
    transform: translateY(-8px);
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
    font-size: clamp(24px, 2.2vw, 38px);
    font-weight: 900;
    color: var(--navy-deep);
    line-height: 1.1;
    letter-spacing: -0.01em;
  }
  .title em {
    font-style: normal;
    color: var(--red);
    background-image: linear-gradient(90deg, var(--red), var(--red));
    background-size: 0% 6px;
    background-position: 0 92%;
    background-repeat: no-repeat;
    padding-bottom: 2px;
    animation: underlineDraw 0.6s 1.2s cubic-bezier(0.65, 0, 0.35, 1) forwards;
  }
  @keyframes underlineDraw { to { background-size: 100% 6px; } }
  @keyframes fadeDown { to { opacity: 1; transform: translateY(0); } }
  @keyframes fadeIn { to { opacity: 1; } }
  @keyframes fadeUp { to { opacity: 1; transform: translateY(0); } }

  .brand-mark {
    position: absolute;
    bottom: 20px;
    right: 28px;
    font-size: clamp(10px, 0.78vw, 12px);
    font-weight: 700;
    letter-spacing: 0.2em;
    color: var(--muted);
    opacity: 0;
    animation: fadeIn 0.8s 1.5s ease-out forwards;
    pointer-events: none;
  }
</style>
</head>
<body>
  <div class="stage" id="stage">
    <div class="header">
      <div class="eyebrow">Platform Portal HC KPB</div>
      <h1 class="title">Dua Platform <em>Inti</em></h1>
    </div>

    <!-- scenes container akan ditambahkan task berikutnya -->

    <div class="brand-mark">PROTON × KPB</div>
  </div>
</body>
</html>
```

- [ ] **Step 2: Visual verify**

Buka `http://localhost:8800/cmp-cdp-mockup.html`. Expected:
- Stage 16:9, grid backdrop
- Eyebrow red "PLATFORM PORTAL HC KPB"
- Title "Dua Platform Inti" dengan "Inti" red + underline draw red t=1.2s
- Brand mark PROTON × KPB pojok kanan-bawah

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scaffold cmp-cdp-mockup — stage + header + brand mark — task 1/11

Stage 16:9 max 1600px, grid backdrop hairline navy 4%, header eyebrow red +
title "Dua Platform Inti" em underline draw red 1.2s, brand mark PROTON × KPB.
Bootstrap-Icons CDN loaded di <head>. Match series visual identity.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Scene container + cross-fade + portal-frame CSS

**Files:**
- Modify: `docs/assets/proton-video/cmp-cdp-mockup.html`

- [ ] **Step 1: Tambah CSS scene + portal-frame**

Sisipkan di `<style>` setelah block `.brand-mark`:

```css
  /* ===== SCENES CONTAINER ===== */
  .scenes {
    flex: 1;
    position: relative;
    min-height: 0;
    margin: clamp(8px, 1vw, 16px) 0;
  }
  .scene {
    position: absolute;
    inset: 0;
    opacity: 0;
    transform: translateY(8px);
    transition: opacity 0.5s ease, transform 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
    pointer-events: none;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: clamp(12px, 1.5vw, 24px);
  }
  .scene.active {
    opacity: 1;
    transform: translateY(0);
  }

  /* ===== PORTAL FRAME (carry from assessment-akhir) ===== */
  .portal-frame {
    width: 100%;
    max-width: 1100px;
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 10px;
    box-shadow: 0 1px 2px rgba(10,36,71,0.03), 0 12px 32px rgba(10,36,71,0.06);
    overflow: hidden;
  }
  .portal-navbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 20px;
    background: #FFFFFF;
    border-bottom: 1px solid var(--hairline);
    box-shadow: 0 2px 4px rgba(10,36,71,0.03);
  }
  .portal-brand {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: clamp(13px, 1.1vw, 16px);
    font-weight: 800;
    color: var(--navy);
    letter-spacing: -0.01em;
  }
  .portal-brand-icon {
    width: clamp(18px, 1.6vw, 24px);
    height: clamp(18px, 1.6vw, 24px);
    fill: var(--navy);
  }
  .portal-nav {
    display: flex;
    gap: clamp(14px, 1.8vw, 28px);
    flex: 1;
    justify-content: center;
  }
  .portal-nav-item {
    font-size: clamp(11px, 0.95vw, 13px);
    font-weight: 500;
    color: var(--text);
    text-decoration: none;
  }
  .portal-nav-item.active { color: var(--navy); font-weight: 700; }
  .portal-user {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: clamp(11px, 0.9vw, 13px);
    color: var(--text);
    font-weight: 600;
  }
  .portal-avatar {
    width: clamp(26px, 2.2vw, 32px);
    height: clamp(26px, 2.2vw, 32px);
    border-radius: 50%;
    background: var(--navy);
    color: #FFFFFF;
    display: grid;
    place-items: center;
    font-weight: 800;
    font-size: clamp(9px, 0.8vw, 11px);
  }

  .portal-breadcrumb {
    padding: 10px 20px 4px;
    font-size: clamp(10px, 0.82vw, 12px);
    color: var(--muted);
    background: var(--paper);
    border-bottom: 1px solid var(--hairline);
  }
  .portal-breadcrumb .crumb { color: var(--muted); }
  .portal-breadcrumb .crumb.active { color: var(--navy-deep); font-weight: 700; }
  .portal-breadcrumb .sep { margin: 0 6px; opacity: 0.6; }

  .portal-title {
    padding: 14px 20px 12px;
    background: #FFFFFF;
    border-bottom: 1px solid var(--hairline);
  }
  .portal-title h2 {
    font-size: clamp(16px, 1.5vw, 22px);
    font-weight: 800;
    color: var(--navy-deep);
    margin-bottom: 2px;
    line-height: 1.2;
    display: flex;
    align-items: center;
    gap: 8px;
  }
  .portal-title h2 .bi {
    font-size: 1.1em;
    color: var(--navy);
  }
  .portal-title p {
    font-size: clamp(10px, 0.82vw, 12px);
    color: var(--muted);
    margin: 0;
  }

  .portal-body {
    padding: clamp(14px, 1.6vw, 22px) clamp(16px, 1.8vw, 24px);
    min-height: clamp(220px, 24vw, 320px);
    position: relative;
    background: #FFFFFF;
  }

  /* CDP scene body needs more vertical space for 2x2 grid */
  #scene-cdp .portal-body {
    min-height: clamp(280px, 30vw, 400px);
  }
```

- [ ] **Step 2: Tambah markup `.scenes` container**

Sisipkan di `.stage`, antara header dan brand-mark:

```html
    <div class="scenes">
      <!-- 2 scene akan ditambahkan task 4-5 -->
    </div>
```

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene container + portal-frame CSS — cmp-cdp-mockup task 2/11

.scenes flex-1 container + .scene cross-fade. Portal-frame chrome carry dari
assessment-akhir: navbar HC Portal + breadcrumb + page title (h2 with bi icon)
+ body. CDP scene body override min-height clamp 280-400px untuk 2x2 grid.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Bootstrap card primitive CSS

**Files:**
- Modify: `docs/assets/proton-video/cmp-cdp-mockup.html`

- [ ] **Step 1: Tambah CSS bs-card + variants**

Sisipkan di `<style>` setelah block `#scene-cdp .portal-body`:

```css
  /* ===== BOOTSTRAP CARD (mockup match real Portal) ===== */
  .bs-card {
    border: 0;
    border-radius: 8px;
    background: #FFFFFF;
    box-shadow: 0 1px 2px rgba(0,0,0,0.06), 0 4px 12px rgba(0,0,0,0.05);
    padding: clamp(14px, 1.6vw, 22px);
    display: flex;
    flex-direction: column;
    gap: clamp(10px, 1vw, 16px);
    opacity: 0;
    transform: translateY(12px) scale(0.96);
    transition: box-shadow 0.4s ease;
  }
  .bs-card-head {
    display: flex;
    align-items: center;
    gap: clamp(10px, 1.2vw, 16px);
  }
  .icon-box {
    width: clamp(40px, 4vw, 60px);
    height: clamp(40px, 4vw, 60px);
    border-radius: 8px;
    display: grid;
    place-items: center;
    font-size: clamp(20px, 2vw, 28px);
    flex-shrink: 0;
  }
  .icon-box.primary   { background: rgba(13,110,253,0.10); color: #0d6efd; }
  .icon-box.info      { background: rgba(13,202,240,0.12); color: #0dcaf0; }
  .icon-box.success   { background: rgba(25,135,84,0.10);  color: #198754; }
  .icon-box.warning   { background: rgba(255,193,7,0.14);  color: #b8870b; }
  .icon-box.secondary { background: rgba(108,117,125,0.10); color: #6c757d; }

  .bs-card .head-text h5 {
    font-size: clamp(13px, 1.1vw, 16px);
    font-weight: 700;
    color: var(--ink);
    line-height: 1.2;
    margin-bottom: 2px;
  }
  .bs-card .head-text small {
    font-size: clamp(10px, 0.82vw, 12px);
    color: var(--muted);
    font-weight: 600;
  }
  .bs-card .desc {
    font-size: clamp(11px, 0.9vw, 13px);
    color: var(--muted);
    line-height: 1.4;
  }
  .bs-card .action {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    padding: 8px 12px;
    border-radius: 6px;
    font-size: clamp(11px, 0.92vw, 13px);
    font-weight: 600;
    color: #FFFFFF;
    text-decoration: none;
    margin-top: auto;
  }
  .action.primary   { background: #0d6efd; }
  .action.info      { background: #0dcaf0; color: #053742; }
  .action.success   { background: #198754; }
  .action.warning   { background: #ffc107; color: #533f03; }
  .action.secondary { background: #6c757d; }

  .bs-card.highlight {
    box-shadow:
      0 1px 2px rgba(15,45,92,0.06),
      0 8px 20px rgba(15,45,92,0.12),
      0 0 0 3px rgba(230,51,41,0.18);
  }

  @keyframes cardPop {
    0%   { opacity: 0; transform: translateY(12px) scale(0.96); }
    100% { opacity: 1; transform: translateY(0) scale(1); }
  }

  .cmp-cards {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: clamp(12px, 1.4vw, 24px);
    width: 100%;
  }
  .cdp-cards {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    grid-template-rows: repeat(2, 1fr);
    gap: clamp(12px, 1.4vw, 24px);
    width: 100%;
  }
```

- [ ] **Step 2: Commit**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.html
git commit -m "$(cat <<'EOF'
feat(proton-video): bootstrap card primitive CSS — cmp-cdp-mockup task 3/11

.bs-card with .bs-card-head (icon-box + head-text), .desc, .action. Icon-box
variants primary/info/success/warning/secondary with Bootstrap bg-opacity-10
+ matching text color. Action button variants. .bs-card.highlight = red ring
(rgba 230,51,41 outer). Grid layouts .cmp-cards (1x3) + .cdp-cards (2x2).
cardPop keyframe untuk stagger reveal.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Scene CMP markup (portal-frame + 3 card grid 1×3)

**Files:**
- Modify: `docs/assets/proton-video/cmp-cdp-mockup.html`

- [ ] **Step 1: Tambah markup scene-cmp di `.scenes` container**

Sisipkan di dalam `.scenes`:

```html
      <div class="scene" id="scene-cmp">
        <div class="portal-frame">
          <div class="portal-navbar">
            <div class="portal-brand">
              <svg class="portal-brand-icon" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M3 21V7l9-4v4h9v14h-7v-6h-4v6H3zm2-2h5v-4h6v4h5V9h-9V5.5L5 8.45V19z"/>
              </svg>
              <span>HC Portal</span>
            </div>
            <div class="portal-nav">
              <a class="portal-nav-item">Beranda</a>
              <a class="portal-nav-item active">CMP</a>
              <a class="portal-nav-item">Histori</a>
            </div>
            <div class="portal-user">
              <div class="portal-avatar">AP</div>
              <span>Andi P.</span>
            </div>
          </div>
          <div class="portal-breadcrumb">
            <span class="crumb">Beranda</span><span class="sep">›</span><span class="crumb active">CMP</span>
          </div>
          <div class="portal-title">
            <h2><i class="bi bi-mortarboard"></i>Competency Management Platform</h2>
            <p>Kelola kompetensi, assessment, dan rencana pengembangan</p>
          </div>
          <div class="portal-body">
            <div class="cmp-cards">
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box info"><i class="bi bi-clipboard-check"></i></div>
                  <div class="head-text"><h5>Assessment Saya</h5><small>Ujian &amp; Evaluasi</small></div>
                </div>
                <div class="desc">Ikuti assessment, lihat hasil, dan pantau riwayat evaluasi Anda</div>
                <a class="action info">Assessment Saya <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box success"><i class="bi bi-patch-check"></i></div>
                  <div class="head-text"><h5>Manajemen Sertifikasi</h5><small>Kelola Sertifikat</small></div>
                </div>
                <div class="desc">Lihat dan kelola semua sertifikat pelatihan dan asesmen pekerja</div>
                <a class="action success">Kelola Sertifikat <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box secondary"><i class="bi bi-journal-text"></i></div>
                  <div class="head-text"><h5>Riwayat Pelatihan</h5><small>Catatan &amp; Pengembangan</small></div>
                </div>
                <div class="desc">Lihat dan kelola riwayat pelatihan serta catatan penilaian</div>
                <a class="action secondary">Lihat Riwayat <i class="bi bi-arrow-right-circle"></i></a>
              </div>
            </div>
          </div>
        </div>
      </div>
```

- [ ] **Step 2: Smoke test scene-cmp**

Sementara tambah `.active` class di scene-cmp untuk verify visual:
```html
<div class="scene active" id="scene-cmp">
```

Refresh. Expected:
- Browser frame mockup tampil
- Navbar: HC Portal + buildings icon + nav (Beranda · **CMP** · Histori) + avatar AP Andi P.
- Breadcrumb "Beranda › CMP"
- Title "Competency Management Platform" dengan bi-mortarboard icon + subtitle
- 3 card 1×3 grid: Assessment Saya (info cyan), Manajemen Sertifikasi (success green), Riwayat Pelatihan (secondary gray)
- Tapi card opacity 0 (animation belum diset di task 6) — bisa pakai DevTools force opacity 1 sementara untuk verify content

**REMOVE `.active`** setelah verify:
```html
<div class="scene" id="scene-cmp">
```

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-cmp markup + 3 card grid 1x3 — cmp-cdp-mockup task 4/11

Portal-frame "Beranda › CMP" + title "Competency Management Platform"
(bi-mortarboard). 3 card: Assessment Saya (info clipboard-check), Manajemen
Sertifikasi (success patch-check), Riwayat Pelatihan (secondary journal-text).
Subtitle Riwayat Pelatihan tweaked ke "Catatan & Pengembangan" untuk bridge
narasi "catatan penilaian". Content match real Views/CMP/Index.cshtml.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Scene CDP markup (portal-frame + 4 card grid 2×2)

**Files:**
- Modify: `docs/assets/proton-video/cmp-cdp-mockup.html`

- [ ] **Step 1: Tambah markup scene-cdp setelah scene-cmp**

Sisipkan setelah scene-cmp di `.scenes`:

```html
      <div class="scene" id="scene-cdp">
        <div class="portal-frame">
          <div class="portal-navbar">
            <div class="portal-brand">
              <svg class="portal-brand-icon" viewBox="0 0 24 24" aria-hidden="true">
                <path d="M3 21V7l9-4v4h9v14h-7v-6h-4v6H3zm2-2h5v-4h6v4h5V9h-9V5.5L5 8.45V19z"/>
              </svg>
              <span>HC Portal</span>
            </div>
            <div class="portal-nav">
              <a class="portal-nav-item">Beranda</a>
              <a class="portal-nav-item active">CDP</a>
              <a class="portal-nav-item">Histori</a>
            </div>
            <div class="portal-user">
              <div class="portal-avatar">AP</div>
              <span>Andi P.</span>
            </div>
          </div>
          <div class="portal-breadcrumb">
            <span class="crumb">Beranda</span><span class="sep">›</span><span class="crumb active">CDP</span>
          </div>
          <div class="portal-title">
            <h2><i class="bi bi-person-workspace"></i>Competency Development Portal</h2>
            <p>Kelola rencana pengembangan individu, coaching, dan pemantauan progres</p>
          </div>
          <div class="portal-body">
            <div class="cdp-cards">
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box primary"><i class="bi bi-file-earmark-pdf"></i></div>
                  <div class="head-text"><h5>Individual Development Plan</h5><small>Dokumen Silabus</small></div>
                </div>
                <div class="desc">Lihat dokumen kurikulum dan rencana pengembangan individu Proton</div>
                <a class="action primary">Lihat Dokumen <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box warning"><i class="bi bi-graph-up-arrow"></i></div>
                  <div class="head-text"><h5>Proton Coaching</h5><small>Status IDP</small></div>
                </div>
                <div class="desc">Pantau progres deliverable Proton dan status persetujuan</div>
                <a class="action warning">Proton Coaching <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box info"><i class="bi bi-cloud-upload"></i></div>
                  <div class="head-text"><h5>Deliverable</h5><small>Bukti Pekerjaan</small></div>
                </div>
                <div class="desc">Upload dan kelola bukti pekerjaan deliverable per tugas</div>
                <a class="action info">Kelola Deliverable <i class="bi bi-arrow-right-circle"></i></a>
              </div>
              <div class="bs-card">
                <div class="bs-card-head">
                  <div class="icon-box success"><i class="bi bi-patch-check"></i></div>
                  <div class="head-text"><h5>Manajemen Sertifikasi</h5><small>Sertifikat Kompetensi</small></div>
                </div>
                <div class="desc">Lihat dan kelola sertifikat kompetensi pekerja</div>
                <a class="action success">Kelola Sertifikat <i class="bi bi-arrow-right-circle"></i></a>
              </div>
            </div>
          </div>
        </div>
      </div>
```

- [ ] **Step 2: Smoke test scene-cdp**

Add `.active` ke scene-cdp sementara:
```html
<div class="scene active" id="scene-cdp">
```

Refresh. Expected:
- Portal-frame breadcrumb "Beranda › CDP", title "Competency Development Portal" (bi-person-workspace)
- 4 card 2×2 grid: IDP (primary blue), Proton Coaching (warning orange), Deliverable (info cyan), Manajemen Sertifikasi (success green)
- Card opacity 0 default (animation task 6)

**REMOVE `.active`** setelah verify.

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-cdp markup + 4 card grid 2x2 — cmp-cdp-mockup task 5/11

Portal-frame "Beranda › CDP" + title "Competency Development Portal"
(bi-person-workspace). 4 card: IDP (primary file-pdf), Proton Coaching
(warning graph-up), Deliverable (info cloud-upload), Manajemen Sertifikasi
(success patch-check). Content match real Views/CDP/Index.cshtml + Deliverable
view (yang real-nya accessed via Proton Coaching). 2x2 grid layout.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Card stagger reveal CSS

**Files:**
- Modify: `docs/assets/proton-video/cmp-cdp-mockup.html`

- [ ] **Step 1: Tambah CSS stagger reveal scoped #scene-X.active**

Sisipkan di `<style>` setelah block `.cdp-cards`:

```css
  /* ===== CARD STAGGER REVEAL ===== */
  /* CMP 3 cards stagger 400ms */
  #scene-cmp.active .cmp-cards .bs-card:nth-child(1) { animation: cardPop 0.5s 1.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #scene-cmp.active .cmp-cards .bs-card:nth-child(2) { animation: cardPop 0.5s 1.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #scene-cmp.active .cmp-cards .bs-card:nth-child(3) { animation: cardPop 0.5s 1.8s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }

  /* CDP 4 cards stagger 400ms */
  #scene-cdp.active .cdp-cards .bs-card:nth-child(1) { animation: cardPop 0.5s 1.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #scene-cdp.active .cdp-cards .bs-card:nth-child(2) { animation: cardPop 0.5s 1.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #scene-cdp.active .cdp-cards .bs-card:nth-child(3) { animation: cardPop 0.5s 1.8s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #scene-cdp.active .cdp-cards .bs-card:nth-child(4) { animation: cardPop 0.5s 2.2s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
```

- [ ] **Step 2: Commit**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.html
git commit -m "$(cat <<'EOF'
feat(proton-video): card stagger reveal CSS — cmp-cdp-mockup task 6/11

Stagger 400ms apiece scoped #scene-X.active. CMP 3 card delays 1.0/1.4/1.8s,
CDP 4 card delays 1.0/1.4/1.8/2.2s. cardPop 0.5s spring keyframe. Cards fire
saat scene activate via JS task 8.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Card highlight CSS sudah ada — verify

**Files:**
- Modify: `docs/assets/proton-video/cmp-cdp-mockup.html`

Task 3 sudah include `.bs-card.highlight` rule. Verify exists tanpa edit.

- [ ] **Step 1: Verify `.bs-card.highlight` exists**

Run:
```bash
grep "bs-card.highlight" docs/assets/proton-video/cmp-cdp-mockup.html
```

Expected match: `.bs-card.highlight {` rule found.

- [ ] **Step 2: Smoke test highlight class manual**

Sementara add `.active` ke scene-cmp dan `.highlight` ke salah satu card untuk verify visual:
```html
<div class="scene active" id="scene-cmp">
  ...
  <div class="bs-card highlight">  <!-- temp test -->
```

Refresh. Expected: card terpilih ada red ring outer (rgba 230,51,41 box-shadow 3px).

**REMOVE temp test classes** setelah verify.

**No commit (verification only).**

---

## Task 8: JS orchestration — milestones + showScene + highlightCard

**Files:**
- Modify: `docs/assets/proton-video/cmp-cdp-mockup.html`

- [ ] **Step 1: Tambah `<script>` sebelum `</body>`**

Sisipkan tepat sebelum `</body>`:

```html
<script>
  // ===== CMP & CDP MOCKUP — ORCHESTRATION =====

  function showScene(id) {
    document.querySelectorAll('.scene').forEach(s => s.classList.remove('active'));
    document.getElementById(id).classList.add('active');
  }

  function highlightCard(sceneId, idx, duration = 800) {
    const card = document.querySelectorAll(`#${sceneId} .bs-card`)[idx - 1];
    if (!card) return;
    card.classList.add('highlight');
    setTimeout(() => card.classList.remove('highlight'), duration);
  }

  const milestones = [
    // Scene CMP (2-10s)
    { t: 2000, fn: () => showScene('scene-cmp') },
    { t: 4500, fn: () => highlightCard('scene-cmp', 1) },
    { t: 5300, fn: () => highlightCard('scene-cmp', 2) },
    { t: 6100, fn: () => highlightCard('scene-cmp', 3) },

    // Cross-fade to CDP (11s)
    { t: 11000, fn: () => showScene('scene-cdp') },

    // CDP highlight sequence (14-17s)
    { t: 14000, fn: () => highlightCard('scene-cdp', 1, 750) },
    { t: 14750, fn: () => highlightCard('scene-cdp', 2, 750) },
    { t: 15500, fn: () => highlightCard('scene-cdp', 3, 750) },
    { t: 16250, fn: () => highlightCard('scene-cdp', 4, 750) }
  ];

  milestones.forEach(m => setTimeout(m.fn, m.t));
</script>
```

- [ ] **Step 2: Visual verify end-to-end**

Refresh. Watch 20s timeline:

| Time | Expected |
|------|----------|
| 0–2s | Header fade-down + em underline + brand mark + grid backdrop. Scenes hidden. |
| 2s | scene-cmp active, portal-frame fade-in |
| 3.0–4.6s | CMP 3 card pop sequential 400ms stagger |
| 4.5s | Card 1 (Assessment) highlight red ring 800ms |
| 5.3s | Card 2 (Sertifikasi) highlight |
| 6.1s | Card 3 (Riwayat) highlight |
| 11s | Cross-fade to scene-cdp |
| 12.0–14.2s | CDP 4 card pop sequential 400ms |
| 14.0/14.75/15.5/16.25s | CDP card highlights (750ms each) |
| 17–20s | Hold final state |

Console: no errors. DevTools inspect class state.

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.html
git commit -m "$(cat <<'EOF'
feat(proton-video): JS orchestration milestones — cmp-cdp-mockup task 8/11

showScene() toggle .active per scene + highlightCard(sceneId, idx, duration)
add/remove .highlight class. 9-entry milestone array drive 20s timeline:
CMP scene+highlights (2-10s) → cross-fade (11s) → CDP scene+highlights
(11-17s) → hold (17-20s). Default 800ms CMP highlights, 750ms CDP highlights.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: `prefers-reduced-motion` fallback

**Files:**
- Modify: `docs/assets/proton-video/cmp-cdp-mockup.html`

- [ ] **Step 1: Tambah `@media` query di `<style>`**

Sisipkan di akhir `<style>` (sebelum `</style>`):

```css
  /* ===== ACCESSIBILITY — REDUCED MOTION ===== */
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      animation-duration: 0.01ms !important;
      animation-delay: 0ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.01ms !important;
    }
    .scene { opacity: 0; }
    .scene#scene-cdp { opacity: 1; transform: none; }
    .title em { background-size: 100% 6px; }
    .bs-card { opacity: 1; transform: none; }
  }
```

- [ ] **Step 2: Update `<script>` dengan reduced-motion branch**

Replace `milestones.forEach(m => setTimeout(m.fn, m.t));` (akhir script) jadi:

```javascript
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (reduced) {
    showScene('scene-cdp');
    document.querySelectorAll('.bs-card').forEach(c => {
      c.style.opacity = '1';
      c.style.transform = 'none';
    });
  } else {
    milestones.forEach(m => setTimeout(m.fn, m.t));
  }
```

- [ ] **Step 3: Visual verify reduced motion**

DevTools → ⋮ → More tools → Rendering. "Emulate prefers-reduced-motion" → "reduce".

Refresh. Expected:
- No animation
- Scene-cdp final state visible immediate (4 card 2×2 grid CDP all visible)
- Em "Inti" underline already drawn
- No setTimeout chain firing

Toggle back "No preference" → full motion.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.html
git commit -m "$(cat <<'EOF'
a11y(proton-video): prefers-reduced-motion fallback — cmp-cdp-mockup task 9/11

CSS reduce animation/transition ke 0.01ms. Force scene-cdp visible sebagai
final state (4 card 2x2 + portal-frame). JS matchMedia branch: skip milestones,
langsung showScene cdp + force all bs-card opacity 1. Playwright record default
no-preference = full motion play.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: Record script `record-cmp-cdp-mockup.mjs`

**Files:**
- Create: `docs/assets/proton-video/record-cmp-cdp-mockup.mjs`

- [ ] **Step 1: Tulis script record**

Tulis `docs/assets/proton-video/record-cmp-cdp-mockup.mjs`:

```javascript
// Record cmp-cdp-mockup.html to WebM via Playwright
// Usage: node docs/assets/proton-video/record-cmp-cdp-mockup.mjs
//
// Output: docs/assets/proton-video/cmp-cdp-mockup.webm (21s, 1920x1080)
// Convert to MP4: ffmpeg -i cmp-cdp-mockup.webm -c:v libx264 -crf 18 -preset slow cmp-cdp-mockup.mp4

import { chromium } from 'playwright';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import http from 'node:http';

const here = path.dirname(fileURLToPath(import.meta.url));
const PORT = 8770;
const URL = `http://127.0.0.1:${PORT}/cmp-cdp-mockup.html`;
const OUT_DIR = here;
const RECORD_DURATION_MS = 21_000; // 20s animation + 1s buffer

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
  const finalPath = path.join(OUT_DIR, 'cmp-cdp-mockup.webm');
  await fs.rename(videoPath, finalPath);
  console.log('[record] Final path:', finalPath);
} catch (err) {
  console.error('[record] ERROR:', err);
  process.exitCode = 1;
} finally {
  server.kill('SIGTERM');
}
```

- [ ] **Step 2: Commit**

```bash
git add docs/assets/proton-video/record-cmp-cdp-mockup.mjs
git commit -m "$(cat <<'EOF'
feat(proton-video): record script Playwright → cmp-cdp-mockup.webm — task 10/11

Port 8770 (hindari konflik dengan series: 8767/8768/8769). Record 21s window
(20s + 1s buffer), viewport 1920×1080 chromium headless. Pattern sama dengan
record scripts series.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: Run record + verify webm output

**Files:**
- Generate: `docs/assets/proton-video/cmp-cdp-mockup.webm`

- [ ] **Step 1: Run record script**

```bash
node "docs/assets/proton-video/record-cmp-cdp-mockup.mjs"
```

Expected log:
```
[record] Server ready, launching browser...
[record] Recording 21s animation...
[record] Video saved (raw): <temp .webm path>
[record] Final path: .../docs/assets/proton-video/cmp-cdp-mockup.webm
```

Total command ~30-35s wall-clock.

- [ ] **Step 2: Verify output**

```bash
ls -la docs/assets/proton-video/cmp-cdp-mockup.webm
```

Expected: file ada, size 2-3 MB. <500 KB = record blank.

Playback Chrome (drag-drop). Verify:
- Durasi 20-21s
- Resolusi 1920×1080
- Visual sesuai task 8 timeline table
- Bootstrap Icons render (no fallback box)
- No glitch

- [ ] **Step 3: Commit hasil record**

```bash
git add docs/assets/proton-video/cmp-cdp-mockup.webm
git commit -m "$(cat <<'EOF'
chore(proton-video): generate cmp-cdp-mockup.webm 20s — task 11/11

PROTON video CMP + CDP mockup animasi 20s, 1920×1080 chromium headless. 2
scene cross-fade real Portal HC KPB mockup (3 CMP card + 4 CDP card 2x2).
Portal-frame chrome + Bootstrap card style + Bootstrap Icons via CDN.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 12: Final verification + DOD check

**Files:** none (verification)

- [ ] **Step 1: Manual SC verification per spec Section 12**

Verify 16 SC:

| SC | Verify | Pass? |
|----|--------|-------|
| SC1 | Durasi 20-21s, action density ≥85% | ☐ |
| SC2 | Em "Inti" underline draw red | ☐ |
| SC3 | Scene CMP portal-frame match real layout | ☐ |
| SC4 | CMP 3 card pop spring sequential | ☐ |
| SC5 | CMP card highlight pulse 3× (red ring) | ☐ |
| SC6 | Cross-fade CMP → CDP smooth | ☐ |
| SC7 | Scene CDP portal-frame | ☐ |
| SC8 | CDP 4 card 2×2 grid pop sequential | ☐ |
| SC9 | CDP card highlight pulse 4× | ☐ |
| SC10 | Card style exact Bootstrap (border-0 shadow + icon-box bg-opacity-10 + h5 + small + p + btn full) | ☐ |
| SC11 | Icon colors per card sesuai real (info/success/secondary/primary/warning) | ☐ |
| SC12 | Bootstrap Icons load (no fallback ☐ box) | ☐ |
| SC13 | DevTools reduced-motion → scene-cdp static visible | ☐ |
| SC14 | `wc -c cmp-cdp-mockup.html` < 32000 | ☐ |
| SC15 | `.webm` 20-21s, 2-3 MB | ☐ |
| SC16 | Re-run record idempotent | ☐ |

- [ ] **Step 2: File size check**

```bash
wc -c docs/assets/proton-video/cmp-cdp-mockup.html
```

Expected: <32000 bytes (target ~28-31 KB).

- [ ] **Step 3: Git status check**

```bash
git status docs/assets/proton-video/ docs/superpowers/
```

Expected:
- `cmp-cdp-mockup.html` — clean
- `record-cmp-cdp-mockup.mjs` — clean
- `cmp-cdp-mockup.webm` — clean
- Spec + plan file — clean

- [ ] **Step 4: Final spec update kalau ada deviasi**

Kalau ada deviasi, update spec + commit:

```bash
git add docs/superpowers/specs/2026-05-21-cmp-cdp-mockup-design.md
git commit -m "$(cat <<'EOF'
docs(proton-video): update spec — capture execution deviations

[describe deviations]

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

No deviasi, skip.

---

## Definition of Done

- [ ] All 11 task completed + atomic commits (Task 1-11; Task 0 + 7 + 12 no commit)
- [ ] All 16 SC pass
- [ ] `cmp-cdp-mockup.html` < 32 KB
- [ ] `cmp-cdp-mockup.webm` 2-3 MB, 20-21s playable
- [ ] `record-cmp-cdp-mockup.mjs` idempotent
- [ ] Naskah docx tidak di-touch
- [ ] `git status` bersih
- [ ] DevTools `prefers-reduced-motion: reduce` simulate → static visible

---

## Rollback (kalau gagal)

3 file baru: `cmp-cdp-mockup.html`, `record-cmp-cdp-mockup.mjs`, `cmp-cdp-mockup.webm`. Kalau gagal:

```bash
git log --oneline docs/assets/proton-video/cmp-cdp-mockup.html
git revert <task1-commit>..<lastcommit>
```

---

## Notes for Executor

- Task 0 setup harus jalan dulu (Playwright + http.server).
- Task 1-9 semua edit `cmp-cdp-mockup.html` single file → no parallelization. Sequential atomic commits.
- Task 7 verification-only (no edit, no commit).
- Task 10 record script independent file.
- Task 11 record command ~30-35s wall-clock.
- Task 12 verification — manual checklist.
- Smoke test scene-cmp/scene-cdp di task 4/5 ingat REMOVE `.active` setelah verify.
- Setiap commit subject ≤80 char, body caveman style.
