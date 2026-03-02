# Phase 89: KKJ Matrix Dynamic Columns - Context

**Gathered:** 2026-03-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Redesign KkjMatrixItem from 15 fixed Target_* columns to a key-value relational model using new KkjColumn and KkjTargetValue tables. Update all consumers: KkjMatrix admin view, CMP/Kkj worker view, PositionTargetHelper, and Assessment flow. This enables each Bagian to have its own set of target columns (dynamic, not hardcoded).

</domain>

<decisions>
## Implementation Decisions

### Data Migration
- Start fresh: remove all existing Target_* data from KkjMatrixItem
- HC will re-input target values via Excel import (Phase 88) after this phase completes
- Keep base KkjMatrixItem data (No, SkillGroup, SubSkillGroup, Indeks, Kompetensi, Bagian) intact

### New Table Design
- **KkjColumn** table: Id, BagianId (FK to KkjBagian), Name (string), DisplayOrder (int)
  - Defines target columns per Bagian (e.g., Bagian "RFCC" → columns "Section Head", "Operator Process Water", etc.)
  - Each Bagian can have different number of columns
- **KkjTargetValue** table: Id, KkjMatrixItemId (FK to KkjMatrixItem), KkjColumnId (FK to KkjColumn), Value (string)
  - Key-value store: each cell is one row
  - Value is string (same as current Target_* columns, typically "1"-"5" or "-")
- Remove all 15 Target_* columns from KkjMatrixItem model
- Remove all 15 Label_* columns from KkjBagian model

### Position Mapping
- **Admin define mapping**: new table PositionColumnMapping (Id, Position string, KkjColumnId FK)
- Admin can set which user position maps to which KkjColumn via UI in KkjMatrix page
- Replaces hardcoded Dictionary in PositionTargetHelper.cs
- PositionTargetHelper.GetTargetLevel() will query PositionColumnMapping + KkjTargetValue instead of reflection
- If user position is not mapped: show **warning to admin** in assessment result ("Posisi X belum di-map ke kolom KKJ")

### KkjMatrix Admin View
- Spreadsheet-like editable view (same UX as current)
- Columns rendered dynamically from KkjColumn table per selected Bagian
- Admin can add/rename/delete/reorder columns directly in UI (like current Bagian management)
- Columns also auto-created during Excel import (Phase 88)

### CMP/Kkj Worker View
- Follow same pattern as KkjMatrix admin view
- Render columns dynamically from DB
- Worker sees columns for their Bagian only (filtered)

### Assessment Flow
- CMPController (lines 1425, 1556) and AdminController (lines 2296, 2368) use GetTargetLevel()
- Update to use new PositionColumnMapping → KkjTargetValue lookup
- UserCompetencyLevel and AssessmentCompetencyMap FK to KkjMatrixItem remains unchanged (only target lookup changes)

### Claude's Discretion
- Exact EF Core migration strategy (code-first migration)
- KkjColumn management UI layout details
- PositionColumnMapping admin UI design
- Performance optimization for key-value queries (eager loading, caching)
- Handling edge cases in KkjMatrixSave for dynamic columns

</decisions>

<specifics>
## Specific Ideas

- User wants system where "setiap bagian memiliki kolom yang berbeda" — each Bagian has its own unique set of target columns
- Example: Bagian RFCC might have "Section Head", "Operator Process Water", "Sr Supervisor" while Bagian GAST has completely different column names
- HC defines the structure — system follows what HC imports/creates
- Current KkjMatrix spreadsheet UX is good — keep it, just make columns dynamic

</specifics>

<code_context>
## Existing Code Insights

### Files to Modify
- `Models/KkjModels.cs` — Remove 15 Target_* from KkjMatrixItem, 15 Label_* from KkjBagian, add KkjColumn + KkjTargetValue + PositionColumnMapping models
- `Helpers/PositionTargetHelper.cs` — Replace hardcoded Dictionary with DB lookup via PositionColumnMapping
- `Controllers/AdminController.cs` — Update KkjMatrix, KkjMatrixSave, KkjMatrixDelete actions; add column management actions; update GetTargetLevel calls
- `Controllers/CMPController.cs` — Update GetTargetLevel calls (lines 1425, 1556)
- `Views/Admin/KkjMatrix.cshtml` — Dynamic column rendering instead of 15 hardcoded columns
- `Views/CMP/Kkj.cshtml` — Dynamic column rendering (read-only)
- `Data/ApplicationDbContext.cs` — Add DbSets, configure FKs for new tables
- `Data/SeedCompetencyMappings.cs` — May need update for new schema

### Established Patterns
- KkjBagian management (add/delete/rename) pattern exists — reuse for KkjColumn management
- ClosedXML for Excel (Phase 88 will use this)
- AntiForgeryToken on all POST
- JSON API responses for inline edit operations

### Integration Points
- UserCompetencyLevel.KkjMatrixItemId → FK unchanged
- AssessmentCompetencyMap.KkjMatrixItemId → FK unchanged
- PositionTargetHelper → used by CMPController + AdminController assessment flows
- Phase 88 (Import/Export) depends on this phase completing first

</code_context>

<deferred>
## Deferred Ideas

- Excel import/export using dynamic columns → Phase 88 (depends on this phase)
- Template per-Bagian download → Phase 88 future enhancement

</deferred>

---

*Phase: 89-kkj-matrix-dynamic-columns*
*Context gathered: 2026-03-02*
