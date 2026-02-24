# Phase 38: Auto-Hide Filter - Research

**Researched:** 2026-02-24
**Domain:** ASP.NET Core LINQ query filtering on date-based cutoff, assessment group lifecycle
**Confidence:** HIGH

## Summary

Phase 38 adds a 7-day auto-hide filter to both the Monitoring and Management tabs in the Assessment view. Assessment groups (defined as `(Title, Category, Schedule.Date)` grouping) whose exam window has closed more than 7 days ago should disappear from both tabs. The implementation is pure backend — no frontend changes needed beyond displaying the filtered results.

The key challenge is understanding the date fallback logic: when `ExamWindowCloseDate` is NULL, use `Schedule.Date` instead. The filter must be applied at the database query level (in the WHERE clause before grouping) for both `GetMonitorData()` (line 277) and the `Assessment()` action's Management branch (line 115).

**Primary recommendation:** Add a computed cutoff date (`DateTime.UtcNow.AddDays(-7)`) in both methods. In the WHERE clause for AssessmentSessions, filter to keep only sessions where `(ExamWindowCloseDate ?? Schedule) >= cutoff`. This leverages SQL Server's COALESCE operator (via C# null-coalescing) to handle the fallback cleanly.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Entity Framework | 8.0+ (current project) | LINQ-to-SQL date filtering | Native ORM for .NET; already in use project-wide |
| SQL Server | (current DB) | COALESCE for NULL fallback | Native `ExamWindowCloseDate ?? Schedule` translates cleanly to SQL |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| DateTime.UtcNow | (BCL) | Current time reference | Standard for all time-based cutoff calculations |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| COALESCE in query | Check in controller, filter in-memory | In-memory filtering on potentially large datasets; less efficient |
| UTC-based cutoff | Local timezone (WIB) | UTC is standard for database storage; WIB conversion happens at display-time only |

---

## Architecture Patterns

### Confirmed Assessment Group Definition (HIGH confidence — read from CMPController.cs)

Assessment groups are NOT entities; they are query-time groupings formed by:
```csharp
.GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
```

**What defines a group:**
- **Title:** Assessment name (e.g., "Safety Induction")
- **Category:** Assessment type (e.g., "IHT", "OTS", "Assessment OJ")
- **Schedule.Date:** The scheduled date (without time component)

Multiple AssessmentSession records with the same (Title, Category, date) are grouped together. The group represents "all workers assigned to this assessment scheduled for this date."

### Confirmed AssessmentSession Model Fields (HIGH confidence — read from Models/AssessmentSession.cs)

| Field | Type | Purpose | Key for Phase 38 |
|-------|------|---------|------------------|
| `Id` | int | Primary key | Used to identify individual sessions |
| `Title` | string | Assessment name | Part of group key |
| `Category` | string | Assessment type | Part of group key |
| `Schedule` | DateTime | Scheduled date+time | Part of group key (date portion); fallback if ExamWindowCloseDate is NULL |
| `Status` | string | "Open", "Upcoming", "Completed", "InProgress", "Abandoned" | Determines which sessions appear in tabs |
| `ExamWindowCloseDate` | DateTime? | Hard cutoff for exam access | Primary date for 7-day filter; NULL if not set |
| `CompletedAt` | DateTime? | When exam was completed | Used by existing filter in GetMonitorData (30-day cutoff on Completed status) |

### Confirmed Existing Cutoff Logic (HIGH confidence — read from CMPController.cs lines 285-291)

GetMonitorData already applies a 30-day cutoff for Completed assessments:
```csharp
var cutoff = DateTime.UtcNow.AddDays(-30);
var monitorSessions = await _context.AssessmentSessions
    .Where(a => a.Status == "Open"
             || a.Status == "InProgress"
             || a.Status == "Abandoned"
             || a.Status == "Upcoming"
             || (a.Status == "Completed" && a.CompletedAt != null && a.CompletedAt >= cutoff))
```

**Phase 38 will ADD a second filter** on top of this, not replace it. Groups older than 7 days (by ExamWindowCloseDate or Schedule date) should be filtered out regardless of status.

### Pattern: Date Cutoff Filter with Fallback

**What:** Use `ExamWindowCloseDate ?? Schedule` to compute an effective close date; filter groups where this date is >= 7 days ago (UTC).

**When to use:** Any time you need to filter by a date field that may be NULL and has a fallback.

**Example:**
```csharp
// In the WHERE clause, before grouping
var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

var sessions = await _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)
    .ToListAsync();
```

**Critical:** The comparison is `>= sevenDaysAgo`, meaning:
- A group closed **exactly 7 days ago** → **still visible** (day 7)
- A group closed **8 or more days ago** → **hidden** (day 8+)

This matches the success criterion: "An assessment group closed exactly 7 days ago is still visible — it disappears on day 8."

