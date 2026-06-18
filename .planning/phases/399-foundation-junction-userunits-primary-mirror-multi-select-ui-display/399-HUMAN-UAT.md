---
status: partial
phase: 399-foundation-junction-userunits-primary-mirror-multi-select-ui-display
source: [399-VERIFICATION.md]
started: 2026-06-18
updated: 2026-06-18
---

## Current Test

[awaiting human testing]

## Tests

### 1. MU-07 modal coach-mapping — round-trip browser (W-09 fixture-skipped di e2e)
expected: Edit pekerja yang hapus unit yang masih dirujuk CoachCoacheeMapping.AssignmentUnit aktif → modal konfirmasi muncul → setujui → mapping ter-deactivate + unit terhapus (1 transaksi). Server guard sudah unit-tested (RemoveUnitGuardTests 5/5); yang belum: alur UI→confirm→deactivate runtime.
result: [pending]

### 2. MU-07 PROTON hard-block — red error render
expected: Edit pekerja yang hapus unit yang masih dirujuk ProtonTrackAssignment aktif → error merah hard-block ("Tutup/bypass PROTON dulu"), simpan ditolak. Server logic unit-tested; render error di browser belum dikonfirmasi (fixture PROTON-aktif absent di e2e).
result: [pending]

### 3. _PSign cetak + tampilan badge visual
expected: Kartu tanda tangan `_PSign` (cetak/cert) tampil SEMUA unit primary-first koma (D-07). Badge primary (hijau bg-success + bintang + "Utama") vs secondary kontras cukup di Home hero (background gelap) + ManageWorkers; cell tidak pecah/wrap aneh. Inherently visual — perlu mata manusia.
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps
