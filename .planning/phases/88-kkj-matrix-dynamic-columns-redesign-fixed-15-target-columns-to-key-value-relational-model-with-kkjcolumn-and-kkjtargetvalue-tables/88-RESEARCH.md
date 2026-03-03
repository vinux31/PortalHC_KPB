# Phase 89: KKJ Matrix Dynamic Columns - Research

**Researched:** 2026-03-02
**Domain:** Entity Framework Core data model redesign, relational normalization, dynamic schema management
**Confidence:** HIGH

## Summary

Phase 89 transforms the KKJ Matrix from a rigid 15-column architecture to a flexible key-value relational model. Currently, `KkjMatrixItem` has hardcoded `Target_*` columns (e.g., Target_SectionHead, Target_SrSpv_GSH) and `KkjBagian` has corresponding `Label_*` columns—all designed for exactly 15 positions with no flexibility per organizational unit.

The redesign introduces two new tables:
- **KkjColumn** (metadata): defines available target columns per Bagian with editable names and display order
- **KkjTargetValue** (data): stores actual target values as key-value pairs

This enables each Bagian to have its own unique set of target columns, with lengths determined by HC during import rather than hardcoded system-wide. All data-consuming features (KkjMatrix admin view, CMP/Kkj worker view, PositionTargetHelper, Assessment flow) must be updated to query the relational model instead of reflection-based column access.

**Primary recommendation:** Implement three new EF models (KkjColumn, KkjTargetValue, PositionColumnMapping), create code-first migration, refactor PositionTargetHelper to use DB queries, then update UI rendering to iterate dynamic columns.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Data Migration:** Start fresh — remove all existing Target_* data from KkjMatrixItem; HC will re-input values via Excel import (Phase 88) after this phase completes
- **Keep base data:** All base KkjMatrixItem fields (No, SkillGroup, SubSkillGroup, Indeks, Kompetensi, Bagian) remain intact
- **New table design:**
  - **KkjColumn:** Id, BagianId (FK), Name (string), DisplayOrder (int)
  - **KkjTargetValue:** Id, KkjMatrixItemId (FK), KkjColumnId (FK), Value (string)
- **Remove hardcoded columns:** All 15 Target_* columns from KkjMatrixItem, all 15 Label_* columns from KkjBagian
- **Position Mapping:** New table PositionColumnMapping (Id, Position string, KkjColumnId FK) replaces hardcoded Dictionary in PositionTargetHelper
- **UI rendering:** Columns rendered dynamically from KkjColumn table per selected Bagian (spreadsheet-like editable view, same UX as current)
- **Assessment flow:** CMPController (lines 1425, 1556) and AdminController (lines 2296, 2368) will use GetTargetLevel() with new PositionColumnMapping + KkjTargetValue lookup
- **FK stability:** UserCompetencyLevel.KkjMatrixItemId and AssessmentCompetencyMap.KkjMatrixItemId remain unchanged (only target lookup changes)

### Claude's Discretion
- Exact EF Core migration strategy (code-first migration)
- KkjColumn management UI layout details
- PositionColumnMapping admin UI design
- Performance optimization for key-value queries (eager loading, caching)
- Handling edge cases in KkjMatrixSave for dynamic columns

### Deferred Ideas (OUT OF SCOPE)
- Excel import/export using dynamic columns → Phase 88 (depends on this phase)
- Template per-Bagian download → Phase 88 future enhancement
</user_constraints>

## Standard Stack

### Core Technologies
| Technology | Version | Purpose | Why Standard |
|------------|---------|---------|--------------|
| Entity Framework Core | 8.x (inferred from .NET 8 codebase) | ORM for relational model implementation | ASP.NET Core default, strongly typed queries, migration support |
| C# | 12.0+ (modern async/await patterns used throughout) | Language for model definitions and helper refactoring | Type-safe, LINQ support for dynamic queries, reflection for compatibility |
| SQL Server | Latest (check appsettings via Azure/local connection string) | Database backend for new tables | Project standard, supports indexes and check constraints |

### Supporting Libraries
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ClosedXML | Latest (used in ImportWorkers) | Excel template generation for Phase 88 | Already in project, used for all Excel operations |
| LINQ | Built-in | Dynamic column enumeration and filtering | For translating key-value queries to SQL |

