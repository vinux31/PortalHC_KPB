# Phase 72: Dual Auth Login Flow - Research

**Researched:** 2026-02-28
**Domain:** ASP.NET Core authentication, IAuthService abstraction, global configuration routing, profile sync, form adaptation
**Confidence:** HIGH (Phase 71 foundation already implemented, patterns verified in codebase)

## Summary

Phase 72 integrates IAuthService abstraction (built in Phase 71) into AccountController's login POST flow, implements global configuration-based auth routing (replacing per-user AuthSource), and adapts UI and import workflows for AD mode. The phase is constrained by locked user decisions from CONTEXT.md, including: no per-user routing, no fallback from LDAP to local auth, mandatory pre-registration by HC, password field hidden in AD mode, dynamic import template generation, and profile sync (FullName/Email only).

Phase 71 completed the foundation: IAuthService interface, LocalAuthService (wrapping PasswordSignInAsync), LdapAuthService (DirectoryEntry LDAP bind with RFC 4515 escaping), AuthResult DTO, and Program.cs factory DI registration. Phase 72 wires this into the login flow and removes AuthSource field via EF migration.

**Primary recommendation:** Rewrite AccountController.Login POST to call IAuthService.AuthenticateAsync, handle null user rejection, sync profiles (AD mode), remove password field visibility in ManageWorkers (AD mode), and generate dynamic import templates conditional on UseActiveDirectory config.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Auth Routing (MAJOR CHANGE from Phase 71)**
- Global config routing — UseActiveDirectory=true → ALL users authenticate via LDAP. No per-user routing.
- AuthSource field REMOVED — no longer needed. Global toggle is the only routing mechanism.
- Phase 72 includes EF migration to DROP AuthSource column from ApplicationUser
- No emergency local admin access — if LDAP is down, nobody can login. This is acceptable per user decision.
- No fallback from LDAP to local auth

**Login Flow (AD mode active)**
- Flow: Email+Password → IAuthService.AuthenticateAsync (LdapAuthService) → Find user by email in DB → Sync profile → SignInAsync → Redirect
- If user not in DB → reject: "Akun Anda belum terdaftar. Hubungi HC."
- No auto-provisioning — user MUST be pre-registered by HC via ManageWorkers
- Redirect behavior: same as current (no changes)
- Remember Me checkbox: retained, works same as now

**Login Flow (Local mode — UseActiveDirectory=false)**
- Flow: Email+Password → IAuthService.AuthenticateAsync (LocalAuthService) → SignInAsync → Redirect
- Zero visual changes to login page — identical to current behavior
- No profile sync in local mode
- ManageWorkers: no changes, password field visible and required
- Import template: unchanged, Password column present

**Login Page Visual**
- Login page identical for both modes — Email + Password fields
- AD mode: small grey hint text below the login button: "Login menggunakan akun Pertamina"
- Local mode: no hint, page is exactly as it is now
- Error display: same pattern as current login page (no styling changes)

**Error Messages (Bahasa Indonesia)**
- Wrong password: "Username atau password salah"
- User not in DB: "Akun Anda belum terdaftar. Hubungi HC."
- LDAP server down: "Tidak dapat menghubungi server autentikasi. Silakan coba lagi nanti."
- Display method: same as current login page error pattern

**Profile Sync (AD mode only)**
- Sync FullName (from AD displayName) and Email (from AD mail) — only these 2 fields
- Sync happens BEFORE session creation (data from AuthResult, no extra AD call)
- Flow: Auth → Find user → Update FullName/Email in DB → SignInAsync → Redirect
- Null handling: skip null AD values, log warning, do not overwrite existing DB data
- Sync failure: login continues anyway (auth succeeded = user allowed in). Retry on next login.
- No detailed sync logging — sync happens silently
- Local mode: no sync at all

**ManageWorkers Adaptation (AD mode active)**
- Create form: password field HIDDEN, system auto-generates random password in backend
- Edit form: password field HIDDEN — user changes password via Pertamina portal, not this app
- FullName and Email fields: READ-ONLY for AD users (synced from AD, HC cannot override)
- No AuthSource column in list view (field being removed entirely)
- Local mode: ManageWorkers unchanged — password field visible, all fields editable

