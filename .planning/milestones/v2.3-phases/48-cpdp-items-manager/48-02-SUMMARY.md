---
phase: 48-cpdp-items-manager
plan: "02"
subsystem: ui
tags: [razor, bootstrap, csharp, aspnetcore, javascript, ajax]

# Dependency graph
requires:
  - phase: 48-01
    provides: CpdpItems GET action and read-mode view with section dropdown
provides:
  - CpdpItemsSave POST — bulk upsert with NamaKompetensi reference guard + audit log
  - CpdpItemDelete POST — delete with IdpItem.Kompetensi reference guard + audit log
  - Edit-mode 7-column table inside #editTableWrapper with horizontal scroll and sticky columns
  - Section filter that applies to both read and edit tables simultaneously
  - Insert-below new row, inline DOM delete for Id=0, AJAX delete for Id>0
  - Bulk save sends JSON to /Admin/CpdpItemsSave, shows Bootstrap Toast, page reload
  - CMP/MappingSectionSelect.cshtml replaced card selection with compact dropdown
affects:
  - 48-03 (multi-cell clipboard + Excel export builds on this edit-mode table)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Typed Razor views use @item.Property (Razor auto-encodes) not Html.AttributeEncode — unavailable on IHtmlHelper<List<T>>"
    - "Edit-mode table injected into #editTableWrapper placeholder div; toggled via d-none/d-flex on btnEdit click"
    - "Bulk save: JS collects all edit rows into array, POST JSON to controller, show Bootstrap Toast 2s, reload"
    - "CpdpItemDelete reference guard: CountAsync(i => i.Kompetensi == item.NamaKompetensi) before delete"
    - "CpdpItemsSave rename guard: block if IdpItems reference old NamaKompetensi before allowing rename"

key-files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/CpdpItems.cshtml
    - Views/CMP/MappingSectionSelect.cshtml

key-decisions:
  - "Razor auto-encoding (@item.Property) replaces Html.AttributeEncode — consistent with Phase 48-01 decision"
  - "filterTables() queries both #readTableWrapper and #editTable tbody rows so section filter persists across mode toggle"
  - "Delete from edit-mode table: Id=0 rows removed from DOM only; Id>0 rows call AJAX /Admin/CpdpItemDelete"
  - "MappingSectionSelect dropdown disables Lihat button until section selected — prevents navigation with empty section"

patterns-established:
  - "Upsert pattern: FindAsync + update each field individually avoids EF tracking conflicts with deserialized JSON"
  - "Reference guard pattern: CountAsync check before delete/rename, return {success:false, blocked:true, message:...}"

requirements-completed:
  - MDAT-02

# Metrics
duration: 5min
completed: 2026-02-26
---

# Phase 48 Plan 02: Edit Mode Table, Bulk-Save, Delete Guard, CMP Dropdown Summary

**CPDP Items full write operations: 7-column sticky edit table, bulk-save POST with NamaKompetensi reference guard, delete POST with IdpItem guard, and CMP mapping section selector converted from cards to dropdown**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-26T13:52:22Z
- **Completed:** 2026-02-26T13:56:40Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments

- Added `CpdpItemsSave` and `CpdpItemDelete` POST actions to AdminController — upsert with rename reference guard, delete with IdpItem.Kompetensi reference guard, both with audit log
- Added 7-column edit-mode table to CpdpItems.cshtml with horizontal scroll, sticky first 2 columns, insert-below, inline delete, and bulk-save with Bootstrap Toast
- Replaced card-based section selection in MappingSectionSelect.cshtml with compact dropdown consistent with Admin editor UX

## Task Commits

Each task was committed atomically:

1. **Task 1: CpdpItemsSave and CpdpItemDelete in AdminController** - `d9badd7` (feat)
2. **Task 2: Edit-mode table and full JS in CpdpItems.cshtml** - `3927820` (feat)
3. **Task 3: MappingSectionSelect.cshtml dropdown replacement** - `e8ed7fa` (feat)

## Files Created/Modified

- `Controllers/AdminController.cs` - Added CpdpItemsSave (bulk upsert + rename guard) and CpdpItemDelete (reference guard + audit log) POST actions
- `Views/Admin/CpdpItems.cshtml` - Added edit-mode CSS, 7-column edit table, full JS for mode toggle, section filter, insert-below, delete, and bulk save
- `Views/CMP/MappingSectionSelect.cshtml` - Replaced card layout with dropdown + Lihat button, same GET endpoint

## Decisions Made

- Razor auto-encoding (`@item.Property`) used instead of `Html.AttributeEncode` — consistent with Phase 48-01 finding that `AttributeEncode` is unavailable on `IHtmlHelper<List<T>>`
- `filterTables()` queries both `#readTableWrapper tbody tr` and `#editTable tbody tr` so section filter state persists when toggling between read and edit mode
- Edit-mode delete: Id=0 rows remove from DOM only; Id>0 rows call AJAX so delete confirmation + reference guard is server-enforced
- MappingSectionSelect Lihat button disabled until section selected — prevents accidental navigation with empty section param

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Replaced Html.AttributeEncode with Razor auto-encoding in edit table**
- **Found during:** Task 2 (edit-mode table implementation)
- **Issue:** Plan specified `@Html.AttributeEncode(item.X)` for input values, but `AttributeEncode` is not defined on `IHtmlHelper<List<CpdpItem>>` — 5 CS1061 build errors
- **Fix:** Replaced all `@Html.AttributeEncode(item.X)` with `@item.X` — Razor auto-encodes property values in HTML attributes
- **Files modified:** Views/Admin/CpdpItems.cshtml
- **Verification:** dotnet build shows only MSB3021 (file lock from running app) — no CS errors
- **Committed in:** 3927820 (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - bug, same issue as Phase 48-01)
**Impact on plan:** Fix necessary for correct HTML output. No scope creep.

## Issues Encountered

- App running in Debug mode locks the exe during build — results in MSB3021 errors that are not C# compilation failures. Only CS-prefixed errors indicate actual code problems. This is consistent with all prior phases.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Edit mode is fully wired; Plan 48-03 can layer multi-cell clipboard paste and Excel export directly on `#editTable`
- CMP Mapping dropdown navigation is consistent with Admin editor section filter

---
*Phase: 48-cpdp-items-manager*
*Completed: 2026-02-26*
