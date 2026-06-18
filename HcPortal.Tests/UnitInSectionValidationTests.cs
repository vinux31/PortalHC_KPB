// Phase 399 Plan 01 Task 5 — WAVE 0 SCAFFOLD (RED, skip-with-reason).
// Kontrak test MU-05 validasi Unit ∈ unit-Bagian. Diisi/diaktifkan plan 02 T2.
//
// Behavior yang dikontrak (plan 02):
//   - Unit asing (di luar unit-Bagian pekerja) DITOLAK (ModelState error).
//   - Unit valid (anak Section) DITERIMA.
//   - PrimaryUnit ∉ checked set → ditolak.
// Strategy: InMemory DB (Guid per test) + GetUnitsForSectionAsync (seed OrganizationUnits
// Bagian→Unit anak). InMemory mendukung query ParentId hierarchy GetUnitsForSectionAsync.
using System;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class UnitInSectionValidationTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T2 (unit asing ditolak)")]
    public void Validate_ForeignUnit_NotChildOfSection_IsRejected()
    {
        // plan 02: Section="Bagian1" punya {UnitA}; submit "UnitZ" → invalid (rejected).
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T2 (unit valid diterima)")]
    public void Validate_UnitChildOfSection_IsAccepted()
    {
        // plan 02: Section="Bagian1" punya {UnitA,UnitB}; submit {UnitA,UnitB} → valid.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T2 (primary ∉ set ditolak)")]
    public void Validate_PrimaryNotInCheckedSet_IsRejected()
    {
        // plan 02: units={UnitA}, primary="UnitB" (tak dicentang) → ditolak / fallback first.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }
}
