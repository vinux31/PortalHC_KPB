---
status: partial
phase: 261-validasi-konsistensi-field-organisasi-di-coachcoacheemapping-dan-directorate
source: [261-VERIFICATION.md]
started: 2026-03-26T04:01:00Z
updated: 2026-03-26T04:01:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Cleanup Report Display
expected: Login sebagai Admin, panggil CleanupCoachCoacheeMappingOrg via POST — redirect kembali ke halaman CoachCoacheeMapping, muncul notifikasi jumlah autoFixed dan daftar unfixable (jika ada)
result: [pending]

### 2. Assign Validation di UI
expected: Assign mapping baru dengan Section/Unit yang tidak ada di OrganizationUnit aktif — form menampilkan error "Section/Unit tidak ditemukan di data organisasi aktif."
result: [pending]

### 3. Import Row Error Display
expected: Import file Excel dengan coachee yang Section/Unit tidak valid — row berstatus Error dengan pesan "tidak valid di OrganizationUnit aktif"
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps
