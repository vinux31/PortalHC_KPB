---
phase: 48-cpdp-items-manager
plan: 01
subsystem: ui
tags: [razor, asp-net-core, cpdp, admin-portal, kkj-idp]

# Dependency graph
requires:
  - phase: 47-kkj-matrix-manager
    provides: AdminController pattern (class-level [Authorize], ViewData Title, KkjMatrix structure)
  - phase: quick-14
    provides: CpdpItem model with Section column, DbSet<CpdpItem> CpdpItems in ApplicationDbContext
provides:
  - GET /Admin/CpdpItems action returning all CpdpItems ordered by No then Id
  - CpdpItems.cshtml read-mode page with section dropdown (RFCC/GAST/NGP/DHT) filtering
  - cpdpItems JS variable (serialized model) for Plan 02 edit mode
  - AntiForgeryToken rendered for Plan 02 AJAX use
  - Placeholder divs (editTableWrapper, editActions) for Plan 02 injection
  - Admin/Index CPDP Items card activated with /Admin/CpdpItems link
affects: [48-02-cpdp-edit-mode, 48-03-multi-cell-clipboard]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Client-side section filtering using data-section attribute on <tr> rows, filterTables() vanilla JS"
    - "Model serialized to JS variable via JsonSerializer in @{} block, injected via Html.Raw(itemsJson)"
    - "Placeholder div pattern (editTableWrapper d-none) for Plan 02 to inject edit UI without modifying this view"

key-files:
  created:
    - Views/Admin/CpdpItems.cshtml
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/Index.cshtml

key-decisions:
  - "data-name attribute uses Razor auto-encoding (@item.NamaKompetensi) instead of Html.AttributeEncode which is not available in typed Razor views"
  - "Client-side section filtering (no page reload) using data-section attribute on each <tr> row"
  - "cpdpItems JS variable initialized with PropertyNamingPolicy = null to match PascalCase C# properties (consistent with Plan 47-02 precedent)"

patterns-established:
  - "Section filter pattern: sectionFilter select + data-section on <tr> rows + filterTables() JS function"
  - "Row count display: rowCount span updated in filterTables() showing 'N baris'"

requirements-completed: [MDAT-02]

# Metrics
duration: 5min
completed: 2026-02-26
---

# Phase 48 Plan 01: CpdpItems page: GET action, read-mode table, section dropdown, Admin/Index link Summary

**Read-only /Admin/CpdpItems page with RFCC/GAST/NGP/DHT section dropdown filtering and Admin/Index card activation for KKJ-IDP Mapping Editor**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-02-26T13:44:42Z
- **Completed:** 2026-02-26T13:49:30Z
- **Tasks:** 3
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments

- GET /Admin/CpdpItems action added to AdminController with CpdpItems ordered by No then Id
- CpdpItems.cshtml read-mode view with breadcrumb, section dropdown, compact table (No/Nama Kompetensi/Indikator Perilaku/Aksi), rowCount display, AntiForgeryToken, and cpdpItems JS variable
- Admin/Index CPDP Items card updated: link to /Admin/CpdpItems, renamed to KKJ-IDP Mapping Editor, Segera badge and opacity-75 removed

## Task Commits

Each task was committed atomically:

1. **Task T1: Add CpdpItems GET action to AdminController** - `d11e1b2` (feat)
2. **Task T2: Create Views/Admin/CpdpItems.cshtml** - `06bf72a` (feat)
3. **Task T3: Update Admin/Index.cshtml CPDP Items card** - `fbf5e41` (feat)

## Files Created/Modified

- `Controllers/AdminController.cs` - Added GET /Admin/CpdpItems action (after KkjMatrixDelete, returns ordered CpdpItems list)
- `Views/Admin/CpdpItems.cshtml` - New read-mode page: breadcrumb, section dropdown, compact table, placeholder divs, toast, JS filter
- `Views/Admin/Index.cshtml` - CPDP Items card activated: link, title, opacity, badge updated

## Decisions Made

- Used `@item.NamaKompetensi` in data-name attribute instead of `@Html.AttributeEncode()` — Razor auto-encodes attribute values; `Html.AttributeEncode` is not available in typed Razor views (CS1061)
- Client-side section filtering with `data-section` on `<tr>` rows (no page reload) matches the plan spec
- `PropertyNamingPolicy = null` in JsonSerializer keeps PascalCase property names matching C# model — consistent with Phase 47-02 decision

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Replaced Html.AttributeEncode() with Razor auto-encoding**
- **Found during:** Task T2 (CpdpItems.cshtml creation) — build verification
- **Issue:** `@Html.AttributeEncode(item.NamaKompetensi)` causes CS1061: method not available in typed Razor IHtmlHelper<List<CpdpItem>>
- **Fix:** Changed to `@item.NamaKompetensi` — Razor HTML-encodes attribute values automatically
- **Files modified:** Views/Admin/CpdpItems.cshtml
- **Verification:** `dotnet build --no-restore` shows no CS errors
- **Committed in:** 06bf72a (Task T2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Auto-fix necessary for correctness — Razor's built-in encoding is equivalent and correct. No scope creep.

## Issues Encountered

- Build errors reported as MSB3027/MSB3021 (file-locked by running HcPortal process) — not compilation errors. C# code compiled cleanly with zero CS errors. This is expected behavior when dotnet build tries to copy output exe while app is running.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- GET /Admin/CpdpItems fully functional as read-mode page
- AntiForgeryToken and cpdpItems JS variable ready for Plan 02 AJAX edit mode
- Placeholder divs (editTableWrapper, editActions) ready for Plan 02 to inject without modifying this view
- Plan 02 can add: edit mode table, CpdpItemSave POST, CpdpItemDelete POST, inline editing

---
*Phase: 48-cpdp-items-manager*
*Completed: 2026-02-26*

## Self-Check: PASSED

- FOUND: Controllers/AdminController.cs
- FOUND: Views/Admin/CpdpItems.cshtml
- FOUND: Views/Admin/Index.cshtml
- FOUND: .planning/phases/48-cpdp-items-manager/48-01-SUMMARY.md
- FOUND commit d11e1b2: feat(48-01): add CpdpItems GET action to AdminController
- FOUND commit 06bf72a: feat(48-01): create CpdpItems.cshtml read-mode view with section filter
- FOUND commit fbf5e41: feat(48-01): activate CPDP Items card in Admin/Index