**Import Template (AD mode active)**
- 1 dynamic template — download endpoint checks UseActiveDirectory config
- AD mode: Excel template WITHOUT Password column. System auto-generates random passwords during import.
- Local mode: Excel template WITH Password column (same as current)
- No AuthSource column in any template (field being removed)
- Import logic: if AD mode, generate random password for each user; if local mode, use password from Excel

**Session/Cookie**
- No changes — 8 hour expiration, sliding expiration, 30 minute session idle timeout
- Same configuration for both AD and local mode

### Claude's Discretion
- Random password generation method (Guid, crypto random, etc.)
- Exact implementation of dynamic import template
- How to handle the AuthSource migration (simple DROP or soft deprecation)
- Profile sync error handling implementation details
- Login page hint positioning and exact CSS styling

### Deferred Ideas (OUT OF SCOPE)
- HC notification when AD user can't login (email mismatch etc.) — needs notification system
- Bulk AuthSource migration tool — not needed since field is being removed
- AD attribute mapping UI — configure via appsettings.json, no UI needed
- Re-validate AD session on every request — standard session cookie is sufficient for intranet app
- "Change password" feature for AD users in this app — not applicable, Pertamina portal handles this

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|---|---|---|
| **AUTH-05** | Login page: Email + Password (identik kedua mode); AD mode tampilkan hint kecil "Login menggunakan akun Pertamina" di bawah form | IAuthService abstraction already tested; global config available in Program.cs; Login.cshtml pattern supports hint addition |
| **AUTH-06** | User belum terdaftar di DB → ditolak: "Akun Anda belum terdaftar. Hubungi HC." (no auto-provisioning, HC pre-registers via ManageWorkers) | LocalAuthService/LdapAuthService patterns show null user rejection; AdminController.ImportWorkers shows HC bulk registration pattern |
| **AUTH-07** | AD user login: sync FullName (displayName) dan Email (mail) saja; skip null values; Role/SelectedView TIDAK pernah diubah | AuthResult DTO already has Email/FullName fields; LdapAuthService extracts from AD; Phase 71 patterns verify integration approach |

</phase_requirements>

---

## Standard Stack

### Core Infrastructure (Already in Place — Phase 71)

| Component | Version | Purpose | Status |
|-----------|---------|---------|--------|
| **IAuthService** | Phase 71 | Authentication abstraction interface | Implemented, factory DI registered |
| **LocalAuthService** | Phase 71 | Local password-based auth using Identity | Implemented, uses CheckPasswordSignInAsync |
| **LdapAuthService** | Phase 71 | AD auth via DirectoryEntry LDAP bind | Implemented, RFC 4515 escaping, 5s timeout |
| **AuthResult DTO** | Phase 71 | Auth success/failure response object | Implemented, carries Email/FullName/ErrorMessage |
| **IConfiguration** | ASP.NET Core 8 | Reads appsettings.json Authentication section | Native, already used |
| **Program.cs DI** | Phase 71 | Factory delegate for conditional service registration | Implemented, switches on UseActiveDirectory config |

### Supporting Libraries

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | 0.105.0+ | Excel template generation/import | Already in project; Phase 69 uses for CoachCoacheeMapping export; AdminController.DownloadImportTemplate pattern established |
| System.DirectoryServices | .NET Framework/Core | LDAP connectivity (COM interop) | Phase 71 added to csproj; only choice for AD on Windows/.NET Core |
| System.Reflection | .NET runtime | Generate random password (SecureRandom option) | Native, no dependency |

---

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
├── AccountController.cs              # Login POST rewrite (Phase 72-01)

Views/
├── Account/
│   └── Login.cshtml                  # Add hint text (Phase 72-02)

Models/
└── ApplicationUser.cs                # Remove AuthSource field (Phase 72-01)

Services/
├── IAuthService.cs                   # (Phase 71, unchanged)
├── LocalAuthService.cs               # (Phase 71, unchanged)
├── LdapAuthService.cs                # (Phase 71, unchanged)
└── AuthResult.cs                     # (Phase 71, unchanged)

Migrations/
└── [timestamp]_RemoveAuthSourceField.cs    # EF migration DROP column (Phase 72-01)

