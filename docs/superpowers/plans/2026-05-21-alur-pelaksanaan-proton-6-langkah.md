# Alur Pelaksanaan PROTON (6 Langkah) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bangun animasi HTML 16:9 standalone untuk PROTON video bagian 4 (Alur Pelaksanaan PROTON · 6 Langkah, durasi 45s), record menjadi `.webm` lewat Playwright, match visual identity track-progresi (navy+red+Inter+grid backdrop).

**Architecture:** Single-file `docs/assets/proton-video/alur-pelaksanaan.html` dengan 7 scene (6 step + outro) hybrid stepper-bottom + scene-main-area cross-fade. CSS-only primitives (browser-frame, card pop, stamp drop, progress bar, toast, timeline backbone). SVG overlay untuk arrow draw (step 1) + timeline backbone (step 6). Inline JS ~85 LOC untuk state machine (setStep, showScene, typeIn, fillProgress, stampFire, showRow, showToast) + milestone array (~40 entries) + `prefers-reduced-motion` branch.

**Tech Stack:** HTML5 + CSS3 (`@keyframes`, custom props, `clamp`, `aspect-ratio`), SVG (`stroke-dasharray`, `stroke-dashoffset`), vanilla JS (`requestAnimationFrame`, `setTimeout`, `matchMedia`), Inter via Google Fonts, Playwright chromium, Python `http.server`.

**Spec reference:** `docs/superpowers/specs/2026-05-21-alur-pelaksanaan-proton-6-langkah-design.md`

---

## File Structure

| File | Status | Tanggung jawab |
|------|--------|----------------|
| `docs/assets/proton-video/alur-pelaksanaan.html` | CREATE | Sumber animasi 45s, 7 scene, semua CSS+SVG+JS inline |
| `docs/assets/proton-video/record-alur-pelaksanaan.mjs` | CREATE | Playwright record 46s, output .webm |
| `docs/assets/proton-video/alur-pelaksanaan.webm` | GENERATE | Output video 45s, 1920×1080, 4-7 MB |
| `docs/Naskah Video PROTON.docx` | NO CHANGE | Naskah narasi (out-of-scope) |

---

## Verification Approach (Non-Unit-Test)

Animasi HTML tidak punya unit test framework. Verifikasi tiap task = **manual visual check di browser**:

1. Start `python -m http.server 8800` di `docs/assets/proton-video/` (sekali di Task 0, background).
2. Buka `http://localhost:8800/alur-pelaksanaan.html` di Chrome.
3. Refresh setelah tiap edit.
4. DevTools Elements panel: inspect class state.
5. DevTools Rendering → simulate `prefers-reduced-motion: reduce` untuk verify Task 13.
6. Stopwatch milestone timing.

---

## Task 0: Setup — verify Playwright + start local server

**Files:** none (environment)

- [ ] **Step 1: Verify Playwright installed**

Run:
```bash
node -e "import('playwright').then(m => console.log('playwright ok'))"
```

Expected: `playwright ok`. Kalau error, install:
```bash
npm install --no-save playwright
npx playwright install chromium
```

- [ ] **Step 2: Start http server background**

Run (terminal terpisah):
```bash
cd "docs/assets/proton-video" && python -m http.server 8800
```

Biarkan running selama implementation.

**No commit (environment only).**

---

## Task 1: Scaffold HTML — stage 16:9 + palette + grid + header + brand mark

**Files:**
- Create: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Create file dengan skeleton stage 16:9 + palette + header**

Tulis `docs/assets/proton-video/alur-pelaksanaan.html`:

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8">
<title>Animasi Alur Pelaksanaan PROTON — Video Bagian 4 (1:15–1:55)</title>
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
    padding: clamp(28px, 3vw, 48px) clamp(36px, 4vw, 64px);
    display: flex;
    flex-direction: column;
  }

  /* ===== GRID BACKDROP ===== */
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

  /* ===== HEADER ===== */
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

  /* ===== BRAND MARK ===== */
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
      <div class="eyebrow">Alur Pelaksanaan PROTON</div>
      <h1 class="title">Enam Langkah <em>Terstruktur</em></h1>
    </div>

    <!-- scenes container + stepper akan ditambahkan task berikutnya -->

    <div class="brand-mark">PROTON × KPB</div>
  </div>
</body>
</html>
```

- [ ] **Step 2: Visual verify**

Buka `http://localhost:8800/alur-pelaksanaan.html`. Expected:
- Stage 16:9 rasio rapi, grid backdrop visible tipis
- Eyebrow merah uppercase "ALUR PELAKSANAAN PROTON"
- Title navy besar "Enam Langkah Terstruktur" dengan "Terstruktur" merah + underline red draw t=1.2s
- Brand mark "PROTON × KPB" pojok bawah-kanan opacity 0.5

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scaffold alur-pelaksanaan — stage 16:9 + header + brand mark — task 1/16

Stage 16:9 max 1600px, grid backdrop hairline navy 4% z=0, header eyebrow red +
title "Enam Langkah Terstruktur" em underline draw red 1.2s, brand mark
PROTON × KPB pojok kanan-bawah. Match track-progresi visual identity.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Stepper component (markup + state machine CSS)

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS stepper di `<style>`**

Sisipkan setelah block `.brand-mark`:

```css
  /* ===== STEPPER ===== */
  .stepper {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0;
    margin-top: auto;
    margin-bottom: 0;
    opacity: 0;
    transform: translateY(8px);
    animation: fadeUp 0.6s 0.9s ease-out forwards;
  }
  .step-node {
    width: clamp(28px, 2.4vw, 40px);
    height: clamp(28px, 2.4vw, 40px);
    border-radius: 50%;
    border: 2px solid var(--hairline);
    display: grid;
    place-items: center;
    font-size: clamp(12px, 1vw, 16px);
    font-weight: 800;
    color: var(--muted);
    background: #FFFFFF;
    transition: all 0.5s cubic-bezier(0.34, 1.56, 0.64, 1);
    position: relative;
  }
  .step-node.active {
    border-color: var(--navy);
    background: var(--navy);
    color: #FFFFFF;
    transform: scale(1.15);
  }
  .step-node.done {
    border-color: var(--navy-soft);
    background: var(--navy-soft);
    color: transparent;
  }
  .step-node.done::after {
    content: "✓";
    position: absolute;
    color: #FFFFFF;
    font-size: 1.1em;
    line-height: 1;
  }
  .step-node.active.final {
    background: var(--red);
    border-color: var(--red);
  }
  .stepper.done-all .step-node {
    background: var(--navy-soft);
    border-color: var(--navy-soft);
    color: transparent;
    transform: scale(1);
  }
  .stepper.done-all .step-node:last-child {
    background: var(--red);
    border-color: var(--red);
  }
  .stepper.done-all .step-node::after {
    content: "✓";
    position: absolute;
    color: #FFFFFF;
    font-size: 1.1em;
    line-height: 1;
  }

  .step-connector {
    width: clamp(40px, 5vw, 80px);
    height: 2px;
    background: var(--hairline);
    transition: background 0.5s ease;
  }
  .step-connector.filled { background: var(--navy-soft); }
  .step-connector.final { background: var(--red); }
  .stepper.done-all .step-connector { background: var(--navy-soft); }
  .stepper.done-all .step-connector:last-of-type { background: var(--red); }
```

- [ ] **Step 2: Tambah markup stepper sebelum brand-mark**

Sisipkan di dalam `.stage`, sebelum `<div class="brand-mark">`:

```html
    <div class="stepper" id="stepper">
      <div class="step-node">1</div>
      <div class="step-connector"></div>
      <div class="step-node">2</div>
      <div class="step-connector"></div>
      <div class="step-node">3</div>
      <div class="step-connector"></div>
      <div class="step-node">4</div>
      <div class="step-connector"></div>
      <div class="step-node">5</div>
      <div class="step-connector"></div>
      <div class="step-node">6</div>
    </div>
```

- [ ] **Step 3: Visual verify stepper**

Refresh. Expected:
- Stepper centered bottom area, 6 circle dengan number 1–6
- Semua nodes pending state: outline navy hairline, fill white, number abu-abu
- 5 connector line abu-abu antar nodes
- Stepper fade-up dari translateY(8px) di t=0.9s

