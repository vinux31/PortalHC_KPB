# Phase 96: Account Pages Audit - Research

**Researched:** 2026-03-05
**Domain:** ASP.NET Core 8.0 MVC - Account Profile & Settings Bug Audit
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Audit Organization:**
- Per functional area — Profile bugs → satu commit, Settings Edit Profile bugs → satu commit, Change Password bugs → satu commit
- Expected: 3-4 commits tergantung findings
- Matches Phase 94's by-flow approach and keeps changes organized by feature area

**Testing Approach:**
- Code review untuk sebagian besar — Profile, Edit Profile, password change, CSRF, null safety
- Browser test hanya untuk — Authentication check (coba akses langsung tanpa login)
- Focus on verifying the specific bug that was fixed

**Bug Priority:**
- Claude's discretion — prioritize based on severity and user impact
- Critical: crashes, null references, raw exceptions shown to users
- High: broken flows, incorrect data displayed, navigation failures
- Medium: UX issues (unclear text, missing links, confusing UI)
- Low: cosmetic issues, typos, minor inconsistencies

**Test Data Approach:**
- Pakai existing users — users dari prior phases (Phase 83 workers, Phase 87 seed data)
- Pragmatic approach: gunakan existing DB users, tambah hanya jika edge case spesific diperlukan

**Area 1: Profile Page**
- Avatar Initials: Biarkan logic existing (2 kata pertama untuk nama multi-kata, 2 karakter pertama untuk satu kata, '?' untuk kosong)
- Empty Field Placeholder: Biarkan tampil '-' untuk field kosong
- Role Display: Biarkan "No Role" untuk user tanpa role
- Testing: Code review saja — tidak perlu browser test untuk Profile

**Area 2: Edit Profile (Settings)**
- Validation Rules: Tambah validasi — PhoneNumber (numeric only), Email (format email, walau field read-only)
- Success/Error Messages: Improve dengan auto-dismiss — tambah JavaScript untuk auto-dismiss alert setelah 5 detik
- Testing: Code review saja — cek validation dan auto-dismiss logic

**Area 3: Change Password**
- AD Mode Handling: Sembunyikan form ganti password jika mode AD aktif (baca config `Authentication:UseActiveDirectory`, jika true sembunyikan section "Ubah Password", tambah info/message bahwa password dikelola oleh AD)
- Local Mode: Biarkan minimum 6 karakter untuk local auth — tidak perlu complexity requirement
- Error Messages: Perbaiki bahasa Indonesia — review semua pesan error password dan perbaiki agar lebih natural/jelas
- Testing: Code review saja untuk logic hide form dan password change; untuk AD: tidak perlu test ganti password (form di-hide)

**Area 4: Cross-page & Auth**
- Navigation: Code review saja — cek link "Edit Profile" di Profile → Settings, cek link "Kembali ke Profil" di Settings → Profile
- CSRF Protection: Verifikasi CSRF — pastikan @Html.AntiForgeryToken() ada di kedua form, pastikan [ValidateAntiForgeryToken] attribute di POST actions, verify implementasi correct
- Authentication Check: Perlu browser test — test: coba akses /Account/Profile dan /Account/Settings tanpa login, verify redirect ke Login page
- Null Safety: Code review saja — cek null handling untuk Model dan ViewBag di views, cek user null check di controller actions

### Claude's Discretion
- Exact order of bug fixes within each area
- Grouping fixes into commits
- Detail improvement untuk bahasa Indonesia error messages

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ACCT-01 | Profile page displays correct user data (Nama, NIP, Email, Position, Unit) | Profile.cshtml (line 1-101) displays all ApplicationUser fields with null-safety checks using `@if (!string.IsNullOrEmpty(Model.XXX))` pattern. Avatar initials logic (lines 5-9) handles multi-word, single-word, and empty names. |
| ACCT-02 | Settings page change password works correctly | ChangePassword POST action (AccountController.cs line 220-255) uses UserManager.ChangePasswordAsync with RefreshSignInAsync. Error handling includes PasswordMismatch specific check (line 244-246). |
| ACCT-03 | Profile edit (FullName, Position) saves correctly | EditProfile POST action (AccountController.cs line 186-217) updates FullName, Position, PhoneNumber via UserManager.UpdateAsync. Uses TempData for success/error messages. |
| ACCT-04 | Avatar initials display correctly from FullName | Profile.cshtml lines 5-9: splits fullName by spaces, takes first 2 words (first char each), or first 2 chars for single-word, or "?" for empty. Handles null/empty cases. |
</phase_requirements>

