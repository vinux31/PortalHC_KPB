# Phase 46: Attempt History - Research

**Researched:** 2026-02-26
**Domain:** ASP.NET Core 8 MVC - EF Core data archival & history tracking
**Confidence:** HIGH

## Summary

Phase 46 implements assessment attempt history archival: when HC resets a completed assessment session, the current attempt data is preserved in a new `AssessmentAttemptHistory` table before the session is cleared. The upgrade to the RecordsWorkerList History tab transforms a unified training+assessment view into two sub-tabs: "Riwayat Assessment" (archived + current completed attempts) and "Riwayat Training" (existing content, moved). The implementation integrates seamlessly with the existing AssessmentSession model, EF Core migration patterns, and ASP.NET Core 8 architecture already established in the project.

**Primary recommendation:** Create `AssessmentAttemptHistory` entity as a simple archival record model; archive ONLY when Reset is called on Completed sessions; split History tab into nested sub-tabs using Bootstrap nav structure already in use; compute Attempt # as count(archived records for that user+title) + 1.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Trigger Archival:** Only the Reset button in HC monitoring creates history records. AbandonExam and ForceClose do NOT trigger archival. Rule: Reset on Completed session → archive → clear session. Reset on anything else → just clear, no archive.
- **History Tab Layout:** Current unified History tab is replaced with 2 sub-tabs: "Riwayat Assessment" and "Riwayat Training" — sub-tabs live inside the existing "History" tab of RecordsWorkerList.
- **Filters on Riwayat Assessment:** Filter by worker name/NIP (search input) AND assessment title (dropdown or search), both combinable.
- **Attempt # Logic:** Sequential per worker per assessment title — current completed session (never reset) = Attempt #1. Abandoned attempts are NOT shown. Attempt # for current session computed as: count of archived attempts for that worker+title + 1.
- **Sort Order in Riwayat Assessment:** Grouped by assessment title, then within each group: date descending (newest attempt first).

### Claude's Discretion
- New table name and schema for archived attempts (e.g., `AssessmentAttemptHistory`)
- Fields to persist per archive: SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber
- EF Core migration approach
- Exact filter UX (dropdown vs text search for assessment title)
- Badge styling for Pass/Fail in the new sub-tab

### Deferred Ideas (OUT OF SCOPE)
- None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| HIST-01 | When HC resets an assessment session, the current attempt data (score, pass/fail, started_at, completed_at, status) is archived as a historical record before the session is cleared | Archival logic triggered in ResetAssessment action; new AssessmentAttemptHistory table stores SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber |
| HIST-02 | HC and Admin can view all historical attempts per worker per assessment in the History tab at /CMP/Records, with an Attempt # column showing sequential attempt number per worker per assessment title | New Riwayat Assessment sub-tab queries unified dataset from AssessmentAttemptHistory + current AssessmentSessions (Status=Completed); controller method filters by UserId, Title, computes AttemptNumber as count+1 |
| HIST-03 | The upgraded History tab displays columns: Nama Pekerja, NIP, Assessment Title, Attempt #, Score, Pass/Fail, Tanggal — showing both archived attempts and current completed sessions | AllWorkersHistoryRow expanded to include AttemptNumber; sub-tab UI mirrors existing History table structure with additional Attempt # column; filtering logic applied in GetAllWorkersHistory() or new dedicated method |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 | Web framework, controllers, views, routing | Project baseline; stable LTS release |
| Entity Framework Core | 8.0 | ORM, migrations, DbContext, relationships | Already integrated; handles all data access |
| SQL Server | Latest | Relational database backend | Proven with KkjMatrices, CpdpItems, AssessmentSessions |
| Bootstrap | 5.x | CSS framework for tab structure and badges | Already in use in RecordsWorkerList.cshtml |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| LINQ (System.Linq) | net8.0 | Query composition, filtering, grouping | Essential for data access patterns in controller |
| EntityFrameworkCore.SqlServer | 8.0 | SQL Server provider for EF Core | Required for Migrations and database operations |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| New AssessmentAttemptHistory table | Archive to JSON blob in AssessmentSession | Loses relational structure for filtering; harder to query Attempt # |
| Bootstrap sub-tabs | Custom JavaScript tab toggle | More code, less accessibility, duplicates existing pattern |
| Computed Attempt # in SQL | Pre-computed AttemptNumber column | Requires triggers or careful sync; computed approach is simpler |

