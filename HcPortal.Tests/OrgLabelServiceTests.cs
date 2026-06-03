using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Services;
using Xunit;

namespace HcPortal.Tests;

/// <summary>
/// Phase 340 unit tests — IOrgLabelService contract coverage.
/// TEST-01 (ORG-LABEL-02c): GetLabel happy path + fallback (D-07).
/// G1-G6 extended coverage: GetAll sort, UpdateAsync/AddAsync/DeleteAsync mutate+cache+audit, GetMaxConfiguredLevel, GetMaxUsedLevelAsync.
/// </summary>
public class OrgLabelServiceTests
{
    // Returns service seeded with 3 default rows (Level 0/1/2).
    private static OrgLabelService MakeService()
    {
        var (svc, _) = MakeServiceWithCtx();
        return svc;
    }

    // Returns (service, context) pair so tests can query AuditLogs / OrganizationUnits directly.
    private static (OrgLabelService svc, ApplicationDbContext ctx) MakeServiceWithCtx(bool seed3Rows = true)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);

        if (seed3Rows)
        {
            ctx.OrganizationLevelLabels.AddRange(
                new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
                new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
                new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" }
            );
            ctx.SaveChanges();
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var auditLog = new AuditLogService(ctx);
        return (new OrgLabelService(ctx, cache, auditLog), ctx);
    }

    // ── TEST-01 — GetLabel (ORG-LABEL-02) ───────────────────────────────────

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

    // ── G1 — GetAll (ORG-LABEL-02c) ─────────────────────────────────────────

    [Fact]
    public void GetAll_Returns3SortedEntries()
    {
        // Arrange: seed rows deliberately out of order to verify sort.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);
        // Insert in reverse order to prove sort is applied by service, not insertion order.
        ctx.OrganizationLevelLabels.AddRange(
            new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" }
        );
        ctx.SaveChanges();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = new OrgLabelService(ctx, cache, new AuditLogService(ctx));

        // Act
        var dict = svc.GetAll();

        // Assert: exactly 3 entries
        Assert.Equal(3, dict.Count);

        // Assert: keys are sorted ascending
        var keys = dict.Keys.ToList();
        Assert.Equal(new[] { 0, 1, 2 }, keys);

        // Assert: values match
        Assert.Equal("Bagian",   dict[0]);
        Assert.Equal("Unit",     dict[1]);
        Assert.Equal("Sub-unit", dict[2]);
    }

    // ── G2 — UpdateAsync (ORG-LABEL-02d) ─────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_KnownLevel_UpdatesRowAndInvalidatesCacheAndLogsAudit()
    {
        // Arrange
        var (svc, ctx) = MakeServiceWithCtx();

        // Prime cache so we can confirm invalidation later.
        var before = svc.GetAll();
        Assert.Equal("Bagian", before[0]);

        // Act
        await svc.UpdateAsync(0, "Departemen", userId: "user-1", actorName: "Rino");

        // Assert: DB row updated
        var row = await ctx.OrganizationLevelLabels.FindAsync(0);
        Assert.NotNull(row);
        Assert.Equal("Departemen", row!.Label);

        // Assert: cache invalidated — call GetAll again; should reflect new label from DB
        var after = svc.GetAll();
        Assert.Equal("Departemen", after[0]);

        // Assert: audit log written
        var log = ctx.AuditLogs.SingleOrDefault(l =>
            l.ActionType == "OrgLabel-Update" &&
            l.TargetId == 0 &&
            l.TargetType == "OrganizationLevelLabel");
        Assert.NotNull(log);
        Assert.Equal("user-1", log!.ActorUserId);
        Assert.Contains("Bagian", log.Description);
        Assert.Contains("Departemen", log.Description);
    }

    [Fact]
    public async Task UpdateAsync_UnknownLevel_Throws()
    {
        var (svc, _) = MakeServiceWithCtx();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.UpdateAsync(99, "Ghost", userId: "user-1", actorName: "Rino"));

        Assert.Contains("99", ex.Message);
    }

    // ── G3 — AddAsync (ORG-LABEL-02e) ─────────────────────────────────────────

    [Fact]
    public async Task AddAsync_NewLevel_InsertsAndLogs()
    {
        // Arrange: start with 3 rows (0/1/2), add level 3
        var (svc, ctx) = MakeServiceWithCtx();

        // Act
        await svc.AddAsync(3, "Sub-sub-unit", userId: "user-2", actorName: "Widodo");

        // Assert: row exists in DB
        var row = await ctx.OrganizationLevelLabels.FindAsync(3);
        Assert.NotNull(row);
        Assert.Equal("Sub-sub-unit", row!.Label);
        Assert.Equal("user-2", row.UpdatedBy);

        // Assert: cache invalidated — GetAll should now show 4 entries
        var dict = svc.GetAll();
        Assert.Equal(4, dict.Count);
        Assert.Equal("Sub-sub-unit", dict[3]);

        // Assert: audit log written
        var log = ctx.AuditLogs.SingleOrDefault(l =>
            l.ActionType == "OrgLabel-Add" &&
            l.TargetId == 3 &&
            l.TargetType == "OrganizationLevelLabel");
        Assert.NotNull(log);
        Assert.Contains("Sub-sub-unit", log!.Description);
    }

    [Fact]
    public async Task AddAsync_ExistingLevel_Throws()
    {
        var (svc, _) = MakeServiceWithCtx();

        // Level 1 already exists in seed.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AddAsync(1, "Duplicate", userId: "user-2", actorName: "Widodo"));

        Assert.Contains("1", ex.Message);
    }

    // ── G4 — DeleteAsync (ORG-LABEL-02f) ──────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_KnownLevel_RemovesAndLogs()
    {
        // Arrange
        var (svc, ctx) = MakeServiceWithCtx();

        // Prime cache
        var before = svc.GetAll();
        Assert.Equal(3, before.Count);

        // Act: delete level 2 (Sub-unit)
        await svc.DeleteAsync(2, userId: "user-3", actorName: "Tri");

        // Assert: row removed from DB
        var row = await ctx.OrganizationLevelLabels.FindAsync(2);
        Assert.Null(row);

        // Assert: cache invalidated — GetAll returns 2 entries
        var after = svc.GetAll();
        Assert.Equal(2, after.Count);
        Assert.False(after.ContainsKey(2));

        // Assert: audit log written
        var log = ctx.AuditLogs.SingleOrDefault(l =>
            l.ActionType == "OrgLabel-Delete" &&
            l.TargetId == 2 &&
            l.TargetType == "OrganizationLevelLabel");
        Assert.NotNull(log);
        Assert.Contains("Sub-unit", log!.Description);
        Assert.Equal("user-3", log.ActorUserId);
    }

    [Fact]
    public async Task DeleteAsync_UnknownLevel_Throws()
    {
        var (svc, _) = MakeServiceWithCtx();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeleteAsync(99, userId: "user-3", actorName: "Tri"));

        Assert.Contains("99", ex.Message);
    }

    // ── G5 — GetMaxConfiguredLevel (ORG-LABEL-07a) ────────────────────────────

    [Fact]
    public void GetMaxConfiguredLevel_With3Rows_Returns2()
    {
        // Arrange: seeded with Level 0, 1, 2
        var svc = MakeService();

        // Act + Assert
        Assert.Equal(2, svc.GetMaxConfiguredLevel());
    }

    [Fact]
    public void GetMaxConfiguredLevel_Empty_Returns0()
    {
        // Arrange: no rows seeded
        var (svc, _) = MakeServiceWithCtx(seed3Rows: false);

        // Act + Assert: empty tabel → return 0
        Assert.Equal(0, svc.GetMaxConfiguredLevel());
    }

    // ── G6 — GetMaxUsedLevelAsync (ORG-LABEL-07b) ─────────────────────────────

    [Fact]
    public async Task GetMaxUsedLevelAsync_WithUnits_ReturnsMax()
    {
        // Arrange: insert OrganizationUnits with various Levels
        var (svc, ctx) = MakeServiceWithCtx();
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Name = "Bagian A",  Level = 0, IsActive = true },
            new OrganizationUnit { Name = "Unit B",    Level = 1, IsActive = true },
            new OrganizationUnit { Name = "Sub-unit C", Level = 2, IsActive = true }
        );
        ctx.SaveChanges();

        // Act
        var max = await svc.GetMaxUsedLevelAsync();

        // Assert
        Assert.Equal(2, max);
    }

    [Fact]
    public async Task GetMaxUsedLevelAsync_Empty_Returns0()
    {
        // Arrange: no OrganizationUnits in DB
        var (svc, _) = MakeServiceWithCtx();
        // OrganizationUnits table left empty (MakeServiceWithCtx only seeds OrganizationLevelLabels)

        // Act
        var max = await svc.GetMaxUsedLevelAsync();

        // Assert
        Assert.Equal(0, max);
    }
}
