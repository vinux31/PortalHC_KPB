# Research Documentation: Auto-Save, Session Resume, and Polling Features

**Date:** 2026-02-24
**Status:** Complete
**Confidence Level:** HIGH

## Overview

This directory contains comprehensive research on the **10 critical pitfalls** that emerge when adding auto-save, session resume, and polling features to an existing ASP.NET Core 8.0 MVC exam system. The research is organized into three main documents:

### Research Files

#### 1. **SUMMARY-AUTO-SAVE-RESUME-POLLING.md** (Roadmap Planning Document)
**Use this file for:** Phase planning, milestone scheduling, risk assessment

- Executive summary of all findings
- 10 pitfall categories with prevention strategies
- Recommended 5-phase rollout (Phase 41–45)
- Phase dependencies and blockers
- Load testing scenarios
- Quick-start checklist

**Audience:** Project lead, phase planners, technical architects

---

#### 2. **PITFALLS-AUTO-SAVE-RESUME-POLLING.md** (Implementation Reference)
**Use this file for:** Phase execution, code review, testing strategy

- **Pitfalls 1–6:** CRITICAL (cause data corruption, security breaches, cascading failures)
- **Pitfalls 7–8:** MODERATE (cause poor UX, inaccurate monitoring)
- **Pitfalls 9–10:** MINOR (poor DX, information leaks)

Each pitfall includes:
- What goes wrong (detailed scenario with timeline)
- Why it happens (root cause analysis)
- Consequences (business impact)
- Prevention (concrete ASP.NET Core + EF Core patterns, code examples)
- Detection (how to identify in production)

**Audience:** Developers, QA engineers, code reviewers

---

## Key Findings at a Glance

### Critical Pitfalls (Must Address in Implementation)

| # | Pitfall | Phase | Fix Category | Confidence |
|---|---------|-------|---|---|
| 1 | Concurrent auto-save creates duplicate answers | 41 | Client debounce + atomic upsert + unique constraint | HIGH |
| 2 | Timer drift and client manipulation | 42 | Server-side elapsed time, remove client timer from API | HIGH |
| 3 | Double-submit race (SubmitExam vs Close Early) | 41+44 | RowVersion + optimistic concurrency | HIGH |
| 4 | TempData token consumed by polling | 41+43 | Move token to database (AssessmentSession.TokenVerified) | HIGH |
| 5 | Polling storm exhausts database | 43 | IMemoryCache 5s TTL + invalidation on state change | HIGH |
| 6 | Upsert conflict via FirstOrDefault pattern | 41 | ExecuteUpdateAsync or database-level upsert | HIGH |
| 7 | Question count changes break resume | 42 | Freeze question count at exam start, prevent mid-exam edits | HIGH |
| 8 | Progress tracking is misleading | 41+45 | Count non-null answers only, recalculate on submit | MEDIUM |
| 9 | SaveAnswer returns no state | 41 | Return answered count + timestamp + confirmation | HIGH |
| 10 | Polling exposes session to wrong user | 43 | Authorization check before returning status | HIGH |

---

## Phased Implementation Map

### Phase 41: SaveAnswer with Auto-Save
**Focus:** Prevent concurrent insert race conditions
**Files to implement:**
- SaveAnswer API endpoint with ExecuteUpdateAsync (atomic upsert)
- JavaScript debounce on exam question clicks (300ms)
- Database migration: unique constraint on (sessionId, qId)
- Database migration: [Timestamp] on AssessmentSession + PackageUserResponse

**Pitfalls addressed:** #1, #6, #9
**Testing:** Load test with rapid clicks (1000+ clicks/second)

---

### Phase 42: Session Resume with Elapsed Time Tracking
**Focus:** Prevent timer drift and support disconnected workers
**Files to implement:**
- ElapsedMinutesBeforePause field on AssessmentSession
- Pause/resume logic in StartExam
- Verify question count frozen in UserPackageAssignment
- Prevent HC from modifying package during InProgress

**Pitfalls addressed:** #2, #7
**Testing:** Resume after 5+ min disconnect, verify timer hasn't drifted

---

### Phase 43: Worker Polling with Caching
**Focus:** Prevent database overload from concurrent polling
**Files to implement:**
- CheckExamStatus endpoint with IMemoryCache (5s TTL)
- Cache invalidation in CloseEarlySession
- Move token verification to AssessmentSession.TokenVerified
- Authorization check (user owns session)

**Pitfalls addressed:** #4, #5, #10
**Testing:** 100 concurrent workers polling for 10 minutes

---

### Phase 44: Close Early with Concurrency Control
**Focus:** Prevent double-submit race when HC and worker overlap
**Files to implement:**
- Verify RowVersion check in SubmitExam and CloseEarlySession
- Serializable transaction option for status transitions
- Conflict handling (DbUpdateConcurrencyException)

**Pitfalls addressed:** #3
**Testing:** Worker submit at same time as HC close early (< 100ms offset)

---

### Phase 45: HC Live Monitoring
**Focus:** Accurate progress tracking without misleading metrics
**Files to implement:**
- HC monitoring endpoint with progress cache (10s TTL)
- Progress counts non-null answers only
- Recalculate Progress on every SaveAnswer
- Audit log for SaveAnswer (optional but recommended)

