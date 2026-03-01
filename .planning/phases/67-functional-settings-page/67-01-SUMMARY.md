---
phase: 68-functional-settings-page
plan: "01"
subsystem: account-settings
tags: [viewmodel, controller, identity, settings, password-change]
dependency_graph:
  requires: []
  provides: [SettingsViewModel, EditProfileViewModel, ChangePasswordViewModel, Settings-GET, EditProfile-POST, ChangePassword-POST]
  affects: [Controllers/AccountController.cs, Views/Account/Settings.cshtml]
tech_stack:
  added: []
  patterns: [composite-viewmodel, bind-prefix-model-binding, tempdata-feedback, refresh-sign-in]
key_files:
  created:
    - Models/SettingsViewModel.cs
  modified:
    - Controllers/AccountController.cs
decisions:
  - "[Phase 68-01]: SettingsViewModel is a composite GET model — EditProfile and ChangePasswordViewModel nested with new() initializers so view never gets null sub-models"
  - "[Phase 68-01]: [Bind(Prefix=EditProfile)] and [Bind(Prefix=ChangePassword)] on POST actions — handles nested model binding from composite form without full SettingsViewModel roundtrip"
  - "[Phase 68-01]: TempData keys are section-specific (ProfileSuccess, ProfileError, PasswordSuccess, PasswordError) — avoids cross-section alert bleed"
  - "[Phase 68-01]: RefreshSignInAsync called after ChangePasswordAsync success — CRITICAL prevents cookie invalidation after security stamp update"
  - "[Phase 68-01]: PasswordMismatch error code maps to user-friendly 'Password lama salah.' — other errors fall through to generic joined Description list"
metrics:
  duration_minutes: 3
  completed_date: "2026-02-27"
  tasks_completed: 2
  files_created: 1
  files_modified: 1
---

# Phase 68 Plan 01: Functional Settings Page — Backend ViewModels and Controller Actions Summary

**One-liner:** Settings backend with composite SettingsViewModel (3 classes), async Settings GET pre-populating EditProfile fields, EditProfile POST via UpdateAsync, and ChangePassword POST via ChangePasswordAsync+RefreshSignInAsync.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Create SettingsViewModel with nested EditProfileViewModel and ChangePasswordViewModel | f34db5e | Models/SettingsViewModel.cs (created) |
| 2 | Update AccountController: async Settings GET with model, add EditProfile POST and ChangePassword POST | aa229bc | Controllers/AccountController.cs (modified) |

## What Was Built

### Models/SettingsViewModel.cs (new)

Three classes in one file following the CoachingViewModels.cs/ProtonViewModels.cs project convention:

- **SettingsViewModel** — composite GET model with `EditProfile` and `ChangePassword` sub-models (both initialized to `new()`) plus read-only display fields: NIP, Email, Role (default "—"), Section, Directorate, Unit
- **EditProfileViewModel** — editable FullName (`[Required][StringLength(100)]`), Position (`[StringLength(100)]`), PhoneNumber (`[StringLength(20)]`)
- **ChangePasswordViewModel** — CurrentPassword, NewPassword (`[Required][StringLength(100, MinimumLength=6)]`), ConfirmNewPassword (`[Required][Compare("NewPassword")]`) all with `[DataType(DataType.Password)]`

### Controllers/AccountController.cs (updated)

**Settings GET** (was sync stub, now async with model):
- Loads user via `GetUserAsync(User)`, gets roles via `GetRolesAsync`
- Creates populated `SettingsViewModel` with pre-filled EditProfile (FullName, Position, PhoneNumber) and empty ChangePassword
- Returns `View(model)`

**EditProfile POST** (new action):
- `[HttpPost][ValidateAntiForgeryToken]` with `[Bind(Prefix = "EditProfile")]`
- Invalid ModelState: `TempData["ProfileError"] = "Periksa kembali isian profil."` + redirect
- Updates user.FullName, user.Position, user.PhoneNumber via `_userManager.UpdateAsync`
- Success: `TempData["ProfileSuccess"] = "Profil berhasil diperbarui."`
- Failure: joined error descriptions in `TempData["ProfileError"]`

**ChangePassword POST** (new action):
- `[HttpPost][ValidateAntiForgeryToken]` with `[Bind(Prefix = "ChangePassword")]`
- Invalid ModelState: `TempData["PasswordError"] = "Periksa kembali isian password."` + redirect
- Calls `_userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword)`
- Success: calls `_signInManager.RefreshSignInAsync(user)` then `TempData["PasswordSuccess"] = "Password berhasil diubah."`
- PasswordMismatch error: `TempData["PasswordError"] = "Password lama salah."`
- Other errors: joined descriptions

## Decisions Made

1. **Composite SettingsViewModel** — single GET model with nested sub-models initialized to `new()` ensures the view always has non-null sub-models for form binding, consistent with the plan spec.

2. **`[Bind(Prefix)]` on POST actions** — per RESEARCH Pitfall 3: when posting from a composite view's sub-form, the prefix disambiguates which sub-model is being bound.

3. **Section-specific TempData keys** — ProfileSuccess/ProfileError and PasswordSuccess/PasswordError are section-specific per CONTEXT decision, allowing the view to display alerts in the correct section.

4. **RefreshSignInAsync after ChangePasswordAsync** — CRITICAL call that refreshes the security stamp cookie, preventing automatic session logout after password change.

5. **PasswordMismatch code check** — `result.Errors.Any(e => e.Code == "PasswordMismatch")` maps to user-friendly Bahasa Indonesia message; other codes fall through to generic error description.

## Verification

1. `dotnet build` — 0 C# compilation errors (MSB3021/MSB3027 are file-lock warnings from running process per Phase 48-03 precedent)
2. Settings GET creates SettingsViewModel with pre-populated EditProfile fields from user
3. EditProfile POST uses `[Bind(Prefix="EditProfile")]` and calls `UpdateAsync`
4. ChangePassword POST uses `[Bind(Prefix="ChangePassword")]` and calls `ChangePasswordAsync` + `RefreshSignInAsync`
5. All TempData keys are section-specific
6. All user-facing messages in Bahasa Indonesia

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

| Item | Status |
|------|--------|
| Models/SettingsViewModel.cs | FOUND |
| Controllers/AccountController.cs | FOUND |
| Commit f34db5e | FOUND |
| Commit aa229bc | FOUND |
