# Phase 365: Test-hardening Coach×Coachee — AF-3 xUnit - Research

**Researched:** 2026-06-12
**Domain:** xUnit integration testing (real SQL Server), behavior-preserving core extraction (pola Phase 363), EF Core filtered unique index
**Confidence:** HIGH

## Summary

Fase ini **mengunci** (parity-lock) perilaku graduate `MarkMappingCompleted` (`Controllers/CoachMappingController.cs:1116`) dengan xUnit baru, mengikuti pola ekstraksi core Phase 363 (`ApproveDeliverableCoreAsync`). Tidak ada perubahan perilaku produksi — hanya refactor seam testable + test baru. Migration tetap `false`.

Seluruh unknown teknis yang ditanyakan planner sudah **terkonfirmasi langsung dari kode** (HIGH confidence, bukan asumsi). Tiga temuan kunci: (1) `ProtonTracks` di-seed oleh **migration** `20260223060707_CreateProtonTrackTable` Step 5 (SQL MERGE 6 baris termasuk `Operator`/`Tahun 3` dan `Panelman`/`Tahun 3`) — bukan oleh `SeedData.cs` — jadi fixture `MigrateAsync()` PASTI punya track Tahun 3, reuse via `FirstAsync`, jangan insert. (2) Unique index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` adalah filtered unique pada `(CoacheeId) WHERE [IsActive] = 1` — set `IsActive=false` membebaskan coachee. (3) Pola 363 (approve/reject) memanggil `SaveChangesAsync` **di dalam core tanpa transaksi sama sekali**, sementara D-02/D-03 fase 365 minta transaksi **tetap di wrapper** — ini divergensi yang harus diselesaikan planner (lihat Open Question OQ-1).

**Primary recommendation:** Ekstrak `static MarkMappingCompletedCore(ApplicationDbContext ctx, int mappingId)` yang melakukan validasi guard + mutasi + cascade + `SaveChangesAsync`, return `(bool ok, string? error, int cascadeCount)`. Ekstrak juga `static IsYearCompletedAsync(ApplicationDbContext ctx, int assignmentId)`. Wrapper tetap: resolve-user → `BeginTransactionAsync` → panggil core → `CommitAsync`/`RollbackAsync` + audit + TempData. Test core langsung via `ProtonCompletionFixture` (real SQL) dengan 6 [Fact], seed Tahun-3-complete chain.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Extract **static core** `MarkMappingCompletedCore` mengikuti pola Phase 363 (`ApproveDeliverableCoreAsync`/`RejectDeliverableCoreAsync`). Endpoint controller jadi wrapper tipis. Alasan: endpoint deref `_userManager.GetUserAsync(User)` di baris PERTAMA (`:1118`) sebelum lookup mapping → trik null-substitute UserManager GAGAL; repo tidak punya preseden UserManager-double. Core extraction membuat logika testable tanpa UserManager.
- **D-02:** **Core murni** — berisi: validasi guard (kembalikan tuple `(bool ok, string? error)`) + mutasi flag/timestamp + cascade deactivate `ProtonTrackAssignment`. **TANPA** `BeginTransactionAsync` di dalam core (core stateless terhadap transaksi).
- **D-03:** **Transaksi + audit log + resolve-user tetap di wrapper.** Controller: resolve user (`Challenge()` bila null) → `BeginTransactionAsync` → panggil core → bila ok `CommitAsync` + `_auditLog.LogAsync` (post-commit) + `TempData["Success"]`; bila gagal `RollbackAsync` + `TempData["Error"]`. Side-effect (audit, TempData, redirect) TIDAK masuk core.
- **D-04:** **Test core via real-SQL `ProtonCompletionFixture`** (`HcPortal.Tests/ProtonCompletionServiceTests.cs:25`, `UseSqlServer`, `IClassFixture`/`IAsyncLifetime`). Alasan: hanya real SQL yang meng-enforce `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` — bukti D-03 (index bebas → re-assignable) tak bisa diuji nyata di InMemory.
- **D-05:** `IsYearCompletedAsync` (`:1100`, private) WAJIB ikut diekstrak/dijadikan static agar core bisa memvalidasi kelengkapan Tahun 3 tanpa instance controller.
- **D-06:** **Standard 5-6 [Fact]** (lihat Validation Architecture untuk daftar lengkap).
- **D-07:** **Full end-state pada core** (happy path): `IsCompleted==true` + `IsActive==false` + `CompletedAt`/`EndDate` non-null + setiap cascaded assignment `IsActive==false` + `DeactivatedAt` non-null + `cascadeCount == jumlah assignment aktif sebelum graduate` + jumlah baris progress TIDAK berubah.
- **D-08:** **Guard assert:** `ok==false` + `error` non-empty (cek **token kunci** saja, BUKAN verbatim — hindari brittle microcopy lock) + mapping & assignment TIDAK termutasi.
- **D-09:** **Audit log TIDAK di-assert** di xUnit.

### Claude's Discretion
- Bentuk signature core final (apakah mapping-null ditangani di core vs wrapper) — finalkan saat planning konsisten dgn pola 363.
- Strategi seed fixture (reuse `SeedProgressChainAsync`-style + tambah Tahun-3 chain + `ProtonFinalAssessment` + `CoachCoacheeMapping`).
- Apakah wrapper diberi 1 smoke-test ringan opsional (di luar core) — opsional, bukan keharusan.

### Deferred Ideas (OUT OF SCOPE)
- **AF-6 race harness** — uji unique-index race (butuh fixture Tahun-2+, multi-thread). Tetap backlog 999.5.
- **e2e Playwright re-assign-after-graduate** — varian (a) backlog 999.5.
- **Rollback-on-failure test** — butuh test level-controller (UserManager-double). Ditolak.
- **Assert baris audit log** — butuh UserManager-double. Ditolak (D-09).
</user_constraints>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Validasi guard graduate (Tahun3 ada + complete) | API/Backend (core static) | — | Logika DB murni; pindah ke core agar testable tanpa controller |
| Mutasi flag/timestamp + cascade deactivate | API/Backend (core static) | Database (filtered unique index) | Mutasi `_context`; index DB yang enforce re-assignability |
| Transaksi (Begin/Commit/Rollback) | API/Backend (wrapper) | — | D-03: orkestrasi atomicity tetap di wrapper (stateless core) |
| Resolve-user + audit log + TempData/redirect | API/Backend (wrapper) | — | D-03/D-09: butuh `_userManager`/`_auditLog`/HTTP context |
| Enforcement `IX_..._ActiveUnique` | Database | — | Hanya real SQL (D-04); InMemory tidak punya filtered index |
| Verifikasi end-state (test) | Test (xUnit) | Database (real SQL fixture) | `ProtonCompletionFixture` + `AsNoTracking` second context |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| xUnit | (existing di `HcPortal.Tests.csproj`) | Test framework | Sudah dipakai seluruh test proyek |
| EF Core (`Microsoft.EntityFrameworkCore`) | (existing) | ORM + `UseSqlServer` + `MigrateAsync` | Fixture `ProtonCompletionFixture` sudah pakai |
| SQL Server Express (`localhost\SQLEXPRESS`) | lokal dev | Real-SQL test DB (disposable `HcPortalDB_Test_<guid>`) | Satu-satunya yang enforce filtered unique index (D-04) |

**Tidak ada paket baru.** Fase test-only; reuse infrastruktur Phase 358/363 yang sudah ada. `[VERIFIED: HcPortal.Tests/ proyek sudah berisi 31 file test, fixture + FakeNotificationService + AuditLogService(ctx) sudah ada]`

### Supporting (reuse, bukan tambah)
| Asset | Lokasi | Purpose |
|-------|--------|---------|
| `ProtonCompletionFixture` | `ProtonCompletionServiceTests.cs:25-61` | `IClassFixture` real-SQL disposable, `MigrateAsync()` penuh |
| Pola `SeedProgressChainAsync` | `ProtonApproveRejectParityTests.cs:31-65` | Basis seed chain track→komp→sub→deliverable→progress |
| Pola panggil static core | `ProtonApproveRejectParityTests.cs:82,111,...` | `var (ok, error, ...) = await Controller.XxxCoreAsync(ctx, ...)` |
| Pola verifikasi `AsNoTracking` 2nd context | `ProtonApproveRejectParityTests.cs:88-89` | `await using var verify = new ApplicationDbContext(_fixture.Options)` |

**Installation:** N/A (tidak ada paket baru).

## Architecture Patterns

### System Architecture Diagram

```
[POST /Admin/MarkMappingCompleted]   <-- HTTP entry (Admin/HC only)
        |
        v
  +-----------------------------+
  | WRAPPER (controller method)  |   D-03: resolve-user, transaksi, audit, redirect
  |  1. user = GetUserAsync()    |---- null ---> Challenge()
  |  2. BeginTransactionAsync    |
  |  3. (ok,err,n)=Core(ctx,id) -+----+
  |  4. ok? CommitAsync          |    |
  |       + auditLog.LogAsync    |    |
  |       + TempData[Success]    |    |
  |     !ok? RollbackAsync       |    |
  |       + TempData[Error]      |    |
  |  5. RedirectToAction         |    |
  +-----------------------------+    |
                                     v
              +------------------------------------------+
              | CORE (static, D-01/D-02)                  |  <-- xUnit memanggil INI langsung
              |  a. mapping = Find(mappingId)             |--- null --> (false,"...tidak ditemukan",0)
              |  b. assignments aktif coachee (Include)   |
              |  c. tahun3 = first TahunKe=="Tahun 3"     |--- null --> (false,"...Tahun 3",0)
              |  d. IsYearCompletedAsync(ctx,t3.Id)       |--- false -> (false,"...belum lulus",0)
              |  e. MUTASI: IsCompleted=true, IsActive=   |
              |     false, CompletedAt, EndDate           |
              |  f. CASCADE: active assignments ->        |
              |     IsActive=false + DeactivatedAt        |
              |  g. cascadeCount = active.Count           |
              |  h. SaveChangesAsync()                    |
              |  -> (true, null, cascadeCount)            |
              +------------------------------------------+
                                     |
                                     v
              +------------------------------------------+
              | IsYearCompletedAsync (static, D-05)       |
              |  progresses = Where(AssignmentId==t3)     |
              |  if empty -> false                        |
              |  allApproved = All(Status=="Approved")    |
              |  hasFA = ProtonFinalAssessments.Any(      |
              |            AssignmentId==t3)              |
              |  return allApproved && hasFA              |
              +------------------------------------------+
                                     |
                                     v
              [ DB: real SQL — IX_CoachCoacheeMappings_CoacheeId_ActiveUnique
                filter WHERE [IsActive]=1; set IsActive=false membebaskan coachee ]
