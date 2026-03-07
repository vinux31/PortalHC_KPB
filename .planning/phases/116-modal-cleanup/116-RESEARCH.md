# Phase 116: Modal Cleanup - Research

**Researched:** 2026-03-07
**Domain:** ASP.NET Core field removal (view, controller, model, migration)
**Confidence:** HIGH

## Summary

Phase 116 is a pure removal task: delete the "Kompetensi Coachee" textarea from the CoachingProton evidence modal and clean up all related backend code. All touchpoints have been verified in the codebase at exact line numbers. The scope is small and well-defined -- 7 file edits plus 1 new migration.

The CoachingLog model also has a CoacheeCompetencies property, but per CONTEXT.md decisions, only CoachingSession is in scope.

**Primary recommendation:** Execute as a single plan -- remove property, remove UI/JS/controller references, add data-clearing migration, verify build compiles.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Run a migration to set all existing CoacheeCompetencies values to empty string
- Column remains in DB table (no column drop), but data is cleared
- Remove CoacheeCompetencies column (header + cell) from the coaching session table in Deliverable.cshtml
- Delete the CoacheeCompetencies property from CoachingSession C# model
- EF Core won't auto-drop the DB column -- it stays but is unmapped

### Claude's Discretion
- Migration naming convention
- Order of changes within the implementation

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MOD-01 | Field "Kompetensi Coachee" dihapus dari evidence modal CoachingProton (textarea #evidenceKoacheeComp) | View textarea (line 866-870), JS clear (line 1342,1347), JS formData.append (line 1383) all confirmed |
| MOD-02 | CoachingSession model dan SubmitEvidenceWithCoaching action tidak lagi menyimpan/menerima field koacheeCompetencies | Model property (CoachingSession.cs line 12), controller param (CDPController.cs line 1884), assignment (line 1994) all confirmed |
</phase_requirements>

## Standard Stack

Not applicable -- this phase uses only existing project infrastructure (ASP.NET Core, EF Core, Razor views). No new libraries needed.

## Architecture Patterns

### Verified Touchpoints (7 edits + 1 new file)

| File | Line(s) | What to Remove/Change |
|------|---------|----------------------|
| `Views/CDP/CoachingProton.cshtml` | 866-870 | Entire `<div class="mb-3">` containing textarea `#evidenceKoacheeComp` |
| `Views/CDP/CoachingProton.cshtml` | 1342, 1347 | JS lines: `const koacheeComp = ...` and `if (koacheeComp) koacheeComp.value = '';` |
| `Views/CDP/CoachingProton.cshtml` | 1383 | `formData.append('koacheeCompetencies', ...)` line |
| `Controllers/CDPController.cs` | 1884 | `[FromForm] string koacheeCompetencies` parameter |
| `Controllers/CDPController.cs` | 1994 | `CoacheeCompetencies = koacheeCompetencies` assignment |
| `Models/CoachingSession.cs` | 12 | `public string CoacheeCompetencies` property |
| `Views/CDP/Deliverable.cshtml` | 385-387 | `<tr>` block showing Kompetensi Coachee |
| New migration file | -- | SQL UPDATE to clear existing data |

### Migration Pattern
Data-only migrations in this project use `migrationBuilder.Sql()` with raw SQL. Example from `20260306101100_MoveSectionHeadToLevel4.cs`:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"UPDATE CoachingSessions SET CoacheeCompetencies = ''");
}
```

Since the property is being removed from the model, EF Core will generate a migration that drops the column. Per the locked decision, the column must remain. This means the migration should be **hand-written** (SQL-only data clearing), NOT generated via `dotnet ef migrations add`. After removing the property, EF Core's model snapshot will no longer track the column, so it stays in DB but is unmapped.

**Important nuance:** If `dotnet ef migrations add` is run after removing the property, EF Core will auto-generate a `DropColumn` call. To avoid this, write the data-clearing migration BEFORE removing the property, OR hand-write the migration class without using the scaffold command. The cleanest approach: hand-write the migration (just the SQL update), then remove the property, then run `dotnet ef migrations add` to get the Designer/Snapshot updated -- but modify the generated Up/Down to remove the DropColumn/AddColumn calls.

### Anti-Patterns to Avoid
- **Running `dotnet ef migrations add` after property removal without review:** Will auto-generate column drop, violating the "column stays" decision. Always inspect generated migration code.

## Don't Hand-Roll

Not applicable -- no complex problems to solve. Pure removal task.

## Common Pitfalls

### Pitfall 1: EF Core Auto-Dropping Column
**What goes wrong:** Removing the C# property and running `dotnet ef migrations add` generates a `DropColumn` migration.
**Why it happens:** EF Core Code-First tracks model-to-schema mapping; missing property = drop column.
**How to avoid:** Hand-write the data-clearing migration. For the snapshot update, either (a) manually edit the generated migration to remove DropColumn, or (b) accept the unmapped column divergence and skip snapshot regeneration.
**Recommended approach:** Write a hand-crafted migration that only clears data. Do NOT regenerate the snapshot -- the column being unmapped but present in DB is harmless for this project's usage.

### Pitfall 2: CoachingLog Also Has the Field
**What goes wrong:** CoachingLog.CoacheeCompetencies (line 25) is left untouched, potentially confusing future developers.
**How to avoid:** This is explicitly out of scope per CONTEXT.md. The planner should note this as a known leftover but not include it in tasks.

### Pitfall 3: Forgetting the Deliverable Display
**What goes wrong:** Modal and controller are cleaned up but Deliverable.cshtml still shows the now-empty field.
**How to avoid:** Include the Deliverable.cshtml `<tr>` removal (lines 385-387) in the same task.

## Code Examples

### Hand-Written Migration
```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    public partial class ClearCoacheeCompetenciesData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE CoachingSessions SET CoacheeCompetencies = ''");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data clearing is irreversible -- no meaningful rollback
        }
    }
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test suite in project) |
| Config file | none |
| Quick run command | `dotnet build` (compile check) |
| Full suite command | `dotnet run` + manual browser verification |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MOD-01 | Evidence modal has no Kompetensi Coachee textarea | manual | `dotnet build` (compile only) | N/A |
| MOD-02 | SubmitEvidenceWithCoaching no longer accepts/stores koacheeCompetencies | manual | `dotnet build` (compile only) | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build` to verify no compile errors
- **Per wave merge:** Manual browser test: open CoachingProton evidence modal, submit evidence, verify Deliverable display
- **Phase gate:** Build green + manual verification of both requirements

### Wave 0 Gaps
None -- no automated test infrastructure exists in this project. Manual verification is the established pattern.

## Open Questions

None -- all touchpoints verified, approach is clear.

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection of all 7 touchpoint files
- Existing migration pattern from `20260306101100_MoveSectionHeadToLevel4.cs`

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries, pure removal
- Architecture: HIGH - all touchpoints verified at exact line numbers
- Pitfalls: HIGH - EF Core column-drop behavior is well-known

**Research date:** 2026-03-07
**Valid until:** 2026-04-07 (stable -- code removal, no external dependencies)
