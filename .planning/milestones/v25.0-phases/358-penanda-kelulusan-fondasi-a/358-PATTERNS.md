# Phase 358: Penanda Kelulusan (fondasi A) - Pattern Map

**Mapped:** 2026-06-10
**Files analyzed:** 8 (3 create, 5 modify)
**Analogs found:** 8 / 8 (semua dalam-repo, line:diverifikasi langsung)

> Catatan otoritas: D-01 = plan susun **fresh dari spec** (`2026-06-09-proton-completion-logic-design.md`); plan draft = referensi sekunder. PATTERNS.md ini memberi excerpt analog konkret yang **sudah diverifikasi terhadap live code** (RESEARCH HIGH), jadi executor tinggal mirror — bukan dari draft yang line-number-nya stale.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Models/ProtonModels.cs` (+`Origin`) | model | — (schema) | field `Notes`/`Status` di kelas sama (L223,217) | exact (same class) |
| `Migrations/<ts>_AddOriginToProtonFinalAssessment.cs` | migration | data-seed (raw SQL UPDATE) | `Migrations/20260303073729_SetExistingRecordsActive.cs` | exact (same pattern) |
| `Services/ProtonCompletionService.cs` (NEW) | service | CRUD (DbContext, ensure/remove/query) | `Services/GradingService.cs` (DI shape) + inline-create `AssessmentAdminController.cs:3742-3765` (logic) | exact (DI) + role-match (logic) |
| `Program.cs` (+`AddScoped`) | config | DI registration | `Program.cs:54` (`AddScoped<GradingService>`) | exact |
| `Services/GradingService.cs` (inject + 3 hook) | service | event-driven (completion hook) | self — pola `_workerDataService` field DI + re-grade flip L457-512 | exact (self-pattern) |
| `Controllers/AssessmentAdminController.cs` (refactor + defensive hook) | controller | request-response | self — `SubmitInterviewResults` L3737-3766 + `FinalizeEssayGrading` L3604 | exact (self) |
| Endpoint backfill (admin POST) — **lean: `AssessmentAdminController`** | controller | batch (idempotent migration trigger) | `ResetAssessment` L3793-3796 (admin POST skeleton) + `AdminController.Maintenance` L60-63 | role-match |
| `HcPortal.Tests/ProtonCompletionServiceTests.cs` (NEW) | test | integration real-SQL | `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-66` | exact (TEST-05 fixture) |

---

## Pattern Assignments

### 1. `Models/ProtonModels.cs` — tambah `Origin` (model)

**Analog:** field di kelas yang sama, `ProtonFinalAssessment` (`Models/ProtonModels.cs:207-226`). Tambah field nullable string `[MaxLength(20)]` setelah `Notes` (L223). Field `KkjMatrixItemId` (L222) ADA di kelas tapi tak relevan 358 — biarkan null (RESEARCH koreksi: draft tak menyebutnya).

**Shape aktual (verified L207-226):**
```csharp
public class ProtonFinalAssessment
{
    public int Id { get; set; }
    public string CoacheeId { get; set; } = "";          // no FK
    public string CreatedById { get; set; } = "";        // HC user id, no FK
    public int ProtonTrackAssignmentId { get; set; }
    public ProtonTrackAssignment? ProtonTrackAssignment { get; set; }
    public string Status { get; set; } = "Completed";
    [Range(0, 5)]
    public int CompetencyLevelGranted { get; set; }      // dormant (A-3), JANGAN drop
    public int? KkjMatrixItemId { get; set; }            // ⚠️ ADA, biarkan null
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    // TAMBAH:
    // [MaxLength(20)] public string? Origin { get; set; }   // "Exam" | "Interview" | "Bypass"
}
```
> `[MaxLength]` butuh `using System.ComponentModel.DataAnnotations;` — `[Range]` sudah dipakai di file ini jadi using sudah ada. Verifikasi top-of-file.

---

### 2. `Migrations/<ts>_AddOriginToProtonFinalAssessment.cs` — data-seed migration

**Analog:** `Migrations/20260303073729_SetExistingRecordsActive.cs` (data-seed via `migrationBuilder.Sql`).

**Cara generate:** `dotnet ef migrations add AddOriginToProtonFinalAssessment` (hasilkan pair `.cs` + `.Designer.cs`, commit keduanya — RESEARCH Build artifacts). EF auto-generate `AddColumn`; executor TAMBAH manual baris `Sql(...)` data-seed di akhir `Up()`.

**Data-seed pattern (verified `SetExistingRecordsActive.cs:11-23`):**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // (AddColumn Origin auto-generated EF di sini)
    migrationBuilder.Sql("UPDATE ProtonFinalAssessments SET Origin = 'Interview' WHERE Origin IS NULL;");
}
protected override void Down(MigrationBuilder migrationBuilder)
{
    // (DropColumn Origin auto-generated)
}
```
> ⚠️ **Nama tabel PLURAL `ProtonFinalAssessments`** (DbSet name, `Data/ApplicationDbContext.cs:43`). JANGAN tulis singular `ProtonFinalAssessment` di SQL — akan gagal di runtime. Analog `SetExistingRecordsActive` pakai `Users`/`ProtonKompetensiList` (plural).

