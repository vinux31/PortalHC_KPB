---
phase: 50-coach-coachee-mapping-manager
plan: "02"
subsystem: Admin Portal - Coach-Coachee Mapping
tags: [admin, coach-coachee, mapping, write-operations, audit-log, excel-export, ajax, proton-track]
dependency_graph:
  requires: [50-01]
  provides: [CoachCoacheeMappingAssign-POST, CoachCoacheeMappingEdit-POST, CoachCoacheeMappingDeactivate-POST, CoachCoacheeMappingReactivate-POST, CoachCoacheeMappingExport-GET, modal-AJAX-wiring]
  affects: [AdminController, Views/Admin/CoachCoacheeMapping.cshtml]
tech_stack:
  added: []
  patterns: [bulk-assign-with-sideeffect, soft-delete, two-step-deactivate-modal, form-urlencoded-simple-params, json-complex-objects, ClosedXML-export]
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/CoachCoacheeMapping.cshtml
decisions:
  - CoachAssignRequest and CoachEditRequest DTOs placed at namespace level (outside class, inside namespace) — clean separation from controller
  - Deactivate does NOT touch ProtonTrackAssignment — track stays when coaching relationship ends (per plan spec)
  - Simple int id params (Deactivate/Reactivate/GetSessionCount) use form-urlencoded; complex objects use [FromBody] JSON — consistent with project AJAX pattern
  - Export uses MemoryStream with ToArray() — stream disposed after SaveAs, bytes already in memory
  - deactivateTargetId stored as var-scoped variable in JS — avoids hidden input race conditions when modal fires multiple times
metrics:
  duration: "~2 minutes"
  completed_date: "2026-02-27"
  tasks_completed: 2
  files_modified: 2
---

# Phase 50 Plan 02: Coach-Coachee Mapping Write Operations Summary

**One-liner:** Full CRUD write operations for Coach-Coachee Mapping — bulk assign with ProtonTrack side-effect, edit, soft-delete deactivation with session count, reactivation, Excel export, and AJAX-wired modals with AuditLog on every state change.

## What Was Built

### Task 1: Write endpoints — Assign, Edit, Deactivate, Reactivate, GetSessionCount, Export
**Commit:** `7c0b86a`

Added 6 new actions to AdminController below the existing CoachCoacheeMapping GET action:

- **CoachCoacheeMappingAssign POST**: Accepts `[FromBody] CoachAssignRequest` (CoachId, CoacheeIds list, optional ProtonTrackId, optional StartDate). Validates non-null, non-empty, no self-assign, no duplicate active mappings. Creates bulk CoachCoacheeMapping rows. If ProtonTrackId provided: deactivates existing assignments for all coachees, adds new ProtonTrackAssignment rows. Single SaveChangesAsync. AuditLog "Assign".
- **CoachCoacheeMappingEdit POST**: Accepts `[FromBody] CoachEditRequest` (MappingId, CoachId, optional ProtonTrackId, optional StartDate). Validates mapping exists, no self-assign, no duplicate on coach change. Updates CoachId and StartDate. ProtonTrack side-effect same as assign. AuditLog "Edit".
- **CoachCoacheeMappingGetSessionCount POST**: Accepts `int id`. Returns Draft coaching session count for the mapping's coach-coachee pair. Used by deactivate modal to show admin session info before confirmation.
- **CoachCoacheeMappingDeactivate POST**: Accepts `int id`. Guards: not found, already inactive. Sets IsActive=false, EndDate=DateTime.Today. ProtonTrackAssignment unchanged. AuditLog "Deactivate".
- **CoachCoacheeMappingReactivate POST**: Accepts `int id`. Guards: not found, already active, duplicate active mapping for same coachee. Sets IsActive=true, EndDate=null. AuditLog "Reactivate".
- **CoachCoacheeMappingExport GET**: Loads all mappings ordered by CoachId/StartDate. Builds user dictionary and active ProtonTrack dictionary keyed by CoacheeId. Creates ClosedXML workbook "Coach-Coachee Mapping" with bold/dark header row (10 columns: Coach Name, Coach Section, Coachee Name, Coachee NIP, Coachee Section, Coachee Position, Current Track, Status, Start Date, End Date). AdjustToContents(). Returns XLSX file.
- **DTOs**: `CoachAssignRequest` and `CoachEditRequest` added at namespace level (inside `HcPortal.Controllers` namespace, outside the class).

### Task 2: Wire modal JS for Assign, Edit, Deactivate, Reactivate AJAX calls
**Commit:** `7e654e9`

Replaced Plan 01 stub functions with full AJAX implementations in `Views/Admin/CoachCoacheeMapping.cshtml @section Scripts`:

- **submitAssign()**: Reads `assignCoachSelect`, checked `.coachee-checkbox` values, `assignProtonTrack`, `assignStartDate`. Validates coach and coachee selection. POSTs JSON to `/Admin/CoachCoacheeMappingAssign`. `location.reload()` on success, `alert(data.message)` on error.
- **submitEdit()**: Reads `editMappingId`, `editCoachSelect`, `editProtonTrack`, `editStartDate`. Validates coach/date. POSTs JSON to `/Admin/CoachCoacheeMappingEdit`. Reload on success.
- **confirmDeactivate(id, coacheeName)**: Sets `deactivateTargetId`, populates coachee name label, shows "Memuat..." then fetches session count via form-urlencoded POST to `/Admin/CoachCoacheeMappingGetSessionCount`. Displays session info string in modal. Opens deactivate confirmation modal via `bootstrap.Modal`.
- **submitDeactivate()**: Uses `deactivateTargetId`. POSTs form-urlencoded `id=X` to `/Admin/CoachCoacheeMappingDeactivate`. Reload on success.
- **reactivateMapping(id)**: `confirm()` dialog. POSTs form-urlencoded `id=X` to `/Admin/CoachCoacheeMappingReactivate`. Reload on success.
- Export button already had `/Admin/CoachCoacheeMappingExport` href — direct GET download, no JS needed.

## Deviations from Plan

None — plan executed exactly as written.

Note: View element IDs (`assignCoachSelect`, `editCoachSelect`, `assignProtonTrack`, `editProtonTrack`) were already set from Plan 01 and differ from the plan's spec aliases (`assignCoachId`, `assignTrackId`, etc.). The implementation correctly used the actual IDs present in the HTML.

## Self-Check

**Files verified:**
- `Controllers/AdminController.cs` contains `CoachCoacheeMappingAssign` — FOUND
- `Controllers/AdminController.cs` contains `CoachCoacheeMappingExport` — FOUND
- `Controllers/AdminController.cs` contains `CoachAssignRequest` — FOUND
- `Views/Admin/CoachCoacheeMapping.cshtml` contains `fetch('/Admin/CoachCoacheeMappingAssign'` — FOUND

**Commits verified:**
- `7c0b86a` feat(50-02): add CoachCoacheeMapping write endpoints to AdminController — FOUND
- `7e654e9` feat(50-02): wire modal AJAX handlers in CoachCoacheeMapping.cshtml — FOUND

**Build:** 0 errors, 32 pre-existing warnings (all in CDPController/CMPController, unrelated to this plan)

## Self-Check: PASSED
