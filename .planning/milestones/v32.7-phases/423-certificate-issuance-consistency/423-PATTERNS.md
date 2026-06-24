# Phase 423: Certificate Issuance Consistency - Pattern Map

**Mapped:** 2026-06-24
**Files analyzed:** 8 (2 NEW source, 4 MODIFY source, 2 MODIFY view) + 2 NEW test
**Analogs found:** 9 / 10 (1 view has only partial-render idiom analog)

> Catatan: research (`423-RESEARCH.md` `d99d55f4`) sudah memetakan 4 cert-issue site secara line-verified. PATTERNS.md ini melengkapinya dengan klasifikasi role/data-flow + analog konkret + petikan kode literal yang harus disalin planner. Semua line-number diverifikasi sesi ini.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| **NEW** `Helpers/CertIssuanceRules.cs` | utility (pure rules) | transform (pure, EF-free) | `Helpers/SessionEditLockRules.cs` + `Helpers/ShuffleToggleRules.cs` | exact (same role+flow) |
| **MODIFY** `Helpers/CertNumberHelper.cs` | utility (async seq + regex) | request-response (EF read MAX) | self (harden in-place) + `Helpers/ShuffleToggleRules.cs` (untuk `ResemblesAutoCertFormat` pure regex) | exact |
| **MODIFY** `Services/GradingService.cs` (`:287`, `:520`) | service | CRUD (grade→complete→issue cert) | self (site `:520` = canonical pattern utk site `:287`) | exact (in-file) |
| **MODIFY** `Controllers/AssessmentAdminController.cs` (`:3887` cert; `:995-1007`/`:1014-1028` guard) | controller | request-response | self (`:520`/`:3887` cert loop; `:1014-1028` unconditional renewal-guard = pola guard anti-dup) | exact (in-file) |
| **MODIFY** `Controllers/TrainingAdminController.cs` (`:759`, `:765`) | controller | CRUD (manual insert) | `Controllers/AssessmentAdminController.cs` cert-loop + `AddManualTraining:269` Permanent⊥ValidUntil | role-match (in-repo) |
| **MODIFY** `Views/Admin/EssayGrading.cshtml` (`:97-104`) | component (Razor) | request-response (render) | self (`:99-104` `CompletedAt` block) + `AssessmentMonitoringDetail.cshtml:243-257` badge switch | exact |
| **MODIFY** `Views/Admin/AssessmentMonitoringDetail.cshtml` (`:243-257`, `:434-446`) | component (Razor) | request-response (render) | self (status badge switch `:243-257`; essay-pending badge `:435-446`) | exact (in-file) |
| **NEW** `HcPortal.Tests/CertIssuanceRulesTests.cs` | test (pure unit) | transform | `HcPortal.Tests/SessionEditLockRulesTests.cs` | exact |
| **NEW** `HcPortal.Tests/CertIssuanceIntegrationTests.cs` | test (integration real-SQL) | CRUD | `HcPortal.Tests/RetakeThenPassCertTests.cs` | exact |

---

## Pattern Assignments

### `Helpers/CertIssuanceRules.cs` (NEW — utility, pure transform)

**Analog:** `Helpers/SessionEditLockRules.cs` (shape) + `Helpers/ShuffleToggleRules.cs` (multi-method static + pure regex/switch). Kedua kelas ini adalah produk Phase 374/422 dan merupakan template carry-forward yang user pilih (CONTEXT D-Discretion).

**Header/namespace/class idiom** (`SessionEditLockRules.cs:1-23`):
```csharp
using HcPortal.Models;

namespace HcPortal.Helpers
{
    /// <summary>
    /// v32.7 Phase 422 ... predicate tunggal supaya bisa di-unit-test tanpa instansiasi controller.
    /// Dipakai DI DUA TEMPAT: guard endpoint POST (defense-in-depth) + view friendly (UX).
    /// </summary>
    public static class SessionEditLockRules
    {
        public static bool IsSessionEditLocked(AssessmentSession s)
            => s.AssessmentType == "PostTest" && s.SamePackage;
    }
}
```

