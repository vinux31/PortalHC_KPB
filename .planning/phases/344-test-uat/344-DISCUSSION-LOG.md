# Phase 344: Test + UAT - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-04
**Phase:** 344-test-uat
**Areas discussed:** Unit Test Scope, Integration Test, Playwright E2E, Manual UAT, Test DB, UAT Mapping

---

## Unit Test Scope (TEST-01..04)

| Option | Description | Selected |
|--------|-------------|----------|
| Isi gap saja | Pertahankan 23 test existing, tambah hanya yang bolong (permission 403, DFS, dup-name) | ✓ |
| Audit + rewrite menyeluruh | Review + konsolidasi semua, risiko ganggu test hijau | |

**User's choice:** Isi gap saja
**Notes:** Planner wajib cek OrganizationControllerTests.cs sebelum nulis test baru agar tidak duplikat.

---

## Integration Test (TEST-05)

| Option | Description | Selected |
|--------|-------------|----------|
| SQL Server LocalDB fresh | Migrate + seed + assert ke DB asli | ✓ |
| EF InMemory seed+read | Konsisten repo tapi tidak validasi migration asli | |
| Dokumentasi manual | Skip automated, dokumentasi manual | |

**User's choice:** SQL Server LocalDB fresh
**Notes:** EF InMemory existing tidak exercise migration SQL asli.

---

## Playwright E2E (TEST-06)

| Option | Description | Selected |
|--------|-------------|----------|
| Spec baru + global.setup existing | File manage-org-label.spec.ts, pola existing | ✓ |
| Extend spec existing | Tambah ke spec lain, campur domain | |

**User's choice:** Spec baru + global.setup existing

---

## Manual UAT vs Automation

| Option | Description | Selected |
|--------|-------------|----------|
| HUMAN-UAT.md checklist | Doc 5 scenario + 5 regresi manual penuh | |
| Maksimalkan Playwright, UAT tipis | Otomasi sebanyak mungkin, manual minimal | ✓ |

**User's choice:** Maksimalkan Playwright, UAT tipis

---

## Test DB (follow-up Integration)

| Option | Description | Selected |
|--------|-------------|----------|
| DB test terpisah disposable | HcPortalDB_Test / LocalDB, drop per run | ✓ |
| Reuse HcPortalDB_Dev + snapshot/restore | Pakai DB dev + BACKUP/RESTORE | |

**User's choice:** DB test terpisah disposable
**Notes:** Hindari snapshot/restore SEED_WORKFLOW via isolasi DB terpisah.

---

## UAT Mapping (follow-up Manual UAT)

| Option | Description | Selected |
|--------|-------------|----------|
| Otomasi 4, manual 1 visual | Playwright 4 scenario, manual: cascade count + 5 regresi | ✓ |
| Otomasi semua 5 + regresi | Push semua ke Playwright | |

**User's choice:** Otomasi 4, manual 1 visual

## Claude's Discretion

- Penamaan/struktur test, fixture DB test disposable, detail seed SQL Playwright.

## Deferred Ideas

None.
