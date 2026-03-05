# Browser Verification Guide - Account Pages

**Plan:** 96-03
**Created:** 2026-03-05
**Status:** Ready for User Verification

## Overview

This guide provides step-by-step instructions for browser verification of Profile and Settings pages. All verification should be done on `https://localhost:7001`

## Pre-Verification Checklist

- [ ] Application is running (dotnet run)
- [ ] Database has test users (from Phase 87 SeedTestData)
- [ ] Browser is in normal mode (for authenticated tests)
- [ ] Private/incognito window available (for auth redirect test)

---

## Task 1: Authentication Redirect Test

**Objective:** Verify unauthenticated users are redirected to Login

**Steps:**

1. Open private/incognito browser window
2. Navigate directly to: `https://localhost:7001/Account/Profile`
3. **Expected:** Redirect to `https://localhost:7001/Account/Login`
4. Navigate directly to: `https://localhost:7001/Account/Settings`
5. **Expected:** Redirect to `https://localhost:7001/Account/Login`
6. **Expected:** No raw exceptions or error pages

**Result:** ⬜ PASS / ⬜ FAIL

**Notes:**
- Both Profile and Settings actions check `User.Identity?.IsAuthenticated` (lines 132, 152 in AccountController.cs)
- Redirect to Login action if not authenticated

---

## Task 2: Profile Display - Complete Data

**Objective:** Verify Profile page displays correctly for user with complete data

**Steps:**

1. Login as admin user (email: `admin@hc.com`, password: `Admin123!`)
2. Navigate to: `https://localhost:7001/Account/Profile`
3. Verify all fields display correctly:

| Field | Expected Value | Expected Display |
|-------|---------------|------------------|
| Avatar Initials | "AD" | Blue circle with "AD" |
| Nama Lengkap | "Admin User" | "Admin User" |
| NIP | "1001" | "1001" |
| Email | "admin@hc.com" | "admin@hc.com" |
| Telepon | "081234567890" | "081234567890" |
| Direktorat | "Corporate HC" | "Corporate HC" |
| Bagian | "Human Capital" | "Human Capital" |
| Unit | "-" | "-" (empty field) |
| Jabatan | "HC Manager" | "HC Manager" |
| Role | "Admin" | "Admin" |

4. Verify no "-" placeholders for fields with data
5. Verify page renders without errors
6. Verify "Edit Profile" button is visible

**Result:** ⬜ PASS / ⬜ FAIL

**Code Reference:** Profile.cshtml lines 5-9 (avatar logic), lines 32-91 (field display)

**Avatar Initials Logic:**
```csharp
var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
var initials = nameParts.Length >= 2
    ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
    : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : "?");
```

---

## Task 3: Profile Display - Incomplete Data

**Objective:** Verify Profile page handles incomplete data gracefully

**Steps:**

1. Find a user with incomplete profile OR create one via Admin > ManageWorkers:
   - Leave NIP empty
   - Leave Phone empty
   - Leave Position empty
2. Login as that user
3. Navigate to: `https://localhost:7001/Account/Profile`
4. Verify empty fields show "-" placeholder
5. Verify avatar initials:
   - **Multi-word name:** First 2 chars of first 2 words (e.g., "John Doe" → "JD")
   - **Single-word name:** First 2 characters (e.g., "Admin" → "AD")
   - **Empty name:** "?" (fallback)
6. Verify no null reference exceptions or error pages

**Test Cases:**

| User Name | Expected Initials | Reason |
|-----------|-------------------|---------|
| "Admin User" | "AU" | Two words → first char of each |
| "Administrator" | "AD" | One word → first 2 chars |
| "" (empty) | "?" | Fallback for empty |

**Result:** ⬜ PASS / ⬜ FAIL

**Known Issue:** Single-character names (e.g., "A") will show "?" instead of "A" - this was noted in plan 96-01 as low-priority

---

## Task 4: Edit Profile Validation

**Objective:** Verify Edit Profile form validation works correctly

**Steps:**

1. Login as any user (e.g., worker: `worker1@hc.com` / `Worker123!`)
2. Navigate to: `https://localhost:7001/Account/Settings`
3. Verify "Edit Profil" section is visible
4. **Test phone validation:**
   - Enter "abc123" in phone field
   - Click "Simpan Profil"
   - **Expected:** Error message "Nomor telepon hanya boleh angka"
5. **Test email validation:**
   - Note: Email field is read-only (disabled) so validation is defense-in-depth
   - The `[EmailAddress]` attribute exists on SettingsViewModel.Email (line 17)
6. **Test successful edit:**
   - Enter valid phone: "08123456789"
   - Click "Simpan Profil"
   - **Expected:** Success message "Profil berhasil diperbarui." appears
   - **Expected:** Message auto-dismisses after 5 seconds
7. Verify data persisted by navigating to Profile page

**Validation Rules (from SettingsViewModel.cs):**

