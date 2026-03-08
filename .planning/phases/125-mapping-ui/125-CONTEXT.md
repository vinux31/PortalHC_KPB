# Phase 125: Mapping UI - Context

**Gathered:** 2026-03-08
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC can see and set assignment unit/section when managing coach-coachee mappings, with full export support. AssignmentSection and AssignmentUnit fields already exist on CoachCoacheeMapping model (Phase 123).

</domain>

<decisions>
## Implementation Decisions

### List Page Columns
- Replace single "Seksi" column with two new columns: "Bagian Penugasan" and "Unit Penugasan"
- No unit/seksi asal columns needed — system operates within one section under section head
- Final column order: Nama, NIP, Bagian Penugasan, Unit Penugasan, Jabatan, Proton Track, Status, Mulai, Aksi
- When AssignmentSection/Unit is null: show dash (—)
- Coach header row (blue): keep as-is (nama coach + seksi coach)
- Section filter dropdown: keep filtering by home section (no change)

### Assign Modal Fields
- Add "Bagian Penugasan" and "Unit Penugasan" dropdowns after coachee checklist, before Proton Track
- Both fields are required (wajib diisi)
- Default value: auto-fill with coachee's own section/unit
- Bulk assign with mixed sections/units: clear the dropdowns (admin must pick manually)
- Field order: Coach → Filter Seksi → Coachee checklist → Bagian Penugasan → Unit Penugasan → Proton Track → Tanggal Mulai

### Excel Export
- Null AssignmentSection/Unit: show dash (—) in Excel
- Flat layout: each row includes Coach column (no merged cells/grouping)
- Column layout: Claude's discretion (at minimum include Bagian Penugasan and Unit Penugasan)

### Claude's Discretion
- Excel export column ordering and whether to include home section/unit for HR reference
- Edit modal: whether to add AssignmentSection/Unit fields there too (likely yes for consistency)
- Cascading dropdown behavior (unit filtered by section selection)

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- CoachCoacheeMapping.cshtml: Full page with assign/edit/deactivate modals, filter form, grouped table
- AdminController CoachCoacheeMapping actions: list, assign, edit, deactivate, reactivate, export
- Sections dropdown: already populated via ViewBag.Sections
- SweetAlert (Swal): used for reactivate toast notification

### Established Patterns
- Assign modal uses checkbox checklist for multi-coachee selection with section filter
- Edit modal is simpler (single coachee, no checklist)
- Export uses `/Admin/CoachCoacheeMappingExport` endpoint
- All modals use Bootstrap 5 with AJAX fetch for submit
- AntiForgeryToken pattern for all POST requests

### Integration Points
- AdminController.CoachCoacheeMapping (GET): needs to pass Bagian/Unit data per coachee row
- AdminController.CoachCoacheeMappingAssign (POST): needs to accept AssignmentSection/AssignmentUnit
- AdminController.CoachCoacheeMappingExport: needs Bagian/Unit columns
- Views/Admin/CoachCoacheeMapping.cshtml: table columns + modal fields

</code_context>

<specifics>
## Specific Ideas

- "Bagian" = Section field in database, "Unit" = Unit field in database
- System operates within one section under a section head, so home section/unit display is unnecessary
- Bagian Penugasan maps to AssignmentSection, Unit Penugasan maps to AssignmentUnit

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 125-mapping-ui*
*Context gathered: 2026-03-08*
