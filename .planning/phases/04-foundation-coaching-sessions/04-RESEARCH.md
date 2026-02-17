# Phase 4: Foundation & Coaching Sessions - Research

**Researched:** 2026-02-17
**Domain:** ASP.NET Core MVC — EF Core schema migration, coaching session data model, CRUD controller/view patterns
**Confidence:** HIGH (based entirely on direct codebase inspection)

---

## Summary

Phase 4 is primarily a **data model and CRUD** problem on top of a well-understood, stable codebase. The v1.0 stack (ASP.NET Core MVC + EF Core + SQL Server + Bootstrap 5 + Bootstrap Icons) is already in place and working. No new libraries are needed.

The central challenge is that the existing `CoachingLog` model is designed for a future Proton-specific form (Phase 5), not for the simple coaching session concept COACH-01 to COACH-03 requires. Phase 4 must introduce a clean `CoachingSession` entity and `ActionItem` entity without touching the existing `CoachingLog` table (which has live data considerations) — and must also fix the broken `TrackingItemId` column and register `CoachCoacheeMapping` in the DbContext so Phase 5 can use it.

The existing `Views/CDP/Coaching.cshtml` is a read-only display stub that renders `List<CoachingLog>` — it needs to be replaced or supplemented with a proper form-backed implementation using the new `CoachingSession` model. The existing CDPController already has a `Coaching()` GET action that works correctly for fetching existing records; only POST actions (create session, add action items) are missing.

**Primary recommendation:** Add `CoachingSession` and `ActionItem` as new entities alongside the existing `CoachingLog`. Do NOT rename or alter `CoachingLog` — it serves Phase 5. Fix `CoachingLog.TrackingItemId` by dropping the column via migration (it has no FK constraint and no data). Register `CoachCoacheeMapping` in DbContext. One migration covers all DB changes.

---

## Standard Stack

### Core (already installed — no additions needed)
| Component | Version | Purpose | Notes |
|-----------|---------|---------|-------|
| ASP.NET Core MVC | .NET 8.0 | Controller/View framework | TargetFramework: net8.0 |
| EF Core SqlServer | 8.0.0 | ORM + migrations | `Microsoft.EntityFrameworkCore.SqlServer` |
| EF Core Tools | 8.0.0 | `dotnet ef migrations add` | Already in csproj |
| ASP.NET Core Identity | 8.0.0 | Auth + user management | ApplicationUser extends IdentityUser |
| Bootstrap 5 | CDN via _Layout.cshtml | UI framework | All existing views use Bootstrap 5 classes |
| Bootstrap Icons | CDN | Icon library | `bi bi-*` pattern used throughout |

### No New Packages Required
Phase 4 does not need any new NuGet packages. All required capabilities are already present in the project.

---

## Architecture Patterns

### Existing Project Structure (relevant to Phase 4)
```
Controllers/
└── CDPController.cs         # Already has Coaching() GET — add POST actions here

Data/
└── ApplicationDbContext.cs  # Add DbSets + OnModelCreating for new entities

Models/
├── CoachingLog.cs           # EXISTING — Proton form model (Phase 5). Do not remove.
├── CoachCoacheeMapping.cs   # EXISTING model — NOT in DbContext yet. Register in migration.
├── CoachingSession.cs       # NEW — simple session: date, topic, notes, coachee
├── ActionItem.cs            # NEW — per-session action: description, due date, status
└── CoachingViewModels.cs    # NEW — ViewModels for create form and history view

Migrations/
└── YYYYMMDDHHMMSS_AddCoachingFoundation.cs  # NEW — one migration for all Phase 4 DB changes

Views/CDP/
└── Coaching.cshtml          # REPLACE — current stub with proper create form and history list
```

