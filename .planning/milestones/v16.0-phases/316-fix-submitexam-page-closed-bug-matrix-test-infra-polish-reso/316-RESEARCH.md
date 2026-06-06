# Phase 316: Fix SubmitExam page-closed bug + matrix test infra polish - Research

**Researched:** 2026-05-11
**Domain:** Playwright E2E test helper hardening (race-tolerant submit pattern + defensive screenshot capture + sentinel verification)
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Submit-exam race fix**
- **D-01:** Pattern `await Promise.all([page.waitForURL(/CMP\/Results\/\d+/), page.click(submitSelector)])` di `examMatrix.ts:155-156`. Race-tolerant canonical Playwright pattern (sejalan Phase 313.1 helper).
- **D-02:** Server-side scope = **Quick server smoke** — manual curl/postman test ke `/CMP/SubmitExam/{id}` konfirmasi 302 redirect target `/CMP/Results/{id}` masih intact. Tidak full audit `CMPController.cs:1569-1717`.
- **D-03:** Selector strategy — pertahankan OR-selector `'#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)'`. Cuma bungkus dengan Promise.all.
- **D-04:** Regression test — re-run matrix S5 (MC-only) sebagai proxy. S5 reach `/CMP/Results/{id}` = fix valid. Tidak buat dedicated `submit-race.spec.ts`.

**Save-indicator hardening**
- **D-05:** Wait pattern saveIndicator — pertahankan `#saveIndicatorText` filter `/saved|tersimpan/i` timeout 5s. Sudah pass 7/8 UAT Phase 315.
- **D-06:** Page-closed handling — `page.isClosed()` check di setiap softAssert step. Closed mid-step → throw `SkipScenarioError` langsung (jangan retry, jangan cascade).
- **D-07:** SignalR readiness gate — pertahankan `window.assessmentHub.state === 'Connected'` timeout 10s.
- **D-08:** Helper scope = **Submit fix + page-closed detect only** — blast radius minimal. MC/MA/Essay save pattern dipertahankan utuh.

**Screenshot path strategy**
- **D-09:** softAssert defensive capture — `page.screenshot()` di try/catch + pre-check `page.isClosed()` sebelum capture. Page closed → skip custom screenshot (jangan throw, jangan retry).
- **D-10:** Path convention — tetap `test-results/matrix-s{id}-{step}.png`. Tidak adopt random suffix Playwright.
- **D-11:** Report renderer fallback — kalau custom path missing di disk, renderer cari Playwright auto-capture path (`test-results/{spec-dirname}/test-failed-{N}.png`) sebagai fallback.
- **D-12:** File edit = `matrixReport.ts` only. Tidak buat helper baru. Tidak ubah `matrixTypes.ts` kecuali wajib type addition.

**Sentinel + full-run re-verify**
- **D-13:** Validation staged 2 langkah — (a) S5 isolated run (`--grep "Scenario 5"`), (b) full 10-scenario run.
- **D-14:** Run command full = `npx playwright test assessment-matrix` tanpa filter. S10 `test.fail()` annotation → exit 0 saat body throw, exit non-0 saat body pass tanpa throw.
- **D-15:** Acceptance criteria:
  - S5 critical reach `/CMP/Results/{id}` redirect
  - S8 [META-AllCorrect] score 100/100
  - S9 [META-AllWrong] score 0/0
  - S10 [META-CollectorCheck] meta finding tercatat di report `## Meta-validation results`
  - Inter-scenario continue: S1 fail → S2-S10 tetap jalan
- **D-16:** UAT items terpisah di `316-UAT.md`: `fix-validate-S5`, `full-run-S1-S10`, `sentinel-meta-verify`, `continue-on-fail-E2E`, `screenshot-path-consistency`.

### Claude's Discretion
- Plan task breakdown (1 plan vs multi-plan) — let planner decide based on dependency analysis.
- Specific JSDoc + comment update di `examMatrix.ts` post-fix (reference Phase 316 fix di docstring).
- Apakah perlu update `tests/playwright-report/index.html` test baseline (kemungkinan auto-regen, tidak manual).

### Deferred Ideas (OUT OF SCOPE)

**Helper enhancements (future phase)**
- `helperMetrics` telemetry (per-step duration, retry count).
- `submitExamRaceSafe()` standalone function — refactor kalau Phase 31x butuh submit isolated.
- Dedicated `submit-race.spec.ts` minimal repro — current decision pakai matrix S5 sebagai proxy.

**App enhancements (future phase)**
- Expose `window.assessmentHub.lastSavedAt` timestamp untuk stronger gate.
- Refactor `#reviewSubmitBtn` + `#mobileSubmitBtn` jadi single canonical submit selector.

</user_constraints>

<phase_requirements>
## Phase Requirements

Phase 316 TIDAK punya REQ ID baru di REQUIREMENTS.md (per ROADMAP line 372 `Requirements: TBD`). Scope di-anchor ke `315-UAT.md` lines 82-86 Acknowledged Gaps:

| Anchor ID | Description (from 315-UAT.md) | Research Support |
|-----------|-------------------------------|------------------|
| GAP-315-1 | Sentinel S8/S9/S10 verification deferred — Layer 2/3 meta-validation blocked by real bug submit-exam page-closed cascade | § Sentinel + test.fail() semantics (verifies exit-code contract); § Continue-on-fail E2E validation |
| GAP-315-2 | Screenshot path mismatch — softAssert custom path vs Playwright auto-capture path | § Screenshot defensive capture (try/catch + fallback rendering) |
| GAP-315-3 | Full inter-scenario continue-on-fail belum demonstrated E2E (10-scenario run) | § Continue-on-fail E2E validation (test.describe.configure serial mode + SkipScenarioError catch boundary) |

Acceptance per D-15 → 5 verifiable items mapped 1:1 ke D-16 UAT list.

</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Develop Workflow** — Lokal verify (`dotnet build` + `dotnet run` + `localhost:5277` + Playwright) → commit/push → **Team IT** promote ke Dev/Prod. Jangan edit kode/DB di server. Phase 316 = test-helper change only, no app code/DB schema impact.
- **Seed Workflow** — Klasifikasi seed sebelum buat. Snapshot DB lokal pre-test. Catat di `docs/SEED_JOURNAL.md`. Restore post-test, tandai `cleaned`. Phase 316 re-use existing matrix seed infra (Phase 315 `globalSetup` BACKUP + RESTORE flow); tidak butuh seed baru.
- **Bahasa Indonesia** — User-facing output dan kontent UAT/report harus Bahasa Indonesia.

## Domain Summary

Phase 316 = surgical fix untuk test infrastructure regression yang muncul di smoke run Phase 315 (2026-05-11T06:14:36Z). Dua perubahan helper + satu screenshot fallback strategy:

1. **`examMatrix.ts:155-156`** — Wrap `page.click(submitSelector)` di `Promise.all([waitForURL, click])` untuk eliminate race where submit-triggered redirect closes browser context sebelum click promise resolve.
2. **`examMatrix.ts:96-148`** — Insert `page.isClosed()` gate di setiap softAssert MC/MA/Essay step → throw SkipScenarioError saat page closed mid-loop (prevent cascade-fail noise di report).
3. **`matrixReport.ts:198-227`** — Defensive `page.screenshot()` di try/catch dengan `page.isClosed()` pre-check; report renderer fallback ke Playwright auto-capture path saat custom path missing.

**Evidence dari smoke run** (`docs/test-reports/2026-05-11-assessment-matrix.md`):
- S5 critical finding: `page.click: Target page, context or browser has been closed` di step `submit-exam`.
- S5 cascade major: `page.check: Test ended` di step `mc-q50027` (langkah sesudah submit context already closed).
- Auto-screenshot Playwright works (path: `test-results/assessment-matrix-Scenario-30ab3-.../test-failed-{1,2,3}.png`); custom-path silent-fail.

**Out of scope:** Server-side SubmitExam refactor, schema migration, dedicated submit-race spec, helper extract function, `helperMetrics` telemetry.

**Primary recommendation:** Implementasi single-plan (atomik) — 3 perubahan saling tergantung (Promise.all fix prerequisite untuk sentinel verifiable; isClosed gate prerequisite untuk screenshot defensive; screenshot fallback prerequisite untuk report renderer integrity). Sequential execution: fix submit race → re-run S5 → page-closed gate → screenshot defensive → re-run full S1-S10 → verify sentinel + continue-on-fail.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Submit-exam navigation race fix | Test helper (`tests/e2e/helpers/examMatrix.ts`) | — | Race ada di Playwright runner side (click vs navigate timing); server-side redirect 302 already correct per smoke run hypothesis |
| Page-closed defensive gate | Test helper (`tests/e2e/helpers/examMatrix.ts`) | Test report (`matrixReport.ts` SkipScenarioError path) | Detect runtime context state; record finding ringkas tanpa cascade noise |
| Screenshot defensive capture | Test report (`tests/e2e/helpers/matrixReport.ts`) | Playwright config (`screenshot: 'on'` auto-capture sebagai fallback source) | softAssert kontrol custom path; Playwright owns auto-capture path |
| Report renderer fallback path | Test report (`tests/e2e/helpers/matrixReport.ts`) | — | Markdown render-time disk check; choose custom-path-if-exists else auto-capture |
| Server 302 redirect verification | Manual quick smoke (curl/dev tools) | — | Confirms `Controllers/CMPController.cs:1569+` SubmitExam returns 302 to `/CMP/Results/{id}`; no code change |
| Sentinel S8/S9/S10 expected behavior | Test spec (`assessment-matrix.spec.ts:223-404`) | Test report (collector isMeta=true section) | Spec body wired correctly per smoke verdict; verify by running post-fix |

## Promise.all submit race fix

### Canonical pattern (verified)

Per Playwright docs: setup wait BEFORE click triggers navigation. `Promise.all` ensures `waitForURL` listener is established before click executes. Reference: `tests/e2e/helpers/exam313.ts:39, 61-66, 86-89, 107` Phase 313.1 sudah pakai pola sama (`page.waitForURL(/\/CMP\/(StartExam|ExamSummary)\/\d+/, { timeout: 10_000 })` setelah click). [VERIFIED: Playwright docs page.waitForURL]

