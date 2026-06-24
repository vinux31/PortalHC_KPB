// Phase 419 D-03 / DEF-416-01 / IN-01 (Wave-0 RED) — kontrak re-spec peringatan cakupan Elemen Teknis (ET).
// Drive REAL AssessmentAdminController.ManagePackageQuestions GET atas SQL Server NYATA (SectionFixture).
// Predikat LAMA (DistinctEt > K dalam 1 paket) TAK PERNAH fire (tiap soal = 1 ET -> distinct <= jumlah soal).
// Re-spec (Plan 04): DistinctEt = distinct ET pool soal SectionNumber=N LINTAS paket-saudara (sibling = package
// lain dalam session yang sama); K = min(count soal SectionNumber=N antar sibling). Fire bila DistinctEt > K.
// Matching lintas-sibling by SectionNumber (IN-01, bukan SectionId). Tetap NON-BLOCKING.
//
// RED: CrossSiblingPool_Fires + GroupBySectionNumber_NotSectionId (predikat lama tak fire / per-paket).
// GREEN: FullCoverage_NoWarning_NonBlocking (kunci semantik non-blocking tetap utuh sebelum & sesudah Plan 04).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class SectionEtWarningTests : IClassFixture<SectionFixture>
{
    private readonly SectionFixture _fixture;
    public SectionEtWarningTests(SectionFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    // ManagePackageQuestions GET hanya memakai _context; deps lain null!/no-op (base ctor cuma menyimpan).
    private AssessmentAdminController MakeController(ApplicationDbContext ctx)
    {
        var httpContext = new DefaultHttpContext();
        #pragma warning disable CS8625
        var ctrl = new AssessmentAdminController(
            context:                ctx,
            userManager:            null!,
            auditLog:               new AuditLogService(ctx),
            env:                    null!,
            cache:                  new MemoryCache(new MemoryCacheOptions()),
            logger:                 NullLogger<AssessmentAdminController>.Instance,
            notificationService:    null!,
            hubContext:             new NoopHubContext(),
            workerDataService:      null!,
            gradingService:         null!,
            protonCompletionService: null!,
            protonBypassService:    null!);
        #pragma warning restore CS8625
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            ActionDescriptor = new ControllerActionDescriptor { ActionName = "ManagePackageQuestions" }
        };
        ctrl.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        return ctrl;
    }

    private static async Task<int> SeedAssessmentAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser
        {
            UserName = "wkr-" + Guid.NewGuid().ToString("N")[..8], Email = "wkr@test.local",
            FullName = "Worker Test", NIP = "12345", IsActive = true, RoleLevel = 6
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();

        var s = new AssessmentSession
        {
            UserId = u.Id, Title = "ET-" + Guid.NewGuid().ToString("N")[..8],
            Category = "OJT", AssessmentType = "Standard", Status = "Draft",
            AccessToken = "", IsTokenRequired = false, Schedule = DateTime.UtcNow,
            DurationMinutes = 60, PassPercentage = 70, Progress = 0,
            ShuffleQuestions = false, ShuffleOptions = false
        };
        ctx.AssessmentSessions.Add(s);
        await ctx.SaveChangesAsync();
        return s.Id;
    }

    // 1 package (pkgNumber) ke session dengan 1 Section (sectionNumber/name) berisi soal ber-ET sesuai array.
    private static async Task<int> AddPackageWithEtAsync(ApplicationDbContext ctx, int sessionId, int pkgNumber,
        int sectionNumber, string sectionName, string[] elemenTeknis)
    {
        var pkg = new AssessmentPackage { AssessmentSessionId = sessionId, PackageName = $"Paket {pkgNumber}", PackageNumber = pkgNumber };
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();

        var sec = new AssessmentPackageSection { AssessmentPackageId = pkg.Id, SectionNumber = sectionNumber, Name = sectionName };
        ctx.AssessmentPackageSections.Add(sec);
        await ctx.SaveChangesAsync();

        int order = 1;
        foreach (var et in elemenTeknis)
        {
            ctx.PackageQuestions.Add(new PackageQuestion
            {
                AssessmentPackageId = pkg.Id, QuestionText = $"P{pkgNumber}-{et}", Order = order++,
                ScoreValue = 10, QuestionType = "MultipleChoice", SectionId = sec.Id, ElemenTeknis = et,
                Options = new List<PackageOption> { new() { OptionText = "A", IsCorrect = true }, new() { OptionText = "B", IsCorrect = false } }
            });
        }
        await ctx.SaveChangesAsync();
        return pkg.Id;
    }

    private static List<AssessmentAdminController.SectionEtWarning> WarningsFrom(AssessmentAdminController ctrl)
        => (ctrl.ViewBag.SectionEtWarnings as IEnumerable<AssessmentAdminController.SectionEtWarning>)?.ToList()
           ?? new List<AssessmentAdminController.SectionEtWarning>();

    // RED: pool ET lintas-sibling Section 1 = {A,B,C,D}=4 > K=min(2,2)=2 -> fire. Predikat lama (per-paket) tak fire.
    [Fact]
    public async Task CrossSiblingPool_Fires()
    {
        int pkg1Id;
        await using (var seed = NewCtx())
        {
            var sid = await SeedAssessmentAsync(seed);
            pkg1Id = await AddPackageWithEtAsync(seed, sid, 1, 1, "Sec1", new[] { "ET-A", "ET-B" });
            await AddPackageWithEtAsync(seed, sid, 2, 1, "Sec1", new[] { "ET-C", "ET-D" });
        }

        await using var ctx = NewCtx();
        var ctrl = MakeController(ctx);
        await ctrl.ManagePackageQuestions(pkg1Id);

        Assert.Contains(WarningsFrom(ctrl), w => w.SectionNumber == 1);
    }

    // RED: dua sibling SectionNumber=1 (AssessmentPackageSection.Id berbeda) -> warning dikelompokkan by
    // SectionNumber (1 entry untuk Section 1), bukan per-Id. Predikat lama: 0 entry.
    [Fact]
    public async Task GroupBySectionNumber_NotSectionId()
    {
        int pkg1Id;
        await using (var seed = NewCtx())
        {
            var sid = await SeedAssessmentAsync(seed);
            pkg1Id = await AddPackageWithEtAsync(seed, sid, 1, 1, "Sec1", new[] { "ET-A", "ET-B" });
            await AddPackageWithEtAsync(seed, sid, 2, 1, "Sec1", new[] { "ET-C", "ET-D" });
        }

        await using var ctx = NewCtx();
        var ctrl = MakeController(ctx);
        await ctrl.ManagePackageQuestions(pkg1Id);

        Assert.Equal(1, WarningsFrom(ctrl).Count(w => w.SectionNumber == 1));
    }

    // GREEN sekarang DAN setelah Plan 04: tiap sibling pakai ET SAMA -> pool distinct=2, K=min(2,2)=2 -> tak fire.
    // Kunci semantik NON-BLOCKING (warning hanya muncul saat cakupan ET benar-benar kurang).
    [Fact]
    public async Task FullCoverage_NoWarning_NonBlocking()
    {
        int pkg1Id;
        await using (var seed = NewCtx())
        {
            var sid = await SeedAssessmentAsync(seed);
            pkg1Id = await AddPackageWithEtAsync(seed, sid, 1, 1, "Sec1", new[] { "ET-A", "ET-B" });
            await AddPackageWithEtAsync(seed, sid, 2, 1, "Sec1", new[] { "ET-A", "ET-B" });
        }

        await using var ctx = NewCtx();
        var ctrl = MakeController(ctx);
        await ctrl.ManagePackageQuestions(pkg1Id);

        Assert.DoesNotContain(WarningsFrom(ctrl), w => w.SectionNumber == 1);
    }

    // REGRESSION (code-review 419 verify lens): sibling dgn ET BERULANG (3x ET-A). Pool distinct lintas-sibling =
    // {A,B,C}=3. K LAMA (raw count soal) = min(3,3)=3 → 3>3 FALSE → warning gagal fire (false-negative) walau peserta
    // paket-2 cuma pernah lihat ET-A. K BENAR (distinct-ET per-paket) = min(3,1)=1 → 3>1 → fire. Mengunci fix unit-mismatch.
    [Fact]
    public async Task RepeatedEtInSibling_Fires()
    {
        int pkg1Id;
        await using (var seed = NewCtx())
        {
            var sid = await SeedAssessmentAsync(seed);
            pkg1Id = await AddPackageWithEtAsync(seed, sid, 1, 1, "Sec1", new[] { "ET-A", "ET-B", "ET-C" });
            await AddPackageWithEtAsync(seed, sid, 2, 1, "Sec1", new[] { "ET-A", "ET-A", "ET-A" });
        }

        await using var ctx = NewCtx();
        var ctrl = MakeController(ctx);
        await ctrl.ManagePackageQuestions(pkg1Id);

        Assert.Contains(WarningsFrom(ctrl), w => w.SectionNumber == 1);
    }
}
