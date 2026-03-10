# Phase 147: Scoring & Results UI - Research

**Researched:** 2026-03-10
**Domain:** Chart.js radar chart + LINQ GroupBy scoring in ASP.NET Core MVC
**Confidence:** HIGH

## Summary

This phase adds sub-competency analysis (scoring + radar chart + summary table) to the existing Results page. The codebase already has Chart.js loaded globally via CDN in `_Layout.cshtml` and two existing chart instances (line + doughnut in CoachingProton partial) that establish the project pattern. The Results controller action (`CMPController.Results`, line 1785) already loads `packageQuestions` with their `SubCompetency` field -- the GroupBy calculation can be inserted right where the viewmodel is built (around line 1912).

**Primary recommendation:** Add a `List<SubCompetencyScore>` property to `AssessmentResultsViewModel`, compute it via LINQ GroupBy in the package path of `CMPController.Results`, and render a Chart.js radar chart + Bootstrap table in `Results.cshtml` between the motivational message (line 101) and Kompetensi Diperoleh section (line 103).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Section placed after motivational message, before Kompetensi Diperoleh
- Card header "Analisis Sub Kompetensi" with bi-radar icon
- Chart on top (full-width, centered, max ~300-350px), table below
- Blue theme: fill rgba(54, 162, 235, 0.2), border rgba(54, 162, 235, 1), points solid blue
- Fixed 0-100% scale with ring labels at 0/25/50/75/100
- Long names truncated to ~20 chars with ellipsis on labels; full name in tooltip
- Min 3 sub-competencies for radar chart; below 3 table only
- Table columns: Sub Kompetensi, Benar, Total, Persentase (sorted alphabetically)
- Percentage badges: bg-success >= PassPercentage, bg-danger below
- Totals row at bottom
- Untagged questions grouped under "Lainnya"
- Legacy assessments (no SubCompetency data): entire section silently hidden
- Admin/HC view identical to owner view
- Print-friendly rendering

### Claude's Discretion
- Exact chart canvas dimensions and responsive breakpoints
- Bootstrap icon choice for card header
- Print CSS details
- Tooltip formatting

### Deferred Ideas (OUT OF SCOPE)
- Sub-competency data on Certificate page
- Enhanced Admin/HC view with team averages/benchmarks
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ANAL-01 | Calculate score per sub-competency after exam submit (GroupBy SubCompetency -> % correct) | LINQ GroupBy on packageQuestions matching responses; insert at line ~1910 in CMPController.Results |
| ANAL-02 | Results page shows Chart.js radar chart with one axis per sub-competency | Chart.js already in _Layout.cshtml; use type: 'radar' with project's blue theme |
| ANAL-03 | Results page shows summary table (Sub Kompetensi, Benar, Total, Persentase) | Bootstrap table-hover pattern already used on Results page |
| ANAL-04 | Radar chart and table hidden when no SubCompetency data (graceful degradation) | Conditional render based on SubCompetencyScores list being empty |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Chart.js | latest (CDN) | Radar chart rendering | Already loaded in _Layout.cshtml line 168 |
| Bootstrap 5 | existing | Table, badges, cards | Project standard |
| Bootstrap Icons | existing | Card header icon | Project standard |
| LINQ | built-in | GroupBy scoring | Established pattern in codebase |

No additional packages needed.

## Architecture Patterns

### ViewModel Extension

Add to `Models/AssessmentResultsViewModel.cs`:

```csharp
public class SubCompetencyScore
{
    public string Name { get; set; } = "";
    public int Correct { get; set; }
    public int Total { get; set; }
    public double Percentage { get; set; }
}
```

Add property to `AssessmentResultsViewModel`:
```csharp
public List<SubCompetencyScore>? SubCompetencyScores { get; set; }
```

### Controller GroupBy Pattern

Insert in package path of `CMPController.Results` (after `responseDict` is built, before viewmodel construction ~line 1910):

```csharp
// Build sub-competency scores from package questions
var subCompScores = packageQuestions
    .GroupBy(q => string.IsNullOrWhiteSpace(q.SubCompetency) ? "Lainnya" : q.SubCompetency)
    .Select(g => {
        var total = g.Count();
        var correct = g.Count(q => {
            responseDict.TryGetValue(q.Id, out var resp);
            if (resp?.PackageOptionId == null) return false;
            var sel = q.Options.FirstOrDefault(o => o.Id == resp.PackageOptionId);
            return sel != null && sel.IsCorrect;
        });
        return new SubCompetencyScore
        {
            Name = g.Key,
            Correct = correct,
            Total = total,
            Percentage = total > 0 ? Math.Round((double)correct / total * 100, 1) : 0
        };
    })
    .OrderBy(s => s.Name)
    .ToList();

// Only include if at least one question has SubCompetency data
// (if all questions lack SubCompetency, the only group would be "Lainnya" from all questions)
var hasRealSubCompetency = packageQuestions.Any(q => !string.IsNullOrWhiteSpace(q.SubCompetency));
```

### Radar Chart Pattern (follows existing Chart.js usage)

```javascript
// Matches existing project pattern from _CoachingProtonContentPartial.cshtml
var ctx = document.getElementById('subCompRadarChart');
if (ctx) {
    var labels = @Html.Raw(Json.Serialize(Model.SubCompetencyScores.Select(s => s.Name)));
    var data = @Html.Raw(Json.Serialize(Model.SubCompetencyScores.Select(s => s.Percentage)));

    // Truncate labels for display
    var shortLabels = labels.map(function(l) {
        return l.length > 20 ? l.substring(0, 17) + '...' : l;
    });

    new Chart(ctx.getContext('2d'), {
        type: 'radar',
        data: {
            labels: shortLabels,
            datasets: [{
                label: 'Persentase Benar',
                data: data,
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                borderColor: 'rgba(54, 162, 235, 1)',
                pointBackgroundColor: 'rgba(54, 162, 235, 1)',
                pointBorderColor: '#fff',
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            scales: {
                r: {
                    min: 0, max: 100,
                    ticks: { stepSize: 25, callback: function(v) { return v + '%'; } },
                    pointLabels: { font: { size: 12 } }
                }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        title: function(items) { return labels[items[0].dataIndex]; },
                        label: function(item) { return item.raw + '%'; }
                    }
                }
            }
        }
    });
}
```

### View Section Placement

Insert in `Views/CMP/Results.cshtml` between line 101 (end of motivational message) and line 103 (Kompetensi Diperoleh):

```
Line 101: closing motivational alert div
-- INSERT: Sub-Competency Analysis card here --
Line 103: @if (Model.CompetencyGains != null ...)
```

### Anti-Patterns to Avoid
- **Counting correctness twice:** The existing code already counts correct/incorrect per question. Reuse `responseDict` for GroupBy rather than re-querying.
- **Showing empty radar chart:** Always gate on `hasRealSubCompetency` and count >= 3 for radar.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Radar chart rendering | Canvas drawing code | Chart.js `type: 'radar'` | Already loaded, battle-tested |
| Label truncation | Custom ellipsis logic | Simple substring + `...` | Tooltip shows full name |

## Common Pitfalls

### Pitfall 1: Counting Only Questions in orderedQuestionIds, Not All packageQuestions
**What goes wrong:** The `orderedQuestionIds` filters questions that are in the shuffled set. GroupBy must use only the questions actually in the assessment (those in `orderedQuestionIds`), not all `packageQuestions` from the DB query.
**How to avoid:** Filter `packageQuestions` to only those in `orderedQuestionIds` before GroupBy.

### Pitfall 2: "Lainnya" Becoming the Only Group
**What goes wrong:** If no questions have SubCompetency, GroupBy produces a single "Lainnya" group covering all questions -- this is meaningless analysis.
**How to avoid:** Check `hasRealSubCompetency` flag; if false, set `SubCompetencyScores = null` to hide the entire section.

### Pitfall 3: Chart.js Radar With < 3 Points
**What goes wrong:** Radar chart looks degenerate with 1-2 axes (just a line or point).
**How to avoid:** Only render `<canvas>` when `SubCompetencyScores.Count >= 3`; otherwise show table only.

### Pitfall 4: Legacy Path Has No SubCompetency
**What goes wrong:** Legacy path (non-package) questions don't have SubCompetency field.
**How to avoid:** Only compute SubCompetencyScores in the package path branch. Legacy path leaves it null.

## Code Examples

### Existing Chart.js CDN (verified in _Layout.cshtml:168)
```html
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
```

### Existing Chart.js Pattern (from _CoachingProtonContentPartial.cshtml:215)
```javascript
new Chart(protonTrendCtx.getContext('2d'), {
    type: 'line',
    data: { labels: protonTrendLabels, datasets: [{ ... }] },
    options: { responsive: true, plugins: { legend: { position: 'top' } }, scales: { ... } }
});
```

### Existing Results Page Card Pattern (from Results.cshtml:106)
```html
<div class="card shadow-sm mb-4">
    <div class="card-header bg-light">
        <h5 class="mb-0"><i class="bi bi-award me-2 text-success"></i>Kompetensi Diperoleh</h5>
    </div>
    <div class="card-body p-0">
        <table class="table table-hover align-middle mb-0">...</table>
    </div>
</div>
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | No automated test framework in project |
| Config file | none |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ANAL-01 | GroupBy scoring produces correct percentages | manual | Browser: submit exam, check results | N/A |
| ANAL-02 | Radar chart renders with correct axes | manual | Browser: verify chart appears | N/A |
| ANAL-03 | Summary table shows correct data | manual | Browser: verify table columns/values | N/A |
| ANAL-04 | Section hidden for legacy/no-data assessments | manual | Browser: view legacy assessment result | N/A |

### Sampling Rate
- Manual browser testing per task
- No automated test suite available

### Wave 0 Gaps
None -- project has no test infrastructure; all verification is manual browser testing (established project pattern).

## Open Questions

None -- all implementation details are clear from the codebase and CONTEXT.md decisions.

## Sources

### Primary (HIGH confidence)
- `Views/CMP/Results.cshtml` -- current Results page structure (227 lines)
- `Controllers/CMPController.cs:1785-1980` -- Results action with package/legacy paths
- `Models/AssessmentResultsViewModel.cs` -- current viewmodel (42 lines)
- `Models/AssessmentPackage.cs:44` -- SubCompetency field on PackageQuestion
- `Views/Shared/_Layout.cshtml:168` -- Chart.js CDN already loaded
- `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml:215-276` -- existing Chart.js pattern

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Chart.js already in project, no new dependencies
- Architecture: HIGH - clear insertion points identified in controller and view
- Pitfalls: HIGH - edge cases well-defined in CONTEXT.md decisions

**Research date:** 2026-03-10
**Valid until:** 2026-04-10 (stable -- no external dependencies changing)
