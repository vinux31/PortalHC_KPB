---
phase: 35-crud-add-edit
plan: "02"
subsystem: ui
tags: [ajax, bootstrap-collapse, fetch, antiforgery, inline-edit, cshtml]

# Dependency graph
requires:
  - phase: 35-crud-add-edit plan 01
    provides: Four POST endpoints — AddKompetensi, AddSubKompetensi, AddDeliverable, EditCatalogItem — in ProtonCatalogController
  - phase: 34-catalog-page
    provides: _CatalogTree.cshtml three-level Bootstrap collapse table structure and GetCatalogTree partial view endpoint

provides:
  - Inline "+ Add Kompetensi/SubKompetensi/Deliverable" trigger links always visible at bottom of each level
  - Empty-state messages ("No X yet") at all three catalog levels when a parent has zero children
  - Pencil icon edit triggers revealed on Bootstrap collapse expand, hidden on collapse
  - All JavaScript for add/edit AJAX interactions in Index.cshtml as initCatalogTree()
  - buildInlineInput, postItem, showInlineError, restoreNameSpan, reloadTree helper functions

affects: [35-crud-add-edit, 36-delete-reorder, ProtonCatalog frontend]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Scripts-in-innerHTML don't execute — move JS out of partial view into parent page when partial is AJAX-reloaded"
    - "initCatalogTree() function called on DOMContentLoaded and after every reloadTree() to re-attach listeners"
    - "Antiforgery token read from parent DOM: document.querySelector('input[name=\"__RequestVerificationToken\"]')"
    - "Bootstrap collapse show.bs.collapse/hide.bs.collapse events used to toggle pencil icon visibility"
    - "reloadTree() fetches GetCatalogTree partial and injects into #treeContainer, then calls initCatalogTree()"

key-files:
  created: []
  modified:
    - Views/ProtonCatalog/_CatalogTree.cshtml
    - Views/ProtonCatalog/Index.cshtml

key-decisions:
  - "[Phase 35-02]: JS moved from _CatalogTree.cshtml to Index.cshtml — browsers do not execute scripts injected via innerHTML; initCatalogTree() called on DOMContentLoaded and after each reloadTree()"
  - "[Phase 35-02]: reloadTree() calls initCatalogTree() after innerHTML injection to re-attach all event listeners on the freshly rendered tree"
  - "[Phase 35-02]: On successful add, full tree reload (reloadTree) used rather than DOM insertion — consistent with existing AJAX pattern and avoids stale state"

patterns-established:
  - "Partial view JS pattern: when a partial is AJAX-reloaded via innerHTML, all its JS must live in the parent page and be re-initialized after each reload"

# Metrics
duration: ~45min
completed: 2026-02-24
---

# Phase 35 Plan 02: Catalog Add/Edit Frontend Summary

**Inline add/edit interactions for the three-level Proton Catalog tree — empty states, "+ Add X" trigger links, pencil icon edit mode, and all AJAX wiring via initCatalogTree() in Index.cshtml**

## Performance

- **Duration:** ~45 min
- **Started:** 2026-02-24 (continuation of Plan 01 session)
- **Completed:** 2026-02-24T00:56:12Z
- **Tasks:** 2 + 1 checkpoint (all complete)
- **Files modified:** 2

## Accomplishments

- Empty-state messages at all three catalog levels ("No Kompetensi yet", "No SubKompetensi yet", "No Deliverables yet") render when a parent has zero children
- "+ Add Kompetensi/SubKompetensi/Deliverable" trigger links always visible at the bottom of each level; clicking reveals inline input with disabled Save and active Cancel
- Pencil icon (bi-pencil) shown on row expand via Bootstrap collapse events, hidden on collapse; clicking pencil puts the name span into inline edit mode pre-filled with current name
- All AJAX wiring moved to Index.cshtml as initCatalogTree() — called on DOMContentLoaded and after every reloadTree() call
- Human verification checkpoint passed (all add/edit flows confirmed working end-to-end)

## Task Commits

Each task was committed atomically:

1. **Task 1 + 2: Add empty states, inline Add rows, pencil icons, and wire AJAX** - `16f83c0` (feat)
2. **Fix: Move catalog tree JS to Index.cshtml** - `66b51a3` (fix)

## Files Created/Modified

- `Views/ProtonCatalog/_CatalogTree.cshtml` - Added empty-state messages at all three levels, "+ Add X" trigger links with placeholder containers, pencil buttons (d-none by default) with data-level/data-id attributes on every item row. JS removed and placed in parent page.
- `Views/ProtonCatalog/Index.cshtml` - Added initCatalogTree() function with all AJAX interaction logic: chevron rotation, pencil visibility via collapse events, buildInlineInput helper, postItem helper, reloadTree helper, showInlineError helper, restoreNameSpan helper, all three Add click handlers, and pencil edit handler.

## Decisions Made

- JS moved from _CatalogTree.cshtml to Index.cshtml because browsers do not execute `<script>` tags injected via innerHTML assignment. Since the partial is reloaded via AJAX (innerHTML = html), any scripts inside it would be silently ignored, making all listeners dead on reload.
- reloadTree() now calls initCatalogTree() after each tree reload to re-attach all event listeners to the freshly rendered DOM.
- On successful add, a full tree reload (reloadTree) is used rather than optimistic DOM insertion — consistent with the established AJAX pattern and avoids any stale state from partial updates.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Scripts in innerHTML are not executed by browsers**
- **Found during:** Task 2 (AJAX wiring)
- **Issue:** The inline IIFE placed inside _CatalogTree.cshtml would not run after AJAX tree reloads because browsers intentionally skip script execution when setting innerHTML. All event listeners would be dead after the first reloadTree() call.
- **Fix:** Moved the entire initCatalogTree() function from _CatalogTree.cshtml into Index.cshtml. Called on DOMContentLoaded and at the end of every reloadTree() invocation. _CatalogTree.cshtml retains no JavaScript.
- **Files modified:** Views/ProtonCatalog/_CatalogTree.cshtml (scripts removed), Views/ProtonCatalog/Index.cshtml (initCatalogTree added)
- **Verification:** Human verification checkpoint passed — all interactions confirmed working after tree reloads.
- **Committed in:** `66b51a3` (fix commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug fix)
**Impact on plan:** Critical fix for correctness — without it, add/edit would work only on first page load, not after any tree reload. No scope creep.

## Issues Encountered

None beyond the auto-fixed script execution issue above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 35 complete: all CAT-03 through CAT-06 requirements met (add Kompetensi, add SubKompetensi, add Deliverable, edit any item inline)
- Phase 36 (delete + reorder) can proceed — ProtonCatalogController has four working POST endpoints, tree renders correctly with AJAX reload pattern established
- The initCatalogTree() pattern in Index.cshtml is the extension point for Phase 36 delete/reorder interactions

## Self-Check: PASSED

- FOUND: Views/ProtonCatalog/_CatalogTree.cshtml
- FOUND: Views/ProtonCatalog/Index.cshtml
- FOUND: .planning/phases/35-crud-add-edit/35-02-SUMMARY.md
- FOUND commit 16f83c0: feat(35-02): add inline add/edit interactions to _CatalogTree.cshtml
- FOUND commit 66b51a3: fix(35-02): move catalog tree JS to Index.cshtml so it runs after innerHTML reload

---
*Phase: 35-crud-add-edit*
*Completed: 2026-02-24*
