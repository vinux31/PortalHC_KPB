using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HcPortal.Tests;

public class NormalizeOrganizationLevelsTests
{
    private static ApplicationDbContext NewCtx() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // Helper: add root, return Id
    private static async Task<int> AddRoot(ApplicationDbContext ctx, string name, int level)
    {
        var r = new OrganizationUnit { Name = name, ParentId = null, Level = level, IsActive = true };
        ctx.OrganizationUnits.Add(r);
        await ctx.SaveChangesAsync();
        return r.Id;
    }
    private static async Task AddChild(ApplicationDbContext ctx, string name, int parentId, int level)
    {
        ctx.OrganizationUnits.Add(new OrganizationUnit { Name = name, ParentId = parentId, Level = level, IsActive = true });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task SplitBrain_Root1_Child2_NormalizedTo0And1()
    {
        using var ctx = NewCtx();
        var rid = await AddRoot(ctx, "RFCC", 1);
        await AddChild(ctx, "UnitA", rid, 2);

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "RFCC").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "UnitA").Level);
    }

    [Fact]
    public async Task InactiveNodes_NormalizedByStructuralDepth_NotSkipped()
    {
        // Level is a structural property (depth), independent of IsActive — inactive units must still be normalized.
        using var ctx = NewCtx();
        var inactiveRoot = new OrganizationUnit { Name = "OldBagian", ParentId = null, Level = 1, IsActive = false };
        ctx.OrganizationUnits.Add(inactiveRoot);
        await ctx.SaveChangesAsync();
        ctx.OrganizationUnits.Add(new OrganizationUnit { Name = "InactiveUnit", ParentId = inactiveRoot.Id, Level = 2, IsActive = false });
        await ctx.SaveChangesAsync();

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "OldBagian").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "InactiveUnit").Level);
    }

    [Fact]
    public async Task AlreadyCorrect_NoChange()
    {
        using var ctx = NewCtx();
        var rid = await AddRoot(ctx, "LAB", 0);
        await AddChild(ctx, "U", rid, 1);

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "LAB").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "U").Level);
    }

    [Fact]
    public async Task MixedDevScenario_AllRootsZero_AllChildrenOne()
    {
        using var ctx = NewCtx();
        // 4 root sudah benar (L0, no child)
        foreach (var n in new[] { "LAB", "OM", "UTL II", "HC" }) await AddRoot(ctx, n, 0);
        // 4 root salah (L1) + anak L2
        foreach (var n in new[] { "RFCC", "DHT", "NGP", "GAST" })
        {
            var rid = await AddRoot(ctx, n, 1);
            await AddChild(ctx, $"{n}-U1", rid, 2);
            await AddChild(ctx, $"{n}-U2", rid, 2);
        }

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.All(ctx.OrganizationUnits.Where(u => u.ParentId == null), u => Assert.Equal(0, u.Level));
        Assert.All(ctx.OrganizationUnits.Where(u => u.ParentId != null), u => Assert.Equal(1, u.Level));
    }

    [Fact]
    public async Task Idempotent_SecondRun_NoChange()
    {
        using var ctx = NewCtx();
        var rid = await AddRoot(ctx, "RFCC", 1);
        await AddChild(ctx, "UnitA", rid, 2);

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());
        var logger2 = new CapturingLogger<SeedData>();
        await SeedData.NormalizeOrganizationLevelsAsync(ctx, logger2);

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "RFCC").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "UnitA").Level);
        Assert.Contains(logger2.Entries, e => e.Message.Contains("0 baris diubah"));
    }

    [Fact]
    public async Task Orphan_Unreachable_LeftUnchanged_WarningLogged()
    {
        using var ctx = NewCtx();
        // ParentId nunjuk Id tak-ada → tak terjangkau dari root
        ctx.OrganizationUnits.Add(new OrganizationUnit { Name = "Orphan", ParentId = 99999, Level = 5, IsActive = true });
        await ctx.SaveChangesAsync();
        var logger = new CapturingLogger<SeedData>();

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, logger);

        Assert.Equal(5, ctx.OrganizationUnits.Single(u => u.Name == "Orphan").Level); // unchanged
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task DeepTree_ThreeLevels_Normalized()
    {
        using var ctx = NewCtx();
        var rid = await AddRoot(ctx, "Root", 5);
        var c = new OrganizationUnit { Name = "Child", ParentId = rid, Level = 9, IsActive = true };
        ctx.OrganizationUnits.Add(c); await ctx.SaveChangesAsync();
        await AddChild(ctx, "Grand", c.Id, 0);

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());

        Assert.Equal(0, ctx.OrganizationUnits.Single(u => u.Name == "Root").Level);
        Assert.Equal(1, ctx.OrganizationUnits.Single(u => u.Name == "Child").Level);
        Assert.Equal(2, ctx.OrganizationUnits.Single(u => u.Name == "Grand").Level);
    }

    [Fact]
    public async Task DevScenario_DoubleRun_SecondIsNoOp()
    {
        using var ctx = NewCtx();
        foreach (var n in new[] { "LAB", "OM", "UTL II", "HC" }) await AddRoot(ctx, n, 0);
        foreach (var n in new[] { "RFCC", "DHT", "NGP", "GAST" })
        {
            var rid = await AddRoot(ctx, n, 1);
            for (int i = 0; i < 4; i++) await AddChild(ctx, $"{n}-U{i}", rid, 2);
        }

        await SeedData.NormalizeOrganizationLevelsAsync(ctx, new CapturingLogger<SeedData>());
        // 8 root, 16 child → {0:8, 1:16}
        Assert.Equal(8, ctx.OrganizationUnits.Count(u => u.Level == 0));
        Assert.Equal(16, ctx.OrganizationUnits.Count(u => u.Level == 1));

        var logger2 = new CapturingLogger<SeedData>();
        await SeedData.NormalizeOrganizationLevelsAsync(ctx, logger2);
        Assert.Contains(logger2.Entries, e => e.Message.Contains("0 baris diubah"));
    }
}
