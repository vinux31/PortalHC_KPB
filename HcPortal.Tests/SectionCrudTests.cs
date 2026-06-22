// Phase 415 SEC-01 (Wave-0) — data-layer integration tests untuk AssessmentPackageSection.
// Membuktikan, pada SQL Server NYATA (bukan InMemory), bahwa migration 415 menegakkan:
//   1. Unique index (AssessmentPackageId, SectionNumber) → duplikat melempar DbUpdateException
//      (Phase 404 lesson: non-filtered unique → DbUpdateException).
//   2. Distinct SectionNumber dalam satu paket boleh.
//   3. FK Question->Section = SET NULL: hapus Section men-set SectionId soal jadi NULL, soal TETAP ada.
//   4. PackageQuestion dengan SectionId=null persist normal (legacy / "Lainnya").
// Fokus murni data-layer (tanpa controller). Fresh DbContext per-assertion (mirror pola integration lain).
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class SectionCrudTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public SectionCrudTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ---- Seed helpers ----
    private static async Task<int> SeedPackageAsync(ApplicationDbContext ctx)
    {
        // AssessmentSession.UserId punya FK ke Users.Id → seed user dulu (pola FlexibleParticipantAddTests).
        var user = new ApplicationUser
        {
            UserName = "sec-" + Guid.NewGuid().ToString("N")[..8],
            Email = "sec@test.local",
            FullName = "Section Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var session = new AssessmentSession
        {
            UserId = user.Id,
            Title = "Sec-" + Guid.NewGuid().ToString("N")[..8],
            Category = "OJT",
            Status = "Open",
            AccessToken = "",
            Schedule = DateTime.UtcNow,
            DurationMinutes = 60,
            PassPercentage = 70,
            Progress = 0
        };
        ctx.AssessmentSessions.Add(session);
        await ctx.SaveChangesAsync();

        var pkg = new AssessmentPackage
        {
            AssessmentSessionId = session.Id,
            PackageName = "Paket A",
            PackageNumber = 1
        };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        return pkg.Id;
    }

    // (1) Unique index (AssessmentPackageId, SectionNumber) ditegakkan → duplikat = DbUpdateException.
    [Fact]
    public async Task DuplicateSectionNumber_SamePackage_ThrowsDbUpdateException()
    {
        int packageId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            seed.AssessmentPackageSections.Add(new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Pompa" });
            await seed.SaveChangesAsync();
        }

        await using var dup = NewCtx();
        dup.AssessmentPackageSections.Add(new AssessmentPackageSection
        { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Kompresor" });

        // Plain non-filtered unique → DbUpdateException (Phase 404 lesson).
        await Assert.ThrowsAsync<DbUpdateException>(() => dup.SaveChangesAsync());
    }

    // (2) SectionNumber berbeda dalam satu paket → boleh.
    [Fact]
    public async Task DistinctSectionNumbers_SamePackage_Succeed()
    {
        int packageId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            seed.AssessmentPackageSections.Add(new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Pompa" });
            seed.AssessmentPackageSections.Add(new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 2, Name = "Kompresor" });
            await seed.SaveChangesAsync();
        }

        await using var verify = NewCtx();
        var count = await verify.AssessmentPackageSections
            .CountAsync(s => s.AssessmentPackageId == packageId);
        Assert.Equal(2, count);
    }

    // (3) FK SetNull: hapus Section men-set SectionId soal jadi NULL — soal TIDAK ikut terhapus.
    [Fact]
    public async Task DeleteSection_SetsQuestionSectionIdToNull_QuestionsRemain()
    {
        int packageId, sectionId, q1Id, q2Id;

        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            var section = new AssessmentPackageSection
            { AssessmentPackageId = packageId, SectionNumber = 1, Name = "Pompa" };
            seed.AssessmentPackageSections.Add(section);
            await seed.SaveChangesAsync();
            sectionId = section.Id;

            var q1 = new PackageQuestion { AssessmentPackageId = packageId, QuestionText = "Q1", Order = 1, SectionId = sectionId };
            var q2 = new PackageQuestion { AssessmentPackageId = packageId, QuestionText = "Q2", Order = 2, SectionId = sectionId };
            seed.PackageQuestions.AddRange(q1, q2);
            await seed.SaveChangesAsync();
            q1Id = q1.Id; q2Id = q2.Id;
        }

        // Hapus Section.
        await using (var del = NewCtx())
        {
            var sec = await del.AssessmentPackageSections.FindAsync(sectionId);
            del.AssessmentPackageSections.Remove(sec!);
            await del.SaveChangesAsync();
        }

        // Reload: soal tetap ada, SectionId di-set NULL oleh FK SET NULL.
        await using var verify = NewCtx();
        var sectionGone = await verify.AssessmentPackageSections.AnyAsync(s => s.Id == sectionId);
        Assert.False(sectionGone);

        var q1Reload = await verify.PackageQuestions.FirstOrDefaultAsync(q => q.Id == q1Id);
        var q2Reload = await verify.PackageQuestions.FirstOrDefaultAsync(q => q.Id == q2Id);
        Assert.NotNull(q1Reload);                 // soal TIDAK terhapus
        Assert.NotNull(q2Reload);
        Assert.Null(q1Reload!.SectionId);         // SectionId di-set NULL
        Assert.Null(q2Reload!.SectionId);
    }

    // (4) PackageQuestion dengan SectionId=null persist normal (legacy / "Lainnya").
    [Fact]
    public async Task QuestionWithNullSection_PersistsFine()
    {
        int packageId, qId;
        await using (var seed = NewCtx())
        {
            packageId = await SeedPackageAsync(seed);
            var q = new PackageQuestion { AssessmentPackageId = packageId, QuestionText = "Lainnya", Order = 1, SectionId = null };
            seed.PackageQuestions.Add(q);
            await seed.SaveChangesAsync();
            qId = q.Id;
        }

        await using var verify = NewCtx();
        var reload = await verify.PackageQuestions.FirstOrDefaultAsync(q => q.Id == qId);
        Assert.NotNull(reload);
        Assert.Null(reload!.SectionId);
    }
}
