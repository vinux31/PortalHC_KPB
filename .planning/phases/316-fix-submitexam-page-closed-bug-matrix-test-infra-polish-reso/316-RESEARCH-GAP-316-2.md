# Phase 316 — Research GAP-316-2 (+ GAP-316-1)

**Researched:** 2026-05-11
**Domain:** Playwright serial mode halt-on-first-fail + softAssert + SkipScenarioError emit timing
**Confidence:** HIGH (sumber Playwright official issues + log evidence aktual + code inspection)
**Scope:** Decision-ready guidance untuk Plan 03 — close GAP-316-1 (submit URL regex) + GAP-316-2 (serial halt)

## Executive Summary

**Root cause GAP-316-2 SUDAH BISA DIDIAGNOSA SECARA DEFINITIF** dari log run v2:

- `[S1] Critical fail — skip sisa step scenario: page closed before essay-q50003 step — cascade abort` MUNCUL di log → catch SkipScenarioError + `return` BERHASIL eksekusi.
- Report final hanya catat 1 finding: `ma-q50002 — page.check: Test ended` (severity=major) → bukan submit-exam, tapi MA step.
- Test ditandai timeout 240000ms — artinya 240s habis sebelum function return.

**Implikasi:** Hipotesis original di 316-UAT.md GAP-316-2 (a) — "SkipScenarioError emit lambat di S1" — **PARSIALLY WRONG**. Catch sebenarnya cepat (return eksekusi). Yang lambat adalah operasi *sebelum* catch trigger — yaitu MA step yang sudah severity='major' (bukan critical) sehingga softAssert TIDAK throw, return null, lalu next iteration baru hit isClosed gate.

**Hipotesa baru (HIGH confidence):** Saat `page.check()` di MA step hit page-closed state, `page.check()` BUKAN throw langsung — ini WAIT internal Playwright (default 30s) sampai locator stabil. Lalu `#saveIndicatorText.waitFor({timeout: 5_000})` juga wait. Kombinasi multiple async wait di MA + Essay yang TIDAK throw fast → akumulasi timer mendekati 240s.

