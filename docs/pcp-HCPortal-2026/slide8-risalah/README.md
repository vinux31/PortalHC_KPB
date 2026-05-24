# PCP SMART 2026 — Slide 8 Risalah Web (v1.0)

> **Audience:** Reviewer PCP, HC management, OPEX, Process Improvement
> **Source PPT:** `docs/pcp-HCPortal-2026/Risalah Web.pptx` slide 8
> **Konteks:** §3.4 Solusi Terpilih — HC Portal (alternative dari `3.4-solusi-terpilih/versi-p-workflow-topology.html`)

## Executive Summary

Slide 8 Risalah Web.pptx saat ini menggunakan Versi P (Workflow Topology Purdue-style + DMZ-analog Buffer Zone). Audit terhadap 4 jurnal akademis + 3 laporan korelasi di `referensi slide 8 solusi terpilih/` menemukan gap: tidak ada Flow Proses eksplisit, tidak ada Formula kuantitatif, zero theory grounding. Folder ini menyediakan 2 versi alternatif yang menambal gap tersebut.

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
