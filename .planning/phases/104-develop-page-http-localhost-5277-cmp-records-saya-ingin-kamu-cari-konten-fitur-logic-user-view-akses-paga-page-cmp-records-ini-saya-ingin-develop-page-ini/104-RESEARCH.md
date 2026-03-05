# Phase 104: Team Training View for CMP/Records - Research

**Researched:** 2026-03-05
**Domain:** ASP.NET Core MVC, Role-Based Access Control, Data Aggregation
**Confidence:** HIGH

## Summary

Phase 104 adds a Team View tab to the existing CMP/Records page, enabling users level 1-4 (Admin, HC, Managers, SectionHead, SrSupervisor) to monitor their team members' training and assessment compliance. This is a VIEW-ONLY monitoring feature with no editing capabilities. The implementation leverages existing infrastructure (WorkerTrainingStatus model, GetWorkersInSection method, UnifiedTrainingRecord model) and follows established portal patterns for role-based access control, client-side filtering, and Bootstrap 5 UI components.

**Primary recommendation:** Use the existing GetWorkersInSection() method as the data source, add a RecordsTeam action to CMPController, create a RecordsTeam.cshtml partial view for the tab content, and create a new RecordsWorkerDetail.cshtml view for individual worker history. All access control uses UserRoles.GetRoleLevel() pattern, with Level 4 (SrSupervisor) locked to their own section via dropdown pre-selection.

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Tab Structure:**
- 2 tabs total: "My Records" (unified assessment + training personal view) and "Team View" (team monitoring)
- My Records tab combines Assessment + Training into single unified view (removes the old separate tabs)
- Team View tab only visible to users level 1-4 (conditional rendering based on UserRoles.GetRoleLevel())
- Tab switching uses Bootstrap 5 nav-tabs pattern

**Table Columns (Team View Worker List):**
- 8 columns: Nama, NIP, Position, Section, Unit, Assessment (count), Training Total (count), Action Detail
- No compliance percentage column (removed — keep it simple)
- No category breakdown columns (MANDATORY, PROTON, etc.) — details in drill-down only
- Action Detail button opens worker detail page
- Table uses click-to-view pattern (not inline expand)

**Summary Cards:**
- No summary cards — removed for cleaner layout
- Simple text counter only: "Showing X workers" above table
- Counter updates dynamically based on active filters
- If filter by section → shows count for that section
- If filter by unit → shows count for that unit
- If no filter → shows total workers in scope

**Filtering Controls:**
- 5 filter controls:
  1. Section dropdown
  2. Unit dropdown
  3. Category dropdown (MANDATORY, PROTON, OJT, etc.) — Claude decides exact options based on TrainingRecord.Kategori data
  4. Status dropdown (ALL, Sudah, Belum) — Bahasa Indonesia
  5. Search text input (search by Nama or NIP)
- Filter layout: Grid 2 rows for better organization
  - Row 1: Section, Unit, Category
  - Row 2: Status, Search, Reset button
- Reset button: Returns all filters to default (ALL options, search cleared)
- Filters apply client-side for performance (no round-trip to server)

**Scope Enforcement for Level 4:**
- Lock dropdown pattern: Section dropdown is visible but locked/pre-selected to user's own section
- Level 4 users see their section in dropdown but cannot change it
- Unit dropdown still functional within the locked section
- This communicates scope clearly while preventing cross-section access

**Worker Detail Page:**
- Separate page route: `/CMP/RecordsWorkerDetail/{workerId}`
- Navigation: Click Action Detail button → navigates to worker detail page
- Breadcrumb: Short format — "CMP > Records > Worker Detail" (not full breadcrumb trail)
- Back button: Yes — button at top of page that returns to Team View with filters preserved
- Page content:
  - Worker info card (4 fields): Nama, NIP, Position, Section
  - Unified assessment + training table (same columns as personal Records page)
- Filters on detail page: Yes — worker detail page has its own filters (category, year, search) for the history table
- No export: No export button on worker detail page — view only

**Compliance Metrics:**
- No compliance percentage calculation — keep it simple
- Display raw counts only (Assessment count, Training count)
- No progress bars in table rows
- Text-only presentation: "12 assessments", "25 trainings"
- Status filter uses simple logic: "Sudah" = has training records, "Belum" = no training records (or filtered by category)

**Responsive Design:**
- Follow existing Records.cshtml pattern for table responsiveness
- Bootstrap 5 table-responsive wrapper for horizontal scroll on mobile
- Hide/show columns based on screen size if needed (Claude decides)

**Empty States:**
- Simple text only: "Tidak ada worker ditemukan" or "Belum ada data"
- No illustrations or elaborate empty states
- Clear and functional

**Table Styling:**
- Follow existing CMP module table patterns
- Bootstrap 5 table-hover for row highlighting
- Consistent with Records.cshtml modern style (shadow, rounded corners if pattern exists)

### Claude's Discretion

**Responsive Design:**
- Follow existing Records.cshtml pattern for table responsiveness
- Bootstrap 5 table-responsive wrapper for horizontal scroll on mobile
- Hide/show columns based on screen size if needed (Claude decides)

**Table Styling:**
- Follow existing CMP module table patterns
- Bootstrap 5 table-hover for row highlighting
- Consistent with Records.cshtml modern style (shadow, rounded corners if pattern exists)

**Category Dropdown Options:**
- Claude decides exact options based on TrainingRecord.Kategori data
- Known categories from codebase: "PROTON", "OJT", "MANDATORY", "IHT", "Assessment OJ", "Licensor", "OTS", "Mandatory HSSE Training"

### Deferred Ideas (OUT OF SCOPE)

- Export to Excel from Team View — deferred to future phase (nice-to-have, not critical)
- Bulk actions on selected workers — deferred to future phase (new capability)
- Email notification to non-compliant workers — deferred to future phase (notification feature)
- Compliance trend visualization (charts) — deferred to future phase (analytics feature)
- Comparison view (worker vs team average) — deferred to future phase (advanced analytics)

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 | Web framework | Project is built on ASP.NET Core 8.0 |
| Entity Framework Core | 8.0.0 | ORM | Already used throughout project for data access |
| Bootstrap | 5.x | UI framework | Already loaded in _Layout.cshtml, used for all portal UI |
| jQuery | (bundled) | DOM manipulation | Already used in existing Records.cshtml for filtering |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap Icons | 1.10.0 | Iconography | Already loaded in _Layout.cshtml for badges and buttons |
| ASP.NET Identity | 8.0.0 | Authentication/Authorization | Used for User.IsInRole() and UserManager |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Client-side filtering | Server-side filtering with AJAX | Client-side is faster for small-medium datasets; server-side needed for 1000+ workers |

**Installation:**
No new packages needed. All required libraries already installed and configured.

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
├── CMPController.cs          # Add RecordsTeam() and RecordsWorkerDetail() actions
Models/
├── WorkerTrainingStatus.cs   # Reuse existing model for team view data
├── UnifiedTrainingRecord.cs  # Reuse existing model for worker detail history
Views/CMP/
├── Records.cshtml            # Modify: Add third tab "Team View" (conditional)
├── RecordsTeam.cshtml        # NEW: Team View tab content (worker list + filters)
├── RecordsWorkerDetail.cshtml # NEW: Worker detail page (unified history table)
```

### Pattern 1: Role-Based Access Control

**What:** Use UserRoles.GetRoleLevel() to determine tab visibility and data access scope

**When to use:** Any time access needs to be restricted based on user role level

**Example:**
```csharp
// In CMPController.RecordsTeam action
var user = await _userManager.GetUserAsync(User);
if (user == null) return Challenge();

var userRole = await _userManager.GetRolesAsync(user);
var roleLevel = UserRoles.GetRoleLevel(userRole.FirstOrDefault());

// Level 5-6 (Coach, Supervisor, Coachee) cannot access
if (roleLevel > 4)
{
    return Forbid();
}

// Level 4 (SrSupervisor) locked to own section
string? sectionFilter = null;
if (roleLevel == 4)
{
    sectionFilter = user.Section; // Lock to own section
}
```

**Source:** C:/Users/Administrator/Desktop/PortalHC_KPB/Models/UserRoles.cs (lines 42-54)

### Pattern 2: Existing Data Reuse

**What:** Leverage GetWorkersInSection() method for Team View data

**When to use:** When fetching worker list with training statistics

**Example:**
```csharp
// In CMPController.RecordsTeam action
var workers = await GetWorkersInSection(
    section: sectionFilter,
    unitFilter: unit,
    category: category,
    search: search,
    statusFilter: statusFilter
);

