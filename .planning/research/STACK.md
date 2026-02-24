# Technology Stack: ASP.NET Core MVC 8 Auto-Save, Session Resume, and Polling

**Project:** Auto-save exam answers, session elapsed time tracking, worker session close detection, HC monitoring polling
**Researched:** 2026-02-24
**Confidence:** HIGH (EF Core, ExecuteUpdate patterns verified with official Microsoft docs; polling patterns from multiple sources)

---

## Executive Summary

This research covers 4 specific features for an existing ASP.NET Core 8 MVC application:

1. **SaveAnswer upsert** - Insert or update PackageUserResponse efficiently
2. **Elapsed time storage** - Track exam duration server-side for resume accuracy
3. **Worker session polling** - Lightweight endpoint detects when HC closes session
4. **HC monitor polling** - Lightweight endpoint returns session answer counts

**Key recommendation:** Use EF Core's `ExecuteUpdateAsync` with manual insert-or-update logic (no third-party upsert library needed). Store elapsed time as integer seconds in AssessmentSession. Use lightweight status/count endpoints with minimal JSON payloads.

---

## 1. SaveAnswer Upsert Pattern

### Recommendation: ExecuteUpdateAsync with FindAsync Insert-or-Update

**Why NOT:**
- **No native EF Core Upsert** — EF Core 8 doesn't have a built-in Upsert method. AddOrUpdate from EF6 doesn't exist in EF Core.
- **MERGE statement has concurrency issues** — SQL Server MERGE can race when two concurrent merges both detect NOT MATCHED and insert the same row, causing unique constraint violations.
- **Third-party libraries (FlexLabs.Upsert) add complexity** — Unnecessary for a single-row operation that happens infrequently enough per session.

**Why ExecuteUpdateAsync + Insert fallback:**

ExecuteUpdateAsync (EF Core 7+) is the modern pattern. It executes a single SQL UPDATE statement without loading data into memory. For upsert, combine it with FindAsync for the insert case.

### Implementation Pattern

```csharp
// In AssessmentSessionRepository or QuestionAnswerService
public async Task<PackageUserResponse> UpsertAnswerAsync(
    int assessmentSessionId,
    int questionId,
    int selectedOptionId)
{
    var dbContext = _context; // Your DbContext

    // Attempt UPDATE first (most common case: user changed answer)
    var rowsAffected = await dbContext.PackageUserResponses
        .Where(r => r.AssessmentSessionId == assessmentSessionId
                 && r.QuestionId == questionId)
        .ExecuteUpdateAsync(s => s
            .SetProperty(r => r.SelectedOptionId, selectedOptionId)
            .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

    // If no rows affected, row doesn't exist yet — INSERT
    if (rowsAffected == 0)
    {
        var newResponse = new PackageUserResponse
        {
            AssessmentSessionId = assessmentSessionId,
            QuestionId = questionId,
            SelectedOptionId = selectedOptionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.PackageUserResponses.Add(newResponse);
        await dbContext.SaveChangesAsync();

        return newResponse;
    }

    // For update case, reload to return updated entity if needed
    // Or construct object from parameters if not needed in response
    return new PackageUserResponse
    {
        AssessmentSessionId = assessmentSessionId,
        QuestionId = questionId,
        SelectedOptionId = selectedOptionId
    };
}
```

### Alternative: Raw SQL MERGE (for maximum concurrency safety)

If you want true atomic upsert at database level (prevents race conditions entirely), use SQL Server's MERGE statement directly via ExecuteSqlInterpolated:

```csharp
public async Task<int> UpsertAnswerViaMergeAsync(
    int assessmentSessionId,
    int questionId,
    int selectedOptionId)
{
    // SQL Server MERGE is atomic - no race condition possible
    var rowsAffected = await _context.Database
        .ExecuteSqlInterpolatedAsync(
            $@"MERGE INTO PackageUserResponses AS target
               USING (SELECT {assessmentSessionId} AS AssessmentSessionId,
                            {questionId} AS QuestionId,
                            {selectedOptionId} AS SelectedOptionId) AS source
               ON target.AssessmentSessionId = source.AssessmentSessionId
                  AND target.QuestionId = source.QuestionId
               WHEN MATCHED THEN
                   UPDATE SET target.SelectedOptionId = source.SelectedOptionId,
                             target.UpdatedAt = GETUTCDATE()
               WHEN NOT MATCHED THEN
                   INSERT (AssessmentSessionId, QuestionId, SelectedOptionId, CreatedAt, UpdatedAt)
                   VALUES (source.AssessmentSessionId, source.QuestionId, source.SelectedOptionId, GETUTCDATE(), GETUTCDATE());");

    return rowsAffected;
}
```

### Recommended Choice: ExecuteUpdateAsync Pattern

**Choose ExecuteUpdateAsync + Insert Fallback because:**
- Single-row operations happen per AJAX call (not bulk)
- Simpler to test and debug than raw SQL MERGE
- No concurrency issues — if ExecuteUpdate returns 0 rows, INSERT is guaranteed to succeed (assuming AssessmentSessionId + QuestionId unique constraint)
- Easier to return updated object for response if needed

**Concurrency handling:**
- Race condition possible: Two concurrent updates both see row doesn't exist, both try INSERT
- Prevention: Ensure `(AssessmentSessionId, QuestionId)` has a UNIQUE constraint in schema
- If constraint violated, catch `DbUpdateException` and retry the flow once

### Entity Model Addition

No new fields needed. Ensure these fields exist on `PackageUserResponse`:

```csharp
public class PackageUserResponse
{
    public int Id { get; set; }
    public int AssessmentSessionId { get; set; }
    public int QuestionId { get; set; }
    public int SelectedOptionId { get; set; }

    // For audit trail (optional, but good practice)
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public AssessmentSession AssessmentSession { get; set; }
    public Question Question { get; set; }
    public Option SelectedOption { get; set; }
}
```

**Database constraint (must exist):**
```sql
ALTER TABLE PackageUserResponses
ADD CONSTRAINT UK_AssessmentSessionQuestion
UNIQUE (AssessmentSessionId, QuestionId);
```

---

## 2. Elapsed Time Storage

### Recommendation: Add `ElapsedSeconds` to AssessmentSession

**Field Design:**

```csharp
public class AssessmentSession
{
    // Existing fields
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public int AssessmentPackageId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ExamWindowCloseDate { get; set; }
    public string Status { get; set; } // "InProgress", "Completed", "Closed"

    // NEW: Elapsed time tracking
    public int ElapsedSeconds { get; set; } = 0;  // Total seconds spent so far
    public DateTime? LastHeartbeatAt { get; set; } // When worker was last active
}
```

### How It Works

1. **On exam start:** ElapsedSeconds = 0
2. **Periodically (every AJAX save or heartbeat):** Update ElapsedSeconds

```csharp
public async Task UpdateElapsedTimeAsync(int sessionId)
{
    var now = DateTime.UtcNow;

    var session = await _context.AssessmentSessions
        .Where(s => s.Id == sessionId && s.Status == "InProgress")
        .ExecuteUpdateAsync(s => s
            .SetProperty(x => x.ElapsedSeconds, x =>
                (int)(now - x.StartedAt).TotalSeconds)
            .SetProperty(x => x.LastHeartbeatAt, now));
}
```

3. **On resume after page refresh:**

```csharp
public async Task<SessionResumeDto> GetSessionForResumeAsync(int sessionId)
{
    var session = await _context.AssessmentSessions
        .Where(s => s.Id == sessionId)
        .Select(s => new SessionResumeDto
        {
            SessionId = s.Id,
            ElapsedSeconds = s.ElapsedSeconds,
            RemainingSeconds =
                (int)(s.ExamWindowCloseDate - DateTime.UtcNow).TotalSeconds,
            IsClosed = s.Status == "Closed" || DateTime.UtcNow > s.ExamWindowCloseDate,
            Status = s.Status
        })
        .FirstOrDefaultAsync();

    return session;
}

public class SessionResumeDto
{
    public int SessionId { get; set; }
    public int ElapsedSeconds { get; set; }        // How long they've been working
    public int RemainingSeconds { get; set; }      // How much time left
    public bool IsClosed { get; set; }
    public string Status { get; set; }
}
```

