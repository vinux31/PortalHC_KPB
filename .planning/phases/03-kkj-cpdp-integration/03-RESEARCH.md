# Phase 3: KKJ/CPDP Integration - Research

**Researched:** 2026-02-14
**Domain:** Competency tracking, gap analysis, and IDP automation
**Confidence:** MEDIUM

## Summary

Phase 3 integrates the existing assessment system with the KKJ competency matrix and CPDP framework to enable automated competency tracking, gap analysis, and personalized development planning. The technical foundation exists: assessment results capture scores and pass/fail status, KKJ matrix defines target competency levels per position, CPDP framework provides competency definitions and development indicators, and IDP items track individual development activities.

The core challenge is establishing the mapping relationships between these disconnected systems. This requires creating a many-to-many relationship between assessments and competencies, tracking individual competency achievement levels over time, calculating gaps between current and target levels, and generating actionable IDP suggestions from gap analysis results.

**Primary recommendation:** Use a join table pattern for assessment-to-competency mapping, create a separate UserCompetencyLevel tracking table with temporal history, implement server-side gap calculation in ViewModels, and use Chart.js radar charts for visual gap analysis following the established Phase 2 pattern.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Entity Framework Core | 8.0 | Database access, relationship mapping | Already in use, excellent many-to-many support since EF Core 5 |
| Chart.js | 4.x (via CDN) | Gap analysis visualization (radar charts) | Already used in Phase 2 for analytics, no additional dependencies |
| Bootstrap 5 | 5.x | UI components, responsive layout | Project standard, consistent styling |
| System.Text.Json | Built-in | JSON serialization for ViewBag chart data | .NET built-in, consistent with Phase 2 pattern |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| LINQ | Built-in | Complex queries, aggregations, gap calculations | All data aggregation and filtering operations |
| ASP.NET Core MVC ViewModels | Pattern | Aggregating data from multiple entities | Complex views requiring assessment + competency + user data |
| Temporal Tables (SQL Server) | SQL Server 2016+ | Optional: Audit trail for competency level changes | Only if full historical tracking is required (can defer) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Join table pattern | JSON column storing competency IDs | Join table is more queryable, supports proper foreign keys, better for reporting |
| Server-side gap calculation | Client-side JavaScript calculation | Server-side is more secure, testable, cacheable, and consistent with MVC pattern |
| Chart.js radar chart | Custom SVG/Canvas | Chart.js is faster to implement, well-documented, already integrated in Phase 2 |
| Separate history table | Single table with version fields | History table provides cleaner separation, unlimited history, simpler queries |

**Installation:**
No new packages required - all libraries already in project.

## Architecture Patterns

### Recommended Project Structure
```
Models/
├── Competency/                    # New competency tracking models
│   ├── AssessmentCompetencyMap.cs    # Many-to-many join table
│   ├── UserCompetencyLevel.cs        # Individual competency achievement tracking
│   └── CompetencyGapViewModel.cs     # Gap analysis view data
├── KkjModels.cs                   # Existing - no changes needed
├── IdpItem.cs                     # Existing - potential enhancement for auto-suggestions
└── AssessmentSession.cs           # Existing - no schema changes needed

Controllers/
└── CMPController.cs               # Add CompetencyGap, UpdateCompetencyLevel actions

Views/CMP/
├── CompetencyGap.cshtml          # Gap analysis dashboard with radar chart
└── CompetencyTracking.cshtml     # Individual competency progress tracking

Data/
└── ApplicationDbContext.cs        # Add DbSets for new entities
```

### Pattern 1: Assessment-to-Competency Mapping (Many-to-Many)

**What:** Link assessments to the specific KKJ competencies they validate using a join table

**When to use:** Every assessment should map to one or more competencies to enable automatic level updates

**Example:**
```csharp
// Models/Competency/AssessmentCompetencyMap.cs
namespace HcPortal.Models.Competency;

public class AssessmentCompetencyMap
{
    public int Id { get; set; }

    // Foreign key to KkjMatrixItem (the competency being assessed)
    public int KkjMatrixItemId { get; set; }
    public KkjMatrixItem? KkjMatrixItem { get; set; }

    // Assessment category that validates this competency
    // E.g., "Assessment OJ", "IHT", "Licencor"
    public string AssessmentCategory { get; set; } = "";

    // Optional: specific assessment title pattern match
    // E.g., if Title contains "GSH Operation" → maps to specific GSH competencies
    public string? TitlePattern { get; set; }

    // Competency level achieved if assessment is passed
    // E.g., passing "Basic GSH Assessment" grants Level 1
    public int LevelGranted { get; set; }

    // Minimum score required (if different from assessment PassPercentage)
    // Nullable: if null, use assessment's PassPercentage
    public int? MinimumScoreRequired { get; set; }
}
```

**Configuration in ApplicationDbContext:**
```csharp
builder.Entity<AssessmentCompetencyMap>(entity =>
{
    entity.HasOne(m => m.KkjMatrixItem)
        .WithMany()
        .HasForeignKey(m => m.KkjMatrixItemId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(m => m.AssessmentCategory);
    entity.HasIndex(m => new { m.AssessmentCategory, m.TitlePattern });
});
```

### Pattern 2: User Competency Level Tracking with History

**What:** Track each user's current competency level for each KKJ skill, with temporal history for audit trail

**When to use:** Whenever an assessment is completed that maps to a competency, update or insert the user's level

**Example:**
```csharp
// Models/Competency/UserCompetencyLevel.cs
namespace HcPortal.Models.Competency;

public class UserCompetencyLevel
{
    public int Id { get; set; }

    // User who achieved this competency level
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    // Competency being tracked
    public int KkjMatrixItemId { get; set; }
    public KkjMatrixItem? KkjMatrixItem { get; set; }

    // Current achieved level (1-5 scale, or however KKJ defines it)
    public int CurrentLevel { get; set; }

    // Target level for this user's position (denormalized from KkjMatrixItem)
    // Copied at record creation based on user's Position field
    public int TargetLevel { get; set; }

    // How this level was achieved
    public string Source { get; set; } = ""; // "Assessment", "Manual", "Training"

    // If from assessment, store which one
    public int? AssessmentSessionId { get; set; }
    public AssessmentSession? AssessmentSession { get; set; }

    // Temporal tracking
    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; } // HC staff who manually adjusted

    // Computed property for gap
    public int Gap => TargetLevel - CurrentLevel;
}
```

