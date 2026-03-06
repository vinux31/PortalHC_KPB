---
phase: 83-master-data-qa
plan: "06"
subsystem: worker-soft-delete
tags: [soft-delete, authentication, admin, account]
dependency_graph:
  requires: [83-05]
  provides: [worker-deactivate-backend, worker-reactivate-backend, login-block-inactive]
  affects: [ManageWorkers, Login, CoachCoacheeMappings, AssessmentSessions]
tech_stack:
  added: []
  patterns: [soft-delete, cascade-close, audit-log]
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Controllers/AccountController.cs
decisions:
  - "[83-06] DeactivateWorker uses null targetId (int?) in AuditLog.LogAsync to match existing DeleteWorker overload; userId included in description string"
  - "[83-06] showInactive=false default means ManageWorkers shows only active users by default — backward compatible"
  - "[83-06] IsActive login block placed as Step 2b, after null check and before AD sync, ensuring deactivated users are blocked regardless of auth mode"
metrics:
  duration: "7 min"
  completed_date: "2026-03-03T07:47:51Z"
  tasks: 2
  files: 2
---

# Phase 83 Plan 06: Worker Soft Delete Backend Summary

**One-liner:** Worker soft delete backend with cascade coaching/assessment close, IsActive login block, and showInactive filter for ManageWorkers.

## Tasks Completed

| # | Task | Commit | Status |
|---|------|--------|--------|
| 1 | Add DeactivateWorker and ReactivateWorker POST actions to AdminController | bffcff2 | Done |
| 2 | Add login block for inactive users in AccountController | df5da27 | Done |

## What Was Built

### Task 1 — AdminController soft delete backend

**ManageWorkers GET** updated to accept `showInactive` parameter (default `false`). When `false`, query filters to `u.IsActive == true`. `ViewBag.ShowInactive` set for view-layer toggle.

**DeactivateWorker POST** (`/Admin/DeactivateWorker`):
- Guards against self-deactivation and already-inactive users
- Counts active coaching mappings (`CoachCoacheeMappings` where `CoachId` or `CoacheeId` matches and `IsActive=true`) and active assessment sessions (`Open`, `Upcoming`, `InProgress`)
- Sets all active coaching mappings to `IsActive=false` and `EndDate=DateTime.Today`
- Sets all active assessment sessions to `Status="Closed"`
- Sets `user.IsActive = false` then `SaveChangesAsync`
- Audit logs with `null` targetId and description string containing userId (matches DeleteWorker overload)
- TempData["Success"] includes cascade counts; redirects to `ManageWorkers?showInactive=true`

**ReactivateWorker POST** (`/Admin/ReactivateWorker`):
- Sets `user.IsActive = true` then `SaveChangesAsync`
- Audit logs action
- Redirects to `ManageWorkers?showInactive=true`

### Task 2 — AccountController login block

Inserted between Step 2 (find user) and Step 3 (AD sync):
```csharp
if (!user.IsActive)
{
    ViewBag.Error = "Akun Anda tidak aktif. Hubungi HC untuk mengaktifkan kembali akun Anda.";
    return View();
}
```
Block fires before any `SignInAsync` call, preventing deactivated users from creating sessions in both local and AD auth modes.

## Deviations from Plan

### Auto-fixed Issues

None — plan executed exactly as written.

**Note on AuditLog targetId:** The plan noted that if `targetId` is `int?`, the user ID should go in the description string. Confirmed from `DeleteWorker` pattern: `LogAsync(actorId, actorName, action, description, null, "ApplicationUser")` — implemented identically.

## Self-Check: PASSED

- FOUND: Controllers/AdminController.cs
- FOUND: Controllers/AccountController.cs
- FOUND commit bffcff2 (Task 1 — AdminController)
- FOUND commit df5da27 (Task 2 — AccountController)