**Multi-method pure-static + switch/regex idiom** (`ShuffleToggleRules.cs:8-28`) — gunakan untuk `DeriveValidUntil` (switch) + `ResemblesAutoCertFormat` (regex) + `PendingAgeBadgeClass`:
```csharp
public static class ShuffleToggleRules
{
    public static bool IsShuffleLocked(bool anyStarted, bool anyAssignment)
        => anyStarted || anyAssignment;

    public static bool ShouldHideShuffleToggle(string? category, string? tahunKe, bool isManualEntry)
        => (category == "Assessment Proton" && tahunKe == "Tahun 3") || isManualEntry;
    // ... beberapa method pure lain di kelas yang sama ...
}
```

**Constants to reference (VERIFIED `Models/AssessmentConstants.cs:9-28`):**
- `AssessmentConstants.AssessmentType.PreTest` = `"PreTest"` (`:9`)
- `AssessmentConstants.CertificateType.Permanent` = `"Permanent"` (`:26`)
- `AssessmentConstants.CertificateType.Annual` = `"Annual"` (`:27`)
- `AssessmentConstants.CertificateType.ThreeYear` = `"3-Year"` (`:28`)

**Field types to honor (VERIFIED `Models/AssessmentSession.cs`):** `CompletedAt` = `DateTime?` (`:57`), `ValidUntil` = `DateOnly?` (`:84`), `GenerateCertificate` = `bool` default false (`:36`), `IsPassed` = `bool?` (`:56`), `RenewsSessionId` = `int?` (`:134`), `CertificateType` = `string?` (`:166`), `AssessmentType` = `string?` (`:175`).

> Methods to implement (per research Pattern 1, lihat `423-RESEARCH.md:162-203`): `ShouldIssueCertificate(AssessmentSession)`, `DeriveValidUntil(string?, DateTime?)`, `ResemblesAutoCertFormat(string?)`, `PendingAgeBadgeClass(DateTime?, DateTime)`. **D-10/A1:** `DeriveValidUntil` HANYA derive utk CertificateType kanonik (Permanent/Annual/3-Year); non-kanonik → ValidUntil dipakai apa adanya dari caller (jangan derive di helper untuk non-kanonik — jalur manual lewati derive).

---

### `Helpers/CertNumberHelper.cs` (MODIFY — utility async + add pure regex)

**Analog:** self (harden in-place). Existing primitives to KEEP & reuse (VERIFIED `CertNumberHelper.cs:12-42`):

**Build + GetNextSeqAsync (existing, jangan ganti format)** (`:20-35`):
```csharp
public static string Build(int seq, DateTime date)
    => $"KPB/{seq:D3}/{ToRomanMonth(date.Month)}/{date.Year}";

public static async Task<int> GetNextSeqAsync(ApplicationDbContext context, int year)
{
    var existing = await context.AssessmentSessions
        .Where(s => s.NomorSertifikat != null && s.NomorSertifikat.EndsWith($"/{year}"))
        .Select(s => s.NomorSertifikat!)
        .ToListAsync();
    return existing.Count == 0 ? 1 :
        existing.Select(n => { var parts = n.Split('/');
            return parts.Length > 1 && int.TryParse(parts[1], out int v) ? v : 0; }).Max() + 1;
}
```

**IsDuplicateKeyException (existing — REUSE for try/catch in all sites)** (`:37-42`):
```csharp
public static bool IsDuplicateKeyException(DbUpdateException ex)
{
    return ex.InnerException?.Message.Contains("IX_AssessmentSessions_NomorSertifikat") == true
        || ex.InnerException?.Message.Contains("2601") == true
        || ex.InnerException?.Message.Contains("2627") == true;
}
```

> Delta (D-03): tambah `ResemblesAutoCertFormat` di sini ATAU di `CertIssuanceRules` (regex `^KPB/\d{3}/[IVX]+/\d{4}$` mirror format `Build`). Research merekomendasi opsi: ekstrak loop retry+jitter ke `CertNumberHelper.TryAssignNextSeqAsync(ctx, sessionId, certNow, maxAttempts)` agar 3 site memanggil 1 fungsi (Open-Q #2 `423-RESEARCH.md:531-533`).

---

### `Services/GradingService.cs` — SITE 1 (`:287`) & SITE 2 (`:520`)

**Analog:** SITE 2 (`:520`, di `RegradeAfterEditAsync`) adalah pola yang LEBIH LENGKAP (sudah cek PreTest + guard `updated > 0`). SITE 1 (`:287`) harus diseragamkan ke pola helper yang sama.

**SITE 1 current — gate TANPA cek PreTest (GRD-01, harus lewat `ShouldIssueCertificate`)** (`GradingService.cs:287-319`):
```csharp
if (session.GenerateCertificate && isPassed)           // ❌ TANPA cek PreTest — ganti ke ShouldIssueCertificate(session)
{
    var certNow = DateTime.Now;
    int certYear = certNow.Year;
    int certAttempts = 0;
    const int maxCertAttempts = 3;                     // ↑ harden: naikkan cap (D-03)
    bool certSaved = false;
    while (!certSaved && certAttempts < maxCertAttempts)
    {
        certAttempts++;
        try
        {
            var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
            await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.NomorSertifikat == null)   // filtered WHERE — KEEP
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
            certSaved = true;
        }
        catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex))
        {
            // Retry dengan sequence baru — DELTA D-03: tambah await Task.Delay(rng.Next(...)) jitter di sini
        }
    }
    if (!certSaved)
    {
        _logger.LogError("Failed to generate certificate number for SessionId={SessionId} after {MaxAttempts} attempts ...", session.Id, maxCertAttempts);
        // DELTA D-03: + sinyal HC non-destruktif (predikat lulus+GenerateCertificate+NomorSertifikat==null SUDAH queryable; tambah audit/UpdatedAt). JANGAN rollback sesi.
    }
}
```

**SITE 2 current — pola REFERENSI (sudah cek PreTest + `updated > 0` guard)** (`GradingService.cs:520-552`):
```csharp
if (session.GenerateCertificate && session.AssessmentType != "PreTest")   // ✅ pola PreTest — seragamkan ke ShouldIssueCertificate
{
    var certNow = DateTime.Now; int certYear = certNow.Year;
    int certAttempts = 0; const int maxCertAttempts = 3; bool certSaved = false;
    while (!certSaved && certAttempts < maxCertAttempts)
    {
        certAttempts++;
        try
        {
            var nextSeq = await HcPortal.Helpers.CertNumberHelper.GetNextSeqAsync(_context, certYear);
            var nomor = HcPortal.Helpers.CertNumberHelper.Build(nextSeq, certNow);
            var updated = await _context.AssessmentSessions
                .Where(s => s.Id == session.Id && s.NomorSertifikat == null)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.NomorSertifikat, nomor));
            if (updated > 0) certSaved = true;                  // ✅ guard updated>0 — adopsi ke SITE 1
        }
        catch (DbUpdateException ex) when (certAttempts < maxCertAttempts && HcPortal.Helpers.CertNumberHelper.IsDuplicateKeyException(ex)) { }
    }
    if (!certSaved) { _logger.LogError("RegradeAfterEditAsync: failed generate cert ..."); }
}
```

**ValidUntil-on-revoke idiom (cast DateOnly?, VERIFIED `:500-501`)** — pola cast yang harus dipakai saat set ValidUntil derived (CERT-02):
```csharp
.SetProperty(r => r.NomorSertifikat, (string?)null)
.SetProperty(r => r.ValidUntil, (DateOnly?)null));   // Phase 327 — cast DateOnly? eksplisit
```

**CompletedAt sebagai "menunggu sejak" (VERIFIED `:224`)** — bukti CERT-07 timestamp sudah ter-set:
```csharp
.SetProperty(r => r.CompletedAt, DateTime.UtcNow)   // di-set saat status→PendingGrading (essay flow)
```

---

### `Controllers/AssessmentAdminController.cs` — SITE 3 (`:3887`) + anti-dup guard (`:995-1028`)

**Analog:** in-file. Cert-loop SITE 3 identik SITE 1/2; guard anti-dup harus meniru pola **unconditional** double-renewal guard (`:1014-1028`), BUKAN pola soft-block `if (!ConfirmDuplicateTitle)` (`:995-1007`).

**SITE 3 current — gate TANPA cek PreTest (GRD-01)** (`AssessmentAdminController.cs:3887-3916`): pola loop identik SITE 1 (lihat di atas). Tambahkan `+ PXF-08 certError surface` yang SUDAH ADA sebagai precedent sinyal (`:3972-3974`):
```csharp
// PXF-08: surface kegagalan cert ke HC (lulus + GenerateCertificate tapi NomorSertifikat masih kosong).
var certError = (session.GenerateCertificate && isPassed && string.IsNullOrEmpty(updatedSession?.NomorSertifikat))
    ? "Nomor sertifikat gagal dibuat, coba lagi." : null;
```
> Sinyal HC (D-03 discretion #1, research §Don't-Hand-Roll): konsisten-kan pola `certError` ini ke SEMUA site + jadikan predikat queryable (`IsPassed==true && GenerateCertificate && AssessmentType!="PreTest" && NomorSertifikat==null`).

**Soft-block judul — POLA YANG TIDAK BOLEH DIPAKAI utk guard cert (VAL-04 bypass-prone)** (`:995-1007`):
```csharp
if (!string.IsNullOrWhiteSpace(model.Title)
    && !isRenewalModePost
    && !ConfirmDuplicateTitle)            // ⚠️ guard cert-aktif TIDAK boleh di dalam blok ini (Pitfall 3)
{
    var dupMatches = await FindTitleDuplicatesAsync(_context, model.Title);
    if (dupMatches.Count > 0) { ModelState.AddModelError("Title", "..."); ViewBag.DuplicateTitleWarning = true; }
}
```

**POLA YANG BENAR utk guard anti-dup — unconditional double-renewal guard (CERT-05 template)** (`:1014-1028`):
```csharp
// Double renewal prevention (per D-10): unconditional (TIDAK dibungkus !ConfirmDuplicateTitle)
if (model.RenewsSessionId.HasValue)
{
    var srcAlreadyRenewed = await _context.AssessmentSessions.AnyAsync(a => a.RenewsSessionId == model.RenewsSessionId && a.IsPassed == true)
        || await _context.TrainingRecords.AnyAsync(t => t.RenewsSessionId == model.RenewsSessionId);
    if (srcAlreadyRenewed)
        ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
}
```
> Guard cert-aktif baru (`HasActiveCertForTitleAsync`) harus ditempatkan SETARA pola ini — unconditional, di luar cabang `ConfirmDuplicateTitle`, dengan pengecualian `RenewsSessionId == null` (D-07). Query template di `423-RESEARCH.md:239-254`.

---

### `Controllers/TrainingAdminController.cs` — SITE 4 (`:759` AddManualAssessment)

**Analog:** cert-loop dari `AssessmentAdminController` (untuk try/catch insert) + `AddManualTraining:269` (untuk Permanent⊥ValidUntil). SITE 4 paling divergen: hardcode `=true`, no PreTest, no try/catch.

**Current insert — hardcode cert + no try/catch (FLD-5.2-02, FLD-5.2-07)** (`TrainingAdminController.cs:739-765`):
```csharp
var session = new AssessmentSession
{
    UserId = wc.UserId, Title = model.Title, Category = model.Category,
    Score = model.Score, PassPercentage = model.PassPercentage, IsPassed = model.IsPassed,
    CompletedAt = model.CompletedAt, Schedule = model.CompletedAt,
    ValidUntil = model.ValidUntil,
    NomorSertifikat = wc.NomorSertifikat,            // ❌ free-text, namespace tak divalidasi (CERT-04)
    ManualSertifikatUrl = certUrl, /* ... */ CertificateType = model.CertificateType,
    AssessmentType = AssessmentConstants.AssessmentType.Manual,
    Status = AssessmentConstants.AssessmentStatus.Completed,
    IsManualEntry = true,
    GenerateCertificate = true,                      // ❌ HARDCODE (D-01 — tunduk ShouldIssueCertificate)
    CreatedAt = DateTime.UtcNow, CreatedBy = currentUserId
};
_context.AssessmentSessions.Add(session);
}
await _context.SaveChangesAsync();                    // ❌ TANPA try/catch DbUpdateException → kolisi = 500 (CERT-04)
```

**Permanent⊥ValidUntil validation to ADD (analog `AddManualTraining:269`)** (`:268-270`):
```csharp
// P06: Permanent + ValidUntil mutual exclusion (field-level error key=ValidUntil)
if (model.CertificateType == "Permanent" && model.ValidUntil != null)
    ModelState.AddModelError("ValidUntil", "Sertifikat Permanent tidak boleh punya tanggal expired.");
```

**Existing manual dup guard (NOT a cert-active guard — CERT-05 masih perlu guard terpisah)** (`AdminBaseController.cs:265-267`):
```csharp
public static Expression<Func<AssessmentSession, bool>> ManualDuplicatePredicate(string userId, string title, DateTime? completedAt)
    => s => s.UserId == userId && s.Title == title && s.CompletedAt == completedAt && s.IsManualEntry;
```
> Delta SITE 4: (1) ganti `GenerateCertificate = true` → derive via `ShouldIssueCertificate`/aturan helper; (2) tambah `ResemblesAutoCertFormat(wc.NomorSertifikat)` reject; (3) bungkus `SaveChangesAsync` (`:765`) dalam `try/catch (DbUpdateException ex) when (CertNumberHelper.IsDuplicateKeyException(ex))` → `ModelState.AddModelError` ramah (bukan 500); (4) tambah Permanent⊥ValidUntil; (5) untuk manual non-kanonik CertificateType, ValidUntil dipakai apa adanya (D-10).

---

### `Views/Admin/EssayGrading.cshtml` (`:97-104`) — CERT-07 badge

**Analog:** self (`:99-104` sudah konsumsi `Model.CompletedAt`). Tambah badge berdampingan.

**Existing CompletedAt consumption idiom** (`EssayGrading.cshtml:99-104`):
```cshtml
@{
    var tooltipText = Model.CompletedAt.HasValue
        ? $"Sudah selesai pada {Model.CompletedAt.Value:dd MMM yyyy HH:mm} WIB"
        : "Penilaian sudah diselesaikan";
}
```
> Delta CERT-07: tambah blok badge (`@if (Model.CompletedAt.HasValue)` → hitung `days = (DateTime.UtcNow - Model.CompletedAt.Value).TotalDays` → `CertIssuanceRules.PendingAgeBadgeClass(...)` → `<span class="badge @cls">Menunggu @((int)days) hari</span>`). Render hanya saat status PendingGrading (belum finalized). `EssayGradingPageViewModel.CompletedAt` sudah ter-expose (research VERIFIED `AssessmentMonitoringViewModel.cs:99`).

---

### `Views/Admin/AssessmentMonitoringDetail.cshtml` (`:243-257`, `:434-446`) — CERT-07 badge (list)

**Analog:** in-file badge idiom — TWO existing switch/badge patterns to copy.

**Status badge switch (`:243-257`)** — copy struktur `switch` + Bootstrap class:
```cshtml
var statusClass = session.UserStatus switch
{
    "Completed"          => "bg-success",
    "InProgress"         => "bg-warning text-dark",
    "Cancelled"          => "bg-secondary",
    "Menunggu Penilaian" => "bg-warning text-dark",
    _                    => "bg-light text-dark border"
};
```

**Essay-pending badge render idiom (`:435-446`)** — lokasi paling relevan utk badge umur (per-worker essay row):
```cshtml
@if (s.EssayPendingCount > 0)
{
    <span class="badge bg-warning text-dark">@s.EssayPendingCount belum dinilai</span>
}
else if (!isFinalized)
{
    <span class="badge bg-info">Siap difinalisasi</span>
}
else { <span class="badge bg-success">Selesai</span> }
```
> Delta CERT-07: di baris essay-pending (`@foreach ... x.HasManualGrading`, `:423`) tambah `<span class="badge @CertIssuanceRules.PendingAgeBadgeClass(s.CompletedAt, DateTime.UtcNow)">Menunggu @((int)(DateTime.UtcNow - s.CompletedAt.Value).TotalDays) hari</span>` saat `UserStatus == "Menunggu Penilaian"`. `MonitoringSessionViewModel.CompletedAt`/`Status` sudah tersedia (research VERIFIED `AssessmentMonitoringViewModel.cs:56,66`).

---

### `HcPortal.Tests/CertIssuanceRulesTests.cs` (NEW — pure unit)

**Analog:** `HcPortal.Tests/SessionEditLockRulesTests.cs` (VERBATIM template — `[Theory]` + `[InlineData]` truth-table, no DB, no fixture).

**Full template to copy** (`SessionEditLockRulesTests.cs:1-27`):
```csharp
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

public class SessionEditLockRulesTests
{
    [Theory]
    [InlineData("PostTest", true, true)]
    [InlineData("PreTest", true, false)]
    [InlineData(null, true, false)]
    public void IsSessionEditLocked_TruthTable(string? assessmentType, bool samePackage, bool expected)
    {
        var session = new AssessmentSession { AssessmentType = assessmentType, SamePackage = samePackage };
        Assert.Equal(expected, SessionEditLockRules.IsSessionEditLocked(session));
    }
}
```
> Truth-tables to cover (research `423-RESEARCH.md:350-378`): `ShouldIssueCertificate` (PreTest→reject; PostTest lulus+generate→true; not-passed/generate-false→false); `DeriveValidUntil` (Permanent→null, Annual→+1y, 3-Year→+3y, non-kanonik→null/passthru); `ResemblesAutoCertFormat` (regex); `PendingAgeBadgeClass` (>3→warning, >7→danger). No fixture, no `[Trait]` → selalu jalan di CI.

---

### `HcPortal.Tests/CertIssuanceIntegrationTests.cs` (NEW — integration real-SQL)

**Analog:** `HcPortal.Tests/RetakeThenPassCertTests.cs` (VERBATIM recipe — `IClassFixture<RetakeServiceFixture>`, `NoOpHubContext`, GradingService ctor recipe, seed helpers).

**`[Trait]` + fixture + NewCtx (`RetakeThenPassCertTests.cs:39-45`):**
```csharp
[Trait("Category", "Integration")]                    // SQL-less CI skip via --filter "Category!=Integration"
public class RetakeThenPassCertTests : IClassFixture<RetakeServiceFixture>
{
    private readonly RetakeServiceFixture _fixture;
    public RetakeThenPassCertTests(RetakeServiceFixture f) => _fixture = f;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);
```

**GradingService ctor recipe (VERBATIM, `:53-61`):**
```csharp
private static GradingService NewGrading(ApplicationDbContext ctx)
{
    var fakeNotif = new FakeNotificationService();
    var audit = new AuditLogService(ctx);
    var completion = new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fakeNotif, audit);
    var bypass = new ProtonBypassService(ctx, completion, fakeNotif, audit, NullLogger<ProtonBypassService>.Instance);
    var worker = new FakeWorkerDataService();
    return new GradingService(ctx, worker, NullLogger<GradingService>.Instance, completion, bypass);
}
```

**Seed helpers (`:74-136`)** — `SeedSessionAsync` (param `generateCertificate`, `assessmentType`, `passPercentage`) + `SeedPackageWithResponsesAsync` (paket + N MC soal benar + assignment `ShuffledQuestionIds`). **Pitfall 2:** `GenerateCertificate` default false → WAJIB set true untuk jalur cert.

**Core assert idiom — exactly-1-cert + format kanonik (`:166-170`):**
```csharp
await using var verify = NewCtx();
int certCount = await verify.AssessmentSessions.CountAsync(a => a.Id == sid && a.NomorSertifikat != null);
Assert.Equal(1, certCount);
var cert = await verify.AssessmentSessions.Where(a => a.Id == sid).Select(a => a.NomorSertifikat).SingleAsync();
Assert.Matches(@"^KPB/\d{3}/[IVX]+/\d{4}$", cert);
```
> Tests to add (research §Validation): CERT-01 (PreTest + generate=true + all-correct → graded TRUE tetapi `NomorSertifikat==null` di site nyata); CERT-03 (seq-fail → sesi tetap Completed + predikat sinyal lulus&NomorSertifikat==null); CERT-05 (`HasActiveCertForTitleAsync` block + renewal `RenewsSessionId!=null` lolos). **Regression guard:** jangan rusak `RetakeThenPassCertTests`, `CertDedupTests`, `CertificateStatusTests`, `CertGateAuditTests`, `CertAlertConsistencyTests`.

---

## Shared Patterns

### Cert-issue retry loop (CERT-03)
**Source:** `Services/GradingService.cs:520-552` (pola paling lengkap: cek PreTest + `updated > 0` guard).
**Apply to:** SITE 1 (`GradingService.cs:287`), SITE 3 (`AssessmentAdminController.cs:3887`), SITE 4 (`TrainingAdminController.cs:765`).
**Invariants WAJIB dipertahankan:** filtered `WHERE NomorSertifikat == null`; `catch (DbUpdateException) when (... IsDuplicateKeyException(ex))`; `CertNumberHelper.Build`/`GetNextSeqAsync`; filtered unique index `IX_AssessmentSessions_NomorSertifikat` (no migration). **Delta D-03:** naikkan cap + `await Task.Delay(Random.Shared.Next(...))` jitter; non-destruktif fallback (no rollback) + sinyal HC.

### Cert eligibility gate (CERT-01)
**Source:** NEW `CertIssuanceRules.ShouldIssueCertificate` (analog `SessionEditLockRules.IsSessionEditLocked`).
**Apply to:** SEMUA 4 site (ganti `session.GenerateCertificate && isPassed` / `&& AssessmentType != "PreTest"` / hardcode `=true`).

### Friendly DbUpdateException handling (CERT-04)
**Source:** `CertNumberHelper.IsDuplicateKeyException` (`:37-42`) + PXF-08 `certError` precedent (`AssessmentAdminController.cs:3972-3974`).
**Apply to:** SITE 4 manual insert `SaveChangesAsync` + ModelState friendly error.

### Anti double-cert guard (CERT-05) — unconditional
**Source:** double-renewal guard pattern `AssessmentAdminController.cs:1014-1028` (unconditional, di luar `ConfirmDuplicateTitle`) + normalizer `AdminBaseController.NormalizeTitleForDup` (`:271-272`).
**Apply to:** create-issue paths di `AssessmentAdminController` (manual/online HC). NEVER di dalam `if (!ConfirmDuplicateTitle)`.

### PendingGrading age badge (CERT-07)
**Source:** badge switch idiom `AssessmentMonitoringDetail.cshtml:243-257` + `:435-446`; data `session.CompletedAt` (set di `GradingService.cs:224`).
**Apply to:** `EssayGrading.cshtml` + `AssessmentMonitoringDetail.cshtml` (2 view, D-08). Pure class via `CertIssuanceRules.PendingAgeBadgeClass`.

### DateOnly/DateTime discipline
**Source:** `GradingService.cs:500-501` (cast `(DateOnly?)null`); `CertificationManagementViewModel.cs:54-66` (`today = DateOnly.FromDateTime(DateTime.UtcNow)`); `GradingService.cs:224` (`CompletedAt = DateTime.UtcNow` UTC).
**Apply to:** `DeriveValidUntil` (CompletedAt `DateTime?` → `DateOnly.FromDateTime` → `AddYears`); anti-dup query (`ValidUntil >= today`); badge age (`(DateTime.UtcNow - CompletedAt).TotalDays`).

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| (none — full coverage) | — | — | Semua file punya analog konkret (in-repo) atau in-file pattern. |

**Partial note:** `Views/Admin/EssayGrading.cshtml` tidak punya badge-umur sejenis di file lain (idiom badge harus diimpor dari `AssessmentMonitoringDetail.cshtml:243-257`/`:435-446`), tapi sudah mengonsumsi `Model.CompletedAt` (`:99-104`) sehingga data + render-context tersedia. Bukan "no analog" — hanya cross-view idiom transplant.

## Metadata

**Analog search scope:** `Helpers/`, `Services/`, `Controllers/`, `Views/Admin/`, `HcPortal.Tests/`, `Models/`
**Files read this session:** 13 (SessionEditLockRules, ShuffleToggleRules, CertNumberHelper, GradingService×2 spans, AssessmentAdminController×2 spans, TrainingAdminController×2 spans, AdminBaseController, AssessmentConstants, AssessmentSession grep, EssayGrading.cshtml, AssessmentMonitoringDetail.cshtml×2 spans, SessionEditLockRulesTests, RetakeThenPassCertTests)
**Pattern extraction date:** 2026-06-24
**Migration:** FALSE (no schema; filtered unique index existing)
