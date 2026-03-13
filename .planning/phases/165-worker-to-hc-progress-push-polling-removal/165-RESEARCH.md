# Phase 165: Worker-to-HC Progress Push + Polling Removal - Research

**Researched:** 2026-03-13
**Domain:** ASP.NET Core SignalR — worker-to-HC push, IHubContext in CMPController, monitoring group, polling removal
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- Push on every answer instantly — SaveAnswer calls push updated answeredCount + totalQuestions to HC monitor group
- Push targets BOTH HC monitoring group AND the worker (worker gets server-confirmed progress)
- Push originates from CMPController: SaveAnswer (progress), StartExam GET (started), SubmitExam POST (submitted)
- HC monitoring page auto-joins "monitor-{batchKey}" group using the same batchKey pattern
- Reconnect auto-rejoins monitor group (INFRA-05 already built in Phase 163)
- Row highlight flash (~1 second, light blue) when row data changes, fades back
- Toast notifications for both start and submit events using existing assessment-hub.js toast pattern
- Score appears immediately in monitoring table row on worker submit
- Start event: HC row status "Belum Mulai" → "Dalam Pengerjaan", progress bar appears at 0%, row flashes
- Submit event: HC row status → "Selesai", score appears, progress bar → 100%, row flashes green briefly
- Start toast: "{Name} memulai ujian"
- Submit toast: "{Name} menyelesaikan ujian (Skor: {score})"
- Polling removal: delete server-side endpoints entirely (CheckExamStatus, GetMonitoringProgress)
- Remove: statusPollInterval (StartExam.cshtml line 786) and pollingTimer (AssessmentMonitoringDetail.cshtml line 840)
- Keep: timerInterval countdown (line 331), 30-second auto-save (line 547), countdownTimer tickCountdowns (line 841)
- navPoll and rPoll: Claude's discretion whether replaceable by SignalR or should stay

### Claude's Discretion

- Row flash animation CSS implementation
- Exact toast message formatting and duration
- Whether navPoll and rPoll intervals can be replaced by SignalR or should stay
- Monitor group naming convention (e.g., "monitor-{title}|{category}|{date}" matching batch key pattern)
- IHubContext injection into CMPController constructor pattern

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MONITOR-01 | HC monitoring page shows real-time answer progress without polling | SaveAnswer pushes `progressUpdate` event with answeredCount + totalQuestions to monitor group; monitoring page `updateRow()` called directly from event handler |
| MONITOR-02 | HC monitoring page instantly shows when a worker completes their exam | SubmitExam POST pushes `workerSubmitted` with sessionId + score + result after rowsAffected > 0 guard; monitoring page updates status/score cells and flashes green |
| MONITOR-03 | HC monitoring page instantly shows when a worker starts their exam | StartExam GET pushes `workerStarted` with sessionId + workerName after first DB write (StartedAt == null path); monitoring page updates status cell and shows 0% progress |
| CLEAN-01 | Polling code (setInterval) removed from monitoring and exam pages after SignalR proven working | statusPollInterval in StartExam.cshtml and pollingTimer in AssessmentMonitoringDetail.cshtml removed; CheckExamStatus and GetMonitoringProgress controller actions deleted entirely |

</phase_requirements>

---

## Summary

Phase 165 is the reverse direction of Phase 164: where Phase 164 pushed HC actions to workers, Phase 165 pushes worker exam actions to the HC monitoring page. The infrastructure is identical — AssessmentHub, assessment-hub.js, IHubContext injection — but now CMPController gets the IHubContext and three new SignalR push calls are added (SaveAnswer, StartExam GET, SubmitExam POST). The HC monitoring page (AssessmentMonitoringDetail.cshtml) already has the `updateRow()` function and all DOM structure needed to consume push events.

The monitoring page already has: `updateRow(session)` function (line 715), `statusBadgeClass()` helper, `updateSummary()`, `buildActionsHtml()`, countdownMap, and the connection badge infrastructure from Phase 164. The push payload just needs to match the shape that `updateRow()` already expects from the polling DTO. The key difference is that push events are partial (one row at a time) while polling was full batch — so `updateSummary()` must be called differently.

