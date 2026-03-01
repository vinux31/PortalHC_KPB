# Phase 48: KKJ-IDP Mapping Editor - Context

**Gathered:** 2026-02-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC can view, create, edit, and delete KKJ-IDP mapping data (formerly "CPDP Items") through a dedicated management page in Kelola Data. Data is organized per Bagian with dynamic columns. CMP/Mapping read-only view is updated to match the new format and stays synced with this data.

**Rename:** "CPDP Items Manager" → "KKJ-IDP Mapping Editor"

</domain>

<decisions>
## Implementation Decisions

### Bagian Selection
- Dropdown filter at top of table (not card selection or tabs)
- Switching bagian reloads table data and column headers for that bagian
- Bagian list: RFCC, DHT/HMU, NGP, GAST (same as existing)

### Table Layout & Columns
- Grouped by Kompetensi using rowspan — multiple rows share one Kompetensi cell
- Columns are **fully dynamic per bagian** — admin/HC can CRUD columns (add, rename, remove)
- No fixed columns — all columns are dynamic and manageable
- Max ~5-8 columns per bagian
- Default columns based on current CMP/Mapping: Kompetensi, Indikator Perilaku, Detail Indikator Perilaku, Individual Development Plan / Silabus (Judul Sub Kompetensi & Target), Target Deliverable
- Column model: Claude's discretion for most practical approach (given 5-8 col max per bagian)

### Edit Interaction
- Spreadsheet inline editing (same pattern as KKJ Matrix Phase 47)
- Edit mode: entire table becomes editable — both header labels AND data cells
- Column CRUD in edit mode: tombol + to add column, tombol x per column header to delete
- Row CRUD: insert-below button per row (same as KKJ Matrix)
- Delete row: warning if CpdpItem is in-use elsewhere (Proton, IDP, etc.)
- Multi-cell selection: drag select, Ctrl+C/V copy-paste, Delete range-clear (same as KKJ Matrix Plan 47-05)
- Bulk save: single "Simpan" button saves all changes (headers + data), Bootstrap Toast confirmation

### CMP/Mapping Relationship
- CMP/Mapping page (/CMP/Mapping) **stays** as read-only view for all roles
- CMP/Mapping is updated to match new format: dropdown bagian filter (replace card selection), dynamic columns
- Data is synced — edits in Kelola Data immediately reflected in CMP/Mapping read-only view
- Same underlying data source, just different access level (edit vs read-only)

### Import/Export
- No Excel import needed — clipboard paste via multi-cell selection (Ctrl+V from Excel) is sufficient
- Excel export: yes, tombol Export to download current bagian data as Excel file

### Claude's Discretion
- Dynamic column database model design (given 5-8 max columns, choose most practical: wide table with Label_N or separate column entity)
- Exact rowspan rendering for Kompetensi grouping
- Column add/remove UX details (animation, confirmation)
- CMP/Mapping view update implementation (shared partial or separate view)

</decisions>

<specifics>
## Specific Ideas

- "Sama seperti page CMP/Mapping seharusnya" — kolom: No, Kompetensi, Indikator Perilaku, Detail Indikator Perilaku, Individual Development Plan / Silabus, Target Deliverable
- "HC tinggal copy paste dari Excel dan paste ke cellnya sekaligus banyak row atau kolom" — multi-cell clipboard is the primary data entry method
- Pattern should be consistent with KKJ Matrix (Phase 47) — spreadsheet inline, edit mode toggle, bulk save, toast confirmation
- CMP/Mapping dropdown filter should be consistent with Kelola Data dropdown

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 48-cpdp-items-manager*
*Context gathered: 2026-02-26*
