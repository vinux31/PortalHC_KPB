---
phase: 317
slug: fix-surf-316-a-ma-essay-mixed-e2e-via-ui
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-11
---

# Phase 317 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright 1.55.0 + TypeScript |
| **Config file** | `tests/playwright.config.ts` |
| **Quick run command** | `cd tests && npx playwright test exam-types --grep "FLOW K"` |
| **Full suite command** | `cd tests && npx playwright test exam-types` |
| **Estimated runtime** | ~3-5 menit (5 FLOW × 1 worker, includes login/wizard/exam/grade/results) |

---

## Sampling Rate

- **After every task commit:** Run `{quick run command for current FLOW}`
- **After every plan wave:** Run full FLOW K-O suite once
- **Before `/gsd-verify-work`:** Full suite must be green + regression smoke FLOW A-J pass rate ≥ baseline
- **Max feedback latency:** ~90s per FLOW

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 317-01-W0 | 01 | 0 | QA-02 | smoke | `npx playwright test exam-types --grep "smoke wave-0"` | ❌ W0 | ⬜ pending |
| 317-01-K | 01 | 1 | QA-02 | e2e | `npx playwright test exam-types --grep "FLOW K"` | ❌ W0 | ⬜ pending |
| 317-01-L | 01 | 1 | QA-02 | e2e | `npx playwright test exam-types --grep "FLOW L"` | ❌ W0 | ⬜ pending |
| 317-02-M | 02 | 2 | QA-02 | e2e | `npx playwright test exam-types --grep "FLOW M"` | ❌ W0 | ⬜ pending |
| 317-02-N | 02 | 2 | QA-02 | e2e | `npx playwright test exam-types --grep "FLOW N"` | ❌ W0 | ⬜ pending |
| 317-02-O | 02 | 3 | QA-02 | e2e | `npx playwright test exam-types --grep "FLOW O"` | ❌ W0 | ⬜ pending |
| 317-02-REG | 02 | 3 | QA-02 | regression | `npx playwright test exam-taking` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Wave 0 = smoke verify untuk 2 Low/Medium confidence assumptions dari RESEARCH (A4 question order, A5 timer var scope) sebelum commit FLOW K + FLOW O code.

- [ ] `tests/e2e/exam-types.spec.ts` — file skeleton + smoke wave-0 test block
- [ ] `tests/e2e/helpers/examTypes.ts` — helper module skeleton (createAssessmentViaWizard, addQuestionsViaUI, addExtraTime stubs)
- [ ] Smoke verify A4: HC create 3 MC questions → coachee start exam → assert q1/q2/q3 render in creation order (DOM textContent match)
- [ ] Smoke verify A5: coachee in exam → `page.evaluate(() => typeof window.timerStartRemaining)` returns 'number' (not 'undefined')
- [ ] Extend `tests/e2e/helpers/wizardSelectors.ts` jika ada selector wizard step belum ada

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| FLOW O timer visual confirmation | QA-02 | SignalR push timing flaky in CI/headless | After AddExtraTime: open coachee browser, verify timer reads new value within 2s |
| AllowAnswerReview=false UX text | QA-02 | Indonesian copy may change | Verify Results page shows "Tinjauan jawaban tidak tersedia" alert-info banner |
| HC wizard 4-step UX flow | QA-02 | Visual regression check | HC creates 1 assessment manually via UI, confirms wizard steps 1→2→3→4 work |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers A4 (question order) + A5 (timer var scope) verification
- [ ] No watch-mode flags (Playwright `--watch` excluded — slow feedback for matrix-style tests)
- [ ] Feedback latency < 90s per FLOW
- [ ] `nyquist_compliant: true` set in frontmatter (after planner reviews)

**Approval:** pending
