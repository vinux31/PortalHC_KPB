---
phase: 06-approval-workflow-completion
plan: 03
subsystem: api
tags: [aspnet, razor, rbac, hc-workflow, final-assessment, efcore, competency-level]

# Dependency graph
requires:
  - phase: 06-approval-workflow-completion
    plan: 02
    provides: ApproveDeliverable/RejectDeliverable POSTs, CreateHCNotificationAsync, Deliverable.cshtml approval UI, HCApprovalStatus fields on progress records

provides:
  - HCReviewDeliverable POST — HC-only, transitions HCApprovalStatus Pending->Reviewed with guard
  - HCApprovals GET — HC queue with pending reviews, unread notifications, ready-for-assessment list
  - CreateFinalAssessment GET — loads track assignment, deliverable counts, AllHCReviewed flag, KKJ dropdown
  - CreateFinalAssessment POST — dual guard (pending reviews + duplicate), creates ProtonFinalAssessment, upserts UserCompetencyLevel
  - HCApprovals.cshtml — full HC workflow page: notifications, pending reviews table, ready-for-assessment cards
  - CreateFinalAssessment.cshtml — three-state view: read-only if exists, warning if incomplete, create form
  - PlanIdp.cshtml Coachee branch — final assessment card with CompetencyLevelGranted, Status, CompletedAt, Notes

affects:
  - Completes Proton lifecycle: HC reviews deliverables, creates final assessment, coachee sees competency level

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Three-state view pattern for CreateFinalAssessment: ExistingAssessment (read-only) / !AllHCReviewed (warning) / ready (create form)"
    - "HCApprovals queue: notifications loaded before marking-read, viewModel built first so unread state visible on first visit"
    - "UserCompetencyLevel upsert: TargetLevel set to competencyLevelGranted when creating new record (Proton is targeted certification, no TargetLevel on KkjMatrixItem)"
    - "Batch coachee name lookup extended to readyForAssessment candidates — avoids N+1 across loop"

key-files:
  created:
    - Views/CDP/HCApprovals.cshtml
    - Views/CDP/CreateFinalAssessment.cshtml
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/PlanIdp.cshtml

key-decisions:
  - "TargetLevel on new UserCompetencyLevel set to competencyLevelGranted — KkjMatrixItem has no TargetLevel property (only string columns per position type); Proton is a targeted certification so achieved level = target"
  - "Index.cshtml HCApprovals link skipped — CDPController.Index action returns View() with no ViewBag.UserRole; HC navigates via URL or future navbar integration"
  - "Notifications marked read after viewModel built but before return — HC sees unread notifications on first visit as intended"
  - "readyForAssessment loop uses batch-extended userNames dict — avoids FindAsync per candidate when name already fetched from pendingReviews batch"

patterns-established:
  - "Full HC workflow on single HCApprovals page: pending reviews + notifications + final assessment candidates — complete without multi-page navigation"
  - "CreateFinalAssessment guard order: role check -> pending HC reviews -> duplicate assessment -> level validation"

# Metrics
duration: 4min
completed: 2026-02-18
---

# Phase 6 Plan 03: HC Workflow — HCApprovals Queue, Final Assessment, Coachee PROTN-08 Summary

**HC review queue (HCApprovals.cshtml), final assessment creation form (CreateFinalAssessment.cshtml), and Coachee PlanIdp final assessment card — completing the full Proton lifecycle**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-18T02:27:35Z
- **Completed:** 2026-02-18T02:31:39Z
- **Tasks:** 2
- **Files modified:** 4 (2 source modified + 2 views created)

