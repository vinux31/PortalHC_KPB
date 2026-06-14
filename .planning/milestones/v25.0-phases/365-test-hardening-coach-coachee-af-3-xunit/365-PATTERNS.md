# Phase 365: Test-hardening Coach×Coachee — AF-3 xUnit - Pattern Map

**Mapped:** 2026-06-12
**Files analyzed:** 2 (1 CREATE test, 1 MODIFY produksi)
**Analogs found:** 2 / 2 (keduanya exact / role+flow match)

> Fase TEST-ONLY + behavior-preserving core extraction (pola Phase 363). Tujuan: kunci (parity-lock) perilaku graduate `MarkMappingCompleted` (AF-3 D-03/D-04) via xUnit real-SQL. Migration=false. Tidak ada paket/fixture baru — reuse infrastruktur Phase 358/363.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `HcPortal.Tests/MarkMappingCompletedTests.cs` (CREATE) | test (xUnit integration) | request-response (panggil static core langsung) + CRUD seed | `HcPortal.Tests/ProtonApproveRejectParityTests.cs` (test skeleton + seed helper + [Fact]) **dan** `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-61` (fixture shape) | exact (struktur identik, beda domain seed) |
| `Controllers/CoachMappingController.cs` (MODIFY) | controller (extract static core + thin wrapper) | request-response + transaksi | `Controllers/CDPController.cs` core 363 (`ApproveDeliverableCoreAsync` :981 / `RejectDeliverableCoreAsync` :1069 + wrapper call :865, :928) | role-match (pola 363, divergensi transaksi → OQ-1) |

**Konfirmasi scope file vs CONTEXT/RESEARCH:** Tepat 2 file (1 test baru + 1 produksi). `git diff Services/` HARUS kosong. SC#2 ROADMAP butuh amendemen (lihat Shared Pattern "Amendemen SC#2" + RESEARCH §🚨).

---

## Pattern Assignments

### `Controllers/CoachMappingController.cs` (controller — extract core + thin wrapper)

**Analog:** `Controllers/CDPController.cs` (core 363 `ApproveDeliverableCoreAsync` / `RejectDeliverableCoreAsync`)

Ini adalah **refactor behavior-preserving**, BUKAN fitur baru. Kode produksi target saat ini monolitik (UserManager deref → guard → transaksi → mutasi → audit, semua inline). Pecah jadi (a) `public static` core berisi guard+mutasi+cascade+SaveChanges, dan (b) wrapper tipis berisi resolve-user + transaksi + audit + redirect.

