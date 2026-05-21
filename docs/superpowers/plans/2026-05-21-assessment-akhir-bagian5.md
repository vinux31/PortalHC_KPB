# Assessment Akhir Per Tahap (Bagian 5) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bangun animasi HTML 16:9 standalone PROTON video bagian 5 (Assessment Akhir Per Tahap, 20s), 2 scene cross-fade (Quiz Y1+Y2 + Result hasil dengan confetti hybrid + badge LULUS), record `.webm` lewat Playwright. Y3 panel juri out-of-scope.

**Architecture:** Single file `docs/assets/proton-video/assessment-akhir.html`. 2 scene main-area cross-fade. Reuse primitives series (browser-frame, card pop spring, progress bar sweep, counter rAF tween, grid backdrop, header pattern). New primitives: stage chip Y1/Y2 toggle, badge LULUS stamp+glow, confetti burst radial (12 partikel), confetti fall ambient (24 flake). JS inline ~75 LOC.

**Tech Stack:** HTML5 + CSS3 (`@keyframes`, custom props, `clamp`, `aspect-ratio`), vanilla JS (`requestAnimationFrame`, `setTimeout`, `matchMedia`), Inter via Google Fonts, Playwright chromium, Python `http.server`.

**Spec reference:** `docs/superpowers/specs/2026-05-21-assessment-akhir-bagian5-design.md`

---

## File Structure

| File | Status | Tanggung jawab |
|------|--------|----------------|
| `docs/assets/proton-video/assessment-akhir.html` | CREATE | Animasi 20s, 2 scene cross-fade, CSS+JS inline |
| `docs/assets/proton-video/record-assessment-akhir.mjs` | CREATE | Playwright record 21s, port 8769 |
| `docs/assets/proton-video/assessment-akhir.webm` | GENERATE | Output 20s, 1920×1080, 3-5 MB |
| `docs/Naskah Video PROTON.docx` | NO CHANGE | Out-of-scope |

---

## Verification Approach

Manual visual check di browser (no unit tests, animasi HTML).

1. Start `python -m http.server 8800` di `docs/assets/proton-video/` (Task 0).
2. Buka `http://localhost:8800/assessment-akhir.html` Chrome.
3. Refresh setelah tiap edit.
4. DevTools Elements panel inspect class state.
5. DevTools Rendering simulate `prefers-reduced-motion: reduce` untuk Task 9.

---

## Task 0: Setup — verify Playwright + start server

**Files:** none

- [ ] **Step 1: Verify Playwright**

```bash
node -e "import('playwright').then(m => console.log('playwright ok'))"
```

Expected: `playwright ok`. Kalau error:
```bash
npm install --no-save playwright
npx playwright install chromium
```

- [ ] **Step 2: Start http server background**

Terminal terpisah:
```bash
cd "docs/assets/proton-video" && python -m http.server 8800
```

**No commit.**

---

## Task 1: Scaffold HTML — stage + palette + grid + header + brand mark

**Files:**
- Create: `docs/assets/proton-video/assessment-akhir.html`

- [ ] **Step 1: Create file dengan skeleton**

Tulis `docs/assets/proton-video/assessment-akhir.html`:

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8">
<title>Animasi Assessment Akhir Per Tahap — PROTON Video Bagian 5 (1:55–2:10)</title>
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
    --gold:      #FFC107;
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
      <div class="eyebrow">Bagian 5 · Assessment Akhir</div>
      <h1 class="title">Standar Penilaian <em>Per Tahap</em></h1>
    </div>

    <!-- scenes container akan ditambahkan task berikutnya -->

    <div class="brand-mark">PROTON × KPB</div>
  </div>
</body>
</html>
```

- [ ] **Step 2: Visual verify**

Buka `http://localhost:8800/assessment-akhir.html`. Expected:
- Stage 16:9, grid backdrop hairline navy 4%
- Eyebrow red uppercase "BAGIAN 5 · ASSESSMENT AKHIR"
- Title navy "Standar Penilaian Per Tahap" dengan "Per Tahap" red + underline red draw t=1.2s
- Brand mark "PROTON × KPB" pojok kanan-bawah

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scaffold assessment-akhir — stage + header + brand mark — task 1/12

Stage 16:9 max 1600px, grid backdrop hairline navy 4%, header eyebrow red +
title "Standar Penilaian Per Tahap" em underline draw red 1.2s, brand mark
PROTON × KPB pojok kanan-bawah. Palette includes --gold #FFC107 untuk confetti.
Match series visual identity (track-progresi + alur-pelaksanaan).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Scene container + cross-fade transition CSS

**Files:**
- Modify: `docs/assets/proton-video/assessment-akhir.html`

- [ ] **Step 1: Tambah CSS `.scenes` + `.scene`**

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
```

- [ ] **Step 2: Tambah markup `.scenes` container**

Sisipkan di `.stage`, antara header dan brand-mark:

```html
    <div class="scenes">
      <!-- 2 scene akan ditambahkan task 4-5 -->
    </div>
