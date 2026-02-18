---
phase: 06-approval-workflow-completion
plan: 01
subsystem: database
tags: [efcore, sqlserver, migration, models, viewmodels, approval-workflow]

# Dependency graph
requires:
  - phase: 05-proton-deliverable-tracking
    provides: ProtonDeliverableProgress, ProtonTrackAssignment, ProtonDeliverable hierarchy — extended in this plan

provides:
  - ProtonDeliverableProgress with 5 approval fields (RejectionReason, ApprovedById, HCApprovalStatus, HCReviewedAt, HCReviewedById)
  - ProtonNotification entity for HC in-app notifications
  - ProtonFinalAssessment entity for final HC assessment records
  - DeliverableViewModel with CanApprove, CanHCReview, CurrentUserRole fields
  - ProtonPlanViewModel with FinalAssessment nullable property
  - HCApprovalQueueViewModel, FinalAssessmentCandidate, FinalAssessmentViewModel
  - ProtonNotifications and ProtonFinalAssessments DbSets registered
  - AddApprovalWorkflow migration applied — 5 columns + 2 new tables in DB

affects:
  - 06-02 (approval controller actions depend on all models/DbSets)
  - 06-03 (HC review, notification, final assessment controller/views)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "String ID fields (no FK) for user references — ApprovedById, HCReviewedById, CoacheeId, RecipientId, CreatedById all stored as plain strings per CoachingLog/ProtonTrackAssignment pattern"
    - "DeleteBehavior.Restrict on all Proton FKs — avoids SQL Server multiple cascade path limitation"
    - "No navigation properties for string ID fields — prevents EF cascade path conflicts"
    - "No KkjMatrixItem nav property on ProtonFinalAssessment — plain int? KkjMatrixItemId field only"

key-files:
  created:
    - Migrations/20260218001418_AddApprovalWorkflow.cs
    - Migrations/20260218001418_AddApprovalWorkflow.Designer.cs
  modified:
    - Models/ProtonModels.cs
    - Models/ProtonViewModels.cs
    - Data/ApplicationDbContext.cs

key-decisions:
  - "String IDs (no FK) for ApprovedById, HCReviewedById, RecipientId, CreatedById — consistent with all prior Proton entity patterns"
  - "HCApprovalStatus is independent of main Status — HC review is non-blocking per deliverable (APPRV-04)"
  - "No KkjMatrixItem navigation property on ProtonFinalAssessment — dropdown queries KkjMatrices DbSet separately to avoid cascade path"
  - "HCApprovalStatus HasDefaultValue('Pending') set in EF config and as C# default initializer"

patterns-established:
  - "Phase 6 approval entities follow same no-FK-for-user-IDs pattern as Phase 4/5"
  - "ProtonFinalAssessment uses Restrict delete on ProtonTrackAssignment FK — consistent with all Proton FK relationships"

# Metrics
duration: 4min
completed: 2026-02-18
---

# Phase 6 Plan 01: Approval Workflow Data Foundation Summary

**EF Core schema extension with 5 approval columns on ProtonDeliverableProgresses, 2 new tables (ProtonNotifications, ProtonFinalAssessments), and 5 new ViewModels for HC approval queue and final assessment workflow**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-18T00:11:09Z
- **Completed:** 2026-02-18T00:15:00Z
- **Tasks:** 2
- **Files modified:** 5 (3 source + 2 migration)

## Accomplishments
- Extended ProtonDeliverableProgress with 5 approval workflow fields — RejectionReason, ApprovedById, HCApprovalStatus, HCReviewedAt, HCReviewedById
- Created ProtonNotification and ProtonFinalAssessment entities following established no-FK-for-user-IDs pattern
- Added HCApprovalQueueViewModel, FinalAssessmentCandidate, FinalAssessmentViewModel and extended existing DeliverableViewModel and ProtonPlanViewModel
- Applied AddApprovalWorkflow migration — 5 columns added to ProtonDeliverableProgresses, ProtonNotifications and ProtonFinalAssessments tables created with indexes, no cascade path errors

## Task Commits

Each task was committed atomically:

1. **Task 1: Extend ProtonDeliverableProgress and add ProtonNotification + ProtonFinalAssessment models** - `135264c` (feat)
2. **Task 2: Extend ViewModels, register DbSets, configure FKs, apply migration** - `c0b2e35` (feat)

## Files Created/Modified
- `Models/ProtonModels.cs` - Extended ProtonDeliverableProgress with 5 new fields; added ProtonNotification and ProtonFinalAssessment classes
- `Models/ProtonViewModels.cs` - Added CanApprove/CanHCReview/CurrentUserRole to DeliverableViewModel; FinalAssessment to ProtonPlanViewModel; new HCApprovalQueueViewModel, FinalAssessmentCandidate, FinalAssessmentViewModel
- `Data/ApplicationDbContext.cs` - Registered ProtonNotifications/ProtonFinalAssessments DbSets; configured FK with Restrict; added indexes; set HCApprovalStatus default
- `Migrations/20260218001418_AddApprovalWorkflow.cs` - Migration script for all schema changes
- `Migrations/20260218001418_AddApprovalWorkflow.Designer.cs` - EF Core designer snapshot

## Decisions Made
- String IDs (no FK) for ApprovedById, HCReviewedById, RecipientId, CreatedById — consistent with all prior Proton entity patterns (CoachingLog, ProtonTrackAssignment)
- HCApprovalStatus is independent of main Status — HC review is non-blocking per deliverable (APPRV-04); "Pending" default on both C# initializer and EF HasDefaultValue
- No KkjMatrixItem nav property on ProtonFinalAssessment — dropdown queries KkjMatrices DbSet separately to avoid cascade path conflicts
- DeleteBehavior.Restrict on ProtonFinalAssessment -> ProtonTrackAssignment FK — consistent with all Proton FK relationships

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None — no external service configuration required.

## Next Phase Readiness
- All data layer complete — ProtonModels.cs, ProtonViewModels.cs, ApplicationDbContext.cs, and migration applied
- Plans 02 and 03 can now implement controller actions using ProtonNotifications, ProtonFinalAssessments DbSets and all new ViewModels
- HCApprovalQueueViewModel ready for HC approval queue controller (Plan 02)
- FinalAssessmentViewModel ready for final assessment page controller (Plan 03)

---
*Phase: 06-approval-workflow-completion*
*Completed: 2026-02-18*

## Self-Check: PASSED

- FOUND: Models/ProtonModels.cs
- FOUND: Models/ProtonViewModels.cs
- FOUND: Data/ApplicationDbContext.cs
- FOUND: Migrations/20260218001418_AddApprovalWorkflow.cs
- FOUND commit: 135264c (Task 1)
- FOUND commit: c0b2e35 (Task 2)
- Build: PASSED (0 errors)
