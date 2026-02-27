# Phase 67: Dynamic Profile Page - Research

**Researched:** 2026-02-27
**Domain:** ASP.NET Core MVC Razor View — @Model binding, null-safe display, initials generation
**Confidence:** HIGH

## Summary

This phase is a pure Razor view rewrite. The controller (`AccountController.Profile()`) already fetches the `ApplicationUser` from the database, calls `GetRolesAsync()`, and passes the user as `return View(user)` with the role set in `ViewBag.UserRole`. The view currently ignores the model and shows hardcoded placeholder data. The task is to rewrite `Views/Account/Profile.cshtml` to declare `@model HcPortal.Models.ApplicationUser` and bind every displayed field to `@Model` properties, with null-safe em-dash fallback and dynamic initials matching the `_Layout.cshtml` algorithm.

No new NuGet packages, no controller changes, no migrations, no JavaScript — this is a single-file Razor view rewrite. All required data is already passed to the view by the existing controller action. The `_Layout.cshtml` already implements the exact initials algorithm needed; the profile page avatar must replicate that same logic inline.

**Primary recommendation:** Rewrite `Profile.cshtml` as a flat label-value layout with two sections (Identitas / Organisasi), no cards, using `@model ApplicationUser` and null-coalescing display helper pattern `@(Model.Property ?? "—")` with the em dash styled `text-muted`.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Field organization:**
- 2 sections with dividers: **Identitas** + **Organisasi**
- Identitas: Nama, NIP, Email, Phone (PhoneNumber from IdentityUser)
- Organisasi: Directorate, Section, Unit, Position, Role
- Layout: label-value rows with section heading (bold) + thin divider between sections — professional corporate style, no cards
- JoinDate skipped — not displayed on profile page

**Profile header:**
- Header area at top: large avatar initials circle + Nama Lengkap + Position subtitle
- Single fixed color for avatar background (primary/blue) — same for all users
- No badge — "Active Employee" badge removed; Role is in the fields below

**Empty field handling:**
- Null/empty fields display "—" (em dash) — not "Belum diisi", not blank
- Sections always shown even if all fields in section are null
- Em dash styled in muted color to differentiate from real data

**Field additions and removals:**
- Phone (IdentityUser.PhoneNumber) added to Identitas section
- Location removed — no field in ApplicationUser
- "Active Employee" status badge removed — hardcoded, no backing data
- Role value comes from UserManager.GetRolesAsync() — first role displayed (already in ViewBag.UserRole)

### Claude's Discretion
- Exact typography (font sizes, weights, spacing between rows)
- Section divider styling (border-bottom, hr, or spacing)
- Avatar circle size on profile page
- Responsive behavior on mobile

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| PROF-01 | Profile page menampilkan data real user login (Nama, NIP, Email, Position, Section, Unit, Directorate, Role, JoinDate) | Controller already passes `ApplicationUser` as `@Model`; all fields exist on the model. JoinDate excluded per CONTEXT decisions. Role via `ViewBag.UserRole`. |
| PROF-02 | Field kosong menampilkan placeholder "Belum diisi", bukan blank/error | CONTEXT overrides this to "—" (em dash). Null-safe pattern: `@(string.IsNullOrEmpty(Model.NIP) ? "—" : Model.NIP)` or `@(Model.NIP ?? "—")` for nullable strings. |
| PROF-03 | Avatar initials dinamis dari FullName user (bukan hardcoded "BS") | _Layout.cshtml already implements the algorithm; Profile page must replicate it inline using `@Model.FullName`. |
</phase_requirements>

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | Already in project | Razor view engine, `@model` directive, `@Model` binding | Project framework |
| Bootstrap 5.3 | CDN (already in _Layout) | Grid system, utility classes (text-muted, fw-bold) | Project UI framework |
| Bootstrap Icons | 1.10.0 (already in _Layout) | `bi-person-circle` and similar icons used throughout views | Project icon library |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.AspNetCore.Identity | Already in project | `IdentityUser.PhoneNumber` — inherited by ApplicationUser | Phone field source |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Inline null-coalescing `@(Model.NIP ?? "—")` | ProfileViewModel with pre-computed display strings | ViewModel adds a new class file for a simple view rewrite; inline null-coalescing is idiomatic Razor for this scope |
| Replicating initials logic inline | Helper method / HtmlHelper extension | Overkill for a single view; inline keeps it self-contained and consistent with how _Layout does it |

