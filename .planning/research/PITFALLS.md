# Pitfalls Research

**Domain:** ASP.NET Core MVC + SignalR real-time assessment monitoring (brownfield integration)
**Researched:** 2026-03-13
**Confidence:** HIGH (based on codebase analysis + official Microsoft docs + community issues)

---

## Critical Pitfalls

### Pitfall 1: Hub Groups Lost on Reconnect — HC Stops Receiving Worker Updates

**What goes wrong:**
HC opens the monitoring page and joins a SignalR group for an assessment batch (e.g., group `"assessment-batch-3"`). Worker progress events are pushed to that group. When the network flickers or the server restarts, the HC's connection reconnects automatically via `withAutomaticReconnect()` — but group membership is NOT restored. The HC's monitoring page reconnects silently, shows no error, but stops receiving worker updates. HC believes the exam is idle while workers are actively submitting.

**Why it happens:**
SignalR groups are in-memory, per-connection state on the server. There is no persistence. When `OnConnectedAsync` is not re-called on reconnect (only a new connection triggers it), the client is connected but unjoined. Microsoft's official docs state explicitly: "Reconnection does not automatically restore group membership — this must be handled manually."

The auto-reconnect callbacks (`onreconnected`) are easy to overlook when building the initial happy path.

**How to avoid:**
1. In the JS client, register an `onreconnected` handler that calls a hub method to re-join the group:
   ```javascript
   connection.onreconnected(async () => {
       await connection.invoke("RejoinBatch", title, category, scheduleDate);
   });
   ```
2. The hub's `RejoinBatch` method calls `Groups.AddToGroupAsync(Context.ConnectionId, groupName)` — same as the initial join.
3. After re-joining, request a state sync from the server to refresh stale data accumulated during the disconnect window.

**Warning signs:**
- HC monitoring page shows all workers as "last seen 30 seconds ago" and never updates
- No JavaScript console errors — reconnect succeeded but HC is silent
- Exam completes but monitoring page never shows "Completed" badge

**Phase to address:**
SignalR infrastructure phase. Add `onreconnected` handler and `RejoinBatch` hub method as part of the initial Hub implementation. Not a retrofit.

---

### Pitfall 2: Cookie Auth Redirect on SignalR Negotiate — 401 Becomes 302

**What goes wrong:**
The worker loads the exam page. The SignalR JS client calls `POST /examHub/negotiate`. ASP.NET Core cookie auth middleware intercepts the 401 and redirects to `/Account/Login?returnUrl=...` — returning HTTP 302 instead of 401. The SignalR JS client receives HTML (the login page) instead of a negotiate JSON response, logs a cryptic error, and fails to connect. Exam page appears to load fine but real-time events never work.

**Why it happens:**
Cookie auth in ASP.NET Core defaults to redirecting unauthenticated requests to the login page. This is correct for page requests but breaks API-style endpoints. SignalR negotiate is an API endpoint — it expects 401, not 302. The middleware's `OnRedirectToLogin` event must be overridden to return 401 for XHR/negotiate requests.

This is documented in ASP.NET Core 10 release notes: starting ASP.NET Core 10, known API endpoints no longer redirect but return 401/403. Earlier versions require manual configuration.

**How to avoid:**
In `Program.cs`, configure cookie auth to suppress redirects for API paths:
```csharp
builder.Services.ConfigureApplicationCookie(options => {
    options.Events.OnRedirectToLogin = context => {
        if (context.Request.Path.StartsWithSegments("/examHub") ||
            context.Request.Path.StartsWithSegments("/monitorHub")) {
            context.Response.StatusCode = 401;
        } else {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});
```
Alternatively, add `[Authorize]` on the Hub class — authenticated users already have valid cookies so negotiation proceeds normally. Verify by testing negotiate endpoint directly.

