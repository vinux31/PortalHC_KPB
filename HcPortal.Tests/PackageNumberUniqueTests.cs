using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 422 SHFX-05/D-02 (Wave 1) — real-SQL: PackageNumber unik + deterministik.
/// Test A: MAX(PackageNumber)+1 setelah hapus paket tengah TIDAK bentrok dengan nomor existing
///         (bukti perbaikan bug count-based existingCount+1).
/// Test B: unique index (AssessmentSessionId, PackageNumber) menolak baris duplikat -> DbUpdateException
///         (jaring pengaman DB-level dari migration AddPackageNumberUniqueIndex).
///
/// [Trait("Category","Integration")] -> skip via dotnet test --filter "Category!=Integration".
/// Fixture menjalankan MigrateAsync penuh -> index unik HADIR di DB test.
/// </summary>
[Trait("Category", "Integration")]
public class PackageNumberUniqueTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public PackageNumberUniqueTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<int> SeedSessionAsync(ApplicationDbContext ctx, string title)
    {
        var user = new ApplicationUser
        {
            UserName = "pkgnum-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "pkgnum@test.local",
            FullName = "PackageNumber Test"
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
            Schedule = new DateTime(2026, 5, 1, 8, 0, 0)
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();
        return session.Id;
    }

    // Test A — MAX+1 anti-bentrok: hapus paket tengah lalu insert baru -> nomor baru == MAX+1 (bukan count+1).
    [Fact]
    public async Task NewPackageNumber_IsMaxPlusOne_AfterMiddleDelete()
    {
        var marker = "TAG-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int sessionId;

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            sessionId = await SeedSessionAsync(ctx, marker);
            ctx.AssessmentPackages.AddRange(
                new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "P1", PackageNumber = 1 },
                new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "P2", PackageNumber = 2 },
                new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = "P3", PackageNumber = 3 });
            await ctx.SaveChangesAsync();
        }

        // Hapus paket TENGAH (PackageNumber 2). Count turun jadi 2 -> count-based akan hasilkan 3 (BENTROK dgn P3).
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var mid = await ctx.AssessmentPackages
                .FirstAsync(p => p.AssessmentSessionId == sessionId && p.PackageNumber == 2);
            ctx.AssessmentPackages.Remove(mid);
            await ctx.SaveChangesAsync();
        }

        // Replikasi logic CreatePackage MAX+1 (server-computed).
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var maxNumber = await ctx.AssessmentPackages
                .Where(p => p.AssessmentSessionId == sessionId)
                .Select(p => (int?)p.PackageNumber)
                .MaxAsync();
            int next = (maxNumber ?? 0) + 1;

            Assert.Equal(4, next);   // MAX(3)+1 = 4, BUKAN count(2)+1 = 3 yang bentrok dgn P3

            var newPkg = new AssessmentPackage
            {
                AssessmentSessionId = sessionId,
                PackageName = "P4",
                PackageNumber = next
            };
            ctx.AssessmentPackages.Add(newPkg);
            await ctx.SaveChangesAsync();   // sukses (4 unik) — bukti tak bentrok
        }

        await using var readCtx = new ApplicationDbContext(_fixture.Options);
        var nums = await readCtx.AssessmentPackages.AsNoTracking()
            .Where(p => p.AssessmentSessionId == sessionId)
            .Select(p => p.PackageNumber).OrderBy(n => n).ToListAsync();
        Assert.Equal(new[] { 1, 3, 4 }, nums);   // 2 dihapus; 4 ditambah tanpa bentrok
    }

    // Test B — unique index menolak (AssessmentSessionId, PackageNumber) duplikat.
    [Fact]
    public async Task UniqueIndex_RejectsDuplicatePackageNumber()
    {
        var marker = "TAG-" + Guid.NewGuid().ToString("N").Substring(0, 8);

        await using var ctx = new ApplicationDbContext(_fixture.Options);
        int sessionId = await SeedSessionAsync(ctx, marker);

        ctx.AssessmentPackages.Add(new AssessmentPackage
        {
            AssessmentSessionId = sessionId,
            PackageName = "Dup1",
            PackageNumber = 1
        });
        await ctx.SaveChangesAsync();

        // Insert kedua dengan (sessionId, 1) IDENTIK -> harus ditolak unique index.
        ctx.AssessmentPackages.Add(new AssessmentPackage
        {
            AssessmentSessionId = sessionId,
            PackageName = "Dup2",
            PackageNumber = 1
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }
}