**Installation:**
No new packages needed — ASP.NET Core 8 and EF Core 8 already in project.

## Architecture Patterns

### Recommended Project Structure

New table integrates into existing structure:
```
Models/
├── AssessmentSession.cs       (existing)
├── AssessmentAttemptHistory.cs (NEW — archive record)
└── AllWorkersHistoryRow.cs    (existing, enhanced with AttemptNumber)

Controllers/
├── CMPController.cs           (ResetAssessment modified, GetAllWorkersHistory enhanced)

Data/
├── ApplicationDbContext.cs    (new DbSet<AssessmentAttemptHistory>)
├── Migrations/                (new migration: 202602XX_AddAssessmentAttemptHistory)

Views/
├── CMP/RecordsWorkerList.cshtml (History tab split into sub-tabs)
```

### Pattern 1: Archival on Reset
**What:** When HC clicks Reset on a Completed session, archive current state BEFORE clearing.
**When to use:** Session status = "Completed" AND Reset triggered.
**Example:**
```csharp
// In CMPController.ResetAssessment (after null check, before clearing)
if (assessment.Status == "Completed")
{
    // Archive current attempt
    var history = new AssessmentAttemptHistory
    {
        SessionId = assessment.Id,
        UserId = assessment.UserId,
        Title = assessment.Title,
        Category = assessment.Category,
        Score = assessment.Score,
        IsPassed = assessment.IsPassed,
        StartedAt = assessment.StartedAt,
        CompletedAt = assessment.CompletedAt,
        ArchivedAt = DateTime.UtcNow,
        AttemptNumber = await _context.AssessmentAttemptHistory
            .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title)
            .CountAsync() + 1
    };
    _context.AssessmentAttemptHistory.Add(history);
}

// Then clear session as normal
assessment.Status = "Open";
assessment.Score = null;
// ... etc
```

### Pattern 2: Unified History Query
**What:** Query combines archived records + current completed sessions; project into AllWorkersHistoryRow with computed Attempt #.
**When to use:** Loading History tab data in Records action.
**Example:**
```csharp
private async Task<List<AllWorkersHistoryRow>> GetAllWorkersHistory()
{
    // Archived attempts (with AttemptNumber already stored)
    var archived = await _context.AssessmentAttemptHistory
        .Include(h => h.User)
        .Select(h => new AllWorkersHistoryRow
        {
            WorkerName = h.User.FullName ?? h.UserId,
            WorkerNIP = h.User.NIP,
            RecordType = "Assessment Online",
            Title = h.Title,
            Date = h.CompletedAt ?? h.StartedAt ?? h.ArchivedAt,
            Score = h.Score,
            IsPassed = h.IsPassed,
            AttemptNumber = h.AttemptNumber
        })
        .ToListAsync();

    // Current completed sessions (Attempt # = count archived + 1)
    var current = await _context.AssessmentSessions
        .Include(a => a.User)
        .Where(a => a.Status == "Completed")
        .Select(a => new AllWorkersHistoryRow
        {
            WorkerName = a.User.FullName ?? a.UserId,
            WorkerNIP = a.User.NIP,
            RecordType = "Assessment Online",
            Title = a.Title,
            Date = a.CompletedAt ?? a.Schedule,
            Score = a.Score,
            IsPassed = a.IsPassed,
            AttemptNumber = _context.AssessmentAttemptHistory
                .Count(h => h.UserId == a.UserId && h.Title == a.Title) + 1
        })
        .ToListAsync();

    // Combine and add training records (unchanged)
    // ... existing training logic ...

    return rows.OrderByDescending(r => r.Date).ToList();
}
```

