# Technology Stack: v2.1 Assessment Resilience & Real-Time Monitoring

**Project:** Portal HC KPB - v2.1 Milestone (auto-save, session resume, exam polling, live monitoring)
**Researched:** 2026-02-24
**Overall Confidence:** HIGH

---

## Executive Summary

The v2.1 milestone adds four resilience features to an existing ASP.NET Core 8 MVC application:

1. **Auto-save answers** on each radio click → `SaveAnswer` AJAX POST (v1.7 endpoint exists)
2. **Session resume** → persist last page + elapsed time via `sessionStorage` + database columns
3. **Worker exam polling** → 10s polling on `CheckExamStatus` endpoint (existing, tested)
4. **Live HC monitoring** → auto-refresh progress every 5-10s via AJAX

**Key Finding: No new NuGet packages required.** Uses:
- Existing ASP.NET Core 8 MVC + EF Core endpoints
- Browser native APIs (Fetch, sessionStorage, setInterval)
- No WebSockets, no job queues, no real-time libraries
- Stack stays lean: server handles ~20 req/sec easily at scale

---

## Recommended Stack

### Backend (Server-Side)

| Technology | Version | Purpose | Status | Changes |
|-----------|---------|---------|--------|---------|
| ASP.NET Core MVC | 8.0 | Web framework | Existing | None — use existing patterns |
| Entity Framework Core | 8.0 | ORM | Existing | None — SaveAnswer already exists |
| SQL Server / SQLite | Current | Database | Existing | Schema only: add 2 columns to AssessmentSession |
| ASP.NET Identity | 8.0 | Auth | Existing | None — antiforgery tokens already used |

**Database Schema Changes (Migration Only):**
```sql
ALTER TABLE AssessmentSessions ADD
    LastPageIndex INT DEFAULT 0,
    ElapsedSeconds INT DEFAULT 0;
```

No new tables, no new dependencies.

### Frontend (Client-Side)

| Technology | Version | Purpose | Status | Notes |
|-----------|---------|---------|--------|-------|
| **Fetch API** | Native ES6 | AJAX to endpoints | Existing | Already used in StartExam.cshtml for polling; increase call frequency |
| **sessionStorage** | Native HTML5 | Client state persistence | **NEW** | Resume page + time across refresh; ephemeral (cleared on logout) |
| **setInterval()** | Native ES3 | Polling timer | Existing | Already used for countdown; reuse for status polling |
| **jQuery** | 3.7.1 (CDN) | DOM manipulation | Existing | Keep for backward compatibility; new code uses `fetch()` |
| **Bootstrap 5** | 5.3.0 (CDN) | UI framework | Existing | Modals, alerts, progress bars; no changes |

**Zero new JavaScript libraries.** All native browser APIs or already loaded via CDN.

### Supporting Libraries (Unchanged)

- ClosedXML (0.105.0) — Excel exports, no interaction with v2.1
- Chart.js — Monitoring visualizations, no code changes (auto-refresh only)

---

## Feature Implementation Details

### 1. Auto-Save on Radio Click

**Status:** Extend existing pattern (SaveAnswer already tested in v1.7)

**Endpoint:** `POST /CMP/SaveAnswer?sessionId=X&questionId=Y&optionId=Z`
- Already exists, already handles concurrent requests
- Uses EF Core `ExecuteUpdateAsync` pattern (try update, fallback to insert)
- Antiforgery tokens already in place

**Client Code:** Fire-and-forget AJAX on radio change
```javascript
document.querySelectorAll('.exam-radio').forEach(radio => {
    radio.addEventListener('change', () => {
        const qId = radio.getAttribute('data-question-id');
        const optId = radio.value;

        // Update local hidden input (fallback if AJAX fails)
        document.getElementById('ans_' + qId).value = optId;

        // NEW: Auto-save to DB (non-blocking)
        fetch('/CMP/SaveAnswer', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `sessionId=${SESSION_ID}&questionId=${qId}&optionId=${optId}`
        }).catch(() => {}); // Ignore network errors
    });
});
```

**Why fetch() over jQuery.ajax():**
- Fetch is modern, simpler, already used for CheckExamStatus polling
- No jQuery dependency added
- Easier error handling with `.catch()`

**Load:** ~5 answers/worker × 100 workers = ~500 POST/min = 8 req/sec (negligible)

---

### 2. Session Resume (Last Page + Elapsed Time)

**Client-Side:** `sessionStorage` for immediate resume
**Server-Side:** Database columns + endpoint to fetch resume state

