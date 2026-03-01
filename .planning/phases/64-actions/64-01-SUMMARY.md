---
phase: 65-actions
plan: 01
subsystem: cdp-proton-approval
tags: [approval, ajax, modal, migration, ef-core, per-role]
dependency_graph:
  requires: [64-02]
  provides: [ApproveFromProgress, RejectFromProgress, HCReviewFromProgress, per-role-approval-schema]
  affects: [ProtonDeliverableProgress, CoachingSession, ProtonProgress view, CDPController]
tech_stack:
  added: []
  patterns: [ajax-fetch-json, bootstrap-modal, bootstrap-toast, bootstrap-tooltip, ef-migration-with-data-fix]
key_files:
  created:
    - Migrations/20260227102013_AddPerRoleApprovalAndCoachingLink.cs
  modified:
    - Models/ProtonModels.cs
    - Models/CoachingSession.cs
    - Models/TrackingModels.cs
    - Controllers/CDPController.cs
    - Views/CDP/ProtonProgress.cshtml
decisions:
  - "Rejection takes overall status precedence — any role rejecting sets Status=Rejected"
  - "Approval columns are fully independent — SrSpv approving does not affect SH column"
  - "HC review endpoint accepts Admin role too (isHC = HC || Admin) for admin simulation"
  - "Data migration resets Locked/Active to Pending and backfills SrSpv fields from existing Approved records"
  - "Role-aware pendingApprovals: SrSpv sees own pending, SH sees own pending, HC sees HC pending, others see total"
metrics:
  duration: ~25 minutes
  completed: 2026-02-27
  tasks_completed: 2
  files_modified: 5
  files_created: 2
requirements: [ACTN-01, ACTN-02]
---

# Phase 65 Plan 01: Actions — Approval Schema and AJAX Endpoints Summary

**One-liner:** Per-role independent approval schema with three AJAX endpoints (ApproveFromProgress, RejectFromProgress, HCReviewFromProgress) wired to a new Tinjau modal with toast and in-place badge updates.

## What Was Built

### Task 1: Schema Migration

**ProtonDeliverableProgress model** received 6 new fields:
- `SrSpvApprovalStatus` (string, default "Pending")
- `SrSpvApprovedById` (nullable string)
- `SrSpvApprovedAt` (nullable DateTime)
- `ShApprovalStatus` (string, default "Pending")
- `ShApprovedById` (nullable string)
- `ShApprovedAt` (nullable DateTime)

Status default changed from "Locked" to "Pending", comment updated.

**CoachingSession** received `ProtonDeliverableProgressId` (nullable int) for phase 65 evidence+coaching modal (Plan 02).

**TrackingItem** received 7 new fields for tooltip data and status-driven button logic:
`SrSpvApproverName`, `SrSpvApprovedAt`, `ShApproverName`, `ShApprovedAt`, `HcReviewerName`, `HcReviewedAt`, `Status`.

**EF Migration** `AddPerRoleApprovalAndCoachingLink` applied with data-fix SQL:
- Converted Locked/Active records to Pending
- Backfilled SrSpvApprovalStatus=Approved from existing Approved records (approver name + date)
- Ensured new string columns default to "Pending" not ""

**CDPController changes:**
- `AssignTrack`: all records created with Status="Pending" (no more sequential Locked/Active)
- `ApproveDeliverable`: removed next-deliverable unlock logic (no more Locked status)
- `ProtonProgress` GET: maps per-role SrSpvApprovalStatus/ShApprovalStatus, builds approver name dictionary for tooltips, implements role-aware pendingApprovals count

### Task 2: AJAX Endpoints + View

**CDPController new actions:**
- `ApproveFromProgress` — validates SrSpv/SH role, section match, Submitted status; sets per-role Approved fields + overall Status=Approved; returns JSON
- `RejectFromProgress` — same validation, requires rejectionReason; sets per-role Rejected fields + overall Status=Rejected; returns JSON
- `HCReviewFromProgress` — validates HC/Admin role; sets HCApprovalStatus=Reviewed; returns JSON

**ProtonProgress.cshtml rewrite:**
- Table now has 8 columns: Kompetensi | Sub Kompetensi | Deliverable | Evidence | Approval Sr. Spv | Approval SH | Approval HC | Detail
- Evidence column: Coach sees "Submit Evidence" button (stub for Plan 02) on Pending/Rejected; others see Sudah Upload / Belum Upload badge
- SrSpv column: Tinjau button appears for SrSpv role when Status=Submitted and SrSpvApprovalStatus=Pending
- SH column: Tinjau button appears for SH role when Status=Submitted and ShApprovalStatus=Pending
- HC column: Review button appears for HC/Admin when Status=Submitted and HCApprovalStatus=Pending
- Detail column: "Lihat Detail" link to Deliverable page
- Tinjau modal: Action dropdown (Approve/Reject), comment field (shown for both, required for Reject), Submit button
- AJAX fetch() handlers update badges in-place after success
- Bootstrap toast notifications on success/failure
- Bootstrap tooltip initialized on all approval badges with approver name + date
- `GetApprovalBadge`, `GetApprovalBadgeWithTooltip`, `GetStatusBadge` @functions helpers

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| Rejection takes overall status precedence | Per CONTEXT.md: "if ANY role rejects, Status=Rejected" |
| Independent per-role columns | Either SrSpv OR SH can approve; no sequential dependency |
| Admin included in HC review | Admin can simulate all roles; consistent with existing HCReviewDeliverable pattern |
| Data migration resets to Pending | Removes Locked/Active distinction entirely as per CONTEXT.md |

## Deviations from Plan

### Auto-fixed Issues

None — plan executed exactly as written, with one minor scope addition:

**[Rule 2 - Missing Functionality] HC review also allows Admin role**
- **Found during:** Task 2 endpoint implementation
- **Issue:** Plan says "HC or Admin" for HCReviewFromProgress; Admin needs this access for testing simulation
- **Fix:** `bool isHC = userRole == UserRoles.HC || userRole == UserRoles.Admin`
- **Files modified:** Controllers/CDPController.cs
- This matches the existing `HCReviewDeliverable` endpoint pattern in the codebase

## Commits

| Task | Hash | Description |
|------|------|-------------|
| 1 | a2e6203 | Schema migration — per-role approval fields, Locked->Pending, CoachingSession FK |
| 2 | 419eab1 | AJAX approval endpoints and ProtonProgress view with Tinjau modal |

## Self-Check

- [x] ProtonDeliverableProgress has SrSpvApprovalStatus, SrSpvApprovedById, SrSpvApprovedAt, ShApprovalStatus, ShApprovedById, ShApprovedAt
- [x] CoachingSession has ProtonDeliverableProgressId
- [x] TrackingItem has SrSpvApproverName, SrSpvApprovedAt, ShApproverName, ShApprovedAt, HcReviewerName, HcReviewedAt, Status
- [x] Migration file exists: 20260227102013_AddPerRoleApprovalAndCoachingLink.cs
- [x] CDPController has ApproveFromProgress, RejectFromProgress, HCReviewFromProgress
- [x] ProtonProgress.cshtml has tinjaModal, actionToast, btnTinjau, btnHcReview
- [x] AssignTrack uses Status="Pending" for all records
- [x] Build passes 0 errors