## Summary

Phase 96 audits the Account (Profile & Settings) pages for bugs. These are self-service user account management pages where workers can view their profile (read-only with avatar initials) and edit limited fields (FullName, Position, PhoneNumber) or change their password (local auth mode only). The audit follows the proven Phase 93-95 pattern: systematic code review → identify bugs → fix → smoke test verification.

**Key insight:** AccountController.cs is relatively small (264 lines) compared to other controllers, with straightforward authentication checks, no role-based authorization (all authenticated users can access their own account), and simple CRUD operations using UserManager. Profile page displays user identity and organization info with proper null-safety patterns. Settings page has two separate forms (EditProfile, ChangePassword) with proper CSRF protection. The main areas to audit are: Profile page null safety and avatar initials, EditProfile validation (add phone numeric validation), ChangePassword AD mode hiding, and cross-page concerns (CSRF, auth checks, navigation).

**Primary recommendation:** Use the existing audit pattern from Phases 93-95—systematic code review using checklists (null safety, validation, CSRF, auth, AD mode handling), identify bugs, fix issues in organized commits (by functional area: Profile, EditProfile, ChangePassword), and verify with smoke testing using existing seed data from Phases 83 and 87. No new test infrastructure or data seeding required—the existing users from SeedTestData.cs provide diverse test cases (different name formats, empty fields, various roles). Key improvements needed: add phone numeric validation, hide password form in AD mode, add auto-dismiss for success/error alerts.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 | Web framework for Profile/Settings pages | Built-in Identity integration, TempData, model validation |
| ASP.NET Core Identity | 8.0 | User management (UserManager, SignInManager) | Standard authentication stack for password changes, profile updates |
| DataAnnotations | System.ComponentModel.DataAnnotations | Server-side validation attributes | Built-in validation framework ([Required], [StringLength], [Compare]) |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap | 5.3 | UI framework for forms, alerts | Already used in Profile.cshtml and Settings.cshtml |
| jQuery | 3.6 | JavaScript utilities | Already loaded in layout for auto-dismiss alerts |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| TempData alerts | Session-based alerts | TempData is simpler for single-redirect messages, automatically cleared |
| Manual validation | FluentValidation | DataAnnotations is sufficient for simple validation rules, no extra dependencies |

**Installation:**
No new packages needed—stack already installed in project:
```bash
# Existing packages (no action required)
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Mvc.DataAnnotations
```

## Architecture Patterns

### Recommended Project Structure
```
Controllers/
├── AccountController.cs (264 lines) — Profile, Settings, EditProfile, ChangePassword, Login, Logout
Models/
├── ApplicationUser.cs — User entity with custom properties
├── SettingsViewModel.cs — Composite model for Settings page (EditProfile + ChangePassword)
Views/Account/
├── Profile.cshtml (101 lines) — Read-only display with avatar initials
├── Settings.cshtml (183 lines) — Two separate forms (EditProfile + ChangePassword)
├── Login.cshtml — Login page
└── AccessDenied.cshtml — Authorization failure page
```

### Pattern 1: Authentication Check Pattern
**What:** Manual authentication check at action entry
**When to use:** For pages requiring authenticated users but no role restrictions
**Example:**
```csharp
// Source: AccountController.cs lines 130-141
public async Task<IActionResult> Profile()
{
    if (User.Identity?.IsAuthenticated != true)
    {
        return RedirectToAction("Login");
    }

    var user = await _userManager.GetUserAsync(User);
    if (user == null)
    {
        return RedirectToAction("Login");
    }

    // ... rest of action
}
```

**Note:** Unlike other controllers with class-level `[Authorize]`, AccountController uses manual checks for Profile/Settings to allow anonymous access to Login/Register pages.

### Pattern 2: TempData Success/Error Messages
**What:** Store success/error messages in TempData for display after redirect
**When to use:** POST actions that redirect to GET (PRG pattern)
**Example:**
```csharp
// Source: AccountController.cs lines 206-214
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
```