```

### Pattern 1: Static Core Extraction (pola Phase 363) — WAJIB DITIRU
**What:** Endpoint controller dipecah jadi (a) wrapper tipis (UserManager/transaksi/notif/redirect) + (b) `public static async Task<(...)>` core berisi logika DB murni.
**When to use:** Endpoint yang deref `_userManager.GetUserAsync` di baris awal (tak bisa di-null-substitute) tapi logika DB-nya perlu di-pin parity-lock.
**Example (signature + return tuple persis pola 363):**
```csharp
// Source: Controllers/CDPController.cs:981 (ApproveDeliverableCoreAsync) — VERIFIED
public static async Task<(bool ok, string? error, bool allApproved)> ApproveDeliverableCoreAsync(
    ApplicationDbContext context, int progressId,
    string actorId, string actorName, string actorRole, bool isSrSpv, bool isSH)
{
    var progress = await context.ProtonDeliverableProgresses...FirstOrDefaultAsync(...);
    if (progress == null) return (false, "Deliverable tidak ditemukan.", false);  // not-found DI CORE
    // ... mutasi ...
    await context.SaveChangesAsync();   // SaveChanges DI CORE
    return (true, null, allApproved);
}
```
**Wrapper memanggil core (pola 363):**
```csharp
// Source: Controllers/CDPController.cs:865-873 — VERIFIED
var (ok, error, allApproved) = await ApproveDeliverableCoreAsync(_context, progressId, user.Id, user.FullName, userRole ?? "", isSrSpv, isSH);
if (!ok) {
    if (error == "Deliverable tidak ditemukan.") return NotFound();
    TempData["Error"] = error;
    return RedirectToAction("CoachingProton");
}
```

### Pattern 2: Test memanggil core langsung + verifikasi 2nd context
**Example:**
```csharp
// Source: ProtonApproveRejectParityTests.cs:82-99 — VERIFIED
var (ok, error) = await CDPController.RejectDeliverableCoreAsync(ctx, ids[0], "srspv-1", ...);
Assert.True(ok);
await using var verify = new ApplicationDbContext(_fixture.Options);   // 2nd context
var p = await verify.ProtonDeliverableProgresses.AsNoTracking().FirstAsync(x => x.Id == ids[0]);
Assert.Equal("Rejected", p.Status);
```

### Anti-Patterns to Avoid
- **InMemory provider:** TIDAK enforce filtered unique index → re-assignability [Fact] palsu-hijau. D-04 melarang.
- **Insert ProtonTrack baru di seed:** UNIQUE `AK_ProtonTracks_TrackType_TahunKe` → throw. Selalu `FirstAsync(t => t.TrackType==... && t.TahunKe==...)` (reuse baris migration).
- **Assert microcopy verbatim:** D-08 cek token kunci ("Tahun 3", "belum lulus") bukan kalimat penuh → hindari brittle test.
- **`BeginTransactionAsync` di dalam core:** D-02 melarang. (CATATAN divergensi vs 363 — lihat OQ-1.)
- **`RemoveRange` progress saat graduate:** AF-3 D-04 melarang; histori progress WAJIB utuh (kode produksi memang tidak RemoveRange — lock ini via [Fact] #6).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| DB test disposable real-SQL | Setup/teardown DB manual | `ProtonCompletionFixture` (existing) | Sudah `MigrateAsync` + drop di sukses & gagal-mid-migration |
| Seed track master | Insert ProtonTrack | `FirstAsync(TrackType+TahunKe)` reuse | Migration sudah seed 6 baris; insert = UNIQUE violation |
| Isolasi antar-[Fact] | DB per-fact / cleanup | `coacheeId` unik via `$"...-{Guid.NewGuid():N}"` | Pola existing (parity tests share fixture DB) |
| Notif/audit service di core | Mock UserManager | Jangan — taruh di wrapper (D-03) | D-01: hindari boilerplate UserManager-double |

**Key insight:** Seluruh infrastruktur test sudah dibangun Phase 358/363. Fase 365 = compose ulang pola yang sudah terbukti hijau, bukan bikin baru.

## Runtime State Inventory

> N/A untuk fase ini. Fase test-only, **bukan** rename/refactor/migrasi data. Tidak ada stored data / live service config / OS-registered state / secrets / build artifact yang berubah. Migration=false. DB lokal `HcPortalDB_Dev` TAK tersentuh (fixture pakai `HcPortalDB_Test_<guid>` disposable). **Verified by:** CONTEXT.md `<domain>` + `ProtonCompletionFixture` connection string (`HcPortalDB_Test_{Guid}`).

## Common Pitfalls

### Pitfall 1: Mengira ProtonTracks di-seed `SeedData.cs` (tak jalan di fixture)
**What goes wrong:** Test gagal `FirstAsync` ProtonTrack karena mengira fixture tak punya track.
**Why it happens:** `SeedData.SeedProtonTracksAsync` (`SeedData.cs:142`) hanya dipanggil saat app startup, BUKAN oleh `MigrateAsync()`.
**How to avoid:** **Track di-seed migration** `20260223060707_CreateProtonTrackTable` Step 5 (SQL MERGE 6 baris). Karena fixture `MigrateAsync()` penuh, 6 track PASTI ada. `[VERIFIED: Migrations/20260223060707_CreateProtonTrackTable.cs:78-93]`
**Warning signs:** `InvalidOperationException: Sequence contains no elements` saat `FirstAsync` track.

### Pitfall 2: Memakai TrackType salah untuk Tahun 3
**What goes wrong:** Seed assignment Tahun 3 dengan TrackType yang tak ada → throw.
**How to avoid:** TrackType valid: `"Operator"` atau `"Panelman"`, keduanya punya `"Tahun 3"`. Existing test pakai `"Operator"`. Reuse: `FirstAsync(t => t.TrackType=="Operator" && t.TahunKe=="Tahun 3")` (Id=6 di seed migration). `[VERIFIED: Migration MERGE baris 86 'Operator','Tahun 3']`

### Pitfall 3: Re-assignability [Fact] tak benar-benar membuktikan apa-apa kalau index hilang
**What goes wrong:** Insert mapping aktif baru "sukses" hanya karena InMemory/test DB tak punya index.
**How to avoid:** Pakai `ProtonCompletionFixture` (real SQL, `MigrateAsync` membangun index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`). Untuk memperkuat: pertimbangkan assert NEGATIF tambahan — insert mapping aktif kedua TANPA graduate dulu HARUS throw `DbUpdateException` (membuktikan index aktif), lalu setelah graduate insert berhasil. (Opsional, perkuat OQ-2.)
**Warning signs:** [Fact] hijau padahal index di-drop — test tidak punya gigi.

### Pitfall 4: Lupa ProtonFinalAssessment butuh unique 1:1 dengan assignment
**What goes wrong:** Seed 2 `ProtonFinalAssessment` untuk 1 assignment → throw.
**How to avoid:** `entity.HasIndex(fa => fa.ProtonTrackAssignmentId).IsUnique()` (`ApplicationDbContext.cs:417`). Satu FinalAssessment per assignment Tahun 3. `[VERIFIED]`

## Code Examples

### Predikat `IsYearCompletedAsync` (yang harus di-static-kan) — VERIFIED
```csharp
// Source: Controllers/CoachMappingController.cs:1100-1110 — VERIFIED
private async Task<bool> IsYearCompletedAsync(int assignmentId)
{
    var progresses = await _context.ProtonDeliverableProgresses
        .Where(p => p.ProtonTrackAssignmentId == assignmentId)
        .ToListAsync();
    if (!progresses.Any()) return false;                       // 0 progress -> belum lulus
    bool allApproved = progresses.All(p => p.Status == "Approved");
    bool hasFinalAssessment = await _context.ProtonFinalAssessments
        .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignmentId);
    return allApproved && hasFinalAssessment;                  // SEMUA Approved DAN ada FinalAssessment
}
```
**Static signature usulan (zero behavior change):**
```csharp
private static async Task<bool> IsYearCompletedAsync(ApplicationDbContext ctx, int assignmentId)
{
    var progresses = await ctx.ProtonDeliverableProgresses
        .Where(p => p.ProtonTrackAssignmentId == assignmentId).ToListAsync();
    if (!progresses.Any()) return false;
    bool allApproved = progresses.All(p => p.Status == "Approved");
    bool hasFinalAssessment = await ctx.ProtonFinalAssessments
        .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignmentId);
    return allApproved && hasFinalAssessment;
}
```
Hanya menyentuh `_context` → `ctx`. **TIDAK** ada UserManager/service lain. `[VERIFIED]` Aman jadi static. Method instance lama bisa dihapus (satu-satunya pemanggil adalah `MarkMappingCompleted` yang juga di-refactor) ATAU delegasi `=> IsYearCompletedAsync(_context, assignmentId)` bila ada pemanggil lain — **CEK pemanggil saat planning** (grep `IsYearCompletedAsync(` di seluruh repo). Saat ini hanya 1 pemanggil di `:1134`.

