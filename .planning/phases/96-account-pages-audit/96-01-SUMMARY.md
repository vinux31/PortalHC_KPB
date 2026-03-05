---
phase: "96"
plan: "96-01"
title: "Audit Profile Page & Cross-Page Concerns"
status: "complete"
date_started: "2026-03-05T04:57:27Z"
date_completed: "2026-03-05T05:02:00Z"
duration_minutes: 5
tasks_completed: 4
total_tasks: 4
files_modified: 0
---

# Phase 96 Plan 01: Audit Profile Page & Cross-Page Concerns Summary

## One-Liner
Systematic code review of Account Profile and Settings pages found only 1 minor bug in avatar initials logic for single-character names.

## Objective
Verify Profile page displays correct user data with proper null safety and avatar initials, audit cross-page concerns (CSRF, auth checks, navigation), and identify bugs for fixing.

## Success Criteria Achieved
- [x] Profile page renders without errors for users with complete and incomplete profile data
- [x] Avatar initials logic verified for multi-word names, single-word names, and empty names (bug found in single-char case)
- [x] All optional profile fields show "-" when null/empty (no null reference exceptions)
- [x] CSRF tokens verified present in both Settings forms with [ValidateAntiForgeryToken] on POST actions
- [x] Authentication checks verified to redirect unauthenticated users to Login page
- [x] Navigation links verified correct (Profile <-> Settings)
- [x] All identified bugs documented with severity and fix priority

## Deviations from Plan

### Auto-fixed Issues

None - this was a code review-only plan with no code changes.

## Bugs Found

### Bug #1: Avatar Initials Logic - Single-Character Name Not Handled
- **Severity:** Medium
- **File:** `Views/Account/Profile.cshtml` (lines 7-9)
- **Issue:** Users with single-character names see "?" instead of their initial
- **Current Code:**
  ```csharp
  var initials = nameParts.Length >= 2
      ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
      : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : "?");
  ```
- **Problem:** When `fullName.Length == 1`, condition `fullName.Length >= 2` is false, returns "?"
- **Fix Required:** Change condition to `fullName.Length >= 1`
- **Estimated Fix Time:** 2 minutes
- **Priority:** High (fix in plan 96-02)

## Verified Correct (No Issues)

### 1. Null Safety
- All optional fields properly use `@if (!string.IsNullOrEmpty(Model.XXX))` pattern
- Fields checked: FullName, NIP, Email, PhoneNumber, Directorate, Section, Unit, Position
- All show "—" when null/empty

### 2. CSRF Protection
- Settings.cshtml line 35: `@Html.AntiForgeryToken()` in EditProfile form
- Settings.cshtml line 147: `@Html.AntiForgeryToken()` in ChangePassword form
- AccountController.cs line 187: `[ValidateAntiForgeryToken]` on EditProfile POST
- AccountController.cs line 221: `[ValidateAntiForgeryToken]` on ChangePassword POST
- Login and Logout POSTs also protected

### 3. Authentication Checks
- Profile action: Checks `User.Identity?.IsAuthenticated` and `user != null`
- Settings action: Checks `User.Identity?.IsAuthenticated` and `user != null`
- EditProfile POST: Checks `user != null`
- ChangePassword POST: Checks `user != null`
- All redirect to Login if checks fail

### 4. Navigation Links
- Profile.cshtml lines 94-96: `<a asp-controller="Account" asp-action="Settings">` correct
- Settings.cshtml lines 9-11: `<a asp-controller="Account" asp-action="Profile">` correct

### 5. Role Display
- Properly handles "No Role" case showing "—"
- ViewBag.UserRole with null-coalescing operator

### 6. SettingsViewModel
- Proper null-safety with nullable string properties
- Default value "—" for Role
- Proper validation attributes on EditProfileViewModel and ChangePasswordViewModel

## Comparison to Previous Audits

**Phase 95 (Admin Portal):** Found 12 raw exception exposures + missing input validation
**Phase 96 (Account Pages):** Found 1 minor avatar logic bug

The Account pages are in much better shape than the Admin portal, showing the benefit of more recent development with better security practices already in place.

## Fix Priority for Plan 96-02

1. **Priority 1:** Fix avatar initials logic (2 min, 1 file: `Views/Account/Profile.cshtml`)
2. **Recommended Commit:** `fix(96-02): fix avatar initials for single-character names`

## Files Requiring Changes in Plan 96-02
- `Views/Account/Profile.cshtml` (1 line change)

## Requirements Completed
- ACCT-01: Profile page displays correct user data with proper null safety
- ACCT-02: Avatar initials display correctly (bug identified for fix in 96-02)
- ACCT-03: CSRF protection verified in Settings forms
- ACCT-04: Authentication checks verified in Profile/Settings actions

## Next Steps
Proceed to plan 96-02 to fix the identified avatar initials bug.
