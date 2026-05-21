# Sosialisasi PortalHC v3 — Merge HTML Style + PDF Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Re-author `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` jadi deck 23 slide yang memakai design HTML existing + data autoritatif dari PDF v2.

**Architecture:** Single-file static HTML monolitik. Edit in-place. CSS + JS + content inline. 23 slide menggunakan kombinasi class HTML existing (cover/stairs/cat-grid/akses-grid/penutup) + new tokens (chip/swimlane/faded-card/status-badge/progress-table-highlight). Navigation existing (keyboard + button + agenda goTo) dipertahankan, total counter `15 → 23`. Drop Chart.js + filterMock fungsi (tidak terpakai).

**Tech Stack:** HTML5, CSS3 (vanilla, gradient + grid + flex), Vanilla JS (no framework). No build step. Browser test = buka file langsung.

**Spec:** `docs/superpowers/specs/2026-05-25-sosialisasi-portalhc-v3-merge-design.md`

**Target file:** `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html`

**Source data:** `docs/Sosialisasi PortalHC v2 — Slide Deck 2026.pdf` (18 page)

---

## Pre-flight

File `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` saat ini **untracked** di git (lihat git status sebelum mulai). File `Sosialisasi-Aplikasi-PortalHC-KPB.html` di root sudah deleted (untracked deletion). Sebelum task 1 jalankan baseline commit supaya rollback aman.

```bash
git status
# verify: docs/Sosialisasi-Aplikasi-PortalHC-KPB.html (untracked)
#         Sosialisasi-Aplikasi-PortalHC-KPB.html (deleted from working dir, was tracked)

# Stage current state as baseline
git rm Sosialisasi-Aplikasi-PortalHC-KPB.html
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "chore(sosialisasi): move sosialisasi HTML into docs/ as baseline v2

Pre-merge checkpoint before v3 rewrite. File identical to root copy,
relocated to docs/ for consistency with PDF sibling."
```

---

## Task 1: Scaffolding — Drop unused assets + update total counter

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (head + script)

**Goal:** Hapus Chart.js CDN, fungsi `filterMock()`, dan update konstanta `total = 15` → `total = 23`. Belum hapus slide content lama — itu task berikutnya.

- [ ] **Step 1: Hapus Chart.js script tag dari `<head>`**

Cari `<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>` dan hapus.

- [ ] **Step 2: Update total slide constant**

Cari `const total = 15;` di blok `<script>` dan ganti jadi `const total = 23;`.

- [ ] **Step 3: Hapus fungsi `filterMock` dan `initCharts`**

Cari fungsi-fungsi terkait mockup CMP/CDP (filterMock) dan Chart.js (initCharts, chartCat, chartGain). Hapus seluruh blok fungsi. Hapus juga panggilan `if (n === 11) setTimeout(initCharts, 100);` di dalam `showSlide`.

- [ ] **Step 4: Verifikasi syntax**

Buka file di browser. Buka DevTools Console. Pastikan tidak ada error JavaScript saat load.

- [ ] **Step 5: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "chore(sosialisasi): drop Chart.js + filterMock, total=23

Pre-cleanup before slide content rewrite. Remove unused mockup row
filtering + chart rendering (slides 7/9/11 will be dropped/replaced).

