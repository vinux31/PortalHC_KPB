---
phase: 315
slug: assessment-matrix-test
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-11
---

**Note:** `nyquist_compliant` dan `wave_0_complete` tetap `false` selama planning draft. Flip ke `true` setelah Plan 01 execute + plan-check pass + spec verifies expected behavior (yaitu setelah `315-INVESTIGATION.md` ditulis dengan 3 verdict definitif dan smoke run Plan 04 menghasilkan exit 0 dengan report shape valid).

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

> Granular task IDs sesuai struktur plan final: `315-{plan-no}-{task-no}` matching Plan 01..05 actual file structure.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 315-01-01 | 01-investigation (Wave 0) | 0 | QA-01 | — | A1/A2/A6 source-code answers logged dengan verdict definitif | research | `grep -nE "^\\*\\*Verdict:\\*\\*" .planning/phases/315-assessment-matrix-test/315-INVESTIGATION.md` returns 3 | ❌ W0 | ⬜ pending |
| 315-02-01 | 02-helpers-foundation | 1 | QA-01 | T-315-01 (hostname guard) + T-315-05 (gitignore) | dbSnapshot.ts rejects non-localhost `-S` arg + matrixTypes.ts exports + tests/.gitignore | unit/compile | `cd tests && npx tsc --noEmit` + `grep "Refusing to target non-localhost" tests/helpers/dbSnapshot.ts` | ❌ W1 | ⬜ pending |
| 315-02-02 | 02-helpers-foundation | 1 | QA-01 | — | matrixReport collector + softAssert + flush + examMatrix.ts (takeExam/gradeEssaysAsHc/verifyResultPage) with concrete selectors | unit/compile | `cd tests && npx tsc --noEmit` + `grep -cE "page\\.locator\\(['\"]" tests/e2e/helpers/examMatrix.ts` returns ≥ 2 for essay grading selectors | ❌ W1 | ⬜ pending |
| 315-03-01 | 03-seed-and-lifecycle | 2 | QA-01 | T-315-02 (data injection) | seed SQL only inserts MATRIX_TEST_2026_05_11-tagged rows; deterministic option ID formula | integration | `sqlcmd -S localhost\SQLEXPRESS -d HcPortalDB_Dev -i tests/sql/assessment-matrix-seed.sql` (lokal) + post-query row count check + `grep "optId = 80001 + (qId - 50001)" tests/sql/assessment-matrix-seed.sql` returns ≥ 1 | ❌ W2 | ⬜ pending |
| 315-03-02 | 03-seed-and-lifecycle | 2 | QA-01 | T-315-03 (cleanup leak) | global.setup BACKUP → seed → Layer 1 → state.json + journal active; global.teardown flush → RESTORE → Layer 4 = 0 → journal cleaned | integration | `cd tests && npx playwright test --list assessment-matrix` (triggers setup) + post-run `sqlcmd ... -Q "SELECT COUNT(*) FROM AssessmentSession WHERE Title LIKE '[MATRIX_TEST_%]%'"` returns 0 + `grep -n "globalTeardown" tests/playwright.config.ts` | ❌ W2 | ⬜ pending |
| 315-04-01 | 04-spec-main | 3 | QA-01 | — | 7 discovery + 3 sentinel test blocks; `test.fail` annotation pada META-CollectorCheck; smoke run S5 exit 0 dengan report markdown valid | e2e | `cd tests && npx playwright test --list assessment-matrix` enumerates 10 tests + `grep -cE "^test\\(" tests/e2e/assessment-matrix.spec.ts` matches expected + `cd tests && npx playwright test assessment-matrix --grep "Scenario 5"` exit 0 | ❌ W3 | ⬜ pending |
| 315-05-01 | 05-report-polish | 4 | QA-01 | — | full-run report has severity + screenshot + concrete hypothesis (no TBD) per finding; meta-validation section separated from discovery summary | e2e + static | `cd tests && npx playwright test assessment-matrix` exit 0 + `! grep -q "TBD" docs/test-reports/$(date +%Y-%m-%d)-assessment-matrix.md` + `grep -n "consoleErrorWhitelist" tests/e2e/helpers/matrixReport.ts` | ❌ W4 | ⬜ pending |
| 315-05-02 | 05-report-polish | 4 | QA-01 | T-315-03 (re-verify) | manual UAT checkpoint: smoke + full + report inspect + 4-Layer + DB cleanup all green | manual checkpoint | (blocking checkpoint — human approves after running steps 1-5 di Plan 05 Task 2 how-to-verify) | ❌ W4 | ⬜ pending |

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
- [ ] `nyquist_compliant: true` set in frontmatter (after Plan 01 execute + plan-check pass)

**Approval:** pending
