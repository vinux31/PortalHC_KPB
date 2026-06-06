# Phase 316: Fix SubmitExam page-closed bug + matrix test infra polish - Context

**Gathered:** 2026-05-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Resolve 3 acknowledged gaps yang diwariskan Phase 315 UAT (`315-UAT.md` lines 82-86):

1. **Submit-exam click race** — `examMatrix.ts` helper page.click submit memicu redirect SEBELUM Playwright finish click event → context closed mid-action → cascade-fail step setelahnya (Scenario 5 critical fail di smoke run 2026-05-11T06:14:36Z).
2. **Screenshot path mismatch** — `softAssert()` tulis path `test-results/matrix-s{id}-{step}.png` ke report tapi Playwright auto-capture tulis ke `test-results/{spec-dirname}/test-failed-{N}.png`. Saat page closed, custom-path silent-fail, hanya auto-capture yang exist.
3. **Sentinel S8/S9/S10 + full inter-scenario continue-on-fail belum tervalidasi E2E** — Phase 315 smoke run S5 cascade-fail, jadi S8/S9/S10 belum ter-exercise; full 10-block run belum demonstrated.

**Out of scope:** Fitur baru, perubahan scope matrix design Phase 315, perubahan UAT design 315, server-side SubmitExam refactor (verified working via 302 redirect smoke).

</domain>

<decisions>
## Implementation Decisions

### Submit-exam race fix
- **D-01:** Pattern: `await Promise.all([page.waitForURL(/CMP\/Results\/\d+/), page.click(submitSelector)])` di `examMatrix.ts:155-156`. Race-tolerant canonical Playwright pattern (sejalan Phase 313.1 helper).
- **D-02:** Server-side scope: **Quick server smoke** — manual curl/postman test ke `/CMP/SubmitExam/{id}` konfirmasi 302 redirect target `/CMP/Results/{id}` masih intact. Tidak full audit `CMPController.cs:1569-1717`.
- **D-03:** Selector strategy: pertahankan OR-selector `'#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)'` (cover review modal desktop + mobile submit). Cuma bungkus dengan Promise.all.
- **D-04:** Regression test: re-run matrix S5 (MC-only scenario) sebagai proxy. S5 berhasil reach `/CMP/Results/{id}` = fix valid. Tidak buat dedicated `submit-race.spec.ts` (reuse existing matrix infra).

### Save-indicator hardening
- **D-05:** Wait pattern saveIndicator: pertahankan current `#saveIndicatorText` filter `/saved|tersimpan/i` timeout 5s di setiap step MC/MA/Essay. Sudah pass 7/8 UAT Phase 315 — tidak ubah.
- **D-06:** Page-closed handling: `page.isClosed()` check di setiap softAssert step. Kalau closed mid-step → throw `SkipScenarioError` langsung (jangan retry, jangan cascade ke step lain di scenario sama).
- **D-07:** SignalR readiness gate: pertahankan `window.assessmentHub.state === 'Connected'` timeout 10s. Belum ada bukti SignalR race di Phase 315 runs.
- **D-08:** Helper scope: **Submit fix + page-closed detect only** — blast radius minimal. MC/MA/Essay save pattern dipertahankan utuh (terbukti working di smoke run). Tidak refactor full helper, tidak extract `submitExam()` jadi function terpisah.

### Screenshot path strategy
- **D-09:** softAssert defensive capture: bungkus `page.screenshot()` di try/catch + pre-check `page.isClosed()` sebelum capture. Page closed → skip custom screenshot (jangan throw, jangan retry).
- **D-10:** Path convention: tetap `test-results/matrix-s{id}-{step}.png` (deterministic, traceable per scenario+step). Tidak adopt random suffix Playwright.
- **D-11:** Report renderer fallback: kalau custom path missing di disk, renderer cari Playwright auto-capture path (`test-results/{spec-dirname}/test-failed-{N}.png`) sebagai fallback. Report tetap link screenshot real, bukan dead link.
- **D-12:** File edit: `tests/e2e/helpers/matrixReport.ts` only. Tidak buat helper baru. Tidak ubah `matrixTypes.ts` kecuali wajib type addition.

### Sentinel + full-run re-verify
- **D-13:** Validation staged 2 langkah: (a) S5 isolated run (`--grep "Scenario 5"`) konfirmasi submit fix; (b) full 10-scenario run (`npx playwright test assessment-matrix`) konfirmasi sentinel + continue-on-fail.
- **D-14:** Run command full: `npx playwright test assessment-matrix` tanpa filter. S10 `test.fail()` annotation → exit 0 saat body throw (collector working), exit non-0 saat collector swallow silently (collector rusak).
- **D-15:** Acceptance criteria per UAT:
  - S5 critical reach `/CMP/Results/{id}` redirect
  - S8 [META-AllCorrect] score 100/100
  - S9 [META-AllWrong] score 0/0
  - S10 [META-CollectorCheck] meta finding tercatat di report `## Meta-validation results`
  - Inter-scenario continue: kalau S1 fail, S2-S10 tetap jalan (bukan stop-on-first-fail)
- **D-16:** UAT items terpisah di `316-UAT.md`: `fix-validate-S5`, `full-run-S1-S10`, `sentinel-meta-verify`, `continue-on-fail-E2E`, `screenshot-path-consistency`. Setiap item independent verifiable.

### Claude's Discretion
- Plan task breakdown (1 plan vs multi-plan) — let planner decide based on dependency analysis
- Specific JSDoc + comment update di `examMatrix.ts` post-fix (reference Phase 316 fix di docstring)
- Apakah perlu update `tests/playwright-report/index.html` test baseline (kemungkinan auto-regen, tidak manual)

