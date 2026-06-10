using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Coverage SeedData.SeedProtonTracksAsync — safety-net seed 6 ProtonTrack
/// (Panelman/Operator × Tahun 1/2/3) yang sebelumnya hanya di-seed via migration.
/// Akar: DB Dev ProtonTracks kosong → dropdown Track + StatusData ProtonData kosong.
/// </summary>
public class SeedProtonTracksTests
{
    private static ApplicationDbContext MakeCtx()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SeedProtonTracksAsync_EmptyTable_Inserts6Tracks()
    {
        using var ctx = MakeCtx();

        await SeedData.SeedProtonTracksAsync(ctx);

        Assert.Equal(6, await ctx.ProtonTracks.CountAsync());
    }

    [Fact]
    public async Task SeedProtonTracksAsync_InsertsExpectedCombosWithDisplayNameAndUrutan()
    {
        using var ctx = MakeCtx();

        await SeedData.SeedProtonTracksAsync(ctx);

        var tracks = await ctx.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
        var expected = new[]
        {
            ("Panelman", "Tahun 1", "Panelman - Tahun 1", 1),
            ("Panelman", "Tahun 2", "Panelman - Tahun 2", 2),
            ("Panelman", "Tahun 3", "Panelman - Tahun 3", 3),
            ("Operator", "Tahun 1", "Operator - Tahun 1", 4),
            ("Operator", "Tahun 2", "Operator - Tahun 2", 5),
            ("Operator", "Tahun 3", "Operator - Tahun 3", 6),
        };

        Assert.Equal(expected.Length, tracks.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Item1, tracks[i].TrackType);
            Assert.Equal(expected[i].Item2, tracks[i].TahunKe);
            Assert.Equal(expected[i].Item3, tracks[i].DisplayName);
            Assert.Equal(expected[i].Item4, tracks[i].Urutan);
        }
    }

    [Fact]
    public async Task SeedProtonTracksAsync_AlreadyPopulated_IsIdempotentNoDuplicate()
    {
        using var ctx = MakeCtx();
        ctx.ProtonTracks.Add(new ProtonTrack
        {
            TrackType = "Panelman", TahunKe = "Tahun 1", DisplayName = "Panelman - Tahun 1", Urutan = 1
        });
        await ctx.SaveChangesAsync();

        // Run twice — must skip both times (guard: if AnyAsync() return).
        await SeedData.SeedProtonTracksAsync(ctx);
        await SeedData.SeedProtonTracksAsync(ctx);

        Assert.Equal(1, await ctx.ProtonTracks.CountAsync());
    }
}
