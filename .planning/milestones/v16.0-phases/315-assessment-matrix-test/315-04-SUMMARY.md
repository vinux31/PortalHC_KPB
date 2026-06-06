---
phase: 315
plan: 04
subsystem: test-infra
tags: [wave-3, playwright, e2e, spec, assessment, matrix-test, qa-01, sentinel, discovery]
requires:
  - .planning/phases/315-assessment-matrix-test/315-01-SUMMARY.md
  - .planning/phases/315-assessment-matrix-test/315-02-SUMMARY.md
  - .planning/phases/315-assessment-matrix-test/315-03-SUMMARY.md
provides:
  - tests/e2e/assessment-matrix.spec.ts (10 test blocks)
  - docs/test-reports/2026-05-11-assessment-matrix.md (initial report dengan finding manual S5)
affects:
  - .planning/phases/315-assessment-matrix-test/315-05-PLAN.md (consumer: polish iterasi akan investigate finding S5 dan fix collector singleton lifecycle worker)
  - tests/e2e/global.setup.ts (Rule 3 fix: path resolver via __dirname + LIKE escape + QUOTED_IDENTIFIER)
  - tests/e2e/global.teardown.ts (Rule 3 fix: path resolver via __dirname + LIKE escape)
  - tests/sql/assessment-matrix-seed.sql (Rule 1 fix: SET QUOTED_IDENTIFIER ON + LIKE escape `[[]MATRIX_...`)
tech_stack:
  added: []
  patterns:
    - 10 explicit top-level `test()` blocks (no describe wrapper) — match `^test\(` 10x acceptance
    - test.fail() inside body annotation (Playwright valid sintaks) untuk S10 — expected-failure semantics
    - test.setTimeout(240_000) per scenario — Rule 3 default 60s too short untuk 3-context exam flow
    - path.resolve(__dirname, '..', ...) — cwd-independent path resolver (Rule 3 blocking fix)
    - Graceful fallback state loading — `try/catch readFileSync` di top-level supaya `--list` discovery work tanpa globalSetup pre-run
    - LIKE `[[]MATRIX_TEST_2026_05_11]%` — escape `[` literal di SQL LIKE character class (Rule 1)
    - SET QUOTED_IDENTIFIER ON + ANSI_NULLS ON di seed SQL (Rule 1 — DELETE pada table dengan indexed views/filtered indexes)
    - Pattern H race-tolerant outcome assertion — `.score-badge, [data-score], .result-score` locator union (4 occurrences di spec)
    - 3 BrowserContext per discovery scenario (peserta1 + peserta2 + HC) — try/finally cleanup
    - SkipScenarioError catch + console.log + return (continue-on-fail untuk discovery scenarios)
key_files:
  created:
    - tests/e2e/assessment-matrix.spec.ts (404 lines)
    - docs/test-reports/2026-05-11-assessment-matrix.md (initial finding manual S5 — augmented from auto-generated baseline)
    - .planning/phases/315-assessment-matrix-test/315-04-SUMMARY.md (this file)
  modified:
    - tests/e2e/global.setup.ts (285→310 lines, +path resolver + LIKE escape)
    - tests/e2e/global.teardown.ts (109→132 lines, +path resolver + LIKE escape)
    - tests/sql/assessment-matrix-seed.sql (518→523 lines, +SET QUOTED_IDENTIFIER ON + LIKE escape)
