# Phase 107: Backend & Worker List Page - Research

**Researched:** 2026-03-06
**Domain:** ASP.NET Core MVC — CDPController actions, role-scoped data access, Razor list page
**Confidence:** HIGH

## Summary

This phase adds two new CDPController actions (HistoriProton and HistoriProtonDetail) and a worker list Razor view. The entire pattern already exists in this codebase — the CoachingProton action (CDPController.cs line 1257+) implements identical role-scoped access with the same RoleLevel branching (levels 1-3 = all, 4 = section, 5 = coach mapping, 6 = self). The worker list page follows the same table+search+filter pattern used throughout the portal.

No new libraries, models, or infrastructure needed. This is purely additive code following established patterns.

**Primary recommendation:** Clone the CoachingProton role-scoping logic verbatim, query ProtonTrackAssignment grouped by CoacheeId, and build a standard Bootstrap table view with client-side filtering.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Table layout with columns: No, Nama, NIP, Unit, Jalur, Progress Proton, Status, Aksi
- Progress Proton column uses visual step indicator (filled/empty circles connected by lines)
- Status badges: Lulus=green, Dalam Proses=yellow, Belum Mulai=gray
- Role scoping: Coachee auto-redirects to detail, Coach sees mapped coachees, SrSpv/SH sees section, HC/Admin sees all
- Dual role uses widest scope (SrSpv > Coach)
- Search by nama/NIP + filters: Section, Unit, Jalur, Status — all auto-apply on change
- Reset button clears all filters
- CDP Hub card (not navbar dropdown): after Coaching Proton, icon bi-clock-history, warna info
- Hub order: Plan IDP, Coaching Proton, Histori Proton, Deliverable, Dashboard
- Feature name: "Histori Proton"
- Breadcrumb: CDP > Histori Proton
- Only workers with >= 1 ProtonTrackAssignment appear
- Assignment without ProtonFinalAssessment = "Dalam Proses"

### Claude's Discretion
- Client-side vs server-side filtering implementation
- Filter cascade behavior (Section -> Unit dependent or independent)
- Pagination size (10/15/20)
- Step indicator CSS implementation details

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| HIST-01 | CDP navbar has "Histori Proton" menu item | CDP Hub card in Views/CDP/Index.cshtml — pattern from existing cards |
| HIST-02 | Coachee sees only own history (redirect to timeline) | RoleLevel==6 branch redirects to HistoriProtonDetail(user.Id) |
| HIST-03 | Coach/SrSpv/SH sees section workers | RoleLevel 4-5 scoping from CoachingProton action, reuse verbatim |
| HIST-04 | HC/Admin sees all workers | RoleLevel <= 3 branch, same as CoachingProton |
| HIST-05 | List shows workers with ProtonTrackAssignment | Query ProtonTrackAssignment grouped by CoacheeId, join Users |
| HIST-06 | Search by nama/NIP | Client-side JS filter on table rows (recommended) |
| HIST-07 | Filter by unit/section | Client-side dropdown filters or server-side query params |
| HIST-08 | Each row shows summary: nama, NIP, progress, status | Join ProtonTrack (TahunKe) + ProtonFinalAssessment for status |
</phase_requirements>

## Standard Stack

### Core (already in project)
| Library | Purpose | Why |
|---------|---------|-----|
| ASP.NET Core MVC | Controller + Razor views | Project framework |
| Entity Framework Core | Data queries | Project ORM |
| Bootstrap 5 | Table, badges, cards | Project design system |
| Bootstrap Icons | bi-clock-history, etc. | Project icon set |

No new packages needed.

## Architecture Patterns

### Recommended Project Structure
```
Controllers/
  CDPController.cs          # Add HistoriProton + HistoriProtonDetail actions
Views/CDP/
  Index.cshtml              # Add Histori Proton card
  HistoriProton.cshtml      # NEW — worker list page
  HistoriProtonDetail.cshtml  # NEW (Phase 108, but action stub needed)
Models/
  HistoriProtonViewModel.cs # NEW — list page view model
```

### Pattern 1: Role-Scoped Data Access (COPY from CoachingProton)
**What:** Branch on RoleLevel to determine visible coachee IDs
**Source:** CDPController.cs lines 1270-1295
```csharp
// Exact pattern from CoachingProton — reuse verbatim
if (userLevel <= 3) // HC/Admin — all
    scopedCoacheeIds = await _context.Users
        .Where(u => u.RoleLevel == 6 && u.IsActive)
        .Select(u => u.Id).ToListAsync();
else if (userLevel == 4) // SrSpv/SH — same section
    scopedCoacheeIds = await _context.Users
        .Where(u => u.Section == user.Section && u.RoleLevel == 6 && u.IsActive)
        .Select(u => u.Id).ToListAsync();
else if (userLevel == 5) // Coach — mapped coachees
    scopedCoacheeIds = await _context.CoachCoacheeMappings
        .Where(m => m.CoachId == user.Id && m.IsActive)
        .Select(m => m.CoacheeId).ToListAsync();
else // Level 6 — redirect to own detail
    return RedirectToAction("HistoriProtonDetail", new { userId = user.Id });
```

### Pattern 2: Coachee Redirect (HIST-02)
**What:** RoleLevel 6 never sees list — immediately redirect to own detail
```csharp
if (userLevel >= 6)
    return RedirectToAction("HistoriProtonDetail", new { userId = user.Id });
```

### Pattern 3: Worker Summary Query
**What:** Get workers with their Proton progress summary
```csharp
// Get all assignments for scoped coachees, grouped by coachee
var assignments = await _context.ProtonTrackAssignments
    .Include(a => a.ProtonTrack)
    .Where(a => scopedCoacheeIds.Contains(a.CoacheeId))
    .ToListAsync();

// Get final assessments for those assignments
var assessmentsByAssignment = await _context.ProtonFinalAssessments
    .Where(fa => assignments.Select(a => a.Id).Contains(fa.ProtonTrackAssignmentId))
    .ToDictionaryAsync(fa => fa.ProtonTrackAssignmentId);

// Group by coachee to build summary rows
var coacheeGroups = assignments.GroupBy(a => a.CoacheeId);
```

### Pattern 4: Progress Indicator Logic
**What:** Determine filled/empty circles for 3-year progress
```csharp
// For each coachee, check which TahunKe they have assignments for
// ProtonTrack.TahunKe values: "Tahun 1", "Tahun 2", "Tahun 3"
// Assignment exists + has ProtonFinalAssessment = filled circle (completed)
// Assignment exists + no ProtonFinalAssessment = filled circle (in progress)
// No assignment = empty circle
```

### Pattern 5: Status Determination
```csharp
// Latest assignment's status:
// Has ProtonFinalAssessment with Status "Completed" → "Lulus"
// Has assignment but no final assessment → "Dalam Proses"
// Edge case: worker with only past (inactive) assignments → show based on latest
```

### Pattern 6: CDP Hub Card (from Index.cshtml)
**What:** Add card after Coaching Proton card
```html
<!-- Histori Proton -->
<div class="col-12 col-md-6 col-lg-3">
    <div class="card border-0 shadow-sm h-100">
        <div class="card-body">
            <div class="d-flex align-items-center mb-3">
                <div class="icon-box bg-info bg-opacity-10 text-info rounded-3 p-3 me-3">
                    <i class="bi bi-clock-history fs-3"></i>
                </div>
                <div>
                    <h5 class="mb-0">Histori Proton</h5>
                    <small class="text-muted">Riwayat Perjalanan</small>
                </div>
            </div>
            <p class="text-muted mb-3">Lihat riwayat perjalanan Proton per pekerja</p>
            <a href="@Url.Action("HistoriProton", "CDP")" class="btn btn-info w-100">
                <i class="bi bi-arrow-right-circle me-2"></i>Lihat Riwayat
            </a>
        </div>
    </div>
</div>
```

### Anti-Patterns to Avoid
- **Don't create a separate controller:** Histori Proton belongs in CDPController (consistent with PlanIdp, CoachingProton, Dashboard)
- **Don't use ViewBag for complex data:** Use a proper ViewModel
- **Don't skip IsActive filter on Users:** Always filter `u.IsActive` for coachee queries

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Role scoping | Custom auth logic | Copy CoachingProton RoleLevel pattern | Already tested, handles edge cases |
| Pagination | Custom pager | Existing pagination pattern from other list pages | Consistency |
| Status badges | Custom CSS | Bootstrap badge classes (bg-success, bg-warning, bg-secondary) | Already in design system |

## Common Pitfalls

### Pitfall 1: Missing IsActive Filter
**What goes wrong:** Inactive/deleted workers appear in list
**How to avoid:** Always include `u.IsActive` when querying Users and `m.IsActive` for CoachCoacheeMappings

### Pitfall 2: Dual Role Scope Confusion
**What goes wrong:** User with both Coach and SrSpv roles sees only coach-scoped data
**How to avoid:** Use RoleLevel (integer) not role name. RoleLevel 4 (SrSpv) < 5 (Coach), so the widest scope wins naturally with the if/else chain