### Kode produksi yang dibagi core vs wrapper (baris-presisi) — VERIFIED
```csharp
// Source: Controllers/CoachMappingController.cs:1116-1179 — VERIFIED
// === KE CORE (D-01/D-02) ===
//   :1120  mapping = FindAsync(mappingId);  if null -> (false,"...tidak ditemukan",0)
//   :1123-1126  assignments aktif coachee (Include ProtonTrack)
//   :1127-1128  tahun3Assignment = first TahunKe=="Tahun 3"
//   :1129-1133  if null -> (false,"Coachee belum memiliki assignment Tahun 3.",0)  [token: "Tahun 3"]
//   :1134      tahun3Complete = IsYearCompletedAsync(ctx, tahun3.Id)
//   :1135-1139 if !complete -> (false,"...Tahun 3 belum lulus...",0)  [token: "belum lulus"]
//   :1143-1146 mapping.IsCompleted=true; CompletedAt; IsActive=false; EndDate
//   :1150-1159 cascade: active assignments -> IsActive=false + DeactivatedAt; cascadeCount
//   :1161      SaveChangesAsync()  (lihat OQ-1: di core vs di wrapper)
//   return (true, null, cascadeCount)
//
// === TETAP DI WRAPPER (D-03) ===
//   :1118-1119 user = GetUserAsync(); if null -> Challenge()
//   :1140      BeginTransactionAsync()
//   :1162      CommitAsync()
//   :1164-1168 auditLog.LogAsync(...) POST-commit
//   :1169-1170 TempData["Success"] + RedirectToAction
//   :1172-1178 catch -> RollbackAsync + TempData["Error"] + Redirect
```

