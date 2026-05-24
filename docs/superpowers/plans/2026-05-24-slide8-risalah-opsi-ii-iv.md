# Slide 8 Risalah Web — Opsi II + IV Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build 2 HTML mockup full-polish v3.7 (`pipeline-outcome.html` + `workflow-topology-refined.html`) + PNG export untuk slide 8 Risalah Web.pptx, comply 7/7 PCP template slot + bonus "Coret" section.

**Architecture:**
- Folder baru `docs/pcp-HCPortal-2026/slide8-risalah/` (terpisah dari folder PCP §3.4 v3.7)
- Opsi II = new file built from scratch, copy CSS dari Versi P existing, body diganti pipeline 3-zone
- Opsi IV = duplicate Versi P, insert 2 callout box + "Coret" section + theory footer + Internal std
- Visual QA via Playwright snapshot per file sebelum commit

**Tech Stack:** HTML5 + CSS3 inline (no JS runtime, no build step) · Playwright MCP untuk visual QA + PNG export · Git tag versioning

**Spec reference:** `docs/superpowers/specs/2026-05-24-slide8-risalah-opsi-ii-iv-design.md`

---

## File Structure

```
docs/pcp-HCPortal-2026/slide8-risalah/          (folder baru)
├─ README.md                                     (index + reference mapping + recovery)
├─ index.html                                    (master viewer 2 card)
├─ pipeline-outcome.html                         (Opsi II — Ogoun framework primary)
├─ pipeline-outcome.png                          (PNG export 1600×~2400)
├─ workflow-topology-refined.html                (Opsi IV — Versi P + 2 callout)
└─ workflow-topology-refined.png                 (PNG export)

docs/superpowers/specs/                          (existing — read-only reference)
└─ 2026-05-24-slide8-risalah-opsi-ii-iv-design.md

docs/pcp-HCPortal-2026/3.4-solusi-terpilih/      (existing — DO NOT MODIFY)
└─ versi-p-workflow-topology.html                (source template untuk Opsi IV)
```

**Each file responsibility:**
- `README.md` — onboard reviewer: pilih versi mana, baca rationale, recovery git tag
- `index.html` — master viewer landing page dengan 2 card link ke versi
- `pipeline-outcome.html` — Opsi II standalone, full polish v3.7, Ogoun pipeline hero
- `workflow-topology-refined.html` — Opsi IV based on Versi P + insertions
- `*.png` — PNG export untuk paste ke PowerPoint manual

---

## Task 1: Setup folder + README master

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/README.md`

**Goal:** Folder ada, README explain rationale + reference mapping + recovery.

- [ ] **Step 1: Create folder**

```bash
mkdir -p "docs/pcp-HCPortal-2026/slide8-risalah"
```

Verify: `ls "docs/pcp-HCPortal-2026/slide8-risalah/"` returns empty (folder exists, contents 0).

- [ ] **Step 2: Write README.md**

Path: `docs/pcp-HCPortal-2026/slide8-risalah/README.md`

```markdown
# PCP SMART 2026 — Slide 8 Risalah Web (v1.0)

> **Audience:** Reviewer PCP, HC management, OPEX, Process Improvement
> **Source PPT:** `docs/pcp-HCPortal-2026/Risalah Web.pptx` slide 8
> **Konteks:** §3.4 Solusi Terpilih — HC Portal (alternative dari `3.4-solusi-terpilih/versi-p-workflow-topology.html`)

## Executive Summary

Slide 8 Risalah Web.pptx saat ini menggunakan Versi P (Workflow Topology Purdue-style + DMZ-analog Buffer Zone). Audit terhadap 4 jurnal akademis + 3 laporan korelasi di `referensi slide 8 solusi terpilih/` menemukan gap: tidak ada Flow Proses eksplisit, tidak ada Formula kuantitatif, zero theory grounding. Folder ini menyediakan 2 versi alternatif yang menambal gap tersebut.

## Cakupan v1.0

### Opsi II — `pipeline-outcome.html` (PRIMARY ALTERNATIVE)

Pemantauan Kompetensi Pipeline (Ogoun & Tamunosiki-Amadi 2023 framework). Hero zone = pipeline 3-stage (Information Gathering & Evaluation → Activity Auditing → Feedback Loop) + Outcome matrix R-coefficient → Panca Mutu.

**Cocok untuk:** Reviewer PCP yang prioritas evidence-based + formula kuantitatif.

### Opsi IV — `workflow-topology-refined.html` (REFINED VERSI P)

Versi P existing + 2 callout box (Flow Proses Ogoun + Formula R → Panca Mutu) + Standar Internal Pertamina + "Coret yang tidak digunakan" section + theory footer.

**Cocok untuk:** Low-risk path — preserve Buffer Zone metafora yang sudah dikenal, tambah theory grounding minimum.

## PCP Template 7-Slot Compliance

| Slot | II | IV |
|------|:--:|:--:|
| 1. Design / Gambar Teknik | ✅ | ✅ |
| 2. Sebelum vs Sesudah | ✅ | ✅ |
| 3. Aspect Table | ✅ | ✅ |
| 4. Flow Proses | ✅ PRIMARY | ✅ callout |
| 5. Formula | ✅ PRIMARY | ✅ callout |
| 6. Issue A-F + Improvement 1-7 | ✅ | ✅ |
| 7. Standar External + Internal | ✅ | ✅ |
| Bonus: Coret tidak digunakan | ✅ | ✅ |

## Reference Mapping (15 item)

| Kode | Sumber | Pakai di |
|------|--------|----------|
| R1 | Ellström & Kock (2008) Competence Development | IV footer |
| R4 | **Ogoun & Tamunosiki-Amadi (2023) Competence Monitoring** | II PRIMARY, IV callout |
| RL1 | Korelasi Jurnal & PPT KPB (docx) | Both footer |
| RL2 | Laporan Korelasi Kuantitatif V2 (docx) | Both formula |
| P1 | Risalah Inovasi PROTON (Fishbone+FMEA) | Both Issue codes |
| P2 | Risalah Panca Mutu | Both formula |
| SE1 | ISO/IEC 27001:2022 | Both |
| SE2 | OWASP Top 10 2021 + ASVS 4.0.3 | Both |
| SE3 | WCAG 2.2 (W3C, 2023) | Both |
| SI1 | Pedoman Kompetensi Teknis A5.2-01/K20000/2025/S9 | Both |
| SI2 | TKO B5.3-04/K20100/2025-S9 | Both |
| SI3 | Kamus Direktori Kompetensi Teknis Pertamina | Both |

R2 Staškeviča (2019) + R3 Ruggiero et al (2026) + RL3 Laporan Strategis Baku = optional, deferred (not used in v1.0).

## Cara Pakai