Manual test state (DevTools console):
```javascript
document.querySelectorAll('.step-node')[0].classList.add('active');  // node 1 → navy filled
document.querySelectorAll('.step-node')[0].classList.replace('active', 'done');  // node 1 → ✓ navy-soft
document.querySelectorAll('.step-node')[5].classList.add('active', 'final');  // node 6 → red
document.getElementById('stepper').classList.add('done-all');  // all done
```

Reset:
```javascript
document.querySelectorAll('.step-node').forEach(n => n.className = 'step-node');
document.getElementById('stepper').classList.remove('done-all');
```

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): stepper component 6 nodes — alur-pelaksanaan task 2/16

CSS state machine pending/active/done/final + done-all override. Spring
transition 0.5s. 5 connector dengan filled/final state. JS-toggle via class
di task orchestration nanti.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Scene container + cross-fade transition CSS

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS `.scenes` + `.scene`**

Sisipkan di `<style>` setelah block `.brand-mark`, sebelum stepper rules:

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
```

- [ ] **Step 2: Tambah markup `.scenes` container sebelum stepper**

Sisipkan di `.stage`, antara `</div>` header dan `<div class="stepper">`:

```html
    <div class="scenes">
      <!-- 7 scene akan ditambahkan task 5-11 -->
    </div>
```

- [ ] **Step 3: Visual verify**

Refresh. Expected: tidak ada visual change (scene container empty). Layout tetap header + stepper. Inspect: `.scenes` div ada antara header dan stepper, fill space available via `flex: 1`.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene container + cross-fade transition — alur-pelaksanaan task 3/16

.scenes flex-1 container, .scene absolute inset-0 opacity-0, .scene.active
opacity-1 + translateY-0 spring. Single active scene at a time. JS-toggle
via showScene() di task 12.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Browser-frame reusable component CSS

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS `.browser-frame`**

Sisipkan di `<style>` setelah `.scene.active` block:

```css
  /* ===== BROWSER FRAME (reusable) ===== */
  .browser-frame {
    width: 100%;
    max-width: 1100px;
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 10px;
    box-shadow: 0 1px 2px rgba(10,36,71,0.03), 0 12px 32px rgba(10,36,71,0.06);
    overflow: hidden;
  }
  .browser-bar {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 16px;
    background: #F5F7FA;
    border-bottom: 1px solid var(--hairline);
  }
  .browser-bar .dots {
    display: flex;
    gap: 6px;
  }
  .browser-bar .dots i {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: #D0D5DD;
    display: inline-block;
  }
  .browser-bar .dots i:first-child { background: #F56565; }
  .browser-bar .dots i:nth-child(2) { background: #ECC94B; }
  .browser-bar .dots i:nth-child(3) { background: #48BB78; }
  .browser-bar .url {
    flex: 1;
    font-size: clamp(11px, 0.85vw, 13px);
    color: var(--muted);
    font-weight: 600;
  }
  .browser-bar .url em {
    font-style: normal;
    color: var(--navy-deep);
    font-weight: 700;
  }
  .browser-body {
    padding: clamp(20px, 2vw, 32px);
    min-height: clamp(280px, 30vw, 420px);
    position: relative;
  }
```

- [ ] **Step 2: Visual smoke test via temporary markup**

Sementara, untuk verify browser-frame, tambahkan di `.scenes`:

```html
    <div class="scenes">
      <div class="scene active" id="smoke-test">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Test</em></span>
          </div>
          <div class="browser-body">
            Body placeholder
          </div>
        </div>
      </div>
    </div>
```

Refresh. Expected:
- Browser frame visible centered di scene area
- Bar abu-abu light dengan 3 dot (merah/kuning/hijau) + URL text
- Body white
- Subtle shadow drop

**REMOVE smoke test markup setelah verify**, kembalikan ke:
```html
    <div class="scenes">
      <!-- 7 scene akan ditambahkan task 5-11 -->
    </div>
```

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): browser-frame reusable component CSS — alur-pelaksanaan task 4/16

Reusable frame: bar 28px height (3 traffic dot + URL strip), body padding +
min-height. Border radius 10px + drop shadow subtle. Reused by scene 1-6.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Scene 1 — HC Assign Coach

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS scene-1 specifics**

Sisipkan di `<style>` setelah block `.browser-body`:

```css
  /* ===== SCENE 1: HC ASSIGN COACH ===== */
  .assign-body {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: clamp(20px, 3vw, 60px);
    min-height: clamp(280px, 30vw, 420px);
  }
  .role-card {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    padding: clamp(16px, 2vw, 28px);
    background: #FFFFFF;
    border: 2px solid var(--navy);
    border-radius: 10px;
    min-width: clamp(140px, 14vw, 200px);
    opacity: 0;
    transform: translateY(12px) scale(0.96);
  }
  .role-card.admin {
    animation: cardPop 0.6s 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
  }
  .role-card.coach {
    animation: cardPop 0.6s 2.5s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
  }
  .role-card.coach.highlight {
    border-color: var(--red);
    box-shadow: 0 0 0 4px rgba(230,51,41,0.15);
  }
  @keyframes cardPop {
    0%   { opacity: 0; transform: translateY(12px) scale(0.96); }
    100% { opacity: 1; transform: translateY(0) scale(1); }
  }
  .role-card .icon-circle {
    width: clamp(48px, 4.5vw, 72px);
    height: clamp(48px, 4.5vw, 72px);
    border-radius: 50%;
    background: var(--sky);
    display: grid;
    place-items: center;
    font-size: clamp(24px, 2.2vw, 36px);
  }
  .role-card .role-name {
    font-size: clamp(13px, 1.1vw, 16px);
    font-weight: 800;
    color: var(--navy-deep);
    text-transform: uppercase;
    letter-spacing: 0.06em;
  }
  .role-card .role-sub {
    font-size: clamp(10px, 0.85vw, 13px);
    font-weight: 600;
    color: var(--muted);
  }

  .arrow-block {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
    max-width: 240px;
  }
  .arrow-svg {
    width: 100%;
    height: 40px;
    overflow: visible;
  }
  .arrow-svg path,
  .arrow-svg polyline {
    fill: none;
    stroke: var(--navy-soft);
    stroke-width: 2.5;
    stroke-linecap: round;
    stroke-linejoin: round;
  }
  .arrow-svg path {
    stroke-dasharray: 170;
    stroke-dashoffset: 170;
    animation: arrowDraw 1.0s 1.2s ease-out forwards;
  }
  .arrow-svg polyline {
    opacity: 0;
    animation: fadeIn 0.3s 2.0s ease-out forwards;
  }
  @keyframes arrowDraw { to { stroke-dashoffset: 0; } }
  .assign-label {
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 800;
    color: var(--red);
    letter-spacing: 0.15em;
    text-transform: uppercase;
    opacity: 0;
    animation: fadeIn 0.4s 1.8s ease-out forwards;
  }
```

- [ ] **Step 2: Tambah markup scene-1 di `.scenes`**

Sisipkan di dalam `.scenes` container:

```html
      <div class="scene" id="scene-1">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Admin</em></span>
          </div>
          <div class="browser-body assign-body">
            <div class="role-card admin">
              <div class="icon-circle">👤</div>
              <div class="role-name">HC Admin</div>
              <div class="role-sub">Penugasan</div>
            </div>
            <div class="arrow-block">
              <svg class="arrow-svg" viewBox="0 0 200 40" preserveAspectRatio="none">
                <path d="M 10 20 L 180 20"/>
                <polyline points="170 12 188 20 170 28"/>
              </svg>
              <div class="assign-label">ASSIGN</div>
            </div>
            <div class="role-card coach">
              <div class="icon-circle">👤</div>
              <div class="role-name">Coach Senior</div>
              <div class="role-sub">Refinery Ops</div>
            </div>
          </div>
        </div>
      </div>
```

- [ ] **Step 3: Smoke test scene-1 visible**

Add `.active` class manual sementara:

```html
      <div class="scene active" id="scene-1">
```

Refresh. Expected:
- Browser frame "portalhc.kpb · Admin"
- Admin card kiri pop spring t=0.4s (sebenarnya t=0.4s setelah scene aktif)
- Arrow draw kiri→kanan t=1.2s + polyline (panah) muncul t=2.0s
- Label "ASSIGN" muncul t=1.8s
- Coach card kanan pop t=2.5s
- Coach card belum highlight (.highlight class belum di-toggle, akan via JS)

Setelah verify, **REMOVE `.active` class** dari scene-1:

```html
      <div class="scene" id="scene-1">
```

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-1 HC Assign Coach — alur-pelaksanaan task 5/16

Browser frame "Admin", 2 role card (Admin kiri + Coach kanan) pop spring,
SVG arrow draw 170px kiri→kanan + polyline panah, label "ASSIGN" merah
center, coach card .highlight state untuk JS toggle.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Scene 2 — Deliverable IDP (sidebar + task + upload + toast)

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS scene-2 specifics**

Sisipkan di `<style>` setelah scene-1 CSS:

```css
  /* ===== SCENE 2: DELIVERABLE IDP ===== */
  .idp-body {
    display: flex;
    gap: 0;
    padding: 0 !important;
    min-height: clamp(280px, 30vw, 420px);
  }
  .sidebar {
    width: clamp(140px, 14vw, 200px);
    background: var(--paper);
    border-right: 1px solid var(--hairline);
    padding: 16px 0;
    display: flex;
    flex-direction: column;
    gap: 2px;
  }
  .sidebar .nav-item {
    padding: 10px 16px;
    font-size: clamp(11px, 0.95vw, 14px);
    font-weight: 600;
    color: var(--text);
    text-decoration: none;
    border-left: 3px solid transparent;
    transition: all 0.4s ease;
  }
  .sidebar .nav-item.active {
    color: var(--navy-deep);
    background: rgba(15,45,92,0.08);
    border-left-color: var(--navy);
    font-weight: 800;
  }
  .idp-main {
    flex: 1;
    padding: clamp(16px, 2vw, 28px);
    position: relative;
  }
  .idp-main h3 {
    font-size: clamp(13px, 1.1vw, 16px);
    font-weight: 800;
    color: var(--navy-deep);
    text-transform: uppercase;
    letter-spacing: 0.06em;
    margin-bottom: 12px;
  }
  .task-card {
    padding: 10px 14px;
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 6px;
    font-size: clamp(11px, 0.95vw, 13px);
    color: var(--text);
    margin-bottom: 6px;
    opacity: 0;
    transform: translateX(20px);
  }
  #scene-2.active .task-card:nth-child(2) {
    animation: slideInRight 0.5s 0.8s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
  }
  #scene-2.active .task-card:nth-child(3) {
    animation: slideInRight 0.5s 1.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
  }
  #scene-2.active .task-card:nth-child(4) {
    animation: slideInRight 0.5s 1.2s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
  }
  @keyframes slideInRight {
    to { opacity: 1; transform: translateX(0); }
  }

  .upload-zone {
    margin-top: 16px;
    padding: 16px;
    background: var(--paper);
    border: 1px dashed var(--hairline);
    border-radius: 8px;
    text-align: center;
  }
  .upload-label {
    display: block;
    font-size: clamp(11px, 0.95vw, 13px);
    font-weight: 700;
    color: var(--navy-soft);
    margin-bottom: 8px;
  }
  .progress-bar {
    height: 8px;
    background: var(--hairline);
    border-radius: 4px;
    overflow: hidden;
    width: 100%;
  }
  .progress-bar .fill {
    height: 100%;
    background: var(--navy);
    width: 0%;
    transition: width 1.8s cubic-bezier(0.65, 0, 0.35, 1);
  }
  .progress-bar.active .fill { width: var(--target, 100%); }

  .toast {
    position: absolute;
    bottom: 16px;
    right: 16px;
    padding: 10px 16px;
    background: var(--navy);
    color: #FFFFFF;
    border-radius: 6px;
    font-size: clamp(12px, 0.95vw, 14px);
    font-weight: 600;
    opacity: 0;
    transform: translateX(40px);
    transition: opacity 0.4s ease, transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
  }
  .toast.show { opacity: 1; transform: translateX(0); }
