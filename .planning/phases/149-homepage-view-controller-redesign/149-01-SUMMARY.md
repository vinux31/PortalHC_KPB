---
phase: 149-homepage-view-controller-redesign
plan: 01
subsystem: homepage
tags: [homepage, viewmodel, controller, css, redesign]
dependency_graph:
  requires: [148-css-audit-cleanup]
  provides: [simplified-homepage]
  affects: [Views/Home/Index.cshtml, Controllers/HomeController.cs, Models/DashboardHomeViewModel.cs, wwwroot/css/home.css]
tech_stack:
  added: []
  patterns: [Bootstrap card border-0 shadow-sm h-100, Bootstrap Icons bi-*]
key_files:
  modified:
    - Models/DashboardHomeViewModel.cs
    - Controllers/HomeController.cs
    - Views/Home/Index.cshtml
    - wwwroot/css/home.css
decisions:
  - Removed _context and ApplicationDbContext DI entirely from HomeController since no remaining action uses it
  - Kept Font Awesome icons in hero section (consistent with existing hero markup)
  - Used Bootstrap Icons (bi-*) for Quick Access cards to match CMP/CDP Index page pattern
  - Removed hero-stats and hero-stat-item CSS rules as they had no HTML usage in the new view
metrics:
  duration: ~10 minutes
  completed: "2026-03-10T08:50:37Z"
  tasks_completed: 2
  files_modified: 4
---

# Phase 149 Plan 01: Homepage View/Controller Redesign Summary

**One-liner:** Replaced glassmorphism dashboard (6 DB queries, 3 cards, timeline, deadlines) with minimal hero + 3 Bootstrap Quick Access cards and 2-property ViewModel.

## What Was Implemented

Complete homepage redesign removing all data-heavy sections. The homepage now shows only:
1. Hero section: purple gradient background, greeting, full name, position badge, unit badge, current date
2. Three Quick Access cards: CDP, Assessment, CMP — using Bootstrap `card border-0 shadow-sm h-100` pattern

## Files Modified

### Models/DashboardHomeViewModel.cs
- **Before:** 64 lines — 9 properties + 3 helper class definitions (TrainingStatusInfo, RecentActivityItem, DeadlineItem)
- **After:** 11 lines — 2 properties only (CurrentUser, Greeting)
- **Delta:** -53 lines

### Controllers/HomeController.cs
- **Before:** 321 lines — Index() with 6 DB queries, 4 private methods (GetMandatoryTrainingStatus, GetRecentActivities, GetUpcomingDeadlines, GetTimeAgo), ApplicationDbContext DI
- **After:** 75 lines — Index() with single user fetch + ViewModel construction, removed _context field and all helper methods
- **Delta:** -246 lines

### Views/Home/Index.cshtml
- **Before:** 299 lines — hero section + 3 glassmorphism cards (IDP, Assessment, Training) + Quick Access section + Recent Activity timeline + Upcoming Deadlines section
- **After:** 59 lines — hero section + 3 Bootstrap Quick Access cards only
- **Delta:** -240 lines

### wwwroot/css/home.css
- **Before:** 264 lines — hero rules + circular-progress + quick-access-card + section-header + gradient-text + badge-gradient + responsive
- **After:** 72 lines — hero rules only (hero-section, hero-content, hero-avatar, hero-greeting, hero-subtitle, hero-badge) + responsive
- **Delta:** -192 lines

## Verification Results

```
grep removed symbols across DashboardHomeViewModel.cs, HomeController.cs, Views/Home/Index.cshtml:
  → 0 results (PASS)

grep "card border-0 shadow-sm" Views/Home/Index.cshtml:
  → 3 (PASS — one per Quick Access card)

grep "hero-section|hero-greeting" Views/Home/Index.cshtml:
  → 2 (PASS — hero section present)

grep circular-progress|gradient-text|section-header|badge-gradient|quick-access-card in home.css:
  → 0 (PASS)
```

## Deviations from Plan

### Auto-fixed Issues

None — plan executed exactly as written.

### Discretion Decisions

1. **CSS hero-stats rules removed** — The plan said to keep hero rules but `hero-stats` and `hero-stat-item` had no corresponding HTML in the new view, so they were removed to keep the file clean.
2. **Removed using Microsoft.EntityFrameworkCore** — Per plan instruction, verified no other action in the file used EF extension methods before removing.

## Commits

- `1562a9b`: feat(149-01): simplify ViewModel and Controller for homepage redesign
- `bc31275`: feat(149-01): rewrite homepage view and strip unused CSS

## Awaiting

Task 3 (checkpoint:human-verify) requires visual verification of the redesigned homepage in the browser.

## Self-Check: PASSED

- Models/DashboardHomeViewModel.cs: FOUND
- Controllers/HomeController.cs: FOUND
- Views/Home/Index.cshtml: FOUND
- wwwroot/css/home.css: FOUND
- Commit 1562a9b: FOUND
- Commit bc31275: FOUND
