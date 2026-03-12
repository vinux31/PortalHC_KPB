---
phase: 160-assignment-removal
plan: "01"
subsystem: coaching-proton
tags: [mapping, deletion, audit, cascade]
dependency_graph:
  requires: []
  provides: [permanent-mapping-deletion, cascade-delete-assignments-progress]
  affects: [CoachCoacheeMappings, ProtonTrackAssignments, ProtonDeliverableProgresses, AuditLog]
tech_stack:
  added: []
  patterns: [fetch-modal-pattern, cascade-delete, audit-log]
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/CoachCoacheeMapping.cshtml
decisions:
  - Cascade delete ALL ProtonTrackAssignments for coachee (not just those correlated by timestamp) since a deactivated mapping means the coachee has no other active mapping context
  - Row removal via data-mapping-id attribute on tr; inline toast alert instead of page reload
metrics:
  duration: 8m
  completed_date: "2026-03-12"
  tasks_completed: 2
  files_modified: 2
---

# Phase 160 Plan 01: Assignment Removal Summary

Permanent deletion of deactivated coach-coachee mappings with cascade removal of ProtonTrackAssignments and ProtonDeliverableProgress, confirmation modal with counts, and audit logging.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add controller actions for delete preview and delete | 54dc170 | Controllers/AdminController.cs |
| 2 | Add Hapus button and delete confirmation modal to view | bd8526b | Views/Admin/CoachCoacheeMapping.cshtml |

## What Was Built

**CoachCoacheeMappingDeletePreview (GET):** Returns JSON with coachName, coacheeName, assignmentCount, progressCount for a deactivated mapping. Guards against active mappings (returns 400).

**CoachCoacheeMappingDelete (POST):** Cascade-deletes ProtonDeliverableProgresses, ProtonTrackAssignments, and the CoachCoacheeMapping in a single SaveChangesAsync call. Audit-logged with actionType "DeleteMapping".

**View changes:** Hapus button in the `else` (deactivated) block of the action column. `data-mapping-id` on each `<tr>` for DOM removal. Delete modal fetches preview counts, shows coach/coachee names and record counts, includes irreversibility warning. On confirmation, row removed from DOM and success toast shown.

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check

- [x] Controllers/AdminController.cs modified with two new actions
- [x] Views/Admin/CoachCoacheeMapping.cshtml updated with button, modal, JS
- [x] Build: 0 errors
- [x] Commits 54dc170 and bd8526b exist

## Self-Check: PASSED
