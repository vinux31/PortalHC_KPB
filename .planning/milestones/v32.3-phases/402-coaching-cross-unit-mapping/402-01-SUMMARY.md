# Plan 402-01 Summary — Wave 0 Contract + RED Tests

**Status:** Complete
**Migration:** FALSE
**Commits:** 922e5961 (seam+DTO) · 36587044 (unit tests) · 4b2dd9a1 (e2e skeleton)

## Objective
Establish backend contracts + tests BEFORE implementation (Nyquist safeguard): static testable seam `CoacheeSectionMatchesCoach` (CXU-02), per-coachee `AssignmentUnits` map DTO field (CXU-03/D-05), RED logic tests (CXU-01/02/03/05), e2e skeleton (port 5270). No endpoint behavior change — pure contract/test.

## What Was Built
- **`CoacheeSectionMatchesCoach(ctx, coacheeId, coachSection)`** static seam in `CoachMappingController.cs` (after `ValidateAssignmentUnitInUserUnits` :62), mirroring 401 seam shape. Returns false on null/empty coachSection, coachee-not-found, or Section mismatch (Trim + OrdinalIgnoreCase).
- **`CoachAssignRequest.AssignmentUnits`** `Dictionary<string,string>?` (coacheeId→unit) added; legacy `AssignmentUnit` retained as fallback.
- **`CrossUnitAssignTests.cs`** — 5 facts (CXU-02 same/cross/empty Section, CXU-03 batch reject, CXU-01 eligible set-aware) + 1 `[Fact(Skip)]` Pitfall-5 single-active deferred to 404.
- **`CdpCoachUnionScopeTests.cs`** — 3 facts (CXU-05 union when unit=null, narrow per-unit, foreign-unit coerced→union no leak).
- **`coaching-crossunit-402.spec.ts`** — 4 CXU test skeletons (`test.skip` pending Plan 03/04), port 5270 + run command documented.

## Key Files
**created:**
- `HcPortal.Tests/CrossUnitAssignTests.cs` — RED logic seams CXU-01/02/03
- `HcPortal.Tests/CdpCoachUnionScopeTests.cs` — RED union scope CXU-05
- `tests/e2e/coaching-crossunit-402.spec.ts` — e2e skeleton
**modified:**
- `Controllers/CoachMappingController.cs` — seam + DTO field (endpoint body UNCHANGED)

## Verification
- `dotnet build` → Build succeeded, 0 Error.
- Filtered tests → Passed 8 / Skipped 1 / Total 9.
- Full suite → **540 passed / 0 failed / 6 skipped / 546 total** (baseline 532/0/5 → +8 pass +1 skip, no regression).
- `npx playwright test coaching-crossunit-402 --list` → 4 CXU tests, no parse errors.

## Deviations
None. Seam-logic tests pass after Task 1 (seam exists) — expected per plan; TRUE RED = endpoint behavior wired in Plan 02/03/04.

## Self-Check: PASSED
Seam + map field compile; legacy AssignmentUnit preserved; endpoint untouched; 8 facts + 1 deferred-skip; e2e parses; suite green.

## Notes for Downstream
- Plan 02 consumes: `CoacheeSectionMatchesCoach` (CXU-02 guard) + `req.AssignmentUnits` per-coachee + `ValidateAssignmentUnitInUserUnits` (CXU-03). MUST validate every map entry server-side (T-402-01).
- Plan 04 builds `AssignmentUnits` map in submitAssign JS; consumes `ViewBag.CoacheeUnits` from Plan 02.
- Plan 03 wires CDP union logic tested in `CdpCoachUnionScopeTests`.