```

- [ ] **Step 3: Visual verify**

Refresh. Expected: no visual change (scenes container empty). Layout tetap header + brand mark.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene container + cross-fade CSS — assessment-akhir task 2/12

.scenes flex-1 container, .scene absolute inset-0 opacity-0, .scene.active
opacity-1 + translateY-0 spring. Single active scene at a time. JS-toggle
via showScene() di task 8.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Browser-frame reusable CSS

**Files:**
- Modify: `docs/assets/proton-video/assessment-akhir.html`

- [ ] **Step 1: Tambah CSS `.browser-frame`**

Sisipkan di `<style>` setelah `.scene.active`:

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

- [ ] **Step 2: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
feat(proton-video): browser-frame reusable CSS — assessment-akhir task 3/12

Reusable frame: bar 28px height (3 traffic dot + URL strip), body padding +
min-height + position relative. Reused by scene-quiz + scene-result.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Scene Quiz Y1+Y2 (markup + CSS + chip + progress + caption)

**Files:**
- Modify: `docs/assets/proton-video/assessment-akhir.html`

- [ ] **Step 1: Tambah CSS scene-quiz**

Sisipkan di `<style>` setelah `.browser-body`:

```css
  /* ===== SCENE QUIZ ===== */
  .quiz-body {
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .stage-chips {
    display: flex;
    gap: clamp(8px, 1vw, 16px);
    margin-bottom: clamp(8px, 1vw, 14px);
  }
  .stage-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 6px 14px;
    border-radius: 20px;
    background: var(--paper);
    border: 1px solid var(--hairline);
    font-size: clamp(11px, 0.95vw, 14px);
    font-weight: 700;
    color: var(--muted);
    letter-spacing: 0.08em;
    text-transform: uppercase;
    transition: all 0.4s cubic-bezier(0.34, 1.56, 0.64, 1);
  }
  .stage-chip.active {
    background: var(--navy);
    border-color: var(--navy);
    color: #FFFFFF;
    transform: scale(1.08);
  }
  .stage-chip.done {
    background: var(--navy-soft);
    border-color: var(--navy-soft);
    color: #FFFFFF;
    transform: scale(1);
  }
  .stage-chip.done::before {
    content: "✓ ";
    font-weight: 900;
  }

  .quiz-meta {
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 700;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    opacity: 0;
    transform: translateY(6px);
  }
  #scene-quiz.active .quiz-meta { animation: fadeUp 0.4s 0.8s ease-out forwards; }

  .quiz-card {
    padding: clamp(14px, 1.6vw, 22px);
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 8px;
    opacity: 0;
    transform: translateY(8px);
    transition: opacity 0.3s ease;
  }
  #scene-quiz.active .quiz-card { animation: fadeUp 0.5s 1.2s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }

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
    opacity: 0;
    transform: translateY(4px);
    transition: border-color 0.3s ease, background 0.3s ease, font-weight 0.3s ease, color 0.3s ease;
  }
  #scene-quiz.active .opt:nth-of-type(1) { animation: fadeUp 0.3s 1.6s ease-out forwards; }
  #scene-quiz.active .opt:nth-of-type(2) { animation: fadeUp 0.3s 1.75s ease-out forwards; }
  #scene-quiz.active .opt:nth-of-type(3) { animation: fadeUp 0.3s 1.90s ease-out forwards; }
  #scene-quiz.active .opt:nth-of-type(4) { animation: fadeUp 0.3s 2.05s ease-out forwards; }
  .opt.selected {
    border-color: var(--navy);
    background: rgba(15,45,92,0.06);
    font-weight: 700;
    color: var(--navy-deep);
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
    transition: width 1.3s cubic-bezier(0.65, 0, 0.35, 1);
  }
  .progress-bar.active .fill { width: var(--target, 100%); }

  .quiz-caption {
    margin-top: 8px;
    text-align: center;
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 700;
    color: var(--muted);
    letter-spacing: 0.08em;
    text-transform: uppercase;
    opacity: 0;
    transition: opacity 0.5s ease;
  }
  .quiz-caption.show { opacity: 1; }
```

- [ ] **Step 2: Tambah markup scene-quiz di `.scenes`**

Sisipkan di dalam `.scenes` container:

```html
      <div class="scene" id="scene-quiz">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Assessment Online</em></span>
          </div>
          <div class="browser-body quiz-body">
            <div class="stage-chips">
              <span class="stage-chip" id="chip-y1">Tahun 1</span>
              <span class="stage-chip" id="chip-y2">Tahun 2</span>
            </div>
            <div class="quiz-meta" id="q-meta">Sub-Kompetensi: Refinery Ops · Soal 5/10</div>
            <div class="quiz-card">
              <div class="q-label">Pertanyaan</div>
              <div class="q-text" id="q-text">Apa fungsi utama refinery catalyst dalam proses cracking?</div>
              <div class="options">
                <label class="opt"><input type="radio" disabled> A) Menyimpan crude oil</label>
                <label class="opt"><input type="radio" disabled> B) Meningkatkan suhu boiler</label>
                <label class="opt"><input type="radio" disabled> C) Mempercepat reaksi kimia</label>
                <label class="opt"><input type="radio" disabled> D) Memurnikan air pendingin</label>
              </div>
            </div>
            <div class="progress-strip">
              <span class="p-label">Progres</span>
              <div class="progress-bar" id="quiz-progress"><div class="fill"></div></div>
              <span class="p-count">10 / 10</span>
            </div>
            <div class="quiz-caption" id="quiz-caption">10 soal · penilaian otomatis</div>
          </div>
        </div>
      </div>