**Kode produksi target SAAT INI (yang dipecah)** — `Controllers/CoachMappingController.cs:1112-1179`:
```csharp
// :1112-1119  WRAPPER: attrs + resolve-user (tak bisa di-null-substitute → alasan D-01)
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> MarkMappingCompleted(int mappingId)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    // :1120-1121  → KE CORE (OQ-2: mapping-null di core, wrapper map → NotFound)
    var mapping = await _context.CoachCoacheeMappings.FindAsync(mappingId);
    if (mapping == null) return NotFound();
    // :1123-1133  → KE CORE: assignments aktif coachee + guard Tahun 3 [token "Tahun 3"]
    var assignments = await _context.ProtonTrackAssignments
        .Include(a => a.ProtonTrack)
        .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
        .ToListAsync();
    var tahun3Assignment = assignments
        .FirstOrDefault(a => a.ProtonTrack != null && a.ProtonTrack.TahunKe == "Tahun 3");
    if (tahun3Assignment == null) { TempData["Error"] = "Coachee belum memiliki assignment Tahun 3."; return RedirectToAction("CoachCoacheeMapping"); }
    // :1134-1139  → KE CORE: guard belum-lulus [token "belum lulus"]
    bool tahun3Complete = await IsYearCompletedAsync(tahun3Assignment.Id);
    if (!tahun3Complete) { TempData["Error"] = "Tidak bisa menandai lulus (graduated): Tahun 3 belum lulus untuk pekerja ini."; return RedirectToAction("CoachCoacheeMapping"); }
    // :1140  TETAP WRAPPER: BeginTransactionAsync (D-03)
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // :1143-1146  → KE CORE: mutasi flag/timestamp
        mapping.IsCompleted = true;
        mapping.CompletedAt = DateTime.UtcNow;
        mapping.IsActive = false;            // AF-3 D-03: bebaskan unique-index IX_CoachCoacheeMappings_CoacheeId_ActiveUnique
        mapping.EndDate = DateTime.UtcNow;   // AF-3 D-03
        // :1148-1159  → KE CORE: cascade deactivate ProtonTrackAssignment (BUKAN RemoveRange → histori utuh)
        var deactivationTime = mapping.EndDate.Value;
        var activeAssignments = await _context.ProtonTrackAssignments
            .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive)
            .ToListAsync();
        foreach (var a in activeAssignments) { a.IsActive = false; a.DeactivatedAt = deactivationTime; }
        int cascadeCount = activeAssignments.Count;
        await _context.SaveChangesAsync();   // :1161 → SaveChanges DI CORE (OQ-1 recommendation)
        // :1162  TETAP WRAPPER: CommitAsync (D-03)
        await transaction.CommitAsync();
        // :1164-1170  TETAP WRAPPER: audit post-commit (D-09 tak di-assert) + TempData + redirect
        var actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";
        await _auditLog.LogAsync(user.Id, actorName, "MarkMappingCompleted", $"... {cascadeCount} ProtonTrackAssignment juga dinonaktifkan", mappingId, "CoachCoacheeMapping");
        TempData["Success"] = "Coachee berhasil ditandai sebagai graduated.";
        return RedirectToAction("CoachCoacheeMapping");
    }
    catch (Exception ex)   // :1172-1178 TETAP WRAPPER: RollbackAsync + TempData[Error] + redirect
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "MarkMappingCompleted transaction failed for mapping {Id}", mappingId);
        TempData["Error"] = "Operasi gagal. Semua perubahan dibatalkan.";
        return RedirectToAction("CoachCoacheeMapping");
    }
}
```

**Signature core pattern — COPY DARI** `Controllers/CDPController.cs:981` (`ApproveDeliverableCoreAsync`):
```csharp
public static async Task<(bool ok, string? error, bool allApproved)> ApproveDeliverableCoreAsync(
    ApplicationDbContext context, int progressId,
    string actorId, string actorName, string actorRole, bool isSrSpv, bool isSH)
{
    var progress = await context.ProtonDeliverableProgresses...FirstOrDefaultAsync(...);
    if (progress == null) return (false, "Deliverable tidak ditemukan.", false);   // not-found DI CORE
    // ... guard race + mutasi ...
    await context.SaveChangesAsync();    // SaveChanges DI CORE (TANPA transaksi di core)
    return (true, null, allApproved);
}
```

**Signature core 365 yang harus ditulis** (adaptasi pola di atas + `cascadeCount` untuk D-07; per RESEARCH:265-269):
```csharp
// public static, context-only (no UserManager) — directly testable via real-SQL fixture.
// Pola CDPController.cs:975-979: "cores own STATE MUTATION only; endpoint owns authz/transaksi/redirect".
public static async Task<(bool ok, string? error, int cascadeCount)> MarkMappingCompletedCore(
    ApplicationDbContext context, int mappingId)
{
    var mapping = await context.CoachCoacheeMappings.FindAsync(mappingId);
    if (mapping == null) return (false, "Mapping tidak ditemukan.", 0);          // OQ-2: not-found DI CORE
    var assignments = await context.ProtonTrackAssignments.Include(a => a.ProtonTrack)
        .Where(a => a.CoacheeId == mapping.CoacheeId && a.IsActive).ToListAsync();
    var tahun3 = assignments.FirstOrDefault(a => a.ProtonTrack != null && a.ProtonTrack.TahunKe == "Tahun 3");
    if (tahun3 == null) return (false, "Coachee belum memiliki assignment Tahun 3.", 0);   // [token "Tahun 3"]
    if (!await IsYearCompletedAsync(context, tahun3.Id))
        return (false, "Tidak bisa menandai lulus (graduated): Tahun 3 belum lulus untuk pekerja ini.", 0);  // [token "belum lulus"]
    // MUTASI (verbatim dari produksi :1143-1159) + SaveChanges DI CORE (OQ-1 recommendation, konsisten pola 363)
    mapping.IsCompleted = true; mapping.CompletedAt = DateTime.UtcNow;
    mapping.IsActive = false;   mapping.EndDate = DateTime.UtcNow;
    var deactivationTime = mapping.EndDate.Value;
    foreach (var a in assignments) { a.IsActive = false; a.DeactivatedAt = deactivationTime; }
    int cascadeCount = assignments.Count;
    await context.SaveChangesAsync();
    return (true, null, cascadeCount);
}
```
> ⚠️ **Divergensi yang harus diputuskan planner (OQ-1):** Pola 363 SaveChanges di core **tanpa transaksi sama sekali**. D-02/D-03 minta transaksi tetap di wrapper. Rekomendasi RESEARCH:360 = core `SaveChangesAsync()` lalu return; wrapper `BeginTransactionAsync` SEBELUM panggil core, `CommitAsync` SETELAH `ok=true`. Test panggil core langsung tanpa transaksi → autocommit → end-state terverifikasi. Zero behavior change (urutan mutasi+save identik, hanya seam-nya dipindah).

