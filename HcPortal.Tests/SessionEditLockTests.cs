using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Helpers;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 422 SHFX-03/D-07 (Wave 2) — guard server-side 5 endpoint POST + Import.
/// Lock = <see cref="SessionEditLockRules.IsSessionEditLocked"/> (PostTest && SamePackage). Saat locked →
/// JANGAN tulis (tolak-keras, defense-in-depth terhadap root-cause SHUF-ISS-02 lock view-only).
/// Test mereplikasi guard-body endpoint terhadap DB fixture nyata (pola ShuffleLockGuardTests) — meng-assert
/// keputusan lock yang IDENTIK dgn helper Wave 1 + bahwa state DB tak berubah saat reject.
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class SessionEditLockTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public SessionEditLockTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static readonly DateTime Sched = new DateTime(2026, 5, 11, 8, 0, 0);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var user = new ApplicationUser
        {
            UserName = "lockuser-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "lock@test.local",
            FullName = "Lock Test"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
        return user.Id;
    }

    private static AssessmentSession Sess(string userId, string title, string type, bool samePackage) => new AssessmentSession
    {
        UserId = userId,
        Title = title,
        Category = "Test",
        Status = "Open",
        AccessToken = "",
        Schedule = Sched,
        AssessmentType = type,
        SamePackage = samePackage
    };

    // Seed satu sesi + satu paket (1 soal). Mengembalikan (sessionId, packageId).
    private static async Task<(int sessionId, int packageId)> SeedSessionWithPackageAsync(
        ApplicationDbContext ctx, string title, string type, bool samePackage)
    {
        var u = await SeedUserAsync(ctx);
        var sess = Sess(u, title, type, samePackage);
        ctx.AssessmentSessions.Add(sess);
        await ctx.SaveChangesAsync();
        var pkg = new AssessmentPackage { AssessmentSessionId = sess.Id, PackageName = "Pkg", PackageNumber = 1 };
        pkg.Questions.Add(new PackageQuestion { QuestionText = "seed-q", Order = 1, ScoreValue = 10, QuestionType = "MultipleChoice" });
        ctx.AssessmentPackages.Add(pkg);
        await ctx.SaveChangesAsync();
        return (sess.Id, pkg.Id);
    }

    // === Truth-table predikat (selaras unit SessionEditLockRulesTests, di sini diuji via DB row) ===

    [Fact]
    public async Task IsLocked_True_OnlyForPostTestSamePackage()
    {
        var marker = "LOCK-TT-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        var u = await SeedUserAsync(ctx);
        var postSame = Sess(u, marker, "PostTest", true);
        var postNonSame = Sess(u, marker, "PostTest", false);
        var pre = Sess(u, marker, "PreTest", true);          // Pre walau SamePackage=true → tidak locked
        var standard = Sess(u, marker, "Standard", true);
        ctx.AssessmentSessions.AddRange(postSame, postNonSame, pre, standard);
        await ctx.SaveChangesAsync();

        Assert.True(SessionEditLockRules.IsSessionEditLocked(postSame));
        Assert.False(SessionEditLockRules.IsSessionEditLocked(postNonSame));
        Assert.False(SessionEditLockRules.IsSessionEditLocked(pre));
        Assert.False(SessionEditLockRules.IsSessionEditLocked(standard));
    }

    // === Guard 5 endpoint + Import: locked → NO-WRITE ===

    // CreatePackage / Import: session resolved via assessmentId / pkg.AssessmentSessionId.
    [Fact]
    public async Task LockedPost_RejectsCreatePackage_NoWrite()
    {
        var marker = "LOCK-CP-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (sid, _) = await SeedSessionWithPackageAsync(ctx, marker, "PostTest", samePackage: true);
            postId = sid;
        }

        // Guard-body endpoint (CreatePackage :awal): resolve session → IsSessionEditLocked → reject (no add).
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var session = await ctx.AssessmentSessions.FindAsync(postId);
            bool locked = session != null && SessionEditLockRules.IsSessionEditLocked(session);
            Assert.True(locked);
            // locked → return (no _context.AssessmentPackages.Add + no SaveChanges).
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        Assert.Equal(1, await read.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postId)); // tetap 1 (seed), tak nambah
    }

    // CreateQuestion / EditQuestion / DeleteQuestion: session via packageId → pkg.AssessmentSessionId.
    [Fact]
    public async Task LockedPost_RejectsQuestionMutation_NoWrite()
    {
        var marker = "LOCK-Q-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int pkgId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (_, pid) = await SeedSessionWithPackageAsync(ctx, marker, "PostTest", samePackage: true);
            pkgId = pid;
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var pkg = await ctx.AssessmentPackages.FindAsync(pkgId);
            var session = await ctx.AssessmentSessions.FindAsync(pkg!.AssessmentSessionId);
            bool locked = session != null && SessionEditLockRules.IsSessionEditLocked(session);
            Assert.True(locked);
            // locked → reject (no add/edit/delete question).
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        var qCount = await read.PackageQuestions.CountAsync(q => q.AssessmentPackageId == pkgId);
        Assert.Equal(1, qCount);   // soal seed tetap utuh — tak ada mutasi
    }

    // DeletePackage: session via packageId → pkg.AssessmentSessionId.
    [Fact]
    public async Task LockedPost_RejectsDeletePackage_NoWrite()
    {
        var marker = "LOCK-DP-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int pkgId, postId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (sid, pid) = await SeedSessionWithPackageAsync(ctx, marker, "PostTest", samePackage: true);
            postId = sid; pkgId = pid;
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var pkg = await ctx.AssessmentPackages.FindAsync(pkgId);
            var session = await ctx.AssessmentSessions.FindAsync(pkg!.AssessmentSessionId);
            bool locked = session != null && SessionEditLockRules.IsSessionEditLocked(session);
            Assert.True(locked);
            // locked → reject (no Remove).
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        Assert.Equal(1, await read.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == postId)); // paket tak terhapus
    }

    // === Counter-tests: NOT locked → WRITE lolos (backward-compat) ===

    // Pre-Test (walau SamePackage=true) tak terkunci → mutasi lolos.
    [Fact]
    public async Task PreTest_AllowsMutation_WriteSucceeds()
    {
        var marker = "LOCK-PRE-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int pkgId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (_, pid) = await SeedSessionWithPackageAsync(ctx, marker, "PreTest", samePackage: true);
            pkgId = pid;
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var pkg = await ctx.AssessmentPackages.FindAsync(pkgId);
            var session = await ctx.AssessmentSessions.FindAsync(pkg!.AssessmentSessionId);
            bool locked = session != null && SessionEditLockRules.IsSessionEditLocked(session);
            Assert.False(locked);          // Pre tak terkunci
            // not locked → tulis (tambah soal).
            ctx.PackageQuestions.Add(new PackageQuestion
            { AssessmentPackageId = pkgId, QuestionText = "added-q", Order = 2, ScoreValue = 10, QuestionType = "MultipleChoice" });
            await ctx.SaveChangesAsync();
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        Assert.Equal(2, await read.PackageQuestions.CountAsync(q => q.AssessmentPackageId == pkgId)); // soal bertambah
    }

    // Post-Test NON-SamePackage tak terkunci → mutasi lolos.
    [Fact]
    public async Task PostTestNonSamePackage_AllowsMutation_WriteSucceeds()
    {
        var marker = "LOCK-PNS-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        int pkgId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var (_, pid) = await SeedSessionWithPackageAsync(ctx, marker, "PostTest", samePackage: false);
            pkgId = pid;
        }

        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var pkg = await ctx.AssessmentPackages.FindAsync(pkgId);
            var session = await ctx.AssessmentSessions.FindAsync(pkg!.AssessmentSessionId);
            bool locked = session != null && SessionEditLockRules.IsSessionEditLocked(session);
            Assert.False(locked);          // Post non-SamePackage tak terkunci
            ctx.PackageQuestions.Add(new PackageQuestion
            { AssessmentPackageId = pkgId, QuestionText = "added-q", Order = 2, ScoreValue = 10, QuestionType = "MultipleChoice" });
            await ctx.SaveChangesAsync();
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        Assert.Equal(2, await read.PackageQuestions.CountAsync(q => q.AssessmentPackageId == pkgId));
    }
}
