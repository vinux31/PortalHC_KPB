---
phase: 280
plan: 01
status: completed
---

# Summary: Anti-Copy Protection pada StartExam

## Changes
- **Views/CMP/StartExam.cshtml**: Added `exam-protected` class to question area div, CSS `user-select: none` rules, and JS IIFE blocking copy/cut/paste/contextmenu/selectstart/dragstart events + Ctrl+C/A/U/S/P keyboard shortcuts.

## Verification
- Build: 0 errors
- `exam-protected` class: 3 occurrences (div, CSS, JS)
- `user-select: none`: 4 vendor-prefixed rules
- Event blocking: contextmenu, selectstart, dragstart confirmed
- Keyboard blocking: keyCode check for [67, 65, 85, 83, 80] confirmed
- Silent block (no alert/confirm): confirmed
- Sidebar/header NOT protected: confirmed (class only on col-lg-9)
