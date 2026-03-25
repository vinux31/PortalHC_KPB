# Phase 253: AddTraining multi-select pekerja dan perbaikan form - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-25
**Phase:** 253-addtraining-multi-select-pekerja-dan-perbaikan-form
**Areas discussed:** Multi-select pekerja, Perilaku simpan bulk, Kategori duplikat, Perbaikan form lainnya, Dynamic rows detail, Validasi multi-select, Library pilihan

---

## Multi-select pekerja — UI

| Option | Description | Selected |
|--------|-------------|----------|
| Searchable multi-select | Dropdown dengan search box + checkbox per pekerja. Pakai Tom Select atau Select2. | ✓ |
| Checkbox list | Daftar semua pekerja dengan checkbox. Scrollable container. | |
| Native multi-select | HTML select multiple standar. Ctrl+click. | |

**User's choice:** Searchable multi-select
**Notes:** None

## Multi-select pekerja — Single mode

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, dua mode | Renewal tetap single, akses langsung bisa multi. Backward compatible. | ✓ |
| Selalu multi-select | Semua flow pakai multi-select. | |

**User's choice:** Ya, dua mode
**Notes:** None

## Perilaku simpan bulk

| Option | Description | Selected |
|--------|-------------|----------|
| 1 record per pekerja | Setiap pekerja dapat training record terpisah. Konsisten dengan flow renewal. | ✓ |
| 1 record shared | Satu training record di-share banyak pekerja. | |

**User's choice:** 1 record per pekerja
**Notes:** None

## File sertifikat

| Option | Description | Selected |
|--------|-------------|----------|
| File sama untuk semua | Satu file upload di-share ke semua record. | |
| Tidak ada upload saat multi | Disable file upload kalau >1 pekerja. | |
| Dynamic rows per pekerja | (User's own idea) Setiap pekerja punya file upload sendiri. | ✓ |

**User's choice:** Setiap pekerja punya file sertifikat sendiri
**Notes:** User menjelaskan bahwa setiap training harus punya pemilih worker sendiri dan file sendiri.

## Dynamic rows — UI upload per pekerja

| Option | Description | Selected |
|--------|-------------|----------|
| Dynamic rows per pekerja | Tampilkan daftar pekerja terpilih sebagai rows/cards dengan file upload + nomor sertifikat. | ✓ |
| Single upload, apply semua | Upload 1 file, semua pekerja dapat file sama. | |
| Upload terpisah di step kedua | Step 1: data training. Step 2: upload per pekerja. | |

**User's choice:** Dynamic rows per pekerja
**Notes:** None

## Dynamic rows — Field per pekerja

| Option | Description | Selected |
|--------|-------------|----------|
| File + Nomor Sertifikat saja | Data training shared. Per pekerja hanya beda file dan nomor sertifikat. | ✓ |
| File + Nomor + ValidUntil + CertType | Semua field sertifikat per pekerja. | |
| Semua field per pekerja | Form lengkap per pekerja. | |

**User's choice:** File + Nomor Sertifikat saja
**Notes:** None

## Validasi — Limit pekerja

| Option | Description | Selected |
|--------|-------------|----------|
| Min 1, tanpa max | Tidak ada batas atas. | |
| Min 1, max 20 | Batas atas 20 pekerja. | ✓ |
| Min 1, max 50 | Batas lebih longgar. | |

**User's choice:** Min 1, max 20
**Notes:** None

## Validasi — Error file

| Option | Description | Selected |
|--------|-------------|----------|
| Block semua, tampilkan error | Validasi semua file sebelum simpan. Tidak ada record disimpan jika ada gagal. | ✓ |
| Simpan yang valid, skip yang gagal | Record tanpa file tetap dibuat. | |

**User's choice:** Block semua, tampilkan error
**Notes:** None

## Library JS

| Option | Description | Selected |
|--------|-------------|----------|
| Tom Select | Lightweight, no jQuery, modern API. | |
| Select2 | Populer, butuh jQuery. Project sudah punya jQuery. | |
| Claude's discretion | Claude pilih berdasarkan stack project. | ✓ |

**User's choice:** Kamu yang tentukan
**Notes:** Project sudah punya jQuery 3.7.1 di _Layout.cshtml.

## Kategori duplikat

| Option | Description | Selected |
|--------|-------------|----------|
| Filter duplikat di query | GroupBy/Distinct di SetTrainingCategoryViewBag. Tidak ubah data. | ✓ |
| Fix di database/seeder | Hapus record duplikat di DB. | |
| Claude's discretion | Claude pilih pendekatan terbaik. | |

**User's choice:** Filter duplikat di query
**Notes:** None

## Perbaikan form lainnya

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak ada, cukup yang dibahas | Focus pada multi-select + dynamic rows + fix duplikat. | ✓ |
| Ada, biar saya jelaskan | User punya perbaikan tambahan. | |

**User's choice:** Tidak ada, cukup yang dibahas
**Notes:** None

---

## Claude's Discretion

- Library JS pilihan (Tom Select vs Select2) — akan dipilih berdasarkan kompatibilitas stack
- Styling dynamic rows (card vs table row vs list item)

## Deferred Ideas

None — discussion stayed within phase scope
