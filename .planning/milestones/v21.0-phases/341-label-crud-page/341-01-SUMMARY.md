---
phase: 341
plan: 01
subsystem: admin-org-label
tags: [controller, viewmodel, admin-crud, aspnet-mvc]
requires: [Phase 340 OrgLabelService, ApplicationDbContext, UserManager]
provides: [OrgLabelController CRUD actions, ManageOrgLevelLabelsViewModel]
affects: [Controllers/OrgLabelController.cs, Models/ViewModels/ManageOrgLevelLabelsViewModel.cs]
tech-stack:
  added: []
  patterns: [JSON success/failure return, ValidateAntiForgeryToken, View() override, inline server validation]
key-files:
  created:
    - Models/ViewModels/ManageOrgLevelLabelsViewModel.cs
  modified:
    - Controllers/OrgLabelController.cs
key-decisions: [D-05 inline validation, D-08 next-level constraint, D-09 extend existing controller, D-10 server-render VM]
requirements-completed: [ORG-LABEL-04, ORG-LABEL-05, ORG-LABEL-06]
duration: "~10 min"
completed: 2026-06-03
---

# Phase 341 Plan 01: Controller + ViewModel Backend Summary

Extended `OrgLabelController` (Phase 340) with 4 CRUD actions (GET render + 3 POST mutation) consuming `OrgLabelService`, plus POCO `ManageOrgLevelLabelsViewModel` for D-10 server-render Razor model binding. Full server-side validation (D-05 + D-08) with Bahasa Indonesia messages.

## Commits

| Task | Hash | Description |
|------|------|-------------|
| 1 | `facd26db` | feat(341-01): add ManageOrgLevelLabelsViewModel POCO |
| 2 | `015b961d` | feat(341-01): extend OrgLabelController with 4 CRUD actions |

## Tasks Completed

- **Task 1** — `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs` (NEW, 20 LoC): `ManageOrgLevelLabelsViewModel` + nested `LabelRowVM` (nullable `Label` = buffer row marker). POCO match `CMPRecordsViewModel`.
- **Task 2** — `Controllers/OrgLabelController.cs` (32 → 211 LoC): DI expansion (+ ApplicationDbContext + UserManager), View() 4-overload override (Pitfall 1), 4 new actions verbatim Code Examples #1-#4. `GetLevelLabels` Phase 340 preserved verbatim.

## LoC Delta

- `Controllers/OrgLabelController.cs`: 32 → 211 (+179 insert / -2 delete)
- `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs`: 20 (new)

## Acceptance Criteria (all PASS)

**Task 1:** ns=1, vm-class=1, row-class=1, nullable-Label=1, build PASS.
**Task 2:** 4 action signatures (Manage/Update/Add/Delete each=1), `[ValidateAntiForgeryToken]`=3, `[Authorize(Roles="Admin, HC")]`=4, View()override=4, D-08 constraint=1, `catch(DbUpdateException)`=1, `label.Trim()`=2, `using ...ViewModels`=1, GetLevelLabels preserved (def=1), build PASS.

## Build + Test Evidence

- `dotnet build HcPortal.csproj` → **Build succeeded**, 0 Error, 0 new warning (all warnings pre-existing in other files).
- `dotnet test HcPortal.Tests` → **31/31 Passed, 0 Failed** (regression-safe; Plan 01 added no tests).

## Threat Mitigations Wired (verifiable via grep)

T-341-01 (CSRF → 3× `[ValidateAntiForgeryToken]`), T-341-02 (RBAC → 4× method `[Authorize(Roles)]`), T-341-03 (D-08 next-level), T-341-04 (D-05 inline checks), T-341-05 (Delete highest-unused guard), T-341-08 (DbUpdateException race catch). T-341-06/07 accepted per plan.

## Deviations from Plan

None - plan executed exactly as written. All code verbatim from RESEARCH Code Examples #1-#5.

Note: Test baseline is 31 (not the 20 cited in plan/STATE — additional tests accrued since Phase 340); all pass, regression-safe.

## Next Phase Readiness

Ready for Plan 02 (Razor view `Views/Admin/ManageOrgLevelLabels.cshtml` + admin card). Action URLs live: `/Admin/ManageOrgLevelLabels`, `/Admin/UpdateLevelLabel`, `/Admin/AddLevelLabel`, `/Admin/DeleteLevelLabel`.

## Self-Check: PASSED
