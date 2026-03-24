---
status: partial
phase: 246-uat-edge-cases-records
source: [246-VERIFICATION.md]
started: 2026-03-24T11:30:00Z
updated: 2026-03-24T11:30:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. HV-01/HV-04: Token salah ditolak dan regenerate token berfungsi di browser
expected: Token salah menampilkan pesan error, token baru diterima setelah regenerate
result: [pending]

### 2. HV-02/HV-03: Force close dan reset melalui monitoring di browser
expected: Rino di-redirect saat AkhiriUjian, status kembali Open setelah Reset
result: [pending]

### 3. HV-05: Alarm banner expired muncul untuk HC/Admin di Home/Index
expected: Banner merah muncul dengan link ke /Admin/RenewalCertificate, renewal flow berjalan end-to-end
result: [pending]

### 4. HV-06/HV-07: Records + export Excel berfungsi di browser
expected: File Excel berhasil didownload dan dapat dibuka, filter date range berfungsi
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
