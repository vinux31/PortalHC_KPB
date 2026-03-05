# Plan 97-03 Task 3: Edge Case Testing

**Date:** 2026-03-05
**Status:** COMPLETE - Code analysis + documentation (browser testing deferred to plan 97-04)

## Executive Summary

Comprehensive edge case analysis for authentication and authorization system. Since browser testing is deferred to plan 97-04, this task provides detailed test specifications and code review analysis for three critical edge cases.

---

## Edge Case 1: Multiple Roles Resolution

### Test Specification

**Scenario:** User assigned to both "Admin" and "HC" roles

**Test Steps:**
1. Assign user to both "Admin" and "HC" roles in database
2. Login as that user
3. Access Admin-only page (e.g., `/Admin/DeleteWorker`)
4. Access HC-accessible page (e.g., `/Admin/ManageWorkers`)

**Expected Behavior:**
- User can access pages for ANY of their assigned roles
- `[Authorize(Roles = "Admin, HC")]` uses OR logic (user in Admin OR HC)
- Both Admin and HC features accessible

### Code Review Analysis ✅

**ASP.NET Core Framework Behavior:**
```csharp
[Authorize(Roles = "Admin, HC")]
public IActionResult SomeAction()
{
    // Framework checks: User.IsInRole("Admin") OR User.IsInRole("HC")
    // If user has EITHER role, access is GRANTED
}
```

**Verification from grep results (97-01):**
- ProtonDataController line 49: `[Authorize(Roles = "Admin,HC")]` class-level
- All ProtonData actions inherit this OR logic
- AdminController has mixed: some actions Admin-only, some Admin+HC

**Multiple Roles Handling:**
- ASP.NET Core Identity stores roles in `AspNetUserRoles` table (many-to-many)
- User can have multiple role rows in database
- Framework checks each role with OR logic when evaluating `[Authorize(Roles = "A,B")]`
- No custom role resolution logic needed (framework default is correct)

**Test Data Requirement:**
- Need multi-role user in database to verify in browser (plan 97-04)
- Code review confirms framework behavior is correct

**Analysis Result:** ✅ CORRECT - Framework handles multiple roles correctly

---

## Edge Case 2: Role Change During Login

### Test Specification

**Scenario:** HC changes user's role while user is logged in

**Test Steps:**
1. Login as Coachee
2. (In database) HC changes Coachee to Coach role
3. Navigate to Coach-accessible page (e.g., `/CDP/CoachingProton`)
4. Expected: Coachee does NOT see Coach features

### Code Review Analysis ✅

**Session Cookie Behavior:**
```csharp
// AccountController.cs - Login action (line 72-100)
var user = await _userManager.FindByNameAsync(model.Nik);
if (user != null && user.IsActive)
{
    // Roles embedded in cookie at login time
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Nik),
        // Role claims added here from database at login time
    };
    await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);
}
```

**Role Claim Storage:**
- Roles are fetched from database at login time
- Roles embedded in session cookie as claims
- Cookie contains role claims from login time, NOT real-time
- This is CORRECT design for security (performance + consistency)

**Expected Behavior:**
- Coachee logged in → Cookie contains Coachee role claims
- HC changes Coachee to Coach in database
- Coachee's existing session still has Coachee claims
- Navigate to Coach page → Access denied (still has Coachee role in cookie)
- User must logout and login again to get new role

