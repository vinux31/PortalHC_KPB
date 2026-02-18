# Stack Research

**Domain:** ASP.NET Core 8 MVC — UX Consolidation (v1.2)
**Researched:** 2026-02-18
**Confidence:** HIGH — all patterns verified against existing codebase; no new dependencies required

---

## Context: What This Milestone Is

This is a restructuring milestone, not a greenfield build. The existing stack (ASP.NET Core 8 MVC, EF Core 8, Razor Views, Bootstrap 5.3, ASP.NET Identity, Chart.js, ClosedXML) already handles every requirement. No new NuGet packages are needed.

The three goals are:

1. Merge `AssessmentSession` + `TrainingRecord` into a unified Razor table with role-based filtering
2. Tab-based role visibility — show/hide tabs per server-side role check, no full page reload
3. Remove the Gap Analysis page cleanly — controller action, view, nav links, cross-links

---

## Recommended Stack

### Core Technologies (All Already in Use — No Changes)

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| ASP.NET Core 8 MVC | 8.0.x | Request routing, controller actions, server-side rendering | `Records()` and `Assessment()` actions in `CMPController` are the merge targets. No framework change needed. |
| Razor Views (.cshtml) | ASP.NET Core 8 | Templating for unified table and tab visibility | Tab visibility via `@if (userRole == ...)` blocks is idiomatic Razor and is already the pattern used in this codebase. |
| EF Core 8 | 8.0.0 | Database queries for merging heterogeneous data | Merge pattern: query both `AssessmentSessions` and `TrainingRecords` in one controller action, project to a shared ViewModel, sort in-memory via LINQ. |
| ASP.NET Identity | 8.0.0 | Role-based tab visibility decisions | `UserManager.GetRolesAsync()` is the established pattern (see `CDPController`, `CMPController`). Resolved server-side before the view renders. |
| Bootstrap 5.3 | 5.3.0 (CDN) | Tab UI component (`nav-tabs` + `tab-pane`) | Already loaded in `_Layout.cshtml`. Bootstrap's native tab component handles show/hide without any page reload. No additional JS library needed. |

### Supporting Libraries (Already Present — No New Additions)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ClosedXML | 0.105.0 | Excel export from unified table | Only needed if the merged view adds an Export Excel button — reuse existing Records view pattern |
| Chart.js | CDN | Charts in the merged view if needed | Already used in `CompetencyGap.cshtml` and `DevDashboard`. Reuse existing CDN script tag pattern. |
| jQuery 3.7.1 | CDN | Tab state persistence via `localStorage` | Already loaded in `_Layout.cshtml`. Use only if tab selection needs to survive a page reload. |

### Development Tools (No Change)

| Tool | Purpose | Notes |
|------|---------|-------|
| EF Core Tools | Migrations | Not needed for this milestone — no schema changes. `TrainingRecord` and `AssessmentSession` tables already exist. |
| .NET 8 SDK | Build | No change. |

---

## Installation

No new packages required. All patterns use what is already in `HcPortal.csproj`.

---

## Patterns for Each Goal

### Goal 1: Merging Heterogeneous Data Sources (AssessmentSession + TrainingRecord)

**Pattern: Project both sources to a shared ViewModel in the controller, merge in-memory, sort unified.**

The codebase already does this for `CoacheeProgressRow` in `DevDashboard` and `TrackingItem` in the `Progress` view. Apply the same approach:

**Shared ViewModel:**
```csharp
// New file: Models/CapabilityRowViewModel.cs
public class CapabilityRowViewModel
{
    public DateTime Date { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";   // Common field, different vocabulary
    public string SourceType { get; set; } = ""; // "Assessment" | "Training"
    public string Status { get; set; } = "";
    public string? Score { get; set; }           // Assessment-only
    public string? CertificateUrl { get; set; }  // Training-only
    public DateTime? ValidUntil { get; set; }    // Training-only (cert expiry)
}
```

