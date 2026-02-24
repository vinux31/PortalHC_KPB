# Phase 40: Training Records History Tab - Research

**Researched:** 2026-02-24
**Domain:** ASP.NET Core MVC — ViewModel extension, Bootstrap tab pattern, cross-worker unified history query
**Confidence:** HIGH

---

## Summary

Phase 40 adds a second Bootstrap tab ("History") to the existing `RecordsWorkerList.cshtml` page. The tab shows a combined chronological list of all workers' training events — both `TrainingRecord` rows and completed `AssessmentSession` rows — in one flat table sorted by tanggal mulai descending.

The core infrastructure is already present. `UnifiedTrainingRecord` is the exact flat projection needed for each history row. `GetUnifiedRecords(userId)` already performs the merge-and-sort for a single user; Phase 40 generalises this to all users by removing the `userId` filter and adding `.Include(a => a.User)` / `.Include(t => t.User)` to project worker names. A new `AllWorkersHistoryViewModel` wraps the existing `List<WorkerTrainingStatus>` worker list plus the new `List<AllWorkersHistoryRow>` history list so that the view receives both from a single `Records` GET action.

Tab persistence in this codebase uses the `?activeTab=history` URL query-param pattern (established in CDP Dashboard). The tab strip is toggled client-side via `new bootstrap.Tab(el).show()` on page load when the param is detected.

**Primary recommendation:** Extend the existing `Records` GET action to also query all-workers history and pass it via a wrapper ViewModel; extend `RecordsWorkerList.cshtml` with a second Bootstrap tab pane; do not create a new controller action or a new route.

---

## Standard Stack

### Core (already in project — no new installs)
| Component | Version | Purpose | Why Standard |
|-----------|---------|---------|--------------|
| `UnifiedTrainingRecord` model | Existing | Flat row projection for each history entry | Already used in per-worker Records view |
| Bootstrap 5 tabs | 5.x (in layout) | Tab strip + pane rendering | Already used in Assessment.cshtml and CDP Dashboard.cshtml |
| EF Core `.Include()` + LINQ in-memory | Existing | Cross-worker data merge | Pattern used in GetWorkersInSection |
| `@model` wrapper ViewModel | New (to create) | Pass both worker list + history to single view | Pattern used in CDPDashboardViewModel |

**Installation:** No new packages required.

---

## Architecture Patterns

### Recommended Project Structure
```
Models/
├── AllWorkersHistoryViewModel.cs   # NEW — wraps worker list + history rows
├── AllWorkersHistoryRow.cs         # NEW — extends UnifiedTrainingRecord with WorkerName/NIP
├── UnifiedTrainingRecord.cs        # Existing — reuse as-is (no changes)
├── WorkerTrainingStatus.cs         # Existing — unchanged
Controllers/
└── CMPController.cs                # Extend Records() GET only
Views/CMP/
└── RecordsWorkerList.cshtml        # Extend with second tab pane
```

### Pattern 1: Wrapper ViewModel (from CDPDashboardViewModel)

**What:** A single ViewModel class holds all sub-models the view needs. The controller populates the appropriate sub-models and passes the wrapper to `View()`.

**When to use:** When a single page has multiple content areas (tabs) that require independent data sets.

**Example (CDPDashboardViewModel pattern applied here):**
```csharp
// Source: Models/CDPDashboardViewModel.cs (existing pattern)
public class RecordsWorkerListViewModel
{
    // Tab 1: existing worker list data
    public List<WorkerTrainingStatus> Workers { get; set; } = new();

    // Tab 2: all-workers history
    public List<AllWorkersHistoryRow> History { get; set; } = new();

    // Filter state passthrough (for ViewBag-free binding in view)
    public string? SelectedSection { get; set; }
    public string? SelectedUnit { get; set; }
    public string? SelectedCategory { get; set; }
    public string? SearchTerm { get; set; }
    public bool IsInitialState { get; set; }
    public bool IsFilterMode { get; set; }
}
```

### Pattern 2: AllWorkersHistoryRow — extends UnifiedTrainingRecord fields

**What:** A flat row with all `UnifiedTrainingRecord` fields plus worker identity fields. The history table needs a "Nama Pekerja" column not present in the per-user `UnifiedTrainingRecord`.

**Key insight:** Do NOT modify `UnifiedTrainingRecord` itself — it is used in the per-worker `Records.cshtml` view without a worker name column. Create a sibling class.

```csharp
// Source: codebase analysis — UnifiedTrainingRecord.cs + AssessmentSession.cs + TrainingRecord.cs
public class AllWorkersHistoryRow
{
    // Worker identity
    public string WorkerName { get; set; } = "";
    public string? WorkerNIP { get; set; }

    // Shared fields (mirrors UnifiedTrainingRecord)
    public DateTime Date { get; set; }          // sort key
    public string RecordType { get; set; } = ""; // "Assessment Online" | "Training Manual"
    public string Title { get; set; } = "";

    // Assessment-only (null for Training Manual rows)
    public int? Score { get; set; }
    public bool? IsPassed { get; set; }

    // Training Manual-only (null for Assessment Online rows)
    public string? Penyelenggara { get; set; }

    // Status: "Passed"/"Failed" for assessments, TrainingRecord.Status for manual
    public string? Status { get; set; }

    // Sort tie-break (Assessment Online = 0, Training Manual = 1)
    public int SortPriority { get; set; }
}
```

### Pattern 3: All-Workers History Query (in CMPController)

**What:** Two batch queries (no per-user loops), in-memory merge, single sort — mirrors the exact shape of `GetUnifiedRecords` but without the `userId` filter and with `.Include(a => a.User)`.

**When to use:** When building a cross-worker flat history list. This is a one-time load on tab render; no pagination required per phase spec (but planner may choose to add simple client-side JS pagination matching the existing workers table).

```csharp
// Source: CMPController.cs GetUnifiedRecords (lines 2412-2460) — generalised pattern
private async Task<List<AllWorkersHistoryRow>> GetAllWorkersHistory()
{
    // Query 1: ALL completed assessments across all users, with User nav property
    var assessments = await _context.AssessmentSessions
        .Include(a => a.User)
        .Where(a => a.Status == "Completed")
        .ToListAsync();

    // Query 2: ALL training records across all users, with User nav property
    var trainings = await _context.TrainingRecords
        .Include(t => t.User)
        .ToListAsync();

    var rows = new List<AllWorkersHistoryRow>();

    rows.AddRange(assessments.Select(a => new AllWorkersHistoryRow
    {
        WorkerName = a.User?.FullName ?? "Unknown",
        WorkerNIP  = a.User?.NIP,
        Date       = a.CompletedAt ?? a.Schedule,
        RecordType = "Assessment Online",
        Title      = a.Title,
        Score      = a.Score,
        IsPassed   = a.IsPassed,
        Status     = a.IsPassed == true ? "Passed" : "Failed",
        SortPriority = 0
    }));

    rows.AddRange(trainings.Select(t => new AllWorkersHistoryRow
    {
        WorkerName   = t.User?.FullName ?? "Unknown",
        WorkerNIP    = t.User?.NIP,
        Date         = t.TanggalMulai ?? t.Tanggal,  // prefer TanggalMulai per phase spec
        RecordType   = "Training Manual",
        Title        = t.Judul ?? "",
        Penyelenggara = t.Penyelenggara,
        Status       = t.Status,
        SortPriority = 1
    }));

    return rows
        .OrderByDescending(r => r.Date)
        .ThenBy(r => r.SortPriority)
        .ToList();
}
```

**Date field note:** Phase spec says "tanggal mulai" for sorting. For `TrainingRecord`, that is `TanggalMulai` (nullable, added in v1.6). Fall back to `Tanggal` when `TanggalMulai` is null. For `AssessmentSession`, that is `CompletedAt ?? Schedule` (same as existing `GetUnifiedRecords`).

### Pattern 4: Bootstrap Tab with URL-param Persistence

