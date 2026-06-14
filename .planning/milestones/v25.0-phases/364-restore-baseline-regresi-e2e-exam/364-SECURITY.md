---
phase: 364
slug: restore-baseline-regresi-e2e-exam
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-12
---

# Phase 364 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Phase TEST-ONLY (edit `tests/e2e/*.spec.ts`, ZERO produksi). Verifikasi retroaktif (State B) — 9/9 threat closed (6 mitigate grep-verified + 3 accept). Surface = test-infra hygiene; produksi read-only (validator REST-06 tak diubah).

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Playwright runner → app @localhost:5277 | Test-controlled HTTP; no external/untrusted input; local-only | title strings (test) |
| spec → REST-06 validator (POST /Admin/CreateAssessment) | Validator = produksi read-only (TIDAK dimodifikasi) | title strings |
| spec / lifecycle → local SQL `localhost\SQLEXPRESS` | sqlcmd BACKUP/RESTORE + D-11 query, localhost-guarded | integer assessmentId |
| globalTeardown → DB | RESTORE dari snapshot + journal cleaned | — |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation / Evidence | Status |
|-----------|----------|-----------|-------------|------------------------|--------|
| T-364-01 | Tampering | DB snapshot/restore ke server salah | mitigate | `tests/helpers/dbSnapshot.ts` REJECT non-localhost `-S` (guard T-315-01 inherited, baris 2+15) + hardcoded `-S localhost\SQLEXPRESS` (`:23`). 364 reuse lifecycle, NO new DB SQL. | closed |
| T-364-02 | Information Disclosure | test creds di repo (accounts.ts, pwd `123456`) | accept | Local-only test accounts vs local DB, AD-off local-dev; no production secret. Diagnostic run tak nambah apa-apa. | closed |
| T-364-03 | Denial | stale matrix rows (Id 9001-9018) abort run | accept | Setup pre-check fail loud + D-10 confirm journal `cleaned` sebelum run. Operational guard, bukan security. | closed |
| T-364-04 | Tampering | SQL injection via D-11 assertion `Id = ${assessmentId}` | mitigate | `exam-types.spec.ts:206` `assessmentId = parseInt(...)` → integer; `:210` interp integer (bukan string user-controlled); pola sama K5 `Id = ${sessionId}`. | closed |
| T-364-05 | Tampering | query ke non-localhost server | mitigate | Sama T-364-01: `dbSnapshot.runSqlcmd` localhost-guard inherited, reused not extended. | closed |
| T-364-06 | Spoofing | test creds (local, pwd 123456) | accept | Local-dev only vs local DB AD-off; no production secret; unchanged by phase. | closed |
| T-364-07 | Tampering | DB dirty bila teardown skip (residual matrix/Pre Test% rows) | mitigate | globalTeardown selalu jalan (Playwright config) + RESTORE snapshot + Layer-4 validation; Task 2/3 verify journal `cleaned`, no residual `Pre Test%`. | closed |
| T-364-08 | Repudiation | bug produksi nyata disembunyikan fixme tanpa jejak | mitigate | D-06/07/09: tiap fixme bawa reason + backlog 999.x id di SUMMARY; D-09 larang hitung fixme sbg pass. Bukti commit `962694e7`: fixme exam-taking→999.7 + essay→999.8 tercatat jujur. | closed |
| T-364-09 | Information Disclosure | local test creds / AD-off di repo | accept | Local-dev only; no production secret; unchanged by phase. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| R-364-01 | T-364-02 / T-364-06 / T-364-09 | Test credentials lokal (pwd `123456`) hanya vs DB lokal AD-off; bukan secret produksi; tak ter-deploy. | Rino | 2026-06-12 |
| R-364-02 | T-364-03 | Stale matrix pre-check fail loud (operational, bukan security); journal cleaned dijaga D-10. | Rino | 2026-06-12 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-12 | 9 | 9 (6 mitigate-verified + 3 accept) | 0 | Claude (grep-verified; test-only, zero production surface — no auditor) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-12
