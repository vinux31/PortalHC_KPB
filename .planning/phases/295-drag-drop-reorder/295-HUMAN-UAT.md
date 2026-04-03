---
status: complete
phase: 295-drag-drop-reorder
source: [295-VERIFICATION.md]
started: "2026-04-03"
updated: "2026-04-03"
---

## Current Test

[testing complete]

## Tests

### 1. Drag handle visibility
expected: Hover over a tree row — grip icon appears on the left, hidden by default
result: pass

### 2. Sibling reorder
expected: Drag a unit up/down within same parent — toast "Urutan berhasil diubah" appears, order persists after refresh
result: pass

### 3. Cross-parent blocked
expected: Cannot drag a unit into a different parent's children list (group:false prevents it)
result: pass

### 4. Revert on error
expected: If server returns error, tree refreshes to original server order
result: pass

## Summary

total: 4
passed: 4
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps
