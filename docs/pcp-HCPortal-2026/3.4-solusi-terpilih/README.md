# PCP SMART 2026 §3.4 — Solusi Terpilih HC Portal (v2)

> **Audience:** Reviewer PCP, manajemen HC, tim implementasi.
> **Domain:** HC Portal (PortalHC_KPB) — web app pengelolaan kompetensi CSU Process KPB.

## Executive Summary

HC Portal menggantikan workflow manual berbasis Excel + FleQi + paperwork + email/WhatsApp dengan single web portal terintegrasi. Hasilnya: pengurangan jumlah tools, jumlah step proses, dan waktu rekap; ditambah audit trail, single source of truth, dan governance compliance.

## Cakupan §3.4

Dokumen ini berisi visualisasi solusi terpilih HC Portal dalam **2 jenis valid** dari 4 pilihan §3.4 ("Design / Gambar Teknik / Flow Proses / Formula"):

1. **Gambar Teknik (utama)** — diagram landscape Sebelum vs Sesudah, dalam 2 versi style untuk reviewer memilih
2. **Flow Proses (lampiran)** — swimlane workflow per 7 fitur impactful

Skip:
- Design (mockup UI) — optional, tidak masuk submission
- Formula — N/A untuk web app HR

## Struktur Folder

```
3.4-solusi-terpilih/
├─ README.md                       (this file)
├─ index.html                      (Master consolidated HTML viewer)
│
├─ gambar-teknik/
│  ├─ versi-a-layered-aktor.html   ← Diagram landscape Layered (5 layer aktor)
│  ├─ versi-b-c4-context.html      ← Diagram landscape C4 (hub-and-spoke)
│  └─ tabel-komparasi.md           ← Tabel Aspek × Sebelum × Sesudah lintas 7 fitur
│
├─ flow-proses/
│  ├─ 01-assessment.md             ← Swimlane Assessment Sebelum + Sesudah
│  ├─ 02-proton-coaching.md
│  ├─ 03-idp-plan.md
│  ├─ 04-kkj-matriks.md
│  ├─ 05-sertifikat-renewal.md
│  ├─ 06-reporting-analytics.md
│  └─ 07-data-pekerja.md
│
├─ pendukung/
│  ├─ tabel-issue-resolved.md      ← Issue A-F + mapping fitur
│  └─ legend-konvensi.md           ← Legend aktor + notasi Mermaid + warna
│
└─ archive/
   └─ diagram-landscape-options.html  ← (discussion mockup, reference)
```

## 2 Versi Gambar Teknik

| Versi | Style | Standar | Match Slide PCP Referensi |
|-------|-------|---------|:--------------------------:|
| **A** | Layered Aktor (5 layer vertikal) | Industry de-facto Layered Architecture | ⭐⭐⭐⭐⭐ |
| **B** | C4 System Context (hub-and-spoke) | C4 Model (Simon Brown) | ⭐⭐⭐ |

**Rekomendasi:** Versi A untuk submission utama (paling match slide referensi). Versi B sebagai alternative kalau audience IT-savvy.

## 7 Fitur Impactful (Flow Proses)

| # | Fitur | Pain Point Sebelum | Value Sesudah |
|---|-------|---------------------|----------------|
| 01 | Assessment Online | FleQi + Excel + grading manual | Online + auto-grade + dashboard |
| 02 | PROTON Coaching | Form cetak + WA + arsip fisik | Form digital 5 fase + evidence link |
| 03 | IDP / Plan | Excel + email + tracking manual | Upload silabus → tampil otomatis |
| 04 | KKJ & Matriks | Share folder + no versioning | Upload terpusat + history + matriks digital |
| 05 | Sertifikat & Renewal | Word + Excel + reactive | Auto-generate + badge expiry + Renewal menu |
| 06 | Reporting / Analytics | Pivot Excel ad-hoc | Dashboard real-time + export |
| 07 | Data Pekerja | 4-5 Excel scattered | DB terpusat + import Excel + audit log |

## Format & Pipeline

- **Source:** Markdown + Mermaid (versionable di git)
- **Review:** HTML standalone (buka di browser)
- **Final submission:** Manual redraw ke PowerPoint untuk slide PCP
- **Lampiran formal:** PDF export

## Catatan Data Kuantitatif

Angka kuantitatif (step, waktu, %) menggunakan **estimasi internal** berdasarkan inventory workflow manual + observasi proses HC. Akan di-refine dengan data riil pasca-implementasi.

## Referensi

- Slide PCP template: `C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Documents\PCP SMART 2026 APQ Rev 9999 Final_1.png`
- TKI: `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`
- Spec design v2: `docs/superpowers/specs/2026-05-21-pcp-hcportal-3.4-v2-design.md`
- Plan v2: `docs/superpowers/plans/2026-05-21-pcp-hcportal-3.4-v2-implementation.md`
- Recovery v1.0: tag `pcp-hcportal-3.4-v1.0` (git checkout)
