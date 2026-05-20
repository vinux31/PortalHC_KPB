# Track & Progresi Animation Overhaul — Design Spec

**Status:** Draft — awaiting user review
**Author:** Brainstorm session 2026-05-21
**Target file:** `docs/assets/proton-video/track-progresi.html`
**Supersedes runtime of:** `docs/superpowers/plans/2026-05-20-proton-track-progresi-animation.md` (initial 25s build)

---

## 1. Goal

Overhaul animasi `track-progresi.html` (PROTON video bagian 3) dari versi 25s pacing longgar menjadi **35s editorial-dataviz hybrid** dengan motion continuity, climax punchy, dan retainable corporate tone untuk audience pekerja kilang Pertamina.

**Quantified targets:**
- Action density naik 46% → ≥70%.
- Dead zone awal (0–5s) hilang; static hold akhir (16.5–25s, 8.5s) menyusut jadi (28–35s, 7s ambient breath).
- File size HTML <30 KB (sekarang 13 KB, prediksi ~22 KB).

---

## 2. Decision Log (from brainstorm)

| # | Decision | Choice |
|---|----------|--------|
| D1 | Improvement scope | E — overhaul total |
| D2 | Durasi target | C — 35s cinematic |
| D3 | Tone & visual mood | Hybrid: D editorial-dataviz (primer) + B energetic spring (accent) |
| D4 | Role row treatment | B — pulse sync per kartu aktif |
| D5 | Climax (Y3 ✓) | C — burst ring + glow pulse 2× (no confetti, no editorial closing line) |
| D6 | Background ambient | C — grid static editorial (no drift, no vignette) |
| D7 | Arrow connector existing | A — hapus `.arrow` element, label "Naik Tahun" jadi caption tooltip di atas backbone segmen |
| D8 | Accessibility | Add `prefers-reduced-motion` fallback |

---

## 3. Architecture

### 3.1 File Scope

| File | Change |
|------|--------|
| `docs/assets/proton-video/track-progresi.html` | EDIT IN PLACE — overhaul CSS + markup + JS |
| `docs/assets/proton-video/record-track-progresi.mjs` | EDIT — `RECORD_DURATION_MS: 26_000` → `36_000` |
| `docs/assets/proton-video/track-progresi.webm` | REGENERATE — 35s, ~3–5 MB |

Naskah video `docs/Naskah Video PROTON.docx` **NOT changed** (durasi 25→35s narasi adjustment = separate concern).

### 3.2 Tech Approach

- **CSS-only** untuk: card pop, chip stamp + tick, role pulse, footer stamp, burst ring, glow pulse, dot breath, grid backdrop, title em underline draw, ✓ drop-bounce.
- **SVG overlay** untuk timeline backbone — `<svg viewBox="0 0 1600 4">` di slot dedicated (24–32px) antara role-row dan timeline. `<path>` stroke-dasharray + stroke-dashoffset 3-segment animate via CSS keyframe.
- **JS minimal** (~35 LOC inline) untuk: state machine activate/dim per kartu, role pulse trigger 3×, counter tick rAF tween, progress dots update.

### 3.3 Why CSS+SVG (not Canvas / Lottie)

- Standalone HTML, no build, no asset depend — match pattern `smart-animation.html`.
- Playwright record straight, no codec issue.
- Maintainable single-file.

---

## 4. Timeline 35s Breakdown