**Installation:** None required — no new packages.

## Architecture Patterns

### Recommended Project Structure

No structural changes — this phase modifies one existing file:

```
Views/
└── Account/
    └── Profile.cshtml    ← rewrite this file only
```

### Pattern 1: @model Directive + @Model Binding

**What:** Declare model type at top of view; bind all displayed values to `@Model` properties.

**When to use:** Any view that receives a typed object from the controller via `return View(object)`.

**Example (from ManageWorkers.cshtml — project precedent):**
```razor
@model List<HcPortal.Models.ApplicationUser>
@{
    var userRolesDict = ViewBag.UserRoles as Dictionary<string, string> ?? new Dictionary<string, string>();
}
```

For Profile page (single user, not a list):
```razor
@model HcPortal.Models.ApplicationUser
@{
    ViewData["Title"] = "Profil Saya";
    var userRole = ViewBag.UserRole as string ?? "—";

    // Initials — replicate _Layout.cshtml algorithm exactly
    var fullName = Model.FullName ?? "";
    var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var initials = nameParts.Length >= 2
        ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
        : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : "?");
}
```

### Pattern 2: Null-Safe Em Dash Display

**What:** Display field value or "—" (em dash) when null/empty. Style em dash in `text-muted` to visually distinguish from real data.

**When to use:** Every nullable `ApplicationUser` field displayed in the view.

**Example:**
```razor
@* For nullable string fields (NIP, Position, Section, Unit, Directorate) *@
<span>@(string.IsNullOrEmpty(Model.NIP) ? "—" : Model.NIP)</span>

@* For nullable but may-never-be-null strings (Email from IdentityUser) *@
<span>@(Model.Email ?? "—")</span>

@* For the em dash, apply muted styling conditionally *@
@{
    var nipDisplay = string.IsNullOrEmpty(Model.NIP) ? null : Model.NIP;
}
@if (nipDisplay == null)
{
    <span class="text-muted">—</span>
}
else
{
    <span>@nipDisplay</span>
}
```

Simpler inline version (preferred for this phase):
```razor
@{
    var nip = string.IsNullOrEmpty(Model.NIP) ? null : Model.NIP;
}
@if (nip != null) { <span>@nip</span> } else { <span class="text-muted">—</span> }
```

Or using a Razor helper variable per row — cleaner in flat-row layout:
```razor
<div class="row mb-2">
    <div class="col-4 fw-semibold text-muted small">NIP</div>
    <div class="col-8">
        @if (!string.IsNullOrEmpty(Model.NIP)) { @Model.NIP }
        else { <span class="text-muted">—</span> }
    </div>
</div>
```

### Pattern 3: Avatar Initials Circle (matching _Layout.cshtml)

**What:** Large circular div with primary background, white text, showing 2-letter initials from FullName.

**When to use:** Profile header area. Must use the same algorithm as _Layout.cshtml navbar avatar.

**Example (from _Layout.cshtml — authoritative source):**
```csharp
// In @{ } block:
var fullName = Model.FullName ?? "";
var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
var initials = nameParts.Length >= 2
    ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
    : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : "?");
```

```razor
<div class="bg-primary text-white rounded-circle d-flex justify-content-center align-items-center fw-bold"
     style="width: 80px; height: 80px; font-size: 2rem;">
    @initials
</div>
```

The size/font-size is at Claude's discretion — the original hardcoded avatar uses `fs-1` with 100×100px.

### Pattern 4: Two-Section Flat Label-Value Layout

**What:** No Bootstrap cards. Two sections separated by a bold heading + `<hr>`. Each field as a Bootstrap row with label col and value col.

**When to use:** Corporate/HRIS profile pages — clean, scannable, no visual noise from card borders.

