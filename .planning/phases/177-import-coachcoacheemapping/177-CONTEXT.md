# Phase 177: Import CoachCoacheeMapping - Context

**Gathered:** 2026-03-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC can bulk-create coach-coachee mappings from an Excel file instead of assigning one-by-one. Includes downloadable template and file upload with validation. Does not change existing single-assign UI or other CoachCoacheeMapping management features.

</domain>

<decisions>
## Implementation Decisions

### Template columns
- Two columns only: NIP Coach, NIP Coachee
- StartDate defaults to today, IsActive defaults to true
- One example row with placeholder NIP values (e.g., 123456, 789012)
- Plain text entry — no reference sheet or dropdowns

### Duplicate handling
- Active duplicate (same coach-coachee pair already active): skip and report in summary
- Inactive duplicate (pair exists but IsActive=false): reactivate — set IsActive=true, StartDate=today
- No overwriting or creating parallel records

### Error display
- Summary page with row numbers after processing: X imported, Y skipped, Z errors
- Per-row error details (unknown NIP, missing data, etc.) with row numbers
- Partial import: valid rows are imported even if some rows have errors
- Follows ImportWorkers result display pattern

### UI placement
- Import Excel and Download Template buttons in top toolbar, next to existing Assign button
- File upload via modal dialog (click Import Excel → modal with file picker + Upload button)
- Results shown as summary on same page after processing
- No separate import page needed

### Claude's Discretion
- Modal styling and layout
- Summary display format (table vs list)
- File size limit
- Excel parsing library choice (ClosedXML vs EPPlus — consistent with existing ImportWorkers)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Import pattern reference
- `Controllers/AdminController.cs` §ImportWorkers (line ~4270) — Reference implementation for download template + upload + process pattern
- `Controllers/AdminController.cs` §DownloadImportTemplate (line ~4281) — Template generation pattern
- `Views/Admin/ImportWorkers.cshtml` — Import UI reference (though this phase uses modal instead of separate page)

### Data model
- `Models/CoachCoacheeMapping.cs` — Target model: Id, CoachId, CoacheeId, IsActive, StartDate, EndDate
- `Controllers/AdminController.cs` §CoachCoacheeMapping (line ~2961) — Existing list page where import buttons will be added
- `Controllers/AdminController.cs` §CoachCoacheeMappingAssign (line ~3083) — Existing assign logic for duplicate-checking reference

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- ImportWorkers/DownloadImportTemplate: complete import pattern with Excel generation, file upload, validation, and error reporting
- CoachCoacheeMappingAssign action: existing duplicate-check logic (checks existing active mappings before creating)
- AuditLog integration pattern already in CoachCoacheeMappingAssign

### Established Patterns
- Excel import uses ClosedXML or EPPlus (check ImportWorkers for which library)
- Authorization: Admin/HC actions use `[Authorize(Roles = "Admin, HC")]`
- CoachId/CoacheeId are ApplicationUser.Id strings, looked up via NIP

### Integration Points
- CoachCoacheeMapping page (`Admin/CoachCoacheeMapping`) — add toolbar buttons
- `_context.CoachCoacheeMappings` DbSet — insert new records
- `_context.Users` — NIP-to-UserId lookup for validation
- AuditLog — log import action

</code_context>

<specifics>
## Specific Ideas

- Follow the ImportWorkers pattern closely — user explicitly prefers this approach
- Modal dialog for upload keeps user on the CoachCoacheeMapping page (no navigation away)
- Reactivation of inactive pairs is a key behavior — not just skip

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 177-import-coachcoacheemapping*
*Context gathered: 2026-03-16*
