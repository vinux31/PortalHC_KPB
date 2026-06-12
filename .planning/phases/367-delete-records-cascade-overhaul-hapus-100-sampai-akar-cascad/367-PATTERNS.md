# Phase 367: Delete Records Cascade Overhaul - Pattern Map

**Mapped:** 2026-06-12
**Files analyzed:** 12 (3 baru, 9 modifikasi)
**Analogs found:** 11 / 12 (1 service baru = orkestrasi analog existing, bukan no-analog murni)
**Bahasa:** Indonesia (CLAUDE.md project rule)

> Catatan drift: semua line number di dokumen ini diverifikasi LANGSUNG terhadap kode aktual 2026-06-12 (post-363/366/371), BUKAN line spec-era 2026-06-10. Phase 363 menggeser `AssessmentAdminController.cs` (kini 7137 baris); Phase 366 menambah image-cleanup; Phase 371 menambah seam online. Planner WAJIB re-verify hanya `ResetAssessment` (#20, ~:4013-4046, A2 belum diverifikasi langsung).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Services/RecordCascadeDeleteService.cs` | service (baru) | event-driven (cascade BFS) + transform | `AssessmentAdminController.DeleteAssessment` (execute per-node) + `CoachCoacheeMappingDeletePreview` (preview read-only) | role-match (orkestrasi multi-analog) |
| `Controllers/TrainingAdminController.cs` — `DeleteManualAssessment` → generik per-session (L-07) | controller | request-response (HTMX delete) | `DeleteManualAssessment` existing (refactor diri sendiri) + `DeleteAssessment` cascade | exact |
| `Controllers/TrainingAdminController.cs` — `DeleteTraining` (honesty L-06 + cascade) | controller | request-response (HTMX delete) | `DeleteTraining` existing + `DeleteTabResult` | exact |
| `Controllers/TrainingAdminController.cs` — `DeleteTabResult` (split sukses/gagal L-06) | utility (helper privat) | request-response (HX-Trigger) | `DeleteTabResult` existing :561-569 | exact (modifikasi diri) |
| `Controllers/TrainingAdminController.cs` — guard duplikat `AddManualAssessment`/`ImportTraining`/`BulkBackfillAssessment` (D-02) | controller | CRUD (insert guard) | `AddManualAssessment` insert loop :684-715 | exact |
| `Controllers/AssessmentAdminController.cs` — `DeleteAssessment` (preserve 366 + route ke service) | controller | request-response | `DeleteAssessment` existing :2185-2383 (gold standard) | exact |
| `Controllers/AssessmentAdminController.cs` — `DeleteAssessmentGroup` (#18 sibling filter + preserve 366) | controller | request-response | sibling query :2408-2414 | exact |
| `Controllers/AssessmentAdminController.cs` — `DeletePrePostGroup` (preserve 366) | controller | request-response | `DeletePrePostGroup` :2582-2769 | exact |
| `Controllers/AssessmentAdminController.cs` — `ResetAssessment` guard IsManualEntry (#20) | controller | request-response | `EditAssessment` guard (bandingkan saat plan) | role-match (verify line) |
| `Services/WorkerDataService.cs` — badge recompute (#16/#17, D-01) | service | transform (formula) | `passedAssessmentLookup` :303-313 + `completedTrainings` :332-334 | exact |
| `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — tombol hapus per-baris + flash + modal trigger (S1/S2/S3) | view (Razor partial) | request-response (HTMX) | seam 371 :366-410 + modal `_AssessmentGroupsTab.cshtml` | exact (extension point) |
| `HcPortal.Tests/RecordCascade*Tests.cs` (5 file baru) | test | (verifikasi) | `ImageCleanupIntegrationTests.cs` (fixture) + `PackageImageDeleteTests.cs` ([Fact] file) | exact |

---

## Pattern Assignments

### `Services/RecordCascadeDeleteService.cs` (service baru, cascade BFS + preview)

Service ini BUKAN primitif baru — ia mengorkestrasi 4 analog existing. Signature di RESEARCH §"Cascade Engine Architecture" (`BuildPreviewAsync` read-only + `ExecuteAsync` 1-tx).

**Analog #1 — execute per-node `AssessmentSession` (PARITY gold standard):** `Controllers/AssessmentAdminController.cs` `DeleteAssessment` lines 2271-2342.

Urutan RemoveRange yang HARUS di-mirror per node session (Restrict FK ordering, sudah teruji 363):
```csharp
// Source: AssessmentAdminController.cs:2271-2339 (VERIFIED 2026-06-12)
// 1. EditLogs (Restrict FK — before session)
var editLogs = await _context.AssessmentEditLogs.Where(e => e.AssessmentSessionId == id).ToListAsync();
if (editLogs.Any()) _context.AssessmentEditLogs.RemoveRange(editLogs);
// 2. PackageUserResponses (Restrict FK — before session)
var pkgResponses = await _context.PackageUserResponses.Where(r => r.AssessmentSessionId == id).ToListAsync();
if (pkgResponses.Any()) _context.PackageUserResponses.RemoveRange(pkgResponses);
// 3. AttemptHistory (no FK — orphan if not removed) — by SessionId
var attemptHistory = await _context.AssessmentAttemptHistory.Where(h => h.SessionId == id).ToListAsync();
if (attemptHistory.Any()) _context.AssessmentAttemptHistory.RemoveRange(attemptHistory);
// 4. UserPackageAssignments (Restrict FK ke Package — before packages)
var pkgAssignments = await _context.UserPackageAssignments.Where(a => a.AssessmentSessionId == id).ToListAsync();
if (pkgAssignments.Any()) _context.UserPackageAssignments.RemoveRange(pkgAssignments);
// 5. Packages + Questions + Options (collect ImagePath/SertifikatUrl SEBELUM RemoveRange)
var packages = await _context.AssessmentPackages
    .Include(p => p.Questions).ThenInclude(q => q.Options)
    .Where(p => p.AssessmentSessionId == id).ToListAsync();
// ... foreach pkg: RemoveRange Options → Questions → Packages
// 6. _context.AssessmentSessions.Remove(assessment);
```

**Tambahan 367 per node session (TIDAK ada di gold standard — ini delta):**
1. `LinkedSessionId` null-clear pasangan SEBELUM Remove (#8, Pitfall 3) — atau pastikan pasangan ikut cascade.
2. PendingProtonBypass **soft-cancel** (L-04, kode di RESEARCH §"Soft-cancel") — `Status="Dibatalkan"` + `ResolvedAt`, BUKAN Remove.
3. `RemoveExamOriginAsync` jika node Proton (#9) — lihat analog #4 di bawah.
4. UserNotifications eksak-match (L-05) — query di RESEARCH §"L-05 Inventory" (HANYA `/CMP/StartExam/{id}` terbukti aktif).

**Analog #2 — preview read-only (GET-preview-partial convention):** `Controllers/CoachMappingController.cs` `CoachCoacheeMappingDeletePreview` lines 1196-1227.
```csharp
// Source: CoachMappingController.cs:1196-1227 (VERIFIED 2026-06-12)
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> CoachCoacheeMappingDeletePreview(int id)
{
    var mapping = await _context.CoachCoacheeMappings.FindAsync(id);
    if (mapping == null) return NotFound(new { success = false, message = "..." });
    // ... read-only count queries, NO mutation ...
    return Json(new { coachName, coacheeName, assignmentCount, progressCount });
}
```
Pola: GET + `[Authorize(Roles="Admin, HC")]` + entity-exists guard + read-only queries + return Json/Partial. Untuk 367, `BuildPreviewAsync` return `List<CascadeNode>` (TANPA mutasi) yang di-render partial modal (bukan Json — UI-SPEC S1 minta tree partial).

**Analog #3 — file atomicity post-commit warn-only (L-08):** `Controllers/TrainingAdminController.cs` `DeleteTraining` lines 579-623.
```csharp
// Source: TrainingAdminController.cs:579-623 (VERIFIED 2026-06-12)
// Capture path SEBELUM Remove (record detached post-Remove):
string? sertifikatPath = null;
if (!string.IsNullOrEmpty(record.SertifikatUrl))
    sertifikatPath = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
// ... tx wrap Remove + SaveChanges + Audit + CommitAsync ...
// File.Delete POST commit, inner try/catch warn-only:
if (sertifikatPath != null && System.IO.File.Exists(sertifikatPath))
{
    try { System.IO.File.Delete(sertifikatPath); }
    catch (Exception ex) { _logger.LogWarning(ex, "File.Delete post-commit failed: {Path}", sertifikatPath); }
}
```
Cascade engine kumpulkan SEMUA `SertifikatUrl`/`ManualSertifikatUrl` ter-collect, File.Delete loop post-commit (#19).

**Analog #4 — cabut penanda Proton (#9):** `Services/ProtonCompletionService.cs` `RemoveExamOriginAsync` lines 113-129.
```csharp
// Source: ProtonCompletionService.cs:113-129 (VERIFIED 2026-06-12)
public async Task<bool> RemoveExamOriginAsync(string coacheeId, int protonTrackId)
// Filter Origin=="Exam" (Interview/Bypass KEBAL). SaveChanges internal :127 (A4 — test rollback in-tx).
```
Call-site di engine:
```csharp
if (session.ProtonTrackId.HasValue)   // node Proton
    await _protonCompletion.RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value);
```

**DI registration:** `Program.cs` line ~57 (pola existing `AddScoped<HcPortal.Services.ProtonCompletionService>()`).
```csharp
// Source: Program.cs:51-68 (VERIFIED — pola AddScoped service kelas konkret)
builder.Services.AddScoped<HcPortal.Services.RecordCascadeDeleteService>();
```
Inject: `ApplicationDbContext`, `ILogger<RecordCascadeDeleteService>`, `ProtonCompletionService`, `AuditLogService`, `IWebHostEnvironment`.

**AuditLog (L-08):** pola gold standard `DeleteAssessment` lines 2349-2364 (`_auditLog.LogAsync(userId, actorName, action, description, entityId, entityType)`, 1 entri/operasi, POST commit, try/catch warn-only).

---

### `Controllers/TrainingAdminController.cs` — `DeleteManualAssessment` generik per-session (L-07)

**Analog:** method existing `DeleteManualAssessment` lines 976-1032 (refactor diri sendiri).

**Perubahan kunci #1 — hapus gate `&& s.IsManualEntry` (L-07):**
```csharp
// SEBELUM (existing :978):
var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == id && s.IsManualEntry);
// SESUDAH (367 L-07 — 1 endpoint layani manual + online):
var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == id);
```

**Perubahan kunci #2 — pre-check renewal BLOKIR → cascade (L-03):** hapus blok :989-1001 (pre-check `referencingTr + referencingAs > 0` → TempData + return). Ganti: panggil `RecordCascadeDeleteService.ExecuteAsync("session", id, mirrorIds)`.

**Atribut keamanan WAJIB preserve (V4 access control):**
```csharp
// Source: TrainingAdminController.cs:973-975 (VERIFIED)
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin, HC")]
```

**`RenewsSessionId` gotcha (Pitfall 2):** komentar existing :987 eksplisit "FK column = RenewsSessionId (BUKAN RenewsTrainingId)". Node session → anak via `RenewsSessionId`. Node training → anak via `RenewsTrainingId`. Jangan tertukar.

---

### `Controllers/TrainingAdminController.cs` — `DeleteTabResult` honesty split (L-06)

**Analog:** method existing lines 561-569 (akar sukses-palsu #1).
```csharp
// SEBELUM (existing :561-569 — SELALU recordDeleted, sukses-palsu #1):
private IActionResult DeleteTabResult()
{
    if (IsHtmxRequest())
    {
        Response.Headers["HX-Trigger"] = "recordDeleted";   // selalu sukses
        return new EmptyResult();
    }
    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
}
```
**SESUDAH (L-06 — split sukses/gagal, kode di RESEARCH §"Honest HTMX trigger"):**
```csharp
// Sukses → recordDeleted (re-fetch + flash hijau S3)
Response.Headers["HX-Trigger"] = "recordDeleted";
// Gagal → recordDeleteFailed + payload + HTTP status gagal (flash merah S3)
Response.StatusCode = StatusCodes.Status400BadRequest;
Response.Headers["HX-Trigger"] = System.Text.Json.JsonSerializer.Serialize(
    new { recordDeleteFailed = new { pesan = errorMsg } });
```
Catch `DbUpdateException`/`Exception` HARUS lewat jalur gagal (Pitfall 5). Pesan generik, JANGAN leak `ex.Message` (V7, pola Phase 334 D6).

---

### `Controllers/TrainingAdminController.cs` — guard duplikat 3 pintu (D-02, #12/#14)

**Analog:** insert loop `AddManualAssessment` lines 684-715 (`Add(session)` :714).

EXACT match `UserId + Title + CompletedAt` (BEDA dari heuristik mirror ±1 hari #15). Predikat + perilaku per pintu di RESEARCH §"Guard Duplikat 3 Pintu":

| Pintu | Method:line | Perilaku | Predikat cek |
|-------|-------------|----------|--------------|
| AddManualAssessment | :653-725, `Add` :714 | **REJECT** (`ModelState.AddModelError` + `return View(model)`) | `AnyAsync(s => s.UserId==wc.UserId && s.Title==model.Title && s.CompletedAt==model.CompletedAt && s.IsManualEntry)` |
| ImportTraining | :1255 (`Add`) | **SKIP-with-report** (`result.Status="Skip"`, `continue`) | `AnyAsync(... CompletedAt==parsedDate ...)` |
| BulkBackfillAssessment | :821-848, `Add` :845 | **SKIP-with-report** (kolom status, jangan increment `success`) | `AnyAsync(...)` — hati-hati DALAM tx (`BeginTransactionAsync` :817); track in-memory set untuk dup intra-batch |

Catatan: `ImportTrainingResult` (`Models/ImportTrainingResult.cs`) saat ini `Status="Success"|"Error"` — tambah `"Skip"` (atau reuse `BudgetTrainingImportResult` yang sudah punya "Skip"). Performa: pre-load existing keys `ToHashSet` daripada `AnyAsync` per-row (N query).

---

### `Controllers/AssessmentAdminController.cs` — 3 endpoint tab 1 (PRESERVE 366, D-04)

**Analog + KONTRAK PRESERVE:** `DeleteAssessment` :2185-2383, `DeleteAssessmentGroup` :2389-..., `DeletePrePostGroup` :2582-2769.

**Image-cleanup 366 yang HARUS dipertahankan verbatim (D-04, Pitfall 4):**
```csharp
// Source: AssessmentAdminController.cs:2318-2325 (collect, DeleteAssessment) — VERIFIED
var imagePaths = packages
    .SelectMany(p => p.Questions)
    .SelectMany(q => new[] { q.ImagePath }.Concat(q.Options.Select(o => o.ImagePath)))
    .Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).Distinct().ToList();
