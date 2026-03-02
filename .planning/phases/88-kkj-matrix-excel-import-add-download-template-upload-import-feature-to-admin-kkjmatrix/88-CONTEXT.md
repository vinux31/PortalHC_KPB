# Phase 88: KKJ Matrix Excel Import/Export - Context

**Gathered:** 2026-03-02
**Status:** Ready for planning (blocked by Dynamic Columns phase)

<domain>
## Phase Boundary

Add Excel download-template + upload-import + export features to Admin/KkjMatrix page, following the ImportWorkers pattern. This phase assumes dynamic columns are already implemented (prerequisite phase).

**Prerequisite:** Phase 89 (Dynamic Columns) must complete first — database restructured from 15 fixed Target_* columns to key-value relational model.

</domain>

<decisions>
## Implementation Decisions

### Template Design
- All columns included in template (base columns + all dynamic target columns for the Bagian)
- Include example row (italic gray) + instruction notes (like ImportWorkers)
- Header labels use readable names (e.g., "Section Head", "Operator Process Water"), not technical database names
- One template for all — all target columns present, leave empty/"-" for irrelevant ones

### Duplicate Handling
- Match key: Kompetensi + Bagian combination
- On duplicate: Update existing (overwrite old data with new Excel data)
- Useful for bulk-updating target levels

### Bagian Validation
- Auto-create: if Bagian in Excel doesn't exist in KkjBagian, automatically create new KkjBagian entry
- HC imports define the structure — system follows

### Column Mapping (Flexible)
- Flexible/fuzzy mapping: normalize headers (lowercase, strip spaces/underscores), then check contains match
- If header matches a KkjColumn name for the Bagian, map to that column
- If header is new (e.g., "Operator Process Water"), auto-create new KkjColumn for the Bagian
- On partial match failure: import columns that match, show warning for unmatched columns

### Page & UI
- Separate import page: ImportKkjMatrix.cshtml (like ImportWorkers pattern)
- Buttons in KkjMatrix toolbar: "Import Excel" and "Download Excel" next to existing "Simpan Semua" button
- Result page: detailed table per row with columns Kompetensi, Bagian, Status (Success/Updated/Error), Message

### Export
- Filter per-Bagian: user selects Bagian before downloading
- Export to Excel with readable column headers

### Authorization
- Admin + HC (same as existing KkjMatrix actions: [Authorize(Roles = "Admin, HC")])

### Audit Log
- Import and Export logged to AuditLog (like ImportWorkers)
- Record: who, when, row counts (success/updated/error)

### Claude's Discretion
- PositionTargetHelper adaptation (will be handled in Dynamic Columns phase)
- Exact fuzzy matching algorithm details
- Excel styling and formatting
- Error message wording (Indonesian)

</decisions>

<specifics>
## Specific Ideas

- Follow ImportWorkers pattern exactly: download template button + file upload + process + redirect to list
- Reference implementation: `Controllers/AdminController.cs` (ImportWorkers + DownloadImportTemplate), `Views/Admin/ImportWorkers.cshtml`
- User wants system to follow header names from imported Excel — if HC imports a file with new column headers, system auto-creates those as KkjColumns
- "Setiap bagian memiliki kolom yang berbeda" — different Bagians can have completely different target column sets

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- ImportWorkers pattern: ClosedXML-based template download + upload + validation + result display
- DownloadImportTemplate: Excel generation with styled headers, example rows, instruction notes
- AuditLog service: `_auditLog.LogAsync()` for recording import/export actions
- KkjMatrix controller actions: existing CRUD (KkjMatrixSave, KkjMatrixDelete, KkjBagianSave)

### Established Patterns
- ClosedXML (XLWorkbook) for all Excel operations
- TempData["Error"] for upload errors, ViewBag for result display
- ValidateAntiForgeryToken on all POST actions
- Result model pattern: ImportWorkerResult { Nama, Email, Role, Status, Message }

### Integration Points
- KkjMatrix.cshtml toolbar: add Import/Export buttons alongside existing "Simpan Semua"
- AdminController: new actions alongside existing KkjMatrix region
- KkjBagian: auto-create new bagians during import
- KkjColumn (new, from Dynamic Columns phase): auto-create new columns during import
- KkjTargetValue (new, from Dynamic Columns phase): store imported target values

</code_context>

<deferred>
## Deferred Ideas

- Dynamic Columns schema redesign → Phase 89 (prerequisite, must complete first)
- PositionTargetHelper adaptation for dynamic columns → Phase 89
- Assessment flow updates for dynamic columns → Phase 89
- Template per-Bagian (download template specific to selected Bagian) → future enhancement after dynamic columns

</deferred>

---

*Phase: 88-kkj-matrix-excel-import*
*Context gathered: 2026-03-02*
