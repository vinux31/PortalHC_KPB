// Phase 402 Plan 01 — CXU-05 coach multi-unit self-scope union/narrow (Nyquist). InMemory dict-projection.
// Coercion is exercised against PRODUCTION CDPController.CoerceCoachUnitScope (no reimplemented ternary — closes the
// false-confidence flagged by code-review IN-06 + secure audit). Endpoint wiring is Plan 03 (CDPController :305/:339);
// the AssignmentUnit post-filter (union/narrow) lives in BuildProtonProgressSubModelAsync and is covered by FilterAxisTests.
// CXU-05: coach with >1 active unit sees UNION of all their mapped coachees by default (unit=null); per-unit narrows;
// foreign unit coerced to null (no cross-coach leak).
// Pitfall 1: ApplicationUser has NO UserUnits nav — query junction UserUnits.
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
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

    // ---- CXU-05: PRODUCTION coercion seam (CDPController.CoerceCoachUnitScope) ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Coerce_blank_request_yields_null(string? requested)
    {
        var coachUnits = new[] { "UnitX", "UnitY" };
        Assert.Null(CDPController.CoerceCoachUnitScope(coachUnits, requested));   // blank => union default
    }

    [Fact]
    public void Coerce_owned_unit_is_kept_case_insensitive()
    {
        var coachUnits = new[] { "UnitX", "UnitY" };
        Assert.Equal("UnitY", CDPController.CoerceCoachUnitScope(coachUnits, "UnitY"));
        // case-insensitive + trim-tolerant: original (operator-supplied) string is returned unchanged when owned
        Assert.Equal(" unity ", CDPController.CoerceCoachUnitScope(coachUnits, " unity "));
    }

    [Fact]
    public void Coerce_foreign_unit_yields_null_no_leak()
    {
        var coachUnits = new[] { "UnitX", "UnitY" };
        // operator supplies a unit the coach does NOT own — must coerce to null (no cross-coach foreign-unit leak)
        Assert.Null(CDPController.CoerceCoachUnitScope(coachUnits, "UnitZ"));
    }

    [Fact]
    public void Coerce_empty_coach_units_yields_null()
    {
        Assert.Null(CDPController.CoerceCoachUnitScope(Array.Empty<string>(), "UnitX"));
    }

    // ---- CXU-05: union/narrow scope end-to-end (production coercion + mapping projection) ----

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
    public async Task Coach_narrows_when_owned_unit_specified()
    {
        await using var ctx = InMemoryContext();
        await SeedCoachUnionAsync(ctx);

        var coachUnits = await ctx.UserUnits
            .Where(uu => uu.UserId == "coach1" && uu.IsActive)
            .Select(uu => uu.Unit).ToListAsync();

        // operator picks owned "UnitY" — production coercion keeps it => AssignmentUnit narrow to c2
        var effectiveUnit = CDPController.CoerceCoachUnitScope(coachUnits, "UnitY");
        Assert.Equal("UnitY", effectiveUnit);

        var scoped = (await ctx.CoachCoacheeMappings
            .Where(m => m.CoachId == "coach1" && m.IsActive
                && (effectiveUnit == null || m.AssignmentUnit == effectiveUnit))
            .Select(m => m.CoacheeId).ToListAsync())
            .ToList();

        Assert.Equal(new[] { "c2" }, scoped);
    }

    [Fact]
    public async Task Foreign_unit_coerced_to_null_yields_union()
    {
        await using var ctx = InMemoryContext();
        await SeedCoachUnionAsync(ctx);

        var coachUnits = await ctx.UserUnits
            .Where(uu => uu.UserId == "coach1" && uu.IsActive)
            .Select(uu => uu.Unit).ToListAsync();

        // operator supplies unit="UnitZ" (∉ coach.UserUnits) — PRODUCTION coercion must return null => union (no leak)
        var effectiveUnit = CDPController.CoerceCoachUnitScope(coachUnits, "UnitZ");
        Assert.Null(effectiveUnit);

        var scoped = (await ctx.CoachCoacheeMappings
            .Where(m => m.CoachId == "coach1" && m.IsActive
                && (effectiveUnit == null || m.AssignmentUnit == effectiveUnit))
            .Select(m => m.CoacheeId).ToListAsync())
            .OrderBy(x => x).ToList();

        Assert.Equal(new[] { "c1", "c2" }, scoped);   // coerced to union, not empty/leak
    }
}
