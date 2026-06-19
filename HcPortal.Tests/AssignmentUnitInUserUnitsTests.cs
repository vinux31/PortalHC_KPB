// Phase 401 Plan 01 Task 2 — PSU-03 helper contract (GREEN against Task-1 helper).
// Menguji CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, coacheeId, assignmentUnit).
//   - AssignmentUnit ∈ unit AKTIF coachee (Trim + OrdinalIgnoreCase) → true.
//   - ∉ / inactive / empty / whitespace → false (skip/reject; JANGAN resolve dari primary).
// Sumber TUNGGAL = junction UserUnits (ApplicationUser TAK punya nav collection — Pitfall 1).
// Strategy: InMemory DB (Guid per test). Helper pure-read → tak butuh SaveChanges setelah seed.
using System;
using System.Threading.Tasks;
using HcPortal.Controllers;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class AssignmentUnitInUserUnitsTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedUnitAsync(ApplicationDbContext ctx, string coacheeId, string unit, bool isActive = true)
    {
        ctx.UserUnits.Add(new UserUnit { UserId = coacheeId, Unit = unit, IsPrimary = false, IsActive = isActive });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Valid_unit_in_active_userunits_returns_true()
    {
        await using var ctx = InMemoryContext();
        await SeedUnitAsync(ctx, "c1", "UnitB");

        var result = await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", "UnitB");

        Assert.True(result);
    }

    [Fact]
    public async Task Unit_not_in_userunits_returns_false()
    {
        await using var ctx = InMemoryContext();
        await SeedUnitAsync(ctx, "c1", "UnitB");

        var result = await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", "UnitZ");

        Assert.False(result);
    }

    [Fact]
    public async Task Empty_assignmentunit_returns_false()
    {
        await using var ctx = InMemoryContext();
        await SeedUnitAsync(ctx, "c1", "UnitB");

        // null dan whitespace harus false — JANGAN resolve dari primary (D-03/PSU-01)
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", null));
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", "  "));
    }

    [Fact]
    public async Task Inactive_userunit_returns_false()
    {
        await using var ctx = InMemoryContext();
        await SeedUnitAsync(ctx, "c1", "UnitB", isActive: false);

        var result = await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", "UnitB");

        Assert.False(result);
    }

    [Fact]
    public async Task Case_and_trim_insensitive_returns_true()
    {
        await using var ctx = InMemoryContext();
        await SeedUnitAsync(ctx, "c1", " unitb ");

        var result = await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", "UNITB");

        Assert.True(result);
    }

    // PSU-03 (401-06): kontrak validasi bypass TargetUnit — controller :1638 menolak TargetUnit
    // yg bukan ∈ worker.UserUnits aktif (cegah orphan AssignmentUnit, Invariant #4).
    [Fact]
    public async Task BypassTargetUnit_not_in_worker_userunits_rejected()
    {
        await using var ctx = InMemoryContext();
        ctx.UserUnits.Add(new UserUnit { UserId = "w1", Unit = "UnitX", IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = "w1", Unit = "UnitY", IsPrimary = false, IsActive = true });
        await ctx.SaveChangesAsync();

        // TargetUnit dimiliki worker → diterima
        Assert.True(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "w1", "UnitY"));
        // TargetUnit BUKAN milik worker → ditolak (akan orphan AssignmentUnit, Invariant #4)
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "w1", "UnitZ"));
        // TargetUnit kosong → ditolak (jangan resolve dari primary)
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "w1", ""));
    }

    // PSU-03 (401-03): Assign batch — semua coachee dapat AssignmentUnit SAMA (req.AssignmentUnit).
    // Loop reject batch bila ADA satu coachee yg tak punya unit itu (CoachMappingController Assign
    // :474+). Primitif keputusan per-coachee: c1 punya "UnitA" → true; c2 tak punya → false.
    [Fact]
    public async Task Assign_batch_rejects_when_one_coachee_lacks_unit()
    {
        await using var ctx = InMemoryContext();
        ctx.UserUnits.Add(new UserUnit { UserId = "c1", Unit = "UnitA", IsPrimary = true, IsActive = true });
        ctx.UserUnits.Add(new UserUnit { UserId = "c2", Unit = "UnitB", IsPrimary = true, IsActive = true });
        await ctx.SaveChangesAsync();

        // batch assign "UnitA" ke {c1, c2}: c1 lolos, c2 gagal ⇒ loop tolak seluruh batch
        Assert.True(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c1", "UnitA"));
        Assert.False(await CoachMappingController.ValidateAssignmentUnitInUserUnits(ctx, "c2", "UnitA"));
    }
}
