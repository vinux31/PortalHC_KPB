---
status: complete
phase: 27-monitoring-status-fix
source: [27-01-SUMMARY.md]
started: 2026-02-21T00:00:00Z
updated: 2026-02-21T00:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. In Progress worker shows "In Progress"
expected: Open the monitoring tab for an assessment where a worker has started but not submitted. Their status should read "In Progress", not "Not started".
result: pass

### 2. Abandoned worker appears in the monitoring list
expected: A worker whose session was Force Closed / Abandoned should be VISIBLE in the monitoring card list. Previously they were excluded from the WHERE clause and would not appear at all.
result: pass

### 3. Abandoned worker shows "Abandoned" (not "In Progress")
expected: An Abandoned worker's UserStatus should be "Abandoned" — not "In Progress". This is critical because Abandoned sessions have StartedAt set and would be misclassified without the ordering fix.
result: pass

### 4. Completed worker still shows "Completed" (no regression)
expected: A worker who submitted and completed the exam still shows "Completed" and is included in the completedCount. Nothing should have broken for the happy path.
result: pass

## Summary

total: 4
passed: 4
issues: 0
pending: 0
skipped: 0

## Gaps

[none yet]
