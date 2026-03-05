# Phase 97: Authentication & Authorization Audit - Context

**Gathered:** 2026-03-05
**Status:** Planning Complete - Ready for Execution

<domain>
## Phase Boundary

Audit authentication and authorization untuk bugs. Login flow (local & AD), AccessDenied page, role-based navigation, return URL security, cookie settings, dan authorization gates.

Requirements: AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05

Controllers in scope: AccountController (Login, Logout, AccessDenied), _Layout.cshtml navigation, Program.cs auth configuration.

</domain>

<decisions>
## Implementation Decisions

### AD Mode Testing Strategy
- **Code review only** — Phase 87 sudah verifikasi AD path via code review
- AD uses same IAuthService interface — logic setelah authenticate identik dengan local path
- LdapAuthService sudah punya proper error handling, timeout, LDAP injection prevention
- Tidak perlu test AD login langsung (butuh koneksi AD yang mungkin tidak available di development)

### Authorization Audit Scope
- **Exhaustive grep audit** — Grep semua `[Authorize]` dan `User.IsInRole()` di seluruh codebase
- Buat authorization matrix: actions → roles → gate type (attribute vs manual check)
- Verify consistency: Admin/HC-only actions properly gated, public actions correctly open
- Document gaps: actions tanpa proper role gates, manual checks yang bisa diganti attributes

### Return URL Security Testing
- **Code review only** — Verifikasi `Url.IsLocalUrl(returnUrl)` ada di AccountController line 112
- ASP.NET Core Url.IsLocalUrl sudah robust terhadap open redirect attacks
- Cukup verify implementation exists — tidak perlu test actual attack vectors

### Session & Auth Edge Cases
- **Include edge cases** — Test scenarios:
  - Multiple roles: User dengan lebih dari 1 role — verify role resolution
  - Role change saat login: HC mengubah role user, user harus re-login untuk dapat role baru
  - Session expiration: What happens saat session expire mid-action
  - Cookie security: Verify httpOnly, secure, sameSite settings

### Test Data Strategy
- **Use existing users** — Database sudah punya users di berbagai roles
- Cukup 1 user per role untuk testing: Admin, HC, SrSpv, SectionHead, Coach, Coachee
- Tidak perlu buat seed data action (berbeda dengan Phase 87/90/95)

### Browser Verification Approach
- **Code review + spot checks** — Audit code thoroughly, browser test hanya critical flows
- Code review: Grep audit, trace authorization logic, verify security settings
- Spot checks: Manual test login (local mode), access restricted pages, verify navigation
- Faster than manual testing semua flows — focus di high-risk areas

### Security Bug Handling
- **Fix immediately** — Auth/authorization bugs are critical security issues
- Fix inline tanpa diskusi tambahan, tapi commit dengan clear security-related message
- User verify fixes via browser testing setelah commit

### Cookie Security Verification
- **Basic check** — Verify minimum security settings di Program.cs ConfigureCookie:
  - httpOnly: true (prevent XSS cookie theft)
  - secure: true (HTTPS only, if SSL enabled)
  - sameSite: Strict or Lax (prevent CSRF)
- Skip advanced settings (lifetime, sliding expiration, cookie name, domain, path)

### Bug Fix Approach (sama dengan Phase 83-85)
- Code review dulu → fix bugs → commit → user verify di browser
- Fix bugs apapun ukurannya (security bugs tidak ada size limit)
- Silent bugs (tidak visible ke user): Fix jika mudah (<20 baris), otherwise log dan skip

### Claude's Discretion
- Authorization matrix format untuk hasil exhaustive audit
- Skenario spot check yang cukup untuk "critical flows"
- Berapa banyak edge cases yang cukup untuk multiple roles/session testing

</decisions>

<specifics>
## Specific Ideas

- Exhaustive grep audit: Grep pattern `[Authorize(` dan `User.IsInRole(` untuk build matrix
- Phase 87 sudah verified login flow (local + inactive block), navigation visibility, AccessDenied page — focus di gaps yang belum di-audit
- Cookie security check: Verify ConfigureCookie options di Program.cs line 70-100 (approximate)
- Role resolution: ASP.NET Core Identity allows multiple roles per user — verify app handles this correctly

</specifics>

<code_context>
## Existing Code Insights

### Key Controllers
- `AccountController.cs`: Login action (line 42-118) dengan IAuthService, inactive block (line 72-76), returnUrl redirect (line 112-115), AccessDenied (line 269-272)
- `IAuthService.cs`, `LocalAuthService.cs`, `LdapAuthService.cs`: Authentication abstraction layer
- `Program.cs`: Auth configuration, cookie options, AccessDeniedPath (line 89)

### Key Views
- `Views/Account/Login.cshtml`: Login form dengan email/password inputs
- `Views/Account/AccessDenied.cshtml`: Indonesian error page (Phase 73)
- `Views/Shared/_Layout.cshtml`: Navigation dengan Kelola Data gated (line 64-71)

### Established Patterns
- Role gating: `[Authorize(Roles = "Admin, HC")]` di controller actions
- Manual checks: `User.IsInRole("RoleName")` di views untuk conditional rendering
- Inactive user block: `IsActive` check di AccountController.Login line 72-76
- Return URL security: `Url.IsLocalUrl(returnUrl)` di AccountController line 112
- AD vs Local: DI factory di Program.cs memilih IAuthService implementation berdasarkan config

### Integration Points
- Login flow: AccountController → IAuthService → SignInManager.SignInAsync
- Authorization: ASP.NET Core Authorization filters automatically redirect ke AccessDeniedPath
- Session management: ASP.NET Core Identity cookies di Program.cs

### Known Security Features (from code review)
- LDAP injection prevention: EscapeLdapFilterValue di LdapAuthService (line 163-185)
- Generic error messages: Technical details never reach UI
- Timeout protection: 5-second timeout untuk LDAP bind
- Open redirect prevention: Url.IsLocalUrl check untuk returnUrl

</code_context>

<deferred>
## Deferred Ideas

- Automated security testing (OWASP ZAP, Burp Suite) — future phase
- AD integration testing dengan test LDAP server — future phase
- Session management optimization (sliding expiration, persistent cookies) — future phase
- Multi-factor authentication — out of scope untuk bug hunting

</deferred>

---

*Phase: 97-authentication-authorization-audit*
*Context gathered: 2026-03-05*
