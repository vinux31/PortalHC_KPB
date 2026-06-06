---
phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso
plan: 05
subsystem: testing
tags: [playwright, e2e, gap-closure, wave-2, describe-restructure, serial-halt]

# Dependency graph
requires:
  - phase: 316-03
    provides: Wave 0 A2 VALID confirmation — describe boundary di `fullyParallel: false` mengisolasi failure (probe `1 failed, 2 passed`)
  - phase: 316-04
    provides: Cascade promotion (a-revised) di matrixReport.ts — defense in depth foundation; tanpa Plan 04 even isolated describe akan hit 240s timeout
provides:
  - assessment-matrix.spec.ts: 10 `test.describe()` blocks (S1-S10) terpisah, each contains 1 `test('execute', ...)`
  - Failure isolation per describe boundary — failure di S1 TIDAK halt S2-S10
  - Spec-level `test.describe.configure({mode:'serial'})` DROPPED — sequential execution preserved via config-level `fullyParallel: false` + `workers: 1`
affects:
  - 316-06 (Wave 3 final validation E2E — full S1-S10 run "0 did not run" expectation)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Describe-per-scenario restructure pattern: wrap each top-level test() ke test.describe('name', () => { test('execute', ...) }) untuk failure isolation per scenario di `fullyParallel: false` mode"
    - "test.fail() annotation preserve di INSIDE test body — scope=test (bukan describe boundary), valid di any describe context per Playwright docs"

key-files:
  created: []
  modified:
    - "tests/e2e/assessment-matrix.spec.ts (10 describe wraps S1-S10 + drop spec-level serial config + S10 docstring update)"

key-decisions:
  - "Drop spec-level `test.describe.configure({mode:'serial'})` di line 64 — sequential execution preserved via `fullyParallel: false` di playwright.config.ts + `workers: 1`. Empirical proof Wave 0 probe (`_throwaway-probe.spec.ts`): `1 failed, 2 passed` confirm describe boundary isolate failure tanpa spec-level serial mode."
  - "Wrap pattern uniform: tiap describe pakai full scenario name (e.g. 'Scenario 5: S5 Online MC only (3 MultipleChoice questions)'), inner test name = 'execute' untuk konsistensi 10 blocks. Output Playwright list reporter akan show path 'Scenario X > execute'."
  - "S8/S9/S10 sentinel inner body PRESERVE 100% literal — semua softAssert call, try/catch/finally, ctx.close logic, cfgAllWrong clone (S9), test.fail() annotation (S10) tetap identical. Wrap pure organizational."
  - "S10 test.fail() annotation placement INSIDE test body line 401 (BUKAN di describe block) — annotation scope=test per Playwright docs. Comment doc append note konfirmasi scope semantic."
  - "Stale docstring di line 396-398 (`literal test( tetap di start-of-line untuk konsistensi 10 test blocks`) UPDATED — sekarang `test.describe(` di start-of-line; doc note jelaskan annotation scope tidak terpengaruh wrap (Rule 1 doc fix)."
  - "Single atomic commit untuk Task 2 — Task 1 hanya gate check (read-only), tidak butuh commit. Audit trail clean: 1 file modified, 1 commit `465578bc`."

patterns-established:
  - "Wave 2 spec restructure pattern: 1 file modified, mechanical wrap 10x identical shape, zero overlap dengan Plan 04 helper scope (parallel-safe historical, no merge conflict)."
  - "Sequential preservation via config layer (not spec layer): `fullyParallel: false` + `workers: 1` di playwright.config.ts cukup; spec-level `describe.configure({mode:'serial'})` redundant + harmful (halt-on-first-fail semantic). Drop spec-level untuk failure isolation."

requirements-completed: [GAP-316-2, GAP-316-3]

# Metrics
duration: ~12 min
completed: 2026-05-11
---

# Phase 316 Plan 05: Wave 2 Spec Restructure Summary

**10 `test()` blocks di-wrap ke 10 `test.describe()` blocks terpisah + spec-level `test.describe.configure({mode:'serial'})` di-drop untuk close GAP-316-2 (d-partial). Failure isolation per describe boundary di `fullyParallel: false` — failure di S1 TIDAK halt S2-S10. TypeScript compile clean, helper signatures unchanged, S8/S9/S10 sentinel inner body preserved literal.**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-05-11 (Wave 2 parallel executor spawn)
- **Completed:** 2026-05-11 (post-commit T2 + SUMMARY)
- **Tasks:** 2/2 completed (Task 1 gate check read-only, Task 2 restructure)
- **Files modified:** 1 (no creation)

## Accomplishments

- **Gate A2 VALID confirmed (Task 1)** — Read `316-VALIDATION.md` line 112 `**Outcome:** **A2 VALID**` dari Plan 03 Wave 0 probe run. Empirical evidence: `_throwaway-probe.spec.ts` output `1 failed, 2 passed` di mode `fullyParallel:false` tanpa spec-level `mode:'serial'`. Konfirmasi describe boundary mengisolasi failure → Plan 05 PROCEED dengan ground-truth backing.

