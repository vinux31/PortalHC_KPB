# Phase 44: Real-Time Monitoring — Research

## RESEARCH COMPLETE

---

## Codebase Findings

### 1. AssessmentMonitoringDetail.cshtml — Current State

**File:** `Views/CMP/AssessmentMonitoringDetail.cshtml`

Current table columns (8 total): Name, NIP, Jumlah Soal, Status, Score, Result, Completed At, Actions

Key observations:
- **No `data-session-id` on `<tr>` rows** — only on Reshuffle buttons via `data-session-id="@session.Id"`
- **Static page** — no polling, HC must manually refresh
- Uses jQuery `$.ajax` for reshuffle (existing pattern to follow)
- Has `#reshuffleForm` hidden form with antiforgery token (package mode only)
- `"Tutup Lebih Awal"` button rendered with `@if (Model.GroupStatus == "Open")` — needs JS-side hiding after all complete
- Summary cards (Total, Completed%, PassRate%) rendered statically with server-side values — need JS update targets
- Empty-row colspan is `colspan="8"`

**New column layout (still 8 columns):**
| Old | New |
|-----|-----|
| Name | Name (keep) |
| NIP | **Removed** |
| Jumlah Soal | **Progress** (format: `X/Total`) |
| Status | Status (keep, live) |
| Score | Score (keep, live) |
| Result | Result (keep, live) |
| Completed At | Completed At (keep) |
| *(none)* | **Time Remaining** (new) |
| Actions | Actions (keep, live) |

Total: still 8 columns. colspan="8" stays unchanged.

---

### 2. AssessmentMonitoringDetail Controller Action (line 394+)

**File:** `Controllers/CMPController.cs`

The action already:
- Accepts `title`, `category`, `scheduleDate` as query params (GET)
- Loads sessions via EF Core, includes User
- Detects package mode by checking `AssessmentPackages` table
- Builds `questionCountMap` (total questions per session):
  - Package mode: `UserPackageAssignments.Join(AssessmentPackages.Include(p => p.Questions))` → count
  - Legacy mode: count `AssessmentQuestions` per session
- Computes 4-state `userStatus`: Completed → Abandoned → InProgress (StartedAt != null) → Not started

**The `GetMonitoringProgress` endpoint will reuse this same logic** for question count + add answered count.

---

### 3. Data Model

**AssessmentSession** (key fields for Phase 44):
- `DurationMinutes` — exam time limit
- `ElapsedSeconds` — seconds worker has spent in exam (updated every 30s + page nav via Phase 43 polling)
- `StartedAt` — null = not started; non-null = InProgress
- `CompletedAt` — null = not done; non-null = completed
- `Score` — nullable int (0–100)
- `IsPassed` — nullable bool
- `PassPercentage` — the passing threshold (0–100)
- `Status` — "Open", "Upcoming", "Completed", "Abandoned"

**PackageUserResponse** (for answered count):
- `AssessmentSessionId` (FK)
- `PackageQuestionId` (FK)
- One row per answered question per session (auto-save from Phase 41)
- `COUNT(PackageUserResponse WHERE sessionId = X)` = answered count

**UserPackageAssignment**:
- `AssessmentSessionId`, `AssessmentPackageId`, `SavedQuestionCount` (nullable int)
- Used by existing questionCountMap pattern to get total questions

---

### 4. GetMonitoringProgress Endpoint Design

**Route:** `GET /CMP/GetMonitoringProgress?title=&category=&scheduleDate=`

**Authorization:** `[Authorize(Roles = "Admin, HC")]`

**Input params:** Same 3 as AssessmentMonitoringDetail (string title, string category, DateTime scheduleDate)

**Query strategy:**
```csharp
// Step 1: load sessions (same as AssessmentMonitoringDetail)
var sessions = await _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Title == title && a.Category == category && a.Schedule.Date == scheduleDate.Date)
    .ToListAsync();

// Step 2: get answered count per session via GROUP BY (not N+1)
var siblingIds = sessions.Select(s => s.Id).ToList();
var answeredCountMap = await _context.PackageUserResponses
    .Where(p => siblingIds.Contains(p.AssessmentSessionId))
    .GroupBy(p => p.AssessmentSessionId)
    .ToDictionaryAsync(g => g.Key, g => g.Count());

// Step 3: get total question count (same as AssessmentMonitoringDetail questionCountMap)
// (detect package mode + build questionCountMap)

// Step 4: project to DTO
```

