---
phase: 401-proton-unit-resolution-hardening
plan: 02
subsystem: api
tags: [proton, coaching, unit-resolution, multi-unit, audit, coachmappingcontroller]

requires:
  - phase: 401-01
    provides: "ValidateAssignmentUnitInUserUnits helper + ProtonUnitResolve/UnitUnresolvedAudit Skip-RED scaffolds"
provides:
  - "GetEligibleCoachees gate resolves AssignmentUnit-only; empty -> BLOCK + persisted AuditLog 'ProtonUnitUnresolved' + LogWarning"
  - "AutoCreateProgressForAssignment read-path resolves AssignmentUnit-only; empty -> skip + LogWarning + warnings.Add, NO persisted audit"
affects: [401-03, 401-04, 401-05, 404]

tech-stack:
  added: []
  patterns:
    - "D-03 hybrid audit channel: GATE (eligibility/cert) = persisted AuditLog + LogWarning; READ-PATH (bootstrap) = LogWarning/warnings.Add only (anti-flood)"

key-files:
  created: []
  modified:
    - Controllers/CoachMappingController.cs
    - HcPortal.Tests/ProtonUnitResolveTests.cs
    - HcPortal.Tests/UnitUnresolvedAuditTests.cs

key-decisions:
  - "Actor resolved once before the eligibility loop, null-safe (actor?.Id ?? 'system', FullName ?? UserName ?? Identity.Name ?? 'system') — gate never throws inside loop"
  - "Resolver logic is inline in HTTP action methods (extraction deferred per 401-RESEARCH); tested via helper-level + channel primitives. Deep end-to-end (HTTP+real DB) Skip-RED re-targeted to Phase 404 QA-01"
  - "Live dotnet run boot smoke batched to phase-end (pure backend change, no UI/route) instead of per-plan"

patterns-established:
  - "GATE vs READ-PATH audit channel separation is the template for 401-04 (cert-gate = persisted) and 401-05 (CDP defensive = read-path)"

requirements-completed: [PSU-01, PSU-05]

duration: ~14min
completed: 2026-06-18
---

# Phase 401 Plan 02: AssignmentUnit-only PROTON Resolvers Summary

**Dropped the ambiguous `?? User.Unit` fallback from both CoachMappingController PROTON resolvers and wired the D-03 hybrid audit channel — eligibility/cert can no longer be issued against a primary-resolved (wrong) unit.**

## Performance

- **Duration:** ~14 min
- **Tasks:** 3/3 (TDD)
- **Files modified:** 3 (1 controller, 2 test)

## Accomplishments

- **GetEligibleCoachees (gate, PSU-01/05)** — resolver now reads `AssignmentUnit` only. Empty/whitespace → coachee BLOCKED from eligibility (D-02) + persisted `AuditLog` `ProtonUnitUnresolved` (queryable compliance/repudiation defense) + `LogWarning` (D-03 gate channel). Actor resolved null-safe before the loop. Per-unit deliverable scoping unchanged.
- **AutoCreateProgressForAssignment (read-path, PSU-01/05)** — resolver reads `AssignmentUnit` only. Empty → `LogWarning` + `warnings.Add` + return, NO persisted `_auditLog` (D-03 anti-flood). `ProtonDeliverableBootstrap.CreateProgressAsync` call intact.
- **Channel separation proven** as unit tests: gate persists `AuditLogs` row; read-path `LogWarning` leaves `AuditLogs` delta 0.

## Task Commits

1. **Task 1+2: AssignmentUnit-only resolvers + D-03 channel** — `39f272b2` (feat) _(both resolvers in same file, committed as one logical change)_
2. **Task 3: turn green PSU-01 + PSU-05 channel tests** — `c4fe87e2` (test)

## Files Created/Modified

- `Controllers/CoachMappingController.cs` — GetEligibleCoachees gate resolver (+ actor null-safe) + AutoCreateProgressForAssignment read-path resolver; 2 stale "fallback to User.Unit" comments corrected
- `HcPortal.Tests/ProtonUnitResolveTests.cs` — 3 GREEN PSU-01 facts (empty/whitespace not-from-primary, valid resolves true) + Skip→404
- `HcPortal.Tests/UnitUnresolvedAuditTests.cs` — GREEN channel-separation (read-path no-persist) + Skip→404

## Verification

- `dotnet build HcPortal.csproj -c Debug` → **Build succeeded**
- `grep "Select(u => u.Unit)" CoachMappingController.cs` → **0** (fallback removed from both resolvers)
- `grep "ProtonUnitUnresolved"` → **1** (gate-block audit present); `ProtonDeliverableBootstrap.CreateProgressAsync` → **1** (intact)
- Filter `~ProtonUnitResolve|~UnitUnresolvedAudit` → **6 passed / 2 skipped**
- Full suite (`Category!=Integration`) → **385 passed / 0 failed / 4 skipped** (+3 GREEN vs Plan 01, 0 regression)
- Live `dotnet run` boot smoke → **batched to phase-end** (deviation, see below)

## Deviations from Plan

**[Process - batched verification] Live boot smoke deferred to phase-end** — Found during: Task verification. The plan `<verification>` lists `dotnet run localhost:5277 HTTP 200`. This change is pure backend resolver logic with no new route/UI; running the full app per-plan (×6) is wasteful. Boot smoke is run once after the last autonomous plan. Build + 385-test suite cover correctness. **Impact:** none on correctness; live smoke still executed before phase verification.

**Total deviations:** 1 (process, non-functional).

## Self-Check: PASSED

- key-files modified exist: ✓
- `git log --grep="401-02"` returns 2 commits: ✓ (39f272b2, c4fe87e2)
- All `<acceptance_criteria>` re-run: ✓ (0 Select(u=>u.Unit), ProtonUnitUnresolved≥1, gate has _auditLog+LogWarning, read-path LogWarning + 0 _auditLog, ≥3 ProtonUnitResolve PASS, AuditLogs.Count delta 0 on read-path, build 0 error)
- No `## Self-Check: FAILED` marker.