- **GAP-316-2 (d-partial) closed (Task 2)** — `assessment-matrix.spec.ts`:
  - Line 61-69: spec-level `test.describe.configure({mode:'serial'})` DROPPED, replaced dengan changelog comment Plan 05 + note collector race safety (Playwright spawn worker per FILE bukan per describe).
  - Line 185-242: S1-S4 4 test() blocks wrapped ke 4 `test.describe('Scenario N: ...', () => { test('execute', ...) })` blocks. Inner setTimeout + runDiscoveryScenario call preserved.
  - Line 250-273: S5-S7 3 test() blocks wrapped identical pattern.
  - Line 285-340: S8 META-AllCorrect wrapped, inner cfg + try/catch + softAssert + ctx.close PRESERVE literal.
  - Line 344-389: S9 META-AllWrong wrapped, inner cfgAllWrong clone + cfgZeroEssay + softAssert + try/catch/finally PRESERVE literal.
  - Line 401-431: S10 META-CollectorCheck wrapped, `test.fail()` annotation PRESERVE INSIDE test body line 403 dengan inline comment menjelaskan scope semantic (annotation scope=test bukan describe). `softAssert(severity='critical')` + force throw PRESERVE literal.
  - Stale docstring line 396-400: UPDATED — replace `literal test( tetap di start-of-line` dengan note scope test.fail() tidak terpengaruh wrap.

- **Plan 06 foundation siap** — Wave 3 full validation E2E sekarang punya:
  1. Plan 04 cascade promotion (a-revised) — eliminate accumulator timing (page-closed cascade catch <10s vs 240s).
  2. Plan 05 describe restructure (d-partial) — even if Plan 04 detect miss di scenario novel, failure boundary per describe akan isolate.
  - **Defense in depth stack** sesuai 316-RESEARCH-GAP-316-2.md line 184-198 "Hybrid (a-revised) + (d-partial)" recommendation.

- **Surgical preserve** — Helper signatures unchanged (runDiscoveryScenario, takeExam, gradeEssaysAsHc, verifyResultPage, softAssert). Module-scope state (SCENARIO_TIMEOUT_MS, STATE_FILE, getScenario, state) unchanged. Imports unchanged. Pure organizational wrap.

## Task Commits

Each task committed atomically dengan `--no-verify` (parallel executor di git worktree). Task 1 was read-only gate check — no commit needed.

1. **Task 1: Wave 0 gate check — confirm A2 outcome** — (no commit, read-only)
   - Read `316-VALIDATION.md` section "Plan 03 Wave 0 — A2 Probe Result"
   - Found: `**Outcome:** **A2 VALID**` di line 112
   - Evidence: `_throwaway-probe.spec.ts` Block B executed setelah Block A fail (`1 failed, 2 passed`)
   - Decision: PROCEED Task 2

2. **Task 2: Wrap 10 test() ke 10 test.describe() + drop spec-level serial config** — `465578bc` (refactor)
   - `tests/e2e/assessment-matrix.spec.ts`:
     - Drop `test.describe.configure({mode:'serial'})` line 64 (replaced dengan changelog comment Plan 05)
     - Wrap S1-S10 → 10 `test.describe('Scenario N: ...', () => { test('execute', async ({browser}) => {...}) })` blocks
     - Inner body S8/S9/S10 sentinel logic preserve LITERAL
     - S10 `test.fail()` annotation INSIDE test body — scope=test, valid di any describe context
     - Stale docstring line 396-400 updated dengan note scope annotation
   - 1 file changed, +219 / -191 lines (mostly indent + structural wrap)
   - TS compile clean (`cd tests && npx tsc --noEmit` exit 0) — validated via main-repo tsc (worktree node_modules not symlinked)
   - Grep validation:
     - `test\.describe\('Scenario` → 10 matches
     - `test\.describe\.configure` → 0 code matches (1 in comment only)
     - `test\.setTimeout(SCENARIO_TIMEOUT_MS)` → 10 matches
     - `^\s+test\.fail\(\);` → 1 match (S10 only, INSIDE test body)
     - `test\('execute'` → 10 matches

## Files Created/Modified

### Modified
- `tests/e2e/assessment-matrix.spec.ts` — 10 `test.describe('Scenario N: ...', () => { test('execute', ...) })` blocks. Spec-level `test.describe.configure({mode:'serial'})` dropped. S10 `test.fail()` annotation preserve INSIDE test body. S8/S9 sentinel inner body (cfg, softAssert, ctx.close, cfgAllWrong, cfgZeroEssay, try/catch/finally) preserved literal. Stale S10 docstring line 396-400 updated.

