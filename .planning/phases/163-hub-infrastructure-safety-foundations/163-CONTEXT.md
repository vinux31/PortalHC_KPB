# Phase 163: Hub Infrastructure & Safety Foundations - Context

**Gathered:** 2026-03-13
**Status:** Ready for planning

<domain>
## Phase Boundary

A working, authenticated SignalR endpoint at `/hubs/assessment` with SQLite concurrency protection and reconnect-safe group membership — the prerequisite for all real-time features. No push events are implemented here (Phase 164-165).

</domain>

<decisions>
## Implementation Decisions

### SignalR Group Strategy
- Per-batch group naming: `batch-{batchId}`
- Workers join group on exam page load (StartExam)
- HC joins group on monitoring detail page load (AssessmentMonitoringDetail)
- Individual worker targeting via `Clients.User(userId)` for AkhiriUjian
- Batch-wide broadcast via group for AkhiriSemua
- No `Dictionary<userId, connectionId>` — already decided in prior sessions

### Race Condition Guards
- Guard all write actions: AkhiriUjian, SubmitExam, Reset, and auto-save (SaveAnswer)
- Use WHERE clause filter pattern: `ExecuteUpdateAsync` with `WHERE Status == "InProgress"` — 0 rows affected = no-op
- First write wins, silent skip — no error shown to the "loser"; they see updated state on next refresh/push
- DB write always before SignalR push (carried forward from prior decisions)

### Reconnect UX
- Toast notification on disconnect: "Koneksi terputus..."
- Toast notification on reconnect: "Koneksi pulih"
- Retry with backoff: 0s, 2s, 5s, 10s, 30s — give up after ~47 seconds
- On permanent failure: toast with "Koneksi gagal" + "Muat Ulang" reload button (no auto-reload)
- On reconnect success: rejoin batch group only — polling fallback covers missed events (polling stays active through Phase 164)
- On 401 during reconnect: show "Sesi login habis — silakan login ulang" with login link

### Hub Method Naming
- Hub class: `AssessmentHub` at `/hubs/assessment`
- Server methods (C#): PascalCase — `JoinBatch`, `LeaveBatch`
- Client events (JS): camelCase — `examClosed`, `sessionReset`, `progressUpdate`
- Phase 163 scope: only `JoinBatch` + `LeaveBatch` methods — event methods added in Phase 164-165

### JS Client Placement
- Load SignalR JS only on assessment pages (StartExam, AssessmentMonitoringDetail) via `@section Scripts`
- Shared module: `wwwroot/js/assessment-hub.js` with connect/reconnect/group-join/toast logic
- SignalR library vendored in `wwwroot/lib/signalr/signalr.min.js` (matches existing wwwroot/lib pattern)
- Simple custom toast function within assessment-hub.js — no Bootstrap Toast dependency

### Auth 401 Configuration
- Cookie auth returns 401 (not 302 redirect) only for `/hubs/` path prefix — no change to other AJAX calls
- Implemented in `ConfigureApplicationCookie` OnRedirectToLogin event

### WAL Mode Activation
- Enable WAL via `PRAGMA journal_mode=WAL;` in Program.cs after EnsureCreatedAsync
- Verify WAL is active by querying `PRAGMA journal_mode;` and logging result on startup

### Claude's Discretion
- Exact toast styling and positioning
- SignalR retry policy implementation details
- WAL PRAGMA execution method (raw SQL via DbContext or separate connection)
- Assessment-hub.js internal structure and API surface

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ExecuteUpdateAsync` pattern already used in CMPController for status-guarded updates — extend to all write actions
- `ConfigureApplicationCookie` in Program.cs (line 88) — add OnRedirectToLogin handler here
- `wwwroot/lib/` directory — established vendor library location (bootstrap, jquery)

### Established Patterns
- Cookie authentication with `AddIdentity` + `ConfigureApplicationCookie`
- EF Core SQLite with `AddDbContext<ApplicationDbContext>`
- `@section Scripts` pattern in views for page-specific JS
- No existing SignalR code — greenfield setup

### Integration Points
- `Program.cs`: Add `AddSignalR()`, `MapHub<AssessmentHub>()`, WAL pragma, auth 401 config
- `Views/CMP/StartExam.cshtml`: Load assessment-hub.js, join batch group
- `Views/Admin/AssessmentMonitoringDetail.cshtml`: Load assessment-hub.js, join batch group
- `Controllers/CMPController.cs`: Add status guards to SubmitExam, SaveAnswer
- `Controllers/AdminController.cs`: Add status guards to AkhiriUjian, Reset

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches. Infrastructure phase follows ASP.NET Core SignalR conventions.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 163-hub-infrastructure-safety-foundations*
*Context gathered: 2026-03-13*
