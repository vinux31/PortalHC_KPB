// Phase 399 Plan 02 Task 1 — MU-02 audit set-diff (D-12) (GREEN).
// Menguji nilai return List<string> changes dari WorkerController.SyncUserUnitsAsync.
//   - set-diff hasilkan entri "Unit +'X'" (added), "Unit -'Y'" (removed),
//     "Primary: 'A' → 'B'" (primary changed) — bukan scalar "Unit: 'a' → 'b'".
// Strategy: InMemory DB (Guid per test). SyncUserUnitsAsync return List<string> changes.
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

public class UserUnitsAuditDiffTests
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
            UserName = "ad-" + Guid.NewGuid().ToString("N")[..8],
            Email = "ad@test.local",
            FullName = "Audit Diff",
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
    public async Task Diff_ReportsAddedAndRemovedUnits()
    {
        await using var ctx = InMemoryContext();
        var user = await SeedUserWithUnitsAsync(ctx, ("UnitA", true), ("UnitB", false));

        // old={A,B}, new={A,C} → "Unit +'UnitC'" dan "Unit -'UnitB'"
        var changes = await WorkerController.SyncUserUnitsAsync(ctx, user, new List<string> { "UnitA", "UnitC" }, "UnitA");

        Assert.Contains("Unit +'UnitC'", changes);
        Assert.Contains("Unit -'UnitB'", changes);
        // bukan scalar lama
        Assert.DoesNotContain(changes, c => c.StartsWith("Unit: '"));
    }

    [Fact]
    public async Task Diff_ReportsPrimaryChange()
    {
        await using var ctx = InMemoryContext();
        var user = await SeedUserWithUnitsAsync(ctx, ("UnitA", true), ("UnitB", false));

        // primary A→B
        var changes = await WorkerController.SyncUserUnitsAsync(ctx, user, new List<string> { "UnitA", "UnitB" }, "UnitB");

        Assert.Contains("Primary: 'UnitA' → 'UnitB'", changes);
    }
}