### Created
None.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Doc fix] Update stale S10 docstring**
- **Found during:** Task 2 (during structural counts validation)
- **Issue:** Comment line 396-398 di docstring S10 read `Kita pakai inner form supaya literal test( tetap di start-of-line untuk konsistensi 10 test blocks` — stale post-restructure karena sekarang `test.describe(` yang di start-of-line, bukan `test(`.
- **Fix:** Updated docstring untuk mention annotation scope=test (bukan describe boundary), reference Playwright docs annotation, note Phase 316 Plan 05 wrap tidak pengaruh test.fail() semantic.
- **Files modified:** `tests/e2e/assessment-matrix.spec.ts` line 396-400
- **Commit:** Bundled di `465578bc` (Task 2)

No architectural deviations, blocking issues, atau auth gates encountered.

## Authentication Gates

None. Plan 05 murni test spec restructure di test directory (`tests/e2e/`), tidak butuh auth, secret, atau credential.

## Threat Flags

None. Spec restructure adalah test infrastructure di test directory — tidak ada surface network endpoint, auth path, atau schema change baru.

## Known Stubs

None. Wrap pattern adalah complete refactor (structural). Tidak ada placeholder, empty values, atau "TODO" introduced.

## Deferred Issues

None — Plan 05 scope fully closed per success criteria.

## Decisions Made

1. **Drop spec-level `test.describe.configure({mode:'serial'})`** — Sequential execution preserved via config-layer (`fullyParallel: false` + `workers: 1`). Wave 0 probe empirical confirm describe boundary isolate failure tanpa spec-level serial. Spec-level serial redundant + harmful (halt-on-first-fail semantic). Source: 316-RESEARCH-GAP-316-2.md line 130-140 + Wave 0 probe outcome.

2. **Inner test name uniform = 'execute'** — Tiap describe block 1 child test bernama 'execute'. Konsisten, simple. Full scenario name (descriptive label) di describe level. Playwright list reporter output akan show path `Scenario 5 > execute` — clear lineage.

3. **S10 `test.fail()` placement INSIDE test body** — Annotation scope=test per Playwright docs (https://playwright.dev/docs/test-annotations). Valid di any describe context. Wrap describe TIDAK pengaruh annotation semantic. Comment inline + docstring update untuk reinforce.

4. **Inner body S8/S9/S10 sentinel logic preserved LITERAL** — Zero edit untuk softAssert calls, cfgAllWrong clone (S9), cfgZeroEssay (S9), try/catch/finally, ctx.close. Pure organizational wrap untuk maintain Plan 01/02 behaviors.

5. **Stale docstring update sebagai Rule 1 fix** — Comment line 396-398 `literal test( tetap di start-of-line` salah post-restructure. Update dengan reference scope semantic + Playwright docs link. Bundled di T2 commit (no separate commit).

6. **Single atomic commit untuk Task 2** — Task 1 read-only gate check (no file edit), tidak butuh commit. Task 2 = 1 file, 1 commit `465578bc`. Audit trail granular sesuai GSD executor protocol.

7. **`--no-verify` flag** — required per parallel executor protocol (git worktree). Pre-commit hooks bypass sesuai instruksi orchestrator.

## TDD Gate Compliance

Plan 05 type `execute`, bukan `tdd`. Tidak ada TDD RED/GREEN/REFACTOR sequence applicable. Validation via E2E full run di Plan 06 sebagai integration test (per VALIDATION.md task 316-06-02).

Gate sequence (test/feat/refactor commit pairing) NOT APPLICABLE. Single `refactor()` commit `465578bc` adalah valid untuk pure structural reorganization tanpa behavioral change.

## Threat Surface Scan

No new network endpoints, auth paths, file access patterns, atau schema changes di Plan 05. Pure test spec restructure di `tests/e2e/` directory. Tidak ada threat flag baru.

## Self-Check: PASSED

Verified:
- `tests/e2e/assessment-matrix.spec.ts` exists (FOUND — modified, contains 10 `test.describe('Scenario`)
- TS compile clean (`cd tests && npx tsc --noEmit` exit 0 — validated via main-repo tsc dengan node_modules; worktree file copied ke main repo, compiled, restored)
- Commit `465578bc` exists in git log (FOUND — Task 2)
- No file deletions (git diff --diff-filter=D --name-only HEAD~1 HEAD empty)
- Helper signatures unchanged (runDiscoveryScenario, takeExam, gradeEssaysAsHc, verifyResultPage, softAssert all intact in calling code)
- Plan 05 success criteria all met:
  - Gate check A2 outcome READ from VALIDATION.md ✓ (A2 VALID confirmed)
  - 10 test() wrapped ke 10 test.describe() blocks ✓ (grep count = 10)
  - `mode:'serial'` dropped at spec level ✓ (0 code matches)
  - test.fail() S10 preserved INSIDE test body ✓ (line 403)
  - TS compile clean ✓ (exit 0)
  - Each task committed atomic ✓ (Task 1 read-only, Task 2 = `465578bc`)
  - SUMMARY.md committed ✓ (next commit)
  - No modifications STATE.md/ROADMAP.md ✓ (parallel executor — orchestrator handles state updates)
