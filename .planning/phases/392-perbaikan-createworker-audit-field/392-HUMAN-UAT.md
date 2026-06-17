---
status: partial
phase: 392-perbaikan-createworker-audit-field
source: [392-VERIFICATION.md]
started: 2026-06-17
updated: 2026-06-17
---

## Current Test

[awaiting human testing — di Dev server (AD mode aktif) pasca-deploy IT]

## Tests

### 1. Field Nama Lengkap & Email bisa diketik saat AD mode AKTIF (Dev/Prod)
expected: Buka `http://10.55.3.3/KPB-PortalHC/Admin/CreateWorker` (AD-on) → field "Nama Lengkap" & "Email" **bisa diketik** (tidak readonly/abu-abu) walau `Authentication:UseActiveDirectory=true`; teks info AD berbunyi "Isi sesuai akun AD Pertamina ... diselaraskan saat login"; buat 1 akun pekerja test → tersimpan.
why_manual: Playwright lokal hanya jalan di AD-off (login lokal). Editability saat AD-on terbukti **by construction** (static grep: `readonly`/`bg-light` dihapus unconditional — TEST A green) + e2e runtime AD-off membuktikan editable/cascade/create. Konfirmasi visual AD-on di Dev = gate sebelum Prod.
result: [pending]

## Summary

total: 1
passed: 0
issues: 0
pending: 1
skipped: 0
blocked: 0

## Gaps

(none — automated 10/10 verified + e2e 3/3 green AD-off; 1 item visual AD-on ditangguhkan ke UAT browser Dev pasca-deploy IT, konsisten alur milestone sebelumnya)
