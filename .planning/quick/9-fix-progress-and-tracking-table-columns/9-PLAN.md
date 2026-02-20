---
phase: quick-9
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Models/TrackingModels.cs
  - Controllers/CDPController.cs
  - Views/CDP/Progress.cshtml
autonomous: true
must_haves:
  truths:
    - "Progress and Tracking table has no Implementasi column"
    - "Column header reads Kompetensi (not Nama Kompetensi CPDP)"
    - "Column header reads Sub Kompetensi (not Nama Sub Kompetensi)"
    - "Deliverable column appears after Sub Kompetensi and before Evidence"
  artifacts:
    - path: "Views/CDP/Progress.cshtml"
      provides: "Updated table headers and data cells"
    - path: "Models/TrackingModels.cs"
      provides: "Deliverable property on TrackingItem"
    - path: "Controllers/CDPController.cs"
      provides: "Deliverable mapping from IdpItem to TrackingItem"
  key_links:
    - from: "Controllers/CDPController.cs"
      to: "Models/TrackingModels.cs"
      via: "TrackingItem.Deliverable = idp.Deliverable"
      pattern: "Deliverable = idp\\.Deliverable"
    - from: "Views/CDP/Progress.cshtml"
      to: "Models/TrackingModels.cs"
      via: "item.Deliverable in table cell"
      pattern: "item\\.Deliverable"
---

<objective>
Fix the Progress & Tracking table columns: remove Implementasi, rename Kompetensi/Sub Kompetensi headers (drop "Nama" prefix), and add a Deliverable column between Sub Kompetensi and Evidence.

Purpose: Align table columns with user expectations — cleaner labels and the missing Deliverable data now visible.
Output: Updated view, model, and controller mapping.
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Views/CDP/Progress.cshtml
@Models/TrackingModels.cs
@Controllers/CDPController.cs (Progress action, lines ~1416-1505)
@Models/IdpItem.cs (has Deliverable property already — line 14)
</context>

<tasks>

<task type="auto">
  <name>Task 1: Add Deliverable to TrackingItem model and controller mapping</name>
  <files>Models/TrackingModels.cs, Controllers/CDPController.cs</files>
  <action>
1. In `Models/TrackingModels.cs`, add a `Deliverable` property to `TrackingItem`:
   ```csharp
   public string Deliverable { get; set; } = "";
   ```
   Add it after the `SubKompetensi` property (line 8), before `EvidenceStatus`.

2. In `Controllers/CDPController.cs`, in the `Progress` action method (~line 1491), update the `IdpItem -> TrackingItem` mapping to include:
   ```csharp
   Deliverable = idp.Deliverable ?? "",
   ```
   Add this line after `SubKompetensi = idp.SubKompetensi ?? "",` (after line 1495).

Note: `IdpItem` already has a `Deliverable` property (confirmed in IdpItem.cs line 14). No schema change needed.
  </action>
  <verify>Run `dotnet build` from project root — must compile with zero errors.</verify>
  <done>TrackingItem has Deliverable property; controller maps it from IdpItem.Deliverable.</done>
</task>

<task type="auto">
  <name>Task 2: Update Progress.cshtml table — remove Implementasi, rename headers, add Deliverable column</name>
  <files>Views/CDP/Progress.cshtml</files>
  <action>
In `Views/CDP/Progress.cshtml`, make these changes to the table:

**A. Table header (thead, lines 241-251):**

1. Rename "Nama Kompetensi CPDP" to "Kompetensi" (line 243).
2. DELETE the "Implementasi" th entirely (line 244).
3. Rename "Nama Sub Kompetensi" to "Sub Kompetensi" (line 245).
4. ADD a new th for "Deliverable" AFTER "Sub Kompetensi" and BEFORE "Evidence":
   ```html
   <th class="p-3" style="width: 15%">Deliverable</th>
   ```
5. Redistribute column widths so they total ~100%. Suggested widths:
   - Kompetensi: 20%
   - Sub Kompetensi: 15%
   - Deliverable: 15%
   - Evidence: 10%
   - Sr. Supervisor: 10%
   - Section Head: 10%
   - HC: 10%
   - Action: 5%

**B. Table body (tbody, lines 254-397):**

1. DELETE the Implementasi td cell that renders `@item.Periode` (line 258).
2. ADD a new td for Deliverable AFTER the Sub Kompetensi td and BEFORE the Evidence td:
   ```html
   <td class="p-3 text-dark small">@item.Deliverable</td>
   ```

Do NOT change the Evidence, Approval, or Action columns — leave them exactly as they are.
  </action>
  <verify>Run `dotnet build` from project root — must compile with zero errors. Visually inspect the rendered HTML structure by checking that the thead has exactly 8 th elements in this order: Kompetensi, Sub Kompetensi, Deliverable, Evidence, Sr. Supervisor, Section Head, HC, Action.</verify>
  <done>Table shows 8 columns in correct order. No Implementasi column. No Nama prefix on Kompetensi or Sub Kompetensi headers. Deliverable column displays between Sub Kompetensi and Evidence.</done>
</task>

</tasks>

<verification>
- `dotnet build` compiles without errors
- Progress.cshtml thead contains exactly 8 th elements
- Column order: Kompetensi | Sub Kompetensi | Deliverable | Evidence | Sr. Supervisor | Section Head | HC | Action
- No "Implementasi" text anywhere in the table
- No "Nama" prefix in any column header
- `@item.Deliverable` is rendered in the Deliverable column cell
</verification>

<success_criteria>
- Build succeeds
- Table has exactly 8 columns in the specified order
- Deliverable data flows from IdpItem -> TrackingItem -> View
</success_criteria>

<output>
After completion, create `.planning/quick/9-fix-progress-and-tracking-table-columns/9-PLAN.md`
</output>
