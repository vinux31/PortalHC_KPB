---
phase: 404-test-sql-riil-uat-docs-invariants
audited: 2026-06-21
asvs_level: L1
threats_total: 9
threats_closed: 9
threats_open: 0
verdict: SECURED
---

# Phase 404 Security Audit

**Phase:** 404 — test-sql-riil-uat-docs-invariants
**Branch:** ITHandoff
**ASVS Level:** L1
**Date:** 2026-06-21

## Scope

Phase 404 is the milestone TEST/UAT/DOCS closer. Plans 01–04 add **zero production surface** (test fixtures + test files + docs + a local browser UAT against the existing app). The single production change in the phase boundary is the **404.1 INT-01 fix** (`ProtonDataController.cs:524`), which *tightens* behavior. Threat register aggregates each plan's `<threat_model>` + the INT-01 fix.

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-404-01-01 | Info Disclosure | accept | CLOSED | `MultiUnitSqlFixture` connstr is localhost `Integrated Security=True` — no secrets, no env vars (identical to 6 existing fixtures). No new disclosure surface. |
| T-404-01-02 | Data hygiene | mitigate | CLOSED | Disposable test DB dropped via `EnsureDeletedAsync` on both success (DisposeAsync) and mid-migration failure (catch). No residual `HcPortalDB_Test_<guid>`. |
| T-404-02-01 | Tampering (data integrity) | mitigate (by test) | CLOSED | `SingleActiveInvariantSqlTests` PROVES existing single-active controls hold on real SQL (the control already exists; the test is regression-proof evidence). No new mutable surface. |
| T-404-02-02 | Test hygiene | accept | CLOSED | Shared fixture DB across Facts — per-Fact unique GUID coachee prevents cross-Fact collision; disposable DB dropped. |
| T-404-03-01 | Tampering/Elevation (data tenancy) | mitigate (by test) | CLOSED | `UnitMembershipInvariantSqlTests` PROVES the existing `AssignmentUnit ∈ UserUnits` tenancy control holds on real SQL; drives existing helper, adds no surface. |
| T-404-03-02 | Info Disclosure (SQLi) | accept | CLOSED | Backfill `ExecuteSqlRawAsync` runs a verbatim copy of the migration's OWN static literal (no user input, no interpolation) — identical to the already-shipped migration. No injection surface. |
| T-404-04-01 | Tampering (local DB integrity) | mitigate | CLOSED | UAT seed lifecycle: `BACKUP` → seed → `RESTORE WITH REPLACE` → SEED_JOURNAL `cleaned`. Baseline verified post-restore (UserUnits=6, temp rows=0). No residual seed; Dev/Prod untouched. |
| T-404-04-02 | Info Disclosure | accept | CLOSED | IT handoff HTML is a static report, no secrets; commit hash is a public ref. Audience = IT. |
| T-404.1-01 | Tampering / Info-misattribution (PSU-01/05) | mitigate | CLOSED | **INT-01 fix** (`ProtonDataController.cs:524`, commit `08615ca5`): removed the `?? User.Unit` fallback in the Phase-129 deliverable auto-sync. A coachee with empty/orphaned `AssignmentUnit` is now **skipped + audit-warned** (`_logger.LogWarning`) instead of silently scoping a new deliverable to the primary unit. This *tightens* data-correctness (no cross-unit misattribution) and adds an audit signal — strictly a security-positive change. Full suite re-run green (562/0/2). |

## Accepted Risks Log

| Risk ID | Threat ID | Basis |
|---------|-----------|-------|
| AR-404-01 | T-404-01-01 / T-404-04-02 | Localhost-only fixture connstr + static IT-handoff report — no secrets, no new disclosure surface (consistent with prior phases). |
| AR-404-02 | INT-02 | 2 HTTP gate-path smoke tests remain `[Fact(Skip)]` — NOT a security gap; the gate behavior is covered at helper-level + grep guard + browser UAT. Deferred to backlog. |

## Notes

- No new endpoints, inputs, controllers, or services were added by Phase 404 plans. The only production code change in the phase boundary (INT-01 / 404.1) reduces an existing data-misattribution path.
- All authz/CSRF surfaces from prior phases (399–403) remain unchanged (confirmed by the milestone integration audit: 0 unprotected routes).

## Security Audit 2026-06-21
| Metric | Count |
|--------|-------|
| Threats found | 9 |
| Closed | 9 |
| Open | 0 |