**Core signature usulan (final, pola 363 + cascadeCount untuk D-07):**
```csharp
public static async Task<(bool ok, string? error, int cascadeCount)> MarkMappingCompletedCore(
    ApplicationDbContext context, int mappingId)
```

### Filtered unique index — VERIFIED
```csharp
// Source: Data/ApplicationDbContext.cs:325-333 — VERIFIED
builder.Entity<CoachCoacheeMapping>(entity =>
{
    entity.HasIndex(m => m.CoachId);
    entity.HasIndex(m => m.CoacheeId)
        .HasFilter("[IsActive] = 1")          // <-- FILTER: hanya baris IsActive=1 yang unik per CoacheeId
        .IsUnique()
        .HasDatabaseName("IX_CoachCoacheeMappings_CoacheeId_ActiveUnique");
    entity.HasIndex(m => new { m.CoachId, m.CoacheeId });
});
```
**Implikasi re-assignability:** satu coachee hanya boleh punya **satu** mapping `IsActive=1`. Graduate set `IsActive=false` → coachee keluar dari filter → mapping aktif BARU untuk coachee yang sama diterima. [Fact] #2 membuktikan ini di real SQL.

### Seed Tahun-3-COMPLETE state (resep, basis `SeedProgressChainAsync`)
```csharp
// Adaptasi ProtonApproveRejectParityTests.cs:31-65 — tambah Tahun 3 + FinalAssessment + Mapping
private static async Task<(int mappingId, int t3AssignmentId, int progressCount)>
    SeedGraduateReadyAsync(ApplicationDbContext ctx, string coacheeId)
{
    var coachId = $"coach-{Guid.NewGuid():N}";
    // 1) Track Tahun 3 — REUSE migration seed (jangan insert)
    var t3 = await ctx.ProtonTracks.FirstAsync(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 3");

    // 2) Assignment aktif Tahun 3
    var asg = new ProtonTrackAssignment { CoacheeId = coacheeId, AssignedById = "hc", ProtonTrackId = t3.Id, IsActive = true };
    ctx.ProtonTrackAssignments.Add(asg);
    await ctx.SaveChangesAsync();

    // 3) Hierarki komp -> sub -> deliverable -> progress (SEMUA Approved untuk lulus)
    var komp = new ProtonKompetensi { Bagian = "B-PAR", Unit = $"U-{coacheeId[..8]}", NamaKompetensi = $"K-{coacheeId}", Urutan = 1, ProtonTrackId = t3.Id };
    ctx.ProtonKompetensiList.Add(komp); await ctx.SaveChangesAsync();
    var sub = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = "Sub", Urutan = 1 };
    ctx.ProtonSubKompetensiList.Add(sub); await ctx.SaveChangesAsync();
    int progressCount = 0;
    foreach (var i in new[] { 1, 2 })
    {
        var d = new ProtonDeliverable { ProtonSubKompetensiId = sub.Id, NamaDeliverable = $"D{i}", Urutan = i };
        ctx.ProtonDeliverableList.Add(d); await ctx.SaveChangesAsync();
        ctx.ProtonDeliverableProgresses.Add(new ProtonDeliverableProgress {
            CoacheeId = coacheeId, ProtonDeliverableId = d.Id, ProtonTrackAssignmentId = asg.Id,
            Status = "Approved", CreatedAt = DateTime.UtcNow });   // <-- Approved = syarat lulus
        await ctx.SaveChangesAsync(); progressCount++;
    }

    // 4) ProtonFinalAssessment (syarat kedua IsYearCompletedAsync) — 1:1 dgn assignment
    ctx.ProtonFinalAssessments.Add(new ProtonFinalAssessment {
        CoacheeId = coacheeId, CreatedById = "hc", ProtonTrackAssignmentId = asg.Id,
        Status = "Completed", CompletedAt = DateTime.UtcNow });
    await ctx.SaveChangesAsync();

    // 5) CoachCoacheeMapping aktif (yang akan di-graduate)
    var mapping = new CoachCoacheeMapping {
        CoachId = coachId, CoacheeId = coacheeId, IsActive = true,
        StartDate = DateTime.UtcNow, IsCompleted = false };
    ctx.CoachCoacheeMappings.Add(mapping); await ctx.SaveChangesAsync();

    return (mapping.Id, asg.Id, progressCount);
}
```
**Field wajib (non-null) yang dikonfirmasi dari model:**
- `CoachCoacheeMapping` (`Models/CoachCoacheeMapping.cs`): `CoachId` (default ""), `CoacheeId` (default ""), `IsActive` (default true), `StartDate` (`DateTime` non-null — **WAJIB di-set**, default `DateTime.MinValue` bila lupa, masih valid insert). `EndDate`/`AssignmentSection`/`AssignmentUnit`/`CompletedAt` nullable. `IsCompleted` default false. `[VERIFIED]`
- `ProtonFinalAssessment` (`Models/ProtonModels.cs:210`): wajib `CoacheeId`, `CreatedById`, `ProtonTrackAssignmentId`, `Status` (default "Completed"), `CompetencyLevelGranted` (int default 0, `[Range(0,5)]` OK), `CreatedAt` (default `DateTime.UtcNow`). `Origin`/`KkjMatrixItemId`/`Notes`/`CompletedAt` nullable. `[VERIFIED]`
- `ProtonTrackAssignment` (`Models/ProtonModels.cs:71`): wajib `CoacheeId`, `AssignedById`, `ProtonTrackId`, `IsActive`; `AssignedAt` default. `DeactivatedAt`/`Origin` nullable. `[VERIFIED]`

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Logika graduate inline di endpoint (UserManager deref di baris awal) | Static core + wrapper tipis | Phase 365 (fase ini) | Testable tanpa UserManager-double |
| Pin perilaku via UAT manual / code-verify (Phase 356-05 AF-3) | xUnit parity-lock otomatis | Phase 365 | Regresi AF-3 ketahuan di `dotnet test` |

