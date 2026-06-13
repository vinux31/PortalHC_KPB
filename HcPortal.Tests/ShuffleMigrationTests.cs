using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 372 SHUF-01 — real-SQL disposable fixture (pola Phase 344/358/366 TEST) yang membuktikan
/// migration <c>AddShuffleTogglesToAssessmentSession</c> apply di SQL Server NYATA dan constraint
/// <c>bit NOT NULL DEFAULT 1</c> meng-ON-kan baris yang TIDAK menyebut kolom shuffle — mekanisme
/// SAMA yang men-backfill 58 baris lama jadi ON.
///
/// WHY (vs EF InMemory): InMemory bypass migrations pipeline (schema-from-model), DEFAULT constraint
/// tak pernah dieksekusi → tak bisa membuktikan backfill. Fixture ini jalankan MigrateAsync() PENUH.
///
/// Disposable HcPortalDB_Test_&lt;guid&gt; di localhost\SQLEXPRESS; HcPortalDB_Dev TAK tersentuh
/// (no SEED_WORKFLOW snapshot). [Trait("Category","Integration")] → CI SQL-less skip via
/// dotnet test --filter "Category!=Integration".
/// </summary>
public class ShuffleMigrationFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    public DbContextOptions<ApplicationDbContext> Options => _options;

    public ShuffleMigrationFixture()
    {
        // localhost-only + Integrated Security (mirror dev connstr guard; no secrets/env). SQLEXPRESS self-signed cert → TrustServerCertificate=True.
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync(); // pipeline penuh (real DDL, bukan schema-from-model) — buktikan migration shuffle apply.
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"Phase 372 SHUF-01 setup failed during MigrateAsync of disposable DB {DbName}. " +
                $"Indikasi MIGRATION-CHAIN break (full chain runs), BUKAN bug shuffle. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

// Isolasi: kedua [Fact] berbagi 1 DB disposable (IClassFixture) tapi tiap test pakai user unik
// (SeedUserAsync GUID) + filter row per UserId → tak ada cross-test contamination. DbContext baru per akses.
[Trait("Category", "Integration")]
public class ShuffleMigrationTests : IClassFixture<ShuffleMigrationFixture>
{
    private readonly ShuffleMigrationFixture _fixture;

    public ShuffleMigrationTests(ShuffleMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    // AssessmentSessions.UserId punya FK ke Users (FK_AssessmentSessions_Users_UserId) → seed user dulu.
    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "shuf-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "shuf@test.local",
            FullName = "Shuffle Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>
    /// SHUF-01a — baris di-INSERT via raw SQL TANPA menyebut kolom shuffle (simulasi baris lama).
    /// DB DEFAULT (CONVERT([bit],(1))) mengisi keduanya → baca balik = true. Mekanisme yang sama
    /// dengan backfill ADD COLUMN ... NOT NULL DEFAULT 1 atas baris existing.
    /// </summary>
    [Fact]
    public async Task Migration_BackfillsRowsOmittingShuffleColumns_ToTrue()
    {
        string userId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            userId = await SeedUserAsync(ctx);

            // Raw INSERT: sebut semua kolom NOT NULL tanpa DB-default, OMIT ShuffleQuestions/ShuffleOptions.
            await ctx.Database.ExecuteSqlRawAsync(
                "INSERT INTO AssessmentSessions (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor, IsTokenRequired, AccessToken) " +
                "VALUES ({0}, 'OldRowSim', 'Test', '2026-01-01T00:00:00', 60, 'Open', 0, '#000000', 0, '')",
                userId);
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var row = await readCtx.AssessmentSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Id)
            .FirstAsync();

        Assert.True(row.ShuffleQuestions);
        Assert.True(row.ShuffleOptions);
    }

    /// <summary>
    /// SHUF-01b — kolom ter-map + persist nilai NON-default (false/true) lewat round-trip EF,
    /// membuktikan kolom benar-benar tersimpan (bukan dipaksa default).
    /// </summary>
    [Fact]
    public async Task Migration_ShuffleColumns_Queryable_RoundTrip()
    {
        int id;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var userId = await SeedUserAsync(ctx);
            var s = new AssessmentSession
            {
                UserId = userId,
                Title = "RoundTrip",
                Category = "Test",
                Status = "Open",
                AccessToken = "",
                ShuffleQuestions = false,
                ShuffleOptions = true
            };
            ctx.AssessmentSessions.Add(s);
            await ctx.SaveChangesAsync();
            id = s.Id;
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var read = await readCtx.AssessmentSessions.AsNoTracking().FirstAsync(s => s.Id == id);

        Assert.False(read.ShuffleQuestions);
        Assert.True(read.ShuffleOptions);
    }
}
