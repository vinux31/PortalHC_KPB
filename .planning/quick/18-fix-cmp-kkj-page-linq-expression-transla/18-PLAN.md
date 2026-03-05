---
phase: 18
plan: 1
type: execute
wave: 1
depends_on: []
files_modified:
  - "Controllers/CMPController.cs"
autonomous: true
requirements: []
---

<objective>
Fix CMP/KKJ page LINQ expression translation error by replacing StringComparison.OrdinalIgnoreCase with ToLower() comparison

Purpose: Resolve EF Core translation error that prevents page from loading
Output: Working CMP/KKJ page with role-based section filtering
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@C:/Users/Administrator/Desktop/PortalHC_KPB/.planning/STATE.md
@C:/Users/Administrator/Desktop/PortalHC_KPB/Controllers/CMPController.cs
@C:/Users/Administrator/Desktop/PortalHC_KPB/Models/KkjModels.cs
</context>

<tasks>

<task type="auto">
  <name>Fix LINQ StringComparison.OrdinalIgnoreCase in CMPController.cs line 65</name>
  <files>Controllers/CMPController.cs</files>
  <action>
  1. Read the CMPController.cs file to locate the problematic query
  2. Replace `.Equals(currentUser.Section, StringComparison.OrdinalIgnoreCase)` with `.ToLower() == currentUser.Section.ToLower()`
  3. Ensure the fix maintains the same case-insensitive comparison behavior
  4. Apply ToLower() to both sides of the comparison for consistency
  </action>
  <verify>
    <automated>dotnet build --configuration Release</automated>
  </verify>
  <done>Build succeeds without LINQ translation errors</done>
</task>

</tasks>

<verification>
Verify the fix by running the application and accessing the CMP/KKJ page
</verification>

<success_criteria>
- No LINQ translation errors in the application
- CMP/KKJ page loads successfully
- Role-based filtering (L5/L6 users see only their section) works correctly
- Case-insensitive comparison behavior preserved
</success_criteria>

<output>
After completion, create `.planning/quick/18-fix-cmp-kkj-page-linq-expression-transla/18-SUMMARY.md`
</output>