// ... :2346 (POST tx.CommitAsync) — helper call:
await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, logger, imagePaths, "DeleteAssessment image");
```

**Call-site eksak per endpoint (preserve verbatim, RESEARCH §"Overlap 366"):**

| Endpoint | Collect imagePaths | Helper call POST-commit | Logger |
|----------|-------------------|------------------------|--------|
| `DeleteAssessment` | :2318-2325 | :2346 `"DeleteAssessment image"` | `logger` LOKAL (:2191 `GetRequiredService`) |
| `DeleteAssessmentGroup` | :2513-2520 | :2542 `"DeleteAssessmentGroup image"` | `logger` LOKAL :2391 |
| `DeletePrePostGroup` | :2704-2711 | :2732 `"DeletePrePostGroup image"` | `logger` LOKAL :2588 |

**GOTCHA logger (KRITIS):** Di 3 method ini `logger` adalah variabel **LOKAL** (`var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();`), BUKAN field `_logger`. Di `TrainingAdminController.cs` sebaliknya pakai field `_logger`. Jangan tertukar saat refactor.

**Rekomendasi arsitektur (RESEARCH Opsi B):** image soal (366) = ranah endpoint, preserve verbatim. File sertifikat (`ManualSertifikatUrl`/`SertifikatUrl`, #19) = ranah cascade engine 367. Dua jenis file, dua jalur cleanup, TIDAK tumpang tindih.

**#18 sibling over-match (`DeleteAssessmentGroup`):** sibling query existing :2408-2414:
```csharp
// Source: AssessmentAdminController.cs:2408-2414 (VERIFIED) — TANPA filter (= temuan #18)
var siblings = await _context.AssessmentSessions
    .Where(a => a.Title == rep.Title && a.Category == rep.Category && a.Schedule.Date == scheduleDate)
    .ToListAsync();