```

- [ ] **Step 2: Tambah markup scene-2**

Sisipkan setelah scene-1 di `.scenes`:

```html
      <div class="scene" id="scene-2">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>IDP</em></span>
          </div>
          <div class="browser-body idp-body">
            <aside class="sidebar">
              <a class="nav-item">Dashboard</a>
              <a class="nav-item active">IDP</a>
              <a class="nav-item">Coaching</a>
              <a class="nav-item">Histori</a>
            </aside>
            <main class="idp-main">
              <h3>Tugas Saya</h3>
              <div class="task-card">☐ Studi Kasus · Due 30 Jun</div>
              <div class="task-card">☐ Log Harian · weekly</div>
              <div class="task-card">☐ Refleksi Coaching · monthly</div>
              <div class="upload-zone">
                <span class="upload-label">📤 Upload Bukti Deliverable</span>
                <div class="progress-bar" id="upload-bar"><div class="fill"></div></div>
              </div>
              <div class="toast" id="toast-upload">✓ Bukti terunggah</div>
            </main>
          </div>
        </div>
      </div>
```

- [ ] **Step 3: Smoke test scene-2**

Add `.active` class manual:

```html
      <div class="scene active" id="scene-2">
```

Refresh. Expected:
- Browser frame "portalhc.kpb · IDP"
- Sidebar kiri 4 nav: Dashboard, **IDP** (highlight navy + left border), Coaching, Histori
- Main area: h3 "TUGAS SAYA", 3 task card slide-in dari kanan stagger 200ms (0.8/1.0/1.2s)
- Upload zone dashed border + label + progress bar abu-abu (belum filled — `.active` class belum di-toggle via JS)
- Toast tersembunyi (belum `.show`)

Test progress bar manual (DevTools console):
```javascript
document.getElementById('upload-bar').style.setProperty('--target', '100%');
document.getElementById('upload-bar').classList.add('active');
// 1.8s later, fill mencapai 100%
document.getElementById('toast-upload').classList.add('show');
// toast slide-in dari kanan
```

Reset + **REMOVE `.active`** dari scene-2:
```html
      <div class="scene" id="scene-2">