### Folded Todos
[None — no pending todos matched Phase 316 scope]

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 315 artifacts (carry-over scope anchor)
- `.planning/phases/315-assessment-matrix-test/315-UAT.md` §"Acknowledged Gaps (carried forward to Phase 316)" lines 82-86 — 3 gap items = scope anchor
- `.planning/phases/315-assessment-matrix-test/315-CONTEXT.md` — original matrix design decisions (sentinel D-06, continue-on-fail design)
- `docs/test-reports/2026-05-11-assessment-matrix.md` — last smoke run report (S5 cascade evidence, screenshot path inconsistency)

### Test helper code (edit targets)
- `tests/e2e/helpers/examMatrix.ts:155-156` — submit click race location (Promise.all wrap)
- `tests/e2e/helpers/examMatrix.ts:96-148` — MC/MA/Essay save-indicator wait loop (page.isClosed gate addition)
- `tests/e2e/helpers/matrixReport.ts` — softAssert defensive screenshot + report renderer fallback path
- `tests/e2e/helpers/matrixTypes.ts` — type definitions (touch only kalau wajib)
- `tests/e2e/assessment-matrix.spec.ts` — spec utama (no edit expected; consumer of helpers)

### App code (read-only reference)
- `Controllers/CMPController.cs:1569+` SubmitExam — server-side handler (302 redirect target `/CMP/Results/{id}`)
- `Controllers/CMPController.cs:4533-4647` EnsureCanSubmitExamAsync + AuditLog SubmitExamBlocked — Tier-1/Tier-2 timer rules
- `Views/CMP/StartExam.cshtml:201,251,469-473` — `#reviewSubmitBtn` (desktop), `#mobileSubmitBtn` (mobile), JS submit handler
- `Views/CMP/StartExam.cshtml:546-547,313-323` — saveIndicator markup + fade animation
- `Hubs/AssessmentHub.cs:134-182,188-252` — SaveTextAnswer (Essay) + SaveMultipleAnswer (MA) SignalR endpoints
- `tests/e2e/helpers/exam313.ts` — Phase 313.1 reference pattern (flat exports, Promise.race race-tolerant)

### Workflow / test infra
- `tests/e2e/global.setup.ts:55-67` — ID collision pre-check (T-315-04)
- `tests/e2e/global.teardown.ts` — Layer 4 cleanup + journal regex active→cleaned
- `docs/SEED_JOURNAL.md` — seed lifecycle audit trail
- `CLAUDE.md` §"Develop Workflow" — local verify → commit/push → Team IT promote

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`softAssert()` mechanism** (`matrixReport.ts`): continue-on-fail wrapper sudah ada; cuma butuh defensive screenshot patch.
- **`SkipScenarioError`** (`matrixReport.ts`): existing critical-fail signal; reuse untuk page-closed scenario abort.
- **Promise.all/Promise.race pattern**: prior decision Phase 313.1, sudah established di `exam313.ts`.
- **`#saveIndicatorText` text filter pattern**: working baseline 7/8 UAT — tidak perlu ganti.

### Established Patterns
- **Flat-export helper**: `tests/e2e/helpers/*.ts` semua flat function exports + JSDoc + source code citation di header (Pattern Phase 313.1 + 315).
- **Source citation di docstring**: helper WAJIB cite `Controllers/...:line` atau `Views/...:line` di header comment.
- **Severity tagging softAssert**: `critical` throw SkipScenarioError (skip scenario steps), `major` continue (record finding).
- **Test naming convention**: `[MATRIX_TEST_2026_05_11] S{id} {name}` — preserve untuk DB Title prefix Layer 4 cleanup.

### Integration Points
- **DB cleanup Layer 4**: tidak terdampak fix (sudah pass UAT). Title prefix tetap `[MATRIX_TEST_2026_05_11]`.
- **Journal regex active→cleaned**: tidak terdampak (lifecycle teardown-driven, bukan helper-driven).
- **Playwright auto-screenshot config**: report renderer fallback path butuh tau `playwright.config.ts` `screenshot: 'only-on-failure'` directory mapping.

### Creative Options
- Helper could expose `submitExamRaceSafe(page, sessionId)` standalone — but D-08 prefers minimal blast radius (no extract).
- Could add metric `helperMetrics.lastSubmitDurationMs` for perf telemetry — out of scope this phase.

</code_context>

<specifics>
## Specific Ideas

- User pakai `Promise.all([waitForURL, click])` snippet di UAT 315 sebagai hypothesis fix — pattern eksplisit, tidak ada deviation.
- User confirm screenshot fallback strategy (report renderer fallback ke Playwright auto-capture path) — bukan eliminasi custom path, dua-duanya supported via fallback.
- Submit fix scope ditekan ke "test-only + quick server smoke" — server full audit di-tolak karena bukti smoke run sebelumnya 302 redirect masih working.

</specifics>

<deferred>
## Deferred Ideas

### Helper enhancements (future phase)
- `helperMetrics` telemetry (per-step duration, retry count) — observability nice-to-have, bukan phase 316 scope.
- `submitExamRaceSafe()` standalone function — refactor future kalau Phase 31x butuh submit isolated.
- Dedicated `submit-race.spec.ts` minimal repro — current decision pakai matrix S5 sebagai proxy.

### App enhancements (future phase)
- Expose `window.assessmentHub.lastSavedAt` timestamp untuk stronger gate (vs saveIndicator label) — app-side change, tidak urgent.
- Refactor `#reviewSubmitBtn` + `#mobileSubmitBtn` jadi single canonical submit selector — markup cleanup, bukan bug fix.

### Reviewed Todos (not folded)
[None]

</deferred>

---

*Phase: 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso*
*Context gathered: 2026-05-11*
