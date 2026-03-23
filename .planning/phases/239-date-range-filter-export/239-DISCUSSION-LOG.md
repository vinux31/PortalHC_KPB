# Phase 239: Date Range Filter & Export - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-23
**Phase:** 239-Date Range Filter & Export
**Areas discussed:** Posisi input date di filter bar, Filtering approach, Format & behavior input date

---

## Posisi input date di filter bar

| Option | Description | Selected |
|--------|-------------|----------|
| Ganti posisi Search di row 2 | Row 2 jadi: Status, Tgl Awal, Tgl Akhir, Reset. Layout tetap 2 baris. | ✓ |
| Pindah ke row 1, geser filter lain | Date range di awal row 1 karena ini filter utama. | |
| Tetap row 2 tapi full-width | Date range span lebih lebar menggantikan search + area kosong. | |

**User's choice:** Ganti posisi Search di row 2
**Notes:** Layout tetap 2 baris seperti sekarang, hanya Search Nama/NIP diganti 2 input date.

---

## Filtering approach

| Option | Description | Selected |
|--------|-------------|----------|
| Server-side via AJAX | Saat date range diisi, kirim request ke server. Akurat, performa baik. | ✓ |
| Client-side penuh | Embed semua data tanggal record di HTML sebagai data attributes. | |
| Hybrid | Date range ke server, filter lain tetap client-side. | |

**User's choice:** Server-side via AJAX
**Notes:** Semua filter (Bagian, Unit, Category, dll) juga dikirim ke server bersama date range. Setiap perubahan filter → AJAX request dengan semua parameter.

### Follow-up: Scope filter server-side

| Option | Description | Selected |
|--------|-------------|----------|
| Semua filter via server | Semua parameter dikirim sekaligus. Konsisten dan count selalu akurat. | ✓ |
| Hanya date range ke server | Filter lain tetap client-side. | |

**User's choice:** Semua filter via server

---

## Format & behavior input date

| Option | Description | Selected |
|--------|-------------|----------|
| Native HTML date input + auto-filter | `<input type="date">` bawaan browser. Auto-filter saat tanggal diisi. | ✓ |
| Native HTML date + tombol Apply | Pakai date input tapi perlu klik Apply. | |
| Date picker library | Library seperti flatpickr. UI lebih kaya tapi perlu dependency. | |

**User's choice:** Native HTML date input + auto-filter
**Notes:** Tidak perlu library tambahan, auto-trigger saat tanggal berubah.

---

## Claude's Discretion

- Debounce timing untuk AJAX requests
- Response format (JSON vs HTML partial)
- Loading indicator saat request
- Error handling

## Deferred Ideas

None
