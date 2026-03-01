# Phase 81: Cleanup — Remove Old Entry Points - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Remove redundant entry points from ManageAssessment and the Kelola Data hub, add Manage Questions shortcut to ManageAssessment dropdown, and fix Assessment Monitoring table height. Four items total: two removals, one addition, one styling fix.

</domain>

<decisions>
## Implementation Decisions

### Monitoring Dropdown Removal (CLN-01)
- Remove only the "Monitoring" item (`<i class="bi bi-binoculars">`) from the ManageAssessment per-group dropdown
- Keep all other items: Edit, Export Excel, Regenerate Token, Delete
- Monitoring is now accessed from the dedicated Assessment Monitoring page (Phase 79/80)

### Training Records Hub Card Removal (CLN-02)
- Remove the "Training Records" card from Admin/Index.cshtml Section C (lines 139-154)
- CMP/Records page remains accessible from ManageAssessment's Training Records tab
- After removal, Section C has 2 cards: Manage Assessment & Training + Assessment Monitoring

### Manage Questions Dropdown Item (new)
- Add "Manage Questions" item to ManageAssessment per-group dropdown
- Dropdown label: "Manage Questions"
- Link to `Admin/ManageQuestions/{id}` — new action in AdminController
- Reuse the same layout and logic from existing `CMP/ManageQuestions` (2-column: add form left, question list right)
- URL pattern: `Admin/ManageQuestions/{assessmentId}`, NOT CMP path
- Page title (header): "Manage Questions"
- Breadcrumb: Kelola Data > Manage Assessment > Kelola Soal
- Back button points to ManageAssessment
- Same Add + Delete functionality (no edit), same authorization (Admin, HC)

### Assessment Monitoring Table Height
- Apply `min-height: calc(100vh - 420px); overflow-y: auto;` to the table container in AssessmentMonitoring.cshtml
- Same pattern used by ManageAssessment table (line 161 of ManageAssessment.cshtml)
- Makes the table fill the screen vertically

### Claude's Discretion
- Exact dropdown item icon for "Manage Questions" (suggest `bi-question-circle` or `bi-list-check`)
- Whether to copy the CMP view file or create a new one that shares the same layout
- Order of dropdown items after adding Manage Questions

</decisions>

<specifics>
## Specific Ideas

- ManageQuestions under Admin should feel like the same page as CMP/ManageQuestions, just with Admin navigation context
- The existing CMP/ManageQuestions pattern: textarea for question, 4 option inputs (A-D), radio for correct answer, delete with confirm

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Views/CMP/ManageQuestions.cshtml`: Complete 2-column question management page (add form + list)
- `CMPController.cs` ManageQuestions action (line 1411): Loads AssessmentSession with Questions/Options
- `CMPController.cs` AddQuestion action (line 1426): Creates question + 4 options
- `CMPController.cs` DeleteQuestion action (line 1464): Deletes question by ID
- ManageAssessment.cshtml dropdown (lines 249-260): Template for adding new dropdown item

### Established Patterns
- Dropdown items use `<a class="dropdown-item">` with Bootstrap icons
- POST actions use `@Html.AntiForgeryToken()` + form submission
- ManageAssessment table uses `min-height: calc(100vh - 420px); overflow-y: auto;` for full-screen height
- AdminController uses `[Authorize(Roles = "Admin, HC")]` for assessment actions

### Integration Points
- ManageAssessment.cshtml dropdown menu (line 249+): Add "Manage Questions" item
- AdminController.cs: Add ManageQuestions/AddQuestion/DeleteQuestion actions (mirror CMP pattern)
- Admin/Index.cshtml Section C (lines 139-154): Remove Training Records card
- AssessmentMonitoring.cshtml table container: Add min-height styling

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 81-cleanup-remove-old-entry-points*
*Context gathered: 2026-03-01*