### Pitfall 3: Jalur Determination
**What goes wrong:** Worker has assignments across both Panelman and Operator tracks
**How to avoid:** Show latest assignment's TrackType. The CONTEXT.md says "latest assignment determines Jalur shown"

### Pitfall 4: Empty ProtonFinalAssessment
**What goes wrong:** Assuming every assignment has a final assessment
**How to avoid:** Left join / null check — assignment without assessment = "Dalam Proses"

## Code Examples

### ViewModel for Worker List
```csharp
public class HistoriProtonViewModel
{
    public List<HistoriProtonWorkerRow> Workers { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? FilterSection { get; set; }
    public string? FilterUnit { get; set; }
    public string? FilterJalur { get; set; }
    public string? FilterStatus { get; set; }
    public List<string> AvailableSections { get; set; } = new();
    public List<string> AvailableUnits { get; set; } = new();
}

public class HistoriProtonWorkerRow
{
    public string UserId { get; set; } = "";
    public string Nama { get; set; } = "";
    public string NIP { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Jalur { get; set; } = ""; // Panelman or Operator
    public bool Tahun1Done { get; set; }
    public bool Tahun2Done { get; set; }
    public bool Tahun3Done { get; set; }
    public bool Tahun1InProgress { get; set; }
    public bool Tahun2InProgress { get; set; }
    public bool Tahun3InProgress { get; set; }
    public string Status { get; set; } = ""; // Lulus, Dalam Proses, Belum Mulai
}
```

### Step Indicator CSS
```css
.step-indicator {
    display: inline-flex;
    align-items: center;
    gap: 0;
}
.step-indicator .step-dot {
    width: 12px; height: 12px;
    border-radius: 50%;
    display: inline-block;
}
.step-indicator .step-dot.done { background-color: var(--bs-success); }
.step-indicator .step-dot.in-progress { background-color: var(--bs-warning); }
.step-indicator .step-dot.empty { background-color: var(--bs-gray-300); }
.step-indicator .step-line {
    width: 16px; height: 2px;
    background-color: var(--bs-gray-400);
    display: inline-block;
}
```

## Discretion Recommendations

### Client-Side vs Server-Side Filtering
**Recommendation: Client-side filtering with server-side role scoping.**
- Role scoping MUST be server-side (security)
- Search/filter on the returned list can be client-side JS for instant UX
- Data volume is small (tens to low hundreds of workers per scope)
- Pagination can be client-side too given small dataset

### Filter Cascade
**Recommendation: Independent dropdowns (not cascading).**
- Simpler implementation
- Section and Unit are independent filter dimensions
- Consistent with existing filter patterns in the portal

### Pagination Size
**Recommendation: 15 rows per page.**
- Balanced between 10 (too few clicks) and 20 (too long)
- Matches common portal patterns

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework in project) |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet run` + manual verification |

### Phase Requirements Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| HIST-01 | CDP hub shows Histori Proton card | manual | N/A | N/A |
| HIST-02 | Coachee redirects to own detail | manual | N/A | N/A |
| HIST-03 | Coach/SrSpv/SH sees section workers | manual | N/A | N/A |
| HIST-04 | HC/Admin sees all workers | manual | N/A | N/A |
| HIST-05 | List shows workers with ProtonTrackAssignment | manual | N/A | N/A |
| HIST-06 | Search by nama/NIP works | manual | N/A | N/A |
| HIST-07 | Filter by unit/section works | manual | N/A | N/A |
| HIST-08 | Row shows summary data | manual | N/A | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` (compilation check)
- **Per wave merge:** Manual browser test of all requirements
- **Phase gate:** All HIST-01 through HIST-08 verified in browser

### Wave 0 Gaps
None -- no automated test infrastructure in this project; manual testing is the established pattern.

## Sources

### Primary (HIGH confidence)
- CDPController.cs — CoachingProton action (lines 1257-1317) for role-scoping pattern
- Models/ProtonModels.cs — ProtonTrackAssignment, ProtonFinalAssessment, ProtonTrack models
- Models/CoachCoacheeMapping.cs — Coach-coachee relationship model
- Views/CDP/Index.cshtml — CDP hub card grid pattern

### Secondary (HIGH confidence)
- Models/ApplicationUser.cs — Section, Unit, RoleLevel fields
- 107-CONTEXT.md — Locked decisions and discretion areas

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all libraries already in project
- Architecture: HIGH - direct clone of CoachingProton patterns
- Pitfalls: HIGH - based on actual codebase analysis

**Research date:** 2026-03-06
**Valid until:** 2026-04-06 (stable internal project)
