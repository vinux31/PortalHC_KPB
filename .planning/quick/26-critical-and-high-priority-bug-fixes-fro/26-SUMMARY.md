---
phase: quick-26
plan: 01
subsystem: security/reliability
tags: [security, bug-fix, open-redirect, null-safety, logging]
dependency_graph:
  requires: []
  provides: [safe-returnUrl-validation, null-safe-excel-import, logged-notification-errors]
  affects: [Views/CMP/Certificate.cshtml, Controllers/AdminController.cs, Controllers/CDPController.cs, Controllers/CMPController.cs]
tech_stack:
  added: []
  patterns: [Uri.IsWellFormedUriString relative-url guard, null-coalescing GetString guard, ILogger.LogWarning catch pattern]
key_files:
  created: []
  modified:
    - Views/CMP/Certificate.cshtml
    - Controllers/AdminController.cs
    - Controllers/CDPController.cs
    - Controllers/CMPController.cs
decisions:
  - Used Uri.IsWellFormedUriString(url, UriKind.Relative) matching existing Results.cshtml pattern for consistency
  - Added ?? "" guard to GetString() calls so null cells become empty string without crash
  - Injected ILogger<CDPController> via constructor (CDPController was the only controller missing it)
  - Kept AdminController line 1068 catch unchanged (intentional — audit log failure must not mask original error)
metrics:
  duration: ~15 minutes
  completed_date: "2026-03-12"
  tasks_completed: 2
  files_modified: 4
---

# Quick Task 26: Critical and High-Priority Bug Fixes Summary

**One-liner:** Patched open redirect in Certificate.cshtml, null-safe Excel GetString in AdminController, and replaced 11 silent catch blocks with LogWarning across 3 controllers.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Fix open redirect and null Excel crashes | b7ecda1 | Certificate.cshtml, AdminController.cs |
| 2 | Replace silent catch blocks with logged catches | ff39b6f | AdminController.cs, CDPController.cs, CMPController.cs |

## What Was Done

### Bug 1 — Open Redirect in Certificate.cshtml (CRITICAL)

The `returnUrl` query parameter was used directly as an href without validation, allowing attackers to redirect users to external URLs.

**Fix:** Added `Uri.IsWellFormedUriString(rawReturnUrl, UriKind.Relative)` guard matching the same pattern used in `Results.cshtml`. External URLs and absolute URLs are silently replaced with the default `/CMP/Assessment` route.

### Bug 2 — Null Crash on Excel Import (HIGH)

`ClosedXML`'s `GetString()` can return `null` for certain cell types. Chaining `.Trim()` directly on that crashes with `NullReferenceException`.

**Fix:** Changed 17 occurrences of `row.Cell(N).GetString().Trim()` to `(row.Cell(N).GetString() ?? "").Trim()` across:
- `ImportWorkers`: 10 cells (columns 1-9, plus column 10 in non-AD mode)
- `ImportPackageQuestions`: 7 cells (columns 1-7, with column 6 also chaining `.ToUpper()`)

### Bug 3 — Silent Catch Blocks Hiding Notification Errors (HIGH)

11 notification `catch` blocks swallowed all exceptions silently, making it impossible to diagnose delivery failures in production.

**Fix:** Replaced all 11 with `catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }`:
- AdminController.cs: 5 blocks (lines ~1017, ~1375, ~3221, ~3345, ~3432)
- CDPController.cs: 5 blocks (lines ~875, ~986, ~1017, ~1047, ~2109)
- CMPController.cs: 1 block (line ~2138)

CDPController required adding `ILogger<CDPController>` field and constructor injection (it was the only controller without one).

**Intentionally excluded:** AdminController line 1068 (`catch { /* don't let audit logging failure mask the original error */ }`) — that pattern is architecturally correct and was not modified.

## Deviations from Plan

None — plan executed exactly as written.

## Verification

- `Uri.IsWellFormedUriString` present in Certificate.cshtml: 1 occurrence (confirmed)
- Bare `.GetString().Trim()` remaining in AdminController: 0 (confirmed)
- Silent `catch {` blocks remaining in target files: 0 CDPController, 0 CMPController, 1 AdminController (intentional)
- Build result: 0 errors, 69 pre-existing warnings (all from LdapAuthService CA1416 and unrelated nullable warnings)

## Self-Check: PASSED

- Views/CMP/Certificate.cshtml: modified, committed b7ecda1
- Controllers/AdminController.cs: modified, committed b7ecda1, ff39b6f
- Controllers/CDPController.cs: modified, committed ff39b6f
- Controllers/CMPController.cs: modified, committed ff39b6f
- Build: 0 errors confirmed
