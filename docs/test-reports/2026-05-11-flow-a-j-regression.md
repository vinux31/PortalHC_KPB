# Phase 317 — Regression Smoke FLOW A-J Baseline

**Date:** 2026-05-11 (baseline anchor date; actual run executed 2026-05-12 during Plan 02 Task 3)
**Spec:** `tests/e2e/exam-taking.spec.ts`
**Command:** `cd tests && npx playwright test exam-taking.spec.ts --reporter=list`
**Phase scope:** Diagnose-only (BUKAN fix — fix regressions ditunda ke Phase 318+ as `SURF-317-{N}` anchors)
**Baseline purpose:** Catat pass rate FLOW A-J SETELAH v15.0 changes (Phase 304-314 — 4-step wizard, schedDateInput, correct_A/B/C/D radios, Razor partial markup) untuk dibandingkan dengan re-run di phase berikutnya.

## Summary

| Metric        | Value                                                  |
|---------------|--------------------------------------------------------|
| Total tests   | 76                                                     |
| Passed        | 0 / 76 (0%)                                            |
| Failed        | 1 / 76 (1.3%) — A1 only; cascade abort triggered       |
| Skipped (cascade) | 74 / 76 (97.4%)                                    |
| Setup passed  | 1 / 1 (global.setup — app + seed matrix OK)            |
| Runtime       | ~10s (aborted early)                                   |
| Exit code     | 1                                                      |

## Cascade Abort — Systemic Root Cause

`tests/e2e/exam-taking.spec.ts:6` declares **file-level serial mode**:

```typescript
test.describe.configure({ mode: 'serial' });
```

Playwright behavior under serial mode: ketika 1 test fail, sisa tests di file ABORT (status `did-not-run`). Dengan A1 fail pada baris awal, **74 tests B1-J8 + Phase 313 block tidak ter-execute** — bukan karena tests itu broken, tapi karena cascade dependency.

**Implication:** Pass rate sebenarnya per-FLOW TIDAK dapat di-extract dari single run. Untuk per-FLOW breakdown akurat, butuh refactor file ke `mode: 'default'` (parallel) atau remove file-level serial config — out of Phase 317 scope (regression diagnose-only).

## Per-FLOW Breakdown (observed)

| FLOW | Tests | Passed | Failed | Did-not-run | Notes                                                  |
|------|-------|--------|--------|-------------|--------------------------------------------------------|
| A    | 15    | 0      | 1 (A1) | 14          | A1 selector legacy → cascade trigger                   |
| B    | 5     | 0      | 0      | 5           | Cascade — not reached                                  |
| C    | 7     | 0      | 0      | 7           | Cascade — not reached                                  |
| D    | 7     | 0      | 0      | 7           | Cascade — not reached                                  |
| E    | 4     | 0      | 0      | 4           | Cascade — not reached                                  |
| F    | 6     | 0      | 0      | 6           | Cascade — not reached                                  |
| G    | 3     | 0      | 0      | 3           | Cascade — not reached                                  |
| H    | 8     | 0      | 0      | 8           | Cascade — not reached                                  |
| I    | 5     | 0      | 0      | 5           | Cascade — not reached                                  |
| J    | 8     | 0      | 0      | 8           | Cascade — not reached                                  |
| 313  | 7     | 0      | 0      | 7           | Cascade — not reached (Phase 313 manual-submit block) |

## Failed Tests Detail

### `A1 - HC creates assessment for coachee` (`SURF-317-A1`)

- **FLOW:** A — Legacy Exam Full Lifecycle (lines 30-310)
- **Failure:**
  ```
  Error: locator.click: Element is not visible
  Call log:
    - waiting for locator('.user-check-item').filter({ hasText: 'rino.prasetyo' }).locator('input')
    - locator resolved to <input name="UserIds" type="checkbox" class="form-check-input user-checkbox" ...>
    - attempting click action
      - scrolling into view if needed
  ```
- **Suspected cause:** Legacy test pakai `.user-check-item input` raw checkbox click. Phase 304+ markup wrap checkbox dalam `<label>` Bootstrap form-check pattern — actual `<input>` element ada di DOM tapi visually hidden behind `<label>` styling. Click `<input>` langsung gagal `not visible`. Pivot: Phase 317 examTypes.ts pakai `.user-check-item[data-email="${email}"] input.user-checkbox` (attribute-based) plus `.check()` (Playwright auto-uses checkbox semantics) — works.
- **Reference:** Phase 317 RESEARCH State-of-the-Art lines 651-666 noted legacy selectors obsolete. Pattern fix sudah ada di `tests/e2e/helpers/examTypes.ts:62-74` (Plan 01).
- **Anchor ID:** `SURF-317-A1` — track at future phase.

## Known Obsolete Selectors (from 317-RESEARCH.md State-of-the-Art)

| Old Approach (FLOW A-J)                                  | Current Markup (Phase 304+)                                  | When Changed             |
|----------------------------------------------------------|--------------------------------------------------------------|--------------------------|
| Single-page `#submitBtn` legacy CreateAssessment         | 4-step wizard `#btnNext1/2/3` + `#btnSubmit`                 | Phase 304 (2026-04-28)   |
| `#ScheduleDate` direct fill                              | `#schedDateInput` + `#schedTimeInput` (hidden combiner)      | Phase 304                |
| `input[name="correct_option_index"][value="N"]`          | `#correct_A/B/C/D` radio (MC) / checkbox (MA)                | Phase 298 (2026-04-07)   |
| `name="question_text"` + `name="options[]"` array fields | `name="questionText"` + `name="optionA"`..`name="optionD"`   | Phase 298                |
| `.user-check-item input` raw checkbox click              | `.user-check-item[data-email] input.user-checkbox` + `.check()` | Phase 307 (panel refactor) |
| `/Admin/ManagePackages/{id}` path-style URL              | `/Admin/ManagePackages?assessmentId={id}` query-string       | Phase 304 (verified Plan 01 W0.1) |
| `/Admin/ManageQuestions` action                          | `/Admin/ManagePackageQuestions?packageId={id}`               | Phase 304                |
| Wizard auto-create package                               | Manual `createDefaultPackage` step required after wizard     | Phase 304 (verified Plan 01 W0.1) |
| Worker positional `.nth(N)` option mapping               | DOM-text match by option label (per-question shuffle CMPController.cs:1188 + StartExam.cshtml:125) | Phase 317 Wave 0 (A4 pivot 2026-05-11) |

## Recommendation

Phase 317 SCOPE = diagnose-only. A1 failure adalah systemic signal:

1. **Tier 1 (cheap fix, Phase 318+):** Patch FLOW A1 user-check-item selector → match Phase 317 examTypes.ts pattern. Test re-run akan reveal per-FLOW pass rate sebenarnya.
2. **Tier 2 (expensive, dedicated Phase 320):** Wholesale rewrite FLOW A-J pakai shared helpers dari `tests/e2e/helpers/examTypes.ts` + `wizardSelectors.ts`. Estimate: 8-12 file changes, 200-400 LOC delta. ROI tinggi (consolidates 76 tests under maintained selector pattern).
3. **Tier 3 (alternative):** Pivot strategy — `tests/e2e/exam-taking.spec.ts` partial deprecate, retain Phase 313 block (block-manual-submit regression) sebagai standalone spec; FLOW A-J replaced via `exam-types.spec.ts` (Phase 317 OOP coverage 5/5) + new `exam-lifecycle.spec.ts` (Phase 320 untuk reset/edit/abandon).

Direkomendasi:
- **Phase 318 (PreTest/PostTest + ExamWindowCloseDate + Certificate PDF):** opportunistic fix A1 selector + extract Phase 313 block ke `exam-block-manual-submit.spec.ts` standalone (remove file-level serial mode dependency).
- **Phase 320 (proposed — FLOW A-J refresh):** dedicated phase wholesale rewrite jika pass rate post-A1-fix masih jatuh > 50%.

## Raw Output Reference

Full output captured at `/tmp/flow-a-j-output.txt` (76 tests, 1 setup + 1 failed + 74 did-not-run).

## Phase 317 Closure Posture

QA-02 coverage **5/5 FLOW K-O HIJAU** via Phase 317 Plan 01 + Plan 02. Regression baseline FLOW A-J `Phase 320`-tracked per recommendation di atas. Phase 317 dapat di-close.
