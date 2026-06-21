---
phase: 404
slug: test-sql-riil-uat-docs-invariants
status: passed
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-21
finalized: 2026-06-21
---

# Phase 404 — Validation Strategy (FINALIZED)

> Per-phase validation contract. Finalized 2026-06-21 after execution: all SQL-real xUnit Facts green + UAT 3/3 PASS.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8.0) + EF SqlServer 8.0.0 (SQL-real) + Playwright (UI/UAT) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (SqlServer via `MultiUnitSqlFixture`) |
| **Quick run command** | `dotnet test --no-build --filter "FullyQualifiedName~MultiUnitSql"` |
| **Full suite command** | `dotnet test` → **562 passed / 0 failed / 2 skipped** (2m) |
| **Estimated runtime** | filtered ~10-30s (MigrateAsync once) · full ~120s |

---

## Per-Task Verification Map

> Phase 404 = TEST/UAT/DOCS closer. "Secure Behavior" = invariant DB proven, not a new mitigation (NOL kode produksi; the lone INT-01 fix is tracked as 404.1).

| Task ID | Plan | Wave | Requirement | Secure Behavior | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------------|-----------|-------------------|--------|
| 404-01 | 01 | 1 | QA-01/03 | `MultiUnitSqlFixture` SQL-real {X,Y}+coach+PROTON via `MigrateAsync` (incl 399) + single-active anchor (402 carry) | integration | `dotnet test --filter ~CrossUnitAssignSqlTests` | ✅ green (1/1) |
| 404-02 | 02 | 2 | QA-03 | single-active: mapping `DbUpdateException` (filtered-unique) + reactivate replication + PTA `Count(IsActive)==1` via real bypass | integration | `dotnet test --filter ~SingleActiveInvariantSqlTests` | ✅ green (3/3) |
| 404-03 | 03 | 2 | QA-01/04 | `AssignmentUnit∈UserUnits` + B-06 cross-unit no-skip + 1:1 + one-primary; 3 backfill stubs (verbatim migration SQL) | integration | `dotnet test --filter ~UnitMembershipInvariantSqlTests` + `~UserUnitsBackfillIntegrationTests` | ✅ green (4/4 + 3/3) |
| 404-04 | 04 | 3 | QA-02 | UAT browser PROTON sekuensial cross-unit @5270 + cert histori + coach multi-unit view; docs D1=b + HTML handoff IT | manual + docs | live UAT 3/3 PASS + `dotnet build` | ✅ green (UAT 3/3) |

---

## Wave 0 Requirements (COMPLETE)

- [x] `HcPortal.Tests/MultiUnitSqlFixture.cs` — shared SQL-real `IClassFixture` (`MigrateAsync` full chain + `SeedCanonicalAsync`).
- [x] Implement `CrossUnitAssignTests` single-active anchor `SingleActive_invariant_is_sql_real_phase404` (402 carry closed; `[Skip]` removed).
- [x] Implement 3 `[Skip]` stubs in `UserUnitsBackfillIntegrationTests` (migration-399 backfill, verbatim SQL).

---

## Manual-Only Verifications (DONE)

| Behavior | Requirement | Result |
|----------|-------------|--------|
| PROTON sekuensial cross-unit T1@X→T2@Y + cert histori per-unit + coach multi-unit view/export | QA-02 | ✅ UAT 3/3 PASS @5270 (Playwright; active_PTA=1/total=2/cert_PFA=1; SEED_JOURNAL cleaned) |
| Docs: batasan D1=b + HTML handoff IT (migration=TRUE Phase 399 + commit-hash placeholder) | QA-02 | ✅ `docs/milestone-v32.3/index.html` + `docs/milestone-v32.3-batasan-d1b.md` |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify (QA-01/03/04 SQL-real xUnit) or manual-only (QA-02 UAT/docs) coverage
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (MultiUnitSqlFixture + anchor)
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved (2026-06-21) — full suite 562/0/2, UAT 3/3 PASS, skip count dropped 6→2 (4 stubs live).