### Current bug (cited)

`tests/e2e/helpers/examMatrix.ts:151-159`:
```typescript
await softAssert(
  { scenario: cfg, step: 'submit-exam', severity: 'critical', page },
  async () => {
    await page.click('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)');
    await page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 });
  },
  'SubmitExam redirects to /CMP/Results/{id}'
);
```

**Bug:** `page.click()` resolve AFTER browser fires navigate. Server returns 302 fast → context destroy mid-click → click promise reject `Target page, context or browser has been closed` BEFORE waitForURL has chance to register navigation.

### Fix (per D-01)

```typescript
await softAssert(
  { scenario: cfg, step: 'submit-exam', severity: 'critical', page },
  async () => {
    await Promise.all([
      page.waitForURL(/\/CMP\/Results\/\d+/, { timeout: 15_000 }),
      page.click('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)'),
    ]);
  },
  'SubmitExam redirects to /CMP/Results/{id}'
);
```

**Why this works:** `waitForURL` arms listener synchronously sebelum `click` fire navigate. Order matters: kalau `click` di Promise.all index 0, listener BELUM ada saat navigate trigger → same bug. Per Playwright API contract `waitForURL` returns Promise that subscribes immediately on call. [VERIFIED: Playwright docs]

### Edge cases

| Case | Behavior | Mitigation |
|------|----------|-----------|
| Mobile path (`#mobileSubmitBtn` via `document.getElementById('examForm').submit()`) per `Views/CMP/StartExam.cshtml:251` | Native form submit, same 302 path | OR-selector di D-03 cover both; Promise.all wrap works untuk both |
| Network idle race | `waitForURL` cuma cek URL change, tidak tunggu networkidle | Sufficient — Results page render server-side; subsequent assertions di `verifyResultPage` handle render |
| Submit blocked oleh server (Tier-1/Tier-2 reject) | Redirect ke `/CMP/ExamSummary/{id}` bukan `/CMP/Results/{id}` | `waitForURL` regex `/\/CMP\/Results\/\d+/` will timeout 15s → softAssert critical → SkipScenarioError → continue scenario S6+ |
| Timer expired mid-submit | Same as above; Tier-2 grace path may still ACCEPT | Acceptable false-negative — matrix S5 isn't testing timer paths (Phase 313 territory) |

### Cited file:line for fix target
- `tests/e2e/helpers/examMatrix.ts:155-156` — replace 2 statements dengan single Promise.all
- `tests/e2e/helpers/exam313.ts:106-108` — reference precedent (Phase 313.1 `assertSubmitSuccess` race-tolerant)

## page.isClosed() defensive gate

### Semantics (verified)

`page.isClosed()` adalah **synchronous boolean check** — return immediately tanpa await. Tidak ada race protection inherent: page CAN close between `isClosed()` returning false dan next action firing. [VERIFIED: Playwright docs page.isClosed]

### Placement strategy (per D-06)

Gate placement = **awal setiap softAssert callback body** (sebelum any page action). Pattern:

```typescript
await softAssert(
  { scenario: cfg, step: `mc-q${q.id}`, severity: 'major', page },
  async () => {
    if (page.isClosed()) {
      throw new SkipScenarioError(`page closed before mc-q${q.id} step — cascade abort`);
    }
    await page.check(...);
    await page.locator(...).waitFor(...);
  },
  `MC q${q.id} optionId=${optId} saved`
);
```

**Why throw SkipScenarioError langsung (not let softAssert turn into 'critical'):**
- `severity='major'` di MC/MA/Essay step → softAssert tidak throw; record finding + continue. Tapi continue ke step berikut juga akan fail same way → 3-5 cascade findings per scenario yang sama (smoke run evidence: S5 punya 2 findings = critical submit + cascade major mc).
- Throw SkipScenarioError explicit dari helper → bypass softAssert severity logic → caller catch di `runDiscoveryScenario` (spec line 154-159) → skip sisa step scenario → report cleaner (1 finding per real bug).

### Race-window acceptance

Antara `page.isClosed()` check dan `page.check()` ada race window ~microseconds. Acceptable risk karena:
1. Page closure trigger biasanya = previous step (submit-exam) yang sudah tercatat sebagai critical finding. Cascade detect-and-abort sufficient.
2. Tidak ada strict gate possible tanpa wrap setiap call dalam try/catch yang re-check (over-engineering, scope D-08 reject).

### Cited file:line for fix target
- `tests/e2e/helpers/examMatrix.ts:96-148` — insert isClosed gate di awal callback MC (line 104), MA (line 118), Essay (line 135)
- `tests/e2e/helpers/matrixReport.ts:30-35` — `SkipScenarioError` class sudah ada (re-use)

## Screenshot defensive capture

### Current bug (cited)

`tests/e2e/helpers/matrixReport.ts:208-209`:
```typescript
const screenshotPath = `test-results/matrix-s${ctx.scenario.id}-${stepSlug}.png`;
await ctx.page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});
```

`page.screenshot()` saat page closed throw → swallowed by `.catch(() => {})` → finding di-record dengan `screenshotPath` field menunjuk ke file yang TIDAK PERNAH DITULIS. Report renderer (line 282-284) blindly emit path → dead link di markdown.

### Playwright auto-capture context (verified)

`tests/playwright.config.ts:13` set `screenshot: 'on'` → Playwright auto-capture screenshot setiap step + test-end termasuk failure case. Path pattern: `test-results/{spec-dirname}/test-failed-{N}.png` per smoke run evidence di `315-UAT.md:24`. Directory `tests/test-results/` (per `tests/e2e/helpers/matrixReport.ts:42` `FINDINGS_DIR = resolve(__dirname, '..', '..', 'test-results')`). [VERIFIED: playwright.config.ts:13]

### Fix (per D-09 + D-11)

**Layer 1 — softAssert defensive capture (matrixReport.ts:198-227):**
```typescript
let screenshotPath: string | undefined;
const stepSlug = ctx.step.replace(/\s+/g, '-').replace(/[^a-zA-Z0-9-]/g, '');
const candidatePath = `test-results/matrix-s${ctx.scenario.id}-${stepSlug}.png`;
if (!ctx.page.isClosed()) {
  try {
    await ctx.page.screenshot({ path: candidatePath, fullPage: true });
    screenshotPath = candidatePath;  // only set kalau write sukses
  } catch (screenErr) {
    // Page may have closed between check dan screenshot. Skip silent — fallback path
    // di-resolve di render time (renderFinding).
  }
}
// screenshotPath di-pass undefined → renderFinding fallback ke auto-capture discovery.
```

**Layer 2 — report renderer fallback (matrixReport.ts:274-287 `renderFinding`):**

Saat `f.screenshotPath` undefined OR file missing di disk, glob scan `test-results/assessment-matrix-Scenario-*/test-failed-*.png` filter by scenarioId hint (matched via path containing scenario title slug) → emit first match. Implementation:
```typescript
function resolveScreenshotPath(f: Finding): string | undefined {
  if (f.screenshotPath && existsSync(resolve(f.screenshotPath))) {
    return f.screenshotPath;  // custom path exists
  }
  // Fallback — scan Playwright auto-capture dir
  const candidates = readdirSync(FINDINGS_DIR, { withFileTypes: true })
    .filter((d) => d.isDirectory() && d.name.startsWith('assessment-matrix-Scenario-'))
    .flatMap((d) => readdirSync(join(FINDINGS_DIR, d.name))
      .filter((n) => n.startsWith('test-failed-'))
      .map((n) => `test-results/${d.name}/${n}`));
  return candidates[0];  // best-effort; not scenario-correlated
}
```

**Caveat (assumed):** Playwright auto-capture directory naming `assessment-matrix-Scenario-{N}-...` di-cite di 315-UAT.md tapi exact slug derivation belum verified untuk all 10 scenarios. Saat implementasi, log directory listing post-run untuk confirm naming convention. [ASSUMED]

### Cited file:line for fix target
- `tests/e2e/helpers/matrixReport.ts:198-227` — softAssert defensive capture
- `tests/e2e/helpers/matrixReport.ts:274-287` — renderFinding fallback
- `tests/playwright.config.ts:13` — `screenshot: 'on'` confirms auto-capture available

## Sentinel + test.fail() semantics

### S10 expected behavior (verified spec)

`tests/e2e/assessment-matrix.spec.ts:370-404` — `test('Scenario 10: META-CollectorCheck Sentinel ...', async ({ browser }) => { test.fail(); ... })`:
- `test.fail()` di body = inner form annotation. Playwright treat test sebagai "expected to fail".
- Body sengaja throw via `softAssert(severity='critical')` → SkipScenarioError → test fails → exit code 0 (expected failure satisfied).
- Kalau body pass tanpa throw (collector swallow finding silently) → Playwright treat "unexpected pass" → exit code non-0 (collector rusak terdeteksi). [VERIFIED: Playwright docs test.fail]

### S8/S9 expected behavior (verified spec)

`assessment-matrix.spec.ts:223-275` (S8) dan `:277-360` (S9):
- S8: peserta1 jawab semua benar (no sabotage) + HC grade penuh → `softAssert` di `:246-264` cek `scoreText` match `/100|full|sempurna/i`. Pass → tidak record finding. Mismatch → record meta finding (`isMeta=true`).
- S9: peserta1 jawab semua salah (clone cfg `correctOptionIds` → wrong option, sabotageOneAnswer Essay → empty) + HC grade `scoreValue=0` → softAssert cek `scoreText` match `/\b0\b|nol|zero/i`. Same pass/fail mechanism.

### Collector verification semantics

Sentinel S10 = self-test untuk collector integrity. Failure modes:

| Failure Mode | Exit Code | Detect How |
|--------------|-----------|------------|
| Collector + softAssert OK (S10 throws as designed) | 0 | Report has `## Meta-validation results` section non-empty with S10 finding |
| Collector swallow finding silently (broken) | non-0 | Playwright report "unexpected pass" untuk S10 |
| softAssert tidak throw saat critical (broken) | 0 (tapi report kosong di meta section) | Manual check: meta section empty padahal S10 ran |

