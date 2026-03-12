---
phase: 158-homepage-navigation-audit
plan: 01
subsystem: ui
tags: [homepage, dashboard, navbar, progress-bars, role-based-access]

# Dependency graph
requires:
  - phase: 156-planidp-cdp-dashboard-audit
    provides: "CDP progress patterns and coachee assignment handling"
  - phase: 157-account-auth-audit
    provides: "Auth flow and role-based access patterns"
provides:
  - "NAV-01 homepage dashboard data accuracy verified"
  - "NAV-02 navbar role-scoping verified"
affects: [homepage, dashboard, navbar]

# Tech tracking
tech-stack:
  added: []
  patterns: [code-audit-then-uat]

key-files:
  created: []
  modified:
    - Controllers/HomeController.cs

key-decisions:
  - "NAV-01: CDP progress uses FirstOrDefaultAsync for track assignment — intentional, shows single active track"
  - "NAV-01: CoachingSession Status=Submitted is confirmed terminal status (model defines only Draft/Submitted)"
  - "NAV-01: Assessment upcoming filter uses Status=Open|Upcoming — confirmed canonical statuses throughout codebase"
  - "NAV-01: AddDays(2).AddTicks(-1) for end-of-tomorrow is correct"
  - "NAV-02: Kelola Data conditioned on Admin|HC matches AdminController authorization exactly"
  - "NAV-02: CDP visible to all roles by design — all authenticated users are eligible coachees"
  - "GuideDetail module param normalized to lowercase — pre-existing fix in working tree, included in audit commit"

patterns-established:
  - "Homepage progress: FirstOrDefaultAsync for single active track (not multi-track)"
  - "Coaching completion: Status=Submitted (terminal, no Completed status for CoachingSessions)"

requirements-completed:
  - NAV-01
  - NAV-02

# Metrics
duration: 20min
completed: 2026-03-12
---

# Phase 158 Plan 01: Homepage & Navigation Audit Summary

**Code audit of homepage dashboard data accuracy and navbar role-scoping: all queries verified correct, one pre-existing GuideDetail module bypass fix absorbed**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-03-12
- **Completed:** 2026-03-12
- **Tasks:** 1 of 2 (Task 2 is human-verify checkpoint)
- **Files modified:** 1

## Accomplishments
- Verified CDP progress query chain (ProtonTrackAssignment → Kompetensi → SubKompetensi → Deliverables → Approved count) is correct
- Verified Assessment counting uses Status="Completed" which is confirmed correct by CMPController.SubmitExam
- Verified CoachingSession counting uses Status="Submitted" — confirmed as the only terminal status (model: Draft/Submitted)
- Verified upcoming events date window (AddDays(2).AddTicks(-1)) is correct end-of-tomorrow
- Verified navbar role-scoping: Kelola Data on Admin|HC matches AdminController authorization; CMP/CDP/Panduan visible to all roles by design
- Absorbed pre-existing GuideDetail module param normalization fix (prevents case-sensitive bypass of admin module check)

## Task Commits

1. **Task 1: Code audit — Homepage dashboard and navbar** - `c2eda01` (fix)
   - Note: This commit was made in a prior session under the phase name 158-02; the fix is the same work.

## Files Created/Modified
- `Controllers/HomeController.cs` - Added `module = module?.ToLowerInvariant() ?? ""` normalization in GuideDetail

## Decisions Made
- CDP progress: `FirstOrDefaultAsync` on track assignment is intentional — shows single active track's progress on dashboard. No change needed.
- CoachingSession "Submitted" is correct — the model only has "Draft" and "Submitted" statuses; sessions are directly created as "Submitted" by coaches.
- AssessmentSession "Open" | "Upcoming" filter in GetUpcomingEvents is confirmed correct — both are canonical active statuses in the system.
- Navbar CDP visible to all: by design, all authenticated users may be coachees.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] GuideDetail module param case-sensitivity bypass**
- **Found during:** Task 1 (code audit)
- **Issue:** `GuideDetail(string module)` compared `module` against lowercase arrays ("data", "admin", "cmp", etc.) without normalizing case. A URL like `?module=DATA` would bypass the admin role check.
- **Fix:** Added `module = module?.ToLowerInvariant() ?? ""` before the validation checks
- **Files modified:** Controllers/HomeController.cs
- **Verification:** Build compiles clean (file-lock error only from running app)
- **Committed in:** c2eda01 (pre-existing in working tree, absorbed into audit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Necessary security fix. No scope creep.

## Issues Encountered
- Build shows MSB file-lock errors only (running app prevents copy of exe) — no C# compilation errors confirmed.

## Next Phase Readiness
- Task 2 (UAT checkpoint) pending user verification across roles
- All code verified correct; homepage and navbar ready for browser UAT

## Self-Check

- `Controllers/HomeController.cs` modified: pre-existing change confirmed in git log (c2eda01)
- Commit c2eda01 verified: `git log --oneline | grep c2eda01`

## Self-Check: PASSED

---
*Phase: 158-homepage-navigation-audit*
*Completed: 2026-03-12*