1. Buka `index.html` di browser untuk master viewer 2 card
2. Klik Opsi II atau Opsi IV untuk review konten
3. Print 🖨️ → Save as PDF (A3 landscape) untuk reviewer
4. PNG export (`*.png`) sudah disediakan untuk paste manual ke PowerPoint slide 8

## Recovery

- Tag `slide8-risalah-v1.0` — `git checkout slide8-risalah-v1.0 -- docs/pcp-HCPortal-2026/slide8-risalah/`
- Spec design: `docs/superpowers/specs/2026-05-24-slide8-risalah-opsi-ii-iv-design.md`
- Plan: `docs/superpowers/plans/2026-05-24-slide8-risalah-opsi-ii-iv.md`

## Konvensi Visual

Identik dengan Versi P existing v3.7:
- Color palette 5 token: `#C8102E` red · `#00558C` blue · `#00A551` green · `#FFC72C` yellow · `#6b7280` gray
- Typography token: `--fs-xs: 0.75rem` floor → `--fs-xl: 2rem`
- Print: `@page { size: A3 landscape; margin: 1cm }`
```

- [ ] **Step 3: Verify file exists + readable**

```bash
ls -la "docs/pcp-HCPortal-2026/slide8-risalah/README.md"
```

Expected: file exists, size > 2KB.

- [ ] **Step 4: Commit**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/README.md"
git commit -m "docs(slide8-risalah): add README master index + reference mapping

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Build pipeline-outcome.html (Opsi II)

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.html`
- Reference: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html` (copy CSS, modify body)

**Goal:** Standalone HTML Opsi II — Sebelum 5-layer (vertical stack) + 3-zone main + Coret section + theory footer. Full polish v3.7.

- [ ] **Step 1: Copy Versi P sebagai template base**

```bash
cp "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html" "docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.html"
```

Verify: file exists, 410 lines (sama dengan source).

- [ ] **Step 2: Modify `<title>` + header subtitle**

Open `docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.html`.

Replace line `<title>§3.4 v3.7 — Versi P: Workflow Topology HC Portal</title>` dengan:

```html
<title>§3.4 v1.0 — Opsi II: Pemantauan Kompetensi Pipeline (Ogoun Framework)</title>
```

Replace di header-bar subtitle:
```html
<small>Versi P: Workflow Topology (Purdue-Style Adaptation)</small>
```
dengan:
```html
<small>Opsi II: Pemantauan Kompetensi Pipeline · Ogoun &amp; Tamunosiki-Amadi (2023)</small>
```

Replace di header-bar border-top color — find `border-top: 5px solid var(--pertamina-green);` (already green, unchanged).

- [ ] **Step 3: Modify meta-bar tujuan**

Find:
```html
<div>📌 <b>Tujuan:</b> Tunjukkan transformasi workflow HC dari multi-tools manual ke single portal terintegrasi</div>
```

Replace dengan:
```html
<div>📌 <b>Tujuan:</b> Pemantauan kompetensi 3-stage (Info Gathering → Auditing → Feedback) → Panca Mutu</div>
```

- [ ] **Step 4: Modify toolbar note**

Find:
```html
<div class="note">📐 <b>Versi P (Primary)</b> — 5 layer aktor + HC Portal sebagai DMZ-analog buffer zone</div>
```

Replace dengan:
```html
<div class="note">📐 <b>Opsi II</b> — Pipeline Pemantauan Kompetensi 3-stage (Ogoun 2023) + Outcome Matrix Spearman R → Panca Mutu</div>
```

- [ ] **Step 5: Add CSS untuk pipeline zone + outcome matrix (sebelum `</style>`)**

Find `</style>` di line ~145. Insert SEBELUM `</style>`:

```css
  /* === Opsi II: 3-zone main layout === */
  .main-3zone { display: grid; grid-template-columns: 200px 1fr 280px; gap: 1rem; max-width: 1400px; margin: 0 auto 2rem; }
  .zone { background: white; border-radius: .75rem; box-shadow: 0 4px 14px rgba(0,0,0,.06); padding: 1rem; }
  .zone-aktor { border-top: 4px solid var(--pertamina-blue); }
  .zone-pipeline { border-top: 4px solid var(--pertamina-yellow); padding: 1.25rem; }
  .zone-outcome { border-top: 4px solid var(--pertamina-green); }
  .zone-label { font-size: var(--fs-xs); font-weight: 700; text-transform: uppercase; letter-spacing: .05em; color: var(--neutral-gray); margin-bottom: .75rem; }
  .aktor-stack { display: flex; flex-direction: column; gap: .5rem; }
  .aktor-item { background: var(--pertamina-blue-light); border: 1px solid var(--pertamina-blue); border-radius: .35rem; padding: .55rem; font-size: var(--fs-sm); display: flex; align-items: center; gap: .4rem; }
  .aktor-item .icon { font-size: 1.1rem; }
  .pipeline-stages { display: grid; grid-template-columns: 1fr 32px 1fr 32px 1fr; gap: .5rem; align-items: stretch; }
  .stage-box { background: var(--hub-grad); color: white; padding: .85rem; border-radius: .5rem; text-align: center; font-size: var(--fs-sm); display: flex; flex-direction: column; gap: .35rem; }
  .stage-box-1 { background: linear-gradient(135deg, #00558C, #0077B5); }
  .stage-box-2 { background: linear-gradient(135deg, #0077B5, #00A551); }
  .stage-box-3 { background: linear-gradient(135deg, #00A551, #5cba7d); }
  .stage-box b { display: block; font-size: var(--fs-sm); margin-bottom: .15rem; }
  .stage-box .stage-sub { font-size: var(--fs-xs); opacity: .9; font-weight: 400; font-style: italic; }
  .stage-box .stage-features { font-size: var(--fs-xs); margin-top: .25rem; line-height: 1.4; }
  .pipeline-arrow { display: flex; align-items: center; justify-content: center; color: var(--pertamina-yellow); font-size: 1.4rem; font-weight: 700; }
  .pipeline-caption { text-align: center; font-size: var(--fs-xs); color: var(--neutral-gray); font-style: italic; margin-top: .75rem; }
  .outcome-table { width: 100%; font-size: var(--fs-xs); border-collapse: collapse; }
  .outcome-table th { background: var(--pertamina-green); color: white; padding: .4rem; text-align: left; }
  .outcome-table td { padding: .4rem; border-bottom: 1px solid var(--border); }
  .outcome-table .signifikan { background: #d4f0dd; font-weight: 600; }
  .outcome-table .qualitative { background: #fff3cd; font-style: italic; color: #856404; }
  .outcome-caption { font-size: var(--fs-xs); color: var(--neutral-gray); margin-top: .5rem; font-style: italic; }

  /* === Tech + Standards row === */
  .tech-std-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; max-width: 1400px; margin: 0 auto 1.5rem; }
  .tech-box, .std-box { background: white; border-radius: .5rem; padding: 1rem; box-shadow: 0 2px 8px rgba(0,0,0,.05); }
  .tech-box { border-top: 3px solid var(--pertamina-blue); }
  .std-box { border-top: 3px solid var(--pertamina-yellow); }
  .tech-chips { display: flex; flex-wrap: wrap; gap: .35rem; margin-top: .5rem; }
  .tech-chip { background: var(--pertamina-blue-light); color: var(--pertamina-blue-dark); padding: .25rem .5rem; border-radius: .25rem; font-size: var(--fs-xs); font-weight: 500; }
  .env-flow { display: flex; gap: .5rem; align-items: center; margin-top: .5rem; font-size: var(--fs-xs); }
  .env-flow .env { padding: .25rem .5rem; border-radius: .25rem; font-weight: 600; }
  .env-flow .env-local { background: #d4f0dd; color: #005a2c; }
  .env-flow .env-dev { background: var(--pertamina-blue-light); color: var(--pertamina-blue-dark); }
  .env-flow .env-prod { background: #ffe8d4; color: #a04000; }
  .std-list { font-size: var(--fs-xs); line-height: 1.6; margin-top: .5rem; }
  .std-list b { color: var(--pertamina-blue); }

  /* === Coret section === */
  .coret-section { background: white; border-radius: .5rem; padding: 1rem; max-width: 1400px; margin: 0 auto 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.05); border-left: 4px solid var(--neutral-gray); }
  .coret-used { color: var(--pertamina-green); font-weight: 600; }
  .coret-strike { text-decoration: line-through; color: var(--neutral-gray); }

  /* === Theory footer === */
  .theory-footer { background: var(--pertamina-blue); color: white; padding: .85rem 1.25rem; border-radius: .5rem; text-align: center; font-size: var(--fs-sm); max-width: 1400px; margin: 0 auto 1rem; }
  .theory-footer b { color: var(--pertamina-yellow); }
```

- [ ] **Step 6: Replace seluruh body content (kecuali header-bar + meta-bar + toolbar + footer)**

Find di body, dari `<div class="diagram-wrap before-style">` (line ~168) sampai `</div>` penutup `.komparasi-section` (line ~402).

**Replace seluruh range tersebut** dengan markup ini:

```html
<!-- ===== SEBELUM (FULL 5-LAYER, sama struktur dgn Versi P) ===== -->
<div class="diagram-wrap before-style">
  <div class="diagram-header">
    <h2>❌ Sebelum (Kondisi Aktual)</h2>
    <div class="tagline">Workflow manual — FMEA RPN: Method=140 · Machine=140 · Man=90 (Risalah PROTON)</div>
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
      <span class="marker issue" title="D: Reporting Ad-Hoc">D</span>
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
      <span class="marker issue" title="A: Tools Terfragmentasi">A</span>
      <span class="marker issue" title="B: No SSoT">B</span>
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
      <span class="marker issue" title="C: No Audit Trail">C</span>
      <span class="marker issue" title="E: No Tracking">E</span>
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
      <div class="comp paper">📁 Arsip fisik</div>
      <div class="comp tool-ext">💬 WhatsApp (foto)</div>
      <div class="comp tool-ext">📧 Email (lampiran)</div>
      <span class="marker issue" title="A: Tools Terfragmentasi">A</span>
      <span class="marker issue" title="E: No Tracking">E</span>
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
      <span class="marker issue" title="A: Tools Terfragmentasi">A</span>
      <span class="marker issue" title="F: Renewal Reaktif">F</span>
    </div>
  </div>

  <table class="legend-table">
    <thead><tr><th>Code</th><th>Issue Sebelum (dari FMEA Risalah PROTON)</th><th>RPN / Lokasi</th></tr></thead>
    <tbody>
      <tr><td><span class="legend-marker issue">A</span></td><td><b>Tools Terfragmentasi</b> — workflow tersebar di 4-5 aplikasi</td><td>Method RPN=140</td></tr>
      <tr><td><span class="legend-marker issue">B</span></td><td><b>No Single Source of Truth</b> — data dicopy ke beberapa Excel</td><td>Machine RPN=140</td></tr>
      <tr><td><span class="legend-marker issue">C</span></td><td><b>No Audit Trail</b> — approval lisan WA tanpa record</td><td>Method RPN=140</td></tr>
      <tr><td><span class="legend-marker issue">D</span></td><td><b>Reporting Ad-Hoc</b> — laporan manual pivot per request</td><td>Method RPN=140</td></tr>
      <tr><td><span class="legend-marker issue">E</span></td><td><b>Workflow Tanpa Tracking</b> — no status history</td><td>Method RPN=140</td></tr>
      <tr><td><span class="legend-marker issue">F</span></td><td><b>Renewal Sertifikat Reaktif</b> — expired sering kelewat</td><td>Method RPN=140</td></tr>
    </tbody>
  </table>
</div>

<div class="transformation-arrow">
  ▼
  <small>Transformasi via HC Portal — Pipeline Pemantauan Kompetensi 3-Stage</small>
</div>

<!-- ===== 3-ZONE MAIN LAYOUT ===== -->
<div class="main-3zone">

  <!-- ZONE KIRI: 5 AKTOR -->
  <div class="zone zone-aktor">
    <div class="zone-label">👥 5 Aktor Hierarchy</div>
    <div class="aktor-stack">
      <div class="aktor-item"><span class="icon">👔</span> <b>Lv5</b> · Manajemen</div>
      <div class="aktor-item"><span class="icon">👤</span> <b>Lv4</b> · HC</div>
      <div class="aktor-item"><span class="icon">🏢</span> <b>Lv3</b> · Atasan</div>
      <div class="aktor-item"><span class="icon">🧑‍🏫</span> <b>Lv2</b> · Coach</div>
      <div class="aktor-item"><span class="icon">👷</span> <b>Lv1</b> · Pekerja</div>
    </div>
  </div>

  <!-- ZONE TENGAH: PIPELINE 3-STAGE (HERO) -->
  <div class="zone zone-pipeline">
    <div class="zone-label" style="text-align:center">🔄 Pemantauan Kompetensi Pipeline (Ogoun &amp; Tamunosiki-Amadi 2023 · Zeb-Obipi 2017)</div>
    <div class="pipeline-stages">
      <div class="stage-box stage-box-1">
        <b>① Information Gathering &amp; Evaluation</b>
        <div class="stage-sub">Self-Assessment · Directed · Shop-Floor</div>
        <div class="stage-features">📝 Assessment Online<br/>📊 KKJ Matrix<br/>🎯 PROTON IDP</div>
      </div>
      <div class="pipeline-arrow">→</div>
      <div class="stage-box stage-box-2">
        <b>② Activity Auditing</b>
        <div class="stage-sub">Evidence Gathering</div>
        <div class="stage-features">📎 Upload Evidence<br/>✅ Approval Workflow<br/>🔍 Audit Log<br/>🔐 RBAC</div>
      </div>
      <div class="pipeline-arrow">→</div>
      <div class="stage-box stage-box-3">
        <b>③ Feedback Loop</b>
        <div class="stage-sub">Real-time · Transparent</div>
        <div class="stage-features">🔥 Heatmap Gap<br/>🏆 Cert Download<br/>🔔 Notifikasi In-App</div>
      </div>
    </div>
    <div class="pipeline-caption">↑ HC Portal (PROTON · Professional Refinery Operations Competency Development) = Single Source of Truth · semua stage di 1 platform</div>
  </div>

  <!-- ZONE KANAN: OUTCOME MATRIX -->
  <div class="zone zone-outcome">
    <div class="zone-label">📊 Outcome × Panca Mutu</div>
    <table class="outcome-table">
      <thead><tr><th>Dimensi</th><th>R</th><th>p</th><th>Mutu</th></tr></thead>
      <tbody>
        <tr class="signifikan"><td><b>Timeliness</b></td><td><b>0.777</b></td><td>0.000</td><td><b>D</b>elivery</td></tr>
        <tr class="signifikan"><td><b>Innovativeness</b></td><td><b>0.610</b></td><td>0.040</td><td><b>C</b>+<b>Q</b></td></tr>
        <tr><td><b>Task Alertness</b></td><td>0.190</td><td>0.089</td><td><b>Moral</b></td></tr>
        <tr class="qualitative"><td><b>HSSE</b></td><td>—</td><td>—</td><td>HSSE*</td></tr>
      </tbody>
    </table>
    <div class="outcome-caption">⚙️ Formula: Spearman R · p&lt;0.05 = signifikan<br/>* HSSE: qualitative inference (no R)</div>
  </div>
</div>

<!-- ===== ISSUE + IMPROVEMENT ROW ===== -->
<div class="komparasi-section">
  <h2>📋 Issue (A-F) & Improvement (1-7)</h2>
  <table class="legend-table">
    <thead><tr><th style="width:8%">Code</th><th style="width:42%">Issue / Improvement</th><th>Mapping</th></tr></thead>
    <tbody>
      <tr><td><span class="legend-marker improvement">1</span></td><td><b>Analytics Dashboard Real-Time</b> (Manajemen)</td><td>→ stage ③ Feedback</td></tr>
      <tr><td><span class="legend-marker improvement">2</span></td><td><b>Master Data Terpusat</b> (HC)</td><td>→ stage ① Info Gathering</td></tr>
      <tr><td><span class="legend-marker improvement">3</span></td><td><b>Audit Log Lengkap</b></td><td>→ stage ② Auditing</td></tr>
      <tr><td><span class="legend-marker improvement">4</span></td><td><b>HC Portal sebagai Hub SSoT</b></td><td>→ semua stage</td></tr>
      <tr><td><span class="legend-marker improvement">5</span></td><td><b>Workflow Approval Terstruktur</b> (Coach→Atasan→HC)</td><td>→ stage ② Auditing</td></tr>
      <tr><td><span class="legend-marker improvement">6</span></td><td><b>Coaching Digital + Evidence Link</b></td><td>→ stage ② Auditing</td></tr>
      <tr><td><span class="legend-marker improvement">7</span></td><td><b>Pekerja Self-Service</b></td><td>→ stage ① + ③</td></tr>
    </tbody>
  </table>
</div>

<!-- ===== TECH STACK + STANDARDS ROW ===== -->
<div class="tech-std-row">
  <div class="tech-box">
    <div class="zone-label">⚙️ Rencana Pembuatan Sistem</div>
    <div class="tech-chips">
      <span class="tech-chip">.NET 8</span>
      <span class="tech-chip">ASP.NET Core MVC</span>
      <span class="tech-chip">EF Core 8</span>
      <span class="tech-chip">SQL Server</span>
      <span class="tech-chip">SignalR</span>
      <span class="tech-chip">Bootstrap</span>
    </div>
    <div class="env-flow">
      <span class="env env-local">🟢 Lokal</span> →
      <span class="env env-dev">🔵 Dev (10.55.3.3)</span> →
      <span class="env env-prod">🟠 Prod (IIS Windows)</span>
    </div>
  </div>
  <div class="std-box">
    <div class="zone-label">📐 Standar Desain / Pengujian / Sertifikasi / Inspeksi</div>
    <div class="std-list">
      <b>External:</b> ISO/IEC 27001:2022 (ISMS) · OWASP Top 10 2021 + ASVS 4.0.3 · WCAG 2.2 (W3C 2023)<br/>
      <b>Internal:</b> Pedoman Kompetensi Teknis A5.2-01/K20000/2025/S9 · TKO B5.3-04/K20100/2025-S9 · Kamus Direktori Kompetensi Teknis Pertamina
    </div>
  </div>
</div>

<!-- ===== CORET SECTION ===== -->
<div class="coret-section">
  <div class="zone-label">✂️ Coret yang tidak digunakan pada langkah 3</div>
  <div style="font-size:var(--fs-sm); line-height:1.7; margin-top:.5rem">
    <span class="coret-used">✓ FMEA</span> · <span class="coret-used">✓ Technical Reference</span> ·
    <span class="coret-strike">Cost &amp; Benefit Analysist</span> ·
    <span class="coret-strike">Scatter Diagram</span> ·
    <span class="coret-strike">P&amp;ID</span> ·
    <span class="coret-strike">As Built Drawing</span> ·
    <span class="coret-strike">PFD</span> ·
    <span class="coret-strike">Gambar Teknik (mekanikal)</span> ·
    <span class="coret-strike">Mekanika Teknik</span> ·
    <span class="coret-strike">Others</span>
    <div style="margin-top:.5rem; font-size:var(--fs-xs); color:var(--neutral-gray); font-style:italic">USED: FMEA (diagnosis Fishbone+RPN), Technical Reference (TKI/SOP). Rest = plant engineering tools, tidak relevan dengan web portal HR.</div>
  </div>
</div>

<!-- ===== THEORY FOOTER ===== -->
<div class="theory-footer">
  <b>Theory grounding:</b> Ogoun &amp; Tamunosiki-Amadi (2023) Competence Monitoring → Employee Responsiveness · Zeb-Obipi (2017) 3-stage activity model · Pertamina Panca Mutu Framework (Q/C/D/HSSE/Moral)
</div>
```

- [ ] **Step 7: Modify footer text**

Find `<div class="footer">` block near end. Replace dengan:

```html
<div class="footer">
  <p>📌 <b>Opsi II — Pipeline Outcome</b> · Slide 8 Risalah Web v1.0 · HC Portal · Tag <code>slide8-risalah-v1.0</code> · Generated 2026-05-24</p>
  <p>💡 Hover marker A-F atau 1-7 untuk detail. Klik 🖨️ Print untuk save PDF.</p>
</div>
```

- [ ] **Step 8: Visual QA via Playwright**

```
mcp__plugin_playwright_playwright__browser_navigate(url="file:///c:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.html")
mcp__plugin_playwright_playwright__browser_snapshot()
```

Expected snapshot output:
- Header bar dengan border-top green
- Sebelum panel dengan 5 layer rows + table legend issue
- Transformation arrow ▼
- Main 3-zone: zone-aktor kiri (5 box), zone-pipeline tengah (3 stage box + 2 arrow), zone-outcome kanan (4-row table)
- Issue+Improvement section dengan 7 row improvement
- Tech-std row 2 col
- Coret section dengan strike-through
- Theory footer banner biru

Bila ada element missing/broken → fix di step sebelumnya, ulangi step 8.

- [ ] **Step 9: Commit**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.html"
git commit -m "feat(slide8-risalah): add Opsi II pipeline-outcome.html

Ogoun framework primary: pipeline 3-stage hero (Info Gathering &
Evaluation → Activity Auditing → Feedback Loop) + outcome matrix
R-coefficient → Panca Mutu. Full polish v3.7 style.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Build workflow-topology-refined.html (Opsi IV)

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.html`
- Source: `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html` (copy + insert)

**Goal:** Versi P existing + 2 callout box (Flow Proses + Formula) + Coret section + Standar Internal + theory footer. Preserve seluruh struktur asli.

- [ ] **Step 1: Copy Versi P sebagai base**

```bash
cp "docs/pcp-HCPortal-2026/3.4-solusi-terpilih/versi-p-workflow-topology.html" "docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.html"
```

- [ ] **Step 2: Modify `<title>` + header subtitle**

Replace `<title>§3.4 v3.7 — Versi P: Workflow Topology HC Portal</title>` dengan:
```html
<title>§3.4 v1.0 — Opsi IV: Workflow Topology Refined + Theory Grounding</title>
```

Replace subtitle:
```html
<small>Versi P: Workflow Topology (Purdue-Style Adaptation)</small>
```
dengan:
```html
<small>Opsi IV: Workflow Topology + Theory Grounding (Versi P-Refined)</small>
```

- [ ] **Step 3: Modify meta-bar tujuan**

Find:
```html
<div>📌 <b>Tujuan:</b> Tunjukkan transformasi workflow HC dari multi-tools manual ke single portal terintegrasi</div>
```
Replace:
```html
<div>📌 <b>Tujuan:</b> Workflow topology + Flow Proses Ogoun + Formula R → Panca Mutu (theory-grounded)</div>
```

- [ ] **Step 4: Modify toolbar note**

Find:
```html
<div class="note">📐 <b>Versi P (Primary)</b> — 5 layer aktor + HC Portal sebagai DMZ-analog buffer zone</div>
```
Replace:
```html
<div class="note">📐 <b>Opsi IV</b> — Versi P + Callout Flow Proses (Ogoun 2023) + Callout Formula R → Panca Mutu</div>
```

- [ ] **Step 5: Add callout + theory + coret CSS sebelum `</style>`**

Insert sebelum `</style>`:

```css
  /* === Callout box style (Opsi IV) === */
  .callout-box { background: #fffbeb; border: 2px dashed var(--pertamina-yellow); border-radius: .5rem; padding: 1rem 1.25rem; max-width: 1400px; margin: 1rem auto 1.5rem; }
  .callout-label { font-size: var(--fs-sm); font-weight: 700; text-transform: uppercase; letter-spacing: .03em; color: #856404; text-align: center; margin-bottom: .75rem; }
  .callout-flow { display: grid; grid-template-columns: 1fr 28px 1fr 28px 1fr; gap: .5rem; align-items: center; }
  .callout-flow .stage { padding: .85rem; border-radius: .5rem; color: white; text-align: center; font-size: var(--fs-sm); font-weight: 600; }
  .callout-flow .stage-1 { background: linear-gradient(135deg, #00558C, #0077B5); }
  .callout-flow .stage-2 { background: linear-gradient(135deg, #0077B5, #00A551); }
  .callout-flow .stage-3 { background: linear-gradient(135deg, #00A551, #5cba7d); }
  .callout-flow .stage small { display: block; font-size: var(--fs-xs); opacity: .9; font-weight: 400; font-style: italic; margin-top: .25rem; }
  .callout-flow .arr { text-align: center; color: #856404; font-size: 1.3rem; font-weight: 700; }
  .callout-caption { text-align: center; font-size: var(--fs-xs); color: #856404; font-style: italic; margin-top: .65rem; }

  .formula-table { width: 100%; border-collapse: collapse; font-size: var(--fs-sm); margin-top: .5rem; }
  .formula-table th { background: var(--pertamina-yellow); color: #5a4400; padding: .55rem .75rem; text-align: left; }
  .formula-table td { padding: .5rem .75rem; border-bottom: 1px solid var(--border); }
  .formula-table .qualitative { background: #fff3cd; font-style: italic; }

  /* === Tech stack + Standards row (NEW for Opsi IV) === */
  .tech-std-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; max-width: 1400px; margin: 0 auto 1.5rem; }
  .tech-box, .std-box { background: white; border-radius: .5rem; padding: 1rem; box-shadow: 0 2px 8px rgba(0,0,0,.05); }
  .tech-box { border-top: 3px solid var(--pertamina-blue); }
  .std-box { border-top: 3px solid var(--pertamina-yellow); }
  .ts-label { font-size: var(--fs-xs); font-weight: 700; text-transform: uppercase; letter-spacing: .05em; color: var(--neutral-gray); margin-bottom: .5rem; }
  .tech-chips { display: flex; flex-wrap: wrap; gap: .35rem; margin-top: .5rem; }
  .tech-chip { background: var(--pertamina-blue-light); color: var(--pertamina-blue-dark); padding: .25rem .5rem; border-radius: .25rem; font-size: var(--fs-xs); font-weight: 500; }
  .env-flow { display: flex; gap: .5rem; align-items: center; margin-top: .5rem; font-size: var(--fs-xs); flex-wrap: wrap; }
  .env-flow .env { padding: .25rem .5rem; border-radius: .25rem; font-weight: 600; }
  .env-flow .env-local { background: #d4f0dd; color: #005a2c; }
  .env-flow .env-dev { background: var(--pertamina-blue-light); color: var(--pertamina-blue-dark); }
  .env-flow .env-prod { background: #ffe8d4; color: #a04000; }
  .std-list { font-size: var(--fs-sm); line-height: 1.7; margin-top: .5rem; }
  .std-list b { color: var(--pertamina-blue); }

  /* === Coret section === */
  .coret-section { background: white; border-radius: .5rem; padding: 1rem 1.25rem; max-width: 1400px; margin: 0 auto 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,.05); border-left: 4px solid var(--neutral-gray); }
  .coret-used { color: var(--pertamina-green); font-weight: 600; }
  .coret-strike { text-decoration: line-through; color: var(--neutral-gray); }

  /* === Theory footer === */
  .theory-footer { background: var(--pertamina-blue); color: white; padding: .85rem 1.25rem; border-radius: .5rem; text-align: center; font-size: var(--fs-sm); max-width: 1400px; margin: 0 auto 1rem; }
  .theory-footer b { color: var(--pertamina-yellow); }
```

- [ ] **Step 6: Insert CALLOUT 1 (Flow Proses) sebelum transformation-arrow**

Find:
```html
<div class="transformation-arrow">
  ▼
  <small>Transformasi via HC Portal</small>
</div>
```

Insert SEBELUM block tersebut:

```html
<!-- ===== CALLOUT 1: Flow Proses Pemantauan Kompetensi ===== -->
<div class="callout-box">
  <div class="callout-label">🆕 Flow Proses Pemantauan Kompetensi (Ogoun &amp; Tamunosiki-Amadi 2023)</div>
  <div class="callout-flow">
    <div class="stage stage-1">① Information Gathering &amp; Evaluation<small>Self-Assessment · Directed · Shop-Floor</small></div>
    <div class="arr">→</div>
    <div class="stage stage-2">② Activity Auditing<small>Evidence Gathering · Audit Log · RBAC</small></div>
    <div class="arr">→</div>
    <div class="stage stage-3">③ Feedback Loop<small>Real-time · Transparent · Multi-Source</small></div>
  </div>
  <div class="callout-caption">Bridging "DMZ Buffer Zone" (IT/OT analog) ↔ HR Competence Monitoring framework · Zeb-Obipi (2017) 3-stage activity model</div>
</div>
```

- [ ] **Step 7: Insert CALLOUT 2 (Formula) setelah komparasi-section**

Find penutup komparasi-section:
```html
    </tbody>
  </table>
</div>
```
(yang menutup `.komparasi-section`)

Insert SETELAH `</div>` penutup komparasi-section:

```html
<!-- ===== CALLOUT 2: Formula Korelasi → Panca Mutu ===== -->
<div class="callout-box">
  <div class="callout-label">🆕 Formula Korelasi Empiris → Panca Mutu (Ogoun §4 + Risalah PROTON Panca Mutu)</div>
  <table class="formula-table">
    <thead><tr><th>Dimensi Responsivitas</th><th>R Spearman (p&lt;0.05)</th><th>Target Panca Mutu</th><th>Fitur HC Portal</th></tr></thead>
    <tbody>
      <tr><td><b>Timeliness</b></td><td><b>R=0.777</b> (p=0.000) Signifikan Sangat Kuat</td><td><b>D</b>elivery</td><td>Real-time DB + SignalR</td></tr>
      <tr><td><b>Innovativeness</b></td><td><b>R=0.610</b> (p=0.040) Signifikan Kuat</td><td><b>C</b>ost + <b>Q</b>uality</td><td>In-house development (~Rp 222.6jt savings)</td></tr>
      <tr><td><b>Task Alertness</b></td><td>R=0.190 (p=0.089) Tidak Signifikan</td><td><b>Moral</b></td><td>Passing grade pre-CSU readiness</td></tr>
      <tr class="qualitative"><td><b>HSSE</b></td><td>— (no R, qualitative inference)</td><td><b>HSSE</b></td><td>Audit-ready evidence + RBAC compliance</td></tr>
    </tbody>
  </table>
  <div class="callout-caption">Sumber: Ogoun &amp; Tamunosiki-Amadi (2023) Spearman Rank Order · Laporan Korelasi Kuantitatif V2 § Panca Mutu mapping</div>
</div>
```

- [ ] **Step 8: Insert Tech Stack + Standar (External + Internal) row setelah CALLOUT 2**

Insert SETELAH CALLOUT 2 div block. Versi P existing tidak punya tech-stack + standards row explicit — ini section baru wajib untuk comply PCP template slot 7:

```html
<!-- ===== TECH STACK + STANDARDS ROW (NEW) ===== -->
<div class="tech-std-row">
  <div class="tech-box">
    <div class="ts-label">⚙️ Rencana Pembuatan Sistem</div>
    <div class="tech-chips">
      <span class="tech-chip">.NET 8</span>
      <span class="tech-chip">ASP.NET Core MVC</span>
      <span class="tech-chip">EF Core 8</span>
      <span class="tech-chip">SQL Server</span>
      <span class="tech-chip">SignalR</span>
      <span class="tech-chip">Bootstrap</span>
    </div>
    <div class="env-flow">
      <span class="env env-local">🟢 Lokal</span> →
      <span class="env env-dev">🔵 Dev (10.55.3.3)</span> →
      <span class="env env-prod">🟠 Prod (IIS Windows)</span>
    </div>
  </div>
  <div class="std-box">
    <div class="ts-label">📐 Standar Desain / Pengujian / Sertifikasi / Inspeksi (QA/QC)</div>
    <div class="std-list">
      <b>External:</b> ISO/IEC 27001:2022 (ISMS) · OWASP Top 10 2021 + ASVS 4.0.3 · WCAG 2.2 (W3C 2023)<br/>
      <b>Internal:</b> Pedoman Kompetensi Teknis A5.2-01/K20000/2025/S9 · TKO B5.3-04/K20100/2025-S9 · Kamus Direktori Kompetensi Teknis Pertamina
    </div>
  </div>
</div>
```

- [ ] **Step 9: Insert Coret section setelah tech-std-row**

Insert SETELAH tech-std-row block:

```html
<!-- ===== CORET SECTION ===== -->
<div class="coret-section">
  <div class="callout-label" style="text-align:left; color:var(--neutral-gray)">✂️ Coret yang tidak digunakan pada langkah 3</div>
  <div style="font-size:var(--fs-sm); line-height:1.7; margin-top:.5rem">
    <span class="coret-used">✓ FMEA</span> · <span class="coret-used">✓ Technical Reference</span> ·
    <span class="coret-strike">Cost &amp; Benefit Analysist</span> ·
    <span class="coret-strike">Scatter Diagram</span> ·
    <span class="coret-strike">P&amp;ID</span> ·
    <span class="coret-strike">As Built Drawing</span> ·
    <span class="coret-strike">PFD</span> ·
    <span class="coret-strike">Gambar Teknik (mekanikal)</span> ·
    <span class="coret-strike">Mekanika Teknik</span> ·
    <span class="coret-strike">Others</span>
    <div style="margin-top:.5rem; font-size:var(--fs-xs); color:var(--neutral-gray); font-style:italic">USED: FMEA (Fishbone+RPN diagnosis Risalah PROTON), Technical Reference (TKI/SOP). Rest = plant engineering tools, tidak relevan dengan web portal HR.</div>
  </div>
</div>
```

- [ ] **Step 10: Insert Theory footer + modify standard footer**

Insert SETELAH coret-section, SEBELUM `.footer`:

```html
<!-- ===== THEORY FOOTER ===== -->
<div class="theory-footer">
  <b>Theory grounding:</b> Ellström &amp; Kock (2008) Integrated Strategy + Enabling Learning Environment · Ogoun &amp; Tamunosiki-Amadi (2023) Competence Monitoring → Employee Responsiveness · Pertamina Panca Mutu Framework
</div>
```

Modify `.footer` text. Find:
```html
<p>📌 <b>Versi P</b> (Workflow Topology) — PCP SMART 2026 §3.4 v3.7 — HC Portal • Tag <code>pcp-hcportal-3.4-v3.7</code> • Generated 2026-05-22</p>
```
Replace:
```html
<p>📌 <b>Opsi IV — Workflow Topology Refined</b> · Slide 8 Risalah Web v1.0 · HC Portal · Tag <code>slide8-risalah-v1.0</code> · Generated 2026-05-24</p>
```

- [ ] **Step 11: Visual QA via Playwright**

```
mcp__plugin_playwright_playwright__browser_navigate(url="file:///c:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.html")
mcp__plugin_playwright_playwright__browser_snapshot()
```

Expected snapshot:
- Header bar blue border-top
- Sebelum 5-layer panel UNCHANGED dari Versi P
- 🆕 Callout 1 yellow dashed: 3 stage Flow Proses
- Transformation arrow ▼
- Sesudah 5-layer + Buffer Zone HC Portal UNCHANGED
- Komparasi 10 aspek UNCHANGED
- 🆕 Callout 2 yellow dashed: Formula table 4 row
- 🆕 Tech-std row 2 col (tech chips kiri + Standar External+Internal kanan)
- 🆕 Coret section gray border-left
- 🆕 Theory footer blue banner
- Footer Versi P-Refined v1.0

- [ ] **Step 12: Commit**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.html"
git commit -m "feat(slide8-risalah): add Opsi IV workflow-topology-refined.html

Based on Versi P existing + 2 callout (Flow Proses Ogoun + Formula
R-coefficient → Panca Mutu) + Standar Internal Pertamina + Coret
section + theory footer. Low-risk path preserving Buffer Zone metafora.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Build index.html (master viewer)

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/index.html`

**Goal:** Landing page dengan 2 card link ke Opsi II + IV, audience matrix, link spec.

- [ ] **Step 1: Write index.html**

Path: `docs/pcp-HCPortal-2026/slide8-risalah/index.html`

```html
<!DOCTYPE html>
<html lang="id">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Slide 8 Risalah Web v1.0 — Index</title>
<style>
  :root {
    --pertamina-red: #C8102E;
    --pertamina-blue: #00558C;
    --pertamina-green: #00A551;
    --pertamina-yellow: #FFC72C;
    --neutral-gray: #6b7280;
    --bg: #f6f7fb;
  }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: #1f2937; padding: 2rem; }
  .container { max-width: 1200px; margin: 0 auto; }
  .hero { background: linear-gradient(135deg, var(--pertamina-blue), var(--pertamina-green)); color: white; padding: 2.5rem; border-radius: 1rem; text-align: center; margin-bottom: 2rem; }
  .hero h1 { margin: 0 0 .5rem; font-size: 2rem; }
  .hero p { margin: 0; opacity: .95; }
  .cards { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; margin-bottom: 2rem; }
  .card { background: white; border-radius: .75rem; box-shadow: 0 4px 16px rgba(0,0,0,.08); padding: 1.5rem; text-decoration: none; color: inherit; transition: transform .2s, box-shadow .2s; }
  .card:hover { transform: translateY(-4px); box-shadow: 0 8px 24px rgba(0,0,0,.12); }
  .card.opsi-ii { border-top: 6px solid var(--pertamina-green); }
  .card.opsi-iv { border-top: 6px solid var(--pertamina-blue); }
  .card h2 { margin: 0 0 .35rem; font-size: 1.35rem; }
  .card .tag { display: inline-block; padding: .2rem .55rem; border-radius: .35rem; font-size: .75rem; font-weight: 700; margin-bottom: .75rem; }
  .card.opsi-ii .tag { background: var(--pertamina-green); color: white; }
  .card.opsi-iv .tag { background: var(--pertamina-blue); color: white; }
  .card p { margin: 0 0 .75rem; font-size: .9rem; color: var(--neutral-gray); }
  .card ul { margin: .5rem 0 0; padding-left: 1.25rem; font-size: .85rem; }
  .card li { margin-bottom: .25rem; }
  .audience-matrix { background: white; padding: 1.5rem; border-radius: .75rem; box-shadow: 0 2px 12px rgba(0,0,0,.05); margin-bottom: 1.5rem; }
  .audience-matrix h3 { margin: 0 0 .75rem; color: var(--pertamina-blue); }
  table { width: 100%; border-collapse: collapse; font-size: .9rem; }
  th { background: var(--pertamina-blue); color: white; text-align: left; padding: .55rem .75rem; }
  td { padding: .5rem .75rem; border-bottom: 1px solid #e5e7eb; }
  .footer { text-align: center; padding: 1.5rem; color: var(--neutral-gray); font-size: .85rem; }
  .footer code { background: white; padding: .15rem .4rem; border-radius: 3px; }
</style>
</head>
<body>
<div class="container">

  <div class="hero">
    <h1>Slide 8 Risalah Web — Solusi Terpilih HC Portal</h1>
    <p>PCP SMART 2026 · §3.4 · v1.0 · 2 versi alternatif</p>
  </div>

  <div class="cards">
    <a href="pipeline-outcome.html" class="card opsi-ii">
      <div class="tag">PRIMARY ALTERNATIVE</div>
      <h2>Opsi II — Pipeline Outcome</h2>
      <p>Pemantauan Kompetensi 3-stage hero + Outcome matrix Spearman R → Panca Mutu</p>
      <ul>
        <li><b>Hero:</b> Pipeline 3-stage (Info Gathering → Auditing → Feedback)</li>
        <li><b>Framework:</b> Ogoun &amp; Tamunosiki-Amadi (2023)</li>
        <li><b>Formula:</b> R=0.777 (Time), 0.610 (Innov), 0.190 (Alert)</li>
        <li><b>Audience:</b> PCP reviewer evidence-based, OPEX</li>
      </ul>
    </a>

    <a href="workflow-topology-refined.html" class="card opsi-iv">
      <div class="tag">REFINED VERSI P (LOW RISK)</div>
      <h2>Opsi IV — Workflow Topology Refined</h2>
      <p>Versi P existing + 2 callout box theory + Standar Internal + Coret + footer</p>
      <ul>
        <li><b>Base:</b> Versi P (Purdue 5-layer + Buffer Zone HC Portal)</li>
        <li><b>+ Callout 1:</b> Flow Proses Ogoun 3-stage</li>
        <li><b>+ Callout 2:</b> Formula R → Panca Mutu</li>
        <li><b>Audience:</b> PCP reviewer preserve metafora familiar</li>
      </ul>
    </a>
  </div>

  <div class="audience-matrix">
    <h3>Matriks Audience → Versi</h3>
    <table>
      <thead><tr><th>Audience</th><th>Rekomendasi</th></tr></thead>
      <tbody>
        <tr><td>PCP reviewer evidence-based + formula kuantitatif</td><td><b>Opsi II</b></td></tr>
        <tr><td>PCP reviewer prefer metafora arsitektur familiar</td><td><b>Opsi IV</b></td></tr>
        <tr><td>HC management showcase</td><td>Opsi IV (kontinuitas dengan Versi P)</td></tr>
        <tr><td>OPEX / Process Improvement</td><td>Opsi II (Pipeline explicit)</td></tr>
      </tbody>
    </table>
  </div>

  <div class="footer">
    <p>📌 Slide 8 Risalah Web v1.0 · Tag <code>slide8-risalah-v1.0</code> · Generated 2026-05-24</p>
    <p>Spec: <code>docs/superpowers/specs/2026-05-24-slide8-risalah-opsi-ii-iv-design.md</code></p>
    <p>Plan: <code>docs/superpowers/plans/2026-05-24-slide8-risalah-opsi-ii-iv.md</code></p>
  </div>

</div>
</body>
</html>
```

- [ ] **Step 2: Visual QA via Playwright**

```
mcp__plugin_playwright_playwright__browser_navigate(url="file:///c:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/docs/pcp-HCPortal-2026/slide8-risalah/index.html")
mcp__plugin_playwright_playwright__browser_snapshot()
```

Expected: hero blue-green gradient, 2 card (green Opsi II + blue Opsi IV) dengan link, audience matrix table 4 row, footer.

- [ ] **Step 3: Verify card link works**

Klik Opsi II card → harus navigate ke `pipeline-outcome.html`.
Klik Opsi IV card → harus navigate ke `workflow-topology-refined.html`.

```
mcp__plugin_playwright_playwright__browser_click(element="Opsi II card", ref=<from snapshot>)
```

Expected: URL berubah ke pipeline-outcome.html, page render correctly. Navigate back, repeat untuk Opsi IV card.

- [ ] **Step 4: Commit**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/index.html"
git commit -m "feat(slide8-risalah): add index.html master viewer

Hero landing + 2 card link ke Opsi II & IV + audience matrix.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: PNG export both versi

**Files:**
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.png`
- Create: `docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.png`

**Goal:** PNG fullPage screenshot kedua HTML untuk paste ke PowerPoint manual.

- [ ] **Step 1: Resize viewport ke A3 landscape proportion**

```
mcp__plugin_playwright_playwright__browser_resize(width=1600, height=1100)
```

- [ ] **Step 2: Navigate + screenshot pipeline-outcome.html**

```
mcp__plugin_playwright_playwright__browser_navigate(url="file:///c:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.html")
mcp__plugin_playwright_playwright__browser_take_screenshot(fullPage=true, type="png", filename="docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.png")
```

Verify: file `pipeline-outcome.png` ada di folder, size 500KB - 2MB.

```bash
ls -lh "docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.png"
```

- [ ] **Step 3: Navigate + screenshot workflow-topology-refined.html**

```
mcp__plugin_playwright_playwright__browser_navigate(url="file:///c:/Users/Administrator/OneDrive%20-%20PT%20Pertamina%20(Persero)/Desktop/PortalHC_KPB/docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.html")
mcp__plugin_playwright_playwright__browser_take_screenshot(fullPage=true, type="png", filename="docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.png")
```

Verify: file `workflow-topology-refined.png` ada, size 500KB - 2MB.

```bash
ls -lh "docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.png"
```

- [ ] **Step 4: Commit PNGs**

```bash
git add "docs/pcp-HCPortal-2026/slide8-risalah/pipeline-outcome.png" "docs/pcp-HCPortal-2026/slide8-risalah/workflow-topology-refined.png"
git commit -m "feat(slide8-risalah): add PNG exports for PowerPoint paste

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Tag release v1.0

**Goal:** Tag git untuk recovery + reference.

- [ ] **Step 1: Verify all 6 files ada**

```bash
ls -la "docs/pcp-HCPortal-2026/slide8-risalah/"
```

Expected output:
- README.md
- index.html
- pipeline-outcome.html
- pipeline-outcome.png
- workflow-topology-refined.html
- workflow-topology-refined.png

- [ ] **Step 2: Verify acceptance criteria**

Manual checklist (mark each ✓):
- [ ] Both HTML render correctly di Chrome/Edge (Task 2 + 3 visual QA passed)
- [ ] Print preview A3 landscape clean (test: open HTML → Ctrl+P → A3 landscape preview, no clipping)
- [ ] PNG export 500KB-2MB each
- [ ] 7/7 PCP slots compliance verified (lihat spec §7)
- [ ] All 15 reference items cited (R1-SI3 sesuai README mapping)
- [ ] No typo, no broken layout @ breakpoint 1200px / 900px (Playwright resize test)
- [ ] All 8 approved default decisions applied (spec §8)
- [ ] README.md punya reference mapping table + recovery instructions

- [ ] **Step 3: Create git tag**

```bash
git tag slide8-risalah-v1.0
git tag --list slide8-risalah-v1.0
```

Expected: tag ada, no error.

- [ ] **Step 4: Verify recovery works**

```bash
git show slide8-risalah-v1.0 --stat | head -20
```

Expected: shows tagged commit + 6 files in slide8-risalah/ folder.

- [ ] **Step 5: Final commit + state report**

```bash
git log --oneline -10 "docs/pcp-HCPortal-2026/slide8-risalah/"
```

Expected: 4-5 commit message dengan prefix `docs(slide8-risalah)` atau `feat(slide8-risalah)`.

---

## Self-Review Checklist

After execution, verify:
- [ ] Folder `docs/pcp-HCPortal-2026/slide8-risalah/` punya 6 file (README + index + 2 HTML + 2 PNG)
- [ ] Tag `slide8-risalah-v1.0` exists
- [ ] All spec §11 acceptance criteria met
- [ ] No file di folder `3.4-solusi-terpilih/` ter-modified (Versi P existing tetap utuh)
- [ ] Both Opsi II + IV implement 8 approved default decisions (spec §8):
  1. HSSE qualitative inference row ✓
  2. Coret section ✓
  3. Sebelum Opsi II vertical full 5-layer ✓
  4. Coexist (tidak overwrite Versi P existing) ✓
  5. "Information Gathering & Evaluation" full term ✓
  6. Audit Log + RBAC split ✓
  7. PROTON resmi definition ✓
  8. Naming generic (`pipeline-outcome` + `workflow-topology-refined`) ✓
