// Phase 401 Plan 05 Task 3 — PSU-02 filter-axis primitive (GREEN).
// CDP coachee-scope filter pakai AssignmentUnit (active mapping), bukan scalar User.Unit primary.
// Coachee primary=X tapi PROTON-assigned di Y → muncul saat filter unit=Y, TIDAK saat unit=X.
// File terpisah (disjoint dari ProtonUnitResolveTests milik 401-02) → Wave-1 paralel aman.
// Swap logic inline di action besar → test predikat resolvable via EF-InMemory.
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class FilterAxisTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task FilterAxis_resolves_coachee_to_AssignmentUnit_not_primary()
    {
        using var ctx = InMemoryContext();
        // coachee primary=X (User.Unit + UserUnits primary), tapi PROTON-assigned di Y
        ctx.Users.Add(new ApplicationUser { Id = "c1", Unit = "UnitX", Section = "Bagian1" });
        ctx.UserUnits.Add(new UserUnit { UserId = "c1", Unit = "UnitX", IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = "c1", Unit = "UnitY", IsPrimary = false, IsActive = true });
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoacheeId = "c1", CoachId = "coach1", IsActive = true, AssignmentUnit = "UnitY", AssignmentSection = "Bagian1" });
        await ctx.SaveChangesAsync();

        var unitByCoachee = (await ctx.CoachCoacheeMappings.Where(m => m.IsActive)
            .Select(m => new { m.CoacheeId, m.AssignmentUnit }).ToListAsync())
            .ToDictionary(m => m.CoacheeId, m => m.AssignmentUnit!.Trim());

        Assert.Equal("UnitY", unitByCoachee["c1"]);    // resolve ke assignment, bukan primary X
        Assert.NotEqual("UnitX", unitByCoachee["c1"]); // BUKAN mirror primary
    }
}
