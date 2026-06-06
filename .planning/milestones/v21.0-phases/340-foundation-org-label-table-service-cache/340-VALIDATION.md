---
phase: 340
slug: foundation-org-label-table-service-cache
status: approved
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-03
---

# Phase 340 — Validation Strategy

> Per-phase validation contract for Phase 340 (Foundation — Tabel + Service + Cache). Reconstructed from SUMMARY artifacts post-execution + Nyquist gap audit 2026-06-03.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelServiceTests"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **Estimated runtime** | ~1 second (quick) / ~3 seconds (full suite, 31 tests) |
| **InMemory provider** | Microsoft.EntityFrameworkCore.InMemory 8.0.0 |
| **Test isolation** | per-`[Fact]` `Guid.NewGuid()` InMemory DB (no cross-test state) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelServiceTests"` (~1s)
- **After every plan wave:** Run `dotnet test HcPortal.Tests` (~3s)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~3 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 340-01-T1 | 01 | 1 | ORG-LABEL-01 | — | Entity class compiles | build | `dotnet build` | ✅ | ✅ green |
| 340-01-T2 | 01 | 1 | ORG-LABEL-01 | T-340-11 | Migration adds table + unique index | manual SQL | `sqlcmd … SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC` | ✅ | ✅ green (MANUAL) |
| 340-01-T3 | 01 | 1 | ORG-LABEL-01 | — | Seed 3 default rows idempotent | manual SQL | `sqlcmd … SELECT Level, Label FROM OrganizationLevelLabels ORDER BY Level` | ✅ | ✅ green (MANUAL) |
| 340-02-T1 | 02 | 2 | ORG-LABEL-02 | — | Interface 7 method signatures | build | `dotnet build` | ✅ | ✅ green |
| 340-02-T2a | 02 | 2 | ORG-LABEL-02a (GetLabel happy) | — | Resolves configured labels | unit | `dotnet test --filter "GetLabel_KnownLevel_ReturnsConfiguredLabel"` | ✅ | ✅ green |
| 340-02-T2b | 02 | 2 | ORG-LABEL-02b (GetLabel fallback) | — | Fallback `"Level {N}"` D-07 | unit | `dotnet test --filter "GetLabel_UnknownLevel_ReturnsFallback"` | ✅ | ✅ green |
| 340-02-T2c | 02 | 2 | ORG-LABEL-02c (GetAll) | — | Returns 3 sorted entries | unit | `dotnet test --filter "GetAll_Returns3SortedEntries"` | ✅ | ✅ green |
| 340-02-T2d | 02 | 2 | ORG-LABEL-02d (UpdateAsync) | T-340-07 | Mutate + cache invalidate + audit log | unit | `dotnet test --filter "UpdateAsync_KnownLevel_UpdatesRowAndInvalidatesCacheAndLogsAudit"` | ✅ | ✅ green |
| 340-02-T2d-neg | 02 | 2 | ORG-LABEL-02d guard | — | Throws on unknown level | unit | `dotnet test --filter "UpdateAsync_UnknownLevel_Throws"` | ✅ | ✅ green |
| 340-02-T2e | 02 | 2 | ORG-LABEL-02e (AddAsync) | T-340-07 | Mutate + cache invalidate + audit log | unit | `dotnet test --filter "AddAsync_NewLevel_InsertsAndLogs"` | ✅ | ✅ green |
| 340-02-T2e-neg | 02 | 2 | ORG-LABEL-02e guard | — | Throws on existing level | unit | `dotnet test --filter "AddAsync_ExistingLevel_Throws"` | ✅ | ✅ green |
| 340-02-T2f | 02 | 2 | ORG-LABEL-02f (DeleteAsync) | T-340-07 | Mutate + cache invalidate + audit log | unit | `dotnet test --filter "DeleteAsync_KnownLevel_RemovesAndLogs"` | ✅ | ✅ green |
| 340-02-T2f-neg | 02 | 2 | ORG-LABEL-02f guard | — | Throws on unknown level | unit | `dotnet test --filter "DeleteAsync_UnknownLevel_Throws"` | ✅ | ✅ green |
| 340-02-T2-DI | 02 | 2 | ORG-LABEL-02 (Scoped DI) | T-340-06 | Startup DI scope validation PASS | manual | `dotnet run` → "Now listening" without captive-dep error | ✅ | ✅ green (MANUAL) |
| 340-02-T2-endpoint | 02 | 2 | ORG-LABEL-03 | T-340-05 | GET returns 200 + JSON dict, auth required | manual curl | `curl -b cookies.txt http://localhost:5279/Admin/GetLevelLabels` → `{"0":"Bagian",…}` | ✅ | ✅ green (MANUAL) |
| 340-02-T2-max-cfg | 02 | 2 | ORG-LABEL-07a (GetMaxConfiguredLevel) | — | Returns MAX(Level) from cache, 0 when empty | unit | `dotnet test --filter "GetMaxConfiguredLevel_With3Rows_Returns2"` + `_Empty_Returns0` | ✅ | ✅ green |
| 340-02-T2-max-used | 02 | 2 | ORG-LABEL-07b (GetMaxUsedLevelAsync) | — | Live query MAX(OrganizationUnits.Level), 0 when empty | unit | `dotnet test --filter "GetMaxUsedLevelAsync_WithUnits_ReturnsMax"` + `_Empty_Returns0` | ✅ | ✅ green |
| 340-03-T1 | 03 | 3 | ORG-LABEL-07 (TEST-01) | T-340-10 | xUnit 2 [Fact] PASS, no cross-test contamination | unit | `dotnet test --filter "OrgLabelServiceTests"` → 13/13 PASS | ✅ | ✅ green |
| 340-03-T2 | 03 | 3 | ORG-LABEL-07c (IT handoff) | T-340-11 | HTML doc generated, all placeholders filled, Cilacap SOP backup section preserved | manual visual + grep | open `docs/DB_HANDOFF_IT_2026-06-03.html` in browser + grep `BACKUP DATABASE` ≥1 + zero `{PLACEHOLDER}` markers | ✅ | ✅ green (MANUAL) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 already in `HcPortal.Tests.csproj` from prior phases. Microsoft.EntityFrameworkCore.InMemory 8.0.0 added during Plan 03 Task 1 (commit `43e94655`).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Migration applied to live SQL Server | ORG-LABEL-01 / 340-01-T2 | Integration with live SqlServer not feasible via InMemory provider; sqlcmd is canonical | `sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -Q "SELECT TOP 1 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"` → expect `20260603012335_AddOrganizationLevelLabel` |
| Seed 3 rows inserted at startup | ORG-LABEL-01 / 340-01-T3 | Startup hook side-effect against live DB | `sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -Q "SELECT Level, Label FROM OrganizationLevelLabels ORDER BY Level"` → expect 3 rows (0 Bagian, 1 Unit, 2 Sub-unit) |
| DI scope validation passes at startup | ORG-LABEL-02 / T-340-06 | EnableValidateOnBuild validates at host build; no isolated unit harness | `ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build --no-launch-profile --urls=http://localhost:5279` — confirm log shows `Now listening on http://localhost:5279` without `Cannot consume scoped service 'ApplicationDbContext'` |
| `GET /Admin/GetLevelLabels` 200 + JSON dict | ORG-LABEL-03 / T-340-05 | WebApplicationFactory + cookie auth Identity test harness not present in repo; adding requires Microsoft.AspNetCore.Mvc.Testing package + new fixture; live curl already verified | (1) Start app Dev mode (see above) (2) `TOKEN=$(curl -s -c c.txt http://localhost:5279/Account/Login \| grep -oP 'name="__RequestVerificationToken"[^>]*value="\K[^"]+' \| head -1)` (3) `curl -s -b c.txt -c c.txt -L -X POST http://localhost:5279/Account/Login -d "Email=admin@pertamina.com&Password=123456&__RequestVerificationToken=$TOKEN"` (4) `curl -b c.txt -i http://localhost:5279/Admin/GetLevelLabels` → expect `HTTP/1.1 200 OK` + body `{"0":"Bagian","1":"Unit","2":"Sub-unit"}` |
| IT handoff HTML doc renders correctly | ORG-LABEL-07c / T-340-11 | HTML visual rendering + brand-color preservation is visual QA, no automated assertion | Open `docs/DB_HANDOFF_IT_2026-06-03.html` in browser, confirm Pertamina red `#e30613` header, 950px container, all 8 sections render. Grep guard: `grep -c "BACKUP DATABASE" docs/DB_HANDOFF_IT_2026-06-03.html` ≥1 (Cilacap SOP preserved). |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or documented Manual-Only justification
- [x] Sampling continuity: no 3 consecutive automated-eligible tasks without automated verify (13 [Fact] cover service surface)
- [x] Wave 0 covers all MISSING references (none — InMemory installed Plan 03 T1)
- [x] No watch-mode flags
- [x] Feedback latency < 5s (full suite ~3s)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-03 (Nyquist audit reconstruction State B + gap fill).