**`IsYearCompletedAsync` → static (D-05)** — COPY transform dari produksi `Controllers/CoachMappingController.cs:1100-1110`:
```csharp
// SEBELUM (instance, deref _context) :1100
private async Task<bool> IsYearCompletedAsync(int assignmentId)
{
    var progresses = await _context.ProtonDeliverableProgresses
        .Where(p => p.ProtonTrackAssignmentId == assignmentId).ToListAsync();
    if (!progresses.Any()) return false;
    bool allApproved = progresses.All(p => p.Status == "Approved");
    bool hasFinalAssessment = await _context.ProtonFinalAssessments
        .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignmentId);
    return allApproved && hasFinalAssessment;
}
// SESUDAH (static, ctx param) — hanya _context → ctx, nol perubahan logika
private static async Task<bool> IsYearCompletedAsync(ApplicationDbContext ctx, int assignmentId) { /* ctx.ProtonDeliverableProgresses ... ctx.ProtonFinalAssessments */ }
```
> **OQ-3 RESOLVED:** Grep `IsYearCompletedAsync(` di repo = **hanya 1 pemanggil produksi** (`:1134`, satu-satunya, akan ikut di-refactor). Maka **ganti langsung ke static** — TIDAK perlu instance-delegasi `=> IsYearCompletedAsync(_context, id)`.

**Wrapper memanggil core pattern — COPY DARI** `Controllers/CDPController.cs:865-873` (Approve) + `:928-936` (Reject):
```csharp
// Phase 363-01: state mutation lives in the shared core (D-01 anti-drift seam)
var (ok, error, allApproved) = await ApproveDeliverableCoreAsync(_context, progressId, user.Id, ...);
if (!ok)
{
    if (error == "Deliverable tidak ditemukan.") return NotFound();   // OQ-2: core error → NotFound
    TempData["Error"] = error;
    return RedirectToAction("CoachingProton");
}
```
Adaptasi 365: wrapper map `error == "Mapping tidak ditemukan."` → `NotFound()`; error guard lain → `TempData["Error"] = error; RedirectToAction("CoachCoacheeMapping")`. Attrs `[HttpPost]`+`[ValidateAntiForgeryToken]`+`[Authorize(Roles="Admin, HC")]` (:1113-1115) **PRESERVE verbatim** di wrapper (Security: zero new surface).

---

### `HcPortal.Tests/MarkMappingCompletedTests.cs` (test — xUnit integration, real SQL)

**Analog 1 (test skeleton + seed helper + [Fact]):** `HcPortal.Tests/ProtonApproveRejectParityTests.cs`
**Analog 2 (fixture shape):** `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-61`

**Imports + class skeleton pattern — COPY DARI** `ProtonApproveRejectParityTests.cs:1-25`:
```csharp
using System;
using Microsoft.EntityFrameworkCore;
using HcPortal.Controllers;   // CoachMappingController.MarkMappingCompletedCore
using HcPortal.Data;          // ApplicationDbContext
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]   // WAJIB — real SQL; CI SQL-less skip via --filter "Category!=Integration"
public class MarkMappingCompletedTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;
    public MarkMappingCompletedTests(ProtonCompletionFixture fixture) { _fixture = fixture; }
    // ...
}
```

