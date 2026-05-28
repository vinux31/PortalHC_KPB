# Phase 328 — Cascade Audit Sweep RESEARCH

**Phase:** 328-cascade-audit-sweep-delete-endpoints
**Date:** 2026-05-28
**Type:** Audit-only (no code change)
**Spec:** `docs/superpowers/specs/2026-05-27-v19.0-cascade-audit-sweep-design.md` (commit `02f620be`)
**Parent milestone:** v19.0 Portal HC Bug Fixes (lihat `project_v19.0_portal_hc_bug_fixes_planned.md`)

---

## 1. Methodology

Audit ini melakukan inspeksi cascade-safety read-only terhadap semua endpoint `Delete*` di codebase Portal HC KPB. Tujuannya menutup follow-up Phase 323 (`323-CONTEXT.md:122`): mengidentifikasi endpoint mana yang masih punya bug cascade FK / orphan file / renewal chain serupa dengan bug yang telah di-fix Phase 323 di `AssessmentAdminController.DeleteAssessment`. Hasil audit jadi input proposal phase fix berikutnya (separate user-driven phases, NOT auto-spawned per D-10).

**Langkah teknis:**

- **(a)** Enumerasi: jalankan `grep -rEn "public async.*Delete\w+\(" Controllers/*.cs Services/*.cs` untuk identifikasi semua method handler `Delete*`. Pattern case-sensitive, scope ke Controllers + Services per D-02.
- **(b)** Per-endpoint inspection: Read tool baca method body dari signature ke closing brace; cross-ref `Data/ApplicationDbContext.cs` untuk verifikasi FK definition (`OnDelete(Restrict|NoAction|Cascade)`) per target entity.
- **(c)** Gold standard comparison: setiap endpoint dibandingkan dengan pattern `Controllers/AssessmentAdminController.DeleteAssessment` post-commit `f1849367` (Phase 323 final).
- **(d)** 7-dimension grading per D-03: D1 Cascade FK, D2 File-DB atomicity, D3 AuditLog, D4 Authorize role, D5 Renewal chain, D6 try/catch DbUpdateException, D7 BeginTransactionAsync.
- **(e)** Severity tagging per D-04: HIGH (D1/D2/D5 fail), MED (D3/D4/D6 fail), LOW (D7 fail), NONE (semua ✅/N/A).
- **(f)** Out-of-scope tegas per D-02 didokumentasikan di Section 7 untuk hindari scope creep audit follow-up.

**Bukan dalam scope audit ini:** Identity `UserManager.DeleteAsync`, soft-delete `IsDeleted` flag, optimistic concurrency, idempotency, stored procedure cascade (detail di Section 7).

**Deliverable:** file markdown tunggal ini (`328-RESEARCH.md`). NO code change. NO migration. NO test. NO Playwright. NO IT_NOTIFY.

---

## 2. Endpoint Inventory

**Command:** `grep -rEn "public async.*Delete\w+\(" Controllers/*.cs Services/*.cs`
**Date executed:** 2026-05-28
**Total raw match count:** 19

### 2A. Raw grep output

```
Controllers/AssessmentAdminController.cs:462:        public async Task<IActionResult> DeleteCategory(int id)
Controllers/AssessmentAdminController.cs:2011:        public async Task<IActionResult> DeleteAssessment(int id)
Controllers/AssessmentAdminController.cs:2199:        public async Task<IActionResult> DeleteAssessmentGroup(int id)
Controllers/AssessmentAdminController.cs:2359:        public async Task<IActionResult> DeletePrePostGroup(int linkedGroupId)
Controllers/AssessmentAdminController.cs:3911:        public async Task<IActionResult> GetDeleteImpact(int id, string type)
Controllers/AssessmentAdminController.cs:5038:        public async Task<IActionResult> DeletePackage(int packageId)
Controllers/AssessmentAdminController.cs:5950:        public async Task<IActionResult> DeleteQuestion(int questionId, int packageId)
Controllers/CDPController.cs:2433:        public async Task<IActionResult> DeleteCoachingSession(int id)
Controllers/CoachMappingController.cs:1114:        public async Task<IActionResult> CoachCoacheeMappingDeletePreview(int id)
Controllers/DocumentAdminController.cs:283:        public async Task<IActionResult> DeleteBagian(int id, bool confirmed = false)
Controllers/OrganizationController.cs:357:        public async Task<IActionResult> DeleteOrganizationUnit(int id)
Controllers/ProtonDataController.cs:559:        public async Task<IActionResult> SilabusDeletePreview(int deliverableId)
Controllers/ProtonDataController.cs:571:        public async Task<IActionResult> SubKompetensiDeletePreview(int subKompetensiId)
Controllers/ProtonDataController.cs:586:        public async Task<IActionResult> KompetensiDeletePreview(int kompetensiId)
Controllers/ProtonDataController.cs:1516:        public async Task<IActionResult> DeleteKompetensi([FromBody] KompetensiDeleteRequest req)
Controllers/TrainingAdminController.cs:559:        public async Task<IActionResult> DeleteTraining(int id)
Controllers/TrainingAdminController.cs:793:        public async Task<IActionResult> DeleteManualAssessment(int id)
Controllers/WorkerController.cs:487:        public async Task<IActionResult> DeleteWorker(string id)
Services/NotificationService.cs:270:        public async Task<bool> DeleteAsync(int notificationId, string userId)
```

### 2B. Classification

Per D-11 (preview/impact pattern noise): 19 match → 14 true delete mutators + 5 read-only preview/impact endpoints.

**Tabel 2B.1 — Actual delete mutators (target audit utama, 7-dim grading):**

| # | Endpoint | File:Line |
|---|----------|-----------|
| 1 | DeleteCategory | AssessmentAdminController.cs:462 |
| 2 | DeleteAssessment | AssessmentAdminController.cs:2011 |
| 3 | DeleteAssessmentGroup | AssessmentAdminController.cs:2199 |
| 4 | DeletePrePostGroup | AssessmentAdminController.cs:2359 |
| 5 | DeletePackage | AssessmentAdminController.cs:5038 |
| 6 | DeleteQuestion | AssessmentAdminController.cs:5950 |
| 7 | DeleteCoachingSession | CDPController.cs:2433 |
| 8 | DeleteBagian | DocumentAdminController.cs:283 |
| 9 | DeleteOrganizationUnit | OrganizationController.cs:357 |
| 10 | DeleteKompetensi | ProtonDataController.cs:1516 |
| 11 | DeleteTraining | TrainingAdminController.cs:559 |
| 12 | DeleteManualAssessment | TrainingAdminController.cs:793 |
| 13 | DeleteWorker | WorkerController.cs:487 |
| 14 | NotificationService.DeleteAsync | NotificationService.cs:270 |

**Tabel 2B.2 — Preview-only methods (out-of-audit untuk 7-dim grading, completeness only, severity NONE):**

| # | Endpoint | File:Line | Note |
|---|----------|-----------|------|
| 1 | GetDeleteImpact | AssessmentAdminController.cs:3911 | GET preview impact, no mutation |
| 2 | CoachCoacheeMappingDeletePreview | CoachMappingController.cs:1114 | Preview confirmation, no mutation |
| 3 | SilabusDeletePreview | ProtonDataController.cs:559 | Preview confirmation, no mutation |
| 4 | SubKompetensiDeletePreview | ProtonDataController.cs:571 | Preview confirmation, no mutation |
| 5 | KompetensiDeletePreview | ProtonDataController.cs:586 | Preview confirmation, no mutation |

### 2C. Indirect Delete Call Sites (RemoveRange di non-Delete methods Services/*)

**Command:** `grep -rEn "_context\.(Remove|RemoveRange)" Services/*.cs`
**Result:** No matches found.

Tidak ada indirect call sites `_context.Remove`/`RemoveRange` di `Services/*.cs` di luar method `Delete*` yang sudah di-audit (`NotificationService.DeleteAsync` row 14 section 3A). Audit Services layer complete.

---

## 3. Audit Table

**Legend cell:** ✅ pass · ❌ fail (severity trigger) · ⚠️ partial (severity trigger) · N/A not applicable (eksplisit alasan, bukan penalty).
**Evidence:** setiap non-N/A cell ❌/⚠️ punya inline ref `(file.cs:LXXX)` atau `(LXXX)` short-form.
**Order:** group by controller file (inventory raw order).

### 3A. Actual delete mutators (7-dim grading)

