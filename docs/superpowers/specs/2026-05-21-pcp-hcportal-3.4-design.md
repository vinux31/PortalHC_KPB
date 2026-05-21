# Design Spec — PCP SMART 2026 §3.4 (HC Portal)

**Tanggal:** 2026-05-21
**Topik:** Dokumen "Design/Gambar Teknik/Flow Proses/Formula Solusi Terpilih" untuk submission PCP SMART 2026
**Domain:** HC Portal (PortalHC_KPB) — web app pengelolaan kompetensi pekerja CSU Process KPB
**Status:** Draft — pending user review

---

## 1. Konteks

PCP SMART 2026 (template Pertamina Patra Niaga) mewajibkan §3.4 berupa visualisasi solusi terpilih: design, gambar teknik, flow proses, atau formula. Slide contoh yang dipakai panitia berisi project OT/ICS cybersecurity dengan diagram Purdue Model 5 level + DMZ.

Project kita (HC Portal) **bukan infrastruktur jaringan** — melainkan web application untuk manajemen kompetensi (Competency Management, Career Development, PROTON Coaching, dll.). Memaksa pendekatan Purdue ke domain web app menghasilkan mismatched semantik.

**Keputusan:** Pivot dari Purdue Topology → kombinasi **Arsitektur Sistem (3-tier)** + **Process Flow Before/After** per fitur impactful. Pendekatan ini fit dengan opsi visualisasi §3.4 yang valid: "Flow Proses" (process flow) dan "Gambar Teknik" (arsitektur sistem).

## 2. Tujuan Dokumen

1. Menyajikan §3.4 dengan format yang **sejajar dengan slide contoh PCP** secara struktur (before-after, tabel improvement, issue resolved) tapi **kontekstual domain HC Portal**.
2. Membuktikan **improvement kuantitatif** (jumlah step, tools, waktu) dan **kualitatif** (audit trail, governance, single source of truth) dari migrasi workflow manual → web portal.
3. Menyediakan **source of truth versionable** (Mermaid) yang nantinya di-redraw manual ke PowerPoint/Draw.io untuk slide PCP final.

## 3. Cakupan & Fokus Fitur

Cakupan: **7 fitur impactful** yang paling representatif menunjukkan transformasi dari workflow manual (Excel/FleQi/paper/email/WA) ke HC Portal.

| # | Fitur | Pain Sebelum | Value Sesudah |
|---|-------|--------------|---------------|
| 1 | Assessment Online | FleQi eksternal + manual rekap Excel + grading manual | Online di portal, auto-grade non-essay, real-time monitor, hasil tersimpan, dashboard |
| 2 | PROTON Coaching | Form paper 5 fase + map fisik + bukti via email/WA | Form digital 5 fase, upload evidence, auto-link deliverable, histori timeline |
| 3 | IDP / Plan | Excel template, distribusi email, no progress tracking | Upload silabus Excel sekali → tampil otomatis ke Plan IDP coachee |
| 4 | KKJ / Matriks Kompetensi | Excel share folder, no versioning | Upload terpusat + history versi + matriks digital |
| 5 | Sertifikat & Renewal | Word/PDF manual, expired sering kelewat | Auto-generate sertifikat + expiry badge + menu Renewal Certificate |
| 6 | Reporting / Analytics | Excel pivot ad-hoc per request HC | Analytics Dashboard real-time, filter periode/bagian, export |
| 7 | Data Pekerja | Excel master scattered, manual update | DB terpusat, import Excel + form, role-based, audit log |

**Out of scope:** Notifikasi, Audit Log, Maintenance Mode, Impersonation (sistem/operasional, bukan core PCP improvement narrative).

## 4. Pendekatan & Format

### 4.1 Format Diagram — Dual Pipeline

| Fase | Format | Tujuan |
|------|--------|--------|
| **Source / Draft** | Mermaid (di file `.md`) | Versionable di git, AI-assisted generation, iterasi cepat |
| **Final / Slide PCP** | PowerPoint / Draw.io (manual redraw) | Presentation-ready, sejajar visual slide PCP contoh |

**Workflow:** Mermaid draft → review konten → approve → manual redraw ke PPT/Draw.io → embed ke slide PCP.

### 4.2 Metrics — Hybrid

