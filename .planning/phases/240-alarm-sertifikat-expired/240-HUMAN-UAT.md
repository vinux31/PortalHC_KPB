---
status: complete
phase: 240-alarm-sertifikat-expired
source: [240-VERIFICATION.md]
started: 2026-03-23
updated: 2026-03-23
---

## Current Test

[testing complete]

## Tests

### 1. Banner visual di Home/Index
expected: Login HC/Admin, banner merah (expired count) dan kuning (akan expired count) muncul setelah hero section
result: pass

### 2. Bell dropdown CERT_EXPIRED
expected: Klik icon bell, ada notifikasi "Sertifikat [Judul] milik [Nama] telah expired" dengan link ke RenewalCertificate
result: pass

### 3. Klik Lihat Detail
expected: Klik "Lihat Detail" di banner navigasi ke /Admin/RenewalCertificate
result: pass

### 4. Role guard — user biasa
expected: Login user biasa (bukan HC/Admin), banner TIDAK muncul di Home/Index
result: pass

### 5. Deduplication notifikasi
expected: Refresh halaman HC/Admin beberapa kali, notifikasi CERT_EXPIRED tidak duplikat
result: pass

## Summary

total: 5
passed: 5
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps
