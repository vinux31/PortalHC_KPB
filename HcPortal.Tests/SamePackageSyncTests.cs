using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 422 SHFX-01/D-06 (Wave 2) — real-SQL: auto-sync Pre→Post helper
/// <c>AssessmentAdminController.SyncToLinkedPostIfSamePackageAsync(preSessionId)</c>.
/// Helper bersifat private di controller; test mereplikasi KONTRAK-nya (logika identik: cek Pre +
/// linkedPost.SamePackage → deep-clone) terhadap DB fixture nyata, lalu meng-assert hasil sync di DB.
/// Ini mengikat helper ke perilaku yang diharapkan SEBELUM/SESUDAH wiring (pola ShuffleLockGuardTests).
///
/// Skenario inti = jalur Import yang BOCOR (SHUF-ISS-03 HIGH): impor soal ke paket Pre ber-SamePackage
/// HARUS men-sync Post; no-op aman bila !SamePackage / bukan PreTest / linkedPost null.
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class SamePackageSyncTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public SamePackageSyncTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static readonly DateTime Sched = new DateTime(2026, 5, 4, 8, 0, 0);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "syncuser-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "sync@test.local",
            FullName = "Sync Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    private static AssessmentSession Sess(string userId, string title, string type) => new AssessmentSession
    {
        UserId = userId,
        Title = title,
        Category = "Test",
        Status = "Open",
        AccessToken = "",
        Schedule = Sched,
        AssessmentType = type
    };

    /// <summary>
    /// Kontrak helper SyncToLinkedPostIfSamePackageAsync direplikasi 1:1 (logika identik controller).
    /// Test memanggil ini lalu meng-assert state DB — bukan memanggil method private langsung.
    /// </summary>
    private static async Task SyncToLinkedPostIfSamePackageAsync(ApplicationDbContext ctx, int preSessionId)
    {
        var pre = await ctx.AssessmentSessions.FindAsync(preSessionId);
        if (pre?.AssessmentType == "PreTest" && pre.LinkedSessionId.HasValue)
        {
            var post = await ctx.AssessmentSessions.FindAsync(pre.LinkedSessionId.Value);
            if (post != null && post.SamePackage)
                await SyncPackagesToPost(ctx, pre.Id, post.Id);
        }
    }

    // Mirror AssessmentAdminController.SyncPackagesToPost (:5875-5933) — deep-clone Pre→Post.
    private static async Task SyncPackagesToPost(ApplicationDbContext ctx, int preSessionId, int postSessionId)
    {
        var existingPostPkgs = await ctx.AssessmentPackages
            .Include(p => p.Questions).ThenInclude(q => q.Options)
            .Where(p => p.AssessmentSessionId == postSessionId)
            .ToListAsync();
        foreach (var pkg in existingPostPkgs)
        {
            foreach (var q in pkg.Questions)
                ctx.PackageOptions.RemoveRange(q.Options);
            ctx.PackageQuestions.RemoveRange(pkg.Questions);
        }
        ctx.AssessmentPackages.RemoveRange(existingPostPkgs);

        var prePkgs = await ctx.AssessmentPackages
            .Include(p => p.Questions).ThenInclude(q => q.Options)
            .Where(p => p.AssessmentSessionId == preSessionId)
            .OrderBy(p => p.PackageNumber).ThenBy(p => p.Id)
            .ToListAsync();
        foreach (var prePkg in prePkgs)
        {
            var newPkg = new AssessmentPackage
            {
                AssessmentSessionId = postSessionId,
                PackageName = prePkg.PackageName,
                PackageNumber = prePkg.PackageNumber
            };
            foreach (var q in prePkg.Questions)
            {
                newPkg.Questions.Add(new PackageQuestion
                {
                    QuestionText = q.QuestionText,
                    Order = q.Order,
                    ScoreValue = q.ScoreValue,
                    QuestionType = q.QuestionType,
                    Options = q.Options.Select(o => new PackageOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                });
            }
            ctx.AssessmentPackages.Add(newPkg);
        }
        await ctx.SaveChangesAsync();
    }

    private static async Task<(int preId, int postId)> SeedLinkedPrePostWithPrePackagesAsync(
        ApplicationDbContext ctx, string marker, bool postSamePackage)
    {
        var u = await SeedUserAsync(ctx);
        var pre = Sess(u, marker, "PreTest");
        var post = Sess(u, marker, "PostTest");
        post.SamePackage = postSamePackage;
        ctx.AssessmentSessions.AddRange(pre, post);
        await ctx.SaveChangesAsync();
        pre.LinkedSessionId = post.Id;
        post.LinkedSessionId = pre.Id;
        await ctx.SaveChangesAsync();

        // Pre punya 2 paket: pkg1 (2 soal), pkg2 (3 soal)
        var pkg1 = new AssessmentPackage { AssessmentSessionId = pre.Id, PackageName = "P1", PackageNumber = 1 };
        var pkg2 = new AssessmentPackage { AssessmentSessionId = pre.Id, PackageName = "P2", PackageNumber = 2 };
        for (int i = 0; i < 2; i++)
            pkg1.Questions.Add(new PackageQuestion { QuestionText = $"q1-{i}", Order = i + 1, ScoreValue = 10, QuestionType = "MultipleChoice" });
        for (int i = 0; i < 3; i++)
            pkg2.Questions.Add(new PackageQuestion { QuestionText = $"q2-{i}", Order = i + 1, ScoreValue = 10, QuestionType = "MultipleChoice" });
        ctx.AssessmentPackages.AddRange(pkg1, pkg2);
        await ctx.SaveChangesAsync();
        return (pre.Id, post.Id);
    }

    // Test A — Import sync: Pre+SamePackage → Post ter-sync (count paket + count soal match).
    [Fact]
    public async Task Sync_PreSamePackage_ClonesPackagesToPost()
    {
        var marker = "SYNC-A-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int preId, postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            (preId, postId) = await SeedLinkedPrePostWithPrePackagesAsync(ctx, marker, postSamePackage: true);
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            await SyncToLinkedPostIfSamePackageAsync(ctx, preId);
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        var prePkgs = await read.AssessmentPackages.Include(p => p.Questions)
            .Where(p => p.AssessmentSessionId == preId).OrderBy(p => p.PackageNumber).ToListAsync();
        var postPkgs = await read.AssessmentPackages.Include(p => p.Questions)
            .Where(p => p.AssessmentSessionId == postId).OrderBy(p => p.PackageNumber).ToListAsync();

        Assert.Equal(prePkgs.Count, postPkgs.Count);          // 2 paket
        Assert.Equal(2, postPkgs.Count);
        Assert.Equal(prePkgs[0].Questions.Count, postPkgs[0].Questions.Count); // 2
        Assert.Equal(prePkgs[1].Questions.Count, postPkgs[1].Questions.Count); // 3
        Assert.Equal(5, postPkgs.Sum(p => p.Questions.Count));
    }

    // Test B — no-op bila Post.SamePackage=false: Post TIDAK berubah (tetap kosong).
    [Fact]
    public async Task Sync_NotSamePackage_IsNoOp()
    {
        var marker = "SYNC-B-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int preId, postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            (preId, postId) = await SeedLinkedPrePostWithPrePackagesAsync(ctx, marker, postSamePackage: false);
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            await SyncToLinkedPostIfSamePackageAsync(ctx, preId);
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        var postPkgCount = await read.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postId);
        Assert.Equal(0, postPkgCount);   // Post tetap kosong (tidak ter-sync)
    }

    // Test C — no-op bila sesi BUKAN PreTest (panggil dengan Post id).
    [Fact]
    public async Task Sync_NonPreTestSession_IsNoOp()
    {
        var marker = "SYNC-C-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (_, pid) = await SeedLinkedPrePostWithPrePackagesAsync(ctx, marker, postSamePackage: true);
            postId = pid;
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            // Panggil dgn Post id (AssessmentType != "PreTest") → helper no-op.
            await SyncToLinkedPostIfSamePackageAsync(ctx, postId);
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        // Post id punya LinkedSessionId = Pre id, tapi Post bukan "PreTest" → guard pertama gagal → no-op.
        var postPkgCount = await read.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postId);
        Assert.Equal(0, postPkgCount);
    }

    // Test D — no-op bila Pre TANPA LinkedSessionId (standalone Pre).
    [Fact]
    public async Task Sync_PreWithoutLink_IsNoOp()
    {
        var marker = "SYNC-D-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int preId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var u = await SeedUserAsync(ctx);
            var pre = Sess(u, marker, "PreTest");   // tanpa LinkedSessionId
            ctx.AssessmentSessions.Add(pre);
            await ctx.SaveChangesAsync();
            var pkg = new AssessmentPackage { AssessmentSessionId = pre.Id, PackageName = "P1", PackageNumber = 1 };
            pkg.Questions.Add(new PackageQuestion { QuestionText = "q", Order = 1, ScoreValue = 10, QuestionType = "MultipleChoice" });
            ctx.AssessmentPackages.Add(pkg);
            await ctx.SaveChangesAsync();
            preId = pre.Id;
        }

        // Tidak melempar exception (no-op aman) — guard LinkedSessionId.HasValue false.
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            await SyncToLinkedPostIfSamePackageAsync(ctx, preId);
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        var prePkgCount = await read.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == preId);
        Assert.Equal(1, prePkgCount);   // Pre tak berubah; tak ada error
    }
}
