# PCP Slide 8 — Versi P Compact Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a compact 1100×440px HTML variant of Versi P (workflow topology) that renders as a PNG suitable for insertion into placeholder #1 (GAMBAR DESAIN) on slide 8 of Risalah Web.pptx, without modifying the v3.7 master.

**Architecture:** Single self-contained HTML file under `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`. Side-by-side dual panels (Sebelum left | Sesudah right) at 545px each, sharing token vocabulary with the master but with compact-specific font scale and dropped chrome (no header bar, no toolbar, no comparison/legend tables). Buffer zone hero in Sesudah aligns to a "no-buffer" dashed-spacer slot in Sebelum to preserve symmetric row heights. Export PNG button uses html2canvas CDN at 2× scale for retina-crisp output.

**Tech Stack:** Plain HTML5 + CSS3 (no build), html2canvas 1.4.1 via CDN, Chrome browser for verification.

**Source spec:** `docs/superpowers/specs/2026-05-22-pcp-slide8-versi-p-compact-design.md`

**Source data reference (read-only, do NOT modify):** `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html`

---

## File Structure

| File | Role | Status |
|---|---|---|
| `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html` | Compact HTML for slide 8 placeholder #1 | CREATE |
| `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/README.md` | Subfolder explainer (purpose, export workflow) | CREATE |
| `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html` | Master v3.7 — source of truth for content | READ-ONLY |

No tests authored (visual artifact, manual verify via Chrome). Verification is dimension + screenshot fidelity checklist at the end.

---

## Task 1: Scaffold Subfolder + HTML Skeleton

**Files:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/README.md`

- [ ] **Step 1: Create subfolder via writing the README**

Create `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/README.md`:

```markdown
# Slide 8 PPT-Specific Variants

Compact HTML variants of §3.4 diagrams tailored for insertion into Risalah Web.pptx slide 8 placeholders.

## Files

- `versi-p-compact.html` — placeholder #1 GAMBAR DESAIN (1100×440 landscape)
- (future) `panel-rencana-compact.html` — placeholder #2 RENCANA PEMBUATAN
- (future) `panel-standard-compact.html` — placeholder #3 STANDARD DESIGN

## Export Workflow

1. Open `versi-p-compact.html` in Chrome (fresh browser, no zoom)
2. Click `📸 Export PNG` button top-right
3. PNG downloads as `versi-p-slide8.png` at 2200×880 (retina @2x)
4. Insert into PowerPoint slide 8 GAMBAR DESAIN box, fit-to-frame

## Design Spec Reference

`docs/superpowers/specs/2026-05-22-pcp-slide8-versi-p-compact-design.md`

## Master File (DO NOT MODIFY for slide variants)

`../versi-p-workflow-topology.html` — full-detail v3.7, audience: engineering review
```

- [ ] **Step 2: Create HTML skeleton with token CSS**

Create `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`:

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<title>§3.4 Slide 8 — Versi P Compact</title>
<script src="https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js"></script>
<style>
  :root {
    --pertamina-red: #C8102E;
    --pertamina-red-light: #fce8eb;
    --pertamina-blue: #00558C;
    --pertamina-blue-dark: #003D63;
    --pertamina-blue-light: #e6f0f7;
    --pertamina-green: #00A551;
    --pertamina-green-light: #d4f0dd;
    --pertamina-yellow: #FFC72C;
    --pertamina-yellow-light: #fff5d6;
    --pertamina-orange-light: #ffe8d1;
    --neutral-gray: #6b7280;
    --neutral-light: #d1d5db;
    --bg: #f6f7fb;
    --hub-grad: linear-gradient(135deg, #00558C, #00A551);

    --fs-xxs: 0.5rem;
    --fs-xs: 0.55rem;
    --fs-sm: 0.65rem;
    --fs-base: 0.75rem;
    --fs-title: 0.85rem;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0;
    padding: 0;
    width: 1100px;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    background: var(--bg);
    color: #1f2937;
  }
  #export-btn {
    position: absolute;
    top: 8px;
    right: 8px;
    background: var(--pertamina-red);
    color: white;
    border: none;
    padding: 6px 12px;
    border-radius: 4px;
    font-weight: 600;
    cursor: pointer;
    z-index: 100;
  }
  @media print { #export-btn { display: none; } }
</style>
</head>
<body>
<button id="export-btn" onclick="exportPNG()">📸 Export PNG</button>

<div id="diagram-row">
  <!-- Panels go here in Task 2 + 3 -->
</div>

<script>
async function exportPNG() {
  const btn = document.getElementById('export-btn');
  btn.style.display = 'none';
  const target = document.getElementById('diagram-row');
  const canvas = await html2canvas(target, {
    scale: 2,
    backgroundColor: '#ffffff',
    useCORS: true
  });
  btn.style.display = '';
  const link = document.createElement('a');
  link.download = 'versi-p-slide8.png';
  link.href = canvas.toDataURL('image/png');
  link.click();
}
</script>
</body>
</html>
```

