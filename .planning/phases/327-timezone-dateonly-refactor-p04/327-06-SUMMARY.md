---
phase: 327-timezone-dateonly-refactor-p04
plan: 06
status: complete (Task 2+3 deferred to Plan 08 UAT bundle)
date: 2026-05-28
commits: [37712c76]
deviations: [task1-done-in-plan05-bonus, task2-3-deferred-plan08]
---

# Plan 327-06 — SUMMARY

## One-Liner
ImportTraining Pattern D cast applied (already done via Plan 05 bonus commit `37712c76`). Razor TagHelper #47628 smoke verify + conditional `[DisplayFormat]` retrofit deferred ke Plan 08 UAT bundle (SC-5 5 halaman wajib browser inspect natural overlap).

## What Was Built

### Task 1: ImportTraining Pattern D cast (✅ done via Plan 05 bonus)
| Line | Change | Commit |
|------|--------|--------|
| TrainingAdminController.cs:1037 | `? vu : (DateTime?)null` → `? DateOnly.FromDateTime(vu) : (DateOnly?)null` | `37712c76` (Plan 05 pulled forward) |
| TrainingAdminController.cs:1138 | Same pattern | `37712c76` (Plan 05 pulled forward) |

**Pull-forward rationale:** Plan 05 acceptance required `dotnet build` 0 Error. Without ImportTraining cast, build had 2 errors (CS0029 DateTime → DateOnly?). Plan 05 included these 2 sites to unblock build acceptance. Plan 06 Task 1 effectively complete pre-execution.

### Task 2 (CHECKPOINT): Razor TagHelper #47628 smoke — DEFERRED
**Decision:** Defer ke Plan 08 UAT bundle (user-approved via AskUserQuestion).
**Rasional:**
1. Plan 08 SC-5 manual UAT cover 5 halaman wajib + form inspect natural overlap dengan Razor TagHelper check
2. .NET 8.0.418 stable + bug #47628 filed 2023 → high probability sudah fixed di patch terbaru
3. Avoid 2-pass browser session (Plan 06 smoke + Plan 08 UAT)
4. Risk acceptable: kalau bug aktif, Plan 08 ketemu di form inspect → emergency retrofit Pattern F (Task 3 spec ready-to-apply)

### Task 3 (CONDITIONAL): [DisplayFormat] retrofit — DEFERRED PENDING TASK 2 OUTCOME
**Status:** Conditional execution pending Plan 08 Task 1 outcome.
**Action:** Kalau Plan 08 SC-5 form inspect flag bug aktif (value="d-M-yyyy" instead "yyyy-MM-dd"), retrofit `[DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]` ke 4 VM property:
- `Models/CreateTrainingRecordViewModel.cs:51`
- `Models/EditTrainingRecordViewModel.cs:53`
- `Models/CreateManualAssessmentViewModel.cs:34, 97`

Code snippet ready-to-paste di Plan 06 Task 3 action block.

## Verification
- Task 1 grep acceptance 3/3 PASS (2 `DateOnly.FromDateTime(vu)` + 0 old pattern + 2 `(DateOnly?)null`)
- `dotnet build HcPortal.sln` → 0 Error (Plan 05 validated)
- `dotnet test HcPortal.sln` → 18/18 GREEN (Plan 05 validated)

## Threats
| ID | Status |
|----|--------|
| T-327-02 (Razor TagHelper bug) | DEFERRED — Plan 08 SC-5 cover. Pattern F retrofit ready-to-apply kalau ketemu. |

## Decisions Applied
- D-13: ImportTraining cast `DateOnly.FromDateTime(vu)` ✓ (via Plan 05 bonus)
- D-14 (Pitfall 5): Smoke-first NO pre-add DisplayFormat ✓ (deferred ke Plan 08)

## Pending Downstream
- **Plan 07:** EF migration generate + sqlcmd pre-check + DB snapshot + apply lokal + JSON API audit
- **Plan 08:** Manual UAT 7 SC + Pitfall 3 JSON timezone smoke + **Razor TagHelper bug verify (Plan 06 Task 2 bundle)** + Phase 326 regression smoke + IT_NOTIFY.md draft

## Commits
- `37712c76` — (Plan 05) ImportTraining 2 site Pattern D cast (bonus pull-forward)
- (Plan 06 zero net commits — Task 1 already done; Task 2+3 deferred to Plan 08)

## Next Plan
Plan 327-07 — EF migration apply (T-327-01 HIGH risk: pre-check sqlcmd zero-jam + DB snapshot SEED_WORKFLOW + apply + post-verify schema). Critical milestone untuk Phase 327 SHIPPED.
