---
phase: 67-dynamic-profile-page
plan: 01
subsystem: ui
tags: [razor, aspnet-identity, bootstrap, profile, ApplicationUser]

# Dependency graph
requires:
  - phase: AccountController
    provides: Profile() action passing ApplicationUser as @Model with ViewBag.UserRole
  - phase: _Layout.cshtml
    provides: initials algorithm (Split/RemoveEmptyEntries/3-branch logic)
provides:
  - Dynamic profile page bound to @model ApplicationUser — no hardcoded placeholders
  - Two-section layout (Identitas + Organisasi) with flat label-value rows
  - Null-safe em dash fallback for all optional fields
  - Avatar initials matching navbar algorithm
affects: [future profile edit/settings, user data display]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "@model HcPortal.Models.ApplicationUser Razor view binding"
    - "Null-safe field display with string.IsNullOrEmpty + text-muted em dash span"
    - "Initials algorithm: Split(' ', RemoveEmptyEntries), 3-branch (2+ words / 2+ chars / ?)"

key-files:
  created: []
  modified:
    - Views/Account/Profile.cshtml

key-decisions:
  - "string.IsNullOrEmpty() used for ALL fields — FullName defaults to string.Empty (never null but may be empty), so ?? alone would not catch it"
  - "Role display: userRole variable with special case — if 'No Role' treat as empty and show em dash"
  - "Avatar subtitle shows Position with fst-italic muted fallback text (not em dash) to distinguish header from field rows"
  - "No cards — flat col-md-8 container with border-bottom header divider and hr between sections per CONTEXT locked decisions"

patterns-established:
  - "Profile header: d-flex with avatar circle (90px) + name h3 + subtitle small, border-bottom divider"
  - "Section heading: text-uppercase fw-bold small text-muted"
  - "Label-value row: row mb-2 py-1 with col-sm-4 text-muted small fw-medium label and col-sm-8 value"

requirements-completed: [PROF-01, PROF-02, PROF-03]

# Metrics
duration: 2min
completed: 2026-02-27
---

# Phase 67 Plan 01: Dynamic Profile Page Summary

**Profile.cshtml rewritten from hardcoded Budi Santoso placeholders to @model ApplicationUser with 9 dynamic field bindings, null-safe em dash fallback, and avatar initials matching _Layout.cshtml algorithm**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-27T12:21:48Z
- **Completed:** 2026-02-27T12:23:30Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Replaced all hardcoded placeholder strings (Budi Santoso, 759921, budi.santoso, +62 812, etc.) with @Model bindings
- Two-section layout (Identitas: FullName/NIP/Email/PhoneNumber; Organisasi: Directorate/Section/Unit/Position/Role) with hr divider
- Avatar initials dynamically computed from Model.FullName using exact same 3-branch algorithm as _Layout.cshtml navbar
- All nullable fields (NIP, Email, PhoneNumber, Position, Section, Unit, Directorate) display em dash in text-muted span when null/empty
- dotnet build: 0 errors, 36 pre-existing warnings (unrelated to this change)

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite Profile.cshtml with @Model binding and two-section layout** - `71edcc9` (feat)
2. **Task 2: Verify no hardcoded placeholders remain and build compiles** - verification only, no code changes (passed inline with Task 1 commit)

## Files Created/Modified
- `Views/Account/Profile.cshtml` - Rewritten from hardcoded placeholder view to dynamic @model ApplicationUser binding with two-section flat layout

## Decisions Made
- `string.IsNullOrEmpty()` used for ALL fields — FullName defaults to `string.Empty` (never null but may be empty), so `??` alone would NOT catch empty FullName
- Role display uses `userRole` variable with special case: if equals "No Role", show em dash (consistent with controller behavior of falling back to "No Role" string)
- Profile header subtitle shows Position with italic muted fallback "Jabatan belum diatur" (not em dash) to visually distinguish header area from field rows below
- No cards per CONTEXT locked decisions — flat `col-md-8` container with `border-bottom` header divider and `<hr>` between sections

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Profile page now shows authenticated user's real data — ready for visual verification at /Account/Profile
- Edit Profile button links to /Account/Settings
- No blockers

---
*Phase: 67-dynamic-profile-page*
*Completed: 2026-02-27*
