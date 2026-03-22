---
status: partial
phase: 234-audit-setup-flow
source: [234-VERIFICATION.md]
started: 2026-03-22T15:05:00Z
updated: 2026-03-22T15:05:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Silabus Delete Modal Warning di UI
expected: Login sebagai Admin, buka halaman Silabus (ada silabus dengan progress aktif). Klik Delete — modal warning muncul menampilkan jumlah coachee dan progress aktif, tombol hard delete disabled.
result: [pending]

### 2. Import Silabus Per-Row Error Table
expected: Upload file Excel dengan 1 baris valid dan 1 baris kolom Deliverable kosong — halaman menampilkan tabel per-baris dengan baris Error (merah) dan tidak ada data masuk ke DB.
result: [pending]

### 3. Assign Tahun 2 Warning Dialog
expected: Assign coachee ke Tahun 2 saat Tahun 1 belum selesai — warning dialog muncul dengan jumlah coachee yang belum selesai; tombol "Tetap Lanjutkan" ada.
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps
