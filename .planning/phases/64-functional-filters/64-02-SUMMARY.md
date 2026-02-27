---
phase: 64-functional-filters
plan: 02
subsystem: ui
tags: [razor, aspnetcore, bootstrap, client-side-search, cascading-dropdowns, role-conditional-ui]

# Dependency graph
requires:
  - phase: 64-functional-filters
    plan: 01
    provides: ProtonProgress action with 5 filter params, ViewBag.AllBagian/AllUnits/AllTracks/AllTahun/Coachees/Selected* values, TotalCount/FilteredCount

provides:
  - ProtonProgress.cshtml with GET form filter bar (Bagian/Unit/Coachee/Track/Tahun/Search/Reset)
  - Role-conditional filter visibility in view (Level 1-2 sees all, Level 4 sees Unit, Level 5 sees Coachee, Level 6 sees none)
  - Bagian->Unit JS cascade (clears unit before submit)
  - Client-side debounced search (300ms, data-kompetensi/data-deliverable attributes)
  - Multi-coachee table view with Coachee column when no specific coachee selected
  - Result count display: "Menampilkan X dari Y data"

affects:
  - 65-actions (action buttons wired to backend use same table structure)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "GET form auto-submit: each dropdown onchange calls this.form.submit() for URL param navigation"
    - "Bagian cascade: JS clears unitSelect innerHTML then repopulates before form.submit()"
    - "Client-side search: 300ms setTimeout debounce, querySelectorAll tr, row.style.display toggle"
    - "Razor selected pattern: if/else blocks on each option (no ternary) to avoid RZ1031"
    - "Pre-compute groupings in top-level @{} block — avoids nested @{} inside @if/@else (RZ1010)"

key-files:
  created: []
  modified:
    - Views/CDP/ProtonProgress.cshtml

key-decisions:
  - "Pre-compute coacheeGroups and kompetensiGroups in top-level @{} block: nested @{} inside @if/@else causes RZ1010; variables declared in inner {} block are scoped out of @foreach reach"
  - "showCoacheeColumn = empty selectedCoacheeId AND userLevel < 6: determines whether multi-coachee or single-coachee table layout renders"
  - "Search input has no name attribute: client-side only, never submitted with GET form"
  - "clearFilters() navigates to @Url.Action('ProtonProgress', 'CDP') with no params — server returns full unfiltered scope for the role"

patterns-established:
  - "Razor grouping: pre-compute in @{} top block, use in @foreach below — avoids RZ1010 nested code block error"
  - "Role-conditional filter bar: userLevel <= 2 for HC/Admin, == 4 for SrSpv, == 5 for Coach, < 6 for all non-Coachee"

requirements-completed: [FILT-01, FILT-02, FILT-03, FILT-04, UI-01, UI-03]

# Metrics
duration: 3min
completed: 2026-02-27
---

# Phase 64 Plan 02: Functional Filters — View Summary

**GET form filter bar with role-conditional dropdowns, Bagian->Unit JS cascade, 300ms debounced client-side search, and server-side Razor rowspan table replacing Phase 63 AJAX model**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-27T04:41:43Z
- **Completed:** 2026-02-27T04:44:41Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments

- Rewrote ProtonProgress.cshtml (502 lines) replacing the Phase 63 AJAX fetch() coachee-switching model with a GET form that auto-submits on every dropdown change
- Implemented role-conditional filter visibility: HC/Admin sees all 5 filters, SrSpv sees Unit/Track/Tahun/Search, Coach sees Coachee/Track/Tahun/Search, Spv sees Track/Tahun/Search only, Coachee sees no filters
- Bagian->Unit cascade: JavaScript clears the unit dropdown options and repopulates from unitsByBagian map before submitting, preventing stale cascade values
- Client-side search with 300ms debounce filters table rows by data-kompetensi and data-deliverable attributes without page reload
- Multi-coachee table shows Coachee column with rowspan grouping when no specific coachee is selected; single-coachee view hides the column

## Task Commits

1. **Task 1: Rewrite ProtonProgress.cshtml with GET form filter bar and cascading dropdowns** - `9a13317` (feat)

**Plan metadata:** (pending final commit)

## Files Created/Modified

- `Views/CDP/ProtonProgress.cshtml` - Full rewrite: GET form filter bar, role-conditional dropdowns, Bagian->Unit cascade JS, debounced search, server-side rowspan table, Phase 63 AJAX removed

## Decisions Made

- Pre-compute coacheeGroups and kompetensiGroups in the top-level `@{}` block: discovered that `@{}` nested inside `@if/@else` causes RZ1010, and variables declared in plain inner `{}` blocks are scoped out of reach of subsequent `@foreach` — single top-level declaration avoids both issues
- showCoacheeColumn flag controls table layout: empty selectedCoacheeId AND userLevel < 6 triggers multi-coachee view with Coachee column and triple-group rowspan
- Search input intentionally has no `name` attribute so it is never submitted with the GET form — client-side only per plan spec
- clearFilters() uses `@Url.Action("ProtonProgress", "CDP")` with no params, relying on the server to return the full role-scoped data set as the "reset" state

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed RZ1010: removed nested @{} inside @if/@else blocks**
- **Found during:** Task 1 (first build attempt)
- **Issue:** Plan spec showed `@{}` code blocks inside `@if (showCoacheeColumn)` and `@else` blocks, which Razor rejects with RZ1010 "Unexpected { after @ character"
- **Fix:** Moved both coacheeGroups and kompetensiGroups LINQ groupings into the top-level `@{}` block at the start of the view; removed the inner nested `@{}` blocks entirely
- **Files modified:** Views/CDP/ProtonProgress.cshtml
- **Verification:** Build 0 errors after fix
- **Committed in:** 9a13317 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - Bug)
**Impact on plan:** Fix required for compilation. No scope change — logic is identical, just declared at correct Razor scope level.

## Issues Encountered

None beyond the RZ1010 auto-fix.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- ProtonProgress.cshtml is fully rewritten with functional GET form filters
- All 6 plan requirements completed: FILT-01 through FILT-04, UI-01, UI-03
- Phase 65 (Actions) can wire approve/reject/coaching/evidence buttons into the existing table action dropdown
- Build passes with 0 errors

## Self-Check: PASSED

- FOUND: Views/CDP/ProtonProgress.cshtml (502 lines, > 350 minimum)
- FOUND commit: 9a13317 (feat(64-02): rewrite ProtonProgress.cshtml with GET form filter bar)
- FOUND: form method="get" at line 65
- FOUND: onBagianChange at line 72, this.form.submit() at lines 92/112/132/152
- FOUND: if/else selected pattern at lines 76/96/116/136/156
- FOUND: data-kompetensi at lines 301/358
- FOUND: Menampilkan at line 193
- NOT FOUND: fetch/GetCoacheeDeliverables/escapeHtml/messageArea (AJAX removed)

---
*Phase: 64-functional-filters*
*Completed: 2026-02-27*
