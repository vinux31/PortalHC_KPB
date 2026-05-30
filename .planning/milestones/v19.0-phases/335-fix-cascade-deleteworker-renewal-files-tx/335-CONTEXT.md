# Phase 335: Fix Cascade DeleteWorker Triple-Fix (D2+D5+D7) - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Discuss mode:** auto (--auto flag, single-pass, all recommended defaults selected)
**Severity:** HIGH (triple-dim)
**Effort:** L (~200-300 LoC delta)

<domain>
## Phase Boundary

Phase 335 menutup **1 HIGH finding triple-dim** Phase 328 Cascade Audit Sweep §4.3 + §9 proposal #6: `DeleteWorker` D2+D5+D7 — final HIGH phase v19.0.

**Scope endpoint:**
1. `DeleteWorker` — `Controllers/WorkerController.cs:487-612` — triple-fix:
   - **D2 (file orphan):** Saat `_userManager.DeleteAsync(user)` cascade-delete User → TR + AS + IdpItems, SertifikatUrl + ManualSertifikatUrl + EvidencePath + EvidencePathHistory physical files TIDAK di-cleanup → orphan disk
   - **D5 (cross-user renewal):** TR/AS Worker A bisa jadi `RenewsTrainingId`/`RenewsSessionId` source untuk Worker B (cross-user, BUKAN same-user Phase 325 P05). Cascade-delete User A → cascade-delete TR/AS A → FK NoAction violation 500 dari Worker B side. Pre-check ABSENT.
   - **D7 (no tx):** 9-step RemoveRange + UserManager.DeleteAsync NOT atomic. Partial-write risk kalau UserManager fail mid-cascade.

**Existing OK (preserve verbatim):**
- L484-486 `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]`
- L489 BadRequest null/empty id
- L491-499 self-deletion guard (currentUser.Id == id check)
- L501-506 user lookup + null check
- L508-509 captured display fields userName + userEmail
- L513-516 userAssessmentIds collection
- L518-531 PackageUserResponses + UserPackageAssignments RemoveRange (FK Restrict on AS)
- L536-540 ProtonDeliverableProgresses RemoveRange (CoacheeId == id)
- L542-547 ProtonFinalAssessments RemoveRange
- L549-554 ProtonTrackAssignments RemoveRange
- L556-561 ProtonNotifications RemoveRange (RecipientId/CoacheeId)
- L563-568 CoachCoacheeMappings RemoveRange
- L570-575 CoachingSessions RemoveRange (CoachId/CoacheeId)
- L577-582 CoachingLogs RemoveRange (CoachId/CoacheeId)
- L587 UserManager.DeleteAsync — Identity store call (uses same ApplicationDbContext per AddEntityFrameworkStores convention — assumed same tx scope)
- L591-602 audit log block dengan inner try/catch warn-only
- L606-608 Identity errors fallback TempData

**Files modified:**
- `Controllers/WorkerController.cs` (~80-120 LoC delta, 1 endpoint)

**Zero schema change. Zero migration. Zero model change. Zero view change.**

</domain>

<decisions>
## Implementation Decisions

### D-01 Source-of-truth: Phase 328 RESEARCH.md §4.3 + §9 proposal #6

Lock ke audit deliverable Phase 328 commit `41f1eef2`:
- §4.3: DeleteWorker HIGH triple-dim (D2+D5+D7)
- §9 #6: `fix-cascade-deleteworker-renewal-files-tx`

### D-02 Cross-User Renewal Pre-Check (D5 Fix) — Block Strategy

Pre-check BEFORE tx scope (early return TempData friendly tanpa tx creation):

```csharp
// Collect user's own TR + AS IDs
var userTrainingIds = await _context.TrainingRecords
    .Where(t => t.UserId == id)
    .Select(t => t.Id)
    .ToListAsync();
// userAssessmentIds sudah exist L513-516

// Count cross-user references (Worker B's TR/AS yang reference Worker A's TR/AS as renewal source)
var crossUserTrReferences = 0;
var crossUserAsReferences = 0;

if (userTrainingIds.Any() || userAssessmentIds.Any())
{
    crossUserTrReferences = await _context.TrainingRecords
        .CountAsync(t => t.UserId != id && (
            (t.RenewsTrainingId.HasValue && userTrainingIds.Contains(t.RenewsTrainingId.Value)) ||
            (t.RenewsSessionId.HasValue && userAssessmentIds.Contains(t.RenewsSessionId.Value))
        ));
    crossUserAsReferences = await _context.AssessmentSessions
        .CountAsync(a => a.UserId != id && (
            (a.RenewsTrainingId.HasValue && userTrainingIds.Contains(a.RenewsTrainingId.Value)) ||
            (a.RenewsSessionId.HasValue && userAssessmentIds.Contains(a.RenewsSessionId.Value))
        ));
}

var totalCrossRefs = crossUserTrReferences + crossUserAsReferences;
if (totalCrossRefs > 0)
{
    TempData["Error"] = $"Tidak bisa hapus pekerja '{userName}': {totalCrossRefs} sertifikat milik pekerja lain menggunakan sertifikat pekerja ini sebagai sumber renewal. Hapus atau update sertifikat pemakai terlebih dulu.";
    return RedirectToAction("ManageWorkers");
}
```

**Strategy:** Block — user MUST manually break renewal chain before deleting. Tidak null-clear otomatis karena UX clarity + audit trail clarity (manual breakage = explicit decision per worker).

[auto] Block (BUKAN null-clear) — Phase 325 P05 pattern dipakai konsisten untuk delete endpoint hardening.

### D-03 File Path Collection (D2 Fix) — Collect SEBELUM Cascade

Collect file paths SEBELUM `UserManager.DeleteAsync` cascade-delete TR + AS:

```csharp
// Re-query full TR + AS objects (untuk akses SertifikatUrl + ManualSertifikatUrl)
var userTrainingRecords = await _context.TrainingRecords
    .Where(t => t.UserId == id)
    .ToListAsync();
var userAssessmentSessions = await _context.AssessmentSessions
    .Where(a => a.UserId == id)
    .ToListAsync();

// Collect file paths — outer-scope nullable list for POST commit access
var allFilePaths = new List<string>();

// TR SertifikatUrl
foreach (var tr in userTrainingRecords)
{
    if (!string.IsNullOrEmpty(tr.SertifikatUrl)) allFilePaths.Add(tr.SertifikatUrl);
}

// AS ManualSertifikatUrl
foreach (var asSession in userAssessmentSessions)
{
    if (!string.IsNullOrEmpty(asSession.ManualSertifikatUrl)) allFilePaths.Add(asSession.ManualSertifikatUrl);
}

// ProtonDeliverableProgress EvidencePath + EvidencePathHistory (JSON parse)
foreach (var p in protonProgress)
{
    if (!string.IsNullOrEmpty(p.EvidencePath)) allFilePaths.Add(p.EvidencePath);
    if (!string.IsNullOrEmpty(p.EvidencePathHistory))
    {
        try
        {
            var history = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(p.EvidencePathHistory) ?? new List<string>();
            allFilePaths.AddRange(history);
        }
        catch (Exception jex)
        {
            _logger.LogWarning(jex, "Failed to parse EvidencePathHistory for progress {Pid} (DeleteWorker)", p.Id);
        }
    }
}
```

Collection happens INSIDE tx (TR/AS objects must be queried before they cascade-detach via UserManager.DeleteAsync), File.Delete POST CommitAsync.

### D-04 Transaction Wrap (D7 Fix) — BeginTransactionAsync + UserManager Interaction

Wrap 9-step RemoveRange + SaveChanges + UserManager.DeleteAsync + audit log dalam tx:

