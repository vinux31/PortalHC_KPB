# Architecture: v2.1 Assessment Resilience & Real-Time Monitoring

**Project:** PortalHC Online Assessment System — Phases 41-45
**Researched:** 2026-02-24
**Overall Confidence:** HIGH

---

## Executive Summary

v2.1 adds four interconnected features (auto-save, session resume, worker polling, HC live monitoring) to the existing monolithic CMPController architecture. The good news: all features leverage **existing endpoints** (SaveAnswer, CheckExamStatus) that are already in place. The challenge: these features introduce concurrency, caching, and state management concerns that require coordinated changes across the controller, database schema, and client-side JavaScript.

**Key architectural decision:** Do NOT refactor CMPController into separate API endpoints. Instead, enhance existing endpoints with:
1. Atomic upsert patterns (SaveAnswer)
2. State tracking fields (AssessmentSession.LastPageIndex, ElapsedSeconds)
3. Memory caching layer (CheckExamStatus, GetMonitoringProgress)
4. Concurrency tokens (RowVersion on AssessmentSession)

This minimizes refactoring risk while solving all four feature requirements.

---

## Current Architecture Overview

### Core Components

```
┌─────────────────────────────────────────────────────────────┐
│                    HTTP Routing Layer                        │
│         (ASP.NET Core routing → CMPController)              │
└────────────────────────┬────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
   ┌────▼──────┐  ┌──────▼──────┐  ┌────▼──────┐
   │Assessment │  │  SaveAnswer  │  │CheckExam  │
   │(GET)      │  │  (POST)      │  │Status(GET)│
   │1700+lines │  │AJAX endpoint │  │Polling    │
   └────────────┘  └──────────────┘  └───────────┘
        │                │                │
        │                │                │
        └────────┬───────┴────────────────┘
                 │
        ┌────────▼──────────────┐
        │  CMPController        │
        │  (~2700 lines)        │
        │                       │
        │ Key Methods:          │
        │ • Assessment()        │
        │ • StartExam()         │
        │ • SubmitExam()        │
        │ • SaveAnswer()        │
        │ • CheckExamStatus()   │
        │ • GetMonitorData()    │
        │ • CloseEarlySession() │
        └────────┬──────────────┘
                 │
        ┌────────▼────────────────────┐
        │  ApplicationDbContext        │
        │  (EF Core DbContext)         │
        │                              │
        │ Key DbSets:                  │
        │ • AssessmentSessions         │
        │ • PackageUserResponses       │
        │ • AssessmentPackages         │
        │ • AssessmentQuestions        │
        │ • ApplicationUsers           │
        └────────┬────────────────────┘
                 │
        ┌────────▼────────────────────┐
        │  SQL Server Database         │
        │  (via EF Core migrations)    │
        └─────────────────────────────┘
```

### Existing Data Models (Relevant to v2.1)

**AssessmentSession** (exam session state)
```csharp
public class AssessmentSession
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Title { get; set; }
    public string Category { get; set; }
    public DateTime Schedule { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; }  // "Open", "InProgress", "Completed", "Abandoned", "Upcoming"
    public int? Score { get; set; }
    public bool? IsPassed { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExamWindowCloseDate { get; set; }
    public int PassPercentage { get; set; }
    public bool AllowAnswerReview { get; set; }
    // ...
}
```

**PackageUserResponse** (per-question answer storage)
```csharp
public class PackageUserResponse
{
    public int Id { get; set; }
    public int AssessmentSessionId { get; set; }
    public int PackageQuestionId { get; set; }
    public int? PackageOptionId { get; set; }  // Nullable: unanswered questions
    public DateTime SubmittedAt { get; set; }
}
```

### Existing Endpoints (Already in CMPController)

**SaveAnswer (POST) — Lines 1033–1071**
- **Current pattern:** Upsert (FirstOrDefaultAsync + Add or Update)
- **Input:** `sessionId, questionId, optionId`
- **Output:** `{ success: bool, error?: string }`
- **Security:** User ownership check
- **Used by:** Exam.cshtml via AJAX

**CheckExamStatus (GET) — Lines 1075–1102**
- **Current pattern:** Query Status + ExamWindowCloseDate
- **Input:** `sessionId`
- **Output:** `{ closed: bool, redirectUrl?: string }`
- **Security:** User ownership check
- **Used by:** Exam.cshtml polling loop (already in place)

**GetMonitorData (GET) — Lines 280–380+**
- **Current pattern:** Query all sessions, group by (Title, Category, Schedule.Date)
- **Input:** None (HC only)
- **Output:** Grouped JSON with per-user status
- **Security:** HC-only role check
- **Used by:** AssessmentMonitoringDetail.cshtml initial load

---

## v2.1 Feature Integration

### Feature 1: Auto-Save (Phase 41)

**What it adds:** JavaScript on exam page auto-saves answers as user selects options.

#### Integration Points

**1. Exam.cshtml (Razor view) — Enhanced**
- Existing: Radio buttons for each option
- New: Event handler on `change` (radio selection) + on Prev/Next click
- Calls: Existing `SaveAnswer` endpoint via AJAX with debounce

**2. SaveAnswer endpoint (CMPController) — Enhanced**
```csharp
// Current (unsafe for concurrent calls):
var existing = await _context.PackageUserResponses
    .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId
                           && r.PackageQuestionId == questionId);
if (existing != null)
    existing.PackageOptionId = optionId;
else
    _context.PackageUserResponses.Add(...);
await _context.SaveChangesAsync();

// Enhanced (atomic):
await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == sessionId
             && r.PackageQuestionId == questionId)
    .ExecuteUpdateAsync(r =>
        r.SetProperty(e => e.PackageOptionId, optionId)
         .SetProperty(e => e.SubmittedAt, DateTime.UtcNow));
```

**3. PackageUserResponse model — New properties**
- Add: `[Timestamp] byte[] RowVersion` — SQL Server rowversion for concurrency detection
- Add: Database unique constraint via migration: `UNIQUE(AssessmentSessionId, PackageQuestionId)`

#### Data Flow

```
User clicks radio button
        ↓
Exam.cshtml: debounce(300ms) → queue AJAX
        ↓
SaveAnswer(sessionId, questionId, optionId)
        ↓
ExecuteUpdateAsync: atomic upsert
   WHERE (sessionId, questionId) = input
   SET OptionId = input, SubmittedAt = now
   RowVersion incremented automatically by SQL Server
        ↓
Unique constraint prevents:
   INSERT new if (sessionId, questionId) already exists
        ↓
DB returns: 1 row updated (or DbUpdateException if constraint violated)
        ↓
Return { success: true } to client
        ↓
UI shows "saved" indicator (optional)
```

#### Components Affected

| Component | Change | Complexity | Breaking? |
|-----------|--------|-----------|-----------|
| Exam.cshtml | Add debounce + event handlers | Low | No |
| CMPController.SaveAnswer | Replace upsert pattern with ExecuteUpdateAsync | Medium | No |
| PackageUserResponse model | Add RowVersion property | Low | No |
| Migration | Create unique constraint, add RowVersion | Medium | No |

**Why RowVersion in Phase 41?**
- Needed for Phase 44 (close-early concurrency safety)
- Easier to add now with SaveAnswer refactor
- Enables concurrent answer saves without corruption

---

### Feature 2: Session Resume (Phase 42)

**What it adds:** When worker returns to an exam, system remembers which page/question they were on and how much time has elapsed.

#### Integration Points

**1. AssessmentSession model — New properties**
```csharp
public int LastPageIndex { get; set; } = 1;      // 1-based page number
public int ElapsedSeconds { get; set; } = 0;     // Server-tracked elapsed time
// RowVersion added in Phase 41
```

**2. UpdateSessionProgress endpoint (NEW)**
```csharp
[HttpPost]
public async Task<IActionResult> UpdateSessionProgress(int sessionId, int pageIndex, int elapsedSeconds)
{
    var session = await _context.AssessmentSessions.FindAsync(sessionId);
    if (session == null) return NotFound();

    var user = await _userManager.GetUserAsync(User);
    if (session.UserId != user.Id) return Forbid();

    // Security: Validate question count hasn't changed mid-exam
    var packages = await _context.AssessmentPackages
        .Where(p => p.AssessmentSessionId == sessionId)
        .SelectMany(p => p.Questions)
        .CountAsync();

    if (packages != session.StoredQuestionCount)
        return Json(new { success = false, error = "Question set changed" });

    // Update session state
    session.LastPageIndex = pageIndex;
    session.ElapsedSeconds = Math.Max(session.ElapsedSeconds, elapsedSeconds);
    await _context.SaveChangesAsync();

    return Json(new { success = true });
}
```

**3. StartExam endpoint (existing) — Enhanced**
```csharp
// After loading session:
if (session.StartedAt == null)
{
    // First entry: initialize
    session.StartedAt = DateTime.UtcNow;
    session.Status = "InProgress";
    session.LastPageIndex = 1;
    session.ElapsedSeconds = 0;
    await _context.SaveChangesAsync();
}

// Resume: populate ViewBag for client
ViewBag.LastPageIndex = session.LastPageIndex;
var remaining = (session.DurationMinutes * 60) - session.ElapsedSeconds;
ViewBag.RemainingSeconds = remaining;
```

