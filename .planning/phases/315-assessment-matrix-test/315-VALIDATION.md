---
phase: 315
slug: assessment-matrix-test
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-11
---

# Phase 315 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | @playwright/test 1.58.2 (TypeScript) |
| **Config file** | `tests/playwright.config.ts` (EDIT — add globalTeardown) |
| **Quick run command** | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` |
| **Full suite command** | `cd tests && npx playwright test assessment-matrix` |
| **Estimated runtime** | ~2-3 min (smoke) / ~10-15 min (full 10 blocks) |

---

## Sampling Rate

- **After every task commit:** Run `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` (smoke; only after Wave 1 helpers compile + Wave 2 spec exists. Earlier tasks use TypeScript compile check via `cd tests && npx tsc --noEmit`.)
- **After every plan wave:** Run `cd tests && npx playwright test assessment-matrix` + inspect `docs/test-reports/YYYY-MM-DD-assessment-matrix.md` + verify Layer 4 cleanup row count = 0
- **Before `/gsd-verify-work`:** Full suite green, smoke produces expected single sentinel-minor finding, Layer 1-4 all pass, report markdown valid
- **Max feedback latency:** 180 seconds (smoke run)

---

## Per-Task Verification Map

> Granular task IDs assigned at planning time. Mapping below uses placeholder task IDs grouped by wave; planner MUST refine when emitting PLAN.md files.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 315-00-01 | 00-investigation | 0 | QA-01 | — | A1/A2/A6 source-code answers logged | research | `grep -n "A1\|A2\|A6" .planning/phases/315-assessment-matrix-test/315-INVESTIGATION.md` | ❌ W0 | ⬜ pending |
| 315-01-01 | 01-helpers | 1 | QA-01 | T-315-01 (hostname guard) | dbSnapshot.ts rejects non-localhost `-S` arg | unit | `cd tests && npx vitest run helpers/dbSnapshot.test.ts` OR `npx tsc --noEmit` | ❌ W1 | ⬜ pending |
| 315-01-02 | 01-helpers | 1 | QA-01 | — | matrixReport collector flush writes valid markdown | unit/compile | `cd tests && npx tsc --noEmit` | ❌ W1 | ⬜ pending |
| 315-01-03 | 01-helpers | 1 | QA-01 | — | examMatrix.ts compiles + exports takeExam/gradeEssaysAsHc/verifyResultPage | compile | `cd tests && npx tsc --noEmit` | ❌ W1 | ⬜ pending |
| 315-01-04 | 02-seed-setup | 1 | QA-01 | T-315-02 (data injection) | seed SQL only inserts MATRIX_TEST_2026_05_11-tagged rows | integration | `sqlcmd -S localhost\SQLEXPRESS -d HcPortalDB_Dev -i tests/sql/assessment-matrix-seed.sql` (lokal) + post-query row count check | ❌ W1 | ⬜ pending |
| 315-01-05 | 02-seed-setup | 1 | QA-01 | — | global.setup.ts BACKUP → seed → state.json → journal entry | integration | `cd tests && npx playwright test --list assessment-matrix` (triggers setup) | ❌ W1 | ⬜ pending |
| 315-01-06 | 02-seed-setup | 1 | QA-01 | T-315-03 (cleanup leak) | global.teardown.ts flush → RESTORE → Layer 4 row count = 0 | integration | runtime via Playwright; post-run `sqlcmd ... -Q "SELECT COUNT(*) FROM AssessmentSession WHERE Title LIKE '[MATRIX_TEST_%]%'"` returns 0 | ❌ W1 | ⬜ pending |
| 315-01-07 | 02-seed-setup | 1 | QA-01 | — | playwright.config.ts registers globalTeardown path | static | `grep -n "globalTeardown" tests/playwright.config.ts` | ❌ W1 | ⬜ pending |
| 315-02-01 | 03-spec-main | 2 | QA-01 | — | 7 discovery scenarios + 3 sentinel test blocks present | static | `grep -cE "^test\(" tests/e2e/assessment-matrix.spec.ts` returns 10 | ❌ W2 | ⬜ pending |
| 315-02-02 | 03-spec-main | 2 | QA-01 | — | sentinel `[META-CollectorCheck]` uses `test.fail()` annotation | static | `grep -n "test.fail" tests/e2e/assessment-matrix.spec.ts` matches sentinel block | ❌ W2 | ⬜ pending |
| 315-02-03 | 03-spec-main | 2 | QA-01 | — | smoke run produces expected single sentinel-minor finding | e2e | `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` exit 0 + report contains "Scenario 5" | ❌ W2 | ⬜ pending |
| 315-03-01 | 04-report-polish | 3 | QA-01 | — | full run report has severity + screenshot + hypothesis per finding | e2e + manual inspect | `cd tests && npx playwright test assessment-matrix` exit 0 + report markdown shape check | ❌ W3 | ⬜ pending |
| 315-03-02 | 04-report-polish | 3 | QA-01 | — | meta-validation section separated from discovery summary | static | `grep -n "## Meta-validation results" docs/test-reports/2026-05-11-assessment-matrix.md` | ❌ W3 | ⬜ pending |
| 315-03-03 | 04-report-polish | 3 | QA-01 | — | console error whitelist documented in matrixReport config | static | `grep -n "consoleErrorWhitelist" tests/e2e/helpers/matrixReport.ts` | ❌ W3 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `.planning/phases/315-assessment-matrix-test/315-INVESTIGATION.md` — answers for A1 (AssessmentPackage cardinality with sibling sessions), A2 (SubmitExam Essay branch — form vs DB read), A6 (UserPackageAssignment auto-create vs pre-seed)
- [ ] Read full body of `Controllers/CMPController.cs` SubmitExam (lines 1569-1800+) — verify Essay save path authoritative source
- [ ] Read `Models/AssessmentPackage.cs` + `Models/UserPackageAssignment.cs` — finalize seed dimensions (18 sessions vs 9 shared)
- [ ] Document final seed cardinality decision (override or confirm spec's "2 peserta per session" wording)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Report markdown readability + actionable hypothesis quality | QA-01 (Success Criteria #6) | Human judgment — markdown valid otomatis, tapi "actionable hypothesis" subyektif | Developer review `docs/test-reports/2026-05-11-assessment-matrix.md` setelah full run; spot-check 3 random finding apakah severity + screenshot path + hypothesis cukup untuk reproduce |
| Screenshot quality (full page, relevan dengan finding) | QA-01 | Visual inspection | Open 3 random screenshot dari `tests/test-results/` linked di report; verify URL bar + relevant UI visible |
| Smoke run protocol compliance | QA-01 (Success Criteria #4) | Workflow gate, bukan code behavior | Developer jalankan smoke run dulu sebelum full run; documented di header docblock `assessment-matrix.spec.ts` atau README di `tests/` |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (A1, A2, A6)
- [ ] No watch-mode flags (`--watch`, `--ui`) in any task command
- [ ] Feedback latency < 180s (smoke); < 900s (full)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