**Acceptance per D-15:** Report harus contain 1 meta finding di section `## Meta-validation results` dengan step `sentinel-collector-check`. Exit code 0.

### Cited file:line for verification
- `tests/e2e/assessment-matrix.spec.ts:362-369` — comment block doc test.fail() rationale
- `tests/e2e/assessment-matrix.spec.ts:371` — `test.fail();` inner form
- `tests/e2e/helpers/matrixReport.ts:164-165` — `discovery vs meta` filter via `isMeta` flag

## Continue-on-fail E2E validation

### How softAssert + SkipScenarioError isolate per-scenario

`tests/e2e/assessment-matrix.spec.ts:154-159` catch boundary:
```typescript
} catch (e) {
  if (e instanceof SkipScenarioError) {
    console.log(`[S${cfg.id}] Critical fail — skip sisa step scenario: ${e.message}`);
    return;  // exit test body normally — Playwright treat test sebagai PASSED
  }
  throw e;
}
```

**Mechanism:**
1. softAssert critical → throw SkipScenarioError
2. `runDiscoveryScenario` catch SkipScenarioError → `return` normal (no re-throw)
3. Playwright test() block sees normal completion → test passes (zero unexpected error)
4. `test.describe.configure({ mode: 'serial' })` (spec line 64) sequential next test() starts
5. Next test() isolasi via fresh `browser.newContext()` (line 129-134) — context dari prev test sudah closed in finally block (line 162-166)

**Result:** S1 fail tidak halt S2-S10. Verified mechanism di smoke run S5-only (S5 fail-then-clean teardown). E2E demonstration butuh full 10-scenario run post submit-race fix (per D-13 staged validation).

### Validation gate untuk E2E demonstration

Post Phase 316 implementation, run `npx playwright test assessment-matrix` (per D-14). Expected report shape:
- Summary line `## Summary` shows aggregated total (e.g., 0 critical kalau bug fixed, atau N critical kalau real bugs surface).
- Report har ≥10 scenarios processed (visible via collector findings OR clean execution log per scenario).
- S10 = expected failure (test.fail satisfied).
- Exit code 0 saat sentinel S10 throw as designed AND no real bugs.

### Cited file:line for verification
- `tests/e2e/assessment-matrix.spec.ts:64` — `test.describe.configure({ mode: 'serial' })`
- `tests/e2e/assessment-matrix.spec.ts:128-167` — `runDiscoveryScenario` orchestrator + SkipScenarioError catch
- `tests/e2e/helpers/matrixReport.ts:222-223` — SkipScenarioError throw site

## Server smoke 302 verify

### Pattern (per D-02)

Manual smoke test sebelum re-run Playwright — confirm server-side SubmitExam still returns 302 ke `/CMP/Results/{id}`. **Tidak full audit Controller code.**

**Approach options:**

1. **PowerShell (Windows native, recommended):**
```powershell
# Login dulu untuk dapat session cookie (manual via browser at http://localhost:5277, copy cookie)
$cookie = ".AspNetCore.Identity.Application=<copy-from-browser>"
$sessionId = 9005  # contoh — pakai sessionId yang current in-flight
$resp = Invoke-WebRequest -Uri "http://localhost:5277/CMP/SubmitExam/$sessionId" `
  -Method Post -Headers @{ "Cookie" = $cookie } -MaximumRedirection 0 -SkipHttpErrorCheck
Write-Host "Status: $($resp.StatusCode)"      # expect 302
Write-Host "Location: $($resp.Headers.Location)"  # expect /CMP/Results/$sessionId
```

2. **Browser DevTools (lowest friction):**
   - Login lokal sebagai `coachee` (rino.prasetyo@pertamina.com)
   - Open Network tab, filter `SubmitExam`
   - Submit exam manually via UI
   - Verify request returns 302 + Location header `/CMP/Results/{id}`

3. **Curl (kalau cygwin/wsl):**
```bash
curl -s -o /dev/null -w "%{http_code} %{redirect_url}\n" \
  -X POST -b "<cookie>" -L 0 \
  http://localhost:5277/CMP/SubmitExam/9005
