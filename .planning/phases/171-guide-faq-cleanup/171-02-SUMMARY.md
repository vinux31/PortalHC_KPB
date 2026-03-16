---
phase: 171-guide-faq-cleanup
plan: "02"
subsystem: ui
tags: [razor, bootstrap, faq, guide, dynamic-counts]

requires:
  - phase: 171-guide-faq-cleanup plan 01
    provides: Simplified GuideDetail accordions with exact item counts per role

provides:
  - Dynamic guide card counts via Razor variables per module per role
  - FAQ expand/collapse all toggle button
  - FAQ categories reordered: Akun & Login > Assessment > CDP & Coaching > Umum > KKJ & CPDP > Admin & Kelola Data
  - Redundant FAQ items removed (step-by-step flows covered by PDF tutorials)
  - Sequential FAQ IDs after cleanup

affects: [guide-page, faq]

tech-stack:
  added: []
  patterns:
    - "Razor int variables for role-conditional counts (isAdminOrHc ternary)"
    - "Bootstrap Collapse.getOrCreateInstance for programmatic FAQ toggle"

key-files:
  created: []
  modified:
    - Views/Home/Guide.cshtml

key-decisions:
  - "FAQ category order: Akun & Login, Assessment, CDP & Coaching, Umum, KKJ & CPDP, Admin & Kelola Data — Umum moved before KKJ as more universally applicable"
  - "Removed step-by-step FAQ items covered by PDF tutorials; kept conceptual/policy FAQ items"

patterns-established:
  - "Guide card counts must reflect actual GuideDetail accordion counts — derive from Plan 01 output"

requirements-completed: [GUIDE-03, FAQ-01, FAQ-02, FAQ-03]

duration: 25min
completed: 2026-03-16
---

# Phase 171 Plan 02: Guide FAQ Cleanup Summary

**Dynamic guide card counts via Razor role variables, FAQ expand/collapse toggle, reordered FAQ categories (Umum moved before KKJ), and removal of step-by-step FAQ items covered by PDF tutorials**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-03-16
- **Completed:** 2026-03-16
- **Tasks:** 3 (2 auto + 1 checkpoint)
- **Files modified:** 1

## Accomplishments
- Guide card counts dynamically reflect actual guide counts per module per role (no hardcoded strings)
- "Buka Semua / Tutup Semua" toggle button expands/collapses all FAQ answers using Bootstrap Collapse API
- FAQ reordered to: Akun & Login > Assessment > CDP & Coaching > Umum > KKJ & CPDP > Admin & Kelola Data
- Duplicate FAQ items removed (assessment flow steps, coaching flow steps covered by PDF tutorials)
- FAQ IDs renumbered sequentially after removals

## Task Commits

Each task was committed atomically:

1. **Task 1: Dynamic card counts and FAQ expand/collapse toggle** - `cede70e` (feat)
2. **Task 2: Reorder FAQ categories, clean up redundant items** - `1c1f87f` (feat)
3. **Task 3: Verify Guide page changes** - checkpoint approved by user

## Files Created/Modified
- `Views/Home/Guide.cshtml` - Dynamic counts, FAQ toggle button, reordered/cleaned FAQ

## Decisions Made
- Umum category moved before KKJ & CPDP (more universally applicable content)
- Removed FAQ items describing step-by-step assessment and coaching flows — these are fully covered by the PDF tutorial cards added in Plan 01

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Guide page overhaul (Phase 171) complete
- Both plans (01 + 02) delivered: simplified accordions, tutorial cards, dynamic counts, improved FAQ
- Ready for next phase

---
*Phase: 171-guide-faq-cleanup*
*Completed: 2026-03-16*