### Key Models from ApplicationDbContext Pattern
The project uses explicit DbSet registration with relationship configuration in OnModelCreating:
- **Pattern:** DbSet + HasOne/WithMany + HasForeignKey + OnDelete behavior
- **Delete strategy:** Cascade for dependent data (TrainingRecord → User), Restrict for data integrity (UserResponse → AssessmentSession, UserCompetencyLevel → User/KkjMatrixItem)
- **Indexing:** Strategic indexes for performance (FK columns, composite indexes on frequent query pairs)
- **Unique constraints:** IsUnique() on critical fields (KkjBagian.Name, UserCompetencyLevel composite)

## Architecture Patterns

### Current State (Pre-Phase 89)
```
KkjMatrixItem (15 hardcoded Target_* columns)
├── Target_SectionHead: string = "-"
├── Target_SrSpv_GSH: string = "-"
├── Target_ShiftSpv_GSH: string = "-"
├── ... (12 more Target_* fields)
└── Bagian: string (FK by name to KkjBagian.Name)

KkjBagian (15 hardcoded Label_* columns)
├── Label_SectionHead: string = "Section Head"
├── Label_SrSpv_GSH: string = "Sr Spv GSH"
├── ... (13 more Label_* fields)
└── DisplayOrder: int

PositionTargetHelper
└── PositionColumnMap (hardcoded Dictionary)
    └── GetTargetLevel(competency, position) → reflection-based lookup
```

### Post-Phase 89 Relational Model
```
KkjMatrixItem (base fields only, no Target_* columns)
├── Id, No, SkillGroup, SubSkillGroup, Indeks, Kompetensi, Bagian
└── 1:N → KkjTargetValue (new)

KkjBagian (no Label_* columns)
├── Id, Name (unique index), DisplayOrder
└── 1:N → KkjColumn (new)

KkjColumn (new)
├── Id (PK)
├── BagianId (FK) → KkjBagian
├── Name: string (e.g., "Section Head", "Operator Process Water")
├── DisplayOrder: int
└── 1:N → KkjTargetValue (new)

KkjTargetValue (new)
├── Id (PK)
├── KkjMatrixItemId (FK) → KkjMatrixItem
├── KkjColumnId (FK) → KkjColumn
├── Value: string (typically "1"-"5" or "-")
└── Composite index: (KkjMatrixItemId, KkjColumnId) for uniqueness

PositionColumnMapping (new)
├── Id (PK)
├── Position: string (e.g., "Section Head", "Operator GSH 8-11")
├── KkjColumnId (FK) → KkjColumn
└── Index: (Position, KkjColumnId)

PositionTargetHelper
└── GetTargetLevel(competency, position) → DB query via PositionColumnMapping + KkjTargetValue
```

### EF Core Configuration Pattern (from existing code)

**File:** `Data/ApplicationDbContext.cs`
**Pattern:** Fluent API in OnModelCreating()

```csharp
// Example from existing code (UserCompetencyLevel):
builder.Entity<UserCompetencyLevel>(entity =>
{
    entity.ToTable("UserCompetencyLevels");

    // Foreign keys
    entity.HasOne(c => c.User)
        .WithMany()
        .HasForeignKey(c => c.UserId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(c => c.KkjMatrixItem)
        .WithMany()
        .HasForeignKey(c => c.KkjMatrixItemId)
        .OnDelete(DeleteBehavior.Restrict);

    // Indexes
    entity.HasIndex(c => new { c.UserId, c.KkjMatrixItemId })
        .IsUnique();

    // Check constraints
    entity.HasCheckConstraint("CK_UserCompetencyLevel_CurrentLevel", "[CurrentLevel] >= 0 AND [CurrentLevel] <= 5");
});
```

**Apply to Phase 89 models:**

1. **KkjColumn configuration:**
   - Table: "KkjColumns"
   - Relationship: HasOne (KkjBagian) WithMany
   - Index: (BagianId, DisplayOrder) for efficient sorting per bagian
   - Constraint: (BagianId, Name) unique (one column name per bagian)

