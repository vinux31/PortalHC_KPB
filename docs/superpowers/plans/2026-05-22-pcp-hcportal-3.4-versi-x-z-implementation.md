# PCP §3.4 Versi X + Versi Z Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build 2 additional landscape diagrams (Versi X BPMN Swimlane + Versi Z BPMN+VSM Hybrid) untuk §3.4 PCP SMART 2026 HC Portal, fokus hero workflow Assessment, dengan update index.html + README.md mereferensikan 4 versi total.

**Architecture:** Standalone HTML5 + inline CSS + inline SVG. No build, no JS framework. Bootstrap Icons CDN untuk icon. Match konvensi existing versi P/C (CSS variables Pertamina palette, max-width 1400px, header-bar 3-col grid, print-friendly @media print).

**Tech Stack:** HTML5, CSS3 (Grid + Flexbox + custom properties), SVG inline, Bootstrap Icons 1.11 CDN.

**Spec reference:** `docs/superpowers/specs/2026-05-22-pcp-hcportal-3.4-versi-x-z-design.md`

**Files map:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html`
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/index.html`
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md`

---

## Task 1: Scaffold Versi X HTML Skeleton + CSS Variables

**Files:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`

- [ ] **Step 1: Create file skeleton**

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>§3.4 v3.6 — Versi X: BPMN Swimlane As-Is/To-Be HC Portal</title>
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">
<style>
  :root {
    --pertamina-red: #C8102E;
    --pertamina-red-light: #fce8eb;
    --pertamina-red-bg: #FEE;
    --pertamina-blue: #00558C;
    --pertamina-blue-dark: #003D63;
    --pertamina-blue-light: #e6f0f7;
    --pertamina-blue-bg: #EEF;
    --pertamina-green: #00A551;
    --pertamina-green-light: #d4f0dd;
    --pertamina-yellow: #FFC72C;
    --neutral-gray: #6b7280;
    --neutral-light: #d1d5db;
    --bg: #f6f7fb;
    --hub-grad: linear-gradient(135deg, #00558C, #00A551);
  }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: #1f2937; padding: 1.5rem; }
</style>
</head>
<body>
<div class="container"></div>
</body>
</html>
```

- [ ] **Step 2: Verify file render**

Open file in browser: `file:///c:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20%28Persero%29/Desktop/PortalHC_KPB/docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`

Expected: blank page with light grey bg, no console error.

- [ ] **Step 3: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html
git commit -m "feat(pcp-3.4-v3.6): scaffold versi X HTML + CSS vars"
```

---

## Task 2: Versi X Header Bar + Container

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`

- [ ] **Step 1: Add header CSS block** (insert before closing `</style>`)

```css
  .container { max-width: 1400px; margin: 0 auto; }
  .header-bar { display: grid; grid-template-columns: 160px 1fr 160px; align-items: center; background: white; border-radius: .75rem; box-shadow: 0 2px 10px rgba(0,0,0,.06); padding: 1rem 1.5rem; border-top: 5px solid var(--pertamina-blue); margin-bottom: 1rem; }
  .logo-pertamina { font-weight: 900; font-size: 1.1rem; color: var(--pertamina-red); line-height: 1.1; }
  .logo-pertamina small { display:block; font-weight: 600; font-size: .7rem; color: var(--neutral-gray); }
  .header-title { text-align: center; }
  .header-title h1 { margin: 0; font-size: 1.4rem; color: var(--pertamina-blue); }
  .header-title small { display:block; color: var(--neutral-gray); font-size: .85rem; margin-top: .15rem; }
  .pcp-badge { text-align: center; background: var(--pertamina-blue); color: white; padding: .5rem .75rem; border-radius: .35rem; font-weight: 700; font-size: .85rem; }
  .pcp-badge small { display:block; font-weight: 400; font-size: .65rem; opacity: .9; }
  .meta-bar { background: white; border-radius: .5rem; padding: .75rem 1.25rem; display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; font-size: .85rem; border-left: 4px solid var(--pertamina-blue); margin-bottom: 1.5rem; }
  .meta-bar b { color: var(--pertamina-blue); }
  .toolbar { display: flex; justify-content: space-between; align-items: center; padding: .75rem 1.25rem; background: white; border-radius: .5rem; box-shadow: 0 1px 3px rgba(0,0,0,.06); margin-bottom: 1.5rem; }
  .toolbar .note { font-size: .85rem; color: var(--neutral-gray); }
  .toolbar button { background: var(--pertamina-red); color: white; border: none; padding: .5rem 1rem; border-radius: .35rem; font-weight: 600; cursor: pointer; }
```

- [ ] **Step 2: Replace `<div class="container"></div>` with full header HTML**

```html
<div class="container">
  <header class="header-bar">
    <div class="logo-pertamina">PERTAMINA<small>KILANG PERTAMINA BALIKPAPAN</small></div>
    <div class="header-title">
      <h1>3.4 Solusi Terpilih HC Portal — Versi X</h1>
      <small>BPMN Swimlane As-Is / To-Be — Hero: Assessment Workflow</small>
    </div>
    <div class="pcp-badge">STEP 3<small>Solusi Terpilih</small></div>
  </header>
  <div class="meta-bar">
    <div><b>Audience:</b> Process Improvement / BPM / SOP Review</div>
    <div><b>Tujuan:</b> Visualisasi transformasi workflow assessment via notasi standard BPMN 2.0 (ISO 19510)</div>
  </div>
  <div class="toolbar">
    <span class="note">📌 Versi X — Workflow-focused, native BPMN notation. Hero workflow: Assessment (6 → 2 step).</span>
    <button onclick="window.print()">🖨️ Print / Save PDF</button>
  </div>
</div>
```

- [ ] **Step 3: Verify in browser**

Reload. Expected: header bar dengan logo kiri, judul tengah, badge STEP 3 kanan; meta-bar 2 kolom; toolbar dengan tombol print merah.

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html
git commit -m "feat(pcp-3.4-v3.6): versi X header bar + container"
```

---

## Task 3: Versi X Sebelum Swimlane (4 Lane + 6 Task + Crisscross)

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`

- [ ] **Step 1: Add swimlane CSS** (append before `</style>`)

```css
  .swimlanes-wrap { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1.5rem; }
  .swimlane-panel { background: white; border-radius: 1rem; box-shadow: 0 6px 30px rgba(0,0,0,.08); padding: 1.5rem; }
  .swimlane-panel.sebelum { border-top: 6px solid var(--pertamina-red); background: linear-gradient(180deg, var(--pertamina-red-bg) 0%, white 30%); }
  .swimlane-panel.sesudah { border-top: 6px solid var(--pertamina-blue); background: linear-gradient(180deg, var(--pertamina-blue-bg) 0%, white 30%); }
  .panel-title { font-size: 1.15rem; font-weight: 700; margin-bottom: 1rem; display: flex; align-items: center; gap: .5rem; }
  .swimlane-panel.sebelum .panel-title { color: var(--pertamina-red); }
  .swimlane-panel.sesudah .panel-title { color: var(--pertamina-blue); }
  .lane { display: grid; grid-template-columns: 90px 1fr; border-bottom: 1px dashed var(--neutral-light); min-height: 70px; padding: .5rem 0; }
  .lane:last-child { border-bottom: none; }
  .lane-label { font-weight: 700; font-size: .8rem; padding: .5rem .25rem; border-right: 2px solid var(--neutral-light); display: flex; flex-direction: column; justify-content: center; color: var(--neutral-gray); }
  .lane.system .lane-label { color: var(--pertamina-blue); background: linear-gradient(90deg, var(--pertamina-blue-light), transparent); }
  .lane.passive .lane-label { opacity: .55; }
  .lane-content { padding: .35rem .5rem; display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; position: relative; }
  .task { background: white; border: 2px solid var(--pertamina-red); border-radius: .5rem; padding: .35rem .6rem; font-size: .78rem; min-width: 90px; max-width: 200px; box-shadow: 0 1px 3px rgba(0,0,0,.08); position: relative; }
  .swimlane-panel.sesudah .task { border-color: var(--pertamina-blue); }
  .task .task-num { font-weight: 800; color: var(--pertamina-red); }
  .swimlane-panel.sesudah .task .task-num { color: var(--pertamina-blue); }
  .task .task-time { display: block; font-size: .68rem; font-style: italic; color: var(--neutral-gray); margin-top: .15rem; }
  .marker { display: inline-block; width: 18px; height: 18px; border-radius: 50%; text-align: center; line-height: 18px; font-size: .7rem; font-weight: 800; color: white; margin-left: .15rem; cursor: help; }
  .marker.issue { background: var(--pertamina-red); }
  .marker.improvement { background: var(--pertamina-green); }
  .event { width: 24px; height: 24px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; font-size: .75rem; font-weight: 800; }
  .event.start { background: var(--pertamina-green); color: white; }
  .event.end { background: white; border: 3px solid var(--pertamina-blue); color: var(--pertamina-blue); }
  .swimlane-panel.sebelum .event.end { border-color: var(--pertamina-red); color: var(--pertamina-red); }
  .data-obj { display: inline-flex; align-items: center; font-size: .9rem; opacity: .85; }
  .data-obj.multi { gap: 1px; }
  .arrow-svg { position: absolute; pointer-events: none; }
  .total-stripe { margin-top: 1rem; padding: .75rem 1rem; border-radius: .5rem; font-weight: 700; text-align: center; }
  .swimlane-panel.sebelum .total-stripe { background: var(--pertamina-red); color: white; }
  .swimlane-panel.sesudah .total-stripe { background: var(--pertamina-blue); color: white; }
  .total-stripe .big { font-size: 1.4rem; }
  .total-stripe .label { font-size: .85rem; opacity: .9; margin-right: .5rem; }
```

- [ ] **Step 2: Add Sebelum swimlane HTML** (after `</div>` of `.toolbar`, before closing `</body>` div)

```html
  <div class="swimlanes-wrap">

    <section class="swimlane-panel sebelum">
      <div class="panel-title"><i class="bi bi-x-circle-fill"></i> SEBELUM (As-Is) — 6 Step Manual</div>

      <div class="lane passive">
        <div class="lane-label">Manajemen</div>
        <div class="lane-content">
          <div class="task" style="opacity:.6">Terima laporan akhir<span class="task-time">passive</span></div>
        </div>
      </div>

      <div class="lane">
        <div class="lane-label">HC</div>
        <div class="lane-content">
          <span class="event start" title="Start workflow">●</span>
          <div class="task">
            <span class="task-num">1.</span> Siapkan form Excel + distribusi email
            <span class="data-obj multi">📄📄📄📄📄</span>
            <span class="marker issue" title="A: Tools Terfragmentasi">A</span>
            <span class="marker issue" title="B: No SSoT">B</span>
            <span class="task-time">1j + 0,5j</span>
          </div>
          <div class="task">
            <span class="task-num">4.</span> Input + cross-check KKJ/Sertifikat
            <span class="data-obj multi">📄📄📄📄</span>
            <span class="marker issue" title="A: Tools">A</span>
            <span class="marker issue" title="B: SSoT">B</span>
            <span class="marker issue" title="D: Reporting ad-hoc">D</span>
            <span class="task-time">4j + 0,5j</span>
          </div>
          <div class="task">
            <span class="task-num">5.</span> Lapor atasan via Word + email
            <span class="data-obj">📄✉️</span>
            <span class="marker issue" title="C: No Audit Trail">C</span>
            <span class="marker issue" title="D: Reporting">D</span>
            <span class="task-time">2j + 8j</span>
          </div>
          <div class="task">
            <span class="task-num">6.</span> Arsip file share
            <span class="data-obj">📄</span>
            <span class="marker issue" title="C: No Audit">C</span>
            <span class="task-time">1j + 0j</span>
          </div>
        </div>
      </div>

      <div class="lane">
        <div class="lane-label">Atasan</div>
        <div class="lane-content">
          <div class="task">
            <span class="task-num">2.</span> Forward email/WA ke pekerja
            <span class="data-obj">✉️📱</span>
            <span class="marker issue" title="A: Tools">A</span>
            <span class="task-time">0,5j + 24j</span>
          </div>
        </div>
      </div>

      <div class="lane">
        <div class="lane-label">Pekerja</div>
        <div class="lane-content">
          <div class="task">
            <span class="task-num">3.</span> Isi form manual, kirim balik
            <span class="data-obj">📄</span>
            <span class="marker issue" title="A: Tools manual">A</span>
            <span class="task-time">1j + 16j</span>
          </div>
          <span class="event end" title="End workflow">○</span>
        </div>
      </div>

      <div class="total-stripe">
        <span class="label">Total Lead Time:</span>
        <span class="big">58,5j</span> <span style="font-size:.85rem">(~7 hari kerja)</span>
        | <span class="label">Active CT:</span> <b>9,5j</b>
        | <span class="label">Tools:</span> <b>4-5</b>
      </div>
    </section>
```

(Note: Sesudah panel added in Task 4.)

- [ ] **Step 3: Verify Sebelum panel renders**

Reload. Expected: panel kiri "Sebelum" dengan 4 lane (Manajemen passive, HC dengan 4 task, Atasan 1 task, Pekerja 1 task), markers A/B/C/D merah dengan tooltip, data object icons, total stripe merah bawah.

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html
git commit -m "feat(pcp-3.4-v3.6): versi X Sebelum swimlane 4 lane + 6 task"
```

---

## Task 4: Versi X Sesudah Swimlane (5 Lane termasuk System + 2 Task)

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`

- [ ] **Step 1: Add Sesudah swimlane HTML** (after `</section>` of Sebelum panel, inside `.swimlanes-wrap`)

```html
    <section class="swimlane-panel sesudah">
      <div class="panel-title"><i class="bi bi-check-circle-fill"></i> SESUDAH (To-Be) — 2 Step via HC Portal</div>

      <div class="lane">
        <div class="lane-label">Manajemen</div>
        <div class="lane-content">
          <div class="task">
            <i class="bi bi-graph-up"></i> Dashboard live
            <span class="marker improvement" title="1: Analytics Dashboard Real-Time">1</span>
            <span class="task-time">self-service</span>
          </div>
        </div>
      </div>

      <div class="lane">
        <div class="lane-label">HC</div>
        <div class="lane-content">
          <span class="event start">●</span>
          <div class="task">
            <span class="task-num">1.</span> Trigger assessment di portal
            <span class="marker improvement" title="2: Master Data Terpusat">2</span>
            <span class="marker improvement" title="4: HC Portal Hub">4</span>
            <span class="task-time">0,1j (6 mnt)</span>
          </div>
        </div>
      </div>

      <div class="lane system">
        <div class="lane-label"><i class="bi bi-gear-fill"></i> HC Portal<br><small style="font-style:italic;font-weight:400">(System)</small></div>
        <div class="lane-content">
          <div class="task" style="background:var(--pertamina-blue-light)">
            <i class="bi bi-file-earmark-text"></i> Form auto-generated
          </div>
          <div class="task" style="background:var(--pertamina-blue-light)">
            <i class="bi bi-shield-check"></i> Audit Log
            <span class="marker improvement" title="3: Audit Log Lengkap">3</span>
          </div>
          <div class="task" style="background:var(--pertamina-blue-light)">
            <i class="bi bi-broadcast"></i> Notif broadcast
          </div>
        </div>
      </div>

      <div class="lane passive">
        <div class="lane-label">Atasan</div>
        <div class="lane-content">
          <div class="task" style="opacity:.7">
            <i class="bi bi-eye"></i> auto-notif (view-only)
            <span class="task-time">passive</span>
          </div>
        </div>
      </div>

      <div class="lane">
        <div class="lane-label">Pekerja</div>
        <div class="lane-content">
          <div class="task">
            <span class="task-num">2.</span> Submit di portal
            <span class="marker improvement" title="3: Audit Log">3</span>
            <span class="marker improvement" title="7: Self-Service">7</span>
            <span class="task-time">0,25j + 8j</span>
          </div>
          <span class="event end">○</span>
        </div>
      </div>

      <div class="total-stripe">
        <span class="label">Total Lead Time:</span>
        <span class="big">8,35j</span> <span style="font-size:.85rem">(~1 hari kerja)</span>
        | <span class="label">Active CT:</span> <b>0,35j</b>
        | <span class="label">Tools:</span> <b>1</b>
      </div>
    </section>

  </div><!-- /swimlanes-wrap -->
```

- [ ] **Step 2: Verify Sesudah panel renders**

Reload. Expected: panel kanan "Sesudah" dengan 5 lane (Manajemen dashboard, HC trigger, **HC Portal System lane central dengan 3 sub-task + ⚙ icon background blue**, Atasan passive view, Pekerja submit). Markers hijau 1/2/3/4/7 dengan tooltip. Total stripe biru.

- [ ] **Step 3: Verify side-by-side layout**

Window width ≥ 1366px → 2 panel berdampingan. < 1366px → tetap berdampingan tapi mungkin overflow horizontal (acceptable per spec).

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html
git commit -m "feat(pcp-3.4-v3.6): versi X Sesudah swimlane 5 lane + System lane central hub"
```

---

## Task 5: Versi X Tables (Aspek + Issue + Improvement) + Legend

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`

- [ ] **Step 1: Add table CSS** (append before `</style>`)

```css
  .legend-section { background: white; border-radius: 1rem; padding: 1.25rem; box-shadow: 0 4px 15px rgba(0,0,0,.06); margin-bottom: 1.5rem; }
  .legend-section h3 { margin: 0 0 .75rem; color: var(--pertamina-blue); font-size: 1rem; }
  .legend-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; }
  .legend-list { display: flex; flex-wrap: wrap; gap: .65rem; font-size: .8rem; }
  .legend-list .item { display: inline-flex; align-items: center; gap: .35rem; padding: .25rem .5rem; background: var(--bg); border-radius: .35rem; }
  .legend-marker { display: inline-block; width: 22px; height: 22px; border-radius: 50%; text-align: center; line-height: 22px; font-weight: 800; color: white; font-size: .75rem; }
  .legend-marker.issue { background: var(--pertamina-red); }
  .legend-marker.improvement { background: var(--pertamina-green); }

  .tables-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1.5rem; }
  .table-card { background: white; border-radius: .75rem; padding: 1.25rem; box-shadow: 0 4px 15px rgba(0,0,0,.06); }
  .table-card h3 { margin: 0 0 .75rem; color: var(--pertamina-blue); font-size: 1rem; border-bottom: 2px solid var(--pertamina-red); padding-bottom: .35rem; }
  .table-card table { width: 100%; border-collapse: collapse; font-size: .8rem; }
  .table-card th, .table-card td { padding: .5rem .65rem; text-align: left; border-bottom: 1px solid var(--neutral-light); }
  .table-card th { background: var(--bg); font-weight: 700; color: var(--pertamina-blue); }
  .table-card tr:last-child td { border-bottom: none; }
  .aspek-table { background: white; border-radius: .75rem; padding: 1.25rem; box-shadow: 0 4px 15px rgba(0,0,0,.06); margin-bottom: 1.5rem; }
  .aspek-table h3 { margin: 0 0 .75rem; color: var(--pertamina-blue); font-size: 1rem; border-bottom: 2px solid var(--pertamina-red); padding-bottom: .35rem; }
  .aspek-table table { width: 100%; border-collapse: collapse; font-size: .85rem; }
  .aspek-table th, .aspek-table td { padding: .55rem .75rem; text-align: left; border-bottom: 1px solid var(--neutral-light); }
  .aspek-table th { background: var(--pertamina-blue); color: white; }
  .aspek-table .col-before { background: var(--pertamina-red-light); color: var(--pertamina-red); }
  .aspek-table .col-after { background: var(--pertamina-green-light); color: var(--pertamina-green); font-weight: 600; }
```

- [ ] **Step 2: Add legend + tables HTML** (after closing `.swimlanes-wrap` div, inside `.container`)

```html
  <section class="legend-section">
    <h3>📖 Legenda BPMN + Marker</h3>
    <div class="legend-grid">
      <div>
        <b style="font-size:.85rem">BPMN Notation:</b>
        <div class="legend-list" style="margin-top:.5rem">
          <span class="item"><span class="event start">●</span> Start event</span>
          <span class="item"><span class="event end">○</span> End event</span>
          <span class="item">□ Task</span>
          <span class="item">→ Sequence flow</span>
          <span class="item">⤳ Message flow</span>
          <span class="item">📄 Data object</span>
          <span class="item"><i class="bi bi-gear-fill"></i> System lane</span>
        </div>
      </div>
      <div>
        <b style="font-size:.85rem">Marker:</b>
        <div class="legend-list" style="margin-top:.5rem">
          <span class="item"><span class="legend-marker issue">A</span> Issue (Sebelum)</span>
          <span class="item"><span class="legend-marker improvement">1</span> Improvement (Sesudah)</span>
        </div>
      </div>
    </div>
  </section>

  <section class="aspek-table">
    <h3>📊 Tabel Aspek Komparasi — Assessment Workflow</h3>
    <table>
      <thead>
        <tr><th>Aspek</th><th class="col-before">Sebelum (Aktual)</th><th class="col-after">Sesudah (Desain)</th><th style="text-align:center">Δ</th></tr>
      </thead>
      <tbody>
        <tr><td><b>Step Count</b></td><td class="col-before">6 step manual</td><td class="col-after">2 step di portal</td><td style="text-align:center;color:var(--pertamina-green);font-weight:700">-67%</td></tr>
        <tr><td><b>Tools</b></td><td class="col-before">4-5 (Excel x4 + Word + Email)</td><td class="col-after">1 (HC Portal)</td><td style="text-align:center;color:var(--pertamina-green);font-weight:700">-80%</td></tr>
        <tr><td><b>Active CT</b></td><td class="col-before">9,5 jam</td><td class="col-after">0,35 jam (21 mnt)</td><td style="text-align:center;color:var(--pertamina-green);font-weight:700">-96%</td></tr>
        <tr><td><b>Lead Time</b></td><td class="col-before">58,5 jam (~7 hari)</td><td class="col-after">8,35 jam (~1 hari)</td><td style="text-align:center;color:var(--pertamina-green);font-weight:700">-86%</td></tr>
        <tr><td><b>Issue Cover</b></td><td class="col-before">A, B, C, D</td><td class="col-after">— (semua resolved)</td><td style="text-align:center;color:var(--pertamina-green);font-weight:700">100%</td></tr>
        <tr><td><b>Audit Trail</b></td><td class="col-before">❌ Tidak ada</td><td class="col-after">✅ Lengkap (CRUD log)</td><td style="text-align:center;color:var(--pertamina-green);font-weight:700">+∞</td></tr>
      </tbody>
    </table>
  </section>

  <div class="tables-grid">
    <section class="table-card">
      <h3><span class="legend-marker issue" style="vertical-align:middle">!</span> Tabel Issue A-F (Konteks)</h3>
      <table>
        <thead><tr><th>No</th><th>Issue</th><th>Lokasi</th></tr></thead>
        <tbody>
          <tr><td><span class="legend-marker issue">A</span></td><td><b>Tools Terfragmentasi</b> — 4-5 aplikasi berbeda</td><td>Multi-aktor</td></tr>
          <tr><td><span class="legend-marker issue">B</span></td><td><b>No SSoT</b> — data dicopy ke beberapa Excel</td><td>HC</td></tr>
          <tr><td><span class="legend-marker issue">C</span></td><td><b>No Audit Trail</b> — approval lisan</td><td>Atasan/HC</td></tr>
          <tr><td><span class="legend-marker issue">D</span></td><td><b>Reporting Ad-Hoc</b> — pivot manual</td><td>HC ↔ Mgmt</td></tr>
          <tr><td><span class="legend-marker issue" style="opacity:.5">E</span></td><td style="opacity:.5">Workflow Tanpa Tracking (PROTON/IDP)</td><td style="opacity:.5">Coach</td></tr>
          <tr><td><span class="legend-marker issue" style="opacity:.5">F</span></td><td style="opacity:.5">Renewal Sertifikat Reaktif</td><td style="opacity:.5">Pekerja</td></tr>
        </tbody>
      </table>
    </section>

    <section class="table-card">
      <h3><span class="legend-marker improvement" style="vertical-align:middle">!</span> Tabel Improvement 1-7</h3>
      <table>
        <thead><tr><th>No</th><th>Improvement</th><th>Applied</th></tr></thead>
        <tbody>
          <tr><td><span class="legend-marker improvement" style="opacity:.5">1</span></td><td style="opacity:.7"><b>Analytics Dashboard Real-Time</b></td><td>Mgmt lane</td></tr>
          <tr><td><span class="legend-marker improvement">2</span></td><td><b>Master Data Terpusat</b></td><td>HC trigger</td></tr>
          <tr><td><span class="legend-marker improvement">3</span></td><td><b>Audit Log Lengkap</b></td><td>Portal + Submit</td></tr>
          <tr><td><span class="legend-marker improvement">4</span></td><td><b>HC Portal sebagai Hub</b></td><td>HC trigger</td></tr>
          <tr><td><span class="legend-marker improvement" style="opacity:.5">5</span></td><td style="opacity:.7">Workflow Approval (IDP/PROTON)</td><td>—</td></tr>
          <tr><td><span class="legend-marker improvement" style="opacity:.5">6</span></td><td style="opacity:.7">Coaching Digital + Evidence Link</td><td>—</td></tr>
          <tr><td><span class="legend-marker improvement">7</span></td><td><b>Pekerja Self-Service</b></td><td>Submit lane</td></tr>
        </tbody>
      </table>
    </section>
  </div>
```

- [ ] **Step 3: Verify tables render**

Reload. Expected: legend section dengan BPMN notation + marker, tabel aspek 6 baris dengan kolom Sebelum (merah) / Sesudah (hijau), 2 tabel side-by-side Issue (A B C D bold, E F faded) + Improvement (2 3 4 7 bold, 1 di Mgmt, 5 6 faded).

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html
git commit -m "feat(pcp-3.4-v3.6): versi X tables (aspek + issue + improvement) + legend"
```

---

## Task 6: Versi X Print CSS + Footer

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html`

- [ ] **Step 1: Add print CSS** (append before `</style>`)

```css
  footer { text-align: center; padding: 1.5rem; color: var(--neutral-gray); font-size: .85rem; }
  footer code { background: white; padding: .15rem .4rem; border-radius: 3px; }

  @media print {
    @page { size: A3 landscape; margin: 1cm; }
    body { padding: 0; background: white; }
    .toolbar { display: none; }
    .swimlane-panel, .legend-section, .aspek-table, .table-card, .header-bar, .meta-bar { box-shadow: none; break-inside: avoid; }
    .swimlanes-wrap { page-break-after: avoid; }
    .tables-grid { page-break-before: auto; }
    * { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
  }
```

- [ ] **Step 2: Add footer HTML** (before closing `</div>` of `.container`)

```html
  <footer>
    <p>📌 <b>Versi X</b> — BPMN Swimlane As-Is/To-Be · PCP SMART 2026 §3.4 v3.6 · HC Portal · Tag <code>pcp-hcportal-3.4-v3.6</code></p>
    <p style="font-size:.75rem">Catatan: angka cycle/lead time = estimasi rekonstruksi back-calculation dari data agregat existing. Refine pasca-implementasi dengan time-motion study.</p>
  </footer>
```

- [ ] **Step 3: Verify print preview**

Browser → Ctrl+P → Layout: Landscape, Paper: A3 (atau A4 landscape jika A3 ga ada). Expected: header + swimlane + tables fit dalam 1-2 page, toolbar print button tersembunyi, colors preserved.

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-x-bpmn-swimlane.html
git commit -m "feat(pcp-3.4-v3.6): versi X print CSS A3 landscape + footer"
```

---

## Task 7: Scaffold Versi Z HTML Skeleton

**Files:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html`

- [ ] **Step 1: Create file dengan skeleton + CSS variables sama dengan Task 1**

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>§3.4 v3.6 — Versi Z: BPMN+VSM Hybrid HC Portal (Lean Six Sigma)</title>
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">
<style>
  :root {
    --pertamina-red: #C8102E;
    --pertamina-red-light: #fce8eb;
    --pertamina-red-bg: #FEE;
    --pertamina-blue: #00558C;
    --pertamina-blue-dark: #003D63;
    --pertamina-blue-light: #e6f0f7;
    --pertamina-blue-bg: #EEF;
    --pertamina-green: #00A551;
    --pertamina-green-light: #d4f0dd;
    --pertamina-yellow: #FFC72C;
    --neutral-gray: #6b7280;
    --neutral-light: #d1d5db;
    --bg: #f6f7fb;
    --hub-grad: linear-gradient(135deg, #00558C, #00A551);
  }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: #1f2937; padding: 1.5rem; }
  .container { max-width: 1400px; margin: 0 auto; }

  .header-bar { display: grid; grid-template-columns: 160px 1fr 160px; align-items: center; background: white; border-radius: .75rem; box-shadow: 0 2px 10px rgba(0,0,0,.06); padding: 1rem 1.5rem; border-top: 5px solid var(--pertamina-green); margin-bottom: 1rem; }
  .logo-pertamina { font-weight: 900; font-size: 1.1rem; color: var(--pertamina-red); line-height: 1.1; }
  .logo-pertamina small { display:block; font-weight: 600; font-size: .7rem; color: var(--neutral-gray); }
  .header-title { text-align: center; }
  .header-title h1 { margin: 0; font-size: 1.4rem; color: var(--pertamina-blue); }
  .header-title small { display:block; color: var(--neutral-gray); font-size: .85rem; margin-top: .15rem; }
  .pcp-badge { text-align: center; background: var(--pertamina-green); color: white; padding: .5rem .75rem; border-radius: .35rem; font-weight: 700; font-size: .85rem; }
  .pcp-badge small { display:block; font-weight: 400; font-size: .65rem; opacity: .9; }
  .meta-bar { background: white; border-radius: .5rem; padding: .75rem 1.25rem; display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; font-size: .85rem; border-left: 4px solid var(--pertamina-green); margin-bottom: 1.5rem; }
  .meta-bar b { color: var(--pertamina-green); }
  .toolbar { display: flex; justify-content: space-between; align-items: center; padding: .75rem 1.25rem; background: white; border-radius: .5rem; box-shadow: 0 1px 3px rgba(0,0,0,.06); margin-bottom: 1.5rem; }
  .toolbar .note { font-size: .85rem; color: var(--neutral-gray); }
  .toolbar button { background: var(--pertamina-red); color: white; border: none; padding: .5rem 1rem; border-radius: .35rem; font-weight: 600; cursor: pointer; }
</style>
</head>
<body>
<div class="container">

  <header class="header-bar">
    <div class="logo-pertamina">PERTAMINA<small>KILANG PERTAMINA BALIKPAPAN</small></div>
    <div class="header-title">
      <h1>3.4 Solusi Terpilih HC Portal — Versi Z</h1>
      <small>BPMN + VSM Hybrid · Lean Six Sigma View · Hero: Assessment</small>
    </div>
    <div class="pcp-badge">STEP 3<small>Solusi Terpilih</small></div>
  </header>
  <div class="meta-bar">
    <div><b>Audience:</b> OPEX / Lean Six Sigma / CI / Data-driven Reviewer</div>
    <div><b>Tujuan:</b> Quantified value stream — cycle time, lead time, VA ratio, waste identification</div>
  </div>
  <div class="toolbar">
    <span class="note">📌 Versi Z — 3 zona: BPMN compact + VSM strip + 5 metric KPI. Lean orthodox VA classification.</span>
    <button onclick="window.print()">🖨️ Print / Save PDF</button>
  </div>

</div>
</body>
</html>
```

- [ ] **Step 2: Verify file renders**

Open in browser. Expected: header dengan border green (membedakan dari versi X biru), toolbar print button.

- [ ] **Step 3: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html
git commit -m "feat(pcp-3.4-v3.6): scaffold versi Z HTML + header (green theme)"
```

---

## Task 8: Versi Z Zona 1 — BPMN Compact

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html`

- [ ] **Step 1: Add BPMN compact CSS** (append before `</style>`)

```css
  .zona { background: white; border-radius: 1rem; padding: 1.25rem 1.5rem; margin-bottom: 1rem; box-shadow: 0 4px 15px rgba(0,0,0,.06); }
  .zona-title { font-size: 1rem; font-weight: 700; color: var(--pertamina-blue); margin: 0 0 .75rem; padding-bottom: .35rem; border-bottom: 2px solid var(--pertamina-green); }
  .bpmn-compact { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
  .bpmn-side { padding: .75rem; border-radius: .5rem; }
  .bpmn-side.sebelum { background: var(--pertamina-red-bg); border-left: 4px solid var(--pertamina-red); }
  .bpmn-side.sesudah { background: var(--pertamina-blue-bg); border-left: 4px solid var(--pertamina-blue); }
  .bpmn-side h4 { margin: 0 0 .5rem; font-size: .9rem; }
  .bpmn-side.sebelum h4 { color: var(--pertamina-red); }
  .bpmn-side.sesudah h4 { color: var(--pertamina-blue); }
  .compact-lane { display: grid; grid-template-columns: 70px 1fr; padding: .25rem 0; border-bottom: 1px dashed rgba(0,0,0,.08); font-size: .78rem; }
  .compact-lane:last-child { border-bottom: none; }
  .compact-lane .lbl { font-weight: 700; color: var(--neutral-gray); align-self: center; }
  .compact-lane .flow { display: flex; flex-wrap: wrap; gap: .25rem; align-items: center; }
  .compact-task { background: white; border: 1.5px solid var(--neutral-light); border-radius: .25rem; padding: .15rem .4rem; font-size: .7rem; }
  .bpmn-side.sebelum .compact-task { border-color: var(--pertamina-red); }
  .bpmn-side.sesudah .compact-task { border-color: var(--pertamina-blue); }
  .bpmn-side.sesudah .compact-task.system { background: var(--pertamina-blue-light); }
```

- [ ] **Step 2: Add Zona 1 HTML** (before closing `</div>` of `.container`)

```html
  <section class="zona">
    <div class="zona-title">🔷 Zona 1 — BPMN Swimlane (Compact)</div>
    <div class="bpmn-compact">

      <div class="bpmn-side sebelum">
        <h4><i class="bi bi-x-circle-fill"></i> SEBELUM — 6 step manual</h4>
        <div class="compact-lane"><div class="lbl">Mgmt</div><div class="flow"><span style="opacity:.5">(passive terima laporan)</span></div></div>
        <div class="compact-lane"><div class="lbl">HC</div><div class="flow">●→<span class="compact-task">1.Siapkan</span>→<span class="compact-task">4.Input+Cross</span>→<span class="compact-task">5.Lapor</span>→<span class="compact-task">6.Arsip</span></div></div>
        <div class="compact-lane"><div class="lbl">Atasan</div><div class="flow">⤳<span class="compact-task">2.Forward</span></div></div>
        <div class="compact-lane"><div class="lbl">Pekerja</div><div class="flow">⤳<span class="compact-task">3.Isi Manual</span>→○</div></div>
      </div>

      <div class="bpmn-side sesudah">
        <h4><i class="bi bi-check-circle-fill"></i> SESUDAH — 2 step di portal</h4>
        <div class="compact-lane"><div class="lbl">Mgmt</div><div class="flow"><span class="compact-task"><i class="bi bi-graph-up"></i> Dashboard live</span></div></div>
        <div class="compact-lane"><div class="lbl">HC</div><div class="flow">●→<span class="compact-task">1.Trigger Portal</span></div></div>
        <div class="compact-lane"><div class="lbl"><i class="bi bi-gear-fill"></i> Portal</div><div class="flow"><span class="compact-task system">Form auto</span><span class="compact-task system">Audit Log</span><span class="compact-task system">Notif</span></div></div>
        <div class="compact-lane"><div class="lbl">Atasan</div><div class="flow"><span style="opacity:.5">⚡auto-notif</span></div></div>
        <div class="compact-lane"><div class="lbl">Pekerja</div><div class="flow">⤳<span class="compact-task">2.Submit Portal</span>→○</div></div>
      </div>

    </div>
  </section>
```

- [ ] **Step 3: Verify Zona 1 renders**

Reload. Expected: zona 1 dengan 2 side panel (Sebelum red, Sesudah blue), lane compact 70px label + flow inline.

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html
git commit -m "feat(pcp-3.4-v3.6): versi Z Zona 1 BPMN compact"
```

---

## Task 9: Versi Z Zona 2 VSM Sebelum (Process + Data Box + Timeline)

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html`

- [ ] **Step 1: Add VSM CSS** (append before `</style>`)

```css
  .vsm-block { margin-bottom: 1rem; padding: .75rem; border-radius: .5rem; }
  .vsm-block.sebelum { background: var(--pertamina-red-bg); }
  .vsm-block.sesudah { background: var(--pertamina-blue-bg); }
  .vsm-block h4 { margin: 0 0 .65rem; font-size: .9rem; }
  .vsm-block.sebelum h4 { color: var(--pertamina-red); }
  .vsm-block.sesudah h4 { color: var(--pertamina-blue); }
  .vsm-row { display: flex; align-items: flex-start; gap: .25rem; overflow-x: auto; padding-bottom: .5rem; }
  .vsm-process { background: white; border: 2px solid var(--pertamina-blue); border-radius: .35rem; padding: .35rem .5rem; min-width: 80px; text-align: center; font-size: .72rem; }
  .vsm-process .pn { font-weight: 800; color: var(--pertamina-blue); display: block; font-size: .7rem; }
  .vsm-process .ct { font-weight: 700; font-size: .75rem; color: var(--pertamina-blue); }
  .vsm-process .va-badge { display: inline-block; font-size: .6rem; font-weight: 800; padding: 1px 4px; border-radius: 3px; margin-top: .15rem; }
  .vsm-process .va-badge.va { background: var(--pertamina-green); color: white; }
  .vsm-process .va-badge.nva { background: var(--neutral-gray); color: white; }
  .vsm-process.va { border-color: var(--pertamina-green); border-width: 3px; }
  .vsm-triangle { color: var(--pertamina-yellow); align-self: center; font-size: 1.8rem; line-height: 1; position: relative; }
  .vsm-triangle::after { content: attr(data-wait); position: absolute; top: -1.2rem; left: 50%; transform: translateX(-50%); font-size: .65rem; color: var(--neutral-gray); white-space: nowrap; font-weight: 700; }
  .vsm-end { align-self: center; width: 22px; height: 22px; border-radius: 50%; background: white; border: 3px solid var(--pertamina-blue); display: inline-flex; align-items: center; justify-content: center; font-weight: 800; color: var(--pertamina-blue); }
  .vsm-databox { display: grid; grid-template-columns: repeat(auto-fit, minmax(40px, 1fr)); gap: .15rem; font-size: .6rem; margin-top: .25rem; padding-top: .25rem; border-top: 1px solid var(--neutral-light); }
  .vsm-databox .dcell { text-align: center; }
  .vsm-databox .dcell .k { display: block; color: var(--neutral-gray); font-weight: 700; }

  .timeline-stripe { margin-top: .75rem; padding: .75rem; background: white; border-radius: .5rem; }
  .timeline-stripe .tline { display: flex; height: 28px; width: 100%; border: 1px solid var(--neutral-light); border-radius: 3px; overflow: hidden; position: relative; }
  .tseg { height: 100%; display: flex; align-items: center; justify-content: center; font-size: .65rem; font-weight: 800; color: white; overflow: hidden; white-space: nowrap; }
  .tseg.va { background: var(--pertamina-green); }
  .tseg.nva { background: repeating-linear-gradient(45deg, var(--neutral-gray), var(--neutral-gray) 4px, #9aa3af 4px, #9aa3af 8px); }
  .tseg.wait { background: repeating-linear-gradient(90deg, var(--pertamina-red-light), var(--pertamina-red-light) 6px, white 6px, white 8px); color: var(--pertamina-red); }
  .timeline-ticks { display: flex; justify-content: space-between; font-size: .65rem; color: var(--neutral-gray); margin-top: .25rem; }
  .vsm-summary { margin-top: .5rem; display: flex; gap: 1rem; font-size: .8rem; flex-wrap: wrap; }
  .vsm-summary b { color: var(--pertamina-blue); }
```

- [ ] **Step 2: Add Zona 2 wrapper + VSM Sebelum HTML** (before closing `</div>` of `.container`)

```html
  <section class="zona">
    <div class="zona-title">📊 Zona 2 — Value Stream Map (Shared Linear Scale 0-58,5j)</div>

    <div class="vsm-block sebelum">
      <h4><i class="bi bi-x-circle-fill"></i> SEBELUM VSM — Total LT 58,5j</h4>
      <div class="vsm-row">
        <span class="event start" style="background:var(--pertamina-green);color:white;width:24px;height:24px;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;font-weight:800;align-self:center">●</span>
        <div class="vsm-process">
          <span class="pn">P1 Siapkan</span>
          <div class="ct">CT 1j</div>
          <div class="vsm-databox">
            <div class="dcell"><span class="k">Tool</span>E×5</div>
            <div class="dcell"><span class="k">Iss</span>A,B</div>
          </div>
          <span class="va-badge nva">NVA</span>
        </div>
        <span class="vsm-triangle" data-wait="0,5j" title="Wait 0,5j">▽</span>
        <div class="vsm-process">
          <span class="pn">P2 Forward</span>
          <div class="ct">CT 0,5j</div>
          <div class="vsm-databox">
            <div class="dcell"><span class="k">Tool</span>Email</div>
            <div class="dcell"><span class="k">Iss</span>A</div>
          </div>
          <span class="va-badge nva">NVA</span>
        </div>
        <span class="vsm-triangle" data-wait="24j">▽</span>
        <div class="vsm-process va">
          <span class="pn">P3 Isi Form</span>
          <div class="ct">CT 1j</div>
          <div class="vsm-databox">
            <div class="dcell"><span class="k">Tool</span>Paper</div>
            <div class="dcell"><span class="k">Iss</span>A</div>
          </div>
          <span class="va-badge va">VA</span>
        </div>
        <span class="vsm-triangle" data-wait="16j">▽</span>
        <div class="vsm-process">
          <span class="pn">P4 Input+X</span>
          <div class="ct">CT 4j</div>
          <div class="vsm-databox">
            <div class="dcell"><span class="k">Tool</span>E×4</div>
            <div class="dcell"><span class="k">Iss</span>A,B,D</div>
          </div>
          <span class="va-badge nva">NVA</span>
        </div>
        <span class="vsm-triangle" data-wait="0,5j">▽</span>
        <div class="vsm-process">
          <span class="pn">P5 Lapor</span>
          <div class="ct">CT 2j</div>
          <div class="vsm-databox">
            <div class="dcell"><span class="k">Tool</span>Word</div>
            <div class="dcell"><span class="k">Iss</span>C,D</div>
          </div>
          <span class="va-badge nva">NVA</span>
        </div>
        <span class="vsm-triangle" data-wait="8j">▽</span>
        <div class="vsm-process">
          <span class="pn">P6 Arsip</span>
          <div class="ct">CT 1j</div>
          <div class="vsm-databox">
            <div class="dcell"><span class="k">Tool</span>Share</div>
            <div class="dcell"><span class="k">Iss</span>C</div>
          </div>
          <span class="va-badge nva">NVA</span>
        </div>
        <span class="vsm-end">○</span>
      </div>

      <div class="timeline-stripe">
        <div class="tline">
          <!-- VA 1j = 1.71% of 58.5; NVA 8.5j = 14.5%; Wait 49j = 83.76% -->
          <div class="tseg va" style="width:1.71%" title="VA 1j">VA</div>
          <div class="tseg nva" style="width:14.5%" title="NVA 8,5j">NVA 8,5j</div>
          <div class="tseg wait" style="width:83.76%" title="Wait 49j">Wait 49j</div>
        </div>
        <div class="timeline-ticks"><span>0j</span><span>10j</span><span>20j</span><span>30j</span><span>40j</span><span>50j</span><span>58,5j</span></div>
        <div class="vsm-summary">
          <span>⏱ <b>Total LT 58,5j</b></span>
          <span>CT <b>9,5j</b></span>
          <span>VA% of CT <b>10,5%</b></span>
          <span>VA% of LT <b>1,7%</b></span>
        </div>
      </div>
    </div>

  </section>
```

- [ ] **Step 3: Verify Zona 2 Sebelum renders**

Reload. Expected: VSM Sebelum dengan 6 process box (P3 border green tebal sebagai VA), 5 triangle ▽ dengan label wait, end circle, timeline stripe (1.7% green sliver + 14.5% striped grey + 83.8% dotted red), tick marker 0/10/20/30/40/50/58,5j.

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html
git commit -m "feat(pcp-3.4-v3.6): versi Z Zona 2 VSM Sebelum 6 process + timeline linear"
```

---

## Task 10: Versi Z Zona 2 VSM Sesudah (Compact)

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html`

- [ ] **Step 1: Add VSM Sesudah HTML** (inside Zona 2 `<section>`, after Sebelum `</div>`)

```html
    <div class="vsm-block sesudah">
      <h4><i class="bi bi-check-circle-fill"></i> SESUDAH VSM — Total LT 8,35j (visible scale 0-58,5j shared)</h4>
      <div class="vsm-row">
        <span class="event start" style="background:var(--pertamina-green);color:white;width:24px;height:24px;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;font-weight:800;align-self:center">●</span>
        <div class="vsm-process">
          <span class="pn">P1 Trigger</span>
          <div class="ct">CT 0,1j</div>
          <div class="vsm-databox">
            <div class="dcell"><span class="k">Tool</span>HCP</div>
            <div class="dcell"><span class="k">Imp</span>2,4</div>
          </div>
          <span class="va-badge nva">NVA</span>
        </div>
        <span class="vsm-triangle" data-wait="0j">▽</span>
        <div class="vsm-process va">
          <span class="pn">P2 Submit</span>
          <div class="ct">CT 0,25j</div>
          <div class="vsm-databox">
            <div class="dcell"><span class="k">Tool</span>HCP</div>
            <div class="dcell"><span class="k">Imp</span>3,7</div>
          </div>
          <span class="va-badge va">VA</span>
        </div>
        <span class="vsm-triangle" data-wait="8j">▽</span>
        <span class="vsm-end">○</span>
      </div>

      <div class="timeline-stripe">
        <div class="tline">
          <!-- shared linear scale 0-58.5j: VA 0.25j=0.43%, NVA 0.1j=0.17%, Wait 8j=13.68%, remaining 85.72% empty -->
          <div class="tseg va" style="width:0.43%" title="VA 0,25j"></div>
          <div class="tseg nva" style="width:0.17%" title="NVA 0,1j"></div>
          <div class="tseg wait" style="width:13.68%" title="Wait 8j">Wait 8j</div>
          <div style="flex:1;background:repeating-linear-gradient(90deg,#f6f7fb,#f6f7fb 4px,white 4px,white 8px);"></div>
        </div>
        <div class="timeline-ticks"><span>0j</span><span>10j</span><span>20j</span><span>30j</span><span>40j</span><span>50j</span><span>58,5j</span></div>
        <div class="vsm-summary">
          <span>⏱ <b>Total LT 8,35j</b> (vs Sebelum 58,5j → <span style="color:var(--pertamina-green);font-weight:800">-86%</span>)</span>
          <span>CT <b>0,35j</b></span>
          <span>VA% of CT <b>71,4%</b> ✅ &gt;Lean 25%</span>
          <span>VA% of LT <b>3%</b></span>
        </div>
      </div>
    </div>
```

- [ ] **Step 2: Verify Sesudah VSM render**

Reload. Expected: VSM Sesudah dengan 2 process box (P2 Submit border green VA), 2 triangle, end circle. Timeline stripe Sesudah jadi tiny segment kiri (~14% total dengan ratio: 0.6% VA+NVA + 13.7% Wait), sisanya 85.7% blank striped. **Visual impact brutal:** strip Sesudah keliatan kecil banget dibanding Sebelum.

- [ ] **Step 3: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html
git commit -m "feat(pcp-3.4-v3.6): versi Z Zona 2 VSM Sesudah compact timeline shared scale"
```

---

## Task 11: Versi Z Zona 3 — Metric Cards Quantified (5 KPI)

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html`

- [ ] **Step 1: Add metric cards CSS** (append before `</style>`)

```css
  .metric-cards { display: grid; grid-template-columns: repeat(5, 1fr); gap: .75rem; }
  .kpi-card { background: white; border-radius: .65rem; padding: 1rem; text-align: center; border-top: 4px solid var(--neutral-gray); box-shadow: 0 2px 8px rgba(0,0,0,.05); position: relative; }
  .kpi-card.lean { border-top-color: var(--pertamina-green); }
  .kpi-card .kpi-title { font-size: .75rem; font-weight: 700; color: var(--neutral-gray); text-transform: uppercase; letter-spacing: .5px; margin-bottom: .35rem; }
  .kpi-card .kpi-before-after { font-size: .9rem; color: var(--pertamina-blue); font-weight: 600; margin-bottom: .25rem; }
  .kpi-card .kpi-delta { font-size: 1.5rem; font-weight: 900; line-height: 1; }
  .kpi-card .kpi-delta.green { color: var(--pertamina-green); }
  .kpi-card .kpi-delta.boost { color: var(--pertamina-blue); }
  .kpi-card .kpi-badge { position: absolute; top: .5rem; right: .5rem; background: var(--pertamina-green); color: white; font-size: .55rem; font-weight: 800; padding: .15rem .35rem; border-radius: 3px; text-transform: uppercase; }
  @media (max-width: 1100px) { .metric-cards { grid-template-columns: repeat(3, 1fr); } }
```

- [ ] **Step 2: Add Zona 3 HTML** (after Zona 2 `</section>`)

```html
  <section class="zona">
    <div class="zona-title">⚡ Zona 3 — Metric KPI Quantified (Lean Six Sigma)</div>
    <div class="metric-cards">

      <div class="kpi-card lean">
        <span class="kpi-badge">✅ Lean</span>
        <div class="kpi-title">Lead Time</div>
        <div class="kpi-before-after">58,5j → 8,35j</div>
        <div class="kpi-delta green">-86%</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-title">Active CT</div>
        <div class="kpi-before-after">9,5j → 0,35j</div>
        <div class="kpi-delta green">-96%</div>
      </div>

      <div class="kpi-card lean">
        <span class="kpi-badge">✅ &gt;25%</span>
        <div class="kpi-title">VA Ratio (of CT)</div>
        <div class="kpi-before-after">10,5% → 71,4%</div>
        <div class="kpi-delta boost">+581%</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-title">Tools</div>
        <div class="kpi-before-after">5 → 1</div>
        <div class="kpi-delta green">-80%</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-title">Step Count</div>
        <div class="kpi-before-after">6 → 2</div>
        <div class="kpi-delta green">-67%</div>
      </div>

    </div>
  </section>
```

- [ ] **Step 3: Verify Zona 3 renders**

Reload. Expected: 5 KPI card horizontal (LT/CT/VA/Tools/Step), card LT + VA dengan badge "✅ Lean" / "✅ >25%" + border green tebal. Delta value besar (1.5rem) green/blue.

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html
git commit -m "feat(pcp-3.4-v3.6): versi Z Zona 3 5 metric KPI cards (Lean badges)"
```

---

## Task 12: Versi Z Tables Inline + Print CSS + Footer

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html`

- [ ] **Step 1: Add tables CSS** (append before `</style>`)

Reuse pattern dari versi X — append:

```css
  .tables-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1rem; }
  .table-card { background: white; border-radius: .75rem; padding: 1rem 1.25rem; box-shadow: 0 4px 15px rgba(0,0,0,.06); }
  .table-card h3 { margin: 0 0 .65rem; color: var(--pertamina-blue); font-size: .95rem; border-bottom: 2px solid var(--pertamina-green); padding-bottom: .35rem; }
  .table-card table { width: 100%; border-collapse: collapse; font-size: .78rem; }
  .table-card th, .table-card td { padding: .4rem .6rem; text-align: left; border-bottom: 1px solid var(--neutral-light); }
  .table-card th { background: var(--bg); font-weight: 700; color: var(--pertamina-blue); }
  .legend-marker { display: inline-block; width: 20px; height: 20px; border-radius: 50%; text-align: center; line-height: 20px; font-weight: 800; color: white; font-size: .7rem; }
  .legend-marker.issue { background: var(--pertamina-red); }
  .legend-marker.improvement { background: var(--pertamina-green); }
  footer { text-align: center; padding: 1.5rem; color: var(--neutral-gray); font-size: .85rem; }
  footer code { background: white; padding: .15rem .4rem; border-radius: 3px; }

  @media print {
    @page { size: A3 landscape; margin: 1cm; }
    body { padding: 0; background: white; }
    .toolbar { display: none; }
    .zona, .table-card, .header-bar, .meta-bar { box-shadow: none; break-inside: avoid; }
    * { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
  }
```

- [ ] **Step 2: Add tables + footer HTML** (after Zona 3 `</section>`)

```html
  <div class="tables-grid">
    <section class="table-card">
      <h3><span class="legend-marker issue" style="vertical-align:middle">!</span> Issue A-F (Konteks)</h3>
      <table>
        <thead><tr><th>No</th><th>Issue</th></tr></thead>
        <tbody>
          <tr><td><span class="legend-marker issue">A</span></td><td><b>Tools Terfragmentasi</b></td></tr>
          <tr><td><span class="legend-marker issue">B</span></td><td><b>No SSoT</b></td></tr>
          <tr><td><span class="legend-marker issue">C</span></td><td><b>No Audit Trail</b></td></tr>
          <tr><td><span class="legend-marker issue">D</span></td><td><b>Reporting Ad-Hoc</b></td></tr>
          <tr><td><span class="legend-marker issue" style="opacity:.5">E</span></td><td style="opacity:.5">Workflow Tracking (lain)</td></tr>
          <tr><td><span class="legend-marker issue" style="opacity:.5">F</span></td><td style="opacity:.5">Renewal Reaktif (lain)</td></tr>
        </tbody>
      </table>
    </section>

    <section class="table-card">
      <h3><span class="legend-marker improvement" style="vertical-align:middle">!</span> Improvement 1-7</h3>
      <table>
        <thead><tr><th>No</th><th>Improvement</th><th>Lane</th></tr></thead>
        <tbody>
          <tr><td><span class="legend-marker improvement" style="opacity:.5">1</span></td><td style="opacity:.7"><b>Analytics Dashboard</b></td><td>Mgmt</td></tr>
          <tr><td><span class="legend-marker improvement">2</span></td><td><b>Master Data Terpusat</b></td><td>HC trigger</td></tr>
          <tr><td><span class="legend-marker improvement">3</span></td><td><b>Audit Log Lengkap</b></td><td>Portal/Submit</td></tr>
          <tr><td><span class="legend-marker improvement">4</span></td><td><b>HC Portal sebagai Hub</b></td><td>HC trigger</td></tr>
          <tr><td><span class="legend-marker improvement" style="opacity:.5">5</span></td><td style="opacity:.7">Workflow Approval (lain)</td><td>—</td></tr>
          <tr><td><span class="legend-marker improvement" style="opacity:.5">6</span></td><td style="opacity:.7">Coaching Digital (lain)</td><td>—</td></tr>
          <tr><td><span class="legend-marker improvement">7</span></td><td><b>Self-Service</b></td><td>Submit</td></tr>
        </tbody>
      </table>
    </section>
  </div>

  <footer>
    <p>📌 <b>Versi Z</b> — BPMN+VSM Hybrid (Lean Six Sigma) · PCP SMART 2026 §3.4 v3.6 · HC Portal · Tag <code>pcp-hcportal-3.4-v3.6</code></p>
    <p style="font-size:.75rem">Catatan: cycle time + VA classification estimasi rekonstruksi. VA orthodox (Lean): step 4 cross-check Excel = NVA (rework/re-entry).</p>
  </footer>
```

- [ ] **Step 3: Print preview**

Browser → Ctrl+P → A3 landscape. Expected: 3 zona + tables fit 1-2 page, colors preserved, toolbar hidden.

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-z-bpmn-vsm-hybrid.html
git commit -m "feat(pcp-3.4-v3.6): versi Z tables inline + print CSS A3 + footer"
```

---

## Task 13: Update index.html — Tambah Versi X + Z + Audience Matrix

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/index.html`

- [ ] **Step 1: Update hero badge "2 Versi" → "4 Versi"**

Find:
```html
      <span>v3.0</span>
      <span>2 Versi Diagram Landscape</span>
      <span>Tag pcp-hcportal-3.4-v3.0</span>
```

Replace with:
```html
      <span>v3.6</span>
      <span>4 Versi Diagram Landscape</span>
      <span>Tag pcp-hcportal-3.4-v3.6</span>
```

- [ ] **Step 2: Add CSS for tertiary/quaternary card variant**

Find `.version-card.secondary { border-top: 6px solid var(--pertamina-yellow); }` and add after:

```css
  .version-card.tertiary { border-top: 6px solid var(--pertamina-blue); }
  .version-card.quaternary { border-top: 6px solid var(--pertamina-red); }
  .version-card.tertiary .badge { background: var(--pertamina-blue); color: white; }
  .version-card.quaternary .badge { background: var(--pertamina-red); color: white; }
  .audience-matrix { background: white; border-radius: 1rem; padding: 1.5rem 2rem; box-shadow: 0 4px 15px rgba(0,0,0,.06); margin-bottom: 1.5rem; }
  .audience-matrix h3 { color: var(--pertamina-blue); margin: 0 0 .75rem; }
  .audience-matrix table { width: 100%; border-collapse: collapse; font-size: .9rem; }
  .audience-matrix th, .audience-matrix td { padding: .55rem .75rem; text-align: left; border-bottom: 1px solid #e5e7eb; }
  .audience-matrix th { background: var(--bg); color: var(--pertamina-blue); font-weight: 700; }
  .audience-matrix .ver { font-weight: 800; }
  .audience-matrix .ver.p { color: var(--pertamina-green); }
  .audience-matrix .ver.c { color: #8a6d00; }
  .audience-matrix .ver.x { color: var(--pertamina-blue); }
  .audience-matrix .ver.z { color: var(--pertamina-red); }
```

- [ ] **Step 3: Add Versi X + Z cards** inside `<div class="versions-grid">`, after `.version-card.secondary`:

```html
    <div class="version-card tertiary">
      <div class="badge">Tertiary</div>
      <div class="icon-big">🔀</div>
      <h2>Versi X — BPMN Swimlane</h2>
      <div class="description">BPMN 2.0 As-Is/To-Be — 4 lane aktor + System lane (HC Portal central hub). Hero workflow: Assessment 6→2 step.</div>
      <ul class="features">
        <li>✅ Notation native BPMN ISO 19510</li>
        <li>✅ 5 lane Sesudah (HC Portal System central)</li>
        <li>✅ Multi data object 📄 visual fragmentasi</li>
        <li>✅ Issue A-D + Improvement 2,3,4,7</li>
        <li>✅ Tabel Aspek 6 baris + total stripe</li>
        <li>✅ Crisscross merah Sebelum, lurus biru Sesudah</li>
      </ul>
      <a href="versi-x-bpmn-swimlane.html" class="open-btn">Buka Versi X →</a>
    </div>

    <div class="version-card quaternary">
      <div class="badge">Quaternary</div>
      <div class="icon-big">📈</div>
      <h2>Versi Z — BPMN+VSM Hybrid</h2>
      <div class="description">Lean Six Sigma view — 3 zona: BPMN compact + VSM strip + 5 KPI cards. Shared linear scale untuk impact visual brutal.</div>
      <ul class="features">
        <li>✅ Value Stream Map dengan VA classification</li>
        <li>✅ Timeline stripe linear 0-58,5j shared</li>
        <li>✅ 5 metric KPI (LT/CT/VA/Tools/Step)</li>
        <li>✅ Lean badge ✅ (VA 71,4% &gt;25% target)</li>
        <li>✅ Process box + Data box + Inventory ▽</li>
        <li>✅ Step 4 cross-check NVA (Lean orthodox)</li>
      </ul>
      <a href="versi-z-bpmn-vsm-hybrid.html" class="open-btn">Buka Versi Z →</a>
    </div>
```

- [ ] **Step 4: Replace `.info-section` "Rekomendasi Pemilihan" dengan audience matrix**

Find:
```html
  <div class="info-section">
    <h3>📌 Rekomendasi Pemilihan</h3>
    <ul>
      <li><b>Submission PCP utama:</b> Versi P (match slide referensi, reviewer engineering familiar)</li>
      <li><b>Executive presentation:</b> Versi C (data-dense, mudah dipahami manajemen)</li>
      <li><b>Internal showcase:</b> keduanya — Versi P jelaskan flow, Versi C jelaskan impact</li>
    </ul>
  </div>
```

Replace with:
```html
  <div class="audience-matrix">
    <h3>🎯 Matriks Audience → Versi Rekomendasi</h3>
    <table>
      <thead>
        <tr><th>Audience</th><th>Versi Rekomendasi</th><th>Alasan</th></tr>
      </thead>
      <tbody>
        <tr>
          <td>Engineering / Operations PCP main</td>
          <td><span class="ver p">Versi P</span></td>
          <td>Match slide PCP referensi (Purdue-style), reviewer engineering familiar</td>
        </tr>
        <tr>
          <td>Executive / Management showcase</td>
          <td><span class="ver c">Versi C</span></td>
          <td>Card-grid 7 fitur, metric per fitur, mudah dicerna manajemen</td>
        </tr>
        <tr>
          <td>Process Improvement / BPM / SOP Review</td>
          <td><span class="ver x">Versi X</span></td>
          <td>BPMN 2.0 standard, workflow-focused, transformasi handoff visible</td>
        </tr>
        <tr>
          <td>OPEX / Lean Six Sigma / CI / Data-driven</td>
          <td><span class="ver z">Versi Z</span></td>
          <td>VSM quantified, VA ratio, timeline visual impact -86% lead time</td>
        </tr>
      </tbody>
    </table>
  </div>
```

- [ ] **Step 5: Update Recovery section + footer**

Find `.info-section` "Recovery Versi Lama" and add v3.0, v3.5 to list:

```html
  <div class="info-section">
    <h3>📁 Recovery Versi Lama</h3>
    <ul>
      <li><code>git checkout pcp-hcportal-3.4-v1.0 -- &lt;path&gt;</code> — recover v1.0 (12 swimlane MD)</li>
      <li><code>git checkout pcp-hcportal-3.4-v2.0 -- &lt;path&gt;</code> — recover v2.0 (15 file hybrid)</li>
      <li><code>git checkout pcp-hcportal-3.4-v3.5 -- &lt;path&gt;</code> — recover v3.5 (sebelum X+Z)</li>
    </ul>
  </div>
```

Find footer and update:
```html
  <footer>
    <p>PCP SMART 2026 §3.4 v3.6 — HC Portal • 4 Versi (P/C/X/Z) • Generated 2026-05-22 • Tag <code>pcp-hcportal-3.4-v3.6</code></p>
  </footer>
```

- [ ] **Step 6: Verify index render**

Open `index.html`. Expected: 4 card horizontal (P green, C yellow, X blue, Z red borders), matrix table 4 baris, recovery 3 entry, footer v3.6.

- [ ] **Step 7: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/index.html
git commit -m "feat(pcp-3.4-v3.6): update index.html — 4 versi + audience matrix"
```

---

## Task 14: Update README.md

**Files:**
- Modify: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md`

- [ ] **Step 1: Replace existing README dengan versi updated**

```markdown
# PCP SMART 2026 §3.4 — Solusi Terpilih HC Portal (v3.6)

> **Audience:** Reviewer PCP, manajemen HC, executive review, Process Improvement, Lean Six Sigma.
> **Domain:** HC Portal (PortalHC_KPB) — web app pengelolaan kompetensi CSU Process KPB.

## Executive Summary

HC Portal menggantikan workflow manual berbasis Excel + FleQi + paperwork + email/WhatsApp dengan single web portal terintegrasi. Improvement median lintas 7 fitur: **-67% step**, **-75% tools**, **~95% waktu** + audit trail, SSoT, governance compliance.

## Cakupan §3.4 v3.6

Dokumen ini berisi **4 diagram landscape** untuk reviewer pilih sesuai audience:

### Versi P — Workflow Topology (PRIMARY)

5 layer Purdue-style adaptasi (Manajemen → HC → Atasan → Coach → Pekerja) dengan HC Portal sebagai "Buffer Zone".
**Cocok untuk:** Submission PCP utama, reviewer engineering/operations.
**File:** `versi-p-workflow-topology.html`

### Versi C — Comparison Dashboard (SECONDARY)

Card-grid layout 7 fitur dengan metric Sebelum/Sesudah/Δ%, HC Portal hub showcase.
**Cocok untuk:** Executive review, management showcase.
**File:** `versi-c-comparison-dashboard.html`

### Versi X — BPMN Swimlane As-Is/To-Be (TERTIARY) — NEW

BPMN 2.0 standard (ISO 19510), 4 lane aktor + 1 system lane (HC Portal central hub) di Sesudah. Hero workflow: Assessment (6→2 step). Visual crisscross merah Sebelum vs lurus biru Sesudah.
**Cocok untuk:** Process Improvement / BPM / SOP Review.
**File:** `versi-x-bpmn-swimlane.html`

### Versi Z — BPMN+VSM Hybrid Lean Six Sigma (QUATERNARY) — NEW

3 zona: BPMN compact + Value Stream Map dengan VA classification + 5 KPI metric cards. Timeline stripe linear shared scale 0-58,5j (visual impact -86% LT brutal).
**Cocok untuk:** OPEX / Lean Six Sigma / CI / Data-driven reviewer.
**File:** `versi-z-bpmn-vsm-hybrid.html`

## Matriks Audience → Versi

| Audience | Versi |
|----------|:---:|
| Engineering / Operations PCP main | Versi P |
| Executive / Management showcase | Versi C |
| Process Improvement / BPM / SOP | **Versi X** |
| OPEX / Lean Six Sigma / CI | **Versi Z** |

## Cara Pakai

1. **Buka `index.html`** di browser untuk master viewer + link 4 versi
2. Klik versi yang sesuai audience target
3. Review konten + print/save PDF (tombol 🖨️ di tiap versi)
4. **Redraw manual ke PowerPoint** untuk slide PCP submission

## Hero Workflow (Versi X + Z)

Versi X dan Z fokus **deep-dive 1 fitur Assessment**:
- Sebelum: 6 step manual, 4-5 tools, 9,5j active CT, 58,5j lead time (~7 hari)
- Sesudah: 2 step via HC Portal, 1 tool, 0,35j active CT, 8,35j lead time (~1 hari)
- **Δ:** -67% step, -80% tools, -96% active CT, -86% lead time
- **VA ratio:** 10,5% → 71,4% (+581% lift)
- Issue cover: A B C D (Tools/SSoT/Audit/Reporting)

Versi P + C tetap show coverage **7 fitur lengkap** untuk overview.

## Catatan Data Kuantitatif

Angka cycle/lead time per step = **estimasi rekonstruksi back-calculation** dari data agregat existing (~95% waktu klaim). Refine pasca-implementasi dengan time-motion study.

## Recovery Versi Lama

- Tag `pcp-hcportal-3.4-v1.0` (swimlane-only) — `git checkout`
- Tag `pcp-hcportal-3.4-v2.0` (hybrid Layered + C4 + 7 swimlane) — `git checkout`
- Tag `pcp-hcportal-3.4-v3.0` s/d `v3.5` (Versi P+C iterasi) — `git checkout`

## Referensi

- Slide PCP reference (page 8): `pendukung/reference-pcp-page8.png` + `.txt`
- Spec design v3 (versi P+C): `docs/superpowers/specs/2026-05-21-pcp-hcportal-3.4-v3-design.md`
- Spec design v3.6 (versi X+Z): `docs/superpowers/specs/2026-05-22-pcp-hcportal-3.4-versi-x-z-design.md`
- Plan v3.6 (versi X+Z): `docs/superpowers/plans/2026-05-22-pcp-hcportal-3.4-versi-x-z-implementation.md`
- TKI: `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`
```

- [ ] **Step 2: Verify README markdown render** (preview di VS Code / GitHub)

Expected: 4 versi listed, matrix audience, hero workflow stats, recovery 3 tag tier, referensi 5 file.

- [ ] **Step 3: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md
git commit -m "docs(pcp-3.4-v3.6): update README — 4 versi + matriks audience + hero workflow"
```

---

## Task 15: Final Verification + Tag Release v3.6

**Files:**
- Tag: `pcp-hcportal-3.4-v3.6`

- [ ] **Step 1: Buka 4 file HTML berdampingan di browser**

```
file:///.../versi-p-workflow-topology.html
file:///.../versi-c-comparison-dashboard.html
file:///.../versi-x-bpmn-swimlane.html
file:///.../versi-z-bpmn-vsm-hybrid.html
file:///.../index.html
```

Manual check checklist:
- [ ] Versi X: 4+5 lane swimlane render, markers A B C D + 1,2,3,4,7 tooltip OK, total stripe match (58,5j / 8,35j), tabel aspek 6 baris, tabel issue/improvement 6+7 baris
- [ ] Versi Z: Zona 1 BPMN compact 2 panel, Zona 2 VSM Sebelum 6 process + 5 triangle + timeline 1.7%/14.5%/83.8%, Zona 2 VSM Sesudah 2 process + Sesudah strip ~14%, Zona 3 5 KPI card (LT/VA dengan badge ✅)
- [ ] index.html: 4 card (P/C/X/Z), matrix 4 baris, recovery v1/v2/v3.5
- [ ] README.md preview OK

- [ ] **Step 2: Print preview test (Ctrl+P)**

Versi X + Versi Z → A3 landscape → toolbar hidden, colors preserved, ≤2 page.

- [ ] **Step 3: Cross-check spec acceptance criteria**

Open `docs/superpowers/specs/2026-05-22-pcp-hcportal-3.4-versi-x-z-design.md` section 8. Verify semua checklist Versi X (11 items) + Versi Z (10 items) + Index+README (3 items) + Git (2 items) ✓.

- [ ] **Step 4: Git status clean**

```bash
git status
```

Expected: clean working tree (semua commit task 1-14 sudah masuk).

- [ ] **Step 5: Create annotated tag**

```bash
git tag -a pcp-hcportal-3.4-v3.6 -m "$(cat <<'EOF'
PCP §3.4 v3.6 — Versi X + Versi Z SHIPPED

Tambah 2 versi diagram landscape baru ke set §3.4 (total 4 versi):

Versi X — BPMN Swimlane As-Is/To-Be
- Notation native BPMN 2.0 ISO 19510
- 4 lane aktor + 1 System lane (HC Portal central) Sesudah
- Hybrid multi-data-object Sebelum (5/4/1/1 icons)
- Target: Process Improvement / BPM / SOP review

Versi Z — BPMN+VSM Hybrid Lean Six Sigma
- 3 zona: BPMN compact + VSM strip + 5 KPI cards
- Shared linear timeline 0-58,5j (visual impact -86% LT brutal)
- VA classification Lean orthodox (step 4 cross-check = NVA)
- 5 KPI: LT -86% | CT -96% | VA +581% | Tools -80% | Step -67%
- Target: OPEX / Lean Six Sigma / CI

Hero workflow: Assessment (6→2 step, A B C D cover, Improvement 2,3,4,7)
Data cycle time = rekonstruksi back-calculation dari agregat ~95% existing.

Index + README updated dengan audience matrix 4 versi.
EOF
)"
```

- [ ] **Step 6: Verify tag**

```bash
git tag -l | grep pcp-hcportal-3.4
git show pcp-hcportal-3.4-v3.6 --stat | head -20
```

Expected: tag listed, annotated message visible.

---

## Verification Summary

After Task 15 complete:
- 4 file HTML viewable di browser (P/C/X/Z) + index.html navigation
- README.md updated reference 4 versi + audience matrix
- Tag `pcp-hcportal-3.4-v3.6` annotated created
- 15 atomic commits di main branch
- Spec acceptance criteria 100% covered
- Print preview A3 landscape verified
- No external JS, single-file portable HTML

Catatan untuk user post-execution:
- **Promosi server Dev/Prod:** tidak applicable (file docs/, bukan kode aplikasi)
- **Manual redraw ke PowerPoint:** TBD oleh user untuk slide PCP final
- **Notifikasi IT:** tidak perlu (no migration, no code change)
