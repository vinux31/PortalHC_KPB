# Phase 164: HC-to-Worker Push Events - Research

**Researched:** 2026-03-13
**Domain:** ASP.NET Core SignalR — IHubContext server-push, client-side event handlers, connection status UI
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- Worker close notification: reuse existing `examClosedModal` in StartExam.cshtml — SignalR handler triggers same modal polling currently shows
- Modal is fully blocking: static backdrop, no X button, no escape key — worker can only click "Lihat Hasil"
- Modal text differs by source: push from HC = "Ujian diakhiri oleh pengawas", polling/timer = "Waktu ujian habis" (dynamic text based on reason)
- "Lihat Hasil" button redirects to ExamResults page
- Reset notification: new blocking modal "Sesi direset oleh pengawas" with "Kembali" button
- On reset received: timer countdown stops, auto-save stops, form controls disabled
- "Kembali" button redirects to /CMP/Assessment
- Connection status badge: positioned in navbar/header area near exam timer (top right)
- 3 states: green Live, yellow Reconnecting, red Disconnected — labels in English
- Badge displayed on both StartExam AND AssessmentMonitoringDetail pages
- Akhiri Semua: same modal as individual Akhiri Ujian — all InProgress workers see "Ujian diakhiri oleh pengawas"
- Group broadcast: single `Clients.Group("batch-{batchId}").SendAsync("examClosed", ...)` call
- Workers with Open/not-started status cancelled by Akhiri Semua do NOT receive push
- HC monitoring page ignores `examClosed` event — no handler registered on monitoring page in Phase 164
- AdminController injects `IHubContext<AssessmentHub>` via constructor
- After DB write completes, controller calls `_hubContext.Clients.Group/User(...).SendAsync(...)` directly
- No service wrapper layer
- `examClosed` payload: `{ reason: "hc_closed" }`
- `sessionReset` payload: `{ reason: "hc_reset" }`
- No redirect URLs in payload — JS knows target pages
- Polling (CheckExamStatus every 10s) stays active alongside SignalR push
- Existing `examClosed` flag in JS prevents double-modal — first trigger (push or poll) sets flag, other skips
- Only StartExam.cshtml handles push events (examClosed, sessionReset)
- AssessmentMonitoringDetail.cshtml only gets connection badge — no event handlers in Phase 164

### Claude's Discretion

- Exact modal styling and icon choices
- Guard implementation for dual trigger (push + polling)
- IHubContext constructor injection pattern details
- Badge CSS implementation and animation

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| PUSH-01 | Worker exam page updates instantly when HC resets their session | `sessionReset` event via `IHubContext.Clients.User(session.UserId).SendAsync("sessionReset", ...)` after DB write in Reset action; client handler stops timer, disables form, shows blocking modal |
| PUSH-02 | Worker exam page redirects to results when HC closes their session (Akhiri Ujian) | `examClosed` event via `IHubContext.Clients.User(session.UserId).SendAsync("examClosed", ...)` after rowsAffected > 0 guard; client reuses existing examClosedModal with dynamic reason text |
| PUSH-03 | All workers in a batch are notified when HC closes all sessions (Akhiri Semua) | `examClosed` event via `IHubContext.Clients.Group("batch-{batchKey}").SendAsync("examClosed", ...)` after SaveChangesAsync; only InProgress sessions receive modal — Open/Cancelled workers are already off the exam page |

</phase_requirements>

---

## Summary

Phase 164 wires three HC controller actions (Reset, AkhiriUjian, AkhiriSemuaUjian) in AdminController to push SignalR events to connected workers. The infrastructure from Phase 163 is fully operational: AssessmentHub is registered, the JS client (`assessment-hub.js`) handles reconnect and group join, and both StartExam and AssessmentMonitoringDetail pages already load the hub script with `window.assessmentBatchKey` set.

The work divides into three parts: (1) inject `IHubContext<AssessmentHub>` into AdminController and add `SendAsync` calls after each DB write, (2) add client-side `on("examClosed")` and `on("sessionReset")` handlers to StartExam.cshtml with appropriate modal and state management, and (3) add a connection status badge to both pages driven by SignalR connection state events already wired in `assessment-hub.js`.

The dual-trigger guard (`examClosed` flag) already exists in StartExam.cshtml and prevents double-modal when both push and polling fire. The only new complexity is that the modal text must be dynamic (reason-based) and the reset modal is entirely new.

