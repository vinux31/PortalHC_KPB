---
phase: 399
slug: foundation-junction-userunits-primary-mirror-multi-select-ui-display
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-18
planned_at: 2026-06-18
validated_at: 2026-06-18
---

# Phase 399 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Derived from 399-RESEARCH.md "## Validation Architecture". Per-Task Verification Map filled by planner.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (.NET 8 / EF Core 8) — project `HcPortal.Tests` |
| **Config file** | none (xUnit auto-discover); `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~UserUnit"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **InMemory DB pattern** | `new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())` (WorkerDataServiceSearchTests.cs:21-24) |
| **Real-SQL pattern** | `OrgLabelMigrationFixture` disposable `HcPortalDB_Test_<guid>` localhost\SQLEXPRESS + `MigrateAsync()` (OrgLabelMigrationIntegrationTests.cs:24-48); `[Trait("Category","Integration")]` |
| **E2E** | Playwright (`tests/e2e/*.spec.ts`), login admin lokal, `--workers=1` (reference_local_e2e_sql_env_fix) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + `dotnet test HcPortal.Tests --filter "Category!=Integration"` (logic tests cepat)
- **After every plan wave:** `dotnet test HcPortal.Tests` (full, +Integration bila SQLEXPRESS) + `dotnet run` localhost:5277 + cek DB lokal (CLAUDE.md gate)
- **Before `/gsd-verify-work`:** full suite green + `dotnet build` 0 error + `dotnet ef database update` applied locally + Playwright spec hijau (plan 03/04 UI)
- **Max feedback latency:** < 60s untuk logic suite (InMemory)

---

## Per-Task Verification Map

> EF-InMemory does NOT enforce filtered-unique `(UserId) WHERE IsPrimary=1` — SQL-real
> invariant enforcement deferred to Phase 404 (QA-01). Phase 399 covers write-through mirror
> logic, primary-recompute, audit set-diff, import parse, validation, MU-07 guard via InMemory +
> 1 backfill integration test (SQL-riil).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 399-01-T1 | 01 | 0 | MU-05 | T-399-01-01 | Migration scaffold (CLI/pin/manual) compile | build | `dotnet build HcPortal.csproj` | ✅ | ✅ green |
| 399-01-T2 | 01 | 0 | MU-05 | T-399-01-02/03 | Model + DbSet + filtered-unique config compile | build | `dotnet build HcPortal.csproj` | ✅ Models/UserUnit.cs + ApplicationDbContext.cs | ✅ green |
| 399-01-T4 | 01 | 0 | MU-05 | T-399-01-01/02 | Migration applies + backfill 1 primary-row/pekerja (Unit non-null), idempotent | manual+sql | `dotnet ef database update` + DB count check | ✅ migration fc015f4d applied lokal | ✅ green (DB: 6 UserUnits/6 IsPrimary, 0 mirror-mismatch, IX_UserUnits_UserId_PrimaryUnique live) |
| 399-01-T5 | 01 | 0 | MU-01..07 | — | 7 test scaffold compile + ter-discover (suite existing tak regresi) | unit | `dotnet test HcPortal.Tests --filter "Category!=Integration"` | ✅ 9 files | ✅ green (suite 366/366) |
| 399-02-T1 | 02 | 1 | MU-01, MU-02 | T-399-02-02 | Write-through set+mirror, 1 IsPrimary, recompute promote/null, audit set-diff | unit (InMemory) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~WriteThrough\|FullyQualifiedName~PrimaryMirror\|FullyQualifiedName~AuditDiff"` | ✅ WriteThrough(4)+PrimaryMirror(2)+AuditDiff(2) | ✅ green (8 facts) |
| 399-02-T2 | 02 | 1 | MU-05, MU-07 | T-399-02-01/03/04 | Validasi Unit∈Bagian (unit asing ditolak); MU-07 PTA→block, mapping→confirm→auto-deactivate | unit (InMemory) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~UnitInSectionValidation\|FullyQualifiedName~RemoveUnitGuard"` | ✅ UnitInSectionValidation(3)+RemoveUnitGuard(5) | ✅ green (8 facts) |
| 399-02-T3 | 02 | 1 | MU-04 | T-399-02-06 | Import parse pipe split+trim+dedup first=primary, backward-compat 1-unit | unit (pure-logic) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ImportMultiUnitParse"` | ✅ ImportMultiUnitParse(3) | ✅ green (3 facts) |
| 399-03-T1 | 03 | 2 | MU-01, MU-02 | T-399-03-01 | initSectionUnitMultiCascade ada + fungsi existing utuh | grep | `node -e` shared-cascade.js contains-check | ✅ wwwroot/js/shared-cascade.js | ✅ green |
| 399-03-T2 | 03 | 2 | MU-01, MU-02 | T-399-03-01/02 | Widget DOM + MU-07 modal Razor compile | build | `dotnet build HcPortal.csproj` | ✅ | ✅ green |
| 399-03-T3 | 03 | 2 | MU-01, MU-02 | T-399-03-01 | Spec Playwright widget ditulis (test-first) | grep | `node -e` spec contains-check | ✅ tests/e2e/multiunit-widget-399.spec.ts | ✅ green |
| 399-03-T4 | 03 | 2 | MU-01, MU-02 | T-399-03-01/02 | Runtime widget + round-trip 2-unit + MU-07 modal/block | e2e (Playwright) | `npx playwright test tests/e2e/multiunit-widget-399.spec.ts --workers=1` | ✅ | ✅ green (widget 9/9; MU-07 modal+PROTON-block render = browser UAT deferred) |
| 399-04-T1 | 04 | 2 | MU-03 | T-399-04-01 | VM Profile/Settings/PSign + AccountController populate compile | build | `dotnet build HcPortal.csproj` | ✅ | ✅ green |
| 399-04-T2 | 04 | 2 | MU-03 | T-399-04-03 | Badge display 5 surface (Razor encode) compile | build | `dotnet build HcPortal.csproj` | ✅ | ✅ green |
| 399-04-T3 | 04 | 2 | MU-03 | — | _PSign all-units + spec Playwright display ditulis | grep+build | `node -e` _PSign + spec contains-check | ✅ tests/e2e/multiunit-display-399.spec.ts | ✅ green |
| 399-04-T4 | 04 | 2 | MU-03 | T-399-04-01 | Runtime 7 surface tampil semua unit incl _PSign cetak + Excel | e2e (Playwright) | `npx playwright test tests/e2e/multiunit-display-399.spec.ts --workers=1` | ✅ | ✅ green (display 8/8; _PSign badge visual = browser UAT deferred) |
| (inv #3) | — | 404 | invariant | — | SQL-real filtered-unique enforce (1 IsPrimary/user) — DEFERRED | integration (SQL) | Phase 404 QA-01 | ❌ P404 | ⏭️ deferred |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · ⏭️ deferred*

---

## Wave 0 Requirements

Created in Plan 01 Task 5 (scaffold) → filled/activated in Plan 02 (logic) + plan 03/04 (e2e):

- [x] `HcPortal.Tests/UserUnitsWriteThroughTests.cs` — MU-01/MU-02 write set + mirror (4 facts ✅)
- [x] `HcPortal.Tests/PrimaryMirrorTests.cs` — MU-02 recompute promote/blok/null (2 facts ✅)
- [x] `HcPortal.Tests/UserUnitsAuditDiffTests.cs` — MU-02 set-diff (2 facts ✅)
- [x] `HcPortal.Tests/ImportMultiUnitParseTests.cs` — MU-04 parse + backward-compat (3 facts ✅)
- [x] `HcPortal.Tests/UnitInSectionValidationTests.cs` — MU-05 validasi (3 facts ✅)
- [x] `HcPortal.Tests/RemoveUnitGuardTests.cs` — MU-07 block vs auto-deactivate (5 facts ✅)
- [x] `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs` — MU-05 backfill idempotent (`[Trait("Category","Integration")]`, 3 facts SKIP — SQL-real, verified live via migration apply; manual-only)
- [x] `tests/e2e/multiunit-widget-399.spec.ts` — MU-01/02 widget runtime (9/9 ✅)
- [x] `tests/e2e/multiunit-display-399.spec.ts` — MU-03 display runtime (8/8 ✅)
- Framework install: tidak perlu (xUnit + InMemory + SqlServer + Playwright sudah tersedia).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Migration + backfill di SQLEXPRESS riil | MU-05 | Schema/DB state | `dotnet ef database update` lokal; tiap user Unit-non-null = 1 primary-row, Unit-null = 0 (plan 01 T4) |
| Widget checkbox+radio render + round-trip | MU-01/MU-02 | Razor dinamis (Lesson 354) | `dotnet run` + Playwright + browser sign-off (plan 03 T4) |
| Display semua unit 7 surface incl _PSign cetak + Excel | MU-03 | Render visual + cetak/binary | Browser Profile/WorkerDetail/Settings/ManageWorkers/Excel/Home/_PSign (plan 04 T4) |

---

## Validation Sign-Off

- [x] All tasks have automated verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (build/test/grep tiap task; e2e di checkpoint)
- [x] Wave 0 covers all MISSING references (7 unit/integration + 2 e2e scaffold)
- [x] No watch-mode flags (`--workers=1` Playwright; one-shot dotnet test)
- [x] Feedback latency acceptable (< 60s logic suite)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** validated 2026-06-18 — NYQUIST-COMPLIANT, 0 gaps

---

## Validation Audit 2026-06-18

State A (audit existing plan-time VALIDATION.md vs post-execution reality).

| Metric | Count |
|--------|-------|
| Requirements audited | 6 (MU-01/02/03/04/05/07) |
| Gaps found (MISSING/failing) | 0 |
| Resolved (auditor) | 0 (none needed) |
| Escalated | 0 |

**Findings:**
- All 9 scaffolded test files exist on disk (7 unit/integration + 2 e2e spec).
- Logic suite GREEN: 19 passed / 3 skipped / 0 failed (`--filter UserUnit|PrimaryMirror|ImportMultiUnitParse|UnitInSectionValidation|RemoveUnitGuard`). 19 facts = 4+2+2+3+5+3.
- 3 skipped = `UserUnitsBackfillIntegrationTests` (`[Trait Category=Integration]`, SQL-real) — intentionally skip-gated; behavior verified live (migration `fc015f4d` applied lokal: 6 UserUnits/6 IsPrimary, 0 mirror-mismatch, `IX_UserUnits_UserId_PrimaryUnique` index live). Documented manual-only.
- e2e widget 9/9 + display 8/8 (per 399-VERIFICATION.md).
- Manual-only carry: MU-07 modal coach-mapping + PROTON hard-block render + _PSign badge visual = browser UAT DEFERRED (399-HUMAN-UAT.md partial) — server logic fully unit-covered.
- No auditor spawn (Step 3: no gaps → Step 6). No new test files generated.
- inv #3 (SQL-real filtered-unique runtime enforce) = DEFERRED to Phase 404 QA-01 (EF-InMemory tak enforce filter).