### Pattern 1: EF Core Migration (adding tables + altering existing)
**What:** One migration that (a) creates `CoachingSessions`, (b) creates `ActionItems`, (c) creates `CoachCoacheeMappings`, and (d) drops the `TrackingItemId` column from `CoachingLogs`.
**When to use:** Anytime the schema changes.
**Example:**
```csharp
// Source: existing migration pattern from Migrations/20260214070450_AddCompetencyTracking.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Drop the orphaned column first (no FK exists, so no constraint to drop)
    migrationBuilder.DropColumn(
        name: "TrackingItemId",
        table: "CoachingLogs");

    // Create CoachCoacheeMappings table
    migrationBuilder.CreateTable(
        name: "CoachCoacheeMappings",
        columns: table => new
        {
            Id = table.Column<int>(nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            IsActive = table.Column<bool>(nullable: false, defaultValue: true),
            StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
            EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_CoachCoacheeMappings", x => x.Id);
            // No FK to Users — use string IDs, consistent with existing CoachingLog pattern
        });

    // Create CoachingSessions table
    migrationBuilder.CreateTable(
        name: "CoachingSessions",
        columns: table => new
        {
            Id = table.Column<int>(nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            CoachId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            CoacheeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            Date = table.Column<DateTime>(type: "datetime2", nullable: false),
            Topic = table.Column<string>(type: "nvarchar(max)", nullable: false),
            Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Status = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "Draft"),
            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_CoachingSessions", x => x.Id);
        });

    // Create ActionItems table
    migrationBuilder.CreateTable(
        name: "ActionItems",
        columns: table => new
        {
            Id = table.Column<int>(nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            CoachingSessionId = table.Column<int>(nullable: false),
            Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
            DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
            Status = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "Open"),
            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_ActionItems", x => x.Id);
            table.ForeignKey(
                name: "FK_ActionItems_CoachingSessions_CoachingSessionId",
                column: x => x.CoachingSessionId,
                principalTable: "CoachingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        });
}
```

### Pattern 2: Controller — GET + POST for CRUD
**What:** CDPController gains a POST action for creating sessions and a POST action for adding action items.
**When to use:** Standard pattern for all form submissions in this codebase.
**Example:**
```csharp
// Source: CDPController.cs existing GET pattern + CMPController.cs POST pattern
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Coach,Admin")]
public async Task<IActionResult> CreateSession(CreateSessionViewModel model)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    if (!ModelState.IsValid)
    {
        // Reload data for re-render
        return View("Coaching", await BuildCoachingViewModel(user));
    }

    var session = new CoachingSession
    {
        CoachId = user.Id,
        CoacheeId = model.CoacheeId,
        Date = model.Date,
        Topic = model.Topic,
        Notes = model.Notes,
        Status = "Draft",
        CreatedAt = DateTime.UtcNow
    };

    _context.CoachingSessions.Add(session);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Sesi coaching berhasil disimpan.";
    return RedirectToAction("Coaching");
}
```

### Pattern 3: History View with Filtering
**What:** Coaching history view filtered by date range and/or status using query parameters.
**When to use:** COACH-03 requirement — user views their coaching session history.
**Example:**
```csharp
// Source: CDPController.cs existing Coaching() pattern + CMPController.cs filter pattern
public async Task<IActionResult> Coaching(
    DateTime? fromDate = null,
    DateTime? toDate = null,
    string? status = null)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    var userId = user.Id;

    var query = _context.CoachingSessions
        .Include(s => s.ActionItems)
        .Where(s => s.CoachId == userId || s.CoacheeId == userId);

    if (fromDate.HasValue)
        query = query.Where(s => s.Date >= fromDate.Value);
    if (toDate.HasValue)
        query = query.Where(s => s.Date <= toDate.Value);
    if (!string.IsNullOrEmpty(status))
        query = query.Where(s => s.Status == status);

    var sessions = await query.OrderByDescending(s => s.Date).ToListAsync();
    // ...
}
```

### Pattern 4: DbContext Entity Registration
**What:** Register new entities in ApplicationDbContext with relationship configuration and indexes.
**When to use:** Every new entity added to the system.
**Example:**
```csharp
// Source: Data/ApplicationDbContext.cs existing pattern
// In DbSets section:
public DbSet<CoachingSession> CoachingSessions { get; set; }
public DbSet<ActionItem> ActionItems { get; set; }
public DbSet<CoachCoacheeMapping> CoachCoacheeMappings { get; set; }

// In OnModelCreating:
builder.Entity<CoachingSession>(entity =>
{
    entity.HasIndex(s => s.CoachId);
    entity.HasIndex(s => s.CoacheeId);
    entity.HasIndex(s => new { s.CoacheeId, s.Date });
    entity.Property(s => s.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
});

builder.Entity<ActionItem>(entity =>
{
    entity.HasOne(a => a.CoachingSession)
        .WithMany(s => s.ActionItems)
        .HasForeignKey(a => a.CoachingSessionId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasIndex(a => a.CoachingSessionId);
    entity.HasIndex(a => a.Status);
});

builder.Entity<CoachCoacheeMapping>(entity =>
{
    entity.HasIndex(m => m.CoachId);
    entity.HasIndex(m => m.CoacheeId);
    entity.HasIndex(m => new { m.CoachId, m.CoacheeId });
});
```

### Anti-Patterns to Avoid
- **Reusing CoachingLog for Phase 4 sessions:** `CoachingLog` has a Proton-specific schema (SubKompetensi, Deliverables, Kesimpulan, Result). Cramming coaching sessions into it would require awkward null fields and confuse Phase 5. Keep them separate.
- **Adding FK to Users for CoachId/CoacheeId on new tables:** The existing `CoachingLog` correctly uses string IDs without FK constraints to avoid cascade delete complexity. Follow the same pattern for `CoachingSessions` and `CoachCoacheeMappings`. Use indexes instead.
- **Eager loading without `.Include()` then accessing navigation properties:** The existing `CoachingLog` has no navigation properties; the new `CoachingSession` should have `ICollection<ActionItem>` and require `.Include(s => s.ActionItems)` when action items are needed.
- **Modifying the existing Coaching view to use both CoachingLog and CoachingSession:** The view model should use only `CoachingSession`. The existing `CoachingLog` rendering in the stub view is dead code (no records in the table) and can be replaced entirely.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Anti-forgery token on forms | Custom CSRF protection | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` (auto in tag helpers) | Already used everywhere in this codebase |
| Date filter UI | Custom JS date pickers | `<input type="date">` HTML5 | Sufficient for internal portal, matches existing style in AssessmentSession forms |
| Status dropdown values | Custom constants class | String constants in model or ViewModel (same pattern as `UserRoles` static class) | Consistent with codebase conventions |
| Action item status tracking | Custom state machine | Simple string status field ("Open", "In Progress", "Done") | Matches IDP approval pattern (string status columns) |
| Loading coachees for coach | Building org-tree traversal | Query `Users` table by Section + RoleLevel, or query `CoachCoacheeMapping` where CoachId = userId | CoachCoacheeMapping already exists for this purpose |

**Key insight:** This codebase never abstracts data access behind repositories or services. Controllers query DbContext directly. Follow that pattern — no service layer needed for Phase 4.

---

## Common Pitfalls

### Pitfall 1: Broken TrackingItemId Column Causes Migration Conflict
**What goes wrong:** If you add `CoachingSession` without first addressing the `CoachingLog.TrackingItemId`, no immediate error — but EF Core snapshot will remain out of sync with the intent. More importantly, if Phase 5 tries to add `TrackingItem` to DbContext, the column will conflict with the model.
**Why it happens:** The `TrackingItemId` column was created in the initial migration without a FK constraint. EF Core does not know it refers to `TrackingItem` (which has never been in DbContext). The snapshot currently treats it as a plain `int` column.
**How to avoid:** Include `migrationBuilder.DropColumn("TrackingItemId", "CoachingLogs")` in the Phase 4 migration. This removes the orphaned column permanently. The corresponding `CoachingLog.TrackingItemId` C# property should also be removed from the model class to keep model and DB in sync.
**Warning signs:** `dotnet build` succeeds but `dotnet ef migrations add` generates an unexpected diff showing TrackingItemId still present.

### Pitfall 2: CoachCoacheeMapping Not in DbContext
**What goes wrong:** The `CoachCoacheeMapping.cs` model file exists but the class is NOT registered in `ApplicationDbContext`. Attempting to query `_context.CoachCoacheeMappings` will cause a compile error. Phase 5 (Proton assignment) depends on this table existing.
**Why it happens:** The model was created as a stub ahead of time but the DbContext registration was never completed.
**How to avoid:** Register `DbSet<CoachCoacheeMapping> CoachCoacheeMappings` in ApplicationDbContext and include the table creation in the Phase 4 migration.
**Warning signs:** `_context.CoachCoacheeMappings` compile error, or discovering in Phase 5 that the table doesn't exist.

### Pitfall 3: Coach Coachee Selector Requires Real Data
**What goes wrong:** The existing `CDPController.Progress()` uses **mock coachee data** for the Coach role. The coaching session create form needs a real dropdown of coachees. If built against mock data, Phase 5 integration will be harder.
**Why it happens:** `Progress()` has a `// Mock data: Coachees for Coach role` block that pre-dates CoachCoacheeMapping.
**How to avoid:** In Phase 4, populate the coachee dropdown from actual `Users` where `RoleLevel == 6` (Coachee) AND `Section == coach.Section`. Once `CoachCoacheeMapping` is in the DB, the dropdown can optionally also pull from that table if populated.
**Warning signs:** Form works in dev with seed users but breaks in production where no Coachee-role users exist in the coach's section.

### Pitfall 4: Existing Coaching.cshtml Uses List<CoachingLog> as Model
**What goes wrong:** Replacing the view model type without updating the `@model` directive causes a runtime `InvalidCastException` or a Razor compilation error.
**Why it happens:** `Coaching.cshtml` line 1 is `@model List<HcPortal.Models.CoachingLog>`. If the controller returns a different model type, the view throws at runtime.
**How to avoid:** Create a new `CoachingHistoryViewModel` that contains `List<CoachingSession>`, filter parameters, and any summary stats. Update both the `@model` directive and the controller return value together.
**Warning signs:** `dotnet build` passes but the page crashes at runtime with a model-type mismatch error.

### Pitfall 5: Status Filter Requires DateTime Parsing from Query String
**What goes wrong:** Date filter parameters arrive as strings from GET query string. Binding `DateTime?` parameters directly to action parameters works, but locale/format issues can cause binding to silently fail, returning null.
**Why it happens:** ASP.NET Core's model binder uses the server's culture settings. Indonesian locale may format dates differently.
**How to avoid:** Use `<input type="date">` in the view (which always submits ISO 8601 `yyyy-MM-dd`) and bind to `string?` parameters, then parse manually with `DateTime.TryParseExact`. Alternatively, bind `DateTime?` and rely on HTML5 date input standardization.
**Warning signs:** Date filter always returns all records regardless of input value.

### Pitfall 6: Cascade Delete on ActionItems vs Restrict
**What goes wrong:** If `ActionItem` uses `DeleteBehavior.Restrict` with `CoachingSession`, deleting a session with action items will throw a database error without a clear user-facing message.
**Why it happens:** Default EF Core behavior may differ from intended behavior. The existing UserResponse uses `Restrict` specifically to avoid accidental deletions.
**How to avoid:** Use `DeleteBehavior.Cascade` for `ActionItem → CoachingSession`. Action items have no independent meaning outside their parent session. This matches the standard parent-child relationship pattern.
**Warning signs:** `DbUpdateException` when attempting to delete a coaching session.

---

## Code Examples

