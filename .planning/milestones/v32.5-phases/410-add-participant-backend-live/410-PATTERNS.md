# Phase 410: Add-Participant Backend Live - Pattern Map

**Mapped:** 2026-06-21
**Files analyzed:** 2 (1 modified controller + 1 new test file)
**Analogs found:** 2 / 2 (both EXACT in-repo analogs; all file:line VERIFIED via Read)

> Catatan: Phase 410 = backend murni, **menambah ke** `AssessmentAdminController.cs` existing (bukan file controller baru). Semua "pattern" = potongan kode NYATA dari batch-create existing yang endpoint baru harus cermin. migration=FALSE (semua kolom sudah ada; 409 sudah tambah kolom removal).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` → `AddParticipantsLive` (HttpPost) | controller (endpoint mutasi AJAX) | request-response + CRUD (create sesi atomic) | **BULK ASSIGN** block `EditAssessment :2151-2272` (sesi-create + tx + notif + audit) — varian Pre/Post `:1942-1998` | EXACT (role + data-flow) |
| `Controllers/AssessmentAdminController.cs` → `GetEligibleParticipantsToAdd` (HttpGet) | controller (endpoint read picker) | request-response (read-only query) | eligible-user source `CreateAssessment GET :655-659` + batch-existence `:2161-2168` | EXACT |
| `Controllers/AssessmentAdminController.cs` → `BuildReadyParticipantSession(...)` (helper privat baru) | utility (session factory) | transform (rep → AssessmentSession ready) | session-init `:2195-2217` (BULK) + tx template `InjectAssessmentService :143-171` | role-match |
| `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` (NEW) | test | event-driven (assert write-path + read-path) | **eligible/idempotency** → `ParticipantRemovalExcludeTests :46-107` (InMemory real-controller); **write-path** → `FlexibleParticipantAddFixture :20-53` (SQLEXPRESS disposable) | EXACT (2 pola, sesuai 2 jenis assert) |

---

## Pattern Assignments

### `AddParticipantsLive` (HttpPost) — controller, request-response + CRUD

**Analog utama:** `Controllers/AssessmentAdminController.cs:2151-2272` (BULK ASSIGN) — referensi TERDEKAT: sudah lakukan idempotency-filter + validate-users + session-init + `BeginTransactionAsync` + notif post-commit + audit, dalam satu blok. Endpoint baru = blok ini diangkat jadi action AJAX dengan param `sessionId` representatif (bukan `id` + `NewUserIds` form-post).

**Attribute/RBAC pattern** (cermin `DeleteAssessment :2286-2288`):
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddParticipantsLive(int sessionId, List<string> userIds)
```
> Kedua atribut LOCKED (spec §H). `GetEligibleParticipantsToAdd` = `[HttpGet]` + `[Authorize(Roles="Admin, HC")]` saja (read-only; antiforgery tak wajib di GET).

**Resolve rep + batch-key (server-authoritative, anti-tampering)** — terima `sessionId`, JANGAN batchKey mentah. Pola batch-key = `Title + Category + Schedule.Date` (sama persis dengan `:2163-2165`):
```csharp
// resolve rep aktif untuk inherit (Pitfall 5 + Open-Q 2: pilih rep RemovedAt==null)
var rep = await _context.AssessmentSessions
    .FirstOrDefaultAsync(s => s.Id == sessionId);   // → null ⇒ return NotFound()/Json 404
```

**Proton reject (guard dini, sebelum write)** — literal konsisten di repo (`:945/1191/3456`):
```csharp
if (rep.Category == "Assessment Proton")
    return BadRequest(new { error = "Penambahan peserta tidak didukung untuk Assessment Proton." });
```

**Window guard (mirror `CMPController.StartExam :968-969`)** — pesan LOCKED beda dari StartExam:
```csharp
// VERIFIED CMPController.cs:968-969
if (rep.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > rep.ExamWindowCloseDate.Value)
    return BadRequest(new { error = "Window ujian sudah tutup, tidak bisa tambah peserta." });
// window null (.HasValue==false) = bebas; WIB = UTC+7
```

