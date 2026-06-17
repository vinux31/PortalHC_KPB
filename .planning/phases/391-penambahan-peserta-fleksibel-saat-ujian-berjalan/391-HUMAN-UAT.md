---
status: partial
phase: 391-penambahan-peserta-fleksibel-saat-ujian-berjalan
source: [391-VERIFICATION.md]
started: 2026-06-17
updated: 2026-06-17
---

## Current Test

[awaiting human testing — di Dev pasca-deploy IT]

## Tests

### 1. Alur penambahan peserta ke assessment yang sedang berjalan (end-to-end)
expected: HC buka EditAssessment pada assessment dengan ≥1 peserta `InProgress`, tambah peserta baru, klik Simpan → (a) muncul alert **biru "Info"** (bukan kuning "Warning") berisi pesan menenangkan "Peserta baru tetap dapat ditambahkan..."; (b) peserta baru berstatus Open/Upcoming dan bisa StartExam; (c) peserta yang sedang `InProgress` TIDAK terganggu (timer/status/jadwal tetap).
result: [pending]

### 2. Guard Completed pada EDIT murni tetap ditolak
expected: HC buka EditAssessment pada assessment yang sesi representatifnya `Completed`, lalu Simpan TANPA menambah peserta → muncul error "Cannot edit completed assessments." (jalur edit murni tetap diblokir; hanya jalur penambahan yang dilonggarkan).
result: [pending]

## Summary

total: 2
passed: 0
issues: 0
pending: 2
skipped: 0
blocked: 0

## Gaps

(none — automated 8/8 verified; 2 item visual/end-to-end ditangguhkan ke UAT browser Dev pasca-deploy IT, konsisten alur DEV BROWSER UAT milestone sebelumnya)
