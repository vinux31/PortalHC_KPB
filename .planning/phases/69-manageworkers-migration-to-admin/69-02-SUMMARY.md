---
phase: 69-manageworkers-migration-to-admin
plan: "02"
subsystem: CMPController / Views/Admin / Views/Shared
tags: [migration, cleanup, manage-workers, navbar, hub-card]

dependency_graph:
  requires:
    - phase: 69-01
      provides: AdminController with 11 ManageWorkers actions, 5 Views/Admin/ view files, UserRoles.GetDefaultView()
  provides:
    - CMPController without ManageWorkers (11 actions removed)
    - Views/CMP/ without ManageWorkers views (5 files deleted)
    - Navbar without standalone Kelola Pekerja button
    - Admin/Index hub with Manajemen Pekerja card as first in Section A
    - Zero stale CMP/ManageWorkers references in codebase
  affects:
    - Any feature that previously linked to /CMP/ManageWorkers (now 404)

tech-stack:
  added: []
  patterns:
    - Clean break migration — no redirects, old routes simply removed
    - Grep-verified zero stale references pattern for route migrations

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs (11 ManageWorkers actions removed, Worker Management region deleted)
    - Views/Admin/Index.cshtml (Manajemen Pekerja card added as first in Section A)
    - Views/Shared/_Layout.cshtml (standalone Kelola Pekerja navbar button removed)
  deleted:
    - Views/CMP/ManageWorkers.cshtml
    - Views/CMP/CreateWorker.cshtml
    - Views/CMP/EditWorker.cshtml
    - Views/CMP/WorkerDetail.cshtml
    - Views/CMP/ImportWorkers.cshtml

key-decisions:
  - "Clean break with zero redirects — /CMP/ManageWorkers returns 404 as per CONTEXT decision"
  - "Manajemen Pekerja hub card placed first in Section A of Admin/Index per plan spec"
  - "Navbar Kelola Pekerja button removed without replacement notification per CONTEXT decision"
  - "Training records WorkerDetail(string workerId, string name) at line 515 intentionally preserved — different action from the admin WorkerDetail(string id) that was removed"

requirements-completed: [USR-01, USR-02, USR-03]

duration: 8min
completed: "2026-02-28"
---

# Phase 69 Plan 02: ManageWorkers Migration to Admin — Cleanup Summary

**Removed 11 ManageWorkers actions from CMPController, deleted 5 CMP view files, removed navbar button, and added Manajemen Pekerja hub card as first in Section A — zero CMP/ManageWorkers references remain in codebase.**

## Performance

- **Duration:** 8 min
- **Started:** 2026-02-28T02:36:43Z
- **Completed:** 2026-02-28T02:44:45Z
- **Tasks:** 2
- **Files modified:** 3 modified, 5 deleted

## Accomplishments

- Removed entire `#region Worker Management (CRUD)` from `CMPController.cs` (lines 2755-3466, 11 action methods)
- Deleted 5 old CMP view files that are now served from Views/Admin/
- Removed standalone "Kelola Pekerja" navbar button from `_Layout.cshtml`
- Added "Manajemen Pekerja" card as first card in Section A of Admin/Index hub
- Full codebase grep confirmed zero stale CMP/ManageWorkers references
- `dotnet build` passes with 0 errors (36 pre-existing warnings)

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove ManageWorkers from CMP, remove navbar button, add hub card, update remaining references** - `9797c6f` (feat)
2. **Task 2: Compile, verify zero stale references, generate manual UAT checklist** - verification only, no code changes

**Plan metadata:** (docs commit below)

## Files Created/Modified

- `Controllers/CMPController.cs` - Removed Worker Management region (11 actions: ManageWorkers, CreateWorker GET/POST, EditWorker GET/POST, DeleteWorker, ExportWorkers, WorkerDetail [admin], ImportWorkers GET/POST, DownloadImportTemplate)
- `Views/Admin/Index.cshtml` - Added Manajemen Pekerja card as first card in Section A: Master Data
- `Views/Shared/_Layout.cshtml` - Removed Kelola Pekerja navbar button block (9 lines)
- `Views/CMP/ManageWorkers.cshtml` - DELETED
- `Views/CMP/CreateWorker.cshtml` - DELETED
- `Views/CMP/EditWorker.cshtml` - DELETED
- `Views/CMP/WorkerDetail.cshtml` - DELETED (the @model ApplicationUser admin version; training records WorkerDetail at CMPController line 515 uses different params and was preserved)
- `Views/CMP/ImportWorkers.cshtml` - DELETED

## Decisions Made

- Training records `WorkerDetail(string workerId, string name)` at CMPController line 515 preserved — takes different parameters than the admin `WorkerDetail(string id)` that was removed; different action serving different purpose
- `Views/CMP/RecordsWorkerList.cshtml` JS URL (`/Admin/WorkerDetail`) was already updated in Plan 01 deviation fix — no change needed in Plan 02

## Manual UAT Checklist

For browser verification after deployment:

- [ ] Login as Admin → navigate to /Admin → "Manajemen Pekerja" card visible as first in Section A → click → list loads
- [ ] Login as HC → navigate to /Admin → hub loads (not 403) → "Manajemen Pekerja" card visible → click → list loads
- [ ] Create worker → saves, redirects to list
- [ ] Edit worker → saves, redirects to list
- [ ] Delete worker → removes, redirects to list
- [ ] Worker Detail → shows account info
- [ ] Import Workers → page loads, download template works, import processes
- [ ] Export Workers → Excel downloads
- [ ] /CMP/ManageWorkers → returns 404 (no redirect)
- [ ] Navbar: "Kelola Pekerja" button absent for all users
- [ ] RecordsWorkerList → click worker name → /Admin/WorkerDetail loads (not 404)

## Deviations from Plan

None — plan executed exactly as written. RecordsWorkerList.cshtml URL was already updated in Plan 01 as documented in 69-01-SUMMARY.md.

## Issues Encountered

None — clean execution. Node.js used for file manipulation (Python not available in environment).

## Next Phase Readiness

- Phase 69 migration complete: ManageWorkers fully migrated from CMP to Admin
- Access path: Admin/Index hub → Manajemen Pekerja card → /Admin/ManageWorkers
- Old access path (/CMP/ManageWorkers) returns 404 — clean break confirmed
- No remaining CMP ManageWorkers references in codebase

## Self-Check: PASSED

Files verified:
- Controllers/CMPController.cs — FOUND (Worker Management region removed)
- Views/Admin/Index.cshtml — FOUND (Manajemen Pekerja card added)
- Views/Shared/_Layout.cshtml — FOUND (navbar button removed)
- Views/CMP/ManageWorkers.cshtml — CONFIRMED DELETED
- Views/CMP/CreateWorker.cshtml — CONFIRMED DELETED
- Views/CMP/EditWorker.cshtml — CONFIRMED DELETED
- Views/CMP/WorkerDetail.cshtml — CONFIRMED DELETED
- Views/CMP/ImportWorkers.cshtml — CONFIRMED DELETED

Commits:
- 9797c6f — FOUND

Build: 0 errors.

---
*Phase: 69-manageworkers-migration-to-admin*
*Completed: 2026-02-28*
