# Phase 73: User Structure Polish - Research

**Researched:** 2026-02-28
**Domain:** User structure finalization & code cleanup
**Confidence:** HIGH

## Summary

Phase 73 is a cleanup/finalization phase that standardizes the codebase after the dual-auth infrastructure (Phases 71-72) was fully implemented. The work involves three focused tasks:

1. **GetDefaultView() sweep**: Controllers already use `UserRoles.GetDefaultView()` (3 call sites in AdminController); SeedData still hardcodes SelectedView strings — replace all hardcoded instances with GetDefaultView() calls
2. **SeedData modernization**: Update seed user definitions, including Rino (Admin role changed to dual Admin+Coachee), and remove AuthSource references that were deleted in Phase 72
3. **ARCHITECTURE.md dual-auth documentation**: Add sections explaining dual-auth architecture for both developers (code flow, interfaces) and operations team (config setup, switching between Local/AD modes)

The phase requires **no new libraries, no new code patterns, no database migrations**. It is purely refactoring and documentation work. Success means consistent role-to-view mapping throughout the codebase and clear operational guidance for deploying dual-auth in production.

**Primary recommendation:** Treat this as a three-part cleanup task: (1) SeedData SelectedView refactor using GetDefaultView(), (2) ARCHITECTURE.md dual-auth sections (dev + ops), (3) verification that no other hardcoded SelectedView instances remain in the codebase.

<user_constraints>

## User Constraints (from CONTEXT.md)

### Locked Decisions

#### Documentation Content (ARCHITECTURE.md)
- **Audience:** Both developers AND ops/deployment team — separate sections for each
- **Detail level:** Medium — developer section covers code flow and interfaces; ops section covers config and setup
- **Include architectural decisions:** Why AuthSource was removed, why global config toggle (vs per-user flag), etc.
- **Language:** English — consistent with existing ARCHITECTURE.md
- **Diagrams:** Text-based (ASCII/Mermaid in Markdown) — exactly 2 required:
  1. Login flow diagram (user login → config check → Local/AD → result)
  2. Service architecture diagram (IAuthService → LocalAuthService / LdapAuthService)
- **Location:** Update existing `.planning/codebase/ARCHITECTURE.md`

#### SeedData Modernization
- **Replace all hardcoded SelectedView** with `GetDefaultView(role)` — computed from role after user creation, NOT manually set in constructor
- **Rino (Admin) changes:**
  - Position: "System Administrator" → "Operator"
  - Role addition: Dual role (Admin + Coachee) via AddToRoleAsync
  - Section/Unit remain null (Admin has full access)
  - SelectedView: Uses role "tertinggi" (highest, Admin = level 1) via GetDefaultView
- **Rustam (Coach) changes:**
  - Position: "Coach" → "Shift Supervisor" (Coach is a ROLE, not a position)
- **All other seed users remain unchanged** (9 total users, all roles covered)
- **Password "123456" stays** for development
- **For dual-role users:** SelectedView uses "highest role level" — actual "choose view at login" deferred to Phase 74

#### GetDefaultView() Sweep Scope
- **Full codebase sweep** — find ALL places that hardcode SelectedView strings
- **Replace with GetDefaultView()** at call sites
- **Controllers already compliant:** 3 call sites in AdminController (CreateWorker POST, EditWorker POST, ImportWorkers POST)
- **SeedData is main remaining location** — expect this to be the bulk of replacements
- **Login flow:** Do NOT reset SelectedView — login uses existing DB value; GetDefaultView() only for create/edit user operations
- **Edge cases:** Unknown role → default "Coachee" (least privilege, already implemented)
- **User without role:** Treated as Coachee (consistent with default)