# Expect: "302 http://localhost:5277/CMP/Results/9005"
```

### Auth context

`Controllers/CMPController.cs:1569+` SubmitExam requires authenticated coachee user owns the session (per `EnsureCanSubmitExamAsync` cited in 316-CONTEXT.md:80). Smoke verify pakai existing fixture account (`tests/helpers/accounts.ts` coachee). Tidak butuh seed baru — pakai matrix session yang current di .matrix-state.json (atau seed manual via UI start exam baru).

**Expected output**: HTTP 302 dengan `Location: /CMP/Results/{N}`. **Out of scope** kalau hasilnya beda (would indicate real server regression — promote ke phase terpisah dengan full audit `:1569-1717`).

## Pitfalls

### Pitfall 1: Promise.all argument ordering reverse
**What goes wrong:** `await Promise.all([page.click(...), page.waitForURL(...)])` — click first → fires navigate → context closes → waitForURL never gets chance.

**Why it happens:** Mental model assume "I want to click first, then wait" — sequential thinking maps to array order.

**How to avoid:** Always `waitForURL` first (listener arm), `click` last (action fire). Mnemonic: "armor before sword."

**Warning signs:** Test still fails dengan same `context closed` error padahal Promise.all already added.

### Pitfall 2: isClosed() check di luar softAssert callback
**What goes wrong:** Check page state di top-level helper sebelum loop → false → next iteration page closes mid-step → still cascade error.

**Why it happens:** Optimization instinct to "check once, reuse." Tapi page state shifts during sequential steps.

**How to avoid:** Check at top of EACH softAssert callback body, NOT outside. Per loop iteration cost is microseconds.

### Pitfall 3: Screenshot fallback path scenario correlation
**What goes wrong:** Playwright auto-capture dir = `assessment-matrix-Scenario-{slug-hash}-chromium/` — slug isn't directly mappable ke `scenario.id`. Fallback returns "best-effort first match" yang mungkin BUKAN scenario yang fail.

**Why it happens:** Playwright test name → directory slug via internal sanitization (not documented as stable API).

**How to avoid:** Accept best-effort fallback per D-11; OR (future enhancement) capture Playwright output dir mapping di globalSetup dan inject ke Finding metadata. Current phase scope: accept "fallback = some screenshot" rather than dead-link in markdown.

**Warning signs:** Report screenshot link doesn't visually match scenario context (e.g., S5 finding linked to S3 screenshot). Document manually di UAT kalau muncul.

### Pitfall 4: Windows path separator inconsistency
**What goes wrong:** `screenshotPath` string written as `test-results/matrix-s5-mc-q50027.png` (forward slash) tapi `existsSync` di Windows accept both — OK. Tapi kalau renderer pakai `path.join(FINDINGS_DIR, ...)` hasilnya backslash → markdown link berisi backslash → broken di GitHub viewer.

**How to avoid:** Selalu emit forward-slash di markdown report. Use `relative` path string concatenation, jangan `path.join` untuk emit value.

**Warning signs:** Report markdown screenshot link tidak click-through di IDE/browser preview.

### Pitfall 5: SkipScenarioError caught di softAssert outer try
**What goes wrong:** Insert `if (page.isClosed()) throw new SkipScenarioError(...)` di dalam softAssert callback body → try-catch di softAssert (line 203-205) catch ALL errors → record finding lagi → record dengan severity=major karena step bukan critical → tidak escalate ke caller.

**Why it happens:** softAssert wrap whole callback. Throwing dari dalam = caught.

**How to avoid:** SkipScenarioError throw HARUS di-recognize di softAssert catch handler — re-throw kalau instanceof SkipScenarioError tanpa record finding. ATAU: check isClosed di luar softAssert callback (di helper wrapper). Decision: amend softAssert handler (matrixReport.ts:205-227) untuk detect-and-rethrow SkipScenarioError tanpa record.

**Warning signs:** S5 still cascade-fail dengan 3-5 findings padahal fix applied.

### Pitfall 6: Playwright fullyParallel + test.describe serial
**What goes wrong:** `tests/playwright.config.ts:8` `fullyParallel: false` + `assessment-matrix.spec.ts:64` `mode: 'serial'` — redundant tapi safe. Concern: kalau ada developer ubah config jadi `fullyParallel: true` di phase lain, matrix test akan run parallel → collector file race condition (writes ke same `matrix-findings-w{idx}.json`).

**How to avoid:** Phase 316 don't touch config. Document di header comment `assessment-matrix.spec.ts` bahwa serial mode wajib dipertahankan.

## Validation Architecture

> Dimension 8 (Nyquist) — `workflow.nyquist_validation: true` di `.planning/config.json`. Section ini di-consume oleh `/gsd-plan-phase` Step 5.5 untuk generate VALIDATION.md.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright 1.4x (existing — same as Phase 315) |
| Config file | `tests/playwright.config.ts` |
| Quick run command | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` |
| Full suite command | `cd tests && npx playwright test assessment-matrix` |

### Phase Requirements → Test Map