## Accomplishments
- HCReviewDeliverable POST: HC-only action transitions HCApprovalStatus from Pending to Reviewed with guard for already-reviewed records; redirects to HCApprovals queue
- HCApprovals GET: queries pending reviews (HCApprovalStatus Pending + Status in Submitted/Approved/Rejected), loads unread notifications, builds readyForAssessment list by checking all progresses Approved + all HCApprovalStatus Reviewed + no existing final assessment; marks notifications read after building viewModel
- CreateFinalAssessment GET: loads track assignment, computes TotalDeliverables/ApprovedDeliverables/AllHCReviewed, loads KKJ dropdown, checks ExistingAssessment
- CreateFinalAssessment POST: enforces two guards (pending HC reviews and duplicate assessment), validates competencyLevelGranted 0-5, creates ProtonFinalAssessment record, upserts UserCompetencyLevel when kkjMatrixItemId provided (TargetLevel = competencyLevelGranted)
- HCApprovals.cshtml: three sections — notifications (dismissible info alerts), pending reviews table (Periksa link + TandaiDiperiksa POST button), ready-for-assessment cards with CreateFinalAssessment link
- CreateFinalAssessment.cshtml: three-state view — read-only card if assessment exists, warning alert + info section if HC reviews incomplete, create form with competency level selector + optional KKJ dropdown + notes textarea
- PlanIdp.cshtml Coachee branch: final assessment card (PROTN-08) showing CompetencyLevelGranted as large display number, Status badge, CompletedAt date, and Notes when present

## Task Commits

Each task was committed atomically:

1. **Task 1: Add HCReviewDeliverable, HCApprovals, CreateFinalAssessment GET+POST to CDPController** - `ae77064` (feat)
2. **Task 2: Create HCApprovals, CreateFinalAssessment views and extend PlanIdp** - `073d02e` (feat)

## Files Created/Modified
- `Controllers/CDPController.cs` - Four new action methods (HCReviewDeliverable POST, HCApprovals GET, CreateFinalAssessment GET, CreateFinalAssessment POST); PlanIdp Coachee path extended to load and pass FinalAssessment
- `Views/CDP/HCApprovals.cshtml` - HC workflow queue page with notifications, pending reviews table, ready-for-assessment card list
- `Views/CDP/CreateFinalAssessment.cshtml` - Three-state final assessment view (read-only/warning/create form)
- `Views/CDP/PlanIdp.cshtml` - Final assessment result card added to Coachee branch (PROTN-08)

## Decisions Made
- TargetLevel on new UserCompetencyLevel record set to competencyLevelGranted — KkjMatrixItem has no TargetLevel property (only position-specific string columns like Target_Panelman_GSH_12_13); Proton is a targeted certification so achieved level equals intended target
- CDPController.Index link for HCApprovals skipped — Index action returns View() with no ViewBag.UserRole; plan specified to skip if role check not straightforward; HC can navigate via URL or future navbar integration
- Notifications marked read after viewModel built — ensures HC sees new notifications as "unread" on first page visit, then they're cleared on subsequent visits
- readyForAssessment loop extends the batch-built userNames dictionary rather than issuing separate queries — avoids N+1 when candidate not in pendingReviews coacheeIds

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- Phase 6 complete: all APPRV-04, PROTN-06, PROTN-07, PROTN-08 requirements satisfied
- Full Proton lifecycle now operational: submission (Phase 5) -> approval/rejection (Phase 6 Plan 02) -> HC review (Phase 6 Plan 03) -> final assessment (Phase 6 Plan 03) -> coachee sees result (Phase 6 Plan 03)
- Phase 7 (DASH-04 competency progress charts) can now read UserCompetencyLevel records updated via Proton final assessments

---
*Phase: 06-approval-workflow-completion*
*Completed: 2026-02-18*

## Self-Check: PASSED

- FOUND: Controllers/CDPController.cs
- FOUND: Views/CDP/HCApprovals.cshtml
- FOUND: Views/CDP/CreateFinalAssessment.cshtml
- FOUND: Views/CDP/PlanIdp.cshtml
- FOUND commit: ae77064 (Task 1)
- FOUND commit: 073d02e (Task 2)
- Build: PASSED (0 errors, 34 pre-existing warnings)