return View("RecordsTeam", workers);
```

**Source:** C:/Users/Administrator/Desktop/PortalHC_KPB/Controllers/CMPController.cs (lines 705-816)

### Pattern 3: Unified Records for Worker Detail

**What:** Reuse GetUnifiedRecords() and UnifiedTrainingRecord model for worker detail page

**When to use:** When showing individual worker's combined assessment + training history

**Example:**
```csharp
// In CMPController.RecordsWorkerDetail action
public async Task<IActionResult> RecordsWorkerDetail(string workerId)
{
    var worker = await _userManager.FindByIdAsync(workerId);
    if (worker == null) return NotFound();

    var unifiedRecords = await GetUnifiedRecords(workerId);

    var viewModel = new WorkerDetailViewModel
    {
        WorkerName = worker.FullName,
        NIP = worker.NIP,
        Position = worker.Position,
        Section = worker.Section,
        UnifiedRecords = unifiedRecords
    };

    return View(viewModel);
}
```

**Source:** C:/Users/Administrator/Desktop/PortalHC_KPB/Controllers/CMPController.cs (lines 570-619)

### Pattern 4: Client-Side Filtering with JavaScript

**What:** Use JavaScript to filter table rows without server round-trip

**When to use:** For responsive UI with small-medium datasets (< 1000 rows)

**Example:**
```javascript
// Filter works on all rows
function filterTable() {
    const section = document.getElementById('sectionFilter').value;
    const unit = document.getElementById('unitFilter').value;
    const category = document.getElementById('categoryFilter').value;
    const status = document.getElementById('statusFilter').value;
    const search = document.getElementById('searchInput').value.toLowerCase();

    document.querySelectorAll('.worker-row').forEach(row => {
        const rowSection = row.getAttribute('data-section');
        const rowUnit = row.getAttribute('data-unit');
        const rowCategories = row.getAttribute('data-categories');
        const hasTraining = row.getAttribute('data-has-training') === 'true';
        const name = row.getAttribute('data-name').toLowerCase();
        const nip = row.getAttribute('data-nip');

        const matchSection = !section || rowSection === section;
        const matchUnit = !unit || rowUnit === unit;
        const matchCategory = !category || rowCategories.includes(category);
        const matchStatus = status === 'ALL' ||
                           (status === 'Sudah' && hasTraining) ||
                           (status === 'Belum' && !hasTraining);
        const matchSearch = !search || name.includes(search) || nip.includes(search);

        row.style.display = (matchSection && matchUnit && matchCategory && matchStatus && matchSearch)
                          ? '' : 'none';
    });

    // Update counter
    const visibleCount = document.querySelectorAll('.worker-row[style=""]').length;
    document.getElementById('workerCounter').textContent = `Showing ${visibleCount} workers`;
}
```

**Source:** C:/Users/Administrator/Desktop/PortalHC_KPB/Views/CMP/Records.cshtml (lines 292-309)

### Anti-Patterns to Avoid

- **Don't create new ViewModels for existing models:** WorkerTrainingStatus already has all fields needed for Team View
- **Don't duplicate filtering logic:** GetWorkersInSection() already implements section, unit, category, search, and status filtering
- **Don't use server-side pagination:** Client-side filtering is sufficient for Team View use case
- **Don't hardcode role checks:** Use UserRoles.GetRoleLevel() pattern for consistency

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Role level determination | Manual if/else on role names | UserRoles.GetRoleLevel() | Centralized role hierarchy logic, consistent across portal |
| Worker statistics calculation | Manual LINQ aggregation | WorkerTrainingStatus model | Already has TotalTrainings, CompletedTrainings, CompletedAssessments properties |
| Unified record display | Separate assessment/training queries | UnifiedTrainingRecord model + GetUnifiedRecords() | Merges assessments and trainings into single list for unified table |
| Client-side filtering | Custom JavaScript filter logic | Bootstrap 5 + jQuery pattern from Records.cshtml | Proven pattern, consistent with existing pages |

**Key insight:** The portal already has all the building blocks needed for Team View. Reuse existing models, methods, and UI patterns rather than duplicating functionality.

## Common Pitfalls

### Pitfall 1: N+1 Query Problem

**What goes wrong:** Loading worker list without Include() causes separate database query for each worker's training records

**Why it happens:** EF Core lazy-loads navigation properties by default

**How to avoid:** Use .Include(u => u.TrainingRecords) when querying users (already implemented in GetWorkersInSection)

**Warning signs:** Slow page load time, excessive database queries visible in logs

**Source:** C:/Users/Administrator/Desktop/PortalHC_KPB/Controllers/CMPController.cs (line 709)

### Pitfall 2: Incorrect Role Level Logic

**What goes wrong:** Level 4 (SrSupervisor) can see workers from other sections

**Why it happens:** Forgetting to lock section filter for role level 4

**How to avoid:** Always check roleLevel == 4 and pre-set sectionFilter to user.Section

**Warning signs:** SrSupervisor users seeing workers outside their section in testing

**Source:** C:/Users/Administrator/Desktop/PortalHC_KPB/Models/UserRoles.cs (lines 58-64)

### Pitfall 3: Filter State Not Preserved

**What goes wrong:** User clicks Action Detail, views worker detail, clicks Back button, and filters are reset

**Why it happens:** Not storing filter state in session or URL parameters

**How to avoid:** Pass filter state as query parameters to RecordsWorkerDetail action and include them in Back button URL

**Warning signs:** UX testing reveals frustration with lost filter context

### Pitfall 4: Empty Table State Not Handled

**What goes wrong:** Table shows no rows when no workers match filter, but no feedback message

**Why it happens:** Only rendering rows, not handling empty case

**How to avoid:** Check Model.Count == 0 and render "Tidak ada worker ditemukan" message

**Warning signs:** Confusing UI when filters return no results

### Pitfall 5: Category Filter Logic Inconsistency

**What goes wrong:** Status "Sudah"/"Belum" filter works differently with category filter vs without

**Why it happens:** GetWorkersInSection() calculates CompletionPercentage based on category, but status filter applies after calculation

**How to avoid:** Document the expected behavior: "Sudah" = has completed training in selected category, "Belum" = no completed training in selected category

**Warning signs:** QA team reports inconsistent filter behavior

## Code Examples

Verified patterns from official sources:

### Access Control Check

```csharp
// Check if user can access Team View
var user = await _userManager.GetUserAsync(User);
var userRole = await _userManager.GetRolesAsync(user);
var roleLevel = UserRoles.GetRoleLevel(userRole.FirstOrDefault());

