---
phase: quick-24
plan: "01"
one_liner: "Remove No column from PlanIdp silabus table"
---

# Quick Task 24: Hide No Column from PlanIdp Silabus Table

## Changes

Removed the "No" column from the silabus table on the PlanIdp page (`/CDP/PlanIdp`):

1. Removed `<th style="width:60px">No</th>` from table header
2. Removed the `<td>` cell that rendered `dRow.No` with rowspan

## Files Modified
- `Views/CDP/PlanIdp.cshtml` (2 lines removed from JS table renderer)
