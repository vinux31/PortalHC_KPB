---
phase: quick-26
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Views/CMP/Certificate.cshtml
  - Controllers/AdminController.cs
  - Controllers/CDPController.cs
  - Controllers/CMPController.cs
autonomous: true
requirements: [BUG-1, BUG-2, BUG-3]
must_haves:
  truths:
    - "returnUrl in Certificate.cshtml only accepts relative URLs"
    - "Excel import does not crash on empty cells"
    - "Notification failures are logged, not silently swallowed"
  artifacts:
    - path: "Views/CMP/Certificate.cshtml"
      provides: "Safe returnUrl validation"
      contains: "Uri.IsWellFormedUriString"
    - path: "Controllers/AdminController.cs"
      provides: "Null-safe GetString calls and logged catch blocks"
    - path: "Controllers/CDPController.cs"
      provides: "Logged catch blocks with ILogger"
    - path: "Controllers/CMPController.cs"
      provides: "Logged catch block"
  key_links: []
---

<objective>
Fix 3 bugs from security/quality audit: open redirect vulnerability (CRITICAL), null crash on Excel import (HIGH), and silent catch blocks hiding notification errors (HIGH).

Purpose: Address critical security vulnerability and two high-priority reliability issues.
Output: Patched files with no new functionality, only defensive fixes.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Views/CMP/Certificate.cshtml
@Views/CMP/Results.cshtml (reference pattern for returnUrl validation)
@Controllers/AdminController.cs
@Controllers/CDPController.cs
@Controllers/CMPController.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix open redirect and null Excel crashes</name>
  <files>Views/CMP/Certificate.cshtml, Controllers/AdminController.cs</files>
  <action>
**Bug 1 — Open redirect in Certificate.cshtml (lines 6-7):**
Replace:
```csharp
var certBackUrl = Context.Request.Query["returnUrl"].ToString();
if (string.IsNullOrEmpty(certBackUrl)) certBackUrl = Url.Action("Assessment", "CMP")!;
```
With (matching Results.cshtml pattern):
```csharp
var rawReturnUrl = Context.Request.Query["returnUrl"].ToString();
var certBackUrl = (!string.IsNullOrEmpty(rawReturnUrl) && Uri.IsWellFormedUriString(rawReturnUrl, UriKind.Relative))
    ? rawReturnUrl
    : Url.Action("Assessment", "CMP")!;
```

**Bug 2 — Null crash on .GetString().Trim() in AdminController.cs:**

In ImportWorkers (lines 4371-4379, 4394): Change all 10 occurrences of `row.Cell(N).GetString().Trim()` to `(row.Cell(N).GetString() ?? "").Trim()`.

In ImportPackageQuestions (lines 5499-5505): Change all 7 occurrences of `row.Cell(N).GetString().Trim()` to `(row.Cell(N).GetString() ?? "").Trim()`. For line 5504 which chains `.Trim().ToUpper()`, result is `(row.Cell(6).GetString() ?? "").Trim().ToUpper()`.

Total: 17 occurrences to fix.
  </action>
  <verify>
    <automated>cd C:/Users/Administrator/Desktop/PortalHC_KPB && grep -c "Uri.IsWellFormedUriString" Views/CMP/Certificate.cshtml && grep -c "GetString().Trim()" Controllers/AdminController.cs</automated>
  </verify>
  <done>Certificate.cshtml has Uri.IsWellFormedUriString validation. AdminController.cs has 0 occurrences of bare .GetString().Trim() (all now have ?? "" guard).</done>
</task>

<task type="auto">
  <name>Task 2: Replace silent catch blocks with logged catches</name>
  <files>Controllers/AdminController.cs, Controllers/CDPController.cs, Controllers/CMPController.cs</files>
  <action>
**CDPController — first add ILogger:**
1. Add field: `private readonly ILogger<CDPController> _logger;`
2. Add `ILogger<CDPController> logger` parameter to constructor (after last param)
3. Add `_logger = logger;` in constructor body

**Then fix all silent catch blocks:**

Each `catch { /* fail silently */ }` or `catch { /* fail-silent */ }` becomes `catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }`.

Exact locations:
- AdminController.cs line 1017: `catch { /* fail silently */ }`
- AdminController.cs line 1375: `catch { /* fail silently */ }`
- AdminController.cs line 3221: `catch { /* fail-silent */ }`
- AdminController.cs line 3345: `catch { /* fail-silent */ }`
- AdminController.cs line 3432: `catch { /* fail-silent */ }`
- CDPController.cs line 875: `catch { /* fail silently */ }`
- CDPController.cs line 986: `catch { /* fail silently */ }`
- CDPController.cs line 1017: `catch { /* fail silently */ }`
- CDPController.cs line 1047: `catch { /* fail silently */ }`
- CDPController.cs line 2109: `catch { /* fail silently */ }`
- CMPController.cs line 2138: `catch { /* fail silently */ }`

Total: 11 catch blocks (8 from audit spec + 3 additional found in AdminController).

Do NOT change line 1068 in AdminController.cs — that one has a different comment about audit logging and is a different pattern.
  </action>
  <verify>
    <automated>cd C:/Users/Administrator/Desktop/PortalHC_KPB && echo "Silent catches remaining:" && grep -c "catch {" Controllers/AdminController.cs Controllers/CDPController.cs Controllers/CMPController.cs; echo "Logger in CDP:" && grep -c "_logger" Controllers/CDPController.cs</automated>
  </verify>
  <done>Zero silent catch blocks remain (except line 1068 AdminController which is intentionally different). CDPController has ILogger injected. All notification failures now logged with LogWarning.</done>
</task>

</tasks>

<verification>
1. `dotnet build` compiles without errors
2. No remaining `catch { /*` patterns in the 3 controller files (except AdminController line 1068)
3. Certificate.cshtml contains `Uri.IsWellFormedUriString`
4. No bare `.GetString().Trim()` in AdminController.cs
</verification>

<success_criteria>
- Open redirect vulnerability eliminated — only relative URLs accepted
- Excel import handles null/empty cells without crashing
- All notification catch blocks log errors instead of swallowing them
- Application builds and runs without regression
</success_criteria>

<output>
After completion, create `.planning/quick/26-critical-and-high-priority-bug-fixes-fro/26-SUMMARY.md`
</output>