### Pattern: Grouping After Filtering

**In GetMonitorData (lines 327-378):**
1. Filter sessions by status AND 7-day cutoff (WHERE clause)
2. Project to anonymous type
3. Group by (Title, Category, Schedule.Date)
4. Build MonitoringGroupViewModel

The filter must happen at step 1 (before ToListAsync) — if you group first, then filter, you lose the database query optimization.

**In Assessment Management (lines 115-177):**
1. Start query with AssessmentSessions
2. Apply search filter (if present) — optional
3. **ADD 7-day cutoff filter** (WHERE clause) — **new in Phase 38**
4. Project to anonymous type
5. Group by (Title, Category, Schedule.Date)
6. Build grouped view

### Anti-Patterns to Avoid

- **Filtering after ToListAsync():** If you call `.ToListAsync()` before filtering, all sessions are loaded into memory first. Use WHERE clauses before `.ToListAsync()` to push the filter to the database.
- **Using local DateTime.Now instead of DateTime.UtcNow:** The database stores UTC times; always compare UTC to UTC.
- **Forgetting the null coalescing in the comparison:** Using `a.ExamWindowCloseDate >= sevenDaysAgo` without the fallback will exclude all NULL rows. Must use `(a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo`.
- **Comparing dates with time components:** Group key uses `a.Schedule.Date` (date only), but the cutoff comparison uses full DateTime. This is correct — the cutoff should use time to give the full 24-hour window on day 7. The grouping uses `.Date` to combine all times on the same day into one group.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Date arithmetic (days) | Manual DateTime calculations | `DateTime.UtcNow.AddDays(n)` | Framework method; handles daylight saving, month boundaries, leap years |
| NULL-to-fallback logic | if (a.ExamWindowCloseDate != null) ... else ... | `a.ExamWindowCloseDate ?? a.Schedule` | Cleaner syntax; translates cleanly to SQL COALESCE |
| Date comparison across timezones | Manual offset math | DateTime.UtcNow | Store and compare everything in UTC; convert to local only for display |

**Key insight:** The null coalescing operator `??` in C# LINQ translates directly to SQL's COALESCE, making this pattern very efficient and readable.

---

## Common Pitfalls

### Pitfall 1: Filtering After Grouping
**What goes wrong:** Code groups sessions first, then attempts to filter the groups. Groups are already formed; filtering removes whole groups from the result, but the original query already materialized.
**Why it happens:** Developer thinks "filter groups" instead of "filter sessions before grouping."
**How to avoid:** Always apply WHERE filters BEFORE calling `.GroupBy()`. The grouping should see only sessions that pass the filter.
**Warning signs:** Query result includes groups that should be hidden; performance is slow (all sessions loaded before filtering).

Example (BAD):
```csharp
var allSessions = await _context.AssessmentSessions.ToListAsync();
var grouped = allSessions
    .GroupBy(a => (a.Title, a.Category, a.Schedule.Date))
    .Where(g => /* try to filter here */);  // ← Too late; all sessions already loaded
```

Example (GOOD):
```csharp
var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
var filtered = await _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo)  // ← Before grouping
    .ToListAsync();
var grouped = filtered
    .GroupBy(a => (a.Title, a.Category, a.Schedule.Date));
```