```

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-2 Deliverable IDP — alur-pelaksanaan task 6/16

Browser frame "IDP" dengan sidebar 4 nav (IDP active highlight), main area
3 task card slide-in stagger 200ms, upload zone dashed + progress bar 0→100%
sweep 1.8s, toast "✓ Bukti terunggah" slide-in kanan.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Scene 3 — Coaching Proton Form (3 field + type-in)

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS scene-3 specifics**

Sisipkan di `<style>` setelah scene-2 CSS:

```css
  /* ===== SCENE 3: COACHING PROTON FORM ===== */
  .coaching-body {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .form-field {
    display: flex;
    flex-direction: column;
    gap: 4px;
    opacity: 0;
    transform: translateY(6px);
  }
  #scene-3.active .form-field:nth-child(1) { animation: fadeUp 0.4s 0.4s ease-out forwards; }
  #scene-3.active .form-field:nth-child(2) { animation: fadeUp 0.4s 0.55s ease-out forwards; }
  #scene-3.active .form-field:nth-child(3) { animation: fadeUp 0.4s 0.70s ease-out forwards; }
  .form-field label {
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 700;
    color: var(--navy-deep);
    text-transform: uppercase;
    letter-spacing: 0.08em;
  }
  .form-field .textarea {
    padding: 8px 12px;
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 6px;
    font-size: clamp(11px, 0.95vw, 13px);
    color: var(--text);
    min-height: 32px;
    font-family: inherit;
  }
  .btn-save {
    align-self: flex-start;
    padding: 8px 16px;
    background: var(--navy);
    color: #FFFFFF;
    border: none;
    border-radius: 6px;
    font-size: clamp(11px, 0.95vw, 13px);
    font-weight: 700;
    cursor: pointer;
    margin-top: 4px;
    opacity: 0;
    transform: translateY(6px);
  }
  #scene-3.active .btn-save { animation: fadeUp 0.4s 3.4s ease-out forwards; }
```

- [ ] **Step 2: Tambah markup scene-3**

Sisipkan setelah scene-2 di `.scenes`:

```html
      <div class="scene" id="scene-3">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Coaching PROTON · Sesi 04</em></span>
          </div>
          <div class="browser-body coaching-body">
            <div class="form-field">
              <label>Diskusi</label>
              <div class="textarea" id="field-diskusi"></div>
            </div>
            <div class="form-field">
              <label>Kesimpulan</label>
              <div class="textarea" id="field-kesimpulan"></div>
            </div>
            <div class="form-field">
              <label>Tindak Lanjut</label>
              <div class="textarea" id="field-tindak"></div>
            </div>
            <button class="btn-save">💾 Simpan Sesi</button>
            <div class="toast" id="toast-coaching">✓ Sesi tersimpan</div>
          </div>
        </div>
      </div>
```

- [ ] **Step 3: Smoke test scene-3**

Add `.active`:
```html
      <div class="scene active" id="scene-3">
```

Refresh. Expected:
- Browser frame "Coaching PROTON · Sesi 04"
- 3 form-field "Diskusi", "Kesimpulan", "Tindak Lanjut" fade-up stagger
- Empty textarea (text akan di-typeIn via JS task 12)
- Button "💾 Simpan Sesi" navy muncul t=3.4s
- Toast hidden

Test type-in manual:
```javascript
const el = document.getElementById('field-diskusi');
let i = 0;
const text = 'Membahas hambatan studi kasus.';
const id = setInterval(() => {
  el.textContent += text[i++];
  if (i >= text.length) clearInterval(id);
}, 25);
```

Reset + **REMOVE `.active`**.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-3 Coaching Proton form — alur-pelaksanaan task 7/16

Browser frame "Coaching PROTON · Sesi 04", 3 form-field (Diskusi, Kesimpulan,
Tindak Lanjut) fade-up stagger 150ms, empty textarea siap typeIn() JS,
btn-save navy + toast hidden untuk JS trigger.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: Scene 4 — Approval Document + 3 Stamps

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS scene-4 specifics**

Sisipkan di `<style>` setelah scene-3 CSS:

```css
  /* ===== SCENE 4: APPROVAL DOCUMENT + STAMPS ===== */
  .approval-body {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: clamp(16px, 2vw, 28px);
  }
  .document-central {
    width: clamp(160px, 16vw, 220px);
    padding: clamp(16px, 1.8vw, 24px);
    background: #FFFFFF;
    border: 2px solid var(--navy-soft);
    border-radius: 8px;
    text-align: center;
    opacity: 0;
    transform: translateY(12px) scale(0.96);
    position: relative;
  }
  #scene-4.active .document-central { animation: cardPop 0.5s 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  .doc-icon {
    font-size: clamp(28px, 2.6vw, 40px);
    line-height: 1;
    margin-bottom: 4px;
  }
  .doc-label {
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 800;
    color: var(--navy-deep);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    margin-bottom: 10px;
  }
  .doc-content {
    display: flex;
    flex-direction: column;
    gap: 4px;
    align-items: stretch;
  }
  .doc-content .text-line {
    height: 4px;
    background: var(--hairline);
    border-radius: 2px;
  }
  .doc-content .text-line.short { width: 60%; align-self: center; }
  .status-badge {
    margin-top: 12px;
    display: inline-block;
    padding: 4px 10px;
    background: var(--red);
    color: #FFFFFF;
    border-radius: 4px;
    font-size: clamp(10px, 0.85vw, 12px);
    font-weight: 800;
    letter-spacing: 0.1em;
    opacity: 0;
    transform: translateY(4px);
    transition: opacity 0.4s ease, transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
  }
  .status-badge.show { opacity: 1; transform: translateY(0); }

  .stamps-row {
    display: flex;
    gap: clamp(12px, 1.5vw, 24px);
    justify-content: center;
  }
  .stamp {
    position: relative;
    width: clamp(120px, 12vw, 180px);
    padding: 12px 16px;
    background: #FFFFFF;
    border: 2px solid var(--navy-soft);
    border-radius: 8px;
    text-align: center;
    opacity: 0;
    transform: translateY(-60px) rotate(-15deg) scale(0.6);
    transform-origin: center;
  }
  .stamp.s1 { border-color: var(--red); }
  .stamp.s3 { border-color: var(--red); box-shadow: 0 0 0 3px rgba(230,51,41,0.2); }
  .stamp.fired { animation: stampDrop 0.6s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  @keyframes stampDrop {
    60%  { opacity: 1; transform: translateY(8px) rotate(2deg) scale(1.1); }
    100% { opacity: 1; transform: translateY(0) rotate(-3deg) scale(1); }
  }
  .stamp.fired::after {
    content: "";
    position: absolute;
    inset: -8px;
    border: 2px solid var(--red);
    border-radius: 12px;
    opacity: 0;
    animation: stampImpact 0.5s ease-out 0.4s forwards;
  }
  @keyframes stampImpact {
    0%   { opacity: 0.8; transform: scale(0.5); }
    100% { opacity: 0;   transform: scale(1.3); }
  }
  .stamp-tick {
    font-size: clamp(20px, 2vw, 32px);
    color: var(--red);
    font-weight: 900;
    line-height: 1;
  }
  .stamp.s2 .stamp-tick { color: var(--navy); }
  .stamp-role {
    font-size: clamp(11px, 0.9vw, 14px);
    font-weight: 800;
    color: var(--navy-deep);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    margin-top: 4px;
  }
  .stamp-date {
    font-size: clamp(10px, 0.78vw, 12px);
    font-weight: 600;
    color: var(--muted);
    margin-top: 2px;
  }
```

- [ ] **Step 2: Tambah markup scene-4**

Sisipkan setelah scene-3:

```html
      <div class="scene" id="scene-4">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Approval Bukti · #5847</em></span>
          </div>
          <div class="browser-body approval-body">
            <div class="document-central">
              <div class="doc-icon">📄</div>
              <div class="doc-label">Bukti Pekerjaan</div>
              <div class="doc-content">
                <div class="text-line"></div>
                <div class="text-line"></div>
                <div class="text-line short"></div>
              </div>
              <div class="status-badge" id="status-verif">✓ TERVERIFIKASI</div>
            </div>
            <div class="stamps-row">
              <div class="stamp s1" id="stamp-1">
                <div class="stamp-tick">✓</div>
                <div class="stamp-role">Sr Supervisor</div>
                <div class="stamp-date">03/05/26</div>
              </div>
              <div class="stamp s2" id="stamp-2">
                <div class="stamp-tick">✓</div>
                <div class="stamp-role">Section Head</div>
                <div class="stamp-date">04/05/26</div>
              </div>
              <div class="stamp s3" id="stamp-3">
                <div class="stamp-tick">✓</div>
                <div class="stamp-role">HC Final</div>
                <div class="stamp-date">05/05/26</div>
              </div>
            </div>
          </div>
        </div>
      </div>
```

- [ ] **Step 3: Smoke test scene-4**

Add `.active`:
```html
      <div class="scene active" id="scene-4">
```

Refresh. Expected:
- Browser frame "Approval Bukti · #5847"
- Document central pop t=0.4s ("Bukti Pekerjaan", 3 text-line skeleton)
- 3 stamp row hidden (`.fired` belum di-add)
- Status badge "TERVERIFIKASI" hidden

Test stamps manual:
```javascript
document.getElementById('stamp-1').classList.add('fired');
// stamp drop, impact ring expand
setTimeout(() => document.getElementById('stamp-2').classList.add('fired'), 1500);
setTimeout(() => document.getElementById('stamp-3').classList.add('fired'), 3000);
setTimeout(() => document.getElementById('status-verif').classList.add('show'), 4500);
```

Reset + **REMOVE `.active`**.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-4 Approval document + 3 stamps — alur-pelaksanaan task 8/16

Browser frame "Approval Bukti · #5847", document central pop dengan
text-line skeleton, 3 stamp horizontal row bawah (Sr Sup red, Section Head
navy, HC Final red+glow). Stamp drop spring + impact ring expand-fade.
Status badge TERVERIFIKASI bottom-fade-up via .show class.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: Scene 5 — Final Assessment Quiz

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS scene-5 specifics**

Sisipkan di `<style>` setelah scene-4 CSS:

```css
  /* ===== SCENE 5: FINAL ASSESSMENT ===== */
  .assess-body {
    display: flex;
    flex-direction: column;
    gap: 14px;
  }
  .meta-strip {
    display: flex;
    gap: clamp(12px, 2vw, 32px);
    padding: 8px 12px;
    background: var(--paper);
    border-radius: 6px;
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 700;
    color: var(--navy-deep);
    opacity: 0;
    transform: translateY(6px);
  }
  #scene-5.active .meta-strip { animation: fadeUp 0.4s 0.5s ease-out forwards; }

  .quiz-card {
    padding: clamp(14px, 1.6vw, 22px);
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 8px;
    opacity: 0;
    transform: translateY(8px);
  }
  #scene-5.active .quiz-card { animation: fadeUp 0.5s 1.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  .q-label {
    font-size: clamp(10px, 0.85vw, 12px);
    font-weight: 800;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 0.1em;
    margin-bottom: 6px;
  }
  .q-text {
    font-size: clamp(13px, 1.15vw, 16px);
    font-weight: 700;
    color: var(--navy-deep);
    margin-bottom: 12px;
  }
  .options {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }
  .opt {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 12px;
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 6px;
    font-size: clamp(11px, 0.95vw, 13px);
    color: var(--text);
    cursor: pointer;
    opacity: 0;
    transform: translateY(4px);
  }
  #scene-5.active .opt:nth-of-type(1) { animation: fadeUp 0.3s 1.5s ease-out forwards; }
  #scene-5.active .opt:nth-of-type(2) { animation: fadeUp 0.3s 1.65s ease-out forwards; }
  #scene-5.active .opt:nth-of-type(3) { animation: fadeUp 0.3s 1.80s ease-out forwards; }
  #scene-5.active .opt:nth-of-type(4) { animation: fadeUp 0.3s 1.95s ease-out forwards; }
  .opt.selected {
    border-color: var(--navy);
    background: rgba(15,45,92,0.06);
    font-weight: 700;
    color: var(--navy-deep);
    transition: all 0.3s ease 2.5s;
  }
  .opt input { accent-color: var(--navy); }

  .progress-strip {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-top: 6px;
  }
  .p-label {
    font-size: clamp(10px, 0.85vw, 12px);
    font-weight: 700;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 0.08em;
  }
  .progress-strip .progress-bar { flex: 1; }
  .p-count {
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 700;
    color: var(--navy-deep);
    font-variant-numeric: tabular-nums;
  }
