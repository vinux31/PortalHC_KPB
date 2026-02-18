# Phase 10: Unified Training Records - Research

**Researched:** 2026-02-18
**Domain:** ASP.NET Core MVC — view-layer merge of two data sources (AssessmentSession + TrainingRecord) into a single unified Razor table with role-branching controller logic
**Confidence:** HIGH — all findings are from direct file reads of the actual codebase; no inference or external sources required

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Table column strategy**
- Single unified table — all columns always visible (no type-conditional hiding)
- Non-applicable cells show `—` (em dash), not a blank — makes "field doesn't apply" explicit
- Column order: Date | Type | Nama/Judul | Score | Pass/Fail | Penyelenggara | Tipe Sertifikat | Berlaku Sampai | Status
- Type column uses a colored badge (pill): e.g., blue for "Assessment Online", green for "Training Manual"

**Table sorting**
- Sorted most-recent-first by date
- Tie-break: Assessment rows appear before Training Manual rows on the same date

**HC worker list metric**
- Completion metric shows a combined count, not a percentage: e.g., "5 completed (3 assessments + 2 trainings)"
- What counts as complete for assessments: `IsPassed = true` only (failed attempts excluded)
- What counts as complete for training records: `Status = Passed` or `Status = Valid` only (Pending, Expired, etc. excluded)
- No percentage denominator — training records have no defined "expected" ceiling

**Certificate expiry warnings**
- Warning shown only when record is already expired (past `Berlaku Sampai` date)
- No lookahead window — no "expiring soon" warning in this phase

**Empty state**
- Worker with no records sees: "Belum ada riwayat pelatihan" (plain text, no call to action)

**Role behavior: Admin SelectedView**
- Admin in `SelectedView="Coachee"` sees the HC worker list (elevated access), not individual records
- Admin always gets the highest-access view regardless of simulated role

### Claude's Discretion
- Badge color choices (exact hex/Bootstrap class for blue/green pills)
- Responsive behavior of the merged table on smaller screens
- Worker list column structure in HC view (beyond the completion count)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

---

## Summary

Phase 10 is a view-layer merge. No new database tables, no migrations, no new routes. Two existing DbSets — `AssessmentSessions` and `TrainingRecords` — are queried together in the existing `Records()` action and merged into a single ordered list exposed via a new ViewModel. The new ViewModel (`UnifiedTrainingRecord`) bridges the two source types into a flat projection that the Razor view can render row-by-row with no conditional branching per column.

