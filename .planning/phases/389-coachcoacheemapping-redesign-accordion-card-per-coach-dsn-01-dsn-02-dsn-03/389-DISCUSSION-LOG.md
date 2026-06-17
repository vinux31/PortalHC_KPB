# Phase 389: CoachCoacheeMapping Redesign — Accordion Card per Coach - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-17
**Phase:** 389-coachcoacheemapping-redesign-accordion-card-per-coach-dsn-01-dsn-02-dsn-03
**Areas discussed:** Default state + tipe accordion, Layout tabel mini, Warna avatar inisial, Gaya toolbar seragam

---

## Gray area selection

| Area | Selected for discussion |
|------|------|
| Default buka/tutup + tipe accordion | ✓ |
| Layout tabel mini di dalam card | ✓ |
| Warna avatar inisial | ✓ |
| Gaya toolbar seragam (DSN-03) | ✓ |

User memilih SEMUA 4 area.

---

## Default state + tipe accordion card

| Option | Description | Selected |
|--------|-------------|----------|
| Tertutup semua, card independen | Default semua card tertutup; banyak boleh terbuka. Rapi untuk scan. | ✓ |
| Terbuka semua, card independen | Parity penuh dgn `collapse show` sekarang; bisa panjang. | |
| Accordion 1-terbuka (data-bs-parent) | Hanya 1 card terbuka; menyulitkan banding antar-coach. | |

**User's choice:** Tertutup semua, card independen
**Notes:** Card independen (bukan single-open) agar admin bisa banding beberapa coach. Default closed = perubahan disengaja dari `collapse show`; badge beban tetap tampak di header tertutup. → D-05.

---

## Layout tabel coachee mini di dalam card (mobile)

| Option | Description | Selected |
|--------|-------------|----------|
| Tabel penuh + table-responsive | 9 kolom utuh, scroll-x di mobile. Parity, risiko terendah. Kolom "Coachee Aktif" lama dilepas. | ✓ |
| Tabel penuh + kolom ringkas di mobile | Sembunyikan kolom non-esensial <768px; tambah CSS responsive. | |

**User's choice:** Tabel penuh + table-responsive
**Notes:** Kolom "Coachee Aktif" lama dilepas (pindah ke badge header). → D-06/D-07.

---

## Warna avatar inisial coach

| Option | Description | Selected |
|--------|-------------|----------|
| bg-primary netral | Reuse idiom ManageWorkers; badge sudah bawa warna beban → hindari double-encode. | ✓ |
| Tint ikut beban | Avatar warna ikut threshold beban; redundan dgn badge. | |
| Warna ikut section (hash) | Warna di-hash dari section; dekoratif tanpa makna beban. | |

**User's choice:** bg-primary netral
**Notes:** Reuse `ManageWorkers.cshtml:251` verbatim. → D-03.

---

## Gaya toolbar header seragam (DSN-03)

| Option | Description | Selected |
|--------|-------------|----------|
| Normalisasi + grup Excel, Tambah Mapping primary | Download/Import/Export dikelompok; "Tambah Mapping" CTA btn-primary solo. Hapus dead onclick. | ✓ |
| Excel jadi 1 dropdown + Tambah Mapping primary | Excel masuk 1 dropdown; paling ringkas; ubah trigger Import. | |
| Normalisasi ukuran saja | Minimal: samakan btn-sm + jarak rapi; tanpa grup/dropdown. | |

**User's choice:** Normalisasi + grup Excel, Tambah Mapping primary
**Notes:** Dead onclick L58 dihapus, fungsi modal tetap via data-bs-toggle. → D-10/D-11.

## Claude's Discretion

- Markup persis btn-group Excel + spacing antar-card.
- Ikon/animasi chevron buka-tutup.
- a11y header toggle (role/aria-expanded/aria-controls) — assert Playwright runtime.
- Blok `<style>` scoped minimal bila perlu.

## Deferred Ideas

None — diskusi tetap dalam scope phase.
