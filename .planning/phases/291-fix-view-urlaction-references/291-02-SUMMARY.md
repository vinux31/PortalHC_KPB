---
phase: 291-fix-view-urlaction-references
plan: "02"
subsystem: Views/Admin
tags: [bugfix, url-routing, organization, document]
dependency_graph:
  requires: []
  provides: [valid-organization-urls, valid-document-urls]
  affects: [ManageOrganization, KkjMatrix, KkjUpload, KkjFileHistory, CpdpFiles, CpdpUpload, CpdpFileHistory]
tech_stack:
  added: []
  patterns: [Url.Action with correct controller name]
key_files:
  created: []
  modified:
    - Views/Admin/ManageOrganization.cshtml
    - Views/Admin/KkjMatrix.cshtml
    - Views/Admin/KkjUpload.cshtml
    - Views/Admin/KkjFileHistory.cshtml
    - Views/Admin/CpdpFiles.cshtml
    - Views/Admin/CpdpUpload.cshtml
    - Views/Admin/CpdpFileHistory.cshtml
decisions:
  - "Organization CRUD actions (add/edit/delete/reorder/toggle) diroute ke OrganizationController bukan AdminController"
  - "Document actions (KKJ+CPDP upload/download/history) diroute ke DocumentAdminController bukan AdminController"
  - "KkjUpload dan CpdpUpload asp-controller tag helper juga diperbaiki ke controller yang tepat"
metrics:
  duration: 8 minutes
  completed: 2026-04-02
  tasks_completed: 2
  files_modified: 7
---

# Phase 291 Plan 02: Fix Organization + Document View Url.Action References Summary

**One-liner:** Perbaiki 38 broken Url.Action references di 7 view files dari "Admin" ke "Organization" dan "DocumentAdmin" controller yang benar.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Fix Organization view Url.Action references | 71925ad4 | Views/Admin/ManageOrganization.cshtml |
| 2 | Fix Document views (KKJ + CPDP) Url.Action references | de33ee50 | 6 KKJ+CPDP view files |

## Changes Made

### Task 1: ManageOrganization.cshtml (16 replacements)
- `AddOrganizationUnit`, `EditOrganizationUnit`, `DeleteOrganizationUnit` -> `Organization`
- `ManageOrganization` (3x edit links) -> `Organization`
- `ReorderOrganizationUnit` (6x root/child/grandchild) -> `Organization`
- `ToggleOrganizationUnitActive` (3x) -> `Organization`
- `Url.Action("Index", "Admin")` tetap tidak berubah (valid)

### Task 2: 6 Document views (22 replacements + 2 asp-controller fixes)
- KkjMatrix.cshtml: KkjMatrix, KkjFileDownload, KkjUpload, KkjFileHistory -> DocumentAdmin
- KkjUpload.cshtml: KkjMatrix (3x) dan asp-controller -> DocumentAdmin
- KkjFileHistory.cshtml: KkjMatrix (2x), KkjFileDownload -> DocumentAdmin
- CpdpFiles.cshtml: CpdpFiles, CpdpFileDownload, CpdpUpload, CpdpFileHistory -> DocumentAdmin
- CpdpUpload.cshtml: CpdpFiles (3x) dan asp-controller -> DocumentAdmin
- CpdpFileHistory.cshtml: CpdpFiles (2x), CpdpFileDownload -> DocumentAdmin

## Verification

- Zero matches untuk non-Index Url.Action("X", "Admin") di semua 7 files
- Build: 0 errors, 70 warnings (semua pre-existing)

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- Commits exist: 71925ad4, de33ee50 (verified via git log)
- All 7 modified files verified with grep (zero broken refs)
- Build passed: 0 errors
