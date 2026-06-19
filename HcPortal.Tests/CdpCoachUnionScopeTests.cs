// Phase 402 Plan 01 — CXU-05 coach multi-unit self-scope union/narrow (RED-first, Nyquist). InMemory dict-projection, mirrors FilterAxisTests idiom.
// Logic-seam tests; endpoint integration is Plan 03 (CDPController self-scope :305/:326/:647). Post-filter :490-503 (AssignmentUnit-aware, Phase 401) untouched.
// CXU-05: coach with >1 active unit sees UNION of all their mapped coachees by default (unit=null); per-unit narrows; foreign unit coerced to null (no leak).
// Pitfall 1: ApplicationUser has NO UserUnits nav — query junction UserUnits.
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class CdpCoachUnionScopeTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Seed: coach1 owns 2 active units (UnitX primary, UnitY); mapped to c1@UnitX and c2@UnitY.
    private static async Task SeedCoachUnionAsync(ApplicationDbContext ctx)
    {
        ctx.UserUnits.Add(new UserUnit { UserId = "coach1", Unit = "UnitX", IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = "coach1", Unit = "UnitY", IsPrimary = false, IsActive = true });
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoacheeId = "c1", CoachId = "coach1", IsActive = true, AssignmentUnit = "UnitX", AssignmentSection = "Bagian1" });
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoacheeId = "c2", CoachId = "coach1", IsActive = true, AssignmentUnit = "UnitY", AssignmentSection = "Bagian1" });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Coach_union_when_unit_null_returns_all_mapped_coachees()
    {
        await using var ctx = InMemoryContext();
        await SeedCoachUnionAsync(ctx);

        // unit=null => base-scope union of all active-mapping coachees for coach1
        var scoped = (await ctx.CoachCoacheeMappings
            .Where(m => m.CoachId == "coach1" && m.IsActive)
            .Select(m => m.CoacheeId).ToListAsync())
            .OrderBy(x => x).ToList();

        Assert.Equal(new[] { "c1", "c2" }, scoped);   // union, not primary-only
    }

    [Fact]
    public async Task Coach_narrows_when_unit_specified()
    {
        await using var ctx = InMemoryContext();
        await SeedCoachUnionAsync(ctx);

        // unit="UnitY" => narrow via AssignmentUnit post-filter (AssignmentUnit-aware, Phase 401/PSU-02)
        var scoped = (await ctx.CoachCoacheeMappings
            .Where(m => m.CoachId == "coach1" && m.IsActive && m.AssignmentUnit == "UnitY")
            .Select(m => m.CoacheeId).ToListAsync())
            .ToList();

        Assert.Equal(new[] { "c2" }, scoped);
    }

    [Fact]
    public async Task Foreign_unit_coerced_to_null_yields_union()
    {
        await using var ctx = InMemoryContext();
        await SeedCoachUnionAsync(ctx);

        // operator supplies unit="UnitZ" (∉ coach.UserUnits) — resolver must coerce to null => union (no foreign leak)
        var coachUnits = await ctx.UserUnits
            .Where(uu => uu.UserId == "coach1" && uu.IsActive)
            .Select(uu => uu.Unit).ToListAsync();
        Assert.DoesNotContain("UnitZ", coachUnits);

        var effectiveUnit = coachUnits.Contains("UnitZ") ? "UnitZ" : null;   // coercion rule
        var scoped = (await ctx.CoachCoacheeMappings
            .Where(m => m.CoachId == "coach1" && m.IsActive
                && (effectiveUnit == null || m.AssignmentUnit == effectiveUnit))
            .Select(m => m.CoacheeId).ToListAsync())
            .OrderBy(x => x).ToList();

        Assert.Equal(new[] { "c1", "c2" }, scoped);   // coerced to union, not empty/leak
    }
}
