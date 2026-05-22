# PCP SMART 2026 §3.4 — Solusi Terpilih HC Portal (v3.7)

> **Audience:** Reviewer PCP, manajemen HC, executive review, Process Improvement, Lean Six Sigma.
> **Domain:** HC Portal (PortalHC_KPB) — web app pengelolaan kompetensi CSU Process KPB.

## Executive Summary

HC Portal menggantikan workflow manual berbasis Excel + FleQi + paperwork + email/WhatsApp dengan single web portal terintegrasi. Improvement median lintas 7 fitur: **-67% step**, **-75% tools**, **~95% waktu** + audit trail, SSoT, governance compliance.

## Cakupan §3.4 v3.7

Dokumen ini berisi **4 diagram landscape** untuk reviewer pilih sesuai audience:

### Versi P — Workflow Topology (PRIMARY) — POLISHED v3.7

5 layer Purdue-style adaptasi (Manajemen → HC → Atasan → Coach → Pekerja) dengan HC Portal sebagai "Buffer Zone".
**Cocok untuk:** Submission PCP utama, reviewer engineering/operations.
**File:** `versi-p-workflow-topology.html`

### Versi C — Comparison Dashboard (SECONDARY) — POLISHED v3.7

Card-grid layout 7 fitur dengan metric Sebelum/Sesudah/Δ%, HC Portal hub showcase.
**Cocok untuk:** Executive review, management showcase.
**File:** `versi-c-comparison-dashboard.html`

### Versi X — BPMN Swimlane As-Is/To-Be (TERTIARY) — POLISHED v3.7

BPMN 2.0 standard (ISO 19510), 4 lane aktor + 1 system lane (HC Portal central hub) di Sesudah. Hero workflow: Assessment (6→2 step). Visual crisscross merah Sebelum vs lurus biru Sesudah.
**Cocok untuk:** Process Improvement / BPM / SOP Review.
**File:** `versi-x-bpmn-swimlane.html`

### Versi Z — BPMN+VSM Hybrid Lean Six Sigma (QUATERNARY) — v3.6 (unpolished, kept as-is)

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

## Changelog

### v3.7-slide8 — Versi P Compact untuk Risalah Web slide 8 (2026-05-22)

- New: `slide8/versi-p-compact.html` (972×340 landscape, side-by-side Sebelum+Sesudah, aspect match box pptx 8.05×25.72 cm)
- Drop chrome (header/toolbar/komparasi/legend table) untuk PNG export ke slide 8 placeholder #1 GAMBAR DESAIN
- Buffer zone Sesudah ↔ no-buffer slot Sebelum symmetric row height
- L4 HC labels shortened (drop "Excel"/"Kelola" prefix) supaya single-row fit
- Tech stack subtitle removed dari buffer zone (akan ke placeholder #3 Standard Design spec)
- Export PNG via html2canvas @2x retina (button corner top-right)
- Master `versi-p-workflow-topology.html` v3.7 **untouched**
- PNG inserted ke `docs/pcp-HCPortal-2026/Risalah Web.pptx` slide 8
- Spec: `docs/superpowers/specs/2026-05-22-pcp-slide8-versi-p-compact-design.md`
- Plan: `docs/superpowers/plans/2026-05-22-pcp-slide8-versi-p-compact-implementation.md`

### v3.7 — Polish Pass (2026-05-22)

- **Versi P, C, X** polished full overhaul:
  - Token typography scale standardize (`--fs-xs` 0.75rem floor, `--fs-sm`, `--fs-base`, `--fs-md`, `--fs-lg`, `--fs-xl`)
  - Floor font 0.75rem (eliminate `.65rem`, `.68rem`, `.7rem`)
  - Header-bar border-top sync ke index card audience colors (P=green, C=yellow, X=blue)
  - Responsive breakpoints @media max-width 1200px + 900px
  - Print A3 landscape + page-break-inside avoid + print-color-adjust exact
  - Versi X: drop bootstrap-icons CDN, replace dengan emoji native; lane-label 90→110px; task max-width 200→220px
  - Versi C: coverage matrix legend (✓ covered / — not applicable)
- **Versi Z** keep as-is (v3.6 baseline, polish ditunda)
- Index.html + tag references v3.6 → v3.7

### v3.6 — Final 4 Versi (2026-05-22 pre-polish)

4 file P/C/X/Z final + index master viewer.

## Recovery Versi Lama

- Tag `pcp-hcportal-3.4-v1.0` (swimlane-only) — `git checkout`
- Tag `pcp-hcportal-3.4-v2.0` (hybrid Layered + C4 + 7 swimlane) — `git checkout`
- Tag `pcp-hcportal-3.4-v3.0` s/d `v3.5` (Versi P+C iterasi) — `git checkout`
- Tag `pcp-hcportal-3.4-v3.6` (4 versi pre-polish baseline) — `git checkout`

## Referensi

- Slide PCP reference (page 8): `pendukung/reference-pcp-page8.png` + `.txt`
- Spec design v3 (versi P+C): `docs/superpowers/specs/2026-05-21-pcp-hcportal-3.4-v3-design.md`
- Spec design v3.6 (versi X+Z): `docs/superpowers/specs/2026-05-22-pcp-hcportal-3.4-versi-x-z-design.md`
- Plan v3.6 (versi X+Z): `docs/superpowers/plans/2026-05-22-pcp-hcportal-3.4-versi-x-z-implementation.md`
- TKI: `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`
