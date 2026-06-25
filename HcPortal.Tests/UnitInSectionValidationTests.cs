// Phase 399 Plan 02 Task 2 — MU-05 validasi Unit ∈ unit-Bagian (GREEN).
// Menguji WorkerController.ValidateUnitsInSection + GetUnitsForSectionAsync (ParentId hierarchy).
//   - Unit asing (di luar unit-Bagian pekerja) DITOLAK (error list non-empty).
//   - Unit valid (anak Section) DITERIMA (error list kosong).
//   - PrimaryUnit ∉ checked set → ditolak.
// Strategy: InMemory DB (Guid per test) + seed OrganizationUnits (Bagian → Unit anak).
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

public class UnitInSectionValidationTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // Seed: Bagian1 (id=1) → {UnitA(id=2), UnitB(id=3)}; Bagian2 (id=4) → {UnitZ(id=5)}
    private static async Task SeedOrgAsync(ApplicationDbContext ctx)
    {
        ctx.OrganizationUnits.AddRange(
            new OrganizationUnit { Id = 1, Name = "Bagian1", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 2, Name = "UnitA", Level = 1, ParentId = 1, IsActive = true },
            new OrganizationUnit { Id = 3, Name = "UnitB", Level = 1, ParentId = 1, IsActive = true },
            new OrganizationUnit { Id = 4, Name = "Bagian2", Level = 0, ParentId = null, IsActive = true },
            new OrganizationUnit { Id = 5, Name = "UnitZ", Level = 1, ParentId = 4, IsActive = true });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Validate_ForeignUnit_NotChildOfSection_IsRejected()
    {
        await using var ctx = InMemoryContext();
        await SeedOrgAsync(ctx);

        var validUnits = await ctx.GetUnitsForSectionAsync("Bagian1");   // {UnitA, UnitB}
        // submit UnitZ (anak Bagian2, asing untuk Bagian1)
        var errors = WorkerController.ValidateUnitsInSection(
            validUnits, new List<string> { "UnitA", "UnitZ" }, "UnitA", "Bagian1");

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("UnitZ"));
    }

    [Fact]
    public async Task Validate_UnitChildOfSection_IsAccepted()
    {
        await using var ctx = InMemoryContext();
        await SeedOrgAsync(ctx);

        var validUnits = await ctx.GetUnitsForSectionAsync("Bagian1");
        var errors = WorkerController.ValidateUnitsInSection(
            validUnits, new List<string> { "UnitA", "UnitB" }, "UnitA", "Bagian1");

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Validate_PrimaryNotInCheckedSet_IsRejected()
    {
        await using var ctx = InMemoryContext();
        await SeedOrgAsync(ctx);

        var validUnits = await ctx.GetUnitsForSectionAsync("Bagian1");
        // units={UnitA}, primary="UnitB" (tak dicentang) → error primary ∉ set
        var errors = WorkerController.ValidateUnitsInSection(
            validUnits, new List<string> { "UnitA" }, "UnitB", "Bagian1");

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Utama"));
    }
}
