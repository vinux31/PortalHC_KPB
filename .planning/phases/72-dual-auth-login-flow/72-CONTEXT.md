# Phase 72: Dual Auth Login Flow - Context

**Gathered:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Rewire AccountController login to use IAuthService abstraction. AD mode: all users authenticate via LDAP (global config toggle). Profile sync (FullName/Email) on AD login. ManageWorkers form and import template adapt to AD mode. Remove AuthSource field (no longer needed — routing is global, not per-user). Login flow belum ada di Phase 71 — Phase 72 implements the actual login integration.

</domain>

<decisions>
## Implementation Decisions

### Auth Routing (MAJOR CHANGE from Phase 71)
- **Global config routing** — UseActiveDirectory=true → ALL users authenticate via LDAP. No per-user routing.
- AuthSource field **REMOVED** — no longer needed. Global toggle is the only routing mechanism.
- Phase 72 includes EF migration to DROP AuthSource column from ApplicationUser
- No emergency local admin access — if LDAP is down, nobody can login. This is acceptable per user decision.
- No fallback from LDAP to local auth

### Login Flow (AD mode active)
- Flow: Email+Password → IAuthService.AuthenticateAsync (LdapAuthService) → Find user by email in DB → Sync profile → SignInAsync → Redirect
- If user not in DB → reject: "Akun Anda belum terdaftar. Hubungi HC."
- No auto-provisioning — user MUST be pre-registered by HC via ManageWorkers
- Redirect behavior: same as current (no changes)
- Remember Me checkbox: retained, works same as now

### Login Flow (Local mode — UseActiveDirectory=false)
- Flow: Email+Password → IAuthService.AuthenticateAsync (LocalAuthService) → SignInAsync → Redirect
- Zero visual changes to login page — identical to current behavior
- No profile sync in local mode
- ManageWorkers: no changes, password field visible and required
- Import template: unchanged, Password column present

### Login Page Visual
- Login page identical for both modes — Email + Password fields
- AD mode: small grey hint text below the login button: "Login menggunakan akun Pertamina"
- Local mode: no hint, page is exactly as it is now
- Error display: same pattern as current login page (no styling changes)

### Error Messages (Bahasa Indonesia)
- Wrong password: "Username atau password salah"
- User not in DB: "Akun Anda belum terdaftar. Hubungi HC."
- LDAP server down: "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti."
- Display method: same as current login page error pattern

### Profile Sync (AD mode only)
- Sync FullName (from AD displayName) and Email (from AD mail) — only these 2 fields
- Sync happens BEFORE session creation (data from AuthResult, no extra AD call)
- Flow: Auth → Find user → Update FullName/Email in DB → SignInAsync → Redirect
- Null handling: skip null AD values, log warning, do not overwrite existing DB data
- Sync failure: login continues anyway (auth succeeded = user allowed in). Retry on next login.
- No detailed sync logging — sync happens silently
- Local mode: no sync at all

### ManageWorkers Adaptation (AD mode active)
- Create form: password field HIDDEN, system auto-generates random password in backend
- Edit form: password field HIDDEN — user changes password via Pertamina portal, not this app
- FullName and Email fields: READ-ONLY for AD users (synced from AD, HC cannot override)
- No AuthSource column in list view (field being removed entirely)
- Local mode: ManageWorkers unchanged — password field visible, all fields editable

### Import Template (AD mode active)
- 1 dynamic template — download endpoint checks UseActiveDirectory config
- AD mode: Excel template WITHOUT Password column. System auto-generates random passwords during import.
- Local mode: Excel template WITH Password column (same as current)
- No AuthSource column in any template (field being removed)
- Import logic: if AD mode, generate random password for each user; if local mode, use password from Excel

### Session/Cookie
- No changes — 8 hour expiration, sliding expiration, 30 minute session idle timeout
- Same configuration for both AD and local mode

### Requirement Updates (to apply before planning)
- **AUTH-05**: Email + Password (same for both modes) — NOT Username + NIP placeholder
- **AUTH-06**: Rejected if not pre-registered by HC — NOT auto-provisioned Coachee
- **AUTH-07**: Sync FullName/Email only — NOT FullName/NIP/Position/Section
- **Phase 71 criteria #2**: AuthSource field removed in Phase 72 (was created in Phase 71, no longer needed)
- **ROADMAP Phase 72 success criteria**: needs full rewrite to match these decisions

### Claude's Discretion
- Random password generation method (Guid, crypto random, etc.)
- Exact implementation of dynamic import template
- How to handle the AuthSource migration (simple DROP or soft deprecation)
- Profile sync error handling implementation details
- Login page hint positioning and exact CSS styling

</decisions>

<specifics>
## Specific Ideas

- User said: "AD ini fungsinya untuk connect server" — AD is auth gateway only, not data source
- User said: password management for AD users is via Pertamina corporate portal, not this app
- Login page should look identical — user should not notice which auth mode is active
- "case ini hampir tidak mungkin terjadi" regarding email changes in AD — HC handles manually if needed
- All auth-related changes (login, ManageWorkers, import) belong in Phase 72 — not split across phases

</specifics>

<deferred>
## Deferred Ideas

- HC notification when AD user can't login (email mismatch etc.) — needs notification system
- Bulk AuthSource migration tool — not needed since field is being removed
- AD attribute mapping UI — configure via appsettings.json, no UI needed
- Re-validate AD session on every request — standard session cookie is sufficient for intranet app
- "Change password" feature for AD users in this app — not applicable, Pertamina portal handles this

## Requirement Changes to Apply (before Phase 72 planning)

These requirements need updating in REQUIREMENTS.md and ROADMAP.md:
- **AUTH-05**: Change from "Username + NIP placeholder (AD mode)" to "Email + Password (same for both modes)"
- **AUTH-06**: Change from "auto-provisioned Coachee" to "rejected if not pre-registered by HC"
- **AUTH-07**: Change from "sync FullName/NIP/Position/Section" to "sync FullName/Email only"
- **Phase 71 success criteria #2**: Note that AuthSource field will be removed in Phase 72
- **Phase 72 success criteria**: Rewrite entirely based on discussion decisions above

</deferred>

---

*Phase: 72-dual-auth-login-flow*
*Context gathered: 2026-02-28*
