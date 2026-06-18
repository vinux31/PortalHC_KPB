# Phase 393: Backend core inject - Pattern Map

**Mapped:** 2026-06-17
**Files analyzed:** 3 (2 NEW + 1 MODIFIED)
**Analogs found:** 3 / 3 (all exact/strong)

> Tujuan file ini: kasih executor **file:line + kutipan kode konkret** yang harus DITIRU (bukan abstraksi). Setiap pola di-tag "Replicate" (salin apa adanya) vs "Change" (sesuaikan untuk inject). Bahasa Indonesia untuk teks user-facing/error/audit (CLAUDE.md).

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Services/InjectAssessmentService.cs` (NEW) | service (orchestrator) | batch + transform + CRUD | `Services/GradingService.cs` (struktur service+ctor DI) + `Controllers/TrainingAdminController.cs:836-985` BulkBackfill (atomic-batch blueprint) + `Controllers/CMPController.cs:1034-1101` (sentinel anchor) + `Controllers/AssessmentAdminController.cs:3637-3871` FinalizeEssayGrading (finalize-replication) | exact (komposit) |
| `HcPortal.Tests/InjectAssessmentServiceTests.cs` (NEW) | test (xUnit integration) | request-response (assert) | `HcPortal.Tests/SubmitResurrectionTests.cs` (real GradingService wiring + disposable DB) + `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` (data-level replication + assertion style) | exact |
| `Program.cs` (MODIFIED — DI registration, ~L54) | config (DI) | n/a | `Program.cs:54` `AddScoped<GradingService>()` | exact |

---

## Pattern Assignments

### `Services/InjectAssessmentService.cs` (service orchestrator, batch+transform)

Empat analog membentuk anatomi service ini. Susun dalam urutan: **[A] struktur kelas/ctor → [B] pre-flight + dedup → [C] transaction batch → [D] per-pekerja insert (sentinel) → [E] grade reuse → [F] essay finalize replication → [G] backdate re-apply → [H] audit in-tx**.

---

#### [A] Struktur service + ctor DI — Analog: `Services/GradingService.cs:17-38`

**Replicate** (pola field readonly + ctor injection, header namespace/usings):
```csharp
// Source: GradingService.cs:1-7, 17-38
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    public class GradingService            // ← inject: InjectAssessmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GradingService> _logger;
        // ... ProtonCompletionService, ProtonBypassService, IWorkerDataService

        public GradingService(ApplicationDbContext context, ..., ILogger<GradingService> logger, ...)
        { _context = context; _logger = logger; /* ... */ }
    }
}
```
**Change untuk inject:** deps = `(ApplicationDbContext context, GradingService gradingService, ILogger<InjectAssessmentService> logger)`. Service inject **memanggil** `GradingService.GradeAndCompleteAsync` (delegasi mesin) — JANGAN duplikasi logic skor. Audit pakai `_context.AuditLogs.Add` langsung (TIDAK butuh `AuditLogService` — lihat anti-pattern). Actor identity (`actorUserId`, `actorName`) = **parameter method** dari caller (service tak punya `HttpContext`; A4 RESEARCH).

**Signature delegasi yang dipanggil** (`GradingService.cs:57`): `public async Task<bool> GradeAndCompleteAsync(AssessmentSession session)` — baca DB (assignment+responses), **non-transaksional**, set Status/Score/IsPassed/ET/cert. WAJIB insert session+assignment+responses+SaveChanges DULU sebelum panggil ini (`GradingService.cs:60-81`).

---

#### [B] Pre-flight validasi NIP up-front + dedup HashSet — Analog: `TrainingAdminController.cs:888-929`

**Replicate** (pre-validate semua NIP SEBELUM tx — D-03 reject-all):
```csharp
// Source: TrainingAdminController.cs:888-899
var nips = rows.Select(r => r.NIP).Distinct().ToList();
var users = await _context.Users
    .Where(u => u.NIP != null && nips.Contains(u.NIP))
    .ToDictionaryAsync(u => u.NIP!);

