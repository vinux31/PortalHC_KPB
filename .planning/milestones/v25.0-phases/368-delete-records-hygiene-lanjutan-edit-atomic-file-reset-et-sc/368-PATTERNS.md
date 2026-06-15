# Phase 368: Delete Records Hygiene Lanjutan - Pattern Map

**Mapped:** 2026-06-13
**Files analyzed:** 12 (5 MODIFY controllers/views + 2 NEW artifacts + 5 NEW test files)
**Analogs found:** 12 / 12 (every finding has an in-codebase precedent — pure "follow neighbor pattern")

> Brownfield ASP.NET Core 8 MVC + EF Core (SQL Server). Phase 368 = 7 hygiene fixes (#21-27). Migration=false.
> Most work MODIFIES existing controllers; NEW artifacts = shared dedup helper (#25) + admin one-time-cleanup endpoint+view (#23) + 5 test files.
> **All analogs verified against live source this session** (line refs accurate as of HEAD `b506558e`).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Controllers/TrainingAdminController.cs` → `EditTraining` (#21 file + #26 renewal) | controller | request-response + file-I/O | `EditManualAssessment` (same file) + `DeleteTraining` (Phase 331 atomic) | exact |
| `Controllers/TrainingAdminController.cs` → `EditManualAssessment` (#21 file) | controller | request-response + file-I/O | `DeleteTraining`/cascade engine (Phase 331) | exact |
| `Controllers/TrainingAdminController.cs` → `ImportTraining` (#24) | controller | batch + transform | `BulkBackfillAssessment` (same file) + existing import body | exact |
| `Controllers/TrainingAdminController.cs` → `BulkBackfillAssessment` (#27) | controller | batch | self (in-place: literal → constant) | exact |
| `Controllers/TrainingAdminController.cs` → NEW `CleanupAttemptHistory` GET+POST (#23) | controller (admin maintenance) | request-response + CRUD-delete | `BulkBackfill` GET + `BulkBackfillAssessment` POST (same file) | exact |
| `Views/Admin/CleanupAttemptHistory.cshtml` (NEW, #23) | view | request-response | `Views/Admin/BulkBackfill.cshtml` | exact |
| `Controllers/AssessmentAdminController.cs` → `ResetAssessment` (#22) | controller | CRUD-delete | existing `RemoveRange` block in same method (L3958-3970) | exact |
| `Models/CertificationManagementViewModel.cs` → NEW static `BuildParentNameLookup` (#25) | model (shared static helper) | transform | `SertifikatRow.DeriveCertificateStatus` (same file) + `AdminBaseController` GroupBy (L140-143) | exact |
| `Controllers/CMPController.cs` → `CertificationManagement` callsite (#25) | controller | transform | `AdminBaseController.cs:140-143` (correct GroupBy) | exact |
| `Controllers/CDPController.cs` → `CertificationManagement` callsite (#25) | controller | transform | `AdminBaseController.cs:140-143` (correct GroupBy) | exact |
| `Views/Admin/Index.cshtml:298` + `_AssessmentGroupsTab.cshtml:320` + `BulkBackfill.cshtml` (#27 label) | view | — | self (label text swap) | exact |
| `HcPortal.Tests/*Tests.cs` (5 NEW: EditAtomicFile, ResetEtScore, OrphanCleanup, ImportTrainingAudit, CertDedup, RenewalValidation) | test | — | `PackageImageDeleteTests.cs:209-238` (Phase 355) + `RecordCascadeIntegrationTests.cs` fixture (Phase 367) | exact |

---

## Pattern Assignments

### `EditTraining` — #21 atomic file replace (controller, request-response + file-I/O)

**Analog:** `DeleteTraining` (Phase 331 atomic via `ImageFileCleanup.DeleteUnreferencedAsync`) + `EditManualAssessment` (same-file structure).

**Current NON-atomic code that #21 FIXES** (`TrainingAdminController.cs:515-526`) — `File.Delete` runs BEFORE `SaveFileAsync`, so a failed upload loses the old file:
```csharp
// Handle file upload — replace old file if new file provided
if (model.CertificateFile != null && model.CertificateFile.Length > 0)
{
    if (!string.IsNullOrEmpty(record.SertifikatUrl))
    {
        var oldPath = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);   // ← WRONG: pre-save delete
    }
    var uploadedUrl = await FileUploadHelper.SaveFileAsync(model.CertificateFile, _env.WebRootPath, "uploads/certificates");
    if (uploadedUrl != null) record.SertifikatUrl = uploadedUrl;
}
```

**Target pattern (Phase 331 atomic):** capture `oldUrl` → `SaveFileAsync` (new file) → set new url → `SaveChangesAsync` (commit) → `FileUploadHelper.DeleteFile(oldUrl)` POST-commit, warn-only. **Delete-old strictly conditional on `uploadedUrl != null` AND non-empty `oldUrl`** (D-06; metadata-only edit must keep the file). `SaveChangesAsync` is at L544 (after metadata assignment) — the post-commit `DeleteFile` must follow it.

**FileUploadHelper signatures `[VERIFIED: Helpers/FileUploadHelper.cs]`** — DO NOT hand-roll:
- `static Task<string?> SaveFileAsync(IFormFile?, string webRootPath, string subFolder, ILogger? = null)` → relative URL or null
- `static void DeleteFile(string webRootPath, string? relativeUrl)` → null-safe, checks `File.Exists`
- `static (bool IsValid, string? Error) ValidateCertificateFile(IFormFile?)` → ext + size + magic-byte

---

### `EditTraining` — #26 renewal validation (controller, request-response + DB lookup)

**Analog:** `ManualDuplicatePredicate` single-source pattern (`AdminBaseController.cs:265-267`) — though #26 is an inline lookup, not a shared predicate. Honesty/no-silent-fail from Phase 367 L-06.

**Critical placement facts (from RESEARCH — do not deviate):**
1. **EditTraining uses TempData-redirect, NOT `ModelState→View`.** Existing DAG validation (`TrainingAdminController.cs:483-494`) adds `ModelState.AddModelError`, then L502-510 collects first error to `TempData["Error"]` + `RedirectToAction("ManageAssessment")`. #26 must feed the SAME flow.
2. The existing `if (!ModelState.IsValid)` check at L502 runs **BEFORE** `FindAsync(model.Id)` at L512. #26 needs `record.UserId` + `record.Renews*Id` (old DB values), so validation must run **AFTER L512** but **BEFORE** the assignment at L541-542. Therefore add a **re-check `!ModelState.IsValid` → TempData firstError + redirect** after the #26 block (Open Question 2 recommendation).
3. **Validate only when the field changed** (D-04, legacy-tolerant): compare `model.Renews*Id != record.Renews*Id`.

**Existing TempData-redirect block to mirror** (`TrainingAdminController.cs:502-510`):
```csharp
if (!ModelState.IsValid)
{
    var firstError = ModelState.Values
        .SelectMany(v => v.Errors)
        .Select(e => e.ErrorMessage)
        .FirstOrDefault() ?? "Data tidak valid.";
    TempData["Error"] = firstError;
    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
}
```

**Target #26 block (insert after L513 `if (record == null) return NotFound();`, before assignment L528+):**
```csharp
// #26 D-04/D-05: validate Renews*Id ONLY when the field changed (legacy-tolerant)
if (model.RenewsTrainingId != record.RenewsTrainingId && model.RenewsTrainingId.HasValue)
{
    var src = await _context.TrainingRecords.FindAsync(model.RenewsTrainingId.Value);
    if (src == null || src.UserId != record.UserId)
        ModelState.AddModelError("", "Sertifikat renewal tidak ditemukan atau bukan milik peserta ini.");
}
if (model.RenewsSessionId != record.RenewsSessionId && model.RenewsSessionId.HasValue)
{
    var srcAs = await _context.AssessmentSessions.FindAsync(model.RenewsSessionId.Value);
    if (srcAs == null || srcAs.UserId != record.UserId)
        ModelState.AddModelError("", "Sesi renewal tidak ditemukan atau bukan milik peserta ini.");
}
// then re-check !ModelState.IsValid → TempData firstError + RedirectToAction (mirror L502-510 above)
```
**Discretion (CONTEXT):** exact ModelState wording (generic V7, no internal leak). **Security:** same-user check (`src.UserId == record.UserId`) is the IDOR mitigation — this IS the root of #26.

---

### `EditManualAssessment` — #21 atomic file replace (controller, request-response + file-I/O)

**Analog:** Same Phase 331 atomic pattern. This method **already uses `FileUploadHelper.DeleteFile`** but in the wrong order (`TrainingAdminController.cs:982-987`):
```csharp
if (model.CertificateFile != null && model.CertificateFile.Length > 0)
{
    FileUploadHelper.DeleteFile(_env.WebRootPath, session.ManualSertifikatUrl);   // ← pre-save delete (WRONG)
    var uploadedUrl = await FileUploadHelper.SaveFileAsync(model.CertificateFile, _env.WebRootPath, "uploads/certificates");
    if (uploadedUrl != null) session.ManualSertifikatUrl = uploadedUrl;
}
```
**Target:** capture `oldUrl = session.ManualSertifikatUrl` → `SaveFileAsync` → set new url → `SaveChangesAsync` (L1004) → `FileUploadHelper.DeleteFile(oldUrl)` POST-commit warn-only, only if `uploadedUrl != null`. Note `ManualSertifikatUrl` is the field here (vs `SertifikatUrl` in EditTraining — D-06 applies to BOTH).

---

### `ImportTraining` — #24 audit + constants + GenerateCertificate=isPassed (controller, batch + transform)

**Analog:** `BulkBackfillAssessment` (same file, L894-906 audit pattern) + `_auditLog.LogAsync`.

**Three changes in the Assessment-import branch** (`TrainingAdminController.cs:1295-1308`):
```csharp
// CURRENT (wrong):
GenerateCertificate = true,                 // L1297 — unconditional
...
AssessmentType = ""                         // L1307 — empty literal
// TARGET (#24/D-08):
GenerateCertificate = isPassed,                              // only passed → certificate
AssessmentType = AssessmentConstants.AssessmentType.Manual,  // constant, not ""
```
`isPassed` already exists at L1263 (`lulusStr.Equals("Ya", ...)`). **`AssessmentConstants.AssessmentType.Manual`** = `"Manual"` (`Models/AssessmentConstants.cs:7`).

**Summary audit at end of method** (before `return View(results);` at L1415) — ONE summary row, not per-row (Open Question 3 recommendation; per-row would spam):
```csharp
var actor = await _userManager.GetUserAsync(User);
if (actor != null)
{
    int ok   = results.Count(r => r.Status == "Success");
    int skip = results.Count(r => r.Status == "Skip");
    int err  = results.Count(r => r.Status == "Error");
    await _auditLog.LogAsync(actor.Id, actor.FullName ?? actor.UserName ?? actor.Id,
        "ImportTraining", $"Import: {ok} sukses, {skip} skip, {err} error.", null, "AssessmentSession");
}
```
**`_auditLog.LogAsync` signature `[VERIFIED: Services/AuditLogService.cs:21-27]`:** `(string actorUserId, string actorName, string actionType, string description, int? targetId = null, string? targetType = null)` — SaveChanges internal.

---

### `BulkBackfillAssessment` — #27 constant (controller, batch)

**Analog:** self (in-place literal swap). At `TrainingAdminController.cs:884`:
```csharp
AssessmentType = "Standard",                                // CURRENT
AssessmentType = AssessmentConstants.AssessmentType.Manual, // TARGET (#27/D-09)
```
**Residue accepted by design:** backfilled sessions remain new identities (`Id` baru, `IsManualEntry=true`) — NOT replicas of originals. Do not "fix" identity (deferred).

---

### NEW `CleanupAttemptHistory` GET + POST — #23 orphan cleanup (controller, admin maintenance, idempotent preview→execute+audit)

**Analog:** `BulkBackfill` GET (`TrainingAdminController.cs:760-765`) + `BulkBackfillAssessment` POST (`:767-921`). **Recommended home: `TrainingAdminController`** (has `_auditLog`, `_context`, `_userManager`, `_env`; owns assessment data import; `Admin/[action]` route). Planner discretion on exact form (D-01).

**GET preview analog** (`TrainingAdminController.cs:760-765`) — read-only, returns View:
```csharp
[HttpGet]
[Authorize(Roles = "Admin")]
public IActionResult BulkBackfill()
{
    return View();
}
```

**POST execute analog (auth + CSRF + role)** (`TrainingAdminController.cs:767-771`):
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin")]
```

**Orphan query — idempotent, narrow (D-02)** `[VERIFIED: Models/AssessmentAttemptHistory.cs:9 SessionId = plain int, no FK to session]`:
```csharp
// 'orphan' = AttemptHistory whose SessionId has NO parent AssessmentSession (dangling).
var orphanQuery = _context.AssessmentAttemptHistory
    .Where(h => !_context.AssessmentSessions.Any(s => s.Id == h.SessionId));
int count = await orphanQuery.CountAsync();             // GET preview-count
// POST: var rows = await orphanQuery.ToListAsync();
//       _context.AssessmentAttemptHistory.RemoveRange(rows);
//       await _context.SaveChangesAsync();
//       await _auditLog.LogAsync(actor.Id, actor.FullName, "CleanupAttemptHistory",
//                 $"Hapus {rows.Count} orphan AttemptHistory.", null, "AssessmentAttemptHistory");
```
Re-run = 0 automatically (query empty after first execute → idempotent). **Security:** `[Authorize(Roles="Admin")]` + `[ValidateAntiForgeryToken]` (destructive endpoint); narrow orphan def + preview-count + audit guard against mass over-delete.

---

### NEW `Views/Admin/CleanupAttemptHistory.cshtml` — #23 preview view (view, request-response)

**Analog:** `Views/Admin/BulkBackfill.cshtml` (`:1-45`). Copy: `ViewData["Title"]`, breadcrumb to `Admin/Index`, `<h2>` header card, the `TempData["Success"]`/`TempData["Error"]` alert blocks (`:24-37` verbatim), `card shadow-sm border-0` body. **Contract (D-01):** display preview-count BEFORE a confirm "Hapus" button (POST form with `@Html.AntiForgeryToken()`). Recommended dedicated view (Open Question 1).

---

### `ResetAssessment` — #22 RemoveRange ET scores (controller, CRUD-delete)

**Analog:** the existing `RemoveRange` cleanup block in the SAME method (`AssessmentAdminController.cs:3958-3970`).

**CRITICAL (RESEARCH correction): ResetAssessment has NO explicit transaction.** It uses ONE `SaveChangesAsync()` at L3974, then `ExecuteUpdateAsync` (L3976-3988). **Do NOT add `BeginTransactionAsync`** (scope creep, D-07).

**Existing cleanup block to follow** (`AssessmentAdminController.cs:3958-3974`):
```csharp
var packageResponses = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == id)
    .ToListAsync();
if (packageResponses.Any())
    _context.PackageUserResponses.RemoveRange(packageResponses);

var assignment = await _context.UserPackageAssignments
    .FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
if (assignment != null)
    _context.UserPackageAssignments.Remove(assignment);

await _context.SaveChangesAsync(); // flush archive + delete operations first  ← L3974
```

**Target #22 insert (immediately before L3974 `SaveChangesAsync`):**
```csharp
// #22 D-07: clear stale ET scores so retake regenerates fresh ET (no unique-index violation)
var etScores = await _context.SessionElemenTeknisScores
    .Where(e => e.AssessmentSessionId == id)
    .ToListAsync();
if (etScores.Any())
    _context.SessionElemenTeknisScores.RemoveRange(etScores);
```
**Why:** unique index `IX_SessionElemenTeknisScores_AssessmentSessionId_ElemenTeknis` `[VERIFIED: ApplicationDbContext.cs:629-631]`. On retake, `GradingService` (L174-194) `Add`s new ET with same `AssessmentSessionId` → unique violation → `catch(DbUpdateException)` swallows + `ChangeTracker.Clear()` → ET stays stale. `SessionElemenTeknisScore` FK = `AssessmentSessionId` (`Models/SessionElemenTeknisScore.cs:9-11`). **Do NOT RemoveRange any other analytics (deferred).**

---

### NEW static `BuildParentNameLookup` — #25 dedup helper (model, transform)

**Analog (home):** `SertifikatRow.DeriveCertificateStatus` (`Models/CertificationManagementViewModel.cs:53-65`) — already a `public static` member on `SertifikatRow` consumed by BOTH CMP + CDP. This is the recommended NEUTRAL home (D-03; NOT AdminBaseController — CMP/CDP are plain `Controller`, don't inherit AdminBase). Planner discretion: this vs a new static util class.

**Analog (correct GroupBy logic):** `AdminBaseController.cs:140-143` — the already-fixed dedup:
```csharp
var categoryById = allCategories.ToDictionary(c => c.Id);
var categoryNameLookup = allCategories
    .Where(c => c.ParentId != null && categoryById.ContainsKey(c.ParentId.Value))
    .GroupBy(c => c.Name)                                          // ← dedup duplicate child Name
    .ToDictionary(g => g.Key, g => categoryById[g.First().ParentId!.Value].Name);
```

**Existing static-helper shape on `SertifikatRow` to mirror** (`Models/CertificationManagementViewModel.cs:53`):
```csharp
public static CertificateStatus DeriveCertificateStatus(DateOnly? validUntil, string? certificateType)
{ ... }
```
**Target:** add `public static Dictionary<string,string> BuildParentNameLookup(IEnumerable<(int Id, string Name, int? ParentId)> categories)` (or accept the projected anon-type fields) wrapping the GroupBy logic above. Single-source; both callsites + the #25 unit test consume it.

---

### `CMPController.CertificationManagement` + `CDPController.CertificationManagement` — #25 callsites (controller, transform)

**Analog:** `AdminBaseController.cs:140-143` (correct version). Both controllers currently have the BROKEN `ToDictionary(c => c.Name)` that throws `ArgumentException` (500) on duplicate child Name across different parents.

**CMP broken callsite** (`CMPController.cs:4156-4159`):
```csharp
var categoryById = allCategories.ToDictionary(c => c.Id);
var categoryNameLookup = allCategories
    .Where(c => c.ParentId != null && categoryById.ContainsKey(c.ParentId.Value))
    .ToDictionary(c => c.Name, c => categoryById[c.ParentId!.Value].Name);   // ← throws on dup key
```
**CDP broken callsite** (`CDPController.cs:4005-4009`) — IDENTICAL bug.

**Target:** replace BOTH with a call to the shared `SertifikatRow.BuildParentNameLookup(allCategories)` (zero drift, D-03). Do not GroupBy-inline twice.

---

### `Views/...` — #27 label swap (view)

**Analog:** self (text swap). Three callsites → **"Bulk Import Nilai (Excel)"**:
- `Views/Admin/BulkBackfill.cshtml` — `ViewData["Title"]` (`:2`), breadcrumb (`:10`), `<h2>` (`:16`), subtitle (`:17`)
- `Views/Admin/Index.cshtml:298` — `<span class="fw-bold">Bulk Backfill (Restore Lost Data)</span>` `[VERIFIED]`
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:320` — `...Bulk Backfill (Restore Lost Data)` `[VERIFIED]`

---

### Test files — analogs (test)

**Analog A — [Fact] file-on-disk replace (#21):** `PackageImageDeleteTests.cs:209-238` `Replace_NewFileWins_DeletesOldFileOnDisk` (Phase 355). Uses `MakeTempDir()` + `File.WriteAllBytes` for old/new, asserts old deleted on disk + new survives, with `try/finally Directory.Delete(recursive)`:
```csharp
var dir = MakeTempDir();
try
{
    var oldPath = Path.Combine(dir, "old.jpg");
    var newPath = Path.Combine(dir, "new.jpg");
    File.WriteAllBytes(oldPath, new byte[] { 1 });
    File.WriteAllBytes(newPath, new byte[] { 2 });
    // ... apply replace intent ...
    Assert.False(File.Exists(oldPath), "file LAMA harus terhapus on disk.");
    Assert.True(File.Exists(newPath), "file BARU harus tetap ada.");
}
finally { Directory.Delete(dir, recursive: true); }
```
**A4 risk (RESEARCH):** extracting #21 logic to a static helper for pure [Fact] is MEDIUM-feasibility; fallback = integration controller-level test. Spec §3.4 only mandates [Fact] replace-file.

**Analog B — integration real-SQL fixture (#22/#23/#26):** `RecordCascadeIntegrationTests.cs` (Phase 367). REUSE `RecordCascadeFixture` (`:21-54`, disposable `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, `MigrateAsync`, drop-on-dispose) + `[Trait("Category","Integration")]` (`:67`) + helpers `SeedUserAsync` (`:83-89`), `NewSession(...renewsSession:...)` (`:91-96`), `NewTraining(...)` (`:98-99`), `FakeWebHostEnvironment` (`:57-65`).

> **RESEARCH correction (do NOT assume otherwise):** `SeedRenewalChainAsync` as a NAMED method **DOES NOT EXIST**. Renewal chains are seeded INLINE via `NewSession(userId, "...", renewsSession: rootId)` / `NewTraining(userId, "...", renewsSession: ...)` (see `:114-122`). Planner writes its own seed helper or inlines.

**#23 orphan insert in test:** no FK to session → insert directly: `ctx.AssessmentAttemptHistory.Add(new AssessmentAttemptHistory { SessionId = 999999, UserId = <valid seeded user>, Title="X", ... })`. Seed a valid User (FK to User exists). Assert preview count → execute → re-run = 0 → AuditLog row.

---

## Shared Patterns

### Audit logging (#24, #23)
**Source:** `Services/AuditLogService.cs:21-27` (`_auditLog.LogAsync`)
**Apply to:** `ImportTraining` (#24 summary), `CleanupAttemptHistory` POST (#23)
```csharp
public async Task LogAsync(
    string actorUserId,
    string actorName,
    string actionType,
    string description,
    int? targetId = null,
    string? targetType = null)
```
Note: `LogAsync` SaveChanges internally — use it for POST-commit / standalone audit. For in-tx per-row audit use raw `_context.AuditLogs.Add` (see `BulkBackfillAssessment:894-907`).

### Type constants (#24, #27)
**Source:** `Models/AssessmentConstants.cs:5-11`
**Apply to:** `ImportTraining` (#24), `BulkBackfillAssessment` (#27)
```csharp
public static class AssessmentType
{
    public const string Manual  = "Manual";
    public const string Online  = "Online";
    public const string PreTest = "PreTest";
    public const string PostTest = "PostTest";
}
```
Use `AssessmentConstants.AssessmentType.Manual` — never the string literals `"Manual"`/`"Standard"`/`""`.

### Atomic file replace (#21 — both EditTraining + EditManualAssessment)
**Source:** Phase 331 pattern (`DeleteTraining`/cascade: collect path → commit → `File.Delete` post-commit warn-only)
**Apply to:** `EditTraining` (`SertifikatUrl`), `EditManualAssessment` (`ManualSertifikatUrl`)
Order: capture old → `SaveFileAsync` new → set url → `SaveChangesAsync` → `FileUploadHelper.DeleteFile(old)` warn-only, only if upload succeeded.

### Static single-source helper / predicate (#25, #26)
**Source:** `AdminBaseController.cs:265-267` (`ManualDuplicatePredicate`) + `:140-143` (GroupBy dedup)
**Apply to:** #25 dedup (extracted to `SertifikatRow`, consumed by CMP+CDP), #26 renewal validation (inline same-user lookup)
Anti-drift: one definition, all callsites + tests consume it. **#25 helper goes in `Models/` (SertifikatRow), NOT AdminBaseController** (CMP/CDP don't inherit AdminBase).

### Idempotent admin maintenance endpoint (#23)
**Source:** `BulkBackfill` GET + `BulkBackfillAssessment` POST (`TrainingAdminController.cs:760-921`)
**Apply to:** `CleanupAttemptHistory` GET (preview-count) + POST (RemoveRange + audit)
GET read-only count → POST execute + audit → re-run auto-empty. `[Authorize(Roles="Admin")]` + `[ValidateAntiForgeryToken]` on POST.

---

## No Analog Found

None. Every Phase 368 finding has a direct in-codebase precedent (Phase 331 atomic file, Phase 355 file-test, Phase 367 fixture/predicate, AdminBase GroupBy, BulkBackfill endpoint+view). This phase is "follow the neighbor pattern" — no novel design.

---

## Metadata

**Analog search scope:** `Controllers/` (TrainingAdmin, AssessmentAdmin, CMP, CDP, AdminBase), `Models/` (CertificationManagementViewModel, AssessmentConstants, AssessmentAttemptHistory, SessionElemenTeknisScore), `Services/` (AuditLogService), `Helpers/` (FileUploadHelper), `Views/Admin/`, `HcPortal.Tests/`
**Files scanned (read):** 12 source + 2 grep verifications
**Pattern extraction date:** 2026-06-13
**Line refs verified against:** HEAD `b506558e` (this session)

### Cross-cutting cautions for planner
- **File-overlap with 367:** `TrainingAdminController.cs` + `AssessmentAdminController.cs` (`ResetAssessment`) — 368 depends 367 shipped (preserve `RecordCascadeDeleteService`, `ImageFileCleanup`, `CascadeHasCompletedOrAnsweredAsync`, `IsResettable` guard).
- **Parallel v27.0 (Phases 372-375)** touches `AssessmentAdminController.cs` + `CMPController.cs` — same files. Do NOT plan/execute v27.0 before 368 ships (ROADMAP coordination).
- **Migration=false** — no `dotnet ef migrations` this phase.