Refs spec docs/superpowers/specs/2026-05-25-sosialisasi-portalhc-v3-merge-design.md"
```

---

## Task 2: CSS Additions — New style tokens

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (di dalam `<style>`, tambah sebelum closing `</style>`)

**Goal:** Tambah CSS class baru yang akan dipakai slide-slide baru. Belum mengubah markup HTML.

- [ ] **Step 1: Tambah CSS chip role pill (slide 6)**

```css
/* =================== CHIP ROLE PILL (slide 6) =================== */
.role-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  justify-content: center;
}
.role-chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  background: rgba(0, 46, 109, 0.08);
  border: 1px solid rgba(0, 46, 109, 0.2);
  border-radius: 12px;
  font-size: 8.5pt;
  font-weight: 600;
  color: var(--navy);
}
body.dark .role-chip {
  background: rgba(255, 255, 255, 0.08);
  border-color: rgba(255, 255, 255, 0.2);
  color: #e0f2fe;
}
.role-chip .chip-icon { font-size: 10pt; }
.step-level-label {
  position: absolute;
  left: -42px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 8pt;
  font-weight: 800;
  color: var(--red);
  background: rgba(237, 28, 36, 0.08);
  padding: 3px 7px;
  border-radius: 6px;
}
```

- [ ] **Step 2: Tambah CSS 2-row card section divider (slide 7 — 2 Jenis Assessment)**

```css
/* =================== 2-ROW CARD (slide 7) =================== */
.jenis-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
  margin-bottom: 16px;
}
.jenis-card {
  background: linear-gradient(135deg, #fff 0%, #f8fafc 100%);
  border: 1.5px solid var(--border);
  border-radius: 14px;
  padding: 20px 22px;
  box-shadow: 0 4px 12px rgba(0, 46, 109, 0.06);
}
.jenis-card.umum { border-top: 4px solid var(--navy); }
.jenis-card.proton { border-top: 4px solid var(--red); }
.jenis-card .jenis-head {
  font-size: 14pt;
  font-weight: 800;
  margin-bottom: 14px;
  display: flex;
  align-items: center;
  gap: 8px;
}
.jenis-card.umum .jenis-head { color: var(--navy); }
.jenis-card.proton .jenis-head { color: var(--red); }
.jenis-row {
  padding: 10px 0;
  border-top: 1px solid var(--border);
}
.jenis-row .jenis-label {
  font-size: 8.5pt;
  font-weight: 700;
  color: var(--slate);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 4px;
}
.jenis-row .jenis-value {
  font-size: 10pt;
  color: var(--text);
  line-height: 1.5;
}
.tip-bar {
  background: linear-gradient(90deg, rgba(0, 150, 64, 0.08), rgba(0, 150, 64, 0.02));
  border-left: 3px solid var(--green);
  padding: 10px 14px;
  font-size: 9.5pt;
  color: var(--text);
  border-radius: 6px;
}
body.dark .jenis-card { background: linear-gradient(135deg, var(--slide-bg) 0%, var(--slide-bg-2) 100%); }
```

- [ ] **Step 3: Tambah CSS 3-swimlane (slide 10, 18, 19)**

```css
/* =================== SWIMLANE (slide 10/18/19) =================== */
.swim-row {
  display: grid;
  grid-template-columns: auto auto auto;
  gap: 12px;
  align-items: stretch;
}
.swim-lane {
  background: linear-gradient(135deg, rgba(0, 46, 109, 0.04), rgba(0, 46, 109, 0.01));
  border: 1px dashed rgba(0, 46, 109, 0.25);
  border-radius: 12px;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.swim-lane.lane-prep { border-color: rgba(0, 150, 64, 0.4); background: linear-gradient(135deg, rgba(0, 150, 64, 0.05), rgba(0, 150, 64, 0.01)); }
.swim-lane.lane-exec { border-color: rgba(0, 46, 109, 0.4); }
.swim-lane.lane-result { border-color: rgba(237, 28, 36, 0.4); background: linear-gradient(135deg, rgba(237, 28, 36, 0.05), rgba(237, 28, 36, 0.01)); }
.swim-label {
  font-size: 8pt;
  font-weight: 800;
  color: var(--slate);
  text-transform: uppercase;
  letter-spacing: 0.8px;
  margin-bottom: 4px;
}
.swim-steps {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}
.swim-step {
  background: #fff;
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 10px 12px;
  flex: 1;
  min-width: 100px;
  text-align: center;
  font-size: 9pt;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.05);
}
.swim-step .step-num-mini {
  display: inline-block;
  width: 22px;
  height: 22px;
  line-height: 22px;
  border-radius: 50%;
  background: var(--navy);
  color: #fff;
  font-weight: 800;
  font-size: 9pt;
  margin-bottom: 6px;
}
.swim-step .step-icon-mini { font-size: 14pt; display: block; margin-bottom: 4px; }
.swim-step .step-title { font-weight: 700; color: var(--navy); margin-bottom: 2px; }
.swim-step .step-desc { font-size: 8pt; color: var(--text-muted); line-height: 1.3; }
.swim-step.highlight { border-color: var(--amber); background: linear-gradient(135deg, #fff8e8, #ffe9c0); }
.swim-step.highlight .step-num-mini { background: var(--amber); }
.output-bar {
  margin-top: 16px;
  background: linear-gradient(90deg, var(--navy), var(--navy-dark));
  color: #fff;
  padding: 12px 18px;
  border-radius: 10px;
  font-size: 10pt;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 8px;
}
body.dark .swim-step { background: var(--slide-bg); border-color: var(--border); }
body.dark .swim-step .step-title { color: #e0f2fe; }
```

- [ ] **Step 4: Tambah CSS faded card (slide 5 BP Coming Soon)**

```css
/* =================== FADED CARD (slide 5 BP) =================== */
.module-card.faded {
  border: 2px dashed var(--slate);
  opacity: 0.78;
  background: linear-gradient(135deg, rgba(100, 116, 139, 0.06), rgba(100, 116, 139, 0.02));
  position: relative;
}
.module-card.faded::before {
  content: '🚧 COMING SOON';
  position: absolute;
  top: 12px;
  right: 12px;
  font-size: 8pt;
  font-weight: 800;
  color: var(--amber);
  background: rgba(245, 158, 11, 0.12);
  padding: 4px 8px;
  border-radius: 6px;
  letter-spacing: 0.5px;
}
.module-card .module-bullets {
  list-style: none;
  margin-top: 12px;
  padding: 0;
}
.module-card .module-bullets li {
  font-size: 9.5pt;
  color: var(--text);
  padding: 4px 0;
  display: flex;
  gap: 6px;
}
.module-card.faded .module-bullets li { color: var(--text-muted); }
```

- [ ] **Step 5: Tambah CSS status badge (slide 22 Cara Akses)**

```css
/* =================== STATUS BADGE (slide 22) =================== */
.akses-card .akses-status {
  display: inline-block;
  font-size: 8.5pt;
  font-weight: 800;
  padding: 4px 10px;
  border-radius: 12px;
  margin-bottom: 8px;
  letter-spacing: 0.5px;
}
.akses-card.dev .akses-status { background: rgba(0, 150, 64, 0.15); color: var(--green); }
.akses-card.prod .akses-status { background: rgba(245, 158, 11, 0.15); color: var(--amber); }
.url-perkiraan {
  font-size: 8pt;
  color: var(--amber);
  margin-top: 4px;
  display: block;
  font-style: italic;
}
.akses-footer {
  margin-top: 16px;
  text-align: center;
  font-size: 10pt;
  font-weight: 600;
  color: var(--text-muted);
  padding: 10px;
  background: rgba(245, 158, 11, 0.08);
  border-radius: 8px;
}
```

- [ ] **Step 6: Tambah CSS progress table highlight (slide 17)**

```css
/* =================== PROGRESI TABLE (slide 17) =================== */
.progresi-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 9.5pt;
}
.progresi-table th, .progresi-table td {
  padding: 12px 14px;
  text-align: left;
  border-bottom: 1px solid var(--border);
  vertical-align: top;
}
.progresi-table thead th {
  background: var(--navy);
  color: #fff;
  font-weight: 700;
  font-size: 9.5pt;
  letter-spacing: 0.3px;
}
.progresi-table tbody td.aspek-label {
  font-weight: 700;
  color: var(--navy);
  background: rgba(0, 46, 109, 0.04);
}
.progresi-table tbody td.diff-cell {
  background: linear-gradient(135deg, rgba(237, 28, 36, 0.08), rgba(245, 158, 11, 0.08));
  font-weight: 700;
  color: var(--red);
}
body.dark .progresi-table tbody td.aspek-label { color: #e0f2fe; background: rgba(255,255,255,0.04); }
body.dark .progresi-table tbody td.diff-cell { color: #ff9aa0; }
```

- [ ] **Step 7: Tambah CSS tree paralel (slide 16)**

```css
/* =================== TREE HIERARKI (slide 16) =================== */
.tree-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 30px;
  margin-bottom: 16px;
}
.tree-col {
  background: linear-gradient(135deg, #fff, #f8fafc);
  border: 1.5px solid var(--border);
  border-radius: 14px;
  padding: 20px;
}
.tree-col.generic { border-top: 4px solid var(--navy); }
.tree-col.example { border-top: 4px solid var(--green); }
.tree-col h4 {
  font-size: 10.5pt;
  color: var(--slate);
  text-transform: uppercase;
  letter-spacing: 0.6px;
  margin-bottom: 14px;
  text-align: center;
}
.tree-node {
  background: var(--slide-bg-2);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 10px 14px;
  margin: 0 auto 8px;
  text-align: center;
  font-size: 10pt;
  font-weight: 600;
  color: var(--text);
  max-width: 80%;
}
.tree-node .node-level {
  display: block;
  font-size: 7.5pt;
  color: var(--slate);
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.6px;
  margin-top: 3px;
}
.tree-arrow {
  text-align: center;
  font-size: 16pt;
  color: var(--slate);
  margin: 0 0 8px;
}
body.dark .tree-col { background: linear-gradient(135deg, var(--slide-bg), var(--slide-bg-2)); }
```

- [ ] **Step 8: Tambah CSS Pre/Post stepper + 2 metric (slide 9) dan dual-track (slide 14) dan ringkasan output (slide 20)**

```css
/* =================== PRE-POST STEPPER (slide 9) =================== */
.pp-stepper {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
  margin-bottom: 20px;
  align-items: stretch;
}
.pp-step {
  background: #fff;
  border: 1.5px solid var(--border);
  border-radius: 12px;
  padding: 14px;
  text-align: center;
  position: relative;
}
.pp-step + .pp-step::before {
  content: '→';
  position: absolute;
  left: -12px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 14pt;
  color: var(--slate);
  font-weight: 800;
}
.pp-step .pp-num {
  display: inline-block;
  width: 28px; height: 28px;
  line-height: 28px;
  border-radius: 50%;
  background: var(--navy);
  color: #fff;
  font-weight: 800;
  font-size: 11pt;
  margin-bottom: 8px;
}
.pp-step .pp-icon { font-size: 22pt; display: block; margin-bottom: 6px; }
.pp-step .pp-title { font-weight: 700; color: var(--navy); font-size: 11pt; }
.pp-step .pp-desc { font-size: 8.5pt; color: var(--text-muted); margin-top: 4px; line-height: 1.4; }
.pp-metrics {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}
.pp-metric {
  background: linear-gradient(135deg, rgba(0, 46, 109, 0.05), rgba(0, 46, 109, 0.01));
  border-left: 4px solid var(--navy);
  border-radius: 10px;
  padding: 14px 16px;
}
.pp-metric.alt { border-left-color: var(--green); background: linear-gradient(135deg, rgba(0, 150, 64, 0.05), rgba(0, 150, 64, 0.01)); }
.pp-metric h5 { font-size: 11pt; color: var(--navy); margin-bottom: 4px; }
.pp-metric.alt h5 { color: var(--green); }
.pp-metric p { font-size: 9.5pt; color: var(--text); line-height: 1.4; }
body.dark .pp-step { background: var(--slide-bg); }

/* =================== DUAL TRACK (slide 14) =================== */
.dt-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
  margin-bottom: 16px;
}
.dt-col {
  background: linear-gradient(135deg, #fff, #f8fafc);
  border: 1.5px solid var(--border);
  border-top: 4px solid var(--navy);
  border-radius: 14px;
  padding: 22px 26px;
  text-align: center;
}
.dt-col.operator { border-top-color: var(--red); }
.dt-col .dt-head {
  font-size: 15pt;
  font-weight: 800;
  margin-bottom: 14px;
  color: var(--navy);
}
.dt-col.operator .dt-head { color: var(--red); }
.dt-pills {
  display: flex;
  justify-content: center;
  gap: 10px;
  margin-bottom: 10px;
}
.dt-pill {
  background: var(--navy);
  color: #fff;
  padding: 8px 16px;
  border-radius: 16px;
  font-weight: 700;
  font-size: 10pt;
}
.dt-col.operator .dt-pill { background: var(--red); }
.dt-note { font-size: 8.5pt; color: var(--text-muted); font-style: italic; }
body.dark .dt-col { background: linear-gradient(135deg, var(--slide-bg), var(--slide-bg-2)); }

/* =================== RINGKASAN (slide 20) =================== */
.ringkasan-cards {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14px;
  margin-bottom: 16px;
}
.ringkasan-card {
  background: linear-gradient(135deg, var(--navy), var(--navy-dark));
  color: #fff;
  border-radius: 14px;
  padding: 20px;
  text-align: center;
  position: relative;
}
.ringkasan-card.t2 { background: linear-gradient(135deg, #1e88e5, #0d47a1); }
.ringkasan-card.t3 { background: linear-gradient(135deg, var(--red), var(--red-dark)); }
.ringkasan-card .rc-year {
  font-size: 9pt;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  font-weight: 700;
  opacity: 0.85;
}
.ringkasan-card .rc-fokus {
  font-size: 14pt;
  font-weight: 800;
  margin: 6px 0 10px;
}
.ringkasan-card .rc-bullet {
  font-size: 9.5pt;
  padding: 4px 0;
  border-top: 1px solid rgba(255,255,255,0.2);
}
.ringkasan-output {
  background: linear-gradient(135deg, var(--amber), #d97706);
  color: #fff;
  border-radius: 14px;
  padding: 18px 24px;
  text-align: center;
}
.ringkasan-output .rio-label {
  font-size: 9.5pt;
  letter-spacing: 1.5px;
  font-weight: 700;
  text-transform: uppercase;
  opacity: 0.85;
}
.ringkasan-output .rio-text {
  font-size: 16pt;
  font-weight: 800;
  margin-top: 6px;
}
```

- [ ] **Step 9: Tambah CSS PROTON 3-tahun card (slide 11) dan stepper Proton (slide 12/13)**

```css
/* =================== PROTON 3-TAHUN (slide 11) =================== */
.proton-tahun-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14px;
}
.proton-tahun-card {
  background: linear-gradient(135deg, #fff, #f8fafc);
  border-top: 4px solid var(--navy);
  border: 1.5px solid var(--border);
  border-radius: 14px;
  padding: 18px 20px;
  position: relative;
}
.proton-tahun-card.tahun-3 {
  border-top-color: var(--red);
  background: linear-gradient(135deg, #fff5f5, #fff);
}
.proton-tahun-card .tahun-eyebrow {
  font-size: 8pt;
  font-weight: 800;
  letter-spacing: 1.2px;
  color: var(--slate);
  text-transform: uppercase;
}
.proton-tahun-card .tahun-title {
  font-size: 17pt;
  font-weight: 800;
  color: var(--navy);
  margin: 4px 0 10px;
}
.proton-tahun-card.tahun-3 .tahun-title { color: var(--red); }
.proton-tahun-card .tahun-track {
  font-size: 9pt;
  color: var(--text-muted);
  font-weight: 600;
  margin-bottom: 10px;
}
.proton-tahun-card .tahun-bullets { list-style: none; padding: 0; }
.proton-tahun-card .tahun-bullets li {
  font-size: 9.5pt;
  padding: 4px 0;
  display: flex;
  gap: 6px;
}
.offline-badge {
  position: absolute;
  top: 14px;
  right: 14px;
  background: rgba(237, 28, 36, 0.15);
  color: var(--red);
  font-size: 7.5pt;
  font-weight: 800;
  padding: 3px 8px;
  border-radius: 6px;
  letter-spacing: 0.5px;
}
body.dark .proton-tahun-card { background: linear-gradient(135deg, var(--slide-bg), var(--slide-bg-2)); }

/* =================== PROTON ALUR STEPPER (slide 12/13) =================== */
.alur-stepper {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
  margin-bottom: 14px;
  align-items: stretch;
}
.alur-step {
  background: #fff;
  border: 1.5px solid var(--border);
  border-radius: 12px;
  padding: 12px 14px;
  text-align: center;
  position: relative;
}
.alur-stepper.offline-mode .alur-step { border-top: 3px solid var(--amber); }
.alur-step + .alur-step::before {
  content: '→';
  position: absolute;
  left: -10px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 13pt;
  color: var(--slate);
  font-weight: 800;
}
.alur-step .as-num {
  display: inline-block;
  width: 24px; height: 24px;
  line-height: 24px;
  border-radius: 50%;
  background: var(--navy);
  color: #fff;
  font-weight: 800;
  font-size: 9pt;
  margin-bottom: 6px;
}
.alur-stepper.offline-mode .alur-step .as-num { background: var(--amber); }
.alur-step .as-title { font-weight: 700; color: var(--navy); font-size: 10pt; margin-bottom: 3px; }
.alur-step .as-desc { font-size: 8pt; color: var(--text-muted); line-height: 1.3; }
.alur-crossref {
  background: rgba(0, 150, 64, 0.06);
  border-left: 3px solid var(--green);
  padding: 8px 12px;
  font-size: 9pt;
  margin-bottom: 10px;
  border-radius: 6px;
}
.alur-warning {
  background: rgba(245, 158, 11, 0.08);
  border-left: 3px solid var(--amber);
  padding: 8px 12px;
  font-size: 9pt;
  font-weight: 600;
  color: var(--text);
  border-radius: 6px;
}
.alur-callout {
  background: rgba(237, 28, 36, 0.06);
  border-left: 3px solid var(--red);
  padding: 8px 12px;
  font-size: 9pt;
  font-weight: 600;
  border-radius: 6px;
  margin-top: 10px;
}
body.dark .alur-step { background: var(--slide-bg); }
```

- [ ] **Step 10: Verifikasi file masih valid**

Buka file di browser. Pastikan tidak ada error console. Slide existing (1-15 lama) masih render normal walaupun konten belum diganti.

- [ ] **Step 11: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi): add CSS tokens for v3 slide variants

New classes: role-chips, jenis-grid, swim-row, module-card.faded,
akses-status, progresi-table, tree-grid, pp-stepper, dt-grid,
ringkasan-cards, proton-tahun-grid, alur-stepper. Style only, no
markup changes yet.

Refs spec docs/superpowers/specs/2026-05-25-sosialisasi-portalhc-v3-merge-design.md"
```

---

## Task 3: Slide 1 — Cover (replace data)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 1 cover block, lines ~1062-1081)

- [ ] **Step 1: Cari slide cover lama**

Cari blok:
```html
<div class="slide cover active" data-slide="1">
  ...
</div>
```

- [ ] **Step 2: Ganti dengan konten baru**

```html
<div class="slide cover active" data-slide="1">
  <div class="cover-strip"></div>
  <div class="cover-content">
    <div class="logo-box">
      <div class="logo-mark">HC</div>
      <div class="logo-text">
        <div class="logo-main">HC Portal</div>
        <div class="logo-sub">KPB</div>
      </div>
    </div>
    <div class="cover-eyebrow">🎯 Sosialisasi Aplikasi</div>
    <h1 class="cover-title">HC <span class="accent">Portal</span> KPB</h1>
    <p class="cover-subtitle">Human Capital Portal &mdash; Kilang Pertamina Balikpapan</p>
    <p class="cover-subtitle" style="margin-top:8px;font-weight:600;color:var(--red);">📅 Balikpapan · 25 Mei 2026</p>
  </div>
</div>
```

Catatan: 3 tag bawah (Competency/Continuous Dev/Analytics) sudah hilang.

- [ ] **Step 3: Verifikasi browser**

Buka file. Slide 1 harus tampil dengan tanggal "Balikpapan · 25 Mei 2026" bawah subtitle, dan TIDAK ADA tag chip-chip kategori.

- [ ] **Step 4: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s1): cover with 25 Mei 2026, drop 3-tag bar"
```

---

## Task 4: Slide 2 — Agenda (remap targets)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 2 block, lines ~1084-1102)

- [ ] **Step 1: Ganti slide 2 markup**

```html
<div class="slide default-deco" data-slide="2">
  <div class="slide-header">
    <div>
      <h1 class="slide-title">Agenda <span class="accent">Presentasi</span></h1>
      <p class="slide-subtitle">Apa saja yang akan dibahas &mdash; klik untuk lompat ke bagian</p>
    </div>
    <div class="slide-badge">SLIDE 2 / 23</div>
  </div>
  <div class="slide-body">
    <div class="agenda-grid">
      <div class="agenda-item" onclick="goTo(3)"><div class="agenda-num">01</div><div class="agenda-content"><h4>Pengenalan</h4><p>Latar belakang, definisi, 3 platform, role</p></div></div>
      <div class="agenda-item" onclick="goTo(7)"><div class="agenda-num">02</div><div class="agenda-content"><h4>Sistem Assessment</h4><p>2 jenis + 5 kategori + Pre/Post Test + alur 7-step</p></div></div>
      <div class="agenda-item" onclick="goTo(11)"><div class="agenda-num">03</div><div class="agenda-content"><h4>Assessment Proton</h4><p>3 tahun · online &amp; interview offline</p></div></div>
      <div class="agenda-item" onclick="goTo(14)"><div class="agenda-num">04</div><div class="agenda-content"><h4>Coaching Proton (CDP)</h4><p>Dual track, IDP, hierarki, progresi 3 tahun</p></div></div>
      <div class="agenda-item" onclick="goTo(21)"><div class="agenda-num">05</div><div class="agenda-content"><h4>Operasional</h4><p>Integrasi keamanan &amp; cara akses portal</p></div></div>
      <div class="agenda-item" onclick="goTo(23)"><div class="agenda-num">06</div><div class="agenda-content"><h4>Q&amp;A</h4><p>Diskusi dan tanya jawab</p></div></div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Klik tiap item agenda. Konfirmasi navigasi ke slide target. (Slide target belum diganti kontennya — yang penting `goTo()` tidak error.)

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s2): agenda 6 section remap to slide 3/7/11/14/21/23"
```

---

## Task 5: Slide 3 — Latar Belakang (update counter only)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 3, lines ~1105-1143)

- [ ] **Step 1: Update slide badge "SLIDE 3 / 15" → "SLIDE 3 / 23"**

Cari `<div class="slide-badge">SLIDE 3 / 15</div>` dalam slide 3 dan ganti `15` jadi `23`. Konten body slide tidak berubah.

- [ ] **Step 2: Verifikasi browser**

Slide 3 menampilkan "Sebelum vs Sesudah" split panel, badge `SLIDE 3 / 23`.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s3): keep latar belakang, badge counter 23"
```

---

## Task 6: Slide 4 — Apa Itu HC Portal + 3 Prinsip

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 4, lines ~1146-1184)

- [ ] **Step 1: Ganti slide 4 markup**

```html
<div class="slide default-deco" data-slide="4">
  <div class="slide-header">
    <div>
      <h1 class="slide-title">Apa Itu <span class="accent">HC Portal</span> KPB?</h1>
      <p class="slide-subtitle">Sistem informasi terpadu untuk Tim Human Capital Kilang Pertamina Balikpapan</p>
    </div>
    <div class="slide-badge">SLIDE 4 / 23</div>
  </div>
  <div class="slide-body">
    <div class="definition-box">
      <div class="quote-mark">"</div>
      <div class="definition-text">
        Sistem informasi berbasis web Tim <strong>Human Capital Kilang Pertamina Balikpapan</strong> untuk <strong>MENGELOLA · MENGEMBANGKAN · MENDAMPINGI</strong> kompetensi pekerja lewat tiga platform terpadu: CMP, CDP, BP.
      </div>
    </div>
    <div class="pilar-grid" style="margin-top:24px;">
      <div class="pilar-card p1">
        <div class="pilar-num">01</div>
        <div class="pilar-icon-wrap">🎯</div>
        <div class="pilar-title">Terpusat</div>
        <div class="pilar-desc">Satu portal untuk seluruh proses kompetensi &amp; pengembangan pekerja KPB.</div>
      </div>
      <div class="pilar-card p2">
        <div class="pilar-num">02</div>
        <div class="pilar-icon-wrap">📐</div>
        <div class="pilar-title">Terstandar</div>
        <div class="pilar-desc">Kriteria, deliverable, sertifikasi mengacu standard Kilang Pertamina Balikpapan.</div>
      </div>
      <div class="pilar-card p3">
        <div class="pilar-num">03</div>
        <div class="pilar-icon-wrap">📊</div>
        <div class="pilar-title">Terukur</div>
        <div class="pilar-desc">Skor, progress, level kompetensi tertrace per pekerja secara real-time.</div>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 4 tampil quote + 3 prinsip card (Terpusat/Terstandar/Terukur), TANPA 2 module card CMP/CDP lama.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s4): apa itu HC Portal + 3 prinsip Terpusat/Terstandar/Terukur"
```

---

## Task 7: Slide 5 — 3 Platform CMP/CDP/BP

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 5, lines ~1187-1217)

- [ ] **Step 1: Ganti slide 5 markup**

```html
<div class="slide default-deco" data-slide="5">
  <div class="slide-header">
    <div>
      <h1 class="slide-title">3 <span class="accent">Platform</span> Terpadu</h1>
      <p class="slide-subtitle">CMP · CDP · BP — manajemen, pengembangan, &amp; strategic partner</p>
    </div>
    <div class="slide-badge">SLIDE 5 / 23</div>
  </div>
  <div class="slide-body">
    <div class="modules-grid" style="grid-template-columns:repeat(3,1fr);gap:14px;">
      <div class="module-card cmp">
        <div class="module-head">
          <div class="module-icon">📋</div>
          <div>
            <div class="module-name">CMP</div>
            <div class="module-subname">Competency Management Platform</div>
          </div>
        </div>
        <div class="module-desc">Platform digital untuk pengelolaan kompetensi terintegrasi — penyusunan KKJ, IDP, asesmen teknis &amp; Safety.</div>
        <ul class="module-bullets">
          <li>📊 Assessment — Ujian online &amp; sertifikasi</li>
          <li>🎓 Assessment Proton — Program 3-tahun</li>
          <li>🔄 Pre / Post Test — Ukur efektivitas training</li>
          <li>🏆 Sertifikasi — Otomatis + renewal lifecycle</li>
        </ul>
      </div>
      <div class="module-card cdp">
        <div class="module-head">
          <div class="module-icon">🌱</div>
          <div>
            <div class="module-name">CDP</div>
            <div class="module-subname">Competency Development Platform</div>
          </div>
        </div>
        <div class="module-desc">Pembelajaran terstruktur untuk menutup gap kompetensi — prinsip blended Learning (Assignment, Coaching, Self Study).</div>
        <ul class="module-bullets">
          <li>🎯 Coaching Proton — Silabus + deliverable + review multi-role</li>
          <li>📋 IDP — Individual Development Plan tahunan</li>
          <li>📚 Training Records — Riwayat training + sertifikat</li>
        </ul>
      </div>
      <div class="module-card faded">
        <div class="module-head">
          <div class="module-icon">🚧</div>
          <div>
            <div class="module-name">BP</div>
            <div class="module-subname">Business Partner</div>
          </div>
        </div>
        <div class="module-desc">Modul HRBP — strategic partner antara HC &amp; unit operasional untuk workforce planning, employee relations, &amp; advisory.</div>
        <ul class="module-bullets">
          <li>For Future — in roadmap</li>
        </ul>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 5 tampil 3 card sejajar. CMP+CDP normal style, BP card dengan dashed border + opacity rendah + badge "🚧 COMING SOON" di pojok kanan atas.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s5): 3 platform CMP/CDP/BP big-menu with coming-soon badge"
```

---

## Task 8: Slide 6 — Struktur Role 10 role L1-L6 + chip

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 6, lines ~1220-1239)

- [ ] **Step 1: Ganti slide 6 markup**

```html
<div class="slide default-deco" data-slide="6">
  <div class="slide-header">
    <div>
      <h1 class="slide-title">Struktur <span class="accent">Role</span> Pengguna</h1>
      <p class="slide-subtitle">10 role · 6 level — makin tinggi tangga, makin luas authority</p>
    </div>
    <div class="slide-badge">SLIDE 6 / 23</div>
  </div>
  <div class="slide-body">
    <div class="stairs">
      <div class="step s6">
        <div class="step-num">L6</div>
        <div class="step-icon">👨‍🎓</div>
        <div class="role-chips"><span class="role-chip"><span class="chip-icon">👨‍🎓</span>Coachee</span></div>
        <div class="step-access">Self assessment &amp; IDP</div>
      </div>
      <div class="step s5">
        <div class="step-num">L5</div>
        <div class="step-icon">🎓</div>
        <div class="role-chips">
          <span class="role-chip"><span class="chip-icon">🎓</span>Coach</span>
          <span class="role-chip"><span class="chip-icon">👤</span>Supervisor</span>
        </div>
        <div class="step-access">Coaching &amp; review</div>
      </div>
      <div class="step s4">
        <div class="step-num">L4</div>
        <div class="step-icon">🏢</div>
        <div class="role-chips">
          <span class="role-chip"><span class="chip-icon">🏢</span>Section Head</span>
          <span class="role-chip"><span class="chip-icon">🧑‍💼</span>Sr Supervisor</span>
        </div>
        <div class="step-access">Section-level monitor</div>
      </div>
      <div class="step s3">
        <div class="step-num">L3</div>
        <div class="step-icon">👔</div>
        <div class="role-chips">
          <span class="role-chip"><span class="chip-icon">👔</span>Direktur</span>
          <span class="role-chip"><span class="chip-icon">📌</span>VP</span>
          <span class="role-chip"><span class="chip-icon">🧭</span>Manager</span>
        </div>
        <div class="step-access">Executive dashboard</div>
      </div>
      <div class="step s2">
        <div class="step-num">L2</div>
        <div class="step-icon">👥</div>
        <div class="role-chips"><span class="role-chip"><span class="chip-icon">👥</span>HC</span></div>
        <div class="step-access">All section access</div>
      </div>
      <div class="step s1">
        <div class="step-num">L1</div>
        <div class="step-icon">🛡️</div>
        <div class="role-chips"><span class="role-chip"><span class="chip-icon">🛡️</span>Admin</span></div>
        <div class="step-access">Full system control</div>
      </div>
    </div>
    <p class="stair-caption">⬅ Operational Level · · · · · · · · · · · · · · · · · · · · · · · · · · · · · · · · · · · · Higher Authority ➡</p>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 6: tangga bottom-up dengan label L1-L6, multi-role step (L3/L4/L5) tampil chip pill berdampingan.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s6): 10 role granular L1-L6 stairs with role chips"
```

---

## Task 9: Slide 7 — 2 Jenis Assessment (BAGIAN 1 opener)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 7, lines ~1242-1293)

- [ ] **Step 1: Ganti slide 7 markup**

```html
<div class="slide default-deco" data-slide="7">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 1</p>
      <h1 class="slide-title">Sistem <span class="accent">Assessment</span></h1>
      <p class="slide-subtitle">Dua jenis assessment utama di HC Portal</p>
    </div>
    <div class="slide-badge">SLIDE 7 / 23</div>
  </div>
  <div class="slide-body">
    <div class="jenis-grid">
      <div class="jenis-card umum">
        <div class="jenis-head">📊 Assessment Umum</div>
        <div class="jenis-row">
          <div class="jenis-label">Kategori</div>
          <div class="jenis-value">Per batch unit operasi / batch berdurasi</div>
        </div>
        <div class="jenis-row">
          <div class="jenis-label">Metode Ujian</div>
          <div class="jenis-value">Ujian online (pilihan ganda), timer otomatis</div>
        </div>
        <div class="jenis-row">
          <div class="jenis-label">Penilaian</div>
          <div class="jenis-value">Otomatis berdasarkan passing grade</div>
        </div>
      </div>
      <div class="jenis-card proton">
        <div class="jenis-head">🎓 Assessment Proton</div>
        <div class="jenis-row">
          <div class="jenis-label">Kategori</div>
          <div class="jenis-value">Per track per tahun (Panelman/Operator, Tahun 1-3)</div>
        </div>
        <div class="jenis-row">
          <div class="jenis-label">Metode Ujian</div>
          <div class="jenis-value">Online (Th 1-2) + Interview offline (Th 3)</div>
        </div>
        <div class="jenis-row">
          <div class="jenis-label">Penilaian</div>
          <div class="jenis-value">Otomatis (online) + Manual panel (interview)</div>
        </div>
      </div>
    </div>
    <div class="tip-bar">💡 <strong>Assessment Umum</strong> untuk evaluasi reguler per batch unit / jenis kompetensi · <strong>Proton</strong> untuk program pengembangan 3 tahun</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 7: eyebrow "BAGIAN 1" merah, 2 kolom card (Umum biru + Proton merah), tip-bar hijau di bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s7): BAGIAN 1 opener with 2-jenis table card"
```

---

## Task 10: Slide 8 — 5 Kategori Assessment Umum (drop Proton, rename OJT)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 8, lines ~1296-1332)

- [ ] **Step 1: Ganti slide 8 markup**

```html
<div class="slide default-deco" data-slide="8">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 1 · CMP</p>
      <h1 class="slide-title">5 Kategori <span class="accent">Assessment Umum</span></h1>
      <p class="slide-subtitle">Cakupan jenis ujian kompetensi di KPB di luar Proton</p>
    </div>
    <div class="slide-badge">SLIDE 8 / 23</div>
  </div>
  <div class="slide-body">
    <div class="cat-grid">
      <div class="cat-card c1">
        <div class="cat-head"><div class="cat-ic">🔧</div><div class="cat-name">Assessment OJT</div></div>
        <div class="cat-desc">On the Job Training &mdash; ujian kompetensi berbasis unit kerja (Alkylation, RFCC NHT, dll)</div>
      </div>
      <div class="cat-card c2">
        <div class="cat-head"><div class="cat-ic">🏫</div><div class="cat-name">IHT</div></div>
        <div class="cat-desc">In House Training &mdash; assessment terkait pelatihan internal perusahaan</div>
      </div>
      <div class="cat-card c3">
        <div class="cat-head"><div class="cat-ic">📜</div><div class="cat-name">Licencor</div></div>
        <div class="cat-desc">Training Licencor &mdash; lisensi &amp; sertifikasi eksternal</div>
      </div>
      <div class="cat-card c4">
        <div class="cat-head"><div class="cat-ic">📍</div><div class="cat-name">OTS</div></div>
        <div class="cat-desc">On The Spot &mdash; assessment langsung di lapangan</div>
      </div>
      <div class="cat-card c5">
        <div class="cat-head"><div class="cat-ic">⚠️</div><div class="cat-name">HSSE</div></div>
        <div class="cat-desc">Mandatory Health, Safety, Security &amp; Environment training</div>
      </div>
    </div>
    <div class="tip-bar" style="margin-top:14px;">📌 <strong>Assessment Proton</strong> dibahas di Bagian 2 — sistem track 3-tahun Panelman/Operator</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 8: 5 card (OJT bukan OJ, drop Proton). Tip-bar bawah info Bagian 2.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s8): 5 kategori Umum (OJT/IHT/Licencor/OTS/HSSE), Proton ke Bagian 2"
```

---

## Task 11: Slide 9 — Pre & Post Test + Gain Score

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 9, lines ~1335-1386)

- [ ] **Step 1: Ganti slide 9 markup**

```html
<div class="slide default-deco" data-slide="9">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 1 · CMP</p>
      <h1 class="slide-title">Pre &amp; Post Test &mdash; <span class="accent">Gain Score</span></h1>
      <p class="slide-subtitle">Pasangan ujian sebelum &amp; sesudah training untuk hitung peningkatan kompetensi</p>
    </div>
    <div class="slide-badge">SLIDE 9 / 23</div>
  </div>
  <div class="slide-body">
    <div class="pp-stepper">
      <div class="pp-step">
        <div class="pp-num">1</div>
        <span class="pp-icon">📋</span>
        <div class="pp-title">Pre Test</div>
        <div class="pp-desc">Sebelum training, ukur baseline kompetensi peserta</div>
      </div>
      <div class="pp-step">
        <div class="pp-num">2</div>
        <span class="pp-icon">🎓</span>
        <div class="pp-title">Training</div>
        <div class="pp-desc">Sesi pembelajaran in-class atau on-the-job</div>
      </div>
      <div class="pp-step">
        <div class="pp-num">3</div>
        <span class="pp-icon">✅</span>
        <div class="pp-title">Post Test</div>
        <div class="pp-desc">Setelah training, ujian dengan paket soal sejenis</div>
      </div>
      <div class="pp-step">
        <div class="pp-num">4</div>
        <span class="pp-icon">📊</span>
        <div class="pp-title">Gain Score</div>
        <div class="pp-desc">Analisis selisih skor Post &mdash; Pre</div>
      </div>
    </div>
    <div class="pp-metrics">
      <div class="pp-metric">
        <h5>📈 Gain Score</h5>
        <p>Selisih skor Post &minus; Pre. Indikator efektivitas training per peserta &amp; per kategori.</p>
      </div>
      <div class="pp-metric alt">
        <h5>🔍 Item Analysis</h5>
        <p>Per-soal: tingkat kesulitan, daya beda, distractor power. Bantu HC perbaiki paket soal.</p>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 9: 4 step horizontal dengan arrow antar step, 2 metric card bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s9): Pre & Post Test 4-step + Gain Score & Item Analysis"
```

---

## Task 12: Slide 10 — Alur Assessment 7 step (3-swimlane)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 10, lines ~1388-1432)

- [ ] **Step 1: Ganti slide 10 markup**

```html
<div class="slide default-deco" data-slide="10">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 1 · ALUR</p>
      <h1 class="slide-title">Alur <span class="accent">Assessment</span> &mdash; 7 Step End-to-End</h1>
      <p class="slide-subtitle">Persiapan → Pelaksanaan → Penilaian</p>
    </div>
    <div class="slide-badge">SLIDE 10 / 23</div>
  </div>
  <div class="slide-body">
    <div class="swim-row" style="grid-template-columns:2fr 3fr 2fr;">
      <div class="swim-lane lane-prep">
        <div class="swim-label">📁 Persiapan (1-2)</div>
        <div class="swim-steps">
          <div class="swim-step">
            <span class="step-num-mini">1</span>
            <span class="step-icon-mini">📁</span>
            <div class="step-title">Persiapan Data</div>
            <div class="step-desc">Kategori per unit, organisasi, daftar pekerja</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">2</span>
            <span class="step-icon-mini">📝</span>
            <div class="step-title">Buat Assessment</div>
            <div class="step-desc">Pilih kategori, set durasi &amp; soal</div>
          </div>
        </div>
      </div>
      <div class="swim-lane lane-exec">
        <div class="swim-label">⚙ Pelaksanaan (3-5)</div>
        <div class="swim-steps">
          <div class="swim-step">
            <span class="step-num-mini">3</span>
            <span class="step-icon-mini">💻</span>
            <div class="step-title">Peserta Ujian</div>
            <div class="step-desc">Login portal, sistem random soal, timer otomatis</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">4</span>
            <span class="step-icon-mini">👁</span>
            <div class="step-title">Monitoring</div>
            <div class="step-desc">Pantau real-time, akhiri manual bila perlu</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">5</span>
            <span class="step-icon-mini">📤</span>
            <div class="step-title">Submit</div>
            <div class="step-desc">Manual atau auto-submit saat timer habis</div>
          </div>
        </div>
      </div>
      <div class="swim-lane lane-result">
        <div class="swim-label">🏆 Penilaian (6-7)</div>
        <div class="swim-steps">
          <div class="swim-step">
            <span class="step-num-mini">6</span>
            <span class="step-icon-mini">⚙</span>
            <div class="step-title">Penilaian Otomatis</div>
            <div class="step-desc">Skor vs passing grade otomatis</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">7</span>
            <span class="step-icon-mini">🏆</span>
            <div class="step-title">Hasil &amp; Laporan</div>
            <div class="step-desc">Sertifikasi + rekap per unit/kategori</div>
          </div>
        </div>
      </div>
    </div>
    <div class="output-bar">📤 <strong>Output:</strong> skor pekerja · status kelulusan · rekap per unit</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 10: 3 swimlane warna beda (hijau/biru/merah), 7 step terdistribusi 2-3-2. Output bar gradient navy bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s10): alur assessment 7-step 3-swimlane (Persiapan/Pelaksanaan/Penilaian)"
```

---

## Task 13: Slide 11 — Assessment Proton 3 Tahun (BAGIAN 2 opener)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 11, lines ~1435-1475 — slide Dashboard Analytics lama, **diganti penuh**)

- [ ] **Step 1: Ganti slide 11 markup penuh (drop Dashboard Analytics)**

```html
<div class="slide default-deco" data-slide="11">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 2</p>
      <h1 class="slide-title">Assessment <span class="accent">Proton</span></h1>
      <p class="slide-subtitle">Program 3 tahun · 1 track per role (Panelman / Operator)</p>
    </div>
    <div class="slide-badge">SLIDE 11 / 23</div>
  </div>
  <div class="slide-body">
    <div class="proton-tahun-grid">
      <div class="proton-tahun-card">
        <div class="tahun-eyebrow">Tahun Pertama</div>
        <div class="tahun-title">Tahun 1</div>
        <div class="tahun-track">Panelman / Operator</div>
        <ul class="tahun-bullets">
          <li>🎯 Track dasar per role</li>
          <li>💻 Ujian online pilihan ganda</li>
          <li>📚 Fokus: kompetensi dasar</li>
        </ul>
      </div>
      <div class="proton-tahun-card">
        <div class="tahun-eyebrow">Tahun Kedua</div>
        <div class="tahun-title">Tahun 2</div>
        <div class="tahun-track">Panelman / Operator</div>
        <ul class="tahun-bullets">
          <li>🎯 Track lanjutan</li>
          <li>💻 Ujian online pilihan ganda</li>
          <li>📚 Fokus: pendalaman proses</li>
        </ul>
      </div>
      <div class="proton-tahun-card tahun-3">
        <span class="offline-badge">🎤 OFFLINE INTERVIEW</span>
        <div class="tahun-eyebrow">Tahun Ketiga</div>
        <div class="tahun-title">Tahun 3</div>
        <div class="tahun-track">Panelman / Operator</div>
        <ul class="tahun-bullets">
          <li>🎯 Track mahir</li>
          <li>🎤 Interview offline oleh panel juri</li>
          <li>📚 Fokus: penguasaan penuh</li>
        </ul>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 11: 3 card Tahun 1/2/3. Tahun 3 punya badge offline merah di pojok kanan atas + border top merah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s11): BAGIAN 2 Assessment Proton 3-tahun (drop Dashboard fiktif)"
```

---

## Task 14: Slide 12 — Alur Proton Th 1 & 2 (Online)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 12, lines ~1478-1514 — slide Integrasi lama, **akan dipindah** ke slide 21 nanti, sekarang konten Alur Proton)

Catatan: slide 12 lama (Integrasi & Keamanan) **belum** hilang — konten Integrasi akan dipindah ke slot slide 21 nanti di task 23. Untuk sekarang, replace markup slide 12 dengan Alur Proton.

- [ ] **Step 1: Ganti slide 12 markup penuh**

```html
<div class="slide default-deco" data-slide="12">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 2 · ALUR</p>
      <h1 class="slide-title">Alur Proton &mdash; Tahun <span class="accent">1 &amp; 2</span></h1>
      <p class="slide-subtitle">Ujian Online (Pilihan Ganda)</p>
    </div>
    <div class="slide-badge">SLIDE 12 / 23</div>
  </div>
  <div class="slide-body">
    <div class="alur-stepper">
      <div class="alur-step">
        <span class="as-num">1</span>
        <div class="as-title">Buat Assessment</div>
        <div class="as-desc">Kategori "Assessment Proton", pilih track (Operator/Panelman) &amp; tahun</div>
      </div>
      <div class="alur-step">
        <span class="as-num">2</span>
        <div class="as-title">Set Paket Soal</div>
        <div class="as-desc">Pilih paket sesuai track tahun, set durasi &amp; passing grade</div>
      </div>
      <div class="alur-step">
        <span class="as-num">3</span>
        <div class="as-title">Peserta Ujian Online</div>
        <div class="as-desc">Login portal, kerjakan soal dalam timer otomatis</div>
      </div>
      <div class="alur-step">
        <span class="as-num">4</span>
        <div class="as-title">Penilaian Otomatis</div>
        <div class="as-desc">Skor otomatis, laporan lulus/tidak per peserta</div>
      </div>
    </div>
    <div class="alur-crossref">💡 <strong>Mirip Assessment Umum</strong> &mdash; beda di kategori &amp; paket soal per track</div>
    <div class="alur-warning">⚠ <strong>Wajib lulus Tahun N</strong> untuk lanjut ke Tahun N+1</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 12: 4 step horizontal dengan arrow, cross-ref hijau, warning kuning.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s12): alur Proton Th 1 & 2 (online, 4 step)"
```

---

## Task 15: Slide 13 — Alur Proton Th 3 (Interview Offline)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 13, lines ~1517-1564 — slide Progress lama, dropped)

- [ ] **Step 1: Ganti slide 13 markup penuh**

```html
<div class="slide default-deco" data-slide="13">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 2 · ALUR</p>
      <h1 class="slide-title">Alur Proton &mdash; Tahun <span class="accent">3</span></h1>
      <p class="slide-subtitle">Interview Offline (Tatap Muka, Panel Juri)</p>
    </div>
    <div class="slide-badge" style="background:var(--amber);">🎤 OFFLINE MODE</div>
  </div>
  <div class="slide-body">
    <div class="alur-stepper offline-mode">
      <div class="alur-step">
        <span class="as-num">1</span>
        <div class="as-title">Buat Assessment</div>
        <div class="as-desc">Pilih track Tahun 3. <strong>Tanpa</strong> durasi &amp; paket soal</div>
      </div>
      <div class="alur-step">
        <span class="as-num">2</span>
        <div class="as-title">Interview Offline</div>
        <div class="as-desc">Panel juri tatap muka, peserta presentasi &amp; dijuri</div>
      </div>
      <div class="alur-step">
        <span class="as-num">3</span>
        <div class="as-title">Penilaian Kompetensi</div>
        <div class="as-desc">Penilaian kompetensi oleh panel juri</div>
      </div>
      <div class="alur-step">
        <span class="as-num">4</span>
        <div class="as-title">Rekap &amp; Sertifikasi</div>
        <div class="as-desc">Input skor ke sistem, sertifikasi level kompetensi</div>
      </div>
    </div>
    <div class="alur-callout">🔔 <strong>Sistem hanya untuk input skor &amp; rekap</strong> &mdash; bukan ujian online</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 13: badge "OFFLINE MODE" amber di header, stepper offline-mode (border-top amber), callout merah bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s13): alur Proton Th 3 interview offline (drop Progress slide)"
```

---

## Task 16: Slide 14 — Coaching Proton Dual Track (BAGIAN 3 opener)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 14, lines ~1566-1598 — slide Cara Akses lama, dipindah ke slide 22 nanti)

- [ ] **Step 1: Ganti slide 14 markup**

```html
<div class="slide default-deco" data-slide="14">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 3 · CDP</p>
      <h1 class="slide-title">Coaching <span class="accent">Proton</span> &mdash; Dual Track</h1>
      <p class="slide-subtitle">Program 3 tahun pengembangan kompetensi · 2 track independen</p>
    </div>
    <div class="slide-badge">SLIDE 14 / 23</div>
  </div>
  <div class="slide-body">
    <div class="dt-grid">
      <div class="dt-col">
        <div class="dt-head">👷 Panelman</div>
        <div class="dt-pills">
          <span class="dt-pill">Th 1</span>
          <span class="dt-pill">Th 2</span>
          <span class="dt-pill">Th 3</span>
        </div>
        <p class="dt-note">3 track terpisah · hierarki &amp; deliverable independen</p>
      </div>
      <div class="dt-col operator">
        <div class="dt-head">🔧 Operator</div>
        <div class="dt-pills">
          <span class="dt-pill">Th 1</span>
          <span class="dt-pill">Th 2</span>
          <span class="dt-pill">Th 3</span>
        </div>
        <p class="dt-note">3 track terpisah · hierarki &amp; deliverable independen</p>
      </div>
    </div>
    <div class="tip-bar">💡 <strong>Setiap track berdiri sendiri</strong> &mdash; hierarki kompetensi &amp; deliverable independen. Pekerja dipromosikan setiap tahun setelah semua deliverable selesai.</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 14: 2 kolom besar (Panelman biru / Operator merah), masing-masing 3 pill Th 1/2/3, tip hijau bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s14): BAGIAN 3 Coaching dual track Panelman/Operator"
```

---

## Task 17: Slide 15 — IDP & Training Records

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (slide 15, lines ~1600-1617 — slide penutup lama, dipindah ke slide 23 nanti)

- [ ] **Step 1: Ganti slide 15 markup**

```html
<div class="slide default-deco" data-slide="15">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 3 · CDP</p>
      <h1 class="slide-title">IDP &amp; <span class="accent">Training Records</span></h1>
      <p class="slide-subtitle">Dua komponen pelengkap Coaching Proton di CDP</p>
    </div>
    <div class="slide-badge">SLIDE 15 / 23</div>
  </div>
  <div class="slide-body">
    <div class="modules-grid" style="grid-template-columns:1fr 1fr;gap:18px;">
      <div class="module-card cmp">
        <div class="module-head">
          <div class="module-icon">📋</div>
          <div>
            <div class="module-name">IDP</div>
            <div class="module-subname">Individual Development Plan (Perpustakaan)</div>
          </div>
        </div>
        <ul class="module-bullets">
          <li>📂 Repository dokumen IDP per pekerja</li>
          <li>📄 Akses dokumen KKJ (Kebutuhan Kompetensi Jabatan)</li>
          <li>👁 Worker view &amp; download dokumen</li>
          <li>🔍 Filter &amp; search per jabatan / unit</li>
        </ul>
      </div>
      <div class="module-card cdp">
        <div class="module-head">
          <div class="module-icon">📚</div>
          <div>
            <div class="module-name">Training Records</div>
            <div class="module-subname">Riwayat Pelatihan</div>
          </div>
        </div>
        <ul class="module-bullets">
          <li>🏫 Training internal &amp; eksternal</li>
          <li>🏷️ Kategori + sub-kategori</li>
          <li>📎 Sertifikat upload (PDF/image)</li>
          <li>⏳ Validity period &amp; renewal</li>
        </ul>
      </div>
    </div>
    <div class="tip-bar" style="margin-top:14px;">💡 IDP &amp; Training Records <strong>terintegrasi dengan profile pekerja</strong> &mdash; jadi referensi gap analysis &amp; promosi.</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 15: 2 module card (IDP + Training Records), 4 bullet masing-masing. Tip-bar hijau bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s15): IDP & Training Records"
```

---

## Task 18: Slide 16 — Hierarki Kompetensi (tree + contoh)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sisipkan slide 16 baru setelah slide 15)

Catatan: slide 16+ semua slide baru — perlu ditambahkan setelah penutup lama. Atau cleaner: simply append `<div class="slide default-deco" data-slide="16">...</div>` setelah slide 15. JS `showSlide()` cuma butuh `[data-slide="N"]` selector.

- [ ] **Step 1: Sisipkan slide 16 markup baru**

Setelah closing `</div>` slide 15 (sebelum closing deck container), tambahkan:

```html
<div class="slide default-deco" data-slide="16">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 3 · STRUKTUR</p>
      <h1 class="slide-title">Hierarki <span class="accent">Kompetensi</span> per Track</h1>
      <p class="slide-subtitle">Track → Kompetensi → Sub-Kompetensi → Deliverable</p>
    </div>
    <div class="slide-badge">SLIDE 16 / 23</div>
  </div>
  <div class="slide-body">
    <div class="tree-grid">
      <div class="tree-col generic">
        <h4>Struktur Umum</h4>
        <div class="tree-node">📁 Track<span class="node-level">Level 0</span></div>
        <div class="tree-arrow">↓</div>
        <div class="tree-node">📂 Kompetensi<span class="node-level">Level 1</span></div>
        <div class="tree-arrow">↓</div>
        <div class="tree-node">📄 Sub-Kompetensi<span class="node-level">Level 2</span></div>
        <div class="tree-arrow">↓</div>
        <div class="tree-node">🎯 Deliverable<span class="node-level">Output Konkret</span></div>
      </div>
      <div class="tree-col example">
        <h4>Contoh Konkret</h4>
        <div class="tree-node">📁 Operator &mdash; Tahun 1<span class="node-level">Track</span></div>
        <div class="tree-arrow">↓</div>
        <div class="tree-node">📂 Safety Operation<span class="node-level">Kompetensi</span></div>
        <div class="tree-arrow">↓</div>
        <div class="tree-node">📄 LOTO (Lock Out Tag Out)<span class="node-level">Sub-Kompetensi</span></div>
        <div class="tree-arrow">↓</div>
        <div class="tree-node">🎯 Submit prosedur LOTO unit X<span class="node-level">Deliverable</span></div>
      </div>
    </div>
    <div class="tip-bar">💡 <strong>Independen per track</strong> &mdash; tidak shared. Semua deliverable selesai = lulus track → promosi ke tahun berikutnya.</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Tekan tombol Next sampai counter "16 / 23". Slide 16 tampil 2 kolom tree (generic + contoh), 4 node masing-masing.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s16): hierarki kompetensi tree + contoh konkret"
```

---

## Task 19: Slide 17 — Progresi 5 Aspek (tabel highlight diff)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sisipkan slide 17 setelah slide 16)

- [ ] **Step 1: Sisipkan slide 17 markup**

```html
<div class="slide default-deco" data-slide="17">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">BAGIAN 3 · PROGRESI</p>
      <h1 class="slide-title">Progresi <span class="accent">Kompetensi</span> per Tahun</h1>
      <p class="slide-subtitle">5 aspek pembanding antara Tahun 1, 2, dan 3</p>
    </div>
    <div class="slide-badge">SLIDE 17 / 23</div>
  </div>
  <div class="slide-body">
    <table class="progresi-table">
      <thead>
        <tr>
          <th>Aspek</th>
          <th>Tahun 1</th>
          <th>Tahun 2</th>
          <th>Tahun 3</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="aspek-label">🎯 Fokus</td>
          <td>Dasar &amp; pengenalan unit kerja</td>
          <td>Lanjutan &amp; pendalaman proses</td>
          <td class="diff-cell">Mahir &amp; penguasaan penuh</td>
        </tr>
        <tr>
          <td class="aspek-label">📦 Deliverable</td>
          <td>Khusus Tahun 1 (beda dari Th 2 &amp; 3)</td>
          <td>Khusus Tahun 2 (beda dari Th 1 &amp; 3)</td>
          <td class="diff-cell">Khusus Tahun 3 (beda dari Th 1 &amp; 2)</td>
        </tr>
        <tr>
          <td class="aspek-label">🔄 Coaching Process</td>
          <td>Submit Evidence → Multi Approval → Final Assessment</td>
          <td>Submit Evidence → Multi Approval → Final Assessment</td>
          <td class="diff-cell">Submit Evidence → Multi Approval → <strong>Final Assessment Interview</strong></td>
        </tr>
        <tr>
          <td class="aspek-label">📝 Assessment</td>
          <td>Ujian online (pilihan ganda)</td>
          <td>Ujian online (pilihan ganda)</td>
          <td class="diff-cell"><strong>Interview offline panel juri</strong></td>
        </tr>
        <tr>
          <td class="aspek-label">🏆 Akhir Tahun</td>
          <td>Sertifikasi Th 1 → lanjut Tahun 2</td>
          <td>Sertifikasi Th 2 → lanjut Tahun 3</td>
          <td class="diff-cell"><strong>Sertifikasi Final → Pekerja Kompeten Penuh</strong></td>
        </tr>
      </tbody>
    </table>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 17: tabel 4 kolom × 5 baris, kolom Tahun 3 di-highlight (background gradient red/amber, text bold merah).

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s17): progresi 5 aspek tabel + highlight Th 3"
```

---

## Task 20: Slide 18 — Alur Coaching Th 1 & 2 (8 step, 3-swimlane)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sisipkan slide 18 setelah slide 17)

- [ ] **Step 1: Sisipkan slide 18 markup**

```html
<div class="slide default-deco" data-slide="18">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">COACHING · TAHUN 1 &amp; 2</p>
      <h1 class="slide-title">Alur <span class="accent">Coaching</span> &mdash; 8 Step</h1>
      <p class="slide-subtitle">Persiapan Silabus → Review Multi-Role → Sertifikasi</p>
    </div>
    <div class="slide-badge">SLIDE 18 / 23</div>
  </div>
  <div class="slide-body">
    <div class="swim-row" style="grid-template-columns:3fr 3fr 2fr;">
      <div class="swim-lane lane-prep">
        <div class="swim-label">📋 Persiapan (1-3)</div>
        <div class="swim-steps">
          <div class="swim-step">
            <span class="step-num-mini">1</span>
            <span class="step-icon-mini">📋</span>
            <div class="step-title">Silabus</div>
            <div class="step-desc">Kompetensi &amp; deliverable per track tahun</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">2</span>
            <span class="step-icon-mini">📤</span>
            <div class="step-title">Guidance</div>
            <div class="step-desc">Dokumen panduan belajar per kompetensi</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">3</span>
            <span class="step-icon-mini">🔗</span>
            <div class="step-title">Assign Coachee</div>
            <div class="step-desc">HC assign coachee ke track tahun</div>
          </div>
        </div>
      </div>
      <div class="swim-lane lane-exec">
        <div class="swim-label">👀 Review Multi-Role (4-6)</div>
        <div class="swim-steps">
          <div class="swim-step">
            <span class="step-num-mini">4</span>
            <span class="step-icon-mini">📥</span>
            <div class="step-title">Submit Evidence</div>
            <div class="step-desc">Coach submit evidence per deliverable</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">5</span>
            <span class="step-icon-mini">👀</span>
            <div class="step-title">Review Multi-Role</div>
            <div class="step-desc">Coach + SrSpv + SH + HC paralel</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">6</span>
            <span class="step-icon-mini">✅</span>
            <div class="step-title">Approval / Revisi</div>
            <div class="step-desc">Approve atau request revisi dgn komentar</div>
          </div>
        </div>
      </div>
      <div class="swim-lane lane-result">
        <div class="swim-label">🏅 Sertifikasi (7-8)</div>
        <div class="swim-steps">
          <div class="swim-step">
            <span class="step-num-mini">7</span>
            <span class="step-icon-mini">📊</span>
            <div class="step-title">Hitung Progress</div>
            <div class="step-desc">% penyelesaian deliverable dalam track</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">8</span>
            <span class="step-icon-mini">🏅</span>
            <div class="step-title">Sertifikasi</div>
            <div class="step-desc">Lulus tahun, naik ke tahun berikutnya</div>
          </div>
        </div>
      </div>
    </div>
    <div class="output-bar">✅ <strong>Output:</strong> sertifikat tahun + eligible naik tahun berikutnya</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 18: 3 swimlane (Persiapan 3 step / Review 3 step / Sertifikasi 2 step), total 8 step.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s18): alur coaching Th 1 & 2 8-step 3-swimlane"
```

---

## Task 21: Slide 19 — Alur Coaching Th 3 Mahir (8 step + diff highlight)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sisipkan slide 19 setelah slide 18)

- [ ] **Step 1: Sisipkan slide 19 markup**

```html
<div class="slide default-deco" data-slide="19">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">COACHING · TAHUN 3 (MAHIR)</p>
      <h1 class="slide-title">Alur Coaching <span class="accent">Mahir</span> &mdash; 8 Step</h1>
      <p class="slide-subtitle">Silabus Mahir → Review Multi-Role → Sertifikasi Final</p>
    </div>
    <div class="slide-badge" style="background:var(--red);">🎯 LEVEL MAHIR</div>
  </div>
  <div class="slide-body">
    <div class="swim-row" style="grid-template-columns:3fr 3fr 2fr;">
      <div class="swim-lane lane-prep">
        <div class="swim-label">🎓 Silabus Mahir (1-3)</div>
        <div class="swim-steps">
          <div class="swim-step highlight">
            <span class="step-num-mini">1</span>
            <span class="step-icon-mini">🎓</span>
            <div class="step-title">Silabus Mahir</div>
            <div class="step-desc">Level mahir, beda dari Th 1-2</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">2</span>
            <span class="step-icon-mini">🔗</span>
            <div class="step-title">Mapping Th 3</div>
            <div class="step-desc">Coachee lulus Th 2 → track Th 3</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">3</span>
            <span class="step-icon-mini">✍</span>
            <div class="step-title">Kerjakan Deliverable</div>
            <div class="step-desc">Submit evidence level mahir</div>
          </div>
        </div>
      </div>
      <div class="swim-lane lane-exec">
        <div class="swim-label">👀 Review Multi-Role (4-6)</div>
        <div class="swim-steps">
          <div class="swim-step">
            <span class="step-num-mini">4</span>
            <span class="step-icon-mini">👀</span>
            <div class="step-title">Review Multi-Role</div>
            <div class="step-desc">Coach + SrSpv + SH + HC paralel</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">5</span>
            <span class="step-icon-mini">✅</span>
            <div class="step-title">Approval / Revisi</div>
            <div class="step-desc">Approve atau request revisi dgn komentar</div>
          </div>
          <div class="swim-step">
            <span class="step-num-mini">6</span>
            <span class="step-icon-mini">📊</span>
            <div class="step-title">Hitung Progress</div>
            <div class="step-desc">% deliverable + skor review</div>
          </div>
        </div>
      </div>
      <div class="swim-lane lane-result">
        <div class="swim-label">🏆 Sertifikasi Final (7-8)</div>
        <div class="swim-steps">
          <div class="swim-step highlight">
            <span class="step-num-mini">7</span>
            <span class="step-icon-mini">🏆</span>
            <div class="step-title">Sertifikasi Final</div>
            <div class="step-desc">Semua deliverable Th 3 selesai</div>
          </div>
          <div class="swim-step highlight">
            <span class="step-num-mini">8</span>
            <span class="step-icon-mini">⭐</span>
            <div class="step-title">Penetapan Level</div>
            <div class="step-desc">Tetapkan level kompetensi pekerja</div>
          </div>
        </div>
      </div>
    </div>
    <div class="output-bar" style="background:linear-gradient(90deg,var(--red),var(--red-dark));">🏆 <strong>Output:</strong> Pekerja kompeten penuh + sertifikasi final + eligible role advance</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 19: badge "LEVEL MAHIR" merah di header, step 1, 7, 8 di-highlight amber (border + background). Output bar merah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s19): alur coaching Th 3 mahir 8-step + diff highlight"
```

---

## Task 22: Slide 20 — Ringkasan Program Proton

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sisipkan slide 20 setelah slide 19)

- [ ] **Step 1: Sisipkan slide 20 markup**

```html
<div class="slide default-deco" data-slide="20">
  <div class="slide-header">
    <div>
      <p style="font-size:8.5pt;color:var(--red);font-weight:800;letter-spacing:1.5px;margin-bottom:4px;">RINGKASAN</p>
      <h1 class="slide-title">Program <span class="accent">Proton</span> &mdash; 3 Tahun</h1>
      <p class="slide-subtitle">Dari kompetensi dasar hingga mahir &amp; sertifikasi final</p>
    </div>
    <div class="slide-badge">SLIDE 20 / 23</div>
  </div>
  <div class="slide-body">
    <div class="ringkasan-cards">
      <div class="ringkasan-card">
        <div class="rc-year">Tahun 1</div>
        <div class="rc-fokus">Kompetensi Dasar</div>
        <div class="rc-bullet">📦 Deliverable Tahun 1</div>
        <div class="rc-bullet">💻 Coaching &amp; Assessment Online</div>
      </div>
      <div class="ringkasan-card t2">
        <div class="rc-year">Tahun 2</div>
        <div class="rc-fokus">Kompetensi Lanjutan</div>
        <div class="rc-bullet">📦 Deliverable Tahun 2</div>
        <div class="rc-bullet">💻 Coaching &amp; Assessment Online</div>
      </div>
      <div class="ringkasan-card t3">
        <div class="rc-year">Tahun 3</div>
        <div class="rc-fokus">Kompetensi Mahir</div>
        <div class="rc-bullet">📦 Deliverable Tahun 3</div>
        <div class="rc-bullet">🎤 Coaching &amp; Review Interview</div>
      </div>
    </div>
    <div class="ringkasan-output">
      <div class="rio-label">🏆 Hasil Akhir</div>
      <div class="rio-text">Pekerja Kompeten · Tersertifikasi · Siap Operasi Kompleks</div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 20: 3 card gradient (navy/biru muda/merah) + output gradient amber bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s20): ringkasan program Proton 3 tahun + hasil akhir"
```

---

## Task 23: Slide 21 — Integrasi & Keamanan (sinkron data)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sisipkan slide 21 setelah slide 20)

- [ ] **Step 1: Sisipkan slide 21 markup**

```html
<div class="slide default-deco" data-slide="21">
  <div class="slide-header">
    <div>
      <h1 class="slide-title">Integrasi &amp; <span class="accent">Keamanan</span></h1>
      <p class="slide-subtitle">Fitur pendukung yang menjaga integritas &amp; kemudahan</p>
    </div>
    <div class="slide-badge">SLIDE 21 / 23</div>
  </div>
  <div class="slide-body">
    <div class="cat-grid">
      <div class="cat-card c1">
        <div class="cat-head"><div class="cat-ic">🔐</div><div class="cat-name">LDAP Pertamina</div></div>
        <div class="cat-desc">Login menggunakan akun Active Directory Pertamina &mdash; Single Sign-On</div>
      </div>
      <div class="cat-card c5">
        <div class="cat-head"><div class="cat-ic">🛡️</div><div class="cat-name">Anti-Copy</div></div>
        <div class="cat-desc">Perlindungan soal ujian online dari copy-paste &mdash; integritas assessment</div>
      </div>
      <div class="cat-card c3">
        <div class="cat-head"><div class="cat-ic">📋</div><div class="cat-name">Audit Log</div></div>
        <div class="cat-desc">Aksi penting tercatat (login, submit, approval) &mdash; transparansi</div>
      </div>
      <div class="cat-card c6">
        <div class="cat-head"><div class="cat-ic">👥</div><div class="cat-name">Role-Based Access</div></div>
        <div class="cat-desc">10 role · 6 level akses sesuai tanggung jawab</div>
      </div>
      <div class="cat-card c2">
        <div class="cat-head"><div class="cat-ic">🔔</div><div class="cat-name">Notifikasi Real-time</div></div>
        <div class="cat-desc">Assessment, approval coaching, renewal sertifikat</div>
      </div>
      <div class="cat-card c4">
        <div class="cat-head"><div class="cat-ic">📥</div><div class="cat-name">Import Excel</div></div>
        <div class="cat-desc">Bulk import training records, assessment, soal &mdash; hemat waktu admin</div>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 21: 6 card 3×2 grid. Role-Based Access menyebut "10 role · 6 level" (sinkron slide 6).

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s21): integrasi & keamanan 6 fitur (sinkron 10 role label)"
```

---

## Task 24: Slide 22 — Cara Akses (Dev aktif / Prod perkiraan)

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sisipkan slide 22 setelah slide 21)

- [ ] **Step 1: Sisipkan slide 22 markup**

```html
<div class="slide default-deco" data-slide="22">
  <div class="slide-header">
    <div>
      <h1 class="slide-title">Cara <span class="accent">Mengakses</span> HC Portal</h1>
      <p class="slide-subtitle">Sekarang fase Development — Production akan menyusul</p>
    </div>
    <div class="slide-badge">SLIDE 22 / 23</div>
  </div>
  <div class="slide-body">
    <div class="akses-grid">
      <div class="akses-card dev">
        <span class="akses-status">✅ AKTIF SEKARANG</span>
        <span class="akses-label">🟢 Development</span>
        <h3>Environment Utama</h3>
        <p>Lingkungan pengembangan &amp; sosialisasi saat ini.</p>
        <div class="url-box">http://10.55.3.3/KPB-PortalHC</div>
        <div class="akses-info">
          <strong>Login:</strong> Akun Active Directory Pertamina<br>
          <strong>Jaringan:</strong> Intranet Pertamina
        </div>
      </div>
      <div class="akses-card prod">
        <span class="akses-status">⏳ BELUM AKTIF</span>
        <span class="akses-label">🚧 Production</span>
        <h3>Environment Target</h3>
        <p>Akses resmi setelah promosi ke Production.</p>
        <div class="url-box">https://appkpb.pertamina.com/KPB-PortalHC</div>
        <span class="url-perkiraan">ⓘ URL perkiraan</span>
        <div class="akses-info">
          <strong>Login:</strong> Akun Active Directory Pertamina<br>
          <strong>Jaringan:</strong> VPN / Intranet Pertamina
        </div>
      </div>
    </div>
    <div class="akses-footer">📌 Saat ini masih development</div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 22: Dev card kiri hijau (AKTIF SEKARANG), Prod card kanan amber (BELUM AKTIF) dengan URL perkiraan italic. Footer amber bawah.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s22): cara akses Dev aktif / Prod URL perkiraan"
```

---

## Task 25: Slide 23 — Penutup + Kontak

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (sisipkan slide 23 setelah slide 22, **gantikan** penutup lama yang sudah dipindah/hilang)

- [ ] **Step 1: Sisipkan slide 23 markup**

```html
<div class="slide penutup" data-slide="23">
  <div class="penutup-content">
    <div class="penutup-icon">🙏</div>
    <h1 class="penutup-title">Terima Kasih</h1>
    <p class="penutup-subtitle">Mari bersama digitalisasi Human Capital Pertamina KPB</p>
    <div class="penutup-qa">
      <span class="penutup-qa-dot"></span>
      Q&amp;A &mdash; Sesi Tanya Jawab
    </div>
    <br>
    <button class="penutup-cta" onclick="window.open('http://10.55.3.3/KPB-PortalHC', '_blank')">
      🚀 Akses HC Portal Sekarang
      <span class="arrow">→</span>
    </button>
    <span class="penutup-url">http://10.55.3.3/KPB-PortalHC</span>
    <div style="margin-top:32px;padding-top:20px;border-top:1px solid rgba(255,255,255,0.2);text-align:center;">
      <div style="font-size:9pt;letter-spacing:1.5px;font-weight:700;opacity:0.7;">📞 KONTAK</div>
      <div style="font-size:13pt;font-weight:700;margin-top:6px;">PT Kilang Pertamina Balikpapan</div>
      <div style="font-size:10pt;opacity:0.85;margin-top:4px;">📅 Balikpapan · 25 Mei 2026</div>
    </div>
  </div>
</div>
```

- [ ] **Step 2: Verifikasi browser**

Slide 23: penutup HTML style + section kontak bawah dengan divider. CTA button arah `http://10.55.3.3/KPB-PortalHC` (Dev), bukan Production.

- [ ] **Step 3: Commit**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "feat(sosialisasi/s23): penutup + kontak PT Kilang Pertamina Balikpapan, CTA → Dev"
```

---

## Task 26: Final QA + Cleanup

**Files:**
- Modify: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (cleanup leftover markup, verify all 23 slides)

- [ ] **Step 1: Hapus markup slide lama yang tidak dipakai**

Cari di file apakah masih ada `data-slide` value di luar range 1-23 (terutama slide HTML lama yang tidak ditimpa karena di-append vs replace). Pastikan tidak ada `data-slide="24"` atau lebih, dan tidak ada slide ganda.

Cek juga: ada `<canvas id="chartCat">`, `<canvas id="chartGain">`, atau class `kpi-card`/`mockup-row`/`mockup-tab` yang tertinggal di markup. Hapus semua.

- [ ] **Step 2: Pastikan total counter benar**

Buka file, search:
- `const total = 23;` (di script)
- 23 `<div class="slide ...">` block (count manually atau via Grep)
- Setiap slide punya `<div class="slide-badge">SLIDE X / 23</div>` dengan X = 1..23 (kecuali slide 1 cover dan slide 23 penutup yang formatnya beda)

- [ ] **Step 3: Test keyboard navigation full deck**

Buka file di Chrome/Edge. Tekan tombol `→` (panah kanan) 22 kali dari slide 1. Verifikasi counter "1/23 → 2/23 → ... → 23/23". Tekan `←` 22 kali balik ke slide 1. Tidak boleh ada error console.

- [ ] **Step 4: Test klik agenda items**

Di slide 2, klik tiap item 01-06. Verifikasi navigasi tepat ke slide 3/7/11/14/21/23.

- [ ] **Step 5: Test dark mode toggle**

Klik tombol "🌙/☀️" di pojok kanan atas. Pastikan semua 23 slide tetap readable di dark mode (text kontras OK, gradient masih terlihat).

- [ ] **Step 6: Final visual audit (per slide)**

Buka tiap slide 1-23 satu per satu. Checklist:
- [ ] s1 Cover: tanggal "25 Mei 2026" tampil
- [ ] s2 Agenda: 6 item, klik works
- [ ] s3 Latar Belakang: split sebelum/sesudah
- [ ] s4 3 prinsip: Terpusat/Terstandar/Terukur card
- [ ] s5 3 platform: CMP+CDP normal, BP faded + "🚧 COMING SOON"
- [ ] s6 Stairs: L1-L6 + chip 10 role
- [ ] s7 BAGIAN 1: 2 jenis card (Umum/Proton)
- [ ] s8 5 kategori: OJT (bukan OJ), no Proton, info bar
- [ ] s9 Pre/Post: 4 step + 2 metric
- [ ] s10 Alur Assessment: 3 swimlane 7 step
- [ ] s11 Proton 3 tahun: Th 3 badge offline
- [ ] s12 Alur Proton Th 1&2: 4 step + cross-ref + warning
- [ ] s13 Alur Proton Th 3: badge OFFLINE, callout
- [ ] s14 Coaching dual track: 2 kolom Panel/Op + 3 pill
- [ ] s15 IDP & Training: 2 card 4 bullet
- [ ] s16 Hierarki: 2 tree (generic + contoh)
- [ ] s17 Progresi: tabel 5 aspek + Th 3 highlight
- [ ] s18 Alur Coaching Th 1&2: 3 swimlane 8 step
- [ ] s19 Alur Coaching Th 3: badge LEVEL MAHIR + 3 highlight step
- [ ] s20 Ringkasan: 3 card gradient + output amber
- [ ] s21 Integrasi: 6 fitur (10 role label)
- [ ] s22 Cara akses: Dev kiri aktif + Prod kanan perkiraan
- [ ] s23 Penutup: kontak PT KPB, CTA arah Dev URL

- [ ] **Step 7: Commit final cleanup**

```bash
git add docs/Sosialisasi-Aplikasi-PortalHC-KPB.html
git commit -m "chore(sosialisasi): final cleanup + visual audit pass 23/23 slides

All 23 slides verified in browser. No console errors. Keyboard nav,
agenda click nav, dark mode toggle all functional. Data PDF
autoritative, style HTML preserved.

Refs spec docs/superpowers/specs/2026-05-25-sosialisasi-portalhc-v3-merge-design.md"
```

- [ ] **Step 8: Tag release (opsional)**

Jika user setuju:

```bash
git tag -a sosialisasi-v3.0 -m "Sosialisasi PortalHC v3 — Merge HTML style + PDF data

23 slides aligned with PDF Slide Deck 2026. Data autoritative from
PDF, design treatment from HTML. Drops 6 HTML fiktif slides
(3 Pilar, mockups, journey card, Dashboard KPI, Progress).
Cover tanggal 25 Mei 2026. Dev URL aktif, Production URL perkiraan."
```

---

## Acceptance Criteria

1. ✅ Total 23 slide aktif (`const total = 23`).
2. ✅ Navigation keyboard (← →) + tombol Prev/Next + agenda click semua berfungsi.
3. ✅ 18 page PDF terpetakan ke slide 1-23 (no missing data, verified per acceptance list di task 26).
4. ✅ Tidak ada data fiktif (no `kpi-card` 156/87%/+23%/42, no `mockup-row` dummy assessment data).
5. ✅ URL Production di slide 22 ditandai "perkiraan" + badge "BELUM AKTIF".
6. ✅ URL Development di slide 22 ditandai aktif + badge "AKTIF SEKARANG".
7. ✅ CTA penutup arah `http://10.55.3.3/KPB-PortalHC` (Dev), bukan Production.
8. ✅ Tanggal "25 Mei 2026" konsisten di slide 1 (cover) + slide 23 (kontak).
9. ✅ Label "OJT" (On the Job Training) di slide 8, bukan "OJ".
10. ✅ BP "Coming Soon" tampil di slide 5 dengan style faded + badge.
11. ✅ 10 role granular tampil di slide 6 stairs (L1-L6 prefix, chip per role).
12. ✅ Style HTML existing dipertahankan (gradient card, dark mode, hover, transisi).
13. ✅ File output: `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html` (single file, in-place edit).
14. ✅ Tidak ada error JavaScript di console saat load + saat navigasi.
15. ✅ Dark mode toggle berfungsi di semua 23 slide.

---

## Catatan untuk Engineer

- **Browser:** Test di Chrome/Edge desktop minimum. Tidak perlu mobile (sosialisasi pakai proyektor desktop).
- **File path:** Selalu pakai `docs/Sosialisasi-Aplikasi-PortalHC-KPB.html`. Tidak ada file di root.
- **CSS specificity:** Semua class baru di Task 2 di-append setelah `</style>` block existing. Tidak menimpa class lama (kecuali `module-card` yang ditambah `.faded` variant).
- **Animation/transition:** Tidak menambah animation baru. Cuma pakai animation existing dari HTML lama.
- **No Chart.js:** Setelah Task 1, tidak ada lagi dependency CDN external.
- **HTML escape:** Pakai `&mdash;` `&amp;` `&minus;` untuk dash/ampersand/minus sesuai HTML existing.
- **Markdown table di spec:** Mapping yang ditulis di spec bukan markup HTML — translate ke `<table>` atau `<div class="...">` sesuai class system.
- **Test data per slide:** Selalu open file di browser, navigate ke slide yang baru diubah, verify visual sebelum commit. Tidak ada automated test.
- **Rollback safety:** Setiap task commit terpisah. Jika ada step yang break, `git revert HEAD` aman.
