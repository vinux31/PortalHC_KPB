---
phase: 316
slug: fix-submitexam-page-closed-bug-matrix-test-infra-polish-reso
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-11
---

# Phase 316 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright 1.4x (existing — inherited dari Phase 315) |
| **Config file** | `tests/playwright.config.ts` |
| **Quick run command** | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` |
| **Full suite command** | `cd tests && npx playwright test assessment-matrix` |
| **Estimated runtime** | ~120-180s S5 isolated; ~20-30 min full 10-scenario |

---

## Sampling Rate

- **After every task commit:** Run quick run command (S5-only)
- **After every plan wave:** Run full suite command
- **Before `/gsd-verify-work`:** Full suite must complete with all D-15 acceptance items satisfied + exit 0
- **Max feedback latency:** 180s for quick run

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Anchor ID | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-----------|------------|-----------------|-----------|-------------------|-------------|--------|
| 316-01-01 | 01 | 1 | GAP-315-1.a | — | Submit-exam reach `/CMP/Results/{id}` post Promise.all fix | E2E | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` | ✅ | ⬜ pending |
| 316-01-02 | 01 | 1 | GAP-315-1.a (cascade) | — | No cascade major findings di S5 post isClosed gate | E2E | Same as 316-01-01; verify `docs/test-reports/{date}-assessment-matrix.md` S5 ≤1 finding | ✅ | ⬜ pending |
| 316-01-03 | 01 | 1 | GAP-315-2.a | — | Report screenshot links resolve to real files | Visual | After full run open `docs/test-reports/{date}-assessment-matrix.md`; click each `Screenshot:` link | ✅ | ⬜ pending |
| 316-01-04 | 01 | 1 | GAP-315-2.b | — | Page-closed scenarios skip custom screenshot gracefully (no Playwright stderr "page closed" warning) | Code review + smoke | Re-run S5 + grep stderr for "page closed" pattern | ✅ | ⬜ pending |
| 316-01-05 | 01 | 2 | GAP-315-1.b | — | S8 [META-AllCorrect] sentinel pass (no meta finding) atau finding documents score mismatch | E2E | `cd tests && npx playwright test assessment-matrix --grep "Scenario 8"` | ✅ | ⬜ pending |
| 316-01-06 | 01 | 2 | GAP-315-1.c | — | S9 [META-AllWrong] sentinel pass (no meta finding) atau finding documents score mismatch | E2E | `cd tests && npx playwright test assessment-matrix --grep "Scenario 9"` | ✅ | ⬜ pending |
| 316-01-07 | 01 | 2 | GAP-315-1.d | — | S10 [META-CollectorCheck] exit 0 + meta finding tercatat | E2E | `cd tests && npx playwright test assessment-matrix --grep "Scenario 10"; echo "exit: $?"` | ✅ | ⬜ pending |
| 316-01-08 | 01 | 2 | GAP-315-3 | — | Full 10-scenario run completes (S1 fail tidak halt S2-S10) | E2E | `cd tests && npx playwright test assessment-matrix` | ✅ | ⬜ pending |
| 316-01-09 | 01 | 1 | D-02 server smoke | — | Server SubmitExam returns 302 Location `/CMP/Results/{id}` | Manual smoke | PowerShell `Invoke-WebRequest -MaximumRedirection 0` OR DevTools Network | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Log Playwright auto-capture directory listing post first re-run untuk confirm naming pattern `assessment-matrix-Scenario-{slug-hash}-*` (Pitfall 3 / Open Question 1). Tune fallback glob kalau pattern berbeda.
- [ ] Confirm `screenshot: 'on'` config emits per-test screenshots di failure path (smoke evidence sudah confirm di Phase 315; re-confirm post-fix).
- [ ] Resolve SkipScenarioError + softAssert wrapper interaction (Pitfall 5 / Open Question 2) — amend softAssert catch handler dengan re-throw branch SEBELUM generic catch.
- [ ] D-02 server smoke procedure documented + executed sekali (PowerShell or DevTools) — capture 302 response + Location header.

*Existing test infrastructure covers all phase requirements — Wave 0 items = experimental validation + handler amendment, not framework gaps.*

---

## Manual-Only Verifications

