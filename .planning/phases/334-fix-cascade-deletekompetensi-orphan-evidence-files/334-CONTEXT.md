# Phase 334: Fix Cascade DeleteKompetensi Orphan Evidence Files - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Discuss mode:** auto (--auto flag, single-pass, all recommended defaults selected)

<domain>
## Phase Boundary

Phase 334 menutup **1 HIGH finding** Phase 328 Cascade Audit Sweep §4.8 + §9 proposal #5: orphan EvidencePath files + info leak `ex.Message` to client di `DeleteKompetensi`.

**Scope endpoint:**
1. `DeleteKompetensi` — `Controllers/ProtonDataController.cs:1516` — D2+D6:
   - D2: L1559 `ProtonDeliverableProgresses.RemoveRange` TIDAK cleanup `EvidencePath` + `EvidencePathHistory` (JSON list) physical files → orphan disk
   - D6: L1588 `Json(new { success = false, message = "Gagal menghapus: " + ex.Message })` → DB constraint detail / table names / exception type BOCOR ke client

**Existing OK (preserve verbatim):**
- `BeginTransactionAsync` L1529 sudah ada (using var transaction, sync disposal)
- 5-step cascade L1542-1574: CoachingSessions → ProtonDeliverableProgresses → Deliverables → SubKompetensi → Kompetensi (FK Restrict order preserved)
- Audit log L1578-1580 POST CommitAsync — TIDAK pindah (out of scope D3 positioning)
- Authorization implicit (no Authorize attribute on this controller — out of scope)

**Existing problems to fix:**
- D2: No EvidencePath collection before ProtonDeliverableProgresses RemoveRange
- D6: `ex.Message` leak via Json error

**Files modified:**
- `Controllers/ProtonDataController.cs` (~40 LoC delta, 1 endpoint)

**Zero schema change. Zero migration. Zero model change. Zero view change.**

</domain>

<decisions>
## Implementation Decisions

### D-01 Source-of-truth: Phase 328 RESEARCH.md §4.8 + §9 proposal #5

Lock seluruh design ke audit deliverable Phase 328 commit `41f1eef2`:
- §4.8: DeleteKompetensi HIGH D2 (orphan EvidencePath) + D6 polish (info leak `ex.Message`)
- §9 #5: `fix-cascade-deletekompetensi-orphan-evidence-files`

### D-02 Evidence Path Collection — Build evidencePaths INSIDE tx, Defer File.Delete POST Commit

Strategi (pattern Phase 333 D-02 reuse):
1. Declare `List<string>? evidencePaths = null;` SEBELUM `using var transaction = ...` L1529
2. Allocate `evidencePaths = new List<string>();` INSIDE tx, SEBELUM `RemoveRange(progresses)` L1559
3. Loop semua progresses, collect `EvidencePath` string + JSON parse `EvidencePathHistory` (per progress entry)
4. File.Delete loop POST `CommitAsync` L1576 — inner try/catch warn-only per file

Penting: collection happens INSIDE tx (before RemoveRange detach), File.Delete OUTSIDE tx (after commit).

### D-03 JSON Parse Pattern — Phase 333 D-02 Verbatim Reuse

```csharp
if (progresses.Any())
{
    evidencePaths = new List<string>();
    foreach (var p in progresses)
    {
        if (!string.IsNullOrEmpty(p.EvidencePath))
            evidencePaths.Add(p.EvidencePath);
        if (!string.IsNullOrEmpty(p.EvidencePathHistory))
        {
            try
            {
                var history = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(p.EvidencePathHistory) ?? new List<string>();
                evidencePaths.AddRange(history);
            }
            catch (Exception jex)
            {
                _logger.LogWarning(jex, "Failed to parse EvidencePathHistory for progress {Pid}", p.Id);
            }
        }
    }
}
```

Same pattern Phase 333 (DeleteCoachingSession) — verbatim reuse.

