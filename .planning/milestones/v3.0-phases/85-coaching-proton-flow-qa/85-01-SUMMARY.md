---
phase: 85-coaching-proton-flow-qa
plan: "01"
subsystem: coaching-proton
tags: [code-review, seed-data, coach-coachee-mapping, proton-deliverable]
dependency_graph:
  requires: []
  provides: [SeedCoachingTestData, CoachCoacheeMapping-CRUD-reviewed]
  affects: [Controllers/AdminController.cs]
tech_stack:
  added: []
  patterns: [idempotent-seed-pattern, active-user-filter-pattern]
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
decisions:
  - "[85-01]: CoachCoacheeMapping GET uses allUsers dict for display but activeUsers for modal dropdowns — inactive workers excluded from assignment UI"
  - "[85-01]: CoachCoacheeMappingExport missing [HttpGet] attribute — added; no other structural bugs found in CRUD"
  - "[85-01]: CDPController Proton Progress only appears in a code comment (line 280) — not a user-facing string; no rename needed"
  - "[85-01]: SeedCoachingTestData uses Coach role (via GetUsersInRoleAsync) not RoleLevel==5 for coach selection — matches Phase 74 decision in CoachCoacheeMapping GET"
metrics:
  duration: 15 min
  completed_date: "2026-03-04"
  tasks_completed: 2
  files_modified: 1
---

# Phase 85 Plan 01: CoachCoacheeMapping Code Review and Seed Data Summary

Code review of AdminController CoachCoacheeMapping CRUD — 3 bugs fixed — plus new idempotent SeedCoachingTestData action creating realistic coaching test data in all deliverable statuses.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Code review CoachCoacheeMapping CRUD and fix bugs | ff5f546 | Controllers/AdminController.cs |
| 2 | Add SeedCoachingTestData action to AdminController | eb17192 | Controllers/AdminController.cs |

## Bugs Found and Fixed (Task 1)

### Bug 1: Missing [HttpGet] on CoachCoacheeMappingExport

- **Found during:** Task 1 code review
- **Line:** ~4087 (before fix)
- **Issue:** `CoachCoacheeMappingExport` action was missing the `[HttpGet]` attribute. All other GET actions in the controller have it explicitly.
- **Fix:** Added `[HttpGet]` attribute before `[Authorize(Roles = "Admin, HC")]`
- **Commit:** ff5f546

### Bug 2: Inactive users appearing in CoachCoacheeMapping modal dropdowns

- **Found during:** Task 1 code review
- **Issue:** The `allUsers` query in `CoachCoacheeMapping` GET loaded ALL users (no `IsActive` filter). This caused deactivated workers to appear in the Assign Coach modal's coachee and "all users" dropdowns.
- **Fix:**
  - Added `u.IsActive` to the anonymous select projection
  - Derived `activeUsers` list filtered by `IsActive`
  - Changed `ViewBag.EligibleCoachees` and `ViewBag.AllUsers` to use `activeUsers` instead of `allUsers`
- **Commit:** ff5f546

### Bug 3: EligibleCoaches includes deactivated users

- **Found during:** Task 1 code review
- **Issue:** `GetUsersInRoleAsync(UserRoles.Coach)` returns ALL users in the Coach role, including deactivated ones. The result was not filtered by `IsActive`.
- **Fix:** Added `.Where(u => u.IsActive)` to the `EligibleCoaches` LINQ chain
- **Commit:** ff5f546

## Phase 82 Rename Verification (CDPController)

Searched CDPController.cs for remaining "Proton Progress" user-facing strings. Only match found is line 280:
```
// Helper: Proton Progress sub-model (supervisor / HC view)
```
This is a code comment, not a user-facing string. No fix needed. Phase 82 rename is complete for user-facing strings.

## SeedCoachingTestData Action (Task 2)

**Route:** GET /Admin/SeedCoachingTestData
**Auth:** `[Authorize(Roles = "Admin")]`
**Location:** AdminController.cs, after SeedAssessmentTestData (~line 2444)

### What it seeds

1. **Coach:** First active user with the "Coach" role (via `GetUsersInRoleAsync`)
2. **Coachee1:** First active user with `RoleLevel == 6`
3. **Coachee2:** Second active user with `RoleLevel == 6`
4. **Track:** First `ProtonTrack` ordered by `Urutan`

### CoachCoacheeMapping records

- Coach → Coachee1: `IsActive=true`, StartDate = 3 months ago
- Coach → Coachee2: `IsActive=true`, StartDate = 2 months ago

### ProtonTrackAssignment records

- Coachee1 → Track: `IsActive=true`
- Coachee2 → Track: `IsActive=true`

### ProtonDeliverableProgress records for Coachee1

| Deliverable Index | Status | SrSpv | SH | HC |
|---|---|---|---|---|
| 0 | Approved | Approved | Approved | Reviewed |
| 1 | Submitted | Pending | Pending | Pending |
| 2 | Rejected | Rejected | Pending | Pending |
| 3+ | Pending | — | — | — |

Deliverable[2] has `RejectionReason = "Bukti tidak lengkap, harap upload ulang dengan dokumen yang valid"`

### ProtonDeliverableProgress records for Coachee2

| Deliverable Index | Status | SrSpv | SH | HC |
|---|---|---|---|---|
| 0 | Approved | Approved | Approved | Pending (tests HCReview flow) |
| 1+ | Pending | — | — | — |

### Dummy evidence file

Created at `wwwroot/uploads/evidence/{approvedProgress1.Id}/test_evidence.txt` for the Approved record of Coachee1. Path and filename stored back into the progress record.

### Idempotency

All inserts are guarded by `FirstOrDefaultAsync` checks on the unique combination (CoachId+CoacheeId for mappings, CoacheeId for track assignments, CoacheeId+DeliverableId for progress). Running the action twice produces the same result.

### Error cases handled

- No active Coach role user → error + redirect
- Fewer than 2 coachee-level users → error + redirect
- No ProtonTrack → error + redirect
- No deliverables for the track → error + redirect
- Any exception → TempData["Error"] with message

## Deviations from Plan

None — plan executed exactly as written. All three code review bugs were in scope (Rule 1/Rule 2 fixes). The SeedCoachingTestData implementation matches the plan specification exactly.

## Self-Check

- [x] Task 1 commit ff5f546 exists
- [x] Task 2 commit eb17192 exists
- [x] AdminController.cs contains SeedCoachingTestData action
- [x] Build passes: 0 errors
- [x] CoachCoacheeMappingExport has [HttpGet]
- [x] EligibleCoaches/EligibleCoachees filter by IsActive