Data/
└── ApplicationDbContext.cs           # Remove AuthSource DbProperty config (Phase 72-01)
```

### Pattern 1: Login Flow with IAuthService (NEW)

**What:** Rewrite AccountController.Login POST to use IAuthService.AuthenticateAsync instead of direct PasswordSignInAsync. Handles null user rejection, profile sync (AD only), and session creation.

**When to use:** All login attempts, both Local and AD modes. IAuthService abstracts the auth mechanism.

**Example (Phase 72-01 implementation):**

```csharp
// POST /Account/Login
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(
    string email,
    string password,
    bool rememberMe = false,
    string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        ModelState.AddModelError(string.Empty, "Email dan Password harus diisi!");
        return View();
    }

    // Step 1: Authenticate using IAuthService (abstraction hides Local/AD)
    var authResult = await _authService.AuthenticateAsync(email, password);

    if (!authResult.Success)
    {
        ModelState.AddModelError(string.Empty, authResult.ErrorMessage);
        return View();
    }

    // Step 2: User authenticated by IAuthService — find in DB
    var user = await _userManager.FindByEmailAsync(email);
    if (user == null)
    {
        // Auth succeeded but user not in DB → reject (HC pre-registers)
        ModelState.AddModelError(string.Empty, "Akun Anda belum terdaftar. Hubungi HC.");
        return View();
    }

    // Step 3: AD mode — sync FullName/Email from AuthResult
    if (_config.GetValue<bool>("Authentication:UseActiveDirectory", false))
    {
        bool changed = false;

        if (!string.IsNullOrEmpty(authResult.FullName) && authResult.FullName != user.FullName)
        {
            user.FullName = authResult.FullName;
            changed = true;
        }

        if (!string.IsNullOrEmpty(authResult.Email) && authResult.Email != user.Email)
        {
            user.Email = authResult.Email;
            changed = true;
        }

        if (changed)
        {
            await _userManager.UpdateAsync(user);
            // Log silently — no detailed logging per CONTEXT
        }
    }

    // Step 4: Create session
    await _signInManager.SignInAsync(user, rememberMe);

    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
    {
        return Redirect(returnUrl);
    }

    return RedirectToAction("Index", "Home");
}
```

**Source:** Pattern verified in LocalAuthService.AuthenticateAsync (Phase 71) and LdapAuthService.AuthenticateAsync (Phase 71); AccountController.Profile/Settings show user lookup and update patterns.

### Pattern 2: Dynamic Import Template (Conditional Columns)

**What:** DownloadImportTemplate GET action reads UseActiveDirectory config and generates Excel template with/without Password column accordingly.

**When to use:** ImportWorkers GET endpoint must serve different templates for AD vs Local mode.

**Example (Phase 72-03 implementation):**

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public IActionResult DownloadImportTemplate()
{
    var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);

    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("Import Workers");

    // Dynamic header list based on config
    var headers = new List<string>
    {
        "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit",
        "Directorate", "Role", "Tgl Bergabung (YYYY-MM-DD)"
    };

    if (!useAD) // Local mode only
    {
        headers.Add("Password");
    }

    // Write headers with styling
    for (int i = 0; i < headers.Count; i++)
    {
        ws.Cell(1, i + 1).Value = headers[i];
        ws.Cell(1, i + 1).Style.Font.Bold = true;
        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
        ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
    }

    // Example row (passwords omitted in AD mode)
    var example = new List<string>
    {
        "Ahmad Fauzi", "ahmad.fauzi@pertamina.com", "123456",
        "Operator", "RFCC", "RFCC LPG Treating Unit (062)",
        "CSU Process", "Coachee", "2024-01-15"
    };

    if (!useAD)
    {
        example.Add("Password123!");
    }

    for (int i = 0; i < example.Count; i++)
    {
        ws.Cell(2, i + 1).Value = example[i];
        ws.Cell(2, i + 1).Style.Font.Italic = true;
        ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.Gray;
    }

    // Hints
    ws.Cell(3, 1).Value = "Kolom Bagian: RFCC / DHT / HMU / NGP / GAST";
    ws.Cell(3, 1).Style.Font.Italic = true;
    ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

    if (useAD)
    {
        ws.Cell(4, 1).Value = "Password: Sistem akan otomatis membuat password acak untuk AD mode.";
        ws.Cell(4, 1).Style.Font.Italic = true;
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;
    }

    ws.Columns().AdjustToContents();

    using var stream = new System.IO.MemoryStream();
    workbook.SaveAs(stream);
    return File(stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "workers_import_template.xlsx");
}
```

**Source:** Phase 71 DownloadImportTemplate shows static template generation; ClosedXML v0.105.0+ in csproj.

