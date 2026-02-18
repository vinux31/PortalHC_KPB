# Phase 11: Assessment Page Role Filter - Research

**Researched:** 2026-02-18
**Domain:** ASP.NET Core MVC — role-gated controller filtering + Razor tab restructure on the existing Assessment page
**Confidence:** HIGH — all findings from direct file reads of the actual codebase; no inference or external sources required

---

## Summary

Phase 11 is a pure behavior change on two existing files: `CMPController.Assessment()` and `Views/CMP/Assessment.cshtml`. No new models, no migrations, no new routes. The work splits into two plans that match the two files.

The controller action (`CMPController.Assessment()`) already has a `view` parameter that toggles between "personal" (worker) and "manage" (HC/Admin) branches. The personal branch today shows ALL statuses including Completed. The manage branch today shows ALL assessments with no status filter. Both branches need surgery:

- **Worker branch** (personal): add `.Where(a.Status == "Open" || a.Status == "Upcoming")` to exclude Completed from the query. This eliminates Completed cards from the controller result rather than hiding them in JavaScript.
- **HC/Admin branch**: the current "manage" view conflates two distinct concerns — CRUD management and system-wide monitoring. These need to become two distinct `view` values (e.g., `"manage"` for CRUD, `"monitor"` for read-only monitoring), or two tabs within the same page load driven by separate data sets passed via ViewBag.

The view (`Assessment.cshtml`) currently uses client-side JavaScript tab filtering: all cards are rendered, and JS hides/shows by `data-status`. After the controller change, workers will no longer receive Completed cards, so the Completed tab in the view becomes dead weight — remove it entirely for workers and add the Training Records callout in its place. For HC/Admin, replace the current single-card grid with a proper Bootstrap tab structure (Management tab and Monitoring tab).

The Admin `SelectedView` complexity from Phase 8 applies here: the gate must use `isHCAccess` (the established pattern: `userRole == UserRoles.HC || (userRole == UserRoles.Admin && user.SelectedView == "HC")`) not just `userRole == "HC"`. The five `SelectedView` values Admin can hold must all route correctly.

**Primary recommendation:** Filter Completed from the worker query at the controller layer, add a Training Records link in the view, restructure HC/Admin assessment page into Management + Monitoring tabs using two ViewBag data sets, using the `isHCAccess` pattern for all role gates.

---

## Prior Decisions (From Codebase History)

These decisions are locked by prior phases and must be honored:

| Decision | Source | Implication for Phase 11 |
|----------|--------|--------------------------|
| `isHCAccess` named bool pattern for HC gates | Phase 8 research | Use `bool isHCAccess = userRole == UserRoles.HC \|\| (userRole == UserRoles.Admin && user.SelectedView == "HC")` |
| Admin SelectedView has five values: HC, Atasan, Coach, Coachee, Admin | Phase 8 | Admin in any SelectedView must hit the correct branch on the Assessment page |
| Training Records is at `/CMP/Records` | Phase 10 | Callout link in worker view points to `Url.Action("Records", "CMP")` |
| Phase 10 completed — Completed assessments live in /CMP/Records | Phase 10 | Justifies removing Completed from the worker Assessment view |

---

## Standard Stack

### Core (already in place — no new dependencies)

| Component | Version | Purpose | Notes |
|-----------|---------|---------|-------|
| ASP.NET Core MVC | Current (net8+) | Controller + Razor | Existing project |
| Entity Framework Core | Current | DB query filtering | `.Where()` for status filter |
| Bootstrap 5.3 | CDN | Tab structure (`nav-tabs`, `tab-content`) | Already in `_Layout.cshtml` |
| Bootstrap Icons 1.10 | CDN | Icons in tabs and callout | Already loaded |
| jQuery | CDN | AJAX (token verification, delete) | Already used in Assessment.cshtml |

**No new packages required.** The entire phase is controller logic + Razor markup changes.