**Pitfalls addressed:** #8, (reduce #5 for HC side)
**Testing:** Verify progress displayed vs actual answered count match

---

## Quick Reference: Prevention Patterns

### Pattern 1: Atomic Upsert (Pitfalls #1, #6)
```csharp
// CORRECT: Atomic at database level
var updated = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == qId)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.PackageOptionId, selectedOptionId)
        .SetProperty(r => r.SubmittedAt, DateTime.UtcNow));

if (updated == 0) {
    _context.PackageUserResponses.Add(new PackageUserResponse { ... });
    await _context.SaveChangesAsync();
}
```

### Pattern 2: Server-Side Elapsed Time (Pitfall #2)
```csharp
// CORRECT: Server-authoritative
if (assessment.StartedAt.HasValue) {
    var elapsed = DateTime.UtcNow - assessment.StartedAt.Value;
    if (elapsed.TotalMinutes > assessment.DurationMinutes + 2) {
        return BadRequest("Time exceeded");
    }
}
// NOT: Accept client-supplied elapsed time
```

### Pattern 3: Optimistic Concurrency (Pitfall #3)
```csharp
public class AssessmentSession {
    ...
    [Timestamp]
    public byte[] Version { get; set; } // Auto-increments on update
}

// On conflict, throws DbUpdateConcurrencyException
try {
    await _context.SaveChangesAsync();
} catch (DbUpdateConcurrencyException ex) {
    return Conflict("Another admin action was applied simultaneously");
}
```

### Pattern 4: Memory Cache (Pitfall #5)
```csharp
// Cache with 5s TTL
if (!_cache.TryGetValue($"exam_status_{sessionId}", out var status)) {
    status = await _context.AssessmentSessions
        .AsNoTracking()
        .Select(a => new { a.Status })
        .FirstOrDefaultAsync(a => a.Id == sessionId);
    _cache.Set($"exam_status_{sessionId}", status, TimeSpan.FromSeconds(5));
}

// Invalidate on state change
_cache.Remove($"exam_status_{sessionId}");
```

### Pattern 5: Database Token Verification (Pitfall #4)
```csharp
// WRONG: TempData consumed on first read
var tokenVerified = TempData[$"TokenVerified_{id}"];

// CORRECT: Database persistence
public class AssessmentSession {
    public bool TokenVerified { get; set; }
    public DateTime? TokenVerifiedAt { get; set; }
}
```

### Pattern 6: Client Debounce (Pitfall #1)
```javascript
// Debounce SaveAnswer AJAX calls
let saveTimeout;
function onOptionChange(qId, optId) {
    clearTimeout(saveTimeout);
    saveTimeout = setTimeout(() => {
        fetch('/CMP/SaveAnswer', {
            method: 'POST',
            body: JSON.stringify({ sessionId, questionId: qId, selectedOptionId: optId })
        });
    }, 300); // 300ms debounce
}
```

---

## Testing Checklist

### Phase 41 Testing
- [ ] Single worker: 1000 rapid clicks on single question → 0 duplicates in DB
- [ ] Concurrent: 10 workers, same question, simultaneous saves → 0 duplicates
- [ ] Unique constraint: INSERT duplicate (sessionId, qId) → 2627 violation error
- [ ] RowVersion: Two concurrent updates → DbUpdateConcurrencyException on second

### Phase 42 Testing
- [ ] Start exam, pause, wait 5 min, resume → elapsed time = 5 min, not 0 or 10 min
- [ ] Resume: question count frozen → if new questions added, resume is blocked
- [ ] Client timer: modify via DevTools → server timer unaffected

### Phase 43 Testing
- [ ] 100 concurrent workers polling every 10s for 10 min → DB queries ≈ 12 (not 600)
- [ ] Cache invalidation: HC closes exam → next poll (within 5s) sees Closed status
- [ ] Auth: polling with wrong user ID → 403 Forbid

### Phase 44 Testing
- [ ] SubmitExam at T=3599, CloseEarlySession at T=3600 → no data loss
- [ ] Concurrent SubmitExam + SubmitExam → second one rejected (idempotent)

### Phase 45 Testing
- [ ] Progress = 40/100 (non-null) even if 60 rows (with some null) → correct display
- [ ] HC monitoring endpoint cached → query latency < 10ms (no DB hit within 10s window)

---

## Sources and References

### Official Microsoft Documentation
- [EF Core Concurrency Handling](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [ASP.NET Core Session State Management](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0)
- [ASP.NET Core MVC Tutorial - Concurrency](https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/concurrency?view=aspnetcore-8.0)

### Industry Best Practices
- [Solving Race Conditions with EF Core Optimistic Locking](https://www.milanjovanovic.tech/blog/solving-race-conditions-with-ef-core-optimistic-locking)
- [API Polling Best Practices](https://www.merge.dev/blog/api-polling-best-practices)
- [Cache Optimization Strategies](https://redis.io/blog/guide-to-cache-optimization-strategies/)
- [OWASP Session Management](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)

---

## Document Versions

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-02-24 | Initial research complete; 10 pitfalls documented, 5 phases planned |

