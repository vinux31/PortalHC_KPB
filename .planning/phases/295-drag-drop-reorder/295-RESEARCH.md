# Phase 295: Drag-drop Reorder — Research

**Researched:** 2026-04-03
**Status:** Complete

## RESEARCH COMPLETE

## 1. Current State Analysis

### Existing Reorder Endpoint
`OrganizationController.cs` line 389-425 has `ReorderOrganizationUnit(int id, string direction)` — swaps DisplayOrder between adjacent siblings. This stays for backward compat.

### Tree Structure
- `orgTree.js` renders tree as `ul.tree-root > li.tree-node > div.tree-row + ul.tree-children`
- Each `li.tree-node` has `data-id` attribute
- Each `ul.tree-children` contains sibling nodes under one parent
- `buildTree()` converts flat JSON to nested tree; `renderNode()` generates HTML
- `initTree()` fetches `/Admin/GetOrganizationTree`, builds tree, renders to `#org-tree-container`

### Key Patterns
- `ajaxPost(url, data)` handles CSRF + fetch POST
- After mutations, full re-render via `initTree()` with expand state preservation (Phase 294 pattern)
- `showToast(message, type)` from `shared-toast.js` for feedback

## 2. SortableJS Integration Approach

### Library Loading
- SortableJS 1.15.7 via CDN: `https://cdn.jsdelivr.net/npm/sortablejs@1.15.7/Sortable.min.js`
- Add `<script>` tag in ManageOrganization.cshtml before orgTree.js
- Decision from STATE.md confirms CDN approach

### Per-Parent Instance Strategy
Each `ul.tree-children` and `ul.tree-root` gets its own `Sortable.create()` instance with `group: false`. This means:
- Items can only sort within their parent container
- Cross-parent drag is physically impossible (SortableJS enforces it)
- Each container is independent

### Configuration
```javascript
Sortable.create(container, {
    handle: '.drag-handle',        // Only drag via grip icon
    animation: 150,                // Smooth reorder animation
    ghostClass: 'sortable-ghost',  // Semi-transparent ghost
    chosenClass: 'sortable-chosen',
    group: false,                  // CRITICAL: no cross-parent
    onEnd: function(evt) {         // After drop
        // Collect ordered IDs from this container
        // POST to ReorderBatch endpoint
    }
});
```

### Drag Handle
- Add grip icon `<i class="bi bi-grip-vertical drag-handle">` before chevron in `renderNode()`
- Show on hover only via CSS: `.drag-handle { opacity: 0; } .tree-row:hover .drag-handle { opacity: 0.5; }`
- Cursor: `grab` on handle, `grabbing` while dragging

## 3. New Backend Endpoint: ReorderBatch

### Design
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ReorderBatch(int? parentId, string orderedIds)
```

- `parentId`: null for root siblings, int for children of specific parent
- `orderedIds`: comma-separated IDs in new order, e.g., "3,1,2"
- Server validates ALL ids belong to the same parent before updating
- Sets `DisplayOrder = index` for each (0-based or 1-based)
- Returns `{success: true, message: "Urutan berhasil diubah."}`

### Validation Requirements
1. Parse orderedIds into int array
2. Load all units with matching parentId
3. Verify every ID in orderedIds belongs to that parent (security: prevent cross-parent manipulation)
4. Verify count matches (no missing/extra IDs)
5. Update DisplayOrder sequentially
6. SaveChanges

## 4. SortableJS Initialization After Render

### Key Challenge
Tree is rendered dynamically by `initTree()`. SortableJS must be initialized AFTER tree HTML is in DOM.

### Solution
Add `initSortable()` function called at end of `initTree()` after `container.innerHTML = html`:
```javascript
function initSortable() {
    document.querySelectorAll('.tree-children, .tree-root').forEach(ul => {
        Sortable.create(ul, { /* config */ });
    });
}
```

### After Re-render
When `initTree()` is called again (after CRUD ops in Phase 294), old Sortable instances are destroyed because DOM is replaced. New `initSortable()` call at end of `initTree()` creates fresh instances.

## 5. Collecting Ordered IDs on Drop

In `onEnd` callback:
```javascript
onEnd: function(evt) {
    const container = evt.from; // ul element
    const parentNode = container.closest('.tree-node');
    const parentId = parentNode ? parentNode.dataset.id : '';
    const orderedIds = Array.from(container.children)
        .map(li => li.dataset.id)
        .join(',');

    ajaxPost('/Admin/ReorderBatch', { parentId, orderedIds })
        .then(res => {
            if (res.success) showToast(res.message, 'success');
            else { showToast(res.message, 'danger'); initTree(); } // revert
        })
        .catch(() => { showToast('Gagal mengubah urutan.', 'danger'); initTree(); });
}
```

## 6. CSS for Drag States

```css
.drag-handle { opacity: 0; cursor: grab; transition: opacity 150ms; color: #6c757d; }
.tree-row:hover .drag-handle { opacity: 0.5; }
.drag-handle:hover { opacity: 1 !important; }
.sortable-ghost { opacity: 0.4; }
.sortable-chosen { background-color: #e3f2fd; border-radius: 4px; }
```

## 7. Expand State Preservation

When drag completes, we do NOT re-render the full tree (unlike CRUD ops). SortableJS physically moves the DOM elements, so expand state is naturally preserved. Only on error do we call `initTree()` to revert.

## 8. Edge Cases

1. **Single child**: Drag handle still shows but dragging has no effect (only 1 item)
2. **Collapsed children**: Drag handle on parent row; children not visible = not draggable (correct behavior)
3. **Concurrent edits**: If another admin changes structure, batch reorder may fail validation → toast error + revert
4. **Empty orderedIds**: Server returns error

## Validation Architecture

### Functional Validation
- Drag within siblings: reorder persists after page refresh
- Cross-parent drag: physically blocked by SortableJS group:false
- Drag handle: visible on hover, invisible otherwise
- Error state: failed POST reverts to server state

### Integration Validation
- SortableJS loads without conflict with existing Bootstrap/jQuery
- initSortable() called after every initTree() render
- ajaxPost CSRF works for new endpoint
- Existing ReorderOrganizationUnit(id, direction) still functional

---
*Phase: 295-drag-drop-reorder*
*Research completed: 2026-04-03*
