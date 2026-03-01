---
phase: 76-role-fixes-broken-link
plan: 01
subsystem: ui
tags: [razor, role-authorization, bootstrap-tabs, admin-hub]

# Dependency graph
requires:
  - phase: 75-placeholder-cleanup
    provides: Admin/Index.cshtml with stub cards removed, clean starting point for role fixes
provides:
  - Role-conditional card rendering in Admin/Index.cshtml (User.IsInRole("Admin"))
  - ProtonDataController.Index accepts string? tab query param and passes to ViewBag.ActiveTab
  - ProtonData/Index.cshtml activates tab from ViewBag.ActiveTab on page load
affects: [admin-hub, proton-data, deliverable-progress-override]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "User.IsInRole(\"Admin\") Razor conditionals for role-gated card visibility"
    - "Query param tab activation via ViewBag.ActiveTab instead of hash-based approach"

key-files:
  created: []
  modified:
    - Views/Admin/Index.cshtml
    - Controllers/ProtonDataController.cs
    - Views/ProtonData/Index.cshtml

key-decisions:
  - "Wrap each Admin-only card col-md-4 div individually inside @if (User.IsInRole(\"Admin\")) — keeps Section structure (headings, badges, hr) intact for both roles"
  - "Use Url.Action(\"Index\", \"ProtonData\", new { tab = \"override\" }) for Deliverable Progress Override href — generates /ProtonData/Index?tab=override reliably"
  - "Keep hash-based fallback in ProtonData/Index.cshtml for backward compatibility with any existing bookmarks"

patterns-established:
  - "Role-gated cards: @if (User.IsInRole(\"Admin\")) wraps the col-md-4 div, not the card internals"
  - "Cross-page tab activation: use query param passed via ViewBag, not window.location.hash"

requirements-completed: [ROLE-01, LINK-01]

# Metrics
duration: 2min
completed: 2026-03-01
---

# Phase 76 Plan 01: Role Fixes & Broken Link Summary

**Admin hub now hides 4 Admin-only cards from HC users using Razor role checks, and Deliverable Progress Override navigates reliably to the Override tab via ?tab=override query param**

## Performance

- **Duration:** 2 min
- **Started:** 2026-03-01T05:26:00Z
- **Completed:** 2026-03-01T05:28:00Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- HC users visiting /Admin now see only 3 cards: Manajemen Pekerja, Silabus & Coaching Guidance, Deliverable Progress Override
- Admin users continue seeing all 7 cards (no regression)
- Deliverable Progress Override card link changed from broken `/ProtonData/Index#override` to `Url.Action` with `tab=override` query param
- ProtonDataController.Index now accepts `string? tab` parameter and sets `ViewBag.ActiveTab`
- ProtonData/Index.cshtml activates the correct tab on page load using server-provided value, with hash fallback for backward compat

## Task Commits

Each task was committed atomically:

1. **Task 1: Hide Admin-only cards from HC users in Admin hub** - `e15bfe6` (feat)
2. **Task 2: Add tab query param to ProtonDataController.Index and activate in view** - `cf8e351` (feat)

## Files Created/Modified
- `Views/Admin/Index.cshtml` - Added `@if (User.IsInRole("Admin"))` wrapping KKJ Matrix, KKJ-IDP Mapping, Coach-Coachee Mapping, and Manage Assessments cards; fixed Deliverable Progress Override href
- `Controllers/ProtonDataController.cs` - Added `string? tab` param to Index action; added `ViewBag.ActiveTab = tab` before return
- `Views/ProtonData/Index.cshtml` - Added `activeTab` Razor variable; replaced hash-based tab activation with query-param approach plus hash fallback

## Decisions Made
- Wrapped each Admin-only card's `col-md-4` div individually inside `@if (User.IsInRole("Admin"))` — keeps Section headings, badges, and `<hr>` separators visible for both roles (Section C heading shows even if HC, but the card row is empty)
- Used `Url.Action("Index", "ProtonData", new { tab = "override" })` instead of hard-coded URL — consistent with rest of Admin hub link pattern
- Kept hash-based fallback in the view alongside the new query-param approach — backward-compatible for any existing bookmarks or direct links

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. Build passed 0 errors on first attempt for both tasks.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Phase 76 Plan 01 complete (ROLE-01, LINK-01 resolved)
- Remaining: ROLE-02 (from Phase 76 Plan 02, if it exists) — check ROADMAP.md
- HC users can now safely use the Admin hub without hitting access-denied errors

## Self-Check

Files verified:
- `Views/Admin/Index.cshtml` — exists with `User.IsInRole("Admin")` conditionals
- `Controllers/ProtonDataController.cs` — exists with `string? tab` parameter and `ViewBag.ActiveTab = tab`
- `Views/ProtonData/Index.cshtml` — exists with `activeTab` variable and query-param tab activation

Commits verified:
- `e15bfe6` — feat(76-01): hide admin-only cards from HC users in Admin hub
- `cf8e351` — feat(76-01): add tab query param to ProtonDataController and activate in view

## Self-Check: PASSED

---
*Phase: 76-role-fixes-broken-link*
*Completed: 2026-03-01*
