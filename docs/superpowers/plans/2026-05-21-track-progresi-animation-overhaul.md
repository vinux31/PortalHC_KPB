# Track & Progresi Animation Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Overhaul `track-progresi.html` dari 25s pacing longgar ke **35s editorial-dataviz hybrid** (D-mode primer + B-mode spring accent) dengan motion continuity (SVG backbone line draw), climax punchy (Y3 burst ring + glow pulse), accessibility (`prefers-reduced-motion`), dan action density 46% → 73%.

**Architecture:** Single-file edit-in-place pada `docs/assets/proton-video/track-progresi.html`. Pure CSS animation untuk visual primitive (chip stamp, glow ring, burst, dot breath, underline draw, grid backdrop), SVG overlay untuk timeline backbone (`stroke-dasharray` + `stroke-dashoffset` 3-segment animate via CSS keyframe), inline JS minimal ~50 LOC untuk state machine (activate/dim cross-card), counter tick rAF tween, role pulse trigger, dan `prefers-reduced-motion` branch.

**Tech Stack:** HTML5 + CSS3 (`@keyframes`, custom props, `clamp`, `aspect-ratio`), SVG (`stroke-dasharray`, `stroke-dashoffset`, `stroke` morph), vanilla JS (`requestAnimationFrame`, `setTimeout`, `matchMedia`), Inter via Google Fonts, Playwright chromium (record), Python `http.server` (serve lokal saat record).

**Spec reference:** `docs/superpowers/specs/2026-05-21-track-progresi-animation-overhaul-design.md`

---

## File Structure

| File | Status | Tanggung jawab |
|------|--------|----------------|
| `docs/assets/proton-video/track-progresi.html` | MODIFY | Sumber animasi — single file, semua markup + CSS + JS |
| `docs/assets/proton-video/record-track-progresi.mjs` | MODIFY (1 line) | Naikkan `RECORD_DURATION_MS` 26000 → 36000 |
| `docs/assets/proton-video/track-progresi.webm` | REGENERATE | Output record 35s, replace 2.1MB existing |
| `docs/Naskah Video PROTON.docx` | NO CHANGE | Naskah narasi (out-of-scope dalam plan ini) |

File yang tidak boleh disentuh: semua di luar 3 file di atas. File untracked existing (`track-progresi-v-G.html`, `screenshot-*.mjs`, dll) tetap abaikan.

---

## Verification Approach (Non-Unit-Test)

Animasi HTML tidak punya unit test framework. Verifikasi tiap task = **manual visual check di browser**:

1. Jalankan `python -m http.server 8800` di `docs/assets/proton-video/` (background, sekali di awal session).
2. Buka `http://localhost:8800/track-progresi.html` di Chrome.
3. Refresh setelah tiap edit, perhatikan animasi.
4. DevTools untuk inspect class state (Elements panel) + console log (no errors).
5. DevTools "Rendering" panel → toggle `prefers-reduced-motion: reduce` untuk verify fallback.
6. Stopwatch untuk timing verification milestone.

Kalau Playwright belum installed, install dulu (Task 0).

---

## Task 0: Setup — verify Playwright + start local server

**Files:** none (environment setup)

- [ ] **Step 1: Verify Playwright installed**

Run:
```bash
node -e "import('playwright').then(m => console.log('playwright ok'))"
```

Expected: `playwright ok`. Kalau error "Cannot find package":
```bash
npm install --no-save playwright
npx playwright install chromium
```

- [ ] **Step 2: Start http server background**

Run (terminal terpisah):
```bash
cd "docs/assets/proton-video" && python -m http.server 8800
```

Server jalan di `http://localhost:8800`. Biarkan running selama implementation.

- [ ] **Step 3: Buka browser baseline**

Open `http://localhost:8800/track-progresi.html`. Confirm versi 25s sekarang jalan. Screenshot final frame (t=20s) sebagai baseline visual reference.

**No commit (environment only).**

---

## Task 1: Grid backdrop + stage::before pseudo

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Add `.stage` `::before` rule**

Edit `<style>` block. Cari rule `.stage { ... }` (sekitar line 39-51). Tambah di-bawah-nya:

```css
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
```

Catatan: `.stage > *` z-index 1 supaya children muncul di atas grid backdrop. Existing `.stage` sudah punya `position: relative` via `overflow: hidden` cascade? Verify — kalau belum, tambahkan `position: relative` ke `.stage`.

- [ ] **Step 2: Verify `.stage` punya `position: relative`**

Cari rule `.stage` di line 39-51. Confirm `position: relative;` ada. Kalau tidak, tambahkan setelah `aspect-ratio: 16 / 9;`.

- [ ] **Step 3: Visual verify**

Refresh browser. Expected: grid 40px×40px tipis abu-abu navy 4% opacity muncul di belakang konten. Tidak dominant, tidak distract. Header + role row + kartu tetap di atas.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): grid backdrop editorial — track-progresi 35s overhaul task 1/17

Static grid 40px hairline navy 4% opacity sebagai backdrop. Z-index 0 di belakang
content layer (z=1). Pseudo-element fade-in 0.4s.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Remove existing `.arrow` elements (cleanup before backbone)

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

Reasoning: Backbone SVG (Task 3) akan menjadi satu-satunya konektor visual. Arrow connector sekarang akan tumpang tindih kalau dibiarkan. Bersihkan markup + CSS lama dulu.

- [ ] **Step 1: Hapus markup arrow 1 dan arrow 2**

Cari dan hapus 2 block ini (sekitar line 349-355 dan 368-374):

```html
      <div class="arrow" id="arrow-1">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
          <line x1="4" y1="12" x2="20" y2="12"/>
          <polyline points="14 6 20 12 14 18"/>
        </svg>
        <span>Naik Tahun</span>
      </div>
```

dan

