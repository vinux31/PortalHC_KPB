# Architecture Research: Coaching Management Integration

**Domain:** Coaching Session Management & Development Dashboard for ASP.NET Core MVC
**Researched:** 2026-02-17
**Confidence:** HIGH

## Existing Architecture Overview

Portal HC KPB follows the classic **ASP.NET Core MVC pattern** with Entity Framework Core:

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │   Home   │  │ Account  │  │   CMP    │  │   CDP    │    │
│  │Controller│  │Controller│  │Controller│  │Controller│    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
│       │             │              │             │          │
│  ┌────▼─────────────▼──────────────▼─────────────▼─────┐    │
│  │              Razor Views (.cshtml)                   │    │
│  │         (Server-side rendering with ViewModels)      │    │
│  └──────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│                     BUSINESS LAYER                           │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────┐    │
│  │    Controllers handle business logic directly        │    │
│  │    (No separate service layer currently)             │    │
│  └──────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│                      DATA LAYER                              │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────┐    │
│  │         ApplicationDbContext (EF Core)                │    │
│  └──────────────────────────────────────────────────────┘    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │  Users   │  │Assessment│  │IdpItems  │  │Coaching  │    │
│  │ (Identity)│ │ Sessions │  │          │  │  Logs    │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                   │
│  │   KKJ    │  │   CPDP   │  │  User    │                   │
│  │ Matrices │  │  Items   │  │Competency│                   │
│  └──────────┘  └──────────┘  └──────────┘                   │
└─────────────────────────────────────────────────────────────┘
```

### Current Component Responsibilities

| Component | Responsibility | Current Implementation |
|-----------|----------------|------------------------|
| **Controllers** | Request handling, business logic, view rendering | Monolithic (CMPController = 1047 lines) |
| **Models** | Domain entities, ViewModels | Separate files per entity in `Models/` |
| **ApplicationDbContext** | Database access via EF Core | Single DbContext with all DbSets |
| **Razor Views** | Server-side rendering | `.cshtml` files in `Views/{Controller}/` |
| **ASP.NET Identity** | Authentication, role-based authorization | 6-level hierarchy (Admin → Coachee) |
| **Migrations** | Schema versioning | EF Core migrations in `Migrations/` |

### Existing Authentication & Authorization Pattern

**Role-Based Access Control (RBAC):**
```csharp
// ApplicationUser.RoleLevel (1-6)
Level 1: Admin (full access, view-switching capability)
Level 2: HC (HR staff)
Level 3: Direktur, VP, Manager
Level 4: Section Head, Sr Supervisor
Level 5: Coach
Level 6: Coachee

// View-based filtering for Admin
ApplicationUser.SelectedView: "HC" | "Atasan" | "Coach" | "Coachee"
```

**Authorization pattern in controllers:**
```csharp
[Authorize]
public class CDPController : Controller
{
    // Check user role
    var userRoles = await _userManager.GetRolesAsync(user);
    var userRole = userRoles.FirstOrDefault();

    // Filter data based on role and selected view
    if (userRole == UserRoles.Admin && user.SelectedView == "Coach")
    {
        // Show coach-specific data
    }
}
```

## Recommended Architecture for Coaching Features

### Integration Strategy: Extend Existing CDPController

**DO NOT create separate controllers.** The coaching management features should extend the existing `CDPController` to maintain consistency with the codebase pattern.

### Component Integration Map

| Component Type | Modify Existing | Create New | Rationale |
|----------------|-----------------|------------|-----------|
| **Controllers** | `CDPController` | None | Keep all CDP features in one controller |
| **Models** | None | `CoachingSession.cs`, `ActionItem.cs`, `CoachingApproval.cs` | New domain entities |
| **ViewModels** | None | `CoachingDashboardViewModel.cs`, `SessionFormViewModel.cs` | New view concerns |
| **DbContext** | `ApplicationDbContext` | None | Add DbSets for new entities |
| **Views** | None | `Views/CDP/Sessions.cshtml`, `Views/CDP/SessionDetails.cshtml`, `Views/CDP/DevelopmentDashboard.cshtml` | New pages |
| **Migrations** | None | New migration for coaching schema | Schema evolution |

### New Data Models Required

#### 1. CoachingSession (Primary Entity)

```csharp
namespace HcPortal.Models
{
    public class CoachingSession
    {
        public int Id { get; set; }

        // Foreign Keys
        public string CoachId { get; set; } = "";
        public ApplicationUser? Coach { get; set; }

        public string CoacheeId { get; set; } = "";
        public ApplicationUser? Coachee { get; set; }

        // Session Details
        public DateTime SessionDate { get; set; }
        public int DurationMinutes { get; set; }
        public string SessionType { get; set; } = ""; // "Technical", "Behavioral", "Career"
        public string Location { get; set; } = ""; // "Online", "On-site"

