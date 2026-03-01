---
phase: 71-ldap-auth-service-foundation
plan: 01
subsystem: auth
tags: [ldap, identity, asp-net-core, ef-migrations, system-directory-services]

# Dependency graph
requires:
  - phase: 69-manageworkers-migration-to-admin
    provides: ApplicationUser model, SignInManager DI registration
provides:
  - IAuthService interface with Task<AuthResult> AuthenticateAsync
  - AuthResult DTO (Success, UserId, Email, FullName, ErrorMessage)
  - AuthenticationConfig POCO (UseActiveDirectory, LdapPath, LdapTimeout, AttributeMapping)
  - LocalAuthService implementing IAuthService via CheckPasswordSignInAsync
  - ApplicationUser.AuthSource property [MaxLength(10)] default "Local"
  - EF migration AddAuthSourceToApplicationUser (Users table)
  - System.DirectoryServices v10.0.0 NuGet package
  - appsettings Authentication section with LDAP config
affects: [71-02-ldap-auth-service, 72-login-controller-integration]

# Tech tracking
tech-stack:
  added: [System.DirectoryServices v10.0.0]
  patterns: [IAuthService/AuthResult abstraction for transparent Local/AD auth, CheckPasswordSignInAsync for password-only validation (controller manages session cookie separately)]

key-files:
  created:
    - Services/IAuthService.cs
    - Services/AuthResult.cs
    - Services/AuthenticationConfig.cs
    - Services/LocalAuthService.cs
    - Migrations/20260228071551_AddAuthSourceToApplicationUser.cs
    - Migrations/20260228071551_AddAuthSourceToApplicationUser.Designer.cs
  modified:
    - HcPortal.csproj
    - Models/ApplicationUser.cs
    - appsettings.json
    - appsettings.Development.json
    - Migrations/ApplicationDbContextModelSnapshot.cs

key-decisions:
  - "LocalAuthService uses CheckPasswordSignInAsync not PasswordSignInAsync — controller retains full control over session cookie creation (Phase 72 calls SignInAsync separately)"
  - "Migration defaultValue corrected from '' to 'Local' — EF generates empty string when default is set via C# property initializer only; manual SQL update applied to 11 existing users"
  - "Table name is 'Users' (custom via ToTable in DbContext), not 'AspNetUsers' — migration reflects this correctly"
  - "IAuthService DI registration intentionally omitted — Plan 02 responsibility (requires LdapAuthService and conditional registration logic)"

patterns-established:
  - "Auth service abstraction: IAuthService hides Local vs AD implementation from controller"
  - "AuthResult DTO: success payload + UserId for Phase 72 SignInAsync + user-safe ErrorMessage"
  - "Authentication config: UseActiveDirectory toggle + env var override pattern (Authentication__UseActiveDirectory)"

requirements-completed: [AUTH-01, AUTH-03, AUTH-08, USTR-01]

# Metrics
duration: 4min
completed: 2026-02-28
---

# Phase 71 Plan 01: LDAP Auth Service Foundation Summary

**IAuthService interface + LocalAuthService + AuthResult DTO + AuthenticationConfig POCO + ApplicationUser.AuthSource field with EF migration and System.DirectoryServices NuGet package**

## Performance

- **Duration:** 4 min
- **Started:** 2026-02-28T07:14:35Z
- **Completed:** 2026-02-28T07:18:44Z
- **Tasks:** 3
- **Files modified:** 10

## Accomplishments
- Auth service abstraction layer established: IAuthService interface + AuthResult DTO consumed by Phase 72 login controller
- LocalAuthService wraps ASP.NET Core Identity CheckPasswordSignInAsync — separates password validation from session cookie creation
- AuthenticationConfig POCO enables UseActiveDirectory toggle + environment variable override for prod/dev switching
- ApplicationUser extended with AuthSource field (EF migration applied, all 11 existing users set to "Local")
- System.DirectoryServices v10.0.0 added for Plan 02 LdapAuthService

## Task Commits

Each task was committed atomically:

1. **Task 1: Add NuGet package, AuthSource field, and EF migration** - `79b1921` (feat)
2. **Task 2: Create service contracts and config POCO** - `24f75d0` (feat)
3. **Task 3: Implement LocalAuthService and update appsettings** - `5ea9a5d` (feat)

## Files Created/Modified
- `Services/IAuthService.cs` - Auth interface with Task<AuthResult> AuthenticateAsync
- `Services/AuthResult.cs` - Result DTO: Success, UserId, Email, FullName, ErrorMessage
- `Services/AuthenticationConfig.cs` - Config POCO: UseActiveDirectory, LdapPath, LdapTimeout, LdapAttributeMapping
- `Services/LocalAuthService.cs` - Identity-based implementation of IAuthService
- `Models/ApplicationUser.cs` - Added AuthSource [MaxLength(10)] property default "Local"
- `HcPortal.csproj` - Added System.DirectoryServices v10.0.0 PackageReference
- `appsettings.json` - Added Authentication section with UseActiveDirectory=false + full LDAP config
- `appsettings.Development.json` - Added Authentication:UseActiveDirectory=false override
- `Migrations/20260228071551_AddAuthSourceToApplicationUser.cs` - EF migration adding AuthSource nvarchar(10) to Users
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated model snapshot

## Decisions Made
- LocalAuthService uses CheckPasswordSignInAsync instead of PasswordSignInAsync: controller retains full control over session cookie creation (Phase 72 AccountController calls SignInAsync after IAuthService returns success)
- IAuthService not registered in Program.cs — Plan 02 handles DI wiring with conditional LocalAuthService vs LdapAuthService registration
- Authentication section added to both appsettings.json (full config) and appsettings.Development.json (UseActiveDirectory=false override only)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] EF migration generated with defaultValue: "" instead of "Local"**
- **Found during:** Task 1 (Generate EF migration)
- **Issue:** EF Core generates `defaultValue: ""` when the model default is set via C# property initializer (`= "Local"`). The migration was applied to the DB with an empty string default constraint.
- **Fix:** Updated migration file to `defaultValue: "Local"`. Ran SQL to update all 11 existing users from '' to 'Local'. Dropped empty-string DB constraint and added proper 'Local' default constraint.
- **Files modified:** Migrations/20260228071551_AddAuthSourceToApplicationUser.cs
- **Verification:** `SELECT COUNT(*) FROM [Users] WHERE AuthSource='Local'` returned 11 (all users)
- **Committed in:** 79b1921 (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Essential correctness fix — existing users would have had empty AuthSource instead of "Local", breaking Phase 72 auth routing logic. No scope creep.

## Issues Encountered
- Running app (process 2912) locked the Debug exe — used `--configuration Release` for all dotnet ef commands as the plan specified as fallback.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Plan 02 (LdapAuthService) can now implement IAuthService and register both services in Program.cs with conditional DI based on AuthenticationConfig.UseActiveDirectory
- Phase 72 (login controller) can inject IAuthService and use AuthResult.UserId for SignInAsync
- AuthSource field on ApplicationUser allows Plan 02 to route Local users through LocalAuthService even when AD mode is active

## Self-Check: PASSED

All created files verified present on disk. All task commits verified in git log:
- 79b1921: feat(71-01): add NuGet package, AuthSource field, and EF migration
- 24f75d0: feat(71-01): create service contracts and config POCO
- 5ea9a5d: feat(71-01): implement LocalAuthService and update appsettings
- cfb657e: docs(71-01): complete LDAP auth service foundation plan

---
*Phase: 71-ldap-auth-service-foundation*
*Completed: 2026-02-28*
