---
phase: 49-assessment-management-migration
plan: "05"
subsystem: Assessment Management
tags: [bug-fix, composite-key, modal, security, navigation]
dependency_graph:
  requires: []
  provides: [composite-key-group-navigation, safe-success-modal, user-history-links, token-guard]
  affects: [AdminController, CreateAssessment, ManageAssessment, AssessmentMonitoringDetail]
tech_stack:
  added: []
  patterns: [composite-key-routing, json-island-pattern, conditional-rendering]
key_files:
  created: []
  modified:
    - Controllers/AdminController.cs
    - Views/Admin/CreateAssessment.cshtml
    - Views/Admin/ManageAssessment.cshtml
    - Views/Admin/AssessmentMonitoringDetail.cshtml
decisions:
  - "JSON island pattern (script type=application/json) used for success modal data to avoid single-quote/JSON conflict in JS string literals"
  - "scheduleDate passed as yyyy-MM-dd string in RedirectToAction for consistent DateTime model binding"
  - "sessions.First().Id used as RepresentativeId in MonitoringGroupViewModel (view still uses it for ResetAssessment/ForceCloseAssessment session-level operations)"
  - "CloseEarly and ForceCloseAll audit log messages updated to use composite key params directly (no rep variable needed)"
metrics:
  duration_minutes: 3
  completed_date: "2026-02-27"
  tasks_completed: 2
  files_modified: 4
---

# Phase 49 Plan 05: UAT Gap Closure — Composite Key Migration Summary

**One-liner:** Migrated 4 group-level assessment actions from fragile RepresentativeId to resilient composite key (title + category + scheduleDate), fixed JS success modal XSS/quote safety, added UserAssessmentHistory links per participant, and gated Regenerate Token button on IsTokenRequired.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Fix CreateAssessment success modal and add UserAssessmentHistory links | 05727b4 | CreateAssessment.cshtml, ManageAssessment.cshtml |
| 2 | Migrate group-level actions from fragile representative ID to composite key | 52015a4 | AdminController.cs, ManageAssessment.cshtml, AssessmentMonitoringDetail.cshtml |

## What Was Built

### Task 1: View Layer Fixes

**CreateAssessment.cshtml — JSON Island Pattern:**
- Added `<script type="application/json" id="createdAssessmentData">@Html.Raw(ViewBag.CreatedAssessment ?? "")</script>` before Scripts section
- Replaced unsafe `var createdData = '@Html.Raw(...)'` with `document.getElementById('createdAssessmentData').textContent.trim()`
- Eliminates SyntaxError when JSON data contains single quotes or other special characters

**ManageAssessment.cshtml — View History Links:**
- Changed each participant `<li>` from plain text to flex row with history icon link
- Added `<a href="@Url.Action("UserAssessmentHistory", "Admin", new { userId = u.UserId })">` per participant

**ManageAssessment.cshtml — Conditional Token Button:**
- Wrapped Regenerate Token `<li>` in `@if ((bool)group.IsTokenRequired)` guard
- Button no longer appears for non-token assessments

### Task 2: Controller and Navigation Fixes

**AdminController.cs — 4 Group-Level Action Signatures Changed:**
- `AssessmentMonitoringDetail(int id)` → `(string title, string category, DateTime scheduleDate)`
- `ExportAssessmentResults(int id)` → `(string title, string category, DateTime scheduleDate)`
- `ForceCloseAll(int id)` → `(string title, string category, DateTime scheduleDate)`
- `CloseEarly(int id)` → `(string title, string category, DateTime scheduleDate)`

**AdminController.cs — All 8 RedirectToAction Calls Updated:**
- ResetAssessment (2): Uses `assessment.Title/Category/Schedule.Date.ToString("yyyy-MM-dd")` from already-loaded session
- ForceCloseAssessment (2): Same — uses session already loaded by `FirstOrDefaultAsync`
- ForceCloseAll (2): Passes `title, category, scheduleDate` directly
- CloseEarly (2): Passes `title, category, scheduleDate` directly

**ManageAssessment.cshtml — Dropdown Links Updated:**
- Monitoring link: `asp-route-id="@group.RepresentativeId"` → `href="@Url.Action(... composite key ...)`
- Export link: Same pattern

**AssessmentMonitoringDetail.cshtml — Form Hidden Fields Updated:**
- ForceCloseAll form: `name="id"` → `name="title"` + `name="category"` + `name="scheduleDate"`
- CloseEarly form: Same replacement

## Verification Results

| Check | Result |
|-------|--------|
| Build: 0 errors | PASS |
| JSON island pattern present | PASS |
| Old `'@Html.Raw` string literal gone | PASS |
| UserAssessmentHistory link in ManageAssessment | PASS |
| IsTokenRequired guard present | PASS |
| AssessmentMonitoringDetail uses composite key signature | PASS |
| No FindAsync(id) in group-level actions | PASS |
| ManageAssessment links use `title = (string)group.Title` | PASS |
| AssessmentMonitoringDetail forms use `name="title"` | PASS |

## Deviations from Plan

None — plan executed exactly as written.

The only auto-fix was handling remaining `rep` variable references in the MonitoringGroupViewModel construction and ExportAssessmentResults filename generation after removing the `FindAsync(id)` lookup — these were implicit in the plan task description ("rest of existing logic remains unchanged") but required updating to use the new parameter names.

## Self-Check: PASSED

- `.planning/phases/49-assessment-management-migration/49-05-SUMMARY.md` — CREATED
- Commit 05727b4 — FOUND (Task 1)
- Commit 52015a4 — FOUND (Task 2)
