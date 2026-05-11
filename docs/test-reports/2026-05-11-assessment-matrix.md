# Phase 315 Matrix Test Report — 2026-05-11

## Summary

**Total discovery findings:** 4
- Critical: 0
- Major: 4
- Minor: 0

**Meta-validation findings:** 0 (excluded dari discovery statistik — lihat § Meta-validation results)

## Discovery findings

### Scenario 2: [MATRIX_TEST_2026_05_11] S2 Online Mixed — verify-result-page

- **Severity:** major
- **Expected:** Result page score visible untuk coachee session 9003
- **Actual:** [2mexpect([22m[31mlocator[39m[2m).[22mtoBeVisible[2m([22m[2m)[22m failed

Locator: locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()
Expected: visible
Timeout: 10000ms
Error: element(s) not found

Call log:
[2m  - Expect "toBeVisible" with timeout 10000ms[22m
[2m  - waiting for locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()[22m

- **Screenshot:** `test-results/matrix-s2-verify-result-page.png`
- **Hypothesis:** Score badge selector tidak ketemu — mungkin scoring belum komplet (Essay grading pending) atau selector view berubah. Cek `Views/CMP/Results.cshtml` + verifikasi `gradeEssaysAsHc` step lulus + `AssessmentSession.Status = "Completed"` di DB.

### Scenario 3: [MATRIX_TEST_2026_05_11] S3 PreTest Mixed — verify-result-page

- **Severity:** major
- **Expected:** Result page score visible untuk coachee session 9005
- **Actual:** [2mexpect([22m[31mlocator[39m[2m).[22mtoBeVisible[2m([22m[2m)[22m failed

Locator: locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()
Expected: visible
Timeout: 10000ms
Error: element(s) not found

Call log:
[2m  - Expect "toBeVisible" with timeout 10000ms[22m
[2m  - waiting for locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()[22m

- **Screenshot:** `test-results/matrix-s3-verify-result-page.png`
- **Hypothesis:** Score badge selector tidak ketemu — mungkin scoring belum komplet (Essay grading pending) atau selector view berubah. Cek `Views/CMP/Results.cshtml` + verifikasi `gradeEssaysAsHc` step lulus + `AssessmentSession.Status = "Completed"` di DB.

### Scenario 4: [MATRIX_TEST_2026_05_11] S4 PostTest Mixed — hc-grade-essays

- **Severity:** major
- **Expected:** HC grades 1 essay questions × 2 sessions + finalize
- **Actual:** locator.waitFor: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#badge_9008_50021').filter({ hasText: /Sudah Dinilai/i }) to be visible[22m

- **Screenshot:** `test-results/matrix-s4-hc-grade-essays.png`
- **Hypothesis:** HC essay grading workflow gagal — selector input score / finalize button mungkin berubah. Cek `Controllers/AssessmentAdminController.cs` SubmitEssayScore + FinalizeEssayGrading (line 2873-2950) + `Views/Admin/AssessmentMonitoringDetail.cshtml` line 348-451 markup.

### Scenario 4: [MATRIX_TEST_2026_05_11] S4 PostTest Mixed — verify-result-page

- **Severity:** major
- **Expected:** Result page score visible untuk coachee session 9007
- **Actual:** [2mexpect([22m[31mlocator[39m[2m).[22mtoBeVisible[2m([22m[2m)[22m failed

Locator: locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()
Expected: visible
Timeout: 10000ms
Error: element(s) not found

Call log:
[2m  - Expect "toBeVisible" with timeout 10000ms[22m
[2m  - waiting for locator('.badge.text-bg-secondary, .badge.text-bg-success, .badge.text-bg-danger').first()[22m

- **Screenshot:** `test-results/matrix-s4-verify-result-page.png`
- **Hypothesis:** Score badge selector tidak ketemu — mungkin scoring belum komplet (Essay grading pending) atau selector view berubah. Cek `Views/CMP/Results.cshtml` + verifikasi `gradeEssaysAsHc` step lulus + `AssessmentSession.Status = "Completed"` di DB.

## Meta-validation results

_Tidak ada finding sentinel di run ini._