```

- [ ] **Step 2: Tambah markup scene-5**

Sisipkan setelah scene-4:

```html
      <div class="scene" id="scene-5">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Final Assessment</em></span>
          </div>
          <div class="browser-body assess-body">
            <div class="meta-strip">
              <span>⏱ Durasi: 60 menit</span>
              <span>📋 Soal: 30</span>
            </div>
            <div class="quiz-card">
              <div class="q-label">Pertanyaan 12 / 30</div>
              <div class="q-text">Berapa langkah dalam Alur Pelaksanaan PROTON?</div>
              <div class="options">
                <label class="opt"><input type="radio" name="q" disabled> A) 4 langkah</label>
                <label class="opt selected"><input type="radio" name="q" checked disabled> B) 6 langkah</label>
                <label class="opt"><input type="radio" name="q" disabled> C) 8 langkah</label>
                <label class="opt"><input type="radio" name="q" disabled> D) 10 langkah</label>
              </div>
            </div>
            <div class="progress-strip">
              <span class="p-label">Progres</span>
              <div class="progress-bar" id="quiz-bar"><div class="fill"></div></div>
              <span class="p-count">12 / 30</span>
            </div>
          </div>
        </div>
      </div>
```

- [ ] **Step 3: Smoke test scene-5**

Add `.active`:
```html
      <div class="scene active" id="scene-5">
```

Refresh. Expected:
- Browser frame "Final Assessment"
- Meta strip "Durasi 60 menit · Soal 30" fade-up t=0.5s
- Quiz card fade-up t=1.0s, pertanyaan "Berapa langkah dalam Alur Pelaksanaan PROTON?"
- 4 option radio fade-up stagger 150ms (1.5/1.65/1.80/1.95s)
- Option B "6 langkah" highlight (selected class — visible immediately karena hardcoded checked)
- Progress bar abu-abu (not filled yet)

Test progress manual:
```javascript
document.getElementById('quiz-bar').style.setProperty('--target', '40%');
document.getElementById('quiz-bar').classList.add('active');
```

Reset + **REMOVE `.active`**.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-5 Final Assessment quiz — alur-pelaksanaan task 9/16

Browser frame "Final Assessment", meta strip durasi+soal, quiz card "Berapa
langkah PROTON?" + 4 option radio (B "6 langkah" selected = self-referential),
progress bar 12/30 untuk JS fill via fillProgress() t=31s.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: Scene 6 — Histori PROTON Timeline (6 row vertical)

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS scene-6 specifics**

Sisipkan di `<style>` setelah scene-5 CSS:

```css
  /* ===== SCENE 6: HISTORI PROTON ===== */
  .histori-body {
    position: relative;
    padding-left: clamp(40px, 4vw, 60px) !important;
  }
  .timeline-backbone {
    position: absolute;
    left: clamp(28px, 3vw, 44px);
    top: clamp(20px, 2vw, 32px);
    bottom: clamp(20px, 2vw, 32px);
    width: 2px;
    overflow: visible;
    height: auto;
  }
  .timeline-backbone path {
    fill: none;
    stroke: var(--navy-soft);
    stroke-width: 2;
    stroke-linecap: round;
    stroke-dasharray: 400;
    stroke-dashoffset: 400;
  }
  #scene-6.active .timeline-backbone path {
    animation: backboneDraw 0.8s 0.4s ease-out forwards;
  }
  @keyframes backboneDraw { to { stroke-dashoffset: 0; } }

  .timeline-row {
    display: flex;
    align-items: center;
    gap: clamp(10px, 1.5vw, 20px);
    padding: clamp(6px, 0.6vw, 10px) 0;
    position: relative;
    opacity: 0;
    transform: translateX(-12px);
    transition: opacity 0.4s ease, transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
  }
  .timeline-row::before {
    content: "";
    position: absolute;
    left: clamp(-24px, -2vw, -16px);
    top: 50%;
    width: 12px;
    height: 12px;
    background: var(--navy);
    border-radius: 50%;
    border: 2px solid #FFFFFF;
    box-shadow: 0 0 0 2px var(--navy-soft);
    transform: translate(-50%, -50%);
  }
  .timeline-row.last::before {
    background: var(--red);
    box-shadow: 0 0 0 2px var(--red);
  }
  .timeline-row.show { opacity: 1; transform: translateX(0); }
  .timeline-row .date {
    font-size: clamp(10px, 0.85vw, 12px);
    font-weight: 800;
    color: var(--navy-soft);
    letter-spacing: 0.08em;
    text-transform: uppercase;
    min-width: clamp(60px, 6vw, 90px);
  }
  .timeline-row .event {
    font-size: clamp(11px, 0.95vw, 13px);
    font-weight: 600;
    color: var(--text);
  }
  .timeline-row.last .event {
    color: var(--red-dark);
    font-weight: 800;
  }
  .histori-caption {
    margin-top: clamp(10px, 1vw, 16px);
    text-align: center;
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 700;
    color: var(--muted);
    letter-spacing: 0.1em;
    text-transform: uppercase;
    opacity: 0;
    transition: opacity 0.5s ease;
  }
  .histori-caption.show { opacity: 1; }
```

- [ ] **Step 2: Tambah markup scene-6**

Sisipkan setelah scene-5:

```html
      <div class="scene" id="scene-6">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Histori PROTON · Coachee X</em></span>
          </div>
          <div class="browser-body histori-body">
            <svg class="timeline-backbone" viewBox="0 0 2 400" preserveAspectRatio="none">
              <path d="M 1 0 L 1 400"/>
            </svg>
            <div class="timeline-row" id="row-1"><span class="date">Mar 2024</span><span class="event">HC Assign Coach</span></div>
            <div class="timeline-row" id="row-2"><span class="date">Sep 2024</span><span class="event">Deliverable Uploaded (5 files)</span></div>
            <div class="timeline-row" id="row-3"><span class="date">Mar 2025</span><span class="event">Coaching Session #08</span></div>
            <div class="timeline-row" id="row-4"><span class="date">Sep 2025</span><span class="event">Multi-Role Approval ✓</span></div>
            <div class="timeline-row" id="row-5"><span class="date">Mar 2026</span><span class="event">Final Assessment · Skor 85%</span></div>
            <div class="timeline-row last" id="row-6"><span class="date">Jul 2026</span><span class="event">Sertifikat Issued ⭐</span></div>
            <div class="histori-caption" id="histori-caption">Rekam jejak permanen</div>
          </div>
        </div>
      </div>
```

- [ ] **Step 3: Smoke test scene-6**

Add `.active`:
```html
      <div class="scene active" id="scene-6">
```

Refresh. Expected:
- Browser frame "Histori PROTON · Coachee X"
- Vertical line backbone draw atas-bawah t=0.4s
- 6 row hidden (`.show` belum)
- Caption hidden

Test rows manual:
```javascript
['row-1','row-2','row-3','row-4','row-5','row-6'].forEach((id, i) => {
  setTimeout(() => document.getElementById(id).classList.add('show'), i * 700);
});
setTimeout(() => document.getElementById('histori-caption').classList.add('show'), 4500);
```

Reset + **REMOVE `.active`**.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-6 Histori timeline 6 row vertikal — alur-pelaksanaan task 10/16

Browser frame "Histori PROTON · Coachee X", SVG vertical line backbone draw
atas-bawah 0.8s, 6 timeline row dengan dot pin kiri (Mar 2024 → Jul 2026,
3-year realistic cycle), row 6 (Sertifikat) red highlight. Caption "Rekam
jejak permanen" via .show class.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: Scene Outro — 6 ikon Recap + Tagline

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah CSS scene-outro specifics**

Sisipkan di `<style>` setelah scene-6 CSS:

```css
  /* ===== SCENE OUTRO: MINI-RECAP 6 IKON ===== */
  #scene-outro {
    flex-direction: column;
  }
  .recap-grid {
    display: grid;
    grid-template-columns: repeat(6, 1fr);
    gap: clamp(8px, 1vw, 16px);
    width: 100%;
    max-width: 1100px;
    margin-bottom: clamp(16px, 2vw, 28px);
  }
  .recap-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    padding: clamp(10px, 1.2vw, 16px);
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 8px;
    opacity: 0;
    transform: translateY(8px);
    transition: opacity 0.4s ease, transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
    position: relative;
  }
  .recap-item:last-child { border-color: var(--red); }
  .recap-item.show { opacity: 1; transform: translateY(0); }
  .recap-item .r-icon {
    font-size: clamp(20px, 2vw, 28px);
    line-height: 1;
  }
  .recap-item .r-label {
    font-size: clamp(10px, 0.78vw, 11px);
    font-weight: 700;
    color: var(--navy-deep);
    text-transform: uppercase;
    letter-spacing: 0.06em;
    text-align: center;
    line-height: 1.2;
  }
  .recap-item .r-check {
    font-size: clamp(14px, 1.2vw, 18px);
    font-weight: 900;
    color: var(--navy-soft);
    opacity: 0;
    transform: scale(0.4);
    transition: opacity 0.3s ease, transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
  }
  .recap-item:last-child .r-check { color: var(--red); }
  .recap-item.checked .r-check { opacity: 1; transform: scale(1); }

  .recap-tagline {
    font-size: clamp(13px, 1.1vw, 16px);
    font-weight: 800;
    color: var(--navy-deep);
    letter-spacing: 0.15em;
    text-transform: uppercase;
    text-align: center;
    opacity: 0;
    transition: opacity 0.6s ease;
  }
  .recap-tagline.show { opacity: 1; }
  .recap-tagline em { font-style: normal; color: var(--red); }
```

- [ ] **Step 2: Tambah markup scene-outro**

Sisipkan setelah scene-6 sebagai scene terakhir:

```html
      <div class="scene" id="scene-outro">
        <div class="recap-grid">
          <div class="recap-item" id="r1"><div class="r-icon">👥</div><div class="r-label">HC Assign Coach</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r2"><div class="r-icon">📤</div><div class="r-label">Upload Bukti</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r3"><div class="r-icon">📝</div><div class="r-label">Catat Sesi</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r4"><div class="r-icon">🗂️</div><div class="r-label">Approve 3-Tier</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r5"><div class="r-icon">📋</div><div class="r-label">Assess Final</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r6"><div class="r-icon">⭐</div><div class="r-label">Histori</div><div class="r-check">✓</div></div>
        </div>
        <div class="recap-tagline">Alur PROTON · <em>6 Langkah Terstruktur</em></div>
      </div>
```

- [ ] **Step 3: Smoke test scene-outro**

Add `.active`:
```html
      <div class="scene active" id="scene-outro">
