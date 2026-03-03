---
phase: 92-admin-cpdp-file-management
plan: 01
subsystem: AdminController / CPDP File Management
tags: [cpdp, file-management, admin, controller]
dependency_graph:
  requires: [91-02 (CpdpFile model + EF migration)]
  provides: [CpdpFiles action, CpdpUpload GET/POST, CpdpFileDownload, CpdpFileArchive]
  affects: [Controllers/AdminController.cs]
tech_stack:
  added: []
  patterns: [soft-delete, file-upload, IFormFile.CopyToAsync, Path.Combine storage]
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
decisions:
  - "CpdpFileArchive uses soft-delete (IsArchived=true) rather than physical file deletion"
  - "Storage path /uploads/cpdp/{bagianId}/ mirrors KKJ's /uploads/kkj/ structure"
  - "CpdpFileDownload uses [Authorize] (all authenticated users); upload/archive use [Authorize(Roles = Admin, HC)]"
  - "No bagian seeding in CpdpFiles — bagians shared with KKJ, already seeded by KkjMatrix action"
metrics:
  duration: 3 min
  completed_date: "2026-03-03T02:53:53Z"
  tasks_completed: 1
  files_modified: 1
---

# Phase 92 Plan 01: CPDP File Management Controller Actions Summary

**One-liner:** Five AdminController actions for CPDP file management mirroring the KKJ pattern with cpdp storage path and soft-delete archive.

## Tasks Completed

| Task | Name | Commit | Files Modified |
|------|------|--------|----------------|
| 1 | Add CPDP File Management controller actions | 76f80d5 | Controllers/AdminController.cs |

## What Was Built

Added a new `#region CPDP File Management` block in `AdminController.cs` after the KKJ Bagian actions (line 290+), containing five action methods:

1. **CpdpFiles GET** — Returns the file listing view with `ViewBag.Bagians`, `ViewBag.FilesByBagian`, and `ViewBag.SelectedBagianId`. Loads only non-archived files ordered by `UploadedAt DESC`.

2. **CpdpUpload GET** — Returns the upload form view with bagian selector.

3. **CpdpUpload POST** — Validates file (PDF/Excel only, max 10MB), saves to `wwwroot/uploads/cpdp/{bagianId}/`, creates `CpdpFile` DB record with uploader name from `_userManager`, redirects to `CpdpFiles` on success.

4. **CpdpFileDownload GET** — Serves file bytes with correct `Content-Type` (`application/pdf` or `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`). Accessible to all authenticated users.

5. **CpdpFileArchive POST** — Soft-deletes by setting `IsArchived = true`. Returns `{success: true/false, message: "..."}` JSON.

## Verification

- `dotnet build` reports **0 errors**, 54 pre-existing warnings (LDAP CA1416 warnings unrelated to this change)
- All 5 action signatures confirmed present via grep

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check

- [x] `Controllers/AdminController.cs` modified — FOUND
- [x] Commit 76f80d5 exists — FOUND
- [x] `dotnet build` 0 errors — PASSED
- [x] All 5 action names present in file — PASSED

## Self-Check: PASSED