### D-04 Reorganize Cascade — Collect Paths SEBELUM RemoveRange Progresses

Current L1554-1561:
```csharp
if (progressIds.Any())
{
    var progresses = await _context.ProtonDeliverableProgresses
        .Where(p => progressIds.Contains(p.Id))
        .ToListAsync();
    _context.ProtonDeliverableProgresses.RemoveRange(progresses);
    await _context.SaveChangesAsync();
}
```

Refactor (insert evidencePaths collection between ToListAsync dan RemoveRange):
```csharp
if (progressIds.Any())
{
    var progresses = await _context.ProtonDeliverableProgresses
        .Where(p => progressIds.Contains(p.Id))
        .ToListAsync();

    // Phase 334 D-02 + D-03: collect evidence paths SEBELUM RemoveRange (progress object detach post-Remove)
    evidencePaths = new List<string>();
    foreach (var p in progresses) { /* ... JSON parse ... */ }

    _context.ProtonDeliverableProgresses.RemoveRange(progresses);
    await _context.SaveChangesAsync();
}
```

### D-05 File.Delete Loop POST CommitAsync — Phase 333 D-05 Verbatim Reuse

```csharp
// AFTER await transaction.CommitAsync() L1576 — BEFORE audit log L1578
if (evidencePaths != null && evidencePaths.Count > 0)
{
    foreach (var relUrl in evidencePaths)
    {
        try
        {
            var physical = Path.Combine(_env.WebRootPath,
                relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
        }
        catch (Exception fex)
        {
            _logger.LogWarning(fex, "File.Delete post-commit failed (Kompetensi evidence): {Path}", relUrl);
        }
    }
}
```

Wait — placement timing: Audit log L1578 POST commit. File.Delete juga POST commit. Choose order:
- File.Delete BEFORE audit log: audit log might fail tapi file sudah dibersih (good for compliance)
- Audit log BEFORE File.Delete: file dibersih reflects audit truth (file deletion is intent, audit captures intent)

[auto] **File.Delete AFTER audit log** — audit log records "Deleted Kompetensi" first as system-of-record event, then physical cleanup. Same order as Phase 333 (audit INSIDE tx, file delete POST). Consistency.

Final order:
1. `await transaction.CommitAsync();` (L1576)
2. `await _auditLog.LogAsync(...)` (L1578-1580, existing position)
3. File.Delete loop (new, POST audit)
4. `return Json(new { success = true });` (L1582)

### D-06 Catch Refactor — DbUpdateException + Exception Fallback, NO ex.Message Leak

Current L1584-1589:
```csharp
catch (Exception ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "Failed to delete Kompetensi {Id}", req.KompetensiId);
    return Json(new { success = false, message = "Gagal menghapus: " + ex.Message });
}
```

**CRITICAL D6 fix:** Remove `+ ex.Message` from client response. Use generic message.

Refactor:
```csharp
catch (DbUpdateException dbEx)
{
    // Phase 334 D-06: using var transaction disposal auto-rollback — no explicit RollbackAsync needed
    _logger.LogWarning(dbEx, "DbUpdate failed for DeleteKompetensi {Id}", req.KompetensiId);
    return Json(new { success = false, message = "Gagal hapus kompetensi: ada constraint database yang dilanggar." });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to delete Kompetensi {Id}", req.KompetensiId);
    return Json(new { success = false, message = "Gagal hapus kompetensi: terjadi kesalahan internal. Hubungi admin." });
}
```

Hapus `await transaction.RollbackAsync()` — `using var transaction` synchronous disposal handles rollback. Hapus `+ ex.Message` di response message (D6 info leak fix).

### D-07 Audit Log Position — Preserve POST CommitAsync (Out of Scope D3)

[auto] Audit log L1578-1580 SAAT INI POST `await transaction.CommitAsync()` L1576. Phase 332/333 pattern audit INSIDE tx, tapi Phase 334 scope = D2+D6 only (NOT D3 positioning).

