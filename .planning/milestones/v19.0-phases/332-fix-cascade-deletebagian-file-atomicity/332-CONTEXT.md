# Phase 332: Fix Cascade DeleteBagian File Atomicity - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Discuss mode:** auto (--auto flag, single-pass, all recommended defaults selected)

<domain>
## Phase Boundary

Phase 332 menutup **1 HIGH finding** Phase 328 Cascade Audit Sweep §4.7 + §9 proposal #3: file-DB atomicity di `DeleteBagian` via `BeginTransactionAsync` wrap + reorder `System.IO.File.Delete` (2 collection KKJ + CPDP archived) POST `CommitAsync` + try/catch DbUpdateException.

**Scope endpoint:**
1. `DeleteBagian` — `Controllers/DocumentAdminController.cs:283` — D2+D6+D7:
   - L327-328 + L342-343 `System.IO.File.Delete` SEBELUM `SaveChangesAsync` L350
   - No try/catch DbUpdateException around L350 → FK violation raw 500
   - No `BeginTransactionAsync`

**Existing OK (preserve):**
- Pre-check active files BLOCK L289-302 (returns Json blocked)
- Confirm dialog L308-317 (needsConfirm Json)
- Audit log L359-364 (already wrapped in inner try/catch — Phase 332 D-04 preserve)
- AJAX-only response pattern (Json success/failure — bukan dual-path TempData)

**Files modified:**
- `Controllers/DocumentAdminController.cs` (~50 LoC delta, 1 endpoint)

**Zero schema change. Zero migration. Zero model change. Zero view change. Zero service injection.**

</domain>

<decisions>
## Implementation Decisions

### D-01 Source-of-truth: Phase 328 RESEARCH.md §4.7 + §9 proposal #3

Lock seluruh design ke audit deliverable Phase 328 commit `41f1eef2`:
- §4.7: DeleteBagian HIGH D2+D6+D7
- §9 #3: `fix-cascade-deletebagian-file-atomicity`

### D-02 Transaction Scope

Wrap dalam `using var tx = await _context.Database.BeginTransactionAsync();`:
- ✅ INCLUDE: `RemoveRange(archivedKkjFiles)` + `RemoveRange(archivedCpdpFiles)` + `Remove(bagianEntity)` + `SaveChangesAsync` + audit log `LogAsync`
- ❌ EXCLUDE: Pre-check active files + confirm dialog (early return BEFORE tx scope)
- ❌ EXCLUDE: File path collection (string list assignment)
- ❌ EXCLUDE: `System.IO.File.Delete` loop (POST `CommitAsync`)

[auto] Audit log INSIDE tx → konsisten Phase 323 D-04 + Phase 331 D-02 pattern. Audit log existing sudah punya inner try/catch L354 — preserve verbatim.

### D-03 File Path Collection Pattern

Capture 2 list path string variables SEBELUM tx scope:

```csharp
// Collect archived KKJ + CPDP file paths SEBELUM tx — string list value-typed, safe post-commit
var archivedKkjFiles = await _context.KkjFiles
    .Where(f => f.OrganizationUnitId == id && f.IsArchived)
    .ToListAsync();
var kkjPaths = archivedKkjFiles
    .Where(f => !string.IsNullOrEmpty(f.FilePath))
    .Select(f => Path.Combine(_env.WebRootPath, f.FilePath.TrimStart('/')))
    .ToList();

var archivedCpdpFiles = await _context.CpdpFiles
    .Where(f => f.OrganizationUnitId == id && f.IsArchived)
    .ToListAsync();
var cpdpPaths = archivedCpdpFiles
    .Where(f => !string.IsNullOrEmpty(f.FilePath))
    .Select(f => Path.Combine(_env.WebRootPath, f.FilePath.TrimStart('/')))
    .ToList();
```

### D-04 File.Delete Reorder Pattern — POST CommitAsync

```csharp
using var tx = await _context.Database.BeginTransactionAsync();
try
{
    if (archivedKkjFiles.Any()) _context.KkjFiles.RemoveRange(archivedKkjFiles);
    if (archivedCpdpFiles.Any()) _context.CpdpFiles.RemoveRange(archivedCpdpFiles);
    _context.OrganizationUnits.Remove(bagianEntity);
    await _context.SaveChangesAsync();
    
    // Audit log INSIDE tx — preserve existing pattern L353-364 verbatim
    var currentUser = await _userManager.GetUserAsync(User);
    try
    {
        var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
            ? (currentUser?.FullName ?? "Unknown")
            : $"{currentUser.NIP} - {currentUser.FullName}";
        await _auditLog.LogAsync(
            currentUser?.Id ?? "", actorName, "DeleteBagian",
            $"Deleted bagian '{bagianEntity.Name}' (ID {id}). Cascaded {totalArchived} archived file(s) (KKJ: {archivedKkjCount}, CPDP: {archivedCpdpCount}).",
            id, "OrganizationUnit");
    }
    catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeleteBagian (bagianId={Id})", id); }
    
    await tx.CommitAsync();
}
catch (DbUpdateException ex)
{
    // tx auto-rollback via using disposal
    _logger.LogWarning(ex, "Delete failed for Bagian (bagianId={Id})", id);
    return Json(new { success = false, message = "Gagal hapus bagian: ada constraint database yang dilanggar." });
}

// POST commit: File.Delete loop dengan inner try/catch warn-only per file
foreach (var path in kkjPaths)
{
    try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    catch (Exception ex) { _logger.LogWarning(ex, "File.Delete post-commit failed (KKJ): {Path}", path); }
}
foreach (var path in cpdpPaths)
{
    try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    catch (Exception ex) { _logger.LogWarning(ex, "File.Delete post-commit failed (CPDP): {Path}", path); }
}

return Json(new { success = true, message = $"Bagian '{bagianEntity.Name}' berhasil dihapus." });
```

### D-05 Pre-Check Active Files + Confirm Dialog — Preserve Verbatim

[auto] Pre-check active files L289-302 + confirm dialog L308-317 TIDAK disentuh. Posisi tetap di OUTSIDE tx scope. Pre-check fail = early return Json blocked TANPA tx creation.

### D-06 Audit Log Position + Inner Try/Catch — Preserve Verbatim

[auto] Audit log L353-364 INSIDE tx scope (sebelum CommitAsync). Inner try/catch (Exception ex) wrap audit log call preserved verbatim — kalau audit log fail, jangan rollback DB (log warning + continue commit).

### D-07 File.Delete Failure Handling — Per File Warn-Only

[auto] Kalau salah satu file fail di-delete POST commit:
- Log warning per file (path + exception)
- Continue iterate file lain (jangan break loop)
- DB sudah committed = success response Json tetap dikirim
- Orphan file di disk = acceptable (manual cleanup later)

### D-08 AJAX Response Pattern — Json Success/Failure

[auto] DeleteBagian = AJAX-only (Json response, bukan dual-path). Preserve pattern:
- Success: `Json(new { success = true, message = "..." })`
- DbUpdateException catch: `Json(new { success = false, message = "..." })`
- Pre-check fail: `Json(new { success = false, blocked = true, message = "..." })` (existing)
- Confirm needed: `Json(new { success = false, needsConfirm = true, ... })` (existing)

### D-09 IT_NOTIFY Strategy — Bundle v19.0

[auto] Sama dengan Phase 329-331 D-07/D-08: ship lokal, BUNDLE batch push ke origin/main bersama Phase 325+326+327+329+330+331+332 saat user release push lock per Phase 327 option-b hold. Append entry ke `docs/IT_NOTIFY.md` Phase 332 section.

### D-10 Acceptance Criteria (Locked)

1. `DeleteBagian` L283-365: tx wrap RemoveRange+Remove+SaveChanges+AuditLog, File.Delete (KKJ+CPDP loops) POST `CommitAsync` dengan inner try/catch per file.
2. Pre-check active files L289-302 + confirm dialog L308-317 preserved verbatim (OUTSIDE tx).
3. Audit log L353-364 preserved verbatim (INSIDE tx, inner try/catch wrap).
4. Catch DbUpdateException baru → Json success=false friendly message.
5. `dotnet build` clean (0 error CS*).
6. `dotnet test --no-build` 18/18 pass (no regression Phase 331 baseline).
7. Manual smoke (deferred ke Dev promo): 
   - Delete Bagian zero archived files → Json success + bagian gone
   - Delete Bagian dengan 2 archived KKJ + 1 archived CPDP → tx commit + 3 files deleted
   - Simulasi DB FK violation → tx rollback + Json success=false + files TETAP ada
8. Commit: `feat(332): cascade atomicity DeleteBagian (tx wrap + File.Delete post-commit KKJ+CPDP)`.
9. SUMMARY.md generated.

### D-11 Plan Structure — Single Plan, 2 Task Wave

1 PLAN.md (`332-01-PLAN.md`) dengan 2 task:
- Task 1: Refactor `DeleteBagian` di DocumentAdminController.cs (~50 LoC) — capture paths, tx wrap, reorder File.Delete loops, add catch DbUpdateException
- Task 2: Verify build + test + grep AC + IT_NOTIFY append + commit + SUMMARY

