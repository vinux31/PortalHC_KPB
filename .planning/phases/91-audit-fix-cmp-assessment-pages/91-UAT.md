---
status: complete
phase: 91-audit-fix-cmp-assessment-pages
source: [91-01-SUMMARY.md, 91-02-SUMMARY.md]
started: 2026-03-04T08:00:00Z
updated: 2026-03-04T08:30:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Records Page Redesign
expected: Navigate to CMP/Records as Worker. Breadcrumb shows CMP > Assessment > Records. Stat cards: Assessment Online / Training Manual / Total Records. Two tabs present. Assessment rows clickable to CMP/Results. Training Manual tab shows training rows.
result: pass

### 2. Results Back Button
expected: Navigate to CMP/Results?id=X (Worker path, no returnUrl). "Kembali" button goes to CMP/Assessment. Then navigate via Admin/UserAssessmentHistory link (which includes returnUrl). "Kembali" button goes back to Admin page instead.
result: pass

### 3. Certificate Back Button
expected: Navigate to CMP/Certificate?id=X. "Kembali" button goes to CMP/Assessment by default. If accessed with ?returnUrl=..., it goes to that URL instead.
result: pass

### 4. Token Verification CSRF
expected: Login as Worker with an "Open + Token Required" assessment. Click the assessment card, enter correct token, click Verify. Token verification succeeds and exam loads (no 400 Bad Request). DevTools confirms POST to /CMP/VerifyToken includes RequestVerificationToken header.
result: pass

### 5. HC Exam Submission
expected: Login as HC user. Navigate to /CMP/StartExam?id=X for an open assessment. HC user can access the exam page (no 403). Can submit exam without 403.
result: pass

### 6. Auto-Save Retry
expected: Start an exam as Worker. Select an answer. Save indicator briefly shows "saving" then "saved" (bottom area). On network hiccup, retries up to 3 times with exponential backoff before showing error warning.
result: pass

### 7. Force-Close Modal
expected: Worker starts an exam. From a separate Admin/HC session, force-close the assessment. Within ~10 seconds, Worker's exam shows a modal dialog "Ujian Ditutup — Ujian ini telah ditutup oleh HC/Administrator" with an OK button. Clicking OK redirects worker away. Not a silent banner redirect.
result: pass

### 8. Single-Package Question Shuffle
expected: Assessment with a single package and multiple questions. Two different Workers start the exam. Question order is different between the two workers.
result: pass

### 9. Option Shuffle and Scoring
expected: Worker starts a package-based exam. A/B/C/D option order for the same question appears different compared to another worker's session. Scoring still works correctly — submitting correct answer gives expected score.
result: pass

## Summary

total: 9
passed: 9
issues: 0
pending: 0
skipped: 0

## Gaps

[none]