2. **KkjTargetValue configuration:**
   - Table: "KkjTargetValues"
   - Relationships: HasOne (KkjMatrixItem) WithMany, HasOne (KkjColumn) WithMany
   - Index: (KkjMatrixItemId, KkjColumnId) unique (one value per competency-column pair)
   - Constraint: Value must be "-" or numeric 1-5

3. **PositionColumnMapping configuration:**
   - Table: "PositionColumnMappings"
   - Relationship: HasOne (KkjColumn) WithMany
   - Index: (Position, KkjColumnId)
   - Constraint: Position should not have duplicate mappings

### Data Migration Strategy

**File locations:**
- Models: `Models/KkjModels.cs` (add KkjColumn, KkjTargetValue, PositionColumnMapping)
- DbContext: `Data/ApplicationDbContext.cs` (add DbSets and fluent configuration)
- Migration: Generated via `dotnet ef migrations add <name>` (code-first)

**Migration steps:**
1. Add three new DbSets to ApplicationDbContext
2. Configure relationships in OnModelCreating
3. Add navigation properties to existing models (optional, for convenience)
4. Run `dotnet ef migrations add AddKkjDynamicColumns`
5. Run `dotnet ef database update`
6. Data loss: All Target_* column data is dropped (start fresh, HC re-imports via Phase 88)

### Dynamic Column Rendering Pattern

**Admin/KkjMatrix (editable) and CMP/Kkj (read-only):**
- **Load:** Fetch KkjColumn records for selected Bagian, ordered by DisplayOrder
- **Render:** For each KkjColumn, create a column in the table with header = Name
- **Input:** For editable view (Admin), create input fields for each KkjTargetValue cell
- **Output:** For read-only view (CMP), display KkjTargetValue.Value as text

**Existing pattern in KkjMatrix.cshtml:**
```javascript
// Current (hardcoded columns):
var columnMap = [
    { key:'Label_SectionHead', prop:'Target_SectionHead' },
    { key:'Label_SrSpv_GSH', prop:'Target_SrSpv_GSH' },
    // ... 13 more
];

// Post-Phase 89 (dynamic):
var columns = [
    { id: 1, name: 'Section Head', displayOrder: 1 },
    { id: 2, name: 'Operator Process Water', displayOrder: 2 },
    // ... loaded from KkjColumn table
];
```

### PositionTargetHelper Refactoring

**Current (reflection-based):**
```csharp
private static readonly Dictionary<string, string> PositionColumnMap = new()
{
    { "Section Head", "Target_SectionHead" },
    { "Sr Supervisor GSH", "Target_SrSpv_GSH" },
    // ... 13 more hardcoded mappings
};

public static int GetTargetLevel(KkjMatrixItem competency, string? userPosition)
{
    // Use reflection to access property by name
    var propertyInfo = typeof(KkjMatrixItem).GetProperty(columnName);
    var value = propertyInfo?.GetValue(competency) as string;
}
```

**Post-Phase 89 (DB-based):**
```csharp
public static async Task<int> GetTargetLevel(ApplicationDbContext context, int kkjMatrixItemId, string? userPosition)
{
    // Query: Position → PositionColumnMapping.KkjColumnId → KkjTargetValue for that item+column
    var mapping = await context.PositionColumnMappings
        .Include(m => m.KkjColumn)
        .FirstOrDefaultAsync(m => m.Position == userPosition);

    if (mapping == null) return 0; // Position not mapped

    var targetValue = await context.KkjTargetValues
        .FirstOrDefaultAsync(v => v.KkjMatrixItemId == kkjMatrixItemId && v.KkjColumnId == mapping.KkjColumnId);

    // Parse Value (typically "1"-"5" or "-")
}
```