var missing = nips.Where(n => !users.ContainsKey(n)).ToList();
if (missing.Any())
{
    // BulkBackfill: TempData + redirect. INJECT D-03: kumpulkan per-row error, return InjectResult{Rejected, PerRowErrors}
    TempData["Error"] = $"NIP tidak ditemukan di AspNetUsers: {string.Join(", ", missing)}...";
    return RedirectToAction(nameof(BulkBackfill));
}
```
**Replicate** (dedup pre-load existing keys + intra-batch `HashSet` skip — D-01/D-02):
```csharp
// Source: TrainingAdminController.cs:913-929
var relevantUserIds = users.Values.Select(u => u.Id).ToList();
var existingUserIds = (await _context.AssessmentSessions
    .Where(s => s.IsManualEntry && s.Title == title && s.CompletedAt == completedAt && relevantUserIds.Contains(s.UserId))
    .Select(s => s.UserId)
    .ToListAsync()).ToHashSet();
var seenInBatch = new HashSet<string>();
var skippedNips = new List<string>();

foreach (var row in rows)
{
    var user = users[row.NIP];
    // duplikat — dilewati (DB existing ATAU intra-batch); JANGAN increment success
    if (existingUserIds.Contains(user.Id) || !seenInBatch.Add(user.Id))
    {
        skippedNips.Add(row.NIP);
        continue;   // skip+lapor, BUKAN gagalkan batch (D-01)
    }
    // ... insert
}
```
**Change untuk inject (D-02 cert-aware key):**
- Kunci dedup BUKAN `s.Title ==` mentah → pakai `AdminBaseController.NormalizeTitleForDup(title)` (lihat shared pattern). Karena normalizer C#-only (tak EF-translatable), tarik kandidat by `(UserId, Schedule.Date)` lalu banding in-memory pakai `NormalizeTitleForDup` — pola `FindTitleDuplicatesAsync` (`AdminBaseController.cs:285-292`).
- Key = `UserId + NormalizeTitleForDup(Title) + Category + Schedule.Date` (+ judul/cert + tanggal bila generate-cert ON).
- **D-09 pre-flight cert manual collision** (tambahan, SEBELUM tx): query `_context.AssessmentSessions.Where(s => manualNumbers.Contains(s.NomorSertifikat))` + cek intra-batch duplicate manual → ada → reject-all (Pitfall 4 RESEARCH).
- **D-06 tanggal** ≤ today; **D-07 EssayScore** range 0..ScoreValue; **essay tanpa skor** = invalid — semua masuk pre-flight reject-all (kumpul `PerRowErrors`, return tanpa tulis).

---

#### [C] Atomic batch transaction wrapper — Analog: `TrainingAdminController.cs:905-984`

**Replicate verbatim** (struktur `BeginTransactionAsync` → try → SaveChanges → audit in-tx → Commit; catch → Rollback — D-04):
```csharp
// Source: TrainingAdminController.cs:905-984
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    var addedSessions = new List<AssessmentSession>();
    // ... per-pekerja: insert + grade + finalize (loop) ...
    await _context.SaveChangesAsync();

    // Per-row audit (in-tx) — TIDAK pakai _auditLog.LogAsync (dia SaveChanges internal)
    foreach (var s in addedSessions)
        _context.AuditLogs.Add(new AuditLog { /* lihat [H] */ });
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    _logger.LogError(ex, "InjectBatch failed for title={Title} category={Category}", title, category);
    // INJECT: bungkus jadi InjectResult dgn pesan rollback (Bahasa Indonesia)
}
```
**Change:** loop per-pekerja di dalam tx jauh lebih kaya dari BulkBackfill (yang cuma `.Add(session)`). Inject = insert session → SaveChanges (dapat Id) → package/questions/options sentinel (1× per batch) → assignment+responses → SaveChanges → `GradeAndCompleteAsync` → (essay) finalize-block → backdate re-apply. Semua granular SaveChanges TETAP dalam tx; commit di akhir (Pitfall 6 insert order).

---

#### [D] Sentinel package anchor + UserPackageAssignment — Analog: `CMPController.cs:1068-1080`

**Replicate** (anchor paket sentinel + assignment ber-`ShuffledQuestionIds`):
```csharp
// Source: CMPController.cs:1068-1080
// Sentinel: store first package ID (no schema change — AssessmentPackageId still required by FK)
var sentinelPackage = packages.First();
assignment = new UserPackageAssignment
{
    AssessmentSessionId = id,
    AssessmentPackageId = sentinelPackage.Id,  // sentinel per discretion decision
    UserId = user.Id,
    ShuffledQuestionIds = JsonSerializer.Serialize(shuffledIds),
    ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optionShuffleDict)
};
assignment.SavedQuestionCount = shuffledIds.Count;
```
**Change untuk inject:**
- `ShuffledQuestionIds` = **urutan ID soal apa adanya** (inject historis, TAK perlu shuffle — JANGAN panggil `ShuffleEngine`).
- `ShuffledOptionIdsPerQuestion = "{}"` (Results fallback ke urutan DB).
- `IsCompleted = true` (sesi inject sudah selesai — bukan resume).
- 1 paket per room (A2 sentinel = paket itu sendiri). Insert package/questions/options 1× per batch (sebelum loop pekerja), assignment+responses per pekerja.

**Konstruksi AssessmentSession field values** (dari CONTEXT discretion + online path `AssessmentAdminController.cs:1460-1473`):
```csharp
var session = new AssessmentSession
{
    UserId = user.Id, Title = title, Category = category,
    IsManualEntry = true,                 // INJ-02 (D vs BulkBackfill yg jg true — TrainingAdminController.cs:946)
    AccessToken = "INJECT",               // (BulkBackfill pakai "BACKFILL" :943 — ganti "INJECT")
    IsTokenRequired = false,
    AssessmentType = "Standard",          // ⚠ literal "Standard" (online :1473) — BUKAN "Manual" (BulkBackfill :948 salah utk inject)
    AllowAnswerReview = true,             // syarat /CMP/Results render rincian per-soal
    GenerateCertificate = generateCert,
    ValidUntil = validUntil,              // DateOnly? — null = permanent (D-10)
    NomorSertifikat = certManual ? manualNumber : null,  // D-09 manual; null = auto via GradeAndComplete
    PassPercentage = passPercentage,
    Schedule = backdate, StartedAt = backdate, CompletedAt = backdate,  // backdate (D-06); akan dioverwrite grade → re-apply [G]
    Status = "Open",                      // grading akan flip ke Completed/PendingGrading
    CreatedAt = DateTime.UtcNow
};
```
> ⚠ **CRITICAL deviasi dari BulkBackfill:** `AssessmentType` BulkBackfill = `AssessmentConstants.AssessmentType.Manual` (`:948`) — INJECT WAJIB `"Standard"` (atau PreTest/PostTest). `"Manual"` punya branch skip di `ShouldEnforceSubmitTimer` + sibling Pre/Post exclude (RESEARCH anti-pattern). `AssessmentConstants.AssessmentType` **tidak punya** member `Standard` → pakai literal `"Standard"` (cocok online `:1473`).

---

#### [E] Reuse GradingService (delegasi mesin skor) — Analog: `GradingService.cs:57, 287-319`

**Replicate via panggilan** (JANGAN salin logic — panggil method):
```csharp
// Setelah session+package+assignment+responses ter-insert & SaveChanges:
await _gradingService.GradeAndCompleteAsync(session);
// → non-essay: Status=Completed + Score + IsPassed + ET + cert (gate isPassed, D-08)
// → ada essay: Status=PendingGrading + interim score (early-return, TANPA cert) — lalu jalankan [F]
```
**Cert auto** sudah di-handle `GradeAndCompleteAsync` (retry 3× + gate `isPassed`, `GradingService.cs:287-319`) → reuse otomatis konsisten (D-08). **Cert manual (D-09):** set `session.NomorSertifikat` SEBELUM grade; karena guard cert `WHERE NomorSertifikat == null` (`:302`), nomor manual yang sudah ter-set tidak akan ditimpa auto.

---

#### [F] Essay finalize — REPLIKASI DATA-LEVEL — Analog: `AssessmentAdminController.cs:3728-3804` (CORE) + `EssayFinalizeRecomputeTests.cs:295-311` (precedent replikasi)

`FinalizeEssayGrading` adalah **controller action** ber-HTTP-coupling (`_userManager.GetUserAsync(User)`, `_auditLog.LogAsync`, `_hubContext` SignalR, `return Json`) → **TIDAK bisa dipanggil dari service**. Replikasi CORE data-level (precedent Phase 387/376). Hanya 2 blok yang relevan untuk inject (pre-check selalu lolos karena D-05 wajib EssayScore).

**Replicate** (status-transition + recompute essay-aware — `AssessmentAdminController.cs:3728-3742`):
```csharp
var agg = AssessmentScoreAggregator.Compute(allQuestions, allResponses, session.PassPercentage);
int finalPercentage = agg.Percentage;
bool isPassed = agg.IsPassed;
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == sessionId && s.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, finalPercentage)
        .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.Completed)
        .SetProperty(r => r.IsPassed, isPassed)
        .SetProperty(r => r.CompletedAt, DateTime.UtcNow));   // ← akan di-backdate re-apply [G]
