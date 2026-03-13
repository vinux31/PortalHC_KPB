---
phase: 163-hub-infrastructure-safety-foundations
plan: 01
subsystem: signalr-infrastructure
tags: [signalr, hub, websocket, wal, assessment, real-time]
dependency_graph:
  requires: []
  provides: [signalr-hub-endpoint, assessment-hub-js, vendored-signalr-js, wal-mode]
  affects: [Views/CMP/StartExam.cshtml, Views/Admin/AssessmentMonitoringDetail.cshtml]
tech_stack:
  added: [Microsoft.AspNetCore.SignalR (built-in ASP.NET Core 8)]
  patterns: [Hub group management (string batchKey), IIFE JS module, WAL pragma on startup]
key_files:
  created:
    - Hubs/AssessmentHub.cs
    - wwwroot/lib/signalr/signalr.min.js
    - wwwroot/js/assessment-hub.js
    - wwwroot/css/assessment-hub.css
  modified:
    - Program.cs
    - Controllers/CMPController.cs
    - Controllers/AdminController.cs
    - Views/CMP/StartExam.cshtml
    - Views/Admin/AssessmentMonitoringDetail.cshtml
decisions:
  - SignalR hub methods accept string batchKey (composite "Title|Category|yyyy-MM-dd") — no single DB batch ID column
  - 401 returned for /hubs/ paths instead of 302 redirect to prevent SignalR negotiate from following redirect
  - WAL pragma guarded by provider name check so SQL Server deployments are unaffected
  - assessment-hub.js uses IIFE pattern exposing window.assessmentHub for Phase 164/165 event handlers
metrics:
  duration_minutes: 4
  completed_date: "2026-03-13"
  tasks_completed: 2
  tasks_total: 3
  files_created: 4
  files_modified: 5
---

# Phase 163 Plan 01: SignalR Hub Infrastructure and Safety Foundations Summary

**One-liner:** SignalR transport layer with AssessmentHub (group join/leave), WAL mode, 401 for negotiate, and reconnect/toast JS module wired into StartExam and AssessmentMonitoringDetail.

## What Was Built

- `Hubs/AssessmentHub.cs`: [Authorize]-decorated Hub with `JoinBatch(string)` and `LeaveBatch(string)` group management. No DB writes inside hub methods.
- `Program.cs`: AddSignalR, MapHub at `/hubs/assessment`, OnRedirectToLogin returning 401 for `/hubs/` paths, SQLite WAL mode activation with provider guard and startup log.
- `wwwroot/lib/signalr/signalr.min.js`: Vendored SignalR 8.0.0 JS client from jsdelivr.
- `wwwroot/js/assessment-hub.js`: IIFE module with `withAutomaticReconnect([0,2000,5000,10000,30000])`, showToast, showPersistentToast, onreconnecting/onreconnected/onclose handlers, JoinBatch on connect/reconnect.
- `wwwroot/css/assessment-hub.css`: Toast styles (fixed top-right, fade-in/out, persistent variant, error variant). No Bootstrap dependency.
- Both controllers set `ViewBag.AssessmentBatchKey = $"{Title}|{Category}|{Schedule.Date:yyyy-MM-dd}"`.
- Both views include signalr.min.js + assessment-hub.js + window.assessmentBatchKey in @section Scripts.

## Checkpoint Reached

Task 3 is `checkpoint:human-verify`. Automation complete; awaiting browser verification.

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Hubs/AssessmentHub.cs: EXISTS
- wwwroot/lib/signalr/signalr.min.js: EXISTS
- wwwroot/js/assessment-hub.js: EXISTS
- wwwroot/css/assessment-hub.css: EXISTS
- Commits: fd89149 (Task 1), 6980a22 (Task 2)
- Build: No C# compile errors (only MSB3027 file-lock from running process)
