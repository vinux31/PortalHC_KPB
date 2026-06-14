---
phase: 375
slug: test-uat
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-14
---

# Phase 375 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

Phase 375 = **test/UAT-only**. No production code, endpoints, schema, or migration added (verified: 0 files under `Migrations/` touched across the 6 phase commits, V7 audit). All three threats are Information-Disclosure of *temporary local-only test/seed data*, contained to `HcPortalDB_Dev` on `localhost`. No new trust boundary, credential, or PII introduced.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| (none new) | Test/UAT exercises existing UI + in-memory unit engine. xUnit, Playwright e2e, and manual exam-diff all target `localhost\SQLEXPRESS / HcPortalDB_Dev` only. | Temporary seed/test rows (assessment, packages, 2 peserta assignment) — no production data, no PII beyond existing local dev accounts. |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-375-01 | I (Info Disclosure) | xUnit fixtures (real-SQL `ProtonCompletionFixture`) | accept | Disposable per-test DB fixture (existing pattern); never touches prod/Dev DB (CLAUDE.md § Develop Workflow); no new credential/PII written. Documented accepted risk. | closed |
| T-375-02 | I (Info Disclosure) | Playwright e2e seed data in local DB (`tests/e2e/shuffle.spec.ts`) | mitigate | `dbSnapshot.ts:33-34` throws `Refusing to target non-localhost SQL Server` (hostname guard); `shuffle.spec.ts:93-98` `beforeAll` BACKUP `HcPortalDB_Dev` → `afterAll` RESTORE → seed does not persist. localhost-only. | closed |
| T-375-03 | I (Info Disclosure) | Manual exam-diff seed (assessment + 2 peserta) in local DB | mitigate | Snapshot pre-UAT (`HcPortalDB_Dev_pre375uat_20260614T003317.bak`) → RESTORE after exam-diff (D-04, success-OR-fail) → DB back to baseline (matrix_sessions=0, pkg9999=0, 58 sessions). `docs/SEED_JOURNAL.md` entry 375 = `cleaned` (lifecycle active→cleaned, verified V7). sqlcmd localhost-only; no Dev/Prod edit (CLAUDE.md). | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-375-01 | T-375-01 | xUnit real-SQL fixtures (`ProtonCompletionFixture`) use a disposable per-test local DB following the existing test pattern; they never read/write prod or Dev DB and leave no new credential/PII. Residual risk is local-only test data, accepted. | Rino (developer) | 2026-06-14 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-14 | 3 | 3 | 0 | Claude (gsd-secure-phase, evidence-direct: threats_open=0 → no auditor spawn per workflow Step 3) |
| 2026-06-14 (re-audit) | 3 | 3 | 0 | Claude (gsd-secure-phase re-run, State A): mitigasi unchanged — `dbSnapshot.ts` non-localhost guard present, `shuffle.spec.ts` 5 beforeAll/afterAll BACKUP/RESTORE, SEED_JOURNAL 375 still `cleaned` (matrix=0, 58 baseline). No drift. |

Verification evidence:
- T-375-02 mitigation present in source: `tests/helpers/dbSnapshot.ts` localhost-guard (throw on non-localhost `-S` host) + `tests/e2e/shuffle.spec.ts` BACKUP/RESTORE in `beforeAll`/`afterAll`.
- T-375-03 mitigation executed: pre-UAT snapshot + RESTORE; `docs/SEED_JOURNAL.md` Phase 375 status `cleaned` (cross-checked by adversarial verification workflow V7 — seed not left in DB, 0 new migration).
- T-375-01 accepted risk documented above (AR-375-01).

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-14
