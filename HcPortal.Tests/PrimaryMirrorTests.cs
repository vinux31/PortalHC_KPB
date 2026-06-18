// Phase 399 Plan 01 Task 5 — WAVE 0 SCAFFOLD (RED, skip-with-reason).
// Kontrak test MU-02 recompute primary. Diisi/diaktifkan plan 02 T1.
//
// Behavior yang dikontrak (plan 02):
//   - Hapus unit primary → promote unit lain jadi primary (deterministik: first remaining).
//   - Kosongkan SEMUA unit → ApplicationUser.Unit = null + 0 baris IsPrimary.
// Strategy: InMemory DB (Guid per test).
using System;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class PrimaryMirrorTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T1 (promote primary saat primary dihapus)")]
    public void RemovePrimaryUnit_PromotesAnotherUnitToPrimary()
    {
        // plan 02: {A(primary),B} → hapus A → B jadi IsPrimary, user.Unit=B.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T1 (kosongkan semua unit → null)")]
    public void ClearAllUnits_SetsUnitNull_AndZeroPrimaryRows()
    {
        // plan 02: units=[] → user.Unit=null + 0 baris UserUnits IsPrimary=1.
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }
}