**4. Exam.cshtml (Razor view) — Enhanced**
```html
<!-- On page load -->
<script>
  const lastPageIndex = @ViewBag.LastPageIndex;
  const remainingSeconds = @ViewBag.RemainingSeconds;

  // Jump to stored page on load
  scrollToPage(lastPageIndex);

  // Set timer display
  updateTimerDisplay(remainingSeconds);
</script>

<!-- On page navigation (Prev/Next click) -->
<script>
  async function onPageChange(newPageIndex) {
    // Before navigating, save page progress
    const elapsed = originalStartTime ?
      Math.floor((Date.now() - originalStartTime) / 1000) : 0;

    const response = await fetch('/CMP/UpdateSessionProgress', {
      method: 'POST',
      body: JSON.stringify({ sessionId, pageIndex: newPageIndex, elapsedSeconds: elapsed }),
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token }
    });

    if (response.ok) {
      navigateToPage(newPageIndex);
    }
  }
</script>
```

#### Data Flow

```
Worker enters exam (StartExam):
  If StartedAt is null:
    Set StartedAt = now
    Set Status = "InProgress"
    Set LastPageIndex = 1
    Set ElapsedSeconds = 0
    Save to DB
  Else:
    Load existing LastPageIndex + ElapsedSeconds

Render Exam.cshtml with:
  ViewBag.LastPageIndex = session.LastPageIndex
  ViewBag.RemainingSeconds = (DurationMinutes * 60) - ElapsedSeconds

Worker navigates pages (Prev/Next):
  JS calculates: elapsed = (currentTime - originalStartTime)
  AJAX POST to UpdateSessionProgress:
    { sessionId, pageIndex, elapsedSeconds }

  UpdateSessionProgress:
    Validate question count unchanged
    Update: LastPageIndex = pageIndex
    Update: ElapsedSeconds = max(stored, client_sent)
    Return success

  On callback, JS navigates to new page

Worker submits exam (SubmitExam):
  Calculate actual elapsed = DateTime.UtcNow - StartedAt
  If elapsed > DurationMinutes * 60 + gracePeriod:
    Reject (overtime not allowed)
  Else:
    Grade answers from PackageUserResponses
    Set CompletedAt, Status = "Completed"
```

#### Components Affected

| Component | Change | Complexity | Breaking? |
|-----------|--------|-----------|-----------|
| AssessmentSession model | Add LastPageIndex, ElapsedSeconds | Low | No |
| CMPController.UpdateSessionProgress | New endpoint | Medium | N/A |
| CMPController.StartExam | Enhance ViewBag population | Low | No |
| Exam.cshtml | Add JS for page tracking + timer UI | Medium | No |
| Migration | Add 2 columns to AssessmentSessions | Low | No |

#### Security Considerations

- **Client timer is not authoritative:** Server uses `DateTime.UtcNow - StartedAt` for final validation
- **ElapsedSeconds stored server-side:** Client sends only as hint; server uses `max(stored, client)` to prevent time reduction
- **Timer UI is decorative:** DevTools cannot manipulate actual exam time; only affects UX display
- **Question count frozen:** Detect mid-exam changes; block resume if changed

---

### Feature 3: Worker Polling (Phase 43)

**What it adds:** Worker JS polls CheckExamStatus every 10-30 seconds to detect if HC closes exam early.

#### Integration Points

**1. CheckExamStatus endpoint (existing) — Enhanced with caching**
```csharp
[HttpGet]
public async Task<IActionResult> CheckExamStatus(int sessionId)
{
    var cacheKey = $"exam_status_{sessionId}";

    if (_cache.TryGetValue(cacheKey, out var cachedResult))
    {
        return Json(cachedResult);
    }

    var session = await _context.AssessmentSessions.FindAsync(sessionId);
    if (session == null) return NotFound();

    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    if (session.UserId != user.Id) return Json(new { closed = false });

    bool isClosed = session.ExamWindowCloseDate.HasValue &&
                    DateTime.UtcNow > session.ExamWindowCloseDate.Value ||
                    session.Status == "Completed";

    var result = new {
        closed = isClosed,
        redirectUrl = isClosed ? Url.Action("Results", new { id = sessionId }) : ""
    };

    // Cache for 5 seconds (balance between freshness and DB load)
    _cache.Set(cacheKey, result, TimeSpan.FromSeconds(5));

    return Json(result);
}
```

**2. CloseEarlySession endpoint (existing) — Enhanced with cache invalidation**
```csharp
// After updating database:
// Clear cache for all affected session IDs
var sessionIds = /* affected sessions */;
foreach (var id in sessionIds)
{
    _cache.Remove($"exam_status_{id}");
}
```

**3. IMemoryCache service (infrastructure)**
- Add to Startup.cs: `services.AddMemoryCache()`
- Inject into CMPController: `public CMPController(..., IMemoryCache cache)`

