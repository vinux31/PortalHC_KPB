# Project Research Summary

**Project:** PortalHC KPB v4.2 — SignalR Real-Time Assessment Monitoring
**Domain:** Brownfield SignalR integration into existing ASP.NET Core 8 MVC + SQLite + Identity app
**Researched:** 2026-03-13
**Confidence:** HIGH

## Executive Summary

PortalHC KPB v4.2 adds real-time push notifications to an already-complete exam monitoring system. The current system works via 10-second polling: workers poll for Reset/ForceClose status changes, and the HC monitoring page polls for worker progress updates. The gap is latency — a ForceClose event can take up to 10 seconds to reach the worker, who keeps answering after their score is locked at zero. SignalR, built into the ASP.NET Core 8 shared framework, is the correct and minimal-cost fix: zero new NuGet packages on the server, one JS client file downloaded via LibMan, two Program.cs lines, one new Hub class, and targeted modifications to two controllers and two views.

The recommended approach is strictly additive integration. All DB writes continue through HTTP controller actions (SubmitExam, ForceClose, SaveAnswer). SignalR Hub methods handle group join/leave only and never write to the database. After each DB write, controllers inject `IHubContext<AssessmentHub>` and push an event to a scoped named group: `exam-{sessionId}` for per-worker events, and `monitor-{title}-{category}-{date}` for HC monitoring events. The existing polling endpoints (`CheckExamStatus`, `GetMonitoringProgress`) are kept as fallback throughout the transition and removed in a follow-on cleanup phase after UAT confirms SignalR is stable.

The top risk is correctness under concurrency, not infrastructure complexity. SignalR does not replace the SQLite database as the state source of truth, but it does shrink the timing window for the existing SubmitExam/ForceClose race condition from 10 seconds to sub-second. That race must be closed with status-guarded `ExecuteUpdateAsync` calls before the SignalR push is wired in. A second reliability risk is that SignalR group membership is lost on reconnect and must be explicitly restored via `onreconnected` callbacks — failing to implement this produces a silent failure where the HC monitoring page reconnects but never receives further worker updates.

## Key Findings

### Recommended Stack

The existing stack (ASP.NET Core 8, Identity, EF Core, SQLite, jQuery, Bootstrap 5) needs only two additions: the SignalR server infrastructure (already in the `Microsoft.NET.Sdk.Web` shared framework — no NuGet install) and the SignalR JS client (`@microsoft/signalr@8.0.7`, downloaded to `wwwroot` via LibMan). The JS client version must match the server .NET major version; `@microsoft/signalr@10.x` must not be used against a net8.0 server due to hub protocol negotiation differences. No Redis backplane is needed for this single-server intranet deployment.

**Core technologies:**
- SignalR server (built-in, net8.0): Hub infrastructure and server-push transport — zero NuGet cost, included in shared framework
- `@microsoft/signalr@8.0.7` JS client: browser-side hub connection — pinned to match server major version, installed via LibMan to `wwwroot`
- `IHubContext<AssessmentHub>`: controller-to-hub push mechanism — correct pattern for server-initiated events from HTTP actions; never call Hub methods directly from controllers

### Expected Features

**Must have (table stakes — v4.2 launch):**
- HC Reset pushes instantly to targeted worker — replaces 10s polling for this time-sensitive event
- HC ForceClose pushes instantly to targeted worker — worker must stop answering the moment the session is locked
- Worker submission event pushes to HC monitor — HC sees "Completed" the moment worker submits, not up to 10s later
- Worker answer progress (answered count) pushes to HC monitor — near-real-time progress without polling
- Connection status indicator ("Live" / "Reconnecting...") on both pages — users need visibility into SignalR state

**Should have (P2, add after v4.2 validation):**
- ForceCloseAll broadcasts to all workers in assessment group simultaneously — eliminates stragglers continuing after session is ended
- CloseEarly push to all active workers in the assessment group
- Reduce poll fallback interval from 10s to 30s once SignalR push is confirmed stable

**Defer (v2+):**
- Explicit graceful fallback toggle (disable SignalR, re-enable polling) for unreliable network environments
- Multi-assessment concurrent monitoring across browser tabs

**Anti-features (do not build):**
- Per-keystroke answer content push to HC — privacy risk and high message volume
- Two-way chat during exam — scope creep and exam integrity risk
- SignalR replacing the 30s answer auto-save XHR — adds server complexity and loses existing debounce logic

### Architecture Approach

The architecture is strictly additive: one new file (`Hubs/AssessmentHub.cs`), two modified controllers (inject `IHubContext`), two modified views (replace `setInterval` polling with hub event handlers), one new JS file in `wwwroot`, and two lines in `Program.cs`. No DB migrations are needed. The Hub uses two named group patterns — `exam-{sessionId}` (worker joins on StartExam page load; receives HC-to-Worker events) and `monitor-{title}-{category}-{date}` (HC joins on AssessmentMonitoringDetail page load; receives Worker-to-HC events). The DB remains the state source of truth; hub pushes are notifications only, and the DB write always happens before the push.

**Major components:**
1. `Hubs/AssessmentHub.cs` (NEW) — defines `JoinExamGroup(sessionId)` and `JoinMonitorGroup(title, category, date)` hub methods; `[Authorize]` on class; no DB writes inside Hub methods
2. `IHubContext<AssessmentHub>` injected into `AdminController` and `CMPController` — pushes `examReset`, `examForceClosed` (Admin); `progressUpdate`, `examSubmitted` (CMP) after existing DB writes
3. JS client in `StartExam.cshtml` — connects hub, joins exam group, handles `examReset` and `examForceClosed` events, re-joins group on `onreconnected`
4. JS client in `AssessmentMonitoringDetail.cshtml` — connects hub, joins monitor group, handles `progressUpdate` event calling existing `updateRow()` / `updateSummary()` functions, re-joins group on `onreconnected`
5. `Program.cs` — `builder.Services.AddSignalR()` after `AddControllersWithViews()`; `app.MapHub<AssessmentHub>("/hubs/assessment")` after `MapControllerRoute`

**Build order (dependency-constrained):**
1. Install JS client to `wwwroot` (prerequisite for view JS changes)
2. Create `AssessmentHub.cs` (prerequisite for Program.cs `MapHub<>` reference)
3. Update `Program.cs` (`AddSignalR()` + `MapHub<>()`)
4. Inject `IHubContext` into AdminController + push from Reset/ForceClose actions (parallel with step 5)
5. Inject `IHubContext` into CMPController + push from SaveAnswer/SubmitExam actions (parallel with step 4)
6. Update `StartExam.cshtml` JS — replace polling, add hub handlers (parallel with step 7)
7. Update `AssessmentMonitoringDetail.cshtml` JS — replace polling, add hub handlers (parallel with step 6)

Each step is independently deployable; the polling fallback remains active throughout the migration.

### Critical Pitfalls

1. **Hub groups lost on reconnect — HC stops receiving updates silently** — Register `connection.onreconnected(() => connection.invoke("JoinMonitorGroup", ...))` on both pages. Verify by simulating disconnect in browser DevTools (offline → online). This is a complete silent failure with no JS console error.

2. **Cookie auth redirects SignalR negotiate: 401 becomes 302** — Configure `OnRedirectToLogin` in `Program.cs` to return 401 for `/hubs/*` paths, or rely on `[Authorize]` on the Hub class ensuring authenticated users pass through. Test `/hubs/assessment/negotiate` in DevTools Network tab before writing any event handlers.

3. **SQLite "database is locked" under concurrent SignalR + HTTP writes** — Enable WAL mode (`PRAGMA journal_mode=WAL`) and set `Busy Timeout=5000` in connection string before building any SignalR features on top. SignalR increases concurrent write frequency significantly versus polling. Cannot be safely retrofitted after features are built.

4. **SubmitExam / ForceClose race condition amplified by SignalR** — Use status-guarded `ExecuteUpdateAsync` in both actions: `WHERE Status != 'Completed'` for ForceClose; `WHERE Status == 'InProgress'` for SubmitExam. This race existed before but was 10 seconds wide; SignalR makes it sub-second. Worker's graded score can be silently overwritten with "Abandoned" without this guard.

5. **Stale connection ID on page reload — ForceClose/Reset miss worker after reconnect** — Never maintain a `userId → connectionId` dictionary. Use `Clients.User(userId)` for per-worker pushes and `Clients.Group(groupName)` for batch events. Connection IDs change on every page load and every reconnect.

## Implications for Roadmap

Based on research, the following 4-phase structure is recommended:

### Phase 1: Hub Infrastructure and Safety Foundations

**Rationale:** All subsequent phases depend on a correctly configured Hub. Three critical pitfalls (WAL mode, auth redirect fix, group-on-reconnect) must be in place before any event handlers are written — retrofitting them after the fact is costly or breaks data. This phase produces no visible user-facing change but is the prerequisite for everything that follows.

**Delivers:** Working SignalR endpoint at `/hubs/assessment`; authenticated connections via cookie auth; in-memory named groups; WAL mode and busy timeout on SQLite; race condition guard on ForceClose/SubmitExam; JS client installed in `wwwroot`; `AssessmentHub.cs` with group join methods and `[Authorize]`.

**Addresses:** All table-stakes infrastructure. Sets up group naming conventions (`exam-{sessionId}`, `monitor-{title}-{category}-{date}`) that all later phases depend on.

**Avoids:** Pitfalls 1 (groups lost on reconnect), 2 (302 on negotiate), 3 (SQLite locked), 4 (SubmitExam/ForceClose race), 5 (stale connectionId).

**Verification checklist before proceeding to Phase 2:**
- `/hubs/assessment/negotiate` returns 101 or 200 JSON (not 302 or HTML) in DevTools Network
- `PRAGMA journal_mode;` returns `wal`
- `onreconnected` handler implemented and verified via DevTools offline/online simulation
- No `Dictionary<userId, connectionId>` anywhere in codebase
- `ForceCloseAssessment` and `SubmitExam` use status-guarded `ExecuteUpdateAsync`

### Phase 2: HC-to-Worker Push Events (Reset and ForceClose)

**Rationale:** HC-to-worker events are the highest user value and highest urgency. ForceClose in particular is time-critical: the worker's score is locked at zero the moment ForceClose fires server-side. With 10s polling, the worker can answer 3-4 more questions that won't count. The IHubContext injection pattern into AdminController is straightforward once Phase 1 infrastructure is proven.

**Delivers:** HC Reset and ForceClose actions push instantly to the targeted worker's browser (sub-second vs previous 10s polling). Worker sees an explanatory modal ("Sesi direset oleh HC") before redirect on Reset. Worker saves in-progress answer before redirect on Reset. ForceClose shows countdown modal before redirect.

**Addresses:** `examReset` and `examForceClosed` events; session-scoped group pattern (`exam-{sessionId}`); `JoinExamGroup` hub method; ForceCloseAll bulk push can be added in this phase (same group-broadcast pattern, low additional cost).

**Avoids:** UX pitfall of silent redirect on ForceClose (worker gets no explanation); Pitfall 4 race condition (covered by Phase 1 guards).

**Implements:** `IHubContext` injection into `AdminController`; `examReset` and `examForceClosed` event handlers in `StartExam.cshtml`; connection status badge on exam page.

### Phase 3: Worker-to-HC Progress Push and Monitoring View

**Rationale:** Worker progress updates replace the HC monitoring page's 10s polling. This is lower urgency than Phase 2 (HC is observing, not being interrupted mid-exam) but completes the full real-time experience. The existing `updateRow()` and `updateSummary()` functions in the monitoring view can be called directly from the hub event handler since the DTO shape should match what the polling endpoint returned — quick verification needed during implementation.

**Delivers:** HC monitoring page sees worker progress and submission events in near-real-time. Connection status badge on monitoring page. Polling endpoints marked as legacy (not yet removed).

**Addresses:** `progressUpdate` and `examSubmitted` events; monitor-group pattern (`monitor-{title}-{category}-{date}`); `JoinMonitorGroup` hub method; `IHubContext` injection into `CMPController`.

**Implements:** `IHubContext` injection into `CMPController` (SaveAnswer, SubmitExam); `progressUpdate` event handler in `AssessmentMonitoringDetail.cshtml`; on-reconnected state refresh (call HTTP endpoint once on reconnect to recover state accumulated during disconnect window).

### Phase 4: Cleanup, CloseEarly Push, and Polling Removal

**Rationale:** After UAT confirms SignalR is stable, polling fallback endpoints become dead code and a maintenance burden. CloseEarly push fits here as it follows the same group-broadcast pattern established in Phase 2. Removing the polling JS eliminates the risk of stale polling data conflicting with real-time hub data.

**Delivers:** Removal of `CheckExamStatus` and `GetMonitoringProgress` polling JS from both views; CloseEarly push to active workers (broadcasts `examWindowClosed` to assessment group); poll fallback interval optionally reduced to 30s before final removal.

**Addresses:** Tech debt from running both polling and SignalR in parallel; P2 feature CloseEarly; poll interval cleanup.

**Avoids:** Shipping dual code paths permanently (two refresh mechanisms for the same data).

### Phase Ordering Rationale

- Phase 1 is non-negotiable first: WAL mode and auth config cannot be safely added after features are built on top; the race condition guard must precede SignalR going live because SignalR makes the race sub-second.
- Phase 2 before Phase 3: HC-to-worker events (exam integrity) outrank worker-to-HC monitoring updates (observability). A delayed ForceClose costs the worker; a delayed progress update costs HC situational awareness but doesn't corrupt exam data.
- Phase 4 deferred: Polling endpoints are safe to keep during UAT; removal is lower priority than delivering the SignalR features.
- Each phase is independently deployable: polling fallback remains active throughout, so a partial rollout cannot break the exam flow for in-progress exams.

### Research Flags

Phases with well-documented, established patterns (no `/gsd:research-phase` needed):
- **Phase 1:** Standard ASP.NET Core SignalR setup. Official Microsoft docs are complete and verified. WAL mode and busy timeout are SQLite best practices with zero ambiguity.
- **Phase 2:** IHubContext push from controller is the canonical documented pattern. Group naming and session scoping are well-established.
- **Phase 3:** Same patterns as Phase 2. One minor validation needed during implementation: confirm the JSON DTO shape from `IHubContext.SendAsync()` matches what the monitoring view's existing `updateRow()` function expects.
- **Phase 4:** Straightforward deletion of polling JS and endpoints, plus CloseEarly broadcast following the established pattern.

No phases require `/gsd:research-phase` — the domain is fully documented via official Microsoft sources, and the codebase was directly analyzed to confirm existing code structure and boundaries.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | SignalR built-in to net8.0 confirmed via official docs. JS client versioning verified on NuGet and jsDelivr. No NuGet installs required. LibMan install command confirmed. |
| Features | HIGH | Based on direct code analysis of existing polling implementation in both views. MVP features are well-scoped to replace existing polling behavior. Event catalogue is complete and traceable to specific code locations. |
| Architecture | HIGH | Official Microsoft docs verified. IHubContext pattern, group scoping, cookie auth flow, and connection lifecycle all confirmed. Build order validated against existing codebase structure. No novel patterns. |
| Pitfalls | HIGH | Each pitfall traced to a specific code path in the existing codebase or a documented ASP.NET Core behavior. WAL mode and race condition analysis are codebase-specific (not generic). |

**Overall confidence:** HIGH

### Gaps to Address

- **DTO shape compatibility for `progressUpdate`:** The monitoring view's existing `updateRow()` function was identified as reusable, but the exact JSON property names expected by that function vs what `IHubContext.SendAsync()` will emit need a quick check during Phase 3 implementation. This is a 5-minute code inspection during implementation, not a research gap.
- **IIS WebSocket feature on deployment server:** The need for IIS WebSocket Protocol feature to be enabled is confirmed. Whether it is already enabled on the production server is unknown. Verify during Phase 1 deployment — if not enabled, SignalR falls back to long polling (functional but slower; check DevTools for 101 vs 200 on hub connection).
- **Corporate proxy WebSocket support:** If the intranet deployment routes through a proxy, WebSocket upgrades may be blocked. Long-polling fallback handles this automatically, but exam sessions longer than the proxy timeout (typically 30s to 2min on IIS) may drop mid-exam. Check proxy WebSocket configuration during UAT.

## Sources

### Primary (HIGH confidence)
- [ASP.NET Core SignalR Introduction — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-8.0) — hub setup, transport options, shared framework inclusion
- [Get Started with ASP.NET Core SignalR — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/tutorials/signalr?view=aspnetcore-8.0) — LibMan install command, exact Program.cs placement, verified 2026-01-28
- [Authentication and authorization in ASP.NET Core SignalR — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-8.0) — cookie auth, `[Authorize]` on Hub, `OnRedirectToLogin` override
- [Manage users and groups in SignalR — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups?view=aspnetcore-10.0) — group membership, reconnect behavior, explicit re-join requirement
- [Security considerations in ASP.NET Core SignalR — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/security?view=aspnetcore-8.0) — role auth on Hub methods, ownership checks
- Direct codebase analysis: `Controllers/AdminController.cs` (GetMonitoringProgress, ResetAssessment, ForceCloseAssessment, ForceCloseAll), `Controllers/CMPController.cs` (SubmitExam, SaveAnswer, CheckExamStatus), `Views/Admin/AssessmentMonitoringDetail.cshtml` (polling JS, updateRow/updateSummary functions), `Views/CMP/StartExam.cshtml` (statusPollInterval JS, force-close modal)

### Secondary (MEDIUM confidence)
- [jsDelivr: @microsoft/signalr](https://www.jsdelivr.com/package/npm/@microsoft/signalr) — confirmed 8.x and 10.x branches available; version pinning guidance
- [SQLite concurrent writes and "database is locked" errors — Ten Thousand Meters](https://tenthousandmeters.com/blog/sqlite-concurrent-writes-and-database-is-locked-errors/) — WAL mode and busy timeout guidance for concurrent write scenarios
- [Managing SignalR ConnectionIds — consultwithgriff.com](https://consultwithgriff.com/signalr-connection-ids) — user-based vs connection-ID-based push patterns; why static dictionaries fail

### Tertiary (LOW confidence)
- GitHub Issue #27956, GitHub Discussion #54818 — concurrency and reconnect behavior edge cases; confirms documented behaviors but from community reports rather than official source

---
*Research completed: 2026-03-13*
*Synthesized from: STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md*
*Ready for roadmap: yes*
