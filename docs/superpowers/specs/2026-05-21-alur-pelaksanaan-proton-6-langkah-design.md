# Alur Pelaksanaan PROTON (6 Langkah) Animation — Design Spec

**Status:** Draft — awaiting user review
**Author:** Brainstorm session 2026-05-21
**Target file:** `docs/assets/proton-video/alur-pelaksanaan.html`
**Series context:** PROTON video bagian 4 (1:15–1:55 di final video, 40s narasi). Animasi durasi 45s (5s buffer untuk editor video).

---

## 1. Goal

Bangun animasi HTML 16:9 standalone untuk PROTON video bagian 4 (Alur Pelaksanaan PROTON · 6 Langkah, durasi 45s), lalu record menjadi `.webm` lewat Playwright. Scope animasi: **digital/UI ilustrasi 6 step saja**. Cutaway talking-head actor footage **out-of-scope** (editor video handle).

**Quantified targets:**
- Durasi exact 45s (intro 2s + 6 step + outro 5s).
- Action density ≥85%.
- 6 scene UI mockup unik dengan cross-fade transition.
- Visual identity match track-progresi (navy + red + Inter + grid backdrop + spring easing).

---

## 2. Decision Log (from brainstorm)

| # | Decision | Choice |
|---|----------|--------|
| D1 | Cutaway talking-head | Out-of-scope (editor handle real actor footage) |
| D2 | File output | Single HTML 45s continuous |
| D3 | Layout | Hybrid stepper bottom + scene main area |
| D4 | UI authenticity | Hybrid recognizable (browser frame + Portal HC KPB style, no pixel-perfect) |
| D5 | Tone | Identical match track-progresi (D editorial-dataviz + B spring) |
| D6 | Timing distribution | Weighted (45s, intro 2s + steps 5/8/5/8/5/7 + outro 5s) |
| D7 | Step 4 diagram | Document central + 3 stamp drop sequential (horizontal row bawah) |
| D8 | Closing frame | Mini-recap 6 ikon row + tagline |
| D9 | Em "Terstruktur" | Underline draw red 0%→100% (carry pattern track-progresi) |
| D10 | Scene 4 framing | Wrap dalam browser-frame "Approval Bukti · #5847" |
| D11 | Step 5 quiz content | Self-referential "Berapa langkah PROTON?" → B) 6 |
| D12 | Step 6 dates | Realistik 3-year cycle (Mar 2024 → Jul 2026) |
| D13 | Audio sync | Narasi 40s align t=2.5s–42.5s; animasi intro 2s + outro 2.5s no-narasi buffer |

---

## 3. Architecture

### 3.1 File Scope

| File | Status | Tanggung jawab |
|------|--------|----------------|
| `docs/assets/proton-video/alur-pelaksanaan.html` | CREATE | Sumber animasi — single file, 7 scene (6 step + outro), CSS + SVG + inline JS |
| `docs/assets/proton-video/record-alur-pelaksanaan.mjs` | CREATE | Playwright record 46s |
| `docs/assets/proton-video/alur-pelaksanaan.webm` | GENERATE | Output 45s, 1920×1080, target 4–7 MB |

Naming consistent dengan `track-progresi.*` pattern.

### 3.2 Tech Stack

CSS3 + SVG + vanilla JS inline + Inter (Google Fonts) + Playwright chromium (record) + Python http.server (serve lokal saat record).

### 3.3 Out of Scope

- Cutaway talking-head actor (editor video).
- Real Portal HC KPB screenshot (pakai hybrid recognizable mockup).
- Naskah audio docx update — narasi tetap 40s, mapping audio→video di post-production.
- Logo PROTON / KPB asli SVG — pakai text mark "PROTON × KPB" cukup, logo asset separate task.

---

## 4. Timeline 45s Breakdown

