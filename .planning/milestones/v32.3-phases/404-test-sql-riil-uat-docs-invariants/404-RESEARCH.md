# Phase 404: Test (SQL Riil) + UAT + Docs + Invariants - Research

**Researched:** 2026-06-21
**Domain:** xUnit SQL-Server integration testing (EF Core 8 migrations pipeline + filtered-unique indexes) + local browser UAT + IT handoff docs
**Confidence:** HIGH (semua temuan diverifikasi langsung di codebase; tidak ada `[ASSUMED]` material)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** **1 shared `MultiUnitSqlFixture`** (`IClassFixture`) — spin SQLEXPRESS sekali, seed dataset multi-unit kanonik dipakai semua Fact QA. Ikut pola fixture SQL-riil existing (`AbandonGuardFixture`, `ImageCleanupFixture`, `EssayFinalizeRecomputeFixture`, `OrgLabelMigrationFixture`) — `new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options`. Jangan reinvent harness.
- **D-02:** Seed fixture kanonik = pekerja **{X, Y} dalam 1 Bagian** + coach cross-unit (megang coachee di unit X dan Y) + PROTON **Tahun1@X → Tahun2@Y** sekuensial (cert tiap unit tersimpan utuh sebagai histori).
- **D-03:** Build schema fixture pakai **`db.Database.Migrate()`** (BUKAN `EnsureCreated`) — jalankan migration riil termasuk **399 `AddUserUnits`** + semua filtered-unique index. Bonus: buktikan migration=TRUE Phase 399 apply bersih di SQL-real (de-risk deploy milestone).
- **D-04:** Connection-string + lifecycle (create/teardown DB disposable) ikut konvensi fixture existing (`_cs`, sqlcmd `-C -I`, SQLEXPRESS). DB fixture **terpisah** dari DB app lokal — tak kena seed UAT.
- **D-05:** Assert invariant di **SEMUA jalur-write**: **Assign + Edit + Import + bypass-TargetUnit + Reactivate + Import-reactivate**. Target constraint DB: filtered-unique `[IsActive]=1` (single-active `ProtonTrackAssignment` + `CoachCoacheeMapping`), `[IsPrimary]=1` (one-primary UserUnits).
- **D-06:** Assert **single-active**: coachee multi-unit T1@X → bypass/reassign T2@Y → **tepat 1 `ProtonTrackAssignment` aktif + 1 `CoachCoacheeMapping` aktif**, termasuk jalur Reactivate + Import-reactivate.
- **D-07:** Assert **`AssignmentUnit ∈ coachee.UserUnits`** di tiap junction-write.
- **D-08:** Assert **B-06 anti-dobel** ikut QA-04: `ProtonDeliverableBootstrap` lintas-unit (CoacheeId sama, deliverable unit X vs Y **tak saling skip**) + **`ProtonKompetensi.Unit` 1:1 per deliverable** (filtered-unique `ApplicationDbContext.cs:429`).
- **D-09:** Stub existing `CrossUnitAssignTests.cs:105 SingleActive_invariant_is_sql_real_phase404()` WAJIB di-implement (jangan body kosong).
- **D-10:** UAT fokus alur paling berisiko: PROTON sekuensial cross-unit T1@X → T2@Y + cert histori per-unit utuh + coach multi-unit lihat/export coachee lintas-unit. Invariant DB diserahkan ke xUnit SQL-riil. Cross-unit assign round-trip sudah di-UAT Phase 402 (`7f5b6a17`) — tak diulang penuh.
- **D-11:** UAT di **`localhost:5270`** (branch ITHandoff — bukan 5277). Gate: `dotnet build` 0 error + `dotnet run` + cek DB lokal + Playwright bila ada.
- **D-12:** Seed data UAT = **temporary local-only** per SEED_WORKFLOW: snapshot DB lokal → insert seed {X,Y}+coach+PROTON → UAT → **restore** + tandai journal `cleaned`. Catat di `docs/SEED_JOURNAL.md`.
- **D-13:** **HTML handoff IT** ikut pola existing (`docs/milestone-v*/index.html`) — isi: notice **migration=TRUE Phase 399** (`AddUserUnits` + backfill), **commit hash**, langkah deploy Dev, daftar fase 399-404. Plus catatan **batasan D1=b** ringkas di **markdown** `docs/`.
- **D-14:** Dokumentasikan batasan **D1=b**: cert/analytics atribusi ke primary unit (`ApplicationUser.Unit` mirror), bukan per-unit penuh.
- **D-15:** Definition-of-done 404: `dotnet build` 0 error + `dotnet test` hijau (suite multi-unit SQL-riil **baru** + suite existing **tak regresi**, baseline ~547/0/6) + UAT browser sign-off.

### Claude's Discretion
- Penamaan file/kelas test (selama ikut konvensi `*Tests.cs` + `*Fixture`).
- Detail teknis connection-string, teardown DB disposable, helper seed fixture.
- Struktur internal HTML handoff (selama memuat info D-13).
- Apakah pisah jadi beberapa test class per-invariant atau 1 class dengan banyak `[Fact]` (selama share `MultiUnitSqlFixture` per D-01).

### Deferred Ideas (OUT OF SCOPE)
- **Re-trigger Phase 402 code-review + secure** (2 seam baru `CDPController.CoerceCoachUnitScope` + `CoachMappingController.FilterEligibleCoachees`). **Task Phase 402 terpisah, dikerjakan sebelum/paralel 404 (pre-milestone-close), BUKAN scope 404.** Jangan lupa sebelum `/gsd-complete-milestone v32.3`.
- **One-time cleanup data test/audit lokal setelah Phase 367** (todo backlog) — tak terkait seed UAT 404. Tetap di backlog.
- Production code change apa pun (kecuali ekstraksi seam test-only bila benar-benar perlu — **hindari**, lihat Risiko R-1).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **QA-01** | Test multi-unit dijalankan di **SQL riil** (EF-InMemory tidak meng-enforce filtered-unique index) — fixture pekerja {X,Y} 1 Bagian + coach cross-unit + PROTON T1@X → T2@Y | `MultiUnitSqlFixture` mengikuti pola 6 fixture SQL-real existing (§Standard Stack, §Pattern 1). `db.Database.Migrate()` menjalankan chain ~94 migration termasuk 399 `AddUserUnitsTable` + seed `ProtonTracks` data-migration → master track tersedia gratis. Pitfall: InMemory `WorkerDataServiceSearchTests`/`UserUnitsBackfillIntegrationTests` membuktikan logic, 404 menambah dimensi DB-constraint. |
| **QA-02** | UAT lokal (build+run+DB lokal, Playwright bila ada) + docs batasan **D1=b** | §UAT Mechanics: SEED_WORKFLOW snapshot→seed→restore + port 5270 (ITHandoff). Recipe terbukti reuse dari SEED_JOURNAL 402-03/04 (rustam coach + iwan3 cross-unit). Docs: HTML handoff (pola v31.0) + markdown D1=b note. |
| **QA-03** | Test invariant **single-active** SQL riil (1 `ProtonTrackAssignment` + 1 `CoachCoacheeMapping` aktif, +Reactivate/Import) | §Write-Path Map: `CoachCoacheeMapping` single-active **DI-ENFORCE DB** (`IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`, `ApplicationDbContext.cs:333-336`). **PENTING:** `ProtonTrackAssignment` single-active **TIDAK** ada filtered-unique index (hanya non-unique `(CoacheeId, IsActive)` `:393`) — di-enforce **app-level** (deactivate-before-create di `CoachCoacheeMappingAssign:718-726` + `MoveAssignmentAsync:413`). Test harus assert via **count**, bukan mengharap DbUpdateException untuk PTA. `ProtonBypassService` drivable SQL-real (precedent `ProtonBypassServiceTests`). |
| **QA-04** | Test invariant `AssignmentUnit ∈ coachee.UserUnits` tiap write + B-06 anti-dobel + `ProtonKompetensi.Unit` 1:1 | Helper static `ValidateAssignmentUnitInUserUnits` + `ProtonDeliverableBootstrap.CreateProgressAsync` (B-06 guard) keduanya drivable SQL-real langsung. `ProtonKompetensi.Unit` 1:1 = filtered-unique `(ProtonTrackAssignmentId, ProtonDeliverableId)` `:429` pada `ProtonDeliverableProgress`. |
</phase_requirements>

## Summary

Phase 404 adalah **fase test/UAT/docs MURNI — NOL kode produksi**. Goal: membuktikan invariant multi-unit (single-active, `AssignmentUnit ∈ UserUnits`, B-06 anti-dobel, `ProtonKompetensi.Unit` 1:1) benar di **SQL Server riil (SQLEXPRESS)** — karena EF-InMemory tidak meng-enforce filtered-unique index, sehingga test InMemory existing bisa "hijau palsu". Codebase punya **harness SQL-real yang matang dan konsisten**: 6+ test class memakai pola `IClassFixture<XFixture>` + DB disposable `HcPortalDB_Test_<guid>@localhost\SQLEXPRESS` + `await ctx.Database.MigrateAsync()` + `EnsureDeletedAsync()` di dispose. Phase 404 tinggal menyalin pola ini menjadi `MultiUnitSqlFixture`, BUKAN reinvent.

Temuan kritis untuk perencanaan: **dua jenis enforcement berbeda**. (1) Single-active **CoachCoacheeMapping** DI-ENFORCE DB (filtered-unique `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`) → test bisa assert via `DbUpdateException` (SQL error 2601/2627) saat insert mapping aktif kedua. (2) Single-active **ProtonTrackAssignment** TIDAK punya filtered-unique index — hanya index non-unique `(CoacheeId, IsActive)`; invariant dijaga **app-level** (deactivate-before-create). Maka untuk PTA test WAJIB assert via **`Count(IsActive) == 1`** setelah memanggil write-path, bukan mengharap exception. Salah-asumsi di sini akan membuat test QA-03 yang menyesatkan.

Driving write-path: tiga helper validasi (`ValidateAssignmentUnitInUserUnits`, `CoacheeSectionMatchesCoach`, `FilterEligibleCoachees`) dan `ProtonDeliverableBootstrap.CreateProgressAsync` semuanya **static/pure dan drivable langsung di SQL-real**. Jalur bypass T1@X→T2@Y bisa di-drive lewat **`ProtonBypassService` end-to-end** (precedent lengkap: `ProtonBypassServiceTests` sudah konstruksi service dengan `FakeNotificationService`+`ProtonCompletionService`+`AuditLogService`+`NullLogger`). Sebaliknya, controller berat (`CoachCoacheeMappingAssign`/`Edit`/`Reactivate`, `ImportCoachCoacheeMapping`) butuh `_userManager.GetUserAsync(User)`+`HttpContext` — **TIDAK** drivable langsung; untuk jalur-jalur ini test harus mereplikasi write-pattern produksi di level DbContext (pola identik `AbandonGuardTests.RunAbandonGuardAsync`) ATAU assert constraint DB secara langsung. Ini bukan production change — ini pola test-replication yang sudah jadi konvensi proyek.

