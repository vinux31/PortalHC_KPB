# Phase 330: Fix Cascade MED Bundle (DeleteCategory + DeletePackage + DeleteQuestion + DeleteOrgUnit + NotificationService) - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Discuss mode:** auto (--auto flag, single-pass, all recommended defaults selected)

<domain>
## Phase Boundary

Phase 330 menutup **5 MED finding** dari Phase 328 Cascade Audit Sweep (§5 MED Findings + §9 proposal #7): mekanis mechanical add `try/catch DbUpdateException` + tambah `_auditLog.LogAsync` di endpoint yang audit trail-nya kosong.

**Scope endpoint:**
1. `DeleteCategory` — `Controllers/AssessmentAdminController.cs:462` — D6: no try/catch DbUpdateException
2. `DeletePackage` — `Controllers/AssessmentAdminController.cs:5090` — D6: SaveChangesAsync tidak di-wrap try/catch (try block hanya sekitar audit log L5124)
3. `DeleteQuestion` — `Controllers/AssessmentAdminController.cs:6002` — D3+D6: NO audit log + no try/catch
4. `DeleteOrganizationUnit` — `Controllers/OrganizationController.cs:357` — D3+D6: NO audit log + no try/catch
5. `NotificationService.DeleteAsync` — `Services/NotificationService.cs:270` — D6 (refactor catch ke specific DbUpdateException); D3 audit log SKIP (lihat D-06)

**Files modified:**
- `Controllers/AssessmentAdminController.cs` (~40-50 LoC delta, endpoint 1+2+3)
- `Controllers/OrganizationController.cs` (~20 LoC delta, endpoint 4)
- `Services/NotificationService.cs` (~5 LoC delta, endpoint 5 catch refactor only)

**Zero schema change. Zero migration. Zero model change. Zero view change.**

</domain>

<decisions>
## Implementation Decisions

### D-01 Source-of-truth: Phase 328 RESEARCH.md §5 MED Findings

Lock seluruh design ke audit deliverable Phase 328 commit `41f1eef2`:
- §5 row 1: DeleteCategory D6
- §5 row 2: DeletePackage D6
- §5 row 3: DeleteQuestion D3+D6
- §5 row 4: DeleteOrganizationUnit D3+D6
- §5 row 5: NotificationService.DeleteAsync D3 (optional)
- §9 proposal #7: fix-med-deletecategory-deletepackage-deletequestion-deleteorganizationunit-deletenotification

### D-02 DeleteCategory Fix

`Controllers/AssessmentAdminController.cs:462`

Current: `SaveChangesAsync` L474 dijalankan langsung tanpa try/catch — FK violation = raw 500.

Fix: Wrap `_context.AssessmentCategories.Remove(category); await _context.SaveChangesAsync();` dalam try/catch DbUpdateException:

```csharp
try
{
    _context.AssessmentCategories.Remove(category);
    await _context.SaveChangesAsync();
    _cache.Remove(CategoriesCacheKey);
}
catch (DbUpdateException)
{
    TempData["Error"] = "Tidak bisa hapus kategori: masih ada data yang berelasi.";
    return RedirectToAction("ManageCategories");
}
```

Lanjutkan audit log + TempData["Success"] setelah try block. `_auditLog.LogAsync` sudah ada (L481) — tidak perlu tambah.

### D-03 DeletePackage Fix

`Controllers/AssessmentAdminController.cs:5090`

Current: `SaveChangesAsync` L5122 di luar semua try/catch. Try block L5124-5137 hanya mengproteksi `_auditLog.LogAsync`.

Fix **minimal** (tidak restructure seluruh method): tambah try/catch DbUpdateException hanya sekitar baris `await _context.SaveChangesAsync()`:

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateException)
{
    TempData["Error"] = "Tidak bisa hapus paket: masih ada data yang berelasi.";
    return RedirectToAction("ManagePackages", new { assessmentId });
}
```

Audit log try block (L5124) tetap di tempatnya — tidak restructure.

### D-04 DeleteQuestion Fix

`Controllers/AssessmentAdminController.cs:6002`

Current: `SaveChangesAsync` L6017 tanpa try/catch. Tidak ada `_auditLog.LogAsync`.

Fix:
1. Wrap `SaveChangesAsync` dalam try/catch DbUpdateException:
```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateException)
{
    TempData["Error"] = "Tidak bisa hapus soal: masih ada data yang berelasi.";
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
```

2. Tambah `_auditLog.LogAsync` SETELAH SaveChanges (pattern verbatim dari DeleteCategory/DeletePackage):
```csharp
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";
await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "DeleteQuestion",
    $"Deleted question [ID={q.Id}] from package [ID={packageId}]",
    packageId, "PackageQuestion");