| Slot | Window | Konten |
|------|--------|--------|
| **Intro** | 0.0–2.0s | Stage + grid backdrop fade-in (0–0.4s). Header eyebrow + title fade-down (0.2–1.0s). Em "Terstruktur" underline draw red (1.2–1.8s). Stepper 6 dot row appear fade-up (0.9–1.5s, all pending). Brand mark fade-in (1.5s, static). |
| **Step 1 — HC Assign Coach** | 2.0–7.0s | Step 1 stepper active. Browser frame "Portal HC KPB · Admin" appear (2.0–2.4s). Admin card pop spring kiri (2.4–3.0s). Arrow draw kiri→kanan + label "ASSIGN" fade (3.2–4.5s). Coach card pop spring kanan + highlight pulse red border (4.5–5.5s). Hold (5.5–7.0s). |
| **Step 2 — Deliverable** | 7.0–15.0s | Cross-fade to scene-2. Step 1 done, step 2 active. Browser frame "Portal HC KPB · IDP" + sidebar (7.0–7.5s). Nav item "IDP" highlight (7.5s). 3 task card slide-in dari kanan stagger 200ms (8.0–8.8s). Cursor click upload (10.5s). Upload progress bar 0→100% sweep (11.0–13.0s). Toast "✓ Bukti terunggah" slide-in (13.5s). Hold (13.5–15.0s). |
| **Step 3 — Coaching Proton** | 15.0–20.0s | Cross-fade to scene-3. Step 2 done, step 3 active. Browser frame "Coaching PROTON · Sesi 04" (15.0–15.4s). 3 form field label fade-up (15.4–15.8s). Field "Diskusi" text type-in (15.6–16.4s). Field "Kesimpulan" type-in (16.5–17.3s). Field "Tindak Lanjut" type-in (17.5–18.3s). Button "Simpan Sesi" pulse + click (18.5s). Toast "✓ Sesi tersimpan" (19.0s). Hold (19.0–20.0s). |
| **Step 4 — Multi-Role Approval** | 20.0–28.0s | Cross-fade to scene-4. Step 3 done, step 4 active. Browser frame "Portal HC KPB · Approval Bukti · #5847" (20.0–20.4s). Document central pop "Bukti Pekerjaan" (20.4–20.8s). Stamp Sr Supervisor drop spring (20.8–21.4s) + impact ring. Stamp Section Head drop (22.5–23.1s) + impact ring. Stamp HC Final drop (24.5–25.1s) + impact ring red final. Status badge "TERVERIFIKASI" fade-up bottom (26.5s). Hold (26.5–28.0s). |
| **Step 5 — Final Assessment** | 28.0–33.0s | Cross-fade to scene-5. Step 4 done, step 5 active. Browser frame "Portal HC KPB · Final Assessment" (28.0–28.4s). Metadata strip "Durasi 60 menit · Soal 30" fade-up (28.5s). Pertanyaan card slide-in (29.0s). 4 option radio fade stagger 150ms (29.5–30.1s). Option B select highlight (30.5s). Progress bar 0→40% sweep "12/30" (31.0–32.5s). Hold (32.5–33.0s). |
| **Step 6 — Histori PROTON** | 33.0–40.0s | Cross-fade to scene-6. Step 5 done, step 6 active+final (red). Browser frame "Portal HC KPB · Histori PROTON" (33.0–33.4s). Vertical SVG line backbone draw atas-bawah (33.4–34.2s). Row 1 "Mar 2024 — HC Assign Coach" slide-in kiri (33.8s). Row 2 "Sep 2024 — Deliverable Uploaded" (34.5s). Row 3 "Mar 2025 — Coaching Session" (35.2s). Row 4 "Sep 2025 — Multi-Role Approval ✓" (35.9s). Row 5 "Mar 2026 — Final Assessment 85%" (36.6s). Row 6 "Jul 2026 — Sertifikat Issued ⭐" red highlight (37.3s). Caption "Rekam jejak permanen" fade-in (38.5s). Hold (38.5–40.0s). |
| **Outro — Mini-Recap** | 40.0–45.0s | Cross-fade to scene-outro. Stepper "done-all" all 6 ticked, step 6 red final retain. Step 6 main scene fade-out (40.0–40.4s). 6 ikon row fade-up stagger 80ms (40.4–41.5s): [HC Assign] [IDP Upload] [Catat Sesi] [Approve 3-Tier] [Assess Final] [Histori]. Tiap ikon ✓ stamp sequential 200ms apart (41.8–43.0s). Tagline "ALUR PROTON · 6 LANGKAH TERSTRUKTUR" fade-in (43.5s). Hold final poster (43.5–45.0s). |

