---
status: partial
phase: 240-alarm-sertifikat-expired
source: [240-VERIFICATION.md]
started: 2026-03-23
updated: 2026-03-23
---

## Current Test

[awaiting human testing]

## Tests

### 1. Banner visual di Home/Index
expected: Login HC/Admin, banner merah (expired count) dan kuning (akan expired count) muncul setelah hero section
result: [pending]

### 2. Bell dropdown CERT_EXPIRED
expected: Klik icon bell, ada notifikasi "Sertifikat [Judul] milik [Nama] telah expired" dengan link ke RenewalCertificate
result: [pending]

### 3. Klik Lihat Detail
expected: Klik "Lihat Detail" di banner navigasi ke /Admin/RenewalCertificate
result: [pending]

### 4. Role guard — user biasa
expected: Login user biasa (bukan HC/Admin), banner TIDAK muncul di Home/Index
result: [pending]

### 5. Deduplication notifikasi
expected: Refresh halaman HC/Admin beberapa kali, notifikasi CERT_EXPIRED tidak duplikat
result: [pending]

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0
blocked: 0

## Gaps
