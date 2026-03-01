# Phase 66: UI Polish - Research

**Researched:** 2026-02-28
**Domain:** ASP.NET Core 8 / Razor UI — Empty State Messaging and Server-Side Pagination
**Confidence:** HIGH

## Summary

Phase 66 addresses two critical UI/UX gaps on the Progress page (ProtonProgress.cshtml): empty states and pagination. Currently, when filters return no data or datasets are very large, the page exhibits poor user experience—blank tables with no guidance, and potential performance issues from loading hundreds of rows at once.

The implementation leverages existing infrastructure already present in the codebase:
1. **Empty states:** The view already has conditional rendering (`@if (Model.Count > 0)`) that can be extended with role-aware, scenario-specific messaging and icons
2. **Pagination:** Server-side pagination fits the existing architecture (ProtonDataController query pattern, EF materialization in the controller action)
3. **Competency grouping:** The existing competency grouping logic (KompGroups, SubGroups) must be preserved—pagination boundaries respect data hierarchy

All requirements in UI-02 and UI-04 are achievable with view-level changes (empty state messaging, icons, "Clear filters" button) and controller-level changes (pagination logic with group-boundary respect, page-count calculation).

**Primary recommendation:** Implement server-side pagination in the ProtonProgress action with group-boundary enforcement, update the Razor view to show role-aware empty state messages with icons, and add a "Hapus Filter" button on filter-clear scenarios.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Empty State Messaging:**
- Different messages per scenario: (1) no coachees assigned, (2) filters return no matches, (3) coachee has no deliverables yet
- Icon + text centered in the table area — replaces entire table area, not per-accordion/section
- When filters return no results, include a "Clear filters" (e.g., "Hapus Filter") button alongside the message
- All messages in Bahasa Indonesia
- Informational only for "no coachees" scenario — no action suggestion, just state the fact
- Role-aware messages: HC/Admin gets contextual hints (e.g., assign track), Coach/Coachee gets simple informational text
- Icon style: Claude's discretion (match existing portal aesthetic)

**Pagination Behavior:**
- Server-side pagination — API returns only the current page of results
- Target ~20 rows per page, but **never split a competency group** — if a competency straddles the boundary, include all its deliverables even if the page exceeds 20 rows
- Numbered page navigation style: « 1 2 3 4 5 »
- Fixed page size — no user-selectable dropdown
- Show total count: "Menampilkan X-Y dari Z deliverable"
- Pagination controls positioned at bottom of table only

