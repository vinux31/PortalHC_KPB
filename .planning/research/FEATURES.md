# Feature Research

**Domain:** Real-time exam/assessment monitoring (SignalR enhancement to existing CMP exam flow)
**Researched:** 2026-03-13
**Confidence:** HIGH (based on direct code analysis of existing system + standard SignalR patterns)

---

## Context: What Already Exists

The existing system is fully functional with polling:

- **Worker side (StartExam.cshtml):** 10-second `setInterval` polling `CheckExamStatus` endpoint. Detects `ForceClose` and `Reset` events. Shows modal, then redirects. Also has 30-second auto-save for answers.
- **HC side (AssessmentMonitoringDetail.cshtml):** 10-second `setInterval` polling `GetMonitoringProgress` endpoint. Updates session rows (status, time remaining, answered count, actions). Stops polling when all sessions are Completed.
- **HC actions (form POST):** ForceCloseAssessment, ResetAssessment, ForceCloseAll, CloseEarly — all page POSTs that redirect back.

The gap: HC actions (Reset, ForceClose) take up to 10 seconds to reach the worker. Worker progress takes up to 10 seconds to reach the HC monitor.

---

## Feature Landscape

### Table Stakes (Users Expect These)

These are what "real-time" means to users in an exam monitoring context. Missing them makes the SignalR upgrade feel incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| HC Reset pushes instantly to worker | Reset is time-sensitive — worker needs to know immediately to restart | MEDIUM | Replace worker's 10s poll. Show "Sesi direset" modal, redirect to StartExam |
| HC ForceClose pushes instantly to worker | ForceClose terminates the exam — delay means worker keeps answering after score is locked at 0 | MEDIUM | Replace worker's 10s poll. Show existing forceCloseModal immediately |
| Worker submission updates HC monitor without poll | HC should see "Completed" the moment worker submits, not up to 10s later | MEDIUM | Replace HC's 10s poll for status changes |
| Worker answer progress updates HC in near-real-time | HC monitoring shows answered count and time remaining — polling gap is acceptable but push is expected | LOW | Answered count updates; can still batch (e.g., on answer save, not every keystroke) |
| Connection status indicator on both pages | Users need to know if SignalR is connected or fell back to polling | LOW | Simple dot/badge: "Live" vs "Reconnecting..." |

### Differentiators (Valuable but Not Required at Launch)

Features that improve the experience beyond the baseline replacement of polling.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| HC ForceCloseAll pushes to ALL workers simultaneously | Currently ForceCloseAll POSTs then each worker waits up to 10s; bulk push avoids stragglers continuing exam | MEDIUM | Hub broadcasts to all workers in the assessment group |
| HC CloseEarly triggers instant notification to active workers | CloseEarly sets ExamWindowCloseDate; workers currently discover this only via poll | MEDIUM | Push "exam window closed" modal to all InProgress workers |
| Worker joins SignalR group scoped to their assessmentId | Enables targeted broadcasts per assessment without broadcasting globally | LOW | Prerequisite for all targeted push features; implement at Hub level |
| Graceful fallback to polling if SignalR disconnects | Network hiccups should not break exam for the worker | MEDIUM | Keep existing polling as fallback; SignalR disables it, reconnect re-enables |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Push every answer save to HC in real-time | "See what the worker is answering right now" | High message volume, privacy concern (seeing answers mid-exam), server load with many concurrent exams | Push answered count only (not which answer), already done on each save event |
| Two-way chat between HC and worker during exam | Seem useful for "I have a question" scenario | Scope creep; exam integrity risk; adds moderation complexity | Post-exam feedback via existing CDP/coaching features |
| Live screen sharing or proctoring | Security/integrity concern | Massive scope expansion requiring external service | Out of scope; token-based access control already handles basic integrity |
| Push notifications to browser (OS-level) | Notify HC even if not on the monitor page | Requires Notification API permission UX, service worker registration | In-page indicators are sufficient; HC is expected to be on the monitor page |
| SignalR replacing the 30s answer auto-save | "Answers save faster" | Auto-save via SignalR adds server-side complexity and loses the existing debounce/conflict resolution logic | Keep existing XHR-based auto-save; SignalR is for status events only |

---

## Feature Dependencies

```
[SignalR Hub (server)] + [Client library loaded on pages]
    └──enables──> [Worker receives Reset push]
    └──enables──> [Worker receives ForceClose push]
    └──enables──> [Worker receives CloseEarly push]
    └──enables──> [HC receives Worker submission event]
    └──enables──> [HC receives Worker progress update]

[Assessment group scoping (joinGroup on connect)]
    └──required by──> [Targeted broadcasts per assessment]
                           └──required by──> [ForceCloseAll bulk push]
                           └──required by──> [CloseEarly push]

[Graceful fallback]
    └──depends on──> [SignalR connected state tracking on client]
```

### Dependency Notes

