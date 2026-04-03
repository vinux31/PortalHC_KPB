---
status: human_needed
phase: 295-drag-drop-reorder
verified_by: automated
date: "2026-04-03"
---

# Verification: Phase 295 — Drag-drop Reorder

## Must-Haves

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | ReorderBatch endpoint exists with parentId validation | PASS | OrganizationController.cs:431 — validates all IDs belong to same parent |
| 2 | SortableJS loaded and initialized per tree-children container with group:false | PASS | ManageOrganization.cshtml:120 CDN, orgTree.js:190 group:false |
| 3 | Drag handle visible on hover, hidden by default | PASS | CSS .drag-handle opacity:0, .tree-row:hover .drag-handle opacity:0.5 |
| 4 | Successful drag-drop within siblings saves new order to database | PASS | orgTree.js:200 ajaxPost ReorderBatch, controller sets DisplayOrder sequentially |
| 5 | Failed reorder reverts tree to server state | PASS | orgTree.js:205-210 calls initTree() on failure |

## Acceptance Criteria Check

- [x] ReorderBatch has [HttpPost], [Authorize(Roles = "Admin, HC")], [ValidateAntiForgeryToken]
- [x] Method validates all IDs belong to same parentId
- [x] Method sets DisplayOrder sequentially (1-based)
- [x] SortableJS 1.15.7 CDN loaded in Scripts section
- [x] .drag-handle and .sortable-ghost CSS rules present
- [x] bi-grip-vertical drag-handle in renderNode output
- [x] initSortable() function exists with group:false and handle:'.drag-handle'
- [x] initSortable() called inside initTree() after updateExpandAllButton()
- [x] ajaxPost('/Admin/ReorderBatch') in onEnd callback
- [x] dotnet build succeeds (no CS errors; only MSB3021 file-lock from running process)

## Human Verification

1. **Drag handle visibility**: Hover over a tree row — grip icon should appear on the left
2. **Sibling reorder**: Drag a unit up/down within same parent — toast "Urutan berhasil diubah" should appear
3. **Cross-parent blocked**: Attempt to drag a unit to a different parent's children list — should not be possible (group:false)
4. **Revert on error**: If server returns error, tree should refresh to original order

## Score

5/5 must-haves verified via code inspection.
4 items need human browser testing.