---

## Architecture Patterns

### How the Current Assessment Action Works

```
GET /CMP/Assessment?view=personal&search=X&page=1
  -> CMPController.Assessment()
  -> Checks view param ("personal" or "manage")
  -> Redirects worker if view == "manage"
  -> Queries AssessmentSessions filtered by userId (personal) or unfiltered (manage)
  -> Returns View(exams) — flat List<AssessmentSession>
```

ViewBag values currently set in `Assessment()`:
- `ViewBag.ViewMode` — `"personal"` or `"manage"`
- `ViewBag.UserRole` — first role string
- `ViewBag.CanManage` — bool: `(userRole == UserRoles.Admin || userRole == "HC")`
- `ViewBag.SearchTerm`, `ViewBag.CurrentPage`, `ViewBag.TotalPages`, `ViewBag.TotalCount`, `ViewBag.PageSize`

### Recommended Architecture for Phase 11

#### Plan 01 — Controller Changes

**Worker path (current `view == "personal"`):**

Add status filter to exclude Completed before query executes:

```csharp
// Source: CMPController.cs Assessment() — worker branch
// After userId filter, before search filter
if (!isHCAccess)
{
    // Workers see only actionable assessments
    query = query.Where(a => a.Status == "Open" || a.Status == "Upcoming");
}
```

**HC/Admin path — two data sets via ViewBag:**

The current single "manage" view needs to split into two concepts without changing the route signature (keep `view` param to avoid breaking existing redirects from EditAssessment/DeleteAssessment that do `RedirectToAction("Assessment", new { view = "manage" })`).

Option A (recommended): Keep `view = "manage"` for the page entry, but load BOTH data sets inside the action and pass both via ViewBag. The view renders two tabs from those ViewBag lists:

```csharp
// Management tab data: ALL assessments (CRUD operations)
var managementData = await _context.AssessmentSessions
    .Include(a => a.User)
    .OrderByDescending(a => a.Schedule)
    .ToListAsync();

// Monitoring tab data: Open + Upcoming across all users (system health view)
var monitorData = await _context.AssessmentSessions
    .Include(a => a.User)
    .Where(a => a.Status == "Open" || a.Status == "Upcoming")
    .OrderBy(a => a.Schedule)
    .ToListAsync();

ViewBag.ManagementData = managementData;
ViewBag.MonitorData = monitorData;
```

This avoids a new `view` value, so existing redirects in `EditAssessment` and `DeleteAssessment` (which redirect to `view = "manage"`) continue to land on the Management tab without changes.

**isHCAccess pattern (mandatory for all gates):**

```csharp
// Source: Phase 8 established pattern (CDPController.Deliverable pattern)
bool isHCAccess = userRole == UserRoles.HC ||
                  (userRole == UserRoles.Admin && user.SelectedView == "HC");
```

The Admin `SelectedView` check: Admin in HC, Atasan, Coach, or Admin view should all see the HC/Admin page (full management access). Only Admin in `SelectedView == "Coachee"` might plausibly be treated as a worker — but per the prior Phase 10 decision, Admin always gets highest-access view. Simplest safe rule: `isHCAccess = userRole == UserRoles.HC || userRole == UserRoles.Admin`.

**Explicit Admin SelectedView routing for Assessment page:**

Looking at existing controller patterns from Phase 8 research:
- Admin in `SelectedView == "HC"`, `"Admin"` — HC/Admin Assessment page (management + monitoring tabs)
- Admin in `SelectedView == "Coachee"`, `"Coach"`, `"Atasan"` — current behavior is redirection to worker view. But the five `SelectedView` values must be manually verified on every modified action (prior decision). The simplest correct rule: `isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC` — Admin always gets management view regardless of SelectedView. This is consistent with the Phase 10 decision ("Admin in all SelectedView states routes to elevated access").

#### Plan 02 — View Changes

**Worker view restructure:**

