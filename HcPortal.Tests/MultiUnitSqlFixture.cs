// Phase 404 (QA-01/QA-03/QA-04) — shared SQL-real fixture for the multi-unit invariant suite.
//
// WHY SQL-riil (vs InMemory): EF-InMemory bypasses the migrations pipeline AND does NOT enforce
// filtered-unique indexes (IX_CoachCoacheeMappings_CoacheeId_ActiveUnique, IX_UserUnits_UserId_PrimaryUnique,
// ProtonKompetensi/deliverable 1:1). Every downstream invariant (QA-03 single-active, QA-04 unit-membership)
// only becomes meaningful against a real SQL Server. MigrateAsync (D-03) runs the FULL migration chain incl
// 399 AddUserUnitsTable + its filtered-unique IX — so this fixture doubles as a deploy-migration smoke test.
//
// Pola disalin VERBATIM dari OrgLabelMigrationFixture (OrgLabelMigrationIntegrationTests.cs:24-66) +
// seed-chain helpers dari ProtonBypassServiceTests.cs:39-66. Disposable HcPortalDB_Test_<guid> di
// localhost\SQLEXPRESS, drop di DisposeAsync DAN di mid-migration failure (catch). DB dev TIDAK disentuh.
// Test classes pakai [Trait("Category","Integration")] → SQL-less CI skip via --filter "Category!=Integration".
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class MultiUnitSqlFixture : IAsyncLifetime
{
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    private readonly string _cs;
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;

    // Stable canonical identifiers — downstream test classes reuse these (D-02).
    public const string Bagian = "Bagian-404";
    public const string UnitX = "UnitX-404";
    public const string UnitY = "UnitY-404";
    public const string CoacheeId = "cee-404-canon";
    public const string CoachId = "coach-404-canon";

    public MultiUnitSqlFixture()
    {
        // localhost-only + Integrated Security (no secrets, no env vars).
        // TrustServerCertificate=True — SQLEXPRESS self-signed cert.
        _cs = $"Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30";
    }

    public async Task InitializeAsync()
    {
        Options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        try
        {
            await using var ctx = new ApplicationDbContext(Options);
            await ctx.Database.MigrateAsync();   // D-03: FULL chain incl 399 AddUserUnitsTable + filtered-unique IX.
                                                 // Pitfall 2: do NOT use the schema-from-model shortcut — it would
                                                 // skip the migration pipeline and not prove migration 399 applies.
            await SeedCanonicalAsync(ctx);        // D-02: {X,Y} 1 Bagian + coach cross-unit + PROTON T1@X / T2@Y.
        }
        catch (Exception ex)
        {
            // A mid-migration/seed throw must NOT leave HcPortalDB_Test_<guid> behind.
            try { await using var c = new ApplicationDbContext(Options); await c.Database.EnsureDeletedAsync(); } catch { /* best-effort */ }
            throw new Xunit.Sdk.XunitException(
                $"Phase 404 MultiUnit fixture setup gagal saat MigrateAsync/seed DB {DbName}. " +
                $"Indikasi MIGRATION-CHAIN break (full chain run, incl 399 AddUserUnitsTable), BUKAN tentu bug multi-unit. Inner: {ex}");
        }
    }

    public async Task DisposeAsync()
    {
        await using var ctx = new ApplicationDbContext(Options);
        await ctx.Database.EnsureDeletedAsync();   // success-path drop of the disposable DB.
    }

    // Master ProtonTracks rows arrive FREE from the migration InsertData — query, never insert.
    private static async Task<int> TrackIdAsync(ApplicationDbContext ctx, string trackType, string tahunKe)
        => (await ctx.ProtonTracks.FirstAsync(t => t.TrackType == trackType && t.TahunKe == tahunKe)).Id;

    // Kompetensi(unit)→Sub→Deliverable chain for a track+unit (pola SeedDeliverablesAsync).
    private static async Task SeedDeliverableChainAsync(ApplicationDbContext ctx, int trackId, string unit)
    {
        var komp = new ProtonKompetensi { Bagian = Bagian, Unit = unit, NamaKompetensi = $"K-{unit}", Urutan = 1, ProtonTrackId = trackId };
        ctx.ProtonKompetensiList.Add(komp);
        await ctx.SaveChangesAsync();
        var sub = new ProtonSubKompetensi { ProtonKompetensiId = komp.Id, NamaSubKompetensi = "Sub", Urutan = 1 };
        ctx.ProtonSubKompetensiList.Add(sub);
        await ctx.SaveChangesAsync();
        ctx.ProtonDeliverableList.Add(new ProtonDeliverable { ProtonSubKompetensiId = sub.Id, NamaDeliverable = "D1", Urutan = 1 });
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedCanonicalAsync(ApplicationDbContext ctx)
    {
        // Org tree: Bagian (Level 0) → UnitX, UnitY (Level 1). User links via Name-string.
        var bagian = new OrganizationUnit { Name = Bagian, Level = 0, ParentId = null };
        ctx.OrganizationUnits.Add(bagian);
        await ctx.SaveChangesAsync();
        ctx.OrganizationUnits.Add(new OrganizationUnit { Name = UnitX, Level = 1, ParentId = bagian.Id });
        ctx.OrganizationUnits.Add(new OrganizationUnit { Name = UnitY, Level = 1, ParentId = bagian.Id });
        await ctx.SaveChangesAsync();

        // Coachee (Unit=UnitX primary mirror) + Coach (cross-unit) — same Bagian.
        ctx.Users.Add(new ApplicationUser { Id = CoacheeId, UserName = CoacheeId, FullName = "Coachee 404", Section = Bagian, Unit = UnitX, IsActive = true, RoleLevel = 6 });
        ctx.Users.Add(new ApplicationUser { Id = CoachId, UserName = CoachId, FullName = "Coach 404", Section = Bagian, Unit = UnitX, IsActive = true, RoleLevel = 5 });
        await ctx.SaveChangesAsync();

        // UserUnits junction (Pitfall 3: ApplicationUser has NO .UserUnits nav — insert into ctx.UserUnits directly).
        ctx.UserUnits.Add(new UserUnit { UserId = CoacheeId, Unit = UnitX, IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = CoacheeId, Unit = UnitY, IsPrimary = false, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = CoachId, Unit = UnitX, IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = CoachId, Unit = UnitY, IsPrimary = false, IsActive = true });
        await ctx.SaveChangesAsync();

        // PROTON chains for BOTH units (T1@X, T2@Y). Do NOT pre-create ProtonTrackAssignments —
        // the sequential T1@X→T2@Y bypass is driven by the invariant Facts (so they assert the active count).
        var t1 = await TrackIdAsync(ctx, "Operator", "Tahun 1");
        var t2 = await TrackIdAsync(ctx, "Operator", "Tahun 2");
        await SeedDeliverableChainAsync(ctx, t1, UnitX);
        await SeedDeliverableChainAsync(ctx, t2, UnitY);
    }
}