**Fixture shape (REUSE, jangan bikin baru) — referensi** `ProtonCompletionServiceTests.cs:25-61`:
```csharp
public class ProtonCompletionFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";   // disposable, HcPortalDB_Dev TAK tersentuh
    public DbContextOptions<ApplicationDbContext> Options => _options;       // dipakai test: new ApplicationDbContext(_fixture.Options)
    public ProtonCompletionFixture() { _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;..."; }
    public async Task InitializeAsync() { ...; await ctx.Database.MigrateAsync(); }   // pipeline penuh → IX_..._ActiveUnique + 6 ProtonTrack seed migration ADA
    public async Task DisposeAsync() { ...; await ctx.Database.EnsureDeletedAsync(); }  // drop di sukses & gagal-mid-migration
}
```
> `MarkMappingCompletedTests` hanya `IClassFixture<ProtonCompletionFixture>` — **TIDAK** definisikan ulang fixture (sudah ada di `ProtonCompletionServiceTests.cs`). **TIDAK** butuh `FakeNotificationService`/`AuditLogService` (notif/audit di wrapper D-03/D-09, beda dari `ProtonCompletionServiceTests.NewSvc`).

**Pola panggil core langsung + verifikasi 2nd context `AsNoTracking` — COPY DARI** `ProtonApproveRejectParityTests.cs:82-99`:
```csharp
var (ok, error) = await CDPController.RejectDeliverableCoreAsync(ctx, ids[0], "srspv-1", ...);
Assert.True(ok);
Assert.Null(error);
await using var verify = new ApplicationDbContext(_fixture.Options);                 // 2nd context — buktikan persisted
var p = await verify.ProtonDeliverableProgresses.AsNoTracking().FirstAsync(x => x.Id == ids[0]);
Assert.Equal("Rejected", p.Status);
```
Adaptasi 365 happy [Fact] #1 (D-07 full end-state):
```csharp
var (ok, error, cascadeCount) = await CoachMappingController.MarkMappingCompletedCore(ctx, mappingId);
Assert.True(ok); Assert.Null(error); Assert.Equal(activeBefore, cascadeCount);
await using var verify = new ApplicationDbContext(_fixture.Options);
var m = await verify.CoachCoacheeMappings.AsNoTracking().FirstAsync(x => x.Id == mappingId);
Assert.True(m.IsCompleted); Assert.False(m.IsActive); Assert.NotNull(m.CompletedAt); Assert.NotNull(m.EndDate);
// tiap cascaded assignment IsActive==false + DeactivatedAt!=null; count progress TAK berubah
```

