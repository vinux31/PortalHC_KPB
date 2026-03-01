---
phase: 77-training-record-redirect-fix
plan: 01
subsystem: Controllers
tags: [training, assessment, roles, redirects, refactor]
requirements: [REDIR-01]

dependency_graph:
  requires: []
  provides:
    - AdminController.ManageAssessment with tab routing (assessment/training/history)
    - AdminController training CRUD (AddTraining, EditTraining, DeleteTraining)
    - AdminController private helpers (GetWorkersInSection, GetAllWorkersHistory, GetUnifiedRecords)
  affects:
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs

tech_stack:
  added: []
  patterns:
    - Tab routing via query param (tab=training|history|assessment) mapped to ViewBag.ActiveTab
    - Lazy-load worker data only when training/history tab is active
    - Audit log calls on all training CRUD mutations

key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Controllers/CMPController.cs

key_decisions:
  - Added IWebHostEnvironment _env to AdminController constructor — required for file upload/delete in training CRUD actions
  - Duplicated GetWorkersInSection, GetAllWorkersHistory, GetUnifiedRecords from CMPController into AdminController — plan spec; view layer in Plan 02 will render them from ViewBag on ManageAssessment
  - CMPController.Records simplified to always return personal Records view — removes elevated-role branch (HC/Admin/Management/Supervisor route to RecordsWorkerList) which will be cleaned up in Plan 02

metrics:
  duration_minutes: 5
  completed_date: "2026-03-01"
  tasks_completed: 2
  files_modified: 2
---

# Phase 77 Plan 01: Training Record Redirect Fix — Controller Refactor Summary

**One-liner:** AdminController gains ManageAssessment tab routing + training CRUD actions; CMPController.Records becomes personal-only with fixed redirects away from the deleted WorkerDetail action.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Refactor AdminController.ManageAssessment + helpers + training CRUD | bcbd3d1 | Controllers/AdminController.cs |
| 2 | Simplify CMPController.Records + fix EditTrainingRecord/DeleteTrainingRecord redirects | 8099af5 | Controllers/CMPController.cs |

## What Was Built

### Task 1 — AdminController.cs

**ManageAssessment refactored:**
- Role widened from `Admin` to `Admin, HC`
- New parameters: `tab`, `section`, `unit`, `category`, `statusFilter`, `isFiltered`
- Tab routing logic: `tab` param maps to `ViewBag.ActiveTab` ("assessment" default, "training", "history")
- Lazy worker-data fetch: `GetWorkersInSection` + `GetAllWorkersHistory` only called when tab=training or tab=history

**Role widening (Admin → Admin, HC):**
- CreateAssessment (GET + POST)
- EditAssessment (GET + POST)
- DeleteAssessmentGroup (POST)
- RegenerateToken (POST)
- ExportAssessmentResults (GET)
- AssessmentMonitoringDetail (GET)
- UserAssessmentHistory (GET)
- AuditLog (GET)

**New Training CRUD actions:**
- `AddTraining` (GET + POST) — worker dropdown, file upload, audit log, redirect to `ManageAssessment?tab=training`
- `EditTraining` (GET + POST) — loads record with Include(User), file replace logic, audit log, redirect to `ManageAssessment?tab=training`
- `DeleteTraining` (POST) — deletes certificate file from disk, audit log, redirect to `ManageAssessment?tab=training`

**Private helper methods added:**
- `GetUnifiedRecords(string userId)` — merges AssessmentSessions + TrainingRecords for personal view
- `GetAllWorkersHistory()` — returns (assessment, training) AllWorkersHistoryRow tuples for history tab
- `GetWorkersInSection(...)` — filtered WorkerTrainingStatus list for training tab

**Constructor change:** Added `IWebHostEnvironment _env` injection (required for file path operations in training CRUD).

### Task 2 — CMPController.cs

**Records action simplified:**
- Removed elevated-role branching (HC/Admin/Management/Supervisor → RecordsWorkerList)
- Now always calls `GetUnifiedRecords(user.Id)` and returns `View("Records", unified)`
- All ViewBag assignments for filter state removed (no longer needed)

**EditTrainingRecord redirect fixes (3 occurrences):**
- File type validation error: `WorkerDetail` → `ManageAssessment?tab=training`
- File size validation error: `WorkerDetail` → `ManageAssessment?tab=training`
- ModelState invalid: `WorkerDetail` → `ManageAssessment?tab=training`
- Success redirect: `WorkerDetail` → `ManageAssessment?tab=training`

**DeleteTrainingRecord redirect fix:**
- Success redirect: `WorkerDetail` → `ManageAssessment?tab=training`

## Verification Results

1. `dotnet build` — 0 errors, 60 warnings (all pre-existing nullable/CA1416 warnings)
2. AdminController contains: AddTraining (GET+POST), EditTraining (GET+POST), DeleteTraining (POST) — CONFIRMED
3. ManageAssessment signature includes: `string? tab = null, string? section = null, string? unit = null` — CONFIRMED
4. ManageAssessment has `[Authorize(Roles = "Admin, HC")]` — CONFIRMED
5. CMPController.Records returns `View("Records", unified)` unconditionally — CONFIRMED
6. CMPController.EditTrainingRecord: all redirects point to `Admin/ManageAssessment` with `tab = "training"` — CONFIRMED
7. CMPController.DeleteTrainingRecord: success redirect points to `Admin/ManageAssessment` with `tab = "training"` — CONFIRMED

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical Functionality] Added IWebHostEnvironment injection to AdminController**
- **Found during:** Task 1, implementing DeleteTraining/EditTraining file operations
- **Issue:** AdminController had no `_env` field; training CRUD actions need `_env.WebRootPath` for certificate file upload/delete
- **Fix:** Added `IWebHostEnvironment _env` field and constructor parameter
- **Files modified:** Controllers/AdminController.cs
- **Commit:** bcbd3d1

## Self-Check: PASSED

- Controllers/AdminController.cs: FOUND
- Controllers/CMPController.cs: FOUND
- .planning/phases/77-training-record-redirect-fix/77-01-SUMMARY.md: FOUND
- Commit bcbd3d1 (Task 1): FOUND
- Commit 8099af5 (Task 2): FOUND
