# Plan 402-02 Summary — Backend Cross-Unit Assign

**Status:** Complete
**Migration:** FALSE
**Commit:** 3a215005

## Objective
Server-authoritative cross-unit assign: CXU-01 (eligible units exposed), CXU-02 (cross-Bagian reject guard), CXU-03 (per-coachee unit resolution + validation) in `CoachCoacheeMappingAssign` + `CoachCoacheeMapping` action.

## What Was Built
**Task 1 — `CoachCoacheeMappingAssign` hardening:**
- (A) AssignmentSection still required (single); single-AssignmentUnit hard-require dropped (now per-coachee fallback).
- (B) **CXU-02 guard**: load `coach.Section` authoritatively → reject if `AssignmentSection != coach.Section`; loop rejects any coachee where `CoacheeSectionMatchesCoach == false` (`"{N} coachee bukan anggota Bagian coach (cross-Bagian ditolak)"`). Server-side boundary; client filter UX-only.
- (C) **CXU-03 loop**: resolve unit per-coachee = `req.AssignmentUnits[cid] ?? req.AssignmentUnit`; validate each ∈ org-tree(coach.Section) AND ∈ coachee.UserUnits (`ValidateAssignmentUnitInUserUnits`, helper 401) → `resolvedUnits` dict.
- (D) mapping creation uses `AssignmentUnit = resolvedUnits[id]` + `AssignmentSection = coach.Section` (per-coachee, not batch).
- (E) audit detail lists per-coachee distinct units.
- KEPT unchanged: authz `[Authorize(Roles="Admin, HC")]`+`[ValidateAntiForgeryToken]`, duplicate-active check, progression hard-block, transaction + ProtonTrack side-effect, friendly-error catch.

**Task 2 — `CoachCoacheeMapping` action:**
- `ViewBag.CoacheeUnits` = `Dictionary<string,List<string>>` coacheeId→active units, **primary-first** (`OrderByDescending(IsPrimary)`, D-02) for eligible coachees. Queried from junction `UserUnits` (Pitfall 1: no nav). New var `unitsByEligibleCoachee` — separate from the 401 orphan `unitsByCoachee` (untouched). Eligible loader stays global.

## Key Files
**modified:** `Controllers/CoachMappingController.cs` — assign guard+loop+mapping+audit; ViewBag.CoacheeUnits dict.

## Verification
- `dotnet build` → Build succeeded, 0 error.
- `dotnet test --filter ~CrossUnit` → Passed 5 / Skipped 1.
- Full suite → **540 passed / 0 failed / 6 skipped** (no regression vs baseline).
- Acceptance greps: cross-Bagian guard, AssignmentSection-eq-coach, `resolvedUnits[id]`, `CoacheeSectionMatchesCoach(_context, cid, coach.Section)` all present (1×); old `AssignmentUnit = req.AssignmentUnit!.Trim()` removed (0); `ViewBag.CoacheeUnits`(1) + `OrderByDescending(IsPrimary)`(1) present; 401 orphan `unitsByCoachee` intact (1).
- Live boot smoke deferred to Plan 04 UAT (app starts there; ViewBag cast safe — EligibleCoachees is `List<ApplicationUser>`).

## Deviations
- T1+T2 committed together (single cohesive commit, same file, small plan) rather than 2 atomic commits.

## Self-Check: PASSED
Server rejects cross-Bagian + per-coachee invalid units; each mapping persists own resolved unit; ViewBag dict primary-first available for Plan 04; authz/hard-block/duplicate-check intact; build 0 error; suite green.

## Notes for Downstream (Plan 04)
- Consume `ViewBag.CoacheeUnits` (cast `Dictionary<string,List<string>>`) → per coachee-item `data-units` JSON; render conditional per-row `<select>` when units.length>1.
- submitAssign JS builds `AssignmentUnits` map (coacheeId→unit); AssignmentSection auto = coach.Section.
- T-402-03/04/05 mitigations live (server guard). Client filter is UX backstop only.
