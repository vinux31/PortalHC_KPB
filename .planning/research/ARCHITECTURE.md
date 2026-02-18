# Architecture Research

**Domain:** ASP.NET Core 8 MVC — v1.2 UX Consolidation (Portal HC KPB)
**Researched:** 2026-02-18
**Confidence:** HIGH — based on direct codebase inspection

---

## System Overview

```
Browser (Razor + Bootstrap 5 + Chart.js)
    |
    v
ASP.NET Core 8 MVC  [Authorize] on all controllers
    |
    +-- CMPController  (~1840 lines)
    |       Assessment, Records, ReportsIndex, CompetencyGap, CpdpProgress,
    |       CreateAssessment, StartExam, SubmitExam, Certificate, Results,
    |       ManageQuestions, EditAssessment, DeleteAssessment, ExportResults,
    |       UserAssessmentHistory, SearchUsers
    |
    +-- CDPController  (~1475 lines)
    |       Dashboard, DevDashboard, Coaching, CreateSession, AddActionItem,
    |       PlanIdp, Progress, ProtonMain, AssignTrack, Deliverable,
    |       ApproveDeliverable, RejectDeliverable, UploadEvidence,
    |       HCApprovals, CreateFinalAssessment, HCReviewDeliverable
    |
    +-- HomeController, AccountController, BPController
    |
    v
ApplicationDbContext  (EF Core + SQL Server)
    |
    +-- AssessmentSessions, AssessmentQuestions, AssessmentOptions, UserResponses
    +-- TrainingRecords
    +-- IdpItems
    +-- KkjMatrices, CpdpItems, AssessmentCompetencyMaps, UserCompetencyLevels
    +-- ProtonTrackAssignments, ProtonDeliverableProgresses,
    |   ProtonKompetensiList, ProtonFinalAssessments, ProtonNotifications
    +-- CoachingSessions, ActionItems
    +-- AspNetUsers (ApplicationUser: FullName, NIP, Section, Unit, RoleLevel, SelectedView)
```

### Component Responsibilities

| Component | Responsibility | Key Notes |
|-----------|---------------|-----------|
| CMPController | Assessment lifecycle, TrainingRecords, HC Reports, CompetencyGap | 1840 lines; CompetencyGap being deleted |
| CDPController | CDP Dashboard, Dev Dashboard, Coaching, Proton workflow | 1475 lines; absorbs HC Reports and Dev Dashboard |
| ApplicationUser.SelectedView | Admin role-switching (HC/Atasan/Coach/Coachee) | Persisted to DB per user |
| ApplicationUser.RoleLevel | Integer hierarchy 1=Admin … 6=Coachee | Drives scope logic in DevDashboard |
| DashboardViewModel | Current CDP Dashboard model | Replaced by CDPDashboardViewModel |
| ReportsDashboardViewModel | HC Reports model — paginated assessments, category stats | Currently lives in CMPController.ReportsIndex; absorbed into Dashboard |
| DevDashboardViewModel | Dev Dashboard — Proton deliverable progress per coachee | Absorbed into Dashboard as sub-model |

---

## Scope of v1.2 Changes

### Component Classification

#### DELETED

| Component | File | Verification before delete |
|-----------|------|---------------------------|
| `CMPController.CompetencyGap()` action | CMPController.cs lines 1533-1632 | No other action calls it |
| `CMPController.GenerateIdpSuggestion()` helper | CMPController.cs lines 1815-1837 | Only called from CompetencyGap() |
| `Views/CMP/CompetencyGap.cshtml` | Views/CMP/ | Delete file |
| `CompetencyGapViewModel` / `CompetencyGapItem` classes | Models/Competency/CompetencyGapViewModel.cs | Grep for usages first; no other consumers expected |
| Any nav link / button pointing to `CMP/CompetencyGap` | Views/CMP/Index.cshtml, _Layout.cshtml | Layout has no current link; check CMP/Index.cshtml |

What is NOT deleted: `UserCompetencyLevels` DB table, `AssessmentCompetencyMaps` table, `KkjMatrices` table, `CpdpProgress()` action — all still used by other features.

#### MODIFIED

| Component | What Changes |
|-----------|-------------|
| `CMPController.Assessment()` | Add status filter for "personal" branch (Open/Upcoming only). Add "monitor" as a valid third view value for HC/Admin |
| `Views/CMP/Assessment.cshtml` | Add monitor tab/toggle (only rendered when canManage == true). Add callout on personal view linking to Records for completed assessments |
| `CMPController.Records()` | Replace `GetPersonalTrainingRecords()` for Coachee path with `BuildUnifiedRecords()`. Supervisor/HC paths unchanged |
| `CMPController.WorkerDetail()` | Optionally update to also use unified records |
| `Views/CMP/Records.cshtml` | Change `@model List<TrainingRecord>` to `@model List<UnifiedCapabilityRecord>`. Add Type column, conditional Score/Certificate columns |
| `Views/CMP/WorkerDetail.cshtml` | Same model change as Records.cshtml if WorkerDetail calls the same helper |
| `CDPController.Dashboard()` | Absorb ReportsIndex query logic and DevDashboard query logic. Build CDPDashboardViewModel instead of DashboardViewModel |
| `Views/CDP/Dashboard.cshtml` | Add Bootstrap tab nav with three tab panes: Overview, HC Reports, Dev Dashboard |
| `Views/Shared/_Layout.cshtml` | Remove standalone "Dev Dashboard" nav entry (line 70) after Dashboard tab is verified |

#### NEW

| Component | Purpose | File |
|-----------|---------|------|
| `UnifiedCapabilityRecord` ViewModel | UNION DTO merging AssessmentSession and TrainingRecord rows | Models/UnifiedCapabilityRecord.cs |
| `CDPDashboardViewModel` | Composite model for unified Dashboard: IDP stats + HcReports sub-model + DevDashboard sub-model | Models/CDPDashboardViewModel.cs |
| `Views/CDP/_HCReportsPartial.cshtml` | Partial view for HC Reports tab content | Views/CDP/_HCReportsPartial.cshtml |
| `Views/CDP/_DevDashboardPartial.cshtml` | Partial view for Dev Dashboard tab content | Views/CDP/_DevDashboardPartial.cshtml |

---

## Integration Points: Detailed

### 1. Assessment Page — Status Filter + HC Monitor View

**Current query:**
```
CMPController.Assessment(view="personal")
    -> WHERE UserId == me   (all statuses)
CMPController.Assessment(view="manage")
    -> WHERE (no filter)    (HC/Admin — all users, all statuses)
```

**New query:**
```
view="personal"
    -> WHERE UserId == me AND Status IN ("Open", "Upcoming")
       (completed excluded — they appear in Records instead)

view="manage"
    -> unchanged (Admin/HC full control — all statuses, all users)

view="monitor"    [new, HC/Admin only]
    -> WHERE Status IN ("Open", "Upcoming")
       system-wide across all users
       include User navigation property for name/section display
```

**What touches it:**
- `CMPController.Assessment()` — add `view == "monitor"` branch; add Status filter on "personal" branch
- `Views/CMP/Assessment.cshtml` — add third toggle button (monitor); gate it on `canManage == true`; add info callout on personal view

**Risk:** Workers who previously found completed assessments here will find an empty area. The callout linking to Records.cshtml is required.

---

### 2. Training Records — Unified UNION ViewModel

**Current:**
```csharp
// Records.cshtml
@model List<HcPortal.Models.TrainingRecord>

// Controller (Coachee path)
var trainingRecords = GetPersonalTrainingRecords(userId);
return View("Records", trainingRecords);
```

**New ViewModel shape:**
```csharp
public class UnifiedCapabilityRecord
{
    public int      Id            { get; set; }
    public string   RecordType    { get; set; }  // "Assessment" | "Training"
    public string   Title         { get; set; }  // Session.Title or TrainingRecord.Judul
    public string   Category      { get; set; }  // Session.Category or TrainingRecord.Kategori
    public DateTime Date          { get; set; }  // Session.CompletedAt or TrainingRecord.Tanggal
    public string   Status        { get; set; }  // "Passed"/"Failed" or original Training status
    public int?     Score         { get; set; }  // Assessment only; null for Training rows
    public bool?    IsPassed      { get; set; }  // Assessment only; null for Training rows
    public string?  SertifikatUrl { get; set; }  // Training only; null for Assessment rows
    public DateTime? ValidUntil   { get; set; }  // Training only; null for Assessment rows
    public int?     AssessmentId  { get; set; }  // Set for Assessment rows — enables link to Results/Certificate
}
```

**Query pattern for BuildUnifiedRecords(userId):**
```csharp
// Fetch completed+passed assessments
var assessmentRows = await _context.AssessmentSessions
    .Where(a => a.UserId == userId && a.Status == "Completed" && a.IsPassed == true)
    .Select(a => new UnifiedCapabilityRecord
    {
        Id = a.Id, RecordType = "Assessment",
        Title = a.Title, Category = a.Category,
        Date = a.CompletedAt ?? a.Schedule,
        Status = "Passed", Score = a.Score,
        IsPassed = a.IsPassed, AssessmentId = a.Id
    }).ToListAsync();

// Fetch training records
var trainingRows = await _context.TrainingRecords
    .Where(t => t.UserId == userId)
    .Select(t => new UnifiedCapabilityRecord
    {
        Id = t.Id, RecordType = "Training",
        Title = t.Judul ?? "", Category = t.Kategori ?? "",
        Date = t.Tanggal, Status = t.Status ?? "",
        SertifikatUrl = t.SertifikatUrl, ValidUntil = t.ValidUntil
    }).ToListAsync();

return assessmentRows.Concat(trainingRows)
    .OrderByDescending(r => r.Date)
    .ToList();
```

**Column rendering matrix:**

| Column | Assessment row | Training row |
|--------|---------------|-------------|
| Type badge | "Assessment" (blue) | "Training" (green) |
| Title | Session.Title | TrainingRecord.Judul |
| Category | Session.Category | TrainingRecord.Kategori |
| Date | CompletedAt | Tanggal |
| Score | Shown if non-null | Hidden |
| Pass/Fail badge | Shown | Hidden |
| Certificate link | Link to CMP/Certificate/{AssessmentId} | SertifikatUrl anchor |
| Valid Until | Hidden | Shown if non-null |

**What touches it:**
- `Models/UnifiedCapabilityRecord.cs` — create
- `CMPController.Records()` — replace Coachee path; supervisor/HC paths unchanged
- `Views/CMP/Records.cshtml` — change `@model`, add Type column, conditional columns
- `Views/CMP/WorkerDetail.cshtml` — same model change

---

### 3. Dashboard Consolidation

**Current state:**

| URL | Action | ViewModel |
|-----|--------|-----------|
| /CDP/Dashboard | CDPController.Dashboard() | DashboardViewModel |
| /CDP/DevDashboard | CDPController.DevDashboard() | DevDashboardViewModel |
| /CMP/ReportsIndex | CMPController.ReportsIndex() | ReportsDashboardViewModel |

**Target state:**

| URL | Action | ViewModel |
|-----|--------|-----------|
| /CDP/Dashboard | CDPController.Dashboard() | CDPDashboardViewModel |
| /CDP/DevDashboard | (keep alive as redirect or standalone) | — |
| /CMP/ReportsIndex | (keep alive as redirect or standalone) | — |

**CDPDashboardViewModel shape:**
```csharp
public class CDPDashboardViewModel
{
    // Tab 1 — Overview (from existing DashboardViewModel)
    public int TotalIdp { get; set; }
    public int IdpGrowth { get; set; }
    public int CompletionRate { get; set; }
    public string CompletionTarget { get; set; } = "";
    public int PendingAssessments { get; set; }
    public int BudgetUsedPercent { get; set; }
    public string BudgetUsedText { get; set; } = "";
    public List<string> ChartLabels { get; set; } = new();
    public List<int> ChartTarget { get; set; } = new();
    public List<int> ChartRealization { get; set; } = new();
    public List<UnitCompliance> TopUnits { get; set; } = new();
    public int TotalCompletedAssessments { get; set; }
    public double OverallPassRate { get; set; }
    public int TotalUsersAssessed { get; set; }

    // Tab 2 — HC Reports (sub-model from ReportsDashboardViewModel)
    public ReportsDashboardViewModel HcReports { get; set; } = new();
    public List<CategoryStatistic> CategoryStats { get; set; } = new();
    public List<int> ScoreDistribution { get; set; } = new();

    // Tab 3 — Dev Dashboard (sub-model from DevDashboardViewModel)
    public DevDashboardViewModel DevDashboard { get; set; } = new();
}
```

**Role gating for sub-model population:**
```csharp
// Only populate HC Reports and Dev Dashboard for roles that will see those tabs
bool showHcReports = userRole == UserRoles.HC ||
                     userRole == UserRoles.Admin ||
                     userRole == UserRoles.SectionHead ||
                     userRole == UserRoles.SrSupervisor;

bool showDevDashboard = userRole == UserRoles.HC ||
                        userRole == UserRoles.Admin ||
                        userRole == UserRoles.Coach ||
                        userRole == UserRoles.SectionHead ||
                        userRole == UserRoles.SrSupervisor;

if (showHcReports) { /* run ReportsIndex query */ }
if (showDevDashboard) { /* run DevDashboard query */ }
```

**Partial view approach:**
- `Views/CDP/_HCReportsPartial.cshtml` — accepts `@model ReportsDashboardViewModel`. Contains the table, filters, category charts from CMP/ReportsIndex.cshtml. Rendered inside Dashboard Tab 2 via `<partial name="_HCReportsPartial" model="Model.HcReports" />`.
- `Views/CDP/_DevDashboardPartial.cshtml` — accepts `@model DevDashboardViewModel`. Contains coachee rows table and doughnut/trend charts from CDP/DevDashboard.cshtml. Rendered inside Dashboard Tab 3 via `<partial name="_DevDashboardPartial" model="Model.DevDashboard" />`.

**Tab structure for Dashboard.cshtml:**
```html
<ul class="nav nav-tabs" id="dashboardTabs">
    <li class="nav-item"><a class="nav-link active" data-bs-target="#overview">Overview</a></li>
    @if (canSeeHcReports)
    {
        <li class="nav-item"><a class="nav-link" data-bs-target="#hc-reports">HC Reports</a></li>
    }
    @if (canSeeDevDashboard)
    {
        <li class="nav-item"><a class="nav-link" data-bs-target="#dev-dashboard">Dev Dashboard</a></li>
    }
</ul>
<div class="tab-content">
    <div class="tab-pane active" id="overview"><!-- existing cards/charts --></div>
    <div class="tab-pane" id="hc-reports">
        <partial name="_HCReportsPartial" model="Model.HcReports" />
    </div>
    <div class="tab-pane" id="dev-dashboard">
        <partial name="_DevDashboardPartial" model="Model.DevDashboard" />
    </div>
</div>
```

---

### 4. Gap Analysis Removal — Checklist

| Step | File / Location | Action |
|------|----------------|--------|
| 1 | CMPController.cs line 1533 | Delete `CompetencyGap()` action |
| 2 | CMPController.cs line 1815 | Delete `GenerateIdpSuggestion()` private helper |
| 3 | Views/CMP/CompetencyGap.cshtml | Delete file |
| 4 | Models/Competency/CompetencyGapViewModel.cs | Grep for `CompetencyGapViewModel` and `CompetencyGapItem` first; delete file if zero hits |
| 5 | Views/CMP/Index.cshtml | Remove any link with `asp-action="CompetencyGap"` |
| 6 | Views/Shared/_Layout.cshtml | Confirm no CompetencyGap link (currently none in layout) |
| 7 | Build | Verify no compile errors |

What survives: `UserCompetencyLevels` DbSet, `AssessmentCompetencyMaps` DbSet, `UserCompetencyLevel` model, `AssessmentCompetencyMap` model, `PositionTargetHelper`, `CpdpProgress()` action, `KkjMatrices` DbSet — all used by SubmitExam and CpdpProgress.

---

## Data Flow Changes

### Before v1.2

```
Worker -> Records.cshtml
    <- List<TrainingRecord> (training only)

HC -> CMP/ReportsIndex
    <- ReportsDashboardViewModel

Supervisor -> CDP/DevDashboard
    <- DevDashboardViewModel

Worker -> CMP/Assessment?view=personal
    <- all statuses including Completed
```

### After v1.2

```
Worker -> Records.cshtml
    <- List<UnifiedCapabilityRecord>
       (completed+passed AssessmentSessions UNION all TrainingRecords)
       sorted by date desc

HC/Admin -> CDP/Dashboard
    <- CDPDashboardViewModel
       Tab 1: IDP overview + assessment widget (existing)
       Tab 2: HC Reports (ReportsDashboardViewModel as sub-model)
       Tab 3: Dev Dashboard (DevDashboardViewModel as sub-model)

Worker -> CMP/Assessment?view=personal
    <- Open/Upcoming only (Completed no longer shown; callout links to Records)

HC -> CMP/Assessment?view=monitor  [new]
    <- system-wide Open/Upcoming assessments across all users
```

---

## Recommended Build Order

### Step 1 — Gap Analysis Removal (no dependencies, pure deletion)

Do this first. Removes dead code and reduces noise during later changes.

1. Delete `CompetencyGap()` action and `GenerateIdpSuggestion()` helper from CMPController.cs
2. Delete `Views/CMP/CompetencyGap.cshtml`
3. Grep solution for `CompetencyGapViewModel` — delete `Models/Competency/CompetencyGapViewModel.cs` when clean
4. Remove any nav links pointing to CompetencyGap
5. Build and confirm no errors

**Dependency:** None. Safe to do in isolation.

---

### Step 2 — UnifiedCapabilityRecord + Records view refactor

1. Create `Models/UnifiedCapabilityRecord.cs`
2. Add `BuildUnifiedRecords(string userId)` private method to CMPController (alongside existing `GetPersonalTrainingRecords`)
3. Update `CMPController.Records()` Coachee branch to call `BuildUnifiedRecords`
4. Update `Views/CMP/Records.cshtml` — change `@model`, add Type column, conditional Score/Certificate columns
5. Update `Views/CMP/WorkerDetail.cshtml` if it renders the same data shape

**Dependency:** Step 1 must be done first (clean compile baseline). Step 2 is self-contained thereafter.

**Breaking point:** The `@model` directive change in Records.cshtml is a compile-time break until the controller change and view change land together. Make both changes in the same edit session.

---

### Step 3 — Assessment page status filter + monitor view

1. Extend `CMPController.Assessment()` — add status filter clause for `view="personal"`; add `view="monitor"` branch
2. Update `Views/CMP/Assessment.cshtml` — add monitor toggle button (gated on `canManage`); add callout for personal view

**Dependency:** None (independent of Steps 2 and 4). Can run in parallel with Step 2 if two developers.

---

### Step 4 — Dashboard consolidation

This is the most complex step. Run after Steps 1-3 are verified.

1. Create `Models/CDPDashboardViewModel.cs`
2. Extract `Views/CDP/_HCReportsPartial.cshtml` — copy table and filter markup from CMP/ReportsIndex.cshtml; adapt `@model` to `ReportsDashboardViewModel`
3. Extract `Views/CDP/_DevDashboardPartial.cshtml` — copy coachee rows table and charts from CDP/DevDashboard.cshtml; adapt `@model` to `DevDashboardViewModel`
4. Rewrite `CDPController.Dashboard()` to build `CDPDashboardViewModel` — absorb ReportsIndex query and DevDashboard query; apply role gating
5. Update `Views/CDP/Dashboard.cshtml` — add Bootstrap tab nav; render partials for Tab 2 and Tab 3
6. Remove standalone "Dev Dashboard" nav item from `_Layout.cshtml` (line 70) after tab is verified

**Keep alive during transition:** Both `CMPController.ReportsIndex()` and `CDPController.DevDashboard()` remain as standalone pages until the Dashboard tabs are verified. Remove or redirect them in a follow-up cleanup commit.

**Dependency:** Steps 1-3 verified. CDPDashboardViewModel must exist before Dashboard.cshtml is updated.

---

### Step 5 — Cleanup (post-verification)

- Optionally add `return RedirectToAction("Dashboard", "CDP")` to CMPController.ReportsIndex if removing from nav
- Optionally keep CDPController.DevDashboard as a redirect to the Dashboard page with the dev tab pre-selected (`#dev-dashboard` hash)
- Remove `DashboardViewModel.cs` from Models if no remaining references
- Remove `GetPersonalTrainingRecords()` private method from CMPController if replaced entirely

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: UNION in Razor View

