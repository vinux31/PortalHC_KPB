# Phase 33: ProtonTrack Schema - Research

**Researched:** 2026-02-23
**Domain:** Entity Framework Core schema migration, FK refactoring, data backfill
**Confidence:** HIGH

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Track DisplayName format:**
- Format: `"Panelman - Tahun 1"` (TrackType + ` - ` + Tahun label in Indonesian)
- Language: Indonesian — `Tahun`, not `Year`
- Not editable by HC/Admin — DisplayName is auto-generated from TrackType + TahunKe at seed/migration time
- Storage: Store as a real DB column (set at seed time) since Phase 34 needs it in a dropdown query

**ProtonTrackAssignment scope:**
- Phase 33 migrates **both** `ProtonKompetensi` AND `ProtonTrackAssignment` to use `ProtonTrackId` FK
- Old `TrackType`+`TahunKe` string columns dropped from both tables in the same migration (no backup columns)
- Rationale: Phase 33's goal is eliminating all string dependencies; leaving `ProtonTrackAssignment` as strings defeats the purpose
- `AssignTrack` action will receive `ProtonTrackId` directly (selected from dropdown), not TrackType+TahunKe strings — no internal lookup needed

**Track data completeness:**
- Exactly 6 tracks exist: Panelman×3 (Tahun 1/2/3) + Operator×3 (Tahun 1/2/3)
- Migration uses **defensive** approach: reads distinct TrackType+TahunKe combinations from existing `ProtonKompetensi` rows to build `ProtonTrack` rows dynamically (safe if data ever drifted)
- After Phase 33, `SeedProtonData.cs` seeds **ProtonTrack rows only** — Kompetensi/SubKompetensi/Deliverable catalog items are managed via the Phase 35 UI going forward
- Existing production data is preserved by the data migration (backfills `ProtonTrackId` on all existing rows)
- Fresh dev installs: only ProtonTrack rows exist after seed; catalog items must be added via the Phase 35 catalog UI

### Claude's Discretion