**Display in view (Settings.cshtml lines 17-29):**
```csharp
@if (TempData["ProfileSuccess"] != null)
{
    <div class="alert alert-success alert-dismissible fade show mb-3" role="alert">
        <i class="bi bi-check-circle-fill me-2"></i>@TempData["ProfileSuccess"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
```

### Pattern 3: Null-Safe Display Pattern
**What:** Conditional rendering for optional model fields
**When to use:** Displaying user profile fields that may be null/empty
**Example:**
```csharp
// Source: Profile.cshtml lines 32-36
<div class="row mb-2 py-1">
    <div class="col-sm-4 text-muted small fw-medium">Nama Lengkap</div>
    <div class="col-sm-8">
        @if (!string.IsNullOrEmpty(Model.FullName)) { @Model.FullName } else { <span class="text-muted">—</span> }
    </div>
</div>
```

### Pattern 4: Avatar Initials Logic
**What:** Generate initials from full name for avatar display
**When to use:** Display user initials when no profile photo available
**Example:**
```csharp
// Source: Profile.cshtml lines 5-9
@{
    var fullName = string.IsNullOrEmpty(Model.FullName) ? "" : Model.FullName;
    var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var initials = nameParts.Length >= 2
        ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
        : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : "?");
}
```

**Display (line 18):**
```html
<div class="bg-primary text-white rounded-circle d-flex justify-content-center align-items-center fw-bold flex-shrink-0 me-4"
     style="width: 90px; height: 90px; font-size: 2rem;">@initials</div>
```

### Pattern 5: Separate Forms with Prefix Binding
**What:** Multiple forms in one view using [Bind(Prefix = "...")]
**When to use:** Settings page with EditProfile and ChangePassword forms
**Example:**
```csharp
// Source: AccountController.cs line 188
public async Task<IActionResult> EditProfile([Bind(Prefix = "EditProfile")] EditProfileViewModel model)

// Source: AccountController.cs line 222
public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model)
```

**View form (Settings.cshtml lines 38-44):**
```csharp
<div class="row mb-3">
    <div class="col-sm-4">
        <label asp-for="EditProfile.FullName" class="col-form-label text-muted small fw-medium">Nama Lengkap</label>
    </div>
    <div class="col-sm-8">
        <input asp-for="EditProfile.FullName" class="form-control" />
        <span asp-validation-for="EditProfile.FullName" class="text-danger small"></span>
    </div>
</div>
```

### Anti-Patterns to Avoid
- **Missing null checks:** Always use `@if (!string.IsNullOrEmpty(Model.XXX))` before displaying optional fields
- **Raw exception exposure:** Never show `result.Errors.Select(e => e.Description).Join()` directly to users—use generic Indonesian messages
- **CSRF token missing:** Always include `@Html.AntiForgeryToken()` in forms and `[ValidateAntiForgeryToken]` on POST actions
- **Hard-coded AD mode detection:** Read from `_config.GetValue<bool>("Authentication:UseActiveDirectory", false)` instead of hard-coding

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Server-side validation | Custom validation logic | DataAnnotations attributes ([Required], [StringLength], [RegularExpression]) | Built-in MVC validation, automatically generates client-side validation |
| Password hashing | Manual password hashing | UserManager.ChangePasswordAsync | Identity handles hashing, salt, history, complexity rules |
| Authentication checks | Custom session/auth logic | User.Identity?.IsAuthenticated + UserManager.GetUserAsync | Standard Identity pattern, handles cookies, claims, security |
| CSRF protection | Manual token validation | [ValidateAntiForgeryToken] attribute | Built-in anti-forgery token generation and validation |
| Avatar initials | Complex name parsing algorithms | Simple string.Split + substring pattern | Existing logic handles multi-word, single-word, empty names correctly |

**Key insight:** ASP.NET Core Identity provides all necessary account management primitives. Don't reinvent user authentication, password management, or profile updates—use UserManager and SignInManager APIs. The existing code follows best practices with null-safe Razor views, TempData for PRG pattern, and proper CSRF protection.

## Common Pitfalls

### Pitfall 1: Avatar Initials Edge Cases
**What goes wrong:** Single-word names, empty/null names, names with special characters may throw exceptions or display incorrect initials
**Why it happens:** String indexing without null/empty checks, assuming multi-word names
**How to avoid:** Existing code (Profile.cshtml lines 5-9) already handles edge cases correctly—no changes needed per user decision
**Warning signs:** `IndexOutOfRangeException`, empty avatar circles, "?" displayed for valid names