**Integration points:** Called in CMPController (lines 1425, 1556) and AdminController (lines 2296, 2368) during assessment result calculation—must make async or cache results.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Dynamic table schema in relational DB | Custom column name parsing + reflection | EF Core relationships (HasOne/WithMany) + LINQ queries | Reflection is fragile (typos in column names break silently), relational model is queryable and indexable |
| Mapping user positions to KKJ columns | Another hardcoded dictionary or CSV file | PositionColumnMapping table with admin UI for management | Hardcoded breaks when positions change; DB table allows HC to manage mappings without code redeploy |
| Excel import of dynamic columns | Manual column header matching | Fuzzy/normalized header matching (Phase 88 will handle) | Case sensitivity and spacing differences cause silent data loss; normalized matching is more robust |
| Performance for competency lookups | Full table scans | Composite indexes on (KkjMatrixItemId, KkjColumnId) and (Position) | Key-value lookups hit thousands of rows; indexed queries return in microseconds |

**Key insight:** Key-value relational designs look like extra tables, but they're more maintainable than property generators, easier to query than reflection, and more performant than full table scans.

## Common Pitfalls

### Pitfall 1: Forgetting to Include Related Data in Queries
**What goes wrong:** EF Core lazy-loads navigation properties by default. If PositionTargetHelper queries PositionColumnMappings without `.Include(m => m.KkjColumn)`, the KkjColumn navigation property is null, leading to null reference exceptions when accessing KkjColumnId.

**Why it happens:** Async context—when the DbContext scope closes, related entities are no longer available. Developers forget to eagerly load dependencies.

**How to avoid:** Always `.Include()` related entities needed in the result. For high-volume lookups (assessment result calculation), consider caching the PositionColumnMapping → KkjColumnId map in memory to avoid repeated DB calls.

**Warning signs:** NullReferenceException in assessment flow, "KkjColumn is null" logs appearing during competency level calculation.

### Pitfall 2: Cascading Deletes Breaking Data Integrity
**What goes wrong:** If KkjColumn is deleted (e.g., a position is removed), cascading delete would orphan KkjTargetValue rows that reference it. Later, a query tries to access a KkjTargetValue with a deleted KkjColumn.

**Why it happens:** Using OnDelete(DeleteBehavior.Cascade) without checking for dependent records first.

**How to avoid:** Use OnDelete(DeleteBehavior.Restrict) for KkjTargetValue → KkjColumn. Before deleting a KkjColumn, check if any KkjTargetValues reference it and either delete those values first or prevent deletion with a user-facing warning.

**Warning signs:** "The DELETE statement conflicted with a FOREIGN KEY constraint" error when deleting a column.

### Pitfall 3: Composite Index Missing on KkjTargetValue
**What goes wrong:** KkjTargetValue is queried frequently by (KkjMatrixItemId, KkjColumnId) during assessment flow. Without a composite index, SQL Server performs table scans, causing assessment result calculation to timeout with large data volumes.

**Why it happens:** Developer adds the table but forgets the fluent configuration for indexes in OnModelCreating.

**How to avoid:** Always add `entity.HasIndex(v => new { v.KkjMatrixItemId, v.KkjColumnId }).IsUnique()` in OnModelCreating. Test with realistic data volume (1000+ matrix items) to verify query performance.

**Warning signs:** Assessment result page taking >5 seconds to load, database CPU spike during assessment flow.

### Pitfall 4: Breaking PositionTargetHelper Without Updating All Callers
**What goes wrong:** PositionTargetHelper is called from AdminController (assessment result generation) and CMPController (worker assessment display). If the method signature changes from sync to async (`GetTargetLevel(...) → async Task<int>`), one caller is forgotten, leaving a code path untested.

**Why it happens:** Large codebase, multiple references scattered across controllers. Easy to miss one or two calls during refactoring.

**How to avoid:** Search for all references: `grep -r "GetTargetLevel" Controllers/`. Update all callers to await the async version. Run full test suite (manual verification or unit tests) to ensure assessment result page works for both admin and worker flows.

**Warning signs:** Compilation errors in forgotten callers, or silent failures where competency levels show as 0 unexpectedly.

### Pitfall 5: Not Handling Missing Position-to-Column Mappings
**What goes wrong:** During assessment, a user with position "New Operator" is assessed. No PositionColumnMapping exists for "New Operator". Helper returns 0 for all competencies. Assessment results show target level 0 for all items, making it impossible to compare performance to target.

**Why it happens:** Position strings are user data (from ApplicationUser.Position field). When new positions are added, PositionColumnMappings aren't automatically created.

