# Phase 197: Excel Export Helper - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Extract common Excel export boilerplate from AdminController, CMPController, CDPController, and ProtonDataController into a shared ExcelExportHelper class. No behavior or output changes — purely internal refactoring.

</domain>

<decisions>
## Implementation Decisions

### Scope
- Boilerplate only — helper handles worksheet creation with bold headers, AdjustToContents, and MemoryStream → FileContentResult conversion
- Data population remains in each controller action (each export has unique columns/data)
- Simple table pattern only — report-style exports with metadata headers (e.g., ExportAssessmentResults) keep their metadata logic in the controller
- Import actions (XLWorkbook from stream) are NOT in scope — helper is export-only

### API Design
- Static utility methods — no DI registration needed
- Two main methods:
  - CreateSheet(workbook, sheetName, headers) → creates worksheet with bold header row
  - ToFileResult(workbook, filename) → AdjustToContents + MemoryStream → FileContentResult
- Controllers still create XLWorkbook and populate data themselves

### Location & Naming
- File: `Helpers/ExcelExportHelper.cs`
- Class: `ExcelExportHelper` (static class)
- Helpers/ folder already exists (empty), appropriate for static utilities vs Services/ which holds DI services

### Claude's Discretion
- Exact method signatures and parameter types
- Whether to add a convenience overload for single-sheet exports
- How to handle the AdjustToContents call (per-sheet in CreateSheet or globally in ToFileResult)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing export implementations
- `Controllers/AdminController.cs` — ~7 export actions (ExportAssessmentResults line 2808, ExportRecords, ExportAuditLog, ExportSilabus, ExportTraining, ExportHistoriProton, etc.)
- `Controllers/CMPController.cs` — ~4 export actions (ExportRecords line 516, ExportRecordsTeamAssessment line 587, ExportRecordsTeamTraining line 650, ExportCoachCoacheeMapping)
- `Controllers/CDPController.cs` — ~2 export actions (ExportProgressExcel line 2101, ExportDeliverable)
- `Controllers/ProtonDataController.cs` — ~2 export actions (ExportSilabus line 619, ExportHistoriProton line 680)

### Shared service pattern (Phase 196)
- `Services/IWorkerDataService.cs` — Interface pattern reference (but helper uses static, not DI)
- `Services/WorkerDataService.cs` — Implementation pattern reference

### Requirements
- `.planning/REQUIREMENTS.md` — SVC-05: Common Excel export helper extraction requirement

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Helpers/` folder exists (empty) — ready for ExcelExportHelper.cs
- ClosedXML already referenced in HcPortal.csproj

### Established Patterns
- All exports follow same pattern: XLWorkbook → AddWorksheet → Cell headers → Bold → Data loop → AdjustToContents → MemoryStream → File()
- Header row is always row 1 with Style.Font.Bold = true (except ExportAssessmentResults which has metadata rows first)
- All exports use `application/vnd.openxmlformats-officedocument.spreadsheetml.document` MIME type
- Some exports have background color on headers (XLColor.LightBlue) — only ExportAssessmentResults

### Integration Points
- Every export action in 4 controllers will call the helper
- No changes to routes, views, or user-facing behavior

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 197-excel-export-helper*
*Context gathered: 2026-03-18*
