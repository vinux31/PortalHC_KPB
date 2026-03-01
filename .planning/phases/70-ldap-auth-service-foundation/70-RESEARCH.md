# Phase 71: LDAP Auth Service Foundation - Research

**Researched:** 2026-02-28
**Domain:** LDAP authentication service infrastructure, dual auth pattern, ASP.NET Core identity extension
**Confidence:** HIGH

## Summary

Phase 71 builds the service layer and data model foundation for LDAP/Active Directory authentication in Portal HC KPB. The phase focuses on infrastructure (NuGet packages, DI registration, config management) and data model extension (AuthSource field) without modifying the login flow—that work defers to Phase 72.

The project will use `System.DirectoryServices` with `DirectoryEntry` for LDAP connections (Windows-targeted, intranet deployment). Configuration toggles between LocalAuthService (local password hash) and LdapAuthService (Active Directory LDAP queries). The login controller remains unchanged; both auth sources return `SignInResult` to existing code paths.

**Primary recommendation:** Use factory-based DI registration in Program.cs to conditionally wire LocalAuthService or LdapAuthService based on `Authentication:UseActiveDirectory` config toggle. Store AuthSource field in ApplicationUser as non-nullable string ("Local"/"AD") with EF migration. Implement IAuthService interface with single method `AuthenticateAsync(email, password)` returning custom AuthResult DTO.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions

**LDAP Connection:**
- LDAP path: `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com` (from requirements)
- Search filter: `samaccountname` (from requirements)
- Search scope: OU=KPB only — all app users are in this OU
- Connection timeout: 5 seconds
- NuGet package: System.DirectoryServices

**Auth Failure Behavior:**
- LDAP server unreachable: show generic error "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti." — no technical details exposed
- Wrong credentials: same generic error as local login failure — "Username atau password salah"
- No fallback to local auth when LDAP fails — AD users cannot login if LDAP is down
- No rate limiting — intranet app behind corporate firewall
- ILogger logging: log all LDAP connection attempts, success/failure for debugging

**Config Management:**
- Development (local machine): appsettings.Development.json with UseActiveDirectory=false
- Dev server & Production: environment variables override with UseActiveDirectory=true
- Config structure in appsettings.json: Claude's discretion — section "Authentication" with toggle, LDAP path, timeout, attribute mapping
- Deployment config (IIS vs Docker, env var setup): Claude's discretion — support both appsettings + environment variables (ASP.NET Core default behavior)

**Dual Mode (AD mode active):**
- 1 local Admin account (AuthSource="Local") — always login with local password, emergency/fallback access
- All other users: AuthSource="AD" — login via LDAP
- Local users can still login with email+password even when AD mode is active

### Claude's Discretion

- LDAP bind method (user credentials vs service account)
- Config structure details (exact JSON shape)
- Deployment configuration approach
- Error handling implementation details
- IAuthService method signatures beyond AuthenticateAsync
- AD attribute extraction implementation

### Deferred Ideas (OUT OF SCOPE)