- **Hub registration requires Program.cs change:** `app.MapHub<ExamHub>("/hubs/exam")` must be in place before any client can connect.
- **Worker group join requires assessmentId:** Worker connects and calls `JoinAssessmentGroup(assessmentId)` on connect. HC monitor joins same group as observer.
- **ForceClose push targets individual session, not whole group:** Use per-session sub-group `"session-{sessionId}"` pattern. Worker joins this group on connect. Avoids server-side connectionId tracking.
- **HC actions remain as form POSTs:** No refactor of AdminController action methods needed beyond adding `IHubContext<ExamHub>.Clients.Group(...).SendAsync(...)` call after the DB update.
- **Do not remove existing polling entirely:** If worker disconnects, push is missed. Keep 10s poll as safety net (or increase interval to 30s once SignalR is active).

---

## MVP Definition

### Launch With (v4.2 — matches PROJECT.md goal)

- [ ] SignalR Hub registered in Program.cs, `/hubs/exam` endpoint
- [ ] Worker page connects to hub, joins session group, listens for `examReset` and `examForceClosed` events — replaces 10s poll for those two events
- [ ] HC monitor page connects to hub, joins assessment group, listens for `progressUpdate` event — replaces 10s poll for status/progress updates
- [ ] Server: AdminController Reset and ForceClose actions broadcast to session group after DB update
- [ ] Server: CMPController SubmitExam broadcasts `progressUpdate` to assessment group after scoring
- [ ] Server: CMPController SaveAnswer broadcasts `progressUpdate` (answered count only) to assessment group
- [ ] Connection status indicator (simple "Live" / "Reconnecting" badge)

### Add After Validation (v1.x / follow-on)

- [ ] ForceCloseAll broadcasts to all workers in assessment group simultaneously
- [ ] CloseEarly push to active workers (same group broadcast pattern)
- [ ] Reduce poll fallback interval from 10s to 30s once push is confirmed working

### Future Consideration (v2+)

- [ ] Explicit graceful fallback toggle (SignalR off -> re-enable polling) for unreliable network environments
- [ ] Multi-assessment concurrent monitoring across tabs

---

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Worker receives Reset push instantly | HIGH | MEDIUM | P1 |
| Worker receives ForceClose push instantly | HIGH | MEDIUM | P1 |
| HC monitor receives submission event | HIGH | MEDIUM | P1 |
| HC monitor receives progress update (answered count) | MEDIUM | LOW | P1 |
| Connection status indicator | MEDIUM | LOW | P1 |
| ForceCloseAll bulk push | MEDIUM | LOW (group broadcast) | P2 |
| CloseEarly push to workers | MEDIUM | LOW (same pattern) | P2 |
| Graceful fallback on disconnect | LOW (poll already exists as safety net) | MEDIUM | P3 |

---

## Event Catalogue (Push Events Needed)

This maps each HC action and worker event to its SignalR broadcast:

| Trigger | Direction | Event Name | Payload | Receiver Group |
|---------|-----------|------------|---------|----------------|
| HC clicks Reset (single) | HC -> Worker | `examReset` | `{ sessionId }` | `"session-{sessionId}"` |
| HC clicks ForceClose (single) | HC -> Worker | `examForceClosed` | `{ sessionId, redirectUrl }` | `"session-{sessionId}"` |
| HC clicks ForceCloseAll | HC -> All workers | `examForceClosed` | `{ sessionId, redirectUrl }` | `"assessment-{assessmentId}"` (all workers) |
| HC clicks CloseEarly | HC -> All workers | `examWindowClosed` | `{ assessmentId }` | `"assessment-{assessmentId}"` |
| Worker submits exam | Worker -> HC | `progressUpdate` | `{ sessionId, status, answeredCount, score }` | `"assessment-{assessmentId}"` |
| Worker saves answer (debounced) | Worker -> HC | `progressUpdate` | `{ sessionId, answeredCount }` | `"assessment-{assessmentId}"` |

**Group naming convention:**
- `"assessment-{assessmentId}"` — HC monitor page joins this; all workers in the assessment join this
- `"session-{sessionId}"` — Worker joins this on connect; HC actions targeting one worker broadcast here

---

## Sources

- Direct code analysis: `Views/CMP/StartExam.cshtml` lines 706-735 — existing worker 10s polling for CheckExamStatus
- Direct code analysis: `Views/Admin/AssessmentMonitoringDetail.cshtml` lines 792-829 — existing HC 10s polling for GetMonitoringProgress
- Direct code analysis: `Views/Admin/AssessmentMonitoringDetail.cshtml` lines 662-678 — ForceClose and Reset action forms
- Direct code analysis: `Controllers/CMPController.cs` — StartExam, SubmitExam, CheckExamStatus, SaveAnswer
- `.planning/PROJECT.md` — milestone v4.2 goal definition
- Standard ASP.NET Core SignalR Hub patterns (HIGH confidence — stable since .NET Core 2.1)

---
*Feature research for: Real-time assessment SignalR (PortalHC KPB v4.2)*
*Researched: 2026-03-13*