**Controller action:**
```csharp
// In CMPController — new or updated unified action
public async Task<IActionResult> CapabilityRecords(string? userId = null)
{
    var currentUser = await _userManager.GetUserAsync(User);
    var roles = await _userManager.GetRolesAsync(currentUser!);
    var userRole = roles.FirstOrDefault();

    // Role-based target user resolution (matches DevDashboard pattern)
    string targetUserId = ResolveTargetUserId(currentUser, userRole, userId);

    // Source 1: AssessmentSession
    var sessions = await _context.AssessmentSessions
        .Where(a => a.UserId == targetUserId && a.Status == "Completed")
        .OrderByDescending(a => a.Schedule)
        .ToListAsync();

    // Source 2: TrainingRecord
    var trainings = await _context.TrainingRecords
        .Where(t => t.UserId == targetUserId)
        .OrderByDescending(t => t.Tanggal)
        .ToListAsync();

    // Project both to unified shape, then merge and sort
    var unified = sessions
        .Select(a => new CapabilityRowViewModel
        {
            Date = a.Schedule,
            Title = a.Title,
            Category = a.Category,       // "Assessment OJ", "IHT", etc.
            SourceType = "Assessment",
            Status = a.IsPassed == true ? "Passed" : "Failed",
            Score = a.Score?.ToString()
        })
        .Concat(trainings.Select(t => new CapabilityRowViewModel
        {
            Date = t.Tanggal,
            Title = t.Judul ?? "",
            Category = t.Kategori ?? "",  // "PROTON", "OJT", "MANDATORY"
            SourceType = "Training",
            Status = t.Status ?? "",
            CertificateUrl = t.SertifikatUrl,
            ValidUntil = t.ValidUntil
        }))
        .OrderByDescending(r => r.Date)   // Unified chronological sort
        .ToList();

    ViewBag.UserRole = userRole;
    return View(unified);
}
```

**Why in-memory merge, not SQL UNION:**
- `AssessmentSession` and `TrainingRecord` have incompatible schemas. EF Core does not support UNION across unrelated `DbSet` entities without raw SQL.
- In-memory LINQ `.Concat().OrderBy()` after two separate queries is the established pattern in this codebase and is correct at these data volumes (per-user sets, not org-wide aggregations across all records).
- Avoids raw SQL and keeps EF type safety.

**Role-based target user resolution (matches existing codebase):**
```csharp
// HC/Admin: show user selector dropdown; Coachee: locked to self
if (userRole == UserRoles.HC || userRole == UserRoles.Admin)
{
    // userId from query param, falls back to currentUser.Id if null
    targetUserId = userId ?? currentUser.Id;
}
else if (userRole is UserRoles.Coach or UserRoles.SrSupervisor or UserRoles.SectionHead)
{
    // Scoped to section — Coach can view any coachee in same section
    targetUserId = userId ?? currentUser.Id;
    // Validate userId is in same section before accepting
}
else // Coachee
{
    targetUserId = currentUser.Id; // Always locked to self
}
```

---

### Goal 2: Tab-Based Role Visibility in Razor (No Page Reload)

**Pattern: Bootstrap 5 native `nav-tabs` + server-side `@if` blocks in Razor.**

No AJAX. No JavaScript framework. Tabs switch client-side via Bootstrap's built-in `data-bs-toggle="tab"`. The role check is resolved server-side before render — restricted tabs are never emitted into the HTML response.

**View structure:**
```cshtml
@{
    var userRole = ViewBag.UserRole as string;
    bool isHcOrAdmin = userRole == "HC" || userRole == "Admin";
    bool isCoachOrAbove = userRole is "Coach" or "Sr Supervisor" or "Section Head";
}

<ul class="nav nav-tabs mb-3" id="capabilityTabs" role="tablist">

    <!-- Tab visible to all roles -->
    <li class="nav-item" role="presentation">
        <button class="nav-link active" id="training-tab"
                data-bs-toggle="tab" data-bs-target="#training-pane"
                type="button" role="tab" aria-selected="true">
            Training Records
        </button>
    </li>

    <!-- Tab visible to all roles -->
    <li class="nav-item" role="presentation">
        <button class="nav-link" id="assessment-tab"
                data-bs-toggle="tab" data-bs-target="#assessment-pane"
                type="button" role="tab" aria-selected="false">
            Assessments
        </button>
    </li>

    <!-- Tab restricted to HC and Admin only -->
    @if (isHcOrAdmin)
    {
        <li class="nav-item" role="presentation">
            <button class="nav-link" id="org-tab"
                    data-bs-toggle="tab" data-bs-target="#org-pane"
                    type="button" role="tab" aria-selected="false">
                Org Summary
            </button>
        </li>
    }

</ul>

<div class="tab-content" id="capabilityTabContent">

    <div class="tab-pane fade show active" id="training-pane" role="tabpanel">
        @* Training records table *@
    </div>

    <div class="tab-pane fade" id="assessment-pane" role="tabpanel">
        @* Assessment table *@
    </div>

    @if (isHcOrAdmin)
    {
        <div class="tab-pane fade" id="org-pane" role="tabpanel">
            @* HC-only org content — not in DOM for other roles *@
        </div>
    }

</div>
```

