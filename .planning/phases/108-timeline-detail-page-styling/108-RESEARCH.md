# Phase 108: Timeline Detail Page & Styling - Research

**Researched:** 2026-03-06
**Domain:** ASP.NET Core MVC Razor view + Bootstrap 5 vertical timeline UI
**Confidence:** HIGH

## Summary

Phase 108 builds the HistoriProtonDetail page: a vertical timeline showing a worker's Proton journey across Tahun 1/2/3. The controller action already exists with full authorization checks (Phase 107) but returns an empty placeholder view. The work is: (1) create a new ViewModel for the detail page, (2) populate it in the controller by querying ProtonTrackAssignment + ProtonFinalAssessment + CoachCoacheeMapping + User data, (3) replace the placeholder view with a left-aligned vertical timeline using Bootstrap 5 cards and custom CSS.

The data model is well understood. ProtonTrackAssignment links a coachee to a ProtonTrack (which has TrackType like "Panelman" and TahunKe like "Tahun 1"). ProtonFinalAssessment marks completion with CompetencyLevelGranted. CoachCoacheeMapping provides coach names. All tables already exist and are queried in the list page action.

**Primary recommendation:** Create HistoriProtonDetailViewModel with worker header info + list of timeline nodes, query all data in one controller method, render with pure CSS vertical timeline (no JS library needed).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Left-aligned vertical timeline: line on the left, content cards to the right
- Filled circles with color: green = Lulus, yellow = Dalam Proses, gray outline = Belum Mulai
- Each node's content in a Bootstrap card (shadow-sm, rounded)
- Connector line: solid between completed nodes, dashed leading to incomplete nodes
- No animation on page load
- Timeline width: col-lg-8 centered
- Summary (collapsed): Tahun label + Jalur + Status badge
- Expanded: Unit, Coach name, Competency Level, Dates (start/end)
- Click/toggle to expand using Bootstrap Collapse
- "Belum Mulai" nodes (no assignment) do NOT appear
- Only nodes with actual ProtonTrackAssignment appear
- Worker can only have 1 track (Panelman OR Operator, not both)
- Worker header card: Nama, NIP, Unit, Section, Jalur
- No step indicator in header
- Back navigation via breadcrumb only: CDP > Histori Proton > Detail
- Page title: "Detail Histori Proton - CDP"
- Same left-aligned layout on mobile, cards go full width

### Claude's Discretion
- Coach data source (CoachCoacheeMapping vs ProtonTrackAssignment)
- Expand/collapse animation style
- Exact card spacing and typography
- Color shades for status badges
- Connector line thickness and styling details

### Deferred Ideas (OUT OF SCOPE)
- Print/export PDF of worker Proton history
- Link from timeline node to CoachingProton page for that specific Tahun
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| HIST-09 | Vertical timeline with node per Proton year | CSS vertical timeline with left-aligned line + Bootstrap cards |
| HIST-10 | Each node shows Tahun Proton + Unit | ProtonTrack.TahunKe for tahun, User.Unit for unit at assignment time (use current unit) |
| HIST-11 | Each node shows Coach name | Query CoachCoacheeMapping for coach, join with Users for FullName |
| HIST-12 | Each node shows Status (Lulus/Dalam Proses/Belum Mulai) | ProtonFinalAssessment existence = Lulus, else = Dalam Proses |
| HIST-13 | Each node shows Competency Level if lulus | ProtonFinalAssessment.CompetencyLevelGranted (0-5) |
| HIST-14 | Each node shows start/end dates | ProtonTrackAssignment.AssignedAt = start, ProtonFinalAssessment.CompletedAt = end |
| HIST-15 | Chronological order (Tahun 1 -> 2 -> 3) | Order by ProtonTrack.Urutan or TahunKe |
| HIST-16 | Consistent Bootstrap 5 design | Reuse existing shadow-sm cards, badge colors from list page |
| HIST-17 | Responsive mobile design | col-lg-8 centered, full-width on mobile naturally |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap 5 | 5.x (already in project) | Card layout, collapse, badges, grid | Already used throughout portal |
| Bootstrap Icons | Already in project | Icons (bi-clock-history, bi-person, etc.) | Already used throughout portal |

### Supporting
No additional libraries needed. Pure CSS for timeline line/circles, Bootstrap Collapse for expand/toggle.

## Architecture Patterns

### New ViewModel Structure

```csharp
// Models/HistoriProtonDetailViewModel.cs
public class HistoriProtonDetailViewModel
{
    // Header info
    public string Nama { get; set; } = "";
    public string NIP { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Section { get; set; } = "";
    public string Jalur { get; set; } = ""; // "Panelman" or "Operator"

    // Timeline nodes (only assignments that exist)
    public List<ProtonTimelineNode> Nodes { get; set; } = new();
}

public class ProtonTimelineNode
{
    public int AssignmentId { get; set; }
    public string TahunKe { get; set; } = ""; // "Tahun 1", "Tahun 2", "Tahun 3"
    public int TahunUrutan { get; set; } // 1, 2, 3 for sorting
    public string Unit { get; set; } = ""; // Worker's unit
    public string CoachName { get; set; } = ""; // From CoachCoacheeMapping
    public string Status { get; set; } = ""; // "Lulus" or "Dalam Proses"
    public int? CompetencyLevel { get; set; } // 0-5, null if not lulus
    public DateTime StartDate { get; set; } // AssignedAt
    public DateTime? EndDate { get; set; } // CompletedAt from ProtonFinalAssessment
}
```

### Controller Query Pattern

The existing `HistoriProtonDetail` action already has auth checks. Extend it to:
1. Query ProtonTrackAssignments for the target userId (Include ProtonTrack)
2. Query ProtonFinalAssessments for those assignment IDs
3. Query CoachCoacheeMappings where CoacheeId = userId and IsActive (or all mappings to find coach per period)
4. Get target user info for header
5. Build ViewModel and return View(viewModel)

### Coach Data Source Recommendation (Claude's Discretion)

Use **CoachCoacheeMapping** as coach source. Reasoning:
- ProtonTrackAssignment has no CoachId field
- CoachCoacheeMapping directly links coach to coachee
- The mapping is per-coachee (not per-assignment), so the same coach applies to all active assignments
- For historical accuracy: query ALL mappings (not just IsActive) to find who was coach at the time
- If multiple mappings exist, use the one closest to the assignment date

Practical simplification: Since most coachees have a single active coach, query the active mapping first. If none, fall back to latest inactive one. Display "N/A" if no mapping found.

### CSS Timeline Pattern

```css
.timeline {
    position: relative;
    padding-left: 40px;
}
.timeline::before {
    content: '';
    position: absolute;
    left: 15px;
    top: 0;
    bottom: 0;
    width: 2px;
    background: var(--bs-gray-400);
}
.timeline-node {
    position: relative;
    margin-bottom: 1.5rem;
}
.timeline-node::before {
    content: '';
    position: absolute;
    left: -33px;
    top: 8px;
    width: 14px;
    height: 14px;
    border-radius: 50%;
    border: 2px solid;
    z-index: 1;
}
.timeline-node.status-lulus::before {
    background-color: var(--bs-success);
    border-color: var(--bs-success);
}
.timeline-node.status-proses::before {
    background-color: var(--bs-warning);
    border-color: var(--bs-warning);
}
```

### Expand/Collapse Pattern (Bootstrap Collapse)

```html
<div class="timeline-node status-lulus">
    <div class="card shadow-sm">
        <div class="card-header bg-transparent"
             data-bs-toggle="collapse"
             data-bs-target="#node-1"
             role="button" style="cursor: pointer;">
            <strong>Tahun 1</strong> - Panelman
            <span class="badge bg-success">Lulus</span>
        </div>
        <div id="node-1" class="collapse">
            <div class="card-body">
                <!-- Detail fields -->
            </div>
        </div>
    </div>
</div>
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Expand/collapse | Custom JS toggle | Bootstrap 5 Collapse component | Already in project, handles animation/accessibility |
| Status badges | Custom styled spans | Bootstrap badge classes (bg-success, bg-warning, bg-secondary) | Matches existing list page exactly |
| Responsive grid | Custom media queries | Bootstrap col-lg-8 offset-lg-2 | Standard grid, already used |
| Date formatting | Manual string formatting | `.ToString("dd MMM yyyy")` in Razor | Standard C# pattern used in project |

## Common Pitfalls

### Pitfall 1: Coach lookup returning wrong coach
**What goes wrong:** CoachCoacheeMapping may have multiple records (active/inactive) for a coachee
**Why it happens:** Coaches can change over time
**How to avoid:** Query all mappings for the coachee, match by date proximity to assignment, or just use the single active one with fallback
**Warning signs:** Coach name showing as "N/A" or showing current coach for old assignments

### Pitfall 2: Missing null checks on ProtonFinalAssessment fields
**What goes wrong:** CompletedAt is nullable, accessing it without check causes errors
**Why it happens:** "Dalam Proses" nodes have no final assessment
**How to avoid:** Always check if assessment exists before accessing CompetencyLevelGranted/CompletedAt

### Pitfall 3: Timeline connector line extending past last node
**What goes wrong:** The ::before pseudo-element line extends to bottom of container
**How to avoid:** Use `height: calc(100% - last-node-offset)` or clip the line at the last node using `.timeline-node:last-child` adjustments

### Pitfall 4: Dashed vs solid connector line logic
**What goes wrong:** CONTEXT specifies solid between completed, dashed leading to incomplete
**How to avoid:** Use segment-based approach: each node controls the line segment below it. Lulus node = solid segment below, Dalam Proses = dashed segment below, last node = no segment

## Code Examples

### Controller Data Query Pattern (based on existing list page at line 2294-2380)

```csharp
// In HistoriProtonDetail action, after auth checks:
var targetUser = await _context.Users.FindAsync(userId);
if (targetUser == null) return NotFound();

var assignments = await _context.ProtonTrackAssignments
    .Include(a => a.ProtonTrack)
    .Where(a => a.CoacheeId == userId)
    .OrderBy(a => a.ProtonTrack!.Urutan)
    .ToListAsync();

if (!assignments.Any()) return NotFound(); // No Proton history

var assignmentIds = assignments.Select(a => a.Id).ToList();
var assessments = await _context.ProtonFinalAssessments
    .Where(fa => assignmentIds.Contains(fa.ProtonTrackAssignmentId))
    .ToDictionaryAsync(fa => fa.ProtonTrackAssignmentId);

// Get coach name
var coachMapping = await _context.CoachCoacheeMappings
    .Where(m => m.CoacheeId == userId)
    .OrderByDescending(m => m.IsActive)
    .FirstOrDefaultAsync();
string coachName = "N/A";
if (coachMapping != null)
{
    var coach = await _context.Users.FindAsync(coachMapping.CoachId);
    coachName = coach?.FullName ?? "N/A";
}

// Determine Jalur
string jalur = assignments.First().ProtonTrack?.TrackType ?? "";

var nodes = assignments.Select(a => {
    var hasAssessment = assessments.TryGetValue(a.Id, out var fa);
    return new ProtonTimelineNode
    {
        AssignmentId = a.Id,
        TahunKe = a.ProtonTrack?.TahunKe ?? "",
        TahunUrutan = a.ProtonTrack?.Urutan ?? 0,
        Unit = targetUser.Unit ?? "",
        CoachName = coachName,
        Status = hasAssessment ? "Lulus" : "Dalam Proses",
        CompetencyLevel = hasAssessment ? fa!.CompetencyLevelGranted : null,
        StartDate = a.AssignedAt,
        EndDate = hasAssessment ? fa!.CompletedAt : null
    };
}).OrderBy(n => n.TahunUrutan).ToList();

var viewModel = new HistoriProtonDetailViewModel
{
    Nama = targetUser.FullName,
    NIP = targetUser.NIP ?? "",
    Unit = targetUser.Unit ?? "",
    Section = targetUser.Section ?? "",
    Jalur = jalur,
    Nodes = nodes
};

return View(viewModel);
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework in project) |
| Config file | none |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet build && dotnet run` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| HIST-09 | Vertical timeline renders | manual | Browser check | N/A |
| HIST-10 | Tahun + Unit shown per node | manual | Browser check | N/A |
| HIST-11 | Coach name shown per node | manual | Browser check | N/A |
| HIST-12 | Status badge per node | manual | Browser check | N/A |
| HIST-13 | Competency level if lulus | manual | Browser check | N/A |
| HIST-14 | Start/end dates per node | manual | Browser check | N/A |
| HIST-15 | Chronological order | manual | Browser check | N/A |
| HIST-16 | Bootstrap 5 consistent design | manual | Browser check | N/A |
| HIST-17 | Responsive mobile | manual | Browser responsive mode | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Per wave merge:** `dotnet build && dotnet run` + browser verification
- **Phase gate:** All 9 requirements verified in browser

### Wave 0 Gaps
None -- no automated test infrastructure in this project; all verification is manual browser testing.

## Sources

### Primary (HIGH confidence)
- Project codebase: `Models/ProtonModels.cs` (ProtonTrack, ProtonTrackAssignment, ProtonFinalAssessment)
- Project codebase: `Models/HistoriProtonViewModel.cs` (existing list ViewModel pattern)
- Project codebase: `Controllers/CDPController.cs` lines 2294-2434 (HistoriProton + HistoriProtonDetail actions)
- Project codebase: `Views/CDP/HistoriProton.cshtml` (list page with status badges, step indicators)
- Project codebase: `Views/CDP/HistoriProtonDetail.cshtml` (current placeholder)

### Secondary (MEDIUM confidence)
- Bootstrap 5 Collapse component: standard Bootstrap feature, well-known API

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - all Bootstrap 5, already in project
- Architecture: HIGH - mirrors existing list page patterns exactly
- Pitfalls: HIGH - derived from actual data model inspection

**Research date:** 2026-03-06
**Valid until:** 2026-04-06 (stable, no external dependencies)
