# Phase 333: Fix Cascade DeleteCoachingSession File Atomicity - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md.

**Date:** 2026-05-28
**Phase:** 333-fix-cascade-deletecoachingsession-file-atomicity
**Mode:** --auto (single-pass, all recommended defaults)

---

## pathsToDelete Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Keep inside tx scope, do File.Delete inside | Status quo broken pattern | |
| Declare outer tx, build inside, delete outside | Defer File.Delete past commit | ✓ |

**Reason:** Phase 331/332 pattern reuse. Variable accessibility outer-scope diperlukan agar visible POST tx scope untuk File.Delete loop.

---

## Catch Refactor Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Single catch generic Exception + throw (status quo) | Raw 500 page ke user | |
| Catch DbUpdateException + Exception fallback friendly | Phase 332 D-04 pattern extended | ✓ |

**Reason:** D6 polish — friendly TempData wajib (raw 500 unacceptable). Specific DbUpdateException untuk message DB constraint, generic Exception untuk fallback internal error.

---

## tx.RollbackAsync Explicit Call

| Option | Description | Selected |
|--------|-------------|----------|
| Keep explicit RollbackAsync di catch | Status quo | |
| Hapus — await using disposal handles rollback | Konsisten Phase 331/332 | ✓ |

**Reason:** `await using var tx` disposal auto-rollback kalau exception escape sebelum CommitAsync. Konsisten Phase 331 D-07.

---

## cleanupNote Timing

| Option | Description | Selected |
|--------|-------------|----------|
| Build outside tx (POST File.Delete) | Reflect actual delete count | |
| Build inside tx (using pathsToDelete.Count at intent time) | Pre-deletion count, audit log atomic | ✓ |

**Reason:** Audit log MUST atomic dengan delete operation (INSIDE tx). pathsToDelete.Count = intent count, accurate enough untuk audit trail. File deletion failure is post-commit warning, doesn't change audit semantics.

---

## Status="Approved" + Sibling Sessions Branches

| Option | Description | Selected |
|--------|-------------|----------|
| Refactor branches | Add safety checks | |
| Preserve verbatim — no File.Delete in those branches | Existing logic correct, no atomicity issue | ✓ |

**Reason:** Branches don't trigger File.Delete (intentional preserve). Out of Phase 333 scope.

---

## IT_NOTIFY Strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Push standalone | | |
| Bundle v19.0 | Phase 327 option-b hold | ✓ |

**Reason:** Konsisten Phase 329-332.

---

## Claude's Discretion

- File.Delete inner try/catch wrap `Exception` (bukan `IOException` specific) — konsisten Phase 331/332 (file system fail beragam)
- Catch order: DbUpdateException FIRST, Exception SECOND — C# catch precedence (specific-to-general)
- Logger message format: `_logger.LogWarning(ex, "File.Delete post-commit failed (CoachingSession evidence): {Path}", relUrl)` — distinguish phase context

## Deferred Ideas

- Integration test xUnit — out of scope
- Refactor JSON parse ke shared helper — scope creep
- Progress revert ke service layer — Phase 297 design territory