- [ ] **Step 3: Open in Chrome to verify load**

Open file in Chrome: `file:///C:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`

Expected: blank page with red `📸 Export PNG` button top-right, no console errors.
If console error mentions html2canvas: verify CDN URL loaded (Network tab).

- [ ] **Step 4: Commit**

```bash
git add "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/"
git commit -m "feat(pcp-slide8): scaffold versi-p-compact skeleton + subfolder README"
```

---

## Task 2: Build Panel SEBELUM (Left)

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`

- [ ] **Step 1: Add panel CSS to `<style>` block**

Append inside `<style>` (before `@media print`):

```css
  #diagram-row {
    display: grid;
    grid-template-columns: 545px 545px;
    gap: 8px;
    padding: 4px;
  }
  .panel {
    background: white;
    border-radius: .5rem;
    padding: 8px;
    box-shadow: 0 2px 8px rgba(0,0,0,.06);
  }
  .panel.sebelum { border-top: 4px solid var(--pertamina-red); }
  .panel.sesudah { border-top: 4px solid var(--pertamina-green); }

  .panel-title {
    font-size: var(--fs-title);
    font-weight: 700;
    margin: 0 0 6px;
    padding-bottom: 4px;
    border-bottom: 1px dashed var(--neutral-light);
  }
  .panel.sebelum .panel-title { color: var(--pertamina-red); }
  .panel.sesudah .panel-title { color: var(--pertamina-blue); }

  .layer {
    display: grid;
    grid-template-columns: 90px 1fr;
    gap: 4px;
    min-height: 48px;
    border-bottom: 1px dashed var(--neutral-light);
    padding: 3px 0;
  }
  .layer:last-child { border-bottom: none; }
  .layer-label {
    font-size: var(--fs-sm);
    font-weight: 700;
    padding: 2px 4px;
    display: flex;
    flex-direction: column;
    justify-content: center;
    color: var(--neutral-gray);
  }
  .layer-label .icon { font-size: 1rem; line-height: 1; }
  .layer-label .actor { font-weight: 700; color: #1f2937; }
  .layer-label .lv { font-size: var(--fs-xxs); font-weight: 500; opacity: .7; }
  .layer-content {
    display: flex;
    flex-wrap: wrap;
    gap: 3px;
    align-items: center;
    padding: 2px 4px;
  }
  .comp {
    padding: 2px 5px;
    border: 1px solid;
    border-radius: 3px;
    font-size: var(--fs-xs);
    white-space: nowrap;
    line-height: 1.3;
  }
  .comp.manual { background: var(--pertamina-red-light); border-color: var(--pertamina-red); color: var(--pertamina-red); }
  .comp.tool-ext { background: var(--pertamina-yellow-light); border-color: var(--pertamina-yellow); color: #8a6500; }
  .comp.paper { background: var(--pertamina-orange-light); border-color: #d97706; color: #9a3412; }
  .comp.portal { background: var(--pertamina-blue-light); border-color: var(--pertamina-blue); color: var(--pertamina-blue-dark); }
  .marker {
    display: inline-block;
    width: 12px;
    height: 12px;
    border-radius: 50%;
    line-height: 12px;
    text-align: center;
    color: white;
    font-size: var(--fs-xxs);
    font-weight: 800;
    margin-left: 2px;
  }
  .marker.issue { background: var(--pertamina-red); }
  .marker.improvement { background: var(--pertamina-green); }

  .no-buffer-slot {
    height: 50px;
    border: 1.5px dashed var(--neutral-light);
    border-radius: 4px;
    margin: 4px 0;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: var(--fs-sm);
    font-style: italic;
    color: var(--neutral-gray);
  }
```

- [ ] **Step 2: Add Sebelum panel HTML inside `#diagram-row`**

Replace `<!-- Panels go here in Task 2 + 3 -->` with:

```html
  <section class="panel sebelum">
    <h2 class="panel-title">❌ SEBELUM (Kondisi Aktual)</h2>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">👔</span>
        <span class="actor">Manajemen</span>
        <span class="lv">Lv 5 · Strategic</span>
      </div>
      <div class="layer-content">
        <span class="comp manual">📄 Laporan PDF/Excel</span>
        <span class="comp tool-ext">📧 Email Pertamina</span>
        <span class="marker issue">D</span>
      </div>
    </div>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">👤</span>
        <span class="actor">HC</span>
        <span class="lv">Lv 4 · Governance</span>
      </div>
      <div class="layer-content">
        <span class="comp manual">📊 Excel Pekerja</span>
        <span class="comp manual">📊 Excel Assessment</span>
        <span class="comp manual">📊 Excel Training</span>
        <span class="comp manual">📊 Excel KKJ</span>
        <span class="comp manual">📊 Excel Sertifikat</span>
        <span class="comp manual">📝 Word Template</span>
        <span class="marker issue">A</span>
        <span class="marker issue">B</span>
      </div>
    </div>

    <div class="no-buffer-slot">— Tidak ada hub terintegrasi —</div>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">🏢</span>
        <span class="actor">Atasan</span>
        <span class="lv">Lv 3 · Supervisory</span>
      </div>
      <div class="layer-content">
        <span class="comp tool-ext">📧 Email kotak masuk</span>
        <span class="comp tool-ext">💬 WhatsApp approval</span>
        <span class="marker issue">C</span>
        <span class="marker issue">E</span>
      </div>
    </div>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">🧑‍🏫</span>
        <span class="actor">Coach</span>
        <span class="lv">Lv 2 · Coaching</span>
      </div>
      <div class="layer-content">
        <span class="comp paper">📋 Form PROTON cetak</span>
        <span class="comp paper">📁 Arsip fisik</span>
        <span class="comp tool-ext">💬 WhatsApp (foto)</span>
        <span class="comp tool-ext">📧 Email (lampiran)</span>
        <span class="marker issue">A</span>
        <span class="marker issue">E</span>
      </div>
    </div>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">👷</span>
        <span class="actor">Pekerja</span>
        <span class="lv">Lv 1 · Operational</span>
      </div>
      <div class="layer-content">
        <span class="comp tool-ext">🌐 FleQi Quiz</span>
        <span class="comp paper">🎓 Sertifikat hardcopy</span>
        <span class="comp manual">📊 Excel pribadi (IDP)</span>
        <span class="marker issue">A</span>
        <span class="marker issue">F</span>
      </div>
    </div>
  </section>
```

- [ ] **Step 3: Reload Chrome, verify Sebelum panel renders**

Refresh browser tab. Expected:
- Left panel only (right is empty grid cell), border-top red
- Title "❌ SEBELUM (Kondisi Aktual)" red bold
- 5 layer rows + 1 dashed `— Tidak ada hub terintegrasi —` between L4 and L3
- Each layer shows actor label left, comp boxes wrap on right
- Markers (A, B, C, D, E, F) red circles at end of layers
- Color: manual=red-light, tool-ext=yellow-light, paper=orange-light

Take screenshot for visual record (DevTools `Capture node screenshot` on `.panel.sebelum`).

- [ ] **Step 4: Commit**

```bash
git add "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html"
git commit -m "feat(pcp-slide8): build Sebelum panel + 5 layer + no-buffer spacer"
```

---

## Task 3: Build Panel SESUDAH (Right) with Buffer Zone Hero

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`

- [ ] **Step 1: Add buffer zone CSS to `<style>`**

Append inside `<style>`:

```css
  .buffer-zone {
    height: 50px;
    background: var(--hub-grad);
    color: white;
    border-radius: 4px;
    margin: 4px 0;
    padding: 6px 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    box-shadow: 0 2px 6px rgba(0,85,140,.25);
    position: relative;
  }
  .buffer-zone .hub-label {
    font-size: var(--fs-base);
    font-weight: 700;
    text-align: center;
  }
  .buffer-zone .marker {
    position: absolute;
    top: 4px;
    right: 6px;
  }
```

- [ ] **Step 2: Add Sesudah panel HTML after Sebelum `</section>`**

Append inside `#diagram-row` after the existing Sebelum `</section>`:

```html
  <section class="panel sesudah">
    <h2 class="panel-title">✅ SESUDAH (Konsep HC Portal)</h2>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">👔</span>
        <span class="actor">Manajemen</span>
        <span class="lv">Lv 5 · Strategic</span>
      </div>
      <div class="layer-content">
        <span class="comp portal">📈 Analytics Dashboard</span>
        <span class="comp portal">🔥 Heatmap Gap</span>
        <span class="comp portal">📤 Export Excel/PDF</span>
        <span class="marker improvement">1</span>
      </div>
    </div>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">👤</span>
        <span class="actor">HC</span>
        <span class="lv">Lv 4 · Governance</span>
      </div>
      <div class="layer-content">
        <span class="comp portal">👥 Kelola Pekerja</span>
        <span class="comp portal">🎯 PROTON IDP</span>
        <span class="comp portal">📝 Paket Assessment</span>
        <span class="comp portal">📊 Kelola KKJ</span>
        <span class="comp portal">🔄 Renewal Cert</span>
        <span class="comp portal">🔍 Audit Log</span>
        <span class="marker improvement">2</span>
        <span class="marker improvement">3</span>
      </div>
    </div>

    <div class="buffer-zone">
      <div class="hub-label">🛡️ BUFFER ZONE — 🌐 HC PORTAL · Single Source of Truth</div>
      <span class="marker improvement">4</span>
    </div>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">🏢</span>
        <span class="actor">Atasan</span>
        <span class="lv">Lv 3 · Supervisory</span>
      </div>
      <div class="layer-content">
        <span class="comp portal">👀 Records Team</span>
        <span class="comp portal">✅ Approval Deliverable</span>
        <span class="comp portal">📊 View Matriks KKJ</span>
        <span class="marker improvement">5</span>
      </div>
    </div>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">🧑‍🏫</span>
        <span class="actor">Coach</span>
        <span class="lv">Lv 2 · Coaching</span>
      </div>
      <div class="layer-content">
        <span class="comp portal">🎯 Coaching PROTON (5 fase)</span>
        <span class="comp portal">📎 Upload Evidence</span>
        <span class="comp portal">📜 Histori PROTON</span>
        <span class="marker improvement">6</span>
      </div>
    </div>

    <div class="layer">
      <div class="layer-label">
        <span class="icon">👷</span>
        <span class="actor">Pekerja</span>
        <span class="lv">Lv 1 · Operational</span>
      </div>
      <div class="layer-content">
        <span class="comp portal">📝 Assessment Online</span>
        <span class="comp portal">📋 Plan IDP</span>
        <span class="comp portal">🏆 Certificate Download</span>
        <span class="comp portal">🔔 Notifikasi In-App</span>
        <span class="marker improvement">7</span>
      </div>
    </div>
  </section>
```

- [ ] **Step 3: Reload Chrome, verify Sesudah panel renders**

Refresh browser. Expected:
- Right panel appears, border-top green
- Title "✅ SESUDAH (Konsep HC Portal)" blue bold
- 5 layer rows + 1 gradient buffer zone hero (blue→green) between L4 and L3
- Buffer zone label "🛡️ BUFFER ZONE — 🌐 HC PORTAL · Single Source of Truth" white text center
- Green marker (4) top-right of buffer zone
- All comp boxes blue (`.portal` class)
- Markers 1-7 green circles at end of layers

- [ ] **Step 4: Commit**

```bash
git add "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html"
git commit -m "feat(pcp-slide8): build Sesudah panel + buffer zone hero gradient"
```

---

## Task 4: Verify Symmetry + Dimension

**Files:**
- Verify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`

- [ ] **Step 1: Open Chrome DevTools, verify body width = 1100px**

In DevTools Console:

```javascript
document.body.getBoundingClientRect()
```

Expected: `width: 1100`, height somewhere ~420-480px. If width drifts, check for unintended margin/padding on `<html>` or `<body>`.

- [ ] **Step 2: Verify panel symmetry**

In DevTools Console:

```javascript
const s = document.querySelector('.panel.sebelum').getBoundingClientRect();
const r = document.querySelector('.panel.sesudah').getBoundingClientRect();
console.log('Sebelum:', s.width, s.height);
console.log('Sesudah:', r.width, r.height);
console.log('Height diff:', Math.abs(s.height - r.height));
```

Expected: both panels width 545. Height diff ≤ 4px (small acceptable due to font metric). If diff > 8px, adjust `.no-buffer-slot` or `.buffer-zone` height in CSS to match.

- [ ] **Step 3: Visual sweep — color + readability check**

Open file fullscreen in Chrome (F11). Confirm:
- Sebelum comp box colors: red-light, yellow-light, orange-light (mixed per row)
- Sesudah comp box colors: all blue-light (`.portal`)
- Layer label "Manajemen / HC / Atasan / Coach / Pekerja" + "Lv N · Subname" visible left
- Markers visible: 6 issue letters (A-F) in Sebelum, 7 improvement numbers (1-7) in Sesudah
- No comp box overflow / text cut
- No layer collapse (each ≥ 48px tall)

If L4 HC overflows (6 box + 2 marker), reduce comp box font to 0.5rem or shorten label text further.

- [ ] **Step 4: No commit** (verification only — fixes that arise here piggyback on Task 5 if needed)

---

## Task 5: Test Export PNG Workflow

**Files:**
- Test: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html`
- Output: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/exports/versi-p-slide8.png`

- [ ] **Step 1: Click Export PNG button in Chrome**

In open Chrome tab, click `📸 Export PNG` button top-right.

Expected: browser downloads `versi-p-slide8.png` to default Downloads folder.

- [ ] **Step 2: Verify PNG dimension + visual fidelity**

Open downloaded PNG. Expected:
- Dimension: **2200 × ~880 px** (2× scale of 1100 × ~440)
- White background (not transparent)
- Both panels rendered identically to browser
- Buffer zone gradient preserved
- No clipping at edges
- No export button visible (hidden during capture)

If dimension wrong: check `scale: 2` in `exportPNG()`. If background black: check `backgroundColor: '#ffffff'`.

- [ ] **Step 3: Move PNG to exports subfolder**

Create `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/exports/` and move:

```bash
mkdir -p "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/exports"
mv "$HOME/Downloads/versi-p-slide8.png" "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/exports/versi-p-slide8.png"
```

(On Windows: `mv` is bash-aliased; adjust path if Downloads is on a different drive.)

- [ ] **Step 4: Open Risalah Web.pptx and insert PNG into slide 8**

Manual step:
1. Open `docs/pcp-HCPortal-2026/Risalah Web.pptx` in PowerPoint
2. Navigate to slide 8
3. Right-click the white box labeled `GAMBAR DESAIN OLEH RINO` → delete placeholder text
4. Insert → Pictures → select `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/exports/versi-p-slide8.png`
5. Resize image to fit the box (Shift+drag corners to maintain aspect ratio)
6. Save pptx (keeps `.pptx` format, do NOT convert)

Verify in PPT: image readable, no distortion, labels legible at slide-show fullscreen.

- [ ] **Step 5: Commit HTML + PNG + pptx**

```bash
git add "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/" "docs/pcp-HCPortal-2026/Risalah Web.pptx"
git commit -m "feat(pcp-slide8): export versi-p-compact PNG + insert into Risalah slide 8"
```

---

## Task 6: Tag + Document Ship

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md` (append changelog)

- [ ] **Step 1: Add changelog entry to README**

Edit `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md`. Find the `## Changelog` section and prepend a new entry above `### v3.7 — Polish Pass (2026-05-22)`:

```markdown
### v3.7-slide8 — Versi P Compact for Risalah Web slide 8 (2026-05-22)

- New file: `slide8/versi-p-compact.html` (1100×440 landscape, side-by-side Sebelum+Sesudah)
- Dropped chrome (header/toolbar/komparasi/legend tables) for PNG export
- Buffer zone Sesudah ↔ no-buffer slot Sebelum for symmetric row heights
- Tech stack subtitle removed from buffer zone (will land in placeholder #3 Standard Design spec)
- Export PNG button via html2canvas @2x scale
- Master `versi-p-workflow-topology.html` v3.7 untouched
- Inserted into `docs/pcp-HCPortal-2026/Risalah Web.pptx` slide 8 placeholder #1
- Spec: `docs/superpowers/specs/2026-05-22-pcp-slide8-versi-p-compact-design.md`
- Plan: `docs/superpowers/plans/2026-05-22-pcp-slide8-versi-p-compact-implementation.md`
```

- [ ] **Step 2: Commit changelog + tag**

```bash
git add "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md"
git commit -m "docs(pcp-3.4): changelog v3.7-slide8 — versi P compact for Risalah"
git tag pcp-hcportal-3.4-slide8-v1.0
```

- [ ] **Step 3: Verify tag created**

```bash
git tag --list "pcp-hcportal-3.4-slide8-*"
```

Expected output: `pcp-hcportal-3.4-slide8-v1.0`

- [ ] **Step 4: Notify Team IT** (manual, no code)

Send commit hash + tag to Team IT per CLAUDE.md DEV_WORKFLOW. Note: no DB migration involved (docs/pptx only).

---

## Verification Checklist (Definition of Done)

After all 6 tasks complete:

- [ ] File `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html` exists
- [ ] HTML body width is 1100px (DevTools verified)
- [ ] Both panels side-by-side, 545px each, gap 8px
- [ ] Sebelum panel: 5 layer rows + 1 no-buffer dashed spacer slot
- [ ] Sesudah panel: 5 layer rows + 1 gradient buffer zone hero
- [ ] Panel height diff ≤ 4px (symmetric)
- [ ] Markers visible: 6 issue letters (A-F) in Sebelum + 7 improvement numbers (1-7) in Sesudah
- [ ] Comp box color coding correct (manual/tool-ext/paper/portal)
- [ ] No console errors in Chrome
- [ ] Export PNG button produces 2200×880 PNG with white background
- [ ] PNG inserted into Risalah Web.pptx slide 8 GAMBAR DESAIN box
- [ ] Master `versi-p-workflow-topology.html` (v3.7) unchanged (`git log --oneline -- docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html` shows no commits from this plan)
- [ ] Changelog entry added to `3.4-solusi-terpilih/README.md`
- [ ] Tag `pcp-hcportal-3.4-slide8-v1.0` created

---

## Out of Scope (Confirmed)

- Modifying master `versi-p-workflow-topology.html` v3.7
- Placeholder #2 (Rencana Pembuatan) — separate plan
- Placeholder #3 (Standard Design) — separate plan
- Promoting pptx to Dev/Prod server (Team IT responsibility per CLAUDE.md)
- Print stylesheet A3 (master v3.7 already covers print)
- Responsive breakpoints (fixed 1100px)

---

## Recovery

If implementation diverges and need to restart:

```bash
git reset --hard <commit-before-task-1>
# OR keep file but reset content:
git checkout HEAD -- "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/slide8/versi-p-compact.html"
```

Master Versi P at any time recoverable via tag: `git checkout pcp-hcportal-3.4-v3.7 -- versi-p-workflow-topology.html`
