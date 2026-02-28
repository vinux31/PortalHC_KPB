# Phase 74: Hybrid Auth & Role Restructuring - Research

**Researched:** 2026-02-28
**Domain:** ASP.NET Core Identity, LDAP authentication, role-based access control
**Confidence:** HIGH

## Summary

Phase 74 implements hybrid authentication (AD with local fallback for admin@pertamina.com) and restructures the role hierarchy to add Supervisor role and demote SectionHead from level 4→3. The current codebase has Phase 71-73 infrastructure (IAuthService abstraction, LdapAuthService, LocalAuthService) ready; this phase adds per-email fallback logic and role fixes.

**Key implementation patterns established by Phase 71-73:**
- IAuthService abstraction in Program.cs DI (reads Authentication:UseActiveDirectory config at startup)
- LdapAuthService performs credential bind + attribute fetch (null-safe FullName/Email sync)
- LocalAuthService wraps Identity.SignInManager.CheckPasswordSignInAsync
- UserRoles.cs holds all role constants and GetDefaultView() for SelectedView mapping
- ApplicationUser has RoleLevel field (1-6, hardcoded in each role definition)
- _Layout.cshtml displays role via GetRolesAsync().FirstOrDefault() (unpredictable under strict one-role policy)

**Primary recommendation:** Phase 74 adds hybrid fallback in AuthenticateAsync dispatcher (check email==admin@pertamina.com, try AD first then LocalAuthService fallback), updates UserRoles with Supervisor role (level 5), fixes role checks in CDPController (change RoleLevel comparisons to explicit role name checks for Coach/EligibleCoaches), updates _Layout.cshtml to use user.RoleLevel + UserRoles.GetDefaultView(), and adds EF migration for SectionHead RoleLevel 4→3 update.

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Hybrid Auth Fallback:**
- Fallback scope: Admin KPB only (admin@pertamina.com)
- Identification: By email — hardcode admin@pertamina.com as the local fallback account
- Login UX: Same form for everyone. System detects admin@pertamina.com and tries AD first, then local auth if AD fails
- AD priority: Always try AD first for admin@pertamina.com, then fallback to local if AD fails
- Fallback UX: Completely silent — no indication of which auth path was used
- Error handling (AD down, regular user): Generic "Login gagal. Silakan coba lagi nanti." — no AD indication
- Error handling (admin wrong password): Same generic error as regular users — no distinction
- Password storage: ASP.NET Identity PasswordHash field. Standard UserManager handles hashing/verification

**Strict One-Role-Per-User Policy:**
- Every user has exactly ONE role
- Form validation: Single-select dropdown (not checkbox)
- Import validation: If user exists → skip + show report with skipped user list
- Current data: Already clean — 10 users all have single role

**Role Display Fix:**
- Problem: `_Layout.cshtml` line 7 uses `GetRolesAsync().FirstOrDefault()` which is unpredictable
- Fix: Replace with approach that respects strict one-role policy (explicit, safe code)
- Source: Claude's discretion — pick approach fitting current architecture

**Role Hierarchy Restructuring:**

| Level | Role | SelectedView | Data Scope |
|-------|------|-------------|------------|
| 1 | Admin | "Admin" | Full (all sections) |
| 2 | HC | "HC" | Full (all sections) |
| 3 | Direktur, VP, Manager, **Section Head** | "Atasan" | Full (all sections) |
| 4 | Sr Supervisor | "Atasan" | Section only |
| 5 | **Supervisor** (NEW), Coach | "Coach" | Unit only |
| 6 | Coachee | "Coachee" | Self only |

Changes from current:
- **Section Head:** level 4 → level 3 (full access like management)
- **Supervisor:** NEW role at level 5 (same access/menu as Coach but no coachee mapping)
- **Sr Supervisor:** Stays level 4 (approve/reject, no evidence upload)

**Supervisor Role Registration:**
- Add to UserRoles.AllRoles, GetRoleLevel() (return 5), GetDefaultView() (return "Coach")
- Create role in database (SeedData CreateRolesAsync)
- No seed user — real users assigned via admin

