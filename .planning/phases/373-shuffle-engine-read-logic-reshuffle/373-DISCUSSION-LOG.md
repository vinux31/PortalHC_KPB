# Phase 373: Shuffle Engine (read logic + reshuffle) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-13
**Phase:** 373-shuffle-engine-read-logic-reshuffle
**Areas discussed:** Dedup core extraction, Anchor determinisme OFF≥2, Reshuffle OFF + bug opsi, Jumlah soal OFF≥2

---

## Area selection

| Option | Description | Selected |
|--------|-------------|----------|
| Dedup core extraction | Satukan `BuildCrossPackageAssignment` duplikat | ✓ |
| Anchor determinisme OFF≥2 | Kunci stabil worker→paket | ✓ |
| Reshuffle OFF + bug opsi | Semantik reshuffle + fix `"{}"` | ✓ |
| Jumlah soal OFF≥2 | Paket utuh vs K-min | ✓ |

**User's choice:** Semua 4 area.

---

## Dedup core extraction

| Option | Description | Selected |
|--------|-------------|----------|
| Satu engine bersama | 1 core pure (soal+opsi) dipakai StartExam + 2 reshuffle; hapus duplikasi | ✓ |
| Dedup soal saja | Satukan question-distribution, opsi inline | |
| Minimal | Core baru utk OFF saja, biarkan duplikasi | |

**User's choice:** Satu engine bersama.
**Notes:** Pola extract-static-core project; menjamin fix bug reshuffle konsisten dgn StartExam. ON-path dipertahankan verbatim (D-01a).

---

## Anchor determinisme OFF≥2

| Option | Description | Selected |
|--------|-------------|----------|
| Filter dulu, baru modulo | daftar paket-ber-soal (PackageNumber); index worker = sibling OrderBy(Id); paket[index % count] | ✓ |
| Modulo semua, skip kosong | index % semuaPaket lalu cari ber-soal berikutnya | |

**User's choice:** Filter dulu, baru modulo.
**Notes:** Peserta baru di-append (Id besar) tak geser assignment lama; guard paket kosong sebelum modulo; tolak "urutan buka"/`assignmentCount % n`.

---

## Reshuffle OFF + bug opsi

| Option | Description | Selected |
|--------|-------------|----------|
| Honor flag + guard tetap | Rebuild per ShuffleQuestions/ShuffleOptions; fix bug `"{}"`; guard Not started/Abandoned tetap | ✓ |
| Honor flag + blok saat Soal OFF | Tolak reshuffle bila ShuffleQuestions OFF | |
| Honor flag + longgarkan guard | Izinkan InProgress | |

**User's choice:** Honor flag + guard tetap.
**Notes:** OFF reshuffle = rebuild deterministik idempotent (tak ada perubahan urutan terlihat by design). Opsi teracak saat ShuffleOptions ON (fix bug `:5119`/`:5213`).

---

## Jumlah soal OFF≥2

| Option | Description | Selected |
|--------|-------------|----------|
| Paket utuh, tak dipotong | Worker dapat seluruh soal paketnya (q.Order); jumlah/nilai-maks bisa beda | ✓ |
| Potong ke K-min | Seragamkan ke paket terkecil | |

**User's choice:** Paket utuh, tak dipotong.
**Notes:** Awalnya user minta penjelasan ("maksutnya apa ini"). Dijelaskan: contoh paket 50/40/45 soal, konsekuensi nilai-maks beda, warning §9 menutupi (UI Phase 374), pass% relatif tetap sebanding, beda dari mode ON ≥2 (sampling K-min). Setelah penjelasan → pilih paket utuh (sesuai spec).

## Claude's Discretion

- Lokasi/nama/signature shared core (pure, tanpa EF di dalam).
- Pembagian penulisan test core: sebagian Wave 0 di 373 vs ditahan penuh ke Phase 375.

## Deferred Ideas

- UI warning §9 + toggle ManagePackages + lock + reminder + hide Proton/Manual → Phase 374.
- xUnit mode-matrix penuh + Playwright UAT → Phase 375.
- Pindah setting `SamePackage` → out of scope (spec §12).