```html
      <div class="arrow" id="arrow-2">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
          <line x1="4" y1="12" x2="20" y2="12"/>
          <polyline points="14 6 20 12 14 18"/>
        </svg>
        <span>Naik Tahun</span>
      </div>
```

- [ ] **Step 2: Hapus CSS `.arrow` + `.arrow svg`**

Cari dan hapus block (sekitar line 238-252):

```css
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

- [ ] **Step 3: Hapus animation delay rules `#arrow-1` dan `#arrow-2`**

Cari dan hapus (di block "ANIMATION DELAYS"):

```css
  #arrow-1 { animation: arrowIn 0.5s 9.0s ease-out forwards; }
```
dan
```css
  #arrow-2 { animation: arrowIn 0.5s 13.0s ease-out forwards; }
```

- [ ] **Step 4: Hapus keyframe `@keyframes arrowIn`**

Cari dan hapus:
```css
  @keyframes arrowIn {
    to { opacity: 1; }
  }
```

- [ ] **Step 5: Fix timeline grid-template-columns**

Cari `.timeline { ... }` rule (sekitar line 153-160). Ganti:
```css
    grid-template-columns: 1fr auto 1fr auto 1fr;
```
Jadi:
```css
    grid-template-columns: 1fr 1fr 1fr;
```

Tanpa arrow di antara kartu, slot `auto` tidak diperlukan lagi.

- [ ] **Step 6: Visual verify**

Refresh browser. Expected: 3 kartu Y1/Y2/Y3 berjejer langsung tanpa arrow di antaranya. Gap antar kartu tetap (via existing `gap: clamp(12px, 1.2vw, 24px)`). Console no errors.

- [ ] **Step 7: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
refactor(proton-video): hapus .arrow connector lama — track-progresi 35s overhaul task 2/17

Cleanup sebelum SVG backbone. 2 arrow element + CSS + keyframe + animation delay
removed. Grid timeline 5-col → 3-col.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Backbone slot HTML + SVG + 3-segment line draw + stroke morph

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah CSS `.backbone-slot` + `.backbone-path`**

Sisipkan di `<style>` setelah block ROLE ROW (sebelum `@keyframes fadeUp` atau setelah `.role-divider`):

```css
  /* ===== BACKBONE ===== */
  .backbone-slot {
    height: clamp(24px, 2.4vw, 32px);
    margin-bottom: clamp(12px, 1.2vw, 20px);
    position: relative;
  }
  .backbone {
    width: 100%;
    height: 100%;
    display: block;
    overflow: visible;
  }
  .backbone-path {
    fill: none;
    stroke: var(--navy-soft);
    stroke-width: 3;
    stroke-linecap: round;
    stroke-dasharray: 1440;
    stroke-dashoffset: 1440;
    opacity: 0;
    animation:
      backboneAppear 0.5s 2.0s ease-out forwards,
      backboneDraw1 1.5s 3.0s cubic-bezier(0.65, 0, 0.35, 1) forwards,
      backboneDraw2 1.8s 9.2s cubic-bezier(0.65, 0, 0.35, 1) forwards,
      backboneDraw3 1.3s 17.7s cubic-bezier(0.65, 0, 0.35, 1) forwards;
  }
  @keyframes backboneAppear { to { opacity: 0.35; } }
  @keyframes backboneDraw1 { to { stroke-dashoffset: 960; opacity: 1; } }
  @keyframes backboneDraw2 { to { stroke-dashoffset: 480; } }
  @keyframes backboneDraw3 { to { stroke-dashoffset: 0; stroke: var(--red); } }
```

Catatan: `stroke-dasharray: 1440` sesuai viewBox width 1600 dengan margin 8px tiap sisi (1600-16=1584 ~ rounded 1440 untuk drawing distance). Adjust kalau visual tidak proportional.

- [ ] **Step 2: Tambah markup backbone setelah role-row, sebelum timeline**

Cari `</div>` penutup `.role-row` (sekitar line 337). Sisipkan setelahnya:

```html
    <div class="backbone-slot">
      <svg class="backbone" viewBox="0 0 1600 4" preserveAspectRatio="none" aria-hidden="true">
        <path class="backbone-path" d="M 8 2 L 1592 2"/>
      </svg>
    </div>
```

- [ ] **Step 3: Visual verify line draw**

Refresh browser. Watch dari 0s:
- t=2.0s: backbone hairline navy muncul (opacity 0.35)
- t=3.0–4.5s: draw kiri ~33%
- t=9.2–11.0s: lanjut 33→66%
- t=17.7–19.0s: lanjut 66→100% + warna morph navy-soft → red

Ada visual gap karena kartu tahun belum ter-resync. Itu OK untuk sekarang.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): SVG backbone line draw 3-segment — track-progresi 35s overhaul task 3/17

Backbone slot 24-32px antara role-row & timeline. SVG path
stroke-dasharray 1440, stroke-dashoffset 1440→960→480→0, stroke morph
navy-soft → red di segmen Y3 final. Animation delay 2.0/3.0/9.2/17.7s.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Tooltip caption "Naik Tahun" 2×

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah CSS `.backbone-caption`**

Sisipkan di block BACKBONE (setelah keyframe `backboneDraw3`):

```css
  .backbone-caption {
    position: absolute;
    top: -18px;
    font-size: clamp(10px, 0.8vw, 12px);
    font-weight: 700;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: var(--muted);
    transform: translateX(-50%);
    opacity: 0;
    pointer-events: none;
  }
  .backbone-caption.c1 { left: 41.67%; animation: captionFlash 1.6s 9.4s ease-out forwards; }
  .backbone-caption.c2 { left: 75%;    animation: captionFlash 1.6s 17.9s ease-out forwards; }
  @keyframes captionFlash {
    0%   { opacity: 0; transform: translateX(-50%) translateY(4px); }
    25%  { opacity: 1; transform: translateX(-50%) translateY(0); }
    75%  { opacity: 1; }
    100% { opacity: 0; }
  }
```

