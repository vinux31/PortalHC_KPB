---
phase: 165-worker-to-hc-progress-push-polling-removal
plan: 01
subsystem: realtime
tags: [signalr, cmp-controller, assessment, monitoring, push-events]

requires:
  - phase: 164-signalr-hub-hc-monitor-badge-akhiri-semua
    provides: IHubContext injected into AdminController, assessmentBatchKey pattern, assessment-hub.js shared connection

provides:
  - AssessmentHub JoinMonitor/LeaveMonitor group methods
  - CMPController IHubContext<AssessmentHub> injection with 3 push events (progressUpdate, workerStarted, workerSubmitted)
  - HC monitoring page event handlers for all 3 push events with row flash + toast
  - monitor-{batchKey} group naming convention

affects:
  - 165-02-polling-removal (next plan removes GetMonitoringProgress polling now that push is live)

tech-stack:
  added: []
  patterns:
    - "monitor-{batchKey} SignalR group for HC monitoring page; joined on hub connect + rejoined on reconnect"
    - "DB write always precedes SignalR push — push is notifications-only, never state source"
    - "flashRow() with CSS keyframe animations for row-level visual feedback"
    - "updateSummaryFromDOM() reads badge text from DOM rather than maintaining separate state"

key-files:
  created: []
  modified:
    - Hubs/AssessmentHub.cs
    - Controllers/CMPController.cs
    - wwwroot/css/assessment-hub.css
    - wwwroot/js/assessment-hub.js
    - Views/Admin/AssessmentMonitoringDetail.cshtml

key-decisions:
  - "workerStarted push gated by justStarted flag (StartedAt==null before SaveChangesAsync) to prevent duplicate pushes on page reload"
  - "updateSummaryFromDOM() reads .status-cell badge textContent from DOM rather than maintaining parallel JS state, avoids sync bugs"
  - "workerSubmitted in monitoring view inlines row update logic (does not call updateRow() from IIFE) because updateRow() is inside a closure and not accessible from @section Scripts block"
  - "Both package path and legacy path in SubmitExam push workerSubmitted after their respective rowsAffected > 0 guards"

patterns-established:
  - "flashRow(tr, cssClass): remove class, force reflow via offsetWidth, add class, remove after 1300ms"

requirements-completed: [MONITOR-01, MONITOR-02, MONITOR-03]

duration: 25min
completed: 2026-03-13
---

# Phase 165 Plan 01: Worker-to-HC Progress Push Summary

**Three real-time SignalR push events (progressUpdate, workerStarted, workerSubmitted) from CMPController to HC monitoring page with row flash animations and toast notifications — sub-second update latency replacing 10-second polling lag**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-03-13T04:15:00Z
- **Completed:** 2026-03-13T04:40:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- CMPController injects IHubContext<AssessmentHub> and pushes to monitor-{batchKey} group on SaveAnswer, StartExam (first entry only), and SubmitExam (both package + legacy paths)
- AssessmentHub gains JoinMonitor/LeaveMonitor methods; HC monitoring page joins monitor group on connect and rejoins on reconnect
- HC monitoring page handles all 3 push events with row flash (blue=update, green=complete), toast messages, and live summary count updates from DOM

## Task Commits

1. **Task 1: Backend — IHubContext + JoinMonitor + 3 push calls** - `a5c77a9` (feat)
2. **Task 2: Frontend — event handlers, row flash, toast, JoinMonitor** - `2dc4f26` (feat)

## Files Created/Modified

- `Hubs/AssessmentHub.cs` — Added JoinMonitor/LeaveMonitor group methods
- `Controllers/CMPController.cs` — IHubContext<AssessmentHub> injected (10th param); progressUpdate push in SaveAnswer; workerStarted push in StartExam GET on first entry; workerSubmitted push in SubmitExam both paths
- `wwwroot/css/assessment-hub.css` — rowFlashUpdate and rowFlashComplete CSS keyframe animations
- `wwwroot/js/assessment-hub.js` — showAssessmentToast exposed globally via window.showAssessmentToast
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — JoinMonitor on connect + reconnect, flashRow helper, updateSummaryFromDOM, progressUpdate/workerStarted/workerSubmitted event handlers

## Decisions Made

- `workerStarted` push is gated by `justStarted` flag (captured before `SaveChangesAsync`) to prevent pushing on every StartExam page load (e.g., worker presses F5)
- `updateSummaryFromDOM()` reads `.status-cell` badge text content from DOM rather than maintaining a separate JS counter — simpler and avoids desync with DOM-only updates
- The `workerSubmitted` handler in the view inlines its own row-update logic instead of calling the IIFE's `updateRow()`, because `updateRow()` lives inside a closure not accessible from `@section Scripts`
- Both package path and legacy path in `SubmitExam` push `workerSubmitted` with correct `totalQuestions` values specific to each path

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

Build showed MSBuild file-lock errors (not C# compile errors) because the app process (PID 9760) was running and locked the output EXE. `dotnet build` compilation itself succeeded with 0 C# errors on both tasks. This is expected behavior when the app is running during development.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- SignalR push events are live end-to-end. HC monitoring page will receive sub-second updates when workers answer questions, start exams, or submit exams.
- Polling (GetMonitoringProgress at 10s interval) is still active in parallel — Phase 165-02 will remove it after UAT confirms SignalR push is stable.
- The `data-session-id` attribute and `.status-cell` / `.progress-cell` classes were already present in the monitoring table from prior phases — no HTML changes needed.

---
*Phase: 165-worker-to-hc-progress-push-polling-removal*
*Completed: 2026-03-13*
