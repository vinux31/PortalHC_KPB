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
