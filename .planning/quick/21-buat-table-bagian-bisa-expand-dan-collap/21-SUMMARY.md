---
phase: quick-21
plan: 01
subsystem: proton-data
tags: [ui, expand-collapse, status-tab]
key-files:
  modified:
    - Views/ProtonData/Index.cshtml
decisions: []
metrics:
  completed: 2026-03-09
  tasks: 1/1
---

# Quick 21: Expand/Collapse Bagian Rows in Status Tab Summary

Bagian rows in ProtonData Status tab now expand/collapse child Unit and Track rows via chevron click, defaulting to collapsed.

## Task Results

| Task | Name | Commit | Status |
|------|------|--------|--------|
| 1 | Add expand/collapse to Bagian rows | b858ad1 | Done |

## Changes Made

- Bagian rows: Added `bagian-toggle` class, chevron icon (`bi-chevron-right`), cursor pointer, and `data-bagian-id`
- Unit rows: Added `bagian-child` class, `data-bagian` attribute, hidden by default
- Track rows: Added `bagian-child` class, `data-bagian` attribute, hidden by default
- Click handler: Delegated via `$('#statusTableBody').on('click', '.bagian-toggle', ...)` toggles children and swaps chevron icon

## Deviations from Plan

None - plan executed exactly as written.

## Self-Check: PASSED