**Action density:** action 0–43.5s / 45s = 96% (intro+outro counted as active beats since they animate). Static hold cuma 1.5s.

---

## 5. Layout Structure

### 5.1 Stage (16:9, 1600px max)

```
┌────────────────────────────────────────────────────┐
│  Header bar (8% top)                                │
│  ▸ Eyebrow: ALUR PELAKSANAAN PROTON (red, uppercase)│
│  ▸ Title: Enam Langkah Terstruktur (em red underline)│
├────────────────────────────────────────────────────┤
│                                                    │
│  MAIN SCENE AREA (~74% mid)                         │
│  ▸ position: relative, scene = position: absolute   │
│  ▸ Cross-fade transition antar scene                │
│                                                    │
│                                                    │
├────────────────────────────────────────────────────┤
│  Stepper row (10% bottom, centered)                │
│  ① — ② — ③ — ④ — ⑤ — ⑥                              │
└────────────────────────────────────────────────────┘
[grid backdrop hairline navy 4% opacity, z=0]
[brand mark "PROTON × KPB" bottom-right corner, opacity 0.5]
```

### 5.2 Header Styling

```css
.header { margin-bottom: clamp(12px, 1.5vw, 24px); }
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
```

### 5.3 Stepper Styling

```css
.stepper {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0;
  margin-top: auto;
  opacity: 0;
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
}
.step-node.active.final {
  background: var(--red);
  border-color: var(--red);
}
.stepper.done-all .step-node {
  background: var(--navy-soft);
  border-color: var(--navy-soft);
  color: transparent;
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

### 5.4 Scene Container + Cross-Fade

```css
.scenes {
  flex: 1;
  position: relative;
  min-height: 0;
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
  padding: clamp(20px, 2vw, 40px);
}
.scene.active {
  opacity: 1;
  transform: translateY(0);
}
```

### 5.5 Brand Mark

```css
.brand-mark {
  position: absolute;
  bottom: 20px;
  right: 28px;
  font-size: clamp(10px, 0.78vw, 12px);
  font-weight: 700;
  letter-spacing: 0.2em;
  color: var(--muted);
  opacity: 0.5;
  pointer-events: none;
}
```

### 5.6 Grid Backdrop (carry from track-progresi)

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
  z-index: 0;
}
.stage > * { position: relative; z-index: 1; }
```

---

## 6. Motion Catalog (Reusable Primitives)

### 6.1 Browser Frame (reusable component)

```css
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
  min-height: clamp(320px, 32vw, 480px);
}
```

### 6.2 Card Pop (spring)

```css
@keyframes cardPop {
  0%   { opacity: 0; transform: translateY(12px) scale(0.96); }
  100% { opacity: 1; transform: translateY(0) scale(1); }
}
```

Cubic-bezier(0.34, 1.56, 0.64, 1) untuk spring overshoot. Reuse di Step 1 (admin/coach card), Step 2 (task card), Step 4 (document).

### 6.3 SVG Arrow Draw (Step 1, Step 6 backbone)

```css
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
  animation: arrowDraw 0.8s 1.0s ease-out forwards;
}
.arrow-svg polyline {
  opacity: 0;
  animation: fadeIn 0.3s 1.7s ease-out forwards;
}
@keyframes arrowDraw { to { stroke-dashoffset: 0; } }
```

### 6.4 Text Type-In JS (Step 3 form)

```javascript
function typeIn(el, text, perChar = 25) {
  el.textContent = '';
  let i = 0;
  const id = setInterval(() => {
    el.textContent += text[i++];
    if (i >= text.length) clearInterval(id);
  }, perChar);
}
```

Speed 25ms/char × ~40 chars = ~1s. Lega untuk 5s window.

### 6.5 Document Stamp Drop (Step 4) — Horizontal Row Bawah

Layout:

```
       ┌─ Document Central ─┐
       │   📄                │
       │   Bukti Pekerjaan  │
       │   ─── ─── ───      │
       └─────────────────────┘
       
  [Stamp s1]    [Stamp s2]    [Stamp s3]
  Sr Supervisor  Section Head   HC Final
  ✓ 03/05/26    ✓ 04/05/26    ✓ 05/05/26
  (red)         (navy)        (red ring)
```

CSS:

```css
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
.stamp.fired {
  animation: stampDrop 0.6s cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
}
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

### 6.6 Progress Bar Sweep

```css
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
  transition: width 1.5s cubic-bezier(0.65, 0, 0.35, 1);
}
.progress-bar.active .fill { width: var(--target, 100%); }
```

Reuse Step 2 (upload 0→100%) dan Step 5 (quiz 0→40%).

### 6.7 Toast Slide-In

```css
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

### 6.8 Timeline Backbone Vertical (Step 6)

```css
.timeline-backbone {
  position: absolute;
  left: 32px;
  top: 0;
  bottom: 0;
  width: 2px;
}
.timeline-backbone path {
  stroke: var(--navy-soft);
  stroke-width: 2;
  stroke-dasharray: 400;
  stroke-dashoffset: 400;
  animation: backboneDraw 0.8s 0.4s ease-out forwards;
}
@keyframes backboneDraw { to { stroke-dashoffset: 0; } }

.timeline-row {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 10px 0 10px 56px;
  position: relative;
  opacity: 0;
  transform: translateX(-12px);
}
.timeline-row::before {
  content: "";
  position: absolute;
  left: 28px;
  top: 50%;
  width: 12px;
  height: 12px;
  background: var(--navy);
  border-radius: 50%;
  border: 2px solid #FFFFFF;
  box-shadow: 0 0 0 2px var(--navy-soft);
  transform: translateY(-50%);
}
.timeline-row.last::before { background: var(--red); box-shadow: 0 0 0 2px var(--red); }
.timeline-row.show { opacity: 1; transform: translateX(0); transition: opacity 0.4s ease, transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1); }
```

JS toggle `.show` per row via stagger 700ms.

---

## 7. Markup Structure

```html
<body>
  <div class="stage">
    <!-- Header -->
    <div class="header">
      <div class="eyebrow">Alur Pelaksanaan PROTON</div>
      <h1 class="title">Enam Langkah <em>Terstruktur</em></h1>
    </div>

    <!-- Scenes -->
    <div class="scenes">

      <!-- Scene 1: HC Assign Coach -->
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
              <svg class="arrow-svg" viewBox="0 0 200 40">
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

      <!-- Scene 2: Deliverable IDP+Upload -->
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

      <!-- Scene 3: Coaching Proton Form -->
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

      <!-- Scene 4: Approval -->
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

      <!-- Scene 5: Final Assessment -->
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
                <label class="opt"><input type="radio" disabled> A) 4 langkah</label>
                <label class="opt selected"><input type="radio" checked disabled> B) 6 langkah</label>
                <label class="opt"><input type="radio" disabled> C) 8 langkah</label>
                <label class="opt"><input type="radio" disabled> D) 10 langkah</label>
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

      <!-- Scene 6: Histori -->
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

      <!-- Scene Outro: Mini-Recap -->
      <div class="scene" id="scene-outro">
        <div class="recap-grid">
          <div class="recap-item" id="r1"><div class="r-icon">👥</div><div class="r-label">HC Assign Coach</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r2"><div class="r-icon">📤</div><div class="r-label">Upload Bukti</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r3"><div class="r-icon">📝</div><div class="r-label">Catat Sesi</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r4"><div class="r-icon">🗂️</div><div class="r-label">Approve 3-Tier</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r5"><div class="r-icon">📋</div><div class="r-label">Assess Final</div><div class="r-check">✓</div></div>
          <div class="recap-item" id="r6"><div class="r-icon">⭐</div><div class="r-label">Histori</div><div class="r-check">✓</div></div>
        </div>
        <div class="recap-tagline">Alur PROTON · 6 Langkah Terstruktur</div>
      </div>

    </div>

    <!-- Stepper -->
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

    <!-- Brand mark -->
    <div class="brand-mark">PROTON × KPB</div>
  </div>

  <script>
    // orchestration
  </script>
</body>
```