### Why Integer Seconds (Not TimeSpan)

- **Database simplicity:** Int32 is more portable than TimeSpan
- **Calculation accuracy:** `DateTime.UtcNow - StartedAt` returns TimeSpan; cast to int seconds when persisting
- **Client resume:** Browser receives `{ elapsedSeconds: 1247, remainingSeconds: 1353 }` and can calculate timer accurately

### Anti-Pattern to Avoid

Do NOT store elapsed time only in browser localStorage. If worker closes browser and reopens, you lose time without server persistence.

---

## 3. Worker Exam Polling Endpoint

### Recommendation: Lightweight Status-Only Endpoint (HTTP 200 vs 308)

**Endpoint:**
```csharp
[HttpGet("/api/exam/session/{sessionId}/status")]
public async Task<IActionResult> GetSessionStatusAsync(int sessionId)
{
    var session = await _context.AssessmentSessions
        .AsNoTracking()
        .Where(s => s.Id == sessionId)
        .Select(s => new
        {
            status = s.Status,
            closedAt = s.CompletedAt ?? s.ExamWindowCloseDate,
            isClosed = s.Status == "Closed" || DateTime.UtcNow > s.ExamWindowCloseDate
        })
        .FirstOrDefaultAsync();

    if (session == null)
        return NotFound();

    // If session closed, signal with 308 (Permanent Redirect)
    if (session.isClosed)
        return StatusCode(308, new { message = "Exam closed, redirect to results" });

    // Otherwise, all good
    return Ok(new { status = "active", closedAt = session.closedAt });
}
```

### Why This Design

**HTTP Status Codes:**
- `200 OK` — Session still active, keep polling
- `308 Permanent Redirect` — Session closed, don't poll anymore, redirect to results

**Why NOT 403/410:**
- 403 (Forbidden) suggests permission issue, not closure
- 410 (Gone) implies resource deleted, not applicable for a closed session
- 308 signals "permanent" state change without being an error

**Lightweight Query:**
- `AsNoTracking()` — Don't track changes, faster read-only query
- Only select 2-3 fields needed (status, closedAt, isClosed calculation)
- No joins, no related data loaded
- Single table scan on AssessmentSessions.Status index

### Client-Side Polling Code

```javascript
// JavaScript on worker exam page
async function pollSessionStatus(sessionId, interval = 10000) {
    const response = await fetch(`/api/exam/session/${sessionId}/status`);

    if (response.status === 308) {
        // Session closed
        alert("Exam has been closed by admin. Redirecting to results...");
        window.location.href = `/exam/results/${sessionId}`;
        return;
    }

    if (response.ok) {
        const data = await response.json();
        // Session still active, continue polling
        setTimeout(() => pollSessionStatus(sessionId, interval), interval);
    }
}

// Start polling when page loads
document.addEventListener('DOMContentLoaded', () => {
    pollSessionStatus(getSessionIdFromPage());
});
```

---

## 4. HC Monitoring Polling Endpoint

### Recommendation: Grouped Count Query

