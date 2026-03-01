# Phase 68: Functional Settings Page - Research

**Researched:** 2026-02-27
**Domain:** ASP.NET Core MVC — Identity ChangePasswordAsync, UpdateAsync, ViewModels, Razor form patterns
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Page structure
- Single scrollable page, semua section di satu halaman (tidak pakai tabs)
- Urutan: Edit Profile di atas, Change Password di bawahnya, non-functional items paling bawah
- Flat rows, col-md-8 centered — konsisten dengan Phase 67 Profile page, tidak pakai cards
- Heading: "Pengaturan Akun" sebagai h3/h4
- Breadcrumb/link kembali ke Profile page di atas heading

#### Form sections
- Setiap section (Edit Profile, Change Password) punya tombol Save sendiri-sendiri
- Edit Profile dan Change Password adalah 2 form terpisah, bukan satu form

#### Editable fields (Edit Profile section)
- FullName — editable text input
- Position — editable text input
- PhoneNumber — editable text input (ditambahkan, di luar roadmap original yang hanya FullName + Position)

#### Read-only fields (Edit Profile section)
- NIP, Email, Role, Section, Directorate, Unit — semua ditampilkan read-only
- Tampilan: input field disabled (background abu-abu) + hint kecil "Dikelola oleh admin" di bawahnya
- Semua org fields ditampilkan, tidak hanya 4 field dari roadmap

#### Password rules
- Ikuti konfigurasi ASP.NET Identity yang sudah ada: minimal 6 karakter, tanpa complexity requirements
- Fields: Password Lama, Password Baru, Konfirmasi Password Baru

#### Validation & feedback
- Validasi on-submit saja (standar ASP.NET MVC ModelState), tidak real-time
- Error muncul di bawah field yang bermasalah (asp-validation-for)
- Pesan sukses/error sebagai alert Bootstrap di atas section yang bersangkutan
- Bahasa Indonesia untuk semua pesan: "Profil berhasil diperbarui", "Password berhasil diubah", "Password lama salah"

#### Post-save behavior
- Setelah profile save: tetap di Settings, alert sukses muncul
- Setelah password save: tetap di Settings, form password di-reset (kosongkan), alert sukses muncul
- Konfirmasi dialog untuk password change saja ("Yakin ubah password?"), edit profile langsung save

#### Non-functional items
- 2FA, Email Notifications, Language — tetap ditampilkan tapi disabled
- Style: label muted text + badge kecil "Segera Hadir" di sebelahnya, toggle/dropdown disabled
- Posisi: section terpisah "Pengaturan Lainnya" di bawah Edit Profile dan Change Password

### Claude's Discretion
- Exact spacing, typography, dan responsive behavior
- Section heading style (uppercase muted, dividers, etc.)
- Alert animation/auto-dismiss timing
- Confirmation dialog implementation (native confirm vs Bootstrap modal)
- Read-only field grouping within Edit Profile section

### Deferred Ideas (OUT OF SCOPE)
- 2FA implementation — future phase
- Email notification system — future phase
- Language/i18n switching — future phase
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| PROF-04 | Settings page: Change Password functional via ChangePasswordAsync | UserManager.ChangePasswordAsync API — takes currentPassword + newPassword, returns IdentityResult. No token needed unlike admin reset. Pattern confirmed in ASP.NET Identity docs. |
| PROF-05 | Settings page: User bisa edit FullName dan Position; NIP/Email/Role/Section read-only | UserManager.UpdateAsync for user.FullName, user.Position, user.PhoneNumber. Three-field EditProfileViewModel with required FullName. Read-only fields rendered as disabled inputs with "Dikelola oleh admin" hint. |
| PROF-06 | Item non-functional (2FA, Notifications, Language) dihapus atau di-mark "Belum Tersedia" disabled | Keep all three items but render with disabled toggle/select + "Segera Hadir" badge in a bottom "Pengaturan Lainnya" section. No POST actions needed. |
</phase_requirements>

## Summary

Phase 68 is a focused ASP.NET Core MVC phase with no new infrastructure. All dependencies are already in the project: `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>` are already injected into `AccountController`. The current Settings page (`Views/Account/Settings.cshtml`) is fully dummy — single card with `onclick="alert('...')"` — and must be rewritten entirely.

The backend work requires two new ViewModels (`EditProfileViewModel`, `ChangePasswordViewModel`) in `Models/` and two new `[HttpPost]` actions in `AccountController`. The Settings GET action already exists but currently passes no model to the view — it must be updated to load user data and pass a composite ViewModel. The existing `UserManager<ApplicationUser>` methods `ChangePasswordAsync` and `UpdateAsync` handle both operations natively; no third-party libraries or EF migrations are needed.

The frontend work is a total rewrite of `Settings.cshtml`. The Phase 67 `Profile.cshtml` provides the exact visual reference: flat rows, col-md-8, text-uppercase muted section labels, no cards. Success/error feedback for this page must be inlined (via TempData keys specific to each form section) since the global `_Layout.cshtml` TempData alerts appear at the top of the page — but the CONTEXT.md asks for alerts "di atas section yang bersangkutan", so section-specific inline alerts are needed.

**Primary recommendation:** Create `SettingsViewModel` as a container holding both `EditProfileViewModel` and `ChangePasswordViewModel`; Settings GET loads user + role and passes the composite model; two POST actions each return `RedirectToAction("Settings")` on success with TempData keys `ProfileSuccess`/`PasswordSuccess`/`ProfileError`/`PasswordError` consumed inline in the view.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.AspNetCore.Identity | (bundled with ASP.NET Core 8) | ChangePasswordAsync, UpdateAsync | Built-in — already used by AccountController |
| System.ComponentModel.DataAnnotations | (BCL) | [Required], [StringLength], [Compare] | Standard MVC validation annotations |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| jquery.validate + jquery.validate.unobtrusive | (already in wwwroot/lib) | Client-side validation | Add `@section Scripts { @await Html.RenderPartialAsync("_ValidationScriptsPartial") }` to Settings.cshtml for instant field feedback |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ChangePasswordAsync | GeneratePasswordResetToken + ResetPasswordAsync | Admin-only reset (no current password check) — wrong for self-service |
| TempData for section alerts | ViewBag inline | TempData survives redirect; ViewBag does not — needed after POST-redirect-GET |

**Installation:** None needed. All stack already present.

---

## Architecture Patterns

### Recommended Project Structure
```
Models/
├── SettingsViewModel.cs    # NEW — composite model for Settings GET view
├── EditProfileViewModel.cs # NEW — FullName, Position, PhoneNumber
├── ChangePasswordViewModel.cs  # NEW — CurrentPassword, NewPassword, ConfirmNewPassword
Controllers/
└── AccountController.cs    # MODIFY — update Settings GET + add 2 POST actions
Views/Account/
└── Settings.cshtml         # REWRITE TOTAL — typed @model SettingsViewModel
```

### Pattern 1: Composite ViewModel for Settings GET
**What:** Settings GET action loads the user, creates a composite ViewModel containing both sub-forms pre-populated from user data.
**When to use:** When a single page hosts multiple independent forms with separate submit endpoints.

```csharp
// AccountController.cs — Settings GET (updated)
[HttpGet]
public async Task<IActionResult> Settings()
{
    if (User.Identity?.IsAuthenticated != true)
        return RedirectToAction("Login");

    var user = await _userManager.GetUserAsync(User);
    if (user == null) return RedirectToAction("Login");

    var roles = await _userManager.GetRolesAsync(user);
    var model = new SettingsViewModel
    {
        EditProfile = new EditProfileViewModel
        {
            FullName = user.FullName,
            Position = user.Position,
            PhoneNumber = user.PhoneNumber
        },
        ChangePassword = new ChangePasswordViewModel(),
        // Read-only display fields
        NIP = user.NIP,
        Email = user.Email,
        Role = roles.FirstOrDefault() ?? "—",
        Section = user.Section,
        Directorate = user.Directorate,
        Unit = user.Unit
    };

    return View(model);
}
```

### Pattern 2: EditProfile POST Action
**What:** POST action for profile edit — validate ModelState, update user fields, call UpdateAsync, redirect with TempData.
**When to use:** Self-service profile update by authenticated user.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditProfile(EditProfileViewModel model)
{
    if (!ModelState.IsValid)
    {
        // Rebuild composite model to re-render Settings page with errors
        var currentUser = await _userManager.GetUserAsync(User);
        // ... rebuild SettingsViewModel with model.EditProfile = model
        TempData["EditProfileError"] = "Periksa kembali isian form.";
        return RedirectToAction("Settings");
    }

    var user = await _userManager.GetUserAsync(User);
    if (user == null) return RedirectToAction("Login");

    user.FullName = model.FullName;
    user.Position = model.Position;
    user.PhoneNumber = model.PhoneNumber;

    var result = await _userManager.UpdateAsync(user);
    if (result.Succeeded)
    {
        TempData["ProfileSuccess"] = "Profil berhasil diperbarui.";
    }
    else
    {
        TempData["ProfileError"] = string.Join("; ", result.Errors.Select(e => e.Description));
    }

    return RedirectToAction("Settings");
}
```

### Pattern 3: ChangePassword POST Action
**What:** POST action for password change — uses ChangePasswordAsync (requires current password verification).
**When to use:** Self-service password change by authenticated user.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
{
    if (!ModelState.IsValid)
    {
        TempData["PasswordError"] = "Periksa kembali isian form password.";
        return RedirectToAction("Settings");
    }

    var user = await _userManager.GetUserAsync(User);
    if (user == null) return RedirectToAction("Login");

    var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
    if (result.Succeeded)
    {
        // Keep user signed in after password change
        await _signInManager.RefreshSignInAsync(user);
        TempData["PasswordSuccess"] = "Password berhasil diubah.";
    }
    else
    {
        // Most common error: PasswordMismatch — current password wrong
        TempData["PasswordError"] = result.Errors.Any(e => e.Code == "PasswordMismatch")
            ? "Password lama salah."
            : string.Join("; ", result.Errors.Select(e => e.Description));
    }

    return RedirectToAction("Settings");
}
```

### Pattern 4: SettingsViewModel with Nested Sub-models
```csharp
// Models/SettingsViewModel.cs
public class SettingsViewModel
{
    public EditProfileViewModel EditProfile { get; set; } = new();
    public ChangePasswordViewModel ChangePassword { get; set; } = new();

    // Read-only display fields (populated from user on GET)
    public string? NIP { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = "—";
    public string? Section { get; set; }
    public string? Directorate { get; set; }
    public string? Unit { get; set; }
}

// Models/EditProfileViewModel.cs
public class EditProfileViewModel
{
    [Required(ErrorMessage = "Nama lengkap harus diisi")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Position { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}

// Models/ChangePasswordViewModel.cs
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Password lama harus diisi")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password baru harus diisi")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konfirmasi password harus diisi")]
    [Compare("NewPassword", ErrorMessage = "Password baru dan konfirmasi tidak cocok")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
```

### Pattern 5: Settings.cshtml Structure (Phase 67-consistent flat rows)
```html
@model HcPortal.Models.SettingsViewModel

@* Section-specific inline alerts (above each form section) *@
@if (TempData["ProfileSuccess"] != null) { ... alert-success ... }
@if (TempData["ProfileError"] != null)   { ... alert-danger  ... }

@* --- SECTION 1: Edit Profile --- *@
<p class="text-uppercase fw-bold small text-muted mb-3">Edit Profil</p>
<form asp-action="EditProfile" method="post">
    @Html.AntiForgeryToken()
    <!-- Editable fields: FullName, Position, PhoneNumber (asp-for Model.EditProfile.*) -->
    <!-- Read-only fields: NIP, Email, Role, Section, Directorate, Unit (disabled inputs) -->
    <button type="submit" class="btn btn-primary">Simpan Profil</button>
</form>

<hr class="my-4">

@* --- SECTION 2: Change Password --- *@
@if (TempData["PasswordSuccess"] != null) { ... }
@if (TempData["PasswordError"] != null)   { ... }
<p class="text-uppercase fw-bold small text-muted mb-3">Ubah Password</p>
<form asp-action="ChangePassword" method="post" id="changePasswordForm">
    @Html.AntiForgeryToken()
    <!-- CurrentPassword, NewPassword, ConfirmNewPassword (asp-for Model.ChangePassword.*) -->
    <button type="submit" class="btn btn-primary" onclick="return confirm('Yakin ubah password?')">Ubah Password</button>
</form>

<hr class="my-4">

@* --- SECTION 3: Pengaturan Lainnya (non-functional, disabled) --- *@
<p class="text-uppercase fw-bold small text-muted mb-3">
    Pengaturan Lainnya
</p>
<!-- 2FA toggle disabled + "Segera Hadir" badge -->
<!-- Email Notifications toggle disabled + "Segera Hadir" badge -->
<!-- Language select disabled + "Segera Hadir" badge -->
```