**Why this approach:**
- Bootstrap 5.3 is already loaded in `_Layout.cshtml`. `data-bs-toggle="tab"` works with zero additional JS.
- The existing codebase consistently uses `@if (userRole == ...)` for role gating in Razor (see `Progress.cshtml`, `_Layout.cshtml` nav items, `Coaching.cshtml`). This matches the established pattern — do not deviate.
- Do NOT use `display:none` CSS toggling for role-gated tabs. Hidden CSS content is still sent in the HTML response and is visible via browser DevTools. Use server-side `@if` so restricted content is never in the DOM for unauthorized users.

**Optional: Tab state persistence across page reloads (use existing jQuery):**
```javascript
// Save active tab on switch
document.querySelectorAll('[data-bs-toggle="tab"]').forEach(tab => {
    tab.addEventListener('shown.bs.tab', e => {
        localStorage.setItem('capabilityActiveTab', e.target.id);
    });
});

// Restore active tab on load
const saved = localStorage.getItem('capabilityActiveTab');
if (saved) {
    const tab = document.getElementById(saved);
    if (tab) bootstrap.Tab.getOrCreateInstance(tab).show();
}
```
This is ~10 lines in `@section Scripts`. No library needed — jQuery is already loaded.

---

### Goal 3: Removing the Gap Analysis Page Cleanly

**Surface area confirmed by codebase audit:**

| Location | What to Remove |
|----------|---------------|
| `Controllers/CMPController.cs` (line ~1533) | `CompetencyGap(string? userId)` action method |
| `Views/CMP/CompetencyGap.cshtml` | Delete the file |
| `Views/CMP/Index.cshtml` (line 72) | Remove the "Gap Analysis" card block (the entire `col-12 col-md-6 col-lg-4` div containing the card) |
| `Views/CMP/CpdpProgress.cshtml` (line 19) | Remove the `<a href="CompetencyGap">Gap Analysis</a>` sibling nav link |
| `Views/Shared/_Layout.cshtml` | No direct link to CompetencyGap — already absent from top nav (confirmed) |
| `Models/Competency/CompetencyGapViewModel.cs` | Delete or retain — only referenced by the CompetencyGap action and view |

**Safe removal order:**
1. Remove the `CompetencyGap` controller action first. This immediately breaks compilation or produces runtime 404s for any remaining `Url.Action("CompetencyGap", ...)` calls — surfaces all cross-links.
2. Delete `Views/CMP/CompetencyGap.cshtml`.
3. Remove `Url.Action("CompetencyGap", ...)` reference from `Views/CMP/CpdpProgress.cshtml` (line 19).
4. Remove the Gap Analysis card from `Views/CMP/Index.cshtml` (line 72 — the entire card `div`).
5. Delete `Models/Competency/CompetencyGapViewModel.cs` after verifying no other references.

**Pre-removal grep to find all references:**
```
Search pattern: CompetencyGap
File scope: **/*.cshtml, **/*.cs
```

Current confirmed references (from codebase audit — complete list):
- `CMPController.cs` — the action method itself
- `Views/CMP/CompetencyGap.cshtml` — the view file
- `Views/CMP/Index.cshtml` — card link (line 72)
- `Views/CMP/CpdpProgress.cshtml` — sibling tab link (line 19)
- `Models/Competency/CompetencyGapViewModel.cs` — ViewModel class used only by this feature

**What NOT to do:**
- Do not add a redirect from `/CMP/CompetencyGap` to another URL. A 404 is semantically correct for removed content. A redirect implies the content moved; it did not.
- Do not leave the `CompetencyGapViewModel.cs` file orphaned — delete it to avoid confusion about dead code.

---

## Alternatives Considered

