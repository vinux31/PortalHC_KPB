---
status: partial
phase: 350-team-view-search-scope-export-parity
source: [350-VERIFICATION.md]
started: 2026-06-05
updated: 2026-06-05
---

## Current Test

[awaiting human testing — non-blocking; phase closed with developer approval 2026-06-05]

## Tests

### 1. Export Assessment XLSX content — Category drop-archived (SF-06 visual)
expected: Dengan filter Kategori aktif (mis. "OJT") + klik Export Assessment → buka file .xlsx → baris = worker on-screen, baris assessment **archived** (legacy tanpa kolom Category) **absen**. Hapus filter Kategori → Export lagi → baris archived **muncul**. (Automated Playwright `cmp-records-350.spec.ts` sudah cover href + counter; isi XLSX = manual eyeball per RESEARCH Open Question 2 keputusan href-only.)
result: [pending]

### 2. Badge count visual unchanged saat search (D-07 visual)
expected: Saat search judul assessment di Team View, badge "Assessment Lulus N" per worker tetap = jumlah seluruh sesi worker (bukan hanya yg cocok search). Code-verified + xUnit `Search_DoesNotMutate_BadgeCounts_D07` GREEN; visual eyeball opsional.
result: [pending]

## Summary

total: 2
passed: 0
issues: 0
pending: 2
skipped: 0
blocked: 0

## Gaps

Tidak ada gap fungsional — 5/5 must-haves code-verified + 109/109 xUnit + Playwright 2 passed (real browser). 2 item di atas = visual eyeball manual (XLSX content + badge), developer approved phase-close 2026-06-05 menerima keduanya sebagai tracked deferred (akan muncul di /gsd-progress + /gsd-audit-uat). Tutup dengan `/gsd-verify-work 350` saat IT/dev sempat eyeball, atau saat promosi Dev.
