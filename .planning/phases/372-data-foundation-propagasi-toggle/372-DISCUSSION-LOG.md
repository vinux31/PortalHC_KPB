# Phase 372: Data Foundation + Propagasi Toggle - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-13
**Phase:** 372-data-foundation-propagasi-toggle
**Areas discussed:** Cakupan Pre/Post saat create, Teks bantu toggle (HC), Ringkasan Langkah 4 Konfirmasi, Penempatan toggle Langkah 3

---

## Cakupan Pre/Post saat Create

| Option | Description | Selected |
|--------|-------------|----------|
| 1 pasang → Pre+Post sama | Satu pasang toggle di Langkah 3, nilai sama ke loop Pre DAN Post; diverge belakangan di ManagePackages (374); ikut pola PassPercentage | ✓ |
| Pasang terpisah Pre vs Post | Dua pasang toggle di wizard sejak create; lebih fleksibel tapi nambah kompleksitas + risiko bingung HC; spec arahkan divergensi di 374 | |

**User's choice:** 1 pasang → Pre+Post sama (Rekomendasi)
**Notes:** Divergensi Pre≠Post terjadi di Phase 374 (ManagePackages), bukan di wizard.

---

## Teks bantu toggle (HC)

| Option | Description | Selected |
|--------|-------------|----------|
| Label + penjelasan singkat | Label + form-text singkat | (di-upgrade user) |
| Label saja | Cuma label tanpa penjelasan | |

**User's choice:** "label + penjelasan **detail**" (free-text — lebih kuat dari opsi "singkat")
**Notes:** HC non-teknis; teks harus jelaskan efek nyata ON vs OFF dengan bahasa awam, cukup detail. Copy final = diskresi Claude saat planning.

---

## Ringkasan Langkah 4 Konfirmasi

| Option | Description | Selected |
|--------|-------------|----------|
| Tampilkan ON/OFF | Masuk summary Langkah 4 bareng Status/Pass%/Token; HC review sebelum submit | ✓ |
| Skip | Tidak tampil di konfirmasi | |

**User's choice:** Tampilkan ON/OFF (Rekomendasi)
**Notes:** —

---

## Penempatan toggle Langkah 3

| Option | Description | Selected |
|--------|-------------|----------|
| Grup B "Pengaturan Ujian" | Sejajar IsTokenRequired pakai form-check form-switch; konsisten tanpa card baru | ✓ |
| Grup/card baru sendiri | Card terpisah "Pengacakan"; lebih menonjol tapi nambah elemen UI | |

**User's choice:** Grup B "Pengaturan Ujian" (Rekomendasi)
**Notes:** Pola `form-check form-switch` (CreateAssessment.cshtml:505-508).

## Claude's Discretion

- Display attribute / property naming exact di entity.
- Copy/teks final penjelasan toggle (selama cukup detail).
- Nama field form / binding view ↔ model.
- Format visual summary Langkah 4.

## Deferred Ideas

- Toggle terpisah Pre vs Post di wizard — ditolak; divergensi di Phase 374.
