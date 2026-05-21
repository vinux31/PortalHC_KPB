# PCP SMART 2026 §3.4 — Solusi Terpilih HC Portal

> **Audience:** Reviewer PCP, manajemen HC, tim implementasi.
> **Domain:** HC Portal (PortalHC_KPB) — web app pengelolaan kompetensi CSU Process KPB.

## Executive Summary

HC Portal menggantikan workflow manual berbasis Excel + FleQi + paperwork + email/WhatsApp dengan single web portal terintegrasi. Hasilnya: pengurangan jumlah tools, jumlah step proses, dan waktu rekap; ditambah audit trail, single source of truth, dan governance compliance.

## Cakupan §3.4

Dokumen ini berisi visualisasi solusi terpilih HC Portal dalam dua bentuk yang valid di PCP §3.4:

1. **Gambar Teknik** — arsitektur sistem 3-tier HC Portal (`00-arsitektur-sistem.md`)
2. **Flow Proses** — process flow before/after untuk 7 fitur impactful (`01..07`)

## Index Dokumen

| File | Topik | Kategori |
|------|-------|----------|
| `00-arsitektur-sistem.md` | Arsitektur Sistem HC Portal | Gambar Teknik |
| `01-flow-assessment.md` | Process Flow Assessment Online | Flow Proses |
| `02-flow-proton-coaching.md` | Process Flow PROTON Coaching | Flow Proses |
| `03-flow-idp-plan.md` | Process Flow IDP / Plan | Flow Proses |
| `04-flow-kkj-matriks.md` | Process Flow KKJ & Matriks Kompetensi | Flow Proses |
| `05-flow-sertifikat-renewal.md` | Process Flow Sertifikat & Renewal | Flow Proses |
| `06-flow-reporting-analytics.md` | Process Flow Reporting / Analytics | Flow Proses |
| `07-flow-data-pekerja.md` | Process Flow Pengelolaan Data Pekerja | Flow Proses |
| `08-tabel-improvement.md` | Tabel Improvement Kuantitatif | Ringkasan |
| `09-tabel-issue-resolved.md` | Tabel Issue A-F & Mapping | Ringkasan |
| `10-legend-aktor.md` | Legend Swimlane & Aktor | Konvensi |

## Format Dokumen

- **Source:** Markdown + Mermaid (versionable di git)
- **Final delivery:** Manual redraw ke PowerPoint / Draw.io oleh tim untuk slide PCP

## Catatan Data Kuantitatif

Angka kuantitatif (jumlah step, waktu hemat, %) menggunakan **estimasi internal** berdasarkan inventory workflow manual sebelum HC Portal dan observasi proses HC. Akan di-refine dengan data riil pasca-implementasi.

## Referensi

- TKI: `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`
- Spec design: `docs/superpowers/specs/2026-05-21-pcp-hcportal-3.4-design.md`
- Implementation plan: `docs/superpowers/plans/2026-05-21-pcp-hcportal-3.4-implementation.md`