**What people do:** Load `List<AssessmentSession>` and `List<TrainingRecord>` separately in ViewBag, concatenate and sort in the view.

**Why it's wrong:** Sorting across two types in Razor is brittle; pagination becomes impossible; filtering cannot be applied.

**Do this instead:** Build `List<UnifiedCapabilityRecord>` in the controller, return a single typed model.

---

### Anti-Pattern 2: Copy-Paste Filter Query into CDPController.Dashboard()

**What people do:** Copy the 100+ line filter and stats query from CMPController.ReportsIndex() verbatim into CDPController.Dashboard().

**Why it's wrong:** Two sources of truth — a filter bug must be fixed in two places. ExportResults in CMPController still uses its own copy.

**Do this instead:** For this milestone, duplicate minimally and add a `// TODO(v1.3): extract to shared service` comment marking it for future extraction. The duplicate is intentional and bounded.

---

### Anti-Pattern 3: Adding a Bool Flag Instead of Extending the View Param

**What people do:** Replace `view=personal|manage` with `isMonitor=true` when adding the HC monitoring mode.

**Why it's wrong:** Breaks existing bookmarks and hard-coded links to `?view=manage`.

**Do this instead:** Add `monitor` as a third string value for the existing `view` parameter. All existing URLs remain valid.

---

### Anti-Pattern 4: Eager Loading All Dashboard Data for Coachee Role

**What people do:** CDPController.Dashboard() unconditionally runs all three data-loading blocks (Overview + HC Reports + Dev Dashboard) for every user.

**Why it's wrong:** Coachee and Coach roles will never see HC Reports; the expensive category stats and score distribution queries run needlessly.

**Do this instead:** Gate sub-model population behind role checks. If role is Coachee or Coach, leave `HcReports` and `DevDashboard` as empty/null. The partial views are simply not rendered in Razor when the user lacks the role.

---

### Anti-Pattern 5: Deleting DashboardViewModel Before CDPDashboardViewModel Is Verified

**What people do:** Rename or delete DashboardViewModel immediately when starting the Dashboard consolidation.

**Why it's wrong:** Dashboard is called in multiple places; a rename creates compile errors before the replacement is ready.

**Do this instead:** Create CDPDashboardViewModel as a new class. Update Dashboard() to return it. Delete DashboardViewModel only when no references remain (Step 5 cleanup).

---

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Current (< 500 users) | All logic in controllers is fine. No service layer needed. |
| 1k-5k users | Extract BuildUnifiedRecords and BuildReportsQuery into a service class injected via DI. Add 5-minute sliding response cache on Dashboard for HC Reports tab. |
| 5k+ users | Add pagination to unified Records view. Consider background job for category statistics precomputation. |

---

## Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| CMPController -> CDPController | CDPController absorbs CMP's ReportsIndex query into Dashboard; no runtime call between controllers | CDPController.Dashboard() duplicates the query — see Anti-Pattern 2 |
| Records.cshtml -> Assessment results | Assessment rows in unified Records link to CMP/Certificate and CMP/Results via `AssessmentId` field | URL link only; no controller coupling |
| Dashboard Tab HC Reports -> ExportResults | Export stays at /CMP/ExportResults; HC Reports tab renders a link to it | ExportResults is not moved in this milestone |
| Layout -> DevDashboard nav item | Current `_Layout.cshtml` line 70 renders Dev Dashboard as top-level nav. Remove after Dashboard tab is verified. | Risk: removing before tab is ready hides the feature |

---

## Sources

- Direct inspection: `Controllers/CMPController.cs` (1840 lines, 2026-02-18)
- Direct inspection: `Controllers/CDPController.cs` (1475 lines, 2026-02-18)
- Direct inspection: `Models/AssessmentSession.cs`, `Models/TrainingRecord.cs`, `Models/DashboardViewModel.cs`, `Models/ReportsDashboardViewModel.cs`, `Models/DevDashboardViewModel.cs`, `Models/ApplicationUser.cs`
- Direct inspection: `Views/Shared/_Layout.cshtml`, `Views/CMP/Assessment.cshtml`, `Views/CMP/Records.cshtml`, `Views/CDP/Dashboard.cshtml`

---
*Architecture research for: Portal HC KPB v1.2 UX Consolidation*
*Researched: 2026-02-18*