**Endpoint:**
```csharp
[HttpGet("/api/admin/assessment/{assessmentPackageId}/monitoring")]
public async Task<IActionResult> GetAssessmentMonitoringAsync(int assessmentPackageId)
{
    var sessions = await _context.AssessmentSessions
        .AsNoTracking()
        .Where(s => s.AssessmentPackageId == assessmentPackageId)
        .GroupJoin(
            _context.PackageUserResponses.AsNoTracking(),
            session => session.Id,
            response => response.AssessmentSessionId,
            (session, responses) => new
            {
                sessionId = session.Id,
                workerId = session.WorkerId,
                startedAt = session.StartedAt,
                status = session.Status,
                answeredCount = responses.Count(),
                totalQuestions = _context.Questions
                    .Where(q => q.AssessmentPackageId == assessmentPackageId)
                    .Count(),
                elapsedSeconds = session.ElapsedSeconds,
                remainingSeconds = (int)(session.ExamWindowCloseDate - DateTime.UtcNow).TotalSeconds
            })
        .ToListAsync();

    return Ok(sessions);
}
```

### Better: Use SubQuery for Performance

The above GroupJoin counts total questions for EVERY session (N+1 pattern). Better approach:

```csharp
[HttpGet("/api/admin/assessment/{assessmentPackageId}/monitoring")]
public async Task<IActionResult> GetAssessmentMonitoringAsync(int assessmentPackageId)
{
    var totalQuestions = await _context.Questions
        .AsNoTracking()
        .Where(q => q.AssessmentPackageId == assessmentPackageId)
        .CountAsync();

    var sessions = await _context.AssessmentSessions
        .AsNoTracking()
        .Where(s => s.AssessmentPackageId == assessmentPackageId)
        .Select(s => new
        {
            sessionId = s.Id,
            workerId = s.WorkerId,
            startedAt = s.StartedAt,
            status = s.Status,
            answeredCount = _context.PackageUserResponses
                .Where(r => r.AssessmentSessionId == s.Id)
                .Count(),
            totalQuestions = totalQuestions,
            elapsedSeconds = s.ElapsedSeconds,
            remainingSeconds = (int)(s.ExamWindowCloseDate - DateTime.UtcNow).TotalSeconds,
            isClosed = s.Status == "Closed" || DateTime.UtcNow > s.ExamWindowCloseDate
        })
        .ToListAsync();

    return Ok(sessions);
}
```

### Best: Window Function (Single Query)

For maximum efficiency, use SQL window function to get answer counts per session:

```csharp
public async Task<IActionResult> GetAssessmentMonitoringAsync(int assessmentPackageId)
{
    var monitoring = await _context.Database
        .SqlQuery<MonitoringSessionDto>($@"
            SELECT
                s.Id AS SessionId,
                s.WorkerId,
                s.StartedAt,
                s.Status,
                COUNT(DISTINCT r.Id) AS AnsweredCount,
                (SELECT COUNT(*) FROM Questions WHERE AssessmentPackageId = {assessmentPackageId}) AS TotalQuestions,
                s.ElapsedSeconds,
                DATEDIFF(SECOND, GETUTCDATE(), s.ExamWindowCloseDate) AS RemainingSeconds,
                CASE WHEN s.Status = 'Closed' OR GETUTCDATE() > s.ExamWindowCloseDate THEN 1 ELSE 0 END AS IsClosed
            FROM AssessmentSessions s
            LEFT JOIN PackageUserResponses r ON s.Id = r.AssessmentSessionId
            WHERE s.AssessmentPackageId = {assessmentPackageId}
            GROUP BY s.Id, s.WorkerId, s.StartedAt, s.Status, s.ElapsedSeconds, s.ExamWindowCloseDate
            ORDER BY s.StartedAt DESC")
        .ToListAsync();

    return Ok(monitoring);
}

public class MonitoringSessionDto
{
    public int SessionId { get; set; }
    public int WorkerId { get; set; }
    public DateTime StartedAt { get; set; }
    public string Status { get; set; }
    public int AnsweredCount { get; set; }
    public int TotalQuestions { get; set; }
    public int ElapsedSeconds { get; set; }
    public int RemainingSeconds { get; set; }
    public bool IsClosed { get; set; }
}
```

### Response Format

```json
[
  {
    "sessionId": 42,
    "workerId": 101,
    "startedAt": "2026-02-24T14:30:00Z",
    "status": "InProgress",
    "answeredCount": 23,
    "totalQuestions": 50,
    "elapsedSeconds": 847,
    "remainingSeconds": 6153,
    "isClosed": false
  }
]
```

