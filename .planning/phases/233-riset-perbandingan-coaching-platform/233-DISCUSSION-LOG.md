# Phase 233: Riset & Perbandingan Coaching Platform - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 233-riset-perbandingan-coaching-platform
**Areas discussed:** Scope Platform, Struktur Perbandingan, Format & Jumlah Dokumen, Kedalaman Rekomendasi

---

## Scope Platform

| Option | Description | Selected |
|--------|-------------|----------|
| 3 platform itu | 360Learning, BetterUp, CoachHub — sudah di-requirements | ✓ |
| Ganti sebagian | Ada platform lain yang lebih relevan untuk konteks manufacturing/industrial HR? | |
| Tambah 1-2 lagi | Tetap 3 itu + tambah platform lain (misal Lattice, 15Five, Torch) | |

**User's choice:** 3 platform sesuai requirements
**Notes:** User juga memutuskan Claude tidak boleh tambah platform lain meskipun ada gap coverage. Fokus hanya 3 platform. Semua 4 area Proton diriset dengan kedalaman sama.

---

## Struktur Perbandingan

| Option | Description | Selected |
|--------|-------------|----------|
| Per Area Proton | Setup, Execution, Monitoring, Completion — paralel dengan Phase 234-237 | ✓ |
| Per Platform | 360Learning, BetterUp, CoachHub — utuh per platform | |
| Hybrid Matrix | Tabel overview + deep-dive per area | |

**User's choice:** Per Area Proton
**Notes:** User meminta analisa ulang sebelum memilih. Setelah dijelaskan kekuatan/kelemahan tiap pendekatan, user memilih Per Area Proton karena paralel dengan phase audit 234-237. Juga memutuskan sertakan deskripsi as-is portal KPB sebagai baseline perbandingan.

---

## Format & Jumlah Dokumen

| Option | Description | Selected |
|--------|-------------|----------|
| 1 dokumen lengkap | Satu HTML dengan semua 4 area + ringkasan + rekomendasi | ✓ |
| 4+1 dokumen seperti Phase 228 | 4 dokumen per area + 1 ringkasan | |
| 2 dokumen | 1 perbandingan + 1 rekomendasi terpisah | |

**User's choice:** 1 dokumen lengkap
**Notes:** Format HTML di docs/, konsisten dengan dokumen project lainnya.

---

## Kedalaman Rekomendasi

| Option | Description | Selected |
|--------|-------------|----------|
| 3-tier + phase mapping | Must-fix / Should-improve / Nice-to-have, di-map ke Phase 234-237 | ✓ |
| 3-tier + actionable detail | Seperti di atas + deskripsi HOW per rekomendasi | |
| High-level saja | List gap dan prioritas tanpa detail implementasi | |

**User's choice:** 3-tier + phase mapping (tanpa detail implementasi)
**Notes:** Rekomendasi juga mencakup validasi fitur differentiator (DIFF-01/02/03).

---

## Claude's Discretion

- Styling dan layout HTML dokumen riset
- Kedalaman narasi per aspek berdasarkan relevansi
- Cara mendeskripsikan flow platform luar

## Deferred Ideas

None — discussion stayed within phase scope
