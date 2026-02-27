# Phase 50: Coach-Coachee Mapping Manager - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin can view, create, edit, and delete Coach-Coachee mappings through a dedicated management page at /Admin/CoachCoacheeMapping. Includes optional Proton Track assignment (merged from Phase 51). Export to Excel. Audit log integration.

</domain>

<decisions>
## Implementation Decisions

### Table layout & display
- Grouped by Coach — each coach is a collapsible section header
- Coach header shows: Name, Section, active coachee count (e.g. "Rino — RFCC — 3 coachee")
- Collapsible: click coach header to expand/collapse coachee list
- Coachee row columns: Name, NIP, Section, Position, Status, StartDate, Actions
- Default: show active mappings only; toggle/checkbox to include inactive
- Section filter dropdown + text search (coach/coachee name)
- Pagination: 20 coach groups per page

### Assign flow
- Modal form triggered by "Tambah Mapping" button
- Coach dropdown: only users with Spv, SrSpv, SectionHead, HC, or Admin role
- Coachee multi-select: section-filtered picker, excludes users who already have an active coach mapping
- Optional Proton Track dropdown (Panelman Tahun 1/2/3, Operator Tahun 1/2/3) — can be left empty
- StartDate: default today, can be changed by Admin
- If coachee already has ProtonTrackAssignment, new selection overwrites the old track
- Bulk assign: pick one coach, select multiple coachees at once

### Edit flow
- Edit via modal (click edit on coachee row)
- Can change: coach, track, StartDate
- Same validation rules as assign

### Unassign & status
- Soft delete: set IsActive=false + EndDate=today (record preserved)
- Confirmation modal shows active coaching session count before deactivation
- Inactive mappings visible via "show all" toggle
- Reactivate button on inactive mappings: sets IsActive=true, clears EndDate

### Validation rules
- 1 coachee = 1 active coach (no duplicate active mappings for same coachee)
- No self-assign (CoachId != CoacheeId)
- No max coachee limit per coach
- Coach eligible roles: Spv, SrSpv, SectionHead, HC, Admin
- Coachee: all users eligible (any role)

### Audit & export
- AuditLogService logs every assign/edit/deactivate/reactivate action
- Export Excel: download all mappings (active + inactive) to spreadsheet

### Admin/Index card
- Section B: Operasional
- Card label: "Coach-Coachee Mapping"

### Claude's Discretion
- Track assignment behavior on deactivate (keep track or remove)
- Exact modal layout and field ordering
- Export Excel column structure
- Empty state design (no mappings yet)

</decisions>

<specifics>
## Specific Ideas

- Track selection merged from Phase 51 scope — optional dropdown in assign/edit modal
- Track overwrite policy: new track replaces existing ProtonTrackAssignment
- Grouped-by-coach display similar to ManageAssessment peserta collapse pattern

</specifics>

<deferred>
## Deferred Ideas

- Phase 51 (Proton Track Assignment Manager) — absorbed into this phase as optional track selection. Phase 51 can be removed from roadmap.

</deferred>

---

*Phase: 50-coach-coachee-mapping-manager*
*Context gathered: 2026-02-27*
