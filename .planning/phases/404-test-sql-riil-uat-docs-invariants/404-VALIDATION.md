---
phase: 404
slug: test-sql-riil-uat-docs-invariants
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-21
---

# Phase 404 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8.0.418) + EF SqlServer 8.0.0 (SQL-real) + Playwright (UI/UAT) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (mode SqlServer via `MultiUnitSqlFixture`) |
| **Quick run command** | `dotnet test --no-build --filter "FullyQualifiedName~MultiUnitSql"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | filtered ~10-30s (MigrateAsync sekali) · full ~105-120s |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --no-build --filter "FullyQualifiedName~MultiUnitSql"`
- **After every plan wave:** Run `dotnet test` (full suite — pastikan tak regresi baseline ~547/0/6)
- **Before `/gsd-verify-work`:** Full suite green + `dotnet build` 0 error
- **Max feedback latency:** ~120 seconds

---

## Per-Task Verification Map

> Phase 404 = TEST/UAT/DOCS closer. "Secure Behavior" di sini = invariant DB yang dibuktikan, bukan mitigasi baru (NOL kode produksi).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 404-01-xx | 01 | 1 | QA-01 | — | Fixture SQL-real {X,Y} 1 Bagian + coach cross-unit + PROTON T1@X→T2@Y via `Migrate()` (incl 399 AddUserUnits) | integration | `dotnet test --filter ~MultiUnitSql` | ❌ W0 | ⬜ pending |
| 404-02-xx | 02 | 2 | QA-03 | — | single-active: CoachCoacheeMapping via `DbUpdateException` (filtered-unique `:333-336`); **PTA via `Count(IsActive)==1`** (app-level, no filtered-unique `:393`) + Reactivate/Import-reactivate | integration | `dotnet test --filter ~MultiUnitSql` | ❌ W0 | ⬜ pending |
| 404-03-xx | 03 | 2 | QA-04 | — | `AssignmentUnit ∈ coachee.UserUnits` tiap write-path + B-06 anti-dobel `ProtonDeliverableBootstrap` lintas-unit + `ProtonKompetensi.Unit` 1:1 (`:429`) | integration | `dotnet test --filter ~MultiUnitSql` | ❌ W0 | ⬜ pending |
| 404-04-xx | 04 | 3 | QA-02 | — | UAT browser PROTON sekuensial cross-unit @5270 + cert histori per-unit + coach multi-unit view; docs D1=b + HTML handoff IT | manual + docs | live UAT + `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/MultiUnitSqlFixture.cs` — shared `IClassFixture` SQL-real (salin pola `OrgLabelMigrationFixture`: DB disposable `HcPortalDB_Test_<guid>@localhost\SQLEXPRESS` + `await ctx.Database.MigrateAsync()` + `EnsureDeletedAsync()` teardown) + `SeedCanonicalAsync` (dataset {X,Y}+coach+PROTON).
- [ ] Implement stub existing `HcPortal.Tests/CrossUnitAssignTests.cs:105 SingleActive_invariant_is_sql_real_phase404()` (jangan biarkan body kosong — carry 402).
- [ ] (Rekomendasi riset) Implement 3 stub `[Skip]` di `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs:72-91` (fixture sudah siap; menguatkan bukti migration 399) — planner putuskan masuk scope atau backlog.

*Catatan: harness SQL-real sudah matang (6+ fixture precedent) — Wave 0 = tambah 1 fixture multi-unit, bukan install framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| PROTON sekuensial cross-unit T1@X→T2@Y end-to-end + cert histori per-unit utuh + coach multi-unit lihat/export coachee lintas-unit | QA-02 | Live browser UAT per CLAUDE.md Develop Workflow (alur paling berisiko, butuh mata manusia) | `dotnet run` @ localhost:5270 (branch ITHandoff, AD off) + seed temporary local-only (snapshot→insert→restore, SEED_JOURNAL cleaned) + DB check |
| Docs: batasan D1=b + HTML handoff IT (migration=TRUE Phase 399 + commit hash) | QA-02 | Artefak dokumentasi (review manusia) | Render HTML + cek isi vs D-13/D-14 CONTEXT |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify (QA-01/03/04 SQL-real xUnit) or manual-only (QA-02 UAT/docs) coverage
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (MultiUnitSqlFixture + stub CrossUnitAssignTests:105)
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