**What:** The second tab is activated on page load when `?activeTab=history` is in the URL. Forms inside the history tab append `activeTab=history` as a hidden input so filter submissions return to the history tab.

**Established pattern (CDP Dashboard.cshtml lines 56-72):**
```javascript
// Source: Views/CDP/Dashboard.cshtml — existing tab activation pattern
(function () {
    var params = new URLSearchParams(window.location.search);
    if (params.get('activeTab') === 'history') {
        var el = document.getElementById('history-tab');
        if (el) { new bootstrap.Tab(el).show(); }
    }
})();
```

**Tab strip HTML (Assessment.cshtml pattern):**
```html
<!-- Source: Views/CMP/Assessment.cshtml lines 91-106 -->
<ul class="nav nav-tabs mb-4" id="recordsTabs" role="tablist">
    <li class="nav-item" role="presentation">
        <button class="nav-link active" id="workers-tab" data-bs-toggle="tab"
                data-bs-target="#workers-pane" type="button" role="tab">
            <i class="bi bi-people me-1"></i>Daftar Pekerja
        </button>
    </li>
    <li class="nav-item" role="presentation">
        <button class="nav-link" id="history-tab" data-bs-toggle="tab"
                data-bs-target="#history-pane" type="button" role="tab">
            <i class="bi bi-clock-history me-1"></i>History
        </button>
    </li>
</ul>
<div class="tab-content">
    <div class="tab-pane fade show active" id="workers-pane" role="tabpanel">
        <!-- existing worker list markup, unchanged -->
    </div>
    <div class="tab-pane fade" id="history-pane" role="tabpanel">
        <!-- new history table -->
    </div>
</div>
```

### Anti-Patterns to Avoid

- **New controller action for History:** Do not create a separate `HistoryTab` action — the phase spec and prior decisions explicitly place this inside the existing `Records` GET. A separate route breaks the "second tab on RecordsWorkerList" requirement.
- **Modifying UnifiedTrainingRecord:** Do not add `WorkerName`/`WorkerNIP` to `UnifiedTrainingRecord` — it is also used by `Records.cshtml` (per-worker view) which does not display worker identity.
- **Passing history via ViewBag:** ViewBag is untyped. The established pattern for complex multi-data views is a typed wrapper ViewModel (CDPDashboardViewModel precedent). Use a typed ViewModel.
- **N+1 queries:** Do NOT loop over workers calling `GetUnifiedRecords` per user. Use two batch queries with `.Include(a => a.User)` as shown above.
- **Eager history load when tab is not shown:** Because both tabs are part of the same server-rendered page, the history list is always loaded on `Records` GET. This is acceptable; the history data set is bounded by the number of training records + completed sessions system-wide. If performance becomes a concern in future, the planner may choose to note it as a future optimisation, but for Phase 40 scope it is fine.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Tab strip with JS activation | Custom tab toggle logic | Bootstrap 5 `data-bs-toggle="tab"` + `new bootstrap.Tab(el).show()` | Already used in 3 other views; no extra JS needed |
| Cross-worker flat list | New ORM pattern | Two `.Include(x => x.User)` queries + in-memory merge | Exact shape of existing `GetUnifiedRecords`, just without userId filter |
| Row type badge | Custom CSS classes | Existing `badge rounded-pill bg-primary` / `bg-success` classes from Records.cshtml | Copy-paste from Records.cshtml lines 126-133 |
| Tab-state persistence | Cookie / localStorage | `?activeTab=history` URL param | Established project pattern in CDP Dashboard |

---

## Common Pitfalls

### Pitfall 1: TanggalMulai Nullable — Fallback Required

**What goes wrong:** `AllWorkersHistoryRow.Date` is set to `t.TanggalMulai` for training rows, but `TanggalMulai` is nullable (added in v1.6). Sorting by a nullable field without a fallback causes null rows to sort unpredictably.

**Why it happens:** `TanggalMulai` was added later than `Tanggal`; many existing `TrainingRecord` rows may have a null `TanggalMulai` and a populated `Tanggal`.

