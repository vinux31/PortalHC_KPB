# PCP HC Portal §3.4 v3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Membuat 4 file output PCP §3.4 v3 — README + 2 diagram landscape HTML (Versi P Workflow Topology + Versi C Comparison Dashboard) + master index viewer.

**Architecture:** Folder `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/` dengan 4 file flat (no subfolder). HTML standalone dengan inline CSS/SVG. Mermaid CDN tidak diperlukan (semua diagram pakai pure HTML/CSS/SVG). Source-of-truth single-file untuk mudah review + redraw PowerPoint.

**Tech Stack:** HTML5 + CSS3 + SVG (untuk connection lines), inline styling (no framework dependency). Bahasa Indonesia. Git versioning.

**Spec reference:** `docs/superpowers/specs/2026-05-21-pcp-hcportal-3.4-v3-design.md`

---

## Konvensi Umum

**Tone:**
- Header & tagline eksekutif
- Tabel/data/label teknis
- Bahasa Indonesia full

**Color palette (5 warna max, konsisten):**
- `--pertamina-red: #C8102E` — Pain / Issue / Sebelum
- `--pertamina-blue: #00558C` — Portal / Digital / Sesudah
- `--pertamina-green: #00A551` — Improvement / Success
- `--pertamina-yellow: #FFC72C` — Transition / Decision
- `--neutral-gray: #6b7280` — Metadata / muted

**Aktor icons:**
- 👔 Manajemen
- 👤 HC
- 🏢 Atasan
- 🧑‍🏫 Coach
- 👷 Pekerja

**Modul portal icons:**
- 📝 Assessment
- 🎯 PROTON
- 📋 IDP
- 📊 KKJ
- 🏆 Sertifikat
- 📈 Reporting
- 👥 Data Pekerja

**Verifikasi tiap file:** buka di browser, cek render lengkap, no console error, print preview OK.

**Commit convention:** `docs(pcp-3.4-v3): wave<N>/<file> — <summary>`

---

## Wave 1 — README

### Task 1: README.md (root index)

**Files:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md`

- [ ] **Step 1: Pastikan folder ada (akan dibuat saat write)**

Run: `mkdir -p docs/pcp-HCPortal-2026/3.4-solusi-terpilih/`

- [ ] **Step 2: Tulis README.md**

Isi:

```markdown
# PCP SMART 2026 §3.4 — Solusi Terpilih HC Portal (v3)

> **Audience:** Reviewer PCP, manajemen HC, executive review.
> **Domain:** HC Portal (PortalHC_KPB) — web app pengelolaan kompetensi CSU Process KPB.

## Executive Summary

HC Portal menggantikan workflow manual berbasis Excel + FleQi + paperwork + email/WhatsApp dengan single web portal terintegrasi. Improvement median lintas 7 fitur: **-67% step**, **-75% tools**, **~95% waktu** + audit trail, SSoT, governance compliance.

## Cakupan §3.4 v3

Dokumen ini berisi **2 diagram landscape** untuk reviewer pilih (lean — fokus pada visualisasi utama, tanpa lampiran detail per fitur):

### Versi P — Workflow Topology (PRIMARY)

5 layer Purdue-style adaptasi (Manajemen → HC → Atasan → Coach → Pekerja) dengan:
- HC Portal sebagai "Buffer Zone" pemisah (analog DMZ di slide PCP referensi)
- Connection lines eksplisit menunjukkan data flow
- Issue marker A-F di Sebelum, Improvement marker 1-7 di Sesudah
- Tabel komparasi inline

**Cocok untuk:** Submission PCP utama, reviewer engineering/operations.
**File:** `versi-p-workflow-topology.html`

### Versi C — Comparison Dashboard (SECONDARY)

Card-grid layout dengan 7 fitur dalam card, masing-masing menampilkan:
- Icon + nama + metric Sebelum/Sesudah/Δ%
- Color-coded border per range improvement
- HC Portal hub showcase di bawah
- Tabel issue A-F + matriks coverage inline

**Cocok untuk:** Executive review, management showcase, presentation singkat.
**File:** `versi-c-comparison-dashboard.html`

## Cara Pakai

1. **Buka `index.html`** di browser untuk master viewer + link 2 versi
2. **Buka `versi-p-workflow-topology.html`** untuk Versi P
3. **Buka `versi-c-comparison-dashboard.html`** untuk Versi C
4. **Pilih versi** sesuai audience
5. **Redraw manual ke PowerPoint** untuk slide PCP submission

## Catatan Data Kuantitatif

Angka kuantitatif (step, waktu, %) = **estimasi internal** berdasarkan inventory workflow manual + observasi proses HC. Refine pasca-implementasi dengan data riil.

## Recovery v1.0 & v2.0

- Tag `pcp-hcportal-3.4-v1.0` (swimlane-only) — recoverable via `git checkout`
- Tag `pcp-hcportal-3.4-v2.0` (hybrid Layered + C4 + 7 swimlane) — recoverable via `git checkout`

## Referensi

- Slide PCP template: `C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Documents\PCP SMART 2026 APQ Rev 9999 Final_1.png`
- Spec design v3: `docs/superpowers/specs/2026-05-21-pcp-hcportal-3.4-v3-design.md`
- Plan v3: `docs/superpowers/plans/2026-05-21-pcp-hcportal-3.4-v3-implementation.md`
- TKI: `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`
```

- [ ] **Step 3: Verify**

Open file. Confirm format Markdown render rapi, link valid.

- [ ] **Step 4: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md
git commit -m "docs(pcp-3.4-v3): wave1/README — index + 2 versi rationale + recovery info"
```

---

## Wave 2 — Versi P (Workflow Topology)

### Task 2: versi-p-workflow-topology.html

