// Phase 399 Plan 01 Task 5 — WAVE 0 SCAFFOLD (RED, skip-with-reason).
// Kontrak test MU-01/MU-02 write-through set + mirror. Diisi/diaktifkan plan 02 T1 saat
// helper SyncUserUnitsAsync(user, units, primaryUnit) di WorkerController sudah ada.
//
// Behavior yang dikontrak (plan 02):
//   - Set >1 unit dalam 1 Bagian ter-persist ke UserUnits (1 baris per unit).
//   - Tepat 1 baris IsPrimary=1.
//   - Mirror: ApplicationUser.Unit == baris IsPrimary (invariant #3).
// Strategy: InMemory DB (Guid per test). InMemory TIDAK enforce filtered-unique index
// (Pitfall 3) — enforce SQL-riil resmi di Phase 404 QA-01.
using System;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class UserUnitsWriteThroughTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T1 (SyncUserUnitsAsync write-through)")]
    public void SyncUserUnits_PersistsMultipleUnits_InOneSection()
    {
        // Arrange/Act/Assert diisi plan 02: panggil SyncUserUnitsAsync(user, ["UnitA","UnitB"], "UnitA")
        // → UserUnits berisi 2 baris (UnitA, UnitB) untuk user.Id.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T1 (tepat 1 IsPrimary)")]
    public void SyncUserUnits_SetsExactlyOnePrimary()
    {
        // plan 02: tepat 1 baris IsPrimary=1 (== primaryUnit arg, atau first bila invalid/null).
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T1 (mirror ApplicationUser.Unit)")]
    public void SyncUserUnits_MirrorsPrimaryToApplicationUserUnit()
    {
        // plan 02: user.Unit == baris IsPrimary (invariant #3 write-through).
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }
}
