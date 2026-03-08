---
phase: 20-move-target-to-deliverable
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Models/ProtonModels.cs
  - Controllers/ProtonDataController.cs
  - Controllers/CDPController.cs
  - Views/ProtonData/Index.cshtml
  - Views/CDP/PlanIdp.cshtml
  - Migrations/YYYYMMDDHHMMSS_MoveTargetToDeliverable.cs
autonomous: true
requirements: [MOVE-TARGET-01]

must_haves:
  truths:
    - "Target field belongs to ProtonDeliverable, not ProtonSubKompetensi"
    - "Existing Target data migrated from SubKompetensi to all child Deliverables"
    - "Silabus view table shows Target per deliverable row (no SubKompetensi rowspan)"
    - "Silabus edit mode saves Target per deliverable"
    - "PlanIdp view shows Target per deliverable row"
  artifacts:
    - path: "Models/ProtonModels.cs"
      provides: "Target property on ProtonDeliverable, removed from ProtonSubKompetensi"
      contains: "public string? Target"
    - path: "Migrations/YYYYMMDDHHMMSS_MoveTargetToDeliverable.cs"
      provides: "Data migration copying Target from SubKompetensi to Deliverables"
  key_links:
    - from: "Controllers/ProtonDataController.cs"
      to: "ProtonDeliverable.Target"
      via: "silabusRows uses d.Target, SilabusSave writes to deliverable.Target"
      pattern: "d\\.Target"
    - from: "Views/ProtonData/Index.cshtml"
      to: "silabusRows[].Target"
      via: "JS renders Target per deliverable row, not per SubKompetensi rowspan"
---

<objective>
Move the Target field from ProtonSubKompetensi to ProtonDeliverable so each deliverable has its own independent Target value. Includes data migration, model change, controller updates, and view JS updates.

Purpose: Allows different deliverables under the same sub-kompetensi to have different targets, which better reflects real-world requirements.
Output: Working silabus with per-deliverable Target in both view and edit modes.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Models/ProtonModels.cs
@Controllers/ProtonDataController.cs
@Controllers/CDPController.cs
@Views/ProtonData/Index.cshtml
@Views/CDP/PlanIdp.cshtml

<interfaces>
From Models/ProtonModels.cs:
```csharp
public class ProtonSubKompetensi
{
    public int Id { get; set; }
    public int ProtonKompetensiId { get; set; }
    public ProtonKompetensi? ProtonKompetensi { get; set; }
    public string NamaSubKompetensi { get; set; } = "";
    public string? Target { get; set; }  // <-- REMOVE THIS
    public int Urutan { get; set; }
    public ICollection<ProtonDeliverable> Deliverables { get; set; } = new List<ProtonDeliverable>();
}

public class ProtonDeliverable
{
    public int Id { get; set; }
    public int ProtonSubKompetensiId { get; set; }
    public ProtonSubKompetensi? ProtonSubKompetensi { get; set; }
    public string NamaDeliverable { get; set; } = "";
    public int Urutan { get; set; }
    // <-- ADD: public string? Target { get; set; }
}
```

From Controllers/ProtonDataController.cs (SilabusRowDto):
```csharp
public class SilabusRowDto
{
    public int KompetensiId { get; set; }
    public string Kompetensi { get; set; } = "";
    public int SubKompetensiId { get; set; }
    public string SubKompetensi { get; set; } = "";
    public int DeliverableId { get; set; }
    public string Deliverable { get; set; } = "";
    public string No { get; set; } = "";
    public string Target { get; set; } = "";  // Already per-row in DTO, no change needed
    public string Bagian { get; set; } = "";
    public string Unit { get; set; } = "";
    public int TrackId { get; set; }
}
```

SQL table names (from STATE.md decisions):
- ProtonDeliverableList (singular)
- ProtonSubKompetensiList
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Model change + EF migration with data migration</name>
  <files>Models/ProtonModels.cs, Migrations/YYYYMMDDHHMMSS_MoveTargetToDeliverable.cs</files>
  <action>
1. In `Models/ProtonModels.cs`:
   - Add `public string? Target { get; set; }` to `ProtonDeliverable` class (after `NamaDeliverable`)
   - Remove `public string? Target { get; set; }` from `ProtonSubKompetensi` class

2. Run `dotnet ef migrations add MoveTargetToDeliverable` to generate the migration.

3. Edit the generated migration to add data migration SQL BEFORE dropping the old column. In the `Up()` method, insert this SQL right at the beginning (before any schema changes):
   ```csharp
   // Copy Target from SubKompetensi to all child Deliverables BEFORE dropping the column
   migrationBuilder.Sql(@"
       UPDATE d
       SET d.Target = s.Target
       FROM ProtonDeliverableList d
       INNER JOIN ProtonSubKompetensiList s ON d.ProtonSubKompetensiId = s.Id
       WHERE s.Target IS NOT NULL;
   ");
   ```
   The migration scaffolder will generate: (a) AddColumn Target to ProtonDeliverableList, (b) DropColumn Target from ProtonSubKompetensiList. Make sure the SQL runs AFTER AddColumn but BEFORE DropColumn. Reorder the generated statements if needed.

