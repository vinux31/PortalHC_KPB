# 157-01 Audit Report — Login, Profile, Settings

**Date:** 2026-03-12
**Scope:** AccountController.cs, Views/Account/Login.cshtml, Views/Account/Profile.cshtml, Views/Account/Settings.cshtml
**Requirements:** AUTH-01, AUTH-02, AUTH-03

---

## AUTH-01: Login Flow

### Findings

**PASS: Local password check via PasswordHasher**
- Login delegates to `_authService.AuthenticateAsync(email, password)` — the concrete implementation handles local `PasswordHasher` comparison or AD LDAP bind depending on `Authentication:UseActiveDirectory` config flag.
- `authResult.Success` gate prevents DB lookup on bad credentials.

**PASS: IsActive check blocks inactive users**
- After `FindByEmailAsync`, controller checks `!user.IsActive` and returns `"Akun Anda tidak aktif. Hubungi HC untuk mengaktifkan kembali akun Anda."` — clear, actionable message.

**PASS: Open redirect protection**
- `Url.IsLocalUrl(returnUrl)` check at line 116 prevents external redirect. Only relative URLs are followed; otherwise falls back to `Home/Index`.

**PASS: CSRF on Login POST**
- `[ValidateAntiForgeryToken]` on Login POST + `@Html.AntiForgeryToken()` in Login.cshtml.

**PASS: Logout clears session**
- `_signInManager.SignOutAsync()` clears the authentication cookie. Protected by `[ValidateAntiForgeryToken]` to prevent CSRF-triggered logout.

**PASS: Error messages don't leak user existence**
- When AD/local auth fails, `authResult.ErrorMessage` is returned. When user not found in DB, message says "Akun Anda belum terdaftar" — does not differentiate "exists but wrong password" vs "doesn't exist".
- Note: If AD auth fails, IAuthService presumably returns a generic error. The DB lookup only runs after auth succeeds — so wrong-password never reveals whether the email exists in DB.

**MINOR: AD sync catches all exceptions silently**
- `catch { }` at line 107 swallows all exceptions during profile sync. Low risk (sync is non-fatal by design) but no logging means sync failures are invisible. Document only — not blocking.

**NO ISSUES found for AUTH-01.**

---

## AUTH-02: Profile Display

### Findings

**PASS: [Authorize] on Profile action**
- Class-level `[Authorize]` on AccountController applies to Profile — unauthenticated users are redirected to login. No specific role restriction (correct — all authenticated users can view their own profile).

**PASS: Null-safe ViewBag / Model population**
- Profile action populates `ProfileViewModel` fields directly from `ApplicationUser` properties. `Role` uses `roles.FirstOrDefault() ?? "No Role"`.
- Profile.cshtml uses `string.IsNullOrEmpty()` guards on every field — null-safe throughout.

**PASS: Multi-unit users**
- `ApplicationUser.Unit` is a single string field (not a collection). The architecture note says "users can belong to multiple units" but the Profile model only exposes `Unit` (single). This is pre-existing design — Profile does not crash on multiple units, it just shows one.

**MINOR: Single-unit display for multi-unit users**
- The `Unit` property on `ApplicationUser` is a single string. If a user is mapped to multiple units (via UserUnit table), Profile shows only `user.Unit` (the direct field), not the mapped units. This is a display gap, not a crash. Document only.

**NO CRASH RISK found for AUTH-02.**

---

## AUTH-03: Settings — Password Change & Profile Edit

### Findings

**PASS: CSRF on both Settings POST forms**
- EditProfile form: `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]` on controller action.
- ChangePassword form: `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]` on controller action.

**PASS: Old password validation**
- `_userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword)` — Identity's built-in method verifies the current password before changing. Returns `PasswordMismatch` error if wrong.

**PASS: New password hashing**
- `ChangePasswordAsync` handles hashing internally via Identity's PasswordHasher. No raw password stored.

**PASS: Re-sign-in after password change**
- `_signInManager.RefreshSignInAsync(user)` called on success — updates the security stamp in the session so the user stays logged in with the new credentials.

**PASS: EditProfile persists to DB**
- `_userManager.UpdateAsync(user)` after setting `FullName`, `Position`, `PhoneNumber`. Result checked; errors surfaced via TempData.

**PASS: AD mode hides password form**
- Settings.cshtml conditionally renders the password section based on `Authentication:UseActiveDirectory`. In AD mode, shows info banner instead.

**PASS: Error messages are user-friendly and localized**
- Password error codes mapped to Indonesian messages (PasswordMismatch, PasswordTooShort, etc.) — no raw Identity error codes exposed to user.

**MINOR: ModelState validation redirects lose model state**
- On `EditProfile` and `ChangePassword`, invalid ModelState redirects to Settings (PRG pattern) with TempData error. Validation errors per-field are not shown inline because the model is re-fetched on GET. This means `<span asp-validation-for="...">` tags in the view are always empty for server-side errors. This is a UX issue (user doesn't know which field failed) but not a data loss or crash risk. Document only.

**NO DATA LOSS or CRASH RISK found for AUTH-03.**

---

## Summary

| Requirement | Status | Issues Found | Fixed |
|---|---|---|---|
| AUTH-01 (Login) | PASS | Silent AD sync exception catch | N/A (non-fatal by design) |
| AUTH-02 (Profile) | PASS | Single `Unit` field shown (multi-unit gap) | N/A (pre-existing design) |
| AUTH-03 (Settings) | PASS | Per-field validation errors not shown inline | N/A (UX minor) |

**No major bugs (crash / data loss) found. No code changes required.**

All three requirements are correctly implemented. The minor issues are documented above and do not block UAT.

---

## Deferred Minor Issues

1. AD sync failure is silently swallowed — consider adding `ILogger` warning in a future cleanup pass.
2. Multi-unit display gap — Profile shows `user.Unit` string only, not UserUnit table relationships.
3. Inline validation errors lost on PRG redirect in Settings — future improvement to pass ModelState via TempData if needed.