**Database Migration:**
- Update existing SectionHead users: RoleLevel 4 → 3 (EF migration script)
- Update seed data to reflect new hierarchy

**Evidence Upload Access:**
- Restricted to Coach role only — not by RoleLevel but by role name check
- Current bug: `user.RoleLevel <= 5` allows SrSupervisor to upload
- Fix: Change to role == Coach specifically

**EligibleCoaches Logic:**
- Filter by role name "Coach" only — not by RoleLevel
- Current bug: `u.RoleLevel <= 5` includes everyone
- Fix: Only users with role "Coach" in dropdown

**Proton/CDP Data Scope:**
- Level 1-3 (Admin, HC, Direktur, VP, Manager, SectionHead): Full access
- Level 4 (Sr Supervisor): Section scope
- Level 5 (Supervisor, Coach): Unit scope
- Level 6 (Coachee): Self only
- CanAccessProton: Remains `RoleLevel <= 5` (all except Coachee)

**Coaching Flow:**
- Coach: inputs coaching data, uploads evidence, has coachee mapping
- Supervisor: same view/menu as Coach, but no coachee mapping
- Sr Supervisor: approves/rejects, no evidence upload

### Claude's Discretion

- Exact approach for role display fix (claim-based vs dedicated field vs explicit single-role query)
- Migration strategy for SectionHead RoleLevel update
- How to add Supervisor role to existing code paths using role name checks
- Exact scope filtering implementation in CDPController

### Deferred Ideas (OUT OF SCOPE)

None — all discussion stayed within phase scope.

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| AUTH-HYBRID | Enable hybrid authentication (AD fallback to local) so dedicated Admin KPB user works in production AD mode | IAuthService abstraction + conditional service factory (Phase 71-72) provide foundation; Phase 74 adds per-email fallback dispatcher in AuthenticateAsync wrapper or AccountController login logic |
| [ROLE-STRUCT] | Role/access fixes: Supervisor new role (level 5), SectionHead demoted level 4→3, upload evidence Coach-only | UserRoles.cs constants + GetRoleLevel() + GetDefaultView() provide pattern; migration updates RoleLevel in Users table; CDPController uses explicit role checks |
| [ROLE-DISPLAY] | _Layout.cshtml use SelectedView (or role priority) instead of GetRolesAsync().FirstOrDefault() | Strict one-role policy + RoleLevel field + UserRoles.GetDefaultView() enable safe SelectedView direct display |

</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Identity | .NET 7+ | Built-in user/role management | Industry standard for ASP.NET, pre-configured in project |
| System.DirectoryServices | .NET Framework included | LDAP/AD connectivity | Official Microsoft library for DirectoryServices, no NuGet needed |
| EntityFramework Core | 7+ | Data persistence, migrations | Project ORM for Users table, RoleLevel field, migrations |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| IConfiguration | .NET Core built-in | Reading appsettings.json values | Conditionally select auth service (UseActiveDirectory toggle) |
| Task.WhenAny / TimeoutAsync pattern | .NET standard | Async timeout enforcement | Phase 71 established this for 5-second LDAP timeout |

## Architecture Patterns

### Pattern 1: IAuthService Abstraction (Phase 71-73 Foundation)

**What:** Single interface hiding Local vs AD implementation details. Program.cs DI factory selects which service based on config.

**When to use:** When auth needs to support multiple backends without controller changes.

**Current implementation:**
```csharp
// Services/IAuthService.cs
public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(string email, string password);
}

// Program.cs (Phase 72)
services.AddScoped<IAuthService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var useAD = config.GetValue<bool>("Authentication:UseActiveDirectory", false);
    if (useAD)
        return new LdapAuthService(config, provider.GetRequiredService<ILogger<LdapAuthService>>());
    else
        return new LocalAuthService(provider.GetRequiredService<SignInManager<ApplicationUser>>(), ...);
});
```