Remove the Completed tab entirely. Replace with a callout card:

```html
<!-- Source: Bootstrap 5 alert component — standard pattern -->
<div class="alert alert-info d-flex align-items-center" role="alert">
    <i class="bi bi-clock-history me-2"></i>
    <div>
        Looking for completed assessments?
        <a href="@Url.Action("Records", "CMP")" class="alert-link">View your Training Records</a>
    </div>
</div>
```

Tab structure for workers (2 tabs only):
- Open (active by default, JS filter `data-status="open"`)
- Upcoming (JS filter `data-status="upcoming"`)

Empty state per tab: existing `id="emptyTabState"` div pattern already handles this — reuse as-is.

**HC/Admin view restructure:**

Replace the current single card grid with Bootstrap tab navigation:

```html
<ul class="nav nav-tabs" id="hcTabs">
    <li class="nav-item">
        <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#managementTab">
            <i class="bi bi-sliders me-1"></i> Management
        </button>
    </li>
    <li class="nav-item">
        <button class="nav-link" data-bs-toggle="tab" data-bs-target="#monitorTab">
            <i class="bi bi-binoculars me-1"></i> Monitoring
        </button>
    </li>
</ul>
```

The Management tab contains the existing CRUD card grid (all assessments, Edit/Questions/Delete/Regen Token actions). The Monitoring tab contains a simpler display of Open + Upcoming assessments across all users — assigned user visible, no management actions, sorted by Schedule ascending.

**The current tab filtering JavaScript must be updated:**

Current JS (`filterCards(status)`) works by hiding `.assessment-card` elements by their `data-status` attribute. For workers, the Completed cards will no longer be in the DOM (filtered at controller), so JS only needs to handle Open/Upcoming switching. For HC/Admin, Bootstrap's native tab system handles Management vs Monitoring panel switching — no custom JS needed for that level. Keep the inner Open/Upcoming filtering for the Monitoring tab if desired, or show all (Open + Upcoming) in a flat list.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Role-gated tab visibility | Custom middleware, session flags | `isHCAccess` bool + `@if` in Razor | Established pattern in every HC-gated view |
| Status filtering | JavaScript hide/show after full DB load | Controller `.Where()` before query | Workers should not receive Completed data at all — don't load then hide |
| Tab switching | Custom JS carousel/accordion | Bootstrap 5 native `nav-tabs` + `tab-content` | Already in the project; zero new dependencies |
| Empty state detection | Counting DOM elements with JS | Two ViewBag lists — empty when `.Count == 0` | Server-side; no DOM walking needed |
| Training Records link | New route/redirect | `Url.Action("Records", "CMP")` | Route already exists from Phase 10 |

**Key insight:** The current Assessment page filters by status client-side (JS hides cards). For workers, move this to the server — don't send Completed data at all. Client-side filtering is acceptable for the Open/Upcoming split because both sets are legitimately sent to the worker.

---

## Common Pitfalls

### Pitfall 1: Breaking Existing Redirects From EditAssessment/DeleteAssessment

**What goes wrong:** `EditAssessment` and `DeleteAssessment` (and `RegenerateToken`) all redirect to `RedirectToAction("Assessment", new { view = "manage" })`. If the `view` parameter semantics change (e.g., `"manage"` is renamed to `"management"`), those redirects land on the wrong view or get rejected.
**Why it happens:** Multiple controller actions encode `view = "manage"` as a literal string.
**How to avoid:** Keep `view = "manage"` as the entry point for the HC/Admin page. Don't rename or add a new `view` value that replaces `"manage"`. The Management tab should be the default active tab when arriving at `view = "manage"`.
**Warning signs:** After the change, clicking Edit on a card and saving should return to the Management tab — verify with a full round-trip test.

### Pitfall 2: Admin SelectedView Five-Value Verification

