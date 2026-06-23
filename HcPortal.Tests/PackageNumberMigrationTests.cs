using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 422 SHFX-05/D-02 (Wave 1) — real-SQL: verifikasi STATEMENT dedup migration
/// AddPackageNumberUniqueIndex Up() Step 1 (ROW_NUMBER OVER PARTITION renumber).
///
/// Karena fixture sudah apply unique index (MigrateAsync), test ini sengaja DROP index dulu
/// agar bisa men-seed baris PackageNumber DUPLIKAT (replikasi state DB lama pra-migration),
/// lalu menjalankan SQL dedup yang IDENTIK dengan migration Up() Step 1 via ExecuteSqlRaw,
/// lalu assert 0 duplikat + renumber gap-free (1..N) per session terurut (PackageNumber, Id).
/// Index di-CREATE ulang di akhir agar DB test konsisten untuk test lain di fixture yang sama.
///
/// [Trait("Category","Integration")] -> skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class PackageNumberMigrationTests : IClassFixture<ProtonCompletionFixture>
{
    private const string IndexName = "IX_AssessmentPackages_SessionId_PackageNumber_Unique";

    // SQL dedup IDENTIK dengan migration Up() Step 1 (yang di-uji statement-nya).
    private const string DedupSql = @"
        WITH Numbered AS (
            SELECT Id,
                   ROW_NUMBER() OVER (PARTITION BY AssessmentSessionId ORDER BY PackageNumber, Id) AS rn
            FROM AssessmentPackages
        )
        UPDATE p
        SET p.PackageNumber = n.rn
        FROM AssessmentPackages p
        INNER JOIN Numbered n ON p.Id = n.Id;";

    private readonly ProtonCompletionFixture _fixture;

    public PackageNumberMigrationTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<int> SeedSessionAsync(ApplicationDbContext ctx, string title)
    {
        var user = new ApplicationUser
        {
            UserName = "pkgmig-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "pkgmig@test.local",
            FullName = "PackageNumber Migration Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var session = new AssessmentSession
        {
            UserId = user.Id,
            Title = title,
            Category = "Test",
            Status = "Open",
            AccessToken = "",
            Schedule = new DateTime(2026, 5, 8, 8, 0, 0)
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();
        return session.Id;
    }

    [Fact]
    public async Task DedupSql_RenumbersDuplicates_GapFreePerSession()
    {
        var marker = "TAG-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int sessionA, sessionB;

        // DROP index agar bisa seed duplikat (replikasi data lama pra-migration).
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            await ctx.Database.ExecuteSqlRawAsync(
                $"DROP INDEX [{IndexName}] ON [AssessmentPackages];");

            sessionA = await SeedSessionAsync(ctx, marker + "-A");
            sessionB = await SeedSessionAsync(ctx, marker + "-B");

            // Session A: duplikat (A,1),(A,1),(A,2) -> harus jadi 1,2,3 gap-free.
            ctx.AssessmentPackages.AddRange(
                new AssessmentPackage { AssessmentSessionId = sessionA, PackageName = "A-d1", PackageNumber = 1 },
                new AssessmentPackage { AssessmentSessionId = sessionA, PackageName = "A-d2", PackageNumber = 1 },
                new AssessmentPackage { AssessmentSessionId = sessionA, PackageName = "A-d3", PackageNumber = 2 });
            // Session B: gap (B,5),(B,9) -> harus jadi 1,2 (renumber per session terisolasi).
            ctx.AssessmentPackages.AddRange(
                new AssessmentPackage { AssessmentSessionId = sessionB, PackageName = "B-g1", PackageNumber = 5 },
                new AssessmentPackage { AssessmentSessionId = sessionB, PackageName = "B-g2", PackageNumber = 9 });
            await ctx.SaveChangesAsync();
        }

        // Jalankan SQL dedup (statement migration Up Step 1).
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            await ctx.Database.ExecuteSqlRawAsync(DedupSql);
        }

        await using (var readCtx = new ApplicationDbContext(_fixture.Options))
        {
            // 0 duplikat per session (untuk session yang di-seed test ini).
            var dups = await readCtx.AssessmentPackages.AsNoTracking()
                .Where(p => p.AssessmentSessionId == sessionA || p.AssessmentSessionId == sessionB)
                .GroupBy(p => new { p.AssessmentSessionId, p.PackageNumber })
                .Where(g => g.Count() > 1)
                .CountAsync();
            Assert.Equal(0, dups);

            // Session A renumber gap-free 1,2,3.
            var numsA = await readCtx.AssessmentPackages.AsNoTracking()
                .Where(p => p.AssessmentSessionId == sessionA)
                .Select(p => p.PackageNumber).OrderBy(n => n).ToListAsync();
            Assert.Equal(new[] { 1, 2, 3 }, numsA);

            // Session B renumber gap-free 1,2 (gap 5,9 dihapus).
            var numsB = await readCtx.AssessmentPackages.AsNoTracking()
                .Where(p => p.AssessmentSessionId == sessionB)
                .Select(p => p.PackageNumber).OrderBy(n => n).ToListAsync();
            Assert.Equal(new[] { 1, 2 }, numsB);
        }

        // RE-CREATE index agar DB test konsisten untuk test lain (best-effort; dedup global sudah jaga 0 dup).
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            await ctx.Database.ExecuteSqlRawAsync(DedupSql);   // pastikan SEMUA session 0-dup sebelum re-create
            await ctx.Database.ExecuteSqlRawAsync(
                $"CREATE UNIQUE INDEX [{IndexName}] ON [AssessmentPackages] ([AssessmentSessionId], [PackageNumber]);");
        }
    }
}
