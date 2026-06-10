# Self-Heal Seed (F1 Org Level + F2 ProtonTrack) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tambah 2 rutin self-heal idempotent ke `Data/SeedData.cs` yang jalan tiap startup — F1 normalisasi `OrganizationUnits.Level` by tree-depth, F2 re-seed 6 ProtonTrack yang hilang.

**Architecture:** Dua static method baru di `SeedData` (pola sama `SeedOrganizationUnitsAsync` existing), dipanggil dari `InitializeAsync`. Tiap method: load → hitung beda → satu `SaveChangesAsync` (atomik via EF implicit-transaction) → log. Error di-catch + return (app tetap start, decision A). No migration, no UI. Test pakai EF InMemory (provider test, bukan SQL).

**Tech Stack:** ASP.NET Core, EF Core (SQL Server prod / InMemory test), xUnit.

**Spec:** `docs/superpowers/specs/2026-06-10-org-level-track-selfheal-design.md`

---

## File Structure

| File | Tanggung jawab | Aksi |
|---|---|---|
| `Data/SeedData.cs` | 2 method self-heal baru + wiring di `InitializeAsync` | Modify |
| `HcPortal.Tests/CapturingLogger.cs` | Test double `ILogger<T>` yang nyimpen entry buat assert warning | Create |
| `HcPortal.Tests/SeedProtonTracksTests.cs` | Unit test F2 (5 [Fact]) | Create |
| `HcPortal.Tests/NormalizeOrganizationLevelsTests.cs` | Unit test F1 (7 [Fact]) | Create |

**Fakta model (terverifikasi):**
- `OrganizationUnit` (`Models/OrganizationUnit.cs`): `int Id`, `string Name`, `int? ParentId`, `int Level`, `int DisplayOrder`, `bool IsActive`. DbSet `OrganizationUnits`.
- `ProtonTrack` (`Models/ProtonModels.cs:8`): `int Id`, `string TrackType`, `string TahunKe`, `string DisplayName`, `int Urutan`. DbSet `ProtonTracks`. Unique `(TrackType,TahunKe)`.
- `ProtonKompetensi.ProtonTrackId` (int) — DbSet `ProtonKompetensiList`. `ProtonTrackAssignment.ProtonTrackId` (int) — DbSet `ProtonTrackAssignments`.
- `InitializeAsync(IServiceProvider serviceProvider, IWebHostEnvironment environment)` — sudah punya `serviceProvider` (ambil `ILogger<SeedData>` dari sini).

---

## Task 1: Test helper — `CapturingLogger<T>`

**Files:**
- Create: `HcPortal.Tests/CapturingLogger.cs`

- [ ] **Step 1: Tulis helper**

```csharp
using Microsoft.Extensions.Logging;

namespace HcPortal.Tests;

/// <summary>ILogger<T> test-double: simpan tiap entry buat assert (mis. ada Warning).</summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
```

- [ ] **Step 2: Build test project**

Run: `dotnet build HcPortal.Tests`
Expected: 0 error (helper compiles; belum dipakai).

- [ ] **Step 3: Commit**

```bash
git add HcPortal.Tests/CapturingLogger.cs
git commit -m "test: add CapturingLogger test double for self-heal seed tests"
```

---

## Task 2: F2 — `SeedProtonTracksAsync`

**Files:**
- Create: `HcPortal.Tests/SeedProtonTracksTests.cs`
- Modify: `Data/SeedData.cs` (tambah method `SeedProtonTracksAsync`)

- [ ] **Step 1: Tulis 5 test (failing)**