**Pola guard [Fact] (D-08 token kunci, BUKAN verbatim) — COPY DARI** `ProtonApproveRejectParityTests.cs:162-164`:
```csharp
Assert.False(ok);
Assert.False(allApproved);
Assert.Contains("diproses oleh approver lain", error!);   // ← token kunci, bukan kalimat penuh
```
Adaptasi 365: `Assert.Contains("Tahun 3", error!)` (#3), `Assert.Contains("belum lulus", error!)` (#4), `Assert.Contains("tidak ditemukan", error!)` (#5) + assert mapping/assignment TAK termutasi (`IsActive==true`, `IsCompleted==false`, `cascadeCount==0`).

**Isolasi per-[Fact] (DB shared antar-fact) — COPY DARI** `ProtonApproveRejectParityTests.cs:71`:
```csharp
var coachee = $"par-{Guid.NewGuid():N}";   // coacheeId unik per fact → tak tabrakan IX_..._ActiveUnique antar-test
```

---

## ⭐ Seed Helper: Tahun-3-COMPLETE (gap utama vs analog)

Analog `SeedProgressChainAsync` (`ProtonApproveRejectParityTests.cs:31-65`) hanya seed **Tahun 1** + progress per-status, **TANPA** `ProtonFinalAssessment` dan **TANPA** `CoachCoacheeMapping`. Untuk fase ini butuh seed state **Tahun 3 lulus penuh** supaya `IsYearCompletedAsync` (= `allApproved && hasFinalAssessment`) return true dan ada mapping aktif yang bisa di-graduate.

### Apa yang ANALOG SEDIAKAN (copy pola ini verbatim):
| Bagian | Sumber analog | Detail |
|--------|---------------|--------|
| Reuse track master (jangan insert) | `:35` | `await ctx.ProtonTracks.FirstAsync(t => t.TrackType=="Operator" && t.TahunKe=="Tahun 1")` |
| Assignment aktif | `:37-38` | `new ProtonTrackAssignment { CoacheeId, AssignedById="hc", ProtonTrackId=track.Id, IsActive=true }` |
| Hierarki komp→sub→deliverable→progress | `:39-63` | `ProtonKompetensi` (Bagian/Unit/NamaKompetensi/Urutan/ProtonTrackId) → `ProtonSubKompetensi` → loop `ProtonDeliverable` + `ProtonDeliverableProgress` |
| Save bertahap (FK butuh Id induk) | `:41,44,51,61` | `await ctx.SaveChangesAsync()` setelah tiap level |

### Apa yang HARUS DITAMBAHKAN (tidak ada di analog):
| Tambahan | Alasan | Bentuk |
|----------|--------|--------|
| **TrackType/Tahun 3** | `FirstAsync` Tahun 1 → ganti `"Tahun 3"` | `FirstAsync(t => t.TrackType=="Operator" && t.TahunKe=="Tahun 3")` (Id=6 seed migration MERGE; **jangan insert** → UNIQUE `AK_ProtonTracks_TrackType_TahunKe`) |
| **Semua progress `Status="Approved"`** | syarat `allApproved` di `IsYearCompletedAsync` | loop set `Status="Approved"` (analog pakai status campuran) |
| **`ProtonFinalAssessment`** (BARU) | syarat kedua `hasFinalAssessment`; 1:1 unik per assignment (`ApplicationDbContext.cs:417` `HasIndex(...).IsUnique()` → Pitfall 4: jangan 2 baris) | `new ProtonFinalAssessment { CoacheeId, CreatedById="hc", ProtonTrackAssignmentId=asg.Id, Status="Completed", CompletedAt=DateTime.UtcNow }` |
| **`CoachCoacheeMapping` aktif** (BARU) | objek yang di-graduate; field wajib `Models/CoachCoacheeMapping.cs` | `new CoachCoacheeMapping { CoachId, CoacheeId, IsActive=true, StartDate=DateTime.UtcNow, IsCompleted=false }` (**`StartDate` WAJIB di-set** — `DateTime` non-null :29; `EndDate`/`CompletedAt` nullable) |
| **Return tuple** | test butuh id + count baseline | `return (mapping.Id, asg.Id, progressCount)` |

Resep lengkap `SeedGraduateReadyAsync` siap-pakai ada di **RESEARCH.md:286-331** — planner copy dari sana (field wajib/nullable sudah `[VERIFIED]` per model di RESEARCH:332-335).

### Varian seed untuk guard [Fact] (turunan helper di atas):
- **#3 no-Tahun3:** seed assignment `TahunKe != "Tahun 3"` (atau tanpa assignment Tahun 3) → `tahun3==null`.
- **#4a belum-lulus:** salah satu progress `Status="Submitted"` (bukan Approved) → `allApproved==false`.
- **#4b belum-lulus:** semua Approved **TAPI skip** `ProtonFinalAssessment` → `hasFinalAssessment==false`.
- **#5 not-found:** panggil core dengan `mappingId=-1` (tak perlu seed).

---

## Shared Patterns

### Static Core Extraction (pola Phase 363) — anti-drift seam
**Source:** `Controllers/CDPController.cs:975-979` (komentar kontrak) + `:981`, `:1069` (core), `:865-873`, `:928-936` (wrapper call)
**Apply to:** `Controllers/CoachMappingController.cs` (`MarkMappingCompletedCore` + static `IsYearCompletedAsync`)
```
"public static: cores own STATE MUTATION only; endpoint owns authz/section-check/input-guard/
 response-shaping. Static + context-only deps → directly testable via real-SQL fixture
 (no InternalsVisibleTo → public static, konvensi CMPController:3969)."
```
Return tuple `(bool ok, string? error, ...)`; not-found ditangani DI CORE (return false+error), wrapper map ke `NotFound()`.

### Real-SQL Integration Fixture (REUSE)
**Source:** `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-61` (`ProtonCompletionFixture`)
**Apply to:** `MarkMappingCompletedTests` (D-04 — hanya real SQL enforce `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`)
- `[Trait("Category","Integration")]` di class test (CI SQL-less skip).
- `IClassFixture<ProtonCompletionFixture>` — jangan redefinisi fixture.
- DB disposable `HcPortalDB_Test_<guid>`; `HcPortalDB_Dev` TAK tersentuh → **tidak perlu SEED_WORKFLOW snapshot/restore**.
- `MigrateAsync()` penuh → 6 ProtonTrack (Operator/Panelman × Tahun 1/2/3) PASTI ada (seed migration `20260223060707_CreateProtonTrackTable` Step 5, BUKAN `SeedData.cs` runtime).

### Re-assignability via filtered unique index ([Fact] #2 — bukti D-03)
**Source:** `Data/ApplicationDbContext.cs:325-333`
```csharp
entity.HasIndex(m => m.CoacheeId)
    .HasFilter("[IsActive] = 1")          // hanya baris IsActive=1 unik per CoacheeId
    .IsUnique()
    .HasDatabaseName("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique");
```
**Apply to:** [Fact] #2 — setelah core `ok=true` (graduate set `IsActive=false`), insert `CoachCoacheeMapping` aktif baru same coachee → **tidak throw**. Opsional perkuat (Pitfall 3): pre-graduate insert duplikat aktif HARUS throw `DbUpdateException` (buktikan index bergigi).

### Amendemen SC#2 ROADMAP (WAJIB sebelum verifier)
**Source:** RESEARCH.md:448-456 + CONTEXT.md `<specifics>`
**Apply to:** plan + ROADMAP SC#2 Phase 365
- SC#2 lama: *"zero file produksi berubah (git diff Controllers/ Services/ kosong)"* — D-01 melanggar (extract core PASTI ubah `Controllers/CoachMappingController.cs`).
- Update SC#2 → *"refactor behavior-preserving + parity-locked — `Controllers/CoachMappingController.cs` disentuh (extract core + wrapper tipis), zero behavior change, dibuktikan via 6 [Fact] core hijau + `dotnet build` 0 error + `dotnet test` full suite 0 regresi. Migration=false."*
- `git diff Services/` HARUS tetap kosong. Verifier TIDAK boleh gate "git diff Controllers/ kosong".

---

## Anti-Patterns to Avoid (dari RESEARCH §Anti-Patterns/Pitfalls)
- **InMemory provider** — tak enforce filtered unique index → [Fact] #2 palsu-hijau. D-04 melarang.
- **Insert ProtonTrack baru di seed** — UNIQUE `AK_ProtonTracks_TrackType_TahunKe` → throw. Selalu `FirstAsync(TrackType+TahunKe)`.
- **2 `ProtonFinalAssessment` per assignment** — unique 1:1 (`ApplicationDbContext.cs:417`) → throw (Pitfall 4).
- **`BeginTransactionAsync` di dalam core** — D-02 melarang (transaksi di wrapper).
- **`RemoveRange` progress saat graduate** — AF-3 D-04 melarang; histori WAJIB utuh ([Fact] #6 lock).
- **Assert microcopy verbatim** — D-08 cek token kunci, bukan kalimat penuh (hindari brittle).

---

## No Analog Found

Tidak ada. Kedua file punya analog kuat di repo (test 358/363, core 363). Tidak perlu fallback ke RESEARCH-only pattern.

---

## Metadata

**Analog search scope:** `HcPortal.Tests/` (test xUnit), `Controllers/` (core 363 + target produksi), `Models/`, `Data/`
**Files scanned (Read penuh / partial):** `ProtonApproveRejectParityTests.cs`, `ProtonCompletionServiceTests.cs:1-70`, `CoachMappingController.cs:1095-1184`, `CDPController.cs:855-944,975-1069`, `Models/CoachCoacheeMapping.cs`
**Grep:** `IsYearCompletedAsync(` (konfirmasi 1 pemanggil produksi → OQ-3 resolved)
**Pattern extraction date:** 2026-06-12
