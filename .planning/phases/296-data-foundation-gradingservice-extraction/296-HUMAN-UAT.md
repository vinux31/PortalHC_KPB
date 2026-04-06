---
status: partial
phase: 296-data-foundation-gradingservice-extraction
source: [296-VERIFICATION.md]
started: 2026-04-06
updated: 2026-04-06
---

## Current Test

[awaiting human testing]

## Tests

### 1. Database migration ter-apply ke schema DB aktual
expected: `dotnet ef database update` berhasil tanpa error, kolom baru muncul di tabel
result: [pending]

### 2. End-to-end SubmitExam flow (worker side)
expected: Worker submit exam → skor terhitung benar, TrainingRecord terbuat, NomorSertifikat ter-generate jika passed + GenerateCertificate=true
result: [pending]

### 3. AkhiriUjian admin flow
expected: Admin akhiri ujian → skor terhitung, SignalR push ke worker, cache invalidation berjalan
result: [pending]

### 4. NotImplementedException handling untuk MA/Essay
expected: Jika ada soal MultipleAnswer/Essay di data existing, controller tidak crash 500 — exception ter-handle gracefully
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
