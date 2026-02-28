# Phase 71: LDAP Auth Service Foundation - Context

**Gathered:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Build the dual authentication infrastructure — LDAP service, local service wrapper, config toggle, AuthSource field. The login flow is NOT changed in this phase (that's Phase 72). This phase delivers the service layer and data model changes only.

</domain>

<decisions>
## Implementation Decisions

### LDAP Connection
- LDAP path: `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com` (from requirements)
- Search filter: `samaccountname` (from requirements)
- Search scope: OU=KPB only — all app users are in this OU
- LDAP bind method: Claude's discretion (user credentials or service account)
- Connection timeout: 5 seconds
- NuGet package: System.DirectoryServices

### AD Attribute Mapping (configurable in appsettings.json)
- Sync only 2 fields from AD on each login: **FullName** and **Email**
- Default mapping: FullName ← `displayName`, Email ← `mail`
- NIP ← `employeeID` (configurable, could be `employeeNumber` — verify with IT)
- Position, Section, Unit, NIP: NOT synced from AD — these come from HC import
- Role and SelectedView: NEVER synced from AD (per AUTH-07)
- Null handling: skip null AD values — do not overwrite existing DB data with null
- Email sync: always update from AD (Email = source of truth from AD)

### Auth Failure Behavior
- LDAP server unreachable: show generic error "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti." — no technical details exposed
- Wrong credentials: same generic error as local login failure — "Username atau password salah"
- No fallback to local auth when LDAP fails — AD users cannot login if LDAP is down
- No rate limiting — intranet app behind corporate firewall
- ILogger logging: log all LDAP connection attempts, success/failure for debugging

### User Provisioning & Login Flow (decisions for Phase 72, captured here)
- **No auto-provisioning** — AUTH-06 changed: user MUST be pre-registered by HC via ManageWorkers before they can login
- Login input: **Email + Password** (same for AD and local mode) — AUTH-05 changed: no NIP/Username field
- Matching key: **Email** — app searches DB by email to find user
- AuthSource routing: cek AuthSource field in DB → "AD" routes to LDAP, "Local" routes to PasswordSignInAsync
- AD sync scope reduced: only FullName + Email (AUTH-07 changed from FullName/NIP/Position/Section)
- User not in DB → reject: "Akun Anda belum terdaftar. Hubungi HC."

### Dual Mode (AD mode aktif)
- 1 local Admin account (AuthSource="Local") — always login with local password, emergency/fallback access
- All other users: AuthSource="AD" — login via LDAP
- Local users can still login with email+password even when AD mode is active

### Config Management
- Development (local machine): appsettings.Development.json with UseActiveDirectory=false
- Dev server & Production: environment variables override with UseActiveDirectory=true
- Config structure in appsettings.json: Claude's discretion — section "Authentication" with toggle, LDAP path, timeout, attribute mapping
- Deployment config (IIS vs Docker, env var setup): Claude's discretion — support both appsettings + environment variables (ASP.NET Core default behavior)

### HC Import Flow Adaptation (Phase 72 scope, captured here)
- AuthSource set automatically based on config toggle — no AuthSource column in Excel template
- ManageWorkers create form: adaptif — password field optional/hidden when AD mode active
- HC still inputs password for AD users (password exists as backup in Identity framework)
- Import template stays the same — no new columns needed

### Edge Cases
- User removed/disabled in AD but exists in DB → LDAP rejects auth → login fails naturally. HC cleans up manually.
- User email changes in AD → email in DB doesn't match → login fails. HC must update email in ManageWorkers.
- Local→AD transition: HC updates AuthSource from "Local" to "AD" for migrating users

### Claude's Discretion
- LDAP bind method (user credentials vs service account)
- Config structure details (exact JSON shape)
- Deployment configuration approach
- Error handling implementation details
- IAuthService method signatures beyond AuthenticateAsync
- AD attribute extraction implementation

</decisions>

<specifics>
## Specific Ideas

- Login page looks identical in AD mode and local mode — no visual difference, just Email + Password
- AD is purely for password verification + FullName/Email sync — all other user data managed locally by HC
- User explained: "AD ini fungsinya untuk connect server" — AD role is authentication gateway, not data source
- Configurable attribute mapping in appsettings.json — can be adjusted after verifying with IT without code changes

</specifics>

<deferred>
## Deferred Ideas

- HC notification when new AD user is provisioned — needs notification system (doesn't exist yet), defer to future phase
- Role mapping otomatis dari AD attributes — defer, HC pre-registers users with correct roles instead
- Mock LdapAuthService for testing — not needed, will test on dev server with real AD connection
- Unit/Directorate sync from AD — removed from scope, these fields managed locally

## Requirement Changes to Apply (before Phase 72 planning)

These requirements need updating in REQUIREMENTS.md based on discussion decisions:
- **AUTH-05**: Change from "Username + NIP placeholder (AD mode)" to "Email + Password (same for both modes)"
- **AUTH-06**: Change from "auto-provisioned Coachee" to "rejected if not pre-registered by HC"
- **AUTH-07**: Change from "sync FullName/NIP/Position/Section" to "sync FullName/Email only"

</deferred>

---

*Phase: 71-ldap-auth-service-foundation*
*Context gathered: 2026-02-28*