| Anchor ID | Behavior | Test Type | Automated Command | Manual Steps |
|-----------|----------|-----------|-------------------|--------------|
| GAP-315-1.a | S5 submit-exam reach `/CMP/Results/{id}` post Promise.all fix | E2E (Playwright matrix) | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` | Confirm report shows S5 zero critical finding; URL Results visible di trace |
| GAP-315-1.b | S8 sentinel 100% score recorded | E2E (Playwright matrix) | `cd tests && npx playwright test assessment-matrix --grep "Scenario 8"` | Confirm report `## Meta-validation results` empty for S8 (no finding = match) OR meta finding documents score mismatch |
| GAP-315-1.c | S9 sentinel 0% score recorded | E2E (Playwright matrix) | `cd tests && npx playwright test assessment-matrix --grep "Scenario 9"` | Same as 1.b for S9 |
| GAP-315-1.d | S10 sentinel collector check exit-code contract | E2E + manual exit check | `cd tests && npx playwright test assessment-matrix --grep "Scenario 10"; echo "exit: $?"` | exit must be 0 (test.fail satisfied); report `## Meta-validation results` non-empty for sentinel-collector-check |
| GAP-315-2.a | Screenshot path resolves to real file di disk (no dead link) | Visual (markdown rendering check) | After full run: open `docs/test-reports/2026-05-11-assessment-matrix.md` | Click each `Screenshot:` link → image opens (Custom path OR auto-capture fallback) |
| GAP-315-2.b | Page-closed scenarios → no custom screenshot emitted (graceful skip) | Code review + smoke | Pre-fix smoke evidence; post-fix re-run S5 | Verify no "page closed" warning di Playwright stderr; report finding may omit screenshot field gracefully |
| GAP-315-3 | Full 10-scenario run completes without halt | E2E (full run) | `cd tests && npx playwright test assessment-matrix` | All 10 test() blocks execute; S1 fail does not block S2-S10; teardown runs |

### Sampling Rate
- **Per task commit:** S5-isolated run (`--grep "Scenario 5"`) — submit race fix proof
- **Per wave merge:** Full 10-scenario run — continue-on-fail + sentinel proof
- **Phase gate:** Full suite green + exit 0 + 5 D-16 UAT items pass

### Wave 0 Gaps
- [ ] Confirm Playwright auto-capture directory naming pattern via experimental run (assumption flagged di Pitfall 3 fallback). If naming differs from `assessment-matrix-Scenario-*`, adjust fallback glob.
- [ ] Confirm `screenshot: 'on'` config (playwright.config.ts:13) actually emits per-test screenshots di failure path (verified via 315-UAT smoke evidence — `test-failed-{1,2,3}.png` confirmed).
- [ ] No new test framework install needed — Playwright existing.

*(Existing test infrastructure covers all phase requirements — Wave 0 gaps = experimental validation, not framework gaps.)*

## Sources

### Primary (HIGH confidence)
- `tests/e2e/helpers/examMatrix.ts:1-277` — current helper code (edit target lines 96-160)
- `tests/e2e/helpers/matrixReport.ts:1-352` — collector + softAssert + renderer (edit target lines 198-287)
- `tests/e2e/helpers/exam313.ts:1-109` — race-tolerant pattern precedent (Phase 313.1)
- `tests/e2e/assessment-matrix.spec.ts:1-405` — spec consumer (no edit, verify only)
- `tests/playwright.config.ts:1-29` — `screenshot: 'on'`, `fullyParallel: false`
- `Views/CMP/StartExam.cshtml:71, 200-203, 251` — `#examForm`, `#reviewSubmitBtn`, `#mobileSubmitBtn`
- `docs/test-reports/2026-05-11-assessment-matrix.md` — smoke run evidence
- `.planning/phases/315-assessment-matrix-test/315-UAT.md:82-86` — scope anchor
- Playwright docs: page.waitForURL, page.isClosed, test.fail (verified via WebFetch)

### Secondary (MEDIUM confidence)
- 315-CONTEXT.md, 315-UAT.md — Phase 315 background decisions
- 316-CONTEXT.md — locked decisions D-01..D-16

### Tertiary (LOW confidence / ASSUMED)
- Playwright auto-capture directory naming pattern (`assessment-matrix-Scenario-*`) — observed di smoke run, NOT verified via Playwright source code. Fallback glob may need adjustment di Wave 0 actual run.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Playwright auto-capture dir slug pattern `assessment-matrix-Scenario-{N}-...` correlates to scenario id via test name slugify | Screenshot defensive capture (fallback rendering) | Fallback path emits unrelated screenshot → mild UX issue, fix during Wave 0 by logging actual dir listing |
| A2 | `page.isClosed()` returns false reliably when page is still attached to alive context | page.isClosed() defensive gate | False-positive throw → false SkipScenarioError → scenario skipped tanpa real bug. Low risk (Playwright docs say synchronous boolean) |
| A3 | Server-side `Controllers/CMPController.cs:1569+` SubmitExam still returns 302 to /CMP/Results/{id} (per smoke verdict hypothesis 2026-05-11) | Server smoke 302 verify | If false (server-side broken), Promise.all fix won't help → Phase 316 expanded scope to server audit. D-02 quick-smoke confirms before phase exit |
| A4 | `test.fail()` inner form (called di body) gives same expected-failure semantics as outer form `test.fail('name', async () => {})` | Sentinel + test.fail() semantics | If different, S10 may exit non-0 even when collector OK. Playwright docs don't explicitly contrast forms — flag at first full run |

## Open Questions (RESOLVED)

> **Status (2026-05-11):** Ketiga open questions sudah ter-encode resolusinya di plan tasks. Lihat inline RESOLVED marker per Q untuk reference plan/task/step yang implement resolusi.

