---
phase: 36-delete-guards
plan: "02"
subsystem: ui
tags: [bootstrap-modal, fetch, ajax, cshtml, collapse-events, xss, cascade-delete]

# Dependency graph
requires:
  - phase: 36-delete-guards plan 01
    provides: GetDeleteImpact GET and DeleteCatalogItem POST endpoints in ProtonCatalogController
  - phase: 35-crud-add-edit plan 02
    provides: initCatalogTree() pattern in Index.cshtml, _CatalogTree.cshtml three-level collapse structure, postItem() helper

provides:
  - Trash icon (bi-trash, text-danger) on all three tree levels with correct d-none visibility rules
  - Shared #deleteModal with three-state body: loading spinner / impact content / error alert
  - initDeleteGuards() wired at all three initCatalogTree() call sites (DOMContentLoaded, reloadTree, onTrackChanged)
  - escapeHtml() XSS helper and showDeleteError() modal error helper at script scope
  - Phase 36 CAT-07 requirement fully implemented end-to-end

affects: [37-reorder, ProtonCatalog frontend]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Bootstrap collapse event bubbling guard: add `if (e.target !== target) return;` to all collapse listeners to prevent child collapse events from incorrectly toggling parent row icons"
    - "deleteConfirmBtn cloneNode trick: replace button with cloneNode(true) before adding listener so re-calling initDeleteGuards() on each tree reload never stacks duplicate confirm listeners"
    - "Modal three-state pattern: loading div (always shown on open) / content div (d-none until impact loaded) / error div (d-none until failure) — only one state visible at a time"
    - "Leaf node icon visibility: Deliverable pencil and trash buttons must NOT have d-none — they have no collapse toggle so they are only visible when their parent SubKompetensi is expanded"

key-files:
  created: []
  modified:
    - Views/ProtonCatalog/_CatalogTree.cshtml
    - Views/ProtonCatalog/Index.cshtml

key-decisions:
  - "[Phase 36-02]: Deliverable pencil-btn loses d-none (fix 841f40c) — leaf nodes have no collapse toggle so the pencil was never revealed; it must always be visible when the parent SubKompetensi is expanded"
  - "[Phase 36-02]: Bootstrap collapse events bubble — `e.target !== target` guard added to all 6 listeners (chevron, pencil show/hide, trash show/hide) so expanding a SubKompetensi does not incorrectly hide the parent Kompetensi's icons"
  - "[Phase 36-02]: text-nowrap added to all three action <td> elements so pencil + trash buttons always render side-by-side regardless of available column width"
  - "[Phase 36-02]: deleteConfirmBtn is re-cloned on every initDeleteGuards() call to prevent listener accumulation across reloadTree() cycles — cloneNode(true) removes all prior listeners from the element"

patterns-established:
  - "Collapse event guard: always add `if (e.target !== target) return;` when attaching show.bs.collapse/hide.bs.collapse to nested collapse structures to prevent cross-level event firing"
  - "Leaf-node icon visibility: leaf rows (no collapse toggle) must not have d-none on their action icons — d-none only appropriate when a collapse toggle controls the row's expansion state"

# Metrics
duration: ~30min
completed: 2026-02-24
---

# Phase 36 Plan 02: Delete Guard Frontend Summary

**Trash icons on all three catalog tree levels + shared Bootstrap modal with loading/content/error states wired to GetDeleteImpact and DeleteCatalogItem endpoints via initDeleteGuards() — CAT-07 complete**

## Performance

- **Duration:** ~30 min
- **Started:** 2026-02-24 (continuation of Phase 36 Plan 01 session)
- **Completed:** 2026-02-24T09:52Z
- **Tasks:** 2 auto + 1 human-verify (all complete)
- **Files modified:** 2

## Accomplishments

- Trash icon (bi-trash, text-danger) added to Kompetensi, SubKompetensi, and Deliverable rows — Kompetensi and SubKompetensi trash buttons hidden (d-none) until row expanded; Deliverable trash always visible when parent SubKompetensi is open
- Shared #deleteModal with three-state body: loading spinner while GetDeleteImpact is in flight; content state showing item name, children count bullets (for Kompetensi/SubKompetensi), and coachee count (blue info if 0, yellow warning if > 0); error state with alert-danger
- initDeleteGuards() called at all three initCatalogTree() call sites ensuring fresh listener registration on every tree reload
- Two post-verification fixes: Bootstrap collapse event bubbling guard (`e.target !== target`) across all 6 collapse listeners; Deliverable pencil-btn d-none removed (leaf nodes have no collapse toggle)
- Human verification checkpoint passed — full end-to-end delete guard workflow confirmed in browser

## Task Commits

1. **Task 1: Add trash icon buttons to _CatalogTree.cshtml** — `350f119` (feat)
2. **Task 2: Add #deleteModal and initDeleteGuards() to Index.cshtml** — `52deb4b` (feat)
3. **Fix: Stop collapse events bubbling across levels; fix icon column width** — `4eb9f02` (fix)
4. **Fix: Show pencil icon on Deliverable rows (leaf nodes have no collapse toggle)** — `841f40c` (fix)

## Files Created/Modified

