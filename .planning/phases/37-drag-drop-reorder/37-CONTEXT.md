# Phase 37: Drag-and-Drop Reorder - Context

**Gathered:** 2026-02-24
**Status:** Ready for planning

<domain>
## Phase Boundary

HC/Admin can drag Kompetensi, SubKompetensi, or Deliverable rows to a new position within their own level — the new order persists immediately via AJAX. Reorder is constrained within the same parent (no cross-parent dragging). Add/edit/delete are already done in Phases 35–36.

</domain>

<decisions>
## Implementation Decisions

### Drag handle placement

- Dedicated **left-side column** — the handle appears in a new narrow column inserted **before the chevron column**
- Icon: **bi-grip-vertical** (⠿)
- Handle is **d-none by default** and revealed **only when the row is expanded** — mirrors the pencil/trash reveal pattern from Phases 35–36
- Because Deliverable rows have no collapse toggle, their handles follow the same always-visible rule as their pencil/trash icons

### Visual feedback during drag

- **Faded/semi-transparent original** stays in-place while dragging; a **placeholder gap** shows the drop target position (SortableJS default `ghostClass` behavior)
- **Cursor:** `grab` on handle hover, `grabbing` while dragging
- **Post-drop:** No success animation — save silently on success; visual order is already correct

### Failed save behavior

- On AJAX failure: **call reloadTree()** to restore the server-true order (same pattern as delete failure)
- Error display: **Claude's discretion** — pick the least disruptive pattern (likely small auto-dismiss alert above the tree)
- **In-flight lock:** Disable all Sortable instances while save is in-flight; re-enable after response (prevents race conditions)
- **Save timing:** Fire POST immediately on each drop (no debounce) — safe because in-flight lock prevents concurrent saves

### Scope and collapse behavior

- **All three levels reorderable:** Kompetensi, SubKompetensi, and Deliverable
- **Kompetensi and SubKompetensi:** Must be **collapsed before dragging** — handle is only visible when expanded, but drag only works on the collapsed parent row (children are not draggable as a group)
- **Deliverable:** Can be dragged without collapsing — they are leaf nodes with no children
- **Cross-track drag:** Not possible — only one track shown at a time; no backend cross-track guard needed
- **Post-reorder refresh:** No reloadTree on success — trust the visual order; reloadTree only on failure

### Claude's Discretion

- Exact SortableJS configuration (`ghostClass`, `chosenClass`, `animation` timing)
- Drop placeholder style (color/style of the target gap)
- Error alert implementation on save failure (position, dismiss timing)
- Urutan field name in DB (existing models use `Urutan` — researcher will confirm actual column names)
- Whether to use a single unified `ReorderCatalogItem` endpoint or three separate endpoints (one per level)

</decisions>

<specifics>
## Specific Ideas

- Collapse-before-drag for Kompetensi/SubKompetensi: handle is revealed on expand, but the drag UX should feel natural — user expands a row to see the handle, then collapses and drags. Claude should implement this naturally (e.g., handle visible on expand, but sortable group operates on parent rows which are collapsed by SortableJS constraints)
- Deliverable rows: always-visible handle (no d-none) matching the always-visible trash/pencil pattern established in Phase 36 fix

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

*Phase: 37-drag-drop-reorder*
*Context gathered: 2026-02-24*
