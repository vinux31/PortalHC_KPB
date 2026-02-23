---
phase: 31-hc-reporting-actions
plan: "02"
subsystem: ui, api
tags: [csharp, asp-net-core, razor, efcore, auditlog, bulk-action]

# Dependency graph
requires:
  - phase: 22-exam-lifecycle-actions
    provides: ForceCloseAssessment pattern (Status=Completed, Score=0, AuditLog call after SaveChanges)
  - phase: 24-hc-audit-log
    provides: AuditLogService.LogAsync — saves immediately; actor name as "NIP - FullName"; audit after primary SaveChangesAsync
  - phase: 31-hc-reporting-actions plan 01
    provides: ExportAssessmentResults action and d-flex gap-2 card-header structure in AssessmentMonitoringDetail.cshtml
provides:
  - ForceCloseAll POST action in CMPController — bulk-transitions Open/InProgress sessions to Abandoned
  - Force Close All button in AssessmentMonitoringDetail.cshtml — danger btn with confirm() guard and antiforgery token
affects: [future reporting phases, audit log queries filtering Action=ForceCloseAll]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Bulk status transition: foreach + single SaveChangesAsync + one AuditLog summary entry (not per-row)"
    - "POST form with @Html.AntiForgeryToken() inside form + onsubmit=return confirm() for destructive bulk actions"
    - "Status=Abandoned for administrative bulk closure (no Score/IsPassed/CompletedAt set); contrast with ForceCloseAssessment which sets Status=Completed with Score=0"

key-files:
  created: []
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/AssessmentMonitoringDetail.cshtml

key-decisions:
  - "ForceCloseAll sets Status=Abandoned (not Completed): bulk administrative closure does not record a score; contrast with per-session ForceCloseAssessment which forces a Completed state with Score=0"
  - "One AuditLog entry per bulk action (not per session): summary entry includes count to keep audit table concise"
  - "Group key uses .Date property on both sides: a.Schedule.Date == scheduleDate.Date — time-of-day insensitive, matches GetMonitorData and AssessmentMonitoringDetail patterns exactly"

patterns-established:
  - "Bulk destructive action pattern: POST form + @Html.AntiForgeryToken() + onsubmit confirm() + btn-danger"
  - "Single audit entry for bulk operations with session count in description"

# Metrics
duration: 2min
completed: 2026-02-23
---

# Phase 31 Plan 02: Force Close All Summary

**ForceCloseAll POST action bulk-transitions all Open/InProgress sessions to Abandoned with one AuditLog summary entry and a confirm()-guarded danger button in the monitoring detail card header**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-23T01:32:55Z
- **Completed:** 2026-02-23T01:34:53Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- ForceCloseAll POST action added to CMPController after ExportAssessmentResults — filters Open/InProgress sessions by Title+Category+Schedule.Date group key, bulk-transitions all to Abandoned, writes one AuditLog summary entry with session count
- Force Close All button added to AssessmentMonitoringDetail.cshtml card header inside the existing d-flex gap-2 container — positioned between Export Results and Reshuffle All, with @Html.AntiForgeryToken() inside the form and onsubmit confirm() guard
- TempData["Error"] path handles the no-eligible-sessions case cleanly before any DB mutation

## Task Commits

Each task was committed atomically:

1. **Task 1: Add ForceCloseAll action to CMPController.cs** - `e0f0271` (feat)
2. **Task 2: Add Force Close All button to AssessmentMonitoringDetail.cshtml** - `0850fb8` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified

- `Controllers/CMPController.cs` — ForceCloseAll POST action inserted at lines 719-766 (after ExportAssessmentResults, before ReshufflePackage). Sets Status=Abandoned + UpdatedAt only; no Score/IsPassed/CompletedAt mutation.
- `Views/CMP/AssessmentMonitoringDetail.cshtml` — Force Close All form inserted at lines 117-127, inside existing d-flex gap-2 card header container. AntiForgeryToken inside form. confirm() guard with Indonesian text.

## Decisions Made

- **Status=Abandoned, not Completed:** ForceCloseAll is an administrative abandonment (session period ended, workers cannot complete). Per-session ForceCloseAssessment sets Status=Completed with Score=0 — semantically a forced completion. ForceCloseAll does not set Score, IsPassed, or CompletedAt.
- **One audit entry per bulk action:** Writing one summary AuditLog row with the session count keeps the audit table readable for bulk operations. Individual per-session rows would add noise without adding value for this administrative action.
- **Group key `.Date` comparison:** `a.Schedule.Date == scheduleDate.Date` — same pattern as GetMonitorData and AssessmentMonitoringDetail, ensures time-of-day differences in the scheduleDate route value do not break session matching.

## Deviations from Plan

None — plan executed exactly as written. Plan 01 had already been executed (ExportAssessmentResults action and d-flex gap-2 card-header structure were both present), so the "Plan 01 not yet executed" fallback path was not needed. The "Plan 01 already executed" path was followed: only the Force Close All form block was inserted between existing Export Results form and Reshuffle All button.

## Issues Encountered

None.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- Phase 31 complete: both plans (ExportAssessmentResults + ForceCloseAll) are implemented and compiled cleanly
- Assessment monitoring page now has full HC reporting and administrative action toolkit: per-user Force Close, Export Results, Force Close All, and Reshuffle All
- v1.8 milestone complete; ready to plan next milestone

---
*Phase: 31-hc-reporting-actions*
*Completed: 2026-02-23*

## Self-Check: PASSED

- Controllers/CMPController.cs — FOUND
- Views/CMP/AssessmentMonitoringDetail.cshtml — FOUND
- .planning/phases/31-hc-reporting-actions/31-02-SUMMARY.md — FOUND
- Commit e0f0271 (Task 1) — FOUND
- Commit 0850fb8 (Task 2) — FOUND
