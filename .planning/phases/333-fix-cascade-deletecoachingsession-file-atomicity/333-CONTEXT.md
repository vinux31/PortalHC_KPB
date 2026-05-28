# Phase 333: Fix Cascade DeleteCoachingSession File Atomicity - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Discuss mode:** auto (--auto flag, single-pass, all recommended defaults selected)

<domain>
## Phase Boundary

Phase 333 menutup **1 HIGH finding** Phase 328 Cascade Audit Sweep §4.6 + §9 proposal #4: file-DB atomicity di `DeleteCoachingSession` via reorder `File.Delete` POST `tx.CommitAsync` + refactor catch friendly (D2 fix + D6 polish).

**Scope endpoint:**
1. `DeleteCoachingSession` — `Controllers/CDPController.cs:2433` — D2 (file delete INSIDE tx tapi SEBELUM CommitAsync) + D6 (generic catch + throw raw 500)

**Existing OK (preserve verbatim):**
- `BeginTransactionAsync` L2455 sudah ada — TIDAK perlu tambah, tinggal reorder file delete
- Role check L2432 `[Authorize(Roles = UserRoles.RolesCoachAndAbove)]` + L2441-2453 active-mapping guard
- ActionItems RemoveRange L2458 + CoachingSessions Remove L2459
- Progress revert state logic L2505-2517 (Status, SrSpvApprovalStatus, ShApprovalStatus, RejectedAt, etc) — MUST stay INSIDE tx
- RecordStatusHistory L2518 — MUST stay INSIDE tx (DB mutation)
- Audit log L2536 — MUST stay INSIDE tx
- Nested-if conditional cleanup: only when (1) progress exists (2) no other sibling sessions (3) status ≠ Approved (4) progress not null
- EvidencePathHistory JSON parse L2478-2487 — preserve inner try/catch warn-only

**Existing problems to fix:**
- L2490-2503 `File.Delete` loop INSIDE tx block tapi SEBELUM SaveChanges L2532 + CommitAsync L2538 → file orphan-deleted if SaveChanges/Commit fail
- L2540 catch generic Exception + `throw` → raw 500 page ke user (no friendly TempData)

**Files modified:**
- `Controllers/CDPController.cs` (~30-40 LoC delta, 1 endpoint)

**Zero schema change. Zero migration. Zero model change. Zero view change. Zero service injection.**

</domain>

<decisions>
## Implementation Decisions

### D-01 Source-of-truth: Phase 328 RESEARCH.md §4.6 + §9 proposal #4

Lock seluruh design ke audit deliverable Phase 328 commit `41f1eef2`:
- §4.6: DeleteCoachingSession HIGH D2 (file inside tx pre-Commit) + D6 polish (generic catch throw)
- §9 #4: `fix-cascade-deletecoachingsession-file-atomicity`

### D-02 File Path Collection — Build pathsToDelete INSIDE tx, Defer File.Delete POST Commit

Strategi:
1. `pathsToDelete` declared di SCOPE OUTER tx (sebelum `await using var tx = ...`) sebagai `List<string>?` — null kalau no cleanup needed
2. Build `pathsToDelete` INSIDE tx (nested-if scope, sama posisi L2473-2488 — collection logic only)
3. JSON parse EvidencePathHistory inner try/catch warn-only — preserve verbatim L2478-2487
4. `cleanupNote` string built INSIDE tx menggunakan `pathsToDelete?.Count ?? 0` (count known at build time, file deletion deferred)
5. **HAPUS** loop File.Delete L2490-2503 dari INSIDE tx
6. Progress revert state L2505-2517 + RecordStatusHistory L2518 + SaveChanges L2532 + audit log L2536 + CommitAsync L2538 INSIDE tx — TIDAK pindah
7. **TAMBAH** loop File.Delete OUTSIDE tx (POST CommitAsync, before return) — iterate `pathsToDelete`, inner try/catch per file warn-only

Penting: `pathsToDelete` accessibility — declare di OUTER scope agar visible POST tx scope. Pakai nullable `List<string>?` initialize null, alokasikan inside tx hanya kalau cleanup path branch.

### D-03 Catch Refactor — DbUpdateException + Generic Fallback (Friendly TempData, No Throw)

Current L2540: `catch (Exception ex) { _logger.LogError(...); await tx.RollbackAsync(); throw; }` → raw 500.

Fix:
```csharp
catch (DbUpdateException dbEx)
{
    _logger.LogWarning(dbEx, "DbUpdate failed for DeleteCoachingSession ID={Id}", id);
    // tx.RollbackAsync TIDAK perlu — `await using` disposal auto-rollback kalau belum committed
    TempData["Error"] = "Gagal hapus sesi coaching: ada constraint database yang dilanggar.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to delete coaching session ID={Id}", id);
    TempData["Error"] = "Gagal hapus sesi coaching: terjadi kesalahan internal. Hubungi admin.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
```

[auto] Hapus eksplisit `await tx.RollbackAsync()` — `await using var tx` disposal auto-rollback kalau exception escape sebelum CommitAsync. Hapus `throw;` — return friendly redirect instead.

