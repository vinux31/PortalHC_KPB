# Phase 8: Fix Admin Role Switcher and Add Admin to Supported Roles - Research

**Researched:** 2026-02-18
**Domain:** ASP.NET Core MVC role simulation via DB-persisted SelectedView field
**Confidence:** HIGH (all findings from direct codebase reading)

## Summary

The Admin role-switcher is implemented as a `SelectedView` string field on `ApplicationUser` (persisted to the database). The Admin can switch to `"HC"`, `"Atasan"`, `"Coach"`, or `"Coachee"` views via `AccountController.SwitchView`. The mechanism works by reading `user.SelectedView` inside each controller action and applying different query logic accordingly. It is NOT session-based and NOT middleware-based — it is a DB column read on every request.

The switcher is broken in several specific places: some controller actions check `user.RoleLevel` or `User.IsInRole()` directly instead of consulting `SelectedView`, so those pages behave as if the Admin has their real role regardless of the selected view. Additionally, when Admin simulates Coach or HC views, `user.Section` and `user.Unit` are null (Admin's profile has no section/unit), which causes empty results in section-scoped queries.

Adding "Admin" to the supported view list requires adding `"Admin"` as an allowed value in `SwitchView`, adding it to the dropdown in `_Layout.cshtml`, and handling what "Admin view" means consistently (effectively a no-op that resets to full admin behavior — the existing paths already handle `userRole == UserRoles.Admin` without SelectedView override).

**Primary recommendation:** Add `"Admin"` to the `allowedViews` array in `AccountController.SwitchView`, add it to the layout dropdown, and fix the specific broken actions that use `user.RoleLevel` or `User.IsInRole()` instead of `SelectedView` for role-simulation logic.

---

## Architecture Patterns

### How the Switcher Works (As-Built)

The mechanism is a **DB-persisted view preference**:

```
User clicks dropdown in _Layout.cshtml
  -> GET /Account/SwitchView?view=HC&returnUrl=/CDP/Dashboard
  -> AccountController.SwitchView reads user, validates, sets user.SelectedView = "HC"
  -> _userManager.UpdateAsync(user) saves to DB
  -> Redirect back to returnUrl or Home
```

On every subsequent page request:
```
Controller action:
  var user = await _userManager.GetUserAsync(User);  // real Admin user
  var roles = await _userManager.GetRolesAsync(user); // real roles: ["Admin"]
  var userRole = roles.FirstOrDefault(); // = "Admin"

  // Then checks user.SelectedView to decide which branch to take
  if (userRole == UserRoles.Admin) {
      if (user.SelectedView == "HC") { /* show HC data */ }
      else if (user.SelectedView == "Coachee") { /* show personal data */ }
      ...
  }
```

Key invariants:
- `userRole` is ALWAYS `"Admin"` regardless of SelectedView (real Identity role)
- `user.RoleLevel` is ALWAYS `1` (Admin) regardless of SelectedView
- `user.Section` and `user.Unit` are null in the seeded Admin user (`admin@hcportal.com`)
- `User.IsInRole("Admin")` is ALWAYS true for the Admin user regardless of SelectedView

### Current Allowed Views

**In `AccountController.SwitchView` (line 138):**
```csharp
var allowedViews = new[] { "HC", "Atasan", "Coach", "Coachee" };
```

**In `_Layout.cshtml` dropdown (lines 103-144):** HC View, Atasan View, Coach View, Coachee View — no "Admin" option.

**In `ApplicationUser.SelectedView` default:** `"Coachee"` (but seed data sets Admin to `"HC"`).

### Roles and Levels

From `UserRoles.cs`:
| Role | RoleLevel | GetRoleLevel() |
|------|-----------|----------------|
| Admin | 1 (DB) | 1 |
| HC | 2 (DB) | 2 |
| Direktur, VP, Manager | 3 (DB) | 3 |
| Section Head, Sr Supervisor | 4 (DB) | 4 |
| Coach | 5 (DB) | 5 |
| Coachee | 6 (DB) | 6 |

SelectedView values map to:
| SelectedView | Simulates | Notes |
|---|---|---|
| `"HC"` | HC role (Level 2) | Full data access |
| `"Atasan"` | Manager/Section Head (Level 3-4) | Scoped by `user.Section` |
| `"Coach"` | Coach (Level 5) | Scoped by `user.Section` or `user.Id` |
| `"Coachee"` | Coachee (Level 6) | Personal data only |
| `"Admin"` (MISSING) | Admin (reset to real role) | Would be a no-op |

---

## Broken Pages — Complete Inventory

### Category A: SelectedView Is Completely Ignored

These actions don't check `user.SelectedView` at all and behave identically regardless of switcher state:

**CDPController.DevDashboard (lines 231-395)**
- Does NOT check `user.SelectedView`
- Always gives Admin full HC-level access (`scopedCoacheeIds = all coachees`)
- Result: "Dev Dashboard" always shows all coachees for Admin regardless of SelectedView
- Fix needed: When `SelectedView == "Coach"` or `"Coachee"` — Admin should see only coachees in their section/unit (or get forbidden). When `SelectedView == "HC"` or `"Admin"` — full access is correct.

**CDPController.ProtonMain (lines 594-631)**
- Authorization check: `if (user.RoleLevel > 5 && userRole != UserRoles.SrSupervisor)` — Admin always passes (RoleLevel=1)
- Data query: `_context.Users.Where(u => u.Section == user.Section && u.RoleLevel == 6)` — Admin has `Section = null`, so result is always empty
- Result: Admin in any SelectedView sees empty coachee list on ProtonMain
- Fix needed: When `SelectedView == "HC"`, show all coachees. When `SelectedView == "Coach"` or `"Atasan"`, need a section to scope to.

**CDPController.CreateSession [HttpPost] (lines 519-558)**
- Authorization: `if (user.RoleLevel > 5) return Forbid();` — Admin (RoleLevel=1) always passes
- Result: Admin can always create coaching sessions even in "Coachee" view
- Fix needed: When `SelectedView == "Coachee"`, block session creation.

**CDPController.UploadEvidence [HttpPost] (lines 1282-1358)**
- Authorization: `if (user.RoleLevel > 5) return Forbid();` — Admin always passes
- Result: Admin can always upload evidence even in "Coachee" view (which is actually fine since Admin-as-Coachee should be able to upload, but the semantic is slightly off)
- Fix: Low priority — Admin-as-Coachee uploading their own evidence is acceptable behavior.

**CDPController.HCApprovals (lines 1034-1126)**
- Authorization: `if (userRole != UserRoles.HC) return Forbid();` — blocks Admin entirely
- Result: Admin in "HC" view cannot access HC Approvals queue
- Fix needed: Allow access when `userRole == UserRoles.Admin && user.SelectedView == "HC"`.

**CDPController.HCReviewDeliverable [HttpPost] (lines 1000-1032)**
- Authorization: `if (userRole != UserRoles.HC) return Forbid();` — blocks Admin entirely
- Result: Admin in "HC" view cannot perform HC review
- Fix needed: Allow when `userRole == UserRoles.Admin && user.SelectedView == "HC"`.

**CDPController.CreateFinalAssessment GET+POST (lines 1128-1279)**
- Authorization: `if (userRole != UserRoles.HC) return Forbid();` — blocks Admin entirely
- Result: Admin in "HC" view cannot create final assessments
- Fix needed: Allow when `userRole == UserRoles.Admin && user.SelectedView == "HC"`.

**CDPController.Deliverable (lines 710-816)**
- `bool isHC = userRole == UserRoles.HC;` — Admin is never HC
- Result: Admin in "HC" view is treated as neither coachee nor HC — falls through to `Forbid()` if the deliverable doesn't belong to them
- Fix needed: `bool isHC = userRole == UserRoles.HC || (userRole == UserRoles.Admin && user.SelectedView == "HC");`

**CDPController.ApproveDeliverable [HttpPost] (lines 820-908)**
- Authorization: `if (userRole != UserRoles.SrSupervisor && userRole != UserRoles.SectionHead) return Forbid();`
- Result: Admin in "Atasan" view cannot approve deliverables
- Fix needed: Allow when `userRole == UserRoles.Admin && (user.SelectedView == "Atasan" || user.SelectedView == "HC")`.

**CDPController.RejectDeliverable [HttpPost] (lines 911-967)**
- Same pattern as ApproveDeliverable
- Fix needed: Same fix.

### Category B: SelectedView Is Checked But section/unit Is Null For Admin

These actions check `user.SelectedView` but then query using `user.Section` or `user.Unit` which are null for Admin:

**CDPController.Coaching (lines 397-514) — Coaching coachee list**
- `user.SelectedView` is checked correctly for query scoping
- But at line 475-482: `coacheeList = _context.Users.Where(u => u.Section == user.Section && u.RoleLevel == 6)` — Admin has `Section = null`
- Result: Even when Admin is in "Coach"/"Atasan" view, the coachee dropdown is empty
- Fix: When Admin selects "Atasan"/"Coach" view and wants to create sessions, they need a section to scope to. Options: (a) show all coachees for Admin-as-Coach, or (b) warn that Admin needs Section set.

**HomeController.Index (lines 23-99)**
- Checks `user.SelectedView == "Atasan" && !string.IsNullOrEmpty(user.Section)` — null-guards exist
- If `user.Section` is null and `SelectedView == "Atasan"`, falls back to personal data
- Result: Admin in "Atasan" view sees personal dashboard (no section data), with a TempData warning already set
- This is actually partially handled — the warning is shown. Acceptable behavior.

**CDPController.Dashboard (lines 131-228)**
- Checks `user.SelectedView == "Atasan" && !string.IsNullOrEmpty(user.Section)` — null-guards exist
- If `user.Section` is null: no filter applied, shows all data (falls through to HC-like behavior)
- Low priority — not visibly broken, just slightly incorrect semantics.

### Category C: User.IsInRole() Bypasses SelectedView (Correctly for Admin-Tools)

These use `User.IsInRole("Admin")` which always returns true for Admin. This is **correct behavior** — these are admin-level operations that shouldn't be blocked by SelectedView:

- `CMPController.VerifyToken` (line 824): `!User.IsInRole("Admin")` — Admin can always verify tokens. OK.
- `CMPController.StartExam` (line 856): Admin can always start exams. OK (for observation).
- `CMPController.SubmitExam` (line 953): Admin can submit. OK.
- `CMPController.Certificate` (line 1079): Admin can view certificates. OK.
- `CMPController.Results` (line 1112): Admin can view results. OK.
- `CMPController.ManageQuestions` / `AddQuestion` (lines 875, 889): `[Authorize(Roles = "Admin, HC")]` — Admin always has access. OK.
- `CMPController.CompetencyGap` (line 1538): `isHcOrAdmin = userRoles.Contains("Admin")` — Admin always has full access to see all users. This is arguably OK.
- `CMPController.CpdpProgress` (line 1640): Same — Admin always sees all users.

### Category D: Navigation in _Layout.cshtml

The navbar uses `userRole` (real role from `GetRolesAsync`) for conditional nav items:
- DevDashboard link shown when `userRole == UserRoles.Admin` — always visible for Admin. OK.
- The switcher dropdown shows current `user.SelectedView` and provides links to switch.
- Body CSS class: `view-@(currentUser?.SelectedView?.ToLower() ?? "coachee")` — correctly reflects SelectedView.
- No nav items are conditionally hidden/shown based on SelectedView — nav is always Admin nav.

---

## What "Admin" View Means

"Admin" as a SelectedView value is a **reset/identity view** — Admin viewing the app as Admin with no role simulation. Since the default behavior when `SelectedView` is unrecognized already gives Admin full access, `"Admin"` in the switcher means: "I am done simulating another role, show me Admin-native behavior."

Mechanically, the controller code for `userRole == UserRoles.Admin` already handles the case where `SelectedView` doesn't match any specific simulation (`else` / fall-through = full admin behavior). Adding `"Admin"` as a valid value just needs:
1. It added to `allowedViews` in `SwitchView`
2. An `"Admin View"` option in the `_Layout.cshtml` dropdown with a checkmark when `SelectedView == "Admin"`
3. No new controller branch needed — existing `else` branches in the Admin switch logic naturally handle `"Admin"` as "no specific simulation"

---

## Key Files and Exact Locations

| File | Relevant Lines | Issue |
|------|----------------|-------|
| `Controllers/AccountController.cs` | 138 | `allowedViews` missing "Admin" |
| `Views/Shared/_Layout.cshtml` | 103-145 | Dropdown missing "Admin" option |
| `Controllers/CDPController.cs` | 240 | DevDashboard ignores SelectedView for Admin |
| `Controllers/CDPController.cs` | 603 | ProtonMain uses `user.Section` (null for Admin) |
| `Controllers/CDPController.cs` | 525 | CreateSession uses `user.RoleLevel` instead of SelectedView |
| `Controllers/CDPController.cs` | 729-748 | Deliverable `isHC` check excludes Admin |
| `Controllers/CDPController.cs` | 797-799 | Deliverable `canApprove`/`canHCReview` exclude Admin |
| `Controllers/CDPController.cs` | 829, 921 | ApproveDeliverable/RejectDeliverable block Admin |
| `Controllers/CDPController.cs` | 1011, 1043 | HCReviewDeliverable/HCApprovals block Admin |
| `Controllers/CDPController.cs` | 1137, 1199 | CreateFinalAssessment blocks Admin |
| `Controllers/CDPController.cs` | 461-481 | Coaching coachee list uses null `user.Section` |

---

## Architecture Patterns

### Pattern 1: Correct Admin SelectedView Switch

This is the established, working pattern used in `CDPController.PlanIdp`, `HomeController.Index`, `CMPController.Records`:

```csharp
// Source: CDPController.cs line 95-110 (working example)
if (userRole == UserRoles.Admin)
{
    if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
    {
        // Show personal / personal-like data
    }
    else if (user.SelectedView == "HC")
    {
        // Show all data
    }
    // For Atasan view, use section-based logic (if section is set)
}
```

### Pattern 2: Fixing HC-Gated Actions for Admin-As-HC

Current broken pattern:
```csharp
// BROKEN: Blocks Admin even when simulating HC
if (userRole != UserRoles.HC) return Forbid();
```

Fixed pattern:
```csharp
// FIXED: Allow Admin simulating HC
bool isHCAccess = userRole == UserRoles.HC ||
                  (userRole == UserRoles.Admin && user.SelectedView == "HC");
if (!isHCAccess) return Forbid();
```

### Pattern 3: Fixing RoleLevel-Based Gates

Current broken pattern:
```csharp
// BROKEN: Admin (RoleLevel=1) always passes, ignores SelectedView
if (user.RoleLevel > 5 && userRole != UserRoles.Admin) return Forbid();
```

Fixed pattern (for gates that should respect SelectedView):
```csharp
// FIXED: Check effective role, not physical RoleLevel
bool isCoacheeView = userRole == UserRoles.Coachee ||
                     (userRole == UserRoles.Admin && user.SelectedView == "Coachee");
if (isCoacheeView) return Forbid(); // Coachee-simulating Admin can't do coach actions
```

### Pattern 4: Fixing isHC bool Derivation

Current broken pattern:
```csharp
bool isHC = userRole == UserRoles.HC;
```

Fixed:
```csharp
bool isHC = userRole == UserRoles.HC ||
            (userRole == UserRoles.Admin && user?.SelectedView == "HC");
```

### Pattern 5: Adding "Admin" to the Dropdown

In `_Layout.cshtml`, after the existing Coachee option (line ~144):
```html
<li><hr class="dropdown-divider"></li>
<li>
    <a class="dropdown-item d-flex align-items-center gap-2"
       asp-controller="Account" asp-action="SwitchView"
       asp-route-view="Admin"
       asp-route-returnUrl="@Context.Request.Path">
        <i class="bi bi-shield-fill"></i>
        <span>Admin View</span>
        @if (currentUser.SelectedView == "Admin")
        {
            <i class="bi bi-check2 ms-auto text-primary"></i>
        }
    </a>
</li>
```

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Role simulation state | Session-based middleware, custom claims principal | Existing `SelectedView` DB field | Already implemented, DB-persisted, works |
| "Effective role" helper | Complex RoleLevel math | Simple `SelectedView` string checks inline | Consistent with existing codebase pattern |
| Navigation conditionality | Role-based nav middleware | Existing `userRole` + `SelectedView` checks in Razor | Already working for most nav items |

**Key insight:** The codebase already has a working pattern for `SelectedView`-based Admin simulation. The fix is to apply the existing pattern consistently to the broken actions — not to replace or redesign the mechanism.

---

## Common Pitfalls

### Pitfall 1: Admin's Section and Unit Are Null
**What goes wrong:** Queries scoped to `user.Section` (e.g., `Where(u => u.Section == user.Section)`) return empty results for Admin.
**Why it happens:** Admin account (`admin@hcportal.com`) has no Section/Unit set in seed data.
**How to avoid:** When Admin simulates "Atasan" or "Coach" view, either (a) fall back to all-access behavior, or (b) prompt Admin to set a Section in their profile.
**Warning signs:** Empty dropdowns or empty coachee lists when Admin switches to Atasan/Coach view.

### Pitfall 2: userRole Is Always "Admin" — Never the Simulated Role
**What goes wrong:** Code that checks `userRole == UserRoles.HC` to grant HC access will always fail for Admin, even when `SelectedView == "HC"`.
**Why it happens:** `GetRolesAsync` returns actual Identity roles — simulation is only in `SelectedView`, not in claims.
**How to avoid:** Use the pattern `userRole == UserRoles.HC || (userRole == UserRoles.Admin && user.SelectedView == "HC")` for every access check that should respect simulation.
**Warning signs:** `[Authorize(Roles = "HC")]` attribute on actions — these will NEVER be reachable by Admin regardless of SelectedView.

### Pitfall 3: `[Authorize(Roles = "...")]` Attribute Cannot See SelectedView
**What goes wrong:** `[Authorize(Roles = "Admin, HC")]` grants Admin access but uses real claims. `[Authorize(Roles = "HC")]` BLOCKS Admin even in HC view.
**Why it happens:** ASP.NET Identity's policy-based role check reads `ClaimsPrincipal`, not `SelectedView`.
**How to avoid:** Don't use `[Authorize(Roles = "HC")]` for actions that Admin-as-HC should reach. Use a manual check inside the action body instead.
**Warning signs:** `ManageQuestions` and `AddQuestion` in CMPController use `[Authorize(Roles = "Admin, HC")]` — this is fine because it includes Admin. But any attribute with ONLY "HC" would block Admin.

### Pitfall 4: SelectedView "Admin" Needs Explicit Handling in Some Actions
**What goes wrong:** Actions that do `if (user.SelectedView == "HC") {...} else if (user.SelectedView == "Atasan") {...} else if (user.SelectedView == "Coach") {...} else if (user.SelectedView == "Coachee") {...}` — once `"Admin"` is added as a valid SelectedView, the `else` / fallthrough behavior needs to be the same as "no specific simulation" (full Admin access).
**How to avoid:** Verify all Admin SelectedView switch blocks handle `"Admin"` via the existing `else` fallthrough or by adding an explicit `else if (user.SelectedView == "Admin")` branch that mirrors HC/full-access behavior.

### Pitfall 5: DB-Persisted SelectedView Means "Admin" Must Be Reset Before Logout
**What goes wrong:** If Admin sets SelectedView to "Coachee" and logs out, next login still shows "Coachee" view (it's persisted in DB).
**Why it happens:** `SelectedView` is saved to the `ApplicationUser` record, not to the session.
**How to avoid:** This is existing behavior and not a new problem. Document it as a known characteristic. Optionally reset SelectedView to "HC" on next Admin login (but that's out of scope for this phase).

---

## Prioritized Fix List

### Must Fix (Pages Are Visibly Broken)

1. **Add "Admin" to allowedViews** in `AccountController.SwitchView` — 1 line change
2. **Add "Admin View" option** to `_Layout.cshtml` dropdown — ~10 lines
3. **HCApprovals**: Allow Admin when `SelectedView == "HC"` — 1 line change
4. **HCReviewDeliverable**: Allow Admin when `SelectedView == "HC"` — 1 line change
5. **CreateFinalAssessment GET+POST**: Allow Admin when `SelectedView == "HC"` — 2 line changes
6. **Deliverable**: Fix `isHC` derivation to include Admin-as-HC — 2 line change
7. **Deliverable**: Fix `canApprove`/`canHCReview` derivations — 2 line changes
8. **ApproveDeliverable**: Allow Admin when `SelectedView == "HC"` or `"Atasan"` — 1 line
9. **RejectDeliverable**: Same fix — 1 line

### Should Fix (Confusing/Incorrect Behavior)

10. **DevDashboard**: Respect SelectedView for Admin — add SelectedView switch block at top
11. **ProtonMain**: Show all coachees when Admin is in HC view; handle null section gracefully
12. **Coaching coachee list**: Show all coachees when Admin is in HC view (not scoped by null Section)
13. **CreateSession**: Block creation when Admin is in "Coachee" view

### Low Priority / Acceptable

14. **UploadEvidence**: Admin-as-Coachee uploading their own evidence — acceptable behavior, low priority
15. **CMPController access checks using `isHcOrAdmin`**: Admin always gets full CompetencyGap/CpdpProgress access — fine, Admin should always be able to see all users

---

## State of the Art (This Codebase)

| Old Pattern | Working Pattern | Impact |
|---|---|---|
| `userRole == UserRoles.HC` (blocks Admin) | `userRole == UserRoles.HC \|\| (userRole == UserRoles.Admin && user.SelectedView == "HC")` | Unlocks HC-gated actions for Admin-as-HC |
| `user.RoleLevel > 5` (always passes Admin) | Check `user.SelectedView` for Admin branch | Correct Coachee/Coach simulation |
| `User.IsInRole("Admin")` | Leave unchanged — correct for Admin-tool access | No change needed |
| `[Authorize(Roles = "Admin, HC")]` | Leave unchanged — Admin is included | No change needed |

---

## Open Questions

1. **What should DevDashboard show when Admin is in "Coach" or "Atasan" view?**
   - What we know: Admin has null Section/Unit, so section-scoped queries return empty
   - What's unclear: Should Admin-as-Coach see all coachees (like HC view) or be blocked?
   - Recommendation: When `SelectedView == "Coach"` or `"Atasan"`, Admin should see all coachees (same as HC) because Admin has no section to scope by. Document this as a known limitation.

2. **Should Admin's SelectedView reset to "Admin" when switching to "Admin View"?**
   - What we know: SeedData sets Admin's SelectedView to "HC" by default
   - What's unclear: User wants "Admin" in the list — does that mean it should also be the default?
   - Recommendation: Change the seed data to set Admin's `SelectedView = "Admin"` as the default, and update `ApplicationUser.SelectedView` default from `"Coachee"` to `"Coachee"` (no change for other users). Admin account seed should be `"Admin"`.

3. **Should `canUpload` respect Admin SelectedView?**
   - Current: `bool canUpload = (progress.Status == "Active" || progress.Status == "Rejected") && user.RoleLevel <= 5;` — Admin always can upload
   - Recommendation: Admin-as-HC should be able to upload (for testing). Leave as-is (low priority fix).

---

## Sources

### Primary (HIGH confidence)
- Direct reading of `Controllers/AccountController.cs` — SwitchView action, allowedViews array
- Direct reading of `Controllers/CDPController.cs` — all action methods, 1453 lines
- Direct reading of `Controllers/CMPController.cs` — all action methods, 1840 lines
- Direct reading of `Controllers/HomeController.cs` — Index, all helper methods
- Direct reading of `Views/Shared/_Layout.cshtml` — dropdown HTML, SelectedView usage
- Direct reading of `Models/ApplicationUser.cs` — SelectedView field definition
- Direct reading of `Models/UserRoles.cs` — all role constants and GetRoleLevel()
- Direct reading of `Program.cs` — no middleware for role simulation

### Secondary (MEDIUM confidence)
- N/A — this is a pure codebase analysis task

### Tertiary (LOW confidence)
- N/A

---

## Metadata

**Confidence breakdown:**
- Switcher mechanism: HIGH — read directly from AccountController.SwitchView and _Layout.cshtml
- Broken pages catalog: HIGH — each broken check identified by exact file and line number
- Fix patterns: HIGH — based on existing working patterns in the same codebase
- "Admin" view semantics: HIGH — derived from how existing Admin-specific branches work

**Research date:** 2026-02-18
**Valid until:** Until any of the listed files are modified (no external dependencies — pure codebase analysis)
