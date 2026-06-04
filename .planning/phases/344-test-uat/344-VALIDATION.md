---
phase: 344
slug: test-uat
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-04
---

# Phase 344 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Phase 344 IS the test/UAT phase — its deliverables ARE tests. Validation here = the tests themselves run green + UAT signed off.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests, EF Core 8) + Playwright (tests/e2e, TypeScript) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · playwright.config.ts |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test HcPortal.Tests` + `npx playwright test tests/e2e/manage-org-label.spec.ts` |
| **Estimated runtime** | xUnit ~2-5s (unit) / +real-DB integration ~10-30s · Playwright ~30-90s |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test HcPortal.Tests --filter "Category!=Integration"`
- **After every plan wave:** Run full `dotnet test HcPortal.Tests`
- **Before `/gsd-verify-work`:** Full xUnit green + Playwright spec green at localhost:5277
- **Max feedback latency:** ~5s (unit) / ~90s (E2E)

---

## Per-Task Verification Map

> Filled/finalized by gsd-planner + Nyquist audit. Skeleton derived from research gap map.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 344-??-?? | ?? | ? | TEST-01 | — | N/A (verify-only, covered) | unit | `dotnet test --filter FullyQualifiedName~OrgLabelServiceTests` | ✅ | ⬜ pending |
| 344-??-?? | ?? | ? | TEST-02 | — | non-Admin/non-HC denied | unit+e2e | reflection attr test + Playwright coach 403 | ❌ W0 | ⬜ pending |
| 344-??-?? | ?? | ? | TEST-03 | — | pre-order DFS order correct | unit+e2e | C# helper test + Playwright DOM order | ❌ W0 | ⬜ pending |
| 344-??-?? | ?? | ? | TEST-04 | — | dup-name per-parent | unit | `dotnet test --filter FullyQualifiedName~OrganizationControllerTests` | ✅ | ⬜ pending |
| 344-??-?? | ?? | ? | TEST-05 | — | migration apply + seed real DB | integration | `dotnet test --filter Category=Integration` | ❌ W0 | ⬜ pending |
| 344-??-?? | ?? | ? | TEST-06 | — | 5 E2E scenarios | e2e | `npx playwright test manage-org-label.spec.ts` | ❌ W0 | ⬜ pending |
| 344-??-?? | ?? | ? | ORG-INTEG-03 | — | no regression | manual+e2e | 344-HUMAN-UAT.md + regression smoke | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/OrgLabelControllerTests.cs` — add permission/attribute test (TEST-02)
- [ ] `HcPortal.Tests/OrganizationControllerTests.cs` or new — DFS pre-order C# helper test (TEST-03)
- [ ] `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` (new) — real-DB fixture (TEST-05); add `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0 to csproj
- [ ] `tests/e2e/manage-org-label.spec.ts` (new) — 5 scenarios (TEST-06)
- [ ] `.planning/phases/344-test-uat/344-HUMAN-UAT.md` (new) — thin manual UAT + regression smoke (ORG-INTEG-03)

*TEST-01 + TEST-04 already covered by existing [Fact] — verify-only, no Wave 0 work.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cascade warning count value accuracy | TEST-06 sc.3 / ORG-INTEG-03 | Visual judgment of displayed count vs reality | Edit large Bagian → confirm modal count matches actual descendant count |
| Regression smoke (drag-reorder, toggle active, delete unit, add unit) | ORG-INTEG-03 | Interactive UI flows, pre-v21.0 parity | 344-HUMAN-UAT.md checklist at localhost:5277 |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
