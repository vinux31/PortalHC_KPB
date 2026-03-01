---
phase: 78-deduplicate-cmp-page-remove-cdp-protonmain-if-identical-to-admin-coachcoacheemapping
plan: "01"
subsystem: CDP, Admin, Models
tags: [cleanup, dead-code, navigation, role-access]
dependency_graph:
  requires: []
  provides: [NAV-01]
  affects: [CDPController, AdminHub, ProtonViewModels]
tech_stack:
  added: []
  patterns: [razor-role-gate, hub-card-pattern]
key_files:
  modified:
    - Controllers/CDPController.cs
    - Models/ProtonViewModels.cs
    - Views/CDP/Index.cshtml
    - Views/Admin/Index.cshtml
  deleted:
    - Views/CDP/ProtonMain.cshtml
key_decisions:
  - "CDPController.Index() simplified from async Task<IActionResult> to sync IActionResult — after removing ViewBag.CanAccessProton, the only await was GetUserAsync(User); removing the await also removes the one reason the method was async"
  - "var user removed from CDPController.Index() entirely — it was only used for the now-deleted CanAccessProton ViewBag assignment; with no other usage in Index(), the variable is dead"
  - "Training Records card uses User.IsInRole guard in Razor — consistent with ManageAssessment card pattern in Section C"
metrics:
  duration: ~8 minutes
  completed: "2026-03-01"
  tasks_completed: 2
  files_modified: 4
  files_deleted: 1
---

# Phase 78 Plan 01: Remove CDP/ProtonMain and Add Training Records Navigation Summary

**One-liner:** Deleted CDPController.ProtonMain + AssignTrack actions, ProtonMainViewModel class, and ProtonMain.cshtml; removed CDP Index Setting Proton section; added Training Records shortcut card to Kelola Data hub for Admin/HC.

## What Was Built

Removed the CDP/ProtonMain page (coach self-service track assignment) which was superseded by Admin/CoachCoacheeMapping in an earlier phase. Admin and HC users now own track assignment exclusively. Added a Training Records navigation card to the Kelola Data hub (Admin/Index Section C) linking to CMP/Records for Admin/HC users (NAV-01).

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Remove ProtonMain/AssignTrack/ProtonMainViewModel/CDP Index block | 866cec3 | Controllers/CDPController.cs, Models/ProtonViewModels.cs, Views/CDP/Index.cshtml, Views/CDP/ProtonMain.cshtml (deleted) |
| 2 | Add Training Records card to Kelola Data hub (NAV-01) | ee705a2 | Views/Admin/Index.cshtml |

## Decisions Made

1. **CDPController.Index() converted from async to sync** — After removing the `ViewBag.CanAccessProton` assignment, the only `await` was `_userManager.GetUserAsync(User)`. With no remaining async work, converting to `IActionResult` avoids a compiler warning about async method lacking awaits and is cleaner overall.

2. **`var user` removed entirely from CDPController.Index()** — The variable was only referenced by the deleted `CanAccessProton` ViewBag line. The plan anticipated this: "If removing this line means `user` is now unused in Index(), also remove the `var user = await _userManager.GetUserAsync(User);` line above it."

3. **Training Records card placed after ManageAssessment card, same row** — Section C's `<div class="row g-3 mb-2">` already holds the ManageAssessment card. The new card is appended inside that same row, maintaining consistent layout.

## Deviations from Plan

None — plan executed exactly as written. The `var user` removal was explicitly anticipated in the plan's Step 1b instructions.

## Verification Results

All 7 checks passed:
1. `dotnet build -c Release` — Build succeeded, 0 errors
2. `grep -rn "ProtonMain|AssignTrack" Controllers/CDPController.cs` — No matches
3. `grep -rn "CanAccessProton" Controllers/CDPController.cs` — No matches
4. `grep -n "ProtonMainViewModel" Models/ProtonViewModels.cs` — No matches
5. `test -f "Views/CDP/ProtonMain.cshtml"` — DELETED
6. `grep -n "ProtonMain" Views/CDP/Index.cshtml` — No matches
7. `grep -n "Records.*CMP|Url.Action.*Records" Views/Admin/Index.cshtml` — Match found at line 142

## Self-Check: PASSED

- Controllers/CDPController.cs — modified and committed (866cec3)
- Models/ProtonViewModels.cs — modified and committed (866cec3)
- Views/CDP/Index.cshtml — modified and committed (866cec3)
- Views/CDP/ProtonMain.cshtml — deleted and committed (866cec3)
- Views/Admin/Index.cshtml — modified and committed (ee705a2)
- Commits verified: 866cec3, ee705a2 both exist in git log