**DTO shape (anonymous object or named class):**
```json
{
  "sessionId": 42,
  "status": "InProgress",
  "progress": 7,
  "totalQuestions": 20,
  "score": null,
  "result": null,
  "remainingSeconds": 345,
  "completedAt": null
}
```

**RemainingSeconds calculation:**
- InProgress: `Math.Max(0, (session.DurationMinutes * 60) - session.ElapsedSeconds)`
- Not Started / Completed / Abandoned: `null`

**Result field:**
- `"Pass"` if IsPassed == true
- `"Fail"` if IsPassed == false
- `null` if IsPassed is null (not yet determined)

**Progress for legacy exams (no package):**
- `totalQuestions` = from legacyQuestionCountMap (count AssessmentQuestions)
- `progress` = count `UserResponses` WHERE sessionId = X (legacy answered count)
- Note: legacy path uses `UserResponse` not `PackageUserResponse`

---

### 5. Frontend Architecture

**Existing jQuery pattern in the view:**
The view already uses `$.ajax` for reshuffle. New polling should use the native `fetch` API (consistent with Phase 41/42/43 pattern — Fetch API not jQuery for new endpoints).

**Two intervals:**
1. `setInterval(fetchProgress, 10000)` — 10s polling
2. `setInterval(tickCountdowns, 1000)` — 1s countdown tick

**Client-side state:**
```js
let countdownMap = {}; // { sessionId: remainingSeconds }
let pollingActive = true;
let pollingTimer = null;
let countdownTimer = null;
```

**fetchProgress() function:**
```js
async function fetchProgress() {
    try {
        const resp = await fetch('/CMP/GetMonitoringProgress?title=...&category=...&scheduleDate=...');
        const sessions = await resp.json();
        sessions.forEach(updateRow);
        updateSummary(sessions);
        updateLastUpdated();
        if (sessions.every(s => s.status === 'Completed')) {
            clearInterval(pollingTimer);
            clearInterval(countdownTimer);
            pollingActive = false;
        }
    } catch (e) {
        showErrorIndicator();
    }
}
```

**DOM targeting:**
- Each `<tr>`: `data-session-id="{sessionId}"`
- Status cell: `<td class="status-cell">` (or use td index)
- Progress cell, Score cell, Result cell, Time Remaining cell — identified by position or class
- Summary: `<span id="count-total">`, `<span id="count-completed">`, `<span id="count-inprogress">`, `<span id="count-notstarted">`
- Last updated: `<span id="last-updated-time">`
- Close Early button: `id="closeEarlyBtn"` (already has `data-bs-toggle` modal)

**updateRow(session):**
```js
function updateRow(session) {
    const tr = document.querySelector(`tr[data-session-id="${session.sessionId}"]`);
    if (!tr) return;
    // update status badge
    // update progress text
    // update score text
    // update result text
    // update time remaining cell (display from countdownMap)
    // update actions buttons (swap ForceClose → ViewResults+Reset on Completed)
    countdownMap[session.sessionId] = session.remainingSeconds; // re-sync
}
```

**tickCountdowns():**
```js
function tickCountdowns() {
    Object.keys(countdownMap).forEach(id => {
        if (countdownMap[id] !== null && countdownMap[id] > 0) {
            countdownMap[id]--;
            const tr = document.querySelector(`tr[data-session-id="${id}"]`);
            if (tr) updateTimeCell(tr, countdownMap[id]);
        }
    });
}
```

