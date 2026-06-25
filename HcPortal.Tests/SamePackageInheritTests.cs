using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// v32.7 Phase 422 SHFX-04/PA-02 (Wave 2) — peserta baru di grup Pre-Post warisi SamePackage = repPost.SamePackage.
/// Bug: newPost (AssessmentAdminController :2024-2046) tak set SamePackage → default false (inkonsisten grup).
/// Fix: tambah <c>SamePackage = repPost.SamePackage</c>. repPost = postGroup.First() (existing peserta).
///
/// Test mereplikasi konstruksi newPost (mirror controller initializer) terhadap DB fixture nyata,
/// meng-assert nilai SamePackage yang ter-persist == repPost.SamePackage (true & counter false).
///
/// [Trait("Category","Integration")] → CI SQL-less skip via dotnet test --filter "Category!=Integration".
/// </summary>
[Trait("Category", "Integration")]
public class SamePackageInheritTests : IClassFixture<ProtonCompletionFixture>
{
    private readonly ProtonCompletionFixture _fixture;

    public SamePackageInheritTests(ProtonCompletionFixture fixture)
    {
        _fixture = fixture;
    }

    private static readonly DateTime Sched = new DateTime(2026, 5, 25, 8, 0, 0);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx, string tag)
    {
        var user = new ApplicationUser
        {
            UserName = tag + "-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Email = "inherit@test.local",
            FullName = "Inherit Test"
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

    // Replika konstruksi newPost (subset relevan) — mirror AssessmentAdminController :2024-2046.
    // KUNCI fix: SamePackage = repPost.SamePackage.
    private static AssessmentSession BuildNewPost(AssessmentSession repPost, string newUserId, string title) =>
        new AssessmentSession
        {
            Title = title,
            Category = repPost.Category,
            Schedule = repPost.Schedule,
            Status = "Upcoming",
            AccessToken = "",
            UserId = newUserId,
            AssessmentType = "PostTest",
            BannerColor = repPost.BannerColor,
            SamePackage = repPost.SamePackage   // <-- SHFX-04 fix
        };

    // repPost.SamePackage=true → newPost warisi true.
    [Fact]
    public async Task NewPost_InheritsSamePackage_True()
    {
        var marker = "INH-T-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var existingUser = await SeedUserAsync(ctx, "exist");
            var repPost = Sess(existingUser, marker, "PostTest");
            repPost.SamePackage = true;   // grup ber-SamePackage
            ctx.AssessmentSessions.Add(repPost);
            await ctx.SaveChangesAsync();

            var newUser = await SeedUserAsync(ctx, "new");
            var newPost = BuildNewPost(repPost, newUser, marker);
            ctx.AssessmentSessions.Add(newPost);
            await ctx.SaveChangesAsync();
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        var posts = await read.AssessmentSessions.AsNoTracking()
            .Where(s => s.Title == marker && s.AssessmentType == "PostTest").ToListAsync();
        Assert.Equal(2, posts.Count);
        Assert.All(posts, p => Assert.True(p.SamePackage));   // BAIK rep maupun newPost = true
    }

    // repPost.SamePackage=false → newPost warisi false (tidak dipaksa true).
    [Fact]
    public async Task NewPost_InheritsSamePackage_False()
    {
        var marker = "INH-F-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        string newUserId;
        await using (var ctx = new ApplicationDbContext(_fixture.Options))
        {
            var existingUser = await SeedUserAsync(ctx, "exist");
            var repPost = Sess(existingUser, marker, "PostTest");
            repPost.SamePackage = false;
            ctx.AssessmentSessions.Add(repPost);
            await ctx.SaveChangesAsync();

            newUserId = await SeedUserAsync(ctx, "new");
            var newPost = BuildNewPost(repPost, newUserId, marker);
            ctx.AssessmentSessions.Add(newPost);
            await ctx.SaveChangesAsync();
        }

        await using var read = new ApplicationDbContext(_fixture.Options);
        var newPostRow = await read.AssessmentSessions.AsNoTracking()
            .FirstAsync(s => s.Title == marker && s.UserId == newUserId);
        Assert.False(newPostRow.SamePackage);   // warisi false (bukan dipaksa)
    }
}
