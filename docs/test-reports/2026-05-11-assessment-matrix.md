# Phase 315 Matrix Test Report — 2026-05-11

## Summary

**Total discovery findings:** 1
- Critical: 0
- Major: 1
- Minor: 0

**Meta-validation findings:** 0 (excluded dari discovery statistik — lihat § Meta-validation results)

## Discovery findings

### Scenario 1: [MATRIX_TEST_2026_05_11] S1 Manual Mixed — ma-q50002

- **Severity:** major
- **Expected:** MA q50002 optionIds=[80005,80006] saved via SaveMultipleAnswer hub
- **Actual:** page.check: Test ended.
Call log:
[2m  - waiting for locator('input.exam-checkbox[data-question-id="50002"][value="80005"]')[22m

- **Screenshot:** `test-results/assessment-matrix-Scenario-1-S1-Manual-Mixed-MC-MA-Essay--chromium/test-failed-1.png`
- **Hypothesis:** MA step gagal akibat page/context closed (kaskade dari critical fail di langkah sebelumnya — biasanya submit-exam navigate race). Cek finding sebelumnya di scenario ini + `Hubs/AssessmentHub.cs` line 188-252.

## Meta-validation results

_Tidak ada finding sentinel di run ini._