**Actions update on status change:**
- Status "Completed": replace ForceClose with `<a href="/CMP/Results/{sessionId}" target="_blank" class="btn btn-success btn-sm">View Results</a>` + Reset form
- Status "Abandoned": Reset form only (no View Results)
- Antiforgery token for Reset/ForceClose: read from existing `#reshuffleForm` (always available since it's package mode), or add a separate hidden form

**Challenge: Antiforgery for inline Reset/ForceClose forms**
The current actions are rendered server-side as `<form>` elements with `@Html.AntiForgeryToken()`. For JS-rendered action buttons, need antiforgery token.

**Approach:** Keep the existing server-rendered forms for initial state. On status change (via JS update), rebuild the actions HTML string using the antiforgery token captured once at page load (same as reshuffle pattern: `document.querySelector('#reshuffleForm input[name="__RequestVerificationToken"]').value`). The `#reshuffleForm` is only present in package mode. For non-package mode, add a global hidden token form.

**Better approach:** Add a global `id="antiforgeryForm"` with `@Html.AntiForgeryToken()` (outside the `@if (Model.IsPackageMode)` block) so token is always available.

**"Tutup Lebih Awal" hiding:**
- Keep server-side `@if (Model.GroupStatus == "Open")` Razor check (controls initial render)
- In JS after each poll: `if (sessions.every(s => s.status === 'Completed')) document.getElementById('closeEarlyBtn')?.style.display = 'none'`
- Add `id="closeEarlyBtn"` to the Tutup Lebih Awal `<button>`

---

### 6. Page Query String Access

The page URL is `/CMP/AssessmentMonitoringDetail?title=...&category=...&scheduleDate=...`

To read these in JS:
```js
const params = new URLSearchParams(window.location.search);
const title = params.get('title');
const category = params.get('category');
const scheduleDate = params.get('scheduleDate');
```

Or embed them in hidden fields:
```html
<input type="hidden" id="hTitle" value="@Model.Title" />
<input type="hidden" id="hCategory" value="@Model.Category" />
<input type="hidden" id="hScheduleDate" value="@Model.Schedule.ToString("yyyy-MM-dd")" />
```

The hidden field approach is safer (avoids URL encoding issues with special chars in title).
Already used: `Model.Schedule.ToString("yyyy-MM-dd")` pattern exists in CloseEarly form inputs.

---

### 7. Summary Cards Update

Current summary cards show: Total Assigned, Completed (X%), Passed (X%)

The CONTEXT.md says update "total workers, completed, in-progress, not-started" counts.

For live updates, the summary cards need id attributes on the dynamic numbers:
- `<span id="count-total">@Model.TotalCount</span>`
- `<span id="count-completed">@Model.CompletedCount</span>`
- Also add InProgress and Not Started counts (not in current cards)

The CONTEXT.md mentions updating the summary counts. The simplest approach is updating the existing 3 cards (Total, Completed, Passed) and adding 2 more (InProgress, Not Started) to show the full picture during an active exam.

Actually, the CONTEXT.md says "total workers, completed, in-progress, not-started". This means the cards should be rearranged. Current 3 cards: Total Assigned, Completed, Pass Rate. New layout could be: Total, Completed, InProgress, Not Started (4 cards), with Pass Rate computed from data.

Or keep existing structure and just add id attributes for JS updates to the existing spans, plus show progress counts. The CONTEXT.md doesn't specify the exact visual layout of summary cards — just that the counts update.

**Decision for planner:** Keep the 3-card layout as-is (Total, Completed, PassRate) with id tags for JS updates. No card restructuring needed — CONTEXT.md didn't specify it.

---

### 8. "Last Updated" Indicator

Add below the table or in the card header:
```html
<small class="text-muted" id="last-updated">
    Last updated: <span id="last-updated-time">—</span>
</small>
```

Update on each successful fetch:
```js
document.getElementById('last-updated-time').textContent = new Date().toLocaleTimeString('id-ID');
```

---

### 9. Error Indicator on Fetch Failure

Small, non-disruptive indicator. Options:
1. Small badge in the header area: `<span id="poll-error" class="badge bg-warning text-dark" style="display:none">⚠ Update error</span>`
2. Show on failure, hide on next success

---

### 10. Plan Structure

**Plan 01: Backend** (Wave 1) — `GetMonitoringProgress` endpoint
- New `[HttpGet][Authorize(Roles = "Admin, HC")]` action in CMPController
- AnsweredCountMap via PackageUserResponse GROUP BY (same pattern as other count queries)
- LegacyAnsweredCountMap via UserResponse GROUP BY (for legacy mode)
- DTO fields: sessionId, status, progress, totalQuestions, score, result, remainingSeconds, completedAt
- Return `Json(dtos)`
- No migration needed
- Files: `Controllers/CMPController.cs` only

**Plan 02: Frontend polling** (Wave 2, depends on Plan 01)
- View modifications: `data-session-id` on `<tr>`, remove NIP column, rename "Jumlah Soal" → "Progress", add "Time Remaining" column, add id attributes for summary cards, add global antiforgery hidden form, add `id="closeEarlyBtn"`, add "Last updated" indicator
- JS block: fetchProgress (10s interval), tickCountdowns (1s interval), updateRow, updateSummary, updateLastUpdated, showErrorIndicator
- JS reads title/category/scheduleDate from hidden fields
- Files: `Views/CMP/AssessmentMonitoringDetail.cshtml` only

No new ViewModel class needed for the endpoint — anonymous object or add `MonitoringProgressDto` to AssessmentMonitoringViewModel.cs.
