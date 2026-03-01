---
phase: 63-data-source-fix
plan: 02
subsystem: Views/CDP
tags: [frontend, proton-progress, ajax, role-based-rendering, rowspan-merging]
dependency_graph:
  requires: [CDPController.ProtonProgress, CDPController.GetCoacheeDeliverables]
  provides: [Views/CDP/ProtonProgress.cshtml]
  affects: [Views/CDP/Index.cshtml]
tech_stack:
  added: []
  patterns: [Razor rowspan grouping, fetch() AJAX, vanilla JS rowspan rebuild, @functions helper block]
key_files:
  created:
    - Views/CDP/ProtonProgress.cshtml
  modified:
    - Views/CDP/Index.cshtml
decisions:
  - RZ1031 fix: replaced ternary selected attribute with if/else block — Razor tag helper does not allow C# expressions in attribute declaration area
  - coacheeInfo div guarded by null check on userLevel != 6 before accessing DOM elements — prevents JS errors when div is absent
  - Separate option rendering in two if/else branches instead of ternary — consistent with plan intent, avoids Razor tag helper restriction
metrics:
  duration_seconds: 119
  completed_date: "2026-02-27"
  tasks_completed: 2
  files_modified: 2
requirements_satisfied: [DATA-01, DATA-02, DATA-03, DATA-04]
---

# Phase 63 Plan 02: ProtonProgress View + CDP Index Update Summary

ProtonProgress.cshtml created with rowspan-merged deliverable table, role-conditional coachee dropdown, AJAX fetch() on coachee change, summary stat cards, and loading/error states; CDP Index card updated to link to ProtonProgress.

## Tasks Completed

| # | Task | Commit | Status |
|---|------|--------|--------|
| 1 | Create ProtonProgress.cshtml view with table, stats, dropdown, and AJAX | 746646a | Done |
| 2 | Update CDP Index card link and navigation label | 5598abd | Done |

## What Was Built

### Task 1 — ProtonProgress.cshtml (746646a)

Created `Views/CDP/ProtonProgress.cshtml` (424 lines):

- **Model:** `@model List<HcPortal.Models.TrackingItem>`

- **Role-conditional coachee section:**
  - Level 6 (Coachee): card with `UserFullName` + track badge — no dropdown
  - Level 5 (Coach): dropdown with coachee list from `ViewBag.Coachees`
  - Level 4/2/1 (SrSpv/HC/Admin): same dropdown structure, controller scopes the list

- **Stat cards (3 in a row):** Progress %, Pending Actions, Pending Approvals — show/hide via `id="statsSection"` with `d-none` toggle

- **Rowspan merging (server-side Razor):**
  - GroupBy Kompetensi → then SubKompetensi → then Items
  - `firstKomp` / `firstSub` boolean flags emit `<td rowspan="N">` only for first row in each group

- **8-column table:** Kompetensi, Sub Kompetensi, Deliverable, Evidence, Approval Sr. Spv, Approval Section Head, Approval HC, Aksi

- **`@functions` block:** `GetApprovalBadge(string)` switch expression renders `<span class="badge ...">` for Approved/Rejected/Pending/Not Started

- **AJAX fetch() on coachee dropdown change:**
  - Shows loading spinner, hides table/stats sections
  - Calls `GetCoacheeDeliverables?coacheeId=...`
  - Handles `{error: "unauthorized"}`, `noTrack`, `noProgress` states
  - Rebuilds table with JS rowspan grouping (`Object.entries` loop)
  - `escapeHtml()` for XSS-safe content injection
  - `getApprovalBadge()` JS function mirrors Razor helper

- **Error/info messages:** `#messageArea` div shows `NoTrackMessage` (warning) / `NoProgressMessage` (info) on initial load; cleared and rebuilt on AJAX response

### Task 2 — CDP Index card update (5598abd)

Updated `Views/CDP/Index.cshtml` Progress & Tracking card:

- Card title: "Progress & Tracking" → "Proton Progress"
- Description: "Track IDP completion progress and approval status" → "Monitor Proton deliverable progress and approval status"
- Button label: "View Progress" → "Proton Progress"
- `Url.Action("Progress", "CDP")` → `Url.Action("ProtonProgress", "CDP")`

## Verification Results

- Build: 0 errors, 31 warnings (all pre-existing, none from new code)
- `@model List<HcPortal.Models.TrackingItem>` at line 1
- Table has 8 columns including Kompetensi, Sub Kompetensi with rowspan merging
- `GetCoacheeDeliverables` referenced in AJAX fetch at line 302
- `ProtonProgress` action linked in Index.cshtml at line 51
- ProtonProgress.cshtml is 424 lines (min_lines: 200 — PASSED)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed RZ1031 Razor tag helper error on `<option selected>`**
- **Found during:** Task 1 (first build attempt)
- **Issue:** `@(c.Id == selectedCoacheeId ? "selected" : "")` in `<option>` attribute causes RZ1031 — Razor tag helper does not allow C# expressions in attribute declaration area
- **Fix:** Replaced ternary with explicit `if (c.Id == selectedCoacheeId) { <option selected> } else { <option> }` blocks for both Coach and SrSpv/HC/Admin dropdowns
- **Files modified:** Views/CDP/ProtonProgress.cshtml
- **Commit:** 746646a (included in Task 1 commit)

## Self-Check: PASSED

- `Views/CDP/ProtonProgress.cshtml`: FOUND (424 lines)
- `Views/CDP/Index.cshtml`: FOUND (updated)
- commit `746646a`: FOUND (Task 1)
- commit `5598abd`: FOUND (Task 2)
- Build: 0 errors
