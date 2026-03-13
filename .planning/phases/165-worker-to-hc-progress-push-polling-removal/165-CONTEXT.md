# Phase 165: Worker-to-HC Progress Push + Polling Removal - Context

**Gathered:** 2026-03-13
**Status:** Ready for planning

<domain>
## Phase Boundary

HC monitoring page receives real-time worker exam progress, start events, and submission events via SignalR push. After UAT confirms stability, legacy polling (CheckExamStatus, GetMonitoringProgress) is removed entirely. No new UI pages — only updating existing AssessmentMonitoringDetail and StartExam views.

</domain>

<decisions>
## Implementation Decisions

### Progress Push Granularity
- Push on every answer instantly — each SaveAnswer call pushes updated count to HC monitoring group
- Payload: answeredCount + totalQuestions (HC page calculates percentage and updates progress bar)
- Push call lives in CMP Controller's SaveAnswer action, after DB write (same pattern as Phase 164)
- Push targets both HC monitoring group AND the worker (worker gets server-confirmed progress)

### HC Monitoring UI Update
- Row brief highlight flash (~1 second, subtle color like light blue) when data changes, then fades back
- HC monitoring page auto-joins "monitor-{batchKey}" group on SignalR connection (uses existing JoinBatch pattern)
- Reconnect auto-rejoins group (INFRA-05 already built in Phase 163)
- Toast notifications for both start and submit events using existing assessment-hub.js toast pattern
- Score appears immediately in monitoring table row on worker submit

### Start Event Behavior
- Push originates from StartExam GET action in CMPController (after session creation DB write)
- HC monitoring row: status changes "Belum Mulai" → "Dalam Pengerjaan", progress bar appears at 0%, row flashes briefly
- Toast on HC page: "{Name} memulai ujian"

### Submit Event Behavior
- Push originates from SubmitExam POST action in CMPController (after grading + score save)
- HC monitoring row: status → "Selesai", score appears, progress bar → 100%, row flashes green briefly
- Toast on HC page: "{Name} menyelesaikan ujian (Skor: {score})"
- Push payload includes score so HC sees it immediately

### Polling Removal Strategy
- Two-stage approach within this phase: (1) Build push + test, (2) Remove polling after UAT checkpoint confirms stability
- Delete server-side endpoints entirely (CheckExamStatus, GetMonitoringProgress controller actions) — no deprecation, clean removal
- Remove: CheckExamStatus poll (StartExam.cshtml line 786) and GetMonitoringProgress poll (AssessmentMonitoringDetail.cshtml line 840)
- Keep: 1-second timer countdown (line 331), 30-second auto-save (line 547) — these are local timer and data persistence, not polling
- navPoll (line 588) and rPoll (line 627): investigate during planning whether these can be replaced by push (Claude's discretion)

### Claude's Discretion
- Row flash animation CSS implementation
- Exact toast message formatting and duration
- Whether navPoll and rPoll intervals can be replaced by SignalR or should stay
- Monitor group naming convention (e.g., "monitor-{title}|{category}|{date}" matching batch key pattern)
- IHubContext injection into CMPController constructor pattern

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AssessmentHub` at `/hubs/assessment`: JoinBatch/LeaveBatch already implemented — add JoinMonitor/LeaveMonitor or reuse batch groups
- `assessment-hub.js`: connection setup, reconnect logic, toast notifications, `window.assessmentHub` exposed
- `assessment-hub.css`: toast styling already exists
- Phase 164 pattern: IHubContext<AssessmentHub> injected in AdminController — replicate in CMPController

### Established Patterns
- DB write before SignalR push always (Phase 163/164 decision)
- Use `Clients.Group()` or `Clients.User()`, never connection ID dictionaries
- `ExecuteUpdateAsync` with WHERE-clause guard for first-write-wins
- Cookie auth returns 401 for `/hubs/` prefix

### Integration Points
- `Controllers/CMPController.cs`: SaveAnswer (add progress push), StartExam GET (add started push), SubmitExam POST (add submitted push)
- `Views/Admin/AssessmentMonitoringDetail.cshtml`: Add SignalR event handlers for progressUpdate, workerStarted, workerSubmitted; add row flash; uses existing connection badge from Phase 164
- `Views/CMP/StartExam.cshtml`: Remove CheckExamStatus polling after UAT
- Polling endpoints to delete: CheckExamStatus (CMPController), GetMonitoringProgress (AdminController)

### Current Polling Code Locations
- `StartExam.cshtml:786` — `setInterval(checkExamStatus, 10000)` polling CheckExamStatus
- `AssessmentMonitoringDetail.cshtml:840` — `setInterval(fetchProgress, 10000)` polling GetMonitoringProgress
- `AssessmentMonitoringDetail.cshtml:841` — `setInterval(tickCountdowns, 1000)` (local timer, keep)

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard SignalR push pattern following established Phase 164 conventions.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 165-worker-to-hc-progress-push-polling-removal*
*Context gathered: 2026-03-13*