Keep audit log verbatim di L1578-1580. Kalau audit log fail post-commit, Json success=true tetap return (existing behavior — acceptable trade-off untuk minimal scope phase).

### D-08 IT_NOTIFY Strategy — Bundle v19.0

[auto] Sama dengan Phase 329-333: ship lokal, BUNDLE batch push ke origin/main bersama Phase 325+...+334 saat user release push lock per Phase 327 option-b hold. Append entry ke `docs/IT_NOTIFY.md` Phase 334 section + smoke scenario #12.

### D-09 Acceptance Criteria (Locked)

1. `DeleteKompetensi` L1516-1590: evidencePaths declared OUTER tx (List<string>? nullable).
2. evidencePaths populate INSIDE tx (loop progresses, JSON parse EvidencePathHistory inner try/catch warn-only) SEBELUM RemoveRange progresses L1559.
3. 5-step cascade L1542-1574 preserved verbatim (CoachingSessions → Progresses → Deliverables → SubKompetensi → Kompetensi).
4. `await transaction.CommitAsync()` L1576 preserved.
5. Audit log L1578-1580 preserved verbatim POST commit.
6. File.Delete loop ADDED POST audit log dengan inner try/catch warn-only per file.
7. Catch refactor: catch DbUpdateException specific + catch Exception fallback. NO `+ ex.Message` di Json response. NO explicit `transaction.RollbackAsync` (using disposal handles).
8. `dotnet build` 0 error CS*.
9. `dotnet test --no-build` 18/18 PASS.
10. Manual smoke deferred ke Dev promo (scenario #12).
11. Commit: `feat(334): cascade orphan evidence DeleteKompetensi (File.Delete post-commit + catch friendly no info leak)`.
12. SUMMARY.md generated.

### D-10 Plan Structure — Single Plan, 2 Task Wave

1 PLAN.md (`334-01-PLAN.md`) dengan 2 task:
- Task 1: Refactor `DeleteKompetensi` di ProtonDataController.cs (~40 LoC) — evidencePaths outer var, populate inside tx, File.Delete loop POST commit, catch refactor friendly
- Task 2: Verify build + test + grep AC + IT_NOTIFY + commit + SUMMARY

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning.**

### Source Truth (Audit-Derived)
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md` §4.8 (DeleteKompetensi HIGH D2+D6) + §9 proposal #5

### Gold Standard Pattern References
- `Controllers/ProtonDataController.cs:1516-1590` `DeleteKompetensi` — current code (target modification)
- `Controllers/CDPController.cs:2433-2575` `DeleteCoachingSession` (Phase 333) — pattern verbatim reuse (declare outer + collect inside tx + File.Delete post-commit + catch friendly)
- `Controllers/DocumentAdminController.cs:283-396` `DeleteBagian` (Phase 332) — multi-file loop + catch DbUpdateException friendly

### Existing Patterns to Preserve
- `Controllers/ProtonDataController.cs:1521-1524` Include SubKompetensi.Deliverables (preload graph)
- `Controllers/ProtonDataController.cs:1529` `using var transaction = await _context.Database.BeginTransactionAsync()`
- `Controllers/ProtonDataController.cs:1542-1574` 5-step cascade order (FK Restrict-safe)
- `Controllers/ProtonDataController.cs:1576` `await transaction.CommitAsync()`
- `Controllers/ProtonDataController.cs:1578-1580` audit log POST commit

### v19.0 Milestone Context
- `.planning/phases/333-fix-cascade-deletecoachingsession-file-atomicity/333-CONTEXT.md` D-02..D-05 — pattern verbatim reuse (JSON parse, File.Delete POST commit, outer scope nullable list)

### Project Workflow
- `CLAUDE.md` — Bahasa Indonesia response default
- `docs/IT_NOTIFY.md` — batch IT notification (append Phase 334 entry)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets (Phase 333 pattern verbatim)
- `List<string>? evidencePaths = null;` outer tx scope (nullable, visible POST commit)
- `System.Text.Json.JsonSerializer.Deserialize<List<string>>` inner try/catch warn-only
- File.Delete inner try/catch warn-only per file (no break loop)
- Path.Combine + TrimStart('/') + Replace('/', Path.DirectorySeparatorChar) cross-platform pattern
- catch DbUpdateException specific + catch Exception fallback (Phase 332/333 D-03)

### Differences from Phase 333
- Existing tx is `using var` (synchronous disposal), bukan `await using var` — same effect (auto-rollback on exception escape)
- Existing audit log POST CommitAsync (Phase 333 audit INSIDE tx) — out of scope to move per Phase 328 §4.8 (D2+D6 only)
- Response is Json (AJAX-only), bukan TempData+Redirect — friendly message wrapping Json error
- `ex.Message` leak via `"Gagal menghapus: " + ex.Message` — CRITICAL D6 info leak, must remove from client response

### Integration Points
- `Microsoft.EntityFrameworkCore` `DbUpdateException` + `BeginTransactionAsync` — already imported
- `System.Text.Json.JsonSerializer` — already imported (other endpoints in same controller use it)
- `Path.DirectorySeparatorChar` — already pattern (CDPController L2495)

</code_context>

<specifics>
## Specific Ideas

- **evidencePaths scope:** Declare `List<string>? evidencePaths = null;` SEBELUM `using var transaction = ...` L1529. Allocate + populate INSIDE tx (BEFORE RemoveRange progresses L1559).
- **JSON parse error log format:** `_logger.LogWarning(jex, "Failed to parse EvidencePathHistory for progress {Pid}", p.Id)` — verbatim Phase 333 pattern.
- **File.Delete loop position:** POST audit log L1578-1580 (audit first, then physical cleanup, then return Json). Consistent ordering: DB → audit → file → response.
- **File.Delete log format:** `_logger.LogWarning(fex, "File.Delete post-commit failed (Kompetensi evidence): {Path}", relUrl)` — distinguishes phase context.
- **Catch order:** DbUpdateException FIRST (specific), Exception SECOND (general fallback).
- **NO `await transaction.RollbackAsync()`** — `using var transaction` synchronous disposal handles it.
- **NO `+ ex.Message`** di Json response — D6 info leak fix. Replace dengan generic message untuk both catch blocks.
- **Json error message 2 variants:** "Gagal hapus kompetensi: ada constraint database yang dilanggar." (DbUpdateException) + "Gagal hapus kompetensi: terjadi kesalahan internal. Hubungi admin." (Exception fallback).
- **NO new test file:** dotnet test --no-build cukup.
- **NO migration, NO schema change, NO model change.**

</specifics>

<deferred>
## Deferred Ideas

- ❌ Move audit log INSIDE tx (D3 positioning) — out of scope Phase 328 §4.8 (D2+D6 only).
- ❌ Add `[Authorize]` attribute — existing convention (controller-level or no auth). Out of scope.
- ❌ Integration test xUnit DeleteKompetensi nested tree — out of scope.
- ❌ Cron janitor cleanup orphan evidence — Phase 999.x backlog.
- ❌ Refactor JSON parse ke shared helper — scope creep.
- ❌ Phase 335 Worker — separate phase per roadmap (HIGH L, kompleks).

</deferred>

---

*Phase: 334-fix-cascade-deletekompetensi-orphan-evidence-files*
*Context gathered: 2026-05-28 via Phase 328 RESEARCH-derived (audit-as-PRD), --auto mode single-pass*
*Source: Phase 328 RESEARCH commit `41f1eef2` §4.8 + §9 proposal #5*
*Pattern: Phase 333 D-02..D-05 verbatim reuse (outer-scope nullable list + INSIDE tx collection + File.Delete POST commit + catch friendly no info leak)*
