// Phase 399 Plan 02 Task 1 — MU-02 recompute primary (GREEN).
// Menguji WorkerController.SyncUserUnitsAsync recompute primary saat primary dihapus / unit dikosongkan.
//   - Hapus unit primary → promote unit lain jadi primary (deterministik: first remaining).
//   - Kosongkan SEMUA unit → ApplicationUser.Unit = null + 0 baris IsPrimary.
// Strategy: InMemory DB (Guid per test).
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

public class PrimaryMirrorTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<ApplicationUser> SeedUserWithUnitsAsync(
        ApplicationDbContext ctx, params (string unit, bool primary)[] units)
    {
        var u = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "pm-" + Guid.NewGuid().ToString("N")[..8],
            Email = "pm@test.local",
            FullName = "Primary Mirror",
            Section = "Bagian1",
            Unit = units.FirstOrDefault(x => x.primary).unit
        };
        ctx.Users.Add(u);
        foreach (var (unit, primary) in units)
            ctx.UserUnits.Add(new UserUnit { UserId = u.Id, Unit = unit, IsPrimary = primary, IsActive = true });
        await ctx.SaveChangesAsync();
        return u;
    }

    [Fact]
    public async Task RemovePrimaryUnit_PromotesAnotherUnitToPrimary()
    {
        await using var ctx = InMemoryContext();
        var user = await SeedUserWithUnitsAsync(ctx, ("UnitA", true), ("UnitB", false));

        // hapus A (primary lama), sisakan B; primary arg null → first remaining (B) jadi primary
        await WorkerController.SyncUserUnitsAsync(ctx, user, new List<string> { "UnitB" }, null);
        await ctx.SaveChangesAsync();

        var rows = await ctx.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();
        Assert.Single(rows);
        Assert.Equal("UnitB", rows.Single().Unit);
        Assert.True(rows.Single().IsPrimary);
        Assert.Equal("UnitB", user.Unit);   // mirror promote
    }

    [Fact]
    public async Task ClearAllUnits_SetsUnitNull_AndZeroPrimaryRows()
    {
        await using var ctx = InMemoryContext();
        var user = await SeedUserWithUnitsAsync(ctx, ("UnitA", true), ("UnitB", false));

        await WorkerController.SyncUserUnitsAsync(ctx, user, new List<string>(), null);
        await ctx.SaveChangesAsync();

        var rows = await ctx.UserUnits.Where(uu => uu.UserId == user.Id).ToListAsync();
        Assert.Empty(rows);
        Assert.Equal(0, rows.Count(r => r.IsPrimary));
        Assert.Null(user.Unit);   // mirror cleared
    }
}
