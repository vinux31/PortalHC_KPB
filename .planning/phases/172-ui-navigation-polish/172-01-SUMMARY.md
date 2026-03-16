---
phase: 172-ui-navigation-polish
plan: "01"
subsystem: ui-guide
tags: [css, razor, styling, guide-pages]
dependency_graph:
  requires: []
  provides: [unified-guide-role-badge, step-variant-blue]
  affects: [wwwroot/css/guide.css, Views/Home/Guide.cshtml, Views/Home/GuideDetail.cshtml]
tech_stack:
  added: []
  patterns: [unified-class-pattern, variant-modifier-pattern]
key_files:
  created: []
  modified:
    - wwwroot/css/guide.css
    - Views/Home/Guide.cshtml
    - Views/Home/GuideDetail.cshtml
decisions:
  - "guide-role-badge is the canonical role badge class; role-badge and guide-step-badge-role are legacy (views updated, CSS aliases kept)"
  - "step-variant-pink removed entirely — replaced by step-variant-blue for CMP/admin modules"
  - "Accordion base styles added as comma-separated selector (.faq-question, .guide-list-btn.accordion-button)"
metrics:
  duration: "8 minutes"
  completed_date: "2026-03-16"
  tasks_completed: 2
  tasks_total: 2
  files_modified: 3
---

# Phase 172 Plan 01: Guide UI Unification Summary

**One-liner:** Unified `.guide-role-badge` class replacing divergent `.role-badge`/`.guide-step-badge-role` badges, replaced dead `.step-variant-pink` with `.step-variant-blue` for CMP/admin modules.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Unify role badges and step badge colors in CSS | 7530f60 | wwwroot/css/guide.css |
| 2 | Update Razor views to use unified classes | 925672f | Views/Home/Guide.cshtml, Views/Home/GuideDetail.cshtml |

## What Was Built

**guide.css changes:**
- Added `.guide-role-badge` — a single unified class for role indicators in both Guide hero and GuideDetail accordion headers. Neutral blue tint (rgba 12% opacity) with matching border, 0.72rem font, 50px border-radius.
- Added `.step-variant-blue` with gradient `#0d6efd → #4dabf7`, hover glow, `@keyframes pulse-glow-blue`, print solid fallback `#0d6efd`, and responsive margin rule.
- Removed `.step-variant-pink` badge rule, hover rule, and `pulse-glow-pink` keyframe entirely.
- Added shared accordion base rule targeting `.faq-question, .guide-list-btn.accordion-button` with unified font-size (0.95rem), padding (0.75rem 1rem), border-radius (0.5rem), and transition.

**View changes:**
- Guide.cshtml: `role-badge` → `guide-role-badge` on hero span; `guide-step-badge-role` → `guide-role-badge` on Kelola Data and Admin Panel card titles.
- GuideDetail.cshtml: All `guide-step-badge-role` → `guide-role-badge`; removed inline `bg-success text-white border-0` overrides on CDP step 5 badge; all `step-variant-pink` → `step-variant-blue` (admin module steps).

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- wwwroot/css/guide.css: modified, contains `guide-role-badge` (2 occurrences), `step-variant-blue` (3 occurrences), no `step-variant-pink`
- Views/Home/Guide.cshtml: contains `guide-role-badge`, no `role-badge` class, no `guide-step-badge-role`
- Views/Home/GuideDetail.cshtml: contains `guide-role-badge`, `step-variant-blue`, no `guide-step-badge-role`, no `step-variant-pink`
- Commits 7530f60 and 925672f verified in git log