| Recommended | Alternative | Why Not |
|-------------|-------------|---------|
| In-memory LINQ `.Concat()` for data merge | `FromSqlRaw` UNION query | More complex, bypasses EF type safety, harder to maintain. Not justified at per-user data volumes. |
| Bootstrap 5 `nav-tabs` for tab UI | Custom CSS toggle + JavaScript | Duplicates functionality already loaded in `_Layout.cshtml`. Bootstrap tabs have accessible ARIA roles built in. |
| Server-side `@if` for role-gated tabs | `display:none` CSS toggling | Security flaw — hidden content is still in the HTML response and visible via browser DevTools. |
| Server-side `@if (userRole == ...)` in Razor | `[Authorize(Policy=...)]` on tab content | Policy attributes apply to controller actions (full routes), not Razor blocks within a single action. Wrong abstraction level. |
| Hard 404 on CompetencyGap removal | 301 redirect to CpdpProgress | Semantically wrong — the content was removed, not moved. |
| Separate API endpoint per tab | Render all tab content on initial page load | Adds network round-trips per tab switch. Unnecessary for this data size. Server-side render on load is the correct pattern for MVC. |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| Any new NuGet package | No new dependency is warranted — Bootstrap, jQuery, EF Core already cover all requirements | Existing stack |
| Blazor or any SPA component | Mixing Blazor into an MVC app creates two rendering pipelines; tab visibility requires zero JS framework | Bootstrap `nav-tabs` with `data-bs-toggle="tab"` |
| `display:none` for role-gated tab content | Sends restricted HTML to the browser; inspectable via DevTools | Server-side `@if` blocks in Razor |
| Raw SQL / `FromSqlRaw` for data merge | Schema mismatch between `AssessmentSession` and `TrainingRecord`; bypasses EF type safety | In-memory LINQ `.Concat().OrderByDescending()` after two separate queries |
| AJAX partial views per tab | Adds network latency per tab switch; unnecessary for this data size | Render all tab content server-side in one response |
| DataTables.js | Adds an external dependency for features that Bootstrap + LINQ pagination already cover | Server-side pagination (existing `CMPController.Assessment` pattern) |

---

## Stack Patterns by Variant

**If the unified table has more than 50 rows per user:**
- Add server-side pagination using the existing `page`/`pageSize` pattern from `CMPController.Assessment` (lines 78-80)
- Do not add client-side pagination libraries

**If tab state must persist across page reloads:**
- Use `localStorage` with the existing jQuery already in `_Layout.cshtml`
- ~10 lines of script in `@section Scripts` — no library needed

**If HC role needs to select any user for the unified view:**
- Apply the `userId` query parameter + dropdown selector pattern from `CMPController.Assessment` (`view == "manage"`) and `CMPController.CompetencyGap` (before removal)
- Render the user-selector dropdown only when `isHcOrAdmin == true`

**If the merged view needs column filtering by SourceType (Assessment vs Training):**
- Pass filter as a query parameter (`?sourceType=Assessment`)
- Apply `.Where(r => r.SourceType == sourceType)` before building the ViewModel
- Render filter buttons as regular `<a>` links (not JavaScript) — consistent with existing filter patterns in `CMPController.Records`

---

## Version Compatibility

| Package | Version in Use | Notes |
|---------|---------------|-------|
| ASP.NET Core | net8.0 TFM | All patterns target .NET 8 APIs |
| EF Core | 8.0.0 | `ToListAsync()`, `FirstOrDefaultAsync()`, `Concat()` — all in use throughout codebase |
| Bootstrap | 5.3.0 (CDN) | `data-bs-toggle="tab"` requires Bootstrap 5.x; confirmed loaded in `_Layout.cshtml` |
| jQuery | 3.7.1 (CDN) | Available for `localStorage` tab persistence if needed; already in `_Layout.cshtml` |
| ClosedXML | 0.105.0 | No change; only relevant if Export Excel is added to unified view |

---

## Sources

- Direct codebase audit (file reads, 2026-02-18) — HIGH confidence:
  - `Controllers/CMPController.cs` — `Records()`, `Assessment()`, `CompetencyGap()` actions
  - `Controllers/CDPController.cs` — `DevDashboard()`, `Coaching()` role patterns
  - `Views/CMP/CompetencyGap.cshtml` — cross-links confirmed
  - `Views/CMP/CpdpProgress.cshtml` — cross-link at line 19 confirmed
  - `Views/CMP/Index.cshtml` — card link at line 72 confirmed
  - `Views/CMP/Records.cshtml` — TrainingRecord table pattern
  - `Views/CDP/Progress.cshtml` — Razor role-gating pattern via `@if`
  - `Views/Shared/_Layout.cshtml` — Bootstrap 5.3 and jQuery CDN confirmed, no CompetencyGap nav link
  - `Models/AssessmentSession.cs` — field inventory for ViewModel projection
  - `Models/TrainingRecord.cs` — field inventory for ViewModel projection
  - `Models/ApplicationUser.cs` — `RoleLevel`, `SelectedView` fields
  - `Models/UserRoles.cs` — role constants
  - `HcPortal.csproj` — package versions

- Bootstrap 5 tab component documentation (`data-bs-toggle="tab"`) — HIGH confidence (stable API, no version-specific gotchas in 5.3.x)
- ASP.NET Core 8 Razor `@if` role gating — HIGH confidence (established pattern, already in use in multiple views in this codebase)

---

*Stack research for: Portal HC KPB v1.2 UX Consolidation*
*Researched: 2026-02-18*
