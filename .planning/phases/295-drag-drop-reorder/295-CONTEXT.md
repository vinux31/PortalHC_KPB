# Phase 295: Drag-drop Reorder - Context

**Gathered:** 2026-04-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC dapat mengubah urutan unit dalam sibling yang sama dengan drag-and-drop. Cross-parent drag diblokir sepenuhnya — node tidak bisa pindah ke parent lain via drag.

</domain>

<decisions>
## Implementation Decisions

### Backend Endpoint
- **D-01:** Buat endpoint baru batch reorder (misal `ReorderBatch`) yang terima `{parentId, orderedIds: [1,3,2]}` — satu POST set semua DisplayOrder sekaligus
- **D-02:** Endpoint existing `ReorderOrganizationUnit(id, direction)` tetap ada (backward compat), endpoint baru untuk drag-drop

### Drag Handle & Visual
- **D-03:** Icon grip ⠿⠿ (dots) di kiri tree row, muncul on-hover saja — cursor berubah `grab` saat hover handle
- **D-04:** Visual saat dragging: ghost semi-transparent + placeholder line biru di posisi drop (SortableJS default behavior)

### Cross-parent Blocking
- **D-05:** Setiap parent punya SortableJS instance sendiri dengan `group: false` — item hanya bisa di-drag dalam sibling
- **D-06:** Coba drag ke luar parent → snap back otomatis. Tidak perlu visual "no drop" indicator khusus.

### Claude's Discretion
- SortableJS configuration details (animation duration, ghostClass, chosenClass)
- Exact grip icon styling dan hover transition
- Loading state saat batch reorder request in-flight
- Error handling jika reorder gagal (toast + revert?)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing Code — Controller
- `Controllers/OrganizationController.cs` line 389-427 — `ReorderOrganizationUnit(id, direction)` existing swap logic, tetap ada
- `Controllers/AdminBaseController.cs` — Base class

### Existing Code — JS & View
- `wwwroot/js/orgTree.js` — ajaxPost(), ajaxGet(), buildTree(), renderNode(), initTree() — akan di-extend dengan drag-drop
- `Views/Admin/ManageOrganization.cshtml` — Tree container, akan ditambah SortableJS script reference
- `wwwroot/js/shared-toast.js` — showToast() untuk feedback

### Prior Phase Context
- `.planning/phases/292-backend-ajax-endpoints/292-CONTEXT.md` — ajaxPost utility, dual-response pattern, CSRF
- `.planning/phases/293-view-shell-tree-rendering/293-CONTEXT.md` — Tree rendering, expand/collapse, renderNode()
- `.planning/phases/294-ajax-crud-lengkap/294-CONTEXT.md` — Full re-render + preserve expand state, action dropdown

### Library
- SortableJS — CDN atau npm, library untuk drag-drop sorting

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ajaxPost(url, data)` — untuk POST batch reorder
- `initTree()` + expand state preserve (Phase 294) — re-render setelah reorder
- `renderNode(node, level)` — perlu extend untuk drag handle icon
- `showToast(message, type)` — feedback sukses/gagal reorder

### Established Patterns
- Full re-render strategy setelah operasi (Phase 294 decision D-13, D-14)
- Bootstrap 5 UI patterns
- Tree `ul/li` structure dengan `.tree-children` dan `.tree-node`

### Integration Points
- SortableJS di-attach ke setiap `.tree-children` container (per-parent group)
- `renderNode()` perlu tambah grip handle HTML
- Endpoint baru `ReorderBatch` di OrganizationController
- Tree refresh setelah reorder via initTree() + preserve state

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 295-drag-drop-reorder*
*Context gathered: 2026-04-03*
