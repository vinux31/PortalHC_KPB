# Phase 29: Auto-transition Upcoming to Open - Research

**Researched:** 2026-02-21
**Domain:** Assessment Status Management, DateTime Handling, Query Patterns
**Confidence:** HIGH

## Summary

Phase 29 implements automatic status transition for assessment sessions: when a session with status "Upcoming" has a scheduled date on or before today, it must transition to "Open" without manual HC action. This is a read-time mutation pattern applied at the query layer.

The codebase uses ASP.NET Core with Entity Framework Core 8.x (SQL Server). DateTime comparisons use `DateTime.UtcNow` consistently, and Schedule is stored as a single `DateTime` property (not a separate entity). Status is a simple string enum ("Open", "Upcoming", "Completed", "InProgress", "Abandoned").

**Primary recommendation:** Implement auto-transition as a query-time filter (via a helper method) applied before status checks in GetMonitorData, Assessment list views, and StartExam — not as a background job. This ensures zero stale reads and no infrastructure complexity.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core | 8.x | Web framework | Project baseline |
| Entity Framework Core | 8.x | ORM | Used for all DB access |
| SQL Server | 2022+ | Database | Project baseline; supports check constraints |
| DateTime | .NET BCL | Date/time handling | Language primitive; `DateTime.UtcNow` established pattern |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| LINQ | .NET 8 | Query composition | For filtering and mutation logic |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Query-time filter | Background job (Hangfire, etc.) | Requires infrastructure, polling overhead; read could be stale between jobs |
| Query-time filter | Computed column/view | DB-only logic; harder to test, version control, audit |
| Query-time filter | EF value converter | Per-property; doesn't handle cross-property mutation (Status → Schedule.Date comparison) |

**Why query-time filter is standard:** Exam systems require zero-stale-state guarantees. A worker must see "Open" the instant their scheduled time arrives, not after a job runs. Query-time mutation is proven pattern in this codebase.

## Architecture Patterns

### Current Status Query Patterns

Status is queried in three main locations:

1. **GetMonitorData** (CMPController.cs:269)
   - Filters: `Status == "Open" || Status == "InProgress" || Status == "Abandoned" || Status == "Upcoming"`
   - Groups by `(Title, Category, Schedule.Date)`
   - HC dashboard visibility

2. **Assessment list (worker view)** (CMPController.cs:~206)
   - Filters: `Status == "Open" || Status == "Upcoming"`
   - User action: can only start if status is "Open"

3. **StartExam** (CMPController.cs:2082)
   - Direct fetch: `assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == id)`
   - Checks: `Status == "Completed"` → block; `Status == "Abandoned"` → block; then transitions to "InProgress"

### Recommended Implementation Pattern

**Helper Method: ApplyScheduleAutoTransition()**

```csharp
// Location: Models/AssessmentSession.cs or a new Services/AssessmentTransitionService.cs

public static void ApplyScheduleAutoTransition(this List<AssessmentSession> sessions)
{
    var todayUtc = DateTime.UtcNow.Date; // Compare at date level for "today or earlier"
    foreach (var session in sessions)
    {
        if (session.Status == "Upcoming" && session.Schedule.Date <= todayUtc)
        {
            session.Status = "Open";
            session.UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

**Usage in GetMonitorData:**

```csharp
var monitorSessions = await _context.AssessmentSessions
    .Where(a => /* existing filters */)
    .ToListAsync(); // Bring to memory

monitorSessions.ApplyScheduleAutoTransition(); // In-memory mutation
// Then use monitorSessions for grouping
```

**Usage in StartExam:**

```csharp
var assessment = await _context.AssessmentSessions
    .FirstOrDefaultAsync(a => a.Id == id);