**Example structure:**
```razor
<!-- Header -->
<div class="d-flex align-items-center mb-4 pb-3 border-bottom">
    <div class="... rounded-circle ...">@initials</div>
    <div class="ms-3">
        <h3 class="fw-bold mb-0">@Model.FullName</h3>
        <p class="text-muted mb-0">@(string.IsNullOrEmpty(Model.Position) ? "—" : Model.Position)</p>
    </div>
</div>

<!-- Section: Identitas -->
<p class="fw-bold text-uppercase small text-muted mb-2">Identitas</p>
<div class="row mb-2">
    <div class="col-sm-4 text-muted small">Nama</div>
    <div class="col-sm-8 fw-semibold">@(string.IsNullOrEmpty(Model.FullName) ? "—" : Model.FullName)</div>
</div>
... (NIP, Email, Telepon)

<hr class="my-3">

<!-- Section: Organisasi -->
<p class="fw-bold text-uppercase small text-muted mb-2">Organisasi</p>
... (Directorate, Section, Unit, Position, Role)
```

### Anti-Patterns to Avoid

- **Hardcoded strings in view:** Never leave "Budi Santoso", "759921", "+62 812 3456 7890" etc. — replace ALL with `@Model` bindings.
- **ViewBag for user fields:** The controller already passes `ApplicationUser` as `return View(user)` — use `@Model.FieldName`, not `ViewBag.SomeField` for model data. Only `ViewBag.UserRole` is needed (for the role from GetRolesAsync).
- **Blank instead of em dash:** Never leave an empty `<span></span>` or `<div></div>` for null fields — always show "—" with `text-muted`.
- **Different initials algorithm from _Layout:** The profile avatar must use the EXACT same algorithm (split on space, take first char of first two words, else first 2 chars of FullName, else "?").
- **Using IdentityUser.UserName for display name:** UserName is typically the email; use `Model.FullName` for display.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| User data fetching | Custom DB query in view | `@Model` — controller already provides it | Controller already calls `GetUserAsync` + `GetRolesAsync` |
| Initials computation | Custom JS or helper class | Inline Razor C# (same as _Layout pattern) | Already proven in _Layout.cshtml, no external dep needed |
| Null display | Complex helper extension | `string.IsNullOrEmpty()` ternary inline | Simple enough for direct Razor use |

## Common Pitfalls

### Pitfall 1: Missing @model Directive

**What goes wrong:** View compiles but `@Model` is dynamic and untyped; no IntelliSense; nullable reference checking bypassed; runtime cast errors possible.

**Why it happens:** Developer adds `@Model.FullName` without the `@model` directive at the top.

**How to avoid:** Line 1 of view MUST be `@model HcPortal.Models.ApplicationUser`.

**Warning signs:** `@Model` works but shows as `dynamic` in IDE; no property suggestions.

---

### Pitfall 2: PhoneNumber Is on IdentityUser, Not ApplicationUser

**What goes wrong:** Developer searches `ApplicationUser.cs` for `PhoneNumber` and doesn't find it, assumes it doesn't exist.

**Why it happens:** `PhoneNumber` is inherited from `IdentityUser` base class, not redeclared in `ApplicationUser`.

**How to avoid:** `@Model.PhoneNumber` works directly — it's inherited. No need to add it to `ApplicationUser.cs`.

**Warning signs:** Searching only `ApplicationUser.cs` for the field name.

---

### Pitfall 3: ViewBag.UserRole Not Cast Safely

**What goes wrong:** `ViewBag.UserRole` throws NullReferenceException if cast directly in view for a user with no roles.

**Why it happens:** Controller sets `ViewBag.UserRole = roles.FirstOrDefault() ?? "No Role"` — but the view must retrieve it with a safe cast.

**How to avoid:** `var userRole = ViewBag.UserRole as string ?? "—";` in the `@{ }` block.

**Warning signs:** Exception on the profile page for users without roles assigned.

---

### Pitfall 4: FullName Can Be Empty String (Not Null)

**What goes wrong:** `Model.FullName ?? "—"` shows empty string, not the em dash, because `FullName` defaults to `string.Empty` in `ApplicationUser`.

