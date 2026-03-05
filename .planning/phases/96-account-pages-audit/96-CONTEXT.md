# Phase 96: Account Pages Audit - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit Account (Profile & Settings) pages for bugs — Profile page displays user info with avatar initials, Settings page handles profile edits and password changes. Focus is finding and fixing bugs, NOT adding new features or changing functionality.

**Pages to Audit:**
- /Account/Profile — Read-only display with avatar initials, user identity (Nama, NIP, Email, Telepon), org info (Direktorat, Bagian, Unit, Jabatan, Role)
- /Account/Settings — Edit Profile form (FullName, Position, PhoneNumber) + Change Password form (Current, New, Confirm)

**Requirements:** ACCT-01 through ACCT-04 (display correct data, password change works, profile edit saves, avatar initials display correctly)

</domain>

<decisions>
## Implementation Decisions

### Audit Organization
- **Per functional area** — Profile bugs → satu commit, Settings Edit Profile bugs → satu commit, Change Password bugs → satu commit
- Expected: 3-4 commits tergantung findings
- Matches Phase 94's by-flow approach and keeps changes organized by feature area

### Testing Approach
- **Code review untuk sebagian besar** — Profile, Edit Profile, password change, CSRF, null safety
- **Browser test hanya untuk** — Authentication check (coba akses langsung tanpa login)
- Focus on verifying the specific bug that was fixed

### Bug Priority
- Claude's discretion — prioritize based on severity and user impact
- Critical: crashes, null references, raw exceptions shown to users
- High: broken flows, incorrect data displayed, navigation failures
- Medium: UX issues (unclear text, missing links, confusing UI)
- Low: cosmetic issues, typos, minor inconsistencies

### Test Data Approach
- **Pakai existing users** — users dari prior phases (Phase 83 workers, Phase 87 seed data)
- Pragmatic approach: gunakan existing DB users, tambah hanya jika edge case spesifik diperlukan

## Area 1: Profile Page

### Avatar Initials
- **Keputusan**: Biarkan logic existing
- Logic saat ini: 2 kata pertama untuk nama multi-kata, 2 karakter pertama untuk satu kata, '?' untuk kosong
- Tidak perlu perubahan

### Empty Field Placeholder
- **Keputusan**: Biarkan tampil '-' untuk field kosong
- Sudah konsisten dengan code saat ini

### Role Display
- **Keputusan**: Biarkan "No Role" untuk user tanpa role
- Sudah appropriate

### Testing
- Code review saja — tidak perlu browser test untuk Profile

## Area 2: Edit Profile (Settings)

### Validation Rules
- **Keputusan**: Tambah validasi
1. **PhoneNumber** — tambah validasi numeric only (hanya angka)
2. **Email** — tambah validasi format email, walau field read-only (untuk consistency)

### Success/Error Messages
- **Keputusan**: Improve dengan auto-dismiss
- Tambah JavaScript untuk auto-dismiss alert setelah 5 detik
- Gunakan Bootstrap alert + setTimeout untuk remove

### Testing
- Code review saja — cek validation dan auto-dismiss logic

## Area 3: Change Password

### AD Mode Handling
- **Keputusan**: Sembunyikan form ganti password jika mode AD aktif
- Baca config `Authentication:UseActiveDirectory`
- Jika true, sembunyikan section "Ubah Password" di Settings.cshtml
- Tambah info/message bahwa password dikelola oleh AD

### Local Mode
- **Keputusan**: Biarkan minimum 6 karakter untuk local auth
- Tidak perlu complexity requirement

### Error Messages
- **Keputusan**: Perbaiki bahasa Indonesia
- Review semua pesan error password dan perbaiki agar lebih natural/jelas

### Testing
- Code review saja untuk logic hide form dan password change
- Untuk AD: tidak perlu test ganti password (form di-hide)

## Area 4: Cross-page & Auth

### Navigation
- **Keputusan**: Code review saja
- Cek link "Edit Profile" di Profile → Settings
- Cek link "Kembali ke Profil" di Settings → Profile

### CSRF Protection
- **Keputusan**: Verifikasi CSRF
- Pastikan @Html.AntiForgeryToken() ada di kedua form
- Pastikan [ValidateAntiForgeryToken] attribute di POST actions
- Verify implementasi correct

### Authentication Check
- **Keputusan**: Perlu browser test
- Test: coba akses /Account/Profile dan /Account/Settings tanpa login
- Verify redirect ke Login page

### Null Safety
- **Keputusan**: Code review saja
- Cek null handling untuk Model dan ViewBag di views
- Cek user null check di controller actions