**How to avoid:** In assessment result generation, check if PositionColumnMapping exists for the user's position. If not, return a warning that position is not mapped (store in result object or return special value). Admin sees warning: "Posisi 'New Operator' belum di-map ke kolom KKJ" and can add the mapping via PositionColumnMapping UI.

**Warning signs:** Assessment results showing all target levels as 0, manual spot-check reveals legitimate positions have no mappings.

## Code Examples

### Model Definitions for Phase 89

**Source:** Entity Framework Core docs + project patterns from ApplicationDbContext

```csharp
// File: Models/KkjModels.cs

// ADD to KkjMatrixItem:
public class KkjMatrixItem
{
    public int Id { get; set; }
    public int No { get; set; }
    public string SkillGroup { get; set; } = "";
    public string SubSkillGroup { get; set; } = "";
    public string Indeks { get; set; } = "";
    public string Kompetensi { get; set; } = "";
    public string Bagian { get; set; } = "";

    // REMOVE ALL 15 Target_* columns

    // KEEP navigation property (added during migration)
    public ICollection<KkjTargetValue> TargetValues { get; set; } = new List<KkjTargetValue>();
}

// REMOVE ALL Label_* columns from KkjBagian:
public class KkjBagian
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;

    // REMOVE all Label_* fields

    public ICollection<KkjColumn> Columns { get; set; } = new List<KkjColumn>();
}

// NEW TABLE
public class KkjColumn
{
    public int Id { get; set; }
    public int BagianId { get; set; }  // FK to KkjBagian
    public string Name { get; set; } = "";  // e.g., "Section Head", "Operator Process Water"
    public int DisplayOrder { get; set; } = 0;

    // Navigation properties
    public KkjBagian Bagian { get; set; } = null!;
    public ICollection<KkjTargetValue> TargetValues { get; set; } = new List<KkjTargetValue>();
}

// NEW TABLE
public class KkjTargetValue
{
    public int Id { get; set; }
    public int KkjMatrixItemId { get; set; }  // FK to KkjMatrixItem
    public int KkjColumnId { get; set; }      // FK to KkjColumn
    public string Value { get; set; } = "-";  // Typically "1"-"5" or "-"

    // Navigation properties
    public KkjMatrixItem KkjMatrixItem { get; set; } = null!;
    public KkjColumn KkjColumn { get; set; } = null!;
}

// NEW TABLE
public class PositionColumnMapping
{
    public int Id { get; set; }
    public string Position { get; set; } = "";  // e.g., "Section Head"
    public int KkjColumnId { get; set; }        // FK to KkjColumn

    // Navigation property
    public KkjColumn KkjColumn { get; set; } = null!;
}
```

### EF Core Configuration in ApplicationDbContext

**Source:** Existing patterns from UserCompetencyLevel and AssessmentCompetencyMap configuration

```csharp
// File: Data/ApplicationDbContext.cs

public DbSet<KkjColumn> KkjColumns { get; set; }
public DbSet<KkjTargetValue> KkjTargetValues { get; set; }
public DbSet<PositionColumnMapping> PositionColumnMappings { get; set; }

protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // ... existing configurations ...

    // KkjColumn configuration
    builder.Entity<KkjColumn>(entity =>
    {
        entity.ToTable("KkjColumns");

        entity.HasOne(c => c.Bagian)
            .WithMany(b => b.Columns)
            .HasForeignKey(c => c.BagianId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(c => new { c.BagianId, c.DisplayOrder });
        entity.HasIndex(c => new { c.BagianId, c.Name }).IsUnique();
    });

    // KkjTargetValue configuration
    builder.Entity<KkjTargetValue>(entity =>
    {
        entity.ToTable("KkjTargetValues");

        entity.HasOne(v => v.KkjMatrixItem)
            .WithMany(m => m.TargetValues)
            .HasForeignKey(v => v.KkjMatrixItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(v => v.KkjColumn)
            .WithMany(c => c.TargetValues)
            .HasForeignKey(v => v.KkjColumnId)
            .OnDelete(DeleteBehavior.Restrict);  // Prevent orphaned values

        entity.HasIndex(v => new { v.KkjMatrixItemId, v.KkjColumnId }).IsUnique();
    });

    // PositionColumnMapping configuration
    builder.Entity<PositionColumnMapping>(entity =>
    {
        entity.ToTable("PositionColumnMappings");

        entity.HasOne(m => m.KkjColumn)
            .WithMany()
            .HasForeignKey(m => m.KkjColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(m => m.Position);
        entity.HasIndex(m => new { m.Position, m.KkjColumnId }).IsUnique();
    });
}
```