**Files:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html`

- [ ] **Step 1: Tulis file**

Isi (HTML standalone enhanced, ~600 lines):

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>§3.4 v3 — Versi P: Workflow Topology HC Portal</title>
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
    --bg: #f6f7fb;
    --border: #d1d5db;
    --hub-grad: linear-gradient(135deg, #00558C, #00A551);
  }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: #1f2937; padding: 1.5rem; }

  /* HEADER BAR with logo placeholder */
  .header-bar {
    max-width: 1400px;
    margin: 0 auto 1rem;
    display: grid;
    grid-template-columns: 160px 1fr 160px;
    align-items: center;
    background: white;
    border-radius: .75rem;
    box-shadow: 0 2px 10px rgba(0,0,0,.06);
    padding: 1rem 1.5rem;
    border-top: 5px solid var(--pertamina-red);
  }
  .logo-pertamina {
    font-weight: 900;
    font-size: 1.1rem;
    color: var(--pertamina-red);
    line-height: 1.1;
  }
  .logo-pertamina small { display:block; font-weight: 600; font-size: .7rem; color: var(--neutral-gray); }
  .header-title { text-align: center; }
  .header-title h1 { margin: 0; font-size: 1.4rem; color: var(--pertamina-blue); }
  .header-title small { display:block; color: var(--neutral-gray); font-size: .85rem; margin-top: .15rem; }
  .pcp-badge {
    text-align: right;
    background: var(--pertamina-blue);
    color: white;
    padding: .5rem .75rem;
    border-radius: .35rem;
    font-weight: 700;
    font-size: .85rem;
  }
  .pcp-badge small { display:block; font-weight: 400; font-size: .65rem; opacity: .9; }

  /* AUDIENCE + TUJUAN */
  .meta-bar {
    max-width: 1400px;
    margin: 0 auto 1.5rem;
    background: white;
    border-radius: .5rem;
    padding: .75rem 1.25rem;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
    font-size: .85rem;
    border-left: 4px solid var(--pertamina-yellow);
  }
  .meta-bar b { color: var(--pertamina-blue); }

  /* TOOLBAR */
  .toolbar {
    max-width: 1400px;
    margin: 0 auto 1.5rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: .75rem 1.25rem;
    background: white;
    border-radius: .5rem;
    box-shadow: 0 1px 3px rgba(0,0,0,.06);
  }
  .toolbar .note { font-size: .85rem; color: var(--neutral-gray); }
  .toolbar button { background: var(--pertamina-red); color: white; border: none; padding: .5rem 1rem; border-radius: .35rem; font-weight: 600; cursor: pointer; }

  /* DIAGRAM WRAP */
  .diagram-wrap {
    max-width: 1400px;
    margin: 0 auto 2.5rem;
    background: white;
    border-radius: 1rem;
    box-shadow: 0 6px 30px rgba(0,0,0,.08);
    padding: 1.5rem 2rem;
    position: relative;
  }
  .diagram-wrap.before-style { border-top: 6px solid var(--pertamina-red); }
  .diagram-wrap.after-style { border-top: 6px solid var(--pertamina-green); }

  .diagram-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
    padding-bottom: .75rem;
    border-bottom: 2px dashed var(--border);
  }
  .diagram-header h2 { margin: 0; font-size: 1.5rem; }
  .before-style .diagram-header h2 { color: var(--pertamina-red); }
  .after-style .diagram-header h2 { color: var(--pertamina-blue); }
  .diagram-header .tagline { font-style: italic; color: var(--neutral-gray); font-size: .85rem; }

  /* LAYER ROW */
  .layer-row {
    display: grid;
    grid-template-columns: 200px 1fr;
    gap: 1rem;
    margin-bottom: .75rem;
    align-items: stretch;
    position: relative;
  }
  .layer-label {
    background: #f3f4f6;
    border-left: 5px solid var(--pertamina-blue);
    padding: .85rem;
    border-radius: .35rem;
    display: flex;
    flex-direction: column;
    justify-content: center;
    text-align: center;
    font-weight: 600;
  }
  .layer-label .icon { font-size: 2rem; margin-bottom: .25rem; }
  .layer-label .level-num { font-size: 1.3rem; color: var(--pertamina-blue); line-height: 1; }
  .layer-label .level-name { font-size: .75rem; color: var(--neutral-gray); text-transform: uppercase; letter-spacing: .05em; margin-top: .25rem; }
  .layer-label .level-actor { font-size: 1.05rem; margin-top: .35rem; }

  .layer-content {
    display: flex;
    flex-wrap: wrap;
    gap: .65rem;
    align-items: center;
    background: #fafbfc;
    border: 1px solid var(--border);
    border-radius: .35rem;
    padding: .85rem;
    position: relative;
  }

  /* COMPONENT chips */
  .comp {
    padding: .5rem .85rem;
    border-radius: .4rem;
    font-size: .85rem;
    font-weight: 500;
    box-shadow: 0 1px 3px rgba(0,0,0,.08);
    border: 1px solid;
    white-space: nowrap;
    display: inline-flex;
    align-items: center;
    gap: .3rem;
    position: relative;
  }
  .comp.manual { background: var(--pertamina-red-light); border-color: var(--pertamina-red); color: var(--pertamina-red); }
  .comp.portal { background: var(--pertamina-blue-light); border-color: var(--pertamina-blue); color: var(--pertamina-blue-dark); }
  .comp.tool-ext { background: #fff3cd; border-color: #d4a017; color: #856404; font-style: italic; }
  .comp.paper { background: #e8e8e8; border-color: #999; color: #555; }

  /* BUFFER ZONE (DMZ-analog) */
  .buffer-zone {
    display: grid;
    grid-template-columns: 200px 1fr;
    gap: 1rem;
    margin: 1.25rem 0;
    position: relative;
  }
  .buffer-zone-label {
    background: linear-gradient(135deg, var(--pertamina-yellow), #ffa500);
    color: #5a4400;
    padding: .85rem;
    border-radius: .5rem;
    text-align: center;
    font-weight: 700;
    font-size: .85rem;
    box-shadow: 0 2px 8px rgba(255,199,44,.4);
    display: flex;
    flex-direction: column;
    justify-content: center;
  }
  .buffer-zone-label small { font-size: .7rem; opacity: .85; font-weight: 600; }
  .portal-hub {
    background: var(--hub-grad);
    color: white;
    padding: 1.25rem 1.5rem;
    border-radius: .75rem;
    text-align: center;
    font-weight: 700;
    font-size: 1.2rem;
    box-shadow: 0 6px 18px rgba(0,85,140,.3);
    position: relative;
  }
  .portal-hub small { display: block; font-size: .75rem; font-weight: 400; opacity: .95; margin-top: .35rem; }
  .portal-hub .marker { background: white; color: var(--pertamina-blue); margin-left: .5rem; }

  /* MARKERS */
  .marker {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
    border-radius: 50%;
    color: white;
    font-weight: 700;
    font-size: .75rem;
    margin-left: .35rem;
    cursor: help;
    transition: transform .15s;
  }
  .marker:hover { transform: scale(1.2); }
  .marker.issue { background: var(--pertamina-red); }
  .marker.improvement { background: var(--pertamina-green); }
  .marker[title] { position: relative; }

  /* CONNECTION LINES (decorative pseudo) */
  .layer-content::after {
    content: '';
    position: absolute;
    left: -16px;
    top: 50%;
    width: 12px;
    height: 2px;
    background: var(--pertamina-blue);
    opacity: .3;
  }
  .before-style .layer-content::after { background: var(--pertamina-red); }

  /* LEGEND tables */
  .legend-table { width: 100%; border-collapse: collapse; margin-top: 1.5rem; font-size: .85rem; }
  .legend-table th { background: var(--pertamina-blue); color: white; text-align: left; padding: .55rem .75rem; }
  .legend-table td { padding: .55rem .75rem; border-bottom: 1px solid var(--border); vertical-align: top; }
  .legend-table td:first-child { width: 50px; text-align: center; }
  .legend-marker {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 30px;
    height: 30px;
    border-radius: 50%;
    color: white;
    font-weight: 700;
    font-size: .85rem;
  }
  .legend-marker.issue { background: var(--pertamina-red); }
  .legend-marker.improvement { background: var(--pertamina-green); }

  /* TABEL KOMPARASI INLINE */
  .komparasi-section { max-width: 1400px; margin: 0 auto 2.5rem; background: white; border-radius: 1rem; box-shadow: 0 6px 30px rgba(0,0,0,.08); padding: 1.5rem 2rem; border-top: 6px solid var(--pertamina-yellow); }
  .komparasi-section h2 { margin: 0 0 1rem; color: #856404; }

  /* TRANSFORMATION arrow */
  .transformation-arrow {
    max-width: 1400px;
    margin: 0 auto 2rem;
    text-align: center;
    font-size: 2.5rem;
    color: var(--pertamina-blue);
  }
  .transformation-arrow small {
    display: block;
    font-size: .85rem;
    color: var(--neutral-gray);
    margin-top: .25rem;
    font-style: italic;
  }

  /* FOOTER */
  .footer { text-align:center; padding:1.5rem; color:var(--neutral-gray); font-size:.85rem; max-width: 1400px; margin: 0 auto; }
  .footer code { background: white; padding: .15rem .4rem; border-radius: 3px; }

  @media print {
    body { padding: 0; background: white; }
    .toolbar { display: none; }
    .diagram-wrap, .komparasi-section { box-shadow: none; page-break-inside: avoid; }
    .header-bar { box-shadow: none; }
  }
</style>
</head>
<body>

<!-- HEADER BAR -->
<div class="header-bar">
  <div class="logo-pertamina">PERTAMINA<br/><small>PATRA NIAGA</small></div>
  <div class="header-title">
    <h1>§3.4 Solusi Terpilih — HC Portal</h1>
    <small>Versi P: Workflow Topology (Purdue-Style Adaptation)</small>
  </div>
  <div class="pcp-badge">PCP SMART<br/><small>2026</small></div>
</div>

<!-- AUDIENCE + TUJUAN -->
<div class="meta-bar">
  <div>🎯 <b>Audience:</b> Reviewer PCP, manajemen HC, tim engineering</div>
  <div>📌 <b>Tujuan:</b> Tunjukkan transformasi workflow HC dari multi-tools manual ke single portal terintegrasi</div>
</div>

<!-- TOOLBAR -->
<div class="toolbar">
  <div class="note">📐 <b>Versi P (Primary)</b> — 5 layer aktor + HC Portal sebagai DMZ-analog buffer zone</div>
  <button onclick="window.print()">🖨️ Print / Save PDF</button>
</div>

<!-- SEBELUM -->
<div class="diagram-wrap before-style">
  <div class="diagram-header">
    <h2>❌ Sebelum (Kondisi Aktual)</h2>
    <div class="tagline">Workflow manual tersebar — 4-5 tools tanpa integrasi</div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">👔</div>
      <div class="level-num">Level 5</div>
      <div class="level-name">Strategic</div>
      <div class="level-actor">Manajemen</div>
    </div>
    <div class="layer-content">
      <div class="comp manual">📄 Laporan PDF/Excel</div>
      <div class="comp tool-ext">📧 Email Pertamina</div>
      <span class="marker issue" title="D: Reporting Ad-Hoc — laporan manual pivot per request">D</span>
    </div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">👤</div>
      <div class="level-num">Level 4</div>
      <div class="level-name">Governance</div>
      <div class="level-actor">HC</div>
    </div>
    <div class="layer-content">
      <div class="comp manual">📊 Excel Master Pekerja</div>
      <div class="comp manual">📊 Excel Master Assessment</div>
      <div class="comp manual">📊 Excel Master Training</div>
      <div class="comp manual">📊 Excel Master KKJ</div>
      <div class="comp manual">📊 Excel Master Sertifikat</div>
      <div class="comp manual">📝 Word Template</div>
      <span class="marker issue" title="A: Tools Terfragmentasi — workflow tersebar di 4-5 aplikasi">A</span>
      <span class="marker issue" title="B: No Single Source of Truth — data dicopy ke beberapa Excel">B</span>
    </div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">🏢</div>
      <div class="level-num">Level 3</div>
      <div class="level-name">Supervisory</div>
      <div class="level-actor">Atasan</div>
    </div>
    <div class="layer-content">
      <div class="comp tool-ext">📧 Email kotak masuk</div>
      <div class="comp tool-ext">💬 WhatsApp approval lisan</div>
      <span class="marker issue" title="C: No Audit Trail — approval lisan tanpa record">C</span>
      <span class="marker issue" title="E: Workflow tanpa tracking — no status history">E</span>
    </div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">🧑‍🏫</div>
      <div class="level-num">Level 2</div>
      <div class="level-name">Coaching</div>
      <div class="level-actor">Coach</div>
    </div>
    <div class="layer-content">
      <div class="comp paper">📋 Form PROTON cetak</div>
      <div class="comp paper">📁 Arsip fisik (map)</div>
      <div class="comp tool-ext">💬 WhatsApp (bukti foto)</div>
      <div class="comp tool-ext">📧 Email (lampiran)</div>
      <span class="marker issue" title="A: Tools terfragmentasi (paperwork + WA + email)">A</span>
      <span class="marker issue" title="E: Workflow tanpa tracking">E</span>
    </div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">👷</div>
      <div class="level-num">Level 1</div>
      <div class="level-name">Operational</div>
      <div class="level-actor">Pekerja</div>
    </div>
    <div class="layer-content">
      <div class="comp tool-ext">🌐 FleQi Quiz (eksternal)</div>
      <div class="comp paper">🎓 Sertifikat hardcopy</div>
      <div class="comp manual">📊 Excel pribadi (IDP)</div>
      <span class="marker issue" title="A: Tools terfragmentasi">A</span>
      <span class="marker issue" title="F: Renewal Sertifikat Reaktif — expired sering kelewat">F</span>
    </div>
  </div>

  <table class="legend-table">
    <thead><tr><th>Code</th><th>Issue Sebelum</th><th>Lokasi di Diagram</th></tr></thead>
    <tbody>
      <tr><td><span class="legend-marker issue">A</span></td><td><b>Tools Terfragmentasi</b> — workflow tersebar di 4-5 aplikasi/medium berbeda</td><td>Level 1, 2, 4</td></tr>
      <tr><td><span class="legend-marker issue">B</span></td><td><b>No Single Source of Truth</b> — data sama dicopy ke beberapa Excel</td><td>Level 4 (HC)</td></tr>
      <tr><td><span class="legend-marker issue">C</span></td><td><b>No Audit Trail</b> — approval lisan, no record</td><td>Level 3 (Atasan)</td></tr>
      <tr><td><span class="legend-marker issue">D</span></td><td><b>Reporting Ad-Hoc</b> — laporan manual pivot tiap request</td><td>Level 5↔4</td></tr>
      <tr><td><span class="legend-marker issue">E</span></td><td><b>Workflow Tanpa Tracking</b> — coaching/approval no status history</td><td>Level 2-3</td></tr>
      <tr><td><span class="legend-marker issue">F</span></td><td><b>Renewal Sertifikat Reaktif</b> — expired sering kelewat</td><td>Level 1</td></tr>
    </tbody>
  </table>
</div>

<!-- TRANSFORMATION ARROW -->
<div class="transformation-arrow">
  ▼
  <small>Transformasi via HC Portal</small>
</div>

<!-- SESUDAH -->
<div class="diagram-wrap after-style">
  <div class="diagram-header">
    <h2>✅ Sesudah (Konsep Improvement — HC Portal)</h2>
    <div class="tagline">Single portal terpusat — semua aktor lewat hub digital terintegrasi</div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">👔</div>
      <div class="level-num">Level 5</div>
      <div class="level-name">Strategic</div>
      <div class="level-actor">Manajemen</div>
    </div>
    <div class="layer-content">
      <div class="comp portal">📈 Analytics Dashboard</div>
      <div class="comp portal">🔥 Heatmap Gap Kompetensi</div>
      <div class="comp portal">📤 Export Excel/PDF</div>
      <span class="marker improvement" title="1: Analytics Dashboard Real-Time — manajemen self-service">1</span>
    </div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">👤</div>
      <div class="level-num">Level 4</div>
      <div class="level-name">Governance</div>
      <div class="level-actor">HC</div>
    </div>
    <div class="layer-content">
      <div class="comp portal">👥 Kelola Pekerja</div>
      <div class="comp portal">🎯 PROTON Data (IDP)</div>
      <div class="comp portal">📝 Kelola Paket Assessment</div>
      <div class="comp portal">📊 Kelola KKJ</div>
      <div class="comp portal">🔄 Renewal Certificate</div>
      <div class="comp portal">🔍 Audit Log</div>
      <span class="marker improvement" title="2: Master Data Terpusat di 1 portal">2</span>
      <span class="marker improvement" title="3: Audit Log Lengkap untuk compliance">3</span>
    </div>
  </div>

  <!-- BUFFER ZONE (DMZ-analog) -->
  <div class="buffer-zone">
    <div class="buffer-zone-label">
      🛡️ BUFFER ZONE<br/>
      <small>DMZ-Analog<br/>HC Portal Layer</small>
    </div>
    <div class="portal-hub">
      🌐 HC PORTAL — Single Source of Truth
      <small>ASP.NET Core 8 • SQL Server • SignalR • Audit Log • Role-Based Access Control</small>
      <span class="marker improvement" title="4: HC Portal sebagai Hub — eliminasi scattered tools">4</span>
    </div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">🏢</div>
      <div class="level-num">Level 3</div>
      <div class="level-name">Supervisory</div>
      <div class="level-actor">Atasan</div>
    </div>
    <div class="layer-content">
      <div class="comp portal">👀 Records Team</div>
      <div class="comp portal">✅ Approval Deliverable</div>
      <div class="comp portal">📊 View Matriks KKJ Bagian</div>
      <span class="marker improvement" title="5: Workflow Approval Terstruktur Coach→Atasan→HC">5</span>
    </div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">🧑‍🏫</div>
      <div class="level-num">Level 2</div>
      <div class="level-name">Coaching</div>
      <div class="level-actor">Coach</div>
    </div>
    <div class="layer-content">
      <div class="comp portal">🎯 Coaching PROTON (5 fase)</div>
      <div class="comp portal">📎 Upload Evidence</div>
      <div class="comp portal">📜 Histori PROTON</div>
      <span class="marker improvement" title="6: Form digital + Evidence auto-link ke Deliverable">6</span>
    </div>
  </div>

  <div class="layer-row">
    <div class="layer-label">
      <div class="icon">👷</div>
      <div class="level-num">Level 1</div>
      <div class="level-name">Operational</div>
      <div class="level-actor">Pekerja</div>
    </div>
    <div class="layer-content">
      <div class="comp portal">📝 Assessment Online</div>
      <div class="comp portal">📋 Plan IDP</div>
      <div class="comp portal">🏆 Certificate Download</div>
      <div class="comp portal">🔔 Notifikasi In-App</div>
      <span class="marker improvement" title="7: Pekerja Self-Service — ujian + sertifikat + IDP">7</span>
    </div>
  </div>

  <table class="legend-table">
    <thead><tr><th>No</th><th>Improvement</th><th>Deskripsi</th></tr></thead>
    <tbody>
      <tr><td><span class="legend-marker improvement">1</span></td><td><b>Analytics Dashboard Real-Time</b></td><td>Manajemen self-service untuk laporan kompetensi, eliminasi pivot Excel ad-hoc</td></tr>
      <tr><td><span class="legend-marker improvement">2</span></td><td><b>Master Data Terpusat</b></td><td>HC kelola pekerja, KKJ, paket assessment, IDP, training di 1 portal</td></tr>
      <tr><td><span class="legend-marker improvement">3</span></td><td><b>Audit Log Lengkap</b></td><td>Setiap aksi (CRUD, approval, impersonation) tercatat untuk compliance</td></tr>
      <tr><td><span class="legend-marker improvement">4</span></td><td><b>HC Portal sebagai Hub</b></td><td>Single source of truth — semua aktor pakai 1 portal, no scattered tools</td></tr>
      <tr><td><span class="legend-marker improvement">5</span></td><td><b>Workflow Approval Terstruktur</b></td><td>Atasan approve/reject deliverable dengan status history (Coach → Atasan → HC)</td></tr>
      <tr><td><span class="legend-marker improvement">6</span></td><td><b>Coaching Digital + Evidence Link</b></td><td>Form PROTON 5 fase digital + upload evidence auto-link ke deliverable IDP</td></tr>
      <tr><td><span class="legend-marker improvement">7</span></td><td><b>Pekerja Self-Service</b></td><td>Ujian online + auto-grade + download sertifikat + lihat IDP + notifikasi in-app</td></tr>
    </tbody>
  </table>
</div>

<!-- TABEL KOMPARASI INLINE -->
<div class="komparasi-section">
  <h2>📋 Tabel Komparasi Aspek</h2>
  <table class="legend-table" style="margin-top:0;">
    <thead><tr><th style="width:30%;">Aspek</th><th style="width:35%;">Sebelum (Aktual)</th><th style="width:35%;">Sesudah (HC Portal)</th></tr></thead>
    <tbody>
      <tr><td><b>Single Source of Truth</b></td><td>❌ Tidak ada (data tersebar 4-5 Excel)</td><td>✅ Ada — DB SQL Server terpusat</td></tr>
      <tr><td><b>Tools yang Dipakai</b></td><td>❌ 4-5 aplikasi (Excel, FleQi, Word, Email, WA)</td><td>✅ 1 portal — HC Portal</td></tr>
      <tr><td><b>Audit Trail</b></td><td>❌ Tidak ada / catatan manual</td><td>✅ Audit log lengkap (siapa-apa-kapan)</td></tr>
      <tr><td><b>Real-Time Data</b></td><td>❌ Snapshot manual</td><td>✅ Real-time (DB + SignalR)</td></tr>
      <tr><td><b>Workflow Approval</b></td><td>❌ Lisan / WhatsApp, no trail</td><td>✅ Terstruktur Coach→Atasan→HC + status history</td></tr>
      <tr><td><b>Self-Service Manajemen</b></td><td>❌ Bergantung rekap HC</td><td>✅ Dashboard role-based</td></tr>
      <tr><td><b>Versioning Dokumen</b></td><td>❌ Manual (rename file)</td><td>✅ Otomatis (timestamp + GUID)</td></tr>
      <tr><td><b>Renewal Sertifikat</b></td><td>❌ Reaktif</td><td>✅ Proaktif (badge expiry + menu Renewal)</td></tr>
      <tr><td><b>Compliance Posture</b></td><td>❌ Reaktif</td><td>✅ Proaktif + audit-ready</td></tr>
      <tr><td><b>Bulk Operation</b></td><td>❌ Copy-paste manual</td><td>✅ Import Excel + validasi</td></tr>
    </tbody>
  </table>
</div>

<div class="footer">
  <p>📌 <b>Versi P</b> (Workflow Topology) — PCP SMART 2026 §3.4 v3.0 — HC Portal • Tag <code>pcp-hcportal-3.4-v3.0</code></p>
  <p>💡 Hover marker A-F atau 1-7 untuk lihat detail. Klik 🖨️ Print untuk save PDF.</p>
</div>

</body>
</html>
```

