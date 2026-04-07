# Phase 301: Advanced Reporting - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-07
**Phase:** 301-advanced-reporting
**Areas discussed:** Item Analysis UI, Gain Score Report, Export Excel, Dashboard Panel

---

## Item Analysis UI

### Penempatan

| Option | Description | Selected |
|--------|-------------|----------|
| Tab baru di Analytics Dashboard | Reuse filter existing, konsisten, tidak perlu entry point baru | ✓ |
| Di halaman Results per assessment | Kontekstual per assessment tapi harus masuk satu-satu | |
| Halaman baru /CMP/ItemAnalysis | Space penuh, filter sendiri, butuh entry point baru | |

**User's choice:** Tab baru di Analytics Dashboard
**Notes:** User awalnya minta penjelasan fungsi phase, setelah dijelaskan memilih opsi ini dengan preview mockup.

### Warning < 30 Responden

| Option | Description | Selected |
|--------|-------------|----------|
| Inline badge kuning | Badge "Data belum cukup" di samping nilai, nilai di-gray-out | ✓ |
| Tooltip only | Hover warning, lebih bersih tapi bisa terlewat | |
| Sembunyikan nilai | Tampilkan "N/A" tanpa angka | |

**User's choice:** Inline badge kuning

### Distractor Analysis

| Option | Description | Selected |
|--------|-------------|----------|
| Tabel opsi per soal | Opsi A/B/C/D dengan jumlah, persentase, highlight benar | ✓ |
| Bar chart horizontal | Visualisasi bar per soal, lebih visual tapi banyak ruang | |
| Claude yang tentukan | Serahkan ke Claude | |

**User's choice:** Tabel opsi per soal

---

## Gain Score Report

### Penempatan

| Option | Description | Selected |
|--------|-------------|----------|
| Tab baru di Analytics Dashboard | Konsisten, semua reporting di satu tempat | ✓ |
| Di halaman Results per PrePostTest | Kontekstual per assessment | |
| Halaman baru /CMP/GainScoreReport | Halaman terpisah lintas assessment | |

**User's choice:** Tab baru di Analytics Dashboard

### Level Tampilan

| Option | Description | Selected |
|--------|-------------|----------|
| Per pekerja DAN per elemen kompetensi | Dua view sesuai RPT-04 | ✓ |
| Per pekerja saja | Hanya tabel per pekerja | |
| Claude yang tentukan | Serahkan ke Claude | |

**User's choice:** Per pekerja DAN per elemen kompetensi

---

## Export Excel

### Format File

| Option | Description | Selected |
|--------|-------------|----------|
| Satu file multi-sheet | Satu .xlsx dengan sheet terpisah | |
| File terpisah per report | Tombol export per tab menghasilkan file berbeda | ✓ |
| Claude yang tentukan | Serahkan ke Claude | |

**User's choice:** File terpisah per report

### Styling

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, styling profesional | Header berwarna, border, auto-fit, freeze row | ✓ |
| Plain data saja | Data mentah tanpa styling | |
| Claude yang tentukan | Serahkan ke Claude | |

**User's choice:** Ya, styling profesional

---

## Dashboard Panel

### Jenis Chart

| Option | Description | Selected |
|--------|-------------|----------|
| Line chart tren per bulan | Sumbu X bulan, Y avg gain score, konsisten dengan existing | ✓ |
| Bar chart per assessment | Satu bar per assessment PrePostTest | |
| Claude yang tentukan | Serahkan ke Claude | |

**User's choice:** Line chart tren per bulan

### Penempatan Panel

| Option | Description | Selected |
|--------|-------------|----------|
| Di tab Trend yang sudah ada | Tambah chart di bawah trend lulus/gagal | ✓ |
| Tab baru "Gain Score Trend" | Tab terpisah khusus tren gain score | |
| Claude yang tentukan | Serahkan ke Claude | |

**User's choice:** Di tab Trend yang sudah ada

---

## Claude's Discretion

- UI spacing, typography, loading states
- Color coding p-value interpretation
- Error handling untuk assessment tanpa PrePostTest data
- Group comparison visualization (RPT-07)

## Deferred Ideas

None — discussion stayed within phase scope
