# Phase 73: Critical Fixes - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix two runtime errors: (1) missing AccessDenied view causes exception on any 403, (2) dead CMPController.WorkerDetail action has no view file. Both are crash-level bugs reachable in production.

</domain>

<decisions>
## Implementation Decisions

### AccessDenied Page
- Simple page with portal navbar (use _Layout.cshtml)
- Message: "Akses Ditolak" with explanation text
- Include "Kembali" button to go back
- Match portal styling (Bootstrap 5, consistent with other portal pages)

### CMPController.WorkerDetail Removal
- Delete the dead `WorkerDetail` action (line 515-531) from CMPController
- Update 5 RedirectToAction("WorkerDetail") calls in CMPController (lines 662, 667, 678, 723, 752) to redirect to `Admin/WorkerDetail` instead
- These are in EditTrainingRecord and DeleteTrainingRecord POST handlers
- Redirect format: `RedirectToAction("WorkerDetail", "Admin", new { id = workerId })`
- Verify Admin/WorkerDetail action signature accepts the right parameters

### Claude's Discretion
- Exact wording of AccessDenied page message
- Whether to show which page was denied or keep it generic
- Any additional styling details

</decisions>

<specifics>
## Specific Ideas

No specific requirements — standard error page and dead code removal.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 73-critical-fixes*
*Context gathered: 2026-03-01*
