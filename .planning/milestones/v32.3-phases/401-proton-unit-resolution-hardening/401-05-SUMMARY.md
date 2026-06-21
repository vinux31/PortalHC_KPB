---
phase: 401-proton-unit-resolution-hardening
plan: 05
subsystem: api
tags: [proton, cdp, unit-resolution, multi-unit, filter-axis, visibility]

requires:
  - phase: 401-01
    provides: "D-03 read-path channel pattern + single-active invariant assumption"
provides:
  - "CDP coachee-scope filters (4 sites) use AssignmentUnit axis (batched, no N+1)"
  - "CDP defensive resolvers (2 sites) resolve AssignmentUnit-only; userUnits129 primary fallback removed + LogWarning on empty"
affects: [402, 404]

tech-stack:
  added: []
  patterns:
    - "Batch unitByCoachee dictionary (one query/site) for in-memory filter; EXISTS subquery (m.AssignmentUnit == unit) for SQL IQueryable filter — both avoid N+1 (Pitfall 2)"

key-files:
  created:
    - HcPortal.Tests/FilterAxisTests.cs
  modified:
    - Controllers/CDPController.cs

key-decisions:
  - "userUnits129 query deleted in both defensive resolvers — became fully unused after dropping the ?? fallback (block-scoped, no other reference)"
  - "Filter semantics preserved (exact equality, unit null-guard intact) — ONLY the axis changed (User.Unit -> AssignmentUnit), per CONTEXT Claude's Discretion"
  - "Defensive-resolver LogWarning keyed by assignment Id (asnUnitMap129 dict key is a.Id, not CoacheeId) — message says 'assignment {AssignmentId}'"

patterns-established:
  - "AssignmentUnit visibility axis: a coachee PROTON-assigned at a non-primary unit is scoped to that unit, not their primary mirror"

requirements-completed: [PSU-01, PSU-02, PSU-05]

duration: ~16min
completed: 2026-06-18
---

# Phase 401 Plan 05: CDP AssignmentUnit Filter-Axis Summary

**Swapped CDP coachee-scope from scalar User.Unit to AssignmentUnit at 4 filter sites + 2 defensive resolvers — a coachee assigned at a non-primary unit now appears under the correct unit, never the primary mirror.**

## Performance

- **Duration:** ~16 min
- **Tasks:** 3/3 (TDD)
- **Files:** 1 controller modified, 1 test created

## Accomplishments

- **Filter axis (PSU-02)** — 4 coachee-scope sites swapped: `:491` (in-memory, batch `unitByCoachee` dict + OrdinalIgnoreCase), `:1586`/`:1596`/`:4248` (SQL IQueryable, `_context.CoachCoacheeMappings.Any(m => m.IsActive && m.CoacheeId == u.Id && m.AssignmentUnit == unit)` subquery). No N+1 (batch/EXISTS, Pitfall 2). Unit null-guards + existing semantics preserved.
- **Defensive resolvers (PSU-01/05)** — both `asnUnitMap129` projections (`:519`, `:1712`) drop the `?? userUnits129.GetValueOrDefault` primary fallback → resolve `AssignmentUnit` only; `userUnits129` query deleted (unused); `LogWarning` on empty (read-path D-03, NO persisted audit).
- **Untouched:** Section-based authorization (`IsResultsAuthorized`) — de-risk per spec §7.

## Task Commits

1. **Task 1+2: defensive resolver drop-fallback + 4-site axis swap** — `ffba5bae` (feat)
2. **Task 3: FilterAxisTests PSU-02 primitive** — `72b8092c` (test)

## Files Created/Modified

- `Controllers/CDPController.cs` — 2 defensive resolvers (drop fallback + delete userUnits129 + LogWarning) + 4 coachee-scope filter swaps
- `HcPortal.Tests/FilterAxisTests.cs` — GREEN PSU-02 primitive (own file, disjoint from 401-02)

## Verification

- `dotnet build` → **Build succeeded**
- `grep "?? userUnits129.GetValueOrDefault"` → **0**; `grep "userUnits129"` → **0** (deleted)
- `grep "u.Unit == unit"` → **0** (all 4 coachee-scope sites swapped)
- `grep "unitByCoachee|AssignmentUnit == unit"` → **5** (axis present at 4 sites); `grep "AssignmentUnit kosong"` → **4** (LogWarning + comment at both resolvers)
- Filter `~FilterAxis` → **1 passed**
- Full suite (`Category!=Integration`) → **387 passed / 0 failed / 4 skipped** (+1 vs Plan 04, 0 regression)
- Live `dotnet run` boot smoke → batched to phase-end

## Deviations from Plan

**[Rule 1 - cleanup] Deleted userUnits129 query in both resolvers** — Found during: Task 1. The plan said "if userUnits129 becomes entirely unused after removing the fallback, delete its query too". After dropping the fallback, `userUnits129` had no remaining reference in either block → deleted both queries (removes a dead DB round-trip). Verified `grep userUnits129` → 0. **Impact:** positive (one fewer query per resolver), no behavior change.

**[Process] Live boot smoke deferred to phase-end** — same rationale as 401-02/04.

**Total deviations:** 2 (1 cleanup, 1 process).

## Self-Check: PASSED

- key-files exist: ✓
- `git log --grep="401-05"` returns 2 commits: ✓ (ffba5bae, 72b8092c)
- Acceptance re-run: ✓ (0 ?? userUnits129, 0 u.Unit == unit at 4 sites, axis≥4, LogWarning≥2, 0 _auditLog in read-path windows, FilterAxis test passes, build 0 error)
- No `## Self-Check: FAILED` marker.