---

## 8. JS Orchestration

```javascript
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

const milestones = [
  // Intro
  { t: 500, fn: () => document.querySelector('.stage').classList.add('ready') },

  // Step 1
  { t: 2000, fn: () => { setStep(1); showScene('scene-1'); } },

  // Step 2
  { t: 7000, fn: () => { setStep(2); showScene('scene-2'); } },
  { t: 11000, fn: () => fillProgress('upload-bar', 100) },
  { t: 13500, fn: () => showToast('toast-upload') },

  // Step 3
  { t: 15000, fn: () => { setStep(3); showScene('scene-3'); } },
  { t: 15600, fn: () => typeIn(document.getElementById('field-diskusi'), 'Membahas hambatan studi kasus refinery ops.') },
  { t: 16500, fn: () => typeIn(document.getElementById('field-kesimpulan'), 'Coachee perlu refresh modul 3.') },
  { t: 17500, fn: () => typeIn(document.getElementById('field-tindak'), 'Penugasan modul ulang minggu depan.') },
  { t: 19000, fn: () => showToast('toast-coaching') },

  // Step 4
  { t: 20000, fn: () => { setStep(4); showScene('scene-4'); } },
  { t: 20800, fn: () => stampFire('stamp-1') },
  { t: 22500, fn: () => stampFire('stamp-2') },
  { t: 24500, fn: () => stampFire('stamp-3') },
  { t: 26500, fn: () => document.getElementById('status-verif').classList.add('show') },

  // Step 5
  { t: 28000, fn: () => { setStep(5); showScene('scene-5'); } },
  { t: 31000, fn: () => fillProgress('quiz-bar', 40) },

  // Step 6
  { t: 33000, fn: () => { setStep(6); showScene('scene-6'); } },
  { t: 33800, fn: () => showRow('row-1') },
  { t: 34500, fn: () => showRow('row-2') },
  { t: 35200, fn: () => showRow('row-3') },
  { t: 35900, fn: () => showRow('row-4') },
  { t: 36600, fn: () => showRow('row-5') },
  { t: 37300, fn: () => showRow('row-6') },
  { t: 38500, fn: () => document.getElementById('histori-caption').classList.add('show') },

  // Outro
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

JS budget: ~85 LOC. Helpers + milestones + reduced-motion branch.

---

## 9. Accessibility — `prefers-reduced-motion`

```css
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
}
```

JS branch: skip setTimeout chain, langsung set state final (scene-outro visible, stepper done-all, all recap items shown+checked).

---

## 10. Risks & Mitigations

| # | Risk | Likelihood | Mitigation |
|---|------|-----------|------------|
| R1 | File size HTML melebihi 35 KB (target) | Med | Reuse browser-frame component. Compress CSS. Estimasi 32-38 KB realistic. |
| R2 | Scene 2 elemen banyak (sidebar+3 task+upload+toast) — overlap timing | Med | Stagger ketat 200ms. Visual verify increment per beat. |
| R3 | Cross-fade 0.5s = 2 scene visible briefly = layered awkward | Low | Opacity ease-in-out timing tuned. |
| R4 | Type-in text panjang melebihi container = wrap glitch | Med | Limit text <50 chars per field. Container fixed min-height. |
| R5 | Stamp absolute positioning Step 4 mis-aligned di rescale | Low | Stage 16:9 fixed proportion. Position relative ke `.stamps-row` flex. |
| R6 | Naskah audio 40s vs animasi 45s mismatch confuse editor | Med | Spec eksplisit di Section 4: narasi t=2.5s–42.5s, intro 2s + outro 2.5s buffer. Documented for editor. |
| R7 | Implementation scope besar (16 task) — bisa molor | Med | Per-scene atomic task. Each commit independently testable. |
| R8 | rAF/setTimeout drift di Playwright headless 45s | Low | Tolerant drift <100ms. Test via record. |
| R9 | Brand mark "PROTON × KPB" text-only kurang authentic vs logo asli | Low | Acceptable v1. Logo SVG asset future task kalau diminta. |

---

## 11. Testing Checklist

- [ ] Local browser refresh — visual 45s end-to-end smooth
- [ ] Stepper progresif 1→6, state pending→active→done sync milestones (DevTools class inspect)
- [ ] Scene 1 admin→coach arrow draw + label ASSIGN
- [ ] Scene 2 sidebar IDP highlight + 3 task card + upload bar + toast
- [ ] Scene 3 form 3 field type-in sequential
- [ ] Scene 4 document + 3 stamp drop spring + impact ring + status badge
- [ ] Scene 5 quiz card + option B select + progress 12/30
- [ ] Scene 6 timeline vertical line draw + 6 row slide-in + sertifikat ⭐ red highlight
- [ ] Outro 6 ikon recap fade-up stagger + ✓ stamp + tagline
- [ ] Cross-fade transition smooth tiap boundary
- [ ] Title em "Terstruktur" underline draw red kiri→kanan
- [ ] DevTools `prefers-reduced-motion: reduce` simulate → static final layout
- [ ] No console errors
- [ ] Playwright record 46s → output `.webm` 45-46s, 4-7 MB
- [ ] Chrome 1920×1080 visual sesuai
- [ ] File size HTML <35 KB

---

## 12. Success Criteria

| # | Criterion | Verify |
|---|-----------|--------|
| SC1 | Durasi exact 45s (action 0–43.5s, hold 43.5–45s) | Video stopwatch |
| SC2 | Action density ≥85% (target 96%) | Manual review window kosong |
| SC3 | 6 scene UI mockup tampil sequential no overlap glitch | Frame-by-frame |
| SC4 | Stepper sync milestones (pending→active→done→final) | DevTools class inspect tiap milestone |
| SC5 | Scene 1 admin→coach arrow + ASSIGN label | Visual |
| SC6 | Scene 2 sidebar IDP + 3 task + upload bar + toast | Visual |
| SC7 | Scene 3 form type-in 3 field sequential | Visual + content match |
| SC8 | Scene 4 document + 3 stamp drop + impact ring + status badge | Visual |
| SC9 | Scene 5 quiz card + option B + progress 12/30 | Visual |
| SC10 | Scene 6 timeline backbone + 6 row + sertifikat ⭐ red | Visual |
| SC11 | Outro recap 6 ikon + ✓ stagger + tagline | Visual final 5s |
| SC12 | Cross-fade transition smooth (no flash) | Visual transition windows |
| SC13 | Palette navy + red + Inter + grid backdrop match track-progresi | Side-by-side visual |
| SC14 | Title em "Terstruktur" underline draw red | Visual |
| SC15 | `prefers-reduced-motion` fallback: scene-outro state immediate | DevTools simulate |
| SC16 | File HTML <35 KB | `wc -c` |
| SC17 | `.webm` 45–46s, 4–7 MB, no glitch | Playback |
| SC18 | Recordable idempotent via `record-alur-pelaksanaan.mjs` | Re-run |
| SC19 | Naskah docx tidak berubah | `git status` |
| SC20 | Audio sync window jelas (narasi t=2.5s–42.5s) | Spec Section 4 documented |

---

## 13. Task Breakdown (preview for writing-plans)

| # | Task | Files |
|---|------|-------|
| 0 | Setup Playwright + http.server verify | env |
| 1 | Scaffold HTML — stage 16:9 + palette + grid backdrop + header + brand mark | alur-pelaksanaan.html |
| 2 | Stepper component (markup + state machine CSS) | alur-pelaksanaan.html |
| 3 | Scene container + cross-fade transition CSS | alur-pelaksanaan.html |
| 4 | Browser-frame reusable component (CSS) | alur-pelaksanaan.html |
| 5 | Scene 1 — HC Assign Coach (markup + CSS + arrow draw) | alur-pelaksanaan.html |
| 6 | Scene 2 — Deliverable IDP (sidebar + task + upload + toast) | alur-pelaksanaan.html |
| 7 | Scene 3 — Coaching Proton form (3 field + type-in) | alur-pelaksanaan.html |
| 8 | Scene 4 — Approval document + 3 stamps + impact ring | alur-pelaksanaan.html |
| 9 | Scene 5 — Final Assessment quiz | alur-pelaksanaan.html |
| 10 | Scene 6 — Histori timeline 6 row vertical | alur-pelaksanaan.html |
| 11 | Scene outro — 6 ikon recap + tagline | alur-pelaksanaan.html |
| 12 | JS orchestration — milestones + helpers (setStep, showScene, typeIn, fillProgress, stampFire, showRow, showToast) | alur-pelaksanaan.html |
| 13 | `prefers-reduced-motion` media query + JS branch | alur-pelaksanaan.html |
| 14 | Record script `record-alur-pelaksanaan.mjs` (duration 46000ms) | record-alur-pelaksanaan.mjs |
| 15 | Run record + verify webm | alur-pelaksanaan.webm |
| 16 | Spec compliance final check (Section 12) | (verification) |

17 atomic task, sequential single-file edit.

---

## 14. Definition of Done

- All 20 SC pass (Section 12)
- 17 atomic commits per task
- `alur-pelaksanaan.webm` di-generate di repo (4–7 MB)
- Naskah docx **tidak** di-touch
- `git status` bersih kecuali file lama untracked
- Spec doc ini di-update kalau ada deviasi saat execute

---

## 15. Rollback Strategy

Single new file `alur-pelaksanaan.html` + record script + webm. Kalau gagal:
- `git revert <task-commits>` — kembali ke state pre-overhaul (track-progresi era)
- File lainnya tidak terpengaruh
- Naskah docx tidak ter-touch

---

## 16. Audio Sync Note (untuk Editor Video)

Animasi durasi 45s, narasi 40s. Mapping:

| Slot | Animasi | Narasi audio |
|------|---------|--------------|
| 0.0–2.5s | Intro animasi | Silent (animation lead-in) |
| 2.5–42.5s | Step 1–6 animasi (40s) | Narasi 40s overlay (sentence per step) |
| 42.5–45.0s | Outro recap hold | Silent (animation tail) |

Editor align narasi audio mulai t=2.5s ke t=42.5s. Cutaway talking-head actor insert per slot:
- Cutaway A (coaching, 3s) cuts during Step 3 window (t=15s–20s, pilih sub-slot 3s)
- Cutaway B (supervisor, 2s) cuts during Step 4 window (t=20s–28s)
- Cutaway C (coachee smile, 3s) cuts during Step 6 window (t=33s–40s) atau outro
