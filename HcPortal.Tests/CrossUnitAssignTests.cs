// Phase 402 Plan 01 — CXU-01/02/03 cross-unit assign logic seams (RED-first, Nyquist). InMemory + static seam, no UserManager/HttpContext mock.
// Logic-seam tests; endpoint integration is Plan 02 (assign wiring) + Plan 03 (CDP scope) + Plan 04 (e2e UI).
// CXU-02 = coachee.Section must == coach.Section (cross-Bagian reject). CXU-03 = per-coachee unit ∈ coachee.UserUnits (batch reject on one bad).
// CXU-01 = eligible set-aware = whole Bagian (not unit). Pitfall 1: ApplicationUser has NO UserUnits nav — query junction.
using System;
using System.Linq;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class CrossUnitAssignTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // ---- CXU-02: CoacheeSectionMatchesCoach static seam ----

    [Fact]
    public async Task CoacheeSectionMatchesCoach_returns_true_when_same_section()
    {
        await using var ctx = InMemoryContext();
        ctx.Users.Add(new ApplicationUser { Id = "c1", Section = "Bagian1" });
        await ctx.SaveChangesAsync();

        Assert.True(await CoachMappingController.CoacheeSectionMatchesCoach(ctx, "c1", "Bagian1"));
    }

    [Fact]
    public async Task CoacheeSectionMatchesCoach_returns_false_when_cross_bagian()
    {
        await using var ctx = InMemoryContext();
        ctx.Users.Add(new ApplicationUser { Id = "c1", Section = "Bagian2" });
        await ctx.SaveChangesAsync();

        Assert.False(await CoachMappingController.CoacheeSectionMatchesCoach(ctx, "c1", "Bagian1"));
    }

    [Fact]
    public async Task CoacheeSectionMatchesCoach_returns_false_when_coach_section_empty()
    {
        await using var ctx = InMemoryContext();
        ctx.Users.Add(new ApplicationUser { Id = "c1", Section = "Bagian1" });
        await ctx.SaveChangesAsync();

        Assert.False(await CoachMappingController.CoacheeSectionMatchesCoach(ctx, "c1", null));
        Assert.False(await CoachMappingController.CoacheeSectionMatchesCoach(ctx, "c1", "  "));
    }

    // ---- CXU-03: per-coachee unit batch validation (reuse 401 helper) ----

    [Fact]
    public async Task PerCoachee_unit_batch_rejects_when_one_unit_not_in_coachee_units()
    {
        await using var ctx = InMemoryContext();
        ctx.UserUnits.Add(new UserUnit { UserId = "c1", Unit = "UnitA", IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = "c2", Unit = "UnitB", IsPrimary = true, IsActive = true });
        await ctx.SaveChangesAsync();

        // map {c1:"UnitA", c2:"UnitA"} — c1 owns UnitA (pass), c2 does NOT (fail) => whole batch must reject
        Assert.True(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", "UnitA"));
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c2", "UnitA"));
    }

    // ---- CXU-01: eligible set-aware via PRODUCTION CoachMappingController.FilterEligibleCoachees ----
    // Set-aware = active AND not-already-mapped, with NO unit scoping (cross-unit within a Bagian survives).

    [Fact]
    public void Eligible_set_aware_is_unit_agnostic_and_excludes_inactive_and_already_mapped()
    {
        // Candidates within one Bagian spanning two different units + one inactive + one already-mapped.
        // INSERTION order is deliberately NOT FullName order (c1="Zeta" first, c2="Alpha" second) so the
        // assertion below actually guards the OrderBy(u.FullName) contract — drop the OrderBy and this fails.
        var candidates = new[]
        {
            new ApplicationUser { Id = "c1", FullName = "Zeta",  Section = "SectionA", Unit = "UnitX", IsActive = true },
            new ApplicationUser { Id = "c2", FullName = "Alpha", Section = "SectionA", Unit = "UnitY", IsActive = true },
            new ApplicationUser { Id = "c4", FullName = "Mid",   Section = "SectionA", Unit = "UnitX", IsActive = false },  // inactive
            new ApplicationUser { Id = "c5", FullName = "Beta",  Section = "SectionA", Unit = "UnitY", IsActive = true },   // already mapped
        };
        var activeCoacheeIds = new[] { "c5" };

        var eligible = CoachMappingController.FilterEligibleCoachees(candidates, activeCoacheeIds)
            .Select(u => u.Id).ToList();

        Assert.Equal(new[] { "c2", "c1" }, eligible);   // sorted by FullName (Alpha < Zeta), NOT insertion order
        Assert.Contains("c1", eligible);                 // UnitX
        Assert.Contains("c2", eligible);                 // UnitY — multi-unit within Bagian still eligible (NOT unit-scoped)
        Assert.DoesNotContain("c4", eligible);           // inactive excluded
        Assert.DoesNotContain("c5", eligible);           // already actively-mapped excluded
    }

    [Fact]
    public void Eligible_set_handles_null_inputs()
    {
        Assert.Empty(CoachMappingController.FilterEligibleCoachees(null!, null!));
    }

}

// Phase 404 (QA-03 anchor, 402-carry) — single-active mapping is DB-enforced via filtered-unique
// IX_CoachCoacheeMappings_CoacheeId_ActiveUnique (ApplicationDbContext.cs:333-336). InMemory does NOT
// enforce filtered-unique indexes, so this MUST run SQL-real via MultiUnitSqlFixture. This live, passing
// Fact replaces the prior [Skip]-empty stub (the 402 VALIDATION anchor name resolves here now).
[Trait("Category", "Integration")]
public class CrossUnitAssignSqlTests : IClassFixture<MultiUnitSqlFixture>
{
    private readonly MultiUnitSqlFixture _fixture;
    public CrossUnitAssignSqlTests(MultiUnitSqlFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);

    [Fact]
    public async Task SingleActive_invariant_is_sql_real_phase404()
    {
        // Inserting a 2nd ACTIVE mapping for the same coachee MUST throw DbUpdateException on real SQL
        // (filtered-unique WHERE [IsActive]=1). Per-Fact unique coachee — DB is shared across Facts in the fixture.
        await using var ctx = NewCtx();
        var coachee = $"sa-{Guid.NewGuid():N}";
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoachId = "co-a", CoacheeId = coachee, IsActive = true, StartDate = DateTime.UtcNow, AssignmentUnit = MultiUnitSqlFixture.UnitX });
        await ctx.SaveChangesAsync();
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoachId = "co-b", CoacheeId = coachee, IsActive = true, StartDate = DateTime.UtcNow, AssignmentUnit = MultiUnitSqlFixture.UnitY });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }
}
