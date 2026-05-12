---
phase: 318
plan: 02
status: completed
commit: 8c490655
date: 2026-05-12
requirements-completed: [QA-08]
---

# Plan 318-02 Summary — SURF-317-A Production Fix (CMPController ToLookup MA-aware)

## Outcome

**Production fix landed + Phase 317 regression 28/28 HIJAU + zero new warnings.** SURF-317-A eliminated — Results page MA tidak lagi throws ArgumentException (HTTP 500). Razor view Path A no-change. K5/M5 DB-based workaround deferred ke Plan 05 (defense-in-depth retained).

## Files Modified

| File | LOC delta | Purpose |
|------|-----------|---------|
| `Controllers/CMPController.cs` | ~140 LOC (lines 2190 + 2209-2293 refactor) | ToDictionary→ToLookup + MA-aware HashSet aggregation di 3 consumer sites |
| `docs/test-reports/2026-05-12-surf-317-a-fix.md` | NEW | Fix report — root cause, refactor diff, regression stats, production promotion notice |

`Views/CMP/Results.cshtml`: **NOT modified** — Path A (loop already HashSet-aware via independent `option.IsSelected` + `option.IsCorrect` checks).

## Refactor Diff Summary

### Line 2190 primary fix
```diff
-var responseDict = packageResponses.ToDictionary(r => r.PackageQuestionId);
+var responseLookup = packageResponses.ToLookup(r => r.PackageQuestionId);
```

### 3 consumer sites refactored
1. **AllowAnswerReview=true loop** (lines 2209-2234) — `selectedOptionIds.HashSet` + MA `SetEquals` / MC `FirstOrDefault` + `string.Join` untuk UserAnswer/CorrectAnswer multi-text + `IsSelected = selectedOptionIds.Contains(o.Id)`
2. **AllowAnswerReview=false branch** (lines 2240-2260) — same MA-aware count logic
3. **ElemenTeknis aggregation** (lines 2267-2293) — same pattern inside `g.Count(q => ...)` lambda

## Razor View Audit (Task 2) Decision

**Path A — no change.** Razor loop (`Views/CMP/Results.cshtml:355-386`) iterate `question.Options` dengan independent boolean `option.IsSelected` + `option.IsCorrect` checks. Multiple options dengan `IsSelected=true` correctly render success/danger badge untuk MA. Markers preserved:
- `<h5 class="mb-0"><i class="bi bi-list-check me-2"></i>Tinjauan Jawaban</h5>` (line 320)
- `<div class="alert alert-info">...Tinjauan jawaban tidak tersedia...</div>` (lines 396-398)

Used by Phase 317 FLOW N4 (negative assertion) + Phase 318 FLOW S (AllowAnswerReview paired comparison, upcoming Plan 04).

## Phase 317 Regression Rerun Stats

- Command: `cd tests && npx playwright test exam-types.spec.ts --reporter=list`
- Result: **28/28 PASS** (1.7m post-fix, vs 1.9-2.0m pre-fix baseline)
- Setup + W0.1/W0.2 + FLOW K (5) + L (6) + M (5) + N (4) + O (5) HIJAU
- Teardown clean (matrix RESTORE OK, 0 rows post-restore, SEED_JOURNAL cleaned)

| FLOW | Pre-fix | Post-fix | Notes |
|------|---------|----------|-------|
| K (MA full cycle) | ✓ K5 DB-based workaround | ✓ K5 DB-based preserved | Workaround now optional (production fix eliminates SURF-317-A) |
| L (Essay) | ✓ | ✓ | No change |
| M (Mixed MC+MA+Essay) | ✓ M5 DB-based workaround | ✓ M5 DB-based preserved | Same workaround disposition as K5 |
| N (MC AllowAnswerReview=false) | ✓ N4 .alert-info | ✓ N4 .alert-info preserved | Razor marker intact |
| O (MC ExtraTime SignalR) | ✓ O5 .badge text-bg-success | ✓ O5 .badge preserved | Razor marker intact |

**Performance:** runtime 1.7m (vs 1.9m baseline) — ∆ -0.2m. Refactor zero perf regression; slightly faster karena HashSet build O(N) tetap cepat dan Results page MA tidak lagi 500 redirect.

## Build + Type Gate

- `dotnet build`: **exit 0** (60 warnings pre-existing CDPController + unrelated CMPController lines — zero new)
- `cd tests && npx tsc --noEmit`: **exit 0**

## Grep Acceptance Checks

| Pattern | Expected | Actual |
|---------|----------|--------|
| `responseLookup` di CMPController.cs | ≥3 | 4 |
| `SetEquals` di CMPController.cs | ≥2 | 4 |
| `selectedOptionIds.Contains` di CMPController.cs | ≥1 | 2 |
| `\.ToDictionary\(r => r\.PackageQuestionId\)` di CMPController.cs | 0 | 0 |
| `responseDict` di CMPController.cs | 0 | 0 |

## K5/M5 UI Assertion Upgrade Status

**DEFERRED to Plan 05 final regression gate.**

Rationale: Plan 02 acceptance #5 met (regression 28/28 HIJAU post-fix). DB-based workaround tetap correct & valid sebagai defense-in-depth. Wave 2 Plans 03-04 prioritized (FLOW P/Q/R/S = 21+ new sub-tests). Plan 05 final gate atau Phase 320 wholesale refresh dapat tackle UI assertion upgrade tanpa blocking Wave 2 forward progress.

## Deviation

None. All Plan 02 acceptance criteria #1-7 met. Acceptance #8 (optional K5/M5 UI upgrade) explicitly deferred per plan instruction "skip upgrade kecuali eksekutor confident punya time".

## Production Promotion Notice

Plan 02 = production code change (`Controllers/CMPController.cs`). Per CLAUDE.md DEV_WORKFLOW:
- Commit hash: **`8c490655`**
- Flag: **"no migration"** (zero schema change, zero seed data change)
- Notify Team IT: rollout ke Dev (10.55.3.3) → setelah verify Dev → rollout ke Production

## Next

Wave 1 selesai (Plan 01 + Plan 02 commits landed). Wave 2 sequential mulai:
- **Plan 318-03** — FLOW P PreTest/PostTest paired full cycle (P1-P6) + FLOW Q ExamWindowCloseDate reject (Q1-Q4) — append `tests/e2e/exam-types.spec.ts` + new helper `createPrePostAssessmentViaWizard` + `yesterday()` utils