**4. Exam.cshtml (Razor view) — Enhanced with polling**
```javascript
// Start polling when exam page loads
const POLLING_INTERVAL_MS = 10000;  // 10 seconds
const sessionId = @Model.Id;

setInterval(async () => {
  try {
    const response = await fetch(`/CMP/CheckExamStatus?sessionId=${sessionId}`);
    const data = await response.json();

    if (data.closed) {
      // HC closed the exam; redirect to results
      window.location.href = data.redirectUrl;
    }
  } catch (error) {
    console.warn('Polling failed:', error);
    // Retry on next interval; don't block exam
  }
}, POLLING_INTERVAL_MS);
```

#### Data Flow

```
Worker loads Exam.cshtml:
  JS timer starts (countdown for UX)
  JS polling interval starts (every 10-30 seconds)

Every 10-30 seconds, JS polls:
  GET /CMP/CheckExamStatus?sessionId=123

CheckExamStatus endpoint:
  Check cache: "exam_status_123"
  Cache hit (within 5 seconds):
    Return cached { closed, redirectUrl }
    No DB hit
  Cache miss:
    Query DB: SELECT Status, ExamWindowCloseDate
    Check: isClosed = (ExamWindowCloseDate < now) OR (Status == "Completed")
    Cache result for 5 seconds
    Return result

If worker's polling returns closed=true:
  JS redirects to /CMP/Results
  Worker sees final score

If HC clicks "Close Early":
  CloseEarlySession():
    Identify affected sessions (group by Title, Category, Schedule)
    For each:
      Calculate score
      Update Status = "Completed"
      Update ExamWindowCloseDate = now
    Clear cache: _cache.Remove("exam_status_" + sessionId)

Next worker polling hits DB (cache expired), gets updated status
```

#### Caching Strategy

| Metric | Value | Rationale |
|--------|-------|-----------|
| Cache TTL | 5 seconds | Balance between freshness (HC closes, worker sees within 5s) and DB load |
| Polling interval | 10–30 seconds | Worker checks every 10-30s; 5s cache = 2-6 cache hits per poll |
| Cache invalidation | On CloseEarlySession | Ensures fresh status after HC action |
| Cache key | exam_status_{sessionId} | Per-session granularity for targeted invalidation |

**Load impact:**
- 100 workers polling every 10 seconds = 10 polls/sec
- With 5s cache: ~2 DB hits/sec (much better than 10/sec without cache)
- At 300 workers: 6 DB hits/sec (still manageable)
- Without cache: 30 DB hits/sec (overwhelms DB)

#### Components Affected

| Component | Change | Complexity | Breaking? |
|-----------|--------|-----------|-----------|
| CMPController.CheckExamStatus | Add caching logic | Low | No |
| CMPController.CloseEarlySession | Add cache invalidation | Low | No |
| Exam.cshtml | Add polling interval JS | Low | No |
| Startup.cs | Register IMemoryCache | Low | No |

**No database schema changes for this feature.**

---

### Feature 4: HC Live Monitoring (Phase 45)

**What it adds:** HC's monitoring dashboard shows real-time progress (# of answers submitted) for each worker.

#### Integration Points

**1. GetMonitoringProgress endpoint (NEW)**
```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> GetMonitoringProgress(string title, string category, DateTime scheduleDate)
{
    var cacheKey = $"monitoring_{title}_{category}_{scheduleDate:yyyy-MM-dd}";

    if (_cache.TryGetValue(cacheKey, out var cachedResult))
    {
        return Json(cachedResult);
    }

    // Query progress for all workers in this assessment group
    var groupDate = scheduleDate.Date;
    var data = await _context.AssessmentSessions
        .Where(s => s.Title == title && s.Category == category && s.Schedule.Date == groupDate)
        .Select(s => new
        {
            userId = s.UserId,
            userFullName = s.User.FullName,
            userNip = s.User.NIP,
            sessionId = s.Id,
            answeredCount = _context.PackageUserResponses
                .Count(r => r.AssessmentSessionId == s.Id && r.PackageOptionId != null),
            totalQuestions = _context.AssessmentPackages
                .Where(p => p.AssessmentSessionId == s.Id)
                .SelectMany(p => p.Questions)
                .Count(),
            status = s.Status
        })
        .ToListAsync();

    // Cache for 5-10 seconds
    _cache.Set(cacheKey, data, TimeSpan.FromSeconds(10));

    return Json(data);
}
```

