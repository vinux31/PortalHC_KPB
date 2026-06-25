// Phase 399 Plan 02 Task 1 — MU-01/MU-02 write-through set + mirror (GREEN).
// Menguji WorkerController.SyncUserUnitsAsync(ctx, user, units, primaryUnit) — single-source write-through.
//   - Set >1 unit dalam 1 Bagian ter-persist ke UserUnits (1 baris per unit).
//   - Tepat 1 baris IsPrimary=1.
//   - Mirror: ApplicationUser.Unit == baris IsPrimary (invariant #3).
// Strategy: InMemory DB (Guid per test). InMemory TIDAK enforce filtered-unique index
// (Pitfall 3) — enforce SQL-riil resmi di Phase 404 QA-01. Helper TIDAK SaveChanges → test commit.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class UserUnitsWriteThroughTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<ApplicationUser> SeedUserAsync(ApplicationDbContext ctx, string? section = "Bagian1")
    {
        var u = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "wt-" + Guid.NewGuid().ToString("N")[..8],
            Email = "wt@test.local",
            FullName = "Write Through",
            Section = section
        };
        ctx.Users.Add(u);
        await ctx.SaveChangesAsync();
        return u;
    }

    [Fact]
    public async Task SyncUserUnits_PersistsMultipleUnits_InOneSection()
    {
        await using var ctx = InMemoryContext();
        var user = await SeedUserAsync(ctx);

        await WorkerController.SyncUserUnitsAsync(ctx, user, new List<string> { "UnitA", "UnitB" }, "UnitA");
        await ctx.SaveChangesAsync();

        var rows = await ctx.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Unit == "UnitA");
        Assert.Contains(rows, r => r.Unit == "UnitB");
        Assert.All(rows, r => Assert.True(r.IsActive));
    }

    [Fact]
    public async Task SyncUserUnits_SetsExactlyOnePrimary()
    {
        await using var ctx = InMemoryContext();
        var user = await SeedUserAsync(ctx);

        await WorkerController.SyncUserUnitsAsync(ctx, user, new List<string> { "UnitA", "UnitB" }, "UnitB");
        await ctx.SaveChangesAsync();

        var rows = await ctx.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();
        Assert.Equal(1, rows.Count(r => r.IsPrimary));
        Assert.Equal("UnitB", rows.Single(r => r.IsPrimary).Unit);
    }

    [Fact]
    public async Task SyncUserUnits_NullOrInvalidPrimary_DefaultsToFirstChecked()
    {
        await using var ctx = InMemoryContext();
        var user = await SeedUserAsync(ctx);

        // primary null → first checked ("UnitA") jadi primary (D-02)
        await WorkerController.SyncUserUnitsAsync(ctx, user, new List<string> { "UnitA", "UnitB" }, null);
        await ctx.SaveChangesAsync();

        var rows = await ctx.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();
        Assert.Equal("UnitA", rows.Single(r => r.IsPrimary).Unit);
    }

    [Fact]
    public async Task SyncUserUnits_MirrorsPrimaryToApplicationUserUnit()
    {
        await using var ctx = InMemoryContext();
        var user = await SeedUserAsync(ctx);

        await WorkerController.SyncUserUnitsAsync(ctx, user, new List<string> { "UnitA", "UnitB" }, "UnitB");
        await ctx.SaveChangesAsync();

        // Mirror invariant #3: user.Unit == baris IsPrimary
        var primaryRow = await ctx.UserUnits.SingleAsync(uu => uu.UserId == user.Id && uu.IsPrimary);
        Assert.Equal(primaryRow.Unit, user.Unit);
        Assert.Equal("UnitB", user.Unit);
    }
}
