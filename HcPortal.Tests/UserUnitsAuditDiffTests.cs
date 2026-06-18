// Phase 399 Plan 01 Task 5 — WAVE 0 SCAFFOLD (RED, skip-with-reason).
// Kontrak test MU-02 audit set-diff (D-12). Diisi/diaktifkan plan 02 T1.
//
// Behavior yang dikontrak (plan 02):
//   - set-diff hasilkan entri "Unit +'X'" (added), "Unit -'Y'" (removed),
//     "Primary: 'A' → 'B'" (primary changed) — bukan scalar "Unit: 'a' → 'b'".
// Strategy: InMemory DB (Guid per test). SyncUserUnitsAsync return List<string> changes.
using System;
using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HcPortal.Tests;

public class UserUnitsAuditDiffTests
{
    private static ApplicationDbContext InMemoryContext() =>
        new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T1 (set-diff added/removed)")]
    public void Diff_ReportsAddedAndRemovedUnits()
    {
        // plan 02: old={A,B}, new={A,C} → changes berisi "Unit +'C'" dan "Unit -'B'".
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }

    [Fact(Skip = "Wave 0 scaffold — diisi plan 02 T1 (set-diff primary-changed)")]
    public void Diff_ReportsPrimaryChange()
    {
        // plan 02: primary A→B → changes berisi "Primary: 'A' → 'B'".
        Assert.True(false, "Wave 0 scaffold — diisi plan 02");
    }
}
