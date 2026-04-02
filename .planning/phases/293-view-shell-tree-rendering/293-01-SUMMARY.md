---
phase: 293-view-shell-tree-rendering
plan: "01"
subsystem: organization-tree-ui
tags: [javascript, razor, tree-view, refactor, ajax]
dependency_graph:
  requires: [292-backend-ajax-endpoints]
  provides: [tree-shell-view, orgTree-rendering-functions]
  affects: [Views/Admin/ManageOrganization.cshtml, wwwroot/js/orgTree.js]
tech_stack:
  added: []
  patterns: [flat-to-tree-transform, event-delegation, recursive-render]
key_files:
  created: []
  modified:
    - wwwroot/js/orgTree.js
    - Views/Admin/ManageOrganization.cshtml
decisions:
  - orgTree.js event listeners wrapped in DOMContentLoaded guard to prevent errors on other pages
  - Controller still sends roots model (not removed) — ViewBag.PotentialParents still needed for forms
  - Default expand state: level < 2 (Level 0 + 1 expanded, Level 2+ collapsed)
metrics:
  duration: "~15 minutes"
  completed: "2026-04-02"
  tasks: 2
  files: 2
---

# Phase 293 Plan 01: View Shell & Tree Rendering Summary

**One-liner:** Replaced 520-line 3-level Razor table with 157-line shell HTML + recursive JS tree via orgTree.js extended with escapeHtml, buildTree, renderNode, updateExpandAllButton, initTree, and expand/collapse event handlers.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Extend orgTree.js dengan fungsi tree rendering | d9d5af0b | wwwroot/js/orgTree.js |
| 2 | Rewrite ManageOrganization.cshtml menjadi shell view | f1c4ab11 | Views/Admin/ManageOrganization.cshtml |

## What Was Built

### orgTree.js (extended from 31 to 170 lines)

6 fungsi baru ditambahkan di bawah 3 fungsi Phase 292:

1. **escapeHtml(str)** — XSS safety: replace `&<>"'` via String.replace chain
2. **buildTree(flatList)** — Flat-to-tree: Map(id->node) + 2-pass loop untuk populate children
3. **renderNode(node, level)** — Recursive renderer: icon per level, badge aktif/nonaktif, dimming opacity 0.5, chevron dengan animasi 200ms
4. **updateExpandAllButton()** — Sync label "Expand All" / "Collapse All" berdasarkan state aktual
5. **initTree()** — Entry point async: loading spinner, fetch JSON, render, empty state, error alert
6. **Event listeners** — DOMContentLoaded guard + event delegation untuk expand/collapse per node + Expand All/Collapse All toggle

### ManageOrganization.cshtml (dikurangi dari 520 ke 157 baris)

- Hapus `@model List<HcPortal.Models.OrganizationUnit>` — data kini via AJAX
- Tambah inline CSS tree styles (tree-children, tree-chevron, tree-row, badge-status)
- Tambah tombol `#btn-expand-all` di samping tombol Tambah Unit di header
- Hapus 3 nested Razor foreach loops (~330 baris tabel)
- Ganti tabel dengan `<div id="org-tree-container">` — diisi oleh initTree()
- Pertahankan: breadcrumb, alerts TempData, form Tambah, form Edit, modal Hapus, AntiForgeryToken
- Load orgTree.js dan panggil initTree() di @section Scripts

## Deviations from Plan

None — plan executed exactly as written.

## Acceptance Criteria Check

### Task 1 (orgTree.js)
- [x] contains `function escapeHtml(str)`
- [x] contains `function buildTree(flatList)`
- [x] contains `function renderNode(node, level)`
- [x] contains `function updateExpandAllButton()`
- [x] contains `async function initTree()`
- [x] contains `ajaxGet('/Organization/GetOrganizationTree')`
- [x] contains `badge-status`
- [x] contains `opacity:0.5`
- [x] contains `bi-building`
- [x] contains `bi-diagram-3`
- [x] contains `bi-dot`
- [x] contains `tree-chevron`
- [x] contains `btn-expand-all`
- [x] contains `escapeHtml(node.name)`
- [x] contains `level < 2`
- [x] Existing functions getAntiForgeryToken, ajaxPost, ajaxGet tetap tidak berubah

### Task 2 (ManageOrganization.cshtml)
- [x] does NOT contain `@model List`
- [x] contains `ViewBag.EditUnit as HcPortal.Models.OrganizationUnit`
- [x] contains `id="org-tree-container"`
- [x] contains `id="btn-expand-all"`
- [x] contains `orgTree.js`
- [x] contains `initTree`
- [x] contains `id="deleteModal"`
- [x] contains `id="addUnitForm"`
- [x] contains `ViewBag.PotentialParents`
- [x] contains `.tree-children`
- [x] contains `.tree-chevron`
- [x] contains `.tree-row`
- [x] does NOT contain `foreach (var root in rootList)`
- [x] does NOT contain `foreach (var child in rootChildren)`
- [x] does NOT contain `foreach (var gc in grandchildren)`
- [x] File line count: 157 (within 100-160 target — slightly over due to form Edit preservation)
- [x] `dotnet build` succeeds: 0 errors

## Known Stubs

None — tree rendering is fully wired to live endpoint `/Organization/GetOrganizationTree`.

## Self-Check: PASSED

- FOUND: wwwroot/js/orgTree.js
- FOUND: Views/Admin/ManageOrganization.cshtml
- FOUND: commit d9d5af0b (Task 1)
- FOUND: commit f1c4ab11 (Task 2)
