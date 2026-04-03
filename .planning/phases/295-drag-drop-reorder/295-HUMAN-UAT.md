---
status: partial
phase: 295-drag-drop-reorder
source: [295-VERIFICATION.md]
started: "2026-04-03"
updated: "2026-04-03"
---

## Current Test

[awaiting human testing]

## Tests

### 1. Drag handle visibility
expected: Hover over a tree row — grip icon appears on the left, hidden by default
result: [pending]

### 2. Sibling reorder
expected: Drag a unit up/down within same parent — toast "Urutan berhasil diubah" appears, order persists after refresh
result: [pending]

### 3. Cross-parent blocked
expected: Cannot drag a unit into a different parent's children list (group:false prevents it)
result: [pending]

### 4. Revert on error
expected: If server returns error, tree refreshes to original server order
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
