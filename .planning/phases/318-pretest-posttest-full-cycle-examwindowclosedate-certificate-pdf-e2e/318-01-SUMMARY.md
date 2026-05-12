---
phase: 318
plan: 01
status: completed-with-deviation
commit: f9704fb7
date: 2026-05-12
---

# Plan 318-01 Summary — SURF-317-A1 Selector Patch

## Outcome

**Scope-bounded patch applied + Phase 317 regression gate HIJAU.** A1 PASS acceptance criterion deferred ke Phase 320 (architectural mismatch, see Deviation).

## Files Modified

| File | LOC delta | Purpose |
|------|-----------|---------|
| `tests/e2e/exam-taking.spec.ts` | 1 line (line 40) | Selector legacy → Phase 304+ form-check compat |
| `docs/test-reports/2026-05-12-surf-317-a1-patch.md` | NEW | Post-patch delta report + Phase 320 anchor catalog |

## Patch Detail

`tests/e2e/exam-taking.spec.ts:40`:

```diff
-    await page.locator('.user-check-item', { hasText: 'rino.prasetyo' }).locator('input').click({ force: true });
+    await page.locator('.user-check-item[data-email="rino.prasetyo@pertamina.com"] input.user-checkbox').check({ force: true });
```

## FLOW A1 Status

- **Pre-patch:** CASCADE-ABORT — legacy selector multi-match wrong element
- **Post-patch:** **FAIL** dengan root cause baru — `.check({ force: true })` visibility error karena Step 2 wizard panel hidden (`<button [disabled]>2. Peserta</button>`)

Selector RESOLVES correctly ke `<input class="form-check-input user-checkbox" name="UserIds" id="user_4a624dbc...">` — element ada di DOM tapi panel parent disabled.

## Phase 317 Regression Gate

- Command: `cd tests && npx playwright test exam-types.spec.ts --reporter=list`
- Result: **28/28 PASS** (2.0m)
- Setup + W0.1/W0.2 + FLOW K (5) + L (6) + M (5) + N (4) + O (5) HIJAU
- Teardown clean (snapshot RESTORE OK, 0 matrix rows, SEED_JOURNAL cleaned)

## Cascade Delta (exam-taking.spec.ts)

| Metric | Baseline 2026-05-11 | Post-patch 2026-05-12 |
|--------|---------------------|------------------------|
| Setup | 1 PASS | 1 PASS |
| FLOW passed | 0 | 0 |
| FLOW failed | 1 (A1) | 1 (A1, new root cause) |
| Cascade-skipped | 74 | 74 |

## TypeScript Compile Gate

`cd tests && npx tsc --noEmit` → **exit 0**

## Scope Guard

`git diff HEAD~1` = 2 files modified:
- `tests/e2e/exam-taking.spec.ts` (1 LOC)
- `docs/test-reports/2026-05-12-surf-317-a1-patch.md` (new)

No other test files, no helpers, no Razor views, no controllers touched. D-318-01 preserved.

## Deviation (vs Plan 01 acceptance criteria)

**Plan 01 acceptance #3 (`A1 PASS via grep "A1 - HC creates"`) NOT MET** karena diagnostic mengungkap `/Admin/CreateAssessment` sekarang 4-step wizard (Phase 304+ refactor); FLOW A1 test (lines 34-57) assume flat-page legacy markup yang sudah hilang.

**Decision (user-confirmed):** Defer A1 + 9 sibling `.user-check-item` instances + lines 34-57 assertions (`#Title`, `#Category`, `#ScheduleDate`, `#submitBtn`) ke **Phase 320 wholesale FLOW A-J refresh** menggunakan helper Phase 317 (`createDefaultAssessmentViaWizard`). Patch line 40 kept — selector correctness preserved + obsolete force-click pattern eliminated.

**Plan 01 acceptance #1, #2, #4-7 MET:**
- Line 40 patched ke Phase 304+ form-check ✓
- Phase 317 regression 28/28 HIJAU ✓
- TypeScript clean ✓
- Delta report exists dengan Phase 320 anchor catalog ✓
- Scope guard preserved (2 file changes only) ✓
- Commit format `fix(318-01): SURF-317-A1 — ...` ✓

## Out-of-Scope Selectors Anchored ke Phase 320

- `/Admin/CreateAssessment` flat → 4-step wizard (same URL, refactored UI)
- 9 sibling `.user-check-item input.click({ force: true })` di lines 320, 407, 552, 706, 837, 967, 1061, 1295, 1408
- `#Title`, `#Category`, `#ScheduleDate`, `#ScheduleTime`, `#submitBtn` legacy field references
- `name="question_text"`, `name="options[]"`, `input[name="correct_option_index"]` (Phase 298+ obsolete)
- `/Admin/ManageQuestions` route (Phase 298+ → `/Admin/ManagePackageQuestions`)
- Wizard auto-create package absence (Phase 317 W0.1 discovery)

## Next

Wave 1 parallel: **Plan 318-02 — SURF-317-A production fix** (`CMPController.cs:2190` `ToLookup` MA-aware refactor + Razor view update + Phase 317 MA regression rerun gate).