- `Views/ProtonCatalog/_CatalogTree.cshtml` — Added trash-btn after pencil-btn on all three levels with correct d-none rules; added me-1 spacing and text-nowrap to action column; removed d-none from Deliverable pencil-btn
- `Views/ProtonCatalog/Index.cshtml` — Added #deleteModal HTML (three-state body + Yes/Delete confirm button); added initDeleteGuards(), escapeHtml(), showDeleteError() functions; added initDeleteGuards() call at all three initCatalogTree() call sites; added `e.target !== target` guard to all 6 collapse listeners

## Decisions Made

- Deliverable pencil-btn had `d-none` in the original Phase 35 implementation — this was a latent bug exposed by adding the trash icon: since Deliverable rows have no collapse toggle, the pencil was never revealed. Fix: removed d-none from Deliverable pencil-btn so both pencil and trash are always visible when the parent SubKompetensi is expanded.
- Bootstrap `show.bs.collapse` and `hide.bs.collapse` events bubble up the DOM — a SubKompetensi collapse event would fire on the Kompetensi container element too, incorrectly hiding the Kompetensi's pencil and trash icons. Fix: `if (e.target !== target) return;` guard on all 6 listeners.
- deleteConfirmBtn is re-cloned on every initDeleteGuards() call via cloneNode(true) + replaceChild — this is the safe pattern for re-initializing listeners on re-render without accumulating duplicates.
- text-nowrap on all three action `<td>` elements prevents pencil and trash from wrapping to separate lines in narrow columns.

## Deviations from Plan

### Auto-fixed Issues (post-verification)

**1. [Rule 1 - Bug] Bootstrap collapse events bubble across nested levels**
- **Found during:** Human verification (Task 3)
- **Issue:** Expanding a SubKompetensi fired `show.bs.collapse` on the parent Kompetensi container element too, because Bootstrap collapse events bubble. This caused the Kompetensi pencil and trash icons to toggle incorrectly when a nested SubKompetensi was expanded/collapsed.
- **Fix:** Added `if (e.target !== target) return;` guard to all 6 collapse listeners (chevron rotation ×2, pencil show/hide ×2, trash show/hide ×2) in initCatalogTree() and initDeleteGuards().
- **Files modified:** Views/ProtonCatalog/Index.cshtml
- **Verification:** Human verification passed after fix — parent icons remain stable when child is toggled.
- **Committed in:** `4eb9f02`

**2. [Rule 1 - Bug] Deliverable pencil icon never visible (latent d-none bug from Phase 35)**
- **Found during:** Human verification (Task 3)
- **Issue:** Deliverable pencil-btn had `d-none` in its class list from Phase 35. Since Deliverable rows are leaf nodes with no collapse toggle, `show.bs.collapse` was never fired on their row — so the pencil was permanently hidden. This bug was masked in Phase 35 because pencil editing of Deliverables was not explicitly verified for the leaf-node case.
- **Fix:** Removed `d-none` from Deliverable pencil-btn class in `_CatalogTree.cshtml` so it renders visible whenever the parent SubKompetensi is expanded.
- **Files modified:** Views/ProtonCatalog/_CatalogTree.cshtml
- **Verification:** Human verification passed after fix — Deliverable pencil icon visible alongside trash icon when parent SubKompetensi is expanded.
- **Committed in:** `841f40c`

**3. [Rule 1 - Bug] Pencil and trash buttons wrap vertically in narrow columns**
- **Found during:** Human verification (Task 3)
- **Issue:** The action column (50px fixed width) caused pencil + trash to render on two lines in some tree depth levels, breaking the intended side-by-side layout.
- **Fix:** Added `text-nowrap` to all three action `<td>` elements in `_CatalogTree.cshtml`.
- **Files modified:** Views/ProtonCatalog/_CatalogTree.cshtml
- **Verification:** Committed with collapse bubbling fix in same commit.
- **Committed in:** `4eb9f02`

---

**Total deviations:** 3 auto-fixed (all Rule 1 — bugs found during human verification)
**Impact on plan:** All three fixes required for correct UI behavior. No scope creep. The collapse bubbling bug and leaf-node d-none bug are structural issues that would have been invisible in unit testing but immediately apparent in browser verification.

## Issues Encountered

None beyond the three auto-fixed bugs above.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 36 complete: CAT-07 (delete guard) fully implemented — trash icons, modal, impact query, cascade delete all working end-to-end
- Phase 37 (reorder/drag) can proceed — the three-level tree structure, initCatalogTree() pattern, and postItem() helper are all ready extension points
- No blockers

---
*Phase: 36-delete-guards*
*Completed: 2026-02-24*

## Self-Check: PASSED

- FOUND: Views/ProtonCatalog/_CatalogTree.cshtml
- FOUND: Views/ProtonCatalog/Index.cshtml
- FOUND: .planning/phases/36-delete-guards/36-02-SUMMARY.md
- FOUND commit 350f119: feat(36-02): add trash icon buttons to _CatalogTree.cshtml
- FOUND commit 52deb4b: feat(36-02): add #deleteModal and initDeleteGuards() to Index.cshtml
- FOUND commit 4eb9f02: fix(36): stop collapse events bubbling across levels; fix icon column width
- FOUND commit 841f40c: fix(36): show pencil icon on Deliverable rows (leaf nodes have no collapse toggle)