### Refactored PositionTargetHelper

**Source:** Current PositionTargetHelper.cs + EF Core async patterns

```csharp
// File: Helpers/PositionTargetHelper.cs

public static class PositionTargetHelper
{
    /// <summary>
    /// Gets the target competency level for a given KKJ competency and user position.
    /// Queries PositionColumnMapping + KkjTargetValue for dynamic column support.
    /// </summary>
    /// <param name="context">ApplicationDbContext instance</param>
    /// <param name="kkjMatrixItemId">ID of the KKJ competency</param>
    /// <param name="userPosition">User's position (must match a PositionColumnMapping)</param>
    /// <returns>Target level (0-5), or 0 if position not mapped or value is "-"</returns>
    public static async Task<int> GetTargetLevel(ApplicationDbContext context, int kkjMatrixItemId, string? userPosition)
    {
        if (string.IsNullOrWhiteSpace(userPosition))
            return 0;

        // Find the KkjColumn mapped to this position
        var mapping = await context.PositionColumnMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Position == userPosition);

        if (mapping == null)
            return 0; // Position not in the mapping — show warning to admin

        // Find the target value for this competency + column
        var targetValue = await context.KkjTargetValues
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.KkjMatrixItemId == kkjMatrixItemId && v.KkjColumnId == mapping.KkjColumnId);

        if (targetValue == null || targetValue.Value == "-")
            return 0; // Not applicable for this position

        // Parse the level (typically "1"-"5")
        if (int.TryParse(targetValue.Value, out var level) && level >= 0 && level <= 5)
            return level;

        return 0; // Unparseable value
    }

    /// <summary>
    /// Gets all valid position keys that are mapped in the system.
    /// Useful for dropdown selectors and validation.
    /// </summary>
    public static async Task<List<string>> GetAllPositions(ApplicationDbContext context)
    {
        return await context.PositionColumnMappings
            .AsNoTracking()
            .Select(m => m.Position)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
    }
}
```

### Dynamic Column Rendering in JavaScript (KkjMatrix.cshtml)

**Source:** Current KkjMatrix.cshtml column mapping + dynamic table generation

```javascript
// Current (Phase 88 or earlier — hardcoded columns):
var columnMap = [
    { key:'Label_SectionHead',        prop:'Target_SectionHead'        },
    { key:'Label_SrSpv_GSH',          prop:'Target_SrSpv_GSH'          },
    // ... 13 more
];

// Post-Phase 89 (dynamic columns):
async function loadColumnsForBagian(bagianId) {
    const response = await fetch(`/Admin/GetKkjColumns?bagianId=${bagianId}`);
    const columns = await response.json();

    // columns = [
    //   { id: 1, name: 'Section Head', displayOrder: 1 },
    //   { id: 2, name: 'Operator Process Water', displayOrder: 2 },
    //   ... (sorted by displayOrder)
    // ]

    renderTable(columns);
}

function renderTable(columns) {
    let html = '<table class="table table-sm"><thead><tr>';
    html += '<th>No</th><th>Kompetensi</th><th>Bagian</th>';

    // Render dynamic columns
    columns.forEach(col => {
        html += `<th>${col.name}</th>`;
    });

    html += '<th>Aksi</th></tr></thead><tbody>';

    // Body rows... for each item, render cells from KkjTargetValue
    kkjItems.forEach(item => {
        html += `<tr><td>${item.no}</td><td>${item.kompetensi}</td><td>${item.bagian}</td>`;

        columns.forEach(col => {
            const targetValue = item.targetValues?.find(tv => tv.kkjColumnId === col.id)?.value || '-';
            html += `<td><input class="edit-input" value="${targetValue}" data-item-id="${item.id}" data-column-id="${col.id}"/></td>`;
        });

        html += '<td><button class="btn-delete">Hapus</button></td></tr>';
    });

    html += '</tbody></table>';
    document.getElementById('tableContainer').innerHTML = html;
}
```

