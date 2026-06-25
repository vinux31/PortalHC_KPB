// Phase 402 Plan 01 — CXU-05 coach multi-unit self-scope union/narrow (Nyquist). InMemory dict-projection.
// BOTH halves of the scope decision are exercised against PRODUCTION code, no reimplemented comparison:
//   - CDPController.CoerceCoachUnitScope  : operator unit -> coach-owned-or-null (foreign -> null = union, no leak).
//   - CDPController.CoacheeMatchesUnitScope : per-coachee AssignmentUnit vs scope (null => union; else OrdinalIgnoreCase+Trim).
// These are the two pure seams the endpoint (CDPController FilterCoachingProton :329 / ExportDashboardProgress :362 /
// post-filter :541-544) routes through. Endpoint wiring + SQL-real round-trip = live UAT (402-04) + Phase 404 QA-03/04.
// CXU-05: coach with >1 active unit sees UNION of all their mapped coachees by default (unit=null); an OWNED unit narrows;
// a FOREIGN unit is coerced to null (no cross-coach leak).
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

    // ---- CXU-05a: PRODUCTION coercion seam (CDPController.CoerceCoachUnitScope) ----

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

    // ---- CXU-05b: PRODUCTION post-filter seam (CDPController.CoacheeMatchesUnitScope) ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Match_null_scope_is_union_always_in_scope(string? scope)
    {
        Assert.True(CDPController.CoacheeMatchesUnitScope("UnitX", scope));
        Assert.True(CDPController.CoacheeMatchesUnitScope("", scope));
        Assert.True(CDPController.CoacheeMatchesUnitScope(null, scope));
    }

    [Fact]
    public void Match_narrow_is_case_insensitive_and_trim_tolerant()
    {
        Assert.True(CDPController.CoacheeMatchesUnitScope("UnitY", "UnitY"));
        Assert.True(CDPController.CoacheeMatchesUnitScope("unity", "UnitY"));     // case-fold (the gap code-review flagged)
        Assert.True(CDPController.CoacheeMatchesUnitScope(" UnitY ", "UnitY"));   // trim-tolerant
        Assert.False(CDPController.CoacheeMatchesUnitScope("UnitX", "UnitY"));    // genuine mismatch narrows out
        Assert.False(CDPController.CoacheeMatchesUnitScope("", "UnitY"));         // blank AssignmentUnit not in a narrow scope
    }

    // ---- CXU-05c: union / narrow / foreign end-to-end (BOTH production seams composed) ----

    [Fact]
    public async Task Coach_union_when_unit_null_returns_all_mapped_coachees()
    {
        await using var ctx = InMemoryContext();
        await SeedCoachUnionAsync(ctx);

        var coachUnits = await ctx.UserUnits
            .Where(uu => uu.UserId == "coach1" && uu.IsActive).Select(uu => uu.Unit).ToListAsync();
        var mappings = await ctx.CoachCoacheeMappings
            .Where(m => m.CoachId == "coach1" && m.IsActive).ToListAsync();

        // operator supplies no unit => coercion yields null => post-filter is union (all in scope)
        var effectiveUnit = CDPController.CoerceCoachUnitScope(coachUnits, null);
        Assert.Null(effectiveUnit);

        var scoped = mappings
            .Where(m => CDPController.CoacheeMatchesUnitScope(m.AssignmentUnit, effectiveUnit))
            .Select(m => m.CoacheeId).OrderBy(x => x).ToList();

        Assert.Equal(new[] { "c1", "c2" }, scoped);   // union, not primary-only
    }

    [Fact]
    public async Task Coach_narrows_when_owned_unit_specified()
    {
        await using var ctx = InMemoryContext();
        await SeedCoachUnionAsync(ctx);

        var coachUnits = await ctx.UserUnits
            .Where(uu => uu.UserId == "coach1" && uu.IsActive).Select(uu => uu.Unit).ToListAsync();
        var mappings = await ctx.CoachCoacheeMappings
            .Where(m => m.CoachId == "coach1" && m.IsActive).ToListAsync();

        // operator picks owned "UnitY" — production coercion keeps it => post-filter narrows to c2
        var effectiveUnit = CDPController.CoerceCoachUnitScope(coachUnits, "UnitY");
        Assert.Equal("UnitY", effectiveUnit);

        var scoped = mappings
            .Where(m => CDPController.CoacheeMatchesUnitScope(m.AssignmentUnit, effectiveUnit))
            .Select(m => m.CoacheeId).ToList();

        Assert.Equal(new[] { "c2" }, scoped);
    }

    [Fact]
    public async Task Foreign_unit_coerced_to_null_yields_union()
    {
        await using var ctx = InMemoryContext();
        await SeedCoachUnionAsync(ctx);

        var coachUnits = await ctx.UserUnits
            .Where(uu => uu.UserId == "coach1" && uu.IsActive).Select(uu => uu.Unit).ToListAsync();
        var mappings = await ctx.CoachCoacheeMappings
            .Where(m => m.CoachId == "coach1" && m.IsActive).ToListAsync();

        // operator supplies unit="UnitZ" (∉ coach.UserUnits) — PRODUCTION coercion returns null => union (no leak)
        var effectiveUnit = CDPController.CoerceCoachUnitScope(coachUnits, "UnitZ");
        Assert.Null(effectiveUnit);

        var scoped = mappings
            .Where(m => CDPController.CoacheeMatchesUnitScope(m.AssignmentUnit, effectiveUnit))
            .Select(m => m.CoacheeId).OrderBy(x => x).ToList();

        Assert.Equal(new[] { "c1", "c2" }, scoped);   // coerced to union, not empty/leak
    }
}
