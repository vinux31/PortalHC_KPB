---
phase: 06-approval-workflow-completion
plan: 02
subsystem: api
tags: [aspnet, razor, rbac, approval-workflow, efcore]

# Dependency graph
requires:
  - phase: 06-approval-workflow-completion
    plan: 01
    provides: ProtonDeliverableProgress with RejectionReason/ApprovedById/HCApprovalStatus fields, ProtonNotification entity, DeliverableViewModel with CanApprove/CanHCReview/CurrentUserRole, ProtonNotifications DbSet

provides:
  - ApproveDeliverable POST action — SrSpv/SectionHead only, Submitted->Approved, sequential unlock, HC notification trigger
  - RejectDeliverable POST action — SrSpv/SectionHead only, Submitted->Rejected with written RejectionReason
  - CreateHCNotificationAsync private helper — deduplication guard, fan-out to all HC users
  - Deliverable GET extended — role fetched, CanApprove/CanHCReview/CurrentUserRole set on ViewModel, HC exempt from section check
  - Deliverable.cshtml approval/rejection UI — Setujui+Tolak buttons (SrSpv/SectionHead), collapsible rejection form with required textarea
  - Deliverable.cshtml rejection reason display — visible to all roles when Status==Rejected
  - Deliverable.cshtml HC review button — POST to HCReviewDeliverable (SrSpv/SectionHead, Pending state)
  - Deliverable.cshtml HC review status indicator — shown inside Approved notice when HCApprovalStatus==Reviewed

affects:
  - 06-03 (HCReviewDeliverable POST action stubbed in view, Plan 03 must add HC review and final assessment controller actions)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "All-approved check uses in-memory state (Status set before SaveChangesAsync) — avoids stale EF tracking issue"
    - "CreateHCNotificationAsync deduplication: AnyAsync on CoacheeId+Type before inserting — idempotent fan-out"
    - "Sequential unlock: find approved record index in ordered list, set next Locked record to Active — single query covers full track"
    - "Approval/rejection forms use Bootstrap collapse for reject form — Setujui always visible, Tolak expands inline textarea"
    - "Section check on approval/rejection mirrors UploadEvidence pattern — query coachee.Section, Forbid if mismatch"

key-files:
  created: []
  modified:
    - Controllers/CDPController.cs
    - Views/CDP/Deliverable.cshtml

key-decisions:
  - "CreateHCNotificationAsync implemented fully in Plan 02 (not a stub) — simple enough to colocate with ApproveDeliverable; Plan 03 does not need to touch it"
  - "HC role (RoleLevel 2) exempted from section check in Deliverable GET — HC reviews all sections; coaches/supervisors still section-checked"
  - "In-memory all-approved check: orderedProgresses includes current record with Status already set to 'Approved' before SaveChangesAsync — avoids checking stale DB state"
  - "RejectDeliverable clears ApprovedById and ApprovedAt on rejection — prevents stale approval data if record was previously approved then re-submitted"

patterns-established:
  - "Phase 6 approval pattern: role check (Forbid) -> status guard (TempData+redirect) -> section check (Forbid) -> mutate -> SaveChanges -> TempData+redirect"
  - "Fan-out notification via GetUsersInRoleAsync + Select -> AddRange — single SaveChangesAsync after batch insert"

# Metrics
duration: 7min
completed: 2026-02-18
---

# Phase 6 Plan 02: Approve/Reject Workflow and Deliverable UI Summary

**ApproveDeliverable/RejectDeliverable POST actions with sequential unlock and HC fan-out notification, plus role-conditional Approve/Reject/HC-Review UI in Deliverable.cshtml**

## Performance

- **Duration:** 7 min
- **Started:** 2026-02-18T00:23:15Z
- **Completed:** 2026-02-18T00:30:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- ApproveDeliverable POST: transitions Submitted->Approved, finds next Locked deliverable in ordered sequence and sets it Active, checks all-approved using in-memory state and triggers CreateHCNotificationAsync if all done
- RejectDeliverable POST: transitions Submitted->Rejected with written RejectionReason, clears ApprovedById/ApprovedAt, section-checked same as Approve
- CreateHCNotificationAsync private helper: deduplication guard (AnyAsync on coachee+type), fans out ProtonNotification records to all HC users in a single AddRange call
- Deliverable GET extended: user role fetched, CanApprove (SrSpv/SectionHead + Submitted), CanHCReview (HC + HCApprovalStatus==Pending), CurrentUserRole set on ViewModel; HC exempted from section check
- Deliverable.cshtml: approval section (Setujui button + collapsible Tolak form with required textarea), rejection reason display, HC review button, HC review status indicator inside Approved notice — all role-conditional

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ApproveDeliverable, RejectDeliverable POSTs and extend Deliverable GET** - `01dbcd7` (feat)
2. **Task 2: Extend Deliverable.cshtml with approval/rejection UI and HC review button** - `26407b2` (feat)

## Files Created/Modified
- `Controllers/CDPController.cs` - ApproveDeliverable POST, RejectDeliverable POST, CreateHCNotificationAsync helper; Deliverable GET extended with role fetch and CanApprove/CanHCReview/CurrentUserRole ViewModel fields; HC section-check exemption
- `Views/CDP/Deliverable.cshtml` - Approval actions section (Setujui + collapsible Tolak with textarea), rejection reason display block, HC review button, HC review status indicator in Approved notice

## Decisions Made
- CreateHCNotificationAsync implemented fully in Plan 02, not as a stub — the logic is simple (deduplication check + fan-out) and colocates cleanly with ApproveDeliverable; Plan 03 does not need to revisit it
- HC role exempted from section check in Deliverable GET — HC reviews deliverables across all sections, so the existing coach section-gate must not apply
- In-memory all-approved check before SaveChangesAsync — orderedProgresses contains the current record with Status already mutated to "Approved" in memory, so checking AllDeliverablesComplete before saving gives the correct answer without a second DB round-trip
- RejectDeliverable clears ApprovedById and ApprovedAt — prevents stale approval metadata if the record was previously approved then re-submitted and then rejected

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- ApproveDeliverable, RejectDeliverable, and CreateHCNotificationAsync are complete — Plan 03 does not need to modify these actions
- Deliverable.cshtml HC review button POSTs to HCReviewDeliverable — Plan 03 must add that POST action to CDPController
- Plan 03 scope: HCReviewDeliverable POST (HC only, HCApprovalStatus Pending->Reviewed), HC Approval Queue page, and Final Assessment controller/views

---
*Phase: 06-approval-workflow-completion*
*Completed: 2026-02-18*

## Self-Check: PASSED

- FOUND: Controllers/CDPController.cs
- FOUND: Views/CDP/Deliverable.cshtml
- FOUND commit: 01dbcd7 (Task 1)
- FOUND commit: 26407b2 (Task 2)
- Build: PASSED (0 errors, 32 pre-existing warnings)