```
**Replicate** (cert block — IDENTIK dgn GradeAndComplete — `AssessmentAdminController.cs:3775-3804`):
```csharp
if (session.GenerateCertificate && isPassed)
{
    var certNow = DateTime.Now; int certYear = certNow.Year;
    int certAttempts = 0; const int maxCertAttempts = 3; bool certSaved = false;
    while (!certSaved && certAttempts < maxCertAttempts)
    {
        certAttempts++;
        try
        {
            var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
            await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
            certSaved = true;
        }
        catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex)) { }
    }
}
```
**Precedent kerangka replikasi data-level** (`EssayFinalizeRecomputeTests.cs:295-311` `MirrorFinalizeWriteAsync`) — pakai sebagai cetak biru bentuk method privat helper di service.

**Change untuk inject:**
- Jalankan blok ini HANYA bila `hasEssay` (deteksi: ada `PackageQuestion.QuestionType=="Essay"` di ShuffledQuestionIds — Pitfall 5).
- Pre-check "semua essay ter-skor" (`AssessmentAdminController.cs:3696-3697`) **boleh di-skip** (D-03 sudah jamin EssayScore ada) — tapi finalize-block WAJIB jalan agar `PendingGrading→Completed` (D-05).
- **HILANGKAN** semua HTTP-coupling: tanpa `_userManager`/`_auditLog.LogAsync`/`_hubContext`/`return Json`/race-friendly response.
- **D-12 cert auto backdate:** ganti `var certNow = DateTime.Now` → `var certNow = session.CompletedAt ?? backdate` dan `certYear = certNow.Year` (pakai tahun ujian backdate, bukan now). Berlaku DI KEDUA tempat: blok cert essay [F] DAN blok cert non-essay yang dijalankan via GradeAndComplete. **CATATAN executor:** `GradeAndCompleteAsync` hard-code `DateTime.Now` (`GradingService.cs:289`) — untuk D-12 non-essay, nomor cert auto perlu di-OVERWRITE pasca-grade dengan basis tanggal backdate, atau set NomorSertifikat manual-style. Planner putuskan mekanisme; semantik: ROMAN/tahun = tanggal ujian.

---

#### [G] Backdate re-apply pasca-grade — Analog: (BARU — derived dari Pitfall 1; tidak ada analog persis, ini deviasi sadar)

`GradeAndCompleteAsync` (`GradingService.cs:224, 263`) dan finalize-block (`AssessmentAdminController.cs:3742`) overwrite `CompletedAt = DateTime.UtcNow`. Untuk D-06 backdate historis, **re-apply** SETELAH grade+finalize, dalam tx yang sama:
```csharp
await _context.AssessmentSessions
    .Where(s => s.Id == session.Id)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.CompletedAt, backdateCompletedAt)
        .SetProperty(r => r.StartedAt, backdateStartedAt)
        .SetProperty(r => r.Schedule, backdateSchedule));
```
**Catat di plan sebagai langkah eksplisit.** Test SC harus assert backdate ter-preserve (bukan ≈ now).

---

#### [H] Audit in-tx — Analog: `TrainingAdminController.cs:958-970`

**Replicate** (`_context.AuditLogs.Add` langsung — BUKAN `LogAsync`; ikut rollback — D-11):
```csharp
// Source: TrainingAdminController.cs:958-970
foreach (var s in addedSessions)
{
    _context.AuditLogs.Add(new AuditLog
    {
        ActorUserId = actor.Id,
        ActorName = actor.FullName ?? actor.UserName ?? actor.Id,
        ActionType = auditTag.Length > 50 ? auditTag.Substring(0, 50) : auditTag,  // MaxLength 50
        Description = $"...Id={s.Id} UserId={s.UserId} NIP={...} Score={s.Score} CompletedAt={s.CompletedAt:yyyy-MM-dd}...",
        TargetId = s.Id,
        TargetType = "AssessmentSession",
        CreatedAt = DateTime.UtcNow
    });
}
await _context.SaveChangesAsync();
```
**Change untuk inject (D-11 — 3 ActionType terpisah agar count bersih):**
- Sukses → `ActionType = "ManualInject"` (count = jumlah sesi sukses; dikunci SC#4). `Description` berisi NIP + skor + sessionId (Bahasa Indonesia).
- Skip duplikat → `ActionType = "ManualInjectSkipped"` (siapa + alasan) — TIDAK menambah count ManualInject.
- Reject pre-flight → `ActionType = "ManualInjectRejected"` (jejak percobaan). Karena reject = SEBELUM tx (no tulisan sesi), audit reject ini perlu commit terpisah (bukan in-tx batch) atau via `LogAsync` di luar tx — planner putuskan; jangan campur ke tx batch sukses.
- `actor` = parameter method (bukan `_userManager.GetUserAsync(User)` — service tak punya `User`).

---

### `HcPortal.Tests/InjectAssessmentServiceTests.cs` (test, xUnit integration)

**Analog primer:** `HcPortal.Tests/SubmitResurrectionTests.cs` (real GradingService wiring + fixture) + `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` (assertion style + seed helper + read-after-commit).

#### [T1] Disposable real-SQL fixture — `SubmitResurrectionTests.cs:25-58` (≡ `EssayFinalizeRecomputeTests.cs:19-52`)

**Replicate verbatim** (ganti nama kelas → `InjectAssessmentFixture`):
```csharp
// Source: SubmitResurrectionTests.cs:25-58
public class SubmitResurrectionFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public SubmitResurrectionFixture()
    { _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30"; }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try { await using var ctx = new ApplicationDbContext(_options); await ctx.Database.MigrateAsync(); }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { }
            throw new Xunit.Sdk.XunitException($"...MIGRATION-CHAIN break, BUKAN bug. Inner: {ex}");
        }
    }
    public async Task DisposeAsync() { await using var ctx = new ApplicationDbContext(_options); await ctx.Database.EnsureDeletedAsync(); }
}