```

Wrap audit log dalam its own `try { } catch (Exception ex) { _logger.LogWarning(...) }` — pattern konsisten dengan DeletePackage audit log block.

### D-05 DeleteOrganizationUnit Fix

`Controllers/OrganizationController.cs:357`

Current: `SaveChangesAsync` L411 tanpa try/catch. Tidak ada `_auditLog.LogAsync`. `_auditLog` **sudah tersedia via AdminBaseController** (field `protected readonly AuditLogService _auditLog` L17 base).

OrganizationController juga punya dual-path response: `IsAjaxRequest()` → return JSON, else → TempData + Redirect.

Fix:
1. Wrap Remove + SaveChangesAsync dalam try/catch DbUpdateException yang handle kedua path:
```csharp
try
{
    _context.OrganizationUnits.Remove(unit);
    await _context.SaveChangesAsync();
}
catch (DbUpdateException)
{
    if (IsAjaxRequest())
        return Json(new { success = false, message = "Tidak bisa hapus unit: masih ada data yang berelasi." });
    TempData["Error"] = "Tidak bisa hapus unit: masih ada data yang berelasi.";
    return RedirectToAction("ManageOrganization");
}
```

2. Tambah `_auditLog.LogAsync` SETELAH try block (sebelum IsAjaxRequest return):
```csharp
try
{
    var currentUser = await _userManager.GetUserAsync(User);
    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
        ? (currentUser?.FullName ?? "Unknown")
        : $"{currentUser.NIP} - {currentUser.FullName}";
    await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "DeleteOrganizationUnit",
        $"Deleted organization unit '{unit.Name}' [ID={unit.Id}] (Level={unit.Level})",
        unit.Id, "OrganizationUnit");
}
catch (Exception ex) { _logger.LogWarning(ex, "Audit log failed for DeleteOrganizationUnit id={Id}", unit.Id); }
```

**Perlu check**: apakah OrganizationController punya `_logger` field dari base. Jika tidak, audit log wrap pakai `try { } catch { }` silent (no logger). [auto] Asumsi _logger tersedia via base (standard pattern); verifikasi saat eksekusi.

### D-06 NotificationService.DeleteAsync — Catch Refactor ONLY, Skip Audit Log

`Services/NotificationService.cs:270`

Phase 328 §5 row 5: D3 audit log "optional — mungkin overkill, kalau dianggap noise, ignore D3". [auto] **SKIP audit log** — NotificationService tidak inject AuditLogService, menambah injection = scope creep keluar dari mechanical fix.

Fix **minimal** (catch-only refactor):
- Ganti `catch (Exception ex)` → `catch (DbUpdateException ex)` di L286.
- `_logger.LogWarning` tetap ada — hanya type exception lebih specific.

```csharp
catch (DbUpdateException ex)
{
    _logger.LogWarning(ex, "Failed to delete notification={NotificationId} for user={UserId}", notificationId, userId);
    return false;
}
```

**Tidak ada audit log, tidak ada injection baru.** Scope MED tetap S.

### D-07 IT_NOTIFY: Bundle v19.0 — JANGAN push standalone

Sama dengan Phase 329 D-07: Ship lokal, bundle batch push ke origin/main bersama Phase 325+326+327+328+329+330 saat user release push lock per Phase 327 option-b hold. Append entry ke `docs/IT_NOTIFY.md`.

### D-08 Error Message Strings (Locked)

| Endpoint | Error message |
|----------|---------------|
| DeleteCategory | `"Tidak bisa hapus kategori: masih ada data yang berelasi."` |
| DeletePackage | `"Tidak bisa hapus paket: masih ada data yang berelasi."` |
| DeleteQuestion | `"Tidak bisa hapus soal: masih ada data yang berelasi."` |
| DeleteOrganizationUnit | `"Tidak bisa hapus unit: masih ada data yang berelasi."` |

Pattern konsisten: "Tidak bisa hapus {entity}: masih ada data yang berelasi." — singkat, Bahasa Indonesia, informative.

### D-09 Acceptance Criteria (Locked)

1. `DeleteCategory` L474: `SaveChangesAsync` wrapped in try/catch DbUpdateException → TempData["Error"] + redirect on catch.
2. `DeletePackage` L5122: `SaveChangesAsync` wrapped in try/catch DbUpdateException → TempData["Error"] + redirect on catch.
3. `DeleteQuestion` L6017: `SaveChangesAsync` wrapped in try/catch DbUpdateException + `_auditLog.LogAsync("DeleteQuestion", ...)` added post-save.
4. `DeleteOrganizationUnit` L411: `SaveChangesAsync` wrapped in try/catch DbUpdateException (dual-path JSON+TempData) + `_auditLog.LogAsync("DeleteOrganizationUnit", ...)` added post-save.
5. `NotificationService.DeleteAsync` L286: `catch (Exception ex)` → `catch (DbUpdateException ex)` (catch-type only, no structural change).
6. `dotnet build` clean. `dotnet test 18/18` pass (no regression from Phase 329 baseline).
7. Manual smoke: attempt FK violation untuk setiap endpoint → expect TempData["Error"] friendly (BUKAN 500).
8. Commit: `feat(330): cascade med-bundle try-catch + audit log (DeleteCategory/Package/Question/OrgUnit)`.
9. SUMMARY.md generated.

### D-10 Plan Structure — Single Plan, 3 Task Wave

1 PLAN.md (`330-01-PLAN.md`) dengan 3 task:
- Task 1: Fix DeleteCategory + DeletePackage + DeleteQuestion di AssessmentAdminController.cs (~50 LoC)
- Task 2: Fix DeleteOrganizationUnit di OrganizationController.cs + NotificationService catch refactor (~25 LoC)
- Task 3: Verify `dotnet build` + `dotnet test` + manual smoke + IT_NOTIFY append + commit + SUMMARY

Sequential dependency: Task 2 setelah Task 1 (independent file, tapi cek build satu kali di Task 3).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning.**

### Source Truth (Audit-Derived)
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-RESEARCH.md` §5 MED Findings + §9 proposal #7 — root cause + remediation prescription per endpoint
- `.planning/phases/328-cascade-audit-sweep-delete-endpoints/328-01-SUMMARY.md` — fix proposal row #7

