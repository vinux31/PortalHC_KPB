---
phase: 148-css-audit-cleanup
plan: 01
subsystem: ui
tags: [css, cleanup, glassmorphism, aos, homepage]

# Dependency graph
requires: []
provides:
  - home.css cleaned of glassmorphism rules (glass-card, backdrop-filter, blur pseudo-elements)
  - home.css cleaned of timeline and deadline-card component rules
  - Home/Index.cshtml free of data-aos animation attributes
affects: [149-homepage-redesign]

# Tech tracking
tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - wwwroot/css/home.css
    - Views/Home/Index.cshtml

key-decisions:
  - "Removed .hero-section::before/::after pseudo-elements (blur decorations) but preserved .hero-section base rule for Phase 149 reuse"
  - "Removed backdrop-filter: blur declarations from .hero-badge and .hero-stat-item individually, kept surrounding rule blocks intact"
  - "Guide.cshtml data-aos attributes deliberately untouched — AOS library stays in _Layout.cshtml"

patterns-established: []

requirements-completed: [CSS-01, CSS-02, CSS-03]

# Metrics
duration: 10min
completed: 2026-03-10
---

# Phase 148 Plan 01: CSS Audit & Cleanup Summary

**Deleted 249 lines of dead CSS (glassmorphism, timeline, deadline-card blocks) from home.css and stripped all data-aos attributes from Home/Index.cshtml to prepare a clean base for Phase 149's HTML redesign.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-03-10T
- **Completed:** 2026-03-10T
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- Removed 113 lines of glassmorphism CSS (glass-card block, card-icon-wrapper block, blur pseudo-elements, backdrop-filter declarations)
- Removed 136 lines of timeline and deadline-card CSS (19 selectors total)
- Stripped 10 data-aos/data-aos-delay attributes from Home/Index.cshtml, preserving all HTML structure
- Confirmed .hero-section base rule intact (background gradient, padding, border-radius, box-shadow)
- Confirmed Guide.cshtml retains all 10 of its data-aos attributes

## Task Commits

Each task was committed atomically:

1. **Task 1: Remove glassmorphism CSS rules** - `667caf5` (feat)
2. **Task 2: Remove timeline and deadline-card CSS rules** - `733da52` (feat)
3. **Task 3: Strip data-aos attributes from Home/Index.cshtml** - `cd5f726` (feat)

## Files Created/Modified
- `wwwroot/css/home.css` - Reduced from 467 lines; glassmorphism (glass-card, ::before/::after blur, backdrop-filter) and timeline/deadline sections removed; hero base, quick-access, circular-progress, section-header rules preserved
- `Views/Home/Index.cshtml` - data-aos attributes removed from all 10 elements; HTML structure unchanged

## Decisions Made
- Kept .hero-badge and .hero-stat-item rule blocks after removing their backdrop-filter lines (rules are non-empty, still used for background/border/padding styling)
- Both hero-section responsive rules in @media block were retained (they reference the base rule, not glassmorphism)

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

```
=== CSS-01: Glassmorphism check ===
0 matches — PASS
=== CSS-02: Timeline/deadline check ===
0 matches — PASS
=== CSS-03: data-aos on Homepage check ===
0 matches — PASS
=== Hero base preserved ===
2 matches — PASS (hero base present in both base and responsive blocks)
=== Guide.cshtml data-aos untouched ===
10 matches — PASS (Guide data-aos intact)
```

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- home.css is free of all removed class references — Phase 149 can safely introduce new Bootstrap card patterns without CSS conflicts
- .hero-section base rule is intact and available for Phase 149's redesigned hero HTML
- Homepage loads without JS errors (AOS library still in _Layout; no data-aos attributes on homepage means AOS.init() runs silently with nothing to animate)
- Glass-card HTML classes still in Index.cshtml (Phase 149 will replace them) — no visual breakage since home.css no longer styles them; Bootstrap card fallback rendering applies

---
*Phase: 148-css-audit-cleanup*
*Completed: 2026-03-10*