- [ ] **Step 2: Verify**

Open file di browser. Konfirmasi:
- Header bar dengan logo + title + PCP badge render
- Audience + Tujuan visible
- 5 layer Sebelum + 5 layer Sesudah
- Buffer Zone label + HC Portal hub di tengah Sesudah
- Marker A-F merah + tooltip hover bekerja
- Marker 1-7 hijau + tooltip hover bekerja
- Tabel komparasi inline di bawah
- Transformation arrow ▼ antara Sebelum & Sesudah
- Print preview bersih (no toolbar)

- [ ] **Step 3: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html
git commit -m "docs(pcp-3.4-v3): wave2/versi-P — Workflow Topology 5 layer + DMZ Buffer Zone + tabel inline + tooltip marker"
```

---

## Wave 3 — Versi C (Comparison Dashboard)

### Task 3: versi-c-comparison-dashboard.html

**Files:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-c-comparison-dashboard.html`

- [ ] **Step 1: Tulis file**

Isi:

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>§3.4 v3 — Versi C: Comparison Dashboard HC Portal</title>
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
    --bg: #f6f7fb;
    --border: #d1d5db;
    --hub-grad: linear-gradient(135deg, #00558C, #00A551);
  }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: #1f2937; padding: 1.5rem; }

  /* HEADER BAR */
  .header-bar {
    max-width: 1400px;
    margin: 0 auto 1rem;
    display: grid;
    grid-template-columns: 160px 1fr 160px;
    align-items: center;
    background: white;
    border-radius: .75rem;
    box-shadow: 0 2px 10px rgba(0,0,0,.06);
    padding: 1rem 1.5rem;
    border-top: 5px solid var(--pertamina-green);
  }
  .logo-pertamina { font-weight: 900; font-size: 1.1rem; color: var(--pertamina-red); line-height: 1.1; }
  .logo-pertamina small { display:block; font-weight: 600; font-size: .7rem; color: var(--neutral-gray); }
  .header-title { text-align: center; }
  .header-title h1 { margin: 0; font-size: 1.4rem; color: var(--pertamina-blue); }
  .header-title small { display:block; color: var(--neutral-gray); font-size: .85rem; margin-top: .15rem; }
  .pcp-badge { text-align: right; background: var(--pertamina-blue); color: white; padding: .5rem .75rem; border-radius: .35rem; font-weight: 700; font-size: .85rem; }
  .pcp-badge small { display:block; font-weight: 400; font-size: .65rem; opacity: .9; }

  /* META */
  .meta-bar {
    max-width: 1400px;
    margin: 0 auto 1.5rem;
    background: white;
    border-radius: .5rem;
    padding: .75rem 1.25rem;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
    font-size: .85rem;
    border-left: 4px solid var(--pertamina-green);
  }
  .meta-bar b { color: var(--pertamina-blue); }

  /* TOOLBAR */
  .toolbar { max-width: 1400px; margin: 0 auto 1.5rem; display: flex; justify-content: space-between; align-items: center; padding: .75rem 1.25rem; background: white; border-radius: .5rem; box-shadow: 0 1px 3px rgba(0,0,0,.06); }
  .toolbar .note { font-size: .85rem; color: var(--neutral-gray); }
  .toolbar button { background: var(--pertamina-red); color: white; border: none; padding: .5rem 1rem; border-radius: .35rem; font-weight: 600; cursor: pointer; }

  /* CARD GRID */
  .dashboard-wrap {
    max-width: 1400px;
    margin: 0 auto 2rem;
    background: white;
    border-radius: 1rem;
    box-shadow: 0 6px 30px rgba(0,0,0,.08);
    padding: 2rem;
  }
  .dashboard-wrap h2 { margin: 0 0 1.5rem; color: var(--pertamina-blue); border-bottom: 3px solid var(--pertamina-red); padding-bottom: .5rem; display: inline-block; }

  .cards-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1rem;
    margin-bottom: 2rem;
  }
  .feature-card {
    background: white;
    border-radius: .75rem;
    padding: 1.25rem;
    box-shadow: 0 2px 8px rgba(0,0,0,.06);
    border-top: 5px solid var(--pertamina-blue);
    transition: transform .15s, box-shadow .15s;
  }
  .feature-card:hover { transform: translateY(-3px); box-shadow: 0 6px 18px rgba(0,0,0,.1); }
  .feature-card.high-impact { border-top-color: var(--pertamina-green); }
  .feature-card.med-impact { border-top-color: var(--pertamina-yellow); }
  .feature-card.low-impact { border-top-color: var(--neutral-gray); }
  .feature-card h3 { margin: 0 0 .5rem; font-size: 1.1rem; display: flex; align-items: center; gap: .5rem; }
  .feature-card .icon-big { font-size: 1.8rem; }
  .metric-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: .65rem;
    margin: .75rem 0;
  }
  .metric-box {
    padding: .5rem .65rem;
    border-radius: .35rem;
    text-align: center;
    font-size: .85rem;
  }
  .metric-box.before { background: var(--pertamina-red-light); color: var(--pertamina-red); border: 1px solid var(--pertamina-red); }
  .metric-box.after { background: var(--pertamina-blue-light); color: var(--pertamina-blue-dark); border: 1px solid var(--pertamina-blue); }
  .metric-box .label { display: block; font-size: .65rem; font-weight: 700; text-transform: uppercase; opacity: .8; margin-bottom: .15rem; }
  .metric-box .value { font-weight: 700; font-size: .95rem; }

  .delta-row {
    display: flex;
    justify-content: space-around;
    margin: .75rem 0;
    padding: .65rem;
    background: var(--pertamina-green-light);
    border-radius: .35rem;
    border: 1px solid var(--pertamina-green);
  }
  .delta-item { text-align: center; }
  .delta-item .label { display: block; font-size: .65rem; color: var(--neutral-gray); text-transform: uppercase; font-weight: 700; }
  .delta-item .value { font-weight: 800; font-size: 1rem; color: var(--pertamina-green); }
  .delta-item .arrow { color: var(--pertamina-green); font-weight: 900; }

  .issue-tags {
    display: flex;
    flex-wrap: wrap;
    gap: .35rem;
    margin-top: .65rem;
  }
  .issue-tag {
    background: var(--pertamina-red);
    color: white;
    font-size: .7rem;
    font-weight: 700;
    padding: .15rem .4rem;
    border-radius: 3px;
  }

  /* AGREGAT BOX */
  .agregat-box {
    background: linear-gradient(135deg, var(--pertamina-yellow), #ffa500);
    color: #5a4400;
    padding: 1.5rem;
    border-radius: .75rem;
    box-shadow: 0 4px 14px rgba(255,199,44,.4);
  }
  .agregat-box h3 { margin: 0 0 1rem; font-size: 1.15rem; }
  .agregat-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 1rem;
  }
  .agregat-item { text-align: center; }
  .agregat-item .metric { font-size: 2rem; font-weight: 900; line-height: 1; }
  .agregat-item .desc { font-size: .8rem; margin-top: .35rem; }

  /* HUB SHOWCASE */
  .hub-showcase {
    background: var(--hub-grad);
    color: white;
    padding: 2rem;
    border-radius: 1rem;
    text-align: center;
    box-shadow: 0 6px 20px rgba(0,85,140,.3);
    margin: 2rem 0;
  }
  .hub-showcase h2 { margin: 0 0 .5rem; color: white; border: none; }
  .hub-showcase .tagline { font-style: italic; opacity: .95; margin-bottom: 1rem; }
  .stack-pills {
    display: flex;
    flex-wrap: wrap;
    gap: .65rem;
    justify-content: center;
  }
  .stack-pill {
    background: rgba(255,255,255,.2);
    padding: .4rem .85rem;
    border-radius: 999px;
    font-size: .85rem;
    font-weight: 600;
  }

  /* ISSUE TABLE */
  .issue-section { max-width: 1400px; margin: 0 auto 2rem; background: white; border-radius: 1rem; box-shadow: 0 6px 30px rgba(0,0,0,.08); padding: 2rem; }
  .issue-section h2 { margin: 0 0 1rem; color: var(--pertamina-red); border-bottom: 3px solid var(--pertamina-red); padding-bottom: .5rem; display: inline-block; }
  .issue-table { width: 100%; border-collapse: collapse; font-size: .9rem; margin-bottom: 1.5rem; }
  .issue-table th { background: var(--pertamina-red); color: white; padding: .55rem .75rem; text-align: left; }
  .issue-table td { padding: .55rem .75rem; border-bottom: 1px solid var(--border); vertical-align: top; }
  .issue-table .code { font-weight: 800; color: var(--pertamina-red); text-align: center; width: 50px; }

  .coverage-table { width: 100%; border-collapse: collapse; font-size: .85rem; }
  .coverage-table th { background: var(--pertamina-blue); color: white; padding: .5rem; text-align: center; }
  .coverage-table th:first-child { text-align: left; }
  .coverage-table td { padding: .45rem .5rem; border: 1px solid var(--border); text-align: center; }
  .coverage-table td:first-child { text-align: left; font-weight: 600; }
  .coverage-table .yes { background: var(--pertamina-green-light); color: var(--pertamina-green); font-weight: 800; }
  .coverage-table .no { background: #f3f4f6; color: var(--neutral-gray); }

  /* FOOTER */
  .footer { text-align:center; padding:1.5rem; color:var(--neutral-gray); font-size:.85rem; max-width: 1400px; margin: 0 auto; }
  .footer code { background: white; padding: .15rem .4rem; border-radius: 3px; }

  @media print {
    body { padding: 0; background: white; }
    .toolbar { display: none; }
    .dashboard-wrap, .issue-section, .hub-showcase { box-shadow: none; page-break-inside: avoid; }
    .header-bar { box-shadow: none; }
    .feature-card:hover { transform: none; }
  }
</style>
</head>
<body>

<!-- HEADER BAR -->
<div class="header-bar">
  <div class="logo-pertamina">PERTAMINA<br/><small>PATRA NIAGA</small></div>
  <div class="header-title">
    <h1>§3.4 Solusi Terpilih — HC Portal</h1>
    <small>Versi C: Comparison Dashboard (Executive View)</small>
  </div>
  <div class="pcp-badge">PCP SMART<br/><small>2026</small></div>
</div>

<!-- AUDIENCE + TUJUAN -->
<div class="meta-bar">
  <div>🎯 <b>Audience:</b> Executive review, manajemen senior, presentation singkat</div>
  <div>📌 <b>Tujuan:</b> Tunjukkan impact kuantitatif HC Portal lintas 7 fitur dalam 1 dashboard view</div>
</div>

<!-- TOOLBAR -->
<div class="toolbar">
  <div class="note">📊 <b>Versi C (Secondary)</b> — Card grid 7 fitur + agregat impact + HC Portal hub showcase</div>
  <button onclick="window.print()">🖨️ Print / Save PDF</button>
</div>

<!-- DASHBOARD CARD GRID -->
<div class="dashboard-wrap">
  <h2>🎯 7 Fitur Impactful — Comparison Card</h2>

  <div class="cards-grid">
    <!-- 01 Assessment -->
    <div class="feature-card high-impact">
      <h3><span class="icon-big">📝</span> 01 Assessment Online</h3>
      <div class="metric-row">
        <div class="metric-box before">
          <span class="label">Sebelum</span>
          <span class="value">6 step</span>
        </div>
        <div class="metric-box after">
          <span class="label">Sesudah</span>
          <span class="value">2 step</span>
        </div>
      </div>
      <div class="delta-row">
        <div class="delta-item"><span class="label">Step</span><span class="value">-67% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Tools</span><span class="value">-75% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Waktu</span><span class="value">~95% <span class="arrow">⬇</span></span></div>
      </div>
      <div class="issue-tags"><span class="issue-tag">A</span><span class="issue-tag">B</span><span class="issue-tag">C</span><span class="issue-tag">D</span></div>
    </div>

    <!-- 02 PROTON -->
    <div class="feature-card high-impact">
      <h3><span class="icon-big">🎯</span> 02 PROTON Coaching</h3>
      <div class="metric-row">
        <div class="metric-box before"><span class="label">Sebelum</span><span class="value">5 step</span></div>
        <div class="metric-box after"><span class="label">Sesudah</span><span class="value">2 step</span></div>
      </div>
      <div class="delta-row">
        <div class="delta-item"><span class="label">Step</span><span class="value">-60% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Tools</span><span class="value">-75% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Waktu</span><span class="value">~95% <span class="arrow">⬇</span></span></div>
      </div>
      <div class="issue-tags"><span class="issue-tag">A</span><span class="issue-tag">C</span><span class="issue-tag">E</span></div>
    </div>

    <!-- 03 IDP -->
    <div class="feature-card high-impact">
      <h3><span class="icon-big">📋</span> 03 IDP / Plan</h3>
      <div class="metric-row">
        <div class="metric-box before"><span class="label">Sebelum</span><span class="value">4 step</span></div>
        <div class="metric-box after"><span class="label">Sesudah</span><span class="value">1 step</span></div>
      </div>
      <div class="delta-row">
        <div class="delta-item"><span class="label">Step</span><span class="value">-75% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Tools</span><span class="value">-67% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Waktu</span><span class="value">~94% <span class="arrow">⬇</span></span></div>
      </div>
      <div class="issue-tags"><span class="issue-tag">A</span><span class="issue-tag">B</span><span class="issue-tag">E</span></div>
    </div>

    <!-- 04 KKJ -->
    <div class="feature-card high-impact">
      <h3><span class="icon-big">📊</span> 04 KKJ & Matriks</h3>
      <div class="metric-row">
        <div class="metric-box before"><span class="label">Sebelum</span><span class="value">4 step</span></div>
        <div class="metric-box after"><span class="label">Sesudah</span><span class="value">1 step</span></div>
      </div>
      <div class="delta-row">
        <div class="delta-item"><span class="label">Step</span><span class="value">-75% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Tools</span><span class="value">-67% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Waktu</span><span class="value">~99% <span class="arrow">⬇</span></span></div>
      </div>
      <div class="issue-tags"><span class="issue-tag">A</span><span class="issue-tag">B</span><span class="issue-tag">D</span></div>
    </div>

    <!-- 05 Sertifikat -->
    <div class="feature-card high-impact">
      <h3><span class="icon-big">🏆</span> 05 Sertifikat & Renewal</h3>
      <div class="metric-row">
        <div class="metric-box before"><span class="label">Sebelum</span><span class="value">6 step</span></div>
        <div class="metric-box after"><span class="label">Sesudah</span><span class="value">2 step</span></div>
      </div>
      <div class="delta-row">
        <div class="delta-item"><span class="label">Step</span><span class="value">-67% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Tools</span><span class="value">-75% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Waktu</span><span class="value">~99% <span class="arrow">⬇</span></span></div>
      </div>
      <div class="issue-tags"><span class="issue-tag">A</span><span class="issue-tag">C</span><span class="issue-tag">F</span></div>
    </div>

    <!-- 06 Reporting -->
    <div class="feature-card high-impact">
      <h3><span class="icon-big">📈</span> 06 Reporting / Analytics</h3>
      <div class="metric-row">
        <div class="metric-box before"><span class="label">Sebelum</span><span class="value">5 step</span></div>
        <div class="metric-box after"><span class="label">Sesudah</span><span class="value">2 step</span></div>
      </div>
      <div class="delta-row">
        <div class="delta-item"><span class="label">Step</span><span class="value">-60% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Tools</span><span class="value">-67% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Waktu</span><span class="value">~96% <span class="arrow">⬇</span></span></div>
      </div>
      <div class="issue-tags"><span class="issue-tag">B</span><span class="issue-tag">D</span></div>
    </div>

    <!-- 07 Data Pekerja -->
    <div class="feature-card high-impact">
      <h3><span class="icon-big">👥</span> 07 Data Pekerja</h3>
      <div class="metric-row">
        <div class="metric-box before"><span class="label">Sebelum</span><span class="value">6 step</span></div>
        <div class="metric-box after"><span class="label">Sesudah</span><span class="value">1 step</span></div>
      </div>
      <div class="delta-row">
        <div class="delta-item"><span class="label">Step</span><span class="value">-83% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Tools</span><span class="value">-80% <span class="arrow">⬇</span></span></div>
        <div class="delta-item"><span class="label">Waktu</span><span class="value">~83% <span class="arrow">⬇</span></span></div>
      </div>
      <div class="issue-tags"><span class="issue-tag">A</span><span class="issue-tag">B</span><span class="issue-tag">C</span></div>
    </div>

    <!-- AGREGAT BOX -->
    <div class="agregat-box">
      <h3>📊 Agregat Impact (Median)</h3>
      <div class="agregat-grid">
        <div class="agregat-item">
          <div class="metric">-67%</div>
          <div class="desc">Step Process</div>
        </div>
        <div class="agregat-item">
          <div class="metric">-75%</div>
          <div class="desc">Tools Count</div>
        </div>
        <div class="agregat-item">
          <div class="metric">~95%</div>
          <div class="desc">Time Saved</div>
        </div>
      </div>
    </div>
  </div>

  <!-- HUB SHOWCASE -->
  <div class="hub-showcase">
    <h2>🌐 HC PORTAL — Single Source of Truth</h2>
    <div class="tagline">Web application terintegrasi yang menyatukan 7 fitur kompetensi HC dalam 1 platform</div>
    <div class="stack-pills">
      <span class="stack-pill">⚙️ ASP.NET Core 8</span>
      <span class="stack-pill">🗄️ SQL Server</span>
      <span class="stack-pill">⚡ SignalR Real-Time</span>
      <span class="stack-pill">🔍 Audit Log</span>
      <span class="stack-pill">🔐 Role-Based Access</span>
      <span class="stack-pill">⏰ Hangfire Background</span>
    </div>
  </div>
</div>

<!-- ISSUE A-F TABLE + COVERAGE MATRIX -->
<div class="issue-section">
  <h2>🛠️ Issue Pain Point & Coverage Matrix</h2>

  <table class="issue-table">
    <thead><tr><th style="width:50px;">Code</th><th style="width:30%;">Issue Sebelum</th><th>Mitigated by HC Portal</th></tr></thead>
    <tbody>
      <tr><td class="code">A</td><td><b>Tools Terfragmentasi</b></td><td>Konsolidasi ke 1 portal — eliminasi Excel scattered, FleQi, Word, paperwork, WA koordinasi</td></tr>
      <tr><td class="code">B</td><td><b>No Single Source of Truth</b></td><td>DB SQL Server terpusat dengan foreign key linking antar entity</td></tr>
      <tr><td class="code">C</td><td><b>No Audit Trail</b></td><td>Audit log seluruh aksi (CRUD, approval, impersonation) + timestamp</td></tr>
      <tr><td class="code">D</td><td><b>Reporting Ad-Hoc</b></td><td>Analytics Dashboard real-time + export Excel/PDF on-demand</td></tr>
      <tr><td class="code">E</td><td><b>Workflow Tanpa Tracking</b></td><td>Status history Coach → Atasan → HC tersimpan di DB</td></tr>
      <tr><td class="code">F</td><td><b>Renewal Sertifikat Reaktif</b></td><td>Badge expiry otomatis + menu Renewal Certificate + planning export</td></tr>
    </tbody>
  </table>

  <h3 style="color: var(--pertamina-blue);">Matriks Coverage — Fitur × Issue</h3>
  <table class="coverage-table">
    <thead>
      <tr>
        <th>Issue \ Fitur</th>
        <th>01 Assessment</th><th>02 PROTON</th><th>03 IDP</th><th>04 KKJ</th><th>05 Sertifikat</th><th>06 Reporting</th><th>07 Data Pekerja</th>
      </tr>
    </thead>
    <tbody>
      <tr><td><b>A</b> Tools</td><td class="yes">✓</td><td class="yes">✓</td><td class="yes">✓</td><td class="yes">✓</td><td class="yes">✓</td><td class="no">—</td><td class="yes">✓</td></tr>
      <tr><td><b>B</b> SSoT</td><td class="yes">✓</td><td class="no">—</td><td class="yes">✓</td><td class="yes">✓</td><td class="no">—</td><td class="yes">✓</td><td class="yes">✓</td></tr>
      <tr><td><b>C</b> Audit</td><td class="yes">✓</td><td class="yes">✓</td><td class="no">—</td><td class="no">—</td><td class="yes">✓</td><td class="no">—</td><td class="yes">✓</td></tr>
      <tr><td><b>D</b> Reporting</td><td class="yes">✓</td><td class="no">—</td><td class="no">—</td><td class="yes">✓</td><td class="no">—</td><td class="yes">✓</td><td class="no">—</td></tr>
      <tr><td><b>E</b> Workflow</td><td class="no">—</td><td class="yes">✓</td><td class="yes">✓</td><td class="no">—</td><td class="no">—</td><td class="no">—</td><td class="no">—</td></tr>
      <tr><td><b>F</b> Renewal</td><td class="no">—</td><td class="no">—</td><td class="no">—</td><td class="no">—</td><td class="yes">✓</td><td class="no">—</td><td class="no">—</td></tr>
    </tbody>
  </table>
</div>

<div class="footer">
  <p>📌 <b>Versi C</b> (Comparison Dashboard) — PCP SMART 2026 §3.4 v3.0 — HC Portal • Tag <code>pcp-hcportal-3.4-v3.0</code></p>
  <p>💡 Hover card untuk highlight. Klik 🖨️ Print untuk save PDF.</p>
</div>

</body>
</html>
```

- [ ] **Step 2: Verify**

Open di browser. Konfirmasi:
- Header bar render
- 7 feature card grid layout
- Tiap card: icon + nama + 2 metric box + delta row + issue tags
- Agregat box dengan median values (-67%, -75%, ~95%)
- HC Portal hub showcase di bawah card
- Tabel Issue A-F + matriks coverage berwarna
- Hover card menaikkan elevation
- Print preview bersih

- [ ] **Step 3: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-c-comparison-dashboard.html
git commit -m "docs(pcp-3.4-v3): wave3/versi-C — Comparison Dashboard 7 card grid + agregat + hub showcase + coverage matrix"
```

---

## Wave 4 — Master Index

### Task 4: index.html (minimal master viewer)

**Files:**
- Create: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/index.html`

- [ ] **Step 1: Tulis file**

Isi:

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>PCP §3.4 v3 — Master Index HC Portal</title>
<style>
  :root {
    --pertamina-red: #C8102E;
    --pertamina-blue: #00558C;
    --pertamina-green: #00A551;
    --pertamina-yellow: #FFC72C;
    --bg: #f6f7fb;
    --neutral-gray: #6b7280;
    --hub-grad: linear-gradient(135deg, #00558C, #00A551);
  }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: #1f2937; padding: 2rem; min-height: 100vh; }
  .container { max-width: 1000px; margin: 0 auto; }
  .hero { text-align: center; margin-bottom: 3rem; }
  .hero h1 { color: var(--pertamina-blue); font-size: 2.2rem; margin: 0 0 .5rem; border-bottom: 4px solid var(--pertamina-red); padding-bottom: .5rem; display: inline-block; }
  .hero .subtitle { color: var(--neutral-gray); font-size: 1.05rem; margin-top: .5rem; }
  .hero .badges { margin-top: 1rem; }
  .hero .badges span { display: inline-block; background: var(--pertamina-blue); color: white; padding: .35rem .85rem; border-radius: .35rem; font-weight: 600; font-size: .85rem; margin: 0 .25rem; }

  .versions-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(360px, 1fr)); gap: 1.5rem; margin-bottom: 2rem; }
  .version-card {
    background: white;
    border-radius: 1rem;
    padding: 2rem;
    box-shadow: 0 6px 20px rgba(0,0,0,.08);
    text-align: center;
    transition: transform .2s, box-shadow .2s;
  }
  .version-card:hover { transform: translateY(-5px); box-shadow: 0 12px 30px rgba(0,0,0,.12); }
  .version-card.primary { border-top: 6px solid var(--pertamina-green); }
  .version-card.secondary { border-top: 6px solid var(--pertamina-yellow); }
  .version-card .badge {
    display: inline-block;
    padding: .25rem .65rem;
    border-radius: 999px;
    font-size: .7rem;
    font-weight: 700;
    text-transform: uppercase;
    margin-bottom: .65rem;
  }
  .version-card.primary .badge { background: var(--pertamina-green); color: white; }
  .version-card.secondary .badge { background: var(--pertamina-yellow); color: #5a4400; }
  .version-card .icon-big { font-size: 4rem; margin: .5rem 0; }
  .version-card h2 { color: var(--pertamina-blue); margin: .5rem 0; }
  .version-card .description { color: var(--neutral-gray); font-size: .95rem; margin: 1rem 0; min-height: 3em; }
  .version-card .features { text-align: left; font-size: .85rem; margin: 1rem 0; }
  .version-card .features li { margin: .35rem 0; }
  .version-card a.open-btn {
    display: inline-block;
    background: var(--hub-grad);
    color: white;
    text-decoration: none;
    padding: .75rem 1.5rem;
    border-radius: .5rem;
    font-weight: 700;
    margin-top: 1rem;
    transition: opacity .15s;
  }
  .version-card a.open-btn:hover { opacity: .85; }

  .info-section { background: white; border-radius: 1rem; padding: 1.5rem 2rem; box-shadow: 0 4px 15px rgba(0,0,0,.06); margin-bottom: 1.5rem; }
  .info-section h3 { color: var(--pertamina-blue); margin: 0 0 .5rem; }
  .info-section p, .info-section li { color: var(--neutral-gray); font-size: .9rem; }

  footer { text-align: center; padding: 1.5rem; color: var(--neutral-gray); font-size: .85rem; }
  footer code { background: white; padding: .15rem .4rem; border-radius: 3px; }
</style>
</head>
<body>
<div class="container">

  <div class="hero">
    <h1>PCP SMART 2026 — §3.4 Solusi Terpilih</h1>
    <div class="subtitle">HC Portal (PortalHC_KPB) — Transformasi Workflow Pengelolaan Kompetensi</div>
    <div class="badges">
      <span>v3.0</span>
      <span>2 Versi Diagram Landscape</span>
      <span>Tag pcp-hcportal-3.4-v3.0</span>
    </div>
  </div>

  <div class="versions-grid">
    <!-- Versi P -->
    <div class="version-card primary">
      <div class="badge">⭐ Primary</div>
      <div class="icon-big">🏛️</div>
      <h2>Versi P — Workflow Topology</h2>
      <div class="description">Diagram landscape 5 layer Purdue-style. HC Portal sebagai DMZ-analog Buffer Zone di tengah. Mirip slide PCP referensi.</div>
      <ul class="features">
        <li>✅ Match format slide PCP referensi 100%</li>
        <li>✅ 5 layer aktor (Manajemen → Pekerja)</li>
        <li>✅ DMZ Buffer Zone (HC Portal hub)</li>
        <li>✅ Marker A-F (issue) + 1-7 (improvement)</li>
        <li>✅ Tooltip popup hover detail</li>
        <li>✅ Tabel komparasi inline</li>
      </ul>
      <a href="versi-p-workflow-topology.html" class="open-btn">Buka Versi P →</a>
    </div>

    <!-- Versi C -->
    <div class="version-card secondary">
      <div class="badge">Secondary</div>
      <div class="icon-big">📊</div>
      <h2>Versi C — Comparison Dashboard</h2>
      <div class="description">Card-grid 7 fitur dengan metric Sebelum/Sesudah/Δ%. Executive view dengan agregat impact + HC Portal hub showcase.</div>
      <ul class="features">
        <li>✅ 7 feature card dengan icon + metric</li>
        <li>✅ Sebelum/Sesudah/Δ% per fitur</li>
        <li>✅ Agregat box median impact</li>
        <li>✅ HC Portal hub + tech stack</li>
        <li>✅ Tabel Issue A-F + coverage matrix</li>
        <li>✅ Executive-friendly data-dense</li>
      </ul>
      <a href="versi-c-comparison-dashboard.html" class="open-btn">Buka Versi C →</a>
    </div>
  </div>

  <div class="info-section">
    <h3>📌 Rekomendasi Pemilihan</h3>
    <ul>
      <li><b>Submission PCP utama:</b> Versi P (match slide referensi, reviewer engineering familiar)</li>
      <li><b>Executive presentation:</b> Versi C (data-dense, mudah dipahami manajemen)</li>
      <li><b>Internal showcase:</b> keduanya — Versi P jelaskan flow, Versi C jelaskan impact</li>
    </ul>
  </div>

  <div class="info-section">
    <h3>🔧 Cara Pakai</h3>
    <ol>
      <li>Klik tombol "Buka Versi P" atau "Buka Versi C" di atas</li>
      <li>Review konten di browser</li>
      <li>Pilih versi yang akan dipakai untuk submission PCP</li>
      <li>Manual redraw ke PowerPoint untuk slide final</li>
      <li>Print / Save PDF tersedia di tiap versi (tombol 🖨️)</li>
    </ol>
  </div>

  <div class="info-section">
    <h3>📁 Recovery Versi Lama</h3>
    <ul>
      <li><code>git checkout pcp-hcportal-3.4-v1.0 -- &lt;path&gt;</code> — recover v1.0 (12 swimlane MD)</li>
      <li><code>git checkout pcp-hcportal-3.4-v2.0 -- &lt;path&gt;</code> — recover v2.0 (15 file hybrid)</li>
    </ul>
  </div>

  <footer>
    <p>PCP SMART 2026 §3.4 v3.0 — HC Portal • Generated 2026-05-21 • Tag <code>pcp-hcportal-3.4-v3.0</code></p>
  </footer>
</div>
</body>
</html>
```

- [ ] **Step 2: Verify**

Open `index.html` di browser. Konfirmasi:
- Hero header dengan title + badges render
- 2 version card (P primary green, C secondary yellow) render side-by-side
- Hover card lift up
- Klik "Buka Versi P" → buka `versi-p-workflow-topology.html`
- Klik "Buka Versi C" → buka `versi-c-comparison-dashboard.html`
- Info section (rekomendasi + cara pakai + recovery) render

- [ ] **Step 3: Commit**

```bash
git add docs/pcp-HCPortal-2026/3.4-solusi-terpilih/index.html
git commit -m "docs(pcp-3.4-v3): wave4/index — master viewer hero + 2 version card + recovery info"
```

---

## Wave 5 — Verifikasi Akhir + Tag

### Task 5: Final Verification

**Files:** Verify only.

- [ ] **Step 1: List file struktur**

Run: `find docs/pcp-HCPortal-2026/3.4-solusi-terpilih -type f | sort`

Expected (4 file):
```
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/README.md
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/index.html
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-c-comparison-dashboard.html
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html
```

- [ ] **Step 2: Check placeholder TBD/TODO**

Run via Grep: pattern `TBD|TODO|FIXME|XXX` path `docs/pcp-HCPortal-2026/3.4-solusi-terpilih`
Expected: No files found

- [ ] **Step 3: Open 3 HTML di browser**

Manual: buka `index.html`, klik link Versi P + Versi C. Konfirmasi:
- index.html: hero + 2 card render
- versi-p: 5 layer Sebelum + Sesudah + DMZ buffer + tabel komparasi
- versi-c: 7 feature card + agregat + hub showcase + issue table + coverage matrix

- [ ] **Step 4: Print preview cek**

Tiap HTML: Ctrl+P (Cmd+P), preview. Konfirmasi no toolbar, layout clean.

- [ ] **Step 5: Tag v3.0**

```bash
git tag pcp-hcportal-3.4-v3.0
git log --oneline pcp-hcportal-3.4-v3.0 | head -10
```

- [ ] **Step 6: Update memory + notifikasi user**

Update memory entry untuk PCP §3.4 status SHIPPED v3.0.

Inform user:
1. 4 file di `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/`
2. Versi P (Workflow Topology) + Versi C (Comparison Dashboard)
3. Master `index.html` dengan link ke kedua versi
4. Tag `pcp-hcportal-3.4-v3.0`
5. v1.0 & v2.0 tags preserved untuk recovery

---

## Acceptance Criteria (Plan-Level)

| Criteria | Pass |
|----------|:----:|
| README.md ada dengan 2 versi rationale + recovery info | ☐ |
| versi-p-workflow-topology.html render: 5 layer + DMZ buffer + marker A-F & 1-7 + tabel komparasi inline + header logo + tooltip hover | ☐ |
| versi-c-comparison-dashboard.html render: 7 card grid + metric Sebelum/Sesudah/Δ + agregat + HC Portal hub + Issue A-F + coverage matrix | ☐ |
| index.html render: hero + 2 version card + link ke kedua versi + recovery info | ☐ |
| Konsisten color palette (5 warna max) | ☐ |
| Bahasa Indonesia full | ☐ |
| Tidak ada TBD/TODO | ☐ |
| Print-friendly @media print di semua HTML | ☐ |
| Tag `pcp-hcportal-3.4-v3.0` dibuat | ☐ |

---

## Self-Review

**Spec coverage:**
- §2 Tujuan v3 → Task 1-4 cover semua ✓
- §4 Versi P → Task 2 ✓
- §5 Versi C → Task 3 ✓
- §6 Konvensi visual → konsisten lintas Task 1-4 ✓
- §7 Struktur output 4 file → Task 1-4 ✓
- §8 Eksekusi wave 1-5 → Wave 1-5 ✓
- §9 Acceptance Criteria → Task 5 ✓
- §11 Out of scope → tidak ada task untuk swimlane/C4/dll ✓

**Placeholder scan:** No TBD/TODO.

**Type consistency:** Color CSS variables konsisten (--pertamina-* + --neutral-gray). Class names konsisten antar HTML (header-bar, meta-bar, toolbar, diagram-wrap). Aktor & modul icons konsisten.

**Cross-file consistency:**
- Issue codes A-F konsisten di Versi P (legend) + Versi C (tags + coverage matrix)
- Improvement numbers 1-7 konsisten di Versi P (legend)
- Metric values (Step Δ, Tools Δ, Waktu Δ) konsisten antara Versi P (tabel komparasi inline) + Versi C (card + agregat)

Plan ready for execution.
