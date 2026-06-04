using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Data;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 344 TEST-05 — real-SQL-Server migration + seed + first-read integration test.
///
/// WHY this exists (vs the EF InMemory unit tests): InMemory never executes migration DDL —
/// it builds schema from the model and bypasses the migrations pipeline. This test proves the
/// real migration 20260603012335_AddOrganizationLevelLabel applies against an actual SQL Server
/// instance (via the migrations pipeline, NOT the schema-from-model shortcut), the production seed populates the 3 default
/// rows (seed is NOT in the migration — Pitfall 2), and OrgLabelService reads configured labels.
///
/// Per D-02: runs on a disposable HcPortalDB_Test_&lt;guid&gt; on localhost\SQLEXPRESS, dropped per
/// run on BOTH success (DisposeAsync) and mid-migration failure (InitializeAsync catch — M1).
/// The shared development database is never touched, so no SEED_WORKFLOW snapshot/restore is needed.
///
/// [Trait("Category","Integration")] lets a SQL-less CI skip it: dotnet test --filter "Category!=Integration".
/// </summary>
public class OrgLabelMigrationFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    public DbContextOptions<ApplicationDbContext> Options => _options;

    public OrgLabelMigrationFixture()
    {
        // localhost-only + Integrated Security (mirrors the dev connstr guard; no secrets, no env vars).
        // TrustServerCertificate=True required — SQLEXPRESS uses a self-signed cert (verified 2026-06-04).
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync();   // MUST run the migrations pipeline, NOT the schema-from-model shortcut —
                                                 // validates the real migration DDL. The full
                                                 // ~91-migration chain runs here (acceptable).
            await SeedData.SeedOrganizationLevelLabelsAsync(ctx); // production seed; migration does NOT seed (Pitfall 2)
        }
        catch (Exception ex)
        {
            // M1: a mid-migration throw must NOT leave HcPortalDB_Test_<guid> behind.
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"TEST-05 setup failed during MigrateAsync/seed of disposable DB {DbName}. " +
                $"This indicates a MIGRATION-CHAIN break (full ~91-migration chain runs), NOT necessarily an OrgLabel bug. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        // Success-path drop of the disposable DB (D-02).
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Integration")]
public class OrgLabelMigrationIntegrationTests : IClassFixture<OrgLabelMigrationFixture>
{
    private readonly OrgLabelMigrationFixture _fixture;

    public OrgLabelMigrationIntegrationTests(OrgLabelMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Migrate_Seed_FirstRead_ReturnsConfiguredLabel_NotFallback()
    {
        using var ctx = new ApplicationDbContext(_fixture.Options);
        var svc = new OrgLabelService(ctx, new MemoryCache(new MemoryCacheOptions()), new AuditLogService(ctx));

        // Configured labels (NOT the "Level N" fallback) — proves migration + production seed worked on real SQL.
        Assert.Equal("Bagian", svc.GetLabel(0));
        Assert.Equal("Unit", svc.GetLabel(1));
        Assert.Equal("Sub-unit", svc.GetLabel(2));

        // Fallback still works on the real DB for an unconfigured level.
        Assert.Equal("Level 99", svc.GetLabel(99));

        // Exactly 3 seeded rows (Pitfall 2: never expect MigrateAsync alone to populate — seed is separate).
        Assert.Equal(3, ctx.OrganizationLevelLabels.Count());
    }
}
