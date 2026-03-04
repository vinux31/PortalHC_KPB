---
phase: 85-coaching-proton-flow-qa
plan: "02"
subsystem: CDP/CoachingProton
tags: [coaching, proton, bug-fix, code-review, status-badge]
dependency_graph:
  requires: [85-01]
  provides: [CDPController-coaching-actions-verified, CoachingProton-view-verified, Deliverable-view-verified]
  affects: [COACH-03, COACH-04]
tech_stack:
  added: []
  patterns: [IsActive-filter, status-guard, canUpload-check]
key_files:
  created: []
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/Deliverable.cshtml
decisions:
  - "[85-02] canUpload status check fixed from 'Active' -> 'Pending'; ProtonDeliverableProgress.Status has no 'Active' value — default is 'Pending'"
  - "[85-02] IsActive filter added to HC/Admin and SrSpv coachee scope queries in CoachingProton action (Phase 83 filter was missing for these two scope branches)"
  - "[85-02] GetCoacheeDeliverables not called from CoachingProton.cshtml — coaching modal uses buildDeliverableData() from table rows instead; endpoint exists for potential future use"
metrics:
  duration: "15 min"
  completed: "2026-03-04"
  tasks: 2
  files: 2
---

# Phase 85 Plan 02: Coaching Proton Code Review and Bug Fixes Summary

Code review and bug-fix of CDPController coaching actions and CoachingProton/Deliverable views, patching three status-check bugs using nonexistent "Active" status and adding missing IsActive user filters.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Code review CDPController coaching actions and fix bugs | 65a06ec | Controllers/CDPController.cs |
| 2 | Targeted review of CoachingProton.cshtml and Deliverable.cshtml | b7a992c | Views/CDP/Deliverable.cshtml |

## Verification

- dotnet build: **0 errors** (only pre-existing warnings)
- CDPController coaching actions reviewed and patched
- CoachingProton.cshtml approval button guards: correct (boolean vars isSrSpv, isSH, isHC, isCoach)
- CSRF tokens: confirmed on all AJAX calls in CoachingProton.cshtml
- Deliverable.cshtml evidence upload form and coaching session display: correct

## Bugs Found and Fixed

### Bug 1 — CDPController.Deliverable canUpload uses wrong status

**Line:** 756
**Issue:** `canUpload = (progress.Status == "Active" || progress.Status == "Rejected") && userRole == UserRoles.Coach`
**Root cause:** `ProtonDeliverableProgress.Status` has no "Active" value — the model comment and default value both say "Pending". This caused `canUpload` to always evaluate to false for initial submissions, blocking coaches from uploading evidence on new deliverables.
**Fix:** Changed `"Active"` to `"Pending"`.
**Commit:** 65a06ec

### Bug 2 — CDPController.UploadEvidence status guard uses wrong status

**Line:** 1051
**Issue:** `if (progress.Status != "Active" && progress.Status != "Rejected")` — would reject any "Pending" status upload, returning an error.
**Fix:** Changed `"Active"` to `"Pending"`.
**Commit:** 65a06ec

### Bug 3 — GetCoacheeDeliverables pendingActions count uses wrong status

**Line:** 2022
**Issue:** `pendingActions = progresses.Count(p => p.Status == "Active" || p.Status == "Rejected")` — would always count 0 for "Active" deliverables.
**Fix:** Changed `"Active"` to `"Pending"`.
**Commit:** 65a06ec

### Bug 4 — Deliverable.cshtml status badge uses wrong status name

**Lines:** 49-53
**Issue:** Badge for `"Active"` status used `bg-primary` (blue). Per plan spec: Pending=gray, Submitted=blue, Approved=green, Rejected=red.
**Fix:** Replaced `"Active"` -> `"Pending"` with `bg-secondary` (gray); reassigned `bg-primary` (blue) to `"Submitted"`.
**Commit:** b7a992c

## Auto-fixed Issues

**1. [Rule 1 - Bug] Missing IsActive filter on user scope queries in CoachingProton**
- **Found during:** Task 1 review
- **Issue:** HC/Admin (level <=2) and SrSpv (level 4) coachee scope queries at lines 1109 and 1115 were missing `u.IsActive` filter, meaning deactivated users (Phase 83 soft-delete) could still appear as coachees.
- **Fix:** Added `&& u.IsActive` to both where clauses.
- **Files modified:** Controllers/CDPController.cs
- **Commit:** 65a06ec

## Code Confirmed Correct (No Changes Needed)

**CDPController.CoachingProton role scoping:**
- Level <=2 (HC/Admin): all active coachees with RoleLevel==6 — confirmed (now with IsActive)
- Level 4 (SrSpv/SH): same section, RoleLevel==6 — confirmed (now with IsActive)
- Level 5 (Coach): CoachCoacheeMappings WHERE CoachId==user.Id AND IsActive — confirmed correct
- Level 6 (Coachee): only user.Id itself — confirmed correct

**CDPController.Deliverable access check:**
- isCoachee = progress.CoacheeId == user.Id — correct
- isCoach = user.RoleLevel <= 5 — correct
- HC full access (no section check) — correct
- Coach section check enforced — correct

**CDPController.SubmitEvidenceWithCoaching:**
- RoleLevel==5 check — correct
- validCoacheeIds loads from CoachCoacheeMappings WHERE CoachId==user.Id AND IsActive — correct
- Status validation: "Pending" or "Rejected" only — correct
- EvidencePath assigned to ALL selected progress records — confirmed (lines 1750-1754, each iteration)
- Each progress.Status = "Submitted" and SubmittedAt = DateTime.UtcNow — confirmed

**ApproveFromProgress / RejectFromProgress / HCReviewFromProgress:**
- All have [ValidateAntiForgeryToken] — confirmed
- SrSpv/SH guard on Approve/Reject — confirmed
- HC guard on HCReview — confirmed
- All return Json({success, message}) pattern — confirmed

**CoachingProton.cshtml:**
- Filter form: method="get", asp-action="CoachingProton", asp-controller="CDP" — correct
- Pagination: all filter params (bagian, unit, trackType, tahun, coacheeId) preserved on all page links — correct
- CSRF: `@Html.AntiForgeryToken()` in @section Scripts; all AJAX calls include token — correct
- Approval buttons use correct boolean guards (isSrSpv, isSH, isHC, isCoach) — correct
- GetCoacheeDeliverables: not called via AJAX from this view; coaching modal uses buildDeliverableData() from table rows

**Deliverable.cshtml:**
- enctype="multipart/form-data" on upload form — confirmed
- Coaching session renders: date, coach name, result badge, CatatanCoach — confirmed
- Approve/Reject buttons: `@if (Model.CanApprove)` — correct
- HC Review button: `@if (Model.CanHCReview)` — correct
- Evidence download: `href="@Model.Progress.EvidencePath"` — correct raw path

## Issues for Browser Verification (Plan 03)

The following items require browser verification in Plan 03:

1. **Coach can see Submit Evidence buttons** on Pending deliverables (verifies canUpload fix)
2. **UploadEvidence POST** accepts Pending-status records (verifies Bug 2 fix)
3. **Status badge on Deliverable page** shows gray for Pending, blue for Submitted
4. **Deactivated coachees do not appear** in CoachingProton table for HC/Admin/SrSpv

## Self-Check: PASSED

Files modified exist:
- Controllers/CDPController.cs — FOUND
- Views/CDP/Deliverable.cshtml — FOUND

Commits exist:
- 65a06ec — FOUND
- b7a992c — FOUND
