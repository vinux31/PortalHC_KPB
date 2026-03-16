# Phase 180: Import Training & Export HistoriProton - Context

**Gathered:** 2026-03-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Two standalone features: (1) Bulk-import training records from Excel on the RecordsTeam page, and (2) Export HistoriProton worker summary list as .xlsx. Both follow established ClosedXML export/import patterns.

</domain>

<decisions>
## Implementation Decisions

### Import Training — Template & Columns
- Template columns: NIP (user lookup), Judul, Kategori, Tanggal, Penyelenggara, Status, ValidUntil, NomorSertifikat
- User lookup by NIP (unique identifier) — error row if NIP not found in system
- Create-only mode — training records are unique events, no natural key for upsert

### Export HistoriProton
- Export the list view (worker summary) — one row per worker: NIP, Nama, Section, Unit, Jalur, Tahun1, Tahun2, Tahun3, Status
- Export respects current filters (search, section, unit, jalur, status) active on the page
- Tahun1/2/3 column values: "Lulus" / "Dalam Proses" / "-" (matching view badges)

### UI Placement
- Import Training: buttons on RecordsTeam Training tab toolbar — "Import Excel" (blue) + "Download Template" (outline), same pattern as ManageWorkers
- Export HistoriProton: green "Export Excel" button on HistoriProton page toolbar next to filters
- Import Training flow: separate page `/CMP/ImportTraining` with file upload + result display

### Claude's Discretion
- Exact column widths, header styling, filename format
- Empty state handling when no data to export
- Error message wording for invalid rows
- Template example row content

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Export pattern reference
- `Controllers/AdminController.cs` line ~3948 — `ExportWorkers` action (ClosedXML pattern)
- `Controllers/AdminController.cs` line ~4537 — `ImportWorkers` action (import pattern)
- `Controllers/AdminController.cs` line ~4545 — `DownloadImportTemplate` action (template pattern)

### Data source — Training
- `Models/TrainingRecord.cs` — TrainingRecord model (UserId, Judul, Kategori, Tanggal, Penyelenggara, Status, ValidUntil, NomorSertifikat, etc.)
- `Controllers/CMPController.cs` — RecordsTeam action, GetUnifiedRecords

### Data source — HistoriProton
- `Controllers/CDPController.cs` line ~2608 — `HistoriProton` action (worker summary query)
- `Models/HistoriProtonViewModel.cs` — HistoriProtonViewModel, HistoriProtonWorkerRow

### Views
- `Views/CMP/RecordsTeam.cshtml` — RecordsTeam view (add import buttons to Training tab)
- `Views/CDP/HistoriProton.cshtml` — HistoriProton view (add export button)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- ClosedXML `XLWorkbook` pattern — Used in 7+ existing export actions
- `ImportWorkers` page pattern — file upload + validation + result display
- `HistoriProtonWorkerRow` — existing ViewModel with all needed worker summary fields
- `TrainingRecord` model — well-defined with all fields for import

### Established Patterns
- Export: `public async Task<IActionResult> ExportXxx(filters)` returning `File(stream, contentType, filename)`
- Import: GET shows upload form, POST processes file, displays result with success/error counts
- Template: GET returns pre-built .xlsx with headers + example row

### Integration Points
- Import Training actions added to `CMPController` (same controller as RecordsTeam)
- Export HistoriProton action added to `CDPController` (same controller as HistoriProton)
- Import page at `/CMP/ImportTraining`

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

*Phase: 180-import-training-export-historiproton*
*Context gathered: 2026-03-16*