**What goes wrong:** A gate like `if (view == "manage" && userRole != UserRoles.Admin && userRole != "HC")` redirects non-HC/Admin to personal view. But a new check using `isHCAccess` might accidentally redirect Admin in Coachee/Coach/Atasan SelectedView to the worker page.
**Why it happens:** Prior decision says Admin SelectedView must be manually verified for every modified action.
**How to avoid:** Use `isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC` (not `user.SelectedView == "HC"`), so Admin always gets management view regardless of selected simulation. Document this decision in plan comments.
**Warning signs:** Admin switching to "Coachee" view and hitting /CMP/Assessment unexpectedly sees only Open/Upcoming instead of management tabs.

### Pitfall 3: Client-Side Tab Filter Leaves Completed Tab Active for Workers

**What goes wrong:** If the Completed tab's `<li>` element is left in the HTML but the cards are gone (filtered at server), the tab renders but shows the empty state — confusing UX.
**Why it happens:** Developer removes card rendering logic but forgets to remove the tab button from the `<ul>`.
**How to avoid:** For workers: remove the Completed `<li class="nav-item">` entirely. Replace with the Training Records callout. Remove the `#completed-tab-pane` section and related JS handling.
**Warning signs:** Worker sees a "Completed" tab that shows an empty state with "No assessments in this category."

### Pitfall 4: Search Filter With Scoped ViewBag Data

**What goes wrong:** The current `search` parameter applies to the single model list. After the HC/Admin split into two ViewBag lists, search might only filter one list or cause confusion about which tab it applies to.
**Why it happens:** The search form posts `view=manage` and a search term; the controller currently applies search to the unified query. With two separate queries, search needs to be applied to both.
**How to avoid:** Apply the `search` filter before splitting into management vs. monitor queries, or apply it to both queries independently. Alternatively, scope search only to the active tab (Management tab gets search; Monitoring is always unfiltered since it only shows Open/Upcoming which is a small set).
**Warning signs:** Search on HC page returns results in Management but not in Monitoring (or vice versa), or pagination counts are wrong.

### Pitfall 5: Pagination Complexity With Two Data Sets

**What goes wrong:** The current action calculates `totalCount`, `totalPages`, etc. for a single query. With two ViewBag lists for HC/Admin, pagination gets ambiguous — which list does the pagination control apply to?
**Why it happens:** The current view has one pagination component for the whole page.
**How to avoid:** For the initial implementation, load without pagination for both HC/Admin tabs (the number of Open+Upcoming assessments system-wide is bounded and small). Pagination is only critical for the Management tab which may have hundreds of records. Use ViewBag with separate count/page values per tab, or simply load all for Monitoring and keep pagination only for Management.
**Warning signs:** Pagination controls show on Monitoring tab and navigate to wrong data.

---

## Code Examples

Verified patterns from codebase reads:

### Pattern 1: isHCAccess Gate (Phase 8 Established Pattern)

```csharp
// Source: Phase 8 RESEARCH.md — established fix pattern for HC gates
bool isHCAccess = userRole == UserRoles.HC ||
                  (userRole == UserRoles.Admin && user.SelectedView == "HC");
// For Assessment page: use the simpler form that always gives Admin management access:
bool isHCAccess = userRole == UserRoles.HC || userRole == UserRoles.Admin;
```

### Pattern 2: Worker Status Filter in Controller

```csharp
// Source: CMPController.Assessment() — extend existing personal branch
// Existing code (line 114-115):
//   query = query.Where(a => a.UserId == userId);
// Add after userId filter:
if (!isHCAccess)
{
    query = query.Where(a => a.Status == "Open" || a.Status == "Upcoming");
}
```

### Pattern 3: Bootstrap 5 Tab Structure (Already Used in Codebase)

The project already uses Bootstrap 5 tabs in `Assessment.cshtml` (lines 79-95: `nav-tabs`, `tab-pane`). The same pattern applies:

