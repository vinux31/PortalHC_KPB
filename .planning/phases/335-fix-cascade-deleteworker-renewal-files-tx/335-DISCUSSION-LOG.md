# Phase 335: Fix Cascade DeleteWorker Triple-Fix - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-05-28
**Phase:** 335-fix-cascade-deleteworker-renewal-files-tx
**Mode:** --auto (single-pass, all recommended defaults)
**Severity:** HIGH (triple-dim D2+D5+D7)
**Effort:** L (~200-300 LoC)

---

## D5 Cross-User Renewal Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Block (Phase 325 P05 pattern) | TempData friendly, user must break renewal chain manually | ✓ |
| Null-clear references automatically | Set RenewsXxx = null in cross-user TR/AS before cascade | |
| Cascade delete cross-user (dangerous) | Auto-delete dependent worker certs | |

**Reason:** Block pattern konsisten Phase 325 P05 + UX clarity (explicit user decision per worker). Null-clear hides intent in audit. Cascade is destructive cross-worker.

---

## UserManager.DeleteAsync Tx Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Trust same-DbContext assumption | AddEntityFrameworkStores<ApplicationDbContext> = same context = same tx | ✓ |
| Fallback _context.Users.Remove direct | Skip Identity hooks, stays in tx explicitly | |
| Skip tx wrap for UserManager call | Accept partial state risk | |

**Reason:** Standard ASP.NET Identity convention. Verify saat eksekusi via Program.cs config check. Fallback ready kalau assumption salah.

---

## Identity Error Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Throw exception kalau result.Succeeded=false | Treat Identity error as throwable | |
| Early return INSIDE try, tx disposal auto-rollback | result.Succeeded check + return RedirectToAction | ✓ |

**Reason:** Identity returns IdentityResult — bukan exception. Manual control flow lebih clean dari throw-catch dance. Disposal handles rollback.

---

## Identity Error Message UX

| Option | Description | Selected |
|--------|-------------|----------|
| Keep current Description display | "Gagal menghapus user: {Description joins}" | ✓ |
| Replace dengan generic message | Hide Identity error detail | |

**Reason:** Identity errors typically actionable user-level ("User has active logins"). Bukan DB constraint info leak. Keep UX.

---

## File Collection Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Single allFilePaths List<string> | Merge 3 sources (TR + AS + Proton) jadi 1 loop | ✓ |
| Separate lists per source | tr / as / proton loops | |

**Reason:** Single loop simpler post-commit. File.Delete logic identical regardless source.

---

## Plan Task Count

| Option | Description | Selected |
|--------|-------------|----------|
| 2 task standard | Task 1 refactor + Task 2 verify | |
| 3 task extended | Task 1 D5 pre-check + Task 2 D2+D7 bulk + Task 3 verify | ✓ |

**Reason:** Complexity L effort. Pre-check + tx wrap conceptually independent. Easier verification per task.

---

## IT_NOTIFY Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Push standalone | | |
| Bundle v19.0 (final phase) | Phase 327 option-b hold + close out milestone | ✓ |

**Reason:** Konsisten Phase 329-334. Phase 335 = final v19.0 phase, batch push complete milestone.

---

## Claude's Discretion

- Cross-user query optimization: combine 2 counts via OR conditions (avoid N+1)
- TempData error message format: include `userName` + count detail for clarity
- File.Delete log format: `"File.Delete post-commit failed (Worker file): {Path}"` — distinguish phase context
- Path.DirectorySeparatorChar cross-platform path pattern (Phase 333/334 reuse)

## Deferred Ideas

- Null-clear alternative — UX clarity prefers block
- Soft-delete worker — Phase 999.x backlog
- Integration test xUnit — out of scope
- Cron janitor — Phase 999.x backlog
- Identity error UX refactor — keep current