Position calculation: timeline 3 kartu equal width. Midpoint Y1↔Y2 ≈ 41.67% (between 33.3% and 50%), midpoint Y2↔Y3 ≈ 75% (between 66.7% dan 83.3%). Adjust visual kalau perlu.

- [ ] **Step 2: Tambah markup caption di dalam `.backbone-slot`**

Edit `.backbone-slot` (dari Task 3) jadi:

```html
    <div class="backbone-slot">
      <svg class="backbone" viewBox="0 0 1600 4" preserveAspectRatio="none" aria-hidden="true">
        <path class="backbone-path" d="M 8 2 L 1592 2"/>
      </svg>
      <span class="backbone-caption c1">Naik Tahun</span>
      <span class="backbone-caption c2">Naik Tahun</span>
    </div>
```

- [ ] **Step 3: Visual verify caption timing**

Refresh. Watch:
- t=9.4s: caption "Naik Tahun" muncul di tengah Y1-Y2, fade in 0.4s, hold 0.8s, fade out 0.4s
- t=17.9s: caption #2 sama pattern, di tengah Y2-Y3

Caption tidak overlap dengan kartu (top: -18px = di atas backbone slot, di bawah role-row).

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): backbone caption "Naik Tahun" 2× — track-progresi 35s overhaul task 4/17

Caption fade in/out (1.6s lifespan) di midpoint Y1-Y2 (t=9.4s) dan Y2-Y3
(t=17.9s). Replace "Naik Tahun" label dari arrow lama, sekarang ride
di atas backbone.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Counter markup + styling per kartu

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah CSS `.counter`**

Sisipkan di `<style>` setelah block `.year-theme` (sebelum `.chips`):

```css
  .counter {
    display: flex;
    align-items: baseline;
    gap: 6px;
    margin: 4px 0 2px;
  }
  .counter .num {
    font-size: clamp(28px, 2.4vw, 40px);
    font-weight: 900;
    color: var(--navy-deep);
    font-variant-numeric: tabular-nums;
    line-height: 1;
  }
  .year-card.y3 .counter .num { color: var(--red-dark); }
  .counter .unit {
    font-size: clamp(10px, 0.85vw, 13px);
    font-weight: 700;
    color: var(--muted);
    letter-spacing: 0.12em;
    text-transform: uppercase;
  }
```

- [ ] **Step 2: Tambah markup counter di Y1**

Cari `.year-card.y1`. Sisipkan counter antara `.year-theme` dan `.chips`:

```html
      <div class="year-card y1" id="card-y1">
        <span class="year-badge">Tahun 1</span>
        <div class="year-theme">Foundation</div>
        <div class="counter">
          <span class="num" id="cnt-y1" data-target="2">0</span>
          <span class="unit">Kompetensi</span>
        </div>
        <div class="chips" id="chips-y1">
          ...
        </div>
        ...
      </div>
```

- [ ] **Step 3: Tambah markup counter di Y2 dan Y3**

Y2 counter:
```html
        <div class="counter">
          <span class="num" id="cnt-y2" data-target="3">0</span>
          <span class="unit">Kompetensi</span>
        </div>
```

Y3 counter:
```html
        <div class="counter">
          <span class="num" id="cnt-y3" data-target="2">0</span>
          <span class="unit">Kompetensi</span>
        </div>
```

Sisipkan di posisi sama (antara `.year-theme` dan `.chips`).

- [ ] **Step 4: Visual verify layout**

Refresh. Expected: setiap kartu sekarang ada angka besar "0" + label "KOMPETENSI" di atas chips. Kartu Y2 (3 chip + counter + footer) **paling padat** — cek tidak overflow vertikal.

Kalau Y2 overflow: kurangi gap kartu dari `clamp(10px, 1vw, 16px)` → `clamp(8px, 0.8vw, 12px)` di rule `.year-card`. Verify ulang.

- [ ] **Step 5: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): counter markup + styling per kartu — track-progresi 35s overhaul task 5/17

Counter angka besar + label "Kompetensi" di antara year-theme dan chips.
3 ID (cnt-y1, cnt-y2, cnt-y3) untuk JS tween di task berikut. Y3 angka
color red-dark. Static "0" sampai task counter JS aktivasi.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Counter rAF tween JS + milestone integration

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Hapus `<script>` lama**

Cari `<script>` block existing (sekitar line 390-406). Hapus seluruh block dari `<script>` sampai `</script>`.

- [ ] **Step 2: Tulis `<script>` baru dengan tweenCounter + milestones extended**

Tulis tepat sebelum `</body>`:

```html
<script>
  // ===== TRACK & PROGRESI ANIMATION ORCHESTRATION =====

  // Helpers
  function setDot(id, classes) {
    const el = document.getElementById(id);
    el.classList.remove('active', 'done', 'final');
    classes.split(' ').forEach(c => el.classList.add(c));
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

  // Milestones (timeline 35s)
  const milestones = [
    // Y1
    { t: 3000,  fn: () => setDot('d1', 'active') },
    { t: 4800,  fn: () => tweenCounter('cnt-y1', 0, 2, 1200) },
    { t: 9500,  fn: () => setDot('d1', 'done') },
    // Y2
    { t: 9700,  fn: () => setDot('d2', 'active') },
    { t: 12500, fn: () => tweenCounter('cnt-y2', 0, 3, 1400) },
    { t: 18000, fn: () => setDot('d2', 'done') },
    // Y3
    { t: 18200, fn: () => setDot('d3', 'active final') },
    { t: 20500, fn: () => tweenCounter('cnt-y3', 0, 2, 1200) }
  ];

  milestones.forEach(m => setTimeout(m.fn, m.t));
</script>
```

- [ ] **Step 3: Visual verify counter animation**

