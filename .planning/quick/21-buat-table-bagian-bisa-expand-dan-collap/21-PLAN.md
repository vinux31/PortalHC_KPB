---
phase: quick-21
plan: 01
type: execute
wave: 1
depends_on: []
files_modified: [Views/ProtonData/Index.cshtml]
autonomous: true
requirements: [QUICK-21]
must_haves:
  truths:
    - "Bagian rows show chevron icon indicating expand/collapse state"
    - "Clicking a Bagian row toggles visibility of its child Unit and Track rows"
    - "Default state is collapsed (children hidden on load)"
  artifacts:
    - path: "Views/ProtonData/Index.cshtml"
      provides: "Expand/collapse JS logic in loadStatusData and click handler"
  key_links:
    - from: "loadStatusData()"
      to: "click handler"
      via: "data-bagian attribute on child rows, bagian-toggle class on parent"
      pattern: "data-bagian|bagian-toggle"
---

<objective>
Make Bagian rows in the Status tab of ProtonData/Index expandable/collapsible. Clicking a Bagian row toggles visibility of its child Unit and Track rows. Default state is collapsed.

Purpose: Improve readability of the Status table when many bagian/unit/track rows exist.
Output: Updated Views/ProtonData/Index.cshtml with expand/collapse behavior.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Views/ProtonData/Index.cshtml (lines 314-342 — loadStatusData function)
</context>

<tasks>

<task type="auto">
  <name>Task 1: Add expand/collapse to Bagian rows in Status tab</name>
  <files>Views/ProtonData/Index.cshtml</files>
  <action>
Modify the `loadStatusData()` function (around lines 314-339) to:

1. On Bagian rows (line 321): Add a chevron icon and data attributes:
   - Add `style="cursor:pointer"` and class `bagian-toggle` to the tr
   - Add a `data-bagian-id` attribute using a sanitized bagian name (replace spaces with dashes)
   - Prepend a chevron icon before bagian text: `<i class="bi bi-chevron-right bagian-chevron me-1"></i>`

2. On Unit rows (line 326): Add `data-bagian="[sanitized-bagian]"` and `class="bagian-child"` and `style="display:none; padding-left:2rem"` (hidden by default)

3. On Track rows (line 335): Add `data-bagian="[sanitized-bagian]"` and `class="bagian-child"` and initial `style="display:none; padding-left:4rem"` (hidden by default)

4. After `$('#statusTableBody').html(html);` (line 337), add click handler delegation:
```javascript
$('#statusTableBody').off('click', '.bagian-toggle').on('click', '.bagian-toggle', function() {
    var bagianId = $(this).data('bagian-id');
    var children = $('#statusTableBody tr.bagian-child[data-bagian="' + bagianId + '"]');
    var chevron = $(this).find('.bagian-chevron');
    children.toggle();
    chevron.toggleClass('bi-chevron-right bi-chevron-down');
});
```

Use a helper variable `bagianId` derived from `currentBagian` to keep Bagian-row and child-row attributes in sync. Example: `var bagianId = row.bagian.replace(/\s+/g, '-');`
  </action>
  <verify>
    <automated>grep -c "bagian-toggle\|bagian-child\|bagian-chevron" Views/ProtonData/Index.cshtml</automated>
  </verify>
  <done>Bagian rows show chevron, clicking toggles child rows, default state is collapsed (children hidden)</done>
</task>

</tasks>

<verification>
- Load ProtonData page, click Status tab
- All Bagian rows show right-chevron, children hidden by default
- Click a Bagian row: children appear, chevron changes to down
- Click again: children hide, chevron changes back to right
</verification>

<success_criteria>
Status tab Bagian rows are expandable/collapsible with chevron indicators. Default collapsed.
</success_criteria>

<output>
After completion, create `.planning/quick/21-buat-table-bagian-bisa-expand-dan-collap/21-SUMMARY.md`
</output>