### Pattern 3: ManageWorkers Form Adaptation (Conditional Field Visibility/Editability)

**What:** CreateWorker/EditWorker POST actions check UseActiveDirectory; if true, auto-generate password and skip user-provided value. View hides/disables password/email/fullname fields for AD users.

**When to use:** ManageWorkers create/edit endpoints and form views must adapt for AD mode.

**Example (Phase 72-02 partial implementation):**

```csharp
// In AdminController.CreateWorker POST
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateWorker(CreateWorkerViewModel model)
{
    // ... validation ...

    var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
    var password = useAD ? GenerateRandomPassword() : model.Password;

    var newUser = new ApplicationUser
    {
        UserName = model.Email,
        Email = model.Email,
        FullName = model.FullName,
        // ... other fields ...
    };

    var result = await _userManager.CreateAsync(newUser, password);
    // ... etc ...
}

// Password generation (Claude's discretion: can use Guid or SecureRandom)
private static string GenerateRandomPassword()
{
    // Option 1: Simple Guid-based (deterministic, fast)
    return Guid.NewGuid().ToString("N").Substring(0, 12);

    // Option 2: Crypto-random (more secure)
    // using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
    // {
    //     byte[] bytes = new byte[16];
    //     rng.GetBytes(bytes);
    //     return Convert.ToBase64String(bytes).Substring(0, 12);
    // }
}
```

**Source:** AdminController.CreateWorker/EditWorker patterns show existing form processing; IConfiguration GetValue pattern in Program.cs.

### Anti-Patterns to Avoid

- **Per-user auth routing (AuthSource field):** User decision locked to remove field entirely. DO NOT try to preserve per-user fallback logic.
- **Auto-provisioning AD users:** CONTEXT decision: user must be pre-registered. Reject "Akun Anda belum terdaftar. Hubungi HC." — no CreateAsync on login.
- **Profile sync with extra AD lookups:** Use AuthResult fields returned from IAuthService; don't make additional LDAP queries post-auth.
- **Local/AD fallback on LDAP timeout:** CONTEXT decision: no fallback. Return LDAP timeout error to user.
- **Revealing technical auth details:** Error messages use generic Indonesian strings only (LdapAuthService already enforces this).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Password generation for AD import | Custom alphabet-based randomizer | Guid.NewGuid().ToString() or SecureRandom | Simplicity, crypto quality, tested |
| Excel template generation with conditional columns | Manual cell-by-cell building | ClosedXML XLWorkbook with List<string> for dynamic headers | Already in project, reduces bugs |
| Config-based service registration | Reflection-based factory | Program.cs factory delegate with GetValue<bool> | Type-safe, clear conditional logic |
| LDAP user lookup | Raw DirectorySearcher calls post-auth | Return from AuthResult (already done in Phase 71) | Eliminates extra LDAP latency |
| Profile sync error handling | Suppress all exceptions silently | Try-catch with log warning, continue on failure | Debuggability without breaking login |

**Key insight:** Phase 71 completed the hardest parts (LDAP binding, RFC 4515 escaping, timeout handling). Phase 72 is primarily integration and UI adaptation. Don't re-invent auth logic; reuse IAuthService abstraction.

---

## Common Pitfalls

### Pitfall 1: Mixing AuthSource Field Removal with Login Logic

**What goes wrong:** Attempting to preserve AuthSource field for "fallback routing" during Phase 72 migration, creating confusion about which version uses which field.

**Why it happens:** Fear that removing the field breaks something. But CONTEXT locked decision: global config is the ONLY routing, so field is unused.

**How to avoid:**
1. Remove AuthSource from ApplicationUser model FIRST (or same commit as login rewrite)
2. Remove FROM DbContext HasMany/HasOne mappings
3. Single EF migration: DROP COLUMN AuthSource
4. Phase 72-01 handles all three (login POST rewrite + model + migration together)

**Warning signs:** Code checking `user.AuthSource` during login, or migration only dropping column without model update.

### Pitfall 2: Creating AD Users During Login ("Auto-Provisioning")

**What goes wrong:** Login POST calls `_userManager.CreateAsync` when AD auth succeeds but user not in DB. User gets auto-registered instead of rejected.

**Why it happens:** Desire for convenience — avoid HC manual registration step. But CONTEXT decision: HC pre-registers all users via ManageWorkers.

**How to avoid:**
```csharp
// CORRECT: Reject if not in DB
if (user == null)
{
    ModelState.AddModelError(string.Empty, "Akun Anda belum terdaftar. Hubungi HC.");
    return View();
}

// WRONG: Don't do this
if (user == null)
{
    var newUser = new ApplicationUser { ... };
    await _userManager.CreateAsync(newUser, ...);  // ← ANTI-PATTERN
}
```

**Warning signs:** Login POST contains _userManager.CreateAsync after auth.

### Pitfall 3: Profile Sync Overwriting with Null Values

**What goes wrong:** AD user returns null FullName → login sync sets user.FullName to null, wiping out HC-set data.

**Why it happens:** Not checking for null/empty before update. Treating AD as source-of-truth for all fields.

**How to avoid:**
```csharp
// CORRECT: Check before update
if (!string.IsNullOrEmpty(authResult.FullName))
{
    user.FullName = authResult.FullName;
}

// WRONG: Overwrites even if null
user.FullName = authResult.FullName;  // ← If null, user data lost
```

**Warning signs:** User data mysteriously clears after login.

### Pitfall 4: Exposing Technical LDAP/Auth Error Details to UI

**What goes wrong:** COMException HRESULT or timeout milliseconds shown on login page instead of generic Indonesian message.

**Why it happens:** Not catching exceptions in LdapAuthService (already done in Phase 71) or exposing AuthResult.ErrorMessage directly without sanitization.

**How to avoid:** LdapAuthService already returns user-safe strings. Verify in Phase 72-01 that AccountController uses authResult.ErrorMessage verbatim:

```csharp
// CORRECT: Use ErrorMessage from IAuthService
ModelState.AddModelError(string.Empty, authResult.ErrorMessage);

// WRONG: Expose COMException details
catch (COMException ex)
{
    ViewBag.Error = $"LDAP error: {ex.Message}";  // ← Leaks tech details
}
```

**Warning signs:** Login page shows "HRESULT 0x8007052E" or "timeout after 5000ms".

### Pitfall 5: Import Template Logic Not Matching ManageWorkers Password Handling

**What goes wrong:** Template generation and import POST don't agree on Password column presence. One has it, one doesn't, causing Excel column mismatch during import.

**Why it happens:** Forgetting to apply same UseActiveDirectory check to both DownloadImportTemplate GET and ImportWorkers POST.

**How to avoid:**
1. Both endpoints read same config: `_config.GetValue<bool>("Authentication:UseActiveDirectory", false)`
2. Both build header list the same way (see Pattern 2 code)
3. Import POST also checks: if AD mode, generate password; if local mode, read from column
4. Verify example data in template template matches what import logic expects

**Warning signs:** Template has Password column but import POST tries to read column 10 and gets NIP instead.

---

## Code Examples

Verified patterns from official sources:

### IAuthService Injection into AccountController

**Source:** Program.cs lines 52-74 (Phase 71), ASP.NET Core DI docs

```csharp
// Program.cs already does this (Phase 71)
var useActiveDirectory = builder.Configuration.GetValue<bool>("Authentication:UseActiveDirectory", false);
if (useActiveDirectory)
{
    builder.Services.AddScoped<IAuthService>(sp =>
        new LdapAuthService(
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ILogger<LdapAuthService>>()
        )
    );
}
else
{
    builder.Services.AddScoped<IAuthService>(sp =>
        new LocalAuthService(
            sp.GetRequiredService<SignInManager<ApplicationUser>>(),
            sp.GetRequiredService<ILogger<LocalAuthService>>()
        )
    );
}

// AccountController constructor (Phase 72-01 NEW)
public AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuthService authService,        // ← NEW: injected by DI
    IConfiguration config)            // ← NEW: for config checks
{
    _userManager = userManager;
    _signInManager = signInManager;
    _authService = authService;
    _config = config;
}
```

### Login POST Using IAuthService