| Beat | Window | Event |
|------|--------|-------|
| **Intro** | 0.0–2.5s | Stage + grid backdrop fade-in (0–0.4s). Eyebrow "Track & Progresi" + Title fade-down (0.2–1.0s). Em "Satu Tujuan" underline draw red kiri→kanan (1.2–1.8s). Role row fade-up (0.9–1.8s). Progress dots show inactive (1.2s). SVG backbone path appear empty hairline navy (2.0–2.5s). |
| **Act Y1 "Foundation"** | 2.5–11.0s | Card Y1 pop spring (2.5–3.2s). Active glow ring Y1 (navy). Role icons pulse 1.0→1.06→1.0 (2.5–3.5s). Backbone draw 0%→33% (3.0–4.5s). Chip stamp ×2 rotate-3°+scale, ✓ tick mini stagger 200ms (4.5–5.8s). Counter "0 → 2 Kompetensi" rAF tick (4.8–6.0s). Dot d1 active navy (3.0s). Footer "Lulus →" stamp slide-in (8.5–9.2s). Card Y1 glow fade ke dim (opacity 0.7) saat Y2 onset. |
| **Caption Y1→Y2** | 9.2–11.0s | Backbone draw 33%→66%. Tooltip caption "Naik Tahun" fade in/out di atas segmen (1s lifespan). Dot d1 → done (navy-soft), d2 → active. |
| **Act Y2 "Pendalaman"** | 11.0–19.0s | Card Y2 pop spring (11.0–11.7s). Glow Y2 (navy-soft). Role pulse #2. Chip stamp ×3 stagger (12.5–14.5s). Counter "0 → 3" tick. Footer "Lulus →" stamp (17.0–17.7s). Y2 glow dim saat Y3 onset. |
| **Caption Y2→Y3** | 17.7–19.0s | Backbone draw 66%→100%, **stroke morph navy-soft → red** di segmen final. Tooltip caption "Naik Tahun" #2. Dot d2 done, d3 active+final (red). |
| **Act Y3 "Mastery" (CLIMAX)** | 19.0–28.0s | Card Y3 pop spring (19.0–19.8s) — red accent border. Glow Y3 (red). Role pulse #3 intensitas naik. Chip stamp ×2 stagger (20.5–22.0s). Counter "0 → 2" tick. ✓ checkmark drop-bounce (24.5–25.3s). **Burst ring radial** scale 0→1.8 fade-out (25.5–26.7s). **Card glow pulse 2×** box-shadow red expand-contract (25.5–28.0s). |
| **Hold + Ambient** | 28.0–35.0s | Semua state final visible. Dot d3 red **breathe pulse** scale 1.0↔1.15 loop 2s. Subtle. Other elements diam — poster frame stable. |

**Action density:** 25.5s active motion / 35s total = **73%** (target ≥70% ✓).

---

## 5. Motion Catalog

### 5.1 SVG Timeline Backbone (NEW — D-mode central device)

Slot dedicated `<div class="backbone-slot">` (24–32px height) di antara role-row dan timeline. SVG inside, full width.

```css
.backbone-slot { height: clamp(24px, 2.4vw, 32px); margin-bottom: clamp(12px, 1.2vw, 20px); position: relative; }
.backbone { width: 100%; height: 100%; display: block; }
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
@keyframes backboneDraw1 { to { stroke-dashoffset: 960; opacity: 1; } }  /* 33% */
@keyframes backboneDraw2 { to { stroke-dashoffset: 480; } }              /* 66% */
@keyframes backboneDraw3 { to { stroke-dashoffset: 0; stroke: var(--red); } } /* 100% + morph red */
```

Tooltip captions "Naik Tahun" — 2 buah, absolute positioned di atas backbone (kiri & kanan center), fade in 0.4s + hold 0.6s + fade out 0.4s.

### 5.2 Chip Stamp + Tick (D-mode)

Ganti fade+slide existing dengan rotate+scale stamp + ✓ tick mini per chip.

```css
.chip {
  opacity: 0;
  transform: rotate(-3deg) scale(0.85);
  transform-origin: left center;
  display: flex;
  align-items: center;
}
@keyframes chipStamp {
  0%   { opacity: 0; transform: rotate(-3deg) scale(0.85); }
  60%  { opacity: 1; transform: rotate(1deg) scale(1.04); }
  100% { opacity: 1; transform: rotate(0) scale(1); }
}
.chip::before {
  content: "✓";
  color: var(--navy-soft);
  font-weight: 800;
  margin-right: 8px;
  opacity: 0;
  animation: chipTick 0.3s ease-out 0.2s forwards;
}
.year-card.y3 .chip::before { color: var(--red); }
@keyframes chipTick { to { opacity: 1; } }
```

Stagger 200ms antar chip dalam satu kartu.

### 5.3 Counter Tick "0 → N" (D-mode dataviz)

Markup baru di tiap kartu antara `.year-theme` dan `.chips`:

```html
<div class="counter">
  <span class="num" id="cnt-y1" data-target="2">0</span>
  <span class="unit">Kompetensi</span>
</div>
```

Styling:

```css
.counter { display: flex; align-items: baseline; gap: 6px; margin: 4px 0 2px; }
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

JS rAF tween 1.2–1.4s easeOutCubic, sync dengan chip stamp window.

### 5.4 Card Pop + Active Glow + Dim

Retain spring `cubic-bezier(0.34, 1.56, 0.64, 1)`. Tambah outer glow ring saat aktif + dim state saat next card aktif.

```css
.year-card.active {
  box-shadow:
    0 1px 2px rgba(10,36,71,0.03),
    0 8px 24px rgba(10,36,71,0.08),
    0 0 0 4px rgba(15,45,92,0.12);
  transition: box-shadow 0.6s ease, opacity 0.6s ease;
}
.year-card.y2.active { box-shadow: 0 0 0 4px rgba(30,66,128,0.12), 0 8px 24px rgba(10,36,71,0.08); }
.year-card.y3.active { animation: glowPulseRed 2.5s 25.5s ease-out 2; }
.year-card.dim { opacity: 0.65; }
@keyframes glowPulseRed {
  0%, 100% { box-shadow: 0 0 0 4px rgba(230,51,41,0.15), 0 8px 24px rgba(230,51,41,0.10); }
  50%      { box-shadow: 0 0 0 12px rgba(230,51,41,0.05), 0 12px 36px rgba(230,51,41,0.18); }
}
```

Toggle `.active` / `.dim` via JS milestone (Section 6).

### 5.5 Role Icon Pulse (B-mode energy)

```css
@keyframes rolePulse {
  0%, 100% { transform: scale(1); border-color: var(--navy); }
  50%      { transform: scale(1.06); border-color: var(--red); }
}
.role-row.pulse .role .icon { animation: rolePulse 1s ease-in-out; }
```

JS toggle `.pulse` class on `.role-row` for 1s at each card onset (3 trigger total).

### 5.6 Y3 Burst Ring + Glow Pulse (CLIMAX)

Burst container absolute positioned di `.year-card.y3` level (bukan footer-line, agar radius cukup besar):

```html
<div class="year-card y3" id="card-y3">
  <span class="burst-ring" aria-hidden="true"></span>
  ...existing content...
</div>
```

```css
.burst-ring {
  position: absolute;
  inset: -8px;
  border: 3px solid var(--red);
  border-radius: 12px;
  opacity: 0;
  pointer-events: none;
  animation: burstRing 1.2s 25.5s cubic-bezier(0.2, 0.8, 0.2, 1) forwards;
}
@keyframes burstRing {
  0%   { opacity: 0.9; transform: scale(0.92); }
  100% { opacity: 0;   transform: scale(1.12); }
}
```

Burst = expanding rounded-rect ring sesuai shape kartu (lebih clean drpd circle ring yang clip card corners).

### 5.7 Y3 ✓ Checkmark Drop-Bounce

```css
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

### 5.8 Final Dot Breath (28–35s ambient)

```css
@keyframes dotBreathe {
  0%, 100% { transform: scale(1); }
  50%      { transform: scale(1.15); }
}
.dot.final.breathing { animation: dotBreathe 2s 28.0s ease-in-out infinite; }
```

JS adds `.breathing` to `#d3` at t=28000ms.

### 5.9 Title Em "Satu Tujuan" Underline Draw

```css
.title em {
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

### 5.10 Grid Backdrop

```css
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
}
```

---

## 6. JS Orchestration

Single milestone array, ~35 LOC total.

```javascript
const milestones = [
  // Y1 act
  { t: 2500,  fn: () => { activate('card-y1'); pulseRole(); setDot('d1', 'active'); } },
  { t: 4800,  fn: () => tweenCounter('cnt-y1', 0, 2, 1200) },
  { t: 9200,  fn: () => { dim('card-y1'); setDot('d1', 'done'); } },

  // Y2 act
  { t: 11000, fn: () => { activate('card-y2'); pulseRole(); setDot('d2', 'active'); } },
  { t: 12500, fn: () => tweenCounter('cnt-y2', 0, 3, 1400) },
  { t: 17700, fn: () => { dim('card-y2'); setDot('d2', 'done'); } },

  // Y3 climax
  { t: 19000, fn: () => { activate('card-y3'); pulseRole(); setDot('d3', 'active final'); } },
  { t: 20500, fn: () => tweenCounter('cnt-y3', 0, 2, 1200) },
  { t: 28000, fn: () => document.getElementById('d3').classList.add('breathing') }
];

const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

if (reduced) {
  // skip all motion; set final state immediately
  document.getElementById('cnt-y1').textContent = '2';
  document.getElementById('cnt-y2').textContent = '3';
  document.getElementById('cnt-y3').textContent = '2';
  ['card-y1','card-y2','card-y3'].forEach(id => document.getElementById(id).classList.add('active'));
  setDot('d1', 'done'); setDot('d2', 'done'); setDot('d3', 'active final');
} else {
  milestones.forEach(m => setTimeout(m.fn, m.t));
}

