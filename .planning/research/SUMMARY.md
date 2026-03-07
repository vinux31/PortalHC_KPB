# Research Summary: v3.8 CoachingProton UI Redesign

**Project:** PortalHC KPB
**Domain:** UI button/badge consistency on enterprise approval workflow
**Researched:** 2026-03-07
**Confidence:** HIGH

## Executive Summary

The CoachingProton page has a single core problem: clickable badge elements (`<span class="badge">`) are visually indistinguishable from read-only status badges. Users cannot tell what is actionable versus informational. This is a well-understood UX antipattern with a straightforward fix -- replace badge-as-button elements with proper `<button>` elements and add icons to status badges for accessibility.

No new libraries or build tooling are needed. Bootstrap 5.3 + Bootstrap Icons (already loaded) provide every class and icon required. The entire change is scoped to a single file: `Views/CDP/CoachingProton.cshtml`. Estimated custom CSS is under 30 lines via an inline `<style>` block. This is a single-phase, low-complexity milestone.

The primary risk is breaking JavaScript event handlers. The page uses `querySelectorAll` with class selectors (`.btnTinjau`, `.btnSubmitEvidence`, `.btnHcReview`, `.btnHcReviewPanel`) bound at page load, plus Bootstrap modal `relatedTarget` for data attribute extraction. JS also rebuilds HTML via `innerHTML` after AJAX operations. All existing class names and data attributes must be preserved; new styling classes should be added alongside them, never replacing them.

## Stack Additions

None. The existing stack is sufficient:

- **Bootstrap 5.3.0** -- buttons, badges, utilities (already loaded via CDN)
- **Bootstrap Icons 1.10.0** -- `bi-check-circle`, `bi-x-circle`, `bi-hourglass`, `bi-eye` (already loaded)
- **Vanilla JS (ES6+)** -- existing handlers stay, no new JS library needed
- **~30 lines custom CSS** -- inline `<style>` block with `cp-` prefixed classes

Do NOT add: Tailwind, FontAwesome, Animate.css, jQuery plugins, SASS/LESS, or separate CSS files.

## Feature Table Stakes

| Change | Why |
|--------|-----|
| Replace badge-as-button with `<button>` in SrSpv/SH columns | Users cannot tell clickable badges from status badges |
| Add icons to all status badges | WCAG accessibility -- color alone is insufficient |
| Hover states on all clickable elements | Missing on badge-as-button; required for affordance |
| Muted styling for "not your turn" pending states | Distinguish "waiting for you" from "waiting for someone else" |
| Icon + label on approval action buttons | Small table buttons need both for scannability |

## Feature Differentiators

| Change | Value |
|--------|-------|
| Contextual button colors for approval actions | `outline-warning` for Tinjau, `outline-success` for Review -- matches semantics |
| Status + action stacked in approval cells | Show badge above button in same cell (HC column already does this) |
| Brief fade/highlight on AJAX success | Immediate feedback that action succeeded |

Defer fade/highlight animation and HC Review Panel visual alignment to a later phase if needed.

## Architecture Approach

All changes go in `Views/CDP/CoachingProton.cshtml` (single file, ~1500 lines Razor + ~400 lines JS).

1. **`<style>` block** at top of view -- custom `cp-` prefixed classes for styling. No separate CSS file (no `site.css` pattern exists in this project; scoped CSS breaks with JS `innerHTML`).
2. **HTML element swaps** -- `<span class="badge">` to `<button class="btn btn-sm">` for clickable elements, preserving all `data-*` attributes and existing class names.
3. **JS `innerHTML` string updates** -- AJAX response handlers construct badge HTML with hardcoded classes (lines ~1096-1200, ~1418-1428). These must be updated in the same phase as HTML changes.
4. **Inline `style=` cleanup** -- replace scattered `cursor:pointer` / `min-width` attributes with `cp-` classes.

Key constraint: use `cp-` prefix on all custom classes to avoid collisions with Bootstrap or other views.

## Watch Out For

1. **Breaking modal triggers (CRITICAL)** -- `.btnTinjau` badges use `data-bs-toggle="modal"` + 6 `data-*` attributes read via `event.relatedTarget`. Preserve all attributes when converting to `<button>`. Test with SrSpv AND Section Head roles.

2. **Non-delegated event listeners (CRITICAL)** -- `.btnSubmitEvidence`, `.btnHcReview`, `.btnHcReviewPanel`, `.btnTinjau` are bound via `querySelectorAll` at page load. Do NOT rename these classes. Add new styling classes alongside them.

3. **AJAX DOM updates by ID (CRITICAL)** -- Cells are updated post-AJAX using `getElementById('srspv-{id}')`, `getElementById('sh-{id}')`, etc. Preserve all cell `id` attributes exactly.

4. **Dual HC Review button locations (MODERATE)** -- HC Review exists in both the main table (`.btnHcReview`) and panel below (`.btnHcReviewPanel`). Panel JS removes the inline button after review. Test both locations.

5. **Rowspan breakage (MODERATE)** -- Table uses complex rowspan for Coachee/Kompetensi columns. Keep `btn-sm` consistently; do not increase button padding.

Must-preserve selector inventory (14 selectors): see PITFALLS-COACHING-PROTON-UI.md for the complete reference.

## Recommendation

**Single phase. Low complexity.** All changes are in one file with no backend impact.

### Build Order

1. Add `<style>` block with all `cp-*` class definitions
2. Convert badge-as-button to proper `<button>` in SrSpv/SH columns (highest risk -- do first)
3. Add icons to all status badges
4. Add muted styling for "not your turn" states
5. Update JS `innerHTML` strings to match new markup
6. Polish secondary buttons (Lihat Detail, Kembali, Export)
7. Remove replaced inline `style=` attributes
8. Test all roles: Coach, SrSpv, Section Head, HC

### Research Flags

- **No phase research needed** -- pure CSS/HTML refactor with well-documented Bootstrap patterns
- **Testing emphasis** -- main risk is JS regression. Plan for role-based testing across Coach, SrSpv, SH, and HC flows

### Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | No changes needed; verified against codebase |
| Features | HIGH | Direct codebase analysis + established UX principles |
| Architecture | HIGH | Single-file change, no unknowns |
| Pitfalls | HIGH | JS selectors inventoried line-by-line from source |

**Overall:** HIGH

### Gaps

- **Real data rowspan testing** -- need test data with 5+ deliverables per sub-kompetensi to verify table layout stability
- **Mobile/responsive behavior** -- not researched; may have existing issues unrelated to this redesign

## Sources

- Direct codebase analysis: `Views/CDP/CoachingProton.cshtml`, `Views/Shared/_Layout.cshtml`
- Bootstrap 5 component documentation (buttons, badges)
- WCAG 2.1 SC 1.4.1 (color-only information conveying)
- Nielsen Norman Group affordance principles

---
*Research completed: 2026-03-07*
*Ready for roadmap: yes*