---

## Validation Audit 2026-06-03

| Metric | Count |
|--------|-------|
| Gaps found | 9 (G1–G9) |
| Resolved (auto tests added) | 6 (G1–G6, 11 new [Fact]) |
| Escalated to MANUAL | 3 (G7 DB state, G8 endpoint integration, G9 HTML render) |
| Tests added | 11 new [Fact] in `HcPortal.Tests/OrgLabelServiceTests.cs` |
| Total OrgLabelServiceTests | 13 (2 baseline TEST-01 + 11 G1–G6) |
| Full suite tally | 31/31 PASS (18 FileUploadHelperTests + 13 OrgLabelServiceTests) |
| Commits | `43e94655` (TEST-01), `06582a9b` (Nyquist fill G1–G6) |
| Pre-existing build warnings delta | 0 new (21 pre-existing preserved) |

Escalation rationale for MANUAL items:
- **G7/G8** — adding `Microsoft.AspNetCore.Mvc.Testing` WebApplicationFactory harness + Identity cookie auth setup is out-of-proportion for one trivial endpoint (Json dict return). Live curl + sqlcmd canonical for current phase scope. Defer integration harness to future cross-phase test infra initiative (not Phase 344-specific).
- **G9** — HTML visual rendering does not benefit from automated assertion; grep guard already enforces structural content (`BACKUP DATABASE`, brand color, required strings).

D-10 D-decision (mutation tests deferred Phase 344) is now OBSOLETE for Phase 340 boundary — G2/G3/G4 mutation tests filled here. Phase 344 scope adjusts accordingly (carry-over: cross-phase integration test harness, if any).