**How to avoid:** Always use `t.TanggalMulai ?? t.Tanggal` when projecting the `Date` sort key for manual training rows. This matches the phase spec ("tanggal mulai") while remaining safe for legacy records.

**Warning signs:** History table shows training rows at the very top or bottom of the sort order in a way that ignores real dates.

### Pitfall 2: ViewModel Change Breaks Existing RecordsWorkerList View

**What goes wrong:** The view currently has `@model List<HcPortal.Models.WorkerTrainingStatus>`. Switching to a wrapper ViewModel requires updating every reference to `Model` in the existing worker-list markup (e.g. `Model.Count`, `@foreach (var worker in Model)`).

**Why it happens:** The view references `Model` as a list directly; after ViewModel switch it becomes `Model.Workers`.

**How to avoid:** When switching the `@model` directive, search the entire cshtml for bare `Model` references and update them to `Model.Workers`. The phase has a dedicated verification checkpoint for this.

**Warning signs:** Compiler error `'RecordsWorkerListViewModel' does not contain a definition for 'Count'`.

### Pitfall 3: Records GET Action Still Passes `List<WorkerTrainingStatus>` After ViewModel Switch

**What goes wrong:** The controller `return View("RecordsWorkerList", workers)` still passes a bare list after the ViewModel is introduced, causing a runtime model-binding error.

**Why it happens:** Two call sites for `View("RecordsWorkerList", ...)` exist: the main worker-list path and the initial-state empty-list path (line 2147). Both must be updated.

**How to avoid:** Update BOTH return points in `Records()` to pass the new wrapper ViewModel. Search for `"RecordsWorkerList"` in CMPController.cs — there are exactly two occurrences.

**Warning signs:** `InvalidOperationException: The model item passed is of type 'List<WorkerTrainingStatus>', but this view requires 'RecordsWorkerListViewModel'`.

### Pitfall 4: AssessmentSession Status Filter

**What goes wrong:** The phase spec says "completed AssessmentSessions (IsPassed is not null)". The existing `GetUnifiedRecords` filters by `a.Status == "Completed"`. These are not identical — some sessions may have `IsPassed != null` but `Status != "Completed"`.

**Why it happens:** Ambiguity between the spec language and the existing code pattern.

**How to avoid:** Use `a.Status == "Completed"` — this is the established filter in `GetUnifiedRecords` and is authoritative for what "completed" means in this codebase. The spec's "IsPassed is not null" is a description of the business intent, not a literal filter. Consistency with the existing per-worker history view is more important than literal spec interpretation.

---

## Code Examples

Verified patterns from codebase:

### Records.cshtml — type badge rendering (copy to history pane)
```csharp
// Source: Views/CMP/Records.cshtml lines 126-133
@if (item.RecordType == "Assessment Online")
{
    <span class="badge rounded-pill bg-primary">Assessment Online</span>
}
else
{
    <span class="badge rounded-pill bg-success">Training Manual</span>
}
```

### Assessment Online — score + pass/fail columns (copy to history pane)
```csharp
// Source: Views/CMP/Records.cshtml lines 136-148
<td class="p-3 text-center">
    @if (item.Score.HasValue) { <span>@item.Score</span> } else { <span class="text-muted">—</span> }
</td>
<td class="p-3 text-center">
    @if (item.IsPassed.HasValue)
    {
        if (item.IsPassed == true) { <span class="badge bg-success">Pass</span> }
        else { <span class="badge bg-danger">Fail</span> }
    }
    else { <span class="text-muted">—</span> }
</td>
```

### Training Manual — penyelenggara column
```csharp
// Source: Views/CMP/Records.cshtml line 149
<td class="p-3">@(item.Penyelenggara ?? "—")</td>
```

### Tab activation on page load (URL param method)
```javascript
// Source: Views/CDP/Dashboard.cshtml lines 56-72 (adapted)
(function () {
    var params = new URLSearchParams(window.location.search);
    if (params.get('activeTab') === 'history') {
        var el = document.getElementById('history-tab');
        if (el) { new bootstrap.Tab(el).show(); }
    }
})();
```