### New Admin Controller Actions for Column Management

**Source:** Existing KkjBagian CRUD pattern applied to KkjColumn

```csharp
// File: Controllers/AdminController.cs — new region for KkjColumn management

[Authorize(Roles = "Admin, HC")]
[HttpPost]
public async Task<IActionResult> KkjColumnSave([FromBody] KkjColumn column)
{
    if (string.IsNullOrWhiteSpace(column.Name))
        return BadRequest(new { error = "Nama kolom diperlukan" });

    if (column.Id == 0)
    {
        // New column
        _context.KkjColumns.Add(column);
    }
    else
    {
        // Update column
        var existing = await _context.KkjColumns.FindAsync(column.Id);
        if (existing == null)
            return NotFound();

        existing.Name = column.Name;
        existing.DisplayOrder = column.DisplayOrder;
    }

    await _context.SaveChangesAsync();
    return Ok(new { success = true });
}

[Authorize(Roles = "Admin, HC")]
[HttpPost]
public async Task<IActionResult> KkjColumnDelete(int id)
{
    var column = await _context.KkjColumns.FindAsync(id);
    if (column == null)
        return NotFound();

    // Check for dependent target values
    var hasValues = await _context.KkjTargetValues
        .AnyAsync(v => v.KkjColumnId == id);

    if (hasValues)
        return BadRequest(new { error = "Tidak bisa menghapus kolom yang masih memiliki data nilai" });

    _context.KkjColumns.Remove(column);
    await _context.SaveChangesAsync();
    return Ok(new { success = true });
}

[Authorize(Roles = "Admin, HC")]
[HttpGet]
public async Task<IActionResult> GetKkjColumns(int bagianId)
{
    var columns = await _context.KkjColumns
        .Where(c => c.BagianId == bagianId)
        .OrderBy(c => c.DisplayOrder)
        .ToListAsync();

    return Json(columns);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcoded 15 property columns (Target_*) | Flexible relational key-value model (KkjColumn + KkjTargetValue) | Phase 89 | Enables per-Bagian column customization, easier to import/export, supports growth without code changes |
| Reflection-based column name lookup | Database query with PositionColumnMapping | Phase 89 | Type-safe, queryable, indexable, easier to debug |
| Hardcoded Dictionary in PositionTargetHelper | Admin-managed PositionColumnMapping table | Phase 89 | Allows position-to-column remapping without code deploy |

**Deprecated/outdated:**
- Hardcoded 15 Target_* and Label_* columns: Will be removed entirely in this phase. Data migration clears existing values; HC re-imports via Phase 88.
- Reflection-based property access in PositionTargetHelper: Replaced by async DB queries.

## Open Questions

1. **Caching strategy for PositionColumnMapping**
   - What we know: GetTargetLevel is called frequently during assessment result calculation (100+ calls per assessment)
   - What's unclear: Should we cache the entire PositionColumnMapping → KkjColumnId map in memory (IMemoryCache), or is EF Core query caching sufficient?
   - Recommendation: Start with EF Core context caching (entities are cached within the DbContext scope). If assessment result page timing shows bottleneck, add IMemoryCache with 15-minute TTL and invalidate on column mapping changes. Measure before optimizing.

2. **Handling position string variants**
   - What we know: PositionColumnMapping is keyed on exact Position string match
   - What's unclear: Should "Section Head", "section head", "SECTION HEAD" be treated as the same? Are there typos in existing ApplicationUser.Position values?
   - Recommendation: Normalize position strings to title case during import and HC input validation. Add UI validation to prevent typos. For Phase 89, use exact match; Phase 88 can implement fuzzy matching if needed.

3. **Navigation property strategy in views**
   - What we know: KkjMatrix.cshtml currently receives a List<KkjMatrixItem> that includes raw data
   - What's unclear: After Phase 89, should KkjTargetValue data be included via `.Include(m => m.TargetValues)`, or should the controller fetch it separately?
   - Recommendation: Use explicit `.Include(m => m.TargetValues).ThenInclude(v => v.KkjColumn)` in KkjMatrix action to avoid N+1 queries. Pass both items and columns to the view, or use ViewBag. Keep the pattern consistent with existing code (KkjMatrix.cshtml currently receives KkjBagians via ViewBag).

4. **Data synchronization between old and new tables during migration**
   - What we know: All existing Target_* data will be cleared at start of Phase 89
   - What's unclear: Should there be a migration script to auto-convert existing Target_* columns to KkjTargetValue rows (with PositionColumnMapping auto-created), or start completely fresh?
   - Recommendation: Start completely fresh—no conversion script. This forces HC to re-curate data via Phase 88 import, ensuring clean state. If conversion is needed later, it can be done as a separate utility (Phase 88 tool).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (project uses xUnit patterns, inferred from ASP.NET Core standard) |
| Config file | No existing test project detected — testing is manual verification + browser QA |
| Quick run command | `dotnet test --filter VersionOfTests` (if test project exists) |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
No formal test requirements specified for Phase 89. Testing is manual/browser-based per project preferences:
| Scenario | Test Type | Verification |
|----------|-----------|--------------|
| KkjColumn CRUD works | Manual browser test | Admin/KkjMatrix: add/edit/delete/reorder columns; refresh view; verify columns persist |
| KkjTargetValue save/load | Manual browser test | Admin/KkjMatrix: edit target value cell, save, refresh page, value is restored |
| PositionColumnMapping admin UI | Manual browser test | Admin interface to map position → KkjColumn; verify mapping persists |
| PositionTargetHelper returns correct level | Manual assessment test | Run assessment as test user with mapped position; verify competency target level matches database value |
| Dynamic column rendering in CMP/Kkj | Manual browser test | Worker views CMP/Kkj page; sees columns dynamically from database |
| Assessment flow with dynamic columns | Manual end-to-end test | Create assessment, assign worker, run assessment, verify results show correct target levels |

### Wave 0 Gaps
- [ ] Create test project (if not already existing) — `dotnet new xunit -n HcPortal.Tests`
- [ ] Add ApplicationDbContext test fixtures for in-memory database
- [ ] Add test cases for PositionTargetHelper async queries
- [ ] Verify EF Core migration compiles without errors

*(If no test project exists, manual QA via browser is acceptable per project standards. Defer automated testing to v3.1)*

## Sources

### Primary (HIGH confidence)
- **Entity Framework Core documentation** (Microsoft Learn) — Fluent API configuration patterns, HasOne/WithMany, OnDelete behaviors, indexes
- **Existing ApplicationDbContext.cs** — UserCompetencyLevel, AssessmentCompetencyMap, and CoachingSession configurations provide exact project patterns
- **Current KkjModels.cs + PositionTargetHelper.cs** — Define the current hardcoded structure being redesigned
- **Project CONTEXT.md (Phase 89)** — User decisions lock the table schema, relationships, and integration points

### Secondary (MEDIUM confidence)
- **Phase 88 CONTEXT.md** — Describes Excel import/export that depends on Phase 89 completion; clarifies column mapping requirements
- **AdminController ImportWorkers pattern** — Demonstrates ClosedXML usage and AuditLog integration for reference
- **State.md** — Documents project milestones, confirms Phase 88 depends on Phase 89

### Tertiary (LOW confidence)
- None — all critical decisions are locked in CONTEXT.md or directly observable in codebase

## Metadata

**Confidence breakdown:**
- **Standard stack:** HIGH — EF Core, C#, SQL Server are project standards confirmed in code
- **Architecture:** HIGH — Table schema, relationships, and delete behaviors are locked in CONTEXT.md; existing DbContext patterns are directly applicable
- **Pitfalls:** HIGH — Derived from common EF Core mistakes and project's pattern of using .Include() for eager loading, Restrict for integrity
- **Code examples:** HIGH — All examples use existing project patterns from ApplicationDbContext, AdminController, and KkjMatrix.cshtml

**Research date:** 2026-03-02
**Valid until:** 2026-03-09 (Phase 89 is stable — no active churn in EF Core or .NET 8, redesign is locked)