function activate(id) {
  document.getElementById(id).classList.add('active');
  document.getElementById(id).classList.remove('dim');
}
function dim(id) {
  document.getElementById(id).classList.remove('active');
  document.getElementById(id).classList.add('dim');
}
function pulseRole() {
  const r = document.getElementById('role-row');
  r.classList.add('pulse');
  setTimeout(() => r.classList.remove('pulse'), 1000);
}
function setDot(id, classes) {
  const el = document.getElementById(id);
  el.classList.remove('active', 'done', 'final');
  classes.split(' ').forEach(c => el.classList.add(c));
}
function tweenCounter(id, from, to, duration) {
  const el = document.getElementById(id);
  const start = performance.now();
  (function frame(now) {
    const p = Math.min(1, (now - start) / duration);
    const eased = 1 - Math.pow(1 - p, 3);
    el.textContent = Math.round(from + (to - from) * eased);
    if (p < 1) requestAnimationFrame(frame);
  })(start);
}
```

---

## 7. Markup Changes

### 7.1 Add

- `<div class="backbone-slot"><svg class="backbone" .../></div>` — antara role-row dan timeline.
- 2× tooltip caption "Naik Tahun" — positioned absolute di backbone-slot.
- `<div class="counter">…</div>` — di tiap year-card antara theme dan chips.
- `<span class="burst-ring">` — di dalam `.year-card.y3` (absolute).
- `id="role-row"` pada existing `.role-row` div.

### 7.2 Remove

- 2× `<div class="arrow" id="arrow-1/2">` — backbone menggantikan fungsinya.

### 7.3 Retain (no change)

- Header (eyebrow + title + em + progress dots).
- Role row markup (Panelman + Operator + divider), tambah ID saja.
- Year card structure (badge + theme + chips + footer-line), tambah counter.
- Brand footer "PROTON · Program Coaching Pekerja KPB".

### 7.4 Z-index Stack

```
stage::before (grid backdrop)            z=0
backbone-slot svg                         z=1 (own slot, not overlap cards)
stage children (cards, role-row, etc)    z=1 (default flow)
burst-ring (Y3)                           z=2 (absolute on card)
```

---

## 8. Accessibility — `prefers-reduced-motion`

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-delay: 0ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
  .year-card { opacity: 1; transform: none; }
  .chip { opacity: 1; transform: none; }
  .backbone-path { stroke-dashoffset: 0; opacity: 1; stroke: var(--red); }
  .title em { background-size: 100% 6px; }
  .dot.final { transform: none; }
}
```

JS branches on `matchMedia('(prefers-reduced-motion: reduce)').matches` — set final state without setTimeout chain.

Playwright record default user-agent = no preference → full motion plays. Real user with motion sensitivity → instant static layout, no animation, no jank.

---

## 9. Risks & Mitigations

| # | Risk | Likelihood | Mitigation |
|---|------|-----------|------------|
| R1 | Counter element bikin Y2 kartu overflow (3 chip + counter + footer) | Med | Kurangi gap kartu `clamp(10,1vw,16px)` → `clamp(8,0.8vw,12px)`; jika still tight, drop `.unit` "Kompetensi" label |
| R2 | Backbone slot ambil ruang vertikal → cards jadi pendek | Low | Slot 24–32px max; kompensasi via reduce header bottom margin sedikit |
| R3 | Burst ring radius rounded-rect tidak match card border-radius | Low | `border-radius: 12px` (lebih besar dari card 10px) untuk efek expand |
| R4 | rAF counter di Playwright headless = inconsistent timing | Low | Headless chromium support rAF stabil; verify via record |
| R5 | setTimeout drift kalau page lag saat record | Low | 35s window tolerant drift <100ms imperceptible |
| R6 | Tooltip "Naik Tahun" overlap dengan dot caption | Low | Position absolute, vertical offset above backbone, font 11px |
| R7 | Title em underline draw clash dengan `clamp` font-size at small viewport | Low | Background-size % independent of font-size |

---

## 10. Testing Checklist

- [ ] Local browser refresh — visual 35s end-to-end smooth, no layout shift
- [ ] Counter Y1=2, Y2=3, Y3=2 final values exact
- [ ] Active/dim transition halus, tidak flicker
- [ ] Backbone draw 3 segmen visible: navy → navy → red morph
- [ ] Tooltip "Naik Tahun" muncul 2× di waktu tepat (~9.5s, ~18s)
- [ ] Y3 burst ring visible & punchy (rounded-rect expand)
- [ ] Glow pulse 2× kelihatan jelas
- [ ] Final hold 28–35s: dot d3 breathe loop, semua state final, no jank
- [ ] Title em "Satu Tujuan" underline red draw kiri→kanan
- [ ] Role icons pulse 3× sync dengan onset tiap kartu
- [ ] Chip stamp rotate+scale + ✓ tick per chip muncul stagger
- [ ] DevTools `prefers-reduced-motion` simulate → static layout langsung visible
- [ ] No console errors
- [ ] Playwright record 36s → output `.webm`, durasi 35–36s, size 2–6 MB
- [ ] Chrome desktop 1920×1080 visual sesuai

