---
status: partial
phase: 353-admin-backend-gambar-crud-sync-atomic-delete
source: [353-VERIFICATION.md]
started: 2026-06-08
updated: 2026-06-08
---

## Current Test

[awaiting human testing — dikonsolidasi Phase 355 Playwright UAT]

## Tests

### 1. Upload gambar end-to-end (IMG-01/02/03)
expected: Tambah soal MC via form + pilih gambar soal & opsi A + alt → simpan → file fisik tersimpan di wwwroot/uploads/questions/{packageId}/, ImagePath/ImageAlt di DB terisi.
result: [pending]

### 2. Replace & hapus → file lama hilang dari disk (IMG-05/06, SYN-02)
expected: Edit soal → ganti gambar (file lama terhapus disk) ATAU centang "Hapus gambar" (file terhapus disk, ImagePath null). Edit teks tanpa sentuh gambar opsi → gambar opsi TETAP (OQ1).
result: [pending]

### 3. Shared-file ref-count no double-delete (D-10/D-11)
expected: Pre+Post SamePackage berbagi path. Hapus soal/paket Pre saat Post masih pakai → file TIDAK terhapus (ref-count skip). Preview admin render gambar soal+opsi.
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps
