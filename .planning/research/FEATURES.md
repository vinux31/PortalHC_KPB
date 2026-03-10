# Feature Landscape

**Domain:** Assessment sub-competency analysis with radar chart visualization
**Researched:** 2026-03-10

## Table Stakes

Features users expect once "sub-competency analysis" is promised. Missing = feature feels half-baked.

| Feature | Why Expected | Complexity | Dependencies |
|---------|--------------|------------|--------------|
| SubCompetency field on AssessmentQuestion | Core data model -- without it nothing else works | Low | DB migration adding nullable string to `AssessmentQuestion` |
| Excel import parses SubCompetency column | Existing import is the only way questions enter the system; must support new column | Low | Existing import logic in AdminController + new DB field |
| Sub-competency summary table on Results page | Users need numbers before a chart means anything: Sub-Kompetensi / Benar / Total / % | Low | `AssessmentResultsViewModel` extension, calculation in controller |
| Radar chart (spider web) on Results page | The headline deliverable -- visual per-sub-competency breakdown | Medium | Chart.js (CDN), canvas element in Results view, calculated scores |
| Graceful handling of untagged questions | Legacy packages won't have tags; must not break Results page | Low | Null/empty check -- group untagged as "Umum" or hide chart entirely |

## Differentiators

Features that elevate beyond bare minimum. Worth building if cheap.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Color-coded pass/fail per axis | Radar chart axes turn red/green based on threshold -- instant gap identification | Low | Conditional colors in Chart.js pointBackgroundColor |
| Comparative radar overlay (attempt 1 vs 2) | Shows improvement over retakes -- valuable for development tracking | Medium | Requires loading previous AttemptHistory; Chart.js supports multiple datasets natively |
| Sub-competency breakdown in AssessmentMonitoring | HC sees aggregate sub-competency weakness across all examinees | High | New aggregation query, new partial in monitoring detail view |
| PDF export of analysis card | HR attaches results to personnel files | Medium | Could reuse existing QuestPDF infrastructure |

## Anti-Features

Features to explicitly NOT build.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Sub-competency master data CRUD | Over-engineering -- sub-competencies are free-text tags per package, not a managed entity | Let Excel import column be source of truth; names are strings on questions |
| Weighted scoring per sub-competency | All questions already have equal ScoreValue (10); weighting adds complexity with no clear user need | Simple correct/total ratio per sub-competency |
| Real-time radar during exam | Reveals weak areas mid-exam, distracting | Only show on Results page after submission |
| Per-question sub-competency edit UI | Questions are bulk-imported via Excel (50-200 per package); per-question editing is wasted effort | Keep Excel as single entry point for tagging |
| D3.js or heavy charting library | Overkill for one radar chart | Chart.js radar type is purpose-built for this; lightweight CDN include |

## Feature Dependencies

```
SubCompetency DB field (migration)
  -> Excel import update (needs field to write to)
  -> Score calculation per sub-competency (needs field to group by)
     -> Summary table on Results (needs calculated scores)
     -> Radar chart on Results (needs calculated scores)
```

Strictly sequential. No parallelism possible within this milestone.

## MVP Recommendation

All table stakes are required -- this is a small, focused milestone:

1. **DB migration** -- add `SubCompetency` nullable string to `AssessmentQuestion`
2. **Excel import update** -- add 7th column "Sub Kompetensi" to template and parsing logic
3. **Calculation logic** -- group answered questions by SubCompetency, compute correct/total/percentage
4. **Summary table** -- render below existing Results content, sorted worst-first
5. **Radar chart** -- Chart.js radar chart alongside summary table

Defer:
- **Comparative overlay**: requires multi-attempt data loading, separate scope
- **Monitoring aggregate**: touches different controller/views, separate milestone
- **PDF export**: layer on after visual is validated

## UX Patterns

**Radar chart conventions in assessment systems:**
- Minimum 3 axes, maximum 8-10 for readability. If a package has 15+ sub-competencies, chart becomes unreadable -- truncate labels or show only chart for packages with reasonable count.
- Use percentage (0-100%) on axes, not raw counts -- normalizes across sub-competencies with different question counts.
- Fill area with semi-transparent color (rgba alpha 0.2-0.3).
- Show data point labels on hover (Chart.js tooltip).

**Summary table conventions:**
- Sort by percentage ascending (worst-first) so weak areas are immediately visible.
- Highlight rows below pass threshold with warning/red background.
- Show column headers: Sub Kompetensi | Benar | Total Soal | Persentase (%).

**Layout:** Summary table above or beside radar chart. Table provides exact numbers; chart provides visual shape of competency profile.

## Complexity Assessment

**Overall: LOW-MEDIUM.** This is data tagging + visualization. No new auth, no new workflows, no multi-user interaction. The hardest part is ensuring Chart.js renders correctly within the existing Results view layout and handling edge cases (0 sub-competencies tagged, 1-2 axes only).

## Sources

- Existing codebase: `Models/AssessmentQuestion.cs` (no SubCompetency field yet), `Models/AssessmentResultsViewModel.cs` (needs extension)
- Chart.js radar chart documentation (stable, well-established API)
- Domain knowledge: standard competency assessment UX in HR/training platforms