### New CoachingSession Model
```csharp
// Pattern source: Models/CoachingLog.cs + Models/IdpItem.cs conventions
namespace HcPortal.Models
{
    public class CoachingSession
    {
        public int Id { get; set; }

        // Participants (string IDs, no FK constraint — consistent with CoachingLog pattern)
        public string CoachId { get; set; } = "";
        public string CoacheeId { get; set; } = "";

        // Session Data (COACH-01)
        public DateTime Date { get; set; }
        public string Topic { get; set; } = "";
        public string? Notes { get; set; }

        // Status: "Draft", "Submitted"
        public string Status { get; set; } = "Draft";

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property for Action Items (COACH-02)
        public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    }
}
```

### New ActionItem Model
```csharp
// Pattern source: Models/IdpItem.cs (DueDate, Status pattern)
namespace HcPortal.Models
{
    public class ActionItem
    {
        public int Id { get; set; }

        // FK to parent session
        public int CoachingSessionId { get; set; }
        public CoachingSession? CoachingSession { get; set; }

        // Item Data (COACH-02)
        public string Description { get; set; } = "";
        public DateTime DueDate { get; set; }

        // Status: "Open", "In Progress", "Done"
        public string Status { get; set; } = "Open";

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### Updated CoachingLog (remove TrackingItemId property)
```csharp
// Source: Models/CoachingLog.cs — remove this property:
// public int TrackingItemId { get; set; }  // REMOVE — column being dropped
// All other properties remain unchanged
```

### CoachingHistoryViewModel
```csharp
// Pattern source: Models/ReportsDashboardViewModel.cs, Models/Competency/CompetencyGapViewModel.cs
namespace HcPortal.Models
{
    public class CoachingHistoryViewModel
    {
        // Filter state (for re-rendering the filter form with current values)
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? StatusFilter { get; set; }

        // Data
        public List<CoachingSession> Sessions { get; set; } = new();

