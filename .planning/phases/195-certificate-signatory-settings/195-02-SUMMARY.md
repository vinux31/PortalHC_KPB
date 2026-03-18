---
phase: 195-certificate-signatory-settings
plan: 02
subsystem: admin-categories
tags: [categories, hierarchy, signatory, certificate, admin-crud]
dependency_graph:
  requires: [195-01]
  provides: [ManageCategories-hierarchy-ui, signatory-dropdown, optgroup-category-dropdowns]
  affects: [CreateAssessment, EditAssessment, ManageCategories]
tech_stack:
  added: [tom-select@2 CDN]
  patterns: [hierarchical-tree-table, optgroup-select, P-Sign preview]
key_files:
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/ManageCategories.cshtml
    - Views/Admin/CreateAssessment.cshtml
    - Views/Admin/EditAssessment.cshtml
decisions:
  - "Inline if/else for selected attribute instead of conditional expression — RZ1031 prevents C# expressions in tag helper attribute declarations"
  - "tom-select loaded via CDN (no local lib) — consistent with no-CDN pattern except this is a lightweight enhancement"
  - "SetCategoriesViewBag() private helper reuses query logic across GET/POST for ManageCategories"
metrics:
  duration: 15min
  completed: "2026-03-18"
  tasks_completed: 3
  files_modified: 4
---

# Phase 195 Plan 02: ManageCategories Hierarchy + Signatory + Optgroup Dropdowns Summary

Admin ManageCategories CRUD updated with parent-child hierarchy, signatory selection, P-Sign preview, and optgroup category dropdowns in CreateAssessment and EditAssessment.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Update AdminController ManageCategories CRUD for hierarchy and signatory | c6ea6e0 | Controllers/AdminController.cs |
| 2 | Update ManageCategories view with tree table, signatory dropdown, P-Sign preview | d3c8819 | Views/Admin/ManageCategories.cshtml |
| 3 | Update CreateAssessment and EditAssessment optgroup dropdowns + fix build error | 0148494 | Views/Admin/CreateAssessment.cshtml, Views/Admin/EditAssessment.cshtml, Views/Admin/ManageCategories.cshtml |

## What Was Built

- `SetCategoriesViewBag()` private helper in AdminController: loads hierarchical parent tree with children/grandchildren, all users for signatory, potential parent categories (depth 0-1)
- `AddCategory` POST: accepts `int? parentId` and `string? signatoryUserId`
- `EditCategory` GET/POST: uses `SetCategoriesViewBag()`, accepts hierarchy + signatory fields
- `DeleteCategory` POST: children pre-check with `AnyAsync(c => c.ParentId == id)` — returns error if children exist
- `CreateAssessment` GET and `EditAssessment` GET: add `ViewBag.ParentCategories` for optgroup rendering
- ManageCategories.cshtml fully rewritten: indented tree table with parent badge, add/edit forms with parent dropdown and searchable signatory dropdown, P-Sign preview block, disabled delete button with tooltip for parent categories
- CreateAssessment.cshtml and EditAssessment.cshtml: optgroup-grouped category dropdowns using `ViewBag.ParentCategories`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] RZ1031 Razor tag helper attribute error in ManageCategories edit form**
- **Found during:** Task 3 build verification
- **Issue:** `@(isSelected ? "selected" : "")` as an attribute expression is invalid in Razor tag helper contexts — throws RZ1031
- **Fix:** Replaced with if/else blocks rendering separate `<option selected>` vs `<option>` elements
- **Files modified:** Views/Admin/ManageCategories.cshtml
- **Commit:** 0148494 (bundled into Task 3 commit)

## Self-Check: PASSED

- Controllers/AdminController.cs: contains `SetCategoriesViewBag`, `int? parentId, string? signatoryUserId` (AddCategory), `AnyAsync(c => c.ParentId == id)`, `ViewBag.ParentCategories`
- Views/Admin/ManageCategories.cshtml: contains `name="parentId"`, `name="signatoryUserId"`, `id="psignPreview"`, `bi bi-arrow-return-right`, `badge bg-light text-secondary border`, `Tidak ada (Kategori Utama)`, `tom-select`, `aria-label="Edit kategori"`, `Hapus sub-kategori terlebih dahulu`, `Penandatangan`
- Views/Admin/CreateAssessment.cshtml: contains `<optgroup label=`, `ViewBag.ParentCategories`, `Pilih Kategori`, `(Induk)`
- Views/Admin/EditAssessment.cshtml: contains `<optgroup label=`, `ViewBag.ParentCategories`
- Build: succeeded