### Pitfall 2: Null Coalescing Returns Wrong Type
**What goes wrong:** `ExamWindowCloseDate ?? Schedule` sometimes returns NULL if both sides are NULL (in rare edge cases). The code compiles but comparison fails.
**Why it happens:** Misunderstanding what ?? does — it only works if the right side is guaranteed non-NULL.
**How to avoid:** `Schedule` is NOT nullable (it's always set on AssessmentSession). Verify both fields in the model. In this case, `ExamWindowCloseDate ?? Schedule` is always safe — if ExamWindowCloseDate is NULL, Schedule is non-NULL.
**Warning signs:** Null reference exception at runtime; NLP error in query translation.

### Pitfall 3: DateTime.Now vs DateTime.UtcNow
**What goes wrong:** Using `DateTime.Now` (local machine time) instead of `DateTime.UtcNow` (UTC) gives wrong cutoff when server timezone doesn't match storage timezone.
**Why it happens:** Habit — local time feels more intuitive.
**How to avoid:** Database stores all dates in UTC. Always use `DateTime.UtcNow` for cutoff calculations. Display-time conversion (to WIB for user view) happens in the view, not in the query.
**Warning signs:** Cutoff dates are off by timezone offset (e.g., 7 hours).

### Pitfall 4: Date Comparison Includes Time Component
**What goes wrong:** Using `>= sevenDaysAgo` where `sevenDaysAgo` is a DateTime (includes time) can cause a group to hide before midnight. A group with ExamWindowCloseDate of "2026-02-17 00:00:00" should be visible on day 7 (2026-02-24 midnight), but if cutoff is computed at 10 AM on day 8, the comparison might exclude it.
**Why it happens:** Confusion between date (date only) and datetime (date + time).
**How to avoid:** This is actually correct behavior — the filter is applied at the current moment, not at midnight. If an assessment closed at Feb 17 at 3 AM, and it's now Feb 24 at 10 AM, it's 7 days 7 hours old — past the 7-day window. The time component matters. No change needed; this is the intended behavior.

Actually, **the pitfall is the opposite**: not including time. If you use `.Date` on the cutoff, you might show groups that are 8+ days old. Use the full DateTime for the cutoff comparison.

### Pitfall 5: Missing the Fallback in Both Queries
**What goes wrong:** Phase 38 must modify BOTH GetMonitorData and the Assessment Management branch. If only one is updated, the tabs show different results.
**Why it happens:** Easy to forget that there are two code paths that return assessment lists.
**How to avoid:** Search for all `.Where()` clauses that filter AssessmentSessions in CMPController. There are exactly two in this phase: Assessment Management (line 115) and GetMonitorData (line 286). Update both.
**Warning signs:** Monitoring tab hides old groups but Management tab shows them (or vice versa).

### Pitfall 6: Edge Case — Group with NULL ExamWindowCloseDate and Very Old Schedule
**What goes wrong:** An assessment has no ExamWindowCloseDate (NULL) and Schedule date is from 1 month ago. The fallback to Schedule should hide it after 7 days, but if the fallback logic is wrong, it might stay visible forever.
**Why it happens:** Incomplete testing of the NULL fallback case.
**How to avoid:** Test with an assessment that has NULL ExamWindowCloseDate and Schedule date > 7 days ago. Verify it's hidden. The filter `(a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo` will evaluate `a.Schedule >= sevenDaysAgo`; if Schedule is old, the condition is FALSE, and the session is filtered out. Correct.
**Warning signs:** Assessments with old Schedule dates and no ExamWindowCloseDate never disappear.

---

## Code Examples

### GetMonitorData Filter Update

```csharp
// Source: C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs lines 275–306
// Current code applies 30-day cutoff on Completed status only
// Phase 38 adds 7-day cutoff on ExamWindowCloseDate ?? Schedule

// NEW: Add 7-day cutoff
var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

var monitorSessions = await _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo  // ← NEW: 7-day filter
             && (a.Status == "Open"
                 || a.Status == "InProgress"
                 || a.Status == "Abandoned"
                 || a.Status == "Upcoming"
                 || (a.Status == "Completed" && a.CompletedAt != null && a.CompletedAt >= DateTime.UtcNow.AddDays(-30))))
    .Select(a => new
    {
        a.Id,
        a.Title,
        a.Category,
        a.Schedule,
        a.Status,
        a.Score,
        a.IsPassed,
        a.CompletedAt,
        a.StartedAt,
        UserFullName = a.User != null ? a.User.FullName : "Unknown",
        UserNIP      = a.User != null ? a.User.NIP      : ""
    })
    .ToListAsync();
```

### Assessment Management Filter Update

```csharp
// Source: C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs lines 112–152
// Current code filters by search only
// Phase 38 adds 7-day cutoff

// NEW: Add 7-day cutoff
var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

var managementQuery = _context.AssessmentSessions
    .Where(a => (a.ExamWindowCloseDate ?? a.Schedule) >= sevenDaysAgo);  // ← NEW: 7-day filter

if (!string.IsNullOrEmpty(search))
{
    var lowerSearch = search.ToLower();
    managementQuery = managementQuery.Where(a =>
        a.Title.ToLower().Contains(lowerSearch) ||
        a.Category.ToLower().Contains(lowerSearch) ||
        (a.User != null && (
            a.User.FullName.ToLower().Contains(lowerSearch) ||
            (a.User.NIP != null && a.User.NIP.Contains(lowerSearch))
        ))
    );
}

var allSessions = await managementQuery
    .OrderByDescending(a => a.Schedule)
    .Select(a => new { /* ... existing fields ... */ })
    .ToListAsync();
```

### What the Filter Does (Step-by-Step)

For an assessment group with:
- ExamWindowCloseDate: 2026-02-17 (set explicitly)
- Schedule: 2026-02-16

**On 2026-02-24 (day 7):**
- `sevenDaysAgo = 2026-02-17 00:00:00`
- `ExamWindowCloseDate ?? Schedule = 2026-02-17`
- `2026-02-17 >= 2026-02-17` → TRUE → **VISIBLE**

**On 2026-02-25 (day 8):**
- `sevenDaysAgo = 2026-02-18 00:00:00`
- `ExamWindowCloseDate ?? Schedule = 2026-02-17`
- `2026-02-17 >= 2026-02-18` → FALSE → **HIDDEN**

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Permanent assessment list | 30-day Completed cutoff (already in GetMonitorData) | Phase 38 | Reduces clutter for long-running systems |
| No cutoff on Management tab | Phase 38 adds 7-day cutoff | 2026-02-24 | Both tabs now use same filter; ensures consistency |
| Manual NULL-check in code | Null coalescing `??` in query | LINQ standard | Cleaner, pushes logic to SQL, better performance |

---

## Open Questions

1. **WIB timezone display vs UTC storage**
   - What we know: Database stores all times in UTC. GetMonitorData already applies WIB display conversion at line 309 (`nowWib = DateTime.UtcNow.AddHours(7)`) for auto-transition of "Upcoming" to "Open". Phase 38 cutoff uses UTC for consistency.
   - What's unclear: Should the 7-day cutoff respect WIB timezone (i.e., consider "7 days ago" in WIB, not UTC)? Or use UTC cutoff (current approach)?
   - Recommendation: Use UTC cutoff (pure UTC arithmetic). The 7-day rule applies at the UTC level. If an assessment closed at Feb 17 05:00 UTC (12:00 PM WIB), it closes in UTC terms on Feb 17; the WIB display happens in the UI layer only. This keeps the database logic timezone-agnostic.

2. **Should hidden groups be completely inaccessible or just filtered from list?**
   - What we know: Success criteria say "no longer appears in the Monitoring tab" and "absent from the Management tab" — implies hidden from view.
   - What's unclear: If a user directly navigates to `/Assessment/AssessmentMonitoringDetail?title=X&category=Y&scheduleDate=Z` for a hidden group (7+ days old), should they be blocked? Or just filtered from the lazy-load list?
   - Recommendation: Groups hidden from the lazy-load list should also be blocked from detail view. Add the same 7-day filter to `AssessmentMonitoringDetail()` method (line 384). This prevents users from accessing hidden groups via direct URL.

3. **Should the 7-day cutoff apply to all statuses or only certain statuses?**
   - What we know: Success criteria imply the cutoff applies universally (groups disappear from both tabs after 7 days, regardless of status).
   - What's unclear: What if a group has Status = "Open" and ExamWindowCloseDate was 10 days ago? The assessment is technically over (past the close date), but Status might not reflect that yet. Should an open-but-closed assessment be hidden?
   - Recommendation: Yes, hide it. The ExamWindowCloseDate is the authoritative close marker. If it's past, the assessment is no longer active, and the group should hide after 7 days regardless of Status value.

---

## Sources

### Primary (HIGH confidence)

- `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AssessmentSession.cs` (lines 1–56) — confirmed `ExamWindowCloseDate` (nullable DateTime), `Schedule` (DateTime, non-nullable), all grouping fields
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs` (lines 83–198, 275–379) — confirmed Assessment() Management branch structure, GetMonitorData() structure, existing GroupBy pattern, existing 30-day Completed cutoff
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Migrations/20260220135244_AddExamWindowCloseDate.cs` — confirmed ExamWindowCloseDate column added, type `datetime2`, nullable
- `C:/Users/rinoa/Desktop/PortalHC_KPB/Data/ApplicationDbContext.cs` — confirmed AssessmentSessions DbSet, existing indexes on Schedule

### Secondary (MEDIUM confidence)

- Entity Framework Core documentation — null-coalescing operator (`??`) in LINQ-to-SQL translates to SQL COALESCE; `DateTime.UtcNow.AddDays(n)` is standard practice for cutoff calculations

### Tertiary (LOW confidence)

- None applicable — all claims verified via codebase inspection or standard framework behavior

---

## Metadata

**Confidence breakdown:**
- Assessment group definition: HIGH — GroupBy pattern confirmed in code
- Model fields (ExamWindowCloseDate, Schedule): HIGH — read directly from model
- Existing filter patterns: HIGH — read from CMPController
- DateTime null-coalescing behavior: MEDIUM — standard LINQ pattern; behavior verified in framework docs
- Two code paths (GetMonitorData, Assessment Management): HIGH — confirmed via line-by-line inspection

**Research date:** 2026-02-24
**Valid until:** 2026-03-26 (stable codebase; 30-day window)

**Key file locations for Phase 38 implementation:**
- Main controller: `C:/Users/rinoa/Desktop/PortalHC_KPB/Controllers/CMPController.cs`
  - Assessment() method, Management branch: lines 112–197
  - GetMonitorData() method: lines 277–379
  - AssessmentMonitoringDetail() method: lines 384–481 (may need 7-day filter added for consistency)
- Model: `C:/Users/rinoa/Desktop/PortalHC_KPB/Models/AssessmentSession.cs` (no changes needed)
- View: `C:/Users/rinoa/Desktop/PortalHC_KPB/Views/CMP/Assessment.cshtml` (no changes needed)