- **DisplayName storage:** Store in DB column for query efficiency (Phase 34's dropdown queries it directly)
- **EF migration structure:** One migration combining add table + backfill + drop cols preferred for atomicity
- **ProtonTrackAssignment FK nullability during migration:** Make nullable during backfill, then make non-null via constraint

### Deferred Ideas (OUT OF SCOPE)

- Navigation placement for Proton Catalog Manager (HC/Admin nav link) — Phase 34 (CAT-09)
- Ability for HC to customize DisplayName when creating a new track — Phase 34's Add Track modal can accept it as an input; user may want this later

---

## Summary

Phase 33 introduces `ProtonTrack` as a dedicated, normalized entity/table to eliminate string-based track identification (`TrackType`+`TahunKe` duplications) from the Proton schema. Currently, both `ProtonKompetensi` and `ProtonTrackAssignment` store TrackType and TahunKe as strings, creating denormalization risks and query inefficiency. The phase creates a single source of truth with exactly 6 tracks (Panelman/Operator × Tahun 1/2/3), adds a `ProtonTrackId` FK to both tables, backfills all existing rows defensively by reading distinct combinations from `ProtonKompetensi`, and drops the string columns.

The schema change is accompanied by seed data simplification: `SeedProtonData.cs` will seed **only** the 6 ProtonTrack rows; Kompetensi/SubKompetensi/Deliverable catalog data is frozen at Phase 33 and managed via Phase 35's new UI going forward. The `AssignTrack` controller action (used by HC) will accept a `ProtonTrackId` directly from a dropdown instead of TrackType+TahunKe strings.

This is a pure schema/data refactor with no UI changes in Phase 33. Phase 34 builds the catalog management UI; Phase 35 adds the dynamic catalog editor.

**Primary recommendation:** Implement one migration that (1) creates ProtonTrack table with TrackType, TahunKe, DisplayName, and Urutan columns; (2) reads distinct (TrackType, TahunKe) from ProtonKompetensi, generates DisplayName as "TrackType - Tahun N", seeds 6 rows; (3) adds nullable ProtonTrackId FK to ProtonKompetensi and ProtonTrackAssignment; (4) backfills FK values via defensive lookup (select ProtonTrackId where TrackType and TahunKe match); (5) alters FK to NOT NULL; (6) drops TrackType and TahunKe columns. Update SeedProtonData.cs to seed only ProtonTrack. Update CDPController to query ProtonTrackId instead of TrackType+TahunKe strings.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Entity Framework Core | 8.0 | ORM for schema migration, FK relationships, LINQ querying | Already in stack for all database operations; proven pattern in codebase (see Phase 5 migration 20260217063156_AddProtonDeliverableTracking.cs) |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0 | SQL Server EF provider for SQL generation and migrations | Paired with EF Core 8.0; codebase uses SQL Server exclusively |
| ASP.NET Core (MVC) | 8.0 | Controller action updates to use ProtonTrackId instead of strings | Standard framework; no new packages needed |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| DbContext OnModelCreating | (EF built-in) | Fluent configuration of FK, indexes, cascade rules | Required for setting DeleteBehavior, Index definitions, constraints on ProtonTrack and related tables |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Single migration (add + backfill + drop) | Split into 3 migrations (add FK, backfill, drop cols) | Single migration is atomic and safer; split migrations increase risk of manual intervention and data loss between steps |
| Defensive backfill via LINQ in migration | Hardcoded SQL values | LINQ is readable but migrations must use raw SQL; SQL is more explicit but harder to validate |
| DisplayName DB column | Computed property at runtime | DB column allows Phase 34 to query/sort by DisplayName directly in dropdown; runtime computation requires post-query transformation |
| ProtonTrackId FK (non-null final) | Keep as nullable | Final non-null ensures data integrity and simplifies downstream logic (no null checks) |

**Installation:** No new packages required. EF Core 8.0 and SqlServer provider are already in csproj.

---

## Architecture Patterns

### Recommended Project Structure
```
Data/
├── ApplicationDbContext.cs          # Add DbSet<ProtonTrack>, configure FK & cascade delete
├── Migrations/
│   ├── 20260223HHMMSS_CreateProtonTrackTable.cs       # Single migration: create + backfill + drop
│   └── ApplicationDbContextModelSnapshot.cs            # (auto-updated)
├── SeedProtonData.cs                # Update to seed ProtonTrack only; remove Kompetensi seeding

Models/
├── ProtonModels.cs                  # Add ProtonTrack entity class

Controllers/
├── CDPController.cs                 # Update PlanIdp and related methods to use ProtonTrackId
```

### Pattern 1: ProtonTrack Entity Definition
**What:** New entity class representing a single track (e.g., "Panelman - Tahun 1") with TrackType, TahunKe, DisplayName (computed at seed), and ordered collection of Kompetensi.

**When to use:** New entity whenever you need a normalized, queryable reference to track identifiers instead of string pairs.

**Example:**
```csharp
// Source: Codebase pattern from ProtonKompetensi (ProtonModels.cs)
public class ProtonTrack
{
    public int Id { get; set; }
    /// <summary>Values: "Panelman" or "Operator"</summary>
    public string TrackType { get; set; } = "";
    /// <summary>Values: "Tahun 1", "Tahun 2", "Tahun 3"</summary>
    public string TahunKe { get; set; } = "";
    /// <summary>Auto-generated display name: e.g. "Panelman - Tahun 1"</summary>
    public string DisplayName { get; set; } = "";
    /// <summary>Display order in UI dropdowns (1-6)</summary>
    public int Urutan { get; set; }

    // Navigation property — one Track has many Kompetensi
    public ICollection<ProtonKompetensi> KompetensiList { get; set; } = new List<ProtonKompetensi>();
}
```

### Pattern 2: FK Configuration in DbContext OnModelCreating
**What:** Configure cascade delete from ProtonTrack → ProtonKompetensi → ProtonSubKompetensi → ProtonDeliverable (existing pattern extends with new top-level Track), and separate FK for ProtonTrackAssignment.

**When to use:** For defining relationship behaviors, indexes, and constraints in EF Core.

**Example:**
```csharp
// Source: ApplicationDbContext.cs OnModelCreating (existing Proton pattern, lines 240-277)
// ProtonKompetensi -> ProtonTrack (new)
builder.Entity<ProtonKompetensi>(entity =>
{
    entity.HasOne<ProtonTrack>()
        .WithMany(t => t.KompetensiList)
        .HasForeignKey(k => k.ProtonTrackId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasIndex(k => k.ProtonTrackId);
});

// ProtonTrackAssignment -> ProtonTrack (new)
builder.Entity<ProtonTrackAssignment>(entity =>
{
    entity.HasOne<ProtonTrack>()
        .WithMany()
        .HasForeignKey(a => a.ProtonTrackId)
        .OnDelete(DeleteBehavior.Restrict);
    entity.HasIndex(a => a.ProtonTrackId);
});
```

### Pattern 3: Defensive Data Backfill in Migration
**What:** Read distinct (TrackType, TahunKe) combinations from ProtonKompetensi table, insert corresponding rows into ProtonTrack with generated DisplayName, then update existing rows with the correct ProtonTrackId.

**When to use:** For non-breaking data migrations that preserve all existing rows and safely establish new FK relationships.

**Key Detail:** Use `migrationBuilder.Sql()` for INSERT and UPDATE operations that cannot be expressed via CreateTable/AddColumn fluent methods.

**Example:**
```csharp
// Source: EF Core migration pattern (Phase 5 reference: 20260217063156_AddProtonDeliverableTracking.cs)
// After adding ProtonTrackId columns as nullable:

// 1. Insert 6 ProtonTrack rows
var distinctTracks = new[] {
    ("Panelman", "Tahun 1"),
    ("Panelman", "Tahun 2"),
    ("Panelman", "Tahun 3"),
    ("Operator", "Tahun 1"),
    ("Operator", "Tahun 2"),
    ("Operator", "Tahun 3")
};

int trackId = 1;
foreach (var (trackType, tahunKe) in distinctTracks)
{
    var displayName = $"{trackType} - {tahunKe}";
    migrationBuilder.InsertData(
        table: "ProtonTracks",
        columns: new[] { "TrackType", "TahunKe", "DisplayName", "Urutan" },
        values: new object[] { trackType, tahunKe, displayName, trackId++ }
    );
}

// 2. Backfill ProtonKompetensi.ProtonTrackId
migrationBuilder.Sql(@"
    UPDATE pk
    SET pk.ProtonTrackId = pt.Id
    FROM ProtonKompetensiList pk
    INNER JOIN ProtonTracks pt
        ON pk.TrackType = pt.TrackType
        AND pk.TahunKe = pt.TahunKe
    WHERE pk.ProtonTrackId IS NULL
");

// 3. Backfill ProtonTrackAssignment.ProtonTrackId
migrationBuilder.Sql(@"
    UPDATE pta
    SET pta.ProtonTrackId = pt.Id
    FROM ProtonTrackAssignments pta
    INNER JOIN ProtonTracks pt
        ON pta.TrackType = pt.TrackType
        AND pta.TahunKe = pt.TahunKe
    WHERE pta.ProtonTrackId IS NULL
");

// 4. Alter FK to NOT NULL
migrationBuilder.AlterColumn<int>(
    name: "ProtonTrackId",
    table: "ProtonKompetensiList",
    type: "int",
    nullable: false,
    oldClrType: typeof(int),
    oldType: "int",
    oldNullable: true);

migrationBuilder.AlterColumn<int>(
    name: "ProtonTrackId",
    table: "ProtonTrackAssignments",
    type: "int",
    nullable: false,
    oldClrType: typeof(int),
    oldType: "int",
    oldNullable: true);

// 5. Drop old string columns
migrationBuilder.DropColumn(name: "TrackType", table: "ProtonKompetensiList");
migrationBuilder.DropColumn(name: "TahunKe", table: "ProtonKompetensiList");
migrationBuilder.DropColumn(name: "TrackType", table: "ProtonTrackAssignments");
migrationBuilder.DropColumn(name: "TahunKe", table: "ProtonTrackAssignments");
```

### Pattern 4: Updated Seed Data (ProtonTrack Only)
**What:** Replace Kompetensi seeding with ProtonTrack seeding. Kompetensi/SubKompetensi/Deliverable data is managed by Phase 35 UI.

**When to use:** After Phase 33 migration, fresh dev installs start with empty Kompetensi but populated ProtonTrack.

**Example:**
```csharp
// Source: SeedProtonData.cs (updated for Phase 33)
public static async Task SeedAsync(ApplicationDbContext context)
{
    // Skip if already seeded
    if (await context.ProtonTracks.AnyAsync())
    {
        Console.WriteLine("ℹ️ ProtonTrack data already exists, skipping...");
        return;
    }

    var tracks = new List<ProtonTrack>
    {
        new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 1", DisplayName = "Panelman - Tahun 1", Urutan = 1 },
        new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 2", DisplayName = "Panelman - Tahun 2", Urutan = 2 },
        new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 3", DisplayName = "Panelman - Tahun 3", Urutan = 3 },
        new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 1", DisplayName = "Operator - Tahun 1", Urutan = 4 },
        new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 2", DisplayName = "Operator - Tahun 2", Urutan = 5 },
        new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 3", DisplayName = "Operator - Tahun 3", Urutan = 6 },
    };

    await context.ProtonTracks.AddRangeAsync(tracks);
    await context.SaveChangesAsync();
    Console.WriteLine("✅ Seeded 6 ProtonTrack rows (Panelman/Operator × Tahun 1/2/3) successfully!");
}
```

### Pattern 5: Controller Update — Query by ProtonTrackId Instead of Strings
**What:** Update CDPController.PlanIdp to load assignment via `ProtonTrackId` FK and navigate to track, instead of filtering Kompetensi by `TrackType` + `TahunKe` strings.

**When to use:** After migration, whenever accessing track-specific data.

**Key Change:**
```csharp
// OLD (Phase 32):
var assignment = await _context.ProtonTrackAssignments
    .Where(a => a.CoacheeId == user.Id && a.IsActive)
    .FirstOrDefaultAsync();
var kompetensiList = await _context.ProtonKompetensiList
    .Include(k => k.SubKompetensiList)
    .Where(k => k.TrackType == assignment.TrackType && k.TahunKe == assignment.TahunKe)
    .ToListAsync();

// NEW (Phase 33):
var assignment = await _context.ProtonTrackAssignments
    .Include(a => a.ProtonTrack)  // NEW navigation property
    .Where(a => a.CoacheeId == user.Id && a.IsActive)
    .FirstOrDefaultAsync();
var kompetensiList = await _context.ProtonKompetensiList
    .Include(k => k.SubKompetensiList)
    .Where(k => k.ProtonTrackId == assignment.ProtonTrackId)  // FK instead of strings
    .ToListAsync();

// ViewModel still exposes TrackType/TahunKe for UI display if needed
var viewModel = new ProtonPlanViewModel
{
    TrackType = assignment.ProtonTrack.TrackType,  // From FK now
    TahunKe = assignment.ProtonTrack.TahunKe,
    KompetensiList = kompetensiList,
};
```

### Anti-Patterns to Avoid
- **Leaving string columns during backfill:** Don't skip the drop-column step; leaving TrackType+TahunKe defeats the entire normalization goal. Data validation must happen in migration, not left for future cleanup.
- **Using hardcoded track IDs in code:** Don't assume ProtonTrackId = 1 for Panelman Tahun 1; always query or load via FK. Hardcoded IDs break on seed order changes.
- **Querying by string instead of FK:** After Phase 33, never write `.Where(k => k.TrackType == "Panelman")` again; always use `.Where(k => k.ProtonTrackId == trackId)`.
- **Nullable FK without final constraint:** Don't leave ProtonTrackId nullable in the final schema; make it non-null to enforce referential integrity and eliminate null-check branches downstream.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| FK backfill logic | Custom C# loop to find matching tracks | EF Core migration SQL backfill | Migration runs atomically in DB; C# logic running outside migration risks partial updates, deadlocks, and rollback issues. SQL is transactional. |
| DisplayName generation | Custom string concatenation logic | Seed-time "TrackType + ' - ' + TahunKe" in migration | DisplayName is set once at seed/migration; no need for runtime computation. Storing in DB allows Phase 34 dropdown to query/sort by DisplayName directly. |
| Track existence validation | Custom check in controller | FK constraint + NOT NULL | Database enforces FK validity; no need to validate in code. Data-level integrity is guaranteed. |
| Cascade delete ordering | Manual deletion of child rows | EF Core DeleteBehavior.Cascade | EF Core and SQL Server handle delete cascades atomically; manual deletion is error-prone and expensive. |

**Key insight:** Schema normalization is a data-layer concern; the database and migrations handle complexity so the application layer remains simple. Hand-rolling any of these patterns introduces race conditions, data loss risks, and maintenance burden.

---

## Common Pitfalls

### Pitfall 1: Incomplete Backfill — Forgetting ProtonTrackAssignment
**What goes wrong:** Developer writes migration that only backfills ProtonKompetensi.ProtonTrackId but forgets ProtonTrackAssignment. Then Phase 34 tries to query assignments by track, encountering NULLs and filtering them out silently — HC can't see assigned tracks.

**Why it happens:** Two separate tables with similar schema; easy to remember one and forget the other. The CONTEXT.md specifically says "migrate **both**" but it's easy to miss.

**How to avoid:** Checklist before finalizing migration: (1) ProtonTrack rows inserted? (2) ProtonKompetensi.ProtonTrackId backfilled? (3) ProtonTrackAssignment.ProtonTrackId backfilled? (4) Both columns made NOT NULL? (5) Both old string columns dropped?

**Warning signs:** In test environment, HC can't select a track in AssignTrack modal, or Phase 34 dropdown is empty. Run `SELECT COUNT(*) FROM ProtonTrackAssignments WHERE ProtonTrackId IS NULL` — if count > 0, backfill failed.

### Pitfall 2: DisplayName Mismatch — Inconsistent Formatting
**What goes wrong:** Seed data creates DisplayName as "Panelman - Tahun 1" (space-dash-space), but migration inserts "Panelman-Tahun 1" (no spaces). Phase 34 UI shows two different formats; users confused about which one is canonical.

**Why it happens:** Multiple places generate DisplayName; no single source of truth. Easy to copy-paste and accidentally change the format.

**How to avoid:** Define DisplayName format exactly once in a constant or helper method. Use the same format in: (1) migration InsertData, (2) SeedProtonData.cs, (3) any controller code that displays/logs it.

```csharp
// Define once:
private static string GetDisplayName(string trackType, string tahunKe)
    => $"{trackType} - {tahunKe}";
```

**Warning signs:** Dropdown in Phase 34 UI shows inconsistent formatting. Run `SELECT DISTINCT DisplayName FROM ProtonTracks` — if you see both "Panelman - Tahun 1" and "Panelman-Tahun 1", you have a mismatch.

### Pitfall 3: FK Constraint Violation During Backfill
**What goes wrong:** Migration adds non-null FK to ProtonKompetensi, then tries to backfill. But if a Kompetensi row has a (TrackType, TahunKe) combination that doesn't exist in ProtonTrack, the UPDATE fails — migration rolls back.

**Why it happens:** Assumption that all existing Kompetensi rows map to one of the 6 expected tracks. But if data drifted (e.g., typo: "Panelman " instead of "Panelman"), backfill breaks.

**How to avoid:** Make FK **nullable** initially, backfill defensively (INNER JOIN only matches existing tracks, orphans left with NULL), then investigate orphans, fix them manually, then make FK non-null. Or use defensive UPSERT in migration to ensure all (TrackType, TahunKe) pairs have ProtonTrack rows before backfilling.

```sql
-- Defensive: Create missing tracks on the fly
INSERT INTO ProtonTracks (TrackType, TahunKe, DisplayName, Urutan)
SELECT DISTINCT pk.TrackType, pk.TahunKe,
       pk.TrackType + ' - ' + pk.TahunKe,
       ROW_NUMBER() OVER (ORDER BY pk.TrackType, pk.TahunKe)
FROM ProtonKompetensiList pk
LEFT JOIN ProtonTracks pt ON pk.TrackType = pt.TrackType AND pk.TahunKe = pt.TahunKe
WHERE pt.Id IS NULL;
```

**Warning signs:** Migration fails with FK constraint error. Check logs for "Violation of PRIMARY KEY/FOREIGN KEY constraint". Run `SELECT DISTINCT TrackType, TahunKe FROM ProtonKompetensiList` and manually verify all rows exist in ProtonTracks.

### Pitfall 4: String Columns Not Dropped — Silent Data Duplication
**What goes wrong:** Migration adds ProtonTrackId FK and backfills, but the final step (DROP COLUMN TrackType, TahunKe) is omitted or commented out "for rollback safety". Months later, code still references the old string columns, defeating the normalization goal. Phase 34 assumes strings are gone, tries to use FK, but old code still queries strings — data gets out of sync.

**Why it happens:** Fear of breaking rollback path; temptation to keep "backup" columns "just in case". Seems safe but creates hidden duplication and confusion.

**How to avoid:** Drop columns in the same migration. If rollback is needed, rollback the entire migration, not individual steps. The CONTEXT.md explicitly says "no backup columns" — trust the decision.

**Warning signs:** Migration has 6+ months of age, and new code still encounters `assignment.TrackType` references. Search codebase for `.TrackType` and `.TahunKe` after Phase 33 ships — should only appear in ProtonTrack entity, not ProtonKompetensi or ProtonTrackAssignment.

### Pitfall 5: Phase 34 Dropdown Query Broken by NULL ProtonTrackId
**What goes wrong:** Phase 34 adds a ProtonTrackId dropdown in the AssignTrack modal, querying `SELECT DisplayName FROM ProtonTracks WHERE IsActive = true`. But if backfill was incomplete, some tracks never got inserted, and dropdown is empty or partial.

**Why it happens:** Migration assumes 6 hardcoded tracks; Phase 35 will allow admins to add custom tracks. If Phase 33 migration doesn't seed all 6, later code breaks. Or backfill script was skipped in production by mistake.

**How to avoid:** After migration, run verification query: `SELECT COUNT(*) FROM ProtonTracks` — must equal 6. Run `SELECT COUNT(*) FROM ProtonKompetensiList WHERE ProtonTrackId IS NULL` — must equal 0 (if Kompetensi seeding is empty, that's OK; if there are orphans, backfill failed).

**Warning signs:** Phase 34 tests fail with "No tracks available" or dropdown is empty. Phase 31's assigment history queries show NULL ProtonTrackId.

---

## Code Examples

Verified patterns from existing codebase:

### Example 1: EF Core Migration Pattern (Create Table + FK)
```csharp
// Source: 20260217063156_AddProtonDeliverableTracking.cs (lines 14-88)
// Shows how existing ProtonKompetensi table was created with FK to parent

protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "ProtonTracks",
        columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            TrackType = table.Column<string>(type: "nvarchar(50)", nullable: false),
            TahunKe = table.Column<string>(type: "nvarchar(50)", nullable: false),
            DisplayName = table.Column<string>(type: "nvarchar(100)", nullable: false),
            Urutan = table.Column<int>(type: "int", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_ProtonTracks", x => x.Id);
            table.UniqueConstraint("UQ_ProtonTracks_TrackType_TahunKe", x => new { x.TrackType, x.TahunKe });
        });

    // Add FK columns as nullable first
    migrationBuilder.AddColumn<int>(
        name: "ProtonTrackId",
        table: "ProtonKompetensiList",
        type: "int",
        nullable: true);

    migrationBuilder.AddColumn<int>(
        name: "ProtonTrackId",
        table: "ProtonTrackAssignments",
        type: "int",
        nullable: true);

    // Create FK constraints
    migrationBuilder.CreateIndex(
        name: "IX_ProtonKompetensiList_ProtonTrackId",
        table: "ProtonKompetensiList",
        column: "ProtonTrackId");

    migrationBuilder.AddForeignKey(
        name: "FK_ProtonKompetensiList_ProtonTracks_ProtonTrackId",
        table: "ProtonKompetensiList",
        column: "ProtonTrackId",
        principalTable: "ProtonTracks",
        principalColumn: "Id",
        onDelete: ReferentialAction.Cascade);

    migrationBuilder.CreateIndex(
        name: "IX_ProtonTrackAssignments_ProtonTrackId",
        table: "ProtonTrackAssignments",
        column: "ProtonTrackId");

    migrationBuilder.AddForeignKey(
        name: "FK_ProtonTrackAssignments_ProtonTracks_ProtonTrackId",
        table: "ProtonTrackAssignments",
        column: "ProtonTrackId",
        principalTable: "ProtonTracks",
        principalColumn: "Id",
        onDelete: ReferentialAction.Restrict);
}
```

### Example 2: DbContext Configuration (OnModelCreating)
```csharp
// Source: ApplicationDbContext.cs OnModelCreating (adapted from existing Proton pattern lines 240-277)
builder.Entity<ProtonTrack>(entity =>
{
    entity.ToTable("ProtonTracks");
    entity.HasKey(t => t.Id);

    // Unique constraint on (TrackType, TahunKe) — ensure no duplicate tracks
    entity.HasIndex(t => new { t.TrackType, t.TahunKe })
        .IsUnique();

    // DisplayName is non-null and non-empty
    entity.Property(t => t.DisplayName)
        .IsRequired()
        .HasMaxLength(100);

    // TrackType and TahunKe are non-null
    entity.Property(t => t.TrackType)
        .IsRequired()
        .HasMaxLength(50);

    entity.Property(t => t.TahunKe)
        .IsRequired()
        .HasMaxLength(50);
});

// ProtonKompetensi -> ProtonTrack
builder.Entity<ProtonKompetensi>(entity =>
{
    // Add ProtonTrackId FK
    entity.HasOne<ProtonTrack>()
        .WithMany(t => t.KompetensiList)
        .HasForeignKey(k => k.ProtonTrackId)
        .OnDelete(DeleteBehavior.Cascade);  // Delete track → cascade delete kompetensi

    entity.HasIndex(k => k.ProtonTrackId);
});

// ProtonTrackAssignment -> ProtonTrack
builder.Entity<ProtonTrackAssignment>(entity =>
{
    // Add ProtonTrackId FK
    entity.HasOne<ProtonTrack>()
        .WithMany()
        .HasForeignKey(a => a.ProtonTrackId)
        .OnDelete(DeleteBehavior.Restrict);  // Don't cascade on track delete (keep assignment history)

    entity.HasIndex(a => a.ProtonTrackId);
});
```

### Example 3: Defensive Backfill SQL in Migration
```csharp
// Source: EF Core SQL backfill pattern (Phase 5+ reference pattern)
// After adding ProtonTrackId columns as nullable:

// Step 1: Ensure all 6 expected tracks exist (defensive UPSERT)
migrationBuilder.Sql(@"
    WITH ExpectedTracks AS (
        SELECT 'Panelman' AS TrackType, 'Tahun 1' AS TahunKe, 'Panelman - Tahun 1' AS DisplayName, 1 AS Urutan
        UNION ALL
        SELECT 'Panelman', 'Tahun 2', 'Panelman - Tahun 2', 2
        UNION ALL
        SELECT 'Panelman', 'Tahun 3', 'Panelman - Tahun 3', 3
        UNION ALL
        SELECT 'Operator', 'Tahun 1', 'Operator - Tahun 1', 4
        UNION ALL
        SELECT 'Operator', 'Tahun 2', 'Operator - Tahun 2', 5
        UNION ALL
        SELECT 'Operator', 'Tahun 3', 'Operator - Tahun 3', 6
    )
    MERGE INTO ProtonTracks pt
    USING ExpectedTracks et
        ON pt.TrackType = et.TrackType AND pt.TahunKe = et.TahunKe
    WHEN NOT MATCHED THEN
        INSERT (TrackType, TahunKe, DisplayName, Urutan)
        VALUES (et.TrackType, et.TahunKe, et.DisplayName, et.Urutan);
");

// Step 2: Backfill ProtonKompetensi
migrationBuilder.Sql(@"
    UPDATE pk
    SET pk.ProtonTrackId = pt.Id
    FROM ProtonKompetensiList pk
    INNER JOIN ProtonTracks pt
        ON pk.TrackType = pt.TrackType AND pk.TahunKe = pt.TahunKe
    WHERE pk.ProtonTrackId IS NULL
");

// Step 3: Backfill ProtonTrackAssignment
migrationBuilder.Sql(@"
    UPDATE pta
    SET pta.ProtonTrackId = pt.Id
    FROM ProtonTrackAssignments pta
    INNER JOIN ProtonTracks pt
        ON pta.TrackType = pt.TrackType AND pta.TahunKe = pt.TahunKe
    WHERE pta.ProtonTrackId IS NULL
");

// Step 4: Make FK non-null (verify no NULLs first)
migrationBuilder.Sql(@"
    IF (SELECT COUNT(*) FROM ProtonKompetensiList WHERE ProtonTrackId IS NULL) > 0
    BEGIN
        RAISERROR('ProtonKompetensiList has NULL ProtonTrackId after backfill', 16, 1);
    END
    IF (SELECT COUNT(*) FROM ProtonTrackAssignments WHERE ProtonTrackId IS NULL) > 0
    BEGIN
        RAISERROR('ProtonTrackAssignments has NULL ProtonTrackId after backfill', 16, 1);
    END
");

migrationBuilder.AlterColumn<int>(
    name: "ProtonTrackId",
    table: "ProtonKompetensiList",
    type: "int",
    nullable: false,
    oldClrType: typeof(int),
    oldType: "int",
    oldNullable: true);

migrationBuilder.AlterColumn<int>(
    name: "ProtonTrackId",
    table: "ProtonTrackAssignments",
    type: "int",
    nullable: false,
    oldClrType: typeof(int),
    oldType: "int",
    oldNullable: true);

// Step 5: Drop old string columns
migrationBuilder.DropColumn(name: "TrackType", table: "ProtonKompetensiList");
migrationBuilder.DropColumn(name: "TahunKe", table: "ProtonKompetensiList");
migrationBuilder.DropColumn(name: "TrackType", table: "ProtonTrackAssignments");
migrationBuilder.DropColumn(name: "TahunKe", table: "ProtonTrackAssignments");
```

### Example 4: Updated CDPController Query Pattern
```csharp
// Source: CDPController.PlanIdp method (existing code, Phase 33 refactor)
// OLD: Filter Kompetensi by TrackType + TahunKe strings
// NEW: Use ProtonTrackId FK

if (isCoacheeView && user != null)
{
    // Load assignment WITH ProtonTrack navigation
    var assignment = await _context.ProtonTrackAssignments
        .Include(a => a.ProtonTrack)  // NEW: load FK'd track
        .Where(a => a.CoacheeId == user.Id && a.IsActive)
        .FirstOrDefaultAsync();

    if (assignment == null)
    {
        ViewBag.UserRole = userRole;
        ViewBag.NoAssignment = true;
        return View();
    }

    // Filter Kompetensi by ProtonTrackId instead of strings
    var kompetensiList = await _context.ProtonKompetensiList
        .Include(k => k.SubKompetensiList)
            .ThenInclude(s => s.Deliverables)
        .Where(k => k.ProtonTrackId == assignment.ProtonTrackId)  // FK instead of TrackType+TahunKe
        .OrderBy(k => k.Urutan)
        .ToListAsync();

    var activeProgress = await _context.ProtonDeliverableProgresses
        .Where(p => p.CoacheeId == user.Id && p.Status == "Active")
        .FirstOrDefaultAsync();

    var finalAssessment = await _context.ProtonFinalAssessments
        .Where(fa => fa.CoacheeId == user.Id)
        .OrderByDescending(fa => fa.CreatedAt)
        .FirstOrDefaultAsync();

    // ViewModel still exposes TrackType/TahunKe for display (read from FK'd track)
    var protonViewModel = new ProtonPlanViewModel
    {
        TrackType = assignment.ProtonTrack.TrackType,  // From FK
        TahunKe = assignment.ProtonTrack.TahunKe,      // From FK
        DisplayName = assignment.ProtonTrack.DisplayName,  // NEW: from FK
        KompetensiList = kompetensiList,
        ActiveProgress = activeProgress,
        FinalAssessment = finalAssessment
    };

    ViewBag.UserRole = userRole;
    ViewBag.IsProtonView = true;
    return View(protonViewModel);
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| TrackType + TahunKe strings in ProtonKompetensi & ProtonTrackAssignment | ProtonTrackId FK to normalized ProtonTrack table | Phase 33 | Eliminates string duplication, enables efficient queries by track, single source of truth for track definitions |
| SeedProtonData seeding full Kompetensi/SubKompetensi/Deliverable catalog | SeedProtonData seeding ProtonTrack only; catalog managed by Phase 35 UI | Phase 33 | Separates seed-time baseline (6 tracks) from user-managed data (catalog); Phase 35 admin UI owns catalog lifecycle |
| No ProtonTrack entity | Dedicated ProtonTrack entity with Id, TrackType, TahunKe, DisplayName, navigation to Kompetensi | Phase 33 | Enables Phase 34 dropdown queries and Phase 35 catalog management |
| AssignTrack receives TrackType + TahunKe strings | AssignTrack receives ProtonTrackId from dropdown | Phase 33 | Simpler controller logic, no string lookups, FK validation built-in |

**Deprecated/outdated:**
- **ProtonKompetensi.TrackType + TahunKe columns:** Replaced by FK. Dropped at end of Phase 33 migration. Do not use after Phase 33.
- **ProtonTrackAssignment.TrackType + TahunKe columns:** Replaced by FK. Dropped at end of Phase 33 migration. Do not reference in code after Phase 33.
- **SeedProtonData seeding Kompetensi/SubKompetensi:** Ownership transitions to Phase 35 UI. Fresh dev installs after Phase 33 have no catalog items until added via UI.

---

## Open Questions

1. **Migration Atomicity: Single vs. Split?**
   - What we know: CONTEXT.md says "one migration or split" is Claude's discretion. Single migration (add table, backfill, drop cols) is atomically safer.
   - What's unclear: Are there performance constraints (e.g., large production Kompetensi table) that favor split migrations?
   - Recommendation: Use single migration. EF Core migrations are designed for atomic execution. If rollback needed, rollback entire migration.

2. **DisplayName Mutability in Phase 34/35?**
   - What we know: CONTEXT.md says DisplayName is "not editable by HC/Admin" at seed time; Phase 34 discretion: "user may want this later."
   - What's unclear: Does Phase 34 need to allow HC to override DisplayName when managing catalog?
   - Recommendation: Phase 33 stores DisplayName as read-only seed value. Phase 34 can show it in dropdown. Phase 35 (future enhancement) can allow edit if needed. Don't build edit logic in Phase 33.

3. **Kompetensi Orphans After Backfill?**
   - What we know: Migration uses defensive INNER JOIN to backfill; orphans (Kompetensi with unknown TrackType+TahunKe) stay NULL.
   - What's unclear: Should migration abort with error if orphans found, or silently leave them NULL and require manual investigation?
   - Recommendation: Add validation query after backfill that raises error if any NULLs remain. Fail loudly so issues are caught in test environment, not production.

---

## Sources

### Primary (HIGH confidence)
- **ProtonModels.cs** (codebase) - Existing ProtonKompetensi, ProtonTrackAssignment entity structure; confirmed string columns TrackType, TahunKe
- **ApplicationDbContext.cs** (codebase) - Existing FK configuration patterns, DeleteBehavior.Cascade/Restrict examples, OnModelCreating structure
- **20260217063156_AddProtonDeliverableTracking.cs** (codebase) - EF Core migration pattern used in project; table creation, FK definition, index creation format
- **SeedProtonData.cs** (codebase) - Current seeding pattern for Proton entities; seed method structure
- **CDPController.cs** (codebase) - Current usage of ProtonTrackAssignment in PlanIdp; shows TrackType+TahunKe string filtering

### Secondary (MEDIUM confidence)
- **Entity Framework Core 8.0 Documentation** - FK configuration, DeleteBehavior enum, migration patterns are standard EF Core; patterns confirmed against codebase usage

### Tertiary (LOW confidence)
- None identified; all research grounded in active codebase patterns

---

## Metadata

**Confidence breakdown:**
- **Standard Stack:** HIGH - EF Core 8.0 and migration patterns are proven in codebase (Phase 5 migration exists, no version changes needed)
- **Architecture:** HIGH - FK relationships follow existing ProtonKompetensi→ProtonSubKompetensi pattern; deletion cascade order mirrors existing hierarchy
- **Pitfalls:** HIGH - Identified from direct codebase examination and common EF Core migration gotchas; defensive backfill pattern is standard practice
- **Code Examples:** HIGH - All examples adapted from verified codebase patterns (Phase 5 migration, CDPController, SeedProtonData, ApplicationDbContext)

**Research date:** 2026-02-23
**Valid until:** 2026-03-23 (30 days — entity frameworks and migration patterns are stable; update if migration encounters runtime issues)
