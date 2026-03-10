# Architecture Patterns

**Domain:** Sub-competency tagging + radar chart for existing assessment portal
**Researched:** 2026-03-10

## Recommended Architecture

### Overview

Add a `SubCompetency` string field to `PackageQuestion`, flow it through the existing import pipeline and Results action, compute per-sub-competency scores server-side in the ViewModel, and render a Chart.js radar chart client-side on Results.cshtml. No new tables, no new controllers, no new endpoints.

### Component Boundaries

| Component | Responsibility | What Changes |
|-----------|---------------|--------------|
| `PackageQuestion` model | Store sub-competency label per question | ADD `SubCompetency` string field |
| EF Migration | Schema update | NEW migration adding column |
| `DownloadQuestionTemplate` | Generate Excel template | ADD column G "Sub Kompetensi" |
| `ImportPackageQuestions` POST | Parse Excel/paste rows | READ column 7 (Excel) or field index 6 (paste), set `SubCompetency` |
| `ImportPackageQuestions.cshtml` | Show format reference | UPDATE format string to include "Sub Kompetensi" |
| `AssessmentResultsViewModel` | Carry result data to view | ADD `List<SubCompetencyScore>` property |
| `CMPController.Results` | Build ViewModel | ADD sub-competency grouping/scoring logic |
| `Results.cshtml` | Display results | ADD radar chart canvas + summary table section |

### Data Flow

```
Import Flow (unchanged shape, wider data):
  Excel/Paste -> AdminController.ImportPackageQuestions
    -> Parse col 7 as SubCompetency (nullable, backward-compatible)
    -> Save PackageQuestion with SubCompetency field

Results Flow (new aggregation step):
  CMPController.Results(id)
    -> Load PackageQuestions (already loaded, includes SubCompetency now)
    -> Load PackageUserResponses (already loaded)
    -> NEW: Group questions by SubCompetency
    -> NEW: For each group, count correct/total
    -> Populate vm.SubCompetencyScores
    -> Return View

View Rendering:
  Results.cshtml
    -> Existing score card (unchanged)
    -> NEW: If SubCompetencyScores has items, render:
      1. Radar chart (Chart.js canvas)
      2. Summary table (Sub Kompetensi | Benar | Total | %)
    -> Existing question review list (unchanged)
```

## New Models

```csharp
// Add to PackageQuestion (Models/AssessmentPackage.cs)
public string? SubCompetency { get; set; }

// Add to AssessmentResultsViewModel (Models/AssessmentResultsViewModel.cs)
public List<SubCompetencyScore>? SubCompetencyScores { get; set; }

// New class in same file
public class SubCompetencyScore
{
    public string SubCompetency { get; set; } = "";
    public int Correct { get; set; }
    public int Total { get; set; }
    public int Percentage => Total > 0 ? (int)Math.Round(Correct * 100.0 / Total) : 0;
}
```

## Integration Points (Detailed)

### 1. Model + Migration

**File:** `Models/AssessmentPackage.cs` line ~41
**Change:** Add `public string? SubCompetency { get; set; }` to `PackageQuestion`
**Migration:** `dotnet ef migrations add AddSubCompetencyToPackageQuestion`
**Risk:** None. Nullable column, zero impact on existing data.

### 2. Excel Template

**File:** `Controllers/AdminController.cs` ~line 5073
**Current headers:** `{ "Question", "Option A", "Option B", "Option C", "Option D", "Correct" }`
**New headers:** `{ "Question", "Option A", "Option B", "Option C", "Option D", "Correct", "Sub Kompetensi" }`
**Also update:** Example row (line ~5083) add example sub-competency value. Instruction row (line ~5100) note the new column.

### 3. Import Parsing (Excel path)

**File:** `Controllers/AdminController.cs` ~line 5168-5174
**Current:** Reads cells 1-6
**Add:** `var sub = row.Cell(7).GetString().Trim();` after line 5173
**Change tuple type** from 6-field to 7-field, or add SubCompetency to question creation.
**Backward compat:** If cell 7 is empty/missing, SubCompetency = null. Old templates still work.

### 4. Import Parsing (Paste path)

**File:** Same controller, paste parsing section
**Current:** Splits tab-delimited into 6 fields
**Add:** Read 7th field if present, default to null if missing
**Backward compat:** Old 6-column pastes still parse correctly (check field count).

### 5. Question Creation

**File:** Same controller, where `new PackageQuestion { ... }` is constructed
**Add:** `SubCompetency = sub` (or whatever the parsed value is)

### 6. Results Scoring

**File:** `Controllers/CMPController.cs` ~line 1840-1926 (package path)
**Where:** After the existing `foreach` loop that builds `questionReviews` or counts correct
**Add:** Group `packageQuestions` by `SubCompetency` (skip null/empty), for each group count how many the user got right using `responseDict`, build `List<SubCompetencyScore>`.
**Important:** This logic runs regardless of `AllowAnswerReview` -- the sub-competency breakdown should always show.