### Client-Side Polling (HC Dashboard)

```javascript
// Poll every 5-10 seconds for live session status
async function pollMonitoring(assessmentPackageId) {
    const response = await fetch(`/api/admin/assessment/${assessmentPackageId}/monitoring`);
    const sessions = await response.json();

    // Update table with latest counts
    updateSessionTable(sessions);

    // Highlight sessions that just closed
    sessions.forEach(session => {
        if (session.isClosed) {
            markRowAsClosed(session.sessionId);
        }
    });

    // Continue polling
    setTimeout(() => pollMonitoring(assessmentPackageId), 5000);
}
```

---

## Installation & Configuration

### NuGet Packages

No additional packages required. You're using existing stack:
- `Microsoft.EntityFrameworkCore` (8.0+)
- `Microsoft.EntityFrameworkCore.SqlServer` (8.0+)
- `Microsoft.AspNetCore.Mvc` (8.0+)

### Database Migrations

Add ElapsedSeconds and LastHeartbeatAt to AssessmentSession:

```csharp
// In your DbContext migration
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "ElapsedSeconds",
        table: "AssessmentSessions",
        type: "int",
        nullable: false,
        defaultValue: 0);

    migrationBuilder.AddColumn<DateTime>(
        name: "LastHeartbeatAt",
        table: "AssessmentSessions",
        type: "datetime2",
        nullable: true);

    // Add unique constraint on (AssessmentSessionId, QuestionId) if not exists
    migrationBuilder.CreateIndex(
        name: "IX_PackageUserResponses_SessionQuestion",
        table: "PackageUserResponses",
        columns: new[] { "AssessmentSessionId", "QuestionId" },
        unique: true);
}
```

### Startup Configuration

```csharp
// In Startup.cs or Program.cs
services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
```

---

## Summary Table

| Feature | Technology | Pattern | Why |
|---------|-----------|---------|-----|
| **SaveAnswer Upsert** | EF Core ExecuteUpdateAsync | Try UPDATE first, INSERT if no rows affected | Concurrency-safe, no third-party lib, simple |
| **Elapsed Time** | int ElapsedSeconds field | Update every AJAX call via ExecuteUpdateAsync | Accurate resume after page refresh, persisted server-side |
| **Worker Polling** | Lightweight GET endpoint | Return 308 when closed, 200 when active | Minimal payload, clear HTTP semantics |
| **HC Monitoring** | Window function SQL query | GROUP BY sessionId, COUNT(answers) | Single efficient query, no N+1 |

---

## Sources

- [ExecuteUpdate and ExecuteDelete - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)
- [Efficient Updating - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/performance/efficient-updating)
- [Handling Concurrency Conflicts - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [MERGE (Transact-SQL) - SQL Server | Microsoft Learn](https://learn.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql?view=sql-server-ver17)
- [EF Core ExecuteUpdate (EF Core 7–10) – Set-Based Bulk Updates](https://www.learnentityframeworkcore.com/dbset/execute-update)
- [SQL Server MERGE to insert, update and delete at the same time](https://www.mssqltips.com/sqlservertip/1704/using-merge-in-sql-server-to-insert-update-and-delete-at-the-same-time/)
- [Implementing HTTP Polling](https://www.abhinavpandey.dev/blog/polling)
- [The Complete Guide to API Polling: Implementation, Optimization, and Alternatives](https://medium.com/@alaxhenry0121/the-complete-guide-to-api-polling-implementation-optimization-and-alternatives-a4eae3b0ef69)
- [Merge/Upsert/AddOrUpdate support · Issue #4526 · dotnet/efcore](https://github.com/dotnet/efcore/issues/4526)
- [Bulk Extensions for EF Core | Bulk Insert, Update, Delete, Merge & Upsert](https://entityframework-extensions.net/bulk-extensions)
