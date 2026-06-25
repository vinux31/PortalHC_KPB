---
phase: 401-proton-unit-resolution-hardening
plan: 06
subsystem: api
tags: [proton, bypass, unit-resolution, multi-unit, filter-axis, validation]

requires:
  - phase: 401-01
    provides: "ValidateAssignmentUnitInUserUnits shared helper"
provides:
  - "BypassList filters coachees by AssignmentUnit (active mapping) not scalar User.Unit"
  - "BypassSave validates TargetUnit in org-tree + worker.UserUnits active before junction write (no orphan AssignmentUnit)"
affects: [404]

tech-stack:
  added: []
  patterns:
    - "Server-authoritative TargetUnit validation: org-tree (GetSectionUnitsDictAsync) + junction (shared helper) — single-layer at controller before service write"

key-files:
  created: []
  modified:
    - Controllers/ProtonDataController.cs
    - HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs

key-decisions:
  - "TargetUnit validation placed AFTER cheap checks (CoacheeId/Reason/non-empty/Mode) and before service delegation — avoids DB round-trip on invalid mode"
  - "Single-layer controller validation (CONTEXT Open Q1) — NO duplicate check added in ProtonBypassService; E8 single-active untouched"
  - "ProtonBypassService NOT modified (listed in files_modified but Task 2 mandated single-layer); the two junction-writes are protected upstream by the controller gate"

patterns-established:
  - "Client-supplied unit that becomes persisted AssignmentUnit MUST pass org-tree + junction validation (mass-assignment guard, Invariant #4)"

requirements-completed: [PSU-02, PSU-03]

duration: ~14min
completed: 2026-06-18
---

# Phase 401 Plan 06: Bypass AssignmentUnit Filter + TargetUnit Validation Summary

**BypassList now scopes by AssignmentUnit and BypassSave rejects a TargetUnit the worker doesn't own — closing the orphan-AssignmentUnit hole where a client payload could break Invariant #4.**

## Performance

- **Duration:** ~14 min
- **Tasks:** 3/3 (TDD)
- **Files:** 1 controller modified, 1 test extended

## Accomplishments

- **BypassList axis (PSU-02)** — `:1517` scalar `x.u.Unit == unit` → correlated subquery `_context.CoachCoacheeMappings.Any(m => m.IsActive && m.CoacheeId == x.a.CoacheeId && m.AssignmentUnit == unitTrim)`. bagian/trackId filters intact.
- **BypassSave validation (PSU-03)** — keep non-empty check, then before service write: worker lookup + org-tree (`GetSectionUnitsDictAsync`) + `TargetUnit ∈ worker.UserUnits active` (shared 401-01 helper). Reject with specific message else → protects `ProtonBypassService` junction-writes from orphan AssignmentUnit (Invariant #4).
- **Untouched:** ProtonBypassService E8 single-active, `[ValidateAntiForgeryToken]`, `[Authorize]`.

## Task Commits

1. **Task 1+2: BypassList axis + BypassSave TargetUnit validation** — `8deecafc` (feat)
2. **Task 3: PSU-03 bypass TargetUnit validation test** — `998dbd32` (test)

## Files Created/Modified

- `Controllers/ProtonDataController.cs` — BypassList axis swap + BypassSave 3-layer TargetUnit validation
- `HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs` — +1 GREEN PSU-03 bypass test (6 total)

## Verification

- `dotnet build` → **Build succeeded**
- `grep "x.u.Unit == unit"` → **0** (BypassList swapped)
- `grep "ValidateAssignmentUnitInUserUnits"` (ProtonData) → **1**; `grep "GetSectionUnitsDictAsync"` → **4** (org-tree check present)
- `grep "string.IsNullOrWhiteSpace(req.TargetUnit)"` → **1** (non-empty retained)
- Filter `~AssignmentUnitInUserUnits` → **6 passed**
- Full suite (`Category!=Integration`) → **388 passed / 0 failed / 4 skipped** (+1 vs Plan 05, 0 regression)
- Live `dotnet run` boot smoke → see phase-end note in 401 (batched across autonomous plans)

## Deviations from Plan

**[Scope clarification] ProtonBypassService not modified** — Found during: Task 2. `files_modified` lists `Services/ProtonBypassService.cs`, but Task 2 explicitly mandated single-layer controller validation (CONTEXT Open Q1 — "do NOT add a duplicate check in ProtonBypassService"). The service's junction-writes are protected upstream by the new controller gate; E8 single-active left untouched. Net: the service file was read (protected target) but correctly not edited. **Impact:** none — matches plan intent.

**[Out-of-scope observation] ProtonDataController:524 defensive resolver** — a separate PROTON unit resolver at `:524` (different method, not in any 401 plan's scope) uses a `?? ...` fallback shape. NOT touched (out of scope; no plan assigned it). Flagged for the verifier/404 in case it warrants a follow-up.

**Total deviations:** 2 (1 scope clarification, 1 observation).

## Self-Check: PASSED

- key-files exist: ✓
- `git log --grep="401-06"` returns 2 commits: ✓ (8deecafc, 998dbd32)
- Acceptance re-run: ✓ (0 x.u.Unit==unit, ValidateAssignmentUnitInUserUnits≥1, GetSectionUnitsDictAsync≥1, non-empty retained, bypass test passes, build 0 error)
- No `## Self-Check: FAILED` marker.
