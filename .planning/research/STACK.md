# Technology Stack

**Project:** PortalHC KPB — v3.17 Assessment Sub-Competency Analysis
**Researched:** 2026-03-10

## Verdict: No New Packages Needed

The existing stack handles all v3.17 requirements. Chart.js is already loaded via CDN. No NuGet or JS additions required.

## Current Stack (unchanged)

| Technology | Version | Purpose |
|------------|---------|---------|
| ASP.NET Core | .NET 8.0 | Web framework |
| EF Core | 8.0.0 | ORM + migrations |
| SQL Server | — | Database |
| Bootstrap 5 | — | UI framework |
| Chart.js | latest (CDN) | Charts — **already in `_Layout.cshtml` line 168** |
| EPPlus / ClosedXML | — | Excel import/export |
| QuestPDF | 2026.2.2 | PDF generation |

## Feature-by-Feature Stack Mapping

### 1. SubCompetency Column on PackageQuestion

**Need:** Add `string? SubCompetency` to `PackageQuestion` model, EF Core migration.

**Stack:** EF Core migrations. Standard nullable string column addition. No new packages.

```csharp
// PackageQuestion addition
public string? SubCompetency { get; set; }
```

### 2. Excel Import Update (Sub Kompetensi column)

**Need:** Add "Sub Kompetensi" column to import template, parse during import.

**Stack:** Existing Excel library already handles this. Add one column read in the import logic.

### 3. Radar Chart (Spider Web) on Results Page

**Need:** Chart.js radar chart showing percentage score per sub-competency.

**Stack:** Chart.js `type: 'radar'` — built-in chart type, no plugins needed. Already loaded globally via CDN.

```javascript
new Chart(ctx, {
    type: 'radar',
    data: {
        labels: subCompetencyNames,   // from server via JSON
        datasets: [{
            label: 'Skor (%)',
            data: percentages,
            backgroundColor: 'rgba(54, 162, 235, 0.2)',
            borderColor: 'rgb(54, 162, 235)'
        }]
    },
    options: {
        scales: { r: { min: 0, max: 100, ticks: { stepSize: 20 } } }
    }
});
```

### 4. Summary Table (Sub Kompetensi | Benar | Total | %)

**Need:** Server-side calculation, render as Bootstrap table.

**Stack:** LINQ GroupBy on `PackageQuestion.SubCompetency`, join with `PackageUserResponse`. Pure C# + Razor.

### 5. ViewModel Extension

```csharp
public List<SubCompetencyScoreItem>? SubCompetencyScores { get; set; }

public class SubCompetencyScoreItem
{
    public string SubCompetency { get; set; } = "";
    public int Correct { get; set; }
    public int Total { get; set; }
    public int Percentage { get; set; }
}
```

## What NOT to Add

| Library | Why Not |
|---------|---------|
| Chart.js NuGet package | Already loaded via CDN — do not duplicate |
| chartjs-plugin-datalabels | Radar chart tooltips/labels work natively |
| D3.js or other charting lib | Chart.js radar is sufficient; D3 is overkill |
| Any server-side chart rendering | Client-side Chart.js handles it |

## Confidence: HIGH

All features use patterns already established in the codebase. Chart.js confirmed present in `_Layout.cshtml`. Zero new dependencies.

## Sources

- Verified: `Views/Shared/_Layout.cshtml` line 168 — Chart.js CDN
- Verified: `Models/AssessmentPackage.cs` — PackageQuestion model
- Verified: `Models/AssessmentResultsViewModel.cs` — current ViewModel
