# Phase 179: Export & Import Silabus Proton - Context

**Gathered:** 2026-03-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Add Excel export and import to the Silabus Proton tab on `/ProtonData/Silabus`. Admin/HC can export current silabus data as .xlsx and bulk-import new/updated entries from a filled template. Covers the 3-level hierarchy: Kompetensi → SubKompetensi → Deliverable.

</domain>

<decisions>
## Implementation Decisions

### Export Excel Structure
- Flat single sheet with columns: Bagian, Unit, Track, Kompetensi, SubKompetensi, Deliverable, Target — easy roundtrip
- Export only active records (IsActive=true), skip soft-deleted kompetensi
- Export respects current Bagian/Unit/Track filter selection on the page

### Import Template & Validation
- Upsert by matching Kompetensi+SubKompetensi+Deliverable name within same Bagian/Unit/Track — update Target & Urutan if found, create if new
- Template is pre-filled with current data (same as export output) + empty rows at bottom for new entries
- Validation errors shown as result page with success/error summary table (row number + error message) — same pattern as ImportWorkers

### UI Placement
- Toolbar row above silabus table: "Export Excel" (green btn-outline-success) + "Import Excel" (blue) + "Download Template" (outline)
- Import flow on separate page `/ProtonData/ImportSilabus` with file upload + process — matches ImportWorkers pattern
- After successful import, redirect back to Silabus tab with same Bagian/Unit/Track filter preserved

### Claude's Discretion
- Exact column widths, header styling, filename format
- Empty state handling when no data to export
- Urutan auto-assignment for new rows during import

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Export pattern reference
- `Controllers/AdminController.cs` line ~3948 — `ExportWorkers` action (ClosedXML pattern)
- `Controllers/AdminController.cs` line ~4537 — `ImportWorkers` action (import pattern)
- `Controllers/AdminController.cs` line ~4545 — `DownloadImportTemplate` action (template pattern)

### Data source
- `Controllers/ProtonDataController.cs` — `SilabusSave`, `SilabusDelete`, `SilabusDeactivate` actions
- `Models/ProtonModels.cs` — `ProtonKompetensi`, `ProtonSubKompetensi`, `ProtonDeliverable`, `ProtonTrack`

### Views
- `Views/ProtonData/Silabus.cshtml` — Silabus tab view (add export/import buttons here)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- ClosedXML `XLWorkbook` pattern — Used in 6+ existing export actions
- `ImportWorkers` page pattern — file upload + validation + result display
- `SilabusRowDto` — existing DTO for silabus batch operations

### Established Patterns
- Export: `public async Task<IActionResult> ExportXxx(filters)` returning `File(stream, contentType, filename)`
- Import: GET shows upload form, POST processes file, displays result with success/error counts
- Template download: GET returns pre-built .xlsx with headers + example row

### Integration Points
- New export/import actions added to `ProtonDataController` (same controller as Silabus)
- Export/import buttons added to existing Silabus.cshtml view toolbar
- Import page at `/ProtonData/ImportSilabus`

</code_context>

<specifics>
## Specific Ideas

No specific requirements — follows established export/import patterns from ManageWorkers.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 179-export-import-silabus-proton*
*Context gathered: 2026-03-16*