| Field | Rule | Error Message |
|-------|------|---------------|
| FullName | Required | "Nama lengkap harus diisi" |
| PhoneNumber | Numeric only | "Nomor telepon hanya boleh angka" |
| PhoneNumber | Max 20 chars | (Server-side) |
| Email | Email format | "Format email tidak valid" (read-only field) |

**Result:** ⬜ PASS / ⬜ FAIL

---

## Task 5: Change Password - Local Auth Mode

**Objective:** Verify Change Password flow in local authentication mode

**Prerequisites:** appsettings.json has `"UseActiveDirectory": false` (default)

**Steps:**

1. Login as any user (e.g., `worker1@hc.com` / `Worker123!`)
2. Navigate to: `https://localhost:7001/Account/Settings`
3. Scroll to "Ubah Password" section
4. **Verify form is visible** (not hidden)
5. **Test wrong current password:**
   - Current Password: "WrongPassword123!"
   - New Password: "NewPassword123!"
   - Confirm: "NewPassword123!"
   - Click "Ubah Password"
   - **Expected:** Error message "Password lama salah."
6. **Test password too short:**
   - Current Password: "Worker123!"
   - New Password: "12345" (5 chars)
   - Confirm: "12345"
   - Click "Ubah Password"
   - **Expected:** Error message "Password baru minimal 6 karakter."
7. **Test password mismatch:**
   - Current Password: "Worker123!"
   - New Password: "NewPassword123!"
   - Confirm: "DifferentPassword123!"
   - Click "Ubah Password"
   - **Expected:** Error message "Password baru dan konfirmasi tidak cocok."
8. **Test successful change:**
   - Current Password: "Worker123!"
   - New Password: "NewWorker123!"
   - Confirm: "NewWorker123!"
   - Click "Ubah Password"
   - **Expected:** Success message "Password berhasil diubah."
   - **Expected:** Message auto-dismisses after 5 seconds
9. **Verify login with new password:**
   - Logout
   - Login with: `worker1@hc.com` / `NewWorker123!`
   - **Expected:** Login successful
10. **Revert password for cleanup:**
    - Change back to "Worker123!"

**Password Error Messages (Indonesian):**

| Error Code | Message |
|------------|---------|
| PasswordMismatch | "Password lama salah." |
| PasswordTooShort | "Password baru minimal 6 karakter." |
| PasswordRequiresUniqueChars | "Password baru harus memiliki minimal 1 karakter unik." |
| PasswordRequiresNonAlphanumeric | "Password baru harus memiliki minimal 1 karakter khusus." |
| PasswordRequiresDigit | "Password baru harus memiliki minimal 1 angka." |
| PasswordRequiresLower | "Password baru harus memiliki minimal 1 huruf kecil." |
| PasswordRequiresUpper | "Password baru harus memiliki minimal 1 huruf besar." |
| Generic | "Terjadi kesalahan saat mengubah password. Coba lagi." |

**Result:** ⬜ PASS / ⬜ FAIL

**Code Reference:** AccountController.cs lines 236-266

---

## Task 6: Change Password - AD Mode

**Objective:** Verify Change Password form is hidden in AD mode

**Prerequisites:** Change appsettings.json to `"UseActiveDirectory": true` and restart

**Steps:**

1. Stop application
2. Edit `appsettings.json`:
   ```json
   "Authentication": {
     "UseActiveDirectory": true
   }
   ```
3. Restart application (`dotnet run`)
4. Login as any user (or skip if AD not configured)
5. Navigate to: `https://localhost:7001/Account/Settings`
6. **Verify "Ubah Password" section shows:**
   - Card header: "Ubah Password" with blue background
   - Info message: "Password dikelola oleh Active Directory. Hubungi admin IT untuk mengubah password Anda."
   - **No password form inputs** (Current Password, New Password, Confirm fields)
7. Take screenshot for documentation
8. **Revert changes:**
   - Stop application
   - Edit `appsettings.json`: `"UseActiveDirectory": false`
   - Restart application

**Expected HTML Structure (AD Mode):**
```html
<div class="card mb-4">
  <div class="card-header bg-primary text-white">
    <h5 class="mb-0"><i class="bi bi-key-fill me-2"></i>Ubah Password</h5>
  </div>
  <div class="card-body">
    <div class="alert alert-info mb-0">
      <i class="bi bi-info-circle-fill me-2"></i>
      Password dikelola oleh Active Directory...
    </div>
  </div>
</div>
```

**Result:** ⬜ PASS / ⬜ FAIL / ⬜ SKIPPED (AD not configured)

**Code Reference:** Settings.cshtml lines 131-212 (conditional rendering)

---

## Task 7: Navigation Links

**Objective:** Verify Profile ↔ Settings navigation works correctly

**Steps:**

1. Login as any user
2. Navigate to: `https://localhost:7001/Account/Profile`
3. Click "Edit Profile" button
4. **Expected:** Redirect to `https://localhost:7001/Account/Settings`
5. Click "Kembali ke Profil" link (top-left)
6. **Expected:** Redirect to `https://localhost:7001/Account/Profile`
7. **Expected:** No broken navigation or 404 errors

