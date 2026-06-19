# Plan 402-03 Summary — CDP Self-Scope Union (CXU-05)

**Status:** Auto-tasks complete; UAT checkpoint DEFERRED to combined session after Plan 04 (user decision — single multi-unit fixture + browser session).
**Migration:** FALSE
**Commit:** e199b715

## Objective
Coach with multi-unit account sees UNION of all their coachees across units on CDP PROTON dashboard (CXU-05). Stop forcing `unit = user.Unit` (primary) at the 3 self-scope sites; enable unit dropdown with coach's own units; per-unit narrow still works (Phase 401 post-filter untouched).

## What Was Built
- **T1 — `ProtonProgressSubModel.UnitFilterEnabled`** (`CDPDashboardViewModel.cs`) bool, default false (no behavior change for non-coaching roles).
- **T2 — `CDPController.cs` 3 self-scope sites:**
  - `:305` FilterCoachingProton + `:326` ExportDashboardProgress (identical block, replace-all): lock Section; validate operator `unit ∈ coach.UserUnits` (query junction) else coerce `unit = null` (union); removed `unit = user.Unit`.
  - `:647` BuildProtonProgressSubModelAsync: `lockedUnit` left null (not pinned to primary).
  - availableUnits override for coaching-role = `coach.UserUnits` (own active units, not all Bagian units via GetUnitsForSectionAsync) + `unitFilterEnabled = true`; wired `UnitFilterEnabled` into subModel initializer.
  - **Post-filter (:465-545) UNTOUCHED** — verified: diff hunks only at 305/326/647/655/681, none inside 465-545.
- **T3 — `_CoachingProtonPartial.cshtml`:** lock condition `Model.RoleLevel >= 5` → `!Model.UnitFilterEnabled && Model.RoleLevel >= 5`; "Semua {Unit}" union option retained; OrgLabels used.

## Key Files
**modified:** `Controllers/CDPController.cs`, `Models/CDPDashboardViewModel.cs`, `Views/CDP/Shared/_CoachingProtonPartial.cshtml`

## Verification
- `dotnet build` → Build succeeded, 0 error.
- `dotnet test --filter ~CdpCoachUnion` → Passed 3/3.
- Full suite → **540 passed / 0 failed / 6 skipped** (no regression).
- Acceptance greps: forced-primary `section = user.Section; unit = user.Unit;` (0), `lockedUnit = user.Unit` (0), coach.UserUnits queries (3), `UnitFilterEnabled = unitFilterEnabled` (1), VM flag (1), view flag (1), old view hardcode (0), "Semua Unit" retained (1).
- Post-filter guard: git diff shows no change within 465-545.

## Deviations
- UAT checkpoint (live browser, multi-unit fixture) DEFERRED to a combined session after Plan 04 per user decision — both 402-03 (CDP union) and 402-04 (assign modal) UAT run together with one multi-unit fixture (efficient: single app boot + seed/restore).

## Self-Check: PASSED (code) / UAT PENDING (combined w/ 402-04)
Self-scope sites no longer force primary; operator unit validated ∈ coach.UserUnits else union; AvailableUnits = coach.UserUnits; dropdown enabled; post-filter intact; build + suite green.

## Notes for Downstream
- Combined UAT after Plan 04: verify (CDP) dropdown enabled + union 2 units + narrow + export; (modal) coach-first scope + per-row unit dropdown + cross-unit batch submit.
- T-402-08/09/10 mitigations live (no foreign-unit/Bagian leak; dropdown only coach's own units; post-filter regression-guarded).