**Phase 74 addition:** Hybrid fallback requires modifying this pattern to try AD first, then fall back to Local for admin@pertamina.com specifically. Options:
1. Create HybridAuthService wrapper that chains AD then Local
2. Modify LdapAuthService to accept fallback delegate (higher complexity)
3. Move fallback logic to AccountController.Login POST (simpler, keeps services pure)

### Pattern 2: Role Display via SelectedView Field

**What:** ApplicationUser.SelectedView is the source of truth for role display. Avoids GetRolesAsync().FirstOrDefault() unpredictability.

**When to use:** When strict one-role-per-user policy is enforced and SelectedView is kept in sync via GetDefaultView().

**Current state:** _Layout.cshtml line 7 uses `GetRolesAsync().FirstOrDefault()` which is unreliable.

**Phase 74 fix:**
```csharp
// Proposed: Read SelectedView directly (simpler, faster, explicit)
var userRole = currentUser?.SelectedView ?? "Coachee";

// OR: Use RoleLevel + lookup (safer with strict policy)
var userRole = UserRoles.GetDefaultView(await _userManager.GetRolesAsync(currentUser)).FirstOrDefault() ?? "Coachee";
```

Phase 74 decision: Use simpler direct SelectedView read since strict one-role policy is now enforced.

### Pattern 3: Role-Level vs Role-Name Checks

**What:** Phase 74 fixes use explicit role name checks (e.g., `role == "Coach"`) instead of level comparisons (e.g., `level <= 5`).

**When to use:** For features tied to specific roles, not access hierarchy levels.

**Examples:**
- Evidence upload: Only `role == "Coach"`, not `level <= 5`
- EligibleCoaches dropdown: Only Coach role, not `level <= 5`
- Can approve (Sr Supervisor only): `role == "Sr Supervisor"`

**Current bugs in CDPController:**
- Line 810: `canUpload = user.RoleLevel <= 5` allows SrSupervisor
- Line ~400: `EligibleCoaches = users.Where(u => u.RoleLevel <= 5)` allows Supervisor

### Pattern 4: Role Registration & Level Mapping (UserRoles.cs)

**What:** Centralized role constants + GetRoleLevel() lookup table ensure consistent hierarchy.

**Current state (Phase 73):**
```csharp
public static int GetRoleLevel(string roleName)
{
    return roleName switch
    {
        Admin => 1, HC => 2,
        Direktur or VP or Manager => 3,
        SectionHead or SrSupervisor => 4,
        Coach => 5, Coachee => 6,
        _ => 6
    };
}
```

**Phase 74 changes:**
1. Add Supervisor constant (new)
2. Update GetRoleLevel: SectionHead → 3, Supervisor → 5
3. Update AllRoles list to include Supervisor
4. Update GetDefaultView: Supervisor → "Coach"

### Anti-Patterns to Avoid

- **Hardcoding role strings in controllers:** Breaks refactoring. Use UserRoles constants instead.
- **Using RoleLevel for role-specific features:** Level is hierarchy; use role name for specific behavior. Supervisor and Coach have same level but different coachee mapping.
- **Storing AuthSource per-user:** Phase 72 decision: global config (UseActiveDirectory) is routing mechanism. No per-user flag.
- **Mixing GetRolesAsync() and RoleLevel:** Inconsistent. One-role policy makes RoleLevel the source of truth; compute role from level or store explicitly.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Multi-backend authentication | Custom auth handler | IAuthService + Program.cs factory | Established pattern in Phase 71-73, avoids duplicate credential handling |
| Role-to-level mapping | String switches scattered | UserRoles.cs centralized lookup | Single source of truth, easier to maintain hierarchy changes |
| LDAP timeout handling | Busy-wait loop | Task.WhenAny + Task.Delay | Non-blocking, prevents 20-40s ADSI hang; Phase 71 verified pattern |
| Password hashing | Custom crypto | Identity.PasswordHasher | Industry standard, salted PBKDF2, no reinvention risk |
| Database migrations | Manual SQL scripts | EF Core migrations | Tracks schema history, reversible, integrated with model |