Refresh. Expected:
- t=4.8–6.0s: Y1 counter tick "0 → 2" smooth
- t=12.5–13.9s: Y2 counter tick "0 → 3"
- t=20.5–21.7s: Y3 counter tick "0 → 2"

Console: no errors. Final angka stop di exact (2, 3, 2). DevTools inspect `#cnt-y1` text content saat final = "2".

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): counter rAF tween + milestone orchestration — track-progresi 35s overhaul task 6/17

Replace simple dot setTimeout dengan milestone array unified. rAF tween
easeOutCubic 1.2-1.4s sync chip stamp window. Setiap counter
(Y1=2, Y2=3, Y3=2) tick smooth dari 0. Helpers setDot + tweenCounter
reusable.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Chip stamp redesign (rotate+scale+tick)

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Update `.chip` base CSS**

Cari rule `.chip { ... }` (sekitar line 208-218). Ganti SELURUH rule jadi:

```css
  .chip {
    background: var(--paper);
    border: 1px solid var(--hairline);
    border-radius: 6px;
    padding: 6px 10px;
    font-size: clamp(11px, 0.95vw, 14px);
    font-weight: 600;
    color: var(--text);
    opacity: 0;
    transform: rotate(-3deg) scale(0.85);
    transform-origin: left center;
    display: flex;
    align-items: center;
  }
  .year-card.y3 .chip { border-color: rgba(230,51,41,0.18); }

  .chip::before {
    content: "\2713"; /* ✓ */
    color: var(--navy-soft);
    font-weight: 800;
    margin-right: 8px;
    opacity: 0;
    animation: chipTick 0.3s ease-out 0.2s forwards;
  }
  .year-card.y3 .chip::before { color: var(--red); }
```

- [ ] **Step 2: Tambah keyframes `chipStamp` dan `chipTick`**

Cari block "ANIMATION KEYFRAMES" (sekitar line 268). Tambah:

```css
  @keyframes chipStamp {
    0%   { opacity: 0; transform: rotate(-3deg) scale(0.85); }
    60%  { opacity: 1; transform: rotate(1deg)  scale(1.04); }
    100% { opacity: 1; transform: rotate(0)     scale(1); }
  }
  @keyframes chipTick { to { opacity: 1; } }
```

Hapus keyframe `chipIn` lama (sudah di-replace):
```css
  @keyframes chipIn {
    to { opacity: 1; transform: translateY(0); }
  }
```

- [ ] **Step 3: Update animation delay rules pakai `chipStamp` + stagger 200ms**

Cari rules `#chips-y1 .chip:nth-child(N)` dst (sekitar line 284-298). Ganti SELURUH block animation delays untuk chips jadi:

```css
  /* Y1 chips — stagger 200ms, start t=4.5s */
  #chips-y1 .chip:nth-child(1) { animation: chipStamp 0.6s 4.5s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #chips-y1 .chip:nth-child(2) { animation: chipStamp 0.6s 4.7s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }

  /* Y2 chips — stagger 200ms, start t=12.5s */
  #chips-y2 .chip:nth-child(1) { animation: chipStamp 0.6s 12.5s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #chips-y2 .chip:nth-child(2) { animation: chipStamp 0.6s 12.7s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #chips-y2 .chip:nth-child(3) { animation: chipStamp 0.6s 12.9s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }

  /* Y3 chips — stagger 200ms, start t=20.5s */
  #chips-y3 .chip:nth-child(1) { animation: chipStamp 0.6s 20.5s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #chips-y3 .chip:nth-child(2) { animation: chipStamp 0.6s 20.7s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
```

- [ ] **Step 4: Visual verify chip stamp**

Refresh. Watch chips:
- Stamp rotate -3° → +1° overshoot → 0°
- Scale 0.85 → 1.04 → 1.0
- ✓ tick mark mini muncul fade-in 200ms setelah chip onset
- Stagger 200ms antar chip dalam satu kartu
- Y3 chips ✓ warna merah, lainnya navy-soft

- [ ] **Step 5: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): chip stamp redesign rotate+scale+✓tick — track-progresi 35s overhaul task 7/17

Replace fade+slide chipIn dengan stamp keyframe (rotate -3°→+1°→0° overshoot,
scale 0.85→1.04→1.0). ✓ tick mini per chip via ::before pseudo, 200ms delay
after stamp. Stagger 200ms antar chip. Y3 ✓ red, lain navy-soft. Animation
delays disesuaikan timeline 35s (Y1=4.5s, Y2=12.5s, Y3=20.5s).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: Card active glow + dim state machine + JS toggle

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Update card animation timing untuk 35s timeline**

Cari rules `#card-y1`, `#card-y2`, `#card-y3` (sekitar line 283-296). Ganti delay:

```css
  #card-y1 { animation: cardIn 0.7s 2.5s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #card-y2 { animation: cardIn 0.7s 11.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
  #card-y3 { animation: cardIn 0.7s 19.0s cubic-bezier(0.34, 1.56, 0.64, 1) forwards; }
```

- [ ] **Step 2: Tambah CSS `.active` + `.dim` states**

Sisipkan di `<style>` setelah block `.year-card.y3 { border-top-color: var(--red); }`:

```css
  /* Active glow + dim state machine */
  .year-card {
    transition: box-shadow 0.6s ease, opacity 0.6s ease;
  }
  .year-card.active {
    box-shadow:
      0 1px 2px rgba(10,36,71,0.03),
      0 8px 24px rgba(10,36,71,0.08),
      0 0 0 4px rgba(15,45,92,0.12);
  }
  .year-card.y2.active {
    box-shadow:
      0 1px 2px rgba(10,36,71,0.03),
      0 8px 24px rgba(10,36,71,0.08),
      0 0 0 4px rgba(30,66,128,0.12);
  }
  .year-card.y3.active {
    box-shadow:
      0 1px 2px rgba(10,36,71,0.03),
      0 8px 24px rgba(230,51,41,0.10),
      0 0 0 4px rgba(230,51,41,0.15);
  }
  .year-card.dim { opacity: 0.65; }
```