### Claude's Discretion
- Exact placement of dual auth section in ARCHITECTURE.md (before or after existing sections)
- Mermaid vs ASCII format for diagrams (user doesn't have strong preference)
- How SeedData computes SelectedView post-creation (loop after AddToRoleAsync, or separate step) — recommend post-creation step for clarity

### Deferred Ideas (OUT OF SCOPE)

#### Phase 74: Role & Access Restructuring (new phase)
1. **Mandatory dual role for Admin** — Admin role always requires second "real" role; UI changes in CreateWorker/EditWorker needed
2. **RoleLevel restructuring** — SectionHead should be level 3 (full access, see all sections) not level 4
3. **Fix upload evidence access** — RoleLevel <= 5 check allows SrSupervisor to upload evidence; only Coach should
4. **Choose view at login** — Dual-role users select which view at login (Admin vs Coachee); requires login UI changes

</user_constraints>

<phase_requirements>

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| USTR-02 | Role-to-SelectedView mapping extracted to shared helper UserRoles.GetDefaultView() | GetDefaultView() already implemented in UserRoles.cs (lines 74-84); called 3x in AdminController (CreateWorker POST line 2785, EditWorker POST line 2934, ImportWorkers POST line 3352). SeedData still uses hardcoded string literals — this phase replaces all 9 user definitions with GetDefaultView() calls. |

</phase_requirements>

## Standard Stack

### Core Technologies
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Identity | 7.x | User management, role assignments | Platform-provided, integrated into all auth flows |
| Entity Framework Core | 7.x | Database persistence | Platform-provided, all user data persists via DbContext |
| C# | 11 | Language | Team standard, static helpers (UserRoles.cs) use modern pattern matching |

### Supporting Libraries
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ClosedXML | 0.105.0 | Excel import (existing, unchanged in Phase 73) | Already present in codebase; Excel template download in ImportWorkers |

### No New Dependencies Required
Phase 73 uses **only existing libraries**. No NuGet packages to add or update.

## Architecture Patterns

### Current Pattern: GetDefaultView() Static Helper

**What:** Centralized role-to-view mapping in static helper method `UserRoles.GetDefaultView(string roleName)`

**Location:** `Models/UserRoles.cs` lines 74-84

**Current Implementation:**
```csharp
/// <summary>
/// Get the default SelectedView for a given role name.
/// Mapping: Admin→"Admin", HC→"HC", Coach→"Coach", management roles→"Atasan", default→"Coachee"
/// </summary>
public static string GetDefaultView(string roleName)
{
    return roleName switch
    {
        Admin => "Admin",
        HC => "HC",
        Coach => "Coach",
        Direktur or VP or Manager or SectionHead or SrSupervisor => "Atasan",
        _ => "Coachee"
    };
}
```

**When to use:** Whenever creating a new user or updating a user's role — set `user.SelectedView = UserRoles.GetDefaultView(userRole)` immediately after role assignment

**Why this pattern works:**
- Single source of truth for role→view mapping
- Consistent across all user creation paths (CreateWorker, EditWorker, ImportWorkers, SeedData)
- Easy to modify if view mapping rules change (Phase 74+)
- No hardcoded view strings scattered through codebase

### Current Usage in Controllers

**Location:** `Controllers/AdminController.cs`

All three user management POST actions already use GetDefaultView():

1. **CreateWorker POST (line 2785):**
   ```csharp
   var selectedView = UserRoles.GetDefaultView(model.Role);
   var user = new ApplicationUser { ..., SelectedView = selectedView };
   ```

2. **EditWorker POST (line 2934):** (role change branch)
   ```csharp
   user.SelectedView = UserRoles.GetDefaultView(model.Role);
   ```

3. **ImportWorkers POST (line 3352):**
   ```csharp
   var selectedView = UserRoles.GetDefaultView(role);
   var newUser = new ApplicationUser { ..., SelectedView = selectedView };
   ```

### Remaining Hardcoded References

**Location:** `Data/SeedData.cs`

All 9 seed users still hardcode SelectedView directly in constructor:

```csharp
// Current (9 instances)
SelectedView = "Admin"  // Rino
SelectedView = "HC"     // Meylisa
SelectedView = "Atasan" // Direktur, VP, Manager, SectionHead, SrSupervisor
SelectedView = "Coach"  // Rustam
SelectedView = "Coachee" // Iwan
```

**Phase 73 task:** Replace all 9 with `UserRoles.GetDefaultView(role)` — allows SeedData to get view from UserRoles constant, not hardcoded string.

### No New Patterns Required
Phase 73 does **not** introduce new patterns. It standardizes existing GetDefaultView() usage already established in Phase 69 and Phase 72 (see STATE.md line 511, 532).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| String-based role→view mapping | Ad hoc switch statements scattered across codebase | UserRoles.GetDefaultView() static helper | Single source of truth; maintainability; no duplication; existing implementation verified in 3 controller call sites |
| Seed data inconsistency | Manual SelectedView strings hardcoded in user constructors | GetDefaultView() computed at seed time | Eliminates role-to-view mapping duplication; ensures consistency with CreateWorker/EditWorker/ImportWorkers |

**Key insight:** Hardcoded view strings in SeedData become out-of-sync with controller logic when role mapping rules change. Static helper ensures single definition of "which view for which role?"

## Common Pitfalls

### Pitfall 1: SeedData Creates User, Then Updates SelectedView

**What goes wrong:** Code like this fails — SelectedView set AFTER AddToRoleAsync, so role-level defaults don't apply:
```csharp
var user = new ApplicationUser { FullName = "Rino", SelectedView = "Admin" };
await userManager.CreateAsync(user, password);
await userManager.AddToRoleAsync(user, UserRoles.Admin); // Role added AFTER view set
```

**Why it happens:** Misunderstanding about when SelectedView should be determined (at user creation vs after role assignment)

**How to avoid:** Set SelectedView **immediately before CreateAsync**, computed from the role that will be assigned:
```csharp
var role = UserRoles.Admin;
var user = new ApplicationUser
{
    FullName = "Rino",
    SelectedView = UserRoles.GetDefaultView(role)  // Determined from role
};
await userManager.CreateAsync(user, password);
await userManager.AddToRoleAsync(user, role);
```

**Warning signs:** Test login as different roles → verify SelectedView matches expected view in session

### Pitfall 2: Forgetting AuthSource References in Older Code

**What goes wrong:** AuthSource field was removed from ApplicationUser in Phase 72 (via migration 20260228113655_RemoveAuthSourceField.cs). Remaining code still tries to access `user.AuthSource`:
```csharp
if (user.AuthSource == "AD") { /* ... */ }  // RUNTIME ERROR — AuthSource is gone
```

**Why it happens:** Search-and-replace not complete; some code paths missed

**How to avoid:** SeedData review — verify no AuthSource references remain. Migration Down() left empty (cannot reverse data deletion), so if code still references it, runtime errors will occur.

**Warning signs:** Runtime errors like "Object has no property 'AuthSource'" during user creation or login

### Pitfall 3: Dual-Role User View Selection Before Phase 74

**What goes wrong:** Rino (Admin + Coachee dual role) tries to use both "Admin" and "Coachee" views, but SelectedView can only be one string. Code tries to switch dynamically:
```csharp
// WRONG — SelectedView is singular, not array
var view = user.Roles.Contains("Admin") ? "Admin" : "Coachee";
```

**Why it happens:** Confusion about "which view should a dual-role user see?"

**How to avoid:** For Phase 73, use "highest role level" — `GetDefaultView()` returns view for highest-priority role (Admin=level 1, Coachee=level 6). "Choose view at login" UI is Phase 74 work.

**Warning signs:** Phase 73 verification shows Rino with SelectedView="Admin" (correct); Phase 74 will add UI to switch between Admin and Coachee views

## Code Examples

Verified patterns from official sources:

### Pattern 1: Create User with Role-Based View (AdminController)

**Source:** AdminController.cs CreateWorker POST (lines 2785-2801)

```csharp
var roleLevel = UserRoles.GetRoleLevel(model.Role);
var selectedView = UserRoles.GetDefaultView(model.Role);  // Computed from role

var user = new ApplicationUser
{
    UserName = model.Email,
    Email = model.Email,
    EmailConfirmed = true,
    FullName = model.FullName,
    // ... other fields
    RoleLevel = roleLevel,
    SelectedView = selectedView  // Set from GetDefaultView()
};

var password = useAD ? GenerateRandomPassword() : model.Password!;
var result = await _userManager.CreateAsync(user, password);
if (result.Succeeded)
{
    await _userManager.AddToRoleAsync(user, model.Role);  // Role assigned after
    // ... audit log
}
```

**Key point:** SelectedView determined before CreateAsync, from role that will be assigned

### Pattern 2: Update User Role and View (AdminController)

**Source:** AdminController.cs EditWorker POST (lines 2930-2934)

```csharp
if (currentRole != model.Role)
{
    if (currentRole != null)
        await _userManager.RemoveFromRoleAsync(user, currentRole);

    await _userManager.AddToRoleAsync(user, model.Role);

    var newRoleLevel = UserRoles.GetRoleLevel(model.Role);
    user.RoleLevel = newRoleLevel;

    // Update SelectedView based on new role
    user.SelectedView = UserRoles.GetDefaultView(model.Role);  // Recompute when role changes

    changes.Add($"Role: '{currentRole}' → '{model.Role}'");
}

var updateResult = await _userManager.UpdateAsync(user);
```

**Key point:** When role changes, recompute SelectedView from new role

### Pattern 3: Bulk Import with Role-Based View (AdminController)

**Source:** AdminController.cs ImportWorkers POST (lines 3351-3368)

```csharp
var roleLevel = UserRoles.GetRoleLevel(role);
var selectedView = UserRoles.GetDefaultView(role);  // Computed from role string from Excel

var newUser = new ApplicationUser
{
    UserName = email,
    Email = email,
    EmailConfirmed = true,
    FullName = nama,
    // ... other fields from Excel row
    RoleLevel = roleLevel,
    SelectedView = selectedView  // Set from GetDefaultView()
};

var createResult = await _userManager.CreateAsync(newUser, password);
if (createResult.Succeeded)
{
    await _userManager.AddToRoleAsync(newUser, role);
    result.Status = "Success";
}
```

**Key point:** Same pattern works for bulk import — compute view from role in the loop

### Pattern 4: SeedData Should Follow Same Pattern (TARGET for Phase 73)

**Current code (SeedData.cs lines 40-51):**
```csharp
(new ApplicationUser
{
    UserName = "rino.prasetyo@pertamina.com",
    Email = "rino.prasetyo@pertamina.com",
    EmailConfirmed = true,
    FullName = "Rino",
    Position = "System Administrator",  // Will change to "Operator"
    Section = null,
    Unit = null,
    RoleLevel = 1,
    SelectedView = "Admin"  // HARDCODED — should use GetDefaultView(UserRoles.Admin)
}, "123456", UserRoles.Admin),
```

**Phase 73 refactor:**
```csharp
(new ApplicationUser
{
    UserName = "rino.prasetyo@pertamina.com",
    Email = "rino.prasetyo@pertamina.com",
    EmailConfirmed = true,
    FullName = "Rino",
    Position = "Operator",  // Updated per CONTEXT decision
    Section = null,
    Unit = null,
    RoleLevel = UserRoles.GetRoleLevel(UserRoles.Admin),
    SelectedView = UserRoles.GetDefaultView(UserRoles.Admin)  // Computed from role
}, "123456", UserRoles.Admin),
```

**Apply to all 9 seed users** — each gets SelectedView = GetDefaultView(roleConstant)

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Per-user AuthSource field (ApplicationUser.AuthSource = "Local" or "AD") | Global config toggle (Authentication:UseActiveDirectory in appsettings.json) | Phase 72 (2026-02-28) | Simplified auth routing; no per-user migration needed; config-driven at app startup |
| Inline switch statements for role→view mapping (3 duplicates in CreateWorker/EditWorker/ImportWorkers) | Centralized UserRoles.GetDefaultView() static helper | Phase 69 (2026-02-27) | Single source of truth; easier to maintain; prevents duplication |
| Hardcoded SeedData view assignments | [PENDING Phase 73] Replace with GetDefaultView() calls | Phase 73 (current) | Consistency with controller pattern; eliminates manual string errors |

**Deprecated/outdated:**
- **AuthSource field:** Removed in Phase 72 migration 20260228113655. Rationale: global config at startup simpler than per-user tracking; LDAP config already specifies auth mode; per-user field introduced unnecessary state.
- **Manual role→view mapping:** Phase 69 centralized to GetDefaultView(); inline switches removed from 3 controller action POST methods. Rationale: single definition prevents divergence when rules change.

## Open Questions

1. **Dual-role user view selection timing**
   - What we know: Rino gets dual roles (Admin + Coachee) via Phase 72 design; SelectedView is singular; GetDefaultView() uses "highest level" (Admin=1, Coachee=6 → Admin wins)
   - What's unclear: When user logs in with multiple roles, should they see a "choose your view" prompt before dashboard, or is SelectedView fixed?
   - Recommendation: Phase 73 keeps SelectedView="Admin" (highest role); Phase 74 adds login UI for dual-role users to select view dynamically. CONTEXT confirms this as deferred.

2. **ARCHITECTURE.md dual-auth section detail level**
   - What we know: Audience is dev + ops; separate sections locked in; 2 diagrams required
   - What's unclear: Exact depth of config details (e.g., full LDAP.DirectoryEntry code, or just OU path + samaccountname?)
   - Recommendation: Dev section: code interfaces and flow (IAuthService, LocalAuthService, LdapAuthService, AccountController.Login integration). Ops section: config setup (appsettings.json toggle, LDAP OU path, testing auth modes). Reference actual codebase for code examples.

3. **AuthSource references scan completeness**
   - What we know: Field removed from model and database in Phase 72 migration
   - What's unclear: Are there any remaining code paths that try to access user.AuthSource (besides SeedData)?
   - Recommendation: Grep codebase for "AuthSource" — expect 0 matches in *.cs files (only in migration history). Verify login flow doesn't reference it.

## Validation Architecture

No test infrastructure changes required. Phase 73 is code organization (SeedData refactor) + documentation (ARCHITECTURE.md). Verification is manual:

- **Manual verification steps for Phase 73-01:**
  1. SeedData: grep for hardcoded SelectedView strings — should find 0 after refactor
  2. SeedData: Run `dotnet ef database drop --force && dotnet ef database update` — seed runs, check user.SelectedView values match roles
  3. ARCHITECTURE.md: Verify 2 diagrams present (login flow + service architecture)
  4. ARCHITECTURE.md: Dev section explains IAuthService, LocalAuthService, LdapAuthService, integration with AccountController.Login
  5. ARCHITECTURE.md: Ops section explains config toggle, LDAP setup, switching between modes
  6. Codebase: `grep -r "AuthSource" --include="*.cs"` — expect 0 matches in non-migration files
  7. Codebase: `grep -r "SelectedView.*=" --include="*.cs"` — expect matches only in GetDefaultView() calls, SeedData GetDefaultView() calls, and ApplicationUser property definition

**No automated test additions needed** — SeedData changes are validated by manual seed run; documentation is reviewed for completeness.

## Sources

### Primary (HIGH confidence)
- **UserRoles.cs** (Models/UserRoles.cs) - GetDefaultView() implementation and role constants verified
- **AdminController.cs** (Controllers/AdminController.cs) - 3 controller action implementations using GetDefaultView() verified
- **SeedData.cs** (Data/SeedData.cs) - All 9 seed user definitions reviewed; all use hardcoded SelectedView strings
- **CONTEXT.md** (Phase 73) - User decisions on Rino role changes, SeedData modernization, and ARCHITECTURE.md requirements
- **REQUIREMENTS.md** (USTR-02) - Requirement definition: "Role-to-SelectedView mapping extracted to shared helper UserRoles.GetDefaultView()"
- **ARCHITECTURE.md** (existing) - Current documentation structure verified; dual-auth section additions needed

### Secondary (MEDIUM confidence)
- **Phase 72 decisions** (STATE.md) - AuthSource removal context and global config routing pattern verified
- **Phase 69 decisions** (STATE.md) - GetDefaultView() extraction history and pattern established

## Metadata

**Confidence breakdown:**
- **Standard stack:** HIGH - No new libraries, only existing ASP.NET Core Identity, EF Core, C# static helpers
- **Architecture:** HIGH - GetDefaultView() pattern already implemented in 3 controller locations; SeedData just needs same pattern applied
- **Implementation scope:** HIGH - Clearly bounded: (1) SeedData replace 9 hardcoded strings + Rino/Rustam changes, (2) ARCHITECTURE.md dual-auth 2 sections + 2 diagrams, (3) verify no orphaned AuthSource or hardcoded SelectedView

**Research date:** 2026-02-28
**Valid until:** 2026-03-07 (7 days — code is stable, requirements locked in CONTEXT.md)