**Client Storage (sessionStorage keys):**
```javascript
sessionStorage.setItem('exam_' + SESSION_ID + '_page', currentPage);
sessionStorage.setItem('exam_' + SESSION_ID + '_elapsed', elapsedSeconds);
```

**On page load (StartExam.cshtml):**
```javascript
// Restore page and time from sessionStorage (if page refresh)
const lastPage = parseInt(sessionStorage.getItem('exam_' + SESSION_ID + '_page')) || 0;
const elapsedStoredSeconds = parseInt(sessionStorage.getItem('exam_' + SESSION_ID + '_elapsed')) || 0;

currentPage = lastPage;
timeRemaining = DURATION_SECONDS - elapsedStoredSeconds;
changePage(lastPage);
```

**Periodic update (every 5 seconds during exam):**
```javascript
setInterval(() => {
    sessionStorage.setItem('exam_' + SESSION_ID + '_page', currentPage);
    sessionStorage.setItem('exam_' + SESSION_ID + '_elapsed', DURATION_SECONDS - timeRemaining);
}, 5000);
```

**On ExamSummary POST:**
```javascript
// Append to form before submit
const form = document.getElementById('examForm');
form.innerHTML += `<input type="hidden" name="lastPageIndex" value="${currentPage}">`;
form.innerHTML += `<input type="hidden" name="elapsedSeconds" value="${DURATION_SECONDS - timeRemaining}">`;
form.submit();
```

**Server-Side Persistence (ExamSummary controller):**
```csharp
[HttpPost]
public async Task<IActionResult> ExamSummary(int id, int lastPageIndex, int elapsedSeconds, ...)
{
    var session = await _context.AssessmentSessions.FindAsync(id);
    session.LastPageIndex = lastPageIndex;
    session.ElapsedSeconds = elapsedSeconds;
    session.Status = "Completed";
    session.CompletedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    // Redirect to results
}
```

**Resume Endpoint (optional, for debugging):**
```csharp
[HttpGet("GetSessionResume")]
public async Task<IActionResult> GetSessionResume(int sessionId)
{
    var session = await _context.AssessmentSessions
        .AsNoTracking()
        .Where(s => s.Id == sessionId && s.Status == "InProgress")
        .Select(s => new {
            lastPageIndex = s.LastPageIndex ?? 0,
            elapsedSeconds = s.ElapsedSeconds,
            remainingSeconds = (int)(s.ExamWindowCloseDate - DateTime.UtcNow).TotalSeconds
        })
        .FirstOrDefaultAsync();

    return Json(session);
}
```

**Why sessionStorage:**
- Persists across page refresh (F5, accidental close)
- Cleared on browser close or logout (security)
- ~200 bytes per session (trivial)
- No network overhead (instant restore)
- Works offline until next AJAX call

**Database columns purpose:**
- Audit trail (compliance: record how long worker took)
- Resume after logout + re-login (sessionStorage cleared)
- Admin can see elapsed time in session history

---

### 3. Worker Exam Polling (Every 10s)

**Status:** Existing endpoint, just reduce interval

**Endpoint:** `GET /CMP/CheckExamStatus?sessionId=X` (already exists, v1.7)
- Response: `{ "closed": bool, "redirectUrl": string }`
- Lightweight query: single SELECT on AssessmentSession.Id
- No new code needed on server

**Client Code (StartExam.cshtml):**
```javascript
// EXISTING: Polling every 30 seconds
// CHANGE: Reduce to 10 seconds for faster HC close detection
const POLL_INTERVAL = 10000; // was 30000

setInterval(() => {
    fetch('/CMP/CheckExamStatus?sessionId=' + SESSION_ID)
        .then(r => r.json())
        .then(data => {
            if (data.closed && !examClosed) {
                examClosed = true;
                clearInterval(statusPollInterval);
                clearInterval(timerInterval);

                // Show notification
                alert('Exam closed by administrator. Redirecting...');

                // Redirect to results
                setTimeout(() => {
                    window.location.href = data.redirectUrl || '/CMP/Assessment';
                }, 3000);
            }
        })
        .catch(() => {}); // Continue polling on network error
}, POLL_INTERVAL);
```

**Load Estimate:**
- 100 concurrent workers → 10 req/sec
- Query: 1 table, 1 WHERE clause, 0 joins
- Database can handle 1000+ req/sec easily
- Network: ~100 bytes per response (minimal)

**No new libraries.** Uses existing Fetch API and setInterval.

---

### 4. Live HC Monitoring Auto-Refresh (Every 5-10s)

