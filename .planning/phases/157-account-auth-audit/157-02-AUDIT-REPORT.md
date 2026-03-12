# 157-02 Audit Report: Authorization Enforcement (AUTH-04)

**Date:** 2026-03-12
**Requirement:** AUTH-04 — Role-restricted URLs redirect to AccessDenied, not 500 errors

---

## 1. Program.cs Auth Configuration

**Status: PASS**

```csharp
// Cookie auth configured at line 88–95:
options.LoginPath = "/Account/Login";
options.LogoutPath = "/Account/Logout";
options.AccessDeniedPath = "/Account/AccessDenied";
options.ExpireTimeSpan = TimeSpan.FromHours(8);
options.SlidingExpiration = true;
```

Middleware order (lines 149–155):
- `app.UseRouting()` — correct
- `app.UseSession()` — correct
- `app.UseAuthentication()` — correct, before Authorization
- `app.UseAuthorization()` — correct

**Finding:** Auth setup is correct. ASP.NET Identity's `ConfigureApplicationCookie` sets both `LoginPath` and `AccessDeniedPath` so unauthorized access redirects properly.

---

## 2. Controller Authorization Matrix

| Controller | Class-level | Notes |
|---|---|---|
| AccountController | `[Authorize]` | Login, AccessDenied have `[AllowAnonymous]` |
| HomeController | `[Authorize]` | All actions protected |
| CMPController | `[Authorize]` | All actions protected |
| CDPController | `[Authorize]` | CoachDashboard + SectionHeadDashboard have additional role restrictions |
| AdminController | `[Authorize]` | All public actions additionally have `[Authorize(Roles = "Admin, HC")]` |
| ProtonDataController | `[Authorize(Roles = "Admin,HC")]` | Class-level role restriction |
| NotificationController | `[Authorize]` | All actions protected |

**Status: PASS — No unprotected public-facing actions found.**

### CDPController Elevated Role Actions
- `CoachDashboard` (line 586): `[Authorize(Roles = "Admin, HC")]`
- `SectionHeadDashboard` (line 2106): `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]`
- `HCDashboard` (line 2194): `[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]`

### AdminController
- Class-level `[Authorize]` (any authenticated user)
- Index and ALL public actions additionally restricted with `[Authorize(Roles = "Admin, HC")]`
- No Admin action is accessible by a plain Worker role

---

## 3. AccessDenied Page

**Status: PASS**

`AccountController.AccessDenied` (line 282):
```csharp
[AllowAnonymous]
public IActionResult AccessDenied()
{
    return View();
}
```

`Views/Account/AccessDenied.cshtml`:
- Renders a user-friendly message in Bahasa Indonesia
- "Anda tidak memiliki izin untuk mengakses halaman ini."
- No ViewBag dependencies — view is self-contained, zero risk of null reference
- Has a "Kembali" (back) button

---

## 4. Edge Case Analysis

### Unauthenticated user hits protected URL
- Redirect chain: `[Authorize]` → 401 → Cookie auth middleware → `LoginPath` → `/Account/Login`
- **Result: PASS — redirects to Login**

### Authenticated Worker hits /Admin/Index
- Worker has no "Admin" or "HC" role
- `[Authorize(Roles = "Admin, HC")]` on `AdminController.Index` → 403
- Cookie auth middleware → `AccessDeniedPath` → `/Account/AccessDenied`
- **Result: PASS — redirects to AccessDenied**

### Authenticated Worker hits /ProtonData/Silabus
- ProtonDataController class-level `[Authorize(Roles = "Admin,HC")]` → 403
- Cookie auth middleware → `/Account/AccessDenied`
- **Result: PASS — redirects to AccessDenied**

---

## 5. Summary

| Check | Result |
|---|---|
| Program.cs AccessDeniedPath configured | PASS |
| Program.cs LoginPath configured | PASS |
| UseAuthentication before UseAuthorization | PASS |
| AccountController.AccessDenied has [AllowAnonymous] | PASS |
| AccessDenied.cshtml renders without exceptions | PASS |
| AdminController all actions role-restricted | PASS |
| ProtonDataController class-level role restriction | PASS |
| All other controllers class-level [Authorize] | PASS |
| No publicly accessible action missing protection | PASS |

**AUTH-04: PASS — All role-restricted URLs will redirect to AccessDenied. Unauthenticated access redirects to Login. No 500 errors expected.**

---

## 6. Bugs Found and Fixed

None. Authorization configuration is correct as implemented.