After UAT confirms SignalR stability, the two polling setIntervals are removed (StatusPollInterval in StartExam, pollingTimer in AssessmentMonitoringDetail) and both server-side polling endpoints are deleted. The navPoll and rPoll intervals are local save-completion guards (not server polls) and should remain — they wait for pending AJAX saves to flush before allowing navigation/submit.

**Primary recommendation:** Inject IHubContext into CMPController (10th constructor param), add three SendAsync calls after their respective DB writes, add JoinMonitor/LeaveMonitor hub methods, add monitoring page SignalR event handlers that call existing `updateRow()`, add row flash CSS, then in the polling-removal wave delete the two setIntervals and two controller actions.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `Microsoft.AspNetCore.SignalR` | Built into ASP.NET Core 8 | IHubContext server-push from CMPController | Same as Phase 164 — no new package |
| `@microsoft/signalr` JS client | 8.x (in wwwroot) | Monitor page SignalR event handlers | Already installed Phase 163 |
| Bootstrap 5 | Project standard | Row flash via CSS class toggle | Already in project |

### No New Packages Required

All dependencies present from Phase 163. This phase is pure wiring — same pattern as Phase 164 but in CMPController instead of AdminController.

**Installation:** None required.

---

## Architecture Patterns

### IHubContext Injection in CMPController

CMPController currently has 9 injected services (UserManager, RoleManager, SignInManager, DbContext, IWebHostEnvironment, AuditLogService, IMemoryCache, ILogger, INotificationService). IHubContext is the 10th.

```csharp
// Add using directives (top of file):
using Microsoft.AspNetCore.SignalR;
using HcPortal.Hubs;

// Add field (after _notificationService):
private readonly IHubContext<AssessmentHub> _hubContext;

// Add constructor parameter (after INotificationService notificationService):
IHubContext<AssessmentHub> hubContext

// Add assignment in constructor body:
_hubContext = hubContext;
```

DI resolves it automatically because `AddSignalR()` registers IHubContext<T> (Phase 163).

### Monitor Group Naming

The HC monitoring page must join a dedicated monitor group — separate from the worker batch group — so HC doesn't receive worker events meant for other workers.

CONTEXT.md specifies Claude's discretion on naming. The batch key pattern already in use is `"{title}|{category}|{date}"`. The monitor group should be `"monitor-{batchKey}"` to parallel the `"batch-{batchKey}"` worker group. This naming:
- Is unambiguous (no collision with worker batch group)
- Uses the same batchKey that AssessmentMonitoringDetail already sets in `ViewBag.AssessmentBatchKey`
- Follows the established prefix convention

**AssessmentHub additions:**

```csharp
public async Task JoinMonitor(string batchKey)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"monitor-{batchKey}");
}

public async Task LeaveMonitor(string batchKey)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"monitor-{batchKey}");
}
```

**assessment-hub.js reconnect** already calls `JoinBatch` on reconnect via `window.assessmentBatchKey`. For monitor group rejoin, the monitoring page's page-specific script registers an additional `onreconnected` handler:

```javascript
window.assessmentHub.onreconnected(function() {
    if (window.assessmentBatchKey) {
        window.assessmentHub.invoke('JoinMonitor', window.assessmentBatchKey).catch(function(err) {
            console.warn('[monitor] JoinMonitor after reconnect failed:', err);
        });
    }
});
```

Note: SignalR allows multiple `onreconnected` registrations — they stack, not replace. The existing `onreconnected` in assessment-hub.js (JoinBatch + toast) will still fire.

### SaveAnswer Push (MONITOR-01)

**Location in CMPController.cs:** After line 287 (the `return Json(new { success = true })`) — specifically after both the `ExecuteUpdateAsync` path and the `SaveChangesAsync` (insert) path resolve.

The current SaveAnswer has two code paths:
1. Update existing row (line 268-273): `ExecuteUpdateAsync`, no `SaveChangesAsync` call
2. Insert new row (line 277-285): `_context.PackageUserResponses.Add` + `SaveChangesAsync`

Both paths reach `return Json(new { success = true })` at line 287. The push must happen before the return in both paths, or extracted after the branching. The simplest approach: push before the final return (after both paths).

But we need `answeredCount` to push. Computing it inline from DB adds a query on every answer save — **avoid this**. Instead:

The push payload should not include the computed count from DB. Instead, push minimal data that lets the HC page compute or increment. Options:
1. Push `answeredCount` by querying the count — 1 extra query per save
2. Push a signal only (no count) — HC page ignores progress magnitude, just refreshes full row via a fetch (defeats the purpose)
3. Push `answeredDelta: +1` — client increments its local counter

Decision (Claude's discretion per CONTEXT.md): Push `answeredDelta: 1` is fragile (misses concurrent updates). Querying the count is the correct approach: one `CountAsync` after DB write, then push. The 1-query overhead per answer save is acceptable given the feature value. This is the same tradeoff accepted when building the endpoint.

**Implementation:**

```csharp
// After the upsert logic and before the final return Json:
var answeredCount = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == sessionId);

// Get totalQuestions from the session's assignment (cached or computed)
var assignment = await _context.UserPackageAssignments
    .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
int totalQuestions = 0;
if (assignment != null)
{
    totalQuestions = assignment.GetShuffledQuestionIds().Count;
}

// Push to monitor group
var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{batchKey}")
    .SendAsync("progressUpdate", new {
        sessionId,
        progress = answeredCount,
        totalQuestions
    });

return Json(new { success = true });
```

**Note on totalQuestions lookup:** The `GetShuffledQuestionIds()` is computed from UserPackageAssignments already loaded. If the assignment is null (legacy path), totalQuestions stays 0 — same as the monitoring page default. This is safe.

**Also push to the worker:** CONTEXT.md says push targets both HC monitoring group AND the worker. Worker-side progress confirmation allows a server-confirmed progress bar update. Use `Clients.User()` for worker:

```csharp
await _hubContext.Clients.User(session.UserId)
    .SendAsync("progressUpdate", new { sessionId, progress = answeredCount, totalQuestions });
```

### StartExam GET Push (MONITOR-03)

**Location in CMPController.cs:** Lines 1036-1041 — the `if (assessment.StartedAt == null)` block where `StartedAt` is set and `SaveChangesAsync` is called. Push after `SaveChangesAsync`:

```csharp
if (assessment.StartedAt == null)
{
    assessment.Status = "InProgress";
    assessment.StartedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    // Push to HC monitor group
    var batchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
    var workerName = user.FullName ?? user.UserName ?? "Peserta";
    await _hubContext.Clients.Group($"monitor-{batchKey}")
        .SendAsync("workerStarted", new {
            sessionId = assessment.Id,
            workerName,
            status = "InProgress"
        });
}
```

**Worker name lookup:** `user` is already loaded at line 982 (`await _userManager.GetUserAsync(User)`). The `FullName` property (or equivalent display name field) must be verified — check ApplicationUser model for the display name property.

### SubmitExam POST Push (MONITOR-02)

**Location in CMPController.cs:** After the `rowsAffected > 0` guard in the package path (line 1662). The session is already graded; `finalPercentage` and `assessment.IsPassed` are computed. Push before the `NotifyIfGroupCompleted` call:

```csharp
if (rowsAffected == 0)
{
    TempData["Info"] = "Ujian Anda sudah diakhiri oleh pengawas.";
    return RedirectToAction("Results", new { id });
}

// Push submission to HC monitor group
var batchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
string? result = finalPercentage >= assessment.PassPercentage ? "Pass" : "Fail";
await _hubContext.Clients.Group($"monitor-{batchKey}")
    .SendAsync("workerSubmitted", new {
        sessionId = id,
        score = finalPercentage,
        result,
        status = "Completed",
        progress = totalQuestions,  // submitted = all answered
        completedAt = DateTime.UtcNow
    });

// Update assignment completion separately (existing code continues)
```

The legacy path (non-package) also needs a push — same logic after its `rowsAffected > 0` check.

### HC Monitoring Page: Event Handlers

These go in `AssessmentMonitoringDetail.cshtml` `@section Scripts`, after the existing badge setup script, inside the same `<script>` block:

```javascript
// -------- SignalR push event handlers (Phase 165) --------

// Join monitor group
if (window.assessmentBatchKey) {
    window.assessmentHub.invoke('JoinMonitor', window.assessmentBatchKey).catch(function(err) {
        console.warn('[monitor] JoinMonitor failed:', err);
    });
}

// Reconnect rejoin (stacks with assessment-hub.js JoinBatch rejoin)
window.assessmentHub.onreconnected(function() {
    if (window.assessmentBatchKey) {
        window.assessmentHub.invoke('JoinMonitor', window.assessmentBatchKey).catch(function(err) {
            console.warn('[monitor] JoinMonitor after reconnect failed:', err);
        });
    }
});

// Progress update (worker answered a question)
window.assessmentHub.on('progressUpdate', function(payload) {
    var tr = document.querySelector('tr[data-session-id="' + payload.sessionId + '"]');
    if (!tr) return;
    var tds = tr.querySelectorAll('td');
    // Update progress cell (col 1)
    tds[1].textContent = payload.totalQuestions > 0
        ? (payload.progress + '/' + payload.totalQuestions)
        : '\u2014';
    // Flash row
    flashRow(tr, 'flash-update');
    updateLastUpdated();
});

// Worker started exam
window.assessmentHub.on('workerStarted', function(payload) {
    var tr = document.querySelector('tr[data-session-id="' + payload.sessionId + '"]');
    if (!tr) return;
    var tds = tr.querySelectorAll('td');
    // Update status cell (col 2)
    tds[2].innerHTML = '<span class="badge bg-warning text-dark">InProgress</span>';
    // Update progress cell (col 1) to 0
    // (totalQuestions not known here — leave as-is or set 0/total if we have it)
    flashRow(tr, 'flash-update');
    showToastFromMonitor(payload.workerName + ' memulai ujian');
    // Update summary counts
    updateSummaryFromDOM();
    updateLastUpdated();
});

// Worker submitted exam
window.assessmentHub.on('workerSubmitted', function(payload) {
    // Build a partial session object matching updateRow() signature
    var session = {
        sessionId:      payload.sessionId,
        status:         'Completed',
        progress:       payload.progress || 0,
        totalQuestions: payload.totalQuestions || 0,
        score:          payload.score,
        result:         payload.result,
        remainingSeconds: null,
        completedAt:    payload.completedAt
    };
    updateRow(session);
    flashRow(document.querySelector('tr[data-session-id="' + payload.sessionId + '"]'), 'flash-complete');
    showToastFromMonitor(payload.workerName + ' menyelesaikan ujian (Skor: ' + payload.score + '%)');
    updateSummaryFromDOM();
    updateLastUpdated();
});
```

**Note on `workerStarted` payload:** The monitoring page's initial server render already set progress to `—/N` (from the Razor template). The push doesn't need to reset it — it only needs to update status. Adding `progress: 0` and `totalQuestions` to the push payload would allow cleaner row update but requires looking up totalQuestions in the StartExam GET action (the assessment may not have a UserPackageAssignment yet at that point since it's set up lazily). Safest: `workerStarted` payload includes `{ sessionId, workerName, status }` only; the progress cell stays as initially rendered.

### Row Flash CSS

```css
/* assessment-hub.css (append) */
@keyframes rowFlashUpdate {
    0%   { background-color: #e8f4ff; }
    100% { background-color: transparent; }
}
@keyframes rowFlashComplete {
    0%   { background-color: #d4edda; }
    100% { background-color: transparent; }
}
.flash-update {
    animation: rowFlashUpdate 1s ease-out forwards;
}
.flash-complete {
    animation: rowFlashComplete 1.2s ease-out forwards;
}
```

```javascript
function flashRow(tr, cssClass) {
    if (!tr) return;
    tr.classList.remove('flash-update', 'flash-complete');
    // Force reflow to restart animation
    void tr.offsetWidth;
    tr.classList.add(cssClass);
    setTimeout(function() { tr.classList.remove(cssClass); }, 1300);
}
```

### updateSummaryFromDOM helper

Since push events update one row at a time (not full batch), calling the existing `updateSummary(sessions)` won't work — it expects a full sessions array. Instead, add a DOM-counting version:

```javascript
function updateSummaryFromDOM() {
    var rows = document.querySelectorAll('tbody tr[data-session-id]');
    var completed = 0, inProgress = 0, notStarted = 0, cancelled = 0;
    rows.forEach(function(tr) {
        var badge = tr.querySelector('.status-cell .badge');
        if (!badge) return;
        var text = badge.textContent.trim();
        if (text === 'Completed')   completed++;
        else if (text === 'InProgress') inProgress++;
        else if (text === 'Dibatalkan') cancelled++;
        else notStarted++;
    });
    var total = rows.length;
    var elTotal      = document.getElementById('count-total');
    var elCompleted  = document.getElementById('count-completed');
    var elInProgress = document.getElementById('count-inprogress');
    var elNotStarted = document.getElementById('count-notstarted');
    var elCancelled  = document.getElementById('count-cancelled');
    if (elTotal)      elTotal.textContent      = total;
    if (elCompleted)  elCompleted.textContent  = completed;
    if (elInProgress) elInProgress.textContent = inProgress;
    if (elNotStarted) elNotStarted.textContent = notStarted;
    if (elCancelled)  elCancelled.textContent  = cancelled;
}
```

### toast from monitoring page

assessment-hub.js exposes `showToast` only within its IIFE — it's not accessible from outside. Options:
1. Add `window.showAssessmentToast = showToast` inside the IIFE (1-line change to assessment-hub.js)
2. Use a simple inline toast implementation on the monitoring page

Option 1 is cleaner. Add one line to assessment-hub.js:

```javascript
// In assessment-hub.js, before closing the IIFE:
window.showAssessmentToast = showToast;
```

Then monitoring page uses: `window.showAssessmentToast(message)`.

If modifying assessment-hub.js is undesirable, implement a minimal inline toast on the monitoring page that appends a dismissible Bootstrap alert or a CSS-only toast.

### navPoll and rPoll — Should These Stay?

**Analysis:** `navPoll` (line 588) and `rPoll` (line 627) in StartExam.cshtml are NOT server polls. They poll `Object.keys(pendingSaves).length === 0 && inFlightSaves.size === 0` — a purely client-side condition checking whether pending AJAX answer saves have completed before navigating or submitting. These have nothing to do with server status. They cannot be replaced by SignalR because they're checking client-side AJAX queue state, not server state.

**Recommendation:** Keep navPoll and rPoll unchanged. They are local synchronization guards for the answer-save queue, not polling.

### Polling Removal

**Wave 2 of this phase (after UAT):**

In `StartExam.cshtml`:
- Line 786: Remove `statusPollInterval = setInterval(checkExamStatus, 10000);`
- Lines 740-784: Remove the entire `checkExamStatus` function and its enclosing comment block
- Line 742: Remove `const CHECK_STATUS_URL` declaration
- Line 743: Remove `var statusPollInterval = null` declaration

In `AssessmentMonitoringDetail.cshtml`:
- Line 840: Remove `pollingTimer = setInterval(fetchProgress, 10000);`
- Lines 807-837: Remove `fetchProgress()` function
- Lines 808-809: Remove `pollingActive` guard (also remove `pollingActive = true` and `var pollingActive` at line 625)
- Lines 844: Remove `fetchProgress()` initial call
- Line 825-833: Remove the terminal-state detection inside fetchProgress (this logic should move to workerSubmitted handler — stop countdownTimer when all sessions complete)
- `var pollingTimer = null;` (line 626) — remove
- `var pollingActive = true;` (line 625) — remove
- `id="poll-error"` span (line 326) — remove (no more polling errors)
- `Last updated` timestamp (line 324) — keep (still updated by push events)

In `CMPController.cs`:
- Lines 336-380: Delete the entire `CheckExamStatus` action method

In `AdminController.cs`:
- Lines 2046-2140: Delete the entire `GetMonitoringProgress` action method

### Anti-Patterns to Avoid

- **Pushing before DB write confirms:** SaveAnswer's upsert either `ExecuteUpdateAsync` or `Add + SaveChangesAsync`. Both paths must complete before pushing. Do not push optimistically.
- **Pushing from inside Hub methods:** The Hub (AssessmentHub.cs) handles only group join/leave. All business logic and push calls live in controller actions.
- **Counting answered questions with N+1 queries:** Use `CountAsync` with a WHERE clause — one query, not loading all responses.
- **Calling `updateSummary(sessions)` from event handlers:** That function expects a full sessions array. Use `updateSummaryFromDOM()` for push-driven updates.
- **Forgetting the legacy path in SubmitExam:** SubmitExam has two grading paths (package and legacy). Both need the SignalR push.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Worker name lookup | Custom query | `user.FullName` (already loaded) | `user` is already fetched via `_userManager.GetUserAsync(User)` in StartExam GET |
| Monitor group routing | Custom dictionary of HC connection IDs | `Clients.Group("monitor-{batchKey}")` | SignalR groups are maintained server-side; HC joins on page load |
| Client-side answered count | Increment counter on worker page | `CountAsync` on DB | Concurrent sessions and page reloads mean client counter would drift |
| Row update from push | Full page reload | Call `updateRow()` directly | `updateRow()` is already implemented and tested via polling |

---

## Common Pitfalls

### Pitfall 1: SubmitExam Has Two Code Paths (Package + Legacy)

**What goes wrong:** Only the package path gets the push, so HC sees incomplete real-time updates for legacy-mode assessments.
**Why it happens:** SubmitExam has an `if (packageAssignment != null)` branch (line 1590) and an `else` legacy branch (line 1700). Each has its own `rowsAffected` guard and its own scoring.
**How to avoid:** Add SignalR push in BOTH branches, after each branch's `rowsAffected == 0` check.
**Warning signs:** Legacy assessments don't update HC monitoring in real time; package assessments do.

### Pitfall 2: User FullName Property

**What goes wrong:** Accessing a display name field that doesn't exist or has a different name on ApplicationUser.
**Why it happens:** ApplicationUser extends IdentityUser with project-specific fields. The field name for worker full name must be verified.
**How to avoid:** Check `Models/ApplicationUser.cs` for the FullName or DisplayName property before writing `user.FullName`.
**Warning signs:** Compile error on the field access, or toast shows null/empty name.

### Pitfall 3: monitor group join timing relative to assessment-hub.js

**What goes wrong:** Page-specific script calls `window.assessmentHub.invoke('JoinMonitor', ...)` before the hub has connected, resulting in a SignalR error.
**Why it happens:** `assessment-hub.js` calls `startHub()` which is async. The page script may run while `connection.start()` is still in flight.
**How to avoid:** Wrap JoinMonitor call in a `setTimeout` or check `window.assessmentHub.state === 'Connected'` before invoking. Alternatively, set a `window.onHubConnected` callback in assessment-hub.js that fires after `start()` resolves.

The simplest fix: after the existing `setTimeout(... hubStatusBadge check ..., 2000)`, add JoinMonitor inside the same timeout. This piggybacks on Phase 164's 2-second initial connect wait:

```javascript
setTimeout(function() {
    if (monBadge && window.assessmentHub && window.assessmentHub.state === 'Connected') {
        monBadge.className = 'badge bg-success ms-1 small';
        monBadge.textContent = 'Live';
    }
    // Join monitor group after connection established
    if (window.assessmentBatchKey && window.assessmentHub.state === 'Connected') {
        window.assessmentHub.invoke('JoinMonitor', window.assessmentBatchKey)
            .catch(function(err) { console.warn('[monitor] JoinMonitor failed:', err); });
    }
}, 2000);
```

**Warning signs:** `invoke` call throws "Cannot send data when the connection is not in the 'Connected' state."

### Pitfall 4: pollingActive flag orphaned after polling removal

**What goes wrong:** After removing the polling loop, code that references `pollingActive` (the terminal-state check at line 825-833) is dead but not removed, or vice versa — removal leaves a reference that throws a ReferenceError.
**Why it happens:** The polling removal wave must remove all interdependent polling state: `pollingTimer`, `pollingActive`, `fetchProgress()`, and the Akhiri Semua button hiding logic inside fetchProgress. The terminal-state detection (hide button when all sessions done) must be replicated in the `workerSubmitted` push handler.
**How to avoid:** When removing `fetchProgress`, port its terminal-state check to the `workerSubmitted` handler:

```javascript
window.assessmentHub.on('workerSubmitted', function(payload) {
    // ... update row, flash, toast ...

    // Check if all sessions are now terminal
    var rows = document.querySelectorAll('tbody tr[data-session-id]');
    var allDone = Array.from(rows).every(function(tr) {
        var badge = tr.querySelector('.status-cell .badge');
        if (!badge) return true;
        var text = badge.textContent.trim();
        return text === 'Completed' || text === 'Dibatalkan' || text === 'Abandoned';
    });
    if (allDone) {
        clearInterval(countdownTimer);
        var akhiriBtn = document.getElementById('akhiriSemuaBtn');
        if (akhiriBtn) akhiriBtn.style.display = 'none';
    }
});
```

**Warning signs:** After polling removal, the "Akhiri Semua" button never hides, or `countdownTimer` keeps running after all sessions complete.

### Pitfall 5: batchKey construction in CMPController

**What goes wrong:** The batchKey constructed in CMPController for the monitor group doesn't match the one set by AssessmentMonitoringDetail in ViewBag.AssessmentBatchKey.
**Why it happens:** AdminController's AssessmentMonitoringDetail action sets: `ViewBag.AssessmentBatchKey = $"{title}|{category}|{scheduleDate.Date:yyyy-MM-dd}"` (line 1902). CMPController has `assessment.Title`, `assessment.Category`, and `assessment.Schedule` available. Must use exact same format.
**How to avoid:** Use `$"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}"` — identical to the AdminController pattern.
**Warning signs:** Push fires (no exception) but HC monitoring page doesn't receive events — group name mismatch silently drops the message.

---

## Code Examples

### CMPController Constructor with IHubContext

```csharp
// Add usings at top of CMPController.cs:
using Microsoft.AspNetCore.SignalR;
using HcPortal.Hubs;

// Add field (after _notificationService field):
private readonly IHubContext<AssessmentHub> _hubContext;

// Add parameter (after INotificationService notificationService):
IHubContext<AssessmentHub> hubContext

// Add in constructor body (after _notificationService = notificationService):
_hubContext = hubContext;
```

### SaveAnswer Push

```csharp
// After the if (updatedCount == 0) { ... } block, before return Json(new { success = true }):
var answeredCount = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == sessionId);

var assignment = await _context.UserPackageAssignments
    .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
int totalQuestions = assignment?.GetShuffledQuestionIds().Count ?? 0;

var batchKey = $"{session.Title}|{session.Category}|{session.Schedule.Date:yyyy-MM-dd}";
await _hubContext.Clients.Group($"monitor-{batchKey}")
    .SendAsync("progressUpdate", new { sessionId, progress = answeredCount, totalQuestions });

// Also confirm to the worker
await _hubContext.Clients.User(session.UserId)
    .SendAsync("progressUpdate", new { sessionId, progress = answeredCount, totalQuestions });

return Json(new { success = true });
```

### AssessmentHub JoinMonitor/LeaveMonitor

```csharp
public async Task JoinMonitor(string batchKey)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"monitor-{batchKey}");
}

public async Task LeaveMonitor(string batchKey)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"monitor-{batchKey}");
}
```

### Monitoring Page: JoinMonitor Call in @section Scripts

```javascript
// After the 2-second badge check timeout in AssessmentMonitoringDetail.cshtml @section Scripts:
setTimeout(function() {
    if (monBadge && window.assessmentHub && window.assessmentHub.state === 'Connected') {
        monBadge.className = 'badge bg-success ms-1 small';
        monBadge.textContent = 'Live';
    }
    if (window.assessmentBatchKey && window.assessmentHub.state === 'Connected') {
        window.assessmentHub.invoke('JoinMonitor', window.assessmentBatchKey)
            .catch(function(err) { console.warn('[monitor] JoinMonitor failed:', err); });
    }
}, 2000);
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| 10s poll (GetMonitoringProgress) | SignalR push from SaveAnswer | Phase 165 | Sub-second progress updates instead of up to 10s lag |
| 10s poll (CheckExamStatus) | SignalR push from HC actions | Phase 164 | Already replaced — poll removed in Phase 165 |
| Static monitoring page | Live row updates with flash | Phase 165 | HC sees worker progress in real time |

---

## Open Questions

1. **ApplicationUser.FullName property name**
   - What we know: StartExam GET has `user` loaded. The full name field on ApplicationUser must be verified.
   - What's unclear: Is it `FullName`, `DisplayName`, or something project-specific?
   - Recommendation: Read `Models/ApplicationUser.cs` before coding the workerStarted push.

2. **totalQuestions in workerStarted push**
   - What we know: UserPackageAssignment may not exist yet when StartExam GET fires (it's created lazily within StartExam GET at line 1069).
   - What's unclear: The assignment is created in StartExam GET (lines 1069-1131) before the push. So `totalQuestions` IS available if we read it after assignment creation.
   - Recommendation: Move the push to after the package assignment creation block (line ~1131) so `assignment.GetShuffledQuestionIds().Count` is available. If no package (legacy path), totalQuestions = 0.

3. **Worker-side progressUpdate handler**
   - What we know: CONTEXT.md says push targets both HC AND the worker. StartExam.cshtml currently has no `progressUpdate` handler.
   - What's unclear: What should the worker-side handler do? Presumably update a local progress indicator.
   - Recommendation: The worker page already tracks `answeredCount` client-side. The server push can be used to confirm/sync it. Add a minimal handler that updates any server-confirmed progress display. This is Claude's discretion on implementation.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Playwright (browser-based UAT) + manual verification |
| Config file | None in project |
| Quick run command | Manual: open two browser tabs (HC monitoring + worker exam), trigger each event |
| Full suite command | Manual flow through all 4 scenarios (progress, start, submit, polling removal) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MONITOR-01 | Worker answers question; HC monitoring row updates progress within 1-2s | Manual browser | N/A — requires two live sessions | No test file |
| MONITOR-02 | Worker submits exam; HC monitoring shows "Selesai" + score instantly | Manual browser | N/A — requires two live sessions | No test file |
| MONITOR-03 | Worker opens exam; HC monitoring shows "Dalam Pengerjaan" + row flash | Manual browser | N/A — requires two live sessions | No test file |
| CLEAN-01 | No setInterval calls for status/progress in StartExam or AssessmentMonitoringDetail | Code inspection | `grep -n "setInterval" Views/CMP/StartExam.cshtml` | N/A — grep check |

### Sampling Rate

- **Per task commit:** `dotnet build` — compile check, no runtime errors
- **Per wave merge:** Manual two-browser test of each push scenario
- **Phase gate:** All 4 requirements verified manually before `/gsd:verify-work`

### Wave 0 Gaps

None — no automated test infrastructure needed. Multi-user SignalR push events require live browser sessions and cannot be meaningfully unit tested without a test hub server. Manual verification is the appropriate approach.

---

## Sources

### Primary (HIGH confidence)

- Direct code inspection of `Controllers/CMPController.cs` — constructor (lines 30-50), SaveAnswer (242-288), StartExam GET (975-1267), SubmitExam POST (1546-1699) — exact structure confirmed
- Direct code inspection of `Controllers/AdminController.cs` — GetMonitoringProgress (2046-2140), ViewBag.AssessmentBatchKey assignment (line 1902)
- Direct code inspection of `Views/Admin/AssessmentMonitoringDetail.cshtml` — updateRow() (715-753), updateSummary() (772-790), fetchProgress() (807-837), polling intervals (839-844), @section Scripts badge code (922-946), existing hubStatusBadge HTML (81)
- Direct code inspection of `Views/CMP/StartExam.cshtml` — setInterval inventory (331, 547, 588, 627, 786), navPoll/rPoll purpose (579-631), CheckExamStatus polling (740-786)
- Direct code inspection of `wwwroot/js/assessment-hub.js` — showToast (9-33), onreconnected handler (61-68), startHub (81-92), window.assessmentHub exposure (94)
- Direct code inspection of `Hubs/AssessmentHub.cs` — JoinBatch/LeaveBatch with `"batch-{batchKey}"` group name pattern
- `.planning/phases/165-worker-to-hc-progress-push-polling-removal/165-CONTEXT.md` — all locked decisions

### Secondary (MEDIUM confidence)

- Phase 164 RESEARCH.md — established IHubContext injection pattern, group naming, anti-patterns
- ASP.NET Core SignalR documentation pattern — multiple `onreconnected` registrations stack, not replace

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages, all confirmed present from Phase 163
- Architecture: HIGH — all source files directly inspected; exact line numbers and function signatures confirmed
- Pitfalls: HIGH — derived from actual code reading (two SubmitExam paths, pollingActive cleanup, batchKey format)

**Research date:** 2026-03-13
**Valid until:** 2026-04-13 (stable — no external dependencies)