**Status:** Existing page, add polling script

**Options:**

**Option A: Refetch full page, extract progress DOM**
```javascript
// In AssessmentMonitoringDetail.cshtml <script> block
const MONITORING_URL = window.location.href;
const POLL_INTERVAL = 10000; // 10 seconds

setInterval(() => {
    fetch(MONITORING_URL)
        .then(r => r.text())
        .then(html => {
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');

            // Extract new progress values
            const newProgress = doc.querySelector('#completedProgress');
            if (newProgress) {
                document.querySelector('#completedProgress').innerText =
                    newProgress.innerText;
            }
        })
        .catch(() => {});
}, POLL_INTERVAL);
```

**Option B: Dedicated JSON endpoint (recommended for efficiency)**
```csharp
// In CMPController.cs
[HttpGet("GetMonitoringProgress")]
public async Task<IActionResult> GetMonitoringProgress(string title, string category, DateTime scheduleDate)
{
    var sessions = await _context.AssessmentSessions
        .AsNoTracking()
        .Where(s => s.Title == title && s.Category == category && s.Schedule == scheduleDate)
        .ToListAsync();

    int total = sessions.Count;
    int completed = sessions.Count(s => s.Status == "Completed");
    int progressPct = total > 0 ? (int)Math.Round(completed * 100.0 / total) : 0;

    return Json(new { completedCount = completed, totalCount = total, progressPct });
}
```

```javascript
// Client code (Option B - more efficient)
const TITLE = '@Model.Title';
const CATEGORY = '@Model.Category';
const SCHEDULE = '@Model.Schedule.ToString("yyyy-MM-ddTHH:mm:ss")';

setInterval(() => {
    fetch(`/CMP/GetMonitoringProgress?title=${encodeURIComponent(TITLE)}&category=${encodeURIComponent(CATEGORY)}&scheduleDate=${encodeURIComponent(SCHEDULE)}`)
        .then(r => r.json())
        .then(data => {
            // Update only the progress column
            document.querySelector('#progressBar').style.width = data.progressPct + '%';
            document.querySelector('#progressText').innerText = data.completedCount + '/' + data.totalCount;
        })
        .catch(() => {});
}, 10000);
```

**Recommendation:** Option B (dedicated endpoint)
- Payload: ~100 bytes vs. ~30KB for full page
- Cleaner separation of concerns
- Easier to test and debug

---

## Database Schema Migration

**Create new migration:**
```bash
dotnet ef migrations add AddSessionResumeColumns
```

**Migration code:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "LastPageIndex",
        table: "AssessmentSessions",
        type: "int",
        nullable: false,
        defaultValue: 0);

    migrationBuilder.AddColumn<int>(
        name: "ElapsedSeconds",
        table: "AssessmentSessions",
        type: "int",
        nullable: false,
        defaultValue: 0);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "LastPageIndex", table: "AssessmentSessions");
    migrationBuilder.DropColumn(name: "ElapsedSeconds", table: "AssessmentSessions");
}
```

**Apply:**
```bash
dotnet ef database update
```

**Entity Model Update:**
```csharp
public class AssessmentSession
{
    // Existing fields...
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // NEW:
    public int LastPageIndex { get; set; } = 0;
    public int ElapsedSeconds { get; set; } = 0;
}
```

---

## Alternatives Considered & Rejected

| Use Case | Proposed | Why Rejected | Our Choice |
|----------|----------|-------------|-----------|
| Auto-save | WebSockets (SignalR) | Overkill; polling sufficient; adds 2 dependencies | Fire-and-forget fetch POST |
| Resume state | Redis cache | Deployment complexity; sessionStorage ephemeral by design | sessionStorage + DB columns |
| Resume state | URL params | Leaks data in browser history | sessionStorage |
| Session polling | WebSockets (SignalR) | Workers don't need real-time; 10s polling fast enough | 10s fetch polling |
| Polling library | Axios | Already have fetch; no extra dependency | Native fetch API |
| Persistence | IndexedDB | Overkill; sessionStorage sufficient for exam duration | sessionStorage (~100 bytes) |

---

## Load Testing Baseline

**Concurrent Scenario:** 100 workers taking exams simultaneously

| Operation | Frequency | Payload | Total/sec |
|-----------|-----------|---------|-----------|
| SaveAnswer (auto-click) | ~1 per min/worker | 150 bytes POST | ~1.6 req/sec |
| CheckExamStatus poll | Every 10s/worker | 100 bytes GET | ~10 req/sec |
| MonitoringDetail poll | Every 10s/HC (1-5 HC) | 100 bytes GET | <1 req/sec |
| **Total** | — | — | **~12 req/sec** |

**Database Load:**
- SaveAnswer: 2 queries (ExecuteUpdate + possible Insert) = moderate I/O
- CheckExamStatus: 1 SELECT = minimal I/O
- Monitoring: 1 SELECT with COUNT = minimal I/O

**Prediction:** ASP.NET Core can handle 1000+ req/sec on modern hardware. 12 req/sec is negligible.

---

## Browsers & Compatibility

| Technology | Min Browser | Note |
|-----------|-----------|------|
| Fetch API | Chrome 42, Firefox 39, Safari 10.1, Edge 15 | Covers 99%+ of modern users |
| sessionStorage | IE 8+ | Covers 100% |
| setInterval() | All | ES3 standard |
| Antiforgery tokens | All | Server-side feature |

**Recommendation:** No polyfills needed; ASP.NET Core 8 assumes modern browsers.

---

## Deployment Checklist

**Server-Side:**
- [ ] Create and apply migration to add 2 columns to AssessmentSession
- [ ] Update AssessmentSession model to include new columns
- [ ] Update ExamSummary POST handler to receive lastPageIndex + elapsedSeconds
- [ ] Verify SaveAnswer endpoint handles concurrent clicks (EF Core already does)
- [ ] Reduce CheckExamStatus polling from 30s to 10s (optional but recommended)

**Client-Side (Frontend):**
- [ ] Add sessionStorage logic to StartExam.cshtml for page/time persistence
- [ ] Update page navigation to update sessionStorage every change
- [ ] Add periodic save of elapsed time to sessionStorage (every 5s)
- [ ] Append lastPageIndex + elapsedSeconds to ExamSummary form before submit
- [ ] Test sessionStorage survives F5 refresh
- [ ] Test sessionStorage clears on logout

**Monitoring (Optional):**
- [ ] Add GetMonitoringProgress endpoint to CMPController (or refetch full page)
- [ ] Add polling script to AssessmentMonitoringDetail.cshtml
- [ ] Test auto-refresh updates progress column every 10s

**Testing:**
- [ ] Load test: 50+ concurrent workers, verify 10s polling doesn't spike CPU
- [ ] Verify antiforgery tokens work with all new AJAX calls
- [ ] Test with network throttling (simulate slow connection)
- [ ] Test SaveAnswer with rapid-fire clicks (100+ per session)

---

## Performance Tips

1. **sessionStorage is fast:** No network round-trip; restore instantly on page load
2. **Fetch is lean:** Simpler than jQuery.ajax; 1KB gzipped
3. **Polling is efficient:** 10s interval with simple SELECT queries scales easily
4. **Fire-and-forget design:** AJAX errors don't block exam; answers in hidden inputs as fallback

---

## Sources & Verification

**ASP.NET Core & EF Core:**
- [SaveAnswer pattern - EF Core ExecuteUpdateAsync](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)
- [Efficient Updating in EF Core](https://learn.microsoft.com/en-us/ef/core/performance/efficient-updating)
- [ASP.NET Core Antiforgery Protection](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery)

**Browser APIs:**
- [Fetch API (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API)
- [sessionStorage (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/Window/sessionStorage)
- [setInterval (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/setInterval)

**Existing Codebase (Verified):**
- StartExam.cshtml: Uses fetch for CheckExamStatus polling (line 321)
- CMPController.cs: SaveAnswer & CheckExamStatus endpoints exist (tested in v1.7)
- _Layout.cshtml: jQuery 3.7.1 + Bootstrap 5.3.0 already included
- AssessmentSession.cs: Model has StartedAt, CompletedAt, ExamWindowCloseDate

---

## Summary: What's New vs. What Exists

| Component | Existing | New | Change |
|-----------|----------|-----|--------|
| SaveAnswer endpoint | ✓ v1.7 | — | Increase click frequency (no code change) |
| CheckExamStatus endpoint | ✓ | — | Reduce polling from 30s to 10s |
| Fetch API usage | ✓ | — | Reuse for auto-save (same pattern) |
| sessionStorage | — | ✓ | New: persist page + time across refresh |
| DB columns (LastPageIndex, ElapsedSeconds) | — | ✓ | New: 1 migration |
| ExamSummary handler | ✓ | — | Extend: receive 2 new params |
| Monitoring auto-refresh | — | ✓ | New: optional polling script |

**Bottom Line:** Mostly extending existing patterns. No new dependencies. No deployment risk.