**2. AssessmentMonitoringDetail.cshtml — Enhanced**
```html
<!-- Progress column added to table -->
<table>
  <thead>
    <tr>
      <th>User</th>
      <th>Status</th>
      <th>Progress</th>  <!-- NEW -->
      <th>Score</th>
    </tr>
  </thead>
  <tbody id="monitoringTableBody">
    <!-- Populated by JS -->
  </tbody>
</table>

<!-- Auto-refresh JS -->
<script>
  const MONITORING_REFRESH_MS = 5000;  // 5 seconds
  const title = '@Model.Title';
  const category = '@Model.Category';
  const scheduleDate = '@Model.ScheduleDate.Date:yyyy-MM-dd';

  setInterval(async () => {
    const response = await fetch(
      `/CMP/GetMonitoringProgress?title=${encodeURIComponent(title)}&category=${category}&scheduleDate=${scheduleDate}`
    );
    const data = await response.json();

    // Update table rows in-place
    updateMonitoringTable(data);
  }, MONITORING_REFRESH_MS);

  function updateMonitoringTable(data) {
    const tbody = document.getElementById('monitoringTableBody');

    data.forEach(row => {
      const existingRow = tbody.querySelector(`[data-user-id="${row.userId}"]`);
      const progress = `${row.answeredCount}/${row.totalQuestions}`;

      if (existingRow) {
        existingRow.querySelector('.progress-cell').textContent = progress;
      } else {
        // Insert new row if worker just appeared
        const newRow = document.createElement('tr');
        newRow.setAttribute('data-user-id', row.userId);
        newRow.innerHTML = `<td>${row.userFullName}</td>
                            <td>${row.status}</td>
                            <td class="progress-cell">${progress}</td>
                            <td>-</td>`;
        tbody.appendChild(newRow);
      }
    });
  }
</script>
```

**3. Assessment.cshtml (existing) — No changes**
- GetMonitorData still used for initial page load
- GetMonitoringProgress used only for periodic refreshes

#### Data Flow

```
HC navigates to Assessment → Monitoring tab:
  GET /CMP/Assessment?view=monitoring

Assessment action renders initial table via GetMonitorData
  Shows all sessions grouped by (Title, Category, Schedule.Date)
  Shows initial "Answered/Total" counts

AssessmentMonitoringDetail.cshtml loads:
  JS sets up auto-refresh interval (5-10 seconds)

Every 5-10 seconds, JS polls:
  GET /CMP/GetMonitoringProgress?title=...&category=...&scheduleDate=...

GetMonitoringProgress endpoint:
  Check cache: "monitoring_{title}_{category}_{date}"
  Cache hit (within 10 seconds):
    Return cached { userId, answeredCount, totalQuestions, ... }
  Cache miss:
    Query DB:
      SELECT s.Id, s.UserId, s.User.FullName, u.NIP
      FROM AssessmentSessions s
      JOIN Users u ON s.UserId = u.Id
      WHERE s.Title = title AND s.Category = category AND s.Schedule.Date = date

      For each session:
        SELECT COUNT(*) FROM PackageUserResponses
        WHERE SessionId = s.Id AND PackageOptionId IS NOT NULL
        (count non-null answers only)

    SELECT COUNT(*) FROM AssessmentPackages
    WHERE AssessmentSessionId = s.Id
    (count total questions)

    Cache 10 seconds

JS receives response:
  Update table rows: answered/total counts
  Highlight sessions that just completed
  Preserve scroll position

If HC clicks "Close Early":
  CloseEarlySession() invalidates cache:
    _cache.Remove("monitoring_{title}_{category}_{date}")

Next HC monitoring refresh (within 10 seconds):
  DB query executes, shows updated status
```

#### Components Affected

| Component | Change | Complexity | Breaking? |
|-----------|--------|-----------|-----------|
| CMPController.GetMonitoringProgress | New endpoint | Medium | N/A |
| AssessmentMonitoringDetail.cshtml | Add Progress column + refresh JS | Medium | No |
| Startup.cs | IMemoryCache already registered (Phase 43) | Low | No |

**No database schema changes for this feature.**

#### Progress Count Definition

- **Count:** Number of PackageUserResponses where PackageOptionId IS NOT NULL
- **Total:** Number of AssessmentQuestions in the package
- **Display:** "XX/YY answered"
- **Unanswered questions:** Excluded from count (they have PackageOptionId = null or no response row)

---

## New vs. Modified Components Summary

### Database Schema Changes

| Change | Phase | EF Core Pattern | Migration Impact |
|--------|-------|-----------------|------------------|
| Add RowVersion to AssessmentSession | 41 | `[Timestamp] byte[] RowVersion` | +8 bytes per row |
| Add RowVersion to PackageUserResponse | 41 | `[Timestamp] byte[] RowVersion` | +8 bytes per row |
| Add unique constraint on (SessionId, QuestionId) | 41 | `HasIndex(...).IsUnique()` | Blocks duplicates |
| Add LastPageIndex to AssessmentSession | 42 | `int LastPageIndex = 1` | +4 bytes per row |
| Add ElapsedSeconds to AssessmentSession | 42 | `int ElapsedSeconds = 0` | +4 bytes per row |

