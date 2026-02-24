---
phase: 37-drag-drop-reorder
plan: "02"
status: complete
completed: 2026-02-24
---

## What Was Built

SortableJS drag-and-drop was implemented and then removed per user decision after testing revealed the UX was too complex to work reliably with the nested-table tree structure (collapse containers don't move with parent rows in SortableJS).

**Net result — one fix shipped:**
- `fix(catalog): preserve expanded state after reloadTree()` — before reloading the tree HTML, captures all currently-open collapse IDs and restores them after `initCatalogTree()` runs. This ensures adding a Kompetensi, SubKompetensi, or Deliverable no longer collapses the parent rows.

## Key Decisions

- **Reorder feature removed:** Drag-and-drop reorder (CAT-08) dropped entirely — the nested-table structure makes SortableJS unreliable (collapse containers don't move with parent rows). Feature is not required for v1.9 launch.
- **Collapse-state preservation added:** `reloadTree()` now saves/restores `.collapse.show` IDs so add-item actions feel seamless.

## Files Modified

- `Views/ProtonCatalog/Index.cshtml` — collapse-state preservation in `reloadTree()`
- `Views/ProtonCatalog/_CatalogTree.cshtml` — reverted to clean 3-column layout (grip handles removed)
- `Views/Shared/_Layout.cshtml` — SortableJS CDN removed
- `Controllers/ProtonCatalogController.cs` — Reorder* actions removed

## Self-Check: PASSED
