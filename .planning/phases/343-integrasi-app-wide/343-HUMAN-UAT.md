---
status: partial
phase: 343-integrasi-app-wide
source: [343-VERIFICATION.md]
started: 2026-06-03
updated: 2026-06-03
---

## Current Test

[awaiting human/browser testing — SC2 visual rename spot-render]

## Tests

### 1. SC2 — Rename label "Bagian"→"Direktorat" muncul di ≥3 page integrasi
expected: Login admin di `http://localhost:5277` → ubah label Level 0 "Bagian" → "Direktorat" via `/Admin/ManageOrgLevelLabels` → buka (1) `/CMP/AnalyticsDashboard` filter label+dropdown, (2) `/Admin/ManageWorkers` filter label+table header, (3) `/CDP/CertificationManagement` filter label. Ketiganya tampil "Direktorat" (bukan "Bagian"), tidak ada fallback "Level 0". Restore label "Bagian" setelah test.
result: [pending]

## Summary

total: 1
passed: 0
issues: 0
pending: 1
skipped: 0
blocked: 0

## Gaps