### Code Components

| Component | Type | Phase | Change Scope | Lines |
|-----------|------|-------|--------------|-------|
| Debounce utility | JS function | 41 | New | +20 |
| SaveAnswer | Endpoint | 41 | Enhanced | +10 |
| UpdateSessionProgress | Endpoint | 42 | New | +30 |
| StartExam | Endpoint | 42 | Enhanced | +10 |
| CheckExamStatus | Endpoint | 43 | Enhanced | +15 |
| CloseEarlySession | Endpoint | 43 | Enhanced | +5 |
| Polling interval | JS function | 43 | New | +15 |
| GetMonitoringProgress | Endpoint | 45 | New | +40 |
| Progress refresh | JS function | 45 | New | +25 |

### Exam.cshtml JavaScript Changes

| Phase | Feature | JS LOC | Complexity |
|-------|---------|--------|-----------|
| 41 | Auto-save debounce + event handlers | +20 | Low |
| 42 | Page tracking + timer UI | +20 | Medium |
| 43 | Polling interval | +15 | Low |

### AssessmentMonitoringDetail.cshtml Changes

| Phase | Feature | Changes |
|-------|---------|---------|
| 45 | Progress column + refresh loop | Add 1 column, +25 JS LOC |

---

## Data Flow Architecture

### Current: Submit-Only Model
```
User clicks option
        ↓
User manually navigates pages
        ↓
User clicks "Submit"
        ↓
SubmitExam grades all answers at once
        ↓
DB: CompletedAt + Score stored
```

### New: Incremental Auto-Save Model
```
User clicks option
        ├─ Auto-save via SaveAnswer (debounced)
        │       ↓
        │  PackageUserResponse upserted
        │
        ├─ Page navigation tracked via UpdateSessionProgress
        │       ↓
        │  LastPageIndex + ElapsedSeconds stored
        │
        ├─ Polling every 10-30s checks exam status
        │       ↓
        │  CheckExamStatus returns cached result
        │       ↓
        │  If HC closed: redirect to Results
        │
        └─ HC monitoring every 5s sees progress
               ↓
            GetMonitoringProgress counts non-null answers

[... user continues exam ...]

User clicks "Submit"
        ↓
SubmitExam calculates elapsed from StartedAt
        ↓
Grades from already-populated PackageUserResponses
        ↓
DB: CompletedAt + Score stored
```

**Key insight:** Auto-save populates PackageUserResponses incrementally. SubmitExam grading logic unchanged; just uses already-populated data instead of collecting it all at submit time.

---

## Recommended Build Order (Dependency Analysis)

### Phase 41: Auto-Save Foundation (CRITICAL)
**What:** SaveAnswer atomic upsert + unique constraint + RowVersion
**Why first:**
- Foundation for all other features
- Race condition must be fixed immediately
- RowVersion needed for later phases (44, 43)

**Dependencies:** None
**Blockers:** None
**Complexity:** Medium

**Deliverables:**
- ExecuteUpdateAsync in SaveAnswer
- Debounce(300ms) on Exam.cshtml
- Migration: unique constraint on (SessionId, QuestionId)
- Migration: [Timestamp] on AssessmentSession + PackageUserResponse

**Verification:**
- Load test: 1 worker, 1000 rapid clicks; verify 0 duplicates in DB
- Performance: SaveAnswer latency <10ms per click

---

### Phase 42: Session Resume (MEDIUM)
**What:** LastPageIndex + ElapsedSeconds + resume logic
**Why here:**
- Depends on RowVersion from Phase 41
- Independent of polling/caching (can test separately)

**Dependencies:** Phase 41 (RowVersion must exist)
**Blockers:** Question count validation must be implemented
**Complexity:** Medium-High

**Deliverables:**
- Migration: LastPageIndex, ElapsedSeconds to AssessmentSession
- UpdateSessionProgress endpoint (new)
- StartExam enhancements (resume detection)
- Exam.cshtml: page tracking JS + timer UI
- Prevent mid-exam question changes (validation)

**Verification:**
- Resume test: Pause, network outage 5 min, resume; timer correct
- Question change test: Add question mid-exam; resume blocked
- Performance: UpdateSessionProgress latency <50ms

---

### Phase 43: Polling + Caching Infrastructure (HIGH VALUE)
**What:** CheckExamStatus caching + cache invalidation
**Why here:**
- Builds on stable auto-save (Phase 41) + resume (Phase 42)
- Introduces caching layer used by Phase 45

**Dependencies:** Phase 42 (session state must be correct)
**Blockers:** IMemoryCache registration, cache TTL tuning
**Complexity:** Medium

