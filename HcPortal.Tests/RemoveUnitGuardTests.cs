// Phase 399 Plan 02 Task 2 — MU-07 guard hapus-unit asimetris (GREEN).
// Menguji WorkerController.EvaluateRemoveUnitGuardAsync (D-10/D-11, Pitfall 4).
//   - Hapus unit dgn ProtonTrackAssignment aktif (resolved PROTON unit) → Blocked (hard).
//   - Hapus unit dgn CoachCoacheeMapping aktif (AssignmentUnit, tanpa PTA terkait) →
//     ConfirmedDeactivate=false → NeedConfirm; true → Deactivated (caller set IsActive/EndDate).
// Strategy: InMemory DB (Guid per test). Reuse pola active-record query (CoacheeId && IsActive).
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

public class RemoveUnitGuardTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task RemoveUnit_WithActiveProtonTrackAssignment_IsBlocked()
    {
        await using var ctx = InMemoryContext();
        var userId = Guid.NewGuid().ToString();
        // mapping aktif AssignmentUnit="UnitA" (unit PROTON teresolusi) + PTA aktif
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoacheeId = userId, CoachId = "c1", IsActive = true, AssignmentUnit = "UnitA" });
        ctx.ProtonTrackAssignments.Add(new ProtonTrackAssignment { CoacheeId = userId, ProtonTrackId = 1, IsActive = true });
        await ctx.SaveChangesAsync();

        // hapus UnitA (== PROTON unit) → hard-block
        var result = await WorkerController.EvaluateRemoveUnitGuardAsync(
            ctx, userId, oldPrimary: "UnitA", removed: new List<string> { "UnitA" }, confirmedDeactivate: false);

        Assert.Equal(WorkerController.RemoveUnitOutcome.Blocked, result.Outcome);
        Assert.Contains("PROTON", result.Message);
    }

    [Fact]
    public async Task RemoveUnit_WithActivePta_NoMapping_ResolvesViaOldPrimary_IsBlocked()
    {
        await using var ctx = InMemoryContext();
        var userId = Guid.NewGuid().ToString();
        // PTA aktif tanpa mapping → unit PROTON jatuh ke oldPrimary (fallback Pitfall 4 / Open Q1b)
        ctx.ProtonTrackAssignments.Add(new ProtonTrackAssignment { CoacheeId = userId, ProtonTrackId = 1, IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await WorkerController.EvaluateRemoveUnitGuardAsync(
            ctx, userId, oldPrimary: "UnitA", removed: new List<string> { "UnitA" }, confirmedDeactivate: false);

        Assert.Equal(WorkerController.RemoveUnitOutcome.Blocked, result.Outcome);
    }

    [Fact]
    public async Task RemoveUnit_WithActiveMapping_NoConfirm_RePrompts()
    {
        await using var ctx = InMemoryContext();
        var userId = Guid.NewGuid().ToString();
        // mapping aktif AssignmentUnit="UnitA", TANPA PTA aktif
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoacheeId = userId, CoachId = "c1", IsActive = true, AssignmentUnit = "UnitA" });
        await ctx.SaveChangesAsync();

        var result = await WorkerController.EvaluateRemoveUnitGuardAsync(
            ctx, userId, oldPrimary: "UnitA", removed: new List<string> { "UnitA" }, confirmedDeactivate: false);

        Assert.Equal(WorkerController.RemoveUnitOutcome.NeedConfirm, result.Outcome);
        Assert.NotNull(result.MappingToDeactivate);
        // belum dimutasi
        Assert.True(result.MappingToDeactivate!.IsActive);
    }

    [Fact]
    public async Task RemoveUnit_WithActiveMapping_Confirmed_AutoDeactivates()
    {
        await using var ctx = InMemoryContext();
        var userId = Guid.NewGuid().ToString();
        ctx.CoachCoacheeMappings.Add(new CoachCoacheeMapping { CoacheeId = userId, CoachId = "c1", IsActive = true, AssignmentUnit = "UnitA" });
        await ctx.SaveChangesAsync();

        var result = await WorkerController.EvaluateRemoveUnitGuardAsync(
            ctx, userId, oldPrimary: "UnitA", removed: new List<string> { "UnitA" }, confirmedDeactivate: true);

        Assert.Equal(WorkerController.RemoveUnitOutcome.Deactivated, result.Outcome);
        Assert.NotNull(result.MappingToDeactivate);
    }

    [Fact]
    public async Task RemoveUnit_NoActiveRefs_IsAllowed()
    {
        await using var ctx = InMemoryContext();
        var userId = Guid.NewGuid().ToString();

        var result = await WorkerController.EvaluateRemoveUnitGuardAsync(
            ctx, userId, oldPrimary: "UnitA", removed: new List<string> { "UnitA" }, confirmedDeactivate: false);

        Assert.Equal(WorkerController.RemoveUnitOutcome.Allowed, result.Outcome);
    }
}