**Primary recommendation:** Add IHubContext to AdminController constructor, call SendAsync after successful DB writes, register two client handlers on StartExam, add reset modal HTML, update examClosedModal text to be dynamic, add connection badge CSS + HTML to both pages.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.AspNetCore.SignalR` | Built into ASP.NET Core 8 | Server-side hub and IHubContext | Part of the framework — no NuGet package needed |
| `@microsoft/signalr` JS client | 8.x (already in wwwroot) | Client connection, reconnect, event handlers | Official client, already installed in Phase 163 |
| Bootstrap 5 Modal | Already in project | Blocking modal UI for close/reset notifications | Already used by existing examClosedModal |

### No New Packages Required

All dependencies are already present from Phase 163. This phase is pure wiring.

---

## Architecture Patterns

### IHubContext Injection Pattern

`IHubContext<THub>` is the standard ASP.NET Core mechanism for sending messages from outside a hub (e.g., from a controller action).

**Constructor injection into AdminController:**

```csharp
// Add to using directives
using Microsoft.AspNetCore.SignalR;
using HcPortal.Hubs;

// Add field
private readonly IHubContext<AssessmentHub> _hubContext;

// Add to constructor parameter list
IHubContext<AssessmentHub> hubContext

// Add to constructor body
_hubContext = hubContext;
```

AdminController currently has 8 injected services. IHubContext is the 9th — no breaking change, DI container resolves it automatically since `AddSignalR()` registers it (Phase 163).

### Targeting a Specific User

`Clients.User(userId)` sends to all connections belonging to a specific Identity user. The `userId` is the ASP.NET Identity user ID string (GUID). AssessmentSession.UserId is already this value.

```csharp
// After successful DB write (rowsAffected > 0):
await _hubContext.Clients.User(session.UserId)
    .SendAsync("examClosed", new { reason = "hc_closed" });
```

**Why User() not Group():** The worker's connection automatically maps to their Identity user via the `IUserIdProvider` — no manual group membership needed. Only the hub's `JoinBatch` group is needed for broadcast (Akhiri Semua).

### Targeting a Group (Akhiri Semua)

```csharp
// batchKey already used in AssessmentHub.JoinBatch:
var batchKey = $"{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"batch-{batchKey}")
    .SendAsync("examClosed", new { reason = "hc_closed" });
```

The group name must match exactly what `JoinBatch` uses: `"batch-{batchKey}"`. Workers join this group when StartExam loads (via assessment-hub.js `JoinBatch` call). Only InProgress workers are on the exam page and in the group.

### Reset Action — Where to Add Push

The Reset action (line 2203) currently returns after `rsRowsAffected == 0` guard. The SendAsync call goes AFTER the `rsRowsAffected > 0` path, before the final redirect:

```csharp
// After the rsRowsAffected == 0 early return (line 2221-2229)
// and after the audit log (line 2231-2240)

// Push to worker
await _hubContext.Clients.User(session.UserId)
    .SendAsync("sessionReset", new { reason = "hc_reset" });

TempData["Success"] = "...";
return RedirectToAction(...);
```

Note: The Reset action loads `assessment` and `session` from DB. `session.UserId` is available as the entity is loaded at line ~2170 (need to verify the variable name in the Reset action — it uses `assessment` not `session`).

### AkhiriUjian — Where to Add Push

After `rowsAffected == 0` early return (line 2300-2309) and after the audit log (line 2314-2322):

```csharp
await _hubContext.Clients.User(session.UserId)
    .SendAsync("examClosed", new { reason = "hc_closed" });
```

`session.UserId` is already available (loaded at line 2257).

### AkhiriSemuaUjian — Where to Add Push

After `SaveChangesAsync()` (line 2372) and after cache invalidation, before the audit log or after:

```csharp
var batchKey = $"{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"batch-{batchKey}")
    .SendAsync("examClosed", new { reason = "hc_closed" });
```

Only InProgress workers (who were on the exam page) receive this — Open/Cancelled workers are not on StartExam and never joined the group, or already left it.

### Client-Side Event Handler Registration

Handlers are registered on `window.assessmentHub` (the connection exposed by assessment-hub.js) in the page-specific `@section Scripts` block, AFTER the hub script loads:

```javascript
// In StartExam.cshtml @section Scripts, after assessment-hub.js loads:
window.assessmentHub.on('examClosed', function(payload) {
    if (examClosed) return;  // dual-trigger guard (flag already exists)
    examClosed = true;
    clearInterval(statusPollInterval);
    clearInterval(timerInterval);
    window.onbeforeunload = null;

    // Update modal text based on reason
    var reasonText = (payload && payload.reason === 'hc_closed')
        ? 'Ujian diakhiri oleh pengawas.'
        : 'Waktu ujian habis.';
    document.getElementById('examClosedReason').textContent = reasonText;

    var redirectTarget = '@Url.Action("ExamResults", "CMP")?sessionId=' + SESSION_ID;
    var modal = new bootstrap.Modal(document.getElementById('examClosedModal'));
    modal.show();

    document.getElementById('closedViewResultsBtn').onclick = function() {
        window.location.href = redirectTarget;
    };

    var countdown = 5;
    var countdownEl = document.getElementById('closedCountdown');
    var countdownInterval = setInterval(function() {
        countdown--;
        countdownEl.textContent = countdown;
        if (countdown <= 0) {
            clearInterval(countdownInterval);
            window.location.href = redirectTarget;
        }
    }, 1000);
});

window.assessmentHub.on('sessionReset', function(payload) {
    // Stop timer and auto-save
    clearInterval(timerInterval);
    clearInterval(autoSaveInterval);  // need to verify variable name
    window.onbeforeunload = null;

    // Disable form controls
    document.querySelectorAll('#examForm input, #examForm button').forEach(function(el) {
        el.disabled = true;
    });

    // Show reset modal
    var modal = new bootstrap.Modal(document.getElementById('sessionResetModal'));
    modal.show();
});
```

### Connection Status Badge

Add badge HTML near the exam timer in StartExam header and near batch info in AssessmentMonitoringDetail. Badge reflects SignalR connection state.

The connection state hooks already exist in assessment-hub.js (`onreconnecting`, `onreconnected`, `onclose`). The badge needs to be updated from those hooks. Since assessment-hub.js is a shared IIFE, the cleanest approach is a lightweight callback pattern: pages define `window.onHubStateChange` before assessment-hub.js runs, and assessment-hub.js calls it if defined.

Alternatively (simpler, no change to assessment-hub.js): register additional handlers on `window.assessmentHub` in page-specific script, since SignalR allows multiple `on*` registrations:

```javascript
// Badge HTML (add to view):
// <span id="hubStatusBadge" class="badge bg-success ms-2">Live</span>

// In page-specific script after hub loads:
var badge = document.getElementById('hubStatusBadge');

window.assessmentHub.onreconnecting(function() {
    if (badge) { badge.className = 'badge bg-warning ms-2'; badge.textContent = 'Reconnecting...'; }
});
window.assessmentHub.onreconnected(function() {
    if (badge) { badge.className = 'badge bg-success ms-2'; badge.textContent = 'Live'; }
});
window.assessmentHub.onclose(function() {
    if (badge) { badge.className = 'badge bg-danger ms-2'; badge.textContent = 'Disconnected'; }
});
```

Note: SignalR allows multiple registrations of `onreconnecting`/`onreconnected`/`onclose` — they stack, they do not replace. The existing handlers in assessment-hub.js (toast notifications) will still fire.

### Anti-Patterns to Avoid

- **Sending push before DB write confirms:** Always check `rowsAffected > 0` or equivalent before calling SendAsync. If DB write is skipped (race condition), do not push.
- **Using connectionId for user targeting:** Connection IDs change on every reconnect. Use `Clients.User(userId)` — ASP.NET Identity maps user ID to all active connections automatically.
- **Registering handlers inside assessment-hub.js:** That file is shared by HC and worker pages. Page-specific event handlers (examClosed, sessionReset) belong in the page's `@section Scripts`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| User-to-connection mapping | Custom `Dictionary<userId, connectionId>` | `Clients.User(userId)` | IUserIdProvider handles mapping automatically; connection IDs change on reconnect |
| Group membership | Custom list of connected session IDs | `Clients.Group("batch-{key}")` | SignalR group is already maintained; JoinBatch called on connect and reconnect |
| Reconnect with group rejoin | Custom reconnect timer | `withAutomaticReconnect` + `onreconnected` | Already implemented in assessment-hub.js — JoinBatch is called on reconnect |

---

## Common Pitfalls

### Pitfall 1: Reset Action Uses Different Variable Name

**What goes wrong:** The Reset action (around line 2170) loads the AssessmentSession as a variable. The CONTEXT says it's at line 2203 and refers to `assessment` for the parent, but the session entity may have a different local variable name.
**How to avoid:** Read the full Reset action before coding. Confirm the session entity variable name and that `UserId` is accessible on it.
**Warning signs:** Compile error accessing `.UserId` — means using wrong variable.

### Pitfall 2: examClosedModal Text is Currently Static

**What goes wrong:** The existing modal body text (line 237 in StartExam.cshtml) hardcodes "Ujian Anda telah diakhiri oleh penyelenggara." The decision requires dynamic text — "Ujian diakhiri oleh pengawas" for HC push, "Waktu ujian habis" for timer/polling.
**How to avoid:** Add a `<span id="examClosedReason">` inside the modal body. Both the polling handler (line 725) and the new SignalR handler write to this span before showing the modal.
**Warning signs:** Modal shows wrong message for timer expiry after this change.

### Pitfall 3: autoSaveInterval Variable Name

**What goes wrong:** The sessionReset handler needs to stop auto-save. The auto-save setInterval variable name in StartExam.cshtml must be verified — if it's not `autoSaveInterval`, the wrong interval is cleared.
**How to avoid:** Search StartExam.cshtml for `setInterval` usages before writing the handler. There are at least two: `statusPollInterval` and `timerInterval`. Auto-save may be a third.
**Warning signs:** Auto-save continues after reset, potentially writing answers on a reset session.

### Pitfall 4: Badge Initial State Before Connection Established

**What goes wrong:** The badge shows "Live" before the connection actually connects, giving false confidence during the brief startup window.
**How to avoid:** Set initial badge state to "Reconnecting..." or a neutral state, then update to "Live" only inside `onreconnected` OR after `connection.start()` resolves. Since assessment-hub.js calls `startHub()` immediately and the badge is added to the page before that script runs, the initial HTML can be a neutral state ("Connecting..."), updated to "Live" after start success.
**Warning signs:** Badge shows "Live" but WebSocket connection failed.

### Pitfall 5: Akhiri Semua batchKey Format

**What goes wrong:** The group name in the controller must match exactly what `JoinBatch` uses. `JoinBatch` in the hub creates: `"batch-{batchKey}"` where batchKey is the raw argument. The AssessmentBatchKey set in the view (CMPController line 1264) is `"{title}|{category}|{date}"`. The controller code in `AkhiriSemuaUjian` has separate `title`, `category`, `scheduleDate` parameters — must reconstruct the same format.
**How to avoid:** Use exactly `$"batch-{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}"` — same format as CMPController's ViewBag assignment.
**Warning signs:** Push fires successfully (no exception) but workers don't receive it — group name mismatch silently drops the message.

---

## Code Examples

### IHubContext Injection in AdminController

```csharp
// Add using:
using Microsoft.AspNetCore.SignalR;
using HcPortal.Hubs;

// Add field (line ~22, after existing fields):
private readonly IHubContext<AssessmentHub> _hubContext;

// Add parameter to constructor (after INotificationService parameter):
IHubContext<AssessmentHub> hubContext

// Add assignment in constructor body (after _notificationService = notificationService):
_hubContext = hubContext;
```

### SendAsync After AkhiriUjian DB Write

```csharp
// Location: after audit log block (around line 2322), before TempData["Success"]
if (rowsAffected > 0)  // already inside this branch — no extra guard needed
{
    await _hubContext.Clients.User(session.UserId)
        .SendAsync("examClosed", new { reason = "hc_closed" });
}
```

### SendAsync After AkhiriSemuaUjian DB Write

```csharp
// Location: after foreach loop and SaveChangesAsync (line 2372)
var batchKey = $"{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"batch-{batchKey}")
    .SendAsync("examClosed", new { reason = "hc_closed" });
```

### New Reset Modal HTML (add to StartExam.cshtml)

```html
<!-- Session reset modal (blocking, no dismiss) -->
<div class="modal fade" id="sessionResetModal" tabindex="-1"
     data-bs-backdrop="static" data-bs-keyboard="false" aria-hidden="true"
     style="z-index: 9999;">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-danger">
            <div class="modal-header bg-danger text-white border-bottom-0 pb-0">
                <h5 class="modal-title fw-bold">
                    <i class="bi bi-arrow-counterclockwise me-2"></i>Sesi Direset
                </h5>
            </div>
            <div class="modal-body text-center py-3">
                <p class="mb-2 fw-semibold">Sesi ujian Anda telah direset oleh pengawas.</p>
                <p class="text-muted small mb-0">Klik tombol di bawah untuk kembali ke daftar ujian.</p>
            </div>
            <div class="modal-footer justify-content-center border-top-0">
                <button type="button" class="btn btn-danger fw-semibold" id="resetKembaliBtn">
                    <i class="bi bi-arrow-left me-1"></i>Kembali
                </button>
            </div>
        </div>
    </div>