```csharp
using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // All 9 RemoveRange existing (L518-582) INSIDE tx
    // ...
    await _context.SaveChangesAsync();
    
    // UserManager.DeleteAsync INSIDE tx (uses same ApplicationDbContext via AddEntityFrameworkStores)
    var result = await _userManager.DeleteAsync(user);
    if (!result.Succeeded)
    {
        // Identity errors — NOT a DbUpdateException, manual rollback via early return
        // tx auto-rollback via using disposal saat exception escape — but we DON'T throw here.
        // Need explicit handling: return error WITHOUT committing tx (disposal will rollback)
        TempData["Error"] = "Gagal menghapus user dari sistem authentication. Hubungi admin.";
        // Tx will rollback via disposal saat method returns (using var scope ends)
        return RedirectToAction("ManageWorkers");
    }
    
    // Audit log INSIDE tx
    try
    {
        var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP) ? (currentUser?.FullName ?? "Unknown") : $"{currentUser.NIP} - {currentUser.FullName}";
        await _auditLog.LogAsync(currentUser.Id, actorName, "DeleteWorker",
            $"Deleted user '{userName}' ({userEmail})", null, "ApplicationUser");
    }
    catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeleteWorker (userId={Id})", id); }
    
    await tx.CommitAsync();
}
catch (DbUpdateException dbEx)
{
    _logger.LogWarning(dbEx, "DbUpdate failed for DeleteWorker {Id}", id);
    TempData["Error"] = "Gagal hapus pekerja: ada constraint database yang dilanggar. Pastikan tidak ada data dependen yang belum dibersihkan.";
    return RedirectToAction("ManageWorkers");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to delete worker {Id}", id);
    TempData["Error"] = "Gagal hapus pekerja: terjadi kesalahan internal. Hubungi admin.";
    return RedirectToAction("ManageWorkers");
}
```

[auto] **UserManager.DeleteAsync di dalam tx scope.** ASP.NET Identity dengan `AddEntityFrameworkStores<ApplicationDbContext>()` menggunakan SAME DbContext instance — same tx scope works.

**Identity error handling:** `result.Succeeded == false` = NOT exception (Identity returns IdentityResult). Need explicit early return INSIDE try block — tx tidak committed = disposal auto-rollback. No throw.

### D-05 File.Delete Loop POST CommitAsync (D2 Fix Completion)

Same pattern Phase 331/332/333/334:

```csharp
// POST tx.CommitAsync, BEFORE TempData["Success"]
if (allFilePaths.Count > 0)
{
    foreach (var relUrl in allFilePaths)
    {
        try
        {
            var physical = Path.Combine(_env.WebRootPath,
                relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
        }
        catch (Exception fex)
        {
            _logger.LogWarning(fex, "File.Delete post-commit failed (Worker file): {Path}", relUrl);
        }
    }
}

TempData["Success"] = $"User '{userName}' berhasil dihapus dari sistem.";
return RedirectToAction("ManageWorkers");
```

### D-06 Catch Refactor — DbUpdateException + Exception Fallback Friendly

Current Identity errors L606-608: `TempData["Error"] = $"Gagal menghapus user: {string.Join(...)}"` — leak Identity error details. Refactor:
- Identity result.Errors → generic friendly (NO leak Description strings) atau log internally + show generic
- Catch DbUpdateException → friendly TempData
- Catch Exception fallback → friendly TempData

[auto] Identity errors leak: keep current Description display in TempData (Identity errors usually localized + user-actionable like "User has logins" — acceptable trade-off, NOT info leak DB constraint). Don't change Identity error UX.

### D-07 Self-Deletion Guard — Preserve Verbatim

[auto] L491-499 self-deletion guard preserve verbatim. OUTSIDE tx scope (early return TempData friendly tanpa tx creation).

### D-08 IT_NOTIFY Strategy — Bundle v19.0

[auto] Sama dengan Phase 329-334: ship lokal, BUNDLE batch push ke origin/main bersama Phase 325-335 saat user release push lock per Phase 327 option-b hold. Append IT_NOTIFY Phase 335 entry + smoke scenario #13 (cross-user renewal pre-check + file orphan + tx atomicity).

### D-09 Acceptance Criteria (Locked)