- [ ] **Step 3: Extend milestone array dengan activate/dim**

Edit `<script>` (dari Task 6). Tambah helpers + extend milestones:

```javascript
  function activate(id) {
    const el = document.getElementById(id);
    el.classList.add('active');
    el.classList.remove('dim');
  }
  function dim(id) {
    const el = document.getElementById(id);
    el.classList.remove('active');
    el.classList.add('dim');
  }
```

Ganti milestones array jadi:

```javascript
  const milestones = [
    // Y1 act
    { t: 3000,  fn: () => { activate('card-y1'); setDot('d1', 'active'); } },
    { t: 4800,  fn: () => tweenCounter('cnt-y1', 0, 2, 1200) },
    { t: 9500,  fn: () => { dim('card-y1'); setDot('d1', 'done'); } },
    // Y2 act
    { t: 11000, fn: () => { activate('card-y2'); setDot('d2', 'active'); } },
    { t: 12500, fn: () => tweenCounter('cnt-y2', 0, 3, 1400) },
    { t: 18000, fn: () => { dim('card-y2'); setDot('d2', 'done'); } },
    // Y3 act
    { t: 19000, fn: () => { activate('card-y3'); setDot('d3', 'active final'); } },
    { t: 20500, fn: () => tweenCounter('cnt-y3', 0, 2, 1200) }
  ];
```

- [ ] **Step 4: Visual verify state machine**

Refresh. Watch:
- t=3.0s: Y1 card pop + glow ring navy outer-shadow muncul
- t=9.5s: Y1 fade ke dim (opacity 0.65), no more glow
- t=11.0s: Y2 pop + glow ring navy-soft
- t=18.0s: Y2 fade dim
- t=19.0s: Y3 pop + glow ring red
- t=28+s: Y3 tetap glow red, Y1/Y2 dim

DevTools inspect: `<div class="year-card y1 dim">` setelah t=9.5s, `<div class="year-card y3 active">` setelah t=19s.

- [ ] **Step 5: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): card active glow + dim state machine — track-progresi 35s overhaul task 8/17

CSS box-shadow ring outer 4px sebagai active glow (navy/navy-soft/red).
.dim opacity 0.65 saat next card aktif. JS helpers activate/dim,
milestone array extended dengan state transitions. Card animation delays
disesuaikan 35s timeline (Y1=2.5s, Y2=11s, Y3=19s).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: Role row pulse trigger (CSS + JS)

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah ID `role-row` di markup**

Cari `<div class="role-row">` (sekitar line 315). Ganti jadi:

```html
    <div class="role-row" id="role-row">
```

- [ ] **Step 2: Tambah CSS keyframe + rule pulse**

Sisipkan di `<style>` setelah block `@keyframes fadeUp`:

```css
  @keyframes rolePulse {
    0%, 100% { transform: scale(1); border-color: var(--navy); }
    50%      { transform: scale(1.06); border-color: var(--red); }
  }
  .role-row.pulse .role .icon { animation: rolePulse 1s ease-in-out; }
```

- [ ] **Step 3: Extend `<script>` dengan `pulseRole` + integrate ke milestones**

Edit `<script>`. Tambah helper:

```javascript
  function pulseRole() {
    const r = document.getElementById('role-row');
    r.classList.add('pulse');
    setTimeout(() => r.classList.remove('pulse'), 1000);
  }
```

Update 3 milestone yang activate card (Y1/Y2/Y3 onset) untuk juga panggil `pulseRole()`:

```javascript
    { t: 3000,  fn: () => { activate('card-y1'); pulseRole(); setDot('d1', 'active'); } },
    ...
    { t: 11000, fn: () => { activate('card-y2'); pulseRole(); setDot('d2', 'active'); } },
    ...
    { t: 19000, fn: () => { activate('card-y3'); pulseRole(); setDot('d3', 'active final'); } },
```

- [ ] **Step 4: Visual verify pulse 3×**

Refresh. Watch role row icons (Panelman + Operator):
- t=3.0s: Panelman + Operator box scale 1.0→1.06→1.0 (1 detik), border-color sempat merah peak
- t=11.0s: Pulse #2
- t=19.0s: Pulse #3

3× pulse total, sync onset tiap kartu.

- [ ] **Step 5: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): role icon pulse sync card onset — track-progresi 35s overhaul task 9/17

CSS rolePulse 1s ease-in-out (scale 1.0→1.06→1.0, border navy→red→navy).
JS helper pulseRole() integrate ke 3 card activate milestones (t=3s/11s/19s).
Role row ID-tagged.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: Title em "Satu Tujuan" underline draw red

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Replace `.title em` rule + add keyframe**

Cari `.title em { font-style: normal; color: var(--red); }` (sekitar line 80). Ganti jadi:

```css
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
  @keyframes underlineDraw {
    to { background-size: 100% 6px; }
  }
```

- [ ] **Step 2: Visual verify underline**

Refresh. Watch title em "Satu Tujuan":
- t=1.2s: garis bawah merah 6px tebal draw kiri → kanan (0.6s duration)
- Final state: full underline merah di bawah "Satu Tujuan"

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): title em "Satu Tujuan" underline draw red — track-progresi 35s overhaul task 10/17

Background-gradient red strip 6px, background-size 0% → 100% kiri→kanan.
Animation delay 1.2s (sync setelah title fade-down complete). Subtle editorial
emphasis pada climax phrase.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: Y3 ✓ checkmark drop-bounce

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Update `.year-card.y3 .footer-line .check` CSS**

Cari `.footer-line .check { font-size: 1.2em; }` (sekitar line 236). Ganti jadi:

```css
  .footer-line .check { font-size: 1.2em; }
  .year-card.y3 .footer-line .check {
    display: inline-block;
    opacity: 0;
    transform: translateY(-16px) scale(0.5);
    animation: checkDrop 0.8s 24.5s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
  }
  @keyframes checkDrop {
    60%  { opacity: 1; transform: translateY(4px) scale(1.2); }
    100% { opacity: 1; transform: translateY(0) scale(1); }
  }
```

- [ ] **Step 2: Update `#foot-y3` animation delay**

Cari `#foot-y3` rule (sekitar line 299). Ganti jadi:

```css
  #foot-y3 { animation: footerIn 0.5s 23.5s ease-out forwards; }
```

(Footer-line text "Kompetensi Tervalidasi" muncul t=23.5s, lalu ✓ drop t=24.5s — sekuensial: text appears first, lalu ✓ jatuh bounce.)

- [ ] **Step 3: Update `#foot-y1` dan `#foot-y2` ke 35s timeline**

Cari dan ganti:
```css
  #foot-y1 { animation: footerIn 0.5s 8.5s ease-out forwards; }
  #foot-y2 { animation: footerIn 0.5s 17.0s ease-out forwards; }
```

- [ ] **Step 4: Visual verify ✓ drop-bounce**

Refresh. Watch Y3 footer:
- t=23.5s: text "Kompetensi Tervalidasi" fade-in
- t=24.5s: ✓ checkmark drop dari atas, bounce (overshoot scale 1.2), settle scale 1.0
- ✓ total drop duration 0.8s

Tidak ada visual jump — bounce smooth.

- [ ] **Step 5: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): Y3 ✓ checkmark drop-bounce + footer timing — track-progresi 35s overhaul task 11/17

✓ drop 0.8s spring (translateY -16→4→0, scale 0.5→1.2→1.0) at t=24.5s,
setelah footer-line text appear t=23.5s. Y1/Y2 footer-line delays
adjusted to 35s timeline (8.5s, 17.0s).

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 12: Y3 climax — burst ring + glow pulse 2×

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Update `.year-card.y3 { position: relative }` (kalau belum)**

Cari `.year-card { ... }` rule (sekitar line 161-173). Confirm `position` ada. Kalau tidak, tambah `position: relative` ke `.year-card`:

```css
  .year-card {
    position: relative; /* anchor untuk burst-ring absolute */
    background: #FFFFFF;
    ...
  }
```

- [ ] **Step 2: Tambah CSS `.burst-ring` + keyframe `burstRing` + `glowPulseRed`**

Sisipkan di `<style>` setelah rule `.year-card.y3.active { ... }` (dari Task 8):

```css
  /* Y3 climax burst ring */
  .burst-ring {
    position: absolute;
    inset: -8px;
    border: 3px solid var(--red);
    border-radius: 14px;
    opacity: 0;
    pointer-events: none;
    animation: burstRing 1.2s 25.5s cubic-bezier(0.2, 0.8, 0.2, 1) forwards;
  }
  @keyframes burstRing {
    0%   { opacity: 0.9; transform: scale(0.92); }
    100% { opacity: 0;   transform: scale(1.12); }
  }

  /* Y3 glow pulse 2× */
  .year-card.y3.active {
    animation: glowPulseRed 2.5s 25.5s ease-out 2;
  }
  @keyframes glowPulseRed {
    0%, 100% {
      box-shadow:
        0 1px 2px rgba(10,36,71,0.03),
        0 8px 24px rgba(230,51,41,0.10),
        0 0 0 4px rgba(230,51,41,0.15);
    }
    50% {
      box-shadow:
        0 1px 2px rgba(10,36,71,0.03),
        0 12px 36px rgba(230,51,41,0.18),
        0 0 0 12px rgba(230,51,41,0.05);
    }
  }
```

- [ ] **Step 3: Tambah `.burst-ring` markup di Y3 card**

Cari `<div class="year-card y3" id="card-y3">`. Sisipkan tepat setelah opening tag:

```html
      <div class="year-card y3" id="card-y3">
        <span class="burst-ring" aria-hidden="true"></span>
        <span class="year-badge">Tahun 3</span>
        ...
      </div>
```

- [ ] **Step 4: Visual verify climax**

Refresh. Watch t=25.5–28.0s pada Y3 card:
- t=25.5s: burst ring rounded-rect (border 3px red, radius 14px) expand dari scale 0.92 → 1.12, opacity 0.9 → 0 (fade out)
- Bersamaan t=25.5s: card glow box-shadow pulse — peak t=26.75s (radius +8px, opacity max), kembali normal t=28s
- Pulse berulang 2× total (durasi 5s combined)

Climax visible & punchy, no norak.

- [ ] **Step 5: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): Y3 climax burst ring + glow pulse 2× — track-progresi 35s overhaul task 12/17

Burst ring rounded-rect (radius 14px) expand 0.92→1.12, fade-out. Card glow
pulse box-shadow ring expand 4→12px, opacity peak 50% at t=26.75s.
Iteration count 2 (5s total). Onset t=25.5s sync setelah ✓ drop-bounce.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 13: Final hold ambient — dot d3 breathe loop

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

- [ ] **Step 1: Tambah keyframe + rule `.dot.final.breathing`**

Sisipkan di `<style>` setelah keyframe `glowPulseRed`:

```css
  @keyframes dotBreathe {
    0%, 100% { transform: scale(1); }
    50%      { transform: scale(1.15); }
  }
  .dot.final.breathing { animation: dotBreathe 2s ease-in-out infinite; }
```

- [ ] **Step 2: Extend milestones — add breathing trigger di t=28000ms**

Edit `<script>` milestones array. Tambah entry di akhir:

```javascript
    // Y3 act
    { t: 19000, fn: () => { activate('card-y3'); pulseRole(); setDot('d3', 'active final'); } },
    { t: 20500, fn: () => tweenCounter('cnt-y3', 0, 2, 1200) },
    // Final ambient
    { t: 28000, fn: () => document.getElementById('d3').classList.add('breathing') }
```