**Result:** ⬜ PASS / ⬜ FAIL

**Code Reference:**
- Profile.cshtml line 94-96 (Edit Profile button)
- Settings.cshtml line 11-13 (Back to Profile link)

---

## Task 8: Success/Error Message Auto-Dismiss

**Objective:** Verify all success/error messages auto-dismiss after 5 seconds

**Test Scenarios:**

| Scenario | Trigger | Expected Message | Auto-Dismiss |
|----------|---------|------------------|--------------|
| Profile save success | Edit Profile → save with valid data | "Profil berhasil diperbarui." | 5 seconds |
| Profile save error | Edit Profile → save with invalid phone | "Nomor telepon hanya boleh angka" | 5 seconds |
| Password change success | Change Password → successful change | "Password berhasil diubah." | 5 seconds |
| Password change error | Change Password → wrong current password | "Password lama salah." | 5 seconds |

**Steps:**

1. Trigger any of the scenarios above
2. Start timer when message appears
3. **Expected:** Message fades out after 5 seconds
4. **Expected:** Message can be dismissed manually via "X" button

**Code Reference:** Settings.cshtml lines 217-220 (JavaScript auto-dismiss)

**Result:** ⬜ PASS / ⬜ FAIL

---

## Requirements Verification Matrix

Map test tasks to requirements:

| Requirement | Description | Test Tasks | Status |
|-------------|-------------|------------|--------|
| ACCT-01 | Profile page displays correct user data | 2, 3 | ⬜ PASS / ⬜ FAIL |
| ACCT-02 | Settings page change password works | 5, 6 | ⬜ PASS / ⬜ FAIL |
| ACCT-03 | Profile edit saves correctly | 4 | ⬜ PASS / ⬜ FAIL |
| ACCT-04 | Avatar initials display correctly | 2, 3 | ⬜ PASS / ⬜ FAIL |

---

## Summary Results

**Total Tasks:** 8
**Passed:** ⬜ 0
**Failed:** ⬜ 0
**Skipped:** ⬜ 0

**Overall Status:** ⬜ ALL PASS / ⬜ HAS FAILURES

**If ALL PASS:**
- Phase 96 is complete
- All ACCT-01 through ACCT-04 requirements verified
- No gap closure plans needed

**If ANY FAIL:**
- Document failures with severity (Critical/High/Medium/Low)
- Create gap closure plan(s) for fixes
- Re-run verification after fixes

---

## Code Verification Summary

**Pre-verification Code Analysis:**

| Component | Verification Method | Status |
|-----------|---------------------|--------|
| Authentication redirect | Code review (AccountController.cs 132-135, 152-155) | ✅ Correct |
| Profile null handling | Code review (Profile.cshtml 32-91) | ✅ Correct |
| Avatar initials logic | Code review (Profile.cshtml 7-9) | ⚠️ Known issue: single-char names |
| Edit Profile validation | Code review (SettingsViewModel.cs 29-41) | ✅ Correct |
| Change Password flow | Code review (AccountController.cs 236-266) | ✅ Correct |
| Indonesian error messages | Code review (AccountController.cs 247-257) | ✅ Correct |
| AD mode conditional | Code review (Settings.cshtml 131-212) | ✅ Correct |
| Auto-dismiss script | Code review (Settings.cshtml 217-220) | ✅ Correct |
| Navigation links | Code review (Profile.cshtml 94-96, Settings.cshtml 11-13) | ✅ Correct |

**Known Issues from Previous Plans:**

1. **Plan 96-01:** Avatar initials don't handle single-character names (shows "?" instead of the character) - Medium severity, deemed acceptable edge case

---

## Test Data Reference

**Default Users (from Phase 87 SeedTestData):**

| Email | Password | Role | Name | Profile Complete |
|-------|----------|------|------|------------------|
| admin@hc.com | Admin123! | Admin | Admin User | ✅ Yes |
| hc@hc.com | Hc123! | HC | HC User | ✅ Yes |
| srspv@hc.com | Srspv123! | SrSpv | Sr Spv User | ✅ Yes |
| sectionhead@hc.com | Sh123! | SectionHead | Section Head User | ✅ Yes |
| coach@hc.com | Coach123! | Coach | Coach User | ✅ Yes |
| worker1@hc.com | Worker123! | Worker | Worker One | ✅ Yes |

---

## Next Steps After Verification

1. **If ALL PASS:**
   - Report results in execution summary
   - Mark Phase 96 complete
   - Update STATE.md with completion

2. **If FAILURES FOUND:**
   - Document each failure with:
     - Task number
     - Expected behavior
     - Actual behavior
     - Severity assessment
     - Suggested fix (if obvious)
   - Create gap closure plan(s) for Phase 96

---

**Verification Guide End**
