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
- Expected: 2-3 commits depending on findings
- Matches Phase 94's by-flow approach and keeps changes organized by feature area

### Testing Approach
- **Smoke test only** — quick verification that pages load and obvious bugs are fixed
- Pattern: Code review → identify bugs → fix → browser verify (same as Phases 93-95)
- Focus on verifying the specific bug that was fixed
- Browser testing only when code review is unclear or requires runtime verification

### Bug Priority
- Claude's discretion — prioritize based on severity and user impact
- Critical: crashes, null references, raw exceptions shown to users
- High: broken flows, incorrect data displayed, navigation failures
- Medium: UX issues (unclear text, missing links, confusing UI)
- Low: cosmetic issues, typos, minor inconsistencies

### Test Data Approach
- **Pakai existing users** — users from prior phases (Phase 83 workers, Phase 87 seed data)
- Test with different user data scenarios:
  - User with all fields populated
  - User with missing optional fields (NIP, Position, PhoneNumber, Section, Directorate, Unit)
  - User with single-word name (avatar initials edge case)
  - User with two-word name (standard initials)
  - User with multi-word name (3+ parts)
- Pragmatic approach: use existing DB users, add only if specific edge case needed

### Specific Areas to Check

**Profile page:**
- Avatar initials logic correctness (lines 5-9 in Profile.cshtml)
- Null/empty handling for all displayed fields
- "Edit Profile" button link to Settings page
- Role display fallback for users without roles

**Settings Edit Profile:**
- FullName required validation working
- Position and PhoneNumber optional handling
- Save persistence (changes actually saved to DB)
- Success/error TempData messages displaying
- Navigation back to Profile after save

**Change Password:**
- Current password validation
- New password min 6 character enforcement
- Confirm password compare validation
- Password mismatch error message (line 244-246 in AccountController)
- RefreshSignInAsync called after successful change
- Error messages for generic password failures

### Claude's Discretion
- Exact order of bug fixes within each area
- Whether to group fixes by area or by bug category
- Which null safety checks are actually needed vs defensive coding
- How deep to investigate each edge case vs smoke test

</decisions>

<specifics>
## Specific Ideas

- Follow Phase 93-95 audit pattern: Code review → Identify bugs → Fix → Smoke test
- Commit style: `fix(account): [description]` with Co-Authored-By footer
- Preserve existing functionality — bug fixes only, no behavior changes
- Focus on the 2 Account pages only — Login/Logout already audited in Phase 87 (DASH-04)
- "Secara menyeluruh dan detail" — thoroughness is the priority (from Phase 95)

</specifics>

<code_context>
## Existing Code Insights

### Key Files
- `Controllers/AccountController.cs` (264 lines) — login, logout, profile, settings, EditProfile POST, ChangePassword POST
- `Views/Account/Profile.cshtml` (101 lines) — read-only display with avatar initials
- `Views/Account/Settings.cshtml` (183 lines) — two separate forms: Edit Profile + Change Password
- `Models/SettingsViewModel.cs` — EditProfileViewModel and ChangePasswordViewModel with DataAnnotations validation
- `Models/ApplicationUser.cs` — FullName, NIP, Position, Section, Directorate, Unit, JoinDate, RoleLevel, SelectedView, IsActive, PhoneNumber (from IdentityUser)

### Established Patterns from Prior Audits
- **Phase 93 (CMP Audit)**: Localization sweep using `CultureInfo.GetCultureInfo("id-ID")`, null checks for DateTime, CSRF token verification
- **Phase 94 (CDP Audit)**: Flow-based organization, role-based filtering, validation error handling via TempData
- **Phase 95 (Admin Audit)**: Per-page organization, cross-cutting concerns in separate commits, pragmatic test data usage

### Avatar Initials Logic (Profile.cshtml lines 5-9)
```csharp
var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
var initials = nameParts.Length >= 2
    ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
    : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : "?");
```
- Edge cases: single-word names, empty/null names, multi-word names (uses first 2 parts only)

### Validation Rules (SettingsViewModel.cs)
- EditProfileViewModel: FullName `[Required]`, Position/PhoneNumber optional `[StringLength(100)]/[StringLength(20)]`
- ChangePasswordViewModel: All fields `[Required]`, NewPassword `[StringLength(100, MinimumLength = 6)]`, Confirm `[Compare("NewPassword")]`

### Error Handling Patterns
- TempData for success/error messages: ProfileSuccess, ProfileError, PasswordSuccess, PasswordError
- Password mismatch specific error: `if (result.Errors.Any(e => e.Code == "PasswordMismatch"))`
- Generic password error fallback: `string.Join("; ", result.Errors.Select(e => e.Description))`

### Integration Points
- Profile page links to Settings via "Edit Profile" button
- Settings page links back to Profile via "Kembali ke Profil" anchor
- Both pages require authentication (redirect to Login if not authenticated)
- UserManager and SignInManager injected into AccountController for user operations

### Reusable Assets
- UserManager<ApplicationUser> — for user lookup and updates
- SignInManager<ApplicationUser> — for RefreshSignInAsync after password change
- DataAnnotations validation — server-side ModelState validation

### Known Bug Patterns to Look For
- **Null safety**: Check Model.XXX null checks in Profile.cshtml
- **Empty string handling**: `string.IsNullOrEmpty(Model.FullName)` pattern used
- **Avatar edge cases**: Single-word names, names with special characters, null/empty names
- **Password errors**: All error paths tested (wrong current, too short new, mismatch confirm)
- **CSRF tokens**: `@Html.AntiForgeryToken()` present in both forms
- **ModelState validation**: Check for missing ModelState.IsValid checks in POST actions

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 96-account-pages-audit*
*Context gathered: 2026-03-05*