</div>
```

### Connection Status Badge HTML

```html
<!-- Add near exam timer in StartExam header, and near monitoring header in AssessmentMonitoringDetail -->
<span id="hubStatusBadge" class="badge bg-secondary ms-2 small">Connecting...</span>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Polling CheckExamStatus every 10s | SignalR push + polling fallback | Phase 164 | Sub-second notification instead of up to 10s delay |
| Static modal text | Dynamic reason-based text | Phase 164 | Same modal reused for both HC push and timer expiry |

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Playwright (browser-based UAT) + manual verification |
| Config file | None in project |
| Quick run command | Manual: open two browser tabs (HC + worker), trigger action |
| Full suite command | Manual flow through all 3 scenarios |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PUSH-01 | HC resets worker session; worker sees blocking modal within 1s | Manual browser | N/A — requires two sessions | No test file |
| PUSH-02 | HC clicks Akhiri Ujian; worker sees examClosedModal and redirects | Manual browser | N/A — requires two sessions | No test file |
| PUSH-03 | HC clicks Akhiri Semua; all InProgress workers see modal | Manual browser | N/A — requires multiple sessions | No test file |

### Sampling Rate

- **Per task commit:** Compile check — `dotnet build` (no runtime errors)
- **Per wave merge:** Manual two-browser test of each push scenario
- **Phase gate:** All 3 PUSH requirements verified manually before `/gsd:verify-work`

### Wave 0 Gaps

None — no automated test infrastructure needed. SignalR push events require live browser sessions and cannot be meaningfully unit tested without a test hub server. Manual verification is the appropriate approach for this phase.

---

## Open Questions

1. **Reset action variable name for UserId**
   - What we know: The Reset action is at line ~2203. It loads `assessment` (the session). The variable holding the UserId needs confirmation.
   - What's unclear: Is it `assessment.UserId` or a separate loaded entity?
   - Recommendation: Read lines 2160-2205 before coding the push call.

2. **autoSaveInterval variable name in StartExam.cshtml**
   - What we know: There is an auto-save mechanism in StartExam that periodically saves answers.
   - What's unclear: The variable name and whether it's a setInterval ID that can be clearInterval'd.
   - Recommendation: Read the auto-save block in StartExam.cshtml before writing the sessionReset handler.

3. **Badge initial state transition timing**
   - What we know: assessment-hub.js calls `startHub()` immediately on script load, which is async.
   - What's unclear: Whether the hub `start()` promise resolution should update the badge to "Live" in assessment-hub.js or in page-specific script.
   - Recommendation: Page-specific script can listen to `onreconnected` for reconnects. For initial connect, register a one-time state check after hub load or use a small timeout. Alternatively, add a `window.onHubConnected` callback hook to assessment-hub.js (minimal change, keeps page code clean).

---

## Sources

### Primary (HIGH confidence)

- Direct code inspection of `wwwroot/js/assessment-hub.js` — connection setup, group join, reconnect handlers
- Direct code inspection of `Hubs/AssessmentHub.cs` — JoinBatch/LeaveBatch with `"batch-{batchKey}"` group name pattern
- Direct code inspection of `Controllers/AdminController.cs` — constructor injection pattern, Reset/AkhiriUjian/AkhiriSemuaUjian action structure
- Direct code inspection of `Views/CMP/StartExam.cshtml` — examClosedModal HTML, examClosed flag, statusPollInterval, timerInterval
- Direct code inspection of `.planning/phases/164-hc-to-worker-push-events/164-CONTEXT.md` — all locked decisions

### Secondary (MEDIUM confidence)

- ASP.NET Core SignalR IHubContext documentation pattern — `Clients.User()` uses IUserIdProvider which maps ASP.NET Identity user ID automatically; `Clients.Group()` uses groups maintained by `AddToGroupAsync`

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages, all dependencies confirmed present
- Architecture: HIGH — code directly inspected, exact variable names/line numbers confirmed
- Pitfalls: HIGH — derived from actual code reading (static modal text, variable names, group name format)

**Research date:** 2026-03-13
**Valid until:** 2026-04-13 (stable — no external dependencies)