### Pattern 6: Editable vs Read-only field rendering in Edit Profile form
```html
@* Editable field *@
<div class="row mb-3">
    <div class="col-sm-4">
        <label asp-for="EditProfile.FullName" class="col-form-label text-muted small fw-medium">Nama Lengkap</label>
    </div>
    <div class="col-sm-8">
        <input asp-for="EditProfile.FullName" class="form-control" />
        <span asp-validation-for="EditProfile.FullName" class="text-danger small"></span>
    </div>
</div>

@* Read-only field *@
<div class="row mb-3">
    <div class="col-sm-4">
        <label class="col-form-label text-muted small fw-medium">NIP</label>
    </div>
    <div class="col-sm-8">
        <input type="text" class="form-control" value="@(Model.NIP ?? "—")" disabled />
        <div class="form-text text-muted small">Dikelola oleh admin</div>
    </div>
</div>
```

### Anti-Patterns to Avoid
- **Two separate GET views for failed forms:** Instead of re-rendering the view directly on ModelState failure (which requires rebuilding the full composite model), use `RedirectToAction("Settings")` with TempData error key. This is simpler and consistent with the project pattern. The downside (field values are lost on error) is acceptable given the short form fields.
- **Using ResetPasswordAsync for self-service password change:** That method requires an admin-generated token and skips current password verification. Always use `ChangePasswordAsync` for user-initiated changes.
- **Storing sub-model errors in ModelState across redirect:** ModelState does not survive a redirect. Use TempData string messages instead, and place asp-validation-for spans inside the forms (they work during same-request re-renders if needed later).
- **Forgetting RefreshSignInAsync after ChangePassword:** After changing password, the security stamp changes. Without `RefreshSignInAsync`, the cookie becomes stale and the user may be logged out on next request.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Current password verification | Custom hash comparison | `UserManager.ChangePasswordAsync(user, currentPwd, newPwd)` | Handles security stamp refresh, lockout tracking, password hash update atomically |
| User field persistence | Direct DbContext.SaveChanges | `UserManager.UpdateAsync(user)` | Handles concurrency stamp, normalized fields, EF tracking |
| Password minimum length enforcement | Manual string length check | ASP.NET Identity options already configured in Program.cs: RequiredLength=6, no complexity | Already configured; ChangePasswordAsync enforces the same policy |

**Key insight:** The entire password-change and profile-update logic is one or two API calls on `UserManager`. The only custom code is ViewModel definitions and routing.

---

## Common Pitfalls

### Pitfall 1: ModelState validation fails silently on redirect
**What goes wrong:** POST action has `ModelState.IsValid == false`, redirects to Settings GET, but the user sees no field-level errors (just the generic TempData banner).
**Why it happens:** ModelState does not survive a redirect — `asp-validation-for` spans only populate during a direct `return View(model)` response.
**How to avoid:** For this project, redirect with TempData error message is the correct approach (consistent with pattern used throughout). Accept that per-field errors are not shown after redirect. The validation annotations still prevent bad data from being saved.
**Warning signs:** Trying to use `TempData["ModelState"]` serialization — this is overly complex and not used in this project.

### Pitfall 2: Forgetting RefreshSignInAsync after ChangePassword
**What goes wrong:** Password change succeeds but user gets 401/logged-out on next request.
**Why it happens:** `ChangePasswordAsync` updates the security stamp on the user. The existing cookie has the old security stamp, which Identity's cookie validation now rejects.
**How to avoid:** Always call `await _signInManager.RefreshSignInAsync(user)` immediately after a successful `ChangePasswordAsync`.