```html
<!-- Source: Assessment.cshtml lines 79-95 — same tab pattern, new labels -->
<ul class="nav nav-tabs mb-4" id="hcAssessmentTabs" role="tablist">
    <li class="nav-item" role="presentation">
        <button class="nav-link active" id="management-tab"
                data-bs-toggle="tab" data-bs-target="#management-tab-pane"
                type="button" role="tab">
            <i class="bi bi-sliders me-1"></i> Management
        </button>
    </li>
    <li class="nav-item" role="presentation">
        <button class="nav-link" id="monitor-tab"
                data-bs-toggle="tab" data-bs-target="#monitor-tab-pane"
                type="button" role="tab">
            <i class="bi bi-binoculars me-1"></i> Monitoring
        </button>
    </li>
</ul>
<div class="tab-content" id="hcAssessmentTabsContent">
    <div class="tab-pane fade show active" id="management-tab-pane" role="tabpanel">
        <!-- Management content: all assessments, CRUD actions -->
    </div>
    <div class="tab-pane fade" id="monitor-tab-pane" role="tabpanel">
        <!-- Monitoring content: Open + Upcoming, read-only -->
    </div>
</div>
```

### Pattern 4: Training Records Callout Link

```html
<!-- Source: Phase 10 — Records action at /CMP/Records is the canonical history destination -->
<div class="alert alert-info d-flex align-items-center gap-2 mb-4" role="alert">
    <i class="bi bi-clock-history fs-5"></i>
    <span>
        Looking for completed assessments?
        <a href="@Url.Action("Records", "CMP")" class="alert-link fw-semibold">
            View your Training Records
        </a>
    </span>
</div>
```

### Pattern 5: Empty State Per Tab (Existing Pattern, Reuse As-Is)

```html
<!-- Source: Assessment.cshtml lines 322-326 — existing empty state, works per tab -->
<div class="text-center py-5 d-none" id="emptyOpenState">
    <i class="bi bi-inbox text-muted mb-3" style="font-size: 4rem;"></i>
    <h5 class="text-muted">No open assessments</h5>
    <p class="text-muted small">Check back later for new assessments.</p>
</div>
```

### Pattern 6: AssessmentSession Status Values

```csharp
// Source: AssessmentSession.cs line 20 (comment) — canonical status strings
// Status: "Open", "Upcoming", "Completed"
// SetStatus logic is in CMPController.SubmitExam() — sets to "Completed" on submission
```

---

## Current State Inventory

What exists today that Phase 11 changes:

### CMPController.Assessment() — Lines 75-157

| Aspect | Current | After Phase 11 |
|--------|---------|----------------|
| Worker status filter | None — all statuses returned | `.Where(Status == "Open" or "Upcoming")` |
| HC/Admin view | Single "manage" view — all assessments | Two ViewBag lists: ManagementData (all) + MonitorData (Open+Upcoming) |
| Role gate | `if (view == "manage" && userRole != Admin && userRole != "HC")` | `isHCAccess` bool used throughout |
| ViewBag.ViewMode | `"personal"` or `"manage"` | Same — plus `ViewBag.ManagementData` and `ViewBag.MonitorData` for HC |

### Assessment.cshtml — Current Tab Structure

Current tabs (lines 79-95): Open | Upcoming | Completed

Worker tabs after Phase 11: Open | Upcoming (+ Training Records callout; no Completed tab)

HC/Admin tabs after Phase 11: Management | Monitoring (Bootstrap native tab switch, not JS filter)

Current JS (`filterCards()`, lines 547-578): filters by `data-status`. After Phase 11, workers still need this for Open/Upcoming switching. HC/Admin tab switch handled by Bootstrap — the CRUD card grid in Management doesn't need JS status filtering.

### Existing Redirects That Must Not Break