---

### 3. `Services/ProtonCompletionService.cs` (NEW, service, CRUD)

**Analog DI shape:** `Services/GradingService.cs:16-30`. **Analog logic (ensure+dedup):** blok inline `AssessmentAdminController.cs:3742-3765` (yang di-ekstrak).

**DI/constructor pattern (verified `GradingService.cs:16-30`):**
```csharp
public class GradingService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkerDataService _workerDataService;
    private readonly ILogger<GradingService> _logger;

    public GradingService(
        ApplicationDbContext context,
        IWorkerDataService workerDataService,
        ILogger<GradingService> logger)
    {
        _context = context;
        _workerDataService = workerDataService;
        _logger = logger;
    }
}
```
> `ProtonCompletionService` mirror persis: inject `ApplicationDbContext _context` (+ `ILogger<ProtonCompletionService>` opsional untuk log skip). TIDAK butuh `IWorkerDataService`.

**Core `EnsureAsync` logic — ekstrak dari inline (verified `AssessmentAdminController.cs:3742-3765`):**
```csharp
var assignment = await _context.ProtonTrackAssignments
    .FirstOrDefaultAsync(a => a.CoacheeId == session.UserId
                           && a.ProtonTrackId == session.ProtonTrackId.Value
                           && a.IsActive);                      // EnsureAsync filter IsActive
if (assignment != null)
{
    var alreadyExists = await _context.ProtonFinalAssessments
        .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id);  // dedup 1-per-assignment
    if (!alreadyExists)
    {
        _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment
        {
            CoacheeId = session.UserId,
            CreatedById = actorForFix?.Id ?? "",
            ProtonTrackAssignmentId = assignment.Id,
            Status = "Completed",
            CompetencyLevelGranted = 0,             // dormant (A-3)
            Notes = $"Interview Tahun 3 lulus. Assessor: {dto.Judges}",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });
    }
}
```
> Generalisasi jadi `EnsureAsync(string coacheeId, int protonTrackId, string createdById, string origin, string? notes)`:
> set `Origin = origin` (eksplisit), `Notes = notes`. Kembalikan `bool` (true=created, false=already-exists/no-assignment) untuk idempotency test. **`SaveChangesAsync` di dalam helper** (lihat Pitfall SaveChanges di Shared Patterns).

**Method kedua `RemoveExamOriginAsync(coacheeId, protonTrackId)`** (D-04/D-06): hapus penanda **HANYA `Origin == "Exam"`** untuk assignment yang cocok — Bypass/Interview kebal (A-M9). Tidak ada analog langsung; pola query mirror dedup di atas + `.Where(fa => fa.Origin == "Exam")`.

**Method ketiga (opsional 358) `GetPassedYearsAsync`** — dipakai backfill/wiring, **TANPA gate enforcement** (D-02; gate = Phase 359). RESEARCH anti-pattern: jangan blok/enforce apa-apa.

---

### 4. `Program.cs` — register `AddScoped` (config)

**Analog:** `Program.cs:54` (registrasi `GradingService` — kelas konkret, bukan interface).

**Pattern (verified L51-54):**
```csharp
builder.Services.AddScoped<HcPortal.Services.AuditLogService>();
// Grading service — Phase 296 D-01: concrete class + DI (sama seperti AuditLogService)
builder.Services.AddScoped<HcPortal.Services.GradingService>();
// TAMBAH (dekat sini):
// builder.Services.AddScoped<HcPortal.Services.ProtonCompletionService>();
```

---

### 5. `Services/GradingService.cs` — inject service + 3 hook (service)

**Analog:** file itu sendiri (pola field DI + titik completion). RESEARCH: GradingService **belum baca `ProtonTrackId`** → tambah pembacaan + guard D-05 di setiap hook.

**Field DI yang ditambah:** `private readonly ProtonCompletionService _protonCompletionService;` (mirror `_workerDataService` L19) — tambah param di constructor L22-30.

**Hook A — non-essay completion (`GradeAndCompleteAsync`):** RESEARCH titik = SETELAH L294 (cert), SEBELUM `return true` L299; non-essay branch dimulai L229. Cabang `hasEssay` early-return di **L226** (TIDAK lewat hook — itulah celah D-05a). Verified non-essay tail:
```csharp
// L296-299
// ---- 7. Notifikasi grup completion ----
await _workerDataService.NotifyIfGroupCompleted(session);
// + HOOK D-06: guard D-05 [Category=="Assessment Proton" && isPassed && session.ProtonTrackId.HasValue]
//   → await _protonCompletionService.EnsureAsync(session.UserId, session.ProtonTrackId.Value, <actorId>, "Exam", notes);
return true;
```

**Hook B+C — re-grade flip (`RegradeAfterEditAsync`, verified L457-512):**
```csharp
bool wasPassed = oldIsPassed ?? false;
if (wasPassed && !isPassed)          // L458 Pass→Fail: revoke sertifikat
{
    // ... ExecuteUpdateAsync NomorSertifikat=null ...
    // + HOOK D-06: guard D-05 → await _protonCompletionService.RemoveExamOriginAsync(session.UserId, session.ProtonTrackId.Value);
}
else if (!wasPassed && isPassed)     // L471 Fail→Pass: generate sertifikat
{
    // ... cert retry loop ...
    // + HOOK D-06: guard D-05 → await _protonCompletionService.EnsureAsync(..., "Exam", ...);
}
// Pass→Pass, Fail→Fail: no cascade (L513)
```
> Guard D-05 harus dievaluasi sebelum baca `session.ProtonTrackId.Value` (null-check `.HasValue`). RESEARCH: `Category` perlu dibaca dari session — pastikan tersedia di scope (load bila perlu).

---

### 6. `Controllers/AssessmentAdminController.cs` — refactor interview + defensive hook (controller)

**Constructor DI (verified L31-50):** sudah inject `ApplicationDbContext context`, `UserManager`, `AuditLogService auditLog`, `IWorkerDataService workerDataService`, `GradingService gradingService` (base `AdminBaseController` simpan `_context`/`_userManager`/`_auditLog`). **TAMBAH** param `ProtonCompletionService protonCompletionService` + field — mirror `_gradingService` L29/L49.

**Refactor D-07 — `SubmitInterviewResults` (verified L3737-3766):** ganti inline-create (excerpt di section 3 di atas) dengan satu panggilan `await _protonCompletionService.EnsureAsync(session.UserId, session.ProtonTrackId.Value, actorForFix?.Id ?? "", "Interview", $"Interview Tahun 3 lulus. Assessor: {dto.Judges}")`. **Jaga urutan SaveChanges** (Pitfall 2 di Shared Patterns): session di-set memory L3732-3735, `_context.SaveChangesAsync()` L3768 — helper share `_context` scoped yang sama, jadi panggil EnsureAsync sebelum/sesudah save konsisten (keduanya ter-flush). Perilaku lama tak berubah.

**Defensive hook D-05a — `FinalizeEssayGrading` (verified ~L3604):** IsPassed final Essay di-set via `ExecuteUpdateAsync` L3566-3572; cert generate L3604. Tambah hook `EnsureAsync(Origin="Exam")` setelah IsPassed di-set (sekitar L3604-3617), guard D-05 [Proton + isPassed]. RESEARCH: praktis idle (Proton tak ada essay di data sekarang) tapi nutup bug-diam (Pitfall 1). Variabel `isPassed` (L3562) + `session` sudah tersedia di scope.

---

### 7. Endpoint backfill — admin POST idempotent (controller, batch)

**Keputusan lokasi (Open Q-3):** **lean = `AssessmentAdminController`** karena sudah inject `_context`, `_auditLog`, `_userManager`, `_gradingService`, dan (setelah section 6) `_protonCompletionService` — `AdminController` TIDAK inject service-service ini (verified L16-33: hanya `_logger`/`_cache`/`_impersonationService`). Bila planner tetap pilih `AdminController`, harus tambah DI `ProtonCompletionService`.

**Analog skeleton admin POST (verified `AssessmentAdminController.cs:3793-3796` — `ResetAssessment`):**
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> BackfillProtonPenanda(...)
{
    // ... idempotent loop ...
    TempData["Success"] = "...";
    return RedirectToAction(...);
}
```
> Catatan: `ResetAssessment` pakai `[Authorize(Roles = "Admin, HC")]`. Pattern_mapping_context minta `Roles="Admin"` — planner putuskan; precedent repo = `"Admin, HC"`. Untuk operasi maintenance 1x, `"Admin"`-only lebih ketat (sah).

**Audit + TempData pattern (verified `SubmitInterviewResults` L3770-3784):**
```csharp
var user = await _userManager.GetUserAsync(User);
if (user != null)
{
    var actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";
    await _auditLog.LogAsync(user.Id, actorName, "BackfillProtonPenanda",
        $"Backfilled N penanda Exam ...", 0, "ProtonFinalAssessment");
}
TempData["Success"] = "Backfill selesai: N penanda dibuat, M dilewati.";
```

**Backfill logic — BEDA dari `EnsureAsync` (Pitfall 3 + 4, D-09/A-M10):**
- Resolve assignment SENDIRI **tanpa filter `IsActive`** (bisa inactive & >1): `Where(a => a.CoacheeId==UserId && a.ProtonTrackId==exam.ProtonTrackId && a.AssignedAt <= exam.CompletedAt).OrderByDescending(a => a.AssignedAt).FirstOrDefault()`.
- ⚠️ `ProtonTrackAssignment` **TIDAK punya `CompletedAt`** (hanya `AssignedAt`+`DeactivatedAt`, `ProtonModels.cs:81-83`). Pakai `exam.CompletedAt` (= `AssessmentSession.CompletedAt`), BUKAN `assignment.CompletedAt`.
- **ENFORCE deliverable 100%** (D-08, bukan opsional — draft Task 10 "opsional" = DRIFT, abaikan). Pakai helper `CoacheeEligibilityCalculator.IsEligiblePerUnit(myProgressStatuses, expectedCount)` (verified `Helpers/CoacheeEligibilityCalculator.cs:14`, static, no DbContext) — jangan loop-count manual.
- Log session yang di-skip (tak ada assignment match).
- Idempotent: cek `AnyAsync(ProtonTrackAssignmentId==id)` sebelum create (sama dedup EnsureAsync).

**`IsEligiblePerUnit` signature (verified L14):**
```csharp
public static bool IsEligiblePerUnit(IReadOnlyList<string> myProgressStatuses, int expectedCount)
// expectedCount <= 0 → false; count != expectedCount → false; else All(s == "Approved")
```

---

### 8. `HcPortal.Tests/ProtonCompletionServiceTests.cs` (NEW, test, integration real-SQL)

**Analog:** `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-66` (TEST-05 disposable real-SQL fixture, Phase 344). Unit murni TIDAK relevan — service butuh DbContext.

**Fixture pattern (verified L24-66) — mirror persis, ganti nama:**
```csharp
public class ProtonCompletionFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public ProtonCompletionFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync();   // pipeline penuh — buktikan migration Origin apply (PCOMP-04)
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { }
            throw new Xunit.Sdk.XunitException($"setup failed during MigrateAsync of {DbName}. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class ProtonCompletionServiceTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;
    public ProtonCompletionServiceTests(ProtonCompletionFixture fixture) { _fixture = fixture; }
    // [Fact] ...
}
```
> `[Trait("Category","Integration")]` (verified L68) → CI SQL-less skip via `dotnet test --filter "Category!=Integration"`. Disposable `HcPortalDB_Test_<guid>` — DB lokal `HcPortalDB_Dev` TAK tersentuh (no SEED_WORKFLOW snapshot untuk test ini).

**[Fact] yang di-assert (RESEARCH Validation §319-324):**
- `EnsureAsync` idempotent: seed assignment aktif → `Assert.True(Ensure)` lalu `Assert.False(Ensure)`; `Assert.Single(...)` (PCOMP-03).
- `EnsureAsync` no-assignment → `Assert.False` + 0 penanda.
- `RemoveExamOriginAsync` selektif: 2 penanda (Origin="Exam" & "Bypass") → Remove → `Assert.Empty(Origin=="Exam")` + `Assert.NotEmpty(Origin=="Bypass")` (PCOMP-02, A-M9).
- `GetPassedYearsAsync` (bila dibuat): TrackType cocok → Contains; beda → Empty.

---

## Shared Patterns

### Single-source penanda (anti-duplikasi)
**Source:** PCOMP-03 / `AssessmentAdminController.cs:3742-3765` (inline lama yang di-ekstrak).
**Apply to:** GradingService (2 hook), AssessmentAdminController (interview + essay defensive), backfill.
Semua pembuatan/penghapusan `ProtonFinalAssessment` HARUS lewat `ProtonCompletionService`. Anti-pattern (yang dihapus phase ini): create inline di >1 tempat.

### Guard D-05 (gate hook)
**Apply to:** SETIAP hook EnsureAsync di GradingService + FinalizeEssayGrading.
Jalan HANYA bila `Category == "Assessment Proton" && isPassed && session.ProtonTrackId.HasValue`. Jangan kepicu di exam non-Proton/Pre-Test. RESEARCH: guard `Category` SAJA TIDAK menutup jalur essay (Pitfall 1) — itu sebab defensive hook di FinalizeEssayGrading (D-05a).

### Origin selektif di re-grade (A-M9)
`RemoveExamOriginAsync` hapus HANYA `Origin == "Exam"`. Interview/Bypass kebal — penting karena Bypass (Phase 360) pakai helper yang sama.

### SaveChanges order (Pitfall 2)
**Apply to:** SubmitInterviewResults refactor.
Helper punya `SaveChangesAsync` internal; controller juga punya save untuk session (`AssessmentAdminController.cs:3768`). Keduanya share `_context` scoped yang sama → tracked session changes ikut ter-flush. Aman selama session sudah dimodifikasi sebelum salah satu save. Test: JSON tersimpan + penanda tidak dobel.

### Audit log pattern
**Source:** `AssessmentAdminController.cs:3770-3784` (`SubmitInterviewResults`).
**Apply to:** backfill endpoint. `_auditLog.LogAsync(userId, actorName, action, detail, entityId, entityType)` — `actorName` = `NIP - FullName` (fallback FullName/"Unknown"). Audit failure jangan break flow (precedent: try/catch warn-only, `FinalizeEssayGrading` L3624-3638).

### Dashboard TAK berubah
**Source:** `Controllers/CDPController.cs:373-377` (verified).
Read-path key off **EKSISTENSI** `ProtonFinalAssessment` (`!= null ? "Completed"`), BUKAN `CompetencyLevelGranted`. Emit penanda Tahun 1/2 langsung bikin "Lulus" tanpa ubah display (A-4). JANGAN sentuh read-path.

---

## RESEARCH koreksi terhadap draft (executor wajib ikut RESEARCH, bukan draft)

| Asumsi draft stale | Koreksi terverifikasi |
|--------------------|----------------------|
| inline-create di `L3740-3766` | aktual blok komentar mulai **L3737**, `if` mulai L3740 (minor) |
| `ProtonTrackAssignment.CompletedAt` untuk backfill | TIDAK ADA — pakai `exam.CompletedAt` (AssessmentSession) + `assignment.AssignedAt` (Pitfall 4) |
| D-05 "Proton=Standard-only/Essay N/A" = fakta kode | TIDAK dijamin kode — Essay universal, `hasEssay` early-return L226 lewati hook → defensive hook D-05a wajib |
| backfill cek 100% "opsional" (Task 10) | DRIFT — D-08 ENFORCE 100% (spec §4.7 + PCOMP-05) |
| `ProtonYearGate` / Task 2/6/7/8/9 | Phase 359 — JANGAN di 358 (D-02). `GetPassedYearsAsync` boleh dibuat TANPA enforce |
| `ProtonFinalAssessment` field set draft | ADA `KkjMatrixItemId` (L222) tak disebut draft — biarkan null |

---

## No Analog Found

Tidak ada. Semua 8 file punya analog dalam-repo terverifikasi.

## Metadata

**Analog search scope:** `Services/`, `Controllers/`, `Models/`, `Migrations/`, `Helpers/`, `HcPortal.Tests/`, `Program.cs`
**Files scanned (read):** GradingService.cs, ProtonModels.cs, AssessmentAdminController.cs, AdminController.cs, AdminBaseController.cs, Program.cs, CoacheeEligibilityCalculator.cs, OrgLabelMigrationIntegrationTests.cs, SetExistingRecordsActive.cs
**Pattern extraction date:** 2026-06-10
**Verification basis:** RESEARCH.md HIGH (file:line live-verified) + re-verifikasi langsung saat mapping