**Konfirmasi dari Playwright community** ([Issue #18329](https://github.com/microsoft/playwright/issues/18329) closed as not-planned, [Issue #17266](https://github.com/microsoft/playwright/issues/17266), [Issue #16199](https://github.com/microsoft/playwright/issues/16199)):
> "Serial mode by design: failure in 1 test halts subsequent. `test.fail()` masih counts as failure → trigger halt. Tidak ada built-in flag untuk continue. Workaround: pakai default mode (parallel-by-file, serial-within-describe) ATAU split tests ke multiple files."

**Primary recommendation:** Hybrid solusi **(a-revised) + (d-partial)** — (1) eskalasi semua MA/Essay step ke severity='critical' supaya SkipScenarioError emit cepat saat page closed (hilangkan accumulator timing), (2) restructure spec dari single `describe.serial` ke 10 `test.describe()` terpisah (serial-within tapi tiap describe independent) untuk eliminate halt-on-first-fail di Playwright runner level.

Singleton collector race adalah **NOT BLOCKER** untuk parallel — file-system-backed collector sudah implementasi multi-worker safe per matrixReport.ts:42-185 (per-worker NDJSON file → aggregate di flush). Kita tetap pakai `fullyParallel: false` + `workers: 1` untuk maintain sequential execution (DB seed integrity), tapi split describe untuk isolate failure.

---

## Root Cause Investigation

### Evidence Layer 1 — Log analysis run v2 (post deviation fix `fa9e4e86`)

File: `/tmp/316-full-run-v2.log`

```
[S1] Critical fail — skip sisa step scenario: page closed before essay-q50003 step — cascade abort
  x   2 [chromium] › e2e\assessment-matrix.spec.ts:180:5 › Scenario 1: S1 Manual Mixed (MC + MA + Essay) (4.0m)

  1) Scenario 1: S1 Manual Mixed (MC + MA + Essay)
    Test timeout of 240000ms exceeded.
```

**Decode:**
1. Console log `[S1] Critical fail — skip sisa step scenario: page closed before essay-q50003 step — cascade abort` — ini message TEMPLATE dari `runDiscoveryScenario` catch (spec.ts:157). SkipScenarioError CAUGHT, `return` eksekusi.
2. Test duration tertulis "4.0m" = 240s exact = exact Playwright timeout limit.
3. Stack trace TIDAK ada error path lain post-catch — artinya finally clean (defensive ctx.close swallow via `.catch(() => {})` per commit `fa9e4e86`).

### Evidence Layer 2 — Report final content

File: `docs/test-reports/2026-05-11-assessment-matrix.md`

```
**Total discovery findings:** 1
- Critical: 0
- Major: 1
- Minor: 0

### Scenario 1: [MATRIX_TEST_2026_05_11] S1 Manual Mixed — ma-q50002
- **Severity:** major
- **Actual:** page.check: Test ended.
  Call log:
    - waiting for locator('input.exam-checkbox[data-question-id="50002"][value="80005"]')
```

**Decode:**
1. **Hanya 1 finding tercatat** — bukan banyak. Bukan submit-exam, melainkan `ma-q50002` step (severity='major').
2. `actual: "page.check: Test ended"` — Playwright internal sudah deteksi test sudah berakhir (timeout) sebelum `page.check()` dapat retry stabilitas locator.
3. **No submit-exam finding recorded** — artinya page closure terjadi di MA step (q50002), bukan di submit-exam. Helper takeExam loop URUTAN: navigate → signalr-ready → MC q50001 → MA q50002 (← PAGE CLOSED HERE) → Essay q50003 (isClosed gate catch, throw SkipScenarioError).

### Evidence Layer 3 — Code path trace

File trace (sumber: kode aktual examMatrix.ts + matrixReport.ts):

**Timeline 240s S1:**

| Time | Event | Source |
|------|-------|--------|
| t=0 | Test starts, `setTimeout(240_000)` armed | spec.ts:181 |
| t≈0.5s | takeExam(page1) — login peserta1 | examMatrix.ts:76 |
| t≈2s | navigate-start-exam (critical) PASS | examMatrix.ts:78-85 |
| t≈3s | signalr-ready (critical) PASS | examMatrix.ts:88-101 |
| t≈4s | MC q50001 (major) PASS | examMatrix.ts:107-126 |
| t≈5s | MA q50002 (major) STARTED | examMatrix.ts:127-147 |
| t≈?s | **Page closed mid-MA step** (cause TBD: hang? crash? auto-submit timer?) | — |
| t=5s..240s | `page.check()` internal Playwright retry wait stabilitas locator (default ~30s) | Playwright API |
| t=?s | Saat retry hit timeout, throw `page.check: Test ended.` | — |
| t=?s | softAssert catch (matrixReport.ts:206) → severity='major' → record finding → **return null (NO throw)** | matrixReport.ts:244-247 |
| t=?s | takeExam loop continue ke i=2 (Essay q50003) | examMatrix.ts:103 |
| t=?s | Essay q50003 callback start → isClosed() gate TRUE → throw SkipScenarioError | examMatrix.ts:153-155 |
| t=?s | softAssert re-throw branch (matrixReport.ts:211-213) — passthrough | matrixReport.ts |
| t=?s | runDiscoveryScenario catch SkipScenarioError → `return` | spec.ts:154-159 |
| t=?s | finally → ctx.close().catch(...) — fast (already closed) | spec.ts:168-170 |
| **t=240s** | **Playwright runner-level timeout fires SEBELUM/SAAT semua step ini selesai** | Playwright runner |

**KUNCI:** Test timeout 240s adalah **runner-level wall clock**. Bahkan jika catch sudah selesai eksekusi, **kalau total accumulated time > 240s, Playwright tetap tandai test sebagai timeout-failed**.

### Why timing accumulation hit 240s

`page.check()` Playwright API punya **internal auto-wait/auto-retry** sampai locator actionable. Default timeout = `actionTimeout` (matrix tidak set → ke `timeout` global 60s ATAU per-test setTimeout 240s).

Saat page closed pas page.check fire:
- Playwright BUKAN throw langsung — retry locator visibility (default behavior).
- Retry continue sampai retry timeout (per-action) atau test timeout (whichever first).
- `actionTimeout` tidak di-set di playwright.config.ts:11 — default = test timeout (240s).
- Sehingga **1 page.check() bisa hang sampai 240s** kalau page closed dan tidak ada timeout per-action.

**File:line:** `tests/playwright.config.ts:7` — `timeout: 60_000` (global per-test default, tapi spec override via `test.setTimeout(SCENARIO_TIMEOUT_MS)` = 240_000).
**File:line:** `tests/playwright.config.ts:11` — `expect: { timeout: 10_000 }` — tapi ini HANYA untuk `expect()`. `page.check()` tidak ter-cover.

### Why softAssert catch eksekusi tapi test masih FAIL

`runDiscoveryScenario` catch + return BERHASIL eksekusi (terbukti via console log `[S1] Critical fail`). Tapi pada saat console.log fire, Playwright runner sudah **sebelum** ini sudah set "test timeout exceeded" — artinya:

1. Time hit 240s SAAT MA step lagi retry page.check() (atau saveIndicator waitFor).
2. Playwright runner mark test as timeout — internal state berubah ke "test ended".
3. SEMUA pending awaits dapat error "Test ended."
4. MA step softAssert callback receive error → catch → record finding (severity major).
5. Loop continue ke Essay step → isClosed() gate trigger → throw SkipScenarioError.
6. Catch + return eksekusi. Console log fire. Finally fire (ctx.close swallow).
7. Test cleanup → Playwright assign final status = "Failed (timeout)".

**Status final: FAIL** karena timeout sudah ke-trigger di step (1), bukan karena ada unhandled exception. **Tidak ada cara catch ini di test body** — timeout adalah runner-level event.

### Confirmation dari Playwright source / docs

- [Test timeouts (Playwright docs)](https://playwright.dev/docs/test-timeouts) — "Test timeout is the maximum time a test is allowed to run, including hooks. Default 30s, configurable via `testInfo.setTimeout()`."
- Playwright doesn't have an API to "extend timeout post-hoc" or "ignore timeout once specific conditions met."
- Once timeout hits, test is unfailable to "pass" — all subsequent operations get `Test ended` injection.

### Sub-options re-evaluation (post-evidence)

| Original Sub-Option | Re-evaluation Verdict |
|---------------------|----------------------|
| (a) Cepetin SkipScenarioError emit | **PARSIALLY VALID** — catch sudah cepat, tapi MA step severity='major' tidak throw → loop continue → accumulator timing. Fix sebenarnya: **eskalasi MA/Essay severity ke 'critical' saat page-closed indicator**, atau guard semua page action di top dengan isClosed throw. |
| (b) Naikkan SCENARIO_TIMEOUT_MS | **TIDAK SOLVE ROOT CAUSE** — kalau page closed, semua page.check() akan retry sampai timeout baru. Cuma menunda. |
| (c) Wrap test() dalam try/catch | **NOT POSSIBLE** untuk timeout — runner-level event tidak catchable di test body. |
| (d) Switch serial → parallel | **NOT IDEAL** — kita butuh sequential untuk DB seed integrity. Tapi **partial-d**: split single `describe.serial` ke 10 separate `describe()` blocks → tetap sequential via `workers: 1`, tapi failure di 1 describe TIDAK halt describe lain ([sumber: Playwright Issue #15741 + community recommendation](https://github.com/microsoft/playwright/issues/15741)). |

---

## Cross-Reference Phase 315

Phase 315 = predecessor punya 10 test() blocks identik di same spec file. UAT Phase 315 (`.planning/phases/315-assessment-matrix-test/315-UAT.md` lines 50-67):

| Phase 315 Test | Status | Notes |
|----------------|--------|-------|
| Cold Start Smoke (S5) | PASS | Smoke run S5-only ber-grep, exit 0 |
| Continue-on-fail behavior | PASS (verbal) | "smoke run S5 menampilkan `[S5] Critical fail` lalu lanjut ke teardown" |
| Sentinel S8/S9/S10 | BLOCKED | "belum ter-exercise di any run karena S1 atau S5 cascade fail" |

**KUNCI Phase 315:** Phase 315 **TIDAK PERNAH** run full S1-S10 sukses. Smoke run hanya `--grep "Scenario 5"` (1 test). Sentinel S8-S10 BLOCKED dari awal. Phase 315 inherit limitasi yang sama dengan Phase 316.

**Beda setup Phase 315 vs 316:** Tidak ada. Spec file `assessment-matrix.spec.ts` identical (sudah diverifikasi line numbers per spec.ts:64 `test.describe.configure({ mode: 'serial' })`).

**Implikasi:** GAP-316-2 BUKAN regression dari Phase 316. Ini latent infrastructure limitation yang Phase 315 sengaja defer dengan "Acknowledged Gaps" (315-UAT.md:82-86). Phase 316 surface limitasi via attempted full-run.

---

## Sub-Options Trade-off Matrix

Dimensi: Complexity (LOC + invasive level), Risk, Side-effect, Alignment dengan Phase 316 surgical principle, Time-to-implement.

| # | Option | Complexity | LOC Delta | Risk | Side-Effect | Surgical Alignment | Time |
|---|--------|-----------|-----------|------|-------------|-------------------|------|
| **(a-revised)** | Eskalasi MA/Essay severity → 'critical' saat error contains "closed"/"Test ended". Atau: tambah isClosed() check di softAssert catch handler — kalau page closed post-error, throw SkipScenarioError langsung. | LOW | ~10-15 LOC di matrixReport.ts softAssert catch + 0 di examMatrix.ts | LOW — minor change di softAssert catch path, sudah ada Phase 316 SkipScenarioError re-throw branch yang model | MA/Essay finding yang bukan page-closed tetap recorded (severity major preserved). Cuma page-closed cascade yang dipromote ke critical (intent matches) | HIGH — minimal blast radius, satu file, follow Pattern 5 mitigation 316-RESEARCH.md:400-407 | 30 min |
| (b) | Naikkan SCENARIO_TIMEOUT_MS dari 240s ke 480s/900s. | TRIVIAL | 1 LOC di spec.ts:71 | HIGH — root cause page closed unfixed → next iteration tetap akan hit timeout baru. False sense of progress. | Run wallclock 2-4× lebih lama untuk 10 scenarios (current ~20min → 40-80min). Dev iteration cost. | LOW — patch only, tidak resolve fundamental issue | 5 min |
| (c) | Wrap test() body dalam try/catch handler runner-level. | IMPOSSIBLE | N/A | N/A | N/A — Playwright runner timeout TIDAK catchable di test body level (runner-internal event). Trying confuses test.fail() S10 annotation semantic. | INVALID | — |
| **(d-partial)** | Split single `test.describe.configure({mode:'serial'})` ke 10 separate `test.describe()` blocks (NO serial mode). Tiap describe punya 1 test(). `workers: 1` + `fullyParallel: false` di config jaga sequential execution. Failure di describe-1 TIDAK halt describe-2..10. | MEDIUM | ~20 LOC restructure di spec.ts (wrap setiap test() dengan describe()). 0 di helpers. | LOW — restructure pure organizational. Tidak ubah helper, tidak ubah collector. | Tiap describe block fresh fixture (browser instance NEW di tiap test) — slight overhead. Sentinel S10 `test.fail()` annotation tetap valid (`test.fail()` di body, scope tidak terpengaruh describe boundary). | HIGH — Address ROOT CAUSE untuk halt-on-first-fail tanpa touching helper or collector | 1-2 jam |
| (d-full) | Switch ke `fullyParallel: true` + `workers: N`. | HIGH | 1 LOC config + audit collector | HIGH — global setup DB seed mismatch kalau parallel writes ke same SQL Server snapshot. matrixReport collector sudah multi-worker safe (file-system NDJSON per worker) — tapi globalSetup BACKUP/RESTORE NOT parallel-safe. | Test infra redesign — phase 316 scope explode. | LOW | 4-8 jam |

**Hybrid solusi:**

| Hybrid | Description | When to use |
|--------|-------------|-------------|
| **(a-revised) + (d-partial)** | Fix root cause page-closed cascade (catch sooner) + restructure spec (isolate failure per scenario). Defense in depth. | **RECOMMENDED — Plan 03 default path** |
| (a-revised) + (b) | Fix catch sooner + raise timeout sebagai safety margin. | Tidak perlu — kalau (a-revised) works, (b) redundant. |
| (d-partial) + (b) | Restructure + raise timeout. | Acceptable kalau (a-revised) lebih risky dari perkiraan. |

---

## Recommended Sub-Option

### Winner: **Hybrid (a-revised) + (d-partial)**

**Rationale:**

1. **(a-revised)** addresses root cause langsung — `page.check: Test ended` di MA step → softAssert catch handler detect closed-state → throw SkipScenarioError langsung (skip severity='major' continue path). Helper tidak perlu diubah (current isClosed() gate di Essay step tetap valid sebagai second-line defense).

2. **(d-partial)** addresses Playwright-level halt-on-first-fail — even if (a-revised) gagal di scenario tertentu (e.g., real bug page closed yang takeExam tidak catch dengan timeout > 240s), tiap describe block independent → S2-S10 tetap run.

3. **Tidak melanggar surgical principle Phase 316** — kedua change minimal:
   - (a-revised) = ~10-15 LOC di matrixReport.ts softAssert catch.
   - (d-partial) = ~20 LOC mechanical restructure di spec.ts (wrap test ke describe).

4. **Singleton collector race NOT BLOCKER** — collector sudah file-system-backed (NDJSON per worker) per matrixReport.ts:42-185. `workers: 1` mempertahankan singleton-in-process; multiple describe TIDAK trigger multiple workers (Playwright spawn worker per **file**, bukan per describe).

5. **`test.fail()` annotation S10 tetap honored** — `test.fail()` adalah test-level annotation; scope describe boundary tidak relevan. Per Playwright docs annotation `test.fail()` valid di any describe context.

### 2nd-Place: **(a-revised) alone**

Kalau time-pressure / risk-aversion tinggi:
- Implement (a-revised) saja → fix page-closed cascade timing.
- Test full run S1-S10 — kalau cascade catch works fast (<60s), S1 won't hit 240s timeout → S2-S10 tetap run di serial mode.
- (d-partial) defer ke phase berikutnya kalau (a-revised) cukup.

**Risiko 2nd-place:** Kalau ada scenario lain yang surface novel page-closed pattern, halt-on-first-fail still possible.

### 3rd-Place (rejected): **(b) — raise timeout**

Selalu inferior — patch saja, tidak fix root cause.

---

## Implementation Sketch (Plan 03 scope preview)

### Scope summary

| Plan 03 sub-task | Deliverable | Files modified | LOC delta |
|------------------|-------------|----------------|-----------|
| 1. GAP-316-1 fix — waitForURL regex | Helper accept `/CMP/(Results|ExamSummary)/{id}` | `tests/e2e/helpers/examMatrix.ts:182` | 1 LOC (regex change) |
| 2. GAP-316-2 (a-revised) — page-closed cascade catch | softAssert handler detect closed state post-error → re-throw SkipScenarioError untuk severity='major' yang error contain "closed"/"Test ended" | `tests/e2e/helpers/matrixReport.ts:206-247` | ~10-15 LOC insert di catch block |
| 3. GAP-316-2 (d-partial) — describe restructure | Wrap 10 test() ke 10 test.describe() block terpisah. Remove `test.describe.configure({mode:'serial'})`. | `tests/e2e/assessment-matrix.spec.ts:64, 180-409` | ~30 LOC structural (10× wrap) |
| 4. Re-run validation | Quick S5-isolated + Full S1-S10 + UAT update | logs + `316-UAT.md` re-verdict | N/A |

**Total estimated:** ~50-60 LOC delta, 3 files modified, ~1-2 jam dev + 30-60 min full run validation.

### Implementation detail per sub-task

#### Sub-task 1 — GAP-316-1 (waitForURL regex)

```typescript
// File: tests/e2e/helpers/examMatrix.ts line 182
// BEFORE:
await Promise.all([
  page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
  page.click('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)'),
]);

// AFTER (D-02 code inspection confirms ExamSummary valid path):
await Promise.all([
  page.waitForURL(/\/CMP\/(Results|ExamSummary)\/\d+/, { timeout: 15_000 }),
  page.click('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)'),
]);
```

**Verification:** Re-run quick `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` → expect S5 submit-exam tidak lagi punya finding "page.waitForURL: Test ended" untuk path `/Results/`. Helper now tolerant terhadap incomplete-answers branch.

#### Sub-task 2 — GAP-316-2 (a-revised softAssert page-closed detection)

```typescript
// File: tests/e2e/helpers/matrixReport.ts, di softAssert catch block (line 206-247)
// INSERT setelah existing SkipScenarioError re-throw branch (line 211-213):

if (e instanceof SkipScenarioError) {
  throw e;
}

// === Phase 316 GAP-316-2 fix — page-closed cascade catch ===
// Saat error contains "closed"/"Test ended", page sudah dead — sisanya step
// scenario akan retry-then-timeout (accumulator → 240s hit). Promote langsung
// ke SkipScenarioError untuk skip sisa step (analog dengan severity='critical'
// behavior bawah, tapi tanpa wait page.screenshot defensive capture yang juga
// akan hang).
const errMsg = (e as { message?: string })?.message ?? String(e);
const isPageClosed = /closed|Test ended/i.test(errMsg) || ctx.page.isClosed();
if (isPageClosed && ctx.severity === 'major') {
  // Record finding pertama (kasih konteks ke report) tapi escalate skip.
  await collector.record({
    scenarioId: ctx.scenario.id,
    scenarioTitle: ctx.scenario.title,
    step: ctx.step,
    expected,
    actual: errMsg,
    screenshotPath: undefined,  // page closed — skip screenshot
    severity: 'critical',  // promoted from major (page-closed cascade)
    isMeta: ctx.isMeta,
  });
  throw new SkipScenarioError(`page-closed cascade at ${ctx.step}: ${errMsg}`);
}

// === Rest of existing catch handler (unchanged) ===
const err = e as { message?: string };
const stepSlug = ctx.step.replace(/\s+/g, '-').replace(/[^a-zA-Z0-9-]/g, '');
// ... defensive screenshot + collector.record + critical throw
```

**Why this works:**
- Page-closed errors propagate fast (Playwright detect via `Target page... closed` immediately).
- Promote to SkipScenarioError → caller (runDiscoveryScenario) catch + return → scenario ends in seconds, not 240s.
- Subsequent scenarios (S2-S10) tetap run.

**Edge case:** Kalau MA step real bug page-closed (BUKAN cascade dari sub) → still skip → masuk report sebagai critical finding di MA step. Acceptable — report tetap punya signal.

#### Sub-task 3 — GAP-316-2 (d-partial describe restructure)

```typescript
// File: tests/e2e/assessment-matrix.spec.ts

// REMOVE:
// test.describe.configure({ mode: 'serial' });

// WRAP each test() in its own describe():
test.describe('Scenario 1: S1 Manual Mixed', () => {
  test('execute', async ({ browser }) => {
    test.setTimeout(SCENARIO_TIMEOUT_MS);
    await runDiscoveryScenario(browser, getScenario(1));
  });
});

test.describe('Scenario 2: S2 Online Mixed', () => {
  test('execute', async ({ browser }) => {
    test.setTimeout(SCENARIO_TIMEOUT_MS);
    await runDiscoveryScenario(browser, getScenario(2));
  });
});

// ... repeat untuk S3-S10
```

**Sequential preservation:** `playwright.config.ts:8 fullyParallel: false` + `projects[chromium].dependencies: ['setup']` jaga single-file = single-worker = sequential. Tanpa `test.describe.configure({mode:'serial'})`, **failure di describe-1 TIDAK trigger skip di describe-2..10** — masing-masing describe independent failure boundary.

**Verification:** Sentinel S10 `test.fail()` annotation tetap valid — annotation scope = test, bukan describe ([Playwright Annotations docs](https://playwright.dev/docs/test-annotations)).

#### Sub-task 4 — Re-run validation

```bash
# Step 1: Quick S5 + sanity
cd tests && npx playwright test assessment-matrix --grep "Scenario 5"
# Expect: exit 0, S5 either PASS (kalau happy path reach Results)
#         atau ada finding submit-exam dengan Actual mentioning ExamSummary URL match (regex fix valid).

# Step 2: Full run S1-S10
cd tests && npx playwright test assessment-matrix
# Expect: 10 tests run (TIDAK ada "did not run"). Beberapa fail (real bugs surface) OK.
#         Sentinel S8/S9 — meta finding ter-record kalau score mismatch.
#         Sentinel S10 — test.fail() satisfied (expected failure).
#         Exit code: 0 kalau S10 sukses fail-as-designed AND no unexpected pass.
```

### Acceptance criteria untuk Plan 03

| Criterion | How to verify |
|-----------|---------------|
| GAP-316-1 closed: S5 reach Results URL OR helper tolerant ExamSummary | Quick run S5 log → no `page.waitForURL: Test ended` finding |
| GAP-316-2 closed: full S1-S10 run TIDAK ada "did not run" | `npx playwright test assessment-matrix` output: "10 tests ran" (passed + failed = 10, did not run = 0) |
| GAP-316-3 closed: sentinel S8/S9/S10 ter-exercise | Report `## Meta-validation results` section non-empty untuk minimal S10 (S8/S9 muncul kalau ada score mismatch) |
| Surgical preserve: TypeScript compile clean | `cd tests && npx tsc --noEmit` exit 0 |
| Surgical preserve: helper signature unchanged | grep `takeExam(`, `runDiscoveryScenario(` — same arg list |

---

## Open Questions

### Q1 — `page.check()` retry timeout default

**Status:** ASSUMED — perlu verify

`page.check()` retry sampai locator actionable. Saat page closed, retry timeout = `actionTimeout` (kalau set di config) ATAU `timeout` global ATAU test-level setTimeout.

`playwright.config.ts:11` tidak set `actionTimeout` — kemungkinan retry sampai test timeout (240s). Kalau set `actionTimeout: 5_000`, page.check() throw cepat saat page closed → MA finding ter-record fast → loop continue → essay isClosed catch.

**Implikasi:** Sub-task 2 (a-revised) bisa di-augment dengan **add `actionTimeout: 10_000` di playwright.config.ts:11** sebagai 3rd-line defense:

```typescript
use: {
  baseURL: 'http://localhost:5277',
  actionTimeout: 10_000,  // Phase 316 GAP-316-2 — bound retry untuk page.{check,click,fill}
  screenshot: 'on',
  trace: 'on-first-retry',
  viewport: { width: 1280, height: 720 },
},
```

**Risiko:** Bisa menyebabkan test flake kalau ada legitimate slow render (SignalR negotiation, dll). Tradeoff: 10s per action vs 240s per test.

**Decision needed user:** Apakah add actionTimeout sebagai bagian Plan 03? **Recommendation: YES** — defense-in-depth, 1 LOC change, easy revert.

### Q2 — `test.describe()` boundary saat failure: confirm dengan dummy run

**Status:** ASSUMED — community claim "describe block independent failure" terverifikasi via [Issue #15741](https://github.com/microsoft/playwright/issues/15741) tapi belum verifikasi di codebase ini.

**Suggested verification (Wave 0 Plan 03):** Sebelum implement full restructure, lakukan 2-block proof:

```typescript
// Test scaffold:
test.describe('Block A', () => {
  test('always-fail', async () => { throw new Error('intentional'); });
});
test.describe('Block B', () => {
  test('should-run', async () => { console.log('B ran'); });
});
```

Run → expect "1 failed, 1 passed" (not "1 failed, 1 did not run"). Confirms describe boundary isolate failure.

### Q3 — Mode 'serial' di config global level vs describe level

**Status:** LOW confidence

`playwright.config.ts` punya `fullyParallel: false` (line 8). Spec punya `test.describe.configure({mode:'serial'})` (line 64). Setelah remove spec-level configure, apakah config-level `fullyParallel: false` masih trigger "serial within file"?

Per [Playwright docs Parallelism](https://playwright.dev/docs/test-parallel): `fullyParallel: false` (default) = "tests within same file run serially di 1 worker". Failure semantic = **NOT halt-on-first-fail** di default mode (per [Issue #18329](https://github.com/microsoft/playwright/issues/18329) "default mode tests can be in parallel mode by default … kalau `fullyParallel: false`, sequential tapi failure tidak halt"). **Konfirmasi via Wave 0 dummy run di Q2.**

### Q4 — Real cause page-closed di MA step S1 (q50002)

**Status:** OPEN — di luar scope Plan 03 (tetap di GAP-316-1 hipotesa: server-side submit redirect).

Why does page closed in MA step (q50002) sebelum sampai submit? Hipotesa:
- (i) Tier-1/Tier-2 timer auto-submit. Saat seed assessment punya `duration` pendek dan client-side timer expire mid-exam → auto trigger submit → page navigate ke ExamSummary. Subsequent MA tick fire saat page navigating → context close mid-action.
- (ii) MA SignalR `SaveMultipleAnswer` error → client handler navigate away.
- (iii) Random Kestrel transient error.

**Recommend defer ke phase berikutnya:** Fix Plan 03 INFRA (helper tolerant + cascade catch + describe restructure). Real bug investigation Q4 = matrix discovery output — let next phase iterate.

---

## Validation Architecture Update (untuk Plan 03)

### Test Framework

Same as Phase 316 — Playwright 1.4x, config `tests/playwright.config.ts`.

### Phase 03 Requirements → Test Map

| Anchor ID | Behavior | Test Type | Automated Command | Expected |
|-----------|----------|-----------|-------------------|----------|
| GAP-316-1 | S5 submit-exam reach Results OR ExamSummary | E2E (Playwright matrix) | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` | exit 0, no submit-exam finding `Test ended` |
| GAP-316-2 | Full S1-S10 run, all 10 exercise | E2E (full run) | `cd tests && npx playwright test assessment-matrix` | "0 did not run", 10 tests executed |
| GAP-316-2 (a-revised) | Page-closed cascade catch fast (<60s per scenario) | E2E + log timing | Inspect log: `Scenario N (X.Ym)` — X < 1 menit per scenario yang fail | Per-scenario wallclock <60s untuk fail scenarios |
| GAP-316-3 | Sentinel S8/S9/S10 ter-exercise | E2E + report | Report `## Meta-validation results` non-empty for at least S10 | Section punya sentinel-collector-check finding |
| Surgical | No regression Phase 316 Plan 01/02 changes | TS compile + grep | `cd tests && npx tsc --noEmit; grep -c "Promise.all" examMatrix.ts; grep -c "isClosed" examMatrix.ts` | TS exit 0; Promise.all count ≥1; isClosed count ≥3 |

### Wave 0 Gaps for Plan 03

- [ ] **Verify describe-boundary failure isolation** — run 2-block dummy spec (Q2 above) before commit to (d-partial) restructure.
- [ ] **Verify actionTimeout impact** — kalau add `actionTimeout: 10_000`, run full S1-S10 baseline first to detect false-positive failures dari legitimate slow operations (SignalR, save indicator).

---

## Sources

### Primary (HIGH confidence)

- `/tmp/316-full-run-v2.log` — actual log S1 timeout 240s + console catch log evidence
- `/tmp/316-full-run.log` — pre-deviation log dengan stack trace `ctx.close()` throw (already mitigated by `fa9e4e86`)
- `docs/test-reports/2026-05-11-assessment-matrix.md` — final report 1 finding (ma-q50002)
- `tests/e2e/helpers/examMatrix.ts:103-188` — takeExam loop dengan isClosed gate inserted (line 115, 133, 154)
- `tests/e2e/helpers/matrixReport.ts:42-185` — file-system-backed collector (multi-worker safe)
- `tests/e2e/helpers/matrixReport.ts:199-247` — softAssert dengan SkipScenarioError re-throw branch (Pitfall 5 mitigation)
- `tests/e2e/assessment-matrix.spec.ts:64,128-172,180-409` — describe.serial + runDiscoveryScenario + 10 test() blocks
- `tests/playwright.config.ts` — timeout=60s default, expect timeout=10s, fullyParallel=false, retries=0
- `.planning/phases/315-assessment-matrix-test/315-UAT.md:50-86` — Phase 315 cross-reference (sentinel always blocked, full-run never demonstrated)
- `.planning/phases/316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso/316-UAT.md` + `316-02-SUMMARY.md` — Phase 316 Plan 02 outcome + gap documentation
- [Playwright Issue #18329](https://github.com/microsoft/playwright/issues/18329) — Feature request for serial-without-halt (closed not-planned)
- [Playwright Issue #17266](https://github.com/microsoft/playwright/issues/17266) — Similar feature request
- [Playwright Issue #16199](https://github.com/microsoft/playwright/issues/16199) — `--max-failures` for serial mode
- [Playwright Issue #18112](https://github.com/microsoft/playwright/issues/18112) — Should not skip tests sequentially
- [Playwright Issue #15741](https://github.com/microsoft/playwright/issues/15741) — Parallel two serial describes (community discussion)

### Secondary (MEDIUM confidence)

- [Playwright docs Test Timeouts](https://playwright.dev/docs/test-timeouts) — timeout semantics
- [Playwright docs Parallelism](https://playwright.dev/docs/test-parallel) — describe boundary + fullyParallel semantic
- [Playwright docs Annotations](https://playwright.dev/docs/test-annotations) — test.fail() scope
- [Playwright Issue #16119](https://github.com/microsoft/playwright/issues/16119) — Bug report on serial assertions
- [Configuring Playwright continue-on-failure (ray.run)](https://ray.run/discord-forum/threads/10498-how-to-continue-on-failure) — Community workaround

### Tertiary (LOW confidence — assumptions flagged)

- A1: `page.check()` retry timeout default = test-level timeout (240s) saat actionTimeout tidak di-set [ASSUMED based on Playwright API docs language; needs empirical confirm di Q1]
- A2: `test.describe()` boundary isolate failure di `fullyParallel: false` mode [VERIFIED via Issue #18329 community discussion + Issue #15741 user reports — but not by codebase dummy run yet; Wave 0 Plan 03 confirms]

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `page.check()` retry up to test-level timeout (240s) jika `actionTimeout` tidak set | Root cause timing accumulator | Kalau actionTimeout default 30s, root cause analysis less precise — tapi remediation tetap valid (catch sooner via cascade detection) |
| A2 | `test.describe()` block boundary mengisolasi failure di `fullyParallel: false` mode (no halt-on-first-fail across describes) | Sub-option (d-partial) | Kalau false, restructure tidak solve halt issue → fallback ke (a-revised) alone. Verify via Wave 0 dummy run. |
| A3 | Page-closed pattern selalu surface error string contain "closed" atau "Test ended" | softAssert cascade detect (a-revised) regex | Kalau ada variasi message tidak ter-match, detect miss → cascade hit timeout. Mitigate dengan broader regex atau pakai `ctx.page.isClosed()` check parallel. |
| A4 | matrixReport collector file-system-backed sudah aman multi-worker (NDJSON append atomic) | Singleton race concern | Plan 05 Phase 315 commit history menunjukkan ini sudah implementasi — verified via grep `appendFile.*workerFindingsFile`. Risk: LOW. |

## Recommendation Recap

**Plan 03 scope:** 3 sub-tasks total (1 surgical regex fix GAP-316-1 + 2 surgical infra changes GAP-316-2). Estimated 1-2 jam dev + 30-60 min validation runs.

**Order of operations:**
1. Sub-task 1 (regex GAP-316-1) — trivial, no dependency.
2. Sub-task 2 (a-revised softAssert cascade catch) — independent.
3. Sub-task 3 (d-partial describe restructure) — independent, can parallel dengan sub-task 2.
4. Optional: Add `actionTimeout: 10_000` di config sebagai 3rd defense (Q1).
5. Sub-task 4 validation: Quick S5 → Full S1-S10 → UAT update.

**Phase 316 close decision:**

Setelah Plan 03 sukses (5 D-16 UAT items PASS termasuk GAP-316-1/2/3 closed), Phase 316 ready close dengan:
- Plan 01 (helper hardening) — DONE merged
- Plan 02 (staged validation + UAT) — DONE merged, 3 carry-forward gaps documented
- Plan 03 (gap closure) — NEW, address all carry-forward
- 316-UAT.md re-verdict: 5 D-16 items PASS

Kalau Plan 03 partial (e.g., (a-revised) works tapi describe restructure ditunda), accept partial close + carry-forward minor gaps to phase 317.

---

*Phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso*
*Research GAP-316-2 + GAP-316-1 completed: 2026-05-11*
*Valid until: 2026-06-10 (30 days — stable Playwright API + test infra)*