**Warning signs:**
- Browser DevTools Network tab: `/examHub/negotiate` returns status 302 or 200 with HTML content-type
- SignalR JS client logs: `"Failed to complete negotiation with the server"` or `"Error: Server returned handshake error: Unexpected character encountered while parsing value"`
- Works in development (where you're logged in) but fails after fresh login in a new session

**Phase to address:**
Phase 1 (Hub infrastructure setup). Test negotiate endpoint immediately after adding the Hub before writing any client-side event handlers.

---

### Pitfall 3: SQLite "Database Is Locked" Under Concurrent SignalR + HTTP Writes

**What goes wrong:**
Worker saves an answer via `POST /CMP/SaveAnswer` (HTTP action, writes to `PackageUserResponses`). Simultaneously, HC calls `ForceCloseAssessment` (HTTP action, writes to `AssessmentSessions`). The Hub pushes that ForceClose event to the worker, and the worker's exam page calls `AbandonExam` (HTTP action, writes `AssessmentSessions`). Three concurrent writes to SQLite in the same 100ms window. SQLite throws `SqliteException: database is locked` on one of them. The worker receives no error feedback — the ForceClose write failed silently.

**Why it happens:**
SQLite only allows one writer at a time per database file. With SignalR, request frequency increases significantly (progress pushes, heartbeats) versus polling. Each push may trigger a DB read/write on the Hub. EF Core's default connection pool for SQLite can also create multiple connections that deadlock each other.

**How to avoid:**
1. Enable WAL (Write-Ahead Logging) mode in `Program.cs` or on first DB connection — WAL allows one concurrent writer plus concurrent readers:
   ```csharp
   // In DbContext OnConfiguring or Program.cs after migration
   context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
   ```
2. Configure a busy timeout so SQLite retries instead of immediately throwing:
   ```
   Data Source=HcPortal.db;Busy Timeout=5000
   ```
3. Keep Hub methods read-only where possible. Hub pushes state *changes* that already happened via HTTP actions — do not write to DB inside Hub methods. HTTP actions are the single writer.

**Warning signs:**
- `Microsoft.Data.Sqlite.SqliteException: database is locked` in application logs
- ForceClose action appears to succeed (returns 200) but session status doesn't change in DB
- Works fine in single-user testing, fails when 5+ workers are active simultaneously

**Phase to address:**
Phase 1 (Hub infrastructure setup). Enable WAL mode and busy timeout in Program.cs before any SignalR features are built on top. Cannot be retrofitted safely after data exists.

---

### Pitfall 4: Race Condition Between HTTP SubmitExam and Hub ForceClose Push

**What goes wrong:**
Worker clicks "Submit" at the exact moment HC clicks "Force Close" on the monitoring page. Both events reach the server within milliseconds:
- `SubmitExam` reads `AssessmentSession`, grades it, sets `Status = "Completed"`, calls `SaveChangesAsync()`
- HC's `ForceCloseAssessment` reads the same session, sets `Status = "Abandoned"`, calls `SaveChangesAsync()`

EF Core's default tracking means one of the writes overwrites the other. The worker's graded result can be replaced by "Abandoned" status, destroying their score. No exception is thrown.

**Why it happens:**
The existing `SubmitExam` action uses optimistic concurrency (`_context.AssessmentSessions.Update(assessment)`). Adding SignalR increases the likelihood that HC triggers ForceClose while worker submits — previously this race window was 10-second polling latency wide; with SignalR it becomes sub-second.

**How to avoid:**
1. Use `ExecuteUpdateAsync` in both `SubmitExam` and `ForceCloseAssessment` with a status guard:
   ```csharp
   // ForceClose only transitions from InProgress → Abandoned, never from Completed
   var updated = await _context.AssessmentSessions
       .Where(s => s.Id == id && s.Status != "Completed")
       .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Abandoned"));
   if (updated == 0) return; // Already completed — skip
   ```
2. `SubmitExam` similarly: only submit if `Status == "InProgress"`.
3. After ForceClose completes server-side, Hub pushes to the worker. Worker JS must redirect to a "session ended" page and NOT attempt to submit.

**Warning signs:**
- Worker submits exam, sees "Results" page with score, but in admin monitoring the session shows "Abandoned"
- Graded score exists in DB but session `IsPassed` is null
- Race visible only during load testing or when HC is very actively monitoring

**Phase to address:**
Phase handling ForceClose+Reset SignalR events. Add status-guarded `ExecuteUpdateAsync` in both `ForceCloseAssessment` and `SubmitExam` before wiring up SignalR pushes. This closes the race window that SignalR amplifies.

---

### Pitfall 5: Worker Tab Re-Opens After ForceClose Push — Connection ID Is Stale

**What goes wrong:**
HC force-closes a worker's session. The Hub sends `forceClose` event to the worker's connection ID. Worker's browser receives it, shows "Session ended" message, and the JS calls `connection.stop()`. Worker then refreshes the page (or presses Back). A new SignalR connection is established with a NEW connection ID. The Hub no longer has a mapping from `userId → connectionId` because the previous connection was stopped. If HC tries Reset → the Hub cannot push the Reset event to the worker's new connection.

**Why it happens:**
Storing `connectionId` per user in memory (a static dictionary or in-memory store) creates stale mappings. Each new page load generates a new connection ID. Server-side user-to-connection mapping becomes invalid on any page reload.

**How to avoid:**
1. Use SignalR's built-in user-based messaging instead of connection-ID-based messaging:
   ```csharp
   // Push to all connections for this user (works across tabs and reconnects)
   await Clients.User(userId).SendAsync("ForceClose");
   ```
   This requires `IUserIdProvider` to be wired up (cookie auth provides `HttpContext.User.Identity.Name` automatically).
2. Never maintain your own `userId → connectionId` dictionary. Use `Clients.User()` and `Clients.Group()` instead.
3. HC monitoring group membership is handled via `Groups.AddToGroupAsync` on connect (and re-join on reconnect per Pitfall 1).

**Warning signs:**
- ForceClose works the first time, but after worker refreshes the page, Reset event never reaches them
- Dictionary key not found exceptions in Hub logs when worker reconnects
- Working in 1-tab test but failing in UAT where worker opens exam in a second tab

**Phase to address:**
Phase 1 (Hub design). Use `Clients.User()` from the start. Do NOT build a user-to-connection-ID map and then migrate away from it later.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Keep polling as fallback alongside SignalR | "Insurance if SignalR fails" | Two code paths for the same data; polling queries run even when SignalR is active; monitoring page may show stale polling data conflicting with real-time data | Acceptable only during transition phase. Remove polling JavaScript once SignalR is stable in UAT. Never ship both permanently. |
| Write to DB inside Hub methods | Simpler — Hub handles everything | SignalR Hub methods execute on a thread-pool thread; concurrent Hub DB writes compound SQLite locking | Never. All DB writes must go through HTTP actions. Hub methods only read state or push notifications. |
| Store connection IDs in a static Dictionary | Fast to prototype | Stale IDs on reconnect; thread-unsafe access; memory leak if connections are never cleaned up | Never in production. Use `Clients.User()` or `Clients.Group()` from day one. |
| Hardcode group name without schedule date | Simpler group naming | Two different exam batches with same title but different dates share a group; HC monitoring one batch receives events from another | Never. Group key must include `title + category + scheduleDate` to be unique. |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Cookie auth + SignalR negotiate | Cookie auth redirects 401 → 302 to login page; SignalR negotiate fails with HTML response | Configure `OnRedirectToLogin` to return 401 for `/examHub` and `/monitorHub` paths; or verify Hub `[Authorize]` lets authenticated users through |
| EF Core DbContext + Hub | Injecting scoped `DbContext` into a singleton Hub causes ObjectDisposedException | Hub lifetime is transient by default in ASP.NET Core SignalR — scoped DbContext injection is safe. Verify DI lifetime. |
| jQuery + SignalR JS client | Loading `@microsoft/signalr` via CDN but also bundling it via npm causes two HubConnection instances | Pick one delivery method. For MVC app: CDN script tag with `integrity` hash is simplest. Do not bundle via webpack in addition. |
| `ForceCloseAll` action + SignalR group push | `ForceCloseAll` closes multiple sessions; must push ForceClose event to EACH affected worker separately | Loop over affected worker userIds, call `Clients.User(userId).SendAsync("ForceClose")` per user. Cannot use `Clients.Group()` for workers since they are not in the same HC group. |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Hub broadcasts to all clients instead of targeted group | Every connected user receives every exam event | Always scope pushes: `Clients.Group(batchGroupName)` for batch events, `Clients.User(userId)` for per-worker events | Breaks silently at any scale — workers on other exams receive irrelevant events; UX confusion |
| Progress push on every keystroke / SaveAnswer | 30+ SaveAnswer calls per worker per minute; each triggers a Hub broadcast | Throttle: push progress only on meaningful milestones (every 5th answer, or when tab changes). OR let HC pull progress on demand. | 20 workers × 30 saves/min = 600 Hub broadcasts/min; multiplied by monitoring connections |
| No server-side keepalive / ping timeout configured | Long-running exam (60 min) drops WebSocket connection silently after 30s idle if worker is reading and not answering | Configure `KeepAliveInterval` and `HandshakeTimeout` in Hub options. Default keep-alive is 15s ping; verify server and reverse proxy (IIS/nginx) WebSocket timeout is longer than exam duration. | Exams longer than default server/proxy timeout (~30s–2min depending on IIS config) |
| IIS WebSocket not enabled | SignalR falls back to Long Polling; not an error but performance degrades significantly | Ensure IIS WebSocket Protocol feature is installed. Verify in `Program.cs` that `UseWebSockets()` is called. Check transport in browser DevTools Network tab (should show 101 Switching Protocols). | Immediate — every deployment on IIS without WebSocket feature enabled |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| HC Hub method callable by Worker role | Worker calls `Clients.Group(batchGroup).SendAsync("ForceClose")` to attack peer exams | Add `[Authorize(Roles = "Admin,HC")]` on Hub methods that trigger HC actions (`ForceClose`, `ResetWorker`). Worker-facing Hub methods (join, progress update) can be authenticated-only without role. |
| No ownership check on Hub methods | Worker passes `sessionId=999` (another worker's session) to Hub method to inject events into another session | Hub methods must verify `session.UserId == Context.UserIdentifier` before processing, same as HTTP actions. |
| connectionId exposed in client-side JS | JS logs connection ID; attacker can guess other connection IDs and target them | Never expose `connection.connectionId` in HTML or JS variables accessible to page scripts. Use `Clients.User()` server-side — never route messages by client-supplied connectionId. |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| ForceClose push arrives with no visual explanation | Worker's exam page freezes or redirects; worker doesn't understand why | Show modal: "Your exam session has been ended by HC. Please contact your proctor." Before redirecting. Give 5-second countdown. |
| Reset push causes instant page reload without saving state | Worker loses current answer draft on the page | On Reset event: first save current in-progress answer (call `SaveAnswer` or `SavePackageAnswer`), THEN reload. |
| Monitoring page shows stale data on reconnect | HC reconnects after disconnect; progress cards show data from before disconnect, never updates | On `onreconnected`: call HTTP polling endpoint once to get full current state, then resume real-time updates. Hybrid: real-time for deltas, HTTP for initial/recovery state. |
| Worker gets ForceClose event but SaveAnswer was in-flight | In-flight XHR completes after ForceClose — answer saved but session is Abandoned; mismatch | Worker JS should set a flag `sessionEnded = true` on ForceClose event; `SaveAnswer` callback checks flag and ignores late responses. |

---

## "Looks Done But Isn't" Checklist

- [ ] **Auth on negotiate endpoint:** Tested `/examHub/negotiate` returns 101/200 (not 302/HTML). Verified in browser DevTools Network tab.
- [ ] **Group re-join on reconnect:** `onreconnected` handler calls `RejoinBatch` or equivalent. Verified by simulating disconnect (DevTools → offline → online).
- [ ] **WAL mode enabled:** `PRAGMA journal_mode=WAL` applied on DB startup. Verified: `PRAGMA journal_mode;` returns `wal`.
- [ ] **ForceClose race guard:** `ForceCloseAssessment` uses `WHERE Status != 'Completed'` guard. `SubmitExam` uses `WHERE Status == 'InProgress'` guard. Both verified with concurrent browser tabs.
- [ ] **User-based push, not connection-ID-based:** No `Dictionary<string, string>` mapping userId to connectionId anywhere in code. All server pushes use `Clients.User()` or `Clients.Group()`.
- [ ] **IIS WebSocket feature:** Browser DevTools shows `101 Switching Protocols` for SignalR connection. Not `200 OK` (long polling fallback).
- [ ] **Role auth on Hub methods:** HC-only Hub methods have `[Authorize(Roles = "Admin,HC")]`. Verified Worker cannot invoke them.
- [ ] **ForceCloseAll pushes to each worker:** When HC uses ForceCloseAll, each affected worker receives the ForceClose event individually. Verified with 2+ workers on same batch.
- [ ] **Polling JS removed after SignalR ships:** `setInterval` polling call removed from monitoring page JS. No duplicate data refresh running alongside SignalR.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Group re-join missing (HC silent after reconnect) | LOW | Add `onreconnected` callback + `RejoinBatch` hub method. No DB changes. Deploy and test. |
| 302 redirect on negotiate (SignalR never connects) | LOW | Add `OnRedirectToLogin` override in `Program.cs`. No schema changes. |
| SQLite locked errors under load | MEDIUM | Enable WAL mode (`PRAGMA journal_mode=WAL`). Requires brief maintenance window on production DB file. Verify no active connections during switch. |
| Submit/ForceClose race corrupts scores | HIGH | Add status-guarded `ExecuteUpdateAsync` to both actions. Requires retesting all exam submission paths. Run full UAT on exam lifecycle. |
| ConnectionId-based push map is stale | MEDIUM | Refactor to `Clients.User()`. Remove static dictionary. Test reconnect, multi-tab, ForceCloseAll scenarios. |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Cookie auth 302 on negotiate | Phase 1: Hub infrastructure | Test negotiate endpoint in browser DevTools before writing any event handlers |
| WAL mode not enabled | Phase 1: Hub infrastructure | Run `PRAGMA journal_mode;` query after startup, assert `wal` |
| ConnectionId-based mapping (stale on reconnect) | Phase 1: Hub design | Code review: zero occurrences of `Dictionary<userId, connectionId>` |
| Group lost on reconnect | Phase 1: Hub infrastructure | Simulate disconnect in DevTools, verify monitoring still receives pushes |
| Submit/ForceClose race | Phase handling ForceClose/Reset events | Concurrent browser tabs test: submit and force-close at same time, verify winner is Completed not Abandoned |
| IIS WebSocket not enabled | Phase 1: Hub infrastructure | DevTools Network: 101 Switching Protocols on Hub connection |
| Role auth missing on Hub methods | Phase 1: Hub infrastructure | Log in as Worker, attempt to invoke HC-only Hub method, expect 403 |
| ForceClose arrives with no UX explanation | Phase: Worker exam page real-time events | UAT: HC force-closes, worker sees explanatory modal (not silent redirect) |

---

## Sources

- [Authentication and authorization in ASP.NET Core SignalR — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0)
- [Security considerations in ASP.NET Core SignalR — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/signalr/security?view=aspnetcore-8.0)
- [Manage users and groups in SignalR — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups?view=aspnetcore-10.0)
- [How to deal with concurrency when using SignalR? — GitHub Issue #27956](https://github.com/dotnet/AspNetCore.Docs/issues/27956)
- [SignalR how to reconnect with same connectionId — GitHub Discussion #54818](https://github.com/dotnet/aspnetcore/discussions/54818)
- [SQLite concurrent writes and "database is locked" errors — Ten Thousand Meters](https://tenthousandmeters.com/blog/sqlite-concurrent-writes-and-database-is-locked-errors/)
- [Managing SignalR ConnectionIds (or why you shouldn't) — consultwithgriff.com](https://consultwithgriff.com/signalr-connection-ids)
- [SignalR use old cookie after user logged in — GitHub Issue #39180](https://github.com/dotnet/aspnetcore/issues/39180)
- Codebase analysis: `Controllers/AdminController.cs` (GetMonitoringProgress, ForceCloseAssessment, ResetAssessment, ForceCloseAll), `Controllers/CMPController.cs` (SubmitExam, SaveAnswer, SavePackageAnswer, AssessmentSessions lifecycle)

---

*Pitfalls research for: Portal HC KPB v4.2 — Adding SignalR real-time monitoring to existing assessment system*
*Specific to: Brownfield integration, cookie auth, SQLite, jQuery clients, HC monitoring + worker exam pages*
*Date: 2026-03-13 | Confidence: HIGH*