**Key insight:** Store target level as denormalized field (copied from KkjMatrixItem at creation time based on user's Position). This prevents issues if position-based targets change later, and simplifies queries.

### Pattern 3: Gap Analysis ViewModel Pattern

**What:** Aggregate user competencies, current/target levels, and gap data into a single ViewModel for the view

**When to use:** CompetencyGap dashboard page that displays multiple competencies with visual gap analysis

**Example:**
```csharp
// Models/Competency/CompetencyGapViewModel.cs
namespace HcPortal.Models.Competency;

public class CompetencyGapViewModel
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? Position { get; set; }
    public string? Section { get; set; }

    // Competency items with gap data
    public List<CompetencyGapItem> Competencies { get; set; } = new();

    // Summary statistics
    public int TotalCompetencies { get; set; }
    public int CompetenciesMet { get; set; }      // CurrentLevel >= TargetLevel
    public int CompetenciesGapped { get; set; }   // CurrentLevel < TargetLevel
    public double OverallProgress { get; set; }   // Percentage of competencies met
}

public class CompetencyGapItem
{
    public int KkjMatrixItemId { get; set; }
    public string Kompetensi { get; set; } = "";
    public string SkillGroup { get; set; } = "";
    public int CurrentLevel { get; set; }
    public int TargetLevel { get; set; }
    public int Gap { get; set; }
    public string Status { get; set; } = ""; // "Met", "Gap", "Not Started"
    public DateTime? LastUpdated { get; set; }
    public string? LastAssessmentTitle { get; set; }

    // For IDP suggestions
    public bool HasIdpActivity { get; set; }
    public string? SuggestedAction { get; set; } // Auto-generated suggestion
}
```

**Controller pattern:**
```csharp
[Authorize]
public async Task<IActionResult> CompetencyGap(string? userId = null)
{
    // If HC viewing another user, use userId parameter; else use current user
    var targetUserId = userId ?? (await _userManager.GetUserAsync(User))?.Id ?? "";
    var targetUser = await _userManager.FindByIdAsync(targetUserId);

    if (targetUser == null) return NotFound();

    // Get target levels for user's position from KKJ matrix
    var positionTargets = GetTargetLevelsForPosition(targetUser.Position);

    // Get user's current competency levels
    var userCompetencies = await _context.UserCompetencyLevels
        .Include(c => c.KkjMatrixItem)
        .Include(c => c.AssessmentSession)
        .Where(c => c.UserId == targetUserId)
        .ToListAsync();

    // Build gap items by merging targets with current levels
    var gapItems = positionTargets.Select(target => {
        var current = userCompetencies.FirstOrDefault(c => c.KkjMatrixItemId == target.Id);
        return new CompetencyGapItem {
            KkjMatrixItemId = target.Id,
            Kompetensi = target.Kompetensi,
            SkillGroup = target.SkillGroup,
            CurrentLevel = current?.CurrentLevel ?? 0,
            TargetLevel = target.TargetLevelForPosition,
            Gap = (target.TargetLevelForPosition) - (current?.CurrentLevel ?? 0),
            Status = /* calculate status */,
            LastUpdated = current?.AchievedAt,
            LastAssessmentTitle = current?.AssessmentSession?.Title
        };
    }).ToList();

    var viewModel = new CompetencyGapViewModel { /* populate */ };
    return View(viewModel);
}
```

### Pattern 4: Automatic Competency Update on Assessment Completion

**What:** When user completes and passes an assessment, automatically update their competency levels based on mapping

**When to use:** Triggered in SubmitExam action after score calculation

**Example:**
```csharp
// In CMPController.SubmitExam, after setting IsPassed and CompletedAt:

if (session.IsPassed == true)
{
    // Find competencies mapped to this assessment
    var mappedCompetencies = await _context.AssessmentCompetencyMaps
        .Include(m => m.KkjMatrixItem)
        .Where(m => m.AssessmentCategory == session.Category &&
                    (m.TitlePattern == null || session.Title.Contains(m.TitlePattern)))
        .ToListAsync();

    foreach (var mapping in mappedCompetencies)
    {
        // Check if user already has a level for this competency
        var existingLevel = await _context.UserCompetencyLevels
            .FirstOrDefaultAsync(c => c.UserId == session.UserId &&
                                     c.KkjMatrixItemId == mapping.KkjMatrixItemId);

        if (existingLevel == null)
        {
            // Create new competency level record
            var targetLevel = GetTargetLevelForUserPosition(session.User.Position, mapping.KkjMatrixItem);
            _context.UserCompetencyLevels.Add(new UserCompetencyLevel {
                UserId = session.UserId,
                KkjMatrixItemId = mapping.KkjMatrixItemId,
                CurrentLevel = mapping.LevelGranted,
                TargetLevel = targetLevel,
                Source = "Assessment",
                AssessmentSessionId = session.Id,
                AchievedAt = DateTime.UtcNow
            });
        }
        else if (mapping.LevelGranted > existingLevel.CurrentLevel)
        {
            // Update to higher level (don't downgrade)
            existingLevel.CurrentLevel = mapping.LevelGranted;
            existingLevel.AssessmentSessionId = session.Id;
            existingLevel.UpdatedAt = DateTime.UtcNow;
        }
    }

    await _context.SaveChangesAsync();
}
```

### Pattern 5: Radar Chart Visualization (Chart.js)

**What:** Display gap analysis as a radar/spider chart showing current vs target levels across competencies

**When to use:** CompetencyGap view to provide visual representation of skill profile

**Example:**
```javascript
// In Views/CMP/CompetencyGap.cshtml
<canvas id="gapChart" height="400"></canvas>

<script>
    const competencies = @Html.Raw(Json.Serialize(Model.Competencies.Select(c => c.Kompetensi).Take(8)));
    const currentLevels = @Html.Raw(Json.Serialize(Model.Competencies.Select(c => c.CurrentLevel).Take(8)));
    const targetLevels = @Html.Raw(Json.Serialize(Model.Competencies.Select(c => c.TargetLevel).Take(8)));

    const ctx = document.getElementById('gapChart').getContext('2d');
    new Chart(ctx, {
        type: 'radar',
        data: {
            labels: competencies,
            datasets: [
                {
                    label: 'Current Level',
                    data: currentLevels,
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    pointBackgroundColor: 'rgb(75, 192, 192)'
                },
                {
                    label: 'Target Level',
                    data: targetLevels,
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.2)',
                    pointBackgroundColor: 'rgb(255, 99, 132)',
                    borderDash: [5, 5] // Dashed line for target
                }
            ]
        },
        options: {
            responsive: true,
            scales: {
                r: {
                    min: 0,
                    max: 5, // Adjust based on KKJ level scale
                    ticks: { stepSize: 1 }
                }
            }
        }
    });
</script>
```

**Source:** [Chart.js Radar Chart Documentation](https://www.chartjs.org/docs/latest/charts/radar.html), [Performance Analysis Using Radar Charts](https://guides.visual-paradigm.com/performance-analysis-for-business-improvement-using-radar-charts-to-identify-gaps-and-take-action/)

### Anti-Patterns to Avoid

- **Storing competency data in assessment results directly:** Don't add competency fields to AssessmentSession. Use proper join tables and separate tracking entities for maintainability and normalization.
- **Recalculating gaps on every page load:** Calculate once in controller, cache in ViewModel. Don't use computed properties that hit the database.
- **Downgrading competency levels:** If a user fails a re-assessment, don't automatically reduce their CurrentLevel. Competency progression should be monotonic (only increases), with manual HC override if needed.
- **Hardcoding position-to-target mappings:** The KkjMatrixItem model already has 15 position-specific target columns. Use reflection or a mapping dictionary to extract the correct target based on user's Position field, don't create switch statements.
- **Showing all competencies on radar chart:** Limit to 6-10 most relevant competencies for readability. Radar charts become cluttered with too many axes.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Many-to-many relationships | Manual join table management with SaveChanges logic | EF Core many-to-many with navigation collections | EF Core automatically handles insertions, deletions, and cascade behavior since version 5.0 |
| IDP suggestion generation | Complex rule engine or AI integration | Template-based string generation with competency data | Simple, predictable, maintainable - save AI for future enhancement |
| Competency level history | Multiple columns (Level_v1, Level_v2, etc.) | Separate history table with timestamp | Unlimited history, simple queries, standard audit pattern |
| Gap visualization | Custom SVG drawing or HTML tables with color coding | Chart.js radar chart | Well-tested, accessible, interactive, consistent with Phase 2 |
| Position-to-target mapping | Hardcoded switch statements | Reflection or dictionary lookup from KkjMatrixItem properties | KkjMatrixItem already has 15 target columns; use dynamic property access |

**Key insight:** This phase is primarily about data modeling and aggregation, not algorithmic complexity. Use established EF Core patterns, leverage existing libraries (Chart.js), and keep business logic simple. The value is in connecting existing systems, not building new frameworks.

## Common Pitfalls

### Pitfall 1: N+1 Query Problem in Gap Analysis

**What goes wrong:** Loading user competencies without Include() causes separate queries for each KkjMatrixItem and AssessmentSession.

**Why it happens:** EF Core lazy loading triggers database calls when navigating to related entities in a loop.

**How to avoid:** Always use `.Include(c => c.KkjMatrixItem).Include(c => c.AssessmentSession)` when loading UserCompetencyLevels. Consider using `.AsSplitQuery()` if Include generates Cartesian explosion.

**Warning signs:** Slow page load, database profiler shows hundreds of SELECT queries for a single page.

### Pitfall 2: Position Field Mismatch Between User and KkjMatrixItem

**What goes wrong:** User.Position is "Sr Supervisor GSH" but KkjMatrixItem has column "Target_SrSpv_GSH" - string matching fails, no target level found.

**Why it happens:** Inconsistent naming conventions between user data and KkjMatrixItem column names.

**How to avoid:** Create a static mapping dictionary that normalizes position names to KkjMatrixItem column names. Handle variations and null positions gracefully with default targets.

**Example solution:**
```csharp
private static readonly Dictionary<string, string> PositionColumnMap = new()
{
    { "Section Head", "Target_SectionHead" },
    { "Sr Supervisor GSH", "Target_SrSpv_GSH" },
    { "Shift Supervisor GSH", "Target_ShiftSpv_GSH" },
    // ... etc
};

private int GetTargetLevel(KkjMatrixItem competency, string? userPosition)
{
    if (string.IsNullOrEmpty(userPosition) ||
        !PositionColumnMap.TryGetValue(userPosition, out var columnName))
    {
        return 1; // Default target
    }

    var property = typeof(KkjMatrixItem).GetProperty(columnName);
    var value = property?.GetValue(competency)?.ToString();
    return int.TryParse(value?.Replace("-", "0"), out var level) ? level : 0;
}
```

### Pitfall 3: Assessment Category is Too Broad for Competency Mapping

**What goes wrong:** All "Assessment OJ" assessments map to all OJ competencies, even if a specific assessment only covers subset.

**Why it happens:** Using only Category for mapping without considering Title or specific competency indicators.

**How to avoid:** Add optional TitlePattern field to AssessmentCompetencyMap. Use pattern matching (Contains, Regex) to narrow down mappings. Allow HC to configure these mappings via UI in future.

**Warning signs:** User takes one basic assessment and suddenly shows proficiency in 20 unrelated competencies.

### Pitfall 4: IDP Auto-Suggestions Become Noise

**What goes wrong:** System generates dozens of IDP suggestions for every gap, overwhelming users with generic recommendations.

**Why it happens:** Creating suggestions for ALL competency gaps without prioritization or filtering.

**How to avoid:**
- Only suggest IDPs for top 3-5 priority gaps (largest gap, critical competencies, or role-specific)
- Check if user already has an active IDP item for that competency before suggesting
- Use CPDP framework's "Silabus" and "IndikatorPerilaku" fields to generate specific, actionable suggestions
- Allow users to dismiss suggestions

**Warning signs:** User has 40 suggested IDP items, engagement drops, features ignored.

### Pitfall 5: Competency Levels Use Different Scales

**What goes wrong:** KKJ matrix uses 1-5 scale, assessments grant levels 1-3, radar chart displays incorrectly.

**Why it happens:** No standardization of competency level definitions across the system.

**How to avoid:**
- Document the official competency level scale (1-5? 0-4? Beginner/Intermediate/Advanced?)
- Validate all LevelGranted values against this scale in AssessmentCompetencyMap
- Add database check constraint: `CHECK (CurrentLevel >= 0 AND CurrentLevel <= 5)`
- Display level labels on charts, not just numbers

**Warning signs:** Radar chart shows impossible values, users confused about what "Level 3" means.

## Code Examples

Verified patterns from research and existing codebase:

### 1. Many-to-Many Relationship Configuration (EF Core 8)

```csharp
// In ApplicationDbContext.OnModelCreating
builder.Entity<AssessmentCompetencyMap>(entity =>
{
    entity.ToTable("AssessmentCompetencyMaps");

    entity.HasOne(m => m.KkjMatrixItem)
        .WithMany()
        .HasForeignKey(m => m.KkjMatrixItemId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(m => m.AssessmentCategory);
    entity.HasIndex(m => new { m.AssessmentCategory, m.TitlePattern });
});

builder.Entity<UserCompetencyLevel>(entity =>
{
    entity.ToTable("UserCompetencyLevels");

    entity.HasOne(c => c.User)
        .WithMany()
        .HasForeignKey(c => c.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(c => c.KkjMatrixItem)
        .WithMany()
        .HasForeignKey(c => c.KkjMatrixItemId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(c => c.AssessmentSession)
        .WithMany()
        .HasForeignKey(c => c.AssessmentSessionId)
        .OnDelete(DeleteBehavior.SetNull);

    // Unique constraint: one current level per user per competency
    entity.HasIndex(c => new { c.UserId, c.KkjMatrixItemId });

    // Check constraints for data integrity
    entity.HasCheckConstraint("CK_UserCompetencyLevel_CurrentLevel",
        "[CurrentLevel] >= 0 AND [CurrentLevel] <= 5");
    entity.HasCheckConstraint("CK_UserCompetencyLevel_TargetLevel",
        "[TargetLevel] >= 0 AND [TargetLevel] <= 5");
});
```

**Source:** [EF Core Many-to-Many Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many), [EF Core Relationships Tutorial](https://dotnettutorials.net/lesson/many-to-many-relationships-in-entity-framework-core/)

### 2. Gap Analysis LINQ Query with Aggregation

```csharp
// Controller action to calculate gap statistics
public async Task<IActionResult> CompetencyGap(string? userId = null)
{
    var targetUserId = userId ?? (await _userManager.GetUserAsync(User))?.Id ?? "";
    var targetUser = await _userManager.FindByIdAsync(targetUserId);

    if (targetUser == null) return NotFound();

    // Get all competencies for user's position (filtered from KKJ matrix)
    var relevantCompetencies = await GetRelevantCompetenciesForPosition(targetUser.Position);

    // Get user's current levels with assessment history
    var userLevels = await _context.UserCompetencyLevels
        .Include(c => c.KkjMatrixItem)
        .Include(c => c.AssessmentSession)
        .Where(c => c.UserId == targetUserId)
        .ToListAsync();

    // Build gap items by left-joining competencies with user levels
    var gapItems = relevantCompetencies.Select(comp =>
    {
        var userLevel = userLevels.FirstOrDefault(ul => ul.KkjMatrixItemId == comp.Id);
        int current = userLevel?.CurrentLevel ?? 0;
        int target = GetTargetLevelForPosition(comp, targetUser.Position);
        int gap = target - current;

        return new CompetencyGapItem
        {
            KkjMatrixItemId = comp.Id,
            Kompetensi = comp.Kompetensi,
            SkillGroup = comp.SkillGroup,
            CurrentLevel = current,
            TargetLevel = target,
            Gap = gap,
            Status = gap <= 0 ? "Met" : (current == 0 ? "Not Started" : "Gap"),
            LastUpdated = userLevel?.AchievedAt,
            LastAssessmentTitle = userLevel?.AssessmentSession?.Title,
            HasIdpActivity = /* check IDP items */,
            SuggestedAction = gap > 0 ? GenerateSuggestion(comp, gap) : null
        };
    })
    .OrderByDescending(g => g.Gap) // Prioritize largest gaps
    .ToList();

    var viewModel = new CompetencyGapViewModel
    {
        UserId = targetUserId,
        UserName = targetUser.FullName,
        Position = targetUser.Position,
        Section = targetUser.Section,
        Competencies = gapItems,
        TotalCompetencies = gapItems.Count,
        CompetenciesMet = gapItems.Count(c => c.Status == "Met"),
        CompetenciesGapped = gapItems.Count(c => c.Status == "Gap" || c.Status == "Not Started"),
        OverallProgress = gapItems.Count > 0
            ? Math.Round(gapItems.Count(c => c.Status == "Met") * 100.0 / gapItems.Count, 1)
            : 0
    };

    return View(viewModel);
}
```

**Source:** Adapted from Phase 2 ReportsIndex pattern in `02-01-PLAN.md`

### 3. Automatic IDP Suggestion Generation

```csharp
private string GenerateSuggestion(KkjMatrixItem competency, int gap)
{
    // Look up CPDP framework for this competency
    var cpdpItem = _context.CpdpItems
        .FirstOrDefault(c => c.NamaKompetensi == competency.Kompetensi);

    if (cpdpItem != null && !string.IsNullOrEmpty(cpdpItem.Silabus))
    {
        // Use CPDP syllabus and deliverables to generate specific suggestion
        return $"Complete training: {cpdpItem.Silabus}. Target: {cpdpItem.TargetDeliverable}";
    }

    // Fallback: generic suggestion based on gap size
    if (gap >= 3)
    {
        return $"Large gap detected. Recommend structured training program for {competency.Kompetensi}.";
    }
    else if (gap == 2)
    {
        return $"Consider intermediate-level assessment or on-the-job training for {competency.Kompetensi}.";
    }
    else
    {
        return $"Schedule next-level assessment to advance in {competency.Kompetensi}.";
    }
}
```

**Source:** Inspired by IDP automation research from [Sprad IDP Templates](https://sprad.io/content/free-idp-template-excel-with-smart-goals-skills-assessment-individual-development-plan)

### 4. Chart.js Radar Chart for Gap Visualization

```html
<!-- In Views/CMP/CompetencyGap.cshtml -->
<div class="card border-0 shadow-sm">
    <div class="card-header bg-white py-3">
        <h6 class="fw-bold mb-0">
            <i class="bi bi-radar me-2 text-primary"></i>Competency Profile
        </h6>
    </div>
    <div class="card-body">
        <canvas id="competencyRadarChart" height="400"></canvas>
    </div>
</div>

<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<script>
    // Serialize data from ViewModel (limit to top 8 competencies for readability)
    const competencyData = {
        labels: @Html.Raw(Json.Serialize(Model.Competencies.Take(8).Select(c => c.Kompetensi))),
        current: @Html.Raw(Json.Serialize(Model.Competencies.Take(8).Select(c => c.CurrentLevel))),
        target: @Html.Raw(Json.Serialize(Model.Competencies.Take(8).Select(c => c.TargetLevel)))
    };

    const ctx = document.getElementById('competencyRadarChart').getContext('2d');
    new Chart(ctx, {
        type: 'radar',
        data: {
            labels: competencyData.labels,
            datasets: [
                {
                    label: 'Current Level',
                    data: competencyData.current,
                    borderColor: 'rgb(54, 162, 235)',
                    backgroundColor: 'rgba(54, 162, 235, 0.2)',
                    pointBackgroundColor: 'rgb(54, 162, 235)',
                    pointBorderColor: '#fff',
                    pointHoverBackgroundColor: '#fff',
                    pointHoverBorderColor: 'rgb(54, 162, 235)'
                },
                {
                    label: 'Target Level',
                    data: competencyData.target,
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.1)',
                    pointBackgroundColor: 'rgb(255, 99, 132)',
                    pointBorderColor: '#fff',
                    pointHoverBackgroundColor: '#fff',
                    pointHoverBorderColor: 'rgb(255, 99, 132)',
                    borderDash: [5, 5] // Dashed line for target
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                r: {
                    min: 0,
                    max: 5,
                    ticks: {
                        stepSize: 1,
                        callback: function(value) {
                            // Optional: Map numbers to labels
                            const labels = ['None', 'Basic', 'Intermediate', 'Advanced', 'Expert', 'Master'];
                            return labels[value] || value;
                        }
                    }
                }
            },
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + context.parsed.r;
                        }
                    }
                }
            }
        }
    });
</script>
```

**Source:** [Chart.js Radar Chart Official Docs](https://www.chartjs.org/docs/latest/charts/radar.html), adapted from Phase 2 Chart.js pattern in `02-03-PLAN.md`

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual competency tracking in spreadsheets | Database-backed competency levels with automatic updates | Modern HR systems (2020s) | Real-time tracking, reduced manual errors, audit trail |
| Generic IDP templates | Competency gap-driven personalized suggestions | Skills-based learning platforms (2023+) | Higher engagement, targeted development |
| Static competency matrices | Dynamic tracking with temporal history | Modern LMS integrations (2022+) | Longitudinal analysis, progress visibility |
| Separate assessment and competency systems | Integrated systems with automatic mapping | Competency-based assessment movement (2024+) | Reduced administrative burden, immediate feedback |

**Deprecated/outdated:**
- **Storing assessment scores without competency linkage:** Modern systems map every assessment to specific competencies for automatic skills tracking
- **One-size-fits-all IDP templates:** Current best practice is personalized, gap-driven development planning
- **Manual competency level updates by HC:** Automation from assessment results is now standard, with manual override as exception

**Source:** [Competency-Based Learning Guide 2025](https://www.verifyed.io/blog/competency-learning-assessment-guide), [Skills Matrix Implementation 2024](https://medium.com/@nydas/implementing-a-skills-matrix-across-data-teams-58675755e20d)

## Open Questions

1. **What competency level scale does KKJ matrix use?**
   - What we know: KkjMatrixItem has 15 target columns with values like "1", "2", "3", "-"
   - What's unclear: Maximum level (3? 5?), whether "-" means "not applicable" or "level 0"
   - Recommendation: Review KKJ seed data or consult domain expert. Assume 0-5 scale for now (0 = no competency, 5 = master). Treat "-" as 0 in calculations.

2. **How granular should assessment-to-competency mapping be?**
   - What we know: Assessments have Category and Title fields
   - What's unclear: Should one assessment map to 1 competency (1:1), several (1:many), or should it be many:many?
   - Recommendation: Use many:many with AssessmentCompetencyMap. One assessment can validate multiple competencies (realistic), and one competency can be validated by multiple assessments (redundancy). Start with Category-level mapping, add TitlePattern for specificity.

3. **Should competency levels be versioned/historical or single current value?**
   - What we know: Competency levels should increase over time as users complete assessments
   - What's unclear: Do we need full history of all level changes, or just current level + last updated timestamp?
   - Recommendation: Start with single current value approach (UserCompetencyLevel table with UpdatedAt). If audit requirements emerge, add separate CompetencyLevelHistory table later. Don't premature-optimize for full temporal tracking.

4. **Who can manually adjust competency levels (override assessment results)?**
   - What we know: HC role has elevated privileges, assessments auto-update levels
   - What's unclear: Can HC manually grant/revoke competency levels? Should this be logged?
   - Recommendation: Allow HC to manually update levels via a dedicated UI (e.g., "Grant Competency" button), with UpdatedBy field tracking who made the change and Source = "Manual". Add this in a follow-up enhancement, not initial implementation.

5. **How to handle competency expiration/recertification?**
   - What we know: Training records have expiry tracking
   - What's unclear: Do competency levels expire? Do users need recertification assessments?
   - Recommendation: Not in scope for Phase 3 initial implementation. Add ExpiresAt field to UserCompetencyLevel in future if business requires it. For now, competency levels are permanent unless manually changed.

## Sources

### Primary (HIGH confidence)
- [EF Core Relationships Documentation](https://learn.microsoft.com/en-us/ef/core/modeling/relationships) - Many-to-many patterns, relationship configuration
- [EF Core Many-to-Many Guide](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many) - Join table implementation
- [Chart.js Radar Chart Docs](https://www.chartjs.org/docs/latest/charts/radar.html) - Radar chart configuration and options
- Existing codebase: `Models/AssessmentSession.cs`, `Models/KkjModels.cs`, `Data/ApplicationDbContext.cs` - Schema and patterns already in use

### Secondary (MEDIUM confidence)
- [ASP.NET MVC ViewModel Patterns](https://dotnettutorials.net/lesson/view-model-asp-net-core-mvc/) - Composite ViewModel architecture
- [10 ASP.NET MVC ViewModel Patterns](https://codejack.com/2024/10/10-aspnet-mvc-viewmodel-patterns-and-examples/) - Flattened ViewModels, data aggregation
- [Skills Matrix Implementation Guide](https://medium.com/@nydas/implementing-a-skills-matrix-across-data-teams-58675755e20d) - Competency tracking database design
- [Competency-Based Assessment 2025](https://www.verifyed.io/blog/competency-learning-assessment-guide) - Modern competency tracking approaches
- [Audit Trail Database Patterns](https://medium.com/techtofreedom/4-common-designs-of-audit-trail-tracking-data-changes-in-databases-c894b7bb6d18) - Temporal data tracking
- [How to Create Skills Matrix](https://www.aihr.com/blog/create-skills-matrix-competency-matrix/) - Competency matrix best practices
- [Radar Chart for Gap Analysis](https://guides.visual-paradigm.com/performance-analysis-for-business-improvement-using-radar-charts-to-identify-gaps-and-take-action/) - Visualization patterns
- [IDP Automation in Enterprise Systems](https://www.archives.gov/files/careers/competencies/e-idp.pdf) - Government IDP automation patterns

### Tertiary (LOW confidence)
- [Competency Mapping Tools 2026](https://blog.imocha.io/competency-mapping-tools) - Commercial solutions overview
- [Skills Base Competency Software](https://www.skills-base.com/competency-skill-mapping-software) - Enterprise competency management features
- [Stimulsoft ASP.NET Dashboards](https://www.stimulsoft.com/en/products/dashboards-web) - Dashboard visualization options

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries already in project, EF Core many-to-many is well-documented
- Architecture: MEDIUM-HIGH - Patterns based on established EF Core and MVC best practices, but domain-specific mapping requires validation
- Pitfalls: MEDIUM - Based on common EF Core issues and competency system research, but not project-specific testing

**Research date:** 2026-02-14
**Valid until:** ~30-45 days (stable domain - EF Core, Chart.js, MVC patterns don't change rapidly)

**Key assumptions made:**
- KKJ competency levels use 0-5 integer scale
- Assessment-to-competency mapping is many-to-many
- User positions in ApplicationUser.Position match KkjMatrixItem target column naming
- No competency expiration/recertification requirements in initial phase
- HC staff will manually configure assessment-to-competency mappings (no auto-detection)
