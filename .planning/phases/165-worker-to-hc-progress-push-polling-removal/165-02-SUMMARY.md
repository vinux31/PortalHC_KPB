---
phase: 165-worker-to-hc-progress-push-polling-removal
plan: 02
subsystem: realtime
tags: [polling-removal, cleanup, signalr, cmp-controller, admin-controller]

requires:
  - phase: 165-worker-to-hc-progress-push-polling-removal
    plan: 01
    provides: SignalR push events for workerProgress, workerStarted, workerSubmitted
---

## What was built
Removed all legacy polling code now that SignalR push events handle real-time updates:

- **StartExam.cshtml**: Removed `checkExamStatus` function, `CHECK_STATUS_URL`, `statusPollInterval`, and the 10s `setInterval` call. The `examClosed` SignalR handler already covers HC-initiated closures.
- **AssessmentMonitoringDetail.cshtml**: Removed `fetchProgress` function, `pollingActive`/`pollingTimer` variables, `poll-error` badge, and the 10s `setInterval` call. Terminal-state detection already ported to `workerSubmitted` handler.
- **CMPController.cs**: Deleted `CheckExamStatus` endpoint (45 lines). Also fixed `LogActivityAsync` to use `IServiceScopeFactory` for thread-safe fire-and-forget DB writes.
- **AdminController.cs**: Deleted `GetMonitoringProgress` endpoint (95 lines).

## Preserved (not polling)
- Countdown timer (`timerInterval`, `tickCountdowns`)
- Auto-save (`saveSessionProgress` every 30s)
- Navigation guards (`navPoll`, `rPoll`)
- Activity log refresh interval

## key-files
### modified
- Views/CMP/StartExam.cshtml
- Views/Admin/AssessmentMonitoringDetail.cshtml
- Controllers/CMPController.cs
- Controllers/AdminController.cs

## Commits
- `b0ede97` fix(165): use IServiceScopeFactory for fire-and-forget activity logging
- `fe0fd8b` feat(165-02): remove legacy polling code

## Self-Check: PASSED
- No `CheckExamStatus` or `GetMonitoringProgress` in controllers
- No polling `setInterval` calls remain (only countdown, auto-save, nav guards)
- Build succeeds with no code errors