[Trait("Category", "Integration")]   // ← WAJIB: ter-exclude dari fast suite
public class SubmitResurrectionTests : IClassFixture<SubmitResurrectionFixture>
```
**Change:** nama → `InjectAssessmentServiceTests` / `InjectAssessmentFixture`. `[Trait("Category","Integration")]` WAJIB (ExecuteUpdateAsync tak jalan di InMemory — RESEARCH anti-pattern).

#### [T2] Real GradingService wiring (gold pattern) — `SubmitResurrectionTests.cs:68-76`

**Replicate verbatim** (sediakan GradingService asli untuk dependency InjectAssessmentService):
```csharp
// Source: SubmitResurrectionTests.cs:68-76
private GradingService NewGradingService(ApplicationDbContext ctx)
{
    var fakeNotif = new FakeNotificationService();
    var audit = new AuditLogService(ctx);
    var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
    var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
    var worker = new FakeWorkerDataService();
    return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
}
```
**Change:** tambah factory `NewInjectService(ctx)` = `new InjectAssessmentService(ctx, NewGradingService(ctx), NullLogger<InjectAssessmentService>.Instance)`. Fakes (`FakeNotificationService`, `FakeWorkerDataService`) + `NullLogger` SUDAH ADA di `HcPortal.Tests/` (jangan buat baru).

#### [T3] Seed helper + ctx baru per call — `SubmitResurrectionTests.cs:78-122` / `EssayFinalizeRecomputeTests.cs:62-99`

**Replicate** (`SeedUserAsync` + seed package/questions/options/responses/assignment, urutan SaveChanges granular — Pitfall 6):
```csharp
// Source: SubmitResurrectionTests.cs:91-121 (adaptasi: inject = beberapa pekerja + MC/MA/Essay mix)
private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
{
    var u = new ApplicationUser { UserName = "inj-" + Guid.NewGuid().ToString("N")[..8], Email = "inj@test.local", FullName = "Inject Test", NIP = "..." };
    ctx.Users.Add(u); await ctx.SaveChangesAsync(); return u.Id;
}
// urutan: session→Save→package→Save→questions→Save→options→Save→responses+assignment→Save
```
**Change:** seed worker WAJIB punya `NIP` (inject resolve by NIP — beda dari SubmitResurrection yg tak set NIP). Buat helper bikin `InjectRequest` (questions+options POCO + List<workerAnswers>) lalu panggil `injectSvc.InjectBatchAsync(req)`.

#### [T4] Assertion style + read-after-commit — `EssayFinalizeRecomputeTests.cs:251-285, 330-335`

**Replicate** (verifikasi via **context BARU** = read-after-commit; assert byte-identik vs `Compute`):
```csharp
// Source: EssayFinalizeRecomputeTests.cs:330-335 + RESEARCH byte-identik
await using var verify = NewCtx();
var s = (await verify.AssessmentSessions.FindAsync(sessionId))!;
var aggOnline = AssessmentScoreAggregator.Compute(questions, responses, passPct);  // sumber kebenaran sama
Assert.Equal(aggOnline.Percentage, s.Score);
Assert.Equal(aggOnline.IsPassed, s.IsPassed);
Assert.Equal(AssessmentConstants.AssessmentStatus.Completed, s.Status);   // essay → Completed (SC#3)
```
**Test yang WAJIB dibuat (map ke 5 SC, RESEARCH §Validation):**
- **SC#1** byte-identik: 3 sesi (MC murni, MA all-or-nothing, Essay ber-EssayScore=80) → assert `Score`/`IsPassed`/`SessionElemenTeknisScores`/`NomorSertifikat` (`KPB/xxx/ROMAN/year`). + MA partial-select=salah + MC salah=0.
- **SC#2** atomic: (a) 1 NIP tak ada → `Rejected`, `AssessmentSessions.Count(inject)==0`. (b) exception mid-batch (cert manual collision pekerja ke-2) → rollback: 0 sesi/assignment/response/audit committed.
- **SC#3** essay→Completed: EssayScore=80 → `Status==Completed` (BUKAN PendingGrading), `Score==80`, `IsPassed==true`, backdate `CompletedAt` ter-preserve ([G]).
- **SC#4** audit count: N pekerja sukses → `AuditLogs.Count(a => a.ActionType=="ManualInject")==N`; tiap entry `TargetId==sessionId`; 1 duplikat → `ManualInjectSkipped` TIDAK menambah count.
- **Fixture shared-DB caveat** (`EssayFinalizeRecomputeTests.cs:174-179` `QuestionOfSessionAsync`): JANGAN `FirstAsync` global — scope query ke sessionId tertentu (DB di-share antar test dalam 1 fixture).

---

### `Program.cs` (DI registration — MODIFIED)

**Analog:** `Program.cs:54`. **Replicate** (sisipkan sejajar, ~L55, setelah GradingService):
```csharp
// Source: Program.cs:54
builder.Services.AddScoped<HcPortal.Services.GradingService>();
// ↓ TAMBAH (Phase 393):
builder.Services.AddScoped<HcPortal.Services.InjectAssessmentService>();
```
**Change:** none. `AddScoped` (sama seperti GradingService — captive `ApplicationDbContext` aman karena keduanya Scoped). Di test, service di-instantiate manual (T2), bukan via DI container.

---

## Shared Patterns

### Pattern: Normalisasi judul untuk dedup (D-02)
**Source:** `Controllers/AdminBaseController.cs:271-272` (`NormalizeTitleForDup`) + `:278-293` (`FindTitleDuplicatesAsync`)
**Apply to:** dedup key di InjectAssessmentService [B]. Static method — panggil langsung `AdminBaseController.NormalizeTitleForDup(title)`.
```csharp
// Source: AdminBaseController.cs:271-272
public static string NormalizeTitleForDup(string? s)
    => System.Text.RegularExpressions.Regex.Replace((s ?? "").Trim(), @"\s+", " ").ToLowerInvariant();
```
> Normalizer C#-only (tak EF-translatable) → tarik kandidat dari DB lalu banding in-memory (`FindTitleDuplicatesAsync:285-292` GroupBy lalu `.Where(g => NormalizeTitleForDup(g.Title) == norm)`).

### Pattern: Pure score aggregator (kill-drift, byte-identik)
**Source:** `Helpers/AssessmentScoreAggregator.cs:26-60` (`Compute`)
**Apply to:** finalize-block [F] (service) + assertion expected-value [T4] (test). Signature: `Compute(IEnumerable<PackageQuestion> questions, IEnumerable<PackageUserResponse> responses, int passPercentage)` → `ScoreAggregateResult(TotalScore, MaxScore, Percentage, IsPassed)`. Essay: `EssayScore.Value` ditambah (0..ScoreValue, D-07). Formula D-04 LOCKED: `percentage = maxScore>0 ? (int)((double)totalScore/maxScore*100) : 0`.

### Pattern: Cert number generation (reuse, jangan format sendiri)
**Source:** `Helpers/CertNumberHelper.cs` — `Build(int seq, DateTime date)` → `KPB/{seq:D3}/{ROMAN}/{year}` (`:20-21`); `GetNextSeqAsync(ctx, year)` MAX+1 (`:23-35`); `IsDuplicateKeyException(ex)` (`:37-42`).
**Apply to:** cert manual collision detect [B] (`IsDuplicateKeyException` substring `IX_AssessmentSessions_NomorSertifikat`) + cert auto [E]/[F]. **D-12:** lewatkan tanggal backdate ke `Build` + `GetNextSeqAsync(backdateYear)`.

### Anti-Patterns (JANGAN — semua VERIFIED di RESEARCH)
- `AuditLogService.LogAsync` di tengah tx batch → dia `SaveChangesAsync` sendiri (`AuditLogService.cs:40-41`) = commit parsial. Pakai `_context.AuditLogs.Add` ([H]).
- `AssessmentType = "Manual"` → ada branch skip (`ShouldEnforceSubmitTimer` + sibling Pre/Post exclude). Pakai `"Standard"`/`"PreTest"`/`"PostTest"`.
- Menulis Score/IsPassed/cert dengan tangan → langgar byte-identik. Lewat `GradeAndCompleteAsync`/`Compute`.
- `GradeAndCompleteAsync` sebelum responses ter-insert → baca DB kosong (`GradingService.cs:79-81`). Insert+SaveChanges DULU.
- EF Core 8 InMemory untuk test → `ExecuteUpdateAsync` tak didukung. Disposable real-SQL ([T1]).
- Backdate hilang → grade overwrite `CompletedAt=UtcNow` (`GradingService.cs:263`, `:224`; finalize `:3742`). Re-apply [G].

---

## No Analog Found

Tidak ada file tanpa analog. Semua 3 file punya analog kuat. Satu-satunya pola **tanpa precedent persis** = backdate re-apply [G] (deviasi sadar dari Pitfall 1 RESEARCH) — bukan file, melainkan langkah dalam InjectAssessmentService; gunakan kerangka `ExecuteUpdateAsync` standar.

---

## Metadata

**Analog search scope:** `Services/`, `Controllers/`, `Helpers/`, `Models/`, `HcPortal.Tests/`, `Program.cs`
**Files scanned (read penuh/parsial):** GradingService.cs, TrainingAdminController.cs (BulkBackfill), AssessmentAdminController.cs (FinalizeEssayGrading), CMPController.cs (sentinel), CertNumberHelper.cs, AssessmentScoreAggregator.cs, AdminBaseController.cs, AssessmentConstants.cs, Program.cs, SubmitResurrectionTests.cs, EssayFinalizeRecomputeTests.cs
**Pattern extraction date:** 2026-06-17
</content>
</invoke>