        // Content
        public string Objectives { get; set; } = "";
        public string DiscussionNotes { get; set; } = "";
        public string CoacheeStrengths { get; set; } = "";
        public string DevelopmentAreas { get; set; } = "";
        public string NextSteps { get; set; } = "";

        // CMP Integration (Optional)
        public int? KkjMatrixItemId { get; set; }  // Link to competency
        public KkjMatrixItem? KkjMatrixItem { get; set; }

        // Approval Workflow
        public string Status { get; set; } = "Draft"; // "Draft", "Pending", "Approved", "Rejected"
        public string? ApproverComments { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    }
}
```

#### 2. ActionItem (Child Entity)

```csharp
namespace HcPortal.Models
{
    public class ActionItem
    {
        public int Id { get; set; }

        // Foreign Key
        public int CoachingSessionId { get; set; }
        public CoachingSession? CoachingSession { get; set; }

        // Action Details
        public string Description { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "Not Started"; // "Not Started", "In Progress", "Completed", "Blocked"
        public int Priority { get; set; } = 2; // 1=High, 2=Medium, 3=Low

        // Progress Tracking
        public int ProgressPercent { get; set; } = 0; // 0-100
        public string? CompletionEvidence { get; set; } // File path or URL
        public DateTime? CompletedAt { get; set; }

        // Notes
        public string? Notes { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
```

#### 3. CoachingApproval (Workflow Entity)

```csharp
namespace HcPortal.Models
{
    public class CoachingApproval
    {
        public int Id { get; set; }

        // Foreign Key
        public int CoachingSessionId { get; set; }
        public CoachingSession? CoachingSession { get; set; }

        // Approver
        public string ApproverId { get; set; } = "";
        public ApplicationUser? Approver { get; set; }
        public string ApproverRole { get; set; } = ""; // "Section Head", "HC"

        // Decision
        public string Decision { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
        public string? Comments { get; set; }
        public DateTime? DecisionDate { get; set; }

        // Audit
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### ViewModels for New Features

#### 1. CoachingDashboardViewModel

```csharp
namespace HcPortal.Models
{
    public class CoachingDashboardViewModel
    {
        // Summary Stats
        public int TotalSessions { get; set; }
        public int SessionsThisMonth { get; set; }
        public int TotalActionItems { get; set; }
        public int CompletedActionItems { get; set; }
        public int ActionItemCompletionRate { get; set; } // %

        // Recent Sessions
        public List<CoachingSession> RecentSessions { get; set; } = new List<CoachingSession>();

        // Action Items by Status
        public int NotStartedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int BlockedCount { get; set; }

        // Pending Approvals (for Section Head/HC)
        public int PendingApprovalsCount { get; set; }
        public List<CoachingSession> PendingApprovals { get; set; } = new List<CoachingSession>();

        // Progress Over Time (Chart Data)
        public List<string> ChartMonths { get; set; } = new List<string>();
        public List<int> ChartSessionCounts { get; set; } = new List<int>();
        public List<int> ChartActionItemCounts { get; set; } = new List<int>();

        // Team Overview (for Coach role)
        public List<CoacheeProgress> TeamProgress { get; set; } = new List<CoacheeProgress>();
    }

    public class CoacheeProgress
    {
        public string CoacheeId { get; set; } = "";
        public string CoacheeName { get; set; } = "";
        public int TotalSessions { get; set; }
        public int CompletedActionItems { get; set; }
        public int TotalActionItems { get; set; }
        public int CompletionRate { get; set; } // %
        public DateTime? LastSessionDate { get; set; }
    }
}
```

#### 2. SessionFormViewModel

```csharp
namespace HcPortal.Models
{
    public class SessionFormViewModel
    {
        // For Coach role: list of coachees to select from
        public List<ApplicationUser> AvailableCoachees { get; set; } = new List<ApplicationUser>();

        // For linking to competency gaps
        public List<KkjMatrixItem> CompetencyGaps { get; set; } = new List<KkjMatrixItem>();

        // Session data (bound to form)
        public CoachingSession Session { get; set; } = new CoachingSession();

        // Action items (bound to form)
        public List<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    }
}
```

## Data Flow Patterns

### 1. Coaching Session CRUD Flow

```
[User: Coach/Coachee]
    ↓ (HTTP POST /CDP/CreateSession)
[CDPController.CreateSession()]
    ↓
[Validate user role & permissions]
    ↓
[Create CoachingSession entity]
    ↓
[_context.CoachingSessions.Add(session)]
    ↓
[await _context.SaveChangesAsync()]
    ↓
[Create ActionItem entities if any]
    ↓
[Redirect to /CDP/SessionDetails/{id}]
```

**Permission Matrix:**
- **Coach (Level 5)**: Create sessions for assigned coachees
- **Coachee (Level 6)**: View own sessions only
- **Section Head (Level 4)**: Approve sessions, view all in section
- **HC (Level 2)**: View all, approve all
- **Admin (Level 1)**: Full access, view-based filtering

### 2. Approval Workflow Flow

```
[Coach creates session]
    ↓
[Status = "Draft"]
    ↓
[Coach submits for approval]
    ↓
[Status = "Pending"]
    ↓
[Create CoachingApproval record for Section Head]
    ↓
[Send notification (future: email/in-app)]
    ↓
[Section Head reviews]
    ↓ (Decision = "Approved")
[Update CoachingSession.Status = "Approved"]
[Update CoachingApproval.Decision = "Approved"]
    ↓
[Session becomes visible to coachee]
```

**Implementation Note:** Start with single-tier approval (Section Head OR HC). Multi-tier approval can be added later if needed.

### 3. Development Dashboard Data Flow

```
[User navigates to /CDP/DevelopmentDashboard]
    ↓
[CDPController.DevelopmentDashboard()]
    ↓
[Get current user & role]
    ↓
[Apply role-based filtering]
    ↓ (if Coachee)
[Query sessions where CoacheeId = userId]
    ↓ (if Coach)
[Query sessions where CoachId = userId]
    ↓ (if Section Head)
[Query sessions where coachee.Section = user.Section]
    ↓ (if HC/Admin)
[Query all sessions (with view-based filtering)]
    ↓
[Aggregate data for dashboard metrics]
    ↓
[Build CoachingDashboardViewModel]
    ↓
[Return View(model)]
```

### 4. CMP Integration Flow (Competency Gap to Coaching)

```
[CMP Assessment completed]
    ↓
[UserCompetencyLevel records created/updated]
    ↓
[Gap identified: TargetLevel - CurrentLevel > 0]
    ↓
[User/Coach navigates to Create Coaching Session]
    ↓
[SessionFormViewModel loads competency gaps]
    ↓
[SELECT * FROM UserCompetencyLevels
 WHERE UserId = @coacheeId AND Gap > 0]
    ↓
[Display gaps in dropdown]
    ↓
[Coach selects gap, creates session]
    ↓
[CoachingSession.KkjMatrixItemId = selected competency]
    ↓
[Future: Track if competency improved after coaching]
```

## Controller Integration Points

### Extend CDPController with New Actions

```csharp
public class CDPController : Controller
{
    // EXISTING ACTIONS (keep as-is)
    public IActionResult Index() { ... }
    public IActionResult PlanIdp() { ... }
    public IActionResult Dashboard() { ... }
    public IActionResult Coaching() { ... }  // MODIFY: Change to Sessions list
    public IActionResult Progress() { ... }

    // NEW ACTIONS (add these)

    // Coaching Session Management
    public async Task<IActionResult> Sessions(string? filter) { ... }
    public async Task<IActionResult> SessionDetails(int id) { ... }
    public IActionResult CreateSession() { ... }
    public async Task<IActionResult> CreateSession(SessionFormViewModel model) { ... }
    public async Task<IActionResult> EditSession(int id) { ... }
    public async Task<IActionResult> EditSession(int id, SessionFormViewModel model) { ... }
    public async Task<IActionResult> DeleteSession(int id) { ... }

    // Action Item Management
    public async Task<IActionResult> UpdateActionItem(int id, ActionItem item) { ... }
    public async Task<IActionResult> CompleteActionItem(int id, string evidence) { ... }

    // Approval Workflow
    public async Task<IActionResult> SubmitForApproval(int sessionId) { ... }
    public async Task<IActionResult> ApproveSession(int id, string comments) { ... }
    public async Task<IActionResult> RejectSession(int id, string comments) { ... }

    // Development Dashboard
    public async Task<IActionResult> DevelopmentDashboard() { ... }
    public async Task<JsonResult> GetProgressChartData(string period) { ... }
}
```

### Recommended Action Naming Convention

**Follow existing pattern:**
- **List views:** `Sessions()` (not `SessionsList()`)
- **Details views:** `SessionDetails(int id)` (not `SessionDetail()`)
- **Create:** `CreateSession()` GET + `CreateSession(model)` POST
- **Edit:** `EditSession(int id)` GET + `EditSession(int id, model)` POST
- **Delete:** `DeleteSession(int id)` POST

### Code Organization Within Controller

```csharp
public class CDPController : Controller
{
    // ===== CONSTRUCTOR & DEPENDENCIES =====
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    // ===== SECTION 1: IDP MANAGEMENT =====
    public async Task<IActionResult> PlanIdp() { ... }

    // ===== SECTION 2: DASHBOARD =====
    public async Task<IActionResult> Dashboard() { ... }

    // ===== SECTION 3: COACHING SESSION MANAGEMENT =====
    public async Task<IActionResult> Sessions() { ... }
    public async Task<IActionResult> SessionDetails(int id) { ... }
    // ... other session actions

    // ===== SECTION 4: ACTION ITEM MANAGEMENT =====
    public async Task<IActionResult> UpdateActionItem() { ... }

    // ===== SECTION 5: APPROVAL WORKFLOW =====
    public async Task<IActionResult> SubmitForApproval() { ... }

    // ===== SECTION 6: DEVELOPMENT DASHBOARD =====
    public async Task<IActionResult> DevelopmentDashboard() { ... }

    // ===== SECTION 7: PROGRESS TRACKING =====
    public async Task<IActionResult> Progress() { ... }
}
```

**Recommended controller size limit:** 600-800 lines per controller. If CDPController exceeds 1000 lines, consider:
1. Extract helper methods to a separate `CDPHelpers.cs` class
2. Extract complex queries to a `CDPQueries.cs` class
3. DO NOT create separate controllers (maintain MVC pattern consistency)

## Database Schema Changes

### ApplicationDbContext Updates

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // EXISTING DbSets
    public DbSet<IdpItem> IdpItems { get; set; }
    public DbSet<CoachingLog> CoachingLogs { get; set; }  // DEPRECATE (replaced by CoachingSessions)
    public DbSet<AssessmentSession> AssessmentSessions { get; set; }
    public DbSet<KkjMatrixItem> KkjMatrices { get; set; }
    public DbSet<UserCompetencyLevel> UserCompetencyLevels { get; set; }

    // NEW DbSets
    public DbSet<CoachingSession> CoachingSessions { get; set; }
    public DbSet<ActionItem> ActionItems { get; set; }
    public DbSet<CoachingApproval> CoachingApprovals { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // NEW: CoachingSession configuration
        builder.Entity<CoachingSession>(entity =>
        {
            entity.HasOne(c => c.Coach)
                .WithMany()
                .HasForeignKey(c => c.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Coachee)
                .WithMany()
                .HasForeignKey(c => c.CoacheeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.KkjMatrixItem)
                .WithMany()
                .HasForeignKey(c => c.KkjMatrixItemId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            entity.HasIndex(c => c.CoachId);
            entity.HasIndex(c => c.CoacheeId);
            entity.HasIndex(c => c.Status);
            entity.HasIndex(c => c.SessionDate);
        });

        // NEW: ActionItem configuration
        builder.Entity<ActionItem>(entity =>
        {
            entity.HasOne(a => a.CoachingSession)
                .WithMany(c => c.ActionItems)
                .HasForeignKey(a => a.CoachingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.DueDate);
        });

        // NEW: CoachingApproval configuration
        builder.Entity<CoachingApproval>(entity =>
        {
            entity.HasOne(a => a.CoachingSession)
                .WithMany()
                .HasForeignKey(a => a.CoachingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.Decision);
        });
    }
}
```

### Migration Strategy

**Create migration:**
```bash
dotnet ef migrations add AddCoachingManagement
dotnet ef database update
```

**Migration dependency order:**
1. `CoachingSessions` table (depends on `Users`, `KkjMatrices`)
2. `ActionItems` table (depends on `CoachingSessions`)
3. `CoachingApprovals` table (depends on `CoachingSessions`, `Users`)

**Data migration consideration:**
- Existing `CoachingLogs` table may have legacy data
- Option 1: Keep both tables, mark `CoachingLogs` as deprecated
- Option 2: Migrate data from `CoachingLogs` to `CoachingSessions` (manual script)
- **Recommendation:** Keep both tables initially, migrate in separate phase

## View Structure

### New View Files

```
Views/
├── CDP/
│   ├── Index.cshtml                    # EXISTING: CDP hub
│   ├── Dashboard.cshtml                # EXISTING: IDP dashboard
│   ├── PlanIdp.cshtml                  # EXISTING: IDP viewer
│   ├── Progress.cshtml                 # EXISTING: Progress tracking
│   ├── Coaching.cshtml                 # MODIFY: Rename to Sessions.cshtml
│   │
│   ├── Sessions.cshtml                 # NEW: Session list view
│   ├── SessionDetails.cshtml           # NEW: Single session view
│   ├── CreateSession.cshtml            # NEW: Create session form
│   ├── EditSession.cshtml              # NEW: Edit session form
│   ├── DevelopmentDashboard.cshtml     # NEW: Progress over time dashboard
│   │
│   └── _SessionCard.cshtml             # NEW: Partial view for session card
│   └── _ActionItemList.cshtml          # NEW: Partial view for action items
```

### View-ViewModel Mapping

| View | ViewModel | Model Type |
|------|-----------|------------|
| `Sessions.cshtml` | `List<CoachingSession>` | Collection view |
| `SessionDetails.cshtml` | `CoachingSession` | Single entity |
| `CreateSession.cshtml` | `SessionFormViewModel` | Complex form |
| `EditSession.cshtml` | `SessionFormViewModel` | Complex form |
| `DevelopmentDashboard.cshtml` | `CoachingDashboardViewModel` | Aggregate data |

### Razor Partial Views Pattern

**Example: _SessionCard.cshtml**
```cshtml
@model HcPortal.Models.CoachingSession

<div class="card shadow-sm mb-3">
    <div class="card-body">
        <div class="d-flex justify-content-between">
            <h5 class="card-title">@Model.SessionType</h5>
            <span class="badge bg-@GetStatusColor(Model.Status)">@Model.Status</span>
        </div>
        <p class="text-muted mb-2">
            <i class="bi bi-calendar-event"></i> @Model.SessionDate.ToString("dd MMM yyyy")
            <i class="bi bi-clock ms-3"></i> @Model.DurationMinutes min
        </p>
        <p class="card-text">@Model.Objectives</p>
        <a asp-action="SessionDetails" asp-route-id="@Model.Id" class="btn btn-sm btn-outline-primary">
            View Details
        </a>
    </div>
</div>
```

**Reuse pattern:** Use partial views to avoid duplication across list and dashboard views.

## Architectural Patterns

### Pattern 1: Role-Based View Filtering

**What:** Filter data queries based on user role and selected view (for Admin).

**When to use:** All coaching session queries in controller actions.

**Implementation:**
```csharp
public async Task<IActionResult> Sessions(string? filter)
{
    var user = await _userManager.GetUserAsync(User);
    var userRoles = await _userManager.GetRolesAsync(user);
    var userRole = userRoles.FirstOrDefault();
    var userId = user?.Id ?? "";

    var sessionsQuery = _context.CoachingSessions.AsQueryable();

    // Role-based filtering
    if (userRole == UserRoles.Admin)
    {
        if (user.SelectedView == "Coach")
        {
            sessionsQuery = sessionsQuery.Where(s => s.CoachId == userId);
        }
        else if (user.SelectedView == "Coachee")
        {
            sessionsQuery = sessionsQuery.Where(s => s.CoacheeId == userId);
        }
        // HC view: no filter (show all)
    }
    else if (userRole == "Coach")
    {
        sessionsQuery = sessionsQuery.Where(s => s.CoachId == userId);
    }
    else if (userRole == "Coachee")
    {
        sessionsQuery = sessionsQuery.Where(s => s.CoacheeId == userId);
    }
    else if (userRole == "Section Head")
    {
        var sectionUserIds = await _context.Users
            .Where(u => u.Section == user.Section)
            .Select(u => u.Id)
            .ToListAsync();
        sessionsQuery = sessionsQuery.Where(s =>
            sectionUserIds.Contains(s.CoachId) ||
            sectionUserIds.Contains(s.CoacheeId));
    }

    var sessions = await sessionsQuery
        .Include(s => s.Coach)
        .Include(s => s.Coachee)
        .OrderByDescending(s => s.SessionDate)
        .ToListAsync();

    return View(sessions);
}
```

**Trade-offs:**
- **Pros:** Secure, consistent with existing codebase pattern
- **Cons:** Query logic repeated across actions (mitigate with helper method)

### Pattern 2: Eager Loading for Performance

**What:** Use `.Include()` to load related entities in a single query.

**When to use:** When displaying session details with coach/coachee names, action items, etc.

**Implementation:**
```csharp
public async Task<IActionResult> SessionDetails(int id)
{
    var session = await _context.CoachingSessions
        .Include(s => s.Coach)
        .Include(s => s.Coachee)
        .Include(s => s.ActionItems)
        .Include(s => s.KkjMatrixItem)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (session == null)
        return NotFound();

    // Authorization check
    var user = await _userManager.GetUserAsync(User);
    if (!CanUserViewSession(user, session))
        return Forbid();

    return View(session);
}
```

**Trade-offs:**
- **Pros:** Avoids N+1 query problem, faster rendering
- **Cons:** Loads more data than needed for list views (use selectively)

### Pattern 3: ViewModel for Complex Forms

**What:** Use dedicated ViewModel for create/edit forms that need additional data (dropdowns, related entities).

**When to use:** CreateSession/EditSession actions.

**Implementation:**
```csharp
public async Task<IActionResult> CreateSession()
{
    var user = await _userManager.GetUserAsync(User);
    var userRoles = await _userManager.GetRolesAsync(user);
    var userRole = userRoles.FirstOrDefault();

    var model = new SessionFormViewModel();

    // Populate coachees list for Coach role
    if (userRole == "Coach" || userRole == UserRoles.Admin)
    {
        model.AvailableCoachees = await _context.Users
            .Where(u => u.RoleLevel == 6) // Coachee level
            .ToListAsync();
    }

    // Load competency gaps for the coachee (if pre-selected)
    // This will be updated via AJAX when coachee is selected

    return View(model);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateSession(SessionFormViewModel model)
{
    if (!ModelState.IsValid)
    {
        // Repopulate dropdowns
        model.AvailableCoachees = await _context.Users
            .Where(u => u.RoleLevel == 6)
            .ToListAsync();
        return View(model);
    }

    var user = await _userManager.GetUserAsync(User);
    model.Session.CoachId = user.Id;

    _context.CoachingSessions.Add(model.Session);
    await _context.SaveChangesAsync();

    // Create action items if any
    foreach (var actionItem in model.ActionItems)
    {
        actionItem.CoachingSessionId = model.Session.Id;
        _context.ActionItems.Add(actionItem);
    }
    await _context.SaveChangesAsync();

    return RedirectToAction("SessionDetails", new { id = model.Session.Id });
}
```

**Trade-offs:**
- **Pros:** Cleaner separation of concerns, easier validation
- **Cons:** More classes to maintain (worth it for complex forms)

### Pattern 4: Status Workflow State Machine

**What:** Enforce valid status transitions for approval workflow.

**When to use:** SubmitForApproval, ApproveSession, RejectSession actions.

**Implementation:**
```csharp
public class SessionWorkflow
{
    private static readonly Dictionary<string, List<string>> ValidTransitions = new()
    {
        { "Draft", new List<string> { "Pending" } },
        { "Pending", new List<string> { "Approved", "Rejected" } },
        { "Approved", new List<string> { "Draft" } }, // Allow re-editing
        { "Rejected", new List<string> { "Draft" } }
    };

    public static bool CanTransition(string currentStatus, string newStatus)
    {
        return ValidTransitions.ContainsKey(currentStatus) &&
               ValidTransitions[currentStatus].Contains(newStatus);
    }
}

// Usage in controller
public async Task<IActionResult> SubmitForApproval(int sessionId)
{
    var session = await _context.CoachingSessions.FindAsync(sessionId);
    if (session == null)
        return NotFound();

    if (!SessionWorkflow.CanTransition(session.Status, "Pending"))
        return BadRequest("Invalid status transition");

    session.Status = "Pending";
    await _context.SaveChangesAsync();

    // Create approval record
    var approval = new CoachingApproval
    {
        CoachingSessionId = sessionId,
        ApproverId = await GetSectionHeadId(session.CoacheeId),
        ApproverRole = "Section Head"
    };
    _context.CoachingApprovals.Add(approval);
    await _context.SaveChangesAsync();

    return RedirectToAction("SessionDetails", new { id = sessionId });
}
```

**Trade-offs:**
- **Pros:** Prevents invalid state changes, easier to add workflow rules later
- **Cons:** Adds complexity (only use if approval workflow is required)

## Anti-Patterns to Avoid

### Anti-Pattern 1: Creating Separate CoachingController

**What people might do:** Create `CoachingController.cs` for new coaching features.

**Why it's wrong:**
- Breaks consistency with existing codebase (all CDP features in CDPController)
- Creates navigation confusion (is it CDP or Coaching?)
- Duplicates authentication/authorization logic

**Do this instead:** Extend CDPController with new actions in organized sections.

### Anti-Pattern 2: Copying ViewBag Pattern for Complex Data

**What people might do:**
```csharp
ViewBag.Coachees = await _context.Users.Where(...).ToListAsync();
ViewBag.CompetencyGaps = await _context.UserCompetencyLevels.Where(...).ToListAsync();
```

**Why it's wrong:**
- No compile-time type safety
- Easy to misspell ViewBag keys
- Hard to debug

**Do this instead:** Use strongly-typed ViewModels:
```csharp
var model = new SessionFormViewModel
{
    AvailableCoachees = await _context.Users.Where(...).ToListAsync(),
    CompetencyGaps = await _context.UserCompetencyLevels.Where(...).ToListAsync()
};
return View(model);
```

### Anti-Pattern 3: Using Session State for Form Data

**What people might do:** Store session creation data in `HttpContext.Session` across multiple page loads.

**Why it's wrong:**
- Session state is for user preferences, not transient form data
- Doesn't work well with back button or multiple tabs
- Memory overhead on server

**Do this instead:** Use ViewModel with hidden fields or TempData for multi-step forms:
```csharp
[HttpPost]
public async Task<IActionResult> CreateSessionStep1(SessionFormViewModel model)
{
    TempData["SessionData"] = JsonSerializer.Serialize(model);
    return RedirectToAction("CreateSessionStep2");
}

public IActionResult CreateSessionStep2()
{
    var sessionData = TempData["SessionData"]?.ToString();
    var model = JsonSerializer.Deserialize<SessionFormViewModel>(sessionData);
    return View(model);
}
```

**Better:** Use single-page form with progressive disclosure (show/hide sections with JavaScript).

### Anti-Pattern 4: N+1 Query Problem

**What people might do:**
```csharp
var sessions = await _context.CoachingSessions.ToListAsync();
foreach (var session in sessions)
{
    // This triggers a separate query for EACH session!
    session.Coach = await _context.Users.FindAsync(session.CoachId);
    session.Coachee = await _context.Users.FindAsync(session.CoacheeId);
}
```

**Why it's wrong:**
- 1 query to get sessions + 2N queries to get users = horrible performance
- Gets worse with more related entities

**Do this instead:** Use eager loading:
```csharp
var sessions = await _context.CoachingSessions
    .Include(s => s.Coach)
    .Include(s => s.Coachee)
    .ToListAsync();
```

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| **0-100 users** | Current monolithic MVC pattern is perfect. No changes needed. |
| **100-1000 users** | Add database indexes on foreign keys (already recommended). Consider output caching for dashboard. |
| **1000+ users** | Add Redis distributed cache for session data. Consider read replicas for reporting queries. |

### Scaling Priorities

1. **First bottleneck:** Dashboard queries (aggregating sessions/action items)
   - **Fix:** Add covering indexes on Status, SessionDate
   - **Fix:** Cache dashboard ViewModel for 5-10 minutes
   - **Fix:** Use `AsNoTracking()` for read-only queries

2. **Second bottleneck:** File uploads (if evidence attachments are large)
   - **Fix:** Move from database storage to Azure Blob Storage
   - **Fix:** Store only file path in `ActionItem.CompletionEvidence`

**Current phase:** No scaling concerns. Build first, optimize later.

## Integration with Existing CMP Features

### CMP → CDP Flow: Competency Gap to Coaching

**Entry point:** CMP Assessment Results page

**Flow:**
1. User completes CMP Assessment
2. `UserCompetencyLevel` records created/updated
3. Gap identified (TargetLevel - CurrentLevel > 0)
4. User clicks "Create Coaching Plan" button
5. Redirect to `/CDP/CreateSession?gap={KkjMatrixItemId}`
6. SessionFormViewModel pre-fills:
   - `CoacheeId` = current user
   - `KkjMatrixItemId` = selected gap
   - `Objectives` = "Address competency gap: {competency name}"

**Implementation:**
```csharp
// In CMP/AssessmentResults.cshtml
<a asp-controller="CDP"
   asp-action="CreateSession"
   asp-route-gap="@competencyLevel.KkjMatrixItemId"
   class="btn btn-primary">
    Create Coaching Plan
</a>

// In CDPController.CreateSession()
public async Task<IActionResult> CreateSession(int? gap)
{
    var model = new SessionFormViewModel();

    if (gap.HasValue)
    {
        var competency = await _context.KkjMatrices.FindAsync(gap.Value);
        if (competency != null)
        {
            model.Session.KkjMatrixItemId = gap.Value;
            model.Session.Objectives = $"Address competency gap: {competency.Kompetensi}";
        }
    }

    // ... rest of logic
}
```

### CDP → CMP Flow: Coaching to Competency Update

**Future enhancement:** After coaching session is completed, optionally allow HC to update `UserCompetencyLevel.CurrentLevel` if competency improved.

**Implementation:** Add button in SessionDetails view (only visible to HC role):
```cshtml
@if (User.IsInRole("HC") && Model.Status == "Approved" && Model.KkjMatrixItemId.HasValue)
{
    <button type="button" class="btn btn-success" data-bs-toggle="modal" data-bs-target="#updateCompetencyModal">
        Update Competency Level
    </button>
}
```

**Modal form:** Update `UserCompetencyLevel.CurrentLevel` for the coachee.

## Recommended Build Order

### Phase 1: Core Data Models & CRUD (Foundation)

**Dependencies:** None

**Build:**
1. Create `CoachingSession.cs`, `ActionItem.cs`, `CoachingApproval.cs` models
2. Update `ApplicationDbContext` with new DbSets
3. Create migration: `AddCoachingManagement`
4. Apply migration

**Testing:** EF Core can save/retrieve entities

**Time estimate:** 2-4 hours

### Phase 2: Basic Session CRUD (No Approval Yet)

**Dependencies:** Phase 1

**Build:**
1. `CDPController.Sessions()` - list view
2. `CDPController.SessionDetails(id)` - detail view
3. `CDPController.CreateSession()` - GET & POST
4. Create `SessionFormViewModel.cs`
5. Create views: `Sessions.cshtml`, `SessionDetails.cshtml`, `CreateSession.cshtml`
6. Implement role-based filtering in queries

**Testing:** Coach can create session, coachee can view own sessions

**Time estimate:** 4-6 hours

### Phase 3: Action Item Management

**Dependencies:** Phase 2

**Build:**
1. Add action item input fields to `CreateSession.cshtml`
2. `CDPController.UpdateActionItem()` - POST (AJAX)
3. `CDPController.CompleteActionItem()` - POST
4. Create `_ActionItemList.cshtml` partial view
5. Add JavaScript for inline editing

**Testing:** Can create action items with session, mark as complete

**Time estimate:** 3-4 hours

### Phase 4: Approval Workflow

**Dependencies:** Phase 2

**Build:**
1. `CDPController.SubmitForApproval()` - POST
2. `CDPController.ApproveSession()` - POST
3. `CDPController.RejectSession()` - POST
4. Create `SessionWorkflow` helper class
5. Add approval UI to `SessionDetails.cshtml`
6. Filter pending approvals for Section Head/HC role

**Testing:** Section Head can approve/reject sessions

**Time estimate:** 3-4 hours

### Phase 5: Development Dashboard

**Dependencies:** Phase 2, Phase 3

**Build:**
1. Create `CoachingDashboardViewModel.cs`
2. `CDPController.DevelopmentDashboard()` - GET
3. Create `DevelopmentDashboard.cshtml`
4. Add Chart.js integration for progress over time
5. Add team overview table for Coach role

**Testing:** Dashboard shows accurate metrics

**Time estimate:** 4-6 hours

### Phase 6: CMP Integration (Optional)

**Dependencies:** Phase 2, existing CMP features

**Build:**
1. Add "Create Coaching Plan" button to CMP AssessmentResults view
2. Pre-fill CreateSession form when `gap` parameter is present
3. Add competency update modal to SessionDetails view

**Testing:** Can create session from competency gap, update level after coaching

**Time estimate:** 2-3 hours

**Total estimated time:** 18-27 hours (3-5 days for single developer)

## Dependency Graph

```
Phase 1: Data Models
    ↓
Phase 2: Session CRUD ← [Required for all other phases]
    ↓               ↓
Phase 3: Action Items   Phase 4: Approval Workflow
    ↓                       ↓
Phase 5: Development Dashboard
    ↓
Phase 6: CMP Integration (optional)
```

**Build order rationale:**
- Phase 1 & 2 must be done first (foundation)
- Phase 3 & 4 can be done in parallel (independent features)
- Phase 5 depends on 3 (action item metrics) but not 4
- Phase 6 is optional enhancement

**Recommended approach:** Build phases 1-2, test thoroughly, then proceed to 3-4-5.

## Sources

**ASP.NET Core MVC Patterns:**
- [Session and state management in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-10.0)
- [Sessions in ASP.NET Core MVC - Dot Net Tutorials](https://dotnettutorials.net/lesson/sessions-in-asp-net-core-mvc/)
- [State Management in ASP.NET Core MVC - Code Maze](https://code-maze.com/state-management-in-asp-net-core-mvc/)

**Approval Workflow Patterns:**
- [Asp.net MVC app with sequential approvals - Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/806957/asp-net-mvc-app-with-sequential-approvals)
- [Implementing Simple / Complex Approval Workflow for an Existing Application - Elsa Workflows](https://github.com/elsa-workflows/elsa-core/discussions/1002)
- [Building Workflow Driven .NET Core Applications with Elsa](https://sipkeschoorstra.medium.com/building-workflow-driven-net-core-applications-with-elsa-139523aa4c50)

**CRUD & Dashboard Patterns:**
- [Tutorial: Implement CRUD Functionality - ASP.NET MVC with EF Core](https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/crud?view=aspnetcore-2.2)
- [Developing ASP.NET Core MVC apps - .NET](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/develop-asp-net-core-mvc-apps)
- [Building a Dashboard with ASP.NET Core and DotVVM](https://medium.com/dotvvm/building-a-dashboard-with-asp-net-core-and-dotvvm-b0439a489a9c)

---
*Architecture research for: CDP Coaching Management & Development Dashboard*
*Researched: 2026-02-17*
*Confidence: HIGH - Based on codebase analysis and official ASP.NET Core documentation*