The critical technical insight is the date-plus-type sort: the controller must project both sources into the same type *before* ordering, so that `.OrderByDescending(r => r.Date).ThenBy(r => r.SortPriority)` works across the combined list. EF Core cannot do cross-DbSet ordering in a single query — the merge must happen in memory after two separate database queries complete. This is acceptable at this scale (one user's records at a time for the worker view).

The HC worker list replaces the existing `WorkerTrainingStatus` percentage model. The existing `GetWorkersInSection()` helper currently loads `TrainingRecords` via `Include()` but ignores `AssessmentSessions` entirely. It must be extended to also load each user's passed assessment count. The new completion display string format ("5 completed (3 assessments + 2 trainings)") replaces the `CompletionPercentage` integer — the `WorkerTrainingStatus` model needs two new integer fields: `CompletedAssessments` and `CompletedTrainings`.

**Primary recommendation:** Build a `UnifiedTrainingRecord` ViewModel class, extend `GetPersonalTrainingRecords()` to merge both sources, replace the `Records.cshtml` tab-based layout with a single flat table, update `WorkerTrainingStatus` with combined count fields, and update `GetWorkersInSection()` to query AssessmentSessions. No migrations needed.

---

## Standard Stack

### Core (No New Dependencies)

| Library | Version | Purpose | Notes |
|---------|---------|---------|-------|
| ASP.NET Core MVC | 10.0 (project target) | Controller, routing | Already in use |
| EF Core | 10.0 (inferred from net10.0 target) | Database queries | Already in use — two separate queries then in-memory merge |
| Razor Pages (.cshtml) | ASP.NET Core 10 | View rendering | Already in use |
| Bootstrap 5 | (via CDN, already in layout) | Badge pills, table, responsive | Already in use — use `badge rounded-pill` for Type column |
| Bootstrap Icons | (via CDN, already in layout) | Row icons | Already in use |

### No New Libraries

This phase introduces zero new dependencies. All work is in the existing controller, new ViewModel classes, and updated Razor views.

---

## Architecture Patterns

### Recommended File Changes

```
Controllers/
└── CMPController.cs              — Modify Records() action, GetPersonalTrainingRecords(),
                                    GetWorkersInSection() helper

Models/
├── UnifiedTrainingRecord.cs      — NEW: flat ViewModel for merged rows
└── WorkerTrainingStatus.cs       — MODIFY: add CompletedAssessments, CompletedTrainings fields

Views/CMP/
├── Records.cshtml                — REPLACE: tab layout → single unified table
└── RecordsWorkerList.cshtml      — MODIFY: completion count display format
```

### Pattern 1: UnifiedTrainingRecord ViewModel

**What:** A flat class that can represent either an AssessmentSession or a TrainingRecord. Non-applicable fields are null (rendered as `—` in the view).
**When to use:** This is the only model the Records view consumes. The controller populates it from both sources.

```csharp
// Source: codebase analysis — pattern derived from AssessmentSession.cs and TrainingRecord.cs

namespace HcPortal.Models
{
    public class UnifiedTrainingRecord
    {
        // Common fields (always populated)
        public DateTime Date { get; set; }
        public string RecordType { get; set; } = ""; // "Assessment Online" | "Training Manual"
        public string Title { get; set; } = "";      // AssessmentSession.Title | TrainingRecord.Judul

        // Assessment-only fields (null for Training Manual rows)
        public int? Score { get; set; }              // AssessmentSession.Score
        public bool? IsPassed { get; set; }          // AssessmentSession.IsPassed

        // Training Manual-only fields (null for Assessment rows)
        public string? Penyelenggara { get; set; }   // TrainingRecord.Penyelenggara
        public string? CertificateType { get; set; } // TrainingRecord.CertificateType
        public DateTime? ValidUntil { get; set; }    // TrainingRecord.ValidUntil

        // Status — both sources have a status concept
        public string? Status { get; set; }
        // AssessmentSession: "Completed" | Training: "Passed" | "Valid" | "Wait Certificate"

        // Sorting helper: 0 for Assessment (sorts first), 1 for Training Manual
        public int SortPriority { get; set; }

        // Expiry: true only if ValidUntil is in the past (no lookahead per decision)
        public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateTime.Now;
    }
}
```

### Pattern 2: Controller Merge — Two Queries, In-Memory OrderBy

**What:** `GetPersonalTrainingRecords()` currently returns `List<TrainingRecord>`. Replace it with a method that returns `List<UnifiedTrainingRecord>`.
**When to use:** For both the worker self-view and the HC WorkerDetail view.

```csharp
// Source: CMPController.cs — extends existing GetPersonalTrainingRecords() helper

private async Task<List<UnifiedTrainingRecord>> GetUnifiedRecords(string userId)
{
    // Query 1: Completed assessments only (Status == "Completed" and IsPassed != null)
    var assessments = await _context.AssessmentSessions
        .Where(a => a.UserId == userId && a.Status == "Completed")
        .ToListAsync();

    // Query 2: All training records
    var trainings = await _context.TrainingRecords
        .Where(t => t.UserId == userId)
        .ToListAsync();

    // Project into unified type
    var unified = new List<UnifiedTrainingRecord>();

    unified.AddRange(assessments.Select(a => new UnifiedTrainingRecord
    {
        Date = a.CompletedAt ?? a.Schedule,
        RecordType = "Assessment Online",
        Title = a.Title,
        Score = a.Score,
        IsPassed = a.IsPassed,
        Penyelenggara = null,
        CertificateType = null,
        ValidUntil = null,
        Status = a.IsPassed == true ? "Passed" : "Failed",
        SortPriority = 0
    }));

    unified.AddRange(trainings.Select(t => new UnifiedTrainingRecord
    {
        Date = t.Tanggal,
        RecordType = "Training Manual",
        Title = t.Judul ?? "",
        Score = null,
        IsPassed = null,
        Penyelenggara = t.Penyelenggara,
        CertificateType = t.CertificateType,
        ValidUntil = t.ValidUntil,
        Status = t.Status,
        SortPriority = 1
    }));

    // Sort: most-recent-first; tie-break: Assessment before Training Manual
    return unified
        .OrderByDescending(r => r.Date)
        .ThenBy(r => r.SortPriority)
        .ToList();
}
```

### Pattern 3: Role Branch in Records() Action

**What:** The current `Records()` action uses `UserRoles.IsCoachingRole(userLevel)` and a manual Admin SelectedView check. Phase 10 changes the branching so Admin always gets the HC worker list (regardless of SelectedView), and only true Coach/Coachee roles get personal records.

The prior decisions note:
- Admin in any SelectedView (including Coachee) → HC worker list
- Coachee/Coach (RoleLevel >= 5, non-Admin) → personal unified table

```csharp
// Source: CMPController.cs Records() action — current logic is lines 644–677

// CURRENT (wrong for this phase):
if (userRole == UserRoles.Admin)
{
    if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
    {
        var personalRecords = await GetPersonalTrainingRecords(user.Id);
        return View("Records", personalRecords);  // Admin gets personal view in Coachee mode
    }
}

// NEW (phase 10 decision — Admin always gets HC list):
bool isHCAccess = userRole == UserRoles.Admin ||
                  userRole == UserRoles.HC ||
                  UserRoles.HasFullAccess(userLevel);  // levels 1-3

bool isCoacheeView = userRole == UserRoles.Coachee ||
                     userRole == UserRoles.Coach;       // levels 5-6, NOT Admin

if (isCoacheeView)
{
    var unified = await GetUnifiedRecords(user!.Id);
    return View("Records", unified);  // Records.cshtml updated to @model List<UnifiedTrainingRecord>
}

// HC, Admin, Management, Supervisor → worker list
```

**Key pattern match:** `isHCAccess` naming (single named bool) follows the established pattern from `CDPController.cs` lines 1025–1027. Use the same convention.

### Pattern 4: WorkerTrainingStatus Model Extension

**What:** Add two new fields for the combined count display. Do NOT remove existing fields (backward compat with any other views that may read them).

```csharp
// Source: Models/WorkerTrainingStatus.cs — add to existing class

// NEW: combined completion count fields for phase 10
public int CompletedAssessments { get; set; }  // IsPassed == true count
public int CompletedTrainings { get; set; }    // Status == "Passed" || Status == "Valid" count

// Computed display string: "5 completed (3 assessments + 2 trainings)"
public string CompletionDisplayText =>
    $"{CompletedAssessments + CompletedTrainings} completed " +
    $"({CompletedAssessments} assessments + {CompletedTrainings} trainings)";
```

### Pattern 5: HC Worker List — Query Extension in GetWorkersInSection()

**What:** The current `GetWorkersInSection()` only loads `TrainingRecords` via `Include()`. Must also query `AssessmentSessions` to compute `CompletedAssessments`.

Because EF Core cannot join across different entity types in a single query easily here, the correct pattern is: after loading users (with TrainingRecords via Include), do a single batch query for all relevant AssessmentSessions, then group in memory.

```csharp
// Source: CMPController.cs GetWorkersInSection() — extends the foreach loop at lines 738–805

// BEFORE the foreach loop, add a batch query:
var userIds = users.Select(u => u.Id).ToList();
var passedAssessments = await _context.AssessmentSessions
    .Where(a => userIds.Contains(a.UserId) && a.IsPassed == true)
    .Select(a => new { a.UserId })
    .ToListAsync();

var passedAssessmentCountByUser = passedAssessments
    .GroupBy(a => a.UserId)
    .ToDictionary(g => g.Key, g => g.Count());

// INSIDE the foreach loop, replace CompletedTrainings calculation:
var completedTrainings = trainingRecords.Count(tr =>
    tr.Status == "Passed" || tr.Status == "Valid"
    // Note: "Permanent" used in current code is NOT in the phase decision — use only Passed|Valid
);

var completedAssessments = passedAssessmentCountByUser.TryGetValue(user.Id, out var count)
    ? count
    : 0;

worker.CompletedAssessments = completedAssessments;
worker.CompletedTrainings = completedTrainings;
```

**Note on "Permanent" status:** The existing `GetWorkersInSection()` code counts `Status == "Permanent"` as complete (line 748). The CONTEXT.md decision says only `Passed` and `Valid` count. Phase 10 must correct this — only include `Passed` and `Valid` in the training completion count.

### Pattern 6: Razor View — Type Badge Pills

**What:** The Type column uses `badge rounded-pill` Bootstrap classes. Use existing Bootstrap color utilities — no custom CSS needed.

```cshtml
@* Source: Bootstrap 5 — badge rounded-pill pattern already used in project *@

@* Assessment Online — blue *@
<span class="badge rounded-pill bg-primary">Assessment Online</span>

@* Training Manual — green *@
<span class="badge rounded-pill bg-success">Training Manual</span>
```

Bootstrap `bg-primary` (#0d6efd blue) and `bg-success` (#198754 green) are the recommended choices for the discretion items. They are already used throughout the project in similar badge contexts (e.g., `WorkerDetail.cshtml` category badges).

### Pattern 7: Razor View — Em Dash for Non-Applicable Cells

**What:** Non-applicable cells must show `—` (U+2014 em dash), not empty or a hyphen.

```cshtml
@* For null fields — Assessment rows for Penyelenggara, CertificateType, ValidUntil *@
@(record.Penyelenggara ?? "—")
@(record.CertificateType ?? "—")
@(record.ValidUntil.HasValue ? record.ValidUntil.Value.ToString("dd MMM yyyy") : "—")

@* For null fields — Training rows for Score, Pass/Fail *@
@(record.Score.HasValue ? record.Score.Value.ToString() : "—")
@if (record.IsPassed.HasValue)
{
    <span class="badge @(record.IsPassed.Value ? "bg-success" : "bg-danger")">
        @(record.IsPassed.Value ? "Pass" : "Fail")
    </span>
}
else
{
    <text>—</text>
}
```

### Pattern 8: Empty State

**What:** If `Model.Count == 0`, show plain text — no icon, no CTA.

```cshtml
@if (!Model.Any())
{
    <tr>
        <td colspan="9" class="p-4 text-muted">Belum ada riwayat pelatihan</td>
    </tr>
}
```

### Pattern 9: Expired Certificate Warning

**What:** Show a warning badge inline in the Berlaku Sampai cell when `IsExpired == true`. No banner, no lookahead.

```cshtml
@if (record.ValidUntil.HasValue)
{
    if (record.IsExpired)
    {
        <span class="badge bg-danger">
            <i class="bi bi-x-circle me-1"></i>Expired
        </span>
    }
    else
    {
        <span>@record.ValidUntil.Value.ToString("dd MMM yyyy")</span>
    }
}
else
{
    <text>—</text>
}
```

### Anti-Patterns to Avoid

- **Using `IsPassed != false` to find passed assessments:** Use `IsPassed == true` explicitly. `IsPassed` is `bool?` — a null value means not yet determined. `!= false` would include null rows.
- **Single LINQ query across both DbSets:** EF Core cannot join `AssessmentSessions` and `TrainingRecords` in one query because they share no FK relationship. Always do two queries, then merge in memory.
- **Type-conditional column hiding:** The decision is "all columns always visible." Do NOT use `@if (record.RecordType == "Training Manual") { <td>...</td> }` patterns that show/hide columns per row. CSS `display:none` on a per-cell basis is also forbidden.
- **Leaving `Status == "Permanent"` in completion count:** The existing code counts "Permanent" as complete. The phase decision says only `Passed` and `Valid` count for the HC worker list metric. Remove "Permanent" from the count filter.
- **Percentage-based completion in WorkerList:** The existing `CompletionPercentage` property computes `completedCount / totalCount * 100`. The phase decision replaces this with a count string. Do not render a percentage in the new HC worker list completion column.
- **Admin personal view in Records():** The current code gives Admin a personal records view when `SelectedView == "Coachee"`. The phase decision reverses this: Admin always gets the HC worker list. Remove the Admin SelectedView personal-records branch.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Merging two result sets | Custom sort algorithm | `.OrderByDescending().ThenBy()` on projected list | LINQ covers this exactly |
| Badge styling | Custom CSS pill classes | Bootstrap `badge rounded-pill bg-primary/bg-success` | Already in project; consistent with existing badges |
| Null-safe cell rendering | Complex helper method | Null-coalescing in Razor: `@(field ?? "—")` | Sufficient for this use case |
| Batch lookup of assessment counts per user | N+1 loop queries | Single `.Where(a => userIds.Contains(a.UserId))` + `GroupBy` in memory | Avoids N+1 query in HC worker list |

---

## Common Pitfalls

### Pitfall 1: IsPassed Null Distinction
**What goes wrong:** Developer uses `a.IsPassed != false` to filter passed assessments, which includes sessions where `IsPassed` is null (not yet graded/completed).
**Why it happens:** `bool?` semantics are non-obvious. "Not false" sounds like "true" but includes null.
**How to avoid:** Always use `a.IsPassed == true` explicitly.
**Warning signs:** Worker list shows higher assessment completion counts than expected; sessions with no questions or incomplete sessions appear as "passed."

### Pitfall 2: Admin Branch Reversal
**What goes wrong:** The current `Records()` action (lines 644–659) sends Admin with `SelectedView == "Coachee"` to the personal records view. Phase 10 inverts this: Admin always gets the HC worker list.
**Why it happens:** Copy-paste from the existing controller logic without reading the phase decision.
**How to avoid:** Delete the entire `if (userRole == UserRoles.Admin)` branch that returns personal records. The Admin falls through to the HC worker list path.
**Warning signs:** Admin login with `SelectedView == "Coachee"` shows a personal records table instead of the worker list.

### Pitfall 3: Missing the "Completed" Status Filter on AssessmentSessions
**What goes wrong:** Developer queries all `AssessmentSessions` for a user without filtering `Status == "Completed"`. Sessions with Status "Open" or "Upcoming" appear in the unified table.
**Why it happens:** The current `GetPersonalTrainingRecords()` only queries TrainingRecords, so there's no precedent to copy from.
**How to avoid:** Always add `.Where(a => a.Status == "Completed")` when querying AssessmentSessions for the training records view. For the pass count in the HC worker list, also add `a.IsPassed == true`.
**Warning signs:** Upcoming assessments appear in the records table. Assessment count in HC worker list is higher than expected.

### Pitfall 4: View Model Type Mismatch
**What goes wrong:** `Records.cshtml` currently has `@model List<HcPortal.Models.TrainingRecord>` at the top. If the controller passes `List<UnifiedTrainingRecord>` without updating the `@model` directive, a runtime InvalidCastException occurs.
**Why it happens:** The view file header is easy to forget when replacing the model.
**How to avoid:** The first edit to `Records.cshtml` must be changing `@model` from `List<TrainingRecord>` to `List<UnifiedTrainingRecord>`. All downstream Razor code referencing `TrainingRecord` fields must also be updated.
**Warning signs:** Yellow error page on navigation to Records, with "The model item passed into the ViewDataDictionary is of type 'List`1[UnifiedTrainingRecord]', but this ViewDataDictionary instance requires a model item of type 'List`1[TrainingRecord]'."

### Pitfall 5: WorkerDetail View Also Uses TrainingRecord List
**What goes wrong:** `WorkerDetail.cshtml` (line 1: `@model List<HcPortal.Models.TrainingRecord>`) is used for the HC drilldown to an individual worker. Phase 10 must also update this view and the `WorkerDetail()` action to use `List<UnifiedTrainingRecord>`.
**Why it happens:** The developer updates `Records.cshtml` but forgets `WorkerDetail.cshtml` which is a separate file served by a separate action.
**How to avoid:** Treat WorkerDetail as a required touch point — same ViewModel change applies to both views.
**Warning signs:** Records.cshtml works correctly but clicking a worker row from the HC list throws a model type error on WorkerDetail.

### Pitfall 6: Responsive Table Width
**What goes wrong:** The unified table has 9 columns. On mobile, horizontal scroll is required. If the `table-responsive` wrapper is missing, the table breaks the layout.
**Why it happens:** Existing views use `table-responsive` but a new table scaffold may omit it.
**How to avoid:** Always wrap the `<table>` in `<div class="table-responsive">`. For the discretion item on responsive behavior: consider `text-nowrap` on date and status columns so content doesn't wrap within cells on narrow screens.
**Warning signs:** Table cells wrap aggressively on screens narrower than 1200px, making the table unreadable.

### Pitfall 7: Date Field for Assessment Row
**What goes wrong:** Developer uses `AssessmentSession.Schedule` as the date for the unified row. But `Schedule` is the planned date, not the completion date. For completed assessments, `CompletedAt` is the accurate date.
**Why it happens:** `Schedule` is always populated; `CompletedAt` is nullable and might be overlooked.
**How to avoid:** Use `a.CompletedAt ?? a.Schedule` as the date projection. If `CompletedAt` is null on a completed session (data quality issue), fall back to `Schedule`.
**Warning signs:** Assessments appear with dates in the future because the scheduled date hasn't passed yet even though the assessment was completed early.

---

## Code Examples

### Current Records() Action — Role Branch to Replace

```csharp
// Source: CMPController.cs lines 643–659 — CURRENT (to be replaced)

// Admin (Level 1) dengan SelectedView override - Gunakan view preference
if (userRole == UserRoles.Admin)
{
    // Jika Admin memilih view Coach/Coachee, tampilkan personal records
    if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
    {
        var personalRecords = await GetPersonalTrainingRecords(user.Id);
        return View("Records", personalRecords);
    }
    // Untuk HC/Atasan view, lanjut ke worker list (existing logic)
}

// Role: Level 5-6 (Coach/Coachee) - Show personal training records
if (UserRoles.IsCoachingRole(userLevel))
{
    var personalRecords = await GetPersonalTrainingRecords(user?.Id ?? "");
    return View("Records", personalRecords);
}
```

### New Records() Role Branch

```csharp
// Source: pattern from CDPController.cs isHCAccess pattern (lines 1025-1027)

// Phase 10: Admin always gets HC worker list (elevated access, not personal view)
bool isCoacheeView = userRole == UserRoles.Coach ||
                     userRole == UserRoles.Coachee;
// Note: Admin is explicitly excluded — Admin gets HC list regardless of SelectedView

if (isCoacheeView)
{
    var unified = await GetUnifiedRecords(user!.Id);
    return View("Records", unified);
}

// HC, Admin (all SelectedView values), Management, Supervisor → worker list
// ... (existing isInitialState logic, GetWorkersInSection call)
```

### WorkerTrainingStatus — Completion Count Display in RecordsWorkerList.cshtml

```cshtml
@* Source: RecordsWorkerList.cshtml — replace CompletionPercentage column *@

@* CURRENT (to replace): *@
@if (worker.CompletionPercentage == 100) { ... SUDAH badge ... } else { ... BELUM badge ... }

@* NEW: *@
<td class="p-3">
    <span class="fw-bold">@(worker.CompletedAssessments + worker.CompletedTrainings)</span>
    <small class="text-muted d-block">
        (@worker.CompletedAssessments assessments + @worker.CompletedTrainings trainings)
    </small>
</td>
```

### GetWorkersInSection — Batch Assessment Query

```csharp
// Source: CMPController.cs — inserted before the foreach loop (currently line 738)

var userIds = users.Select(u => u.Id).ToList();

// Batch load passed assessment counts — avoids N+1
var passedAssessmentsByUser = await _context.AssessmentSessions
    .Where(a => userIds.Contains(a.UserId) && a.IsPassed == true)
    .GroupBy(a => a.UserId)
    .Select(g => new { UserId = g.Key, Count = g.Count() })
    .ToListAsync();

var passedAssessmentLookup = passedAssessmentsByUser
    .ToDictionary(x => x.UserId, x => x.Count);

// Inside the foreach loop:
int completedAssessments = passedAssessmentLookup.TryGetValue(user.Id, out var aCount) ? aCount : 0;
int completedTrainings = trainingRecords.Count(tr => tr.Status == "Passed" || tr.Status == "Valid");

worker.CompletedAssessments = completedAssessments;
worker.CompletedTrainings = completedTrainings;
```

---

## Complete Touch Point Inventory

All files that must be modified for Phase 10. This is the complete list — confirmed by codebase audit.

| # | File | Change Type | What Changes |
|---|------|-------------|--------------|
| 1 | `Models/UnifiedTrainingRecord.cs` | CREATE NEW | New flat ViewModel for merged rows |
| 2 | `Models/WorkerTrainingStatus.cs` | MODIFY | Add `CompletedAssessments`, `CompletedTrainings` fields; add `CompletionDisplayText` computed property |
| 3 | `Controllers/CMPController.cs` | MODIFY | Replace `Records()` role branch; replace `GetPersonalTrainingRecords()` with `GetUnifiedRecords()`; extend `GetWorkersInSection()` with batch assessment query |
| 4 | `Views/CMP/Records.cshtml` | REPLACE | Tab layout → single unified table; `@model` → `List<UnifiedTrainingRecord>`; remove filter controls that only apply to TrainingRecord (category tabs, StatusFilter) |
| 5 | `Views/CMP/RecordsWorkerList.cshtml` | MODIFY | Replace completion percentage column with combined count display |
| 6 | `Views/CMP/WorkerDetail.cshtml` | MODIFY | `@model` → `List<UnifiedTrainingRecord>`; table columns updated to match unified schema |

**Confirmed NOT changing:**
- `Data/ApplicationDbContext.cs` — no new entities, no new DbSets
- `Models/TrainingRecord.cs` — no changes needed; queried as-is
- `Models/AssessmentSession.cs` — no changes needed; queried as-is
- No migrations — no schema changes
- No new routes — the `Records()` and `WorkerDetail()` actions keep their existing routes
- `Views/Shared/_Layout.cshtml` — Records nav link already exists; no change needed

---

## Open Questions

1. **AssessmentSession "Completed" display status in the Status column**
   - What we know: Assessment Status field values are "Open", "Upcoming", "Completed". Only Completed sessions appear in the unified view.
   - What's unclear: What label should the Status column show for Assessment rows? The schema has `IsPassed` (bool?) for pass/fail but Status is always "Completed" for displayed rows.
   - Recommendation: Show "Passed" or "Failed" derived from `IsPassed` in the Status column for assessment rows (same value used in the Pass/Fail column). This makes the Status column useful. If user wants "Completed" verbatim, the planner should specify — but "Passed"/"Failed" is more actionable and matches TrainingRecord's `Passed`/`Valid` vocabulary.

2. **WorkerDetail — summary stat cards**
   - What we know: `WorkerDetail.cshtml` currently shows 4 stat cards (Total Training, Completed, Pending, Expiring Soon). These reference `TrainingRecord`-specific fields.
   - What's unclear: Should the stat cards be updated with assessment data, simplified, or removed entirely?
   - Recommendation: Update stat cards to reflect the unified view: "Total Records" (all rows), "Completed" (passed assessments + valid/passed trainings), "Pending" (Wait Certificate trainings only), remove "Expiring Soon" (no lookahead in this phase per decision). This is a reasonable interpretation — planner should confirm.

3. **Records.cshtml — existing filter bar**
   - What we know: Current `Records.cshtml` has search, year filter, and status filter controls that operate on `TrainingRecord.Kategori` and `TrainingRecord.Status` via JavaScript `data-*` attributes.
   - What's unclear: Do these filters survive into the unified view, adapted for the new columns?
   - Recommendation: Keep search (by Title) and year filter (by Date.Year). Remove category/status filter for now — filtering is explicitly Phase 11 scope. Replacing the tab navigation with a single table means the tab-based filter is gone; simple search + year filter on the unified table is Phase 10 scope.

---

## Sources

### Primary (HIGH confidence — direct file reads, 2026-02-18)

- `Controllers/CMPController.cs` — Records() action (lines 612–677), GetPersonalTrainingRecords() (lines 699–709), GetWorkersInSection() (lines 712–805), isHCAccess pattern compared against CDPController
- `Controllers/CDPController.cs` — `isHCAccess` pattern (lines 1025–1027), `isCoacheeView` pattern (lines 47–48, 528–529) — established naming conventions
- `Controllers/HomeController.cs` — Admin SelectedView branching pattern (lines 34–55)
- `Models/TrainingRecord.cs` — fields: Judul, Kategori, Tanggal, Penyelenggara, Status, ValidUntil, CertificateType, IsExpiringSoon
- `Models/AssessmentSession.cs` — fields: Title, Category, Schedule, CompletedAt, Score, IsPassed, Status
- `Models/WorkerTrainingStatus.cs` — existing fields and CompletionPercentage computed property
- `Models/ApplicationUser.cs` — SelectedView field, RoleLevel field
- `Models/UserRoles.cs` — role constants, HasFullAccess(), IsCoachingRole() helpers
- `Data/ApplicationDbContext.cs` — TrainingRecords and AssessmentSessions DbSets confirmed present
- `Views/CMP/Records.cshtml` — full read: tab navigation, @model, existing JS filter logic
- `Views/CMP/RecordsWorkerList.cshtml` — full read: CompletionPercentage column, clickable rows to WorkerDetail
- `Views/CMP/WorkerDetail.cshtml` — full read: @model List<TrainingRecord>, stat cards, table structure
- `.planning/phases/10-unified-training-records/10-CONTEXT.md` — user decisions, locked choices

---

## Metadata

**Confidence breakdown:**
- Touch point inventory: HIGH — all files confirmed by direct read; no inference
- ViewModel design: HIGH — derived directly from field analysis of both source models
- Controller merge pattern: HIGH — standard LINQ; EF Core in-memory merge is established pattern in this project
- Role branch logic: HIGH — isHCAccess/isCoacheeView patterns confirmed by CDPController reads
- Razor rendering patterns: HIGH — Bootstrap badge classes confirmed in use across WorkerDetail.cshtml; em dash is a Unicode literal, no library needed
- Open questions: MEDIUM — require planner judgment but do not block implementation

**Research date:** 2026-02-18
**Valid until:** Valid until any commit touches CMPController.cs, WorkerTrainingStatus.cs, Records.cshtml, RecordsWorkerList.cshtml, or WorkerDetail.cshtml. Re-verify line references before executing.
