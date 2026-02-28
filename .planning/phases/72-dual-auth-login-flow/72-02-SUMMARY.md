---
phase: 72-dual-auth-login-flow
plan: 02
subsystem: Views (Account/Login, Admin/CreateWorker, Admin/EditWorker)
tags: [razor, configuration, ad-mode, ui-conditional, auth]
dependency_graph:
  requires: [72-01]
  provides: [AUTH-05]
  affects: [Views/Account/Login.cshtml, Views/Admin/CreateWorker.cshtml, Views/Admin/EditWorker.cshtml]
tech_stack:
  added: []
  patterns: [IConfiguration inject in Razor views, conditional readonly attribute, @if server-side conditional rendering]
key_files:
  created: []
  modified:
    - Views/Account/Login.cshtml
    - Views/Admin/CreateWorker.cshtml
    - Views/Admin/EditWorker.cshtml
decisions:
  - "@inject IConfiguration used with full namespace (Microsoft.Extensions.Configuration.IConfiguration) to avoid ambiguity in Razor views"
  - "readonly='@(isAdMode ? readonly : null)' pattern preserves form POST value binding — disabled inputs would be excluded from POST"
  - "Password/ConfirmPassword excluded from DOM entirely in AD mode (@if not CSS d-none) — prevents accidental submission of empty password override"
  - "EditWorker hr+hint block (Kosongkan kolom password) wrapped in same @if (!isAdMode) block — irrelevant UI removed together with password fields"
metrics:
  duration: 112s
  completed_date: "2026-02-28"
  tasks_completed: 2
  files_modified: 3
---

# Phase 72 Plan 02: AD Mode View Adaptation Summary

**One-liner:** Conditional Razor rendering for AD mode in Login hint text and CreateWorker/EditWorker password field hiding via @inject IConfiguration.

## What Was Built

Three Razor views adapted for dual-auth mode using `@inject IConfiguration Config` and `isAdMode` variable reading `Authentication:UseActiveDirectory` from appsettings.

**Login.cshtml:** Small grey hint "Login menggunakan akun Pertamina" inserted between submit button and Lupa Password link, rendered only when `isAdMode=true`. Zero visual change in local mode.

**CreateWorker.cshtml:** In AD mode — FullName and Email inputs become `readonly` with `bg-light` background and "Dikelola oleh AD" info text. Password and ConfirmPassword fields are replaced by an `alert-info` div explaining passwords are managed via the Pertamina portal.

**EditWorker.cshtml:** Same pattern as CreateWorker — readonly FullName/Email with AD hint, password section (including the existing hr/hint block) conditionally hidden in AD mode and replaced with the same alert-info div.

## Tasks Completed

| Task | Name | Commit | Files Modified |
|------|------|--------|----------------|
| 1 | Add AD mode hint to Login.cshtml | 1daca6a | Views/Account/Login.cshtml |
| 2 | Adapt CreateWorker and EditWorker views for AD mode | d3d5fcc | Views/Admin/CreateWorker.cshtml, Views/Admin/EditWorker.cshtml |

## Decisions Made

1. **Full namespace for IConfiguration inject** — `@inject Microsoft.Extensions.Configuration.IConfiguration Config` avoids potential Razor ambiguity issues compared to using a short alias.

2. **readonly not disabled for FullName/Email** — `disabled` inputs are excluded from form POST; `readonly` inputs are included. This ensures FullName/Email values are submitted even in AD mode (controller needs them for user creation).

3. **@if not CSS d-none for password fields** — Password fields completely excluded from DOM in AD mode. This is cleaner than hiding with CSS and avoids any risk of empty password values being submitted.

4. **EditWorker hr+hint block wrapped together** — The "Kosongkan kolom password jika tidak ingin mengubah" hint and hr divider are only relevant when password fields are shown. Both wrapped in the same `@if (!isAdMode)` block for coherent UI.

## Verification

- `dotnet build --configuration Release` — 0 errors, 58 warnings (all pre-existing CA1416 from LdapAuthService, out of scope)
- Login.cshtml has `@inject Microsoft.Extensions.Configuration.IConfiguration Config` and hint div in `@if (isAdMode)`
- CreateWorker.cshtml and EditWorker.cshtml have inject + isAdMode variable
- Password col-md-6 divs wrapped in `@if (!isAdMode)` with AD info alert as else branch
- FullName/Email inputs have `readonly="@(isAdMode ? "readonly" : null)"` in both admin views

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

Files exist:
- Views/Account/Login.cshtml — FOUND
- Views/Admin/CreateWorker.cshtml — FOUND
- Views/Admin/EditWorker.cshtml — FOUND

Commits exist:
- 1daca6a (feat(72-02): add AD mode hint to Login.cshtml) — FOUND
- d3d5fcc (feat(72-02): adapt CreateWorker and EditWorker views for AD mode) — FOUND