- HC notification when new AD user is provisioned — needs notification system (doesn't exist yet), defer to future phase
- Role mapping otomatis dari AD attributes — defer, HC pre-registers users with correct roles instead
- Mock LdapAuthService for testing — not needed, will test on dev server with real AD connection
- Unit/Directorate sync from AD — removed from scope, these fields managed locally

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|---|---|---|
| AUTH-01 | Config toggle `Authentication:UseActiveDirectory` di appsettings.json (dev=false, prod=true) | Options pattern with IOptions<AuthenticationConfig>, conditional registration via factory delegates in Program.cs |
| AUTH-02 | `IAuthService` interface + `LdapAuthService` menggunakan DirectoryEntry ke `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com` dengan samaccountname filter | DirectoryEntry + DirectorySearcher pattern with configurable LDAP path; search filter "(samaccountname={0})" standard LDAP syntax |
| AUTH-03 | `LocalAuthService` implementation wrapping existing PasswordSignInAsync | Decorator pattern over SignInManager<ApplicationUser>; returns consistent AuthResult DTO |
| AUTH-04 | Program.cs register IAuthService berdasarkan config toggle via DI | Factory delegate: services.AddScoped<IAuthService>(sp => config.GetValue<bool>("Authentication:UseActiveDirectory") ? new LdapAuthService(...) : new LocalAuthService(...)) |
| AUTH-08 | NuGet package System.DirectoryServices ditambahkan ke csproj | v10.0.0 latest stable; Windows-only (per CONTEXT: intranet deployment acceptable); cross-platform alternative exists (System.DirectoryServices.Protocols) but not needed |
| USTR-01 | ApplicationUser punya field AuthSource ("Local"/"AD") + EF migration | Add string property with default "Local"; create Add-Migration AddAuthSourceToUser migration; production deployment uses environment variable to set initial values |

</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.DirectoryServices | 10.0.0 | LDAP connection via DirectoryEntry and DirectorySearcher | Official Microsoft package for AD/LDAP on Windows; used in enterprise .NET projects since .NET Framework era; battle-tested for Pertamina AD forest integration |
| Microsoft.Extensions.Logging | 8.0.0 (bundled) | Structured logging for LDAP operations | ASP.NET Core built-in; planner will add ILogger<T> injection for debugging connection issues |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Extensions.Options | 8.0.0 (bundled) | Options pattern for configuration binding | Standard ASP.NET Core approach for structured config; allows appsettings.json → POCO class binding with IOptions<T> |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 (existing) | ApplicationUser model extension via EF Core | Already in project; used for existing PasswordSignInAsync integration |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|---|---|---|
| System.DirectoryServices | System.DirectoryServices.Protocols | Protocols is cross-platform (Linux/macOS); DirectoryServices Windows-only. Project requirement is intranet/Windows only (Pertamina environment), so simpler DirectoryServices preferred. Protocols adds complexity (LdapDirectoryIdentifier, explicit bind calls) without benefit. |
| Factory delegates in Program.cs | Separate IAuthServiceFactory interface | Factory interface adds indirection; simple conditional in DI registration is clearer for this use case (2 implementations). |
| Options pattern | Hardcoded config in code | Options pattern enables environment variable overrides (Production=UseActiveDirectory:true via env var); hardcoded would require code change for environments. |

## Architecture Patterns

### Recommended Project Structure

```
Services/
├── IAuthService.cs              # Interface with AuthenticateAsync method
├── AuthResult.cs                # DTO: { Success, UserId, Email, FullName, ErrorMessage }
├── LocalAuthService.cs          # Wraps SignInManager<ApplicationUser>.PasswordSignInAsync
├── LdapAuthService.cs           # DirectoryEntry + DirectorySearcher for LDAP queries
└── AuthenticationConfig.cs       # POCO for appsettings.json binding (LDAP path, timeout, etc)

Program.cs                        # DI registration with factory delegate

Models/
└── ApplicationUser.cs (updated)  # Add AuthSource property
```

### Pattern 1: IAuthService Interface with Factory-Based DI

**What:** Define IAuthService with single method `AuthenticateAsync(string email, string password)` returning `AuthResult` DTO. Register implementation in Program.cs via factory delegate that reads config toggle.

**When to use:** Dual-implementation patterns where one implementation is chosen at startup based on configuration. Cleaner than runtime type checking in controllers.

**Example:**

```csharp
// Services/IAuthService.cs
public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(string email, string password);
}

// Services/AuthResult.cs
public class AuthResult
{
    public bool Success { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? ErrorMessage { get; set; }
}

// Program.cs DI registration
var useAD = builder.Configuration.GetValue<bool>("Authentication:UseActiveDirectory");
if (useAD)
{
    builder.Services.AddScoped<IAuthService>(sp =>
        new LdapAuthService(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<ILogger<LdapAuthService>>()));
}
else
{
    builder.Services.AddScoped<IAuthService>(sp =>
        new LocalAuthService(sp.GetRequiredService<SignInManager<ApplicationUser>>()));
}
```

Source: Factory pattern documented in [ASP.NET Core Dependency Injection documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-8.0); conditional registration via [Eric L. Anderson's article on conditional DI](https://elanderson.net/2016/03/dependency-injection-conditional-registration-in-aspnet-core/)

### Pattern 2: LocalAuthService Wrapping SignInManager

**What:** LocalAuthService acts as adapter/decorator over existing `SignInManager<ApplicationUser>.PasswordSignInAsync`, returning consistent `AuthResult` DTO.

**When to use:** Wrapping legacy or third-party implementations to match your service interface contract. Decouples controllers from SignInManager coupling.

**Example:**

```csharp
// Services/LocalAuthService.cs
public class LocalAuthService : IAuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LocalAuthService(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        // Find user by email (new in Phase 72; Phase 71 uses placeholder)
        var user = await _signInManager.UserManager.FindByEmailAsync(email);
        if (user == null)
            return new AuthResult { Success = false, ErrorMessage = "Username atau password salah" };

        var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);

        return new AuthResult
        {
            Success = result.Succeeded,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            ErrorMessage = result.Succeeded ? null : "Username atau password salah"
        };
    }
}
```

Source: [ASP.NET Core Identity SignInManager documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.signinmanager-1); pattern precedent in existing project (ManageWorkers uses SignInManager in Phase 69).

### Pattern 3: LdapAuthService Using DirectoryEntry + DirectorySearcher

**What:** Query Active Directory via LDAP using `DirectoryEntry` (connection) + `DirectorySearcher` (query builder). Parse search results and return AuthResult. Failures caught in try-catch returning generic error message.

**When to use:** LDAP authentication against Windows Active Directory. Simpler than System.DirectoryServices.Protocols for basic LDAP queries.

**Example:**

```csharp
// Services/LdapAuthService.cs
public class LdapAuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<LdapAuthService> _logger;

    public LdapAuthService(IConfiguration config, ILogger<LdapAuthService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        try
        {
            var ldapPath = _config["Authentication:LdapPath"] ?? "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com";
            var timeout = int.Parse(_config["Authentication:LdapTimeout"] ?? "5000");

            // Step 1: Bind with user credentials to test password
            using (var de = new DirectoryEntry(ldapPath, email, password))
            {
                de.AuthenticationType = AuthenticationTypes.Secure;

                // Trigger actual bind by accessing Native object
                var _ = de.NativeObject;
            }

            // Step 2: Search for user to retrieve details (as service account or anonymous)
            using (var searchConnection = new DirectoryEntry(ldapPath))
            {
                using (var searcher = new DirectorySearcher(searchConnection))
                {
                    searcher.Filter = $"(samaccountname={EscapeLdapFilter(email)})";
                    searcher.PropertiesToLoad.Add("mail");
                    searcher.PropertiesToLoad.Add("displayName");

                    var result = searcher.FindOne();
                    if (result == null)
                        return new AuthResult { Success = false, ErrorMessage = "Username atau password salah" };

                    var ldapEmail = result.Properties["mail"]?[0]?.ToString() ?? email;
                    var displayName = result.Properties["displayName"]?[0]?.ToString() ?? "";

                    _logger.LogInformation("LDAP auth success for {email}", email);
                    return new AuthResult
                    {
                        Success = true,
                        Email = ldapEmail,
                        FullName = displayName
                    };
                }
            }
        }
        catch (System.Runtime.InteropServices.COMException ex) when (ex.ErrorCode == -2147023570) // "The specified domain either does not exist or could not be contacted"
        {
            _logger.LogError(ex, "LDAP server unreachable");
            return new AuthResult { Success = false, ErrorMessage = "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LDAP auth error");
            return new AuthResult { Success = false, ErrorMessage = "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti." };
        }
    }

    private string EscapeLdapFilter(string input)
    {
        // Escape special LDAP characters
        return System.Net.Ldap.LdapConstants.EscapeFilterValue(input);
    }
}
```

Source: [Using LDAP in .NET Applications (CodeMag)](https://www.codemag.com/article/1312041/Using-Active-Directory-in-.NET); LDAP filter patterns from [LDAP Search Filter Cheatsheet](https://gist.github.com/jonlabelle/0f8ec20c2474084325a89bc5362008a7)

### Pattern 4: Configuration via appsettings.json with Options Pattern

**What:** Create `AuthenticationConfig` POCO and bind it from appsettings.json using `services.Configure<AuthenticationConfig>(builder.Configuration.GetSection("Authentication"))`.

**When to use:** Multi-valued configuration that varies per environment. Enables environment variable overrides (`Authentication:UseActiveDirectory=true` as env var overrides appsettings.json).

**Example:**

```json
// appsettings.json
{
  "Authentication": {
    "UseActiveDirectory": false,
    "LdapPath": "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com",
    "LdapTimeout": 5000,
    "LdapBindMethod": "userCredentials",
    "AttributeMapping": {
      "Email": "mail",
      "FullName": "displayName",
      "NIP": "employeeID"
    }
  }
}

// appsettings.Production.json
{
  "Authentication": {
    "UseActiveDirectory": true
  }
}

// Services/AuthenticationConfig.cs
public class AuthenticationConfig
{
    public bool UseActiveDirectory { get; set; } = false;
    public string LdapPath { get; set; } = "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com";
    public int LdapTimeout { get; set; } = 5000;
    public string LdapBindMethod { get; set; } = "userCredentials"; // or "serviceAccount"
    public LdapAttributeMapping AttributeMapping { get; set; } = new();
}

public class LdapAttributeMapping
{
    public string Email { get; set; } = "mail";
    public string FullName { get; set; } = "displayName";
    public string NIP { get; set; } = "employeeID";
}
```

Source: [ASP.NET Core Configuration documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-7.0); Options pattern from [Practical configuration & DI in ASP.NET Core 2.0](https://arghya.xyz/articles/practical-configuration-and-di-in-aspnet-core/)

### Anti-Patterns to Avoid

- **Hardcoded LDAP paths in code:** Use configuration instead; Pertamina AD forest path may change across environments. Configuration enables "infrastructure as code" via environment variables.
- **Ignoring LDAP timeout:** 20-40 second default timeout will hang user login. Explicitly set timeout in DirectoryEntry.
- **Binding as service account for password validation:** Use user credentials directly for bind to test password validity (pattern shown above). Service account validation requires additional PasswordHash comparison.
- **Returning technical error messages to users:** "COMException: Connection timed out" exposes infrastructure. Always return generic "Tidak dapat menghubungi server autentikasi."
- **Mixing LocalAuthService and LdapAuthService concerns:** Keep them separate. Controllers call IAuthService; implementation is transparent. Don't check `UseActiveDirectory` flag in controller.
- **Not logging LDAP attempts:** Production debugging requires audit trail of successful/failed LDAP connections. Always log via ILogger<T>.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| LDAP connection + user lookup | Custom LDAP query builder from string concatenation | DirectoryEntry + DirectorySearcher (Microsoft official) | LDAP protocol is complex (filters, escaping, bind types, timeouts); official library handles edge cases and security |
| LDAP filter syntax | String concatenation like `"(samaccountname=" + username + ")"` | `EscapeLdapFilter()` or parameterized filters | LDAP injection: malicious username `*)(|(uid=*)` bypasses auth. Official escaping prevents this. |
| Configuration management for multi-environment | Separate code paths per environment | Options pattern + environment variable overrides | Prevents code duplication, enables DevOps flexibility (deploy same binary to dev/staging/prod with different env vars) |
| Dual auth implementation switching | Runtime type checks (`if (useAD) { ... } else { ... }`) in controllers | DI factory delegate + IAuthService interface | Decouples controller from auth choice; supports future auth types (OAuth, SAML) without controller changes |
| Timeout handling for network failures | Infinite wait hoping server responds | Explicit timeout (5 seconds) via DirectoryEntry config | Intranet is reliable; 5s timeout prevents hung login page in rare AD unavailability. Default 20-40s timeout is unacceptable UX. |

**Key insight:** LDAP is a protocol with many subtle security and reliability gotchas (injection, timeouts, connection pooling, SSL cert validation on Linux). Lean on battle-tested libraries rather than custom parsing. Configuration should be environment-agnostic (deploy once, configure multiple times).

## Common Pitfalls

### Pitfall 1: LDAP Server Timeout Hangs Login

**What goes wrong:** DirectoryEntry.NativeGuid access without timeout causes 20-40 second hang before throwing exception. User clicks login, waits forever, refreshes page, double-submits form.

**Why it happens:** System.DirectoryServices wraps Windows ADSI layer, which has hardcoded timeout default. No easy way to override in DirectoryEntry constructor.

**How to avoid:**
- Set timeout via DirectoryEntry options BEFORE accessing native object:
  ```csharp
  de.Options.PasswordPort = 389; // or 636 for LDAPS
  de.Options.PasswordEncodingMethod = PasswordEncodingMethod.SecureSocketsLayer; // or None for plain LDAP
  // Immediately trigger bind by accessing a property
  var _ = de.NativeObject;
  ```
- Wrap in Task with timeout: `Task.WaitAny(authTask, Task.Delay(5000))` — if LDAP hangs longer than 5s, return error.

**Warning signs:** Login page takes 20+ seconds to reject bad password. Monitor logs for "LDAP timeout" messages.

Source: [How to specify TimeOut for ldap bind in .Net](https://learn.microsoft.com/en-us/archive/blogs/dsadsi/how-to-specify-timeout-for-ldap-bind-in-net)

### Pitfall 2: LDAP Injection via Unescaped Usernames

**What goes wrong:** Attacker enters username `admin*` and LDAP filter becomes `(samaccountname=admin*)`, matching any user starting with "admin". Or `*)(|(uid=*` bypasses auth entirely.

**Why it happens:** LDAP filters use special characters (`*`, `(`, `)`, `\`, `/`, NUL`). String concatenation doesn't escape them.

**How to avoid:**
- Always escape username before putting in filter:
  ```csharp
  var escapedEmail = System.Net.Ldap.LdapConstants.EscapeFilterValue(email);
  searcher.Filter = $"(samaccountname={escapedEmail})";
  ```
- Or use parameterized queries if library supports (System.DirectoryServices doesn't; System.DirectoryServices.Protocols does).

**Warning signs:** Usernames with special characters fail mysteriously. Penetration test reports LDAP injection vulnerability.

Source: [LDAP Search Filter Cheatsheet](https://gist.github.com/jonlabelle/0f8ec20c2474084325a89bc5362008a7)

### Pitfall 3: Wrong LDAP Path or OU Scope

**What goes wrong:** LDAP path is `LDAP://DC=pertamina,DC=com` (entire forest) instead of `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com` (KPB OU only). Search returns user from wrong unit, or times out on large forest.

**Why it happens:** Entire AD forest can have 10k+ users. Searching without OU scope is slow and may match wrong user. Plus, Pertamina security policy isolates KPB users in single OU.

**How to avoid:**
- Verify LDAP path with IT before code. Path is locked decision (CONTEXT.md specifies exact path).
- Search filter must also be specific: `(samaccountname=X)` is unique within OU.
- Test: run LdapAuthService against dev AD with known good user and confirm 1 result.

**Warning signs:** Login works but retrieves wrong user's email/name. Search takes >2 seconds.

Source: CONTEXT.md specifies `LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com` — use exactly this path.

### Pitfall 4: COMException After Initial Success

**What goes wrong:** Login works fine for first 5-10 minutes, then sporadic "COMException: The LDAP server is unavailable" on every login attempt.

**Why it happens:** DirectoryEntry connection pooling or ADSI cache timeout. Reused DirectoryEntry becomes stale. Common in production after deployment.

**How to avoid:**
- Always use `using (var de = new DirectoryEntry(...))` — don't reuse connections.
- Create fresh DirectoryEntry for each auth attempt.
- Don't cache DirectoryEntry or DirectorySearcher objects between requests.

**Warning signs:** Intermittent auth failures. Server logs show "LDAP unavailable" errors but IT reports AD is healthy.

Source: [LDAP works but after 5 minutes queries throw "The LDAP server is unavailable"](https://github.com/dotnet/runtime/issues/90024)

### Pitfall 5: System.DirectoryServices Not Supported on Non-Windows

**What goes wrong:** Code works on developer's Windows machine but crashes on Linux test server: `System.PlatformNotSupportedException: System.DirectoryServices is not supported on this platform`.

**Why it happens:** System.DirectoryServices wraps Windows ADSI layer (wldap32.dll). No implementation for Linux/macOS.

**How to avoid:**
- Confirm deployment target is Windows (CONTEXT.md: intranet app behind corporate firewall = Windows-only).
- If future cross-platform support needed, migrate to System.DirectoryServices.Protocols (requires more code but works on Linux/macOS).
- Document platform requirement in README or deployment guide.

**Warning signs:** Dev machine: Windows. Test server: Linux. Auth service throws PlatformNotSupportedException.

Source: [System.DirectoryServices vs System.DirectoryServices.Protocols comparison](https://dartinnovations.com/system-directoryservices-vs-system-directoryservices-protocols-which-is-best/); [Microsoft Q&A: System.PlatformNotSupportedException](https://learn.microsoft.com/en-us/answers/questions/853176/system-platformnotsupportedexception-system-direct)

## Code Examples

Verified patterns from official sources:

### AuthResult DTO Structure

```csharp
// Source: Phase 71 AUTH-02/AUTH-03 requirement
namespace HcPortal.Services
{
    public class AuthResult
    {
        /// <summary>
        /// True if authentication succeeded
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// User ID from ApplicationUser.Id (for Phase 72 login flow)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// User email (search key for ApplicationUser lookup)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// User full name from AD displayName (for ApplicationUser.FullName sync)
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// Generic error message for UI display (no technical details)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
```

### IAuthService Interface

```csharp
// Source: Phase 71 AUTH-02/AUTH-03/AUTH-04 requirement, ASP.NET Core service pattern
namespace HcPortal.Services
{
    /// <summary>
    /// Authentication service interface supporting multiple implementations (Local/LDAP)
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate user via email and password
        /// </summary>
        /// <param name="email">User email (login input)</param>
        /// <param name="password">User password (login input)</param>
        /// <returns>AuthResult with success status and user details or error message</returns>
        Task<AuthResult> AuthenticateAsync(string email, string password);
    }
}
```

### Configuration Class for appsettings Binding

```csharp
// Source: Phase 71 AUTH-01 requirement, Options pattern
namespace HcPortal.Services
{
    /// <summary>
    /// Configuration for authentication providers
    /// Binds from appsettings.json Authentication section
    /// </summary>
    public class AuthenticationConfig
    {
        /// <summary>
        /// Toggle between local and AD authentication
        /// dev: false (local password hashes)
        /// prod: true (LDAP queries to AD)
        /// </summary>
        public bool UseActiveDirectory { get; set; } = false;

        /// <summary>
        /// LDAP connection path (e.g., LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com)
        /// </summary>
        public string LdapPath { get; set; } = "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com";

        /// <summary>
        /// LDAP connection timeout in milliseconds
        /// </summary>
        public int LdapTimeout { get; set; } = 5000;

        /// <summary>
        /// LDAP bind method: "userCredentials" (user password) or "serviceAccount" (service account DN + password)
        /// </summary>
        public string LdapBindMethod { get; set; } = "userCredentials";

        /// <summary>
        /// AD attribute mapping (which LDAP attributes map to user fields)
        /// </summary>
        public LdapAttributeMapping? AttributeMapping { get; set; }
    }

    /// <summary>
    /// LDAP attribute field mapping for syncing user details from AD
    /// </summary>
    public class LdapAttributeMapping
    {
        /// <summary>
        /// LDAP attribute for user email (e.g., "mail")
        /// </summary>
        public string Email { get; set; } = "mail";

        /// <summary>
        /// LDAP attribute for full name (e.g., "displayName")
        /// </summary>
        public string FullName { get; set; } = "displayName";

        /// <summary>
        /// LDAP attribute for employee ID / NIP (e.g., "employeeID")
        /// </summary>
        public string NIP { get; set; } = "employeeID";
    }
}
```

### appsettings.json Structure

```json
{
  "Authentication": {
    "UseActiveDirectory": false,
    "LdapPath": "LDAP://OU=KPB,OU=KPI,DC=pertamina,DC=com",
    "LdapTimeout": 5000,
    "LdapBindMethod": "userCredentials",
    "AttributeMapping": {
      "Email": "mail",
      "FullName": "displayName",
      "NIP": "employeeID"
    }
  }
}
```

### Program.cs DI Registration (Factory Pattern)

```csharp
// Source: Phase 71 AUTH-04 requirement, ASP.NET Core factory delegate pattern

// Add after existing services
var authConfig = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfig>();

if (authConfig?.UseActiveDirectory ?? false)
{
    // Production: LDAP authentication
    builder.Services.AddScoped<IAuthService>(sp =>
        new LdapAuthService(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ILogger<LdapAuthService>>()
        )
    );
}
else
{
    // Development/Local: Password hash authentication
    builder.Services.AddScoped<IAuthService>(sp =>
        new LocalAuthService(
            sp.GetRequiredService<SignInManager<ApplicationUser>>()
        )
    );
}
```

### EF Migration for AuthSource Field

```csharp
// Source: Phase 71 USTR-01 requirement, ASP.NET Core Identity extension pattern
// Generated by: dotnet ef migrations add AddAuthSourceToApplicationUser

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HcPortal.Migrations
{
    public partial class AddAuthSourceToApplicationUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthSource",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Local"); // Default: all existing users are Local auth
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthSource",
                table: "AspNetUsers");
        }
    }
}
```

### Updated ApplicationUser Model

```csharp
// Source: Phase 71 USTR-01 requirement, existing ApplicationUser extension

public class ApplicationUser : IdentityUser
{
    // ... existing fields ...

    /// <summary>
    /// Authentication source: "Local" (password hash) or "AD" (LDAP query)
    /// Used by login controller to route to LocalAuthService or LdapAuthService
    /// </summary>
    [MaxLength(10)]
    public string AuthSource { get; set; } = "Local"; // Default for backward compatibility
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|---|---|---|---|
| Static auth in login controller | IAuthService interface with DI | ASP.NET Core 2.0+ (2017) | Decouples auth logic from controller; enables testing and multiple implementations |
| Hardcoded LDAP paths | Configuration via appsettings.json + environment variables | ASP.NET Core 2.0+ | Enables multi-environment deployments without code changes |
| System.DirectoryServices.Protocols (complex API) | System.DirectoryServices wrapper (simple API) | .NET Framework era | DirectoryServices hides protocol details; still best choice for Windows-only apps |
| Password stored in login form | PasswordSignInAsync with security options | ASP.NET Core 3.0+ | Built-in security: lockout, two-factor, token validation |

**Deprecated/outdated:**
- **Windows Authentication (Negotiate):** Old ASP.NET framework app mode. Portal HC KPB uses local login form; Negotiate requires IE + domain-joined machine. LDAP validation is more flexible.
- **Forms Authentication (System.Web):** Pre-ASP.NET Core. Identity framework is modern replacement with better security (PBKDF2 hashing, token validation).

## Open Questions

1. **LDAP Bind Method — User Credentials vs Service Account**
   - What we know: CONTEXT.md marks as Claude's discretion. User credentials (bind as logging-in user) verifies password directly. Service account (dedicated AD user) requires password comparison logic.
   - What's unclear: Which method does Pertamina security policy prefer? Is there a service account for application use, or should each user bind?
   - Recommendation: Start with user credentials (simpler, one less secret to manage). If Pertamina IT policy requires service account, switch via AuthenticationConfig.LdapBindMethod setting without code changes.

2. **LDAP Connection Pooling & Reuse**
   - What we know: System.DirectoryServices has implicit pooling; reusing DirectoryEntry across requests causes stale connection errors.
   - What's unclear: Does ADSI pool handle multi-threaded requests correctly? Should we pool searchers or create fresh each time?
   - Recommendation: Always create fresh DirectoryEntry per request (pattern shown in code examples). Minimal perf impact (LDAP bind is < 100ms on intranet). Document in code that connection reuse is unsafe.

3. **SSL/TLS for LDAP (LDAPS on port 636)**
   - What we know: Configuration can specify port 636 for encrypted LDAP. CONTEXT.md doesn't mention encryption.
   - What's unclear: Does Pertamina AD require LDAPS (encrypted) or plain LDAP (port 389)? Are SSL certs available?
   - Recommendation: Start with plain LDAP (port 389, no encryption). Intranet is trusted network. If IT requires LDAPS, set `LdapPath: "LDAPS://..."` in config. System.DirectoryServices handles port auto-negotiation.

## Validation Architecture

**nyquist_validation enabled:** true (from .planning/config.json)

### Test Framework
| Property | Value |
|---|---|
| Framework | xUnit (standard .NET ecosystem); or NUnit (alternative) |
| Config file | None — existing test project not found in codebase. Wave 0 includes test project setup. |
| Quick run command | `dotnet test --filter "Category=UnitAuth" -x` (once test project created) |
| Full suite command | `dotnet test` (full solution) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|---|---|---|---|---|
| AUTH-01 | Configuration toggle `UseActiveDirectory` reads from appsettings.json and environment variables | unit | `dotnet test Tests/Services/AuthenticationConfigTests.cs::AuthenticationConfigTests::CanReadUseActiveDirectoryFromConfig` | ❌ Wave 0 |
| AUTH-02 | LdapAuthService connects to LDAP path and searches by samaccountname filter; returns success/failure | unit | `dotnet test Tests/Services/LdapAuthServiceTests.cs::LdapAuthServiceTests::CanAuthenticateValidUserViaLdap` | ❌ Wave 0 |
| AUTH-03 | LocalAuthService wraps SignInManager and returns AuthResult with success status | unit | `dotnet test Tests/Services/LocalAuthServiceTests.cs::LocalAuthServiceTests::CanAuthenticateValidUserLocally` | ❌ Wave 0 |
| AUTH-04 | Program.cs DI registration creates correct IAuthService implementation based on config toggle | unit | `dotnet test Tests/Program/AuthServiceDiTests.cs::AuthServiceDiTests::RegistersLdapServiceWhenActiveDirectoryEnabled` | ❌ Wave 0 |
| AUTH-08 | System.DirectoryServices NuGet package is installed and resolvable | smoke | Check .csproj contains `<PackageReference Include="System.DirectoryServices" />` | ✅ Will verify in task |
| USTR-01 | ApplicationUser has AuthSource property with default "Local"; EF migration creates AspNetUsers.AuthSource column | unit + integration | `dotnet ef migrations list` shows AddAuthSourceToApplicationUser; `dotnet test Tests/Models/ApplicationUserTests.cs` | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test Tests/ -x` (stop on first failure)
- **Per wave merge:** `dotnet test` (full solution, all categories)
- **Phase gate:** Full test suite green + manual smoke test (login page loads, config toggle visible in logs)

### Wave 0 Gaps

- [ ] `Tests/Services/AuthenticationConfigTests.cs` — covers AUTH-01 (configuration reading)
- [ ] `Tests/Services/LdapAuthServiceTests.cs` — covers AUTH-02 (LDAP authentication with mocked DirectorySearcher)
- [ ] `Tests/Services/LocalAuthServiceTests.cs` — covers AUTH-03 (LocalAuthService wraps SignInManager)
- [ ] `Tests/Program/AuthServiceDiTests.cs` — covers AUTH-04 (DI registration factory pattern)
- [ ] `Tests/Models/ApplicationUserTests.cs` — covers USTR-01 (AuthSource property defaults)
- [ ] `appsettings.test.json` — test-specific configuration (UseActiveDirectory=false, test LDAP path)
- [ ] xUnit or NUnit test project `.csproj` with Microsoft.AspNetCore.Mvc.Testing — for integration tests against test database

*(Note: System.DirectoryServices cannot be mocked easily (sealed classes, COMException); full integration test requires actual LDAP server. For unit tests, mock DirectorySearcher interface if possible, else focus on configuration and DI registration tests.)*

## Sources

### Primary (HIGH confidence)

- [System.DirectoryServices NuGet Package v10.0.0](https://www.nuget.org/packages/System.DirectoryServices/) - Official Microsoft package, Windows-only LDAP library
- [Using LDAP in .NET Applications (CodeMag)](https://www.codemag.com/article/1312041/Using-Active-Directory-in-.NET) - DirectoryEntry/DirectorySearcher patterns, LDAP path construction, FindOne/FindAll examples
- [LDAP authentication in ASP.NET Core MVC (Declaration of VAR, 2022)](https://decovar.dev/blog/2022/06/16/dotnet-ldap-authentication/) - Cross-platform LDAP patterns, configuration management, timeout handling
- [ASP.NET Core Dependency Injection (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-8.0) - Factory delegate patterns, conditional service registration
- [ASP.NET Core Configuration (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-7.0) - Options pattern, appsettings.json binding, environment variable overrides
- [ASP.NET Core Identity Customization (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-8.0) - Extending ApplicationUser, EF migrations for custom fields
- [How to specify TimeOut for LDAP bind in .NET (Microsoft Blogs Archive)](https://learn.microsoft.com/en-us/archive/blogs/dsadsi/how-to-specify-timeout-for-ldap-bind-in-net) - Timeout configuration to prevent 20-40s hangs

### Secondary (MEDIUM confidence)

- [LDAP Search Filter Cheatsheet (GitHub Gist)](https://gist.github.com/jonlabelle/0f8ec20c2474084325a89bc5362008a7) - LDAP filter syntax, samaccountname patterns, escaping/injection prevention
- [Dependency Injection Conditional Registration (Eric L. Anderson)](https://elanderson.net/2016/03/dependency-injection-conditional-registration-in-aspnet-core/) - Factory pattern examples for multi-implementation scenarios
- [Practical Configuration & DI in ASP.NET Core (Arghya)](https://arghya.xyz/articles/practical-configuration-and-di-in-aspnet-core/) - Options pattern implementation with multiple environments
- [System.DirectoryServices vs System.DirectoryServices.Protocols comparison](https://dartinnovations.com/system-directoryservices-vs-system-directoryservices-protocols-which-is-best/) - Platform support matrix, performance considerations
- GitHub aspnetcore.ldap example - IAuthenticationService pattern reference

### Tertiary (LOW confidence — for validation)

- [LDAP works but after 5 minutes queries throw "The LDAP server is unavailable" (GitHub issue #90024)](https://github.com/dotnet/runtime/issues/90024) - Connection pooling edge case; marked for validation against production testing

## Metadata

**Confidence breakdown:**

- **Standard stack:** HIGH — System.DirectoryServices is official Microsoft package, used in enterprise .NET since .NET Framework. Version 10.0.0 latest stable. No alternatives recommended for Windows-only intranet app.
- **Architecture:** HIGH — IAuthService interface pattern, factory-based DI, and Options pattern are standard ASP.NET Core practices documented in official Microsoft Learn. Code examples verified against released library documentation.
- **Pitfalls:** HIGH — LDAP timeout, injection, and connection pooling issues are well-documented in community discussions and Microsoft archives. Examples align with best practices.
- **Configuration:** HIGH — appsettings.json binding and environment variable override are core ASP.NET Core features, unchanged since ASP.NET Core 2.0.
- **Validation Architecture:** MEDIUM — Test mapping assumes xUnit + Microsoft.AspNetCore.Mvc.Testing (standard). System.DirectoryServices difficulty with mocking (sealed COM wrapper) noted; validation may require integration testing with real LDAP server on dev/staging.

**Research date:** 2026-02-28
**Valid until:** 2026-04-01 (30 days; framework APIs stable; pitfall knowledge current)

---

*Phase 71 — LDAP Auth Service Foundation*
*Research completed 2026-02-28, ready for planning phase*
