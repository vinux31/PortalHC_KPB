# Phase 315 Matrix Test Report — 2026-05-11

## Summary

**Total discovery findings:** 25
- Critical: 9
- Major: 16
- Minor: 0

**Meta-validation findings:** 1 (excluded dari discovery statistik — lihat § Meta-validation results)

## Discovery findings

### Scenario 1: [MATRIX_TEST_2026_05_11] S1 Manual Mixed — essay-q50003

- **Severity:** major
- **Expected:** Essay q50003 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** page.fill: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('textarea.exam-essay[data-question-id="50003"]')[22m

- **Screenshot:** `test-results/matrix-s1-essay-q50003.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 1: [MATRIX_TEST_2026_05_11] S1 Manual Mixed — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s1-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

### Scenario 2: [MATRIX_TEST_2026_05_11] S2 Online Mixed — ma-q50008

- **Severity:** major
- **Expected:** MA q50008 optionIds=[80029,80030] saved via SaveMultipleAnswer hub
- **Actual:** page.check: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('input.exam-checkbox[data-question-id="50008"][value="80029"]')[22m

- **Screenshot:** `test-results/matrix-s2-ma-q50008.png`
- **Hypothesis:** MA `SaveMultipleAnswer` SignalR hub invoke mungkin gagal, atau locator `input.exam-checkbox[data-question-id="..."][value="..."]` tidak match DOM. Cek `Hubs/AssessmentHub.cs` line 188-252 + hub state log + verifikasi `correctOptionIds` di seed match dengan DOM `data-question-id` + `value`.

### Scenario 2: [MATRIX_TEST_2026_05_11] S2 Online Mixed — essay-q50009

- **Severity:** major
- **Expected:** Essay q50009 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** page.fill: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('textarea.exam-essay[data-question-id="50009"]')[22m

- **Screenshot:** `test-results/matrix-s2-essay-q50009.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 2: [MATRIX_TEST_2026_05_11] S2 Online Mixed — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s2-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

### Scenario 3: [MATRIX_TEST_2026_05_11] S3 PreTest Mixed — ma-q50014

- **Severity:** major
- **Expected:** MA q50014 optionIds=[80053,80054] saved via SaveMultipleAnswer hub
- **Actual:** page.check: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('input.exam-checkbox[data-question-id="50014"][value="80053"]')[22m

- **Screenshot:** `test-results/matrix-s3-ma-q50014.png`
- **Hypothesis:** MA `SaveMultipleAnswer` SignalR hub invoke mungkin gagal, atau locator `input.exam-checkbox[data-question-id="..."][value="..."]` tidak match DOM. Cek `Hubs/AssessmentHub.cs` line 188-252 + hub state log + verifikasi `correctOptionIds` di seed match dengan DOM `data-question-id` + `value`.

### Scenario 3: [MATRIX_TEST_2026_05_11] S3 PreTest Mixed — essay-q50015

- **Severity:** major
- **Expected:** Essay q50015 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** page.fill: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('textarea.exam-essay[data-question-id="50015"]')[22m

- **Screenshot:** `test-results/matrix-s3-essay-q50015.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 3: [MATRIX_TEST_2026_05_11] S3 PreTest Mixed — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s3-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

### Scenario 4: [MATRIX_TEST_2026_05_11] S4 PostTest Mixed — ma-q50020

- **Severity:** major
- **Expected:** MA q50020 optionIds=[80077,80078] saved via SaveMultipleAnswer hub
- **Actual:** page.check: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('input.exam-checkbox[data-question-id="50020"][value="80077"]')[22m

- **Screenshot:** `test-results/matrix-s4-ma-q50020.png`
- **Hypothesis:** MA `SaveMultipleAnswer` SignalR hub invoke mungkin gagal, atau locator `input.exam-checkbox[data-question-id="..."][value="..."]` tidak match DOM. Cek `Hubs/AssessmentHub.cs` line 188-252 + hub state log + verifikasi `correctOptionIds` di seed match dengan DOM `data-question-id` + `value`.

### Scenario 4: [MATRIX_TEST_2026_05_11] S4 PostTest Mixed — essay-q50021

- **Severity:** major
- **Expected:** Essay q50021 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** page.fill: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('textarea.exam-essay[data-question-id="50021"]')[22m

- **Screenshot:** `test-results/matrix-s4-essay-q50021.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 4: [MATRIX_TEST_2026_05_11] S4 PostTest Mixed — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s4-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

### Scenario 5: [MATRIX_TEST_2026_05_11] S5 Online MC only — mc-q50026

- **Severity:** major
- **Expected:** MC q50026 optionId=80101 saved
- **Actual:** page.check: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('input.exam-radio[data-question-id="50026"][value="80101"]')[22m

- **Screenshot:** `test-results/matrix-s5-mc-q50026.png`
- **Hypothesis:** MC HTTP `/CMP/SaveAnswer` mungkin 500/timeout, atau `#saveIndicatorText` selector berubah / race-collapse. Cek `Controllers/CMPController.cs` SaveAnswer (line 348-417) + Network tab response status + `Views/CMP/StartExam.cshtml` indicator transition.

### Scenario 5: [MATRIX_TEST_2026_05_11] S5 Online MC only — mc-q50027

- **Severity:** major
- **Expected:** MC q50027 optionId=80105 saved
- **Actual:** page.check: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('input.exam-radio[data-question-id="50027"][value="80105"]')[22m

- **Screenshot:** `test-results/matrix-s5-mc-q50027.png`
- **Hypothesis:** MC HTTP `/CMP/SaveAnswer` mungkin 500/timeout, atau `#saveIndicatorText` selector berubah / race-collapse. Cek `Controllers/CMPController.cs` SaveAnswer (line 348-417) + Network tab response status + `Views/CMP/StartExam.cshtml` indicator transition.

### Scenario 5: [MATRIX_TEST_2026_05_11] S5 Online MC only — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s5-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

### Scenario 6: [MATRIX_TEST_2026_05_11] S6 Online MA only — ma-q50032

- **Severity:** major
- **Expected:** MA q50032 optionIds=[80125,80126] saved via SaveMultipleAnswer hub
- **Actual:** page.check: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('input.exam-checkbox[data-question-id="50032"][value="80125"]')[22m

- **Screenshot:** `test-results/matrix-s6-ma-q50032.png`
- **Hypothesis:** MA `SaveMultipleAnswer` SignalR hub invoke mungkin gagal, atau locator `input.exam-checkbox[data-question-id="..."][value="..."]` tidak match DOM. Cek `Hubs/AssessmentHub.cs` line 188-252 + hub state log + verifikasi `correctOptionIds` di seed match dengan DOM `data-question-id` + `value`.

### Scenario 6: [MATRIX_TEST_2026_05_11] S6 Online MA only — ma-q50033

- **Severity:** major
- **Expected:** MA q50033 optionIds=[80129,80130] saved via SaveMultipleAnswer hub
- **Actual:** page.check: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('input.exam-checkbox[data-question-id="50033"][value="80129"]')[22m

- **Screenshot:** `test-results/matrix-s6-ma-q50033.png`
- **Hypothesis:** MA `SaveMultipleAnswer` SignalR hub invoke mungkin gagal, atau locator `input.exam-checkbox[data-question-id="..."][value="..."]` tidak match DOM. Cek `Hubs/AssessmentHub.cs` line 188-252 + hub state log + verifikasi `correctOptionIds` di seed match dengan DOM `data-question-id` + `value`.

### Scenario 6: [MATRIX_TEST_2026_05_11] S6 Online MA only — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s6-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

### Scenario 7: [MATRIX_TEST_2026_05_11] S7 Online Essay only — essay-q50037

- **Severity:** major
- **Expected:** Essay q50037 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** locator.waitFor: Timeout 5000ms exceeded.
Call log:
[2m  - waiting for locator('#saveIndicatorText').filter({ hasText: /saved|tersimpan/i }) to be visible[22m

- **Screenshot:** `test-results/matrix-s7-essay-q50037.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 7: [MATRIX_TEST_2026_05_11] S7 Online Essay only — essay-q50038

- **Severity:** major
- **Expected:** Essay q50038 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** page.fill: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('textarea.exam-essay[data-question-id="50038"]')[22m

- **Screenshot:** `test-results/matrix-s7-essay-q50038.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 7: [MATRIX_TEST_2026_05_11] S7 Online Essay only — essay-q50039

- **Severity:** major
- **Expected:** Essay q50039 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** page.fill: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('textarea.exam-essay[data-question-id="50039"]')[22m

- **Screenshot:** `test-results/matrix-s7-essay-q50039.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 7: [MATRIX_TEST_2026_05_11] S7 Online Essay only — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s7-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

### Scenario 8: [MATRIX_TEST_2026_05_11] [META-AllCorrect] Sentinel — essay-q50045

- **Severity:** major
- **Expected:** Essay q50045 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** page.fill: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('textarea.exam-essay[data-question-id="50045"]')[22m

- **Screenshot:** `test-results/matrix-s8-essay-q50045.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 8: [MATRIX_TEST_2026_05_11] [META-AllCorrect] Sentinel — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s8-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

### Scenario 9: [MATRIX_TEST_2026_05_11] [META-AllWrong] Sentinel — essay-q50051

- **Severity:** major
- **Expected:** Essay q50051 saved via SaveTextAnswer hub (debounce 2s)
- **Actual:** locator.waitFor: Timeout 5000ms exceeded.
Call log:
[2m  - waiting for locator('#saveIndicatorText').filter({ hasText: /saved|tersimpan/i }) to be visible[22m
[2m    15 × locator resolved to hidden <span id="saveIndicatorText">Soal no. 1, saved</span>[22m

- **Screenshot:** `test-results/matrix-s9-essay-q50051.png`
- **Hypothesis:** Essay `SaveTextAnswer` SignalR mungkin gagal atau debounce 2s mismatched dengan helper wait. Cek `Hubs/AssessmentHub.cs` line 134-182 + `Views/CMP/StartExam.cshtml` line 861-904 (2s setTimeout) + textarea event listener.

### Scenario 9: [MATRIX_TEST_2026_05_11] [META-AllWrong] Sentinel — submit-exam

- **Severity:** critical
- **Expected:** SubmitExam redirects to /CMP/Results/{id} OR /CMP/ExamSummary/{id} (incomplete-answers branch)
- **Actual:** page.click: Timeout 10000ms exceeded.
Call log:
[2m  - waiting for locator('#reviewSubmitBtn, [type="submit"]:not(.btn-cancel)')[22m
[2m    - locator resolved to 2 elements. Proceeding with the first one: <button type="submit" b-06zfpy70xb="" class="dropdown-item text-danger fw-bold">…</button>[22m
[2m  - attempting click action[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m    - waiting 20ms[22m
[2m    2 × waiting for element to be visible, enabled and stable[22m
[2m      - element is not visible[22m
[2m    - retrying click action[22m
[2m      - waiting 100ms[22m
[2m    19 × waiting for element to be visible, enabled and stable[22m
[2m       - element is not visible[22m
[2m     - retrying click action[22m
[2m       - waiting 500ms[22m

- **Screenshot:** `test-results/matrix-s9-submit-exam.png`
- **Hypothesis:** SubmitExam mungkin redirect ke `/CMP/ExamSummary/{id}` (timer expired) atau alert banner muncul. Cek `Controllers/CMPController.cs` SubmitExam (line 1569+) + verify timer state pre-submit.

## Meta-validation results

### Scenario 10: [MATRIX_TEST_2026_05_11] [META-CollectorCheck] Sentinel — sentinel-collector-check

- **Severity:** critical
- **Expected:** Sentinel CollectorCheck: collector record meta finding + throw SkipScenarioError
- **Actual:** Sentinel CollectorCheck intentionally fails — verifikasi collector record finding sebagai meta + throw SkipScenarioError untuk test.fail() expected-failure semantics.
- **Screenshot:** `test-results/matrix-s10-sentinel-collector-check.png`
- **Hypothesis:** Hypothesis otomatis tidak tersedia untuk step+actual pattern ini. Periksa screenshot, URL bar, browser console log, dan Playwright trace (kalau di-enable). Catat pattern baru di `deriveHypothesis()` matrixReport.ts untuk reproducibility iterasi berikutnya.