**Idempotency by existence (cermin BULK ASSIGN `:2161-2174`)** — D-01: skip user dengan sesi APAPUN (tanpa pandang RemovedAt) untuk konsistensi Restore-only:
```csharp
// VERIFIED :2162-2168 (BULK ASSIGN). 410 D-01: HAPUS filter RemovedAt → skip sesi APAPUN
var alreadyInBatch = await _context.AssessmentSessions
    .Where(a => a.Title == rep.Title && a.Category == rep.Category && a.Schedule.Date == rep.Schedule.Date)
    .Select(a => a.UserId).Distinct().ToListAsync();
var toAdd   = userIds.Where(uid => !alreadyInBatch.Contains(uid)).Distinct().ToList();
var skipped = userIds.Where(uid => alreadyInBatch.Contains(uid)).Distinct().ToList(); // → skipped[] D-03
```
> ⚠️ **Beda dari BULK ASSIGN** (`:2161` skip TANPA filter RemovedAt sudah benar untuk D-01). Jangan tambah `&& a.RemovedAt == null` di sini — itu akan izinkan dobel-sesi untuk user removed (langgar Restore-only).

**Validate user IDs exist (cermin `:2178-2189`)**:
```csharp
var userDictionary = await _context.Users
    .Where(u => toAdd.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
var missingUsers = toAdd.Except(userDictionary.Keys).ToList();   // → 400 jika ada
```

**Session-init field-inherit (cermin `:2195-2217` BULK ASSIGN)** — inherit dari `rep`, status ready, kolom removal null:
```csharp
// VERIFIED :2195-2217 — Pitfall 2: AssessmentType ?? "Standard" (NOT NULL Dev/Prod)
var session = new AssessmentSession
{
    Title           = rep.Title,
    Category        = rep.Category,
    AssessmentType  = rep.AssessmentType ?? "Standard",          // :2201 (UAT fix 391)
    Schedule        = rep.Schedule,
    DurationMinutes = rep.DurationMinutes,
    Status          = DeriveReadyStatus(rep.Schedule, rep.ExamWindowCloseDate),  // :2204 → Open/Upcoming, NEVER InProgress
    PassPercentage  = rep.PassPercentage,
    AllowAnswerReview = rep.AllowAnswerReview,
    ShuffleQuestions  = rep.ShuffleQuestions,
    ShuffleOptions    = rep.ShuffleOptions,
    GenerateCertificate = rep.GenerateCertificate,
    ExamWindowCloseDate = rep.ExamWindowCloseDate,
    IsTokenRequired = rep.IsTokenRequired,
    AccessToken     = rep.AccessToken,
    BannerColor     = rep.BannerColor,
    Progress        = 0,
    UserId          = uid,
    CreatedBy       = actorId
    // StartedAt/CompletedAt/RemovedAt = null (default) — WAJIB tetap null (PART-06)
};
```

**Transaksi atomic + commit + notif post-commit (cermin `:2219-2258` BULK ASSIGN)** — pola identik dengan `InjectBatchAsync :88-421`:
```csharp
// VERIFIED :2219-2258
_context.AssessmentSessions.AddRange(newSessions);
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await _context.SaveChangesAsync();           // dapat Id (untuk Pre/Post cross-link + JSON)
    // audit DALAM tx (atau setelah commit — BULK pakai setelah; pilih konsisten)
    await transaction.CommitAsync();
    // notif HANYA setelah commit (Pitfall 4) — swallow per-notif (:2230-2240)
    foreach (var ns in newSessions) {
        try {
            await _notificationService.SendAsync(ns.UserId, "ASMT_ASSIGNED", "Assessment Baru",
                $"Anda telah di-assign assessment \"{ns.Title}\"", $"/CMP/StartExam/{ns.Id}");
        } catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
    }
}
catch { await transaction.RollbackAsync(); throw; }   // :2254-2257 — rollback → 0 sesi (PART-06 atomic)
```

**Audit (cermin `:2244-2250`)** — actionType CONTEXT = `"AddParticipantLive"`:
```csharp
await _auditLog.LogAsync(actorId, actorName, "AddParticipantLive",
    $"Added {newSessions.Count} participant(s) to '{rep.Title}' ({rep.Category})", rep.Id, "AssessmentSession");
```

**Actor + audit-name resolve** (VERIFIED `:5582-5583`):
```csharp
var hcUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
```

**JSON response (D-03 + D-04)** — return baris baru untuk DOM inject 412 (SignalR broadcast DEFER 412):
```csharp
return Json(new {
    added    = newSessions.Select(s => new { id = s.Id, fullName = ..., nip = ..., status = s.Status }),
    skipped  = skipped.Select(uid => new { /* fullName, nip dari userDictionary/Users lookup */ }),
    addedCount = newSessions.Count, skippedCount = skipped.Count
});
```
> ⚠️ **JANGAN** sentuh `_hubContext` untuk `participantAdded` (D-04 / Anti-Pattern RESEARCH) — broadcast = Phase 412.

---

### Pre/Post branch (PART-07) — bagian dari `AddParticipantsLive`

**Analog:** `Controllers/AssessmentAdminController.cs:1942-1998` (add-to-existing-PrePost di EditAssessment). ⚠️ Spec/CONTEXT menyebut `:1926` — **drift +16 baris**; lokasi AKTUAL pair-create = `:1942-1998` (`:1926` kini cascade-delete package). Pakai `:1942-1998`.

**Detect (single-source predikat, cermin `AdminBaseController :248-249`)**:
```csharp
bool isPrePost = IsPrePostSession(rep);   // AssessmentType == "PreTest" || "PostTest"
```

**Pair-create + cross-link (VERIFIED `:1942-1998`)**:
```csharp
var linkedGroupId = rep.LinkedGroupId!.Value;     // :1946 — ambil dari rep (gabung grup existing)
var newPre  = new AssessmentSession { ..., AssessmentType = "PreTest",  LinkedGroupId = linkedGroupId,
                                      Status = DeriveReadyStatus(prePre.Schedule, ...) };   // ⚠ ganti hardcoded "Upcoming" :1956 → DeriveReadyStatus (PART-06)
var newPost = new AssessmentSession { ..., AssessmentType = "PostTest", LinkedGroupId = linkedGroupId,
                                      Status = DeriveReadyStatus(...) };
_context.AssessmentSessions.AddRange(newPre, newPost);
await _context.SaveChangesAsync();                 // :1994 — dapat Id
newPre.LinkedSessionId  = newPost.Id;              // :1996-1997 cross-link
newPost.LinkedSessionId = newPre.Id;
await _context.SaveChangesAsync();
```
> ⚠️ **Deviasi sadar dari analog:** `:1956/:1977` hardcode `Status = "Upcoming"`. Untuk 410, ganti `DeriveReadyStatus(schedule, window)` (PART-06 ready-status). Test PART-07 assert 2 sesi (PreTest+PostTest) + `LinkedSessionId` cross-set + status ready.

---

### `GetEligibleParticipantsToAdd` (HttpGet) — controller, read-only

**Analog:** user-source `CreateAssessment GET :655-659` + batch-existence `:2161-2168`.

**User source (VERIFIED `:655-659`)** — D-02: TANPA filter unit/section:
```csharp
var users = await _context.Users
    .Where(u => u.IsActive)                          // :656 — sumber pekerja existing
    .OrderBy(u => u.FullName)                        // :657
    // D-02 (OVERRIDE spec §B4): TANPA unit/section filter — admin bebas tambah siapa saja
    .Select(u => new { u.Id, u.FullName, u.NIP })
    .ToListAsync();
```

**Exclude-by-batch (D-01: exclude sesi APAPUN, RemovedAt diabaikan)**:
```csharp
var rep = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);  // → 404 jika absen
var alreadyInBatch = await _context.AssessmentSessions
    .Where(a => a.Title == rep.Title && a.Category == rep.Category && a.Schedule.Date == rep.Schedule.Date)
    // D-01: TANPA filter RemovedAt — user soft-removed TETAP excluded (hanya balik via Restore 411)
    .Select(a => a.UserId).Distinct().ToListAsync();
var eligible = users.Where(u => !alreadyInBatch.Contains(u.Id)).ToList();
return Json(eligible);
```
> Discretion (A2): exclude akun admin/HC bersifat opsional. Repo TIDAK punya pemakaian `GetUsersInRoleAsync` existing — bila dipakai = pola baru. Default aman: `IsActive` saja (idempotency tetap melindungi).

---

### `BuildReadyParticipantSession(...)` (helper privat baru) — utility, transform

