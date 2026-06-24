# Phase 417: Section Pagination - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 417-section-pagination
**Areas discussed:** Tampilan Header Section, Navigator/Palette Soal, Indikator Halaman+Section, Resume saat halaman geser

---

## Tampilan Header Section

| Option | Description | Selected |
|--------|-------------|----------|
| Nama+Nomor, ulang '(lanjutan)' | Header 'Section 2: Nama'; diulang '(lanjutan)' di halaman sambungan | |
| Nama saja, ulang | Header cuma nama Section; diulang di halaman sambungan | ✓ |
| Nama+Nomor, TIDAK diulang | Header hanya di halaman pertama Section | |
| Nama+Nomor+jumlah soal | Header tampilkan 'X soal', diulang | |

**User's choice:** Nama saja, header diulang dgn '(lanjutan)' saat auto-pecah.
**Notes:** Konsistensi → indikator halaman juga pakai nama Section saja (tanpa nomor).

---

## Navigator/Palette Soal

| Option | Description | Selected |
|--------|-------------|----------|
| Dikelompokkan per-Section + label | Grid nomor dipecah per-Section dgn label di atas tiap grup | ✓ |
| Flat 1..N | Satu grid datar tanpa pengelompokan (perilaku sekarang) | |
| Flat + pemisah warna | Satu grid, garis/warna pemisah antar-Section tanpa label | |

**User's choice:** Dikelompokkan per-Section + label.
**Notes:** Assessment tanpa Section tetap flat 1..N (backward-compat).

---

## Indikator Halaman+Section

| Option | Description | Selected |
|--------|-------------|----------|
| Section aktif + halaman | '<Nama Section> — Halaman 2/5' | ✓ |
| Halaman saja | 'Halaman 2/5' saja | |
| Halaman + total soal | 'Halaman 2/5 · soal 11–20 dari 50' | |

**User's choice:** Section aktif + halaman (pakai nama Section saja, selaras header).

---

## Resume saat halaman geser

| Option | Description | Selected |
|--------|-------------|----------|
| Toast informatif | Toast 'Lanjut dari soal no. X' saat resume ke halaman terhitung | ✓ |
| Diam | Langsung ke halaman terhitung tanpa notifikasi | |
| Toast hanya bila bergeser | Toast hanya bila halaman terhitung ≠ LastActivePage tersimpan | |

**User's choice:** Toast informatif (reuse pola showResumeFailureToast).

## Claude's Discretion

- Bentuk perhitungan PageNumber per-soal (controller vs view).
- Wording/penempatan tombol "Semua section mulai halaman baru" di UI Kelola Section.
- Mobile 5-soal/halaman ikut aturan Section.
- Styling header/navigator/toast.

## Deferred Ideas

- Header dgn progress/jumlah soal per-Section — ditolak (header nama saja).
- PAG-04 export label Section → Fase 419.
