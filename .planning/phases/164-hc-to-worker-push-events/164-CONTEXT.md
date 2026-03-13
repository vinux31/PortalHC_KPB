# Phase 164: HC-to-Worker Push Events - Context

**Gathered:** 2026-03-13
**Status:** Ready for planning

<domain>
## Phase Boundary

HC actions (Reset, Akhiri Ujian, Akhiri Semua) push instantly to worker exam pages via SignalR, eliminating the 10-second polling window. Connection status badge visible on exam and monitoring pages. No worker-to-HC push (Phase 165).

</domain>

<decisions>
## Implementation Decisions

### Worker Close Notification (Akhiri Ujian)
- Reuse existing `examClosedModal` in StartExam.cshtml — SignalR handler triggers the same modal that polling currently shows
- Modal is fully blocking: static backdrop, no X button, no escape key — worker can only click "Lihat Hasil"
- Text differs by source: push from HC = "Ujian diakhiri oleh pengawas", polling/timer = "Waktu ujian habis" (dynamic text based on reason)
- "Lihat Hasil" button redirects to ExamResults page

### Reset Notification
- New blocking modal: "Sesi direset oleh pengawas" with "Kembali" button
- On reset received: timer countdown stops, auto-save stops, form controls disabled
- "Kembali" button redirects to Assessment list page (/CMP/Assessment), not directly to StartExam
- Worker must re-enter exam manually from the assessment list

### Connection Status Badge
- Positioned in navbar/header area, near exam timer (top right)
- 3 states: 🟢 Live, 🟡 Reconnecting, 🔴 Disconnected
- Labels in English: "Live" / "Reconnecting" / "Disconnected"
- Displayed on both worker exam page (StartExam) AND HC monitoring page (AssessmentMonitoringDetail)
- Badge reflects SignalR connection state events (onreconnecting, onreconnected, onclose)

### Akhiri Semua Broadcast
- Same modal as individual Akhiri Ujian — all InProgress workers see "Ujian diakhiri oleh pengawas"
- Group broadcast: single `Clients.Group("batch-{batchId}").SendAsync("examClosed", ...)` call
- Workers with Open/Not-started status (cancelled by Akhiri Semua) do NOT receive push — they see Cancelled status when they open Assessment page later
- HC monitoring page ignores `examClosed` event — no handler registered on monitoring page for this event

### IHubContext Injection
- AdminController injects `IHubContext<AssessmentHub>` via constructor
- After DB write completes, controller calls `_hubContext.Clients.Group/User(...).SendAsync(...)` directly
- No service wrapper layer — direct injection is sufficient for 2-3 events

### Event Payloads
- `examClosed` → `{ reason: "hc_closed" }` — JS uses reason to set modal text dynamically
- `sessionReset` → `{ reason: "hc_reset" }` — JS triggers reset modal
- No redirect URLs in payload — JS already knows target pages (Results for close, Assessment for reset)

### Fallback / Dual Trigger
- Polling (CheckExamStatus every 10s) stays active alongside SignalR push
- Existing `examClosed` flag in JS prevents double-modal: whoever triggers first (push or poll) sets flag, the other skips

### Page Scope
- Only StartExam.cshtml handles push events (examClosed, sessionReset)
- AssessmentMonitoringDetail.cshtml only gets connection badge — no event handlers for Phase 164

### Claude's Discretion
- Exact modal styling and icon choices
- Guard implementation for dual trigger (push + polling)
- IHubContext constructor injection pattern details
- Badge CSS implementation and animation

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AssessmentHub` at `/hubs/assessment`: JoinBatch/LeaveBatch already implemented
- `assessment-hub.js`: connection setup, reconnect logic, toast notifications, `window.assessmentHub` exposed
- `assessment-hub.css`: toast styling already exists
- `examClosedModal` in StartExam.cshtml (line 227): existing modal to reuse for push-triggered close
- `examClosed` flag in StartExam.cshtml JS (line 718): prevents double-trigger, reusable for SignalR guard

### Established Patterns
- `ExecuteUpdateAsync` with WHERE-clause guard for first-write-wins (Phase 163)
- `@section Scripts` for page-specific JS loading
- `window.assessmentBatchKey` set in view, consumed by assessment-hub.js
- Cookie auth returns 401 for `/hubs/` prefix (Phase 163)

### Integration Points
- `Controllers/AdminController.cs`: AkhiriUjian (line 2254), AkhiriSemuaUjian (line 2336), Reset (line 2203) — add IHubContext.SendAsync after DB write
- `Views/CMP/StartExam.cshtml`: Add SignalR event handlers for examClosed + sessionReset, add reset modal, add connection badge
- `Views/Admin/AssessmentMonitoringDetail.cshtml`: Add connection badge only (no event handlers in Phase 164)
- `wwwroot/js/assessment-hub.js`: May need to expose connection state change hooks for badge updates

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard SignalR push pattern following ASP.NET Core conventions.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 164-hc-to-worker-push-events*
*Context gathered: 2026-03-13*
