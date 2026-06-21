---
phase: 404-test-sql-riil-uat-docs-invariants
verified: 2026-06-21
status: passed
requirements_total: 4
requirements_satisfied: 4
method: goal-backward (test execution + browser UAT + artifact inspection)
---

# Phase 404 Verification — Test (SQL Riil) + UAT + Docs + Invariants

**Status:** passed
**Verified:** 2026-06-21
**Method:** Goal-backward — confirmed the phase delivered SQL-real proof of the multi-unit invariants + UAT + docs, not just that tasks ran.

## Phase Goal

Prove the full multi-unit + coaching cross-unit + PROTON sequential capability is correct on **SQL riil** (not EF-InMemory, which does not enforce filtered-unique indexes), invariants single-active + `AssignmentUnit ∈ UserUnits` + B-06 anti-dobel preserved, with local UAT passing and the D1=b limitation documented.

## Requirements Coverage

| Requirement | Status | Evidence |
|-------------|--------|----------|
| QA-01 (SQL-real fixture {X,Y}+coach+PROTON via Migrate incl 399) | ✅ satisfied | `HcPortal.Tests/MultiUnitSqlFixture.cs` — disposable `HcPortalDB_Test_<guid>` migrated via `MigrateAsync` (full chain incl 399 `AddUserUnitsTable` + filtered-unique IX) + canonical seed. Backfill idempotency proven by `UserUnitsBackfillIntegrationTests` (3 Facts, verbatim migration SQL). Green. |
| QA-02 (UAT @5270 + D1=b docs + IT handoff) | ✅ satisfied | Browser UAT 3/3 PASS @localhost:5270 (Playwright): (1) Iwan multi-unit display "Alkylation (Utama)"+"RFCC NHT"; (2) coach Rustam cross-unit coachees Iwan@RFCC NHT + Rino@Alkylation; (3) PROTON sequential active_PTA=1/total=2/cert_PFA=1. Seed snapshot→restore, SEED_JOURNAL cleaned. Docs: `docs/milestone-v32.3/index.html` (migration=TRUE Fase 399) + `docs/milestone-v32.3-batasan-d1b.md`. |
| QA-03 (single-active invariants SQL-real, all write-paths + reactivate) | ✅ satisfied | `SingleActiveInvariantSqlTests` 3 Facts green — mapping 2nd-active → `DbUpdateException` (filtered-unique, R-2 covers Assign/Edit/Import/Reactivate) + reactivate-without-deactivate replication + PTA single-active by COUNT after REAL `ProtonBypassService` T1@X→T2@Y (Pitfall #1) + cert histori co-exist. Plus `CrossUnitAssignSqlTests` anchor (402 carry) green. |
| QA-04 (`AssignmentUnit∈UserUnits` + B-06 + 1:1 invariants) | ✅ satisfied | `UnitMembershipInvariantSqlTests` 4 Facts green — `ValidateAssignmentUnitInUserUnits` (production helper) accept member/reject non-member/blank + B-06 cross-unit no-skip via production `ProtonDeliverableBootstrap` + 1:1 dup-progress `DbUpdateException` + one-primary `DbUpdateException`. |

## Test Evidence

- **Full suite:** 562 passed / 0 failed / 2 skipped (2m). Skip count dropped 6→2 (4 stubs implemented this phase; remaining 2 = pre-existing HTTP smoke stubs, see note).
- **Build:** 0 errors (28 warnings baseline; main app build 0 errors / 24 warnings baseline).
- **Boundary (R-1):** NOL kode produksi within the phase plans — only test files + docs.

## Goal Achievement Assessment

The phase goal IS achieved: every milestone invariant that EF-InMemory could not enforce (filtered-unique single-active mapping, one-primary UserUnits, ProtonKompetensi/deliverable 1:1) is now proven against real SQL Server, the membership/B-06/no-clobber controls are exercised via production helpers, the riskiest end-to-end flow is human-verified in the browser, and the IT deploy artifacts (migration=TRUE handoff + D1=b limitation) exist.

## Carry / Notes

- **INT-01 (PSU-01/05)** — surfaced by the milestone integration audit: `ProtonDataController.cs:524` Phase-129 auto-sync retained a `?? User.Unit` fallback (escaped Phase 401 scope). **Fixed under 404.1** (commit `08615ca5`: drop fallback → skip + audit-warn on empty AssignmentUnit). Full suite re-run green (562/0/2).
- **INT-02 (low)** — 2 HTTP gate-path smoke stubs (`ProtonUnitResolveTests:70`, `UnitUnresolvedAuditTests:73`) remain `[Fact(Skip)]` (the suite's 2 skips). Gate behavior covered at helper-level + grep guard + browser UAT; HTTP-action smoke deferred to backlog (accepted — not a functional gap).

---
*Phase: 404-test-sql-riil-uat-docs-invariants*
*Verified: 2026-06-21 — passed*