### Controller: Records() return with wrapper ViewModel
```csharp
// Pattern: both return points in Records() must use the wrapper
// Initial state (no filter applied):
return View("RecordsWorkerList", new RecordsWorkerListViewModel
{
    Workers = new List<WorkerTrainingStatus>(),
    History = await GetAllWorkersHistory(),
    IsInitialState = true,
    // ... other filter state
});

// Filtered state:
return View("RecordsWorkerList", new RecordsWorkerListViewModel
{
    Workers = await GetWorkersInSection(section, unit, category, search, statusFilter),
    History = await GetAllWorkersHistory(),
    IsInitialState = false,
    // ... other filter state
});
```

---

## State of the Art

| Old Approach | Current Approach | Impact for Phase 40 |
|--------------|------------------|---------------------|
| Per-user unified records only | Per-user via `GetUnifiedRecords(userId)` | Phase 40 generalises this to all users |
| ViewBag for filter state | ViewBag still used in Records() | Planner may keep ViewBag or move to ViewModel; both work |
| No tabs on RecordsWorkerList | New Bootstrap tab strip | Established tab pattern already in 3 other views |

---

## Open Questions

1. **History tab: load always vs. load on demand**
   - What we know: History is loaded on every `Records` GET once the ViewModel is introduced, regardless of which tab is active.
   - What's unclear: The total row count for the history tab may be large if there are many workers and many records.
   - Recommendation: For Phase 40 scope, always load — the data set is bounded by the number of workers (tens, not millions). If performance becomes a concern, it is an optimisation for a future phase.

2. **Client-side search/filter on the History tab**
   - What we know: The phase spec does not mention filtering on the History tab; it only requires confirmation of worker activity without navigating to individual worker pages.
   - What's unclear: Whether a basic name search on the history table is expected.
   - Recommendation: Planner should include a simple JS `input[type=text]` filter (matching the existing Records.cshtml `searchInput` pattern) for usability, but this is not a hard requirement from the spec.

3. **Date column label in history table: "Tanggal" vs. "Tanggal Mulai"**
   - What we know: Phase spec says "tanggal mulai" is the sort key. For assessments, this maps to `CompletedAt ?? Schedule`. For trainings, `TanggalMulai ?? Tanggal`.
   - What's unclear: Column header label in Indonesian.
   - Recommendation: Use "Tanggal Mulai" as the column header; render `Date` field from the ViewModel row.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CMPController.cs` — `Records()` action (lines 2099-2156), `GetUnifiedRecords()` (lines 2412-2460), `GetWorkersInSection()` (lines 2462-2580)
- `Models/UnifiedTrainingRecord.cs` — full field inventory
- `Models/TrainingRecord.cs` — field inventory including `TanggalMulai`, `Penyelenggara`, `UserId`
- `Models/AssessmentSession.cs` — field inventory including `Status`, `IsPassed`, `CompletedAt`, `UserId`
- `Models/WorkerTrainingStatus.cs` — existing worker list row shape
- `Models/CDPDashboardViewModel.cs` — wrapper ViewModel pattern (canonical precedent)
- `Views/CMP/RecordsWorkerList.cshtml` — full current view (502 lines); no existing tabs
- `Views/CMP/Records.cshtml` — unified record rendering: badge, score, pass/fail, penyelenggara columns
- `Views/CDP/Dashboard.cshtml` — tab strip + URL-param persistence pattern
- `Views/CMP/Assessment.cshtml` — two-tab HC view pattern

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all components are existing, no new libraries
- Architecture: HIGH — verified from actual controller and view code
- Pitfalls: HIGH — identified from direct code inspection (nullable TanggalMulai, two return points, model binding)
- Tab pattern: HIGH — three existing examples in codebase

**Research date:** 2026-02-24
**Valid until:** 2026-03-31 (stable codebase, no external dependencies)
