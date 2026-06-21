---
phase: 401-proton-unit-resolution-hardening
plan: 01
subsystem: testing
tags: [proton, coaching, unit-resolution, multi-unit, userunits, testable-seam, xunit]

requires:
  - phase: 399-foundation
    provides: "UserUnits junction (DbSet, IsActive/IsPrimary), WorkerController.ValidateUnitsInSection testable-seam pattern, InMemoryContext test factory, CapturingLogger"
provides:
  - "public static CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, assignmentUnit) — single-source AssignmentUnit ∈ active UserUnits validator (empty→false, Trim+OrdinalIgnoreCase)"
  - "5 new xUnit test files (10 GREEN + 4 Skip-RED) describing PSU-01/03/04/05/07 observable behavior"
affects: [401-02, 401-03, 401-04, 401-05, 401-06]

tech-stack:
  added: []
  patterns:
    - "Public static async testable seam querying junction UserUnits directly (no ApplicationUser nav property — Pitfall 1), mirrors WorkerController.ValidateUnitsInSection"
    - "Skip-RED test scaffold: every [Fact(Skip=...)] reason names the downstream plan (401-0X) that turns it green — suite stays green while contract is committed (Nyquist: tests before impl)"

key-files:
  created:
    - HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs
    - HcPortal.Tests/ProtonUnitResolveTests.cs
    - HcPortal.Tests/CleanupNoClobberTests.cs
    - HcPortal.Tests/UnitUnresolvedAuditTests.cs
    - HcPortal.Tests/ReactivateUnitValidationTests.cs
  modified:
    - Controllers/CoachMappingController.cs

key-decisions:
  - "Helper placed after View overrides in CoachMappingController (grouped, easy-to-find); acceptance checks grep/content not location"
  - "AssignmentUnitInUserUnitsTests are GREEN immediately (helper shipped Task 1) — act as the contract test; resolver/cleanup/audit/reactivation target-behavior tests are Skip-RED pending production wiring in 401-02..06"
  - "No production call sites added — helper only DEFINES the contract; wiring is 401-03 (Assign/Edit/Import/Cleanup/Reactivate) + 401-06 (bypass TargetUnit)"

patterns-established:
  - "Single-source junction validator: empty/whitespace AssignmentUnit → false (never resolve from primary User.Unit) — the core PSU-01 invariant"
  - "D-03 hybrid audit channel primitives proven: persisted AuditLog 'ProtonUnitUnresolved' (gate-block) vs CapturingLogger Warning-only (read-path)"

requirements-completed: [PSU-01, PSU-03, PSU-04, PSU-05, PSU-07]

duration: ~18min
completed: 2026-06-18
---

# Phase 401 Plan 01: PROTON Unit-Resolution Foundation Summary

**Shipped the single-source `ValidateAssignmentUnitInUserUnits` testable seam + 5 RED test scaffolds — the contract every Wave-1/2 plan (401-02..06) implements against.**

## Performance

- **Duration:** ~18 min
- **Tasks:** 3/3 (TDD)
- **Files created:** 5 test files
- **Files modified:** 1 controller

## Accomplishments

- **PSU-03 helper** — `public static async Task<bool> ValidateAssignmentUnitInUserUnits(ApplicationDbContext, string coacheeId, string? assignmentUnit)` in `CoachMappingController`. Queries junction `UserUnits` (active-only) directly — NO `ApplicationUser.UserUnits` nav property (Pitfall 1, would force a migration). Empty/whitespace → `false` (the PSU-01 "never resolve from primary" invariant). Trim + OrdinalIgnoreCase. EF-InMemory testable without `InternalsVisibleTo`.
- **5 test files** describing PSU-01/03/04/05/07: 10 GREEN (helper contract + audit/reactivation primitives) + 4 Skip-RED (resolver-skip, cleanup no-clobber, D-03 hybrid channel, reactivation guard) — each Skip names its downstream plan.
- **0 migration**, build 0 error, full suite green.

## Task Commits

1. **Task 1: Extract ValidateAssignmentUnitInUserUnits helper** — `d65b09fa` (feat)
2. **Task 2: RED scaffolds PSU-03 helper + PSU-01 resolver + PSU-04 no-clobber** — `5709142b` (test)
3. **Task 3: RED scaffolds PSU-05 audit channel + PSU-07 reactivation** — `c10a786a` (test)

## Files Created/Modified

- `Controllers/CoachMappingController.cs` — added the static helper (22 lines) after View overrides
- `HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs` — 5 GREEN: accept/reject/empty/inactive/case-trim
- `HcPortal.Tests/ProtonUnitResolveTests.cs` — 1 GREEN sanity + 1 Skip-RED (401-02/04/05)
- `HcPortal.Tests/CleanupNoClobberTests.cs` — 1 GREEN sanity + 1 Skip-RED (401-03)
- `HcPortal.Tests/UnitUnresolvedAuditTests.cs` — 2 GREEN (persisted + read-path primitives) + 1 Skip-RED (401-02/05)
- `HcPortal.Tests/ReactivateUnitValidationTests.cs` — 1 GREEN sanity + 1 Skip-RED (401-03)

## Verification

- `dotnet build HcPortal.csproj -c Debug` → **Build succeeded, 0 Error(s)**
- Helper filter (`~AssignmentUnitInUserUnits|~ProtonUnitResolve|~CleanupNoClobber`) → **7 passed / 2 skipped**
- Audit/reactivation filter (`~UnitUnresolvedAudit|~ReactivateUnitValidation`) → **3 passed / 2 skipped**
- Full suite (`Category!=Integration`) → **382 passed / 0 failed / 4 skipped** (4 skip = new RED scaffolds; +10 GREEN vs baseline, 0 regression)
- `grep coachee.UserUnits|Include(...UserUnits)` in Controllers + Tests → **0** (no nav-property usage)

## Deviations from Plan

None - plan executed exactly as written.

## Next Plan Readiness

- **401-02** (drop `?? User.Unit` in GetEligibleCoachees gate + AutoCreateProgressForAssignment read-path, D-03 channels) — turns ProtonUnitResolve + UnitUnresolvedAudit Skip-RED green.
- **401-04/05/06** call the same helper for cert-gate / CDP filter-axis / bypass TargetUnit.
- **401-03** (Wave 2, autonomous:false) wires Assign/Edit/Import/Cleanup/Reactivate + UI indicator — turns CleanupNoClobber + Reactivate Skip-RED green; AF-4 window ±5s grep guard lives in its acceptance.

## Self-Check: PASSED

- key-files.created exist on disk: ✓ (5 test files + modified controller)
- `git log --grep="401-01"` returns 3 commits: ✓ (d65b09fa, 5709142b, c10a786a)
- All `<acceptance_criteria>` re-run: ✓ (helper grep=1, nav-prop=0, build 0 error, helper-call≥5, all Skip carry 401-0 reason, filtered runs exit 0)
- No `## Self-Check: FAILED` marker.
