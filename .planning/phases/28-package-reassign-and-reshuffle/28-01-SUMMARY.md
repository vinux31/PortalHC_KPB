---
phase: 28-package-reassign-and-reshuffle
plan: "01"
subsystem: assessment-monitoring
tags: [reshuffle, package-assignment, cmp-controller, view-models, ajax, audit-log]
dependency_graph:
  requires: []
  provides:
    - ReshufflePackage POST action (CMPController)
    - ReshuffleAll POST action (CMPController)
    - IsPackageMode flag on MonitoringGroupViewModel
    - PendingCount on MonitoringGroupViewModel
    - PackageName on MonitoringSessionViewModel
    - AssignmentId on MonitoringSessionViewModel
  affects:
    - Views/CMP/AssessmentMonitoringDetail.cshtml (28-02 will use new fields)
tech_stack:
  added: []
  patterns:
    - Sibling session lookup (Title + Category + Schedule.Date) for package scope
    - Fisher-Yates shuffle reuse (existing Shuffle<T> helper)
    - Batch SaveChangesAsync for bulk operations (accumulate changes, save once)
    - Audit try/catch to avoid rolling back successful operations
key_files:
  created: []
  modified:
    - Models/AssessmentMonitoringViewModel.cs
    - Controllers/CMPController.cs
decisions:
  - PendingCount uses "Not started" string (not "Pending") to match existing 4-state UserStatus projection
  - ReshufflePackage selects different package only when 2+ packages exist AND current assignment exists; otherwise picks from all packages
  - ReshuffleAll accumulates all DB changes (removes + adds) and calls SaveChangesAsync once before audit log
  - Audit calls wrapped in try/catch to prevent audit failure from rolling back successful reshuffles
metrics:
  duration: 3min
  completed: 2026-02-21
  tasks: 2
  files: 2
---

# Phase 28 Plan 01: Package Reshuffle Backend Summary

**One-liner:** JWT-free reshuffle backend — two JSON POST actions (single + bulk) with Fisher-Yates re-shuffle, eligibility guard (Not started only), and audit logging; view models enriched with IsPackageMode/PendingCount/PackageName/AssignmentId.

## What Was Built

### Task 1: Extended View Models + AssessmentMonitoringDetail
- `MonitoringGroupViewModel` gains `IsPackageMode` (bool) and `PendingCount` (int)
- `MonitoringSessionViewModel` gains `PackageName` (string) and `AssignmentId` (int?)`
- `AssessmentMonitoringDetail` action now queries `AssessmentPackages.CountAsync` to detect package mode, then loads `UserPackageAssignments` joined to `AssessmentPackages` to build an assignment map keyed by session ID
- Populates `PackageName` and `AssignmentId` on every session view model; populates `IsPackageMode` and `PendingCount` on the group view model

### Task 2: ReshufflePackage + ReshuffleAll Controller Actions
**ReshufflePackage (single worker):**
- Accepts `sessionId`, loads session, derives 4-state UserStatus
- Guards: returns JSON error if session not found or status is not "Not started"
- Loads sibling sessions (Title + Category + Schedule.Date) and all packages
- Selects different package if 2+ packages and existing assignment; otherwise picks randomly from all
- Deletes old `UserPackageAssignment`, creates new one with Fisher-Yates shuffled question IDs and per-question option IDs
- Writes audit log with old → new package name, wrapped in try/catch
- Returns `{ success, packageName, assignmentId }`

**ReshuffleAll (bulk):**
- Accepts `title`, `category`, `scheduleDate` group identifiers
- Loads all sibling sessions with User includes
- Guards: returns JSON error if no sessions found or no packages found
- Single `Random` instance reused across all workers for true batch randomness
- Iterates all sessions: skips non-"Not started" workers with Indonesian reason labels
- Accumulates all DB removes + adds, calls `SaveChangesAsync()` once
- Writes single audit log entry with reshuffled count
- Returns `{ success, results, reshuffledCount }` where results is a per-worker list

## Verification Results

1. `dotnet build` passes — 0 errors, 35 pre-existing warnings
2. `MonitoringGroupViewModel` has `IsPackageMode` (bool) and `PendingCount` (int) — confirmed
3. `MonitoringSessionViewModel` has `PackageName` (string) and `AssignmentId` (int?) — confirmed
4. `AssessmentMonitoringDetail` queries packages and assignments, populates all new fields — confirmed
5. `ReshufflePackage` POST exists, returns JSON, guards against non-Not-started workers, picks different package when possible — confirmed
6. `ReshuffleAll` POST exists, returns JSON with per-worker results, only reshuffles Not started workers — confirmed
7. Both actions have `[Authorize(Roles = "Admin, HC")]` and `[ValidateAntiForgeryToken]` — confirmed (lines 574, 692)
8. Both actions write audit logs — confirmed

## Deviations from Plan

None — plan executed exactly as written.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | 777153a | feat(28-01): extend view models and populate package info in AssessmentMonitoringDetail |
| 2 | cd6cec4 | feat(28-01): add ReshufflePackage and ReshuffleAll POST controller actions |

## Self-Check: PASSED
