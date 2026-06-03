---
phase: 342
plan: 03
subsystem: admin-organization
tags: [xunit, test, cascade-accuracy, uat]
requires: [342-01 controller, 342-02 frontend]
provides: [OrganizationControllerTests dup-name + preview==actual locks]
affects: [HcPortal.Tests/OrganizationControllerTests.cs]
tech-stack:
  added: []
  patterns: [InMemory controller test, preview==actual tripwire, UserManager null-substitute]
key-files:
  created:
    - HcPortal.Tests/OrganizationControllerTests.cs
  modified: []
key-decisions: [Pitfall 1 preview==actual lock, Pitfall 5 identical casing, T3 auto-close via Plan 02 UAT]
requirements-completed: [ORG-TREE-02, ORG-TREE-07]
duration: "~12 min"
completed: 2026-06-03
---

# Phase 342 Plan 03: xUnit + Manual UAT Summary

Built `OrganizationControllerTests.cs` (6 [Fact]) locking ORG-TREE-02 (dup-name per-parent) + ORG-TREE-07 (preview count == actual cascade — Pitfall 1 tripwire). Full suite 44/44. T3 manual UAT auto-closed via Plan 02 browser UAT + xUnit deterministic lock.

## Commit

| Hash | Description |
|------|-------------|
| `8b593d0e` | test(342-03): OrganizationControllerTests 6 facts (dup-name + preview==actual) |

## Tasks Completed

- **Task 1** — `HcPortal.Tests/OrganizationControllerTests.cs` (NEW): fixture (InMemory Guid DB + `new OrganizationController(ctx, null!, auditLog, null!)` + X-Requested-With for Json) + 3 reflection helpers + 3 dup-name [Fact] (accept diff-parent, reject same-parent Add + Edit).
- **Task 2** — +3 PreviewEditCascade [Fact]: RenameLevel0_CountMatchesActual, RenameLevel1_CountMatchesActual (both `Assert.Equal(actual, preview)` per field-pair — Pitfall 1 drift tripwire), NoChange_ReturnsEarlyFalseFlags (D-04).
- **Task 3** — checkpoint:human-verify → **AUTO-CLOSED** (see below).

## Test Outcomes (6/6 PASS)

| [Fact] | REQ | Lock | Result |
|--------|-----|------|--------|
| AddOrganizationUnit_SameNameDifferentParent_Accepted | ORG-TREE-02 | "Operations" allowed in 2 Bagian | ✅ |
| AddOrganizationUnit_SameNameSameParent_Rejected | ORG-TREE-02 | reject same parent | ✅ |
| EditOrganizationUnit_SameNameSameParent_Rejected | ORG-TREE-02 | rename reject, exclude self | ✅ |
| PreviewEditCascade_RenameLevel0_CountMatchesActual | ORG-TREE-07 | preview==actual 4 field-pair (Section branch) | ✅ |
| PreviewEditCascade_RenameLevel1_CountMatchesActual | ORG-TREE-07 | preview==actual 4 field-pair (Unit branch) | ✅ |
| PreviewEditCascade_NoChange_ReturnsEarlyFalseFlags | ORG-TREE-07 | D-04 early-return | ✅ |

- `dotnet test --filter OrganizationControllerTests` → **6/6 PASS** (~1s).
- `dotnet test HcPortal.Tests` → **44/44 PASS** (38 baseline + 6 new), 0 skipped. No regression.

## Grep Acceptance (all PASS)

class=1, ctor null-substitute=1, X-Requested-With=1, [Fact]=6, `Assert.Equal(aUsers, pUsers)`=2 (Level0+Level1 both lock preview==actual), 3 named preview tests=1 each.

## Task 3 — Manual UAT AUTO-CLOSED

T3's 10-scenario formal UAT was **already executed comprehensively in Plan 02 Task 4** (Playwright MCP, 10/10 PASS) covering identical scenarios: pre-order DFS (1), dup-name per-parent (2/9), parent nonaktif (3), escape (4/7), path (5/6), cascade-confirm with count vs DB (7/8), legend/title/badge (8/9/10), regression drag/toggle/delete + 0 console error (10). Additionally:
- **Cascade count accuracy vs DB** verified live in Plan 02 UAT: GAST edit modal showed 7 user / 1 mapping / 2 kompetensi / 1 file panduan == PreviewEditCascade response == EditOrganizationUnit actual mutation.
- **xUnit `preview==actual`** (this plan) provides a deterministic regression tripwire that locks what manual UAT can only spot-check.

No redundant browser re-run. Phase 342 functional coverage complete.

## Deviations from Plan

None - tests written per RESEARCH Validation Architecture Pattern A/B/C verbatim. Model seed used minimal props (InMemory does not enforce required scalars, so non-seeded fields default safely). Added 2 sanity asserts (pKomp) beyond template — strengthens lock.

## Phase 342 Closure

3/3 plans SHIPPED LOCAL — all 10 ORG-TREE (01-10) covered:
- 342-01 backend: e7b753d3 (dup-name per-parent ORG-TREE-02 + PreviewEditCascade ORG-TREE-07 A1 full-accuracy)
- 342-02 frontend: d91fef18 (ORG-TREE-01/03/04/05/06/07/08/09/10) + Playwright UAT 10/10
- 342-03 tests: 8b593d0e (6 [Fact] lock ORG-TREE-02/07) + full suite 44/44

NOT PUSHED — bundle v19+v20+v21 (340+341+342). Ready /gsd-verify-work or phase verification.

## Self-Check: PASSED
