// Phase 415 SEC-01 (Wave-0) — disposable SQLEXPRESS fixture untuk Section data-layer tests.
// Copy pola FlexibleParticipantAddFixture: DB sekali-pakai HcPortalDB_Test_{guid}, MigrateAsync di
// InitializeAsync (mengeksekusi migration 415 AddAssessmentPackageSection pada DB segar → memberi
// cakupan real-SQL untuk FK SetNull + unique index yang TIDAK bisa ditegakkan EF InMemory), dan
// EnsureDeletedAsync di DisposeAsync. HcPortalDB_Dev TIDAK disentuh.
using System;
using System.Threading.Tasks;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class SectionFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    public DbContextOptions<ApplicationDbContext> Options => _options;

    public SectionFixture()
    {
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(_options);
            await ctx.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            try { await using var cleanup = new ApplicationDbContext(_options); await cleanup.Database.EnsureDeletedAsync(); } catch { }
            throw new Xunit.Sdk.XunitException(
                $"Phase 415 SectionFixture setup failed during MigrateAsync of {DbName}. Indikasi MIGRATION-CHAIN break, BUKAN bug fix. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.EnsureDeletedAsync();
    }
}
