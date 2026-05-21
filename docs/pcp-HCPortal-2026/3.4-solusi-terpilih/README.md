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
