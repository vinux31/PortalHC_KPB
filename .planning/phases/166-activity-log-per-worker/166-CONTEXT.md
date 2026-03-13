# Phase 166: Activity Log Per-Worker - Context

**Gathered:** 2026-03-13
**Status:** Ready for planning

<domain>
## Phase Boundary

HC can view a detailed timeline of each worker's exam activity — page navigation, disconnect/reconnect events, start, and submit — for audit and fairness purposes. Activity data is stored server-side and survives browser refresh. Accessed via modal from the HC monitoring detail page.

</domain>

<decisions>
## Implementation Decisions

### Event Capture Scope
- Events to log: exam start, page navigation, disconnect, reconnect, exam submit
- Answer saves are NOT logged (too noisy — user explicitly excluded this)
- Action-only logging — no answer content captured
- Disconnect/reconnect detected server-side via Hub OnDisconnectedAsync/OnConnectedAsync

### Capture Mechanism
- Page navigation: worker browser calls SignalR hub method (e.g., `LogPageNav`) using existing connection
- Start/submit: controller-side insert in StartExam/SubmitExam actions alongside existing DB writes
- Disconnect/reconnect: server-side AssessmentHub overrides (OnDisconnectedAsync/OnConnectedAsync)

### Log Viewing UX
- Modal opens from a button/icon on the worker's row in monitoring detail
- Button only visible for workers who have at least 1 logged event
- Plain text chronological timeline (timestamp + description), no color-coded icons
- Brief summary header at top: answered count, time spent, disconnect count
- Data loads once when modal opens (not live-updating)

### Event Types & Labels
- Database stores enum codes: "started", "page_nav", "disconnected", "reconnected", "submitted"
- Display labels in Bahasa Indonesia: "Memulai Ujian", "Pindah ke Halaman X", "Terputus", "Tersambung Kembali", "Menyelesaikan Ujian"

### Data Retention
- Logs persist while assessment record exists
- Cascade-deleted when HC deletes the assessment (not on group close)
- No limit on event count per worker per session

### Performance
- Activity log DB writes are fire-and-forget (async, don't wait) — logging never blocks the exam flow

### Claude's Discretion
- Whether to show an event count badge on the log button in the worker row
- Exact modal layout and sizing
- ActivityLog table schema design (columns, indexes)
- Summary header calculation logic

</decisions>

<specifics>
## Specific Ideas

- Modal title: "Activity Log: {Worker Name}"
- Summary format: "Dijawab: 7/10 | Waktu: 23 menit | Terputus: 1x"
- Timeline entries show time only (HH:mm:ss), not full datetime — exam is within a single day

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AssessmentHub` (Hubs/AssessmentHub.cs): Add hub methods for LogPageNav, override OnDisconnectedAsync/OnConnectedAsync
- `assessment-hub.js`: Existing SignalR connection setup, reconnect logic — add hub.invoke('LogPageNav') calls
- Bootstrap modal pattern used throughout the app for similar detail views

### Established Patterns
- DB-write-before-push (Phase 163/164 convention)
- IHubContext<AssessmentHub> injection in controllers (Phase 164/165)
- `ExecuteUpdateAsync` with WHERE-clause guard for first-write-wins
- Cookie auth returns 401 for `/hubs/` prefix

### Integration Points
- `Controllers/CMPController.cs`: StartExam (log "started"), SubmitExam (log "submitted")
- `Hubs/AssessmentHub.cs`: Add LogPageNav method, override OnDisconnectedAsync/OnConnectedAsync
- `Views/Admin/AssessmentMonitoringDetail.cshtml`: Add log button per worker row, modal markup, AJAX fetch for log data
- `Views/CMP/StartExam.cshtml`: Add hub.invoke('LogPageNav') on page navigation JS
- New: `Models/ExamActivityLog.cs` entity + DbSet registration
- New: Controller action to fetch activity log data (GET endpoint for modal AJAX)

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 166-activity-log-per-worker*
*Context gathered: 2026-03-13*
