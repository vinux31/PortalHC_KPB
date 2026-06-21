// Phase 399 Plan 01 Task 5 — WAVE 0 SCAFFOLD (RED, skip-with-reason).
// Kontrak test MU-05 backfill idempotent — SQL-RIIL (bukan InMemory).
//
// WHY SQL-riil (vs InMemory): InMemory bypass migrations pipeline + TIDAK enforce
// filtered-unique index (Pitfall 3). Backfill `AddUserUnitsTable` (INSERT...SELECT WHERE NOT EXISTS)
// + filtered-unique IX_UserUnits_UserId_PrimaryUnique hanya bisa diverifikasi di SQL Server asli.
//
// Behavior yang dikontrak (diaktifkan plan 02/404):
//   - Pekerja Unit non-null → tepat 1 baris UserUnits IsPrimary=1.
//   - Pekerja Unit null/empty → 0 baris.
//   - Idempotent: re-run backfill SQL → 0 baris baru (WHERE NOT EXISTS).
//
// Pola fixture disalin dari OrgLabelMigrationFixture (OrgLabelMigrationIntegrationTests.cs:24-66):
// disposable HcPortalDB_Test_<guid> di localhost\SQLEXPRESS + MigrateAsync, drop di DisposeAsync.
// DB dev TIDAK disentuh → tak butuh snapshot/restore SEED_WORKFLOW.
// [Trait("Category","Integration")] → SQL-less CI skip via --filter "Category!=Integration".
using System;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class UserUnitsBackfillFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;

    public UserUnitsBackfillFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(Options);
            // Migrations pipeline (full chain) — menjalankan AddUserUnitsTable DDL + backfill SQL pada
            // DB kosong (0 Users → 0 backfill rows). Plan 02/404 seed Users dulu lalu re-run/verify.
            await ctx.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(Options); await cleanup.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"Wave 0 backfill fixture setup gagal saat MigrateAsync DB disposable {DbName}. " +
                $"Indikasi MIGRATION-CHAIN break (full chain run), belum tentu bug UserUnits. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class UserUnitsBackfillIntegrationTests : IClassFixture<UserUnitsBackfillFixture>
{
    private readonly UserUnitsBackfillFixture _fixture;

    public UserUnitsBackfillIntegrationTests(UserUnitsBackfillFixture fixture)
    {
        _fixture = fixture;
    }

    // VERBATIM copy of the migration-399 backfill statement (Migrations/20260618045427_AddUserUnitsTable.cs:51-57).
    // Re-running the migration's OWN SQL is what truthfully proves idempotency — do NOT hand-roll new SQL.
    private const string BackfillSql = @"
                INSERT INTO UserUnits (UserId, Unit, IsPrimary, IsActive)
                SELECT u.Id, u.Unit, 1, 1
                FROM Users u
                WHERE u.Unit IS NOT NULL AND u.Unit <> ''
                  AND NOT EXISTS (SELECT 1 FROM UserUnits uu WHERE uu.UserId = u.Id)
            ";

    [Fact]
    public async Task Backfill_UserWithNonNullUnit_HasExactlyOnePrimaryRow()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var userId = $"bf-nn-{Guid.NewGuid():N}";
        ctx.Users.Add(new ApplicationUser { Id = userId, UserName = userId, FullName = $"Worker {userId[..8]}", Unit = "UnitX-bf" });
        await ctx.SaveChangesAsync();

        await ctx.Database.ExecuteSqlRawAsync(BackfillSql);

        Assert.Equal(1, await ctx.UserUnits.CountAsync(uu => uu.UserId == userId && uu.IsPrimary));
        Assert.Equal(1, await ctx.UserUnits.CountAsync(uu => uu.UserId == userId)); // exactly one total
    }

    [Fact]
    public async Task Backfill_UserWithNullUnit_HasZeroRows()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var nullUser = $"bf-null-{Guid.NewGuid():N}";
        var emptyUser = $"bf-empty-{Guid.NewGuid():N}";
        ctx.Users.Add(new ApplicationUser { Id = nullUser, UserName = nullUser, FullName = $"Worker {nullUser[..8]}", Unit = null });
        ctx.Users.Add(new ApplicationUser { Id = emptyUser, UserName = emptyUser, FullName = $"Worker {emptyUser[..8]}", Unit = "" });
        await ctx.SaveChangesAsync();

        await ctx.Database.ExecuteSqlRawAsync(BackfillSql);

        Assert.Equal(0, await ctx.UserUnits.CountAsync(uu => uu.UserId == nullUser));
        Assert.Equal(0, await ctx.UserUnits.CountAsync(uu => uu.UserId == emptyUser));
    }

    [Fact]
    public async Task Backfill_RerunIsIdempotent_NoDuplicateRows()
    {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var userId = $"bf-idem-{Guid.NewGuid():N}";
        ctx.Users.Add(new ApplicationUser { Id = userId, UserName = userId, FullName = $"Worker {userId[..8]}", Unit = "UnitY-bf" });
        await ctx.SaveChangesAsync();

        await ctx.Database.ExecuteSqlRawAsync(BackfillSql); // run 1 → inserts 1 row
        await ctx.Database.ExecuteSqlRawAsync(BackfillSql); // run 2 → WHERE NOT EXISTS makes it insert 0 rows

        Assert.Equal(1, await ctx.UserUnits.CountAsync(uu => uu.UserId == userId)); // still exactly 1 (idempotent)
    }
}
