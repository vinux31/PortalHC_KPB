---
phase: 396-import-excel-retire-bulkbackfill
plan: 05
subsystem: controller
tags: [retire, bulkbackfill, cleanup, inj-11, route-404, e2e, no-migration]

# Dependency graph
requires:
  - phase: 396-import-excel-retire-bulkbackfill
    plan: 04
    provides: "Excel inject path (Step-5 toggle + download/upload/preview/commit) is now the single end-to-end batch-inject entry-point — BulkBackfill can be removed without leaving HC tool-less"
provides:
  - "Controllers/TrainingAdminController.cs — legacy GET BulkBackfill + POST BulkBackfillAssessment actions hard-removed (2 non-contiguous blocks); CleanupAttemptHistory (between them), ManualDuplicatePredicate, and `using ClosedXML.Excel` all KEPT"
  - "Views/Admin/BulkBackfill.cshtml — deleted"
  - "Views/Admin/Index.cshtml — Section D 'Bulk Import Nilai (Excel)' card removed"
  - "Views/Admin/Shared/_AssessmentGroupsTab.cshtml — BulkBackfill dropdown-item + its now-orphan divider removed"
  - "tests/e2e/inject-excel-396.spec.ts — Scenario 6 (INJ-11): /Admin/BulkBackfill + /Admin/BulkBackfillAssessment route-404 + cards/links-gone assertions"