### D-04 Cleanup Note Build Timing — INSIDE tx, Count from pathsToDelete Collection

`cleanupNote` built INSIDE tx (audit log entry assembly). Use `pathsToDelete?.Count ?? 0` even though File.Delete happens later — count REPRESENTS intent (paths collected for deletion), accurate dari audit perspective.

Audit log message tetap: `$"Progress {progress.Id} reverted to Pending, {pathsToDelete.Count} file(s) cleaned."` — bahasa "cleaned" still accurate (DB references nullified + queued for disk cleanup).

### D-05 File.Delete Failure Handling — Per-File Warn-Only, Continue Loop

Same pattern Phase 331/332:
```csharp
if (pathsToDelete != null && pathsToDelete.Count > 0)
{
    foreach (var relUrl in pathsToDelete)
    {
        try
        {
            var physical = Path.Combine(_env.WebRootPath,
                relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
        }
        catch (Exception fex)
        {
            _logger.LogWarning(fex, "File.Delete post-commit failed (CoachingSession evidence): {Path}", relUrl);
        }
    }
}
```

Inner catch `Exception` (bukan `IOException` specific) — konsisten Phase 331/332 D-07.

### D-06 Active Mapping Guard + Authorization — Preserve Verbatim

[auto] L2441-2453 active mapping guard (Coach-only restriction) + L2432 role check + L2436 user challenge — TIDAK disentuh. Posisi tetap OUTSIDE tx scope (early return TempData friendly tanpa tx creation).

### D-07 Progress Status="Approved" Branch — Preserve Verbatim

[auto] L2521-2524 branch "Progress sudah Approved — state & file dipertahankan" — TIDAK disentuh. No File.Delete dalam branch ini (intentional preserve).

### D-08 Sibling Sessions Branch — Preserve Verbatim

[auto] L2526-2529 "Sibling sessions masih ada — progress state dipertahankan" — TIDAK disentuh. No File.Delete (sibling masih perlu file).

### D-09 IT_NOTIFY Strategy — Bundle v19.0

[auto] Sama dengan Phase 329-332: ship lokal, BUNDLE batch push ke origin/main bersama Phase 325+...+333 saat user release push lock per Phase 327 option-b hold. Append entry ke `docs/IT_NOTIFY.md` Phase 333 section + smoke scenario #11.

### D-10 Acceptance Criteria (Locked)

1. `DeleteCoachingSession` L2433-2543: File.Delete loop dipindah dari INSIDE tx (L2490-2503) ke OUTSIDE tx (POST CommitAsync L2538).
2. `pathsToDelete` declared di SCOPE OUTER tx sebagai `List<string>?` — accessible post-tx.
3. `pathsToDelete` build logic + JSON parse + cleanupNote tetap INSIDE tx (collection only, no actual File.Delete).
4. Progress revert state L2505-2517 + RecordStatusHistory L2518 + SaveChanges L2532 + audit log L2536 preserved verbatim INSIDE tx.
5. Catch generic Exception L2540 + throw direfactor → catch DbUpdateException (friendly TempData) + catch Exception fallback (friendly TempData) + return RedirectToAction. NO `throw`. NO explicit `tx.RollbackAsync` (await using disposal handles it).
6. `[Authorize]` + active mapping guard L2441-2453 + role check L2432 preserved verbatim OUTSIDE tx.
7. `dotnet build` clean (0 error CS*).
8. `dotnet test --no-build` 18/18 pass (no regression).
9. Manual smoke deferred ke Dev promo (scenario #11 IT_NOTIFY).
10. Commit: `feat(333): cascade atomicity DeleteCoachingSession (File.Delete post-commit + catch friendly)`.
11. SUMMARY.md generated.

### D-11 Plan Structure — Single Plan, 2 Task Wave

1 PLAN.md (`333-01-PLAN.md`) dengan 2 task:
- Task 1: Refactor `DeleteCoachingSession` di CDPController.cs (~40 LoC) — move pathsToDelete declaration outer tx, defer File.Delete POST commit, refactor catch DbUpdateException + Exception fallback friendly
- Task 2: Verify build + test + grep AC + IT_NOTIFY + commit + SUMMARY

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning.**

### Source Truth (Audit-Derived)
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md` §4.6 (DeleteCoachingSession HIGH D2+D6) + §9 proposal #4

### Gold Standard Pattern References
- `Controllers/CDPController.cs:2433-2543` `DeleteCoachingSession` — current code (target modification)
- `Controllers/TrainingAdminController.cs:559-625` `DeleteTraining` (Phase 331) — File.Delete POST commit pattern
- `Controllers/DocumentAdminController.cs:283-396` `DeleteBagian` (Phase 332) — multi-file loop POST commit + catch DbUpdateException friendly

### Existing Patterns to Preserve
- `Controllers/CDPController.cs:2432` `[Authorize(Roles = UserRoles.RolesCoachAndAbove)]`
- `Controllers/CDPController.cs:2441-2453` active mapping guard (Coach-only restriction TempData friendly)
- `Controllers/CDPController.cs:2455` `await using var tx = await _context.Database.BeginTransactionAsync()`
- `Controllers/CDPController.cs:2478-2487` JSON parse EvidencePathHistory inner try/catch warn-only
- `Controllers/CDPController.cs:2505-2517` progress revert state (Status, ApprovalStatus, RejectedAt, etc) — DB mutation INSIDE tx
- `Controllers/CDPController.cs:2518` RecordStatusHistory call — DB mutation INSIDE tx
- `Controllers/CDPController.cs:2536` `_auditLog.LogAsync` — INSIDE tx
- `Controllers/CDPController.cs:2521-2529` Status="Approved" + sibling sessions branches — verbatim preserve

### Base Infrastructure
- `Controllers/CDPController.cs` — `_context`, `_userManager`, `_auditLog`, `_logger`, `_env`, `RecordStatusHistory` all available

### Project Workflow
- `CLAUDE.md` — Bahasa Indonesia response default
- `docs/IT_NOTIFY.md` — batch IT notification (append Phase 333 entry)

### v19.0 Milestone Context
- `.planning/phases/331-fix-cascade-deletetraining-deletemanualassessment-atomicity/331-CONTEXT.md` — atomicity tx wrap pattern
- `.planning/phases/332-fix-cascade-deletebagian-file-atomicity/332-CONTEXT.md` — multi-file loop + catch friendly pattern

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets (Phase 331/332 pattern)
- `await using var tx` disposal auto-rollback — no explicit RollbackAsync needed (D-03)
- File.Delete inner try/catch warn-only per file — no break loop, log + continue
- catch DbUpdateException + catch Exception fallback Json/TempData friendly (Phase 332 D-04 pattern)
- File path collection BEFORE actual File.Delete — defer operation past commit boundary

### Differences from Phase 331/332
- Existing BeginTransactionAsync sudah ada (L2455) — TIDAK perlu tambah baru, reorganize saja
- File path collection nested di dalam conditional cleanup branch (only when no sibling sessions + status ≠ Approved) — outer-scope variable accessibility needed
- Progress revert state logic (~13 fields) MUST tetap INSIDE tx — file delete doesn't influence DB state mutations
- JSON parse EvidencePathHistory inner try/catch — preserve verbatim (already correct pattern)
- RecordStatusHistory call — DB mutation, INSIDE tx
- Catch refactor more complex: BOTH DbUpdateException specific + Exception fallback (vs Phase 332 single catch)

### Integration Points
- `Microsoft.EntityFrameworkCore` `DbUpdateException` + `BeginTransactionAsync` — already imported
- `System.Text.Json.JsonSerializer` — already imported (EvidencePathHistory parse)
- `Path.DirectorySeparatorChar` + cross-platform path normalization — already pattern (L2495)

</code_context>

<specifics>
## Specific Ideas

- **pathsToDelete scope:** Declare `List<string>? pathsToDelete = null;` SEBELUM `await using var tx = ...` L2455. Allocate inside conditional branch L2467 (no other sessions + status ≠ Approved). Read POST tx.CommitAsync.
- **File.Delete loop position:** SETELAH `await tx.CommitAsync()` (L2538) tapi SEBELUM `TempData["Success"]` (L2541) and `return RedirectToAction` (L2542).
- **Catch order:** DbUpdateException FIRST (specific), Exception SECOND (general fallback) — C# catch precedence order.
- **NO explicit `tx.RollbackAsync()`** di catch blocks — `await using` disposal handles rollback. Konsisten Phase 331/332 pattern.
- **NO `throw`** di catch — replace dengan return RedirectToAction friendly.
- **cleanupNote pattern preserved** — count akurat from pathsToDelete.Count at INSIDE tx build time (even though deletion is post-commit, count is known).
- **Cross-platform path normalization** `Replace('/', Path.DirectorySeparatorChar)` — preserve verbatim L2495.
- **JSON history parse** EvidencePathHistory — preserve inner try/catch warn-only L2478-2487 verbatim.
- **Status branches (Approved, sibling exists)** — preserve verbatim, no File.Delete in those branches.

</specifics>

<deferred>
## Deferred Ideas

- ❌ Integration test xUnit DeleteCoachingSession multi-scenario — out of scope.
- ❌ Cron janitor cleanup orphan file — Phase 999.x backlog.
- ❌ Refactor JSON history parse ke shared helper — scope creep.
- ❌ Progress revert state ke service layer — scope creep, design Phase 297 territory.
- ❌ Phase 334 Kompetensi + Phase 335 Worker — separate phase per roadmap.

</deferred>

---

*Phase: 333-fix-cascade-deletecoachingsession-file-atomicity*
*Context gathered: 2026-05-28 via Phase 328 RESEARCH-derived (audit-as-PRD), --auto mode single-pass*
*Source: Phase 328 RESEARCH commit `41f1eef2` §4.6 + §9 proposal #4*
*Pattern: Phase 331 D-03 + Phase 332 D-04 (file delete post-commit + catch DbUpdateException friendly) + complex existing tx reuse*
