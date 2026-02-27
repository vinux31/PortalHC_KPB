# Phase 52: DeliverableProgress Override - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC can view all ProtonDeliverableProgress records and override stuck or erroneous statuses. Also removes sequential lock logic (parallel with Phase 65 v2.4 work) — all deliverables become Active on assignment instead of Locked.

Scope: Override page + lock removal. New approval actions on Progress page are Phase 65.

</domain>

<decisions>
## Implementation Decisions

### Record Discovery
- Tab ke-3 di /ProtonData: "Coaching Proton Override" (after Silabus and Coaching Guidance)
- Tab only visible/rendered for Admin and HC roles
- Filter cascade: Bagian → Unit → Track (consistent with Silabus tab pattern)
- Per-worker rows: one row per worker, deliverable badges as sub-columns showing status
- Additional filter dropdown above table: Semua / Hanya Rejected / Hanya Pending HC (filter by status to focus on problematic records)

### Table & Badge Display
- Badge colors: Active=blue, Submitted=yellow, Approved=green, Rejected=red
- No "Locked" status — removed (see Lock Removal below)
- All badges are clickable (including Approved — admin may need to revert)
- Clicking any badge opens Bootstrap modal with full context + override form

### Override Modal Content
- Full context display: current status, evidence file (download link), all timestamps (SubmittedAt/ApprovedAt/RejectedAt), RejectionReason, HCApprovalStatus, ApprovedById/HCReviewedById
- Override form with 2 dropdowns:
  1. Status utama: Active / Submitted / Approved / Rejected
  2. HC Status: Pending / Reviewed
- Editable RejectionReason textarea (can edit or clear)
- Mandatory "Alasan Override" textarea — required before save
- Single "Simpan Override" button — no double confirmation needed

### Override Behavior
- Auto-fill timestamps on status change:
  - → Approved: set ApprovedAt=now, ApprovedById=current admin/HC user
  - → Rejected: set RejectedAt=now
  - → Submitted: set SubmittedAt=now
  - → Active: clear ApprovedAt, RejectedAt, SubmittedAt timestamps
- Override reason logged to AuditLog (existing v1.7 AuditLogService)
- Individual override only — no bulk operations

### Override Access
- Admin AND HC roles can access tab and perform overrides
- Both roles have identical capabilities on this page

### Sequential Lock Removal (parallel with Phase 65)
- Remove "Locked" status entirely from the system
- CDPController AssignTrack: all deliverables created as "Active" (not first=Active, rest=Locked)
- CDPController Deliverable(): remove sequential lock check (lines 817-830)
- ProtonProgress stats: remove Locked from status counts and chart labels
- Override dropdown: no Locked option
- Existing "Locked" records in DB: bulk-update to "Active" (migration or startup seed)

### Claude's Discretion
- Exact modal layout and spacing
- AJAX vs full page reload on save
- Loading spinner behavior
- Error message display format

</decisions>

<specifics>
## Specific Ideas

- Filter cascade should share the same Bagian/Unit/Track dropdowns pattern used in Silabus tab
- Badge style should match Bootstrap badge classes (bg-primary, bg-warning, bg-success, bg-danger)
- AuditLog entry format: "Override deliverable progress #{id}: {oldStatus} → {newStatus}. Alasan: {reason}"

</specifics>

<deferred>
## Deferred Ideas

- Bulk override (select multiple records, override all at once) — future enhancement if needed
- Override history log visible in modal (who overrode what when) — separate from AuditLog page
- Worker reassignment/transfer handling (orphan progress records) — future phase

</deferred>

---

*Phase: 52-deliverableprogress-override*
*Context gathered: 2026-02-27*
