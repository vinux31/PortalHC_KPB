# Phase 411: Remove + Restore Backend Live - Pattern Map

**Mapped:** 2026-06-21
**Files analyzed:** 2 (1 modified controller, 1 new test file)
**Analogs found:** 2 / 2 (both exact, in-repo, verified file:line this session)

> Semua excerpt di bawah DIVERIFIKASI ulang via `Read` terhadap kode produksi terkini (HEAD `46b88d72`), bukan disalin dari RESEARCH. Catatan koreksi: `EnsureCanDeleteAsync` ada di **`:7465`** (RESEARCH menyebut `:7203` di satu tempat — yang BENAR `:7465`). `DeletePrePostGroup` di **`:2830`** (CONTEXT menyebut `:2566` — itu salah; `:2566` adalah baris di dalam `DeleteAssessment`). `DeleteAssessmentPeserta` **tidak ada** di controller (grep di `AssessmentAdminController.cs` = 0 — hanya muncul di view + planning + spec) → stub mati DIKONFIRMASI.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` — `RemoveParticipantLive` (HttpPost JSON) | controller (endpoint) | request-response / CRUD-delete | `AddParticipantsLive` `:2353-2501` (attrs+actor+Proton+window+JSON) ⊕ `DeleteAssessment` `:2545-2665` (cascade-invoke) | exact (split across 2 analogs) |
| `Controllers/AssessmentAdminController.cs` — `RestoreParticipantLive` (HttpPost JSON) | controller (endpoint) | request-response / CRUD-update | `AddParticipantsLive` `:2353-2501` (attrs+actor+audit+JSON) | role-match (no direct restore analog; clear-3-cols inverse of soft-set) |
| `Controllers/AssessmentAdminController.cs` — `DeleteAssessmentPeserta` (HttpPost redirect) | controller (endpoint) | request-response (full-page redirect) | `DeleteAssessment` `:2545-2665` (TempData+RedirectToAction) ⊕ form `EditAssessment.cshtml:666-670` | exact (redirect-delete pattern) |
| `Controllers/AssessmentAdminController.cs` — `RemoveParticipantCoreAsync` (private shared) | service-logic (private method) | CRUD-delete / transform | `AddParticipantsLive` Langkah 4-7 (hybrid orchestration) ⊕ `DeletePrePostGroup` `:2830` partner-loop ⊕ `RecordCascadeDeleteService.ExecuteAsync` `:175` | exact (composed) |
| `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` (NEW) | test (integration) | test | `FlexibleParticipantAddLiveTests.cs` (read InMemory + write SQLEXPRESS) ⊕ `FlexibleParticipantAddTests.cs` fixture | exact (REUSE infra) |

**Catatan organisasi (single file):** Sisipkan 3 endpoint + 2 private helper (`RemoveParticipantCoreAsync` + `SessionHasDataAsync`) di `AssessmentAdminController.cs`, paling natural **tepat sesudah `CreateEagerAssignmentsAsync` `:2542`** dan **sebelum `// --- DELETE ASSESSMENT ---` `:2544`** — mengelompokkan semua endpoint "participant live" (Add → Remove → Restore → DeletePeserta) berdekatan. `RecordCascadeDeleteService` & `IsPrePostSession` & `EnsureCanDeleteAsync` semua dipanggil, BUKAN diubah.

---

## Pattern Assignments

### `RemoveParticipantLive` (controller, request-response / CRUD-delete)

**Analog A — attrs + actor + Proton-reject + JSON shape:** `AssessmentAdminController.cs:2353-2397` (`AddParticipantsLive`)
**Analog B — cascade hard-delete invoke:** `AssessmentAdminController.cs:2618` (`DeleteAssessment`)

**Endpoint signature + attrs pattern** (`AddParticipantsLive` `:2353-2356` — copy verbatim, ganti param):
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RemoveParticipantLive(int sessionId, string? reason)
```

**Load + 404 + Proton-reject pattern** (`AddParticipantsLive` `:2364-2369` — mirror urutan, ganti pesan):
```csharp
var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
if (session == null) return NotFound(new { error = "Sesi tidak ditemukan." });

// Proton reject (guard dini, sebelum write) — mirror :2368
if (session.Category == "Assessment Proton")
    return BadRequest(new { error = "Penghapusan peserta tidak didukung untuk Assessment Proton." });
```

**Idempotency guard** (spec §B2.1 — letakkan paling awal SETELAH load + Proton, SEBELUM resolve partner):
```csharp
if (session.RemovedAt != null)
    return Json(new { sessionId, mode = "noop" });   // no-op sukses (anti double-action, Pitfall 3)
```

**Actor resolution pattern** (`AddParticipantsLive` `:2395-2397` — copy verbatim 3 baris; juga sumber `RemovedBy`):
```csharp
var hcUser = await _userManager.GetUserAsync(User);
var actorId = hcUser?.Id ?? "";
var actorName = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
```

**Wrapper → delegasi ke core → JSON outcome** (CONTEXT discretion: `{ sessionId, mode, linkedSessionId? }`):
```csharp
var outcome = await RemoveParticipantCoreAsync(session, reason, actorId, actorName);
if (!outcome.Ok) return BadRequest(new { error = outcome.Message });   // reason-wajib-soft → 400 di sini
return Json(new { sessionId, mode = outcome.Mode /* "hard"|"soft" */, linkedSessionId = outcome.PartnerId });
// D-03/D-04: JANGAN sentuh _hubContext (broadcast participantRemoved = Phase 412). Mirror komentar :2480-2481.
```

---

### `RemoveParticipantCoreAsync` (private shared logic, CRUD-delete) — JANTUNG fase

**Analog A — hybrid orchestration & transaction:** `AddParticipantsLive` Langkah 4-7 (`:2376-2467`)
**Analog B — cascade hard-delete:** `DeleteAssessment:2618` + `RecordCascadeDeleteService.ExecuteAsync:175`
**Analog C — Pre/Post partner-loop:** `DeletePrePostGroup:2830-2904`

**Has-data detection** (Pattern 1, D-01 — VERIFIED count idiom dari `DeleteAssessment:2599-2600` + `EnsureCanDeleteAsync:2488-2489`):
```csharp
private async Task<bool> SessionHasDataAsync(AssessmentSession s)
{
    if (s.StartedAt != null) return true;                       // StartedAt:46 — sudah mulai = berdata
    return await _context.PackageUserResponses
        .AnyAsync(r => r.AssessmentSessionId == s.Id);          // ada jawaban = berdata
    // D-01: UserPackageAssignment SENGAJA TIDAK dihitung — eager-UPA (410) bukan jejak pengerjaan.
}
```

**Pre/Post partner resolution via `LinkedSessionId`** (Pattern 5 + Pitfall 1 — `LinkedSessionId:186`, cross-link di-set 410 `:2438-2439`; `IsPrePostSession` static `AdminBaseController.cs:248`):
```csharp
AssessmentSession? partner = null;
if (IsPrePostSession(session) && session.LinkedSessionId.HasValue)
    partner = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == session.LinkedSessionId.Value);

// Evaluasi GABUNGAN: salah satu berdata → soft KEDUANYA; keduanya bersih → hard KEDUANYA.
bool anyHasData = await SessionHasDataAsync(session)
                  || (partner != null && await SessionHasDataAsync(partner));
```
> ⚠ ANTI-PATTERN (Pitfall 1): JANGAN pakai `DeletePrePostGroup` / `LinkedGroupId` untuk pair satu peserta. `DeletePrePostGroup:2838-2840` query `Where(a => a.LinkedGroupId == linkedGroupId)` = **seluruh batch lintas-user**. Pair satu peserta = `LinkedSessionId` (per-peserta cross-link). Mirror *logika partner-handling* (loop+evaluasi gabungan), bukan signature/query-nya.

**Reason-wajib gate (D-02)** — DITEMPATKAN SETELAH evaluasi `anyHasData`, SEBELUM eksekusi (Pitfall 5):
```csharp
if (anyHasData && string.IsNullOrWhiteSpace(reason))
    return Outcome.Fail("Alasan penghapusan wajib diisi.");   // 400 hanya pada jalur soft
```

**Soft-remove write (set 3 kolom, JANGAN sentuh lainnya)** (Pattern 3 — kolom `AssessmentSession.cs:99-103`):
```csharp
foreach (var s in new[] { session, partner }.Where(x => x != null))
{
    s!.RemovedAt = DateTime.UtcNow;   // :99 UTC, SUMBER KEBENARAN "removed"
    s.RemovedBy = actorId;            // :101 cermin CreatedBy
    s.RemovalReason = reason;         // :103 WAJIB pada jalur soft (D-02)
    // INVARIANT (Pitfall 2): JANGAN sentuh Score, IsPassed, NomorSertifikat, ManualSertifikatUrl, Status, response.
    //   Guard re-entry 409 (StartExam/SubmitExam/JoinBatch) andalkan RemovedAt — Status InProgress TETAP InProgress.
}
await _context.SaveChangesAsync();
```

**Hard-delete via cascade (reuse, jangan re-implement)** (Pattern 2 — service-resolve idiom `DeleteAssessment:2551`, invoke `:2618`):
```csharp
var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();
foreach (var s in new[] { session, partner }.Where(x => x != null))
{
    var result = await cascade.ExecuteAsync("session", s!.Id, Enumerable.Empty<int>(), actorId, actorName);
    if (!result.Success)
        return Outcome.Fail(result.ErrorMessage ?? "Gagal menghapus peserta.");   // 500-class, pesan generik
}
// CASCADE VERIFIED hapus 9 artefak termasuk UserPackageAssignments (RecordCascadeDeleteService.cs:221-222) →
//   D-01 satisfied OTOMATIS, eager-UPA 410 ikut terhapus. Tak perlu perluas service.
```
> CASCADE COVERAGE (`RecordCascadeDeleteService.cs:212-258`, VERIFIED): `AssessmentEditLogs:212` · `PackageUserResponses:215` · `AssessmentAttemptHistory` by `SessionId`:218 (Pitfall 4: tak orphan User) · **`UserPackageAssignments:221`** · `AssessmentPackages`+`PackageQuestions`+`PackageOptions:224-232` · LinkedSessionId null-clear pasangan `:236-237` · UserNotifications eksak-match `:244-247` · file cert post-commit `:292-301` · audit "CascadeDelete" `:307`. Semua 1-tx (`:204` BeginTransaction → `:283` Commit).

**Audit double-log** (Pattern 6 — cascade tulis "CascadeDelete" internal `:307`; tambah konteks user-facing, pola `DeleteAssessment:2635`):
```csharp
await _auditLog.LogAsync(actorId, actorName, "RemoveParticipantLive",
    $"Removed participant session [ID={session.Id}] '{session.Title}' mode={mode} reason='{reason}'",
    session.Id, "AssessmentSession");
```

**Keputusan #5 (longgarkan `EnsureCanDeleteAsync`):** Endpoint live ini **TIDAK memanggil** `EnsureCanDeleteAsync:7465` sama sekali. Jalur soft-remove justru DIPILIH untuk peserta Completed/cert sehingga HC boleh (cert utuh + reversibel). Jalur hard hanya not-started+0-response (per definisi tak pernah Completed) → guard no-op natural. Mitigasi = soft-remove + audit wajib (spec §H).

---

### `RestoreParticipantLive` (controller, request-response / CRUD-update)

**Analog:** `AddParticipantsLive:2353-2356` (attrs) + soft-remove inverse + audit `:2456`

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RestoreParticipantLive(int sessionId)
{
    var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
    if (session == null) return NotFound(new { error = "Sesi tidak ditemukan." });
    if (session.RemovedAt == null)
        return BadRequest(new { error = "Sesi ini tidak dalam keadaan dihapus." });   // PRMV-04 guard

    var hcUser = await _userManager.GetUserAsync(User);                                 // actor :2395-2397
    var actorId = hcUser?.Id ?? "";
    var actorName = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";

    // Pre/Post simetri (Assumption A2): restore partner via LinkedSessionId juga, konsisten pair-as-unit.
    var partner = (IsPrePostSession(session) && session.LinkedSessionId.HasValue)
        ? await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == session.LinkedSessionId.Value) : null;
    foreach (var s in new[] { session, partner }.Where(x => x != null && x.RemovedAt != null))
    { s!.RemovedAt = null; s.RemovedBy = null; s.RemovalReason = null; }   // clear 3 kolom :99-103
    await _context.SaveChangesAsync();

    await _auditLog.LogAsync(actorId, actorName, "RestoreParticipantLive",
        $"Restored participant session [ID={sessionId}] '{session.Title}'", sessionId, "AssessmentSession");
    return Json(new { sessionId, restored = true });   // D-03/D-04: JANGAN broadcast (412)
}
```

---

### `DeleteAssessmentPeserta` (controller, redirect variant — D-04 fix stub mati)

**Analog A — redirect+TempData delete:** `DeleteAssessment:2545-2548, 2649-2650`
**Analog B — dead form contract (VERIFIED `EditAssessment.cshtml:666-670`):** POST `asp-action="DeleteAssessmentPeserta"`, hidden `sessionId` + `returnToId` (= `Model.Id`) + `@Html.AntiForgeryToken()`. Tak ada field `reason`.

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAssessmentPeserta(int sessionId, int returnToId)
{
    var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
    if (session == null) { TempData["Error"] = "Sesi tidak ditemukan."; return RedirectToAction("EditAssessment", new { id = returnToId }); }
    if (session.Category == "Assessment Proton") { TempData["Error"] = "Tidak didukung untuk Assessment Proton."; return RedirectToAction("EditAssessment", new { id = returnToId }); }

    var hcUser = await _userManager.GetUserAsync(User);
    var actorId = hcUser?.Id ?? "";
    var actorName = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";

    // OPEN Q1 (planner decide): form lama tak punya field reason. Varian redirect = jalur HARD (not-started, reason opsional).
    //   Bila core menentukan jalur SOFT (berdata → butuh reason) → outcome.Ok=false "Alasan wajib" → arahkan ke kontrol
    //   Monitoring Detail (412) yang punya modal+reason. Endpoint JSON RemoveParticipantLive (untuk 412) tetap full hybrid.
    var outcome = await RemoveParticipantCoreAsync(session, reason: null, actorId, actorName);
    TempData[outcome.Ok ? "Success" : "Error"] = outcome.Message;
    return RedirectToAction("EditAssessment", new { id = returnToId });
}
```
> Setelah implement, hapus `style="display:none;"` di form `EditAssessment.cshtml:666` + wire tombol hapus per-peserta (UI-wiring ranah planner — tombol existing jadi "hidup"). RESEARCH State-of-Art: stub grep=0 → delegasi core (D-04).

---

### `HcPortal.Tests/FlexibleParticipantRemoveTests.cs` (NEW — integration)

**Analog:** `FlexibleParticipantAddLiveTests.cs` (struktur 2-bagian) + `FlexibleParticipantAddTests.cs:20-53` (`FlexibleParticipantAddFixture`)

**Fixture REUSE pattern** (`FlexibleParticipantAddTests.cs:20-53` — `HcPortalDB_Test_{guid}` SQLEXPRESS + `MigrateAsync`):
```csharp
// CONN: $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30"
// InitializeAsync → MigrateAsync (kalau gagal = MIGRATION-CHAIN break, BUKAN bug). DisposeAsync → EnsureDeletedAsync.
// REUSE FlexibleParticipantAddFixture apa adanya via [IClassFixture] (jangan duplikat).
```

**Bagian A — read-path InMemory real-controller** (`FlexibleParticipantAddLiveTests.cs:52-85` `MakeController`): idempotency (RemovedAt!=null → noop), Proton-reject (400), restore-guard (non-removed → 400), reason-wajib-soft. `_userManager`/`_notif` null aman bila action tak panggil. **Catatan:** path soft/hard yang menyentuh `_userManager.GetUserAsync` butuh stub (Bagian B).

**Bagian B — write-path SQLEXPRESS** (`FlexibleParticipantAddLiveTests.cs:213-285` — `StubUserManager` + `NoopNotificationService` + `MakeLiveController`): hard-delete (row+UPA hilang via `AnyAsync==false`), soft (RemovedAt set NYATA + Score/cert UNCHANGED), Pre/Post pair via LinkedSessionId, audit row, eager-UPA D-01.

**Controller ctor (12 args, VERIFIED `:33-45`)** untuk `MakeLiveController`:
```csharp
new AssessmentAdminController(ctx, userManager: new StubUserManager(actor), auditLog, env: null!,
    cache, logger: NullLogger<...>.Instance, notificationService: new NoopNotificationService(),
    hubContext: null!, workerDataService: null!, gradingService: null!,
    protonCompletionService: null!, protonBypassService: null!);
// ControllerContext.ActionDescriptor.ActionName WAJIB di-set (NRE bila null).
```

**🔴 TEST-INFRA GAP TERBESAR 411 (service-provider stub untuk hard-delete):**
Jalur hard-delete `RemoveParticipantCoreAsync` panggil `HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>()`. Di test, `DefaultHttpContext().RequestServices` KOSONG → `GetRequiredService` throw. 410 TIDAK butuh ini (add tak cascade). Planner WAJIB salah satu:
- **(Rekomendasi)** Bangun mini-DI di `MakeLiveController` untuk write-path hard-delete: set `ctrl.ControllerContext.HttpContext.RequestServices = sp` di mana `sp` punya `RecordCascadeDeleteService` + dependency-nya. **Dependency chain VERIFIED:**
  - `RecordCascadeDeleteService(ApplicationDbContext, ILogger<RecordCascadeDeleteService>, ProtonCompletionService, AuditLogService, IWebHostEnvironment)` — `RecordCascadeDeleteService.cs:28-33`
  - `ProtonCompletionService(ApplicationDbContext, ILogger<ProtonCompletionService>, INotificationService, AuditLogService)` — `ProtonCompletionService.cs:25-29`
  - `IWebHostEnvironment` → stub dgn `WebRootPath` = temp dir (cascade hapus file cert post-commit; not-started tak ada cert → aman tapi WebRootPath harus non-null).
  ```csharp
  var services = new ServiceCollection();
  services.AddSingleton(ctx);                                   // ApplicationDbContext sama
  services.AddSingleton<AuditLogService>(new AuditLogService(ctx));
  services.AddSingleton<INotificationService>(new NoopNotificationService());
  services.AddSingleton(NullLogger<ProtonCompletionService>.Instance);
  services.AddSingleton(NullLogger<RecordCascadeDeleteService>.Instance);
  services.AddSingleton<IWebHostEnvironment>(stubEnvWithTempWebRoot);
  services.AddScoped<ProtonCompletionService>();
  services.AddScoped<RecordCascadeDeleteService>();
  ctrl.ControllerContext.HttpContext.RequestServices = services.BuildServiceProvider();
  ```
- **(Alternatif lebih ringan)** Test jalur hard-delete dgn memanggil `RecordCascadeDeleteService.ExecuteAsync` LANGSUNG (assert UPA+row hilang), dan test soft/idempotent/Proton/reason/restore via controller. Trade-off: hard-path tak lewat `RemoveParticipantCoreAsync` end-to-end (kurang integral). RESEARCH: escalate ke `WebApplicationFactory` HANYA bila mini-DI jadi rumit.

**De-tautology (WAJIB, lesson 999.12):** Setiap test JALANKAN action ASLI / assert kolom DB NYATA. Hard: `UserPackageAssignments.AnyAsync(...) == false` + `AssessmentSessions.AnyAsync(...) == false`. Soft: `RemovedAt` set NYATA + `Score`/`NomorSertifikat` UNCHANGED. JANGAN replica predikat `SessionHasDataAsync`.

**Seed helper extend** (`FlexibleParticipantAddLiveTests.cs:300` `SeedRepSessionAsync`): tambah in-progress (`StartedAt` set + ≥1 `PackageUserResponse`), completed-certified (`NomorSertifikat` + `Status="Completed"` + `ManualSertifikatUrl`), Pre/Post pair (`LinkedSessionId` cross-set).

---

## Shared Patterns

### Authentication / Authorization (RBAC + antiforgery)
**Source:** `AssessmentAdminController.cs:2353-2355` (`AddParticipantsLive`)
**Apply to:** SEMUA 3 endpoint baru (`RemoveParticipantLive`, `RestoreParticipantLive`, `DeleteAssessmentPeserta`) — PLIV-03.
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```
> IDOR (V4): `sessionId` di-load+validasi exist; partner Pre/Post di-resolve server-side via `LinkedSessionId` (TAK terima id partner dari client). Admin/HC memang berwenang lintas-peserta.

### Actor resolution (untuk audit + `RemovedBy`)
**Source:** `AssessmentAdminController.cs:2395-2397`
**Apply to:** Remove + Restore + DeletePeserta (3 baris verbatim).
```csharp
var hcUser = await _userManager.GetUserAsync(User);
var actorId = hcUser?.Id ?? "";
var actorName = string.IsNullOrWhiteSpace(hcUser?.NIP) ? (hcUser?.FullName ?? "Unknown") : $"{hcUser.NIP} - {hcUser.FullName}";
```

### Audit trail (non-repudiation PLIV-03)
**Source:** `Services/AuditLogService.cs:21-27` (signature) + `AssessmentAdminController.cs:2456` (call pattern)
**Apply to:** Remove (`"RemoveParticipantLive"`) + Restore (`"RestoreParticipantLive"`) + DeletePeserta. Cascade tulis `"CascadeDelete"` sendiri `:307` → endpoint-audit = konteks user-facing tambahan (double-log pola `DeleteAssessment:2635`).
```csharp
public async Task LogAsync(string actorUserId, string actorName, string actionType,
    string description, int? targetId = null, string? targetType = null)   // SaveChanges internal
```

### Hard-delete cascade (Don't Hand-Roll)
**Source:** `Services/RecordCascadeDeleteService.cs:175` (`ExecuteAsync`), resolve idiom `AssessmentAdminController.cs:2551`
**Apply to:** Jalur hard-delete di `RemoveParticipantCoreAsync` (+ DeletePeserta lewat core).
```csharp
public async Task<CascadeResult> ExecuteAsync(string rootType, int rootId,
    IEnumerable<int> mirrorTrainingIdsToInclude, string actorId, string actorName)
// rootType="session"; pass Enumerable.Empty<int>() untuk mirror (tak relevan single live-remove).
// Return CascadeResult(bool Success, int DeletedCount, List<int> DeletedSessionIds, List<int> DeletedTrainingIds, string? ErrorMessage)
```

### Pre/Post detection (single-source)
**Source:** `Controllers/AdminBaseController.cs:248-249` (`public static`)
**Apply to:** Remove + Restore (resolve partner). Dipanggil langsung (sudah static di base).
```csharp
public static bool IsPrePostSession(AssessmentSession session)
    => session.AssessmentType == "PreTest" || session.AssessmentType == "PostTest";
```

### Soft-remove columns (single-source 409)
**Source:** `Models/AssessmentSession.cs:99-103`
**Apply to:** Soft-remove write (set) + Restore (clear).
```csharp
public DateTime? RemovedAt { get; set; }    // :99  null=aktif; non-null=removed (SUMBER KEBENARAN)
public string?   RemovedBy { get; set; }    // :101 userId Admin/HC
public string?   RemovalReason { get; set; }// :103 max 500 (Fluent HasMaxLength), WAJIB pada soft (D-02)
```

---

## No Analog Found

Tidak ada file tanpa analog. Semua 5 unit kerja memiliki analog in-repo yang VERIFIED. **Satu** elemen tanpa analog langsung = **`RestoreParticipantLive` (clear-3-cols)** — tidak ada endpoint "un-remove" sebelumnya, namun ia adalah inverse mekanis dari soft-remove write (`AssessmentSession.cs:99-103`) → role-match cukup, planner tinggal balik `set` → `null`.

| Elemen | Role | Data Flow | Catatan |
|--------|------|-----------|---------|
| `RestoreParticipantLive` | controller | CRUD-update | Tak ada restore-analog; pakai inverse soft-set + audit pattern `AddParticipantsLive`. Bukan blocker. |
| Service-provider stub (test hard-delete) | test-infra | — | GAP terbesar 411 (lihat di atas) — 410 tak butuh; mini-DI baru. Pola DI registrasi ada di `Program.cs` (planner cermin urutan). |

---

## Metadata

**Analog search scope:** `Controllers/AssessmentAdminController.cs`, `Controllers/AdminBaseController.cs`, `Services/RecordCascadeDeleteService.cs`, `Services/AuditLogService.cs`, `Services/ProtonCompletionService.cs`, `Models/AssessmentSession.cs`, `Views/Admin/EditAssessment.cshtml`, `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs`, `HcPortal.Tests/FlexibleParticipantAddTests.cs`, spec §B2/B3/B5/F.
**Files scanned:** 10 (semua excerpt Read-verified terhadap HEAD `46b88d72`).
**Line-number corrections vs upstream:** `EnsureCanDeleteAsync` `:7465` (bukan `:7203`); `DeletePrePostGroup` `:2830` (bukan `:2566`); `DeleteAssessmentPeserta` controller-grep = 0 (stub mati dikonfirmasi).
**Pattern extraction date:** 2026-06-21