- [ ] **Step 3: Visual verify dot breath**

Refresh, fast-forward (atau wait) ke t=28+s. Watch dot ke-3 (d3, merah):
- t=28.0s onwards: scale loop 1.0 ↔ 1.15 every 2s, smooth ease-in-out
- Infinite loop, sampai page refresh

Final hold 7s ambient feel — tidak total static.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
feat(proton-video): final hold dot d3 breath ambient — track-progresi 35s overhaul task 13/17

CSS keyframe dotBreathe 2s ease-in-out infinite (scale 1.0↔1.15). JS toggle
.breathing class di t=28000ms (start hold window). Subtle life sign selama
7s final hold, prevent total static.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 14: `prefers-reduced-motion` media query + JS branch

**Files:**
- Modify: `docs/assets/proton-video/track-progresi.html`

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
    .year-card { opacity: 1; transform: none; }
    .year-card.dim { opacity: 1; }
    .chip { opacity: 1; transform: none; }
    .chip::before { opacity: 1; }
    .backbone-path { stroke-dashoffset: 0; opacity: 1; stroke: var(--red); }
    .title em { background-size: 100% 6px; }
    .dot.final { transform: none; }
    .burst-ring { display: none; }
    .backbone-caption { display: none; }
  }
```

- [ ] **Step 2: Update `<script>` dengan reduced-motion branch**

Edit `<script>`. Wrap `milestones.forEach(...)` dengan branch:

```javascript
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  if (reduced) {
    // Skip animation, set final state immediately
    document.getElementById('cnt-y1').textContent = '2';
    document.getElementById('cnt-y2').textContent = '3';
    document.getElementById('cnt-y3').textContent = '2';
    ['card-y1', 'card-y2', 'card-y3'].forEach(id => {
      document.getElementById(id).classList.add('active');
    });
    setDot('d1', 'done');
    setDot('d2', 'done');
    setDot('d3', 'active final');
  } else {
    milestones.forEach(m => setTimeout(m.fn, m.t));
  }
```

- [ ] **Step 3: Visual verify reduced motion**

Buka DevTools → ⋮ → More tools → Rendering. Cari "Emulate CSS media feature prefers-reduced-motion" → set to "reduce".

Refresh. Expected:
- No animation visible
- Layout langsung final state: 3 kartu visible active (all glow), chips all visible with ✓ tick, counter "2", "3", "2", title em underline already drawn red, dot 3 red final
- Burst ring hidden, backbone caption hidden
- No motion, no transition

Toggle balik ke "No preference", refresh — animasi full kembali jalan.

- [ ] **Step 4: Commit**

```bash
git add docs/assets/proton-video/track-progresi.html
git commit -m "$(cat <<'EOF'
a11y(proton-video): prefers-reduced-motion fallback — track-progresi 35s overhaul task 14/17

CSS media query reduce animation/transition ke 0.01ms, force final state
visible. JS matchMedia branch: skip setTimeout chain, langsung set
counter values + active classes + dot states. Burst-ring & captions
display:none. Playwright record default no-preference = full motion play.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 15: Record script duration 26→36s

**Files:**
- Modify: `docs/assets/proton-video/record-track-progresi.mjs`

- [ ] **Step 1: Update `RECORD_DURATION_MS`**

Read `docs/assets/proton-video/record-track-progresi.mjs`. Cari line:

```javascript
const RECORD_DURATION_MS = 26_000; // 25s animation + 1s buffer
```

Ganti jadi:

```javascript
const RECORD_DURATION_MS = 36_000; // 35s animation + 1s buffer
```

- [ ] **Step 2: Verify file content correct**

Run:
```bash
grep "RECORD_DURATION_MS" docs/assets/proton-video/record-track-progresi.mjs
```

Expected: `const RECORD_DURATION_MS = 36_000; // 35s animation + 1s buffer`

- [ ] **Step 3: Commit**

```bash
git add docs/assets/proton-video/record-track-progresi.mjs
git commit -m "$(cat <<'EOF'
chore(proton-video): record duration 26s → 36s — track-progresi 35s overhaul task 15/17

Single-line tweak. 35s animation timeline + 1s buffer = 36s record window.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 16: Run record + verify .webm output

**Files:**
- Generate: `docs/assets/proton-video/track-progresi.webm`

- [ ] **Step 1: Stop existing http.server (kalau ada di port 8800)**

Record script start sendiri http.server di port 8767. Pastikan port itu free.

Kalau Task 0 http.server di port 8800 masih jalan, biarkan (port beda).

- [ ] **Step 2: Run record script**

Run:
```bash
node "docs/assets/proton-video/record-track-progresi.mjs"
```

Expected log:
```
[record] Server ready, launching browser...
[record] Recording 36s animation...
[record] Video saved (raw): <temp .webm path>
[record] Final path: .../docs/assets/proton-video/track-progresi.webm
```

Durasi command total ~45-50 detik (start server + browser + recording 36s + cleanup).

- [ ] **Step 3: Verify file size + durasi**

Run:
```bash
ls -la docs/assets/proton-video/track-progresi.webm
```

Expected: file ada, size 2–6 MB range. Kalau <500 KB → kemungkinan record gagal/blank.

Buka file di Chrome (drag-drop ke tab). Verify:
- Durasi 35–36 detik
- Resolusi 1920×1080
- Visual timeline correct: header → role row → backbone draw → kartu Y1 pop → counter tick → chip stamp → "Naik Tahun" caption → Y2 sequence → caption → Y3 climax (burst + glow pulse) → final hold dot breath
- No glitch / frame hilang / black screen

Kalau ada glitch, debug `track-progresi.html` (cek console errors saat record via headless mode), re-record.

- [ ] **Step 4: Commit hasil record**

```bash
git add docs/assets/proton-video/track-progresi.webm
git commit -m "$(cat <<'EOF'
chore(proton-video): regenerate track-progresi.webm 35s — track-progresi 35s overhaul task 16/17

