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
- [ ] Login page loads without errors
- [ ] After successful login, redirect to `/Home/Index` (default)
- [ ] Auth cookie visible in DevTools with name `.AspNetCore.Identity.Application`
- [ ] Cookie attributes visible: HttpOnly=true, Secure=<depends on SSL>, SameSite=Lax

**Bugs Found:**
- <Document any issues with format: SEVERITY | Description | Steps to reproduce>

---

### Flow 2: AccessDenied Page - AUTH-03

**Purpose:** Verify unauthorized access attempts redirect to AccessDenied page

**Test Steps:**
1. Login as Coachee role (coachee@test.com)
2. Navigate directly to `/Admin/Index` (Kelola Data hub - Admin/HC only)
3. Observe redirect behavior

**Expected Results:**
- [ ] Redirect to `/Account/AccessDenied` (not 403 or 404)
- [ ] AccessDenied.cshtml renders with Indonesian error message
- [ ] Error message is user-friendly (no technical details)
- [ ] "Kembali ke Beranda" button redirects to `/Home/Index`

**Bugs Found:**
- <Document any issues>

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
- [ ] Admin user sees "Kelola Data" menu item
- [ ] HC user sees "Kelola Data" menu item
- [ ] Coachee user does NOT see "Kelola Data" menu item
- [ ] Menu item has gear icon: `<i class="bi bi-gear-fill me-1"></i>Kelola Data`

**Bugs Found:**
- <Document any issues>

---

### Flow 4: Return URL Security - AUTH-05

**Purpose:** Verify open redirect protection prevents malicious redirects

**Test Steps:**
1. Logout (ensure no active session)
2. Navigate to: `/Account/Login?returnUrl=http://evil.com/malicious`
3. Login with valid credentials (admin@test.com)
4. Observe redirect behavior after login

**Expected Results:**
- [ ] Login page loads with returnUrl parameter in URL
- [ ] After successful login, redirect to `/Home/Index` (fallback)
- [ ] NOT redirected to `http://evil.com/malicious` (blocked by Url.IsLocalUrl check)
- [ ] No open redirect vulnerability

**Bugs Found:**
- <Document any issues>

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
- <Document any issues>

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

- [ ] Flow 1: Login (local mode) tested
- [ ] Flow 2: AccessDenied page tested
- [ ] Flow 3: Navigation visibility tested (all 3 roles)
- [ ] Flow 4: Return URL security tested
- [ ] Flow 5: Multiple roles tested
- [ ] All bugs documented with severity and reproduction steps
- [ ] Results ready for review in plan 97-03
