---
phase: 344-test-uat
plan: 01
status: complete
completed: 2026-06-04
requirements: [TEST-01, TEST-02, TEST-03, TEST-04]
commits:
  - 9f984658
  - 12320265
  - d4d3d220
---

# Phase 344 Plan 01 — Summary

## What was built

Closed the two real C# unit-test gaps (TEST-02 permission, TEST-03 pre-order DFS); TEST-01 + TEST-04 confirmed already-covered (verify-only, no duplicates — D-01).

- **Task 1 (`9f984658`, comment fix `12320265`)** — New `Helpers/OrgTreePreOrder.cs` pure static helper porting `orgTree.js` buildTree + flattenTreePreOrder (D-05). NO internal re-sort — consumes endpoint-ordered flat input in insertion order (C1 fidelity). Standalone — `GetOrganizationTree` endpoint NOT refactored. Added 2 DISCRIMINATING [Fact] to `OrganizationControllerTests.cs` asserting against the helper only (not the flat endpoint): root-DisplayOrder-out-of-id-order → `[4,5,1,2,3]`; grandchild-before-uncle DFS≠BFS → `[10,20,5,30]` (+ `NotEqual` BFS) + Depth==2 grandchild assertions (C2).
- **Task 2 (`d4d3d220`)** — 5 reflection-attribute [Fact] in `OrgLabelControllerTests.cs` (TEST-02): assert `[Authorize(Roles="Admin, HC")]` on the 4 mutating actions + `GetLevelLabels` open-read contract (ORG-LABEL-03). Live 403 deferred to Plan 03.

## key-files
created:
  - Helpers/OrgTreePreOrder.cs
modified:
  - HcPortal.Tests/OrganizationControllerTests.cs
  - HcPortal.Tests/OrgLabelControllerTests.cs

## Verification (evidence)

- `dotnet build HcPortal.csproj` + `HcPortal.Tests` — Build succeeded, 0 errors.
- PreOrder tests: Passed! 2/2.
- OrganizationControllerTests: Passed! 8/8 (6 existing untouched + 2 new).
- OrgLabelControllerTests: Passed! 12/12 (7 existing + 5 new).
- OrgLabelServiceTests (TEST-01): Passed! 13/13 unchanged (no duplicates).
- Full unit suite (Category!=Integration): **Passed! 51/51.**
- Grep guards: C1 `OrderBy|.Sort(|OrderByDescending` in helper = 0 (no re-sort); `GetOrganizationTree` in OrgControllerTests = 0 (silent-pass trap avoided); `BuildPreOrder` calls = 3; 5 role methods; `Admin, HC` ×4.

## Deviations

- **Bug fix vs plan's verbatim helper code (TDD-caught):** The plan's snippet used `flat.GroupBy(n => n.ParentId).ToDictionary(g => g.Key, ...)`. Roots have `ParentId == null`; `Dictionary<int?,_>` rejects a null key → `ArgumentNullException` at runtime (both PreOrder tests failed initially). Rewrote as a roots-list + a `Dictionary<int,List>` of children keyed by non-null parent id, preserving the same insertion-order / no-re-sort semantics. Tests then passed 2/2. Same observable behavior the plan specified; only the internal data structure changed to be null-safe.
- **Comment rephrasing (post-test):** Comments containing literal `OrderBy`/`Sort` (helper) and `GetOrganizationTree` (test) tripped the plan's grep-0 silent-pass guards even though the CODE was correct. Reworded to "re-order" / "flat tree endpoint output". No logic change.

## Self-Check: PASSED
