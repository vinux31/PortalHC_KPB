# Phase 117: Status History - Context

**Gathered:** 2026-03-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Create a `DeliverableStatusHistory` table that permanently records every deliverable status change with actor, timestamp, and reason. This is data-layer only — the timeline UI is Phase 119.

</domain>

<decisions>
## Implementation Decisions

### Actor data capture
- Store User ID + cached display name + cached display role per entry
- Role labels use display format: "Sr. Supervisor", "Section Head", "HC", "Coach"
- No general notes field — only rejection entries carry a reason string

### Backfill strategy
- No retroactive history entries for existing data
- All new status changes after migration go live are recorded, including actions on pre-existing deliverables

### History entry scope
- No "Created" entry — history starts from first meaningful action (Submitted)
- Each per-role action = 1 separate entry (HIST-03)
- Status types recorded:
  - Submitted
  - SrSpv Approved
  - SH Approved
  - HC Reviewed
  - SrSpv Rejected (carries RejectionReason)
  - SH Rejected (carries RejectionReason)
  - Re-submitted
- Total: 7 distinct status types
- Concurrent approvals: each action is its own entry, ordered by timestamp naturally (no priority logic)

### Rejection reason handling
- RejectionReason stored in BOTH history entry AND ProtonDeliverableProgress (backward compat)
- On re-submit: ProtonDeliverableProgress.RejectionReason is cleared (set to null)
- History preserves all past rejection reasons permanently (HIST-02)

### Ordering
- Sort by Timestamp ASC, tie-break by Id — sufficient for Phase 119 timeline
- No extra SortOrder column needed

### Claude's Discretion
- Table schema details (column types, indexes, constraints)
- Migration implementation approach
- Where to insert history-recording calls in CDPController (all status-change points)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ProtonDeliverableProgress` model (Models/ProtonModels.cs:85): Has Status, per-role approval fields, RejectionReason, timestamps
- `CoachingSession` model: Related entity linked via ProtonDeliverableProgressId

### Established Patterns
- No FK constraint pattern: Actor IDs stored as strings without foreign key (matches CoachingLog, CoachCoacheeMapping)
- Status as string enum: "Pending", "Submitted", "Approved", "Rejected" (not C# enum)
- Per-role approval: SrSpvApprovalStatus, ShApprovalStatus, HCApprovalStatus as independent fields

### Integration Points
- CDPController.cs: Status changes happen in multiple actions:
  - SubmitEvidence / SubmitEvidenceWithCoaching (Submitted, Re-submitted)
  - ApproveDeliverable / ApproveDeliverableJson (SrSpv/SH Approved)
  - RejectDeliverable / RejectDeliverableJson (SrSpv/SH Rejected)
  - HCReviewDeliverable actions (HC Reviewed)
- ApplicationDbContext.cs: DbSet registration needed
- Migration: New table, no column changes to existing tables

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 117-status-history*
*Context gathered: 2026-03-07*
