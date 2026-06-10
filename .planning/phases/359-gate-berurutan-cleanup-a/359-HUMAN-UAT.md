---
status: partial
phase: 359-gate-berurutan-cleanup-a
source: [359-VERIFICATION.md]
started: 2026-06-10T04:40:00Z
updated: 2026-06-10T04:40:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. CreateAssessment Proton gate (skip-summary, PCOMP-06/07/08)
expected: Buat Assessment Proton untuk campuran worker eligible + tak-100% deliverable + Tahun N-1 belum lulus @ localhost:5277 (AD lokal `Authentication__UseActiveDirectory=false dotnet run`). Hanya worker eligible dapat session; banner Warning menyebut jumlah di-skip + alasan ("X belum 100% deliverable, Y Tahun sebelumnya belum lulus"); semua tak-eligible → 0 session, tanpa transaksi terbuka, kembali ke form.
result: [pending]

### 2. CoachMapping cross-year hard-block (PCOMP-07)
expected: Assign Tahun 2 untuk coachee yang Tahun 1 belum lulus via POST CoachCoacheeMappingAssign. JSON success=false dengan pesan S2 ("Tidak bisa assign Tahun 2: Tahun 1 (TrackType) belum lulus untuk N coachee"); TIDAK ada tombol/konfirmasi "Tetap lanjutkan?". Assign Tahun 1 (tanpa prasyarat) tetap sukses.
result: [pending]

### 3. Graduation gate (PCOMP-09)
expected: Klik "Mark graduated" untuk worker Tahun 3 belum lulus → banner Error S2 ("Tidak bisa menandai lulus (graduated): Tahun 3 belum lulus untuk pekerja ini"), IsCompleted TIDAK di-set. Untuk worker Tahun 3 lulus (penanda ada) → sukses, mapping IsCompleted=true + cascade deactivate assignment.
result: [pending]

### 4. CDP/Histori render tanpa level + grafik tren (PCOMP-10)
expected: Buka dashboard CDP coachee, view supervisor/HC Proton, dan HistoriProtonDetail @5277. Badge "Status Proton: Lulus/Belum Lulus" tanpa angka level; tabel coachee badge Lulus/In Progress/No track tanpa angka; TIDAK ada card grafik tren maupun placeholder "no data"; HistoriProton tanpa blok "Level Kompetensi"; semua halaman render tanpa error 500.
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
