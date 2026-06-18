# Phase 388: Label Hasil + CoachWorkload Polish - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-17
**Phase:** 388-label-hasil-coachworkload-polish-lbl-03-dsn-04-dsn-05
**Areas discussed:** CSS cleanup approach (DSN-05), Card framing (DSN-04), Saran Penyeimbangan nesting (DSN-04)

---

## DSN-05 — CSS cleanup approach

| Option | Description | Selected |
|--------|-------------|----------|
| Bootstrap util + scoped `<style>` minimal | Kelas Bootstrap sebisanya; sisanya 1 blok style kecil. Tanpa file CSS baru. | ✓ |
| Scoped `<style>` block saja | Semua custom size jadi kelas di 1 blok style di atas view. | |
| Shared CSS file | Buat/extend CSS bersama (preseden records.css). Lintas-halaman. | |

**User's choice:** Bootstrap util + scoped `<style>` minimal
**Notes:** Idiomatik, footprint kecil, sesuai scope 1 halaman.

---

## DSN-04 — Card framing

| Option | Description | Selected |
|--------|-------------|----------|
| Card + card-header (ikon+judul) | Konsisten penuh dgn card "Grafik Beban Coach"/"Detail Beban Coach". | ✓ |
| Card-body polos tanpa header | Bungkus card tanpa header bar. Lebih ringan. | |

**User's choice:** Card + card-header (ikon+judul)
**Notes:** Seragam dgn section lain di halaman yang sama.

---

## DSN-04 — Saran Penyeimbangan nesting (hindari card-in-card)

| Option | Description | Selected |
|--------|-------------|----------|
| Item jadi list-group dalam 1 card | Section = 1 card; tiap saran jadi baris list-group. No nesting. | ✓ |
| Section header-bar saja, item tetap card | Heading gaya card-header, item saran tetap card terpisah. | |
| Biarkan apa adanya | Cuma rapikan heading h5. | |

**User's choice:** Item jadi list-group dalam 1 card
**Notes:** Bersih, tanpa card-in-card. PARITY: wajib jaga hook JS `#sug-{id}` / `.suggestion-card` / `.approve-btn` / `.skip-btn` + data-* (lihat CONTEXT D-08).

## Claude's Discretion
- Pemilihan kelas Bootstrap font-size paling dekat visual.
- Penyelarasan spacing util Bootstrap.
- Chevron transition: pindah kelas atau biarkan.
- Pilihan ikon `bi-*` untuk card-header.

## Deferred Ideas
- None.

## Reviewed Todos (not folded)
- "One-time cleanup data test/audit lokal setelah Phase 367 ship" [database, 0.6] — tak relevan (DB cleanup, bukan UI).