### Gold Standard Pattern References
- `Controllers/AssessmentAdminController.cs:462-487` `DeleteCategory` — current code (target modification, audit log sudah ada)
- `Controllers/AssessmentAdminController.cs:5090-5155` `DeletePackage` — current code (target modification)
- `Controllers/AssessmentAdminController.cs:6002-6037` `DeleteQuestion` — current code (target modification)
- `Controllers/OrganizationController.cs:357-417` `DeleteOrganizationUnit` — current code (target modification)
- `Services/NotificationService.cs:270-291` `DeleteAsync` — current code (target modification)
- `Controllers/AssessmentAdminController.cs:2230-2250` `DeleteAssessmentGroup` pre-check block (Phase 329 D-02 pattern) — catch DbUpdateException reference

### Base Infrastructure
- `Controllers/AdminBaseController.cs:17` — `protected readonly AuditLogService _auditLog;` (OrganizationController inherits via AdminBaseController)
- `Controllers/AdminBaseController.cs` — `_logger` field availability check (verify saat eksekusi Task 2)

### Project Workflow
- `CLAUDE.md` — Bahasa Indonesia response default, dev workflow, seed workflow
- `docs/IT_NOTIFY.md` — batch IT notification (append Phase 330 entry post-ship)

### v19.0 Milestone Context
- `.planning/phases/329-fix-cascade-deleteassessmentgroup-deleteprepostgroup-renewal/329-CONTEXT.md` D-07 — bundle push strategy
- `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` — parent audit spec
- `docs/IT_NOTIFY.md` — batch IT notification (update post-ship)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AuditLogService._auditLog.LogAsync(userId, actorName, action, detail, entityId, entityType)` — standard audit pattern, verbatim dari DeleteCategory L481
- `_userManager.GetUserAsync(User)` + NIP-based actorName — pattern verbatim di semua controller endpoints
- `IsAjaxRequest()` helper — OrganizationController dual-path response, sudah ada

### Established Patterns
- catch DbUpdateException inner BEFORE generic catch: Phase 329 D-04 + DeleteAssessment gold standard
- Audit log wrapped in `try { await _auditLog.LogAsync(...); } catch (Exception ex) { _logger.LogWarning(...); }` — DeletePackage L5124-5137
- TempData["Error"] + RedirectToAction = standard friendly error redirect pattern seluruh controller

### Integration Points
- `DbUpdateException` namespace: `Microsoft.EntityFrameworkCore` — sudah di-import di semua Controllers/
- `AuditLogService` — DI via AdminBaseController, tersedia di OrganizationController tanpa perubahan injection

</code_context>

<specifics>
## Specific Ideas

- **Catch type:** `catch (DbUpdateException)` (no `ex` variable needed kalau tidak dipakai dalam block). Kalau ada logger call: `catch (DbUpdateException ex)`.
- **Error message pattern:** "Tidak bisa hapus {entity}: masih ada data yang berelasi." — konsisten 5 endpoint.
- **NotificationService scope:** Minimal — hanya ganti `Exception` → `DbUpdateException` di catch. Tidak inject AuditLog, tidak tambah method signatures.
- **DeleteOrganizationUnit _logger check:** AdminBaseController memiliki `_logger` field (cek L saat eksekusi). Kalau ada: pakai `_logger.LogWarning(ex, ...)`. Kalau tidak: silent catch untuk audit log failure.
- **No new test file:** Re-run dotnet test 18/18 cukup. Manual smoke per Acceptance Criteria D-09 item 7.

</specifics>

<deferred>
## Deferred Ideas

- ❌ DeleteWorker HIGH bundle (D2 file cleanup + D5 renewal cross-user + D7 tx wrap) — terpisah jadi Phase 331+ per Phase 328 §9 row #6. Scope terlalu besar dan UserManager interaction high-risk.
- ❌ DeleteKompetensi + DeleteBagian HIGH bundle — Phase 328 §4 row HIGH lainnya, deferred ke Phase 331+.
- ❌ Soft delete `IsDeleted` column + global query filter — Phase 325 D-04 deferred v20.0+.
- ❌ NotificationService audit log injection — scope creep dari mechanical fix, deferred kalau stakeholder request.

</deferred>

---

*Phase: 330-fix-cascade-med-bundle-delete-category-package-question-orgu*
*Context gathered: 2026-05-28 via Phase 328 RESEARCH-derived (audit-as-PRD), --auto mode single-pass*
*Source: Phase 328 RESEARCH commit `41f1eef2` §5 MED Findings + §9 proposal #7*