**Source:** Phase 71 LocalAuthService.AuthenticateAsync & LdapAuthService.AuthenticateAsync, standard ASP.NET Core pattern

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        ModelState.AddModelError(string.Empty, "Email dan Password harus diisi!");
        return View();
    }

    // Use IAuthService abstraction (works for Local or AD, determined by DI)
    var authResult = await _authService.AuthenticateAsync(email, password);

    if (!authResult.Success)
    {
        ModelState.AddModelError(string.Empty, authResult.ErrorMessage);
        return View();
    }

    // Find user in DB (HC must pre-register)
    var user = await _userManager.FindByEmailAsync(email);
    if (user == null)
    {
        ModelState.AddModelError(string.Empty, "Akun Anda belum terdaftar. Hubungi HC.");
        return View();
    }

    // AD mode: sync profile fields
    var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);
    if (useAD && authResult.FullName != null && authResult.FullName != user.FullName)
    {
        user.FullName = authResult.FullName;
        await _userManager.UpdateAsync(user);
    }

    // Also sync Email if different (rare but possible)
    if (useAD && authResult.Email != null && authResult.Email != user.Email)
    {
        user.Email = authResult.Email;
        await _userManager.UpdateAsync(user);
    }

    // Create session
    await _signInManager.SignInAsync(user, rememberMe);

    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
    {
        return Redirect(returnUrl);
    }

    return RedirectToAction("Index", "Home");
}
```

### Dynamic Import Template Generation

**Source:** ClosedXML v0.105.0 XLWorkbook API, Phase 71 Program.cs factory pattern, AdminController.DownloadImportTemplate lines 3209-3245

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public IActionResult DownloadImportTemplate()
{
    var useAD = _config.GetValue<bool>("Authentication:UseActiveDirectory", false);

    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("Import Workers");

    var headers = new List<string>
    {
        "Nama", "Email", "NIP", "Jabatan", "Bagian", "Unit", "Directorate", "Role", "Tgl Bergabung (YYYY-MM-DD)"
    };

    if (!useAD)
    {
        headers.Add("Password");
    }

    // Write headers
    for (int i = 0; i < headers.Count; i++)
    {
        var cell = ws.Cell(1, i + 1);
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
        cell.Style.Font.FontColor = XLColor.White;
    }

    // Example row
    var example = new List<object>
    {
        "Ahmad Fauzi", "ahmad.fauzi@pertamina.com", "123456", "Operator",
        "RFCC", "RFCC LPG Treating Unit (062)", "CSU Process", "Coachee", "2024-01-15"
    };

    if (!useAD)
    {
        example.Add("Password123!");
    }

    for (int i = 0; i < example.Count; i++)
    {
        var cell = ws.Cell(2, i + 1);
        cell.Value = example[i];
        cell.Style.Font.Italic = true;
        cell.Style.Font.FontColor = XLColor.Gray;
    }

    // Instructions
    ws.Cell(3, 1).Value = "Kolom Bagian: RFCC / DHT / HMU / NGP / GAST";
    ws.Cell(3, 1).Style.Font.Italic = true;
    ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

    if (useAD)
    {
        ws.Cell(4, 1).Value = "Password: Otomatis dibuat sistem untuk AD mode.";
        ws.Cell(4, 1).Style.Font.Italic = true;
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;
    }

    ws.Columns().AdjustToContents();

    using var stream = new System.IO.MemoryStream();
    workbook.SaveAs(stream);
    return File(stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "workers_import_template.xlsx");
}
```

### AuthResult DTO (Already Implemented Phase 71)

**Source:** AuthResult.cs (Phase 71)

```csharp
public class AuthResult
{
    public bool Success { get; set; }
    public string? UserId { get; set; }        // Not used by Phase 72 (lookup in DB instead)
    public string? Email { get; set; }         // From DB or AD mail attribute
    public string? FullName { get; set; }      // From DB or AD displayName attribute
    public string? ErrorMessage { get; set; }  // User-safe message if Success=false
}
```

---

## State of the Art

| Approach | Timeline | Current Status |
|----------|----------|-----------------|
| Per-user AuthSource field + conditional routing | Phase 71 (created) → Phase 72 (removed) | PHASED OUT — global config replaces it |
| Direct PasswordSignInAsync in login POST | Current (pre-Phase-72) | REPLACED by IAuthService.AuthenticateAsync |
| Static import template | Current | REPLACED by config-aware dynamic generation |
| Password required for all users | Current | CHANGED — optional in AD mode (auto-generated) |

### Deprecated/Outdated

- **Per-user AuthSource field:** Only needed if users could switch auth modes independently. Global config provides simpler, more secure routing.
- **PasswordSignInAsync in AccountController:** Couples controller to Identity API. IAuthService abstraction decouples and enables auth swapping.
- **Static import template:** Cannot adapt for different auth modes without code change + redeploy.