**Deliverables:**
- IMemoryCache registration in Startup.cs
- CheckExamStatus caching (5s TTL)
- CloseEarlySession cache invalidation
- Exam.cshtml polling (10-30s interval)
- Verify DB load is acceptable (100 workers, 10s polling)

**Verification:**
- Load test: 100 concurrent workers, 10s polling, 10 min duration; DB <30% CPU
- Cache hit rate: Should be >80% (5s cache + 10-30s polling)
- Cache invalidation: Close Early clears cache; next poll sees fresh status

---

### Phase 44: Close Early Enhancement (OPTIONAL, HIGHER RISK)
**What:** Ensure Close Early safe from double-submit race
**Why optional:**
- Close Early already exists (Phase 39 shipped)
- Phase 44 hardens it; not a new feature
- Can defer if schedule tight

**Dependencies:** Phase 41 (RowVersion), Phase 43 (caching)
**Blockers:** None (can be added after core features)
**Complexity:** Medium

**Deliverables:**
- RowVersion check on AssessmentSession (already from Phase 41)
- Test: Worker submits at T=3599s, HC closes at T=3600s (race)
- Handle DbUpdateConcurrencyException gracefully
- No data loss: both requests succeed, one final state

**Verification:**
- Concurrency test: Simultaneous SubmitExam + CloseEarlySession
- Verify: Scores match, no orphaned records, no duplicate updates

---

### Phase 45: HC Monitoring (LOWEST PRIORITY)
**What:** GetMonitoringProgress endpoint + progress column
**Why here:**
- HC monitoring is nice-to-have; worker features must-have
- Depends on stable caching (Phase 43)
- Easiest to add on top of existing infrastructure

**Dependencies:** Phase 43 (caching), Phase 41 (stable SaveAnswer)
**Blockers:** None
**Complexity:** Medium

**Deliverables:**
- GetMonitoringProgress endpoint (new, mirrors CheckExamStatus caching)
- AssessmentMonitoringDetail.cshtml: Progress column + refresh JS
- Verify HC sees worker progress in real-time (5-10s latency)
- Count only non-null answers (WHERE PackageOptionId IS NOT NULL)

**Verification:**
- Real-time test: HC watches worker; progress updates within 5-10s
- Accuracy test: Answer counts match DB
- Unload test: With 500 workers, progress queries <2s latency

---

## Critical Dependency Graph

```
Phase 41: Auto-Save + RowVersion
    │
    ├─→ Phase 42: Resume
    │       │
    │       └─→ Phase 43: Polling (hard dep on stable session state)
    │               │
    │               ├─→ Phase 44: Close Early (uses RowVersion + caching)
    │               │
    │               └─→ Phase 45: HC Monitoring (reuses caching)
    │
    └─→ Phase 43 can start after 41+42 mostly done, but 42 should complete before caching goes live
```

**Cannot skip phases:** Each phase enables the next.

---

## Architectural Patterns & Recommendations

### Pattern 1: Atomic Upsert (SaveAnswer)
```csharp
// Prevents duplicate rows on concurrent saves
await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId)
    .ExecuteUpdateAsync(r =>
        r.SetProperty(e => e.PackageOptionId, optionId)
         .SetProperty(e => e.SubmittedAt, DateTime.UtcNow));
```

### Pattern 2: Server-Side Timer (Resume)
```csharp
// Client timer untrustworthy; use server clock
var elapsed = DateTime.UtcNow - session.StartedAt.Value;
var remaining = (session.DurationMinutes * 60) - (int)elapsed.TotalSeconds;
```

### Pattern 3: Memory Cache with TTL (Polling)
```csharp
var cacheKey = $"exam_status_{sessionId}";
if (!_cache.TryGetValue(cacheKey, out var result))
{
    result = /* DB query */;
    _cache.Set(cacheKey, result, TimeSpan.FromSeconds(5));
}
return Json(result);
```

### Pattern 4: Optimistic Concurrency (RowVersion)
```csharp
// EF Core detects RowVersion mismatch, throws DbUpdateConcurrencyException
public byte[] RowVersion { get; set; }  // [Timestamp] in migration
```

### Pattern 5: Cache Invalidation
```csharp
// Clear cache when state changes
_cache.Remove($"exam_status_{sessionId}");
_cache.Remove($"monitoring_{title}_{category}_{date}");
```

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: TempData for Cross-Request State
**Problem:** TempData consumed after first access; polling fails
**Fix:** Move to database (bool TokenVerified on AssessmentSession)

### Anti-Pattern 2: Client-Side Timer in API
**Problem:** Client can manipulate with DevTools
**Fix:** Server calculates from StartedAt; client sends nothing time-related

