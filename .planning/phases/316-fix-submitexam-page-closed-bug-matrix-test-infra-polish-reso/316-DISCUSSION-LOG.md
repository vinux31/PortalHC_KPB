# Phase 316: Fix SubmitExam page-closed bug + matrix test infra polish - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-11
**Phase:** 316-fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso
**Areas discussed:** Submit-exam race fix, Save-indicator hardening, Screenshot path strategy, Sentinel + full-run re-verify

---

## Submit-exam race fix

### Q1: Pattern fix submit-exam click race?

| Option | Description | Selected |
|--------|-------------|----------|
| Promise.all wait+click | `await Promise.all([page.waitForURL(/CMP\/Results\/\d+/), page.click(submitSelector)])` — pattern Playwright canonical. (Recommended — hipotesis UAT 315) | ✓ |
| Promise.race + timeout | Race waitForURL dgn explicit timeout, click di-trigger via evaluate() biar tidak hold context | |
| Click + waitForNavigation | Pakai `page.waitForNavigation` legacy API (deprecated di Playwright modern — tidak rekomendasi) | |

**User's choice:** Promise.all wait+click (sesuai rekomendasi)

### Q2: Server-side SubmitExam diperiksa juga atau test-only?

| Option | Description | Selected |
|--------|-------------|----------|
| Test-only fix | Bug = test race. Skip server scope. (Recommended) | |
| Quick server smoke | Tambahan: manual curl/postman test `/CMP/SubmitExam/{id}` konfirmasi 302 redirect intact — sanity check | ✓ |
| Full server audit | Audit lengkap `CMPController.cs:1569-1717` SubmitExam path + EnsureCanSubmitExamAsync — overkill | |

**User's choice:** Quick server smoke

### Q3: Selector strategy fix submit?

| Option | Description | Selected |
|--------|-------------|----------|
| Keep current OR-selector | `'#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)'` cover review modal + mobile. (Recommended) | ✓ |
| Conditional desktop/mobile | Branch helper berdasar viewport — extra branch | |
| Force scroll + first match | scrollIntoView + `.first()` — kurangi ambiguity selector | |

**User's choice:** Keep current OR-selector

### Q4: Test regression untuk submit fix?

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse matrix S5 | Re-run S5 (MC-only) sebagai proxy. (Recommended) | ✓ |
| Dedicated unit test | Tulis `submit-race.spec.ts` minimal repro — extra file | |
| Both | Dedicated spec + matrix S5 — belt-and-suspenders | |

**User's choice:** Reuse matrix S5

---

## Save-indicator hardening

### Q1: Wait pattern saveIndicator setelah jawab MC/MA/Essay?

| Option | Description | Selected |
|--------|-------------|----------|
| Strict per-step wait | Pertahankan `#saveIndicatorText` filter 'saved\|tersimpan' timeout 5s. (Recommended) | ✓ |
| Add fallback poll DB | Tambah optional DB-poll PackageUserResponse — lebih robust, lebih lambat | |
| Relax to networkidle | Ganti ke `page.waitForLoadState('networkidle')` — false-positive prone | |

**User's choice:** Strict per-step wait

### Q2: Handle page/context closed mid-step?

| Option | Description | Selected |
|--------|-------------|----------|
| Detect + skip remaining | Wrap saveIndicator wait dgn `page.isClosed()` check; throw SkipScenarioError. (Recommended) | ✓ |
| Defensive try/catch swallow | Catch 'Target page closed' error di softAssert; treat as critical fail | |
| No special handling | Biarkan exception bubble | |

**User's choice:** Detect + skip remaining

### Q3: SignalR readiness gate — perlu strengthening?

| Option | Description | Selected |
|--------|-------------|----------|
| Keep current | `window.assessmentHub.state === 'Connected'` timeout 10s. (Recommended) | ✓ |
| Add reconnect retry | Kalau state ≠ Connected setelah 10s, restart hub + retry 1x | |
| Track lastSavedAt timestamp | Expose `window.assessmentHub.lastSavedAt` di app | |

**User's choice:** Keep current

### Q4: Scope perubahan examMatrix.ts?

| Option | Description | Selected |
|--------|-------------|----------|
| Submit + page-closed only | Fix submit Promise.all + page-closed detect. Minimal blast radius. (Recommended) | ✓ |
| Full helper hardening pass | Refactor seluruh helper + retry on network blip | |
| Extract submit ke helper terpisah | Pisah `submitExam()` dari `takeExam()` — extra refactor | |

**User's choice:** Submit + page-closed only

---

## Screenshot path strategy

*All recommended defaults locked per user instruction "sesuai reco kamu".*

### Decisions
- **softAssert defensive capture:** try/catch + `page.isClosed()` pre-check sebelum `page.screenshot()`
- **Path convention:** tetap `test-results/matrix-s{id}-{step}.png` (deterministic)
- **Renderer fallback:** kalau custom path missing, fallback ke Playwright auto-capture path
- **File edit:** `matrixReport.ts` only

---

## Sentinel + full-run re-verify

*All recommended defaults locked per user instruction "sesuai reco kamu".*

### Decisions
- **Validation staged:** S5 isolated run (verify fix) → full 10-scenario run (sentinel + continue-on-fail)
- **Run command:** `npx playwright test assessment-matrix` no filter (S10 `test.fail()` semantics)
- **Acceptance:** S5 redirect, S8=100/100, S9=0/0, S10 meta finding tercatat, inter-scenario continue verified
- **UAT items:** terpisah — fix-validate-S5, full-run-S1-S10, sentinel-meta-verify, continue-on-fail-E2E, screenshot-path-consistency

---

## Claude's Discretion

- Plan task breakdown (1 plan vs multi-plan) — planner decide via dependency analysis
- JSDoc + comment update di `examMatrix.ts` post-fix
- `tests/playwright-report/index.html` baseline regen handling

## Deferred Ideas

- `helperMetrics` telemetry (per-step duration, retry count)
- `submitExamRaceSafe()` standalone function (future refactor)
- Dedicated `submit-race.spec.ts` minimal repro
- `window.assessmentHub.lastSavedAt` timestamp expose (app-side change)
- Markup cleanup: single canonical submit selector vs `#reviewSubmitBtn` + `#mobileSubmitBtn`