// Hide Team View tab for level 5-6
@if (roleLevel <= 4)
{
    <li class="nav-item">
        <a class="nav-link" id="tab-team" data-bs-toggle="tab" href="#pane-team">
            <i class="bi bi-people me-1"></i>Team View
        </a>
    </li>
}
```

### Lock Section Dropdown for Level 4

```razor
<!-- Section dropdown -->
<select id="sectionFilter" class="form-select" @(roleLevel == 4 ? "disabled" : "")>
    <option value="">All Sections</option>
    @foreach (var section in Model.Select(w => w.Section).Distinct().OrderBy(s => s))
    {
        <option value="@section" @(roleLevel == 4 && section == userSection ? "selected" : "")>
            @section
        </option>
    }
</select>
```

### Worker Table with Data Attributes

```razor
<tbody>
    @foreach (var worker in Model)
    {
        <tr class="worker-row"
            data-section="@worker.Section"
            data-unit="@worker.Unit"
            data-categories="@string.Join(",", worker.TrainingRecords.Select(t => t.Kategori))"
            data-has-training="@(worker.CompletedTrainings > 0 ? "true" : "false")"
            data-name="@worker.WorkerName.ToLower()"
            data-nip="@(worker.NIP ?? "")">
            <td>@worker.WorkerName</td>
            <td>@worker.NIP</td>
            <td>@worker.Position</td>
            <td>@worker.Section</td>
            <td>@worker.Unit</td>
            <td>@worker.CompletedAssessments</td>
            <td>@worker.TotalTrainings</td>
            <td>
                <a asp-action="RecordsWorkerDetail" asp-route-workerId="@worker.WorkerId"
                   class="btn btn-sm btn-outline-primary">
                    <i class="bi bi-eye"></i> Detail
                </a>
            </td>
        </tr>
    }
</tbody>
```

### Worker Detail Page with Breadcrumb

```razor
<!-- Breadcrumb -->
<nav aria-label="breadcrumb" class="mb-3">
    <ol class="breadcrumb small">
        <li class="breadcrumb-item"><a href="/CMP">CMP</a></li>
        <li class="breadcrumb-item"><a asp-action="Records" asp-controller="CMP">Records</a></li>
        <li class="breadcrumb-item active">Worker Detail</li>
    </ol>
</nav>

<!-- Back button -->
<div class="mb-3">
    <a asp-action="Records" asp-controller="CMP" asp-fragment="team"
       class="btn btn-outline-secondary">
        <i class="bi bi-arrow-left me-1"></i> Back to Team View
    </a>
</div>

