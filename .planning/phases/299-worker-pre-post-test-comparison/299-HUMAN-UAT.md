---
status: partial
phase: 299-worker-pre-post-test-comparison
source: [299-VERIFICATION.md]
started: 2026-04-07T00:00:00Z
updated: 2026-04-07T00:00:00Z
---

## Current Test

[awaiting human testing — deferred to end of milestone v14.0]

## Tests

### 1. Pre-Post card pair visible with badge and arrow
expected: Pre-Test dan Post-Test muncul sebagai 2 card terhubung dengan border kiri biru, badge Pre-Test/Post-Test, dan arrow icon
result: [pending]

### 2. Post-Test blocked when Pre not completed
expected: Post card opacity-50 (grayed) dengan tombol disabled "Selesaikan Pre-Test terlebih dahulu"
result: [pending]

### 3. Post-Test shows "Pre-Test tidak diselesaikan" when Pre expired
expected: Badge merah "Pre-Test tidak diselesaikan" tampil di Post card
result: [pending]

### 4. Tab filtering works for pair cards
expected: Pair card muncul di tab yang sesuai status Post-Test
result: [pending]

### 5. Riwayat Ujian badge Pre-Test/Post-Test
expected: Badge bg-info "Pre-Test" atau badge bg-primary "Post-Test" tampil sebelum judul di tabel Riwayat
result: [pending]

### 6. Results Post-Test comparison section with gain score
expected: Card "Perbandingan Pre-Post Test" tampil dengan tabel Elemen Kompetensi, Skor Pre, Skor Post, Gain Score
result: [pending]

### 7. Gain score color coding
expected: +X.X% hijau, -X.X% merah, 0% abu-abu, dash dengan "Menunggu penilaian Essay" saat pending
result: [pending]

### 8. Mobile responsive
expected: Di viewport < 768px: card stack vertikal, arrow berubah ke panah bawah
result: [pending]

## Summary

total: 8
passed: 0
issues: 0
pending: 8
skipped: 0
blocked: 0

## Gaps