Buat `HcPortal.Tests/SeedProtonTracksTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HcPortal.Tests;

public class SeedProtonTracksTests
{
    private static ApplicationDbContext NewCtx() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Empty_SeedsSixTracks()
    {
        using var ctx = NewCtx();
        await SeedData.SeedProtonTracksAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(6, ctx.ProtonTracks.Count());
        Assert.Equal("Panelman - Tahun 1",
            ctx.ProtonTracks.Single(t => t.TrackType == "Panelman" && t.TahunKe == "Tahun 1").DisplayName);
        Assert.Equal(4,
            ctx.ProtonTracks.Single(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 1").Urutan);
    }

    [Fact]
    public async Task Idempotent_RunTwice_StillSix()
    {
        using var ctx = NewCtx();
        await SeedData.SeedProtonTracksAsync(ctx, new CapturingLogger<SeedData>());
        await SeedData.SeedProtonTracksAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(6, ctx.ProtonTracks.Count());
    }

    [Fact]
    public async Task Partial_FillsMissingOnly()
    {
        using var ctx = NewCtx();
        ctx.ProtonTracks.AddRange(
            new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 1", DisplayName = "Panelman - Tahun 1", Urutan = 1 },
            new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 3", DisplayName = "Operator - Tahun 3", Urutan = 6 });
        await ctx.SaveChangesAsync();

        await SeedData.SeedProtonTracksAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(6, ctx.ProtonTracks.Count());
    }

    [Fact]
    public async Task Preserve_ExistingCustomDisplayName_NotOverwritten()
    {
        using var ctx = NewCtx();
        ctx.ProtonTracks.Add(new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 1", DisplayName = "CUSTOM", Urutan = 99 });
        await ctx.SaveChangesAsync();

        await SeedData.SeedProtonTracksAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(6, ctx.ProtonTracks.Count());
        Assert.Equal("CUSTOM",
            ctx.ProtonTracks.Single(t => t.TrackType == "Panelman" && t.TahunKe == "Tahun 1").DisplayName);
    }

    [Fact]
    public async Task OrphanChild_NotCorrupted_WarningLogged()
    {
        using var ctx = NewCtx();
        ctx.ProtonKompetensiList.Add(new ProtonKompetensi { ProtonTrackId = 777, NamaKompetensi = "X", Bagian = "RFCC", Unit = "U" });
        await ctx.SaveChangesAsync();
        var logger = new CapturingLogger<SeedData>();

        await SeedData.SeedProtonTracksAsync(ctx, logger);

        Assert.Equal(6, ctx.ProtonTracks.Count());
        Assert.Equal(777, ctx.ProtonKompetensiList.Single().ProtonTrackId); // unchanged
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }
}
```

> Catatan: kalau `ProtonKompetensi` punya property required selain di atas, set juga (InMemory tak enforce NOT NULL, tapi compile butuh nama property benar — cek `Models/ProtonModels.cs:27`). `NamaKompetensi`/`Bagian`/`Unit` adalah string; sesuaikan nama persis bila beda.

- [ ] **Step 2: Run — verify FAIL**

Run: `dotnet test --filter "FullyQualifiedName~SeedProtonTracksTests"`
Expected: FAIL kompilasi — `SeedData.SeedProtonTracksAsync` belum ada.

- [ ] **Step 3: Implement `SeedProtonTracksAsync`**

Di `Data/SeedData.cs`, tambah `using Microsoft.Extensions.Logging;` di atas (kalau belum ada), lalu tambah method ini di dalam class `SeedData`:

```csharp
/// <summary>
/// Self-heal: pastikan 6 ProtonTrack master ada (Panelman/Operator × Tahun 1-3).
/// Insert-if-missing by (TrackType,TahunKe) — idempotent, preserve existing.
/// Track aslinya di-seed migration CreateProtonTrackTable (sekali); ini bikin tahan-banting
/// kalau baris hilang (mis. restore DB). Tak memperbaiki ref ProtonTrackId lama yang dangling.
/// </summary>
public static async Task SeedProtonTracksAsync(ApplicationDbContext context, ILogger logger)
{
    try
    {
        var expected = new[]
        {
            new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 1", DisplayName = "Panelman - Tahun 1", Urutan = 1 },
            new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 2", DisplayName = "Panelman - Tahun 2", Urutan = 2 },
            new ProtonTrack { TrackType = "Panelman", TahunKe = "Tahun 3", DisplayName = "Panelman - Tahun 3", Urutan = 3 },
            new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 1", DisplayName = "Operator - Tahun 1", Urutan = 4 },
            new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 2", DisplayName = "Operator - Tahun 2", Urutan = 5 },
            new ProtonTrack { TrackType = "Operator", TahunKe = "Tahun 3", DisplayName = "Operator - Tahun 3", Urutan = 6 },
        };

        // Pre-check orphan (log saja — tidak diperbaiki, lihat spec Non-Goals)
        var orphanKomp = await context.ProtonKompetensiList
            .CountAsync(k => !context.ProtonTracks.Any(t => t.Id == k.ProtonTrackId));
        var orphanAssign = await context.ProtonTrackAssignments
            .CountAsync(a => !context.ProtonTracks.Any(t => t.Id == a.ProtonTrackId));
        if (orphanKomp > 0 || orphanAssign > 0)
            logger.LogWarning("SeedProtonTracks: orphan ProtonTrackId — Kompetensi={K}, Assignment={A} (pre-existing, tak diperbaiki).",
                orphanKomp, orphanAssign);

        var existingKeys = await context.ProtonTracks
            .Select(t => new { t.TrackType, t.TahunKe }).ToListAsync();
        var existingSet = new HashSet<string>(existingKeys.Select(k => $"{k.TrackType}|{k.TahunKe}"));

        var missing = expected.Where(e => !existingSet.Contains($"{e.TrackType}|{e.TahunKe}")).ToList();
        if (missing.Count == 0)
        {
            logger.LogInformation("SeedProtonTracks: 6 track sudah lengkap, no-op.");
            return;
        }

        context.ProtonTracks.AddRange(missing);
        await context.SaveChangesAsync();   // satu save → atomik (EF implicit transaction)
        logger.LogInformation("SeedProtonTracks: {Count} track di-seed.", missing.Count);
    }
    catch (DbUpdateException ex)
    {
        logger.LogError(ex, "SeedProtonTracks: SaveChanges gagal, dilewati (data tak berubah).");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "SeedProtonTracks: error tak terduga, dilewati.");
    }
}
```

- [ ] **Step 4: Run — verify PASS**

Run: `dotnet test --filter "FullyQualifiedName~SeedProtonTracksTests"`
Expected: PASS 5/5.

- [ ] **Step 5: Commit**

```bash
git add Data/SeedData.cs HcPortal.Tests/SeedProtonTracksTests.cs
git commit -m "feat(seed): F2 self-heal SeedProtonTracksAsync — re-seed 6 ProtonTrack idempotent"
```

---

## Task 3: F1 — `NormalizeOrganizationLevelsAsync`

**Files:**
- Create: `HcPortal.Tests/NormalizeOrganizationLevelsTests.cs`
- Modify: `Data/SeedData.cs` (tambah method `NormalizeOrganizationLevelsAsync`)

- [ ] **Step 1: Tulis 7 test (failing)**

Buat `HcPortal.Tests/NormalizeOrganizationLevelsTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HcPortal.Tests;

public class NormalizeOrganizationLevelsTests
{
    private static ApplicationDbContext NewCtx() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // Helper: add root, return Id
    private static async Task<int> AddRoot(ApplicationDbContext ctx, string name, int level)
    {
        var r = new OrganizationUnit { Name = name, ParentId = null, Level = level, IsActive = true };
        ctx.OrganizationUnits.Add(r);
        await ctx.SaveChangesAsync();
        return r.Id;
    }
    private static async Task AddChild(ApplicationDbContext ctx, string name, int parentId, int level)
    {
        ctx.OrganizationUnits.Add(new OrganizationUnit { Name = name, ParentId = parentId, Level = level, IsActive = true });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task SplitBrain_Root1_Child2_NormalizedTo0And1()
    {
        using var ctx = NewCtx();
        var rid = await AddRoot(ctx, "RFCC", 1);
        await AddChild(ctx, "UnitA", rid, 2);

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "RFCC").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "UnitA").Level);
    }

    [Fact]
    public async Task AlreadyCorrect_NoChange()
    {
        using var ctx = NewCtx();
        var rid = await AddRoot(ctx, "LAB", 0);
        await AddChild(ctx, "U", rid, 1);

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "LAB").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "U").Level);
    }

    [Fact]
    public async Task MixedDevScenario_AllRootsZero_AllChildrenOne()
    {
        using var ctx = NewCtx();
        // 4 root sudah benar (L0, no child)
        foreach (var n in new[] { "LAB", "OM", "UTL II", "HC" }) await AddRoot(ctx, n, 0);
        // 4 root salah (L1) + anak L2
        foreach (var n in new[] { "RFCC", "DHT", "NGP", "GAST" })
        {
            var rid = await AddRoot(ctx, n, 1);
            await AddChild(ctx, $"{n}-U1", rid, 2);
            await AddChild(ctx, $"{n}-U2", rid, 2);
        }

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.All(ctx.OrganizationUnits.Where(u => u.ParentId == null), u => Assert.Equal(0, u.Level));
        Assert.All(ctx.OrganizationUnits.Where(u => u.ParentId != null), u => Assert.Equal(1, u.Level));
    }

    [Fact]
    public async Task Idempotent_SecondRun_NoChange()
    {
        using var ctx = NewCtx();
        var rid = await AddRoot(ctx, "RFCC", 1);
        await AddChild(ctx, "UnitA", rid, 2);

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());
        var logger2 = new CapturingLogger<SeedData>();
        await SeedData.NormalizeOrganizationLevelsAsync(ctx, logger2);

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "RFCC").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "UnitA").Level);
        Assert.Contains(logger2.Entries, e => e.Message.Contains("0 baris diubah"));
    }

    [Fact]
    public async Task Orphan_Unreachable_LeftUnchanged_WarningLogged()
    {
        using var ctx = NewCtx();
        // ParentId nunjuk Id tak-ada → tak terjangkau dari root
        ctx.OrganizationUnits.Add(new OrganizationUnit { Name = "Orphan", ParentId = 99999, Level = 5, IsActive = true });
        await ctx.SaveChangesAsync();
        var logger = new CapturingLogger<SeedData>();

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, logger);

        Assert.Equal(5, ctx.OrganizationUnits.Single(u => u.Name == "Orphan").Level); // unchanged
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task DeepTree_ThreeLevels_Normalized()
    {
        using var ctx = NewCtx();
        var rid = await AddRoot(ctx, "Root", 5);
        var c = new OrganizationUnit { Name = "Child", ParentId = rid, Level = 9, IsActive = true };
        ctx.OrganizationUnits.Add(c); await ctx.SaveChangesAsync();
        await AddChild(ctx, "Grand", c.Id, 0);

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "Root").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "Child").Level);
        Assert.Equal(2, ctx.OrganizationUnits.Single(u => u.Name == "Grand").Level);
    }

    [Fact]
    public async Task DevScenario_DoubleRun_SecondIsNoOp()
    {
        using var ctx = NewCtx();
        foreach (var n in new[] { "LAB", "OM", "UTL II", "HC" }) await AddRoot(ctx, n, 0);
        foreach (var n in new[] { "RFCC", "DHT", "NGP", "GAST" })
        {
            var rid = await AddRoot(ctx, n, 1);
            for (int i = 0; i < 4; i++) await AddChild(ctx, $"{n}-U{i}", rid, 2);
        }

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());
        // 8 root, 16 child → {0:8, 1:16}
        Assert.Equal(8, ctx.OrganizationUnits.Count(u => u.Level == 0));
        Assert.Equal(16, ctx.OrganizationUnits.Count(u => u.Level == 1));

        var logger2 = new CapturingLogger<SeedData>();
        await SeedData.NormalizeOrganizationLevelsAsync(ctx, logger2);
        Assert.Contains(logger2.Entries, e => e.Message.Contains("0 baris diubah"));
    }
}
```

- [ ] **Step 2: Run — verify FAIL**

Run: `dotnet test --filter "FullyQualifiedName~NormalizeOrganizationLevelsTests"`
Expected: FAIL kompilasi — `SeedData.NormalizeOrganizationLevelsAsync` belum ada.

- [ ] **Step 3: Implement `NormalizeOrganizationLevelsAsync`**

Di `Data/SeedData.cs`, tambah method ini di dalam class `SeedData`:

```csharp
/// <summary>
/// Self-heal: recompute OrganizationUnits.Level dari kedalaman tree (root=0, anak=induk+1).
/// UPDATE hanya baris yang Level-nya beda → idempotent, no-op kalau sudah konsisten.
/// Baris orphan/cycle (tak terjangkau dari root) di-skip + log warning (decision A).
/// Atomik via satu SaveChangesAsync. Hanya ubah kolom Level — tak tambah/hapus baris.
/// </summary>
public static async Task NormalizeOrganizationLevelsAsync(ApplicationDbContext context, ILogger logger)
{
    try
    {
        var all = await context.OrganizationUnits.ToListAsync();
        if (all.Count == 0) return;

        var childrenByParent = all
            .Where(u => u.ParentId.HasValue)
            .GroupBy(u => u.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // BFS dari root → depth
        var depth = new Dictionary<int, int>();
        var queue = new Queue<OrganizationUnit>();
        foreach (var root in all.Where(u => u.ParentId == null))
        {
            depth[root.Id] = 0;
            queue.Enqueue(root);
        }
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!childrenByParent.TryGetValue(node.Id, out var kids)) continue;
            foreach (var kid in kids)
            {
                if (depth.ContainsKey(kid.Id)) continue; // cycle guard / sudah dikunjungi
                depth[kid.Id] = depth[node.Id] + 1;
                queue.Enqueue(kid);
            }
        }

        var orphans = all.Where(u => !depth.ContainsKey(u.Id)).ToList();
        if (orphans.Count > 0)
            logger.LogWarning("NormalizeOrgLevels: {Count} unit orphan/unreachable di-skip. Ids: {Ids}",
                orphans.Count, string.Join(",", orphans.Select(o => o.Id)));

        var changed = all.Where(u => depth.ContainsKey(u.Id) && u.Level != depth[u.Id]).ToList();
        if (changed.Count == 0)
        {
            logger.LogInformation("NormalizeOrgLevels: 0 baris diubah (sudah konsisten), {Orphan} orphan.", orphans.Count);
            return;
        }

        foreach (var u in changed) u.Level = depth[u.Id];
        await context.SaveChangesAsync();   // satu save → atomik (EF implicit transaction)
        logger.LogInformation("NormalizeOrgLevels: {Count} baris dinormalisasi, {Orphan} orphan di-skip.",
            changed.Count, orphans.Count);
    }
    catch (DbUpdateException ex)
    {
        logger.LogError(ex, "NormalizeOrgLevels: SaveChanges gagal, dilewati (data tak berubah).");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "NormalizeOrgLevels: error tak terduga, dilewati.");
    }
}
```

- [ ] **Step 4: Run — verify PASS**

Run: `dotnet test --filter "FullyQualifiedName~NormalizeOrganizationLevelsTests"`
Expected: PASS 7/7.

- [ ] **Step 5: Commit**

```bash
git add Data/SeedData.cs HcPortal.Tests/NormalizeOrganizationLevelsTests.cs
git commit -m "feat(seed): F1 self-heal NormalizeOrganizationLevelsAsync — recompute org Level by tree-depth"
```

---

## Task 4: Wire kedua method ke `InitializeAsync`

**Files:**
- Modify: `Data/SeedData.cs` (method `InitializeAsync`, baris ~13-30)

- [ ] **Step 1: Tambah logger retrieval + 2 panggilan**

Ganti isi `InitializeAsync` (setelah baris yang ambil `context`) jadi seperti ini. Tambahkan `using Microsoft.Extensions.Logging;` di atas file kalau belum ada.

Cari blok existing:

```csharp
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Create Roles (always — needed in all environments)
            await CreateRolesAsync(roleManager);

            // 2. Create bootstrap admin account (all environments — needed for first login)
            await CreateAdminUserAsync(userManager);

            // 3. Seed OrganizationUnits (safety net for fresh deployment)
            await SeedOrganizationUnitsAsync(context);

            // 4. Seed OrganizationLevelLabels — Phase 340 D-01 (permanent + prod-required)
            await SeedOrganizationLevelLabelsAsync(context);
```

Ganti jadi:

```csharp
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<SeedData>>();

            // 1. Create Roles (always — needed in all environments)
            await CreateRolesAsync(roleManager);

            // 2. Create bootstrap admin account (all environments — needed for first login)
            await CreateAdminUserAsync(userManager);

            // 3. Seed OrganizationUnits (safety net for fresh deployment)
            await SeedOrganizationUnitsAsync(context);

            // 3b. F1 self-heal — normalisasi Level org by tree-depth (idempotent, runs all envs)
            await NormalizeOrganizationLevelsAsync(context, logger);

            // 4. Seed OrganizationLevelLabels — Phase 340 D-01 (permanent + prod-required)
            await SeedOrganizationLevelLabelsAsync(context);

            // 5. F2 self-heal — re-seed 6 ProtonTrack master (idempotent)
            await SeedProtonTracksAsync(context, logger);
```

- [ ] **Step 2: Build seluruh solution**

Run: `dotnet build`
Expected: 0 error.

- [ ] **Step 3: Run SELURUH test suite (regression)**

Run: `dotnet test --filter "Category!=Integration"`
Expected: semua PASS (test existing + 12 baru). Tak ada yang regress.

- [ ] **Step 4: Commit**

```bash
git add Data/SeedData.cs
git commit -m "feat(seed): wire F1 NormalizeOrganizationLevels + F2 SeedProtonTracks into InitializeAsync"
```

---

## Task 5: Verifikasi runtime lokal (manual, no code change)

Buktikan self-heal kerja end-to-end di app beneran (bukan cuma unit test). Ikuti SEED_WORKFLOW (snapshot → rusak → verify → restore).

- [ ] **Step 1: Snapshot DB lokal**

```powershell
sqlcmd -S 'localhost\SQLEXPRESS' -d master -E -C -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:\Temp\HcPortalDB_Dev_pre_selfheal_20260610.bak' WITH FORMAT, INIT;"
```

- [ ] **Step 2: Baseline — confirm lokal sudah benar (self-heal harusnya no-op)**