### Claude's Discretion
- Exact order of bug fixes within each area
- Grouping fixes into commits
- Detail improvement untuk bahasa Indonesia error messages

</decisions>

<specifics>
## Specific Ideas

- Follow Phase 93-95 audit pattern: Code review → Identify bugs → Fix → Smoke test
- Commit style: `fix(account): [description]` dengan Co-Authored-By footer
- Preserve existing functionality — bug fixes only, no behavior changes
- Focus pada 2 Account pages saja — Login/Logout sudah audited di Phase 87

**Validasi yang ditambahkan:**
- Phone: `[RegularExpression(@"^[0-9]+$", ErrorMessage = "Nomor telepon hanya boleh angka")]`
- Email: `[EmailAddress(ErrorMessage = "Format email tidak valid")]`

**Auto-dismiss alert (JavaScript):**
```javascript
setTimeout(function() {
    $('.alert').fadeOut('slow');
}, 5000);
```

**Hide password form untuk AD mode (Settings.cshtml):**
```csharp
@{
    var useAD = Context.RequestServices.GetRequiredService<IConfiguration>()
        .GetValue<bool>("Authentication:UseActiveDirectory", false);
}
@if (!useAD) {
    <!-- Password form section -->
}
```

</specifics>

<code_context>
## Existing Code Insights

### Key Files
- `Controllers/AccountController.cs` (264 lines) — login, logout, profile, settings, EditProfile POST, ChangePassword POST
- `Views/Account/Profile.cshtml` (101 lines) — read-only display dengan avatar initials
- `Views/Account/Settings.cshtml` (183 lines) — dua separate forms: Edit Profile + Change Password
- `Models/SettingsViewModel.cs` — EditProfileViewModel dan ChangePasswordViewModel dengan DataAnnotations validation
- `Models/ApplicationUser.cs` — FullName, NIP, Position, Section, Directorate, Unit, JoinDate, RoleLevel, SelectedView, IsActive, PhoneNumber (dari IdentityUser)

### Avatar Initials Logic (Profile.cshtml lines 5-9)
```csharp
var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
var initials = nameParts.Length >= 2
    ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
    : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : "?");
```
- Edge cases: single-word names, empty/null names, multi-word names (uses first 2 parts)

### Validation Rules (SettingsViewModel.cs)
- EditProfileViewModel: FullName `[Required]`, Position/PhoneNumber optional `[StringLength(100)]/[StringLength(20)]`
- ChangePasswordViewModel: All fields `[Required]`, NewPassword `[StringLength(100, MinimumLength = 6)]`, Confirm `[Compare("NewPassword")]`

### Error Handling Patterns
- TempData untuk success/error messages: ProfileSuccess, ProfileError, PasswordSuccess, PasswordError
- Password mismatch specific error: `if (result.Errors.Any(e => e.Code == "PasswordMismatch"))`
- Generic password error fallback: `string.Join("; ", result.Errors.Select(e => e.Description))`

### CSRF Protection
- `@Html.AntiForgeryToken()` ada di kedua form (Settings.cshtml lines 35, 147)
- `[ValidateAntiForgeryToken]` attribute ada di EditProfile dan ChangePassword POST actions

### Authentication Checks
- Profile action: `if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");`
- Settings action: `if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");`

### Integration Points
- Profile links ke Settings via "Edit Profile" button (line 94-96)
- Settings links back ke Profile via "Kembali ke Profil" anchor (line 9-11)
- Both pages require authentication (redirect ke Login jika tidak authenticated)

### AD Mode Config
- Login action membaca config: `_config.GetValue<bool>("Authentication:UseActiveDirectory", false)`
- Config key: `Authentication:UseActiveDirectory`

### Reusable Assets
- UserManager<ApplicationUser> — untuk user lookup dan updates
- SignInManager<ApplicationUser> — untuk RefreshSignInAsync setelah password change
- DataAnnotations validation — server-side ModelState validation
- IConfiguration — untuk baca AD mode setting

### Known Bug Patterns to Look For
- **Null safety**: Cek Model.XXX null checks di Profile.cshtml
- **Empty string handling**: `string.IsNullOrEmpty(Model.FullName)` pattern used
- **Avatar edge cases**: Single-word names, names dengan special characters, null/empty names
- **Password errors**: All error paths tested (wrong current, too short new, mismatch confirm)
- **CSRF tokens**: Verify `@Html.AntiForgeryToken()` present di kedua forms
- **ModelState validation**: Cek untuk missing ModelState.IsValid checks di POST actions
- **AD mode detection**: Verify logic baca config dan hide form

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 96-account-pages-audit*
*Context gathered: 2026-03-05*
