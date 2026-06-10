using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 358 TEST-05 — real-SQL-Server disposable fixture untuk ProtonCompletionService.
///
/// WHY (vs EF InMemory): InMemory bypass migrations pipeline (schema-from-model). Fixture ini
/// jalankan MigrateAsync() PENUH → buktikan migration AddOriginToProtonFinalAssessment (kolom Origin)
/// apply di SQL Server NYATA (PCOMP-04).
///
/// Disposable HcPortalDB_Test_&lt;guid&gt; di localhost\SQLEXPRESS, drop per run di SUKSES
/// (DisposeAsync) DAN gagal mid-migration (InitializeAsync catch). DB lokal HcPortalDB_Dev
/// TAK tersentuh — no SEED_WORKFLOW snapshot/restore.
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
///
/// [Fact] service (Ensure/Remove/GetPassedYears) ditambah Plan 02.
/// </summary>
public class ProtonCompletionFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    public DbContextOptions<ApplicationDbContext> Options => _options;

    public ProtonCompletionFixture()
    {
        // localhost-only + Integrated Security (mirror dev connstr guard; no secrets, no env vars).
        // TrustServerCertificate=True — SQLEXPRESS self-signed cert.
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
            // mid-migration throw TAK boleh tinggalkan HcPortalDB_Test_<guid>.
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException($"TEST-05 setup failed during MigrateAsync of disposable DB {DbName}. Mengindikasikan MIGRATION-CHAIN break, bukan tentu bug Origin. Inner: {ex}");
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

    public ProtonCompletionServiceTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Smoke: kolom Origin ter-map + queryable lewat EF setelah migration. Seed chain
    /// ProtonTrack → ProtonTrackAssignment → ProtonFinalAssessment (TANPA set Origin → null pada insert baru),
    /// lalu Where(Origin == null) resolve tanpa throw + Single. Membuktikan migration AddOrigin apply.
    /// </summary>
    [Fact]
    public async Task Migration_AddsOriginColumn()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);

        // ProtonTracks di-seed migration (HasData) — reuse existing, jangan insert (UNIQUE TrackType+TahunKe).
        var track = await ctx.ProtonTracks.FirstAsync();

        var assignment = new ProtonTrackAssignment { CoacheeId = "test-coachee", AssignedById = "test-hc", ProtonTrackId = track.Id, IsActive = true };
        ctx.ProtonTrackAssignments.Add(assignment);
        await ctx.SaveChangesAsync();

        var fa = new ProtonFinalAssessment
        {
            CoacheeId = "test-coachee",
            CreatedById = "test-hc",
            ProtonTrackAssignmentId = assignment.Id,
            Status = "Completed",
            CompletedAt = DateTime.UtcNow
            // Origin sengaja TIDAK di-set → null pada insert baru
        };
        ctx.ProtonFinalAssessments.Add(fa);
        await ctx.SaveChangesAsync();

        // Kolom Origin ter-map: query EF tidak throw + baris baru ber-Origin null.
        var nullOrigin = await ctx.ProtonFinalAssessments.Where(x => x.Origin == null).ToListAsync();
        Assert.Single(nullOrigin);
        Assert.Null(nullOrigin[0].Origin);
    }
}