```csharp
// After building questionReviews / counting correctCount
var subScores = packageQuestions
    .Where(q => !string.IsNullOrEmpty(q.SubCompetency))
    .GroupBy(q => q.SubCompetency!)
    .Select(g => {
        int total = g.Count();
        int correct = g.Count(q => {
            responseDict.TryGetValue(q.Id, out var resp);
            if (resp?.PackageOptionId == null) return false;
            var opt = q.Options.FirstOrDefault(o => o.Id == resp.PackageOptionId);
            return opt != null && opt.IsCorrect;
        });
        return new SubCompetencyScore { SubCompetency = g.Key, Correct = correct, Total = total };
    })
    .OrderBy(s => s.SubCompetency)
    .ToList();
```

Set `viewModel.SubCompetencyScores = subScores.Any() ? subScores : null;`

### 7. Results View - Radar Chart

**File:** `Views/CMP/Results.cshtml`
**Where:** Between the score summary card and the question review section
**What:** Conditional block: only render if `Model.SubCompetencyScores` has items

```html
@if (Model.SubCompetencyScores?.Any() == true)
{
    <!-- Sub-Competency Analysis -->
    <div class="card shadow-sm mb-4">
        <div class="card-header"><h5>Analisa Sub Kompetensi</h5></div>
        <div class="card-body">
            <div class="row">
                <div class="col-md-6">
                    <canvas id="radarChart" height="300"></canvas>
                </div>
                <div class="col-md-6">
                    <table class="table table-sm"><!-- summary table --></table>
                </div>
            </div>
        </div>
    </div>
}
```

**Chart.js:** Add via CDN `<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>`. Serialize labels and data as JSON inline.

```javascript
new Chart(document.getElementById('radarChart'), {
    type: 'radar',
    data: {
        labels: @Html.Raw(Json.Serialize(Model.SubCompetencyScores.Select(s => s.SubCompetency))),
        datasets: [{
            label: 'Score %',
            data: @Html.Raw(Json.Serialize(Model.SubCompetencyScores.Select(s => s.Percentage))),
            fill: true,
            backgroundColor: 'rgba(54, 162, 235, 0.2)',
            borderColor: 'rgb(54, 162, 235)',
            pointBackgroundColor: 'rgb(54, 162, 235)'
        }]
    },
    options: {
        scales: { r: { min: 0, max: 100, ticks: { stepSize: 20 } } },
        plugins: { legend: { display: false } }
    }
});
```

## Patterns to Follow

### Pattern 1: Nullable Field for Backward Compatibility
**What:** `SubCompetency` is `string?` (nullable). Import accepts old templates without the column.
**Why:** Existing questions in DB get null. Existing Excel files without col 7 still import. Radar chart only shows when data exists.

### Pattern 2: Server-Side Aggregation, Client-Side Rendering
**What:** Compute scores in controller, serialize to view, let Chart.js render.
**Why:** No AJAX needed. Follows existing Results pattern (all data in ViewModel). Chart.js is lightweight (~60KB CDN).

### Pattern 3: Conditional UI Section
**What:** Radar + table only render when `SubCompetencyScores` is populated.
**Why:** Old assessment results (before migration) show no radar -- graceful degradation.

## Anti-Patterns to Avoid

### Anti-Pattern 1: Separate Sub-Competency Table
**What:** Creating a `SubCompetency` entity with FK from `PackageQuestion`
**Why bad:** Over-engineering. Sub-competency is just a label for grouping. A simple string field is sufficient. No need for CRUD on sub-competencies separately.
**Instead:** `string? SubCompetency` on PackageQuestion.

### Anti-Pattern 2: Client-Side Score Calculation
**What:** Sending raw question data to JS and computing scores in browser
**Why bad:** Leaks answer keys to client. Inconsistent with existing server-side grading.
**Instead:** Compute in controller, send only aggregated percentages.

### Anti-Pattern 3: Modifying Exam Submission Logic
**What:** Computing sub-competency scores at submission time and storing them
**Why bad:** Unnecessary denormalization. Results page already loads all questions + responses. Computing on-the-fly is fast (typically 20-50 questions).
**Instead:** Compute in Results action from existing data.

## Build Order (Dependency-Aware)

| Step | What | Depends On | Files Modified |
|------|------|------------|----------------|
| 1 | Add `SubCompetency` to model + migration | Nothing | `Models/AssessmentPackage.cs`, new migration |
| 2 | Update Excel template + import parsing | Step 1 | `Controllers/AdminController.cs` |
| 3 | Update import view format reference | Step 2 | `Views/Admin/ImportPackageQuestions.cshtml` |
| 4 | Add `SubCompetencyScore` to ViewModel | Step 1 | `Models/AssessmentResultsViewModel.cs` |
| 5 | Add scoring logic in Results action | Steps 1, 4 | `Controllers/CMPController.cs` |
| 6 | Add radar chart + table to Results view | Steps 4, 5 | `Views/CMP/Results.cshtml` |

Steps 2-3 (import) and 4-5 (scoring) are independent tracks that can be built in parallel after step 1. Step 6 depends on both tracks being complete for end-to-end testing.

## Scalability Considerations

Not applicable -- assessment packages have 20-50 questions max. No performance concerns with in-memory grouping.

## Sources

- Direct codebase analysis of existing models, controllers, and views
- Chart.js radar chart documentation (training data, HIGH confidence -- Chart.js radar API is stable and well-known)
