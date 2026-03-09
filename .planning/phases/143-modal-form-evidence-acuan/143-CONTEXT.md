# Phase 143: Modal Form Evidence Acuan - Context

**Gathered:** 2026-03-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Add 4 "Acuan" textarea fields (Pedoman, TKO/TKI/TKPA, Best Practice, Dokumen) to the existing evidence coaching modal. Persist to database via new CoachingSession properties + migration. Display saved Acuan data on Deliverable detail page.

</domain>

<decisions>
## Implementation Decisions

### Modal Placement
- Acuan card appears after Date field, before Catatan Coach
- Flow: Deliverable selector → Tanggal → **Acuan card** → Catatan Coach → Kesimpulan → Result → File Evidence

### Card Styling
- Bootstrap card with light border, card-header titled "Acuan", bg-light background
- Not collapsible — always visible when modal opens

### Field Requirements
- All 4 textareas are optional — coach fills whichever are relevant
- No placeholder text — labels are self-explanatory
- 2 rows per textarea (compact, coach can still type multi-line)

### Edit Behavior
- Follow existing edit rules for Catatan Coach — no special handling for Acuan fields

### DB Column Naming
- Properties: AcuanPedoman, AcuanTko, AcuanBestPractice, AcuanDokumen
- All nullable string, no max length constraint

### Deliverable Detail Display
- Add 4 new rows above Catatan Coach in the existing session table
- Only show rows that have data (skip empty Acuan fields)
- Same styling as existing rows (text-muted label, pre-line value)

### Claude's Discretion
- Exact textarea CSS styling within the card
- Migration naming convention
- JS submit handler implementation details

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches within the decisions above.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- Evidence modal: `Views/CDP/CoachingProton.cshtml` line 848-906 — add Acuan card here
- CoachingSession model: `Models/CoachingSession.cs` — add 4 new string properties
- Deliverable detail: `Views/CDP/Deliverable.cshtml` line 318-337 — add rows to session table

### Established Patterns
- Modal fields use `mb-3` wrapper with `form-label` and `form-control` classes
- JS submit uses FormData with `formData.append()` for each field (line 1376+)
- Detail page uses `table-sm table-borderless` with `text-muted` labels

### Integration Points
- Controller: `CDPController.cs` — SubmitEvidence action needs 4 new parameters
- JS: Evidence submit handler around line 1271 needs to append 4 new fields
- EF migration for 4 new nullable string columns on CoachingSessions table

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 143-modal-form-evidence-acuan*
*Context gathered: 2026-03-09*
