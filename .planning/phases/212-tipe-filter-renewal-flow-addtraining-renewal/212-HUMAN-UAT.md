---
status: partial
phase: 212-tipe-filter-renewal-flow-addtraining-renewal
source: [212-VERIFICATION.md]
started: 2026-03-21T07:00:00Z
updated: 2026-03-21T07:00:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Filter Tipe — visual dan fungsional
expected: Buka `/Admin/RenewalCertificate`, pilih "Training" pada dropdown Tipe. Tabel hanya menampilkan baris dengan RecordType = Training; summary cards ikut terupdate.
result: [pending]

### 2. Single Renew modal
expected: Klik tombol "Renew" pada sembarang baris. Modal "Pilih Metode Renewal" muncul dengan judul dan nama pekerja terisi; dua tombol tersedia.
result: [pending]

### 3. Bulk renew mixed-type error
expected: Pilih baris Assessment dan baris Training sekaligus, klik Renew Group. Modal menampilkan alert merah "Tidak dapat bulk renew campuran tipe" dan tombol Lanjutkan disabled.
result: [pending]

### 4. AddTraining renewal banner dan prefill
expected: Buka `/Admin/AddTraining?renewTrainingId=<id_valid>`. Banner kuning "Mode Renewal" muncul, field Judul dan Kategori ter-prefill, field Peserta ter-set ke pekerja asal.
result: [pending]

### 5. AddTraining POST FK tersimpan
expected: Submit form AddTraining dari renewal mode. TrainingRecord baru memiliki `RenewsTrainingId` atau `RenewsSessionId` terisi sesuai sumber.
result: [pending]

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0
blocked: 0

## Gaps