**Analog pola:** session-init `:2195-2217` (field-inherit) + tx wrapper `InjectAssessmentService :143-171` (AMBIL POLA tx, BUKAN kode — semantik inject = Completed+graded+cert, beda).

Rekomendasi signature (RESEARCH primary): `BuildReadyParticipantSession(AssessmentSession rep, string userId, string? actorId)` → emit `AssessmentSession` ready (field di atas). Pre/Post = helper emit pair atau caller branch. Helper dipakai juga oleh jalur create existing / `RemoveParticipantLive` (411) untuk hindari duplikasi (CONTEXT).

**⚠️ KEPUTUSAN DESAIN A1 (planner WAJIB putuskan di awal — eager vs lazy UPA):**
- **Opsi A (REKOMENDASI, zero-regresi):** helper emit **`AssessmentSession` saja**. `UserPackageAssignment` dibuat LAZY oleh `CMPController.StartExam :1064-1102` saat worker buka ujian — identik dengan SEMUA peserta existing (BULK ASSIGN `:2151` & Pre/Post `:1942` juga tak buat UPA). "Siap-mulai" terpenuhi via status Open/Upcoming.
- **Opsi B (eager):** helper juga emit `UserPackageAssignment` via `ShuffleEngine.BuildQuestionAssignment` (pola `CMPController :1074` / `:5557`) — butuh load `AssessmentPackages` batch + hitung `workerIndex`. Tambah kompleksitas + risiko `workerIndex` drift. Pilih HANYA bila stakeholder mau UPA di DB segera.
- CONTEXT/spec "buat `UserPackageAssignment` cermin paket" = literal opsi B, tapi langgar pola existing. **Angkat sebagai keputusan eksplisit; test integration cermin opsi yang dipilih.**

**Lazy-UPA reference (VERIFIED `CMPController.cs:1064-1102`)** — bukti tak ada peserta punya UPA sebelum StartExam:
```csharp
var assignment = await _context.UserPackageAssignments.FirstOrDefaultAsync(a => a.AssessmentSessionId == id);
if (assignment == null) {
    var shuffledIds = ShuffleEngine.BuildQuestionAssignment(packages, assessment.ShuffleQuestions, workerIndex, rng);
    // ... build optionShuffle ... sentinel package ...
    assignment = new UserPackageAssignment {
        AssessmentSessionId = id, AssessmentPackageId = sentinelPackage.Id, UserId = user.Id,
        ShuffledQuestionIds = JsonSerializer.Serialize(shuffledIds),
        ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionShuffleDict) };
    _context.UserPackageAssignments.Add(assignment);   // lazy, on first StartExam
}
```

---

### `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` (NEW) — test

**Dua pola, sesuai dua jenis assert** (de-tautology WAJIB, lesson 999.12 — test JALANKAN logika ASLI, jangan replica predikat):

#### Pola 1 — Eligible / idempotency / exclude-by-batch → InMemory real-controller
**Analog:** `HcPortal.Tests/ParticipantRemovalGuardTests.cs:46-107` (kelas `ParticipantRemovalExcludeTests`).

**MakeController helper (VERIFIED `:51-85`)** — instantiate controller ASLI dengan service null untuk yang tak dipakai:
```csharp
var ctx = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var ctrl = new AssessmentAdminController(
    ctx, userManager: null!, auditLog: new AuditLogService(ctx), env: null!,
    cache: new MemoryCache(new MemoryCacheOptions()), logger: NullLogger<...>.Instance,
    notificationService: null!, hubContext: null!, workerDataService: null!,
    gradingService: null!, protonCompletionService: null!, protonBypassService: null!);
ctrl.ControllerContext = new ControllerContext {
    HttpContext = new DefaultHttpContext(),
    ActionDescriptor = new ControllerActionDescriptor { ActionName = "GetEligibleParticipantsToAdd" } };
// + ViewData/TempData/Url stub bila action sentuh View()/Url.Action
```
> ⚠️ `GetEligibleParticipantsToAdd` return `Json(...)` (bukan View) → tak perlu StubUrlHelper/ViewData penuh; tetap set `ActionDescriptor.ActionName` (override View() ref). EF InMemory drop baris dengan FK absen → seed `ApplicationUser` cocok tiap `UserId` (pola `MakeUser :102-107`).

**Test exclude-by-batch (D-01):** seed batch dengan 1 sesi aktif + 1 sesi `RemovedAt != null` untuk user berbeda → panggil `await ctrl.GetEligibleParticipantsToAdd(repId)` ASLI → assert KEDUA user excluded (removed TETAP excluded, bukan ditawarkan).

#### Pola 2 — Write-path (ready-status / Pre-Post / window / atomic) → SQLEXPRESS disposable
**Analog:** `HcPortal.Tests/FlexibleParticipantAddTests.cs:20-84` (fixture + seed helpers).

**Fixture (VERIFIED `:20-53`)** — DB disposable `HcPortalDB_Test_{guid}`, `MigrateAsync` (HcPortalDB_Dev TIDAK disentuh), `[Trait("Category","Integration")]`:
```csharp
public class FlexibleParticipantAddFixture : IAsyncLifetime {
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    // _cs = Server=localhost\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;...
    public async Task InitializeAsync() { ...; await ctx.Database.MigrateAsync(); }
    public async Task DisposeAsync()    { await ctx.Database.EnsureDeletedAsync(); }
}
[Trait("Category", "Integration")]
public class FlexibleParticipantAddTests : IClassFixture<FlexibleParticipantAddFixture> { ... }
```
> ⚠️ **Anti-tautology gap di analog existing:** `FlexibleParticipantAddTests :86-99` pakai **REPLICA** `DeriveReadyStatus`/`IsRunning` (test data-level Plan 01, controller-berat tak di-drive). Untuk 410, write-path test boleh seed via SQLEXPRESS + assert kolom NYATA (ready-status, RemovedAt null, 2 sesi Pre/Post + LinkedSessionId, window-reject 0-write). Idealnya drive action ASLI; bila controller terlalu berat untuk write-path (antiforgery/userManager), minimal pakai SQLEXPRESS dengan kolom nyata (BUKAN replica predikat `WindowAllowsAddition` fiktif = anti-pattern 999.12). De-tautology: eligible/idempotency WAJIB lewat real-controller (Pola 1).

**Seed helpers (VERIFIED `:63-84`):** `SeedUserAsync` (ApplicationUser), `SeedSiblingSessionAsync` (sesi batch dgn startedAt/completedAt/examWindowCloseDate). Tambah: seed batch Pre/Post (AssessmentType PreTest+PostTest + LinkedGroupId/LinkedSessionId), seed batch Proton (`Category="Assessment Proton"`), seed sesi `RemovedAt != null` (exclude D-01).

**Test map (dari RESEARCH §Validation):**
| Req | Behavior | Pola |
|-----|----------|------|
| PART-06 | sesi baru ready-status (Open/Upcoming, NEVER InProgress); StartedAt/CompletedAt/RemovedAt null | Pola 2 SQLEXPRESS |
| PART-06 | idempotent: user dengan sesi di batch → skipped[] (tak dobel) | Pola 1 real-controller (idempotency guard) |
| PART-06 | window tutup (`ExamWindowCloseDate < now+7h`) → 400 + 0-write | Pola 2 (assert no-write) |
| PART-06 | atomic: gagal create → rollback (0 sesi) | Pola 2 (opsional) |
| PART-06 | eligible exclude user dgn sesi APAPUN di batch (incl. removed, D-01) | Pola 1 real-controller |
| PART-07 | Pre/Post → pair Pre+Post (2 sesi + LinkedSessionId cross-set) | Pola 2 |
| (scope §F) | Proton (`Category=="Assessment Proton"`) → tolak + 0-write | Pola 1 atau 2 |

---

## Shared Patterns

### Authentication / RBAC
**Source:** `AssessmentAdminController.cs:2286-2288` (DeleteAssessment) — pola endpoint mutasi admin.
**Apply to:** KEDUA endpoint baru.
```csharp
[Authorize(Roles = "Admin, HC")]   // KEDUA endpoint (mutasi + read picker) — spec §H, V4
[ValidateAntiForgeryToken]          // HANYA AddParticipantsLive (POST) — V13; GET picker tak wajib
```

### Server-authoritative resolve (anti-tampering)
**Source:** prinsip `InjectAssessmentService.ResolveLinkContextAsync` (T-397-06).
**Apply to:** KEDUA endpoint — terima `sessionId` representatif, resolve batch-key (`Title+Category+Schedule.Date`) server-side. JANGAN trust `batchKey`/`LinkedGroupId` mentah dari client (V1, threat Tampering lintas-batch).

### Transaksi atomic + rollback
**Source:** `AssessmentAdminController.cs:2219-2258` (BULK ASSIGN) ≈ `InjectAssessmentService.cs:88-421`.
**Apply to:** `AddParticipantsLive` write-path. `BeginTransactionAsync` → SaveChanges → CommitAsync → notif post-commit; catch → RollbackAsync. Notif/broadcast HANYA setelah commit (Pitfall 4, spec §G).

### Ready-status derivation
**Source:** `AssessmentAdminController.cs:2276-2283` (`DeriveReadyStatus`, Phase 391) — `private static`, kelas sama, panggil LANGSUNG.
**Apply to:** semua sesi baru (standard + Pre/Post). NEVER InProgress.
```csharp
private static string DeriveReadyStatus(DateTime schedule, DateTime? examWindowCloseDate) {
    var nowWib = DateTime.UtcNow.AddHours(7);
    if (schedule <= nowWib) return AssessmentConstants.AssessmentStatus.Open;
    return AssessmentConstants.AssessmentStatus.Upcoming;
}
```

### Notif + audit
**Source:** `AssessmentAdminController.cs:2227-2250`. `INotificationService.SendAsync(userId, type, title, message, actionUrl?)` (`INotificationService.cs:20`); `AuditLogService.LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` (`AuditLogService.cs:21-42`).
**Apply to:** `AddParticipantsLive` — `"ASMT_ASSIGNED"` per user (post-commit, swallow) + audit `"AddParticipantLive"`.

### Pre/Post detect
**Source:** `AdminBaseController.cs:248-249` (`IsPrePostSession`) — single-source predikat.
**Apply to:** branch pair-create di `AddParticipantsLive`.

---

## No Analog Found

Semua file punya analog EXACT in-repo. Tidak ada gap analog.

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| — | — | — | (kosong — 2/2 ter-cover) |

---

## Cross-cutting Pitfalls (carry ke planner)

1. **Line-drift spec** — `:1926` (spec) → AKTUAL `:1942-1998` (Pre/Post pair-create). Verifikasi tiap file:line via Grep saat implement (file di-edit 409; akan di-edit 411 sequential).
2. **AssessmentType NULL** — kolom NOT NULL default 'Standard' Dev/Prod → `rep.AssessmentType ?? "Standard"` (`:2201`).
3. **Idempotency scope (D-01)** — Add skip + Eligible exclude = sesi APAPUN (tanpa pandang RemovedAt). Jangan `&& a.RemovedAt == null` (akan izinkan dobel-sesi user removed).
4. **Notif sebelum commit** — notif/broadcast HANYA setelah CommitAsync (Pitfall 4).
5. **Status hardcoded di Pre/Post analog** — `:1956/:1977` hardcode `"Upcoming"`; 410 ganti `DeriveReadyStatus` (PART-06).
6. **SignalR OUT OF SCOPE** — JANGAN sentuh `_hubContext` untuk `participantAdded` (D-04 → 412).
7. **EF global HasQueryFilter FORBIDDEN** (409 decision) — pakai `.Where(s => s.RemovedAt == null)` eksplisit, bukan query-filter.
8. **Anti-tautology test** — eligible/idempotency lewat real-controller (Pola 1 `ParticipantRemovalExcludeTests`); JANGAN replica predikat (analog `FlexibleParticipantAddTests :86-99` adalah replica Plan-01 — jangan tiru polanya untuk read-path logic).

---

## Metadata

**Analog search scope:** `Controllers/AssessmentAdminController.cs`, `Controllers/CMPController.cs`, `Controllers/AdminBaseController.cs`, `Services/InjectAssessmentService.cs`, `Services/AuditLogService.cs`, `Services/INotificationService.cs`, `HcPortal.Tests/ParticipantRemovalGuardTests.cs`, `HcPortal.Tests/FlexibleParticipantAddTests.cs`
**Files scanned:** 8 (semua file:line VERIFIED via Read; RESEARCH sudah re-verify, PATTERNS re-konfirmasi excerpt langsung)
**Pattern extraction date:** 2026-06-21
**migration:** FALSE (semua kolom ada; 409 sudah tambah removal)
