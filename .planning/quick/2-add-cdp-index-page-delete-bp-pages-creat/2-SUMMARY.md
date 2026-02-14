---
phase: quick-2
plan: 01
subsystem: ui-navigation
tags: [cdp, bp, navbar, cleanup, ux]
dependency_graph:
  requires: []
  provides:
    - CDP hub page (card-based navigation)
    - Simplified navbar structure (direct links)
    - BP placeholder page
  affects:
    - Views/Shared/_Layout.cshtml (navbar)
    - CDP module navigation flow
    - BP module visibility
tech_stack:
  added: []
  patterns:
    - Card-based hub pattern (consistent with CMP/Index)
    - Indonesian localization for under-development pages
key_files:
  created:
    - Views/CDP/PlanIdp.cshtml
  modified:
    - Views/CDP/Index.cshtml
    - Views/Shared/_Layout.cshtml
    - Controllers/CDPController.cs
    - Views/BP/Index.cshtml
    - Controllers/BPController.cs
  deleted:
    - Views/BP/Simulation.cshtml
    - Views/BP/Historical.cshtml
    - Views/BP/EligibilityValidator.cshtml
    - Views/BP/TalentProfile.cshtml
    - Views/BP/PointSystem.cshtml
decisions:
  - CDP hub pattern matches CMP hub for UX consistency
  - BP features hidden behind placeholder until proper implementation
  - Navbar simplified from dropdowns to direct links for cleaner UX
  - Indonesian used for BP placeholder to match user base
metrics:
  duration: 165
  completed_date: 2026-02-14
---

# Quick Task 2: Add CDP Hub Page and Clean Up BP Module

**One-liner:** Created CDP card-based hub page, moved IDP viewer to PlanIdp action, simplified navbar to direct links, and replaced BP features with Indonesian under-development placeholder.

## Summary

Implemented a proper landing page for the CDP module using a card-based layout consistent with the CMP/Index pattern. The CDP hub now provides clear navigation to 4 core features: Plan IDP (Silabus), Laporan Coaching, Progress & Tracking, and Dashboard Monitoring.

The original IDP Proton PDF viewer was preserved by moving it to a new `CDP/PlanIdp` action and view, ensuring no functionality was lost while freeing up the Index route for the hub page.

For the BP module, all placeholder/dummy feature pages (TalentProfile, PointSystem, EligibilityValidator, Simulation, Historical) were deleted, and the module now shows a clean Indonesian-language "under development" page with a friendly rocket icon and information about upcoming features.

The navbar was simplified by replacing the CDP and BP dropdown menus with direct links, creating a cleaner and more consistent navigation experience alongside the CMP module.

## Tasks Completed

### Task 1: Create CDP/Index hub page and update navbar for CDP and BP

**Status:** Complete
**Commit:** `a7d6e20`

**Changes:**
- Created `Views/CDP/Index.cshtml` as a 4-card hub page with links to Plan IDP, Coaching, Progress, and Dashboard
- Created `Views/CDP/PlanIdp.cshtml` by copying the original Index.cshtml content (IDP Proton PDF viewer)
- Added `PlanIdp` action method to `CDPController` with the original Index logic
- Simplified `Index` action in `CDPController` to just `return View();`
- Updated `Views/Shared/_Layout.cshtml` navbar to replace CDP and BP dropdown menus with simple nav-item direct links

**Files modified:**
- `Views/CDP/Index.cshtml` (replaced with hub page)
- `Views/CDP/PlanIdp.cshtml` (created - preserved original PDF viewer)
- `Controllers/CDPController.cs` (added PlanIdp action, simplified Index)
- `Views/Shared/_Layout.cshtml` (removed dropdowns, added direct links)

**Verification:**
- Build compiles successfully
- CDP/Index shows 4-card hub page
- CDP/PlanIdp preserves original IDP PDF viewer functionality
- Navbar shows CMP, CDP, BP as simple links (no dropdowns)

### Task 2: Delete BP feature pages and simplify BPController to under-development placeholder

**Status:** Complete
**Commit:** `e4fb05d`

**Changes:**
- Deleted 5 BP feature view files:
  - `Views/BP/Simulation.cshtml`
  - `Views/BP/Historical.cshtml`
  - `Views/BP/EligibilityValidator.cshtml`
  - `Views/BP/TalentProfile.cshtml`
  - `Views/BP/PointSystem.cshtml`
- Replaced `Views/BP/Index.cshtml` with Indonesian "under development" placeholder page featuring:
  - Rocket icon (bi-rocket-takeoff)
  - Clean card layout
  - Message: "Modul ini sedang dalam tahap pengembangan"
  - Info alert listing upcoming features
  - "Kembali ke Beranda" button
- Simplified `Controllers/BPController.cs` to minimal implementation:
  - Removed all action methods except `Index`
  - Removed all ViewModel classes (TalentProfileViewModel, PerformanceRecord, CareerHistory, PointSystemViewModel, PointActivity, EligibilityViewModel, EligibilityCriteria)
  - Removed unused dependencies (UserManager, ApplicationDbContext)
  - Only imports: Microsoft.AspNetCore.Mvc and Microsoft.AspNetCore.Authorization

**Files modified:**
- `Views/BP/Index.cshtml` (replaced with placeholder)
- `Controllers/BPController.cs` (simplified to 13 lines)

**Files deleted:**
- 5 BP feature views (listed above)

**Verification:**
- Build compiles with zero errors
- Only `Index.cshtml` remains in `Views/BP/` directory
- BP/Index displays styled Indonesian placeholder page
- BP/TalentProfile, BP/PointSystem, BP/EligibilityValidator return 404 (actions removed)

## Deviations from Plan

None - plan executed exactly as written.

## Technical Notes

**CDP Hub Pattern:**
- Follows exact same structure as CMP/Index.cshtml
- 4 cards in responsive grid (col-12 col-md-6 col-lg-3)
- Each card has icon-box, title, subtitle, description, and action button
- Hover effects for card lift and shadow enhancement
- Consistent with Bootstrap 5 utilities and custom icon-box styling

**Controller Refactoring:**
- CDPController.Index now serves as hub landing (no parameters needed)
- CDPController.PlanIdp preserves original PDF viewer logic with all filters
- BPController reduced from 228 lines to 13 lines (95% reduction)

**Localization:**
- BP placeholder uses Indonesian: "Modul ini sedang dalam tahap pengembangan"
- Features listed in Indonesian: "Fitur Talent Profile, Point System, Career Simulation, dan Eligibility Validator akan segera tersedia"
- Button text: "Kembali ke Beranda"

**Navigation UX:**
- Before: CDP had 4 dropdown items, BP had 3 dropdown items
- After: CDP and BP are single-click direct links (faster navigation)
- All CDP features still accessible via hub cards
- BP shows single clear message about development status

## Self-Check: PASSED

**Created files verified:**
```
FOUND: Views/CDP/PlanIdp.cshtml
```

**Modified files verified:**
```
FOUND: Views/CDP/Index.cshtml (hub page with 4 cards)
FOUND: Views/Shared/_Layout.cshtml (simplified navbar)
FOUND: Controllers/CDPController.cs (Index + PlanIdp actions)
FOUND: Views/BP/Index.cshtml (placeholder page)
FOUND: Controllers/BPController.cs (minimal controller)
```

**Deleted files verified:**
```
NOT FOUND: Views/BP/Simulation.cshtml (correctly deleted)
NOT FOUND: Views/BP/Historical.cshtml (correctly deleted)
NOT FOUND: Views/BP/EligibilityValidator.cshtml (correctly deleted)
NOT FOUND: Views/BP/TalentProfile.cshtml (correctly deleted)
NOT FOUND: Views/BP/PointSystem.cshtml (correctly deleted)
```

**Commits verified:**
```
FOUND: a7d6e20 (CDP hub and navbar)
FOUND: e4fb05d (BP cleanup)
```

**Build status:**
```
Build succeeded (warnings only, no errors)
```

All deliverables verified successfully.
