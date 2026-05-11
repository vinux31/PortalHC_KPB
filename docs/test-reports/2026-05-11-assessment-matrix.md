# Phase 315 Matrix Test Report — 2026-05-11

## Summary

**Total discovery findings:** 2
- Critical: 1
- Major: 1
- Minor: 0

**Meta-validation findings:** 0 (excluded dari discovery statistik — lihat § Meta-validation results)

## Discovery findings

### Scenario 1: [MATRIX_TEST_2026_05_11] S1 Manual Mixed — essay-q50003

- **Severity:** major
- **Expected:** Essay q50003 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** page.fill: Test ended.
Call log:
[2m  - waiting for locator('textarea.exam-essay[data-question-id="50003"]')[22m

- **Screenshot:** `test-results/matrix-s1-essay-q50003.png`
- **Hypothesis:** Essay step gagal akibat page/context closed (kaskade dari critical fail di langkah sebelumnya — biasanya submit-exam navigate race). Cek finding sebelumnya di scenario ini + `Hubs/AssessmentHub.cs` line 134-182.

### Scenario 1: [MATRIX_TEST_2026_05_11] S1 Manual Mixed — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id}
- **Actual:** page.click: Target page, context or browser has been closed
- **Screenshot:** `test-results/matrix-s1-submit-exam.png`
- **Hypothesis:** SubmitExam click race: page.click trigger redirect SEBELUM Playwright finish click event → context closed mid-action (recurrence dari Plan 04 S5 finding). Solusi: refactor helper pakai `Promise.all([page.waitForURL("**/CMP/Results/**"), page.click(...)])` race-tolerant pattern. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) untuk redirect target.

## Meta-validation results

_Tidak ada finding sentinel di run ini._