**Why it happens:** `ApplicationUser.FullName` is declared as `string FullName = string.Empty` — it is never null, but may be empty.

**How to avoid:** Use `string.IsNullOrEmpty(Model.FullName)` instead of null check alone.

**Warning signs:** Avatar initials show "?" for users with no FullName set; display shows blank instead of em dash.

---

### Pitfall 5: Initials Mismatch Between Navbar and Profile Page

**What goes wrong:** Profile page avatar shows different initials from the navbar avatar.

**Why it happens:** Developer writes a slightly different initials algorithm (e.g., takes first letter only, or uses UserName instead of FullName).

**How to avoid:** Copy the exact algorithm from `_Layout.cshtml` lines 8–12. Do not derive initials from `Model.UserName` or `Model.Email`.

**Warning signs:** Testing with a 3-word name like "Ahmad Budi Santoso" — navbar shows "AB", profile shows something else.

## Code Examples

### Complete @{ } Block for Profile.cshtml

```razor
@model HcPortal.Models.ApplicationUser
@{
    ViewData["Title"] = "Profil Saya";
    var userRole = ViewBag.UserRole as string ?? "—";

    // Avatar initials — same algorithm as _Layout.cshtml
    var fullName = string.IsNullOrEmpty(Model.FullName) ? "" : Model.FullName;
    var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var initials = nameParts.Length >= 2
        ? $"{nameParts[0][0]}{nameParts[1][0]}".ToUpper()
        : (fullName.Length >= 2 ? fullName.Substring(0, 2).ToUpper() : "?");

    // Null-safe display helpers
    string D(string? val) => string.IsNullOrEmpty(val) ? "—" : val;
}
```

Note: The local helper function `string D(string? val)` is valid in Razor `@{ }` blocks and cleans up repetitive ternary expressions in the markup. This is a local Razor function, not a C# method on the class.

### Null-Safe Row Helper Pattern

```razor
@* Reusable label-value row with em-dash fallback *@
@{
    void Row(string label, string? value)
    {
        var display = string.IsNullOrEmpty(value) ? null : value;
    }
}

<div class="row mb-2 py-1">
    <div class="col-sm-4 text-muted small fw-medium">NIP</div>
    <div class="col-sm-8">
        @if (!string.IsNullOrEmpty(Model.NIP)) { @Model.NIP }
        else { <span class="text-muted">—</span> }
    </div>
</div>
```

### Section Heading + Divider Pattern

```razor
<h6 class="fw-bold text-uppercase text-muted small mb-3 mt-4">Identitas</h6>

@* ... rows ... *@

<hr class="my-4">

<h6 class="fw-bold text-uppercase text-muted small mb-3">Organisasi</h6>

@* ... rows ... *@
```

### Role Display (from ViewBag)

```razor
@* Role from GetRolesAsync — passed as ViewBag.UserRole by controller *@
<div class="row mb-2 py-1">
    <div class="col-sm-4 text-muted small fw-medium">Role</div>
    <div class="col-sm-8">
        @{ var roleDisplay = ViewBag.UserRole as string; }
        @if (!string.IsNullOrEmpty(roleDisplay) && roleDisplay != "No Role")
        {
            <span>@roleDisplay</span>
        }
        else
        {
            <span class="text-muted">—</span>
        }
    </div>
</div>
```

### Complete Field Inventory

Fields from `ApplicationUser` to bind:

| Display Label | Source | Property Path | Nullable? |
|--------------|--------|---------------|-----------|
| Nama | ApplicationUser | `Model.FullName` | Never null (defaults to "") |
| NIP | ApplicationUser | `Model.NIP` | `string?` — use IsNullOrEmpty |
| Email | IdentityUser (base) | `Model.Email` | `string?` — use IsNullOrEmpty |
| Telepon | IdentityUser (base) | `Model.PhoneNumber` | `string?` — use IsNullOrEmpty |
| Directorate | ApplicationUser | `Model.Directorate` | `string?` — use IsNullOrEmpty |
| Bagian (Section) | ApplicationUser | `Model.Section` | `string?` — use IsNullOrEmpty |
| Unit | ApplicationUser | `Model.Unit` | `string?` — use IsNullOrEmpty |
| Jabatan (Position) | ApplicationUser | `Model.Position` | `string?` — use IsNullOrEmpty |
| Role | ViewBag | `ViewBag.UserRole as string` | Never null (controller sets "No Role" fallback) |

