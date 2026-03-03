# Phase 89: KKJ Matrix Dynamic Columns - Context (Rewrite)

**Gathered:** 2026-03-02 (updated)
**Status:** Ready for planning
**Reason for update:** UAT abandoned — user requests full page rewrite. Backend (models, migration, helper) is solid. Views and controller actions need clean rewrite from scratch.

<domain>
## Phase Boundary

Clean rewrite of KkjMatrix admin page (Views/Admin/KkjMatrix.cshtml) and CMP/Kkj worker view (Views/CMP/Kkj.cshtml) plus their controller actions (AdminController KkjMatrix region, CMPController.Kkj). Backend data model (KkjColumn, KkjTargetValue, KkjMatrixItem, KkjBagian) and migration are working — no changes needed to models or database.

**Why rewrite:** Previous implementation patched old 15-column code with new dynamic column code. Result was messy/conflicting. User said: "hapus semua code di page KkjMatrix, susun ulang dari awal sehingga hasilnya bersih."

</domain>

<decisions>
## Implementation Decisions

### Page Layout (Admin KkjMatrix)
- Matrix table always visible at top
- Admin tools (Bagian management, column management) in collapsible cards below table
- Bagian selector is a dropdown filter in the page header
- When no Bagian selected: show only the Bagian dropdown, no table
- Bagian CRUD (add/rename/delete) stays on this page, near the Bagian dropdown

### Read/Edit Mode
- Toggle between read mode (clean styled table) and edit mode (input fields)
- Button to switch between modes
- Read mode: styled, non-editable table display
- Edit mode: all cells become editable (fixed columns + dynamic columns)

### Edit Mode — Row Operations
- Full CRUD in edit mode: add new rows, delete rows, edit all cells
- **Inline row insert**: HC can insert a new row below any existing row, not just at the end
- One Save button commits all changes (batch save, single request)
- Cancel button discards all unsaved changes and returns to read mode

### Column Management
- **Claude's discretion** for UI approach (collapsible card, modal, or inline header editing)
- HC can add/rename/delete/reorder columns for current Bagian
- Columns are per-Bagian (each Bagian has its own set)

### Target Value Validation
- Only values 1, 2, 3, 4, 5, or - (dash) allowed in dynamic column cells
- Reject any other input

### Data Entry
- Start simple: basic cell-by-cell editing only
- NO clipboard paste from Excel (defer to future enhancement)
- NO multi-cell selection (defer to future enhancement)
- NO keyboard shortcuts beyond basic Tab navigation

### Role Permissions — Admin KkjMatrix
- Access: Admin and HC roles only (Authorize(Roles = "Admin, HC"))
- Both Admin and HC have full CRUD on matrix, columns, and bagian

### CMP/Kkj Worker View
- Design follows Admin KkjMatrix style but read-only table (no edit mode)
- Full rewrite of both view and CMPController.Kkj action
- Always show Bagian dropdown (options filtered by role)
- Role-based Bagian access:
  - HC, Admin, Level 3, Level 4: see ALL Bagians in dropdown
  - Level 5 and Level 6: see only their own Bagian in dropdown

### Table Styling
- Admin KkjMatrix is the design source; CMP/Kkj follows same design (read-only)
- First 5 columns frozen/sticky (No, SkillGroup, SubSkillGroup, Indeks, Kompetensi) when scrolling horizontally
- Sticky header row when scrolling vertically
- Clean, modern Bootstrap styling

### Position Mapping — DEFERRED
- PositionColumnMapping feature removed from this phase scope
- Remove PositionMapping CRUD actions from AdminController
- Keep PositionColumnMapping model and table in DB (no migration changes)
- PositionTargetHelper stays as-is (already async, already working)
- Position mapping UI will be implemented in a future phase

### Backend Rewrite Scope
- **AdminController**: Rewrite KkjMatrix region (KkjMatrix, KkjMatrixSave, KkjColumn CRUD actions). Remove PositionMapping CRUD actions
- **CMPController**: Rewrite Kkj() action with role-based Bagian filtering
- **Models**: No changes — KkjColumn, KkjTargetValue, KkjBagian, KkjMatrixItem stay as-is
- **Migration**: No changes — existing migration is correct
- **PositionTargetHelper**: No changes — already refactored to async

### Claude's Discretion
- Column management UI layout (card, modal, or inline)
- Whether to keep PositionColumnMapping model/DbSet or clean it up
- Table CSS specifics (exact colors, spacing, scrollbar styling)
- Toast notification and error message styling
- Edit mode input field types and styling
- Performance optimization (eager loading strategy)

</decisions>

<specifics>
## Specific Ideas

- "hapus semua data/code di page KkjMatrix, susun ulang dari awal" — full clean rewrite, no patching
- Each Bagian has its own unique set of target columns (dynamic, not hardcoded)
- HC defines the structure (columns) AND inputs all data (rows + values)
- Current spreadsheet-like UX concept is good — rebuild it cleanly
- Phase 88 (Excel import) will work after this rewrite completes — HC downloads template with fixed + dynamic columns

</specifics>

<code_context>
## Existing Code Insights

### Working Backend (NO changes needed)
- `Models/KkjModels.cs` — KkjColumn, KkjTargetValue, PositionColumnMapping, KkjBagian, KkjMatrixItem all working
- `Data/ApplicationDbContext.cs` — DbSets and OnModelCreating configuration working
- `Migrations/20260302093959_AddKkjDynamicColumns.cs` — Applied successfully
- `Helpers/PositionTargetHelper.cs` — Async DB-query based, working

### Files to Rewrite from Scratch
- `Views/Admin/KkjMatrix.cshtml` — DELETE all content, rewrite from scratch (~1300 lines → clean new version)
- `Views/CMP/Kkj.cshtml` — DELETE all content, rewrite from scratch (~290 lines → clean new version)
- `Controllers/AdminController.cs` — Rewrite KkjMatrix region actions, remove PositionMapping actions
- `Controllers/CMPController.cs` — Rewrite Kkj() action with role-based filtering

### Established Patterns
- KkjBagian management (add/delete/rename) pattern — reuse for clean column management
- AntiForgeryToken on all POST actions
- JSON API responses for AJAX operations
- Bootstrap 5 + Bootstrap Icons for UI

### Integration Points
- Phase 88 (Import/Export) depends on this rewrite completing
- Assessment flow (PositionTargetHelper) not affected by this rewrite
- UserCompetencyLevel and AssessmentCompetencyMap FKs unchanged

</code_context>

<deferred>
## Deferred Ideas

- Position mapping UI (PositionColumnMapping CRUD) — future phase
- Excel clipboard paste into matrix table — future enhancement
- Multi-cell selection and keyboard shortcuts — future enhancement
- Excel import/export using dynamic columns — Phase 88 (depends on this phase)

</deferred>

---

*Phase: 89-kkj-matrix-dynamic-columns (rewrite)*
*Context gathered: 2026-03-02 (updated)*
