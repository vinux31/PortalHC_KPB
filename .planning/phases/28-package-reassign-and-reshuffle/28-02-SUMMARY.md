---
phase: 28-package-reassign-and-reshuffle
plan: "02"
subsystem: assessment-monitoring
tags: [reshuffle, package-assignment, ui, ajax, toast, modal, view]
dependency_graph:
  requires:
    - 28-01 (IsPackageMode, PendingCount, PackageName on view models; ReshufflePackage + ReshuffleAll controller actions)
  provides:
    - Reshuffle UI in AssessmentMonitoringDetail (per-worker + bulk)
  affects:
    - Views/CMP/AssessmentMonitoringDetail.cshtml
tech_stack:
  added: []
  patterns:
    - Bootstrap 5 Toast for per-worker reshuffle feedback
    - Bootstrap 5 Modal for bulk reshuffle result list
    - Hidden form antiforgery token extraction pattern (reliable over regex)
    - In-place DOM update (pkgCell.textContent) after single-worker reshuffle
    - Page reload on modal close after bulk reshuffle
    - event parameter passed to reshuffleAll(event) for button reference
key_files:
  created: []
  modified:
    - Views/CMP/AssessmentMonitoringDetail.cshtml
decisions:
  - "reshuffleAll(event) receives event from onclick='reshuffleAll(event)' — simpler than document.querySelector"
  - "Hidden form pattern for antiforgery token — more reliable than regex parsing of @Html.AntiForgeryToken() output"
  - "Page reload on modal close for bulk reshuffle — acceptable since many rows change; single-worker stays in-place"
  - "Script block is conditional on @if (Model.IsPackageMode) — no dead JS for question-mode assessments"
metrics:
  duration: 2min
  completed: 2026-02-21
  tasks: 1
  files: 1
---

# Phase 28 Plan 02: Package Reshuffle Frontend Summary

**One-liner:** Bootstrap 5 reshuffle UI on AssessmentMonitoringDetail — per-worker AJAX button with in-place cell update + toast, and bulk Reshuffle All with confirmation dialog and result modal; all controls hidden for question-mode assessments.

## What Was Built

### Task 1: Reshuffle UI Controls in AssessmentMonitoringDetail

**Package column:**
- Added `<th>Package</th>` in thead, wrapped in `@if (Model.IsPackageMode)`
- Added `<td id="pkg-@session.Id">` in each tbody row, wrapped in `@if (Model.IsPackageMode)`
- Updated empty-state row colspan: `@(Model.IsPackageMode ? 8 : 7)`

**Per-worker Reshuffle button:**
- Rendered for all sessions when `Model.IsPackageMode`
- Active (`onclick="reshuffleWorker(sessionId)"`) for `Not started` workers
- Disabled (HTML `disabled` attr + CSS `disabled` class) for InProgress/Completed/Abandoned
- `title` attribute provides Indonesian tooltip explaining why disabled

**Reshuffle All button:**
- Added to card-header via `d-flex justify-content-between align-items-center` layout
- Visible only when `Model.IsPackageMode && Model.PendingCount > 0`
- Shows pending count: `Reshuffle All (@Model.PendingCount pending)`
- `onclick="reshuffleAll(event)"` passes DOM event for button reference

**Antiforgery token:**
- Hidden `<form id="reshuffleForm">@Html.AntiForgeryToken()</form>` at bottom of container
- JS reads: `document.querySelector('#reshuffleForm input[name="__RequestVerificationToken"]').value`

**Toast (per-worker feedback):**
- `#reshuffleToast` — Bootstrap 5 `text-bg-success` toast, top-right fixed position
- `#reshuffleToastBody` — dynamically set to `"Package berhasil di-reshuffle → {packageName}"`

**Result modal (Reshuffle All):**
- `#reshuffleResultModal` — Bootstrap 5 scrollable modal
- `#reshuffleResultBody` — populated with a `<table>` of worker name + status
- On `hidden.bs.modal` (once): `location.reload()` to refresh package cells

**JavaScript:**
- `reshuffleWorker(sessionId)`: POST to `/CMP/ReshufflePackage`, spinner during call, updates `#pkg-{id}` cell in-place on success, shows toast
- `reshuffleAll(event)`: `confirm()` dialog, POST to `/CMP/ReshuffleAll`, result modal, reload on close
- Entire `<script>` block wrapped in `@if (Model.IsPackageMode)` — no dead code for question-mode

## Verification Results

1. `dotnet build` passes — 0 errors, 35 pre-existing warnings (same count as 28-01)
2. Package column added with `IsPackageMode` guard — confirmed in view
3. Per-worker Reshuffle button rendered for all workers in package-mode — confirmed
4. Active button for Not started, disabled + tooltip for InProgress/Completed/Abandoned — confirmed
5. Reshuffle All button in card-header with pending count — confirmed
6. Hidden form antiforgery pattern + JS reads token correctly — confirmed
7. Toast container and result modal present — confirmed
8. `reshuffleAll(event)` receives event from `onclick="reshuffleAll(event)"` — confirmed (per important note)
9. Script block gated on `@if (Model.IsPackageMode)` — confirmed

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Passed `event` to `reshuffleAll` via `onclick` attribute**
- **Found during:** Task 1 (per important_note in plan)
- **Issue:** Plan's `reshuffleAll()` used bare `event` global, which is unreliable in strict mode
- **Fix:** Changed onclick to `onclick="reshuffleAll(event)"` and function signature to `reshuffleAll(event)`
- **Files modified:** Views/CMP/AssessmentMonitoringDetail.cshtml
- **Commit:** 97704c3 (same task commit)

**2. [Rule 2 - Missing critical functionality] Used hidden-form antiforgery pattern**
- **Found during:** Task 1 (per implementation notes in plan)
- **Issue:** Regex-parsing `@Html.AntiForgeryToken()` string is fragile
- **Fix:** Used `<form id="reshuffleForm" style="display:none">@Html.AntiForgeryToken()</form>` and read `.value` from the input — more reliable
- **Files modified:** Views/CMP/AssessmentMonitoringDetail.cshtml
- **Commit:** 97704c3 (same task commit, plan itself recommended this approach)

**3. [Rule 1 - Bug] Handled "else" branch in Actions cell for package-mode with no other action**
- **Found during:** Task 1
- **Issue:** When `IsPackageMode` is true and a worker's status is neither Completed/Abandoned nor InProgress/Not started (edge case), the previous `else` branch showed `—` unconditionally. With Reshuffle button always rendered in package-mode, the `—` fallback became redundant only for non-package mode.
- **Fix:** Wrapped the `—` fallback inside `@if (!Model.IsPackageMode)` to avoid double-rendering in package mode
- **Files modified:** Views/CMP/AssessmentMonitoringDetail.cshtml
- **Commit:** 97704c3 (same task commit)

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | 97704c3 | feat(28-02): add reshuffle UI controls to AssessmentMonitoringDetail |

## Self-Check: PASSED