**Verification:** Code review confirms proper handling:
- Empty/null name → returns "?"
- Single-word name → returns first 2 characters (if length >= 2)
- Multi-word name → returns first character of first 2 words

### Pitfall 2: Missing AD Mode Password Form Hiding
**What goes wrong:** Change Password form shown in AD mode, causing user confusion
**Why it happens:** Settings.cshtml doesn't check `Authentication:UseActiveDirectory` config
**How to avoid:** Read config in view using `@inject IConfiguration` or pass from controller, conditionally render password form section
**Warning signs:** Users complain they can't change password, error "Password mismatch" in AD mode

**Required fix (per CONTEXT.md):**
```csharp
@inject IConfiguration _config
@{
    var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
}
@if (!useAD) {
    <!-- Password form section -->
} else {
    <div class="alert alert-info">
        <i class="bi bi-info-circle me-2"></i>
        Password dikelola oleh Active Directory. Hubungi admin IT untuk mengubah password.
    </div>
}
```

### Pitfall 3: Missing Phone Numeric Validation
**What goes wrong:** Users can enter letters, special characters in phone number field
**Why it happens:** EditProfileViewModel.PhoneNumber only has `[StringLength(20)]` without format validation
**How to avoid:** Add `[RegularExpression(@"^[0-9]+$", ErrorMessage = "Nomor telepon hanya boleh angka")]` attribute
**Warning signs:** Database contains phone numbers like "abc123", "0812-3456-7890" (dashes)

**Required fix (per CONTEXT.md):**
```csharp
// Models/SettingsViewModel.cs line 35-36
[StringLength(20)]
[RegularExpression(@"^[0-9]+$", ErrorMessage = "Nomor telepon hanya boleh angka")]
public string? PhoneNumber { get; set; }
```

### Pitfall 4: Alert Auto-Dismiss Missing
**What goes wrong:** Success/error alerts stay visible indefinitely, cluttering UI
**Why it happens:** Bootstrap alerts require manual dismissal or JavaScript auto-dismiss
**How to avoid:** Add JavaScript to auto-dismiss alerts after 5 seconds using `setTimeout`
**Warning signs:** Multiple alerts stacked on page, user confusion about current state

**Required fix (per CONTEXT.md):**
```javascript
// Add to Settings.cshtml at bottom
@section Scripts {
    <script>
        setTimeout(function() {
            $('.alert').fadeOut('slow');
        }, 5000);
    </script>
}
```

### Pitfall 5: Raw Identity Error Messages
**What goes wrong:** Generic English error messages shown to users ("PasswordMismatch", "PasswordTooShort")
**Why it happens:** `result.Errors.Select(e => e.Description).Join("; ")` exposes Identity error codes
**How to avoid:** Map Identity errors to natural Indonesian messages (already done for PasswordMismatch)
**Warning signs:** English error messages, technical error codes displayed to end users

**Current code (AccountController.cs line 244-251):**
```csharp
if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
{
    TempData["PasswordError"] = "Password lama salah.";
}
else
{
    TempData["PasswordError"] = string.Join("; ", result.Errors.Select(e => e.Description));
}
```

**Improvement needed:** Review all Identity error codes and add Indonesian mappings for common cases (PasswordTooShort, PasswordRequiresUniqueChars, etc.)

### Pitfall 6: Missing CSRF Protection
**What goes wrong:** Forms vulnerable to Cross-Site Request Forgery attacks
**Why it happens:** Missing `@Html.AntiForgeryToken()` in views or `[ValidateAntiForgeryToken]` on POST actions
**How to avoid:** Verify CSRF token present in both forms (EditProfile, ChangePassword) and attributes on POST actions
**Warning signs:** Forms submit without tokens, POST actions missing ValidateAntiForgeryToken attribute

**Verification:** Code review confirms proper CSRF protection:
- Settings.cshtml line 35: `@Html.AntiForgeryToken()` in EditProfile form ✓
- Settings.cshtml line 147: `@Html.AntiForgeryToken()` in ChangePassword form ✓
- AccountController.cs line 187: `[ValidateAntiForgeryToken]` on EditProfile POST ✓
- AccountController.cs line 221: `[ValidateAntiForgeryToken]` on ChangePassword POST ✓