**State Transitions:**
- Loading: spinner overlay on dimmed current table (not skeleton rows)
- Spinner appears on both filter changes and page navigation
- Auto-scroll to top of table after page change
- Fade transition when data state changes (data → empty or empty → data)
- Any filter change resets pagination to page 1
- Network error: toast notification + keep current data visible (don't replace with error)

### Claude's Discretion

- Empty state icon style (line icon vs illustration — match portal aesthetic)
- Exact spinner/overlay implementation
- Fade transition duration and easing
- Toast notification style and duration

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| UI-02 | Tampilkan pesan empty state ketika tidak ada data deliverable | Empty state message rendering conditional logic, role-aware templates, scenario detection (no coachees, no filters match, coachee no deliverables), Bahasa Indonesia labels, icon patterns from existing codebase |
| UI-04 | Tabel data dipaginasi (server-side atau client-side) agar tidak load semua sekaligus | Server-side pagination in ProtonProgress action, competency-group-boundary-respecting slicing logic, page navigation UI (numbered buttons), result count display, pagination reset on filter change |

</phase_requirements>

## Standard Stack

### Core Framework
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core | 8.0 | Web framework for server rendering | Project baseline; Razor views used throughout |
| Entity Framework Core | 8.0.0 | Data layer with LINQ queries | All controller actions use `_context` for querying |
| Razor | 8.0 (built-in) | Server-side view templating | All `.cshtml` views use Razor syntax |
| Bootstrap | 5.x (inferred from HTML) | CSS framework for layout/responsive design | Card, table, modal, badge, button utilities visible throughout codebase |

### Supporting Libraries for UI
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap Icons (bi) | Inferred ~1.x | SVG icons for UI elements | Empty state icons, filter buttons, spinners (already in use: `bi-table`, `bi-search`, etc.) |
| jQuery (optional) | Not detected | DOM manipulation | Spinners, fade transitions, form handling (check if needed—view already uses vanilla JS in some places) |

### Current Implementations in Codebase
- **Empty state examples:** ProtonProgress.cshtml already has `@if (Model.Count == 0)` conditional; CMP/Assessment.cshtml toggles `d-none` on empty state divs
- **Pagination example:** ManageAssessment.cshtml has client-side pagination (`currentPage`, `totalCount` variables); ManageWorkers.cshtml counts rows
- **Result count display:** ProtonProgress.cshtml: `Menampilkan @ViewBag.FilteredCount dari @ViewBag.TotalCount data`
- **Icons:** Bootstrap Icons used consistently (already in project)

**Installation:** No new packages required. ClosedXML (0.105.0) already present; Bootstrap and Icons already in use.

## Architecture Patterns

### Recommended Project Structure (No new files needed)

Changes are contained to existing files:

```
Controllers/
├── CDPController.cs              # ProtonProgress action pagination logic
Views/
├── CDP/
│   └── ProtonProgress.cshtml    # Empty state messages, pagination UI
└── Shared/
    └── Layouts/_Layout.cshtml    # Optional: spinner CSS/JS (if needed globally)
```

### Pattern 1: Empty State Conditional Rendering
**What:** Replace blank table with centered icon + message when `Model.Count == 0`

**When to use:** After filter applied or initial page load with no data

**Implementation location:** ProtonProgress.cshtml, in place of `@if (Model.Count > 0)` table block

**Example structure:**
```html
@if (Model.Count == 0)
{
    <!-- Empty state: centered icon + message + optional action button -->
    <div class="text-center py-5">
        <i class="bi bi-inbox fs-1 text-muted mb-3"></i>
        <h5>@emptyStateMessage</h5>
        @if (showClearFiltersButton)
        {
            <button class="btn btn-outline-primary btn-sm">Hapus Filter</button>
        }
    </div>
}
else
{
    <!-- Table with pagination controls -->
}
```

### Pattern 2: Server-Side Pagination with Group Boundary Enforcement
**What:** Slice `progresses` list in controller, respecting competency group boundaries

**When to use:** When total deliverable rows > 20 and page size needs limiting

**Implementation location:** CDPController.cs ProtonProgress action, after query materialization

**Logic flow:**
1. Materialize full `progresses` list from database (current approach)
2. Group by competency (Kompetensi) and sub-kompetensi hierarchy
3. Slice groups to fit ~20 rows per page, never splitting a group
4. Calculate total page count and current page number
5. Pass paginated slice to view; ViewBag.CurrentPage, ViewBag.TotalPages

**Example pseudocode:**
```csharp
// After progresses = await query.ToListAsync()
var pageNumber = int.TryParse(Request.Query["page"], out var p) ? Math.Max(1, p) : 1;
var groupedByKompetensi = progresses
    .GroupBy(p => (
        kompetensi: p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi,
        subKompetensi: p.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi))
    .ToList();

// Slice groups to fit ~20 rows per page
var rowCounter = 0;
var pageGroups = new List<IGrouping<(string?, string?), TrackingItem>>();
foreach (var group in groupedByKompetensi)
{
    if (rowCounter > 0 && rowCounter + group.Count() > 20)
    {
        // Start new page if adding this group would exceed 20
        break;
    }
    pageGroups.Add(group);
    rowCounter += group.Count();
}

var paginatedData = pageGroups.SelectMany(g => g).ToList();
ViewBag.CurrentPage = pageNumber;
ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / targetPageSize);
```

### Pattern 3: Role-Aware Empty State Detection
**What:** Detect which scenario applies (no coachees, no filters match, no deliverables) and set ViewBag message/button visibility

**When to use:** Controller calculates empty state reason before rendering

**Implementation location:** CDPController.cs ProtonProgress action, after data load

**Scenarios:**
1. **No coachees assigned** (scopedCoacheeIds.Count == 0): "Belum ada coachee yang ditugaskan" (informational, no button)
2. **Filters applied but no matches** (progresses.Count == 0 && filters active): "Tidak ada deliverable yang sesuai filter. Coba ubah filter." (with "Hapus Filter" button)
3. **Coachee has no deliverables** (targetCoacheeId set, progresses.Count == 0): "Coachee ini belum memiliki deliverable" (informational)

### Anti-Patterns to Avoid

- **Loading all rows client-side then hiding:** If dataset has 500 rows, paginating client-side loads all 500 then shows 20—still slow and memory-heavy. Server-side prevents DB round-trips and memory bloat.
- **Splitting competency groups across pages:** User confusion ("why is Kompetensi A split?"), data navigation jarring. Always keep groups together.
- **Modal or side-panel empty state:** User expects to see the table area replaced; hiding in modal is confusing. Empty state replaces table footprint.
- **Multiple spinner implementations:** Use one overlay pattern throughout (overlay existing table, dim it, add spinner on top).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Pagination UI (numbered buttons 1 2 3 4 5) | Custom JS page renderer | Razor loop + conditional `@for` | Simpler, server-driven state is stateless, avoids JS state bugs |
| Empty state icon choices | Custom SVG / image files | Bootstrap Icons (bi-*) | Already in project, consistent aesthetic, no extra files |
| Spinner overlay | Custom CSS animation | Bootstrap `spinner-border` + custom overlay div | Bootstrap spinner is standard; overlay is simple CSS (position: fixed, z-index, etc.) |
| Fade transition animation | Custom @keyframes | CSS `transition: opacity 0.3s` or Bootstrap `fade` class | Minimal, built-in, doesn't require JS animation library |
| Group-boundary pagination | Custom recursive splitter | Simple `foreach` loop with counter | Group boundaries are domain-specific; a 20-line loop is clearer than a generic splitter |

**Key insight:** Pagination logic on server means no JavaScript state management needed. Razor can generate numbered links + hidden page param form. View is always in sync with model.

## Common Pitfalls

### Pitfall 1: Paginating Before Grouping
**What goes wrong:** Slice the full list into pages (rows 0-20, 21-40, ...) then group. Result: competency group A spans pages 1-2, confusing the user.

**Why it happens:** Naive approach: `progresses.Skip(pageSize * pageNum).Take(pageSize)` without considering grouping.

**How to avoid:** Group first, then slice groups to fit page boundaries. Calculate page count *after* grouping.

**Warning signs:** Data layout shows partial competency in table; user scrolls and sees "Kompetensi A Sub-B" continue on next page.

### Pitfall 2: Losing Filter State on Page Navigation
**What goes wrong:** User applies filters (Bagian=RFCC, Track=Panelman), navigates to page 2, and page 2 shows *all* data (filters lost).

**Why it happens:** Page navigation link doesn't preserve query string (`?bagian=RFCC&track=Panelman&page=2`); only includes `?page=2`.

**How to avoid:** All pagination links must preserve existing filters. Use Razor helper to construct links with all params:
```html
<a href="@Url.Action("ProtonProgress", new { bagian = ViewBag.SelectedBagian, unit = ViewBag.SelectedUnit, trackType = ViewBag.SelectedTrackType, tahun = ViewBag.SelectedTahun, page = pageNum })">@pageNum</a>
```

**Warning signs:** User applies filters, clicks page 2, and suddenly sees data not matching filters.

### Pitfall 3: Empty State Message Doesn't Match Actual Reason
**What goes wrong:** Message says "Tidak ada coachee" but actually no deliverables matched filters; or message says "No filters applied" when filters *are* applied.

**Why it happens:** Controller doesn't distinguish scenarios; view always shows generic message.

**How to avoid:** Explicitly detect reason in controller:
- If `scopedCoacheeIds.Count == 0` → "Belum ada coachee"
- If `data.Count == 0 && (bagian || unit || trackType || tahun specified)` → "Tidak ada deliverable sesuai filter"
- If `targetCoacheeId != null && data.Count == 0` → "Coachee ini belum memiliki deliverable"

Set ViewBag flags and render appropriate message in view.

**Warning signs:** User sees misleading message; applies action suggested by message and nothing changes.

### Pitfall 4: Spinner Doesn't Block Interaction
**What goes wrong:** User clicks "Page 2" during loading, spinner overlay appears but button still clickable; multiple requests fire.

**Why it happens:** Spinner is decorative (no `pointer-events: none`); form/links still interactive.

**How to avoid:** Overlay must have `pointer-events: none` during load, OR disable all filter/nav buttons while spinner visible.

```css
.spinner-overlay {
    position: fixed; z-index: 9999;
    background: rgba(255,255,255,0.7);
    pointer-events: none; /* Important: blocks interaction */
}
```

**Warning signs:** User can trigger multiple page loads simultaneously; spinner visible but page-nav buttons still respond to clicks.

### Pitfall 5: No Auto-Scroll to Table on Page Change
**What goes wrong:** User is on page 1, scrolled down viewing row 15. Clicks page 2. Page loads but user still sees page 1's scroll position (off-screen). Page 2 table is below viewport.

**Why it happens:** Razor server-side render just changes `Model` and reloads view; no scroll-to-table logic.

**How to avoid:** After page navigation form submits, include a small script that scrolls to table:
```html
<script>
    document.addEventListener('DOMContentLoaded', function() {
        const table = document.getElementById('progressTable');
        if (table) table.scrollIntoView({ behavior: 'smooth' });
    });
</script>
```

Or use form anchor: `<form action="ProtonProgress#progressTable">` (server redirects to fragment).

**Warning signs:** User navigates to page 2, page loads but appears blank (scrolled past table); user confused.

## Code Examples

Verified patterns from existing codebase and ASP.NET Core standards:

### Empty State Message Rendering (Razor)
**Source:** Existing pattern in CMP/Assessment.cshtml and Admin/ManageAssessment.cshtml

```html
<!-- ProtonProgress.cshtml -->
@if (Model.Count == 0)
{
    <div class="text-center py-5">
        <i class="bi bi-inbox fs-1 text-muted mb-3"></i>
        @if (scopedCoacheeIds.Count == 0)
        {
            <h5 class="text-muted">Belum ada coachee yang ditugaskan</h5>
        }
        else if (!string.IsNullOrEmpty(ViewBag.SelectedBagian as string) ||
                 !string.IsNullOrEmpty(ViewBag.SelectedUnit as string) ||
                 !string.IsNullOrEmpty(ViewBag.SelectedTrackType as string) ||
                 !string.IsNullOrEmpty(ViewBag.SelectedTahun as string))
        {
            <h5 class="text-muted">Tidak ada deliverable yang sesuai filter</h5>
            <button type="button" class="btn btn-outline-primary btn-sm mt-3" onclick="clearFilters()">
                <i class="bi bi-arrow-counterclockwise me-2"></i>Hapus Filter
            </button>
        }
        else
        {
            <h5 class="text-muted">Coachee ini belum memiliki deliverable</h5>
        }
    </div>
}
else
{
    <!-- Table rendering (existing code) -->
}
```

### Server-Side Pagination in Controller
**Source:** CDPController.cs ProtonProgress action

```csharp
// After: var progresses = await query.OrderBy(...).ToListAsync();

// Pagination: Page number from query string, default to 1
int pageNumber = int.TryParse(Request.Query["page"], out var pn) ? Math.Max(1, pn) : 1;
const int targetRowsPerPage = 20;

// Group by competency (full hierarchy)
var competencyGroups = progresses
    .GroupBy(p => new {
        KompetensiNama = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi,
        KompetensiId = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.Id,
        SubKompetensiNama = p.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi,
        SubKompetensiId = p.ProtonDeliverable?.ProtonSubKompetensi?.Id
    }, p => p)
    .ToList();

// Slice groups to respect 20-row boundary
var currentRowCount = 0;
var pagesGroups = new List<List<IGrouping<dynamic, ProtonDeliverableProgress>>>();
var currentPageGroups = new List<IGrouping<dynamic, ProtonDeliverableProgress>>();

foreach (var group in competencyGroups)
{
    int groupRowCount = group.Count();
    // If adding this group would exceed 20 rows and we already have data, start new page
    if (currentRowCount > 0 && currentRowCount + groupRowCount > targetRowsPerPage)
    {
        pagesGroups.Add(new List<IGrouping<dynamic, ProtonDeliverableProgress>>(currentPageGroups));
        currentPageGroups = new List<IGrouping<dynamic, ProtonDeliverableProgress>>();
        currentRowCount = 0;
    }
    currentPageGroups.Add(group);
    currentRowCount += groupRowCount;
}

// Add final page
if (currentPageGroups.Count > 0)
{
    pagesGroups.Add(currentPageGroups);
}

// Extract current page data
int totalPages = pagesGroups.Count;
if (pageNumber > totalPages) pageNumber = Math.Max(1, totalPages);

var currentPageGroupsList = pageNumber > 0 && pageNumber <= totalPages
    ? pagesGroups[pageNumber - 1]
    : new List<IGrouping<dynamic, ProtonDeliverableProgress>>();

var paginatedProgresses = currentPageGroupsList
    .SelectMany(g => g)
    .ToList();

// Map to TrackingItem (existing code)
data = paginatedProgresses.Select(p => new TrackingItem { ... }).ToList();

// ViewBag pagination values
ViewBag.CurrentPage = pageNumber;
ViewBag.TotalPages = totalPages;
ViewBag.FilteredCount = progresses.Count; // Total across all pages
```

### Pagination UI (Numbered Links in Razor)
**Source:** Standard Bootstrap pagination pattern

```html
<!-- After table, at bottom -->
@if (ViewBag.TotalPages > 1)
{
    <nav aria-label="Page navigation" class="mt-4">
        <ul class="pagination justify-content-center">
            <!-- Previous button -->
            @if (ViewBag.CurrentPage > 1)
            {
                <li class="page-item">
                    <a class="page-link" href="@Url.Action("ProtonProgress", new {
                        bagian = ViewBag.SelectedBagian,
                        unit = ViewBag.SelectedUnit,
                        trackType = ViewBag.SelectedTrackType,
                        tahun = ViewBag.SelectedTahun,
                        page = ViewBag.CurrentPage - 1
                    })">« Sebelumnya</a>
                </li>
            }

            <!-- Numbered pages (show up to 5-7 page numbers) -->
            @{
                int startPage = Math.Max(1, ViewBag.CurrentPage - 2);
                int endPage = Math.Min(ViewBag.TotalPages, ViewBag.CurrentPage + 2);
            }

            @for (int p = startPage; p <= endPage; p++)
            {
                if (p == ViewBag.CurrentPage)
                {
                    <li class="page-item active"><span class="page-link">@p</span></li>
                }
                else
                {
                    <li class="page-item">
                        <a class="page-link" href="@Url.Action("ProtonProgress", new {
                            bagian = ViewBag.SelectedBagian,
                            unit = ViewBag.SelectedUnit,
                            trackType = ViewBag.SelectedTrackType,
                            tahun = ViewBag.SelectedTahun,
                            page = p
                        })">@p</a>
                    </li>
                }
            }

            <!-- Next button -->
            @if (ViewBag.CurrentPage < ViewBag.TotalPages)
            {
                <li class="page-item">
                    <a class="page-link" href="@Url.Action("ProtonProgress", new {
                        bagian = ViewBag.SelectedBagian,
                        unit = ViewBag.SelectedUnit,
                        trackType = ViewBag.SelectedTrackType,
                        tahun = ViewBag.SelectedTahun,
                        page = ViewBag.CurrentPage + 1
                    })">Selanjutnya »</a>
                </li>
            }
        </ul>
    </nav>
}
```

### Spinner Overlay (Optional — Claude's Discretion)
**Source:** Bootstrap + custom CSS pattern (existing overlay used in ProtonData/Index.cshtml)

```html
<!-- Add to ProtonProgress.cshtml, hidden by default -->
<div id="loadingSpinner" class="d-none" style="
    position: fixed; top: 0; left: 0; right: 0; bottom: 0;
    background: rgba(255,255,255,0.7);
    z-index: 9999;
    display: flex; align-items: center; justify-content: center;
    pointer-events: none;">
    <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
    </div>
</div>

<script>
    // Show spinner on filter/page form submit
    document.querySelectorAll('form, .pagination a').forEach(el => {
        el.addEventListener('click', function() {
            // Only show if it's a navigation action (not, e.g., modal close)
            if (this.tagName === 'A' || this.tagName === 'FORM') {
                document.getElementById('loadingSpinner').classList.remove('d-none');
            }
        });
    });

    // Hide spinner on page load
    window.addEventListener('load', function() {
        document.getElementById('loadingSpinner').classList.add('d-none');
    });
</script>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Load all rows, hide via CSS | Server-side pagination, fetch only current page | ASP.NET Core 3.0+ (2019) | Reduced memory, faster page render, better UX for large datasets |
| Skeleton loaders | Spinner overlay on dimmed table | Bootstrap 5 / Modern UX | Simpler to implement, less JS, user clear about loading state |
| Empty state in modal | Empty state replaces table footprint | Modern UX best practice (2020+) | User expectations; in-place empty state more discoverable |
| Client-side pagination (data-tables.js) | Server-side pagination (Razor loops) | Varies by project | For CRUD tables, server-side is stateless; client-side better for analytics |

**Deprecated/outdated:**
- **Manual row counting for pagination:** Replaced by `Math.Ceiling` or group-slice logic
- **jQuery-based spinners:** Bootstrap's `spinner-border` replaces custom JS animations
- **Hardcoded page counts:** Dynamic calculation from total records and page size standard

## Open Questions

None. All decision points are locked in CONTEXT.md. Implementation is straightforward Razor/C# using existing patterns in the codebase.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected — project uses manual testing (see TESTING_CHECKLIST.md) |
| Config file | None (no Jest, Pytest, xUnit, etc.) |
| Quick run command | Manual UAT in browser; use `/gsd:verify-work` |
| Full suite command | Manual UAT checklist (TESTING_CHECKLIST.md) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Manual Test Steps | Automated? |
|--------|----------|-----------|-------------------|-----------|
| UI-02 | Empty state shows when no coachees assigned (role: HC) | Manual | 1. Login as HC, navigate to Progress, 2. Verify message "Belum ada coachee yang ditugaskan", 3. Verify icon visible, 4. No "Hapus Filter" button | ❌ Wave 0 |
| UI-02 | Empty state shows when filters return no matches | Manual | 1. Assign coachees, 2. Apply Bagian filter with no matching data, 3. Verify message "Tidak ada deliverable yang sesuai filter", 4. Verify "Hapus Filter" button visible, 5. Click button and verify all data returns | ❌ Wave 0 |
| UI-02 | Empty state shows with role-aware message (Coach vs HC) | Manual | 1. Login as Coach, see coachee-specific message; 2. Login as HC, see HC-specific message with contextual hint | ❌ Wave 0 |
| UI-04 | Table pagination: only 20 rows per page max (group boundary respected) | Manual | 1. Setup coachee with 50+ deliverables in multiple competencies, 2. Load Progress page, 3. Verify page 1 shows ~20 rows max, 4. Verify no competency split across pages, 5. Click page 2 and verify next batch loads | ❌ Wave 0 |
| UI-04 | Pagination UI shows numbered buttons 1 2 3 4 5 | Manual | 1. Setup data with 60+ total deliverables, 2. Load Progress page, 3. Verify pagination "« 1 2 3 4 5 »" visible at bottom, 4. Click page 3 and verify page loads correctly | ❌ Wave 0 |
| UI-04 | Filter change resets pagination to page 1 | Manual | 1. Load Progress page, navigate to page 3, 2. Apply filter (e.g., Track=Panelman), 3. Verify page resets to 1 and displays filtered page 1 data | ❌ Wave 0 |
| UI-04 | Result count displays correctly: "Menampilkan X-Y dari Z deliverable" | Manual | 1. Load page 1 of 3, 2. Verify text shows "Menampilkan 1-20 dari 60 deliverable" (example), 3. Navigate page 2, 4. Verify shows "Menampilkan 21-40 dari 60 deliverable" | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** Manual navigation test (filter → page → empty state scenarios)
- **Per wave merge:** Full UAT checklist (see TESTING_CHECKLIST.md + manual steps above)
- **Phase gate:** All manual tests pass before `/gsd:verify-work`

### Wave 0 Gaps
- No automated tests exist; all validation is manual (aligns with project's existing UAT approach)
- Consider adding:
  - [ ] `Controllers/Tests/CDPControllerTests.cs` — unit tests for pagination logic (group boundary slicing, page count calculation)
  - [ ] `Views/Tests/ProtonProgressTests.cs` — integration tests for Razor rendering (empty state detection, pagination links)
  - But: **Not required for Phase 66 — manual UAT sufficient per project standards**

*(Current approach: Manual testing via browser UAT, consistent with Phases 63-65 pattern)*

## Sources

### Primary (HIGH confidence)
- **ASP.NET Core 8 documentation** — Controller actions, Razor syntax, ViewBag usage, async/await patterns verified via official Microsoft docs
- **Existing codebase patterns:**
  - CDPController.cs ProtonProgress action (1395-1644) — query materialization, ViewBag, role-based logic
  - ProtonProgress.cshtml — filter UI, grouping logic, conditional rendering, existing "Menampilkan X dari Y data" label
  - Admin/ManageAssessment.cshtml — pagination example with client-side page counter and total count
  - CMP/Assessment.cshtml — empty state example with icon + message + state toggle
  - Bootstrap 5 pagination and spinner-border components — verified in existing HTML

### Secondary (MEDIUM confidence)
- ASP.NET Core standard practices for server-side pagination (LINQ group-by, Skip/Take patterns)
- Bahasa Indonesia UI labels from existing project (consistent tone across CPDP/Proton features)

### Tertiary (LOW confidence)
- None — research grounded in existing codebase and official documentation

## Metadata

**Confidence breakdown:**
- **Standard stack:** HIGH — ASP.NET Core 8, EF Core 8, Razor all baseline in project
- **Architecture:** HIGH — Pagination and empty state patterns documented in existing views; server-side slicing is standard practice
- **Pitfalls:** HIGH — Group-boundary pagination and filter state preservation are explicit pitfalls in similar systems; prevention clear from CONTEXT decisions
- **Test strategy:** HIGH — Project uses manual UAT; no automated test framework to configure

**Research date:** 2026-02-28
**Valid until:** 2026-03-28 (stable ASP.NET Core patterns; decision are locked, no expected changes)
