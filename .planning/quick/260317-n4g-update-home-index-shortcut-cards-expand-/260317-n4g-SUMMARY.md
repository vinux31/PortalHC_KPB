---
phase: quick
plan: 260317-n4g
subsystem: Views/Home
tags: [ui, shortcut-cards, labels, icons]
key-decisions:
  - Used small.fw-semibold instead of h6 to accommodate longer portal names without overflow
key-files:
  modified:
    - Views/Home/Index.cshtml
metrics:
  duration: ~3min
  completed: 2026-03-17
  tasks_completed: 1
  files_modified: 1
---

# Quick Task 260317-n4g: Update Home/Index Shortcut Cards — Expand Labels

CDP and CMP shortcut cards now display full portal names with representative icons (bi-person-gear and bi-mortarboard).

## Tasks Completed

| # | Task | Commit | Notes |
|---|------|--------|-------|
| 1 | Update CDP and CMP shortcut card labels and icons | 71740fe | h6 -> small.fw-semibold for label fit |

## Changes Made

- **CDP card:** icon `bi-calendar-check` -> `bi-person-gear`; label `CDP` -> `Competency Development Portal`
- **CMP card:** icon `bi-book` -> `bi-mortarboard`; label `CMP` -> `Competency Management Portal`
- **Assessment card:** unchanged
- **Typography:** `h6` replaced with `small.fw-semibold` to prevent longer text from overflowing the card

## Deviations from Plan

None — plan executed exactly as written. The h6->small change was explicitly suggested in the plan's action description.

## Self-Check: PASSED

- Views/Home/Index.cshtml modified and committed (71740fe)
- No C# compiler errors (MSB3021 is a file-lock on running process, not a compilation error)