### Pitfall 3: asp-for binding with nested ViewModel
**What goes wrong:** `asp-for="EditProfile.FullName"` generates `name="EditProfile.FullName"` — but the POST action parameter `EditProfileViewModel model` expects `name="FullName"`.
**Why it happens:** MVC model binding strips the prefix if the POST action parameter matches the property name, but only when `[Bind]` prefix or the form is bound to a flat model.
**How to avoid:** POST actions use flat ViewModels (`EditProfileViewModel model`, `ChangePasswordViewModel model`) — NOT the composite `SettingsViewModel`. The form fields use `asp-for="EditProfile.FullName"` in the view (bound to composite model for display), but the generated `name` attribute will be `EditProfile.FullName`. The POST action must accept parameter named `EditProfile` (matching the prefix) OR use the flat model with the correct binding prefix. **Recommended solution:** Use prefix matching — declare the action parameter as `EditProfileViewModel editProfile` so MVC strips `EditProfile.` prefix automatically. OR use explicit `[Bind(Prefix = "EditProfile")]`.

### Pitfall 4: PhoneNumber requires SetPhoneNumberAsync not UpdateAsync
**What goes wrong:** Setting `user.PhoneNumber = value` and calling `UpdateAsync` does not trigger phone number confirmation reset or normalization.
**Why it happens:** `PhoneNumber` is an IdentityUser built-in field with special handling (confirmation, normalization). However, for this HRIS use case with no phone confirmation workflow, `user.PhoneNumber = value; await _userManager.UpdateAsync(user)` is sufficient and correct. `SetPhoneNumberAsync` would also reset `PhoneNumberConfirmed` to false, which is irrelevant here.
**How to avoid:** Direct assignment + UpdateAsync is fine for this project. Do NOT call `SetPhoneNumberAsync` as it would also require token management.

### Pitfall 5: Settings GET does not pass model to view
**What goes wrong:** The current `Settings()` GET action has no parameter or model — just `return View()`. After adding `@model SettingsViewModel` to Settings.cshtml, the view will throw a NullReferenceException.
**Why it happens:** The existing GET action is a stub; it needs to be updated to load user + create the composite model.
**How to avoid:** Update the GET action as the FIRST step — load user, build SettingsViewModel, return View(model).

---

## Code Examples

Verified patterns from project codebase:

### AuditLog (optional for profile/password changes — consistent with EditWorker pattern)
```csharp
// Source: CMPController.cs lines 3048-3061 (EditWorker)
// AuditLogService is already injected into AccountController? NO — it is NOT.
// AccountController only injects UserManager and SignInManager.
// For Phase 68, AuditLog is optional. If desired, inject AuditLogService into AccountController constructor.
// Pattern: wrap in try/catch so audit failure never blocks user operation.
try
{
    await _auditLog.LogAsync(user.Id, user.FullName, "EditProfile", "User updated profile.", null, "ApplicationUser");
}
catch { /* audit failure must not block */ }
```

### TempData consumption in view — section-specific alerts
```html
@* Source: Pattern from Views/CDP/Deliverable.cshtml (TempData["Success"]/["Error"]) *@
@* But for Settings, we need SECTION-SPECIFIC alerts — use unique keys per form *@

@if (TempData["ProfileSuccess"] != null)
{
    <div class="alert alert-success alert-dismissible fade show mb-3" role="alert">
        <i class="bi bi-check-circle-fill me-2"></i>@TempData["ProfileSuccess"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
@if (TempData["ProfileError"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show mb-3" role="alert">
        <i class="bi bi-x-circle-fill me-2"></i>@TempData["ProfileError"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
```

### Non-functional items with "Segera Hadir" badge (disabled)
```html
<div class="row mb-3 py-1">
    <div class="col-sm-8">
        <span class="text-muted">Two-Factor Authentication</span>
        <span class="badge bg-secondary ms-2" style="font-size: 0.7rem;">Segera Hadir</span>
    </div>
    <div class="col-sm-4 text-end">
        <div class="form-check form-switch d-inline-block">
            <input class="form-check-input" type="checkbox" disabled>
        </div>
    </div>
</div>
```

