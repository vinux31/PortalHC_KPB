# Stack Research

**Domain:** Real-time assessment monitoring — SignalR additions to existing ASP.NET Core 8 MVC app
**Researched:** 2026-03-13
**Confidence:** HIGH (verified via official Microsoft docs and NuGet/jsDelivr)

---

## Context: What Already Exists (Do Not Re-add)

The following are already in `HcPortal.csproj` and `Program.cs` — do not reinstall or reconfigure:

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.0
- `Microsoft.EntityFrameworkCore.Sqlite` / `SqlServer` 8.0.0
- `ClosedXML`, `QuestPDF`, Bootstrap 5, jQuery, Chart.js
- `builder.Services.AddControllersWithViews()`, `UseAuthentication()`, `UseAuthorization()`, `UseSession()`, `UseRouting()`

---

## New Stack Additions

### Core Technologies (New Only)

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| SignalR server | built-in (net8.0) | Hub, server-push infrastructure | Included in ASP.NET Core shared framework — zero NuGet install needed |
| `@microsoft/signalr` JS client | 8.0.x (pin to match server) | Browser-side SignalR connection | Official JS client, ships as standalone file, no npm build pipeline needed |

**Critical version note:** The JS client version should match the server .NET version. Since the server is net8.0, use `@microsoft/signalr@8.x` not the latest 10.x. Mismatched major versions can cause protocol negotiation failures. As of 2026-03, `@microsoft/signalr@8.0.7` is the latest 8.x release on the 8.x branch.

### Supporting Libraries (New Only)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| None additional | — | — | SignalR server and JS client are sufficient for this use case |

---

## Installation

### Server Side — No NuGet package needed

SignalR is part of the `Microsoft.NET.Sdk.Web` shared framework in ASP.NET Core 8. Nothing to add to `HcPortal.csproj`.

Verify by checking that `Microsoft.AspNetCore.SignalR` namespace resolves without any package reference — it will.

### JS Client — Download to wwwroot

Use LibMan (available via dotnet tool) or direct download:

```bash
# Option A: LibMan CLI (recommended for production — local file, no external CDN dependency)
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman install @microsoft/signalr@8.0.7 -p unpkg -d wwwroot/js/signalr --files dist/browser/signalr.min.js
```

Target path: `wwwroot/js/signalr/signalr.min.js`

Reference in views:
```html
<script src="~/js/signalr/signalr.min.js"></script>
```

### Program.cs Changes — Two Lines Only

```csharp
// In builder section (after AddControllersWithViews on line 13):
builder.Services.AddSignalR();

// In app section (after MapControllerRoute on line 158, before app.Run()):
app.MapHub<HcPortal.Hubs.AssessmentHub>("/hubs/assessment");
```

**Placement in existing Program.cs:**
- `builder.Services.AddSignalR()` — add after line 13 (`AddControllersWithViews()`)
- `app.MapHub<...>()` — add after line 158 (`app.MapControllerRoute(...)`)
- No changes to existing middleware order required — SignalR works with current `UseRouting()` / `UseAuthentication()` / `UseAuthorization()` pipeline as-is

---

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| SignalR (built-in) | Long polling (current 10s) | Current polling is fine if sub-10s latency is not required; only replace if HC needs instant feedback |
| SignalR (built-in) | Raw WebSockets API | SignalR wraps WebSockets + SSE + long-polling fallback — no reason to use raw WebSockets |
| SignalR (built-in) | Server-Sent Events (SSE) | SSE is unidirectional server→client only; SignalR needed for bidirectional worker↔HC events |
| Local file in wwwroot | CDN for JS client | CDN is acceptable for dev; production should use local file to avoid external dependency |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `@microsoft/signalr@10.x` | Major version mismatch with net8.0 server — hub protocol v2 differences can cause negotiation failures | `@microsoft/signalr@8.x` (matches server runtime) |
| `Microsoft.AspNetCore.SignalR.Client` NuGet | Server-to-server .NET SignalR client — only needed if another .NET service calls the hub | Not needed; browser JS client handles all connections |
| `@aspnet/signalr` npm package | Deprecated legacy package (pre-2.1 era) | `@microsoft/signalr` |
| `@microsoft/signalr-protocol-msgpack` | Binary MessagePack protocol — adds dependency complexity for no benefit at this scale | JSON protocol (SignalR default) is sufficient for assessment events |
| Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) | Only needed for multi-server scale-out | Single-server deployment — not needed |

---

## Stack Patterns

**For Hub authorization (assessment pages require login):**
```csharp
[Authorize]
public class AssessmentHub : Hub { ... }
```
Works with existing Identity cookie auth — no extra configuration. SignalR uses the same auth pipeline as MVC controllers.

**For group-scoped messaging (HC monitors one exam, not all exams):**
```csharp
// On connect, join exam group:
await Groups.AddToGroupAsync(Context.ConnectionId, $"exam-{examId}");

// Send to specific exam group only:
await Clients.Group($"exam-{examId}").SendAsync("WorkerProgress", data);
```
Do not use `Clients.All` — exam events are scoped per-exam.

**For client reconnection:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/assessment")
    .withAutomaticReconnect()   // handles transient disconnects
    .build();
```
Always use `.withAutomaticReconnect()` — exam sessions can be 60+ minutes long.

---

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| SignalR server (net8.0 built-in) | `@microsoft/signalr@8.x` | Matching major version ensures hub protocol compatibility — HIGH confidence |
| SignalR server (net8.0 built-in) | `@microsoft/signalr@7.x` | Works but not ideal — pin to 8.x |
| `@microsoft/signalr@10.x` | net8.0 server | Avoid — v10 JS client may negotiate hub protocol version not fully compatible with 8.0 server |
| ASP.NET Core SignalR | SQLite / Entity Framework Core | No conflict — SignalR has no persistence layer; SQLite and EF Core continue unchanged |
| ASP.NET Core SignalR | Session middleware (`UseSession`) | No conflict — SignalR connections are independent of ASP.NET session; existing `UseSession()` untouched |
| ASP.NET Core SignalR | Identity cookie auth | Compatible — `[Authorize]` on Hub class works automatically |

---

## Sources

- [ASP.NET Core SignalR Introduction (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-8.0) — HIGH confidence, official, verified 2026-03-13
- [Get Started with ASP.NET Core SignalR (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/tutorials/signalr?view=aspnetcore-8.0) — HIGH confidence, provides exact Program.cs setup and LibMan commands, updated 2026-01-28
- [NuGet: Microsoft.AspNetCore.SignalR.Client](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client) — HIGH confidence, version history confirmed 2026-03-13
- [jsDelivr: @microsoft/signalr](https://www.jsdelivr.com/package/npm/@microsoft/signalr) — MEDIUM confidence, confirmed 8.x and 10.x branches available

---

*Stack research for: SignalR real-time assessment — PortalHC KPB v4.2*
*Researched: 2026-03-13*