**Key insight:** Hybrid fallback and role restructuring touch auth, identity, and authorization — all well-supported by ASP.NET Core. Hand-rolling introduces credential handling bugs, timeout issues, and desynchronization between UserRoles and database. Trust the framework patterns established in Phase 71-73.

## Common Pitfalls

### Pitfall 1: Fallback Loop / Mutual Exception Handling

**What goes wrong:** If fallback logic catches all exceptions, a simple typo in email/password gets masked. E.g., LdapAuthService throws timeout exception, code assumes fallback but LocalAuthService also fails, returns generic "auth failed" without knowing why.

**Why it happens:** Exception handling is broad (`catch (Exception ex)`) and async fallback hides the original error.

**How to avoid:**
- Log each auth attempt separately (phase 71 has LogInformation/LogWarning)
- Distinguish expected failures (wrong password) from infrastructure failures (LDAP timeout)
- For hybrid: AD timeout → fallback OK. AD rejection (bad credentials) → no fallback.
- Return same error message to user in both cases (security: don't reveal auth method)

**Warning signs:** User reports "login works sometimes, fails unpredictably" or "password change doesn't work". Check logs for timeout vs credential mismatch.

### Pitfall 2: SectionHead RoleLevel Migration Not Applied at Login

**What goes wrong:** EF migration runs, updates RoleLevel in database. But in-memory objects (during request) still see old cached value from SignInManager/UserManager query. Old level persists until next login or cache clear.

**Why it happens:** UserManager caches identity claims. RoleLevel field must be explicitly reloaded or claim refreshed.

**How to avoid:**
- Ensure migration runs before deploy
- Test: migrate, login, check user.RoleLevel in controller (should be 3 for SectionHead)
- If not reflected, call `await _signInManager.RefreshSignInAsync(user)` after migration loads (admin function for testing)
- Alternatively, add post-migration verification test

**Warning signs:** Admins report SectionHead sees wrong data scope after update. Check database directly (SELECT * FROM Users WHERE Id='...' AND RoleLevel=3). If DB correct but page shows old scope, cache issue.

### Pitfall 3: Supervisor Role Added to AllRoles but Not Seeded

**What goes wrong:** Code checks `user.IsInRole("Supervisor")` but role doesn't exist in AspNetRoles table. Authorization always fails. Identity silently returns false for non-existent role.

**Why it happens:** UserRoles.AllRoles includes Supervisor, but CreateRolesAsync seed skips it (or migration doesn't create it).

**How to avoid:**
- Add Supervisor to both AllRoles list AND CreateRolesAsync loop
- After deploy, run SeedData manually or add migration that creates role: `roleManager.CreateAsync(new IdentityRole("Supervisor"))`
- Test: SELECT * FROM AspNetRoles; should show 9 roles including Supervisor
- Test: User with Supervisor role can login and GetRolesAsync includes "Supervisor"

**Warning signs:** Supervisor user added to database (RoleLevel 5) but pages show no special behavior. Check AspNetRoles table — if missing, that's the issue.

### Pitfall 4: Hybrid Fallback Email Check Case-Sensitive

**What goes wrong:** Admin login with "Admin@pertamina.com" (capital A) vs hardcoded "admin@pertamina.com" (lowercase). Email comparison fails, always tries AD. Password is in Local database but never tried.

**Why it happens:** String equality on email is case-sensitive by default in C#.

**How to avoid:**
- Use case-insensitive comparison: `email.Equals("admin@pertamina.com", StringComparison.OrdinalIgnoreCase)`
- Or: normalize input before comparison: `email.ToLowerInvariant()`
- Document the expected email format for admin account

**Warning signs:** Admin user can login from AD but fails when AD is down (fallback not triggered). Check logs for "LDAP timeout → no fallback attempt".

### Pitfall 5: Role Name Checks Without Null Guard

**What goes wrong:** Code assumes `userRole != null` after `GetRolesAsync().FirstOrDefault()` and compares `userRole == "Coach"`. Returns false for null, user sees "unauthorized" instead of actual role check.

**Why it happens:** FirstOrDefault returns null if no roles; comparison with string doesn't throw, silently fails.

**How to avoid:**
- After getting role, check null: `if (userRole == null) return Unauthorized();`
- Or use SelectedView field (always set, never null) instead of GetRolesAsync
- Test with user having no roles assigned (edge case)

**Warning signs:** New user can login but all pages show "unauthorized" even with correct role. Check database UserRoles table — if empty for that user, that's the issue.

## Code Examples

Verified patterns from project and ASP.NET Core official sources:

### Hybrid Auth Fallback Dispatcher (Option A: HybridAuthService)

```csharp
// Services/HybridAuthService.cs
public class HybridAuthService : IAuthService
{
    private readonly IAuthService _adService;
    private readonly IAuthService _localService;
    private readonly ILogger<HybridAuthService> _logger;
    private readonly IConfiguration _config;

    public HybridAuthService(LdapAuthService adService, LocalAuthService localService,
        ILogger<HybridAuthService> logger, IConfiguration config)
    {
        _adService = adService;
        _localService = localService;
        _logger = logger;
        _config = config;
    }

    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        bool isAdminFallback = email.Equals("admin@pertamina.com", StringComparison.OrdinalIgnoreCase);

        if (isAdminFallback)
        {
            _logger.LogInformation("Hybrid fallback path for admin account: trying AD first");
            var adResult = await _adService.AuthenticateAsync(email, password);
            if (adResult.Success)
                return adResult;

            _logger.LogInformation("AD failed, trying local auth as fallback");
            return await _localService.AuthenticateAsync(email, password);
        }

        // Non-admin: use configured default (AD or Local)
        var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
        return useAD
            ? await _adService.AuthenticateAsync(email, password)
            : await _localService.AuthenticateAsync(email, password);
    }
}

// Program.cs registration
services.AddScoped<IAuthService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var useAD = config.GetValue<bool>("Authentication:UseActiveDirectory", false);

    var adService = new LdapAuthService(config, provider.GetRequiredService<ILogger<LdapAuthService>>());
    var localService = new LocalAuthService(provider.GetRequiredService<SignInManager<ApplicationUser>>(), ...);

    return new HybridAuthService(adService, localService,
        provider.GetRequiredService<ILogger<HybridAuthService>>(), config);
});
```

**Source:** Composite pattern over IAuthService (Phase 71 established interface); null-safe email check with OrdinalIgnoreCase.

### Role Display via SelectedView (Clean Approach)

```csharp
// Views/Shared/_Layout.cshtml
@{
    var currentUser = await UserManager.GetUserAsync(User);
    var userRole = currentUser?.SelectedView ?? "Coachee";
    // ... initials code unchanged
}
<!-- Later in navbar -->
<span class="badge bg-primary bg-opacity-10 text-primary" style="font-size: 0.65rem;">@userRole</span>
```

**Source:** Phase 73 established SelectedView as kept-in-sync field; strict one-role policy makes this safe.

### EligibleCoaches with Coach-Only Filter

```csharp
// Controllers/AdminController.cs — ManageWorkers GET or BulkAssign endpoint
var allUsers = await _userManager.Users.ToListAsync();

// Phase 74: Coach role only (not level <= 5)
ViewBag.EligibleCoaches = allUsers
    .Where(async u => (await _userManager.GetRolesAsync(u)).Contains(UserRoles.Coach))
    .OrderBy(u => u.FullName)
    .ToList();
```

**Problem:** This is inefficient (N GetRolesAsync calls). Better approach:

```csharp
// Load UserRoles table join for efficiency
var eligibleCoaches = await _context.Users
    .Where(u => u.UserRoles.Any(ur => ur.Role.Name == UserRoles.Coach))
    .OrderBy(u => u.FullName)
    .ToListAsync();

ViewBag.EligibleCoaches = eligibleCoaches;
```

**Source:** EF Core query patterns; UserRoles FK relationship (ASP.NET Identity design).

### SectionHead RoleLevel Migration

```csharp
// Migrations/[DateStamp]_UpdateSectionHeadRoleLevel.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        UPDATE Users
        SET RoleLevel = 3
        WHERE Id IN (
            SELECT u.Id FROM Users u
            INNER JOIN UserRoles ur ON u.Id = ur.UserId
            INNER JOIN Roles r ON ur.RoleId = r.Id
            WHERE r.Name = 'Section Head'
        )
    ");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Revert
    migrationBuilder.Sql(@"
        UPDATE Users
        SET RoleLevel = 4
        WHERE Id IN (
            SELECT u.Id FROM Users u
            INNER JOIN UserRoles ur ON u.Id = ur.UserId
            INNER JOIN Roles r ON ur.RoleId = r.Id
            WHERE r.Name = 'Section Head'
        )
    ");
}
```

**Source:** EF Core migration template; uses role name join for safety (RoleLevel is redundant with role name, migration ensures sync).

### Evidence Upload Coach-Only Check

```csharp
// Controllers/CDPController.cs — UploadEvidence
var userRoles = await _userManager.GetRolesAsync(user);
bool isCoach = userRoles.Contains(UserRoles.Coach);

// Current buggy check:
// bool canUpload = progress.Status == "Active" && user.RoleLevel <= 5;  // WRONG: allows SrSupervisor

// Phase 74 fix:
bool canUpload = progress.Status == "Active" && isCoach;

if (!canUpload)
{
    TempData["Error"] = "Hanya Coach yang dapat upload evidence.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
```

**Source:** Explicit role check replaces level comparison; consistent with one-role-per-user policy.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Per-user AuthSource field | Global Authentication:UseActiveDirectory config | Phase 72 | Simpler routing, no migration per-user, config change at startup |
| Role name via GetRolesAsync() | RoleLevel field + UserRoles lookup | Phase 69/73 | Hierarchical access (level checks), SelectedView mapping, faster queries |
| Separate User/Admin forms | Single login form + transparent service | Phase 72 | Same UX for all users, no "enter your AD/local password here" hint |
| Manual role creation in controllers | SeedData.CreateRolesAsync + constants | Phase 69 | 9 roles pre-created, typos in role names caught at seed time |

**Deprecated/outdated:**
- AuthSource field (ApplicationUser): Removed Phase 72. Global config is the source of truth.
- Multiple role strings (e.g., "Admin" hardcoded in 5 places): UserRoles.cs is single source (Phase 69+).
- RoleLevel computed from role name dynamically: GetRoleLevel() lookup table is now standard.

## Open Questions

1. **Hybrid fallback strategy: HybridAuthService wrapper vs AccountController inline logic?**
   - What we know: Phase 71-73 established IAuthService abstraction. Fallback needs AD try first + Local fallback.
   - What's unclear: Is it cleaner to wrap services or handle in controller?
   - Recommendation: HybridAuthService wrapper (Option A above) keeps fallback logic testable and reusable. If complexity grows, wrapper pattern scales better than inline controller logic.

2. **SelectedView sync with GetDefaultView() — how to enforce?**
   - What we know: Phase 73 uses GetDefaultView() in SeedData. _Layout.cshtml should read SelectedView directly for display.
   - What's unclear: When role changes (edit profile), must SelectedView update automatically or manually?
   - Recommendation: Auto-update via controller POST when role changes. In EditProfile action, after saving role: `user.SelectedView = UserRoles.GetDefaultView(newRole); await _userManager.UpdateAsync(user);`

3. **Supervisor role — no seed user needed?**
   - What we know: Context says "no seed user — real users assigned via admin"
   - What's unclear: How is Supervisor role tested if no seed user exists?
   - Recommendation: Role seed in CreateRolesAsync (10 roles, no user assigned). Testing team assigns via ManageWorkers. Document that Supervisor role exists but has no default user.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected (no test directory, no xUnit/NUnit imports) |
| Config file | None — Wave 0 required |
| Quick run command | N/A — Wave 0 setup needed |
| Full suite command | N/A — Wave 0 setup needed |

### Phase Requirements → Test Map

**Note:** nyquist_validation is enabled (.planning/config.json), but no test infrastructure exists. Wave 0 gaps below list what's needed before unit tests can run.

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AUTH-HYBRID | Admin account tries AD first, falls back to Local on AD timeout/failure | Integration | N/A — Wave 0 | ❌ |
| AUTH-HYBRID | Non-admin accounts only use configured auth method (no fallback) | Integration | N/A — Wave 0 | ❌ |
| [ROLE-STRUCT] | Supervisor role at level 5 in UserRoles.GetRoleLevel | Unit | N/A — Wave 0 | ❌ |
| [ROLE-STRUCT] | SectionHead role at level 3 after migration | Integration | N/A — Wave 0 | ❌ |
| [ROLE-DISPLAY] | _Layout.cshtml displays currentUser.SelectedView (not GetRolesAsync) | Manual (visual) | N/A — Wave 0 | ❌ |
| [ROLE-STRUCT] | Evidence upload restricted to Coach role only | Integration | N/A — Wave 0 | ❌ |
| [ROLE-STRUCT] | EligibleCoaches dropdown includes only Coach role | Integration | N/A — Wave 0 | ❌ |

### Wave 0 Gaps

- [ ] `tests/` directory — xUnit or NUnit project setup
- [ ] `HcPortal.Tests.csproj` — test project with dependencies (xUnit.Runner.VisualStudio, Moq, etc.)
- [ ] `tests/Services/HybridAuthServiceTests.cs` — auth fallback logic tests
- [ ] `tests/Models/UserRolesTests.cs` — role level mapping and GetDefaultView tests
- [ ] `tests/Controllers/CDPControllerTests.cs` — evidence upload Coach-only, EligibleCoaches filtering
- [ ] AppFactory fixture for InMemory DbContext + test users (Phase 73 seed data pattern)
- [ ] Seed test data (admin, SectionHead, Coach, Supervisor users with roles assigned)

**Framework recommendation:** xUnit (ASP.NET Core standard since 2017; better async support, simpler syntax than NUnit).

## Sources

### Primary (HIGH confidence)
- Phase 71-73 codebase — IAuthService, LdapAuthService, LocalAuthService, UserRoles.cs, AccountController.Login, SeedData.CreateRolesAsync
- CONTEXT.md (74-hybrid-auth-role-restructuring) — locked decisions on fallback scope, one-role policy, role hierarchy
- REQUIREMENTS.md — AUTH-HYBRID requirement definition, v2.5 milestone scope
- ApplicationUser model — RoleLevel field (1-6), SelectedView field (String, default "Coachee")
- _Layout.cshtml — current role display via GetRolesAsync().FirstOrDefault() (line 7), navbar KC access check (line 67)
- CDPController — current bugs (RoleLevel <= 5 for coach checks, EligibleCoaches level filter)

### Secondary (MEDIUM confidence)
- ASP.NET Core Identity documentation (official) — UserManager roles, SignInManager session, IdentityRole creation, RoleManager API
- System.DirectoryServices documentation (Microsoft) — LDAP binding, attribute fetch, timeout patterns (Phase 71 verified)
- Entity Framework Core migrations documentation — SQL scripts in migrations, Up/Down patterns, data migration safety

### Tertiary (LOW confidence)
- None — no external research needed; project patterns are established and verified in Phase 71-73.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries pre-integrated, Phase 71-73 patterns verified
- Architecture: HIGH — IAuthService abstraction established, UserRoles centralized, one-role policy locked in CONTEXT
- Pitfalls: HIGH — email case-sensitivity, migration timing, role seeding all predictable in ASP.NET Core context

**Research date:** 2026-02-28
**Valid until:** 2026-03-14 (framework/config patterns stable; if ASP.NET Core 9 released, some Identity APIs may shift)

---

*Phase: 74-hybrid-auth-role-restructuring*
*Research complete: 2026-02-28*