if (assessment != null && assessment.Status == "Upcoming" && assessment.Schedule.Date <= DateTime.UtcNow.Date)
{
    assessment.Status = "Open";
    assessment.UpdatedAt = DateTime.UtcNow;
}
// Then check final status
```

### Pattern Rationale

- **Timing:** Apply transition BEFORE any status check
- **Persistence:** Transition in-memory first, save once if state changed (or skip save in read-only contexts like GetMonitorData)
- **DateTime comparison:** Use `.Date <= DateTime.UtcNow.Date` to compare at day level (not time level) — spec says "scheduled date arrives"
- **No background job:** Transient servers and load-balanced deployments mean background jobs cannot guarantee uniqueness; query-time is always fresh

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Status mutation logic scattered | Custom query builders per endpoint | Centralized helper method | Avoids duplicate transition logic, audit trail fragmentation |
| Stale status in distributed cache | Cache invalidation strategy | Query-time mutation | Cache-busting is error-prone; fresh reads guarantee correctness |
| Scheduled transitions via polling | Custom scheduled service | EF notification on load | Simpler, fewer moving parts, no infrastructure coupling |

**Key insight:** The spec says "transition happens on the next page load or AJAX call after the scheduled date passes." This is explicitly a read-time responsibility, not a background job. The codebase confirms this: no Hangfire, no background services in Program.cs. Stay with simple, deterministic patterns.

## Common Pitfalls

### Pitfall 1: Comparing `DateTime` to `DateOnly` or Forgetting to `.Date`

**What goes wrong:** `if (assessment.Schedule < DateTime.UtcNow)` compares time-of-day; a session scheduled for 2026-02-21 08:00 won't open until 08:01 UTC on that day, confusing users who expect it open at midnight.

**Why it happens:** DateTime includes time; logic author forgets spec says "scheduled date arrives" not "scheduled time arrives."

**How to avoid:** Always compare `.Date` properties: `assessment.Schedule.Date <= DateTime.UtcNow.Date`

**Warning signs:** Test fails for session on Feb 21 at 07:00 UTC, expects "Open" but sees "Upcoming."

### Pitfall 2: Forgetting to Save After Transition

**What goes wrong:** Helper transitions status in-memory, but code path exits without `SaveChangesAsync()`. Next query still reads "Upcoming" from DB.

**Why it happens:** Helper mutates object; caller assumes mutation is persisted automatically (EF only tracks objects loaded via the context).

**How to avoid:**
- In read-only endpoints (GetMonitorData): Don't save; transition is display-only
- In mutable endpoints (StartExam, Assessment edits): Call `SaveChangesAsync()` after transition
- Document which endpoints persist vs. display-only

**Warning signs:** GetMonitorData shows "Open," but refreshing the page shows "Upcoming" again; or assessment.Schedule.Date is today but Status stays "Upcoming."

### Pitfall 3: Persisting Transition in GetMonitorData

**What goes wrong:** GetMonitorData applies transition, saves to DB. Now every HC dashboard load updates all sibling sessions, creating audit log spam and unexpected UpdatedAt timestamps.

**Why it happens:** Implementer forgets GetMonitorData is display-only; tries to simplify by persisting all mutations.

**How to avoid:** GetMonitorData transitions in-memory for display only; **do not save.** Only StartExam, Assessment edit, or an explicit HC "Open Assessment" action persists transitions.

**Warning signs:** AuditLog shows hundreds of "AssessmentSession updated" with no actor; UpdatedAt is recent but HC didn't touch anything.

### Pitfall 4: Not Handling Sibling Sessions

**What goes wrong:** Transitions only the directly-accessed session, leaving siblings with mismatched statuses. GetMonitorData groups by (Title, Category, Schedule.Date) — if one sibling is "Open" and another is "Upcoming," groupStatus logic breaks.

**Why it happens:** StartExam transition logic doesn't account for the grouping pattern established in GetMonitorData.

**How to avoid:** When transitioning a session, also transition its siblings (same Title, Category, Schedule.Date). Or apply transition at query time for all siblings together.

**Warning signs:** HC sees group status "Upcoming," clicks detail, finds one worker with "Open" session; or GetMonitorData and StartExam show different statuses for same assessment.

### Pitfall 5: Stale Reads in GetMonitorData AJAX

**What goes wrong:** GetMonitorData caches result in front-end JavaScript; user's scheduled time arrives, but dashboard doesn't refresh—still shows "Upcoming."

**Why it happens:** AJAX response is cached client-side; browser doesn't re-poll GetMonitorData.

**How to avoid:** Ensure frontend re-queries GetMonitorData on page load or periodic refresh (e.g., every 30s). Document that GetMonitorData must not be cached by browsers (`Cache-Control: no-cache`).

**Warning signs:** User loads dashboard at 07:59, refreshes at 08:05, still sees "Upcoming" despite schedule date being today.

## Code Examples

Verified patterns from codebase:

### Current Status Query Pattern (GetMonitorData)

```csharp
// Source: CMPController.cs:269-298
public async Task<IActionResult> GetMonitorData()
{
    var cutoff = DateTime.UtcNow.AddDays(-30);
    var monitorSessions = await _context.AssessmentSessions
        .Where(a => a.Status == "Open"
                 || a.Status == "InProgress"
                 || a.Status == "Abandoned"
                 || a.Status == "Upcoming"
                 || (a.Status == "Completed" && a.CompletedAt != null && a.CompletedAt >= cutoff))
        .Select(a => new { /* projection */ })
        .ToListAsync();

    // Grouping pattern (will be updated to apply transition before grouping)
    var monitorGroups = monitorSessions
        .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
        .Select(g => { /* ... */ });

    return Json(monitorGroups);
}
```

### Current Status Mutation Pattern (StartExam)

```csharp
// Source: CMPController.cs:2128-2133
if (assessment.StartedAt == null)
{
    assessment.Status = "InProgress";
    assessment.StartedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```

**Adaptation for Upcoming → Open transition:**

```csharp
var assessment = await _context.AssessmentSessions
    .FirstOrDefaultAsync(a => a.Id == id);

if (assessment == null) return NotFound();

// Apply auto-transition before any checks
if (assessment.Status == "Upcoming" && assessment.Schedule.Date <= DateTime.UtcNow.Date)
{
    assessment.Status = "Open";
    assessment.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}

// Then existing status checks (Completed, Abandoned)
if (assessment.Status == "Completed") { /* block */ }
if (assessment.Status == "Abandoned") { /* block */ }

// Then mark InProgress on first load
if (assessment.StartedAt == null)
{
    assessment.Status = "InProgress";
    assessment.StartedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```

### Worker Assessment List Query (Apply Transition)

```csharp
// Source: CMPController.cs:206
// Current:
query = query.Where(a => a.Status == "Open" || a.Status == "Upcoming");

// After auto-transition applied:
var sessions = await query.ToListAsync();
sessions.ForEach(a => {
    if (a.Status == "Upcoming" && a.Schedule.Date <= DateTime.UtcNow.Date)
        a.Status = "Open";
});

// Then build view model from sessions
var viewModel = sessions.Select(a => new AssessmentSessionViewModel {
    Status = a.Status, /* ... */
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| HC manually clicks "Open Assessment" button | Automatic on scheduled date | Phase 29 | Reduces HC workload, eliminates stale "Upcoming" states |
| Status checked only once on page load | Status checked on every query (GetMonitorData, StartExam, Assessment list) | Phase 29 | Guarantees fresh state, no stale cache |

**Deprecated/outdated:**
- Manual assessment opening: Phase 29 replaces this with auto-transition

## Open Questions

1. **Should transitions be persisted in GetMonitorData, or display-only?**
   - What we know: Spec says "transition happens on next page load or AJAX call"; doesn't specify persistence
   - What's unclear: Does HC expect UpdatedAt to change when they view dashboard? Will audit log spam be tolerable?
   - Recommendation: **Display-only** in GetMonitorData. Persist only in StartExam and HC edit actions. This avoids audit spam and keeps UpdatedAt meaningful (reflects HC actions, not display).

2. **Should we apply transition to sibling sessions together?**
   - What we know: Codebase groups assessments by (Title, Category, Schedule.Date). If siblings have different statuses, group logic breaks.
   - What's unclear: Which query endpoint should apply transition to all siblings, or just the accessed session?
   - Recommendation: Apply at GetMonitorData query level (before grouping) and StartExam (before checks). Both use sibling lookups — consistent.

3. **What's the comparison granularity: date or datetime?**
   - What we know: Spec says "scheduled date arrives"; Schedule is DateTime, not DateOnly
   - What's unclear: Does "arrives" mean start of day (00:00 UTC) or exact time?
   - Recommendation: Use `.Date` for comparisons (`Schedule.Date <= DateTime.UtcNow.Date`). This treats all sessions with same date as ready at start of day UTC. Simpler, matches user expectation.

4. **How should the transition helper be tested?**
   - What we know: No existing test suite in codebase (no *Test projects visible)
   - What's unclear: Where/how to add unit tests for transition logic
   - Recommendation: Create inline unit test file `Tests/AssessmentTransitionTests.cs` with xUnit. Test cases: (1) Upcoming before date, (2) Upcoming on date, (3) Upcoming after date, (4) Open unchanged, (5) Completed unchanged, (6) Sibling transitions together.

## Sources

### Primary (HIGH confidence)
- **CMPController.cs (lines 269-354):** GetMonitorData implementation, status filtering, grouping logic, sibling session patterns
- **CMPController.cs (lines 2080-2150):** StartExam implementation, status checks, transition to InProgress
- **Models/AssessmentSession.cs:** Status property (string), Schedule property (DateTime), navigation properties
- **Data/ApplicationDbContext.cs (lines 83-105):** AssessmentSession configuration, Schedule index, CreatedAt default, check constraints
- **Program.cs:** Middleware/service configuration — no background jobs or scheduled services; DateTime.UtcNow used for audit timestamps
- **Views/CMP/EditAssessment.cshtml (lines 18-24):** Status options in HC UI ("Open", "Upcoming", "Completed")
- **Codebase patterns:** `DateTime.UtcNow` used consistently; no Hangfire, no HostedService; query-time filtering pattern established (see ExamWindowCloseDate checks)

### Secondary (MEDIUM confidence)
- **.planning/REQUIREMENTS.md:** SCHED-01 requirement definition ("Upcoming automatically transition to Open when scheduled date arrives")
- **.planning/phases/26-*:** Phase 26 completed (Data Integrity Safeguards); confirms check constraints and audit log infrastructure

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** - ASP.NET Core 8.x, EF Core, SQL Server confirmed in Program.cs, DbContext, model files
- Architecture: **HIGH** - Status query patterns documented in CMPController; transition opportunity identified in StartExam
- Query patterns: **HIGH** - Sibling lookup, grouping, DateTime comparisons all verified in code
- Pitfalls: **MEDIUM** - Based on code inspection; no explicit bug reports found; recommendations derived from exam system best practices and DateTime pitfalls

**Research date:** 2026-02-21
**Valid until:** 2026-03-07 (16 days — stable domain, no framework churn expected)

**Design decisions locked by this research:**
1. Use query-time mutation, not background jobs
2. Apply transition before status checks (GetMonitorData, StartExam, worker list)
3. Compare at `.Date` level, not full DateTime
4. Persist in StartExam/edit actions, display-only in GetMonitorData
5. Transition siblings together (same Title, Category, Schedule.Date)