---

## Open Questions

None identified. Phase 71 provided all foundation; CONTEXT.md constraints are clear; implementation patterns are straightforward.

---

## Validation Architecture

**Testing Framework:** xUnit (detected in test projects via csproj)

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| **AUTH-05** | Login page shows hint for AD mode, no hint for local | integration | `dotnet test` on Controllers test project | ❌ Wave 0 |
| **AUTH-05** | Email + Password fields present both modes, visually identical | manual UI | Manual browser check | N/A |
| **AUTH-06** | AD user not in DB → rejected with "Akun Anda belum terdaftar..." | unit | Mock IAuthService success + null user lookup | ❌ Wave 0 |
| **AUTH-06** | Local user not in DB → same rejection | unit | Mock LocalAuthService null user | ❌ Wave 0 |
| **AUTH-07** | Profile sync updates FullName/Email if provided in AuthResult | unit | Mock AuthResult with FullName → verify user.FullName updated | ❌ Wave 0 |
| **AUTH-07** | Profile sync skips null values, doesn't overwrite with null | unit | Mock AuthResult.FullName=null → verify user.FullName unchanged | ❌ Wave 0 |
| **AUTH-07** | Role/SelectedView never modified during login | unit | Pre-set role, auth, verify unchanged | ❌ Wave 0 |
| Dynamic Import | AD mode template missing Password column | integration | Mock config AD=true, assert header count matches | ❌ Wave 0 |
| Dynamic Import | Local mode template includes Password column | integration | Mock config AD=false, assert header count matches | ❌ Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test --filter "Category=Auth" -x` — quick auth tests only
- **Per wave merge:** `dotnet test` — full suite
- **Phase gate:** Full suite green + manual UI verification (login page hint rendering) before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `Tests/Controllers/AccountControllerTests.cs` — covers AUTH-05, AUTH-06, AUTH-07 (mock IAuthService, null user, profile sync scenarios)
- [ ] `Tests/Integration/ImportTemplateTests.cs` — covers dynamic template generation (mock config, assert headers)
- [ ] Test fixtures for: MockIAuthService, ApplicationUser seeding, IConfiguration mocks
- [ ] xUnit setup: ensure existing test infrastructure compatible

*(Create tests as part of implementation — treat test code as integral to Phase 72-01 controller task.)*

---

## Sources

### Primary (HIGH confidence)

- **Program.cs lines 52-74** — Phase 71 IAuthService DI factory registration verified in codebase
- **Services/IAuthService.cs** — Interface spec verified (Phase 71)
- **Services/LocalAuthService.cs** — Pattern for CheckPasswordSignInAsync verified (Phase 71)
- **Services/LdapAuthService.cs** — LDAP auth with error handling verified (Phase 71), RFC 4515 escaping confirmed
- **Controllers/AdminController.cs lines 3209-3245** — DownloadImportTemplate pattern verified
- **Models/ApplicationUser.cs** — Current fields confirmed (NIP, Position, Section, etc.)
- **ASP.NET Core 8 Documentation** — Dependency Injection, IConfiguration, SignInManager patterns (standard framework)

### Secondary (MEDIUM confidence)

- **ClosedXML 0.105.0 API** — XLWorkbook, cell styling verified in project usage (Phase 69 export)
- **System.DirectoryServices** — DirectoryEntry, DirectorySearcher confirmed in Phase 71 LdapAuthService
- **Microsoft.AspNetCore.Identity** — UserManager, SignInManager patterns confirmed in existing AccountController

### Tertiary (LOW confidence)

None — all critical patterns verified by implemented code or official docs.

---

## Metadata

**Confidence breakdown:**
- **Standard Stack:** HIGH — Phase 71 foundation verified; all libraries in project
- **Architecture:** HIGH — IAuthService abstraction tested; patterns established in Phase 71
- **Pitfalls:** HIGH — CONTEXT.md constraints eliminate ambiguity; anti-patterns identified
- **Code Examples:** HIGH — Sourced from Phase 71 implementations and existing AdminController

**Research date:** 2026-02-28
**Valid until:** 2026-03-28 (Phase 71 stable; authentication patterns unlikely to change; 30-day validity)

---

*Phase 72 research complete. Planner can proceed to task creation.*
