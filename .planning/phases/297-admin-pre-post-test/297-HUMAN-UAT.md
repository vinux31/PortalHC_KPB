---
status: partial
phase: 297-admin-pre-post-test
source: [297-VERIFICATION.md]
started: 2026-04-07T00:00:00Z
updated: 2026-04-07T00:00:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Toggle show/hide dual-section jadwal saat pilih Pre-Post di form
expected: Saat dropdown tipe diubah ke "Pre-Post Test", section jadwal Pre dan Post muncul dan section jadwal standar tersembunyi. Saat diubah kembali ke "Standard", sebaliknya.
result: [pending]

### 2. Validasi backend PostSchedule > PreSchedule saat submit
expected: Submit form dengan PostSchedule <= PreSchedule menghasilkan error validasi "Jadwal Post-Test harus setelah jadwal Pre-Test"
result: [pending]

### 3. Expand/collapse baris monitoring Pre-Post dengan sub-rows
expected: Baris Pre-Post di AssessmentMonitoring dapat di-expand menampilkan sub-row Pre-Test dan Post-Test dengan stat masing-masing
result: [pending]

### 4. Badge "Pre-Post Test" di ManageAssessment
expected: Assessment Pre-Post menampilkan badge "Pre-Post Test" di card ManageAssessment
result: [pending]

### 5. Aliran copy paket soal Pre ke Post end-to-end
expected: Tombol "Copy dari Pre-Test" di ManagePackages Post berhasil deep-clone soal+opsi dari Pre ke Post
result: [pending]

### 6. Guard reset Pre dengan data Post berstatus Completed di DB
expected: Reset Pre-Test diblokir jika Post-Test sudah berstatus Completed, menampilkan pesan error
result: [pending]

### 7. Tab Pre/Post di EditAssessment dengan data LinkedGroupId valid
expected: EditAssessment menampilkan tab Pre dan Post terpisah dengan jadwal per-fase yang bisa diedit independen
result: [pending]

## Summary

total: 7
passed: 0
issues: 0
pending: 7
skipped: 0
blocked: 0

## Gaps
