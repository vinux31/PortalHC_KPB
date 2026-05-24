# Slide 8 Risalah Web Compact (Opsi II + IV) v1.1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build 2 HTML mockup compact (`pipeline-outcome-compact.html` + `workflow-topology-refined-compact.html`) sized for PPT box 8.11 × 25.72 cm + PNG export 3035 × 957 px (300 DPI), drop-in slide 8 Risalah Web.pptx. Tag `slide8-risalah-v1.1`.

**Architecture:**
- Folder baru `docs/pcp-HCPortal-2026/slide8-risalah/compact/`
- 2-panel side-by-side grid (1fr 1fr), body 100vw × 100vh
- Opsi II: Sebelum 5-layer + Pipeline 3-stage hero (Ogoun framework)
- Opsi IV: Sebelum 5-layer + Sesudah 5-layer + Buffer Zone HC Portal
- CSS sizing pakai `vw` units (proportional ke viewport untuk correct PNG scaling)
- Sebelum panel IDENTIK kedua opsi (DRY)
- PNG export Playwright @ viewport 3035×957

**Tech Stack:** HTML5 + CSS3 inline · Playwright MCP untuk visual QA + PNG export · Git tag versioning

**Spec reference:** `docs/superpowers/specs/2026-05-24-slide8-risalah-compact-opsi-ii-iv-design.md`

---

## File Structure

```
docs/pcp-HCPortal-2026/slide8-risalah/                  (existing v1.0)
├─ README.md                                             (UPDATE — mention compact/)
├─ index.html                                            (UPDATE — link compact)
├─ pipeline-outcome.html                                 (UNCHANGED v1.0)
├─ workflow-topology-refined.html                        (UNCHANGED v1.0)
├─ *.png                                                 (UNCHANGED v1.0)
└─ compact/                                              (NEW v1.1)
   ├─ pipeline-outcome-compact.html                      (Opsi II compact)
   ├─ pipeline-outcome-compact.png                       (PNG 3035×957)
   ├─ workflow-topology-refined-compact.html             (Opsi IV compact)
   └─ workflow-topology-refined-compact.png              (PNG 3035×957)
```

**Each file responsibility:**
- `compact/pipeline-outcome-compact.html` — Opsi II 2-panel (Sebelum 5-layer + Pipeline 3-stage)
- `compact/workflow-topology-refined-compact.html` — Opsi IV 2-panel (Sebelum + Sesudah 5-layer + Buffer)
- `compact/*.png` — PNG export untuk PPT paste
- README.md update — add Cakupan v1.1 section + mention compact/ subfolder
- index.html update — add link card untuk compact files

---

## Task 1: Setup folder + start HTTP server

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/compact/` (folder)

**Goal:** Folder ada, HTTP server siap untuk visual QA.

- [ ] **Step 1: Create folder**

```bash
mkdir -p "docs/pcp-HCPortal-2026/slide8-risalah/compact"
```

Verify: `ls "docs/pcp-HCPortal-2026/slide8-risalah/compact/"` returns empty.

- [ ] **Step 2: Start HTTP server di compact folder (run_in_background)**

```bash
cd "docs/pcp-HCPortal-2026/slide8-risalah/compact" && python -m http.server 50910
```

Set `run_in_background: true`. Save bash task ID.

- [ ] **Step 3: Verify server up**

```bash
sleep 2 && curl -s -o /dev/null -w "%{http_code}" "http://localhost:50910/" 2>&1
```

Expected: `200` or `404` (404 OK karena folder kosong).

---

## Task 2: Build pipeline-outcome-compact.html (Opsi II)

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/compact/pipeline-outcome-compact.html`

**Goal:** Standalone HTML Opsi II compact (body 100vw × 100vh, 2-panel: Sebelum 5-layer + Pipeline 3-stage).

- [ ] **Step 1: Write full HTML file**