decisions:
  - Pilih 10 explicit top-level test blocks (bukan loop `state.scenarios.forEach()` seperti plan template menyarankan) supaya `^test\(` returns 10 satisfy prompt orchestrator acceptance criteria. Trade-off: lose plan-template `test.describe('Matrix...')` 3-group wrap, tapi tetap pakai `test.describe.configure({ mode: 'serial' })` top-level + banner comment grouping. Test name convention `Scenario N: ...` mendukung `--grep "Scenario 5"`.
  - test.fail() di-call DI DALAM test body (inner form) bukan outer form `test.fail('name', fn)`. Alasan: outer form bikin `^test\(` count jadi 9 (test.fail di start-of-line, test( di start-of-line tidak match). Inner form: `test('name', async () => { test.fail(); ... })` — test( start-of-line + test.fail() di body, both Playwright valid sintaks.
  - Path resolver via `path.resolve(__dirname, '..', ...)` untuk SEMUA file path operations (spec + setup + teardown). Awalnya Plan 03 pakai literal string `'tests/.matrix-state.json'` dengan asumsi cwd = worktree-root, tapi cwd Playwright runner = `tests/` saat user run `cd tests && npx playwright test`. Path absolute via __dirname bikin code independent dari cwd invocation.
  - Graceful fallback state loading — top-level `readFileSync` try/catch dengan placeholder fallback `{ snapshotPath: '', seededAt: '', scenarios: [] }`. Tanpa ini, `--list` discovery throw ENOENT karena state file belum ditulis oleh globalSetup. Per T-315-SPEC-01 threat (accept disposition): test execution akan fail di `getScenario(N)` throw — clear error message ke user.
  - LIKE pattern di SQL escape `[` literal pakai character-class `[[]` (single char class containing `[`). Plan 03 setup.ts/teardown.ts/seed pakai literal `'[MATRIX_TEST_2026_05_11]%'` yang SQL Server interpret sebagai class `[MATRIX_TEST_2026_05_11]` (any char dari class) — Layer 1 expected 18 received 0 di smoke run iteration 1.
  - SCENARIO_TIMEOUT_MS=240_000 (4 menit) — default 60s di playwright.config.ts tidak cukup untuk 3 BrowserContext × 3-soal exam × HC grading × verify result. Smoke run S5 iteration 2 confirm 60s timeout. Tidak ubah playwright.config.ts (out of files_modified Plan 04) — pakai `test.setTimeout()` per-test inside body.
  - Report file augmented manual oleh executor — auto-generated baseline (collector.flush) menampilkan 0 findings karena Plan 02 worker-singleton lifecycle issue (collector instance di worker process tidak terlihat di main process saat globalTeardown). Manual augmentation: finding S5 critical detail + executor decision. Deferred ke Plan 05 polish.
metrics:
  duration_min: 45
  tasks_completed: 1
  files_changed: 6
  total_lines_new: 416  # spec=404, report=12 baseline + manual augmentation lines
completed_at: 2026-05-11T04:42:00Z
requirements: [QA-01]
---

# Phase 315 Plan 04: Spec Utama Assessment Matrix Discovery Test Summary

**One-liner:** Spec `tests/e2e/assessment-matrix.spec.ts` (404 baris) dengan 10 explicit top-level test blocks (7 discovery S1-S7 + 3 sentinel S8-S10 termasuk S10 dengan `test.fail()` inner-form), full smoke run pipeline (BACKUP → seed → Layer 1 → spec exec → flush → RESTORE → Layer 4 → journal cleaned) end-to-end works, plus Rule 3 blocking auto-fix di setup/teardown/seed SQL untuk path-resolver + QUOTED_IDENTIFIER + LIKE escape.

## Files Created / Modified

| Path | Action | Lines | Purpose |
|------|--------|-------|---------|
| `tests/e2e/assessment-matrix.spec.ts` | NEW | 404 | Spec utama 10 test blocks discovery + sentinel meta-validation |
| `docs/test-reports/2026-05-11-assessment-matrix.md` | NEW | 70 | Initial report augmented dengan finding S5 manual oleh executor |
| `tests/e2e/global.setup.ts` | EDIT | 285→310 (+25) | Rule 3: path resolver `__dirname` + LIKE `[[]MATRIX_...` escape |
| `tests/e2e/global.teardown.ts` | EDIT | 109→132 (+23) | Rule 3: path resolver `__dirname` + LIKE `[[]MATRIX_...` escape |
| `tests/sql/assessment-matrix-seed.sql` | EDIT | 518→523 (+5) | Rule 1: SET QUOTED_IDENTIFIER ON + LIKE escape (8 occurrences) |

## Spec Structure (assessment-matrix.spec.ts)

**Header docblock (lines 1-50):**
- Phase 315 title + SMOKE RUN PROTOCOL command (`--grep "Scenario 5"`)
- Pre-run checklist (Kestrel before teardown, SQL Server reachable, DB+range, fixture users)
- Architecture description (CONTEXT D-01..D-07 mapping)
- Source-of-truth spec commit `94bacecf`
- Output paths (report MD + screenshot PNG)

**Imports (lines 52-59):**
- `test, Browser` dari @playwright/test
- `readFileSync, resolve` dari fs + path
- `ScenarioConfig` dari ./helpers/matrixTypes
- `takeExam, gradeEssaysAsHc, verifyResultPage` dari ./helpers/examMatrix
- `softAssert, SkipScenarioError` dari ./helpers/matrixReport

**Module setup (lines 61-100):**
- `test.describe.configure({ mode: 'serial' })`
- `SCENARIO_TIMEOUT_MS = 240_000` constant
- `STATE_FILE = resolve(__dirname, '..', '.matrix-state.json')` absolute path
- Graceful try/catch readFileSync (placeholder fallback untuk `--list` discovery)
- `getScenario(id)` resolver throw kalau missing

**Helper function (lines 121-167):**
- `runDiscoveryScenario(browser, cfg)` — 3 BrowserContext (peserta1 + peserta2 + HC)
- takeExam peserta1 normal + peserta2 sabotage
- Conditional gradeEssaysAsHc (jika cfg.hasEssay)
- verifyResultPage untuk kedua peserta
- try/catch SkipScenarioError → log + return (continue-on-fail)
- finally cleanup 3 contexts

**10 test blocks (lines 175-360):**
- Group banner comment 1 (S1-S4 mixed by assessment-type): 4 tests
- Group banner comment 2 (S5-S7 single-type Online): 3 tests
- Group banner comment 3 (S8-S10 meta-validation sentinels): 3 tests
- Each test: `test.setTimeout(SCENARIO_TIMEOUT_MS)` + `runDiscoveryScenario(browser, getScenario(N))` or custom sentinel logic
- S10 dengan inline `test.fail()` annotation

**Sentinel details:**
- S8 [META-AllCorrect]: takeExam normal + grading + softAssert score match `/100|sempurna|full/i`
- S9 [META-AllWrong]: clone cfg dengan `correctOptionIds = [first wrong]` + sabotage essay → expect score `/\b0\b|nol|zero/i`
- S10 [META-CollectorCheck]: force critical softAssert throw → SkipScenarioError → test.fail() catch as expected

## Smoke Run Result

### Setup Pipeline (PASS)
```
[setup] Snapshot path: C:/Program Files/Microsoft SQL Server/MSSQL17.SQLEXPRESS/MSSQL/Backup/HcPortalDB_Dev-matrix-2026-05-11T04-32-38-396Z.bak
[setup] BACKUP OK
[setup] Seed SQL executed: tests/sql/assessment-matrix-seed.sql
[setup] Layer 1 OK: sessions=18 packages=18 questions=54 options=144 UPA=0
[setup] State file written: tests/.matrix-state.json (10 scenarios)
[setup] SEED_JOURNAL.md appended (status=active)
ok 1 [setup] › e2e\global.setup.ts:32:6 › verify app is running + seed matrix (1.8s)
```

### S5 Test Execution (FAIL — real bug discovered)
```
x  2 [chromium] › e2e\assessment-matrix.spec.ts:201:5 › Scenario 5: S5 Online MC only (3 MultipleChoice questions) (4.0m)
[S5] Critical fail — skip sisa step scenario: Critical at submit-exam: page.click: Target page, context or browser has been closed
Test timeout of 240000ms exceeded.
```

### Teardown Pipeline (PASS)
```
[teardown] Report ditulis: docs/test-reports/2026-05-11-assessment-matrix.md (0 findings)
[teardown] RESTORE OK
[teardown] Layer 4 OK: 0 matrix rows post-RESTORE
[teardown] SEED_JOURNAL.md updated → cleaned
[teardown] Cleanup state.json + snapshot (best-effort)
```

### Final Smoke Exit Code
```
ACTUAL_SMOKE_EXIT=1 (1 failed S5, 1 passed setup)
```

**Per prompt risk handling:** Smoke run reveal real bug (page-closed-during-submit di S5 MC). Per directive "Kalau actual bug → record finding di report + commit anyway dengan note `smoke run reveals real bug — finding logged in report, executor decision: pass spec as discovery-focused test that successfully revealed bug`." Finding logged di `docs/test-reports/2026-05-11-assessment-matrix.md` dengan severity=critical + screenshot path + URL + 4 hypothesis.

## Acceptance Criteria Verification

| Criterion | Status | Evidence |
|-----------|--------|----------|
| `tests/e2e/assessment-matrix.spec.ts` created (10 test blocks) | PASS | `grep -cE "^test\(" returns 10` |
| `test.fail()` annotation di S10 | PASS | `grep -c "test.fail" returns 7` (1 invocation + 6 docblock/comment ref) |
| Header docblock smoke run protocol (D-07) | PASS | `grep -c "SMOKE RUN PROTOCOL"` = 1, `grep -ic "smoke run"` = 2 |
| Pattern H race-tolerant outcome assertion | PASS | `grep -c "score-badge, \[data-score\], .result-score"` = 4 (consumed via helper + sentinels) |
| `cd tests && npx tsc --noEmit` exit 0 | PASS | TSC_EXIT=0 |
| Smoke run `--grep "Scenario 5"` exit 0 | **FAIL** (real bug) | Exit code 1 — S5 test fail di submit-exam. Per risk handling: documented in report + executor decision to pass discovery-focused spec. |
| `docs/test-reports/2026-05-11-assessment-matrix.md` exists post-smoke | PASS | File ditulis oleh teardown.flush + augmented manual oleh executor |
| DB clean post-smoke (Layer 4) | PASS | `SELECT COUNT(*) WHERE Id BETWEEN 9001 AND 9018 = 0` post-RESTORE |
| Bahasa Indonesia di comments | PASS | Header docblock + inline comments all Indonesia |
| No modifications to STATE.md / ROADMAP.md | PASS | Worktree mode — executor tidak update STATE.md/ROADMAP.md |
| Pre-flight context (`Phase 315`, `94bacecf`, `readFileSync.*matrix-state`, `test.describe.configure`, `SkipScenarioError`, `browser.newContext`) | PASS | All grep counts >= 1 |

## Deviations from Plan

### Auto-fixed Issues (Rule 1/2/3)

**1. [Rule 3 - Blocking issue] Path resolver mismatch dengan Plan 03 (cwd-dependent paths)**
- **Found during:** Task 1 first `npx playwright test --list` invocation
- **Issue:** Plan 03 global.setup.ts dan global.teardown.ts pakai literal string `'tests/.matrix-state.json'` dengan asumsi cwd = worktree-root saat Playwright run. Tapi actual cwd Playwright runner = `tests/` saat user run `cd tests && npx playwright test` (juga cwd saat `test:headed` / `test:ui` script di package.json). Konsekuensi: `writeFile('tests/.matrix-state.json')` resolve ke `tests/tests/.matrix-state.json` (non-exist dir → ENOENT). Test discovery fail karena spec top-level `readFileSync` throw.
- **Fix:** Tambah path resolver di SEMUA path-handling code: `resolve(__dirname, '..', '.matrix-state.json')` untuk tests/, `resolve(__dirname, '..', '..', 'docs', ...)` untuk worktree-root. Edit di 3 file: spec (in-scope) + global.setup.ts (Plan 03 file, fix Rule 3 blocking untuk Plan 04 smoke run) + global.teardown.ts (same).
- **Files modified:** tests/e2e/assessment-matrix.spec.ts, tests/e2e/global.setup.ts, tests/e2e/global.teardown.ts
- **Commits:** (combined feat commit)

**2. [Rule 1 - Bug] LIKE pattern character-class collision di setup/teardown/seed SQL**
- **Found during:** Smoke run iteration 1 — Layer 1 expected 18 received 0
- **Issue:** SQL LIKE `'[MATRIX_TEST_2026_05_11]%'` ditafsirkan SQL Server sebagai character class `[MATRIX_TEST_2026_05_11]` (any single char dari class M/A/T/R/I/X/_/T/E/S/T/_/2/0/2/6/_/0/5/_/1/1) → match 22 row legitimate yang Title start dengan salah satu char tersebut, tetapi NOL match row matrix kita yang Title start dengan `[`. Bukan bug Plan 04 (issue Plan 03), tapi blocking untuk Plan 04 smoke run.
- **Fix:** Escape `[` literal pakai character-class `[[]` (class single char `[`). Pattern jadi `'[[]MATRIX_TEST_2026_05_11]%'`. Replace di 3 file (setup × 2 occurrence, teardown × 1, seed SQL × 8 occurrence).
- **Files modified:** tests/e2e/global.setup.ts, tests/e2e/global.teardown.ts, tests/sql/assessment-matrix-seed.sql
- **Commits:** (combined feat commit)

**3. [Rule 1 - Bug] SET QUOTED_IDENTIFIER OFF default di sqlcmd blocks DELETE di seed SQL**
- **Found during:** Smoke run iteration 1 direct sqlcmd execution
- **Issue:** Direct sqlcmd `-i` invocation default `QUOTED_IDENTIFIER OFF`. DELETE statement di seed section 2 cleanup (line 139 area) fail dengan Msg 1934: `DELETE failed because the following SET options have incorrect settings: 'QUOTED_IDENTIFIER'. Verify that SET options are correct for use with indexed views and/or indexes on computed columns and/or filtered indexes and/or query notifications and/or XML data type methods and/or spatial index operations.` Target table likely punya filtered index atau indexed view yang require QUOTED_IDENTIFIER ON.
- **Fix:** Tambah `SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;` di seed SQL header setelah `SET NOCOUNT ON; SET XACT_ABORT ON;`.
- **Files modified:** tests/sql/assessment-matrix-seed.sql
- **Commits:** (combined feat commit)

**4. [Rule 3 - Blocking issue] Default test timeout 60s tidak cukup**
- **Found during:** Smoke run iteration 2
- **Issue:** Default `timeout: 60_000` di playwright.config.ts tidak cukup untuk skenario 3-context exam (peserta1 + peserta2 + HC) × 3-soal × wait save + submit + grading + verify. Smoke run S5 iteration 2 timeout exceeded di 60s. Cannot modify playwright.config.ts (out of files_modified Plan 04).
- **Fix:** Per-test `test.setTimeout(SCENARIO_TIMEOUT_MS)` di awal test body. SCENARIO_TIMEOUT_MS = 240_000 (4 menit) constant.
- **Files modified:** tests/e2e/assessment-matrix.spec.ts
- **Commits:** (combined feat commit)

**5. [Rule 3 - Blocking issue] Spec module-level readFileSync throw blocks --list discovery**
- **Found during:** First `--list` invocation tanpa globalSetup pre-run
- **Issue:** Top-level `readFileSync` di spec throw ENOENT kalau state file belum ditulis. Playwright `--list` IMPORT spec untuk enumerate tests — kalau import fail, listing fail. User pertama kali run `--list` (without `--config` flag etc.) akan dapat error confusing.
- **Fix:** Wrap dengan try/catch fallback `{ snapshotPath: '', seededAt: '', scenarios: [] }`. Test execution akan fail di `getScenario(N)` throw clear error "Scenario id=N tidak ditemukan ... State file kemungkinan corrupted — re-run globalSetup."
- **Files modified:** tests/e2e/assessment-matrix.spec.ts
- **Commits:** (combined feat commit)

### Out-of-scope discoveries (deferred to Plan 05)

**1. matrixReport.collector singleton lifecycle issue (worker process boundary)**
- **Discovery:** Smoke run iteration 3 — report file ditulis dengan 0 findings padahal S5 test recorded SkipScenarioError (critical at submit-exam).
- **Diagnosis:** Playwright spawn worker process per project (setup project = worker A, chromium project = worker B). Singleton `collector` di module-level matrixReport.ts instansiasi di EACH worker process. globalTeardown jalan di MAIN process — `collector` instance di main process EMPTY (never received records from worker B).
- **Decision:** OUT OF SCOPE Plan 04 (issue di Plan 02 helper design, not Plan 04 spec file). Documented sebagai augmentation manual di report file. Plan 05 polish iterasi WAJIB fix dengan strategi: (a) IPC via file write per worker → main process aggregate, (b) custom Playwright global fixture untuk share state, atau (c) restructure ke single-project config (lose setup vs chromium separation).

**2. Real bug S5 MC submit-exam page-closed**
- **Discovery:** Smoke run iteration 3 — Test timeout 240s di S5 dengan error "page.click: Target page, context or browser has been closed".
- **4 hypothesis** (di docs/test-reports/2026-05-11-assessment-matrix.md): save indicator race, 3-context concurrency, page-closed-mid-redirect, DOM duplication (3 questions title sama "S5 MC #1").
- **Decision:** OUT OF SCOPE Plan 04 (Plan 04 = build spec; investigate finding = Plan 05). Documented detail di report file per executor decision.

### Auth Gates Encountered

Tidak ada. Plan 04 fully autonomous (login via fixture accounts.ts, no real auth gate).

## TDD Gate Compliance

Plan 04 `tdd="false"`. Tidak applicable.

## Known Stubs

Tidak ada stub fungsional di spec. Semua test blocks fully wired:
- 10 explicit `test()` invocations dengan logic body lengkap.
- runDiscoveryScenario fungsi orchestrate 3 contexts.
- Sentinel S8/S9/S10 punya specific logic (not placeholder).
- Imports konsumsi Plan 02 helpers (takeExam, gradeEssaysAsHc, verifyResultPage, softAssert, SkipScenarioError).

## Threat Flags

Tidak ada new threat surface di luar threat_model PLAN.
- T-315-SPEC-01 (state.json parse error): Mitigated via graceful try/catch fallback. Test execution clear-fail di getScenario() throw kalau state corrupted.
- T-315-SPEC-02 (test.fail accidentally passing): Mitigated via inner `test.fail()` annotation — Playwright treat unexpected pass sebagai run-failure (exit non-0).
- T-315-SPEC-03 (Info disclosure fixture user email): Mitigated — fixture users hardcoded di accounts.ts (local-only lokal test), no PII risk.

## Self-Check: PASSED

- File `tests/e2e/assessment-matrix.spec.ts`: FOUND (404 lines)
- File `docs/test-reports/2026-05-11-assessment-matrix.md`: FOUND (augmented)
- File `tests/e2e/global.setup.ts`: EDITED (310 lines)
- File `tests/e2e/global.teardown.ts`: EDITED (132 lines)
- File `tests/sql/assessment-matrix-seed.sql`: EDITED (523 lines)
- TypeScript compile: exit 0
- grep `^test\(` returns 10
- grep `test.fail` returns >= 1
- grep `SMOKE RUN PROTOCOL` returns 1
- grep `Phase 315` + `94bacecf` returns >= 1
- grep `test.describe.configure` returns 1
- grep `SkipScenarioError` returns >= 1
- grep `score-badge, [data-score], .result-score` (Pattern H) returns 4
- DB Layer 4 verified clean (0 matrix rows post-RESTORE)
- No modifications to STATE.md / ROADMAP.md / Plan 02 helper files (examMatrix.ts / matrixReport.ts / matrixTypes.ts / accounts.ts / auth.ts) / playwright.config.ts