| Tipe | Contoh | Kapan Dipakai |
|------|--------|---------------|
| Kuantitatif estimasi | "-50% step", "-95% waktu rekap", "-67% tools" | Headline impact statement |
| Kualitatif justifikasi | "audit trail terbentuk", "single source of truth", "compliance governance" | Untuk aspek yang sulit dikuantifikasi |

Sumber data: hitung dari diagram swimlane (step count), wawancara HC (waktu estimasi), inventory tools yang dipakai sebelum HC Portal.

### 4.3 Tone

- **Narasi & headline:** Eksekutif (untuk reviewer PCP, manajemen)
- **Data, tabel, diagram label:** Teknis (konsisten dengan terminologi sistem)

## 5. Struktur Output

```
docs/pcp-HCPortal-2026/3.4-solusi-terpilih/
├─ README.md                       ← Index + executive summary
├─ 00-arsitektur-sistem.md         ← Gambar Teknik 3-tier HC Portal
├─ 01-flow-assessment.md           ← Process Flow Assessment Online
├─ 02-flow-proton-coaching.md      ← Process Flow PROTON 5 fase
├─ 03-flow-idp-plan.md             ← Process Flow IDP
├─ 04-flow-kkj-matriks.md          ← Process Flow KKJ
├─ 05-flow-sertifikat-renewal.md   ← Process Flow Sertifikat
├─ 06-flow-reporting-analytics.md  ← Process Flow Reporting
├─ 07-flow-data-pekerja.md         ← Process Flow Data Pekerja
├─ 08-tabel-improvement.md         ← Tabel Improvement kuantitatif
├─ 09-tabel-issue-resolved.md      ← Tabel Issue A-F pain points
└─ 10-legend-aktor.md              ← Legend swimlane + aktor
```

**Total:** 12 file Markdown + Mermaid embedded.

## 6. Template Per File

### 6.1 File `00-arsitektur-sistem.md`

```markdown
# Arsitektur Sistem HC Portal

## Konteks (eksekutif)
[1 paragraf: stack teknologi + tujuan arsitektur]

## Diagram Arsitektur 3-Tier (Mermaid)
[Diagram: Browser → ASP.NET Core → SQL Server + side modules]

## Komponen Utama (Teknis)
| Layer | Komponen | Fungsi |
| Presentation | Razor Views + Bootstrap | UI |
| Application | ASP.NET Core 8 + SignalR + Hangfire + Identity | Logic, real-time, jobs, auth |
| Data | EF Core + SQL Server | ORM + DB |
| Storage | File system / Storage path | Evidence, sertifikat, KKJ |

## Justifikasi Pemilihan Stack
[Why ASP.NET Core, Why SQL Server, Why SignalR]
```

### 6.2 File `01..07-flow-{fitur}.md` (Konsisten)

```markdown
# Process Flow — [Nama Fitur]

## Konteks (eksekutif, 1 paragraf)
[Apa fitur, siapa aktor, kenapa penting]

## Flow SEBELUM — Workflow Manual

```mermaid
[swimlane: aktor manual + tools eksternal]
```

## Flow SESUDAH — HC Portal

```mermaid
[swimlane: aktor + portal step]
```

## Tabel Komparasi Step

| Aspek | Sebelum | Sesudah | Improvement |
| Jumlah step | X | Y | -Z% |
| Jumlah tools | N | 1 portal | -% |
| Waktu (estimasi) | A jam | B menit | -% |
| Aktor terlibat | M | M | tetap |
| Risiko data hilang | Tinggi | Rendah (audit log) | qualitative |

## Issue yang Diselesaikan
- [ref ke 09-tabel-issue-resolved.md: A, B, C, ...]

## Benefit
**Kuantitatif:**
- [...]

**Kualitatif:**
- [...]
```

### 6.3 File `08-tabel-improvement.md`

Tabel ringkasan 7 fitur, satu baris per fitur, kolom: step Δ, tools Δ, waktu Δ, key benefit.

### 6.4 File `09-tabel-issue-resolved.md`

Tabel issue A-F pain point manual workflow + mapping ke fitur yang menyelesaikan.

### 6.5 File `10-legend-aktor.md`

Legend:
- Aktor: USER / COACHEE / COACH / ATASAN / HC / SISTEM
- Notasi swimlane Mermaid
- Convention: tools eksternal (Excel/FleQi/email/WA), paperwork (form, sertifikat), portal step

## 7. Urutan Eksekusi (Wave-Based)

| Wave | File | Justifikasi |
|------|------|-------------|
| **1 — Foundation** | README + 00 Arsitektur + 10 Legend | Tetapkan kerangka, konvensi diagram, aktor |
| **2 — Flow Impactful** | 01 Assessment + 02 PROTON | Dua fitur paling kompleks, paling banyak improvement |
| **3 — Flow Tambahan** | 03 IDP + 04 KKJ + 05 Sertifikat + 06 Reporting + 07 Data Pekerja | Sisanya, pola sudah baku setelah Wave 2 |
| **4 — Ringkasan** | 08 Improvement + 09 Issue | Konsolidasi data dari Wave 2-3 |

## 8. Data yang Dibutuhkan (Pre-Execution)

Sebelum mulai tulis flow per fitur, butuh data ini:

1. **Inventory tools manual sebelum HC Portal** — list aplikasi (FleQi, Excel master, Word, email Pertamina, WhatsApp, dll.)
2. **Estimasi waktu manual per fitur** — wawancara HC: berapa jam HC rekap assessment manual? Berapa lama buat sertifikat per pekerja? Berapa lama prepare laporan?
3. **Volume operasi** — jumlah pekerja CSU Process, jumlah assessment per tahun, jumlah deliverable IDP per coachee, dll.
4. **Step count manual** — gambar workflow lama (boleh asumsi based on TKI outline + interview)

**Asumsi awal:** Data kuantitatif boleh estimasi (label "estimasi") di Wave 1-3, refine dengan data riil saat Wave 4.

## 9. Verifikasi & Acceptance Criteria

| Criteria | Pass |
|----------|------|
| 12 file ada di `docs/pcp-HCPortal-2026/3.4-solusi-terpilih/` | ☐ |
| Setiap file flow (01-07) punya Mermaid before + after + tabel komparasi | ☐ |
| File 08 menampilkan ringkasan 7 fitur dengan minimal 3 metrik per fitur | ☐ |
| File 09 punya minimal 6 issue (A-F) ter-mapping ke fitur | ☐ |
| README punya executive summary ≤ 1 halaman + index navigasi | ☐ |
| Mermaid diagram render benar (no syntax error) di GitHub/VS Code preview | ☐ |
| Konsisten tone: eksekutif narasi, teknis data | ☐ |
| Tidak ada placeholder TBD/TODO setelah Wave 4 selesai | ☐ |

## 10. Out of Scope (Eksplisit)

- Redraw final ke PowerPoint / Draw.io — manual oleh user
- Integrasi ke slide PCP master (.pptx) — manual oleh user
- Data riil kuantitatif (akan estimasi dulu, refine kemudian)
- Fitur Notifikasi, Audit Log, Maintenance, Impersonation (operasional, bukan core PCP)
- Translasi ke Bahasa Inggris (full Bahasa Indonesia per CLAUDE.md)

## 11. Risiko & Mitigasi

| Risiko | Dampak | Mitigasi |
|--------|--------|----------|
| Data kuantitatif estimasi terlalu optimis | Reviewer ragukan kredibilitas | Label "estimasi internal" eksplisit; siapkan justifikasi metode estimasi |
| Mermaid swimlane terlalu panjang (terlalu detail) | Diagram tidak terbaca | Limit max 10 step per swimlane; pecah ke sub-flow bila lebih |
| Redraw manual ke PPT memakan waktu | Slide PCP terlambat | Mermaid harus se-readable mungkin agar redraw cepat (clear label, no overlap) |
| Tone eksekutif tercampur dengan teknis | Reviewer bingung audience | Pisahkan section: "Konteks" eksekutif, "Tabel/Diagram" teknis |

## 12. Referensi

- **Slide template:** `C:\Users\Administrator\OneDrive - PT Pertamina (Persero)\Documents\PCP SMART 2026 APQ Rev 9999 Final_1.png`
- **Feature inventory:** `wwwroot/documents/TKI/Draft-BAB-X-INSTRUKSI-KERJA-outline.md`
- **Screenshot fitur:** `wwwroot/documents/TKI/screenshots/` (A01..E04, 28 file)
- **CLAUDE.md:** Bahasa Indonesia wajib; dev workflow Lokal → Dev → Prod

## 13. Next Step

Setelah spec ini di-approve oleh user → invoke `superpowers:writing-plans` skill untuk membuat implementation plan per wave (granular task per file).
