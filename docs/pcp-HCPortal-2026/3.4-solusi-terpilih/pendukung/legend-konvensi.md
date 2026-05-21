# Legend & Konvensi — §3.4 HC Portal

> Dipakai konsisten di seluruh diagram & file §3.4.

## Aktor

| Kode | Nama | Definisi |
|------|------|----------|
| `MANAJEMEN` | Manajemen | Direktur / VP / VP Mgr — strategic view |
| `HC` | Human Capital | Fungsi pengelola kompetensi & pengembangan |
| `ATASAN` | Atasan | Sr Supervisor / Section Head / Manager |
| `COACH` | Coach | Pekerja senior pendamping coachee |
| `COACHEE` | Coachee | Pekerja yang mengikuti program pengembangan |
| `USER` | User / Pekerja | Pekerja umum CSU Process |
| `SISTEM` | Sistem HC Portal | Web app (otomatis, tanpa intervensi manual) |

## Layer Aktor (untuk Gambar Teknik Versi A)

| Level | Aktor | Posisi Strategis |
|-------|-------|-------------------|
| **L5** | Manajemen | Strategic — laporan kompetensi & insight |
| **L4** | HC | Governance — kelola master data |
| **L3** | Atasan | Supervisory — review & approval |
| **L2** | Coach | Coaching — pendampingan pengembangan |
| **L1** | Pekerja (USER/COACHEE) | Operational — eksekusi assessment & coaching |

## Tools Sebelum (Manual Workflow)

| Tools | Tipe | Fungsi |
|-------|------|--------|
| Excel Master | Spreadsheet share folder | Data pekerja, KKJ, training, anggaran |
| FleQi Quiz | Aplikasi web eksternal | Ujian assessment online |
| Form PROTON cetak | Paperwork | Form coaching 5 fase |
| Word / PDF | Dokumen statis | Sertifikat, laporan |
| Email Pertamina | Channel komunikasi | Distribusi dokumen, approval |
| WhatsApp | Channel komunikasi | Koordinasi cepat, approval lisan |
| Arsip fisik | Map / lemari | Bukti coaching, evidence hardcopy |

## Notasi Mermaid (Flow Proses)

Tipe diagram: `flowchart LR` (left-right).

Konvensi node:
- `[Aktor: aksi]` rectangle = aksi manual oleh aktor
- `(Tools eksternal)` rounded = tools non-portal (Excel, FleQi, dll.)
- `{{Sistem: aksi}}` hexagon = aksi otomatis HC Portal
- `[/Decision/]` parallelogram = percabangan

## Konvensi Warna (Redraw PowerPoint)

| Element | Warna | Hex |
|---------|-------|-----|
| Manual / tools eksternal (pain) | Merah muda / abu-abu | #fce8eb / #e8e8e8 |
| Portal / digital (gain) | Biru Pertamina | #00558C |
| Hijau improvement (gain) | Hijau Pertamina | #00A551 |
| Kuning warning / decision | Kuning Pertamina | #FFC72C |
| Merah pain point marker | Merah Pertamina | #C8102E |
| Hub HC Portal (gradient) | Biru→Hijau | linear-gradient #00558C → #00A551 |

## Marker

- **Issue marker:** lingkaran merah, huruf A-F, lokasi pain point di diagram Sebelum
- **Improvement marker:** lingkaran hijau, angka 1-N, lokasi intervensi di diagram Sesudah

## Mapping Fitur ke File Flow Proses

| Fitur | File |
|-------|------|
| Assessment Online | `flow-proses/01-assessment.md` |
| PROTON Coaching | `flow-proses/02-proton-coaching.md` |
| IDP / Plan | `flow-proses/03-idp-plan.md` |
| KKJ & Matriks | `flow-proses/04-kkj-matriks.md` |
| Sertifikat & Renewal | `flow-proses/05-sertifikat-renewal.md` |
| Reporting / Analytics | `flow-proses/06-reporting-analytics.md` |
| Data Pekerja | `flow-proses/07-data-pekerja.md` |