### Pattern 3: Sub-Tab Structure in Razor
**What:** Split existing History tab into nested tabs using Bootstrap nav structure already in view.
**When to use:** Rendering History tab pane in RecordsWorkerList.cshtml.
**Example:**
```html
<!-- Main History Tab Pane (existing) -->
<div class="tab-pane fade" id="history-tab-pane" role="tabpanel" aria-labelledby="history-tab">

    <!-- Nested Sub-Tabs (NEW) -->
    <ul class="nav nav-tabs mb-3" id="historySubTabs" role="tablist">
        <li class="nav-item" role="presentation">
            <button class="nav-link active" id="riwayat-assessment-tab" data-bs-toggle="tab"
                    data-bs-target="#riwayat-assessment-pane" type="button" role="tab">
                <i class="bi bi-file-text me-1"></i> Riwayat Assessment
                <span class="badge bg-secondary ms-1">@assessmentCount</span>
            </button>
        </li>
        <li class="nav-item" role="presentation">
            <button class="nav-link" id="riwayat-training-tab" data-bs-toggle="tab"
                    data-bs-target="#riwayat-training-pane" type="button" role="tab">
                <i class="bi bi-book me-1"></i> Riwayat Training
                <span class="badge bg-secondary ms-1">@trainingCount</span>
            </button>
        </li>
    </ul>

    <div class="tab-content" id="historySubTabsContent">

        <!-- Riwayat Assessment Sub-Tab -->
        <div class="tab-pane fade show active" id="riwayat-assessment-pane" role="tabpanel">
            <!-- Filters: Worker Name/NIP search + Assessment Title dropdown -->
            <div class="row g-2 mb-3">
                <div class="col-md-4">
                    <input type="text" class="form-control" placeholder="Cari nama/NIP..." id="workerFilter">
                </div>
                <div class="col-md-4">
                    <select class="form-select" id="assessmentFilter">
                        <option value="">-- Semua Assessment --</option>
                        <!-- Options populated from data -->
                    </select>
                </div>
            </div>

            <!-- Table: Nama Pekerja, NIP, Assessment Title, Attempt #, Score, Pass/Fail, Tanggal -->
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>Nama Pekerja</th>
                        <th>NIP</th>
                        <th>Assessment Title</th>
                        <th>Attempt #</th>
                        <th>Score</th>
                        <th>Pass/Fail</th>
                        <th>Tanggal</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var row in Model.AssessmentHistory)
                    {
                        <tr>
                            <td>@row.WorkerName</td>
                            <td>@row.WorkerNIP</td>
                            <td>@row.Title</td>
                            <td><strong>@row.AttemptNumber</strong></td>
                            <td>@row.Score</td>
                            <td>
                                @if (row.IsPassed == true)
                                { <span class="badge bg-success">Pass</span> }
                                else if (row.IsPassed == false)
                                { <span class="badge bg-danger">Fail</span> }
                            </td>
                            <td>@row.Date.ToString("dd MMM yyyy")</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>

        <!-- Riwayat Training Sub-Tab (existing content moved here) -->
        <div class="tab-pane fade" id="riwayat-training-pane" role="tabpanel">
            <!-- Existing training history table from Phase 40 -->
        </div>
    </div>
</div>
```

### Anti-Patterns to Avoid
- **Archiving on ForceClose:** ForceClose already sets final Completed state and is meant to finalize the session. Only Reset should trigger archival to preserve the audit trail of resets.
- **Deleting archived records:** Archived data is immutable audit history — never delete or update archived records. If a record needs correction, append a new archive entry.
- **Computing Attempt # at display time:** Avoid expensive COUNT queries on every page load. Either pre-compute in the archive record or cache the result.
- **Storing entire session object:** Archive only the minimal fields needed (Score, IsPassed, dates) — not all UI-related props like BannerColor, Progress, ElapsedSeconds.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Migration from legacy to new table | Custom SQL scripts, stored procedures | EF Core migrations (dotnet ef migrations add) | Migrations handle versioning, rollback, team consistency; SQL scripts are error-prone and hard to track |
| Historical record sequencing | Manual sequence numbers in application logic | EF Core computed columns or database triggers | Database enforces uniqueness, survives restarts; app logic is fragile |
| Filtering by multiple fields | Custom string parsing, manual LINQ | LINQ with .Where() chaining | Type-safe, composable, efficient SQL generation |
| Date handling in archives | String timestamps, local time | DateTime.UtcNow for all archive timestamps | Consistency across timezones; queries work correctly regardless of server timezone |

**Key insight:** EF Core migrations and LINQ are battle-tested for audit/archival patterns. Handrolled solutions introduce concurrency bugs, timezone issues, and fragility with schema evolution.

## Common Pitfalls

### Pitfall 1: Archiving on Wrong Triggers
**What goes wrong:** Code archives on every Reset regardless of Status, or tries to archive on ForceClose. This creates duplicate history entries for already-abandoned sessions or breaks the "only Reset creates history" contract.
**Why it happens:** Developers assume "Reset always means data loss" and archive preemptively. But user intent is: Reset = admin restart; AbandonExam/ForceClose = session lifecycle end. Only Reset needs preservation.
**How to avoid:** Guard archival with explicit Status check: `if (assessment.Status == "Completed") { archive(); }`. Add unit test verifying Completed→Reset archives, but InProgress→Reset does not.
**Warning signs:** History table shows duplicate entries for same session; archived records with Status != "Completed"; ForceClose sessions appearing in history.

