# Phase 415: Section Foundation + Import Excel Diperluas - Discussion Log

> **Audit trail only.** Jangan dipakai sebagai input planning/execution. Keputusan ada di CONTEXT.md.

**Date:** 2026-06-22
**Phase:** 415-section-foundation-import-excel-diperluas
**Areas discussed:** UI Kelola Section, Urut+Assign, Template Excel + dual-format, Validasi mismatch D-13

---

## UI Kelola Section

| Option | Selected |
|--------|----------|
| Panel inline di ManagePackageQuestions | ✓ |
| Halaman khusus ManagePackageSections | |
| Modal dari ManagePackages | |

**Pilihan:** Panel inline di ManagePackageQuestions (1 layar bareng kelola soal).

## Urut + Assign Soal→Section

| Option | Selected |
|--------|----------|
| No.Section angka + dropdown di form soal | ✓ |
| Drag-drop urut + bulk-assign soal terpilih | |
| Tombol naik/turun + dropdown per-soal | |

**Pilihan:** No.Section angka (1,2,3) untuk urutan + dropdown pilih Section di form soal. JS minim, no drag-drop. Bulk-assign defer.

## Template Excel + Dual-Format

| Option | Selected |
|--------|----------|
| Satu template universal + deteksi otomatis by jumlah-kolom | ✓ |
| Dua template terpisah + HC pilih/tandai | |

**Catatan:** user minta penjelasan simpel dulu (file lama 9-kolom harus tetap bisa di-import). Setelah dijelaskan → pilih deteksi-otomatis (≤9 kolom=lama A–D no-section; >9=baru). HC tak pilih manual.

## Validasi Mismatch Struktur Section (D-13)

| Option | Selected |
|--------|----------|
| Saat upload import (daftar error) + guard ulang saat mulai ujian | ✓ |
| Hanya saat simpan/import | |
| Hanya saat mulai ujian | |

**Pilihan:** Dua titik — tolak saat import (daftar lengkap, sebut SectionNumber + jumlah) + guard ulang sebelum mulai ujian (anti drift edit manual).

## Claude's Discretion
- Abstraksi `SectionAwareQuestionProvider`/`IQuestionSequence` (spec §13) — planner.
- Mekanik migration, index, impl fingerprint, struktur partial view.

## Deferred Ideas
- Bulk-assign soal→Section; drag-drop reorder (SortableJS) — future.
- Todo "cleanup data-test 367" — tak relevan, defer.
