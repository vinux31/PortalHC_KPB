# Phase 82: Cleanup & Rename - Context

**Gathered:** 2026-03-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Rename "Proton Progress" to "Coaching Proton" everywhere (including URLs/routes), remove orphaned/duplicate CMP pages, add AuditLog card to Kelola Data hub, and document the decision to keep Override Silabus & Coaching Guidance tabs.

</domain>

<decisions>
## Implementation Decisions

### Rename scope (CLN-01)
- Full rename including URL/route: `/CDP/ProtonProgress` → `/CDP/CoachingProton`
- Rename the controller action from `ProtonProgress` to `CoachingProton`
- Rename view file `Views/CDP/ProtonProgress.cshtml` → `Views/CDP/CoachingProton.cshtml`
- Rename partial `Views/CDP/Shared/_ProtonProgressPartial.cshtml` → `Views/CDP/Shared/_CoachingProtonPartial.cshtml`
- Update all display text: page titles, nav entries, hub cards, breadcrumbs
- Update Excel worksheet name and PDF headers from "Proton Progress" to "Coaching Proton"
- Update all `Url.Action("ProtonProgress", ...)` and `RedirectToAction("ProtonProgress")` calls

### Orphan removal (CLN-02, CLN-03, CLN-04)
- Simply delete the orphaned actions and views — no redirects needed
- CMP/CpdpProgress: delete action, view, and ViewModel (`CpdpProgressViewModel.cs`)
- CMP/CreateTrainingRecord: delete action and view (`CreateTrainingRecord.cshtml`). `Admin/AddTraining` is the canonical page. Keep `CreateTrainingRecordViewModel.cs` only if Admin/AddTraining uses it, otherwise delete.
- CMP/ManageQuestions: delete the CMP action. `Admin/ManageQuestions` is canonical.
- Visiting deleted URLs will return 404 — this is acceptable

### AuditLog card placement (CLN-05)
- Claude's Discretion: place the AuditLog card in the most logical section of the Kelola Data hub (`Admin/Index.cshtml`)
- Role-gated: visible to Admin and HC only, not Worker

### Override Silabus & Coaching Guidance (CLN-06)
- Decision: KEEP as-is — the ProtonData/Index page with Silabus and Coaching Guidance tabs is finalized and functional
- Document this decision in PROJECT.md: "Override Silabus & Coaching Guidance tabs retained — fully functional, no changes needed"

### Claude's Discretion
- Exact placement/section of AuditLog card in Admin/Index hub
- Icon and description text for the AuditLog card
- Whether `CreateTrainingRecordViewModel.cs` is shared with Admin/AddTraining (investigate before deleting)
- Any additional "Proton Progress" text in model properties or internal comments (rename display-facing ones, leave internal code names if only used programmatically)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — straightforward cleanup with clear targets.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Views/Admin/AuditLog.cshtml`: AuditLog view already exists
- `Services/AuditLogService.cs`: AuditLog service already wired up
- `Models/AuditLog.cs`: AuditLog model already defined

### Established Patterns
- Kelola Data hub (`Admin/Index.cshtml`): card-based layout with sections, role-gated visibility via `User.IsInRole()`
- CDP hub (`Views/CDP/Index.cshtml`): card links to controller actions

### Integration Points
- `CDPController.cs`: ProtonProgress action (line ~1011), BuildProtonProgressSubModelAsync helper, Excel/PDF export methods
- `Views/CDP/Index.cshtml`: hub card linking to ProtonProgress
- `CMPController.cs`: CpdpProgress (line ~2409), CreateTrainingRecord (line ~348), ManageQuestions (line ~1414)
- `Views/CDP/ProtonProgress.cshtml` + `Views/CDP/Shared/_ProtonProgressPartial.cshtml`
- `Models/CDPDashboardViewModel.cs`: ProtonProgressData property

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 82-cleanup-rename*
*Context gathered: 2026-03-02*
