---
phase: quick-10
plan: "01"
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/CMPController.cs
  - Views/CMP/Assessment.cshtml
autonomous: true
must_haves:
  truths:
    - "Monitoring tab loads data successfully when HC/Admin clicks it on /CMP/Assessment?view=manage"
    - "Non-HC/Admin users cannot access GetMonitorData endpoint"
    - "Fetch errors display a meaningful message instead of silently failing"
  artifacts:
    - path: "Controllers/CMPController.cs"
      provides: "GetMonitorData action with correct role check via UserManager"
      contains: "GetRolesAsync"
    - path: "Views/CMP/Assessment.cshtml"
      provides: "Monitoring tab AJAX with proper error handling"
      contains: "res.ok"
  key_links:
    - from: "Views/CMP/Assessment.cshtml"
      to: "/CMP/GetMonitorData"
      via: "fetch() on shown.bs.tab event"
      pattern: "fetch.*GetMonitorData"
---

<objective>
Fix "Failed to load monitoring data. Please refresh the page." error on /CMP/Assessment?view=manage Monitoring tab.

**Root cause:** `GetMonitorData()` action reads user role from `HttpContext.Session.GetString("UserRole")` — but this session key is NEVER written anywhere in the application. Every other action in CMPController uses `_userManager.GetRolesAsync(user)` to determine the role. Because the session key is always null, it defaults to `"Worker"`, the HC check fails, and `Forbid()` is returned (403 HTML). The JS `fetch()` then calls `res.json()` on the HTML response, which throws, landing in the `.catch()` handler that shows the error message.

**Fix:** Replace the broken session-based role check with the standard `_userManager.GetRolesAsync(user)` pattern used by every other action. Also add `res.ok` check in the JS fetch to provide better error diagnostics.

Purpose: Restore monitoring tab functionality that was broken since quick-6 introduced the AJAX lazy-load.
Output: Working monitoring tab on Assessment manage page.
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/quick/6-fix-slow-performance-on-assessment-manag/6-SUMMARY.md
@Controllers/CMPController.cs (lines 245-317 — GetMonitorData action)
@Views/CMP/Assessment.cshtml (lines 852-930 — monitoring tab AJAX)
@Models/UserRoles.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix GetMonitorData role check and harden JS fetch error handling</name>
  <files>Controllers/CMPController.cs, Views/CMP/Assessment.cshtml</files>
  <action>
**CMPController.cs — GetMonitorData() action (around line 247):**

Replace the broken session-based role check:
```csharp
var userRole = HttpContext.Session.GetString("UserRole") ?? "Worker";
bool isHCAccess = userRole == "HC" || userRole == "Admin";
if (!isHCAccess) return Forbid();
```

With the standard UserManager pattern (same as Assessment action at line 87-98):
```csharp
var user = await _userManager.GetUserAsync(User);
var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
var userRole = userRoles.FirstOrDefault();
bool isHCAccess = userRole == UserRoles.Admin || userRole == UserRoles.HC;
if (!isHCAccess) return Forbid();
```

This is the exact same pattern used by Assessment(), Kkj(), Records(), and every other action in CMPController. Use the `UserRoles.Admin` and `UserRoles.HC` constants (not string literals "Admin"/"HC") for consistency.

**Views/CMP/Assessment.cshtml — monitoring AJAX block (around line 862):**

Change the fetch chain from:
```js
fetch('/CMP/GetMonitorData')
    .then(function (res) { return res.json(); })
```

To check `res.ok` before parsing JSON:
```js
fetch('/CMP/GetMonitorData')
    .then(function (res) {
        if (!res.ok) throw new Error('HTTP ' + res.status);
        return res.json();
    })
```

This ensures that non-200 responses (403, 500, etc.) produce a clear error rather than a confusing JSON parse failure. The existing `.catch()` handler at line 925 already displays the error message, so no other changes needed in the JS.

**Why this is the fix:** The session key "UserRole" is never set anywhere in the entire application. Every controller action resolves the role via `_userManager.GetRolesAsync()`. The quick-6 implementation incorrectly assumed session-based role storage existed.
  </action>
  <verify>
1. `dotnet build` — zero errors, zero warnings related to GetMonitorData
2. Grep for `Session.GetString("UserRole")` in CMPController.cs — should return NO matches (the broken pattern is gone)
3. Grep for `_userManager.GetRolesAsync` in GetMonitorData — should return a match (the correct pattern is in place)
4. Grep for `res.ok` in Assessment.cshtml monitoring fetch block — should return a match
  </verify>
  <done>
GetMonitorData uses _userManager.GetRolesAsync() for role resolution (not session). JS fetch checks res.ok before parsing JSON. Build succeeds with zero errors.
  </done>
</task>

</tasks>

<verification>
- `dotnet build` passes with 0 errors
- No remaining `Session.GetString("UserRole")` in CMPController.cs
- GetMonitorData follows same auth pattern as Assessment action
- JS fetch has res.ok guard before res.json()
</verification>

<success_criteria>
Monitoring tab on /CMP/Assessment?view=manage loads data successfully instead of showing "Failed to load monitoring data" error. The fix uses the established _userManager.GetRolesAsync() pattern consistent with every other action in CMPController.
</success_criteria>

<output>
After completion, create `.planning/quick/10-fix-monitoring-data-error-on-assessment-/10-SUMMARY.md`
</output>
