---
phase: quick-14
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Models/KkjModels.cs
  - Data/SeedMasterData.cs
  - Controllers/CMPController.cs
autonomous: true
requirements: [QUICK-14]

must_haves:
  truths:
    - "CpdpItem model has a Section property"
    - "EF Core migration adds Section column to CpdpItems table"
    - "All existing CpdpItems rows have Section = 'RFCC' (migration SQL sets default)"
    - "CMPController.Mapping() filters CpdpItems by the section parameter"
    - "Each bagian (RFCC/GAST/NGP/DHT) shows only its own rows on the Mapping page"
  artifacts:
    - path: "Models/KkjModels.cs"
      provides: "CpdpItem class with Section property"
      contains: "public string Section"
    - path: "Data/SeedMasterData.cs"
      provides: "Seed data with Section = 'RFCC' on every CpdpItem"
      contains: "Section = \"RFCC\""
    - path: "Controllers/CMPController.cs"
      provides: "Mapping() action filters by section"
      contains: "Where(c => c.Section == section)"
  key_links:
    - from: "Controllers/CMPController.cs Mapping()"
      to: "CpdpItems table"
      via: ".Where(c => c.Section == section)"
      pattern: "Where.*Section.*section"
    - from: "EF Core migration"
      to: "CpdpItems table"
      via: "migrationBuilder.AddColumn + Sql UPDATE"
      pattern: "AddColumn.*Section"
---

<objective>
Add a Section column to the CpdpItem model and database table, then filter the Mapping() controller action so each bagian only sees its own data.

Purpose: CpdpItems currently has no section discriminator, so all 4 bagian (RFCC/GAST/NGP/DHT) see identical rows. Adding Section enables per-section data management.
Output: Section column in model + migration, existing rows set to "RFCC", Mapping() filtered by section param.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md

Key facts gathered during planning:
- CpdpItem model is in Models/KkjModels.cs at line 71, no Section property exists
- 10 CpdpItems in seed (SeedMasterData.cs lines 156-212), ALL are RFCC NHT content
- CMPController.Mapping() at lines 75-91 loads all CpdpItems with no WHERE clause
- Migration naming convention: timestamp_PascalCaseName (e.g. 20260225100000_ClearUserPackageAssignments)
- EF Core provider: SQLite (HcPortal.db)
- ApplicationDbContext maps CpdpItem to table "CpdpItems" (no Fluent config needed for new string prop)
- 4 valid sections: RFCC, GAST, NGP, DHT
</context>

<tasks>

<task type="auto">
  <name>Task 1: Add Section property to CpdpItem model and update seed data</name>
  <files>Models/KkjModels.cs, Data/SeedMasterData.cs</files>
  <action>
In Models/KkjModels.cs, add `public string Section { get; set; } = "";` to CpdpItem after the Status property (line ~80):

```csharp
public string Status { get; set; } = "";
public string Section { get; set; } = "";   // RFCC | GAST | NGP | DHT
```

In Data/SeedMasterData.cs, add `Section = "RFCC"` to every `new CpdpItem { ... }` initializer in SeedCpdpItemsAsync(). There are 10 CpdpItem objects (lines 159-211). Add it as the last property before the closing `}` of each initializer. All 10 items are RFCC NHT content so all get Section = "RFCC".

The SeedCpdpItemsAsync already has an `if (await context.CpdpItems.AnyAsync()) return;` guard, so the seed only runs on fresh databases. Existing rows are handled in Task 2 via migration SQL.
  </action>
  <verify>
Build compiles without error:
`cd C:/Users/Administrator/Desktop/PortalHC_KPB && dotnet build --no-restore -q 2>&1 | tail -5`
  </verify>
  <done>CpdpItem has Section property; all 10 seed entries have Section = "RFCC"; `dotnet build` exits 0</done>
</task>

<task type="auto">
  <name>Task 2: Create EF Core migration and filter CMPController.Mapping() by section</name>
  <files>Migrations/ (auto-generated), Controllers/CMPController.cs</files>
  <action>
**Step A — Generate migration:**
Run from project root:
```bash
cd C:/Users/Administrator/Desktop/PortalHC_KPB
dotnet ef migrations add AddSectionToCpdpItem
```
This creates `Migrations/{timestamp}_AddSectionToCpdpItem.cs` and `...Designer.cs`.

**Step B — Edit the migration to backfill existing rows:**
Open the newly generated migration file. In the `Up()` method, AFTER the `migrationBuilder.AddColumn<string>(...)` call, add:
```csharp
migrationBuilder.Sql("UPDATE CpdpItems SET Section = 'RFCC' WHERE Section IS NULL OR Section = ''");
```
This ensures the 10 existing live-database rows get Section = 'RFCC'. The column default from EF Core will be empty string (model default), so this UPDATE is necessary.

**Step C — Apply migration:**
```bash
dotnet ef database update
```

**Step D — Filter CMPController.Mapping() by section:**
In Controllers/CMPController.cs, change the CPDP query in the Mapping() action (around line 86):

Before:
```csharp
var cpdpData = await _context.CpdpItems
    .OrderBy(c => c.No)
    .ToListAsync();
```

After:
```csharp
var cpdpData = await _context.CpdpItems
    .Where(c => c.Section == section)
    .OrderBy(c => c.No)
    .ToListAsync();
```

No using statement changes needed (LINQ Where is already available via Microsoft.EntityFrameworkCore).
  </action>
  <verify>
1. Migration file exists: `ls C:/Users/Administrator/Desktop/PortalHC_KPB/Migrations/*AddSectionToCpdpItem.cs`
2. Migration applied: `dotnet ef migrations list 2>&1 | tail -3` shows AddSectionToCpdpItem (applied)
3. Column exists in DB: `sqlite3 C:/Users/Administrator/Desktop/PortalHC_KPB/HcPortal.db "PRAGMA table_info(CpdpItems);" | grep -i section`
4. Existing rows backfilled: `sqlite3 C:/Users/Administrator/Desktop/PortalHC_KPB/HcPortal.db "SELECT COUNT(*) FROM CpdpItems WHERE Section='RFCC';"` returns 10
5. Build still clean: `dotnet build --no-restore -q 2>&1 | tail -3`
  </verify>
  <done>
- Migration file exists and is applied
- CpdpItems.Section column in SQLite with all 10 rows = 'RFCC'
- CMPController.Mapping() has .Where(c => c.Section == section) in query
- dotnet build exits 0
- Visiting /CMP/Mapping?section=RFCC shows 10 rows; /CMP/Mapping?section=GAST shows 0 rows (correct — no GAST data seeded yet)
  </done>
</task>

</tasks>

<verification>
Manual smoke test after tasks complete:
1. Run app: `dotnet run` (or use existing dev server)
2. Navigate to /CMP/Mapping — should redirect to MappingSectionSelect (section param missing)
3. Click RFCC — should show 10 competency rows
4. Click GAST — should show empty table (no GAST data seeded yet, which is correct)
5. Click NGP / DHT — should also show empty tables
</verification>

<success_criteria>
- CpdpItem model compiles with Section property
- Migration AddSectionToCpdpItem is applied to HcPortal.db
- All 10 existing CpdpItems rows have Section = 'RFCC' in the database
- CMPController.Mapping() filters by section, so RFCC shows its rows and GAST/NGP/DHT show empty (pending their own seed data)
- dotnet build exits 0
</success_criteria>

<output>
After completion, create `.planning/quick/14-add-section-column-to-cpdpitem-model-mig/14-SUMMARY.md` using the standard summary template.
Update `.planning/STATE.md` Quick Tasks Completed table with entry for quick task 14.
</output>