affects:
  - "398 (Test + UAT 'seakan online' — no second inject entry-point remains; regression suite must stay green)"

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Action-routed controller ([Route(\"Admin/[action]\")]): deleting an action removes its endpoint, so the old URL returns 404 BEFORE authorization (contrast: an existing protected action returns 302→login when unauthenticated)"
    - "Surgical removal of 2 NON-contiguous action blocks while preserving a sibling action (CleanupAttemptHistory) physically located between them"

key-files:
  created: []
  modified:
    - Controllers/TrainingAdminController.cs
    - Views/Admin/Index.cshtml
    - Views/Admin/Shared/_AssessmentGroupsTab.cshtml
    - tests/e2e/inject-excel-396.spec.ts
  deleted:
    - Views/Admin/BulkBackfill.cshtml

key-decisions:
  - "Hard-remove (not redirect/stub) — INJ-11 calls for no second entry-point; the Excel inject path (396-04) is the replacement. 404 is the correct, honest signal for the dead URL."
  - "KEEP CleanupAttemptHistory (sits between the two removed blocks), ManualDuplicatePredicate, and `using ClosedXML.Excel` (still used by the new InjectExcelHelper path) — verified by DuplicateGuardTests staying green and build 0-error."
  - "0 migration confirmed by inspection: the change touches only controller actions + Razor views + e2e — no entity/DbContext/Migrations/Models-schema files — so there is definitionally no model diff (the throwaway `dotnet ef migrations add _verify` skipped due to the known EF-CLI-v10-vs-EF8 env blocker; deletion-only change makes a diff impossible)."

patterns-established:
  - "Route-retirement verification = unauthenticated curl/e2e: dead action → 404, live protected action → 302; proves the endpoint is gone, not merely access-blocked"

requirements-completed:
  - INJ-11

# Metrics
duration: ~refactor + orchestrator-driven finalization (prior executor died mid-finalize on a connection error after the refactor commit; orchestrator completed verification + e2e + SUMMARY + tracking inline)
completed: 2026-06-18
---

# Phase 396 Plan 05: Retire Legacy BulkBackfill (INJ-11) Summary

**Hard-removed the legacy "Bulk Import Nilai (Excel)" / BulkBackfill tool now that the new Excel inject path (396-04) is the single batch-inject entry-point: deleted the GET `BulkBackfill` + POST `BulkBackfillAssessment` actions (two non-contiguous blocks in `TrainingAdminController.cs`, keeping `CleanupAttemptHistory` between them), deleted `Views/Admin/BulkBackfill.cshtml`, and removed both UI entry-points (the Section D card in `Index.cshtml` + the dropdown-item and its orphan divider in `_AssessmentGroupsTab.cshtml`). `CleanupAttemptHistory`, `ManualDuplicatePredicate`, and `using ClosedXML.Excel` all kept. Build 0-error, DuplicateGuardTests 9/9, the old routes return 404 at runtime, and 0 migration.**

## Performance
- **Tasks:** 2 (refactor removal + e2e route-404 assertions)
- **Files:** 3 modified + 1 deleted (refactor); 1 e2e spec modified

## Accomplishments
- **Controller** — `TrainingAdminController.cs` −172 lines: GET `BulkBackfill` and POST `BulkBackfillAssessment` removed. `CleanupAttemptHistory` (which physically sat between the two blocks) preserved intact; `ManualDuplicatePredicate` (×3 refs) and `using ClosedXML.Excel` (×1, used by the new InjectExcelHelper) preserved.
- **View deleted** — `Views/Admin/BulkBackfill.cshtml` (−119 lines).
- **UI entry-points removed** — `Index.cshtml` Section D card "Bulk Import Nilai (Excel)" (−16 lines) + `_AssessmentGroupsTab.cshtml` dropdown-item and its now-orphan divider (−6 lines).
- **No residual references** — `grep "BulkBackfill"` across `Controllers/` + `Views/` returns nothing.
- **e2e Scenario 6** — added to `inject-excel-396.spec.ts`: asserts `/Admin/BulkBackfill` (GET) and `/Admin/BulkBackfillAssessment` (POST) both 404, that the live `/Admin/InjectAssessment` is NOT 404 (302 redirect — proves it's a real retirement not a global break), and that `/Admin` has zero `a[href*="BulkBackfill"]` links and no "Bulk Import Nilai (Excel)" text.

## Task Commits
1. **Task 1: Hard-remove BulkBackfill (controller actions + view + 2 UI entry-points)** — `74f266bf` (refactor) — 4 files, 313 deletions.
2. **Task 2: e2e route-404 + cards-gone assertions** — committed by orchestrator finalization (see below) — `tests/e2e/inject-excel-396.spec.ts`.

**Plan metadata:** docs commit (SUMMARY + ROADMAP).

## Decisions Made
- **Hard-remove, not redirect** — INJ-11 requires eliminating the duplicate entry-point; a 404 is the honest result for the dead URL, and the Excel inject path is the replacement.
- **Preserve `CleanupAttemptHistory`/`ManualDuplicatePredicate`/`ClosedXML` import** — verified still referenced + tested (DuplicateGuardTests 9/9).
- **0 migration by inspection** — deletion-only change to actions/views/e2e; no entity/DbContext/Migrations diff possible. The `dotnet ef migrations add _verify` probe was skipped due to the known EF-CLI-v10-vs-EF8 environment blocker; the deletion-only nature makes a model diff impossible, so the gate holds.

## Deviations from Plan
- **Execution note (not a code deviation):** the Wave-5 executor agent committed the refactor (`74f266bf`) then died on a runtime "Connection closed mid-response" error before finishing verification + the e2e assertions + SUMMARY/tracking. The orchestrator completed those steps inline: verified build/tests/routes, added + ran the e2e Scenario 6, wrote this SUMMARY, and updated ROADMAP. No code logic differs from the plan.

## Verification
- **Build:** `dotnet build HcPortal.csproj` → 0 error (24 pre-existing warnings, out of scope).
- **DuplicateGuardTests:** 9/9 PASSED (CleanupAttemptHistory + ManualDuplicatePredicate intact).
- **Route 404 (runtime, unauthenticated curl):** `GET /Admin/BulkBackfill` → 404; `POST /Admin/BulkBackfillAssessment` → 404; `GET /Admin/InjectAssessment` (live, protected) → 302 (login redirect, not 404).
- **e2e Scenario 6 (Playwright, --workers=1, live main-tree server AD-off):** PASSED (route 404 + cards/links gone); setup/teardown DB snapshot/restore clean (0 findings).
- **No residual refs:** `grep BulkBackfill Controllers/ Views/` → none.
- **0 migration** — no `Migrations/`/`Data/`/entity-schema diff.

## Issues Encountered
- Wave-5 executor terminated by a runtime API connection error after the refactor commit. Recovered via orchestrator spot-check + inline finalization (per execute-phase fallback protocol). No work lost.

## User Setup Required
None.

## Next Phase Readiness
- **Phase 396 complete (5/5 plans).** Excel inject path is the single batch-inject entry-point; BulkBackfill fully retired (INJ-11).
- **0 migration** for the whole phase. Handoff: branch main, notify IT migration=FALSE; ❌ no Dev/Prod edits.
- **INJ-10 + INJ-11** are the phase requirements — INJ-11 closed here; INJ-10 (Excel import) delivered across Plans 01-04, final close at phase verification.

## Self-Check: PASSED
- FOUND: Controllers/TrainingAdminController.cs (BulkBackfill actions removed, CleanupAttemptHistory kept)
- DELETED: Views/Admin/BulkBackfill.cshtml
- FOUND: Views/Admin/Index.cshtml (Section D card removed)
- FOUND: Views/Admin/Shared/_AssessmentGroupsTab.cshtml (dropdown removed)
- FOUND: tests/e2e/inject-excel-396.spec.ts (Scenario 6 added)
- FOUND commit: 74f266bf (refactor 396-05 hard-remove BulkBackfill)

---
*Phase: 396-import-excel-retire-bulkbackfill*
*Completed: 2026-06-18*