**Primary recommendation:** Buat `MultiUnitSqlFixture` (salin `OrgLabelMigrationFixture` + seed kanonik {X,Y}/coach/PROTON) → implement stub `SingleActive_invariant_is_sql_real_phase404` + isi 3 stub `UserUnitsBackfillIntegrationTests` → tambah Fact per write-path (assert: count single-active untuk PTA, `DbUpdateException` untuk mapping+UserUnits-primary, `ValidateAssignmentUnitInUserUnits`+bootstrap B-06 untuk QA-04) → drive bypass via `ProtonBypassService` (reuse precedent) → UAT 1 alur PROTON sekuensial cross-unit @5270 (snapshot→seed→restore) → HTML handoff (pola v31.0) + markdown D1=b → gate `dotnet test` (Category=Integration butuh SQLEXPRESS live).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Migration-chain apply (incl. 399 AddUserUnits) | Database / EF migrations pipeline | — | `db.Database.Migrate()` menjalankan DDL + backfill SQL riil; InMemory bypass pipeline (Pitfall utama fase) |
| Filtered-unique index enforcement (single-active mapping, one-primary UserUnits, ProtonKompetensi 1:1) | Database (SQL Server) | — | DB-only constraint; alasan eksistensi 404 |
| Single-active ProtonTrackAssignment | Application (controller/service logic) | Database (non-unique index only) | TIDAK ada filtered-unique untuk PTA; dijaga deactivate-before-create di kode |
| AssignmentUnit ∈ UserUnits validation | Application (static helper `ValidateAssignmentUnitInUserUnits`) | Database (read junction) | Pure-read helper, drivable SQL-real langsung |
| B-06 anti-dobel deliverable bootstrap | Application (`ProtonDeliverableBootstrap.CreateProgressAsync`) | Database (write progress) | Static helper, SaveChanges internal, drivable langsung |
| Bypass T1@X→T2@Y orchestration | Application (`ProtonBypassService`) | Database | Service drivable SQL-real (precedent `ProtonBypassServiceTests`) |
| UAT browser flow | Frontend (Razor @5270) + Application + Database | — | Manual/Playwright; snapshot→seed→restore DB app lokal terpisah |
| IT handoff doc | Static (HTML/markdown di `docs/`) | — | Self-contained styled report, pola v31.0 |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| xUnit | 2.9.3 | Test framework | Sudah dipakai seluruh `HcPortal.Tests` [VERIFIED: 402-VALIDATION.md] |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Provider SQL-real | `UseSqlServer(_cs)` di semua fixture integration [VERIFIED: AbandonGuardTests.cs:39] |
| SQL Server Express | localhost\SQLEXPRESS | Engine DB disposable + DB app lokal | Konvensi connstr fixture + `HcPortalDB_Dev` [VERIFIED: 6 fixture + SEED_WORKFLOW.md:16] |
| .NET SDK | 8.0.418 | Build/test runtime | [VERIFIED: 402-VALIDATION.md] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Playwright | (tests/ dir) | UAT browser otomatis | Bila UAT butuh runtime Razor/JS (lesson Phase 354) — opsional per D-11 "bila ada" |
| Microsoft.Extensions.Logging.Abstractions | (transitive) | `NullLogger<T>.Instance` | Konstruksi service di test (precedent `ProtonBypassServiceTests:36`) |
| `FakeNotificationService` (test helper existing) | — | Stub `INotificationService` | Konstruksi `ProtonBypassService`/`ProtonCompletionService` di SQL-real [VERIFIED: FakeNotificationService.cs] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `db.Database.Migrate()` | `EnsureCreated()` | DITOLAK by D-03 — `EnsureCreated` membangun schema dari model, **bypass** migration pipeline → tak membuktikan migration 399 apply bersih + tak menjamin filtered-unique index DDL terbentuk identik prod (Pitfall #2). |
| SQL-real fixture | EF InMemory | DITOLAK by domain — InMemory **tidak** enforce filtered-unique index → test hijau-palsu (alasan eksistensi fase, spec §10d). |
| Drive controller `CoachCoacheeMappingAssign` end-to-end | Replikasi write-pattern di level DbContext | Controller butuh `UserManager.GetUserAsync(User)`+`HttpContext` (tak ada di unit test tanpa `WebApplicationFactory`). Pola replikasi DbContext = konvensi proyek (`AbandonGuardTests`). |

**Installation:** Tidak ada paket baru. Semua sudah ada di `HcPortal.Tests.csproj`.

**Version verification:** xUnit 2.9.3 + EF Core SqlServer 8.0.0 [VERIFIED: 402-VALIDATION.md frontmatter + AbandonGuardTests.cs:39]. Tidak perlu `npm view` (proyek .NET). Tidak ada upgrade paket di fase test-only.

## Architecture Patterns

### System Architecture Diagram (alur eksekusi test SQL-real)

```
[dotnet test]
     │
     ├─ filter: Category!=Integration  ──►  CI SQL-less (skip semua SQL-real)
     │
     └─ filter: (default / Category=Integration)  ──►  butuh localhost\SQLEXPRESS LIVE
           │
           ▼
   [MultiUnitSqlFixture.InitializeAsync]  (sekali per test class, D-01)
           │
           ├─ new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs)
           │      _cs = Server=localhost\SQLEXPRESS;Database=HcPortalDB_Test_<guid>;...
           │
           ├─ await ctx.Database.MigrateAsync()   ◄── jalankan chain ~94 migration
           │      • 399 AddUserUnitsTable (filtered-unique IX_UserUnits_*)   [QA-01 bukti deploy]
           │      • CreateProtonTrackTable InsertData → master ProtonTracks tersedia GRATIS
           │      • catch → EnsureDeletedAsync (no DB leak) + XunitException "MIGRATION-CHAIN break"
           │
           └─ SeedCanonicalAsync(ctx)  (D-02)
                  • Bagian B; Unit X, Unit Y (org-tree)
                  • Worker coachee: UserUnits {X(primary), Y} dalam 1 Bagian
                  • Coach: UserUnits cross-unit (X & Y)
                  • ProtonKompetensi(unit=X)→Sub→Deliverable + (unit=Y)→Sub→Deliverable
           │
           ▼
   [Fact QA-03/QA-04]  ──►  drive write-path  ──►  assert
           │                                              │
           ├─ single-active MAPPING: insert 2nd active    ├─ Assert.Throws<DbUpdateException> (2601/2627)
           ├─ single-active PTA: call write-path           ├─ Assert.Equal(1, count IsActive)  ◄── BUKAN exception
           ├─ one-primary UserUnits: insert 2nd primary    ├─ Assert.Throws<DbUpdateException>
           ├─ AssignmentUnit∈UserUnits: helper             ├─ Assert.True/False ValidateAssignmentUnitInUserUnits
           ├─ B-06: CreateProgressAsync X then Y           ├─ Assert deliverable X & Y co-exist (tak saling skip)
           └─ bypass T1@X→T2@Y: ProtonBypassService        └─ Assert.Equal(1, active PTA) + cert histori utuh
           │
           ▼
   [MultiUnitSqlFixture.DisposeAsync]  ──►  EnsureDeletedAsync (drop HcPortalDB_Test_<guid>)
```

### Recommended Project Structure
```
HcPortal.Tests/
├── MultiUnitSqlFixture.cs            # (atau inline di file test) D-01 shared fixture + SeedCanonicalAsync
├── MultiUnitInvariantSqlTests.cs     # QA-03/QA-04 Facts [Trait Category=Integration]
│                                     #   (boleh dipecah per-invariant ATAU 1 class — Claude discretion)
├── CrossUnitAssignTests.cs           # ISI stub :105 SingleActive_invariant_is_sql_real_phase404 (D-09)
└── UserUnitsBackfillIntegrationTests.cs  # ISI 3 stub backfill (idempotent + primary + null)  ◄── BONUS, lihat Open Q1
docs/
├── milestone-v32.3/index.html        # D-13 HTML handoff IT (pola v31.0)
└── (markdown D1=b note — bisa di handoff atau file terpisah)        # D-14
docs/SEED_JOURNAL.md                  # +1 entry UAT 404 (D-12)
```

### Pattern 1: Disposable-DB SQL-real Fixture (SALIN PERSIS)
**What:** `IClassFixture` yang spin DB `HcPortalDB_Test_<guid>`, migrate, drop di dispose.
**When to use:** Semua Fact QA-01/03/04 (D-01/D-03/D-04).
**Example:**
```csharp
// Source: HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-66 (pola identik 6 fixture)
public class MultiUnitSqlFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;

    public MultiUnitSqlFixture()
    {
        // localhost-only + Integrated Security; SQLEXPRESS self-signed cert → TrustServerCertificate=True
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(Options);
            await ctx.Database.MigrateAsync();        // D-03: pipeline penuh — buktikan 399 AddUserUnits apply
            await SeedCanonicalAsync(ctx);            // D-02: {X,Y} 1 Bagian + coach cross-unit + PROTON chain
        }
        catch (Exception ex)
        {
            try { await using var c = new ApplicationDbContext(Options); await c.Database.EnsureDeletedAsync(); } catch { }
            throw new Xunit.Sdk.XunitException(
                $"Phase 404 MultiUnit fixture setup gagal saat MigrateAsync/seed DB {DbName}. " +
                $"Indikasi MIGRATION-CHAIN break (full chain run, incl 399), BUKAN tentu bug multi-unit. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.EnsureDeletedAsync();
    }
}
```

### Pattern 2: Assert single-active via DbUpdateException (MAPPING + UserUnits primary)
**What:** Untuk constraint yang DI-ENFORCE DB (filtered-unique), test menyisipkan baris kedua dan mengharap exception.
**When to use:** `CoachCoacheeMapping` aktif kedua (QA-03), UserUnits primary kedua (QA-01/QA-03).
**Example:**
```csharp
// Source: pola error-handling produksi CoachMappingController.cs:773-776 (2601/2627 = unique-violation)
[Fact]
public async Task SecondActiveMapping_SameCoachee_ViolatesFilteredUnique()
{
    await using var ctx = new ApplicationDbContext(_fixture.Options);
    ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoachId="co1", CoacheeId="cee1", IsActive=true, StartDate=DateTime.Today, AssignmentUnit="UnitX" });
    await ctx.SaveChangesAsync();
    ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoachId="co2", CoacheeId="cee1", IsActive=true, StartDate=DateTime.Today, AssignmentUnit="UnitY" });
    var ex = await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    // IX_CoachCoacheeMappings_CoacheeId_ActiveUnique → SQL 2601/2627. InMemory TIDAK akan throw (alasan SQL-real).
}
```

### Pattern 3: Assert single-active PTA via COUNT (BUKAN exception)
**What:** `ProtonTrackAssignment` single-active tidak punya filtered-unique index → drive write-path lalu hitung.
**When to use:** QA-03 jalur T1@X→T2@Y (bypass/reassign).
**Example:**
```csharp
// Rationale: ApplicationDbContext.cs:393 hanya HasIndex((CoacheeId,IsActive)) NON-unique.
// Single-active dijaga app-level (deactivate-before-create), bukan DB. Maka:
var activeCount = await ctx.ProtonTrackAssignments.CountAsync(a => a.CoacheeId == coacheeId && a.IsActive);
Assert.Equal(1, activeCount);   // setelah bypass T1→T2: T1 deactivated, T2 active → tepat 1
```

### Pattern 4: Drive bypass T1@X→T2@Y via ProtonBypassService (precedent lengkap)
**What:** Konstruksi service dengan dependency fake + seed PROTON chain, panggil entry-point bypass.
**When to use:** QA-03 single-active cross-unit end-to-end + cert histori.
**Example:**
```csharp
// Source: HcPortal.Tests/ProtonBypassServiceTests.cs:30-72 (NewBypassSvc + SeedDeliverablesAsync)
private static ProtonBypassService NewBypassSvc(ApplicationDbContext ctx, FakeNotificationService? notif = null)
{
    var fake = notif ?? new FakeNotificationService();
    return new(ctx,
        new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance, fake, new AuditLogService(ctx)),
        fake, new AuditLogService(ctx), NullLogger<ProtonBypassService>.Instance);
}
// SeedDeliverablesAsync(ctx, trackId, unit, count): Kompetensi(unit)→Sub→N Deliverable → drive CreateProgressAsync
```

### Pattern 5: B-06 anti-dobel + ProtonKompetensi.Unit 1:1 (QA-04)
**What:** Drive `ProtonDeliverableBootstrap.CreateProgressAsync` untuk unit X lalu Y dengan CoacheeId sama.
**When to use:** QA-04.
**Example:**
```csharp
// Source: Helpers/ProtonDeliverableBootstrap.cs:55-65 (B-06 guard: skip deliverable yg SUDAH ada progress utk coachee)
// Test: bootstrap unit X (track T1), lalu unit Y (track T2) untuk coachee yang SAMA.
// Assert: deliverable unit Y TETAP dibuat (tak ter-skip oleh existing unit-X progress) — cross-unit tak saling skip.
// Assert ProtonKompetensi.Unit 1:1: insert 2 ProtonDeliverableProgress dgn (ProtonTrackAssignmentId, ProtonDeliverableId)
//   identik → DbUpdateException (IX unique :429). Beda deliverableId → OK.
```

### Anti-Patterns to Avoid
- **Mengharap `DbUpdateException` untuk single-active ProtonTrackAssignment:** TIDAK ada filtered-unique index untuk PTA — assert via count. Salah-asumsi membuat test QA-03 menyesatkan (false negative).
- **`EnsureCreated()`:** bypass migration pipeline → tak membuktikan 399 apply (melanggar D-03).
- **Drive controller berat langsung (`CoachCoacheeMappingAssign`):** butuh `UserManager`/`HttpContext`. Pakai pola replikasi DbContext atau service-layer (`ProtonBypassService`).
- **Reuse DB app lokal `HcPortalDB_Dev` untuk fixture xUnit:** fixture WAJIB DB disposable terpisah (D-04). DB app lokal hanya untuk UAT (snapshot/restore).
- **Lupa `[Trait("Category","Integration")]`:** tanpa ini test SQL-real tak bisa di-skip di CI SQL-less. Semua 30 SQL-real test existing memakainya.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| DB disposable lifecycle | Manual create/drop SQL script | Pola `IAsyncLifetime` fixture existing (6 contoh) | Sudah handle mid-migration cleanup + XunitException diagnostik |
| Konstruksi `ProtonBypassService` | Mock UserManager/HttpContext | `NewBypassSvc` helper + `FakeNotificationService` | Precedent `ProtonBypassServiceTests` 1:1 |
| Seed PROTON chain | Insert manual kompetensi/sub/deliverable ad-hoc | `SeedDeliverablesAsync` pola (`ProtonBypassServiceTests:54-66`) | Chain Kompetensi→Sub→Deliverable + return ids |
| Drive deliverable bootstrap | Replikasi logic B-06 di test | `ProtonDeliverableBootstrap.CreateProgressAsync` (static, SaveChanges internal) | Helper produksi langsung — test = bukti kontrak nyata |
| Validasi AssignmentUnit∈UserUnits | Query junction manual | `CoachMappingController.ValidateAssignmentUnitInUserUnits` (static, public) | Helper produksi langsung |
| HTML handoff dari nol | Tulis CSS/struktur baru | Salin `docs/milestone-v31.0/index.html` | Self-contained styled report, struktur header/verdict/grid sudah jadi |
| Snapshot/restore DB UAT | Script ad-hoc | `sqlcmd` BACKUP/RESTORE per SEED_WORKFLOW §5 | SOP terbukti, journal audit-trail |

**Key insight:** Hampir SEMUA mekanik fase 404 sudah ada sebagai pola/precedent di codebase. Nilai fase = **mengaplikasikan pola yang tepat** ke dimensi multi-unit, BUKAN membangun harness baru. Risiko terbesar = salah memilih strategi assert (exception vs count) untuk enforcement DB vs app-level.

## Runtime State Inventory

> Fase test/UAT/docs murni — NOL rename/refactor produksi. Tidak ada perubahan string runtime.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — fase tidak menyentuh/me-rename data produksi. Fixture xUnit pakai DB disposable; UAT pakai snapshot→restore (revert penuh). | none |
| Live service config | None — verified: tidak ada perubahan controller/service/config produksi. | none |
| OS-registered state | None. | none |
| Secrets/env vars | None — connstr fixture hard-coded localhost Integrated Security (no secrets). | none |
| Build artifacts | Test DLL `HcPortal.Tests.dll` akan berisi test baru (normal `dotnet test` rebuild). Tidak ada egg-info/stale package. | rebuild via `dotnet test` (otomatis) |

**Nothing found in 4/5 categories:** Verified via grep produksi (CONTEXT D-13/Boundary: "tak ada schema/write produksi").

## Common Pitfalls

### Pitfall 1: Mengira ProtonTrackAssignment single-active di-enforce DB
**What goes wrong:** Test mengharap `DbUpdateException` saat menyisipkan PTA aktif kedua untuk coachee yang sama → test tak pernah throw → either false-pass (kalau hanya cek "tak ada baris ganda" lewat jalur lain) atau test ditulis salah.
**Why it happens:** `CoachCoacheeMapping` PUNYA filtered-unique (`:333-336`), tetapi `ProtonTrackAssignment` HANYA punya index non-unique `(CoacheeId, IsActive)` (`:393`). Single-active PTA dijaga app-level (deactivate-before-create di `CoachCoacheeMappingAssign:718-726`, `MoveAssignmentAsync:413-414`).
**How to avoid:** QA-03 PTA → assert `CountAsync(a.CoacheeId==id && a.IsActive) == 1` SETELAH memanggil write-path produksi (bypass service / replikasi pattern). Mapping & UserUnits-primary → boleh assert via `DbUpdateException`.
**Warning signs:** Fact PTA yang memakai `Assert.ThrowsAsync<DbUpdateException>` tanpa memanggil write-path.

### Pitfall 2: EnsureCreated menyembunyikan kegagalan migration 399
**What goes wrong:** Schema terbentuk dari model (filtered-unique index mungkin ada), tetapi migration 399 `AddUserUnitsTable` + backfill SQL TIDAK pernah dieksekusi → de-risk deploy (bonus D-03) gagal total tanpa diketahui.
**Why it happens:** `EnsureCreated()` membaca model snapshot, bukan migration DDL.
**How to avoid:** WAJIB `await ctx.Database.MigrateAsync()` (D-03). Catch block lempar `XunitException` "MIGRATION-CHAIN break" agar kegagalan migration terbaca jelas (pola 6 fixture).
**Warning signs:** Fixture memanggil `EnsureCreated`.

### Pitfall 3: ApplicationUser TIDAK punya nav-collection UserUnits
**What goes wrong:** `coachee.UserUnits` → CS1061 compile error; atau pakai `u.UserUnits.Any(...)` di query.
**Why it happens:** Junction `UserUnits` di-query via `_context.UserUnits.Where(uu => uu.UserId == ...)`, BUKAN nav-prop (lihat helper `:56-59`; Pitfall #1 di 400/402 notes).
**How to avoid:** Query `ctx.UserUnits` correlated. Reuse helper `ValidateAssignmentUnitInUserUnits` yang sudah benar.
**Warning signs:** `.UserUnits` diakses dari instance `ApplicationUser`.

### Pitfall 4: Test SQL-real gagal saat SQLEXPRESS tidak hidup → salah-diagnosa sebagai bug
**What goes wrong:** `dotnet test` lokal gagal massal karena SQLBrowser/SQLEXPRESS mati (NTLM loopback), bukan karena kode salah.
**Why it happens:** Fixture butuh `localhost\SQLEXPRESS` LIVE; reference memory `reference_local_e2e_sql_env_fix` mencatat start SQLBrowser + shared-memory conn.
**How to avoid:** Gate D-15 jalankan di mesin dengan SQLEXPRESS hidup. CI SQL-less filter `Category!=Integration`. Catch-block XunitException sudah membedakan "migration-chain break" dari "bug multi-unit".
**Warning signs:** Semua 30 Integration test gagal bersamaan dengan connection error.

### Pitfall 5: Seed UAT bocor / DB app lokal tak ter-restore
**What goes wrong:** Seed temporary {X,Y}/coach/PROTON nempel di `HcPortalDB_Dev` lewat session → mengotori test/UAT berikutnya.
**Why it happens:** Lupa restore (SEED_WORKFLOW Step 5) — sukses ATAU gagal.
**How to avoid:** snapshot→seed→restore + journal `cleaned` (D-12). Recipe terbukti di SEED_JOURNAL (402-03/04, 400-01, 403-02).
**Warning signs:** Journal entry status `active` tertinggal; `UserUnits` count ≠ 6 baseline setelah UAT.

## Code Examples

### Stub anchor yang WAJIB diisi (D-09)
```csharp
// Source: HcPortal.Tests/CrossUnitAssignTests.cs:103-105 (CURRENT — body kosong [Skip])
[Fact(Skip = "deferred to Phase 404 QA-03 — SQL-real single-active invariant")]
public void SingleActive_invariant_is_sql_real_phase404() { }
// → 404: ubah jadi [Trait("Category","Integration")] + IClassFixture<MultiUnitSqlFixture>,
//   ATAU pindahkan implement ke MultiUnitInvariantSqlTests dan hapus stub (Claude discretion D-01).
//   Yang penting: invariant single-active TER-COVER di SQL-real, stub tak lagi [Skip] kosong.
```

### Stub backfill SQL-real yang juga menanti 404 (BONUS — lihat Open Q1)
```csharp
// Source: HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs:72-91 (3 Fact [Skip] "diisi plan 02/404")
// Backfill_UserWithNonNullUnit_HasExactlyOnePrimaryRow / _WithNullUnit_HasZeroRows / _RerunIsIdempotent
// Fixture UserUnitsBackfillFixture sudah ada (Migrate). 404 tinggal seed Users + assert.
```

### Filter command (gate D-15)
```bash
# Full suite (butuh SQLEXPRESS live) — baseline ~547/0/6, target: +N pass, 0 fail, stub [Skip] berkurang
dotnet test
# CI SQL-less / cepat tanpa SQLEXPRESS:
dotnet test --filter "Category!=Integration"
# Hanya suite multi-unit SQL-real baru:
dotnet test --filter "FullyQualifiedName~MultiUnitInvariantSqlTests|FullyQualifiedName~CrossUnitAssignTests"
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Test multi-unit InMemory (logic) | + dimensi SQL-real (DB-constraint) | Phase 404 (now) | Filtered-unique index akhirnya ter-uji; hijau-palsu InMemory tertutup |
| Single-active deferred [Skip] | Implement SQL-real | Phase 404 | Carry 402 ditutup; stub :105 hidup |

**Deprecated/outdated:** Tidak ada. Pola fixture SQL-real masih konvensi aktif (30 test class, terbaru Phase 399 `UserUnitsBackfillFixture`).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Baseline suite ~547/0/6 (dari 402-VALIDATION 2026-06-21) masih akurat saat 404 dieksekusi | D-15 gate | Rendah — angka pasti diverifikasi saat run; "tak regresi" tetap valid sebagai prinsip. Jika 402 re-review/secure (Deferred) menambah test, baseline naik. |

**Catatan:** Semua klaim teknis lain (struktur fixture, index lines, helper signatures, write-path, B-06 logic, bypass precedent) **[VERIFIED: codebase]** — dibaca langsung dari file. Tabel ini hanya 1 baris karena hanya angka baseline yang bergantung pada state runtime saat eksekusi.

## Open Questions

1. **Apakah 404 juga mengisi 3 stub `UserUnitsBackfillIntegrationTests` (backfill SQL-real)?**
   - What we know: 3 Fact `[Skip]` ber-pesan **"diisi plan 02/404"** [VERIFIED: UserUnitsBackfillIntegrationTests.cs:72-91]. Fixture `UserUnitsBackfillFixture` sudah ada (migrate). Backfill = bagian migration 399 (QA-01 menyebut "fixture {X,Y} 1 Bagian"); membuktikan backfill bersih = sejalan bonus D-03 "de-risk deploy 399".
   - What's unclear: CONTEXT hanya eksplisit menyebut stub `CrossUnitAssignTests:105` (D-09). Backfill stub tidak disebut eksplisit tetapi pesannya menunjuk 404.
   - Recommendation: **Planner masukkan 3 stub backfill ke scope 404** (low-cost, fixture siap, menutup [Skip] residual + memperkuat bukti QA-01 migration 399). Jika ditolak, 6 [Skip] baseline tetap (3 backfill + lainnya). Konfirmasi ringan saat planning.

2. **Granularitas test class: 1 class banyak Fact vs pisah per-invariant?**
   - What we know: D-01 mensyaratkan share `MultiUnitSqlFixture`; granularitas = Claude's Discretion.
   - What's unclear: Pemecahan optimal untuk keterbacaan.
   - Recommendation: 1 `MultiUnitSqlFixture` shared; pisah test class per-tema (single-active / unit-membership / B-06) bila >8 Fact, semuanya `IClassFixture<MultiUnitSqlFixture>`. xUnit instansiasi fixture sekali per class — jika dipecah ke N class, fixture jalan N kali (N DB disposable). Trade-off waktu vs isolasi; untuk ~94-migration chain, **pertimbangkan 1 class** agar `MigrateAsync` (mahal) jalan sekali. (Planner putuskan.)

3. **Cert histori per-unit "tersimpan utuh" — di mana cert tersimpan saat cross-unit T1@X→T2@Y?**
   - What we know: D1=b = atribusi primary (no kolom unit-at-issue). Cert nomor pada `AssessmentSession.NomorSertifikat` (filtered-unique `:229-231`). PROTON final = `ProtonFinalAssessment` (unique per assignment `:443`).
   - What's unclear: Apakah UAT mengecek cert per-unit lewat `ProtonFinalAssessment` (per-assignment, jadi per-unit utuh) atau `AssessmentSession`.
   - Recommendation: UAT verifikasi histori via `ProtonFinalAssessment`/`ProtonTrackAssignment` per-track (1:1 per assignment → cert T1 dan T2 co-exist sebagai 2 assignment inactive+active). Dokumentasikan D1=b: laporan agregat atribusi ke primary unit, BUKAN cert hilang.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test | ✓ (asumsi mesin dev) | 8.0.418 | — |
| SQL Server Express (`localhost\SQLEXPRESS`) | Fixture SQL-real + DB app UAT | ✓ (HcPortalDB_Dev live) | SQLEXPRESS | **TIDAK ada** — SQL-real test WAJIB; tanpa ini gate D-15 tak bisa dijalankan (CI bisa skip via `Category!=Integration` tapi bukti QA-01/03/04 hilang) |
| SQLBrowser service | Koneksi named-instance | perlu START | — | shared-memory/lpc conn override (ref memory `reference_local_e2e_sql_env_fix`) |
| sqlcmd | snapshot/restore UAT | ✓ | flags `-S ... -E` (SEED_WORKFLOW) / `-C -I` (CONTEXT D-04) | — |
| Playwright | UAT browser (opsional "bila ada") | ✓ (`tests/` dir) | — | UAT manual browser @5270 |
| dotnet ef CLI | TIDAK diperlukan (no migration baru) | n/a | — | — |

**Missing dependencies with no fallback:**
- SQL Server Express harus LIVE saat eksekusi gate D-15 (untuk membuktikan QA-01/03/04). Ini bukan blocker baru — mesin dev sudah punya `HcPortalDB_Dev` di SQLEXPRESS [VERIFIED: SEED_WORKFLOW.md:16 + 30 Integration test existing].

**Missing dependencies with fallback:**
- Playwright UAT → fallback UAT manual browser (D-11 "Playwright bila ada").

## Validation Architecture

> nyquist_validation: true (config) — section WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (.NET 8.0.418) + Playwright (UAT opsional) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (tidak ada `xunit.runner.json` — default parallelism per-class; tiap fixture DB unik by GUID → aman paralel) |
| Quick run command | `dotnet test --filter "FullyQualifiedName~MultiUnitInvariantSqlTests\|FullyQualifiedName~CrossUnitAssignTests"` |
| Full suite command | `dotnet test` (butuh SQLEXPRESS live) |
| CI SQL-less command | `dotnet test --filter "Category!=Integration"` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| QA-01 | Migration 399 apply + fixture {X,Y}/coach/PROTON di SQL-real | integration | `dotnet test --filter "FullyQualifiedName~MultiUnitSqlFixture"` (via class yang pakai fixture) | ❌ Wave 0 (`MultiUnitSqlFixture` baru) |
| QA-01 | Backfill 1 primary-row/pekerja idempotent SQL-real | integration | `dotnet test --filter "FullyQualifiedName~UserUnitsBackfillIntegrationTests"` | ⚠️ fixture ada, 3 Fact `[Skip]` (Open Q1) |
| QA-03 | Single-active `CoachCoacheeMapping` (filtered-unique → DbUpdateException) | integration | (Fact di MultiUnitInvariantSqlTests) | ❌ Wave 0 |
| QA-03 | Single-active `ProtonTrackAssignment` (count==1 setelah bypass T1@X→T2@Y) | integration | (Fact, drive `ProtonBypassService`) | ❌ Wave 0 |
| QA-03 | Single-active lintas Reactivate + Import-reactivate | integration | (Fact, replikasi reactivate pattern) | ❌ Wave 0 |
| QA-03 | Stub `SingleActive_invariant_is_sql_real_phase404` hidup | integration | `dotnet test --filter "FullyQualifiedName~CrossUnitAssignTests"` | ⚠️ stub `[Skip]` :105 (D-09) |
| QA-04 | `AssignmentUnit ∈ coachee.UserUnits` tiap write | integration | (Fact, `ValidateAssignmentUnitInUserUnits`) | ❌ Wave 0 |
| QA-04 | B-06 anti-dobel `CreateProgressAsync` cross-unit X vs Y tak saling skip | integration | (Fact, `ProtonDeliverableBootstrap`) | ❌ Wave 0 |
| QA-04 | `ProtonKompetensi.Unit` 1:1 (`(PTAId,DeliverableId)` unique) | integration | (Fact, insert duplikat → DbUpdateException) | ❌ Wave 0 |
| QA-02 | UAT PROTON sekuensial cross-unit @5270 + cert histori + coach multi-unit view | manual / Playwright | `cd tests && npx playwright test` (bila spec dibuat) ATAU manual @5270 | manual-only (browser) |
| QA-02 | Docs D1=b (HTML handoff + markdown) | manual review | n/a | ❌ Wave 0 (`docs/milestone-v32.3/index.html`) |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~MultiUnitInvariantSqlTests"` (subset SQL-real baru)
- **Per wave merge:** `dotnet test` (full suite, SQLEXPRESS live) — pastikan 0 regresi baseline ~547
- **Phase gate (D-15):** `dotnet build` 0 error + `dotnet test` hijau penuh + UAT browser sign-off

### Wave 0 Gaps
- [ ] `HcPortal.Tests/MultiUnitSqlFixture.cs` (atau inline) — `IClassFixture` + `SeedCanonicalAsync` {X,Y}/coach/PROTON (QA-01)
- [ ] `HcPortal.Tests/MultiUnitInvariantSqlTests.cs` — Fact QA-03/QA-04 (single-active, unit-membership, B-06, 1:1)
- [ ] `HcPortal.Tests/CrossUnitAssignTests.cs:105` — implement stub `SingleActive_invariant_is_sql_real_phase404` (D-09)
- [ ] `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs` — isi 3 Fact `[Skip]` (Open Q1, opsional tapi direkomendasi)
- [ ] `docs/milestone-v32.3/index.html` — handoff IT (pola v31.0)
- [ ] `docs/` markdown D1=b note + `docs/SEED_JOURNAL.md` entry UAT 404
- [ ] (opsional) `tests/e2e/*-404.spec.ts` — Playwright UAT PROTON sekuensial cross-unit

*Framework install: none — `dotnet test` infra siap (xUnit + SqlServer provider sudah referenced).*

## Security Domain

> security_enforcement absent di config → enabled. Fase test/UAT/docs — surface keamanan minimal, tetapi invariant yang di-test ADALAH kontrol keamanan data-integrity.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Fase tidak menyentuh auth; test memakai DbContext langsung (no login flow) |
| V3 Session Management | no | — |
| V4 Access Control | indirect | Invariant `AssignmentUnit ∈ UserUnits` + single-active = guard data-tenancy multi-unit; 404 MEMBUKTIKAN kontrol ini, tidak membuatnya |
| V5 Input Validation | indirect | Helper `ValidateAssignmentUnitInUserUnits` = validasi membership; di-test (bukan ditambah) |
| V6 Cryptography | no | — |
| V11 Business Logic | yes (test) | Single-active + B-06 = integritas business-logic; 404 = bukti regression-proof di SQL-real |

### Known Threat Patterns for {SQL-real integration test}

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Connstr berisi secret | Information Disclosure | Connstr fixture = localhost Integrated Security, NO secrets/env (pola 6 fixture) — pertahankan |
| DB disposable leak (test data residu) | — (kebersihan) | `EnsureDeletedAsync` di dispose + catch mid-migration (pola existing) |
| Seed UAT bocor ke DB app | Tampering (integritas DB lokal) | SEED_WORKFLOW snapshot→restore + journal (D-12) |
| Expose `dbEx.Message` mentah | Information Disclosure | (produksi sudah mitigasi `:778-779`) — test tak boleh menambah surface; assert pada type/inner saja |

**Catatan keamanan:** 404 TIDAK menambah surface produksi. Satu-satunya risiko keamanan adalah jika perencanaan tergoda mengekstraksi seam dari controller (Risiko R-1 di bawah) — itu akan memicu re-review+secure. Hindari.

## Risiko Khusus (flag eksplisit per instruksi)

### R-1: Seam-extraction temptation (controller tak drivable → produksi change)
**Trade-off:** Jalur `CoachCoacheeMappingAssign`/`Edit`/`Reactivate` + `ImportCoachCoacheeMapping` butuh `UserManager.GetUserAsync(User)`+`HttpContext` — TIDAK drivable di unit test tanpa `WebApplicationFactory`. Godaan: ekstrak "core" static seam (seperti yang dilakukan 402 untuk `FilterEligibleCoachees`/`CoerceCoachUnitScope`).
**Mengapa ini risiko:** Ekstraksi seam = **production change** → memicu policy re-run `/gsd-code-review 404` + `/gsd-secure-phase 404` (sama seperti carry 402). Melanggar boundary "NOL kode produksi" fase 404.
**Mitigasi (rekomendasi):** JANGAN ekstrak seam baru. Untuk jalur controller, gunakan SALAH SATU:
  (a) **Assert constraint DB langsung** — sisipkan baris (mis. 2 mapping aktif) → `DbUpdateException`. Constraint single-active mapping di-enforce DB, jadi cukup membuktikan DB menolak, terlepas dari jalur controller mana yang memicu (Assign/Edit/Import/Reactivate semua tunduk index yang SAMA).
  (b) **Replikasi write-pattern di level DbContext** (pola `AbandonGuardTests.RunAbandonGuardAsync` — replikasi UPDATE-guard identik produksi). Ini test-code, bukan production change.
  (c) Untuk jalur dengan helper static yang SUDAH ada (`ValidateAssignmentUnitInUserUnits`, `ProtonDeliverableBootstrap`, `ProtonBypassService`) — drive langsung.
**Verdict:** Semua 6 jalur-write (D-05) BISA di-cover tanpa seam baru via kombinasi (a)+(b)+(c). Jika planner menemukan satu invariant yang BENAR-BENAR tak bisa tanpa seam, **flag sebagai keputusan eksplisit** (trade-off: seam → re-trigger review+secure 404) — jangan diam-diam ekstrak.

### R-2: Single-active mapping di-enforce DB, tapi jalur Assign/Edit/Import/Reactivate adalah jalur kode BERBEDA
**Trade-off:** D-05 minta assert "di SEMUA jalur-write". Constraint DB-level (filtered-unique mapping) menolak baris aktif kedua TERLEPAS jalur. Tetapi single-active PTA (app-level) HARUS di-test per-jalur karena logic deactivate-before-create bisa beda antar method.
**Mitigasi:** Untuk mapping → 1 Fact membuktikan DB-enforcement cukup mewakili semua jalur (constraint sama). Untuk PTA → test minimal jalur Assign (`:718-726`) + bypass (`MoveAssignmentAsync:413`) + reactivate (`:1221-1225`) via count, karena tiga method ini punya logic deactivate berbeda. Import-reactivate berbagi jalur reactivate. Planner pertimbangkan cakupan minimal-tapi-representatif (CONTEXT §specifics: "sempit-tapi-dalam").

## Sources

### Primary (HIGH confidence — dibaca langsung di codebase)
- `HcPortal.Tests/AbandonGuardTests.cs:25-145` — pola fixture SQL-real + replikasi write-pattern
- `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs:24-95` — pola `Migrate()` + seed + assert (template `MultiUnitSqlFixture`)
- `HcPortal.Tests/EssayFinalizeRecomputeTests.cs:19-70`, `ImageCleanupIntegrationTests.cs:31-60`, `UserUnitsBackfillIntegrationTests.cs` (full) — varian pola fixture
- `HcPortal.Tests/ProtonBypassServiceTests.cs:1-80` — precedent drive `ProtonBypassService` SQL-real (`NewBypassSvc` + `SeedDeliverablesAsync`)
- `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-70` — `ProtonCompletionFixture` (Migrate seed ProtonTracks)
- `HcPortal.Tests/CrossUnitAssignTests.cs:1-106` — stub :105 (D-09) + InMemory CXU existing
- `Data/ApplicationDbContext.cs:333-336, 350-358, 393, 429, 443, 452-457` — peta filtered-unique index
- `Controllers/CoachMappingController.cs:52-90, 549-816, 1159-1247` — helper static + write-path Assign/Reactivate
- `Helpers/ProtonDeliverableBootstrap.cs` (full) — B-06 guard
- `Services/ProtonBypassService.cs:400-470` — `MoveAssignmentAsync` (T1→T2 single-active)
- `Migrations/20260223060707_CreateProtonTrackTable.cs` — InsertData ProtonTracks (master gratis via Migrate)
- `docs/SEED_WORKFLOW.md` (full), `docs/DEV_WORKFLOW.md:1-26` (§1 port 5270), `docs/SEED_JOURNAL.md` (recipe 402-03/04, 400-01, 403-02)
- `docs/milestone-v31.0/index.html:1-40` — template HTML handoff
- `.planning/phases/402-coaching-cross-unit-mapping/402-VALIDATION.md` — carry single-active deferred + baseline 547/0/6
- `.planning/ROADMAP.md:174-229` + `.planning/REQUIREMENTS.md:46-49` — QA-01..04 acceptance

### Secondary (MEDIUM)
- `.planning/config.json` — nyquist_validation:true, use_worktrees:true
- MEMORY topic `reference_local_e2e_sql_env_fix` — SQLBrowser/shared-memory conn (Pitfall 4)

### Tertiary (LOW)
- None — semua klaim ter-verifikasi langsung di file.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — paket & versi diverifikasi (xUnit 2.9.3, EF SqlServer 8.0.0); fixture pola 6 contoh
- Architecture: HIGH — pola fixture + write-path + index lines dibaca langsung
- Pitfalls: HIGH — Pitfall 1 (PTA tak punya filtered-unique) diverifikasi line-by-line `ApplicationDbContext.cs:393` vs `:333-336`

**Research date:** 2026-06-21
**Valid until:** 2026-07-21 (stabil — fase test-only; satu-satunya volatilitas = baseline count suite, diverifikasi saat run)
