---
status: partial
phase: 266-review-submit-hasil
source: [266-VERIFICATION.md]
started: 2026-03-27T15:30:00Z
updated: 2026-03-27T15:30:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. SUBMIT-02 — Warning soal belum dijawab (re-test setelah fix)
expected: Login arsyad, jawab sebagian soal, klik "Selesai & Tinjau Jawaban". Alert kuning muncul + baris table-warning untuk soal belum dijawab. Hitungan answered count benar.
result: [pending]

### 2. CERT-01 — Download PDF sertifikat (re-test setelah fix)
expected: Login rino, navigasi ke Results/9, klik "Lihat Sertifikat" lalu download PDF. File .pdf ter-download valid, bukan HTTP 204 No Content.
result: [pending]

## Summary

total: 2
passed: 0
issues: 0
pending: 2
skipped: 0
blocked: 0

## Gaps
