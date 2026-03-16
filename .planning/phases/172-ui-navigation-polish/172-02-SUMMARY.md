---
phase: 172-ui-navigation-polish
plan: 02
subsystem: frontend
tags: [ui, navigation, breadcrumb, back-to-top, guide]
dependency_graph:
  requires: []
  provides: [back-to-top-button, guidedetail-breadcrumb]
  affects: [Guide.cshtml, GuideDetail.cshtml, guide.css]
tech_stack:
  added: []
  patterns: [fixed-position-button, fade-toggle, breadcrumb-nav]
key_files:
  created: []
  modified:
    - wwwroot/css/guide.css
    - Views/Home/Guide.cshtml
    - Views/Home/GuideDetail.cshtml
decisions:
  - Replaced back-button on GuideDetail with full breadcrumb (Beranda > Panduan > Module) so Panduan link serves as back navigation
  - Removed orphaned .guide-detail-nav and .guide-back-btn CSS rules — no longer referenced anywhere
metrics:
  duration: ~10 minutes
  completed: 2026-03-16
  tasks_completed: 2
  tasks_total: 2
  files_modified: 3
---

# Phase 172 Plan 02: UI Navigation Polish — Back-to-Top and Breadcrumb Summary

**One-liner:** Floating back-to-top button (fade in after 300px scroll) added to Guide and GuideDetail; GuideDetail back button replaced with three-level breadcrumb using .guide-breadcrumb CSS.

## Tasks Completed

| Task | Name | Commit | Files |
| ---- | ---- | ------ | ----- |
| 1 | Add back-to-top button to Guide and GuideDetail | 3b603d7 | wwwroot/css/guide.css, Views/Home/Guide.cshtml, Views/Home/GuideDetail.cshtml |
| 2 | Add breadcrumb to GuideDetail page | 09b3711 | Views/Home/GuideDetail.cshtml, wwwroot/css/guide.css |

## What Was Built

### Back-to-Top Button (Task 1)
- Added `.guide-back-to-top` CSS: fixed bottom-right (2rem), 44px circle, white bg, border-radius 50%, opacity 0/hidden by default, transitions to visible on `.visible` class
- Mobile responsive: 38px, 1rem inset at max-width 768px
- Print media: `display: none`
- Added identical button markup to Guide.cshtml and GuideDetail.cshtml with `id="backToTopBtn"`
- JS: scroll listener toggles `.visible` class when `window.scrollY > 300`, click handler calls `window.scrollTo({ top: 0, behavior: 'smooth' })`

### Breadcrumb Navigation (Task 2)
- Added `moduleBreadcrumb` string variable to GuideDetail.cshtml Razor block
- Switch cases: cmp → "CMP", cdp → "CDP", account → "Akun", data → "Kelola Data", admin → "Admin Panel"
- Replaced `<nav class="guide-detail-nav">` back-button with `<nav class="guide-breadcrumb">` breadcrumb: Beranda > Panduan > [moduleBreadcrumb]
- Uses existing `.guide-breadcrumb` CSS already defined in guide.css (with mobile responsive rules)
- Panduan item links to `asp-controller="Home" asp-action="Guide"` — serves as back navigation
- Removed orphaned `.guide-detail-nav`, `.guide-back-btn`, `.guide-back-btn:hover` CSS rules and all other stray references (reduced-motion, mobile, print, focus-visible blocks)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Cleanup] Removed all orphaned .guide-back-btn CSS references**
- **Found during:** Task 2
- **Issue:** After removing `.guide-back-btn` definition, stray selectors remained in reduced-motion block, mobile @media block, print block, and focus-visible block
- **Fix:** Removed all 4 orphaned selector references
- **Files modified:** wwwroot/css/guide.css
- **Commit:** 09b3711

## Self-Check

### Created/Modified Files
- [x] wwwroot/css/guide.css — FOUND
- [x] Views/Home/Guide.cshtml — FOUND
- [x] Views/Home/GuideDetail.cshtml — FOUND

### Commits
- [x] 3b603d7 — feat(172-02): add back-to-top button
- [x] 09b3711 — feat(172-02): add breadcrumb to GuideDetail

## Self-Check: PASSED