```

Refresh. Expected:
- 6 recap-item grid (semua hidden initially — `.show` belum)
- Tagline hidden

Test sequence manual:
```javascript
['r1','r2','r3','r4','r5','r6'].forEach((id, i) => {
  setTimeout(() => document.getElementById(id).classList.add('show'), i * 80);
});
setTimeout(() => {
  ['r1','r2','r3','r4','r5','r6'].forEach((id, i) => {
    setTimeout(() => document.getElementById(id).classList.add('checked'), i * 200);
  });
}, 1400);
setTimeout(() => document.querySelector('.recap-tagline').classList.add('show'), 3100);
```

Expected:
- 6 item fade-up stagger 80ms
- Setiap item ✓ stamp scale-in stagger 200ms after
- Tagline "ALUR PROTON · 6 LANGKAH TERSTRUKTUR" muncul

Reset + **REMOVE `.active`**.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-outro mini-recap 6 ikon + tagline — alur-pelaksanaan task 11/16

Grid 6 recap-item (ikon + label + ✓ stamp), border-red di item terakhir
(Histori). Fade-up stagger 80ms + ✓ scale-in stagger 200ms. Tagline center
"Alur PROTON · 6 Langkah Terstruktur" via .show class. Final poster
40-45s.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 12: JS Orchestration — milestones + helpers

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah `<script>` block sebelum `</body>`**

Sisipkan tepat sebelum `</body>`:

```html
<script>
  // ===== ALUR PELAKSANAAN PROTON — ORCHESTRATION =====

  function setStep(n) {
    const nodes = document.querySelectorAll('.step-node');
    const conns = document.querySelectorAll('.step-connector');
    nodes.forEach((el, i) => {
      el.classList.remove('active', 'done', 'final');
      if (i + 1 < n) el.classList.add('done');
      if (i + 1 === n) el.classList.add('active');
      if (n === 6 && i + 1 === 6) el.classList.add('final');
    });
    conns.forEach((el, i) => {
      el.classList.toggle('filled', i + 1 < n);
      el.classList.toggle('final', i + 1 === 5 && n === 6);
    });
  }

  function showScene(id) {
    document.querySelectorAll('.scene').forEach(s => s.classList.remove('active'));
    document.getElementById(id).classList.add('active');
  }

  function typeIn(el, text, perChar = 25) {
    el.textContent = '';
    let i = 0;
    const id = setInterval(() => {
      el.textContent += text[i++];
      if (i >= text.length) clearInterval(id);
    }, perChar);
  }

  function fillProgress(id, target) {
    const el = document.getElementById(id);
    el.style.setProperty('--target', target + '%');
    el.classList.add('active');
  }

  function stampFire(id) {
    document.getElementById(id).classList.add('fired');
  }

  function showToast(id) {
    document.getElementById(id).classList.add('show');
  }

  function showRow(id) {
    document.getElementById(id).classList.add('show');
  }

  function highlightCoach() {
    document.querySelector('.role-card.coach').classList.add('highlight');
  }

  const milestones = [
    // Step 1: HC Assign Coach (2-7s)
    { t: 2000, fn: () => { setStep(1); showScene('scene-1'); } },
    { t: 4800, fn: () => highlightCoach() },

    // Step 2: Deliverable IDP (7-15s)
    { t: 7000,  fn: () => { setStep(2); showScene('scene-2'); } },
    { t: 11000, fn: () => fillProgress('upload-bar', 100) },
    { t: 13500, fn: () => showToast('toast-upload') },

    // Step 3: Coaching Proton (15-20s)
    { t: 15000, fn: () => { setStep(3); showScene('scene-3'); } },
    { t: 15600, fn: () => typeIn(document.getElementById('field-diskusi'), 'Membahas hambatan studi kasus refinery ops.') },
    { t: 16500, fn: () => typeIn(document.getElementById('field-kesimpulan'), 'Coachee perlu refresh modul 3.') },
    { t: 17500, fn: () => typeIn(document.getElementById('field-tindak'), 'Penugasan modul ulang minggu depan.') },
    { t: 19000, fn: () => showToast('toast-coaching') },

    // Step 4: Approval (20-28s)
    { t: 20000, fn: () => { setStep(4); showScene('scene-4'); } },
    { t: 20800, fn: () => stampFire('stamp-1') },
    { t: 22500, fn: () => stampFire('stamp-2') },
    { t: 24500, fn: () => stampFire('stamp-3') },
    { t: 26500, fn: () => document.getElementById('status-verif').classList.add('show') },

    // Step 5: Final Assessment (28-33s)
    { t: 28000, fn: () => { setStep(5); showScene('scene-5'); } },
    { t: 31000, fn: () => fillProgress('quiz-bar', 40) },

    // Step 6: Histori (33-40s)
    { t: 33000, fn: () => { setStep(6); showScene('scene-6'); } },
    { t: 33800, fn: () => showRow('row-1') },
    { t: 34500, fn: () => showRow('row-2') },
    { t: 35200, fn: () => showRow('row-3') },
    { t: 35900, fn: () => showRow('row-4') },
    { t: 36600, fn: () => showRow('row-5') },
    { t: 37300, fn: () => showRow('row-6') },
    { t: 38500, fn: () => document.getElementById('histori-caption').classList.add('show') },

    // Outro recap (40-45s)
    { t: 40000, fn: () => { showScene('scene-outro'); document.getElementById('stepper').classList.add('done-all'); } },
    { t: 40400, fn: () => document.getElementById('r1').classList.add('show') },
    { t: 40480, fn: () => document.getElementById('r2').classList.add('show') },
    { t: 40560, fn: () => document.getElementById('r3').classList.add('show') },
    { t: 40640, fn: () => document.getElementById('r4').classList.add('show') },
    { t: 40720, fn: () => document.getElementById('r5').classList.add('show') },
    { t: 40800, fn: () => document.getElementById('r6').classList.add('show') },
    { t: 41800, fn: () => document.getElementById('r1').classList.add('checked') },
    { t: 42000, fn: () => document.getElementById('r2').classList.add('checked') },
    { t: 42200, fn: () => document.getElementById('r3').classList.add('checked') },
    { t: 42400, fn: () => document.getElementById('r4').classList.add('checked') },
    { t: 42600, fn: () => document.getElementById('r5').classList.add('checked') },
    { t: 42800, fn: () => document.getElementById('r6').classList.add('checked') },
    { t: 43500, fn: () => document.querySelector('.recap-tagline').classList.add('show') }
  ];

  milestones.forEach(m => setTimeout(m.fn, m.t));
</script>
```

- [ ] **Step 2: Visual verify end-to-end**

Refresh. Watch 45s end-to-end:

| Time | Expected |
|------|----------|
| 0–2s | Header + grid + stepper appear, all scene hidden |
| 2s | Stepper step 1 active, scene-1 fade-in |
| 4.8s | Coach card highlight red border |
| 7s | Step 1 done, step 2 active, scene-2 |
| 11s | Upload bar fill 0→100% |
| 13.5s | Toast "Bukti terunggah" slide-in |
| 15s | Step 3 active, scene-3 form |
| 15.6–18.3s | 3 field type-in sequential |
| 19s | Toast "Sesi tersimpan" |
| 20s | Step 4 active, scene-4 document |
| 20.8/22.5/24.5s | Stamp 1/2/3 drop sequential |
| 26.5s | TERVERIFIKASI badge |
| 28s | Step 5 active, scene-5 quiz |
| 31s | Quiz progress 12/30 fill |
| 33s | Step 6 active final red, scene-6 timeline |
| 33.8–37.3s | 6 row slide-in stagger |
| 38.5s | Caption "Rekam jejak permanen" |
| 40s | Scene-outro fade-in, stepper done-all |
| 40.4–40.8s | 6 recap-item show stagger |
| 41.8–42.8s | 6 ✓ stamp sequential |
| 43.5s | Tagline appear |
| 43.5–45s | Hold final poster |

Console: no errors. DevTools inspect class state per milestone.

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
feat(proton-video): JS orchestration milestones + helpers — alur-pelaksanaan task 12/16

7 helper (setStep, showScene, typeIn, fillProgress, stampFire, showToast,
showRow, highlightCoach) + 40-entry milestone array drive 45s timeline.
Step transitions, cross-fade scene, stamp fire, counter tween, recap stagger.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 13: `prefers-reduced-motion` media query + JS branch

**Files:**
- Modify: `docs/assets/proton-video/alur-pelaksanaan.html`

- [ ] **Step 1: Tambah `@media (prefers-reduced-motion: reduce)` di `<style>`**

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
    .scene#scene-outro { opacity: 1; transform: none; }
    .title em { background-size: 100% 6px; }
    .recap-item, .recap-tagline { opacity: 1; transform: none; }
    .recap-item .r-check { opacity: 1; transform: scale(1); }
  }
```

- [ ] **Step 2: Update `<script>` dengan reduced-motion branch**

Replace `milestones.forEach(m => setTimeout(m.fn, m.t));` (akhir script) jadi:

```javascript
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (reduced) {
    showScene('scene-outro');
    setStep(6);
    document.getElementById('stepper').classList.add('done-all');
    ['r1','r2','r3','r4','r5','r6'].forEach(id => {
      document.getElementById(id).classList.add('show', 'checked');
    });
    document.querySelector('.recap-tagline').classList.add('show');
  } else {
    milestones.forEach(m => setTimeout(m.fn, m.t));
  }
```

- [ ] **Step 3: Visual verify reduced motion**

Buka DevTools → ⋮ → More tools → Rendering. "Emulate CSS media feature prefers-reduced-motion" → set "reduce".

Refresh. Expected:
- No animation visible
- Layout final state immediate: scene-outro visible (6 recap-item dengan ✓ ticked + tagline visible)
- Stepper done-all (all 6 ticked, last red)
- Header dengan em underline already drawn
- Brand mark visible
- No setTimeout chain firing

Toggle balik ke "No preference", refresh — animasi full kembali jalan.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.html
git commit -m "$(cat <<'EOF'
a11y(proton-video): prefers-reduced-motion fallback — alur-pelaksanaan task 13/16

CSS reduce animation/transition ke 0.01ms, force scene-outro visible
sebagai final state (recap + tagline). JS matchMedia branch: skip
milestones, langsung set final state semua (showScene outro, setStep 6,
stepper done-all, recap show+checked, tagline show). Playwright record
default no-preference = full motion play.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 14: Record Script `record-alur-pelaksanaan.mjs`

**Files:**
- Create: `docs/assets/proton-video/record-alur-pelaksanaan.mjs`

- [ ] **Step 1: Tulis script record**

Tulis `docs/assets/proton-video/record-alur-pelaksanaan.mjs`:

```javascript
// Record alur-pelaksanaan.html to WebM via Playwright
// Usage: node docs/assets/proton-video/record-alur-pelaksanaan.mjs
//
// Output: docs/assets/proton-video/alur-pelaksanaan.webm (46s, 1920x1080)
// Convert to MP4: ffmpeg -i alur-pelaksanaan.webm -c:v libx264 -crf 18 -preset slow alur-pelaksanaan.mp4

import { chromium } from 'playwright';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import http from 'node:http';

const here = path.dirname(fileURLToPath(import.meta.url));
const PORT = 8768;
const URL = `http://127.0.0.1:${PORT}/alur-pelaksanaan.html`;
const OUT_DIR = here;
const RECORD_DURATION_MS = 46_000; // 45s animation + 1s buffer

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
  const finalPath = path.join(OUT_DIR, 'alur-pelaksanaan.webm');
  await fs.rename(videoPath, finalPath);
  console.log('[record] Final path:', finalPath);
} catch (err) {
  console.error('[record] ERROR:', err);
  process.exitCode = 1;
} finally {
  server.kill('SIGTERM');
}
```

Catatan: PORT 8768 (beda dari track-progresi 8767) untuk hindari konflik.

- [ ] **Step 2: Commit**

```bash
git add docs/assets/proton-video/record-alur-pelaksanaan.mjs
git commit -m "$(cat <<'EOF'
feat(proton-video): record script Playwright → alur-pelaksanaan.webm — task 14/16

Port 8768 (hindari konflik track-progresi 8767), record 46s window (45s + 1s
buffer), viewport 1920×1080 chromium headless. Pattern sama dengan
record-track-progresi.mjs.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 15: Run record + verify webm output

**Files:**
- Generate: `docs/assets/proton-video/alur-pelaksanaan.webm`

- [ ] **Step 1: Stop existing http.server di port 8800 (kalau perlu)**

Record script start sendiri server di port 8768. Pastikan port itu free.

- [ ] **Step 2: Run record script**

Run:
```bash
node "docs/assets/proton-video/record-alur-pelaksanaan.mjs"
```

Expected log:
```
[record] Server ready, launching browser...
[record] Recording 46s animation...
[record] Video saved (raw): <temp .webm path>
[record] Final path: .../docs/assets/proton-video/alur-pelaksanaan.webm
```

Total command duration ~55-60s.

- [ ] **Step 3: Verify file output**

Run:
```bash
ls -la docs/assets/proton-video/alur-pelaksanaan.webm
```

Expected: file ada, size 4–7 MB range. Kalau <1 MB → kemungkinan record blank, debug HTML console errors.

Buka file di Chrome (drag-drop):
- Durasi 45–46s
- Visual sesuai task 12 timeline table
- Resolusi 1920×1080
- No glitch

- [ ] **Step 4: Commit hasil record**

```bash
git add docs/assets/proton-video/alur-pelaksanaan.webm
git commit -m "$(cat <<'EOF'
chore(proton-video): generate alur-pelaksanaan.webm 45s — task 15/16

PROTON video bagian 4 animasi 45s, 1920×1080 chromium headless. 7 scene
(6 step + outro) cross-fade, hybrid stepper + scene-main-area pattern.
Match track-progresi visual identity (navy+red+Inter+grid backdrop).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 16: Final verification + DOD check

**Files:** none (verification only)

- [ ] **Step 1: Manual SC verification per Section 12 spec**

Buka spec `docs/superpowers/specs/2026-05-21-alur-pelaksanaan-proton-6-langkah-design.md` Section 12. Verify 20 SC:

| SC | Verify | Pass? |
|----|--------|-------|
| SC1 | Stopwatch .webm durasi 45–46s | ☐ |
| SC2 | Action density ≥85% (target 96%) — review window kosong manual | ☐ |
| SC3 | 6 scene UI tampil sequential, no overlap glitch | ☐ |
| SC4 | Stepper progress 1→6 sync milestones (class inspect) | ☐ |
| SC5 | Scene 1 arrow + ASSIGN label visible | ☐ |
| SC6 | Scene 2 sidebar + task + upload bar + toast | ☐ |
| SC7 | Scene 3 form type-in 3 field sequential | ☐ |
| SC8 | Scene 4 document + 3 stamp + impact ring + status badge | ☐ |
| SC9 | Scene 5 quiz + option B + progress 12/30 | ☐ |
| SC10 | Scene 6 timeline 6 row + sertifikat ⭐ red | ☐ |
| SC11 | Outro recap + ✓ stagger + tagline | ☐ |
| SC12 | Cross-fade smooth | ☐ |
| SC13 | Palette+typo match track-progresi visual side-by-side | ☐ |
| SC14 | Title em underline draw red | ☐ |
| SC15 | DevTools `prefers-reduced-motion: reduce` → static final visible | ☐ |
| SC16 | `wc -c docs/assets/proton-video/alur-pelaksanaan.html` < 35000 | ☐ |
| SC17 | `.webm` 45–46s, 4–7 MB, no glitch | ☐ |
| SC18 | Re-run `node record-alur-pelaksanaan.mjs` idempotent | ☐ |
| SC19 | `git diff docs/Naskah Video PROTON.docx` no new change | ☐ |
| SC20 | Spec Section 16 audio sync window documented | ☐ (spec exists) |

- [ ] **Step 2: Run file size check**

```bash
wc -c docs/assets/proton-video/alur-pelaksanaan.html
```

Expected: <35000 bytes (target ~30-33 KB).

- [ ] **Step 3: Verify git status bersih**

```bash
git status docs/assets/proton-video/ docs/superpowers/
```

Expected:
- `alur-pelaksanaan.html` — clean
- `record-alur-pelaksanaan.mjs` — clean
- `alur-pelaksanaan.webm` — clean
- Spec + plan file — clean
- File untracked lama (`track-progresi-v-G.html`, dll) — abaikan

- [ ] **Step 4: Verify naskah docx not changed in plan ini**

```bash
git status "docs/Naskah Video PROTON.docx"
```

Expected: tidak ada change baru dari task 1-15 (kalau ada modif pre-existing dari session sebelumnya, biarkan, bukan tanggung jawab plan ini).

- [ ] **Step 5: Final spec update kalau ada deviasi**

Kalau saat execute ada deviasi dari spec (timing tweak, layout adjust), update spec dan commit:

```bash
git add docs/superpowers/specs/2026-05-21-alur-pelaksanaan-proton-6-langkah-design.md
git commit -m "$(cat <<'EOF'
docs(proton-video): update spec — capture execution deviations

[describe deviations briefly]

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

Kalau no deviasi, skip step ini.

---

## Definition of Done

- [ ] All 16 task completed + atomic commits
- [ ] All 20 SC pass (Task 16 Step 1)
- [ ] `alur-pelaksanaan.html` < 35 KB
- [ ] `alur-pelaksanaan.webm` 4–7 MB, 45–46s playable
- [ ] `record-alur-pelaksanaan.mjs` idempotent
- [ ] Naskah docx tidak di-touch dari plan ini
- [ ] `git status` bersih
- [ ] Spec di-update kalau ada deviasi (atau tetap kalau none)
- [ ] DevTools `prefers-reduced-motion: reduce` simulate → static layout langsung visible

---

## Rollback (kalau gagal)

3 file baru: `alur-pelaksanaan.html`, `record-alur-pelaksanaan.mjs`, `alur-pelaksanaan.webm`. Kalau gagal di tengah:

```bash
git log --oneline docs/assets/proton-video/alur-pelaksanaan.html
git revert <task1-commit>..<lastcommit>
```

Atau reset hard (destructive — konfirmasi dulu):

```bash
git reset --hard <commit-before-task-1>
```

---

## Notes for Executor

- Task 0 setup harus jalan dulu (Playwright + http.server).
- Task 1–13 semua edit `alur-pelaksanaan.html` single file → no parallelization. Sequential atomic commits.
- Setiap task 5-11 (per-scene) ada "smoke test" via `.active` class manual + DevTools console — ingat REMOVE `.active` setelah verify supaya scene tidak aktif default (JS milestone Task 12 yang aktivasi).
- Task 14 record script independent file — bisa kapan saja sebelum Task 15.
- Task 15 record command ~55-60s wall-clock. Jalankan setelah Task 13 complete + visual verify Task 12 pass.
- Task 16 verification — manual checklist.
- Setiap commit subject ≤80 char, body caveman style.