| Behavior | Anchor ID | Why Manual | Test Instructions |
|----------|-----------|------------|-------------------|
| Screenshot path resolves visually di report markdown | GAP-315-2.a | Markdown link click-through belum E2E-testable; visual check only | Open `docs/test-reports/{date}-assessment-matrix.md` di IDE preview OR GitHub. Click each `Screenshot:` link → image opens (custom path OR fallback). No dead links. |
| Server 302 redirect smoke | D-02 | Manual prerequisite — no automated test untuk standalone server smoke | PowerShell `Invoke-WebRequest http://localhost:5277/CMP/SubmitExam/{id} -Method Post -Cookie {auth} -MaximumRedirection 0`. Expect Status 302 + Location `/CMP/Results/{id}`. |
| Playwright auto-capture dir naming verification (Wave 0) | A1 | One-shot empirical check before fallback glob tune | After first post-fix run: `ls tests/test-results/ | grep assessment-matrix-Scenario`. Verify dir slug pattern matches expected. |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (Pitfall 5 amendment, auto-capture dir confirm)
- [ ] No watch-mode flags
- [ ] Feedback latency < 180s for quick run, < 30 min for full
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending

---

## Plan 03-06 Gap Closure Tasks (added 2026-05-11)

### Per-Task Verification Map (Gap Closure)

| Task ID | Plan | Wave | Anchor ID | Secure Behavior | Test Type | Automated Command | Status |
|---------|------|------|-----------|-----------------|-----------|-------------------|--------|
| 316-03-01 | 03 | 0 | A2-probe-write | Probe spec 2-block ditulis dengan synthetic throw/pass | Code | `test -f tests/e2e/_throwaway-probe.spec.ts && cd tests && npx tsc --noEmit` | ⬜ pending |
| 316-03-02 | 03 | 0 | A2-probe-run | Probe run capture log + decision di VALIDATION.md | E2E + log | `cd tests && npx playwright test e2e/_throwaway-probe.spec.ts --project=chromium 2>&1 \| tee /tmp/316-w0-probe.log; grep -qE "(passed\|did not run)" /tmp/316-w0-probe.log` | ⬜ pending |
| 316-04-01 | 04 | 1 | GAP-316-1 | examMatrix.ts:182 regex `(Results\|ExamSummary)` | Code | `grep -qE "Results\\\\\|ExamSummary" tests/e2e/helpers/examMatrix.ts && cd tests && npx tsc --noEmit` | ⬜ pending |
| 316-04-02 | 04 | 1 | GAP-316-2 (a-revised) | matrixReport.ts softAssert cascade promotion branch | Code | `grep -qE "isPageClosedError" tests/e2e/helpers/matrixReport.ts && cd tests && npx tsc --noEmit` | ⬜ pending |
| 316-05-01 | 05 | 2 | A2-gate-check | Read VALIDATION.md A2 outcome — proceed atau skip | Bash | `grep -qE "A2 (VALID\|INVALID)" .planning/phases/316-*/316-VALIDATION.md` | ⬜ pending |
| 316-05-02 | 05 | 2 | GAP-316-2 (d-partial) | 10 test.describe() blocks + serial config dropped | Code | `grep -c "test\.describe\('Scenario" tests/e2e/assessment-matrix.spec.ts \| grep -q "^10$"` | ⬜ pending |
| 316-06-01 | 06 | 3 | actionTimeout-defense | playwright.config.ts use.actionTimeout: 10_000 | Code | `grep -qE "actionTimeout: 10_000" tests/playwright.config.ts && cd tests && npx tsc --noEmit` | ⬜ pending |
| 316-06-02 | 06 | 3 | full-validation | Quick S5 + full S1-S10 runs executed + report regenerated + probe deleted | E2E | `test ! -f tests/e2e/_throwaway-probe.spec.ts && test -f /tmp/316-final-full.log && grep -qE "## Meta-validation results" docs/test-reports/2026-05-11-assessment-matrix.md` | ⬜ pending |
| 316-06-03 | 06 | 3 | UAT-reverdict | 316-UAT.md re-verdict 5 D-16 items pasca Plan 04+05+06 | Doc | `grep -qE "Plan 04\+05\+06\|gap closure\|CLOSED" .planning/phases/316-*/316-UAT.md` | ⬜ pending |

### Wave 0 Probe Outcome

*Result captured by Plan 03 Task 2. Lihat juga `/tmp/316-w0-probe.log`.*

(To be filled after Plan 03 execution.)

