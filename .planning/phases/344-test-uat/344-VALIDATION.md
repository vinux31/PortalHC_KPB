---
phase: 344
slug: test-uat
status: finalized
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-04
updated: 2026-06-04
---

# Phase 344 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Phase 344 IS the test/UAT phase — its deliverables ARE tests. Validation here = the tests themselves run green + UAT signed off.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests, EF Core 8) + Playwright (tests/e2e, TypeScript) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/playwright.config.ts |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test HcPortal.Tests` + (app on :5277) `cd tests && npx playwright test e2e/manage-org-label.spec.ts` |
| **Estimated runtime** | xUnit ~2-5s (unit) / +real-DB integration ~10-30s · Playwright ~30-90s |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests --filter "Category!=Integration"`
- **After every plan wave:** Run full `dotnet test HcPortal.Tests`
- **Before `/gsd-verify-work`:** Full xUnit green + Playwright spec green at localhost:5277 + 344-HUMAN-UAT.md signed off
- **Max feedback latency:** ~5s (unit) / ~90s (E2E)

---

## Per-Task Verification Map

> Finalized by gsd-planner 2026-06-04. All plans assigned Wave 1 (no file overlaps → parallel).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 344-01-T1 | 01 | 1 | TEST-03 | — | pre-order DFS order correct (helper) | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~PreOrder"` | ❌ W0 (Helpers/OrgTreePreOrder.cs + [Fact]) | ⬜ pending |
| 344-01-T2 | 01 | 1 | TEST-02, TEST-01, TEST-04 | T-344-01 | non-Admin/non-HC contract asserted; TEST-01/04 verify-only | unit (reflection) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelControllerTests"` | ❌ W0 (+5 attr [Fact]); ✅ TEST-01/04 exist | ⬜ pending |
| 344-02-T1 | 02 | 1 | TEST-05 | T-344-02 | provider for real-DB test | config | `dotnet build HcPortal.Tests` | ❌ W0 (csproj +SqlServer 8.0.0) | ⬜ pending |
| 344-02-T2 | 02 | 1 | TEST-05 | T-344-02 | migration apply + seed + first read on real SQL | integration | `dotnet test HcPortal.Tests --filter "Category=Integration"` | ❌ W0 (OrgLabelMigrationIntegrationTests.cs + fixture) | ⬜ pending |
| 344-03-T1 | 03 | 1 | TEST-06, TEST-02 | T-344-01, T-344-03 | 5 E2E scenarios + live 403 (coach) | e2e | (app:5277) `cd tests && npx playwright test e2e/manage-org-label.spec.ts --reporter=line` | ❌ W0 (manage-org-label.spec.ts) | ⬜ pending |
| 344-03-T2 | 03 | 1 | ORG-INTEG-03 | — | thin manual UAT doc authored | doc | `test -f 344-HUMAN-UAT.md && grep -c SMOKE >= 4` | ❌ W0 (344-HUMAN-UAT.md) | ⬜ pending |
| 344-03-T3 | 03 | 1 | ORG-INTEG-03 | T-344-03 | cascade count visual + 4 regression smoke parity | manual (checkpoint) | 344-HUMAN-UAT.md checklist at :5277 (human) | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Verify-only (no Wave 0 work — D-01):**
- TEST-01: `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelServiceTests"` — 13 existing [Fact], unchanged.
- TEST-04: `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"` — dup-name per-parent [Fact] already present.

---

## Requirement → Plan Coverage (all 7 IDs mapped)

| Requirement | Plan(s) | How Covered |
|-------------|---------|-------------|
| TEST-01 | 01 (verify-only) | Existing OrgLabelServiceTests GetLabel happy+fallback — run & confirm green, no new test (D-01). |
| TEST-02 | 01 (unit reflection) + 03 (e2e 403) | 5 attribute [Fact] (contract) + coach→/Admin/ManageOrgLevelLabels redirect/403 (live pipeline). |
| TEST-03 | 01 | New pure C# helper OrgTreePreOrder.BuildPreOrder + deterministic 3-level/multi-root [Fact] (D-05). |
| TEST-04 | 01 (verify-only) | Existing OrganizationControllerTests dup-name per-parent — confirm green, no new test (D-01). |
| TEST-05 | 02 | Disposable real-SQL-Server fixture: MigrateAsync + SeedData seed + service first read (D-02). |
| TEST-06 | 03 | New manage-org-label.spec.ts, 5 scenarios at :5277 (D-03). |
| ORG-INTEG-03 | 03 | 4 automated Playwright scenarios + 1 cascade-count visual + 4 regression smoke in 344-HUMAN-UAT.md (D-04). |

---

## Wave 0 Requirements

- [ ] `Helpers/OrgTreePreOrder.cs` (new production helper) — TEST-03 (Plan 01 T1)
- [ ] `HcPortal.Tests/OrganizationControllerTests.cs` — add pre-order [Fact] (Plan 01 T1)
- [ ] `HcPortal.Tests/OrgLabelControllerTests.cs` — add 5 reflection attribute [Fact] (Plan 01 T2)
- [ ] `HcPortal.Tests/HcPortal.Tests.csproj` — add Microsoft.EntityFrameworkCore.SqlServer 8.0.0 (Plan 02 T1)
- [ ] `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` (new) — real-DB fixture, [Trait("Category","Integration")] (Plan 02 T2)
- [ ] `tests/e2e/manage-org-label.spec.ts` (new) — 5 scenarios + 403 (Plan 03 T1)
- [ ] `.planning/phases/344-test-uat/344-HUMAN-UAT.md` (new) — thin manual UAT + regression smoke (Plan 03 T2)

*TEST-01 + TEST-04 already covered by existing [Fact] — verify-only, no Wave 0 work.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cascade warning count value accuracy | TEST-06 sc.3 / ORG-INTEG-03 | Visual judgment of displayed count vs reality (reserved from Playwright per D-04) | Edit large Bagian → confirm modal count matches actual descendant count (UAT-5 in 344-HUMAN-UAT.md) |
| Regression smoke (drag-reorder, toggle active, delete unit, add unit) | ORG-INTEG-03 | Interactive UI flows, pre-v21.0 parity | SMOKE-1..4 in 344-HUMAN-UAT.md at localhost:5277 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (Plan 03 T3 is the single intentional manual checkpoint — D-04)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (5 of 7 tasks automated; manual checkpoint is terminal)
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 90s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** finalized 2026-06-04