        // Summary stats for cards at top of page
        public int TotalSessions => Sessions.Count;
        public int DraftSessions => Sessions.Count(s => s.Status == "Draft");
        public int SubmittedSessions => Sessions.Count(s => s.Status == "Submitted");
        public int TotalActionItems => Sessions.Sum(s => s.ActionItems?.Count ?? 0);
        public int OpenActionItems => Sessions
            .SelectMany(s => s.ActionItems ?? new List<ActionItem>())
            .Count(a => a.Status == "Open");
    }
}
```

### CreateSession POST Action (CDPController)
```csharp
// Pattern source: Controllers/CMPController.cs POST pattern (CreateAssessment)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateSession(CreateSessionViewModel model)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    ModelState.Remove("CoachId"); // Set server-side
    if (!ModelState.IsValid)
    {
        TempData["Error"] = "Form tidak valid. Periksa kembali isian Anda.";
        return RedirectToAction("Coaching");
    }

    var session = new CoachingSession
    {
        CoachId = user.Id,
        CoacheeId = model.CoacheeId,
        Date = model.Date,
        Topic = model.Topic,
        Notes = model.Notes,
        Status = "Draft",
        CreatedAt = DateTime.UtcNow
    };

    _context.CoachingSessions.Add(session);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Sesi coaching berhasil dicatat.";
    return RedirectToAction("Coaching");
}
```

### AddActionItem POST Action
```csharp
// Pattern source: same as CreateSession — standard POST pattern
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddActionItem(int sessionId, AddActionItemViewModel model)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    // Verify the session belongs to this coach
    var session = await _context.CoachingSessions
        .FirstOrDefaultAsync(s => s.Id == sessionId && s.CoachId == user.Id);
    if (session == null) return NotFound();

    var item = new ActionItem
    {
        CoachingSessionId = sessionId,
        Description = model.Description,
        DueDate = model.DueDate,
        Status = "Open",
        CreatedAt = DateTime.UtcNow
    };

    _context.ActionItems.Add(item);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Action item berhasil ditambahkan.";
    return RedirectToAction("Coaching");
}
```

### Coaching GET with Filters (updated)
```csharp
// Source: CDPController.Coaching() existing pattern + CMPController filter pattern
public async Task<IActionResult> Coaching(
    DateTime? fromDate = null,
    DateTime? toDate = null,
    string? status = null)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    var userId = user.Id;

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault();

    var query = _context.CoachingSessions
        .Include(s => s.ActionItems)
        .AsQueryable();

    // Role-based filter: Coach sees sessions they lead; Coachee sees sessions about them
    if (userRole == UserRoles.Coach)
        query = query.Where(s => s.CoachId == userId);
    else
        query = query.Where(s => s.CoacheeId == userId);

    if (fromDate.HasValue) query = query.Where(s => s.Date >= fromDate.Value);
    if (toDate.HasValue) query = query.Where(s => s.Date <= toDate.Value);
    if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);

    var sessions = await query.OrderByDescending(s => s.Date).ToListAsync();

    // Build coachee list for Coach role (for create form dropdown)
    List<ApplicationUser> coacheeList = new();
    if (userRole == UserRoles.Coach || userRole == UserRoles.Admin)
    {
        coacheeList = await _context.Users
            .Where(u => u.Section == user.Section && u.RoleLevel == 6)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    var viewModel = new CoachingHistoryViewModel
    {
        FromDate = fromDate,
        ToDate = toDate,
        StatusFilter = status,
        Sessions = sessions
    };

    ViewBag.CoacheeList = coacheeList;
    ViewBag.UserRole = userRole;
    return View(viewModel);
}
```

---

## State of the Art

| Current State | Phase 4 Target | Notes |
|---------------|----------------|-------|
| `CoachingLog` has orphaned `TrackingItemId` int column in DB (no FK, no data) | Column dropped via migration | Safe to drop — no FK constraint exists per migration 20260209010204 |
| `CoachCoacheeMapping` model file exists but not in DbContext or DB | Table created in Phase 4 migration | Phase 5 depends on this table |
| `Coaching.cshtml` renders `List<CoachingLog>` with fake modal form (no POST) | Renders `CoachingHistoryViewModel` with real form backed by `CoachingSession` | Existing stub is fully replaceable |
| CDPController.Coaching() returns `List<CoachingLog>` | Returns `CoachingHistoryViewModel` | Keeps existing GET route, adds filter params |
| No `CoachingSession` or `ActionItem` entities exist | Both created with single migration | Clean separation from Proton-specific `CoachingLog` |

**Deprecated/removed:**
- `CoachingLog.TrackingItemId` property: Remove from model class + drop from DB. The `TrackingItem` model is only a display DTO (used in CDPController.Progress()), not a DB entity.

---

## Open Questions

1. **Should CoachCoacheeMapping FK to Users be enforced at the DB level?**
   - What we know: Existing `CoachingLog` uses string IDs without FK constraints. The `CoachCoacheeMapping` model has no FKs defined.
   - What's unclear: Whether Phase 5 (Proton assignment) needs to query this table with `.Include()` on the Coach/Coachee navigation properties.
   - Recommendation: Keep consistent with existing pattern (no FK constraints, use indexes). Phase 5 can add navigation properties and FK constraints via a separate migration if needed.

2. **What is the master deliverable data state? (Kompetensi > Sub Kompetensi > Deliverable hierarchy)**
   - What we know: `IdpItem` has `Kompetensi`, `SubKompetensi`, `Deliverable` as nullable string columns (flat, per-user). `CpdpItem` has `NamaKompetensi`, `IndikatorPerilaku`, `DetailIndikator`, `Silabus`, `TargetDeliverable` as flat rows (master data, not hierarchical). `KkjMatrixItem` has competency names but no sub-competency/deliverable hierarchy.
   - What's unclear: There is NO dedicated master deliverable hierarchy table (Kompetensi → Sub Kompetensi → Deliverable) in the current DB. Phase 5 (PROTN-02) requires Coachee to view structured deliverable list — this hierarchy will need to be created, either from the CPDP data or as a new master table.
   - Recommendation for Phase 4: Do NOT attempt to create the deliverable hierarchy table in Phase 4. That is Phase 5 work. Phase 4 only needs to ensure the coaching session data model is correct and stable.

3. **Should `CoachingSession` support coachee assignment from `CoachCoacheeMapping` or direct user selection?**
   - What we know: `CoachCoacheeMapping` will exist in the DB after Phase 4. However, it will have no data (no HC or Admin UI to populate it yet). Phase 5 adds the assignment UI (PROTN-01).
   - What's unclear: Whether Phase 4 should enforce that a session can only be created for an officially mapped coachee.
   - Recommendation: In Phase 4, populate the create-session coachee dropdown from `Users` where `Section == coach.Section && RoleLevel == 6`. This works without CoachCoacheeMapping data. Do not enforce a CoachCoacheeMapping constraint at this stage.

4. **What happens to existing `CoachingLog` records when `TrackingItemId` is dropped?**
   - What we know: The `CoachingLogs` table was created but there is no indication of live production data. The field has no FK constraint so dropping it is a pure column removal, not a cascade operation.
   - What's unclear: Whether there are any rows in `CoachingLogs` in the production database.
   - Recommendation: The migration should simply use `migrationBuilder.DropColumn("TrackingItemId", "CoachingLogs")`. SQL Server allows dropping a column with data (the data is simply lost). Since this column has no FK constraint and holds no meaningful data (TrackingItem is not a DB table), the risk is nil.

---

## Sources

### Primary (HIGH confidence — direct codebase inspection)
- `Models/CoachingLog.cs` — existing model structure and fields
- `Models/CoachCoacheeMapping.cs` — existing model, confirmed not in DbContext
- `Models/TrackingModels.cs` — confirmed TrackingItem is a display-only DTO
- `Data/ApplicationDbContext.cs` — confirmed CoachCoacheeMapping missing from DbSets; confirmed CoachingLog has no FK config for TrackingItemId
- `Migrations/20260209010204_AddAllEntities.cs` — confirmed CoachingLogs table created without FK on TrackingItemId
- `Controllers/CDPController.cs` — confirmed Coaching() GET works; no POST actions exist; mock coachee data confirmed
- `Views/CDP/Coaching.cshtml` — confirmed stub modal with `alert()` onclick (no real POST)
- `HcPortal.csproj` — confirmed .NET 8.0, EF Core 8.0.0, no additional packages needed
- `Program.cs` — confirmed SQL Server only (no SQLite dev/prod split), auto-migrate on startup
- `.planning/codebase/CONVENTIONS.md` — naming, patterns, error handling

### Secondary (HIGH confidence — planning docs from this repo)
- `.planning/REQUIREMENTS.md` — COACH-01, COACH-02, COACH-03 definitions, Phase 4 scope
- `.planning/ROADMAP.md` — Phase 4 success criteria, phase dependencies
- `.planning/phases/03-kkj-cpdp-integration/03-01-PLAN.md` — confirmed plan format, migration pattern used in v1.0

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages are in csproj; no new dependencies needed
- Architecture (new entities): HIGH — models follow identical patterns to existing CoachingLog, IdpItem, UserCompetencyLevel
- Migration strategy (drop TrackingItemId): HIGH — confirmed no FK constraint in migration; confirmed TrackingItem is not a DB entity
- CoachCoacheeMapping missing from DbContext: HIGH — confirmed by reading ApplicationDbContext.cs
- Master deliverable hierarchy: HIGH (it does NOT exist) — confirmed by inspecting all model files and migrations
- Common pitfalls: HIGH — confirmed by reading Coaching.cshtml, CDPController, and migration files directly

**Research date:** 2026-02-17
**Valid until:** 2026-03-17 (stable ASP.NET Core 8.0 stack; findings are codebase-specific and won't change unless someone edits the files)
