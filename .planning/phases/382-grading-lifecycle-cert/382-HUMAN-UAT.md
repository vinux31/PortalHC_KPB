---
status: partial
phase: 382-grading-lifecycle-cert
source: [382-VERIFICATION.md]
started: "2026-06-15"
updated: "2026-06-15"
---

## Current Test

[awaiting human testing — auto-approved under auto-mode; DB-coherence already proven by tests]

## Tests

### 1. Konfirmasi visual sertifikat null (tanpa ValidUntil) tampil "Aktif/Permanen" konsisten di semua surface
expected: Sertifikat worker lulus dengan `ValidUntil == null` (non-Permanent) tampil sebagai **Aktif** (bukan Expired) di: dashboard CMP, dashboard CDP, worklist Renewal (cert null TIDAK muncul di worklist renewal), dan badge Home. Tally Renewal/CDP tidak menghitung cert null sebagai expired.
result: [pending — visual/pixel only]
note: Koherensi DB-level sudah terbukti otomatis via `CertAlertConsistencyTests` (4 fact, predicate-mirror lock) + e2e #12 (DB-assert cert visibility). Yang tersisa hanya konfirmasi rendering pixel di browser — bukan blocker korektnes. Auto-approved 2026-06-15 sesuai instruksi user (jalan otomatis, tak tanya per langkah).

## Summary

total: 1
passed: 0
issues: 0
pending: 1
skipped: 0
blocked: 0

## Gaps
