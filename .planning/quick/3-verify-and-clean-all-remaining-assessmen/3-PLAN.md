---
phase: quick-3
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Models/ReportsDashboardViewModel.cs
  - Models/CDPDashboardViewModel.cs
autonomous: true
must_haves:
  truths:
    - "No orphaned Assessment Analytics access points exist in the CMP module"
    - "No dead ViewModel classes remain in the codebase"
    - "UserAssessmentHistoryViewModel still works for CMP/UserAssessmentHistory"
  artifacts:
    - path: "Models/ReportsDashboardViewModel.cs"
      provides: "Only UserAssessmentHistoryViewModel (ReportsDashboardViewModel class removed)"
    - path: "Models/CDPDashboardViewModel.cs"
      provides: "Accurate comments (no stale 'will be deleted' references)"
  key_links:
    - from: "Controllers/CMPController.cs"
      to: "Models/ReportsDashboardViewModel.cs"
      via: "new UserAssessmentHistoryViewModel"
      pattern: "UserAssessmentHistoryViewModel"
    - from: "Views/CMP/UserAssessmentHistory.cshtml"
      to: "Models/ReportsDashboardViewModel.cs"
      via: "@model directive"
      pattern: "UserAssessmentHistoryViewModel"
---

<objective>
Verify that no stale Assessment Analytics access points remain in the CMP module after the card was removed from CMP/Index.cshtml, and clean up the one orphaned artifact found during sweep.

Purpose: Complete the removal of legacy Assessment Analytics access paths from CMP, ensuring the CDP/Dashboard Analytics tab is the single canonical location.
Output: Clean codebase with no orphaned ReportsDashboardViewModel class and no stale comments.
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@Models/ReportsDashboardViewModel.cs
@Models/CDPDashboardViewModel.cs
</context>

<sweep_results>
A thorough codebase sweep was performed during planning. Here are the findings:

ALREADY CLEAN (no action needed):
- Views/CMP/Index.cshtml: Assessment Analytics card already removed. Only 4 cards remain (KKJ Matrix, CPDP Mapping, Assessments, Training Records).
- Controllers/CMPController.cs: No ReportsIndex action exists. No Assessment Analytics references.
- Views/Shared/_Layout.cshtml: No Assessment Analytics or HC Reports nav links. Only a standard "CMP" link to CMP/Index.
- Views/CMP/ReportsIndex.cshtml: Already deleted (Phase 12-03).
- Views/CDP/DevDashboard.cshtml: Already deleted (Phase 12-03).
- Models/DevDashboardViewModel.cs: Already deleted (Phase 12-03).
- No sidebar, nav, or layout references to Assessment Analytics in CMP context anywhere.

ACTIVE AND CORRECT (do NOT touch):
- Views/CDP/Dashboard.cshtml: Assessment Analytics tab (role-gated, HC/Admin only) -- this is the canonical location.
- Views/CDP/Shared/_AssessmentAnalyticsPartial.cshtml: Analytics partial view -- active.
- Controllers/CDPController.cs: BuildAnalyticsSubModelAsync -- active helper.

NEEDS CLEANUP (one item):
- Models/ReportsDashboardViewModel.cs: Contains orphaned ReportsDashboardViewModel class (zero references) AND still-active UserAssessmentHistoryViewModel class. The orphaned class must be removed; the active class must be preserved.
- Models/CDPDashboardViewModel.cs: Contains stale comment "ReportsDashboardViewModel will be deleted in Plan 12-03" that was never updated.
</sweep_results>

<tasks>

<task type="auto">
  <name>Task 1: Remove orphaned ReportsDashboardViewModel class and clean stale comments</name>
  <files>Models/ReportsDashboardViewModel.cs, Models/CDPDashboardViewModel.cs</files>
  <action>
1. In Models/ReportsDashboardViewModel.cs:
   - DELETE the entire `ReportsDashboardViewModel` class (lines 3-18: the class with Assessments, TotalAssessments, PassedCount, PassRate, AverageScore, TotalAssigned, CurrentPage, TotalPages, PageSize, CurrentFilters, AvailableCategories, AvailableSections properties).
   - KEEP the `UserAssessmentHistoryViewModel` class (lines 20-32) intact -- it is still actively used by CMPController.UserAssessmentHistory action and Views/CMP/UserAssessmentHistory.cshtml.
   - KEEP the namespace declaration.

2. In Models/CDPDashboardViewModel.cs:
   - Update the comment block at lines 88-92. Change from:
     ```
     // Supporting classes (copied from DevDashboardViewModel + ReportsDashboardViewModel)
     // These classes are self-contained here; DevDashboardViewModel and
     // ReportsDashboardViewModel will be deleted in Plan 12-03.
     ```
     To:
     ```
     // Supporting classes for CDP Dashboard
     // (Originally from DevDashboardViewModel + ReportsDashboardViewModel, now canonical here)
     ```
     This removes the stale "will be deleted" reference since that cleanup is now being completed.

3. Build the project to confirm no compilation errors:
   ```
   dotnet build
   ```
  </action>
  <verify>
- `dotnet build` succeeds with no errors
- `grep -r "ReportsDashboardViewModel" --include="*.cs" --include="*.cshtml" .` returns zero matches (the class is gone)
- `grep -r "UserAssessmentHistoryViewModel" --include="*.cs" --include="*.cshtml" .` still returns 3 matches (CMPController.cs, ReportsDashboardViewModel.cs, UserAssessmentHistory.cshtml)
- `grep -r "will be deleted" Models/CDPDashboardViewModel.cs` returns zero matches
  </verify>
  <done>
- ReportsDashboardViewModel class is deleted from the codebase (zero references confirmed)
- UserAssessmentHistoryViewModel remains functional in its existing file
- CDPDashboardViewModel.cs has accurate, non-stale comments
- Project compiles successfully
  </done>
</task>

</tasks>

<verification>
1. `dotnet build` passes -- no broken references
2. Full codebase grep for "ReportsDashboardViewModel" in .cs/.cshtml files returns zero matches
3. CMP/UserAssessmentHistory page still works (UserAssessmentHistoryViewModel intact)
4. No Assessment Analytics references exist in CMP module views or controller (confirmed in sweep)
</verification>

<success_criteria>
- Zero orphaned Assessment Analytics artifacts remain in the codebase
- CMP/Index.cshtml has no Assessment Analytics card (already confirmed)
- No nav/sidebar links to Assessment Analytics exist in CMP context (already confirmed)
- ReportsDashboardViewModel class deleted, UserAssessmentHistoryViewModel preserved
- Project builds cleanly
</success_criteria>

<output>
After completion, create `.planning/quick/3-verify-and-clean-all-remaining-assessmen/3-SUMMARY.md`
</output>
