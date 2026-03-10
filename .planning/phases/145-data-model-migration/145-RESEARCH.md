# Phase 145: Data Model & Migration - Research

**Researched:** 2026-03-10
**Domain:** EF Core migration, SQL Server, ASP.NET Core data model
**Confidence:** HIGH

## Summary

This phase adds a single nullable string property `SubCompetency` to the existing `PackageQuestion` model and creates an EF Core migration to add the column to SQL Server. This is a straightforward, low-risk schema change.

The existing codebase uses EF Core with SQL Server (`UseSqlServer`), namespace `HcPortal.Migrations`, and follows a consistent pattern for adding nullable string columns (see recent `AddAcuanFieldsToCoachingSession` migration as reference).

**Primary recommendation:** Add property to model, scaffold migration with `dotnet ef migrations add`, verify it runs cleanly.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| SUBTAG-02 | PackageQuestion menyimpan field SubCompetency (nullable string) via migration | Direct: add `string? SubCompetency` property + EF Core migration adds `nvarchar(max)` nullable column |
</phase_requirements>

## Standard Stack

### Core
| Library | Purpose | Why Standard |
|---------|---------|--------------|
| EF Core (SQL Server) | ORM + migrations | Already in use project-wide |

No additional packages needed.

## Architecture Patterns

### Model Location
- **File:** `Models/AssessmentPackage.cs` (line 27, `PackageQuestion` class)
- **Add property after** `ScoreValue` (line 39), before the Navigation section

### Migration Pattern (from existing codebase)
The recent `AddAcuanFieldsToCoachingSession` migration is the exact template:
```csharp
migrationBuilder.AddColumn<string>(
    name: "SubCompetency",
    table: "PackageQuestions",
    type: "nvarchar(max)",
    nullable: true);
```

### Scaffold Command
```bash
dotnet ef migrations add AddSubCompetencyToPackageQuestion
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Migration SQL | Hand-written ALTER TABLE | `dotnet ef migrations add` scaffolding |

## Common Pitfalls

### Pitfall 1: Forgetting nullable annotation
**What goes wrong:** Without `?` on the string property, EF Core may generate a NOT NULL column
**How to avoid:** Use `public string? SubCompetency { get; set; }` (nullable reference type)

### Pitfall 2: Table name mismatch
**What goes wrong:** Using wrong table name in migration
**How to avoid:** EF Core convention pluralizes: model `PackageQuestion` -> table `PackageQuestions`

## Code Examples

### Property Addition
```csharp
// In Models/AssessmentPackage.cs, inside PackageQuestion class
public string? SubCompetency { get; set; }
```

### Expected Migration Up
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "SubCompetency",
        table: "PackageQuestions",
        type: "nvarchar(max)",
        nullable: true);
}
```

### Expected Migration Down
```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "SubCompetency",
        table: "PackageQuestions");
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual verification (no automated test framework detected) |
| Quick run command | `dotnet ef database update` |
| Full suite command | `dotnet build && dotnet ef database update` |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SUBTAG-02 | SubCompetency column exists, nullable | smoke | `dotnet ef database update` + verify build | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Per wave merge:** `dotnet ef database update`
- **Phase gate:** Build succeeds, migration applies without error

### Wave 0 Gaps
None -- no test infrastructure needed for a migration-only phase. Build success and migration apply are sufficient validation.

## Open Questions

None. This is a well-understood pattern with clear precedent in the codebase.

## Sources

### Primary (HIGH confidence)
- `Models/AssessmentPackage.cs` lines 27-45 -- current PackageQuestion model
- `Migrations/20260309090731_AddAcuanFieldsToCoachingSession.cs` -- identical pattern reference
- `Program.cs` -- confirms `UseSqlServer`

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - existing project patterns
- Architecture: HIGH - direct codebase inspection
- Pitfalls: HIGH - well-known EF Core behavior

**Research date:** 2026-03-10
**Valid until:** 2026-04-10 (stable domain)