```

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-quiz Y1+Y2 markup + CSS — assessment-akhir task 4/12

Browser frame "Assessment Online" dengan stage chip Y1/Y2 (pending/active/done
state), quiz card (label+text+4 option pilihan ganda + selected highlight),
progress bar + caption. Animasi fade-up stagger 150ms opt scoped #scene-quiz.active.
Markup ready untuk JS milestone task 8.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Scene Result (markup + result card + score block + badge + meta + pass caption)

**Files:**
- Modify: `docs/assets/proton-video/assessment-akhir.html`

- [ ] **Step 1: Tambah CSS scene-result**

Sisipkan di `<style>` setelah block `.quiz-caption.show`:

```css
  /* ===== SCENE RESULT ===== */
  .result-body {
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .result-card {
    position: relative;
    z-index: 2;
    width: clamp(360px, 38vw, 540px);
    padding: clamp(24px, 2.4vw, 36px);
    background: #FFFFFF;
    border: 1px solid var(--hairline);
    border-radius: 12px;
    text-align: center;
    box-shadow: 0 1px 2px rgba(10,36,71,0.03), 0 12px 32px rgba(10,36,71,0.08);
    opacity: 0;
    transform: translateY(12px) scale(0.96);
  }
  #scene-result.active .result-card { animation: cardPop 0.6s 0.4s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  @keyframes cardPop {
    0%   { opacity: 0; transform: translateY(12px) scale(0.96); }
    100% { opacity: 1; transform: translateY(0) scale(1); }
  }

  .r-title {
    font-size: clamp(12px, 1vw, 14px);
    font-weight: 800;
    color: var(--navy-deep);
    text-transform: uppercase;
    letter-spacing: 0.15em;
    opacity: 0;
  }
  #scene-result.active .r-title { animation: fadeUp 0.4s 1.0s ease-out forwards; }

  .score-block {
    display: flex;
    align-items: baseline;
    justify-content: center;
    gap: 8px;
    margin: clamp(16px, 2vw, 24px) 0 4px;
  }
  .score-num {
    font-size: clamp(72px, 8vw, 120px);
    font-weight: 900;
    color: var(--navy-deep);
    font-variant-numeric: tabular-nums;
    line-height: 1;
  }
  .score-unit {
    font-size: clamp(20px, 2vw, 32px);
    font-weight: 700;
    color: var(--muted);
  }
  .score-label {
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 800;
    color: var(--navy-soft);
    letter-spacing: 0.2em;
    text-transform: uppercase;
    margin-bottom: clamp(14px, 1.6vw, 22px);
    opacity: 0;
  }
  #scene-result.active .score-label { animation: fadeIn 0.4s 2.0s ease-out forwards; }

  .badge-lulus {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 10px 28px;
    background: #FFFFFF;
    border: 3px solid var(--red);
    border-radius: 999px;
    font-size: clamp(18px, 1.8vw, 28px);
    font-weight: 900;
    color: var(--red);
    letter-spacing: 0.15em;
    opacity: 0;
    transform: translateY(-40px) rotate(-15deg) scale(0.5);
    position: relative;
  }
  .badge-lulus .b-tick {
    font-size: 1.1em;
    color: var(--red);
  }
  .badge-lulus.fired {
    animation:
      badgeDrop 0.6s cubic-bezier(0.34, 1.56, 0.64, 1) forwards,
      badgeGlow 2.5s 0.6s ease-out 2;
  }
  @keyframes badgeDrop {
    60%  { opacity: 1; transform: translateY(8px) rotate(2deg) scale(1.1); }
    100% { opacity: 1; transform: translateY(0) rotate(-3deg) scale(1); }
  }
  @keyframes badgeGlow {
    0%, 100% { box-shadow: 0 0 0 4px rgba(230,51,41,0.15); }
    50%      { box-shadow: 0 0 0 16px rgba(230,51,41,0.06); }
  }
  .badge-lulus.fired::after {
    content: "";
    position: absolute;
    inset: -8px;
    border: 3px solid var(--red);
    border-radius: 999px;
    opacity: 0;
    animation: badgeImpact 0.6s ease-out 0.4s forwards;
  }
  @keyframes badgeImpact {
    0%   { opacity: 0.9; transform: scale(0.7); }
    100% { opacity: 0;   transform: scale(1.5); }
  }

  .result-meta {
    margin-top: clamp(14px, 1.6vw, 22px);
    font-size: clamp(11px, 0.95vw, 14px);
    font-weight: 600;
    color: var(--muted);
    opacity: 0;
    transform: translateY(6px);
    transition: opacity 0.4s ease, transform 0.4s ease;
  }
  .result-meta.show { opacity: 1; transform: translateY(0); }

  .pass-caption {
    position: absolute;
    bottom: clamp(8px, 1vw, 16px);
    left: 50%;
    transform: translateX(-50%);
    z-index: 4;
    font-size: clamp(11px, 0.9vw, 13px);
    font-weight: 700;
    color: var(--navy-soft);
    letter-spacing: 0.1em;
    text-transform: uppercase;
    opacity: 0;
    text-align: center;
    white-space: nowrap;
    transition: opacity 0.6s ease;
  }
  .pass-caption.show { opacity: 1; }
