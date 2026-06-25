# Phase 399: Foundation — Junction UserUnits + Primary-Mirror + Multi-Select UI + Display - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-18
**Phase:** 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
**Areas discussed:** Widget multi-select + primary, Format Bulk Import multi-unit, Cert/print _PSign + format display, Guard hapus-unit (MU-07)

---

## Gray-area selection

User memilih SEMUA 4 area (multiSelect): Widget multi-select + primary · Format Bulk Import multi-unit · Cert/print _PSign + format display · Guard hapus-unit (MU-07).

---

## Area 1 — Widget multi-select Unit + penanda primary

| Option | Description | Selected |
|--------|-------------|----------|
| Checkbox-list + radio primary | Cascade render checkbox-list unit Bagian (reuse SectionUnitsJson) + radio "Primary" per baris | ✓ |
| `<select multiple>` + dropdown Primary | Native multi-select ctrl-click + dropdown primary terpisah | |
| Chip/tag multi-select | Input chip/tag, chip pertama/ber-bintang = primary (butuh lib, no preseden) | |

**User's choice:** Checkbox-list + radio primary (rekomendasi)
**Notes:** Reuse penuh dict existing, no lib baru, primary eksplisit. → D-01/02/03.

---

## Area 2 — Format Bulk Import Excel multi-unit

| Option | Description | Selected |
|--------|-------------|----------|
| 1 sel delimiter, unit-1=primary | `Cell(6)`="UnitA\|UnitB", pipe delimiter, unit pertama=primary, layout existing tetap | ✓ |
| Delimiter + kolom Primary terpisah | Semua unit 1 sel + kolom "Primary Unit" baru (geser layout) | |
| Multi-baris per pekerja | 1 baris=1 unit, NIP berulang, flag primary (redundan field non-unit) | |

**User's choice:** 1 sel delimiter, unit-1=primary (rekomendasi)
**Notes:** Backward-compat template lama wajib. Pipe `|` dipilih (aman dari koma di nama unit). → D-04/05/06.

---

## Area 3 — Tampilan cert/print _PSign + format display

| Option | Description | Selected |
|--------|-------------|----------|
| _PSign primary-only, admin semua | _PSign (cert/print) primary-only [selaras D1=b]; surface admin tampil semua unit | |
| Semua surface tampil semua unit | _PSign JUGA tampil semua unit; konsisten penuh lintas 7 surface | ✓ |
| Semua primary-only kecuali WorkerDetail | Hanya WorkerDetail tampil semua; sisanya primary-only (langgar MU-03) | |

**User's choice:** Semua surface tampil semua unit (NON-rekomendasi — pilihan sadar)
**Notes:** Operator pilih konsistensi tampil-semua mengalahkan default cert-print-primary-only. Tradeoff D1=b (cert atribusi primary) diterima sengaja. _PSign print = primary-first koma-join. → D-07/08/09.

---

## Area 4 — Guard hapus-unit (MU-07)

| Option | Description | Selected |
|--------|-------------|----------|
| Hard-block + pesan referensi | Tolak simpan, list referensi aktif yg blok, operator deactivate manual | |
| Konfirmasi → auto-deactivate | Tampilkan dampak → konfirmasi → auto-deactivate referensi + hapus unit 1 transaksi | ✓ |
| Auto-deactivate diam-diam | Langsung deactivate+hapus tanpa konfirmasi (ditolak — silent destructive) | |

**User's choice:** Konfirmasi → auto-deactivate (NON-rekomendasi)
**Notes:** Picu follow-up scope (lihat bawah). → D-10/11/12.

### Follow-up Area 4 — scope auto-deactivate

| Option | Description | Selected |
|--------|-------------|----------|
| Coach-mapping auto; PROTON tetap blok | Coach-mapping aktif auto-deactivate-after-confirm; ProtonTrackAssignment aktif → HARD-BLOCK | ✓ |
| Auto-deactivate keduanya | Deactivate coach-mapping + PROTON track 1 transaksi (batalkan PROTON aktif dari form Worker) | |
| Keduanya auto, PROTON ekstra-warning | Auto keduanya + konfirmasi tingkat-2 utk PROTON aktif | |

**User's choice:** Coach-mapping auto; PROTON tetap blok (rekomendasi)
**Notes:** MU-07 jadi asimetris — coach-mapping mulus, PROTON aktif dilindungi hard-block (hindari abandon PROTON tahun-berjalan tak sengaja). → D-10/D-11.

---

## Claude's Discretion

- Lokasi backfill (migration `Up` vs `SeedData`) — lean migration `Up`.
- Mekanisme konfirmasi MU-07 (server round-trip vs AJAX pre-check) — lean server round-trip.
- Styling badge/chip primary + struktur DOM cascade checkbox-list.
- Index opsional unique `(UserId, Unit)`.

## Deferred Ideas

- MU-06 set-aware listing → Phase 400.
- Migrasi pembaca scalar `user.Unit` → bertahap.
- Cert/analytics per-unit akurat → out-of-scope (milestone terpisah).
- Todo "cleanup data test lokal pasca-367" — reviewed, NOT folded (tak relevan).
