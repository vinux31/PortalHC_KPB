---
phase: 05-proton-deliverable-tracking
plan: 02
subsystem: ui
tags: [razor, mvc, bootstrap, proton, deliverable-tracking, cdp]

# Dependency graph
requires:
  - phase: 05-01
    provides: ProtonTrackAssignment, ProtonDeliverableProgress, ProtonKompetensi/SubKompetensi/Deliverable entities and DbSets, ProtonMainViewModel, ProtonPlanViewModel
provides:
  - ProtonMain GET action: lists coachees with their track assignments and active deliverable links
  - AssignTrack POST action: creates ProtonTrackAssignment and bulk-inserts ProtonDeliverableProgress records (first=Active, rest=Locked)
  - Views/CDP/ProtonMain.cshtml: coach UI for track assignment with Bootstrap modal and Lihat Deliverable links per coachee
  - Updated PlanIdp.cshtml: hybrid view supporting Coachee DB-driven deliverable table AND existing PDF path for other roles
  - Navigation entry points to Deliverable page (Plan 03) from both ProtonMain and PlanIdp
affects: ["05-03-deliverable-page", "phase-6-proton-assessment"]

# Tech tracking
tech-stack:
  added: ["IWebHostEnvironment injection in CDPController (for Plan 03 file upload)"]
  patterns:
    - "Hybrid Razor view using @model object? + ViewBag flags for multi-role rendering"
    - "Eager progress creation on track assignment (bulk insert first=Active, rest=Locked)"
    - "Role check pattern: RoleLevel <= 5 OR SrSupervisor (consistent with Coaching page)"

key-files:
  created:
    - Views/CDP/ProtonMain.cshtml
    - .planning/phases/05-proton-deliverable-tracking/05-02-SUMMARY.md
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/PlanIdp.cshtml
    - Views/CDP/Index.cshtml

key-decisions:
  - "@model object? in PlanIdp.cshtml supports hybrid rendering without breaking existing PDF path"
  - "IWebHostEnvironment added to CDPController constructor now (Plan 03 needs it for file upload) to avoid double constructor modification"
  - "Coachee role detection: userRole == UserRoles.Coachee OR (Admin with SelectedView == Coachee)"

patterns-established:
  - "Hybrid Razor: @model object? + ViewBag flag dispatch at top of view, cast with Model as ConcreteType in each branch"
  - "Proton navigation chain: ProtonMain -> AssignTrack POST -> ProtonMain | PlanIdp -> /CDP/Deliverable/{id}"

# Metrics
duration: 4min
completed: 2026-02-17
---

# Phase 5 Plan 02: Proton Main and PlanIdp with Navigation Summary

**ProtonMain coach UI for track assignment with bulk eager progress creation, and hybrid PlanIdp coachee view with 3-level deliverable hierarchy table and active deliverable navigation button**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-17T06:35:37Z
- **Completed:** 2026-02-17T06:39:37Z
- **Tasks:** 2
- **Files modified:** 4 (3 modified + 1 created)

## Accomplishments
- ProtonMain page: coach sees all coachees with current track, can assign track via Bootstrap modal, and sees "Lihat Deliverable" link per coachee with active progress
- AssignTrack POST: atomically deactivates old assignment, deletes old progresses, creates new assignment, bulk-inserts all progress records for the track (first=Active, rest=Locked)
- PlanIdp hybrid view: Coachee role gets DB-driven read-only deliverable table (3 levels: Kompetensi > SubKompetensi > Deliverable) with prominent "Lanjut ke Deliverable Aktif" button above; other roles get unchanged PDF path
- CDP Index navigation card added for Proton Main

## Task Commits

Each task was committed atomically:

1. **Task 1: ProtonMain and AssignTrack actions + view** - `6fabd67` (feat)
2. **Task 2: PlanIdp hybrid Coachee view** - `0e98e57` (feat)

## Files Created/Modified
- `Controllers/CDPController.cs` - Added IWebHostEnvironment, ProtonMain GET, AssignTrack POST, Coachee path in PlanIdp GET
- `Views/CDP/ProtonMain.cshtml` - New: coachee list table, assign modal, Lihat Deliverable links
- `Views/CDP/PlanIdp.cshtml` - Hybrid view: @model object?, IsProtonView branch for Coachee, NoAssignment branch, existing PDF branch unchanged
- `Views/CDP/Index.cshtml` - Added Proton Main navigation card

## Decisions Made
- `@model object?` chosen for PlanIdp to avoid breaking existing view while supporting ProtonPlanViewModel for Coachee role — cast at runtime with `Model as HcPortal.Models.ProtonPlanViewModel`
- Coachee role path inserted BEFORE Admin view-based filtering block in PlanIdp so it short-circuits early and returns `View(protonViewModel)` with IsProtonView flag
- IWebHostEnvironment added to CDPController constructor now (Plan 03 needs it for file upload) — noted as forward-looking addition to avoid double modification

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Razor RZ1010 errors from @{} blocks inside @if{} blocks**
- **Found during:** Task 2 (PlanIdp hybrid rendering)
- **Issue:** Used `@{ var protonModel = ... }` and `@{ ViewData["Title"] = ... }` inside `@if {}` blocks — Razor prohibits nested `@{}` inside code blocks
- **Fix:** Removed `@` prefix from code statements inside `@if {}` and `@else {}` branches (they're already in code context)
- **Files modified:** Views/CDP/PlanIdp.cshtml
- **Verification:** Build succeeded with 0 errors after fix
- **Committed in:** 0e98e57 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — Razor syntax bug)
**Impact on plan:** Fix was purely syntactic — no logic change. The architectural intent was correct.

## Issues Encountered
- Razor syntax: `@{ }` blocks inside `@if { }` are invalid (RZ1010). Inside a Razor code block, you're already in C# context — no `@` prefix needed for statements. Fixed by stripping the `@` prefix.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Navigation entry points to Deliverable page are in place: `/CDP/Deliverable/{progressId}` links from both ProtonMain and PlanIdp
- Plan 03 can now build the Deliverable GET action and view — the progressId routing is established
- IWebHostEnvironment is already injected into CDPController — Plan 03 can use `_env` for file upload disk path

---
*Phase: 05-proton-deliverable-tracking*
*Completed: 2026-02-17*
