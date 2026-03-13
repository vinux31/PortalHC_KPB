---
phase: 166-activity-log-per-worker
plan: "02"
subsystem: assessment-activity-log
tags: [signalr, monitoring, frontend, ajax]
dependency_graph:
  requires: [166-01]
  provides: [activity-log-modal, page-nav-logging]
  affects: [Views/Admin/AssessmentMonitoringDetail, Views/CMP/StartExam]
tech_stack:
  added: []
  patterns: [fetch-ajax, bootstrap-modal, signalr-invoke]
key_files:
  created: []
  modified:
    - Views/CMP/StartExam.cshtml
    - Views/Admin/AssessmentMonitoringDetail.cshtml
    - Hubs/AssessmentHub.cs
    - Migrations/20260313045808_AddExamActivityLog.cs
decisions:
  - "Log button shown for all worker statuses (not conditional) — simpler, always available for HC audit"
  - "buildActionsHtml gets worker name from DOM col 0 (not from API payload) — avoids adding workerName to GetMonitoringProgress DTO"
  - "showActivityLog defined outside IIFE to be accessible from inline onclick attributes"
  - "pageNumber sent as 0-based from JS, stored as 1-based via Hub (pageNumber + 1)"
metrics:
  duration: "25 minutes"
  completed_date: "2026-03-13"
  tasks_completed: 1
  files_modified: 4
---

# Phase 166 Plan 02: Activity Log Frontend Summary

**One-liner:** Activity log modal with AJAX fetch + per-row log button on monitoring detail, SignalR LogPageNav on worker page change.

## What Was Built

### Task 1: Worker exam page nav logging + monitoring detail modal

**StartExam.cshtml — page navigation logging:**
- Added `hub.invoke('LogPageNav', SESSION_ID, currentPage)` call inside `performPageSwitch()`
- Wrapped in try-catch with hub state check — never blocks navigation if SignalR unavailable
- Sends 0-based `currentPage` to hub; hub stores as 1-based "Halaman N"

**AssessmentMonitoringDetail.cshtml — log button:**
- Added `<button onclick="showActivityLog(...)">` with `bi-clock-history` icon to each static worker row
- Also added to `buildActionsHtml()` for dynamically-rendered rows (after polling updates)
- Worker name extracted from DOM col 0 in `buildActionsHtml`

**AssessmentMonitoringDetail.cshtml — activity log modal:**
- Bootstrap modal `#activityLogModal` with loading spinner, summary div, timeline div
- `showActivityLog(sessionId, workerName)` function fetches `/Admin/GetActivityLog?sessionId=`
- Summary renders: "Dijawab: X | Waktu: Y menit | Terputus: Zx"
- Timeline renders as Bootstrap `table-sm` with timestamp (HH:mm:ss) + Bahasa Indonesia label
- `eventLabel()` maps event types to icons + Indonesian text

**Hubs/AssessmentHub.cs — logging overrides:**
- Constructor now takes `IServiceScopeFactory` (DI)
- `LogPageNav(int sessionId, int pageNumber)` — fire-and-forget with new scope
- `OnConnectedAsync` — logs "reconnected" for any InProgress session of the connecting user
- `OnDisconnectedAsync` — logs "disconnected" same pattern
- All DB writes use `Task.Run` + new scope to avoid DbContext threading issues

**Migration:**
- `AddExamActivityLog` migration created; DB already had the table applied

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Backend from 166-01 not committed but already existed in codebase**
- **Found during:** Pre-execution check (git log showed 166-01 never ran)
- **Issue:** Plan 166-01 was never committed, but code was already present: `ExamActivityLog.cs`, `DbSet<ExamActivityLog>` in DbContext, `LogActivityAsync` in CMPController, `GetActivityLog` in AdminController, and Hub lifecycle overrides in `AssessmentHub.cs`
- **Fix:** Verified existing implementations, added missing migration, fixed `pageNumber + 1` in Hub, removed duplicate methods I initially tried to add
- **Files verified:** Models/ExamActivityLog.cs, Data/ApplicationDbContext.cs, Controllers/CMPController.cs, Controllers/AdminController.cs, Hubs/AssessmentHub.cs

## Self-Check

### Files Exist
- Views/CMP/StartExam.cshtml — FOUND (modified)
- Views/Admin/AssessmentMonitoringDetail.cshtml — FOUND (modified)
- Hubs/AssessmentHub.cs — FOUND (modified)
- Migrations/20260313045808_AddExamActivityLog.cs — FOUND

### Commits
- d4cd1e4 — feat(166-02): activity log frontend — FOUND

## Self-Check: PASSED

## Awaiting Human Verification

Task 2 is a checkpoint:human-verify. The HC must verify end-to-end:
1. Worker takes exam, navigates pages, submits
2. HC opens monitoring detail, clicks log button
3. Modal shows summary + timeline with Bahasa Indonesia labels
