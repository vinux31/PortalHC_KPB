---
phase: 164-hc-to-worker-push-events
plan: "01"
subsystem: SignalR / AdminController
tags: [signalr, push-events, assessment, real-time]
requirements: [PUSH-01, PUSH-02, PUSH-03]

dependency_graph:
  requires: [163-01]
  provides: [HC-to-worker push for Reset, AkhiriUjian, AkhiriSemuaUjian]
  affects: [Controllers/AdminController.cs]

tech_stack:
  added: []
  patterns: [IHubContext<T> DI injection, Clients.User() for individual push, Clients.Group() for batch push]

key_files:
  modified:
    - Controllers/AdminController.cs

decisions:
  - IHubContext<AssessmentHub> injected as 9th constructor parameter — DI resolves automatically via AddSignalR() registered in Phase 163
  - SendAsync placed after audit log, before TempData/redirect, strictly after DB write confirmation
  - AkhiriSemuaUjian uses Clients.Group($"batch-{batchKey}") with format matching JoinBatch in assessment-hub.js

metrics:
  duration: "190s"
  completed_date: "2026-03-13"
  tasks_completed: 2
  files_modified: 1
---

# Phase 164 Plan 01: HC-to-Worker Push Events Summary

**One-liner:** IHubContext<AssessmentHub> injected into AdminController with 3 SendAsync push calls (sessionReset + examClosed x2) gated behind successful DB writes.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Inject IHubContext into AdminController | dd80dd6 | Controllers/AdminController.cs |
| 2 | Add SendAsync calls to Reset, AkhiriUjian, AkhiriSemuaUjian | 82e31b0 | Controllers/AdminController.cs |

## What Was Built

AdminController now has SignalR push capability. When HC takes action:

- **Reset** (PUSH-01): Sends `sessionReset` event to the specific worker via `Clients.User(assessment.UserId)` after `rsRowsAffected > 0`
- **AkhiriUjian** (PUSH-02): Sends `examClosed` event to the specific worker via `Clients.User(session.UserId)` after `rowsAffected > 0`
- **AkhiriSemuaUjian** (PUSH-03): Sends `examClosed` event to the batch group via `Clients.Group($"batch-{batchKey}")` after `SaveChangesAsync()` and cache invalidation

The batchKey format (`{title}|{category}|{date:yyyy-MM-dd}`) matches what JoinBatch uses in assessment-hub.js.

## Verification

- `IHubContext<AssessmentHub>` appears 2 times in AdminController (field + constructor param) — confirmed
- `SendAsync.*sessionReset` appears 1 time (Reset action) — confirmed
- `SendAsync.*examClosed` appears 2 times (AkhiriUjian + AkhiriSemuaUjian) — confirmed
- No `Dictionary.*connectionId` patterns — confirmed
- Build: 0 CS compilation errors (file-lock MSB error is from running dev server, not a code issue)

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check: PASSED

- Controllers/AdminController.cs: modified with IHubContext injection and 3 SendAsync calls
- Commit dd80dd6: feat(164-01): inject IHubContext<AssessmentHub> into AdminController
- Commit 82e31b0: feat(164-01): add SignalR push calls to Reset, AkhiriUjian, AkhiriSemuaUjian