35s editorial-dataviz hybrid recording, 1920×1080 chromium headless.
Replace 2.1MB 25s version. Action density 73%, climax di t=25-28s,
final hold dot breath 28-35s.

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

## Task 17: Spec compliance final check + DOD verification

**Files:** none (verification only)

- [ ] **Step 1: Manual SC verification per Section 11 spec**

Buka spec `docs/superpowers/specs/2026-05-21-track-progresi-animation-overhaul-design.md` Section 11. Verify 20 SC satu per satu:

| SC | Verify command/visual | Pass? |
|----|----------------------|-------|
| SC1 | Open `.webm`, stopwatch durasi total = 35–36s | ☐ |
| SC2 | Manual review window kosong = ≥73% active | ☐ |
| SC3 | Screenshot t=4.5/11/19/25s — backbone 33/66/100% red | ☐ |
| SC4 | DevTools inspect final state cnt-y1=2, cnt-y2=3, cnt-y3=2 | ☐ |
| SC5 | Frame-by-frame chip stamp rotate+scale+✓ tick visible | ☐ |
| SC6 | Visual 3× role pulse di t=3s/11s/19s | ☐ |
| SC7 | Visual Y3 burst ring + glow pulse 2× = climax punchy | ☐ |
| SC8 | DevTools class inspect: card-y1 dim → card-y2 active dst | ☐ |
| SC9 | Visual t=28-35s dot d3 breath loop visible | ☐ |
| SC10 | Visual grid backdrop visible tapi tidak distract | ☐ |
| SC11 | Manual size check Y2 kartu tidak overflow 1920×1080 | ☐ |
| SC12 | Visual palette navy + red + Inter retain | ☐ |
| SC13 | `.webm` 35-36s, 2-6 MB, no glitch | ☐ |
| SC14 | `wc -c docs/assets/proton-video/track-progresi.html` < 30000 | ☐ |
| SC15 | DevTools `prefers-reduced-motion: reduce` → static layout, no anim | ☐ |
| SC16 | Visual title em "Satu Tujuan" underline red draw kiri→kanan | ☐ |
| SC17 | Visual Y3 ✓ drop-bounce visible & cute | ☐ |
| SC18 | Frame compare backbone Y3 segment morph navy-soft → red | ☐ |
| SC19 | Visual caption "Naik Tahun" 2× di t=9.4s & 17.9s | ☐ |
| SC20 | `git diff docs/Naskah Video PROTON.docx` → empty (no change) | ☐ |

Mark setiap SC pass kalau verified.

- [ ] **Step 2: Run file size check**

```bash
wc -c docs/assets/proton-video/track-progresi.html
```

Expected: <30000 bytes (target ~22 KB).

- [ ] **Step 3: Verify git status bersih**

```bash
git status docs/assets/proton-video/ docs/superpowers/
```

Expected:
- `docs/assets/proton-video/track-progresi.html` — clean (last task committed)
- `docs/assets/proton-video/record-track-progresi.mjs` — clean
- `docs/assets/proton-video/track-progresi.webm` — clean
- File untracked lama (`track-progresi-v-G.html`, dll) — tetap untracked (abaikan)
- Spec + plan file — clean

- [ ] **Step 4: Verify naskah docx not changed**

```bash
git status "docs/Naskah Video PROTON.docx"
```

Expected: kalau ada baseline modification dari sebelum plan ini, tidak ada change baru dari task 1-16.

Spec section 13 DOD: naskah docx **tidak** di-touch. Confirm.

- [ ] **Step 5: Final commit (kalau ada deviasi spec update)**

Kalau saat execute ada deviasi dari spec (misalnya gap kartu adjust, caption position adjust), update spec file dan commit:

```bash
git add docs/superpowers/specs/2026-05-21-track-progresi-animation-overhaul-design.md
git commit -m "$(cat <<'EOF'
docs(proton-video): update spec — capture execution deviations

[describe deviations]

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

Kalau no deviasi, skip step ini.

---

## Definition of Done

- [ ] All 17 task completed dan commit
- [ ] All 20 SC pass (Task 17 Step 1 table)
- [ ] `track-progresi.html` file size <30 KB
- [ ] `track-progresi.webm` di-update di repo (3–5 MB range)
- [ ] `record-track-progresi.mjs` durasi 36000ms committed
- [ ] Naskah video docx **tidak** di-touch (`git diff` empty)
- [ ] `git status` bersih kecuali file untracked lama
- [ ] Spec file di-update kalau ada deviasi (atau tetap kalau none)
- [ ] DevTools `prefers-reduced-motion: reduce` simulate → static layout langsung visible

---

## Rollback (kalau gagal)

Single file `track-progresi.html` + `record-track-progresi.mjs` + `track-progresi.webm`. Kalau gagal di tengah:

```bash
# Revert ke versi 25s (commit terakhir sebelum task 1)
git log --oneline docs/assets/proton-video/track-progresi.html
git revert <task1-commit>..<lastcommit>
```

Atau reset hard kalau belum push:

```bash
git reset --hard <commit-before-task-1>
```

⚠️ `git reset --hard` destructive. Konfirmasi dulu kalau perlu.

---

## Notes for Executor

- Task 0 setup harus jalan dulu (Playwright + http.server).
- Task 1–14 semua edit `track-progresi.html` single file → no parallelization. Sequential commits.
- Task 15 single-line edit `record-track-progresi.mjs` (independent), bisa kapan saja tapi sebelum Task 16.
- Task 16 record = command 45-50s wall clock. Jalankan setelah Task 14 complete dan visual verify pass.
- Task 17 verification — tidak ada code change, manual checklist.
- Setiap commit subject ≤80 char, body explain "why" + task number.
- Caveman commit style untuk subject (terse), body normal.
