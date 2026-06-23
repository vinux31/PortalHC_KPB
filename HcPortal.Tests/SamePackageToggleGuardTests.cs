using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 422 SHFX-02/D-01 (Wave 3) — real-SQL: toggle SamePackage pasca-create
/// (<c>AssessmentAdminController.ToggleSamePackage(assessmentId, samePackage)</c>).
/// Endpoint butuh HTTP/antiforgery context; test mereplikasi KONTRAK keputusan-nya 1:1 (guard anyStarted,
/// ON sync + clear stale UPA, OFF keep clone) terhadap DB fixture nyata, lalu meng-assert state DB.
/// Mengikat perilaku endpoint SEBELUM gerbang UAT (pola SamePackageSyncTests / ShuffleLockGuardTests).
///
/// Cakupan:
///  A — ON sync: grup belum-mulai, Post.SamePackage=false, Pre punya paket → ON → Post.SamePackage==true + paket Post == Pre.
///  B — OFF keep: Post.SamePackage=true + paket clone ada → OFF → SamePackage==false + paket Post MASIH ADA (Pitfall 5).
///  C — guard reject anyStarted: peserta StartedAt!=null di grup → SamePackage TIDAK berubah.
///  D — allow belum-mulai: grup belum-mulai → toggle ALLOW (berubah).
///  E — Open Q2 dangling: belum-mulai TAPI ada UPA Post → ON sync → TIDAK ada UPA dangling.
///  F — non-Post / non-paired reject: toggle pada Pre/Standard → no change.
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class SamePackageToggleGuardTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public SamePackageToggleGuardTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static readonly DateTime Sched = new DateTime(2026, 5, 11, 8, 0, 0);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "toguser-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "toggle@test.local",
            FullName = "Toggle Test"
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
    /// Kontrak endpoint ToggleSamePackage direplikasi 1:1 (logika identik controller). Mengembalikan
    /// (ok, success) — ok=false bila ditolak guard / non-paired. Test meng-assert state DB setelahnya.
    /// </summary>
    private static async Task<bool> ToggleSamePackageAsync(ApplicationDbContext ctx, int assessmentId, bool samePackage)
    {
        var post = await ctx.AssessmentSessions.FindAsync(assessmentId);
        if (post == null) return false;
        if (post.AssessmentType != "PostTest" || !post.LinkedSessionId.HasValue) return false;   // non-paired reject

        var groupIds = new[] { post.Id, post.LinkedSessionId.Value };
        bool anyStarted = await ctx.AssessmentSessions
            .AnyAsync(s => groupIds.Contains(s.Id) &&
                           (s.StartedAt != null || s.Status == "InProgress" || s.Status == "Completed"));
        if (anyStarted) return false;   // guard reject (no write)

        post.SamePackage = samePackage;
        post.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        if (samePackage)
        {
            // Open Q2: clear stale Post UPA SEBELUM sync (cegah dangling assignment ke paket Post yang dihapus).
            var stalePostAssignments = await ctx.UserPackageAssignments
                .Where(a => a.AssessmentSessionId == post.Id)
                .ToListAsync();
            if (stalePostAssignments.Any())
                ctx.UserPackageAssignments.RemoveRange(stalePostAssignments);
            await ctx.SaveChangesAsync();

            // ON → sync Pre→Post (helper kontrak — pre = LinkedSessionId).
            await SyncToLinkedPostIfSamePackageAsync(ctx, post.LinkedSessionId.Value);
        }
        // OFF → KEEP paket clone (Pitfall 5: tak ada sync/delete).
        return true;
    }

    // Mirror SyncToLinkedPostIfSamePackageAsync + SyncPackagesToPost (controller :5958/:5891).
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

    /// <summary>Seed grup Pre-Post ter-link; Pre punya 2 paket (2+3 soal). Post.SamePackage = arg.</summary>
    private static async Task<(int preId, int postId)> SeedLinkedPrePostAsync(
        ApplicationDbContext ctx, string marker, bool postSamePackage, bool seedPostPackages = false)
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

        var pkg1 = new AssessmentPackage { AssessmentSessionId = pre.Id, PackageName = "P1", PackageNumber = 1 };
        var pkg2 = new AssessmentPackage { AssessmentSessionId = pre.Id, PackageName = "P2", PackageNumber = 2 };
        for (int i = 0; i < 2; i++)
            pkg1.Questions.Add(new PackageQuestion { QuestionText = $"q1-{i}", Order = i + 1, ScoreValue = 10, QuestionType = "MultipleChoice" });
        for (int i = 0; i < 3; i++)
            pkg2.Questions.Add(new PackageQuestion { QuestionText = $"q2-{i}", Order = i + 1, ScoreValue = 10, QuestionType = "MultipleChoice" });
        ctx.AssessmentPackages.AddRange(pkg1, pkg2);
        await ctx.SaveChangesAsync();

        if (seedPostPackages)
        {
            // Paket Post lama (akan ditimpa saat ON sync) — untuk test OFF-keep + dangling UPA.
            var postPkg = new AssessmentPackage { AssessmentSessionId = post.Id, PackageName = "OldPost", PackageNumber = 1 };
            postPkg.Questions.Add(new PackageQuestion { QuestionText = "oldq", Order = 1, ScoreValue = 10, QuestionType = "MultipleChoice" });
            ctx.AssessmentPackages.Add(postPkg);
            await ctx.SaveChangesAsync();
        }

        return (pre.Id, post.Id);
    }

    // Test A — ON sync: belum-mulai, Post.SamePackage=false → ON → SamePackage=true + paket Post == Pre.
    [Fact]
    public async Task ToggleOn_NoStarted_SyncsPackagesAndSetsTrue()
    {
        var marker = "TOG-A-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int preId, postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
            (preId, postId) = await SeedLinkedPrePostAsync(ctx, marker, postSamePackage: false);

        bool ok;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
            ok = await ToggleSamePackageAsync(ctx, postId, samePackage: true);

        Assert.True(ok);
        await using var read = new ApplicationDbContext(_fixture.Options);
        var post = await read.AssessmentSessions.FindAsync(postId);
        Assert.True(post!.SamePackage);
        var prePkgs = await read.AssessmentPackages.Include(p => p.Questions)
            .Where(p => p.AssessmentSessionId == preId).OrderBy(p => p.PackageNumber).ToListAsync();
        var postPkgs = await read.AssessmentPackages.Include(p => p.Questions)
            .Where(p => p.AssessmentSessionId == postId).OrderBy(p => p.PackageNumber).ToListAsync();
        Assert.Equal(2, postPkgs.Count);
        Assert.Equal(prePkgs.Sum(p => p.Questions.Count), postPkgs.Sum(p => p.Questions.Count));  // 5
    }

    // Test B — OFF keep: Post.SamePackage=true + paket clone ada → OFF → SamePackage=false + paket MASIH ADA.
    [Fact]
    public async Task ToggleOff_KeepsClonedPackages()
    {
        var marker = "TOG-B-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (preId, pid) = await SeedLinkedPrePostAsync(ctx, marker, postSamePackage: true);
            postId = pid;
            // Sync dulu supaya Post punya paket clone.
            await SyncToLinkedPostIfSamePackageAsync(ctx, preId);
        }

        int postPkgCountBefore;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
            postPkgCountBefore = await ctx.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postId);
        Assert.Equal(2, postPkgCountBefore);   // clone hadir sebelum OFF

        bool ok;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
            ok = await ToggleSamePackageAsync(ctx, postId, samePackage: false);

        Assert.True(ok);
        await using var read = new ApplicationDbContext(_fixture.Options);
        var post = await read.AssessmentSessions.FindAsync(postId);
        Assert.False(post!.SamePackage);   // lock dilepas
        var postPkgCount = await read.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postId);
        Assert.Equal(2, postPkgCount);     // paket clone DIPERTAHANKAN (Pitfall 5)
    }

    // Test C — guard reject: peserta StartedAt!=null di grup → SamePackage TIDAK berubah.
    [Fact]
    public async Task Toggle_AnyStarted_RejectsNoChange()
    {
        var marker = "TOG-C-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (preId, pid) = await SeedLinkedPrePostAsync(ctx, marker, postSamePackage: false);
            postId = pid;
            // Set salah satu sesi grup sudah-mulai (Pre StartedAt).
            var pre = await ctx.AssessmentSessions.FindAsync(preId);
            pre!.StartedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        bool ok;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
            ok = await ToggleSamePackageAsync(ctx, postId, samePackage: true);

        Assert.False(ok);   // ditolak guard
        await using var read = new ApplicationDbContext(_fixture.Options);
        var post = await read.AssessmentSessions.FindAsync(postId);
        Assert.False(post!.SamePackage);   // TIDAK berubah
        var postPkgCount = await read.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postId);
        Assert.Equal(0, postPkgCount);     // tak ada sync (no write)
    }

    // Test D — allow belum-mulai: grup belum-mulai → toggle ALLOW (SamePackage berubah).
    [Fact]
    public async Task Toggle_NoStarted_Allowed()
    {
        var marker = "TOG-D-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (_, pid) = await SeedLinkedPrePostAsync(ctx, marker, postSamePackage: false);
            postId = pid;
        }

        bool ok;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
            ok = await ToggleSamePackageAsync(ctx, postId, samePackage: true);

        Assert.True(ok);   // belum-mulai → boleh
        await using var read = new ApplicationDbContext(_fixture.Options);
        var post = await read.AssessmentSessions.FindAsync(postId);
        Assert.True(post!.SamePackage);
    }

    // Test E — Open Q2 dangling: belum-mulai TAPI ada UPA Post → ON sync → TIDAK ada UPA dangling.
    [Fact]
    public async Task ToggleOn_ClearsStalePostAssignments_NoDangling()
    {
        var marker = "TOG-E-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int postId; int stalePkgId; string userId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (_, pid) = await SeedLinkedPrePostAsync(ctx, marker, postSamePackage: false, seedPostPackages: true);
            postId = pid;
            // Ambil paket Post lama + buat UPA yang menunjuk paket Post lama (akan dihapus saat ON sync).
            var oldPostPkg = await ctx.AssessmentPackages.FirstAsync(p => p.AssessmentSessionId == postId);
            stalePkgId = oldPostPkg.Id;
            var post = await ctx.AssessmentSessions.FindAsync(postId);
            userId = post!.UserId;
            ctx.UserPackageAssignments.Add(new UserPackageAssignment
            {
                AssessmentSessionId = postId,
                AssessmentPackageId = stalePkgId,
                UserId = userId
            });
            await ctx.SaveChangesAsync();
        }

        bool ok;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
            ok = await ToggleSamePackageAsync(ctx, postId, samePackage: true);

        Assert.True(ok);
        await using var read = new ApplicationDbContext(_fixture.Options);
        // Tak ada UPA yang menunjuk paket Post yang sudah dihapus (dangling).
        var allPostPkgIds = await read.AssessmentPackages
            .Where(p => p.AssessmentSessionId == postId).Select(p => p.Id).ToListAsync();
        var postUpas = await read.UserPackageAssignments
            .Where(a => a.AssessmentSessionId == postId).ToListAsync();
        Assert.All(postUpas, a => Assert.Contains(a.AssessmentPackageId, allPostPkgIds));
        // Stale UPA (ke paket lama yang dihapus) tak boleh tersisa.
        Assert.DoesNotContain(postUpas, a => a.AssessmentPackageId == stalePkgId);
    }

    // Test F — non-Post / non-paired reject: toggle pada Pre → no change, ok=false.
    [Fact]
    public async Task Toggle_NonPaired_Rejects()
    {
        var marker = "TOG-F-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int preId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (pid, _) = await SeedLinkedPrePostAsync(ctx, marker, postSamePackage: false);
            preId = pid;
        }

        bool ok;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
            ok = await ToggleSamePackageAsync(ctx, preId, samePackage: true);   // panggil dgn Pre id

        Assert.False(ok);   // bukan PostTest → ditolak
        await using var read = new ApplicationDbContext(_fixture.Options);
        var pre = await read.AssessmentSessions.FindAsync(preId);
        Assert.False(pre!.SamePackage);   // tak berubah
    }
}