```

- [ ] **Step 2: Tambah markup scene-result**

Sisipkan setelah scene-quiz di `.scenes`:

```html
      <div class="scene" id="scene-result">
        <div class="browser-frame">
          <div class="browser-bar">
            <span class="dots"><i></i><i></i><i></i></span>
            <span class="url">portalhc.kpb · <em>Hasil Assessment</em></span>
          </div>
          <div class="browser-body result-body">
            <!-- confetti containers akan ditambahkan task 6 + 7 -->
            <div class="result-card">
              <div class="r-title">Hasil Assessment</div>
              <div class="score-block">
                <span class="score-num" id="score-num">0</span>
                <span class="score-unit">/100</span>
              </div>
              <div class="score-label">NILAI</div>
              <div class="badge-lulus">
                <span class="b-tick">✓</span>
                <span class="b-text">LULUS</span>
              </div>
              <div class="result-meta" id="result-meta">8 dari 10 soal benar · Durasi 45 menit</div>
            </div>
            <div class="pass-caption" id="pass-caption">Nilai minimal 75 — syarat naik ke tahap berikutnya</div>
          </div>
        </div>
      </div>
```

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
feat(proton-video): scene-result card + score + badge + caption — assessment-akhir task 5/12

Browser frame "Hasil Assessment" dengan result-card centered (z=2), score block
(0 → counter tween 85 + /100 + NILAI), badge LULUS compound animation
(badgeDrop spring + badgeGlow pulse 2× + badgeImpact ring expand), meta line
shown via .show class, pass-caption (z=4 above confetti) shown via .show.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Confetti burst (12 partikel radial)

**Files:**
- Modify: `docs/assets/proton-video/assessment-akhir.html`

- [ ] **Step 1: Tambah CSS `.confetti-burst`**

Sisipkan di `<style>` setelah `.pass-caption.show`:

```css
  /* ===== CONFETTI BURST (12 partikel radial) ===== */
  .confetti-burst {
    position: absolute;
    top: 50%; left: 50%;
    width: 0; height: 0;
    pointer-events: none;
    z-index: 3;
  }
  .confetti-burst .particle {
    position: absolute;
    width: clamp(8px, 0.8vw, 12px);
    height: clamp(8px, 0.8vw, 12px);
    opacity: 0;
    margin-left: -6px;
    margin-top: -6px;
  }
  .confetti-burst .particle:nth-child(1)  { --angle: 0deg;   background: var(--red);       }
  .confetti-burst .particle:nth-child(2)  { --angle: 30deg;  background: var(--navy);      }
  .confetti-burst .particle:nth-child(3)  { --angle: 60deg;  background: var(--gold);  border-radius: 50%; }
  .confetti-burst .particle:nth-child(4)  { --angle: 90deg;  background: var(--red);       }
  .confetti-burst .particle:nth-child(5)  { --angle: 120deg; background: var(--navy-soft); }
  .confetti-burst .particle:nth-child(6)  { --angle: 150deg; background: var(--gold);  border-radius: 50%; }
  .confetti-burst .particle:nth-child(7)  { --angle: 180deg; background: var(--red);       }
  .confetti-burst .particle:nth-child(8)  { --angle: 210deg; background: var(--navy);      }
  .confetti-burst .particle:nth-child(9)  { --angle: 240deg; background: var(--gold);  border-radius: 50%; }
  .confetti-burst .particle:nth-child(10) { --angle: 270deg; background: var(--red);       }
  .confetti-burst .particle:nth-child(11) { --angle: 300deg; background: var(--navy-soft); }
  .confetti-burst .particle:nth-child(12) { --angle: 330deg; background: var(--gold);  border-radius: 50%; }

  .confetti-burst.fired .particle {
    animation: confettiBurst 1.2s ease-out forwards;
  }
  @keyframes confettiBurst {
    0%   { opacity: 0.9; transform: rotate(var(--angle)) translateX(0) rotate(0deg); }
    100% { opacity: 0;   transform: rotate(var(--angle)) translateX(clamp(120px, 14vw, 220px)) rotate(360deg); }
  }