### Anti-Pattern 3: Unbounded Polling Without Cache
**Problem:** Scales to DB overload at 100+ workers
**Fix:** 5-second memory cache; reduces DB hits 3-6x

### Anti-Pattern 4: Silent Overwrites on Concurrent Updates
**Problem:** Race condition; last-write-wins loses data
**Fix:** RowVersion [Timestamp]; throw DbUpdateConcurrencyException

### Anti-Pattern 5: Changing Question Set Mid-Exam
**Problem:** Worker's submitted answers become invalid
**Fix:** Validate question count unchanged in UpdateSessionProgress

---

## Scalability Analysis

### Load Scenarios

**At 100 Concurrent Workers:**
- Auto-save: ~20 saves/min per worker × 100 = 2000 saves/min = 33 DB writes/sec
- Polling: ~6 checks/min per worker (10s interval) × 100 = 600 checks/min = 10 DB reads/sec (6 with 5s cache)
- Monitoring: ~12 HC checks/min = 1 DB read/sec (0.1 with 10s cache)
- Total: 43 DB operations/sec (mostly cached)

**Without caching:** 43 DB ops/sec = ~2-3 seconds per query; cascade failure
**With Phase 43 caching:** 10-20 DB ops/sec = <100ms latency; sustainable

**At 300 Workers:**
- Without cache: 100+ DB ops/sec = overload
- With 5s cache: 20-30 DB ops/sec = manageable

**Optimization checkpoints:**
- Phase 41: Monitor SaveAnswer latency (should be <10ms)
- Phase 42: Monitor UpdateSessionProgress latency (should be <50ms)
- Phase 43: Load test 100 workers polling; verify DB <30% CPU
- Phase 45: Load test 500 workers monitoring; verify <2s query latency

---

## Implementation Roadmap

### Phase 41 Checklist
- [ ] Add RowVersion property to models
- [ ] Create migration for unique constraint
- [ ] Implement ExecuteUpdateAsync in SaveAnswer
- [ ] Add debounce function to Exam.cshtml
- [ ] Event handlers on option radios
- [ ] Load test: 1000 rapid clicks, verify 0 duplicates
- [ ] Performance test: SaveAnswer <10ms

### Phase 42 Checklist
- [ ] Add LastPageIndex, ElapsedSeconds to AssessmentSession
- [ ] Create migration for new columns
- [ ] Implement UpdateSessionProgress endpoint
- [ ] Enhance StartExam with resume logic
- [ ] Add page tracking JS to Exam.cshtml
- [ ] Prevent mid-exam question changes
- [ ] Test: Resume after network outage
- [ ] Test: Question change blocks resume

### Phase 43 Checklist
- [ ] Register IMemoryCache in Startup.cs
- [ ] Enhance CheckExamStatus with caching
- [ ] Enhance CloseEarlySession with cache invalidation
- [ ] Add polling interval to Exam.cshtml
- [ ] Test: 100 workers, 10s polling, verify DB load
- [ ] Monitor: Cache hit rate >80%
- [ ] Verify: Close Early invalidates cache

### Phase 44 Checklist
- [ ] Test close-early race condition
- [ ] Verify DbUpdateConcurrencyException handling
- [ ] Ensure scores match between concurrent requests
- [ ] Check for orphaned Competency records

### Phase 45 Checklist
- [ ] Implement GetMonitoringProgress endpoint
- [ ] Add Progress column to monitoring table
- [ ] Add refresh JS to AssessmentMonitoringDetail
- [ ] Test: Progress updates within 5-10s
- [ ] Verify: Count only non-null answers
- [ ] Load test: 500 workers, verify <2s latency

---

## Summary

v2.1's four features integrate cleanly into the existing CMPController with minimal refactoring:

1. **Phase 41:** Harden SaveAnswer with atomic upsert + concurrency detection
2. **Phase 42:** Add resume capability with server-side time tracking
3. **Phase 43:** Introduce caching infrastructure for polling scalability
4. **Phase 44:** Harden Close Early (optional)
5. **Phase 45:** Add HC live monitoring (lowest priority, reuses Phase 43 patterns)

All phases share the same architectural principles: server-side authority (time, state), atomic operations, and strategic caching. No breaking changes to existing APIs; pure enhancement of current endpoints.

---

## Sources

- [EF Core Concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [ExecuteUpdateAsync](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)
- [Memory Cache in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-8.0)
- [Previous v2.1 Research: SUMMARY-AUTO-SAVE-RESUME-POLLING.md](./SUMMARY-AUTO-SAVE-RESUME-POLLING.md)
- [Previous v2.1 Research: PITFALLS-AUTO-SAVE-RESUME-POLLING.md](./PITFALLS-AUTO-SAVE-RESUME-POLLING.md)