1. **Playwright auto-capture directory naming exact pattern** — **RESOLVED** (Plan 02 Task 1 Step 4 Wave 0 verify)
   - What we know: smoke run produced `test-results/assessment-matrix-Scenario-30ab3-.../test-failed-{1,2,3}.png` (315-UAT.md:24)
   - What's unclear: how the `30ab3-...` slug suffix maps to `scenario.id` — appears to be Playwright-internal hash of test name
   - Recommendation: log directory listing at globalTeardown start; emit map in collector output; tune fallback glob accordingly. Acceptable for Phase 316 = best-effort fallback (any screenshot beats dead link).
   - **Resolution path:** Plan 02 Task 1 Step 4 (`ls tests/test-results/ | grep "^assessment-matrix-Scenario"`) runtime-verify A1 assumption + record actual naming pattern di `/tmp/316-validation-summary.txt`. Plan 01 Task 3 Bagian B `resolveScreenshotPath()` implement fallback dengan filter prefix `assessment-matrix-Scenario-`; Wave 0 verify confirm pattern atau flag perlu tune di future plan.

2. **SkipScenarioError caught oleh softAssert when thrown FROM dalam callback** — **RESOLVED** (Plan 01 Task 1 softAssert re-throw amendment)
   - What we know: softAssert try-catch (matrixReport.ts:203-205) catches all errors
   - What's unclear: if `if (page.isClosed()) throw new SkipScenarioError(...)` di dalam callback body → softAssert catch → record finding + (severity=major) return null → NO escalation
   - Recommendation: amend softAssert catch handler dengan `if (e instanceof SkipScenarioError) throw e;` re-throw branch BEFORE generic error handling. OR alternative: check isClosed di luar softAssert (di examMatrix helper wrapper). See Pitfall 5.
   - **Resolution path:** Plan 01 Task 1 implement re-throw branch (`if (e instanceof SkipScenarioError) throw e;`) di catch handler softAssert PALING ATAS, sebelum screenshot capture + collector.record. Acceptance criteria Task 1 enforce ordering (catch block: re-throw FIRST, lalu defensive screenshot, lalu collector.record). Plan 01 Task 3 Bagian A inject `if (page.isClosed()) throw new SkipScenarioError(...)` di MC/MA/Essay softAssert callback bodies — bekerja sequential dengan Task 1 re-throw branch (Task 1 commit dulu, lalu Task 3 gate aktif).

3. **Full 10-scenario runtime budget** — **RESOLVED** (Plan 02 Task 1 background-run note + Step 2)
   - What we know: per-scenario timeout 240s (spec line 71); 10 scenarios × ~120-180s actual = ~20-30 min full run
   - What's unclear: total wallclock + impact on developer workflow iteration
   - Recommendation: run di terminal background; result not required for Promise.all fix verification (S5-only run sufficient for D-13.a). Document expected wallclock di UAT 316.
   - **Resolution path:** Plan 02 Task 1 Step 2 (full run command) explicitly notes "Expected runtime ~20-30 min. Bisa run di background (`run_in_background: true` flag)." Wallclock actual akan didokumentasi di `316-UAT.md` Task 2 sebagai bagian dari `full-run-S1-S10` UAT item evidence. D-13.a (S5 isolated) cukup untuk validate Promise.all fix tanpa wait full run — staged validation D-13 design memang split quick/full untuk akomodasi runtime budget ini.


## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js + npm | Playwright test runner | Assumed ✓ (Phase 315 ran successfully) | — | None — blocks execution |
| Playwright | E2E test framework | ✓ (`tests/playwright.config.ts` exists, Phase 315 ran) | 1.4x | None — blocks execution |
| sqlcmd / SQL Server Express | Snapshot BACKUP/RESTORE (re-use Phase 315 globalSetup/teardown) | ✓ (verified Phase 315 6/6 runs cleaned) | — | None — blocks teardown |
| `dotnet` runtime | App server `http://localhost:5277` during exam phase | ✓ (project active) | — | None — blocks all exam steps |
| curl / PowerShell `Invoke-WebRequest` | Server smoke 302 verify (D-02) | ✓ (Windows native PowerShell) | — | DevTools Network tab manual |

**Missing dependencies with no fallback:** None — all infrastructure inherited dari Phase 315.

## Metadata

**Confidence breakdown:**
- Promise.all submit race fix: HIGH — pattern verified via Playwright docs + Phase 313.1 precedent at exam313.ts:39,61,86,107
- page.isClosed() gate: HIGH — synchronous boolean per docs; placement strategy aligns with D-06
- Screenshot defensive: MEDIUM — fallback path naming ASSUMED (flag A1), graceful degradation safe
- Sentinel S8/S9/S10 semantics: HIGH — spec verified, exit-code contract documented in 316-CONTEXT decisions
- Continue-on-fail: HIGH — mechanism verified at spec line 154-166 + serial mode line 64
- Server smoke: HIGH — manual procedure, low risk
- Validation Architecture: HIGH — Playwright + sqlcmd infra inherited dari Phase 315

**Research date:** 2026-05-11
**Valid until:** 2026-06-10 (30 days — stable test infra changes; no fast-moving dependencies)

---

*Phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso*
*Research completed: 2026-05-11*