Fields NOT displayed (per CONTEXT decisions):
- `JoinDate` — skipped
- `Location` — no backing field in ApplicationUser
- "Active Employee" badge — removed

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Hardcoded placeholder data in Profile.cshtml | `@model ApplicationUser` + `@Model` binding | Phase 67 | Profile shows real user data |
| Hardcoded "BS" initials | Dynamic initials from `Model.FullName` (same algorithm as _Layout) | Phase 67 | Avatar matches navbar avatar |
| "Active Employee" badge | Removed — no backing data | Phase 67 | No false/hardcoded status shown |
| Location field shown | Removed — no field in ApplicationUser | Phase 67 | No empty/broken field |

## Open Questions

1. **Controller Auth Attribute**
   - What we know: `AccountController.Profile()` checks `User.Identity?.IsAuthenticated` manually and redirects to Login if false.
   - What's unclear: Whether to add `[Authorize]` attribute to the action or leave the manual redirect in place.
   - Recommendation: Leave as-is — this is not in scope for Phase 67. Do not touch the controller.

2. **"No Role" fallback string**
   - What we know: Controller sets `ViewBag.UserRole = roles.FirstOrDefault() ?? "No Role"`.
   - What's unclear: Should "No Role" display as the string or be converted to "—"?
   - Recommendation: Treat "No Role" as a valid display value since the controller explicitly sets it; OR treat it the same as a missing value and show "—". The CONTEXT says em dash for null/empty — "No Role" is not technically null/empty. Planner should decide and document in the plan. LOW risk either way.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | None detected — project has no automated test infrastructure |
| Config file | None |
| Quick run command | Manual browser test |
| Full suite command | Manual browser test |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PROF-01 | Profile page shows real user data from @Model | Manual smoke | Login as test user, navigate to /Account/Profile | N/A — manual only |
| PROF-02 | Null fields show "—" not blank | Manual smoke | Check user with missing NIP/Section/Unit/Position | N/A — manual only |
| PROF-03 | Avatar initials match FullName | Manual smoke | Verify initials match navbar avatar | N/A — manual only |

### Wave 0 Gaps

No test framework — all verification is manual browser testing. No Wave 0 test files needed.

## Sources

### Primary (HIGH confidence)

- `Views/Account/Profile.cshtml` (project file) — current state: hardcoded placeholder, no @model directive
- `Controllers/AccountController.cs` (project file) — Profile() action: passes `ApplicationUser` as view model, sets `ViewBag.UserRole`
- `Models/ApplicationUser.cs` (project file) — field inventory: FullName, NIP, Position, Section, Unit, Directorate, JoinDate (plus inherited IdentityUser.Email, PhoneNumber)
- `Views/Shared/_Layout.cshtml` (project file) — initials algorithm (lines 8–12), authoritative source for avatar pattern
- `Views/CMP/ManageWorkers.cshtml` (project file) — project precedent for `@model HcPortal.Models.ApplicationUser` usage
- `.planning/phases/67-dynamic-profile-page/67-CONTEXT.md` — locked user decisions on layout, fields, null handling

### Secondary (MEDIUM confidence)

- ASP.NET Core MVC Razor syntax — `@model` directive, `@Model` binding, `@{ }` blocks with local functions — well-established, no version uncertainty for this project's runtime

### Tertiary (LOW confidence)

None.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — project files examined directly; no new dependencies needed
- Architecture: HIGH — controller already passes the model; all fields verified in ApplicationUser/IdentityUser; _Layout.cshtml initials algorithm confirmed
- Pitfalls: HIGH — all pitfalls derived from reading actual project code (FullName defaults to "", PhoneNumber is on IdentityUser base, ViewBag.UserRole cast safety)

**Research date:** 2026-02-27
**Valid until:** 2026-03-27 (stable domain — pure Razor view rewrite with no external dependencies)