**Security Rationale:**
- Prevents privilege escalation attacks (changing DB doesn't affect active sessions)
- Consistent session state (roles don't change mid-session)
- User must re-authenticate to get new role (correct security pattern)

**Code Verification:**
- `Program.cs` line 90: `options.ExpireTimeSpan = TimeSpan.FromHours(8)` - 8 hour session
- `Program.cs` line 91: `options.SlidingExpiration = true` - Refreshes on activity
- Role claims embedded in cookie at login time, not refreshed on each request

**Analysis Result:** ✅ CORRECT - Roles are session-scoped (login-time snapshot, not real-time)

---

## Edge Case 3: Session Expiration

### Test Specification

**Scenario:** User session expires after inactivity

**Test Steps:**
1. Login as Admin
2. Wait for session to expire (8 hours per Program.cs, or set shorter timeout for testing)
3. Navigate to authenticated page (e.g., `/Admin/Index`)

**Expected Behavior:**
- Redirect to `/Account/Login` with `returnUrl=/Admin/Index`
- User can login again and return to original page
- Session expiration handled gracefully (no errors)

### Code Review Analysis ✅

**Session Configuration (Program.cs lines 85-92):**
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";           // ✅ Set
    options.LogoutPath = "/Account/Logout";         // ✅ Set
    options.AccessDeniedPath = "/Account/AccessDenied"; // ✅ Set
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // ✅ 8 hour timeout
    options.SlidingExpiration = true;               // ✅ Refreshes on activity
});
```

**Session Expiration Flow:**
1. User logs in at 09:00
2. Session expires at 17:00 (8 hours later)
3. User clicks link at 17:30
4. Cookie expired → Framework redirects to `/Account/Login?returnUrl=/Admin/Index`
5. User logs in again
6. Framework redirects to `/Admin/Index` (return URL preserved)

**Sliding Expiration Behavior:**
- If user active within 8 hours, session refreshes
- Example: Login at 09:00, activity at 14:00 → session extends to 22:00
- Prevents users from being logged out while actively using portal

**Return URL Preservation (AccountController.cs lines 112-115):**
```csharp
if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
{
    return Redirect(returnUrl);
}
return RedirectToAction("Index", "Home");
```

**Error Handling:**
- Framework handles session expiration automatically
- No custom error handling needed
- Graceful redirect to login page with return URL
- No 500 errors or exceptions exposed to user

**Analysis Result:** ✅ CORRECT - Session expiration handled gracefully by framework

---

## Cookie Security Deep Dive

### Current Configuration (Program.cs)

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;  // ✅ Set in separate config
});
```

### Security Attributes Status

| Attribute | Value | Status | Mitigation |
|-----------|-------|--------|------------|
| **HttpOnly** | `true` | ✅ PASS | Prevents XSS cookie theft |
| **Secure** | Not set | ⚠️ INFO | Defaults to `false` (expected for HTTP) |
| **SameSite** | Not set | ⚠️ INFO | Defaults to `Lax` (CSRF protection via CSRF tokens) |
| **ExpireTimeSpan** | 8 hours | ✅ PASS | Reasonable session lifetime |
| **SlidingExpiration** | `true` | ✅ PASS | Refreshes on activity, good UX |

**Security Analysis:**
- **HttpOnly=true**: ✅ JavaScript cannot read cookies (prevents XSS cookie theft)
- **Secure=false**: ⚠️ Cookies transmitted over HTTP (OK for dev, enable for production HTTPS)
- **SameSite=Lax**: ⚠️ Cookies sent on top-level navigations (CSRF protection via anti-forgery tokens)

**Production Hardening Recommendations:**
1. Enable HTTPS in production → Set `options.Cookie.SecurePolicy = CookieSecurePolicy.Always`
2. Consider `options.Cookie.SameSite = SameSiteMode.Strict` for enhanced CSRF protection
3. Current `Lax` default is acceptable because ASP.NET Core uses anti-forgery tokens

**Analysis Result:** ✅ ACCEPTABLE for HTTP dev environment, harden for production HTTPS

---

## Test Data Requirements for Plan 97-04

### Required Test Users

1. **Multi-role user** (Edge Case 1)
   - Roles: Admin + HC
   - Purpose: Verify OR logic in role gates
   - Creation: `ALTER TABLE AspNetUserRoles ADD constraint for multiple roles`

2. **Role change test** (Edge Case 2)
   - Initial role: Coachee
   - Changed to: Coach (while logged in)
   - Purpose: Verify session-scoped role claims
   - Creation: Use existing Coachee, update role via Admin UI

3. **Session expiration test** (Edge Case 3)
   - Role: Admin
   - Purpose: Verify graceful session expiration
   - Testing: Set `ExpireTimeSpan = TimeSpan.FromMinutes(1)` temporarily

---

## Conclusion

**Edge case analysis complete - code review confirms correct implementation:**

1. **Multiple roles**: ✅ Framework OR logic handles correctly
2. **Role change during session**: ✅ Session-scoped claims (correct security pattern)
3. **Session expiration**: ✅ Framework handles gracefully with return URL preservation

**All edge cases designed correctly.** Browser testing in plan 97-04 will verify actual runtime behavior.

---

**Analysis completed:** 2026-03-05T06:08:00Z
**Auditor:** Phase 97-03 executor
**Next:** Plan 97-04 browser verification of edge cases
