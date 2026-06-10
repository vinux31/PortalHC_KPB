using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
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

    // DB shared antar-fact dalam fixture → tiap fact pakai coacheeId unik untuk isolasi.
    private static ProtonCompletionService NewSvc(ApplicationDbContext ctx)
        => new ProtonCompletionService(ctx, NullLogger<ProtonCompletionService>.Instance);

    private async Task<int> TrackIdAsync(ApplicationDbContext ctx, string trackType, string tahunKe)
        => (await ctx.ProtonTracks.FirstAsync(t => t.TrackType == trackType && t.TahunKe == tahunKe)).Id;

    private async Task<ProtonTrackAssignment> SeedAssignmentAsync(ApplicationDbContext ctx, string coacheeId, int trackId, bool active = true)
    {
        var a = new ProtonTrackAssignment { CoacheeId = coacheeId, AssignedById = "hc", ProtonTrackId = trackId, IsActive = active };
        ctx.ProtonTrackAssignments.Add(a);
        await ctx.SaveChangesAsync();
        return a;
    }

    [Fact]
    public async Task EnsureAsync_Idempotent()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"idem-{Guid.NewGuid():N}";
        var trackId = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var asg = await SeedAssignmentAsync(ctx, coachee, trackId);
        var svc = NewSvc(ctx);

        Assert.True(await svc.EnsureAsync(coachee, trackId, "hc", "Exam", "t"));
        Assert.False(await svc.EnsureAsync(coachee, trackId, "hc", "Exam", "t"));
        Assert.Single(ctx.ProtonFinalAssessments.Where(fa => fa.ProtonTrackAssignmentId == asg.Id));
    }

    [Fact]
    public async Task EnsureAsync_NoAssignment_ReturnsFalse()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"noasg-{Guid.NewGuid():N}";
        var trackId = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var svc = NewSvc(ctx);

        Assert.False(await svc.EnsureAsync(coachee, trackId, "hc", "Exam", "t"));
        Assert.Empty(ctx.ProtonFinalAssessments.Where(fa => fa.CoacheeId == coachee));
    }

    [Fact]
    public async Task RemoveExamOrigin_SelektifExamOnly()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"rmexam-{Guid.NewGuid():N}";
        var trackExam = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var trackBypass = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        var a1 = await SeedAssignmentAsync(ctx, coachee, trackExam);
        var a2 = await SeedAssignmentAsync(ctx, coachee, trackBypass);
        ctx.ProtonFinalAssessments.Add(new ProtonFinalAssessment { CoacheeId = coachee, CreatedById = "hc", ProtonTrackAssignmentId = a1.Id, Status = "Completed", Origin = "Exam", CompletedAt = DateTime.UtcNow });
        ctx.ProtonFinalAssessments.Add(new ProtonFinalAssessment { CoacheeId = coachee, CreatedById = "hc", ProtonTrackAssignmentId = a2.Id, Status = "Completed", Origin = "Bypass", CompletedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var svc = NewSvc(ctx);

        Assert.True(await svc.RemoveExamOriginAsync(coachee, trackExam));
        Assert.Empty(ctx.ProtonFinalAssessments.Where(fa => fa.CoacheeId == coachee && fa.Origin == "Exam"));
        Assert.NotEmpty(ctx.ProtonFinalAssessments.Where(fa => fa.CoacheeId == coachee && fa.Origin == "Bypass"));
    }

    [Fact]
    public async Task GetPassedYears_MatchTrackType()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var coachee = $"passed-{Guid.NewGuid():N}";
        var trackId = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var asg = await SeedAssignmentAsync(ctx, coachee, trackId);
        ctx.ProtonFinalAssessments.Add(new ProtonFinalAssessment { CoacheeId = coachee, CreatedById = "hc", ProtonTrackAssignmentId = asg.Id, Status = "Completed", Origin = "Exam", CompletedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var svc = NewSvc(ctx);

        Assert.Contains("Tahun 1", await svc.GetPassedYearsAsync(coachee, "Operator"));
        Assert.Empty(await svc.GetPassedYearsAsync(coachee, "Panelman"));
    }
}