1. Pre-check D5 cross-user renewal: query `crossUserTrReferences` + `crossUserAsReferences` BEFORE tx, block dengan TempData friendly kalau totalCrossRefs > 0.
2. File path collection D2: re-query `userTrainingRecords` + `userAssessmentSessions` (full objects), build `allFilePaths` list dari SertifikatUrl + ManualSertifikatUrl + protonProgress EvidencePath + JSON history.
3. Tx wrap D7: `using var tx = BeginTransactionAsync` wrap semua 9-step RemoveRange + SaveChanges + UserManager.DeleteAsync + audit log + CommitAsync.
4. Identity error path: `result.Succeeded == false` early return INSIDE try (tx disposal auto-rollback).
5. File.Delete loop POST CommitAsync dengan inner try/catch warn-only per file.
6. Catch refactor: DbUpdateException specific + Exception fallback friendly. NO explicit RollbackAsync (using disposal handles).
7. Self-deletion guard + Authorization preserved verbatim.
8. `dotnet build` 0 error CS*.
9. `dotnet test --no-build` 18/18 PASS.
10. Manual smoke deferred ke Dev promo (scenario #13 multi-scenario).
11. Commit: `feat(335): cascade triple-fix DeleteWorker (cross-user renewal pre-check + file post-commit + tx wrap)`.
12. SUMMARY.md generated.

### D-10 Plan Structure — Single Plan, 3 Task Wave

1 PLAN.md (`335-01-PLAN.md`) dengan 3 task (extended dari standard 2 karena complexity):
- Task 1: Pre-check D5 cross-user renewal + Identity error refactor (~30 LoC) — early return pattern
- Task 2: D2 file collection + D7 tx wrap + UserManager interaction + catch refactor (~80 LoC) — bulk refactor
- Task 3: Verify build + test + grep AC + IT_NOTIFY + commit + SUMMARY

[auto] **3 task** karena complexity. Pre-check + tx wrap conceptually independent steps, easier to verify per task.

### D-11 UserManager.DeleteAsync Tx Behavior — Assumption + Verify

[auto] Assumption: ASP.NET Identity dengan `AddEntityFrameworkStores<ApplicationDbContext>()` di Program.cs menggunakan same `ApplicationDbContext` instance per scoped DI lifetime. UserManager.DeleteAsync → calls _userStore.DeleteAsync → calls `_context.SaveChangesAsync()` underneath. Same `DbContext` = same active transaction.

Verify saat eksekusi: check `Program.cs` AddEntityFrameworkStores config + run test edge case (Identity error mid-tx, verify rollback).

Jika ASSUMPTION SALAH (UserManager bypass tx): fallback strategy = use `_context.Users.Remove(user)` directly + `SaveChangesAsync()` (skips Identity hooks but stays in tx). Document deviation.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning.**

### Source Truth (Audit-Derived)
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md` §4.3 (DeleteWorker HIGH triple-dim) + §9 proposal #6

### Gold Standard Pattern References
- `Controllers/WorkerController.cs:487-612` `DeleteWorker` — current code (target modification)
- `Controllers/AssessmentAdminController.cs:DeleteAssessment` (Phase 325 P05) — same-user renewal pre-check pattern (extend ke cross-user di Phase 335)
- `Controllers/TrainingAdminController.cs:559-625` (Phase 331) — File path capture pattern + tx wrap
- `Controllers/CDPController.cs:2433-2575` (Phase 333) — Multi-file collection + JSON parse + outer-scope nullable
- `Controllers/ProtonDataController.cs:1516-1640` (Phase 334) — File collection + catch friendly + cascade order

### Phase 325 P05 Pre-Check Pattern (extend ke cross-user)
- DeleteAssessment cross-user check: query TR/AS where RenewsXxxId IN userTargetIds, but Phase 325 P05 limited to same-user. Phase 335 extends ke `UserId != id` filter (cross-user).

### Schema Assumption Points
- `TrainingRecord` model: has `UserId`, `RenewsTrainingId`, `RenewsSessionId`, `SertifikatUrl`, `ManualSertifikatUrl` fields
- `AssessmentSession` model: has `UserId`, `RenewsTrainingId`, `RenewsSessionId`, `ManualSertifikatUrl` fields
- `ProtonDeliverableProgress` model: has `CoacheeId` (string == User.Id), `EvidencePath`, `EvidencePathHistory` (JSON string) fields
- `Program.cs`: `AddEntityFrameworkStores<ApplicationDbContext>()` — Identity uses same DbContext as `_context`

### v19.0 Milestone Context
- `.planning/phases/331-...` `.planning/phases/332-...` `.planning/phases/333-...` `.planning/phases/334-...` — pattern progression

### Project Workflow
- `CLAUDE.md` — Bahasa Indonesia
- `docs/IT_NOTIFY.md` — batch IT notification (append Phase 335 entry final v19.0)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets (Phase 331-334 pattern reuse)
- File path collection BEFORE Remove (string list value-typed safe post-commit)
- BeginTransactionAsync wrap + using disposal auto-rollback
- File.Delete loop POST CommitAsync dengan inner try/catch warn-only per file
- catch DbUpdateException specific + catch Exception fallback friendly
- JSON parse EvidencePathHistory inner try/catch warn-only

### New Complexity (Phase 335 specific)
- **Cross-user renewal pre-check**: extension Phase 325 P05 dengan `UserId != id` filter — 2 count queries (TR + AS)
- **UserManager.DeleteAsync inside tx**: assumption ASP.NET Identity menggunakan same DbContext per AddEntityFrameworkStores. Verify saat execute.
- **Identity error early return**: result.Succeeded == false bukan exception — early return INSIDE try, tx disposal auto-rollback
- **Multi-source file collection**: 3 sources (TR SertifikatUrl + AS ManualSertifikatUrl + ProtonProgress EvidencePath/History)
- **9-step cascade RemoveRange + UserManager call atomic** dalam 1 tx scope

### Integration Points
- `Microsoft.EntityFrameworkCore` `DbUpdateException` + `BeginTransactionAsync` — already imported
- `Microsoft.AspNetCore.Identity` `UserManager.DeleteAsync` — Identity store interaction
- `System.Text.Json.JsonSerializer` — JSON history parse

</code_context>

<specifics>
## Specific Ideas

- **Pre-check position:** SEBELUM `using var tx = ...`. Early return TempData friendly tanpa tx creation.
- **File path list:** Single `List<string> allFilePaths` (not nullable — always allocated, may be empty).
- **Path collection timing:** INSIDE tx scope (sebelum SaveChanges + UserManager.DeleteAsync), karena Remove + cascade akan detach objects.
- **Pre-existing userAssessmentIds L513-516:** keep, reuse. Add `userTrainingIds` similar pattern.
- **Pre-existing 9-step RemoveRange L518-582:** preserve verbatim, wrap dalam tx.
- **Pre-existing SaveChanges L584:** preserve verbatim, INSIDE tx.
- **Pre-existing UserManager.DeleteAsync L587:** preserve verbatim, INSIDE tx.
- **Pre-existing audit log L590-602:** preserve verbatim, INSIDE tx (after UserManager success).
- **Identity result.Errors UX:** keep current Description display (NOT info leak DB constraint — actionable Identity-level message like "User has active logins").
- **Catch order:** DbUpdateException FIRST, Exception SECOND.
- **NO explicit RollbackAsync** — using var disposal.
- **NO throw** — replace dengan return RedirectToAction friendly.
- **Cross-user query optimization:** combine 2 counts via OR conditions to avoid N+1.

</specifics>

<deferred>
## Deferred Ideas

- ❌ Null-clear renewal references (alternative to block) — UX clarity prefers explicit user action, plus audit trail.
- ❌ Soft-delete worker — Phase 999.x backlog (Phase 325 D-04 deferred).
- ❌ Integration test xUnit DeleteWorker — out of scope (manual UAT 5+ scenario).
- ❌ Cron janitor cleanup orphan files — Phase 999.x backlog.
- ❌ Refactor 9-step RemoveRange ke service layer — scope creep.
- ❌ Identity error message refactor — keep current UX.

</deferred>

---

*Phase: 335-fix-cascade-deleteworker-renewal-files-tx*
*Context gathered: 2026-05-28 via Phase 328 RESEARCH-derived (audit-as-PRD), --auto mode single-pass*
*Source: Phase 328 RESEARCH commit `41f1eef2` §4.3 + §9 proposal #6*
*Pattern: Phase 325 P05 (renewal pre-check extended cross-user) + Phase 331/332/333/334 (file collection + File.Delete post-commit + catch friendly) + Identity store tx interaction*
*v19.0 FINAL phase. 11/11 = 100% milestone post-ship.*
