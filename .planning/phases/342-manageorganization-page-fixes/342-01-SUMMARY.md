---
phase: 342
plan: 01
subsystem: admin-organization
tags: [controller, aspnet-mvc, cascade-preview, validation]
requires: [EditOrganizationUnit cascade, ApplicationDbContext]
provides: [dup-name per-parent, PreviewEditCascade endpoint]
affects: [Controllers/OrganizationController.cs]
tech-stack:
  added: []
  patterns: [per-parent uniqueness, read-only count action, mirror-cascade-predicate]
key-files:
  created: []
  modified:
    - Controllers/OrganizationController.cs
key-decisions: [ORG-TREE-02 per-parent dup, A1 full-accuracy reparent count, D-04 early-return]
requirements-completed: [ORG-TREE-02, ORG-TREE-07]
duration: "~8 min"
completed: 2026-06-03
---

# Phase 342 Plan 01: Backend dup-name per-parent + PreviewEditCascade Summary

Scoped duplicate-name validation from GLOBAL to PER-PARENT in Add/EditOrganizationUnit (ORG-TREE-02) and added read-only `PreviewEditCascade` count action (ORG-TREE-07) mirroring the actual EditOrganizationUnit cascade with A1 full-accuracy.

## Commit

| Hash | Description |
|------|-------------|
| `e7b753d3` | feat(342-01): dup-name per-parent + PreviewEditCascade |

## Tasks Completed

- **Task 1** — 2 dup-check edits in `Controllers/OrganizationController.cs`: AddOrganizationUnit + EditOrganizationUnit now `AnyAsync(... && u.ParentId == parentId [&& u.Id != id])`. Global predicate removed. Error message preserved (Bahasa Indonesia).
- **Task 2** — new `PreviewEditCascade(int id, string name, int? parentId)` action inserted before `IsDescendantAsync`: trio attribute, D-04 early-return, count predicates mirror EditOrganizationUnit cascade exactly, A1 full-accuracy reparent (4 field-pairs), Json 6 props, no `_userManager` call.

## LoC Delta

- `Controllers/OrganizationController.cs`: +61 / -2 (~521 → ~580).

## A1 Decision (for Plan 03 test calibration)

**Full-accuracy reparent count** locked: reparent (`parentChanged && Level>=1 && !nameChanged`) counts ALL 4 field-pairs (Users.Unit / CoachCoacheeMappings.AssignmentUnit / ProtonKompetensiList.Unit / CoachingGuidanceFiles.Unit), matching actual mutation L247-261 — NOT users-only (spec under-report). Plan 03 `preview==actual` test must assert all 4.

## Acceptance Criteria (all PASS)

**Task 1:** build PASS, `u.ParentId == parentId`=4 (≥2; incl pre-existing Where/Max), global predicate=0, `&& u.ParentId == parentId && u.Id != id`=1, error message=4 (≥2).
**Task 2:** build PASS, PreviewEditCascade=1, `[ValidateAntiForgeryToken]`=7 (≥4), early-return=1, `CountAsync(u => u.Section == oldName)`=1, `CountAsync(u => u.Unit == oldName)`=2, `CountAsync(m => m.AssignmentUnit == oldName)`=2 (A1), mapShape=1, guideShape=1.

## Build + Test Evidence

- `dotnet build HcPortal.csproj` → Build succeeded, 0 Error, 0 new warning.
- `dotnet test HcPortal.Tests` → **38/38 Passed, 0 Failed** (regression-safe; no new tests this plan).

## Threat Mitigations Wired

T-342-01 (CSRF ValidateAntiForgeryToken), T-342-02 (RBAC Authorize Admin,HC), T-342-03 (FindAsync null-guard + read-only), T-342-04 (per-parent server dup), T-342-06 (count mirror = no drift). T-342-05 accepted.

## Deviations from Plan

None - plan executed exactly as written (code verbatim from RESEARCH Code Examples + plan action). EditOrganizationUnit/AddOrganizationUnit cascade body unchanged.

## Next Phase Readiness

Ready for Plan 02 (frontend orgTree.js + ManageOrganization.cshtml) — endpoint `/Admin/PreviewEditCascade` live, returns 6-prop JSON. Plan 03 can assert preview==actual (all 4 field-pairs).

## Self-Check: PASSED