**Deprecated/outdated untuk fase ini:**
- Pola InMemory `OrganizationControllerTests` yang DISEBUT SC roadmap → TIDAK dipakai (D-01/D-04: UserManager deref + filtered unique index).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (kosong) | — | Semua klaim teknis terverifikasi langsung dari kode/migration sesi ini. |

**Tidak ada klaim `[ASSUMED]`.** Seluruh unknown (predikat, index filter, seed track, field wajib, pola core) dikonfirmasi via Read/Grep file aktual. Tidak perlu konfirmasi user untuk fakta teknis. (Keputusan desain OQ-1/OQ-2 di bawah adalah pilihan planning, bukan asumsi fakta.)

## Open Questions

1. **OQ-1 — SaveChangesAsync di core vs wrapper, dan letak transaksi.**
   - **What we know:** Pola 363 (approve/reject) memanggil `SaveChangesAsync` **di dalam core, TANPA transaksi sama sekali** (`CDPController.cs:1064`). Tapi `MarkMappingCompleted` produksi saat ini **membungkus mutasi dengan transaksi** di wrapper (`:1140` Begin, `:1162` Commit). D-02 melarang `BeginTransactionAsync` di core; D-03 minta transaksi tetap di wrapper.
   - **What's unclear:** Apakah core memanggil `SaveChangesAsync` (lalu wrapper hanya `CommitAsync` transaksi yang membungkusnya) ATAU core hanya mutasi tanpa SaveChanges (wrapper SaveChanges + Commit)?
   - **Recommendation:** **Core memanggil `SaveChangesAsync()` lalu return** (konsisten pola 363, dan test bisa verifikasi end-state tanpa transaksi). Wrapper membuka `BeginTransactionAsync` SEBELUM panggil core, dan `CommitAsync` SETELAH core return `ok=true`. SaveChanges di dalam transaksi wrapper tetap atomik; commit men-finalize. Test memanggil core langsung tanpa transaksi → SaveChanges core auto-commit (autocommit mode) → end-state terverifikasi. **Zero behavior change** karena urutan mutasi+save identik; hanya seam-nya yang dipindah. Finalkan di planning; PASTIKAN `dotnet test` + build hijau membuktikan parity.

2. **OQ-2 — Mapping-null: di core atau wrapper?**
   - **What we know:** Produksi: `if (mapping == null) return NotFound();` di wrapper (`:1121`), SEBELUM transaksi. Pola 363: core return `(false, "...tidak ditemukan.")` lalu wrapper map ke `NotFound()`.
   - **Recommendation:** **Tangani di core** (return `(false, "Mapping tidak ditemukan.", 0)`) agar [Fact] #5 bisa test core langsung tanpa controller; wrapper map `error == "Mapping tidak ditemukan."` → `NotFound()` (pola 363 `:870`). Konsisten + testable. (Discretion D-06 #5 mengizinkan finalisasi saat planning.)

3. **OQ-3 — Pemanggil lain `IsYearCompletedAsync`?**
   - **What we know:** Saat ini hanya 1 pemanggil (`:1134`).
   - **Recommendation:** Grep `IsYearCompletedAsync(` saat planning. Jika hanya 1 → ganti langsung ke static. Jika >1 → static + instance delegasi `=> IsYearCompletedAsync(_context, id)` (zero behavior change).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| SQL Server Express `localhost\SQLEXPRESS` (Integrated Security) | `ProtonCompletionFixture` real-SQL test | ✓ (existing tests Phase 358/363 jalan lokal) | SQLEXPRESS | Tidak ada — D-04 wajib real SQL |
| .NET SDK + `dotnet test` | Gate verifikasi | ✓ | (existing) | — |
| `dotnet run` localhost:5277 (AD-off) | — | N/A | — | **Tidak perlu** — fase xUnit-only, `dotnet test` = gate |

**Catatan CI:** Fixture ditandai `[Trait("Category","Integration")]`. CI tanpa SQL skip via `dotnet test --filter "Category!=Integration"`. Test baru `MarkMappingCompletedTests` HARUS ikut `[Trait("Category","Integration")]` (real SQL). `[VERIFIED: ProtonCompletionServiceTests.cs:21,63]`

