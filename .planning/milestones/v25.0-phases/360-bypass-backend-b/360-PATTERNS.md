# Phase 360: Bypass Backend (B) - Pattern Map

**Mapped:** 2026-06-10
**Files analyzed:** 11 (4 baru, 7 modifikasi)
**Analogs found:** 11 / 11 (semua punya analog konkret di codebase — fitur ini murni orkestrasi service existing)

> Catatan kunci (RESEARCH §Don't Hand-Roll): Hampir seluruh "kerja" Phase 360 = **mengorkestrasi service yang sudah ada dalam urutan + transaksi yang benar**. Hampir setiap file punya analog yang harus disalin pola-nya verbatim, bukan ditulis dari nol.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Models/ProtonModels.cs` (+`PendingProtonBypass`, +`Origin` di `ProtonTrackAssignment`) | model | CRUD | `ProtonFinalAssessment` (:207-229, same file) | exact |
| `Migrations/<ts>_AddPendingProtonBypassAndAssignmentOrigin.cs` (BARU) | migration | schema/file-I/O | `Migrations/20260610014907_AddOriginToProtonFinalAssessment.cs` | exact |
| `Data/ApplicationDbContext.cs` (+DbSet +index) | config | CRUD | DbSet block (:30-43) + filtered-unique config (:322-330, same file) | exact |
| `Services/ProtonBypassService.cs` (BARU) | service | transform + event-driven | `Services/ProtonCompletionService.cs` (struktur) + `CoachMappingController.cs:565-646` (tx) | role-match (komposit) |
| `Services/GradingService.cs` (+inject +3 hook) | service | event-driven | hook `EnsureAsync` existing (:304-309/:485-487/:531-534, same file) | exact |
| `Services/NotificationService.cs` (+template `PROTON_BYPASS_READY`) | service | pub-sub | `_templates` dict (:34-93) + `SendByTemplateAsync` (:231-265, same file) | exact |
| `Controllers/ProtonDataController.cs` (+6 endpoint) | controller | request-response / CRUD | `OverrideSave` (:1397-1464) + class header (:79-100, same file) | exact |
| `Controllers/AssessmentAdminController.cs` (:1368 exempt; :3754 essay hook) | controller | request-response | gate existing (:1364-1391) + `FinalizeEssayGrading` hook (:3750-3759, same file) | exact |
| `Controllers/CoachMappingController.cs` (:533 isi exempt; extract bootstrap helper) | controller | CRUD | placeholder (:530-541) + `AutoCreateProgressForAssignment` (:1424-1493, same file) | exact |
| `Program.cs` (+`AddScoped<ProtonBypassService>`) | config | DI | `ProtonCompletionService` reg (:56-57, same file) | exact |
| `HcPortal.Tests/ProtonBypassServiceTests.cs` (BARU) | test | integration | `HcPortal.Tests/ProtonCompletionServiceTests.cs` (:25-90) | exact |

---

## Pattern Assignments

### `Models/ProtonModels.cs` (model, CRUD) — PBYP-01, D-04

**Analog A (kolom Origin):** `ProtonTrackAssignment` class itu sendiri (`Models/ProtonModels.cs:71-84`).

Tambah satu properti nullable (baris lama = null = "Normal", tanpa backfill — D-04). Salin gaya anotasi `[MaxLength(20)]` dari `ProtonFinalAssessment.Origin` (:224-226):
```csharp
// di ProtonTrackAssignment, setelah DeactivatedAt (:83)
/// <summary>Phase 360 (D-04) — penanda exempt cross-year: null = Normal (tidak exempt), "Bypass" = stempel permanen. Hanya {null, "Bypass"}.</summary>
[MaxLength(20)]
public string? Origin { get; set; }
```

**Analog B (entity PendingProtonBypass):** `ProtonFinalAssessment` (`Models/ProtonModels.cs:207-229`) — pola entity Proton: `int Id`, FK string tanpa constraint (komentar "No FK constraint — matches pattern"), nav property opsional, status string, `DateTime CreatedAt = DateTime.UtcNow`, timestamp resolusi nullable.
```csharp
public class ProtonFinalAssessment
{
    public int Id { get; set; }
    /// <summary>No FK constraint — matches pattern.</summary>
    public string CoacheeId { get; set; } = "";
    public string CreatedById { get; set; } = "";
    public int ProtonTrackAssignmentId { get; set; }
    public ProtonTrackAssignment? ProtonTrackAssignment { get; set; }
    /// <summary>Values: "Completed"</summary>
    public string Status { get; set; } = "Completed";
    ...
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
```
Buat `PendingProtonBypass` dengan field per spec §6 (lifecycle `Status` string "Menunggu"/"Siap"/"Selesai"/"Dibatalkan", `CoacheeId`/`InitiatedById` string no-FK, `SourceProtonTrackId`/`TargetProtonTrackId` int, `LinkedAssessmentSessionId int?`, `Mode` string CL-A/B(a)/B(b)/C, `TargetUnit`/`TargetCoachId`/`Reason`, `CreatedAt`/`ResolvedAt`). Ikuti konvensi: `string ... = ""`, `DateTime ... = DateTime.UtcNow`.

---

### `Migrations/<ts>_AddPendingProtonBypassAndAssignmentOrigin.cs` (migration, schema) — PBYP-01, D-04

**Analog:** `Migrations/20260610014907_AddOriginToProtonFinalAssessment.cs` (migration #1 — toolchain sama, sukses 2026-06-10).

Pola `AddColumn` nvarchar(20) nullable + tabel plural. **BEDA dari analog:** JANGAN ada `migrationBuilder.Sql("UPDATE ...")` seed (D-04 — baris lama biar null, tanpa backfill). Gabung kolom + `CreateTable` dalam satu `Up`:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "Origin",
        table: "ProtonFinalAssessments",
        type: "nvarchar(20)",
        maxLength: 20,
        nullable: true);

    // ⚠️ analog #1 PUNYA baris seed ini — Phase 360 TIDAK (Origin assignment biar null, D-04):
    // migrationBuilder.Sql("UPDATE ProtonFinalAssessments SET Origin = 'Interview' WHERE Origin IS NULL;");
}
```
Generate via: `dotnet ef migrations add AddPendingProtonBypassAndAssignmentOrigin --context ApplicationDbContext`. **SNAPSHOT DB lokal dulu** (`sqlcmd ... BACKUP DATABASE`, SEED_WORKFLOW) sebelum `dotnet ef database update`. Verifikasi `ApplicationDbContextModelSnapshot.cs` ter-update (cegah drift — Pitfall 6). Notify IT: migration#2 = tabel `PendingProtonBypass` + kolom `ProtonTrackAssignment.Origin`.

---

### `Data/ApplicationDbContext.cs` (config, CRUD) — PBYP-01

**Analog A (DbSet):** blok DbSet Proton (`Data/ApplicationDbContext.cs:38-43`).
```csharp
public DbSet<ProtonTrackAssignment> ProtonTrackAssignments { get; set; }
public DbSet<ProtonDeliverableProgress> ProtonDeliverableProgresses { get; set; }
...
public DbSet<ProtonFinalAssessment> ProtonFinalAssessments { get; set; }
```
Tambah `public DbSet<PendingProtonBypass> PendingProtonBypasses { get; set; }` (PLURAL — konvensi proyek; nama tabel ter-derive plural).

**Analog B (index config — bila perlu index Status/CoacheeId):** filtered-unique E15 (`Data/ApplicationDbContext.cs:322-330`):
```csharp
builder.Entity<CoachCoacheeMapping>(entity =>
{
    entity.HasIndex(m => m.CoacheeId)
        .HasFilter("[IsActive] = 1")
        .IsUnique()
        .HasDatabaseName("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique");
});
```
Untuk `PendingProtonBypass` cukup index non-unique pada `CoacheeId` + `Status` (query D-10 "AnyAsync Status ∈ {Menunggu,Siap}"). **JANGAN** bikin filtered-unique pending kecuali spec minta — blok dobel pending sudah ditangani app-level (D-10), bukan DB constraint.

---

### `Services/ProtonBypassService.cs` (service, transform + event-driven) — PBYP-02..06, D-08/D-09

**Analog struktur kelas:** `Services/ProtonCompletionService.cs` (full file) — service scoped, ctor inject `ApplicationDbContext` + `ILogger`, method async return bool/Task, XML doc tebal, helper murni.
```csharp
public class ProtonCompletionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProtonCompletionService> _logger;

    public ProtonCompletionService(ApplicationDbContext context, ILogger<ProtonCompletionService> logger)
    { _context = context; _logger = logger; }

    public async Task<bool> EnsureAsync(string coacheeId, int protonTrackId, string createdById, string origin, string? notes)
    { ... }
}
```
`ProtonBypassService` inject tambahan: `ProtonCompletionService` + `INotificationService` + `AuditLogService` (semua sudah scoped DI). **Dependency satu arah** — JANGAN inject `GradingService` (circular — Open Q3).

**Analog transaksi all-or-nothing (D-09, Pitfall 4):** `CoachMappingController.cs:565-646` — pola Phase 333/334.
```csharp
await using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // semua step (EnsureAsync, bootstrap, coach, insert pending) — masing-masing SaveChangesAsync internal OK di dalam tx
    await _context.SaveChangesAsync();
    await tx.CommitAsync();
}
catch (DbUpdateException dbEx) when (
    dbEx.InnerException?.Message.Contains("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique") == true
    || dbEx.InnerException?.Message.Contains("2601") == true
    || dbEx.InnerException?.Message.Contains("2627") == true)
{
    _logger.LogWarning(dbEx, "Assign race: coachee already has active coach (unique-index violation)");
    await tx.RollbackAsync();
    return Json(new { success = false, message = "Coachee sudah memiliki coach aktif untuk unit ini. Nonaktifkan mapping lama terlebih dahulu." });
}
catch (Exception ex)
{
    _logger.LogError(ex, "CoachCoacheeMappingAssign failed ...");
    await tx.RollbackAsync();
    return Json(new { success = false, message = "Gagal menyimpan assignment. Operasi dibatalkan." }); // JANGAN expose ex.Message (D6 Phase 334)
}
```
Catatan: service mengembalikan result-object/tuple (bukan `Json`) karena controller yang `Json`-kan. Tapi pola tx/catch identik. **KECUALI** method hook `MarkPendingReadyIfAnyAsync` — TIDAK buka tx (jalan di hot-path grading, anti-pattern RESEARCH).

**Analog penanda (reuse, JANGAN duplikat — Pitfall 1):** `ProtonCompletionService.EnsureAsync` (:36) + `RemoveExamOriginAsync` (:70).
```csharp
await _protonCompletionService.EnsureAsync(coacheeId, sourceTrackId, hcId, "Bypass", reason);
// ⚠️ EnsureAsync resolve assignment WHERE IsActive (:38-39) → panggil SEBELUM deactivate source assignment.
```

**Analog atomic claim Siap→Selesai (D-12, Pitfall 7):** `GradingService.cs:234-251` (ExecuteUpdateAsync + WHERE + rowsAffected guard).
```csharp
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == session.Id && s.Status != "Completed")
    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, "Completed") ...);
if (rowsAffected == 0) { /* race → tolak ramah */ return false; }
```
Untuk konfirmasi: `.Where(p => p.Id == pendingId && p.Status == "Siap").ExecuteUpdateAsync(... "Selesai" ...)`; `rowsAffected == 0` → klik dobel / sudah diproses → tolak ramah.

---

### `Services/GradingService.cs` (service, event-driven) — PBYP-03, Pitfall 2

**Analog ctor injection:** `GradingService.cs:23-33` — tambah `ProtonBypassService` ke ctor (pola identik dengan `ProtonCompletionService` yang sudah di-inject di :21/:27/:32).
```csharp
public GradingService(
    ApplicationDbContext context,
    IWorkerDataService workerDataService,
    ILogger<GradingService> logger,
    ProtonCompletionService protonCompletionService)   // + tambah ProtonBypassService protonBypassService
{ ...; _protonCompletionService = protonCompletionService; }
```

**Analog hook (3 titik — pasang `MarkPendingReadyIfAnyAsync` PERSIS setelah `EnsureAsync` existing):**

1. `GradeAndCompleteAsync` (:304-309) — exam langsung lulus:
```csharp
if (session.Category == "Assessment Proton" && isPassed && session.ProtonTrackId.HasValue)
{
    await _protonCompletionService.EnsureAsync(session.UserId, session.ProtonTrackId.Value, session.CreatedBy ?? "", "Exam", $"Exam Proton lulus (skor {finalPercentage}%).");
    // + await _protonBypassService.MarkPendingReadyIfAnyAsync(session.Id); // flip Menunggu→Siap + notif HC
}
```
2a. `RegradeAfterEditAsync` cabang Fail→Pass (:530-534) — flip pending ke "Siap".
2b. `RegradeAfterEditAsync` cabang Pass→Fail (:483-487) — D-15: setelah `RemoveExamOriginAsync`, pending balik "Menunggu".
```csharp
if (session.Category == "Assessment Proton" && session.ProtonTrackId.HasValue)
    await _protonCompletionService.RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value);
    // + revert pending Siap→Menunggu di sini (D-15)
```
3. `AssessmentAdminController.FinalizeEssayGrading` (:3754-3759) — cabang essay early-return (Pitfall 2: `GradeAndCompleteAsync` early-return saat hasEssay → harus di-cover di sini juga). Hook idempotent → aman walau dobel terbit.

**Anti-pattern:** Hook = flip flag + notif SAJA, **tanpa** `BeginTransactionAsync` (jalan di hot-path grading; eksekusi pindah tetap di HC via `BypassConfirm`).

---

### `Services/NotificationService.cs` (service, pub-sub) — PBYP-03

**Analog template:** `_templates` dict (`Services/NotificationService.cs:34-93`).
```csharp
["ASMT_RESULTS_READY"] = new NotificationTemplate
{
    Title = "Assessment Results Ready",
    MessageTemplate = "Your results for {AssessmentTitle} are ready. Score: {Score}%",
    ActionUrlTemplate = "/CMP/AssessmentResults/{AssessmentId}"
},
```
Tambah entry (ke `InitiatedById` HC, BUKAN worker):
```csharp
["PROTON_BYPASS_READY"] = new NotificationTemplate
{
    Title = "Bypass Siap Diselesaikan",
    MessageTemplate = "...", // mis. "Exam bypass {WorkerName} lulus — siap dikonfirmasi."
    ActionUrlTemplate = "/ProtonData/Override?tab=bypass&pending={PendingId}"
},
```

**Analog kirim:** `SendByTemplateAsync` (:231-265) — substitusi placeholder `{Key}` dari dict context.
```csharp
await _notificationService.SendByTemplateAsync(pending.InitiatedById, "PROTON_BYPASS_READY",
    new Dictionary<string, object> { ["PendingId"] = pending.Id });
```

---

### `Controllers/ProtonDataController.cs` (controller, request-response) — PBYP-07

**Analog class-level auth + ctor:** header (`Controllers/ProtonDataController.cs:79-100`) — `[Authorize(Roles = "Admin,HC")]` di kelas, `_auditLog`/`_userManager`/`_context`/`_logger` sudah ter-inject. Tambah `ProtonBypassService` ke ctor (pola sama).
```csharp
[Authorize(Roles = "Admin,HC")]
public class ProtonDataController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuditLogService _auditLog;
    ...
    public ProtonDataController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditLogService auditLog, ...) { ... }
}
```

**Analog endpoint POST mutator:** `OverrideSave` (`:1397-1464`) — `[HttpPost]` + `[ValidateAntiForgeryToken]` + `[FromBody]` request DTO + validasi server-side awal + `Json(new { success = false, message })`.
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> OverrideSave([FromBody] OverrideSaveRequest req)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    if (string.IsNullOrWhiteSpace(req.OverrideReason))
        return Json(new { success = false, message = "Alasan override wajib diisi." });
    var validStatuses = new[] { "Pending", "Submitted", "Approved", "Rejected" };
    if (!validStatuses.Contains(req.NewStatus))
        return Json(new { success = false, message = "Status tidak valid." });
    ...
}
```
6 endpoint: `BypassList`/`BypassPendingList`/`BypassDetail` (GET read) + `BypassSave`/`BypassConfirm`/`BypassCancelPending` (POST mutator dengan `[ValidateAntiForgeryToken]`). DTO request class taruh di top file (pola `OverrideSaveRequest` :70-77). Semua mutator delegasi ke `ProtonBypassService` lalu `_auditLog.LogAsync(...)` (pola :649-651). Validasi server-side: alasan wajib, Δtahun≤1, mode valid (V5 — jangan percaya form).

---

### `Controllers/AssessmentAdminController.cs` (controller, request-response) — D-06a, D-05

**Analog gate cross-year (titik exempt a):** `:1364-1391`.
```csharp
foreach (var uid in UserIds)
{
    // (a) Cross-year gate (D-03/D-07): penanda Tahun N-1 (kecuali renewal).
    if (!isRenewal && !await _protonCompletionService.IsPrevYearPassedAsync(uid, trackType, prevTahunKe))
    { gateSkippedPrevYear++; continue; }
    // (b) Deliverable 100% per-unit (:1373-1389) — JANGAN diubah (D-05).
    ...
}
```
Tambah cek `isBypassAssignment` SEBELUM skip cross-year (RESEARCH Code Example):
```csharp
bool isBypassAssignment = await _context.ProtonTrackAssignments
    .AnyAsync(a => a.CoacheeId == uid && a.ProtonTrackId == protonTrackId && a.IsActive && a.Origin == "Bypass");
if (!isRenewal && !isBypassAssignment && !await _protonCompletionService.IsPrevYearPassedAsync(uid, trackType, prevTahunKe))
{ gateSkippedPrevYear++; continue; }
// Gate 100% (:1373-1389) TETAP — D-05.
```

**Analog essay hook (titik 3 Pitfall 2):** `FinalizeEssayGrading` (`:3750-3759`) — penanda `EnsureAsync` sudah ada di :3754; sisipkan `MarkPendingReadyIfAnyAsync` setelahnya (pola identik hook GradingService).

---

### `Controllers/CoachMappingController.cs` (controller, CRUD) — D-06b, PBYP-04/05

**Analog isi placeholder exempt (titik b):** `:530-541` — `isExemptFromCrossYear` saat ini hardcode `false`.
```csharp
bool isExemptFromCrossYear = false; // Phase 360 isi: req bypass-origin || renewal
if (isExemptFromCrossYear) continue;
```
Isi dengan cek `worker active assignment Origin=="Bypass"` (pola sama `isBypassAssignment` di AssessmentAdminController) — untuk re-assign normal pasca-bypass.

**Analog deactivate-lama→create-baru coach (E15, PBYP-04, Pitfall 5):** `:573-619` — deactivate assignment beda track lalu reuse-inactive/create-new. Untuk coach mapping ikuti D-16: jika `TargetCoachId` diisi → `mapping lama.IsActive=false` → SaveChanges → create baru; jika null → jangan sentuh. Catch 2601/2627 (pola :628-639).

**Analog bootstrap deliverable (PBYP-05, Pitfall 3) — EKSTRAK + PARAMETRISASI:** `AutoCreateProgressForAssignment` (`:1424-1493`).
```csharp
private async Task<List<string>> AutoCreateProgressForAssignment(int assignmentId, int protonTrackId, string coacheeId)
{
    // ⚠️ MASALAH: resolve unit dari ACTIVE MAPPING (:1428-1432), bukan parameter:
    var assignmentUnit = await _context.CoachCoacheeMappings
        .Where(m => m.CoacheeId == coacheeId && m.IsActive)
        .Select(m => m.AssignmentUnit).FirstOrDefaultAsync();
    ...
    var deliverableIds = await _context.ProtonDeliverableList
        .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == protonTrackId
                 && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == resolvedUnit.Trim())  // .Trim() 2-sisi
        .Select(d => d.Id).ToListAsync();
    // + insert ProtonDeliverableProgress Status="Pending" + DeliverableStatusHistory "Pending" (:1465-1490)
}
```
**Ekstrak varian** yang **terima `string resolvedUnit` eksplisit** (dari form bypass, BUKAN active mapping — D-05/PBYP-05). Pertahankan filter `.Trim()` 2-sisi identik (konsisten dengan gate 100% `AssessmentAdminController.cs:1382`). Pertahankan initial StatusHistory "Pending" (ActorId="system"). Pilihan ekstrak: helper internal-visible di controller / pindah ke service / static helper — Claude's Discretion. JANGAN duplikat filter unit (inkonsistensi = bug bootstrap).

---

### `Program.cs` (config, DI) — D-08

**Analog:** registrasi `ProtonCompletionService` (`Program.cs:56-57`).
```csharp
// Proton completion service — Phase 358 PCOMP-03: single-source penanda kelulusan Proton
builder.Services.AddScoped<HcPortal.Services.ProtonCompletionService>();
```
Tambah sebaris: `builder.Services.AddScoped<HcPortal.Services.ProtonBypassService>();`

---

### `HcPortal.Tests/ProtonBypassServiceTests.cs` (test, integration) — PBYP-02..06

**Analog:** `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-90` — disposable real-SQL fixture (Phase 344 TEST-05 / 358).
```csharp
public class ProtonCompletionFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public ProtonCompletionFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;...";
    }
    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.MigrateAsync();   // pipeline penuh — buktikan migration#2 apply
    }
    public async Task DisposeAsync()
    { await using var ctx = new ApplicationDbContext(_options); await ctx.Database.EnsureDeletedAsync(); }
}

[Trait("Category", "Integration")]
public class ProtonCompletionServiceTests : IClassFixture<ProtonCompletionFixture> { ... }
```
**REUSE `ProtonCompletionFixture` apa adanya** (disposable `HcPortalDB_Test_<guid>` — TIDAK sentuh DB lokal, no snapshot). Tandai `[Trait("Category","Integration")]` agar bisa di-skip `--filter "Category!=Integration"`. Cover PBYP-02..06 + extend `ProtonYearGateIntegrationTests.cs` untuk kasus exempt Origin="Bypass". Tambah pure-unit validasi (Δtahun/1-aktif/alasan) pola static `ProtonYearGate` (no DB) untuk feedback cepat.

---

## Shared Patterns

### Transaksi all-or-nothing (D-09, Pitfall 4)
**Source:** `Controllers/CoachMappingController.cs:565-646`
**Apply to:** `ProtonBypassService.BypassSave/BypassConfirm/BypassCancelPending` (semua orchestrator). `BeginTransactionAsync` + `CommitAsync` + `catch DbUpdateException` (2601/2627 ramah) + `catch Exception` generik (JANGAN expose `ex.Message` — D6 Phase 334, log saja). KECUALI hook `MarkPendingReadyIfAnyAsync` (no tx).

### Penanda kelulusan — reuse helper 358 (Pitfall 1)
**Source:** `Services/ProtonCompletionService.cs:36` (`EnsureAsync`), `:70` (`RemoveExamOriginAsync`), `:107` (`IsPrevYearPassedAsync`)
**Apply to:** CL-B(a)+konfirmasi B(b) (`EnsureAsync` Origin="Bypass"/"Exam"); hook re-grade fail (`RemoveExamOriginAsync`, selektif Exam-only kebal Bypass); gate exempt cek. **Urutan wajib:** terbitkan penanda source SEBELUM deactivate source assignment (`EnsureAsync` filter `IsActive`).

### Atomic status-transition guard (D-12, Pitfall 7)
**Source:** `Services/GradingService.cs:234-251` (`ExecuteUpdateAsync` + WHERE + `rowsAffected==0` guard)
**Apply to:** `BypassConfirm` Siap→Selesai (anti klik-dobel / 2 HC barengan). Conditional update `WHERE Status=="Siap"`; `rowsAffected==0` → tolak ramah.

### Audit log (PBYP-07, V4)
**Source:** `Services/AuditLogService.cs:21` (`LogAsync`), pemakaian `CoachMappingController.cs:649-651`
**Apply to:** Setiap operasi bypass (BypassSave/Confirm/CancelPending) — `_auditLog.LogAsync(actor.Id, actor.FullName, actionType, description, targetType: "PendingProtonBypass")`. `LogAsync` SaveChanges internal → aman di dalam tx (bagian rollback).

### CSRF + Authorize (PBYP-07, V2/V4/CSRF)
**Source:** `Controllers/ProtonDataController.cs:79` (`[Authorize(Roles="Admin,HC")]` kelas) + `:1398-1399` (`[HttpPost]`+`[ValidateAntiForgeryToken]`)
**Apply to:** Semua 6 endpoint (auth dari kelas) + `[ValidateAntiForgeryToken]` per POST mutator. Re-validasi role server-side, bukan hanya UI.

### Notif template (PBYP-03)
**Source:** `Services/NotificationService.cs:34-93` (`_templates`), `:231-265` (`SendByTemplateAsync`)
**Apply to:** Hook flip pending→Siap kirim `PROTON_BYPASS_READY` ke `InitiatedById` (HC), bukan worker.

---

## No Analog Found

Tidak ada — semua 11 file punya analog konkret di codebase. Satu-satunya logika "benar-benar baru" adalah **orkestrasi decision-tree §5** di `ProtonBypassService` (switch per closure mode CL-A/B(a)/B(b)/C), tapi tiap step di dalamnya (penanda, bootstrap, coach, tx, notif, audit) menyalin pola existing yang sudah dipetakan di atas. Pemecahan strategy vs switch = Claude's Discretion (planner).

## Open Items untuk Planner (dari RESEARCH, bukan gap pattern)

1. **Definisi konkret "cancel exam aktif S" (D-14/E5)** — belum ada operasi eksplisit di kode. Planner tetapkan: set `Status` non-completable atau hapus `UserPackageAssignment` untuk sesi Proton in-progress source-year, EXCLUDE `LinkedAssessmentSessionId` CL-B(b). Konfirmasi user bila ambigu.
2. **Default jadwal/durasi/KKM sesi bare CL-B(b)** — `PassPercentage` default 70 (`AssessmentSession.cs:30`), durasi dari form/default 60, schedule now/+1 hari.
3. **Risiko circular DI** — `GradingService → ProtonBypassService` satu arah; `ProtonBypassService` TIDAK boleh inject `GradingService`.

## Metadata

**Analog search scope:** `Services/` (ProtonCompletionService, GradingService, NotificationService, AuditLogService), `Controllers/` (ProtonDataController, AssessmentAdminController, CoachMappingController), `Models/ProtonModels.cs`, `Data/ApplicationDbContext.cs`, `Migrations/`, `Program.cs`, `HcPortal.Tests/`
**Files scanned:** 12 file analog dibaca langsung (line-verified)
**Pattern extraction date:** 2026-06-10