### Pitfall 2: N+1 Query in Attempt # Computation
**What goes wrong:** Loop over current completed sessions and count archive records for each: `foreach (session) { count = query.Count(...) }`. Results in 1 initial query + N count queries.
**Why it happens:** Developers compute Attempt # per row without batching. Works on small datasets, catastrophic on 100+ sessions.
**How to avoid:** Batch the count lookup: `var counts = await _context.AssessmentAttemptHistory.GroupBy(h => new { h.UserId, h.Title }).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();` then lookup in-memory.
**Warning signs:** History tab takes >2 seconds to load; database shows repeated SELECT COUNT queries; application logs show high query volume.

### Pitfall 3: Forgetting AttemptNumber Column in AllWorkersHistoryRow
**What goes wrong:** History tab loads but Attempt # column is null/blank for all rows because AllWorkersHistoryRow doesn't have the field yet.
**Why it happens:** AllWorkersHistoryRow was designed for Phase 40 (unified training+assessment) and didn't include attempt sequencing. Developers forget to extend the model.
**How to avoid:** Extend AllWorkersHistoryRow with `public int? AttemptNumber { get; set; }` before writing any archive logic. Update both the model and all projection queries.
**Warning signs:** Column renders but shows no data; Type mismatch errors in view; compiler error on AttemptNumber property access.

### Pitfall 4: Timezone Mismatch in Archived Dates
**What goes wrong:** Archive StartedAt and CompletedAt use local DateTime.Now, but queries compare to UTC. History shows wrong order or wrong dates depending on server timezone.
**Why it happens:** Project uses mixed DateTime handling — some code uses UtcNow, some uses Now. Archives inherit the inconsistency.
**How to avoid:** Always use `DateTime.UtcNow` for all archived timestamps. Query comparisons are reliable and timezone-agnostic.
**Warning signs:** History dates don't match session detail view; sorting by date produces wrong order; tests pass locally but fail in production (different timezone).

### Pitfall 5: Including Unrelated Data in Archive
**What goes wrong:** Developer archives the entire AssessmentSession object (including Progress, BannerColor, AccessToken, ElapsedSeconds, LastActivePage) instead of just the meaningful fields. Bloats the table and creates security exposure.
**Why it happens:** Copying all fields from source entity is the path of least resistance. "Easier than deciding which fields matter."
**How to avoid:** Explicitly list only semantic fields in the archive model: SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber, ArchivedAt. Test that archive table is 9-10 columns, not 20+.
**Warning signs:** Archive table has 20+ columns; includes UI state like BannerColor; secrets like AccessToken are persisted; row size > 200 bytes.

## Code Examples

Verified patterns from official sources:

### Create AssessmentAttemptHistory Model
```csharp
// Source: EF Core 8 conventions (https://learn.microsoft.com/en-us/ef/core/modeling/entity-types)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HcPortal.Models
{
    public class AssessmentAttemptHistory
    {
        [Key]
        public int Id { get; set; }

        // Reference to original session (for audit trail)
        public int SessionId { get; set; }

        // User who took the attempt
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }

        // Assessment metadata
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";

        // Result
        public int? Score { get; set; }
        public bool? IsPassed { get; set; }

        // Timing
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Archive metadata
        public int AttemptNumber { get; set; } // 1, 2, 3...
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### EF Core Migration (Add Table)
```csharp
// Source: EF Core 8 migrations (https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
using Microsoft.EntityFrameworkCore.Migrations;

