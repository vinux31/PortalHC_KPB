---
status: diagnosed
phase: 266-review-submit-hasil
source: [266-VERIFICATION.md]
started: 2026-03-27T15:30:00Z
updated: 2026-03-28T08:15:00Z
---

## Current Test

[testing complete]

## Tests

### 1. SUBMIT-02 — Warning soal belum dijawab (re-test setelah fix)
expected: Login arsyad, jawab sebagian soal, klik "Selesai & Tinjau Jawaban". Alert kuning muncul + baris table-warning untuk soal belum dijawab. Hitungan answered count benar.
result: issue
reported: "ExamSummary menampilkan 'Semua 15 soal sudah dijawab. Siap dikumpulkan.' padahal hanya 2/15 yang dijawab (soal 1 dan 3). Tidak ada alert kuning warning. Tidak ada baris table-warning untuk soal kosong. Answered count 100% salah."
severity: blocker

### 2. CERT-01 — Download PDF sertifikat (re-test setelah fix)
expected: Login rino, navigasi ke Results/9, klik "Lihat Sertifikat" lalu download PDF. File .pdf ter-download valid, bukan HTTP 204 No Content.
result: pass

## Summary

total: 2
passed: 1
issues: 1
pending: 0
skipped: 0
blocked: 0

## Gaps

- truth: "Alert kuning muncul + baris table-warning untuk soal belum dijawab. Hitungan answered count benar."
  status: failed
  reason: "User reported: ExamSummary menampilkan 'Semua 15 soal sudah dijawab. Siap dikumpulkan.' padahal hanya 2/15 yang dijawab. Tidak ada alert kuning. Tidak ada table-warning. Answered count salah."
  severity: blocker
  test: 1
  artifacts:
    - Views/CMP/ExamSummary.cshtml
    - Controllers/CMPController.cs (ExamSummary action)
  missing: []
