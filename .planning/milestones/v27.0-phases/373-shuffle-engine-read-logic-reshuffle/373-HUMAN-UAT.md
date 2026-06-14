---
status: partial
phase: 373-shuffle-engine-read-logic-reshuffle
source: [373-VERIFICATION.md]
started: 2026-06-13
updated: 2026-06-13
---

## Current Test

[awaiting human testing — `dotnet run` @localhost:5277, AD off: `Authentication__UseActiveDirectory=false dotnet run`]

## Tests

### 1. Exam ON → soal teracak di layar ujian
expected: Buka assessment dengan ShuffleQuestions=ON via StartExam. Urutan/pemilihan soal teracak per peserta (perilaku existing tak berubah, SC#1). Halaman 200, tak ada RuntimeBinderException.
result: [pending]

### 2. Exam OFF + 1 paket → urutan soal identik semua peserta
expected: Assessment ShuffleQuestions=OFF, 1 paket. Dua akun peserta berbeda → urutan soal IDENTIK (urut `q.Order`), bukan acak.
result: [pending]

### 3. Reshuffle ShuffleOptions=ON → DB dict non-kosong (bug fix SHUF-09)
expected: HC reshuffle session Not-started/Abandoned dari assessment ShuffleOptions=ON → kolom `UserPackageAssignment.ShuffledOptionIdsPerQuestion` di DB BUKAN `"{}"` (opsi teracak). Sebelum fix selalu `"{}"`.
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps

(none — 5/5 must-haves verified programatik; 329/329 test hijau. Item di atas = UAT visual/live, scope penuh Playwright = Phase 375.)