### Pitfall 7: Null Reference Exceptions in Profile Display
**What goes wrong:** Profile page crashes with NullReferenceException for users with missing profile data
**Why it happens:** Accessing Model.XXX properties without null checks
**How to avoid:** Use `@if (!string.IsNullOrEmpty(Model.XXX))` pattern throughout Profile.cshtml
**Warning signs:** Profile page errors for users with incomplete data, yellow screen of death

**Verification:** Code review confirms proper null safety:
- All fields use `@if (!string.IsNullOrEmpty(Model.XXX))` pattern ✓
- Avatar initials logic handles empty/null fullName ✓
- No direct property access without null checks ✓

## Code Examples

Verified patterns from official sources:

### Adding Numeric Phone Validation
```csharp
// Source: CONTEXT.md decision + ManageUserViewModel.cs pattern
[StringLength(20)]
[RegularExpression(@"^[0-9]+$", ErrorMessage = "Nomor telepon hanya boleh angka")]
public string? PhoneNumber { get; set; }
```

### Hiding Password Form in AD Mode
```csharp
// Source: CONTEXT.md decision + AccountController.cs line 79 pattern
@inject IConfiguration _config
@{
    var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
}
@if (!useAD) {
    <!-- Existing Change Password form -->
} else {
    <div class="alert alert-info mb-3">
        <i class="bi bi-info-circle-fill me-2"></i>
        Password dikelola oleh Active Directory. Hubungi admin IT untuk mengubah password Anda.
    </div>
}
```

### Auto-Dismiss Alerts with jQuery
```csharp
// Source: CONTEXT.md decision + Bootstrap 5 documentation
@section Scripts {
    <script>
        $(document).ready(function() {
            setTimeout(function() {
                $('.alert').fadeOut('slow');
            }, 5000);
        });
    </script>
}
```

### Improved Password Error Messages
```csharp
// Source: AccountController.cs line 244-251 pattern extended
var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
if (result.Succeeded)
{
    await _signInManager.RefreshSignInAsync(user);
    TempData["PasswordSuccess"] = "Password berhasil diubah.";
}
else
{
    var error = result.Errors.FirstOrDefault();
    if (error != null)
    {
        TempData["PasswordError"] = error.Code switch
        {
            "PasswordMismatch" => "Password lama salah.",
            "PasswordTooShort" => "Password baru minimal 6 karakter.",
            "PasswordRequiresUniqueChars" => "Password baru harus memiliki minimal 1 karakter unik.",
            "PasswordRequiresNonAlphanumeric" => "Password baru harus memiliki minimal 1 karakter khusus.",
            "PasswordRequiresDigit" => "Password baru harus memiliki minimal 1 angka.",
            "PasswordRequiresLower" => "Password baru harus memiliki minimal 1 huruf kecil.",
            "PasswordRequiresUpper" => "Password baru harus memiliki minimal 1 huruf besar.",
            _ => "Terjadi kesalahan saat mengubah password. Coba lagi."
        };
    }
    else
    {
        TempData["PasswordError"] = "Terjadi kesalahan saat mengubah password. Coba lagi.";
    }
}
```

