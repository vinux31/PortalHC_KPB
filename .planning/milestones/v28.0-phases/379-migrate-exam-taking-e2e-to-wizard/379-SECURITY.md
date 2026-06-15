---
phase: 379
slug: migrate-exam-taking-e2e-to-wizard
asvs_level: L1
threats_open: 0
threats_closed: 18
status: SECURED
audited: 2026-06-14
---

# SECURITY.md — Phase 379: migrate-exam-taking-e2e-to-wizard

**Scope:** TEST-INFRA ONLY. Fase ini tidak mengubah satu pun file kode produksi (Controllers/, Views/, Data/SeedData.cs). Nol permukaan serangan produksi baru. Seluruh ancaman bersifat test-infra: seed sementara lokal, guard DB non-localhost, dan pembatasan scope.

---

## Threat Verification

| Threat ID | Category | Disposition | Evidence |
|-----------|----------|-------------|----------|
| T-379-01 | Tampering | mitigate | `docs/SEED_JOURNAL.md` baris 379-03 status=`cleaned` (2026-06-14); `global.teardown.ts:65` `db.restore()` safety-net; snapshot `C:\Temp\HcPortalDB_Dev_pre379_protonseed.bak` terdokumentasi |
| T-379-02 | Tampering | mitigate | `tests/helpers/dbSnapshot.ts:40-43` — guard `Refusing to target non-localhost SQL Server` diimplementasi; SQLCMD_BASE_ARGS hardcode `-S localhost\SQLEXPRESS` |
| T-379-03 | Scope/Tampering | accept | Scope test-infra dipatuhi; `379-VERIFICATION.md` kolom "Scope test-infra only (0 prod code)" = PASS; tidak ada modifikasi Controllers/ Views/ |
| T-379-04 | Tampering | mitigate | `exam-taking.spec.ts` pakai `uniqueTitle()` (12 occurrences); per-flow cleanup step SURVIVE; `global.teardown.ts:65` `db.restore()` |
| T-379-05 | Scope | accept | Sama dengan T-379-03; tidak ada perubahan produksi; bug → backlog |
| T-379-06 | Info Disclosure | accept | Akun test (rino/iwan3/hc) = existing test-only credentials, bukan surface baru; tidak ada credential sensitif di spec |
| T-379-07 | Tampering | mitigate | `global.teardown.ts:65` `db.restore()`; `docs/SEED_JOURNAL.md` status=`cleaned`; `uniqueTitle()` isolasi per-flow |
| T-379-08 | Spoofing/CSRF | accept | Interview form pakai `@Html.AntiForgeryToken` (auto-included dalam real-form-submit test); bukan attack surface baru; `379-03-PLAN.md` interfaces Flow E3 mencatat `@Html.AntiForgeryToken auto-included` |
| T-379-09 | Scope | accept | Drift produksi → backlog, tidak difix inline; `379-03-SUMMARY.md` "0 kode produksi diubah" |
| T-379-10 | Tampering | mitigate | `uniqueTitle()` di exam-taking.spec.ts (12 occurrences); cleanup F/G/H SURVIVE; `global.teardown.ts:65` `db.restore()` |
| T-379-11 | DoS (flaky/timeout) | mitigate | `exam-taking.spec.ts` — `waitForFunction` (2 occurrences); `waitForTimeout(70_000)` = 0, `waitForTimeout(12_000)` = 0 (verified `379-VERIFICATION.md`); Flow G timer deterministik event-driven |
| T-379-12 | Scope | accept | Bug timer/monitoring → backlog; `379-04-SUMMARY.md` "0 kode produksi" |
| T-379-13 | Tampering | mitigate | `uniqueTitle()` 12 occurrences; cleanup I/J/K SURVIVE; `global.teardown.ts:65` `db.restore()` |
| T-379-14 | Tampering | mitigate | `db.queryScalar()` memanggil `runSqlcmd()` yang memuat guard `Refusing to target non-localhost SQL Server` (`dbSnapshot.ts:40-43`); target hardcode `localhost\SQLEXPRESS` |
| T-379-15 | Scope | accept | Flow K K6 `Score===80` PASS (fix Phase 376 terbukti hidup); bila gagal → backlog, bukan fix inline; `379-RUN-EVIDENCE.md` K6 Status=Completed, Score=80, 122ms |
| T-379-16 | Tampering | mitigate | `docs/SEED_JOURNAL.md` baris 379-03 status=`cleaned`; sqlcmd RESTORE eksplisit ke `HcPortalDB_Dev_pre379_protonseed.bak`; ProtonTrackAssignments Bypass T3 count 1→0 terverifikasi (`379-06-SUMMARY.md`) |
| T-379-17 | Tampering | mitigate | `tests/helpers/dbSnapshot.ts:40-43` guard non-localhost; `restore()` di `dbSnapshot.ts:80-99` strip flag `-d` tapi tetap mewarisi guard via `runSqlcmd()`; SQLCMD_BASE_ARGS `-S localhost\SQLEXPRESS` hardcode |
| T-379-18 | Scope | accept | Bug produksi → backlog/STATE; gate hanya patch test-side minor untuk hijau; `379-06-SUMMARY.md` "0 kode produksi" |

---

## Accepted Risks Log

Ancaman berikut di-*accept* berdasarkan alasan bahwa fase ini adalah **test-infra murni** (0 perubahan runtime produksi):

| Threat ID | Alasan Penerimaan |
|-----------|-------------------|
| T-379-03 | Scope dinyatakan eksplisit dalam setiap PLAN dan VERIFICATION; `git status Controllers/ Views/` kosong sepanjang fase |
| T-379-05 | Identik T-379-03; bug produksi yang tersurface → backlog STATE, tidak difix inline (CONTEXT D) |
| T-379-06 | Akun test (rino/iwan3/hc) bukan rahasia baru; sudah eksis di suite-suite sebelumnya; tidak ada token/password literal sensitif ditambahkan |
| T-379-08 | `@Html.AntiForgeryToken` sudah di-render oleh Razor form; test submit via real HTTP form → token ikut otomatis; bukan serangan surface baru |
| T-379-09 | Drift controller/view Proton adalah teknis yang diketahui; diselesaikan di sisi test (lokalisasi, dropdown kebab); tidak ada kode produksi yang berubah |
| T-379-12 | Drift timer/monitoring diselesaikan test-side (waitForFunction event-driven); tidak ada bug nyata di timer produksi yang terungkap |
| T-379-15 | Flow K K6 lulus (Score=80); jika kelak gagal → surface ke backlog, bukan fix inline |
| T-379-18 | Full suite hijau (75/0/7); satu-satunya penyesuaian = test-side; tidak ada kode produksi |

---

## Unregistered Flags

Tidak ada threat flag baru dari SUMMARY.md yang tidak terpetakan ke threat register. Seluruh deviasi di 6 SUMMARY bersifat test-infra drift (lokalisasi UI, dropdown kebab, helper-completion) dan tidak membuka permukaan serangan baru.

---

## Run Evidence

- Full suite: **75 passed, 7 skipped, 0 failed** (6.4m) — `379-RUN-EVIDENCE.md`
- Helper additive: 0 signature existing diubah — `379-VERIFICATION.md`
- Seed lifecycle: ProtonTrackAssignments Bypass T3 count 1→0 (restored) — `docs/SEED_JOURNAL.md`
- DB guard: `tests/helpers/dbSnapshot.ts:40-43` — "Refusing to target non-localhost SQL Server"
- ASVS Level: L1 (test-infra phase, 0 production code change)