namespace HcPortal.Data.Migrations
{
    public partial class AddAssessmentAttemptHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentAttemptHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    IsPassed = table.Column<bool>(type: "bit", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAttemptHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAttemptHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttemptHistory_UserId",
                table: "AssessmentAttemptHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttemptHistory_UserId_Title",
                table: "AssessmentAttemptHistory",
                columns: new[] { "UserId", "Title" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentAttemptHistory");
        }
    }
}
```

### Register DbSet in ApplicationDbContext
```csharp
// Source: EF Core 8 DbContext (https://learn.microsoft.com/en-us/ef/core/dbcontext)
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // ... existing DbSets ...

    // NEW: Archive table
    public DbSet<AssessmentAttemptHistory> AssessmentAttemptHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ... existing configurations ...

        // NEW: Configure AssessmentAttemptHistory
        builder.Entity<AssessmentAttemptHistory>(entity =>
        {
            entity.HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(h => h.UserId);
            entity.HasIndex(h => new { h.UserId, h.Title }); // For counting attempts per user+title
            entity.Property(h => h.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
```

### Archival Logic in ResetAssessment
```csharp
// Source: Current project pattern in CMPController.ResetAssessment
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ResetAssessment(int id)
{
    var assessment = await _context.AssessmentSessions
        .FirstOrDefaultAsync(a => a.Id == id);

    if (assessment == null) return NotFound();

    // NEW: Archive only if Status = Completed
    if (assessment.Status == "Completed")
    {
        // Count existing attempts for this user+title
        int existingAttempts = await _context.AssessmentAttemptHistory
            .Where(h => h.UserId == assessment.UserId && h.Title == assessment.Title)
            .CountAsync();

        var history = new AssessmentAttemptHistory
        {
            SessionId = assessment.Id,
            UserId = assessment.UserId,
            Title = assessment.Title,
            Category = assessment.Category,
            Score = assessment.Score,
            IsPassed = assessment.IsPassed,
            StartedAt = assessment.StartedAt,
            CompletedAt = assessment.CompletedAt,
            AttemptNumber = existingAttempts + 1,
            ArchivedAt = DateTime.UtcNow
        };
        _context.AssessmentAttemptHistory.Add(history);
    }

    // EXISTING: Clear session (delete responses, assignments, reset state)
    var responses = await _context.UserResponses
        .Where(r => r.AssessmentSessionId == id)
        .ToListAsync();
    if (responses.Any())
        _context.UserResponses.RemoveRange(responses);

    var packageResponses = await _context.PackageUserResponses
        .Where(r => r.AssessmentSessionId == id)
        .ToListAsync();
    if (packageResponses.Any())
        _context.PackageUserResponses.RemoveRange(packageResponses);

    var assignment = await _context.UserPackageAssignments
        .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
    if (assignment != null)
        _context.UserPackageAssignments.Remove(assignment);

    // Reset session to Open state
    assessment.Status = "Open";
    assessment.Score = null;
    assessment.IsPassed = null;
    assessment.CompletedAt = null;
    assessment.StartedAt = null;
    assessment.ElapsedSeconds = 0;
    assessment.LastActivePage = null;
    assessment.Progress = 0;
    assessment.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    // EXISTING: Audit log and redirect
    var rsUser = await _userManager.GetUserAsync(User);
    var rsActorName = $"{rsUser?.NIP ?? "?"} - {rsUser?.FullName ?? "Unknown"}";
    await _auditLog.LogAsync(
        rsUser?.Id ?? "",
        rsActorName,
        "ResetAssessment",
        $"Reset assessment '{assessment.Title}' for user {assessment.UserId} [ID={id}]",
        id,
        "AssessmentSession");

    TempData["Success"] = "Sesi ujian telah direset. Peserta dapat mengikuti ujian kembali.";
    return RedirectToAction("AssessmentMonitoringDetail", new
    {
        title = assessment.Title,
        category = assessment.Category,
        scheduleDate = assessment.Schedule
    });
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual training records only | Training + Assessment online records unified | Phase 40 (2026-02-25) | Enabled complete audit trail for competency tracking |
| Single History tab (training + assessment mixed) | Split into sub-tabs (Assessment history + Training history) | Phase 46 (current) | Clearer UX, easier to filter by assessment attempts |
| Lost reset history (session cleared, data gone) | Persistent attempt history with archival | Phase 46 (current) | Full audit trail for compliance and worker development tracking |

**Deprecated/outdated:**
- None for this phase. AllWorkersHistoryRow is current and will be extended, not replaced.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (project-standard, if configured) or MSTest; currently no .Tests project detected — see Wave 0 |
| Config file | None — see Wave 0 |
| Quick run command | `dotnet test` (if .Tests project exists) or manual integration test via UI |
| Full suite command | `dotnet test` (all test projects in solution) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| HIST-01 | Reset on Completed session archives attempt + clears session; Reset on non-Completed session clears without archiving | unit/integration | `dotnet test --filter Category=ResetAssessment` | ❌ Wave 0 — need test project |
| HIST-02 | Riwayat Assessment tab displays archived + current completed attempts; filters by worker name/NIP and assessment title | integration/e2e | Manual: navigate /CMP/Records, apply filters, verify rows match criteria | ❌ Wave 0 |
| HIST-03 | History table shows columns: Nama Pekerja, NIP, Title, Attempt #, Score, Pass/Fail, Tanggal; Attempt # computed correctly per user+title | unit/integration | `dotnet test --filter Category=AttemptNumbering` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** Manual integration test: reset a completed session, verify archive created and session cleared.
- **Per wave merge:** Full /CMP/Records smoke test: load History tab, apply filters, verify table matches expectations.
- **Phase gate:** Audit trail verification: check AssessmentAttemptHistory table populated correctly before `/gsd:verify-work`.

### Wave 0 Gaps
- [ ] `Tests/Integration/ResetAssessmentTests.cs` — covers HIST-01 (archival trigger, status transitions)
- [ ] `Tests/Integration/AttemptHistoryViewTests.cs` — covers HIST-02 and HIST-03 (tab UI, filtering, Attempt # computation)
- [ ] Test project setup: `HcPortal.Tests.csproj` with xUnit + FluentAssertions
- [ ] Integration test fixtures: ApplicationDbContext test double, seeded user/assessment data

*(Note: The project does not currently have a test infrastructure. Recommend creating a basic test project in Wave 0 before implementation. Manual verification via UI is acceptable if test infrastructure setup is deferred.)*

## Open Questions

1. **Test Framework**
   - What we know: Project has no visible .Tests project or xUnit/MSTest references.
   - What's unclear: Should Phase 46 include test infrastructure setup, or defer to later phase?
   - Recommendation: Include simple integration tests for archival logic and view filtering. Can use in-memory DbContext for unit tests; defer full E2E testing infrastructure.

2. **Filter UI Details**
   - What we know: Assessment title filter can be dropdown or text search (Claude's discretion).
   - What's unclear: Should dropdown be hardcoded list of titles, or dynamically populated from database?
   - Recommendation: Dynamically populated. Pre-fetch distinct Assessment titles from AssessmentAttemptHistory + AssessmentSessions (Completed). Reduces maintenance burden.

3. **Performance of Attempt # Computation**
   - What we know: COUNT query per row could be slow at scale.
   - What's unclear: Is 500+ sessions expected in the first release?
   - Recommendation: Use batch COUNT approach (GroupBy + ToList) for now. Monitor with profiler. If slow, pre-compute in the migration or cache.

4. **Sorting Within Assessment Title Groups**
   - What we know: User decided "grouped by title, then date descending" (newest first).
   - What's unclear: Should sub-groups also show Pass attempts before Fail, or strictly by date?
   - Recommendation: Strictly by date descending. Keeps UI predictable and sorting logic simple.

## Sources

### Primary (HIGH confidence)
- **EF Core 8 Documentation** (https://learn.microsoft.com/en-us/ef/core/) — Entity relationships, migrations, DbContext patterns verified
- **ASP.NET Core 8 MVC** (https://learn.microsoft.com/en-us/aspnet/core) — Controller action patterns, routing, Razor view syntax verified
- **Project codebase** (HcPortal.csproj, ApplicationDbContext.cs, CMPController.cs) — Existing migration patterns, model structure, controller patterns from Phases 1-45

### Secondary (MEDIUM confidence)
- **Bootstrap 5 Tabs** (https://getbootstrap.com/docs/5.0/components/navs-tabs/) — Nested tab structure used in existing RecordsWorkerList.cshtml
- **SQL Server datetime handling** — Project baseline confirmed with AssessmentSession timestamps and CreatedAt audit fields

### Tertiary (LOW confidence)
- None — all critical findings verified with project code or official documentation.

## Metadata

**Confidence breakdown:**
- **Standard stack:** HIGH — ASP.NET Core 8, EF Core 8, SQL Server already fully integrated in codebase
- **Architecture:** HIGH — Archival and history patterns match existing audit patterns (AuditLog service, CreatedAt fields); sub-tab structure follows Phase 40 precedent
- **Pitfalls:** HIGH — Derived from common EF Core archival mistakes and project-specific patterns observed in ResetAssessment method

**Research date:** 2026-02-26
**Valid until:** 2026-03-26 (30 days — stable technologies, no breaking API changes expected)

**Research conducted by:** Claude Code (gsd-phase-researcher)
**Phase:** 46 - Attempt History
**Status:** Ready for planning
