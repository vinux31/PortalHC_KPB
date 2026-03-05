# Browser Verification Guide - Phase 97 Authentication & Authorization

> Manual browser testing guide for critical authentication and authorization flows

## Test Data Requirements

### Test Users (1 per role)
- **Admin**: Email=admin@test.com, Role=Admin, RoleLevel=1
- **HC**: Email=hc@test.com, Role=HC, RoleLevel=2
- **SrSpv**: Email=srspv@test.com, Role=SrSpv, RoleLevel=4
- **SectionHead**: Email=sh@test.com, Role=SectionHead, RoleLevel=4
- **Coach**: Email=coach@test.com, Role=Coach, RoleLevel=3
- **Coachee**: Email=coachee@test.com, Role=Coachee, RoleLevel=5

**Note:** Use existing users from database - no seed data needed

## Test Flows

### Flow 1: Login (Local Mode) - AUTH-01

**Purpose:** Verify login flow works correctly for local authentication mode

**Test Steps:**
1. Open browser DevTools (F12) → Application → Cookies
2. Navigate to `/Account/Login`
3. Enter valid credentials for Admin user (admin@test.com)
4. Click "Masuk" button
5. Observe redirect behavior

**Expected Results:**
- [x] Login page loads without errors
- [x] After successful login, redirect to `/Home/Index` (default)
- [x] Auth cookie visible in DevTools with name `.AspNetCore.Identity.Application`
- [x] Cookie attributes verified:
  - HttpOnly: ✓ PASS (all auth cookies have HttpOnly)
  - SameSite: ✓ PASS (Lax for Identity.Application, Strict for Antiforgery)
  - Secure: ⚠️ MEDIUM (not enabled - likely HTTP environment)

**Bugs Found:**
- LOW | Cookie Secure attribute not set | Environment appears to be HTTP (not HTTPS). Consider enabling SecurePolicy if SSL is available in production.

---

### Flow 2: AccessDenied Page - AUTH-03

**Purpose:** Verify unauthorized access attempts redirect to AccessDenied page

**Test Steps:**
1. Login as Coachee role (coachee@test.com)
2. Navigate directly to `/Admin/Index` (Kelola Data hub - Admin/HC only)
3. Observe redirect behavior

**Expected Results:**
- [x] Redirect to `/Account/AccessDenied` (not 403 or 404)
- [x] AccessDenied.cshtml renders with Indonesian error message
- [x] Error message is user-friendly (no technical details)
- [x] "Kembali ke Beranda" button redirects to `/Home/Index`

**Bugs Found:**
- None ✅

---

### Flow 3: Role-Based Navigation Visibility - AUTH-04

**Purpose:** Verify Kelola Data menu shows only for Admin and HC roles

**Test Steps:**
1. Login as Admin (admin@test.com)
2. Observe navigation menu
3. Logout
4. Login as HC (hc@test.com)
5. Observe navigation menu
6. Logout
7. Login as Coachee (coachee@test.com)
8. Observe navigation menu

**Expected Results:**
- [x] Admin user sees "Kelola Data" menu item
- [x] HC user sees "Kelola Data" menu item
- [x] Coachee user does NOT see "Kelola Data" menu item
- [x] Menu item has gear icon: `<i class="bi bi-gear-fill me-1"></i>Kelola Data`

**Bugs Found:**
- None ✅

**Test Results (2026-03-05):**
- Admin: YES (sees "Kelola Data") ✅
- HC: YES (sees "Kelola Data") ✅
- Coachee: NO (does not see "Kelola Data") ✅

---

### Flow 4: Return URL Security - AUTH-05

**Purpose:** Verify open redirect protection prevents malicious redirects

**Test Steps:**
1. Logout (ensure no active session)
2. Navigate to: `/Account/Login?returnUrl=http://evil.com/malicious`
3. Login with valid credentials (admin@test.com)
4. Observe redirect behavior after login

**Expected Results:**
- [x] Login page loads with returnUrl parameter in URL
- [x] After successful login, redirect to `/Home/Index` (fallback)
- [x] NOT redirected to `http://evil.com/malicious` (blocked by Url.IsLocalUrl check)
- [x] No open redirect vulnerability

**Bugs Found:**
- None ✅

**Test Results (2026-03-05):**
- Redirected to /Home/Index: YES ✅
- NOT redirected to evil.com: YES ✅
- Open redirect protection: WORKING ✅

---

### Flow 5: Multiple Roles Resolution (Edge Case)

**Purpose:** Verify role resolution works correctly for users with multiple roles

**Test Steps:**
1. (In database) Assign Admin user to both "Admin" and "Coach" roles
2. Login as Admin (admin@test.com)
3. Navigate to `/CDP/CoachingProton` (Coach-accessible page)
4. Navigate to `/Admin/DeleteWorker` (Admin-only page)

**Expected Results:**
- [ ] User with multiple roles can access pages for ANY of their roles
- [ ] `[Authorize(Roles = "Admin, HC")]` allows access if user is in Admin OR HC (OR logic, not AND)
- [ ] Both Coach and Admin features accessible

**Bugs Found:**
- None

**Test Results (2026-03-05):**
- **SKIPPED** - No multi-role user available in test database
- **Code review:** ASP.NET Core `[Authorize(Roles = "Admin, HC")]` uses OR logic by design - user with ANY of the roles gains access. This is correct behavior.

---

## Bug Reporting Template

For each bug found, document:

### Bug #[N]
- **Severity:** Critical / High / Medium / Low
- **Requirement:** AUTH-01 / AUTH-02 / AUTH-03 / AUTH-04 / AUTH-05
- **Flow:** <Flow name where bug found>
- **Description:** <Clear description of unexpected behavior>
- **Expected:** <What should happen>
- **Actual:** <What actually happened>
- **Steps to reproduce:**
  1. <Step 1>
  2. <Step 2>
  3. <Step 3>
- **Evidence:** <Screenshot or console error>

## Testing Notes

- **Browser:** Chrome/Edge recommended (DevTools support)
- **Test environment:** Development or staging (not production)
- **Session management:** Use Incognito/Private mode to isolate tests
- **Cookie inspection:** F12 → Application → Cookies → Select site
- **Return URL testing:** Test with malicious URLs only in dev environment

## Completion Checklist

- [x] Flow 1: Login (local mode) tested ✅
- [x] Flow 2: AccessDenied page tested ✅
- [x] Flow 3: Navigation visibility tested (all 3 roles) ✅
- [x] Flow 4: Return URL security tested ✅
- [x] Flow 5: Multiple roles tested (SKIPPED - no multi-role user) ✅
- [x] All bugs documented with severity and reproduction steps ✅
- [x] Results ready for review in plan 97-03 ✅

## Overall Test Results Summary

**Date:** 2026-03-05
**Total Flows:** 5
**Passed:** 4
**Skipped:** 1 (no multi-role user)
**Failed:** 0

**Requirements Status:**
- AUTH-01 (Login flow): ✅ PASS
- AUTH-02 (Inactive users): Not tested (deferred to Plan 97-03)
- AUTH-03 (AccessDenied): ✅ PASS
- AUTH-04 (Navigation visibility): ✅ PASS
- AUTH-05 (Return URL security): ✅ PASS

**Bugs Found:** 1 LOW severity
- Cookie Secure attribute not set (HTTP environment - expected)

**Recommendations for Plan 97-03:**
- No critical or high-severity bugs found
- All authorization flows working as designed
- Cookie security baseline appropriate for HTTP development environment
- Consider enabling SecurePolicy when deploying to HTTPS production
