# Phase 331: Fix Cascade DeleteTraining + DeleteManualAssessment Atomicity - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Discuss mode:** auto (--auto flag, single-pass, all recommended defaults selected)

<domain>
## Phase Boundary

Phase 331 menutup **2 HIGH finding** Phase 328 Cascade Audit Sweep (§4.1 + §4.2 + §9 proposal #1): file-DB atomicity di `DeleteTraining` + `DeleteManualAssessment` via `BeginTransactionAsync` wrap + reorder `File.Delete` POST `CommitAsync`.

**Scope endpoint:**
1. `DeleteTraining` — `Controllers/TrainingAdminController.cs:559` — D2+D7: `System.IO.File.Delete` L585 SEBELUM `SaveChangesAsync` L590; no `BeginTransactionAsync`
2. `DeleteManualAssessment` — `Controllers/TrainingAdminController.cs:793` — D2+D7: `FileUploadHelper.DeleteFile` L816 SEBELUM `SaveChangesAsync` L820; no `BeginTransactionAsync`

**D5 sudah Phase 325 P05 covered** (pre-check renewal L568-580 + L802-805). D6 catch DbUpdateException sudah ada (L598 + L828). Phase 331 fokus D2+D7 only.

**Files modified:**
- `Controllers/TrainingAdminController.cs` (~80 LoC delta, 2 endpoint)

**Zero schema change. Zero migration. Zero model change. Zero view change. Zero service injection.**

</domain>

<decisions>
## Implementation Decisions

### D-01 Source-of-truth: Phase 328 RESEARCH.md §4.1 + §4.2 + §9 proposal #1

Lock seluruh design ke audit deliverable Phase 328 commit `41f1eef2`:
- §4.1: DeleteTraining D2 file-DB atomicity broken
- §4.2: DeleteManualAssessment D2 file-DB atomicity broken
- §9 #1: `fix-cascade-deletetraining-deletemanualassessment-atomicity`

### D-02 Transaction Scope

Wrap dalam `using var tx = await _context.Database.BeginTransactionAsync();`:
- ✅ INCLUDE: `Remove` + `SaveChangesAsync` + audit log `LogAsync`
- ❌ EXCLUDE: pre-check renewal queries (read-only, no need tx)
- ❌ EXCLUDE: file path capture (string variable assignment)
- ❌ EXCLUDE: `File.Delete` (must be POST `CommitAsync`)

[auto] Audit log INSIDE tx → konsisten dengan Phase 323 D-04 pattern. Kalau audit log fail = rollback DB juga, prevent partial state (record removed tapi audit gone).

### D-03 File.Delete Reorder Pattern

**DeleteTraining (System.IO.File.Delete):**

```csharp
// 1. Capture file path SEBELUM Remove (record akan detached)
string? sertifikatPath = null;
if (!string.IsNullOrEmpty(record.SertifikatUrl))
{
    sertifikatPath = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
}

using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // Pre-check renewal (existing L568-580) — TIDAK pindah, biarkan di luar tx
    // ...
    
    var actor = await _userManager.GetUserAsync(User);
    _context.TrainingRecords.Remove(record);
    await _context.SaveChangesAsync();
    
    if (actor != null)
        await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
            $"Training record dihapus: {record.Judul}", record.Id, "TrainingRecord");
    
    await tx.CommitAsync();
    
    // 2. File.Delete POST commit — kalau fail, log warning tapi tx udah commit (no rollback)
    if (sertifikatPath != null && System.IO.File.Exists(sertifikatPath))
    {
        try { System.IO.File.Delete(sertifikatPath); }
        catch (Exception ex) { _logger.LogWarning(ex, "File.Delete post-commit failed: {Path}", sertifikatPath); }
    }
    
    TempData["Success"] = "Training record berhasil dihapus.";
}
catch (DbUpdateException ex)
{
    // tx auto-rollback via using disposal
    _logger.LogWarning(ex, "Delete failed for TrainingRecord {Id}", record.Id);
    TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
}
```

**DeleteManualAssessment (FileUploadHelper.DeleteFile):**

```csharp
// 1. Capture file URL SEBELUM Remove
string? manualSertifikatUrl = session.ManualSertifikatUrl;

using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // Pre-check renewal (existing L802-805) — biarkan di luar tx
    // ...
    
    var actor = await _userManager.GetUserAsync(User);
    _context.AssessmentSessions.Remove(session);
    await _context.SaveChangesAsync();
    
    if (actor != null)
        await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
            $"Assessment manual dihapus: {session.Title}", session.Id, "AssessmentSession");
    
    await tx.CommitAsync();
    
    // 2. File.Delete POST commit
    if (!string.IsNullOrEmpty(manualSertifikatUrl))
    {
        try { FileUploadHelper.DeleteFile(_env.WebRootPath, manualSertifikatUrl); }
        catch (Exception ex) { _logger.LogWarning(ex, "FileUploadHelper.DeleteFile post-commit failed: {Url}", manualSertifikatUrl); }
    }
    
    TempData["Success"] = "Assessment manual berhasil dihapus.";
}
catch (DbUpdateException ex)
{
    _logger.LogWarning(ex, "Delete failed for AssessmentSession (manual) {Id}", session.Id);
    TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
}
```

### D-04 Pre-Check Renewal Position — KEEP at L568-580 + L802-805

[auto] Pre-check renewal (existing Phase 325 P05) tetap di POSISI semula (sebelum tx scope). Alasan:
- Pre-check = read-only count query, tidak butuh tx
- Early return TempData friendly tanpa tx overhead
- Kalau pre-check pindah masuk tx, tx scope membesar tanpa benefit

Pre-check return RedirectToAction = early exit BEFORE tx creation = no tx leak.

### D-05 File.Delete Failure Handling — Log Warning, Don't Rollback

[auto] Kalau `File.Delete` fail POST `CommitAsync`:
- DB sudah committed (record gone)
- File orphan di disk = ACCEPTABLE (cleanup manual via janitor cron later)
- `_logger.LogWarning` log path + exception
- TempData["Success"] tetap ditampilkan (DB perspective = succeed)

Alternatif "rollback DB kalau file delete fail" REJECTED:
- Tx sudah committed, tidak bisa rollback
- Re-insert record = race condition + audit log duplicate
- Manual recovery lebih clean

### D-06 TempData["Success"] Timing — POST CommitAsync

[auto] `TempData["Success"] = "...";` set SETELAH `tx.CommitAsync()` succeed. Kalau commit fail → catch block override dengan `TempData["Error"]`. Kalau `File.Delete` fail POST commit, success tetap shown (DB perspective benar).

### D-07 Catch Block — Preserve Existing Catch DbUpdateException

[auto] Catch `DbUpdateException` L598 + L828 SUDAH ADA (Phase 325 P05). Pertahankan verbatim:
- Log warning via `_logger.LogWarning`
- `TempData["Error"]` friendly message

Tambahan: `using var tx` disposal pattern auto-rollback kalau catch hit sebelum `CommitAsync`. Tidak perlu explicit `tx.RollbackAsync()` di catch.

### D-08 IT_NOTIFY Strategy — Bundle v19.0

[auto] Sama dengan Phase 329 D-07 + Phase 330 D-07: ship lokal, BUNDLE batch push ke origin/main bersama Phase 325+326+327+329+330+331 saat user release push lock per Phase 327 option-b hold. Append entry ke `docs/IT_NOTIFY.md` Phase 331 section.

### D-09 Acceptance Criteria (Locked)

1. `DeleteTraining` L559-605: `BeginTransactionAsync` wrap Remove+SaveChanges+AuditLog. `System.IO.File.Delete` POST `CommitAsync` dengan try/catch warn-only.
2. `DeleteManualAssessment` L793-834: `BeginTransactionAsync` wrap Remove+SaveChanges+AuditLog. `FileUploadHelper.DeleteFile` POST `CommitAsync` dengan try/catch warn-only.
3. Pre-check renewal L568-580 + L802-805 tetap di posisi semula (OUTSIDE tx).
4. Catch `DbUpdateException` L598 + L828 preserved (warn + TempData["Error"]).
5. `dotnet build` clean. `dotnet test 18/18` pass (no regression from Phase 330 baseline).
6. Manual smoke: 
   - DeleteTraining record sukses → file sertifikat hilang dari disk
   - DeleteTraining record dengan pre-check fail (renewal child) → TempData["Error"], file tetap ada
   - DeleteTraining record dengan DB FK violation simulasi → TempData["Error"], file tetap ada (tx rollback)
   - DeleteManualAssessment 3 scenario serupa
7. Commit: `feat(331): cascade atomicity DeleteTraining + DeleteManualAssessment (tx wrap + File.Delete post-commit)`.
8. SUMMARY.md generated.

### D-10 Plan Structure — Single Plan, 3 Task Wave

1 PLAN.md (`331-01-PLAN.md`) dengan 3 task:
- Task 1: Refactor `DeleteTraining` di TrainingAdminController.cs (~40 LoC) — capture path, tx wrap, reorder File.Delete
- Task 2: Refactor `DeleteManualAssessment` di TrainingAdminController.cs (~40 LoC) — capture path, tx wrap, reorder FileUploadHelper.DeleteFile
- Task 3: Verify `dotnet build` + `dotnet test` + grep AC + manual smoke + IT_NOTIFY append + commit + SUMMARY

Sequential dependency: Task 2 independent dari Task 1 (different endpoint same file). Build+test final di Task 3.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning.**

### Source Truth (Audit-Derived)
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md` §4.1 (DeleteTraining HIGH) + §4.2 (DeleteManualAssessment HIGH) + §9 proposal #1 — root cause + remediation prescription
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-01-SUMMARY.md` — fix proposal row #1

### Gold Standard Pattern References
- `Controllers/TrainingAdminController.cs:559-605` `DeleteTraining` — current code (target modification)
- `Controllers/TrainingAdminController.cs:793-834` `DeleteManualAssessment` — current code (target modification)
- `Helpers/FileUploadHelper.cs` `DeleteFile(webRootPath, url)` — verify signature for D-03 (verify saat eksekusi)

### Phase 323 Atomicity Pattern (verbatim reuse)
- `Controllers/AssessmentAdminController.cs:DeleteAssessment` — gold standard tx wrap + post-commit file cleanup (kalau ada). Verify pattern saat eksekusi.

### Phase 325 P05 Pre-Check Pattern (preserve, do not modify)
- `Controllers/TrainingAdminController.cs:568-580` `DeleteTraining` pre-check L568-580 (RenewsTrainingId TR+AS count)
- `Controllers/TrainingAdminController.cs:802-805` `DeleteManualAssessment` pre-check L802-805 (RenewsSessionId TR+AS count)

### Base Infrastructure
- `Controllers/AdminBaseController.cs` — `_context`, `_userManager`, `_auditLog`, `_logger`, `_env` semua tersedia via base inheritance

### Project Workflow
- `CLAUDE.md` — Bahasa Indonesia response default, dev workflow, seed workflow
- `docs/IT_NOTIFY.md` — batch IT notification (append Phase 331 entry post-ship)

### v19.0 Milestone Context
- `.planning/phases/330-fix-cascade-med-bundle-delete-category-package-question-orgu/330-CONTEXT.md` D-07 — bundle push strategy (precedent)
- `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` — parent audit spec

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `_context.Database.BeginTransactionAsync()` — EF Core 8 standard pattern, `using var tx` disposal auto-rollback
- `_auditLog.LogAsync(userId, actorName, action, detail, entityId, entityType)` — standard audit pattern, sudah dipakai L593 + L823
- `_userManager.GetUserAsync(User)` — sudah dipakai L588 + L818
- `FileUploadHelper.DeleteFile(webRootPath, url)` — wrapper untuk session.ManualSertifikatUrl path resolution

### Established Patterns
- catch DbUpdateException + `_logger.LogWarning` + TempData["Error"]: Phase 325 P05 D-05 (L598 + L828)
- Pre-check renewal CountAsync sebelum Remove: Phase 325 P05 D-04/D-11 (L568-580 + L802-805)
- Audit log post-SaveChanges INSIDE tx scope: Phase 323 D-04 pattern (parity check saat eksekusi)

### Integration Points
- `Microsoft.EntityFrameworkCore` `BeginTransactionAsync` — already imported (DbUpdateException sudah ada)
- `_env.WebRootPath` — `IWebHostEnvironment` base injection
- `System.IO.File` — System namespace, no import needed
- `Path.Combine` — System.IO, no import needed

</code_context>

<specifics>
## Specific Ideas

- **Tx scope:** `using var tx = await _context.Database.BeginTransactionAsync();` AT START dari try block (sebelum any DB mutation). Disposal di end of method auto-rollback kalau exception escape sebelum `CommitAsync`.
- **File path capture:** STRING variable assignment BEFORE Remove. Record/Session object akan detached post-Remove, tapi string sudah captured value-typed.
- **File.Delete try/catch INNER:** `try { ... } catch (Exception ex) { _logger.LogWarning(...); }` — inner catch warn-only, JANGAN throw (DB sudah commit, tidak boleh fail user request).
- **CommitAsync placement:** SETELAH audit log, SEBELUM File.Delete + TempData["Success"]. Order: SaveChanges → AuditLog → CommitAsync → File.Delete → TempData["Success"].
- **Catch DbUpdateException sudah handle TOCTOU:** kalau ada race condition antara pre-check renewal dan Remove (window kecil), catch DbUpdateException akan trap FK violation dari DB → tx auto-rollback + friendly TempData["Error"].
- **NO new test file:** Re-run `dotnet test --no-build` cukup. Manual smoke per AC D-09 item 6.
- **NO migration, NO schema change, NO model change.**

</specifics>

<deferred>
## Deferred Ideas

- ❌ Integration test xUnit untuk DeleteTraining/DeleteManualAssessment full lifecycle — out of scope. Manual smoke cukup untuk MED-impact mechanical fix.
- ❌ Cron janitor untuk cleanup orphan file post-fail — Phase 999.x backlog idea, low priority (manual disk audit cukup).
- ❌ Refactor catch dengan generic Exception fallback (selain DbUpdateException) — existing catch sudah cover DbUpdateException specific, generic fallback = scope creep.
- ❌ Phase 332 Bagian + Phase 333 CoachingSession + Phase 334 Kompetensi + Phase 335 Worker — separate phase per roadmap, depends Phase 331.

</deferred>

---

*Phase: 331-fix-cascade-deletetraining-deletemanualassessment-atomicity*
*Context gathered: 2026-05-28 via Phase 328 RESEARCH-derived (audit-as-PRD), --auto mode single-pass*
*Source: Phase 328 RESEARCH commit `41f1eef2` §4.1 + §4.2 + §9 proposal #1*