```powershell
sqlcmd -S 'localhost\SQLEXPRESS' -d HcPortalDB_Dev -E -C -h -1 -W -Q "SELECT Level, COUNT(*) FROM OrganizationUnits GROUP BY Level ORDER BY Level; SELECT COUNT(*) FROM ProtonTracks;"
```
Expected: org mulai Level 0 (mis. `{0:4, 1:17}`), ProtonTracks = 6.

- [ ] **Step 3: Rusak-buatan — geser root ke Level 1 + hapus tracks**

```powershell
sqlcmd -S 'localhost\SQLEXPRESS' -d HcPortalDB_Dev -E -C -Q "UPDATE OrganizationUnits SET Level = Level + 1; DELETE FROM ProtonKompetensiList; DELETE FROM ProtonTrackAssignments; DELETE FROM ProtonTracks;"
```
> `ProtonKompetensiList`/`ProtonTrackAssignments` di-clear dulu krn FK (Cascade/Restrict) — biar DELETE tracks lolos di lokal test.

- [ ] **Step 4: Restart app (self-heal jalan saat startup)**

```powershell
$env:Authentication__UseActiveDirectory="false"; dotnet run
```
> AD=false wajib di lokal (appsettings handoff AD=true). Tunggu app listen di `http://localhost:5277`. Cek log startup: cari baris `NormalizeOrgLevels: N baris dinormalisasi` + `SeedProtonTracks: 6 track di-seed`. Lalu stop (Ctrl+C).

- [ ] **Step 5: Verify self-heal benerin data**

```powershell
sqlcmd -S 'localhost\SQLEXPRESS' -d HcPortalDB_Dev -E -C -h -1 -W -Q "SELECT Level, COUNT(*) FROM OrganizationUnits GROUP BY Level ORDER BY Level; SELECT COUNT(*) FROM ProtonTracks;"
```
Expected: org balik mulai Level 0 (no Level ≥ 2), ProtonTracks = 6.

- [ ] **Step 6: Verify UI tab Status render**

`$env:Authentication__UseActiveDirectory="false"; dotnet run` → buka `http://localhost:5277/ProtonData/Index` → tab **Status** → baris muncul (matriks Unit × Track, awal merah). Stop app.

- [ ] **Step 7: Restore DB lokal**

```powershell
sqlcmd -S 'localhost\SQLEXPRESS' -d master -E -C -Q "ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='C:\Temp\HcPortalDB_Dev_pre_selfheal_20260610.bak' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"
```

- [ ] **Step 8: Catat di SEED_JOURNAL.md** — tandai skenario rusak-buatan `cleaned` (restored).

---

## Post-Plan: Handoff IT

Setelah semua task PASS + push ke main: bikin `docs/DB_HANDOFF_IT_<tgl>.html` (gaya existing) — deploy commit ke Dev → backup DB → restart → self-heal auto-jalan. Sertakan query verifikasi (spec §6) + rollback (restore .bak, no migration). **Bukan bagian eksekusi plan ini** — task terpisah setelah merge.

---

## Self-Review (writing-plans)

**Spec coverage:** F1 §3.2 → Task 3; F2 §3.3 → Task 2; wiring §3.1 → Task 4; logger final §3.1 → Task 4 Step 1; tests §4 (F1×7, F2×5) → Task 2/3; verifikasi lokal §5 → Task 5; handoff §6 → Post-Plan. Semua ke-cover.

**Placeholder scan:** tak ada TBD/TODO; semua step ada kode/command + expected output nyata.

**Type consistency:** `SeedProtonTracksAsync(ApplicationDbContext, ILogger)` + `NormalizeOrganizationLevelsAsync(ApplicationDbContext, ILogger)` konsisten antara test (Task 2/3 Step 1), impl (Step 3), wiring (Task 4). DbSet (`ProtonTracks`, `ProtonKompetensiList`, `ProtonTrackAssignments`, `OrganizationUnits`) + property (`TrackType`/`TahunKe`/`DisplayName`/`Urutan`/`ProtonTrackId`/`ParentId`/`Level`) sesuai model terverifikasi. Log string `"0 baris diubah"` dipakai konsisten di impl F1 + assert test 4 & 7.

**Catatan eksekusi:** kalau `ProtonKompetensi` punya required property selain `ProtonTrackId/NamaKompetensi/Bagian/Unit`, sesuaikan di Task 2 Step 1 test 5 (InMemory tak enforce, tapi nama property harus compile).