```

- [ ] **Step 2: Tambah markup `.confetti-burst` di scene-result body**

Cari `<div class="browser-body result-body">` di scene-result. Sisipkan di dalamnya, tepat sebelum `<div class="result-card">`:

```html
            <div class="confetti-burst" aria-hidden="true">
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
              <span class="particle"></span><span class="particle"></span>
            </div>
```

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
feat(proton-video): confetti burst radial 12 partikel — assessment-akhir task 6/12

Burst container absolute top:50% left:50% (center of result-body), z=3.
12 particle dengan --angle 0/30/60/.../330deg, color rotate red/navy/gold/
navy-soft/gold/etc, beberapa border-radius:50% (lingkaran). Animation:
rotate(--angle) translateX(0→clamp 120-220px) rotate(0→360deg), opacity
0.9→0, 1.2s ease-out forwards. Fired via .confetti-burst.fired class.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Confetti fall (24 flake ambient)

**Files:**
- Modify: `docs/assets/proton-video/assessment-akhir.html`

- [ ] **Step 1: Tambah CSS `.confetti-fall`**

Sisipkan di `<style>` setelah keyframe `confettiBurst`:

```css
  /* ===== CONFETTI FALL (24 flake ambient) ===== */
  .confetti-fall {
    position: absolute;
    inset: 0;
    pointer-events: none;
    overflow: hidden;
    z-index: 3;
  }
  .confetti-fall .flake {
    position: absolute;
    top: -20px;
    width: clamp(6px, 0.6vw, 10px);
    height: clamp(6px, 0.6vw, 10px);
    opacity: 0;
  }
  .confetti-fall .flake:nth-child(3n+1)  { background: var(--red); }
  .confetti-fall .flake:nth-child(3n+2)  { background: var(--navy); border-radius: 50%; }
  .confetti-fall .flake:nth-child(3n)    { background: var(--gold); }

  .confetti-fall.active .flake:nth-child(1)  { left: 4%;  animation: confettiFall 3.5s 0.0s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(2)  { left: 10%; animation: confettiFall 3.8s 0.15s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(3)  { left: 16%; animation: confettiFall 3.2s 0.30s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(4)  { left: 22%; animation: confettiFall 3.6s 0.10s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(5)  { left: 28%; animation: confettiFall 3.3s 0.45s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(6)  { left: 34%; animation: confettiFall 3.9s 0.20s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(7)  { left: 40%; animation: confettiFall 3.4s 0.55s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(8)  { left: 46%; animation: confettiFall 3.7s 0.35s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(9)  { left: 52%; animation: confettiFall 3.5s 0.65s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(10) { left: 58%; animation: confettiFall 3.8s 0.05s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(11) { left: 64%; animation: confettiFall 3.2s 0.75s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(12) { left: 70%; animation: confettiFall 3.6s 0.25s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(13) { left: 76%; animation: confettiFall 3.3s 0.85s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(14) { left: 82%; animation: confettiFall 3.9s 0.40s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(15) { left: 88%; animation: confettiFall 3.4s 0.95s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(16) { left: 94%; animation: confettiFall 3.7s 0.50s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(17) { left: 8%;  animation: confettiFall 3.5s 1.05s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(18) { left: 20%; animation: confettiFall 3.8s 1.15s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(19) { left: 32%; animation: confettiFall 3.2s 1.25s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(20) { left: 44%; animation: confettiFall 3.6s 1.20s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(21) { left: 56%; animation: confettiFall 3.3s 1.30s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(22) { left: 68%; animation: confettiFall 3.9s 1.35s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(23) { left: 80%; animation: confettiFall 3.4s 1.40s ease-in forwards; }
  .confetti-fall.active .flake:nth-child(24) { left: 92%; animation: confettiFall 3.7s 1.45s ease-in forwards; }

  @keyframes confettiFall {
    0%   { opacity: 0; transform: translateY(-20px) rotate(0deg); }
    10%  { opacity: 1; }
    90%  { opacity: 1; }
    100% { opacity: 0; transform: translateY(clamp(300px, 35vw, 500px)) rotate(540deg); }
  }
```

- [ ] **Step 2: Tambah markup `.confetti-fall` di scene-result body**

Sisipkan di dalam `<div class="browser-body result-body">`, setelah `.confetti-burst` block, sebelum `.result-card`:

```html
            <div class="confetti-fall" aria-hidden="true">
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
              <span class="flake"></span><span class="flake"></span><span class="flake"></span>
            </div>
```

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
feat(proton-video): confetti fall ambient 24 flake — assessment-akhir task 7/12

Fall container inset:0 overflow:hidden z=3. 24 flake top:-20px, random left%
4-94%, delay stagger 0-1.45s, duration 3.2-3.9s ease-in. Color rotate
red/navy/gold via nth-child(3n+1/2/0). Animation: translateY(-20px → clamp
300-500px) + rotate(0→540deg), opacity fade in t=10% + fade out t=90%.
Triggered via .confetti-fall.active class.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: JS orchestration — milestones + helpers

**Files:**
- Modify: `docs/assets/proton-video/assessment-akhir.html`

- [ ] **Step 1: Tambah `<script>` block sebelum `</body>`**

Sisipkan tepat sebelum `</body>`:

```html
<script>
  // ===== ASSESSMENT AKHIR — ORCHESTRATION =====

  function showScene(id) {
    document.querySelectorAll('.scene').forEach(s => s.classList.remove('active'));
    document.getElementById(id).classList.add('active');
  }

  function setChip(id, state) {
    const el = document.getElementById(id);
    el.classList.remove('active', 'done');
    state.split(' ').forEach(c => el.classList.add(c));
  }

  function fillProgress(id, target) {
    const el = document.getElementById(id);
    el.style.setProperty('--target', target + '%');
    el.classList.add('active');
  }

  function resetProgress(id) {
    const el = document.getElementById(id);
    el.classList.remove('active');
    el.style.setProperty('--target', '0%');
  }

  function tweenCounter(id, from, to, duration) {
    const el = document.getElementById(id);
    const start = performance.now();
    function frame(now) {
      const p = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - p, 3);
      el.textContent = Math.round(from + (to - from) * eased);
      if (p < 1) requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
  }

  function fireBadge() {
    document.querySelector('.badge-lulus').classList.add('fired');
    document.querySelector('.confetti-burst').classList.add('fired');
    setTimeout(() => document.querySelector('.confetti-fall').classList.add('active'), 300);
  }

  function selectOption(idx) {
    document.querySelectorAll('#scene-quiz .opt').forEach(o => o.classList.remove('selected'));
    document.querySelectorAll('#scene-quiz .opt')[idx].classList.add('selected');
  }

  function swapQuizContent() {
    const card = document.querySelector('#scene-quiz .quiz-card');
    card.style.opacity = '0';
    setTimeout(() => {
      document.getElementById('q-text').textContent = 'Bagaimana prinsip kerja energy management di unit refinery?';
      document.getElementById('q-meta').textContent = 'Sub-Kompetensi: Energy Management · Soal 5/10';
      document.querySelectorAll('#scene-quiz .opt').forEach(o => o.classList.remove('selected'));
      card.style.opacity = '1';
    }, 200);
  }

  const milestones = [
    // Quiz Y1 (2-6.5s)
    { t: 2000, fn: () => { showScene('scene-quiz'); setChip('chip-y1', 'active'); } },
    { t: 4800, fn: () => selectOption(2) },
    { t: 5200, fn: () => fillProgress('quiz-progress', 100) },

    // Y1 done → Y2 active (6.5-8.5s)
    { t: 6500, fn: () => { setChip('chip-y1', 'done'); resetProgress('quiz-progress'); swapQuizContent(); } },
    { t: 6800, fn: () => setChip('chip-y2', 'active') },
    { t: 7000, fn: () => fillProgress('quiz-progress', 100) },
    { t: 7500, fn: () => selectOption(1) },

    // Y2 done + caption (8.5-9s)
    { t: 8500, fn: () => setChip('chip-y2', 'done') },
    { t: 9000, fn: () => document.getElementById('quiz-caption').classList.add('show') },

    // Cross-fade to Result (11-12s)
    { t: 11000, fn: () => showScene('scene-result') },

    // Score tween (13.2-14.7s)
    { t: 13200, fn: () => tweenCounter('score-num', 0, 85, 1500) },

    // Meta line (14.7s)
    { t: 14700, fn: () => document.getElementById('result-meta').classList.add('show') },

    // Badge + confetti (15.2s)
    { t: 15200, fn: () => fireBadge() },

    // Pass criteria caption (17.5s)
    { t: 17500, fn: () => document.getElementById('pass-caption').classList.add('show') }
  ];

  milestones.forEach(m => setTimeout(m.fn, m.t));
</script>
```

- [ ] **Step 2: Visual verify end-to-end**

Refresh. Watch 20s timeline:

| Time | Expected |
|------|----------|
| 0–2s | Header fade-down + em underline + brand mark fade. Scene hidden. |
| 2s | scene-quiz active, chip Y1 active scale 1.08 |
| 2.8s | Meta strip fade-up |
| 3.2s | Quiz card fade-up |
| 3.6–4.2s | 4 opt stagger |
| 4.8s | Option C selected (navy highlight) |
| 5.2s | Progress bar fill 0→100% |
| 6.5s | Quiz card fade out 0→200ms, content swap Y1→Y2, chip Y1 done ✓, progress reset 0%, card fade in |
| 6.8s | Chip Y2 active |
| 7.0s | Progress fill 0→100% |
| 7.5s | Option B selected |
| 8.5s | Chip Y2 done ✓ |
| 9.0s | Caption "10 soal · penilaian otomatis" |
| 11s | Cross-fade to scene-result |
| 12.4s | Result card pop spring |
| 13.0s | Title "Hasil Assessment" |
| 13.2–14.7s | Score counter 0 → 85 |
| 14.0s | "/100" + "NILAI" label |
| 14.7s | Meta line "8 dari 10 soal benar" |
| 15.2s | Badge LULUS drop + impact ring + glow pulse |
| 15.5s | Confetti burst 12 partikel radial expand |
| 15.8s | Confetti fall 24 flake start drop |
| 17.5s | Pass caption "Nilai minimal 75" |
| 19–20s | Hold final poster |

Console: no errors. DevTools inspect class state per milestone.

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
feat(proton-video): JS orchestration milestones + helpers — assessment-akhir task 8/12

8 helper (showScene, setChip, fillProgress, resetProgress, tweenCounter,
fireBadge, selectOption, swapQuizContent) + 13-entry milestone array drive
20s timeline. swapQuizContent uses opacity fade 200ms untuk smooth Y1→Y2
swap. fireBadge triggers badge + burst sync + fall ambient 300ms after.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: `prefers-reduced-motion` fallback

**Files:**
- Modify: `docs/assets/proton-video/assessment-akhir.html`

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
    .scene#scene-result { opacity: 1; transform: none; }
    .title em { background-size: 100% 6px; }
    .result-card, .result-meta, .pass-caption { opacity: 1; transform: none; }
  }
```

- [ ] **Step 2: Update `<script>` dengan reduced-motion branch**

Edit `<script>`. Replace `milestones.forEach(m => setTimeout(m.fn, m.t));` (akhir script) jadi:

```javascript
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (reduced) {
    showScene('scene-result');
    document.getElementById('score-num').textContent = '85';
    document.getElementById('result-meta').classList.add('show');
    document.querySelector('.badge-lulus').classList.add('fired');
    document.querySelector('.confetti-burst').classList.add('fired');
    document.querySelector('.confetti-fall').classList.add('active');
    document.getElementById('pass-caption').classList.add('show');
  } else {
    milestones.forEach(m => setTimeout(m.fn, m.t));
  }
```

- [ ] **Step 3: Visual verify reduced motion**

DevTools → ⋮ → More tools → Rendering. "Emulate CSS media feature prefers-reduced-motion" → set "reduce".

Refresh. Expected:
- No animation
- Layout final: scene-result visible (card + score 85 + badge fired + confetti both shown + caption visible)
- Em underline already drawn red
- No setTimeout chain firing

Toggle balik "No preference", refresh — full motion play.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/assessment-akhir.html
git commit -m "$(cat <<'EOF'
a11y(proton-video): prefers-reduced-motion fallback — assessment-akhir task 9/12

CSS reduce animation/transition ke 0.01ms, force scene-result visible (card +
meta + caption opacity 1). JS matchMedia branch: skip setTimeout chain,
langsung set final state (scene-result active, score 85, badge fired, confetti
both, meta+caption shown). Playwright record default no-preference = full
motion play.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: Record script `record-assessment-akhir.mjs`

**Files:**
- Create: `docs/assets/proton-video/record-assessment-akhir.mjs`

- [ ] **Step 1: Tulis script record**

Tulis `docs/assets/proton-video/record-assessment-akhir.mjs`:

```javascript
// Record assessment-akhir.html to WebM via Playwright
// Usage: node docs/assets/proton-video/record-assessment-akhir.mjs
//
// Output: docs/assets/proton-video/assessment-akhir.webm (21s, 1920x1080)
// Convert to MP4: ffmpeg -i assessment-akhir.webm -c:v libx264 -crf 18 -preset slow assessment-akhir.mp4

import { chromium } from 'playwright';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import http from 'node:http';

const here = path.dirname(fileURLToPath(import.meta.url));
const PORT = 8769;
const URL = `http://127.0.0.1:${PORT}/assessment-akhir.html`;
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
  const finalPath = path.join(OUT_DIR, 'assessment-akhir.webm');
  await fs.rename(videoPath, finalPath);
  console.log('[record] Final path:', finalPath);
} catch (err) {
  console.error('[record] ERROR:', err);
  process.exitCode = 1;
} finally {
  server.kill('SIGTERM');
}
```

Port 8769 (beda dari track-progresi 8767 + alur-pelaksanaan 8768) untuk hindari konflik.

- [ ] **Step 2: Commit**

```bash
git add docs/assets/proton-video/record-assessment-akhir.mjs
git commit -m "$(cat <<'EOF'
feat(proton-video): record script Playwright → assessment-akhir.webm — task 10/12

Port 8769 (hindari konflik track-progresi 8767 + alur-pelaksanaan 8768), record
21s window (20s + 1s buffer), viewport 1920×1080 chromium headless. Pattern
sama dengan record-track-progresi.mjs + record-alur-pelaksanaan.mjs.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: Run record + verify webm output

**Files:**
- Generate: `docs/assets/proton-video/assessment-akhir.webm`

- [ ] **Step 1: Run record script**

```bash
node "docs/assets/proton-video/record-assessment-akhir.mjs"
```

Expected log:
```
[record] Server ready, launching browser...
[record] Recording 21s animation...
[record] Video saved (raw): <temp .webm path>
[record] Final path: .../docs/assets/proton-video/assessment-akhir.webm
```

Total command ~30-35s.

- [ ] **Step 2: Verify output**

```bash
ls -la docs/assets/proton-video/assessment-akhir.webm
```

Expected: file ada, size 3-5 MB. Kalau <500 KB → record blank, debug HTML.

Playback Chrome (drag-drop): durasi 20-21s, resolusi 1920×1080, visual sesuai task 8 timeline table, no glitch.

- [ ] **Step 3: Commit hasil record**

```bash
git add docs/assets/proton-video/assessment-akhir.webm
git commit -m "$(cat <<'EOF'
chore(proton-video): generate assessment-akhir.webm 20s — task 11/12

PROTON video bagian 5 animasi 20s, 1920×1080 chromium headless. 2 scene
cross-fade (Quiz Y1+Y2 chip toggle + Result card+badge+confetti hybrid).
Match series visual identity (navy+red+Inter+grid backdrop+spring easing).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 12: Final verification + DOD check

**Files:** none (verification)

- [ ] **Step 1: Manual SC verification per spec Section 12**

Buka spec `docs/superpowers/specs/2026-05-21-assessment-akhir-bagian5-design.md` Section 12. Verify 20 SC:

| SC | Verify | Pass? |
|----|--------|-------|
| SC1 | Stopwatch .webm 20-21s | ☐ |
| SC2 | Quiz Y1 active → done → Y2 active → done chip transition | ☐ |
| SC3 | Quiz content swap Y1→Y2 opacity fade | ☐ |
| SC4 | Progress bar fill 0→100% × 2 | ☐ |
| SC5 | Cross-fade quiz → result smooth | ☐ |
| SC6 | Result card pop spring | ☐ |
| SC7 | Score counter 0 → 85 | ☐ |
| SC8 | Badge LULUS stamp drop + impact + glow pulse 2× | ☐ |
| SC9 | Confetti burst 12 partikel radial | ☐ |
| SC10 | Confetti fall 24 flake ambient | ☐ |
| SC11 | Pass caption visible z=4 above confetti | ☐ |
| SC12 | Palette+typo match series | ☐ |
| SC13 | Em "Per Tahap" underline draw red | ☐ |
| SC14 | Browser frame "Assessment Online" + "Hasil Assessment" | ☐ |
| SC15 | DevTools reduced-motion → static visible | ☐ |
| SC16 | `wc -c assessment-akhir.html` < 30000 | ☐ |
| SC17 | .webm 20-21s, 3-5 MB, no glitch | ☐ |
| SC18 | Re-run record idempotent | ☐ |
| SC19 | `git diff docs/Naskah Video PROTON.docx` no new change | ☐ |
| SC20 | Spec Section 16 audio sync window exists | ☐ |

- [ ] **Step 2: File size check**

```bash
wc -c docs/assets/proton-video/assessment-akhir.html
```

Expected: <30000 bytes (target ~25-29 KB).

- [ ] **Step 3: Git status check**

```bash
git status docs/assets/proton-video/ docs/superpowers/
```

Expected:
- `assessment-akhir.html` — clean
- `record-assessment-akhir.mjs` — clean
- `assessment-akhir.webm` — clean
- Spec + plan file — clean

- [ ] **Step 4: Final spec update kalau ada deviasi**

Kalau saat execute ada deviasi, update spec + commit:

```bash
git add docs/superpowers/specs/2026-05-21-assessment-akhir-bagian5-design.md
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

- [ ] All 12 task completed + atomic commits (Task 1-11; Task 0 + 12 no commit)
- [ ] All 20 SC pass (Task 12 Step 1)
- [ ] `assessment-akhir.html` < 30 KB
- [ ] `assessment-akhir.webm` 3-5 MB, 20-21s playable
- [ ] `record-assessment-akhir.mjs` idempotent
- [ ] Naskah docx tidak di-touch
- [ ] `git status` bersih
- [ ] DevTools `prefers-reduced-motion: reduce` simulate → static visible

---

## Rollback (kalau gagal)

3 file baru: `assessment-akhir.html`, `record-assessment-akhir.mjs`, `assessment-akhir.webm`. Kalau gagal di tengah:

```bash
git log --oneline docs/assets/proton-video/assessment-akhir.html
git revert <task1-commit>..<lastcommit>
```

Atau reset hard (destructive, konfirmasi dulu):

```bash
git reset --hard <commit-before-task-1>
```

---

## Notes for Executor

- Task 0 setup harus jalan dulu (Playwright + http.server).
- Task 1-9 semua edit `assessment-akhir.html` single file → no parallelization. Sequential atomic commits.
- Task 10 record script independent file.
- Task 11 record command ~30-35s wall-clock. Jalankan setelah Task 9 complete + visual verify Task 8 pass.
- Task 12 verification — manual checklist.
- Setiap commit subject ≤80 char, body caveman style.
