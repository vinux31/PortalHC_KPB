---
status: passed
phase: 343-integrasi-app-wide
source: [343-VERIFICATION.md]
started: 2026-06-03
updated: 2026-06-03
---

## Current Test

[selesai — SC2 verified live via Playwright MCP 2026-06-03]

## Tests

### 1. SC2 — Rename label "Bagian"→"Direktorat" muncul di ≥3 page integrasi
expected: Login admin di `http://localhost:5277` → ubah label Level 0 "Bagian" → "Direktorat" via `/Admin/ManageOrgLevelLabels` → buka (1) `/CMP/AnalyticsDashboard` filter label+dropdown, (2) `/Admin/ManageWorkers` filter label+table header, (3) `/CDP/CertificationManagement` filter label. Ketiganya tampil "Direktorat" (bukan "Bagian"), tidak ada fallback "Level 0". Restore label "Bagian" setelah test.
result: PASS (Playwright MCP, 2026-06-03, admin@pertamina.com) — ketiga page menampilkan "Direktorat" di filter label + dropdown ("Semua Direktorat") + table header (ManageWorkers + CertificationManagement). Unit header tetap "Unit"; data unit-name (GAST/Alkylation Unit (065)) untouched; tidak ada fallback "Level 0". Cache OrgLabelService ter-bust otomatis saat CRUD save. Label DI-RESTORE ke "Bagian" (DB lokal baseline).

## Summary

total: 1
passed: 1
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

None — SC2 PASS.
