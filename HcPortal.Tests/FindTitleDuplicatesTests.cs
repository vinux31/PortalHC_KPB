// judul-fleksibel-cek-duplikat (2026-06-15) — uji helper SHARED FindTitleDuplicatesAsync vs real-SQL
// (pola DuplicateGuardTests + RecordCascadeFixture). Dup = judul ternormalisasi (trim+collapse ws+lower)
// LINTAS kategori; satu "assessment" = grup distinct (Title, Category, Schedule.Date).
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Xunit;

namespace HcPortal.Tests;

[Trait("Category", "Integration")]
public class FindTitleDuplicatesTests : IClassFixture<RecordCascadeFixture>
{
    private readonly RecordCascadeFixture _fixture;
    public FindTitleDuplicatesTests(RecordCascadeFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    private static async Task<string> SeedUserAsync(ApplicationDbContext ctx)
    {
        var u = new ApplicationUser { UserName = "ttl-" + Guid.NewGuid().ToString("N")[..8], Email = "ttl@test.local", FullName = "Title Test" };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u.Id;
    }

    private static AssessmentSession Sess(string userId, string title, string category, DateTime schedule) =>
        new AssessmentSession { UserId = userId, Title = title, Category = category, Status = "Upcoming", AccessToken = "", Schedule = schedule };

    [Fact]
    public async Task ExactTitle_Detected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var title = "Cek Judul " + Guid.NewGuid().ToString("N")[..8];
        ctx.AssessmentSessions.Add(Sess(uid, title, "OTS", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        var matches = await AdminBaseController.FindTitleDuplicatesAsync(ctx, title);
        Assert.NotEmpty(matches);
    }

    [Fact]
    public async Task CaseAndWhitespaceInsensitive_Detected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var token = Guid.NewGuid().ToString("N")[..8];
        ctx.AssessmentSessions.Add(Sess(uid, "Pre Test " + token, "OTS", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        // beda case + spasi ganda + spasi tepi → tetap kembar
        var matches = await AdminBaseController.FindTitleDuplicatesAsync(ctx, "  pre  test " + token + "  ");
        Assert.NotEmpty(matches);
    }

    [Fact]
    public async Task CrossCategory_Detected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        var title = "Lintas Kat " + Guid.NewGuid().ToString("N")[..8];
        ctx.AssessmentSessions.Add(Sess(uid, title, "Licencor", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        // judul sama, beda kategori → tetap kembar (lintas kategori)
        var matches = await AdminBaseController.FindTitleDuplicatesAsync(ctx, title);
        Assert.Contains(matches, m => m.Category == "Licencor");
    }

    [Fact]
    public async Task DifferentTitle_NotDetected()
    {
        await using var ctx = NewCtx();
        var uid = await SeedUserAsync(ctx);
        ctx.AssessmentSessions.Add(Sess(uid, "Judul A " + Guid.NewGuid().ToString("N")[..8], "OTS", new DateTime(2026, 4, 1)));
        await ctx.SaveChangesAsync();
        var matches = await AdminBaseController.FindTitleDuplicatesAsync(ctx, "Judul Z " + Guid.NewGuid().ToString("N")[..8]);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task EmptyTitle_ReturnsEmpty()
    {
        await using var ctx = NewCtx();
        Assert.Empty(await AdminBaseController.FindTitleDuplicatesAsync(ctx, ""));
        Assert.Empty(await AdminBaseController.FindTitleDuplicatesAsync(ctx, "   "));
    }
}