| # | Endpoint | File:Line | D1 FK | D2 Atomicity | D3 Audit | D4 Auth | D5 Renewal | D6 Error | D7 Tx | Severity | Remediation Outline |
|---|----------|-----------|-------|--------------|----------|---------|------------|----------|-------|----------|---------------------|
| 1 | DeleteCategory | AssessmentAdminController.cs:462 | ⚠️ (L467 pre-check `ParentId` hierarchy hanya — tidak verify FK dari `AssessmentSession.Category` string label tapi karena `Category` di session = string non-FK, OK) | N/A (no file ops) | ✅ (L481 `_auditLog.LogAsync "DeleteCategory"`) | ✅ (L460 `[Authorize(Roles="Admin, HC")]`) | N/A (category not in renewal chain) | ❌ (no `try/catch DbUpdateException` — FK violation 500 raw) | ❌ (no `BeginTransactionAsync` — single Remove acceptable tapi tidak guard hierarchy race) | **MED** | Add try/catch DbUpdateException + TempData friendly. Tx optional (single-step delete). |
| 2 | DeleteAssessment | AssessmentAdminController.cs:2011 | ✅ (L2094-2148 RemoveRange editLogs+responses+attempts+assignments+packages — Phase 323 fix) | N/A (no file ops in this endpoint; manual sertifikat file di-handle separate `DeleteManualAssessment`) | ✅ (L2163 `_auditLog.LogAsync "DeleteAssessment"`) | ✅ (L2009 `[Authorize(Roles="Admin, HC")]`) | ✅ (L2040-2052 Phase 325 P05 pre-check `RenewsSessionId` di TR+AS — BLOCK pattern) | ✅ (L2180 catch `DbUpdateException` + friendly TempData) | ✅ (L2057 `BeginTransactionAsync` + L2156 CommitAsync) | **NONE** | Gold standard reference (Section 8). |
| 3 | DeleteAssessmentGroup | AssessmentAdminController.cs:2199 | ✅ (L2266-2314 RemoveRange editLogs+responses+attempts+assignments+packages untuk semua siblings) | N/A (no file ops) | ✅ (L2330 `_auditLog.LogAsync`) | ✅ (L2197) | ❌ (no `RenewsSessionId` pre-check untuk siblings — Phase 325 P05 hanya cover DeleteAssessment, group endpoint terlewat → FK NoAction violation 500 jika sibling jadi renewal source) | ⚠️ (L2347 catch generic Exception, NOT specific DbUpdateException — generic friendly tapi tidak distinguish FK vs lain) | ✅ (L2231 `BeginTransactionAsync`) | **HIGH** | Pasang pre-check `RenewsSessionId` di `siblingIds` (TR+AS count), block kalau >0, paralel pola DeleteAssessment L2040-2052. |
| 4 | DeletePrePostGroup | AssessmentAdminController.cs:2359 | ✅ (L2419-2471 RemoveRange editLogs+responses+attempts+assignments+packages untuk groupIds) | N/A (no file ops) | ✅ (L2481 `_auditLog.LogAsync`) | ✅ (L2357) | ❌ (no `RenewsSessionId` pre-check untuk groupIds — same gap dengan DeleteAssessmentGroup) | ⚠️ (L2497 catch generic Exception, NOT specific DbUpdateException) | ✅ (L2382 `BeginTransactionAsync`) | **HIGH** | Same as DeleteAssessmentGroup: pasang renewal pre-check sebelum tx. |
| 5 | DeletePackage | AssessmentAdminController.cs:5038 | ✅ (L5050-5067 RemoveRange responses+assignments+options+questions sebelum `Package.Remove` — Restrict FK UPA→Package handled) | N/A (no file ops) | ✅ (L5076 `_auditLog.LogAsync "DeletePackage"`) | ✅ (L5036) | N/A (Package not in renewal chain) | ❌ (try block hanya wrap audit log L5072, BUKAN `SaveChangesAsync` — FK violation 500 raw) | ❌ (no `BeginTransactionAsync` — multi-step RemoveRange → SaveChanges atomic via single SaveChanges call, partial-write risk low tapi tidak explicit) | **MED** | Wrap `SaveChangesAsync` di try/catch `DbUpdateException`. Tx wrap optional (single SaveChanges sudah atomic). |
| 6 | DeleteQuestion | AssessmentAdminController.cs:5950 | ✅ (L5958-5963 RemoveRange responses+options sebelum `Question.Remove`) | N/A (no file ops) | ❌ (NO `_auditLog.LogAsync` — silent delete, audit gap) | ✅ (L5948) | N/A (Question not in renewal chain) | ❌ (no try/catch DbUpdateException) | ❌ (no tx wrap) | **MED** | Tambah `_auditLog.LogAsync("DeleteQuestion", ...)` + try/catch DbUpdateException. |
| 7 | DeleteCoachingSession | CDPController.cs:2433 | ✅ (L2458 ActionItems RemoveRange + L2459 Session.Remove + L2505-2517 progress state revert) | ⚠️ (L2490-2503 `System.IO.File.Delete` INSIDE tx scope tapi SEBELUM final SaveChanges L2532 / CommitAsync L2538 — kalau SaveChanges fail post-File-Delete, file orphan-deleted) | ✅ (L2536 `_auditLog.LogAsync "DeleteCoachingSession"`) | ✅ (L2432 `[Authorize(Roles = UserRoles.RolesCoachAndAbove)]` + L2441-2453 active-mapping guard) | N/A (CoachingSession not in renewal chain) | ⚠️ (L2540 catch generic Exception + `throw` — no friendly TempData, raw 500 ke user) | ✅ (L2455 `BeginTransactionAsync` + L2538 CommitAsync) | **HIGH** | Move `System.IO.File.Delete` POST `tx.CommitAsync` (Phase 323 pattern D2). Tambah friendly TempData di catch. |
| 8 | DeleteBagian | DocumentAdminController.cs:283 | ✅ (L289-302 pre-check active files BLOCK; L319-347 RemoveRange archived files+OrganizationUnits sebelum SaveChanges) | ❌ (L327 + L343 `System.IO.File.Delete` archived files SEBELUM SaveChanges L350 — orphan file risk jika DB fail) | ✅ (L359 `_auditLog.LogAsync "DeleteBagian"`) | ✅ (L281 `[Authorize(Roles="Admin, HC")]`) | N/A | ❌ (try block hanya wrap audit log L354, BUKAN SaveChanges — FK violation raw 500) | ❌ (no `BeginTransactionAsync` — multi-step file+DB tidak atomic) | **HIGH** | Wrap dalam `BeginTransactionAsync`, move `System.IO.File.Delete` POST `SaveChanges+CommitAsync`. Plus try/catch DbUpdateException. |
| 9 | DeleteOrganizationUnit | OrganizationController.cs:357 | ✅ (L372-408 4× pre-check BLOCK: active children, KKJ/CPDP files, assigned users, Proton data; clean Remove L410 hanya jika semua bersih) | N/A (no file ops, file delete di DeleteBagian) | ❌ (NO `_auditLog.LogAsync` — silent delete unit) | ✅ (L355 `[Authorize(Roles="Admin, HC")]`) | N/A | ❌ (no try/catch DbUpdateException) | ❌ (no tx — single SaveChanges acceptable) | **MED** | Tambah `_auditLog.LogAsync("DeleteOrganizationUnit", ...)` + try/catch DbUpdateException. |
| 10 | DeleteKompetensi | ProtonDataController.cs:1516 | ✅ (L1542-1573 explicit cascade: sessions→progress→deliverables→subkomp→komp, FK Restrict handled urut) | ❌ (`ProtonDeliverableProgress.EvidencePath` physical files TIDAK di-cleanup saat progresses RemoveRange L1559 — orphan file disk risk) | ✅ (L1578 `_auditLog.LogAsync "Delete" ProtonKompetensi`) | ✅ (class-level L79 `[Authorize(Roles="Admin,HC")]`) | N/A | ⚠️ (L1584 catch generic Exception, NOT DbUpdateException specific; juga return `ex.Message` ke client = info leak) | ✅ (L1529 `BeginTransactionAsync` + L1576 CommitAsync) | **HIGH** | Iterate progresses, collect EvidencePath, `System.IO.File.Delete` POST commit (Phase 323 D2 pattern). Plus refactor catch jangan expose `ex.Message`. |
| 11 | DeleteTraining | TrainingAdminController.cs:559 | ✅ (L568-580 pre-check `RenewsTrainingId` di TR+AS BLOCK delete jika ada child) | ❌ (L585 `System.IO.File.Delete` SEBELUM SaveChangesAsync L590 — orphan file risk jika DB fail) | ✅ (L593 `_auditLog.LogAsync`) | ✅ (L558 `[Authorize(Roles="Admin, HC")]`) | ✅ (L568-580 pre-check pattern; bukan null-clear Phase 323 tapi semantically setara — FK violation prevented) | ✅ (L598 catch `DbUpdateException` + L602 friendly TempData) | ❌ (no `BeginTransactionAsync` — file delete + DB remove tidak atomic) | **HIGH** | Phase 325 sudah pasang pre-check renewal chain (D5 ✅). Sisa: wrap `BeginTransactionAsync`, pindahkan `System.IO.File.Delete` POST `SaveChangesAsync+CommitAsync` (Phase 323 pattern D2/D7) |
| 12 | DeleteManualAssessment | TrainingAdminController.cs:793 | ✅ (L802-805 pre-check `RenewsSessionId` di TR+AS) | ❌ (L816 `FileUploadHelper.DeleteFile` SEBELUM SaveChangesAsync L820 — orphan file risk) | ✅ (L823 `_auditLog.LogAsync`) | ✅ (L792 `[Authorize(Roles="Admin, HC")]`) | ✅ (L802-805 pre-check pattern; equivalent to null-clear FK-safety) | ✅ (L828 catch `DbUpdateException`) | ❌ (no `BeginTransactionAsync` — file delete + DB remove tidak atomic) | **HIGH** | Same as DeleteTraining: tx wrap + reorder file delete post-DB-commit (Phase 323 pattern D2/D7) |
| 13 | DeleteWorker | WorkerController.cs:487 | ✅ (L513-582 extensive RemoveRange 9 entitas: PackageUserResponses, UserPackageAssignments, ProtonDeliverableProgresses, ProtonFinalAssessments, ProtonTrackAssignments, ProtonNotifications, CoachCoacheeMappings, CoachingSessions, CoachingLogs sebelum UserManager.DeleteAsync L587) | ❌ (NO physical file cleanup — saat User cascade-delete TR/AS, `SertifikatUrl`/`ManualSertifikatUrl`/`EvidencePath` files orphan di disk; juga `ProtonDeliverableProgresses` removed L539-540 tapi evidence files tidak dibersihkan) | ✅ (L594 `_auditLog.LogAsync "DeleteWorker"`) | ✅ (L485 `[Authorize(Roles="Admin, HC")]`) | ❌ (User cascade-delete via UserManager.DeleteAsync L587 akan trigger Cascade ke TR+AS user; TR/AS user mungkin jadi renewal source untuk OTHER worker's TR/AS via `RenewsTrainingId`/`RenewsSessionId` NoAction → FK violation 500 saat cascade. Pre-check ABSENT) | ❌ (no try/catch around SaveChanges atau UserManager.DeleteAsync) | ❌ (no `BeginTransactionAsync` — multi-step massive RemoveRange + UserManager call NOT atomic, partial-write risk jika UserManager.DeleteAsync fail mid-cascade) | **HIGH** | Multiple HIGH dim: (D2) loop TR/AS+Proton progress, collect file paths, `File.Delete` POST commit. (D5) pre-check TR/AS milik user di-renewal-reference worker lain, block atau null-clear. (D7) `BeginTransactionAsync` wrap full cascade. Pattern Phase 323 + Phase 325 P05. |
| 14 | NotificationService.DeleteAsync | NotificationService.cs:270 | ✅ (L281 `UserNotifications.Remove(notification)` — UserNotification entitas standalone, no FK out, no children) | N/A (no file ops) | ❌ (NO `_auditLog.LogAsync` — silent delete notification) | N/A (service layer; auth dilakukan caller controller, anti-IDOR ownership check L276 `notification.UserId != userId` ada ✅) | N/A (not renewal chain) | ⚠️ (L286 catch generic Exception, NOT `DbUpdateException` specific; return `false` tanpa propagate friendly error) | ❌ (no tx — single Remove + SaveChanges atomic, acceptable) | **MED** | Tambah `_auditLog.LogAsync("DeleteNotification", ...)` jika audit notification needed (mungkin overkill — confirm dengan stakeholder; kalau dianggap noise, ignore D3). Optional refactor catch jadi specific DbUpdateException. |

### 3B. Preview-only methods (no 7-dim grading, no mutation)

| # | Endpoint | File:Line | Severity | Note |
|---|----------|-----------|----------|------|
| P1 | GetDeleteImpact | AssessmentAdminController.cs:3911 | NONE | Read-only impact preview, no DB mutation |
| P2 | CoachCoacheeMappingDeletePreview | CoachMappingController.cs:1114 | NONE | Preview confirmation page, no DB mutation |
| P3 | SilabusDeletePreview | ProtonDataController.cs:559 | NONE | Preview confirmation, no DB mutation |
| P4 | SubKompetensiDeletePreview | ProtonDataController.cs:571 | NONE | Preview confirmation, no DB mutation |
| P5 | KompetensiDeletePreview | ProtonDataController.cs:586 | NONE | Preview confirmation, no DB mutation |

---

## 4. HIGH Findings

**Total HIGH:** 8 endpoint (row #3, #4, #7, #8, #10, #11, #12, #13 dari Section 3A).
Detail penuh wajib untuk 2 pre-confirmed HIGH (D-06): DeleteTraining + DeleteManualAssessment. 6 HIGH lain dibahas ringkas dengan repro + remediation pointer.

### 4.1 DeleteTraining (TrainingAdminController.cs:559-605)

**Severity:** HIGH
**Dimensions failing:** D2 (File-DB atomicity), D7 (Transaction wrap)
**Dimensions UPDATED post-Phase-325 P05:** D5 renewal chain sebelumnya FAIL (D-06 brainstorm 2026-05-27), sekarang ✅ via pre-check pattern L568-580 — methodology note: severity HIGH tetap valid karena D2/D7 fail, bukan D5.

**Evidence (verbatim L555-605):**

```csharp
// POST /Admin/DeleteTraining
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> DeleteTraining(int id)
{
    var record = await _context.TrainingRecords.FindAsync(id);
    if (record == null) return NotFound();

    try
    {
        // Phase 325 P05 D-04/D-11: pre-check referencing rows SEBELUM hapus (UX friendly).
        // TrainingRecord punya 2 jenis renewal child: TR lain + AS lain. FK column = RenewsTrainingId.
        var referencingTr = await _context.TrainingRecords
            .CountAsync(t => t.RenewsTrainingId == record.Id);
        var referencingAs = await _context.AssessmentSessions
            .CountAsync(a => a.RenewsTrainingId == record.Id);

        if (referencingTr + referencingAs > 0)
        {
            var total = referencingTr + referencingAs;
            TempData["Error"] = $"Tidak bisa hapus: {total} sertifikat lain "
                              + "menggunakan record ini sebagai sumber renewal. "
                              + "Hapus atau update sertifikat pemakai terlebih dulu.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        if (!string.IsNullOrEmpty(record.SertifikatUrl))
        {
            var path = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);  // ❌ D2 — file delete SEBELUM SaveChanges
        }

        var actor = await _userManager.GetUserAsync(User);
        _context.TrainingRecords.Remove(record);
        await _context.SaveChangesAsync();   // ❌ D7 — bukan dalam tx

        if (actor != null)
            await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
                $"Training record dihapus: {record.Judul}", record.Id, "TrainingRecord");

        TempData["Success"] = "Training record berhasil dihapus.";
    }
    catch (DbUpdateException ex)
    {
        _logger.LogWarning(ex, "Delete failed for TrainingRecord {Id}", record.Id);
        TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
    }
    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
}
```

**FK reference (`Data/ApplicationDbContext.cs:157-165`):**

```csharp
entity.HasOne<TrainingRecord>()
    .WithMany()
    .HasForeignKey(t => t.RenewsTrainingId)
    .OnDelete(DeleteBehavior.NoAction);   // Cross-table NoAction → pre-clear di app-level
```

**Reproduction path (D2 orphan file scenario):**

1. Admin upload sertifikat TR id=42 dengan `SertifikatUrl = "/uploads/cert42.pdf"`. File ada di disk.
2. Operator simulasi DB outage (kill SQL Server connection sebelum POST DeleteTraining/42).
3. Admin invoke `POST /Admin/DeleteTraining/42`.
4. L585 `System.IO.File.Delete(path)` jalan — `cert42.pdf` terhapus dari disk.
5. L590 `SaveChangesAsync()` gagal karena DB outage → catch L598 fire → TempData["Error"].
6. **Result:** File `cert42.pdf` hilang dari disk, tapi row `TrainingRecord id=42` MASIH ada di DB dengan `SertifikatUrl = "/uploads/cert42.pdf"` (dangling reference). User lihat row TR + tombol "lihat sertifikat" tapi klik = 404.

**Remediation snippet (Phase 323 pattern):**

```csharp
var pathToDelete = !string.IsNullOrEmpty(record.SertifikatUrl)
    ? Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'))
    : null;

using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // pre-check renewal chain (already exists L568-580)
    // ...

    _context.TrainingRecords.Remove(record);
    await _context.SaveChangesAsync();
    await tx.CommitAsync();

    // file delete POST commit — kalau commit fail, file masih utuh
    if (pathToDelete != null && System.IO.File.Exists(pathToDelete))
        System.IO.File.Delete(pathToDelete);

    await _auditLog.LogAsync(...);
}
catch (DbUpdateException ex)
{
    await tx.RollbackAsync();
    TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
}
```

**Recommended fix phase:** P-329 candidate (Section 9 row 1).

---

### 4.2 DeleteManualAssessment (TrainingAdminController.cs:793-834)

**Severity:** HIGH
**Dimensions failing:** D2 (File-DB atomicity), D7 (Transaction wrap)
**Dimensions UPDATED post-Phase-325 P05:** D5 renewal chain sebelumnya FAIL (D-06 brainstorm), sekarang ✅ via pre-check pattern L802-805. HIGH stays via D2/D7.

**Evidence (verbatim L789-834):**

```csharp
// POST /Admin/DeleteManualAssessment
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> DeleteManualAssessment(int id)
{
    var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == id && s.IsManualEntry);
    if (session == null) return NotFound();

    try
    {
        // Phase 325 P05 D-04/D-11: pre-check referencing rows SEBELUM hapus.
        var referencingTr = await _context.TrainingRecords
            .CountAsync(t => t.RenewsSessionId == session.Id);
        var referencingAs = await _context.AssessmentSessions
            .CountAsync(a => a.RenewsSessionId == session.Id);

        if (referencingTr + referencingAs > 0)
        {
            // ... block delete dengan TempData["Error"]
            return RedirectToAction(...);
        }

        FileUploadHelper.DeleteFile(_env.WebRootPath, session.ManualSertifikatUrl);  // ❌ D2 — sebelum SaveChanges

        var actor = await _userManager.GetUserAsync(User);
        _context.AssessmentSessions.Remove(session);
        await _context.SaveChangesAsync();   // ❌ D7 — bukan dalam tx

        if (actor != null)
            await _auditLog.LogAsync(...);

        TempData["Success"] = "Assessment manual berhasil dihapus.";
    }
    catch (DbUpdateException ex)
    {
        _logger.LogWarning(ex, "Delete failed for AssessmentSession (manual) {Id}", session.Id);
        TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
    }
    return RedirectToAction(...);
}
```

**Reproduction path:** Same shape dengan 4.1 — `FileUploadHelper.DeleteFile` L816 jalan sebelum `SaveChanges` L820 fail → `ManualSertifikatUrl` file orphan-deleted, row `AssessmentSession` masih ada dengan dangling URL.

**Remediation snippet:** Identical pattern dengan 4.1 — wrap tx, file delete POST commit.

**Recommended fix phase:** P-329 candidate (bundled dengan 4.1 di Section 9 row 1 — same controller, same pattern).

---

### 4.3 DeleteWorker (WorkerController.cs:487-612)

**Severity:** HIGH
**Dimensions failing:** D2 (File-DB atomicity), D5 (Renewal chain), D7 (Transaction wrap)

**Evidence ringkas:**
- L587 `UserManager.DeleteAsync(user)` → cascade-delete User-owned TR + AS via EF Cascade FK (`ApplicationDbContext.cs:148-149, 178`).
- TR/AS user yang terhapus mungkin jadi renewal source untuk OTHER worker's TR/AS via `RenewsTrainingId`/`RenewsSessionId` NoAction FK → FK violation 500 di tengah cascade. Pre-check `RenewsTrainingId IN (userAssessmentIds)` ATAU `RenewsSessionId IN (userAssessmentIds)` ABSENT.
- Physical files `SertifikatUrl`/`ManualSertifikatUrl`/`EvidencePath` ter-orphan di disk saat owning row cascade-deleted via UserManager.DeleteAsync — TIDAK ada loop file cleanup.
- No `BeginTransactionAsync` wrap untuk 9× RemoveRange + UserManager call (partial-write risk).

**Reproduction path:**
1. Worker A punya TR id=100 (sertifikat).
2. Worker B punya TR id=101 dengan `RenewsTrainingId = 100`.
3. Admin invoke `POST /Admin/DeleteWorker/{worker-a-id}`.
4. `UserManager.DeleteAsync` cascade-delete Worker A → cascade-delete TR id=100.
5. FK NoAction fire: TR id=101 still references id=100 → DB throws FK constraint exception.
6. **Result:** Multi-row partial state (some Proton/Coach entities removed di L513-582, others rolled back by EF auto-tx of single SaveChanges; UserManager.DeleteAsync state unclear). User lihat error generic. Worker B's TR id=101 masih ada dengan `RenewsTrainingId=100` pointing ke ghost.

**Remediation pointer:** Apply Phase 323 + Phase 325 P05 hybrid: pre-check renewal chain across user's TR+AS, null-clear OR block; wrap full method dalam `BeginTransactionAsync`; collect physical file paths sebelum cascade, `File.Delete` POST commit. Cost MEDIUM-LARGE (kompleks cascade).

**Recommended fix phase:** P-330 candidate (Section 9 row 2 — separate dari P-329 karena scope berbeda + risiko regression UserManager interaction).

---

### 4.4 DeleteAssessmentGroup (AssessmentAdminController.cs:2199-2353)

**Severity:** HIGH
**Dimensions failing:** D5 (Renewal chain pre-check absent across siblings)

**Evidence ringkas:**
Phase 325 P05 D-11 hanya tambah pre-check `RenewsSessionId` di **DeleteAssessment** (L2040-2052). **DeleteAssessmentGroup** TIDAK punya pre-check setara untuk `siblingIds` (lihat L2218-2230 — langsung lompat ke tx + cascade). Bila salah satu session sibling jadi renewal source untuk TR/AS lain → FK NoAction violation 500 di tengah cascade.

**Reproduction path:**
1. Admin buat assessment group "Skill Test" pada 2026-04-01 (3 sibling sessions, ids = 200, 201, 202).
2. Worker selesai session id=200 → renewal di TR id=999 (`RenewsSessionId=200`).
3. Admin hapus group via `POST /DeleteAssessmentGroup/200`.
4. L2266-2273 RemoveRange editLogs OK; L2317 `_context.AssessmentSessions.Remove(session)` foreach.
5. SaveChanges L2322 fire → FK NoAction violation karena TR id=999 reference id=200.
6. tx Rollback (L2231 wrap), TempData["Error"] generic "Gagal menghapus grup".

**Remediation pointer:** Inject pre-check sebelum tx scope (L2230 area) — paralel `DeleteAssessment` L2040-2052 tapi count over `siblingIds`. Block atau null-clear.

**Recommended fix phase:** P-331 candidate (atau bundle dengan 4.5 di phase yang sama — same controller, same gap).

---

### 4.5 DeletePrePostGroup (AssessmentAdminController.cs:2359-2503)

**Severity:** HIGH
**Dimensions failing:** D5 (Renewal chain pre-check absent — same gap dengan 4.4)

**Evidence ringkas:** Paralel DeleteAssessmentGroup — L2381-2418 area langsung tx + cascade tanpa renewal pre-check across `groupIds`. Phase 325 P05 D-11 tidak cover Pre-Post group endpoint.

**Reproduction + remediation:** Identik 4.4 dengan substitusi `siblingIds` → `groupIds`.

**Recommended fix phase:** Bundle dengan 4.4 (P-331).

---

### 4.6 DeleteCoachingSession (CDPController.cs:2433-2543)

**Severity:** HIGH
**Dimensions failing:** D2 (file delete inside tx scope tapi sebelum final SaveChanges/Commit — partial orphan risk)

**Evidence ringkas:** L2490-2503 `System.IO.File.Delete` loop EvidencePath + history files dijalankan INSIDE try/tx block, tapi SEBELUM `SaveChangesAsync` L2532 + `CommitAsync` L2538. Kalau SaveChanges/Commit fail post-File-Delete, rollback DB → row masih ada tapi file gone. Severity HIGH karena evidence file = legal record coaching.

**Remediation pointer:** Move file delete POST `tx.CommitAsync()` (Phase 323 pattern). Plus refactor catch L2540 jangan `throw` raw — return RedirectToAction dengan TempData["Error"] friendly.

**Recommended fix phase:** P-332 candidate (CDP-specific, terpisah karena role-tier guard distinct dari Admin endpoints).

---

### 4.7 DeleteBagian (DocumentAdminController.cs:283-365)

**Severity:** HIGH
**Dimensions failing:** D2 (file delete sebelum SaveChanges), D6 (no try/catch around SaveChanges), D7 (no tx)

**Evidence ringkas:** L327 + L343 `System.IO.File.Delete` archived KKJ/CPDP files dijalankan SEBELUM L350 `SaveChangesAsync`. Try block L354 hanya wrap audit log. Multi-step file+DB tanpa tx.

**Reproduction path:** Bagian dengan 2 archived KKJ files → delete trigger File.Delete kedua file → simulasi DB outage saat SaveChanges → file gone tapi bagian + archived rows masih ada (dangling FilePath).

**Remediation pointer:** Wrap `BeginTransactionAsync` + move file delete POST commit + try/catch DbUpdateException around SaveChanges.

**Recommended fix phase:** P-333 candidate (Document admin, terpisah controller).

---

### 4.8 DeleteKompetensi (ProtonDataController.cs:1516-1590)

**Severity:** HIGH
**Dimensions failing:** D2 (orphan EvidencePath files saat ProtonDeliverableProgresses RemoveRange)

**Evidence ringkas:** L1554-1561 RemoveRange `ProtonDeliverableProgresses` tanpa cleanup `EvidencePath` / `EvidencePathHistory` physical files. Kompetensi delete cascade ke deliverables + progress + sessions, tapi evidence files yang attached ke progress orphan di disk.

**Remediation pointer:** Iterate progresses sebelum RemoveRange, collect EvidencePath + EvidencePathHistory, `File.Delete` POST commit. Plus refactor catch L1584 jangan expose `ex.Message` ke client.

**Recommended fix phase:** P-334 candidate (Proton-specific, terpisah karena interaksi dengan ActionItems + Deliverables tree).

---

## 5. MED Findings

MED = D3 (audit) / D4 (auth) / D6 (error handling) fail. 4 endpoint dari Section 3A.

| # | Endpoint | File:Line | Dimensions Failing (D3/D4/D6) | 1-line Issue Summary | Recommended Fix |
|---|----------|-----------|-------------------------------|----------------------|-----------------|
| 1 | DeleteCategory | AssessmentAdminController.cs:462 | D6 (no try/catch DbUpdateException) | FK violation 500 raw kalau ada Category dependent row (saat ini Category=string non-FK di session, jadi low-impact tapi defensive coding gap) | Wrap SaveChanges dalam try/catch DbUpdateException + friendly TempData. |
| 2 | DeletePackage | AssessmentAdminController.cs:5038 | D6 (try block hanya around audit log) | FK violation 500 raw kalau Package masih reference UPA actively | Wrap SaveChanges di try/catch DbUpdateException; move audit log to outer try. |
| 3 | DeleteQuestion | AssessmentAdminController.cs:5950 | D3 (NO audit log), D6 (no try/catch) | Silent question delete + raw 500 on FK violation | Tambah `_auditLog.LogAsync("DeleteQuestion", ...)` + try/catch DbUpdateException. |
| 4 | DeleteOrganizationUnit | OrganizationController.cs:357 | D3 (NO audit log), D6 (no try/catch) | Silent unit delete + raw 500 on FK violation | Tambah `_auditLog.LogAsync("DeleteOrganizationUnit", ...)` + try/catch DbUpdateException. |
| 5 | NotificationService.DeleteAsync | NotificationService.cs:270 | D3 (NO audit log) | Silent notification delete; D4 N/A (service layer) | Optional: audit log jika stakeholder ingin track notification dismiss (low priority, possible noise). |

**Note:** 5 MED finding di atas SEPARATE dari 8 HIGH finding section 4 — beberapa HIGH row JUGA punya MED-level gap (e.g., DeleteCoachingSession D6 ⚠️ generic catch + DeleteKompetensi D6 ⚠️ info leak). Severity HIGH menyerap MED ke remediation bundle yang sama.

---

## 6. LOW Findings

LOW = hanya D7 (transaction wrap) fail dengan D1-D6 semua ✅. Endpoint single-step delete tanpa file ops atau multi-step cascade kompleks.

| # | Endpoint | File:Line | Dimensions Failing | 1-line Issue Summary | Recommended Fix |
|---|----------|-----------|-------------------|----------------------|-----------------|

Tidak ada LOW murni dalam audit ini — endpoint dengan D7 fail SEMUA punya juga D2 atau D3/D6 fail yang mengeskalasi ke HIGH/MED. Hygiene tx-wrap concern tertangkap di remediation HIGH/MED bundle.

**Statement:** No LOW findings — semua D7 fail endpoints sudah diserap ke HIGH/MED via dim lain.

---

## 7. Out-of-Scope Statement

Audit ini SECARA EKSPLISIT tidak mengevaluasi area di bawah (sesuai D-02 lock di CONTEXT.md):

1. **Identity framework — `UserManager.DeleteAsync`**: ASP.NET Identity adalah separate concern dengan framework-managed cascade (AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens). Audit cascade pada Identity layer = separate phase (kalau pernah dipikir).
2. **Soft-delete / `IsDeleted` flag**: Codebase saat ini pakai hard delete. Adopsi soft-delete model (e.g., `IsDeleted` column + global query filter) = product decision, scope v20.0+, bukan audit ini.
3. **Concurrency / optimistic locking**: `[ConcurrencyCheck]` / `RowVersion` field audit pada Delete endpoint = v20.0+ concern. Phase 312 sudah pasang TOCTOU re-check di DeleteAssessment, tapi systematic concurrency audit out-of-scope.
4. **Idempotency / re-delete behavior**: Apa yang terjadi jika user double-click Delete (2x POST)? Phase 325 P05 sudah pasang pre-check pattern yang sebagian besar idempotent-safe via `if (record == null) return NotFound()`. Tapi UX-level idempotency (e.g., return success on second delete) = separate UX concern.
5. **Cascade in stored procedures**: Codebase pakai EF Core exclusively, no T-SQL stored procedures for delete logic confirmed via `grep -r "CREATE PROCEDURE" Migrations/` (none found). Stored proc cascade = out-of-scope (none exists).

Lima area di atas TIDAK dievaluasi dalam audit ini sesuai D-02. Bila stakeholder ingin audit salah satu area, buka phase baru via `/gsd-add-phase`.

---

## 8. Remediation Pattern Template (Phase 323 Gold Standard)

**Reference:** `Controllers/AssessmentAdminController.cs` `DeleteAssessment(int id)` dari L2011 sampai closing brace L2193 (post-commit `f1849367` Phase 323 final, ditambah Phase 325 P05 D-11 pre-check L2040-2052).

**Pattern checklist (apply ke endpoint HIGH/MED):**

1. `[HttpPost] + [Authorize(Roles = "Admin, HC")] + [ValidateAntiForgeryToken]` attribute trio → **D4**
2. `if (entity == null) return NotFound();` early guard
3. Pre-check renewal chain references **OUTSIDE tx scope** untuk fail-fast UX (Phase 325 P05 D-11): `await _context.TrainingRecords.CountAsync(t => t.RenewsXxxId == id)` + `await _context.AssessmentSessions.CountAsync(a => a.RenewsXxxId == id)`, return TempData["Error"] jika count > 0 → **D5**
4. `using var tx = await _context.Database.BeginTransactionAsync();` → **D7**
5. Pre-load child entities yang FK Restrict ke target → **D1 prep**
6. `_context.<ChildTable>.RemoveRange(...)` per FK Restrict child (EditLogs, UPA, etc) → **D1**
7. `_context.<TargetTable>.Remove(target);` → **D1**
8. `await _context.SaveChangesAsync();` → **D2 (DB first)**
9. `await tx.CommitAsync();` → **D7**
10. **POST commit** — physical file delete: `if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(absolutePath)) System.IO.File.Delete(absolutePath);` → **D2 (file post-DB)**
11. `await _auditLog.LogAsync("Delete<Entity>", description, id, "<EntityName>");` → **D3**
12. `catch (DbUpdateException ex) { logger.LogWarning(...); TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar."; return RedirectToAction(...); }` → **D6**

**Verbatim snippet (excerpt L2011-2193 — pre-check + tx scope + cascade + audit):**

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAssessment(int id)
{
    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();

    try
    {
        var assessment = await _context.AssessmentSessions
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assessment == null)
        {
            logger.LogWarning($"Delete attempt failed: Assessment {id} not found");
            TempData["Error"] = "Assessment not found.";
            return RedirectToAction("ManageAssessment");
        }

        var assessmentTitle = assessment.Title;
        logger.LogInformation($"Attempting to delete assessment {id}: {assessmentTitle}");

        // D-19: Block delete individual jika bagian Pre-Post group
        if (assessment.AssessmentType == "PreTest" || assessment.AssessmentType == "PostTest")
        {
            TempData["Error"] = "Sesi ini bagian dari grup Pre-Post Test. Gunakan 'Hapus Grup' untuk menghapus keduanya.";
            return RedirectToAction("ManageAssessment");
        }

        // Phase 325 P05 D-11: pre-check referencing rows SEBELUM buka tx scope (fail-fast UX friendly).
        var refTr = await _context.TrainingRecords
            .CountAsync(t => t.RenewsSessionId == id);
        var refAs = await _context.AssessmentSessions
            .CountAsync(a => a.RenewsSessionId == id);

        if (refTr + refAs > 0)
        {
            var total = refTr + refAs;
            TempData["Error"] = $"Tidak bisa hapus: {total} sertifikat lain "
                              + "menggunakan record ini sebagai sumber renewal. "
                              + "Hapus atau update sertifikat pemakai terlebih dulu.";
            return RedirectToAction("ManageAssessment");
        }

        // PHASE 312 WR-01: bungkus guard + cascade dalam transaction (TOCTOU mitigation)
        using var tx = await _context.Database.BeginTransactionAsync();

        // ... role-tier guard + snapshot pre-delete counts ...

        // PHASE 323: Delete AssessmentEditLogs (Restrict FK — must be removed before session)
        var editLogs = await _context.AssessmentEditLogs
            .Where(e => e.AssessmentSessionId == id)
            .ToListAsync();
        if (editLogs.Any())
        {
            _context.AssessmentEditLogs.RemoveRange(editLogs);
        }

        // Delete PackageUserResponses (Restrict FK)
        var pkgResponses = await _context.PackageUserResponses
            .Where(r => r.AssessmentSessionId == id)
            .ToListAsync();
        if (pkgResponses.Any())
            _context.PackageUserResponses.RemoveRange(pkgResponses);

        // ... AssessmentAttemptHistory + UserPackageAssignments + AssessmentPackages cascade ...

        _context.AssessmentSessions.Remove(assessment);

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        // Audit log POST commit
        await _auditLog.LogAsync(
            deleteUser?.Id ?? "",
            deleteActorName,
            "DeleteAssessment",
            $"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} EditLogsCount={preDeleteEditLogsCount}",
            id,
            "AssessmentSession");

        TempData["Success"] = $"Assessment '{assessmentTitle}' has been deleted successfully.";
        return RedirectToAction("ManageAssessment");
    }
    catch (DbUpdateException ex)
    {
        // Phase 325 P05 D-05: safety net jika race TOCTOU antara pre-check dan tx commit.
        logger.LogWarning(ex, "Delete failed for AssessmentSession {Id}: FK constraint", id);
        TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
        return RedirectToAction("ManageAssessment");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting assessment {Id}", id);
        TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi.";
        return RedirectToAction("ManageAssessment");
    }
}
```

**Catatan methodology drift:** Pattern Phase 323 original (commit `f1849367`) sebatas `RemoveRange(AssessmentEditLogs)` + cascade. Phase 325 P05 D-11 menambah pre-check `RenewsSessionId` pattern. Saat ini gold standard = Phase 323 cascade + Phase 325 pre-check hybrid. **Pre-check pattern preferred over null-clear** untuk UX clarity (block dengan friendly error vs. silent orphan-renewal). Endpoint candidate fix berikutnya SHOULD mengikuti hybrid pattern ini.

---

## 9. Recommended Next Phases (Proposal Only)

**PROPOSAL ONLY — user decides priority post-audit. Setiap phase fix berikutnya = separate `/gsd-add-phase` + `/gsd-plan-phase` cycle. TIDAK auto-spawned (D-10).**

Sortir by severity (HIGH first) lalu by remediation cost (S < M < L).

| # | Proposed Phase Slug | Scope | Severity Trigger | Estimated Effort | Source Section |
|---|---------------------|-------|------------------|------------------|----------------|
| 1 | `fix-cascade-deletetraining-deletemanualassessment-atomicity` | DeleteTraining + DeleteManualAssessment: wrap `BeginTransactionAsync`, move `File.Delete` POST commit (D2+D7 fix). D5 sudah OK post-Phase-325. | HIGH | **S-M** (~2 endpoint, mirror Phase 323 pattern, no schema change, ~80 LoC delta) | §4.1 + §4.2 |
| 2 | `fix-cascade-deleteassessmentgroup-deleteprepostgroup-renewal-precheck` | Pasang renewal chain pre-check di DeleteAssessmentGroup + DeletePrePostGroup (D5 fix, paralel Phase 325 P05 D-11). | HIGH | **S** (~2 endpoint, 1 controller, ~40 LoC delta, no migration) | §4.4 + §4.5 |
| 3 | `fix-cascade-deletebagian-file-atomicity` | DeleteBagian: wrap tx + move File.Delete POST commit + try/catch DbUpdateException (D2+D6+D7). | HIGH | **S-M** (~1 endpoint, archived files loop, ~50 LoC delta) | §4.7 |
| 4 | `fix-cascade-deletecoachingsession-file-atomicity` | DeleteCoachingSession: move evidence File.Delete loop POST tx.Commit (D2 fix), refactor catch friendly (D6 polish). | HIGH | **M** (~1 endpoint, complex revert state logic, careful regress test pasif/active session) | §4.6 |
| 5 | `fix-cascade-deletekompetensi-orphan-evidence-files` | DeleteKompetensi: iterate progresses, collect EvidencePath + History, File.Delete POST commit (D2). Plus refactor catch jangan expose `ex.Message`. | HIGH | **M** (~1 endpoint, nested SubKompetensi → Deliverable → Progress tree, careful JsonSerializer history parsing) | §4.8 |
| 6 | `fix-cascade-deleteworker-renewal-files-tx` | DeleteWorker: pre-check renewal cross-user, file cleanup loop, wrap tx full method, refactor UserManager.DeleteAsync interaction (D2+D5+D7 — multiple HIGH). | HIGH | **L** (~1 endpoint, massive 9-step cascade, UserManager.DeleteAsync interaction risk, full regression sweep needed) | §4.3 |
| 7 | `fix-med-deletecategory-deletepackage-deletequestion-deleteorganizationunit-deletenotification` | Bundle 5 MED finding: try/catch DbUpdateException + tambah audit log mana yang missing (DeleteQuestion + DeleteOrganizationUnit). | MED | **S** (~5 endpoint, mostly mechanical add try/catch + audit log, ~100 LoC delta total) | §5 |

**Catatan prioritisasi:**
- **Quick wins (bundle): #2 (S) + #7 (S)** — minimal regression risk, mechanical fixes, bisa shipped dalam 1 sesi.
- **Single-bug high-value: #1 (S-M)** — pre-confirmed HIGH dari brainstorm D-06, surface area kecil, lifetime risk tinggi (sertifikat orphan).
- **High-effort high-value: #6 (L)** — DeleteWorker = paling kompleks tapi paling tinggi blast radius (user lifecycle).
- **Could batch:** #1+#2 (same controller AssessmentAdmin) saat code freeze ok.

**User akan jalankan `/gsd-add-phase` per phase yang ingin di-execute setelah review audit ini.**

---

## Appendix — Methodology Drift Note (Reviewer)

**D-06 brainstorm 2026-05-27** menyatakan DeleteTraining + DeleteManualAssessment HIGH karena **"renewal chain bug + file-DB atomicity broken"**. Audit ini 2026-05-28 konfirmasi: **renewal chain sudah TER-FIX oleh Phase 325 P05** (commit range 7069ead2..77a9c375 SHIPPED LOCAL belum push) via pre-check pattern. Sisa HIGH dimensi = D2 (file-DB atomicity) + D7 (no tx). Severity HIGH tetap valid via D2 fail, BUKAN D5.

**Implikasi planner P-329:** Scope fix DeleteTraining + DeleteManualAssessment sekarang FOKUS ke atomicity + tx wrap saja (~80 LoC delta), TIDAK perlu re-implement renewal pre-check (sudah ada Phase 325).

**Implikasi planner P-331:** DeleteAssessmentGroup + DeletePrePostGroup tetap butuh renewal pre-check (Phase 325 P05 gap — hanya cover DeleteAssessment).
