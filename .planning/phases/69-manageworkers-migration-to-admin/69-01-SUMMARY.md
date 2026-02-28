---
phase: 69-manageworkers-migration-to-admin
plan: "01"
subsystem: AdminController / UserRoles / Views/Admin
tags: [migration, authorization, manage-workers, HC-access, helper-extraction]
dependency_graph:
  requires: []
  provides:
    - AdminController.ManageWorkers (GET/POST CRUD)
    - AdminController.CreateWorker (GET/POST)
    - AdminController.EditWorker (GET/POST)
    - AdminController.DeleteWorker (POST)
    - AdminController.ExportWorkers (GET)
    - AdminController.WorkerDetail (GET)
    - AdminController.ImportWorkers (GET/POST)
    - AdminController.DownloadImportTemplate (GET)
    - UserRoles.GetDefaultView() helper
    - Views/Admin/ManageWorkers.cshtml
    - Views/Admin/CreateWorker.cshtml
    - Views/Admin/EditWorker.cshtml
    - Views/Admin/WorkerDetail.cshtml
    - Views/Admin/ImportWorkers.cshtml
  affects:
    - CMPController (ManageWorkers actions still present — removed in Plan 02)
    - Views/CMP/RecordsWorkerList.cshtml (hardcoded JS URL updated)
tech_stack:
  added: []
  patterns:
    - Per-action [Authorize(Roles = "Admin, HC")] override with class-level [Authorize]
    - UserRoles.GetDefaultView() static helper extraction pattern
    - Admin-style breadcrumb navigation (Kelola Data > Manajemen Pekerja > page)
key_files:
  created:
    - Views/Admin/ManageWorkers.cshtml
    - Views/Admin/CreateWorker.cshtml
    - Views/Admin/EditWorker.cshtml
    - Views/Admin/WorkerDetail.cshtml
    - Views/Admin/ImportWorkers.cshtml
  modified:
    - Models/UserRoles.cs (added GetDefaultView() method)
    - Controllers/AdminController.cs (class-level auth changed, 11 ManageWorkers actions added, per-action auth on all existing actions)
    - Views/CMP/RecordsWorkerList.cshtml (JS URL /CMP/WorkerDetail → /Admin/WorkerDetail)
decisions:
  - "Changed AdminController class-level from [Authorize(Roles='Admin')] to [Authorize] (authenticated only); added per-action [Authorize(Roles='Admin')] to all 30+ existing Admin-only actions to preserve access control"
  - "Added per-action [Authorize(Roles='Admin, HC')] to Index() and all 11 ManageWorkers actions — required because ASP.NET Core AND's multiple [Authorize] attributes so class-level Admin-only would block HC even with per-action override"
  - "Flat Views/Admin/ placement for all 5 view files (no subfolder) — no filename conflicts with existing Admin views"
  - "GetDefaultView() uses existing UserRoles constants (Admin, HC, Coach, etc.) matching SectionHead/SrSupervisor to 'Atasan'"
  - "RecordsWorkerList.cshtml JS URL updated as deviation Rule 2 (pitfall identified in RESEARCH.md: hardcoded /CMP/WorkerDetail)"
metrics:
  duration_minutes: 11
  tasks_completed: 2
  files_created: 5
  files_modified: 3
  completed_date: "2026-02-28"
---

# Phase 69 Plan 01: ManageWorkers Migration to Admin — Backend + Views Summary

**One-liner:** Migrated all 11 ManageWorkers action methods from CMPController to AdminController with HC+Admin per-action authorization, extracted GetDefaultView() helper to UserRoles.cs, and created 5 view files in Views/Admin/ with Admin-style breadcrumbs and zero CMP URL references.

## What Was Built

### Task 1: GetDefaultView() helper + AdminController migration

**Models/UserRoles.cs** — Added `GetDefaultView(string roleName)` static method:
- Maps Admin→"Admin", HC→"HC", Coach→"Coach", management roles→"Atasan", default→"Coachee"
- Uses existing UserRoles constants — no string literals in switch arms

**Controllers/AdminController.cs** — Key changes:
1. Class-level `[Authorize(Roles = "Admin")]` changed to `[Authorize]` (authenticated only)
2. Per-action `[Authorize(Roles = "Admin")]` added to all 30+ existing Admin-only actions
3. Per-action `[Authorize(Roles = "Admin, HC")]` added to `Index()` and all 11 ManageWorkers actions
4. All 11 ManageWorkers action methods copied from CMPController (lines 2757-3465)
5. 3 inline role switch statements replaced with `UserRoles.GetDefaultView(model.Role)`
6. All `RedirectToAction("ManageWorkers", "CMP")` updated to `RedirectToAction("ManageWorkers")`

### Task 2: View files in Views/Admin/

5 new files created with:
- Admin-style 3-level breadcrumb (Kelola Data > Manajemen Pekerja > Page)
- All `Url.Action("X", "CMP")` changed to `Url.Action("X", "Admin")`
- All `asp-controller="CMP"` changed to `asp-controller="Admin"`
- Back/Cancel buttons linking to Admin/ManageWorkers (not CMP/ManageWorkers)
- Zero remaining CMP references verified by grep

**Deviation fix:** `Views/CMP/RecordsWorkerList.cshtml` line 643 hardcoded JS URL `/CMP/WorkerDetail?id=` updated to `/Admin/WorkerDetail?id=` (identified in RESEARCH.md pitfall 3, fixed as Rule 2).

## Verification Results

All plan verification criteria met:
- `dotnet build` — 0 errors, 37 warnings (pre-existing)
- `GetDefaultView` in UserRoles.cs — line 74 (method definition)
- `GetDefaultView` called 3 times in AdminController — lines 2781, 2928, 3314
- `[Authorize(Roles = "Admin, HC")]` count: 12 (11 ManageWorkers + 1 Index)
- CMP references in 5 Admin view files: 0
- All 5 view files exist in Views/Admin/

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical Fix] Updated RecordsWorkerList.cshtml hardcoded JS URL**
- **Found during:** Task 2 (pre-identified in RESEARCH.md pitfall 3)
- **Issue:** `Views/CMP/RecordsWorkerList.cshtml` line 643 contained hardcoded `/CMP/WorkerDetail?id=` — after CMP removal in Plan 02, clicking worker names in RecordsWorkerList would return 404
- **Fix:** Updated to `/Admin/WorkerDetail?id=` to point to migrated endpoint
- **Files modified:** `Views/CMP/RecordsWorkerList.cshtml`
- **Commit:** 70a2bb0

## Commits

| Commit | Description |
|--------|-------------|
| 4f2eeaa | feat(69-01): add GetDefaultView() to UserRoles, migrate 11 ManageWorkers actions to AdminController with HC auth |
| 70a2bb0 | feat(69-01): copy 5 ManageWorkers view files to Views/Admin/ with all CMP refs updated to Admin |

## Self-Check: PASSED

Files created:
- Views/Admin/ManageWorkers.cshtml — FOUND
- Views/Admin/CreateWorker.cshtml — FOUND
- Views/Admin/EditWorker.cshtml — FOUND
- Views/Admin/WorkerDetail.cshtml — FOUND
- Views/Admin/ImportWorkers.cshtml — FOUND
- Models/UserRoles.cs (modified) — FOUND with GetDefaultView()
- Controllers/AdminController.cs (modified) — FOUND with 11 ManageWorkers actions

Commits:
- 4f2eeaa — FOUND
- 70a2bb0 — FOUND

Build: 0 errors.
