# Phase 160: Assignment Removal - Context

**Gathered:** 2026-03-12
**Status:** Ready for planning

<domain>
## Phase Boundary

Add a "Hapus" button on the CoachCoacheeMapping page for deactivated mappings that permanently deletes the mapping, its ProtonTrackAssignments, and all associated ProtonDeliverableProgress rows. The action requires confirmation and is logged to AuditLog.

</domain>

<decisions>
## Implementation Decisions

### Confirmation UX
- Bootstrap modal with details (not browser confirm() or typed confirmation)
- Modal shows: coachee name, coach name, number of track assignments, number of progress records to be deleted
- Full Indonesian language throughout: "Hapus mapping coach-coachee ini? Data berikut akan dihapus permanen:"
- On success: toast/alert message ("Mapping berhasil dihapus") + row removed from table without full page reload

### Deletion scope
- Hard delete (permanent removal from DB) — no soft delete
- Cascade deletes ALL assignments for the mapping (active + inactive) since mapping is deactivated
- Single SaveChangesAsync call (EF Core implicit transaction) — no explicit transaction needed

### Button placement
- Inline per-row, next to existing Edit/Reaktivasi buttons
- Deactivated row shows: Edit | Reaktivasi | Hapus (both restore and delete available)
- Styling: btn-sm btn-outline-danger with trash icon (bi-trash)
- Button only visible for deactivated mappings (never for active ones)

### Audit logging
- AuditLog actionType: "DeleteMapping"
- Description: summary with counts — "Hapus mapping: Coach [name] → Coachee [name], N track assignments, M progress records deleted"
- DB records only — no new audit log UI page
- Uses existing AuditLogService.LogAsync pattern

### Claude's Discretion
- Exact modal layout and spacing
- AJAX implementation details (fetch vs jQuery)
- Error handling for failed deletions
- How to count assignments/progress for the modal preview (eager load or separate query)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches. Follow existing CoachCoacheeMapping page patterns (deactivate modal is the reference implementation for the Hapus modal).

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AuditLogService.LogAsync`: Existing audit logging with actorUserId, actorName, actionType, description, targetId
- `AuditLog` model: Has ActorUserId, ActorName, ActionType, Description fields
- Deactivate modal in CoachCoacheeMapping.cshtml: Reference pattern for Bootstrap modal + fetch POST + dynamic content

### Established Patterns
- Inline action buttons per-row: Edit (btn-outline-secondary), Deactivate (btn-outline-danger), Reactivate (btn-outline-success)
- Modal confirmation: Bootstrap 5 modal with header, body, footer (Batal + action button)
- AJAX actions: fetch() to controller POST endpoint, then DOM manipulation on success

### Integration Points
- `AdminController.cs`: Add new POST action for delete (alongside existing Deactivate/Reactivate)
- `CoachCoacheeMapping.cshtml`: Add Hapus button in action column + delete modal
- `AuditLogService`: Call LogAsync after successful deletion

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 160-assignment-removal*
*Context gathered: 2026-03-12*