---

## 11. Success Criteria

| # | Criterion | Verify |
|---|-----------|--------|
| SC1 | Durasi exact 35s (action 0–28s, hold 28–35s) | Stopwatch + record video durasi |
| SC2 | Action density ≥70% | Window kosong manual review (target 73%) |
| SC3 | SVG backbone line draw progresif visible 33/66/100% red | Screenshot t=4.5s, 11s, 19s, 25s |
| SC4 | Counter tick smooth, berhenti exact (2, 3, 2) | DevTools final state |
| SC5 | Chip stamp rotate+scale animation + ✓ tick visible | Visual frame-by-frame |
| SC6 | Role icons pulse 3× sync card onset | Visual |
| SC7 | Y3 burst ring + glow pulse 2× = climax punchy | User visual confirmation |
| SC8 | Active card glow + dim previous state machine works | DevTools class inspect |
| SC9 | Hold 28–35s ada dot breath, tidak total static | Visual final 7s |
| SC10 | Grid backdrop visible tapi tidak distract | Visual review |
| SC11 | Layout tidak overflow di 1920×1080 (Y2 paling padat) | Manual size check |
| SC12 | Palette + typography retain (navy + red + Inter) | Visual diff vs current |
| SC13 | Record `.webm` 35–36s, size 2–6 MB, no glitch | Playback verify |
| SC14 | File HTML size <30 KB | `wc -c` |
| SC15 | `prefers-reduced-motion` fallback bekerja (static layout) | DevTools simulate |
| SC16 | Title em "Satu Tujuan" underline draw red kiri→kanan | Visual |
| SC17 | Y3 ✓ checkmark drop-bounce visible & cute (not norak) | Visual |
| SC18 | Backbone Y3 segment stroke morph navy-soft → red | Frame compare |
| SC19 | Tooltip "Naik Tahun" muncul 2× di waktu tepat | Visual timing |
| SC20 | Naskah video docx **tidak berubah** | `git status` |

---

## 12. Task Breakdown (preview for writing-plans)

Sequential (most tasks touch same file = no parallelization).

| # | Task | File(s) |
|---|------|---------|
| 1 | Grid backdrop + stage::before pseudo | track-progresi.html |
| 2 | Backbone slot HTML + SVG markup + CSS line-draw 3-segment + stroke morph red | track-progresi.html |
| 3 | Tooltip caption "Naik Tahun" 2× | track-progresi.html |
| 4 | Remove existing `.arrow` elements (markup + CSS cleanup) | track-progresi.html |
| 5 | Counter markup + styling per kartu | track-progresi.html |
| 6 | Counter rAF tween JS + milestone integration | track-progresi.html |
| 7 | Chip stamp redesign (rotate+scale+tick CSS) | track-progresi.html |
| 8 | Card active glow + dim state machine + JS toggle | track-progresi.html |
| 9 | Role row pulse trigger (CSS + JS) | track-progresi.html |
| 10 | Title em underline draw red | track-progresi.html |
| 11 | Y3 ✓ checkmark drop-bounce | track-progresi.html |
| 12 | Y3 climax — burst ring + glow pulse 2× | track-progresi.html |
| 13 | Final hold ambient — dot breath JS+CSS | track-progresi.html |
| 14 | `prefers-reduced-motion` media query + JS branch | track-progresi.html |
| 15 | Record script duration 26→36s | record-track-progresi.mjs |
| 16 | Run record + verify .webm output | track-progresi.webm |
| 17 | Spec compliance final check (Section 11) | (verification) |

17 task, 17 atomic commits.

---

## 13. Definition of Done

- All 20 SC pass (Section 11)
- 17 atomic commits per task
- `track-progresi.webm` di-update di repo (size lama 2.1 MB → expected 3–5 MB)
- Naskah video docx **tidak** di-touch
- `git status` bersih kecuali file lama untracked (existing, abaikan)
- Spec doc ini di-update kalau ada deviasi saat execute

---

## 14. Rollback Strategy

Single file `track-progresi.html` overhaul. Kalau gagal:
- `git revert <overhaul-commits>` — kembali ke 25s version
- `.webm` lama (2.1 MB, 26s) auto restore via git
- Naskah video docx tidak terpengaruh (tidak diubah)