### Model binding prefix for nested ViewModel forms
```csharp
// POST action — use parameter name matching the composite ViewModel property name
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditProfile(EditProfileViewModel editProfile)
// MVC will strip "EditProfile." prefix from form fields like "EditProfile.FullName"
// because parameter name "editProfile" matches (case-insensitive) the property container name
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual password hash comparison | `UserManager.ChangePasswordAsync` | ASP.NET Core 1.0+ | Handles security stamp, lockout, hash update atomically |
| Direct DbContext update for user | `UserManager.UpdateAsync` | ASP.NET Core 1.0+ | Handles concurrency stamp, EF tracking |

**Deprecated/outdated:**
- `HttpContext.Authentication` (pre-Core): replaced by `SignInManager` — already correctly used in project.
- `PasswordHasher` direct usage: not needed; ChangePasswordAsync wraps it.

---

## Open Questions

1. **Should AuditLogService be injected into AccountController for Phase 68?**
   - What we know: AccountController currently only has UserManager + SignInManager. AuditLogService is registered as scoped in Program.cs.
   - What's unclear: The CONTEXT.md makes no mention of audit logging for profile/password changes.
   - Recommendation: Follow the existing EditWorker precedent (CMPController logs profile edits). Inject AuditLogService and log both EditProfile and ChangePassword. Wrap in try/catch. This is a minor addition that is consistent with the codebase.

2. **Model binding prefix behavior for nested ViewModel — needs verification during implementation**
   - What we know: In Razor, `asp-for="EditProfile.FullName"` generates `name="EditProfile.FullName"`. MVC model binding with parameter name `editProfile` should strip the `EditProfile.` prefix.
   - What's unclear: The exact casing rules for prefix stripping (is it case-insensitive?).
   - Recommendation: Use explicit `[Bind(Prefix = "EditProfile")]` attribute on the POST parameter to be unambiguous: `public async Task<IActionResult> EditProfile([Bind(Prefix="EditProfile")] EditProfileViewModel model)`.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (no automated test framework detected in project) |
| Config file | None |
| Quick run command | Manual browser test |
| Full suite command | Manual browser test |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PROF-04 | Change password with correct current password succeeds; wrong current password shows "Password lama salah" | manual-only | N/A — no test framework | N/A |
| PROF-05 | Edit FullName/Position/PhoneNumber saves and reflects on Profile page; NIP/Email/Role/Section are read-only (disabled inputs) | manual-only | N/A | N/A |
| PROF-06 | 2FA/Notifications/Language visible but disabled with "Segera Hadir" badge; no functional behavior | manual-only | N/A | N/A |

### Wave 0 Gaps
None — no automated test infrastructure to set up. All tests are manual UAT.

---

## Sources

### Primary (HIGH confidence)
- Project codebase: `Controllers/AccountController.cs` — existing Settings GET stub, UserManager/SignInManager injection confirmed
- Project codebase: `Models/ApplicationUser.cs` — FullName, Position fields confirmed; PhoneNumber is inherited from IdentityUser
- Project codebase: `Program.cs` — Identity password policy: RequiredLength=6, no digit/upper/lower/nonalpha requirements
- Project codebase: `Views/Account/Profile.cshtml` — visual pattern reference: flat rows, col-md-8, text-uppercase muted section labels, no cards
- Project codebase: `Views/Shared/_Layout.cshtml` lines 135-165 — global TempData["Warning"/"Error"/"Success"] alerts
- Project codebase: `Controllers/CMPController.cs` lines 2983-3064 — EditWorker pattern: UpdateAsync, role change, password reset, TempData, AuditLog
- Project codebase: `Models/ManageUserViewModel.cs` — ViewModel annotation patterns: [Required], [StringLength], [Compare], [DataType]

### Secondary (MEDIUM confidence)
- ASP.NET Core Identity standard: `ChangePasswordAsync(user, currentPassword, newPassword)` returns `IdentityResult`; requires current password — this is the correct method for self-service password change (not ResetPasswordAsync which is admin-only and token-based)
- ASP.NET Core Identity standard: `RefreshSignInAsync(user)` must be called after ChangePasswordAsync to prevent cookie invalidation due to security stamp update

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project, no new dependencies
- Architecture: HIGH — patterns directly observed in existing CMPController.EditWorker and Profile.cshtml
- Pitfalls: HIGH — binding prefix and RefreshSignInAsync are well-known ASP.NET Identity patterns; stub GET pitfall directly observed in existing code

**Research date:** 2026-02-27
**Valid until:** 2026-03-27 (stable framework; 30-day window)