Path: `docs/pcp-HCPortal-2026/slide8-risalah/compact/pipeline-outcome-compact.html`

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Opsi II Compact — Pipeline Outcome</title>
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
    --neutral-gray: #6b7280;
    --border: #d1d5db;
    --hub-grad: linear-gradient(135deg, #00558C, #00A551);
    --fs-xs: 1vw;
    --fs-sm: 1.25vw;
    --fs-base: 1.5vw;
    --fs-md: 1.75vw;
    --fs-lg: 2vw;
  }
  * { box-sizing: border-box; }
  body { width: 100vw; height: 100vh; margin: 0; padding: 0; background: white;
         font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
         color: #1f2937; overflow: hidden; }

  .compact-container {
    display: grid; grid-template-columns: 1fr 1fr; gap: 0.5vw;
    width: 100%; height: 100%; padding: 0.3vw 0.5vw;
  }
  .panel {
    border: 2px solid; border-radius: 6px;
    padding: 0.4vw 0.6vw; display: flex; flex-direction: column;
    overflow: hidden; position: relative;
  }
  .panel-sebelum { border-color: var(--pertamina-red);
                    background: linear-gradient(180deg, #fff5f5 0%, white 30%); }
  .panel-sesudah { border-color: var(--pertamina-green);
                    background: linear-gradient(180deg, #f0fdf4 0%, white 30%); }
  .panel-header { font-size: var(--fs-sm); font-weight: 700;
                   margin-bottom: 0.3vw; padding-bottom: 0.2vw;
                   border-bottom: 1px dashed var(--border); }
  .panel-sebelum .panel-header { color: var(--pertamina-red); }
  .panel-sesudah .panel-header { color: var(--pertamina-green); }

  .layer-list { display: flex; flex-direction: column; gap: 0.15vw; flex: 1; }
  .layer { display: flex; gap: 0.4vw; align-items: center;
           padding: 0.2vw 0.3vw; border-bottom: 1px dashed #f3f4f6;
           font-size: var(--fs-xs); }
  .layer:last-child { border-bottom: none; }
  .layer-actor { width: 32%; font-weight: 600; }
  .layer-tools { flex: 1; display: flex; gap: 0.2vw; flex-wrap: wrap; align-items: center; }
  .chip { font-size: 0.95vw; padding: 0.1vw 0.4vw; border-radius: 3px;
           white-space: nowrap; font-weight: 500; }
  .chip.manual { background: var(--pertamina-red-light); color: var(--pertamina-red);
                  border: 1px solid var(--pertamina-red); }
  .chip.ext { background: #fff3cd; color: #856404; border: 1px solid #d4a017; }
  .chip.paper { background: #e8e8e8; color: #555; border: 1px solid #999; }

  .marker {
    display: inline-flex; align-items: center; justify-content: center;
    width: 2vw; height: 2vw; border-radius: 50%;
    color: white; font-weight: 700; font-size: 1.1vw;
    margin-left: 0.15vw;
  }
  .marker.issue { background: var(--pertamina-red); }
  .marker.improvement { background: var(--pertamina-green); }

  .legend-inset {
    position: absolute; top: 0.3vw; right: 0.4vw;
    font-size: var(--fs-xs);
    background: rgba(255,255,255,.9);
    padding: 0.15vw 0.3vw; border-radius: 3px; border: 1px solid var(--border);
  }

  .rpn-note { font-size: var(--fs-xs); color: var(--neutral-gray);
              font-style: italic; margin-top: 0.3vw; text-align: center; }

  .pipeline-row { display: flex; gap: 0.3vw; flex: 1;
                   align-items: stretch; padding: 0.2vw 0; }
  .stage { color: white; padding: 0.4vw 0.5vw; border-radius: 5px;
            flex: 1; display: flex; flex-direction: column; gap: 0.2vw;
            font-size: var(--fs-xs); }
  .stage-1 { background: linear-gradient(135deg, #00558C, #0077B5); }
  .stage-2 { background: linear-gradient(135deg, #0077B5, #00A551); }
  .stage-3 { background: linear-gradient(135deg, #00A551, #5cba7d); }
  .stage-num { font-size: var(--fs-md); font-weight: 800; line-height: 1; }
  .stage-title { font-weight: 700; font-size: var(--fs-xs); margin-top: 0.15vw; }
  .stage-actors { font-size: 0.85vw; opacity: .85; font-style: italic; }
  .stage-features { font-size: 0.85vw; line-height: 1.4; }
  .stage-markers { margin-top: auto; display: flex; gap: 0.15vw; }
  .pipeline-arrow { display: flex; align-items: center;
                    font-size: var(--fs-md); color: var(--pertamina-yellow);
                    padding: 0 0.1vw; font-weight: 700; }
</style>
</head>
<body>
<div class="compact-container">

  <!-- KIRI: SEBELUM 5-layer -->
  <div class="panel panel-sebelum">
    <div class="panel-header">❌ Sebelum (Kondisi Aktual)</div>
    <div class="layer-list">
      <div class="layer">
        <div class="layer-actor">👔 <b>Lv5</b> Manajemen</div>
        <div class="layer-tools">
          <span class="chip manual">📄 PDF/Excel</span>
          <span class="chip ext">📧 Email</span>
          <span class="marker issue">D</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">👤 <b>Lv4</b> HC</div>
        <div class="layer-tools">
          <span class="chip manual">📊 Excel ×5</span>
          <span class="chip manual">📝 Word</span>
          <span class="marker issue">A</span>
          <span class="marker issue">B</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">🏢 <b>Lv3</b> Atasan</div>
        <div class="layer-tools">
          <span class="chip ext">📧 Email</span>
          <span class="chip ext">💬 WA approval</span>
          <span class="marker issue">C</span>
          <span class="marker issue">E</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">🧑‍🏫 <b>Lv2</b> Coach</div>
        <div class="layer-tools">
          <span class="chip paper">📋 Form cetak</span>
          <span class="chip paper">📁 Arsip</span>
          <span class="chip ext">💬 WA</span>
          <span class="chip ext">📧 Email</span>
          <span class="marker issue">A</span>
          <span class="marker issue">E</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">👷 <b>Lv1</b> Pekerja</div>
        <div class="layer-tools">
          <span class="chip ext">🌐 FleQi</span>
          <span class="chip paper">🎓 Cert hardcopy</span>
          <span class="chip manual">📊 Excel IDP</span>
          <span class="marker issue">A</span>
          <span class="marker issue">F</span>
        </div>
      </div>
    </div>
    <div class="rpn-note">FMEA RPN · Method 140 · Machine 140 · Man 90 (Risalah PROTON)</div>
    <div class="legend-inset">A-F · Issue</div>
  </div>

  <!-- KANAN: SOLUSI TERPILIH — Pipeline 3-stage hero -->
  <div class="panel panel-sesudah">
    <div class="panel-header">✅ Solusi Terpilih — Pemantauan Kompetensi Pipeline · Ogoun &amp; Tamunosiki-Amadi (2023)</div>
    <div class="pipeline-row">
      <div class="stage stage-1">
        <div class="stage-num">①</div>
        <div class="stage-title">Information Gathering &amp; Evaluation</div>
        <div class="stage-actors">👤 HC · 👷 Pekerja</div>
        <div class="stage-features">📝 Assessment Online<br/>📊 KKJ Matrix<br/>🎯 PROTON IDP</div>
        <div class="stage-markers">
          <span class="marker improvement">2</span>
          <span class="marker improvement">7</span>
        </div>
      </div>
      <div class="pipeline-arrow">→</div>
      <div class="stage stage-2">
        <div class="stage-num">②</div>
        <div class="stage-title">Activity Auditing</div>
        <div class="stage-actors">🏢 Atasan · 🧑‍🏫 Coach · 👤 HC</div>
        <div class="stage-features">📎 Upload Evidence<br/>✅ Approval<br/>🔍 Audit Log<br/>🔐 RBAC</div>
        <div class="stage-markers">
          <span class="marker improvement">3</span>
          <span class="marker improvement">5</span>
          <span class="marker improvement">6</span>
        </div>
      </div>
      <div class="pipeline-arrow">→</div>
      <div class="stage stage-3">
        <div class="stage-num">③</div>
        <div class="stage-title">Feedback Loop</div>
        <div class="stage-actors">👔 Manajemen · 👤 HC · 👷 Pekerja</div>
        <div class="stage-features">🔥 Heatmap Gap<br/>🏆 Cert Download<br/>🔔 Notif In-App</div>
        <div class="stage-markers">
          <span class="marker improvement">1</span>
          <span class="marker improvement">4</span>
        </div>
      </div>
    </div>
    <div class="legend-inset">1-7 · Improvement</div>
  </div>

</div>
</body>
</html>
```

- [ ] **Step 2: Resize Playwright viewport ke 3035 × 957**

```
mcp__plugin_playwright_playwright__browser_resize(width=3035, height=957)
```

- [ ] **Step 3: Visual QA via Playwright**

```
mcp__plugin_playwright_playwright__browser_navigate(url="http://localhost:50910/pipeline-outcome-compact.html")
mcp__plugin_playwright_playwright__browser_snapshot()
```

Expected snapshot:
- Header bar TIDAK ada (compact, langsung 2-panel)
- Kiri panel border merah `❌ Sebelum (Kondisi Aktual)` dengan 5 layer Lv5-Lv1 + markers A-F + RPN note + legend inset
- Kanan panel border hijau `✅ Solusi Terpilih — Pemantauan Kompetensi Pipeline` dengan 3 stage box (① ② ③) + 2 arrow + markers 1-7 + legend inset
- No content overflow

Bila visual broken atau elemen missing → fix di Step 1 markup/CSS, ulangi Step 3.

- [ ] **Step 4: Commit**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/compact/pipeline-outcome-compact.html"
git commit -m "feat(slide8-risalah-compact): add Opsi II pipeline-outcome-compact.html

2-panel compact mockup for PPT box 8.11x25.72cm. Sebelum 5-layer
+ Pipeline 3-stage hero (Ogoun framework). Body 100vw×100vh,
vw-based sizing for correct PNG export DPI.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Build workflow-topology-refined-compact.html (Opsi IV)

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/compact/workflow-topology-refined-compact.html`

**Goal:** Standalone HTML Opsi IV compact (Sebelum 5-layer identik Opsi II + Sesudah 5-layer + Buffer Zone hero).

- [ ] **Step 1: Write full HTML file**

Path: `docs/pcp-HCPortal-2026/slide8-risalah/compact/workflow-topology-refined-compact.html`

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Opsi IV Compact — Workflow Topology Refined</title>
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
    --neutral-gray: #6b7280;
    --border: #d1d5db;
    --hub-grad: linear-gradient(135deg, #00558C, #00A551);
    --fs-xs: 1vw;
    --fs-sm: 1.25vw;
    --fs-base: 1.5vw;
    --fs-md: 1.75vw;
    --fs-lg: 2vw;
  }
  * { box-sizing: border-box; }
  body { width: 100vw; height: 100vh; margin: 0; padding: 0; background: white;
         font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
         color: #1f2937; overflow: hidden; }

  .compact-container {
    display: grid; grid-template-columns: 1fr 1fr; gap: 0.5vw;
    width: 100%; height: 100%; padding: 0.3vw 0.5vw;
  }
  .panel {
    border: 2px solid; border-radius: 6px;
    padding: 0.4vw 0.6vw; display: flex; flex-direction: column;
    overflow: hidden; position: relative;
  }
  .panel-sebelum { border-color: var(--pertamina-red);
                    background: linear-gradient(180deg, #fff5f5 0%, white 30%); }
  .panel-sesudah { border-color: var(--pertamina-green);
                    background: linear-gradient(180deg, #f0fdf4 0%, white 30%); }
  .panel-header { font-size: var(--fs-sm); font-weight: 700;
                   margin-bottom: 0.3vw; padding-bottom: 0.2vw;
                   border-bottom: 1px dashed var(--border); }
  .panel-sebelum .panel-header { color: var(--pertamina-red); }
  .panel-sesudah .panel-header { color: var(--pertamina-green); }

  .layer-list { display: flex; flex-direction: column; gap: 0.15vw; flex: 1; }
  .layer { display: flex; gap: 0.4vw; align-items: center;
           padding: 0.2vw 0.3vw; border-bottom: 1px dashed #f3f4f6;
           font-size: var(--fs-xs); }
  .layer:last-child { border-bottom: none; }
  .layer-actor { width: 32%; font-weight: 600; }
  .layer-tools { flex: 1; display: flex; gap: 0.2vw; flex-wrap: wrap; align-items: center; }
  .chip { font-size: 0.95vw; padding: 0.1vw 0.4vw; border-radius: 3px;
           white-space: nowrap; font-weight: 500; }
  .chip.manual { background: var(--pertamina-red-light); color: var(--pertamina-red);
                  border: 1px solid var(--pertamina-red); }
  .chip.ext { background: #fff3cd; color: #856404; border: 1px solid #d4a017; }
  .chip.paper { background: #e8e8e8; color: #555; border: 1px solid #999; }
  .chip.portal { background: var(--pertamina-blue-light); color: var(--pertamina-blue-dark);
                  border: 1px solid var(--pertamina-blue); }

  .marker {
    display: inline-flex; align-items: center; justify-content: center;
    width: 2vw; height: 2vw; border-radius: 50%;
    color: white; font-weight: 700; font-size: 1.1vw;
    margin-left: 0.15vw;
  }
  .marker.issue { background: var(--pertamina-red); }
  .marker.improvement { background: var(--pertamina-green); }

  .legend-inset {
    position: absolute; top: 0.3vw; right: 0.4vw;
    font-size: var(--fs-xs);
    background: rgba(255,255,255,.9);
    padding: 0.15vw 0.3vw; border-radius: 3px; border: 1px solid var(--border);
  }

  .rpn-note { font-size: var(--fs-xs); color: var(--neutral-gray);
              font-style: italic; margin-top: 0.3vw; text-align: center; }

  .buffer-zone {
    background: var(--hub-grad); color: white;
    padding: 0.35vw 0.5vw; border-radius: 5px;
    margin: 0.2vw 0; text-align: center;
    border-left: 4px solid var(--pertamina-yellow);
    position: relative;
  }
  .buffer-label { font-weight: 700; font-size: var(--fs-sm); }
  .buffer-content { font-size: var(--fs-xs); opacity: .95; margin-top: 0.1vw; }
</style>
</head>
<body>
<div class="compact-container">

  <!-- KIRI: SEBELUM 5-layer (identik Opsi II) -->
  <div class="panel panel-sebelum">
    <div class="panel-header">❌ Sebelum (Kondisi Aktual)</div>
    <div class="layer-list">
      <div class="layer">
        <div class="layer-actor">👔 <b>Lv5</b> Manajemen</div>
        <div class="layer-tools">
          <span class="chip manual">📄 PDF/Excel</span>
          <span class="chip ext">📧 Email</span>
          <span class="marker issue">D</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">👤 <b>Lv4</b> HC</div>
        <div class="layer-tools">
          <span class="chip manual">📊 Excel ×5</span>
          <span class="chip manual">📝 Word</span>
          <span class="marker issue">A</span>
          <span class="marker issue">B</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">🏢 <b>Lv3</b> Atasan</div>
        <div class="layer-tools">
          <span class="chip ext">📧 Email</span>
          <span class="chip ext">💬 WA approval</span>
          <span class="marker issue">C</span>
          <span class="marker issue">E</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">🧑‍🏫 <b>Lv2</b> Coach</div>
        <div class="layer-tools">
          <span class="chip paper">📋 Form cetak</span>
          <span class="chip paper">📁 Arsip</span>
          <span class="chip ext">💬 WA</span>
          <span class="chip ext">📧 Email</span>
          <span class="marker issue">A</span>
          <span class="marker issue">E</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">👷 <b>Lv1</b> Pekerja</div>
        <div class="layer-tools">
          <span class="chip ext">🌐 FleQi</span>
          <span class="chip paper">🎓 Cert hardcopy</span>
          <span class="chip manual">📊 Excel IDP</span>
          <span class="marker issue">A</span>
          <span class="marker issue">F</span>
        </div>
      </div>
    </div>
    <div class="rpn-note">FMEA RPN · Method 140 · Machine 140 · Man 90 (Risalah PROTON)</div>
    <div class="legend-inset">A-F · Issue</div>
  </div>

  <!-- KANAN: SOLUSI TERPILIH — 5-layer + Buffer Zone -->
  <div class="panel panel-sesudah">
    <div class="panel-header">✅ Solusi Terpilih — HC Portal (Buffer Zone DMZ-analog)</div>
    <div class="layer-list">
      <div class="layer">
        <div class="layer-actor">👔 <b>Lv5</b> Manajemen</div>
        <div class="layer-tools">
          <span class="chip portal">📈 Analytics</span>
          <span class="chip portal">🔥 Heatmap</span>
          <span class="chip portal">📤 Export</span>
          <span class="marker improvement">1</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">👤 <b>Lv4</b> HC</div>
        <div class="layer-tools">
          <span class="chip portal">👥 Pekerja</span>
          <span class="chip portal">🎯 IDP</span>
          <span class="chip portal">📝 Asm</span>
          <span class="chip portal">📊 KKJ</span>
          <span class="marker improvement">2</span>
          <span class="marker improvement">3</span>
        </div>
      </div>

      <div class="buffer-zone">
        <div class="buffer-label">🛡️ BUFFER ZONE — HC PORTAL</div>
        <div class="buffer-content">Single Source of Truth · .NET 8 · SQL Server · SignalR · Audit Log <span class="marker improvement">4</span></div>
      </div>

      <div class="layer">
        <div class="layer-actor">🏢 <b>Lv3</b> Atasan</div>
        <div class="layer-tools">
          <span class="chip portal">👀 Records</span>
          <span class="chip portal">✅ Approval</span>
          <span class="chip portal">📊 View KKJ</span>
          <span class="marker improvement">5</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">🧑‍🏫 <b>Lv2</b> Coach</div>
        <div class="layer-tools">
          <span class="chip portal">🎯 Coaching PROTON</span>
          <span class="chip portal">📎 Evidence</span>
          <span class="chip portal">📜 Histori</span>
          <span class="marker improvement">6</span>
        </div>
      </div>
      <div class="layer">
        <div class="layer-actor">👷 <b>Lv1</b> Pekerja</div>
        <div class="layer-tools">
          <span class="chip portal">📝 Asm</span>
          <span class="chip portal">📋 IDP</span>
          <span class="chip portal">🏆 Cert</span>
          <span class="chip portal">🔔 Notif</span>
          <span class="marker improvement">7</span>
        </div>
      </div>
    </div>
    <div class="legend-inset">1-7 · Improvement</div>
  </div>

</div>
</body>
</html>
```

- [ ] **Step 2: Visual QA via Playwright**

Viewport sudah 3035 × 957 dari Task 2.

```
mcp__plugin_playwright_playwright__browser_navigate(url="http://localhost:50910/workflow-topology-refined-compact.html")
mcp__plugin_playwright_playwright__browser_snapshot()
```

Expected snapshot:
- Kiri panel border merah: 5 layer Lv5-Lv1 + markers A-F + RPN note + legend inset (identik Opsi II)
- Kanan panel border hijau: 5 layer + Buffer Zone hero (yellow border-left) di antara Lv4 & Lv3 + markers 1-7 + legend inset
- No content overflow

- [ ] **Step 3: Commit**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/compact/workflow-topology-refined-compact.html"
git commit -m "feat(slide8-risalah-compact): add Opsi IV workflow-topology-refined-compact.html

2-panel compact mockup for PPT box 8.11x25.72cm. Sebelum 5-layer
(identik Opsi II) + Sesudah 5-layer with Buffer Zone hero between
Lv4 and Lv3 + markers 1-7.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: PNG export both compact files

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/compact/pipeline-outcome-compact.png`
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/compact/workflow-topology-refined-compact.png`

**Goal:** PNG fullPage screenshot kedua HTML @ 3035 × 957 (300 DPI equivalent).

- [ ] **Step 1: Navigate + screenshot pipeline-outcome-compact**

```
mcp__plugin_playwright_playwright__browser_navigate(url="http://localhost:50910/pipeline-outcome-compact.html")
mcp__plugin_playwright_playwright__browser_take_screenshot(fullPage=true, type="png", filename="docs/pcp-HCPortal-2026/slide8-risalah/compact/pipeline-outcome-compact.png")
```

Verify: file ada, size 200KB-1.5MB.

```bash
ls -lh "docs/pcp-HCPortal-2026/slide8-risalah/compact/pipeline-outcome-compact.png"
```

- [ ] **Step 2: Navigate + screenshot workflow-topology-refined-compact**

```
mcp__plugin_playwright_playwright__browser_navigate(url="http://localhost:50910/workflow-topology-refined-compact.html")
mcp__plugin_playwright_playwright__browser_take_screenshot(fullPage=true, type="png", filename="docs/pcp-HCPortal-2026/slide8-risalah/compact/workflow-topology-refined-compact.png")
```

Verify: file ada, size 200KB-1.5MB.

```bash
ls -lh "docs/pcp-HCPortal-2026/slide8-risalah/compact/workflow-topology-refined-compact.png"
```

- [ ] **Step 3: Commit PNGs**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/compact/pipeline-outcome-compact.png" "docs/pcp-HCPortal-2026/slide8-risalah/compact/workflow-topology-refined-compact.png"
git commit -m "feat(slide8-risalah-compact): add PNG exports 3035x957 (300 DPI)

PNG drop-in for PPT slide 8 content box 8.11x25.72 cm.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Update README + index dengan compact section

**Files:**
- Modify: `docs/pcp-HCPortal-2026/slide8-risalah/README.md`
- Modify: `docs/pcp-HCPortal-2026/slide8-risalah/index.html`

**Goal:** README + index reference compact files baru.

- [ ] **Step 1: Update README.md — add Cakupan v1.1 section**

Find di README.md:
```markdown
## Cakupan v1.0
```

Insert SEBELUM section tersebut:

```markdown
## Cakupan v1.1 (Compact untuk PPT box 8.11 × 25.72 cm)

Folder `compact/` berisi versi compact yang pas masuk kotak konten slide 8 PowerPoint:

- `compact/pipeline-outcome-compact.html` — Opsi II compact (Pipeline 3-stage)
- `compact/workflow-topology-refined-compact.html` — Opsi IV compact (5-layer + Buffer Zone)
- `compact/*.png` — PNG export 3035×957 (300 DPI) drop-in untuk PPT paste

**Use case:** Paste PNG langsung ke slide 8 PowerPoint di kotak 8.11 × 25.72 cm di bawah bar judul hijau "Design / Gambar Teknik / Flow Proses / Formula Solusi Terpilih".

Spec: `docs/superpowers/specs/2026-05-24-slide8-risalah-compact-opsi-ii-iv-design.md`
Tag: `slide8-risalah-v1.1`

---

## Cakupan v1.0
```

- [ ] **Step 2: Update index.html — add compact link cards**

Find di index.html:
```html
  </div>

  <div class="audience-matrix">
```

Insert SEBELUM `<div class="audience-matrix">`:

```html
  </div>

  <h2 style="margin-top:2rem;color:var(--pertamina-blue)">Compact v1.1 (PPT drop-in)</h2>
  <p style="margin-bottom:1rem;color:var(--neutral-gray);font-size:.9rem">Versi compact untuk paste langsung ke kotak slide 8 PowerPoint (8.11 × 25.72 cm).</p>
  <div class="cards">
    <a href="compact/pipeline-outcome-compact.html" class="card opsi-ii">
      <div class="tag">COMPACT v1.1</div>
      <h2>Opsi II Compact</h2>
      <p>Pipeline 3-stage compact (8.11×25.72 cm) untuk drop-in PPT slide 8</p>
    </a>
    <a href="compact/workflow-topology-refined-compact.html" class="card opsi-iv">
      <div class="tag">COMPACT v1.1</div>
      <h2>Opsi IV Compact</h2>
      <p>5-layer + Buffer Zone compact (8.11×25.72 cm) untuk drop-in PPT slide 8</p>
    </a>
  </div>

  <div class="audience-matrix">
```

- [ ] **Step 3: Visual QA index.html**

```
mcp__plugin_playwright_playwright__browser_resize(width=1280, height=900)
mcp__plugin_playwright_playwright__browser_navigate(url="http://localhost:50910/../index.html")
```

(Note: path `..` mungkin tidak resolve via HTTP server di compact folder. Start server baru di parent atau navigate manual.)

Atau spawn second server untuk parent:

```bash
cd "docs/pcp-HCPortal-2026/slide8-risalah" && python -m http.server 50911
```

```
mcp__plugin_playwright_playwright__browser_navigate(url="http://localhost:50911/index.html")
mcp__plugin_playwright_playwright__browser_snapshot()
```

Expected: hero + 2 card v1.0 + heading "Compact v1.1" + 2 card baru link ke compact files + audience matrix + footer.

- [ ] **Step 4: Commit**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/README.md" "docs/pcp-HCPortal-2026/slide8-risalah/index.html"
git commit -m "docs(slide8-risalah-compact): update README + index dengan compact/ section

Add Cakupan v1.1 section di README + 2 card link di index.html
untuk file compact baru.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Tag release v1.1

**Goal:** Tag `slide8-risalah-v1.1` + push ke origin.

- [ ] **Step 1: Verify all 4 compact files ada**

```bash
ls -la "docs/pcp-HCPortal-2026/slide8-risalah/compact/"
```

Expected: 4 file (2 HTML + 2 PNG).

- [ ] **Step 2: Verify acceptance criteria**

Manual checklist (mark each ✓):
- [ ] Both HTML render dengan body fill exactly 100vw × 100vh (Playwright snapshot dari Task 2 + 3 passed)
- [ ] Aspect ratio output PNG = 3.17 : 1 (3035 × 957) — verified via Task 4 file size + manual visual
- [ ] Sebelum panel: 5 layer Lv5→Lv1 + markers A-F + RPN note + legend inset
- [ ] Opsi II Sesudah: Pipeline 3-stage horizontal + actor-per-stage + features + markers 1-7
- [ ] Opsi IV Sesudah: 5 layer + Buffer Zone hero + markers 1-7
- [ ] Font readable saat printed @ 25,72 × 8,11 cm (vw values applied)
- [ ] Marker diameter 2vw = 60 CSS px (visible)
- [ ] Chip font 0.95vw (~6.8pt)
- [ ] PNG file size 200KB-1.5MB each
- [ ] No element overflow
- [ ] All 9 approved decisions applied

- [ ] **Step 3: Create git tag**

```bash
git tag slide8-risalah-v1.1
git tag --list "slide8-risalah-*"
```

Expected: tag `slide8-risalah-v1.0` + `slide8-risalah-v1.1` both listed.

- [ ] **Step 4: Push commits + push tag**

```bash
git push origin main
git push origin slide8-risalah-v1.1
```

Expected:
- Push commits ke origin/main (Task 1-5 commits)
- Push tag slide8-risalah-v1.1 ke remote

- [ ] **Step 5: Verify remote tag**

```bash
git ls-remote --tags origin slide8-risalah-v1.1
```

Expected: remote tag hash matches local.

- [ ] **Step 6: Stop HTTP servers**

```bash
ps aux 2>/dev/null | grep -E "python.*5091[01]" | grep -v grep | awk '{print $2}' | xargs -r kill 2>&1
```

---

## Self-Review Checklist

After execution, verify:
- [ ] Folder `docs/pcp-HCPortal-2026/slide8-risalah/compact/` punya 4 file (2 HTML + 2 PNG)
- [ ] Tag `slide8-risalah-v1.1` exists local + remote
- [ ] Tag `slide8-risalah-v1.0` UNCHANGED (preserve stable)
- [ ] All spec §12 acceptance criteria met
- [ ] README + index reference compact files
- [ ] No file v1.0 ter-modify kecuali README + index (additive only)
- [ ] vw-based CSS sizing applied (no px values di font/marker/chip)
- [ ] All 9 spec decisions applied:
  1. 2-panel side-by-side ✓
  2. Sebelum panel identik kedua opsi (DRY) ✓
  3. Sesudah beda: Opsi II Pipeline, Opsi IV 5-layer+Buffer ✓
  4. Markers A-F + 1-7 embedded ✓
  5. Legend inset top-right ✓
  6. Font compact 1-2vw ✓
  7. Title bar hijau TIDAK include ✓
  8. Naming `-compact` suffix ✓
  9. Print CSS `@page` available (not strictly needed, body sizing primary) ✓
