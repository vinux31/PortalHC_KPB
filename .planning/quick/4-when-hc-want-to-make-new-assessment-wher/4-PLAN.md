---
phase: quick-004
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Views/CMP/Assessment.cshtml
autonomous: true

must_haves:
  truths:
    - "HC user sees a 'Create Assessment' button on the Assessment manage view at all times, not only when list is empty"
    - "Clicking 'Create Assessment' navigates to CMP/CreateAssessment"
  artifacts:
    - path: "Views/CMP/Assessment.cshtml"
      provides: "Persistent Create Assessment button in manage view header"
      contains: "CreateAssessment"
  key_links:
    - from: "Views/CMP/Assessment.cshtml"
      to: "CMPController.CreateAssessment"
      via: "asp-action link"
      pattern: "asp-action=\"CreateAssessment\""
---

<objective>
Add a persistent "Create Assessment" button to the Assessment manage view header so HC users can always create new assessments regardless of whether assessments already exist.

Purpose: Currently the "Create Assessment" button only appears in the empty state (when managementData.Count == 0). HC users with existing assessments have no way to create new ones from the UI. This is a UX gap introduced when the Assessment Analytics card was removed from CMP Index in v1.2.

Output: Assessment.cshtml updated with a visible "Create Assessment" button in the manage view header area.
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Views/CMP/Assessment.cshtml
@Controllers/CMPController.cs (CreateAssessment action exists at lines 392-427, authorized for Admin+HC)
</context>

<tasks>

<task type="auto">
  <name>Task 1: Add persistent Create Assessment button to manage view header</name>
  <files>Views/CMP/Assessment.cshtml</files>
  <action>
In Views/CMP/Assessment.cshtml, locate the header section (lines 12-53) where the manage/personal view toggle buttons are rendered inside the `@if (canManage)` block.

Add a "Create Assessment" button next to the existing view toggle button, but ONLY visible in manage view mode. The button should:
1. Be placed inside the `@if (viewMode == "manage")` branch (around line 39-43), alongside the "Personal View" button
2. Link to `asp-action="CreateAssessment"` on the CMP controller
3. Use Bootstrap btn-success styling with a bi-plus-circle icon to visually distinguish it from the "Personal View" outline button
4. Text: "Create Assessment"

Implementation: Inside the `@if (viewMode == "manage")` block (line 39), wrap both the existing "Personal View" button and the new "Create Assessment" button in a `d-flex gap-2` container:

```razor
@if (viewMode == "manage")
{
    <div class="d-flex gap-2">
        <a asp-action="CreateAssessment" class="btn btn-success">
            <i class="bi bi-plus-circle me-1"></i> Create Assessment
        </a>
        <a asp-action="Assessment" asp-route-view="personal" class="btn btn-outline-primary">
            <i class="bi bi-person-circle me-1"></i> Personal View
        </a>
    </div>
}
```

Do NOT remove the existing empty-state "Create Assessment" button (line 123-125) — it serves as an onboarding CTA when no assessments exist yet. The header button ensures discoverability at all times.

Do NOT modify the personal view or worker view sections.
  </action>
  <verify>
Open the file and confirm:
1. The `d-flex gap-2` wrapper contains both buttons in the manage view branch
2. The `asp-action="CreateAssessment"` link is present with btn-success styling
3. The existing "Personal View" button remains unchanged
4. The empty-state Create Assessment button (around line 123) is still present
5. No changes to the worker/personal view sections
  </verify>
  <done>
HC user visiting CMP/Assessment?view=manage sees a green "Create Assessment" button in the page header at all times, whether the assessment list is empty or populated. Clicking it navigates to the CreateAssessment form.
  </done>
</task>

</tasks>

<verification>
- Visual: Navigate to /CMP/Assessment?view=manage as HC role — "Create Assessment" button visible in header
- Navigation: Click "Create Assessment" — arrives at /CMP/CreateAssessment form
- No regression: "Personal View" toggle still works, worker view unchanged, empty-state CTA still present
</verification>

<success_criteria>
- Green "Create Assessment" button visible in manage view header alongside "Personal View" toggle
- Button links to CMP/CreateAssessment (existing controller action)
- No changes to worker/personal view
</success_criteria>

<output>
After completion, create `.planning/quick/4-when-hc-want-to-make-new-assessment-wher/4-SUMMARY.md`
</output>