### Email Validation for Read-Only Field
```csharp
// Source: CONTEXT.md decision + ManageUserViewModel.cs line 21 pattern
// Note: Email is read-only in Settings, but validation adds consistency
[StringLength(100)]
[EmailAddress(ErrorMessage = "Format email tidak valid")]
public string? Email { get; set; }
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual auth checks in every action | [Authorize] attribute (class-level or per-action) | ASP.NET Core 2.0+ | Cleaner code, declarative auth |
| Session-based temp data | TempData with ITempDataProvider | ASP.NET Core 1.0 | Automatic cleanup, works with cookies |
| jQuery validation | DataAnnotations + jQuery Unobtrusive Validation | ASP.NET Core 3.0 | Server and client validation from single source |

**Deprecated/outdated:**
- **WebForms**: Replaced by MVC/Razor Pages—modern ASP.NET Core uses MVC pattern
- **Membership Provider**: Replaced by ASP.NET Core Identity—IdentityUser with UserManager is current standard
- **SimpleMembership**: Deprecated in MVC 5, removed in Core—use Identity APIs

## Open Questions

1. **Phone number format validation**
   - What we know: User decided to add numeric-only validation
   - What's unclear: Should we allow spaces, dashes, or plus prefix (e.g., "+62 812 3456 7890")?
   - Recommendation: Stick to numeric-only per user decision to avoid format complexity. If international format support needed, add separate field for country code.

2. **Auto-dismiss alert timing**
   - What we know: User decided 5-second auto-dismiss
   - What's unclear: Should alert pause on hover (user reading long message)?
   - Recommendation: Keep simple 5-second timeout per user decision. If UX issues reported, add hover pause in Phase 97 refinements.

3. **AD mode password change guidance**
   - What we know: Hide form and show message in AD mode
   - What's unclear: What specific message text? Should we link to IT support contact?
   - Recommendation: Use generic message "Hubungi admin IT untuk mengubah password" per CONTEXT.md. If organization has specific IT helpdesk procedure, add contact details in appsettings configuration.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None (manual testing only) |
| Config file | N/A — no automated test infrastructure |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ACCT-01 | Profile displays correct user data | Manual (browser) | Smoke test with existing users | ❌ No automated tests |
| ACCT-02 | Change password works | Manual (browser) | Smoke test password change flow | ❌ No automated tests |
| ACCT-03 | Profile edit saves correctly | Manual (browser) | Smoke test EditProfile form | ❌ No automated tests |
| ACCT-04 | Avatar initials display correctly | Code review | Verify initials logic | ❌ No automated tests |

### Sampling Rate
- **Per task commit:** Manual smoke test (verify specific bug fix)
- **Per wave merge:** N/A — no automated test suite
- **Phase gate:** Manual browser verification of fixed bugs

### Wave 0 Gaps
- **No test infrastructure exists** — This is a manual testing phase
- Existing seed data from Phases 83 and 87 provides adequate test coverage
- No automated test framework (xUnit, NUnit) present in project
- Smoke test approach sufficient for bug fixes (code review + browser verify)

**Note:** Phase 96 is a bug hunting phase, not test infrastructure setup. Manual testing follows the proven Phase 93-95 pattern: code review → identify bugs → fix → browser verify. Future v3.3 requirements include automated test suite (AUTO-01), but out of scope for v3.2.

## Sources

### Primary (HIGH confidence)
- **AccountController.cs** — All authentication, profile, and password logic (264 lines)
- **Views/Account/Profile.cshtml** — Profile display with avatar initials (101 lines)
- **Views/Account/Settings.cshtml** — EditProfile and ChangePassword forms (183 lines)
- **Models/ApplicationUser.cs** — User entity with custom properties (73 lines)
- **Models/SettingsViewModel.cs** — EditProfileViewModel and ChangePasswordViewModel with DataAnnotations (58 lines)
- **appsettings.json** — AD mode configuration (line 13: `"UseActiveDirectory": false`)
- **Models/ManageUserViewModel.cs** — Reference validation patterns ([EmailAddress] on line 21)

### Secondary (MEDIUM confidence)
- **Phase 95 RESEARCH.md** — Audit pattern from Admin portal audit (same bug hunting approach)
- **Phase 94 CONTEXT.md** — By-flow organization pattern (3-4 commits by functional area)
- **Phase 93 VERIFICATION.md** — Code review checklists (null safety, CSRF, validation)
- **Data/SeedTestData.cs** — Existing test users from Phase 87 (admin users, workers, various roles)

### Tertiary (LOW confidence)
- **Bootstrap 5 documentation** — Alert component auto-dismiss pattern (well-established pattern)
- **ASP.NET Core Identity documentation** — UserManager.ChangePasswordAsync error codes (documented in MSDN)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries are project defaults (ASP.NET Core 8.0, Identity, DataAnnotations)
- Architecture: HIGH - Code review completed all Account-related files, patterns are standard ASP.NET Core MVC
- Pitfalls: HIGH - All pitfalls identified from actual code review, verified against user decisions in CONTEXT.md

**Research date:** 2026-03-05
**Valid until:** 2026-04-05 (30 days — Account pages are stable, no breaking changes expected)

---

*Research complete. Ready for planning phase 96.*
*Planner can now create 96-01-PLAN.md, 96-02-PLAN.md, etc.*