Sequential dependency: Task 2 depends Task 1.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning.**

### Source Truth (Audit-Derived)
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md` §4.7 (DeleteBagian HIGH) + §9 proposal #3

### Gold Standard Pattern References
- `Controllers/DocumentAdminController.cs:283-367` `DeleteBagian` — current code (target modification)
- `Controllers/TrainingAdminController.cs:559-625` `DeleteTraining` (Phase 331) — atomicity pattern reuse (single file collection)
- `Controllers/TrainingAdminController.cs:813-868` `DeleteManualAssessment` (Phase 331) — atomicity pattern reuse (different helper)

### Existing Patterns to Preserve
- `Controllers/DocumentAdminController.cs:289-302` active files BLOCK pre-check (Json blocked)
- `Controllers/DocumentAdminController.cs:308-317` archived files confirm dialog (Json needsConfirm)
- `Controllers/DocumentAdminController.cs:353-364` audit log block dengan inner try/catch

### Base Infrastructure
- `Controllers/AdminBaseController.cs` — `_context`, `_userManager`, `_auditLog`, `_logger`, `_env` semua tersedia via base inheritance (DocumentAdminController inherits)

### Project Workflow
- `CLAUDE.md` — Bahasa Indonesia response default, dev workflow, seed workflow
- `docs/IT_NOTIFY.md` — batch IT notification (append Phase 332 entry post-ship)

### v19.0 Milestone Context
- `.planning/phases/331-fix-cascade-deletetraining-deletemanualassessment-atomicity/331-CONTEXT.md` D-02..D-07 — pattern reuse (tx scope, file capture, reorder, warn-only)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_context.Database.BeginTransactionAsync()` — EF Core 8 standard pattern, `using var tx` disposal auto-rollback (precedent Phase 331)
- `_auditLog.LogAsync(userId, actorName, action, detail, entityId, entityType)` — standard audit pattern sudah dipakai L359
- `_userManager.GetUserAsync(User)` — sudah dipakai L353
- `Path.Combine(_env.WebRootPath, FilePath.TrimStart('/'))` — pattern verbatim L326 + L342

### Established Patterns (Phase 331 reuse)
- File path capture string variable BEFORE Remove (kkjPaths + cpdpPaths lists)
- tx.CommitAsync() SEBELUM File.Delete
- Inner try/catch (Exception ex) warn-only per File.Delete operation
- catch (DbUpdateException ex) outer dengan friendly Json error response

### Integration Points
- `Microsoft.EntityFrameworkCore` `BeginTransactionAsync` + `DbUpdateException` — already imported
- `System.IO.File` + `Path.Combine` — System namespace, no import needed
- `Json(...)` return pattern — already established di endpoint (5 existing branches)

</code_context>

<specifics>
## Specific Ideas

- **Tx scope start:** `using var tx = ...` AT START of try block (sebelum RemoveRange).
- **Catch position:** Catch DbUpdateException WRAP try block, return Json success=false. Outside catch: File.Delete loops (only reached jika commit success).
- **File loop catch INNER:** `try { File.Delete } catch (Exception ex) { _logger.LogWarning(...) }` per file — JANGAN throw, JANGAN break loop.
- **AJAX-only:** Endpoint sudah Json-pure (5 existing Json returns). Preserve pattern di catch baru.
- **Pre-check + confirm:** Lompat ke OUTSIDE tx (early return Json sebelum file collection + tx).
- **File collection ToListAsync:** Sudah ada existing L319-321 + L334-336. Pertahankan, hanya extract path string ke list terpisah.
- **NO new test file:** Re-run `dotnet test --no-build` cukup.
- **NO migration, NO schema change, NO model change.**

</specifics>

<deferred>
## Deferred Ideas

- ❌ Integration test xUnit untuk DeleteBagian full lifecycle — out of scope.
- ❌ Cron janitor untuk cleanup orphan file post-fail — Phase 999.x backlog.
- ❌ Refactor pre-check active files BLOCK ke shared helper method — scope creep dari mechanical fix.
- ❌ Phase 333 CoachingSession + Phase 334 Kompetensi + Phase 335 Worker — separate phase per roadmap.

</deferred>

---

*Phase: 332-fix-cascade-deletebagian-file-atomicity*
*Context gathered: 2026-05-28 via Phase 328 RESEARCH-derived (audit-as-PRD), --auto mode single-pass*
*Source: Phase 328 RESEARCH commit `41f1eef2` §4.7 + §9 proposal #3*
*Pattern: Phase 331 D-02..D-07 reuse (atomicity tx wrap + File.Delete post-commit)*