| Action | Current Redirect Target | Status After Phase 11 |
|--------|------------------------|----------------------|
| EditAssessment POST | `RedirectToAction("Assessment", new { view = "manage" })` | Still valid — lands on Management tab |
| DeleteAssessment POST | Same | Same |
| RegenerateToken | N/A (AJAX, no redirect) | Unchanged |

---

## State of the Art

| Old Pattern | New Pattern for Phase 11 | Impact |
|-------------|--------------------------|--------|
| JS-based tab hide/show for Completed | Server-side query filter removes Completed | Completed data never transmitted to worker browser |
| Single "manage" view for all HC/Admin content | Management tab (CRUD) + Monitoring tab (read-only overview) | Cleaner separation of concerns |
| `userRole != "HC"` gate | `isHCAccess` bool gate | Admin always gets management access regardless of SelectedView |
| Three-tab layout for all users | Two-tab for workers; two-tab for HC/Admin (different tabs) | Contextually appropriate tab sets |

---

## Open Questions

1. **Should Admin in Coachee/Coach/Atasan SelectedView see the worker page or the HC/Admin page?**
   - What we know: Phase 10 decision says "Admin always gets highest-access view regardless of simulated role" for the Records page
   - What's unclear: ROADMAP requirement says "HC or Admin" sees Management and Monitoring tabs — it doesn't address Admin-in-simulated-role
   - Recommendation: Use `isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC` (Admin always gets HC/Admin experience, no SelectedView branching needed). Consistent with Phase 10 decision.

2. **Should Monitoring tab have its own search / pagination?**
   - What we know: Open+Upcoming assessments across all users is expected to be a small set (tens, not hundreds)
   - What's unclear: Whether HC users want to search/filter within the Monitoring tab
   - Recommendation: For initial implementation, no search or pagination on Monitoring tab — flat list sorted by Schedule ascending. Can add later if needed.

3. **Does the worker callout belong above the tabs or inside a tab?**
   - What we know: The callout must be visible — success criterion says "visible link or callout"
   - Recommendation: Place it above the tabs (before `<ul class="nav nav-tabs">`), always visible regardless of which tab is active. A small `alert-info` banner is less intrusive than an extra tab.

---

## Sources

### Primary (HIGH confidence)

- Direct read of `Controllers/CMPController.cs` — Assessment() action lines 75-157, all related actions
- Direct read of `Views/CMP/Assessment.cshtml` — complete 813-line file
- Direct read of `Views/CMP/Records.cshtml` — Training Records destination (Phase 10 output)
- Direct read of `Models/AssessmentSession.cs` — Status field and valid values
- Direct read of `Models/UserRoles.cs` — role constants and level hierarchy
- Direct read of `Models/ApplicationUser.cs` — SelectedView field
- Direct read of `Views/Shared/_Layout.cshtml` — Bootstrap 5 CDN confirmed, SelectedView dropdown
- Direct read of `.planning/phases/08-fix-admin-role-switcher-and-add-admin-to-supported-roles/08-RESEARCH.md` — isHCAccess pattern, SelectedView five-value inventory
- Direct read of `.planning/phases/10-unified-training-records/10-VERIFICATION.md` — Phase 10 complete, /CMP/Records confirmed
- Direct read of `.planning/ROADMAP.md` — Phase 11 requirements ASMT-01/02/03, success criteria

### Secondary (MEDIUM confidence)
- N/A — pure codebase analysis, no external sources needed

### Tertiary (LOW confidence)
- N/A

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new dependencies; all tooling confirmed present
- Architecture patterns: HIGH — directly derived from reading the exact files to be changed
- Controller changes: HIGH — exact lines identified, filter logic is straightforward EF Core `.Where()`
- View restructure: HIGH — Bootstrap 5 tab pattern already used in the same file
- Admin SelectedView routing: HIGH — cross-referenced with Phase 8 research and Phase 10 decision

**Research date:** 2026-02-18
**Valid until:** Until CMPController.cs or Assessment.cshtml are modified by another phase (stable internal codebase — no external dependencies)
