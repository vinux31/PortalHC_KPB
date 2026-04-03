---
phase: 295
plan: 1
title: "SortableJS drag-drop reorder sibling-only"
status: complete
started: "2026-04-03"
completed: "2026-04-03"
duration_minutes: 8
tasks_completed: 2
tasks_total: 2
---

# Summary: 295-01 SortableJS Drag-drop Reorder Sibling-Only

## What was built

Drag-and-drop reorder of organization units within the same parent using SortableJS. Cross-parent drag is blocked both client-side (group: false) and server-side (parentId validation).

## Key files

### Created
(none)

### Modified
- `Controllers/OrganizationController.cs` — Added `ReorderBatch` endpoint with sibling validation
- `wwwroot/js/orgTree.js` — Added drag handle in renderNode, `initSortable()` function with SortableJS
- `Views/Admin/ManageOrganization.cshtml` — Added SortableJS CDN, drag-handle CSS, sortable ghost/chosen styles

## Task Details

| # | Task | Status |
|---|------|--------|
| 1 | Add ReorderBatch endpoint to OrganizationController | Done |
| 2 | Add drag handle to renderNode and SortableJS initialization | Done |

## Deviations

None.

## Self-Check: PASSED