```
Tambah filter `LinkedGroupId == null && AssessmentType bukan PreTest/PostTest && !IsManualEntry` (OQ-3 — verifikasi terhadap tab-1 list query saat plan).

---

### `Controllers/AssessmentAdminController.cs` — `ResetAssessment` guard (#20)

**Analog:** guard `EditAssessment` (bandingkan saat plan). Line ~:4013-4046 BELUM diverifikasi langsung (file 7137 baris, A2). **Planner WAJIB grep `ResetAssessment` + verify line sebelum edit.** Tambah guard `IsManualEntry` → tolak dengan pesan (manual tak boleh di-reset).

---

### `Services/WorkerDataService.cs` — badge recompute (#16/#17, D-01)

**Analog:** formula existing `passedAssessmentLookup` :303-313 + `completedTrainings` :332-334.
```csharp
// Source: WorkerDataService.cs:303-313 (#16 — count ALL IsPassed termasuk online) — VERIFIED
passedAssessmentLookup = await _context.AssessmentSessions.AsNoTracking()
    .Where(a => userIds.Contains(a.UserId) && a.IsPassed == true)
    .GroupBy(a => a.UserId).Select(g => new { UserId = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.UserId, x => x.Count);
// :332-334 (#17 — hanya Passed/Valid/Permanent)
var completedTrainings = trainingRecords.Count(tr =>
    tr.Status == "Passed" || tr.Status == "Valid" || tr.Status == "Permanent");
```
**D-01 RECOMPUTE = baris tampil:** count per jenis = jumlah baris yang benar-benar tampil di partial per jenis (online+manual+training), supaya badge cocok list. `CompletionDisplayText` di `WorkerTrainingStatus.cs:55-57`. Ini fix data/formula, BUKAN view (IC-4).

---

### `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — tombol hapus + flash + modal (S1/S2/S3)

**Analog seam:** existing :366-410 (kolom Aksi, 3 cabang `@if row.IsOnline`).

**IC-3 — online jadi deletable:** cabang `@if (row.IsOnline)` :368-379 saat ini HANYA "Lihat hasil" (komentar :371 "placeholder Phase 367 — extension point"). TAMBAH `btn-sm btn-outline-danger` + `bi bi-trash` identik manual/training, dalam `d-flex gap-1` yang sama. Online tetap NO Edit (View + Delete saja).

**IC-1 — rewire SEMUA 3 tombol hapus (online baru + training + manual):** BUKAN lagi `hx-post` langsung. Trigger `GET DeletePreview(type, id)` → inject modal body → show. Hapus `hx-confirm` native (D-03 — modal pengganti). Pola tombol existing yang di-rewire:
```html
<!-- Source: _TrainingRecordsTab.cshtml:387-393 (training) & :402-408 (manual) — VERIFIED -->
<button type="button" class="btn btn-sm btn-outline-danger" title="Hapus"
        hx-post="@Url.Action("DeleteTraining", "TrainingAdmin")"
        hx-vals='@Html.Raw($"{{\"id\": {row.Id}, \"__RequestVerificationToken\": \"{antiToken}\"}}")'
        hx-confirm="Hapus training record ini?"  <!-- HAPUS (D-03) -->
        hx-swap="none">
    <i class="bi bi-trash"></i>
</button>
```

**Anon-shape 10-property (Pitfall 6, lesson 354/371):** proyeksi :296-303 `{ Type, Date, Title, Detail, Status, StatusClass, ValidUntil, Id, IsOnline, CanViewResult }`. JAGA shape identik saat tambah kolom/baris — `RuntimeBinderException` runtime (build hijau, browser 500). Verifikasi via Playwright runtime, bukan cuma build.

**Re-fetch listener preserve (:178-183):** hidden div `hx-trigger="recordDeleted from:body"`, `hx-include="#filterFormTraining"`. **Preserve filter.** Flash S3 di-render DI ATAS struktur ini (top of partial), bukan menggantikan re-fetch.

**Modal shell analog (S1):** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:462-518` (`deleteAssessmentModal` — `modal-header bg-danger text-white`, scrollable, server-rendered URLs, aria-live spinner). Lihat UI-SPEC §"Component Inventory" untuk peta reuse lengkap.

---

### `HcPortal.Tests/RecordCascade*Tests.cs` (5 file test baru)

**Analog #1 — integration real-SQL disposable fixture (#5-11, L-04, L-08):** `HcPortal.Tests/ImageCleanupIntegrationTests.cs` lines 31-67.
```csharp
// Source: ImageCleanupIntegrationTests.cs:31-67 (VERIFIED) — pola fixture disposable
public class ImageCleanupFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    // _cs = "Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;..."
    public async Task InitializeAsync()  // ctx.Database.MigrateAsync() full chain
    public async Task DisposeAsync()     // ctx.Database.EnsureDeletedAsync()
}
[Trait("Category", "Integration")]   // skip via --filter "Category!=Integration"
public class ...IntegrationTests : IClassFixture<...Fixture>
```
Seed renewal-chain (RESEARCH §"Cara seed"): `ApplicationUser` minimal DULU (FK), lalu session induk → TR anak (`RenewsSessionId=induk.Id`) → AS cucu (`RenewsTrainingId=anak.Id`) + artefak per-tabel. Assert: `ctx.<Table>.CountAsync(...) == 0` (hapus) / `Status=="Dibatalkan"` (soft-cancel L-04) / `LinkedSessionId==null` (null-clear #8) / AuditLog `.Description` contains Ids turunan.

**Analog #2 — [Fact] file-on-disk post-commit (#19):** `HcPortal.Tests/PackageImageDeleteTests.cs` lines 18-45 (logic-contract) + Phase 355 `Replace_NewFileWins_DeletesOldFileOnDisk` + `ImageCleanupIntegrationTests` `WriteFakeImage`/`MakeTempWebRoot` :83-95.
```csharp
// Source: ImageCleanupIntegrationTests.cs:83-95 (VERIFIED) — temp webroot + fake file helper
private static string MakeTempWebRoot() { ... Path.Combine(Path.GetTempPath(), "hcportal-imgtest-"+Guid...); }
private static string WriteFakeImage(string webRoot, string relUrl) { ... File.WriteAllBytes(physical, new byte[]{1,2,3}); }
```
Assert `File.Exists(certPath) == false` post-commit untuk sertifikat manual.

**Analog #3 — unit (no SQL):** `FakeNotificationService.cs` untuk inject; traversal/preview==execute/cycle-guard/badge/guard-duplikat/mirror-heuristik = pola unit InMemory atau logic-contract (`PackageImageDeleteTests` style). Quick run: `dotnet test HcPortal.Tests --filter "Category!=Integration"`.

5 file Wave 0 (RESEARCH §"Wave 0 Gaps"): `RecordCascadeServiceTests`, `RecordCascadeIntegrationTests`, `RecordCascadeFileTests`, `DuplicateGuardTests`, `MirrorHeuristicTests`, `BadgeRecomputeTests` + sibling/reset/ui tests. Baseline: 229/229 (post-366).

---

## Shared Patterns

### Authentication / Authorization (V4)
**Source:** `TrainingAdminController.cs:973-975` + `AssessmentAdminController.cs:2186-2188`
**Apply to:** SEMUA endpoint delete baru/refactored (terutama generik per-session L-07)
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```
Antiforgery token di HTMX: `__RequestVerificationToken` via `hx-vals`/body (pola existing :389).

### File Atomicity Post-Commit (L-08)
**Source:** `TrainingAdminController.cs:579-584, 617-623` (capture path pre-Remove → File.Delete post-commit warn-only)
**Apply to:** cascade engine (sertifikat) + 3 endpoint tab 1 (image via helper 366)
Pola: collect path SEBELUM RemoveRange < SaveChanges < CommitAsync < File.Delete (inner try/catch per file). DB-first; orphan file acceptable, data loss tidak.

### Honest HTMX Flash (L-06)
**Source:** `TrainingAdminController.cs:561-569` (akar #1 yang di-split) + UI-SPEC IC-2
**Apply to:** semua endpoint delete dari tab Input Records
Sukses → `HX-Trigger: recordDeleted`. Gagal → `HX-Trigger: recordDeleteFailed` + payload + HTTP status gagal. Flash di-render DI DALAM partial ter-swap (`alert alert-success`/`alert alert-danger`).

### Error Handling — no info leak (V7)
**Source:** Phase 334 D6 pola (NO `+ ex.Message` ke user) + `DeleteAssessment:2370-2382`
**Apply to:** semua catch di cascade engine + endpoint
```csharp
catch (DbUpdateException ex) {
    logger.LogWarning(ex, "..."); // detail ke log
    // pesan generik ke user, JANGAN ex.Message:
    TempData["Error"] = "Gagal hapus: ada constraint database yang dilanggar.";
}
```

### Path Traversal Confinement (V12)
**Source:** `TrainingAdminController.cs:583` `Path.Combine(_env.WebRootPath, url.TrimStart('/'))`
**Apply to:** semua File.Delete di cascade engine

### Image-Cleanup 366 Preserve (D-04)
**Source:** `Helpers/ImageFileCleanup.DeleteUnreferencedAsync` (signature di RESEARCH :131-137)
**Apply to:** 3 endpoint tab 1 (preserve verbatim, jangan duplikasi/hilangkan)
GOTCHA: `logger` LOKAL di `AssessmentAdminController` Delete* methods, BUKAN `_logger`.

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| (tidak ada full no-analog) | — | — | `RecordCascadeDeleteService` adalah service BARU, tapi setiap sub-pola punya analog kuat (gold-standard delete, preview, file-atomicity, proton-helper). Yang BARU murni = orkestrasi BFS rekursif lintas-tabel + cycle-guard `HashSet<(string,int)> visited` — tidak ada analog cascade-traversal existing; blueprint di RESEARCH §"Traversal". Mirror-heuristik ±1 hari (#15) juga tanpa analog langsung (query baru, RESEARCH §"Mirror Legacy"). |

**Catatan untuk planner:** untuk 2 elemen tanpa analog langsung (BFS traversal + mirror heuristik), gunakan blueprint RESEARCH §"Cascade Engine Architecture" + §"Mirror Legacy" sebagai sumber pola (bukan RESEARCH Code Examples eksternal).

---

## Metadata

**Analog search scope:** `Controllers/` (AssessmentAdmin, TrainingAdmin, CoachMapping, ProtonData, CMP), `Services/` (ProtonCompletion, WorkerData), `Views/Admin/Shared/` (_TrainingRecordsTab, _AssessmentGroupsTab), `Program.cs`, `HcPortal.Tests/`
**Files scanned (read langsung):** 9 (AssessmentAdminController, TrainingAdminController, CoachMappingController, ProtonCompletionService, WorkerDataService, _TrainingRecordsTab.cshtml, Program.cs, ImageCleanupIntegrationTests, PackageImageDeleteTests)
**Drift verification:** semua line aktual diverifikasi 2026-06-12 (kongruen dengan RESEARCH Drift table). HANYA `ResetAssessment` #20 (~:4013-4046) belum diverifikasi langsung — planner verify.
**Pattern extraction date:** 2026-06-12

---

## PATTERN MAPPING COMPLETE
