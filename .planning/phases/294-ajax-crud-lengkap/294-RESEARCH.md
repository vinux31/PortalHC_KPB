# Phase 294: AJAX CRUD Lengkap — Research

**Researched:** 2026-04-03
**Status:** Complete

## Current State Analysis

### View (ManageOrganization.cshtml) — 204 lines
- **Lines 39-42:** "Tambah Unit" button triggers collapse `#addUnitForm` — MUST be replaced with modal open
- **Lines 62-98:** Collapsible Add form with Razor `@Html.AntiForgeryToken()` and `ViewBag.PotentialParents` — MUST be removed entirely
- **Lines 100-152:** Inline Edit card (yellow) using `ViewBag.EditUnit` — MUST be removed entirely
- **Lines 166-188:** Delete modal `#deleteModal` with form POST — MUST be converted to AJAX submit
- **Lines 190-204:** Scripts section — delete modal listener uses `relatedTarget` pattern

### orgTree.js — 169 lines
- `renderNode(node, level)` returns HTML string — MUST be modified to add kebab dropdown per node
- `initTree()` fetches `/Admin/GetOrganizationTree` — reusable for refresh after CRUD
- Event delegation on `container.click` for expand/collapse — MUST not conflict with dropdown/kebab clicks
- `buildTree(flatList)` converts flat array to tree — can reuse flat data for parent dropdown population

### OrganizationController.cs — 427 lines
- All 4 CRUD endpoints already have dual-response (IsAjaxRequest check) — no backend changes needed
- `AddOrganizationUnit(string name, int? parentId)` — params: name, parentId
- `EditOrganizationUnit(int id, string name, int? parentId)` — params: id, name, parentId
- `ToggleOrganizationUnitActive(int id)` — param: id
- `DeleteOrganizationUnit(int id)` — param: id
- All return `{success: bool, message: string}` JSON for AJAX

### shared-toast.js — 15 lines
- `showToast(message, type)` — type: 'success' or 'danger'
- Already loaded globally in layout (no extra script tag needed)

## Key Technical Decisions

### 1. Event Delegation Conflict
The current `container.click` handler on `.tree-row` triggers expand/collapse. Adding kebab dropdown inside `.tree-row` will trigger both. Solution: kebab click must `e.stopPropagation()` OR check `e.target` is not inside dropdown before toggling expand.

### 2. Parent Dropdown in Modal
CONTEXT D-02 says populate from flat JSON already loaded (not re-fetch). The flat array from `initTree()` is not stored globally. Solution: store last-fetched flat data in a module-level variable (e.g., `let _flatUnits = []`) and reuse for modal dropdown.

### 3. Exclude Self + Descendants in Edit Modal
When editing unit X, parent dropdown must exclude X and all descendants. Use `buildTree()` logic or traverse flat array to find descendants by walking parentId chain.

### 4. Expand State Preservation (D-14)
Before `initTree()` re-render, collect all `.tree-node[data-expanded="true"]` IDs. After render, restore expanded state.

### 5. Children Warning (D-10)
For toggle/delete on nodes with children: check `node.children.length > 0` in JS and show warning text in modal/confirm. Backend also validates but frontend should warn first.

## Integration Points

- `/Admin/AddOrganizationUnit` — POST {name, parentId}
- `/Admin/EditOrganizationUnit` — POST {id, name, parentId}
- `/Admin/ToggleOrganizationUnitActive` — POST {id}
- `/Admin/DeleteOrganizationUnit` — POST {id}
- `/Admin/GetOrganizationTree` — GET, returns flat JSON array

## Risk Assessment

- **Low risk:** Backend is fully ready, no changes needed
- **Medium risk:** Event delegation conflict between expand/collapse and dropdown — must be handled carefully
- **Low risk:** Modal form validation — Bootstrap 5 validation classes are straightforward

## RESEARCH COMPLETE
