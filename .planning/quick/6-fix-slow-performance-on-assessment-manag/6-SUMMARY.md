---
phase: quick-6
plan: "01"
subsystem: assessment-manage
tags: [performance, lazy-load, ajax, projection, cmp]
dependency_graph:
  requires: []
  provides:
    - GET /CMP/GetMonitorData (JSON endpoint for monitoring tab)
  affects:
    - Controllers/CMPController.cs (Assessment action, new GetMonitorData action)
    - Views/CMP/Assessment.cshtml (monitor tab pane replaced with AJAX loader)
tech_stack:
  added: []
  patterns:
    - EF Core Select() projection instead of Include() for management query
    - Bootstrap tab shown.bs.tab event for lazy AJAX load
    - fetch() API with once-guard (loaded flag)
key_files:
  modified:
    - Controllers/CMPController.cs
    - Views/CMP/Assessment.cshtml
decisions:
  - Select() projection on management query — project only 12 needed fields; no full User entity loaded
  - Monitor query moved to GET /CMP/GetMonitorData — called once on first tab click via fetch()
  - monitorBadge shows "..." until tab clicked, then updates to actual group count from JSON response
  - Role guard in GetMonitorData uses HttpContext.Session UserRole (consistent with rest of CMP auth pattern)
metrics:
  duration: ~4 min
  completed: "2026-02-20"
  tasks_completed: 2
  files_modified: 2
---

# Quick Task 6: Fix Slow Performance on Assessment Manage Page — Summary

**One-liner:** Eliminated second DB full scan on manage page load by replacing Include() with Select() projection on management query and moving monitor query to a lazy-load AJAX endpoint called once on first tab click.

## What Was Done

### Task 1 — Replace management Include() with Select() projection in CMPController

**Before:** Assessment manage action loaded all AssessmentSession rows with `.Include(a => a.User)`, pulling full User entities into memory, then ran a second `.Include(a => a.User)` query for monitoring data on every page load regardless of whether the Monitoring tab was ever opened.

**After:**
- Management query uses `.Select(a => new { ... })` projecting only the 12 fields needed (Id, Title, Category, Schedule, DurationMinutes, Status, IsTokenRequired, AccessToken, PassPercentage, AllowAnswerReview, CreatedAt, UserFullName, UserEmail). No full User entity loaded.
- Monitor query block completely removed from Assessment action.
- New `GetMonitorData()` action added — returns `Json(monitorGroups)` with the same MonitoringGroupViewModel shape. HC/Admin role guard via session check. Projected Select() on the filtered monitor subset too.

**Commit:** 7cfedc2

### Task 2 — Wire monitoring tab to lazy-load AJAX in Assessment.cshtml

**Before:** Monitor tab badge showed `@monitorGroups.Count` server-rendered; monitor table was a full Razor foreach block always rendered.

**After:**
- `monitorGroups` ViewBag read line removed (ViewBag.MonitorData no longer populated).
- Monitor tab badge changed to `<span id="monitorBadge">...</span>` — static placeholder.
- Monitoring tab pane replaced with spinner + empty `<div id="monitorContent">`.
- IIFE JS block added: listens for `shown.bs.tab` on `#monitor-tab`, fires `fetch('/CMP/GetMonitorData')` once (loaded-flag guard prevents duplicate requests).
- On success: updates badge count, hides spinner, renders table with same columns (Assessment, Schedule, Status, Completion progress bar, Pass Rate, View Details link).
- Detail links use correct format: `/CMP/AssessmentMonitoringDetail?title=...&category=...&scheduleDate=...`

**Commit:** 564432f

## Success Criteria Verification

| Criterion | Status |
|-----------|--------|
| Zero compile errors (dotnet build — no `error CS`) | PASS |
| No `.Include(a => a.User)` in manage branch of Assessment action | PASS |
| GetMonitorData action exists and returns Json(monitorGroups) | PASS |
| Monitor tab content rendered client-side via fetch, not server-rendered Razor | PASS |

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check

- [x] `Controllers/CMPController.cs` — modified
- [x] `Views/CMP/Assessment.cshtml` — modified
- [x] Commit 7cfedc2 exists (Task 1)
- [x] Commit 564432f exists (Task 2)
- [x] No `error CS` in dotnet build output
- [x] `monitorGroups` removed from Assessment.cshtml
- [x] `GetMonitorData` action present in CMPController.cs

## Self-Check: PASSED
