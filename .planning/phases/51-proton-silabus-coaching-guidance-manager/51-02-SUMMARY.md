---
phase: 51-proton-silabus-coaching-guidance-manager
plan: 02
subsystem: ui, database
tags: [asp.net-mvc, razor, javascript, batch-save, rowspan, inline-crud, audit-log]

# Dependency graph
requires:
  - phase: 51-01
    provides: ProtonDataController GET Index, JSON data island (silabusData), silabusTableContainer placeholder, AntiForgeryToken

provides:
  - SilabusSave POST endpoint (batch upsert Kompetensi/SubKompetensi/Deliverable hierarchy with orphan cleanup)
  - SilabusDelete POST endpoint (single deliverable delete with empty-parent cascade cleanup)
  - SilabusRowDto and SilabusDeleteRequest DTOs at namespace level
  - Silabus tab view mode with rowspan-merged Kompetensi/SubKompetensi table
  - Silabus tab edit mode with individual input rows per deliverable
  - Inline add (+) button inserts row after current (copies Kompetensi/SubKompetensi from current row)
  - Inline delete (trash) with Bootstrap modal confirmation for saved rows; DOM-only for unsaved rows
  - Tambah Baris button appends blank row
  - Simpan Semua batch POST to /ProtonData/SilabusSave with page reload after success
  - Empty state for no-filter and no-data conditions
  - AuditLog entries for all Silabus save and delete operations
affects:
  - 51-03 (Coaching Guidance tab — same view file, silabusDeleteModal and filter patterns)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Rowspan merge: compute kompSpan (consecutive same Kompetensi), then subSpan within each group — render first row of group with rowspan attr, subsequent rows omit merged cells"
    - "JSON filter state island (<script type=application/json id=silabusFilter>) — passes Razor-computed Bagian/Unit/TrackId/hasFilter to IIFE JS without inline Razor in script"
    - "IIFE pattern for Silabus CRUD state (silabusRows, isEditMode, deleteTargetIndex) — avoids global namespace pollution"
    - "Batch save pattern: read DOM inputs into array → validate → enrich with filter context → POST JSON → reload on success"
    - "Deferred delete index pattern: deleteTargetIndex stored in closure, used by modal confirm handler — avoids data-* race conditions"

key-files:
  created: []
  modified:
    - Controllers/ProtonDataController.cs
    - Views/ProtonData/Index.cshtml

key-decisions:
  - "SilabusRowDto and SilabusDeleteRequest DTOs placed at namespace level (outside controller class) — consistent with Phase 50-02 CoachAssignRequest/CoachEditRequest pattern"
  - "SilabusSave flushes SaveChangesAsync after each Kompetensi and SubKompetensi upsert to get EF-generated IDs for FK chaining before next hierarchy level"
  - "Orphan cleanup after batch save: find all Deliverables/SubKompetensi/Kompetensi for this Bagian+Unit+Track scope not in savedIds and remove — prevents stale data if user deletes rows before saving"
  - "JSON silabusFilter island avoids embedding Razor variables directly in IIFE JavaScript — clean separation of server/client data"
  - "deleteTargetIndex stored in IIFE closure variable, not data attribute — avoids stale value if re-render happens between click and modal confirm"
  - "Batal button reloads page (window.location.reload()) to restore pristine server state — simpler and more reliable than reverting in-memory edits"

patterns-established:
  - "JSON filter state island pattern for passing Razor computed values to JS IIFEs without Razor in script blocks"
  - "Rowspan view mode + flat edit mode toggle: two separate render functions, shared silabusRows array in IIFE state"

requirements-completed: [OPER-02]

# Metrics
duration: 3min
completed: 2026-02-27
---

# Phase 51 Plan 02: Silabus Tab CRUD — View/Edit Mode, Rowspan Table, Inline Add/Delete, Save All

**Silabus tab fully functional: rowspan view table (Kompetensi/SubKompetensi merged cells), edit mode with flat input rows, inline add/delete, and batch Save All via SilabusSave/SilabusDelete endpoints with AuditLog**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-02-27T06:47:00Z
- **Completed:** 2026-02-27T06:50:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Added `SilabusSave` POST endpoint: batch upserts full Kompetensi/SubKompetensi/Deliverable hierarchy from flat row array, handles Id=0 (new) vs Id>0 (update), groups new Kompetensi/SubKompetensi by text to avoid duplicates, cleans orphaned entities in scope, logs to AuditLog
- Added `SilabusDelete` POST endpoint: deletes a single deliverable, cascades to clean empty SubKompetensi and Kompetensi parents, logs to AuditLog
- Replaced Plan 01 placeholder in `silabusTableContainer` with full JS-driven rendering: view mode (rowspan for Kompetensi/SubKompetensi groups) and edit mode (expanded flat input rows per deliverable)
- Inline add (+): inserts row after current position, copies Kompetensi/SubKompetensi from current row for convenience
- Inline delete (trash): unsaved rows removed DOM-only; saved rows show Bootstrap modal confirmation then call SilabusDelete
- Simpan Semua: reads DOM inputs, validates non-empty Kompetensi/Deliverable, enriches with filter context, POSTs to SilabusSave, reloads page on success
- silabusFilter JSON island added to pass Bagian/Unit/TrackId/hasFilter from Razor to JavaScript without inline Razor in script blocks

## Task Commits

Each task was committed atomically:

1. **Task 1: Add SilabusSave and SilabusDelete endpoints to ProtonDataController** - `b7c3cd0` (feat)
2. **Task 2: Implement Silabus tab JavaScript — view/edit mode, rowspan rendering, inline add/delete, Save All** - `2715d29` (feat)

## Files Created/Modified

- `Controllers/ProtonDataController.cs` - Added SilabusRowDto + SilabusDeleteRequest DTOs at namespace level; SilabusSave POST with batch upsert + orphan cleanup + AuditLog; SilabusDelete POST with parent cascade cleanup + AuditLog
- `Views/ProtonData/Index.cshtml` - Replaced Plan 01 placeholder: silabusTableContainer now JS-rendered; added silabusFilter JSON island; added silabusDeleteModal; added full IIFE JS (renderViewTable, renderEditTable, wireEditTableEvents, saveAll, delete modal handler)

## Decisions Made

- SilabusSave flushes `SaveChangesAsync` after each Kompetensi and SubKompetensi upsert to retrieve EF-generated IDs for FK chaining to the next hierarchy level — necessary because EF doesn't return auto-increment IDs until after flush
- Orphan cleanup uses post-save scope query: load all Kompetensi for Bagian+Unit+Track, then remove any Deliverable/SubKompetensi/Kompetensi not in the saved ID sets — handles cases where user deleted rows before saving
- `silabusFilter` JSON island pattern: Razor computes `{ bagian, unit, trackId, hasFilter }` and serializes into a `<script type=application/json>` element — IIFE reads this cleanly without Razor interpolation inside `<script>` blocks
- `deleteTargetIndex` stored as IIFE closure variable (not DOM data attribute) — avoids stale value from a potential re-render between trash click and modal confirm button click

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- /ProtonData Silabus tab fully functional: view mode with rowspan merges, edit mode with inline add/delete, batch Save All persisting to DB
- SilabusSave and SilabusDelete endpoints available for integration testing
- Coaching Guidance tab placeholder intact, ready for Plan 03 file CRUD implementation
- silabusDeleteModal can be reused or Plan 03 can add its own modal for guidance file deletes

---
*Phase: 51-proton-silabus-coaching-guidance-manager*
*Completed: 2026-02-27*
