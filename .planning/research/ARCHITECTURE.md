# Architecture Research

**Domain:** Real-time SignalR integration into existing ASP.NET Core 8 MVC + Identity app
**Researched:** 2026-03-13
**Confidence:** HIGH (official Microsoft docs verified)

## Standard Architecture

### System Overview

```
  Browser: HC Monitoring Page              Browser: Worker Exam Page
  (AssessmentMonitoringDetail.cshtml)      (StartExam.cshtml)
        |                                        |
  SignalR JS client                        SignalR JS client
        |                                        |
        +------------------+  +-----------------+
                           |  |
              WebSocket / SSE / Long-poll (auto-negotiated)
                           |
                  [ /hubs/assessment ]
                           |
                  AssessmentHub.cs
                   (Hub class, Hubs/)
                     |           |
              IHubContext    Groups in memory
                     |           |
           AdminController   On JS connect:
           CMPController      worker invokes JoinExamGroup(sessionId)
           (inject IHubContext  HC invokes JoinMonitorGroup(title, category, date)
            to push events)
                     |
              ApplicationDbContext
              (state still persisted in DB — SignalR is additive, not a replacement)
```

### Component Responsibilities

| Component | Responsibility | Location |
|-----------|----------------|----------|
| `AssessmentHub` | Hub class — defines client-callable methods, manages group join/leave | `Hubs/AssessmentHub.cs` (NEW) |
| `IHubContext<AssessmentHub>` | Injected into controllers to push server-initiated events | Used in `AdminController`, `CMPController` |
| Worker exam group `exam-{sessionId}` | Receives HC-to-Worker pushes: `examReset`, `examForceClosed` | Joined by worker on `StartExam` page load |
| Monitor group `monitor-{title}-{category}-{date}` | Receives Worker-to-HC pushes: `progressUpdate`, `examSubmitted` | Joined by HC on `AssessmentMonitoringDetail` page load |
| JS client (monitoring view) | Connects hub, handles `progressUpdate` event, replaces `setInterval(fetchProgress, 10000)` | `AssessmentMonitoringDetail.cshtml` (MODIFIED) |
| JS client (exam view) | Connects hub, handles `examReset` and `examForceClosed` events, replaces `setInterval(checkExamStatus, 10000)` | `StartExam.cshtml` (MODIFIED) |

## Recommended Project Structure

```
PortalHC_KPB/
├── Hubs/
│   └── AssessmentHub.cs          # NEW — single Hub class for all assessment real-time events
├── Controllers/
│   ├── AdminController.cs        # MODIFIED — inject IHubContext, push after Reset/ForceClose
│   └── CMPController.cs          # MODIFIED — inject IHubContext, push after SaveAnswer/SubmitExam
├── Program.cs                    # MODIFIED — AddSignalR(), MapHub<AssessmentHub>()
├── Views/
│   ├── Admin/
│   │   └── AssessmentMonitoringDetail.cshtml  # MODIFIED — replace polling with hub events
│   └── CMP/
│       └── StartExam.cshtml                   # MODIFIED — replace status poll with hub events
└── wwwroot/
    └── lib/
        └── microsoft/
            └── signalr/
                └── dist/
                    └── browser/
                        └── signalr.min.js     # NEW — installed via libman or npm
```

### Structure Rationale

- **Hubs/**: Single folder matches ASP.NET Core convention. One hub is sufficient — all assessment real-time events share the same connection lifecycle and auth context.
- **No new controller**: Hub handles its own connection lifecycle (OnConnectedAsync, OnDisconnectedAsync). Controllers push via IHubContext, not by calling Hub methods directly.
- **Single hub**: Two hubs would require two JS connections. One hub with named groups is simpler. The URL `/hubs/assessment` is clean and scope-specific.

## Architectural Patterns

### Pattern 1: Hub Groups for Exam Sessions

**What:** On page load, the JS client calls a hub method (`JoinExamGroup` or `JoinMonitorGroup`) which adds the connection to an in-memory named group. All subsequent server pushes target the group name, not individual connection IDs.

**When to use:** Always — this is the correct model for this use case. Worker group is per-session (one session, one worker). Monitor group is per-assessment-batch (one batch, potentially multiple HC users).

**Trade-offs:** Groups are in-memory only. Server restart clears all group memberships. The JS client must re-join groups after reconnect via the `onreconnected` callback. For a single-server intranet deployment this is fully sufficient.

**Example:**
```csharp
// Hubs/AssessmentHub.cs
[Authorize]
public class AssessmentHub : Hub
{
    public async Task JoinExamGroup(int sessionId)
    {
        // Worker joins group for their own exam session
        await Groups.AddToGroupAsync(Context.ConnectionId, $"exam-{sessionId}");
    }

    public async Task JoinMonitorGroup(string title, string category, string scheduleDate)
    {
        // HC joins monitoring group for an assessment batch
        var groupName = $"monitor-{title}-{category}-{scheduleDate}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}
```

### Pattern 2: IHubContext Push from MVC Controllers

**What:** After HC performs Reset or ForceClose (POST actions in AdminController), inject `IHubContext<AssessmentHub>` and call `Clients.Group(...).SendAsync(...)` to push the event to affected workers. After SaveAnswer or SubmitExam in CMPController, push a `progressUpdate` to the HC monitoring group.

**When to use:** When the event origin is an HTTP POST action in a controller, not a hub method call. This is the correct pattern — do not try to call Hub methods from controllers.

**Trade-offs:** IHubContext does not have access to `Context` (no caller info). It can only push to users, groups, or all. This is fine since these are server-originated events.

**Example:**
```csharp
// AdminController.cs — constructor addition
private readonly IHubContext<AssessmentHub> _hub;

// In ResetAssessment POST — after DB save:
await _hub.Clients.Group($"exam-{id}").SendAsync("examReset", new {
    sessionId = id,
    message   = "Ujian telah direset oleh HC."
});

// In ForceCloseAssessment POST — after DB save:
await _hub.Clients.Group($"exam-{id}").SendAsync("examForceClosed", new {
    sessionId   = id,
    redirectUrl = Url.Action("Assessment", "CMP")
});
```

### Pattern 3: Cookie Auth Flows Automatically — No Extra Config

**What:** Because this is a browser-based MVC app using ASP.NET Identity cookie auth, the SignalR JS client automatically sends the auth cookie on the WebSocket/SSE upgrade request. The hub sees `Context.User` populated with the same claims as MVC controllers.

**When to use:** Always — this is the default behavior for browser clients with cookie auth. The official ASP.NET Core 8 docs confirm: "If the user is logged in to an app, the SignalR connection automatically inherits this authentication."

**Trade-offs:** None for this project. The only additional step is placing `[Authorize]` on the Hub class to reject unauthenticated connections at the hub level, mirroring the `[Authorize]` already on all MVC controllers.

**Example:**
```csharp
[Authorize]  // mirrors pattern on all MVC controllers in this app
public class AssessmentHub : Hub
{
    // Context.User is the authenticated ApplicationUser
    // Context.UserIdentifier is ClaimTypes.NameIdentifier (ASP.NET Identity user ID)
}
```

## Data Flow

### HC ForceClose to Worker Push

```
HC clicks "Tutup Paksa"
    |
    v
POST /Admin/ForceCloseAssessment/{id}  [Authorize(Roles="Admin,HC")]
    |
    v
AdminController: update DB status = "Completed"  (DB write first)
    |
    v
IHubContext<AssessmentHub>.Clients.Group("exam-{id}").SendAsync("examForceClosed", payload)
    |
    v
AssessmentHub (in-memory) resolves group "exam-{id}"
    |
    v
WebSocket push to worker's browser
    |
    v
Worker JS: receives "examForceClosed" event
    |
    v
JS handler: show forceCloseModal, then redirect
(replaces: setInterval(checkExamStatus, 10000) polling /CMP/CheckExamStatus)
```

### Worker Progress to HC Monitoring Push

```
Worker answers question
    |
    v
POST /CMP/SaveAnswer  (existing endpoint)
    |
    v
CMPController: save answer in DB
    |
    v
IHubContext<AssessmentHub>.Clients
    .Group("monitor-{title}-{category}-{date}")
    .SendAsync("progressUpdate", dto)
    |
    v
AssessmentHub resolves monitoring group
    |
    v
WebSocket push to all HC browsers watching that batch
    |
    v
HC monitoring view JS: receives "progressUpdate" — calls existing updateRow(data)
(replaces: setInterval(fetchProgress, 10000) polling /Admin/GetMonitoringProgress)
```

### Connection Lifecycle

```
Worker loads StartExam page
    |
    v
JS: new HubConnectionBuilder().withUrl("/hubs/assessment").withAutomaticReconnect().build()
    |   (auth cookie sent automatically on WS upgrade — no extra config needed)
    v
connection.start()  =>  Hub.OnConnectedAsync fires
    |
    v
JS: connection.invoke("JoinExamGroup", sessionId)
    |
    v
Hub: Groups.AddToGroupAsync(connectionId, "exam-{sessionId}")
    |
    v
Worker is now in group — receives push events until disconnect

On reconnect (withAutomaticReconnect handles this automatically):
    |
    v
connection.onreconnected(() => { connection.invoke("JoinExamGroup", sessionId); })
(Group membership is not preserved across reconnect — must re-join explicitly)
```

## Integration Points

### New Components

| Component | File | What It Is |
|-----------|------|-----------|
| `AssessmentHub` | `Hubs/AssessmentHub.cs` | New Hub class — cookie auth, group join methods |
| SignalR client lib | `wwwroot/lib/microsoft/signalr/dist/browser/signalr.min.js` | Client JS library via libman install |
| Program.cs SignalR wiring | `Program.cs` | `builder.Services.AddSignalR()` + `app.MapHub<AssessmentHub>("/hubs/assessment")` |

### Modified Components

| Component | File | What Changes |
|-----------|------|-------------|
| `AdminController` | `Controllers/AdminController.cs` | Inject `IHubContext<AssessmentHub>` in constructor; push `examReset` after ResetAssessment; push `examForceClosed` after ForceCloseAssessment and ForceCloseAll |
| `CMPController` | `Controllers/CMPController.cs` | Inject `IHubContext<AssessmentHub>` in constructor; push `progressUpdate` after SaveAnswer; push `examSubmitted` after SubmitExam |
| Monitoring view JS | `Views/Admin/AssessmentMonitoringDetail.cshtml` | Replace `setInterval(fetchProgress, 10000)` with hub connection + `JoinMonitorGroup` call + `progressUpdate` event handler; existing `updateRow()` and `updateSummary()` functions can be reused as-is |
| Exam view JS | `Views/CMP/StartExam.cshtml` | Replace `setInterval(checkExamStatus, 10000)` with hub connection + `JoinExamGroup` call + `examForceClosed` / `examReset` event handlers; existing force-close modal and redirect logic reused |

### Unchanged Components

| Component | Reason |
|-----------|--------|
| `CheckExamStatus` action in CMPController | Keep as legacy fallback during rollout; remove in cleanup phase after UAT |
| `GetMonitoringProgress` action in AdminController | Keep as legacy fallback during rollout; remove in cleanup phase after UAT |
| All DB schema / migrations | No migration needed — SignalR is transport only, all state stays in `AssessmentSession` table |
| `INotificationService` | Different system (persistent in-app notifications stored in DB) — not replaced by SignalR |
| `AssessmentSession.Status` column | Remains source of truth — DB write always happens before hub push |

## Build Order

Dependencies must be addressed in this sequence:

**Step 1: Install SignalR client library**
`wwwroot/lib` must have `signalr.min.js` before view JS can reference it. Use `libman install @microsoft/signalr` or copy from node_modules. This is a prerequisite for Steps 6 and 7.

**Step 2: Create `Hubs/AssessmentHub.cs`**
Must exist before Program.cs can reference `AssessmentHub` in `MapHub<>`.

**Step 3: Update `Program.cs`**
Add `builder.Services.AddSignalR()` after existing services. Add `app.MapHub<AssessmentHub>("/hubs/assessment")` after `app.UseAuthorization()` — the middleware order matters. This makes the hub reachable.

**Step 4: Inject IHubContext into AdminController**
Add `IHubContext<AssessmentHub>` as constructor parameter. Push `examReset` from ResetAssessment, `examForceClosed` from ForceCloseAssessment and ForceCloseAll. No DB changes needed.

**Step 5: Inject IHubContext into CMPController**
Add `IHubContext<AssessmentHub>` as constructor parameter. Push `progressUpdate` from SaveAnswer (each answer saved), `examSubmitted` from SubmitExam. No DB changes needed.

**Step 6: Update `StartExam.cshtml` JS**
Replace `checkExamStatus` polling block with hub connection, `JoinExamGroup` invocation, and `examForceClosed` / `examReset` handlers. Keep existing timer, submit logic, and modal markup unchanged.

**Step 7: Update `AssessmentMonitoringDetail.cshtml` JS**
Replace `fetchProgress` polling block with hub connection, `JoinMonitorGroup` invocation, and `progressUpdate` handler. The existing `updateRow()` and `updateSummary()` functions can be called directly from the handler since the DTO shape from the hub push should match what the polling endpoint returned.

Steps 4 and 5 can be done in parallel. Steps 6 and 7 can be done in parallel. Each step is independently deployable — the polling fallback continues to function during the migration.

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Single-server intranet (current) | In-memory SignalR groups — fully sufficient, zero additional infra |
| Multi-server / load-balanced | Add Redis backplane: `builder.Services.AddSignalR().AddStackExchangeRedis(connectionString)` — no Hub code changes |
| Azure-hosted | Azure SignalR Service replaces in-memory transport with a managed service — one config change in Program.cs |

### Scaling Priorities

1. **Current project:** No scaling concern. Single intranet server. In-memory groups are the correct and simplest choice.
2. **If multi-server ever required:** Redis backplane is one line addition. Hub code and client JS are unchanged.

## Anti-Patterns

### Anti-Pattern 1: Calling Hub Methods Directly from Controller

**What people do:** Try to call methods defined in `AssessmentHub` from `AdminController` via `new AssessmentHub(...)` or a static reference.

**Why it's wrong:** Hub instances are created per-connection by the SignalR infrastructure. Manually instantiating a Hub does not give access to the connection pool — no clients receive the message.

**Do this instead:** Inject `IHubContext<AssessmentHub>` into the controller. This gives access to `Clients`, `Groups`, and `Users` without a hub instance.

### Anti-Pattern 2: Un-scoped Group Names

**What people do:** Use generic group names like `"monitoring"` or `"exam"` without per-session scoping.

**Why it's wrong:** All HC users across all active assessments receive each other's updates. ForceClose events fire on the wrong workers' screens.

**Do this instead:** Scope group names to the entity. `"exam-{sessionId}"` for worker groups (one session = one worker). `"monitor-{title}-{category}-{date}"` for HC monitoring groups — this key already exists in the codebase (`GetMonitoringProgress` uses the same three parameters as the batch identifier).

### Anti-Pattern 3: Replacing DB State with SignalR State

**What people do:** Stop persisting status changes to DB and rely on SignalR push as the source of truth.

**Why it's wrong:** SignalR connections are ephemeral. Worker refreshes page mid-exam — no event is replayed. HC opens a second browser tab — they see stale data. The DB is the only reliable state store.

**Do this instead:** DB write always happens first. SignalR push happens after the DB write succeeds. The push is a notification that something changed, not the change itself.

### Anti-Pattern 4: Removing Polling Endpoints Before Hub Is Proven Stable

**What people do:** Delete `CheckExamStatus` and `GetMonitoringProgress` in the same commit that adds SignalR.

**Why it's wrong:** If the hub has a configuration bug or WebSocket is blocked by a corporate proxy, workers and HC have no fallback.

**Do this instead:** Keep both endpoints during the first milestone. Mark them `// LEGACY: fallback for SignalR`. Remove in a subsequent cleanup phase after UAT confirms the hub is reliable.

### Anti-Pattern 5: Not Re-joining Groups After Reconnect

**What people do:** Set up group join on initial connect but forget the `onreconnected` callback.

**Why it's wrong:** `withAutomaticReconnect()` re-establishes the WebSocket connection but does NOT restore group membership (groups are keyed by connection ID, which changes on reconnect). The worker appears connected but receives no push events.

**Do this instead:** Register `connection.onreconnected(() => { connection.invoke("JoinExamGroup", sessionId); })` in the exam view JS. Same pattern for the monitoring view.

## Sources

- [Authentication and authorization in ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-8.0) — HIGH confidence, official Microsoft docs
- [Manage users and groups in SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups?view=aspnetcore-10.0) — HIGH confidence, official Microsoft docs
- Direct code inspection: `Controllers/AdminController.cs` (GetMonitoringProgress, ResetAssessment, ForceCloseAssessment, ForceCloseAll), `Controllers/CMPController.cs` (CheckExamStatus, SubmitExam, SaveAnswer), `Views/Admin/AssessmentMonitoringDetail.cshtml` (polling JS), `Views/CMP/StartExam.cshtml` (statusPollInterval JS)

---
*Architecture research for: SignalR real-time assessment integration, ASP.NET Core 8 MVC*
*Researched: 2026-03-13*
