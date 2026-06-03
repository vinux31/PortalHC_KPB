using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 340 TEST-01 — covers IOrgLabelService.GetLabel happy path + fallback contract (D-07).
/// Mutation methods (Update/Add/Delete) tested formally di Phase 344 per D-10.
/// </summary>
public class OrgLabelServiceTests
{
    private static OrgLabelService MakeService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.OrganizationLevelLabels.AddRange(
            new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" }
        );
        ctx.SaveChanges();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var auditLog = new AuditLogService(ctx);
        return new OrgLabelService(ctx, cache, auditLog);
    }

    [Fact]
    public void GetLabel_KnownLevel_ReturnsConfiguredLabel()
    {
        var svc = MakeService();

        Assert.Equal("Bagian",   svc.GetLabel(0));
        Assert.Equal("Unit",     svc.GetLabel(1));
        Assert.Equal("Sub-unit", svc.GetLabel(2));
    }

    [Fact]
    public void GetLabel_UnknownLevel_ReturnsFallback()
    {
        var svc = MakeService();

        Assert.Equal("Level 99", svc.GetLabel(99));
        Assert.Equal("Level 5",  svc.GetLabel(5));
    }
}
