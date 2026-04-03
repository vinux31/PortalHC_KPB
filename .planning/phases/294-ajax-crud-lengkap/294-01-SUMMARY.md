# Summary: 294-01 AJAX CRUD Modal + Action Dropdown + Tree Refresh

## Status: COMPLETE

## What was done

### Task 1: View cleanup — removed Razor forms, added shared modal
- Removed collapsible Add form (`#addUnitForm`), inline Edit card (`ViewBag.EditUnit`), TempData alert blocks
- Changed "Tambah Unit" button to `onclick="openAddModal()"`
- Added shared Add/Edit modal (`#unitModal`) with id, name, parent fields
- Converted delete modal from form-submit to AJAX (`onclick="submitDelete()"`)
- Added `#deleteModalWarning` for child count warning
- Added `shared-toast.js` script reference
- Added `@Html.AntiForgeryToken()` in tree card for CSRF
- Added `.action-dropdown` hover CSS

### Task 2: orgTree.js — kebab dropdown, modal functions, AJAX CRUD, tree refresh
- Added `_flatUnits` and `_expandedIds` module-level variables
- Added `saveExpandState()` and `setDefaultExpandState()` for expand persistence
- Rewrote `renderNode()` with kebab dropdown (3-dots-vertical) containing: Tambah Sub-unit, Edit, Toggle, Hapus
- Added `findUnit()`, `getDescendantIds()`, `populateParentDropdown()` helpers
- Added `openAddModal(parentId)`, `openEditModal(id)`, `submitUnitModal()` for Add/Edit
- Added `doToggle(id)` for toggle active/inactive
- Added `openDeleteModal(id, name, childCount)` and `submitDelete()` for delete
- Added `.action-dropdown` guard in click handler to prevent expand/collapse on dropdown click
- Updated `initTree()` to preserve expand state on refresh

## Files modified
- `Views/Admin/ManageOrganization.cshtml` — view rewrite (204 → 118 lines)
- `wwwroot/js/orgTree.js` — AJAX CRUD functions (169 → 269 lines)

## Duration
~8 minutes