4. In the `Down()` method, add reverse data migration:
   ```csharp
   // Copy first deliverable's Target back to SubKompetensi
   migrationBuilder.Sql(@"
       UPDATE s
       SET s.Target = (SELECT TOP 1 d.Target FROM ProtonDeliverableList d WHERE d.ProtonSubKompetensiId = s.Id AND d.Target IS NOT NULL)
       FROM ProtonSubKompetensiList s;
   ");
   ```

5. Run `dotnet ef database update` to apply.
  </action>
  <verify>
    <automated>dotnet ef database update && dotnet build</automated>
  </verify>
  <done>ProtonDeliverable has Target column with data migrated from SubKompetensi. ProtonSubKompetensi no longer has Target. Project builds.</done>
</task>

<task type="auto">
  <name>Task 2: Update controllers and views to use Deliverable.Target</name>
  <files>Controllers/ProtonDataController.cs, Controllers/CDPController.cs, Views/ProtonData/Index.cshtml, Views/CDP/PlanIdp.cshtml</files>
  <action>
**ProtonDataController.cs — Index action (line ~163):**
Change `Target = s.Target ?? "-"` to `Target = d.Target ?? "-"` (read from deliverable, not sub-kompetensi).

**ProtonDataController.cs — SilabusSave action:**
- In the SubKompetensi upsert section (~lines 268-290): Remove ALL `Target = ...` assignments from SubKompetensi creation/update. Specifically:
  - Line ~272: Remove `subKomp.Target = string.IsNullOrWhiteSpace(row.Target) ? null : row.Target.Trim();`
  - Line ~276: Remove `Target = string.IsNullOrWhiteSpace(row.Target) ? null : row.Target.Trim()` from the SubKompetensi constructor
  - Line ~287: Same removal from the other SubKompetensi constructor
- In the Deliverable upsert section (after SubKompetensi): Add Target to the deliverable entity. Find where `deliv.NamaDeliverable = row.Deliverable.Trim()` is set and add `deliv.Target = string.IsNullOrWhiteSpace(row.Target) ? null : row.Target.Trim();` for both update and create paths.

**CDPController.cs — PlanIdp/CoachingProton action (line ~134):**
Change `Target = s.Target ?? ""` to `Target = d.Target ?? ""`.

**Views/ProtonData/Index.cshtml — View mode table (lines ~457-459):**
The Target cell currently uses SubKompetensi rowspan: `<td rowspan="${subSpan}">`. Change it to render per-deliverable (no rowspan). Move the Target cell from the `if (k === j)` block (SubKompetensi first-row) to render on every row, like the Deliverable cell:
```javascript
// Remove from inside "if (k === j)" block:
// html += `<td rowspan="${subSpan}" class="align-middle">${escHtml(dRow.Target || '-')}</td>`;

// Add after SubKompetensi cell block, outside any conditional (renders every row):
html += `<td class="align-middle">${escHtml(dRow.Target || '-')}</td>`;
```

**Views/ProtonData/Index.cshtml — Edit mode (renderEditRow, line ~562):**
No change needed — Target is already rendered per-row as a flat input field.

**Views/ProtonData/Index.cshtml — Insert row (line ~601):**
The insert-row handler copies `Target: currRow.Target || ''`. No change needed.

**Views/CDP/PlanIdp.cshtml (line ~237):**
Same pattern as ProtonData view mode. Change Target from SubKompetensi rowspan to per-deliverable:
```javascript
// Remove from SubKompetensi first-row block:
// html += '<td rowspan="' + subSpan + '" class="align-middle">' + escHtml(dRow.Target || '-') + '</td>';

// Add per-row (outside SubKompetensi conditional):
html += '<td class="align-middle">' + escHtml(dRow.Target || '-') + '</td>';
```
  </action>
  <verify>
    <automated>dotnet build</automated>
  </verify>
  <done>Silabus view shows Target per deliverable row. SilabusSave persists Target to ProtonDeliverable. PlanIdp shows Target per deliverable. CDPController reads d.Target. All builds clean.</done>
</task>

</tasks>

<verification>
1. `dotnet build` succeeds with no errors
2. Navigate to ProtonData/Index, select a track with existing silabus data — Target values display per deliverable row (not merged by SubKompetensi)
3. Enter edit mode, change Target on individual deliverables within same SubKompetensi to different values, save — values persist independently
4. Navigate to CDP/PlanIdp — Target displays per deliverable row
</verification>

<success_criteria>
- ProtonDeliverable model has Target field, ProtonSubKompetensi does not
- Existing data migrated (no data loss)
- Silabus view and edit modes work with per-deliverable Target
- PlanIdp view shows per-deliverable Target
- All controllers read/write Target from ProtonDeliverable
</success_criteria>

<output>
After completion, create `.planning/quick/20-move-target-to-deliverable/20-SUMMARY.md`
</output>