**Connection string source (untuk planner):** Hard-coded di fixture constructor, BUKAN env var/secret: `Server=localhost\SQLEXPRESS;Database=HcPortalDB_Test_<guid>;Integrated Security=True;TrustServerCertificate=True;...` (`ProtonCompletionServiceTests.cs:37`). DB `HcPortalDB_Dev` lokal TAK tersentuh — no SEED_WORKFLOW snapshot/restore diperlukan.

## Validation Architecture

> Nyquist enabled (config tidak set `workflow.nyquist_validation:false`). Section ini = sumber derivasi VALIDATION.md.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (existing `HcPortal.Tests`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (existing) |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~MarkMappingCompletedTests"` |
| Full suite command | `dotnet test` (atau `--filter "Category!=Integration"` di CI SQL-less) |
| Fixture | `ProtonCompletionFixture` (real SQL disposable, `IClassFixture`) — REUSE |

### Phase Scenario (D-06) → Test Map
| # | Skenario | Test Type | Assert (token/end-state) | Automated Command | File Exists? |
|---|----------|-----------|--------------------------|-------------------|-------------|
| 1 | **Happy** — Tahun 3 lulus → graduate | integration | `ok==true`; mapping `IsCompleted==true` + `IsActive==false` + `CompletedAt!=null` + `EndDate!=null`; tiap cascaded assignment `IsActive==false` + `DeactivatedAt!=null`; `cascadeCount==N` (jumlah aktif pra-graduate); jumlah progress TAK berubah | `dotnet test --filter "...MarkMappingCompleted_Happy_FullEndState"` | ❌ Wave 0 |
| 2 | **Re-assignability** — pasca-graduate insert mapping aktif baru same coachee | integration | Setelah core ok=true → `ctx.CoachCoacheeMappings.Add(new{...IsActive=true,same CoacheeId})` + `SaveChangesAsync` → **tidak throw** (index bebas). (Opsional perkuat: pre-graduate insert duplikat HARUS throw `DbUpdateException`.) | `dotnet test --filter "...ReassignableAfterGraduate"` | ❌ Wave 0 |
| 3 | **Guard no-Tahun3** — coachee tanpa assignment Tahun 3 | integration | `ok==false`; `error` contains `"Tahun 3"`; mapping `IsActive==true`+`IsCompleted==false` (tak termutasi); `cascadeCount==0` | `dotnet test --filter "...Guard_NoTahun3"` | ❌ Wave 0 |
| 4 | **Guard Tahun3-belum-lulus** — Tahun3 ada, progress tak semua Approved / tak ada FinalAssessment | integration | `ok==false`; `error` contains `"belum lulus"`; mapping & assignment tak termutasi. (Buat 2 sub-kasus: a) progress ada `Submitted`; b) semua Approved tapi `ProtonFinalAssessment` tidak ada) | `dotnet test --filter "...Guard_Tahun3Incomplete"` | ❌ Wave 0 |
| 5 | **Mapping null/not-found** | integration | core(`ctx`, `mappingId=-1`) → `ok==false` + `error` contains `"tidak ditemukan"` (per OQ-2) | `dotnet test --filter "...MappingNotFound"` | ❌ Wave 0 |
| 6 | **Histori progress utuh** — graduate TIDAK `RemoveRange` progress | integration | `count(ProtonDeliverableProgresses where AssignmentId==t3) ` sama sebelum & sesudah core ok=true | `dotnet test --filter "...ProgressHistoryIntact"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "FullyQualifiedName~MarkMappingCompletedTests"` (cepat, hanya 6 fact)
- **Per wave merge / build gate:** `dotnet build HcPortal.csproj` (0 error) + `dotnet test` (semua hijau, 0 regresi vs baseline)
- **Phase gate:** Full suite green + build green = bukti parity (zero behavior change D-07/D-08).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/MarkMappingCompletedTests.cs` — 6 [Fact] (skenario D-06), `[Trait("Category","Integration")]`, `IClassFixture<ProtonCompletionFixture>`, helper `SeedGraduateReadyAsync`
- [ ] Refactor produksi `Controllers/CoachMappingController.cs` — ekstrak `MarkMappingCompletedCore` + static `IsYearCompletedAsync` (prasyarat agar core callable dari test)
- [ ] Framework install: **tidak perlu** — xUnit + fixture + FakeNotificationService + AuditLogService(ctx) sudah ada
- [ ] Shared fixture: **tidak perlu baru** — reuse `ProtonCompletionFixture`

*(Catatan: `MarkMappingCompletedCore` tidak butuh `FakeNotificationService`/`AuditLogService` — notif/audit ada di wrapper D-03/D-09. Berbeda dari `ProtonCompletionServiceTests.NewSvc` yang inject keduanya.)*

## Security Domain

> `security_enforcement` default enabled. Fase test-only, tidak menambah surface baru.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no (perilaku tak berubah) | Wrapper `[Authorize(Roles="Admin,HC")]` + `GetUserAsync`→`Challenge()` PRESERVED (D-03) |
| V3 Session Management | no | — |
| V4 Access Control | no (preserved) | `[Authorize(Roles="Admin, HC")]` di wrapper TIDAK dipindah ke core |
| V5 Input Validation | no | `mappingId` int route param; guard validasi domain di core |
| V6 Cryptography | no | — |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Bypass authorization via core dipanggil langsung | Elevation of Privilege | `[Authorize]` WAJIB tetap di wrapper (D-03); core static = unit-testable saja, TIDAK exposed sebagai endpoint |
| Mutasi tanpa transaksi (partial write) | Tampering | Transaksi tetap di wrapper (D-03); core SaveChanges dalam scope transaksi wrapper (OQ-1) |
| Information leak via error verbatim | Information Disclosure | D-08 test token kunci; error message domain-safe (tak bocor `ex.Message`) — preserved |

**Catatan:** Zero behavior change = zero new attack surface. Refactor harus preserve `[Authorize(Roles="Admin, HC")]` (`:1115`) + `[ValidateAntiForgeryToken]` (`:1114`) + `[HttpPost]` (`:1113`) di wrapper verbatim.

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk semua respon user-facing. (RESEARCH.md ini mengikuti.)
- **Verifikasi lokal wajib:** untuk fase ini = `dotnet build` (0 error) + `dotnet test` (hijau). `dotnet run` localhost:5277 **tidak diperlukan** (xUnit-only, tak ada perubahan UI/perilaku runtime).
- **Migration:** `false`. Tidak ada file migration. Notify IT: tidak ada flag migration.
- **Seed Workflow:** TIDAK diperlukan — fixture pakai DB disposable `HcPortalDB_Test_<guid>`, DB lokal `HcPortalDB_Dev` tak tersentuh.
- **Jangan edit kode/DB di Dev/Prod.** Jangan push tanpa verifikasi lokal.

## 🚨 Flag untuk Planner — Amendemen ROADMAP SC#2 (WAJIB)

ROADMAP SC#2 Phase 365 berbunyi *"zero file produksi berubah (git diff Controllers/ Services/ kosong)"*. Keputusan D-01 (extract core, pola 363) **melanggar** klausa ini karena `Controllers/CoachMappingController.cs` PASTI berubah (ekstrak core + wrapper tipis + static `IsYearCompletedAsync`).

**Plan WAJIB:**
1. **Update ROADMAP SC#2** → *"refactor behavior-preserving + parity-locked — `Controllers/CoachMappingController.cs` disentuh (extract core + wrapper tipis), zero behavior change, dibuktikan via test core hijau + build hijau. Migration=false."*
2. **Buktikan zero behavior change:** (a) 6 [Fact] core hijau; (b) `dotnet build` 0 error; (c) `dotnet test` full suite 0 regresi vs baseline (~214+ test). Verifier TIDAK boleh gate "git diff Controllers/ kosong".
3. `Services/` tetap **tidak** berubah (hanya `Controllers/CoachMappingController.cs` + 1 file test baru). `git diff Services/` HARUS kosong.

## Sources

### Primary (HIGH confidence)
- `Controllers/CoachMappingController.cs:1100-1179` — `IsYearCompletedAsync` + `MarkMappingCompleted` (target refactor). Read langsung.
- `Controllers/CDPController.cs:865-873, 928-936, 981-1067` — pola core 363 (signature, return tuple, wrapper call, SaveChanges-in-core). Read langsung.
- `Data/ApplicationDbContext.cs:325-333, 416-417` — filtered unique index `[IsActive]=1` + ProtonFinalAssessment unique. Read langsung.
- `Migrations/20260223060707_CreateProtonTrackTable.cs:78-93` — seed 6 ProtonTrack via SQL MERGE (Operator/Panelman × Tahun 1/2/3). Read langsung.
- `Models/CoachCoacheeMapping.cs`, `Models/ProtonModels.cs:71-232` — field wajib/nullable. Read langsung.
- `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-183` — `ProtonCompletionFixture` API + isolasi per-fact. Read langsung.
- `HcPortal.Tests/ProtonApproveRejectParityTests.cs:31-65` — pola `SeedProgressChainAsync` + panggil core langsung + verifikasi 2nd context. Read langsung.
- `Data/SeedData.cs:134-165` — konfirmasi ProtonTracks di-seed migration (bukan SeedData runtime di fixture). Read langsung.

### Secondary (MEDIUM confidence)
- `.planning/phases/356-.../356-05-SUMMARY.md` — evidence AF-3 (graduate IsActive=0+IsCompleted=1, re-assignability code-verified). Konteks, bukan klaim teknis baru.

### Tertiary (LOW confidence)
- (tidak ada — tak ada WebSearch; semua dari kode lokal)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — infrastruktur test (fixture/pola) sudah ada & hijau di Phase 358/363, di-Read langsung.
- Architecture (core extraction): HIGH — pola 363 di-Read presisi; divergensi transaksi (OQ-1) diidentifikasi eksplisit.
- Pitfalls: HIGH — tiap pitfall ditarik dari konstrain DB aktual (unique index, filter, FK) yang di-Read.
- Seed shape: HIGH — predikat `IsYearCompletedAsync` + field model + seed track migration semua terkonfirmasi.

**Research date:** 2026-06-12
**Valid until:** 2026-07-12 (stabil — kode produksi & schema lokal; no fast-moving deps). Re-verifikasi bila `CoachMappingController.MarkMappingCompleted` atau index `CoachCoacheeMapping` berubah sebelum planning.
