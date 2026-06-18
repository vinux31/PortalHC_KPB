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

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02/404 (backfill 1 primary-row/pekerja Unit non-null)")]
    public void Backfill_UserWithNonNullUnit_HasExactlyOnePrimaryRow()
    {
        // plan 02/404: seed Users (Unit non-null), apply backfill → 1 baris IsPrimary=1 per user.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02/404");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02/404 (Unit null → 0 baris)")]
    public void Backfill_UserWithNullUnit_HasZeroRows()
    {
        // plan 02/404: user Unit null/empty → 0 baris UserUnits.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02/404");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02/404 (idempotent re-run no dup)")]
    public void Backfill_RerunIsIdempotent_NoDuplicateRows()
    {
        // plan 02/404: jalankan backfill SQL dua kali → run-2 INSERT 0 baris (WHERE NOT EXISTS).
        Assert.True(false, "Wave 0 scaffold — diisi plan 02/404");
    }
}