<!-- Worker info card -->
<div class="card mb-3">
    <div class="card-body">
        <h5 class="card-title">@Model.WorkerName</h5>
        <p class="card-text mb-1"><strong>NIP:</strong> @Model.NIP</p>
        <p class="card-text mb-1"><strong>Position:</strong> @Model.Position</p>
        <p class="card-text mb-0"><strong>Section:</strong> @Model.Section</p>
    </div>
</div>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual role checking with strings | UserRoles.GetRoleLevel() centralized method | Phase 69 (LDAP Auth Service Foundation) | Consistent role hierarchy across all controllers |
| Separate Assessment and Training views | Unified Records view with tab switching | Phase 10 (Unified Training Records) | Single source of truth for worker history |
| Server-side pagination | Client-side filtering for small datasets | Phase 40 (Training Records History Tab) | Faster UI response, better UX |

**Deprecated/outdated:**
- SectionHead role level changed from 4 to 3 (full access) in Phase 69 — do not assume SectionHead is level 4
- Records page no longer has separate Assessment/Training tabs — unified into single "My Records" tab

## Open Questions

1. **Category dropdown exact options**
   - What we know: TrainingRecord.Kategori contains values like "PROTON", "OJT", "MANDATORY", "IHT", "Assessment OJ"
   - What's unclear: Exact list of all possible category values in the database
   - Recommendation: Query distinct categories from TrainingRecords table dynamically: `_context.TrainingRecords.Select(t => t.Kategori).Distinct().OrderBy(k => k)`

2. **Section and Unit dropdown population**
   - What we know: Need to populate dropdowns with distinct values from users table
   - What's unclear: Should sections be hardcoded or queried dynamically?
   - Recommendation: Query dynamically from Users table to handle new sections automatically: `Model.Select(w => w.Section).Distinct().OrderBy(s => s)`

3. **Worker detail page filter state preservation**
   - What we know: Need to preserve filters when navigating back from worker detail
   - What's unclear: Should filter state be in URL params, session, or TempData?
   - Recommendation: Use URL query parameters for all filters (section, unit, category, status, search) — allows bookmarking and browser back button to work correctly

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | None (project has no test infrastructure) |
| Config file | None |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| (None specified) | Manual browser testing required | Manual-only | N/A | N/A |

### Sampling Rate

- **Per task commit:** Manual browser verification of tab visibility, filter behavior, and access control
- **Per wave merge:** Manual browser verification of full user flows
- **Phase gate:** Manual UAT by user before marking phase complete

### Wave 0 Gaps

- [ ] No test infrastructure exists — project has never had automated tests
- [ ] All testing is manual browser-based verification
- [ ] No framework install: N/A — manual testing only

**Note:** Project has historically relied on manual browser testing. This phase continues that pattern.

## Sources

### Primary (HIGH confidence)

- C:/Users/Administrator/Desktop/PortalHC_KPB/Models/UserRoles.cs — Role level constants and GetRoleLevel() method
- C:/Users/Administrator/Desktop/PortalHC_KPB/Models/WorkerTrainingStatus.cs — Reusable model for team view data structure
- C:/Users/Administrator/Desktop/PortalHC_KPB/Models/UnifiedTrainingRecord.cs — Unified assessment + training record model
- C:/Users/Administrator/Desktop/PortalHC_KPB/Models/TrainingRecord.cs — Training record entity with Kategori field
- C:/Users/Administrator/Desktop/PortalHC_KPB/Models/ApplicationUser.cs — User entity with Section, Unit, Position fields
- C:/Users/Administrator/Desktop/PortalHC_KPB/Controllers/CMPController.cs — Existing GetWorkersInSection() and GetUnifiedRecords() methods
- C:/Users/Administrator/Desktop/PortalHC_KPB/Views/CMP/Records.cshtml — Existing UI patterns for tabs, filtering, table styling

### Secondary (MEDIUM confidence)

- C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/phases/104-develop-page-http-localhost-5277-cmp-records-saya-ingin-kamu-cari-konten-fitur-logic-user-view-akses-paga-page-cmp-records-ini-saya-ingin-develop-page-ini/104-CONTEXT.md — User decisions and requirements for this phase
- C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/codebase/CONVENTIONS.md — Project coding conventions and patterns
- C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/STATE.md — Project history and architectural decisions

### Tertiary (LOW confidence)

- None — all findings verified from codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries already in use, verified from HcPortal.csproj
- Architecture: HIGH - All patterns verified from existing controller and view code
- Pitfalls: HIGH - All pitfalls identified from code review and documented phase histories

**Research date:** 2026-03-05
**Valid until:** 30 days (stable architecture, existing patterns)
