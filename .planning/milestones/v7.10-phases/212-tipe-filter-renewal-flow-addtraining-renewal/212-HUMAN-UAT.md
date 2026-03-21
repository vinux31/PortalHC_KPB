---
status: complete
phase: 212-tipe-filter-renewal-flow-addtraining-renewal
source: [212-VERIFICATION.md]
started: 2026-03-21T07:00:00Z
updated: 2026-03-21T07:26:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Filter Tipe — visual dan fungsional
expected: Buka `/Admin/RenewalCertificate`, pilih "Training" pada dropdown Tipe. Tabel hanya menampilkan baris dengan RecordType = Training; summary cards ikut terupdate.
result: pass

### 2. Single Renew modal
expected: Klik tombol "Renew" pada sembarang baris. Modal "Pilih Metode Renewal" muncul dengan judul dan nama pekerja terisi; dua tombol tersedia.
result: pass

### 3. Bulk renew mixed-type error
expected: Pilih baris Assessment dan baris Training sekaligus, klik Renew Group. Modal menampilkan alert merah "Tidak dapat bulk renew campuran tipe" dan tombol Lanjutkan disabled.
result: skipped
reason: Checkbox cross-group otomatis disabled — mixed-type tidak bisa di-trigger. Safety net code ada tapi hanya untuk edge case.

### 4. AddTraining renewal banner dan prefill
expected: Buka `/Admin/AddTraining?renewTrainingId=<id_valid>`. Banner kuning "Mode Renewal" muncul, field Judul dan Kategori ter-prefill, field Peserta ter-set ke pekerja asal.
result: pass

### 5. AddTraining POST FK tersimpan
expected: Submit form AddTraining dari renewal mode. TrainingRecord baru memiliki `RenewsTrainingId` atau `RenewsSessionId` terisi sesuai sumber.
result: pass

## Summary

total: 5
passed: 4
issues: 0
pending: 0
skipped: 1
blocked: 0

## Gaps
