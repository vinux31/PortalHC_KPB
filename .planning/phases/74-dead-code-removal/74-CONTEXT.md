# Phase 74: Dead Code Removal - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Delete all orphaned views, dead controller actions, and unreferenced static files left over from the CMP-to-Admin migration. 10 items total: 6 orphaned Razor views, 2 dead controller actions, 2 unreferenced static files.

</domain>

<decisions>
## Implementation Decisions

### Deletion Strategy
- Straightforward deletion — remove files and code blocks directly
- Verify each item has no remaining references before deletion (grep codebase)
- Build must pass after all deletions

### Orphaned Views (VIEW-01 through VIEW-06)
- Delete 5 CMP views: CreateAssessment, EditAssessment, UserAssessmentHistory, AuditLog, AssessmentMonitoringDetail
- Delete 1 CDP view: Progress.cshtml
- All migrated to Admin or never rendered — safe to delete

### Dead Controller Actions (ACTN-01, ACTN-02)
- Delete `CMPController.GetMonitorData` action — replaced by Admin/GetMonitoringProgress
- Delete `CDPController.Progress` redirect stub — no inbound links
- Clean up any route references to these actions if found

### Unreferenced Static Files (FILE-01, FILE-02)
- Delete `wwwroot/css/site.css` — no view references it
- Delete `wwwroot/js/site.js` — no view references it

### Claude's Discretion
- Order of deletions (views first, actions, then static files — or all at once)
- Whether to group deletions into one commit or multiple
- Any additional dead code discovered during the process

</decisions>

<specifics>
## Specific Ideas

No specific requirements — standard dead code cleanup.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 74-dead-code-removal*
*Context gathered: 2026-03-01*
