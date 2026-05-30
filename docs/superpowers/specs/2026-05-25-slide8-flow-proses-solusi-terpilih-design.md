# Slide 8 — Flow Proses Solusi Terpilih HC Portal v2.0

> **Date:** 2026-05-25
> **Context:** PCP SMART 2026 §3.4 — Risalah Web.pptx slide 8
> **Output:** `docs/pcp-HCPortal-2026/slide8-risalah/flow-proses-solusi-terpilih.html`

## Goal

Buat versi baru slide 8 §3.4 yang mengikuti format referensi PCP orang lain (2-panel: kiri = sebelum/target visual, kanan = flow proses implementasi kronologis). Fresh start — tidak extend Opsi II/IV existing.

## Format: 2-Panel Landscape

Identik struktur dengan referensi "Flow Proses Solusi Terpilih" dari PCP Indoor Startup Simulation:

| Panel | Konten |
|-------|--------|
| **Kiri (44%)** — kotak putih | 3 "bangunan" pilar: Ref → Sebelum → Target |
| **Kanan (56%)** — kotak hijau tua | 9-step flow implementasi, snake clockwise |

## Panel Kiri — 3 Bangunan Pilar

**Metafora:** Bangunan dengan atap segitiga (gaya Pedoman Cap Building referensi).

**Pilar (7 fitur HC Portal):** CMP · CDP · BP · Assessment · OJT · Laporan · Admin

| Kolom | Label | Status Pilar |
|-------|-------|-------------|
| 1 | Ref: Pedoman Cap Building | Semua netral (framework standar) |
| 2 | Sebelum Inovasi | CMP ✓ · CDP ✓ · BP ✓ · Assessment ❌ · OJT ❌ · Laporan ❌ · Admin ❌ |
| 3 | Target | Semua ✓ via HC Portal Terintegrasi |

**Atap warna:**
- Ref = abu-abu (`#6b7280`)
- Sebelum = merah (`#b91c1c`)
- Target = hijau (`#15803d`)

**Bottom note:** `[Lampiran: data monitoring kompetensi pekerja CSU Process KPB]`

## Panel Kanan — 9-Step Flow Implementasi

**Layout:** Snake clockwise 3×3 grid + panah putih antar sel.

| Step | Label | Foto/Screenshot |
|------|-------|-----------------|
| 1 | Idea | Placeholder |
| 2 | Development / Pembangunan Portal HC | Placeholder |
| 3 | Penyusunan & Pengisian Data Pekerja ke Web | Placeholder |
| 4 | Sosialisasi Team HC Internal | Placeholder |
| 5 | Trial Proton — Penetapan Coach & Coachee | Placeholder |
| 6 | Trial Assessment Proton | Placeholder |
| 7 | First Assessment (Pre-Post Test) | Placeholder |
| 8 | Penyusunan TKI | Placeholder |
| 9 | Kick Off Meeting Proton ★ | Placeholder (highlight kuning = endpoint) |

**Snake pattern:**
```
[1] → [2] → [3]
              ↓
[6] ← [5] ← [4]
↓
[7] → [8] → [9★]
```

## Visual Style

- Warna: Pertamina palette existing (`#C8102E` red · `#00558C` blue · `#00A551` green · `#FFC72C` yellow · `#6b7280` gray)
- Background slide: hijau muda (seperti referensi `#b7d8b0`)
- Judul bar: `#1a5c3a` hijau tua
- Print: `@page { size: A3 landscape; margin: 1cm }`
- Font floor: `0.75rem`

## File Output

```
docs/pcp-HCPortal-2026/slide8-risalah/flow-proses-solusi-terpilih.html
```

Tambahkan card ke `docs/pcp-HCPortal-2026/slide8-risalah/index.html` sebagai Opsi V.

## Constraints

- Foto/screenshot = placeholder — user isi sendiri
- Tidak replace/modify Opsi II & IV existing
- Standalone HTML, no external CDN dependency
