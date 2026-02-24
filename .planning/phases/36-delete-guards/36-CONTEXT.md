# Phase 36: Delete Guards - Context

**Gathered:** 2026-02-24
**Status:** Ready for planning

<domain>
## Phase Boundary

HC/Admin can delete any catalog item (Kompetensi, SubKompetensi, or Deliverable) only after a modal shows the coachee impact count and child summary, and receives explicit confirmation. Deletion cascades to all child items in the correct order. Add/edit (Phase 35) and reorder (Phase 37) are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Delete trigger visibility

- Delete (trash) icon appears in the **same column as the pencil icon**, on the **far right of the row**
- For Kompetensi and SubKompetensi rows: trash icon is **revealed when the row is expanded** (same pattern as Phase 35 pencil)
- For Deliverable rows (leaf nodes, no expand/collapse): trash icon is **always visible** when the row is visible (Deliverables are only visible when their parent is expanded)
- Icon order when both are visible: **pencil first, then trash** — `✏ 🗑`
- Trash icon styled in **red/danger** (`text-danger`) to signal destructive action; pencil remains muted

### Hard confirmation mechanism

- Confirmation is a single prominent **"Yes, Delete" button** (Bootstrap `btn-danger`) — no typing required
- While the delete AJAX call is in-flight: **button is disabled and shows a spinner** to prevent double-click
- If the delete fails on the server: **show error message inside the modal** (modal stays open, error alert appears within it, user can retry or close)

### Modal content

- **One shared Bootstrap modal** (`#deleteModal`) reused for all delete actions — populated dynamically before opening (Claude decides the implementation pattern)
- **Opening flow**: clicking trash triggers AJAX fetch of impact data → **modal opens immediately with a loading spinner** → populates with real data when response arrives
- **Modal body for Kompetensi/SubKompetensi (has children)**:
  - Item name as modal title: `Delete "Kompetensi A"?`
  - Children summary (bullet list): `• N SubKompetensi` / `• N Deliverables`
  - If coachees > 0: warning line — `⚠ N active coachees have progress on this item or its children.`
  - If coachees = 0: neutral line — `ℹ No active coachees affected.`
  - Footer: `[Cancel]` and `[Yes, Delete]` (danger button)
- **Modal body for Deliverable (leaf node)**:
  - Item name as modal title: `Delete "Deliverable X"?`
  - No children summary
  - Same coachee count line (warning or neutral)
  - Footer: same `[Cancel]` / `[Yes, Delete]`

### Post-delete tree update

- After successful deletion: **modal closes → `reloadTree()` is called** — full tree reloads via AJAX (consistent with Phase 35 add behavior)
- No targeted DOM removal — server-rendered tree is the source of truth

### Claude's Discretion

- Exact Bootstrap modal markup and ID structure
- Loading spinner implementation inside modal (before data loads)
- Error alert styling inside modal on server failure
- Which fields `GetDeleteImpact` returns (count of children per level + coachee count)

</decisions>

<specifics>
## Specific Ideas

- The trash-on-expand pattern mirrors Phase 35's pencil-on-expand — consistent UX across the row action column
- The shared modal approach avoids N modals in the DOM (one per row would bloat the page)

</specifics>

<deferred>
## Deferred Ideas

- None — discussion stayed within phase scope

</deferred>

---

*Phase: 36-delete-guards*
*Context gathered: 2026-02-24*
