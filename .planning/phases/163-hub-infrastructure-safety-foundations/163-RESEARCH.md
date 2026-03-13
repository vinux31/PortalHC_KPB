# Phase 163: Hub Infrastructure & Safety Foundations - Research

**Researched:** 2026-03-13
**Domain:** ASP.NET Core SignalR, SQLite WAL mode, JS client reconnect handling, concurrency guards
**Confidence:** HIGH

## Summary

This phase wires up the technical prerequisites for all real-time assessment features: a SignalR hub registered and protected by cookie auth, SQLite WAL mode to survive concurrent writes, reconnect-safe group membership via a shared JS module, and status-guarded `ExecuteUpdateAsync` calls on the four write actions.

The project currently uses SQL Server in development (`appsettings.Development.json`) but the **published production build uses SQLite** (`publish/appsettings.json` — `Data Source=HcPortal.db`). WAL mode is a SQLite-specific concern and must be applied conditionally or always (safe to run on SQLite, no-op concern for SQL Server). Program.cs already uses `UseSqlServer` hardcoded; the published binary picks up SQLite from `publish/appsettings.json` at runtime because it contains the literal connection string. The WAL PRAGMA must execute only when the provider is SQLite.

No SignalR code exists anywhere in the codebase. This is a greenfield SignalR setup on a mature cookie-auth ASP.NET Core 8 Identity app. The `@microsoft/signalr` JS client must be vendored into `wwwroot/lib/signalr/` to match the project's existing no-npm convention (Bootstrap, jQuery are all vendored under `wwwroot/lib/`).

**Primary recommendation:** Follow standard ASP.NET Core 8 SignalR setup — `AddSignalR()` + `MapHub<AssessmentHub>("/hubs/assessment")` + `[Authorize]` on the hub class — then add the two targeted tweaks: `OnRedirectToLogin` for 401 on `/hubs/` paths and a single WAL PRAGMA guarded by provider name check.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**SignalR Group Strategy**
- Per-batch group naming: `batch-{batchId}`
- Workers join group on exam page load (StartExam)
- HC joins group on monitoring detail page load (AssessmentMonitoringDetail)
- Individual worker targeting via `Clients.User(userId)` for AkhiriUjian
- Batch-wide broadcast via group for AkhiriSemua
- No `Dictionary<userId, connectionId>` — already decided in prior sessions

**Race Condition Guards**
- Guard all write actions: AkhiriUjian, SubmitExam, Reset, and auto-save (SaveAnswer)
- Use WHERE clause filter pattern: `ExecuteUpdateAsync` with `WHERE Status == "InProgress"` — 0 rows affected = no-op
- First write wins, silent skip — no error shown to the "loser"; they see updated state on next refresh/push
- DB write always before SignalR push (carried forward from prior decisions)

**Reconnect UX**
- Toast notification on disconnect: "Koneksi terputus..."
- Toast notification on reconnect: "Koneksi pulih"
- Retry with backoff: 0s, 2s, 5s, 10s, 30s — give up after ~47 seconds
- On permanent failure: toast with "Koneksi gagal" + "Muat Ulang" reload button (no auto-reload)
- On reconnect success: rejoin batch group only — polling fallback covers missed events (polling stays active through Phase 164)
- On 401 during reconnect: show "Sesi login habis — silakan login ulang" with login link

**Hub Method Naming**
- Hub class: `AssessmentHub` at `/hubs/assessment`
- Server methods (C#): PascalCase — `JoinBatch`, `LeaveBatch`
- Client events (JS): camelCase — `examClosed`, `sessionReset`, `progressUpdate`
- Phase 163 scope: only `JoinBatch` + `LeaveBatch` methods — event methods added in Phase 164-165

**JS Client Placement**
- Load SignalR JS only on assessment pages (StartExam, AssessmentMonitoringDetail) via `@section Scripts`
- Shared module: `wwwroot/js/assessment-hub.js` with connect/reconnect/group-join/toast logic
- SignalR library vendored in `wwwroot/lib/signalr/signalr.min.js` (matches existing wwwroot/lib pattern)
- Simple custom toast function within assessment-hub.js — no Bootstrap Toast dependency

**Auth 401 Configuration**
- Cookie auth returns 401 (not 302 redirect) only for `/hubs/` path prefix — no change to other AJAX calls
- Implemented in `ConfigureApplicationCookie` OnRedirectToLogin event

**WAL Mode Activation**
- Enable WAL via `PRAGMA journal_mode=WAL;` in Program.cs after EnsureCreatedAsync
- Verify WAL is active by querying `PRAGMA journal_mode;` and logging result on startup

### Claude's Discretion
- Exact toast styling and positioning
- SignalR retry policy implementation details
- WAL PRAGMA execution method (raw SQL via DbContext or separate connection)
- Assessment-hub.js internal structure and API surface

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| INFRA-01 | SignalR Hub class registered with `AddSignalR()` and `MapHub` in Program.cs | Standard ASP.NET Core 8 pattern — verified in official docs |
| INFRA-02 | `@microsoft/signalr@8.x` JS client library installed in wwwroot | Vendor from npm dist or CDN copy into `wwwroot/lib/signalr/` |
| INFRA-03 | Cookie auth configured to return 401 (not 302) on SignalR negotiate endpoint | `OnRedirectToLogin` event in `ConfigureApplicationCookie` — path check on `/hubs/` |
| INFRA-04 | SQLite WAL mode enabled on application startup | `PRAGMA journal_mode=WAL;` via `Database.ExecuteSqlRawAsync` after Migrate(), guarded by provider check |
| INFRA-05 | Client-side reconnect handling re-joins SignalR groups after connection restore | `withAutomaticReconnect([0,2000,5000,10000,30000])` + `onreconnected` callback calls `JoinBatch` |
</phase_requirements>

---

## Standard Stack

### Core (Server)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.AspNetCore.SignalR | 8.0 (built-in, no separate package) | Hub infrastructure, group management, `Clients.User()` | Ships in ASP.NET Core Web SDK — no extra NuGet needed |

### Core (Client)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| @microsoft/signalr | 8.x (latest 8.x from CDN) | HubConnectionBuilder, reconnect, groups | Official Microsoft JS client for ASP.NET Core SignalR 8 |

### No New NuGet Packages Required
ASP.NET Core SignalR is built into the `Microsoft.NET.Sdk.Web` SDK for .NET 8. No additional package reference is needed. The existing `Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.0` and EF Core packages already in `HcPortal.csproj` are sufficient.

**JS client installation (vendored — no npm):**
```
Download signalr.min.js from:
https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js
Place at: wwwroot/lib/signalr/signalr.min.js
```

## Architecture Patterns

### Recommended Project Structure (additions only)
```
Hubs/
└── AssessmentHub.cs          # Hub class with JoinBatch, LeaveBatch
wwwroot/js/
└── assessment-hub.js         # Shared JS module: connect, reconnect, toast, group join
wwwroot/lib/signalr/
└── signalr.min.js            # Vendored @microsoft/signalr@8.x browser build
```

### Pattern 1: Hub Registration in Program.cs

**What:** `AddSignalR()` in the service collection, `MapHub<T>()` in the endpoint routing block.

**When to use:** Always for any SignalR hub in ASP.NET Core 8.

**Important:** `MapHub` must come **after** `UseAuthentication()` and `UseAuthorization()`. The existing Program.cs already has the correct middleware order — just add `MapHub` alongside `MapControllerRoute`.

```csharp
// In builder.Services section:
builder.Services.AddSignalR();

// In app routing section (after UseAuthentication, UseAuthorization):
app.MapHub<AssessmentHub>("/hubs/assessment");
```

### Pattern 2: Hub Class with [Authorize]

**What:** Hub class decorated with `[Authorize]` so unauthenticated WebSocket upgrades are rejected at the hub level, not just at negotiate.

```csharp
// Source: Official ASP.NET Core 8 SignalR docs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class AssessmentHub : Hub
{
    public async Task JoinBatch(int batchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"batch-{batchId}");
    }

    public async Task LeaveBatch(int batchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"batch-{batchId}");
    }
}
```

**Note:** No DB writes inside Hub methods — per locked decision. Group membership is handled by SignalR's in-memory group store; no persistence needed here.

### Pattern 3: Cookie Auth 401 for /hubs/ Paths (INFRA-03)

**What:** By default in ASP.NET Core 8, unauthenticated requests trigger a 302 redirect to `/Account/Login`. For SignalR's negotiate endpoint this breaks the JS client (it follows the redirect and gets HTML, not JSON). The fix is to intercept `OnRedirectToLogin` and return 401 when the path starts with `/hubs/`.

```csharp
// In ConfigureApplicationCookie (Program.cs line 88):
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    // ADD THIS:
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
```

**Why only `/hubs/`:** All other AJAX calls in the app already handle 302 gracefully (browser follows redirect to login page). Changing global behavior would break existing flows. Scoping to `/hubs/` is safe and surgical.

### Pattern 4: SQLite WAL Mode (INFRA-04)

**What:** SQLite default journal mode is DELETE (rollback journal). Under concurrent reads + writes (polling AJAX + HC write actions firing simultaneously), WAL mode eliminates write lock contention that causes "database is locked" errors.

**Important constraint discovered:** Program.cs calls `context.Database.Migrate()` (not `EnsureCreatedAsync`). The WAL PRAGMA must run **after** `Migrate()` succeeds. The PRAGMA is idempotent — safe to run on every startup.

**Provider check required:** The codebase uses `UseSqlServer` in Program.cs but the publish directory's `appsettings.json` points to a SQLite file. At runtime the provider is determined by the connection string. Running `PRAGMA journal_mode=WAL;` against SQL Server would throw. Guard with provider name check:

```csharp
// After context.Database.Migrate(); in Program.cs startup scope:
if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
{
    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    var journalMode = await context.Database
        .SqlQueryRaw<string>("PRAGMA journal_mode;")
        .FirstOrDefaultAsync();
    logger.LogInformation("SQLite journal mode: {Mode}", journalMode);
}
```

**Alternative (Claude's discretion):** Use a raw `SqliteConnection` to avoid any EF overhead. But `ExecuteSqlRawAsync` on the existing `context.Database` is simpler and avoids a separate connection string parse.

### Pattern 5: SignalR JS Client — Reconnect + Group Rejoin (INFRA-05)

**What:** `HubConnectionBuilder.withAutomaticReconnect()` accepts a custom retry delay array. After reconnect, the `onreconnected` callback re-invokes `JoinBatch` so the connection is back in the correct group.

**Key insight:** SignalR groups are per-connection, not per-user. When a client reconnects, it gets a new `connectionId` and is **not** automatically in any group. The `onreconnected` callback is the only place to re-join.

```javascript
// wwwroot/js/assessment-hub.js (skeleton)
// Source: ASP.NET Core SignalR JS client docs

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/assessment")
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build();

connection.onreconnecting(() => {
    showToast("Koneksi terputus...");
});

connection.onreconnected(async () => {
    showToast("Koneksi pulih");
    // Re-join group — batchId must be available in page scope
    if (typeof currentBatchId !== 'undefined') {
        await connection.invoke("JoinBatch", currentBatchId);
    }
});

connection.onclose(async (error) => {
    // Permanent failure after all retries exhausted
    if (error?.message?.includes("401") || (error && error.statusCode === 401)) {
        showToast("Sesi login habis — silakan login ulang", "/Account/Login");
    } else {
        showToastWithReload("Koneksi gagal. Muat Ulang untuk mencoba lagi.");
    }
});

async function startConnection() {
    try {
        await connection.start();
    } catch (err) {
        // Initial connection failed — handled by onclose
    }
}
```

**`withAutomaticReconnect([0, 2000, 5000, 10000, 30000])`:** Retries at 0s, 2s, 5s, 10s, 30s — total ~47s before giving up. After the array is exhausted, `onclose` fires. This matches the locked decision exactly.

### Pattern 6: Status-Guarded ExecuteUpdateAsync (Race Condition Guards)

**What:** Replace the current read-then-write pattern in `AkhiriUjian` and `SubmitExam` with a single atomic `ExecuteUpdateAsync` that filters on `Status == "InProgress"`. Check `rowsAffected == 0` to detect the race.

**Current state of AkhiriUjian:** Reads session, checks `isInProgress` manually, then calls `GradeFromSavedAnswers(session)` + `SaveChangesAsync()`. This is a classic TOCTOU race — two simultaneous callers both pass the check before either writes.

**Current state of SubmitExam:** Reads assessment, checks `Status == "Completed"` as guard — same TOCTOU vulnerability.

**Correct pattern (first write wins):**
```csharp
// AkhiriUjian — status-guarded update
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == id
             && s.StartedAt != null
             && s.CompletedAt == null
             && s.Score == null
             && s.Status != "Cancelled"
             && s.Status != "Abandoned")
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Status, "Completed")
        .SetProperty(r => r.CompletedAt, DateTime.UtcNow)
        // ... score, etc.
    );

if (rowsAffected == 0)
{
    // Race lost — already closed by worker or another HC action. Silent skip.
    return RedirectToAction("AssessmentMonitoringDetail", ...);
}
```

**Note on GradeFromSavedAnswers:** This helper reads saved answers and computes score. It cannot be folded into a single `ExecuteUpdateAsync` because the score depends on a computed value from related records. The guard pattern should: (1) acquire the lock with a status-guarded `ExecuteUpdateAsync` that marks status as "Completed" or sets a sentinel, OR (2) use optimistic concurrency via a RowVersion/timestamp. Simpler approach: guard the `SaveChangesAsync` by checking if the session is still in the expected state immediately before saving (re-fetch after grade computation). The **first-write-wins** intent means: re-fetch, verify still InProgress, grade, save. If re-fetch finds already Completed → return no-op.

### Anti-Patterns to Avoid
- **Storing connectionId in a dictionary:** Connection IDs change on every reconnect. Use `Clients.User(userId)` (routed by ASP.NET Core Identity's `NameIdentifier` claim) or named groups.
- **DB writes inside Hub methods:** Hub methods run on the SignalR dispatcher; blocking DB calls increase connection backlog. Pass the batchId in `JoinBatch` for Groups only.
- **Running PRAGMA on SQL Server:** `PRAGMA` is SQLite-only. Guard with provider name check.
- **MapHub before UseAuthentication:** The hub's `[Authorize]` attribute won't fire correctly. The existing middleware order in Program.cs is correct — just append `MapHub` after `MapControllerRoute`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| WebSocket transport negotiation | Custom WebSocket upgrade handler | SignalR's built-in transport negotiation | Handles WebSocket, Server-Sent Events, long-polling fallback transparently |
| Reconnect with backoff | Custom setTimeout loop | `withAutomaticReconnect([...])` | Built-in, handles connection state machine correctly |
| Per-user message routing | `Dictionary<userId, connectionId>` | `Clients.User(userId)` | Connection IDs change on reconnect; Identity maps user ID to all current connections |
| Group management persistence | Database table for group membership | SignalR in-memory groups | Groups are intentionally ephemeral; clients rejoin on page load |

## Common Pitfalls

### Pitfall 1: 302 Redirect on Negotiate Breaks JS Client
**What goes wrong:** Without the `OnRedirectToLogin` override, unauthenticated negotiate requests get a 302 to `/Account/Login`. The JS client follows it and receives HTML, causing a connection error rather than a clean 401.
**How to avoid:** Add `OnRedirectToLogin` event handler scoped to `/hubs/` path prefix in `ConfigureApplicationCookie`.
**Warning signs:** DevTools Network tab shows negotiate returning 302 followed by a 200 with `text/html` content-type.

### Pitfall 2: Group Membership Lost on Reconnect
**What goes wrong:** `withAutomaticReconnect` re-establishes the WebSocket but the new connection has no group memberships. Workers stop receiving push events silently.
**How to avoid:** Always call `JoinBatch(batchId)` inside the `onreconnected` callback.
**Warning signs:** Polling still works but SignalR push events stop after a browser tab goes offline then back online.

### Pitfall 3: WAL PRAGMA on Wrong Provider
**What goes wrong:** The same `Program.cs` is used for both SQL Server (dev) and SQLite (publish). Running `PRAGMA journal_mode=WAL;` against SQL Server throws `SqlException: Incorrect syntax near 'PRAGMA'`.
**How to avoid:** Guard with `context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite"`.
**Warning signs:** App crashes on startup in SQL Server environment with syntax error on PRAGMA.

### Pitfall 4: TOCTOU Race in AkhiriUjian / SubmitExam
**What goes wrong:** Worker submits exam at the same moment HC clicks Akhiri Ujian. Both read session as InProgress, both proceed to grade — the second write overwrites the score from the first.
**How to avoid:** Use `ExecuteUpdateAsync` with the InProgress condition in the WHERE clause. Check `rowsAffected == 0` for silent skip. For `GradeFromSavedAnswers` (which requires read-compute-write), re-fetch session after grade computation and check status before `SaveChangesAsync`; if already Completed, abort.
**Warning signs:** Monitoring detail shows unexpected score changes, or two completion timestamps in logs.

### Pitfall 5: IIS WebSocket Protocol Feature Not Enabled
**What goes wrong:** SignalR falls back to long-polling, which works but is less efficient and may drop at corporate proxy timeouts.
**How to avoid:** This is a deployment concern (noted in STATE.md). During Phase 163 UAT, verify negotiate response in DevTools — if transport shows `longPolling` instead of `webSockets`, enable the IIS WebSocket Protocol Windows Feature.
**Warning signs:** Negotiate response JSON shows `availableTransports` without `WebSockets` or connection uses `long_polling` transport.

## Code Examples

### Complete Program.cs additions

```csharp
// 1. Service registration (alongside AddControllersWithViews):
builder.Services.AddSignalR();

// 2. Cookie auth with 401 for /hubs/ (replace existing ConfigureApplicationCookie block):
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// 3. WAL mode (inside the startup scope, after context.Database.Migrate()):
if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
{
    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    var mode = (await context.Database
        .SqlQueryRaw<string>("PRAGMA journal_mode;")
        .ToListAsync())
        .FirstOrDefault();
    logger.LogInformation("SQLite journal mode active: {Mode}", mode);
}

// 4. Endpoint mapping (alongside MapControllerRoute):
app.MapHub<AssessmentHub>("/hubs/assessment");
```

### AssessmentHub.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HcPortal.Hubs;

[Authorize]
public class AssessmentHub : Hub
{
    public async Task JoinBatch(int batchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"batch-{batchId}");
    }

    public async Task LeaveBatch(int batchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"batch-{batchId}");
    }
}
```

### assessment-hub.js skeleton

```javascript
// wwwroot/js/assessment-hub.js
// Loaded via @section Scripts on StartExam and AssessmentMonitoringDetail pages.
// Calling page must set window.assessmentBatchId before loading this script.

(function () {
    'use strict';

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/assessment')
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .build();

    function showToast(message, linkUrl, linkText) {
        // Custom toast — no Bootstrap Toast dependency
        // Positioning and styling at Claude's discretion
        var toast = document.createElement('div');
        toast.className = 'assessment-toast';
        toast.textContent = message;
        if (linkUrl) {
            var a = document.createElement('a');
            a.href = linkUrl;
            a.textContent = linkText || 'Klik di sini';
            toast.appendChild(a);
        }
        document.body.appendChild(toast);
        setTimeout(function () { toast.remove(); }, 5000);
    }

    function showPersistentToast(message, buttonText, buttonAction) {
        var toast = document.createElement('div');
        toast.className = 'assessment-toast assessment-toast--persistent';
        toast.textContent = message;
        var btn = document.createElement('button');
        btn.textContent = buttonText;
        btn.onclick = buttonAction;
        toast.appendChild(btn);
        document.body.appendChild(toast);
    }

    connection.onreconnecting(function () {
        showToast('Koneksi terputus...');
    });

    connection.onreconnected(function () {
        showToast('Koneksi pulih');
        var batchId = window.assessmentBatchId;
        if (batchId) {
            connection.invoke('JoinBatch', batchId).catch(function (err) {
                console.error('JoinBatch after reconnect failed:', err);
            });
        }
    });

    connection.onclose(function (error) {
        var is401 = error && (
            (error.message && error.message.indexOf('401') !== -1) ||
            error.statusCode === 401
        );
        if (is401) {
            showToast('Sesi login habis — silakan login ulang', '/Account/Login', 'Login ulang');
        } else {
            showPersistentToast('Koneksi gagal.', 'Muat Ulang', function () {
                window.location.reload();
            });
        }
    });

    async function startHub() {
        try {
            await connection.start();
            var batchId = window.assessmentBatchId;
            if (batchId) {
                await connection.invoke('JoinBatch', batchId);
            }
        } catch (err) {
            // onclose will fire after all retries
        }
    }

    startHub();

    // Expose connection for Phase 164/165 event handlers
    window.assessmentHub = connection;
})();
```

### Page-level usage (StartExam.cshtml)

```html
@section Scripts {
    <script src="~/lib/signalr/signalr.min.js"></script>
    <script>
        window.assessmentBatchId = @Model.BatchId;
    </script>
    <script src="~/js/assessment-hub.js"></script>
    <!-- existing exam scripts below -->
}
```

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| No real-time — polling only | SignalR WebSocket with long-poll fallback | Enables instant push for Phase 164-165 |
| Read-then-write status checks | `ExecuteUpdateAsync` with WHERE guard | Eliminates TOCTOU race on concurrent close |
| SQLite default DELETE journal | WAL mode | Allows concurrent reads during writes |

## Open Questions

1. **`GradeFromSavedAnswers` grading race**
   - What we know: `GradeFromSavedAnswers` is a helper that reads saved answers and sets score properties on the session object in memory, then the caller calls `SaveChangesAsync`.
   - What's unclear: The safest atomic pattern when score computation requires a read of related rows (can't fold into a single `ExecuteUpdateAsync`).
   - Recommendation: Re-fetch session inside the status guard scope, grade, then check `rowsAffected` from a conditional `ExecuteUpdateAsync` that only sets `Status=Completed WHERE Status != Completed`. If 0 rows: abort (race lost). This requires minor refactor of `GradeFromSavedAnswers` flow.

2. **`Clients.User(userId)` identifier alignment**
   - What we know: ASP.NET Core Identity's default `IUserIdProvider` uses `ClaimTypes.NameIdentifier` which maps to `AspNetUsers.Id` (GUID string). The codebase already uses `User.FindFirstValue(ClaimTypes.NameIdentifier)` for userId lookups.
   - What's unclear: Whether `session.UserId` (the value stored in AssessmentSession) matches what `Clients.User()` uses as the key.
   - Recommendation: Verify `session.UserId` is the Identity `Id` GUID (not email or NIP). From code review, `UserId` is set from `_userManager.GetUserAsync(User).Id` — this is the GUID, which matches Identity's `NameIdentifier` claim. No custom `IUserIdProvider` needed.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected — no test project, no pytest/jest/vitest config |
| Config file | None — Wave 0 gap |
| Quick run command | Manual browser + DevTools verification |
| Full suite command | Manual UAT per success criteria |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INFRA-01 | `/hubs/assessment/negotiate` returns 200 JSON (not 302 HTML) when logged in | smoke | Manual: DevTools Network tab, check negotiate response | ❌ Wave 0 |
| INFRA-02 | `signalr.min.js` loads without 404 | smoke | Manual: DevTools Network tab | ❌ Wave 0 |
| INFRA-03 | Negotiate returns 401 (not 302) when not logged in | smoke | Manual: open incognito, check negotiate | ❌ Wave 0 |
| INFRA-04 | `PRAGMA journal_mode;` returns `wal` | smoke | Manual: SQLite CLI or startup log | ❌ Wave 0 |
| INFRA-05 | DevTools offline/online toggle causes automatic group rejoin | smoke | Manual: DevTools Application > Service Workers offline toggle | ❌ Wave 0 |

**Note:** This project has no automated test infrastructure. All INFRA requirements are infrastructure smoke tests best verified manually in browser DevTools. No automated test framework setup is needed for Phase 163. The success criteria defined in the phase objective serve as the acceptance checklist.

### Sampling Rate
- **Per task commit:** Manual smoke check per task (see success criteria)
- **Per wave merge:** Full success criteria checklist (5 items)
- **Phase gate:** All 5 success criteria TRUE before `/gsd:verify-work`

### Wave 0 Gaps
None requiring automated setup — manual verification sufficient for all INFRA requirements. No test framework install needed.

## Sources

### Primary (HIGH confidence)
- [ASP.NET Core SignalR Authentication and Authorization (.NET 8)](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-8.0) — Cookie auth 401 pattern, `[Authorize]` on hub, `Clients.User()` behavior
- [ASP.NET Core SignalR JavaScript Client](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-9.0) — `withAutomaticReconnect`, `onreconnected`, `onclose`
- Project codebase: `Program.cs`, `HcPortal.csproj`, `appsettings.json`, `publish/appsettings.json` — confirmed SQLite in production, SQL Server in dev, no existing SignalR

### Secondary (MEDIUM confidence)
- [jsDelivr @microsoft/signalr CDN](https://www.jsdelivr.com/package/npm/@microsoft/signalr) — Vendored file source for `signalr.min.js`
- [cdnjs microsoft-signalr](https://cdnjs.com/libraries/microsoft-signalr) — Alternative CDN source

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — built-in to ASP.NET Core 8 Web SDK, no new NuGet needed
- Architecture: HIGH — standard hub setup verified against official docs; WAL guard pattern derived from codebase inspection
- Pitfalls: HIGH — 302-vs-401 pitfall confirmed in official docs; WAL provider guard confirmed from codebase analysis; group-on-reconnect is documented SignalR behavior
- Race condition pattern: MEDIUM — `ExecuteUpdateAsync` pattern is used elsewhere in codebase; GradeFromSavedAnswers refactor approach is reasoned but not officially prescribed

**Research date:** 2026-03-13
**Valid until:** 2026-09-13 (stable ASP.NET Core 8 APIs)
